using System;
using UnityEngine;

//27092026 GPU-Loeser fuer eine Pyramidenstufe mit TGV-Regularisierung (Shader Resources/TGVL1.compute).
//  Gleiche Schnittstelle wie TvL1Gpu (Begin / RunWarp / End), damit vis_3D beide gleich aufrufen kann.
//  TvL1Gpu und TVL1.compute bleiben unveraendert; TGV ist ueber vis_3D.set_tv_use_tgv umschaltbar.
//  Alle Methoden im Unity-Hauptthread aufrufen.
public class TgvL1Gpu : IDisposable
{
    const int GROUP = 256;
    static readonly float STEP = 1f / Mathf.Sqrt(12f); // tau = sigma, tau*sigma*||K||^2 <= 1

    ComputeShader cs;
    int k_warp, k_reduce, k_data, k_dual_p, k_dual_q, k_primal_u, k_primal_w;

    ComputeBuffer b_i0, b_i1, b_i1x, b_i1y, b_i1wx, b_i1wy, b_grad, b_rhoc;
    ComputeBuffer b_u1, b_u2, b_ub1, b_ub2, b_v1, b_v2;
    ComputeBuffer b_p11, b_p12, b_p21, b_p22;
    ComputeBuffer b_w11, b_w12, b_w21, b_w22, b_wb11, b_wb12, b_wb21, b_wb22;
    ComputeBuffer b_q1a, b_q1b, b_q1c, b_q2a, b_q2b, b_q2c;
    ComputeBuffer b_partial, b_state, b_err;

    int size, groups, max_iterations;
    readonly uint[] state_host = new uint[2];

    public static string last_error = null;

    /// <summary>
    /// Loads the TGV compute shader (Resources/TGVL1.compute) and looks up its kernels; does nothing if already loaded.
    /// </summary>
    /// <returns>True if the shader and all kernels were found.</returns>
    public bool Load()
    {
        if (cs != null)
            return true;
        cs = Resources.Load<ComputeShader>("TGVL1");
        if (cs == null)
        {
            last_error = "ComputeShader Resources/TGVL1.compute nicht gefunden";
            return false;
        }
        string[] names = { "KWarp", "KReduce", "KTgvData", "KTgvDualP", "KTgvDualQ", "KTgvPrimalU", "KTgvPrimalW" };
        int[] ids = new int[names.Length];
        for (int k = 0; k < names.Length; k++)
        {
            ids[k] = cs.FindKernel(names[k]);
            if (!cs.IsSupported(ids[k]))
            {
                last_error = "Kernel " + names[k] + " in TGVL1.compute nicht lauffaehig (Shader-Compile-Fehler?)";
                cs = null;
                return false;
            }
        }
        (k_warp, k_reduce, k_data, k_dual_p, k_dual_q, k_primal_u, k_primal_w) =
            (ids[0], ids[1], ids[2], ids[3], ids[4], ids[5], ids[6]);
        Debug.Log("TGV-L1 GPU bereit: " + SystemInfo.graphicsDeviceName);
        return true;
    }

