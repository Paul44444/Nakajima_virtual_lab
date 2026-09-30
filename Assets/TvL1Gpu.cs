using System;
using UnityEngine;

//23092026 GPU-Loeser fuer eine Pyramidenstufe von TV-L1 (Gegenstueck zu vis_3D.Dual_TVL1_optic_flow_new).
//  Shader: Assets/Resources/TVL1.compute. Alle Methoden muessen im Unity-Hauptthread laufen
//  (ComputeShader/ComputeBuffer), vis_3D ruft sie deshalb ueber run_on_main_thread auf.
//  Ablauf je Stufe: Begin (Hochladen) -> RunWarp je Warp -> End (Herunterladen, Puffer freigeben).
public class TvL1Gpu : IDisposable
{
    const int GROUP = 256;

    ComputeShader cs;
    int k_warp, k_step_a, k_step_b, k_reduce;

    ComputeBuffer b_i0, b_i1, b_i1x, b_i1y;
    ComputeBuffer b_i1wx, b_i1wy, b_grad, b_rhoc;
    ComputeBuffer b_u1, b_u2;
    ComputeBuffer b_p11, b_p12, b_p21, b_p22;
    ComputeBuffer b_partial, b_state, b_err;

    int size, groups, max_iterations;
    readonly uint[] state_host = new uint[2];

    public static string last_error = null;

    /// <summary>
    /// Checks whether the current graphics device supports compute shaders.
    /// </summary>
    /// <returns>True if compute shaders are available.</returns>
    public static bool IsSupported()
    {
        return SystemInfo.supportsComputeShaders;
    }

    /// <summary>
    /// Loads the TV-L1 compute shader (Resources/TVL1.compute) and looks up its kernels; does nothing if already loaded.
    /// </summary>
    /// <returns>True if the shader and all kernels were found.</returns>
    public bool Load()
    {
        if (cs != null)
            return true;
        cs = Resources.Load<ComputeShader>("TVL1");
        if (cs == null)
        {
            last_error = "ComputeShader Resources/TVL1.compute nicht gefunden";
            return false;
        }
        k_warp = cs.FindKernel("KWarp");
        k_step_a = cs.FindKernel("KStepA");
        k_step_b = cs.FindKernel("KStepB");
        k_reduce = cs.FindKernel("KReduce");
        // nicht kompilierte Kernels (Shader-Fehler) sofort erkennen statt falsche Ergebnisse zu liefern
        string[] names = { "KWarp", "KStepA", "KStepB", "KReduce" };
        int[] ids = { k_warp, k_step_a, k_step_b, k_reduce };
        for (int k = 0; k < ids.Length; k++)
        {
            if (!cs.IsSupported(ids[k]))
            {
                last_error = "Kernel " + names[k] + " in TVL1.compute nicht lauffaehig (Shader-Compile-Fehler?)";
                cs = null;
                return false;
            }
        }
        Debug.Log("TV-L1 GPU bereit: " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")");
        return true;
    }

    // Maximale Gruppenzahl je Dispatch-Dimension (D3D11/12)
    /// <summary>
    /// Checks whether an image of the given size fits into a single one-dimensional dispatch (at most 65535 thread groups).
    /// </summary>
    /// <param name="nx">Image width in pixels.</param>
    /// <param name="ny">Image height in pixels.</param>
    /// <returns>True if the image can be processed in one dispatch.</returns>
    public static bool FitsDispatch(int nx, int ny)
    {
        return (nx * ny + GROUP - 1) / GROUP <= 65535;
    }

