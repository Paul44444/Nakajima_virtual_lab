using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//23092026 Nutzerwunsch: Renderaufloesung frei per Schieberegler (64..2048 px, 32er-Schritte).
//  Beim Ziehen wird nur die Anzeige aktualisiert; angewendet (set_render_res + refresh_cams) wird
//  beim Loslassen. Aenderungen ueber die alten Toggles (res_panel) werden uebernommen und die Toggles
//  umgekehrt passend gesetzt. Waehrend einer laufenden Analyse gesperrt.
public class ResolutionSlider : MonoBehaviour, IPointerUpHandler
{
    public const int STEP = 32;
    public const int MIN_RES = 64;
    public const int MAX_RES = 2048;

    Slider slider;
    Text label;
    vis_3D vis;
    int shown_res = -1;
    bool dragging = false;

    /// <summary>
    /// Creates the render-resolution slider (64 to 2048 px in steps of 32) with its label under a parent transform.
    /// </summary>
    /// <param name="canvas">Parent transform of the slider panel.</param>
    /// <param name="font">Font of the label.</param>
    /// <param name="position">Anchored position (anchor top right of the parent).</param>
    /// <returns>The slider component.</returns>
    public static ResolutionSlider Create(Transform canvas, Font font, Vector2 position)
    {
        GameObject root = new GameObject("resolution_slider_panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(canvas, false);
        root.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.9f);
        RectTransform rr = root.GetComponent<RectTransform>();
        rr.anchorMin = rr.anchorMax = rr.pivot = new Vector2(1f, 1f);
        rr.anchoredPosition = position;
        rr.sizeDelta = new Vector2(245f, 38f);

        GameObject textObj = new GameObject("resolution_label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObj.transform.SetParent(root.transform, false);
        Text text = textObj.GetComponent<Text>();
        text.font = font;
        text.fontSize = 15;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        RectTransform tr = text.rectTransform;
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(0f, 1f);
        tr.pivot = new Vector2(0f, 0.5f);
        tr.anchoredPosition = new Vector2(8f, 0f);
        tr.sizeDelta = new Vector2(70f, 0f);

        GameObject sliderObj = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderObj.name = "resolution_slider";
        sliderObj.transform.SetParent(root.transform, false);
        RectTransform sr = sliderObj.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0f, 0.5f);
        sr.anchorMax = new Vector2(1f, 0.5f);
        sr.pivot = new Vector2(0.5f, 0.5f);
        sr.offsetMin = new Vector2(82f, -9f);
        sr.offsetMax = new Vector2(-12f, 9f);
        // Farben (DefaultControls ohne Sprites: einfache Flaechen)
        foreach (Image img in sliderObj.GetComponentsInChildren<Image>())
        {
            if (img.gameObject.name == "Background") img.color = new Color(0.35f, 0.35f, 0.38f, 1f);
            if (img.gameObject.name == "Fill") img.color = new Color(0.35f, 0.8f, 1f, 1f);
            if (img.gameObject.name == "Handle") img.color = Color.white;
        }

        Slider s = sliderObj.GetComponent<Slider>();
        s.wholeNumbers = true;
        s.minValue = MIN_RES / STEP;
        s.maxValue = MAX_RES / STEP;

        ResolutionSlider rs = sliderObj.AddComponent<ResolutionSlider>();
        rs.slider = s;
        rs.label = text;
        s.onValueChanged.AddListener(v => rs.OnDrag((int)v * STEP));
        return rs;
    }

    /// <summary>
    /// Returns the lab controller (cached after the first lookup).
    /// </summary>
    /// <returns>The vis_3D component or null.</returns>
    vis_3D Vis()
    {
        if (vis == null)
        {
            GameObject sphere = GameObject.Find("sphere");
            vis = sphere != null ? sphere.GetComponent<vis_3D>() : null;
        }
        return vis;
    }

    /// <summary>
    /// Updates the label while the slider is dragged; the resolution is applied on release.
    /// </summary>
    /// <param name="res">Resolution under the handle in pixels.</param>
    void OnDrag(int res)
    {
        dragging = true;
        label.text = "r" + res;
    }

    /// <summary>
    /// Applies the dragged resolution when the mouse button is released.
    /// </summary>
    /// <param name="eventData">Pointer event data (unused).</param>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!dragging)
            return;
        dragging = false;
        Apply((int)slider.value * STEP);
    }

    /// <summary>
    /// Sets the render resolution of the lab and refreshes the cameras; refused while an analysis is running.
    /// </summary>
    /// <param name="res">New resolution in pixels.</param>
    void Apply(int res)
    {
        vis_3D v = Vis();
        if (v == null)
            return;
        if (v.get_is_started())
        {
            ExperimentImageGallery.SetResultsText("Aufloesung kann waehrend einer laufenden Analyse nicht geaendert werden.");
            ExperimentImageGallery.ShowResultsWindow();
            ShowRes(v.get_render_res());
            return;
        }
        if (res == v.get_render_res())
            return;
        v.set_render_res(res);
        v.refresh_cams();
        SyncToggles(res);
        ShowRes(res);
        ExperimentImageGallery.SetResultsText("Renderaufloesung: r" + res + " (" + res + " x " + res + " px)."
            + "\nGilt ab der naechsten Analyse (with_exp + with_tv -> Start). Karten/Genauigkeit lesen"
            + " die Ergebnisse dieser Aufloesung."
            + (res > 1024 ? "\nHinweis: Masken/Ground Truth laufen auf der CPU und brauchen bei grossen Bildern deutlich laenger." : ""));
    }

    /// <summary>
    /// Shows a resolution on the slider and label without triggering its listener.
    /// </summary>
    /// <param name="res">Resolution in pixels.</param>
    void ShowRes(int res)
    {
        shown_res = res;
        label.text = "r" + res;
        int idx = Mathf.Clamp(Mathf.RoundToInt((float)res / STEP), (int)slider.minValue, (int)slider.maxValue);
        slider.SetValueWithoutNotify(idx);
    }

    // alte Toggles im res_panel an die Aufloesung angleichen (ohne deren Listener auszuloesen)
    /// <summary>
    /// Sets the legacy resolution toggles of the scene to match the resolution (without triggering their listeners).
    /// </summary>
    /// <param name="res">Resolution in pixels.</param>
    static void SyncToggles(int res)
    {
        GameObject canvas = GameObject.Find("Canvas");
        Transform panel = canvas != null ? canvas.transform.Find("res_panel") : null;
        if (panel == null)
            return;
        foreach (int r in new[] { 128, 256, 512, 1600 })
        {
            Transform t = panel.Find("toggle_" + r);
            Toggle toggle = t != null ? t.GetComponent<Toggle>() : null;
            if (toggle != null)
                toggle.SetIsOnWithoutNotify(res == r);
        }
    }

    /// <summary>
    /// Unity callback: follows resolution changes made elsewhere (legacy toggles, saved settings).
    /// </summary>
    void Update()
    {
        // Aenderungen von aussen (Toggles, PlayerPrefs beim Start) uebernehmen
        vis_3D v = Vis();
        if (v != null && !dragging && v.get_render_res() != shown_res)
            ShowRes(v.get_render_res());
    }
}