    /// <summary>
    /// Uploads the data of one pyramid level to the GPU and initialises the primal variables (flow, auxiliary field, second-order field w) and the dual variables of the TGV energy. Must be called on the Unity main thread before RunWarp.
    /// </summary>
    /// <param name="I0">Reference image of the level (row-major, nx*ny).</param>
    /// <param name="I1">Target image of the level.</param>
    /// <param name="I1x">x derivative of the target image.</param>
    /// <param name="I1y">y derivative of the target image.</param>
    /// <param name="u1">Initial horizontal flow (from the coarser level).</param>
    /// <param name="u2">Initial vertical flow.</param>
    /// <param name="nx">Width of the level.</param>
    /// <param name="ny">Height of the level.</param>
    /// <param name="lambda">Weight of the data term.</param>
    /// <param name="theta">Coupling parameter between flow and auxiliary variable.</param>
    /// <param name="epsilon">Stopping tolerance of the iterations.</param>
    /// <param name="max_its">Maximum number of iterations per warp.</param>
    /// <param name="grad_is_zero">Threshold below which the squared image gradient is treated as zero.</param>
    /// <param name="alpha0">Weight of the second-order term of TGV.</param>
    /// <param name="alpha1">Weight of the first-order term of TGV.</param>
    public void Begin(float[] I0, float[] I1, float[] I1x, float[] I1y, float[] u1, float[] u2,
        int nx, int ny, float lambda, float theta, float epsilon, int max_its, float grad_is_zero,
        float alpha0, float alpha1)
    {
        Release();
        size = nx * ny;
        groups = (size + GROUP - 1) / GROUP;
        max_iterations = max_its;

        b_i0 = Upload(I0); b_i1 = Upload(I1); b_i1x = Upload(I1x); b_i1y = Upload(I1y);
        b_u1 = Upload(u1); b_u2 = Upload(u2);
        b_ub1 = Upload(u1); b_ub2 = Upload(u2); // ubar = u zu Beginn der Stufe
        b_i1wx = Zeros(size); b_i1wy = Zeros(size); b_grad = Zeros(size); b_rhoc = Zeros(size);
        b_v1 = Zeros(size); b_v2 = Zeros(size);
        b_p11 = Zeros(size); b_p12 = Zeros(size); b_p21 = Zeros(size); b_p22 = Zeros(size);
        b_w11 = Zeros(size); b_w12 = Zeros(size); b_w21 = Zeros(size); b_w22 = Zeros(size);
        b_wb11 = Zeros(size); b_wb12 = Zeros(size); b_wb21 = Zeros(size); b_wb22 = Zeros(size);
        b_q1a = Zeros(size); b_q1b = Zeros(size); b_q1c = Zeros(size);
        b_q2a = Zeros(size); b_q2b = Zeros(size); b_q2c = Zeros(size);
        b_partial = Zeros(groups);
        b_err = Zeros(1);
        b_state = new ComputeBuffer(2, sizeof(uint));
        b_state.SetData(new uint[] { 0u, 0u });

        cs.SetInt("nx", nx);
        cs.SetInt("ny", ny);
        cs.SetInt("size_px", size);
        cs.SetInt("num_groups", groups);
        cs.SetFloat("l_t", lambda * theta);
        cs.SetFloat("theta", theta);
        cs.SetFloat("eps_sq", epsilon * epsilon);
        cs.SetFloat("grad_is_zero", grad_is_zero);
        cs.SetFloat("tgv_tau", STEP);
        cs.SetFloat("tgv_sigma", STEP);
        cs.SetFloat("alpha0", alpha0);
        cs.SetFloat("alpha1", alpha1);

        // Warp (wie TvL1Gpu)
        cs.SetBuffer(k_warp, "I0_r", b_i0);
        cs.SetBuffer(k_warp, "I1_r", b_i1);
        cs.SetBuffer(k_warp, "I1x_r", b_i1x);
        cs.SetBuffer(k_warp, "I1y_r", b_i1y);
        cs.SetBuffer(k_warp, "U1_r", b_u1);
        cs.SetBuffer(k_warp, "U2_r", b_u2);
        cs.SetBuffer(k_warp, "I1wx", b_i1wx);
        cs.SetBuffer(k_warp, "I1wy", b_i1wy);
        cs.SetBuffer(k_warp, "Grad", b_grad);
        cs.SetBuffer(k_warp, "RhoC", b_rhoc);

        cs.SetBuffer(k_data, "State_r", b_state);
        cs.SetBuffer(k_data, "U1_r", b_u1);
        cs.SetBuffer(k_data, "U2_r", b_u2);
        cs.SetBuffer(k_data, "I1wx_r", b_i1wx);
        cs.SetBuffer(k_data, "I1wy_r", b_i1wy);
        cs.SetBuffer(k_data, "Grad_r", b_grad);
        cs.SetBuffer(k_data, "RhoC_r", b_rhoc);
        cs.SetBuffer(k_data, "V1", b_v1);
        cs.SetBuffer(k_data, "V2", b_v2);

        cs.SetBuffer(k_dual_p, "State_r", b_state);
        cs.SetBuffer(k_dual_p, "Ub1_r", b_ub1);
        cs.SetBuffer(k_dual_p, "Ub2_r", b_ub2);
        cs.SetBuffer(k_dual_p, "Wb11_r", b_wb11);
        cs.SetBuffer(k_dual_p, "Wb12_r", b_wb12);
        cs.SetBuffer(k_dual_p, "Wb21_r", b_wb21);
        cs.SetBuffer(k_dual_p, "Wb22_r", b_wb22);
        cs.SetBuffer(k_dual_p, "P11", b_p11);
        cs.SetBuffer(k_dual_p, "P12", b_p12);
        cs.SetBuffer(k_dual_p, "P21", b_p21);
        cs.SetBuffer(k_dual_p, "P22", b_p22);

        cs.SetBuffer(k_dual_q, "State_r", b_state);
        cs.SetBuffer(k_dual_q, "Wb11_r", b_wb11);
        cs.SetBuffer(k_dual_q, "Wb12_r", b_wb12);
        cs.SetBuffer(k_dual_q, "Wb21_r", b_wb21);
        cs.SetBuffer(k_dual_q, "Wb22_r", b_wb22);
        cs.SetBuffer(k_dual_q, "Q1a", b_q1a);
        cs.SetBuffer(k_dual_q, "Q1b", b_q1b);
        cs.SetBuffer(k_dual_q, "Q1c", b_q1c);
        cs.SetBuffer(k_dual_q, "Q2a", b_q2a);
        cs.SetBuffer(k_dual_q, "Q2b", b_q2b);
        cs.SetBuffer(k_dual_q, "Q2c", b_q2c);

        cs.SetBuffer(k_primal_u, "State_r", b_state);
        cs.SetBuffer(k_primal_u, "P11_r", b_p11);
        cs.SetBuffer(k_primal_u, "P12_r", b_p12);
        cs.SetBuffer(k_primal_u, "P21_r", b_p21);
        cs.SetBuffer(k_primal_u, "P22_r", b_p22);
        cs.SetBuffer(k_primal_u, "V1_r", b_v1);
        cs.SetBuffer(k_primal_u, "V2_r", b_v2);
        cs.SetBuffer(k_primal_u, "U1", b_u1);
        cs.SetBuffer(k_primal_u, "U2", b_u2);
        cs.SetBuffer(k_primal_u, "Ub1", b_ub1);
        cs.SetBuffer(k_primal_u, "Ub2", b_ub2);
        cs.SetBuffer(k_primal_u, "Partial", b_partial);

        cs.SetBuffer(k_primal_w, "State_r", b_state);
        cs.SetBuffer(k_primal_w, "P11_r", b_p11);
        cs.SetBuffer(k_primal_w, "P12_r", b_p12);
        cs.SetBuffer(k_primal_w, "P21_r", b_p21);
        cs.SetBuffer(k_primal_w, "P22_r", b_p22);
        cs.SetBuffer(k_primal_w, "Q1a_r", b_q1a);
        cs.SetBuffer(k_primal_w, "Q1b_r", b_q1b);
        cs.SetBuffer(k_primal_w, "Q1c_r", b_q1c);
        cs.SetBuffer(k_primal_w, "Q2a_r", b_q2a);
        cs.SetBuffer(k_primal_w, "Q2b_r", b_q2b);
        cs.SetBuffer(k_primal_w, "Q2c_r", b_q2c);
        cs.SetBuffer(k_primal_w, "W11", b_w11);
        cs.SetBuffer(k_primal_w, "W12", b_w12);
        cs.SetBuffer(k_primal_w, "W21", b_w21);
        cs.SetBuffer(k_primal_w, "W22", b_w22);
        cs.SetBuffer(k_primal_w, "Wb11", b_wb11);
        cs.SetBuffer(k_primal_w, "Wb12", b_wb12);
        cs.SetBuffer(k_primal_w, "Wb21", b_wb21);
        cs.SetBuffer(k_primal_w, "Wb22", b_wb22);

        cs.SetBuffer(k_reduce, "Partial_r", b_partial);
        cs.SetBuffer(k_reduce, "State", b_state);
        cs.SetBuffer(k_reduce, "ErrOut", b_err);
    }