    /// <summary>
    /// Uploads the data of one pyramid level to the GPU and initialises the primal and dual variables. Must be called on the Unity main thread before RunWarp.
    /// </summary>
    /// <param name="I0">Reference image of the level (row-major, nx*ny).</param>
    /// <param name="I1">Target image of the level.</param>
    /// <param name="I1x">x derivative of the target image.</param>
    /// <param name="I1y">y derivative of the target image.</param>
    /// <param name="u1">Initial horizontal flow (from the coarser level).</param>
    /// <param name="u2">Initial vertical flow.</param>
    /// <param name="nx">Width of the level.</param>
    /// <param name="ny">Height of the level.</param>
    /// <param name="tau">Time step of the dual update.</param>
    /// <param name="lambda">Weight of the data term.</param>
    /// <param name="theta">Coupling parameter between flow and auxiliary variable.</param>
    /// <param name="epsilon">Stopping tolerance of the iterations.</param>
    /// <param name="max_its">Maximum number of iterations per warp.</param>
    /// <param name="grad_is_zero">Threshold below which the squared image gradient is treated as zero.</param>
    public void Begin(float[] I0, float[] I1, float[] I1x, float[] I1y, float[] u1, float[] u2,
        int nx, int ny, float tau, float lambda, float theta, float epsilon, int max_its, float grad_is_zero)
    {
        Release();
        size = nx * ny;
        groups = (size + GROUP - 1) / GROUP;
        max_iterations = max_its;

        b_i0 = Upload(I0);
        b_i1 = Upload(I1);
        b_i1x = Upload(I1x);
        b_i1y = Upload(I1y);
        b_u1 = Upload(u1);
        b_u2 = Upload(u2);
        b_i1wx = Zeros(size);
        b_i1wy = Zeros(size);
        b_grad = Zeros(size);
        b_rhoc = Zeros(size);
        // p wird je Stufe mit 0 initialisiert (wie in der CPU-Version)
        b_p11 = Zeros(size);
        b_p12 = Zeros(size);
        b_p21 = Zeros(size);
        b_p22 = Zeros(size);
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
        cs.SetFloat("taut", tau / theta);
        cs.SetFloat("eps_sq", epsilon * epsilon);
        cs.SetFloat("grad_is_zero", grad_is_zero);

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

        cs.SetBuffer(k_step_a, "State_r", b_state);
        cs.SetBuffer(k_step_a, "I1wx_r", b_i1wx);
        cs.SetBuffer(k_step_a, "I1wy_r", b_i1wy);
        cs.SetBuffer(k_step_a, "Grad_r", b_grad);
        cs.SetBuffer(k_step_a, "RhoC_r", b_rhoc);
        cs.SetBuffer(k_step_a, "P11_r", b_p11);
        cs.SetBuffer(k_step_a, "P12_r", b_p12);
        cs.SetBuffer(k_step_a, "P21_r", b_p21);
        cs.SetBuffer(k_step_a, "P22_r", b_p22);
        cs.SetBuffer(k_step_a, "U1", b_u1);
        cs.SetBuffer(k_step_a, "U2", b_u2);
        cs.SetBuffer(k_step_a, "Partial", b_partial);

        cs.SetBuffer(k_step_b, "State_r", b_state);
        cs.SetBuffer(k_step_b, "U1_r", b_u1);
        cs.SetBuffer(k_step_b, "U2_r", b_u2);
        cs.SetBuffer(k_step_b, "P11", b_p11);
        cs.SetBuffer(k_step_b, "P12", b_p12);
        cs.SetBuffer(k_step_b, "P21", b_p21);
        cs.SetBuffer(k_step_b, "P22", b_p22);

        cs.SetBuffer(k_reduce, "Partial_r", b_partial);
        cs.SetBuffer(k_reduce, "State", b_state);
        cs.SetBuffer(k_reduce, "ErrOut", b_err);
    }

    // Ein Warp: Warp-Kernel, dann bis zu max_iterations Iterationen. Rueckgabe: ausgefuehrte Iterationen.
    /// <summary>
    /// Performs one warp: warps the target image with the current flow and runs the primal-dual iterations until convergence or the iteration limit.
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
            cs.Dispatch(k_step_a, groups, 1, 1);
            cs.Dispatch(k_step_b, groups, 1, 1);
            cs.Dispatch(k_reduce, 1, 1, 1);
        }

        // einzige Synchronisation je Warp
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
        Rel(ref b_u1); Rel(ref b_u2);
        Rel(ref b_p11); Rel(ref b_p12); Rel(ref b_p21); Rel(ref b_p22);
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
