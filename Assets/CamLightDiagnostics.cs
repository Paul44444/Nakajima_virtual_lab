using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

//20092026 Diagnose fuer die HDRP-Assertion "std::abs(det) > FLT_MIN" im klassischen
//with_exp/Aufloesungs-/start-Workflow: prueft jeden Frame alle Kameras und Lichter
//auf degenerierte Transforms/Matrizen und loggt den Verursacher (einmal pro Objekt
//und Sekunde, damit das Editor.log nicht vollaeuft). Rein lesend, keine Seiteneffekte.
//Entfernen, sobald die Ursache gefunden ist.
public class CamLightDiagnostics : MonoBehaviour
{
    private readonly Dictionary<int, float> lastLogTime = new Dictionary<int, float>();
    private float lastSummaryTime = -999f;
    private string lastSummary = "";

    /// <summary>
    /// Creates the diagnostics object once at start-up (persistent across scene loads).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindAnyObjectByType<CamLightDiagnostics>() != null)
            return;
        GameObject obj = new GameObject("CamLightDiagnostics");
        DontDestroyOnLoad(obj);
        obj.AddComponent<CamLightDiagnostics>();
        Debug.Log("CamLightDiagnostics installed.");
    }

    /// <summary>
    /// Checks whether all components of a vector are finite.
    /// </summary>
    /// <param name="v">Vector to check.</param>
    /// <returns>True if no component is NaN or infinite.</returns>
    private static bool Finite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }

    /// <summary>
    /// Checks whether all components of a quaternion are finite.
    /// </summary>
    /// <param name="q">Quaternion to check.</param>
    /// <returns>True if no component is NaN or infinite.</returns>
    private static bool Finite(Quaternion q)
    {
        return float.IsFinite(q.x) && float.IsFinite(q.y) && float.IsFinite(q.z) && float.IsFinite(q.w);
    }

    /// <summary>
    /// Formats a float with six significant digits (invariant culture).
    /// </summary>
    /// <param name="v">Value to format.</param>
    /// <returns>Formatted string.</returns>
    private static string F(float v)
    {
        return v.ToString("G6", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a vector as (x, y, z).
    /// </summary>
    /// <param name="v">Vector to format.</param>
    /// <returns>Formatted string.</returns>
    private static string V(Vector3 v)
    {
        return "(" + F(v.x) + ", " + F(v.y) + ", " + F(v.z) + ")";
    }

    /// <summary>
    /// Builds the hierarchy path of a transform (parent/child/...).
    /// </summary>
    /// <param name="t">Transform whose path is built.</param>
    /// <returns>Slash-separated path from the root.</returns>
    private static string Path(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    /// <summary>
    /// Limits log output to one message per object and second.
    /// </summary>
    /// <param name="o">Object that would be logged.</param>
    /// <returns>True if a message for this object was logged less than one second ago.</returns>
    private bool Throttled(Object o)
    {
        int id = o.GetHashCode();
        float now = Time.unscaledTime;
        if (lastLogTime.TryGetValue(id, out float last) && now - last < 1f)
            return true;
        lastLogTime[id] = now;
        return false;
    }

    /// <summary>
    /// Unity callback: checks all cameras and lights for degenerate transforms or projection matrices and logs the offending objects (read-only).
    /// </summary>
    private void LateUpdate()
    {
        StringBuilder summary = new StringBuilder();

        foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            Transform t = cam.transform;
            List<string> problems = new List<string>();
            if (!Finite(t.position)) problems.Add("position not finite");
            if (!Finite(t.rotation)) problems.Add("rotation not finite");
            if (!Finite(t.lossyScale)) problems.Add("scale not finite");
            if (Mathf.Abs(t.lossyScale.x * t.lossyScale.y * t.lossyScale.z) < 1e-12f) problems.Add("scale ~0");
            if (!cam.orthographic && !(cam.fieldOfView > 0f && cam.fieldOfView < 180f)) problems.Add("fov=" + F(cam.fieldOfView));
            if (cam.orthographic && !(cam.orthographicSize > 0f)) problems.Add("orthoSize=" + F(cam.orthographicSize));
            if (!(cam.aspect > 0f) || !float.IsFinite(cam.aspect)) problems.Add("aspect=" + F(cam.aspect));
            if (!(cam.nearClipPlane > 0f) || !(cam.farClipPlane > cam.nearClipPlane)) problems.Add("clip=" + F(cam.nearClipPlane) + "/" + F(cam.farClipPlane));
            if (cam.pixelWidth <= 0 || cam.pixelHeight <= 0) problems.Add("pixelRect=" + cam.pixelWidth + "x" + cam.pixelHeight);
            float det = 0f;
            bool detOk = true;
            try
            {
                Matrix4x4 vp = cam.projectionMatrix * cam.worldToCameraMatrix;
                det = vp.determinant;
                detOk = float.IsFinite(det) && Mathf.Abs(det) > float.Epsilon;
            }
            catch { detOk = false; }
            if (!detOk) problems.Add("det(VP)=" + F(det));

            string line = Path(t) + (cam.enabled && cam.gameObject.activeInHierarchy ? " [on]" : " [off]")
                + " pos=" + V(t.position) + " euler=" + V(t.eulerAngles) + " scale=" + V(t.lossyScale)
                + " fov=" + F(cam.fieldOfView) + " aspect=" + F(cam.aspect)
                + " clip=" + F(cam.nearClipPlane) + "/" + F(cam.farClipPlane)
                + " px=" + cam.pixelWidth + "x" + cam.pixelHeight
                + " rt=" + (cam.targetTexture != null ? cam.targetTexture.width + "x" + cam.targetTexture.height : "none")
                + " det=" + F(det);
            summary.AppendLine("  CAM " + line);
            if (problems.Count > 0 && !Throttled(cam))
                Debug.LogError("CamLightDiagnostics: degenerate CAMERA " + Path(t) + ": "
                    + string.Join(", ", problems) + " | " + line);
        }

        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include))
        {
            Transform t = light.transform;
            List<string> problems = new List<string>();
            if (!Finite(t.position)) problems.Add("position not finite");
            if (!Finite(t.rotation)) problems.Add("rotation not finite");
            if (!Finite(t.lossyScale)) problems.Add("scale not finite");
            if (Mathf.Abs(t.lossyScale.x * t.lossyScale.y * t.lossyScale.z) < 1e-12f) problems.Add("scale ~0");
            if (!float.IsFinite(light.intensity)) problems.Add("intensity=" + F(light.intensity));
            if (!float.IsFinite(light.range)) problems.Add("range=" + F(light.range));
            float det = t.localToWorldMatrix.determinant;
            if (!float.IsFinite(det) || Mathf.Abs(det) < 1e-12f) problems.Add("det(L2W)=" + F(det));

            string line = Path(t) + (light.enabled && light.gameObject.activeInHierarchy ? " [on]" : " [off]")
                + " type=" + light.type + " shadows=" + light.shadows
                + " pos=" + V(t.position) + " euler=" + V(t.eulerAngles) + " scale=" + V(t.lossyScale)
                + " intensity=" + F(light.intensity) + " det=" + F(det);
            summary.AppendLine("  LIGHT " + line);
            if (problems.Count > 0 && !Throttled(light))
                Debug.LogError("CamLightDiagnostics: degenerate LIGHT " + Path(t) + ": "
                    + string.Join(", ", problems) + " | " + line);
        }

        // Zustands-Snapshot nur loggen, wenn er sich geaendert hat (und hoechstens alle 2 s).
        string s = summary.ToString();
        if (s != lastSummary && Time.unscaledTime - lastSummaryTime > 2f)
        {
            lastSummary = s;
            lastSummaryTime = Time.unscaledTime;
            Debug.Log("CamLightDiagnostics snapshot (frame " + Time.frameCount + "):\n" + s);
        }
    }

}