    /// <summary>
    /// Performs one warp: warps the target image with the current flow and runs the TGV primal-dual iterations until convergence or the iteration limit.
    /// </summary>
    /// <param name="warp">Index of the warp within the level (0-based).</param>
    /// <returns>Number of iterations executed.</returns>
    public int RunWarp(int warp)
    {
        cs.SetInt("warp_idx", warp);
        cs.Dispatch(k_warp, groups, 1, 1);

        state_host[0] = 0u;
        state_host[1] = 0u;
        b_state.SetData(state_host);

        for (int n = 0; n < max_iterations; n++)
        {
            cs.Dispatch(k_data, groups, 1, 1);
            cs.Dispatch(k_dual_p, groups, 1, 1);
            cs.Dispatch(k_dual_q, groups, 1, 1);
            cs.Dispatch(k_primal_u, groups, 1, 1);
            cs.Dispatch(k_primal_w, groups, 1, 1);
            cs.Dispatch(k_reduce, 1, 1, 1);
        }

        b_state.GetData(state_host);
        return (int)state_host[1];
    }

    /// <summary>
    /// Downloads the flow of the level from the GPU and releases all buffers.
    /// </summary>
    /// <param name="u1_out">Receives the horizontal flow (nx*ny).</param>
    /// <param name="u2_out">Receives the vertical flow (nx*ny).</param>
    public void End(float[] u1_out, float[] u2_out)
    {
        b_u1.GetData(u1_out, 0, 0, size);
        b_u2.GetData(u2_out, 0, 0, size);
        Release();
    }

