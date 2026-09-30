using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentImageGallery : MonoBehaviour
{
    private static ExperimentImageGallery instance;
    private readonly List<Texture2D> textures = new List<Texture2D>();
    private readonly List<Sprite> sprites = new List<Sprite>();
    private readonly List<string> paths = new List<string>();

    private GameObject window;
    private Button openButton;
    private Image image;
    private Text title;
    private Text lightingLabel;
    private Text counter;
    private Text resultsText;
    private int currentIndex;

    /// <summary>
    /// Creates the gallery (singleton) with its complete user interface under the canvas, if it does not exist yet.
    /// </summary>
    /// <param name="canvas">Canvas that receives the gallery.</param>
    /// <returns>The gallery instance.</returns>
    public static ExperimentImageGallery EnsureCreated(Transform canvas)
    {
        if (instance != null)
            return instance;

        GameObject galleryObject = new GameObject("experiment_image_gallery");
        galleryObject.transform.SetParent(canvas, false);
        instance = galleryObject.AddComponent<ExperimentImageGallery>();
        instance.BuildUi(canvas);
        return instance;
    }

    /// <summary>
    /// Removes all images of the gallery (e.g. before a new analysis).
    /// </summary>
    public static void ClearCurrentExperiment()
    {
        if (instance != null)
            instance.ClearImages();
    }

    /// <summary>
    /// Appends an image file to the gallery.
    /// </summary>
    /// <param name="path">Path of the image file.</param>
    public static void AddRenderedImage(string path)
    {
        if (instance != null && !string.IsNullOrEmpty(path) && File.Exists(path))
            instance.LoadImage(path);
    }

    //28092026 Bild hinzufuegen oder, falls derselbe Pfad schon in der Galerie ist, dort ersetzen
    //  (z.B. belichtete TV-Eingangsbilder, die bei jedem Lauf neu geschrieben werden)
    /// <summary>
    /// Adds an image or, if the same path is already in the gallery, replaces it in place (for files rewritten by every run).
    /// </summary>
    /// <param name="path">Path of the image file.</param>
    public static void AddOrReplaceImage(string path)
    {
        if (instance == null || string.IsNullOrEmpty(path) || !File.Exists(path))
            return;
        int idx = instance.paths.IndexOf(path);
        if (idx >= 0)
        {
            Destroy(instance.sprites[idx]);
            Destroy(instance.textures[idx]);
            instance.sprites.RemoveAt(idx);
            instance.textures.RemoveAt(idx);
            instance.paths.RemoveAt(idx);
            instance.LoadImage(path, idx);
        }
        else
        {
            instance.LoadImage(path);
        }
    }

    //28092026 Bild an Position index setzen; ist es schon in der Galerie, wird es dorthin verschoben (keine Doppel)
    /// <summary>
    /// Places an image at a given position; an image that is already in the gallery is moved there (no duplicates).
    /// </summary>
    /// <param name="path">Path of the image file.</param>
    /// <param name="index">Target position (0 = first).</param>
    public static void InsertOrMoveImage(string path, int index)
    {
        if (instance == null || string.IsNullOrEmpty(path) || !File.Exists(path))
            return;
        int old = instance.paths.IndexOf(path);
        if (old >= 0)
        {
            Destroy(instance.sprites[old]);
            Destroy(instance.textures[old]);
            instance.sprites.RemoveAt(old);
            instance.textures.RemoveAt(old);
            instance.paths.RemoveAt(old);
        }
        instance.LoadImage(path, Mathf.Min(index, instance.sprites.Count));
    }

    //28092026 erstes Bild der Galerie anzeigen
    /// <summary>
    /// Shows the first image of the gallery.
    /// </summary>
    public static void ShowFirst()
    {
        if (instance == null || instance.sprites.Count == 0)
            return;
        instance.currentIndex = 0;
        instance.UpdateDisplay();
    }

    //23092026 Bild an Position index einfuegen (z.B. Genauigkeits-Panels vor den Einzelkarten)
    //  und direkt anzeigen.
    /// <summary>
    /// Inserts an image at a given position and shows it.
    /// </summary>
    /// <param name="path">Path of the image file.</param>
    /// <param name="index">Insert position.</param>
    public static void InsertRenderedImage(string path, int index)
    {
        if (instance != null && !string.IsNullOrEmpty(path) && File.Exists(path))
            instance.LoadImage(path, index);
    }

    //27092026 Beschriftungen, die von gespeicherten Einstellungen abhaengen, nach dem Laden der PlayerPrefs
    //  aktualisieren (die Galerie wird in vis_3D.Start vor load_render_res_pref gebaut)
    private Button speckleModeButton;
    private InputField analysisExposureField; //28092026
    /// <summary>
    /// Updates labels that depend on saved settings (speckle mode, exposure factor) after the settings have been loaded.
    /// </summary>
    /// <param name="vis">Lab controller that holds the settings.</param>
    public static void RefreshStateLabels(vis_3D vis)
    {
        if (instance == null || vis == null)
            return;
        if (instance.speckleModeButton != null)
            instance.speckleModeButton.GetComponentInChildren<Text>().text = "Speckle: " + vis.describe_speckle_texture();
        if (instance.analysisExposureField != null) //28092026
            instance.analysisExposureField.text = vis.get_analysis_exposure().ToString("G4", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Opens the gallery window if it contains images.
    /// </summary>
    public static void ShowWhenFinished()
    {
        if (instance != null && instance.sprites.Count > 0)
            instance.Show();
    }

    /// <summary>
    /// Opens the gallery window even without images (to show the result report).
    /// </summary>
    public static void ShowResultsWindow()
    {
        if (instance != null)
            instance.ShowEvenWithoutImages();
    }

    /// <summary>
    /// Builds the complete interface: gallery button, control panel with the tabs Analyse, Sweeps, Stereo, and Realbild, TV-parameter panel, gallery window, tooltip, and the hover explanations of all controls.
    /// </summary>
    /// <param name="canvas">Canvas that receives the interface.</param>
    private void BuildUi(Transform canvas)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        openButton = CreateButton("gallery_open_button", canvas, "Bilder (0)", font);
        RectTransform openRect = openButton.GetComponent<RectTransform>();
        openRect.anchorMin = new Vector2(1f, 1f);
        openRect.anchorMax = new Vector2(1f, 1f);
        openRect.pivot = new Vector2(1f, 1f);
        openRect.anchoredPosition = new Vector2(-10f, -135f);
        openRect.sizeDelta = new Vector2(170f, 38f);
        openButton.onClick.AddListener(Show);
        openButton.gameObject.SetActive(false);

        //30092026 Nutzerwunsch: GUI aufgeraeumt. Alle Bedienelemente der Galerie liegen in EINEM schmalen Panel am
        //  rechten Rand (unter "Bilder (N)") mit Reitern Analyse / Sweeps / Stereo / Realbild, statt einzeln ueber den
        //  Bildschirm verteilt (ueberlappten die Path-Panels und die Bildvorschau der Szene). Klick auf den aktiven
        //  Reiter klappt das Panel ein. Funktionen und Listener unveraendert, nur Eltern-Objekt und Position neu.
        //  Raster (Anker oben rechts im Reiter): zwei Spalten x = -10 / -137 (Buttons 118 breit, ganze Zeile 245),
        //  Zeilen von oben nach unten ueber den Zeiger y (NextRow / Header).
        BuildControlPanel(canvas, font);
        const float C0 = -10f, C1 = -137f;
        Vector2 TR = new Vector2(1f, 1f);
        Vector2 B = new Vector2(118f, 38f), B2 = new Vector2(245f, 38f);
        float y;

        // ---------------- Reiter "Analyse" ----------------
        Transform tabAnalyse = CreateTab("Analyse", font);
        y = -10f;

        //23092026 Nutzerwunsch: Genauigkeitsanalyse (value, value_ref, loss_abs, loss_rel fuer u und v + Statistik)
        Button accuracyButton = CreateButton("accuracy_button", tabAnalyse, "Genauigkeit", font);
        SetRect(accuracyButton.GetComponent<RectTransform>(), new Vector2(C0, NextRow(ref y)), B2, TR);
        accuracyButton.GetComponentInChildren<Text>().fontSize = 16;
        accuracyButton.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis != null)
                vis.save_accuracy_analysis();
        });
        Button loadSavedButton = CreateButton("load_saved_images_button", tabAnalyse, "Bilder: Laden", font);
        SetRect(loadSavedButton.GetComponent<RectTransform>(), new Vector2(C0, NextRow(ref y)), B2, TR);
        loadSavedButton.GetComponentInChildren<Text>().fontSize = 16;
        loadSavedButton.onClick.AddListener(LoadSavedImages);

        //24092026 Nutzerwunsch: Frames (Zeitschritte der Probe) im Programm waehlen, z.B. "28, 29" oder "27-29"
        //30092026 Zeile: [Beschriftung 70][Feld 120][OK 45]
        float yFrames = NextRow(ref y);
        Text framesLabel = CreateText("frames_label", tabAnalyse, "Frames:", font, 15, TextAnchor.MiddleRight);
        SetRect(framesLabel.rectTransform, new Vector2(-185f, yFrames), new Vector2(70f, 38f), TR);
        InputField framesField = CreateInputField("frames_field", tabAnalyse, "", font);
        SetRect(framesField.GetComponent<RectTransform>(), new Vector2(-60f, yFrames), new Vector2(120f, 38f), TR);
        {
            vis_3D vis0 = Vis();
            if (vis0 != null)
                framesField.text = string.Join(", ", vis0.get_frames());
        }
        Button framesApply = CreateButton("frames_apply", tabAnalyse, "OK", font);
        SetRect(framesApply.GetComponent<RectTransform>(), new Vector2(C0, yFrames), new Vector2(45f, 38f), TR);
        framesApply.GetComponentInChildren<Text>().fontSize = 15;
        framesApply.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis == null)
                return;
            SetResultsText(vis.set_frames_from_text(framesField.text));
            framesField.text = string.Join(", ", vis.get_frames());
            ShowResultsWindow();
        });

        //28092026 Nutzerwunsch: Belichtung k fuer die normale Analyse (Start -> Genauigkeit), 1 = Original
        float yK = NextRow(ref y);
        Text analysisKLabel = CreateText("analysis_exposure_label", tabAnalyse, "Analyse-k:", font, 15, TextAnchor.MiddleRight);
        SetRect(analysisKLabel.rectTransform, new Vector2(-185f, yK), new Vector2(80f, 38f), TR);
        InputField analysisK = CreateInputField("analysis_exposure_field", tabAnalyse, "1", font);
        SetRect(analysisK.GetComponent<RectTransform>(), new Vector2(-60f, yK), new Vector2(120f, 38f), TR);
        {
            vis_3D vis0 = Vis();
            if (vis0 != null)
                analysisK.text = vis0.get_analysis_exposure().ToString("G4", CultureInfo.InvariantCulture);
        }
        analysisExposureField = analysisK;
        Button analysisKApply = CreateButton("analysis_exposure_apply", tabAnalyse, "OK", font);
        SetRect(analysisKApply.GetComponent<RectTransform>(), new Vector2(C0, yK), new Vector2(45f, 38f), TR);
        analysisKApply.GetComponentInChildren<Text>().fontSize = 15;
        analysisKApply.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis == null)
                return;
            double k = TvParse(analysisK);
            string entered = analysisK.text;
            bool valid = vis.set_analysis_exposure(double.IsNaN(k) ? float.NaN : (float)k);
            analysisK.text = vis.get_analysis_exposure().ToString("G4", CultureInfo.InvariantCulture);
            SetResultsText((valid ? "" : "Eingabe \"" + entered + "\" ungueltig (erlaubt: Zahl > 0, hoechstens "
                    + vis_3D.EXPOSURE_MAX.ToString(CultureInfo.InvariantCulture) + ") - Wert unveraendert.\n")
                + "Belichtung fuer die Analyse: k = " + analysisK.text
                + (vis.get_analysis_exposure() == 1f ? " (Original)" : "")
                + "\nGilt ab der naechsten Analyse (with_exp + with_tv -> Start), danach \"Genauigkeit\"."
                + "\nDie belichteten TV-Eingangsbilder liegen in Assets/analysis_results/exposure_images/ (*_analyse_k*.png).");
            ShowResultsWindow();
        });

        //23092026 Nutzerwunsch: Renderaufloesung frei per Schieberegler (64..2048 px); 245 breit wie eine ganze Zeile
        ResolutionSlider.Create(tabAnalyse, font, new Vector2(C0, NextRow(ref y)));

        //27092026 Speckle-Muster fuer den Nakajima-Look: gemessen (Standard) / zufaellig / zufaellig kontrastreich
        speckleModeButton = CreateButton("speckle_mode_button", tabAnalyse, "Speckle: ...", font);
        SetRect(speckleModeButton.GetComponent<RectTransform>(), new Vector2(C0, NextRow(ref y)), B2, TR);
        speckleModeButton.GetComponentInChildren<Text>().fontSize = 14;
        {
            vis_3D vis0 = Vis();
            if (vis0 != null)
                speckleModeButton.GetComponentInChildren<Text>().text = "Speckle: " + vis0.describe_speckle_texture();
        }
        speckleModeButton.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis == null)
                return;
            vis.set_speckle_texture_mode(vis.get_speckle_texture_mode() + 1);
            speckleModeButton.GetComponentInChildren<Text>().text = "Speckle: " + vis.describe_speckle_texture();
            SetResultsText("Speckle-Muster: " + vis.describe_speckle_texture()
                + "\nGilt ab der naechsten Analyse (with_exp + with_tv -> Start), nur im Nakajima-Look.");
            ShowResultsWindow();
        });

        //21092026 Umschalter "Render-Look": klassisch vs. Nakajima (Szene wie "Realbild: Neu")
        Button lookButton = CreateButton("nakajima_look_button", tabAnalyse, "Look: klassisch", font);
        SetRect(lookButton.GetComponent<RectTransform>(), new Vector2(C0, NextRow(ref y)), B2, TR);
        lookButton.GetComponentInChildren<Text>().fontSize = 12;
        {
            GameObject sphere0 = GameObject.Find("sphere");
            vis_3D vis0 = sphere0 != null ? sphere0.GetComponent<vis_3D>() : null;
            if (vis0 != null)
                lookButton.GetComponentInChildren<Text>().text =
                    LookLabel(vis0.get_nakajima_look());
        }
        lookButton.onClick.AddListener(() =>
        {
            GameObject sphere = GameObject.Find("sphere");
            if (sphere == null)
                return;
            vis_3D vis = sphere.GetComponent<vis_3D>();
            vis.set_nakajima_look(!vis.get_nakajima_look());
            lookButton.GetComponentInChildren<Text>().text =
                LookLabel(vis.get_nakajima_look());
            SetResultsText(vis.get_nakajima_look()
                ? "Render-Look: Nakajima (Szene wie 'Realbild: Neu'). Gilt ab der naechsten Analyse (with_exp + with_tv -> Start)."
                : "Render-Look: klassisch. Gilt ab der naechsten Analyse (with_exp + with_tv -> Start).");
        });

        //22092026 Nutzerwunsch: Panel zum Einstellen der TV-L1-Parameter (Regularisierung).
        //30092026 Schalter im Reiter "Analyse", das Panel selbst oeffnet sich links neben dem Bedienpanel
        BuildTvParameterPanel(canvas, tabAnalyse, new Vector2(C0, NextRow(ref y)), B2, font);

        //27092026 Nutzerwunsch: Parameterstudie (TV/TGV-Parameter) starten bzw. letzte Studie anzeigen
        float yStudy = NextRow(ref y);
        Button studyButton = CreateButton("param_study_button", tabAnalyse, "Parameter: Neu", font);
        SetRect(studyButton.GetComponent<RectTransform>(), new Vector2(C1, yStudy), B, TR);
        studyButton.GetComponentInChildren<Text>().fontSize = 13;
        studyButton.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis != null)
                vis.run_param_study(); // zweiter Klick waehrend der Studie = Abbruch nach dem laufenden Durchgang
        });
        Button studyLoadButton = CreateButton("param_study_load_button", tabAnalyse, "Parameter: Laden", font);
        SetRect(studyLoadButton.GetComponent<RectTransform>(), new Vector2(C0, yStudy), B, TR);
        studyLoadButton.GetComponentInChildren<Text>().fontSize = 13;
        studyLoadButton.onClick.AddListener(async () =>
        {
            vis_3D vis = Vis();
            if (vis != null)
                await vis.show_param_study();
        });
        FinishTab(y);

        // ---------------- Reiter "Sweeps" ----------------
        // je Studie: Ueberschrift, Werteliste (ganze Zeile), darunter [Laden] [Neu]
        Transform tabSweeps = CreateTab("Sweeps", font);
        y = -10f;

        //28092026 Nutzerwunsch: Lichtstaerken als Liste (Vorgabe: Bereich aus Abb. 5a im Paper, 0.01 ... 100)
        //29092026 Nutzerwunsch: Bereich um einige Groessenordnungen erweitert (1e-4 ... 1e4, dichter um die
        //  Uebergaenge aus dem Paper). Die alte gespeicherte Vorgabe wird durch die neue ersetzt.
        const string lightingDefault = "0.0001, 0.0003, 0.001, 0.003, 0.01, 0.02, 0.035, 0.036, 0.04, 0.1, 0.2, 0.5, 1, 2, 5, 6, 10, 30, 100, 300, 1000, 10000";
        string lightingSaved = PlayerPrefs.GetString("lighting_levels", lightingDefault);
        if (lightingSaved == "0.01, 0.02, 0.035, 0.036, 0.04, 0.1, 0.2, 0.5, 1, 2, 5, 6, 10, 100")
            lightingSaved = lightingDefault;
        Header(tabSweeps, "Licht (Faktor I der Lampen)", ref y, font);
        InputField lightingLevels = CreateInputField("lighting_levels", tabSweeps, lightingSaved, font);
        SetRect(lightingLevels.GetComponent<RectTransform>(), new Vector2(C0, NextRow(ref y)), B2, TR);
        float yLight = NextRow(ref y);
        Button analysisButton = CreateButton("lighting_analysis_button", tabSweeps, "Neu", font);
        SetRect(analysisButton.GetComponent<RectTransform>(), new Vector2(C0, yLight), B, TR);
        analysisButton.GetComponentInChildren<Text>().fontSize = 16;
        analysisButton.onClick.AddListener(() =>
        {
            GameObject sphere = GameObject.Find("sphere");
            if (sphere != null)
            {
                PlayerPrefs.SetString("lighting_levels", lightingLevels.text);
                PlayerPrefs.Save();
                sphere.GetComponent<vis_3D>().start_lighting_sweep(lightingLevels.text);
            }
        });
        Button loadLightingButton = CreateButton("load_lighting_analysis_button", tabSweeps, "Laden", font);
        SetRect(loadLightingButton.GetComponent<RectTransform>(), new Vector2(C1, yLight), B, TR);
        loadLightingButton.GetComponentInChildren<Text>().fontSize = 16;
        loadLightingButton.onClick.AddListener(() => LoadPreviousAnalysis("lighting"));

        //29092026 Nutzerwunsch: mehr Stufen, v.a. staerkeres Rauschen (N = 1 -> sigma = 100 % der Vollaussteuerung);
        //  Liste wird gemerkt, bereits berechnete Stufen werden weiterverwendet
        Header(tabSweeps, "Rauschen (Nmax [e⁻], ∞ = ohne)", ref y, font);
        InputField noiseLevels = CreateInputField("noise_levels", tabSweeps,
            PlayerPrefs.GetString("noise_levels", "∞, 10000, 1000, 300, 100, 30, 10, 5, 3, 2, 1, 0.5"), font);
        SetRect(noiseLevels.GetComponent<RectTransform>(), new Vector2(C0, NextRow(ref y)), B2, TR);
        float yNoise = NextRow(ref y);
        Button noiseButton = CreateButton("noise_analysis_button", tabSweeps, "Neu", font);
        SetRect(noiseButton.GetComponent<RectTransform>(), new Vector2(C0, yNoise), B, TR);
        noiseButton.GetComponentInChildren<Text>().fontSize = 16;
        noiseButton.onClick.AddListener(() =>
        {
            GameObject sphere = GameObject.Find("sphere");
            if (sphere != null)
            {
                PlayerPrefs.SetString("noise_levels", noiseLevels.text);
                PlayerPrefs.Save();
                sphere.GetComponent<vis_3D>().start_noise_sweep(noiseLevels.text);
            }
        });
        Button loadNoiseButton = CreateButton("load_noise_analysis_button", tabSweeps, "Laden", font);
        SetRect(loadNoiseButton.GetComponent<RectTransform>(), new Vector2(C1, yNoise), B, TR);
        loadNoiseButton.GetComponentInChildren<Text>().fontSize = 16;
        loadNoiseButton.onClick.AddListener(() => LoadPreviousAnalysis("noise"));

        //29092026 alle vorhandenen synthetischen Materialien (Resources/Targets/fbx_files/Materials/speckle_*), Liste gemerkt
        Header(tabSweeps, "Speckle (Größen; p<s> = prozedural)", ref y, font);
        InputField speckleLevels = CreateInputField("speckle_levels", tabSweeps,
            PlayerPrefs.GetString("speckle_levels", "0.035; 0.070; 0.175; 0.280; 0.350; 0.500; 0.700; 1.400; 2.100; 2.800; 7.000"), font);
        SetRect(speckleLevels.GetComponent<RectTransform>(), new Vector2(C0, NextRow(ref y)), B2, TR);
        float ySpeckle = NextRow(ref y);
        Button speckleButton = CreateButton("speckle_analysis_button", tabSweeps, "Neu", font);
        SetRect(speckleButton.GetComponent<RectTransform>(), new Vector2(C0, ySpeckle), B, TR);
        speckleButton.GetComponentInChildren<Text>().fontSize = 16;
        speckleButton.onClick.AddListener(() =>
        {
            GameObject sphere = GameObject.Find("sphere");
            if (sphere != null)
            {
                PlayerPrefs.SetString("speckle_levels", speckleLevels.text);
                PlayerPrefs.Save();
                sphere.GetComponent<vis_3D>().start_speckle_sweep(speckleLevels.text);
            }
        });
        Button loadSpeckleButton = CreateButton("load_speckle_analysis_button", tabSweeps, "Laden", font);
        SetRect(loadSpeckleButton.GetComponent<RectTransform>(), new Vector2(C1, ySpeckle), B, TR);
        loadSpeckleButton.GetComponentInChildren<Text>().fontSize = 16;
        loadSpeckleButton.onClick.AddListener(() => LoadPreviousAnalysis("speckle"));

        //28092026 Nutzerwunsch: Belichtungsstudie (Kamerabilder mit Faktor k belichten, 8-Bit-Saettigung,
        //  optional Schrotrauschen "N=...")
        Header(tabSweeps, "Belichtung (Faktor k, vorhandene Bilder)", ref y, font);
        InputField exposureLevels = CreateInputField("exposure_levels", tabSweeps,
            "0.1, 0.25, 0.5, 1, 1.5, 2, 4", font);
        SetRect(exposureLevels.GetComponent<RectTransform>(), new Vector2(C0, NextRow(ref y)), B2, TR);
        float yExposure = NextRow(ref y);
        Button exposureButton = CreateButton("exposure_study_button", tabSweeps, "Neu", font);
        SetRect(exposureButton.GetComponent<RectTransform>(), new Vector2(C0, yExposure), B, TR);
        exposureButton.GetComponentInChildren<Text>().fontSize = 16;
        exposureButton.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis != null)
                vis.run_exposure_study(exposureLevels.text); // zweiter Klick = Abbruch nach der laufenden Stufe
        });
        Button exposureLoadButton = CreateButton("exposure_study_load_button", tabSweeps, "Laden", font);
        SetRect(exposureLoadButton.GetComponent<RectTransform>(), new Vector2(C1, yExposure), B, TR);
        exposureLoadButton.GetComponentInChildren<Text>().fontSize = 16;
        exposureLoadButton.onClick.AddListener(async () =>
        {
            vis_3D vis = Vis();
            if (vis != null)
                await vis.show_exposure_study();
        });
        FinishTab(y);

        // ---------------- Reiter "Stereo" ----------------
        Transform tabStereo = CreateTab("Stereo", font);
        y = -10f;
        //29092026 Nutzerwunsch: Hoehenanalyse (Stereo cam_0/cam_1, Abb. 7): "Neu" = letzte normale Analyse,
        //  "Licht"/"Speckle" = alle Stufen der jeweiligen Sweep-Tabelle
        Header(tabStereo, "Höhe (Stereo-Tiefe)", ref y, font);
        float yH1 = NextRow(ref y), yH2 = NextRow(ref y);
        Button heightButton = CreateButton("height_analysis_button", tabStereo, "Neu", font);
        SetRect(heightButton.GetComponent<RectTransform>(), new Vector2(C1, yH1), B, TR);
        heightButton.GetComponentInChildren<Text>().fontSize = 16;
        heightButton.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis != null) vis.run_stereo("normal");
        });
        Button heightLightingButton = CreateButton("height_lighting_button", tabStereo, "Licht", font);
        SetRect(heightLightingButton.GetComponent<RectTransform>(), new Vector2(C0, yH1), B, TR);
        heightLightingButton.GetComponentInChildren<Text>().fontSize = 16;
        heightLightingButton.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis != null) vis.run_stereo("lighting");
        });
        Button heightSpeckleButton = CreateButton("height_speckle_button", tabStereo, "Speckle", font);
        SetRect(heightSpeckleButton.GetComponent<RectTransform>(), new Vector2(C1, yH2), B, TR);
        heightSpeckleButton.GetComponentInChildren<Text>().fontSize = 16;
        heightSpeckleButton.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis != null) vis.run_stereo("speckle");
        });
        //30092026 Nutzerwunsch: 3D-Verschiebungsfeld (Scene Flow) aus beiden Kameras, zusaetzlich zur Hoehenanalyse
        Header(tabStereo, "3D-Fluss (beide Kameras)", ref y, font);
        float yF1 = NextRow(ref y), yF2 = NextRow(ref y);
        Button sceneFlowButton = CreateButton("scene_flow_button", tabStereo, "Neu", font);
        SetRect(sceneFlowButton.GetComponent<RectTransform>(), new Vector2(C1, yF1), B, TR);
        sceneFlowButton.GetComponentInChildren<Text>().fontSize = 16;
        sceneFlowButton.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis != null) vis.run_scene_flow("normal");
        });
        Button sceneFlowLightingButton = CreateButton("scene_flow_lighting_button", tabStereo, "Licht", font);
        SetRect(sceneFlowLightingButton.GetComponent<RectTransform>(), new Vector2(C0, yF1), B, TR);
        sceneFlowLightingButton.GetComponentInChildren<Text>().fontSize = 16;
        sceneFlowLightingButton.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis != null) vis.run_scene_flow("lighting");
        });
        Button sceneFlowSpeckleButton = CreateButton("scene_flow_speckle_button", tabStereo, "Speckle", font);
        SetRect(sceneFlowSpeckleButton.GetComponent<RectTransform>(), new Vector2(C1, yF2), B, TR);
        sceneFlowSpeckleButton.GetComponentInChildren<Text>().fontSize = 16;
        sceneFlowSpeckleButton.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis != null) vis.run_scene_flow("speckle");
        });
        FinishTab(y);

        // ---------------- Reiter "Realbild" ----------------
        Transform tabReal = CreateTab("Realbild", font);
        y = -10f;
        Header(tabReal, "Realbild (Dateiname)", ref y, font);
        InputField realImageName = CreateInputField("nakajima_real_image", tabReal, "image-000000.png", font);
        SetRect(realImageName.GetComponent<RectTransform>(), new Vector2(C0, NextRow(ref y)), B2, TR);

        //18092026 Nutzerwunsch: Kamera-FOV fuer den Realbild-Vergleich auf 10 Grad reduziert
        //(vorher 13.4 als hartcodierter Default-Text dieses Inputfelds).
        //18092026 (2) Nutzerwunsch: nochmal 20% weniger (10 * 0.8 = 8).
        //20092026 Nutzerwunsch: von 8 auf 5 Grad reduziert.
        //20092026 (2) Nutzerwunsch: nochmal 1 Grad weniger (5 -> 4).
        //21092026 Nutzerwunsch: 50 % weiter rausgezoomt -> sichtbarer Ausschnitt x1.5:
        //fov = 2*atan(1.5*tan(4°/2)) = 6.0° (gleicher Wert wie field_of_view in vis_3D.Start)
        float yFov = NextRow(ref y);
        Text comparisonFovLabel = CreateText("nakajima_comparison_fov_label", tabReal, "Kamera-FOV [°]:", font, 15, TextAnchor.MiddleRight);
        SetRect(comparisonFovLabel.rectTransform, new Vector2(-75f, yFov), new Vector2(180f, 38f), TR);
        InputField comparisonFov = CreateInputField("nakajima_comparison_fov", tabReal, "6", font);
        SetRect(comparisonFov.GetComponent<RectTransform>(), new Vector2(C0, yFov), new Vector2(60f, 38f), TR);

        float yCmp = NextRow(ref y);
        Button compareButton = CreateButton("nakajima_compare_button", tabReal, "Neu", font);
        SetRect(compareButton.GetComponent<RectTransform>(), new Vector2(C0, yCmp), B, TR);
        compareButton.GetComponentInChildren<Text>().fontSize = 16;
        compareButton.onClick.AddListener(() =>
        {
            GameObject sphere = GameObject.Find("sphere");
            if (sphere != null)
                NakajimaRealImageComparison.Run(sphere.GetComponent<vis_3D>(),
                    realImageName.text, comparisonFov.text);
        });
        Button loadComparisonButton = CreateButton("nakajima_load_comparison_button", tabReal, "Laden", font);
        SetRect(loadComparisonButton.GetComponent<RectTransform>(), new Vector2(C1, yCmp), B, TR);
        loadComparisonButton.GetComponentInChildren<Text>().fontSize = 16;
        loadComparisonButton.onClick.AddListener(NakajimaRealImageComparison.LoadPrevious);
        //18092026 Nutzerwunsch: Vergleichsbilder auch mit der jeweils anderen Kamera
        //aufnehmen (cam_1, die um den gleichen Winkel in die entgegengesetzte Richtung
        //geneigt ist), zum Vergleich mit den cam_0-Bildern von "Realbild: Neu".
        Button compareButtonOtherCam = CreateButton("nakajima_compare_button_other_cam", tabReal,
            "Neu (andere Kamera)", font);
        SetRect(compareButtonOtherCam.GetComponent<RectTransform>(), new Vector2(C0, NextRow(ref y)), B2, TR);
        compareButtonOtherCam.GetComponentInChildren<Text>().fontSize = 14;
        compareButtonOtherCam.onClick.AddListener(() =>
        {
            GameObject sphere = GameObject.Find("sphere");
            if (sphere != null)
                NakajimaRealImageComparison.Run(sphere.GetComponent<vis_3D>(),
                    realImageName.text, comparisonFov.text, useOtherCamera: true);
        });
        FinishTab(y);

        SelectTab(PlayerPrefs.GetInt("gui_tab", 0));
        MoveOldPanelsLeft(canvas);

        window = CreatePanel("gallery_window", canvas, new Color(0.08f, 0.1f, 0.13f, 0.96f));
        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = Vector2.zero;
        windowRect.sizeDelta = new Vector2(1000f, 650f);

        title = CreateText("title", window.transform, "Analysebilder", font, 24, TextAnchor.MiddleLeft);
        SetRect(title.rectTransform, new Vector2(20f, -15f), new Vector2(820f, 38f), new Vector2(0f, 1f));

        lightingLabel = CreateText("lighting_label", window.transform, "Beleuchtung", font, 18,
            TextAnchor.MiddleLeft);
        lightingLabel.color = new Color(0.35f, 0.8f, 1f, 1f);
        SetRect(lightingLabel.rectTransform, new Vector2(20f, -53f), new Vector2(650f, 30f),
            new Vector2(0f, 1f));

        Button closeButton = CreateButton("close_button", window.transform, "X", font);
        SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(-15f, -15f), new Vector2(42f, 36f), new Vector2(1f, 1f));
        closeButton.onClick.AddListener(Hide);

        //28092026 Nutzerwunsch: Zoom mit dem Mausrad. Das Bild liegt in einem maskierten Ausschnitt
        //  (image_viewport); Mausrad = Zoom um den Mauszeiger, linke Maustaste ziehen = verschieben,
        //  rechte Maustaste / Doppelklick = zuruecksetzen. Der Zoom bleibt beim Blaettern erhalten
        //  (gleiche Stelle bei naechstem k vergleichen).
        GameObject viewportObject = new GameObject("image_viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObject.transform.SetParent(window.transform, false);
        viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(25f, 85f);
        viewportRect.offsetMax = new Vector2(-300f, -92f);

        GameObject imageObject = new GameObject("rendered_image", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(viewportObject.transform, false);
        image = imageObject.GetComponent<Image>();
        image.color = Color.white;
        image.preserveAspect = true;
        RectTransform imageRect = image.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        zoomLabel = CreateText("zoom_label", window.transform, "Mausrad: Zoom", font, 13, TextAnchor.MiddleLeft);
        zoomLabel.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        SetRect(zoomLabel.rectTransform, new Vector2(25f, 64f), new Vector2(650f, 20f), Vector2.zero);

        Text resultsHeader = CreateText("results_header", window.transform, "Fehlerwerte", font, 20,
            TextAnchor.UpperLeft);
        //23092026 Bugfix: Pivot rechts -> x = -280 legte den Text in den Bildbereich (ab -535).
        //  Jetzt rechtsbuendig im 300-px-Streifen neben dem Bild, unter "Drehen"/"Kamera".
        SetRect(resultsHeader.rectTransform, new Vector2(-15f, -104f), new Vector2(265f, 28f),
            new Vector2(1f, 1f));

        resultsText = CreateText("results", window.transform,
            "Noch keine Analyse ausgeführt.", font, 14, TextAnchor.UpperLeft);
        SetRect(resultsText.rectTransform, new Vector2(-15f, -134f), new Vector2(265f, 440f),
            new Vector2(1f, 1f));
        resultsText.verticalOverflow = VerticalWrapMode.Overflow; //23092026 langer Bericht nicht abschneiden

        Button previousButton = CreateButton("previous_button", window.transform, "<  Zurück", font);
        SetRect(previousButton.GetComponent<RectTransform>(), new Vector2(25f, 18f), new Vector2(150f, 45f), Vector2.zero);
        previousButton.onClick.AddListener(Previous);

        counter = CreateText("counter", window.transform, "0 / 0", font, 20, TextAnchor.MiddleCenter);
        RectTransform counterRect = counter.rectTransform;
        counterRect.anchorMin = new Vector2(0.5f, 0f);
        counterRect.anchorMax = new Vector2(0.5f, 0f);
        counterRect.pivot = new Vector2(0.5f, 0f);
        counterRect.anchoredPosition = new Vector2(0f, 18f);
        counterRect.sizeDelta = new Vector2(260f, 45f);

        Button nextButton = CreateButton("next_button", window.transform, "Weiter  >", font);
        SetRect(nextButton.GetComponent<RectTransform>(), new Vector2(-25f, 18f), new Vector2(150f, 45f), new Vector2(1f, 0f));
        nextButton.onClick.AddListener(Next);

        //21092026 Nutzerwunsch: beim Blaettern nur eine Kamera zeigen (Bilder derselben Kamera
        //ueber die Zeit vergleichen, statt cam_0/cam_1 im Wechsel). Karten/Plots ohne
        //Kamera im Pfad werden immer gezeigt.
        cameraFilterButton = CreateButton("camera_filter_button", window.transform, "Kamera: alle", font);
        SetRect(cameraFilterButton.GetComponent<RectTransform>(), new Vector2(-15f, -60f), new Vector2(150f, 36f), new Vector2(1f, 1f));
        cameraFilterButton.GetComponentInChildren<Text>().fontSize = 16;
        cameraFilterButton.onClick.AddListener(CycleCameraFilter);

        //21092026 Nutzerwunsch: alle Bilder in der Galerie (nur zur Ansicht) in 90-Grad-
        //Schritten drehen. Winkel wird gemerkt (PlayerPrefs). Die Dateien bleiben unveraendert.
        rotateButton = CreateButton("rotate_button", window.transform, "Drehen 90°", font);
        SetRect(rotateButton.GetComponent<RectTransform>(), new Vector2(-170f, -60f), new Vector2(120f, 36f), new Vector2(1f, 1f));
        rotateButton.GetComponentInChildren<Text>().fontSize = 16;
        rotateButton.onClick.AddListener(RotateDisplay);
        displayRotation = PlayerPrefs.GetInt("gallery_rotation", 0);
        ApplyDisplayRotation();

        window.SetActive(false);

        //30092026 Tooltip zuletzt anlegen (liegt ueber allem) und Erklaerungen an die Bedienelemente haengen
        BuildTooltip(canvas, font);
        AddTooltips();
    }

    //30092026 Nutzerwunsch: kurze Erklaerung je Bedienelement (Objektname -> Text), erscheint beim Hovern
    /// <summary>
    /// Attaches a short explanation (tooltip) to every control of the control panel and of the TV-parameter panel, looked up by object name.
    /// </summary>
    private void AddTooltips()
    {
        var tips = new Dictionary<string, string>
        {
            // Reiter (Ueberfahren = Vorschau, Klick = fest oeffnen, erneuter Klick = einklappen)
            { "tab_Analyse", "Einzelanalyse: Genauigkeit gegen die Ground Truth, Frames, Belichtung k, Auflösung, Speckle-Muster, Render-Look, TV-Parameter und Parameterstudie.\nÜberfahren = Vorschau, Klick = fest öffnen, erneuter Klick = einklappen." },
            { "tab_Sweeps", "Parameterstudien über viele Stufen: Licht, Schrotrauschen, Speckle-Größe und Belichtung, jeweils neu rechnen oder letzte Ergebnisse laden.\nÜberfahren = Vorschau, Klick = fest öffnen, erneuter Klick = einklappen." },
            { "tab_Stereo", "Auswertungen mit beiden Kameras: Stereo-Tiefe (Höhenkarte) und 3D-Verschiebungsfeld in Raumkoordinaten, jeweils für die normale Analyse oder alle Licht- bzw. Speckle-Stufen.\nÜberfahren = Vorschau, Klick = fest öffnen, erneuter Klick = einklappen." },
            { "tab_Realbild", "Vergleich mit dem Foto des Nakajima-Versuchs: Szene im Nakajima-Look rendern und dem Realbild gegenüberstellen (cam_0 oder cam_1).\nÜberfahren = Vorschau, Klick = fest öffnen, erneuter Klick = einklappen." },
            // Analyse
            { "accuracy_button", "Vergleicht den zuletzt berechneten TV-Fluss (Frames aus dem Feld \"Frames\") mit der Ground Truth aus dem Mesh: Fehlerkarten für u/v, Dehnung und Statistik. Schreibt die Rohkarten nach exp_normal/time_flow_v/nice_pics." },
            { "load_saved_images_button", "Lädt die gespeicherten Analysebilder in die Galerie." },
            { "frames_field", "Zeitschritte der Probe, z.B. \"1, 27\" oder \"28, 29\" (auch Bereiche wie \"27-29\"). Mit OK übernehmen; gilt für Start, Genauigkeit, Höhe und 3D-Fluss." },
            { "frames_apply", "Frames übernehmen." },
            { "analysis_exposure_field", "Belichtungsfaktor k für die normale Analyse: die Bilder werden vor der TV-Rechnung mit k skaliert (1 = Original). In den Sweeps wird k ignoriert." },
            { "analysis_exposure_apply", "Belichtungsfaktor übernehmen." },
            { "resolution_slider_panel", "Renderauflösung der Kamerabilder (quadratisch, 64 bis 2048 px). Wird beim Loslassen angewendet." },
            { "speckle_mode_button", "Speckle-Textur für den Nakajima-Look umschalten (gemessen / zufällig / zufällig kontrastreich). Gilt ab dem nächsten Start." },
            { "nakajima_look_button", "Render-Look umschalten: klassisch oder Nakajima (Kameras, Licht und Probe wie im Realbild-Vergleich). Gilt ab dem nächsten Start." },
            { "tv_params_button", "TV-/TGV-Parameter (lambda, theta, Pyramide, Warps, Regularisierer). Überfahren = Vorschau, Klick = fest öffnen bzw. schließen." },
            { "param_study_button", "Startet die Parameterstudie (lambda, theta, Pyramidenstufen, Warps, Abbruchtoleranz, Regularisierung) am aktuellen Bildpaar. Zweiter Klick bricht nach dem laufenden Durchgang ab." },
            { "param_study_load_button", "Zeigt die Diagramme der letzten Parameterstudie." },
            // Sweeps
            { "lighting_levels", "Lichtfaktoren I für den Licht-Sweep, durch Komma getrennt. 1 = Referenzbeleuchtung (Lampen light1/light2 aus exp_setup)." },
            { "lighting_analysis_button", "Rendert und analysiert alle Lichtstufen der Liste. Bereits berechnete Stufen werden wiederverwendet; Ergebnisse in lighting_sweep.tsv." },
            { "load_lighting_analysis_button", "Zeigt Tabelle und Diagramm des letzten Licht-Sweeps." },
            { "noise_levels", "Vollaussteuerung Nmax in Elektronen für das Schrotrauschen (unendlich = rauschfrei). Kleinere Werte = stärkeres Rauschen." },
            { "noise_analysis_button", "Analysiert alle Rauschstufen der Liste; Ergebnisse in noise_sweep.tsv." },
            { "load_noise_analysis_button", "Zeigt Tabelle und Diagramm des letzten Rausch-Sweeps." },
            { "speckle_levels", "Speckle-Stufen, durch Semikolon getrennt: Materialgrößen (z.B. 0.700) oder prozedurale Muster p<s> (z.B. p4 = Kreisdurchmesser 4 Texturpixel)." },
            { "speckle_analysis_button", "Rendert und analysiert alle Speckle-Stufen der Liste; Ergebnisse in speckle_sweep.tsv." },
            { "load_speckle_analysis_button", "Zeigt Tabelle und Diagramm des letzten Speckle-Sweeps." },
            { "exposure_levels", "Belichtungsfaktoren k, angewendet auf die vorhandenen Kamerabilder (8-Bit-Sättigung; optional \"N=...\" für Schrotrauschen)." },
            { "exposure_study_button", "Belichtungsstudie auf den vorhandenen Bildern (ohne neues Rendern). Zweiter Klick bricht nach der laufenden Stufe ab." },
            { "exposure_study_load_button", "Zeigt die letzte Belichtungsstudie." },
            // Stereo
            { "height_analysis_button", "Stereo-Tiefe (cam_0/cam_1, Triangulation über die Kameramatrizen) für die letzte normale Analyse, Vergleich mit dem Mesh; Ergebnisse in depth_results.tsv." },
            { "height_lighting_button", "Stereo-Tiefe für alle Stufen des Licht-Sweeps." },
            { "height_speckle_button", "Stereo-Tiefe für alle Stufen des Speckle-Sweeps." },
            { "scene_flow_button", "3D-Verschiebungsfeld aus beiden Kameras (Stereo im ersten Frame + zeitlicher Fluss in cam_0 und cam_1) für die letzte normale Analyse, Vergleich mit dem Mesh; Ergebnisse in sceneflow_results.tsv." },
            { "scene_flow_lighting_button", "3D-Verschiebungsfeld für alle Stufen des Licht-Sweeps." },
            { "scene_flow_speckle_button", "3D-Verschiebungsfeld für alle Stufen des Speckle-Sweeps." },
            // Realbild
            { "nakajima_real_image", "Dateiname des Realbilds (Foto des Nakajima-Versuchs) für den Vergleich." },
            { "nakajima_comparison_fov", "Kamera-Öffnungswinkel in Grad für die Vergleichsaufnahme (Standard 6)." },
            { "nakajima_compare_button", "Rendert die Szene im Nakajima-Look mit cam_0 und stellt sie dem Realbild gegenüber." },
            { "nakajima_load_comparison_button", "Zeigt den letzten Realbild-Vergleich." },
            { "nakajima_compare_button_other_cam", "Wie \"Neu\", aber mit der zweiten Kamera (cam_1)." },
            // TV-Parameter-Panel
            { "tv_lambda", "Gewicht des Datenterms: größer = weniger Glättung, kleiner = glatteres Feld (Referenz 0.05). Leer = automatisch." },
            { "tv_theta", "Kopplung zwischen Fluss und Hilfsvariable im Dualverfahren (Referenz 0.3); kaum Einfluss auf das Ergebnis." },
            { "tv_scales", "Anzahl der Pyramidenstufen. Wird intern begrenzt, damit die gröbste Stufe mindestens ca. 16 px groß bleibt." },
            { "tv_warps", "Warps je Pyramidenstufe: mehr = genauer bei großen Verschiebungen, aber langsamer." },
            { "tv_iterations", "Maximale Anzahl Iterationen je Warp." },
            { "tv_epsilon", "Abbruchtoleranz der Iterationen (Referenz 5e-7); ab ca. 1e-2 bricht die Rechnung zu früh ab." },
            { "tv_strain_sigma", "Gauß-Glättung der Verschiebung vor dem Ableiten der Dehnung in Pixeln (leer = 12, 0 = aus)." },
            { "tv_tgv_ratio", "Verhältnis alpha0/alpha1 der TGV-Regularisierung; wirkt nur, wenn Regularisierung = TGV." },
            { "tv_params_apply", "Eingegebene Parameter übernehmen (gelten ab der nächsten Analyse)." },
            { "tv_params_reset", "Alle Felder leeren = automatische Standardwerte." },
            { "tv_gpu_button", "Rechenweg umschalten: GPU (Compute Shader, schnell) oder CPU (Referenz)." },
            { "tv_tgv_button", "Regularisierung umschalten: TV (erste Ordnung) oder TGV (zweite Ordnung, nur GPU)." },
        };
        foreach (GameObject root in new[] { controlPanel, tvPanel })
        {
            if (root == null)
                continue;
            foreach (RectTransform r in root.GetComponentsInChildren<RectTransform>(true))
                if (tips.TryGetValue(r.gameObject.name, out string text))
                    Tip(r.gameObject, text);
        }
    }

    //30092026 Bedienpanel mit Reitern (siehe BuildUi): schmale Spalte am rechten Rand unter "Bilder (N)", waechst nach
    //  unten; oben 2x2 Reiter (Lesereihenfolge), darunter der Inhalt des aktiven Reiters. Aktiven Reiter erneut
    //  klicken = einklappen (nur Reiter sichtbar). Reiterhoehe ergibt sich aus dem Zeilenzeiger (FinishTab).
    private const float PANEL_W = 265f, TAB_ROW_H = 90f, PANEL_TOP = -180f;
    private GameObject controlPanel;
    private readonly List<GameObject> tabContents = new List<GameObject>();
    private readonly List<Button> tabButtons = new List<Button>();
    private readonly List<float> tabHeights = new List<float>();
    private int activeTab = -1;
    private static readonly Color TAB_ACTIVE = new Color(0.55f, 0.8f, 1f, 1f), TAB_IDLE = new Color(0.72f, 0.72f, 0.72f, 1f);

    //30092026 Hover: Vorschau-Reiter (nur solange der Cursor im Panel ist), animierte Panelhoehe, Einblenden des Inhalts
    private int previewTab = -1;
    private float panelHeight = -1f, panelTarget;
    private readonly List<CanvasGroup> tabGroups = new List<CanvasGroup>();
    private static readonly Color TAB_PREVIEW = new Color(0.78f, 0.9f, 1f, 1f);

    /// <summary>
    /// Creates the control panel at the right edge below the gallery button; its content is clipped during the open/close animation.
    /// </summary>
    /// <param name="canvas">Canvas that receives the panel.</param>
    /// <param name="font">Font of the controls.</param>
    private void BuildControlPanel(Transform canvas, Font font)
    {
        controlPanel = CreatePanel("control_panel", canvas, new Color(0.1f, 0.12f, 0.15f, 0.93f));
        SetRect(controlPanel.GetComponent<RectTransform>(), new Vector2(-10f, PANEL_TOP), new Vector2(PANEL_W, TAB_ROW_H),
            new Vector2(1f, 1f));
        controlPanel.AddComponent<RectMask2D>(); //30092026 Inhalt waehrend der Auf-/Zuklapp-Animation abschneiden
    }

    /// <summary>
    /// Creates a tab button (2x2 layout, reading order) and the content container of the tab; hovering the button opens a preview.
    /// </summary>
    /// <param name="label">Text of the tab.</param>
    /// <param name="font">Font of the button.</param>
    /// <returns>Transform of the content container that receives the controls of the tab.</returns>
    private Transform CreateTab(string label, Font font)
    {
        int idx = tabContents.Count;
        // Reiter 0 1 / 2 3 von links nach rechts: gerade Indizes links (x = -137), ungerade rechts (x = -10)
        Vector2 pos = new Vector2(idx % 2 == 0 ? -137f : -10f, -8f - 40f * (idx / 2));
        Button b = CreateButton("tab_" + label, controlPanel.transform, label, font);
        SetRect(b.GetComponent<RectTransform>(), pos, new Vector2(118f, 34f), new Vector2(1f, 1f));
        b.GetComponentInChildren<Text>().fontSize = 16;
        b.onClick.AddListener(() => SelectTab(idx == activeTab ? -1 : idx));
        //30092026 Hover ueber dem Reiter = Vorschau; schliesst, sobald der Cursor das Panel verlaesst (UpdateHoverUi)
        HoverTip hover = b.gameObject.AddComponent<HoverTip>();
        hover.onEnter = () =>
        {
            previewTab = idx;
            ApplyTabDisplay();
        };
        GameObject content = new GameObject("tab_content_" + label, typeof(RectTransform), typeof(CanvasGroup));
        content.transform.SetParent(controlPanel.transform, false);
        SetRect(content.GetComponent<RectTransform>(), new Vector2(0f, -TAB_ROW_H), new Vector2(PANEL_W, 0f),
            new Vector2(1f, 1f));
        tabGroups.Add(content.GetComponent<CanvasGroup>());
        tabContents.Add(content);
        tabButtons.Add(b);
        tabHeights.Add(0f);
        return content.transform;
    }

    // Hoehe des zuletzt angelegten Reiters aus dem Zeilenzeiger y (negativ, unterhalb der letzten Zeile)
    /// <summary>
    /// Sets the height of the tab created last from the row cursor.
    /// </summary>
    /// <param name="y">Row cursor after the last control (negative, below the last row).</param>
    private void FinishTab(float y)
    {
        int i = tabContents.Count - 1;
        tabHeights[i] = -y + 2f;
        tabContents[i].GetComponent<RectTransform>().sizeDelta = new Vector2(PANEL_W, tabHeights[i]);
    }

    // Klick auf einen Reiter: fest oeffnen (pinnen) bzw. den gepinnten Reiter wieder einklappen
    /// <summary>
    /// Pins a tab open (click) or collapses the panel when the pinned tab is clicked again; ends a hover preview and stores the choice.
    /// </summary>
    /// <param name="idx">Index of the tab, or -1 to collapse.</param>
    private void SelectTab(int idx)
    {
        activeTab = idx >= 0 && idx < tabContents.Count ? idx : -1;
        previewTab = -1; // Klick beendet eine Vorschau (auch beim Einklappen sofort zu)
        ApplyTabDisplay();
        PlayerPrefs.SetInt("gui_tab", activeTab);
    }

    //30092026 angezeigt wird der Vorschau-Reiter, sonst der gepinnte; Hoehe wird in UpdateHoverUi animiert
    /// <summary>
    /// Shows the previewed tab or else the pinned one, colours the tab buttons, and sets the target height of the panel animation.
    /// </summary>
    private void ApplyTabDisplay()
    {
        int shown = previewTab >= 0 ? previewTab : activeTab;
        for (int i = 0; i < tabContents.Count; i++)
        {
            bool on = i == shown;
            if (on && !tabContents[i].activeSelf)
                tabGroups[i].alpha = 0f; // neu sichtbar: einblenden
            tabContents[i].SetActive(on);
            tabButtons[i].GetComponent<Image>().color = i == activeTab ? TAB_ACTIVE : i == shown ? TAB_PREVIEW : TAB_IDLE;
        }
        panelTarget = TAB_ROW_H + (shown >= 0 ? tabHeights[shown] : 0f);
        if (panelHeight < 0f) // erster Aufruf beim Aufbau: ohne Animation
        {
            panelHeight = panelTarget;
            controlPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(PANEL_W, panelHeight);
            foreach (CanvasGroup g in tabGroups)
                g.alpha = 1f;
        }
    }

    //30092026 TV-Parameter-Panel: Hover ueber "TV-Parameter" = Vorschau, Klick = fest oeffnen/schliessen; animiert
    private bool tvPinned, tvPreview;
    private float tvAnim;          // 0 = zu, 1 = offen
    private CanvasGroup tvGroup;
    private RectTransform tvToggleRect;

    /// <summary>
    /// Opens the TV-parameter panel (fade-in animation), fills its fields, and brings it and the tooltip to the front.
    /// </summary>
    private void OpenTvPanel()
    {
        if (!tvPanel.activeSelf)
        {
            tvPanel.SetActive(true);
            tvAnim = 0f;
            RefreshTvFields();
        }
        tvPanel.transform.SetAsLastSibling();
        if (tooltip != null)
            tooltip.transform.SetAsLastSibling();
    }

    // ---------------- Tooltip ----------------
    private GameObject tooltip;
    private Text tooltipText;
    private CanvasGroup tooltipGroup;
    private RectTransform canvasRect;
    private const float TIP_DELAY = 0.4f, TIP_W = 250f;

    /// <summary>
    /// Creates the tooltip panel, which never blocks pointer events.
    /// </summary>
    /// <param name="canvas">Canvas that receives the tooltip.</param>
    /// <param name="font">Font of the tooltip text.</param>
    private void BuildTooltip(Transform canvas, Font font)
    {
        canvasRect = canvas as RectTransform;
        tooltip = CreatePanel("hover_tooltip", canvas, new Color(0.06f, 0.07f, 0.09f, 0.95f));
        tooltip.GetComponent<Image>().raycastTarget = false; // darf das Hover-Ziel nicht verdecken
        tooltipGroup = tooltip.AddComponent<CanvasGroup>();
        tooltipGroup.blocksRaycasts = false;
        tooltipGroup.interactable = false;
        RectTransform tr = tooltip.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipText = CreateText("text", tooltip.transform, "", font, 13, TextAnchor.UpperLeft);
        tooltipText.raycastTarget = false;
        tooltipText.color = new Color(0.92f, 0.95f, 1f, 1f);
        RectTransform xr = tooltipText.rectTransform;
        xr.anchorMin = Vector2.zero;
        xr.anchorMax = Vector2.one;
        xr.offsetMin = new Vector2(8f, 6f);
        xr.offsetMax = new Vector2(-8f, -6f);
        tooltip.SetActive(false);
    }

    // Erklaerung an ein Bedienelement haengen (Name -> Text in AddTooltips)
    /// <summary>
    /// Attaches a tooltip text to a control (adds the hover component if needed).
    /// </summary>
    /// <param name="g">Control that shows the text.</param>
    /// <param name="text">Explanation.</param>
    private static void Tip(GameObject g, string text)
    {
        if (g == null)
            return;
        HoverTip h = g.GetComponent<HoverTip>();
        if (h == null)
            h = g.AddComponent<HoverTip>();
        h.text = text;
    }

    /// <summary>
    /// Per-frame update of the hover interface: closes the tab preview when the pointer leaves the panel, animates the panel height and the fade-in, handles preview and pinning of the TV-parameter panel, and positions the tooltip next to the cursor after a short delay.
    /// </summary>
    private void UpdateHoverUi()
    {
        if (controlPanel == null)
            return;
        Camera cam = UiCamera();
        Vector2 mouse = Input.mousePosition;
        float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 16f); // weiche Annaeherung (~0.15 s)

        // Reiter-Vorschau schliesst, sobald der Cursor das Bedienpanel verlaesst
        if (previewTab >= 0 && !RectTransformUtility.RectangleContainsScreenPoint(
                controlPanel.GetComponent<RectTransform>(), mouse, cam))
        {
            previewTab = -1;
            ApplyTabDisplay();
        }
        // Panelhoehe und Einblenden animieren
        if (Mathf.Abs(panelHeight - panelTarget) > 0.5f)
        {
            panelHeight = Mathf.Lerp(panelHeight, panelTarget, k);
            controlPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(PANEL_W, panelHeight);
        }
        else if (panelHeight != panelTarget)
        {
            panelHeight = panelTarget;
            controlPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(PANEL_W, panelHeight);
        }
        foreach (CanvasGroup g in tabGroups)
            if (g.gameObject.activeSelf && g.alpha < 1f)
                g.alpha = Mathf.Min(1f, g.alpha + Time.unscaledDeltaTime * 6f);

        // TV-Parameter-Panel: Vorschau schliesst, wenn der Cursor weder auf dem Schalter noch im Panel ist
        if (tvPanel != null)
        {
            bool overTv = tvPanel.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(
                tvPanel.GetComponent<RectTransform>(), mouse, cam);
            bool overToggle = tvToggleRect != null && tvToggleRect.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(tvToggleRect, mouse, cam);
            if (tvPreview && !tvPinned && !overTv && !overToggle)
                tvPreview = false;
            bool wantOpen = tvPinned || tvPreview;
            if (wantOpen)
                tvAnim = Mathf.Min(1f, tvAnim + Time.unscaledDeltaTime * 7f);
            else if (tvPanel.activeSelf)
            {
                tvAnim = Mathf.Max(0f, tvAnim - Time.unscaledDeltaTime * 7f);
                if (tvAnim <= 0f)
                    tvPanel.SetActive(false);
            }
            if (tvPanel.activeSelf && tvGroup != null)
            {
                float e = 1f - (1f - tvAnim) * (1f - tvAnim); // ease-out
                tvGroup.alpha = e;
                tvPanel.transform.localScale = new Vector3(0.94f + 0.06f * e, 0.94f + 0.06f * e, 1f);
            }
        }

        // Tooltip: nach kurzer Verzoegerung neben dem Cursor, weicht am Bildrand zur anderen Seite aus
        HoverTip cur = HoverTip.current;
        bool show = cur != null && !string.IsNullOrEmpty(cur.text) && cur.isActiveAndEnabled
            && Time.unscaledTime - HoverTip.enterTime > TIP_DELAY && !Input.GetMouseButton(0);
        if (show)
        {
            if (!tooltip.activeSelf)
            {
                tooltip.SetActive(true);
                tooltipGroup.alpha = 0f;
                tooltip.transform.SetAsLastSibling();
            }
            if (tooltipText.text != cur.text)
                tooltipText.text = cur.text;
            RectTransform tr = tooltip.GetComponent<RectTransform>();
            float h = tooltipText.cachedTextGenerator.GetPreferredHeight(cur.text,
                tooltipText.GetGenerationSettings(new Vector2(TIP_W - 16f, 0f))) / tooltipText.pixelsPerUnit + 12f;
            tr.sizeDelta = new Vector2(TIP_W, h);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouse, cam, out Vector2 local);
            Rect c = canvasRect.rect;
            bool left = local.x > c.center.x;                // rechte Bildhaelfte -> Tooltip links vom Cursor
            bool up = local.y - 20f - h < c.yMin;            // unten kein Platz -> ueber dem Cursor
            tr.pivot = new Vector2(left ? 1f : 0f, up ? 0f : 1f);
            tr.anchoredPosition = local - c.center + new Vector2(left ? -14f : 14f, up ? 18f : -20f);
            tooltipGroup.alpha = Mathf.Min(1f, tooltipGroup.alpha + Time.unscaledDeltaTime * 8f);
        }
        else if (tooltip != null && tooltip.activeSelf)
        {
            tooltipGroup.alpha -= Time.unscaledDeltaTime * 10f;
            if (tooltipGroup.alpha <= 0f)
                tooltip.SetActive(false);
        }
    }

    //30092026 Nutzerwunsch: aeltere Szenen-Panels (Choose_panel "Paths" mit der Bildvorschau maps, series_panel),
    //  die in den Streifen des Bedienpanels ragen, zur Laufzeit nach links schieben (Szenendatei bleibt unveraendert).
    //  Alle rechtsbuendigen alten Panels im betroffenen Hoehenbereich wandern um denselben Betrag, damit ihre
    //  Anordnung zueinander erhalten bleibt; hoechstens bis zum linken Bildrand.
    /// <summary>
    /// Shifts right-aligned legacy scene panels that overlap the strip of the control panel (e.g. the path panel with its image preview and the series panel) to the left by a common offset, at most to the left screen edge; the scene file is not changed.
    /// </summary>
    /// <param name="canvas">Canvas whose children are checked.</param>
    private void MoveOldPanelsLeft(Transform canvas)
    {
        RectTransform canvasRect = canvas as RectTransform;
        if (canvasRect == null)
            return;
        Canvas.ForceUpdateCanvases();
        Rect c = canvasRect.rect;
        float maxTab = 0f;
        foreach (float h in tabHeights)
            maxTab = Mathf.Max(maxTab, h);
        // freizuhaltender Streifen: Bedienpanel (voll ausgeklappt) samt "Bilder (N)" darueber, 10 Einheiten Rand
        float stripLeft = c.xMax - 10f - PANEL_W - 10f;
        float stripTop = c.yMax - 130f, stripBottom = c.yMax + PANEL_TOP - TAB_ROW_H - maxTab;

        var group = new List<(RectTransform r, Rect bounds)>();
        float need = 0f, room = float.MaxValue;
        Vector3[] corners = new Vector3[4];
        foreach (Transform child in canvas)
        {
            RectTransform r = child as RectTransform;
            if (r == null || !child.gameObject.activeSelf || child == controlPanel.transform
                || (tvPanel != null && child == tvPanel.transform) || child == openButton.transform
                || (window != null && child == window.transform) || r.anchorMin.x < 0.99f)
                continue;
            // Ausdehnung des Panels samt sichtbarer Unterelemente in Canvas-Koordinaten
            float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
            foreach (RectTransform d in r.GetComponentsInChildren<RectTransform>(false))
            {
                d.GetWorldCorners(corners);
                foreach (Vector3 w in corners)
                {
                    Vector3 p = canvasRect.InverseTransformPoint(w);
                    xMin = Mathf.Min(xMin, p.x); xMax = Mathf.Max(xMax, p.x);
                    yMin = Mathf.Min(yMin, p.y); yMax = Mathf.Max(yMax, p.y);
                }
            }
            if (yMax < stripBottom || yMin > stripTop)
                continue; // nicht im Hoehenbereich des Bedienpanels
            group.Add((r, Rect.MinMaxRect(xMin, yMin, xMax, yMax)));
            need = Mathf.Max(need, xMax - stripLeft);
            room = Mathf.Min(room, xMin - c.xMin);
        }
        if (need <= 0f || group.Count == 0)
            return;
        float dx = Mathf.Min(need, Mathf.Max(0f, room));
        var moved = new List<string>();
        foreach (var g in group)
        {
            g.r.anchoredPosition -= new Vector2(dx, 0f);
            moved.Add(g.r.name);
        }
        Debug.Log("GUI: alte Panels um " + dx.ToString("0", CultureInfo.InvariantCulture) + " nach links verschoben ("
            + string.Join(", ", moved) + ")" + (dx < need ? " - Fenster zu schmal, Rest-Ueberlappung "
            + (need - dx).ToString("0", CultureInfo.InvariantCulture) : ""));
    }

    // naechste Zeile (Hoehe 45) am Zeilenzeiger y: gibt die obere Kante zurueck und rueckt y weiter
    /// <summary>
    /// Returns the top of the next control row and advances the row cursor by one row (45 units).
    /// </summary>
    /// <param name="y">Row cursor, updated in place.</param>
    /// <returns>Top edge of the row.</returns>
    private static float NextRow(ref float y)
    {
        float top = y;
        y -= 45f;
        return top;
    }

    // Abschnittsueberschrift (Hoehe 22) am Zeilenzeiger y
    /// <summary>
    /// Adds a section heading at the row cursor and advances the cursor.
    /// </summary>
    /// <param name="parent">Tab content that receives the heading.</param>
    /// <param name="label">Heading text.</param>
    /// <param name="y">Row cursor, updated in place.</param>
    /// <param name="font">Font of the heading.</param>
    private static void Header(Transform parent, string label, ref float y, Font font)
    {
        Text t = CreateText("section_" + label, parent, label, font, 14, TextAnchor.LowerLeft);
        t.color = new Color(0.6f, 0.85f, 1f, 1f);
        SetRect(t.rectTransform, new Vector2(-10f, y), new Vector2(245f, 20f), new Vector2(1f, 1f));
        y -= 22f;
    }

    //21092026 Beschriftung des Look-Umschalters: zeigt den AKTIVEN Zustand und was ein Klick tut.
    /// <summary>
    /// Label of the render-look switch showing the active look and the effect of a click.
    /// </summary>
    /// <param name="nakajima">True if the Nakajima look is active.</param>
    /// <returns>Button text.</returns>
    private static string LookLabel(bool nakajima)
    {
        return nakajima ? "Look AKTIV: Nakajima  (Klick -> klassisch)" : "Look AKTIV: klassisch  (Klick -> Nakajima)";
    }

    //22092026 TV-Parameter-Panel: leeres Feld = "auto" (der bisherige, vom Modus abhaengige Wert).
    private GameObject tvPanel;
    private InputField tvLambda, tvTheta, tvScales, tvWarps, tvIterations, tvEpsilon, tvStrainSigma, tvTgvRatio;

    //30092026 Schalter liegt im Reiter "Analyse" (toggleParent), das Panel auf dem Canvas links neben dem Bedienpanel
    /// <summary>
    /// Creates the TV-parameter button inside a tab and the TV-parameter panel (lambda, theta, pyramid levels, warps, iterations, epsilon, strain smoothing, TGV ratio, GPU/CPU and TV/TGV switches) next to the control panel.
    /// </summary>
    /// <param name="canvas">Canvas that receives the panel.</param>
    /// <param name="toggleParent">Tab content that receives the button.</param>
    /// <param name="togglePos">Position of the button.</param>
    /// <param name="toggleSize">Size of the button.</param>
    /// <param name="font">Font of the controls.</param>
    private void BuildTvParameterPanel(Transform canvas, Transform toggleParent, Vector2 togglePos, Vector2 toggleSize, Font font)
    {
        Button toggle = CreateButton("tv_params_button", toggleParent, "TV-Parameter", font);
        SetRect(toggle.GetComponent<RectTransform>(), togglePos, toggleSize, new Vector2(1f, 1f));
        toggle.GetComponentInChildren<Text>().fontSize = 16;

        tvPanel = CreatePanel("tv_params_panel", canvas, new Color(0.12f, 0.12f, 0.14f, 0.96f));
        SetRect(tvPanel.GetComponent<RectTransform>(), new Vector2(-(PANEL_W + 20f), PANEL_TOP),
            new Vector2(300f, 486f), new Vector2(1f, 1f)); //23092026 330 -> 372 fuer den GPU-Schalter; 27092026 -> 410 fuer Strain-sigma, -> 486 fuer TGV; 30092026 links neben dem Bedienpanel
        tvPanel.SetActive(false);
        //30092026 Hover = Vorschau (schliesst beim Wegbewegen), Klick = fest oeffnen bzw. wieder schliessen;
        //  Ein-/Ausblenden animiert in UpdateHoverUi
        tvGroup = tvPanel.AddComponent<CanvasGroup>();
        tvToggleRect = toggle.GetComponent<RectTransform>();
        toggle.onClick.AddListener(() =>
        {
            tvPinned = !tvPinned;
            tvPreview = false;
            if (tvPinned)
                OpenTvPanel();
        });
        HoverTip toggleHover = toggle.gameObject.AddComponent<HoverTip>();
        toggleHover.onEnter = () =>
        {
            if (tvPinned)
                return;
            tvPreview = true;
            OpenTvPanel();
        };

        Text header = CreateText("tv_params_header", tvPanel.transform,
            "TV-L1-Parameter (leer = auto)", font, 17, TextAnchor.UpperLeft);
        SetRect(header.rectTransform, new Vector2(12f, -10f), new Vector2(280f, 26f), new Vector2(0f, 1f));

        tvLambda = AddTvField("lambda (Datenterm, gross = weniger Glaettung)", "tv_lambda", 44f, font);
        tvTheta = AddTvField("theta (Kopplung)", "tv_theta", 82f, font);
        tvScales = AddTvField("nscales (Pyramidenstufen)", "tv_scales", 120f, font);
        tvWarps = AddTvField("nwarps (Warps je Stufe)", "tv_warps", 158f, font);
        tvIterations = AddTvField("Iterationen (max)", "tv_iterations", 196f, font);
        tvEpsilon = AddTvField("epsilon (Abbruch)", "tv_epsilon", 234f, font);
        //27092026 Glaettung vor dem Ableiten der Dehnung bei "Genauigkeit" (leer = 12 px, 0 = aus)
        tvStrainSigma = AddTvField("Strain-Glaettung sigma [px]", "tv_strain_sigma", 272f, font);
        //27092026 TGV: Verhaeltnis alpha0/alpha1 (leer = 3); wirkt nur, wenn Regularisierung = TGV
        tvTgvRatio = AddTvField("TGV alpha0/alpha1", "tv_tgv_ratio", 310f, font);

        Button apply = CreateButton("tv_params_apply", tvPanel.transform, "Uebernehmen", font);
        SetRect(apply.GetComponent<RectTransform>(), new Vector2(12f, -354f), new Vector2(135f, 36f), new Vector2(0f, 1f));
        apply.GetComponentInChildren<Text>().fontSize = 15;
        apply.onClick.AddListener(ApplyTvFields);

        Button reset = CreateButton("tv_params_reset", tvPanel.transform, "Auto", font);
        SetRect(reset.GetComponent<RectTransform>(), new Vector2(155f, -354f), new Vector2(133f, 36f), new Vector2(0f, 1f));
        reset.GetComponentInChildren<Text>().fontSize = 15;
        reset.onClick.AddListener(() =>
        {
            tvLambda.text = tvTheta.text = tvScales.text = "";
            tvWarps.text = tvIterations.text = tvEpsilon.text = "";
            ApplyTvFields();
        });

        //23092026 Rechenweg TV-L1: GPU (ComputeShader) oder CPU (Referenz)
        tvGpuButton = CreateButton("tv_gpu_button", tvPanel.transform, TvGpuLabel(true), font);
        SetRect(tvGpuButton.GetComponent<RectTransform>(), new Vector2(12f, -398f), new Vector2(276f, 36f), new Vector2(0f, 1f));
        tvGpuButton.GetComponentInChildren<Text>().fontSize = 15;
        tvGpuButton.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis == null)
                return;
            vis.set_tv_use_gpu(!vis.get_tv_use_gpu());
            RefreshTvFields();
            SetResultsText("TV-Rechenweg: " + (vis.get_tv_use_gpu() ? "GPU" : "CPU")
                + "\nGilt ab der naechsten Analyse. Laufzeit steht danach im Log (\"TV-L1 fertig: ...\").");
            ShowResultsWindow();
        });

        //27092026 Nutzerwunsch: TGV-Regularisierung als Option; ein Klick schaltet jederzeit auf TV zurueck
        tvTgvButton = CreateButton("tv_tgv_button", tvPanel.transform, TvTgvLabel(false), font);
        SetRect(tvTgvButton.GetComponent<RectTransform>(), new Vector2(12f, -442f), new Vector2(276f, 36f), new Vector2(0f, 1f));
        tvTgvButton.GetComponentInChildren<Text>().fontSize = 15;
        tvTgvButton.onClick.AddListener(() =>
        {
            vis_3D vis = Vis();
            if (vis == null)
                return;
            vis.set_tv_use_tgv(!vis.get_tv_use_tgv());
            RefreshTvFields();
            SetResultsText("Regularisierung: " + vis.describe_regularization()
                + "\nGilt ab der naechsten Analyse (with_exp + with_tv -> Start)."
                + (vis.get_tv_use_tgv() && !vis.get_tv_use_gpu() ? "\nAchtung: TGV nur auf der GPU - Rechenweg steht auf CPU." : "")
                + (vis.get_tv_use_tgv() ? "\nTGV konvergiert langsamer: ggf. Iterationen erhoehen (z.B. 500)." : ""));
            ShowResultsWindow();
        });
    }

    private Button tvGpuButton;
    private Button tvTgvButton;

    //27092026 Beschriftung des Regularisierungs-Schalters (zeigt den AKTIVEN Zustand)
    /// <summary>
    /// Label of the regulariser switch showing the active regulariser.
    /// </summary>
    /// <param name="tgv">True if TGV is active.</param>
    /// <returns>Button text.</returns>
    private static string TvTgvLabel(bool tgv)
    {
        return tgv ? "Regularisierung AKTIV: TGV  (Klick -> TV)" : "Regularisierung AKTIV: TV  (Klick -> TGV)";
    }

    /// <summary>
    /// Label of the computation-path switch showing whether the GPU or the CPU is used.
    /// </summary>
    /// <param name="gpu">True if the GPU path is active.</param>
    /// <returns>Button text.</returns>
    private static string TvGpuLabel(bool gpu)
    {
        return gpu ? "Rechenweg AKTIV: GPU  (Klick -> CPU)" : "Rechenweg AKTIV: CPU  (Klick -> GPU)";
    }

    /// <summary>
    /// Adds a labelled input field to the TV-parameter panel.
    /// </summary>
    /// <param name="label">Description shown left of the field.</param>
    /// <param name="name">Object name of the field.</param>
    /// <param name="y">Vertical offset from the top of the panel.</param>
    /// <param name="font">Font of label and field.</param>
    /// <returns>The input field.</returns>
    private InputField AddTvField(string label, string name, float y, Font font)
    {
        Text text = CreateText(name + "_label", tvPanel.transform, label, font, 13, TextAnchor.MiddleLeft);
        SetRect(text.rectTransform, new Vector2(12f, -y), new Vector2(190f, 30f), new Vector2(0f, 1f));
        InputField field = CreateInputField(name, tvPanel.transform, "", font);
        SetRect(field.GetComponent<RectTransform>(), new Vector2(208f, -y), new Vector2(80f, 30f), new Vector2(0f, 1f));
        return field;
    }

    /// <summary>
    /// Looks up the lab controller.
    /// </summary>
    /// <returns>The vis_3D component or null.</returns>
    private static vis_3D Vis()
    {
        GameObject sphere = GameObject.Find("sphere");
        return sphere != null ? sphere.GetComponent<vis_3D>() : null;
    }

    /// <summary>
    /// Fills the TV-parameter fields with the current overrides (empty = automatic) and updates the switch labels.
    /// </summary>
    private void RefreshTvFields()
    {
        vis_3D vis = Vis();
        if (vis == null)
            return;
        tvLambda.text = TvText(vis.get_tv_lambda_override());
        tvTheta.text = TvText(vis.get_tv_theta_override());
        tvScales.text = TvText(vis.get_tv_nscales_override());
        tvWarps.text = TvText(vis.get_tv_nwarps_override());
        tvIterations.text = TvText(vis.get_tv_iterations_override());
        tvEpsilon.text = TvText(vis.get_tv_epsilon_override());
        if (tvStrainSigma != null)
            tvStrainSigma.text = vis.get_strain_sigma().ToString("0.##", CultureInfo.InvariantCulture);
        if (tvTgvRatio != null)
            tvTgvRatio.text = vis.get_tgv_ratio().ToString("0.##", CultureInfo.InvariantCulture);
        if (tvTgvButton != null)
            tvTgvButton.GetComponentInChildren<Text>().text = TvTgvLabel(vis.get_tv_use_tgv());
        if (tvGpuButton != null)
            tvGpuButton.GetComponentInChildren<Text>().text = TvGpuLabel(vis.get_tv_use_gpu());
    }

    /// <summary>
    /// Formats a parameter override for its input field.
    /// </summary>
    /// <param name="value">Override value (NaN or non-positive = automatic).</param>
    /// <returns>Text for the field, empty for automatic.</returns>
    private static string TvText(double value)
    {
        return (double.IsNaN(value) || value <= 0d) ? "" : value.ToString("G6", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats an integer parameter override for its input field.
    /// </summary>
    /// <param name="value">Override value (non-positive = automatic).</param>
    /// <returns>Text for the field, empty for automatic.</returns>
    private static string TvText(int value)
    {
        return value <= 0 ? "" : value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses a number from an input field; comma and point are both accepted as decimal separator.
    /// </summary>
    /// <param name="field">Input field.</param>
    /// <returns>The value, or NaN if the field is empty or invalid.</returns>
    private static double TvParse(InputField field)
    {
        string text = field.text == null ? "" : field.text.Trim();
        if (text.Length == 0)
            return double.NaN;
        //22092026 Komma und Punkt beide als Dezimaltrennzeichen akzeptieren, unabhaengig von
        //der Systemsprache (NumberStyles.Float laesst keine Tausendertrenner zu, "0,001"
        //kann also nicht als 1 missverstanden werden).
        double value;
        if (double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            return value;
        return double.NaN;
    }

    //27092026 wie TvParse, aber 0 ist ein gueltiger Wert (Strain-Glaettung aus); ungueltig -> NaN
    /// <summary>
    /// Like TvParse, but zero is a valid value (e.g. strain smoothing off).
    /// </summary>
    /// <param name="field">Input field.</param>
    /// <returns>The value (at least 0), or NaN if empty or invalid.</returns>
    private static double TvParseOrZero(InputField field)
    {
        double value = TvParse(field);
        return (double.IsNaN(value) || value < 0d) ? double.NaN : value;
    }

    /// <summary>
    /// Parses a positive integer from an input field.
    /// </summary>
    /// <param name="field">Input field.</param>
    /// <returns>The rounded value, or -1 for empty or invalid input.</returns>
    private static int TvParseInt(InputField field)
    {
        double value = TvParse(field);
        return (double.IsNaN(value) || value <= 0d) ? -1 : (int)System.Math.Round(value);
    }

    /// <summary>
    /// Passes the values of the TV-parameter panel to the lab controller and reports the active parameters.
    /// </summary>
    private void ApplyTvFields()
    {
        vis_3D vis = Vis();
        if (vis == null)
            return;
        vis.set_tv_overrides(TvParse(tvLambda), TvParse(tvTheta), TvParseInt(tvScales),
            TvParseInt(tvWarps), TvParseInt(tvIterations), TvParse(tvEpsilon));
        //27092026 Strain-Glaettung: leer = 12 px (Default), 0 = aus; wirkt sofort beim naechsten "Genauigkeit"
        string sig_text = tvStrainSigma != null ? tvStrainSigma.text.Trim() : "";
        vis.set_strain_sigma(sig_text == "" ? 12f : (float)TvParseOrZero(tvStrainSigma));
        //27092026 TGV alpha0/alpha1 (leer oder ungueltig = 3)
        vis.set_tgv_ratio(tvTgvRatio != null ? (float)TvParse(tvTgvRatio) : 3f);
        RefreshTvFields();
        SetResultsText("TV-Parameter: " + vis.describe_tv_overrides()
            + "\nRegularisierung: " + vis.describe_regularization()
            + "\nGilt ab der naechsten Analyse (with_exp + with_tv -> Start)."
            + "\nStrain-Glaettung sigma = " + vis.get_strain_sigma().ToString("0.##", CultureInfo.InvariantCulture)
            + " px (gilt beim naechsten \"Genauigkeit\", ohne neue Analyse).");
        ShowResultsWindow();
    }

    private Button rotateButton;
    private int displayRotation = 0; // 0, 90, 180, 270 (Grad, gegen den Uhrzeigersinn)

    /// <summary>
    /// Rotates the displayed images by 90 degrees (view only; the files are unchanged) and stores the angle.
    /// </summary>
    private void RotateDisplay()
    {
        displayRotation = (displayRotation + 90) % 360;
        PlayerPrefs.SetInt("gallery_rotation", displayRotation);
        PlayerPrefs.Save();
        ApplyDisplayRotation();
    }

    //28092026 Zoom/Verschieben der Galerieansicht (alter Input Manager, wie Cam_manager)
    private RectTransform viewportRect;
    private Text zoomLabel;
    private float zoom = 1f;
    private bool panning;
    private Vector2 panStartLocal, panStartPosition;
    private float lastClickTime = -1f;
    private const float ZoomMax = 16f;

    /// <summary>
    /// Returns the camera needed for screen-to-UI conversions (null for screen-space overlay canvases).
    /// </summary>
    /// <returns>Camera of the canvas or null.</returns>
    private Camera UiCamera()
    {
        Canvas canvas = window != null ? window.GetComponentInParent<Canvas>() : null;
        return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    //28092026 fuer Cam_manager: Mausrad/Maustasten ueber der Galerie nicht an die Kamera weitergeben
    /// <summary>
    /// Checks whether the pointer is over the gallery window, the control panel, or the TV-parameter panel (used by the camera control to ignore the mouse wheel there).
    /// </summary>
    /// <returns>True if the pointer is over one of these panels.</returns>
    public static bool PointerOverGallery()
    {
        if (instance == null)
            return false;
        //30092026 auch Bedienpanel und TV-Parameter-Panel: Mausrad dort nicht an die Kamera weitergeben
        foreach (GameObject g in new[] { instance.window, instance.controlPanel, instance.tvPanel })
            if (g != null && g.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(
                    g.GetComponent<RectTransform>(), Input.mousePosition, instance.UiCamera()))
                return true;
        return false;
    }

    /// <summary>
    /// Resets zoom and panning of the displayed image.
    /// </summary>
    private void ResetZoom()
    {
        zoom = 1f;
        panning = false;
        if (image == null) return;
        image.rectTransform.localScale = Vector3.one;
        image.rectTransform.anchoredPosition = Vector2.zero;
        UpdateZoomLabel();
    }

    /// <summary>
    /// Updates the zoom hint below the image.
    /// </summary>
    private void UpdateZoomLabel()
    {
        if (zoomLabel != null)
            zoomLabel.text = zoom <= 1.001f ? "Mausrad: Zoom"
                : "Zoom " + zoom.ToString("0.0", CultureInfo.InvariantCulture) + "x  (ziehen: verschieben, Rechtsklick/Doppelklick: zuruecksetzen)";
    }

    // Verschiebung begrenzen, damit das Bild nicht aus dem Ausschnitt wandert
    /// <summary>
    /// Limits the panning so that the zoomed image does not leave the viewport.
    /// </summary>
    /// <param name="position">Requested image position.</param>
    /// <returns>Clamped position.</returns>
    private Vector2 ClampPan(Vector2 position)
    {
        Vector2 size = viewportRect.rect.size;
        float mx = 0.5f * size.x * (zoom - 1f), my = 0.5f * size.y * (zoom - 1f);
        return new Vector2(Mathf.Clamp(position.x, -mx, mx), Mathf.Clamp(position.y, -my, my));
    }

    /// <summary>
    /// Unity callback: updates the hover interface and handles zoom (mouse wheel around the cursor), panning (drag), and reset (right click or double click) of the gallery image.
    /// </summary>
    private void Update()
    {
        UpdateHoverUi(); //30092026 Tooltips, Reiter-Vorschau, Animationen (auch ohne offenes Bildfenster)
        if (window == null || !window.activeInHierarchy || viewportRect == null || image == null)
            return;
        Camera uiCamera = UiCamera();
        Vector2 mouse = Input.mousePosition;
        bool over = RectTransformUtility.RectangleContainsScreenPoint(viewportRect, mouse, uiCamera);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect, mouse, uiCamera, out Vector2 local);
        local -= viewportRect.rect.center; // relativ zur Mitte (Pivot des Bildes)
        RectTransform imageRect = image.rectTransform;

        float scroll = Input.mouseScrollDelta.y;
        if (over && scroll != 0f)
        {
            float newZoom = Mathf.Clamp(zoom * Mathf.Pow(1.2f, scroll), 1f, ZoomMax);
            // Punkt unter dem Mauszeiger bleibt stehen: a' = p - (p - a) * s'/s
            Vector2 a = imageRect.anchoredPosition;
            Vector2 a2 = local - (local - a) * (newZoom / zoom);
            zoom = newZoom;
            imageRect.localScale = new Vector3(zoom, zoom, 1f);
            imageRect.anchoredPosition = ClampPan(a2);
            UpdateZoomLabel();
        }

        if (over && Input.GetMouseButtonDown(1))
            ResetZoom();
        if (over && Input.GetMouseButtonDown(0))
        {
            if (Time.unscaledTime - lastClickTime < 0.3f)
            {
                ResetZoom();
                lastClickTime = -1f;
                return;
            }
            lastClickTime = Time.unscaledTime;
            panning = zoom > 1.001f;
            panStartLocal = local;
            panStartPosition = imageRect.anchoredPosition;
        }
        if (panning)
        {
            if (!Input.GetMouseButton(0))
                panning = false;
            else
                imageRect.anchoredPosition = ClampPan(panStartPosition + (local - panStartLocal));
        }
    }

    /// <summary>
    /// Applies the stored display rotation to the image and the rotate button.
    /// </summary>
    private void ApplyDisplayRotation()
    {
        if (image == null)
            return;
        image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, displayRotation);
        if (rotateButton != null)
            rotateButton.GetComponentInChildren<Text>().text = displayRotation == 0 ? "Drehen 90°" : "Gedreht: " + displayRotation + "°";
    }

    private Button cameraFilterButton;
    private string cameraFilter = "alle"; // "alle" | "cam_0" | "cam_1"

    /// <summary>
    /// Cycles the camera filter of the gallery (all images, only cam_0, only cam_1).
    /// </summary>
    private void CycleCameraFilter()
    {
        cameraFilter = cameraFilter == "alle" ? "cam_0" : cameraFilter == "cam_0" ? "cam_1" : "alle";
        cameraFilterButton.GetComponentInChildren<Text>().text = "Kamera: " + cameraFilter;
        if (sprites.Count > 0 && !MatchesFilter(paths[currentIndex]))
            Next();
        else
            UpdateDisplay();
    }

    /// <summary>
    /// Checks whether an image passes the camera filter; maps and plots without camera in the path always pass.
    /// </summary>
    /// <param name="path">Path of the image.</param>
    /// <returns>True if the image is shown.</returns>
    private bool MatchesFilter(string path)
    {
        if (cameraFilter == "alle")
            return true;
        string normalized = path.Replace('\\', '/');
        bool isCam0 = normalized.Contains("/cam_0/");
        bool isCam1 = normalized.Contains("/cam_1/");
        if (!isCam0 && !isCam1)
            return true; // Karten/Plots ohne Kamera
        return cameraFilter == "cam_0" ? isCam0 : isCam1;
    }

    /// <summary>
    /// Counts the images that pass the camera filter.
    /// </summary>
    /// <returns>Number of visible images.</returns>
    private int CountMatching()
    {
        int n = 0;
        foreach (string path in paths)
            if (MatchesFilter(path)) n++;
        return n;
    }

    /// <summary>
    /// Position of an image among the images that pass the camera filter.
    /// </summary>
    /// <param name="index">Index in the complete list.</param>
    /// <returns>1-based position among the visible images.</returns>
    private int IndexAmongMatching(int index)
    {
        int n = 0;
        for (int i = 0; i <= index && i < paths.Count; i++)
            if (MatchesFilter(paths[i])) n++;
        return n;
    }

    //21092026 Nutzerwunsch: gespeicherte Renders (cam_0/cam_1) und Karten (nice_pics/plot_*)
    //fuer das aktuelle Experiment + Aufloesung von der Platte laden - ohne neue Analyse.
    //Sortierung: erst cam_0 nach Zeitindex, dann cam_1, dann die Karten.
    /// <summary>
    /// Loads the saved renders (cam_0, cam_1) and maps of the current experiment and resolution from disk without a new analysis.
    /// </summary>
    public static void LoadSavedImages()
    {
        if (instance == null)
            return;
        GameObject sphere = GameObject.Find("sphere");
        vis_3D vis = sphere != null ? sphere.GetComponent<vis_3D>() : null;
        if (vis == null)
            return;
        string experiment = vis.get_experiment();
        if (string.IsNullOrEmpty(experiment))
            experiment = "exp_normal";
        int res = vis.get_render_res();
        string experimentDirectory = Application.dataPath + experiment.Replace(".", "");
        string suffix = "_r" + res + ".png";

        var entries = new List<(int order, int t, string path)>();
        for (int cam = 0; cam < 2; cam++)
        {
            string uvDir = Path.Combine(experimentDirectory, "cam_" + cam, "uv");
            if (!Directory.Exists(uvDir))
                continue;
            foreach (string file in Directory.GetFiles(uvDir, "*im_*" + suffix))
            {
                Match m = Regex.Match(Path.GetFileName(file), @"im_(\d+)_r\d+\.png$");
                if (!m.Success)
                    continue;
                entries.Add((cam, int.Parse(m.Groups[1].Value), file));
            }
        }
        string nicePics = Path.Combine(Application.dataPath + "exp_normal", "time_flow_v", "nice_pics");
        if (Directory.Exists(nicePics))
        {
            //23092026 Genauigkeits-Panels (u, v) vor den Einzelkarten
            foreach (string file in Directory.GetFiles(nicePics, "accuracy_" + experiment + "_*_r" + res + "_*.png"))
                entries.Add((2, -1, file));
            foreach (string file in Directory.GetFiles(nicePics, "plot_" + experiment + "_*" + "_r" + res + "_min*.png"))
                entries.Add((2, 0, file));
        }
        //28092026 belichtete TV-Eingangsbilder (Analyse-k) dieser Aufloesung, nach den Karten
        string exposureDir = Path.Combine(Application.dataPath, "analysis_results", "exposure_images");
        if (Directory.Exists(exposureDir))
        {
            foreach (string file in Directory.GetFiles(exposureDir, "*_r" + res + "_analyse_k*.png"))
                entries.Add((3, 0, file));
        }
        entries.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order)
            : a.t != b.t ? a.t.CompareTo(b.t) : string.CompareOrdinal(a.path, b.path));

        instance.ClearImages();
        foreach (var entry in entries)
            instance.LoadImage(entry.path);
        if (entries.Count > 0)
            instance.currentIndex = 0;
        instance.resultsText.text = entries.Count > 0
            ? entries.Count + " gespeicherte Bilder geladen (" + experiment + ", r" + res + ")."
            : "Keine gespeicherten Bilder fuer " + experiment + " bei r" + res + " gefunden ("
                + experimentDirectory + ").";
        instance.openButton.gameObject.SetActive(true);
        instance.ShowEvenWithoutImages();
    }

    /// <summary>
    /// Appends a statistics line of an analysis to the result report.
    /// </summary>
    /// <param name="experiment">Experiment label.</param>
    /// <param name="mean">Mean error.</param>
    /// <param name="std">Standard deviation.</param>
    /// <param name="min">Minimum.</param>
    /// <param name="max">Maximum.</param>
    public static void AddAnalysisResult(string experiment, float mean, float std, float min, float max)
    {
        if (instance == null)
            return;

        string row = experiment + "\n  mean: " + mean.ToString("G5")
            + "  std: " + std.ToString("G5")
            + "\n  min: " + min.ToString("G5")
            + "  max: " + max.ToString("G5");

        if (instance.resultsText.text.StartsWith("Noch keine"))
            instance.resultsText.text = row;
        else
            instance.resultsText.text += "\n\n" + row;
    }

    /// <summary>
    /// Replaces the result report of the gallery.
    /// </summary>
    /// <param name="value">New report text.</param>
    public static void SetResultsText(string value)
    {
        if (instance != null)
            instance.resultsText.text = value;
    }

    //28092026 Hinweis (z. B. Plot-Fehler) an den Ergebnistext anhaengen
    /// <summary>
    /// Appends a line to the result report (e.g. a plotting error).
    /// </summary>
    /// <param name="value">Text to append.</param>
    public static void AppendResultsText(string value)
    {
        if (instance != null)
            instance.resultsText.text += "\n" + value;
    }

    /// <summary>
    /// Shows the table and the plot of the last illumination, noise, or speckle study.
    /// </summary>
    /// <param name="analysis">Study: lighting, noise, or speckle.</param>
    public static void LoadPreviousAnalysis(string analysis)
    {
        if (instance == null)
            return;

        string fileName;
        switch (analysis)
        {
            case "lighting": fileName = "lighting_sweep.tsv"; break;
            case "noise": fileName = "noise_sweep.tsv"; break;
            case "speckle": fileName = "speckle_sweep.tsv"; break;
            default: throw new ArgumentException("Unknown analysis: " + analysis);
        }

        string resultPath = Path.Combine(Application.dataPath, "analysis_results", fileName);
        if (!File.Exists(resultPath))
        {
            instance.resultsText.text = "Keine gespeicherten Ergebnisse für " + analysis + " gefunden.";
            instance.openButton.gameObject.SetActive(true);
            instance.Show();
            return;
        }

        instance.ClearImages();
        string[] lines = File.ReadAllLines(resultPath);
        var experiments = new List<string>();
        //28092026 Statistik ueber den Spaltenkopf finden (neue Tabellen haben Zusatzspalten hinten);
        //  alte Tabellen ohne "mean_v_error" im Kopf: wie bisher die letzten vier Spalten
        int meanColumn = lines.Length > 0 ? Array.IndexOf(lines[0].Split('\t'), "mean_v_error") : -1;
        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            string[] columns = lines[lineIndex].Split('\t');
            if (columns.Length < 5 || string.IsNullOrWhiteSpace(columns[0]))
                continue;
            int statisticsStart = meanColumn >= 0 && meanColumn + 3 < columns.Length ? meanColumn : columns.Length - 4;
            if (!float.TryParse(columns[statisticsStart], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float mean)
                || !float.TryParse(columns[statisticsStart + 1], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float std)
                || !float.TryParse(columns[statisticsStart + 2], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float min)
                || !float.TryParse(columns[statisticsStart + 3], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float max))
                continue;
            experiments.Add(columns[0]);
            AddAnalysisResult(columns[0], mean, std, min, max);
        }

        foreach (string experiment in experiments)
        {
            // construct_blade_path() historically concatenates Application.dataPath
            // and a dot-free experiment name, e.g. Assets + speckle_0035.
            string experimentDirectory = Application.dataPath + experiment.Replace(".", "");
            if (!Directory.Exists(experimentDirectory))
                continue;
            var imagePaths = new List<string>(Directory.GetFiles(experimentDirectory, "*.png",
                SearchOption.AllDirectories));
            imagePaths.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string imagePath in imagePaths)
                instance.LoadImage(imagePath);
        }

        if (experiments.Count == 0)
            instance.resultsText.text = "Die gespeicherte Ergebnistabelle enthält keine gültigen Zeilen.";
        instance.openButton.gameObject.SetActive(true);
        instance.Show();

        //28092026 Diagramm (wie Abb. 5/6 im Paper) erzeugen und vorne in die Galerie setzen
        vis_3D vis = Vis();
        if (vis != null && experiments.Count > 0)
            _ = vis.show_sweep_plot(analysis);
    }

    /// <summary>
    /// Loads an image file into the gallery.
    /// </summary>
    /// <param name="path">Path of the image file.</param>
    /// <param name="insertAt">Insert position, or -1 to append.</param>
    private void LoadImage(string path, int insertAt = -1)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Destroy(texture);
            Debug.LogWarning("Could not display rendered image: " + path);
            return;
        }

        //21092026 Standard-WrapMode "Repeat" filtert am linken Rand die rechte Spalte
        //(Farbbalken) hinein -> sichtbarer Gradient in der ersten Pixelspalte.
        texture.wrapMode = TextureWrapMode.Clamp;
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), 100f);
        if (insertAt >= 0 && insertAt <= sprites.Count)
        {
            textures.Insert(insertAt, texture);
            sprites.Insert(insertAt, sprite);
            paths.Insert(insertAt, path);
            currentIndex = insertAt == 0 ? 0 : currentIndex; // Anzeige auf dem ersten Panel lassen
        }
        else
        {
            textures.Add(texture);
            sprites.Add(sprite);
            paths.Add(path);
            currentIndex = sprites.Count - 1;
        }
        openButton.gameObject.SetActive(true);
        UpdateDisplay();
    }

    /// <summary>
    /// Removes all images and releases their textures.
    /// </summary>
    private void ClearImages()
    {
        foreach (Sprite sprite in sprites)
            Destroy(sprite);
        foreach (Texture2D texture in textures)
            Destroy(texture);

        sprites.Clear();
        textures.Clear();
        paths.Clear();
        resultsText.text = "Noch keine Analyse ausgeführt.";
        currentIndex = 0;
        image.sprite = null;
        ResetZoom();
        openButton.gameObject.SetActive(false);
        window.SetActive(false);
    }

    /// <summary>
    /// Shows the previous image that passes the camera filter.
    /// </summary>
    private void Previous()
    {
        if (sprites.Count == 0) return;
        for (int step = 0; step < sprites.Count; step++)
        {
            currentIndex = (currentIndex - 1 + sprites.Count) % sprites.Count;
            if (MatchesFilter(paths[currentIndex]))
                break;
        }
        UpdateDisplay();
    }

    /// <summary>
    /// Shows the next image that passes the camera filter.
    /// </summary>
    private void Next()
    {
        if (sprites.Count == 0) return;
        for (int step = 0; step < sprites.Count; step++)
        {
            currentIndex = (currentIndex + 1) % sprites.Count;
            if (MatchesFilter(paths[currentIndex]))
                break;
        }
        UpdateDisplay();
    }

    /// <summary>
    /// Opens the gallery window.
    /// </summary>
    private void Show()
    {
        window.SetActive(true);
        window.transform.SetAsLastSibling();
        UpdateDisplay();
    }

    /// <summary>
    /// Opens the gallery window even if it contains no images.
    /// </summary>
    private void ShowEvenWithoutImages()
    {
        window.SetActive(true);
        window.transform.SetAsLastSibling();
        openButton.gameObject.SetActive(true);
        if (sprites.Count > 0)
            UpdateDisplay();
    }

    /// <summary>
    /// Closes the gallery window and shows the gallery button.
    /// </summary>
    private void Hide()
    {
        window.SetActive(false);
        openButton.gameObject.SetActive(true);
        openButton.transform.SetAsLastSibling();
    }

    /// <summary>
    /// Shows the current image with counter, title, and illumination label.
    /// </summary>
    private void UpdateDisplay()
    {
        if (sprites.Count == 0) return;
        image.sprite = sprites[currentIndex];
        counter.text = cameraFilter == "alle"
            ? (currentIndex + 1) + " / " + sprites.Count
            : IndexAmongMatching(currentIndex) + " / " + CountMatching() + "  (" + cameraFilter + ")";
        title.text = Path.GetFileName(paths[currentIndex]);
        lightingLabel.text = GetLightingLabel(paths[currentIndex]);
        openButton.GetComponentInChildren<Text>().text = "Bilder (" + sprites.Count + ")";
    }

    /// <summary>
    /// Derives a readable description of the experiment (illumination, shot noise, speckle size) from the folder of an image.
    /// </summary>
    /// <param name="path">Path of the image.</param>
    /// <returns>Description for the title line.</returns>
    private static string GetLightingLabel(string path)
    {
        DirectoryInfo directory = new FileInfo(path).Directory;
        while (directory != null)
        {
            if (directory.Name == "Assetslighting_01")
                return "Beleuchtung: 0,1";
            if (directory.Name == "Assetslighting_1")
                return "Beleuchtung: 1,0";
            if (directory.Name == "Assetslighting_8")
                return "Beleuchtung: 8,0";
            //28092026 neue Labels aus der Liste: lighting_0p035 -> 0.035
            if (directory.Name.StartsWith("Assetslighting_") && directory.Name.Contains("p")
                || System.Text.RegularExpressions.Regex.IsMatch(directory.Name, @"^Assetslighting_[1-9]\d*$"))
                return "Beleuchtung: " + directory.Name.Substring("Assetslighting_".Length).Replace('p', '.');
            if (directory.Name == "Assetsshot_clean")
                return "Schrotrauschen: aus (Nmax = ∞ e⁻)";
            if (directory.Name.StartsWith("Assetsshot_e"))
                return "Schrotrauschen: Nmax = "
                    + directory.Name.Substring("Assetsshot_e".Length).Replace('p', '.') + " e⁻";
            if (directory.Name.StartsWith("Assetsspeckle_"))
            {
                string compact = directory.Name.Substring("Assetsspeckle_".Length);
                //29092026 prozedurale Stufen: speckle_p<s> (s in Texturpixeln, p statt Punkt)
                if (compact.StartsWith("p"))
                    return "Speckle: prozedural, Kreisdurchmesser " + compact.Substring(1).Replace('p', '.') + " Texturpixel";
                if (compact.Length > 3)
                    compact = compact.Insert(compact.Length - 3, ".");
                return "Speckle-Größe: " + compact;
            }

            directory = directory.Parent;
        }

        return "Beleuchtung: normales Experiment";
    }

    /// <summary>
    /// Creates a coloured UI panel.
    /// </summary>
    /// <param name="name">Object name.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="color">Background colour.</param>
    /// <returns>The panel object.</returns>
    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    /// <summary>
    /// Creates a button with a centred label.
    /// </summary>
    /// <param name="name">Object name.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="label">Button text.</param>
    /// <param name="font">Font of the text.</param>
    /// <returns>The button.</returns>
    private static Button CreateButton(string name, Transform parent, string label, Font font)
    {
        GameObject buttonObject = CreatePanel(name, parent, new Color(0.88f, 0.88f, 0.88f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        Text text = CreateText("text", buttonObject.transform, label, font, 20, TextAnchor.MiddleCenter);
        text.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    /// <summary>
    /// Creates a single-line input field.
    /// </summary>
    /// <param name="name">Object name.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="value">Initial text.</param>
    /// <param name="font">Font of the text.</param>
    /// <returns>The input field.</returns>
    private static InputField CreateInputField(string name, Transform parent, string value, Font font)
    {
        GameObject inputObject = CreatePanel(name, parent, new Color(0.96f, 0.96f, 0.96f, 1f));
        InputField input = inputObject.AddComponent<InputField>();
        Text text = CreateText("text", inputObject.transform, value, font, 17, TextAnchor.MiddleLeft);
        text.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(8f, 0f);
        text.rectTransform.offsetMax = new Vector2(-8f, 0f);
        input.textComponent = text;
        input.text = value;
        input.lineType = InputField.LineType.SingleLine;
        return input;
    }

    /// <summary>
    /// Creates a text element.
    /// </summary>
    /// <param name="name">Object name.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="value">Text.</param>
    /// <param name="font">Font.</param>
    /// <param name="fontSize">Font size.</param>
    /// <param name="alignment">Text alignment.</param>
    /// <returns>The text component.</returns>
    private static Text CreateText(string name, Transform parent, string value, Font font,
        int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = value;
        return text;
    }

    /// <summary>
    /// Sets anchor, pivot, position, and size of a UI element (anchor and pivot are the same point).
    /// </summary>
    /// <param name="rect">Element to place.</param>
    /// <param name="position">Anchored position.</param>
    /// <param name="size">Size.</param>
    /// <param name="anchor">Anchor and pivot (e.g. (1,1) = top right).</param>
    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