    /// <summary>
    /// Creates a compute buffer and fills it with the given data.
    /// </summary>
    /// <param name="data">Values to upload.</param>
    /// <returns>The new buffer (caller releases it).</returns>
    static ComputeBuffer Upload(float[] data)
    {
        ComputeBuffer b = new ComputeBuffer(data.Length, sizeof(float));
        b.SetData(data);
        return b;
    }

    /// <summary>
    /// Creates a zero-initialised compute buffer.
    /// </summary>
    /// <param name="n">Number of float elements (at least 1).</param>
    /// <returns>The new buffer.</returns>
    static ComputeBuffer Zeros(int n)
    {
        ComputeBuffer b = new ComputeBuffer(Math.Max(1, n), sizeof(float));
        b.SetData(new float[Math.Max(1, n)]);
        return b;
    }

    /// <summary>
    /// Releases a compute buffer if it exists and sets the reference to null.
    /// </summary>
    /// <param name="b">Buffer to release.</param>
    static void Rel(ref ComputeBuffer b)
    {
        if (b != null)
        {
            b.Release();
            b = null;
        }
    }

    /// <summary>
    /// Releases all GPU buffers of the current level.
    /// </summary>
    public void Release()
    {
        Rel(ref b_i0); Rel(ref b_i1); Rel(ref b_i1x); Rel(ref b_i1y);
        Rel(ref b_i1wx); Rel(ref b_i1wy); Rel(ref b_grad); Rel(ref b_rhoc);
        Rel(ref b_u1); Rel(ref b_u2); Rel(ref b_ub1); Rel(ref b_ub2); Rel(ref b_v1); Rel(ref b_v2);
        Rel(ref b_p11); Rel(ref b_p12); Rel(ref b_p21); Rel(ref b_p22);
        Rel(ref b_w11); Rel(ref b_w12); Rel(ref b_w21); Rel(ref b_w22);
        Rel(ref b_wb11); Rel(ref b_wb12); Rel(ref b_wb21); Rel(ref b_wb22);
        Rel(ref b_q1a); Rel(ref b_q1b); Rel(ref b_q1c); Rel(ref b_q2a); Rel(ref b_q2b); Rel(ref b_q2c);
        Rel(ref b_partial); Rel(ref b_state); Rel(ref b_err);
    }

    /// <summary>
    /// Releases all GPU resources (IDisposable).
    /// </summary>
    public void Dispose()
    {
        Release();
    }
}
