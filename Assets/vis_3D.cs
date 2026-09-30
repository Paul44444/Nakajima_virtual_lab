using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Contracts;
//using System.Windows.Controls.Image;
//using System.Windows.Media.Imaging;
//using System.Drawing.Image;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.VisualScripting;
//using UnityEditor.Build.Player;
//using UnityEditor.Experimental.GraphView;
//using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using UnityEngine.XR;
using static System.Net.Mime.MediaTypeNames;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using static UnityEditor.PlayerSettings;
using static UnityEditor.ShaderData;

//using static UnityEditor.AddressableAssets.Build.BuildPipelineTasks.GenerateLocationListsTask;

//using static UnityEditor.PlayerSettings;
//using static UnityEditor.Progress;
//using static UnityEditor.ShaderData;
using static UnityEngine.Random;
//using static UnityEngine.UIElements.UxmlAttributeDescription;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Stopwatch = System.Diagnostics.Stopwatch;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class vis_3D : MonoBehaviour
{
    Vector2[] uv_start;

    GameObject canvas;
    Transform log_field;
    Cam_manager cam_script;
    Cam_manager cam_exp;
    Camera cam_for_uv_0;
    Camera cam_for_uv_1;
    Match_steps match_control;
    T_control_script t_control;
    Strain strain_control;
    U_V u_v_control;
    Experiment_Control experiment_control;
    Strain_D strain_d;

    bool force_flat = true;
    int test_1 = 1 + 1;

    string path_stereo = null;
    string path_time_flow_u = null;// info (paul): this will be overwritten anyway, see init_params()
    string path_time_flow_v = null;

    // info (paul): This will be overwritten in the init_params() function; for more params also look at init_params()
    int res_x = 1024;
    int res_y = 1024;
    int im_cnt = 20;
    int t_idx = -1;

    int blade_idx_max = 98;

    // info (paul): "normal": no derivative
    //      "derivative_1": first derivative
    string strain_mode = "normal";

    // info (paul): Whether to plot u or t
    string u_v_mode = "v";//"u";

    // --- TV Progress and ETA tracking ---
    private GameObject progress_panel;
    private RectTransform progress_bar_fill;
    private TextMeshProUGUI progress_status_text;
    private TextMeshProUGUI progress_eta_text;
    private Stopwatch progress_stopwatch = new Stopwatch();
    private double total_work_units = 1.0;
    private double completed_work_units = 0.0;
    private volatile int series_current_idx = 1;
    private volatile int series_total_count = 1;
    private volatile int status_scale_display = 1;
    private volatile int status_total_scales = 1;
    private volatile int status_current_warp = 0;
    private volatile int status_total_warps = 1;
    private volatile int status_current_it = 0;
    private volatile int status_max_it = 300;
    private volatile int status_nx = 0;
    private volatile int status_ny = 0;
    private volatile bool is_tv_running = false;
    private volatile bool is_tv_finished = false;
    private bool cv_action_running = false;

    /// <summary>
    /// Looks up the progress panel of the scene (bar and text) used for long computations.
    /// </summary>
    public void init_progress_ui()
    {
        if (canvas == null)
            canvas = GameObject.Find("Canvas");

        if (canvas != null)
        {
            Transform pPanel = canvas.transform.Find("Progress_Panel");
            if (pPanel != null)
            {
                progress_panel = pPanel.gameObject;
                Transform bg = pPanel.Find("Bar_Background");
                if (bg != null)
                {
                    Transform fill = bg.Find("Bar_Fill");
                    if (fill != null)
                        progress_bar_fill = fill.GetComponent<RectTransform>();
                }
                Transform st = pPanel.Find("Status_Text");
                if (st != null)
                    progress_status_text = st.GetComponent<TextMeshProUGUI>();

                Transform eta = pPanel.Find("ETA_Text");
                if (eta != null)
                    progress_eta_text = eta.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    /// <summary>
    /// Sets the position within a series of computations (e.g. a stage of a sweep) for the progress display and starts the stopwatch at the first item.
    /// </summary>
    /// <param name="current_idx">Index of the current item (1-based).</param>
    /// <param name="total_count">Number of items.</param>
    public void set_series_progress_info(int current_idx, int total_count)
    {
        series_current_idx = Mathf.Max(1, current_idx);
        series_total_count = Mathf.Max(1, total_count);
        if (current_idx <= 1 && !progress_stopwatch.IsRunning)
        {
            progress_stopwatch.Restart();
        }
    }

    /// <summary>
    /// Initialises the progress estimate of one optical-flow computation from the pyramid sizes, warps, and iterations.
    /// </summary>
    /// <param name="nscales">Number of pyramid levels.</param>
    /// <param name="nx">Width of each level.</param>
    /// <param name="ny">Height of each level.</param>
    /// <param name="warps">Warps per level.</param>
    /// <param name="max_iterations">Maximum iterations per warp.</param>
    public void start_tv_progress(int nscales, List<int> nx, List<int> ny, int warps, int max_iterations)
    {
        is_tv_running = true;
        is_tv_finished = false;
        total_work_units = 0.0;
        for (int s = 0; s < nscales; s++)
        {
            double scale_pixels = (double)nx[s] * (double)ny[s];
            total_work_units += (double)warps * (double)max_iterations * scale_pixels;
        }
        if (total_work_units <= 0.0) total_work_units = 1.0;

        completed_work_units = 0.0;
        status_total_scales = nscales;
        status_total_warps = warps;
        status_max_it = max_iterations;
        if (!progress_stopwatch.IsRunning)
        {
            progress_stopwatch.Restart();
        }
    }

    /// <summary>
    /// Advances the progress estimate by one iteration and stores the status for the display.
    /// </summary>
    /// <param name="s">Current pyramid level.</param>
    /// <param name="nscales">Number of levels.</param>
    /// <param name="current_warp">Current warp.</param>
    /// <param name="total_warps">Warps per level.</param>
    /// <param name="current_it">Current iteration.</param>
    /// <param name="max_it">Maximum iterations.</param>
    /// <param name="nx">Width of the level.</param>
    /// <param name="ny">Height of the level.</param>
    public void update_tv_progress_step(int s, int nscales, int current_warp, int total_warps, int current_it, int max_it, int nx, int ny)
    {
        double pixel_weight = (double)nx * (double)ny;
        completed_work_units += pixel_weight;

        status_scale_display = nscales - s;
        status_total_scales = nscales;
        status_current_warp = current_warp;
        status_total_warps = total_warps;
        status_current_it = current_it;
        status_max_it = max_it;
        status_nx = nx;
        status_ny = ny;
    }

    /// <summary>
    /// Updates the progress bar and text on the main thread (called from Update); also opens the gallery when a background computation has requested it.
    /// </summary>
    public void render_progress_ui_main_thread()
    {
        if (gallery_show_pending)
        {
            gallery_show_pending = false;
            ExperimentImageGallery.ShowWhenFinished();
        }
        if (progress_panel == null)
        {
            init_progress_ui();
        }

        if (!is_tv_running && !is_tv_finished)
        {
            return;
        }

        if (progress_panel != null && !progress_panel.activeSelf)
        {
            progress_panel.SetActive(true);
        }

        if (is_tv_finished)
        {
            if (progress_bar_fill != null)
                progress_bar_fill.sizeDelta = new Vector2(520f, progress_bar_fill.sizeDelta.y);

            double total_sec = progress_stopwatch.ElapsedMilliseconds / 1000.0;
            string total_str = FormatTimeSpan(total_sec);

            if (progress_status_text != null)
            {
                if (series_total_count > 1)
                    progress_status_text.text = $"TV Optical Flow: Alle {series_total_count} Bilder abgeschlossen!";
                else
                    progress_status_text.text = "TV Optical Flow: Abgeschlossen!";
            }

            if (progress_eta_text != null)
            {
                progress_eta_text.text = $"Fortschritt: 100% | Gesamtzeit: {total_str} | Fertig";
            }
            return;
        }

        double single_progress = completed_work_units / total_work_units;
        if (single_progress > 1.0) single_progress = 1.0;
        if (single_progress < 0.0) single_progress = 0.0;

        double overall_progress = ((double)(series_current_idx - 1) + single_progress) / (double)series_total_count;
        if (overall_progress > 1.0) overall_progress = 1.0;
        if (overall_progress < 0.0) overall_progress = 0.0;

        if (progress_bar_fill != null)
        {
            progress_bar_fill.sizeDelta = new Vector2((float)(520.0 * overall_progress), progress_bar_fill.sizeDelta.y);
        }

        if (progress_status_text != null)
        {
            string img_prefix = (series_total_count > 1) ? $"[Bild {series_current_idx}/{series_total_count}] " : "";
            if (progress_prefix != "")
                img_prefix = progress_prefix; //27092026 z.B. "[Parameterstudie 12/35: theta] "
            progress_status_text.text = $"{img_prefix}TV Flow | Scale {status_scale_display}/{status_total_scales} ({status_nx}x{status_ny}) | Warp {status_current_warp + 1}/{status_total_warps} | Iteration {status_current_it}/{status_max_it}";
        }

        if (progress_eta_text != null)
        {
            long elapsed_ms = progress_stopwatch.ElapsedMilliseconds;
            double elapsed_sec = elapsed_ms / 1000.0;
            string elapsed_str = FormatTimeSpan(elapsed_sec);

            string eta_str = "--:--";
            if (overall_progress > 0.005)
            {
                double remaining_sec = (elapsed_sec / overall_progress) * (1.0 - overall_progress);
                eta_str = FormatTimeSpan(remaining_sec);
            }

            int pct = (int)(overall_progress * 100.0);
            string img_info = (series_total_count > 1) ? $" (Bild {series_current_idx}/{series_total_count})" : "";
            if (progress_prefix != "")
                img_info = $" ({progress_kind}, Lauf {series_current_idx}/{series_total_count})"; //27092026, 29092026 progress_kind
            progress_eta_text.text = $"Fortschritt: {pct}%{img_info} | Vergangen: {elapsed_str} | Verbleibend (ETA): ~{eta_str}";
        }
    }

    /// <summary>
    /// Finishes the progress display when the last item of a series is done.
    /// </summary>
    public void finish_tv_progress()
    {
        if (series_current_idx >= series_total_count)
        {
            finish_all_tv_progress();
        }
    }

    /// <summary>
    /// Stops the progress display; UI actions are deferred to the main thread because this may be called from the flow thread.
    /// </summary>
    public void finish_all_tv_progress()
    {
        progress_stopwatch.Stop();
        is_tv_finished = true;
        is_tv_running = false;

        //20092026 Bugfix: diese Funktion wird aus dem TV-Hintergrund-Thread (Task.Run in
        //cv_main_async -> compute_ims -> Dual_TVL1_optic_flow_multiscale) aufgerufen.
        //UI-Zugriffe (Galerie oeffnen) sind nur im Hauptthread erlaubt -> nur Flag setzen,
        //render_progress_ui_main_thread() (aus Update) oeffnet die Galerie dann.
        if (get_with_exp() && !analysis_sweep_running())
            gallery_show_pending = true;
    }
    private volatile bool gallery_show_pending = false;
    //27092026 Praefix der Fortschrittsanzeige waehrend der Parameterstudie ("" = normal)
    private string progress_prefix = "";
    //29092026 Nutzerwunsch: Gesamtfortschritt auch bei Licht/Rauschen/Speckle-Sweeps und der Hoehenanalyse
    //  (vorher begann der Balken je Stufe neu). Art des Laufs fuer die ETA-Zeile.
    private string progress_kind = "Parameterstudie";

    /// <summary>
    /// Shows the overall progress of a study (e.g. stage 3 of 27) in front of the flow progress.
    /// </summary>
    /// <param name="current">Current stage (1-based).</param>
    /// <param name="total">Number of stages.</param>
    /// <param name="kind">Kind of study shown in the label.</param>
    /// <param name="label">Label of the current stage.</param>
    void set_overall_progress(int current, int total, string kind, string label)
    {
        progress_kind = kind;
        set_series_progress_info(current, total);
        progress_prefix = "[" + kind + " " + current + "/" + total + ": " + label + "] ";
    }

    /// <summary>
    /// Clears the overall-progress prefix after a study.
    /// </summary>
    void reset_overall_progress()
    {
        progress_prefix = "";
        progress_kind = "Parameterstudie";
        series_current_idx = 1;
        series_total_count = 1;
    }

    /// <summary>
    /// Formats a duration as mm:ss or hh:mm:ss.
    /// </summary>
    /// <param name="seconds">Duration in seconds.</param>
    /// <returns>Formatted duration, or --:-- if unknown.</returns>
    private string FormatTimeSpan(double seconds)
    {
        if (double.IsInfinity(seconds) || double.IsNaN(seconds) || seconds < 0) return "--:--";
        int totalSec = (int)seconds;
        int min = totalSec / 60;
        int sec = totalSec % 60;
        int hrs = min / 60;
        min = min % 60;

        if (hrs > 0)
            return $"{hrs:D2}:{min:D2}:{sec:D2}";
        else
            return $"{min:D2}:{sec:D2}";
    }


    // info (paul): whether to take the derivative in x or y direction
    string strain_d_mode = "x";

    bool is_visible = false;
    bool ready_for_next_blade = true;
    private float pic_timer = 0f; // info (paul): value will be changed over time
    float pic_time = 0.5f;

    // info (paul): the index for the "current" blade
    int blade_idx_secret;
    private bool blades_created = false; // info (paul): whether all blades were created successfully
    public bool with_match_tex;

    // info (paul): blades_pos is the central position for the blades rendering
    Vector3 blades_pos;
    int match_steps = -1;

    bool with_main = false;

    // info (paul): whether to include experiments, rendering etc.
    bool with_exp = false;//true;
    bool is_started = false;

    private int dt_compare = 4;//4;//27092024 1;//6;//03092024 1;

    bool done_normal_exps = false;
    bool done_normal_ims = false;
    bool done_render_acts = false;
    bool done_speckle_ims = false;
    bool done_ground_truth = false;

    List<GameObject> blades;
    List<int[]> blade_tris;

    // info (paul): camera parameters for the validation
    private float field_of_view = -1f;
    float field_of_view_heights = -1f;

    // info (paul): If you change that, you should also manually change the size []x[] of
    //      "cam_tex_0" and "cam_tex_1" under Assets/Resources/Targets/fbx_files/cam_tex_0
    int render_res = 512;

    // info (paul): scale factor for the flow
    float flow_scale_factor = 1.0f;

    // info (paul): plot_mode: possible values: 
    string plot_mode = "loss_rel";
    string heights_mode = "value";
    string paint_with = "uv";

    float cam_angle = -1f;
    float cam_angle_0 = float.NaN;
    float cam_angle_1 = float.NaN;
    string experiment = "exp_normal";
    public Camera cam_0;
    public Camera cam_1;

    public float uv_scale = float.NaN; // info (paul): uv_scale > 1f makes more smaller speckles

    //18092026 Nutzerwunsch: fuer den "Realbild: Neu"-Vergleich soll statt der aus
    //verts_1.txt generierten Referenzprobe die neue, korrekt schmale FBX-Probe
    //(nakajima_50_fbx.fbx) verwendet werden. Skalierungsfaktor hier zentral anpassbar,
    //falls die Probe zu gross/klein erscheint.
    //18092026 (2) Nutzerwunsch: FBX-Probe war ca. Faktor 400 zu klein -> Skalierung
    //entsprechend erhoeht (vorher 1f).
    //18092026 (3) Nutzerwunsch: nochmal Faktor 3 groesser (400 * 3 = 1200).
    public float nakajima_fbx_blade_scale = float.NaN;

    //20092026 Nutzerwunsch: zusaetzliche Drehung der FBX-Probe um ihre eigene
    //(lokale) Z-Achse. Analog zu nakajima_fbx_blade_scale nur als NaN-Platzhalter
    //deklariert und der eigentliche Wert in Start() zugewiesen - Zuweisungen
    //ausserhalb einer Funktion werden von Unity manchmal ignoriert.
    public float nakajima_fbx_blade_local_z_rotation = float.NaN;
    //21092026 Nutzerwunsch: In-Plane-Drehung der Nakajima-Probe (um ihre Flaechennormale =
    //lokale y-Achse des FBX, "vertikale Achse" im Bild), in Grad. Wird nach allen anderen
    //Rotationen angewendet; die verts-Proben im Nakajima-Look folgen automatisch.
    public float nakajima_fbx_blade_in_plane_rotation = float.NaN;

    // info (paul): the actions of what is rendered
    public List<Actioner> render_acts;
    bool ready_for_next_act = true;
    int render_idx = 0;

    // info (paul): for making the speckle patterns; (action; speckle_size; speckle_dist)
    public List<(Action, float, float)> speckle_acts;

    bool with_print_paths = false;

    float speckle_size = -1f;
    float speckle_dist = -1f;
    float plane_side_len = 10f;

    GameObject speckle_parent;

    Material speckle_mat;
    List<Actioner> exp_cv_acts;
    int cam_idx_for_pic = 0;
    bool lighting_sweep_running = false;
    bool noise_sweep_running = false;
    bool speckle_sweep_running = false;
    int noise_sample_index = 0;

    /// <summary>
    /// Checks whether an illumination, noise, or speckle sweep is running.
    /// </summary>
    /// <returns>True during a sweep.</returns>
    private bool analysis_sweep_running()
    {
        return lighting_sweep_running || noise_sweep_running || speckle_sweep_running;
    }

    string category = null;
    string category_muc = null;
    List<int> blade_idxs = null;
    List<int> our_blade_idxs = null;
    bool with_our_idxs = false;
    bool use_ncorr;
    string path_base;
    public string path_dic;
    string root_path;
    string path_project;

    float v_mean_global;
    float v_std_global;
    float v_min_global;
    float v_max_global;

    float fov_for_heights = -1f; // 27112024: We assume, that for heights calculations, our fov is always 40, perhaps make that 
                                 //      mode dynamic one day

    bool ground_truth_from_flow = false;
    float heights_fov = -1f;
    float default_dist = -1f;

    List<string> im_paths;
    string in_dir;
    string out_dir;

    Transform explorer;

    // info (paul): variable for fast computing aborting
    bool break_now = false;
    List<float> strain_x;
    List<float> strain_y;

    // info (paul): The config object, which is currently written to;
    //          Meaning: The user pushes buttons etc. in order to set lighting and so on, 
    //          and these values are then written into the "config_now" properties.
    ExpConfig config_now;
    Params params_now;

    // info (paul): The layer, where symbols are located
    int symbols_layer;

    string blade_path;

    List<List<float>> truth_0_u;
    List<List<float>> truth_0_v;
    List<List<float>> truth_1_u;
    List<List<float>> truth_1_v;

    public float truth_map_scale;
    bool series = false;
    int series_idx = 0;

    // Start is called before the first frame update
    /// <summary>
    /// Unity callback: initialises the lab - paths, cameras, lights, sample meshes, speckle settings, saved preferences, the user interface (gallery and control panel), and the scene panels.
    /// </summary>
    void Start()
    {
        main_thread_id = System.Threading.Thread.CurrentThread.ManagedThreadId; //23092026 fuer run_on_main_thread
        tv_gpu_shutdown = false;
        symbols_layer = 6;
        category = "";//12122025 "muc";
        category_muc = "simple";//"simple";//"gom_curve"; // or "simple" might be nice
        speckle_file = "speckle_0.070";

        break_now = false;
        with_main = false;
        with_exp = false;
        is_started = false;
        series = false;
        series_idx = 0;

        ground_truth_from_flow = true;//17122024

        use_ncorr = false;
        fov_for_heights = 40f;//17122024 20f;//04122024 40f;
        heights_fov = fov_for_heights;//04122024 40f
        //18092026 Kamera-Abstand um Faktor 5 vergroessert, passend zum Faktor-5-Scale der
        //Probe (siehe load_blade_from_verts / category != "muc"). Damit bleibt der
        //Blickwinkel auf die Probe (Verhaeltnis Objektgroesse/Kameraabstand) exakt
        //gleich wie vorher, nur ist jetzt die ganze (jetzt 5x groessere) Probe im Bild.
        //18092026 (2) Nutzerwunsch: nochmal x2.5 weiter weg (1500 -> 3750).
        //18092026 (3) Nutzerwunsch: 30% naeher dran (3750 * 0.7 = 2625).
        default_dist = 2625f;
        if (category == "muc")
        {
            fov_for_heights = 5f;
            heights_fov = fov_for_heights;
            default_dist = 300f;//18032025 900f;
        }
        else
        {
            this.nakajima_fbx_blade_scale = 3f;
            //20092026 Nutzerwunsch: Probe zusaetzlich um 90 Grad um ihre eigene
            //lokale Z-Achse drehen.
            this.nakajima_fbx_blade_local_z_rotation = -90f;
            //22092026 In-Plane-Drehung (um die Flaechennormale), belegt an den Rohdaten:
            //Die Probe ist in lokal x eingeschnuert (219 -> 69.7 an der Taille), ihre
            //Laengsachse ist lokal y. Die Bild-x-Achse liegt auf lokal x
            //(corr(d_x_screen, lokal x) = 0.61, mit lokal y = 0), d.h. ohne Drehung zeigt u
            //die QUERdehnung, und die Laengsdehnung verschwindet in v unter der Kuppelhoehe,
            //die die geneigte Kamera (cam_angle 10 Grad um die globale X-Achse) einseitig
            //in Bild-y projiziert (d_y = -14.15 .. +0.06).
            //Mit 90 Grad liegt die Laengsachse horizontal: u = Laengsrichtung (symmetrischer
            //+/- Split links/rechts, Kerben oben/unten), v = Querrichtung + Kuppel-Parallaxe.
            this.nakajima_fbx_blade_in_plane_rotation = 90f;
        }    
        uv_scale = 1f;//24112024 1f; //07112024 5f;//30102024 5f;
        //24092026 Nutzerwunsch: Frame 28 -> 29 (aus den neuen STL-Exporten; vorher 27 -> 28, 28 -> 29,
        //  27 -> 29, 10 -> 15, 95 -> 96, 60 -> 70, 50 -> 99, davor 3 -> 15); drittes Element weiterhin
        //  nur Platzhalter fuer with_dt
        blade_idxs = new List<int>() {28, 29, 28}; //24092026 {27, 28, 27}; {28, 29, 28}; {27, 29, 27}; //23092026 {10, 15, 10}; {95, 96, 95}; {60, 70, 60}; {50, 99, 50}; {3, 15, 3}; //13072025 15, 23, 23 };//{15, 25, 27, 28};//{15, 25, 27};//15032025 {15, 16, 17};//25022025 { 1, 20, 28 };//16122024 { 8, 9, 10};//12122024 //11112024 {1, 20, 28};//04112024 {1, 20, 28};//30102024 { 1, 20, 30, 98};//, 98 };//, 92, 93, 94 };
        //30112024A blade_idxs = new List<int>() { 28, 29};
        our_blade_idxs = new List<int>() { 80, 90 };
        //24092026 Nutzerwunsch: Frames im Programm waehlbar (Feld "Frames"); gespeicherte Wahl hat Vorrang
        string frames_pref = PlayerPrefs.GetString("frames", "");
        if (frames_pref != "")
        {
            string frames_msg;
            if (!try_parse_frames(frames_pref, out List<int> frames_l, out frames_msg))
                Debug.LogWarning("Gespeicherte Frames ungueltig ('" + frames_pref + "'): " + frames_msg);
            else
                blade_idxs = frames_to_blade_idxs(frames_l);
        }
        Debug.Log("Frames: " + describe_frames());
        speckle_mat = (Material)Resources.Load("Targets/speckle_mat");

        speckle_acts = new List<(Action, float, float)>();
        field_of_view_heights = 40f;//06122024 
        //18092026 Nutzerwunsch: FOV der Kameras von 30 auf 10 reduziert.
        //21092026 Nutzerwunsch: 50 % weiter rausgezoomt (sichtbarer Ausschnitt x1.5):
        //2*atan(1.5*tan(2°)) = 6.0°. Gleicher Wert wie das FOV-Feld von "Realbild: Neu".
        set_field_of_view(6);//20092026 vorher 30, dann 10, dann -20% -> 8, dann 5, dann 4
        im_paths = new List<string>();

        strain_x = new List<float>();
        strain_y = new List<float>();

        canvas = GameObject.Find("Canvas");
        ExperimentImageGallery.EnsureCreated(canvas.transform);

        truth_map_scale = 1f;

        //23092026 Pfade rechnerunabhängig: path_project = Ordner über Assets; path_base optional aus
        //  <Projekt>/path_base.txt, sonst der alte Pfad (falls vorhanden), sonst der Ordner über dem Projekt.
        //  path_base = "/Users/Paul/Desktop/DIC_2025_for_travel/";
        path_project = System.IO.Path.GetFullPath(UnityEngine.Application.dataPath + "/..").Replace('\\', '/') + "/";
        path_base = resolve_path_base(path_project);
        path_dic = path_base + "DIC_package/";
        root_path = path_dic + "exp_normal/time_flow_v/nice_pics/";
        //23092026 path_project = "/Users/Paul/Desktop/DIC_2025_for_travel/unter2_Windows_native _october/";//14032025 "not_relevant";//13022024 "C:/Users/go73jem/unter2_Windows_native";

        //blade_path = "C:/Users/Paul/Desktop/DIC_2025_for_travel/play_blender_pycahrm/" +
        //    "write_mesh/" + "mucverts_29.txt";

        path_dic = UnityEngine.Application.dataPath;
        root_path = path_dic + "/analysis_results/";
        Directory.CreateDirectory(root_path);
        //03112025 blade_path = UnityEngine.Application.dataPath + "/verts/verts_30.txt";
        blade_path = UnityEngine.Application.dataPath + "/verts/verts_15.txt";//12122025 "/verts/mucverts_28.txt";

        string log_info = "path_dic: " + path_dic + "\n blade_path: " + blade_path;

        log_field = canvas.transform.Find("log_field");
        log_field.GetComponent<TextMeshProUGUI>().text = log_info;

        speckle_parent = GameObject.Find("speckle_parent");

        if (true)//20092024
        {
            manage_lighting_settings();

            with_match_tex = true;

            blades_pos = new Vector3(-5000f, 0f, -200f);//17062025 new Vector3(0f, 0f, -200f);
            if (category == "muc")
            {
                blades_pos = new Vector3(-5000f, 0f, -200f);//new Vector3(-760f, -200f, -240f);
            }
            init_params(blade_idxs);

            // info (paul): set up params
            explorer = canvas.transform.Find("explorer");
            explorer.gameObject.SetActive(false);
            log_field = canvas.transform.Find("log_field");
            TextMeshProUGUI tmpro = log_field.GetComponent<TextMeshProUGUI>();

            // info (paul): validation
            load_render_res_pref();
            load_cam_light();
            //24102024A string blade_path_first = blade_path_for_idx(blade_idx_min);

            render_acts = init_render_acts(render_acts);
            //25042024 GameObject surface_obj = start_renders(blade_path: blade_path_first, blade_idx: blade_idx_min, with_uv_init: true);

            set_ready_for_next_blade(true);
            set_blade_idx(-1);
            set_pic_timer(0f);
            //07122024 if (!with_exp)
            //07122024 {
            //07122024     done_normal_exps = true;
            //07122024     done_normal_ims = true;
            //07122024     done_render_acts = true;
            //07122024     done_speckle_ims = true;
            //07122024     done_ground_truth = true;
            //07122024     ready_for_next_act = false;
            //07122024     //22102024 set_blade_idx(blade_idx_max);
            //07122024 }
            //07122024 exp_cv_acts = set_up_render_list();
        }

        // info (paul): set up cam script
        GameObject cam_obj = GameObject.Find("MainCamera");
        GameObject cam_exp_obj = GameObject.Find("exp_cam");
        cam_script = cam_obj.GetComponent<Cam_manager>();
        cam_exp = cam_exp_obj.GetComponent<Cam_manager>();
        cam_script.do_start();

        Transform designer_obj = canvas.transform.Find("design_exp_panel");
        Designer designer = designer_obj.GetComponent<Designer>();
        designer.do_start();

        Transform choose_panel_obj = canvas.transform.Find("Choose_panel");
        Choose_panel choose_panel = choose_panel_obj.GetComponent<Choose_panel>();
        choose_panel.do_start();

        Transform series_panel_obj = canvas.transform.Find("series_panel");
        Series_panel series_panel = series_panel_obj.GetComponent<Series_panel>();
        series_panel.do_start();

        init_lab();
        config_now = new ExpConfig();
        params_now = new Params();
    }
    /// <summary>
    /// Returns the parameters of the experiment that is currently rendered or analysed.
    /// </summary>
    /// <returns>Current experiment parameters.</returns>
    public Params get_params_now()
    {
        return params_now;
    }
    /// <summary>
    /// Returns the display scale of the reference maps.
    /// </summary>
    /// <returns>Scale factor.</returns>
    public float get_truth_map_scale()
    {
        return this.truth_map_scale;
    }
    /// <summary>
    /// Sets the display scale of the reference maps.
    /// </summary>
    /// <param name="value">Scale factor.</param>
    public void set_truth_map_scale(float value)
    {
        this.truth_map_scale = value;
    }

    /// <summary>
    /// Places the decorative laboratory objects (table, camera models) around the sample.
    /// </summary>
    public void init_lab()
    {
        // info (paul): set up the nice theme for the virtual lab

        GameObject table_prefab = Resources.Load("Targets/table") as GameObject;
        GameObject cam_prefab = Resources.Load("Targets/cam") as GameObject;

        //30102025 Vector3 table_pos = new Vector3(blades_pos.x, blades_pos.y - 5f, blades_pos.z);//30102025 -5f //11f //-5f
        Vector3 table_pos = new Vector3(blades_pos.x - 47f, blades_pos.y - 5f, blades_pos.z + 45f);//30102025 -5f //11f //-5f
        GameObject table = Instantiate(table_prefab, table_pos, UnityEngine.Quaternion.identity);
        table.SetActive(false);//41 94

        GameObject bup_prefab = Resources.Load("Targets/exp_setup") as GameObject;
        Vector3 vec_1 = new Vector3(1f, 0f, 0f);//(0f, 1f, 0f);
        Vector3 vec_2 = new Vector3(-1f, 0f, 0f);//(-1f, 0f, 0f);
        Vector3 bup_pos = new Vector3(table_pos.x - 1600f, table_pos.y + 0f, table_pos.z + 300f);//30102025 + 0f
        GameObject bup = Instantiate(bup_prefab, bup_pos, UnityEngine.Quaternion.LookRotation(vec_1, vec_2));
        bup.transform.localScale = new Vector3(300f, 300f, 300f);
    }

    List<float> strain_xx_muc = new List<float>() { };
    List<float> strain_xy_muc = new List<float>() { };
    List<float> strain_yx_muc = new List<float>() { };
    List<float> strain_yy_muc = new List<float>() { };

    /// <summary>
    /// Evaluation sequence for the MUC test variant: computes and exports the reference strain maps.
    /// </summary>
    public void analyze_params_muc()
    {
        // info (paul): do the strain for ref
        set_strain_mode("derivative_1");
        set_plot_mode("value_ref");
        set_paint_with("uv");
        set_experiment("exp_normal");

        set_u_v_mode("u");//x
        set_strain_d_mode("x");
        refresh_plane_with_params();

        set_u_v_mode("u");
        set_strain_d_mode("y");
        refresh_plane_with_params();

        set_u_v_mode("v");//y
        set_strain_d_mode("x");
        refresh_plane_with_params();

        set_u_v_mode("v");
        set_strain_d_mode("y");
        refresh_plane_with_params();

        (List<float> minor_ref, List<float> major_ref) = minor_major(this.strain_xx_muc,
            this.strain_xy_muc, this.strain_yx_muc, this.strain_yy_muc);

        //19032025 write_gom(minor_ref, major_ref, title: "gom_ref.txt");

        // info (paul): for values
        set_strain_mode("derivative_1");
        set_plot_mode("value");
        set_paint_with("uv");
        set_experiment("exp_normal");

        set_u_v_mode("u");//x
        set_strain_d_mode("x");
        refresh_plane_with_params();

        set_u_v_mode("u");
        set_strain_d_mode("y");
        refresh_plane_with_params();

        set_u_v_mode("v");//y
        set_strain_d_mode("x");
        refresh_plane_with_params();

        set_u_v_mode("v");
        set_strain_d_mode("y");
        refresh_plane_with_params();

        (List<float> minor_vals, List<float> major_vals) = minor_major(this.strain_xx_muc,
            this.strain_xy_muc, this.strain_yx_muc, this.strain_yy_muc);

        List<float> minors = minor_vals;
        write_gom(minor_vals, major_vals, title: "gom_val.txt");
    }

    /// <summary>
    /// Linear blend of two lists.
    /// </summary>
    /// <param name="minor_ref">First list.</param>
    /// <param name="minor">Second list.</param>
    /// <param name="add_share">Weight of the second list (0..1).</param>
    /// <returns>Blended list.</returns>
    public List<float> blend_floats(List<float> minor_ref, List<float> minor, float add_share)
    {
        List<float> blend = new List<float>();

        for (int i = 0; i < minor.Count; i++)
        {
            blend.Add((1f - add_share) * minor_ref[i] + add_share * minor[i]);
        }

        return blend;
    }

    /// <summary>
    /// Returns camera 0 of the stereo rig.
    /// </summary>
    /// <returns>Camera 0.</returns>
    public Camera get_cam_for_uv_0()
    {
        return this.cam_for_uv_0;
    }
    /// <summary>
    /// Returns camera 1 of the stereo rig.
    /// </summary>
    /// <returns>Camera 1.</returns>
    public Camera get_cam_for_uv_1()
    {
        return this.cam_for_uv_1;
    }
    /// <summary>
    /// Stores the reference map of component u for camera 0.
    /// </summary>
    /// <param name="input">Reference map.</param>
    public void set_truth_0_u(List<List<float>> input)
    {
        this.truth_0_u = input;
    }
    /// <summary>
    /// Returns the reference map of component u for camera 0.
    /// </summary>
    /// <returns>Reference map.</returns>
    public List<List<float>> get_truth_0_u()
    {
        return truth_0_u;
    }
    /// <summary>
    /// Stores the reference map of component u for camera 1.
    /// </summary>
    /// <param name="input">Reference map.</param>
    public void set_truth_1_u(List<List<float>> input)
    {
        this.truth_1_u = input;
    }
    /// <summary>
    /// Returns the reference map of component u for camera 1.
    /// </summary>
    /// <returns>Reference map.</returns>
    public List<List<float>> get_truth_1_u()
    {
        return truth_1_u;
    }

    /// <summary>
    /// Stores the reference map of component v for camera 0.
    /// </summary>
    /// <param name="input">Reference map.</param>
    public void set_truth_0_v(List<List<float>> input)
    {
        this.truth_0_v = input;
    }
    /// <summary>
    /// Returns the reference map of component v for camera 0.
    /// </summary>
    /// <returns>Reference map.</returns>
    public List<List<float>> get_truth_0_v()
    {
        return this.truth_0_v;
    }
    /// <summary>
    /// Stores the reference map of component v for camera 1.
    /// </summary>
    /// <param name="input">Reference map.</param>
    public void set_truth_1_v(List<List<float>> input)
    {
        this.truth_1_v = input;
    }
    /// <summary>
    /// Returns the reference map of component v for camera 1.
    /// </summary>
    /// <returns>Reference map.</returns>
    public List<List<float>> get_truth_1_v()
    {
        return this.truth_1_v;
    }
    /// <summary>
    /// Returns the position at which the sample is placed.
    /// </summary>
    /// <returns>Sample position in world coordinates.</returns>
    public Vector3 get_blades_pos()
    {
        return this.blades_pos;
    }
    /// <summary>
    /// Computes minor and major principal strain from the components of the strain tensor.
    /// </summary>
    /// <param name="s_xx">Normal strain xx.</param>
    /// <param name="s_xy">Shear component xy.</param>
    /// <param name="s_yx">Shear component yx.</param>
    /// <param name="s_yy">Normal strain yy.</param>
    /// <returns>Tuple (minor strain, major strain) per point.</returns>
    public (List<float>, List<float>) minor_major(List<float> s_xx,
        List<float> s_xy, List<float> s_yx, List<float> s_yy)
    {
        // info (paul): calculate major and minor strain

        List<float> minor = new List<float>();
        List<float> major = new List<float>();

        for (int i = 0; i < s_xx.Count; i++)
        {
            float s_xx_i = s_xx[i];
            float s_xy_i = (s_xy[i] + s_yx[i]) / 2f;
            float s_yy_i = s_yy[i];
            float s_yx_i = s_yx[i];

            // info (paul): Die folgenden 2 Werte sind die Invarianten, nicht Hauptspannungen
            //15032025 float I_1 = s_xx_i + s_yy_i;
            //15032025 float I_2 = s_xx_i*s_yy_i + s_xy_i*s_xy_i;

            // info (paul): Jetzt kommen die Hauptspannungen 2d
            float sqrt_term = Mathf.Sqrt(Mathf.Pow((s_xx_i - s_yy_i) / 2f, 2) + Mathf.Pow(s_xy_i, 2));

            float I_1 = (s_xx_i + s_yy_i) / 2 + sqrt_term;
            float I_2 = (s_xx_i + s_yy_i) / 2 - sqrt_term;

            float minor_i = s_xx_i; //Mathf.Min(I_1, I_2);//s_xx_i;//16032025 Mathf.Min(I_1, I_2);
            float major_i = s_yy_i; //Mathf.Max(I_1, I_2);//s_yy_i;//16032025 Mathf.Max(I_1, I_2);

            minor.Add(minor_i);
            major.Add(major_i);
        }

        return (minor, major);
    }

    /// <summary>
    /// Legacy evaluation sequence: steps through the time steps and exports flow and strain maps (optionally a CSV for the MUC evaluation).
    /// </summary>
    public void analyze_params()
    {
        // info (paul): an analysis tool to go through the steps and do an analysis
        // info (paul): if with_gom == True, then a csv file will be created for the
        //          MUC evaluation

        // info (paul): set t_idx to 0
        set_t_idx(0);
        if (category == "muc")
        {
            set_t_idx(2);
        }

        // info (paul): clean info file
        string path = root_path + "info.txt";
        File.WriteAllText(path, String.Empty);

        // info (paul): do the heights
        set_strain_mode("normal");
        set_plot_mode("loss_rel");
        set_paint_with("heights");
        for (int i = 0; i < render_acts.Count; i++)
        {
            bool is_heights = render_acts[i].get_label().EndsWith("_heights");
            if (true)//19032025 (is_heights)
            {
                set_experiment(render_acts[i].get_label());
                refresh_plane_with_params();
            }
        }
    }


    /// <summary>
    /// Builds a speckle pattern from cylinder objects on a plane (legacy texture generator, rendered by the speckle camera).
    /// </summary>
    public void make_speckle_tex()
    {
        remove_children(speckle_parent);

        float speckle_size = this.speckle_size;//0.7f;
        float speckle_dist = this.speckle_dist;//1f;

        // info (paul): make a plane
        float plane_side_len = this.plane_side_len;
        Vector3 plane_pos = new Vector3(0f, 0f, 100f);

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.position = plane_pos;
        plane.transform.localScale = new Vector3(0.1f * plane_side_len,
            0.1f * plane_side_len, 0.1f * plane_side_len);
        plane.name = "speckle_plane";
        string mat_file = "Targets/fbx_files/Materials/perfect_white";
        Material perfect_white = (Material)(Resources.Load(mat_file));
        plane.GetComponent<Renderer>().material = perfect_white;
        plane.transform.SetParent(speckle_parent.transform);

        // info (paul): make speckle discs as very flat cylinders

        int num_per_side = (int)(plane_side_len / speckle_dist) + 1;//10;
        for (int i = 0; i < num_per_side; i++)
        {
            for (int j = 0; j < num_per_side; j++)
            {
                add_single_speckle(i, j, speckle_dist, speckle_size,
                    plane_pos, plane_side_len);
            }
        }
    }
    /// <summary>
    /// Renders the speckle plane and saves it as speckle texture (speckle_patterns/speckle_size.png).
    /// </summary>
    public void render_speckles()
    {
        Texture2D tex = speckle2tex();

        // info (paul): save tex
        string label = "speckle_" + this.speckle_size.ToString() + ".png";
        string path_l = path_dic + "speckle_patterns/" + label;


        System.IO.File.WriteAllBytes(path_l, tex.EncodeToPNG());
    }

    /// <summary>
    /// Reads the render target of the speckle camera into a texture.
    /// </summary>
    /// <returns>Texture of the speckle pattern.</returns>
    public Texture2D speckle2tex()
    {
        GameObject cam_obj = GameObject.Find("speckle_cam");
        Camera cam = cam_obj.GetComponent<Camera>();

        RenderTexture render_tex = cam.targetTexture;
        int our_height = render_tex.height;

        Texture2D tex = new Texture2D(our_height, our_height);
        RenderTexture.active = render_tex;
        tex.ReadPixels(new Rect(0, 0, our_height, our_height), 0, 0);
        tex.Apply();
        return tex;
    }
    /// <summary>
    /// Adds one speckle (flat black cylinder) to the speckle plane.
    /// </summary>
    /// <param name="i_idx">Grid row.</param>
    /// <param name="j_idx">Grid column.</param>
    /// <param name="speckle_dist">Grid spacing.</param>
    /// <param name="speckle_size">Diameter of the speckle.</param>
    /// <param name="plane_pos">Position of the plane.</param>
    /// <param name="plane_side_len">Side length of the plane.</param>
    public void add_single_speckle(int i_idx, int j_idx, float speckle_dist, float speckle_size,
        Vector3 plane_pos, float plane_side_len)
    {
        // info (paul): make speckle object
        GameObject speckle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        speckle.GetComponent<Renderer>().material = speckle_mat;
        //speckle.GetComponent<Renderer>().material.color = Color.black;

        // info (paul): position, scale
        float pos_x = (float)(j_idx) * speckle_dist;//((float)i) / ((float)num_per_side) * plane_side_len;
        float pos_z = (float)(i_idx) * speckle_dist;//((float)j) / ((float)num_per_side) * plane_side_len;
        speckle.transform.localScale = new Vector3(speckle_size, 0.01f, speckle_size);

        float rand_x = UnityEngine.Random.Range(0f, 1f) * speckle_dist;
        float rand_y = UnityEngine.Random.Range(0f, 1f) * speckle_dist;

        float speckle_x = plane_pos.x - 0.5f * plane_side_len + pos_x + rand_x;
        float speckle_z = plane_pos.z - 0.5f * plane_side_len + pos_z + rand_y;
        speckle.transform.position = new Vector3(speckle_x, plane_pos.y,
            speckle_z);

        speckle.transform.SetParent(speckle_parent.transform);
    }
    public List<Actioner> init_render_acts(List<Actioner> acts) //01022025 goals: theta, lambda, light pos, 
                                                                //01022025: 2 Reflexionsgrade, (2 Kr?mmungsgrade), 5 Geometrien; Bildrauschen, Linsenverzerrung
    {
        acts = new List<Actioner>();

        //22022025 acts.Add(new Actioner(start_exp_normal, "exp_fun", pars: new Params()));
        //02072025A acts.Add(new Actioner(start_exp_normal, "exp_normal", pars: new Params()));
        acts.Add(new Actioner(start_exp_from_config_now, "exp_normal", pars: new Params()));

        // info (paul): lighting
        //B acts.Add(new Actioner(start_lighting_001, "lighting_001"));//A 
        //B acts.Add(new Actioner(start_lighting_002, "lighting_002"));//A 
        //B acts.Add(new Actioner(start_lighting_003, "lighting_003"));//A // C start
        //B acts.Add(new Actioner(start_lighting_0032, "lighting_0032"));
        //B acts.Add(new Actioner(start_lighting_0034, "lighting_0034"));
        //B acts.Add(new Actioner(start_lighting_00345, "lighting_00345"));
        //B acts.Add(new Actioner(start_lighting_0035, "lighting_0035"));
        //B acts.Add(new Actioner(start_lighting_00355, "lighting_00355"));
        //B acts.Add(new Actioner(start_lighting_0036, "lighting_0036"));
        //B acts.Add(new Actioner(start_lighting_0038, "lighting_0038"));
        //B acts.Add(new Actioner(start_lighting_004, "lighting_004"));//A 
        //B acts.Add(new Actioner(start_lighting_005, "lighting_005"));//A 
        //B acts.Add(new Actioner(start_lighting_01, "lighting_01"));  //B  // C end
        //B acts.Add(new Actioner(start_lighting_02, "lighting_02"));  //B 
        //B acts.Add(new Actioner(start_lighting_05, "lighting_05"));  //B 
        //26022025 acts.Add(new Actioner(start_lighting_07, "lighting_07", pars: new Params(lighting_intensity: 0.0f)));  // C

        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_default", pars: new Params()));
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_0001", pars: new Params(lighting_intensity: 0.001f)));
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_0005", pars: new Params(lighting_intensity: 0.005f)));
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_001", pars: new Params(lighting_intensity: 0.01f)));
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_005", pars: new Params(lighting_intensity: 0.05f)));
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_01", pars: new Params(lighting_intensity: 0.1f)));
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_07", pars: new Params(lighting_intensity: 0.7f)));
        //02032025 
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_1", pars: new Params(lighting_intensity: 1.0f)));
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_2", pars: new Params(lighting_intensity: 2.0f)));
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_3", pars: new Params(lighting_intensity: 3.0f)));
        //02032025 //C acts.Add(new Actioner(start_exp_params, "lighting_4", pars: new Params(lighting_intensity: 4.0f)));
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_5", pars: new Params(lighting_intensity: 5.0f)));
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_6", pars: new Params(lighting_intensity: 6.0f)));
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_7", pars: new Params(lighting_intensity: 7.0f)));
        //02032025 acts.Add(new Actioner(start_exp_params, "lighting_8", pars: new Params(lighting_intensity: 8.0f)));


        //C acts.Add(new Actioner(start_exp_params, "lighting_10", pars: new Params(lighting_intensity: 10.0f)));

        //B acts.Add(new Actioner(start_lighting_08, "lighting_08"));  //A 
        //B acts.Add(new Actioner(start_lighting_1, "lighting_1"));    // C 
        //B acts.Add(new Actioner(start_lighting_2, "lighting_2"));    //A 
        //B acts.Add(new Actioner(start_lighting_3, "lighting_3"));    //B 
        //B acts.Add(new Actioner(start_lighting_4, "lighting_4"));    //A 
        //B acts.Add(new Actioner(start_lighting_5, "lighting_5"));    //A 
        //B acts.Add(new Actioner(start_lighting_6, "lighting_6"));    //B 
        //B acts.Add(new Actioner(start_lighting_10, "lighting_10"));  //A 
        //B acts.Add(new Actioner(start_lighting_100, "lighting_100"));//A 

        // info (paul): speckles
        //E acts.Add(new Actioner(start_speckle_0_035, "speckle_0.035"));
        //E acts.Add(new Actioner(start_speckle_0_07, "speckle_0.070"));
        //E acts.Add(new Actioner(start_speckle_0_175, "speckle_0.175"));
        //E acts.Add(new Actioner(start_speckle_0_35, "speckle_0.350"));
        //E acts.Add(new Actioner(start_speckle_0_7, "speckle_0.700"));
        //D acts.Add(new Actioner(start_speckle_1_4, "speckle_1.000"));
        //D //A acts.Add(new Actioner(start_speckle_2_1, "speckle_2.100"));//2.000
        //D acts.Add(new Actioner(start_speckle_2_8, "speckle_2.800"));
        //D acts.Add(new Actioner(start_speckle_7_0, "speckle_7.000"));
        //D acts.Add(new Actioner(start_speckle_14_0, "speckle_14.000"));

        // info (paul): heights
        //A acts.Add(new Actioner(start_lighting_005_heights, "lighting_005_heights"));
        //A acts.Add(new Actioner(start_lighting_01_heights, "lighting_01_heights"));
        //A acts.Add(new Actioner(start_lighting_05_heights, "lighting_05_heights"));
        //A acts.Add(new Actioner(start_lighting_07_heights, "lighting_07_heights"));
        //A acts.Add(new Actioner(start_lighting_08_heights, "lighting_08_heights"));
        //A 
        //A acts.Add(new Actioner(start_lighting_001_heights, "lighting_001_heights"));
        //A acts.Add(new Actioner(start_lighting_002_heights, "lighting_002_heights"));
        //A acts.Add(new Actioner(start_lighting_003_heights, "lighting_003_heights"));
        //A acts.Add(new Actioner(start_lighting_004_heights, "lighting_004_heights")); 
        //A acts.Add(new Actioner(start_lighting_005_heights, "lighting_005_heights"));
        //A acts.Add(new Actioner(start_lighting_01_heights, "lighting_01_heights"));  
        //A acts.Add(new Actioner(start_lighting_02_heights, "lighting_02_heights"));   
        //A acts.Add(new Actioner(start_lighting_05_heights, "lighting_05_heights"));  
        //16012025 acts.Add(new Actioner(start_lighting_07_heights, "lighting_07_heights"));
        //A acts.Add(new Actioner(start_lighting_08_heights, "lighting_08_heights"));  
        //A acts.Add(new Actioner(start_lighting_1_heights, "lighting_1_heights"));    
        //A acts.Add(new Actioner(start_lighting_2_heights, "lighting_2_heights"));    
        //A acts.Add(new Actioner(start_lighting_3_heights, "lighting_3_heights"));    
        //A acts.Add(new Actioner(start_lighting_4_heights, "lighting_4_heights"));    
        //acts.Add(new Actioner(start_lighting_5_heights, "lighting_5_heights"));
        //acts.Add(new Actioner(start_lighting_6_heights, "lighting_6_heights"));    
        //acts.Add(new Actioner(start_lighting_10_heights, "lighting_10_heights"));

        //AAA acts.Add(new Actioner(start_exp_params, "lighting_00001", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //AAA     lighting_intensity: 0.0001f)));
        //AAA acts.Add(new Actioner(start_exp_params, "lighting_0001", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //AAA     lighting_intensity: 0.001f)));
        //AAA acts.Add(new Actioner(start_exp_params, "lighting_001", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //AAA     lighting_intensity: 0.01f)));
        //AAA acts.Add(new Actioner(start_exp_params, "lighting_01", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //AAA     lighting_intensity: 0.1f)));
        //AAA acts.Add(new Actioner(start_exp_params, "lighting_02", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //AAA     lighting_intensity: 0.2f)));
        //A acts.Add(new Actioner(start_exp_params, "lighting_05", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //A     lighting_intensity: 0.5f)));
        //AAA acts.Add(new Actioner(start_exp_params, "lighting_1", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //AAA     lighting_intensity: 1f)));
        //AAA acts.Add(new Actioner(start_exp_params, "lighting_2", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //AAA     lighting_intensity: 2f)));
        //AAA acts.Add(new Actioner(start_exp_params, "lighting_4", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //AAA     lighting_intensity: 4f)));
        //AAA acts.Add(new Actioner(start_exp_params, "lighting_8", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //AAA     lighting_intensity: 8f)));

        //06032025 acts.Add(new Actioner(start_exp_params, "gauss_5", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //06032025     lighting_intensity: 5f)));
        //06032025 acts.Add(new Actioner(start_exp_params, "gauss_10", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //06032025     lighting_intensity: 10f)));
        //D acts.Add(new Actioner(start_exp_params, "gauss_100", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //D     lighting_intensity: 100f)));

        //G acts.Add(new Actioner(start_speckle_0_035_heights, "speckle_0.035_heights"));
        //F acts.Add(new Actioner(start_lighting_100_heights, "lighting_100_heights"));

        //G acts.Add(new Actioner(start_speckle_0_07_heights, "speckle_0.070_heights"));
        //G acts.Add(new Actioner(start_speckle_0_175_heights, "speckle_0.175_heights"));
        //G acts.Add(new Actioner(start_speckle_0_35_heights, "speckle_0.350_heights"));
        //G acts.Add(new Actioner(start_speckle_0_7_heights, "speckle_0.700_heights"));

        //20032025 acts.Add(new Actioner(start_exp_params, "gauss_00", pars: new Params(speckle_size: 0.035f, gaussian_error: 0f,
        //20032025     lighting_intensity: 0.5f)));
        //20032025 acts.Add(new Actioner(start_exp_params, "gauss_001", pars: new Params(speckle_size: 0.035f, gaussian_error: 0.01f,
        //20032025     lighting_intensity: 0.5f)));
        //20032025 acts.Add(new Actioner(start_exp_params, "gauss_01", pars: new Params(speckle_size: 0.035f, gaussian_error: 0.1f,
        //20032025     lighting_intensity: 0.5f)));
        //20032025 acts.Add(new Actioner(start_exp_params, "gauss_02", pars: new Params(speckle_size: 0.035f, gaussian_error: 0.2f,
        //20032025     lighting_intensity: 0.5f)));
        //20032025 acts.Add(new Actioner(start_exp_params, "gauss_03", pars: new Params(speckle_size: 0.035f, gaussian_error: 0.3f,
        //20032025     lighting_intensity: 0.5f)));
        //20032025 acts.Add(new Actioner(start_exp_params, "gauss_05", pars: new Params(speckle_size: 0.035f, gaussian_error: 0.5f,
        //20032025     lighting_intensity: 0.5f)));
        //20032025 acts.Add(new Actioner(start_exp_params, "gauss_1", pars: new Params(speckle_size: 0.035f, gaussian_error: 1f,
        //20032025     lighting_intensity: 0.5f)));
        //20032025 acts.Add(new Actioner(start_exp_params, "gauss_2", pars: new Params(speckle_size: 0.035f, gaussian_error: 2f,
        //20032025     lighting_intensity: 0.5f)));
        //20032025 acts.Add(new Actioner(start_exp_params, "gauss_5", pars: new Params(speckle_size: 0.035f, gaussian_error: 5f,
        //20032025     lighting_intensity: 0.5f)));
        //20032025 acts.Add(new Actioner(start_exp_params, "gauss_10", pars: new Params(speckle_size: 0.035f, gaussian_error: 10f,
        //20032025     lighting_intensity: 0.5f)));
        //acts.Add(new Actioner(start_exp_params, "gauss_10", pars: new Params(speckle_size: 14f, gaussian_error: 0.1f)));
        //acts.Add(new Actioner(start_exp_params, "gauss_100", pars: new Params(speckle_size: 14f, gaussian_error: 1f)));
        //acts.Add(new Actioner(start_exp_params, "hey_whats_up_2", pars: new Params(lighting_pos_x: 2f)));

        return acts;
    }

    /// <summary>
    /// Sets the path of the current sample mesh.
    /// </summary>
    /// <param name="input">Path of the mesh file.</param>
    public void set_blade_path(string input)
    {
        this.blade_path = input;
    }
    /// <summary>
    /// Returns the path of the current sample mesh.
    /// </summary>
    /// <returns>Path of the mesh file.</returns>
    public string get_blade_path()
    {
        return blade_path;
    }

    /// <summary>
    /// Sets the base path of the experiment folders.
    /// </summary>
    /// <param name="input">Base path.</param>
    public void set_path_dic(string input)
    {
        this.path_dic = input;
    }
    /// <summary>
    /// Returns the base path of the experiment folders (the Assets folder; experiment labels are appended directly).
    /// </summary>
    /// <returns>Base path.</returns>
    public string get_path_dic()
    {
        return path_dic;
    }
    /// <summary>
    /// Adds time to the picture timer.
    /// </summary>
    /// <param name="value">Time to add in seconds.</param>
    public void add_to_pic_timer(float value)
    {
        float val_now = get_pic_timer();
        float val_new = val_now + value;
        set_pic_timer(val_new);

    }
    /// <summary>
    /// Sets the picture timer.
    /// </summary>
    /// <param name="value">Time in seconds.</param>
    public void set_pic_timer(float value)
    {
        this.pic_timer = value;
    }
    /// <summary>
    /// Returns the picture timer.
    /// </summary>
    /// <returns>Time in seconds.</returns>
    public float get_pic_timer()
    {
        return pic_timer;
    }

    /// <summary>
    /// Returns whether the sample objects have been created.
    /// </summary>
    /// <returns>True if created.</returns>
    public bool get_blades_created()
    {
        return blades_created;
    }
    /// <summary>
    /// Marks whether the sample objects have been created.
    /// </summary>
    /// <param name="input">True if created.</param>
    public void set_blades_created(bool input)
    {
        blades_created = input;
    }
    /// <summary>
    /// Sets the field of view of the cameras.
    /// </summary>
    /// <param name="input">Field of view in degrees.</param>
    public void set_field_of_view(float input)
    {
        this.field_of_view = input;
    }
    /// <summary>
    /// Returns the field of view of the cameras.
    /// </summary>
    /// <returns>Field of view in degrees.</returns>
    public float get_field_of_view()
    {
        return this.field_of_view;
    }
    /// <summary>
    /// Returns the index of the currently displayed time step of the sample.
    /// </summary>
    /// <returns>Time-step index.</returns>
    public int get_blade_idx()
    {
        return this.blade_idx_secret;
    }
    /// <summary>
    /// Sets the index of the currently displayed time step of the sample.
    /// </summary>
    /// <param name="input">Time-step index.</param>
    public void set_blade_idx(int input)
    {
        this.blade_idx_secret = input;
    }

    /// <summary>
    /// Enables or disables the optical-flow computation of the standard analysis (toggle with_tv).
    /// </summary>
    /// <param name="val">True to compute the flow.</param>
    public void set_with_main(bool val)
    {
        this.with_main = val;
    }
    /// <summary>
    /// Returns whether the optical flow is computed in the standard analysis.
    /// </summary>
    /// <returns>True if enabled.</returns>
    public bool get_with_main()
    {
        return this.with_main;
    }
    /// <summary>
    /// Adds a user-supplied image to the list of images for analysis.
    /// </summary>
    /// <param name="file_path">Path of the image.</param>
    public void add_to_im_files(string file_path)
    {
        im_paths.Add(file_path);
        refresh_tv_files(get_im_paths());
    }
    /// <summary>
    /// Removes an image from the list of images for analysis.
    /// </summary>
    /// <param name="file_path">Path of the image.</param>
    public void remove_from_im_files(string file_path)
    {
        im_paths.Remove(file_path);
        refresh_tv_files(get_im_paths());
    }
    /// <summary>
    /// Returns the list of user-supplied images.
    /// </summary>
    /// <returns>Image paths.</returns>
    public List<string> get_im_paths()
    {
        return im_paths;
    }
    /// <summary>
    /// Replaces the list of user-supplied images.
    /// </summary>
    /// <param name="input">Image paths.</param>
    public void set_im_paths(List<string> input)
    {
        im_paths = input;
    }
    /// <summary>
    /// Sets the input folder of an image series.
    /// </summary>
    /// <param name="input">Folder path.</param>
    public void set_in_dir(string input)
    {
        this.in_dir = input;
    }
    /// <summary>
    /// Returns the input folder of an image series.
    /// </summary>
    /// <returns>Folder path.</returns>
    public string get_in_dir()
    {
        return this.in_dir;
    }
    /// <summary>
    /// Sets the output folder of an image series.
    /// </summary>
    /// <param name="input">Folder path.</param>
    public void set_out_dir(string input)
    {
        this.out_dir = input;
    }
    /// <summary>
    /// Returns the output folder of an image series.
    /// </summary>
    /// <returns>Folder path.</returns>
    public string get_out_dir()
    {
        return this.out_dir;
    }
    /// <summary>
    /// Marks whether a user-supplied image series is evaluated.
    /// </summary>
    /// <param name="val">True for series mode.</param>
    public void set_series(bool val)
    {
        series = val;
    }
    /// <summary>
    /// Returns whether a user-supplied image series is evaluated.
    /// </summary>
    /// <returns>True in series mode.</returns>
    public bool get_series()
    {
        return series;
    }

    /// <summary>
    /// Sets the render resolution and stores it, so that it is restored after a restart of Play mode.
    /// </summary>
    /// <param name="input">Resolution in pixels (square images).</param>
    public void set_render_res(int input)
    {
        this.render_res = input;
        //21092026 Nutzerwunsch: zuletzt gewaehlte Aufloesung merken, damit sie nach einem
        //Play-Neustart wieder gilt (sonst suchen value/loss-Buttons Ergebnisse bei r512,
        //waehrend die Analyse mit r128 gerechnet wurde).
        PlayerPrefs.SetInt("render_res", input);
        PlayerPrefs.Save();
    }
    //21092026 Aufloesung aus PlayerPrefs laden (Default 128, wenn noch nichts gespeichert).
    /// <summary>
    /// Loads the stored render resolution (64 to 2048 px or the legacy 1600; default 128).
    /// </summary>
    public void load_render_res_pref()
    {
        int saved = PlayerPrefs.GetInt("render_res", 128);
        //23092026 frei waehlbare Aufloesung (Schieberegler, 64..2048), 1600 bleibt fuer den alten Toggle
        if ((saved >= 64 && saved <= 2048) || saved == 1600)
            this.render_res = saved;
        Debug.Log("Renderaufloesung aus PlayerPrefs: r" + this.render_res);
        load_tv_overrides();
        nakajima_look = PlayerPrefs.GetInt("nakajima_look", 0) == 1;
        nakajima_real_aspect = PlayerPrefs.GetFloat("nakajima_real_aspect", 1f);
        nakajima_real_fov = PlayerPrefs.GetFloat("nakajima_real_fov", float.NaN);
        Debug.Log("Render-Look: " + (nakajima_look ? "Nakajima (wie Realbild: Neu)" : "klassisch")
            + ", Realbild-Aspect " + nakajima_real_aspect.ToString("F3", CultureInfo.InvariantCulture)
            + ", Realbild-FOV " + nakajima_real_fov.ToString("F2", CultureInfo.InvariantCulture)
            + " -> quadratische Ersatz-FOV " + nakajima_equivalent_square_fov().ToString("F2", CultureInfo.InvariantCulture));
    }

    //21092026 Nutzerwunsch "Render-Look: Nakajima": Der klassische with_exp-Render-Schritt
    //(verts-Proben, TV-Fluss, Ground Truth) verwendet dieselbe Szene wie "Realbild: Neu":
    //Probe an Position/Ausrichtung/Groesse der Nakajima-FBX-Referenz, gemessene
    //Speckle-Textur, gleiche Kamera (cam_0, field_of_view). Die verts-Meshes (z.B. 3, 15)
    //sind dieselbe Probe wie der FBX (Umriss +-109.5 x +-117.5), nur in der lokalen
    //x/y-Ebene statt x/z. Umschaltbar (PlayerPrefs), "klassisch" bleibt erhalten.
    private bool nakajima_look = false;
    //21092026 Seitenverhaeltnis und FOV des letzten "Realbild: Neu"-Renders (Realfoto
    //2400x1728 -> 1.389). Die TV-Pipeline rendert quadratisch; damit das quadratische Bild
    //dieselbe Breite abdeckt wie das Realbild-Render, bekommt die Kamera im Nakajima-Look
    //die aequivalente vertikale FOV: fov' = 2*atan(aspect * tan(fov/2)).
    private float nakajima_real_aspect = 1f; //21092026 "Realbild: Neu" rendert jetzt quadratisch
    private float nakajima_real_fov = float.NaN;
    /// <summary>
    /// Field of view of a square image that covers the same horizontal extent as the non-square real camera.
    /// </summary>
    /// <returns>Field of view in degrees.</returns>
    public float nakajima_equivalent_square_fov()
    {
        float base_fov = float.IsNaN(nakajima_real_fov) ? this.field_of_view : nakajima_real_fov;
        float half = base_fov * 0.5f * Mathf.Deg2Rad;
        return 2f * Mathf.Atan(nakajima_real_aspect * Mathf.Tan(half)) * Mathf.Rad2Deg;
    }
    /// <summary>
    /// Returns whether the Nakajima render look is active.
    /// </summary>
    /// <returns>True for the Nakajima look.</returns>
    public bool get_nakajima_look() { return nakajima_look; }
    /// <summary>
    /// Switches between the classic and the Nakajima render look and stores the choice; applies from the next analysis.
    /// </summary>
    /// <param name="value">True for the Nakajima look.</param>
    public void set_nakajima_look(bool value)
    {
        nakajima_look = value;
        PlayerPrefs.SetInt("nakajima_look", value ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("Render-Look: " + (nakajima_look ? "Nakajima (wie Realbild: Neu)" : "klassisch")
            + " - gilt ab der naechsten Analyse (Start).");
    }

    //21092026 Platziert eine verts-Probe so, dass sie mit der Nakajima-FBX-Referenz
    //(apply_nakajima_fbx_blade_transform, wie bei "Realbild: Neu") zusammenfaellt. Die
    //Transformation wird aus der Referenz abgeleitet statt Winkel zu raten:
    //  - Vorrotation um X (+-90), weil der FBX in der lokalen x/z-Ebene liegt (y duenn),
    //    die verts-Meshes in x/y (z = Hoehe/Dom); Vorzeichen so, dass der Dom (lokal +z)
    //    zur Kamera cam_0 zeigt (wie im realen Nakajima-Versuch).
    //  - Massstab aus der gemeinsamen Umrissbreite (x) beider Meshes.
    //  - Anker: Grundplatte (verts z_min bzw. FBX y_min) in der Umrissmitte, damit der
    //    Dom-Frame (z bis ~16) nicht ueber die Bounds-Mitte verschoben wird.
    /// <summary>
    /// Places a sample mesh like the FBX reference of the Nakajima setup: orientation towards camera 0, scale from the common outline width, anchored at the base plate.
    /// </summary>
    /// <param name="surface_obj">Sample object to place.</param>
    public void align_blade_to_nakajima_reference(GameObject surface_obj)
    {
        GameObject reference = load_nakajima_fbx_blade(obj_name: "nakajima_look_reference_tmp");
        try
        {
            Mesh ref_mesh = reference.GetComponent<MeshFilter>().sharedMesh;
            Mesh v_mesh = surface_obj.GetComponent<MeshFilter>().sharedMesh;
            Bounds bf = ref_mesh.bounds;
            Bounds bv = v_mesh.bounds;
            float unit = bf.size.x / Mathf.Max(1e-6f, bv.size.x);

            Vector3 anchor_f = new Vector3(bf.center.x, bf.min.y, bf.center.z);
            Vector3 anchor_v = new Vector3(bv.center.x, bv.center.y, bv.min.z);
            Vector3 anchor_world = reference.transform.TransformPoint(anchor_f);
            Vector3 cam_pos = cam_for_uv_0 != null ? cam_for_uv_0.transform.position
                : new Vector3(blades_pos.x, blades_pos.y + default_dist, blades_pos.z);

            surface_obj.transform.localScale = reference.transform.localScale * unit;
            foreach (float pre_x in new float[] { 90f, -90f })
            {
                surface_obj.transform.rotation = reference.transform.rotation * UnityEngine.Quaternion.Euler(pre_x, 0f, 0f);
                surface_obj.transform.position = Vector3.zero;
                surface_obj.transform.position = anchor_world - surface_obj.transform.TransformPoint(anchor_v);
                Vector3 dome_dir = surface_obj.transform.TransformDirection(Vector3.forward);
                if (Vector3.Dot(dome_dir, cam_pos - anchor_world) > 0f)
                    break;
            }
            Debug.Log("Nakajima-Look: '" + surface_obj.name + "' an FBX-Referenz ausgerichtet (scale="
                + surface_obj.transform.localScale.x.ToString("G4", CultureInfo.InvariantCulture)
                + ", pos=" + surface_obj.transform.position.ToString("F1")
                + ", euler=" + surface_obj.transform.eulerAngles.ToString("F1") + ").");
        }
        finally
        {
            Destroy(reference);
        }
    }

    //21092026 gemessene synthetische Speckle-Textur (wie bei "Realbild: Neu") auf ein Material.
    /// <summary>
    /// Assigns the experiment-derived speckle texture to a material.
    /// </summary>
    /// <param name="material">Material of the sample.</param>
    /// <param name="tiling">Texture tiling.</param>
    public void apply_measured_speckle_texture(Material material, Vector2 tiling)
    {
        Texture2D measured = load_experimental_statistics_speckle();
        if (material == null || measured == null)
            return;
        if (material.HasProperty("_BaseColorMap"))
        {
            material.SetTexture("_BaseColorMap", measured);
            material.SetTextureScale("_BaseColorMap", tiling);
        }
        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", measured);
            material.SetTextureScale("_MainTex", tiling);
        }
    }
    /// <summary>
    /// Returns the render resolution.
    /// </summary>
    /// <returns>Resolution in pixels.</returns>
    public int get_render_res()
    {
        return this.render_res;
    }

    string im_path_0;
    string im_path_1;
    /// <summary>
    /// Uses the first two user-supplied images as the image pair for the flow computation.
    /// </summary>
    /// <param name="im_paths">List of image paths.</param>
    public void refresh_tv_files(List<string> im_paths)
    {
        if (im_paths.Count > 1)
        {
            im_path_0 = im_paths[0];
            im_path_1 = im_paths[1];
        }


        ;
    }
    /// <summary>
    /// Rebuilds the list of chosen files in the file explorer.
    /// </summary>
    public void refresh_files_list()
    {
        //this.explorer

        clean_children(explorer.Find("chosen"));

        for (int i = 0; i < get_im_paths().Count; i++)
        {
            add_chosen_block(i);
        }
    }
    /// <summary>
    /// Destroys all children of a transform.
    /// </summary>
    /// <param name="obj">Parent transform.</param>
    public void clean_children(Transform obj)
    {
        int childCnt = obj.childCount;

        for (int i = 0; i < childCnt; i++)
        {
            Transform child = obj.GetChild(childCnt - 1 - i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
    }
    /// <summary>
    /// Adds an entry for a chosen file to the file explorer.
    /// </summary>
    /// <param name="block_idx">Index of the file in the list.</param>
    public void add_chosen_block(int block_idx)
    {
        GameObject block_template = Resources.Load("chosen_block") as GameObject;
        GameObject block = Instantiate(block_template);
        block.AddComponent<Chosen>();
        block.name = "chosen_block";

        float height = block.GetComponent<RectTransform>().sizeDelta.y;
        float pos_x = 50;
        float pos_y = -20 - block_idx * height;

        block.transform.SetParent(explorer.Find("chosen"));
        block.GetComponent<RectTransform>().anchoredPosition = new Vector2(pos_x, pos_y);
        string path_name = get_im_paths()[block_idx];
        string[] chunks = path_name.Split("/");
        string file_name = chunks[chunks.Length - 1];
        block.transform.Find("label").GetComponent<UnityEngine.UI.Text>().text = file_name;
        block.GetComponent<Chosen>().set_file_path(path_name);

        // info (paul): assign image:
        byte[] binaryImageData = File.ReadAllBytes(path_name);
        Texture2D tex = new Texture2D(512, 512);
        tex.LoadImage(binaryImageData);
        Sprite sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100.0f);

        UnityEngine.UI.Image im = block.transform.Find("im").GetComponent<UnityEngine.UI.Image>();
        //UnityEngine.UIElements.Image el_im = block.transform.Find("im").GetComponent<UnityEngine.UIElements.Image>();
        im.sprite = sprite;
    }

    /// <summary>
    /// Replaces the list of user-supplied images.
    /// </summary>
    /// <param name="files">Image paths.</param>
    public void set_im_files(List<string> files)
    {
        this.im_paths = files;
    }
    /// <summary>
    /// Returns the list of user-supplied images.
    /// </summary>
    /// <returns>Image paths.</returns>
    public List<string> get_im_files()
    {
        return this.im_paths;
    }
    /// <summary>
    /// Enables or disables rendering of the camera images in the standard analysis (toggle with_exp).
    /// </summary>
    /// <param name="val">True to render.</param>
    public void set_with_exp(bool val)
    {
        this.with_exp = val;

        if (!with_exp)
        {
            done_normal_exps = true;
            done_normal_ims = true;
            done_render_acts = true;
            done_speckle_ims = true;
            done_ground_truth = true;
            ready_for_next_act = false;
            //22102024 set_blade_idx(blade_idx_max);
        }
    }
    /// <summary>
    /// Returns whether the camera images are rendered in the standard analysis.
    /// </summary>
    /// <returns>True if rendering is enabled.</returns>
    public bool get_with_exp()
    {
        return this.with_exp;
    }
    /// <summary>
    /// Starts (or marks as finished) the standard analysis; starting prepares the experiment list, cameras, and progress display.
    /// </summary>
    /// <param name="val">True to start.</param>
    public void set_is_started(bool val)
    {
        this.is_started = val;

        if (val)
        {
            if (get_with_exp())
                ExperimentImageGallery.ClearCurrentExperiment();

            series_idx = 0;
            series_current_idx = 1;
            series_total_count = 1;
            progress_stopwatch.Reset();
            set_reg_idx(0);
            render_idx = 0;
            cam_idx_for_pic = 0;
            speckle_idx = 0;
            break_now = false;
            cv_action_running = false;

            // info (paul): prepare for start
            exp_cv_acts = set_up_render_list();
        }
    }
    /// <summary>
    /// Returns whether an analysis is running.
    /// </summary>
    /// <returns>True while an analysis runs.</returns>
    public bool get_is_started()
    {
        return this.is_started;
    }

    //28092026 Nutzerwunsch: Lichtstaerken als Liste aus der GUI (vorher fest 0.1 / 1 / 8).
    //  Label "lighting_<Wert mit p statt Punkt>", z.B. 0.035 -> lighting_0p035 (eindeutig, auch nach remove_dots).
    //  29092026 Die Lichtstaerke skaliert light1/light2 aus exp_setup (siehe set_up_exp_setup_lights); vorher
    //  wurde nur ein schwaches Richtungslicht variiert, neben dem aktiven Szenen-Spotlicht wirkungslos.
    /// <summary>
    /// Starts the illumination study: for each factor I the lamps light1/light2 of the setup are scaled (all other lights off, ambient light min(I, 1)), the images are rendered and analysed, and the results are merged into lighting_sweep.tsv.
    /// </summary>
    /// <param name="intensityValues">List of illumination factors separated by comma, semicolon, or space.</param>
    public void start_lighting_sweep(string intensityValues = "0.1, 1, 8")
    {
        if (get_is_started())
        {
            Debug.LogWarning("An analysis is already running.");
            return;
        }

        var intensities = new List<float>();
        foreach (string token in (intensityValues ?? "").Split(new[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            if (float.TryParse(token.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                && value > 0f && !intensities.Contains(value))
                intensities.Add(value);
        if (intensities.Count == 0)
        {
            Debug.LogWarning("Enter positive light intensities, e.g. 0.01, 0.1, 1, 10, 100.");
            return;
        }
        intensities.Sort();
        render_acts = new List<Actioner>();
        foreach (float value in intensities)
            render_acts.Add(new Actioner(start_exp_params,
                "lighting_" + exposure_label(value).Replace('.', 'p'),
                pars: new Params(lighting_intensity: value)));

        lighting_sweep_running = true;
        noise_sweep_running = false;
        speckle_sweep_running = false;
        set_series(false);
        set_with_exp(true);
        set_with_main(true);
        set_paint_with("uv");
        set_strain_mode("normal");
        set_plot_mode("loss_rel");
        im_paths.Clear();
        im_path_0 = null;
        im_path_1 = null;

        merge_sweep_table("lighting_sweep.tsv",
            "experiment\tlighting_intensity\tmean_v_error\tstd_v_error\tmin_v_error\tmax_v_error" + SWEEP_EXTRA_HEADER,
            "lighting_intensity", intensities, "Lichtanalyse");
        set_is_started(true);
        Debug.Log("Started lighting analysis for " + intensities.Count + " intensities: "
            + string.Join(", ", intensities.Select(v => v.ToString("G6", CultureInfo.InvariantCulture)))
            + (Math.Abs(analysis_exposure - 1f) > 1e-6f ? " (Analyse-k = " + analysis_exposure.ToString("G4", CultureInfo.InvariantCulture)
                + " wird im Sweep ignoriert)" : ""));
    }

    //29092026 Nutzerwunsch: bereits berechnete Stufen weiterverwenden (Licht und Rauschen). Die Tabelle wird nicht
    //  mehr ueberschrieben, sondern zusammengefuehrt: vorhandene Zeilen bleiben, wenn Kopf, Aufloesung,
    //  Regularisierer und sigma passen und der Wert nicht erneut in der Liste steht (sonst ersetzt der neue Lauf
    //  die Zeile). "inf" in der Schluesselspalte zaehlt als 0 (= ohne Rauschen). Vorher wird gesichert.
    /// <summary>
    /// Merges the results of a sweep into its table: existing rows with the same resolution, regulariser, and strain smoothing are kept unless the stage is recomputed; the previous table is backed up.
    /// </summary>
    /// <param name="file">Table file in analysis_results.</param>
    /// <param name="header">Header line of the table.</param>
    /// <param name="key_column">Column that identifies a stage.</param>
    /// <param name="new_values">Stage values of the current run.</param>
    /// <param name="what">Name of the study for the log.</param>
    void merge_sweep_table(string file, string header, string key_column, List<float> new_values, string what)
    {
        string tsv = root_path + file;
        var kept = new List<string>();
        int dropped = 0;
        if (File.Exists(tsv))
        {
            string[] old_lines = File.ReadAllLines(tsv);
            if (old_lines.Length > 0 && old_lines[0] == header)
            {
                string[] head = old_lines[0].Split('\t');
                int c_key = Array.IndexOf(head, key_column), c_res = Array.IndexOf(head, "render_res");
                string setup_now = sweep_setup_key();
                foreach (string line in old_lines.Skip(1))
                {
                    string[] cells = line.Split('\t');
                    if (cells.Length < c_res + 3) continue;
                    float old_value;
                    if (cells[c_key] == "inf") old_value = 0f;
                    else if (!float.TryParse(cells[c_key], NumberStyles.Float, CultureInfo.InvariantCulture, out old_value)) continue;
                    if (string.Join("\t", cells, c_res, 3) != setup_now) { dropped++; continue; }
                    if (new_values.Any(v => Math.Abs(v - old_value) <= 1e-6f * Math.Max(1f, v))) continue;
                    kept.Add(line);
                }
            }
            if (old_lines.Length > 1)
                File.Copy(tsv, root_path + Path.GetFileNameWithoutExtension(file) + "_before_"
                    + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".tsv", true);
        }
        File.WriteAllText(tsv, header + "\n" + (kept.Count > 0 ? string.Join("\n", kept) + "\n" : ""));
        if (kept.Count > 0 || dropped > 0)
            Debug.Log(what + ": " + kept.Count + " vorhandene Stufen werden weiterverwendet"
                + (dropped > 0 ? ", " + dropped + " mit anderer Aufloesung/Regularisierung/sigma verworfen" : "") + ".");
    }

    /// <summary>
    /// Starts the photon shot-noise study: for each full-scale capacity N_max the images are degraded with Poisson noise and analysed; results in noise_sweep.tsv.
    /// </summary>
    /// <param name="peakElectronValues">List of N_max values (inf or the infinity sign = noise-free).</param>
    public void start_noise_sweep(string peakElectronValues)
    {
        if (get_is_started())
        {
            Debug.LogWarning("An analysis is already running.");
            return;
        }

        var levels = new List<float>();
        string[] tokens = (peakElectronValues ?? "").Split(new[] { ',', ';', ' ' },
            StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawToken in tokens)
        {
            string token = rawToken.Trim();
            if (token == "∞" || token.Equals("inf", StringComparison.OrdinalIgnoreCase)
                || token.Equals("clean", StringComparison.OrdinalIgnoreCase))
            {
                if (!levels.Contains(0f)) levels.Add(0f);
                continue;
            }
            if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture,
                out float electrons) && electrons > 0f && !levels.Contains(electrons))
                levels.Add(electrons);
        }
        if (levels.Count == 0)
        {
            Debug.LogWarning("Enter positive peak-electron values, e.g. ∞, 10000, 1000, 100.");
            return;
        }

        render_acts = new List<Actioner>();
        foreach (float electrons in levels)
        {
            //29092026 auch nicht-ganzzahlige N (z.B. 0.5 -> shot_e0p5), ganzzahlige wie bisher (shot_e100)
            string label = electrons <= 0f ? "shot_clean" : "shot_e" + exposure_label(electrons).Replace('.', 'p');
            render_acts.Add(new Actioner(start_exp_params, label,
                pars: new Params(lighting_intensity: 1f, poisson_error: electrons)));
        }

        noise_sweep_running = true;
        lighting_sweep_running = false;
        speckle_sweep_running = false;
        set_series(false);
        set_with_exp(true);
        set_with_main(true);
        set_paint_with("uv");
        set_strain_mode("normal");
        set_plot_mode("loss_rel");
        im_paths.Clear();
        im_path_0 = null;
        im_path_1 = null;
        merge_sweep_table("noise_sweep.tsv",
            "experiment\tpeak_electrons\tfull_scale_relative_sigma\tmean_v_error\tstd_v_error\tmin_v_error\tmax_v_error" + SWEEP_EXTRA_HEADER,
            "peak_electrons", levels, "Rauschanalyse");
        set_is_started(true);
        Debug.Log("Started Poisson shot-noise analysis for peak-electron levels: " + peakElectronValues);
    }

    /// <summary>
    /// Starts the speckle-size study with material sizes or procedural patterns (entries p&lt;s&gt;), renders and analyses each stage; results in speckle_sweep.tsv.
    /// </summary>
    /// <param name="diameterValues">List of speckle sizes or p&lt;s&gt; entries separated by semicolon.</param>
    public void start_speckle_sweep(string diameterValues)
    {
        if (get_is_started())
        {
            Debug.LogWarning("An analysis is already running.");
            return;
        }

        var diameters = new List<float>();
        string[] tokens = (diameterValues ?? "").Split(new[] { ';', ' ', '\t' },
            StringSplitOptions.RemoveEmptyEntries);
        foreach (string raw in tokens)
        {
            //29092026 "p<s>" = prozedurale Textur mit Kreisdurchmesser s in Texturpixeln (intern als -s gefuehrt)
            string token = raw.Trim();
            bool procedural = token.StartsWith("p", StringComparison.OrdinalIgnoreCase);
            if (procedural) token = token.Substring(1);
            float diameter;
            bool parsed = float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out diameter)
                || float.TryParse(token, NumberStyles.Float, CultureInfo.CurrentCulture, out diameter);
            if (parsed && diameter > 0f)
            {
                if (procedural) diameter = -diameter;
                if (!diameters.Contains(diameter)) diameters.Add(diameter);
            }
        }
        if (diameters.Count == 0)
        {
            Debug.LogWarning("Enter positive speckle sizes separated by semicolons, e.g. 0.035; 0.070; 0.175.");
            return;
        }

        render_acts = new List<Actioner>();
        foreach (float diameter in diameters)
        {
            if (diameter < 0f)
            {
                string tex_path = procedural_speckle_path(-diameter);
                if (!File.Exists(tex_path))
                {
                    Debug.LogWarning("Ueberspringe prozedurale Speckle-Groesse p" + (-diameter).ToString("G6", CultureInfo.InvariantCulture)
                        + ": Textur fehlt (" + tex_path + "). Erzeugen mit: python scripts/make_procedural_speckles.py "
                        + (-diameter).ToString("G6", CultureInfo.InvariantCulture));
                    continue;
                }
                render_acts.Add(new Actioner(start_exp_params, "speckle_p" + procedural_speckle_label(-diameter),
                    pars: new Params(speckle_size: diameter, lighting_intensity: 1f)));
                continue;
            }
            string formatted = diameter.ToString("0.000", CultureInfo.InvariantCulture);
            string materialPath = "Targets/fbx_files/Materials/speckle_" + formatted;
            if (Resources.Load<Material>(materialPath) == null)
            {
                Debug.LogWarning("Skipping speckle size " + formatted
                    + ": material Resources/" + materialPath + " is missing.");
                continue;
            }
            render_acts.Add(new Actioner(start_exp_params, "speckle_" + formatted,
                pars: new Params(speckle_size: diameter, lighting_intensity: 1f)));
        }
        if (render_acts.Count == 0)
        {
            Debug.LogWarning("None of the requested speckle materials exists.");
            return;
        }

        speckle_sweep_running = true;
        lighting_sweep_running = false;
        noise_sweep_running = false;
        set_series(false);
        set_with_exp(true);
        set_with_main(true);
        set_paint_with("uv");
        set_strain_mode("normal");
        set_plot_mode("loss_rel");
        im_paths.Clear();
        im_path_0 = null;
        im_path_1 = null;
        //29092026 wie Licht/Rauschen: vorhandene Stufen weiterverwenden (gleiche Aufloesung/Regularisierung/sigma)
        merge_sweep_table("speckle_sweep.tsv",
            "experiment\tspeckle_size\tmean_v_error\tstd_v_error\tmin_v_error\tmax_v_error" + SWEEP_EXTRA_HEADER,
            "speckle_size", render_acts.Select(a => a.pars.get_speckle_size()).ToList(), "Speckle-Analyse");
        set_is_started(true);
        Debug.Log("Started speckle-size analysis with " + render_acts.Count + " levels: " + diameterValues
            + (nakajima_look ? " (synthetische Kreismuster; die realistische Nakajima-Textur wird im Speckle-Sweep nicht verwendet)" : ""));
    }
    //05092024 public void set_render_idx(int value)
    //05092024 {
    //05092024     this.render_idx = value;
    //05092024 }
    //05092024 public int get_render_idx()
    //05092024 {
    //05092024     return this.render_idx ;
    //05092024 }
    /// <summary>
    /// Legacy placeholder for global lighting settings (no effect).
    /// </summary>
    public void manage_lighting_settings()
    {
        //26062024 // Create an instance of LightingSettings
        //26062024 LightingSettings lightingSettings = new LightingSettings();
        //26062024 
        //26062024 // Configure the LightingSettings object
        //26062024 lightingSettings.realtimeEnvironmentLighting.e
        //26062024 
        //26062024 // Assign the LightingSettings object to the active Scene
        //26062024 Lightmapping.lightingSettings = lightingSettings;
        //26062024 return Lightmapping;
    }

    /// <summary>
    /// Removes the previously generated sample objects.
    /// </summary>
    public void clean_blades()
    {
        // info (paul): remove previously generated blades from the sample
        GameObject blades = GameObject.Find("blades");
        remove_children(blades);

        this.set_visible(false);
    }
    /// <summary>
    /// Starts creating and rendering the sample objects of the time steps.
    /// </summary>
    /// <param name="blade_path">Path of the first mesh.</param>
    /// <param name="blade_idx">Time-step index (-1 = all).</param>
    /// <param name="with_uv_init">True to initialise the texture coordinates by projection.</param>
    /// <returns>The created sample object.</returns>
    public GameObject start_renders(string blade_path, int blade_idx = -1, bool with_uv_init = false)
    {
        // info (paul):
        clean_blades();

        // info (paul): Start the machinery of creating and rendering all the blades
        GameObject surface_obj = load_blade_from_verts(blade_path: blade_path,
            blade_idx: blade_idx, with_uv_init: with_uv_init, with_collider: false);
        return surface_obj;
    }

    /// <summary>
    /// Legacy helper: converts the rendered PNG images to TIFF with an external script.
    /// </summary>
    public void png2tiff()
    {
        // info (paul): load png files in DIC_package directory, convert them to tiffs and save that again

        //Process proc = new Process();

        ProcessStartInfo psi = new ProcessStartInfo();
        //psi.FileName = "C:/Users/go73jem/AppData/Local/Microsoft/WindowsApps/PythonSoftwareFoundation.Python.3.12_qbz5n2kfra8p0";
        psi.FileName = "python";

        var script = path_project + "Assets/script_paul.py";
        psi.Arguments = $"\"{script}\"";

        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        var errors = "";
        var results = "";

        using (var process = Process.Start(psi))
        {
            errors = process.StandardError.ReadToEnd();
            results = process.StandardOutput.ReadToEnd();
        }

        //Process.Start("python", "script_paul21.py").WaitForExit();
    }
    /// <summary>
    /// Legacy helper: calls an external batch file (no longer used).
    /// </summary>
    public void call_main_batch()
    {
        // this is never used anymore, right?
        // info (paul): call the main.bat file

        // info (paul): load png files in DIC_package directory, convert them to tiffs and save that again

        //Process proc = new Process();

        ProcessStartInfo psi = new ProcessStartInfo();
        //psi.FileName = "C:/Users/go73jem/AppData/Local/Microsoft/WindowsApps/PythonSoftwareFoundation.Python.3.12_qbz5n2kfra8p0";
        psi.FileName = path_base + "main_remote.bat";

        //var script = "C:/Users/go73jem/unter2_Windows_native/Assets/script_paul.py";
        psi.Arguments = $"\"\"";

        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        var errors = "";
        var results = "";

        using (var process = Process.Start(psi))
        {
            errors = process.StandardError.ReadToEnd();
            results = process.StandardOutput.ReadToEnd();
        }

        Debug.Log("ERRORS: ");
        Debug.Log(errors.ToString());
        Debug.Log("Results: ");
        Debug.Log(results);

        //Process.Start("python", "script_paul21.py").WaitForExit();
    }

    /// <summary>
    /// Returns the number of matching steps of the display.
    /// </summary>
    /// <returns>Number of steps.</returns>
    public int get_match_steps()
    {
        return match_steps;
    }

    /// <summary>
    /// Sets the number of matching steps of the display.
    /// </summary>
    /// <param name="input">Number of steps.</param>
    public void set_match_steps(int input)
    {
        match_steps = input;
        //dt_compare = match_steps;
    }

    /// <summary>
    /// Marks whether the next sample object can be rendered.
    /// </summary>
    /// <param name="input">True when ready.</param>
    public void set_ready_for_next_blade(bool input)
    {
        ready_for_next_blade = input;
    }
    /// <summary>
    /// Returns whether the next sample object can be rendered.
    /// </summary>
    /// <returns>True when ready.</returns>
    public bool get_ready_for_next_blade()
    {
        return ready_for_next_blade;
    }

    /// <summary>
    /// Returns whether the sample is visible (texture coordinates initialised).
    /// </summary>
    /// <returns>True if visible.</returns>
    public bool get_visible()
    {
        return is_visible;
    }
    /// <summary>
    /// Marks the sample as visible (texture coordinates initialised).
    /// </summary>
    /// <param name="input">True if visible.</param>
    public void set_visible(bool input)
    {
        is_visible = input;
    }

    /// <summary>
    /// Path of the mesh file of a time step: the legacy folder under the base path if it exists, otherwise Assets/verts; converts an STL export on demand.
    /// </summary>
    /// <param name="idx">Time step (frame) of the simulation.</param>
    /// <returns>Path of verts_&lt;idx&gt;.txt.</returns>
    public string blade_path_for_idx(int idx)
    {
        // info (paul): dir from lsdyna: C:\Users\go73jem\Desktop\nakajima_full\stl\
        // info (paul): "idx+1", weil 1 entspricht 0, 2 entspricht 1, etc. 
        string blade_dir = path_base + "/play_blender_pycahrm/write_mesh/verts_" +
            idx.ToString() + ".txt";
        //25102024 (idx+1).ToString() + ".txt";

        //23092026 Fallback: Kopie der verts im Projekt (Assets/verts/), falls write_mesh auf diesem Rechner fehlt
        if (!File.Exists(blade_dir))
        {
            string blade_dir_assets = UnityEngine.Application.dataPath + "/verts/verts_" + idx.ToString() + ".txt";
            //24092026 Nutzerwunsch: fehlt die verts-Datei, direkt aus einem STL-Export erzeugen
            if (!File.Exists(blade_dir_assets))
            {
                convert_stl_for_idx(idx, blade_dir_assets);
            }
            if (File.Exists(blade_dir_assets))
            {
                blade_dir = blade_dir_assets;
            }
            else
            {
                Debug.LogError("verts-Datei nicht gefunden: " + blade_dir + " und " + blade_dir_assets
                    + " (auch keine STL t_" + idx + "_stl.stl in: " + string.Join(", ", stl_search_dirs()) + ")");
            }
        }
        return blade_dir;
    }

    //24092026 Frames (Zeitschritte der Probe) zur Laufzeit waehlen. blade_idxs = Frames + Platzhalter,
    //  weil with_dt je Paar t, t+1, t+2 liest (bei 2 Frames wie bisher der erste, sonst der letzte).
    /// <summary>
    /// Returns the frames (time steps) of the analysis without the internal placeholder entry.
    /// </summary>
    /// <returns>List of frames.</returns>
    public List<int> get_frames()
    {
        return blade_idxs.Take(Math.Max(0, blade_idxs.Count - 1)).ToList();
    }

    /// <summary>
    /// Describes the frames for reports, e.g. 1 -&gt; 27.
    /// </summary>
    /// <returns>Description text.</returns>
    public string describe_frames()
    {
        return string.Join(" -> ", get_frames());
    }

    /// <summary>
    /// Converts frames to the internal index list (frames plus a placeholder needed by the pairwise evaluation).
    /// </summary>
    /// <param name="frames">Frames of the analysis.</param>
    /// <returns>Internal index list.</returns>
    static List<int> frames_to_blade_idxs(List<int> frames)
    {
        List<int> idxs = new List<int>(frames);
        idxs.Add(frames.Count == 2 ? frames[0] : frames[frames.Count - 1]);
        return idxs;
    }

    // "28, 29", "28 29", "28;29", "28 -> 29" oder Bereich "27-29" (= 27, 28, 29)
    /// <summary>
    /// Parses frames from text such as 28, 29 or 28 -&gt; 29 or a range 27-29.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="frames">Receives the parsed frames.</param>
    /// <param name="msg">Receives an error message.</param>
    /// <returns>True if the input is valid.</returns>
    static bool try_parse_frames(string text, out List<int> frames, out string msg)
    {
        frames = new List<int>();
        msg = "";
        string t = (text ?? "").Replace("->", ",").Replace(";", ",").Replace(" ", ",");
        foreach (string tok in t.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] range = tok.Split('-');
            if (range.Length == 2 && int.TryParse(range[0], out int a) && int.TryParse(range[1], out int b))
            {
                int step = a <= b ? 1 : -1;
                for (int f = a; f != b + step; f += step)
                    frames.Add(f);
            }
            else if (int.TryParse(tok, out int f))
            {
                frames.Add(f);
            }
            else
            {
                msg = "'" + tok + "' ist keine Zahl";
                return false;
            }
        }
        if (frames.Count < 2)
        {
            msg = "mindestens zwei Frames noetig (z.B. 28, 29)";
            return false;
        }
        if (frames.Any(f => f < 0))
        {
            msg = "Frames muessen >= 0 sein";
            return false;
        }
        return true;
    }

    // Rueckgabe: Meldung fuer das Ergebnisfenster
    /// <summary>
    /// Sets the frames of the analysis from text (not while an analysis is running) and checks that the meshes exist.
    /// </summary>
    /// <param name="text">Input text of the Frames field.</param>
    /// <returns>Message for the result window.</returns>
    public string set_frames_from_text(string text)
    {
        if (get_is_started())
            return "Frames koennen waehrend einer laufenden Analyse nicht geaendert werden.";
        if (!try_parse_frames(text, out List<int> frames, out string msg))
            return "Frames ungueltig: " + msg;

        // jeder Frame braucht verts_<n>.txt (write_mesh oder Assets/verts) oder eine STL zum Umwandeln
        List<int> missing = new List<int>();
        foreach (int f in frames)
        {
            bool has_verts = File.Exists(path_base + "/play_blender_pycahrm/write_mesh/verts_" + f + ".txt")
                || File.Exists(UnityEngine.Application.dataPath + "/verts/verts_" + f + ".txt");
            bool has_stl = stl_search_dirs().Any(d => File.Exists(System.IO.Path.Combine(d, "t_" + f + "_stl.stl"))
                || File.Exists(System.IO.Path.Combine(d, "prev_t_" + f + "_stl.stl")));
            if (!has_verts && !has_stl)
                missing.Add(f);
        }
        if (missing.Count > 0)
            return "Keine Probendaten fuer Frame(s) " + string.Join(", ", missing)
                + " (weder verts_<n>.txt in Assets/verts noch t_<n>_stl.stl in "
                + string.Join(", ", stl_search_dirs()) + ").";

        blade_idxs = frames_to_blade_idxs(frames);
        set_t_idx(blade_idxs.Count - 1); // wie init_params; refresh_params klemmt auf die Anzahl Flussfelder
        set_blade_tris(init_tris_empty());
        PlayerPrefs.SetString("frames", string.Join(",", frames));
        PlayerPrefs.Save();
        Debug.Log("Frames gesetzt: " + describe_frames() + " (blade_idxs " + string.Join(",", blade_idxs) + ")");
        return "Frames: " + describe_frames()
            + "\nGilt ab der naechsten Analyse (with_exp + with_tv -> Start)."
            + (frames.Count > 2 ? "\nMehr als 2 Frames: die Flussfelder der Einzelschritte werden aufsummiert (bisher wenig getestet)." : "")
            + "\nHinweis: die Ergebnisse ueberschreiben die vorherigen derselben Aufloesung.";
    }

    //24092026 Ordner, in denen nach STL-Exporten (t_<n>_stl.stl) gesucht wird
    /// <summary>
    /// Folders that are searched for STL exports (project/stls and Downloads/nakajima_stls).
    /// </summary>
    /// <returns>List of folders.</returns>
    static List<string> stl_search_dirs()
    {
        string project = System.IO.Path.GetFullPath(UnityEngine.Application.dataPath + "/..");
        string downloads = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "nakajima_stls");
        return new List<string> { System.IO.Path.Combine(project, "stls"), downloads };
    }

    //24092026 Wandelt t_<idx>_stl.stl (ASCII, mehrere Bauteile) in das verts-Format um - wie
    //  scripts/stl2verts.py, an t_30 <-> verts_30 ueberprueft: nur "Blech_innen", danach
    //  "Nh_Bereich_fest", alle Eckpunkte in STL-Reihenfolge, "x y z" mit 5 Nachkommastellen, \r\n.
    //  Rueckgabe true, wenn out_path geschrieben wurde.
    /// <summary>
    /// Converts the STL export t_&lt;idx&gt;_stl.stl into the mesh format (sample parts only, vertex order preserved), like scripts/stl2verts.py.
    /// </summary>
    /// <param name="idx">Time step.</param>
    /// <param name="out_path">Path of the mesh file to write.</param>
    /// <returns>True if the file was written.</returns>
    bool convert_stl_for_idx(int idx, string out_path)
    {
        string stl = null;
        foreach (string dir in stl_search_dirs())
        {
            foreach (string name in new[] { "t_" + idx + "_stl.stl", "prev_t_" + idx + "_stl.stl" })
            {
                string p = System.IO.Path.Combine(dir, name);
                if (File.Exists(p)) { stl = p; break; }
            }
            if (stl != null) break;
        }
        if (stl == null)
            return false;

        Stopwatch sw = Stopwatch.StartNew();
        string[] parts = { "Blech_innen", "Nh_Bereich_fest" };
        var lines = new Dictionary<string, List<string>> { { parts[0], new List<string>() }, { parts[1], new List<string>() } };
        CultureInfo ci = CultureInfo.InvariantCulture;
        char[] sep = { ' ', '\t' };
        try
        {
            List<string> cur = null;
            using (var reader = new StreamReader(stl))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string s = line.TrimStart();
                    if (s.StartsWith("vertex"))
                    {
                        if (cur == null) continue;
                        string[] t = s.Substring(6).Split(sep, StringSplitOptions.RemoveEmptyEntries);
                        cur.Add(double.Parse(t[0], ci).ToString("F5", ci) + " "
                            + double.Parse(t[1], ci).ToString("F5", ci) + " "
                            + double.Parse(t[2], ci).ToString("F5", ci));
                    }
                    else if (s.StartsWith("solid"))
                    {
                        string name = s.Substring(5).Trim();
                        cur = lines.ContainsKey(name) ? lines[name] : null;
                    }
                }
            }
            if (lines[parts[0]].Count == 0 || lines[parts[1]].Count == 0)
            {
                Debug.LogError("STL " + stl + ": Bauteile Blech_innen/Nh_Bereich_fest nicht gefunden (ASCII-STL erwartet).");
                return false;
            }
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(out_path));
            using (var w = new StreamWriter(out_path, false, new UTF8Encoding(false)))
            {
                w.NewLine = "\r\n";
                foreach (string part in parts)
                    foreach (string l in lines[part])
                        w.WriteLine(l);
            }
            Debug.Log("STL umgewandelt: " + stl + " -> " + out_path + " ("
                + (lines[parts[0]].Count + lines[parts[1]].Count) + " Eckpunkte, "
                + sw.ElapsedMilliseconds + " ms)");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("STL-Umwandlung fehlgeschlagen (" + stl + "): " + e.Message);
            try { if (File.Exists(out_path)) File.Delete(out_path); } catch (Exception) { }
            return false;
        }
    }

    //23092026 path_base bestimmen: 1) erste Zeile von <Projekt>/path_base.txt, 2) alter Pfad auf Pauls Rechner,
    //  3) Ordner über dem Projekt. Rückgabe immer mit "/" am Ende.
    /// <summary>
    /// Determines the base path: first line of path_base.txt in the project, else a legacy path, else the folder above the project.
    /// </summary>
    /// <param name="project_dir">Project folder.</param>
    /// <returns>Base path ending with a slash.</returns>
    string resolve_path_base(string project_dir)
    {
        string candidate = null;
        string cfg_file = project_dir + "path_base.txt";
        if (File.Exists(cfg_file))
        {
            string line = File.ReadAllLines(cfg_file).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
            if (line != null)
            {
                candidate = line.Trim().Trim('"');
            }
        }
        if (candidate == null && Directory.Exists("/Users/Paul/Desktop/DIC_2025_for_travel/"))
        {
            candidate = "/Users/Paul/Desktop/DIC_2025_for_travel/";
        }
        if (candidate == null)
        {
            candidate = System.IO.Path.GetFullPath(project_dir + "..");
        }
        candidate = candidate.Replace('\\', '/');
        if (!candidate.EndsWith("/"))
        {
            candidate += "/";
        }
        Debug.Log("path_base: " + candidate + " | path_project: " + project_dir);
        return candidate;
    }

    /// <summary>
    /// Sets the folder of the stereo results.
    /// </summary>
    /// <param name="new_path">Folder path.</param>
    public void set_path_stereo(string new_path)
    {
        this.path_stereo = new_path;
    }

    /// <summary>
    /// Returns the folder of the stereo results.
    /// </summary>
    /// <returns>Folder path.</returns>
    public string get_path_stereo()
    {
        return path_stereo;
    }
    //public void set_path_time_flow(string new_path)
    //{
    //    this.path_time_flow_u = new_path;
    //}

    /// <summary>
    /// Returns the folder of the flow maps of component u.
    /// </summary>
    /// <returns>Folder path.</returns>
    public string get_path_time_flow_u()
    {
        return path_time_flow_u;
    }

    /// <summary>
    /// Sets the folder of the flow maps of component u.
    /// </summary>
    /// <param name="new_path">Folder path.</param>
    public void set_path_time_flow_u(string new_path)
    {
        this.path_time_flow_u = new_path;
    }

    /// <summary>
    /// Returns the folder of the flow maps of component v.
    /// </summary>
    /// <returns>Folder path.</returns>
    public string get_path_time_flow_v()
    {
        return path_time_flow_v;
    }

    /// <summary>
    /// Sets the folder of the flow maps of component v.
    /// </summary>
    /// <param name="new_path">Folder path.</param>
    public void set_path_time_flow_v(string new_path)
    {
        this.path_time_flow_v = new_path;
    }
    /// <summary>
    /// Sets the default display parameters (flat display, component, strain mode, time index, resolution, ...).
    /// </summary>
    /// <param name="blade_idxs">Internal index list of the frames.</param>
    public void init_params(List<int> blade_idxs)
    {
        // info (paul): set default values for parameters; 
        //      they may be changed later during the "game"

        this.force_flat = true;

        //03072024 this.set_path_stereo("C:/Users/go73jem/Desktop/DIC_package/unter2_Windows_native_Data/");//08062024 "C:/Users/go73jem/Pictures/displacements_u.csv";
        this.set_path_stereo(path_base);

        //string path_stereo = "C:/Users/go73jem/Pictures/displacements_u.csv";
        //10052024 string path_time_flow = "C:/Users/go73jem/unter2_Windows_native/Assets/Resources/Targets/flow_159_160.png";
        //16052024 string path_time_flow = "C:/Users/go73jem/Desktop/DIC_package/time_flow.png";//12052024 
        //05062024 this.path_time_flow = "C:/Users/go73jem/Desktop/DIC_package_pre_05062024/time_flow/";
        this.set_path_time_flow_u(path_dic);//08062024 "C:/Users/go73jem/Desktop/DIC_package/time_flow/";
        this.set_path_time_flow_v(path_dic);//08062024 "C:/Users/go73jem/Desktop/DIC_package/time_flow/";

        this.res_x = -1;//13062024 1026; //120;//200;//600;
        this.res_y = -1;//13062024 1031; //192;//576;// 1728;
        this.im_cnt = 20;
        set_t_idx(blade_idxs.Count - 1);//2;//14062024 2;
        set_match_steps(1);//1//2//15062024 6);
    }

    /// <summary>
    /// Registers the time-step control of the scene UI.
    /// </summary>
    /// <param name="t_control_input">Time-step control.</param>
    public void init_t_control(T_control_script t_control_input)
    {
        this.t_control = t_control_input;
    }
    /// <summary>
    /// Registers the matching-steps control of the scene UI.
    /// </summary>
    /// <param name="match_control">Matching-steps control.</param>
    public void init_match_control(Match_steps match_control)
    {
        this.match_control = match_control;
    }

    /// <summary>
    /// Registers the strain-mode control of the scene UI.
    /// </summary>
    /// <param name="input">Strain-mode control.</param>
    public void init_strain_control(Strain input)
    {
        this.strain_control = input;
    }
    /// <summary>
    /// Registers the component control of the scene UI.
    /// </summary>
    /// <param name="input">Component control.</param>
    public void init_u_v_control(U_V input)
    {
        this.u_v_control = input;
    }
    /// <summary>
    /// Registers the experiment control of the scene UI.
    /// </summary>
    /// <param name="input">Experiment control.</param>
    public void init_experiment_control(Experiment_Control input)
    {
        this.experiment_control = input;
    }
    /// <summary>
    /// Registers the strain-direction control of the scene UI.
    /// </summary>
    /// <param name="input">Strain-direction control.</param>
    public void init_strain_d_control(Strain_D input)
    {
        this.strain_d = input;
    }

    /// <summary>
    /// Returns the texture coordinates of the sample in the first frame.
    /// </summary>
    /// <returns>Texture coordinates per vertex.</returns>
    public Vector2[] get_uv_start()
    {
        return this.uv_start;
    }
    /// <summary>
    /// Stores the texture coordinates of the sample in the first frame (they attach the texture to the material).
    /// </summary>
    /// <param name="input">Texture coordinates per vertex.</param>
    public void set_uv_start(Vector2[] input)
    {
        this.uv_start = input;
    }
    /// <summary>
    /// Enables the flat display of maps (plane instead of the 3D surface).
    /// </summary>
    /// <param name="force_flat_input">True for the flat display.</param>
    public void set_force_flat(bool force_flat_input)
    {
        force_flat = force_flat_input;
    }
    /// <summary>
    /// Sets the horizontal display resolution.
    /// </summary>
    /// <param name="res_x_input">Width in pixels.</param>
    public void set_res_x(int res_x_input)
    {
        res_x = res_x_input;
    }
    /// <summary>
    /// Sets the vertical display resolution.
    /// </summary>
    /// <param name="res_y_input">Height in pixels.</param>
    public void set_res_y(int res_y_input)
    {
        res_y = res_y_input;
    }
    /// <summary>
    /// Sets the number of images of the current sequence.
    /// </summary>
    /// <param name="im_cnt_input">Number of images.</param>
    public void set_im_cnt(int im_cnt_input)
    {
        im_cnt = im_cnt_input;
    }
    /// <summary>
    /// Sets the displayed time index.
    /// </summary>
    /// <param name="t_idx_input">Time index.</param>
    public void set_t_idx(int t_idx_input)
    {
        t_idx = t_idx_input;
    }
    /// <summary>
    /// Sets the strain display mode (normal or derivatives).
    /// </summary>
    /// <param name="input">Strain mode.</param>
    public void set_strain_mode(string input)
    {
        strain_mode = input;
    }
    /// <summary>
    /// Sets the displayed component (u, v, or z).
    /// </summary>
    /// <param name="input">Component.</param>
    public void set_u_v_mode(string input)
    {
        u_v_mode = input;
    }
    /// <summary>
    /// Sets the derivative direction of the strain display (x or y).
    /// </summary>
    /// <param name="input">Direction.</param>
    public void set_strain_d_mode(string input)
    {
        strain_d_mode = input;
    }

    /// <summary>
    /// Returns the displayed component.
    /// </summary>
    /// <returns>u, v, or z.</returns>
    public string get_u_v_mode()
    {
        return u_v_mode;
    }

    /// <summary>
    /// Returns whether maps are displayed flat.
    /// </summary>
    /// <returns>True for the flat display.</returns>
    public bool get_force_flat()
    {
        return force_flat;
    }
    /// <summary>
    /// Returns the horizontal display resolution.
    /// </summary>
    /// <returns>Width in pixels.</returns>
    public int get_res_x()
    {
        return res_x;
    }
    /// <summary>
    /// Returns the vertical display resolution.
    /// </summary>
    /// <returns>Height in pixels.</returns>
    public int get_res_y()
    {
        return res_y;
    }
    /// <summary>
    /// Returns the number of images of the current sequence.
    /// </summary>
    /// <returns>Number of images.</returns>
    public int get_im_cnt()
    {
        return im_cnt;
    }
    /// <summary>
    /// Returns the displayed time index.
    /// </summary>
    /// <returns>Time index.</returns>
    public int get_t_idx()
    {
        return t_idx;
    }
    /// <summary>
    /// Returns the strain display mode.
    /// </summary>
    /// <returns>Strain mode.</returns>
    public string get_strain_mode()
    {
        return strain_mode;
    }
    /// <summary>
    /// Returns the derivative direction of the strain display.
    /// </summary>
    /// <returns>x or y.</returns>
    public string get_strain_d_mode()
    {
        return strain_d_mode;
    }

    /// <summary>
    /// Legacy export of all maps needed for the manuscript figures.
    /// </summary>
    public void save_all()
    {
        // info (paul): save all images, that the paper may need, in the corresponding directories

        //plot_mode = "value_ref";////"loss_rel""loss_abs""value""value_ref"""
        //heights_mode = "value";//"value""value_ref""loss_abs""loss_rel"""
        //paint_with = "uv";//"uv";"heights"

        refresh_plane_with_params();
    }

    /// <summary>
    /// Creates empty triangle lists for the sample objects.
    /// </summary>
    /// <param name="blades_cnt">Number of objects (-1 = number of frames).</param>
    /// <returns>List of empty triangle arrays.</returns>
    public List<int[]> init_tris_empty(int blades_cnt = -1)
    {
        List<int[]> blade_tris = new List<int[]>();

        if (blades_cnt == -1)
        {
            blades_cnt = blade_idxs.Count;
        }

        //211022024 for (int i = blade_idx_min; i < blade_idx_max; i++)
        for (int i = 0; i < blades_cnt; i++)
        {
            blade_tris.Add(null);
        }
        return blade_tris;
    }
    //21092026 true, wenn fuer Experiment + aktuelle Aufloesung TV-Flussfelder vorliegen.
    /// <summary>
    /// Checks whether flow results exist for the current experiment and resolution.
    /// </summary>
    /// <param name="missing_path">Receives the path that was checked.</param>
    /// <returns>True if the flow maps exist.</returns>
    public bool flow_results_available(out string missing_path)
    {
        missing_path = path_dic + remove_dots(get_experiment()) + "/time_flow_u/time_flow_u_0_r"
            + get_render_res().ToString() + ".png";
        return File.Exists(missing_path);
    }

    /// <summary>
    /// Rebuilds the displayed map (value, reference, or error of the chosen component, strain, or depth) on the sample or the flat plane; shows a message if no results exist for the resolution.
    /// </summary>
    /// <param name="path_time_flow_v">Folder of the flow maps (null = default).</param>
    /// <param name="t_idx">Time index (-1 = current).</param>
    /// <param name="with_save">True to save the displayed map.</param>
    public void refresh_plane_with_params(
        string path_time_flow_v = null,
        int t_idx = -1, bool with_save = false)
    {
        //21092026 Nutzerwunsch: Aufloesung umschaltbar. Liegt fuer die gewaehlte Aufloesung
        //keine Analyse vor, klare Meldung statt einer Nullkarte.
        string missing;
        if (!flow_results_available(out missing))
        {
            string msg = "Keine TV-Ergebnisse fuer '" + get_experiment() + "' bei r" + get_render_res()
                + ". Bitte Analyse (with_exp + with_tv) mit dieser Aufloesung laufen lassen. (" + missing + ")";
            Debug.LogWarning(msg);
            ExperimentImageGallery.SetResultsText(msg);
            ExperimentImageGallery.ShowResultsWindow();
            return;
        }

        //23092026 Zeitmessung je Schritt, um langsame Refreshes (save/Genauigkeit) zu finden
        Stopwatch sw = Stopwatch.StartNew();
        t_gt_ms = t_tris_ms = t_choose_ms = 0;

        // info (paul): init params
        (path_time_flow_v, t_idx) = refresh_params(path_time_flow_v, t_idx);
        List<List<float>> heights, heights_chosen;
        float scale_factor;
        if (refresh_batch_depth > 0 && batch_heights != null)
        {
            //23092026 im Refresh-Durchgang unveraendert -> wiederverwenden (nur die alte Ebene entfernen,
            //  was manage_heights sonst nebenbei erledigt)
            (heights, heights_chosen, scale_factor) = batch_heights.Value;
            clean_platine_plane();
        }
        else
        {
            (heights, heights_chosen, scale_factor) = manage_heights();
            if (refresh_batch_depth > 0)
                batch_heights = (heights, heights_chosen, scale_factor);
        }
        long t_heights = sw.ElapsedMilliseconds;
        (Texture2D flow_tex, List<List<float>> mat) = manage_flow(path_time_flow_v, path_time_flow_v,
            heights, heights_chosen);
        long t_flow = sw.ElapsedMilliseconds;

        GameObject platine_plane = null;
        (platine_plane, _) = make_platine_plane(heights_chosen, null,
            flow_tex, force_flat: force_flat, scale_factor: scale_factor,
            with_save: with_save);
        long t_plane = sw.ElapsedMilliseconds;

        manage_assign(platine_plane, t_idx);
        long t_all = sw.ElapsedMilliseconds;
        Debug.Log("Refresh-Zeiten r" + get_render_res() + " [" + get_plot_mode() + ", " + get_u_v_mode()
            + (with_save ? ", save" : "") + "] in ms: gesamt " + t_all
            + " | Hoehen " + t_heights
            + " | Fluss+Karten " + (t_flow - t_heights)
            + " (davon GT laden " + t_gt_ms + ", Maske laden " + t_tris_ms + ", Karten rechnen " + t_choose_ms + ")"
            + " | Ebene/Export " + (t_plane - t_flow)
            + " | Rest " + (t_all - t_plane));
    }
    private long t_gt_ms, t_tris_ms, t_choose_ms;

    //23092026 Refresh-Durchgang (save / Genauigkeit): mehrere Refreshes hintereinander, bei denen sich
    //  nur Modus (value/value_ref/loss_*) und Komponente (u/v) aendern. Modusunabhaengige Zwischen-
    //  ergebnisse werden fuer die Dauer des Durchgangs gemerkt und danach verworfen.
    private int refresh_batch_depth = 0;
    private (List<List<float>> heights, List<List<float>> heights_chosen, float scale_factor)? batch_heights = null;
    private (int t_idx, List<List<float>> u, List<List<float>> v, List<Texture2D> texs_u, List<Texture2D> texs_v,
        int res_x, int res_y)? batch_flow = null;
    private int batch_ref_value_t_idx = int.MinValue;
    private List<List<float>> batch_ref_u, batch_ref_v, batch_ref_z, batch_value_u, batch_value_v;
    private float batch_coverage_ref, batch_value_coverage;

    /// <summary>
    /// Starts a batch of display refreshes that share intermediate results (e.g. u and v in one export).
    /// </summary>
    void begin_refresh_batch()
    {
        if (refresh_batch_depth == 0)
            clear_refresh_batch();
        refresh_batch_depth++;
    }

    /// <summary>
    /// Ends a batch of display refreshes and clears the shared intermediate results.
    /// </summary>
    void end_refresh_batch()
    {
        refresh_batch_depth = Math.Max(0, refresh_batch_depth - 1);
        if (refresh_batch_depth == 0)
            clear_refresh_batch();
    }

    /// <summary>
    /// Clears the intermediate results shared within a refresh batch.
    /// </summary>
    void clear_refresh_batch()
    {
        batch_heights = null;
        batch_flow = null;
        batch_ref_value_t_idx = int.MinValue;
        batch_ref_u = batch_ref_v = batch_ref_z = batch_value_u = batch_value_v = null;
    }

    /// <summary>
    /// Recomputes and displays the reference map of the flow.
    /// </summary>
    /// <param name="with_save">True to save the map.</param>
    private void refresh_uv_ground_truth_with_params(bool with_save)
    {
        string flow_root = get_path_time_flow_v();
        (Texture2D flow_tex, _) = manage_flow(flow_root, flow_root, null, null);
        save_current_flow(flow_tex, with_save);
    }

    //21092026 Nutzerwunsch: "save" exportiert u- UND v-Komponente (zwei Galerie-Bilder),
    //unabhaengig von der aktuellen Einstellung im u/v-Panel; diese wird danach wiederhergestellt.
    //23092026 restore = false: Ansicht danach nicht neu aufbauen (spart einen Refresh je Modus
    //  in save_accuracy_analysis, das am Ende selbst einmal neu aufbaut).
    /// <summary>
    /// Exports the maps of both components u and v, independent of the component selected in the display.
    /// </summary>
    /// <param name="restore">True to rebuild the previous display afterwards.</param>
    public void save_maps_u_and_v(bool restore = true)
    {
        string mode_before = get_u_v_mode();
        begin_refresh_batch(); //23092026 Zwischenergebnisse fuer u, v und die Wiederherstellung teilen
        try
        {
            foreach (string comp in new string[] { "u", "v" })
            {
                set_u_v_mode(comp);
                refresh_plane_with_params(with_save: true);
            }
        }
        finally
        {
            set_u_v_mode(mode_before);
            if (restore)
                refresh_plane_with_params();
            end_refresh_batch();
        }
    }

    //23092026 Nutzerwunsch: Genauigkeitsanalyse mit einem Klick. Exportiert value, value_ref,
    //  loss_abs und loss_rel fuer u und v (wie Modus waehlen -> save) und schreibt eine Statistik
    //  (Anzeige im Ergebnisfenster, Log und accuracy_<exp>_r<res>.txt in nice_pics).
    //  Die Fehlerdefinitionen bleiben die bisherigen aus flow_or_loss_ij (Nutzerentscheidung):
    //  loss_abs = | |u| - |u_ref| |, loss_rel = loss_abs / |u_ref|, mit der bisherigen Maske.
    //  Bias/RMSE/MAE/corr werden zusaetzlich aus der vorzeichenbehafteten Differenz u - u_ref
    //  gerechnet (wie scripts/tv_error.py).
    /// <summary>
    /// Accuracy analysis (button Genauigkeit): loads the flow and the mesh reference once, computes value, reference, absolute and relative error for u and v, strain maps, and statistics, writes the raw float maps to nice_pics, and shows panels and report in the gallery.
    /// </summary>
    public void save_accuracy_analysis()
    {
        string missing;
        if (!flow_results_available(out missing))
        {
            refresh_plane_with_params(); // zeigt die Meldung "Keine TV-Ergebnisse ..."
            return;
        }

        //23092026 Direkter Weg statt 9 kompletter Anzeige-Refreshes (Hoehenkarte, Mesh, Glaettung,
        //  Kodierung ... je Modus und Komponente, bei r1024 > 20 s): Flussfelder, Ground Truth und
        //  Maske einmal laden, die 4 Karten je Komponente mit choose_flow_or_loss rechnen (gleiche
        //  Definitionen wie bisher) und daraus Einzelkarten, Panels und Statistik erzeugen.
        //  Die 3D-Ansicht bleibt dabei unveraendert.
        Stopwatch sw = Stopwatch.StartNew();
        string plot_mode_before = get_plot_mode();
        string[] modes = { "value", "value_ref", "loss_abs", "loss_rel" };
        var maps = new Dictionary<string, (List<List<float>> u, List<List<float>> v)>();
        long t_load = 0, t_maps = 0;
        try
        {
            (string dir_flow, int t_idx) = refresh_params(null, -1);
            string exp_dir = remove_dots(get_experiment());
            FileInfo[] files_u = new DirectoryInfo(dir_flow + exp_dir + "/time_flow_u/").GetFiles("*.*");
            FileInfo[] files_v = new DirectoryInfo(dir_flow + exp_dir + "/time_flow_v/").GetFiles("*.*");
            var flow_files = select_flow_files(files_u, files_v);
            (List<List<List<float>>> mats_u, List<List<List<float>>> mats_v, _, _, _, _) =
                find_flow_mats(flow_files, im_cnt: flow_files["u"].Count);
            (mats_u, mats_v) = scale_flows(mats_u, mats_v, scale_fac: this.flow_scale_factor);
            (List<List<float>> flow_u, List<List<float>> flow_v) = find_accum_flow(mats_u, mats_v, t_idx);
            (float[] d_xs, float[] d_ys, float[] d_zs) = load_distortion_ground_truth(blade_idx: t_idx);
            (int[,] tri_idx, float[][][] barys) = load_tris(blade_idx: 0);
            t_load = sw.ElapsedMilliseconds;

            foreach (string mode in modes)
            {
                set_plot_mode(mode, is_internal: true);
                (List<List<float>> su, List<List<float>> sv, _, _) = choose_flow_or_loss(
                    flow_u, flow_v, tri_idx, barys, d_xs, d_ys, d_zs);
                maps[mode] = (su, sv);
            }
            t_maps = sw.ElapsedMilliseconds;
        }
        catch (Exception exception)
        {
            string msg = "Genauigkeitsanalyse abgebrochen: " + exception.Message;
            Debug.LogError(msg + "\n" + exception);
            ExperimentImageGallery.SetResultsText(msg);
            ExperimentImageGallery.ShowResultsWindow();
            return;
        }
        finally
        {
            set_plot_mode(plot_mode_before, is_internal: true);
        }

        //27092026 Rohdaten (TV und Ground Truth, ungeglaettet, float) fuer Auswertungen ausserhalb von Unity,
        //  z.B. scripts/strain_sweep.py (Dehnungsfenster sigma optimieren). Format: int magic, int n, int m,
        //  dann n*m float32, Index i*m + j mit m[i][j] = Pixel (x = i, y = j, Zeile von oben).
        foreach (string mode in new[] { "value", "value_ref" })
            foreach (string comp in new[] { "u", "v" })
                write_raw_map(comp == "u" ? maps[mode].u : maps[mode].v, path_dic + "exp_normal/time_flow_v/nice_pics/"
                    + "accuracy_raw_" + mode + "_" + comp + "_r" + get_render_res() + ".f32");

        //23092026 2x2-Panels (wie im Manuskript) ganz vorne in die Galerie
        int front_idx = 0;
        //27092026 Dehnungsfelder (exx aus u, eyy aus v): Panels nach den Fluss-Panels, Statistik in den Bericht
        List<string> strain_panels = new List<string>();
        string strain_report = "";
        foreach (string comp in new string[] { "u", "v" })
        {
            try
            {
                last_strain_panel = null;
                last_strain_report = "";
                string panel_path = export_accuracy_component(comp, modes, maps);
                ExperimentImageGallery.InsertRenderedImage(panel_path, front_idx++);
                if (last_strain_panel != null)
                    strain_panels.Add(last_strain_panel);
                strain_report += last_strain_report;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Genauigkeits-Karten " + comp + " nicht erstellt: " + exception.Message);
            }
        }
        foreach (string p in strain_panels)
            ExperimentImageGallery.InsertRenderedImage(p, front_idx++);
        long t_export = sw.ElapsedMilliseconds;
        Debug.Log("Genauigkeit r" + get_render_res() + " in ms: gesamt " + t_export + " | Laden " + t_load
            + " | Karten rechnen " + (t_maps - t_load) + " | Einfaerben/Export " + (t_export - t_maps));

        string exp = remove_dots(get_experiment());
        string header = "Genauigkeit " + exp + ", r" + get_render_res() + ", t_idx " + get_t_idx()
            + ", " + DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            //27092026 Einstellungen (gelten fuer die zuletzt gerechnete Analyse, sofern seitdem nicht geaendert)
            + "\nFrames " + describe_frames() + " | Speckle " + describe_speckle_texture()
            + " | Belichtung k " + analysis_exposure.ToString("0.###", CultureInfo.InvariantCulture) //28092026
            + " | Regularisierung " + describe_regularization()
            + " | " + describe_tv_overrides();
        string report = header + "\n"
            + accuracy_block("u", maps["value"].u, maps["value_ref"].u, maps["loss_abs"].u, maps["loss_rel"].u)
            + accuracy_block("v", maps["value"].v, maps["value_ref"].v, maps["loss_abs"].v, maps["loss_rel"].v)
            + (strain_report == "" ? "" : "Dehnung (Bild, px/px; Fehler |e - e_ref|, rel. maskiert bei |e_ref| < 10 % max):\n"
                + strain_report);

        string report_path = path_dic + "exp_normal/time_flow_v/nice_pics/accuracy_" + exp
            + "_r" + get_render_res() + ".txt";
        try
        {
            arrange_dir(report_path);
            System.IO.File.WriteAllText(report_path, report);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Genauigkeitsbericht nicht gespeichert: " + exception.Message);
        }

        Debug.Log(report + "\n-> " + report_path);
        ExperimentImageGallery.SetResultsText(report);
        ExperimentImageGallery.ShowResultsWindow();
    }

    //23092026 Karten einer Komponente fuer "Genauigkeit": Anzeige-Aufbereitung wie in der bisherigen
    //  Pipeline (Randglaettung + Mittelwertfilter, siehe find_total_match_tex), Kodierung wie save_tex
    //  (norm_mat + mat2tex, damit "Bilder: Laden" und scripts/figure_accuracy.py sie lesen koennen),
    //  eingefaerbte Einzelkarten in die Galerie, dazu das 2x2-Panel. Rueckgabe: Pfad des Panels.
    /// <summary>
    /// Exports the maps of one component for the accuracy analysis (individual maps and a 2x2 panel) and adds them to the gallery.
    /// </summary>
    /// <param name="comp">Component u or v.</param>
    /// <param name="modes">Map types (value, value_ref, loss_abs, loss_rel).</param>
    /// <param name="maps">Maps of both components per type.</param>
    /// <returns>Path of the panel image.</returns>
    string export_accuracy_component(string comp, string[] modes,
        Dictionary<string, (List<List<float>> u, List<List<float>> v)> maps)
    {
        string folder = path_dic + "exp_normal/time_flow_v/nice_pics/";
        Directory.CreateDirectory(folder);
        string exp = get_experiment();
        string strain = get_strain_mode();
        string suffix = "_" + comp + "_r" + get_render_res().ToString();
        CultureInfo ci = CultureInfo.InvariantCulture;

        List<List<float>>[] disp = new List<List<float>>[4];
        float[] mins = new float[4];
        float[] maxs = new float[4];
        for (int k = 0; k < 4; k++)
        {
            List<List<float>> stream = comp == "u" ? maps[modes[k]].u : maps[modes[k]].v;
            disp[k] = filter_mean_comp(smoothen_frame(stream));
            (mins[k], maxs[k]) = find_min_max(disp[k], with_padding: true);
            if (mins[k] > maxs[k]) { mins[k] = 0f; maxs[k] = 1f; } // keine gueltigen Werte
            // 0 als Grenze vermeiden (norm_mat wuerde sie auf 0.1 bzw. 1 setzen), damit Kodierung und
            // Dekodierung dieselben Grenzen nutzen. 27092026: winziger Betrag statt 0.1 (siehe encoding_range)
            float tiny = Mathf.Max(1e-12f, 1e-6f * Mathf.Max(Mathf.Abs(mins[k]), Mathf.Abs(maxs[k])));
            if (maxs[k] == 0f) maxs[k] = tiny;
            if (mins[k] == 0f) mins[k] = -tiny;
        }

        // Flow und Flow ref im Panel: gemeinsame Skala
        float f_min = Mathf.Min(mins[0], mins[1]);
        float f_max = Mathf.Max(maxs[0], maxs[1]);
        //27092026 Nutzerwunsch: dritte Zeile mit log10 der Fehlerkarten (Zellen 4, 5)
        Texture2D[] cells_tex = new Texture2D[6];
        for (int k = 0; k < 4; k++)
        {
            string mode = modes[k];
            string stem = exp + "_uv_" + strain + "_" + mode + suffix;
            Texture2D enc = mat2tex(norm_mat(disp[k], lower: mins[k], upper: maxs[k]), with_switch_dims: true);

            // Mittelwert/Streuung fuer die params-Datei (wie save_tex: exp, paint, strain, mode, mean, std, min, max)
            double s = 0, s2 = 0;
            int n = 0;
            for (int i = 10; i < disp[k].Count - 10; i++)
                for (int j = 10; j < disp[k][i].Count - 10; j++)
                {
                    float x = disp[k][i][j];
                    if (float.IsNaN(x) || float.IsInfinity(x)) continue;
                    s += x; s2 += (double)x * x; n++;
                }
            double mean = n > 0 ? s / n : double.NaN;
            double std = n > 0 ? Math.Sqrt(Math.Max(0.0, s2 / n - mean * mean)) : double.NaN;
            write_to_txt(folder + "params_" + stem + ".txt", exp + "\tuv\t" + strain + "\t" + mode + "\t"
                + mean.ToString(ci) + "\t" + std.ToString(ci) + "\t" + mins[k].ToString(ci) + "\t" + maxs[k].ToString(ci),
                mode: "replace");
            System.IO.File.WriteAllBytes(folder + "im_" + stem + ".png", enc.EncodeToPNG());

            //23092026 Nutzerwunsch: bei "Genauigkeit" nur das Panel anzeigen - keine eingefaerbten
            //  Einzelkarten mehr (sparte bei r1024 mehrere Sekunden); die gibt es weiter ueber "save".
            cells_tex[k] = (k < 2)
                ? colorize_encoded_map(enc, mode, mins[k], maxs[k], f_min, f_max)
                : colorize_encoded_map(enc, mode, mins[k], maxs[k]);
            Destroy(enc);
        }

        //27092026 log10 der Fehlerkarten (aus denselben Anzeige-Karten wie Zeile 2). Fehler unter der
        //  Untergrenze (auch exakt 0) werden auf sie gesetzt, sonst waere log10 = -inf.
        const float LOG_FLOOR = 1e-3f; // 0.001 px bzw. 0.1 %
        for (int k = 2; k < 4; k++)
            cells_tex[k + 2] = colorize_matrix(log10_map(disp[k], LOG_FLOOR), "log_" + modes[k]);

        //27092026 Name nennt die 3 Zeilen; das alte 2x2-Panel gleichen Namensstamms ersetzen, damit
        //  "Bilder: Laden" nicht beide Fassungen zeigt
        string old_panel = folder + "accuracy_" + exp + suffix + "_oben-Flow-FlowRef_unten-lossAbs-lossRel.png";
        try { if (File.Exists(old_panel)) File.Delete(old_panel); } catch (Exception) { }
        string panel_path = compose_panel(cells_tex, folder + "accuracy_" + exp + suffix
            + "_Flow-FlowRef_lossAbs-lossRel_log10-lossAbs-lossRel.png");

        //27092026 Nutzerwunsch: Dehnungsfeld dieser Komponente (u -> exx = du/dx, v -> eyy = dv/dy) aus
        //  denselben geglaetteten Anzeige-Karten von TV (disp[0]) und Ground Truth (disp[1])
        bool d_dx = comp == "u";
        string e_name = d_dx ? "exx" : "eyy";
        //27092026 Dehnungsfenster wie bei DIC: Gauss-Glaettung (sigma in px) vor dem Ableiten, fuer TV und
        //  Ground Truth identisch. Die GT ist pro FE-Element linear interpoliert, ihre Ableitung also pro
        //  Element konstant (feine Linien = Netzabdruck); TV-L1 neigt zu Stufen. sigma = 0: aus.
        List<List<float>> s_val = strain_sigma > 0f ? gaussian_nan(disp[0], strain_sigma) : disp[0];
        List<List<float>> s_gt = strain_sigma > 0f ? gaussian_nan(disp[1], strain_sigma) : disp[1];
        (List<List<float>> e, List<List<float>> e_ref, List<List<float>> e_abs, List<List<float>> e_rel) =
            strain_maps(s_val, s_gt, d_dx);
        //27092026 Nutzerwunsch: Farbskalen der Dehnungskarten robust (99. Perzentil statt Maximum), damit
        //  einzelne Ausreisser nicht die ganze Karte abdunkeln; Werte darueber zeigen die hellste Farbe
        const double Q_HI = 0.99, Q_LO = 0.01;
        float lim = Mathf.Max(quantile_of(e, Q_HI, absolute: true), quantile_of(e_ref, Q_HI, absolute: true));
        if (!(lim > 0f)) lim = 1e-6f;
        const float STRAIN_LOG_FLOOR = 1e-5f; // abs. Dehnungsfehler; rel. Fehler weiter 0.1 %
        List<List<float>> log_abs = log10_map(e_abs, STRAIN_LOG_FLOOR);
        List<List<float>> log_rel = log10_map(e_rel, LOG_FLOOR);
        Func<List<List<float>>, float> q_hi = mat => { float q = quantile_of(mat, Q_HI); return q > 0f ? q : 1e-6f; };
        Func<List<List<float>>, (float, float)> q_log = mat =>
        {
            float lo = quantile_of(mat, Q_LO), hi = quantile_of(mat, Q_HI);
            if (float.IsNaN(lo) || float.IsNaN(hi)) return (float.NaN, float.NaN);
            return hi - lo < 1e-3f ? (hi - 1f, hi) : (lo, hi);
        };
        (float la_lo, float la_hi) = q_log(log_abs);
        (float lr_lo, float lr_hi) = q_log(log_rel);
        Texture2D[] strain_cells =
        {
            colorize_matrix(e, "value", -lim, lim),
            colorize_matrix(e_ref, "value", -lim, lim),
            colorize_matrix(e_abs, "loss_abs", 0f, q_hi(e_abs)),
            colorize_matrix(e_rel, "loss_rel", 0f, q_hi(e_rel)),
            colorize_matrix(log_abs, "log_loss_abs", la_lo, la_hi),
            colorize_matrix(log_rel, "log_loss_rel", lr_lo, lr_hi),
        };
        string strain_path = compose_panel(strain_cells, folder + "accuracy_" + exp + "_" + e_name
            + "_r" + get_render_res() + "_Strain-StrainRef_lossAbs-lossRel_log10-lossAbs-lossRel.png");
        last_strain_report = accuracy_block((d_dx ? "exx = du/dx" : "eyy = dv/dy")
            + ", Glaettung sigma " + strain_sigma.ToString("0.#", CultureInfo.InvariantCulture) + " px",
            e, e_ref, e_abs, e_rel, unit: "", dec: 4);
        last_strain_panel = strain_path;
        return panel_path;
    }
    private string last_strain_report = "", last_strain_panel = null;

    //27092026 Dehnung aus einer Verschiebungskarte (Bild-Dehnung in px/px) per zentraler Differenz.
    //  Matrix-Konvention wie ueberall: m[i][j] = Pixel (x = i, y = j). d_dx: d/dx (i), sonst d/dy (j).
    //  Fehler: abs = |e - e_ref| (mit Vorzeichen verglichen), rel = abs / |e_ref|, maskiert wo
    //  |e_ref| < 10 % von max |e_ref|. Rand (10 px) und Pixel mit NaN-Nachbarn -> NaN.
    /// <summary>
    /// Strain of a displacement map and of its reference by central differences, with absolute and relative error (relative masked where the reference strain is below 10 % of its maximum).
    /// </summary>
    /// <param name="val">Measured displacement map.</param>
    /// <param name="gt">Reference displacement map.</param>
    /// <param name="d_dx">True for the x derivative, false for y.</param>
    /// <returns>Tuple (strain, reference strain, absolute error, relative error).</returns>
    (List<List<float>>, List<List<float>>, List<List<float>>, List<List<float>>) strain_maps(
        List<List<float>> val, List<List<float>> gt, bool d_dx)
    {
        int n = val.Count, m = val[0].Count;
        //27092026 20 statt 10 px: die Flusskarten sind nur in [6, n-6) berechnet, der 10x10-Mittelwertfilter
        //  der Anzeige zieht die Rohwerte ausserhalb bis ca. 11 px herein -> riesige Ableitungen am Rand
        const int pad = 20;
        List<List<float>> e = new List<List<float>>(n), e_ref = new List<List<float>>(n);
        for (int i = 0; i < n; i++)
        {
            List<float> r1 = new List<float>(m), r2 = new List<float>(m);
            for (int j = 0; j < m; j++)
            {
                float a = float.NaN, b = float.NaN;
                if (i >= pad && i < n - pad && j >= pad && j < m - pad)
                {
                    a = d_dx ? 0.5f * (val[i + 1][j] - val[i - 1][j]) : 0.5f * (val[i][j + 1] - val[i][j - 1]);
                    b = d_dx ? 0.5f * (gt[i + 1][j] - gt[i - 1][j]) : 0.5f * (gt[i][j + 1] - gt[i][j - 1]);
                }
                r1.Add(a);
                r2.Add(b);
            }
            e.Add(r1);
            e_ref.Add(r2);
        }

        float ref_max = 0f;
        foreach (List<float> row in e_ref)
            foreach (float x in row)
                if (!float.IsNaN(x) && !float.IsInfinity(x))
                    ref_max = Mathf.Max(ref_max, Mathf.Abs(x));
        float thr = 0.1f * ref_max;

        List<List<float>> e_abs = new List<List<float>>(n), e_rel = new List<List<float>>(n);
        for (int i = 0; i < n; i++)
        {
            List<float> ra = new List<float>(m), rr = new List<float>(m);
            for (int j = 0; j < m; j++)
            {
                float a = e[i][j], b = e_ref[i][j];
                bool ok = !float.IsNaN(a) && !float.IsNaN(b) && !float.IsInfinity(a) && !float.IsInfinity(b);
                float d = ok ? Mathf.Abs(a - b) : float.NaN;
                ra.Add(d);
                rr.Add(ok && Mathf.Abs(b) >= thr && thr > 0f ? d / Mathf.Abs(b) : float.NaN);
            }
            e_abs.Add(ra);
            e_rel.Add(rr);
        }
        return (e, e_ref, e_abs, e_rel);
    }

    //27092026 Gauss-Glaettung vor dem Ableiten der Dehnung (sigma in px, PlayerPrefs "strain_sigma",
    //  einstellbar im TV-Parameter-Panel). Default 12 px.
    private float strain_sigma = 12f; //27092026 Default 4 -> 12 px (strain_sweep: exx-Fehler 14.6 % -> 7.5 %)
    /// <summary>
    /// Returns the Gaussian smoothing applied before differentiation.
    /// </summary>
    /// <returns>Standard deviation in pixels.</returns>
    public float get_strain_sigma() { return strain_sigma; }
    /// <summary>
    /// Sets and stores the Gaussian smoothing applied before differentiation (invalid values give the default 12 px).
    /// </summary>
    /// <param name="sigma">Standard deviation in pixels (0 = off).</param>
    public void set_strain_sigma(float sigma)
    {
        strain_sigma = (float.IsNaN(sigma) || sigma < 0f) ? 12f : sigma;
        PlayerPrefs.SetFloat("strain_sigma", strain_sigma);
        PlayerPrefs.Save();
    }

    //27092026 Separable Gauss-Glaettung mit NaN-Behandlung (normierte Faltung): NaN-Pixel zaehlen nicht mit,
    //  Pixel, deren Fenster zu weniger als der Haelfte gueltig ist, werden NaN (Probenrand bleibt scharf).
    //  Konvention m[i][j]; Radius 3 sigma.
    /// <summary>
    /// Separable Gaussian smoothing that ignores NaN pixels (normalised convolution); pixels with less than half valid weight become NaN.
    /// </summary>
    /// <param name="m">Map m[i][j].</param>
    /// <param name="sigma">Standard deviation in pixels.</param>
    /// <returns>Smoothed map.</returns>
    static List<List<float>> gaussian_nan(List<List<float>> m, float sigma)
    {
        int n = m.Count, w = m[0].Count;
        int r = Math.Max(1, (int)Math.Ceiling(3f * sigma));
        float[] k = new float[2 * r + 1];
        for (int t = -r; t <= r; t++)
            k[t + r] = Mathf.Exp(-0.5f * t * t / (sigma * sigma));

        float[,] val = new float[n, w];
        float[,] wgt = new float[n, w];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < w; j++)
            {
                float x = m[i][j];
                bool ok = !float.IsNaN(x) && !float.IsInfinity(x);
                val[i, j] = ok ? x : 0f;
                wgt[i, j] = ok ? 1f : 0f;
            }

        // Pass 1 entlang j
        float[,] v1 = new float[n, w], w1 = new float[n, w];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < w; j++)
            {
                float sv = 0f, sw = 0f;
                for (int t = -r; t <= r; t++)
                {
                    int jj = j + t;
                    if (jj < 0 || jj >= w) continue;
                    sv += k[t + r] * val[i, jj];
                    sw += k[t + r] * wgt[i, jj];
                }
                v1[i, j] = sv;
                w1[i, j] = sw;
            }

        // Pass 2 entlang i
        float k_sum = 0f;
        foreach (float kk in k) k_sum += kk;
        float full = k_sum * k_sum;
        List<List<float>> outm = new List<List<float>>(n);
        for (int i = 0; i < n; i++)
        {
            List<float> row = new List<float>(w);
            for (int j = 0; j < w; j++)
            {
                if (wgt[i, j] == 0f) { row.Add(float.NaN); continue; }
                float sv = 0f, sw = 0f;
                for (int t = -r; t <= r; t++)
                {
                    int ii = i + t;
                    if (ii < 0 || ii >= n) continue;
                    sv += k[t + r] * v1[ii, j];
                    sw += k[t + r] * w1[ii, j];
                }
                row.Add(sw >= 0.5f * full ? sv / sw : float.NaN);
            }
            outm.Add(row);
        }
        return outm;
    }

    //27092026 Karte als float32-Rohdatei (siehe save_accuracy_analysis)
    /// <summary>
    /// Writes a map as raw float32 file (int magic, n, m, then values; see docs/data_formats.md).
    /// </summary>
    /// <param name="m">Map m[i][j] with i = image column.</param>
    /// <param name="path">Output file.</param>
    static void write_raw_map(List<List<float>> m, string path)
    {
        try
        {
            int n = m.Count, w = m[0].Count;
            float[] f = new float[n * w];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < w; j++)
                    f[i * w + j] = m[i][j];
            byte[] buf = new byte[12 + f.Length * 4];
            Buffer.BlockCopy(BitConverter.GetBytes(0x57415246), 0, buf, 0, 4); // "FRAW"
            Buffer.BlockCopy(BitConverter.GetBytes(n), 0, buf, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(w), 0, buf, 8, 4);
            Buffer.BlockCopy(f, 0, buf, 12, f.Length * 4);
            System.IO.File.WriteAllBytes(path, buf);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Rohdaten nicht geschrieben (" + path + "): " + e.Message);
        }
    }

    //27092026 Quantil q (0..1) der endlichen Werte einer Karte (optional von |Wert|); NaN, wenn keine Werte
    /// <summary>
    /// Quantile of the finite values of a map.
    /// </summary>
    /// <param name="m">Map.</param>
    /// <param name="q">Quantile in 0..1.</param>
    /// <param name="absolute">True to use absolute values.</param>
    /// <returns>Quantile, or NaN if there are no finite values.</returns>
    static float quantile_of(List<List<float>> m, double q, bool absolute = false)
    {
        List<float> vals = new List<float>();
        foreach (List<float> row in m)
            foreach (float x in row)
                if (!float.IsNaN(x) && !float.IsInfinity(x))
                    vals.Add(absolute ? Mathf.Abs(x) : x);
        if (vals.Count == 0)
            return float.NaN;
        vals.Sort();
        int idx = (int)Math.Round(q * (vals.Count - 1));
        return vals[Math.Max(0, Math.Min(vals.Count - 1, idx))];
    }

    //27092026 log10 einer Fehlerkarte mit Untergrenze (Werte darunter, auch 0, -> log10(floor)); NaN bleibt NaN
    /// <summary>
    /// Base-10 logarithm of an error map with a lower limit.
    /// </summary>
    /// <param name="m">Error map.</param>
    /// <param name="floor">Lower limit (smaller values and zero are set to it).</param>
    /// <returns>Logarithmic map.</returns>
    static List<List<float>> log10_map(List<List<float>> m, float floor)
    {
        List<List<float>> lg = new List<List<float>>(m.Count);
        foreach (List<float> row in m)
        {
            List<float> lrow = new List<float>(row.Count);
            foreach (float x in row)
                lrow.Add(float.IsNaN(x) || float.IsInfinity(x) ? float.NaN : Mathf.Log10(Mathf.Max(x, floor)));
            lg.Add(lrow);
        }
        return lg;
    }

    //27092026 Grenzen fuer Kodierung/Dekodierung einer Karte (wie norm_mat: 0 als Grenze vermeiden)
    /// <summary>
    /// Value range used to encode and decode a map as image (zero is avoided as a bound, as in norm_mat).
    /// </summary>
    /// <param name="m">Map.</param>
    /// <param name="mode">Map type (log_ prefix for logarithmic maps).</param>
    /// <returns>Tuple (minimum, maximum).</returns>
    (float, float) encoding_range(List<List<float>> m, string mode)
    {
        bool is_log = mode.StartsWith("log_");
        (float mn, float mx) = find_min_max(m, with_padding: true);
        if (mn > mx) { mn = is_log ? -3f : 0f; mx = is_log ? 0f : 1f; } // keine gueltigen Werte
        //27092026 Bugfix: norm_mat ersetzt eine Grenze von exakt 0 durch 0.1 (min) bzw. 1 (max). Bei kleinen
        //  Werten (Dehnungsfehler ~0.001) lag die Untergrenze 0.1 dann ueber allen Werten -> Karte komplett
        //  gesaettigt. Stattdessen um einen winzigen, zum Wertebereich relativen Betrag verschieben.
        float tiny = Mathf.Max(1e-12f, 1e-6f * Mathf.Max(Mathf.Abs(mn), Mathf.Abs(mx)));
        if (mx == 0f) mx = is_log ? 1e-4f : tiny;
        if (mn == 0f) mn = is_log ? -1e-4f : -tiny;
        if (is_log && mx - mn < 1e-3f) mn = mx - 1f;
        return (mn, mx);
    }

    //27092026 Matrix -> eingefaerbte Zelle mit Farbbalken (Kodierung wie save_tex, dann colorize_encoded_map);
    //  scale_min/scale_max optional fuer eine gemeinsame Skala mehrerer Zellen
    /// <summary>
    /// Renders a map as coloured cell with colour bar.
    /// </summary>
    /// <param name="m">Map.</param>
    /// <param name="mode">Map type (defines colour map and label).</param>
    /// <param name="scale_min">Optional common lower bound of the colour scale.</param>
    /// <param name="scale_max">Optional common upper bound of the colour scale.</param>
    /// <returns>Coloured texture.</returns>
    Texture2D colorize_matrix(List<List<float>> m, string mode, float scale_min = float.NaN, float scale_max = float.NaN)
    {
        (float mn, float mx) = encoding_range(m, mode);
        Texture2D enc = mat2tex(norm_mat(m, lower: mn, upper: mx), with_switch_dims: true);
        Texture2D cell = colorize_encoded_map(enc, mode, mn, mx, scale_min, scale_max);
        Destroy(enc);
        return cell;
    }

    //27092026 Zellen zeilenweise (2 Spalten) zu einem Panel zusammensetzen, speichern, Zellen freigeben
    /// <summary>
    /// Arranges cells in rows of two into one panel image, saves it, and releases the cells.
    /// </summary>
    /// <param name="cells_tex">Cells in reading order.</param>
    /// <param name="panel_path">Output file.</param>
    /// <returns>Path of the panel.</returns>
    string compose_panel(Texture2D[] cells_tex, string panel_path)
    {
        int cw = cells_tex[0].width;
        int ch = cells_tex[0].height;
        int gap = Mathf.Max(8, cw / 16);
        int n_rows = (cells_tex.Length + 1) / 2;
        int W = 2 * cw + 3 * gap;
        int H = n_rows * ch + (n_rows + 1) * gap;
        Texture2D panel = new Texture2D(W, H, TextureFormat.RGBA32, false);
        Color[] dst = new Color[W * H];
        Color bg = new Color(0.16f, 0.16f, 0.18f, 1f);
        for (int i = 0; i < dst.Length; i++)
            dst[i] = bg;
        for (int k = 0; k < cells_tex.Length; k++)
        {
            Texture2D c = cells_tex[k];
            int col = k % 2;
            int row_from_top = k / 2;
            int x0 = gap + col * (cw + gap);
            int y0 = gap + (n_rows - 1 - row_from_top) * (ch + gap); // Texture2D: y = 0 ist unten
            Color[] src = c.GetPixels();
            for (int y = 0; y < Mathf.Min(ch, c.height); y++)
                for (int x = 0; x < Mathf.Min(cw, c.width); x++)
                    dst[(y0 + y) * W + x0 + x] = src[y * c.width + x];
            Destroy(c);
        }
        panel.SetPixels(dst);
        panel.Apply();
        System.IO.File.WriteAllBytes(panel_path, panel.EncodeToPNG());
        Destroy(panel);
        return panel_path;
    }

    // ============================================================================================
    //27092026 Nutzerwunsch (Paper-Revision): Parameterstudie zur Begruendung der TV-/TGV-Parameter.
    //  Einfaktor-Studie (one-at-a-time) um die aktuellen Einstellungen als Basis: je Parameter werden
    //  die Werte unten durchlaufen, alle anderen bleiben auf der Basis. Rendering, Ground Truth und Maske
    //  der letzten Analyse werden wiederverwendet (TV-Parameter aendern die Bilder nicht); je Lauf wird
    //  nur der Fluss neu gerechnet (cv_main_async) und wie bei "Genauigkeit" ausgewertet.
    //  Ergebnis: Assets/analysis_results/param_study_latest.tsv (+ Kopie mit Zeitstempel), nach jedem Lauf
    //  gespeichert; Plots per scripts/plot_param_study.py in Assets/analysis_results/param_study_plots/.
    //  Am Ende werden die Einstellungen wiederhergestellt und der Basislauf erneut gerechnet.
    static readonly double[] STUDY_LAMBDA = { 0.01, 0.02, 0.05, 0.1, 0.2 };
    static readonly double[] STUDY_THETA = { 0.1, 0.2, 0.3, 0.55, 0.8 };
    static readonly int[] STUDY_NSCALES = { 1, 2, 3, 4, 5, 6, 7 };
    static readonly int[] STUDY_NWARPS = { 1, 2, 3, 5, 8 };
    static readonly double[] STUDY_EPSILON = { 1e-2, 1e-3, 1e-4, 1e-5, 1e-6 };
    static readonly float[] STUDY_TGV_RATIO = { 1f, 2f, 3f, 5f, 10f, 20f };

    class StudyConfig
    {
        public string group;
        public double lambda, theta, epsilon;
        public int nscales, nwarps, iterations;
        public bool tgv;
        public float ratio;
    }

    private bool param_study_running = false;
    private bool param_study_cancel = false;
    /// <summary>
    /// Returns whether the parameter study is running.
    /// </summary>
    /// <returns>True while the study runs.</returns>
    public bool is_param_study_running() { return param_study_running; }
    //27092026 Bugfix: statisch, damit auch bei "Reload Domain disabled" (Enter Play Mode Options) keine zweite
    //  Studie parallel starten kann - eine Studie aus einer frueheren Play-Sitzung lief sonst weiter, beide
    //  teilten GPU-Puffer und Ergebnisdateien (Nullfluss, identische falsche Kennzahlen)
    private static bool s_param_study_active = false;
    private static bool s_param_study_cancel = false;

    /// <summary>
    /// Folder of the study results (Assets/analysis_results/).
    /// </summary>
    /// <returns>Folder path.</returns>
    string param_study_dir() { return path_dic + "/analysis_results/"; }

    /// <summary>
    /// Runs the numerical parameter study on the current image pair: varies lambda, theta, pyramid levels, warps, stopping tolerance, and regulariser one at a time around the reference, evaluates each run like the accuracy analysis, and writes param_study_latest.tsv and plots. A second call stops after the current run.
    /// </summary>
    public async void run_param_study()
    {
        if (param_study_running || s_param_study_active)
        {
            param_study_cancel = true;
            s_param_study_cancel = true;
            ExperimentImageGallery.SetResultsText("Parameterstudie: Abbruch nach dem laufenden Durchgang ...");
            ExperimentImageGallery.ShowResultsWindow();
            return;
        }
        if (is_tv_running)
        {
            ExperimentImageGallery.SetResultsText("Parameterstudie: gerade laeuft eine TV-Berechnung - bitte warten.");
            ExperimentImageGallery.ShowResultsWindow();
            return;
        }
        if (get_is_started())
        {
            ExperimentImageGallery.SetResultsText("Parameterstudie: bitte warten, bis die laufende Analyse fertig ist.");
            ExperimentImageGallery.ShowResultsWindow();
            return;
        }
        if (!flow_results_available(out string missing))
        {
            ExperimentImageGallery.SetResultsText("Parameterstudie braucht eine fertige Analyse bei r" + get_render_res()
                + " (with_exp + with_tv -> Start). Fehlt: " + missing);
            ExperimentImageGallery.ShowResultsWindow();
            return;
        }
        string exp = get_experiment();
        string proj_dir = path_dic + remove_dots(exp);
        List<int> frames = get_frames();
        foreach (int f in frames)
        {
            string im = proj_dir + "/cam_0/uv/" + category + "im_" + f + "_r" + get_render_res() + ".png";
            if (!File.Exists(im))
            {
                ExperimentImageGallery.SetResultsText("Parameterstudie: gerendertes Bild fehlt (" + im
                    + "). Bitte zuerst eine Analyse mit diesen Frames und dieser Aufloesung rechnen.");
                ExperimentImageGallery.ShowResultsWindow();
                return;
            }
        }

        // Basis = aktuell wirksame Einstellungen
        (double lam0, double th0, int ns0, int nw0) = set_up_pars(true, exp);
        double eps0 = PAR_DEFAULT_EPSILON;
        int its0 = MAX_ITERATIONS;
        bool tgv0 = tv_use_tgv;
        float ratio0 = tgv_ratio;
        (double o_lam, double o_th, int o_ns, int o_nw, int o_its, double o_eps) = (tv_lambda_override,
            tv_theta_override, tv_nscales_override, tv_nwarps_override, tv_iterations_override, tv_epsilon_override);

        //27092026 nscales wird in find_displ begrenzt (groebste Stufe >= ca. 16 px): wirksamen Wert verwenden,
        //  damit Basis und Wertereihe zusammenpassen und keine wirkungslosen Werte gerechnet werden
        int res_now = get_render_res();
        int ns_cap = Math.Max(1, (int)(1 + Math.Log(Math.Sqrt(2.0) * res_now / 16.0) / Math.Log(1.0 / PAR_DEFAULT_ZFACTOR)));
        ns0 = Math.Min(ns0, ns_cap);

        //27092026 Nutzerwunsch: der gewaehlte (Basis-)Wert ist immer Teil jeder Wertereihe
        // Vergleich mit Toleranz: gespeicherte Werte sind float (0.05 -> 0.0500000007)
        Func<IEnumerable<double>, double, List<double>> with_base_d = (grid, b) =>
        {
            List<double> l = grid.ToList();
            if (!l.Any(x => Math.Abs(x - b) <= 1e-6 * Math.Max(Math.Abs(x), Math.Abs(b))))
                l.Add(b);
            return l.OrderBy(x => x).ToList();
        };
        Func<IEnumerable<int>, int, List<int>> with_base_i = (grid, b) =>
            grid.Concat(new[] { b }).Distinct().OrderBy(x => x).ToList();

        List<StudyConfig> configs = new List<StudyConfig>();
        Func<string, StudyConfig> basecfg = g => new StudyConfig { group = g, lambda = lam0, theta = th0, epsilon = eps0,
            nscales = ns0, nwarps = nw0, iterations = its0, tgv = tgv0, ratio = ratio0 };
        configs.Add(basecfg("basis"));
        foreach (double x in with_base_d(STUDY_LAMBDA, lam0)) { var c = basecfg("lambda"); c.lambda = x; configs.Add(c); }
        foreach (double x in with_base_d(STUDY_THETA, th0)) { var c = basecfg("theta"); c.theta = x; configs.Add(c); }
        foreach (int x in with_base_i(STUDY_NSCALES.Where(v => v <= ns_cap), ns0)) { var c = basecfg("nscales"); c.nscales = x; configs.Add(c); }
        foreach (int x in with_base_i(STUDY_NWARPS, nw0)) { var c = basecfg("nwarps"); c.nwarps = x; configs.Add(c); }
        foreach (double x in with_base_d(STUDY_EPSILON, eps0)) { var c = basecfg("epsilon"); c.epsilon = x; configs.Add(c); }
        { var c = basecfg("regularisierung"); c.tgv = false; configs.Add(c); }
        foreach (double x in tgv0 ? with_base_d(STUDY_TGV_RATIO.Select(v => (double)v), ratio0) : STUDY_TGV_RATIO.Select(v => (double)v).ToList())
        { var c = basecfg("regularisierung"); c.tgv = true; c.ratio = (float)x; configs.Add(c); }

        CultureInfo ci = CultureInfo.InvariantCulture;
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        Directory.CreateDirectory(param_study_dir());
        string tsv_latest = param_study_dir() + "param_study_latest.tsv";
        string tsv_stamp = param_study_dir() + "param_study_" + stamp + ".tsv";
        List<string> lines = new List<string>
        {
            "# Parameterstudie " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + " | " + exp + " | r" + get_render_res()
                + " | Frames " + describe_frames() + " | Speckle " + describe_speckle_texture()
                + " | Belichtung k " + analysis_exposure.ToString("0.###", ci) //28092026
                + " | Strain-sigma " + strain_sigma.ToString("0.##", ci) + " px",
            "# Basis: lambda " + lam0.ToString("G4", ci) + ", theta " + th0.ToString("G4", ci) + ", nscales " + ns0
                + ", nwarps " + nw0 + ", its " + its0 + ", eps " + eps0.ToString("G4", ci)
                + ", " + (tgv0 ? "TGV alpha0/alpha1 " + ratio0.ToString("G4", ci) : "TV"),
            string.Join("\t", new[] { "run", "group", "lambda", "theta", "nscales", "nwarps", "iterations", "epsilon",
                "regularization", "tgv_ratio", "time_s", "iterations_total",
                "u_bias", "u_rmse", "u_mae", "u_corr", "v_bias", "v_rmse", "v_mae", "v_corr",
                "exx_mae", "exx_rmse", "exx_corr", "exx_rel_mae", "eyy_mae", "eyy_rmse", "eyy_corr", "eyy_rel_mae" })
        };

        param_study_running = true;
        param_study_cancel = false;
        s_param_study_active = true;
        s_param_study_cancel = false;
        break_now = false;
        Stopwatch total = Stopwatch.StartNew();
        int done = 0;
        try
        {
            for (int k = 0; k < configs.Count; k++)
            {
                // Abbruch per Knopf, oder wenn diese Komponente beendet wurde (Play gestoppt)
                if (param_study_cancel || s_param_study_cancel || this == null || !isActiveAndEnabled || tv_gpu_shutdown)
                    break;
                StudyConfig c = configs[k];
                apply_study_config(c);
                string desc = describe_study_config(c);
                double eta = done > 0 ? total.Elapsed.TotalSeconds / done * (configs.Count - done) : double.NaN;
                ExperimentImageGallery.SetResultsText("Parameterstudie: Lauf " + (k + 1) + " / " + configs.Count
                    + " (" + c.group + ")\n" + desc
                    + (double.IsNaN(eta) ? "" : "\nRestzeit ca. " + (eta / 60.0).ToString("0.0", ci) + " min")
                    + "\n(Knopf \"Parameter: Neu\" erneut druecken = Abbruch nach diesem Lauf)");
                ExperimentImageGallery.ShowResultsWindow();

                //27092026 Fortschrittsbalken ueber die ganze Studie (Balken, Prozent und Restzeit beziehen sich
                //  auf alle Laeufe; die Zeile nennt Lauf k/N und die gerade variierte Gruppe)
                progress_prefix = "[Parameterstudie " + (k + 1) + "/" + configs.Count + ": " + c.group + "] ";
                set_series_progress_info(k + 1, configs.Count);

                Stopwatch sw = Stopwatch.StartNew();
                await cv_main_async(0, d_cam: 0, with_dt: true, exp_label: exp, pars: null, proj_dir_override: proj_dir);
                double secs = sw.Elapsed.TotalSeconds;
                long its_total = tv_stat_iterations;

                Dictionary<string, double> m = compute_study_metrics();
                Func<string, string> g = key => m.TryGetValue(key, out double val) ? val.ToString("G6", ci) : "NaN";
                lines.Add(string.Join("\t", new[] { (k + 1).ToString(), c.group, c.lambda.ToString("G6", ci),
                    c.theta.ToString("G6", ci), c.nscales.ToString(), c.nwarps.ToString(), c.iterations.ToString(),
                    c.epsilon.ToString("G6", ci), c.tgv ? "TGV" : "TV", c.tgv ? c.ratio.ToString("G6", ci) : "",
                    secs.ToString("F2", ci), its_total.ToString(),
                    g("u_bias"), g("u_rmse"), g("u_mae"), g("u_corr"), g("v_bias"), g("v_rmse"), g("v_mae"), g("v_corr"),
                    g("exx_mae"), g("exx_rmse"), g("exx_corr"), g("exx_rel_mae"),
                    g("eyy_mae"), g("eyy_rmse"), g("eyy_corr"), g("eyy_rel_mae") }));
                System.IO.File.WriteAllLines(tsv_latest, lines);
                System.IO.File.WriteAllLines(tsv_stamp, lines);
                done++;
                Debug.Log("Parameterstudie " + (k + 1) + "/" + configs.Count + ": " + desc + " -> u MAE "
                    + g("u_mae") + ", exx rel " + g("exx_rel_mae") + ", " + secs.ToString("F1", ci) + " s");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Parameterstudie abgebrochen: " + e);
            ExperimentImageGallery.SetResultsText("Parameterstudie abgebrochen: " + e.Message
                + "\nBisherige Ergebnisse: " + tsv_latest);
        }
        finally
        {
            // Einstellungen des Nutzers wiederherstellen
            set_tv_overrides(o_lam, o_th, o_ns, o_nw, o_its, o_eps);
            tv_use_tgv = tgv0;
            tgv_ratio = ratio0;
            param_study_running = false;
            s_param_study_active = false;
        }
        if (this == null || !isActiveAndEnabled || tv_gpu_shutdown)
        {
            progress_prefix = "";
            Debug.LogWarning("Parameterstudie beendet (Play gestoppt) nach " + done + " Laeufen.");
            return; // kein Basislauf mehr: die Szene ist nicht mehr aktiv
        }

        // Basislauf erneut rechnen, damit die normalen Ergebnisdateien wieder zu den Einstellungen passen
        try
        {
            ExperimentImageGallery.SetResultsText("Parameterstudie: " + done + " Laeufe fertig, Basislauf wird wiederhergestellt ...");
            progress_prefix = "[Parameterstudie: Basislauf wird wiederhergestellt] ";
            series_current_idx = 1;
            series_total_count = 1;
            progress_stopwatch.Reset();
            await cv_main_async(0, d_cam: 0, with_dt: true, exp_label: exp, pars: null, proj_dir_override: proj_dir);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Basislauf nach der Parameterstudie fehlgeschlagen: " + e.Message);
        }
        finally
        {
            progress_prefix = "";
        }

        Debug.Log("Parameterstudie fertig: " + done + " Laeufe in " + (total.Elapsed.TotalMinutes).ToString("0.0", ci)
            + " min -> " + tsv_latest);
        await show_param_study(regenerate_plots: true);
    }

    /// <summary>
    /// Applies the solver settings of one run of the parameter study.
    /// </summary>
    /// <param name="c">Settings of the run.</param>
    void apply_study_config(StudyConfig c)
    {
        tv_lambda_override = c.lambda;
        tv_theta_override = c.theta;
        tv_nscales_override = c.nscales;
        tv_nwarps_override = c.nwarps;
        tv_iterations_override = c.iterations;
        tv_epsilon_override = c.epsilon;
        tv_use_tgv = c.tgv;
        tgv_ratio = c.ratio;
    }

    /// <summary>
    /// Describes the solver settings of a run for the log.
    /// </summary>
    /// <param name="c">Settings of the run.</param>
    /// <returns>Description text.</returns>
    static string describe_study_config(StudyConfig c)
    {
        CultureInfo ci = CultureInfo.InvariantCulture;
        return "lambda " + c.lambda.ToString("G4", ci) + ", theta " + c.theta.ToString("G4", ci) + ", nscales " + c.nscales
            + ", nwarps " + c.nwarps + ", its " + c.iterations + ", eps " + c.epsilon.ToString("G3", ci)
            + ", " + (c.tgv ? "TGV " + c.ratio.ToString("G4", ci) : "TV");
    }

    //27092026 Kennzahlen eines Laufs (wie "Genauigkeit", ohne Karten): Fluss u/v gegen Ground Truth und
    //  Dehnungen exx/eyy (mit dem aktuellen Strain-sigma)
    /// <summary>
    /// Error measures of the current run without maps: flow u and v against the reference and strains exx and eyy with the current strain smoothing (bias, RMSE, MAE, correlation, relative MAE).
    /// </summary>
    /// <returns>Dictionary of named measures.</returns>
    Dictionary<string, double> compute_study_metrics()
    {
        Dictionary<string, double> r = new Dictionary<string, double>();
        (string dir_flow, int t_idx) = refresh_params(null, -1);
        string exp_dir = remove_dots(get_experiment());
        FileInfo[] files_u = new DirectoryInfo(dir_flow + exp_dir + "/time_flow_u/").GetFiles("*.*");
        FileInfo[] files_v = new DirectoryInfo(dir_flow + exp_dir + "/time_flow_v/").GetFiles("*.*");
        var flow_files = select_flow_files(files_u, files_v);
        (List<List<List<float>>> mats_u, List<List<List<float>>> mats_v, _, _, _, _) =
            find_flow_mats(flow_files, im_cnt: flow_files["u"].Count);
        (mats_u, mats_v) = scale_flows(mats_u, mats_v, scale_fac: this.flow_scale_factor);
        (List<List<float>> flow_u, List<List<float>> flow_v) = find_accum_flow(mats_u, mats_v, t_idx);
        (float[] d_xs, float[] d_ys, float[] d_zs) = load_distortion_ground_truth(blade_idx: t_idx);
        (int[,] tri_idx, float[][][] barys) = load_tris(blade_idx: 0);

        string mode_before = get_plot_mode();
        List<List<float>> vu, vv, gu, gv;
        try
        {
            set_plot_mode("value", is_internal: true);
            (vu, vv, _, _) = choose_flow_or_loss(flow_u, flow_v, tri_idx, barys, d_xs, d_ys, d_zs);
            set_plot_mode("value_ref", is_internal: true);
            (gu, gv, _, _) = choose_flow_or_loss(flow_u, flow_v, tri_idx, barys, d_xs, d_ys, d_zs);
        }
        finally
        {
            set_plot_mode(mode_before, is_internal: true);
        }

        //29092026 in den Sweeps die Rohkarten (TV und Ground Truth, float) je Stufe im Experimentordner ablegen,
        //  fuer die Manuskript-Abbildungen (scripts/make_paper_flow_figure.py); Format wie in save_accuracy_analysis
        if (analysis_sweep_running())
        {
            try
            {
                string raw_dir = dir_flow + exp_dir + "/";
                string rs = "_r" + get_render_res() + ".f32";
                write_raw_map(vu, raw_dir + "accuracy_raw_value_u" + rs);
                write_raw_map(vv, raw_dir + "accuracy_raw_value_v" + rs);
                write_raw_map(gu, raw_dir + "accuracy_raw_value_ref_u" + rs);
                write_raw_map(gv, raw_dir + "accuracy_raw_value_ref_v" + rs);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Sweep: Rohkarten nicht geschrieben: " + e.Message);
            }
        }

        void put(string key, List<List<float>> a, List<List<float>> b, bool rel)
        {
            (int n, double bias, double rmse, double mae, double corr, double gt_abs) = pair_stats(a, b);
            r[key + "_bias"] = bias;
            r[key + "_rmse"] = rmse;
            r[key + "_mae"] = mae;
            r[key + "_corr"] = corr;
            if (rel)
                r[key + "_rel_mae"] = gt_abs > 0 ? mae / gt_abs : double.NaN;
        }
        put("u", vu, gu, false);
        put("v", vv, gv, false);

        foreach (bool d_dx in new[] { true, false })
        {
            List<List<float>> dv = filter_mean_comp(smoothen_frame(d_dx ? vu : vv));
            List<List<float>> dg = filter_mean_comp(smoothen_frame(d_dx ? gu : gv));
            if (strain_sigma > 0f)
            {
                dv = gaussian_nan(dv, strain_sigma);
                dg = gaussian_nan(dg, strain_sigma);
            }
            (List<List<float>> e, List<List<float>> e_ref, _, _) = strain_maps(dv, dg, d_dx);
            put(d_dx ? "exx" : "eyy", e, e_ref, true);
        }
        return r;
    }

    //29092026 Nutzerwunsch: Stereo-Tiefe (Abb. 7) im neuen Ablauf. Der alte Stereo-Schritt (cv_main_async mit
    //  d_cam = 1) ist nicht mehr nutzbar: er schreibt in dieselben time_flow_*_0-Dateien wie der zeitliche Fluss
    //  (ueberschreibt ihn) und scheitert an der Bildpruefung (drittes Bild). Daher eigener Schritt:
    //  TV zwischen cam_0 und cam_1 im selben Frame (Position pos in blade_idxs, Standard 1 = verformter Frame),
    //  Umrechnung Disparitaet -> Abstand exakt wie read_heights_tv (switch_mat, unnorm_mat, mat_raw2dists mit
    //  Cosinus-Korrektur, transpose, mirror), Referenz wie load_heights_ref (gerenderte Tiefe, gleiche Position).
    //  Ausgabe: Rohkarten depth_tv/depth_ref/disparity (f32, Format wie write_raw_map) im Experimentordner und
    //  eine Zeile in analysis_results/depth_results.tsv. Fehler brechen die Analyse nicht ab.
    //29092026 Nutzerwunsch: Hoehenanalyse ueber eigene Buttons (run_stereo) statt automatisch nach jeder Analyse
    public bool stereo_depth_enabled = false;
    static bool stereo_running = false;

    //29092026 "Hoehe: Neu" (which = "normal": letzte normale Analyse, Ordner exp_normal) bzw. "Hoehe: Licht"
    //  (which = "lighting": alle Stufen aus lighting_sweep.tsv). Nutzt die vorhandenen Bilder beider Kameras,
    //  rechnet nur den Stereo-Schritt (TV mit den aktuellen Einstellungen) und schreibt depth_results.tsv.
    //30092026 Stufen fuer Hoehen- bzw. 3D-Flussanalyse (aus run_stereo ausgelagert, unveraendert)
    /// <summary>
    /// Stages for depth or 3D-displacement analysis: the standard analysis (normal) or all stages of the lighting, speckle, or noise table that have an experiment folder.
    /// </summary>
    /// <param name="which">normal, lighting, speckle, or noise.</param>
    /// <returns>List of (label, parameters).</returns>
    List<(string label, Params pars)> collect_stereo_jobs(string which)
    {
        CultureInfo ci = CultureInfo.InvariantCulture;
        var jobs = new List<(string label, Params pars)>();
        //29092026 auch fuer die Speckle- (und Rausch-)Stufen: Stufen aus der jeweiligen Sweep-Tabelle
        string key_col = which == "lighting" ? "lighting_intensity" : which == "speckle" ? "speckle_size"
            : which == "noise" ? "peak_electrons" : null;
        if (key_col != null)
        {
            string tsv = root_path + which + "_sweep.tsv";
            if (File.Exists(tsv))
            {
                string[] lines = File.ReadAllLines(tsv);
                int c_key = Array.IndexOf(lines[0].Split('\t'), key_col);
                var keyed = new List<(float key, string label, Params pars)>();
                foreach (string line in lines.Skip(1))
                {
                    string[] cells = line.Split('\t');
                    if (c_key < 0 || cells.Length <= c_key) continue;
                    float value;
                    if (cells[c_key] == "inf") value = 0f;
                    else if (!float.TryParse(cells[c_key], NumberStyles.Float, ci, out value)) continue;
                    if (!Directory.Exists(path_dic + remove_dots(cells[0]))) continue;
                    Params p = which == "lighting" ? new Params(lighting_intensity: value)
                        : which == "speckle" ? new Params(speckle_size: value, lighting_intensity: 1f)
                        : new Params(lighting_intensity: 1f, poisson_error: value);
                    keyed.Add((value, cells[0], p));
                }
                keyed.Sort((a, b) => a.key.CompareTo(b.key));
                foreach (var kj in keyed) jobs.Add((kj.label, kj.pars));
            }
        }
        else
        {
            //29092026 normale Analyse (exp_normal), sonst die Referenzstufe des Licht-Sweeps (I = 1) als Schnelltest
            string rs_l = get_render_res().ToString(ci);
            string f_l = blade_idxs[Math.Min(1, blade_idxs.Count - 1)].ToString(ci);
            if (File.Exists(path_dic + "exp_normal/cam_1/uv/" + category + "im_" + f_l + "_r" + rs_l + ".png"))
                jobs.Add(("exp_normal", null));
            else if (Directory.Exists(path_dic + "lighting_1"))
                jobs.Add(("lighting_1", new Params(lighting_intensity: 1f)));
        }
        return jobs;
    }

    /// <summary>
    /// Depth analysis (buttons Hoehe): computes the stereo depth for the selected stages, keeps the array-to-screen mapping of the reference stage for all stages, writes depth_results.tsv, and shows the map panels in the gallery.
    /// </summary>
    /// <param name="which">normal, lighting, speckle, or noise.</param>
    public async void run_stereo(string which)
    {
        if (stereo_running) { Debug.LogWarning("Hoehenanalyse laeuft bereits."); return; }
        if (get_is_started()) { Debug.LogWarning("Waehrend einer laufenden Analyse keine Hoehenanalyse."); return; }
        stereo_running = true;
        string exp_before = get_experiment();
        CultureInfo ci = CultureInfo.InvariantCulture;
        try
        {
            var jobs = collect_stereo_jobs(which); //30092026 ausgelagert, auch fuer run_scene_flow
            if (jobs.Count == 0)
            {
                ExperimentImageGallery.SetResultsText(which == "lighting"
                    ? "Keine Licht-Stufen gefunden - zuerst \"Licht: Neu\" laufen lassen."
                    : which == "speckle" ? "Keine Speckle-Stufen gefunden - zuerst \"Speckle: Neu\" laufen lassen."
                    : "Keine normale Analyse gefunden - zuerst \"Start\" laufen lassen.");
                ExperimentImageGallery.ShowResultsWindow();
                return;
            }
            var summary = new StringBuilder("Hoehenanalyse (Stereo cam_0/cam_1, Frame " + blade_idxs[Math.Min(1, blade_idxs.Count - 1)]
                + ", " + describe_regularization() + ")\nexperiment | MAE [mm] | rel. [%] | Relief [%] | Disp.-Fehler [px] | Selbsttest [mm]\n");
            //29092026 Referenzstufe (Lichtstaerke am naechsten an 1) zuerst rechnen und ihre Zuordnung fuer alle
            //  weiteren Stufen festhalten; Galerie/Zusammenfassung bleiben nach Lichtstaerke sortiert
            stereo_mapping_lock = null;
            var order = Enumerable.Range(0, jobs.Count).OrderBy(q => jobs[q].pars == null ? 0.0
                : Math.Abs(Math.Log(Math.Max(1e-9, jobs[q].pars.get_lighting_intensity()))))
                .ThenBy(q => Math.Abs(q - jobs.Count / 2)).ToList(); // Speckle/Rauschen: mittlere Stufe als Referenz
            var lines_by_job = new string[jobs.Count];
            for (int step = 0; step < order.Count; step++)
            {
                int k = order[step];
                //29092026 Play-Modus beendet (Kameras zerstoert, laeuft wegen "Reload Domain disabled" sonst weiter) -> abbrechen
                if (this == null || !isActiveAndEnabled || cam_for_uv_0 == null || cam_for_uv_1 == null)
                {
                    Debug.LogWarning("Hoehenanalyse abgebrochen: Play-Modus beendet bzw. Kameras nicht mehr vorhanden.");
                    break;
                }
                ExperimentImageGallery.SetResultsText("Hoehenanalyse " + (step + 1) + " / " + jobs.Count + ": " + jobs[k].label);
                set_overall_progress(step + 1, jobs.Count, "Höhenanalyse", jobs[k].label); //29092026 Gesamtfortschritt
                set_experiment(jobs[k].label);
                stereo_last_mapping = null;
                await stereo_depth_step(path_dic + remove_dots(jobs[k].label), jobs[k].pars);
                if (step == 0 && stereo_last_mapping.HasValue)
                {
                    stereo_mapping_lock = stereo_last_mapping;
                    Debug.Log("Hoehenanalyse: Zuordnung o" + stereo_mapping_lock.Value.o + "m" + stereo_mapping_lock.Value.m
                        + " an " + jobs[k].label + " bestimmt und fuer alle Stufen festgehalten.");
                }
                string last = File.Exists(root_path + "depth_results.tsv") ? File.ReadLines(root_path + "depth_results.tsv").Last() : "";
                string[] c = last.Split('\t');
                if (c.Length > 10 && c[0] == jobs[k].label)
                    lines_by_job[k] = c[0] + " | " + fmt_num(c[5], 1) + " | " + fmt_num(c[7], 100) + " | " + fmt_num(c[8], 100)
                        + " | " + fmt_num(c[9], 1) + " | " + fmt_num(c[10], 1) + "\n";
            }
            stereo_mapping_lock = null;
            foreach (string l in lines_by_job) if (l != null) summary.Append(l);
            summary.Append("Tabelle: " + root_path + "depth_results.tsv");

            //29092026 Nutzerwunsch: Karten direkt ansehen - je Stufe ein Panel (Tiefe TV, Referenz, Fehler,
            //  Disparitaetsfehler, exakte Disparitaet) per scripts/plot_depth_maps.py, vorne in der Galerie
            //  (Reihenfolge wie die Stufen, "Weiter" = naechste Stufe)
            string rs_p = get_render_res().ToString(ci);
            List<string> dirs = jobs.Select(j => path_dic + remove_dots(j.label)).ToList();
            string args = rs_p + " " + string.Join(" ", dirs.Select(d => "\"" + d + "\""));
            ExperimentImageGallery.SetResultsText(summary.ToString() + "\nKarten werden erzeugt ...");
            string plot_msg = await Task.Run(() => run_python(path_project + "scripts/plot_depth_maps.py", args));
            int ins = 0;
            foreach (string d in dirs)
            {
                string png = d + "/depth_maps_r" + rs_p + ".png";
                if (File.Exists(png)) ExperimentImageGallery.InsertOrMoveImage(png, ins++);
            }
            if (ins > 0)
            {
                ExperimentImageGallery.ShowFirst();
                summary.Append("\nGalerie: Kartenpanel je Stufe (Weiter = naechste Stufe).");
            }
            if (plot_msg != "") summary.Append("\n" + plot_msg);
            ExperimentImageGallery.SetResultsText(summary.ToString());
            ExperimentImageGallery.ShowResultsWindow();
        }
        catch (Exception e)
        {
            Debug.LogError("Hoehenanalyse abgebrochen: " + e);
        }
        finally
        {
            set_experiment(exp_before);
            stereo_running = false;
            if (is_tv_running) finish_all_tv_progress();
            reset_overall_progress();
        }
    }

    /// <summary>
    /// Formats a numeric table cell, optionally scaled.
    /// </summary>
    /// <param name="s">Cell text.</param>
    /// <param name="factor">Scale factor (e.g. 100 for percent).</param>
    /// <returns>Formatted number, or the input if it is not a number.</returns>
    static string fmt_num(string s, double factor)
    {
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
            ? (d * factor).ToString("0.###", CultureInfo.InvariantCulture) : s;
    }
    //29092026 alte Umrechnung (mat_raw2dists) - passt nicht mehr zum Kameraaufbau seit 18.09.2026 (Kameras um die
    //  globale x-Achse um -10/+10 Grad gedreht, 180 Grad gerollt, FOV 20 statt 40 Grad, cam_angle_1 - cam_angle_0 = 0
    //  -> NaN). Nur noch zur Nachvollziehbarkeit; ersetzt durch stereo_depth_step (Triangulation, s.u.).
    const string DEPTH_HEADER_LEGACY = "experiment\tlighting_intensity\tpeak_electrons\tspeckle_size\tframe\tdepth_mae_mm"
        + "\tdepth_bias_mm\tdepth_rel_mae\tdepth_rel_relief\tn_px\tref_mean_mm\trender_res\tregularization\tstrain_sigma";

    /// <summary>
    /// Legacy depth computation from the disparity with the old angle formula (does not match the current camera setup; kept for traceability).
    /// </summary>
    /// <param name="proj_dir">Experiment folder.</param>
    /// <param name="pars">Parameters of the stage.</param>
    /// <param name="pos">Frame position.</param>
    async Task stereo_depth_step_legacy(string proj_dir, Params pars, int pos = 1)
    {
        const string DEPTH_HEADER = DEPTH_HEADER_LEGACY;
        CultureInfo ci = CultureInfo.InvariantCulture;
        int t_before = get_t_idx();
        try
        {
            pos = Math.Max(0, Math.Min(pos, blade_idxs.Count - 1));
            int frame = blade_idxs[pos];
            string rs = get_render_res().ToString(ci);
            string im0 = proj_dir + "/cam_0/uv/" + category + "im_" + frame + "_r" + rs + ".png";
            string im1 = proj_dir + "/cam_1/uv/" + category + "im_" + frame + "_r" + rs + ".png";
            if (!File.Exists(im0) || !File.Exists(im1))
            {
                Debug.LogWarning("Stereo: Kamerabilder fehlen (" + im0 + " / " + im1 + ")");
                return;
            }
            im_dressed imd_0 = manage_read_im(im0);
            im_dressed imd_1 = manage_read_im(im1);
            List<List<float>> I0 = new List<List<float>> { imd_0.im_vec, imd_0.im_vec };
            List<List<float>> I1 = new List<List<float>> { imd_1.im_vec, imd_1.im_vec };
            (PAR_DEFAULT_LAMBDA, PAR_DEFAULT_THETA, PAR_DEFAULT_NSCALES, PAR_DEFAULT_NWARPS) =
                set_up_pars(false, get_experiment() + "_heights");
            int n_x = imd_0.width, n_y = imd_0.height;
            (List<List<List<float>>> u_mat, _, List<float> min_u, _, List<float> max_u, _) = await Task.Run(() =>
                compute_ims(I0, I1, n_x, n_y, scale_fac: 1, with_dt: false));
            if (u_mat == null) return;

            // Disparitaet -> Abstand wie read_heights_tv (dort ueber save_floats2/load_floats2 = Identitaet)
            List<List<float>> disp = unnorm_mat(switch_mat(copy_mat(u_mat[0])), min_u[0], max_u[0]);
            List<List<float>> depth = mirror_mat(transpose_mat(mat_raw2dists(copy_mat(disp))), idx: "i");
            set_t_idx(pos);
            (_, List<List<float>> depth_ref) = load_heights_ref();

            // Kennzahlen auf der Probe: Referenz gueltig und nahe ihrem Median (Tisch/Hintergrund liegen weit weg)
            List<float> refs = new List<float>();
            foreach (List<float> row in depth_ref)
                foreach (float x in row)
                    if (!float.IsNaN(x) && !float.IsInfinity(x) && x > 0f) refs.Add(x);
            refs.Sort();
            double med = refs.Count > 0 ? refs[refs.Count / 2] : double.NaN;
            int n = 0;
            double s_ad = 0, s_d = 0, s_rel = 0, s_ref = 0;
            var inside = new List<(double t, double g)>();
            int ni = Math.Min(depth.Count, depth_ref.Count), nj = Math.Min(depth[0].Count, depth_ref[0].Count);
            for (int i = 20; i < ni - 20; i++)
                for (int j = 20; j < nj - 20; j++)
                {
                    double t = depth[i][j], g = depth_ref[i][j];
                    if (double.IsNaN(t) || double.IsNaN(g) || double.IsInfinity(t) || g <= 0 || Math.Abs(g - med) > 0.2 * med)
                        continue;
                    n++; s_ad += Math.Abs(t - g); s_d += t - g; s_rel += Math.Abs(t - g) / g; s_ref += g;
                    inside.Add((t, g));
                }
            double mae = n > 0 ? s_ad / n : double.NaN, ref_mean = n > 0 ? s_ref / n : double.NaN;
            double relief = 0;
            foreach (var p in inside) relief += Math.Abs(p.g - ref_mean);
            relief = n > 0 ? relief / n : double.NaN; // mittlere Abweichung der Referenz von ihrem Mittel (Relief)

            string exp_dir = proj_dir.EndsWith("/") ? proj_dir : proj_dir + "/";
            write_raw_map(depth, exp_dir + "depth_tv_r" + rs + ".f32");
            write_raw_map(depth_ref, exp_dir + "depth_ref_r" + rs + ".f32");
            write_raw_map(disp, exp_dir + "disparity_r" + rs + ".f32");

            string tsv = root_path + "depth_results.tsv";
            if (!File.Exists(tsv) || File.ReadLines(tsv).FirstOrDefault() != DEPTH_HEADER)
            {
                if (File.Exists(tsv))
                    File.Copy(tsv, root_path + "depth_results_before_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".tsv", true);
                File.WriteAllText(tsv, DEPTH_HEADER + "\n");
            }
            Func<float, string> f = v => float.IsNaN(v) ? "NaN" : v.ToString("G6", ci);
            string row_s = get_experiment() + "\t" + f(pars == null ? float.NaN : pars.get_lighting_intensity())
                + "\t" + f(pars == null ? float.NaN : pars.get_poisson_error())
                + "\t" + f(pars == null ? float.NaN : pars.get_speckle_size()) + "\t" + frame
                + "\t" + mae.ToString("G6", ci) + "\t" + (n > 0 ? s_d / n : double.NaN).ToString("G6", ci)
                + "\t" + (n > 0 ? s_rel / n : double.NaN).ToString("G6", ci)
                + "\t" + (relief > 0 ? mae / relief : double.NaN).ToString("G6", ci) + "\t" + n
                + "\t" + ref_mean.ToString("G6", ci) + "\t" + sweep_setup_key();
            write_to_txt(tsv, row_s, mode: "append");
            Debug.Log("Stereo-Tiefe " + get_experiment() + " (Frame " + frame + "): MAE " + mae.ToString("0.###", ci)
                + " mm, rel. " + (n > 0 ? 100 * s_rel / n : double.NaN).ToString("0.###", ci) + " %, bezogen aufs Relief "
                + (relief > 0 ? 100 * mae / relief : double.NaN).ToString("0.#", ci) + " % (" + n + " Px, Referenz im Mittel "
                + ref_mean.ToString("0.#", ci) + " mm)");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Stereo-Tiefe nicht berechnet (" + get_experiment() + "): " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            set_t_idx(t_before);
        }
    }

    // ============================================================================================
    //29092026 Stereo-Tiefe per Triangulation mit den tatsaechlichen Unity-Kameras (ersetzt mat_raw2dists).
    //  Ground Truth (live aus der Szene, je Frame-Position zwischengespeichert): Strahl von cam_0 durch jedes Pixel
    //  auf das Mesh (Layer 7) -> Oberflaechenpunkt P, Abstand |P - cam_0|, exakte Disparitaet = Projektion von P in
    //  cam_1 minus Pixel in cam_0. Alles viewport-basiert in Bildschirmkoordinaten (x rechts, y oben).
    //  TV-Stereofluss cam_0 -> cam_1: die Zuordnung TV-Array -> Bildschirm (Orientierung, Komponenten, Vorzeichen;
    //  64 Moeglichkeiten) wird gegen die exakte Disparitaet bestimmt und protokolliert.
    //  Tiefe: Mittelpunkt der kuerzesten Verbindung der Sehstrahlen (cam_0 durch p, cam_1 durch p + d).
    //  Selbsttest: exakte Disparitaet durch dieselbe Triangulation -> muss die Ground Truth treffen.
    //  Einheiten: Unity-Einheiten / (Skalierung der Probe) = mm (Mesh in mm).
    const string DEPTH_HEADER = "experiment\tlighting_intensity\tpeak_electrons\tspeckle_size\tframe\tdepth_mae_mm"
        + "\tdepth_bias_mm\tdepth_rel_mae\tdepth_rel_relief\tdisparity_epe_px\tselftest_mae_mm\tn_px\tref_mean_mm"
        + "\tmapping\trender_res\tregularization\tstrain_sigma";
    string stereo_gt_key = null;
    float[,] stereo_gt_dist, stereo_gt_dx, stereo_gt_dy;
    float stereo_units_per_mm = 1f;
    double stereo_selftest_units = double.NaN, stereo_roundtrip_px = double.NaN;
    (int o, int m)? stereo_mapping_lock = null;
    (int o, int m)? stereo_last_mapping = null;

    //29092026 Projektion und Triangulation direkt ueber die Kameramatrizen (projectionMatrix * worldToCameraMatrix,
    //  wie beim Rendern), doppelte Genauigkeit. Grund: WorldToViewportPoint/ViewportPointToRay ergaben eine
    //  konstante Querverschiebung von 3.2 px gegenueber den Bildern und einen Selbsttestfehler von 0.16 mm.
    /// <summary>
    /// Camera matrix P*V (projection times world-to-camera) in double precision, as used for rendering.
    /// </summary>
    /// <param name="c">Camera.</param>
    /// <returns>4x4 matrix.</returns>
    static double[,] cam_matrix(Camera c)
    {
        UnityEngine.Matrix4x4 m = c.projectionMatrix * c.worldToCameraMatrix; // explizit: auch System.Numerics ist eingebunden
        var r = new double[4, 4];
        for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) r[i, j] = m[i, j];
        return r;
    }
    //29092026 4x4-Inverse in doppelter Genauigkeit (Gauss-Jordan). Die float-Inverse der Projektionsmatrix
    //  (near 0.3, sehr grosser far) war zu ungenau: Treffer lagen 1.6 px neben der Pixelmitte.
    /// <summary>
    /// Inverse of a 4x4 matrix in double precision (Gauss-Jordan with pivoting).
    /// </summary>
    /// <param name="M">Matrix to invert.</param>
    /// <returns>Inverse matrix.</returns>
    static double[,] invert4(double[,] M)
    {
        var a = new double[4, 8];
        for (int i = 0; i < 4; i++) { for (int j = 0; j < 4; j++) a[i, j] = M[i, j]; a[i, 4 + i] = 1; }
        for (int c = 0; c < 4; c++)
        {
            int p = c;
            for (int r = c + 1; r < 4; r++) if (Math.Abs(a[r, c]) > Math.Abs(a[p, c])) p = r;
            if (p != c) for (int k = 0; k < 8; k++) { double t = a[c, k]; a[c, k] = a[p, k]; a[p, k] = t; }
            double d = a[c, c];
            for (int k = 0; k < 8; k++) a[c, k] /= d;
            for (int r = 0; r < 4; r++)
            {
                if (r == c) continue;
                double f = a[r, c];
                if (f != 0) for (int k = 0; k < 8; k++) a[r, k] -= f * a[c, k];
            }
        }
        var inv = new double[4, 4];
        for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) inv[i, j] = a[i, 4 + j];
        return inv;
    }
    /// <summary>
    /// Transforms a point from normalised device coordinates back to world coordinates.
    /// </summary>
    /// <param name="inv">Inverse camera matrix.</param>
    /// <param name="u">x in NDC (-1..1).</param>
    /// <param name="v">y in NDC (-1..1).</param>
    /// <param name="z">Depth in NDC (-1 = near plane).</param>
    /// <returns>World point.</returns>
    static Vector3 unproject(double[,] inv, double u, double v, double z)
    {
        double[] q = { u, v, z, 1 };
        double[] r = new double[4];
        for (int i = 0; i < 4; i++) { double s = 0; for (int k = 0; k < 4; k++) s += inv[i, k] * q[k]; r[i] = s; }
        return new Vector3((float)(r[0] / r[3]), (float)(r[1] / r[3]), (float)(r[2] / r[3]));
    }

    //30092026 Pixelstrahl komplett in doppelter Genauigkeit: zwei Punkte desselben NDC-Pixels (Near-Ebene z=-1 und
    //  z=0) entprojizieren und die Richtung aus ihrer double-Differenz bilden. Vorher: Richtung = float(p_near) - c0 mit
    //  p_near nur ~0.3 Einheiten vor der Kamera bei Weltkoordinaten ~1e3 -> Rundung verdreht den Strahl um ~1e-3 rad
    //  (Log "Abweichung Pixelmitte <-> Matrix-Projektion" 3.2 px statt ~0).
    /// <summary>
    /// Ray through a pixel, built entirely in double precision from two unprojected points of the same pixel (near plane and z = 0); every point of the ray projects exactly onto the pixel centre.
    /// </summary>
    /// <param name="inv">Inverse camera matrix.</param>
    /// <param name="u_ndc">x of the pixel centre in NDC.</param>
    /// <param name="v_ndc">y of the pixel centre in NDC.</param>
    /// <returns>Ray in world coordinates.</returns>
    static Ray pixel_ray(double[,] inv, double u_ndc, double v_ndc)
    {
        double[] a = new double[3], b = new double[3];
        foreach (var (z, dst) in new[] { (-1.0, a), (0.0, b) })
        {
            double[] q = { u_ndc, v_ndc, z, 1 };
            double[] r = new double[4];
            for (int i = 0; i < 4; i++) { double s = 0; for (int k = 0; k < 4; k++) s += inv[i, k] * q[k]; r[i] = s; }
            for (int i = 0; i < 3; i++) dst[i] = r[i] / r[3];
        }
        double dx = b[0] - a[0], dy = b[1] - a[1], dz = b[2] - a[2], dn = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        return new Ray(new Vector3((float)a[0], (float)a[1], (float)a[2]), new Vector3((float)(dx / dn), (float)(dy / dn), (float)(dz / dn)));
    }

    /// <summary>
    /// Projects a world point with a camera matrix to viewport coordinates.
    /// </summary>
    /// <param name="M">Camera matrix P*V.</param>
    /// <param name="P">World point.</param>
    /// <returns>Viewport coordinates (x, y) in 0..1.</returns>
    static (double x, double y) project_vp(double[,] M, Vector3 P)
    {
        double cx = M[0, 0] * P.x + M[0, 1] * P.y + M[0, 2] * P.z + M[0, 3];
        double cy = M[1, 0] * P.x + M[1, 1] * P.y + M[1, 2] * P.z + M[1, 3];
        double cw = M[3, 0] * P.x + M[3, 1] * P.y + M[3, 2] * P.z + M[3, 3];
        return (0.5 * (cx / cw + 1.0), 0.5 * (cy / cw + 1.0));
    }
    // lineare Triangulation (DLT, kleinste Quadrate) aus zwei Viewport-Punkten
    /// <summary>
    /// Linear triangulation (DLT, least squares) of a point from its viewport positions in two cameras.
    /// </summary>
    /// <param name="M0">Camera matrix of camera 0.</param>
    /// <param name="x0">Viewport x in camera 0.</param>
    /// <param name="y0">Viewport y in camera 0.</param>
    /// <param name="M1">Camera matrix of camera 1.</param>
    /// <param name="x1">Viewport x in camera 1.</param>
    /// <param name="y1">Viewport y in camera 1.</param>
    /// <returns>World point (X, Y, Z), NaN if degenerate.</returns>
    static (double X, double Y, double Z) triangulate_dlt(double[,] M0, double x0, double y0, double[,] M1, double x1, double y1)
    {
        var A = new double[4, 4];
        void rows(double[,] M, double x, double y, int r)
        {
            double u = 2 * x - 1, v = 2 * y - 1;
            for (int k = 0; k < 4; k++) { A[r, k] = u * M[3, k] - M[0, k]; A[r + 1, k] = v * M[3, k] - M[1, k]; }
        }
        rows(M0, x0, y0, 0);
        rows(M1, x1, y1, 2);
        // Normalgleichungen (A[:, 0..2])^T A[:, 0..2] X = -(A[:, 0..2])^T A[:, 3]
        var N = new double[3, 3]; var b = new double[3];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++) { double s = 0; for (int r = 0; r < 4; r++) s += A[r, i] * A[r, j]; N[i, j] = s; }
            double t = 0; for (int r = 0; r < 4; r++) t -= A[r, i] * A[r, 3]; b[i] = t;
        }
        double det = N[0, 0] * (N[1, 1] * N[2, 2] - N[1, 2] * N[2, 1]) - N[0, 1] * (N[1, 0] * N[2, 2] - N[1, 2] * N[2, 0])
                   + N[0, 2] * (N[1, 0] * N[2, 1] - N[1, 1] * N[2, 0]);
        if (Math.Abs(det) < 1e-300) return (double.NaN, double.NaN, double.NaN);
        double Dx = b[0] * (N[1, 1] * N[2, 2] - N[1, 2] * N[2, 1]) - N[0, 1] * (b[1] * N[2, 2] - N[1, 2] * b[2]) + N[0, 2] * (b[1] * N[2, 1] - N[1, 1] * b[2]);
        double Dy = N[0, 0] * (b[1] * N[2, 2] - N[1, 2] * b[2]) - b[0] * (N[1, 0] * N[2, 2] - N[1, 2] * N[2, 0]) + N[0, 2] * (N[1, 0] * b[2] - b[1] * N[2, 0]);
        double Dz = N[0, 0] * (N[1, 1] * b[2] - b[1] * N[2, 1]) - N[0, 1] * (N[1, 0] * b[2] - b[1] * N[2, 0]) + b[0] * (N[1, 0] * N[2, 1] - N[1, 1] * N[2, 0]);
        return (Dx / det, Dy / det, Dz / det);
    }

    /// <summary>
    /// Midpoint of the shortest connection between two rays.
    /// </summary>
    /// <param name="a">First ray (normalised direction).</param>
    /// <param name="b">Second ray (normalised direction).</param>
    /// <returns>Midpoint, NaN for parallel rays.</returns>
    static Vector3 ray_midpoint(Ray a, Ray b)
    {
        Vector3 w0 = a.origin - b.origin;
        double B = Vector3.Dot(a.direction, b.direction), D = Vector3.Dot(a.direction, w0), E = Vector3.Dot(b.direction, w0);
        double den = 1.0 - B * B; // Richtungen sind normiert
        if (Math.Abs(den) < 1e-12) return new Vector3(float.NaN, float.NaN, float.NaN);
        double s = (B * E - D) / den, t = (E - B * D) / den;
        return 0.5f * (a.origin + (float)s * a.direction + b.origin + (float)t * b.direction);
    }

    /// <summary>
    /// Reference for the stereo depth: ray casting per pixel of camera 0 onto a freshly loaded mesh of the frame gives the distance and the exact disparity; includes self-tests (cached per frame, resolution, and camera pose).
    /// </summary>
    /// <param name="pos">Frame position.</param>
    /// <param name="W">Image width.</param>
    /// <param name="H">Image height.</param>
    /// <returns>True if the reference could be computed.</returns>
    bool stereo_ground_truth(int pos, int W, int H)
    {
        string key = pos + "_" + W + "x" + H + "_" + cam_for_uv_0.transform.position + cam_for_uv_1.transform.position
            + cam_for_uv_0.fieldOfView;
        if (stereo_gt_key == key) return true;
        //29092026 Mesh des Frames unabhaengig vom Szenenzustand frisch laden (gleiche Platzierung wie beim Rendern,
        //  load_blade_from_verts), fuer die Strahlen verwenden und danach wieder entfernen; alle uebrigen Proben
        //  waehrenddessen deaktivieren, damit kein Strahl sie trifft
        List<GameObject> blades = collect_blades();
        List<bool> active = blades.Select(b => b.activeSelf).ToList();
        GameObject blade = null;
        try
        {
            string blade_path = blade_path_for_idx(blade_idxs[pos]);
            blade = load_blade_from_verts(blade_path, blade_idx: -1, with_uv_init: false, with_collider: false);
            blade.name = "stereo_gt_mesh_" + blade_idxs[pos];
            for (int k = 0; k < blades.Count; k++) blades[k].SetActive(false);
            blade.SetActive(true);
            MeshCollider mc = blade.GetComponent<MeshCollider>();
            if (mc == null) mc = blade.AddComponent<MeshCollider>();
            mc.sharedMesh = blade.GetComponent<MeshFilter>().sharedMesh;
            blade.layer = 7;
            Physics.SyncTransforms();
            stereo_gt_dist = new float[W, H]; stereo_gt_dx = new float[W, H]; stereo_gt_dy = new float[W, H];
            Vector3 c0 = cam_for_uv_0.cameraToWorldMatrix.MultiplyPoint(Vector3.zero);
            double[,] M0 = cam_matrix(cam_for_uv_0), M1 = cam_matrix(cam_for_uv_1);
            double[,] inv0d = invert4(M0);
            int hits = 0;
            double s_self = 0, s_rt = 0;
            for (int sx = 0; sx < W; sx++)
                for (int sy = 0; sy < H; sy++)
                {
                    Vector3 vp0 = new Vector3((sx + 0.5f) / W, (sy + 0.5f) / H, 0f);
                    //29092026 Strahl aus der inversen Kameramatrix (ViewportPointToRay weicht hier um ~3.2 px ab)
                    double u_ndc = 2.0 * (sx + 0.5) / W - 1.0, v_ndc = 2.0 * (sy + 0.5) / H - 1.0;
                    // zwei Punkte des Pixelstrahls: Kamerazentrum (exakt) und ein Punkt auf der Near-Ebene
                    //30092026 Strahl in doppelter Genauigkeit (pixel_ray), vorher float(p_near) - c0 -> 3.2 px Versatz
                    Ray r = pixel_ray(inv0d, u_ndc, v_ndc);
                    if (Physics.Raycast(r, out RaycastHit hit, Mathf.Infinity, 1 << 7))
                    {
                        // Disparitaet ueber die Kameramatrizen, bezogen auf die Matrix-Projektion in cam_0
                        (double x0, double y0) = project_vp(M0, hit.point);
                        (double x1, double y1) = project_vp(M1, hit.point);
                        stereo_gt_dist[sx, sy] = (hit.point - c0).magnitude;
                        stereo_gt_dx[sx, sy] = (float)((x1 - x0) * W);
                        stereo_gt_dy[sx, sy] = (float)((y1 - y0) * H);
                        s_rt += Math.Sqrt((x0 - vp0.x) * (x0 - vp0.x) * W * W + (y0 - vp0.y) * (y0 - vp0.y) * H * H);
                        (double X, double Y, double Z) = triangulate_dlt(M0, x0, y0, M1, x1, y1);
                        s_self += Math.Sqrt((X - hit.point.x) * (X - hit.point.x) + (Y - hit.point.y) * (Y - hit.point.y) + (Z - hit.point.z) * (Z - hit.point.z));
                        hits++;
                    }
                    else
                        stereo_gt_dist[sx, sy] = stereo_gt_dx[sx, sy] = stereo_gt_dy[sx, sy] = float.NaN;
                }
            stereo_selftest_units = hits > 0 ? s_self / hits : double.NaN;
            stereo_roundtrip_px = hits > 0 ? s_rt / hits : double.NaN;
            stereo_units_per_mm = Mathf.Abs(blade.transform.lossyScale.x) > 0f ? Mathf.Abs(blade.transform.lossyScale.x) : 1f;
            stereo_gt_key = key;
            Debug.Log("Stereo-GT (Position " + pos + ", Frame " + blade_idxs[pos] + "): " + hits + " Pixel auf der Probe, "
                + "Skalierung " + stereo_units_per_mm.ToString("G4", CultureInfo.InvariantCulture) + " Einheiten/mm, "
                + "Kamerabstand " + ((cam_for_uv_1.transform.position - c0).magnitude / stereo_units_per_mm).ToString("0.#", CultureInfo.InvariantCulture)
                + " mm (Basis), FOV " + cam_for_uv_0.fieldOfView.ToString("0.##", CultureInfo.InvariantCulture) + " Grad"
                + " | Selbsttest (Matrix-Triangulation der exakten Disparitaet) "
                + (stereo_selftest_units / stereo_units_per_mm).ToString("0.######", CultureInfo.InvariantCulture) + " mm"
                + " | Abweichung Pixelmitte <-> Matrix-Projektion des Treffers in cam_0 (sollte ~0 sein) "
                + stereo_roundtrip_px.ToString("0.####", CultureInfo.InvariantCulture) + " px");
            return hits > 0;
        }
        catch (Exception e)
        {
            Debug.LogError("Stereo-GT: Mesh fuer Frame " + blade_idxs[pos] + " nicht geladen/ausgewertet: " + e.Message);
            return false;
        }
        finally
        {
            if (blade != null)
            {
                blade.SetActive(false);
                Destroy(blade);
            }
            for (int k = 0; k < blades.Count; k++) blades[k].SetActive(active[k]);
        }
    }

    /// <summary>
    /// Stereo depth of one stage: TV flow from camera 0 to camera 1, mapping of the flow array to screen axes against the exact disparity, DLT triangulation per pixel, comparison with the reference, raw maps and a row of depth_results.tsv.
    /// </summary>
    /// <param name="proj_dir">Experiment folder.</param>
    /// <param name="pars">Parameters of the stage.</param>
    /// <param name="pos">Frame position (default 1 = second frame).</param>
    async Task stereo_depth_step(string proj_dir, Params pars, int pos = 1)
    {
        CultureInfo ci = CultureInfo.InvariantCulture;
        try
        {
            pos = Math.Max(0, Math.Min(pos, blade_idxs.Count - 1));
            int frame = blade_idxs[pos];
            string rs = get_render_res().ToString(ci);
            string im0 = proj_dir + "/cam_0/uv/" + category + "im_" + frame + "_r" + rs + ".png";
            string im1 = proj_dir + "/cam_1/uv/" + category + "im_" + frame + "_r" + rs + ".png";
            if (!File.Exists(im0) || !File.Exists(im1))
            {
                Debug.LogWarning("Stereo: Kamerabilder fehlen (" + im0 + " / " + im1 + ")");
                return;
            }
            im_dressed imd_0 = manage_read_im(im0);
            im_dressed imd_1 = manage_read_im(im1);
            int W = imd_0.width, H = imd_0.height;
            if (!stereo_ground_truth(pos, W, H)) return;

            List<List<float>> I0 = new List<List<float>> { imd_0.im_vec, imd_0.im_vec };
            List<List<float>> I1 = new List<List<float>> { imd_1.im_vec, imd_1.im_vec };
            (PAR_DEFAULT_LAMBDA, PAR_DEFAULT_THETA, PAR_DEFAULT_NSCALES, PAR_DEFAULT_NWARPS) =
                set_up_pars(false, get_experiment() + "_heights");
            (List<List<List<float>>> u_mat, List<List<List<float>>> v_mat, List<float> min_u, List<float> min_v,
                List<float> max_u, List<float> max_v) = await Task.Run(() => compute_ims(I0, I1, W, H, scale_fac: 1, with_dt: false));
            if (u_mat == null) return;
            int ni = u_mat[0].Count, nj = u_mat[0][0].Count;
            float[,] U = new float[ni, nj], V = new float[ni, nj];
            for (int i = 0; i < ni; i++)
                for (int j = 0; j < nj; j++)
                {
                    // find_displ normiert auf [0, 1]; bei konstantem Feld (z.B. schwarzes Bild -> Fluss 0) ergibt die
                    // Normierung 0/0 = NaN -> dann den konstanten Wert selbst nehmen (Tiefe wird berechnet, Fehler gross)
                    U[i, j] = max_u[0] > min_u[0] ? min_u[0] + u_mat[0][i][j] * (max_u[0] - min_u[0]) : min_u[0];
                    V[i, j] = max_v[0] > min_v[0] ? min_v[0] + v_mat[0][i][j] * (max_v[0] - min_v[0]) : min_v[0];
                }

            // Zuordnung TV-Array -> Bildschirm gegen die exakte Disparitaet bestimmen
            Func<int, int, int, (int, int)> idx = (o, sx, sy) =>
            {
                int a = (o & 2) != 0 ? W - 1 - sx : sx, b = (o & 4) != 0 ? H - 1 - sy : sy;
                return (o & 1) != 0 ? (b, a) : (a, b);
            };
            Func<int, int, int, int, (float, float)> vec = (o, m, sx, sy) =>
            {
                (int i, int j) = idx(o, sx, sy);
                if (i < 0 || j < 0 || i >= ni || j >= nj) return (float.NaN, float.NaN);
                float p = U[i, j], q = V[i, j];
                (float x, float y) = (m & 1) != 0 ? (q, p) : (p, q);
                return ((m & 2) != 0 ? -x : x, (m & 4) != 0 ? -y : y);
            };
            //29092026 Bewertung verschiebungsinvariant: je Komponente den Median der Abweichung abziehen, damit ein
            //  konstanter Versatz die Wahl nicht verfaelscht; der Versatz wird separat protokolliert
            Func<List<double>, double> median = l => { if (l.Count == 0) return double.NaN; l.Sort(); return l[l.Count / 2]; };
            var scores = new List<(double err, int o, int m, double off_x, double off_y)>();
            for (int o = 0; o < 8; o++)
                for (int m = 0; m < 8; m++)
                {
                    var ex = new List<double>(); var ey = new List<double>();
                    for (int sx = 8; sx < W - 8; sx += 8)
                        for (int sy = 8; sy < H - 8; sy += 8)
                        {
                            if (float.IsNaN(stereo_gt_dx[sx, sy])) continue;
                            (float x, float y) = vec(o, m, sx, sy);
                            if (float.IsNaN(x)) continue;
                            ex.Add(x - stereo_gt_dx[sx, sy]); ey.Add(y - stereo_gt_dy[sx, sy]);
                        }
                    double mx = median(new List<double>(ex)), my = median(new List<double>(ey));
                    var r = new List<double>(ex.Count);
                    for (int q = 0; q < ex.Count; q++) r.Add(Math.Sqrt((ex[q] - mx) * (ex[q] - mx) + (ey[q] - my) * (ey[q] - my)));
                    double sc = median(r);
                    scores.Add((double.IsNaN(sc) ? double.MaxValue : sc, o, m, mx, my));
                }
            scores.Sort((a, b) => a.err.CompareTo(b.err));
            (double best_err, int bo, int bm, double off_x, double off_y) = scores[0];
            //29092026 Zuordnung ist eine feste Eigenschaft des Bild-Einlesens: in run_stereo an der Referenzstufe
            //  bestimmt und dann fuer alle Stufen festgehalten (die fast symmetrische Probe laesst sonst zwischen
            //  gespiegelten Varianten pendeln)
            if (stereo_mapping_lock.HasValue)
            {
                var locked = scores.First(s => s.o == stereo_mapping_lock.Value.o && s.m == stereo_mapping_lock.Value.m);
                (best_err, bo, bm, off_x, off_y) = locked;
            }
            stereo_last_mapping = (bo, bm);
            string mapping = "o" + bo + "m" + bm + (stereo_mapping_lock.HasValue ? "*" : "");

            // Triangulation je Pixel ueber die Kameramatrizen (DLT); Selbsttest aus stereo_ground_truth
            Vector3 c0 = cam_for_uv_0.cameraToWorldMatrix.MultiplyPoint(Vector3.zero);
            double[,] M0 = cam_matrix(cam_for_uv_0), M1 = cam_matrix(cam_for_uv_1);
            float upm = stereo_units_per_mm;
            var depth = new List<List<float>>(W); var dref = new List<List<float>>(W); var epe = new List<List<float>>(W);
            int n = 0;
            double s_ad = 0, s_d = 0, s_rel = 0, s_ref = 0, s_self = 0, s_epe = 0;
            var gts = new List<double>();
            for (int sx = 0; sx < W; sx++)
            {
                var r_t = new List<float>(H); var r_g = new List<float>(H); var r_e = new List<float>(H);
                for (int k = 0; k < H; k++) { r_t.Add(float.NaN); r_g.Add(float.NaN); r_e.Add(float.NaN); }
                for (int sy = 0; sy < H; sy++)
                {
                    int row = H - 1 - sy; // Ausgabe wie die uebrigen Rohkarten: j = Zeile von oben
                    float g_dist = stereo_gt_dist[sx, sy];
                    if (float.IsNaN(g_dist)) continue;
                    double vx = (sx + 0.5) / W, vy = (sy + 0.5) / H; // Pixelmitte in cam_0 (Viewport)
                    (float dx, float dy) = vec(bo, bm, sx, sy);
                    if (float.IsNaN(dx)) continue;
                    (double X, double Y, double Z) = triangulate_dlt(M0, vx, vy, M1, vx + dx / (double)W, vy + dy / (double)H);
                    double t_mm = Math.Sqrt((X - c0.x) * (X - c0.x) + (Y - c0.y) * (Y - c0.y) + (Z - c0.z) * (Z - c0.z)) / upm;
                    double g_mm = g_dist / upm, self_mm = g_mm; // Selbsttest separat (stereo_selftest_units)
                    r_g[row] = (float)g_mm;
                    if (double.IsNaN(t_mm)) continue;
                    r_t[row] = (float)t_mm;
                    double ep = Math.Sqrt((dx - stereo_gt_dx[sx, sy]) * (dx - stereo_gt_dx[sx, sy]) + (dy - stereo_gt_dy[sx, sy]) * (dy - stereo_gt_dy[sx, sy]));
                    r_e[row] = (float)ep;
                    if (sx < 20 || sy < 20 || sx >= W - 20 || sy >= H - 20) continue; // Rand wie bei den Flusskarten
                    n++; s_ad += Math.Abs(t_mm - g_mm); s_d += t_mm - g_mm; s_rel += Math.Abs(t_mm - g_mm) / g_mm;
                    s_ref += g_mm; s_self += Math.Abs(self_mm - g_mm); s_epe += ep; gts.Add(g_mm);
                }
                depth.Add(r_t); dref.Add(r_g); epe.Add(r_e);
            }
            double mae = n > 0 ? s_ad / n : double.NaN, ref_mean = n > 0 ? s_ref / n : double.NaN;
            double relief = 0;
            foreach (double g in gts) relief += Math.Abs(g - ref_mean);
            relief = n > 0 ? relief / n : double.NaN; // mittlere Abweichung der Referenz von ihrem Mittel (Relief)

            string exp_dir = proj_dir.EndsWith("/") ? proj_dir : proj_dir + "/";
            write_raw_map(depth, exp_dir + "depth_tv_r" + rs + ".f32");
            write_raw_map(dref, exp_dir + "depth_ref_r" + rs + ".f32");
            write_raw_map(epe, exp_dir + "disparity_epe_r" + rs + ".f32");
            //29092026 Diagnose: exakte Disparitaet (Bildschirm, x rechts / y oben) und TV-Rohfluss (Array-Indizes)
            //  als Rohkarten, damit Zuordnung und Komponenten ausserhalb von Unity geprueft werden koennen
            Func<float[,], List<List<float>>> to_lists = a =>
            {
                var l = new List<List<float>>(a.GetLength(0));
                for (int i = 0; i < a.GetLength(0); i++)
                {
                    var r = new List<float>(a.GetLength(1));
                    for (int j = 0; j < a.GetLength(1); j++) r.Add(a[i, j]);
                    l.Add(r);
                }
                return l;
            };
            write_raw_map(to_lists(stereo_gt_dx), exp_dir + "stereo_gt_dx_screen_r" + rs + ".f32");
            write_raw_map(to_lists(stereo_gt_dy), exp_dir + "stereo_gt_dy_screen_r" + rs + ".f32");
            write_raw_map(to_lists(U), exp_dir + "stereo_tv_u_array_r" + rs + ".f32");
            write_raw_map(to_lists(V), exp_dir + "stereo_tv_v_array_r" + rs + ".f32");
            Debug.Log("Stereo-Kameras: cam_0 aspect " + cam_for_uv_0.aspect.ToString("0.####", ci) + ", pixel "
                + cam_for_uv_0.pixelWidth + "x" + cam_for_uv_0.pixelHeight + ", target "
                + (cam_for_uv_0.targetTexture != null ? cam_for_uv_0.targetTexture.width + "x" + cam_for_uv_0.targetTexture.height : "keins")
                + ", physical " + cam_for_uv_0.usePhysicalProperties + ", lensShift " + cam_for_uv_0.lensShift
                + " | cam_1 aspect " + cam_for_uv_1.aspect.ToString("0.####", ci) + ", target "
                + (cam_for_uv_1.targetTexture != null ? cam_for_uv_1.targetTexture.width + "x" + cam_for_uv_1.targetTexture.height : "keins")
                + " | Bild " + W + "x" + H + " | Top-5 Zuordnungen: "
                + string.Join(", ", scores.Take(5).Select(s => "o" + s.o + "m" + s.m + "=" + s.err.ToString("0.###", ci))));

            string tsv = root_path + "depth_results.tsv";
            if (!File.Exists(tsv) || File.ReadLines(tsv).FirstOrDefault() != DEPTH_HEADER)
            {
                if (File.Exists(tsv))
                    File.Copy(tsv, root_path + "depth_results_before_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".tsv", true);
                File.WriteAllText(tsv, DEPTH_HEADER + "\n");
            }
            Func<double, string> f = v => double.IsNaN(v) ? "NaN" : v.ToString("G6", ci);
            string row_s = get_experiment() + "\t" + f(pars == null ? double.NaN : pars.get_lighting_intensity())
                + "\t" + f(pars == null ? double.NaN : pars.get_poisson_error())
                + "\t" + f(pars == null ? double.NaN : pars.get_speckle_size()) + "\t" + frame
                + "\t" + f(mae) + "\t" + f(n > 0 ? s_d / n : double.NaN) + "\t" + f(n > 0 ? s_rel / n : double.NaN)
                + "\t" + f(relief > 0 ? mae / relief : double.NaN) + "\t" + f(n > 0 ? s_epe / n : double.NaN)
                + "\t" + f(stereo_selftest_units / upm) + "\t" + n + "\t" + f(ref_mean) + "\t" + mapping
                + "\t" + sweep_setup_key();
            write_to_txt(tsv, row_s, mode: "append");
            Debug.Log("Stereo-Tiefe " + get_experiment() + " (Frame " + frame + "): MAE " + mae.ToString("0.###", ci)
                + " mm, Bias " + (n > 0 ? s_d / n : double.NaN).ToString("0.###", ci) + " mm, rel. "
                + (n > 0 ? 100 * s_rel / n : double.NaN).ToString("0.###", ci) + " %, bezogen aufs Relief "
                + (relief > 0 ? 100 * mae / relief : double.NaN).ToString("0.#", ci) + " %, Disparitaetsfehler "
                + (n > 0 ? s_epe / n : double.NaN).ToString("0.###", ci) + " px | Selbsttest "
                + (stereo_selftest_units / upm).ToString("0.######", ci) + " mm | Versatz TV - GT: x " + off_x.ToString("0.###", ci) + " px, y " + off_y.ToString("0.###", ci) + " px | " + n + " Px, Referenz im Mittel "
                + ref_mean.ToString("0.#", ci) + " mm, Relief " + relief.ToString("0.##", ci) + " mm | Zuordnung " + mapping
                + " (Median " + best_err.ToString("0.###", ci) + " px, naechste " + scores[1].err.ToString("0.###", ci) + " px)");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Stereo-Tiefe nicht berechnet (" + get_experiment() + "): " + e.Message + "\n" + e.StackTrace);
        }
    }

    // ============================================================================================
    //30092026 Nutzerwunsch: 3D-Verschiebungsfeld (Scene Flow) in Raumkoordinaten aus beiden Kameras, zusaetzlich
    //  zur Hoehenanalyse (die bleibt unveraendert). Fuer jedes Pixel p von cam_0 in Frame A (Standard: Frame 1):
    //    Stereo-TV cam_0 -> cam_1 in Frame A:  p1 = p + s(p)             -> P_A = Triangulation(p, p1)
    //    zeitlicher TV-Fluss cam_0 A -> B:      p' = p + f0(p)
    //    zeitlicher TV-Fluss cam_1 A -> B:      p1' = p1 + f1(p1)        -> P_B = Triangulation(p', p1')
    //    3D-Verschiebung D = P_B - P_A (mm).
    //  Variante B (Kontrolle): Stereo-TV zusaetzlich in Frame B, P_B = Triangulation(p', p' + s_B(p')).
    //  Ground Truth: Strahl von cam_0 durch p aufs Mesh von Frame A -> Dreieck + baryzentrische Koordinaten ->
    //    derselbe Materialpunkt auf dem Mesh von Frame B (gleiche Topologie, verts_<frame>.txt).
    //  Komponenten im Stereo-Rig-System: x entlang der Basis (cam_0 -> cam_1), z zu den Kameras (Winkelhalbierende
    //    der Blickrichtungen, ~Probennormale), y = z x x. Zuordnung TV-Array -> Bildschirm wie in stereo_depth_step.
    const string SCENE_FLOW_HEADER = "experiment\tlighting_intensity\tpeak_electrons\tspeckle_size\tframe_a\tframe_b"
        + "\tepe3d_mm\trel_epe3d\tmae_x_mm\tmae_y_mm\tmae_z_mm\trel_mae_x\trel_mae_y\trel_mae_z\tmean_abs_ref_mm"
        + "\tepe3d_b_mm\trel_epe3d_b\tpos_mae_mm\tflow0_epe_px\tflow1_epe_px\tdisp_epe_px\tselftest_mm\tn_px\tmapping"
        + "\trender_res\tregularization\tstrain_sigma";
    string sf_gt_key = null;
    float[,,] sf_gt_pa, sf_gt_pb; // Weltkoordinaten (Unity-Einheiten) je cam_0-Pixel [sx, sy, xyz], NaN ohne Treffer
    double sf_bary_check_units = double.NaN;

    /// <summary>
    /// Reference of the 3D displacement: ray casting per pixel of camera 0 onto the mesh of frame A gives triangle and barycentric coordinates; the same material point on the mesh of frame B gives its new position (cached).
    /// </summary>
    /// <param name="pos_a">Position of frame A.</param>
    /// <param name="pos_b">Position of frame B.</param>
    /// <param name="W">Image width.</param>
    /// <param name="H">Image height.</param>
    /// <returns>True if the reference could be computed.</returns>
    bool scene_flow_ground_truth(int pos_a, int pos_b, int W, int H)
    {
        string key = pos_a + "_" + pos_b + "_" + W + "x" + H + "_" + cam_for_uv_0.transform.position
            + cam_for_uv_1.transform.position + cam_for_uv_0.fieldOfView;
        if (sf_gt_key == key) return true;
        List<GameObject> blades = collect_blades();
        List<bool> active = blades.Select(b => b.activeSelf).ToList();
        GameObject blade_a = null, blade_b = null;
        try
        {
            blade_a = load_blade_from_verts(blade_path_for_idx(blade_idxs[pos_a]), blade_idx: -1, with_uv_init: false, with_collider: false);
            blade_b = load_blade_from_verts(blade_path_for_idx(blade_idxs[pos_b]), blade_idx: -1, with_uv_init: false, with_collider: false);
            blade_a.name = "scene_flow_gt_mesh_" + blade_idxs[pos_a];
            blade_b.name = "scene_flow_gt_mesh_" + blade_idxs[pos_b];
            Mesh mesh_a = blade_a.GetComponent<MeshFilter>().sharedMesh, mesh_b = blade_b.GetComponent<MeshFilter>().sharedMesh;
            int[] tris = mesh_a.triangles;
            Vector3[] verts_a = mesh_a.vertices, verts_b = mesh_b.vertices;
            if (verts_a.Length != verts_b.Length || tris.Length != mesh_b.triangles.Length)
            {
                Debug.LogError("3D-Fluss-GT: Meshes von Frame " + blade_idxs[pos_a] + " und " + blade_idxs[pos_b]
                    + " haben unterschiedliche Topologie (" + verts_a.Length + " / " + verts_b.Length + " Knoten)");
                return false;
            }
            UnityEngine.Matrix4x4 ltw_a = blade_a.transform.localToWorldMatrix, ltw_b = blade_b.transform.localToWorldMatrix;
            for (int k = 0; k < blades.Count; k++) blades[k].SetActive(false);
            blade_b.SetActive(false); // nur Knoten gebraucht, kein Strahl darf es treffen
            blade_a.SetActive(true);
            MeshCollider mc = blade_a.GetComponent<MeshCollider>();
            if (mc == null) mc = blade_a.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh_a;
            blade_a.layer = 7;
            Physics.SyncTransforms();

            sf_gt_pa = new float[W, H, 3]; sf_gt_pb = new float[W, H, 3];
            Vector3 c0 = cam_for_uv_0.cameraToWorldMatrix.MultiplyPoint(Vector3.zero);
            double[,] M0g = cam_matrix(cam_for_uv_0);
            double[,] inv0d = invert4(M0g);
            int hits = 0;
            double s_bary = 0, s_rt = 0;
            for (int sx = 0; sx < W; sx++)
                for (int sy = 0; sy < H; sy++)
                {
                    double u_ndc = 2.0 * (sx + 0.5) / W - 1.0, v_ndc = 2.0 * (sy + 0.5) / H - 1.0;
                    Ray r = pixel_ray(inv0d, u_ndc, v_ndc);
                    bool ok = Physics.Raycast(r, out RaycastHit hit, Mathf.Infinity, 1 << 7) && hit.triangleIndex >= 0
                        && 3 * hit.triangleIndex + 2 < tris.Length;
                    if (ok)
                    {
                        int t0 = tris[3 * hit.triangleIndex], t1 = tris[3 * hit.triangleIndex + 1], t2 = tris[3 * hit.triangleIndex + 2];
                        Vector3 bc = hit.barycentricCoordinate;
                        Vector3 pa = ltw_a.MultiplyPoint(bc.x * verts_a[t0] + bc.y * verts_a[t1] + bc.z * verts_a[t2]);
                        Vector3 pb = ltw_b.MultiplyPoint(bc.x * verts_b[t0] + bc.y * verts_b[t1] + bc.z * verts_b[t2]);
                        s_bary += (pa - hit.point).magnitude; // Kontrolle: baryzentrischer Punkt = Trefferpunkt
                        (double rx, double ry) = project_vp(M0g, hit.point); // Kontrolle: Treffer liegt auf der Pixelmitte
                        s_rt += Math.Sqrt(Math.Pow((rx - (sx + 0.5) / W) * W, 2) + Math.Pow((ry - (sy + 0.5) / H) * H, 2));
                        sf_gt_pa[sx, sy, 0] = hit.point.x; sf_gt_pa[sx, sy, 1] = hit.point.y; sf_gt_pa[sx, sy, 2] = hit.point.z;
                        sf_gt_pb[sx, sy, 0] = pb.x; sf_gt_pb[sx, sy, 1] = pb.y; sf_gt_pb[sx, sy, 2] = pb.z;
                        hits++;
                    }
                    else
                        for (int c = 0; c < 3; c++) sf_gt_pa[sx, sy, c] = sf_gt_pb[sx, sy, c] = float.NaN;
                }
            sf_bary_check_units = hits > 0 ? s_bary / hits : double.NaN;
            stereo_units_per_mm = Mathf.Abs(blade_a.transform.lossyScale.x) > 0f ? Mathf.Abs(blade_a.transform.lossyScale.x) : 1f;
            sf_gt_key = key;
            Debug.Log("3D-Fluss-GT (Frames " + blade_idxs[pos_a] + " -> " + blade_idxs[pos_b] + "): " + hits + " Pixel auf der Probe, "
                + verts_a.Length + " Knoten | Kontrolle baryzentrischer Punkt <-> Treffer "
                + (sf_bary_check_units / stereo_units_per_mm).ToString("0.######", CultureInfo.InvariantCulture) + " mm (sollte ~0 sein)"
                + " | Abweichung Pixelmitte <-> Projektion des Treffers " + (hits > 0 ? s_rt / hits : double.NaN).ToString("0.####", CultureInfo.InvariantCulture) + " px (sollte ~0 sein)");
            return hits > 0;
        }
        catch (Exception e)
        {
            Debug.LogError("3D-Fluss-GT: Meshes nicht geladen/ausgewertet: " + e.Message + "\n" + e.StackTrace);
            return false;
        }
        finally
        {
            if (blade_a != null) { blade_a.SetActive(false); Destroy(blade_a); }
            if (blade_b != null) { blade_b.SetActive(false); Destroy(blade_b); }
            for (int k = 0; k < blades.Count; k++) blades[k].SetActive(active[k]);
        }
    }

    //30092026 TV-Fluss zweier Bilder als Arrays (entnormiert wie in stereo_depth_step); Parameter vorher per set_up_pars
    /// <summary>
    /// Computes the TV flow between two images and returns it as de-normalised arrays (solver parameters must be set before).
    /// </summary>
    /// <param name="im_a">First image.</param>
    /// <param name="im_b">Second image.</param>
    /// <param name="W">Width.</param>
    /// <param name="H">Height.</param>
    /// <returns>Tuple (U, V) of flow arrays, or nulls on failure.</returns>
    async Task<(float[,] U, float[,] V)> tv_flow_arrays(List<float> im_a, List<float> im_b, int W, int H)
    {
        List<List<float>> I0 = new List<List<float>> { im_a, im_a };
        List<List<float>> I1 = new List<List<float>> { im_b, im_b };
        (List<List<List<float>>> u_mat, List<List<List<float>>> v_mat, List<float> min_u, List<float> min_v,
            List<float> max_u, List<float> max_v) = await Task.Run(() => compute_ims(I0, I1, W, H, scale_fac: 1, with_dt: false));
        if (u_mat == null) return (null, null);
        int ni = u_mat[0].Count, nj = u_mat[0][0].Count;
        float[,] U = new float[ni, nj], V = new float[ni, nj];
        for (int i = 0; i < ni; i++)
            for (int j = 0; j < nj; j++)
            {
                U[i, j] = max_u[0] > min_u[0] ? min_u[0] + u_mat[0][i][j] * (max_u[0] - min_u[0]) : min_u[0];
                V[i, j] = max_v[0] > min_v[0] ? min_v[0] + v_mat[0][i][j] * (max_v[0] - min_v[0]) : min_v[0];
            }
        return (U, V);
    }

    //30092026 TV-Array -> Bildschirm (x rechts, y oben, Pixel) mit Zuordnung (o, m) wie in stereo_depth_step
    /// <summary>
    /// Converts flow arrays to screen coordinates (x right, y up, pixels) with a given axis mapping.
    /// </summary>
    /// <param name="U">First flow array.</param>
    /// <param name="V">Second flow array.</param>
    /// <param name="o">Orientation index (transpose and flips).</param>
    /// <param name="m">Component index (order and signs).</param>
    /// <param name="W">Screen width.</param>
    /// <param name="H">Screen height.</param>
    /// <returns>Array [x, y, component].</returns>
    static float[,,] flow_to_screen(float[,] U, float[,] V, int o, int m, int W, int H)
    {
        int ni = U.GetLength(0), nj = U.GetLength(1);
        var r = new float[W, H, 2];
        for (int sx = 0; sx < W; sx++)
            for (int sy = 0; sy < H; sy++)
            {
                int a = (o & 2) != 0 ? W - 1 - sx : sx, b = (o & 4) != 0 ? H - 1 - sy : sy;
                (int i, int j) = (o & 1) != 0 ? (b, a) : (a, b);
                if (i < 0 || j < 0 || i >= ni || j >= nj) { r[sx, sy, 0] = r[sx, sy, 1] = float.NaN; continue; }
                float p = U[i, j], q = V[i, j];
                (float x, float y) = (m & 1) != 0 ? (q, p) : (p, q);
                r[sx, sy, 0] = (m & 2) != 0 ? -x : x;
                r[sx, sy, 1] = (m & 4) != 0 ? -y : y;
            }
        return r;
    }

    //30092026 bilinear an kontinuierlicher Bildschirmposition (Pixelmitte von (sx, sy) liegt bei sx + 0.5, sy + 0.5)
    /// <summary>
    /// Bilinear sampling of a screen-space flow at a continuous position (pixel centres at +0.5).
    /// </summary>
    /// <param name="F">Screen-space flow [x, y, component].</param>
    /// <param name="px">x position in pixels.</param>
    /// <param name="py">y position in pixels.</param>
    /// <returns>Interpolated flow (x, y), NaN outside.</returns>
    static (double x, double y) sample_screen(float[,,] F, double px, double py)
    {
        int W = F.GetLength(0), H = F.GetLength(1);
        double x = px - 0.5, y = py - 0.5;
        int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
        if (x0 < 0 || y0 < 0 || x0 + 1 >= W || y0 + 1 >= H) return (double.NaN, double.NaN);
        double fx = x - x0, fy = y - y0;
        double[] res = new double[2];
        for (int c = 0; c < 2; c++)
            res[c] = F[x0, y0, c] * (1 - fx) * (1 - fy) + F[x0 + 1, y0, c] * fx * (1 - fy)
                   + F[x0, y0 + 1, c] * (1 - fx) * fy + F[x0 + 1, y0 + 1, c] * fx * fy;
        return (res[0], res[1]);
    }

    //30092026 alle 64 Zuordnungen gegen eine exakte Bildschirm-Verschiebung bewerten (versatzinvariant, Median)
    /// <summary>
    /// Scores all 64 axis mappings of a flow against an exact screen-space displacement (offset-invariant median error).
    /// </summary>
    /// <param name="U">First flow array.</param>
    /// <param name="V">Second flow array.</param>
    /// <param name="gdx">Exact x displacement per screen pixel.</param>
    /// <param name="gdy">Exact y displacement per screen pixel.</param>
    /// <param name="W">Width.</param>
    /// <param name="H">Height.</param>
    /// <returns>Mappings sorted by error with their median offsets.</returns>
    static List<(double err, int o, int m, double off_x, double off_y)> score_mappings(float[,] U, float[,] V,
        float[,] gdx, float[,] gdy, int W, int H)
    {
        Func<List<double>, double> median = l => { if (l.Count == 0) return double.NaN; l.Sort(); return l[l.Count / 2]; };
        var scores = new List<(double err, int o, int m, double off_x, double off_y)>();
        for (int o = 0; o < 8; o++)
            for (int m = 0; m < 8; m++)
            {
                float[,,] F = flow_to_screen(U, V, o, m, W, H);
                var ex = new List<double>(); var ey = new List<double>();
                for (int sx = 8; sx < W - 8; sx += 8)
                    for (int sy = 8; sy < H - 8; sy += 8)
                    {
                        if (float.IsNaN(gdx[sx, sy]) || float.IsNaN(F[sx, sy, 0])) continue;
                        ex.Add(F[sx, sy, 0] - gdx[sx, sy]); ey.Add(F[sx, sy, 1] - gdy[sx, sy]);
                    }
                double mx = median(new List<double>(ex)), my = median(new List<double>(ey));
                var r = new List<double>(ex.Count);
                for (int q = 0; q < ex.Count; q++) r.Add(Math.Sqrt((ex[q] - mx) * (ex[q] - mx) + (ey[q] - my) * (ey[q] - my)));
                double sc = median(r);
                scores.Add((double.IsNaN(sc) ? double.MaxValue : sc, o, m, mx, my));
            }
        scores.Sort((a, b) => a.err.CompareTo(b.err));
        return scores;
    }

    /// <summary>
    /// 3D displacement of one stage from both cameras: stereo flow in frame A, temporal flow in camera 0 and camera 1, DLT triangulation of both positions, difference in the stereo-rig frame; variant B uses a second stereo flow in frame B; writes raw maps and a row of sceneflow_results.tsv.
    /// </summary>
    /// <param name="proj_dir">Experiment folder.</param>
    /// <param name="pars">Parameters of the stage.</param>
    /// <param name="pos_a">Position of frame A.</param>
    /// <param name="pos_b">Position of frame B.</param>
    async Task scene_flow_step(string proj_dir, Params pars, int pos_a = 0, int pos_b = 1)
    {
        CultureInfo ci = CultureInfo.InvariantCulture;
        try
        {
            pos_a = Math.Max(0, Math.Min(pos_a, blade_idxs.Count - 1));
            pos_b = Math.Max(0, Math.Min(pos_b, blade_idxs.Count - 1));
            int fa = blade_idxs[pos_a], fb = blade_idxs[pos_b];
            string rs = get_render_res().ToString(ci);
            Func<int, int, string> im_path = (cam, fr) => proj_dir + "/cam_" + cam + "/uv/" + category + "im_" + fr + "_r" + rs + ".png";
            foreach (string p in new[] { im_path(0, fa), im_path(1, fa), im_path(0, fb), im_path(1, fb) })
                if (!File.Exists(p)) { Debug.LogWarning("3D-Fluss: Kamerabild fehlt (" + p + ")"); return; }
            im_dressed a0 = manage_read_im(im_path(0, fa)), a1 = manage_read_im(im_path(1, fa));
            im_dressed b0 = manage_read_im(im_path(0, fb)), b1 = manage_read_im(im_path(1, fb));
            int W = a0.width, H = a0.height;
            if (!scene_flow_ground_truth(pos_a, pos_b, W, H)) return;
            double[,] M0 = cam_matrix(cam_for_uv_0), M1 = cam_matrix(cam_for_uv_1);
            float upm = stereo_units_per_mm;

            // exakte Bildschirm-Verschiebungen aus der GT: Disparitaet in Frame A, zeitlicher Fluss in cam_0 und cam_1
            var g_disp_x = new float[W, H]; var g_disp_y = new float[W, H];
            var g_f0_x = new float[W, H]; var g_f0_y = new float[W, H];
            for (int sx = 0; sx < W; sx++)
                for (int sy = 0; sy < H; sy++)
                {
                    if (float.IsNaN(sf_gt_pa[sx, sy, 0])) { g_disp_x[sx, sy] = g_disp_y[sx, sy] = g_f0_x[sx, sy] = g_f0_y[sx, sy] = float.NaN; continue; }
                    Vector3 pa = new Vector3(sf_gt_pa[sx, sy, 0], sf_gt_pa[sx, sy, 1], sf_gt_pa[sx, sy, 2]);
                    Vector3 pb = new Vector3(sf_gt_pb[sx, sy, 0], sf_gt_pb[sx, sy, 1], sf_gt_pb[sx, sy, 2]);
                    (double x0, double y0) = project_vp(M0, pa); (double x1, double y1) = project_vp(M1, pa);
                    (double x0b, double y0b) = project_vp(M0, pb);
                    g_disp_x[sx, sy] = (float)((x1 - x0) * W); g_disp_y[sx, sy] = (float)((y1 - y0) * H);
                    g_f0_x[sx, sy] = (float)((x0b - x0) * W); g_f0_y[sx, sy] = (float)((y0b - y0) * H);
                }

            // TV-Laeufe: Stereo mit den Parametern der Hoehenanalyse, zeitlich mit denen der normalen Analyse
            (PAR_DEFAULT_LAMBDA, PAR_DEFAULT_THETA, PAR_DEFAULT_NSCALES, PAR_DEFAULT_NWARPS) = set_up_pars(false, get_experiment() + "_heights");
            (float[,] SU, float[,] SV) = await tv_flow_arrays(a0.im_vec, a1.im_vec, W, H);
            if (SU == null) return;
            (float[,] SBU, float[,] SBV) = await tv_flow_arrays(b0.im_vec, b1.im_vec, W, H);
            if (SBU == null) return;
            (PAR_DEFAULT_LAMBDA, PAR_DEFAULT_THETA, PAR_DEFAULT_NSCALES, PAR_DEFAULT_NWARPS) = set_up_pars(false, get_experiment());
            (float[,] F0U, float[,] F0V) = await tv_flow_arrays(a0.im_vec, b0.im_vec, W, H);
            if (F0U == null) return;
            (float[,] F1U, float[,] F1V) = await tv_flow_arrays(a1.im_vec, b1.im_vec, W, H);
            if (F1U == null) return;
            if (this == null || cam_for_uv_0 == null || cam_for_uv_1 == null) return; // Play-Modus beendet

            // Zuordnung an der Stereo-Disparitaet bestimmen (bzw. festgehaltene verwenden), gilt fuer alle TV-Felder
            var scores = score_mappings(SU, SV, g_disp_x, g_disp_y, W, H);
            (double best_err, int bo, int bm, _, _) = scores[0];
            if (stereo_mapping_lock.HasValue)
                (best_err, bo, bm, _, _) = scores.First(s => s.o == stereo_mapping_lock.Value.o && s.m == stereo_mapping_lock.Value.m);
            stereo_last_mapping = (bo, bm);
            string mapping = "o" + bo + "m" + bm + (stereo_mapping_lock.HasValue ? "*" : "");
            float[,,] S = flow_to_screen(SU, SV, bo, bm, W, H), SB = flow_to_screen(SBU, SBV, bo, bm, W, H);
            float[,,] F0 = flow_to_screen(F0U, F0V, bo, bm, W, H), F1 = flow_to_screen(F1U, F1V, bo, bm, W, H);

            // Stereo-Rig-System
            Vector3 c0 = cam_for_uv_0.cameraToWorldMatrix.MultiplyPoint(Vector3.zero);
            Vector3 c1 = cam_for_uv_1.cameraToWorldMatrix.MultiplyPoint(Vector3.zero);
            Vector3 ez = -(cam_for_uv_0.transform.forward + cam_for_uv_1.transform.forward).normalized;
            Vector3 ex = ((c1 - c0) - Vector3.Dot(c1 - c0, ez) * ez).normalized;
            Vector3 ey = Vector3.Cross(ez, ex).normalized;
            Func<double, double, double, double[]> to_rig = (X, Y, Z) =>
            {
                var w = new Vector3((float)X, (float)Y, (float)Z);
                return new double[] { Vector3.Dot(w, ex) / upm, Vector3.Dot(w, ey) / upm, Vector3.Dot(w, ez) / upm };
            };

            // Ausgabekarten (i = sx, j = Zeile von oben), Rig-System, mm
            string[] names = { "tv", "tv_b", "ref" };
            var maps = new Dictionary<string, List<List<float>>>();
            foreach (string nm in names) foreach (string c in new[] { "x", "y", "z" }) maps[nm + "_" + c] = new List<List<float>>(W);
            foreach (string c in new[] { "x", "y", "z" }) maps["pos_ref_" + c] = new List<List<float>>(W);
            var epe_map = new List<List<float>>(W);
            int n = 0, n_b = 0, n_f1 = 0;
            double s_epe = 0, s_epe_b = 0, s_g = 0, s_pos = 0, s_f0 = 0, s_f1 = 0, s_disp = 0, s_self = 0;
            double[] s_mae = new double[3], s_gc = new double[3];
            for (int sx = 0; sx < W; sx++)
            {
                foreach (var l in maps.Values) { var r = new List<float>(H); for (int k = 0; k < H; k++) r.Add(float.NaN); l.Add(r); }
                { var r = new List<float>(H); for (int k = 0; k < H; k++) r.Add(float.NaN); epe_map.Add(r); }
                for (int sy = 0; sy < H; sy++)
                {
                    if (float.IsNaN(sf_gt_pa[sx, sy, 0])) continue;
                    int row = H - 1 - sy;
                    double gax = sf_gt_pa[sx, sy, 0], gay = sf_gt_pa[sx, sy, 1], gaz = sf_gt_pa[sx, sy, 2];
                    double[] G = to_rig(sf_gt_pb[sx, sy, 0] - gax, sf_gt_pb[sx, sy, 1] - gay, sf_gt_pb[sx, sy, 2] - gaz);
                    double[] PAr = to_rig(gax - c0.x, gay - c0.y, gaz - c0.z);
                    for (int c = 0; c < 3; c++)
                    {
                        maps["ref_" + "xyz"[c]][sx][row] = (float)G[c];
                        maps["pos_ref_" + "xyz"[c]][sx][row] = (float)PAr[c];
                    }
                    double px = sx + 0.5, py = sy + 0.5;
                    if (float.IsNaN(S[sx, sy, 0]) || float.IsNaN(F0[sx, sy, 0])) continue;
                    double p1x = px + S[sx, sy, 0], p1y = py + S[sx, sy, 1];
                    (double f1x, double f1y) = sample_screen(F1, p1x, p1y);
                    double p0bx = px + F0[sx, sy, 0], p0by = py + F0[sx, sy, 1];
                    if (double.IsNaN(f1x)) continue;
                    (double AX, double AY, double AZ) = triangulate_dlt(M0, px / W, py / H, M1, p1x / W, p1y / H);
                    (double BX, double BY, double BZ) = triangulate_dlt(M0, p0bx / W, p0by / H, M1, (p1x + f1x) / W, (p1y + f1y) / H);
                    double[] D = to_rig(BX - AX, BY - AY, BZ - AZ);
                    // Variante B: Stereo auch in Frame B
                    (double sbx, double sby) = sample_screen(SB, p0bx, p0by);
                    double[] DB = { double.NaN, double.NaN, double.NaN };
                    if (!double.IsNaN(sbx))
                    {
                        (double B2X, double B2Y, double B2Z) = triangulate_dlt(M0, p0bx / W, p0by / H, M1, (p0bx + sbx) / W, (p0by + sby) / H);
                        DB = to_rig(B2X - AX, B2Y - AY, B2Z - AZ);
                    }
                    double epe = 0, epe_b = 0, gn = 0;
                    for (int c = 0; c < 3; c++)
                    {
                        maps["tv_" + "xyz"[c]][sx][row] = (float)D[c];
                        maps["tv_b_" + "xyz"[c]][sx][row] = (float)DB[c];
                        epe += (D[c] - G[c]) * (D[c] - G[c]); epe_b += (DB[c] - G[c]) * (DB[c] - G[c]); gn += G[c] * G[c];
                    }
                    epe = Math.Sqrt(epe); epe_b = Math.Sqrt(epe_b); gn = Math.Sqrt(gn);
                    epe_map[sx][row] = (float)epe;
                    if (double.IsNaN(epe) || sx < 20 || sy < 20 || sx >= W - 20 || sy >= H - 20) continue; // Rand wie Hoehenanalyse
                    // Kontrollen: Position in Frame A, 2D-Fluesse gegen exakte Bildschirm-Verschiebungen, Selbsttest
                    double pos_err = Math.Sqrt((AX - gax) * (AX - gax) + (AY - gay) * (AY - gay) + (AZ - gaz) * (AZ - gaz)) / upm;
                    Vector3 gpb = new Vector3(sf_gt_pb[sx, sy, 0], sf_gt_pb[sx, sy, 1], sf_gt_pb[sx, sy, 2]);
                    (double q0x, double q0y) = project_vp(M0, gpb); (double q1x, double q1y) = project_vp(M1, gpb);
                    (double r1x, double r1y) = project_vp(M1, new Vector3((float)gax, (float)gay, (float)gaz));
                    double e_f0 = Math.Sqrt(Math.Pow(F0[sx, sy, 0] - g_f0_x[sx, sy], 2) + Math.Pow(F0[sx, sy, 1] - g_f0_y[sx, sy], 2));
                    (double f1gx, double f1gy) = sample_screen(F1, r1x * W, r1y * H); // cam_1-Fluss am exakten Stereo-Punkt
                    double e_f1 = Math.Sqrt(Math.Pow(f1gx - (q1x - r1x) * W, 2) + Math.Pow(f1gy - (q1y - r1y) * H, 2));
                    double e_disp = Math.Sqrt(Math.Pow(S[sx, sy, 0] - g_disp_x[sx, sy], 2) + Math.Pow(S[sx, sy, 1] - g_disp_y[sx, sy], 2));
                    (double TX, double TY, double TZ) = triangulate_dlt(M0, q0x, q0y, M1, q1x, q1y);
                    double self = Math.Sqrt((TX - gpb.x) * (TX - gpb.x) + (TY - gpb.y) * (TY - gpb.y) + (TZ - gpb.z) * (TZ - gpb.z)) / upm;
                    n++; s_epe += epe; s_g += gn; s_pos += pos_err; s_disp += e_disp; s_self += self;
                    s_f0 += e_f0; if (!double.IsNaN(e_f1)) { s_f1 += e_f1; n_f1++; }
                    if (!double.IsNaN(epe_b)) { s_epe_b += epe_b; n_b++; }
                    for (int c = 0; c < 3; c++) { s_mae[c] += Math.Abs(D[c] - G[c]); s_gc[c] += Math.Abs(G[c]); }
                }
            }
            string exp_dir = proj_dir.EndsWith("/") ? proj_dir : proj_dir + "/";
            foreach (var kv in maps) write_raw_map(kv.Value, exp_dir + "sceneflow_" + kv.Key + "_r" + rs + ".f32");
            write_raw_map(epe_map, exp_dir + "sceneflow_epe_r" + rs + ".f32");

            Func<double, string> f = v => double.IsNaN(v) || double.IsInfinity(v) ? "NaN" : v.ToString("G6", ci);
            double N = Math.Max(n, 1), mean_g = s_g / N, NB = Math.Max(n_b, 1), NF1 = Math.Max(n_f1, 1);
            string tsv = root_path + "sceneflow_results.tsv";
            if (!File.Exists(tsv) || File.ReadLines(tsv).FirstOrDefault() != SCENE_FLOW_HEADER)
            {
                if (File.Exists(tsv))
                    File.Copy(tsv, root_path + "sceneflow_results_before_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".tsv", true);
                File.WriteAllText(tsv, SCENE_FLOW_HEADER + "\n");
            }
            string row_s = get_experiment() + "\t" + f(pars == null ? double.NaN : pars.get_lighting_intensity())
                + "\t" + f(pars == null ? double.NaN : pars.get_poisson_error())
                + "\t" + f(pars == null ? double.NaN : pars.get_speckle_size()) + "\t" + fa + "\t" + fb
                + "\t" + f(s_epe / N) + "\t" + f(s_epe / Math.Max(s_g, 1e-12))
                + "\t" + f(s_mae[0] / N) + "\t" + f(s_mae[1] / N) + "\t" + f(s_mae[2] / N)
                + "\t" + f(s_mae[0] / s_gc[0]) + "\t" + f(s_mae[1] / s_gc[1]) + "\t" + f(s_mae[2] / s_gc[2]) + "\t" + f(mean_g)
                + "\t" + f(s_epe_b / NB) + "\t" + f(s_epe_b / NB / Math.Max(mean_g, 1e-12)) + "\t" + f(s_pos / N)
                + "\t" + f(s_f0 / N) + "\t" + f(s_f1 / NF1) + "\t" + f(s_disp / N) + "\t" + f(s_self / N) + "\t" + n + "\t" + mapping
                + "\t" + sweep_setup_key();
            write_to_txt(tsv, row_s, mode: "append");
            Debug.Log("3D-Fluss " + get_experiment() + " (Frames " + fa + " -> " + fb + ", " + n + " Px): 3D-Fehler "
                + (s_epe / N).ToString("0.####", ci) + " mm = " + (100 * s_epe / Math.Max(s_g, 1e-12)).ToString("0.##", ci)
                + " % von |D| (Mittel " + mean_g.ToString("0.###", ci) + " mm) | Komponenten x/y/z (Basis/quer/zu den Kameras) MAE "
                + (s_mae[0] / N).ToString("0.####", ci) + " / " + (s_mae[1] / N).ToString("0.####", ci) + " / " + (s_mae[2] / N).ToString("0.####", ci)
                + " mm, mittl. |GT| " + (s_gc[0] / N).ToString("0.###", ci) + " / " + (s_gc[1] / N).ToString("0.###", ci) + " / " + (s_gc[2] / N).ToString("0.###", ci)
                + " mm | Variante B (Stereo in beiden Frames) " + (s_epe_b / NB).ToString("0.####", ci) + " mm"
                + " | Position Frame A " + (s_pos / N).ToString("0.####", ci) + " mm | 2D-Fehler Stereo/cam_0/cam_1 "
                + (s_disp / N).ToString("0.###", ci) + " / " + (s_f0 / N).ToString("0.###", ci) + " / " + (s_f1 / NF1).ToString("0.###", ci)
                + " px | Selbsttest " + (s_self / N).ToString("0.######", ci) + " mm | Zuordnung " + mapping
                + " (Median " + best_err.ToString("0.###", ci) + " px) | Rig: ex " + ex.ToString("F3") + ", ey " + ey.ToString("F3") + ", ez " + ez.ToString("F3"));
        }
        catch (Exception e)
        {
            Debug.LogWarning("3D-Fluss nicht berechnet (" + get_experiment() + "): " + e.Message + "\n" + e.StackTrace);
        }
    }

    //30092026 Buttons "3D-Fluss: Neu / Licht / Speckle" (Galerie): Ablauf wie run_stereo
    /// <summary>
    /// 3D-displacement analysis (buttons 3D-Fluss) for the selected stages, analogous to run_stereo; shows the map panels in the gallery.
    /// </summary>
    /// <param name="which">normal, lighting, speckle, or noise.</param>
    public async void run_scene_flow(string which)
    {
        if (stereo_running) { Debug.LogWarning("Hoehen-/3D-Flussanalyse laeuft bereits."); return; }
        if (get_is_started()) { Debug.LogWarning("Waehrend einer laufenden Analyse keine 3D-Flussanalyse."); return; }
        stereo_running = true;
        string exp_before = get_experiment();
        CultureInfo ci = CultureInfo.InvariantCulture;
        try
        {
            var jobs = collect_stereo_jobs(which);
            if (jobs.Count == 0)
            {
                ExperimentImageGallery.SetResultsText(which == "lighting"
                    ? "Keine Licht-Stufen gefunden - zuerst \"Licht: Neu\" laufen lassen."
                    : which == "speckle" ? "Keine Speckle-Stufen gefunden - zuerst \"Speckle: Neu\" laufen lassen."
                    : "Keine normale Analyse gefunden - zuerst \"Start\" laufen lassen.");
                ExperimentImageGallery.ShowResultsWindow();
                return;
            }
            int fa = blade_idxs[0], fb = blade_idxs[Math.Min(1, blade_idxs.Count - 1)];
            var summary = new StringBuilder("3D-Fluss (cam_0 + cam_1, Frames " + fa + " -> " + fb + ", " + describe_regularization()
                + ")\nexperiment | 3D-Fehler [mm] | rel. [%] | MAE x/y/z [mm] | Variante B [mm] | 2D cam_0 [px]\n");
            stereo_mapping_lock = null;
            var order = Enumerable.Range(0, jobs.Count).OrderBy(q => jobs[q].pars == null ? 0.0
                : Math.Abs(Math.Log(Math.Max(1e-9, jobs[q].pars.get_lighting_intensity()))))
                .ThenBy(q => Math.Abs(q - jobs.Count / 2)).ToList();
            var lines_by_job = new string[jobs.Count];
            for (int step = 0; step < order.Count; step++)
            {
                int k = order[step];
                if (this == null || !isActiveAndEnabled || cam_for_uv_0 == null || cam_for_uv_1 == null)
                {
                    Debug.LogWarning("3D-Flussanalyse abgebrochen: Play-Modus beendet bzw. Kameras nicht mehr vorhanden.");
                    break;
                }
                ExperimentImageGallery.SetResultsText("3D-Fluss " + (step + 1) + " / " + jobs.Count + ": " + jobs[k].label);
                set_overall_progress(step + 1, jobs.Count, "3D-Fluss", jobs[k].label);
                set_experiment(jobs[k].label);
                stereo_last_mapping = null;
                await scene_flow_step(path_dic + remove_dots(jobs[k].label), jobs[k].pars);
                if (step == 0 && stereo_last_mapping.HasValue)
                {
                    stereo_mapping_lock = stereo_last_mapping;
                    Debug.Log("3D-Fluss: Zuordnung o" + stereo_mapping_lock.Value.o + "m" + stereo_mapping_lock.Value.m
                        + " an " + jobs[k].label + " bestimmt und fuer alle Stufen festgehalten.");
                }
                string tsv = root_path + "sceneflow_results.tsv";
                string last = File.Exists(tsv) ? File.ReadLines(tsv).Last() : "";
                string[] c = last.Split('\t');
                if (c.Length > 20 && c[0] == jobs[k].label)
                    lines_by_job[k] = c[0] + " | " + fmt_num(c[6], 1) + " | " + fmt_num(c[7], 100) + " | " + fmt_num(c[8], 1) + " / "
                        + fmt_num(c[9], 1) + " / " + fmt_num(c[10], 1) + " | " + fmt_num(c[15], 1) + " | " + fmt_num(c[18], 1) + "\n";
            }
            stereo_mapping_lock = null;
            foreach (string l in lines_by_job) if (l != null) summary.Append(l);
            summary.Append("Tabelle: " + root_path + "sceneflow_results.tsv\n(x = entlang der Stereobasis, y = quer, z = zu den Kameras)");

            // Kartenpanel je Stufe (scripts/plot_scene_flow.py), vorne in der Galerie
            string rs_p = get_render_res().ToString(ci);
            List<string> dirs = jobs.Select(j => path_dic + remove_dots(j.label)).ToList();
            string args = rs_p + " " + string.Join(" ", dirs.Select(d => "\"" + d + "\""));
            ExperimentImageGallery.SetResultsText(summary.ToString() + "\nKarten werden erzeugt ...");
            string plot_msg = await Task.Run(() => run_python(path_project + "scripts/plot_scene_flow.py", args));
            int ins = 0;
            foreach (string d in dirs)
            {
                string png = d + "/sceneflow_maps_r" + rs_p + ".png";
                if (File.Exists(png)) ExperimentImageGallery.InsertOrMoveImage(png, ins++);
            }
            if (ins > 0)
            {
                ExperimentImageGallery.ShowFirst();
                summary.Append("\nGalerie: Kartenpanel je Stufe (Weiter = naechste Stufe).");
            }
            if (plot_msg != "") summary.Append("\n" + plot_msg);
            ExperimentImageGallery.SetResultsText(summary.ToString());
            ExperimentImageGallery.ShowResultsWindow();
        }
        catch (Exception e)
        {
            Debug.LogError("3D-Flussanalyse abgebrochen: " + e);
        }
        finally
        {
            set_experiment(exp_before);
            stereo_running = false;
            if (is_tv_running) finish_all_tv_progress();
            reset_overall_progress();
        }
    }

    //27092026 Vergleich Wert <-> Referenz im berechneten Fenster (wie accuracy_block):
    //  Anzahl, Bias, RMSE, MAE, Korrelation, mittleres |Referenz|
    /// <summary>
    /// Compares a map with its reference inside the evaluated window.
    /// </summary>
    /// <param name="val">Measured map.</param>
    /// <param name="gt">Reference map.</param>
    /// <returns>Tuple (count, bias, RMSE, MAE, correlation, mean absolute reference).</returns>
    (int, double, double, double, double, double) pair_stats(List<List<float>> val, List<List<float>> gt)
    {
        int c_i = val.Count / 2, c_j = val[0].Count / 2;
        int n = 0;
        double s_d = 0, s_d2 = 0, s_ad = 0, s_x = 0, s_y = 0, s_xx = 0, s_yy = 0, s_xy = 0, s_ay = 0;
        for (int i = 6; i < 2 * c_i - 6; i++)
            for (int j = 6; j < 2 * c_j - 6; j++)
            {
                double x = val[i][j], y = gt[i][j];
                if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y)) continue;
                double d = x - y;
                n++; s_d += d; s_d2 += d * d; s_ad += Math.Abs(d);
                s_x += x; s_y += y; s_xx += x * x; s_yy += y * y; s_xy += x * y; s_ay += Math.Abs(y);
            }
        if (n == 0) return (0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
        double cov = s_xy / n - (s_x / n) * (s_y / n);
        double vx = s_xx / n - (s_x / n) * (s_x / n), vy = s_yy / n - (s_y / n) * (s_y / n);
        return (n, s_d / n, Math.Sqrt(s_d2 / n), s_ad / n, (vx > 0 && vy > 0) ? cov / Math.Sqrt(vx * vy) : double.NaN, s_ay / n);
    }

    // ============================================================================================
    //28092026 Nutzerwunsch: Belichtungsstudie. Die vorhandenen Kamerabilder der letzten Analyse werden je Stufe
    //  "belichtet" wie bei einer linearen Kamera: g' = min(255, round(k * g)) (k = relative Belichtungszeit;
    //  Unterbelichtung -> wenige Graustufen, Ueberbelichtung -> Saettigung). Optional Schrotrauschen mit
    //  N_peak Elektronen bei Vollaussteuerung: K ~ Poisson(k * g/255 * N_peak), g' = min(K, N_peak)/N_peak * 255.
    //  (29092026 korrigiert: die alte Lichtanalyse wirkte nicht, weil nur ein schwaches Zusatzlicht variiert wurde;
    //  jetzt skaliert "Licht: Neu" die Laborlampen light1/light2.)
    //  Je Stufe wird TV/TGV mit den aktuellen Einstellungen gerechnet (cv_main_async ueber im_paths) und wie bei
    //  "Genauigkeit" ausgewertet. Ergebnis: Assets/analysis_results/exposure_study_latest.tsv (+ Zeitstempel),
    //  Plots per scripts/plot_exposure_study.py. Danach wird der Basislauf (Originalbilder) wiederhergestellt.
    /// <summary>
    /// Exposure study on the existing camera images: scales them by factors k (8-bit clipping, optional shot noise N=...), computes and evaluates the flow for each stage, writes exposure_study_latest.tsv and plots; afterwards the original images are restored.
    /// </summary>
    /// <param name="spec">List of factors, optionally with N=&lt;electrons&gt;.</param>
    public async void run_exposure_study(string spec)
    {
        CultureInfo ci = CultureInfo.InvariantCulture;
        if (param_study_running || s_param_study_active)
        {
            param_study_cancel = true;
            s_param_study_cancel = true;
            ExperimentImageGallery.SetResultsText("Studie: Abbruch nach dem laufenden Durchgang ...");
            ExperimentImageGallery.ShowResultsWindow();
            return;
        }
        if (is_tv_running || get_is_started())
        {
            ExperimentImageGallery.SetResultsText("Belichtungsstudie: bitte warten, bis die laufende Berechnung fertig ist.");
            ExperimentImageGallery.ShowResultsWindow();
            return;
        }

        // Eingabe: Faktoren (Komma/Leerzeichen/Semikolon), optional "N=20000" fuer Schrotrauschen
        List<float> levels = new List<float>();
        float n_peak = 0f;
        foreach (string raw in (spec ?? "").Replace(";", ",").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string tok = raw.Trim();
            if (tok.StartsWith("N=", StringComparison.OrdinalIgnoreCase) || tok.StartsWith("N:", StringComparison.OrdinalIgnoreCase))
            {
                float.TryParse(tok.Substring(2).Replace(',', '.'), NumberStyles.Float, ci, out n_peak);
                continue;
            }
            if (float.TryParse(tok, NumberStyles.Float, ci, out float k) && k > 0f && !levels.Contains(k))
                levels.Add(k);
        }
        levels.Sort();
        if (levels.Count == 0)
        {
            ExperimentImageGallery.SetResultsText("Belichtungsstudie: bitte Faktoren eingeben, z.B. \"0.1, 0.25, 0.5, 1, 2, 4\" (optional \"N=20000\").");
            ExperimentImageGallery.ShowResultsWindow();
            return;
        }
        if (!flow_results_available(out string missing))
        {
            ExperimentImageGallery.SetResultsText("Belichtungsstudie braucht eine fertige Analyse bei r" + get_render_res()
                + " (with_exp + with_tv -> Start). Fehlt: " + missing);
            ExperimentImageGallery.ShowResultsWindow();
            return;
        }
        string exp = get_experiment();
        string proj_dir = path_dic + remove_dots(exp);
        Dictionary<int, byte[]> originals = new Dictionary<int, byte[]>();
        foreach (int f in blade_idxs.Distinct())
        {
            string im = proj_dir + "/cam_0/uv/" + category + "im_" + f + "_r" + get_render_res() + ".png";
            if (!File.Exists(im))
            {
                ExperimentImageGallery.SetResultsText("Belichtungsstudie: gerendertes Bild fehlt (" + im + ").");
                ExperimentImageGallery.ShowResultsWindow();
                return;
            }
            originals[f] = File.ReadAllBytes(im);
        }

        string img_dir = param_study_dir() + "exposure_images/";
        Directory.CreateDirectory(img_dir);
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        string tsv_latest = param_study_dir() + "exposure_study_latest.tsv";
        string tsv_stamp = param_study_dir() + "exposure_study_" + stamp + ".tsv";
        List<string> lines = new List<string>
        {
            "# Belichtungsstudie " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + " | " + exp + " | r" + get_render_res()
                + " | Frames " + describe_frames() + " | Speckle " + describe_speckle_texture()
                + " | Strain-sigma " + strain_sigma.ToString("0.##", ci) + " px"
                + " | Schrotrauschen " + (n_peak > 0f ? "N_peak " + n_peak.ToString("G6", ci) : "aus"),
            "# TV: " + describe_tv_overrides() + " | Regularisierung " + describe_regularization(),
            string.Join("\t", new[] { "run", "exposure", "n_peak", "mean_gray", "std_gray", "saturated_pct", "black_pct",
                "levels_used", "time_s", "u_bias", "u_rmse", "u_mae", "u_corr", "v_bias", "v_rmse", "v_mae", "v_corr",
                "exx_mae", "exx_rmse", "exx_corr", "exx_rel_mae", "eyy_mae", "eyy_rmse", "eyy_corr", "eyy_rel_mae" })
        };

        param_study_running = true;
        param_study_cancel = false;
        s_param_study_active = true;
        s_param_study_cancel = false;
        break_now = false;
        List<string> im_paths_before = new List<string>(im_paths);
        int done = 0;
        exposure_bypass = true; //28092026 Bilder der Studie sind bereits belichtet -> Analyse-k nicht zusaetzlich anwenden
        try
        {
            for (int k = 0; k < levels.Count; k++)
            {
                if (param_study_cancel || s_param_study_cancel || this == null || !isActiveAndEnabled || tv_gpu_shutdown)
                    break;
                float kexp = levels[k];

                // belichtete Bilder erzeugen (Statistik vom ersten Frame, Probenmitte)
                Dictionary<int, string> paths = new Dictionary<int, string>();
                (double mean_g, double std_g, double sat, double blk, int n_levels) = (double.NaN, double.NaN, double.NaN, double.NaN, 0);
                foreach (var kv in originals)
                {
                    string outp = img_dir + "im_" + kv.Key + "_k" + exposure_label(kexp) + ".png";
                    var st = write_exposed_image(kv.Value, outp, kexp, n_peak, seed: kv.Key * 7919 + k);
                    if (kv.Key == blade_idxs[0])
                        (mean_g, std_g, sat, blk, n_levels) = st;
                    paths[kv.Key] = outp;
                }
                im_paths = blade_idxs.Select(f => paths[f]).ToList();

                progress_prefix = "[Belichtungsstudie " + (k + 1) + "/" + levels.Count + ": k = " + exposure_label(kexp) + "] ";
                progress_kind = "Belichtungsstudie"; //29092026
                set_series_progress_info(k + 1, levels.Count);
                ExperimentImageGallery.SetResultsText("Belichtungsstudie: Stufe " + (k + 1) + " / " + levels.Count
                    + " (k = " + exposure_label(kexp) + (n_peak > 0f ? ", N_peak " + n_peak.ToString("G6", ci) : "")
                    + ")\nBild: Mittel " + mean_g.ToString("0.0", ci) + ", gesaettigt " + sat.ToString("0.00", ci) + " %"
                    + "\n(Knopf \"Belichtung: Neu\" erneut druecken = Abbruch nach dieser Stufe)");
                ExperimentImageGallery.ShowResultsWindow();

                Stopwatch sw = Stopwatch.StartNew();
                await cv_main_async(0, d_cam: 0, with_dt: true, exp_label: exp, pars: null, proj_dir_override: proj_dir);
                double secs = sw.Elapsed.TotalSeconds;
                Dictionary<string, double> m = compute_study_metrics();
                Func<string, string> g = key => m.TryGetValue(key, out double val) ? val.ToString("G6", ci) : "NaN";
                lines.Add(string.Join("\t", new[] { (k + 1).ToString(), kexp.ToString("G6", ci), n_peak.ToString("G6", ci),
                    mean_g.ToString("G5", ci), std_g.ToString("G5", ci), sat.ToString("G5", ci), blk.ToString("G5", ci),
                    n_levels.ToString(), secs.ToString("F2", ci),
                    g("u_bias"), g("u_rmse"), g("u_mae"), g("u_corr"), g("v_bias"), g("v_rmse"), g("v_mae"), g("v_corr"),
                    g("exx_mae"), g("exx_rmse"), g("exx_corr"), g("exx_rel_mae"),
                    g("eyy_mae"), g("eyy_rmse"), g("eyy_corr"), g("eyy_rel_mae") }));
                System.IO.File.WriteAllLines(tsv_latest, lines);
                System.IO.File.WriteAllLines(tsv_stamp, lines);
                done++;
                Debug.Log("Belichtungsstudie k=" + kexp + ": gesaettigt " + sat.ToString("0.00", ci) + " %, u MAE " + g("u_mae")
                    + ", exx rel " + g("exx_rel_mae"));

                //28092026 Nutzerwunsch: je Stufe die Felder sichern (Genauigkeits-Panels, Bericht, Rohdaten, belichtetes
                //  Eingangsbild) -> exposure_study_plots/k_<k>/; scripts/plot_exposure_study.py zeigt sie nebeneinander
                try
                {
                    save_accuracy_analysis();
                    string kdir = param_study_dir() + "exposure_study_plots/k_" + exposure_label(kexp) + "/";
                    Directory.CreateDirectory(kdir);
                    string nice = path_dic + "exp_normal/time_flow_v/nice_pics/";
                    string rs = "_r" + get_render_res();
                    string exp_name = get_experiment();
                    string kp = "k" + exposure_label(kexp) + "_"; // k im Dateinamen -> in der Galerie sichtbar
                    foreach (string f in Directory.GetFiles(nice, "accuracy_*" + rs + "*"))
                    {
                        string fn = Path.GetFileName(f);
                        if (fn.EndsWith(".png"))
                        {
                            // Panels: kurze, sortierbare Namen mit k (1_u, 2_v, 3_exx, 4_eyy)
                            string part = fn.Contains("_" + exp_name + "_u_") ? "1_u_flow" : fn.Contains("_" + exp_name + "_v_") ? "2_v_flow"
                                : fn.Contains("_exx_") ? "3_exx_strain" : fn.Contains("_eyy_") ? "4_eyy_strain" : null;
                            if (part == null || fn.Contains("_oben-")) continue; // alte 2x2-Panels nicht mitnehmen
                            File.Copy(f, kdir + kp + part + ".png", true);
                        }
                        else
                        {
                            File.Copy(f, kdir + fn, true); // Bericht und Rohdaten (Namen fuer die Skripte unveraendert)
                        }
                    }
                    File.Copy(paths[blade_idxs[0]], kdir + kp + "0_input_frame" + blade_idxs[0] + ".png", true);
                    if (paths.ContainsKey(blade_idxs[1]))
                        File.Copy(paths[blade_idxs[1]], kdir + kp + "0_input_frame" + blade_idxs[1] + ".png", true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Felder der Stufe k=" + kexp + " nicht gesichert: " + e.Message);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Belichtungsstudie abgebrochen: " + e);
            ExperimentImageGallery.SetResultsText("Belichtungsstudie abgebrochen: " + e.Message + "\nBisherige Ergebnisse: " + tsv_latest);
        }
        finally
        {
            im_paths = im_paths_before; // wieder die gerenderten Originalbilder
            exposure_bypass = false;
            param_study_running = false;
            s_param_study_active = false;
        }
        if (this == null || !isActiveAndEnabled || tv_gpu_shutdown)
        {
            progress_prefix = "";
            return;
        }

        // Basislauf mit den Originalbildern wiederherstellen
        try
        {
            progress_prefix = "[Belichtungsstudie: Basislauf wird wiederhergestellt] ";
            series_current_idx = 1;
            series_total_count = 1;
            progress_stopwatch.Reset();
            await cv_main_async(0, d_cam: 0, with_dt: true, exp_label: exp, pars: null, proj_dir_override: proj_dir);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Basislauf nach der Belichtungsstudie fehlgeschlagen: " + e.Message);
        }
        finally
        {
            progress_prefix = "";
            progress_kind = "Parameterstudie";
        }
        await show_exposure_study(regenerate_plots: true);
    }

    //28092026 Bugfix: k fuer Datei-/Ordnernamen und Anzeige ungerundet (float, bis 9 gueltige Stellen, ohne
    //  Exponentenschreibweise). Vorher "0.####": 0.01662 und 0.01664 wurden beide zu "0.0166" -> Stufen
    //  ueberschrieben sich gegenseitig.
    /// <summary>
    /// Unrounded text form of an exposure factor for file names and display.
    /// </summary>
    /// <param name="k">Exposure factor.</param>
    /// <returns>Label text.</returns>
    static string exposure_label(float k)
    {
        string s = ((decimal)k).ToString("0.#############", CultureInfo.InvariantCulture);
        return s;
    }

    //28092026 Belichtungsfaktor fuer die normale Analyse (PlayerPrefs "analysis_exposure", 1 = Original).
    //  exposure_bypass: waehrend der Belichtungsstudie aus (deren Bilder sind bereits belichtet).
    private float analysis_exposure = 1f;
    private bool exposure_bypass = false;
    /// <summary>
    /// Returns the exposure factor k of the normal analysis.
    /// </summary>
    /// <returns>Exposure factor (1 = unmodified images).</returns>
    public float get_analysis_exposure() { return analysis_exposure; }
    //28092026 gueltiger Bereich: jeder Wert > 0 (Nutzerwunsch: beliebig klein, um den Zusammenbruch zu sehen);
    //  obere Grenze nur gegen Tippfehler. Rueckgabe: Wert gueltig
    public const float EXPOSURE_MIN = 1e-9f, EXPOSURE_MAX = 1000f;
    /// <summary>
    /// Sets and stores the exposure factor k of the normal analysis (applied to the images before the flow computation, ignored in sweeps).
    /// </summary>
    /// <param name="k">Exposure factor (greater than 0).</param>
    /// <returns>True if the value was valid.</returns>
    public bool set_analysis_exposure(float k)
    {
        bool ok = !float.IsNaN(k) && k >= EXPOSURE_MIN && k <= EXPOSURE_MAX;
        if (!ok)
        {
            Debug.LogWarning("Belichtung k = " + k + " ausserhalb " + EXPOSURE_MIN + " .. " + EXPOSURE_MAX
                + " - bleibt bei " + analysis_exposure);
            return false;
        }
        analysis_exposure = k;
        PlayerPrefs.SetFloat("analysis_exposure", analysis_exposure);
        PlayerPrefs.Save();
        Debug.Log("Belichtung fuer die Analyse: k = " + analysis_exposure.ToString("G4", CultureInfo.InvariantCulture));
        return true;
    }

    //28092026 belichtetes TV-Eingangsbild zum Ansehen speichern (Werte 0..1 in Textur-Reihenfolge wie tex2floats)
    /// <summary>
    /// Saves the exposed input image of the flow computation for inspection (analysis_results/exposure_images).
    /// </summary>
    /// <param name="mat">Gray values 0..1 in texture order.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <param name="src_path">Path of the original image (for the file name).</param>
    void save_exposed_preview(List<float> mat, int width, int height, string src_path)
    {
        try
        {
            string dir = param_study_dir() + "exposure_images/";
            Directory.CreateDirectory(dir);
            Texture2D t = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color32[] px = new Color32[width * height];
            for (int q = 0; q < px.Length && q < mat.Count; q++)
            {
                byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(mat[q] * 255f), 0, 255);
                px[q] = new Color32(b, b, b, 255);
            }
            t.SetPixels32(px);
            t.Apply();
            string name = Path.GetFileNameWithoutExtension(src_path) + "_analyse_k"
                + exposure_label(analysis_exposure) + ".png";
            System.IO.File.WriteAllBytes(dir + name, t.EncodeToPNG());
            Destroy(t);
            //28092026 Nutzerwunsch: belichtetes Bild in der Galerie zeigen (ersetzt die Fassung eines frueheren Laufs)
            ExperimentImageGallery.AddOrReplaceImage(dir + name);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Belichtetes Vorschaubild nicht gespeichert: " + e.Message);
        }
    }

    //28092026 Belichtetes Bild schreiben; Rueckgabe: Mittel/Std des Grauwerts, Anteil gesaettigt/schwarz [%] und
    //  Anzahl genutzter Graustufen (jeweils in der Bildmitte, 50 % der Flaeche)
    /// <summary>
    /// Writes an exposed (and optionally noisy) version of an image.
    /// </summary>
    /// <param name="png">PNG bytes of the original.</param>
    /// <param name="out_path">Output file.</param>
    /// <param name="k">Exposure factor.</param>
    /// <param name="n_peak">Full-scale electrons for shot noise (0 = off).</param>
    /// <param name="seed">Random seed of the noise.</param>
    /// <returns>Tuple (mean, std of the gray value, saturated %, black %, number of gray levels) in the image centre.</returns>
    static (double, double, double, double, int) write_exposed_image(byte[] png, string out_path, float k, float n_peak, int seed)
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(png);
        Color32[] px = tex.GetPixels32();
        System.Random rnd = new System.Random(seed);
        int w = tex.width, h = tex.height;
        double s = 0, s2 = 0;
        int n = 0, n_sat = 0, n_blk = 0;
        bool[] used = new bool[256];
        for (int idx = 0; idx < px.Length; idx++)
        {
            float gray = px[idx].g; // Kamerabilder sind grau (R = G = B); manage_read_im liest Kanal 1
            float v = k * gray;
            if (n_peak > 0f)
            {
                double lambda = Math.Min(v / 255.0, 1e6) * n_peak;
                int cnt = sample_poisson(rnd, lambda);
                v = (float)(Math.Min(cnt, n_peak) / n_peak * 255.0);
            }
            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(v), 0, 255);
            px[idx] = new Color32(b, b, b, 255);
            int x = idx % w, y = idx / w;
            if (x >= w / 4 && x < 3 * w / 4 && y >= h / 4 && y < 3 * h / 4)
            {
                s += b; s2 += (double)b * b; n++;
                if (b >= 255) n_sat++;
                if (b == 0) n_blk++;
                used[b] = true;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        System.IO.File.WriteAllBytes(out_path, tex.EncodeToPNG());
        UnityEngine.Object.Destroy(tex);
        double mean = n > 0 ? s / n : double.NaN;
        return (mean, n > 0 ? Math.Sqrt(Math.Max(0, s2 / n - mean * mean)) : double.NaN,
            n > 0 ? 100.0 * n_sat / n : double.NaN, n > 0 ? 100.0 * n_blk / n : double.NaN, used.Count(u => u));
    }

    /// <summary>
    /// Shows the last exposure study (summary and plots).
    /// </summary>
    /// <param name="regenerate_plots">True to regenerate the plots.</param>
    public async Task show_exposure_study(bool regenerate_plots = false)
    {
        string tsv = param_study_dir() + "exposure_study_latest.tsv";
        if (!File.Exists(tsv))
        {
            ExperimentImageGallery.SetResultsText("Noch keine Belichtungsstudie vorhanden (" + tsv + ").");
            ExperimentImageGallery.ShowResultsWindow();
            return;
        }
        string plot_dir = param_study_dir() + "exposure_study_plots/";
        string plot = plot_dir + "exposure_study.png";
        string msg = "";
        if (regenerate_plots || !File.Exists(plot) || File.GetLastWriteTimeUtc(plot) < File.GetLastWriteTimeUtc(tsv))
            msg = await Task.Run(() => run_python(path_project + "scripts/plot_exposure_study.py", "\"" + tsv + "\" \"" + plot_dir + "\""));

        // Kurzfassung: k | Mittel | gesaettigt | u/v-MAE | exx/eyy rel.
        CultureInfo ci = CultureInfo.InvariantCulture;
        string[] all = File.ReadAllLines(tsv);
        List<string> rows = all.Where(l => !l.StartsWith("#") && l.Trim() != "").ToList();
        StringBuilder sb = new StringBuilder();
        foreach (string c in all.Where(l => l.StartsWith("#"))) sb.Append(c.TrimStart('#', ' ')).Append('\n');
        if (rows.Count > 1)
        {
            string[] head = rows[0].Split('\t');
            Func<string[], string, double> col = (cells, name) =>
            {
                int i = Array.IndexOf(head, name);
                return i >= 0 && i < cells.Length && double.TryParse(cells[i], NumberStyles.Float, ci, out double d) ? d : double.NaN;
            };
            sb.Append("k | Grau-Mittel | gesaettigt % | u/v-MAE [px] | exx/eyy rel. [%]\n");
            foreach (string r in rows.Skip(1))
            {
                string[] cells = r.Split('\t');
                sb.Append(string.Format(ci, "{0,-6} {1,6:0.0} {2,6:0.00} | {3:0.000} / {4:0.000} | {5:0.0} / {6:0.0}\n",
                    col(cells, "exposure"), col(cells, "mean_gray"), col(cells, "saturated_pct"), col(cells, "u_mae"),
                    col(cells, "v_mae"), 100 * col(cells, "exx_rel_mae"), 100 * col(cells, "eyy_rel_mae")));
            }
        }
        sb.Append("Tabelle: ").Append(tsv);
        ExperimentImageGallery.SetResultsText(sb.ToString() + (msg == "" ? "" : "\n" + msg));
        //28092026 Nutzerwunsch: das normale Panel zeigen und mit "Weiter" dasselbe Panel beim naechsten k.
        //  Reihenfolge ganz vorne in der Galerie: Flow u fuer alle k (aufsteigend), dann Flow v, exx, eyy,
        //  dann die belichteten Eingangsbilder; die Uebersichtsbilder ganz am Ende.
        List<string> ordered = new List<string>();
        if (Directory.Exists(plot_dir))
        {
            var kdirs = new List<(double kv, string d)>();
            foreach (string d in Directory.GetDirectories(plot_dir, "k_*"))
                if (double.TryParse(Path.GetFileName(d).Substring(2), NumberStyles.Float, ci, out double kv))
                    kdirs.Add((kv, d));
            kdirs.Sort((a, b) => a.kv.CompareTo(b.kv));
            foreach (string part in new[] { "_1_u_flow", "_2_v_flow", "_3_exx_strain", "_4_eyy_strain", "_0_input_frame" })
                foreach (var (kv, d) in kdirs)
                    ordered.AddRange(Directory.GetFiles(d, "k*" + part + "*.png").OrderBy(p => Path.GetFileName(p), StringComparer.Ordinal));
        }
        string fields = plot_dir + "exposure_fields.png";
        if (File.Exists(fields)) ordered.Add(fields);
        if (File.Exists(plot)) ordered.Add(plot);
        for (int q = 0; q < ordered.Count; q++)
            ExperimentImageGallery.InsertOrMoveImage(ordered[q], q);
        ExperimentImageGallery.ShowFirst();
        if (ordered.Count > 2)
            sb.Append("\nGalerie: Flow u je k (Weiter = naechstes k), dann v, exx, eyy, Eingangsbilder, Uebersichten.");
        ExperimentImageGallery.SetResultsText(sb.ToString() + (msg == "" ? "" : "\n" + msg));
        ExperimentImageGallery.ShowResultsWindow();
    }

    //27092026 Letzte Studie anzeigen: Zusammenfassung ins Ergebnisfenster, Plots (matplotlib) in die Galerie.
    //  regenerate_plots: Plot-Skript ausfuehren (sonst nur, wenn Plots fehlen oder aelter als die Tabelle sind)
    /// <summary>
    /// Shows the last parameter study: summary in the result window and plots in the gallery.
    /// </summary>
    /// <param name="regenerate_plots">True to rerun the plot script (otherwise only if plots are missing or outdated).</param>
    public async Task show_param_study(bool regenerate_plots = false)
    {
        string tsv = param_study_dir() + "param_study_latest.tsv";
        if (!File.Exists(tsv))
        {
            ExperimentImageGallery.SetResultsText("Noch keine Parameterstudie vorhanden (" + tsv + ").");
            ExperimentImageGallery.ShowResultsWindow();
            return;
        }
        string plot_dir = param_study_dir() + "param_study_plots/";
        string overview = plot_dir + "param_study_overview.png";
        bool stale = !File.Exists(overview) || File.GetLastWriteTimeUtc(overview) < File.GetLastWriteTimeUtc(tsv);
        string plot_msg = "";
        if (regenerate_plots || stale)
        {
            string script = path_project + "scripts/plot_param_study.py";
            plot_msg = await Task.Run(() => run_python(script, "\"" + tsv + "\" \"" + plot_dir + "\""));
        }

        string summary = summarize_param_study(tsv);
        ExperimentImageGallery.SetResultsText(summary + (plot_msg == "" ? "" : "\n" + plot_msg));
        if (Directory.Exists(plot_dir))
        {
            List<string> pngs = Directory.GetFiles(plot_dir, "*.png").OrderBy(p => p.Contains("overview") ? 0 : 1)
                .ThenBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
            for (int k = 0; k < pngs.Count; k++)
                ExperimentImageGallery.InsertRenderedImage(pngs[k], k);
        }
        ExperimentImageGallery.ShowResultsWindow();
    }

    //27092026 Python-Skript ausfuehren (fuer die Plots); Rueckgabe: "" bei Erfolg, sonst Hinweistext
    /// <summary>
    /// Runs a Python script (python, py, or python3) for the plots.
    /// </summary>
    /// <param name="script">Path of the script.</param>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Empty string on success, otherwise a hint text.</returns>
    static string run_python(string script, string args)
    {
        foreach (string exe in new[] { "python", "py", "python3" })
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, "\"" + script + "\" " + args)
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                using (Process pr = Process.Start(psi))
                {
                    string err = pr.StandardError.ReadToEnd();
                    pr.StandardOutput.ReadToEnd();
                    if (!pr.WaitForExit(120000))
                    {
                        try { pr.Kill(); } catch (Exception) { }
                        return "Plot-Skript: Zeitueberschreitung.";
                    }
                    if (pr.ExitCode == 0)
                        return "";
                    return "Plot-Skript fehlgeschlagen (" + exe + "): " + (err.Length > 300 ? err.Substring(err.Length - 300) : err);
                }
            }
            catch (Exception)
            {
                // naechsten Interpreter versuchen
            }
        }
        return "Plots nicht erzeugt: kein Python gefunden (benoetigt numpy, matplotlib). Tabelle: siehe TSV.";
    }

    //27092026 Kurzfassung der Studie fuer das Ergebnisfenster: je Parametergruppe die Werte mit
    //  Fluss-MAE (u, v) und relativem Dehnungsfehler (exx, eyy); bester Wert je Gruppe (min. exx-Fehler) markiert
    /// <summary>
    /// Short summary of the parameter study: per parameter group the values with flow MAE and relative strain error; the best value per group is marked.
    /// </summary>
    /// <param name="tsv">Result table.</param>
    /// <returns>Summary text.</returns>
    static string summarize_param_study(string tsv)
    {
        CultureInfo ci = CultureInfo.InvariantCulture;
        string[] all = File.ReadAllLines(tsv);
        List<string> comments = all.Where(l => l.StartsWith("#")).ToList();
        List<string> rows = all.Where(l => !l.StartsWith("#") && l.Trim() != "").ToList();
        if (rows.Count < 2)
            return string.Join("\n", comments) + "\n(noch keine Laeufe)";
        string[] head = rows[0].Split('\t');
        Func<string[], string, string> col = (cells, name) =>
        {
            int i = Array.IndexOf(head, name);
            return i >= 0 && i < cells.Length ? cells[i] : "";
        };
        Func<string, double> num = s => double.TryParse(s, NumberStyles.Float, ci, out double d) ? d : double.NaN;
        StringBuilder sb = new StringBuilder();
        foreach (string c in comments) sb.Append(c.TrimStart('#', ' ')).Append('\n');
        sb.Append("Wert: u-MAE / v-MAE [px] | exx / eyy rel. Fehler [%] | Zeit [s]\n");
        var data = rows.Skip(1).Select(l => l.Split('\t')).ToList();
        foreach (var grp in data.GroupBy(cells => col(cells, "group")))
        {
            string name = grp.Key;
            string key = name == "regularisierung" ? "regularization" : name;
            double best = grp.Select(cells => num(col(cells, "exx_rel_mae"))).Where(x => !double.IsNaN(x)).DefaultIfEmpty(double.NaN).Min();
            sb.Append(name).Append(":\n");
            foreach (var cells in grp)
            {
                string label = name == "basis" ? "Basis"
                    : name == "regularisierung" ? (col(cells, "regularization") == "TGV" ? "TGV " + col(cells, "tgv_ratio") : "TV")
                    : col(cells, key);
                double exx = num(col(cells, "exx_rel_mae"));
                sb.Append(string.Format(ci, "  {0,-8} {1:0.000} / {2:0.000} | {3:0.0} / {4:0.0} | {5:0.0}{6}\n",
                    label, num(col(cells, "u_mae")), num(col(cells, "v_mae")), 100 * exx,
                    100 * num(col(cells, "eyy_rel_mae")), num(col(cells, "time_s")), exx == best && grp.Count() > 1 ? "  <- min" : ""));
            }
        }
        sb.Append("Tabelle: ").Append(tsv);
        return sb.ToString();
    }

    //23092026 Statistikblock einer Komponente fuer save_accuracy_analysis
    //27092026 unit/dec: Einheit und Nachkommastellen (Fluss in px mit 2-3 Stellen, Dehnung dimensionslos mit mehr)
    /// <summary>
    /// Statistics block of one component for the accuracy report.
    /// </summary>
    /// <param name="comp">Component name.</param>
    /// <param name="val">Measured map.</param>
    /// <param name="gt">Reference map.</param>
    /// <param name="loss_abs">Absolute error map.</param>
    /// <param name="loss_rel">Relative error map.</param>
    /// <param name="unit">Unit shown in the report.</param>
    /// <param name="dec">Number of decimals.</param>
    /// <returns>Report text.</returns>
    string accuracy_block(string comp, List<List<float>> val, List<List<float>> gt,
        List<List<float>> loss_abs, List<List<float>> loss_rel, string unit = "px", int dec = 2)
    {
        CultureInfo ci = CultureInfo.InvariantCulture;
        if (val == null || gt == null || loss_abs == null || loss_rel == null)
            return comp + ": keine Daten\n";

        // Paare (TV, Ground Truth) innerhalb der Probe
        int n = 0;
        double s_d = 0, s_d2 = 0, s_ad = 0, s_x = 0, s_y = 0, s_xx = 0, s_yy = 0, s_xy = 0;
        float v_min = float.PositiveInfinity, v_max = float.NegativeInfinity;
        float g_min = float.PositiveInfinity, g_max = float.NegativeInfinity;
        List<float> abs_vals = new List<float>();
        List<float> rel_vals = new List<float>();
        //23092026 Bugfix: nur das Fenster auswerten, das choose_flow_or_loss wirklich berechnet
        //  (Mitte +- (Mitte - 6)); ausserhalb stehen unbearbeitete Rohwerte (z.B. -511), die
        //  Bias/Mittelwerte/corr verfaelschten.
        int c_i = val.Count / 2;
        int c_j = val[0].Count / 2;
        int i_lo = c_i - (c_i - 6), i_hi = c_i + (c_i - 6);
        int j_lo = c_j - (c_j - 6), j_hi = c_j + (c_j - 6);
        for (int i = i_lo; i < i_hi; i++)
        {
            for (int j = j_lo; j < j_hi; j++)
            {
                float x = val[i][j];
                float y = gt[i][j];
                if (!float.IsNaN(x) && !float.IsNaN(y) && !float.IsInfinity(x) && !float.IsInfinity(y))
                {
                    double d = x - y;
                    n++;
                    s_d += d; s_d2 += d * d; s_ad += Math.Abs(d);
                    s_x += x; s_y += y; s_xx += (double)x * x; s_yy += (double)y * y; s_xy += (double)x * y;
                    v_min = Mathf.Min(v_min, x); v_max = Mathf.Max(v_max, x);
                    g_min = Mathf.Min(g_min, y); g_max = Mathf.Max(g_max, y);
                }
                float a = loss_abs[i][j];
                if (!float.IsNaN(a) && !float.IsInfinity(a))
                    abs_vals.Add(a);
                float r = loss_rel[i][j];
                if (!float.IsNaN(r) && !float.IsInfinity(r))
                    rel_vals.Add(r);
            }
        }
        if (n == 0)
            return comp + ": keine gueltigen Pixel\n";

        double bias = s_d / n;
        double rmse = Math.Sqrt(s_d2 / n);
        double mae = s_ad / n;
        double cov = s_xy / n - (s_x / n) * (s_y / n);
        double var_x = s_xx / n - (s_x / n) * (s_x / n);
        double var_y = s_yy / n - (s_y / n) * (s_y / n);
        double corr = (var_x > 0 && var_y > 0) ? cov / Math.Sqrt(var_x * var_y) : double.NaN;

        abs_vals.Sort();
        rel_vals.Sort();
        Func<List<float>, double, float> pct = (list, q) =>
            list.Count == 0 ? float.NaN : list[Mathf.Clamp((int)Math.Round(q * (list.Count - 1)), 0, list.Count - 1)];
        Func<List<float>, double> mean = list => list.Count == 0 ? double.NaN : list.Average(f => (double)f);
        Func<List<float>, float, double> share_below = (list, lim) =>
            list.Count == 0 ? double.NaN : 100.0 * list.Count(f => f < lim) / list.Count;

        StringBuilder sb = new StringBuilder();
        string f1 = "F" + dec, f2 = "F" + (dec + 1);
        string u_s = unit == "" ? "" : " " + unit;
        sb.Append(comp + " (" + n + " Pixel in der Probe)\n");
        sb.Append(string.Format(ci, "  TV {0} .. {1}{4} | GT {2} .. {3}{4}\n", v_min.ToString(f1, ci), v_max.ToString(f1, ci),
            g_min.ToString(f1, ci), g_max.ToString(f1, ci), u_s));
        sb.Append(string.Format(ci, "  Wert-Ref: Bias {0} | RMSE {1} | MAE {2}{3} | corr {4:F4}\n",
            bias.ToString(f2, ci), rmse.ToString(f2, ci), mae.ToString(f2, ci), u_s, corr));
        sb.Append(string.Format(ci, "  loss_abs ({0} Px, {1:F1} %): Mittel {2} | Median {3} | P95 {4} | max {5}{6}\n",
            abs_vals.Count, 100.0 * abs_vals.Count / n, mean(abs_vals).ToString(f2, ci), pct(abs_vals, 0.5).ToString(f2, ci),
            pct(abs_vals, 0.95).ToString(f2, ci), (abs_vals.Count > 0 ? abs_vals[abs_vals.Count - 1] : float.NaN).ToString(f2, ci), u_s));
        sb.Append(string.Format(ci, "  loss_rel ({0} Px, {1:F1} %): Mittel {2:F1} % | Median {3:F1} % | P95 {4:F1} % | <5 %: {5:F1} % | <10 %: {6:F1} % der Px\n",
            rel_vals.Count, 100.0 * rel_vals.Count / n, 100.0 * mean(rel_vals), 100.0 * pct(rel_vals, 0.5),
            100.0 * pct(rel_vals, 0.95), share_below(rel_vals, 0.05f), share_below(rel_vals, 0.10f)));
        return sb.ToString();
    }

    /// <summary>
    /// Assigns the display plane to the cameras and refreshes the scene-UI controls.
    /// </summary>
    /// <param name="platine_plane">Display plane of the maps.</param>
    /// <param name="t_idx">Time index.</param>
    public void manage_assign(GameObject platine_plane, int t_idx)
    {
        assign_to_cam(platine_plane);

        // info (paul): assign to t_panel
        //20092026 Die Panels registrieren sich einzeln (set_*_control); nicht jedes
        //existiert in der Szene -> jedes einzeln auf null pruefen statt nur t_control.
        if (match_control != null)
            match_control.refresh_panel(t_idx);
        if (t_control != null)
            t_control.refresh_t_panel(t_idx);
        if (strain_control != null)
            strain_control.refresh_info(strain_mode);
        if (u_v_control != null)
            u_v_control.refresh_info(u_v_mode);
        if (strain_d != null)
            strain_d.refresh_info(strain_d_mode);
        if (experiment_control != null)
            experiment_control.refresh_info(experiment);
    }

    /// <summary>
    /// Resolves the flow folder and time index of the current display.
    /// </summary>
    /// <param name="path_time_flow_v">Flow folder (null = default).</param>
    /// <param name="t_idx">Time index (-1 = current).</param>
    /// <returns>Tuple (flow folder, time index).</returns>
    public (string, int) refresh_params(string path_time_flow_v, int t_idx)
    {
        //13012025 if (path_stereo == null) { path_stereo = this.get_path_stereo(); }
        //13012025 if (path_time_flow_u == null) { path_time_flow_u = this.get_path_time_flow_u(); }
        if (path_time_flow_v == null) { path_time_flow_v = this.get_path_time_flow_v(); }
        //if (res_x == -1) { res_x = this.res_x; }
        //if (res_y == -1) { res_y = this.res_y; }
        //13012025 if (im_cnt == -1) { im_cnt = this.im_cnt; }
        //20092026 t_idx = -1 (Startwert) haette in match_all_to_start t_max = -1 zur Folge,
        //d.h. es wird gar kein Flussfeld akkumuliert (Karte komplett 0). Das TV-Feld
        //beschreibt Frame 0 -> 1, also mindestens t_idx = 1 verwenden.
        if (this.get_t_idx() < 1) { this.set_t_idx(1); }
        //22092026 ... und nach oben auf die Anzahl vorhandener Flussfelder klemmen: die
        //Initialisierung setzt t_idx = blade_idxs.Count-1 (=2), dazu gibt es aber weder
        //ein Flussfeld noch eine Topologie-Datei (3__blade_tris) -> NullReference in find_node.
        int t_idx_max = Mathf.Max(1, blade_idxs.Count - 2);
        if (this.get_t_idx() > t_idx_max)
        {
            Debug.LogWarning("t_idx " + this.get_t_idx() + " > " + t_idx_max + " (Anzahl Flussfelder) -> auf " + t_idx_max + " gesetzt.");
            this.set_t_idx(t_idx_max);
        }
        if (t_idx == -1) { t_idx = this.get_t_idx(); }
        if (get_blade_tris() == null) { set_blade_tris(init_tris_empty()); }
        return (path_time_flow_v, t_idx);
    }
    /// <summary>
    /// Loads measured and reference depth maps of the current display (legacy height display).
    /// </summary>
    /// <returns>Tuple (depth, reference depth, scale).</returns>
    public (List<List<float>>, List<List<float>>, float) manage_heights()
    {
        // info (paul): heights/ heights_ref/ heights_diff
        bool force_flat = this.get_force_flat();

        //08102024 List<List<float>> heights = read_dists(path_stereo, t_idx);
        //20092026 Die Stereo-/Tiefenkarten (heights) entstehen nur im Stereo-Schritt, der im
        //aktuellen with_exp/with_tv-Workflow nicht laeuft. Fuer die Flusskarten (paint_with
        //== "uv": value/value_ref/loss_abs/loss_rel) werden sie nicht gebraucht -> bei
        //fehlenden Dateien auf flache Null-Hoehen ausweichen statt mit Exception abzubrechen.
        List<List<float>> heights, heights_uncut, heights_ref, heights_ref_uncut;
        try
        {
            (heights, heights_uncut) = read_heights_tv();//26022025 trivial_heights();//11012024 read_heights_tv();
        }
        catch (Exception exception) when (exception is FileNotFoundException || exception is DirectoryNotFoundException || exception is NullReferenceException)
        {
            Debug.LogWarning("manage_heights: keine TV-Hoehenkarte, verwende flache Hoehen. " + exception.Message);
            (heights, heights_uncut) = trivial_heights();
        }
        try
        {
            (heights_ref, heights_ref_uncut) = load_heights_ref();//26022025 trivial_heights();//11012024 load_heights_ref();
        }
        catch (Exception exception) when (exception is FileNotFoundException || exception is DirectoryNotFoundException || exception is NullReferenceException)
        {
            Debug.LogWarning("manage_heights: keine Referenz-Hoehenkarte, verwende flache Hoehen. " + exception.Message);
            (heights_ref, heights_ref_uncut) = trivial_heights();
        }
        List<List<float>> diff_im = find_diff(heights_uncut, heights_ref_uncut, mode: heights_mode);
        //27112024 List<List<float>> diff_im = find_diff(heights, heights_ref, mode: heights_mode);

        float nice_floor = 0f;//031222024 -240f;
        (List<List<float>> heights_chosen, float scale_factor) = choose_heights(
            add_to_mat(heights_uncut, nice_floor),
            add_to_mat(heights_ref_uncut, nice_floor), diff_im);

        manage_heights_loss(heights_uncut, heights_ref_uncut, scale_factor);

        // info (paul): make the actual plane object
        clean_platine_plane();
        //13012024 write_mat_for_debug(heights);

        if (false)//force_flat
        {
            heights = force_heights(heights);
        }

        return (heights, heights_chosen, scale_factor);
    }

    /// <summary>
    /// Zero depth maps (flat display).
    /// </summary>
    /// <returns>Tuple of two zero maps.</returns>
    public (List<List<float>>, List<List<float>>) trivial_heights()
    {
        int render_res = get_render_res();
        List<List<float>> mat_0 = zeros_of_size(render_res, render_res);
        List<List<float>> mat_1 = zeros_of_size(render_res, render_res);
        return (mat_0, mat_1);
    }
    /// <summary>
    /// Reads the TV depth map and its reference of the current experiment (legacy height display).
    /// </summary>
    /// <returns>Tuple (depth, reference depth).</returns>
    public (List<List<float>>, List<List<float>>) read_heights_tv()
    {
        List<List<float>> mat_u_pre_pre = load_heights_raw();
        List<List<float>> mat_u_pre = switch_mat(mat_u_pre_pre);

        // info (paul): find min and max val
        (float min_val, float max_val) = find_min_max_for_heights();
        List<List<float>> mat_u = unnorm_mat(mat_u_pre, min_val, max_val);
        // List<List<float>> mat_u = mat_u_pre;

        // info (paul): reconstruct the distance map in 3d space from disparities
        List<List<float>> mat = mat_raw2dists(mat_u);
        //18032025 List<List<float>> mat = mat_u;

        // info (paul): cut off floor
        mat = transpose_mat(mat);//24062024
        mat = mirror_mat(mat, idx: "i");//24062024

        //03122024 mat = filter_mean_comp(mat);
        //mat_cut = filter_mean_comp(mat_cut);
        //mat_cut = filter_mean_comp(mat_cut);
        //mat_cut = filter_mean_comp(mat_cut);
        //mat_cut = filter_mean_comp(mat_cut);

        List<List<float>> mat_cut = cut_off(mat);

        return (mat_cut, mat);
    }
    /// <summary>
    /// Loads the raw stereo map of the current experiment (legacy).
    /// </summary>
    /// <returns>Raw map.</returns>
    public List<List<float>> load_heights_raw()
    {
        int t_idx_0 = 2;
        string current_exp = remove_dots(get_experiment());

        string blade_idx_str = null;
        try
        {
            blade_idx_str = blade_idxs[t_idx_0].ToString();
        }
        catch
        {
            blade_idx_str = blade_idxs[t_idx_0].ToString();
        }

        // info (paul): this is actually not a png file: 
        string path_heights = path_dic +
                current_exp + "/heights/heights_" + blade_idx_str + "_r" +
                 get_render_res().ToString() + ".png";
        //20092026 klare Meldung statt NullReferenceException, wenn fuer diese
        //Aufloesung/dieses Experiment noch keine Analyse (with_exp + with_tv -> Start) lief.
        if (!File.Exists(path_heights))
            throw new FileNotFoundException("Keine TV-Ergebnisse fuer '" + current_exp + "' bei r"
                + get_render_res() + " vorhanden (" + path_heights + "). Bitte zuerst eine Analyse mit "
                + "with_exp + with_tv und dieser Aufloesung per 'Start' durchlaufen lassen.");
        float[][] mat_u_pre_pre_pre = load_floats2(full_path: path_heights);
        List<List<float>> mat_u_pre_pre = floats2_to_lists(mat_u_pre_pre_pre);
        return mat_u_pre_pre;
    }
    /// <summary>
    /// Reads the stored value range of the stereo map.
    /// </summary>
    /// <returns>Tuple (minimum, maximum).</returns>
    public (float, float) find_min_max_for_heights()
    {
        string min_max_file = path_dic + remove_dots(get_experiment()) +
                "/min_max_u_" + blade_idxs[blade_idxs.Count - 1].ToString() + "_heights.txt";
        string min_max_str = load_txt_line(min_max_file);
        string[] strs = min_max_str.Split(" ");
        float min_val = float.Parse(strs[0]);
        float max_val = float.Parse(strs[1]);
        return (min_val, max_val);
    }

    /// <summary>
    /// Loads an image file as matrix of gray values.
    /// </summary>
    /// <param name="path">Image file.</param>
    /// <param name="with_switch_dims">True to transpose.</param>
    /// <returns>Matrix m[i][j].</returns>
    public List<List<float>> load_tex_to_mat(string path, bool with_switch_dims = false)
    {
        // info (paul): load a tex and convvert it into mat

        string file_path_u = path;//"C:/Users/go73jem/Desktop/DIC_package/exp_normal/time_flow_v/debug_im_cv.png";
        byte[] im_bytes_u = System.IO.File.ReadAllBytes(file_path_u);

        // info (paul): assuming, that the resolution of the first image is 
        //      the resolution of all the images
        if (true)//20062024 (t_idx == 0)
        {
            (res_x, res_y) = bytes2res(im_bytes_u);
        }

        Texture2D tex_albedo_u = new Texture2D(res_x, res_y);
        tex_albedo_u.LoadImage(im_bytes_u);
        List<List<float>> mat_u = tex2mat(tex_albedo_u, with_switch_dims: with_switch_dims);
        return mat_u;
    }

    /// <summary>
    /// Displays depth, reference, or depth error on the sample according to the height mode.
    /// </summary>
    /// <param name="heights">Measured depth.</param>
    /// <param name="heights_ref">Reference depth.</param>
    /// <param name="scale_factor">Display scale.</param>
    public void manage_heights_loss(List<List<float>> heights, List<List<float>> heights_ref, float scale_factor)
    {
        // info (paul): new parts for heightsList<List<float>>
        if (get_paint_with() == "heights")
        {
            (List<List<float>> stream_heights, float coverage_l) = choose_heights_or_loss(
                heights, heights_ref, scale_factor, threshold: 0.1f);
            //A (List<List<float>> stream_u, List<List<float>> stream_v, float coverage) = manage_flow_or_loss(heights_chosen, heights_chosen, t_idx);

            // info (paul): scale label
            (float stream_u_min, float stream_u_max) = find_min_max(stream_heights, with_padding: true, lower_floor: 200f);//28112024A lower_floor: 200f);//11112024 
            (float stream_v_min, float stream_v_max) = find_min_max(stream_heights, with_padding: true, lower_floor: 200f);//28112024A lower_floor: 200f);//11112024 
            (float stream_u_mean, float dev_u) = find_mean_in_all(stream_heights, span: 20, coverage: coverage_l);//find_mean_in_span(stream_u, span: 20, j_off: 50);//find_mean_in_all(stream_u, span: 20);//find_mean_in_span(stream_u, span: 20, j_off: 50);//find_mean_in_span(stream_u, span: 20);
            (float stream_v_mean, float dev_v) = find_mean_in_all(stream_heights, span: 20, coverage: coverage_l);//find_mean_in_span(stream_v, span: 20, j_off: 50);//find_mean_in_all(stream_v, span: 20);//find_mean_in_span(stream_v, span: 20, j_off: 50);//find_mean_in_span(stream_v, span: 20);
            //11112024 update_scale_label(stream_u_mean, dev_u, stream_v_mean, dev_v);
            update_scale_label_ext(stream_u_mean, dev_u, stream_u_min, stream_u_max, stream_v_mean,
                dev_v, stream_v_min, stream_v_max);
        }
    }
    /// <summary>
    /// Clips a depth map below its meaningful minimum (removes background).
    /// </summary>
    /// <param name="heights_chosen_input">Depth map.</param>
    /// <returns>Clipped copy.</returns>
    public List<List<float>> cut_off(List<List<float>> heights_chosen_input)
    {
        List<List<float>> heights_chosen = copy_mat(heights_chosen_input);

        (float min_val, float max_val) = find_min_max(heights_chosen, with_padding: true);
        float floor = min_val + 1f;//14072024 max_val - 1000f;//10072024 30f;

        //24062024 // info (paul): cut off values below floor
        //24062024 heights_chosen = cut_off_below_floor(heights_chosen, floor);

        // info (paul): finding the "meaningful" min
        float min_meaningful = find_min_meaningful(heights_chosen, floor);

        // info (paul): cut off values below floor
        heights_chosen = cut_off_below_floor(heights_chosen, floor: min_meaningful);

        return heights_chosen;
    }

    /// <summary>
    /// Smallest depth value above a floor.
    /// </summary>
    /// <param name="heights_chosen">Depth map.</param>
    /// <param name="floor">Floor value.</param>
    /// <returns>Minimum above the floor.</returns>
    public float find_min_meaningful(List<List<float>> heights_chosen, float floor)
    {
        float min_val_meaningful = 9999f;
        for (int i = 0; i < heights_chosen.Count; i++)
        {
            for (int j = 0; j < heights_chosen[0].Count; j++)
            {
                float height_ij = heights_chosen[i][j];
                if (floor < height_ij)//floor+1 < height_ij
                {
                    min_val_meaningful = Mathf.Min(height_ij, min_val_meaningful);
                }
                //24062024 heights_chosen[i][j] = height_ij;
            }
        }
        return min_val_meaningful;
    }

    /// <summary>
    /// Sets depth values below a floor to the floor.
    /// </summary>
    /// <param name="heights_chosen">Depth map (modified).</param>
    /// <param name="floor">Floor value.</param>
    /// <returns>The modified map.</returns>
    public List<List<float>> cut_off_below_floor(List<List<float>> heights_chosen, float floor)
    {
        // info (paul): cut off below floor
        for (int i = 0; i < heights_chosen.Count; i++)
        {
            for (int j = 0; j < heights_chosen[0].Count; j++)
            {
                float height_ij = heights_chosen[i][j];
                if (height_ij < floor)
                {
                    height_ij = floor;
                }
                heights_chosen[i][j] = height_ij;
                heights_chosen[i][j] -= floor;
            }
        }

        // info (paul): subtract floor
        /*for (int i = 0; i < heights_chosen.Count; i++)
        {
            for (int j = 0; j < heights_chosen[0].Count; j++)
            {
                float height_ij = heights_chosen[i][j];
                if (height_ij < floor)
                {
                    height_ij = floor;
                }
                heights_chosen[i][j] = height_ij;

                //heights_chosen[i][j] -= floor;
            }
        }*/

        return heights_chosen;
    }

    /// <summary>
    /// Selects the depth map to display according to the height mode (value, reference, or error).
    /// </summary>
    /// <param name="heights">Measured depth.</param>
    /// <param name="heights_ref">Reference depth.</param>
    /// <param name="diff_im">Difference map.</param>
    /// <returns>Tuple (selected map, coverage).</returns>
    public (List<List<float>>, float) choose_heights(List<List<float>> heights, List<List<float>> heights_ref, List<List<float>> diff_im)
    {
        // info (paul): Choose heights based on the mode, what should be plotted as heightmap


        List<List<float>> chosen = null;
        float scale_factor = 1f;

        string heights_mode_l = this.get_heights_mode();
        if (heights_mode_l == "value")
        {
            chosen = heights;
            scale_factor = 1f;//08102024 3f;
        }
        if (heights_mode_l == "value_ref")
        {
            chosen = heights_ref;
            scale_factor = 1f;
        }
        if (heights_mode_l == "loss_abs")
        {
            chosen = diff_im;//TODO: rel/abs
            scale_factor = 1f;//08102024 3f;
        }
        if (heights_mode_l == "loss_rel")
        {
            chosen = diff_im; // TODO: rel/abs
            scale_factor = 1f;//08102024 3f;
        }

        return (chosen, scale_factor);
    }
    /// <summary>
    /// Returns the height display mode.
    /// </summary>
    /// <returns>value, value_ref, loss_abs, or loss_rel.</returns>
    public string get_heights_mode()
    {
        return this.heights_mode;
    }

    /// <summary>
    /// Sets the height display mode and switches the display to depth.
    /// </summary>
    /// <param name="input">value, value_ref, loss_abs, or loss_rel.</param>
    /// <param name="is_internal">True for internal changes without switching the display.</param>
    public void set_heights_mode(string input, bool is_internal = false)
    {
        this.heights_mode = input;
        if (!is_internal)
        {
            set_paint_with("heights");
            //set_plot_mode("", is_internal: true);
        }
    }

    /// <summary>
    /// Difference between measured and reference depth.
    /// </summary>
    /// <param name="heights">Measured depth.</param>
    /// <param name="heights_ref">Reference depth.</param>
    /// <param name="mode">absolute or relative.</param>
    /// <returns>Difference map.</returns>
    public List<List<float>> find_diff(List<List<float>> heights, List<List<float>> heights_ref, string mode = "absolute")
    {
        // info (paul): mode: "absolute": the normal difference is used
        //                    "relative": difference/value (the relative difference) is used
        //              heights_ref has not the same resolution as heights, due to the NCorr scale down,
        //              be aware of that.

        int length = heights.Count;
        int height = heights[0].Count;

        List<List<float>> diffs = zeros_of_size(length, height);
        (float min_height, float max_height) = find_min_max(heights, with_padding: true);

        for (int i = 0; i < length; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if ((i < heights.Count) && (j < heights[0].Count))
                {
                    int scale_fac = 1;//08102024 3;
                    float heights_ij = heights[i][j];
                    float heights_ref_ij = float.NaN;
                    try
                    {
                        heights_ref_ij = heights_ref[scale_fac * i][scale_fac * j];
                    }
                    catch
                    {
                        heights_ref_ij = heights_ref[scale_fac * i][scale_fac * j];
                    }
                    float diff = Mathf.Abs(heights_ij - heights_ref_ij);

                    // info (paul): if the heights_ij is close to floor/ heights_min, 
                    //      it is apparently out of the roi, i.e. we se the diff just to zero, 
                    //      because it would be meaningless to calculate it.
                    if (heights_ij < min_height + 1f)
                    {
                        diff = 0f;
                    }

                    // info (paul): relative difference with somehow the maximum of the
                    //      two value as "value". Perhaps there is a better solution for "value", who knows. 
                    float value = Mathf.Max(heights_ij, heights_ref_ij);
                    float frac = diff / value;

                    if (frac != 0f && !float.IsNaN(frac) && !float.IsInfinity(frac))
                    {
                        ;
                    }
                    if (diff > value)
                    {
                        frac = 0f; // info (paul): if diff bigger than value, 
                        //      than probably value is de facto 0, therefore
                        //      we ignore this.
                    }

                    if (mode == "loss_rel") //"relative"
                    {
                        diffs[i][j] = frac;//diff;//frac;
                    }
                    if (mode == "loss_abs") // "absolute"
                    {
                        diffs[i][j] = diff; //value;//diff;//frac;
                    }
                }
            }
        }

        return diffs;
    }

    /// <summary>
    /// Removes the display plane of the maps.
    /// </summary>
    public void clean_platine_plane()
    {
        GameObject plane = GameObject.Find("platine_plane");
        if (plane != null)
        {
            remove_obj(plane);
        }
    }
    /// <summary>
    /// Adds a constant to all entries of a matrix.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <param name="scale">Constant to add.</param>
    /// <returns>New matrix.</returns>
    public List<List<float>> add_to_mat(List<List<float>> mat, float scale)
    {
        List<List<float>> new_mat = copy_mat(mat);

        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < mat.Count; j++)
            {
                new_mat[i][j] = mat[i][j] + scale;
            }
        }
        return new_mat;
    }
    /// <summary>
    /// Multiplies all entries of a matrix by a constant.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <param name="scale">Factor.</param>
    /// <returns>New matrix.</returns>
    public List<List<float>> multiply_with_scalar(List<List<float>> mat,
        float scale)
    {
        List<List<float>> new_mat = copy_mat(mat);

        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < mat.Count; j++)
            {
                new_mat[i][j] = mat[i][j] * scale;
            }
        }
        return new_mat;
    }
    /// <summary>
    /// Destroys a game object.
    /// </summary>
    /// <param name="obj">Object to destroy.</param>
    public void remove_obj(GameObject obj)
    {
        // info (paul): destroy platine plane
        Destroy(obj);
    }

    /// <summary>
    /// Connects the display plane with the interactive cameras (if present).
    /// </summary>
    /// <param name="platine_plane">Display plane.</param>
    public void assign_to_cam(GameObject platine_plane)
    {
        //20092026 "exp_cam" traegt in der Szene kein Cam_manager-Skript (nur MainCamera)
        //-> NullReferenceException beim value/loss-Button. Beide Zuweisungen optional.
        GameObject cam_obj = GameObject.Find("MainCamera");
        GameObject cam_exp_obj = GameObject.Find("exp_cam");
        cam_script = cam_obj != null ? cam_obj.GetComponent<Cam_manager>() : null;
        cam_exp = cam_exp_obj != null ? cam_exp_obj.GetComponent<Cam_manager>() : null;

        // info (paul): assign the object to the camera:
        if (cam_script != null)
            cam_script.platine_plane = platine_plane.transform;
        if (cam_exp != null)
            cam_exp.platine_plane = platine_plane.transform;
    }

    /// <summary>
    /// Appends text to the log field of the scene.
    /// </summary>
    /// <param name="text">Text to append.</param>
    public void log(string text)
    {
        log_field.GetComponent<TextMeshProUGUI>().text += text;
    }

    // Update is called once per frame

    // info (paul): for the speckle rendering
    int speckle_idx = 0;
    private int reg_idx = 0;

    /// <summary>
    /// Unity callback: executes jobs queued by the flow thread (GPU calls), updates the progress display, and advances the rendering and analysis state machine of the standard analysis and the sweeps.
    /// </summary>
    void Update()
    {
        //23092026 Auftraege aus dem TV-Hintergrundthread (GPU-Aufrufe) im Hauptthread ausfuehren.
        //  Steht ganz oben, damit spaetere return-Pfade in Update() die GPU-Rechnung nicht blockieren.
        while (main_thread_jobs.TryDequeue(out Action job))
        {
            job();
        }

        if (get_is_started())
        {
            ;
        }
        if (get_series())
        {
            ;
        }

        if (get_is_started() && !get_series())
        {
            //update_render_acts();

            //22092026 Felder wie speckle_acts/exp_cv_acts (Listen mit Delegates) sind nicht
            //Unity-serialisierbar: laedt der Editor die Assemblies waehrend des Play-Modus neu
            //(Skriptaenderung/Refresh), werden sie null, Start() laeuft aber NICHT erneut.
            //Bisher warf Update() dann in jedem Frame eine NullReferenceException, wodurch auch
            //der Rest von Update (Fortschritt, Galerie, naechster Analyseschritt) nie lief.
            if (speckle_acts == null)
                speckle_acts = new List<(Action, float, float)>();
            if (exp_cv_acts == null)
            {
                is_started = false;
                string msg = "Die Analyse wurde durch ein Neuladen der Skripte (Kompilierung im "
                    + "laufenden Play-Modus) unterbrochen und kann nicht fortgesetzt werden. "
                    + "Bitte Play neu starten und die Analyse erneut ausfuehren.";
                Debug.LogWarning(msg);
                ExperimentImageGallery.SetResultsText(msg);
                ExperimentImageGallery.ShowResultsWindow();
                return;
            }

            if (speckle_idx < speckle_acts.Count)
            {
                this.speckle_size = speckle_acts[speckle_idx].Item2;
                this.speckle_dist = speckle_acts[speckle_idx].Item3;
                speckle_acts[speckle_idx].Item1.Invoke();
                speckle_idx += 1;
            }

            if (!cv_action_running && get_reg_idx() < exp_cv_acts.Count)
            {
                add_to_reg_idx(1);
                set_series_progress_info(get_reg_idx(), exp_cv_acts.Count);

                // info (paul): execute act from actioner
                exp_cv_acts[get_reg_idx() - 1].act.Invoke();

            }
        }
        else if (get_is_started() && get_series())
        {
            List<string> im_paths = this.get_im_paths();
            //cv_for_paths(path0: im_paths[0], path1: im_paths[1], -1, d_cam: 0, with_dt: true, 
            //    pars: pars);
            string in_dir = this.get_in_dir();
            string out_dir = this.get_out_dir();

            (series_idx, is_started) = cv_series(im_paths, in_dir: in_dir,
                out_dir: out_dir, series_idx: series_idx, is_started: get_is_started());
        }

        GameObject platine_plane = GameObject.Find("platine_plane");
        if (platine_plane != null)
        {
            platine_plane.GetComponent<MeshRenderer>().material.color = Color.white;
        }

        render_progress_ui_main_thread();

    }

    /// <summary>
    /// Returns the index of the current rendering step.
    /// </summary>
    /// <returns>Step index.</returns>
    public int get_reg_idx()
    {
        return reg_idx;
    }

    /// <summary>
    /// Sets the index of the current rendering step.
    /// </summary>
    /// <param name="input">Step index.</param>
    public void set_reg_idx(int input)
    {
        reg_idx = input;
    }

    /// <summary>
    /// Advances the index of the current rendering step.
    /// </summary>
    /// <param name="value">Increment.</param>
    public void add_to_reg_idx(int value)
    {
        int reg_idx = get_reg_idx();
        set_reg_idx(reg_idx + value);
    }

    /// <summary>
    /// Legacy placeholder for updating the sample images.
    /// </summary>
    public void update_blade_pics_if()
    {
        //23092024 bool blades_created = get_blades_created();
        //23092024 if (blades_created)
        //23092024 {
        //23092024     update_blade_pics();
        //23092024 }
    }

    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 0.035.
    /// </summary>
    public void start_speckle_0_035()
    {
        start_speckle(diameter: 0.035f);
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 0.07.
    /// </summary>
    public void start_speckle_0_07()
    {
        start_speckle(diameter: 0.07f);
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 0.35.
    /// </summary>
    public void start_speckle_0_35()
    {
        start_speckle(diameter: 0.35f);
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 0.175.
    /// </summary>
    public void start_speckle_0_175()
    {
        start_speckle(diameter: 0.175f);
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 0.7.
    /// </summary>
    public void start_speckle_0_7()
    {
        start_speckle(diameter: 0.7f);
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 1.4.
    /// </summary>
    public void start_speckle_1_4()
    {
        start_speckle(diameter: 1.4f);
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 2.1.
    /// </summary>
    public void start_speckle_2_1()
    {
        start_speckle(diameter: 2.1f);
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 2.8.
    /// </summary>
    public void start_speckle_2_8()
    {
        start_speckle(diameter: 2.8f);
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 7.0.
    /// </summary>
    public void start_speckle_7_0()
    {
        start_speckle(diameter: 7.0f);
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 14.0.
    /// </summary>
    public void start_speckle_14_0()
    {
        start_speckle(diameter: 14.0f);
    }

    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 0.5.
    /// </summary>
    public void start_speckle_05()
    {
        start_speckle(diameter: 0.5f);
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 1.
    /// </summary>
    public void start_speckle_1()
    {
        start_speckle(diameter: 1f);
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 2.
    /// </summary>
    public void start_speckle_2()
    {
        start_speckle(diameter: 2f);
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment with material size 4.
    /// </summary>
    public void start_speckle_4()
    {
        start_speckle(diameter: 4f);
    }
    /// <summary>
    /// Starts a single speckle experiment: sets the speckle material (and optionally the field of view) and renders and analyses the images.
    /// </summary>
    /// <param name="diameter">Material size (-1 = keep).</param>
    /// <param name="fov">Field of view in degrees (-1 = keep).</param>
    /// <param name="label">Experiment label (empty = derived from the size).</param>
    public void start_speckle(float diameter = -1f, float fov = -1f, string label = "")
    {
        // info (paul): do a speckle experiment analysis, since lighting is finished;
        //          the synthetic images will be created
        if (fov >= 0f)
        {
            set_field_of_view(fov);
            this.cam_for_uv_0.fieldOfView = fov;
            this.cam_for_uv_1.fieldOfView = fov;
        }

        // info (paul): set params
        ready_for_next_act = true;
        done_render_acts = true;
        set_ready_for_next_blade(true);
        set_blade_idx(-1);
        set_pic_timer(0f);
        set_experiment("speckle_" + diameter.ToString("0.000") + label);//
        bool below_max = get_with_exp() && (get_blade_idx() + 1) < (blade_idxs.Count - 1);//21102024B (blade_idx_max - blade_idx_min);
        string blade_path_first = blade_path_for_idx(blade_idxs[0]);//24102024A (blade_idx_min);

        // info (paul): set up lighting
        set_up_lighting(y_coord: 30f);

        // info (paul): set speckle file path
        string speckle_file = "speckle_" + diameter.ToString("0.000");//(diameter).ToString();
        set_speckle_file(speckle_file);
        //04092024 set_speckle_file("checkerboard");

        // info (paul): 
        List<GameObject> blades = collect_blades();
        for (int i = 0; i < blades.Count; i++)
        {
            apply_speckles(blades[i]);
        }
        start_renders(blade_path: blade_path_first, blade_idx: blade_idxs[0], with_uv_init: true);
        set_below_max(below_max);
        if (fov >= 0f)
        {
            set_field_of_view(20f);
        }
    }
    /// <summary>
    /// Creates the two cameras (and their symbols) from an experiment configuration.
    /// </summary>
    /// <param name="config">Experiment configuration.</param>
    /// <param name="cam_angle">Tilt angle of the cameras in degrees.</param>
    public void set_up_cams(ExpConfig config, float cam_angle = 30f)
    {
        // info (paul): clean up old cams
        GameObject cam_parent = GameObject.Find("cams_0_1_parent");
        remove_children(cam_parent);

        // info (paul): set up cameras
        Vector3 blades_pos = get_blades_pos();
        float pos_0_real_x = blades_pos.x + config.get_cam_poss()[0][0];
        float pos_0_real_y = blades_pos.y + config.get_cam_poss()[0][1];
        float pos_0_real_z = blades_pos.z + config.get_cam_poss()[0][2];
        float pos_1_real_x = blades_pos.x + config.get_cam_poss()[1][0];
        float pos_1_real_y = blades_pos.y + config.get_cam_poss()[1][1];
        float pos_1_real_z = blades_pos.z + config.get_cam_poss()[1][2];

        Vector3 pos_0 = new Vector3(pos_0_real_x, pos_0_real_y, pos_0_real_z);
        Vector3 pos_1 = new Vector3(pos_1_real_x, pos_1_real_y, pos_1_real_z);

        clean_symbols();
        this.cam_0 = set_up_cam("cam_0", "cam_prefab_0_" + get_render_res().ToString(), pos_0,
            angle: config.get_cam_angle() + 0f, config: config);//-10f
        this.cam_1 = set_up_cam("cam_1", "cam_prefab_1_" + get_render_res().ToString(), pos_1,
            angle: -config.get_cam_angle() + 0f, config: config);//-10f

        // info (paul): in design_exp_panel init im_1_panel, im_2_panel:
        if (false)
        {
            Transform designer = canvas.transform.Find("design_exp_panel");
            Transform im_1_panel = designer.Find("im_1_panel");
            Transform im_2_panel = designer.Find("im_2_panel");

            RenderTexture cam_0_tex = this.cam_0.GetComponent<Camera>().targetTexture;
            //Sprite sprite = Sprite.Create(cam_0_tex, new Rect(0.0f, 0.0f, cam_0_tex.width, cam_0_tex.height),
            //    new Vector2(0.5f, 0.5f), 100.0f);
            Material mat1 = (Material)Resources.Load("Targets/fbx_files/cam_0_mat");
            mat1.mainTexture = cam_0_tex;
            im_1_panel.GetComponent<UnityEngine.UI.Image>().material = mat1;
        }
    }
    /// <summary>
    /// Removes the camera symbols from the scene.
    /// </summary>
    public void clean_symbols()
    {
        GameObject symbols = GameObject.Find("cams_symbols");
        remove_children(symbols);
    }

    //20092026 true, wenn config_now nie ueber "Confirm" befuellt wurde (fov NaN/<=0 oder
    //alle Kamera-Offsets 0). Dann darf set_up_cams(config) nicht verwendet werden.
    /// <summary>
    /// Checks whether an experiment configuration was never filled via Confirm (invalid field of view or all camera offsets zero).
    /// </summary>
    /// <param name="config">Configuration to check.</param>
    /// <returns>True if it must not be used to set up the cameras.</returns>
    public bool config_is_unconfirmed(ExpConfig config)
    {
        if (config == null)
            return true;
        float fov = config.get_fov();
        if (float.IsNaN(fov) || fov <= 0f)
            return true;
        foreach (float[] cam_pos in config.get_cam_poss())
            foreach (float coord in cam_pos)
                if (coord != 0f)
                    return false;
        return true;
    }

    /// <summary>
    /// Starts an experiment from the current configuration of the experiment-design panel (cameras, lights, sample, speckles).
    /// </summary>
    public void start_exp_from_config_now()
    {
        ExpConfig config = this.get_config_now();

        // info (paul): start an experimental analysis from a configuration 
        //      file, which contains all the relevant information on what 
        //      experimental conditions should be met.

        if (config.fov >= 0f)
        {
            set_field_of_view(config.fov);
            this.cam_for_uv_0.fieldOfView = config.fov;
            this.cam_for_uv_1.fieldOfView = config.fov;
        }

        // info (paul): set params
        ready_for_next_act = true;
        done_render_acts = true;
        set_ready_for_next_blade(true);
        set_blade_idx(-1);
        set_pic_timer(0f);
        //20092026 Bugfix: ohne bestaetigte Config hiess das Experiment hier
        //"speckle_0,070NO_LABEL" (Kultur-abhaengiges Dezimalkomma!), die Renderbilder
        //landeten also in Assetsspeckle_0,070NO_LABEL/cam_0/uv/. Der TV-Schritt
        //(manage_cv_async) arbeitet aber unter dem Label der Aktion ("exp_normal") und
        //fand die Bilder nicht (FileNotFoundException im_3_r128.png). Unbestaetigte
        //Config -> "exp_normal", sonst wie bisher (jetzt kulturunabhaengig formatiert).
        if (config_is_unconfirmed(config))
            set_experiment("exp_normal");
        else
            set_experiment("speckle_" + config.diameter.ToString("0.000", CultureInfo.InvariantCulture) + config.label);
        bool below_max = get_with_exp() && (get_blade_idx() + 1) < (blade_idxs.Count - 1);//21102024B (blade_idx_max - blade_idx_min);
        string blade_path_manual = config.get_blade_path();
        string blade_path_first = null;
        if (blade_path_manual != null)
        {
            blade_path_first = blade_path_manual;
        }
        if (blade_path_manual == null)
        {
            blade_path_first = blade_path_for_idx(blade_idxs[0]);//24102024A (blade_idx_min);
        }

        // info (paul): set up lighting
        //20092026 Bugfix: "Start" laeuft seit 02072025 ueber diesen Config-Pfad (siehe
        //init_render_acts). Ohne vorheriges "Confirm" ist config_now ein leeres
        //ExpConfig() mit ambient_intensity = NaN, fov = NaN und Kamera-Offsets (0,0,0).
        //Das ergab ein Licht mit NaN-Intensitaet und beide Kameras mit fieldOfView = NaN
        //direkt auf blades_pos -> singulaere View-Projection-Matrix -> HDRP-Assertion
        //"std::abs(det) > FLT_MIN". Fehlende Werte fallen jetzt auf das klassische
        //Setup (load_cam_light/refresh_cams: default_dist, cam_angle 10, field_of_view)
        //zurueck, damit "Start" dieselben Kameras nutzt wie "Realbild: Neu".
        float ambient_intensity = config.get_ambient_intensity();
        if (float.IsNaN(ambient_intensity))
            ambient_intensity = 1f;
        set_up_lighting(y_coord: 30f, intensity: ambient_intensity);

        // info (paul): set up cams (in the old system it was done in Start()
        if (config_is_unconfirmed(config))
        {
            ExperimentImageGallery.SetResultsText("Analyse gestartet - Render-Look: "
                + (nakajima_look ? "Nakajima (Realbild)" : "klassisch") + ", r" + get_render_res());
            Debug.Log("start_exp_from_config_now: config_now wurde nicht bestaetigt (fov="
                + config.get_fov().ToString(CultureInfo.InvariantCulture)
                + ", Kamera-Offsets 0) -> klassisches Kamera-Setup via refresh_cams().");
            clean_symbols();
            refresh_cams();
        }
        else
        {
            set_up_cams(config);
        }

        // info (paul): set speckle file path
        string speckle_file = "speckle_" + config.diameter.ToString("0.000", CultureInfo.InvariantCulture);//(diameter).ToString();
        set_speckle_file(speckle_file);
        //04092024 set_speckle_file("checkerboard");

        // info (paul): this apply_speckles is irrelevant for manual; in manual, they call apply_speckles from somewhere else
        List<GameObject> blades = collect_blades();
        for (int i = 0; i < blades.Count; i++)
        {
            apply_speckles(blades[i]);
        }
        GameObject surface_obj = start_renders(blade_path: blade_path_first, blade_idx: blade_idxs[0],
            with_uv_init: true);
        set_below_max(below_max);
        if (config.fov >= 0f)
        {
            set_field_of_view(20f);
        }
    }


    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.001.
    /// </summary>
    public void start_lighting_0001()
    {
        start_lighting(intensity: 0.001f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.01.
    /// </summary>
    public void start_lighting_001()
    {
        start_lighting(intensity: 0.01f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.02.
    /// </summary>
    public void start_lighting_002()
    {
        start_lighting(intensity: 0.02f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.03.
    /// </summary>
    public void start_lighting_003()
    {
        start_lighting(intensity: 0.03f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.032.
    /// </summary>
    public void start_lighting_0032()
    {
        start_lighting(intensity: 0.032f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.034.
    /// </summary>
    public void start_lighting_0034()
    {
        start_lighting(intensity: 0.034f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.0345.
    /// </summary>
    public void start_lighting_00345()
    {
        start_lighting(intensity: 0.0345f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.035.
    /// </summary>
    public void start_lighting_0035()
    {
        start_lighting(intensity: 0.035f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.0355.
    /// </summary>
    public void start_lighting_00355()
    {
        start_lighting(intensity: 0.0355f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.036.
    /// </summary>
    public void start_lighting_0036()
    {
        start_lighting(intensity: 0.036f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.038.
    /// </summary>
    public void start_lighting_0038()
    {
        start_lighting(intensity: 0.038f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.04.
    /// </summary>
    public void start_lighting_004()
    {
        start_lighting(intensity: 0.04f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.05.
    /// </summary>
    public void start_lighting_005()
    {
        start_lighting(intensity: 0.05f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.1.
    /// </summary>
    public void start_lighting_01()
    {
        start_lighting(intensity: 0.1f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.2.
    /// </summary>
    public void start_lighting_02()
    {
        start_lighting(intensity: 0.2f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.5.
    /// </summary>
    public void start_lighting_05()
    {
        start_lighting(intensity: 0.5f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.7.
    /// </summary>
    public void start_lighting_07()
    {
        start_lighting(intensity: 0.7f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 0.8.
    /// </summary>
    public void start_lighting_08()
    {
        start_lighting(intensity: 0.8f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 1.
    /// </summary>
    public void start_lighting_1()
    {
        start_lighting(intensity: 1f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 2.
    /// </summary>
    public void start_lighting_2()
    {
        start_lighting(intensity: 2f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 3.
    /// </summary>
    public void start_lighting_3()
    {
        start_lighting(intensity: 3f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 4.
    /// </summary>
    public void start_lighting_4()
    {
        start_lighting(intensity: 4f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 5.
    /// </summary>
    public void start_lighting_5()
    {
        start_lighting(intensity: 5f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 6.
    /// </summary>
    public void start_lighting_6()
    {
        start_lighting(intensity: 6f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 10.
    /// </summary>
    public void start_lighting_10()
    {
        start_lighting(intensity: 10f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 100.
    /// </summary>
    public void start_lighting_100()
    {
        start_lighting(intensity: 100f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 10000.
    /// </summary>
    public void start_lighting_10000()
    {
        start_lighting(intensity: 10000f);
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment with factor 1e8.
    /// </summary>
    public void start_lighting_100000000()
    {
        start_lighting(intensity: 100000000f);
    }

    /// <summary>
    /// Legacy shortcut: speckle experiment (size 0.035) with the field of view of the height analysis.
    /// </summary>
    public void start_speckle_0_035_heights()
    {
        start_speckle(diameter: 0.035f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment (size 0.07) with the field of view of the height analysis.
    /// </summary>
    public void start_speckle_0_07_heights()
    {
        start_speckle(diameter: 0.07f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment (size 0.175) with the field of view of the height analysis.
    /// </summary>
    public void start_speckle_0_175_heights()
    {
        start_speckle(diameter: 0.175f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment (size 0.35) with the field of view of the height analysis.
    /// </summary>
    public void start_speckle_0_35_heights()
    {
        start_speckle(diameter: 0.35f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: speckle experiment (size 0.7) with the field of view of the height analysis.
    /// </summary>
    public void start_speckle_0_7_heights()
    {
        start_speckle(diameter: 0.7f, fov: heights_fov, label: "_heights");
    }


    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 0.01) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_001_heights()
    {
        start_lighting(intensity: 0.01f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 0.02) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_002_heights()
    {
        start_lighting(intensity: 0.02f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 0.03) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_003_heights()
    {
        start_lighting(intensity: 0.03f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 0.04) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_004_heights()
    {
        start_lighting(intensity: 0.04f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 0.05) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_005_heights()
    {
        start_lighting(intensity: 0.05f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 0.1) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_01_heights()
    {
        start_lighting(intensity: 0.1f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 0.2) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_02_heights()
    {
        start_lighting(intensity: 0.2f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 0.5) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_05_heights()
    {
        start_lighting(intensity: 0.5f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 0.7) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_07_heights()
    {
        start_lighting(intensity: 0.7f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 0.8) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_08_heights()
    {
        start_lighting(intensity: 0.8f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 1) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_1_heights()
    {
        start_lighting(intensity: 1f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 2) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_2_heights()
    {
        start_lighting(intensity: 2f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 3) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_3_heights()
    {
        start_lighting(intensity: 3f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 4) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_4_heights()
    {
        start_lighting(intensity: 4f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 5) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_5_heights()
    {
        start_lighting(intensity: 5f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 6) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_6_heights()
    {
        start_lighting(intensity: 6f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 10) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_10_heights()
    {
        start_lighting(intensity: 10f, fov: heights_fov, label: "_heights");
    }
    /// <summary>
    /// Legacy shortcut: illumination experiment (factor 100) with the field of view of the height analysis.
    /// </summary>
    public void start_lighting_100_heights()
    {
        start_lighting(intensity: 100f, fov: heights_fov, label: "_heights");
    }


    /// <summary>
    /// Removes dots from a label (used for folder names).
    /// </summary>
    /// <param name="val">Label.</param>
    /// <returns>Label without dots.</returns>
    public string remove_dots(string val)
    {
        try
        {
            return val.Replace(".", "");
        }
        catch
        {
            return val.Replace(".", "");
        }
    }
    /// <summary>
    /// Starts a single illumination experiment: sets the lamp factor (and optionally the field of view) and renders and analyses the images.
    /// </summary>
    /// <param name="intensity">Illumination factor I (-1 = keep).</param>
    /// <param name="fov">Field of view in degrees (-1 = keep).</param>
    /// <param name="label">Suffix of the experiment label.</param>
    public void start_lighting(float intensity = -1f, float fov = -1f, string label = "")
    {
        // info (paul): do lighting analysis, since "normal" is finished
        if (fov >= 0f)
        {
            this.set_field_of_view(fov);
            this.cam_for_uv_0.fieldOfView = fov;
            this.cam_for_uv_1.fieldOfView = fov;
        }
        done_normal_ims = true;
        ready_for_next_act = true;
        set_ready_for_next_blade(true);
        set_blade_idx(-1);
        set_pic_timer(0f);
        set_experiment("lighting_" + remove_dots(intensity.ToString()) + label);
        //21102024B bool below_max = (get_blade_idx() + 1) < (blade_idx_max - blade_idx_min);
        bool below_max = get_with_exp() && (get_blade_idx() + 1) < blade_idxs.Count - 1;
        string blade_path_first = blade_path_for_idx(blade_idxs[0]);//24102024A blade_idx_min);
        set_up_lighting(y_coord: -30f, intensity: intensity);
        start_renders(blade_path: blade_path_first, blade_idx: blade_idxs[0], with_uv_init: true);
        set_below_max(below_max);
        if (fov >= 0f)
        {
            this.set_field_of_view(20f);
            this.cam_for_uv_0.fieldOfView = 20f;//16012025
            this.cam_for_uv_1.fieldOfView = 20f;//16012025
        }
        //return below_max;
    }

    /// <summary>
    /// Applies the parameters of the current render action (illumination, noise, speckles, field of view) before rendering.
    /// </summary>
    public void start_exp_params()
    {
        // info (paul): fetch params
        //23022025 Actioner current_act = exp_cv_acts[get_reg_idx() - 1];
        Actioner current_act = this.render_acts[render_idx];
        int cv_idx = current_act.get_cv_render_idx();
        Params pars = current_act.pars;

        // info (paul): standard procedure
        done_normal_exps = true;
        set_ready_for_next_blade(true);
        set_blade_idx(-1);
        set_pic_timer(0f);
        set_experiment(current_act.get_label());
        noise_sample_index = 0;
        string blade_path_first = blade_path_for_idx(blade_idxs[0]);//24102024A (blade_idx_min);

        // info (paul): set up lighting
        set_up_lighting(y_coord: pars.get_lighting_pos_y(), intensity: pars.get_lighting_intensity());

        // info (paul): set up speckle texture
        string speckle_file = "speckle_" + pars.get_speckle_size().ToString("0.000", CultureInfo.InvariantCulture);//(diameter).ToString();
        //29092026 negative Groesse = prozedurale Textur (Durchmesser -size in Texturpixeln, make_procedural_speckles.py);
        //  Traeger-Material speckle_0.175 (Standard-Lit), dessen Textur apply_speckles ersetzt
        procedural_speckle_s = pars.get_speckle_size() < 0f ? -pars.get_speckle_size() : float.NaN;
        if (!float.IsNaN(procedural_speckle_s))
            speckle_file = "speckle_0.175";
        set_speckle_file(speckle_file);
        //04092024 set_speckle_file("checkerboard");

        List<GameObject> blades = collect_blades();
        for (int i = 0; i < blades.Count; i++)
        {
            apply_speckles(blades[i]);
        }

        // info (paul): start renders
        start_renders(blade_path: blade_path_first, blade_idx: blade_idxs[0], with_uv_init: true);
    }

    /// <summary>
    /// Starts the rendering of the standard experiment (all frames).
    /// </summary>
    public void start_exp_normal()
    {
        done_normal_exps = true;
        set_ready_for_next_blade(true);
        set_blade_idx(-1);
        set_pic_timer(0f);
        set_experiment("exp_normal");
        string blade_path_first = blade_path_for_idx(blade_idxs[0]);//24102024A (blade_idx_min);
        start_renders(blade_path: blade_path_first, blade_idx: blade_idxs[0], with_uv_init: true);
    }

    // info (paul): I think, this function is out of date and should be removed to
    //      to get a clearer line to take_pic()
    //22022025public int update_render(bool below_max, int blade_idx_l)
    //22022025{
    //22022025    if (get_ready_for_next_blade() && below_max)
    //22022025    {
    //22022025        //set_blade_idx(blade_idx_l + 1);
    //22022025        blade_idx_l += 1;
    //22022025        set_ready_for_next_blade(false);
    //22022025        activate_blade(blade_idx: blade_idx_l);
    //22022025    }
    //22022025
    //22022025    if (!get_ready_for_next_blade())
    //22022025    {
    //22022025        add_to_pic_timer(Time.deltaTime);
    //22022025    }
    //22022025
    //22022025    if (get_pic_timer() > pic_time && below_max)
    //22022025    {
    //22022025        add_to_pic_timer(-pic_time);
    //22022025        take_pic(blade_idx_l, cam_idx: 0);
    //22022025        take_pic(blade_idx_l, cam_idx: 1);
    //22022025
    //22022025        take_ref_pic(blade_idx_l, cam_idx: 0);
    //22022025    }
    //22022025    return blade_idx_l;
    //22022025}
    bool below_max = true;
    /// <summary>
    /// Marks whether the current step is below the maximum number of steps.
    /// </summary>
    /// <param name="input">True if below the maximum.</param>
    public void set_below_max(bool input)
    {
        this.below_max = input;
    }
    /// <summary>
    /// Returns whether the current step is below the maximum number of steps.
    /// </summary>
    /// <returns>True if below the maximum.</returns>
    public bool get_below_max()
    {
        return below_max;
    }


    /// <summary>
    /// Builds the list of render actions of the standard analysis.
    /// </summary>
    /// <returns>List of actions.</returns>
    public List<Actioner> set_up_render_list()
    {
        // info (paul): make the individual render actions as a list

        List<Actioner> acts = new List<Actioner>();
        acts = add_im_steps(acts);

        return acts;
    }
    /// <summary>
    /// Adds the image-rendering steps of all experiments to the action list (if with_exp is set).
    /// </summary>
    /// <param name="acts">Action list.</param>
    /// <returns>Extended action list.</returns>
    public List<Actioner> add_im_steps(List<Actioner> acts)
    {
        if (get_with_exp())
        {
            for (int render_idx = 0; render_idx < render_acts.Count; render_idx++)
            {
                acts.Add(new Actioner(exe_render_acts, "exe_render_acts"));
                //21102024B for (int i = 0; i < blade_idx_max - blade_idx_min; i++)
                for (int i = 0; i < blade_idxs.Count; i++)
                {
                    acts.Add(new Actioner(activate_blade_act, "activate_blade_act" + render_acts[render_idx].get_label(),
                        pars: render_acts[render_idx].pars));
                    acts.Add(new Actioner(take_pic_act, "take_pic_act" + render_acts[render_idx].get_label(),
                        pars: render_acts[render_idx].pars));
                    acts.Add(new Actioner(take_pic_act, "take_pic_act" + render_acts[render_idx].get_label(),
                        pars: render_acts[render_idx].pars));
                    acts.Add(new Actioner(take_ref_pic_act, "take_ref_pic_act" + render_acts[render_idx].get_label(),
                        pars: render_acts[render_idx].pars));
                }

                //25092024 acts.Add(new Actioner(exe_render_acts, "exe_render_acts"));
            }
        }
        for (int i = 0; i < render_acts.Count; i++)
        {
            acts.Add(new Actioner(manage_cv_act, render_acts[i].get_label(),
                cv_render_idx: i, pars: render_acts[i].pars));
        }

        return acts;
    }

    /// <summary>
    /// Coroutine wrapper that starts the current flow computation.
    /// </summary>
    /// <returns>Coroutine enumerator.</returns>
    public IEnumerator cv_routine()
    {
        manage_cv_act();
        yield return null;
    }
    /// <summary>
    /// Runs the flow computation of the current action and advances to the next action afterwards.
    /// </summary>
    public async void manage_cv_act()
    {
        cv_action_running = true;
        Actioner current_act = exp_cv_acts[get_reg_idx() - 1];
        int cv_idx = current_act.get_cv_render_idx();
        Params pars = current_act.pars;
        if (analysis_sweep_running()) //29092026 Gesamtfortschritt ueber alle Stufen
            set_overall_progress(get_reg_idx(), exp_cv_acts.Count,
                lighting_sweep_running ? "Licht-Analyse" : noise_sweep_running ? "Rausch-Analyse" : "Speckle-Analyse",
                current_act.get_label());

        try
        {
            await manage_cv_async(cv_idx, pars);
            Debug.Log("cv_idx: " + cv_idx.ToString());
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            is_started = false;
            lighting_sweep_running = false;
            noise_sweep_running = false;
            speckle_sweep_running = false;
            reset_overall_progress();
        }
        finally
        {
            cv_action_running = false;
        }

        if (get_reg_idx() >= exp_cv_acts.Count && is_started)
        {
            finish_all_tv_progress();
            string finished_sweep = lighting_sweep_running ? "lighting" : noise_sweep_running ? "noise"
                : speckle_sweep_running ? "speckle" : null;
            lighting_sweep_running = false;
            noise_sweep_running = false;
            speckle_sweep_running = false;
            is_started = false;
            reset_overall_progress();
            ExperimentImageGallery.ShowWhenFinished();
            render_acts = init_render_acts(render_acts);
            if (finished_sweep != null)
                await show_sweep_plot(finished_sweep); //28092026 Diagramm wie Abb. 5/6
        }
    }

    /// <summary>
    /// Computes the optical flow of one experiment (image pairs of the selected frames) and evaluates it.
    /// </summary>
    /// <param name="cv_idx">Index of the flow action.</param>
    /// <param name="pars">Parameters of the experiment.</param>
    public async Task manage_cv_async(int cv_idx, Params pars)
    {
        // info (paul): set initial experiment
        set_experiment("exp_normal");//24112024 "exp_normal"

        // info (paul): The refresh part
        if (get_with_exp())//25092024 
        {
            manage_distortion_ground_truth();//05092024 //24092024
        }

        if (true)
        {
            // info (paul): The call DIC remotely part (22062024)
            //30082024 png2tiff();
            //06122024 for (int i = 1; i < render_acts.Count; i++)
            //06122024 {

            string exp_l = render_acts[cv_idx].get_label();
            set_experiment(exp_l);
            //cv_main(cv_idx, d_cam: 1, with_dt: false, exp_label: exp_l, pars: pars);//06032025 

            if (this.get_with_main())
            {
                //List<string> im_paths = this.get_im_paths();
                //cv_for_paths(path0: im_paths[0], path1: im_paths[1], -1, d_cam: 0, with_dt: true, 
                //    pars: pars);
                //cv_series(im_paths, pars: pars);
                await cv_main_async(cv_idx, d_cam: 0, with_dt: true, exp_label: exp_l, pars: pars);
            }

            // Compare the computed flow with the ground truth while the lighting
            // experiment is still active, so all files are loaded from its folder.
            try
            {
                // The TV field describes frame 0 -> frame 1. Ground-truth index 0
                // is the zero-displacement reference and would invalidate every
                // relative-error sample, so compare against accumulated index 1.
                set_t_idx(1);
                refresh_uv_ground_truth_with_params(with_save: analysis_sweep_running());
            }
            catch (Exception exception)
            {
                Debug.LogError("Ground-truth comparison failed for " + exp_l + ": " + exception);
                progress_stopwatch.Stop();
                is_tv_running = false;
                is_tv_finished = false;
                if (progress_status_text != null)
                    progress_status_text.text = "Ground-Truth-Auswertung fehlgeschlagen: " + exp_l;
                if (progress_eta_text != null)
                    progress_eta_text.text = "Analyse abgebrochen – Details stehen im Unity-Log.";
                throw;
            }

            //29092026 Stereo-Tiefe (Abb. 7) fuer dieses Experiment, solange es noch aktiv ist
            if (this.get_with_main() && stereo_depth_enabled)
                await stereo_depth_step(path_dic + remove_dots(exp_l), pars);

            set_experiment("exp_normal");
        }

        done_ground_truth = true;
    }

    /// <summary>
    /// Executes the current render action and logs the progress.
    /// </summary>
    public void exe_render_acts()
    {
        this.render_acts[render_idx].act.Invoke();

        string progress_info = "progress: " + render_idx.ToString() + " / " +
            render_acts.Count.ToString();
        Debug.Log(progress_info);
        render_idx += 1;
    }
    /// <summary>
    /// Shows the sample of the next time step.
    /// </summary>
    public void activate_blade_act()
    {
        int blade_idx_l = get_blade_idx();
        blade_idx_l += 1;
        set_blade_idx(blade_idx_l);
        activate_blade(blade_idx: blade_idx_l);
    }
    //public void take_ref_pic_act()
    //{
    //    int blade_idx_l = get_blade_idx();
    //    take_ref_pic(blade_idx_l, cam_idx: 0);
    //}
    /// <summary>
    /// Legacy placeholder for updating the sample images.
    /// </summary>
    public void update_blade_pics()
    {
        //23092024 // blades times
        //23092024 activate_blade(blade_idx: blade_idx_l);
        //23092024 
        //23092024 take_pic(blade_idx_l, cam_idx: 0);
        //23092024 take_pic(blade_idx_l, cam_idx: 1);
        //23092024 take_ref_pic(blade_idx_l, cam_idx: 0);
        //23092024 
        //23092024 // one time
        //23092024 this.render_acts[render_idx].act.Invoke();
        //23092024 render_idx += 1;


    }
    /// <summary>
    /// Computes and stores the pixel-to-triangle assignment and the image-plane reference displacement for all time steps (camera 0).
    /// </summary>
    public void manage_distortion_ground_truth()
    {
        List<GameObject> blades = collect_blades();
        this.set_blades(blades);
        this.init_blade_tris(blades);
        for (int i = 0; i < blades.Count; i++)
        {
            // info (paul): We always match the blade i to the first blade at idx 0; 
            //      This means, that for i = 0, obviously all values will be zero

            activate_blade(i);

            //04112024 int idx_other = Mathf.Min((i + get_dt_compare()), blades.Count - 1);
            int idx_other = Mathf.Min((i + get_match_steps()), blades.Count - 1);

            //21092026 Bugfix: Fuer category != "muc" wurde die Ground Truth aus festen
            //Weltkoordinaten (z, -x, y) gebildet (with_proj = false). Seit Kameras und Probe
            //gedreht wurden (X-Achsen-Stereobasis, 180-Grad-Roll, 90 Grad um Y), passt diese
            //Zuordnung nicht mehr zum Kamerabild: value_ref war gegenueber dem TV-Fluss um
            //90 Grad verdreht (u <-> v), und die Loss-Karten verglichen falsche Komponenten.
            //Jetzt immer ueber die Kamera projizieren (Bildkoordinaten in Pixeln, wie der
            //TV-Fluss) - unabhaengig von Kamera-/Probenausrichtung. "muc" bleibt wie bisher.
            bool with_proj = true;
            if (category == "muc" && category_muc == "gom_curve")
            {
                with_proj = false;
            }
            (float[] d_xs, float[] d_ys, float[] d_zs) = construct_distortions(blades[i], blades[idx_other],
                cam: null, with_proj: with_proj);//0; i//24092024 1 oder so

            //16012025B (float[] d_xs, float[] d_ys, float[] d_zs) = construct_distortions(blades[0], blades[i]);

            // info (paul): save values (actually, cam_idx doesn't make sense here, so we just set it to 0)
            save_floats_for_blade(d_xs, cam_idx: 0, blade_idx: i, label: "_d_xs", with_uv_mode: false);
            save_floats_for_blade(d_ys, cam_idx: 0, blade_idx: i, label: "_d_ys", with_uv_mode: false);
            save_floats_for_blade(d_zs, cam_idx: 0, blade_idx: i, label: "_d_zs", with_uv_mode: false);

            // info (paul): mesh tris
            Mesh current_mesh = blades[i].GetComponent<MeshFilter>().mesh;
            int[] tris = current_mesh.triangles;
            save_ints_for_blade(tris, cam_idx: 0, blade_idx: i, label: "_blade_tris", with_uv_mode: false);

            save_tris(blade_idx: i);
        }
    }
    /// <summary>
    /// Initialises the triangle lists of the sample objects.
    /// </summary>
    /// <param name="blades">Sample objects of the time steps.</param>
    public void init_blade_tris(List<GameObject> blades)
    {
        List<int[]> blade_tris = init_tris_empty(blades.Count);//25092024 new List<int[]>();

        for (int i = 0; i < blades.Count; i++)
        {
            Mesh mesh = blades[i].GetComponent<MeshFilter>().sharedMesh;
            blade_tris[i] = mesh.triangles;
        }

        set_blade_tris(blade_tris);
    }
    /// <summary>
    /// Stores the sample objects of the time steps.
    /// </summary>
    /// <param name="input">Sample objects.</param>
    public void set_blades(List<GameObject> input)
    {
        this.blades = input;
    }
    /// <summary>
    /// Returns the sample objects of the time steps.
    /// </summary>
    /// <returns>Sample objects.</returns>
    public List<GameObject> get_blades()
    {
        return blades;
    }
    /// <summary>
    /// Stores the triangle indices of a time step.
    /// </summary>
    /// <param name="blade_idx">Time-step index.</param>
    public void save_tris(int blade_idx)
    {
        // info (paul): save triangle idxs

        List<GameObject> blades = collect_blades();

        (int[,] tris, float[][][] barys) = im2triangles(cam: cam_for_uv_0);

        // info (paul): cam_idx is 0, because is irrelevant here anyway
        save_ints2_for_blade(tris, cam_idx: 0, blade_idx: blade_idx, label: "_tris", with_uv_mode: false);
        save_floats3_for_blade(barys, cam_idx: 0, blade_idx: blade_idx, label: "_barys", with_uv_mode: false);
    }
    /// <summary>
    /// Assigns to every pixel of a camera the hit triangle of the sample and the barycentric coordinates (ray casting).
    /// </summary>
    /// <param name="cam">Camera.</param>
    /// <returns>Tuple (triangle index per pixel [x, y], barycentric coordinates per pixel).</returns>
    public (int[,], float[][][]) im2triangles(Camera cam)
    {
        int width = cam.pixelWidth;
        int height = cam.pixelHeight;

        int[,] tris = new int[width, height];
        float[][][] barys = new float[width][][];

        for (int i = 0; i < width; i++)
        {
            barys[i] = new float[height][];
            for (int j = 0; j < height; j++)
            {
                (int tri_idx, float[] bary) = find_triangle(j, i, cam: cam);
                tris[i, j] = tri_idx;
                barys[i][j] = bary;
            }
        }

        return (tris, barys);
    }
    /// <summary>
    /// Loads (or computes) the pixel-to-triangle assignment of the standard experiment.
    /// </summary>
    /// <param name="blade_idx">Time-step index.</param>
    /// <returns>Tuple (triangle index per pixel, barycentric coordinates).</returns>
    public (int[,], float[][][]) load_tris(int blade_idx)
    {
        string current_exp = get_experiment();
        set_experiment("exp_normal");

        // info (paul): cam_idx is 0, because is irrelevant here anyway
        int[,] tris = load_ints2_for_blade(cam_idx: 0, blade_idx: blade_idx, label: "_tris", with_uv_mode: false);
        float[][][] barys = load_floats3_for_blade(cam_idx: 0, blade_idx: blade_idx, label: "_barys", with_uv_mode: false);
        set_experiment(current_exp);

        return (tris, barys);
    }

    /// <summary>
    /// Adds two arrays element-wise (second may be null).
    /// </summary>
    /// <param name="floats_a">First array.</param>
    /// <param name="floats_b">Second array or null.</param>
    /// <returns>Sum.</returns>
    public float[] add_floats(float[] floats_a, float[] floats_b)
    {
        float[] floats_c = new float[floats_a.Length];

        if (floats_b == null)
        {
            floats_b = new float[floats_a.Length];
        }

        for (int i = 0; i < floats_a.Length; i++)
        {
            floats_c[i] = floats_a[i] + floats_b[i];
        }


        return floats_c;
    }
    /// <summary>
    /// Accumulates the per-vertex displacement over a range of time steps.
    /// </summary>
    /// <param name="t_idx_start">First time step.</param>
    /// <param name="t_idx_end">Last time step.</param>
    /// <param name="label">Component label.</param>
    /// <returns>Accumulated displacement per vertex.</returns>
    public float[] find_dxs_acc(int t_idx_start, int t_idx_end, string label)
    {
        float[] d_xs = null;

        //05112024 for (int i_idx = t_idx; i_idx < t_idx + get_match_steps(); i_idx++)
        //17012025 for (int i_idx = t_idx_start; i_idx < t_idx_start + blade_idxs.Count - 1; i_idx++)
        for (int i_idx = t_idx_start; i_idx < t_idx_end; i_idx++)
        {
            float[] d_xs_l = load_floats_for_blade(cam_idx: 0, blade_idx: i_idx, label: label, with_uv_mode: false);
            d_xs = add_floats(d_xs_l, d_xs);
        }
        if (t_idx_end == 0)
        {
            float[] d_xs_l = load_floats_for_blade(cam_idx: 0, blade_idx: 0, label: label, with_uv_mode: false);
            d_xs = zeros_of_size(d_xs_l.Length).ToArray();
        }

        return d_xs;
    }
    /// <summary>
    /// Image-plane reference displacement of all vertices between the first frame and a later frame (projected vertex positions of both meshes).
    /// </summary>
    /// <param name="blade_idx">Time-step index of the later frame.</param>
    /// <param name="from_path">Optional stored file.</param>
    /// <returns>Tuple (dx, dy, dz) per vertex.</returns>
    public (float[], float[], float[]) load_distortion_ground_truth(int blade_idx, string from_path = null)
    {
        List<GameObject> blades = collect_blades();
        //for (int i = 0; i < blades.Count; i++)
        //{
        // info (paul): We always match the blade i to the first blade at idx 0; 
        //      This means, that for i = 0, obviously all values will be zero
        //(float[] d_xs, float[] d_ys, float[] d_zs) = map_distortions(blades[0], blades[i]);

        // info (paul): save values (actually, cam_idx doesn't make sense here, so we just set it to 0)

        string current_exp = get_experiment();
        set_experiment("exp_normal");

        float[] d_xs = find_dxs_acc(t_idx_start: 0, t_idx_end: blade_idx, "_d_xs");
        float[] d_ys = find_dxs_acc(t_idx_start: 0, t_idx_end: blade_idx, "_d_ys");
        float[] d_zs = find_dxs_acc(t_idx_start: 0, t_idx_end: blade_idx, "_d_zs");
        //B float[] d_xs = zeros_of_size(498600).ToArray();
        //B float[] d_ys = zeros_of_size(498600).ToArray();
        //B float[] d_zs = zeros_of_size(498600).ToArray();

        //float[] d_xs = load_floats_for_blade(cam_idx: 0, blade_idx: blade_idx, label: "_d_xs", with_uv_mode: false);
        //float[] d_ys = load_floats_for_blade(cam_idx: 0, blade_idx: blade_idx, label: "_d_ys", with_uv_mode: false);
        //float[] d_zs = load_floats_for_blade(cam_idx: 0, blade_idx: blade_idx, label: "_d_zs", with_uv_mode: false);

        int[] blade_tris_i = load_ints_for_blade(cam_idx: 0, blade_idx: blade_idx + get_match_steps(),
            label: "_blade_tris", with_uv_mode: false);
        //22092026 Fallback: alle Frames haben dieselbe Topologie (wird in diagnose_lighting_flow
        //vorausgesetzt). Fehlt die Datei fuer blade_idx+match_steps, die naechste vorhandene nehmen.
        for (int fallback = blade_idx; blade_tris_i == null && fallback >= 0; fallback--)
        {
            blade_tris_i = load_ints_for_blade(cam_idx: 0, blade_idx: fallback, label: "_blade_tris", with_uv_mode: false);
            if (blade_tris_i != null)
                Debug.LogWarning("Topologie-Datei fuer Frame " + (blade_idx + get_match_steps()) + " fehlt - verwende Frame " + fallback + ".");
        }
        if (blade_tris_i == null)
            throw new FileNotFoundException("Keine Topologie-Datei (*__blade_trisints_r" + get_render_res()
                + ") fuer '" + get_experiment() + "' gefunden. Bitte Analyse mit with_exp laufen lassen.");

        if (true)//13072024 (blade_tris.Count < blade_idx_max - blade_idx_min)
        {
            //24092024 blade_tris[blade_idx] = blade_tris_i;
            set_blade_tris_at(blade_idx, blade_tris_i);
        }

        set_experiment(current_exp);

        return (d_xs, d_ys, d_zs);
    }
    /// <summary>
    /// Stores the triangle indices of one time step.
    /// </summary>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="slice">Triangle indices.</param>
    public void set_blade_tris_at(int blade_idx, int[] slice)
    {
        List<int[]> blade_tris = get_blade_tris();
        blade_tris[blade_idx] = slice;
        set_blade_tris(blade_tris);
    }
    /// <summary>
    /// Stores the triangle lists of all time steps.
    /// </summary>
    /// <param name="input">Triangle lists.</param>
    public void set_blade_tris(List<int[]> input)
    {
        this.blade_tris = input;
    }
    /// <summary>
    /// Returns the triangle lists of all time steps.
    /// </summary>
    /// <returns>Triangle lists.</returns>
    public List<int[]> get_blade_tris()
    {
        return this.blade_tris;
    }
    /// <summary>
    /// Projects 3D positions into the image of a camera.
    /// </summary>
    /// <param name="now">World positions.</param>
    /// <param name="cam">Camera.</param>
    /// <returns>Projected positions.</returns>
    public Vector3[] pos2uvs(Vector3[] now, Camera cam)
    {
        // info (paul): map 3d coordinates to 2d position

        Vector3[] now_proj = new Vector3[now.Length];

        for (int i = 0; i < now.Length; i++)
        {
            now_proj[i] = cam.WorldToScreenPoint(now[i]);
        }

        return now_proj;
    }

    /// <summary>
    /// Transforms local mesh vertices to world coordinates.
    /// </summary>
    /// <param name="local">Local vertices.</param>
    /// <param name="blade">Object whose transform is applied.</param>
    /// <returns>World positions.</returns>
    public Vector3[] transform_to_world(Vector3[] local, GameObject blade)
    {
        // info (paul): transform local verts to global, scaled, rotated etc. verts

        Vector3[] globals = new Vector3[local.Length];

        for (int i = 0; i < local.Length; i++)
        {
            Vector3 local_i = local[i];
            Vector3 global_i = blade.transform.TransformPoint(local_i);
            globals[i] = global_i;
        }

        return globals;
    }
    /// <summary>
    /// World positions and, optionally, projected image positions of all vertices of a sample object.
    /// </summary>
    /// <param name="current_blade">Sample object.</param>
    /// <param name="cam">Camera.</param>
    /// <param name="with_proj">True to project.</param>
    /// <returns>Tuple (world positions, projected positions).</returns>
    public (Vector3[], Vector3[]) proj_blade(GameObject current_blade, Camera cam,
        bool with_proj = true)
    {
        Mesh current_mesh = current_blade.GetComponent<MeshFilter>().mesh;
        Vector3[] now_local = current_mesh.vertices;
        Vector3[] now = transform_to_world(now_local, current_blade);
        Vector3[] now_proj = null;
        if (with_proj)
        {
            now_proj = pos2uvs(now, cam: cam);
        }
        else
        {
            now_proj = new Vector3[now.Length];
            for (int i = 0; i < now.Length; i++)
            {
                now_proj[i] = new Vector3(now[i].z, -now[i].x, now[i].y);
            }
        }
        return (now_proj, now_proj);
    }

    /// <summary>
    /// Empty placeholder (unused).
    /// </summary>
    public void aaaaa()
    {
        ;

        ;
    }

    /// <summary>
    /// Displacement of all vertices between two sample objects, in the image plane of a camera or in 3D.
    /// </summary>
    /// <param name="current_blade">Sample of the first frame.</param>
    /// <param name="next_blade">Sample of the second frame.</param>
    /// <param name="cam">Camera (default camera 0).</param>
    /// <param name="with_proj">True for image-plane displacement.</param>
    /// <returns>Tuple (dx, dy, dz) per vertex.</returns>
    public (float[], float[], float[]) construct_distortions(GameObject current_blade,
        GameObject next_blade, Camera cam = null, bool with_proj = true)
    {
        if (cam == null)
        {
            cam = this.cam_for_uv_0;
        }

        (Vector3[] now_proj, Vector3[] now) = proj_blade(current_blade, cam: cam, with_proj: with_proj);//17032025 false
        (Vector3[] next_proj, Vector3[] next) = proj_blade(next_blade, cam: cam, with_proj: with_proj);//17032025 false

        //Mesh current_mesh = current_blade.GetComponent<MeshFilter>().mesh;
        //Mesh next_mesh = next_blade.GetComponent<MeshFilter>().mesh;
        //
        //Vector3[] now_local = current_mesh.vertices;
        //Vector3[] next_local = next_mesh.vertices;
        //
        //Vector3[] now = transform_to_world(now_local, current_blade);
        //Vector3[] next = transform_to_world(next_local, next_blade);
        //
        //Vector3[] now_proj = pos2uvs(now, cam: cam_for_uv_0);
        //Vector3[] next_proj = pos2uvs(next, cam: cam_for_uv_0);

        //15032025A (float[] d_xs, float[] d_ys, float[] d_zs) = vec_diff(now_proj, next_proj);
        (float[] d_xs, float[] d_ys, float[] d_zs) = vec_diff(now, next);

        float min_dx = d_xs.Min();
        float max_dx = d_xs.Max();

        return (d_xs, d_ys, d_zs);

    }

    /// <summary>
    /// Component-wise difference of two point lists.
    /// </summary>
    /// <param name="now_proj">First positions.</param>
    /// <param name="next_proj">Second positions.</param>
    /// <returns>Tuple (dx, dy, dz).</returns>
    public (float[], float[], float[]) vec_diff(Vector3[] now_proj, Vector3[] next_proj)
    {
        float[] d_xs = new float[now_proj.Length];
        float[] d_ys = new float[now_proj.Length];
        float[] d_zs = new float[now_proj.Length];

        for (int i = 0; i < now_proj.Length; i++)
        {
            float d_x = next_proj[i].x - now_proj[i].x;
            float d_y = next_proj[i].y - now_proj[i].y;
            float d_z = next_proj[i].z - now_proj[i].z;

            d_xs[i] = d_x;
            d_ys[i] = d_y;
            d_zs[i] = d_z;
        }

        return (d_xs, d_ys, d_zs);
    }

    /// <summary>
    /// Reads a numeric matrix from a CSV/text file.
    /// </summary>
    /// <param name="filePath">File or folder.</param>
    /// <param name="t_idx">Time index for the file name (-1 = none).</param>
    /// <param name="direct_access">True if filePath is the file itself.</param>
    /// <returns>Matrix.</returns>
    public List<List<float>> read_dists(string filePath, int t_idx = -1, bool direct_access = false)
    {
        // info (paul): read out file into strings at path
        List<string[]> lines = read_lines_from(filePath, t_idx, direct_access: direct_access);

        // info (paul): convert strings to numbers:
        List<List<float>> mat_raw = strs2mat(lines);

        // info (paul): reconstruct the distance map in 3d space from disparities
        List<List<float>> mat = mat_raw;//07102024 mat_raw2dists(mat_raw);

        // info (paul): cut off floor
        if (!direct_access)
        {
            mat = transpose_mat(mat);//24062024
            mat = cut_off(mat);
        }
        return mat;
    }

    /// <summary>
    /// Reads the lines of a data file split into fields.
    /// </summary>
    /// <param name="filePath">File or folder.</param>
    /// <param name="t_idxl">Time index for the file name.</param>
    /// <param name="direct_access">True if filePath is the file itself.</param>
    /// <returns>List of field arrays.</returns>
    public List<string[]> read_lines_from(string filePath, int t_idxl, bool direct_access = false)
    {
        // info (paul): open the file and read the lines into strings

        //03072024 string full_path = filePath + "/displacements_u_" + t_idx + ".csv";
        //05072024 string full_path = filePath + "/" + get_experiment() + "/cam_0/uv/displacements_u_" + t_idx + ".csv";
        //26072024 string full_path = filePath + "/" + get_experiment() + "/stereo/displacements_u_" + t_idx + ".csv";

        string full_path = filePath;
        if (!direct_access)
        {
            full_path = filePath + "/" + "exp_normal" + "/stereo/displacements_u_" + t_idx + ".csv";
        }


        List<string[]> lines = new List<string[]>();
        try
        {
            using (StreamReader reader = new StreamReader(full_path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] els = line.Split(',');
                    lines.Add(els);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        log("\n 1B");
        return lines;
    }

    /// <summary>
    /// Legacy conversion of a disparity matrix to distances with the old angle formula (does not match the current camera setup).
    /// </summary>
    /// <param name="mat">Disparity matrix.</param>
    /// <returns>Distance matrix.</returns>
    public List<List<float>> mat_raw2dists(List<List<float>> mat)
    {
        // info (paul): converted the float value matrix from the file, 
        //          to the correct distance matrix (including trigonometrics etc.)
        float min_val = float.NaN;

        mat = transpose_mat(mat);
        mat = invert_sign_of_mat(mat);
        (mat, min_val) = set_zeros_to_min(mat);
        mat = disp2dist(mat, min_val);//16052024
        return mat;
    }

    /// <summary>
    /// Converts text fields to a float matrix.
    /// </summary>
    /// <param name="lines">Lines split into fields.</param>
    /// <returns>Matrix.</returns>
    public List<List<float>> strs2mat(List<string[]> lines)
    {
        // info (paul): convert lines of strings to matrix of floats

        List<List<float>> mat = new List<List<float>>();
        log("\n lines: " + lines.Count.ToString());

        for (int i_idx = 0; i_idx < lines.Count; i_idx++)
        {
            mat.Add(new List<float>());
            for (int j_idx = 0; j_idx < lines[0].Length; j_idx++)
            {
                string line_el = lines[i_idx][j_idx];
                float value = float.Parse(line_el, CultureInfo.InvariantCulture);
                if (value != 0)
                {
                    ;
                }
                mat[i_idx].Add(value);
            }
        }

        return mat;
    }

    /// <summary>
    /// Replaces zero entries by the minimum of the matrix (background below the analysed area).
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <returns>Tuple (modified matrix, minimum).</returns>
    public (List<List<float>>, float) set_zeros_to_min(List<List<float>> mat)
    {
        // info (paul): set the zero values to the min value of mat,
        //      in order to achieve, that the surrounding un-analyzed area
        //      is not above the lowest part of the analyzed area of the
        //      displacement/stereo optical flow

        // Produktionsmanagement im Nutzfahrzeugbau, im Mai

        (float min_val, float max_val) = find_max_2d(mat);

        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < mat[0].Count; j++)
            {
                bool is_zero = (mat[i][j] == 0f);

                if (is_zero)
                {
                    mat[i][j] = min_val;
                }
            }
        }

        return (mat, min_val);
    }

    /// <summary>
    /// Debugging helper: overwrites a matrix with a synthetic profile.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <returns>Modified matrix.</returns>
    public List<List<float>> overwrite(List<List<float>> mat)
    {
        // info (paul): overwrite values for debugging reasons

        int length = mat[0].Count;
        float len_mid = (float)length / 2f;

        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < length; j++)
            {
                float new_val = -0.001f * (i - 100) * (i - 100) - 0.001f * (j - len_mid) * (j - len_mid);
                float p_fac = 1.0f;
                mat[i][j] = (1f - p_fac) * mat[i][j] + p_fac * (new_val);
            }
        }

        return mat;
    }
    /// <summary>
    /// Legacy conversion of disparities to distances.
    /// </summary>
    /// <param name="mat">Disparity matrix.</param>
    /// <param name="min_val">Minimum disparity.</param>
    /// <returns>Distance matrix.</returns>
    public List<List<float>> disp2dist(List<List<float>> mat, float min_val)
    {
        //List<List<float>> dists = copy_mat(mat);
        //
        //for (int width_idx = 0; width_idx < mat.Count; width_idx++)
        //{
        //    //for (int j = 0; j < mat[0].Count; j++)
        //    for (int height_idx = 0; height_idx < mat[0].Count; height_idx++)
        //    {
        //        if (true)//(disp_ij > min_val + 0.1f)
        //        {
        //            dists[width_idx][height_idx] = disp2dist_ij_new(x_l,
        //                  x_r, span, width_idx);
        //        }
        //    }
        //}

        (List<List<float>> xi_val_mat, List<List<float>> xi_p_val_mat) = act_5(mat);//disp2dist_ij_new(mat, act_5);
        List<List<float>> dists = act_6(xi_val_mat, xi_p_val_mat);//disp2dist_ij_new(mat, act_6);
        List<List<float>> dists_3D = to_3d(dists);
        return dists_3D;//17122024 dists;
    }

    /// <summary>
    /// Legacy conversion of distances to a 3D height map.
    /// </summary>
    /// <param name="dists">Distance matrix.</param>
    /// <returns>Height map.</returns>
    List<List<float>> to_3d(List<List<float>> dists)
    {
        List<List<float>> dists_3D = copy_mat(dists);

        for (int i = 0; i < dists.Count; i++)
        {
            for (int j = 0; j < dists[i].Count; j++)
            {
                float y_l = (float)i;
                float y_r = y_l;

                float dist = dists[i][j];
                //18052025 float gamma_span = 0.5f * fov_for_heights * Mathf.PI / 180f;
                float gamma_span = 0.5f * fov_for_heights * Mathf.PI / 180f;

                (float xi_val, _) = pix2xi(y_l, y_r, span: dists.Count, gamma_span);
                dist = dist / Mathf.Cos(xi_val);
                dists_3D[i][j] = dist;
            }
        }

        return dists_3D;
    }

    /// <summary>
    /// Older legacy conversion of disparities to distances.
    /// </summary>
    /// <param name="mat">Disparity matrix.</param>
    /// <param name="min_val">Minimum disparity.</param>
    /// <returns>Distance matrix.</returns>
    public List<List<float>> disp2dist_old(List<List<float>> mat, float min_val)
    {
        //mat = overwrite(mat);

        List<List<float>> dists = copy_mat(mat);

        for (int width_idx = 0; width_idx < mat.Count; width_idx++)
        {
            //for (int j = 0; j < mat[0].Count; j++)
            for (int height_idx = 0; height_idx < mat[0].Count; height_idx++)
            {
                float disp_ij = mat[width_idx][height_idx];
                float x_l = (float)width_idx;
                float x_r = (float)(x_l) + disp_ij;
                float span = (float)mat.Count;

                if (disp_ij > min_val + 0.1f)
                {
                    //dists[width_idx][height_idx] = disp2dist_ij(x_l,
                    //      x_r, span, width_idx);
                    dists[width_idx][height_idx] = disp2dist_ij(disp_ij);
                }
            }
        }

        return dists;
    }

    /// <summary>
    /// List of zeros with the length of the input.
    /// </summary>
    /// <param name="input">Reference list.</param>
    /// <returns>Zero list.</returns>
    public List<float> zeros_like(List<float> input)
    {
        List<float> vals = new List<float>();
        for (int i = 0; i < input.Count; i++)
        {
            vals.Add(0f);
        }
        return vals;
    }

    /// <summary>
    /// Matrix of (0, 0) tuples with the shape of the input.
    /// </summary>
    /// <param name="mat">Reference matrix.</param>
    /// <returns>Zero matrix.</returns>
    public List<List<(int, int)>> zeros_like(List<List<(int, int)>> mat)
    {
        List<List<(int, int)>> empty = new List<List<(int, int)>>();

        for (int i = 0; i < mat.Count; i++)
        {
            empty.Add(new List<(int, int)>());
            for (int j = 0; j < mat[0].Count; j++)
            {
                empty[i].Add((0, 0));
            }
        }
        return empty;
    }

    /// <summary>
    /// Float matrix of zeros with the shape of an integer-tuple matrix.
    /// </summary>
    /// <param name="mat">Reference matrix.</param>
    /// <param name="return_type">Result type (floats).</param>
    /// <returns>Zero matrix.</returns>
    public List<List<float>> zeros_like(List<List<(int, int)>> mat, string return_type = "floats")
    {
        List<List<float>> empty = new List<List<float>>();

        for (int i = 0; i < mat.Count; i++)
        {
            empty.Add(new List<float>());
            for (int j = 0; j < mat[0].Count; j++)
            {
                empty[i].Add(0f);
            }
        }
        return empty;
    }
    /// <summary>
    /// Float matrix of zeros with the shape of a float-tuple matrix.
    /// </summary>
    /// <param name="mat">Reference matrix.</param>
    /// <param name="return_type">Result type (floats).</param>
    /// <returns>Zero matrix.</returns>
    public List<List<float>> zeros_like(List<List<(float, float)>> mat, string return_type = "floats")
    {
        List<List<float>> empty = new List<List<float>>();

        for (int i = 0; i < mat.Count; i++)
        {
            empty.Add(new List<float>());
            for (int j = 0; j < mat[0].Count; j++)
            {
                empty[i].Add(0f);
            }
        }
        return empty;
    }
    /// <summary>
    /// Float matrix of zeros with the shape of an integer matrix.
    /// </summary>
    /// <param name="mat">Reference matrix.</param>
    /// <returns>Zero matrix.</returns>
    public List<List<float>> zeros_like(List<List<int>> mat)
    {
        List<List<float>> empty = new List<List<float>>();

        for (int i = 0; i < mat.Count; i++)
        {
            empty.Add(new List<float>());
            for (int j = 0; j < mat[0].Count; j++)
            {
                empty[i].Add(0f);
            }
        }
        return empty;
    }
    /// <summary>
    /// Float matrix of zeros with the shape of the input.
    /// </summary>
    /// <param name="mat">Reference matrix.</param>
    /// <returns>Zero matrix.</returns>
    public List<List<float>> zeros_like(List<List<float>> mat)
    {
        List<List<float>> empty = new List<List<float>>();

        for (int i = 0; i < mat.Count; i++)
        {
            empty.Add(new List<float>());
            for (int j = 0; j < mat[0].Count; j++)
            {
                empty[i].Add(0f);
            }
        }
        return empty;
    }
    /// <summary>
    /// Float matrix of zeros with the shape of a 2D integer array.
    /// </summary>
    /// <param name="mat">Reference array.</param>
    /// <returns>Zero matrix.</returns>
    public List<List<float>> zeros_like(int[,] mat)
    {
        List<List<float>> empty = new List<List<float>>();

        for (int i = 0; i < mat.GetLength(0); i++)
        {
            empty.Add(new List<float>());
            for (int j = 0; j < mat.GetLength(1); j++)
            {
                empty[i].Add(0f);
            }
        }
        return empty;
    }
    public List<List<float>> copy_mat(List<List<float>> mat)//zeros_like
    {
        List<List<float>> empty = new List<List<float>>();

        for (int i = 0; i < mat.Count; i++)
        {
            empty.Add(new List<float>());
            for (int j = 0; j < mat[0].Count; j++)
            {
                empty[i].Add(mat[i][j]);
            }
        }
        return empty;
    }
    //public List<List<float>> zeros_of_size(int size_x, int size_y)
    //{
    //    List<List<float>> empty = new List<List<float>>();
    //
    //    for (int i = 0; i < size_x; i++)
    //    {
    //        empty.Add(new List<float>());
    //        for (int j = 0; j < size_y; j++)
    //        {
    //            empty[i].Add(0f);
    //        }
    //    }
    //    return empty;
    //}
    /// <summary>
    /// List of zeros.
    /// </summary>
    /// <param name="num">Length.</param>
    /// <returns>Zero list.</returns>
    public List<float> zeros_of_size(int num)
    {
        List<float> empty = new List<float>();

        for (int i = 0; i < num; i++)
        {
            empty.Add(0f);
        }
        return empty;
    }
    /// <summary>
    /// List of double zeros.
    /// </summary>
    /// <param name="num">Length.</param>
    /// <returns>Zero list.</returns>
    public List<double> doubles_of_size(int num)
    {
        List<double> empty = new List<double>();

        for (int i = 0; i < num; i++)
        {
            empty.Add(0f);
        }
        return empty;
    }
    /// <summary>
    /// List of integer zeros.
    /// </summary>
    /// <param name="num">Length.</param>
    /// <returns>Zero list.</returns>
    public List<int> ints_of_size(int num)
    {
        List<int> empty = new List<int>();

        for (int i = 0; i < num; i++)
        {
            empty.Add(0);
        }
        return empty;
    }
    /// <summary>
    /// Matrix of (0, 0) tuples with the shape of the input.
    /// </summary>
    /// <param name="mat">Reference matrix.</param>
    /// <returns>Zero matrix.</returns>
    public List<List<(float, float)>> zero_tuples_like(List<List<float>> mat)
    {
        List<List<(float, float)>> empty = new List<List<(float, float)>>();

        for (int i = 0; i < mat.Count; i++)
        {
            empty.Add(new List<(float, float)>());
            for (int j = 0; j < mat[0].Count; j++)
            {
                empty[i].Add((0f, 0f));
            }
        }
        return empty;
    }

    /// <summary>
    /// Legacy conversion of one disparity to a distance (fixed camera angles).
    /// </summary>
    /// <param name="d_x">Disparity.</param>
    /// <returns>Distance.</returns>
    public float disp2dist_ij(float d_x)
    {
        // info (paul): disp is d_x

        // info (paul): Parameter TODO: replace by actual values
        float alpha = 22f / 180f; // info (paul): rotation angle between the cameras
        float gamma_span = 0.2f * Mathf.PI; // info (paul): span of screen
        float d_x_span = 20f;
        float D_p = 10f; // probably the reference distance; D_p kind of corresponds to gamma_pp
        float d_x_pp = 200f; // info (paul): basically this is a ref offset, which is the distance g between the camera
        float dist_forward = 3f; // info (paul): how much the cams are different in there parallel distance

        // info (paul): for non-small angles we would have
        //      float gamma = Mathf.Atan(d_x/d_x_span * Mathf.Tan(gamma_span));
        //      float gamma_pp = Mathf.Atan(d_x_pp/d_x_span * Mathf.Tan(gamma_span));
        //      float D_val = (D_p * Mathf.Tan(gamma_pp + alpha))/(Mathf.Tan(gamma));

        // info (paul): get angle from d_x pixel position on screen
        float gamma = d_x / d_x_span * gamma_span;

        // info (paul): adjust for dist_forward:
        //23042024 float tan_beta = Mathf.Atan(1/(1/Mathf.Tan(gamma) + 1/d_x_pp));
        //23042024 float beta = Mathf.Atan(tan_beta);
        float beta = gamma;

        // info (paul): adjust for camera rotation (quite simple)
        float epsilon = beta + alpha;

        // info (paul): calculate the distance with the parallaxe
        float gamma_pp = d_x_pp / d_x_span * gamma_span;
        float D_val = (D_p * (beta + epsilon)) / gamma_pp; // I just switched gamma and gamma_pp

        return D_val;
    }

    /// <summary>
    /// Legacy: viewing angles of a point in both cameras from its pixel positions.
    /// </summary>
    /// <param name="x_l">Position in the left image.</param>
    /// <param name="x_r">Position in the right image.</param>
    /// <param name="span">Image width.</param>
    /// <param name="gamma_span">Half field of view.</param>
    /// <returns>Tuple of both angles.</returns>
    public (float, float) pix2xi(float x_l, float x_r, float span, float gamma_span)
    {
        float x_l_centric = (x_l - 0.5f * span) / (0.5f * span);
        float x_r_centric = (x_r - 0.5f * span) / (0.5f * span);

        float xi_val_tan = x_r_centric * Mathf.Tan(gamma_span);
        float xi_p_val_tan = x_l_centric * Mathf.Tan(gamma_span);
        float xi_val = Mathf.Atan(xi_val_tan);
        float xi_p_val = Mathf.Atan(xi_p_val_tan);
        return (xi_val, xi_p_val);
    }
    /// <summary>
    /// Legacy distance from pixel positions in both cameras (angle formula).
    /// </summary>
    /// <param name="x_l">Position in the left image.</param>
    /// <param name="x_r">Position in the right image.</param>
    /// <param name="span">Image width.</param>
    /// <returns>Distance.</returns>
    public float disp2dist_new(float x_l, float x_r, float span)
    {
        // info (paul): - xi is the angle from the one camera (further behind and on the right
        //          site, assuming the object is on the point side)
        //              - xi_p: angle from the other camera
        //              - alpha: rotation angle from the other camera

        // info (paul): nakajima-close parameters
        float gamma_span = 0.5f * cam_for_uv_0.fieldOfView * Mathf.PI / 180f; // info (paul): span of screen (I think half of it)
        float alpha = 2 * cam_angle * Mathf.PI / 180f; // info (paul): rotation angle between the cameras
        float g_val = (cam_for_uv_1.transform.position.x - cam_for_uv_0.transform.position.x); // info (paul): horizontal distance of the cameras
        float h_val = (cam_for_uv_1.transform.position.y - cam_for_uv_0.transform.position.y); // info (paul): depth distance of the cameras

        // info (paul): getting xi from d_x or so
        (float xi_val, float xi_p_val) = pix2xi(x_l, x_r, span, gamma_span);

        // info (paul): doing all the rest
        float dist_val = find_dist_val(g_val, h_val, alpha, xi_val, xi_p_val);

        return dist_val;//d_val;
    }


    /// <summary>
    /// Legacy: disparity and pixel positions of one matrix element.
    /// </summary>
    /// <param name="mat">Disparity matrix.</param>
    /// <param name="width_idx">Column.</param>
    /// <param name="height_idx">Row.</param>
    /// <returns>Tuple (disparity, left position, right position, width).</returns>
    public (float, float, float, float) el2vals(List<List<float>> mat, int width_idx, int height_idx)
    {
        float disp_ij = mat[width_idx][height_idx];

        float x_l = (float)width_idx;
        float x_r = (float)(x_l) + disp_ij;
        float span = (float)mat.Count;
        return (disp_ij, x_l, x_r, span);
    }


    /// <summary>
    /// Legacy: viewing angles of one element for the Nakajima camera setup.
    /// </summary>
    /// <param name="disp_ij">Disparity.</param>
    /// <param name="x_l">Left position.</param>
    /// <param name="x_r">Right position.</param>
    /// <param name="span">Image width.</param>
    /// <returns>Tuple of both angles.</returns>
    public (float, float) act_5_ij(float disp_ij, float x_l, float x_r, float span)
    {
        // info (paul): nakajima-close parameters

        float gamma_span = 0.5f * fov_for_heights * Mathf.PI / 180f; // info (paul): span of screen (I think half of it)
        float alpha = 2 * cam_angle * Mathf.PI / 180f; // info (paul): rotation angle between the cameras
        float g_val = (cam_for_uv_1.transform.position.x - cam_for_uv_0.transform.position.x); // info (paul): horizontal distance of the cameras
        float h_val = (cam_for_uv_1.transform.position.y - cam_for_uv_0.transform.position.y); // info (paul): depth distance of the cameras

        (float xi_val, float xi_p_val) = pix2xi(x_l, x_r, span, gamma_span);
        return (xi_val, xi_p_val);
    }


    int width_idx_0 = 256;
    int height_idx_0 = 256;
    /// <summary>
    /// Legacy: viewing angles of all elements of a disparity matrix.
    /// </summary>
    /// <param name="mat">Disparity matrix.</param>
    /// <returns>Tuple of angle matrices.</returns>
    public (List<List<float>>, List<List<float>>) act_5(List<List<float>> mat)
    {
        List<List<float>> xi_val_mat = copy_mat(mat);
        List<List<float>> xi_p_val_mat = copy_mat(mat);

        for (int width_idx = 0; width_idx < mat.Count; width_idx++)
        {
            for (int height_idx = 0; height_idx < mat[width_idx].Count; height_idx++)
            {
                if (width_idx == width_idx_0 && height_idx == height_idx_0)
                {
                    ;
                }
                (float disp_ij, float x_l, float x_r, float span) = el2vals(mat, width_idx, height_idx);
                (float xi_val, float xi_p_val) = act_5_ij(disp_ij, x_l, x_r, span);
                (xi_val_mat[width_idx][height_idx], xi_p_val_mat[width_idx][height_idx]) = (xi_val, xi_p_val);
            }
        }
        return (xi_val_mat, xi_p_val_mat);
    }

    /// <summary>
    /// Legacy: distances from the viewing angles of both cameras.
    /// </summary>
    /// <param name="xi_val_mat">Angles of camera 0.</param>
    /// <param name="xi_p_val_mat">Angles of camera 1.</param>
    /// <returns>Distance matrix.</returns>
    public List<List<float>> act_6(
        List<List<float>> xi_val_mat, List<List<float>> xi_p_val_mat)
    {
        List<List<float>> dist_mat = copy_mat(xi_val_mat);

        // info (paul): calculate g and h
        float cam_x = cam_for_uv_1.transform.position.x;
        float cam_y = cam_for_uv_1.transform.position.y;
        if (category == "muc")
        {
            cam_x = Mathf.Abs(cam_for_uv_0.transform.position.z - cam_for_uv_1.transform.position.z);
        }

        float x_val = Mathf.Abs(cam_x);
        float d_val = Mathf.Sqrt(cam_x * cam_x + cam_y * cam_y);
        float g_val = 2 * x_val * Mathf.Cos(Mathf.PI / 2f - Mathf.Acos(x_val / d_val));//27112024  (cam_for_uv_1.transform.position.x - cam_for_uv_0.transform.position.x); // info (paul): horizontal distance of the cameras
        float h_val = g_val * Mathf.Tan(Mathf.PI / 2f - Mathf.Acos(x_val / d_val));//27112024  (cam_for_uv_1.transform.position.y - cam_for_uv_0.transform.position.y); // info (paul): depth distance of the cameras
        if (category == "muc")
        {
            g_val = x_val;
            h_val = Mathf.Abs(cam_for_uv_1.transform.position.y - cam_for_uv_0.transform.position.y);
        }

        // info (paul): find the distances
        for (int i = 0; i < xi_val_mat.Count; i++)
        {
            for (int j = 0; j < xi_val_mat[i].Count; j++)
            {
                if (i == width_idx_0 && j == height_idx_0)
                {
                    ;
                }

                // info (paul): nakajima-close parameters
                // 18032025 float gamma_span = 0.5f * fov_for_heights * Mathf.PI / 180f; // info (paul): span of screen (I think half of it)
                float alpha = (cam_angle_1 - cam_angle_0) * Mathf.PI / 180f; // info (paul): rotation angle between the cameras

                float dist_val = find_dist_val(g_val, h_val, alpha, xi_val_mat[i][j], xi_p_val_mat[i][j]);
                dist_mat[i][j] = dist_val;
            }
        }
        return (dist_mat);
    }

    /// <summary>
    /// Legacy: applies a per-element conversion function to a disparity matrix.
    /// </summary>
    /// <param name="mat">Disparity matrix.</param>
    /// <param name="act">Conversion function.</param>
    /// <returns>Converted matrix.</returns>
    public List<List<float>> disp2dist_ij_new(List<List<float>> mat, Func<float, float, float, float, float> act)
    {
        for (int width_idx = 0; width_idx < mat.Count; width_idx++)
        {
            for (int height_idx = 0; height_idx < mat[width_idx].Count; height_idx++)
            {
                (float disp_ij, float x_l, float x_r, float span) = el2vals(mat, width_idx, height_idx);
                mat[width_idx][height_idx] = act(disp_ij, x_l, x_r, span);
            }
        }
        return mat;

        //A // info (paul): - xi is the angle from the one camera (further behind and on the right
        //A //          site, assuming the object is on the point side)
        //A //              - xi_p: angle from the other camera
        //A //              - alpha: rotation angle from the other camera
        //A 
        //A // info (paul): nakajima-close parameters
        //A float gamma_span = 0.5f * cam_for_uv_0.fieldOfView * Mathf.PI / 180f; // info (paul): span of screen (I think half of it)
        //A float alpha = 2 * cam_angle * Mathf.PI / 180f; // info (paul): rotation angle between the cameras
        //A float g_val = (cam_for_uv_1.transform.position.x - cam_for_uv_0.transform.position.x); // info (paul): horizontal distance of the cameras
        //A float h_val = (cam_for_uv_1.transform.position.y - cam_for_uv_0.transform.position.y); // info (paul): depth distance of the cameras
        //A 
        //A // info (paul): getting xi from d_x or so
        //A (float xi_val, float xi_p_val) = pix2xi(x_l, x_r, span, gamma_span);
        //A 
        //A // info (paul): doing all the rest
        //A float dist_val = find_dist_val(g_val, h_val, alpha, xi_val, xi_p_val);
        //A 
        //A return dist_val;//d_val;
    }

    /// <summary>
    /// Legacy: distance of a point from the camera geometry (triangle relations).
    /// </summary>
    /// <param name="g_val">Horizontal camera offset.</param>
    /// <param name="h_val">Vertical camera offset.</param>
    /// <param name="alpha">Angle between the cameras.</param>
    /// <param name="xi_val">Angle in camera 0.</param>
    /// <param name="xi_p_val">Angle in camera 1.</param>
    /// <returns>Distance.</returns>
    public float find_dist_val(float g_val, float h_val, float alpha, float xi_val, float xi_p_val)
    {
        float c_val = Mathf.Sqrt(g_val * g_val + h_val * h_val);

        float alpha_1 = Mathf.Atan(h_val / g_val);
        float alpha_2 = Mathf.Atan(g_val / h_val);//c_val);
        float alpha_3 = Mathf.PI - alpha_2 - alpha - xi_p_val;//yep
        float alpha_4 = Mathf.PI / 2f - alpha_1;//yep
        float alpha_5 = Mathf.PI - alpha_4 - alpha_3 - xi_val;//yep//02052024
        float alpha_6 = Mathf.PI / 2f - xi_val;//yep

        float d_val_p = c_val * Mathf.Sin(alpha_3) / Mathf.Sin(alpha_5);// 100.0, 4.3, -2.8
        float d_val = d_val_p * Mathf.Sin(alpha_6);

        float dist_val = d_val;//xi_val - xi_p_val;//d_val;// - alpha_5;//Mathf.Sin(alpha_3) / Mathf.Sin(alpha_5);
        return dist_val;
    }

    /// <summary>
    /// Transposes a matrix.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <returns>Transposed matrix.</returns>
    public List<List<float>> transpose_mat(List<List<float>> mat)
    {
        //log("\n 1CA");
        List<List<float>> mat_t = new List<List<float>>();
        //log("\n 1CB");
        //log("\n 1CBmat_cnt: " + mat.Count);
        //try
        //{
        //    log("\n 1CBmat[0]_cnt: " + (mat[0].Count).ToString());
        //}
        //catch (Exception e)
        //{
        //    log("\n error: " + e.ToString());
        //}

        int mat_cnt = -1;
        try
        {
            mat_cnt = mat[0].Count;
        }
        catch
        {
            mat_cnt = mat[0].Count;
        }
        for (int i = 0; i < mat_cnt; i++)
        {
            if (i == 0)
            { log("\n 1CB" + i.ToString() + "A"); }
            mat_t.Add(new List<float>());
            if (i == 0)
            { log("\n 1CB" + i.ToString() + "B"); }
            for (int j = 0; j < mat.Count; j++)
            {
                if (i == 0)
                { log("\n 1CB" + i.ToString() + j.ToString() + "C"); }
                mat_t[i].Add(mat[j][i]);
                if (i == 0)
                { log("\n 1CB" + i.ToString() + j.ToString() + "D"); }
            }
        }
        log("\n 1CC");
        return mat_t;
    }

    /// <summary>
    /// Mirrors a matrix along one index.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <param name="idx">i or j.</param>
    /// <returns>Mirrored matrix.</returns>
    public List<List<float>> mirror_mat(List<List<float>> mat, string idx = "i")
    {
        List<List<float>> mat_t = new List<List<float>>();

        int mat_cnt = -1;
        try
        {
            mat_cnt = mat[0].Count;
        }
        catch
        {
            mat_cnt = mat[0].Count;
        }
        for (int i = 0; i < mat_cnt; i++)
        {
            mat_t.Add(new List<float>());
            for (int j = 0; j < mat.Count; j++)
            {
                if (idx == "i")
                {
                    mat_t[i].Add(mat[i][mat_cnt - j - 1]);
                }
                else
                {
                    mat_t[i].Add(mat[mat_cnt - i - 1][j]);
                }
            }
        }
        return mat_t;
    }

    /// <summary>
    /// Negates all entries of a matrix in place.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <returns>The negated matrix.</returns>
    public List<List<float>> invert_sign_of_mat(List<List<float>> mat)
    {
        for (int i_idx = 0; i_idx < mat.Count; i_idx++)
        {
            for (int j_idx = 0; j_idx < mat[0].Count; j_idx++)
            {
                mat[i_idx][j_idx] = -mat[i_idx][j_idx];
            }
        }
        return mat;
    }
    /// <summary>
    /// Replaces the outer frame of a matrix by values of the neighbouring interior (removes border artefacts).
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <param name="padding">Width of the frame in pixels.</param>
    /// <returns>Smoothed matrix.</returns>
    public List<List<float>> smoothen_frame(List<List<float>> mat, int padding = 10)
    {
        List<List<float>> mat_new = copy_mat(mat);

        // info (paul): make sure, this outer 5 pixel or so frame is kind of similar to the pixels next to it
        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < mat[0].Count; j++)
            {
                bool cond_1 = i < padding;
                bool cond_2 = i > mat.Count - padding;
                bool cond_3 = j < padding;
                bool cond_4 = j > mat.Count - padding;
                bool cond_all = cond_1 || cond_2 || cond_3 || cond_4;

                if (cond_all)
                {
                    mat_new[i][j] = mat_new[padding + padding][padding + padding];
                }
            }
        }

        return mat_new;
    }
    /// <summary>
    /// Creates the display surface of a map (flat plane or relief from a height map) with the map as texture.
    /// </summary>
    /// <param name="heights">Height map.</param>
    /// <param name="points">Grid points of the surface.</param>
    /// <param name="flow_tex">Texture of the map.</param>
    /// <param name="force_flat">True for a flat plane.</param>
    /// <param name="scale_factor">Height scale (-1 = automatic).</param>
    /// <param name="with_save">True to save the texture.</param>
    /// <returns>Tuple (surface object, grid points).</returns>
    public (GameObject, List<List<(float, float)>>) make_platine_plane(
        List<List<float>> heights, List<List<(float, float)>> points, Texture2D flow_tex,
        bool force_flat = false, float scale_factor = -1f, bool with_save = false)
    {
        Mesh mesh;
        (mesh, _) = make_mesh(heights, force_flat: force_flat, scale_factor: scale_factor);
        (GameObject crossing_obj, int res_x, int res_y) = (null, -1, -1);

        (crossing_obj, points, res_x, res_y) = make_plane_with_mesh(mesh, points,
            flow_tex, with_save: with_save);
        (this.res_x, this.res_y) = (res_x, res_y);

        return (crossing_obj, points);
    }



    /// <summary>
    /// Creates the display object from a mesh and a map texture.
    /// </summary>
    /// <param name="mesh">Surface mesh.</param>
    /// <param name="points">Grid points.</param>
    /// <param name="flow_tex">Texture of the map.</param>
    /// <param name="with_save">True to save the texture.</param>
    /// <returns>Tuple (object, grid points, width, height).</returns>
    public (GameObject, List<List<(float, float)>>, int, int) make_plane_with_mesh(Mesh mesh,
        List<List<(float, float)>> points, Texture2D flow_tex,
        bool with_save = false)
    {
        Material province_mat;
        (province_mat, points, res_x, res_y) = manage_material(
            points, flow_tex: flow_tex, with_save: with_save);

        //20092026 "platine_2" ist in der Szene deaktiviert -> GameObject.Find liefert null
        //(NullReferenceException beim value/loss-Button). Nur zuweisen, wenn vorhanden.
        GameObject plane2 = GameObject.Find("platine_2");
        if (plane2 != null && plane2.GetComponent<MeshRenderer>() != null)
            plane2.GetComponent<MeshRenderer>().material = province_mat;

        // info (paul): assign material
        GameObject surface_obj = setup_surface_obj(mesh);
        surface_obj.GetComponent<MeshRenderer>().material = province_mat;

        string flow_mat_name = "Targets/mat_1";
        Material mat_l = (Material)(Resources.Load(flow_mat_name));
        surface_obj.GetComponent<MeshRenderer>().material = mat_l;

        Material mat_ll = new Material(Shader.Find("HDRP/Lit"));
        //mat_ll.SetTexture("_MainTex", mat_l.mainTexture);
        mat_ll.mainTexture = mat_l.mainTexture;
        //mat_ll.shader.
        mat_ll.EnableKeyword("_EMISSION");
        //mat_ll.SetKeyword(UnityEngine.Rendering.GlobalKeyword.Create("_UseEmissiveIntensity"), true);
        //mat_ll.EnableKeyword("_UseEmissiveIntensity");
        //mat_ll.shader.key
        //mat_ll.shader.SetKeyword(UnityEngine.Rendering.GlobalKeyword.Create("_UseEmissiveIntensity"), true);
        //mat_ll.SetKeyword(mat_ll.Key, mat_l.mainTexture);

        //surface_obj.GetComponent<MeshRenderer>().material.color = Color.white;
        //surface_obj.GetComponent<MeshRenderer>().sharedMaterial = mat_ll;

        return (surface_obj, points, res_x, res_y);
    }
    //
    /// <summary>
    /// Creates a game object with mesh filter, renderer, and collider for a mesh.
    /// </summary>
    /// <param name="mesh">Mesh.</param>
    /// <param name="obj_name">Object name.</param>
    /// <returns>The object.</returns>
    public GameObject setup_surface_obj(Mesh mesh, string obj_name = "platine_plane")
    {
        // info (paul): setup the object, to which the mesh
        //      will be applied, which is supposed to reconstruct 
        //      the object from the experiment

        GameObject surface_obj = new GameObject();
        surface_obj.name = obj_name;
        surface_obj.transform.parent = null;
        surface_obj.AddComponent<MeshFilter>();
        surface_obj.GetComponent<MeshFilter>().mesh = mesh;
        surface_obj.AddComponent<MeshRenderer>();
        surface_obj.transform.position = new UnityEngine.Vector3(0.0f, 0f,
                0.0f);

        if (surface_obj.GetComponent<MeshRenderer>() == null)
        {
            surface_obj.AddComponent<MeshRenderer>();
        }
        return surface_obj;
    }

    /// <summary>
    /// Creates a synthetic test matrix.
    /// </summary>
    /// <param name="len_x">Number of columns.</param>
    /// <param name="len_y">Number of rows.</param>
    /// <returns>Test matrix.</returns>
    public List<List<float>> init_test_mat(int len_x, int len_y)
    {
        List<List<float>> mat_test = zeros_of_size(len_x, len_y);

        for (int i = 0; i < len_x; i++)
        {
            for (int j = 0; j < len_y; j++)
            {
                //mat_test[i][j] = ((float)(i + j)) / ((float)(len_x + len_y));// j % 2;//((float)(i + j))/((float)(len_x+len_y));
                mat_test[i][j] = j % 2;
            }
        }

        return mat_test;
    }

    string speckle_file;//21102024 "speckle_5.000";//25092024 "speckle_5";//11072024 "speckle_pattern";
    Texture2D experimentalStatisticsSpeckleTexture;
    /// <summary>
    /// Sets the speckle material of the sample.
    /// </summary>
    /// <param name="input">Material name (e.g. speckle_0.070).</param>
    public void set_speckle_file(string input)
    {
        this.speckle_file = input;
    }
    /// <summary>
    /// Returns the speckle material of the sample.
    /// </summary>
    /// <returns>Material name.</returns>
    public string get_speckle_file()
    {
        return this.speckle_file;
    }

    /// <summary>
    /// Applies the speckle pattern to a sample: material series, procedural texture (p&lt;s&gt;), or the experiment-derived texture of the Nakajima look.
    /// </summary>
    /// <param name="obj">Sample object.</param>
    /// <param name="file_name">Name of the pattern (legacy).</param>
    public void apply_speckles(GameObject obj, string file_name = "speckle_pattern")
    {
        //23092024 string mat_file = "Targets/fbx_files/Materials/" + remove_dots(get_speckle_file());
        string mat_file = "Targets/fbx_files/Materials/" + get_speckle_file();
        Material speckle_mat = Resources.Load<Material>(mat_file);

        // config_now used to start with diameter = NaN until the experiment dialog
        // was confirmed. That produced a lookup for "speckle_NaN" and assigned a
        // missing material to the specimen, which Unity rendered pink.
        if (speckle_mat == null)
        {
            const string fallback_mat_file = "Targets/fbx_files/Materials/speckle_pattern";
            Debug.LogWarning("Speckle material not found at Resources/" + mat_file
                + ". Using Resources/" + fallback_mat_file + " instead.");
            speckle_mat = Resources.Load<Material>(fallback_mat_file);
        }

        if (speckle_mat == null)
        {
            Debug.LogError("No speckle material could be loaded for " + obj.name + ".");
            return;
        }

        obj.GetComponent<Renderer>().material = speckle_mat;

        //29092026 prozedurale Speckle-Textur (nur im Speckle-Sweep, Stufen "p<s>"): nahtlos, beliebig fein
        if (speckle_sweep_running && !float.IsNaN(procedural_speckle_s))
        {
            Texture2D proc = load_procedural_speckle(procedural_speckle_s);
            Material m = obj.GetComponent<Renderer>().material;
            if (proc != null && m != null)
            {
                foreach (string prop in new[] { "_BaseColorMap", "_MainTex" })
                    if (m.HasProperty(prop))
                    {
                        m.SetTexture(prop, proc);
                        m.SetTextureScale(prop, Vector2.one);
                        m.SetTextureOffset(prop, Vector2.zero);
                    }
                return;
            }
        }

        //21092026 Render-Look "Nakajima": dieselbe gemessene Speckle-Textur und dasselbe
        //Tiling wie bei "Realbild: Neu" (capture_nakajima_comparison_image), damit die
        //TV-Bilder zum Realbild-Vergleich passen. Wirkt auf die Material-Instanz des Objekts.
        //29092026 Nutzerwunsch: Speckle-Groessenanalyse mit den synthetischen Kreismustern (Materialien
        //  speckle_<d>) - im Speckle-Sweep die Textur daher NICHT durch die realistische ersetzen, sonst haetten
        //  alle Stufen dasselbe Muster. Geometrie/Kameras/Licht des Nakajima-Looks bleiben unveraendert.
        if (nakajima_look && category != "muc" && !speckle_sweep_running)
            apply_measured_speckle_texture(obj.GetComponent<Renderer>().material, new Vector2(2.5f, 2.5f));
    }

    //27092026 Speckle-Muster fuer den Nakajima-Look waehlbar (PlayerPrefs "speckle_texture_mode"):
    //  0 = gemessene Statistik (Original, spiegelsymmetrisch), 1 = zufaellig mit gleicher Statistik,
    //  2 = zufaellig kontrastreich. Die Varianten erzeugt scripts/make_speckle_textures.py in Assets/cam00.
    static readonly string[] speckle_texture_files = { "experimental_speckle_synthetic.png",
        "speckle_random_same.png", "speckle_random_highcontrast.png" };
    static readonly string[] speckle_texture_labels = { "gemessen (gespiegelt)", "zufaellig", "zufaellig kontrastreich" };
    private int speckle_texture_mode = 0;
    private int loaded_speckle_texture_mode = -1;
    /// <summary>
    /// Returns the selected speckle texture of the Nakajima look.
    /// </summary>
    /// <returns>Index of the texture.</returns>
    public int get_speckle_texture_mode() { return speckle_texture_mode; }
    /// <summary>
    /// Name of the selected speckle texture.
    /// </summary>
    /// <returns>Description text.</returns>
    public string describe_speckle_texture() { return speckle_texture_labels[speckle_texture_mode]; }
    /// <summary>
    /// Selects and stores the speckle texture of the Nakajima look (cyclic).
    /// </summary>
    /// <param name="mode">Index of the texture.</param>
    public void set_speckle_texture_mode(int mode)
    {
        speckle_texture_mode = ((mode % speckle_texture_files.Length) + speckle_texture_files.Length) % speckle_texture_files.Length;
        PlayerPrefs.SetInt("speckle_texture_mode", speckle_texture_mode);
        PlayerPrefs.Save();
        Debug.Log("Speckle-Muster: " + describe_speckle_texture());
    }

    //29092026 prozedurale Speckle-Texturen (Assets/cam00/procedural/proc_speckle_s<s>.png), mit Mipmaps und
    //  trilinearer/anisotroper Filterung, damit sehr feine Muster im Bild korrekt zu Grau mitteln
    float procedural_speckle_s = float.NaN;
    readonly Dictionary<string, Texture2D> procedural_speckle_cache = new Dictionary<string, Texture2D>();
    /// <summary>
    /// Label of a procedural speckle diameter (dot replaced by p).
    /// </summary>
    /// <param name="s">Diameter in texture pixels.</param>
    /// <returns>Label text.</returns>
    public static string procedural_speckle_label(float s)
    {
        return s.ToString("G6", CultureInfo.InvariantCulture).Replace('.', 'p');
    }
    /// <summary>
    /// Path of the procedural speckle texture of a diameter (Assets/cam00/procedural).
    /// </summary>
    /// <param name="s">Diameter in texture pixels.</param>
    /// <returns>File path.</returns>
    public static string procedural_speckle_path(float s)
    {
        return Path.Combine(UnityEngine.Application.dataPath, "cam00", "procedural",
            "proc_speckle_s" + procedural_speckle_label(s) + ".png");
    }
    /// <summary>
    /// Loads a procedural speckle texture (cached).
    /// </summary>
    /// <param name="s">Diameter in texture pixels.</param>
    /// <returns>Texture, or null if the file is missing.</returns>
    private Texture2D load_procedural_speckle(float s)
    {
        string path = procedural_speckle_path(s);
        if (procedural_speckle_cache.TryGetValue(path, out Texture2D cached) && cached != null)
            return cached;
        if (!File.Exists(path))
        {
            Debug.LogWarning("Prozedurale Speckle-Textur fehlt: " + path + " (scripts/make_procedural_speckles.py)");
            return null;
        }
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, true);
        if (!tex.LoadImage(File.ReadAllBytes(path), false))
        {
            Destroy(tex);
            Debug.LogWarning("Prozedurale Speckle-Textur nicht lesbar: " + path);
            return null;
        }
        tex.name = Path.GetFileNameWithoutExtension(path);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;
        procedural_speckle_cache[path] = tex;
        Debug.Log("Prozedurale Speckle-Textur geladen: " + path + " (" + tex.width + " px, " + tex.mipmapCount + " Mipmaps)");
        return tex;
    }

    /// <summary>
    /// Loads the selected experiment-derived speckle texture (cached until the selection changes).
    /// </summary>
    /// <returns>Texture.</returns>
    private Texture2D load_experimental_statistics_speckle()
    {
        if (experimentalStatisticsSpeckleTexture != null && loaded_speckle_texture_mode == speckle_texture_mode)
            return experimentalStatisticsSpeckleTexture;
        experimentalStatisticsSpeckleTexture = null; //27092026 anderes Muster gewaehlt -> neu laden

        string path = Path.Combine(UnityEngine.Application.dataPath, "cam00",
            speckle_texture_files[speckle_texture_mode]); //27092026 vorher fest "experimental_speckle_synthetic.png"
        if (!File.Exists(path))
        {
            Debug.LogWarning("Experimental-statistics speckle texture is missing: " + path);
            return null;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (!texture.LoadImage(File.ReadAllBytes(path), false))
        {
            Destroy(texture);
            Debug.LogWarning("Could not load experimental-statistics speckle texture: " + path);
            return null;
        }
        texture.name = Path.GetFileNameWithoutExtension(path);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        experimentalStatisticsSpeckleTexture = texture;
        loaded_speckle_texture_mode = speckle_texture_mode;
        Debug.Log("Speckle-Textur geladen: " + path);
        return texture;
    }


    /// <summary>
    /// Reference depth maps of the current frame (legacy height display).
    /// </summary>
    /// <returns>Tuple of reference maps.</returns>
    public (List<List<float>>, List<List<float>>) load_heights_ref()
    {
        List<List<float>> mat_u;

        // info (paul): load camera values
        //18032025 int blade_idx = blade_idxs.Count - 1;
        int blade_idx = get_t_idx();//get_blade_idx();
        float depth_min = load_float_for_blade(cam_idx: 0, blade_idx: blade_idx,
            label: "_depth_min", with_uv_mode: false);
        float depth_max = load_float_for_blade(cam_idx: 0, blade_idx: blade_idx,
            label: "_depth_max", with_uv_mode: false);

        mat_u = load_floats_list_2_for_blade(cam_idx: 0, blade_idx: blade_idx,
            label: "_depth_mat", with_uv_mode: false);
        string mode = (ground_truth_from_flow) ? "plus_minus" : "normal";

        mat_u = unnorm_mat(mat_u, depth_min, depth_max, mode: mode);

        mat_u = mat_raw2dists(mat_u); //18032025
        transpose_mat(mat_u);// - seems to be a turn too much

        List<List<float>> mat_cut = cut_off(mat_u);

        return (mat_cut, mat_u);
    }

    /// <summary>
    /// Creates the material of the display surface from the map texture.
    /// </summary>
    /// <param name="points">Grid points.</param>
    /// <param name="flow_tex">Texture of the map.</param>
    /// <param name="with_save">True to save the texture.</param>
    /// <returns>Tuple (material, grid points, width, height).</returns>
    public (Material, List<List<(float, float)>>, int, int) manage_material(
        List<List<(float, float)>> points, Texture2D flow_tex, bool with_save = false)
    {

        //A Texture2D flow_tex = manage_flow(dir_time_flow_u, dir_time_flow_v,
        //A     points, heights_chosen);

        // info (paul): make material
        Material flow_mat = flow_tex2mat(flow_tex);

        // info (paul): We say cam_idx = 0, since cam_idx makes no sense anyway
        save_current_flow(flow_tex, with_save);

        return (flow_mat, points, res_x, res_y);
    }
    /// <summary>
    /// Loads the flow maps of the current experiment and builds the displayed texture (value, reference, or error of the chosen component, strain).
    /// </summary>
    /// <param name="dir_time_flow_u">Folder of the u maps.</param>
    /// <param name="dir_time_flow_v">Folder of the v maps.</param>
    /// <param name="heights">Height map.</param>
    /// <param name="heights_chosen">Displayed height map.</param>
    /// <returns>Tuple (texture, displayed map).</returns>
    public (Texture2D, List<List<float>>) manage_flow(string dir_time_flow_u, string dir_time_flow_v,
        List<List<float>> heights, List<List<float>> heights_chosen)
    {
        // info (paul): load texture from image, if only one image
        string path_time_flow_u = dir_time_flow_u + remove_dots(get_experiment()) +
            "/time_flow_u/";
        string path_time_flow_v = dir_time_flow_v + remove_dots(get_experiment()) +
            "/time_flow_v/";
        Texture2D tex_albedo;

        bool is_dir = path_time_flow_u.EndsWith("/");
        if (!is_dir)
        {
            tex_albedo = new Texture2D(res_x, res_y);
            byte[] im_bytes = File.ReadAllBytes(path_time_flow_u);
            tex_albedo.LoadImage(im_bytes);
        }

        // info (paul): if multiple images loaded, load them all
        List<List<float>> flow_mat_0;
        List<List<float>> flow_mat_1;
        List<List<float>> stream_z;
        List<List<(float, float)>> points_next;

        (flow_mat_0, res_x, res_y, stream_z) = load_and_construct_flow(
            path_time_flow_u, path_time_flow_v, res_x,
            res_y, im_cnt: im_cnt, t_idx: get_t_idx());

        // info (paul): getting the strain from heights_chosen or xy-flow in flow_tex
        List<List<float>> flow_mat = manage_strain(flow_mat_0, heights, stream_z);

        if (this.get_strain_mode() == "strain_rate")
        {
            (flow_mat_1, _, _, _) = load_and_construct_flow(
            path_time_flow_u, path_time_flow_v, res_x,
            res_y, im_cnt: im_cnt, t_idx: get_t_idx() + 1);
            List<List<float>> flow_mat_next = manage_strain(flow_mat_1, heights, stream_z);
            List<List<float>> strain_diff = mat_diff(flow_mat_next, flow_mat);
            flow_mat = strain_diff;

            //17012025 if (this.get_strain_mode() == "strain_rate")
            //17012025 {
            //17012025      flow_mat = strain_diff;
            //17012025 }
        }

        // info (paul): overwrite with chosen height map:
        if (get_paint_with() == "heights")
        {
            List<List<float>> heights_l = norm_mat(heights_chosen, lower_floor: 200f);
            (float loss_heights_min_1, float loss_heights_max_1) = find_min_max(
                heights_l, with_padding: true);
            flow_mat = heights_l;
        }

        // info (paul): smoothen frame; seems currently not really used
        List<List<float>> flow_mat_smoothed = smoothen_frame(flow_mat);
        if (get_plot_or_heights_mode() == "loss_rel" || get_plot_or_heights_mode() == "loss_abs")//03122024 
        {
            flow_mat_smoothed = flow_mat_smoothed;//18112024 log_mat(flow_mat_smoothed);
            flow_mat_smoothed = shift_above_zero(flow_mat_smoothed, offset: 0.1f);
        }

        List<List<float>> flow_mat_normed = norm_mat(flow_mat_smoothed);
        List<List<float>> flow_mat_logged = flow_mat_normed;//log_mat(flow_mat_normed);
        Texture2D flow_tex = mat2tex(flow_mat_logged, with_switch_dims: true);
        return (flow_tex, flow_mat_logged);
    }

    /// <summary>
    /// Collects strain values for the export of the MUC evaluation.
    /// </summary>
    /// <param name="strains_comp">Strain map.</param>
    public void note_strains(List<List<float>> strains_comp)
    {
        // info (paul): function is part of the framework for gom/muc strain saving

        List<float> strains = matrix2list(strains_comp);

        string strain_d_mode = get_strain_d_mode();
        string u_v_mode = get_u_v_mode();

        if (u_v_mode == "u" && strain_d_mode == "y")
        {
            strains = cut_positives(strains);//19032025 remove that later; blend
            strain_xx_muc = strains;
        }
        if (u_v_mode == "u" && strain_d_mode == "x")
        {
            strain_xy_muc = strains;
        }
        if (u_v_mode == "v" && strain_d_mode == "y")
        {
            strain_yx_muc = strains;
        }
        if (u_v_mode == "v" && strain_d_mode == "x")
        {
            strain_yy_muc = strains;
        }

        //if (strain_mode == "y")
        //{
        //    strain_y_muc = strains;
        //}
    }

    /// <summary>
    /// Sets positive entries of a list to zero.
    /// </summary>
    /// <param name="mat">List of values.</param>
    /// <returns>Modified copy.</returns>
    public List<float> cut_positives(List<float> mat)
    {
        List<float> mat_new = zeros_like(mat);

        for (int i = 0; i < mat.Count; i++)
        {
            if (mat[i] > 0)
            {
                mat_new[i] = 0f;
            }
            else
            {
                mat_new[i] = mat[i];
            }
        }

        return mat_new;
    }

    /// <summary>
    /// Checks whether a pixel lies inside the image without a border.
    /// </summary>
    /// <param name="idx_x">Column.</param>
    /// <param name="idx_y">Row.</param>
    /// <param name="padding">Border width.</param>
    /// <returns>True if inside.</returns>
    public bool in_boundaries(int idx_x, int idx_y, int padding = 10)
    {
        bool cond_1 = idx_x > padding;
        bool cond_2 = idx_x < get_render_res() - padding;
        bool cond_3 = idx_y > padding;
        bool cond_4 = idx_y < get_render_res() - padding;

        bool is_in = cond_1 && cond_2 && cond_3 && cond_4;

        return is_in;
    }

    /// <summary>
    /// Writes minor and major strain in a text format for comparison with GOM.
    /// </summary>
    /// <param name="minor">Minor strain.</param>
    /// <param name="major">Major strain.</param>
    /// <param name="title">Output file name.</param>
    public void write_gom(List<float> minor, List<float> major, string title = "gom.txt")
    {
        string path = root_path + title;
        string info_str = "";

        for (int i = 0; i < minor.Count; i++)
        {
            int idx_x = i % get_render_res();
            int idx_y = (i - idx_x) / get_render_res();

            if (idx_x % 4 == 0 && idx_y % 4 == 0)
            {
                if (i >= 1)
                {
                    info_str += "\n";
                }

                // info (paul): id x   y   z    major_strsain                   minor_strain                          v_min       v_maj
                bool in_bounds = in_boundaries(idx_x, idx_y, padding: 20);
                if (in_bounds)
                {
                    info_str += i.ToString() + "\t " + idx_x.ToString() + "\t " + idx_y.ToString()
                        + "\t 0\t" + major[i].ToString() + "\t" + minor[i].ToString() + "\t" + "0" + "\t" + "0";
                }
            }
        }

        write_to_txt(path, info_str, mode: "new");


        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = "python3";

        var script = path_base + "play_blender_pycahrm/write_mesh/txt2csv.py";//23092026 "/Users/Paul/Desktop/DIC_2025_for_travel/play_blender_pycahrm/write_mesh/txt2csv.py";//path_project + "Assets/txt2csv.py";
        psi.Arguments = $"\"{script}\"";

        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        var errors = "";
        var results = "";

        using (var process = Process.Start(psi))
        {
            errors = process.StandardError.ReadToEnd();
            results = process.StandardOutput.ReadToEnd();
        }




        return;


    }

    /// <summary>
    /// Element-wise sum of two matrices.
    /// </summary>
    /// <param name="mat_0">First matrix.</param>
    /// <param name="mat_1">Second matrix.</param>
    /// <returns>Sum.</returns>
    public List<List<float>> mat_sum(List<List<float>> mat_0, List<List<float>> mat_1)
    {
        List<List<float>> sum = zeros_like(mat_0);

        for (int i = 0; i < mat_0.Count; i++)
        {
            for (int j = 0; j < mat_0[0].Count; j++)
            {
                sum[i][j] = mat_0[i][j] + mat_1[i][j];
            }
        }

        return sum;
    }
    /// <summary>
    /// Element-wise difference of two matrices.
    /// </summary>
    /// <param name="mat_0">First matrix.</param>
    /// <param name="mat_1">Second matrix.</param>
    /// <returns>Difference mat_0 - mat_1.</returns>
    public List<List<float>> mat_diff(List<List<float>> mat_0, List<List<float>> mat_1)
    {
        List<List<float>> sum = zeros_like(mat_0);

        for (int i = 0; i < mat_0.Count; i++)
        {
            for (int j = 0; j < mat_0[0].Count; j++)
            {
                sum[i][j] = mat_0[i][j] - mat_1[i][j];
            }
        }

        return sum;
    }
    /// <summary>
    /// Natural logarithm of all entries.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <returns>Logarithmic matrix.</returns>
    public List<List<float>> log_mat(List<List<float>> mat)
    {
        List<List<float>> logged = copy_mat(mat);

        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < mat[0].Count; j++)
            {
                logged[i][j] = Mathf.Log10(mat[i][j]);//0.1f*Mathf.Log(mat[i][j]);
            }
        }

        return logged;
    }
    /// <summary>
    /// Shifts all values so that the minimum becomes zero (plus offset).
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <param name="offset">Additional offset.</param>
    /// <returns>Shifted matrix.</returns>
    public List<List<float>> shift_above_zero(List<List<float>> mat, float offset = 0f)
    {
        // info (paul): this function will shift all values, so that the 
        //      min_val is 0.

        (float min_val, float max_val) = find_min_max(mat, with_padding: true);

        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < mat[0].Count; j++)
            {
                mat[i][j] += -min_val + offset;
            }
        }

        return mat;
    }
    /// <summary>
    /// Creates a material that shows a map texture.
    /// </summary>
    /// <param name="flow_tex">Texture of the map.</param>
    /// <returns>Material.</returns>
    public Material flow_tex2mat(Texture2D flow_tex)
    {
        string flow_mat_name = "Targets/mat_1";
        Material flow_mat = (Material)(Resources.Load(flow_mat_name));

        string[] keywords = flow_mat.shaderKeywords;
        flow_mat.SetTexture("_BaseColorMap", flow_tex);

        //flow_mat.SetTexture("");
        //flow_mat.SetTexture("_MainTex", flow_tex);
        //16022025 flow_mat.SetTexture("_EmissionMap", flow_tex);
        //16022025 flow_mat.EnableKeyword("_EMISSION");
        flow_mat.SetTexture("_EmissiveColorMap", flow_tex);
        //flow_mat.SetTexture("_EmissiveColor", new Color(1f, 1f, 1f, 1f));
        //flow_mat.SetTexture("_EmissionMap", flow_tex);
        //flow_mat.SetColor("_EmissiveColor", Color.green);
        //flow_mat.SetColor("_EmissionColor ", Color.green);
        return flow_mat;
    }

    // info (paul): image noise:
    /// <summary>
    /// Applies the image noise of an experiment (Gaussian and/or Poisson shot noise) to a rendered image.
    /// </summary>
    /// <param name="image">Gray values.</param>
    /// <param name="pars">Parameters of the experiment.</param>
    /// <returns>Noisy image.</returns>
    public List<List<float>> manage_image_noise(List<List<float>> image, Params pars)
    {
        //if (exp_cv_acts[get_reg_idx() - 1].get_label().EndsWith("gauss_3"))
        //{
        //;
        //}

        List<List<float>> image_1 = manage_gauss_noise(image, gaussian_error: pars.get_gaussian_error());
        List<List<float>> image_2 = manage_poisson_noise(image_1,
            peakElectrons: pars.get_poisson_error(), sampleIndex: noise_sample_index++);
        if (get_reg_idx() - 1 == 10)
        {
            write_mat_for_debug(image_2, scale: 1f);
        }
        return image_2;
    }

    /// <summary>
    /// Adds Gaussian noise to an image.
    /// </summary>
    /// <param name="image">Gray values.</param>
    /// <param name="gaussian_error">Standard deviation.</param>
    /// <returns>Noisy image.</returns>
    public List<List<float>> manage_gauss_noise(List<List<float>> image, float gaussian_error = 1f)
    {
        if (gaussian_error > 2f)
        {
            ;
        }

        List<List<float>> image_1 = copy_mat(image);

        for (int i = 0; i < image.Count; i++)
        {
            for (int j = 0; j < image[i].Count; j++)
            {
                float rand = UnityEngine.Random.Range(-1f, 1f);
                image_1[i][j] = image[i][j] + rand * gaussian_error;
            }
        }

        bool with_debug = false;
        if (with_debug)
        {
            write_mat_for_debug(image, scale: 1f);
            write_mat_for_debug(image_1, scale: 1f);
        }
        return image_1;
    }

    // Photon shot noise: K ~ Poisson(N_peak * I), I_noisy = K / N_peak.
    // N_peak is the full-scale electron capacity. Thus the full-scale relative
    // standard deviation is 1/sqrt(N_peak). A value <= 0 disables shot noise.
    /// <summary>
    /// Photon shot noise: K ~ Poisson(N_peak * I), I_noisy = K / N_peak (reproducible per sample index).
    /// </summary>
    /// <param name="image">Gray values in 0..1.</param>
    /// <param name="peakElectrons">Full-scale electron capacity N_peak (0 or less = off).</param>
    /// <param name="sampleIndex">Index for the random seed.</param>
    /// <returns>Noisy image.</returns>
    public List<List<float>> manage_poisson_noise(List<List<float>> image,
        float peakElectrons, int sampleIndex)
    {
        List<List<float>> image_1 = copy_mat(image);
        if (peakElectrons <= 0f || float.IsNaN(peakElectrons))
            return image_1;

        int seed = 216613626;
        string seedText = get_experiment() + ":" + sampleIndex.ToString(CultureInfo.InvariantCulture);
        unchecked
        {
            foreach (char character in seedText)
            {
                seed ^= character;
                seed *= 16777619;
            }
        }
        var random = new System.Random(seed);
        for (int i = 0; i < image.Count; i++)
        for (int j = 0; j < image[i].Count; j++)
        {
            double normalized = Mathf.Clamp01(image[i][j]);
            double lambda = normalized * peakElectrons;
            image_1[i][j] = Mathf.Clamp01((float)(sample_poisson(random, lambda) / peakElectrons));
        }

        return image_1;
    }

    /// <summary>
    /// Draws a Poisson-distributed number (inversion for small mean, normal approximation for large mean).
    /// </summary>
    /// <param name="random">Random generator.</param>
    /// <param name="lambda">Mean.</param>
    /// <returns>Sample.</returns>
    private static int sample_poisson(System.Random random, double lambda)
    {
        if (lambda <= 0d) return 0;
        if (lambda < 30d)
        {
            double limit = Math.Exp(-lambda);
            int count = 0;
            double product = 1d;
            do { count++; product *= random.NextDouble(); } while (product > limit);
            return count - 1;
        }

        // At high photon counts the standardized Poisson distribution converges
        // rapidly to N(0,1); rounding gives an accurate, efficient sensor model.
        double u1 = Math.Max(random.NextDouble(), double.Epsilon);
        double u2 = random.NextDouble();
        double normal = Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
        return Math.Max(0, (int)Math.Round(lambda + Math.Sqrt(lambda) * normal));
    }

    /// <summary>
    /// Saves the displayed map as image (with a label of the display mode).
    /// </summary>
    /// <param name="flow_tex">Displayed texture.</param>
    /// <param name="with_save">True to save.</param>
    public void save_current_flow(Texture2D flow_tex, bool with_save)
    {
        if (with_save)
        {
            string active_mode = find_active_mode();
            string label_l = "_" + paint_with.ToString() + "_" + active_mode.ToString();

            save_tex(flow_tex);
            //20092026 Beim manuellen "save"-Klick (ausserhalb einer laufenden Analyse) ist
            //exp_cv_acts leer/null bzw. reg_idx-1 ungueltig -> NullReferenceException.
            //Dann die aktuellen Parameter (params_now) verwenden.
            Params pars = null;
            int act_idx = get_reg_idx() - 1;
            if (exp_cv_acts != null && act_idx >= 0 && act_idx < exp_cv_acts.Count && exp_cv_acts[act_idx] != null)
                pars = exp_cv_acts[act_idx].pars;
            if (pars == null)
                pars = get_params_now();
            save_png(flow_tex, cam_idx: 0, blade_idx: t_idx, label: label_l, pars: pars);
        }
    }

    /// <summary>
    /// Name of the active display mode (flow, heights, ...).
    /// </summary>
    /// <returns>Mode name.</returns>
    public string find_active_mode()
    {
        string active_mode = null;

        if (paint_with == "uv")
        {
            active_mode = plot_mode;
        }
        if (paint_with == "heights")
        {
            active_mode = heights_mode;
        }

        return active_mode;
    }

    /// <summary>
    /// Multiplies a square matrix by a factor in place.
    /// </summary>
    /// <param name="input">Matrix.</param>
    /// <param name="factor">Factor.</param>
    /// <returns>The matrix.</returns>
    public List<List<float>> multiply(List<List<float>> input, float factor)
    {
        for (int i = 0; i < input.Count; i++)
        {
            for (int j = 0; j < input.Count; j++)
            {
                input[i][j] *= factor;
            }
        }
        return input;
    }

    /// <summary>
    /// Multiplies a list by a factor in place.
    /// </summary>
    /// <param name="input">List.</param>
    /// <param name="factor">Factor.</param>
    /// <returns>The list.</returns>
    public List<float> multiply(List<float> input, float factor)
    {
        for (int i = 0; i < input.Count; i++)
        {
            input[i] *= factor;
        }
        return input;
    }
    /// <summary>
    /// Computes the displayed strain map from the flow according to the strain mode (derivative, strain rate, minor/major strain).
    /// </summary>
    /// <param name="flow_tex">Displacement map.</param>
    /// <param name="heights_chosen">Height map.</param>
    /// <param name="stream_z">Out-of-plane component (if available).</param>
    /// <returns>Strain map.</returns>
    public List<List<float>> manage_strain(List<List<float>> flow_tex,
        List<List<float>> heights_chosen, List<List<float>> stream_z)
    {
        List<List<float>> strain = flow_tex;

        string strain_mode_l = this.get_strain_mode();
        if (strain_mode_l == "derivative_1" || strain_mode_l == "strain_rate")
        {
            if (plot_mode == "value" || plot_mode == "value_ref")
            {
                strain = calc_strain(flow_tex, heights_chosen, stream_z); //16052024 tex_albedo


                (float stream_u_min, float stream_u_max) = find_min_max(strain, with_padding: true);//11112024 
                (float stream_v_min, float stream_v_max) = find_min_max(strain, with_padding: true);//11112024 
                (float stream_u_mean, float dev_u) = find_mean_in_all(strain, span: 20);
                (float stream_v_mean, float dev_v) = find_mean_in_all(strain, span: 20);
                if (get_paint_with() == "uv")
                {
                    update_scale_label_ext(stream_u_mean, dev_u, stream_u_min, stream_u_max,
                        stream_v_mean, dev_v, stream_v_min, stream_v_max);
                    //11112024 update_scale_label(stream_u_mean, dev_u, stream_v_mean, dev_v);
                }
                note_strains(strain);

                bool len_a = (strain_xx_muc.Count > 0);
                bool len_b = (strain_xy_muc.Count > 0);
                bool len_c = (strain_yx_muc.Count > 0);
                bool len_d = (strain_yy_muc.Count > 0);
                bool long_enough = len_a && len_b && len_c && len_d;

                if (long_enough)
                {
                    (List<float> minor_l, List<float> major_l) = minor_major(this.strain_xx_muc,
                        this.strain_xy_muc, this.strain_yx_muc, this.strain_yy_muc);

                    List<List<float>> minor = list2matrix(minor_l, get_render_res(), get_render_res());
                    List<List<float>> major = list2matrix(major_l, get_render_res(), get_render_res());

                    if (get_strain_d_mode() == "x")
                    {
                        strain = strain;//19032025 minor;
                    }

                    if (get_strain_d_mode() == "y")
                    {
                        strain = strain;//19032025 major;
                    }
                }
            }
            if (plot_mode == "loss_abs" || plot_mode == "loss_rel")
            {
                strain = update_label_for_strain(heights_chosen);
            }
        }

        return strain;
    }

    /// <summary>
    /// Writes the value range of the strain map (and its reference) to the scale label.
    /// </summary>
    /// <param name="heights_chosen">Height map.</param>
    /// <returns>The strain map.</returns>
    public List<List<float>> update_label_for_strain(List<List<float>> heights_chosen)
    {
        //List<List<float>> strains = tex2mat(strain, with_switch_dims: false);

        // info (paul): write min, max to label
        // info (paul): get strains_ref
        List<List<float>> strains_ref = (this.get_u_v_mode() == "u") ?
            this.get_ref_u() : this.get_ref_v();
        List<List<float>> strains_ref_minus = multiply(strains_ref, -1f);

        // info (paul): get strains
        List<List<float>> strains_value = (this.get_u_v_mode() == "u") ?
            this.get_u() : this.get_v();

        // info (paul): calc derivatives (from deformation to strain):
        strains_ref_minus = calc_strain(strains_ref, heights_chosen, zeros_like(strains_ref));
        strains_value = calc_strain(strains_value, heights_chosen, zeros_like(strains_ref));

        (List<List<float>> strain_map, float coverage_l) = choose_heights_or_loss(
            strains_value, strains_ref_minus, 1f, threshold: 0.01f, mode: "rel");//13112024 1f, 0.1f, mode: "rel" //0.1f, mode: "abs"
        (float stream_u_min, float stream_u_max) = find_min_max(strain_map, with_padding: true);//11112024 
        (float stream_v_min, float stream_v_max) = find_min_max(strain_map, with_padding: true);//11112024 
        List<List<float>> strain_map_abs = mat_abs(strain_map);
        (float stream_u_mean, float dev_u) = find_mean_in_all(strain_map_abs, span: -1, coverage: 1f,
            padding: strain_map_abs.Count / 2 - 20);//find_mean_in_span(stream_u, span: 20, j_off: 50);
        (float stream_v_mean, float dev_v) = find_mean_in_all(strain_map_abs, span: -1, coverage: 1f,
            padding: strain_map_abs.Count / 2 - 20);//find_mean_in_span(stream_v, span: 20, j_off: 50);
        if (get_paint_with() == "uv")
        {
            //11112024 update_scale_label(stream_u_mean, dev_u, stream_v_mean, dev_v);
            update_scale_label_ext(stream_u_mean, dev_u, stream_u_min, stream_u_max, stream_v_mean,
                dev_v, stream_v_min, stream_v_max);

        }
        return strain_map_abs;
    }
    /// <summary>
    /// Absolute value of all entries.
    /// </summary>
    /// <param name="strain">Matrix.</param>
    /// <returns>Matrix of absolute values.</returns>
    public List<List<float>> mat_abs(List<List<float>> strain)
    {
        List<List<float>> strain_abs = copy_mat(strain);

        for (int i = 0; i < strain.Count; i++)
        {
            for (int j = 0; j < strain[0].Count; j++)
            {
                strain_abs[i][j] = Mathf.Abs(strain[i][j]);
            }
        }

        return strain_abs;
    }
    /// <summary>
    /// Debugging helper: overwrites a texture with a synthetic pattern.
    /// </summary>
    /// <param name="tex">Texture.</param>
    /// <returns>Modified texture.</returns>
    public Texture2D overwrite_tex(Texture2D tex)
    {
        Color[] cols = new Color[tex.width * tex.height];
        for (int i = 0; i < tex.width; i++)
        {
            for (int j = 0; j < tex.height; j++)
            {
                float col_x = (float)(i % 2);
                float col_y = 0f;// (float)(j%tex.height);
                cols[i * tex.height + j] = new Color(0f, 0.7f, 0f, 1f);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();

        return tex;
    }

    /// <summary>
    /// Loads the flow maps of a folder or file pair and builds the accumulated displacement relative to the first frame.
    /// </summary>
    /// <param name="path_time_flow_u">Folder or file of the u maps.</param>
    /// <param name="path_time_flow_v">Folder or file of the v maps.</param>
    /// <param name="res_x">Width.</param>
    /// <param name="res_y">Height.</param>
    /// <param name="im_cnt">Number of images (-1 = all).</param>
    /// <param name="t_idx">Time index.</param>
    /// <param name="from_path">True to load stored data.</param>
    /// <returns>Tuple (chosen map, width, height, out-of-plane component).</returns>
    public (List<List<float>>, int, int, List<List<float>>) load_and_construct_flow(
        string path_time_flow_u, string path_time_flow_v,
        int res_x, int res_y, int im_cnt = -1, int t_idx = -1, bool from_path = false)
    {
        List<List<float>> flow_mat = null;// = new Texture2D(res_x, res_y);
        List<List<float>> stream_z = null;

        bool is_dir = path_time_flow_u.EndsWith("/");
        if (is_dir)
        {
            DirectoryInfo dir_u = new DirectoryInfo(path_time_flow_u);
            FileInfo[] dir_info_u = dir_u.GetFiles("*.*");

            DirectoryInfo dir_v = new DirectoryInfo(path_time_flow_v);
            FileInfo[] dir_info_v = dir_v.GetFiles("*.*");

            if (with_match_tex) // info (paul): This is supposed to be always true, even if you don't do the long-range matching
            {
                (flow_mat, res_x, res_y, stream_z) = find_total_match_tex(dir_info_u,
                    dir_info_v, im_cnt: im_cnt, t_idx: t_idx, from_path: from_path);
            }
        }

        return (flow_mat, res_x, res_y, stream_z);
    }

    /// <summary>
    /// Assigns the flow image files (names like time_flow_u_&lt;t&gt;.png) to their time indices.
    /// </summary>
    /// <param name="dir_info_u">Files of the u folder.</param>
    /// <param name="dir_info_v">Files of the v folder.</param>
    /// <returns>Dictionary component -&gt; (time index -&gt; file).</returns>
    public Dictionary<string, Dictionary<int, FileInfo>> select_flow_files(FileInfo[] dir_info_u,
        FileInfo[] dir_info_v)
    {
        // info (paul): We assume, that the datafile is 
        //      has a name like "time_flow_u_172.png"

        List<FileInfo> flow_files = new List<FileInfo>();
        Dictionary<string, Dictionary<int, FileInfo>> flow_dic = new Dictionary<string, Dictionary<int, FileInfo>>();
        flow_dic.Add("u", new Dictionary<int, FileInfo>());
        flow_dic.Add("v", new Dictionary<int, FileInfo>());

        //20092026 Im time_flow-Ordner liegen Dateien mehrerer Aufloesungen/Laeufe
        //(z.B. _r128, _r512, _r1600, ohne Suffix). Nur die der aktuellen Renderaufloesung
        //verwenden, sonst mischen sich alte Ergebnisse in die Anzeige. Gibt es keine
        //passenden Dateien, bleibt das alte Verhalten (alle Dateien) erhalten.
        string res_suffix = "_r" + get_render_res().ToString() + ".png";
        bool has_res_u = dir_info_u.Any(f => f.Name.StartsWith("time_flow_u_") && f.Name.EndsWith(res_suffix));
        bool has_res_v = dir_info_v.Any(f => f.Name.StartsWith("time_flow_v_") && f.Name.EndsWith(res_suffix));
        if (has_res_u && has_res_v)
        {
            dir_info_u = dir_info_u.Where(f => f.Name.EndsWith(res_suffix)).ToArray();
            dir_info_v = dir_info_v.Where(f => f.Name.EndsWith(res_suffix)).ToArray();
        }

        for (int i = 0; i < dir_info_u.Length; i++)
        {
            FileInfo info = dir_info_u[i];
            bool is_flow_u = info.Name.StartsWith("time_flow_u_");

            if (is_flow_u)
            {
                string time_str = Regex.Match(info.Name, @"\d+").Value;
                int time_idx = int.Parse(time_str);

                //flow_files.Add(info);
                if (!flow_dic["u"].ContainsKey(time_idx))
                {
                    flow_dic["u"].Add(time_idx, info);
                }
            }
        }
        for (int i = 0; i < dir_info_v.Length; i++)
        {
            FileInfo info = dir_info_v[i];
            bool is_flow_v = info.Name.StartsWith("time_flow_v_");

            if (is_flow_v)
            {
                string time_str = Regex.Match(info.Name, @"\d+").Value;
                int time_idx = int.Parse(time_str);
                if (!flow_dic["v"].ContainsKey(time_idx))
                {
                    flow_dic["v"].Add(time_idx, info);
                }
            }
        }

        return flow_dic;
    }

    /// <summary>
    /// Splits a point matrix into x and y components.
    /// </summary>
    /// <param name="points">Matrix of (x, y) tuples.</param>
    /// <returns>Tuple (x map, y map).</returns>
    public (List<List<float>>, List<List<float>>) points2flow(List<List<(float, float)>> points)
    {
        List<List<float>> flow_x = new List<List<float>>();
        List<List<float>> flow_y = new List<List<float>>();

        flow_x = zeros_like(points, return_type: "float");
        flow_y = zeros_like(points, return_type: "float");

        for (int i = 0; i < points.Count; i++)
        {
            for (int j = 0; j < points[i].Count; j++)
            {
                flow_x[i][j] = points[i][j].Item1;
                flow_y[i][j] = points[i][j].Item2;
            }
        }

        return (flow_x, flow_y);
    }

    /// <summary>
    /// Subtracts the mean of both components from a point matrix.
    /// </summary>
    /// <param name="points_pos">Matrix of (x, y) tuples.</param>
    /// <returns>Centred matrix.</returns>
    public List<List<(float, float)>> subtract_mean(List<List<(float, float)>> points_pos)
    {
        // info (paul): getting mean
        float sum_1 = 0f;
        float sum_2 = 0f;

        int len_x = points_pos.Count;
        int len_y = points_pos[0].Count;
        for (int i = 0; i < len_x; i++)
        {
            for (int j = 0; j < len_y; j++)
            {
                sum_1 += points_pos[i][j].Item1;
                sum_2 += points_pos[i][j].Item2;
            }
        }

        float cnt_float = (float)(len_x * len_y);
        float mean_1 = sum_1 / cnt_float;
        float mean_2 = sum_2 / cnt_float;

        // info (paul): subtracting mean
        for (int i = 0; i < len_x; i++)
        {
            for (int j = 0; j < len_y; j++)
            {
                float item1 = points_pos[i][j].Item1;
                float item2 = points_pos[i][j].Item2;

                points_pos[i][j] = (item1 - mean_1, item2 - mean_2);
            }
        }
        return points_pos;
    }

    /// <summary>
    /// Checks whether a value is finite and non-zero.
    /// </summary>
    /// <param name="value">Value.</param>
    /// <returns>True if useful.</returns>
    public bool is_useful(float value)
    {
        bool is_useful = (value != 0f && !float.IsNaN(value) && !float.IsInfinity(value));
        return is_useful;
    }
    /// <summary>
    /// Checks whether a value is finite.
    /// </summary>
    /// <param name="value">Value.</param>
    /// <returns>True if not NaN or infinite.</returns>
    public bool is_meaningful(float value)
    {
        bool meaningful = (!float.IsNaN(value) && !float.IsInfinity(value));
        return meaningful;
    }
    /// <summary>
    /// Subtracts the grid position (i, j) from tracked point positions (gives the displacement).
    /// </summary>
    /// <param name="points_pos">Matrix of positions.</param>
    /// <returns>Displacement matrix.</returns>
    public List<List<(float, float)>> subtract_grid_pos(List<List<(float, float)>> points_pos)
    {
        for (int i = 0; i < points_pos.Count; i++)
        {
            for (int j = 0; j < points_pos[i].Count; j++)
            {
                float val_x = points_pos[i][j].Item1;
                float val_y = points_pos[i][j].Item2;
                float adjusted_x = val_x - (float)i;
                float adjusted_y = val_y - (float)j;
                points_pos[i][j] = (adjusted_x, adjusted_y);
                if (i == 10 && j == 350)
                {
                    ;
                }
                if (is_useful(adjusted_x))
                {
                    ;
                }
                if (is_useful(adjusted_y))
                {
                    ;
                }
            }
        }

        return points_pos;
    }

    /// <summary>
    /// Loads all flow files, accumulates them to the start frame, and compares with the reference.
    /// </summary>
    /// <param name="dir_info_u">Files of the u folder.</param>
    /// <param name="dir_info_v">Files of the v folder.</param>
    /// <param name="im_cnt">Number of images (-1 = all).</param>
    /// <param name="t_idx">Time index.</param>
    /// <param name="from_path">True to load stored data.</param>
    /// <returns>Tuple (chosen map, width, height, out-of-plane component).</returns>
    public (List<List<float>>, int, int, List<List<float>>) find_total_match_tex(
        FileInfo[] dir_info_u, FileInfo[] dir_info_v,
        int im_cnt = -1, int t_idx = -1, bool from_path = false)
    {
        // info (paul): get flow files
        Dictionary<string, Dictionary<int, FileInfo>> flow_files = select_flow_files(
            dir_info_u, dir_info_v);

        // info (paul): load files
        if (flow_files != null)
        {
            im_cnt = flow_files["u"].Count;
        }

        (List<List<List<float>>> flow_mats_u, List<List<List<float>>> flow_mats_v,
            List<Texture2D> texs_albedo_u, List<Texture2D> texs_albedo_v,
            int res_x, int res_y) = (null, null, null, null, -1, -1);

        //23092026 Innerhalb eines Refresh-Durchgangs (save/Genauigkeit) sind die geladenen und
        //  aufsummierten Flussfelder fuer alle Modi/Komponenten gleich -> nur einmal laden.
        List<List<float>> accum_u = null, accum_v = null;
        bool use_batch = refresh_batch_depth > 0 && !from_path;
        if (use_batch && batch_flow != null && batch_flow.Value.t_idx == t_idx)
        {
            (accum_u, accum_v, texs_albedo_u, texs_albedo_v, res_x, res_y) =
                (batch_flow.Value.u, batch_flow.Value.v, batch_flow.Value.texs_u, batch_flow.Value.texs_v,
                batch_flow.Value.res_x, batch_flow.Value.res_y);
        }
        else
        {
            (flow_mats_u, flow_mats_v, texs_albedo_u,
                texs_albedo_v, res_x, res_y) = find_flow_mats(
                flow_files, im_cnt: im_cnt);// info (paul): somewhat performance heavy

            // info (paul): scale flow mats for debugging reasons
            (flow_mats_u, flow_mats_v) = scale_flows(flow_mats_u, flow_mats_v,
                scale_fac: this.flow_scale_factor);

            if (use_batch)
            {
                (accum_u, accum_v) = find_accum_flow(flow_mats_u, flow_mats_v, t_idx);
                batch_flow = (t_idx, accum_u, accum_v, texs_albedo_u, texs_albedo_v, res_x, res_y);
            }
        }

        (List<List<float>> flow_mat, List<List<float>> stream_z) =
            manage_match_to_start(flow_mats_u, flow_mats_v, t_idx,
            texs_albedo_u, texs_albedo_v, from_path: from_path, accum_u: accum_u, accum_v: accum_v);

        //tex = texs_albedo_u[0];

        // info (paul): average to smoothen
        // 17032025ABC
        List<List<float>> flow_mat_smoothed = smoothen_frame(flow_mat);
        List<List<float>> flow_mat_mean = filter_mean_comp(flow_mat_smoothed);

        return (flow_mat_mean, res_x, res_y, stream_z);
    }

    /// <summary>
    /// Multiplies all flow maps by a factor.
    /// </summary>
    /// <param name="flow_mats_u">u maps per time step.</param>
    /// <param name="flow_mats_v">v maps per time step.</param>
    /// <param name="scale_fac">Factor.</param>
    /// <returns>Tuple of scaled maps.</returns>
    public (List<List<List<float>>>, List<List<List<float>>>) scale_flows(List<List<List<float>>> flow_mats_u,
        List<List<List<float>>> flow_mats_v, float scale_fac)
    {
        int len_t = flow_mats_u.Count;
        int len_x = flow_mats_u[0].Count;
        int len_y = flow_mats_u[0][0].Count;

        for (int t_idx = 0; t_idx < len_t; t_idx++)
        {
            for (int i = 0; i < len_x; i++)
            {
                for (int j = 0; j < len_y; j++)
                {
                    flow_mats_u[t_idx][i][j] = flow_mats_u[t_idx][i][j] * scale_fac;
                    flow_mats_v[t_idx][i][j] = flow_mats_v[t_idx][i][j] * scale_fac;
                }
            }
        }
        return (flow_mats_u, flow_mats_v);
    }

    /// <summary>
    /// Loads an externally computed flow map (CSV, exp_normal) for comparison.
    /// </summary>
    /// <returns>Matrix.</returns>
    public List<List<float>> manage_load_csv()
    {
        string path = path_dic + "exp_normal/time_flow_v/flow_v_csv.csv";
        //15112024 string path = "C:/Users/go73jem/Desktop/DIC_package/exp_normal/time_flow_v/flow_v_csv_28_29.csv";

        List<List<float>> mat_val = read_dists(path, direct_access: true);

        return mat_val;
    }
    /// <summary>
    /// Reference image-plane displacement between two samples (wrapper of find_truth_flow).
    /// </summary>
    /// <param name="sample_1">First sample.</param>
    /// <param name="sample_2">Second sample.</param>
    /// <param name="cam">Camera.</param>
    /// <returns>Tuple (u, v) reference maps.</returns>
    public (List<List<float>>, List<List<float>>) manage_truth_flow(GameObject sample_1, GameObject sample_2, Camera cam)
    {
        (List<List<float>> stream_u, List<List<float>> stream_v) = find_truth_flow(sample_1, sample_2, cam);
        return (stream_u, stream_v);
    }
    /// <summary>
    /// Computes the per-pixel reference displacement between two samples from the vertex displacements and the barycentric pixel assignment.
    /// </summary>
    /// <param name="blade_1">First sample.</param>
    /// <param name="blade_2">Second sample.</param>
    /// <param name="cam">Camera.</param>
    /// <returns>Tuple (u, v) reference maps.</returns>
    public (List<List<float>>, List<List<float>>) find_truth_flow(GameObject blade_1, GameObject blade_2, Camera cam)
    {
        // info (paul): find uv flow from start
        
        (float[] d_xs, float[] d_ys, float[] d_zs) = construct_distortions(blade_1, blade_2,
            cam: cam, with_proj: true);//0; i//24092024 1 oder so
        //(int[,] tri_idx, float[][][] barys) = load_tris(blade_idx: 0);//Bt_idx);
        (int[,] tri_idx, float[][][] barys) = im2triangles(cam: cam);

        Mesh current_mesh = blade_1.GetComponent<MeshFilter>().mesh;
        int[] tris = current_mesh.triangles;
        //26072025 Mesh current_mesh = blade_1.GetComponent<MeshFilter>().mesh;
        //26072025 int[] tris = current_mesh.triangles;



        int i_min = 0;
        int i_max = get_render_res();
        int j_min = 0;
        int j_max = get_render_res();

        List<List<float>> stream_u = zeros_of_size(get_render_res(), get_render_res());
        List<List<float>> stream_v = zeros_of_size(get_render_res(), get_render_res());
        List<List<float>> stream_z = zeros_of_size(get_render_res(), get_render_res());

        for (int i = i_min; i < i_max; i++)//17072024 stream_u[0].Count; i++)
        {
            for (int j = j_min; j < j_max; j++)//17072024 stream_u.Count; j++)
            {
                // info (paul): If a triangle was found, which is close to the pixel, then load and assign 
                //      the corresponding flow values

                //22092026 Bugfix: Die TV-Flussmatrizen werden beim Laden transponiert
                //(tex2mat mit with_switch_dims = true): stream[i][j] = Pixel (x = i, y = j).
                //Die Maske aus im2triangles ist tris[y, x]. Die alte Abfrage
                //tri_idx[i, res-1-j] (= Pixel x = res-1-j, y = i) war dazu transponiert und
                //gespiegelt: Ground Truth und Umriss standen um 90 Grad zum TV-Fluss.
                //24092026 Bugfix: die Spiegelung war aber noetig - nur auf der anderen Achse. Die Maske
                //  (im2triangles) zaehlt Bildschirm-y von unten, die TV-Matrix die Zeilen von oben
                //  (an r1024, 28->29 geprueft: ohne Spiegelung u corr 0.82 / v 0.54, mit 0.993 / 0.997).
                //  Frueher: int idx_val = tri_idx[j, i]; float[] bary = barys[j][i];
                int j_mask = tri_idx.GetLength(0) - 1 - j;
                int idx_val = tri_idx[j_mask, i];
                float[] bary = barys[j_mask][i];

                bool pos_idx = idx_val > 0;
                if (pos_idx)
                {
                    (float d_x_ref, float d_y_ref, float d_z_ref) = find_truth_flow_ij(
                        i, j, idx_val, get_t_idx(), bary, d_xs, d_ys, d_zs, tris: tris);
                    (stream_u[i][j], stream_v[i][j], stream_z[i][j]) = (d_x_ref, d_y_ref, d_z_ref);
                }
                else
                {
                    (stream_u[i][j], stream_v[i][j], stream_z[i][j]) = (0f, 0f, 0f);
                }
            }
        }
        return (stream_u, stream_v);
    }

    /// <summary>
    /// Reference displacement of one pixel by barycentric interpolation of the vertex displacements.
    /// </summary>
    /// <param name="i">Column.</param>
    /// <param name="j">Row.</param>
    /// <param name="tri_idx">Hit triangle.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="bary">Barycentric coordinates.</param>
    /// <param name="d_xs">x displacement per vertex.</param>
    /// <param name="d_ys">y displacement per vertex.</param>
    /// <param name="d_zs">z displacement per vertex.</param>
    /// <param name="tris">Triangle index list (optional).</param>
    /// <returns>Tuple (dx, dy, dz).</returns>
    public (float, float, float) find_truth_flow_ij(int i, int j, int tri_idx, int blade_idx,
        float[] bary, float[] d_xs, float[] d_ys, float[] d_zs, int[] tris = null)
    {
        (int node_idx_0, int node_idx_1, int node_idx_2) = find_node(i, j, tri_idx, blade_idx, blade_tris: tris);

        // info (paul): These are the ground truth values
        float d_x_ref = bary[0] * d_xs[node_idx_0] + bary[1] * d_xs[node_idx_1] + bary[2] * d_xs[node_idx_2];
        float d_y_ref = -1f * (bary[0] * d_ys[node_idx_0] + bary[1] * d_ys[node_idx_1] + bary[2] * d_ys[node_idx_2]);//11112024 -// info (paul): The "-" turns around the picture (hopefully)
        float d_z_ref = bary[0] * d_zs[node_idx_0] + bary[1] * d_zs[node_idx_1] + bary[2] * d_zs[node_idx_2];
        return (d_x_ref, d_y_ref, d_z_ref);
    }

    /// <summary>
    /// Accumulates the flow from the start frame to a time step (or uses pre-summed fields) and compares it with the reference displacement.
    /// </summary>
    /// <param name="flow_mats_u">u maps per time step.</param>
    /// <param name="flow_mats_v">v maps per time step.</param>
    /// <param name="t_idx">Time index.</param>
    /// <param name="texs_albedo_u">Albedo textures (u).</param>
    /// <param name="texs_albedo_v">Albedo textures (v).</param>
    /// <param name="from_path">True to load stored data.</param>
    /// <param name="accum_u">Pre-summed u field (optional).</param>
    /// <param name="accum_v">Pre-summed v field (optional).</param>
    /// <returns>Tuple (u, v) maps.</returns>
    public (List<List<float>>, List<List<float>>) manage_match_to_start(
        List<List<List<float>>> flow_mats_u, List<List<List<float>>> flow_mats_v,
        int t_idx, List<Texture2D> texs_albedo_u, List<Texture2D> texs_albedo_v,
        bool from_path = false, List<List<float>> accum_u = null, List<List<float>> accum_v = null)
    {
        // info (paul): find uv flow from start
        //23092026 accum_u/accum_v: bereits aufsummierte Felder aus dem Refresh-Durchgang
        (List<List<float>> flow_u, List<List<float>> flow_v) = (accum_u != null && accum_v != null)
            ? (accum_u, accum_v)
            : find_accum_flow(flow_mats_u, flow_mats_v, t_idx);

        List<List<float>> flow_mat = null;
        List<List<float>> stream_z = null;

        if (from_path)
        {
            flow_mat = choose_coord(flow_u, flow_v, u_v_mode);
            stream_z = null;
        }
        else
        {
            // info (paul): add matlab ref flow:
            if (use_ncorr)//(get_plot_mode() == "value")
            {
                List<List<float>> loaded = manage_load_csv();
                List<List<float>> scaled = scale_res(loaded);
                List<List<float>> transposed = transpose_mat(scaled);
                flow_v = transposed;
            }

            List<List<float>> stream_u = null;
            List<List<float>> stream_v = null;
            float coverage = float.NaN;
            (stream_u, stream_v, stream_z, coverage) =
                manage_flow_or_loss(flow_u, flow_v, t_idx);

            // info (paul): scale label
            (float stream_u_min, float stream_u_max) = find_min_max(stream_u, with_padding: true);//11112024 
            (float stream_v_min, float stream_v_max) = find_min_max(stream_v, with_padding: true);//11112024 
            (float stream_u_mean, float dev_u) = find_mean_in_all(stream_u, span: 20, coverage: coverage);
            (float stream_v_mean, float dev_v) = find_mean_in_all(stream_v, span: 20, coverage: coverage);
            if (analysis_sweep_running())
            {
                var lightingU = lighting_stats(stream_u);
                var lightingV = lighting_stats(stream_v);
                (stream_u_mean, dev_u, stream_u_min, stream_u_max) =
                    (lightingU.mean, lightingU.std, lightingU.min, lightingU.max);
                (stream_v_mean, dev_v, stream_v_min, stream_v_max) =
                    (lightingV.mean, lightingV.std, lightingV.min, lightingV.max);
            }
            if (get_paint_with() == "uv")
            {
                update_scale_label_ext(stream_u_mean, dev_u, stream_u_min, stream_u_max, stream_v_mean,
                    dev_v, stream_v_min, stream_v_max);
                //11112024 update_scale_label(stream_u_mean, dev_u, stream_v_mean, dev_v);
            }

            save_means(stream_u_mean, stream_v_mean,
                blade_idx: get_blade_idx());

            // info (paul): choose between u, v and z coord/comp
            List<List<float>> flow_mats_chosen = choose_coord(stream_u, stream_v, u_v_mode);

            // info (paul): convert to cols, apply to tex; also for debugging overwriting is also possible
            flow_mat = to_tex_if(flow_mats_chosen, texs_albedo_u,
                texs_albedo_v);
            
            (float min_val, float max_val) = find_min_max(flow_mat, with_padding: true);
            _ = 1 + 1;
        }

        return (flow_mat, stream_z);
    }

    /// <summary>
    /// Upsamples a square matrix by pixel repetition.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <param name="scale">Factor.</param>
    /// <returns>Upsampled matrix.</returns>
    public List<List<float>> scale_res(List<List<float>> mat,
        int scale = 2)
    {
        // info (paul): scale matrix to higher resolution

        List<List<float>> scaled = zeros_of_size(scale * mat.Count,
            scale * mat.Count);

        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < mat[i].Count; j++)
            {
                scaled[scale * i][scale * j] = mat[i][j];
                scaled[scale * i + 1][scale * j] = mat[i][j];
                scaled[scale * i][scale * j + 1] = mat[i][j];
                scaled[scale * i + 1][scale * j + 1] = mat[i][j];
            }
        }
        return scaled;
    }
    /// <summary>
    /// Saves the mean displacements of a time step.
    /// </summary>
    /// <param name="u_mean">Mean of u.</param>
    /// <param name="v_mean">Mean of v.</param>
    /// <param name="blade_idx">Time-step index.</param>
    public void save_means(float u_mean, float v_mean,
        int blade_idx)
    {
        string u_path = construct_blade_path(cam_idx: 0,
            blade_idx, label: get_plot_mode() + "_u", type: "float",
            with_uv_mode: false);
        string v_path = construct_blade_path(cam_idx: 0,
            blade_idx, label: get_plot_mode() + "_v", type: "float",
            with_uv_mode: false);

        save_float(u_mean, full_path: u_path);
        save_float(v_mean, full_path: v_path);
    }
    /// <summary>
    /// Sums the frame-to-frame flow fields up to a time step.
    /// </summary>
    /// <param name="flow_mats_u">u maps per time step.</param>
    /// <param name="flow_mats_v">v maps per time step.</param>
    /// <param name="t_idx">Time index.</param>
    /// <returns>Tuple of accumulated (u, v).</returns>
    public (List<List<float>>, List<List<float>>) find_accum_flow(List<List<List<float>>> flow_mats_u,
        List<List<List<float>>> flow_mats_v, int t_idx)
    {
        // info (paul): match points
        // A three-frame dt experiment currently produces one flow field (frame 0
        // to frame 1). Include that field when comparing it with ground truth 0.
        int match_end = (t_idx == 0 && flow_mats_u.Count == 1) ? 1 : t_idx;
        List<List<(float, float)>> points_now = match_all_to_start(flow_mats_u, flow_mats_v,
                t_min: 0, t_max: match_end);
        //14012025    t_min: t_idx, t_max: t_idx + get_match_steps());// info (paul): very performance heavy

        // info (paul): subtract mean, normalize etc. partly for nice visualization
        List<List<(float, float)>> points_normed = treat_nice(points_now);

        (List<List<float>> stream_u, List<List<float>> stream_v) = points2flow(points_normed);//12062024 points_normed);
        return (stream_u, stream_v);
    }

    /// <summary>
    /// Converts tracked positions into displacements (optionally with debug visualisation).
    /// </summary>
    /// <param name="points_now">Tracked positions.</param>
    /// <returns>Displacements.</returns>
    public List<List<(float, float)>> treat_nice(List<List<(float, float)>> points_now)
    {
        List<List<(float, float)>> points_rel = subtract_grid_pos(points_now);

        bool with_visualize_nice = false;//true
        if (with_visualize_nice)
        {
            List<List<(float, float)>> points_fluc = subtract_mean(points_rel);

            // info (paul): norm
            List<List<(float, float)>> points_normed = norm_points(points_fluc);//14062024 points_fluc);
        }
        return points_rel;//points_normed;
    }

    List<List<float>> ref_u = null;
    List<List<float>> ref_v = null;
    List<List<float>> ref_z = null;
    float coverage_ref = float.NaN;

    List<List<float>> value_u = null;
    List<List<float>> value_v = null;
    float value_coverage = float.NaN;

    /// <summary>
    /// Returns the last reference u map.
    /// </summary>
    /// <returns>Map.</returns>
    public List<List<float>> get_ref_u()
    {
        return ref_u;
    }

    /// <summary>
    /// Returns the last reference v map.
    /// </summary>
    /// <returns>Map.</returns>
    public List<List<float>> get_ref_v()
    {
        return ref_v;
    }

    /// <summary>
    /// Returns the last computed u map.
    /// </summary>
    /// <returns>Map.</returns>
    public List<List<float>> get_u()
    {
        return value_u;
    }

    /// <summary>
    /// Returns the last computed v map.
    /// </summary>
    /// <returns>Map.</returns>
    public List<List<float>> get_v()
    {
        return value_v;
    }
    /// <summary>
    /// Converts an integer matrix to floats.
    /// </summary>
    /// <param name="ints">Integer matrix.</param>
    /// <returns>Float matrix.</returns>
    public List<List<float>> ints2floats(List<List<int>> ints)
    {
        List<List<float>> floats = zeros_like(ints);

        for (int i = 0; i < ints.Count; i++)
        {
            for (int j = 0; j < ints[0].Count; j++)
            {
                ;//25112024 ...
                floats[i][j] = (float)ints[i][j];
            }
        }

        return floats;
    }
    /// <summary>
    /// Converts a 2D integer array to a float matrix.
    /// </summary>
    /// <param name="ints">Integer array.</param>
    /// <returns>Float matrix.</returns>
    public List<List<float>> ints2floats(int[,] ints)
    {
        List<List<float>> floats = zeros_like(ints);

        for (int i = 0; i < ints.GetLength(0); i++)
        {
            for (int j = 0; j < ints.GetLength(1); j++)
            {
                ;//25112024 ...
                floats[i][j] = (float)ints[i, j];
            }
        }

        return floats;
    }
    // TV matrices are indexed [x][y], with y increasing down the input PNG.
    // Intersect the reference specimen directly: generated meshes are on layer 0,
    // whereas the legacy global raycast only searches layer 7.
    private (List<List<float>>, List<List<float>>, List<List<float>>, float)
        diagnose_lighting_flow(List<List<float>> flowU, List<List<float>> flowV, int targetIndex)
    {
        var specimens = collect_blades();
        if (targetIndex != 1 || specimens.Count < 2 || get_match_steps() != 1)
            throw new InvalidOperationException("Lighting comparison requires frame 0 -> 1 and match_steps=1.");
        var source = specimens[0];
        var target = specimens[targetIndex];
        var sourceMesh = source.GetComponent<MeshFilter>().sharedMesh;
        var targetMesh = target.GetComponent<MeshFilter>().sharedMesh;
        var triangles = sourceMesh.triangles;
        if (sourceMesh.vertexCount != targetMesh.vertexCount || !triangles.SequenceEqual(targetMesh.triangles))
            throw new InvalidOperationException("Ground-truth meshes must have matching vertex order and topology.");
        var collider = source.GetComponent<MeshCollider>();
        if (collider == null || collider.convex || collider.sharedMesh != sourceMesh)
            throw new InvalidOperationException("Reference specimen needs a non-convex collider matching its rendered mesh.");
        int width = flowU.Count, height = flowU[0].Count;
        if (width != cam_for_uv_0.pixelWidth || height != cam_for_uv_0.pixelHeight)
            throw new InvalidOperationException("TV resolution differs from the ground-truth camera viewport.");
        var from = transform_to_world(sourceMesh.vertices, source);
        var to = transform_to_world(targetMesh.vertices, target);
        var truthU = zeros_like(flowU);
        var truthV = zeros_like(flowV);
        var truthZ = zeros_like(flowV);
        var errorU = zeros_like(flowU);
        var errorV = zeros_like(flowV);
        var absoluteU = zeros_like(flowU);
        var absoluteV = zeros_like(flowV);
        bool wasActive = source.activeSelf, wasEnabled = collider.enabled;
        const int flowPadding = 5;
        const float relativeDenominatorFloor = 0.1f;
        int meshPixels = 0, usableFlowPixels = 0, behindCamera = 0, smallU = 0, smallV = 0;
        try
        {
            source.SetActive(true);
            collider.enabled = true;
            Physics.SyncTransforms();
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                truthU[x][y] = truthV[x][y] = truthZ[x][y] = float.NaN;
                errorU[x][y] = errorV[x][y] = absoluteU[x][y] = absoluteV[x][y] = float.NaN;
                var ray = cam_for_uv_0.ScreenPointToRay(new Vector3(x + 0.5f, height - y - 0.5f));
                if (!collider.Raycast(ray, out RaycastHit hit, float.PositiveInfinity)) continue;
                if (hit.triangleIndex < 0) continue;
                meshPixels++;
                // match_all_to_start() deliberately leaves this border outside its
                // interpolation domain. subtract_grid_pos() turns those untouched
                // grid coordinates into values as low as -511, so they are not flow.
                if (x < flowPadding || x >= width - flowPadding
                    || y < flowPadding || y >= height - flowPadding)
                    continue;
                usableFlowPixels++;
                int offset = hit.triangleIndex * 3;
                int a = triangles[offset], b = triangles[offset + 1], c = triangles[offset + 2];
                Vector3 weights = hit.barycentricCoordinate;
                Vector3 p = from[a] * weights.x + from[b] * weights.y + from[c] * weights.z;
                Vector3 q = to[a] * weights.x + to[b] * weights.y + to[c] * weights.z;
                Vector3 screenP = cam_for_uv_0.WorldToScreenPoint(p);
                Vector3 screenQ = cam_for_uv_0.WorldToScreenPoint(q);
                if (screenP.z <= 0 || screenQ.z <= 0) { behindCamera++; continue; }
                // Project the corresponding surface point, not interpolated vertex displacements.
                float u = screenQ.x - screenP.x, v = screenP.y - screenQ.y;
                truthU[x][y] = u; truthV[x][y] = v; truthZ[x][y] = q.z - p.z;
                absoluteU[x][y] = Mathf.Abs(flowU[x][y] - u);
                absoluteV[x][y] = Mathf.Abs(flowV[x][y] - v);
                if (Mathf.Abs(u) >= relativeDenominatorFloor) errorU[x][y] = absoluteU[x][y] / Mathf.Abs(u);
                else smallU++;
                if (Mathf.Abs(v) >= relativeDenominatorFloor) errorV[x][y] = absoluteV[x][y] / Mathf.Abs(v);
                else smallV++;
            }
        }
        finally
        {
            collider.enabled = wasEnabled;
            source.SetActive(wasActive);
            Physics.SyncTransforms();
        }
        var uStats = lighting_stats(errorU);
        var vStats = lighting_stats(errorV);
        string report = "Ground-truth diagnosis " + get_experiment() + ": frame 0 -> 1, mesh "
            + blade_idxs[0] + " -> " + blade_idxs[1] + ", camera=" + cam_for_uv_0.name
            + ", source layer=" + source.layer + ", pixels=" + width + "x" + height
            + ", coordinates=[x][y top-down], displacement=pixels, signed difference\n"
            + "mesh pixels=" + meshPixels + ", behind camera=" + behindCamera
            + ", flow-domain pixels=" + usableFlowPixels + ", padding=" + flowPadding
            + ", denominator below " + relativeDenominatorFloor.ToString(CultureInfo.InvariantCulture)
            + ": u=" + smallU + ", v=" + smallV + "\n"
            + lighting_summary("TV u before masks", flowU) + "\n"
            + lighting_summary("TV v before masks", flowV) + "\n"
            + lighting_summary("GT u before masks", truthU) + "\n"
            + lighting_summary("GT v before masks", truthV) + "\n"
            + lighting_summary("absolute u before denominator mask", absoluteU) + "\n"
            + lighting_summary("absolute v before denominator mask", absoluteV) + "\n"
            + lighting_summary("relative u after denominator mask", errorU) + "\n"
            + lighting_summary("relative v after denominator mask", errorV);
        Debug.Log(report);
        File.WriteAllText(Path.Combine(root_path, get_experiment() + "_diagnosis.txt"), report);
        if (meshPixels == 0 || uStats.count == 0 || vStats.count == 0)
            throw new InvalidOperationException("No valid ground-truth comparison pixels; see diagnosis file.");
        ref_u = truthU; ref_v = truthV; ref_z = truthZ;
        value_u = flowU; value_v = flowV;
        coverage_ref = value_coverage = (float)meshPixels / (width * height);
        return (errorU, errorV, truthZ, (float)vStats.count / meshPixels);
    }

    /// <summary>
    /// Statistics of the finite entries of a map.
    /// </summary>
    /// <param name="values">Map.</param>
    /// <returns>Tuple (count, mean, std, min, max).</returns>
    private static (int count, float mean, float std, float min, float max) lighting_stats(List<List<float>> values)
    {
        int count = 0;
        double mean = 0, m2 = 0;
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        foreach (var row in values)
        foreach (float value in row)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) continue;
            count++;
            double delta = value - mean;
            mean += delta / count;
            m2 += delta * (value - mean);
            min = Mathf.Min(min, value); max = Mathf.Max(max, value);
        }
        return count == 0 ? (0, float.NaN, float.NaN, float.NaN, float.NaN)
            : (count, (float)mean, (float)Math.Sqrt(m2 / count), min, max);
    }

    /// <summary>
    /// One-line text summary of the statistics of a map.
    /// </summary>
    /// <param name="name">Name of the map.</param>
    /// <param name="values">Map.</param>
    /// <returns>Text.</returns>
    private static string lighting_summary(string name, List<List<float>> values)
    {
        var stats = lighting_stats(values);
        return string.Format(CultureInfo.InvariantCulture,
            "{0}: finite={1}, mean={2:G9}, std={3:G9}, min={4:G9}, max={5:G9}",
            name, stats.count, stats.mean, stats.std, stats.min, stats.max);
    }


    /// <summary>
    /// Computes the displayed map (flow, reference, or error) for the current time step; during sweeps it uses the lighting diagnosis.
    /// </summary>
    /// <param name="flow_u">u map.</param>
    /// <param name="flow_v">v map.</param>
    /// <param name="t_idx">Time index.</param>
    /// <returns>Tuple (u result, v result, z result, loss).</returns>
    public (List<List<float>>, List<List<float>>, List<List<float>>, float) manage_flow_or_loss(
        List<List<float>> flow_u, List<List<float>> flow_v, int t_idx)
    {
        if (analysis_sweep_running())
            return diagnose_lighting_flow(flow_u, flow_v, t_idx);

        // info (paul): wording: "flow" denotes the disparity components u/v of the optical flow, 
        //      while "stream" might also be the loss

        // info (paul): we assume, that a certain t_idx corresponds to a certain blade
        Stopwatch sw_l = Stopwatch.StartNew(); //23092026 Zeitmessung (siehe refresh_plane_with_params)
        (float[] d_xs, float[] d_ys, float[] d_zs) = load_distortion_ground_truth(blade_idx: t_idx);
        t_gt_ms += sw_l.ElapsedMilliseconds;
        sw_l.Restart();
        //(float[] screen_x, float[] screen_y) = load_screen_poss();
        (int[,] tri_idx, float[][][] barys) = load_tris(blade_idx: 0);//Bt_idx);
        t_tris_ms += sw_l.ElapsedMilliseconds;
        sw_l.Restart();

        //23092026 tri_idx_floats wird nicht verwendet, kostete aber eine volle Kopie der Maske
        //List<List<float>> tri_idx_floats = ints2floats(tri_idx);

        (List<List<float>> stream_u, List<List<float>> stream_v, List<List<float>> stream_z,
            float coverage) = choose_flow_or_loss(
            flow_u, flow_v, tri_idx, barys, d_xs, d_ys, d_zs);

        // info (paul): save value ref matrix, which is later needed for the ground truth
        //23092026 value_ref/value haengen nicht vom Modus ab -> im Refresh-Durchgang nur einmal rechnen
        bool batch_ok = refresh_batch_depth > 0 && batch_ref_value_t_idx == t_idx && batch_ref_u != null;
        if (batch_ok)
        {
            (this.ref_u, this.ref_v, this.ref_z, this.coverage_ref) = (batch_ref_u, batch_ref_v, batch_ref_z, batch_coverage_ref);
            (this.value_u, this.value_v, this.value_coverage) = (batch_value_u, batch_value_v, batch_value_coverage);
        }
        else
        {
        string actual_mode = this.get_plot_mode();
        this.set_plot_mode("value_ref", is_internal: true);
        (this.ref_u, this.ref_v, this.ref_z, this.coverage_ref) = choose_flow_or_loss(
            flow_u, flow_v, tri_idx, barys, d_xs, d_ys, d_zs);
        this.set_plot_mode(actual_mode, is_internal: true);

        // info (paul): save value matrix, later needed
        actual_mode = this.get_plot_mode();
        this.set_plot_mode("value", is_internal: true);
        (this.value_u, this.value_v, _, this.value_coverage) = choose_flow_or_loss(
            flow_u, flow_v, tri_idx, barys, d_xs, d_ys, d_zs);
        this.set_plot_mode(actual_mode, is_internal: true);

            if (refresh_batch_depth > 0)
            {
                batch_ref_value_t_idx = t_idx;
                (batch_ref_u, batch_ref_v, batch_ref_z, batch_coverage_ref) = (this.ref_u, this.ref_v, this.ref_z, this.coverage_ref);
                (batch_value_u, batch_value_v, batch_value_coverage) = (this.value_u, this.value_v, this.value_coverage);
            }
        }

        t_choose_ms += sw_l.ElapsedMilliseconds;

        //23092026 Karten des aktuellen Modus fuer die Genauigkeitsanalyse merken
        last_stream_u = stream_u;
        last_stream_v = stream_v;

        return (stream_u, stream_v, stream_z, coverage);
    }
    private List<List<float>> last_stream_u = null;
    private List<List<float>> last_stream_v = null;

    /// <summary>
    /// Computes the displayed height map (value, reference, or absolute/relative error).
    /// </summary>
    /// <param name="heights">Measured heights.</param>
    /// <param name="heights_ref">Reference heights.</param>
    /// <param name="scale_factor">Scale factor.</param>
    /// <param name="threshold">Error clipping threshold.</param>
    /// <param name="mode">rel or abs.</param>
    /// <returns>Tuple (map, loss).</returns>
    public (List<List<float>>, float) choose_heights_or_loss(List<List<float>> heights,
        List<List<float>> heights_ref, float scale_factor, float threshold = 10f,
        string mode = "rel")
    {
        List<List<float>> stream_u = copy_mat(heights);
        //A List<List<float>> stream_v = copy_mat(flow_v);

        int i_center = (int)Mathf.Floor((float)(0.5f * heights.Count)); //07102024 256;
        int j_center = (int)Mathf.Floor((float)(0.5f * heights.Count)); //07102024 256;
        int i_span = i_center - 6;//07102024 250;//20;
        int j_span = i_center - 6;//07102024 250;//20;

        int i_off = 0;
        int j_off = 0;

        int i_min = i_center + i_off - i_span;
        int i_max = i_center + i_off + i_span;
        int j_min = j_center + j_off - j_span;
        int j_max = j_center + j_off + j_span;

        (float u_min, float u_max) = find_min_max(stream_u, with_padding: true);
        //A (float v_min, float v_max) = find_min_max(stream_v, with_padding: true);

        int counter = 0;
        int nonzero_counter = 0;

        for (int i = i_min; i < i_max; i++)//17072024 stream_u[0].Count; i++)
        {
            for (int j = j_min; j < j_max; j++)//17072024 stream_u.Count; j++)
            {
                bool isNaN;
                (stream_u[i][j], isNaN) = heights_or_loss_ij(stream_u, heights_ref, i, j,
                    u_min, u_max, scale_factor, threshold: threshold, mode: mode);
                nonzero_counter += (isNaN ? 0 : 1);
                counter += 1;
            }
        }

        float coverage = (float)(nonzero_counter) / (float)152072;//151808//152061//152072
        return (stream_u, coverage);
    }

    /// <summary>
    /// Computes the displayed flow map per pixel (value, reference, or error) and the statistics of the error.
    /// </summary>
    /// <param name="flow_u">u map.</param>
    /// <param name="flow_v">v map.</param>
    /// <param name="tri_idx">Triangle index per pixel.</param>
    /// <param name="barys">Barycentric coordinates per pixel.</param>
    /// <param name="d_xs">x displacement per vertex.</param>
    /// <param name="d_ys">y displacement per vertex.</param>
    /// <param name="d_zs">z displacement per vertex.</param>
    /// <returns>Tuple (u result, v result, z result, loss).</returns>
    public (List<List<float>>, List<List<float>>, List<List<float>>, float) choose_flow_or_loss(List<List<float>> flow_u,
        List<List<float>> flow_v, int[,] tri_idx, float[][][] barys, float[] d_xs, float[] d_ys, float[] d_zs)
    {
        List<List<float>> stream_u = copy_mat(flow_u);//mat_like(flow_u);//copy_mat(flow_u);
        List<List<float>> stream_v = copy_mat(flow_v);//mat_like(flow_v);//copy_mat(flow_v);
        List<List<float>> stream_z = copy_mat(flow_v);//mat_like(flow_v);//copy_mat(flow_v);

        int i_center = flow_v.Count / 2;//26102024 256;
        int j_center = flow_v.Count / 2;//26102024 256;
        int i_span = i_center - 6;//26102024 250;//20;
        int j_span = j_center - 6;//26102024 250;//20;

        int i_off = 0;
        int j_off = 0;

        int i_min = i_center + i_off - i_span;
        int i_max = i_center + i_off + i_span;
        int j_min = j_center + j_off - j_span;
        int j_max = j_center + j_off + j_span;

        if (false)
        {
            // info (paul): ncorr comparison
            j_max = 512 - 86; //A i_min = 154;
            j_min = 512 - 413; //A i_max = 389;
            i_min = 122; //A j_min = 98;
            i_max = 357; //A j_max = 425;
        }

        (float u_min, float u_max) = find_min_max(flow_u, with_padding: true);
        (float v_min, float v_max) = find_min_max(flow_v, with_padding: true);

        int counter = 0;
        int nonzero_counter = 0;

        for (int i = i_min; i < i_max; i++)//17072024 stream_u[0].Count; i++)
        {
            for (int j = j_min; j < j_max; j++)//17072024 stream_u.Count; j++)
            {
                // info (paul): If a triangle was found, which is close to the pixel, then load and assign 
                //      the corresponding flow values

                //22092026 Bugfix: Die TV-Flussmatrizen werden beim Laden transponiert
                //(tex2mat mit with_switch_dims = true): stream[i][j] = Pixel (x = i, y = j).
                //Die Maske aus im2triangles ist tris[y, x]. Die alte Abfrage
                //tri_idx[i, res-1-j] (= Pixel x = res-1-j, y = i) war dazu transponiert und
                //gespiegelt: Ground Truth und Umriss standen um 90 Grad zum TV-Fluss.
                //24092026 Bugfix: die Spiegelung war aber noetig - nur auf der anderen Achse. Die Maske
                //  (im2triangles) zaehlt Bildschirm-y von unten, die TV-Matrix die Zeilen von oben
                //  (an r1024, 28->29 geprueft: ohne Spiegelung u corr 0.82 / v 0.54, mit 0.993 / 0.997).
                //  Frueher: int idx_val = tri_idx[j, i]; float[] bary = barys[j][i];
                int j_mask = tri_idx.GetLength(0) - 1 - j;
                int idx_val = tri_idx[j_mask, i];
                float[] bary = barys[j_mask][i];

                bool pos_idx = idx_val > 0;
                if (pos_idx)
                {
                    bool isNaN = false;
                    (stream_u[i][j], stream_v[i][j], stream_z[i][j], isNaN) = flow_or_loss_ij(
                        stream_u, stream_v, d_xs, d_ys, d_zs, i, j, idx_val, bary,
                        get_t_idx(), u_min, u_max, v_min, v_max);//22102024 ca. t_idx
                    //stream_v[i][j] = stream_v[i][j];
                    nonzero_counter += (isNaN ? 0 : 1);
                    counter += 1;
                }
                else
                {
                    //21092026 Pixel ausserhalb des Proben-Meshes (kein Raycast-Treffer) sind
                    //"kein Wert" (NaN) statt 0: so bleiben sie in Karten schwarz, verfaelschen
                    //aber weder Wertebereich/Farbskala noch die Sweep-Statistiken
                    //(find_min_max, find_mean_in_all, lighting_stats ueberspringen NaN).
                    (stream_u[i][j], stream_v[i][j], stream_z[i][j]) = (float.NaN, float.NaN, float.NaN);
                }
            }
        }

        float coverage = (float)(nonzero_counter) / (float)20239;//(float)(counter);//((float)(counter))/((float)(i_max* j_max));
        if (lighting_sweep_running)
            Debug.Log("Ground-truth comparison: " + nonzero_counter + " / " + counter
                + " mesh pixels have a valid relative error.");
        return (stream_u, stream_v, stream_z, coverage);
    }

    /// <summary>
    /// Writes the value range of the chosen component to the scale label.
    /// </summary>
    /// <param name="loss_u_min">Minimum of u.</param>
    /// <param name="loss_u_max">Maximum of u.</param>
    /// <param name="loss_v_min">Minimum of v.</param>
    /// <param name="loss_v_max">Maximum of v.</param>
    public void update_scale_label(float loss_u_min, float loss_u_max, float loss_v_min, float loss_v_max)
    {
        Transform scale_label = canvas.transform.Find("scale_label");
        TextMeshProUGUI tmpro = scale_label.GetComponent<TextMeshProUGUI>();

        if (this.u_v_mode == "u")
        {
            tmpro.text = "min: " + loss_u_min.ToString() + "; max: " + loss_u_max.ToString();
        }

        if (this.u_v_mode == "v")
        {
            tmpro.text = "min: " + loss_v_min.ToString() + "; max: " + loss_v_max.ToString();
        }
    }
    /// <summary>
    /// Writes mean, standard deviation, and range of the chosen component to the scale label.
    /// </summary>
    /// <param name="loss_u_mean">Mean of u.</param>
    /// <param name="loss_u_std">Std of u.</param>
    /// <param name="loss_u_min">Minimum of u.</param>
    /// <param name="loss_u_max">Maximum of u.</param>
    /// <param name="loss_v_mean">Mean of v.</param>
    /// <param name="loss_v_std">Std of v.</param>
    /// <param name="loss_v_min">Minimum of v.</param>
    /// <param name="loss_v_max">Maximum of v.</param>
    public void update_scale_label_ext(float loss_u_mean, float loss_u_std, float loss_u_min, float loss_u_max,
        float loss_v_mean, float loss_v_std, float loss_v_min, float loss_v_max)
    {
        Transform scale_label = canvas.transform.Find("switch_panel").Find("scale_label");
        TextMeshProUGUI tmpro = scale_label.GetComponent<TextMeshProUGUI>();

        if (this.u_v_mode == "u")
        {
            //11112024 tmpro.text = "min: " + loss_u_min.ToString() + "; max: " + loss_u_max.ToString();
            tmpro.text = "mean: " + loss_u_mean.ToString() + "std: " + loss_u_std.ToString() +
                "; min: " + loss_u_min.ToString() + "; max: " + loss_u_max.ToString();
        }

        if (this.u_v_mode == "v")
        {
            //11112024 tmpro.text = "min: " + loss_v_min.ToString() + "; max: " + loss_v_max.ToString();
            tmpro.text = "mean: " + loss_v_mean.ToString() + "std: " + loss_v_std.ToString() +
                "; min: " + loss_v_min.ToString() + "; max: " + loss_v_max.ToString();
        }

        //21092026 Bugfix: hier wurden immer die v-Statistiken gespeichert, auch wenn die
        //u-Komponente angezeigt/exportiert wird. Dadurch bekam die u-Karte v_min=0 als
        //Wertebereich, negative u-Werte wurden beim Einfaerben auf schwarz geklemmt.
        //Jetzt die Statistik der gewaehlten Komponente (u_v_mode) speichern.
        if (this.u_v_mode == "u")
        {
            set_v_mean(loss_u_mean);
            set_v_std(loss_u_std);
            set_v_min(loss_u_min);
            set_v_max(loss_u_max);
        }
        else
        {
            set_v_mean(loss_v_mean);
            set_v_std(loss_v_std);
            set_v_min(loss_v_min);
            set_v_max(loss_v_max);
        }

        write_info(loss_u_mean, loss_u_std, loss_u_min, loss_u_max,
            loss_v_mean, loss_v_std, loss_v_min, loss_v_max);
    }

    /// <summary>
    /// Stores the global mean of v.
    /// </summary>
    /// <param name="input">Value.</param>
    public void set_v_mean(float input)
    {
        v_mean_global = input;
    }
    /// <summary>
    /// Stores the global standard deviation of v.
    /// </summary>
    /// <param name="input">Value.</param>
    public void set_v_std(float input)
    {
        v_std_global = input;
    }
    /// <summary>
    /// Stores the global minimum of v.
    /// </summary>
    /// <param name="input">Value.</param>
    public void set_v_min(float input)
    {
        v_min_global = input;
    }
    /// <summary>
    /// Stores the global maximum of v.
    /// </summary>
    /// <param name="input">Value.</param>
    public void set_v_max(float input)
    {
        v_max_global = input;
    }
    /// <summary>
    /// Returns the global mean of v.
    /// </summary>
    /// <returns>Value.</returns>
    public float get_v_mean()
    {
        return v_mean_global;
    }
    /// <summary>
    /// Returns the global standard deviation of v.
    /// </summary>
    /// <returns>Value.</returns>
    public float get_v_std()
    {
        return v_std_global;
    }
    /// <summary>
    /// Returns the global minimum of v.
    /// </summary>
    /// <returns>Value.</returns>
    public float get_v_min()
    {
        return v_min_global;
    }
    /// <summary>
    /// Returns the global maximum of v.
    /// </summary>
    /// <returns>Value.</returns>
    public float get_v_max()
    {
        return v_max_global;
    }
    /// <summary>
    /// Appends the statistics of the current map with all settings to the result files (and the sweep TSV).
    /// </summary>
    /// <param name="loss_u_mean">Mean of u.</param>
    /// <param name="loss_u_std">Std of u.</param>
    /// <param name="loss_u_min">Minimum of u.</param>
    /// <param name="loss_u_max">Maximum of u.</param>
    /// <param name="loss_v_mean">Mean of v.</param>
    /// <param name="loss_v_std">Std of v.</param>
    /// <param name="loss_v_min">Minimum of v.</param>
    /// <param name="loss_v_max">Maximum of v.</param>
    public void write_info(float loss_u_mean, float loss_u_std, float loss_u_min, float loss_u_max,
        float loss_v_mean, float loss_v_std, float loss_v_min, float loss_v_max)
    {
        string exp = get_experiment();
        string paint_mode = get_paint_with();
        string strain_mode = get_strain_mode();
        string plot_mode = get_plot_mode();

        string info_str = exp + "\t" + paint_mode + "\t" + strain_mode + "\t" + plot_mode + "\t" + loss_v_mean + "\t" +
            loss_v_std + "\t" + loss_v_min + "\t" + loss_v_max;
        string path = root_path + "info.txt";

        write_to_txt(path, info_str, mode: "append");

        if (analysis_sweep_running() && plot_mode == "loss_rel")
        {
            string extra = sweep_extra_metrics();
            string sweep_row = exp + "\t" + loss_v_mean.ToString(CultureInfo.InvariantCulture)
                + "\t" + loss_v_std.ToString(CultureInfo.InvariantCulture)
                + "\t" + loss_v_min.ToString(CultureInfo.InvariantCulture)
                + "\t" + loss_v_max.ToString(CultureInfo.InvariantCulture) + extra;
            if (lighting_sweep_running)
            {
                Actioner action = render_acts.Find(candidate => candidate.get_label() == exp);
                float intensity = action == null ? float.NaN : action.pars.get_lighting_intensity();
                string lightingRow = exp + "\t" + intensity.ToString("G9", CultureInfo.InvariantCulture)
                    + sweep_row.Substring(exp.Length);
                write_to_txt(root_path + "lighting_sweep.tsv", lightingRow, mode: "append");
            }
            else if (noise_sweep_running)
            {
                Actioner action = render_acts.Find(candidate => candidate.get_label() == exp);
                float electrons = action == null ? float.NaN : action.pars.get_poisson_error();
                float relativeSigma = electrons > 0f ? 1f / Mathf.Sqrt(electrons) : 0f;
                string noiseRow = exp + "\t" + (electrons > 0f
                        ? electrons.ToString("G9", CultureInfo.InvariantCulture) : "inf")
                    + "\t" + relativeSigma.ToString("G9", CultureInfo.InvariantCulture)
                    + "\t" + loss_v_mean.ToString(CultureInfo.InvariantCulture)
                    + "\t" + loss_v_std.ToString(CultureInfo.InvariantCulture)
                    + "\t" + loss_v_min.ToString(CultureInfo.InvariantCulture)
                    + "\t" + loss_v_max.ToString(CultureInfo.InvariantCulture) + extra;
                write_to_txt(root_path + "noise_sweep.tsv", noiseRow, mode: "append");
            }
            else if (speckle_sweep_running)
            {
                Actioner action = render_acts.Find(candidate => candidate.get_label() == exp);
                float diameter = action == null ? float.NaN : action.pars.get_speckle_size();
                string speckleRow = exp + "\t" + diameter.ToString("0.000", CultureInfo.InvariantCulture)
                    + "\t" + loss_v_mean.ToString(CultureInfo.InvariantCulture)
                    + "\t" + loss_v_std.ToString(CultureInfo.InvariantCulture)
                    + "\t" + loss_v_min.ToString(CultureInfo.InvariantCulture)
                    + "\t" + loss_v_max.ToString(CultureInfo.InvariantCulture) + extra;
                write_to_txt(root_path + "speckle_sweep.tsv", speckleRow, mode: "append");
            }
            ExperimentImageGallery.AddAnalysisResult(exp, loss_v_mean, loss_v_std,
                loss_v_min, loss_v_max);
        }
    }

    //28092026 Zusatzspalten der Licht/Rauschen/Speckle-Tabellen (fuer die Diagramme wie Abb. 5/6 im Paper):
    //  Fluss-MAE u/v, rel. Dehnungsfehler exx/eyy (wie "Genauigkeit", mit strain_sigma) und die Renderaufloesung
    //  (Speckle-Durchmesser in Bildpixeln = d * Aufloesung / 100). Bei Fehlern NaN, der Sweep laeuft weiter.
    //29092026 + Regularisierer (TV/TGV) und Dehnungsglaettung sigma, damit Tabellen eindeutig zuzuordnen sind
    const string SWEEP_EXTRA_HEADER = "\tu_mae\tv_mae\texx_rel_mae\teyy_rel_mae\trender_res\tregularization\tstrain_sigma";
    /// <summary>
    /// Key of the analysis setup (resolution, TV/TGV, strain sigma) for the sweep TSV.
    /// </summary>
    /// <returns>Tab-separated text.</returns>
    string sweep_setup_key()
    {
        CultureInfo ci = CultureInfo.InvariantCulture;
        return get_render_res().ToString(ci) + "\t" + (get_tv_use_tgv() && get_tv_use_gpu() ? "TGV" : "TV")
            + "\t" + strain_sigma.ToString("G6", ci);
    }
    /// <summary>
    /// Additional metrics of the current analysis for the sweep TSV.
    /// </summary>
    /// <returns>Tab-separated text.</returns>
    string sweep_extra_metrics()
    {
        CultureInfo ci = CultureInfo.InvariantCulture;
        string res = "\t" + sweep_setup_key();
        try
        {
            Dictionary<string, double> m = compute_study_metrics();
            Func<string, string> g = key => m.TryGetValue(key, out double d) ? d.ToString("G6", ci) : "NaN";
            return "\t" + g("u_mae") + "\t" + g("v_mae") + "\t" + g("exx_rel_mae") + "\t" + g("eyy_rel_mae") + res;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Sweep: Dehnungsmetriken nicht berechnet: " + e.Message);
            return "\tNaN\tNaN\tNaN\tNaN" + res;
        }
    }

    //28092026 Nutzerwunsch: Diagramm zu "Licht/Rauschen/Speckle" im Programm (orientiert an Abb. 5/6 im Paper):
    //  scripts/plot_sweeps.py erzeugt <analysis>_sweep_plot.png (rel. Verschiebungsfehler und rel. Dehnungsfehler
    //  ueber dem Parameter, Plateau schattiert, Trend gepunktet); das Bild kommt vorne in die Galerie.
    /// <summary>
    /// Creates the sweep diagram with scripts/plot_sweeps.py and puts it at the front of the gallery.
    /// </summary>
    /// <param name="analysis">Name of the analysis (e.g. lighting).</param>
    /// <returns>Task.</returns>
    public async Task show_sweep_plot(string analysis)
    {
        string tsv = root_path + analysis + "_sweep.tsv";
        if (!File.Exists(tsv)) return;
        string out_dir = root_path + "sweep_plots/";
        string exposure_tsv = param_study_dir() + "exposure_study_latest.tsv";
        string args = analysis + " \"" + tsv + "\" \"" + out_dir + "\" " + get_render_res().ToString(CultureInfo.InvariantCulture)
            + (analysis == "lighting" && File.Exists(exposure_tsv) ? " \"" + exposure_tsv + "\"" : "");
        string msg = await Task.Run(() => run_python(path_project + "scripts/plot_sweeps.py", args));
        string png = out_dir + analysis + "_sweep_plot.png";
        if (File.Exists(png) && File.GetLastWriteTimeUtc(png) >= File.GetLastWriteTimeUtc(tsv))
        {
            ExperimentImageGallery.InsertOrMoveImage(png, 0);
            string paper_png = out_dir + analysis + "_sweep_paper.png"; //29092026 Paper-Stil (Abb. 5a/6e) direkt dahinter
            if (File.Exists(paper_png))
                ExperimentImageGallery.InsertOrMoveImage(paper_png, 1);
            ExperimentImageGallery.ShowFirst();
        }
        if (msg != "")
            ExperimentImageGallery.AppendResultsText(msg);
    }

    int i_test = 330;
    int j_test = 230;

    /// <summary>
    /// Displayed value of one pixel of the height map (value, reference, or error).
    /// </summary>
    /// <param name="stream_u">Measured heights.</param>
    /// <param name="stream_ref">Reference heights.</param>
    /// <param name="i">Column.</param>
    /// <param name="j">Row.</param>
    /// <param name="u_min">Lower display limit.</param>
    /// <param name="u_max">Upper display limit.</param>
    /// <param name="scale_factor">Scale factor.</param>
    /// <param name="threshold">Lower threshold of the reference.</param>
    /// <param name="threshold_up">Error clipping threshold.</param>
    /// <param name="mode">rel or abs.</param>
    /// <returns>Tuple (value, valid).</returns>
    public (float, bool) heights_or_loss_ij(List<List<float>> stream_u, List<List<float>> stream_ref,
        int i, int j, float u_min, float u_max, float scale_factor, float threshold = 0.1f,
        float threshold_up = 0.01f, string mode = "rel")
    {
        // info (paul): These are the ground truth values
        if (i == i_test && j == j_test)
        {
            ;
        }

        float d_x_ref = stream_ref[(int)(scale_factor * i)][(int)(scale_factor * j)];

        // TODO: I think, actually stream_u and stream_v are only equal to d_x and d_y, if the 
        //      camera has infinite distance. So it would be more precise to project d_x and d_y to 
        //      the camera somehow and this would then probably also include d_z
        float u_ij = float.NaN;
        try
        {
            u_ij = stream_u[i][j];
        }
        catch
        {
            u_ij = stream_u[i][j];
        }
        //A float v_ij = stream_v[i][j];

        // info (paul): absolute error
        float err_x_abs = Mathf.Abs(Mathf.Abs(u_ij) - Mathf.Abs(d_x_ref));//for debugging to not look at sign
        //A float err_y_abs = Mathf.Abs(Mathf.Abs(v_ij) - Mathf.Abs(d_y_ref));//for debugging

        bool cond_1 = Mathf.Abs(d_x_ref) < threshold;
        bool cond_2 = Mathf.Abs(u_ij) < threshold;
        bool cond_3 = u_ij < u_min + threshold_up;
        bool cond_4 = u_ij > u_max - threshold_up;

        if (cond_1 || cond_2 ||
            cond_3 || cond_4)
        {
            err_x_abs = float.NaN;//0.3f;//float.NaN;
        }
        else
        {
            ;
        }
        //A if (Mathf.Abs(d_y_ref) < threshold || Mathf.Abs(v_ij) < threshold || v_ij < v_min + threshold_up || v_ij > v_max - threshold_up)
        //A {
        //A     err_y_abs = float.NaN;//0.3f;//float.NaN;
        //A }

        //17072024 float err_x_abs = Mathf.Abs(u_ij - d_x_ref);
        //17072024 float err_y_abs = Mathf.Abs(v_ij - d_y_ref);

        if (!float.IsNaN(err_x_abs))
        {
            ;
        }

        // info (paul): relative error
        float err_x_rel = err_x_abs / Mathf.Abs(d_x_ref);
        if (get_paint_with() == "heights")
        {
            err_x_rel = err_x_abs / Mathf.Abs(d_x_ref - default_dist);
        }
        //A float err_y_rel = err_y_abs / d_y_ref;

        //err_x_rel = threshold_err(err_x_rel);
        //err_y_rel = threshold_err(err_y_rel);

        (float val_u, float val_v) = (float.NaN, float.NaN);
        string plot_mode = this.get_plot_or_heights_mode();

        //A if (plot_mode == "loss_rel")
        //A {
        //A     //A (val_u, val_v) = (err_x_rel, err_y_rel);
        //A     (val_u, val_v) = (err_x_rel, float.NaN);
        //A }
        //A if (plot_mode == "loss_abs")
        //A {
        //A     //A (val_u, val_v) = (err_x_abs, err_y_abs);
        //A     (val_u, val_v) = (err_x_abs, float.NaN);
        //A }
        if (plot_mode == "value")
        {
            //A (val_u, val_v) = (u_ij, v_ij);
            (val_u, val_v) = (u_ij, float.NaN);
        }
        if (plot_mode == "value_ref")
        {
            (val_u, val_v) = (d_x_ref, float.NaN);
        }

        if (plot_mode == "loss_rel")//18112024 (mode == "loss_rel")
        {
            val_u = err_x_rel;
        }
        if (plot_mode == "loss_abs")//18112024
        {
            val_u = err_x_abs;
        }

        return (val_u, float.IsNaN(val_u)); //(err_x_rel, err_y_rel);// (err_x_rel, err_y_rel);//(d_x_ref, d_y_ref);//17062024 (err_x_rel, err_y_rel);
    }
    /// <summary>
    /// Clips a value at an upper threshold.
    /// </summary>
    /// <param name="value">Value.</param>
    /// <param name="threshold">Upper limit.</param>
    /// <returns>Clipped value.</returns>
    public float curb_err(float value, float threshold)
    {
        value = Mathf.Clamp(value, value, threshold);
        return value;
    }

    /// <summary>
    /// Displayed value of one pixel of the flow map (value, reference, or error) from the barycentric reference displacement.
    /// </summary>
    /// <param name="stream_u">u map.</param>
    /// <param name="stream_v">v map.</param>
    /// <param name="d_xs">x displacement per vertex.</param>
    /// <param name="d_ys">y displacement per vertex.</param>
    /// <param name="d_zs">z displacement per vertex.</param>
    /// <param name="i">Column.</param>
    /// <param name="j">Row.</param>
    /// <param name="tri_idx">Hit triangle.</param>
    /// <param name="bary">Barycentric coordinates.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="u_min">Lower limit of u.</param>
    /// <param name="u_max">Upper limit of u.</param>
    /// <param name="v_min">Lower limit of v.</param>
    /// <param name="v_max">Upper limit of v.</param>
    /// <returns>Tuple (u, v, z, valid).</returns>
    public (float, float, float, bool) flow_or_loss_ij(List<List<float>> stream_u, List<List<float>> stream_v,
        float[] d_xs, float[] d_ys, float[] d_zs, int i, int j, int tri_idx, float[] bary, int blade_idx,
        float u_min, float u_max, float v_min, float v_max)
    {
        (int node_idx_0, int node_idx_1, int node_idx_2) = find_node(i, j, tri_idx, blade_idx);

        // info (paul): These are the ground truth values
        float d_x_ref = bary[0] * d_xs[node_idx_0] + bary[1] * d_xs[node_idx_1] + bary[2] * d_xs[node_idx_2];
        float d_y_ref = -1f * (bary[0] * d_ys[node_idx_0] + bary[1] * d_ys[node_idx_1] + bary[2] * d_ys[node_idx_2]);//11112024 -// info (paul): The "-" turns around the picture (hopefully)
        float d_z_ref = bary[0] * d_zs[node_idx_0] + bary[1] * d_zs[node_idx_1] + bary[2] * d_zs[node_idx_2];

        // TODO: I think, actually stream_u and stream_v are only equal to d_x and d_y, if the 
        //      camera has infinite distance. So it would be more precise to project d_x and d_y to 
        //      the camera somehow and this would then probably also include d_z

        float u_ij = stream_u[i][j];
        float v_ij = stream_v[i][j];

        // info (paul): absolute error
        float err_x_abs = Mathf.Abs(Mathf.Abs(u_ij) - Mathf.Abs(d_x_ref));//for debugging to not look at sign
        float err_y_abs = Mathf.Abs(Mathf.Abs(v_ij) - Mathf.Abs(d_y_ref));//for debugging

        // The lighting sweep contains meaningful sub-pixel motion. The historic
        // one-pixel cutoff removed every sample and produced NaN statistics.
        float threshold = lighting_sweep_running ? 1e-4f : 1f;
        float threshold_up = 0.01f;//11112024 0.01f;//27092024 0.01f;
        if (category == "muc")
        {
            threshold = 0.1f;
            threshold_up = 0.01f;
        }

        if (i == i_test && j == j_test)
        {
            ;
        }

        if (Mathf.Abs(d_x_ref) < threshold || Mathf.Abs(u_ij) < threshold ||
            u_ij < u_min + threshold_up || u_ij > u_max - threshold_up)
        {
            err_x_abs = float.NaN;//0.3f;//float.NaN;
        }
        if (Mathf.Abs(d_y_ref) < threshold || Mathf.Abs(v_ij) < threshold ||
            v_ij < v_min + threshold_up || v_ij > v_max - threshold_up)
        {
            err_y_abs = float.NaN;//0.3f;//float.NaN;
        }

        //17072024 float err_x_abs = Mathf.Abs(u_ij - d_x_ref);
        //17072024 float err_y_abs = Mathf.Abs(v_ij - d_y_ref);

        // info (paul): relative error
        float err_x_rel = err_x_abs / Mathf.Abs(d_x_ref);
        float err_y_rel = err_y_abs / Mathf.Abs(d_y_ref);
        if (err_y_rel < 0f)
        {
            ;
        }
        //A err_x_rel = curb_err(err_x_rel, threshold: 1f);
        //A err_y_rel = curb_err(err_y_rel, threshold: 1f);

        //err_x_rel = threshold_err(err_x_rel);
        //err_y_rel = threshold_err(err_y_rel);

        (float val_u, float val_v, float val_z) = (float.NaN, float.NaN, float.NaN);
        string plot_mode = this.get_plot_mode();
        if (plot_mode == "loss_rel")
        {
            (val_u, val_v) = (err_x_rel, err_y_rel);
        }
        if (plot_mode == "loss_abs")
        {
            (val_u, val_v) = (err_x_abs, err_y_abs);
        }
        if (plot_mode == "value")
        {
            (val_u, val_v, val_z) = (u_ij, v_ij, d_z_ref);// TODO: replace d_z_ref later
        }
        if (plot_mode == "value_ref")
        {
            (val_u, val_v, val_z) = (d_x_ref, d_y_ref, d_z_ref);
        }

        return (val_u, val_v, val_z, float.IsNaN(val_v)); //(err_x_rel, err_y_rel);// (err_x_rel, err_y_rel);//(d_x_ref, d_y_ref);//17062024 (err_x_rel, err_y_rel);
    }

    /// <summary>
    /// Marks relative errors above 1 with a fixed value.
    /// </summary>
    /// <param name="err_x_rel">Relative error.</param>
    /// <returns>Error or marker value.</returns>
    public float threshold_err(float err_x_rel)
    {
        float threshold = 1.0f;
        if (Mathf.Abs(err_x_rel) > threshold)
        {
            err_x_rel = 10f;
        }
        else
        {
            err_x_rel = -10f;
        }
        return err_x_rel;
    }

    /// <summary>
    /// Returns the display mode of the flow (value, reference, error).
    /// </summary>
    /// <returns>Mode.</returns>
    public string get_plot_mode()
    {
        return plot_mode;
    }
    /// <summary>
    /// Returns the display mode of the current map type (flow or heights).
    /// </summary>
    /// <returns>Mode.</returns>
    public string get_plot_or_heights_mode()
    {
        string mode = null;
        if (get_paint_with() == "uv")
        {
            mode = get_plot_mode();
        }
        if (get_paint_with() == "heights")
        {
            mode = get_heights_mode();
        }
        return mode;
    }
    /// <summary>
    /// Sets the display mode of the flow.
    /// </summary>
    /// <param name="val">Mode.</param>
    /// <param name="is_internal">True if set internally (no UI update).</param>
    public void set_plot_mode(string val, bool is_internal = false)
    {
        this.plot_mode = val;
        this.plot_mode = val;
        if (!is_internal)
        {
            set_paint_with("uv");
            //set_plot_mode("", is_internal: true);
        }
    }
    /// <summary>
    /// Sets the displayed map type (uv, heights, ...).
    /// </summary>
    /// <param name="value">Map type.</param>
    public void set_paint_with(string value)
    {
        this.paint_with = value;
    }

    /// <summary>
    /// Returns the displayed map type.
    /// </summary>
    /// <returns>Map type.</returns>
    public string get_paint_with()
    {
        return this.paint_with;
    }

    /// <summary>
    /// Vertex indices of a triangle.
    /// </summary>
    /// <param name="i_idx">Column.</param>
    /// <param name="j_idx">Row.</param>
    /// <param name="tri_idx">Triangle index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="blade_tris">Triangle index list (optional).</param>
    /// <returns>Tuple of the three vertex indices.</returns>
    public (int, int, int) find_node(int i_idx, int j_idx, int tri_idx, int blade_idx,
        int[] blade_tris = null)
    {
        //int tri_idx = ;//find_triangle(i, j);

        //12112024 float t_1 = -1f;
        //12112024 float t_2 = -1f;
        //12112024 float t_3 = -1f;
        //12112024 float t_4 = -1f;
        //12112024 
        //12112024 if (i_idx == 231 && j_idx > 634) { tik(); }
        //12112024 List<GameObject> blades = this.get_blades();
        //12112024 if (i_idx == 231 && j_idx > 634) { t_1 = tok(); }
        //12112024 if (i_idx == 231 && j_idx > 634) { tik(); }
        //12112024 
        //12112024 Mesh blade_mesh = null;
        //12112024 //blade_mesh = blades[t_idx].GetComponent<MeshFilter>().sharedMesh;
        //12112024 if (i_idx == 231 && j_idx > 634) { t_2 = tok(); }
        //12112024 if (i_idx == 231 && j_idx > 634) { tik(); }

        //try
        //{
        //    blade_mesh = blades[t_idx].GetComponent<MeshFilter>().sharedMesh;
        //}
        //catch
        //{
        //    blade_mesh = blades[t_idx].GetComponent<MeshFilter>().sharedMesh;
        //}

        // info (paul): get the 3 point idxs of the triangle
        //17062024 int[] tri_0 = blade_mesh.triangles;
        //12112024 if (i_idx == 231 && j_idx > 634) { t_3 = tok(); }
        //12112024 if (i_idx == 231 && j_idx > 634) { tik(); }
        //12112024 

        (int node_0, int node_1, int node_2) = (-1, -1, -1);
        if (blade_tris == null)
        {
            node_0 = get_blade_tris()[blade_idx][3 * tri_idx + 0];
            node_1 = get_blade_tris()[blade_idx][3 * tri_idx + 1];
            node_2 = get_blade_tris()[blade_idx][3 * tri_idx + 2];
        }
        else
        {
            node_0 = blade_tris[3 * tri_idx + 0];
            node_1 = blade_tris[3 * tri_idx + 1];
            node_2 = blade_tris[3 * tri_idx + 2];
        }

            //13072024 int node_1 = blade_tris[t_idx][3 * tri_idx + 1];
            //13072024 int node_2 = blade_tris[t_idx][3 * tri_idx + 2];
            //12112024 if (i_idx == 231 && j_idx > 634) {
            //12112024     t_4 = tok();
            //12112024 }

            // info (paul): for now we just pick the first idx
            return (node_0, node_1, node_2);
    }

    /// <summary>
    /// Hit triangle and barycentric coordinates of a pixel (ray cast).
    /// </summary>
    /// <param name="i">Column.</param>
    /// <param name="j">Row.</param>
    /// <param name="cam">Camera.</param>
    /// <returns>Tuple (triangle index, barycentric coordinates).</returns>
    public (int, float[]) find_triangle(int i, int j, Camera cam)
    {
        // TODO: Pick the closest vertex or interpolate would be even better, instead of
        //      just picking some vertex of the triangle - I think we did that now - done

        //12112024 for (int idx = 0; idx < blades.Count; idx++)
        //12112024 {
        //12112024     blades[idx].SetActive(true);
        //12112024 }

        int tri_idx = -1;
        float[] bary = null;
        Ray ray_ij = cam.ScreenPointToRay(new Vector3(i, j));

        RaycastHit hit;
        int layerMask = 1 << 7; //  info (paul): means: only include layer 7.

        //15032025 bool has_hit = Physics.Raycast(cam.transform.position, ray_ij.direction, out hit, Mathf.Infinity);
        bool has_hit = Physics.Raycast(ray_ij.origin, ray_ij.direction, out hit, Mathf.Infinity, layerMask);
        if (has_hit)
        {
            tri_idx = hit.triangleIndex;
            bary = vec2floats(hit.barycentricCoordinate);
            string name_l = hit.transform.gameObject.name;
        }
        return (tri_idx, bary);
    }

    /// <summary>
    /// Selects the u or v component.
    /// </summary>
    /// <param name="stream_u">u map.</param>
    /// <param name="stream_v">v map.</param>
    /// <param name="u_v_mode">u or v.</param>
    /// <returns>Chosen map.</returns>
    public List<List<float>> choose_coord(List<List<float>> stream_u, List<List<float>> stream_v, string u_v_mode)
    {
        List<List<float>> flow_mats_chosen = new List<List<float>>();
        if (u_v_mode == "u")
        {
            flow_mats_chosen = stream_u; //27052024 flow_mats_u[t_idx];
        }
        if (u_v_mode == "v")
        {
            flow_mats_chosen = stream_v; //27052024 flow_mats_v[t_idx];
        }
        if (u_v_mode == "z")
        {
            flow_mats_chosen = stream_v; // just to have it not empty
        }
        return flow_mats_chosen;
    }

    /// <summary>
    /// Normalises both components of a point matrix.
    /// </summary>
    /// <param name="points_fluc">Matrix of (x, y) tuples.</param>
    /// <returns>Normalised matrix.</returns>
    public List<List<(float, float)>> norm_points(List<List<(float, float)>> points_fluc)
    {
        (List<List<float>> points_x, List<List<float>> points_y) = split_match(points_fluc);//points_fluc);
        points_x = norm_mat(points_x);
        points_y = norm_mat(points_y);
        List<List<(float, float)>> points_normed = merge(points_x, points_y);
        return points_normed;
    }

    /// <summary>
    /// Debugging helper: optionally replaces the map by an albedo texture.
    /// </summary>
    /// <param name="flow_mats_chosen">Chosen map.</param>
    /// <param name="texs_albedo_u">Albedo textures (u).</param>
    /// <param name="texs_albedo_v">Albedo textures (v).</param>
    /// <returns>Map.</returns>
    public List<List<float>> to_tex_if(List<List<float>> flow_mats_chosen,
        List<Texture2D> texs_albedo_u, List<Texture2D> texs_albedo_v)
    {
        bool overwrite_for_simple = false;
        Texture2D tex = null;
        if (overwrite_for_simple)
        {
            // info (paul): looks like that is more debugging stuff, lets not worry
            //20102024              about it so much
            //20102024 tex = mat2tex(flow_mats_chosen, with_switch_dims: false);
            //20102024 
            //20102024 // info (paul): Seems it is overwriting with a tex_v directly,
            //20102024              perh. for debugging
            //20102024 if (u_v_mode == "v") { tex = texs_albedo_v[t_idx]; }
        }
        else
        {
            //11112024 flow_mats_chosen = norm_mat(flow_mats_chosen);//11112024 , lower: -3f, upper: 3f);
            tex = mat2tex(flow_mats_chosen, with_switch_dims: false);
        }
        return flow_mats_chosen;
    }

    /// <summary>
    /// Splits an integer tuple matrix into two float matrices.
    /// </summary>
    /// <param name="match_mat">Matrix of (x, y) tuples.</param>
    /// <returns>Tuple (x map, y map).</returns>
    public (List<List<float>>, List<List<float>>) split_match(
        List<List<(int, int)>> match_mat)
    {
        List<List<float>> match_u = zeros_like(match_mat, return_type: "floats");
        List<List<float>> match_v = zeros_like(match_mat, return_type: "floats");

        for (int i = 0; i < match_mat.Count; i++)
        {
            for (int j = 0; j < match_mat[0].Count; j++)
            {
                match_u[i][j] = match_mat[i][j].Item1;
                match_v[i][j] = match_mat[i][j].Item2;
            }
        }

        return (match_u, match_v);
    }

    /// <summary>
    /// Splits a float tuple matrix into two float matrices.
    /// </summary>
    /// <param name="match_mat">Matrix of (x, y) tuples.</param>
    /// <returns>Tuple (x map, y map).</returns>
    public (List<List<float>>, List<List<float>>) split_match(
    List<List<(float, float)>> match_mat)
    {
        List<List<float>> match_u = zeros_like(match_mat, return_type: "floats");
        List<List<float>> match_v = zeros_like(match_mat, return_type: "floats");

        for (int i = 0; i < match_mat.Count; i++)
        {
            for (int j = 0; j < match_mat[0].Count; j++)
            {
                match_u[i][j] = match_mat[i][j].Item1;
                match_v[i][j] = match_mat[i][j].Item2;
            }
        }

        return (match_u, match_v);
    }

    /// <summary>
    /// Merges two matrices into a tuple matrix.
    /// </summary>
    /// <param name="mat_1">x map.</param>
    /// <param name="mat_2">y map.</param>
    /// <returns>Matrix of (x, y) tuples.</returns>
    public List<List<(float, float)>> merge(List<List<float>> mat_1, List<List<float>> mat_2)
    {
        List<List<(float, float)>> merged = zero_tuples_like(mat_1);

        for (int i = 0; i < mat_1.Count; i++)
        {
            for (int j = 0; j < mat_1[0].Count; j++)
            {
                merged[i][j] = (mat_1[i][j], mat_2[i][j]);
            }
        }

        return merged;
    }

    /// <summary>
    /// Fills a point matrix with the grid positions (offset by i_min, j_min).
    /// </summary>
    /// <param name="points_now">Point matrix.</param>
    /// <param name="i_min">Column offset.</param>
    /// <param name="j_min">Row offset.</param>
    /// <returns>Grid matrix.</returns>
    public List<List<(float, float)>> make_grid(List<List<(float, float)>> points_now, int i_min, int j_min)
    {
        for (int i = 0; i < points_now.Count; i++)
        {
            for (int j = 0; j < points_now[0].Count; j++)
            {
                float val_1 = (float)(i);//18062024  + i_min);
                float val_2 = (float)(j);//18062024  + j_min);
                points_now[i][j] = (val_1, val_2);
            }
        }

        return points_now;
    }

    /// <summary>
    /// Tracks all pixels over the frames back to the start frame (Lagrangian accumulation of the flow).
    /// </summary>
    /// <param name="flow_mats_u">u maps per time step.</param>
    /// <param name="flow_mats_v">v maps per time step.</param>
    /// <param name="t_min">First time step.</param>
    /// <param name="t_max">Last time step.</param>
    /// <returns>Tracked positions.</returns>
    public List<List<(float, float)>> match_all_to_start(List<List<List<float>>> flow_mats_u,
    List<List<List<float>>> flow_mats_v, int t_min = -1, int t_max = -1)
    {
        // TODO: correct to match a lot of points and not just a single point

        int padding = 5;

        int i_min = 0 + padding;
        int i_max = flow_mats_u[0].Count - padding;

        int j_min = 0 + padding;
        int j_max = flow_mats_u[0][0].Count - padding;

        List<List<(float, float)>> points_now = zero_tuples_of_size(i_max - i_min + 2 * padding, j_max - j_min + 2 * padding, type: "float");
        points_now = make_grid(points_now, i_min, j_min);

        //26102024 for (int t = t_min; t < t_max; t++)
        //14012024 for (int t = 0; t < flow_mats_v.Count; t++)
        //14012024 for (int t = 0; t < get_t_idx(); t++)
        int upper_idx = Mathf.Min(flow_mats_v.Count, t_max);
        for (int t = t_min; t < upper_idx; t++)
        {
            List<List<(float, float)>> points_next = match_slice(padding, i_min, i_max, j_min,
                j_max, t, flow_mats_u, flow_mats_v, points_now);
            points_now = points_next;
        }

        //14072024 for (int i = 0; i < 2; i++)
        //14072024 {
        //14072024     points_now = filter_mean(points_now);
        //14072024 }

        //14072024 points_now = paint_for_debug(points_now);

        return points_now;
    }
    /// <summary>
    /// Mean filter of a map (inner area only).
    /// </summary>
    /// <param name="points_now">Map.</param>
    /// <returns>Filtered map.</returns>
    public List<List<float>> filter_mean_comp(
    List<List<float>> points_now)
    {
        List<List<float>> points_next = zeros_of_size(points_now.Count,
            points_now.Count);

        int padding = 5;

        //23092026 Beschleunigung: gleiches Ergebnis wie plain_conv_comp (Fenster k, l = -5..4, nur
        //  Indizes 1..N-1, Mittel ueber gueltige Zellen, NaN/Inf im Fenster -> NaN), aber ueber
        //  2D-Praefixsummen statt 100 Zugriffen je Pixel (bei r1024 ca. 1 s je Karte).
        //  Frueher: points_next[i][j] = plain_conv_comp(points_now, i, j);
        int n = points_now.Count;
        int m = points_now[0].Count;
        double[,] sum = new double[n + 1, m + 1];
        int[,] bad = new int[n + 1, m + 1];
        for (int i = 0; i < n; i++)
        {
            double row_sum = 0;
            int row_bad = 0;
            List<float> row = points_now[i];
            for (int j = 0; j < m; j++)
            {
                float v = row[j];
                if (float.IsNaN(v) || float.IsInfinity(v)) row_bad++;
                else row_sum += v;
                sum[i + 1, j + 1] = sum[i, j + 1] + row_sum;
                bad[i + 1, j + 1] = bad[i, j + 1] + row_bad;
            }
        }
        const int k_lo = -5, k_hi = 4; // wie plain_conv_comp mit plaquette_size = 5
        for (int i = padding; i < n - padding; i++)
        {
            int r0 = Math.Max(i + k_lo, 1), r1 = Math.Min(i + k_hi, n - 1);
            for (int j = padding; j < m - padding; j++)
            {
                int c0 = Math.Max(j + k_lo, 1), c1 = Math.Min(Math.Min(j + k_hi, n - 1), m - 1); // Original prueft j gegen Count
                if (r1 < r0 || c1 < c0)
                {
                    points_next[i][j] = float.NaN;
                    continue;
                }
                int nb = bad[r1 + 1, c1 + 1] - bad[r0, c1 + 1] - bad[r1 + 1, c0] + bad[r0, c0];
                if (nb > 0)
                {
                    points_next[i][j] = float.NaN;
                    continue;
                }
                double s = sum[r1 + 1, c1 + 1] - sum[r0, c1 + 1] - sum[r1 + 1, c0] + sum[r0, c0];
                int cnt = (r1 - r0 + 1) * (c1 - c0 + 1);
                points_next[i][j] = (float)(s / cnt);
            }
        }

        return points_next;
    }
    /// <summary>
    /// Mean filter of a tuple matrix (inner area only).
    /// </summary>
    /// <param name="points_now">Matrix of (x, y) tuples.</param>
    /// <returns>Filtered matrix.</returns>
    public List<List<(float, float)>> filter_mean(
        List<List<(float, float)>> points_now)
    {
        List<List<(float, float)>> points_next = zero_tuples_of_size(points_now.Count,
            points_now.Count, type: "float");

        int padding = 5;

        for (int i = padding; i < points_now.Count - padding; i++)
        {
            for (int j = padding; j < points_now[0].Count - padding; j++)
            {
                (float item_1, float item_2) = plain_conv(points_now, i, j);

                points_next[i][j] = (item_1, item_2);
            }
        }

        return points_next;
    }
    /// <summary>
    /// Mean of a square neighbourhood of a pixel (finite values only).
    /// </summary>
    /// <param name="points_now">Map.</param>
    /// <param name="i">Column.</param>
    /// <param name="j">Row.</param>
    /// <param name="plaquette_size">Half width of the neighbourhood.</param>
    /// <returns>Mean.</returns>
    public float plain_conv_comp(List<List<float>>
        points_now, int i, int j, int plaquette_size = 5)
    {
        float sum_1 = 0f;
        float sum_2 = 0f;
        int cnt = 0;

        for (int k = -plaquette_size; k < plaquette_size; k++)
        {
            for (int l = -plaquette_size; l < plaquette_size; l++)
            {

                if (i > 10 && j > 10)
                {
                    ;
                }
                int idx_i = i + k;
                int idx_j = j + l;

                bool above_lower_i = (idx_i > 0);
                bool below_upper_i = (idx_i < points_now.Count);

                bool above_lower_j = (idx_j > 0);
                bool below_upper_j = (idx_j < points_now.Count);

                bool in_frame = above_lower_i && below_upper_i && above_lower_j && below_upper_j;
                if (in_frame)
                {
                    float summand_1 = points_now[idx_i][idx_j];

                    sum_1 += summand_1;
                    cnt += 1;
                }
            }
        }

        float item_1 = sum_1 / ((float)(cnt));

        //float item_1 = (1 / 5f) * (points_now[i][j].Item1 + points_now[i][j - 1].Item1
        //    + points_now[i][j + 1].Item1 + points_now[i - 1][j].Item1 +
        //    points_now[i + 1][j].Item1);
        //float item_2 = (1 / 5f) * (points_now[i][j].Item2 + points_now[i][j - 1].Item2
        //    + points_now[i][j + 1].Item2 + points_now[i - 1][j].Item2 +
        //    points_now[i + 1][j].Item2);
        return item_1;
    }
    /// <summary>
    /// Mean of a square neighbourhood of a pixel for both components.
    /// </summary>
    /// <param name="points_now">Matrix of (x, y) tuples.</param>
    /// <param name="i">Column.</param>
    /// <param name="j">Row.</param>
    /// <param name="plaquette_size">Half width of the neighbourhood.</param>
    /// <returns>Tuple of means.</returns>
    public (float, float) plain_conv(List<List<(float, float)>>
        points_now, int i, int j, int plaquette_size = 1)
    {
        float sum_1 = 0f;
        float sum_2 = 0f;
        int cnt = 0;

        for (int k = -plaquette_size; k < plaquette_size; k++)
        {
            for (int l = -plaquette_size; l < plaquette_size; l++)
            {

                if (i > 10 && j > 10)
                {
                    ;
                }
                int idx_i = i + k;
                int idx_j = j + l;

                bool above_lower_i = (idx_i > 0);
                bool below_upper_i = (idx_i < points_now.Count);

                bool above_lower_j = (idx_j > 0);
                bool below_upper_j = (idx_j < points_now.Count);

                bool in_frame = above_lower_i && below_upper_i && above_lower_j && below_upper_j;
                if (in_frame)
                {
                    float summand_1 = points_now[idx_i][idx_j].Item1;
                    float summand_2 = points_now[idx_i][idx_j].Item2;

                    sum_1 += summand_1;
                    sum_2 += summand_2;
                    cnt += 1;
                }
            }
        }

        float item_1 = sum_1 / ((float)(cnt));
        float item_2 = sum_2 / ((float)(cnt));

        //float item_1 = (1 / 5f) * (points_now[i][j].Item1 + points_now[i][j - 1].Item1
        //    + points_now[i][j + 1].Item1 + points_now[i - 1][j].Item1 +
        //    points_now[i + 1][j].Item1);
        //float item_2 = (1 / 5f) * (points_now[i][j].Item2 + points_now[i][j - 1].Item2
        //    + points_now[i][j + 1].Item2 + points_now[i - 1][j].Item2 +
        //    points_now[i + 1][j].Item2);
        return (item_1, item_2);
    }
    /// <summary>
    /// Debugging helper: marks a line in a point matrix.
    /// </summary>
    /// <param name="points">Point matrix.</param>
    /// <returns>Modified matrix.</returns>
    public List<List<(float, float)>> paint_for_debug(
        List<List<(float, float)>> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            points[256][i] = (10f, 10f);
            points[256][i] = (10f, 10f);
        }

        return points;
    }


    /// <summary>
    /// Tracks the points of a sub-area over one time step.
    /// </summary>
    /// <param name="padding">Border width.</param>
    /// <param name="i_min">First column.</param>
    /// <param name="i_max">Last column.</param>
    /// <param name="j_min">First row.</param>
    /// <param name="j_max">Last row.</param>
    /// <param name="t">Time step.</param>
    /// <param name="flow_mats_u">u maps per time step.</param>
    /// <param name="flow_mats_v">v maps per time step.</param>
    /// <param name="points_now">Current positions.</param>
    /// <returns>New positions.</returns>
    public List<List<(float, float)>> match_slice(int padding, int i_min, int i_max, int j_min, int j_max, int t,
        List<List<List<float>>> flow_mats_u, List<List<List<float>>> flow_mats_v, List<List<(float, float)>> points_now)
    {
        List<List<(float, float)>> points_next = zero_tuples_of_size(i_max - i_min + 2 * padding, j_max - j_min + 2 * padding, type: "float");

        for (int i = i_min; i < i_max; i++)
        {
            for (int j = j_min; j < j_max; j++)
            {
                (float, float) vals_l = match_to_start(flow_mats_u,
                    flow_mats_v, points_now[i][j], t_min: t, t_max: t + 1, i: i, j: j);

                points_next[i][j] = vals_l;//(vals_l[0].Item1 + (float)(j % 2), vals_l[0].Item2 + (float)(j % 2));//((float)(i % 2), (float)(i % 2));//vals_l[0];//(flow_mats_u[t_idx][i][j], flow_mats_v[t_idx][i][j]);//27052024 vals_l[0];
            }
        }
        return points_next;
    }
    /// <summary>
    /// Tracks one point over the frames by bilinear interpolation of the flow.
    /// </summary>
    /// <param name="flow_mats_u">u maps per time step.</param>
    /// <param name="flow_mats_v">v maps per time step.</param>
    /// <param name="point">Start position.</param>
    /// <param name="t_min">First time step.</param>
    /// <param name="t_max">Last time step.</param>
    /// <param name="i">Column (debugging).</param>
    /// <param name="j">Row (debugging).</param>
    /// <returns>End position.</returns>
    public (float, float) match_to_start(List<List<List<float>>> flow_mats_u,
    List<List<List<float>>> flow_mats_v, (float, float) point, int t_min = -1,
    int t_max = -1, int i = -1, int j = -1)
    {
        // info (paul): find match matrices
        //(float, float) point = (3.5f, 5.2f);// info (paul): or whatever your startpoint is
        Dictionary<float, (float, float)> points_dic = new Dictionary<float, (float, float)>();

        if (t_min < 0)
        {
            t_min = 0;
            t_max = flow_mats_u.Count;
            if (t_max > 4)
            {
                t_max = 0;
            }
        }

        for (int t_idx = t_min; t_idx < t_max; t_idx++)
        {
            try
            {
                point = find_next_point(flow_mats_u[t_idx], flow_mats_v[t_idx], point, i: i, j: j);
            }
            catch
            {
                point = find_next_point(flow_mats_u[t_idx], flow_mats_v[t_idx], point, i: i, j: j);
            }
            points_dic.Add((float)(t_idx), point);
        }

        List<(float, float)> vals_l = points_dic.Values.ToList();

        return vals_l[0];
    }

    /// <summary>
    /// Older version of the point tracking (unused).
    /// </summary>
    /// <param name="flow_mats_u">u maps per time step.</param>
    /// <param name="flow_mats_v">v maps per time step.</param>
    /// <param name="im_cnt">Number of images.</param>
    /// <returns>Match matrix.</returns>
    public List<List<(int, int)>> match_to_start_old(List<List<List<float>>> flow_mats_u,
        List<List<List<float>>> flow_mats_v, int im_cnt = -1)
    {
        // info (paul): find match matrices
        //25052024 List<List<List<(int, int)>>> match_mats = new List<List<List<(int, int)>>>();
        (float, float) point = (3.5f, 5.2f);// info (paul): or whatever your startpoint is

        if (im_cnt < 0)
        {
            im_cnt = flow_mats_u.Count;
        }
        for (int t_idx = 0; t_idx < im_cnt; t_idx++)
        {
            //25052024 List<List<(int, int)>> match_mat = match_at_t(flow_mats_u, flow_mats_v, t_idx);
            //25052024 match_mats.Add(match_mat);
            point = find_next_point(flow_mats_u[t_idx], flow_mats_v[t_idx], point);
        }

        List<List<(int, int)>> total_match = null; //25052024  find_total_match(match_mats);

        return total_match;
    }

    /// <summary>
    /// Values of the four neighbouring pixels and the fractional offsets of a point (for bilinear interpolation).
    /// </summary>
    /// <param name="mat">Map.</param>
    /// <param name="point_x">x position.</param>
    /// <param name="point_y">y position.</param>
    /// <returns>Tuple (v00, v01, v10, v11, rest_x, rest_y).</returns>
    public (float, float, float, float, float, float) find_interpolate_square(
        List<List<float>> mat, float point_x, float point_y)
    {
        // info (paul): find the points of the square around the point (point_x, point_y)
        // we assume, that point_x and point_y are > 0;

        float lower_x_f = (float)Math.Floor(point_x);
        float upper_x_f = (float)Math.Ceiling(point_x);
        float lower_y_f = (float)Math.Floor(point_y);
        float upper_y_f = (float)Math.Ceiling(point_y);

        int lower_x = (int)lower_x_f;//29052024 (int)(lower_x_f);
        int upper_x = (int)upper_x_f;//29052024 (int)(lower_x_f);
        int lower_y = (int)lower_y_f;//29052024 (int)(lower_x_f);
        int upper_y = (int)upper_y_f;//29052024 (int)(lower_x_f);

        int len_x = mat.Count;
        int len_y = mat[0].Count;
        bool x_in_frame = (0 <= lower_x) && (upper_x < len_x);
        bool y_in_frame = (0 <= lower_y) && (upper_y < len_y);

        (float val_00, float val_01, float val_10, float val_11)
            = (0f, 0f, 0f, 0f);
        (float rest_x, float rest_y) = (0f, 0f);
        if (x_in_frame && y_in_frame)
        {
            rest_x = point_x - lower_x_f;
            rest_y = point_y - lower_y_f;//29052024 point_x - lower_y_f;

            val_00 = mat[lower_x][lower_y];
            val_01 = mat[lower_x][upper_y];
            val_10 = mat[upper_x][lower_y];
            val_11 = mat[upper_x][upper_y];
        }
        else
        {
            float val_mean = extrapolate(mat, lower_x, upper_x, lower_y, upper_y);
            val_00 = -0.2f;//val_mean;
            val_01 = -0.2f;//val_mean;
            val_10 = -0.2f;//val_mean;
            val_11 = -0.2f;//val_mean;
        }

        return (val_00, val_01, val_10, val_11, rest_x, rest_y);
    }

    /// <summary>
    /// Checks whether a pixel lies inside the matrix.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <param name="point">Pixel (i, j).</param>
    /// <returns>True if inside.</returns>
    public bool check_if_in_frame(List<List<float>> mat, (int, int) point)
    {
        int len_x = mat.Count;
        int len_y = mat[0].Count;

        bool x_in_range = (0 <= point.Item1 && point.Item1 < len_x);
        bool y_in_range = (0 <= point.Item2 && point.Item2 < len_y);

        bool in_frame = x_in_range && y_in_range;

        return in_frame;
    }

    /// <summary>
    /// Rough extrapolation of a value outside the matrix from the nearest valid pixels.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <param name="lower_x">Lower x index.</param>
    /// <param name="upper_x">Upper x index.</param>
    /// <param name="lower_y">Lower y index.</param>
    /// <param name="upper_y">Upper y index.</param>
    /// <returns>Value.</returns>
    public float extrapolate(List<List<float>> mat, int lower_x, int upper_x, int lower_y, int upper_y)
    {
        // info (paul): a very rough way to interpolate, I am too lazy now to make it more complex, 
        //      probably also not so important.

        int len_x = mat.Count;
        int len_y = mat[0].Count;

        float val = float.NaN;

        bool in_frame_00 = check_if_in_frame(mat, (lower_x, lower_y));
        bool in_frame_01 = check_if_in_frame(mat, (lower_x, upper_y));
        bool in_frame_10 = check_if_in_frame(mat, (upper_x, lower_y));
        bool in_frame_11 = check_if_in_frame(mat, (upper_x, upper_y));

        if (in_frame_00)
        {
            val = mat[lower_x][lower_y];
        }
        if (in_frame_01)
        {
            val = mat[lower_x][upper_y];
        }
        if (in_frame_10)
        {
            val = mat[upper_x][lower_y];
        }
        if (in_frame_11)
        {
            val = mat[upper_x][upper_y];
        }

        /*if (lower_x < 0)
        {
            if (lower_y < 0)
            {
                ;
            }

            if (0 <= lower_y && lower_y < len_y)
            {
                ;
            }

            if (len_y <= lower_y)
            {
                ;
            }
        }

        if (len_x < lower_x)
        {
            if (lower_y < 0)
            {
                ;
            }

            if (0 <= lower_y && lower_y < len_x)
            {
                ;
            }

            if (len_x <= lower_y)
            {
                ;
            }
        }*/

        return val;

    }
    /// <summary>
    /// Bilinearly interpolated value at a sub-pixel position.
    /// </summary>
    /// <param name="mat">Map.</param>
    /// <param name="point_x">x position.</param>
    /// <param name="point_y">y position.</param>
    /// <returns>Value.</returns>
    public float interpolate_at(List<List<float>> mat, float point_x, float point_y)
    {
        // info (paul): get the linearly interpolated value at a point (point_x, point_y)

        (float val_00, float val_01, float val_10, float val_11,
            float rest_x, float rest_y) =
            find_interpolate_square(mat, point_x, point_y);

        float weight_00 = (1 - rest_x) * (1 - rest_y);
        float weight_01 = (1 - rest_x) * (rest_y);
        float weight_10 = (rest_x) * (1 - rest_y);
        float weight_11 = (rest_x) * (rest_y);

        float result = val_00 * weight_00 + val_01 * weight_01 + val_10 * weight_10 + val_11 * weight_11;
        return result;
    }

    /// <summary>
    /// Moves a point by the interpolated flow (u, v).
    /// </summary>
    /// <param name="mat_u">u map.</param>
    /// <param name="mat_v">v map.</param>
    /// <param name="point">Position.</param>
    /// <param name="i">Column (debugging).</param>
    /// <param name="j">Row (debugging).</param>
    /// <returns>New position.</returns>
    public (float, float) find_next_point(List<List<float>> mat_u, List<List<float>> mat_v, (float, float) point,
        int i = -1, int j = -1)
    {
        // info (paul): find next point by interpolation with u and v and so on

        // info (paul): find the next values by interpolating u and v
        float u_val = interpolate_at(mat_u, point.Item1, point.Item2);
        float v_val = interpolate_at(mat_v, point.Item1, point.Item2);

        float point_i = point.Item1;
        float point_j = point.Item2;

        float x_next = point_i + u_val;
        float y_next = point_j + v_val;

        return (x_next, y_next);
    }

    /// <summary>
    /// Legacy: chains integer match matrices over all time steps.
    /// </summary>
    /// <param name="match_mats">Match matrices per time step.</param>
    /// <returns>Total match matrix.</returns>
    public List<List<(int, int)>> find_total_match(List<List<List<(int, int)>>> match_mats)
    {
        // info (paul): calculate the global match_matrix and assign initial values
        List<List<(int, int)>> total_match = init_total_match_mat(match_mats);


        // info (paul): for each time step add this component
        for (int t = 0; t < match_mats.Count; t++)
        {
            total_match = make_next_match(match_mats, total_match, t);

        }

        return total_match;
    }

    /// <summary>
    /// Legacy: applies the match matrix of one time step to the total match.
    /// </summary>
    /// <param name="match_mats">Match matrices.</param>
    /// <param name="total_match">Total match.</param>
    /// <param name="t">Time step.</param>
    /// <returns>Updated total match.</returns>
    public List<List<(int, int)>> make_next_match(
        List<List<List<(int, int)>>> match_mats,
        List<List<(int, int)>> total_match, int t)
    {

        for (int i = 0; i < match_mats[0].Count; i++)
        {
            for (int j = 0; j < match_mats[0][0].Count; j++)
            {
                int i_now = total_match[i][j].Item1;
                int j_now = total_match[i][j].Item2;
                (int i_next, int j_next) = match_mats[t][i_now][j_now];
                total_match[i][j] = (i_next, j_next);
            }
        }
        return total_match;
    }

    /// <summary>
    /// Legacy: initialises the total match with the identity.
    /// </summary>
    /// <param name="match_mats">Match matrices.</param>
    /// <returns>Identity match matrix.</returns>
    public List<List<(int, int)>> init_total_match_mat(List<List<List<(int, int)>>> match_mats)
    {
        List<List<(int, int)>> total_match = zeros_like(match_mats[0]);
        for (int i = 0; i < match_mats[0].Count; i++)
        {
            for (int j = 0; j < match_mats[0][0].Count; j++)
            {
                total_match[i][j] = (i, j);
            }
        }
        return total_match;
    }
    /// <summary>
    /// Matrix of (0, 0) float tuples.
    /// </summary>
    /// <param name="len_x">Number of columns.</param>
    /// <param name="len_y">Number of rows.</param>
    /// <param name="type">Element type (float).</param>
    /// <returns>Zero matrix.</returns>
    public List<List<(float, float)>> zero_tuples_of_size(int len_x, int len_y, string type = "float")
    {
        List<List<(float, float)>> zeros = new List<List<(float, float)>>();
        for (int i = 0; i < len_x; i++)
        {
            zeros.Add(new List<(float, float)>());

            for (int j = 0; j < len_y; j++)
            {
                zeros[i].Add((0, 0));
            }
        }

        return zeros;
    }
    /// <summary>
    /// 3D array of (0, 0) float tuples.
    /// </summary>
    /// <param name="len_t">Number of time steps.</param>
    /// <param name="len_x">Number of columns.</param>
    /// <param name="len_y">Number of rows.</param>
    /// <param name="type">Element type (float).</param>
    /// <returns>Zero array.</returns>
    public List<List<List<(float, float)>>> zero_tuples_of_size(int len_t, int len_x, int len_y, string type = "float")
    {
        List<List<List<(float, float)>>> tuples = new List<List<List<(float, float)>>>();

        for (int t = 0; t < len_t; t++)
        {
            List<List<(float, float)>> zeros = new List<List<(float, float)>>();
            tuples.Add(zeros);

            for (int i = 0; i < len_x; i++)
            {
                zeros.Add(new List<(float, float)>());

                for (int j = 0; j < len_y; j++)
                {
                    zeros[i].Add((0, 0));
                }
            }
        }

        return tuples;
    }
    /// <summary>
    /// Matrix of (0, 0) integer tuples.
    /// </summary>
    /// <param name="len_x">Number of columns.</param>
    /// <param name="len_y">Number of rows.</param>
    /// <returns>Zero matrix.</returns>
    public List<List<(int, int)>> zero_tuples_of_size(int len_x, int len_y)
    {
        List<List<(int, int)>> zeros = new List<List<(int, int)>>();
        for (int i = 0; i < len_x; i++)
        {
            zeros.Add(new List<(int, int)>());

            for (int j = 0; j < len_y; j++)
            {
                zeros[i].Add((0, 0));
            }
        }

        return zeros;
    }
    /// <summary>
    /// Float matrix of zeros.
    /// </summary>
    /// <param name="len_x">Number of columns.</param>
    /// <param name="len_y">Number of rows.</param>
    /// <returns>Zero matrix.</returns>
    public List<List<float>> zeros_of_size(int len_x, int len_y)
    {
        List<List<float>> zeros = new List<List<float>>();
        for (int i = 0; i < len_x; i++)
        {
            zeros.Add(new List<float>());

            for (int j = 0; j < len_y; j++)
            {
                zeros[i].Add(0f);
            }
        }

        return zeros;
    }
    /// <summary>
    /// 3D float array of zeros.
    /// </summary>
    /// <param name="len_x">First dimension.</param>
    /// <param name="len_y">Second dimension.</param>
    /// <param name="len_z">Third dimension.</param>
    /// <returns>Zero array.</returns>
    public List<List<List<float>>> zeros_of_size(int len_x, int len_y, int len_z)
    {
        List<List<List<float>>> zeros = new List<List<List<float>>>();
        for (int i = 0; i < len_x; i++)
        {
            zeros.Add(new List<List<float>>());

            for (int j = 0; j < len_y; j++)
            {
                zeros[i].Add(new List<float>());
                for (int k = 0; k < len_z; k++)
                {
                    zeros[i][j].Add(0f);
                }
            }
        }

        return zeros;
    }
    /// <summary>
    /// Double matrix of zeros.
    /// </summary>
    /// <param name="len_x">Number of columns.</param>
    /// <param name="len_y">Number of rows.</param>
    /// <returns>Zero matrix.</returns>
    public List<List<double>> doubles_of_size(int len_x, int len_y)
    {
        List<List<double>> zeros = new List<List<double>>();
        for (int i = 0; i < len_x; i++)
        {
            zeros.Add(new List<double>());

            for (int j = 0; j < len_y; j++)
            {
                zeros[i].Add(0f);
            }
        }

        return zeros;
    }
    /// <summary>
    /// Legacy: integer match matrix of one time step from rounded flow.
    /// </summary>
    /// <param name="mats_u">u maps.</param>
    /// <param name="mats_v">v maps.</param>
    /// <param name="t_idx">Time index.</param>
    /// <returns>Match matrix.</returns>
    public List<List<(int, int)>> match_at_t(List<List<List<float>>> mats_u, List<List<List<float>>> mats_v, int t_idx)
    {
        // info (paul): 

        int width = mats_u[0].Count;
        int height = mats_v[0][0].Count;

        List<List<(int, int)>> match_t = zero_tuples_of_size(width, height);//new List<List<(int, int)>>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float u_val = mats_u[t_idx][i][j];
                float v_val = mats_v[t_idx][i][j];

                if (u_val > 0.5f)
                {
                    ;
                }
                if (v_val > 0.5f)
                {
                    ;
                }

                float i_pos_next = (float)i + u_val;
                float j_pos_next = (float)j + v_val;

                int i_idx_next = i;//(int)(i_pos_next);
                int j_idx_next = j;//(int)(j_pos_next);

                match_t[i][j] = (i_idx_next, j_idx_next);
            }
        }

        //match_step();
        return match_t;
    }

    /// <summary>
    /// Legacy: converts a disparity matrix into a pixel match matrix (unfinished).
    /// </summary>
    /// <param name="disps">Disparity matrix.</param>
    public void disp2match(List<List<float>> disps)
    {
        // info (paul): convert the disparity matrix into a matrix, 
        //      which matches each pixel to the corresponding other pixel


        for (int i = 0; i < disps.Count; i++)
        {
            for (int j = 0; j < disps[0].Count; j++)
            {
                ;
            }
        }
    }


    public (List<List<List<float>>>, List<List<List<float>>>, List<Texture2D>, List<Texture2D>,
        int, int) find_flow_mats(Dictionary<string, Dictionary<int, FileInfo>> flow_files,
        int im_cnt = -1)
    {
        List<List<List<float>>> mats_u = new List<List<List<float>>>();
        List<List<List<float>>> mats_v = new List<List<List<float>>>();

        int t_cnt = im_cnt;//16052024 flow_files["u"].Count;
        List<int> keys_u = flow_files["u"].Keys.ToList();
        List<int> keys_v = flow_files["v"].Keys.ToList();

        List<Texture2D> texs_albedo_u = new List<Texture2D>();// (res_x, res_y);
        List<Texture2D> texs_albedo_v = new List<Texture2D>();// (res_x, res_y);

        (int res_x, int res_y) = (-1, -1);

        List<int> available_keys = flow_files["u"].Keys
            .Where(key => flow_files["v"].ContainsKey(key))
            .OrderBy(key => key)
            .ToList();

        foreach (int key in available_keys)
        {
            List<List<float>> mat_u = load_flow_u(flow_files, keys_u, texs_albedo_u, blade_idx: key);

            //17072024 mat_u = filter_mean_comp(mat_u);//17072024 mean to smooth errors

            mats_u.Add(mat_u);

            List<List<float>> mat_v = load_flow_v(flow_files, keys_v, texs_albedo_v, blade_idx: key);

            //17072024 mat_v = filter_mean_comp(mat_v);

            //mat_v = take_share(mat_v);
            mats_v.Add(mat_v);

        }

        return (mats_u, mats_v, texs_albedo_u, texs_albedo_v, res_x, res_y);
    }

    /// <summary>
    /// Loads the u flow map of a time step (float copy .f32 if current, otherwise the PNG).
    /// </summary>
    /// <param name="flow_files">Assigned flow files.</param>
    /// <param name="keys_u">Time indices of the u files.</param>
    /// <param name="texs_albedo_u">Collects the loaded textures.</param>
    /// <param name="blade_idx">Time index.</param>
    /// <returns>u map.</returns>
    public List<List<float>> load_flow_u(Dictionary<string, Dictionary<int, FileInfo>> flow_files,
        List<int> keys_u, List<Texture2D> texs_albedo_u, int blade_idx = -1)
    {
        int key_v = blade_idx;
        string file_path_u = flow_files["u"][key_v].FullName;
        byte[] im_bytes_u = System.IO.File.ReadAllBytes(file_path_u);

        DirectoryInfo dir_v = new DirectoryInfo(path_time_flow_v);
        FileInfo[] dir_info_v = dir_v.GetFiles("*");

        // info (paul): assuming, that the resolution of the first image is 
        //      the resolution of all the images
        if (true)//20062024 (t_idx == 0)
        {
            (res_x, res_y) = bytes2res(im_bytes_u);
        }

        //27092026 verlustfreie Kopie (<png>.f32) bevorzugen: das PNG hat nur 256 Stufen (bei u = +-3.6 px
        //  ca. 0.03 px je Stufe), was die Dehnung (Ableitung) wellig macht
        Texture2D tex_albedo_u = read_flow_float_copy(file_path_u);
        if (tex_albedo_u == null)
        {
            tex_albedo_u = new Texture2D(res_x, res_y);
            tex_albedo_u.LoadImage(im_bytes_u);
        }
        texs_albedo_u.Add(tex_albedo_u);
        List<List<float>> mat_u = tex2mat(tex_albedo_u);

        // info (paul): find min and max val
        string experiment_dir = flow_files["u"][key_v].Directory.Parent.FullName;
        string min_max_file = Path.Combine(experiment_dir, "min_max_u_" + key_v.ToString() + ".txt");
        if (!File.Exists(min_max_file))
            min_max_file = Path.Combine(experiment_dir, "min_max_u_" + key_v.ToString()
                + "_r" + get_render_res().ToString() + ".txt");
        string min_max_str = load_txt_line(min_max_file);
        string[] strs = min_max_str.Split(" ");
        float min_val = float.Parse(strs[0]);
        float max_val = float.NaN;
        max_val = float.Parse(strs[1]);
        mat_u = unnorm_mat(mat_u, min_val, max_val);

        return mat_u;

    }

    /// <summary>
    /// Loads the v flow map of a time step (float copy .f32 if current, otherwise the PNG).
    /// </summary>
    /// <param name="flow_files">Assigned flow files.</param>
    /// <param name="keys_v">Time indices of the v files.</param>
    /// <param name="texs_albedo_v">Collects the loaded textures.</param>
    /// <param name="blade_idx">Time index.</param>
    /// <returns>v map.</returns>
    public List<List<float>> load_flow_v(Dictionary<string, Dictionary<int, FileInfo>> flow_files,
        List<int> keys_v, List<Texture2D> texs_albedo_v, int blade_idx = -1)
    {
        int key_v = blade_idx;
        string file_path_v = flow_files["v"][key_v].FullName;
        byte[] im_bytes_v = System.IO.File.ReadAllBytes(file_path_v);

        //27092026 verlustfreie Kopie bevorzugen (siehe load_flow_u)
        Texture2D tex_albedo_v = read_flow_float_copy(file_path_v);
        if (tex_albedo_v == null)
        {
            tex_albedo_v = new Texture2D(res_x, res_y);
            tex_albedo_v.LoadImage(im_bytes_v);
        }
        texs_albedo_v.Add(tex_albedo_v);
        List<List<float>> mat_v = tex2mat(tex_albedo_v);

        // info (paul): find min and max val
        string experiment_dir = flow_files["v"][key_v].Directory.Parent.FullName;
        string min_max_file = Path.Combine(experiment_dir, "min_max_v_" + key_v.ToString() + ".txt");
        if (!File.Exists(min_max_file))
            min_max_file = Path.Combine(experiment_dir, "min_max_v_" + key_v.ToString()
                + "_r" + get_render_res().ToString() + ".txt");
        string min_max_str = load_txt_line(min_max_file);
        string[] strs = min_max_str.Split(" ");
        float min_val = float.Parse(strs[0]);
        float max_val = float.Parse(strs[1]);

        mat_v = unnorm_mat(mat_v, min_val, max_val);

        return mat_v;
    }

    //27092026 Verlustfreie Kopie eines Flussfeld-PNGs: dieselben Farbwerte, die mat2tex in die 8-Bit-Textur
    //  schreibt (floats2col_mat + matrix2list), aber als float. Beim Laden wird daraus eine RGBAFloat-Textur,
    //  die denselben Weg (tex2mat, unnorm_mat) geht - gleiche Orientierung, nur ohne Rundung auf 256 Stufen.
    //  Gueltig nur, wenn nicht aelter als das PNG.
    const int FLOW_F32_MAGIC = 0x32334646; // "FF32"

    /// <summary>
    /// Writes a lossless float copy of a flow map next to the PNG (&lt;png&gt;.f32: magic, n, m, float32 values).
    /// </summary>
    /// <param name="mat">Flow map.</param>
    /// <param name="png_path">Path of the PNG.</param>
    void write_flow_float_copy(List<List<float>> mat, string png_path)
    {
        try
        {
            int w = mat.Count, h = mat[0].Count; // wie mat2tex: Texture2D(res_x = Count, res_y = [0].Count)
            UnityEngine.Color[] cols = matrix2list(floats2col_mat(mat), w, h, with_switch_dims: false);
            byte[] buf = new byte[12 + cols.Length * 16];
            Buffer.BlockCopy(BitConverter.GetBytes(FLOW_F32_MAGIC), 0, buf, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(w), 0, buf, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(h), 0, buf, 8, 4);
            float[] f = new float[cols.Length * 4];
            for (int k = 0; k < cols.Length; k++)
            {
                f[4 * k] = cols[k].r; f[4 * k + 1] = cols[k].g; f[4 * k + 2] = cols[k].b; f[4 * k + 3] = cols[k].a;
            }
            Buffer.BlockCopy(f, 0, buf, 12, f.Length * 4);
            System.IO.File.WriteAllBytes(png_path + ".f32", buf);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Verlustfreie Flusskopie nicht geschrieben (" + png_path + ".f32): " + e.Message);
        }
    }

    /// <summary>
    /// Reads the float copy of a flow map if it exists and is not older than the PNG.
    /// </summary>
    /// <param name="png_path">Path of the PNG.</param>
    /// <returns>Float texture, or null.</returns>
    Texture2D read_flow_float_copy(string png_path)
    {
        string path = png_path + ".f32";
        if (!File.Exists(path) || File.GetLastWriteTimeUtc(path) < File.GetLastWriteTimeUtc(png_path))
            return null;
        try
        {
            byte[] buf = File.ReadAllBytes(path);
            if (buf.Length < 12 || BitConverter.ToInt32(buf, 0) != FLOW_F32_MAGIC) return null;
            int w = BitConverter.ToInt32(buf, 4), h = BitConverter.ToInt32(buf, 8);
            if (buf.Length != 12 + w * h * 16) return null;
            float[] f = new float[w * h * 4];
            Buffer.BlockCopy(buf, 12, f, 0, f.Length * 4);
            UnityEngine.Color[] cols = new UnityEngine.Color[w * h];
            for (int k = 0; k < cols.Length; k++)
                cols[k] = new UnityEngine.Color(f[4 * k], f[4 * k + 1], f[4 * k + 2], f[4 * k + 3]);
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false);
            tex.SetPixels(cols);
            return tex;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads width and height from the header of PNG bytes.
    /// </summary>
    /// <param name="im_bytes_u">Image bytes.</param>
    /// <returns>Tuple (width, height).</returns>
    public (int, int) bytes2res(byte[] im_bytes_u)
    {
        // info (paul): test ints:
        // int intValue = 256;
        // byte[] intBytes = BitConverter.GetBytes(intValue);
        // Array.Reverse(intBytes);
        // byte[] result = intBytes;

        byte[] bytes_4 = { im_bytes_u[0], im_bytes_u[1], im_bytes_u[2], im_bytes_u[3] };

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes_4);
        }

        int result_2 = BitConverter.ToInt32(bytes_4);

        byte[] width_bytes = { im_bytes_u[16], im_bytes_u[17], im_bytes_u[18], im_bytes_u[19] };
        byte[] height_bytes = { im_bytes_u[20], im_bytes_u[21], im_bytes_u[22], im_bytes_u[23] };

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(width_bytes);
            Array.Reverse(height_bytes);
        }

        int res_x = BitConverter.ToInt32(width_bytes);
        int res_y = BitConverter.ToInt32(height_bytes);
        return (res_x, res_y);
    }

    /// <summary>
    /// Computes the strain map from a displacement map (derivatives in the object plane, optionally with the height component).
    /// </summary>
    /// <param name="mat_pre">Displacement map.</param>
    /// <param name="heights_chosen">Height map.</param>
    /// <param name="stream_z">Out-of-plane component.</param>
    /// <returns>Strain map.</returns>
    public List<List<float>> calc_strain(List<List<float>> mat_pre,
        List<List<float>> heights_chosen, List<List<float>> stream_z)
    {
        // info (paul): uncut version is calc_strain_copy

        // info (paul): calculate e.g. the strain field from the
        //      disparities or whatever is most convenient

        // info (paul): getting texture pixels as float-matrix (red colorchannel)
        //20102024 List<List<float>> mat = tex2mat(mat_displayed, with_switch_dims: false);

        // info (paul): If we look for 3rd coordinate strain, use heights instead of mat:
        if (get_u_v_mode() == "z")
        {
            mat_pre = heights_chosen;
        }

        // info (paul): make the screen correction; should be used, only to the already calculated 
        //          flow or difference or sth like that
        List<List<float>> mat_pre_unproj = unproj_flow(mat_pre);
        write_mat_for_debug(mat_pre_unproj, scale: 1f);

        // info (paul): Do physics filter, e.g. strain field etc. (mat_1, mat_2 are strain derivatives)
        (List<List<float>> mat_x, List<List<float>> mat_y) = find_physics_filter(mat_pre_unproj,
            stream_z);
        write_mat_for_debug(mat_x, scale: 1f);
        write_mat_for_debug(mat_y, scale: 1f);

        // info (paul): converting back floats-matrix to colors and assign to texture
        // info (paul): choose, whether to display u or v

        List<List<float>> mat_displayed = mat_x;
        if (strain_d_mode == "x")
        {
            mat_displayed = mat_x;
        }
        if (strain_d_mode == "y")
        {
            mat_displayed = mat_y;
        }

        //17032025 for (int i = 0; i < 1; i++)
        //17032025 {
        //17032025     mat_displayed = filter_mean_comp(mat_displayed);
        //17032025 }

        //20102024 Texture2D tex_out = mat2tex(mat_displayed, with_switch_dims: false);
        return mat_displayed;
    }

    /// <summary>
    /// Converts image-plane displacements (pixels) to object-plane displacements with the field of view.
    /// </summary>
    /// <param name="mat_pre">Displacement map in pixels.</param>
    /// <returns>Displacement map in object units.</returns>
    public List<List<float>> unproj_flow(List<List<float>> mat_pre)
    {
        List<List<float>> unproj = copy_mat(mat_pre);

        // info (paul): concerning field of view, we assume, that the
        //      camera / targettexture
        //      aspect ratio is 1:1
        int res_x = mat_pre.Count;
        int res_y = mat_pre[0].Count;
        for (int i = 0; i < res_x; i++)
        {
            for (int j = 0; j < res_y; j++)
            {
                float fov_degs_vert = cam_for_uv_0.fieldOfView / 2f;
                float fov_degs_hori = cam_for_uv_0.fieldOfView / 2f;

                float frac_hori = (i - 0.5f * res_x) / (float)res_x;
                float frac_vert = (j - 0.5f * res_y) / (float)res_y;

                float degs_x = frac_hori * fov_degs_vert;
                float degs_y = frac_vert * fov_degs_hori;

                float rad_x = degs_x * Mathf.PI / 180f;
                float rad_y = degs_y * Mathf.PI / 180f;

                float flow = mat_pre[i][j];
                if (flow != 0f)
                {
                    ;
                }
                if (Math.Abs(flow) > 1f)
                {
                    ;
                }
                float flow_unproj = flow / Mathf.Cos(rad_x) / Mathf.Cos(rad_y);
                unproj[i][j] = flow_unproj;
            }
        }

        return unproj;
    }

    //12062024 public List<List<float>> randomize_mat(List<List<float>> mat)
    //12062024 {
    //12062024     res_x = mat.Count;
    //12062024     res_y = mat[0].Count;
    //12062024     for (int i = 0; i < res_x; i++)
    //12062024     {
    //12062024         for (int j = 0; j < res_y; j++)
    //12062024         {
    //12062024             mat[i][j] = UnityEngine.Random.Range(0f, 1f);
    //12062024         }
    //12062024     }
    //12062024     return mat;
    //12062024 }
    //public Texture2D mat2tex(List<List<float>> mat)
    //{
    //    int res_x = mat.Count;
    //    int res_y = mat[0].Count;
    //    
    //    Texture2D tex = new Texture2D(res_x, res_y);
    //
    //    UnityEngine.Color[,] cols_mat = floats2col_mat(mat);//22052024 mat_displayed//mat_1
    //    UnityEngine.Color[] cols_1d = matrix2list(cols_mat, res_x, res_y, marker: "tex");
    //    tex.SetPixels(cols_1d);
    //    tex.Apply();
    //    return tex;
    //}

    /// <summary>
    /// Older texture-based strain computation (unused).
    /// </summary>
    /// <param name="tex_input">Displacement texture.</param>
    /// <returns>Strain texture.</returns>
    public Texture2D calc_strain_copy(Texture2D tex_input)
    {
        // info (paul): calculate e.g. the strain field from the
        //      disparities or whatever is most convenient

        Texture2D tex = new Texture2D(tex_input.width, tex_input.height);
        int res_x = tex_input.width;
        int res_y = tex_input.height;

        // info (paul): getting texture pixels as float-matrix (red colorchannel)
        //15052024 Color[] cols = tex.GetPixels(0, 0, res_x, res_y);
        //15052024 Color[,] cols_mat = list2matrix(cols, res_x, res_y);
        //15052024 List<List<float>> mat = cols_mat2floats_mat(cols_mat);
        List<List<float>> mat = tex2mat(tex_input);//15052024 , res_x, res_y);

        // info (paul): Do physics filter, e.g. strain field etc. (mat_1, mat_2 are strain derivatives)
        (List<List<float>> mat_1, List<List<float>> mat_2) = find_physics_filter(mat, zeros_like(mat));

        // info (paul): converting back floats-matrix to colors and assign to texture

        // info (paul): choose, whether to display u or v
        List<List<float>> mat_displayed = mat_1;
        if (strain_d_mode == "x")
        {
            mat_displayed = mat_1;
        }
        if (strain_d_mode == "y")
        {
            mat_displayed = mat_2;
        }

        UnityEngine.Color[,] cols_mat = floats2col_mat(mat_2);//22052024 mat_displayed//mat_1
        UnityEngine.Color[] cols_1d = matrix2list(cols_mat, res_x, res_y, marker: "tex", with_switch_dims: true);
        tex.SetPixels(cols_1d);
        tex.Apply();

        return tex;
    }

    /// <summary>
    /// Converts a matrix to a float texture.
    /// </summary>
    /// <param name="mat_1">Matrix.</param>
    /// <param name="with_switch_dims">True to swap the dimensions.</param>
    /// <returns>Texture.</returns>
    public Texture2D mat2tex(List<List<float>> mat_1, bool with_switch_dims = false)
    {
        // perh. TODO: at some point make dim_switch false by default and not true

        int res_x = mat_1.Count;
        int res_y = mat_1[0].Count;

        Texture2D tex = new Texture2D(res_x, res_y);
        UnityEngine.Color[,] cols_mat = floats2col_mat(mat_1);
        UnityEngine.Color[] cols_1d = matrix2list(cols_mat, res_x, res_y,
            with_switch_dims: with_switch_dims);//15052024 res_x, res_y
        tex.SetPixels(cols_1d);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Converts a flat list to a texture.
    /// </summary>
    /// <param name="floats">Values.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <returns>Texture.</returns>
    public Texture2D floats2tex(List<float> floats, int width, int height)
    {
        Color[] cols = new Color[width * height];

        for (int i = 0; i < floats.Count; i++)
        {
            cols[i] = new Color(floats[i] / 255f, 0f, 0f, 1f);
        }

        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(0, 0, width, height, cols);

        return tex;
    }

    /// <summary>
    /// Reads one colour channel of a texture as a flat list.
    /// </summary>
    /// <param name="tex">Texture.</param>
    /// <param name="with_switch_dims">True to swap the dimensions.</param>
    /// <param name="color_channel">Channel index.</param>
    /// <returns>Values.</returns>
    public List<float> tex2floats(Texture2D tex, bool with_switch_dims = true, int color_channel = 0)//15052024 , int res_x, int res_y)
    {
        int res_x = tex.width;
        int res_y = tex.height;

        UnityEngine.Color[] cols = tex.GetPixels(0, 0, res_x, res_y);
        if (with_switch_dims)
        {
            cols = switch_dims(cols, res_y, res_x);
        }

        // info (paul): cols to floats:
        List<float> floats = new List<float>();
        for (int i = 0; i < cols.Length; i++)
        {
            //floats.Add(cols[i].r);
            floats.Add(cols[i][color_channel]);
        }

        return floats;
    }
    /// <summary>
    /// Reads a texture as a matrix.
    /// </summary>
    /// <param name="tex">Texture.</param>
    /// <param name="with_switch_dims">True to swap the dimensions.</param>
    /// <param name="for_im">True for image gray values.</param>
    /// <returns>Matrix.</returns>
    public List<List<float>> tex2mat(Texture2D tex, bool with_switch_dims = true, 
        bool for_im = false)//15052024 , int res_x, int res_y)
    {
        int res_x = tex.width;
        int res_y = tex.height;

        UnityEngine.Color[] cols = tex.GetPixels(0, 0, res_x, res_y);
        if (with_switch_dims)
        {
            cols = switch_dims(cols, res_y, res_x);
        }

        UnityEngine.Color[,] cols_mat = list2matrix(cols, res_x, res_y);
        List<List<float>> mat = null;
        if (for_im)
        {
            mat = cols_mat2floats_mat_for_im(cols_mat);
        }
        else
        {
            mat = cols_mat2floats_mat(cols_mat);
        }
        return mat;
    }
    /// <summary>
    /// Sum of the red channel of a colour matrix.
    /// </summary>
    /// <param name="cols">Colour matrix.</param>
    /// <returns>Sum.</returns>
    public float sum_cols_r(UnityEngine.Color[,] cols)
    {
        float sum = 0f;
        for (int i = 0; i < cols.GetLength(0); i++)
        {
            for (int j = 0; j < cols.GetLength(1); j++)
            {
                sum += cols[i, j].r;
            }
        }

        return sum;
    }

    /// <summary>
    /// Derivatives of a map in x and y.
    /// </summary>
    /// <param name="mat">Map.</param>
    /// <param name="stream_z">Out-of-plane component.</param>
    /// <returns>Tuple (d/dx, d/dy).</returns>
    public (List<List<float>>, List<List<float>>) find_physics_filter(List<List<float>> mat,
        List<List<float>> stream_z)
    {
        // info (paul): AA AA
        List<List<float>> mat_1 = derive_x(mat, stream_z);
        List<List<float>> mat_2 = derive_y(mat, stream_z);

        // info (paul): norm:
        //18112024 mat_1 = norm_mat(mat_1);
        //18112024 mat_2 = norm_mat(mat_2);
        return (mat_1, mat_2);
    }

    /// <summary>
    /// Mean and standard deviation of a map in the inner area (without border, optional coverage limit).
    /// </summary>
    /// <param name="mat">Map.</param>
    /// <param name="span">Size of the evaluation window.</param>
    /// <param name="only_meaningful">True to ignore NaN and infinite values.</param>
    /// <param name="coverage">Share of the image to evaluate (NaN = default).</param>
    /// <param name="padding">Border width.</param>
    /// <returns>Tuple (mean, std).</returns>
    public (float, float) find_mean_in_all(List<List<float>> mat, int span = 20,
        bool only_meaningful = true, float coverage = float.NaN, int padding = 20)
    {
        // info (paul): "only_meaningful" says, that Infinity or NaN values will
        //      be excluded from min-max calculation (as it would usually make sense,
        //      except you have some special situation perhaps)

        float max_val = -999999f;
        float min_val = 999999f;
        float sum = 0f;
        int cnt = 0;

        int center_x = mat.Count / 2;
        int center_y = mat.Count / 2;

        int i_off = 0;
        int j_off = 50;
        //int padding = 20;

        int i_min = 0 + padding;//23092024 center_x + i_off - span;
        int i_max = mat.Count - padding;//23092024 center_x + i_off + span;
        int j_min = 0 + padding;//23092024 center_y + j_off - span;
        int j_max = mat.Count - padding;//23092024 center_y + j_off + span;

        int strange_cnt = 0;

        List<List<float>> plaquette = zeros_of_size(i_max - i_min, j_max - j_min);
        for (int i = i_min; i < i_max; i++)
        {
            for (int j = j_min; j < j_max; j++)
            {
                bool use_val = !only_meaningful || (only_meaningful && is_meaningful(mat[i][j]));
                if (use_val)
                {
                    float mat_ij = mat[i][j];
                    plaquette[i - i_min][j - j_min] = mat[i][j];
                    if (Mathf.Abs(mat_ij) > 0.0000f)//05032025 0.001f)//23092024 (Mathf.Abs(mat_ij) < 2f)
                    {
                        float mat_ij_abs = Math.Abs(mat[i][j]);
                        sum += mat_ij_abs;//Math.Abs(mat[i][j])
                        cnt += 1;

                        if (mat_ij_abs > 2f)
                        {
                            ;
                        }
                        //mat[i][j] = 1f;//for debugging
                    }
                    else
                    {
                        strange_cnt += 1;
                    }
                }
            }
        }

        float mean = sum / ((float)(cnt));
        float sq_mean = find_mat_std_2(mat, mean, only_meaningful, i_min, i_max, j_min, j_max);

        // info (paul): take into account the coverage thing
        //11112024 float mean_covered = mean / coverage;
        //11112024 float sq_mean_covered = sq_mean / coverage;

        return (mean, sq_mean); //11112024 (mean_covered, sq_mean_covered);
    }
    /// <summary>
    /// Mean and standard deviation of a map in a window.
    /// </summary>
    /// <param name="mat">Map.</param>
    /// <param name="span">Window size.</param>
    /// <param name="only_meaningful">True to ignore NaN and infinite values.</param>
    /// <param name="j_off">Row offset of the window.</param>
    /// <returns>Tuple (mean, std).</returns>
    public (float, float) find_mean_in_span(List<List<float>> mat, int span = 20, bool only_meaningful = true, int j_off = 50)
    {
        // info (paul): "only_meaningful" says, that Infinity or NaN values will
        //      be excluded from min-max calculation (as it would usually make sense,
        //      except you have some special situation perhaps)

        float max_val = -999999f;
        float min_val = 999999f;
        float sum = 0f;
        int cnt = 0;

        int center_x = mat.Count / 2;
        int center_y = mat.Count / 2;

        int i_off = 0;
        //int j_off = 50;

        int i_min = center_x + i_off - span;
        int i_max = center_x + i_off + span;
        int j_min = center_y + j_off - span;
        int j_max = center_y + j_off + span;

        int strange_cnt = 0;

        List<List<float>> plaquette = zeros_of_size(i_max - i_min, j_max - j_min);
        for (int i = i_min; i < i_max; i++)
        {
            for (int j = j_min; j < j_max; j++)
            {
                bool use_val = !only_meaningful || (only_meaningful && is_meaningful(mat[i][j]));
                if (use_val)
                {
                    float mat_ij = mat[i][j];
                    plaquette[i - i_min][j - j_min] = mat[i][j];
                    if (mat_ij < 2f)
                    {
                        sum += Math.Abs(mat[i][j]);
                        cnt += 1;
                        //mat[i][j] = 0.3f;//for debugging
                    }
                    else
                    {
                        strange_cnt += 1;
                    }
                }
            }
        }

        float mean = sum / ((float)(cnt));
        float sq_mean = find_mat_std_2(mat, mean, only_meaningful, i_min, i_max, j_min, j_max);

        return (mean, sq_mean);
    }

    /// <summary>
    /// Standard deviation of a map in a rectangle.
    /// </summary>
    /// <param name="mat">Map.</param>
    /// <param name="mean">Mean.</param>
    /// <param name="only_meaningful">True to ignore NaN and infinite values.</param>
    /// <param name="i_min">First column.</param>
    /// <param name="i_max">Last column.</param>
    /// <param name="j_min">First row.</param>
    /// <param name="j_max">Last row.</param>
    /// <returns>Standard deviation.</returns>
    public float find_mat_std_1(List<List<float>> mat, float mean, bool only_meaningful,
        int i_min, int i_max, int j_min, int j_max)
    {
        float sq_sum = 0f;
        int cnt = 0;

        for (int i = i_min; i < i_max; i++)
        {
            for (int j = j_min; j < j_max; j++)
            {
                bool use_val = !only_meaningful || (only_meaningful && is_meaningful(mat[i][j]));
                if (use_val)
                {
                    // info (paul): We assume that mean is always positive, since during 
                    //      its calculation we used mean
                    sq_sum += Math.Abs(Mathf.Abs(mat[i][j]) - mean);
                    cnt += 1;
                }
            }
        }

        float sq_mean = sq_sum / ((float)(cnt));
        return sq_mean;
    }
    /// <summary>
    /// Standard deviation of a map in a rectangle (variant).
    /// </summary>
    /// <param name="mat">Map.</param>
    /// <param name="mean">Mean.</param>
    /// <param name="only_meaningful">True to ignore NaN and infinite values.</param>
    /// <param name="i_min">First column.</param>
    /// <param name="i_max">Last column.</param>
    /// <param name="j_min">First row.</param>
    /// <param name="j_max">Last row.</param>
    /// <returns>Standard deviation.</returns>
    public float find_mat_std_2(List<List<float>> mat, float mean, bool only_meaningful,
    int i_min, int i_max, int j_min, int j_max)
    {
        float sq_sum = 0f;
        int cnt = 0;

        for (int i = i_min; i < i_max; i++)
        {
            for (int j = j_min; j < j_max; j++)
            {
                bool use_val = !only_meaningful || (only_meaningful && is_meaningful(mat[i][j]));
                if (use_val)
                {
                    // info (paul): We assume that mean is always positive, since during 
                    //      its calculation we used mean
                    sq_sum += Mathf.Pow(Mathf.Abs(mat[i][j]) - mean, 2);
                    cnt += 1;
                }
            }
        }

        float sq_mean = sq_sum / ((float)(cnt));
        float std = Mathf.Sqrt(sq_mean);
        return sq_mean;
    }
    /// <summary>
    /// Minimum of a list (optionally of a section).
    /// </summary>
    /// <param name="u">Values.</param>
    /// <param name="n_x">Width of the section.</param>
    /// <param name="n_y">Height of the section.</param>
    /// <param name="offset">Start index.</param>
    /// <returns>Minimum.</returns>
    float find_min(List<float> u, int n_x = -1, int n_y = -1, int offset = 0)
    {
        List<float> u_taken = u;
        if (n_x >= 0 || n_y >= 0)
        {
            u_taken = u.Skip(offset).Take(n_x * n_y).ToList();
        }
        float min_val = u_taken.Min();
        return min_val;
    }

    /// <summary>
    /// Minimum of a jagged array.
    /// </summary>
    /// <param name="jaggedArray">Array.</param>
    /// <returns>Minimum.</returns>
    float find_min(float[][] jaggedArray)
    {
        float minValue = float.MaxValue; // 1. Start with the LARGEST possible value

        foreach (float[] innerArray in jaggedArray)
        {
            if (innerArray == null) continue;

            foreach (float value in innerArray)
            {
                // 2. Check if the current value is SMALLER than our record
                if (value < minValue)
                {
                    minValue = value; // 3. Update the record
                }
            }
        }
        return minValue;
    }
    /// <summary>
    /// Maximum of a jagged array.
    /// </summary>
    /// <param name="jaggedArray">Array.</param>
    /// <returns>Maximum.</returns>
    float find_max(float[][] jaggedArray)
    {
        // info (paul): max of float[][]
        float maxValue = float.MinValue; // Start with the smallest possible value

        foreach (float[] innerArray in jaggedArray)
        {
            if (innerArray == null) continue; // Safety check for null rows

            foreach (float value in innerArray)
            {
                if (value > maxValue)
                {
                    maxValue = value;
                }
            }
        }
        return maxValue;
    }

    /// <summary>
    /// Maximum of a list (optionally of a section).
    /// </summary>
    /// <param name="u">Values.</param>
    /// <param name="n_x">Width of the section.</param>
    /// <param name="n_y">Height of the section.</param>
    /// <param name="offset">Start index.</param>
    /// <returns>Maximum.</returns>
    float find_max(List<float> u, int n_x = -1, int n_y = -1, int offset = 0)
    {
        List<float> u_taken = u;
        if (n_x >= 0 || n_y >= 0)
        {
            u_taken = u.Skip(offset).Take(n_x * n_y).ToList();
        }
        float max_val = u_taken.Max();
        return max_val;
    }
    /// <summary>
    /// Minimum and maximum of a list.
    /// </summary>
    /// <param name="mat">Values.</param>
    /// <param name="only_meaningful">True to ignore NaN and infinite values.</param>
    /// <returns>Tuple (min, max).</returns>
    public (float, float) find_min_max(List<float> mat, bool only_meaningful = true)
    {
        // info (paul): "only_meaningful" says, that Infinity or NaN values will
        //      be excluded from min-max calculation (as it would usually make sense,
        //      except you have some special situation perhaps)

        float max_val = -999999f;
        float min_val = 999999f;
        for (int i = 0; i < mat.Count; i++)
        {
            float mat_ij = mat[i];
            bool use_val = !only_meaningful || (only_meaningful && is_meaningful(mat[i]));
            if (use_val)
            {
                if (max_val < mat_ij)
                {
                    ;
                }
                max_val = Mathf.Max(max_val, mat_ij);

            }
            if (use_val)
            {
                if (min_val > mat_ij)
                {
                    ;
                }
                min_val = Mathf.Min(min_val, mat_ij);
            }
        }

        return (min_val, max_val);
    }
    /// <summary>
    /// Minimum and maximum of a matrix.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <param name="only_meaningful">True to ignore NaN and infinite values.</param>
    /// <param name="with_padding">True to ignore the border.</param>
    /// <param name="lower_floor">Values below are ignored.</param>
    /// <returns>Tuple (min, max).</returns>
    public (float, float) find_min_max(List<List<float>> mat, bool only_meaningful = true,
        bool with_padding = false, float lower_floor = -999999999f)
    {
        // info (paul): "only_meaningful" says, that Infinity or NaN values will
        //      be excluded from min-max calculation (as it would usually make sense,
        //      except you have some special situation perhaps)

        int padding = 0;
        if (with_padding)
        {
            padding = 10;//25092024
        }

        float max_val = -999999f;
        float min_val = 999999f;
        for (int i = padding; i < mat.Count - padding; i++)
        {
            for (int j = padding; j < mat[0].Count - padding; j++)
            {
                float mat_ij = mat[i][j];
                bool use_val = !only_meaningful || (only_meaningful && is_meaningful(mat[i][j]));
                if (use_val)
                {
                    max_val = Mathf.Max(max_val, mat_ij);
                }
                if (use_val)
                {
                    if (lower_floor < mat_ij)
                    {
                        min_val = Mathf.Min(min_val, mat_ij);
                    }
                    //min_val = Mathf.Max(min_val, lower_floor);
                }
                //float val = (mat[i][j + 1] - mat[i][j - 1]) / 2f;
                //mat_new[i][j] = val;
            }
        }

        return (min_val, max_val);
    }

    /// <summary>
    /// Scales a normalised matrix back to the range depth_min..depth_max.
    /// </summary>
    /// <param name="mat">Normalised matrix.</param>
    /// <param name="depth_min">Lower bound.</param>
    /// <param name="depth_max">Upper bound.</param>
    /// <param name="mode">Scaling mode.</param>
    /// <returns>Scaled matrix.</returns>
    public List<List<float>> unnorm_mat(List<List<float>> mat, float depth_min, float depth_max, string mode = "normal")
    {
        // info (paul): kind of the reverse of norming a matrix: We scale it up again to the scale from depth_min to depth_max
        //          But: This does only the normal scaling, not this special +/- thing, which norm_mat includes

        List<List<float>> mat_new = mat_like(mat);

        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < mat[0].Count; j++)
            {
                float mat_el = mat[i][j];

                if (mode == "normal")
                {
                    // info (paul): normalization, so that 0 remains 0 and values of the one sign are just cut off
                    float mat_el_new = -1f;
                    mat_el_new = mat_el * (depth_max - depth_min) + depth_min;
                    mat_new[i][j] = mat_el_new;
                }
                else if (mode == "plus_minus")
                {
                    // info (paul): normalization, so that 0 remains 0 and values of the one sign are just cut off


                    if (depth_min < 0 && depth_max > 0)
                    {
                        // info (paul): If there are positive and neg. values, use this special norming strategy
                        float mat_el_new = -1f;
                        if (mat_el >= 0)
                        {
                            mat_el_new = mat_el * depth_max;//26112024 
                        }
                        else
                        {
                            mat_el_new = mat_el * (-depth_min);//26112024 
                        }
                        mat_new[i][j] = mat_el_new;
                    }
                    else
                    {
                        float mat_el_new = mat_el * (depth_max - depth_min) + depth_min;
                        mat_new[i][j] = mat_el_new;
                    }
                }
            }
        }
        return mat_new;
    }


    /// <summary>
    /// Normalises a matrix to 0..1 (optionally with custom bounds).
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <param name="lower">Custom lower bound (NaN = minimum).</param>
    /// <param name="upper">Custom upper bound (NaN = maximum).</param>
    /// <param name="lower_floor">Values below are ignored.</param>
    /// <returns>Normalised matrix.</returns>
    public List<List<float>> norm_mat(List<List<float>> mat, float lower = float.NaN,
        float upper = float.NaN, float lower_floor = -999999999f)
    {
        // info (paul): lower and upper are a possibility to overwrite min_val and 
        //      and max_val and "norm" with respect to custom scale

        List<List<float>> mat_new = mat_like(mat);

        // info (paul): getting min_val and max_val; overwrite if input not NaN
        (float min_val, float max_val) = find_min_max(mat, with_padding: true, lower_floor: lower_floor);
        if (!float.IsNaN(lower))
        {
            min_val = lower;
        }
        if (!float.IsNaN(upper))
        {
            max_val = upper;
        }

        // info (paul): If they are 0, then avoid division-by-zero, 
        //      by giving a value
        if (max_val == 0f)
        {
            max_val = 1f;
        }
        if (min_val == 0f)
        {
            min_val = 0.1f;//14072024 1f;
        }

        //min_val = 0f; // for debugging

        // info (paul): actual norming
        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < mat[0].Count; j++)
            {
                float mat_el = mat[i][j];
                //13052024 max_val = Mathf.Max(max_val, mat[i][j]);
                //float val = (mat[i][j + 1] - mat[i][j - 1]) / 2f;

                // info (paul): normal normalization
                //float mat_el_new = (mat_el - min_val) / (max_val - min_val);
                //mat_new[i][j] = (1 - mat_el_new); // info (paul): Let's just scale it up by sth, for debugging reasons

                // info (paul): normalization, so that 0 remains 0 and values of the one sign are just cut off
                float mat_el_new = -1f;

                if (min_val < 0 && max_val > 0)
                {
                    // info (paul): If there are positive and neg. values, use this special norming strategy
                    if (mat_el >= 0)
                    {
                        mat_el_new = (mat_el) / (max_val);//26112024 
                    }
                    else
                    {
                        mat_el_new = (mat_el) / (-min_val);//26112024 
                    }
                }
                else
                {
                    // info (paul): Else, use a more "normal" norming strategy

                    mat_el_new = (mat_el - min_val) / (max_val - min_val);
                }

                mat_new[i][j] = mat_el_new;
            }
        }

        return mat_new;
    }

    /// <summary>
    /// Zero matrix with the shape of the input.
    /// </summary>
    /// <param name="mat">Reference matrix.</param>
    /// <returns>Zero matrix.</returns>
    public List<List<float>> mat_like(List<List<float>> mat)
    {
        List<List<float>> floats = new List<List<float>>();

        for (int i = 0; i < mat.Count; i++)
        {
            floats.Add(new List<float>());
            for (int j = 0; j < mat[0].Count; j++)
            {
                floats[i].Add(0f);
            }
        }

        return floats;
    }

    /// <summary>
    /// Central difference in x of a map.
    /// </summary>
    /// <param name="mat">Map.</param>
    /// <param name="stream_z">Out-of-plane component.</param>
    /// <returns>Derivative.</returns>
    public List<List<float>> derive_x(List<List<float>> mat, List<List<float>> stream_z)
    {
        List<List<float>> mat_new = mat_like(mat);

        int res_x = mat.Count;
        int res_y = mat[0].Count;

        for (int i = 0; i < res_x; i++)
        {
            for (int j = 0; j < res_y; j++)
            {
                bool j_lower = (j - 1 >= 0);
                bool j_upper = (j + 1 <= mat[0].Count - 1);
                bool j_ok = j_lower && j_upper;

                if (j_ok)
                {
                    float log_val = find_d_x_val(mat, i, j, stream_z);
                    float i_share_2 = (float)(i % 2);
                    float i_share = (float)(i) / ((float)res_x);
                    float j_share = (float)(j) / ((float)res_y);

                    mat_new[i][j] = log_val;//22052024 j_share;//22052024 log_val;
                }
            }
        }

        return mat_new;
    }

    /// <summary>
    /// Central difference in y of a map.
    /// </summary>
    /// <param name="mat">Map.</param>
    /// <param name="stream_z">Out-of-plane component.</param>
    /// <returns>Derivative.</returns>
    public List<List<float>> derive_y(List<List<float>> mat, List<List<float>> stream_z)
    {
        List<List<float>> mat_new = mat_like(mat);

        int res_x = mat.Count;
        int res_y = mat[0].Count;

        write_mat_for_debug(mat, scale: 1f);
        for (int i = 0; i < res_x; i++)
        {
            for (int j = 0; j < res_y; j++)
            {
                bool i_lower = (i - 1 >= 0);
                bool i_upper = (i + 1 <= mat.Count - 1);
                bool i_ok = i_lower && i_upper;

                if (i_ok)//22052024 (i_ok)
                {
                    float i_share_2 = (float)(i % 2);
                    float i_share = (float)(i) / ((float)res_x);
                    float j_share = (float)(j) / ((float)res_y);

                    //25052024 mat_new[i][j] = (mat[i + 1][j] - mat[i - 1][j]) / 2f;//i_share;//;//22052024 j_share;//22052024 (mat[i + 1][j] - mat[i - 1][j]) / 2f;
                    mat_new[i][j] = find_d_y_val(mat, i, j, stream_z);

                }
            }
        }
        write_mat_for_debug(mat_new, scale: 1f);
        return mat_new;
    }

    /// <summary>
    /// Derivative in y at one pixel (optionally along the surface using the height component).
    /// </summary>
    /// <param name="mat">Map.</param>
    /// <param name="i">Column.</param>
    /// <param name="j">Row.</param>
    /// <param name="stream_z">Out-of-plane component.</param>
    /// <returns>Derivative (NaN at invalid pixels).</returns>
    public float find_d_y_val(List<List<float>> mat, int i, int j, List<List<float>> stream_z)
    {
        // info (paul): pure 2D
        // 16032025 float val = (mat[i + 1][j] - mat[i - 1][j]) / 2f;
        float val_return = float.NaN;

        if (category_muc == "gom_curve")
        {
            //} info (paul): pure 3D

            float l_standard = 2f * this.cam_0.orthographicSize / get_render_res();//2f;
            float dist_hori = mat[i + 1][j] - mat[i - 1][j];//17032025   + 2f;
            float dist_z = stream_z[i + 1][j] - stream_z[i - 1][j];
            //float dist_z = 0f;
            //17032025 float val = Mathf.Sqrt(dist_hori*dist_hori + dist_z*dist_z)/2f + 1f;//17032025  // - 1f;
            float val = dist_hori + Mathf.Abs(dist_z);

            val = dist_hori;
            if (check_diag())
            {
                //val += Mathf.Abs(dist_z);

                val = Mathf.Sqrt((dist_hori + l_standard) * (dist_hori + l_standard) + dist_z * dist_z) / l_standard - 1f;
                //val = Mathf.Sqrt(dist_hori*dist_hori + dist_z*dist_z)/2f + 1f;
            }
            val_return = val;
        }

        if (category_muc == "simple")
        {
            float val = (mat[i + 1][j] - mat[i - 1][j]) / 2f + 1f;

            // info (paul): take logarithm
            //mat_new[i][j] = Mathf.Log(mat_new[i][j]);
            float log_val = 0f;
            if (val < 0)
            {
                log_val = -Mathf.Log(-val);//Mathf.Log(-val)
            }
            else if (val > 0)
            {
                log_val = Mathf.Log(val);//17052024 0f;
            }
            else
            {
                log_val = 0f;
            }

            val_return = log_val;
        }
        return val_return;//17032025 log_val
    }
    /// <summary>
    /// Checks whether component and derivative direction form a normal strain (u/x or v/y).
    /// </summary>
    /// <returns>True for a diagonal component.</returns>
    public bool check_diag()
    {
        bool is_diag = false;

        if (u_v_mode == "u" && strain_d_mode == "y")
        {
            is_diag = true;
        }
        if (u_v_mode == "u" && strain_d_mode == "x")
        {
            is_diag = false;
        }
        if (u_v_mode == "v" && strain_d_mode == "y")
        {
            is_diag = false;
        }
        if (u_v_mode == "v" && strain_d_mode == "x")
        {
            is_diag = true;
        }

        return is_diag;
    }

    bool strain_with_log = false;//true
    /// <summary>
    /// Derivative in x at one pixel (optionally along the surface using the height component).
    /// </summary>
    /// <param name="mat">Map.</param>
    /// <param name="i">Column.</param>
    /// <param name="j">Row.</param>
    /// <param name="stream_z">Out-of-plane component.</param>
    /// <returns>Derivative (NaN at invalid pixels).</returns>
    public float find_d_x_val(List<List<float>> mat, int i, int j, List<List<float>> stream_z)
    {
        // info (paul): pure 2D
        //16032025 float val = (mat[i][j + 1] - mat[i][j - 1]) / 2f;
        float val_return = float.NaN;

        if (category_muc == "gom_curve")
        {
            // info (paul): pure 3D
            float l_standard = 4f * this.cam_0.orthographicSize / get_render_res();//2f;
            float dist_hori = mat[i][j + 1] - mat[i][j - 1];//17032025  + 2f;
            float dist_z = stream_z[i][j + 1] - stream_z[i][j - 1];

            float val = dist_hori;
            if (check_diag())
            {
                //val += Mathf.Abs(dist_z);
                val = Mathf.Sqrt((dist_hori + l_standard) * (dist_hori + l_standard) + dist_z * dist_z) / l_standard - 1f;
                //17032025 val = Mathf.Sqrt(dist_hori*dist_hori + dist_z*dist_z);
            }
            //float val = Mathf.Sqrt(dist_hori*dist_hori + dist_z*dist_z)/2f;// - 1f;
            val_return = val;
        }

        // info (paul): take logarithm
        //mat_new[i][j] = Mathf.Log(mat_new[i][j]);
        if (category_muc == "simple")
        {
            float val = (mat[i][j + 1] - mat[i][j - 1]) / 2f + 1f;
            float log_val = val;
            if (true)//(strain_with_log)
            {
                log_val = 0f;
                if (val < 0)
                {
                    log_val = -Mathf.Log(-val);//Mathf.Log(-val)
                }
                else if (val > 0)
                {
                    log_val = Mathf.Log(val);//17052024 0f;
                }
                else
                {
                    log_val = 0f;
                }
            }

            val_return = log_val;
        }
        return val_return;//17032025 log_val;
    }

    /// <summary>
    /// Scales the values of a map in place (legacy).
    /// </summary>
    /// <param name="floats">Map.</param>
    /// <returns>The map.</returns>
    public List<List<float>> take_share(List<List<float>> floats)
    {
        int res_x = floats.Count; //25052024 
        int res_y = floats[0].Count; //25052024 

        for (int i = 0; i < res_x; i++)
        {
            for (int j = 0; j < res_y; j++)
            {
                float i_share_2 = (float)(i % 2);
                float i_share = ((float)(i)) / ((float)(res_x));
                float j_share = ((float)(j)) / ((float)(res_y));
                floats[i][j] = i_share_2;
            }
        }

        return floats;
    }

    /// <summary>
    /// Converts a float matrix to a colour matrix (value in the red channel).
    /// </summary>
    /// <param name="floats">Matrix.</param>
    /// <returns>Colour matrix.</returns>
    public UnityEngine.Color[,] floats2col_mat(List<List<float>> floats)
    {
        int res_x = floats.Count;
        int res_y = floats[0].Count;

        UnityEngine.Color[,] col_mat = new UnityEngine.Color[res_x, res_y];

        for (int i = 0; i < res_x; i++)
        {
            for (int j = 0; j < res_y; j++)
            {

                float floats_ij = floats[i][j];
                if (!is_meaningful(floats_ij))
                {
                    //21092026 NaN/Inf -> 0 (schwarz), statt undefinierte Farbwerte in die Textur zu schreiben.
                    floats_ij = 0f;
                }

                col_mat[i, j].r = floats_ij;
                col_mat[i, j].g = floats_ij;
                col_mat[i, j].b = floats_ij;
                col_mat[i, j].a = 1f;
            }
        }

        return col_mat;
    }

    /// <summary>
    /// Converts a colour matrix to gray values of an image.
    /// </summary>
    /// <param name="cols_mat">Colour matrix.</param>
    /// <returns>Float matrix.</returns>
    public List<List<float>> cols_mat2floats_mat_for_im(UnityEngine.Color[,] cols_mat)
    {
        List<List<float>> floats = new List<List<float>>();

        for (int i = 0; i < cols_mat.GetLength(0); i++)
        {
            floats.Add(new List<float>());
            for (int j = 0; j < cols_mat.GetLength(1); j++)
            {
                Color color = cols_mat[i, j];
                // Camera pixels are ordinary RGB intensities. The previous code
                // reused the signed red/green encoding of flow textures and made
                // every green-dominant pixel negative. matrix2list() then encoded
                // those negatives as pure green, causing the visible color cast.
                float luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
                floats[i].Add(Mathf.Clamp01(luminance));
            }
        }
        return floats;
    }
    /// <summary>
    /// Converts a colour matrix to a float matrix (red channel).
    /// </summary>
    /// <param name="cols_mat">Colour matrix.</param>
    /// <returns>Float matrix.</returns>
    public List<List<float>> cols_mat2floats_mat(UnityEngine.Color[,] cols_mat)
    {
        List<List<float>> floats = new List<List<float>>();

        for (int i = 0; i < cols_mat.GetLength(0); i++)
        {
            floats.Add(new List<float>());
            for (int j = 0; j < cols_mat.GetLength(1); j++)
            {
                float red_val = cols_mat[i, j].r;
                float green_val = cols_mat[i, j].g;
                float val_chosen = Mathf.Max(red_val, green_val);
                if (green_val > red_val)
                {
                    val_chosen = -val_chosen;
                }
                floats[i].Add(val_chosen);
            }
        }
        return floats;
    }

    /// <summary>
    /// Converts a flat colour array to a colour matrix.
    /// </summary>
    /// <param name="cols">Colours.</param>
    /// <param name="res_x">Width.</param>
    /// <param name="res_y">Height.</param>
    /// <returns>Colour matrix.</returns>
    public UnityEngine.Color[,] list2matrix(UnityEngine.Color[] cols, int res_x, int res_y)
    {
        // info (paul): convert array of colors into matrix of colors (we call it list2...,
        //      because of convenience)

        UnityEngine.Color[,] col_mat = new UnityEngine.Color[res_x, res_y];

        for (int i = 0; i < res_x; i++)
        {
            for (int j = 0; j < res_y; j++)
            {
                col_mat[i, j] = cols[i * res_y + j];
            }
        }

        return col_mat;
    }

    /// <summary>
    /// Converts a flat list to a matrix.
    /// </summary>
    /// <param name="list">Values.</param>
    /// <param name="res_x">Width.</param>
    /// <param name="res_y">Height.</param>
    /// <returns>Matrix.</returns>
    public List<List<float>> list2matrix(List<float> list, int res_x, int res_y)
    {
        // info (paul): convert array of colors into matrix of colors (we call it list2...,
        //      because of convenience)

        //UnityEngine.Color[,] floats_mat = new UnityEngine.Color[res_x, res_y];
        List<List<float>> floats_mat = zeros_of_size(res_x, res_y);

        for (int i = 0; i < res_x; i++)
        {
            for (int j = 0; j < res_y; j++)
            {
                floats_mat[i][j] = list[i * res_y + j];
            }
        }

        return floats_mat;
    }

    /// <summary>
    /// Converts a vector to a float array.
    /// </summary>
    /// <param name="vec">Vector.</param>
    /// <returns>Array {x, y, z}.</returns>
    public float[] vec2floats(Vector3 vec)
    {
        return new float[3] { vec.x, vec.y, vec.z };
    }
    /// <summary>
    /// Converts a flat array to a jagged matrix.
    /// </summary>
    /// <param name="cols">Values.</param>
    /// <param name="res_x">Width.</param>
    /// <param name="res_y">Height.</param>
    /// <param name="with_switch_dims">True to swap the dimensions.</param>
    /// <returns>Matrix.</returns>
    public float[][] floats2matrix(float[] cols, int res_x, int res_y, bool with_switch_dims = false)
    {
        // info (paul): convert array of colors into matrix of colors (we call it list2...,
        //      because of convenience)

        //float[,] col_mat = new float[res_x, res_y];
        float[][] col_mat = new float[res_x][];
        for (int i = 0; i < res_x; i++)
        {
            col_mat[i] = new float[res_y];
        }

        for (int i = 0; i < res_x; i++)
        {
            for (int j = 0; j < res_y; j++)
            {
                col_mat[i][j] = cols[i * res_y + j];
            }
        }

        if (with_switch_dims)
        {
            col_mat = switch_dims(col_mat, res_x, res_y);
        }

        return col_mat;
    }

    /// <summary>
    /// Intended to swap the dimensions of a matrix; currently returns the input unchanged.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <returns>Matrix.</returns>
    public List<List<float>> switch_mat(List<List<float>> mat)
    {
        // info (paul): switch dims; I think, actually thiss currently does not do anything, because returns mat instead of mat_1
        List<List<float>> mat_1 = copy_mat(mat);

        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < mat[0].Count; j++)
            {
                mat_1[i][j] = mat[j][i];
            }
        }

        return mat;
    }
    /// <summary>
    /// Flattens a matrix to a list.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <returns>List.</returns>
    public List<float> matrix2list(List<List<float>> mat)
    {
        List<float> list = zeros_of_size(mat.Count * mat[0].Count);

        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < mat[0].Count; j++)
            {
                list[i * mat.Count + j] = mat[i][j];
            }
        }

        return list;
    }

    /// <summary>
    /// Flattens a colour matrix to an array (texture order).
    /// </summary>
    /// <param name="col_mat">Colour matrix.</param>
    /// <param name="res_x">Width.</param>
    /// <param name="res_y">Height.</param>
    /// <param name="marker">Debugging marker.</param>
    /// <param name="with_switch_dims">True to swap the dimensions.</param>
    /// <returns>Colour array.</returns>
    public UnityEngine.Color[] matrix2list(UnityEngine.Color[,] col_mat, int res_x, int res_y,
        string marker = null, bool with_switch_dims = false)
    {
        // info (paul): convert array of colors into matrix of colors (we call it list2...,
        //      because of convenience)
        //
        //      marker: just helpful marker for debugging

        //Color[,] col_mat = new Color[res_x, res_y];

        UnityEngine.Color[] cols = new UnityEngine.Color[res_x * res_y];

        res_x = col_mat.GetLength(0);
        res_y = col_mat.GetLength(1);

        //22052024debugging int res_y_play = res_y - 72;//ideal: res_y - 72
        for (int i = 0; i < res_x; i++)
        {
            for (int j = 0; j < res_y; j++)
            {
                UnityEngine.Color col_l = col_mat[i, j];//[i, j]

                if (col_l.r >= 0)
                {
                    //cols[j * res_y + i] = new Color(col_l.r, 0f, 0f, 1f);//16052024 col_l

                    //22052024debugging //cols[i * res_y_play + j] = new Color(j_share, 0f, 0f, 1f);//verm.22052024 col_l.r//16052024 col_l
                    cols[i * res_y + j] = col_l;//22052024 new Color(j_share, 0f, 0f, 1f);
                }
                else
                {
                    cols[i * res_y + j] = new UnityEngine.Color(0f, -col_l.r, 0f, 1f);//16052024 col_l
                }

                if (col_l.r != 0f && !float.IsNaN(col_l.r) && !float.IsInfinity(col_l.r))
                {
                    ;
                }
            }
        }

        if (with_switch_dims)
        {
            cols = switch_dims(cols, res_x, res_y);
        }
        return cols;
    }
    /// <summary>
    /// Swaps the dimensions of a jagged matrix.
    /// </summary>
    /// <param name="cols">Matrix.</param>
    /// <param name="res_x">Width.</param>
    /// <param name="res_y">Height.</param>
    /// <returns>Transposed matrix.</returns>
    public float[][] switch_dims(float[][] cols, int res_x, int res_y)
    {
        // info (paul): kind of switch cols dims (cols is 1d but represents
        //          the flattened version of a 2d array for a texture)

        float[][] cols_new = new float[res_y][];

        for (int j = 0; j < res_y; j++)
        {
            cols_new[j] = new float[res_x];

            for (int i = 0; i < res_x; i++)
            {
                cols_new[j][res_x - 1 - i] = cols[i][j];
            }
        }

        return cols_new;
    }
    /// <summary>
    /// Swaps the dimensions of a flat colour array.
    /// </summary>
    /// <param name="cols">Colours.</param>
    /// <param name="res_x">Width.</param>
    /// <param name="res_y">Height.</param>
    /// <returns>Transposed colour array.</returns>
    public UnityEngine.Color[] switch_dims(UnityEngine.Color[] cols, int res_x, int res_y)
    {
        // info (paul): kind of switch cols dims (cols is 1d but represents
        //          the flattened version of a 2d array for a texture)

        UnityEngine.Color[] cols_new = new UnityEngine.Color[res_x * res_y];

        for (int i = 0; i < res_x; i++)
        {
            for (int j = 0; j < res_y; j++)
            {
                cols_new[j * res_x + i] = cols[i * res_y + j];
            }
        }

        return cols_new;
    }

    /// <summary>
    /// Older version of matrix2list (unused).
    /// </summary>
    /// <param name="col_mat">Colour matrix.</param>
    /// <param name="res_x">Width.</param>
    /// <param name="res_y">Height.</param>
    /// <param name="marker">Debugging marker.</param>
    /// <returns>Colour array.</returns>
    public UnityEngine.Color[] matrix2list_copy(UnityEngine.Color[,] col_mat, int res_x, int res_y, string marker = null)
    {
        // info (paul): convert array of colors into matrix of colors (we call it list2...,
        //      because of convenience)
        //
        //      marker: just helpful marker for debugging

        //Color[,] col_mat = new Color[res_x, res_y];

        UnityEngine.Color[] cols = new UnityEngine.Color[res_x * res_y];

        List<(int, int)> nan_idxs = new List<(int, int)>();

        if (marker == "tex")
        {
            ;
        }

        // info (paul): overwrite matrix for debugging:
        /*for (int i = 0; i < res_x; i++)
        {
            for (int j = 0; j < res_y; j++)
            {
                float i_share = ((float)(i)) / ((float)(res_x));
                float j_share = ((float)(j)) / ((float)(res_y));

                col_mat[i, j] = new Color(i_share, 0f, 0f, 1f);
            }
        }*/




        //22052024debugging int res_y_play = res_y - 72;//ideal: res_y - 72
        for (int i = 0; i < res_y; i++)
        {
            for (int j = 0; j < res_x; j++)
            {
                UnityEngine.Color col_l = col_mat[j, i];//[i, j]
                //17052024 if (col_l.r == 0f)
                //17052024 {
                //17052024     col_l.r = 1f;
                //17052024 }
                //17052024 else if (col_l.r > 0f)
                //17052024 {
                //17052024     col_l.r = 1f;
                //17052024 }
                //17052024 else if (col_l.r < 0f)
                //17052024 {
                //17052024     col_l.r = 1f;
                //17052024 }
                //17052024 else
                //17052024 {
                //17052024     ;//col_l.r = 1f;
                //17052024     nan_idxs.Add((i, j));
                //17052024 }

                if (col_l.r >= 0)
                {
                    //cols[j * res_y + i] = new Color(col_l.r, 0f, 0f, 1f);//16052024 col_l

                    float i_share = ((float)(i)) / ((float)(res_y));
                    float j_share = ((float)(j)) / ((float)(res_x));
                    //22052024debugging //cols[i * res_y_play + j] = new Color(j_share, 0f, 0f, 1f);//verm.22052024 col_l.r//16052024 col_l
                    cols[i * res_x + j] = col_l;//22052024 new Color(j_share, 0f, 0f, 1f);
                }
                else
                {
                    cols[j * res_y + i] = new UnityEngine.Color(0f, -col_l.r, 0f, 1f);//16052024 col_l
                }
            }
        }

        if (marker == "tex")
        {
            ;
        }

        return cols;
    }

    /// <summary>
    /// Sets the resolution from a height map and returns it.
    /// </summary>
    /// <param name="heights">Height map.</param>
    /// <returns>The height map.</returns>
    public List<List<float>> force_heights(List<List<float>> heights)
    {
        // info (paul): I assume, that res_x and res_y are just the dimensions of the heights array, right?
        res_x = heights.Count;
        res_y = heights[0].Count;

        heights = zeros_of_size(res_x, res_y);

        for (int i = 0; i < res_x; i++)
        {
            for (int j = 0; j < res_y; j++)
            {
                heights[i][j] = 0f;
            }
        }

        return heights;
    }

    /// <summary>
    /// Creates a surface mesh from a height map (or a flat grid).
    /// </summary>
    /// <param name="heights">Height map.</param>
    /// <param name="force_flat">True for a flat grid.</param>
    /// <param name="scale_factor">Height scale (-1 = automatic).</param>
    /// <returns>Tuple (mesh, grid points).</returns>
    public (Mesh, List<List<float>>) make_mesh(List<List<float>> heights, bool force_flat = false, float scale_factor = -1f)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        int len_x = heights.Count;//08052024 200;
        int len_y = heights[0].Count;//08052024 576;

        heights = norm_mat(heights);
        heights = zeros_like(heights);//01032025
        mesh.vertices = init_verts(heights, len_x: len_x, len_y: len_y,
            force_flat: force_flat, scale_factor: scale_factor);

        mesh.triangles = init_tris(mesh.vertices, len_x, len_y);
        UnityEngine.Vector2[] uvs = init_uvs(mesh, len_x, len_y);

        //A mesh.vertices = new Vector3[] {
        //A     Vector3.zero, Vector3.right, Vector3.up
        //A };

        //mesh.triangles = new int[] {
        //    0, 1, 2
        //};

        //A
        //Amesh.normals = new Vector3[] {
        //A    Vector3.back, Vector3.back, Vector3.back
        //A};

        //mesh.uv = new Vector2[] {
        //     Vector2.zero, Vector2.right, Vector2.up
        //};
        mesh.uv = uvs;

        //Amesh.tangents = new Vector4[] {
        //A    new Vector4(1f, 0f, 0f, -1f),
        //A    new Vector4(1f, 0f, 0f, -1f),
        //A    new Vector4(1f, 0f, 0f, -1f)
        //A};

        //12042024A // TODO: set up uvs
        //12042024A Vector2[] uvs = new Vector2[4];
        //12042024A uvs[0] = new Vector2(0, 0);
        //12042024A uvs[1] = new Vector2(0, 1);
        //12042024A uvs[2] = new Vector2(1, 0);
        //12042024A uvs[3] = new Vector2(1, 1);
        //12042024A mesh.uv = uvs;
        //12042024A 
        //12042024A // info (paul): set normals:
        //12042024A Vector3[] normals = new Vector3[]{Vector3.up, Vector3.up , Vector3.up , Vector3.up };
        //12042024A mesh.normals = normals;

        return (mesh, heights);
    }

    /// <summary>
    /// Texture coordinates of the grid vertices.
    /// </summary>
    /// <param name="mesh">Mesh.</param>
    /// <param name="len_x">Number of cells in x.</param>
    /// <param name="len_y">Number of cells in y.</param>
    /// <returns>UV array.</returns>
    public UnityEngine.Vector2[] init_uvs(Mesh mesh, int len_x, int len_y)
    {
        UnityEngine.Vector2[] uvs = new UnityEngine.Vector2[mesh.vertices.Length];
        int counter = 0;

        for (int i = 0; i < len_x + 1; i++)
        {
            for (int j = 0; j < len_y + 1; j++)
            {
                float frac_x = uv_scale * ((float)i) / ((float)(len_x));
                float frac_y = uv_scale * ((float)j) / ((float)(len_y));
                uvs[counter] = new UnityEngine.Vector2(frac_x, frac_y);
                counter += 1;
            }
        }

        return uvs;
    }

    /// <summary>
    /// Triangle indices of the grid (two triangles per cell).
    /// </summary>
    /// <param name="verts">Vertices.</param>
    /// <param name="len_x">Number of cells in x.</param>
    /// <param name="len_y">Number of cells in y.</param>
    /// <returns>Index array.</returns>
    public int[] init_tris(UnityEngine.Vector3[] verts, int len_x, int len_y)
    {
        List<int> tris = new List<int>();

        for (int i_idx = 0; i_idx < len_x; i_idx++)
        {
            for (int j_idx = 0; j_idx < len_y; j_idx++)
            {
                // info (paul): first triangle
                int vert_1_idx = i_idx * (len_y + 1) + j_idx;
                int vert_2_idx = (i_idx + 1) * (len_y + 1) + j_idx;
                int vert_3_idx = i_idx * (len_y + 1) + j_idx + 1;

                tris.Add(vert_3_idx);
                tris.Add(vert_2_idx);
                tris.Add(vert_1_idx);

                // info (paul): second triangle
                int vert_4_idx = (i_idx + 1) * (len_y + 1) + j_idx;
                int vert_5_idx = (i_idx + 1) * (len_y + 1) + j_idx + 1;
                int vert_6_idx = i_idx * (len_y + 1) + j_idx + 1;

                tris.Add(vert_6_idx);
                tris.Add(vert_5_idx);
                tris.Add(vert_4_idx);
            }
        }

        int[] tris_array = tris.ToArray();

        return tris_array;
    }


    /// <summary>
    /// Vertices of the grid with the heights of the map.
    /// </summary>
    /// <param name="mat">Height map.</param>
    /// <param name="len_x">Number of cells in x.</param>
    /// <param name="len_y">Number of cells in y.</param>
    /// <param name="force_flat">True for a flat grid.</param>
    /// <param name="scale_factor">Height scale.</param>
    /// <returns>Vertex array.</returns>
    public UnityEngine.Vector3[] init_verts(List<List<float>> mat, int len_x = 10, int len_y = 10,
        bool force_flat = false, float scale_factor = 1f)
    {
        UnityEngine.Vector3[,] vecs = new UnityEngine.Vector3[len_x + 1, len_y + 1];

        float d_x = 10f * scale_factor;
        float d_z = 10f * scale_factor;

        (float mat_min, float mat_max) = find_max_2d(mat);
        if (get_heights_mode() == "value")
        {
            mat = multiply_with_scalar(mat, -1f);
            mat = add_to_mat(mat, 0f);
        }

        //for (int i_idx = 0; i_idx < len_x + 1; i_idx++)
        //{
        //    for (int j_idx = 0; j_idx < len_y + 1; j_idx++)
        //    {
        for (int i_idx = 0; i_idx < len_x + 1; i_idx++)
        {
            for (int j_idx = 0; j_idx < len_y + 1; j_idx++)
            {
                // info (paul): get height val
                float height_val = 0f;

                int off_x = 0;
                int off_y = 0;

                bool in_bounds_up = (i_idx - off_x < mat.Count) && (j_idx - off_y < mat[0].Count);

                bool in_bounds_down_x = (i_idx - off_x >= 0);
                bool in_bounds_down_y = (j_idx - off_y >= 0);
                bool in_bounds_down = in_bounds_down_x && in_bounds_down_y;

                bool in_bounds = in_bounds_down && in_bounds_up;

                bool min_meaningful = !float.IsNaN(mat_min);
                bool max_meaningful = !float.IsNaN(mat_max);
                if (in_bounds)
                {
                    float height_raw = mat[i_idx - off_x][j_idx - off_y];
                    if (height_raw != 0f)
                    {
                        if (min_meaningful && max_meaningful)
                        {
                            height_val = (height_raw - mat_min) / (mat_max - mat_min);
                        }
                        else
                        {
                            height_val = height_raw;
                        }
                        if (height_val > 0f)
                        {
                            height_val *= 500f;
                        }
                    }
                }
                float our_floor = -200f;
                if (float.IsNaN(height_val) || float.IsInfinity(height_val))
                {
                    height_val = our_floor;
                }

                if (force_flat)
                {
                    height_val = 0f;
                }
                else
                {
                    ;
                }

                int border_l = 128;
                if (i_idx < border_l || i_idx > len_x - border_l ||
                    j_idx < border_l || j_idx > len_x - border_l)
                {
                    height_val = our_floor;
                }


                float pos_x = d_x * (float)i_idx;
                float pos_z = d_z * (float)j_idx;
                vecs[i_idx, j_idx] = new UnityEngine.Vector3(pos_x, 0f, pos_z);//01032025 (pos_x, height_val, pos_z);
                UnityEngine.Vector3 vec_l = vecs[i_idx, j_idx];

                //make_sphere_at(vec_l.x, vec_l.y, vec_l.z, size: 5f);
            }
        }

        UnityEngine.Vector3[] flat = flatten_vec_2d(vecs);

        return flat;
    }
    int i_test_1 = -1;
    int j_test_1 = -1;
    /// <summary>
    /// Minimum and maximum of a matrix.
    /// </summary>
    /// <param name="matrix">Matrix.</param>
    /// <returns>Tuple (max, min).</returns>
    public (float, float) find_max_2d(List<List<float>> matrix)
    {
        float max_val = -9999999f;
        float min_val = 9999999f;

        for (int i = 0; i < matrix.Count; i++)
        {
            for (int j = 0; j < matrix[0].Count; j++)
            {
                if (i == 70)
                {
                    if (j == 70)
                    {
                        ;
                    }
                }
                if (i == i_test_1)
                {
                    ;
                }
                if (j == j_test_1)
                {
                    ;
                }

                float mat_val_l = matrix[i][j];
                if (mat_val_l < 0f)
                {
                    ;
                }
                if (is_meaningful(mat_val_l))
                {
                    min_val = Mathf.Min(min_val, mat_val_l);
                    max_val = Mathf.Max(max_val, mat_val_l);
                }
            }
        }
        return (min_val, max_val);
    }
    /// <summary>
    /// Flattens a 2D vector array.
    /// </summary>
    /// <param name="vecs_2d">Vector array.</param>
    /// <returns>Flat array.</returns>
    public UnityEngine.Vector3[] flatten_vec_2d(UnityEngine.Vector3[,] vecs_2d)
    {
        int l_x = vecs_2d.GetLength(0);
        int l_y = vecs_2d.GetLength(1);

        UnityEngine.Vector3[] flat = new UnityEngine.Vector3[l_x * l_y];
        int counter = 0;

        for (int i_idx = 0; i_idx < l_x; i_idx++)
        {
            for (int j_idx = 0; j_idx < l_y; j_idx++)
            {
                flat[counter] = vecs_2d[i_idx, j_idx];
                counter += 1;
            }
        }

        return flat;
    }

    /// <summary>
    /// Creates a sphere as a marker.
    /// </summary>
    /// <param name="x">x position.</param>
    /// <param name="y">y position.</param>
    /// <param name="z">z position.</param>
    /// <param name="size">Diameter.</param>
    /// <returns>Sphere object.</returns>
    public GameObject make_sphere_at(float x, float y, float z, float size = 1f)
    {
        //(GameObject sphere_local, _) = but1.build_object(new Vector3(x, y, z), 
        //        Quaternion.identity, -1, "Targets/symbols/street_line_symbol");

        GameObject sphere_local = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere_local.transform.position = new UnityEngine.Vector3(x, y, z);
        sphere_local.transform.localScale = new UnityEngine.Vector3(size, size, size);
        sphere_local.name = "sphere_marker";
        //12042024 set_layer(sphere_local, 11);
        return sphere_local;
    }

    /// <summary>
    /// Legacy entry point of the rendering of the Nakajima samples (empty).
    /// </summary>
    public void main_render()
    {
        // info (paul): Kind of the main function for the rendering of the nakajima samples
        //      for validation purposes:

        ; ;
    }
    /// <summary>
    /// Recreates the stereo cameras above the sample.
    /// </summary>
    public void refresh_cams()
    {
        GameObject cam_parent = GameObject.Find("cams_0_1_parent");
        remove_children(cam_parent);

        Vector3 pos = new Vector3(blades_pos.x, blades_pos.y + default_dist, blades_pos.z + 0f);
        if (category == "muc" && category_muc == "simple")
        {
            //18032025 pos = new Vector3(blades_pos.x, blades_pos.y + 9*default_dist, blades_pos.z);
            pos = new Vector3(blades_pos.x, blades_pos.y + 9 * default_dist, blades_pos.z);

            //03032025 pos = new Vector3(blades_pos.x - 100f, blades_pos.y + default_dist, blades_pos.z + 50f);
        }
        if (category == "muc" && category_muc == "gom_curve")
        {
            pos = new Vector3(blades_pos.x, blades_pos.y + default_dist, blades_pos.z);
            //03032025 pos = new Vector3(blades_pos.x - 100f, blades_pos.y + default_dist, blades_pos.z + 50f);
        }
        this.cam_angle_0 = 10f;//1f
        this.cam_angle_1 = cam_angle_0;
        if (category == "muc" && category_muc == "simple")
        {
            this.cam_angle_0 = 0f;
            this.cam_angle_1 = 2f;
        }
        this.cam_0 = set_up_cam("cam_0", "cam_prefab_0_" + get_render_res().ToString(), pos, angle: -cam_angle_0);//-10f
        this.cam_1 = set_up_cam("cam_1", "cam_prefab_1_" + get_render_res().ToString(), pos, angle: cam_angle_1);//10f
    }

    /// <summary>
    /// Sets up the two cameras (tilt 10 degrees) and the lighting.
    /// </summary>
    public void load_cam_light()
    {
        Vector3 pos = new Vector3(blades_pos.x, blades_pos.y + default_dist, blades_pos.z + 0f);

        // info (paul): set up two cameras
        cam_angle = 10f;//1f
        if (category == "muc")
        {
            cam_angle = 0f;
        }
        set_up_cam("cam_0", "cam_prefab_0_" + get_render_res().ToString(), pos, angle: -cam_angle);//-10f
        set_up_cam("cam_1", "cam_prefab_1_" + get_render_res().ToString(), pos, angle: cam_angle);//10f

        set_up_lighting(y_coord: 30f, intensity: 1f);//22102025 1f);//16072025 0f);
        

        // info (paul): set up blade object
        //10062024 GameObject blade_90 = load_blade(cam);
    }
    /// <summary>
    /// Destroys all children of an object.
    /// </summary>
    /// <param name="game_obj">Parent object.</param>
    public void remove_children(GameObject game_obj)
    {
        int child_cnt = game_obj.transform.childCount;
        for (int i = child_cnt - 1; i >= 0; i--)
        {
            Transform child = game_obj.transform.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
    }
    //29092026 Nutzerwunsch: relevant sind nur die beiden Laborlampen light1/light2 aus exp_setup.
    //  Beide bekommen dieselbe Intensitaet = Grundwert (Prefab-Wert von light1, einmal gemerkt) * intensity;
    //  alle anderen Lichtquellen (Szenen-"Spot Light", exp_setup/ambience, das fruehere Richtungslicht
    //  unter "lightings") werden deaktiviert. Faktor 1 = Originalbeleuchtung des Aufbaus.
    //  Fehlen light1/light2, gilt das alte Verhalten (eigenes Richtungslicht, 10 lx * intensity).
    float exp_light_base = float.NaN;
    public bool use_exp_setup_lights = true;

    /// <summary>
    /// Uses the lamps of the lab model (if present) and scales their intensity.
    /// </summary>
    /// <param name="intensity">Illumination factor.</param>
    /// <returns>True if lab lamps were found.</returns>
    bool set_up_exp_setup_lights(float intensity)
    {
        Light[] all = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<Light> lab = new List<Light>();
        foreach (Light l in all)
        {
            if (l.name != "light1" && l.name != "light2") continue;
            for (Transform p = l.transform.parent; p != null; p = p.parent)
                if (p.name.StartsWith("exp_setup")) { lab.Add(l); break; }
        }
        if (lab.Count == 0)
            return false;
        if (float.IsNaN(exp_light_base))
        {
            Light first = lab.Find(l => l.name == "light1") ?? lab[0];
            exp_light_base = first.intensity;
            Debug.Log("Laborlampen light1/light2: Grundintensitaet " + exp_light_base.ToString("G6", CultureInfo.InvariantCulture)
                + " (" + first.lightUnit + ")");
        }
        if (float.IsNaN(intensity))
            intensity = 1f;
        set_ambient_factor(Mathf.Min(intensity, 1f));
        foreach (Light l in all)
        {
            if (lab.Contains(l))
            {
                l.gameObject.SetActive(true);
                l.enabled = true;
                l.intensity = exp_light_base * intensity; // gleiche Einheit wie im Prefab
            }
            else if (l.enabled)
                l.enabled = false;
        }
        return true;
    }

    //29092026 Umgebungslicht (Himmel: indirektes diffuses Licht und Reflexionen) mitdimmen. Ohne das bleibt das
    //  Bild bei kleinen Lampenwerten auf Grauwert ~8 stehen. Faktor = min(Lichtstaerke, 1): unterhalb von 1
    //  wird alles gemeinsam dunkler, ab 1 bleibt das Umgebungslicht wie im Original (fruehere Werte >= 1 gelten weiter).
    Volume ambient_volume;
    IndirectLightingController ambient_ctrl;
    /// <summary>
    /// Sets the ambient light via a global HDRP volume (created if missing).
    /// </summary>
    /// <param name="f">Ambient factor.</param>
    void set_ambient_factor(float f)
    {
        if (ambient_volume == null)
        {
            GameObject go = new GameObject("lab_ambient_volume");
            ambient_volume = go.AddComponent<Volume>();
            ambient_volume.isGlobal = true;
            ambient_volume.priority = 1000f;
            VolumeProfile prof = ScriptableObject.CreateInstance<VolumeProfile>();
            ambient_ctrl = prof.Add<IndirectLightingController>(true);
            ambient_volume.sharedProfile = prof;
        }
        ambient_ctrl.indirectDiffuseLightingMultiplier.Override(f);
        ambient_ctrl.reflectionLightingMultiplier.Override(f);
        ambient_ctrl.reflectionProbeIntensityMultiplier.Override(f);
    }

    /// <summary>
    /// Creates the lighting of the scene (lab lamps or own lights).
    /// </summary>
    /// <param name="y_coord">Height of the lights.</param>
    /// <param name="intensity">Illumination factor.</param>
    public void set_up_lighting(float y_coord = 30f, float intensity = 1f)
    {
        // info (paul): remove lights
        GameObject lightings_parent = GameObject.Find("lightings");
        remove_children(lightings_parent);

        if (use_exp_setup_lights)
        {
            if (set_up_exp_setup_lights(intensity))
                return;
            Debug.LogWarning("light1/light2 (exp_setup) nicht gefunden - verwende das Richtungslicht wie bisher.");
        }

        // info (paul): create new light
        GameObject light_obj = new GameObject("light");
        if (light_obj.GetComponent<Light>() == null)
        {
            light_obj.AddComponent<Light>();
        }
        if (light_obj.GetComponent<HDAdditionalLightData>() == null)
        {
            light_obj.AddComponent<HDAdditionalLightData>();
        }

        Light light = light_obj.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(255f / 255f, 244f / 255f, 214f / 255f, 1f);
        light.shadows = LightShadows.Soft;
        //30012025 light.intensity = intensity;

        HDAdditionalLightData light_hdrp = light_obj.GetComponent<HDAdditionalLightData>();
        //20092026 NaN-Guard (unbestaetigte ExpConfig liefert ambient_intensity = NaN).
        if (float.IsNaN(intensity))
            intensity = 1f;
        light_hdrp.SetIntensity(intensity * 10f, LightUnit.Lux);//15022025 1000f

        light_obj.transform.position = new Vector3(0f, 100.6f, -162.4f);

        // info (paul): eulerAngles A
        light_obj.transform.eulerAngles = new Vector3(50f, y_coord, 0f);
        light_obj.transform.SetParent(lightings_parent.transform);

        // info (paul): eulerAngles B
        //light_obj.transform.eulerAngles = new Vector3(50f, y_coord, 0f);

        //lightComp.color = Color.blue;
        //lightGameObject.transform.position = new Vector3(0, 5, 0);
        ;
    }
    //public void ;;
    /// <summary>
    /// Creates a camera from a prefab at a position and tilt (with a runtime render target for free resolutions).
    /// </summary>
    /// <param name="cam_name">Object name.</param>
    /// <param name="prefab_name">Name of the camera prefab.</param>
    /// <param name="pos">Position.</param>
    /// <param name="angle">Tilt angle in degrees.</param>
    /// <param name="config">Experiment configuration (optional).</param>
    /// <returns>The camera.</returns>
    public Camera set_up_cam(string cam_name, string prefab_name, Vector3 pos, float angle = 0f, ExpConfig config=null)
    {
        GameObject cam_prefab = (GameObject)Resources.Load("Targets/fbx_files/" + prefab_name);
        //23092026 Nutzerwunsch: frei waehlbare Aufloesung. Prefabs (mit fester RenderTexture) gibt es
        //  nur fuer 128/256/512; fuer andere Werte das 512er-Prefab als Vorlage nehmen und unten eine
        //  RenderTexture der gewuenschten Groesse (gleiches Format/Tiefe) zur Laufzeit zuweisen.
        bool runtime_target = false;
        if (cam_prefab == null && prefab_name.LastIndexOf('_') > 0)
        {
            string template_name = prefab_name.Substring(0, prefab_name.LastIndexOf('_')) + "_512";
            cam_prefab = (GameObject)Resources.Load("Targets/fbx_files/" + template_name);
            runtime_target = cam_prefab != null;
        }
        if (cam_prefab == null)
        {
            throw new Exception("Kamera-Prefab nicht gefunden: " + prefab_name);
        }
        GameObject cam_obj = Instantiate(cam_prefab);
        //11062024 cam_obj.transform.position = new UnityEngine.Vector3(-3.1f, 306f, 1f);//10062024 (-3.1f, 106f, 1f);
        //11062024 cam_obj.transform.LookAt(new UnityEngine.Vector3(0f, 0f, 0f));
        cam_obj.transform.position = pos;//new UnityEngine.Vector3(blades_pos.x, blades_pos.y + 300f, blades_pos.z + 0f);
        cam_obj.transform.LookAt(blades_pos);
        if (false)//03032025 category == "muc")
        {
            cam_obj.transform.LookAt(new Vector3(blades_pos.x - 50f, blades_pos.y + 0f, blades_pos.z + 50f));
        }
        cam_obj.name = cam_name;

        //cam_obj.transform.RotateAround(blades_pos, Vector3.forward, 0.3f*angle);
        //28022025 cam_obj.transform.RotateAround(blades_pos, Vector3.forward, angle);
        if (category == "muc")
        {
            //A cam_obj.transform.RotateAround(cam_obj.transform.position, cam_obj.transform.forward, 90f);
            //A cam_obj.transform.RotateAround(blades_pos, new Vector3(1f, 0f, 0f), 90f);
            cam_obj.transform.RotateAround(cam_obj.transform.position, cam_obj.transform.forward, 90f);
        }
        //18092026 Nutzerwunsch: Kameras rotieren jetzt relativ zueinander um die globale
        //X-Achse statt um die globale Z-Achse (vorher new Vector3(0f, 0f, 1f)). Passend dazu
        //wird die Probe unten in load_blade_from_verts() zusaetzlich um 90 Grad um die
        //globale Y-Achse gedreht platziert.
        cam_obj.transform.RotateAround(blades_pos, new Vector3(1f, 0f, 0f), angle);//18092026 vorher new Vector3(0f, 0f, 1f)

        // info (paul): little final position adjustments
        if (category == "muc")
        {
            Vector3 pos_l = cam_obj.transform.position;
            //01032025 cam_obj.transform.position = new Vector3(pos_l.x + 20f, pos_l.y, pos_l.z - 20f);
            cam_obj.transform.position = new Vector3(pos_l.x - 0f, pos_l.y, pos_l.z + 0f);
        }
        else
        {
            //18092026 Nutzerwunsch: Kamera zusaetzlich um 90 Grad um ihre eigene finale
            //Kamera-/Blickachse (transform.forward) rollen, damit die Probe im gerenderten
            //Bild entsprechend um 90 Grad gedreht erscheint. Erst hier (nach LookAt, der
            //X-Achsen-Paarverdrehung und den Positions-Anpassungen) anwenden, damit um die
            //tatsaechliche finale Blickachse gerollt wird. Nur fuer den aktuellen Modus
            //(category != "muc"), damit der bestehende "muc"-Roll weiter oben unangetastet bleibt.
            cam_obj.transform.RotateAround(cam_obj.transform.position, cam_obj.transform.forward, 90f);

            //20092026 Nutzerwunsch: beide Kameras zusaetzlich (nochmal) um 90 Grad um ihre
            //eigene lokale Z-/Kamera-Achse rollen (insgesamt jetzt 180 Grad Roll).
            cam_obj.transform.RotateAround(cam_obj.transform.position, cam_obj.transform.forward, 90f);
        }

        GameObject cam_parent = GameObject.Find("cams_0_1_parent");
        cam_obj.transform.SetParent(cam_parent.transform);

        Camera cam = cam_obj.GetComponent<Camera>();
        if (runtime_target)
        {
            assign_runtime_target(cam, cam_name);
        }

        if (config == null && category != "muc")
        {
            cam.fieldOfView = this.field_of_view; // standard: 60;
            //21092026 Nakajima-Look: quadratisches Bild soll dieselbe Breite zeigen wie das
            //(breitere) "Realbild: Neu"-Render -> aequivalente FOV (etwas rausgezoomt).
            if (nakajima_look)
                cam.fieldOfView = nakajima_equivalent_square_fov();
        }
        if (config == null && category == "muc" && category_muc != "gom_curve")
        {
            cam.fieldOfView = 5f;//20f;//18032025 5f;//20f;
        }
        if (config == null && category == "muc" && category_muc == "gom_curve")
        {
            cam.orthographic = true;
            cam.orthographicSize = 150f;
        }
        if (config != null)
        {
            //20092026 NaN-Guard: eine unbestaetigte Config hat fov = NaN, was zu einer
            //singulaeren Projektionsmatrix (HDRP-Assertion) fuehrt.
            float config_fov = config.get_fov();
            cam.fieldOfView = (float.IsNaN(config_fov) || config_fov <= 0f)
                ? this.field_of_view : config_fov;
        }


        if (cam_name == "cam_0")
        {
            this.cam_for_uv_0 = cam;
        }
        if (cam_name == "cam_1")
        {
            this.cam_for_uv_1 = cam;
        }

        // info (paul): add cam symbol
        add_cam_symbol(cam);

        // info (paul): make sure, that the camera does not render symbols-layer
        LayerMask mask = LayerMask.GetMask(new string[] { "Default" , "Experiment"});
        cam.cullingMask = mask;

        return cam;
    }
    //23092026 RenderTexture der aktuellen Aufloesung fuer eine Kamera, kopiert Format/Tiefe der
    //  Prefab-Vorlage. Die vorherige Laufzeit-Textur derselben Kamera wird freigegeben.
    private readonly Dictionary<string, RenderTexture> runtime_cam_targets = new Dictionary<string, RenderTexture>();
    /// <summary>
    /// Assigns a render texture of the chosen resolution (same format as the prefab template).
    /// </summary>
    /// <param name="cam">Camera.</param>
    /// <param name="cam_name">Camera name (for messages).</param>
    void assign_runtime_target(Camera cam, string cam_name)
    {
        RenderTexture template = cam.targetTexture;
        if (template == null)
        {
            Debug.LogWarning("assign_runtime_target: Vorlage ohne targetTexture (" + cam_name + ")");
            return;
        }
        int res = get_render_res();
        RenderTextureDescriptor desc = template.descriptor;
        desc.width = res;
        desc.height = res;
        RenderTexture rt = new RenderTexture(desc);
        rt.name = "cam_tex_runtime_" + cam_name + "_" + res;
        rt.filterMode = template.filterMode;
        rt.wrapMode = template.wrapMode;
        rt.Create();

        if (runtime_cam_targets.TryGetValue(cam_name, out RenderTexture old) && old != null)
        {
            if (RenderTexture.active == old)
                RenderTexture.active = null;
            old.Release();
            Destroy(old);
        }
        runtime_cam_targets[cam_name] = rt;
        cam.targetTexture = rt;
        Debug.Log("Kamera " + cam_name + ": Laufzeit-RenderTexture " + res + "x" + res);
    }

    /// <summary>
    /// Shows a camera symbol at the camera position.
    /// </summary>
    /// <param name="cam">Camera.</param>
    public void add_cam_symbol(Camera cam)
    {
        GameObject game_obj_prefab = Resources.Load("cam_symbol") as GameObject;
        GameObject symbol_obj = Instantiate(game_obj_prefab, cam.transform.position, cam.transform.rotation);
        symbol_obj.transform.localScale = new Vector3(1000f, 1000f, 1000f);

        GameObject symbols_parent = GameObject.Find("cams_symbols");
        symbol_obj.transform.SetParent(symbols_parent.transform);
        symbol_obj.layer = symbols_layer;

        // info (paul): rotate cam symbol, so that it looks fitting
        symbol_obj.transform.RotateAround(symbol_obj.transform.position, symbol_obj.transform.forward, -90f);
    }

    public void create_other_blades()//init_blades
    {
        //List<int> idxs_long = new List<int>(){80, 90};

        //21102024B for (int i = blade_idx_min + 1; i < blade_idx_max; i++)

        // info (paul): start from one, since "other_blades" means, 
        //      the first one is already created
        for (int i = 1; i < blade_idxs.Count; i++)
        {
            //21102024B int idx_long_B = i;
            //21102024B if (with_our_idxs)
            //21102024B {
            //21102024B     int idx_long_A = i - blade_idx_min - 1;
            //21102024B     idx_long_B = our_blade_idxs[idx_long_A];
            //21102024B }

            int t_idx = blade_idxs[i];

            //A if (!with_our_idxs)
            //A {
            //A     idx_long_B = i;
            //A }
            string file_path = blade_path_for_idx(t_idx);
            GameObject blade_new = load_blade_from_verts(file_path, blade_idx: t_idx, with_uv_init: false, 
                with_collider: false);

            apply_speckles(blade_new);
        }
    }

    /// <summary>
    /// Shows only the sample of a time step.
    /// </summary>
    /// <param name="blade_idx">Time-step index.</param>
    public void activate_blade(int blade_idx)
    {
        List<GameObject> blades = collect_blades();

        //11062024 for (int i = 0; i < blades.Count; i++)//blades.Count; i++)
        //11062024 {
        activate_blade_from(blades, blade_idx: blade_idx);
        //11062024 }
    }

    /// <summary>
    /// Shows only one sample of a list.
    /// </summary>
    /// <param name="blades">Samples.</param>
    /// <param name="blade_idx">Index of the shown sample.</param>
    public void activate_blade_from(List<GameObject> blades, int blade_idx)
    {
        // info (paul): deactivate all blades
        for (int i = 0; i < blades.Count; i++)
        {
            GameObject blade = blades[i];
            blade.SetActive(false);
        }

        // info (paul): activate blade at idx "idx"
        try
        {
            blades[blade_idx].SetActive(true);
        }
        catch
        {
            blades[blade_idx].SetActive(true);
        }

        // info (paul): take picture:
        //if (ready_for_next_blade)
        //{
        //set_ready_for_next_blade(false);
        //}
    }

    /// <summary>
    /// Takes an image manually (camera settings from the design panels).
    /// </summary>
    /// <param name="save_path">Output path.</param>
    /// <param name="cam_idx">Camera index.</param>
    public void take_pic_manual(string save_path, int cam_idx = 0)
    {
        // info (paul): this is for taking pictures, when the user enters cam pos etc. over the design panels
        take_pic(blade_idx: -1, cam_idx: cam_idx, save_path: save_path, pars: params_now);
    }
    /// <summary>
    /// Takes the image of the current render action and time step.
    /// </summary>
    public void take_pic_act()
    {
        int blade_idx = get_blade_idx();
        int t_idx = blade_idxs[blade_idx];

        Actioner current_act = exp_cv_acts[get_reg_idx() - 1];//23022025 t_idx];
        Params pars = current_act.pars;
        take_pic(t_idx, cam_idx_for_pic, pars: pars);

        cam_idx_for_pic += 1;
        cam_idx_for_pic = cam_idx_for_pic % 2;
    }

    /// <summary>
    /// Renders an image of a camera, applies post-processing and noise, and saves it.
    /// </summary>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="pars">Parameters of the experiment.</param>
    /// <param name="save_path">Output path (optional).</param>
    void take_pic(int blade_idx, int cam_idx, Params pars, string save_path = null)
    {
        Texture2D tex = cam2tex(cam_idx);
        Texture2D tex_post = post_proc(tex, pars: pars);
        // info (paul): save as png image
        save_png(tex_post, cam_idx, save_path: save_path, blade_idx: blade_idx, pars: pars);
    }

    /// <summary>
    /// Post-processing of a rendered image (gray values, noise).
    /// </summary>
    /// <param name="tex">Rendered image.</param>
    /// <param name="pars">Parameters of the experiment.</param>
    /// <returns>Processed image.</returns>
    public Texture2D post_proc(Texture2D tex, Params pars)
    {
        //Texture2D tex_1 = new Texture2D(tex.width, tex.height);

        // TODO: The image has 3 color channels, so if you want to implement lins_verzerr etc., 
        //      then you have to apply these each of these color channels
        List<List<float>> mat_1 = tex2mat(tex, with_switch_dims: false, for_im: true);
        List<List<float>> mat_2 = lins_verzerr(mat_1);
        List<List<float>> mat_3 = manage_image_noise(mat_2, pars: pars);

        Texture2D tex_2 = mat2tex(mat_3);
        //Texture2D tex_2 = tex;
        return tex_2;
    }

    /// <summary>
    /// Placeholder for lens distortion (currently identity).
    /// </summary>
    /// <param name="unverzerrt">Undistorted image.</param>
    /// <returns>The input.</returns>
    public List<List<float>> lins_verzerr(List<List<float>> unverzerrt)
    {
        List<List<float>> verzerr = unverzerrt;// TODO: add the actual calculation
        return verzerr;
    }

    Stopwatch watch = null;

    /// <summary>
    /// Starts the stopwatch.
    /// </summary>
    public void tik()
    {
        // info (paul): Start stopwatch
        this.watch = new Stopwatch();
        this.watch.Start();
    }

    /// <summary>
    /// Stops the stopwatch.
    /// </summary>
    /// <returns>Elapsed ticks.</returns>
    public long tok()
    {
        // info (paul): read out stop watch and restart it
        this.watch.Stop();
        long ticks = this.watch.ElapsedTicks;
        return ticks;
    }

    /// <summary>
    /// Takes the reference image (triangle assignment) of the current time step.
    /// </summary>
    public void take_ref_pic_act()//(int blade_idx, int cam_idx)
    {
        // info (paul): get params
        int blade_idx = get_blade_idx();
        int cam_idx = 0;

        // info (paul): assign and activate mesh collider
        List<GameObject> blades = collect_blades();
        Mesh mesh_l = blades[blade_idx].GetComponent<MeshFilter>().sharedMesh;

        blades[blade_idx].AddComponent<MeshCollider>();
        blades[blade_idx].GetComponent<MeshCollider>().sharedMesh = mesh_l;
        //blades[cam_idx].GetComponent<MeshCollider>().collider.convex = true;

        // info (paul): try a ground truth for the pure heights flow
        if (get_blade_tris() == null)
        {
            set_blade_tris(init_tris_empty());
        }
        Mesh mesh = blades[blade_idx].GetComponent<MeshFilter>().sharedMesh;
        blade_tris.Add(mesh.triangles);

        List<List<float>> heights_flow = null;
        if (this.get_blade_tris() != null && ground_truth_from_flow)
        {
            heights_flow = manage_heights_flow(blades[blade_idx], blade_idx);
        }

        // info (paul): conventional direct distane calculation
        List<List<float>> dists = (heights_flow == null) ? find_dists(500000000) : heights_flow;//28112024 find_dists(500000000);

        Actioner current_act = exp_cv_acts[get_reg_idx() - 1];//23022025 t_idx];
        Params pars = current_act.pars;

        (float depth_min, float depth_max) = find_min_max(dists, with_padding: true);
        List<List<float>> dists_normed = norm_mat(dists);
        Texture2D depth_tex = mat2tex(dists_normed, with_switch_dims: true);
        save_png(depth_tex, cam_idx, blade_idx: blade_idx, label:"_depth", pars: pars);

        save_floats_list_2_for_blade(dists_normed, cam_idx, blade_idx, label: "_depth_mat", with_uv_mode: false);//07102024re

        save_float_for_blade(depth_min, cam_idx, blade_idx, label: "_depth_min", with_uv_mode: false);
        save_float_for_blade(depth_max, cam_idx, blade_idx, label: "_depth_max", with_uv_mode: false);

        // info (paul): deactivate mesh collider
    }
    /// <summary>
    /// Computes the reference out-of-plane displacement map of a time step.
    /// </summary>
    /// <param name="blade">Sample.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <returns>Map.</returns>
    public List<List<float>> manage_heights_flow(GameObject blade, int blade_idx)
    {
        // info (paul): init blade tris, will be needed
        List<GameObject> blades = collect_blades();
        this.set_blades(blades);
        init_blade_tris(blades);

        // info (paul): ground truth for pure heights flow
        (Vector3[] proj_0, _) = proj_blade(blade, cam: cam_for_uv_0);
        (Vector3[] proj_1, _) = proj_blade(blade, cam: cam_for_uv_1);
        (float[] d_xs, float[] d_ys, float[] d_zs) = vec_diff(proj_0, proj_1);
        (int[,] tris, float[][][] barys) = im2triangles(cam: cam_for_uv_0);

        List<List<float>> heights_flow = construct_heights_flow(d_xs, d_ys, d_zs,
            tris, barys, blade_idx);
        //d_xs[tris[0,0]] = 
        return heights_flow;
    }
    /// <summary>
    /// Per-pixel out-of-plane reference displacement by barycentric interpolation.
    /// </summary>
    /// <param name="d_xs">x displacement per vertex.</param>
    /// <param name="d_ys">y displacement per vertex.</param>
    /// <param name="d_zs">z displacement per vertex.</param>
    /// <param name="tris">Triangle index per pixel.</param>
    /// <param name="barys">Barycentric coordinates per pixel.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <returns>Map.</returns>
    public List<List<float>> construct_heights_flow(float[] d_xs, float[] d_ys,
        float[] d_zs, int[,] tris, float[][][] barys, int blade_idx)
    {
        List<List<float>> heights_flow = zeros_of_size(tris.GetLength(0), tris.GetLength(1));

        for (int i = 0; i < tris.GetLength(0); i++)
        {
            for (int j = 0; j < tris.GetLength(1); j++)
            {
                int tri_idx = tris[i, j];

                if (tri_idx >= 0)
                {
                    (float d_xs_ij, float d_ys_ij, float d_zs_ij) = func_11(
                        barys[i][j], d_xs, d_ys, d_zs, i, j, tri_idx, blade_idx);
                    heights_flow[i][j] = d_xs_ij; //d_xs[tri_idx];
                }
                else
                {
                    heights_flow[i][j] = -1;
                }
            }
        }

        return heights_flow;
    }
    /// <summary>
    /// Barycentric reference displacement of one pixel.
    /// </summary>
    /// <param name="bary">Barycentric coordinates.</param>
    /// <param name="d_xs">x displacement per vertex.</param>
    /// <param name="d_ys">y displacement per vertex.</param>
    /// <param name="d_zs">z displacement per vertex.</param>
    /// <param name="i">Column.</param>
    /// <param name="j">Row.</param>
    /// <param name="tri_idx">Triangle index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <returns>Tuple (dx, dy, dz).</returns>
    public (float, float, float) func_11(float[] bary, float[] d_xs, float[] d_ys, float[] d_zs,
        int i, int j, int tri_idx, int blade_idx)
    {
        (int node_idx_0, int node_idx_1, int node_idx_2) = find_node(i, j, tri_idx, blade_idx);

        // info (paul): These are the ground truth values
        float d_x_ref = bary[0] * d_xs[node_idx_0] + bary[1] * d_xs[node_idx_1] + bary[2] * d_xs[node_idx_2];
        float d_y_ref = -1f * (bary[0] * d_ys[node_idx_0] + bary[1] * d_ys[node_idx_1] + bary[2] * d_ys[node_idx_2]);//11112024 -// info (paul): The "-" turns around the picture (hopefully)
        float d_z_ref = bary[0] * d_zs[node_idx_0] + bary[1] * d_zs[node_idx_1] + bary[2] * d_zs[node_idx_2];
        return (d_x_ref, d_y_ref, d_z_ref);
    }
    /// <summary>
    /// Computes and saves the pixel-to-triangle assignment of a time step (mesh collider and ray casts).
    /// </summary>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="cam_idx">Camera index.</param>
    public void take_ref_pic(int blade_idx, int cam_idx)
    {
        // info (paul): assign and activate mesh collider
        List<GameObject> blades = collect_blades();
        Mesh mesh_l = blades[blade_idx].GetComponent<MeshFilter>().sharedMesh;

        blades[blade_idx].AddComponent<MeshCollider>();
        blades[blade_idx].GetComponent<MeshCollider>().sharedMesh = mesh_l;
        //blades[cam_idx].GetComponent<MeshCollider>().collider.convex = true;

        Stopwatch watch = new Stopwatch();
        watch.Start();
        List<List<float>> dists = find_dists(500000000);
        watch.Stop();
        long secs = watch.ElapsedMilliseconds;

        (float depth_min, float depth_max) = find_min_max(dists, with_padding: true);
        List<List<float>> dists_normed = norm_mat(dists);
        Texture2D depth_tex = mat2tex(dists_normed, with_switch_dims: true);
        save_png(depth_tex, cam_idx, blade_idx: blade_idx, label: "_depth");

        //15072024 save_floats_list_2_for_blade(dists_normed, cam_idx, blade_idx, label: "_depth_mat", with_uv_mode: false);

        save_float_for_blade(depth_min, cam_idx, blade_idx, label: "_depth_min", with_uv_mode: false);
        save_float_for_blade(depth_max, cam_idx, blade_idx, label: "_depth_max", with_uv_mode: false);

        // info (paul): deactivate mesh collider

    }
    /// <summary>
    /// Saves a value for a time step.
    /// </summary>
    /// <param name="value">Value.</param>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    public void save_float_for_blade(float value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "float", with_uv_mode: with_uv_mode);
        save_float(value, full_path: path);
    }
    /// <summary>
    /// Loads a float array of a time step.
    /// </summary>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    /// <returns>Array.</returns>
    public float[] load_floats_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats", with_uv_mode: with_uv_mode);
        //17012025path = "C:/Users/go73jem/Desktop/DIC_package/exp_normal/cam_0/floats/0__d_xsfloats_r128";
        float[] value = load_floats(full_path: path);
        return value;
    }
    /// <summary>
    /// Loads an integer array of a time step.
    /// </summary>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    /// <returns>Array.</returns>
    public int[] load_ints_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "ints", with_uv_mode: with_uv_mode);
        int[] value = load_ints(full_path: path);
        return value;
    }
    /// <summary>
    /// Loads a 2D integer array of a time step.
    /// </summary>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    /// <returns>Array or null.</returns>
    public int[,] load_ints2_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "ints2", with_uv_mode: with_uv_mode);
        int[,] value = load_ints2(full_path: path);
        if (value == null)
        {
            ;
        }
        return value;
    }
    /// <summary>
    /// Loads a 3D float array of a time step.
    /// </summary>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    /// <returns>Array or null.</returns>
    public float[][][] load_floats3_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats3", with_uv_mode: with_uv_mode);
        float[][][] value = load_floats3(full_path: path);
        if (value == null)
        {
            ;
        }
        return value;
    }

    /// <summary>
    /// Converts a nested list to a jagged array.
    /// </summary>
    /// <param name="lists">Nested list.</param>
    /// <returns>Jagged array.</returns>
    public float[][] lists_to_floats2(List<List<float>> lists)
    {
        float[][] floats = new float[lists.Count][];
        for (int i = 0; i < lists.Count; i++)
        {
            floats[i] = lists[i].ToArray();
        }

        return floats;
    }
    /// <summary>
    /// Converts a jagged array to a nested list.
    /// </summary>
    /// <param name="lists">Jagged array.</param>
    /// <returns>Nested list.</returns>
    public List<List<float>> floats2_to_lists(float[][] lists)
    {
        List<List<float>> floats = new List<List<float>>();
        if (lists == null)
        {
            ;
        }

        try
        {
            _ = lists.Length;
        }
        catch
        {
            _ = lists.Length;
        }

        for (int i = 0; i < lists.Length; i++)
        {
            floats.Add(new List<float>());
            floats[i] = lists[i].ToList();
        }

        return floats;
    }


    /// <summary>
    /// Shows an image (file or texture) in a panel.
    /// </summary>
    /// <param name="im_1_panel">Panel.</param>
    /// <param name="save_path">Image file (optional).</param>
    /// <param name="input">Texture (optional).</param>
    /// <returns>The panel.</returns>
    public Transform display_from_path(Transform im_1_panel, string save_path = null, Texture2D input = null)
    {
        // info (paul): assign to images to panels
        Texture2D texture = null;
        if (save_path != null)
        {
            byte[] bytes = File.ReadAllBytes(save_path);

            (int width_pre, int height_pre) = bytes2res(bytes);
            texture = new Texture2D(width_pre, height_pre, TextureFormat.ARGB32, false);
            texture.LoadImage(bytes);
        }

        if (input != null)
        {
            texture = input;
        }

        // info (paul): ;;
        Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), 100.0f);//texture

        // info (paul): ;;
        im_1_panel.GetComponent<UnityEngine.UI.Image>().sprite = sprite;
        return im_1_panel;
    }

    /// <summary>
    /// Saves a float array of a time step.
    /// </summary>
    /// <param name="value">Array.</param>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    public void save_floats_for_blade(float[] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats", with_uv_mode: with_uv_mode);
        save_floats(value, full_path: path);
    }
    /// <summary>
    /// Saves a nested float list of a time step.
    /// </summary>
    /// <param name="lists">Nested list.</param>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    public void save_floats_list_2_for_blade(List<List<float>> lists, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        float[][] value = lists_to_floats2(lists);
        save_floats2_for_blade(value, cam_idx, blade_idx, label, with_uv_mode);
    }
    /// <summary>
    /// Saves a jagged float array of a time step.
    /// </summary>
    /// <param name="value">Array.</param>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    public void save_floats2_for_blade(float[][] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats2", with_uv_mode: with_uv_mode);
        save_floats2(value, full_path: path);
    }
    /// <summary>
    /// Saves an integer array of a time step.
    /// </summary>
    /// <param name="value">Array.</param>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    public void save_ints_for_blade(int[] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "ints", with_uv_mode: with_uv_mode);
        save_ints(value, full_path: path);
    }
    /// <summary>
    /// Saves a 2D integer array of a time step.
    /// </summary>
    /// <param name="value">Array.</param>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    public void save_ints2_for_blade(int[,] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "ints2", with_uv_mode: with_uv_mode);
        save_ints2(value, full_path: path);
    }
    /// <summary>
    /// Saves a 3D float array of a time step.
    /// </summary>
    /// <param name="value">Array.</param>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    public void save_floats3_for_blade(float[][][] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats3", with_uv_mode: with_uv_mode);
        save_floats3(value, full_path: path);
    }

    /// <summary>
    /// Loads a value of a time step.
    /// </summary>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    /// <returns>Value.</returns>
    public float load_float_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "float", with_uv_mode: with_uv_mode);
        float value = load_float(full_path: path);
        return value;
    }
    /// <summary>
    /// Loads a nested float list of a time step.
    /// </summary>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="with_uv_mode">True to add the u/v mode to the path.</param>
    /// <returns>Nested list.</returns>
    public List<List<float>> load_floats_list_2_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        //float[][] value = lists2floats2(lists);
        string exp_l = remove_dots(get_experiment());
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats2", experiment: exp_l, with_uv_mode: with_uv_mode);
        float[][] floats = load_floats2(full_path: path);
        List<List<float>> lists = floats2_to_lists(floats);
        return lists;
    }


    /// <summary>
    /// Saves a value (binary serialisation).
    /// </summary>
    /// <param name="value">Value.</param>
    /// <param name="file_name">File name under persistentDataPath.</param>
    /// <param name="full_path">Full path (alternative).</param>
    public void save_float(float value, string file_name = null, string full_path = null)
    {
        string path = null;
        if (file_name != null)
        {
            path = UnityEngine.Application.persistentDataPath + "/" + file_name;
        }
        if (full_path != null)
        {
            path = full_path;
        }
        if (true)//(File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Create);

            formatter.Serialize(stream, value);
            stream.Close();
        }
    }

    /// <summary>
    /// Saves a float array.
    /// </summary>
    /// <param name="value">Array.</param>
    /// <param name="file_name">File name under persistentDataPath.</param>
    /// <param name="full_path">Full path (alternative).</param>
    public void save_floats(float[] value, string file_name = null, string full_path = null)
    {
        string path = null;
        if (file_name != null)
        {
            path = UnityEngine.Application.persistentDataPath + "/" + file_name;
        }
        if (full_path != null)
        {
            path = full_path;
        }
        if (true)//(File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Create);

            formatter.Serialize(stream, value);
            stream.Close();
        }
    }
    /// <summary>
    /// Saves a jagged float array.
    /// </summary>
    /// <param name="value">Array.</param>
    /// <param name="file_name">File name under persistentDataPath.</param>
    /// <param name="full_path">Full path (alternative).</param>
    public void save_floats2(float[][] value, string file_name = null, string full_path = null)
    {
        string path = null;
        if (file_name != null)
        {
            path = UnityEngine.Application.persistentDataPath + "/" + file_name;
        }
        if (full_path != null)
        {
            path = full_path;
        }
        if (true)//(File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Create);

            formatter.Serialize(stream, value);
            stream.Close();
        }
    }

    /// <summary>
    /// Saves an integer array.
    /// </summary>
    /// <param name="value">Array.</param>
    /// <param name="file_name">File name under persistentDataPath.</param>
    /// <param name="full_path">Full path (alternative).</param>
    public void save_ints(int[] value, string file_name = null, string full_path = null)
    {
        string path = null;
        if (file_name != null)
        {
            path = UnityEngine.Application.persistentDataPath + "/" + file_name;
        }
        if (full_path != null)
        {
            path = full_path;
        }
        if (true)//(File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Create);

            formatter.Serialize(stream, value);
            stream.Close();
        }
    }
    /// <summary>
    /// Saves a 2D integer array (and a fast binary copy).
    /// </summary>
    /// <param name="value">Array.</param>
    /// <param name="file_name">File name under persistentDataPath.</param>
    /// <param name="full_path">Full path (alternative).</param>
    public void save_ints2(int[,] value, string file_name = null, string full_path = null)
    {
        string path = null;
        if (file_name != null)
        {
            path = UnityEngine.Application.persistentDataPath + "/" + file_name;
        }
        if (full_path != null)
        {
            path = full_path;
        }
        //23092026 nur noch im schnellen Rohformat speichern (<pfad>.ibin); BinaryFormatter kostete bei
        //  grossen Aufloesungen viele Sekunden je Analyse. load_ints2 liest beide Formate.
        //  bisher: BinaryFormatter formatter = new BinaryFormatter(); formatter.Serialize(new FileStream(path, FileMode.Create), value);
        write_fast_ints2(path, value);
    }
    /// <summary>
    /// Saves a 3D float array (and a fast binary copy).
    /// </summary>
    /// <param name="value">Array.</param>
    /// <param name="file_name">File name under persistentDataPath.</param>
    /// <param name="full_path">Full path (alternative).</param>
    public void save_floats3(float[][][] value, string file_name = null, string full_path = null)
    {
        string path = null;
        if (file_name != null)
        {
            path = UnityEngine.Application.persistentDataPath + "/" + file_name;
        }
        if (full_path != null)
        {
            path = full_path;
        }
        //23092026 nur noch im schnellen Rohformat speichern (<pfad>.fbin), siehe save_ints2.
        //  bisher: BinaryFormatter formatter = new BinaryFormatter(); formatter.Serialize(new FileStream(path, FileMode.Create), value);
        write_fast_floats3(path, value);
    }

    //23092026 Schnelle Rohkopien grosser Arrays neben der BinaryFormatter-Datei (<pfad>.ibin / .fbin).
    //  BinaryFormatter braucht fuer float[1024][1024][3] ca. 20 s (ein Objekt je Pixel), das Rohformat
    //  Millisekunden. Die Kopie gilt nur, wenn sie nicht aelter als die Originaldatei ist; sonst wird
    //  sie ignoriert und beim naechsten Laden neu geschrieben. Fehler beim Schreiben sind unkritisch.
    const int FAST_MAGIC_I2 = 0x49324631; // "1F2I"
    const int FAST_MAGIC_F3 = 0x46334631; // "1F3F"

    /// <summary>
    /// Checks whether the fast binary copy exists and is not older than the original.
    /// </summary>
    /// <param name="original">Original file.</param>
    /// <param name="copy">Binary copy.</param>
    /// <returns>True if the copy can be used.</returns>
    static bool fast_copy_valid(string original, string copy)
    {
        // gueltig, wenn vorhanden und nicht aelter als eine evtl. noch vorhandene alte Datei
        return File.Exists(copy)
            && (!File.Exists(original) || File.GetLastWriteTimeUtc(copy) >= File.GetLastWriteTimeUtc(original));
    }

    /// <summary>
    /// Writes a 2D integer array as a fast binary copy (.ibin).
    /// </summary>
    /// <param name="path">Path of the original file.</param>
    /// <param name="data">Array.</param>
    static void write_fast_ints2(string path, int[,] data)
    {
        if (data == null) return;
        try
        {
            using (var w = new BinaryWriter(new BufferedStream(File.Create(path + ".ibin"), 1 << 20)))
            {
                int a = data.GetLength(0), b = data.GetLength(1);
                w.Write(FAST_MAGIC_I2); w.Write(a); w.Write(b);
                byte[] buf = new byte[a * b * sizeof(int)];
                Buffer.BlockCopy(data, 0, buf, 0, buf.Length);
                w.Write(buf);
            }
        }
        catch (Exception e) { Debug.LogWarning("Schnellkopie nicht geschrieben: " + path + ".ibin (" + e.Message + ")"); }
    }

    /// <summary>
    /// Reads the fast binary copy of a 2D integer array.
    /// </summary>
    /// <param name="path">Path of the original file.</param>
    /// <returns>Array, or null.</returns>
    static int[,] read_fast_ints2(string path)
    {
        string copy = path + ".ibin";
        if (!fast_copy_valid(path, copy)) return null;
        try
        {
            byte[] all = File.ReadAllBytes(copy);
            if (all.Length < 12 || BitConverter.ToInt32(all, 0) != FAST_MAGIC_I2) return null;
            int a = BitConverter.ToInt32(all, 4), b = BitConverter.ToInt32(all, 8);
            if (all.Length != 12 + a * b * sizeof(int)) return null;
            int[,] data = new int[a, b];
            Buffer.BlockCopy(all, 12, data, 0, a * b * sizeof(int));
            return data;
        }
        catch (Exception) { return null; }
    }

    /// <summary>
    /// Writes a 3D float array as a fast binary copy (.fbin).
    /// </summary>
    /// <param name="path">Path of the original file.</param>
    /// <param name="data">Array.</param>
    static void write_fast_floats3(string path, float[][][] data)
    {
        if (data == null) return;
        try
        {
            using (var w = new BinaryWriter(new BufferedStream(File.Create(path + ".fbin"), 1 << 20)))
            {
                w.Write(FAST_MAGIC_F3);
                w.Write(data.Length);
                foreach (float[][] row in data)
                {
                    w.Write(row == null ? -1 : row.Length);
                    if (row == null) continue;
                    foreach (float[] cell in row)
                    {
                        w.Write(cell == null ? -1 : cell.Length);
                        if (cell == null) continue;
                        foreach (float f in cell) w.Write(f);
                    }
                }
            }
        }
        catch (Exception e) { Debug.LogWarning("Schnellkopie nicht geschrieben: " + path + ".fbin (" + e.Message + ")"); }
    }

    /// <summary>
    /// Reads the fast binary copy of a 3D float array.
    /// </summary>
    /// <param name="path">Path of the original file.</param>
    /// <returns>Array, or null.</returns>
    static float[][][] read_fast_floats3(string path)
    {
        string copy = path + ".fbin";
        if (!fast_copy_valid(path, copy)) return null;
        try
        {
            byte[] all = File.ReadAllBytes(copy);
            int pos = 0;
            Func<int> next_int = () => { int v = BitConverter.ToInt32(all, pos); pos += 4; return v; };
            if (all.Length < 8 || next_int() != FAST_MAGIC_F3) return null;
            int a = next_int();
            float[][][] data = new float[a][][];
            for (int i = 0; i < a; i++)
            {
                int b = next_int();
                if (b < 0) continue;
                data[i] = new float[b][];
                for (int j = 0; j < b; j++)
                {
                    int c = next_int();
                    if (c < 0) continue;
                    float[] cell = new float[c];
                    Buffer.BlockCopy(all, pos, cell, 0, c * sizeof(float));
                    pos += c * sizeof(float);
                    data[i][j] = cell;
                }
            }
            return data;
        }
        catch (Exception) { return null; }
    }

    /// <summary>
    /// Loads a value.
    /// </summary>
    /// <param name="file_name">File name under persistentDataPath.</param>
    /// <param name="full_path">Full path (alternative).</param>
    /// <returns>Value.</returns>
    public float load_float(string file_name = null, string full_path = null)
    {
        string path = null;
        if (file_name != null)
        {
            path = UnityEngine.Application.persistentDataPath + "/" + file_name;
        }
        if (full_path != null)
        {
            path = full_path;
        }

        float data = float.NaN;
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            data = (float)formatter.Deserialize(stream);
            stream.Close();
        }
        else
        {
            data = 0f; // or whatever a good default value is
        }

        return data;
    }
    /// <summary>
    /// Loads a jagged float array.
    /// </summary>
    /// <param name="file_name">File name under persistentDataPath.</param>
    /// <param name="full_path">Full path (alternative).</param>
    /// <returns>Array or null.</returns>
    public float[][] load_floats2(string file_name = null, string full_path = null)
    {
        string path = null;
        if (file_name != null)
        {
            path = UnityEngine.Application.persistentDataPath + "/" + file_name;
        }
        if (full_path != null)
        {
            path = full_path;
        }

        float[][] data = null;
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            data = (float[][])formatter.Deserialize(stream);
            stream.Close();
        }
        else
        {
            data = null; // or whatever a good default value is
        }

        return data;
    }

    /// <summary>
    /// Reads the first line of a text file.
    /// </summary>
    /// <param name="file_name">File.</param>
    /// <returns>Line, or null.</returns>
    public string load_txt_line(string file_name)
    {
        //File file = null;
        StreamReader inp_stm = null;
        try
        {
            inp_stm = new StreamReader(file_name);
        }
        catch
        {
            inp_stm = new StreamReader(file_name);
        }
        string line = inp_stm.ReadLine();

        return line;
    }

    //23092026 Datei-Zwischenspeicher fuer grosse, nur gelesene Arrays (Maske, baryzentrische
    //  Koordinaten). Gueltig, solange Pfad, Aenderungszeit und Groesse gleich sind; eine neue
    //  Analyse ueberschreibt die Dateien und wird damit automatisch neu geladen.
    private static readonly Dictionary<string, (DateTime mtime, long len, object data)> file_cache =
        new Dictionary<string, (DateTime, long, object)>();
    /// <summary>
    /// Returns cached file content if the file is unchanged (time and size).
    /// </summary>
    /// <param name="path">File.</param>
    /// <param name="data">Cached content.</param>
    /// <returns>True if found.</returns>
    static bool try_get_cached(string path, out object data)
    {
        data = null;
        FileInfo fi = new FileInfo(path);
        if (file_cache.TryGetValue(path, out var entry) && entry.mtime == fi.LastWriteTimeUtc && entry.len == fi.Length)
        {
            data = entry.data;
            return true;
        }
        return false;
    }
    /// <summary>
    /// Caches loaded file content (limited to 16 entries).
    /// </summary>
    /// <param name="path">File.</param>
    /// <param name="data">Content.</param>
    static void put_cached(string path, object data)
    {
        FileInfo fi = new FileInfo(path);
        if (file_cache.Count > 16)
            file_cache.Clear(); // einfache Begrenzung (Aufloesungswechsel)
        file_cache[path] = (fi.LastWriteTimeUtc, fi.Length, data);
    }

    /// <summary>
    /// Loads a float array.
    /// </summary>
    /// <param name="file_name">File name under persistentDataPath.</param>
    /// <param name="full_path">Full path (alternative).</param>
    /// <returns>Array or null.</returns>
    public float[] load_floats(string file_name = null, string full_path = null)
    {
        string path = null;
        if (file_name != null)
        {
            path = UnityEngine.Application.persistentDataPath + "/" + file_name;
        }
        if (full_path != null)
        {
            path = full_path;
        }

        float[] data = null;
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            data = (float[])formatter.Deserialize(stream);
            stream.Close();
        }
        else
        {
            data = null; // or whatever a good default value is
        }

        return data;
    }
    /// <summary>
    /// Loads an integer array.
    /// </summary>
    /// <param name="file_name">File name under persistentDataPath.</param>
    /// <param name="full_path">Full path (alternative).</param>
    /// <returns>Array or null.</returns>
    public int[] load_ints(string file_name = null, string full_path = null)
    {
        string path = null;
        if (file_name != null)
        {
            path = UnityEngine.Application.persistentDataPath + "/" + file_name;
        }
        if (full_path != null)
        {
            path = full_path;
        }

        int[] data = null;
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            data = (int[])formatter.Deserialize(stream);
            stream.Close();
        }
        else
        {
            data = null; // or whatever a good default value is
        }

        return data;
    }
    /// <summary>
    /// Loads a 2D integer array (fast copy or cache if possible).
    /// </summary>
    /// <param name="file_name">File name under persistentDataPath.</param>
    /// <param name="full_path">Full path (alternative).</param>
    /// <returns>Array or null.</returns>
    public int[,] load_ints2(string file_name = null, string full_path = null)
    {
        string path = null;
        if (file_name != null)
        {
            path = UnityEngine.Application.persistentDataPath + "/" + file_name;
        }
        if (full_path != null)
        {
            path = full_path;
        }

        int[,] data = null;
        //23092026 Schnelles Rohformat (<pfad>.ibin, seit 23.09.2026 das einzige, das gespeichert wird)
        //  bevorzugen; alte BinaryFormatter-Dateien werden beim ersten Laden einmalig umgewandelt.
        //  Zwischenspeicher im Speicher, da "save"/"Genauigkeit" dieselbe Datei mehrfach laden.
        bool use_copy = fast_copy_valid(path, path + ".ibin");
        string src = use_copy ? path + ".ibin" : path;
        if (File.Exists(src))
        {
            if (try_get_cached(src, out object cached))
                return (int[,])cached;
            if (use_copy)
                data = read_fast_ints2(path);
            if (data == null && File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(path, FileMode.Open);

                data = (int[,])formatter.Deserialize(stream);
                stream.Close();
                write_fast_ints2(path, data);
            }
            if (data != null)
                put_cached(src, data);
        }
        else
        {
            data = null; // or whatever a good default value is
        }

        return data;
    }
    /// <summary>
    /// Loads a 3D float array (fast copy or cache if possible).
    /// </summary>
    /// <param name="file_name">File name under persistentDataPath.</param>
    /// <param name="full_path">Full path (alternative).</param>
    /// <returns>Array or null.</returns>
    public float[][][] load_floats3(string file_name = null, string full_path = null)
    {
        string path = null;
        if (file_name != null)
        {
            path = UnityEngine.Application.persistentDataPath + "/" + file_name;
        }
        if (full_path != null)
        {
            path = full_path;
        }

        float[][][] data = null;
        //23092026 wie load_ints2 (Rohformat <pfad>.fbin bevorzugen); float[][][] ist fuer
        //  BinaryFormatter besonders teuer (ein Objekt je Pixel, bei r1024 ca. 20 s).
        bool use_copy = fast_copy_valid(path, path + ".fbin");
        string src = use_copy ? path + ".fbin" : path;
        if (File.Exists(src))
        {
            if (try_get_cached(src, out object cached))
                return (float[][][])cached;
            if (use_copy)
                data = read_fast_floats3(path);
            if (data == null && File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(path, FileMode.Open);

                data = (float[][][])formatter.Deserialize(stream);
                stream.Close();
                write_fast_floats3(path, data);
            }
            if (data != null)
                put_cached(src, data);
        }
        else
        {
            data = null; // or whatever a good default value is
        }

        return data;
    }
    /// <summary>
    /// Legacy: distance of every pixel to the camera by ray casting.
    /// </summary>
    /// <param name="curb">Maximum number of operations (debugging).</param>
    /// <returns>Distance map.</returns>
    public List<List<float>> find_dists(int curb = 99999999)
    {
        // info (paul): curb is to curb the number of operations, in case 
        //          that the resolution is high, so that for debugging 
        //          unity does not get stuck in an almost indefinite loop

        int width = cam_for_uv_0.pixelWidth;
        int height = cam_for_uv_1.pixelHeight;

        List<List<float>> dists = zeros_of_size(len_x: width, len_y: height);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (curb > 0)
                {
                    Ray ray_ij = cam_for_uv_0.ScreenPointToRay(new Vector3(i, j));

                    RaycastHit hit;
                    bool has_hit = Physics.Raycast(cam_for_uv_0.transform.position, ray_ij.direction, out hit, Mathf.Infinity);
                    if (has_hit)
                    {
                        dists[i][j] = hit.distance;
                    }
                    curb -= 1;
                }
            }
        }

        return dists;
    }

    /// <summary>
    /// Renders the image of a camera into a texture.
    /// </summary>
    /// <param name="cam_idx">Camera index.</param>
    /// <returns>Texture.</returns>
    public Texture2D cam2tex(int cam_idx)
    {
        int our_height = this.get_render_res();//11062024 256
        // info (paul): crate render_tex
        RenderTexture render_tex = null;

        if (cam_idx == 0)
        {
            render_tex = cam_for_uv_0.targetTexture;
        }
        if (cam_idx == 1)
        {
            render_tex = cam_for_uv_1.targetTexture;
        }
        //12012025A-continue here tomorrow
        // info (paul): create texture2D
        Texture2D tex = new Texture2D(our_height, our_height);
        RenderTexture.active = render_tex;
        tex.ReadPixels(new Rect(0, 0, our_height, our_height), 0, 0);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Renders the comparison image for the real image (same gray-value pipeline as the experiments, own field of view and aspect, optionally with the projected experiment texture or camera 1).
    /// </summary>
    /// <param name="comparisonFov">Field of view in degrees.</param>
    /// <param name="comparisonAspect">Aspect ratio.</param>
    /// <param name="projectedExperimentalTexture">Projected experiment texture (optional).</param>
    /// <param name="textureScaleX">Texture scale x.</param>
    /// <param name="textureScaleY">Texture scale y.</param>
    /// <param name="textureOffsetX">Texture offset x.</param>
    /// <param name="textureOffsetY">Texture offset y.</param>
    /// <param name="useCam1">True for camera 1.</param>
    /// <returns>Image.</returns>
    public Texture2D capture_nakajima_comparison_image(float comparisonFov = 55f,
        float comparisonAspect = 1f, Texture2D projectedExperimentalTexture = null,
        float textureScaleX = 1f, float textureScaleY = 1f,
        float textureOffsetX = 0f, float textureOffsetY = 0f, bool useCam1 = false)
    {
        // The comparison must use the same illuminated, grayscale image pipeline as
        // the DIC experiments. cam2tex() alone is the raw HDRP buffer and can be very
        // dark and coloured, depending on the last UI action.
        //18092026 Nutzerwunsch: Vergleichsbild wahlweise mit der jeweils anderen Kamera
        //aufnehmen (cam_1 statt cam_0), die um den gleichen Winkel in die entgegengesetzte
        //Richtung geneigt ist ("umgekehrte Richtung um 10 Grad geneigt").
        Camera captureCam = useCam1 ? cam_for_uv_1 : cam_for_uv_0;
        Camera syncCam = useCam1 ? cam_for_uv_0 : cam_for_uv_1;
        if (captureCam == null)
            throw new InvalidOperationException("Nakajima render camera "
                + (useCam1 ? "cam_1" : "cam_0") + " is not initialized.");

        // Use the same lighting direction and specimen state as the regular lighting
        // analysis. Otherwise the result depends on whichever analysis was run last:
        // with the old 30 degree light, only the curved centre of a deformed specimen
        // was illuminated and appeared as a circular patch on black.
        Params comparisonParams = new Params(speckle_size: 0.035f, lighting_intensity: 1f,
            gaussian_error: 0f, poisson_error: 0f);
        set_up_lighting(y_coord: comparisonParams.get_lighting_pos_y(), intensity: 1f);

        set_speckle_file("speckle_0.035");
        // Compare like with like: the first experimental cam00 image is the
        // undeformed acquisition, so use the first (planar) simulation mesh.
        //18092026 Nutzerwunsch: die zuvor aus verts_1.txt rekonstruierte Referenzprobe
        //war im Vergleich zum Experiment zu breit. Fuer "Realbild: Neu" wird jetzt
        //stattdessen die neue, korrekt schmale FBX-Probe (nakajima_50_fbx.fbx) geladen,
        //texturiert und aufgenommen.
        string referenceName = "nakajima_50_fbx_blade";
        List<GameObject> comparisonBlades = collect_blades();
        if (comparisonBlades.Find(blade => blade != null && blade.name == referenceName) == null)
        {
            // The gallery button can be used immediately after entering Play Mode,
            // before an analysis has created any specimens. Build the reference mesh
            // here and initialise its planar UVs synchronously; Vis_action normally
            // does this later in OnBecameVisible, which is too late for Camera.Render().
            GameObject createdBlade = load_nakajima_fbx_blade();
            Mesh createdMesh = createdBlade.GetComponent<MeshFilter>().mesh;
            //20092026 Bugfix: die planare UV-Projektion lief bisher fest auf die
            //lokalen x/y-Achsen. Die FBX-Probe liegt aber in der x/z-Ebene (y ist
            //fuer alle Vertices konstant), d.h. rangeY war ~0 und v ueber die ganze
            //Flaeche konstant -> Textur wurde zu Streifen verschmiert. Jetzt werden
            //die beiden Achsen mit der groessten Ausdehnung als u/v verwendet.
            createdMesh.uv = planar_uvs_from_largest_axes(createdMesh.vertices);

            // info (paul): consistent with load_blade_from_verts(blade_idx != -1):
            // park the specimen under "blades" so collect_blades() finds it again
            // on the next comparison call instead of recreating/duplicating it.
            GameObject blades_parent = GameObject.Find("blades");
            if (blades_parent != null)
                createdBlade.transform.SetParent(blades_parent.transform, true);

            comparisonBlades = collect_blades();
            Debug.Log("Nakajima comparison created missing FBX reference specimen from "
                + "Resources/Targets/fbx_files/nakajima/nakajima_50_fbx");
        }

        GameObject comparisonBlade = comparisonBlades.Find(blade =>
            blade != null && blade.name == referenceName);
        if (comparisonBlade == null)
            comparisonBlade = comparisonBlades.Find(blade => blade != null && blade.activeSelf);
        if (comparisonBlade == null)
            comparisonBlade = comparisonBlades[0];

        //18092026 Nutzerwunsch: Skalierung/Rotation der FBX-Probe auch dann neu
        //anwenden, wenn sie schon vorher (mit ggf. veralteten Werten) in dieser
        //Play-Session angelegt wurde - sonst wirkt eine Aenderung an
        //nakajima_fbx_blade_scale erst nach einem Play-Neustart.
        if (comparisonBlade.name == referenceName)
            apply_nakajima_fbx_blade_transform(comparisonBlade);

        foreach (GameObject blade in comparisonBlades)
        {
            if (blade != null)
                blade.SetActive(blade == comparisonBlade);
        }
        if (comparisonBlade.GetComponent<Renderer>() == null)
            throw new InvalidOperationException("Die Nakajima-Vergleichsprobe besitzt keinen Renderer.");
        apply_speckles(comparisonBlade);
        // For the comparison render, use a seamless random-phase texture whose
        // histogram and spatial power spectrum were measured from the central
        // speckled region of cam00/image-000000. Other rendering paths continue
        // to use the existing selectable speckle materials.
        Material comparisonSpeckleMaterial = comparisonBlade.GetComponent<Renderer>().material;
        Texture2D measuredSpeckle = load_experimental_statistics_speckle();
        //20092026 Nutzerwunsch: synthetische Textur wirkte zu fein -> Tiling-Faktor
        //(vorher 5x5, d.h. die Textur wiederholte sich 5x ueber die Probe) um Faktor 2
        //verringert (5 / 2 = 2.5), damit die Speckles gleichmaessig groeber erscheinen.
        Vector2 comparisonSpeckleTiling = new Vector2(2.5f, 2.5f);
        if (comparisonSpeckleMaterial.HasProperty("_BaseColorMap"))
        {
            if (measuredSpeckle != null)
                comparisonSpeckleMaterial.SetTexture("_BaseColorMap", measuredSpeckle);
            comparisonSpeckleMaterial.SetTextureScale("_BaseColorMap", comparisonSpeckleTiling);
        }
        if (comparisonSpeckleMaterial.HasProperty("_MainTex"))
        {
            if (measuredSpeckle != null)
                comparisonSpeckleMaterial.SetTexture("_MainTex", measuredSpeckle);
            comparisonSpeckleMaterial.SetTextureScale("_MainTex", comparisonSpeckleTiling);
        }
        Debug.Log("Nakajima comparison uses specimen '" + comparisonBlade.name
            + "' with standard lighting direction "
            + comparisonParams.get_lighting_pos_y().ToString(CultureInfo.InvariantCulture) + " degrees.");

        float oldFov0 = captureCam.fieldOfView;
        float oldFov1 = syncCam != null ? syncCam.fieldOfView : oldFov0;
        float oldAspect0 = captureCam.aspect;
        float oldAspect1 = syncCam != null ? syncCam.aspect : oldAspect0;
        RenderTexture oldTarget0 = captureCam.targetTexture;
        RenderTexture oldActive = RenderTexture.active;
        Mesh comparisonMesh = comparisonBlade.GetComponent<MeshFilter>().mesh;
        Vector2[] oldComparisonUvs = null;
        Material oldComparisonMaterial = null;
        Material experimentalMaterial = null;
        HDAdditionalCameraData hdCameraData = captureCam.GetComponent<HDAdditionalCameraData>();
        bool oldCustomRenderingSettings = false;
        FrameSettings oldFrameSettings = default(FrameSettings);
        FrameSettingsOverrideMask oldFrameSettingsOverrideMask = default(FrameSettingsOverrideMask);
        if (hdCameraData != null)
        {
            // The FOV search renders several projection changes back-to-back. An
            // active HDRP Motion Blur volume interprets those changes as rapid
            // camera motion and produces radial zoom streaks. The experiment
            // camera is stationary, so override Motion Blur for this capture only.
            oldCustomRenderingSettings = hdCameraData.customRenderingSettings;
            oldFrameSettings = hdCameraData.renderingPathCustomFrameSettings;
            oldFrameSettingsOverrideMask = hdCameraData.renderingPathCustomFrameSettingsOverrideMask;
            hdCameraData.customRenderingSettings = true;
            hdCameraData.renderingPathCustomFrameSettings.SetEnabled(
                FrameSettingsField.MotionBlur, false);
            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[
                (uint)FrameSettingsField.MotionBlur] = true;
        }
        comparisonAspect = Mathf.Clamp(comparisonAspect, 0.5f, 2.5f);
        int comparisonHeight = get_render_res();
        int comparisonWidth = Mathf.Max(2, Mathf.RoundToInt(comparisonHeight * comparisonAspect));
        RenderTexture comparisonTarget = new RenderTexture(comparisonWidth, comparisonHeight, 24,
            RenderTextureFormat.ARGB32);
        comparisonTarget.Create();
        captureCam.fieldOfView = Mathf.Clamp(comparisonFov, 2f, 120f);
        captureCam.aspect = comparisonAspect;
        //21092026 fuer den Nakajima-Look der TV-Pipeline merken (siehe nakajima_equivalent_square_fov)
        nakajima_real_aspect = comparisonAspect;
        nakajima_real_fov = captureCam.fieldOfView;
        PlayerPrefs.SetFloat("nakajima_real_aspect", nakajima_real_aspect);
        PlayerPrefs.SetFloat("nakajima_real_fov", nakajima_real_fov);
        captureCam.targetTexture = comparisonTarget;
        if (syncCam != null)
        {
            syncCam.fieldOfView = captureCam.fieldOfView;
            syncCam.aspect = comparisonAspect;
        }

        if (projectedExperimentalTexture != null)
        {
            // Project the registered experimental photograph through the same
            // camera used for rendering. The UV transform is identical to the
            // one used by WarpRealToRendered in the comparison analysis.
            oldComparisonUvs = comparisonMesh.uv;
            Vector3[] vertices = comparisonMesh.vertices;
            Vector2[] projectedUvs = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 viewport = captureCam.WorldToViewportPoint(
                    comparisonBlade.transform.TransformPoint(vertices[i]));
                projectedUvs[i] = new Vector2(
                    0.5f + (viewport.x - 0.5f) * textureScaleX + textureOffsetX,
                    0.5f + (viewport.y - 0.5f) * textureScaleY + textureOffsetY);
            }
            comparisonMesh.uv = projectedUvs;

            Renderer comparisonRenderer = comparisonBlade.GetComponent<Renderer>();
            oldComparisonMaterial = comparisonRenderer.material;
            experimentalMaterial = new Material(oldComparisonMaterial);
            projectedExperimentalTexture.wrapMode = TextureWrapMode.Clamp;
            projectedExperimentalTexture.filterMode = FilterMode.Bilinear;
            if (experimentalMaterial.HasProperty("_BaseColorMap"))
            {
                experimentalMaterial.SetTexture("_BaseColorMap", projectedExperimentalTexture);
                experimentalMaterial.SetTextureScale("_BaseColorMap", Vector2.one);
            }
            if (experimentalMaterial.HasProperty("_MainTex"))
            {
                experimentalMaterial.SetTexture("_MainTex", projectedExperimentalTexture);
                experimentalMaterial.SetTextureScale("_MainTex", Vector2.one);
            }
            if (experimentalMaterial.HasProperty("_BaseColor"))
                experimentalMaterial.SetColor("_BaseColor", Color.white);
            if (experimentalMaterial.HasProperty("_Color"))
                experimentalMaterial.SetColor("_Color", Color.white);
            comparisonRenderer.material = experimentalMaterial;
        }

        try
        {
            captureCam.Render();
            RenderTexture.active = comparisonTarget;
            Texture2D raw = new Texture2D(comparisonWidth, comparisonHeight, TextureFormat.RGB24, false);
            raw.ReadPixels(new Rect(0, 0, comparisonWidth, comparisonHeight), 0, 0);
            raw.Apply();
            Texture2D processed = post_proc(raw, comparisonParams);
            Destroy(raw);
            return processed;
        }
        finally
        {
            RenderTexture.active = oldActive;
            captureCam.targetTexture = oldTarget0;
            captureCam.fieldOfView = oldFov0;
            captureCam.aspect = oldAspect0;
            if (hdCameraData != null)
            {
                hdCameraData.renderingPathCustomFrameSettings = oldFrameSettings;
                hdCameraData.renderingPathCustomFrameSettingsOverrideMask =
                    oldFrameSettingsOverrideMask;
                hdCameraData.customRenderingSettings = oldCustomRenderingSettings;
            }
            if (syncCam != null)
            {
                syncCam.fieldOfView = oldFov1;
                syncCam.aspect = oldAspect1;
            }
            if (projectedExperimentalTexture != null)
            {
                comparisonMesh.uv = oldComparisonUvs;
                comparisonBlade.GetComponent<Renderer>().material = oldComparisonMaterial;
                if (experimentalMaterial != null)
                    Destroy(experimentalMaterial);
            }
            comparisonTarget.Release();
            Destroy(comparisonTarget);
        }
    }

    /// <summary>
    /// Returns the name of the current experiment.
    /// </summary>
    /// <returns>Experiment name.</returns>
    public string get_experiment()
    {
        return this.experiment;
    }
    /// <summary>
    /// Sets the name of the current experiment.
    /// </summary>
    /// <param name="input">Experiment name.</param>
    public void set_experiment(string input)
    {
        if (input == null)
        {
            ;
        }
        if (input == "exp_normal")
        {
            ;
        }

        this.experiment = input;
    }

    /// <summary>
    /// Builds the file path of a data item of an experiment, camera, and time step.
    /// </summary>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="type">Data type or extension.</param>
    /// <param name="experiment">Experiment (null = current).</param>
    /// <param name="with_uv_mode">True to add the u/v mode.</param>
    /// <returns>Path.</returns>
    public string construct_blade_path(int cam_idx, int blade_idx, string label,
        string type = ".png", string experiment = null, bool with_uv_mode = false)
    {
        //25062024 string path = Application.persistentDataPath + "/blade_" + blade_idx.ToString() + "_cam_" + cam_idx.ToString() + label + type;

        //03072024 string dir_path = Application.persistentDataPath + "/" + this.get_experiment().ToString() + "/cam_" + cam_idx + "/";
        //26072024 string dir_path = "C:/Users/go73jem/Desktop/DIC_package/" + this.get_experiment().ToString() + "/cam_" + cam_idx + "/";
        //05092024 string dir_path = "C:/Users/go73jem/Desktop/DIC_package/" + "exp_normal" + "/cam_" + cam_idx + "/";

        if (experiment == null)
        {
            experiment = remove_dots(get_experiment());
        }

        string project_dir = path_dic + experiment;
        string dir_path = project_dir + "/cam_" + cam_idx + "/";

        if (with_uv_mode)
        {
            dir_path += (this.paint_with + "/");
            //uv_mode = "uv";
            //uv_mode = "heights";
        }
        if (type == "float" || type == "floats" || type == "floats2" || type == "ints2")
        {
            dir_path += (type + "/");
        }

        System.IO.Directory.CreateDirectory(dir_path);
        string path = dir_path + blade_idx.ToString() + "_" + label + type;
        path = path + "_r" + get_render_res().ToString();

        return path;
    }
    /// <summary>
    /// Saves a texture as PNG (default: screenshot path).
    /// </summary>
    /// <param name="tex">Texture.</param>
    /// <param name="path">Output path.</param>
    public void save_tex(Texture2D tex, string path = null)
    {
        if (path == null)
        {
            // info (paul): for the screenshot taking
            path = path_dic + "exp_normal/time_flow_v/nice_pics/screenshot.png";

            // info (paul): read path params
            string exp = get_experiment();
            string paint_mode = get_paint_with();
            string strain_mode = get_strain_mode();
            string plot_or_heights_mode = get_plot_or_heights_mode();

            float v_mean = get_v_mean();
            float v_std = get_v_std();
            float v_min = get_v_min();
            float v_max = get_v_max();

            // info (paul): save params
            //21092026 Nutzerwunsch: Komponente (u/v) und Aufloesung in den Dateinamen, damit
            //sich Exporte fuer r128/r256/r512 bzw. u/v nicht gegenseitig ueberschreiben.
            string variant = "_" + get_u_v_mode() + "_r" + get_render_res().ToString();
            string file_name = "params_" + exp + "_" + paint_mode + "_" + strain_mode + "_" +
                plot_or_heights_mode + variant + ".txt";
            string params_path = path_dic + "exp_normal/time_flow_v/nice_pics/" +
                file_name;

            string info_str = exp + "\t" + paint_mode + "\t" + strain_mode + "\t" +
                plot_or_heights_mode + "\t" + v_mean + "\t" + v_std + "\t" + v_min + "\t" + v_max;
            write_to_txt(params_path, info_str, mode: "replace");

            // info (paul): save im
            file_name = "im_" + exp + "_" + paint_mode + "_" + strain_mode + "_" + plot_or_heights_mode + variant + ".png";
            string im_path = path_dic + "exp_normal/time_flow_v/nice_pics/" +
                file_name;
            path = im_path;

            //20092026 Nutzerwunsch: die exportierte (encodierte) Karte gleich in Unity
            //als eingefaerbte Karte (blau-schwarz-rot bzw. schwarz-rot + Farbbalken)
            //in der Bildergalerie anzeigen, statt sie separat per Skript zu plotten.
            try
            {
                string plot_name = "plot_" + exp + "_" + strain_mode + "_" + plot_or_heights_mode + variant
                    + "_min" + v_min.ToString("0.###", CultureInfo.InvariantCulture)
                    + "_max" + v_max.ToString("0.###", CultureInfo.InvariantCulture) + ".png";
                string plot_path = path_dic + "exp_normal/time_flow_v/nice_pics/" + plot_name;
                Texture2D plot_tex = colorize_encoded_map(tex, plot_or_heights_mode, v_min, v_max);
                System.IO.File.WriteAllBytes(plot_path, plot_tex.EncodeToPNG());
                Destroy(plot_tex);
                ExperimentImageGallery.AddRenderedImage(plot_path);
                ExperimentImageGallery.ShowResultsWindow();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Karten-Plot konnte nicht erzeugt werden: " + exception.Message);
            }
        }
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
    }

    //20092026 Dekodiert eine mit mat2tex/norm_mat encodierte Karte (positiv: grau R=G=B=p,
    //negativ: gruen R=0,G=p; siehe matrix2list) zurueck in Werte und faerbt sie ein:
    //value/value_ref: blau (negativ) - schwarz (0) - rot (positiv), symmetrisch um 0;
    //loss_abs/loss_rel: schwarz (0) - rot (max). Rechts ein Farbbalken. Die Zahlenwerte
    //(min/max) stehen im Dateinamen, den die Galerie als Titel anzeigt.
    //23092026 optional scale_min/scale_max: gemeinsame Farbskala (z.B. Flow und Flow ref im
    //  Genauigkeits-Panel); v_min/v_max bleiben die Werte zum Dekodieren dieser Datei.
    /// <summary>
    /// Colours an encoded map (loss: black to red, otherwise diverging) and adds a colour bar with labels.
    /// </summary>
    /// <param name="encoded">Encoded map.</param>
    /// <param name="mode">Display mode.</param>
    /// <param name="v_min">Minimum for decoding.</param>
    /// <param name="v_max">Maximum for decoding.</param>
    /// <param name="scale_min">Common colour scale minimum (optional).</param>
    /// <param name="scale_max">Common colour scale maximum (optional).</param>
    /// <returns>Coloured image.</returns>
    public Texture2D colorize_encoded_map(Texture2D encoded, string mode, float v_min, float v_max,
        float scale_min = float.NaN, float scale_max = float.NaN)
    {
        int w = encoded.width;
        int h = encoded.height;
        int bar_gap = Mathf.Max(4, w / 32);
        int bar_w = Mathf.Max(8, w / 16);
        bool is_loss = mode != null && mode.StartsWith("loss");
        bool plus_minus = v_min < 0f && v_max > 0f;
        float lim = Mathf.Max(Mathf.Abs(v_min), Mathf.Abs(v_max), 1e-9f);
        //21092026 Farbskala: nur bei beiden Vorzeichen symmetrisch um 0 (blau-schwarz-rot),
        //sonst linear ueber den tatsaechlichen Bereich [v_min, v_max] (schwarz -> rot bzw.
        //schwarz -> blau), damit z.B. ein v-Feld von 6.1..6.7 px nicht komplett rot wird.
        //27092026 log-Karten ("log_loss_abs"/"log_loss_rel", Werte = log10 des Fehlers): schwarz -> rot
        //  ueber den tatsaechlichen Bereich; Beschriftung als 10^x (siehe unten)
        bool is_log = mode != null && mode.StartsWith("log_");
        ColorScale scale = new ColorScale(is_loss, plus_minus, v_min, v_max, lim, sequential: is_log);
        if (!float.IsNaN(scale_min) && !float.IsNaN(scale_max))
        {
            scale = new ColorScale(is_loss, scale_min < 0f && scale_max > 0f, scale_min, scale_max,
                Mathf.Max(Mathf.Abs(scale_min), Mathf.Abs(scale_max), 1e-9f), sequential: is_log);
        }

        //23092026 Beschriftung des Farbbalkens (Pixelschrift, siehe draw_label): Platz rechts daneben
        int font_s = Mathf.Max(1, h / 170);
        int label_gap = 2 * font_s;
        int label_w = 7 * 6 * font_s;
        bool as_percent = mode == "loss_rel" || mode == "log_loss_rel";

        Color[] src = encoded.GetPixels();
        Texture2D outTex = new Texture2D(w + bar_gap + bar_w + label_gap + label_w, h, TextureFormat.RGBA32, false);
        Color[] dst = new Color[outTex.width * outTex.height];
        for (int i = 0; i < dst.Length; i++)
            dst[i] = Color.black;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = src[y * w + x];
                if (c.r <= 0f && c.g <= 0f)
                {
                    dst[y * outTex.width + x] = Color.black;           // kein Wert (ausserhalb der Probe)
                    continue;
                }
                float p = c.r > 0f ? c.r : -c.g;                       // encodierter Wert in [-1, 1]
                float value = plus_minus
                    ? (p >= 0f ? p * v_max : p * (-v_min))              // norm_mat, Fall min<0<max
                    : p * (v_max - v_min) + v_min;                      // norm_mat, sonst
                dst[y * outTex.width + x] = scale.map(value);
            }
        }

        // Farbbalken rechts (unten = Skalenminimum, oben = Skalenmaximum)
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / Mathf.Max(1, h - 1);
            Color c = scale.map(scale.lo + (scale.hi - scale.lo) * t);
            for (int x = w + bar_gap; x < w + bar_gap + bar_w; x++)
                dst[y * outTex.width + x] = c;
        }

        //23092026 Werte am Farbbalken: oben max, unten min, bei Vorzeichenwechsel auch die 0
        int lx = w + bar_gap + bar_w + label_gap;
        int glyph_h = 7 * font_s;
        //27092026 log-Karten: Beschriftung mit dem Fehler selbst (10^x) statt dem Exponenten
        Func<float, float> label_val = x => is_log ? Mathf.Pow(10f, x) : x;
        draw_label(dst, outTex.width, h, lx, h - 1, format_scale_value(label_val(scale.hi), as_percent), font_s);
        draw_label(dst, outTex.width, h, lx, glyph_h - 1, format_scale_value(label_val(scale.lo), as_percent), font_s);
        if (is_log)
        {
            // ganze Zehnerpotenzen dazwischen (z.B. 0.01, 0.1, 1)
            for (int e = Mathf.CeilToInt(scale.lo); e <= Mathf.FloorToInt(scale.hi); e++)
            {
                int ye = Mathf.RoundToInt((e - scale.lo) / (scale.hi - scale.lo) * (h - 1));
                if (ye > 2 * glyph_h && ye < h - 2 * glyph_h)
                    draw_label(dst, outTex.width, h, lx, ye + glyph_h / 2, format_scale_value(Mathf.Pow(10f, e), as_percent), font_s);
            }
        }
        else if (scale.lo < 0f && scale.hi > 0f)
        {
            int y0 = Mathf.RoundToInt((0f - scale.lo) / (scale.hi - scale.lo) * (h - 1));
            if (y0 > 2 * glyph_h && y0 < h - 2 * glyph_h)
                draw_label(dst, outTex.width, h, lx, y0 + glyph_h / 2, "0", font_s);
        }
        outTex.SetPixels(dst);
        outTex.Apply();
        outTex.wrapMode = TextureWrapMode.Clamp;
        return outTex;
    }

    //23092026 Zahl fuer die Farbbalken-Beschriftung (loss_rel in Prozent)
    /// <summary>
    /// Formats a value of the colour bar label.
    /// </summary>
    /// <param name="v">Value.</param>
    /// <param name="as_percent">True for percent.</param>
    /// <returns>Text.</returns>
    static string format_scale_value(float v, bool as_percent)
    {
        CultureInfo ci = CultureInfo.InvariantCulture;
        if (as_percent)
        {
            float p = 100f * v;
            if (Mathf.Abs(p) >= 100f) return p.ToString("0", ci) + "%";
            if (Mathf.Abs(p) >= 1f || p == 0f) return p.ToString("0.#", ci) + "%";
            return p.ToString("F" + small_value_decimals(p), ci) + "%"; //27092026 z.B. 0.1% / 0.01%
        }
        float a = Mathf.Abs(v);
        if (a >= 100f) return v.ToString("0", ci);
        if (a >= 10f) return v.ToString("0.#", ci);
        if (a >= 0.1f || a == 0f) return v.ToString("0.##", ci);
        return v.ToString("F" + small_value_decimals(v), ci); //27092026 z.B. 0.01 / 0.001 (log-Karten)
    }

    //27092026 Nachkommastellen, damit kleine Werte zwei gueltige Ziffern behalten (max. 6)
    /// <summary>
    /// Number of decimals so that small values keep two significant digits (max. 6).
    /// </summary>
    /// <param name="v">Value.</param>
    /// <returns>Decimals.</returns>
    static int small_value_decimals(float v)
    {
        float a = Mathf.Abs(v);
        return Mathf.Clamp(Mathf.CeilToInt(-Mathf.Log10(a)) + 1, 1, 6);
    }

    //23092026 Minimale 5x7-Pixelschrift (nur Ziffern . - %), damit Beschriftungen direkt in die
    //  PNG geschrieben werden koennen. Zeilen von oben nach unten.
    static readonly Dictionary<char, string[]> label_font = new Dictionary<char, string[]>
    {
        { '0', new[] { " ### ", "#   #", "#  ##", "# # #", "##  #", "#   #", " ### " } },
        { '1', new[] { "  #  ", " ##  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### " } },
        { '2', new[] { " ### ", "#   #", "    #", "   # ", "  #  ", " #   ", "#####" } },
        { '3', new[] { "#####", "   # ", "  #  ", "   # ", "    #", "#   #", " ### " } },
        { '4', new[] { "   # ", "  ## ", " # # ", "#  # ", "#####", "   # ", "   # " } },
        { '5', new[] { "#####", "#    ", "#### ", "    #", "    #", "#   #", " ### " } },
        { '6', new[] { "  ## ", " #   ", "#    ", "#### ", "#   #", "#   #", " ### " } },
        { '7', new[] { "#####", "    #", "   # ", "  #  ", " #   ", " #   ", " #   " } },
        { '8', new[] { " ### ", "#   #", "#   #", " ### ", "#   #", "#   #", " ### " } },
        { '9', new[] { " ### ", "#   #", "#   #", " ####", "    #", "   # ", " ##  " } },
        { '.', new[] { "     ", "     ", "     ", "     ", "     ", " ##  ", " ##  " } },
        { '-', new[] { "     ", "     ", "     ", "#####", "     ", "     ", "     " } },
        { '%', new[] { "##   ", "##  #", "   # ", "  #  ", " #   ", "#  ##", "   ##" } },
    };

    // Schreibt text weiss in dst (Breite tex_w, Hoehe tex_h, y = 0 unten); y_top = oberste Pixelzeile
    /// <summary>
    /// Writes text in white into a pixel array (bitmap font).
    /// </summary>
    /// <param name="dst">Pixel array.</param>
    /// <param name="tex_w">Width.</param>
    /// <param name="tex_h">Height.</param>
    /// <param name="x0">Start column.</param>
    /// <param name="y_top">Top row.</param>
    /// <param name="text">Text.</param>
    /// <param name="s">Pixel scale.</param>
    static void draw_label(Color[] dst, int tex_w, int tex_h, int x0, int y_top, string text, int s)
    {
        int x = x0;
        foreach (char ch in text)
        {
            if (label_font.TryGetValue(ch, out string[] rows))
            {
                for (int r = 0; r < 7; r++)
                    for (int c = 0; c < 5; c++)
                    {
                        if (rows[r][c] != '#')
                            continue;
                        for (int dy = 0; dy < s; dy++)
                            for (int dx = 0; dx < s; dx++)
                            {
                                int px = x + c * s + dx;
                                int py = y_top - r * s - dy;
                                if (px >= 0 && px < tex_w && py >= 0 && py < tex_h)
                                    dst[py * tex_w + px] = Color.white;
                            }
                    }
            }
            x += 6 * s;
        }
    }

    //21092026 Farbskala fuer colorize_encoded_map: lo/hi = angezeigter Wertebereich.
    private struct ColorScale
    {
        public float lo, hi;
        private bool diverging;   // blau (negativ) - schwarz (0) - rot (positiv)
        private bool negative;    // schwarz -> blau (alle Werte <= 0)

        //27092026 sequential: schwarz -> rot linear ueber [v_min, v_max] (log-Fehlerkarten, Werte meist < 0)
        /// <summary>
        /// Colour scale for maps (loss, diverging, negative, or sequential).
        /// </summary>
        /// <param name="is_loss">True for error maps.</param>
        /// <param name="plus_minus">True for signed maps.</param>
        /// <param name="v_min">Minimum.</param>
        /// <param name="v_max">Maximum.</param>
        /// <param name="lim">Symmetric limit.</param>
        /// <param name="sequential">True for black to red over [v_min, v_max].</param>
        public ColorScale(bool is_loss, bool plus_minus, float v_min, float v_max, float lim, bool sequential = false)
        {
            diverging = !is_loss && plus_minus && !sequential;
            negative = !is_loss && !plus_minus && v_max <= 0f && !sequential;
            if (sequential) { lo = v_min; hi = v_max; }
            else if (is_loss) { lo = 0f; hi = Mathf.Max(v_max, 1e-9f); }
            else if (diverging) { lo = -lim; hi = lim; }
            else { lo = v_min; hi = v_max; }
            if (hi - lo < 1e-9f) hi = lo + 1e-9f;
        }

        /// <summary>
        /// Colour of a value.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <returns>Colour (black for NaN).</returns>
        public Color map(float value)
        {
            if (float.IsNaN(value))
                return Color.black;
            if (diverging)
            {
                float s = Mathf.Clamp(value / hi, -1f, 1f);
                return s >= 0f ? new Color(s, 0f, 0f, 1f) : new Color(0f, 0f, -s, 1f);
            }
            float t = Mathf.Clamp01((value - lo) / (hi - lo));
            if (negative)
                return new Color(0f, 0f, 1f - t, 1f);   // lo (negativster Wert) = blau, hi (~0) = schwarz
            return new Color(t, 0f, 0f, 1f);
        }
    }
    /// <summary>
    /// Output path of an image (DIC package folder or labelled file).
    /// </summary>
    /// <param name="label">Label.</param>
    /// <param name="tex">Image.</param>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <returns>Path.</returns>
    public string manage_png_path(string label, Texture2D tex, int cam_idx, int blade_idx)
    {
        // info (paul): save in the DIC-package directory
        if (label == "")
        {
            write_in_dic(tex, cam_idx, blade_idx);
        }

        // info (paul): save in the unity directory
        //25062024 string path_l = construct_blade_path(cam_idx, blade_idx, label);

        if (with_our_idxs && blade_idx < this.our_blade_idxs.Count)
        {
            blade_idx = this.our_blade_idxs[blade_idx];
        }
        string path_l = construct_blade_path(cam_idx, blade_idx, label, with_uv_mode: true);
        return path_l;
    }
    /// <summary>
    /// Saves a rendered image as PNG and writes the parameter file.
    /// </summary>
    /// <param name="tex">Image.</param>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="save_path">Output path (optional).</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="label">Label.</param>
    /// <param name="pars">Parameters of the experiment.</param>
    public void save_png(Texture2D tex, int cam_idx, string save_path=null, 
        int blade_idx=-1, string label = "", Params pars = null)
    {
        // info (paul): "label" is sth, that you can add, to give a special name

        //16062024 string path_l = Application.persistentDataPath + "/blade_" + blade_idx.ToString() + "_cam_" + cam_idx.ToString() + label + ".png";
        string path_l = save_path;
        if (save_path == null || save_path == "")
        {
            path_l = manage_png_path(label, tex, cam_idx, blade_idx);
        }


        string written_path = path_l.Replace("uv/", "uv/" + category);
        System.IO.File.WriteAllBytes(written_path, tex.EncodeToPNG());

        if (get_with_exp() && (string.IsNullOrEmpty(label) || analysis_sweep_running()))
            ExperimentImageGallery.AddRenderedImage(written_path);
        save_params_file(path_l, pars);
        // e.g. "saved blade under: muc/Users/paulrichter/Desktop/DIC_2025_for_travel/DIC_package/exp_normal/cam_0/uv/2__uv_value.png_r512"
        string blade_info = "saved blade under: " + category + path_l;
        if (with_print_paths)
        {
            Debug.Log(blade_info);
        }
        set_ready_for_next_blade(true);
    }

    /// <summary>
    /// Writes the parameters of the experiment next to the image.
    /// </summary>
    /// <param name="blade_path">Image path.</param>
    /// <param name="pars">Parameters (null = nothing).</param>
    public void save_params_file(string blade_path, Params pars)
    {
        //20092026 ohne Parameter (manueller save-Klick) nichts schreiben statt NullReference.
        if (pars == null)
            return;
        string dir_path = blade_path.Substring(0, blade_path.LastIndexOf("/"));
        string path = dir_path + "/info.txt";

        // info (paul): params for all the lighting
        float speckle_size = -1f;
        float lighting_intensity = -1f;

        float lighting_pos_x = -1f;
        float lighting_pos_y = -1f;
        float lighting_pos_z = -1f;

        float gaussian_error = -1f;
        float poisson_error = -1f;
        float lens_distortion = -1f;

        // info (paul): converting to string
        string params_str = null;
        try
        {
            params_str = "speckle_size: " + pars.get_speckle_size().ToString();
        }
        catch
        {
            params_str = "speckle_size: " + pars.get_speckle_size().ToString();
        }

        params_str += "\n lighting_intensity: " + pars.get_lighting_intensity().ToString();
        params_str += "\n lighting_pos_x: " + pars.get_lighting_pos_x().ToString();
        params_str += "\n lighting_pos_y: " + pars.get_lighting_pos_y().ToString();
        params_str += "\n lighting_pos_z: " + pars.get_lighting_pos_z().ToString();
        params_str += "\n gaussian_error: " + pars.get_gaussian_error().ToString();
        params_str += "\n poisson_error: " + pars.get_poisson_error().ToString();
        params_str += "\n lens_distortion: " + pars.get_lens_distortion().ToString();

        // info (paul): save as txt file
        try
        {
            File.WriteAllText(path, params_str);
        }
        catch
        {
            File.WriteAllText(path, params_str);
        }
    }

    /// <summary>
    /// Compares two parameter sets.
    /// </summary>
    /// <param name="pars1">First set.</param>
    /// <param name="pars2">Second set.</param>
    /// <returns>True if equal.</returns>
    public bool compare_params(Params pars1, Params pars2)
    {
        bool same_1 = false;
        try
        {
            same_1 = (pars1.get_speckle_size() == pars2.get_speckle_size());
        }
        catch
        {
            same_1 = (pars1.get_speckle_size() == pars2.get_speckle_size());
        }
        bool same_2 = (pars1.get_lighting_intensity() == pars2.get_lighting_intensity());
        bool same_3 = (pars1.get_lighting_pos_x() == pars2.get_lighting_pos_x());
        bool same_4 = (pars1.get_lighting_pos_y() == pars2.get_lighting_pos_y());
        bool same_5 = (pars1.get_lighting_pos_z() == pars2.get_lighting_pos_z());
        bool same_6 = (pars1.get_gaussian_error() == pars2.get_gaussian_error());
        bool same_7 = (pars1.get_poisson_error() == pars2.get_poisson_error());
        bool same_8 = (pars1.get_lens_distortion() == pars2.get_lens_distortion());

        bool all_same = (same_1 && same_2 && same_3 && same_4 &&
            same_5 && same_6 && same_7 && same_8);

        return all_same;

    }
    /// <summary>
    /// Finds the folder of a render action with matching parameters.
    /// </summary>
    /// <param name="pars_ref">Parameters.</param>
    /// <returns>Tuple (folder, parameters found).</returns>
    public (string, Params) find_dir_for_params(Params pars_ref)
    {
        Params pars_found = null;
        string proj_path_found = null;

        for (int i = 0; i < render_acts.Count; i++)
        {
            string exp_name = render_acts[i].get_label();
            string proj_path = path_dic + remove_dots(exp_name);
            string dir_path = proj_path + "/cam_0/uv/";
            // During a sweep, later experiment folders do not exist yet.
            // Only compare parameters from runs that have already rendered.
            if (!File.Exists(dir_path + "/info.txt"))
                continue;
            Params pars = load_params_file(file: null, dir_path: dir_path);
            bool pars_same = compare_params(pars_ref, pars);

            if (pars_same)
            {
                pars_found = pars;
                proj_path_found = proj_path;
                //break;
            }
        }

        return (proj_path_found, pars_found);
    }
    /// <summary>
    /// Reads a parameter file.
    /// </summary>
    /// <param name="file">Parameter file.</param>
    /// <param name="dir_path">Folder (optional).</param>
    /// <returns>Parameters.</returns>
    public Params load_params_file(string file, string dir_path = null)
    {
        if (dir_path == null)
        {
            dir_path = file.Substring(0, file.LastIndexOf("/"));
        }

        string path = dir_path + "/info.txt";

        StreamReader stream = new StreamReader(path);
        string text = stream.ReadToEnd();
        string[] params_strs = text.Split('\n');

        float speckle_size = from_params(text, "speckle_size");
        float lighting_intensity = from_params(text, "lighting_intensity");
        float lighting_pos_x = from_params(text, "lighting_pos_x");
        float lighting_pos_y = from_params(text, "lighting_pos_y");
        float lighting_pos_z = from_params(text, "lighting_pos_z");
        float gaussian_error = from_params(text, "gaussian_error");
        float poisson_error = from_params(text, "poisson_error");
        float lens_distortion = from_params(text, "lens_distortion");

        Params pars = new Params();
        pars.set_speckle_size(speckle_size);
        pars.set_lighting_intensity(lighting_intensity);
        pars.set_lighting_pos_x(lighting_pos_x);
        pars.set_lighting_pos_y(lighting_pos_y);
        pars.set_lighting_pos_z(lighting_pos_z);
        pars.set_gaussian_error(gaussian_error);
        pars.set_poisson_error(poisson_error);
        pars.set_lens_distortion(lens_distortion);



        return pars;
    }

    /// <summary>
    /// Reads a numeric value after a key from the text of a parameter file.
    /// </summary>
    /// <param name="text">Text.</param>
    /// <param name="key">Key.</param>
    /// <returns>Value.</returns>
    public float from_params(string text, string key)
    {
        int key_idx = text.LastIndexOf(key);
        int text_cnt = text.Length;
        string cut = text.Substring(key_idx, text_cnt - key_idx);
        int idx_start = cut.IndexOf(":") + 2;
        int idx_end = cut.IndexOf("\n");
        if (idx_end == -1)
        {
            idx_end = cut.Length;
        }
        string value_str = cut.Substring(idx_start, idx_end - idx_start);
        float value = float.Parse(value_str);

        return value;
    }

    /// <summary>
    /// Legacy: left or right folder of a camera.
    /// </summary>
    /// <param name="cam_idx">Camera index.</param>
    /// <returns>Folder.</returns>
    public string choose_dir(int cam_idx)
    {
        string dir_l = path_dic + "left";
        string dir_r = path_dic + "right";
        string dir_chosen = null;
        if (cam_idx == 0)
        {
            dir_chosen = dir_l;
        }
        if (cam_idx == 1)
        {
            dir_chosen = dir_r;
        }
        return dir_chosen;
    }

    /// <summary>
    /// Saves an image in the uv folder of the experiment.
    /// </summary>
    /// <param name="tex">Image.</param>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="blade_idx">Time-step index.</param>
    public void write_in_dic(Texture2D tex, int cam_idx, int blade_idx)
    {
        //02072024 string dir_chosen = choose_dir(cam_idx);

        // info (paul): save png under path
        string dir_path = path_dic + remove_dots(this.get_experiment().ToString()) + "/cam_" + cam_idx + "/uv/";
        if (this.get_experiment() != "exp_normal")
        {
            ;
        }
        string path_png = dig_path(dir_path, blade_idx);
        System.IO.File.WriteAllBytes(path_png, tex.EncodeToPNG());

        // info (paul): convert to .tif via Python file
        //30082024 convert_to_tif();
    }

    /// <summary>
    /// Creates a folder if needed and returns the image path of a time step.
    /// </summary>
    /// <param name="dir_path">Folder.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <returns>Path.</returns>
    public string dig_path(string dir_path, int blade_idx)
    {
        //application.persistentDataPath
        bool dir_exists = Directory.Exists(dir_path);
        if (!dir_exists)
        {
            System.IO.Directory.CreateDirectory(dir_path);
            bool dir_exists_test = Directory.Exists(dir_path);
        }

        if (with_our_idxs && blade_idx < this.our_blade_idxs.Count)
        {
            blade_idx = this.our_blade_idxs[blade_idx];
        }

        string path_png = dir_path + category + "im_" + blade_idx.ToString() +
            "_r" + get_render_res().ToString() + ".png";
        return path_png;
    }

    /// <summary>
    /// Legacy: converts images to TIFF with a Python script.
    /// </summary>
    public void convert_to_tif()
    {
        Process proc = new Process();
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = "png2tiff.py";//"script_paul.py";

        Process.Start("python", "Assets/png2tiff.py").WaitForExit();
    }

    /// <summary>
    /// Placeholder for a Hausdorff distance (empty).
    /// </summary>
    public void hausdorff()
    {
        // info (paul): find hausdorff distance

        for (int i = 0; i < -1; i++)
        {
            // TODO: ...
        }

    }

    /// <summary>
    /// Older version of the image rendering (unused).
    /// </summary>
    public void take_pic_old()
    {
        int our_height = 256;

        // info (paul): crate render_tex
        //10062024 RenderTexture render_tex = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        //10062024 render_tex.Create();
        //10062024 cam_for_uv.Render();
        //10062024 
        //10062024 
        //10062024 cam_for_uv.targetTexture = render_tex;
        //10062024 
        //10062024 
        RenderTexture render_tex = cam_for_uv_0.targetTexture;//12062024 cam_for_uv.targetTexture;

        // info (paul): create texture2D
        Texture2D tex = new Texture2D(our_height, our_height);
        //RenderTexture.active = render_tex;
        tex.ReadPixels(new Rect(0, 0, render_tex.width, render_tex.height), 0, 0);
        tex.Apply();


        // info (paul): render to tex
        //cam_for_uv.Render();


        // info (paul): save as png image

        string path_l = UnityEngine.Application.persistentDataPath + "/ABC.png";
        File.WriteAllBytes(path_l, tex.EncodeToPNG());

        // info (paul): release render_tex
        render_tex.Release();
    }
    /// <summary>
    /// Collects the sample objects (children of 'blades').
    /// </summary>
    /// <returns>Samples.</returns>
    public List<GameObject> collect_blades()
    {
        GameObject blades = GameObject.Find("blades");
        int child_cnt = blades.transform.childCount;
        List<GameObject> children = new List<GameObject>();

        for (int i = 0; i < child_cnt; i++)
        {
            Transform child = blades.transform.GetChild(i);
            children.Add(child.gameObject);
        }

        return children;
    }

    /// <summary>
    /// Builds a mesh from a vertex text file (verts_*.txt).
    /// </summary>
    /// <param name="blade_path">File.</param>
    /// <returns>Mesh.</returns>
    public Mesh load_mesh_from_verts(string blade_path)
    {
        string[][] verts_strs = load_vert_strings(blade_path: blade_path);
        //21042024B int blade_idx = get_blade_idx();
        //21042024B int t_idx = blade_idxs[blade_idx];
        float[][] verts_coords = strs2floats(verts_strs);//21042024B , blade_idx: t_idx);

        // info (paul): set up the meshb
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = init_verts_from_coords(verts_coords);
        mesh.triangles = tris_from_coords(verts_coords);
        return mesh;
    }


    /// <summary>
    /// Mirrors vertices in x and/or z.
    /// </summary>
    /// <param name="verts">Vertices.</param>
    /// <param name="mirr_x">Factor for x (-1 = mirror).</param>
    /// <param name="mirr_z">Factor for z (-1 = mirror).</param>
    /// <returns>Mirrored vertices.</returns>
    public Vector3[] mirror_verts(Vector3[] verts, float mirr_x, float mirr_z)
    {
        List<Vector3> verts_new = new List<Vector3>();

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 vert = verts[i];
            Vector3 vert_1 = new Vector3(mirr_x * vert.x, mirr_z * vert.y, vert.z);
            verts_new.Add(vert_1);
        }

        return verts_new.ToArray();
    }

    /// <summary>
    /// Shifts triangle indices (optionally flips normals).
    /// </summary>
    /// <param name="tris">Indices.</param>
    /// <param name="offset">Offset.</param>
    /// <param name="flip_normals">True to flip the orientation.</param>
    /// <returns>Shifted indices.</returns>
    public int[] shift_tris(int[] tris, int offset = -1, bool flip_normals = false)
    {
        List<int> shifted = new List<int>();

        for (int i = 0; i < tris.Length; i++)
        {
            shifted.Add(tris[i] + offset);
        }

        if (flip_normals)
        {
            for (int i = 0; i < tris.Length; i++)
            {
                if (i % 3 == 0)
                {
                    (shifted[i + 1], shifted[i + 2]) = (shifted[i + 2], shifted[i + 1]);
                }
            }
        }

        return shifted.ToArray();
    }

    /// <summary>
    /// Determines the bounds of the vertices.
    /// </summary>
    /// <param name="verts">Vertices.</param>
    public void find_verts_min_max(Vector3[] verts)
    {
        List<float> xs = new List<float>();
        List<float> ys = new List<float>();
        List<float> zs = new List<float>();

        for (int i = 0; i < verts.Length; i++)
        {
            xs.Add(verts[i].x);
            ys.Add(verts[i].y);
            zs.Add(verts[i].z);
        }

        float min_x = xs.Min();
        float max_x = xs.Max();

        float min_y = ys.Min();
        float max_y = ys.Max();

        float min_z = zs.Min();
        float max_z = zs.Max();

        _ = 1 + 1;
        return;
    }

    /// <summary>
    /// Completes a quarter model to the full sample by mirroring.
    /// </summary>
    /// <param name="mesh">Quarter mesh.</param>
    /// <returns>Full mesh.</returns>
    public Mesh mirror_mesh(Mesh mesh)
    {
        find_verts_min_max(mesh.vertices);

        Vector3[] verts_1 = mirror_verts(mesh.vertices, mirr_x: -1f, mirr_z: 1f);
        Vector3[] verts_2 = mirror_verts(mesh.vertices, mirr_x: 1f, mirr_z: -1f);
        Vector3[] verts_3 = mirror_verts(mesh.vertices, mirr_x: -1f, mirr_z: -1f);
        Vector3[] verts_all = mesh.vertices.Concat(verts_1).Concat(verts_2).Concat(verts_3).ToArray();

        int[] tris_1 = shift_tris(mesh.triangles, offset: verts_1.Length, flip_normals: true);
        int[] tris_2 = shift_tris(mesh.triangles, offset: 2 * verts_1.Length, flip_normals: true);
        int[] tris_3 = shift_tris(mesh.triangles, offset: 3 * verts_1.Length);
        int[] tris_all = mesh.triangles.Concat(tris_1).Concat(tris_2).Concat(tris_3).ToArray();

        // info (paul): vertices
        mesh.vertices = verts_all;
        mesh.triangles = tris_all;

        return mesh;
    }

    /// <summary>
    /// Loads a sample object from a vertex file (mesh, UVs, collider, speckles).
    /// </summary>
    /// <param name="blade_path">File.</param>
    /// <param name="blade_idx">Time-step index.</param>
    /// <param name="with_uv_init">True to compute UVs.</param>
    /// <param name="with_collider">True to add a mesh collider.</param>
    /// <param name="with_speckles">True to apply the speckle pattern.</param>
    /// <returns>Sample object.</returns>
    public GameObject load_blade_from_verts(string blade_path, int blade_idx = -1, bool with_uv_init = false, 
        bool with_collider = true, bool with_speckles = false)
    {
        // info (paul): get blade_name
        int last_slash = blade_path.LastIndexOf("/") + 1;
        string blade_name = blade_path.Substring(last_slash);

        // info (paul): 
        Mesh mesh = load_mesh_from_verts(blade_path);
        if (category == "muc")
        {
            mesh = mirror_mesh(mesh);
        }

        // info (paul): create the object
        //21102024B string obj_name = "verts_mesh_" + blade_idx.ToString();
        string obj_name = "verts_mesh_" + blade_idx.ToString();
        GameObject surface_obj = setup_surface_obj(mesh, obj_name: obj_name);
        //20092026 Bugfix: find_triangle() raycastet nur gegen Layer 7 ("Experiment"), der
        //bisher nur im Confirm-Button-Pfad gesetzt wurde. Im klassischen with_exp-Pfad
        //blieben die Proben auf Layer 0 -> Pixel-zu-Dreieck-Maske (tris/barys) komplett -1
        //-> value/loss-Karten komplett schwarz. Die Kameras rendern Default+Experiment,
        //am Rendering aendert sich nichts.
        surface_obj.layer = 7;
        if (category == "muc")
        {
            surface_obj.transform.localScale = new Vector3(3f, 3f, 3f);
        }
        else
        {
            //18092026 Probe wurde mit localScale 1 (Default) viel zu klein dargestellt,
            //da diese Skalierung zuvor nur fuer category=="muc" galt. Faktor 5 auf
            //Nutzerwunsch ergaenzt, damit die geladene Probe wieder sichtbar/plausibel gross ist.
            surface_obj.transform.localScale = new Vector3(5f, 5f, 5f);
        }
        if (nakajima_look && category != "muc")
        {
            //21092026 Render-Look "Nakajima": Probe wie die FBX-Referenz bei "Realbild: Neu"
            //platzieren (ersetzt die klassische Skalierung/Rotation im else-Zweig).
            align_blade_to_nakajima_reference(surface_obj);
        }
        else
        {
        surface_obj.transform.RotateAround(new Vector3(),
             new Vector3(1f, 0f, 0f), angle: -90f);

        //18092026 Nutzerwunsch: passend zur Kamera-Verdrehung um die globale X- statt
        //Z-Achse (siehe set_up_cam) wird die Probe jetzt immer (nicht mehr nur fuer
        //blade_name.StartsWith("muc")) zusaetzlich um 90 Grad um die globale Y-Achse
        //gedreht platziert, damit die Ausrichtung der Probe wieder zur neuen
        //Stereo-Basisrichtung der Kameras passt.
        surface_obj.transform.RotateAround(new Vector3(),
             new Vector3(0f, 1f, 0f), angle: 90f);

        surface_obj.transform.position = blades_pos;
        }

        // info (paul): set parent
        if (blade_idx != -1)
        {
            GameObject blades = GameObject.Find("blades");
            surface_obj.transform.SetParent(blades.transform);
        }

        // info (paul): set uvs
        if (with_uv_init)
        {
            surface_obj.AddComponent<Vis_action>();
            surface_obj.GetComponent<Vis_action>().check_vis = false;
        }
        else
        {
            try
            {
                surface_obj.GetComponent<MeshFilter>().mesh.uv = uv_start;
            }
            catch
            {
                surface_obj.GetComponent<MeshFilter>().mesh.uv = uv_start;
            }
        }

        // info (paul): set collider
        Mesh mesh_l = surface_obj.GetComponent<MeshFilter>().sharedMesh;
        if (surface_obj.GetComponent<MeshCollider>() == null)
        {
            surface_obj.AddComponent<MeshCollider>();
        }
        surface_obj.GetComponent<MeshCollider>().sharedMesh = mesh_l;

        // info (paul): add speckle tex if included:
        if (with_collider)
        {
            // // info (paul): get the projected uv coordinates
            // UnityEngine.Vector3[] uvs = obj2uvs(gameObject);
            // 
            // // info (paul): assign the uv coordinates to this obj:
            // uvs2obj(gameObject, uvs);
            // apply_speckles(gameObject);
            // create_other_blades();
        }

        return surface_obj;
    }

    //20092026 planare UV-Projektion, unabhaengig davon, in welcher lokalen Ebene das
    //Mesh liegt: die Achse mit der kleinsten Ausdehnung (Blechdicke/Woelbung) wird
    //verworfen, die beiden anderen auf [0, uv_scale] normiert.
    /// <summary>
    /// Planar UV projection on the two largest axes of the mesh (the thinnest axis is dropped).
    /// </summary>
    /// <param name="vertices">Vertices.</param>
    /// <returns>UVs.</returns>
    public Vector2[] planar_uvs_from_largest_axes(Vector3[] vertices)
    {
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        foreach (Vector3 vertex in vertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }
        Vector3 range = max - min;

        // info (paul): Achse mit kleinster Ausdehnung = Flaechennormale -> verwerfen
        int drop = 0;
        if (range.y < range[drop]) drop = 1;
        if (range.z < range[drop]) drop = 2;
        int axis_u = drop == 0 ? 1 : 0;
        int axis_v = drop == 2 ? 1 : 2;
        float range_u = Mathf.Max(1e-6f, range[axis_u]);
        float range_v = Mathf.Max(1e-6f, range[axis_v]);
        Debug.Log("planar_uvs_from_largest_axes: extents " + range.ToString("F3")
            + " -> u = axis " + axis_u + ", v = axis " + axis_v + " (dropped axis " + drop + ")");

        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            uvs[i] = new Vector2(uv_scale * (vertices[i][axis_u] - min[axis_u]) / range_u,
                uv_scale * (vertices[i][axis_v] - min[axis_v]) / range_v);
        return uvs;
    }

    //18092026 Nutzerwunsch: setzt Skalierung und Ausrichtung der Nakajima-FBX-Probe.
    //Wird sowohl bei der Neuerstellung als auch bei jeder Wiederverwendung eines
    //bereits vorhandenen Objekts aufgerufen, damit Aenderungen an
    //nakajima_fbx_blade_scale (oder den Rotationswinkeln) sofort beim naechsten Klick
    //auf "Realbild: Neu" wirken, ohne dass die Play-Session neu gestartet werden muss.
    //Setzt die Rotation vor dem erneuten Anwenden auf Quaternion.identity zurueck,
    //damit wiederholte Aufrufe sich nicht aufaddieren.
    /// <summary>
    /// Sets scale and rotation of the FBX sample (resets the rotation first).
    /// </summary>
    /// <param name="blade_obj">Sample object.</param>
    public void apply_nakajima_fbx_blade_transform(GameObject blade_obj)
    {
        blade_obj.transform.rotation = UnityEngine.Quaternion.identity;
        blade_obj.transform.localScale = new Vector3(nakajima_fbx_blade_scale,
            nakajima_fbx_blade_scale, nakajima_fbx_blade_scale);

        // info (paul): gleiche Achsenkorrektur wie bei den verts-basierten Proben
        // (siehe load_blade_from_verts), damit die Probe zur aktuellen Kamera-
        // Ausrichtung (globale X-Achsen-Stereobasis) passt.
        blade_obj.transform.RotateAround(new Vector3(), new Vector3(1f, 0f, 0f), angle: -90f);
        blade_obj.transform.RotateAround(new Vector3(), new Vector3(0f, 1f, 0f), angle: 90f);
        blade_obj.transform.position = blades_pos;

        //18092026 Nutzerwunsch: Textur/UV waren bereits korrekt (nur von der falschen
        //Seite betrachtet) - zusaetzliche 90-Grad-Drehung um die eigene X-Achse, in
        //Position (blades_pos) belassen, um die Probe richtig zur Kamera auszurichten.
        blade_obj.transform.RotateAround(blade_obj.transform.position, new Vector3(1f, 0f, 0f), angle: 90f);

        //20092026 Nutzerwunsch: zusaetzliche Drehung um die lokale (eigene) Z-Achse der
        //Probe - im Gegensatz zu den RotateAround-Aufrufen oben (globale Achsen) dreht
        //Space.Self um die aktuelle, bereits gedrehte Objekt-Achse.
        if (!float.IsNaN(nakajima_fbx_blade_local_z_rotation))
        {
            blade_obj.transform.Rotate(0f, 0f, nakajima_fbx_blade_local_z_rotation, Space.Self);
        }
        //21092026 In-Plane-Drehung um die Flaechennormale (lokale y-Achse des FBX-Meshes).
        if (!float.IsNaN(nakajima_fbx_blade_in_plane_rotation))
        {
            blade_obj.transform.Rotate(0f, nakajima_fbx_blade_in_plane_rotation, 0f, Space.Self);
        }
    }

    //18092026 Nutzerwunsch: laedt die neue, korrekt schmale Nakajima-Probe aus einer
    //echten FBX-Datei (statt aus verts_*.txt rekonstruiert) fuer den Realbild-Vergleich.
    //Folgt denselben Achsen-/Positions-Konventionen wie load_blade_from_verts, damit die
    //Probe zur aktuellen Kamera-/Stereobasis-Ausrichtung passt.
    /// <summary>
    /// Loads the narrow Nakajima sample from an FBX file for the real-image comparison.
    /// </summary>
    /// <param name="resource_path">Resources path.</param>
    /// <param name="obj_name">Object name.</param>
    /// <returns>Sample object.</returns>
    public GameObject load_nakajima_fbx_blade(
        string resource_path = "Targets/fbx_files/nakajima/nakajima_50_fbx",
        string obj_name = "nakajima_50_fbx_blade")
    {
        GameObject fbx_prefab = (GameObject)Resources.Load(resource_path);
        if (fbx_prefab == null)
            throw new FileNotFoundException(
                "Nakajima-FBX-Probe wurde nicht gefunden unter Resources/" + resource_path);

        GameObject fbx_instance = Instantiate(fbx_prefab);

        // info (paul): FBX-Importe liegen haeufig als Hierarchie vor (leeres Root-
        // Objekt mit dem eigentlichen Mesh in einem Kindobjekt). Die restliche
        // Pipeline (Speckle-Material, Kamera-Projektion, Sichtbarkeit) spricht das
        // Objekt mit MeshFilter/Renderer direkt an, deshalb wird genau dieses
        // Objekt herausgeloest und als eigenstaendiges Objekt weiterverwendet.
        GameObject blade_obj = fbx_instance;
        MeshFilter mesh_filter = fbx_instance.GetComponent<MeshFilter>();
        if (mesh_filter == null)
        {
            mesh_filter = fbx_instance.GetComponentInChildren<MeshFilter>();
            if (mesh_filter == null)
            {
                Destroy(fbx_instance);
                throw new InvalidOperationException(
                    "Die Nakajima-FBX-Probe (" + resource_path + ") enthaelt kein Mesh.");
            }
            blade_obj = mesh_filter.gameObject;
            blade_obj.transform.SetParent(null, true);
            Destroy(fbx_instance);
        }
        if (blade_obj.GetComponent<Renderer>() == null)
        {
            Destroy(blade_obj);
            throw new InvalidOperationException(
                "Die Nakajima-FBX-Probe (" + resource_path + ") besitzt keinen Renderer.");
        }

        blade_obj.name = obj_name;

        //18092026 Nutzerwunsch: Skalierung/Rotation muessen JEDES Mal frisch gesetzt
        //werden, nicht nur bei der Neuerstellung - sonst behaelt ein bereits in der
        //laufenden Play-Session vorhandenes Objekt (von collect_blades() gefunden und
        //wiederverwendet) dauerhaft seine alten Werte, selbst nachdem der Skalierungs-
        //faktor im Code geaendert wurde. Siehe apply_nakajima_fbx_blade_transform().
        apply_nakajima_fbx_blade_transform(blade_obj);

        if (blade_obj.GetComponent<MeshCollider>() == null)
        {
            blade_obj.AddComponent<MeshCollider>();
        }
        blade_obj.GetComponent<MeshCollider>().sharedMesh = blade_obj.GetComponent<MeshFilter>().sharedMesh;

        return blade_obj;
    }

    /// <summary>
    /// Triangle indices for consecutive vertices.
    /// </summary>
    /// <param name="verts">Vertex coordinates.</param>
    /// <returns>Indices.</returns>
    public int[] tris_from_coords(float[][] verts)
    {
        int scale_fac = 1;//08102024 3;
        int[] tris = new int[scale_fac * verts.Length];

        for (int i = 0; i < verts.Length; i++)
        {
            // info (paul): As you see, it is the super simple version
            tris[i] = i;
        }

        return tris;
    }

    /// <summary>
    /// Vertices from coordinate arrays.
    /// </summary>
    /// <param name="verts_coords">Coordinates.</param>
    /// <returns>Vertices.</returns>
    public Vector3[] init_verts_from_coords(float[][] verts_coords)
    {
        Vector3[] vecs = new Vector3[verts_coords.Length];

        for (int i = 0; i < verts_coords.Length; i++)
        {
            float[] coords = verts_coords[i];
            //20062024 Vector3 vec = new Vector3(coords[0], coords[1], coords[2]);
            Vector3 vec = new Vector3(coords[0], coords[1], coords[2]);
            vecs[i] = vec;
        }

        return vecs;
    }

    /// <summary>
    /// Converts text fields to floats.
    /// </summary>
    /// <param name="verts_strs">Fields.</param>
    /// <returns>Coordinates.</returns>
    public float[][] strs2floats(string[][] verts_strs)//212024B, int blade_idx = -1)
    {
        float[][] verts_cos = new float[verts_strs.Length][];

        for (int i = 0; i < verts_strs.Length; i++)
        {
            verts_cos[i] = new float[verts_strs[i].Length];

            for (int j = 0; j < verts_strs[i].Length; j++)
            {
                string str_l = verts_strs[i][j];
                float float_l = float.Parse(str_l, CultureInfo.InvariantCulture);

                verts_cos[i][j] = float_l;
            }

            //212024B H??? verts_cos[i][0] += (float)0f * (blade_idx - blade_idxs[0]);//21102024B this.blade_idx_min);
        }

        ;

        return verts_cos;
    }

    /// <summary>
    /// Reads the lines of a vertex file split into fields.
    /// </summary>
    /// <param name="blade_path">File.</param>
    /// <returns>Fields per line.</returns>
    public string[][] load_vert_strings(string blade_path)
    {
        //string file_path = "C:/Users/go73jem/Desktop/play_blender_pycahrm/write_mesh/verts_92.txt";
        // /Users/paulrichter/Desktop/DIC_2025_for_travel/write_mesh
        StreamReader inp_stm = new StreamReader(blade_path);
        List<string[]> lines = new List<string[]>();

        while (!inp_stm.EndOfStream)
        {
            string inp_ln = inp_stm.ReadLine();
            string[] splits = inp_ln.Split(" ");
            lines.Add(splits);

            // Do Something with the input. 
        }

        inp_stm.Close();
        return lines.ToArray();
    }

    /// <summary>
    /// Euclidean length sqrt(x^2 + y^2).
    /// </summary>
    /// <param name="x">x.</param>
    /// <param name="y">y.</param>
    /// <returns>Length.</returns>
    public double hypot(double x, double y)
    {
        double result = Math.Sqrt(x * x + y * y);
        return result;
    }

    // info (paul): OpenCV TV part
    /**
    *
    * Function to compute the optical flow in one scale
    *
    **/

    int MAX_ITERATIONS = 300;//1800;//900;//27092024 300;
    double PRESMOOTHING_SIGMA = 0.8d;
    double GRAD_IS_ZERO = 1E-10d;//1E-10d;








    /*14032025 
    /// <summary>
    /// CPU TV-L1 optical flow at one scale (older 1D version, Zach et al. / Sanchez et al.).
    /// </summary>
    /// <param name="I0">Source image.</param>
    /// <param name="I1">Target image.</param>
    /// <param name="u1">x component of the flow.</param>
    /// <param name="u2">y component of the flow.</param>
    /// <param name="nx">Width.</param>
    /// <param name="ny">Height.</param>
    /// <param name="tau">Time step.</param>
    /// <param name="lambda">Data weight.</param>
    /// <param name="theta">Coupling parameter.</param>
    /// <param name="warps">Number of warps.</param>
    /// <param name="epsilon">Stopping threshold.</param>
    /// <param name="mode">Mode.</param>
    void Dual_TVL1_optic_flow(
            List<float> I0,           // source image
            List<float> I1,           // target image
            List<float> u1,           // x component of the optical flow
            List<float> u2,           // y component of the optical flow
            int nx,      // image width
            int ny,      // image height
            float tau,     // time step
            float lambda,  // weight parameter for the data term
            float theta,   // weight parameter for (u - v)?
            int warps,   // number of warpings per scale
            float epsilon, // tolerance for numerical convergence
            bool verbose  // enable/disable the verbose mode
        )
    {
        if (break_now)
        {
            return;
        }

        int size = nx * ny;
        float l_t = lambda * theta;

        List<float> I1x = zeros_of_size(size);
        List<float> I1y = zeros_of_size(size);
        List<float> I1w = zeros_of_size(size);
        List<float> I1wx = zeros_of_size(size);
        List<float> I1wy = zeros_of_size(size);

        List<float> rho_b = zeros_of_size(size);
        List<float> rho_c = zeros_of_size(size);
        List<float> rho_d = zeros_of_size(size);
        List<float> rho_e = zeros_of_size(size);
        List<float> rho_f = zeros_of_size(size);
        List<float> rho_g = zeros_of_size(size);

        List<float> v1 = zeros_of_size(size);
        List<float> v2 = zeros_of_size(size);
        List<float> p11 = zeros_of_size(size);
        List<float> p12 = zeros_of_size(size);
        List<float> p21 = zeros_of_size(size);
        List<float> p22 = zeros_of_size(size);
        List<float> div = zeros_of_size(size);
        List<float> grad = zeros_of_size(size);
        List<float> div_p1 = zeros_of_size(size);
        List<float> div_p2 = zeros_of_size(size);
        List<float> u1x = zeros_of_size(size);
        List<float> u1y = zeros_of_size(size);
        List<float> u2x = zeros_of_size(size);
        List<float> u2y = zeros_of_size(size);

        // info (paul): debugging quantities
        List<float> d2_debug = zeros_of_size(size);
        List<float> fi_debug = zeros_of_size(size);
        List<float> rho_debug = zeros_of_size(size);
        List<float> decs_debug = zeros_of_size(size);

        centered_gradient(I1, I1x, I1y, nx, ny);

        // for debugging reasons:
        //29082024 for (int i = 0; i < I1.Count; i++)
        //29082024 {
        //29082024     I1[i] = i;
        //29082024 }

        // initialization of p
        for (int i = 0; i < size; i++)
        {
            p11[i] = 0f;
            p12[i] = 0f;
            p21[i] = 0f;
            p22[i] = 0f;
        }

        for (int warpings = 0; warpings < warps; warpings++)
        {
            if (break_now)
            {
                break;
            }



            // compute the warping of the target image and its derivatives
            I1w  = bicubic_interpolation_warp_new(I1, u1, u2, I1w, nx, ny, true);
            I1wx = bicubic_interpolation_warp_new(I1x, u1, u2, I1wx, nx, ny, true);
            I1wy = bicubic_interpolation_warp_new(I1y, u1, u2, I1wy, nx, ny, true);
            (grad, rho_c) = compute_grad_rho_c_new(I0, I1w, I1wx, I1wy, u1,
                u2, size, grad, rho_b, rho_c, rho_d, rho_e, rho_f, rho_g);

            //write_for_debug(u2, with_norm: false);


            //A for (int i = 0; i < size; i++)
            //A {
            //A     float Ix2 = I1wx[i] * I1wx[i];
            //A     float Iy2 = I1wy[i] * I1wy[i];
            //A 
            //A     // store the |Grad(I1)|^2
            //A     grad[i] = (Ix2 + Iy2);
            //A 
            //A     // compute the constant part of the rho function
            //A     rho_c[i] = (I1w[i] - I1wx[i] * u1[i]
            //A         - I1wy[i] * u2[i] - I0[i]);
            //A     rho_g[i] = (I1w[i] - I0[i]);
            //A     rho_b[i] = (I1w[i] - I1wx[i] * u1[i]);
            //A     rho_d[i] = (I1w[i]);
            //A     rho_e[i] = (I1wx[i] * u1[i]);
            //A     rho_f[i] = (I1wx[i]);
            //A 
            //A     byte[] bytes_l = BitConverter.GetBytes(rho_c[i]);
            //A }

            int n = 0;
            float error = Mathf.Infinity;
            float eps_sq = epsilon * epsilon;
            while (error > eps_sq && n < MAX_ITERATIONS)
            {
                n++;

                if (n == MAX_ITERATIONS - 2)
                {
                    ;
                }

                (error, p11, p12, p21, p22, u1, u2, v1, v2) = inner_optim_step_new(rho_c,
                    size, u1, u2, I1wx, I1wy, rho_debug, grad, l_t, warpings, fi_debug,
                    decs_debug, v1, v2, d2_debug, div_p1, div_p2, nx, ny, theta, p11, p12,
                    p21, p22, error, u1x, u1y, u2x, u2y, tau);
            }

            if (verbose)
            {
                //27072024 string log_str = stderr + "Warping: " + warpings.ToString() + "Iterations: %d, " + n.ToString() + "Error: %f\n" + error;
                string log_str = "stderr";
            }
        }
    }*/
    /*31032025 - seems old
        public (float, List<float>, List<float>, List<float>,
        List<float>, List<float>, List<float>, List<float>, List<float>
        ) inner_optim_step(
        List<float> rho_c, int size, List<float> u1, List<float> u2, List<float> I1wx,
        List<float> I1wy, List<float> rho_debug, List<float> grad, float l_t,
        int warpings, List<float> fi_debug, List<float> decs_debug, List<float> v1,
        List<float> v2, List<float> d2_debug, List<float> div_p1, List<float> div_p2,
        int nx, int ny, float theta, List<float> p11, List<float> p12, List<float> p21, List<float> p22,
        float error, List<float> u1x, List<float> u1y, List<float> u2x, List<float> u2y, float tau)
    {
        for (int i = 0; i < size; i++)
        {
            float rho = rho_c[i]
                + (I1wx[i] * u1[i] + I1wy[i] * u2[i]);
            rho_debug[i] = rho;

            float d1, d2;
            float grad_i = grad[i];
            if (rho < -l_t * grad_i)
            {
                d1 = l_t * I1wx[i];
                d2 = l_t * I1wy[i];
                decs_debug[i] = 0f;
            }
            else
            {
                if (rho > l_t * grad_i)
                {
                    d1 = -l_t * I1wx[i];
                    d2 = -l_t * I1wy[i];
                    decs_debug[i] = 0.5f;
                }
                else
                {
                    if (grad_i < GRAD_IS_ZERO) //29092026 Bugfix: in jedem Warp (wie IPOL), sonst NaN bei g = 0
                    {
                        d1 = d2 = 0;
                        decs_debug[i] = 0.75f;
                    }
                    else
                    {
                        float fi = -rho / grad_i;
                        fi_debug[i] = fi;
                        d1 = fi * I1wx[i];
                        d2 = fi * I1wy[i];
                        decs_debug[i] = 1f;
                    }
                }
            }

            d2_debug[i] = d2;

            v1[i] = u1[i] + d1;
            v2[i] = u2[i] + d2;
        }
        if (warpings == 1)
        {
            int a = 1 + 1;
        }
        // compute the divergence of the dual variable (p1, p2)
        divergence(p11, p12, div_p1, nx, ny);
        divergence(p21, p22, div_p2, nx, ny);

        // estimate the values of the optical flow (u1, u2)
        error = (float)0.0;
        for (int i = 0; i < size; i++)
        {
            float u1k = u1[i];
            float u2k = u2[i];

            float prod_u1_l = theta * div_p1[i];
            float sum_u1_l = v1[i] + prod_u1_l;
            u1[i] = sum_u1_l;

            float prod_u2_l = theta * div_p2[i];
            float sum_u2_l = v2[i] + prod_u2_l;
            u2[i] = sum_u2_l;

            //05082024 u1[i] = v1[i] + theta * div_p1[i];
            //05082024 u2[i] = v2[i] + theta * div_p2[i];

            error += (u1[i] - u1k) * (u1[i] - u1k) +
                (u2[i] - u2k) * (u2[i] - u2k);
        }
        error /= size;

        // compute the gradient of the optical flow (Du1, Du2)
        forward_gradient(u1, u1x, u1y, nx, ny);
        forward_gradient(u2, u2x, u2y, nx, ny);

        // estimate the values of the dual variable (p1, p2)
        for (int i = 0; i < size; i++)
        {
            float taut = tau / theta;
            float g1 = (float)hypot(u1x[i], u1y[i]);
            float g2 = (float)hypot(u2x[i], u2y[i]);
            float ng1 = 1f + taut * g1;
            float ng2 = 1f + taut * g2;

            p11[i] = (p11[i] + taut * u1x[i]) / ng1;
            p12[i] = (p12[i] + taut * u1y[i]) / ng1;
            p21[i] = (p21[i] + taut * u2x[i]) / ng2;
            p22[i] = (p22[i] + taut * u2y[i]) / ng2;
        }

        return (error, p11, p12, p21, p22, u1, u2, v1, v2);
    }
    */

    /// <summary>
    /// Warps an image with a vector field using bicubic interpolation.
    /// </summary>
    /// <param name="input">Image.</param>
    /// <param name="u">x component.</param>
    /// <param name="v">y component.</param>
    /// <param name="output">Warped image.</param>
    /// <param name="nx">Width.</param>
    /// <param name="ny">Height.</param>
    void bicubic_interpolation_warp(
        List<float> input,     // image to be warped
        List<float> u,         // x component of the vector field
        List<float> v,         // y component of the vector field
        List<float> output,    // image warped with bicubic interpolation
        int nx,        // image width
        int ny,        // image height
        bool border_out // if true, put zeros outside the region
    )
    {
        for (int i = 0; i < ny; i++)
        {
            for (int j = 0; j < nx; j++)
            {
                int p = i * nx + j;
                float uu = (float)(j + u[p]);
                float vv = (float)(i + v[p]);
                if (p == 17)
                {
                    int a_l = 1 + 1;
                }
                if (p == 129)
                {
                    ;
                }
                // obtain the bicubic interpolation at position (uu, vv)
                output[p] = bicubic_interpolation_at(input,
                        uu, vv, nx, ny, border_out);
            }
        }
    }

    /// <summary>
    /// TV-L1 optical flow at one scale (GPU if available, otherwise CPU).
    /// </summary>
    /// <param name="I0">Source images.</param>
    /// <param name="I1">Target images.</param>
    /// <param name="ux">x component of the flow.</param>
    /// <param name="uy">y component of the flow.</param>
    /// <param name="nx">Width.</param>
    /// <param name="ny">Height.</param>
    /// <param name="tau">Time step.</param>
    /// <param name="lambda">Data weight.</param>
    /// <param name="theta">Coupling parameter.</param>
    /// <param name="warps">Number of warps.</param>
    /// <param name="epsilon">Stopping threshold.</param>
    /// <param name="verbose">True for log output.</param>
    /// <param name="scale_idx">Index of the scale.</param>
    /// <param name="total_scales">Number of scales.</param>
    /// <returns>Tuple (ux, uy).</returns>
    (List<List<float>>, List<List<float>>) Dual_TVL1_optic_flow_new(
            List<List<float>> I0,           // source image
            List<List<float>> I1,           // target image
            List<List<float>> ux,           // x component of the optical flow
            List<List<float>> uy,           // y component of the optical flow
            int nx,      // image width
            int ny,      // image height
            float tau,     // time step
            float lambda,  // weight parameter for the data term
            float theta,   // weight parameter for (u - v)?
            int warps,   // number of warpings per scale
            float epsilon, // tolerance for numerical convergence
            bool verbose,  // enable/disable the verbose mode
            int scale_idx = 0,
            int total_scales = 1
        )
    {
        if (break_now)
        {
            return (null, null);
        }

        //23092026 GPU-Pfad; bei einem Fehler wird fuer den Rest des Laufs die CPU-Version unten benutzt
        if (tv_use_gpu && !tv_gpu_failed)
        {
            if (try_tvl1_gpu(I0, I1, ux, uy, nx, ny, tau, lambda, theta, warps, epsilon, scale_idx, total_scales))
            {
                return (ux, uy);
            }
        }

        int size = nx * ny;
        float l_t = lambda * theta;

        List<List<float>> I1x = zeros_of_size(I0.Count, size);
        List<List<float>> I1y = zeros_of_size(I0.Count, size);
        List<List<float>> I1w = zeros_of_size(I0.Count, size);
        List<List<float>> I1wx = zeros_of_size(I0.Count, size);
        List<List<float>> I1wy = zeros_of_size(I0.Count, size);

        List<List<float>> rho_b = zeros_of_size(I0.Count, size);
        List<List<float>> rho_c = zeros_of_size(I0.Count, size);
        List<List<float>> rho_d = zeros_of_size(I0.Count, size);
        List<List<float>> rho_e = zeros_of_size(I0.Count, size);
        List<List<float>> rho_f = zeros_of_size(I0.Count, size);
        List<List<float>> rho_g = zeros_of_size(I0.Count, size);

        List<List<float>> vx = zeros_of_size(I0.Count, size);
        List<List<float>> vy = zeros_of_size(I0.Count, size);
        List<List<float>> p11 = zeros_of_size(I0.Count, size);
        List<List<float>> p12 = zeros_of_size(I0.Count, size);
        List<List<float>> p21 = zeros_of_size(I0.Count, size);
        List<List<float>> p22 = zeros_of_size(I0.Count, size);
        List<float> div = zeros_of_size(size);
        List<List<float>> grad = zeros_of_size(I0.Count, size);
        List<List<float>> div_p1 = zeros_of_size(I0.Count, size);
        List<List<float>> div_p2 = zeros_of_size(I0.Count, size);
        List<List<float>> u1x = zeros_of_size(I0.Count, size);
        List<List<float>> u1y = zeros_of_size(I0.Count, size);
        List<List<float>> u2x = zeros_of_size(I0.Count, size);
        List<List<float>> u2y = zeros_of_size(I0.Count, size);

        // info (paul): debugging quantities
        List<float> d2_debug = zeros_of_size(size);
        List<float> fi_debug = zeros_of_size(size);
        List<float> rho_debug = zeros_of_size(size);
        List<float> decs_debug = zeros_of_size(size);

        (I1x, I1y) = centered_gradient_new(I1, I1x, I1y, nx, ny);

        // initialization of p
        for (int kt = 0; kt < I0.Count; kt++)
        {
            for (int i = 0; i < size; i++)
            {
                p11[kt][i] = 0f;
                p12[kt][i] = 0f;
                p21[kt][i] = 0f;
                p22[kt][i] = 0f;
            }
        }

        for (int warpings = 0; warpings < warps; warpings++)
        {
            if (break_now)
            {
                break;
            }

            // compute the warping of the target image and its derivatives
            I1w = bicubic_interpolation_warp_new(I1, ux, uy, I1w, nx, ny, true);
            I1wx = bicubic_interpolation_warp_new(I1x, ux, uy, I1wx, nx, ny, true);
            I1wy = bicubic_interpolation_warp_new(I1y, ux, uy, I1wy, nx, ny, true);

            (grad, rho_c) = compute_grad_rho_c_new(I0, I1w, I1wx, I1wy, ux,
                uy, size, grad, rho_b, rho_c, rho_d, rho_e, rho_f, rho_g);

            int n = 0;
            float error = Mathf.Infinity;
            float eps_sq = epsilon * epsilon;
            while (error > eps_sq && n < MAX_ITERATIONS)
            {
                n++;

                (error, p11, p12, p21, p22, ux, uy, vx, vy) = inner_optim_step_new(rho_c[0],
                    size, ux, uy, I1wx, I1wy, rho_debug, grad, l_t, warpings, fi_debug,
                    decs_debug, vx, vy, d2_debug, div_p1, div_p2, nx, ny, theta, p11, p12,
                    p21, p22, error, u1x, u1y, u2x, u2y, tau);

                update_tv_progress_step(scale_idx, total_scales, warpings, warps, n, MAX_ITERATIONS, nx, ny);
            }

            // In case the loop converged early (before MAX_ITERATIONS), account for remaining work units in this warp
            if (n < MAX_ITERATIONS)
            {
                int remaining_its = MAX_ITERATIONS - n;
                completed_work_units += (double)remaining_its * (double)nx * (double)ny;
            }

            if (verbose)
            {
                //27072024 string log_str = stderr + "Warping: " + warpings.ToString() + "Iterations: %d, " + n.ToString() + "Error: %f\n" + error;
                string log_str = "stderr";
            }
        }

        return (ux, uy);
    }

    //23092026 Fuehrt job im Unity-Hauptthread aus (aus Update) und wartet auf das Ergebnis.
    //  Wird es bereits im Hauptthread aufgerufen, laeuft job direkt.
    T run_on_main_thread<T>(Func<T> job)
    {
        if (System.Threading.Thread.CurrentThread.ManagedThreadId == main_thread_id)
        {
            return job();
        }

        T result = default(T);
        Exception job_error = null;
        // bewusst ohne Dispose: ein abgebrochener Auftrag darf spaeter noch Set() aufrufen
        System.Threading.ManualResetEventSlim done = new System.Threading.ManualResetEventSlim(false);
        main_thread_jobs.Enqueue(() =>
        {
            try { result = job(); }
            catch (Exception e) { job_error = e; }
            finally { done.Set(); }
        });
        while (!done.Wait(100))
        {
            if (break_now || tv_gpu_shutdown)
            {
                throw new OperationCanceledException("TV-GPU-Auftrag abgebrochen");
            }
        }
        if (job_error != null)
        {
            throw new Exception("TV-GPU-Auftrag fehlgeschlagen: " + job_error.Message, job_error);
        }
        return result;
    }

    /// <summary>
    /// Copies the first values of a list into an array.
    /// </summary>
    /// <param name="list">List.</param>
    /// <param name="size">Number of values.</param>
    /// <returns>Array.</returns>
    static float[] tv_to_array(List<float> list, int size)
    {
        float[] arr = new float[size];
        for (int i = 0; i < size; i++)
        {
            arr[i] = list[i];
        }
        return arr;
    }

    /// <summary>
    /// Checks whether two lists have the same first values.
    /// </summary>
    /// <param name="a">First list.</param>
    /// <param name="b">Second list.</param>
    /// <param name="size">Number of values.</param>
    /// <returns>True if equal.</returns>
    static bool tv_same_prefix(List<float> a, List<float> b, int size)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }
        for (int i = 0; i < size; i++)
        {
            float x = a[i];
            float y = b[i];
            if (x != y && !(float.IsNaN(x) && float.IsNaN(y)))
            {
                return false;
            }
        }
        return true;
    }

    //23092026 Eine Pyramidenstufe auf der GPU. Ergebnis wird in ux/uy (in place) geschrieben.
    //  Rueckgabe false -> Aufrufer rechnet diese Stufe auf der CPU.
    //  Zeitschritte kt mit identischen Eingaben (cv_main_async uebergibt {im, im}) werden nur einmal gerechnet.
    //  Anmerkung: die CPU-Version nutzt fuer alle kt rho_c[0]; die GPU rechnet jedes kt mit seinem eigenen rho_c.
    //  Bei identischen kt (derzeit immer) ist das gleich.
    /// <summary>
    /// Computes one scale of the flow on the GPU (TV-L1 or TGV-L1).
    /// </summary>
    /// <param name="I0">Source images.</param>
    /// <param name="I1">Target images.</param>
    /// <param name="ux">x component.</param>
    /// <param name="uy">y component.</param>
    /// <param name="nx">Width.</param>
    /// <param name="ny">Height.</param>
    /// <param name="tau">Time step.</param>
    /// <param name="lambda">Data weight.</param>
    /// <param name="theta">Coupling parameter.</param>
    /// <param name="warps">Number of warps.</param>
    /// <param name="epsilon">Stopping threshold.</param>
    /// <param name="scale_idx">Index of the scale.</param>
    /// <param name="total_scales">Number of scales.</param>
    /// <returns>False if the caller has to compute on the CPU.</returns>
    bool try_tvl1_gpu(List<List<float>> I0, List<List<float>> I1, List<List<float>> ux, List<List<float>> uy,
        int nx, int ny, float tau, float lambda, float theta, int warps, float epsilon,
        int scale_idx, int total_scales)
    {
        int size = nx * ny;
        int n_t = Math.Min(Math.Min(I0.Count, I1.Count), Math.Min(ux.Count, uy.Count));
        int max_its = MAX_ITERATIONS;
        float grad_is_zero = (float)GRAD_IS_ZERO;
        //27092026 TGV statt TV (umschaltbar, siehe set_tv_use_tgv); bei einem TGV-Fehler weiter mit GPU-TV
        bool use_tgv = tv_use_tgv && !tgv_failed;
        float alpha1 = 1f;                  // entspricht der bisherigen TV-Gewichtung von |grad u|
        float alpha0 = tgv_ratio * alpha1;

        try
        {
            if (!TvL1Gpu.FitsDispatch(nx, ny))
            {
                throw new Exception("Bild zu gross fuer einen Dispatch: " + nx + "x" + ny);
            }

            int[] src = new int[n_t];
            for (int kt = 0; kt < n_t; kt++)
            {
                src[kt] = kt;
                for (int k2 = 0; k2 < kt; k2++)
                {
                    if (src[k2] == k2 && tv_same_prefix(I0[kt], I0[k2], size) && tv_same_prefix(I1[kt], I1[k2], size)
                        && tv_same_prefix(ux[kt], ux[k2], size) && tv_same_prefix(uy[kt], uy[k2], size))
                    {
                        src[kt] = k2;
                        break;
                    }
                }
            }

            float[][] res_u1 = new float[n_t][];
            float[][] res_u2 = new float[n_t][];
            bool progress_counted = false;

            for (int kt = 0; kt < n_t; kt++)
            {
                if (src[kt] != kt)
                {
                    continue;
                }

                float[] i0 = tv_to_array(I0[kt], size);
                float[] i1 = tv_to_array(I1[kt], size);
                float[] u1 = tv_to_array(ux[kt], size);
                float[] u2 = tv_to_array(uy[kt], size);

                // zentrierter Gradient von I1 einmal je Stufe auf der CPU (identisch zur CPU-Version)
                List<float> gx = zeros_of_size(size);
                List<float> gy = zeros_of_size(size);
                (gx, gy) = centered_gradient_new(I1[kt], gx, gy, nx, ny);
                float[] i1x = gx.ToArray();
                float[] i1y = gy.ToArray();

                run_on_main_thread(() =>
                {
                    if (use_tgv)
                    {
                        if (tgv_gpu == null)
                            tgv_gpu = new TgvL1Gpu();
                        if (!TvL1Gpu.IsSupported() || !tgv_gpu.Load())
                            throw new Exception(TvL1Gpu.IsSupported() ? TgvL1Gpu.last_error : "ComputeShader werden nicht unterstuetzt");
                        tgv_gpu.Begin(i0, i1, i1x, i1y, u1, u2, nx, ny, lambda, theta, epsilon, max_its, grad_is_zero,
                            alpha0, alpha1);
                        return 0;
                    }
                    if (tv_gpu == null)
                    {
                        tv_gpu = new TvL1Gpu();
                    }
                    if (!TvL1Gpu.IsSupported() || !tv_gpu.Load())
                    {
                        throw new Exception(TvL1Gpu.IsSupported() ? TvL1Gpu.last_error : "ComputeShader werden nicht unterstuetzt");
                    }
                    tv_gpu.Begin(i0, i1, i1x, i1y, u1, u2, nx, ny, tau, lambda, theta, epsilon, max_its, grad_is_zero);
                    return 0;
                });

                for (int w = 0; w < warps; w++)
                {
                    if (break_now)
                    {
                        break;
                    }
                    int warp_l = w;
                    int its = run_on_main_thread(() => use_tgv ? tgv_gpu.RunWarp(warp_l) : tv_gpu.RunWarp(warp_l));
                    tv_stat_iterations += its;

                    if (!progress_counted)
                    {
                        // gleiche Fortschrittsbuchhaltung wie die CPU-Schleife (ein Warp = max_its * Pixel)
                        update_tv_progress_step(scale_idx, total_scales, w, warps, its, max_its, nx, ny);
                        completed_work_units += (double)(max_its - 1) * (double)size;
                    }
                }
                progress_counted = true;

                run_on_main_thread(() =>
                {
                    if (use_tgv)
                        tgv_gpu.End(u1, u2);
                    else
                        tv_gpu.End(u1, u2);
                    return 0;
                });
                res_u1[kt] = u1;
                res_u2[kt] = u2;
            }

            if (break_now)
            {
                return true; // Abbruch: wie die CPU-Version ohne weiteres Ergebnis zurueck
            }

            // erst nach Erfolg aller kt zurueckschreiben, damit ein CPU-Rueckfall sauber startet
            for (int kt = 0; kt < n_t; kt++)
            {
                float[] r1 = res_u1[src[kt]];
                float[] r2 = res_u2[src[kt]];
                for (int i = 0; i < size; i++)
                {
                    ux[kt][i] = r1[i];
                    uy[kt][i] = r2[i];
                }
            }
            return true;
        }
        catch (OperationCanceledException)
        {
            release_tv_gpu_async();
            return true;
        }
        catch (Exception e)
        {
            release_tv_gpu_async();
            if (use_tgv)
            {
                //27092026 TGV-Fehler: fuer den Rest des Laufs mit dem bewaehrten GPU-TV weiterrechnen
                tgv_failed = true;
                Debug.LogError("TGV-L1 GPU fehlgeschlagen, weiter mit TV-L1 (GPU): " + e);
                return try_tvl1_gpu(I0, I1, ux, uy, nx, ny, tau, lambda, theta, warps, epsilon, scale_idx, total_scales);
            }
            tv_gpu_failed = true;
            Debug.LogError("TV-L1 GPU fehlgeschlagen, weiter auf der CPU: " + e);
            return false;
        }
    }

    /// <summary>
    /// Releases the GPU solvers on the main thread.
    /// </summary>
    void release_tv_gpu_async()
    {
        main_thread_jobs.Enqueue(() =>
        {
            if (tv_gpu != null)
            {
                tv_gpu.Release();
            }
            if (tgv_gpu != null)
            {
                tgv_gpu.Release();
            }
        });
    }

    /// <summary>
    /// Unity callback: stops running GPU jobs and parameter studies and frees buffers.
    /// </summary>
    void OnDisable()
    {
        //23092026 laufende GPU-Auftraege abbrechen und Puffer freigeben (Play beenden / Reload)
        tv_gpu_shutdown = true;
        if (param_study_running)
            s_param_study_cancel = true; //27092026 laufende Parameterstudie beenden
        if (tv_gpu != null)
        {
            tv_gpu.Release();
            tv_gpu = null;
        }
        if (tgv_gpu != null)
        {
            tgv_gpu.Release();
            tgv_gpu = null;
        }
    }

    /// <summary>
    /// Gradient magnitude and constant part of the linearised data term (rho_c) per pixel.
    /// </summary>
    /// <param name="I0">Source images.</param>
    /// <param name="I1w">Warped target images.</param>
    /// <param name="I1wx">x gradient of the warped image.</param>
    /// <param name="I1wy">y gradient of the warped image.</param>
    /// <param name="ux">x component.</param>
    /// <param name="uy">y component.</param>
    /// <param name="size">Number of pixels.</param>
    /// <param name="grad">Squared gradient (output).</param>
    /// <param name="rho_c">Constant part (output).</param>
    /// <returns>Tuple (grad, rho_c).</returns>
    public (List<List<float>>, List<List<float>>) compute_grad_rho_c_new(List<List<float>> I0, List<List<float>> I1w,
        List<List<float>> I1wx, List<List<float>> I1wy, List<List<float>> ux,
        List<List<float>> uy, int size, List<List<float>> grad, List<List<float>> rho_b, List<List<float>> rho_c,
        List<List<float>> rho_d, List<List<float>> rho_e, List<List<float>> rho_f, List<List<float>> rho_g)
    {
        for (int i = 0; i < size; i++)
        {
            for (int kt = 0; kt < I1wx.Count; kt++)
            {
                float Ix2 = I1wx[kt][i] * I1wx[kt][i];
                float Iy2 = I1wy[kt][i] * I1wy[kt][i];

                // store the |Grad(I1)|^2
                grad[kt][i] = (Ix2 + Iy2);

                // compute the constant part of the rho function
                rho_c[kt][i] = (I1w[kt][i] - I1wx[kt][i] * ux[kt][i]
                    - I1wy[kt][i] * uy[kt][i] - I0[kt][i]);

                rho_g[kt][i] = (I1w[kt][i] - I0[kt][i]);
                rho_b[kt][i] = (I1w[kt][i] - I1wx[kt][i] * ux[kt][i]);
                rho_d[kt][i] = (I1w[kt][i]);
                rho_e[kt][i] = (I1wx[kt][i] * ux[kt][i]);
                rho_f[kt][i] = (I1wx[kt][i]);

                byte[] bytes_l = BitConverter.GetBytes(rho_c[kt][i]);
            }
        }

        return (grad, rho_c);
    }

    public (float, List<List<float>>, List<List<float>>, List<List<float>>,
        List<List<float>>, List<List<float>>, List<List<float>>, List<List<float>>, List<List<float>>
        ) inner_optim_step_new(
        List<float> rho_c, int size, List<List<float>> ux, List<List<float>> uy, List<List<float>> I1wx,
        List<List<float>> I1wy, List<float> rho_debug, List<List<float>> grad, float l_t,
        int warpings, List<float> fi_debug, List<float> decs_debug, List<List<float>> vx,
        List<List<float>> vy, List<float> d2_debug, List<List<float>> div_p1, List<List<float>> div_p2,
        int nx, int ny, float theta, List<List<float>> p11, List<List<float>> p12, List<List<float>> p21,
        List<List<float>> p22, float error, List<List<float>> u1x, List<List<float>> u1y, List<List<float>> u2x,
        List<List<float>> u2y, float tau)
    {
        // perh. TODO: make not just one large kt for loop, but smaller individual ones for
        //      each processing step
        for (int kt = 0; kt < ux.Count; kt++)
        {
            for (int i = 0; i < size; i++)
            {
                // info (paul): thresholding (eq. 13, 12 in paper)
                (float d1, float d2) = find_d1d2(rho_c, rho_debug,
                    I1wx[kt], I1wy[kt], ux[kt], uy[kt], i,
                    l_t, grad[kt], decs_debug, fi_debug,
                    d2_debug, warpings);

                vx[kt][i] = ux[kt][i] + d1;
                vy[kt][i] = uy[kt][i] + d2;
            }
        }

        // compute the divergence of the dual variable (p1, p2)
        div_p1 = divergence(p11, p12, div_p1, nx, ny);
        div_p2 = divergence(p21, p22, div_p2, nx, ny);

        for (int kt = 0; kt < ux.Count; kt++)
        {
            // estimate the values of the optical flow (u1, u2) - eq. 11 in paper
            (ux[kt], uy[kt], error) = estimate_u1u2(div_p1[kt], div_p2[kt], vx[kt], vy[kt], ux[kt],
                uy[kt], error, size, theta);

            // compute the gradient of the optical flow (Du1, Du2)
            (u1x[kt], u1y[kt]) = forward_gradient(ux[kt], u1x[kt], u1y[kt], nx, ny);
            (u2x[kt], u2y[kt]) = forward_gradient(uy[kt], u2x[kt], u2y[kt], nx, ny);
        }

        // estimate the values of the dual variable (p1, p2) - eq. 10 in paper
        for (int kt = 0; kt < ux.Count; kt++)
        {
            (p11[kt], p12[kt], p21[kt], p22[kt]) = find_p1p2(size, tau, theta,
                u1x[kt], u1y[kt], u2x[kt], u2y[kt], p11[kt], p12[kt], p21[kt], p22[kt]);
        }

        return (error, p11, p12, p21, p22, ux, uy, vx, vy);
    }

    /// <summary>
    /// Thresholding step of TV-L1 at one pixel (data term).
    /// </summary>
    /// <param name="rho_c">Constant part.</param>
    /// <param name="rho_debug">Debug output.</param>
    /// <param name="I1wx">x gradient.</param>
    /// <param name="I1wy">y gradient.</param>
    /// <param name="ux">x component.</param>
    /// <param name="uy">y component.</param>
    /// <param name="i">Pixel index.</param>
    /// <param name="l_t">lambda * theta.</param>
    /// <param name="grad">Squared gradient.</param>
    /// <param name="warpings">Warp index.</param>
    /// <returns>Tuple (d1, d2) update.</returns>
    public (float, float) find_d1d2(List<float> rho_c, List<float> rho_debug,
        List<float> I1wx, List<float> I1wy, List<float> ux, List<float> uy, int i,
        float l_t, List<float> grad, List<float> decs_debug, List<float> fi_debug,
        List<float> d2_debug, int warpings)
    {
        float rho = rho_c[i] + (I1wx[i] * ux[i] + I1wy[i] * uy[i]);
        rho_debug[i] = rho;

        float d1, d2;
        float grad_i = grad[i];
        if (rho < -l_t * grad_i)
        {
            d1 = l_t * I1wx[i];
            d2 = l_t * I1wy[i];
            decs_debug[i] = 0f;
        }
        else
        {
            if (rho > l_t * grad_i)
            {
                d1 = -l_t * I1wx[i];
                d2 = -l_t * I1wy[i];
                decs_debug[i] = 0.5f;
            }
            else
            {
                if (grad_i < GRAD_IS_ZERO) //29092026 Bugfix: in jedem Warp (wie IPOL), sonst NaN bei g = 0
                {
                    d1 = d2 = 0;
                    decs_debug[i] = 0.75f;
                }
                else
                {
                    float fi = -rho / grad_i;
                    fi_debug[i] = fi;
                    d1 = fi * I1wx[i];
                    d2 = fi * I1wy[i];
                    decs_debug[i] = 1f;
                }
            }
        }

        d2_debug[i] = d2;
        return (d1, d2);
    }

    /// <summary>
    /// Updates the flow from the auxiliary field and the divergence of the dual variable.
    /// </summary>
    /// <param name="div_p1">Divergence of p1.</param>
    /// <param name="div_p2">Divergence of p2.</param>
    /// <param name="vx">Auxiliary x.</param>
    /// <param name="vy">Auxiliary y.</param>
    /// <param name="ux">x component.</param>
    /// <param name="uy">y component.</param>
    /// <param name="error">Change (output).</param>
    /// <param name="size">Number of pixels.</param>
    /// <param name="theta">Coupling parameter.</param>
    /// <returns>Tuple (ux, uy, change).</returns>
    public (List<float>, List<float>, float) estimate_u1u2(List<float> div_p1,
        List<float> div_p2, List<float> vx, List<float> vy, List<float> ux,
        List<float> uy, float error, int size, float theta)
    {
        error = (float)0.0;
        for (int i = 0; i < size; i++)
        {
            float u1k = ux[i];
            float u2k = uy[i];

            float prod_u1_l = theta * div_p1[i];
            float sum_u1_l = vx[i] + prod_u1_l;
            ux[i] = sum_u1_l;

            float prod_u2_l = theta * div_p2[i];
            float sum_u2_l = vy[i] + prod_u2_l;
            uy[i] = sum_u2_l;

            //05082024 u1[i] = v1[i] + theta * div_p1[i];
            //05082024 u2[i] = v2[i] + theta * div_p2[i];

            error += (ux[i] - u1k) * (ux[i] - u1k) +
                (uy[i] - u2k) * (uy[i] - u2k);
        }
        error /= size;
        return (ux, uy, error);
    }
    /// <summary>
    /// Stores the current experiment configuration.
    /// </summary>
    /// <param name="config_now">Configuration.</param>
    public void set_config_now(ExpConfig config_now)
    {
        this.config_now = config_now;
    }
    /// <summary>
    /// Returns the current experiment configuration.
    /// </summary>
    /// <returns>Configuration.</returns>
    public ExpConfig get_config_now()
    {
        return config_now;
    }
    /// <summary>
    /// Updates the dual variables p of TV-L1.
    /// </summary>
    /// <param name="size">Number of pixels.</param>
    /// <param name="tau">Time step.</param>
    /// <param name="theta">Coupling parameter.</param>
    /// <param name="u1x">x gradient of u1.</param>
    /// <param name="u1y">y gradient of u1.</param>
    /// <param name="u2x">x gradient of u2.</param>
    /// <param name="u2y">y gradient of u2.</param>
    /// <param name="p11">Dual variable.</param>
    /// <param name="p12">Dual variable.</param>
    /// <param name="p21">Dual variable.</param>
    /// <param name="p22">Dual variable.</param>
    /// <returns>Tuple of updated dual variables.</returns>
    public (List<float>, List<float>, List<float>, List<float>) find_p1p2(float
        size, float tau, float theta, List<float> u1x, List<float> u1y,
        List<float> u2x, List<float> u2y, List<float> p11, List<float> p12,
        List<float> p21, List<float> p22)
    {
        for (int i = 0; i < size; i++)
        {
            float taut = tau / theta;
            float g1 = (float)hypot(u1x[i], u1y[i]);
            float g2 = (float)hypot(u2x[i], u2y[i]);
            float ng1 = 1f + taut * g1;
            float ng2 = 1f + taut * g2;

            p11[i] = (p11[i] + taut * u1x[i]) / ng1;
            p12[i] = (p12[i] + taut * u1y[i]) / ng1;
            p21[i] = (p21[i] + taut * u2x[i]) / ng2;
            p22[i] = (p22[i] + taut * u2y[i]) / ng2;
        }
        return (p11, p12, p21, p22);
    }

    /**
     *
     * Compute the max and min of an array
     *
     **/

    // info (paul): an image pointer vector with the corresponding width and height information
    //26072024 typedef struct {
    //26072024         List<float> im_vec;
    //26072024         int width;
    //26072024         int height;
    //26072024     }
    //26072024     im_dressed;

    /// <summary>
    /// Minimum and maximum of a matrix.
    /// </summary>
    /// <param name="x">Matrix.</param>
    /// <returns>Tuple (min, max).</returns>
    static (float, float) getminmax(
        List<List<float>> x, // input array
        int x_cnt           // array size
    )
    {
        float min_val = x[0][0];
        float max_val = x[0][0];

        //int sizeof_x = sizeof(x);
        //int sizeof_float = sizeof(float);
        //int cnt_l = sizeof_x / sizeof_float;

        for (int k = 0; k < x.Count; k++)
        {
            for (int i = 1; i < x[0].Count; i++)
            {
                //int cnt_l2 = cnt_l + 1;

                //float x_el_pre = x[i - 1];
                float x_el = x[k][i];
                if (x_el < min_val)
                    min_val = x[k][i];
                if (x_el > max_val)
                    max_val = x[k][i];
            }
        }
        return (min_val, max_val);

    }

    /**
     *
     * Function to normalize the images between 0 and 255
     *
     **/
    /// <summary>
    /// Normalises both images to a common gray-value range [0, 255].
    /// </summary>
    /// <param name="I0">First images.</param>
    /// <param name="I1">Second images.</param>
    /// <param name="I0n">Normalised first images (output).</param>
    /// <param name="I1n">Normalised second images (output).</param>
    /// <returns>Tuple of normalised images.</returns>
    (List<List<float>>, List<List<float>>) image_normalization(
            List<List<float>> I0,  // input image0
            List<List<float>> I1,  // input image1
            List<List<float>> I0n,       // normalized output image0
            List<List<float>> I1n,       // normalized output image1
            int size          // size of the image
            )
    {
        //float max0, max1, min0, min1;

        // obtain the max and min of each image
        //02042024 getminmax(&min0, &max0, I0, size);
        //02042024 getminmax(&min1, &max1, I1, size);
        (float min0, float max0) = getminmax(I0, size);
        (float min1, float max1) = getminmax(I1, size);

        // obtain the max and min of both images
        float max = Mathf.Max((float)max0, (float)max1);//(max0 > max1) ? max0 : max1;
        float min = Mathf.Min((float)min0, (float)min1); //(min0 < min1) ? min0 : min1;
        float den = max - min;

        if (den > 0)
        {
            // normalize both images
            for (int k = 0; k < I0.Count; k++)
            {
                for (int i = 0; i < I0[0].Count; i++)
                {
                    I0n[k][i] = 255f * (I0[k][i] - min) / den;
                    I1n[k][i] = 255f * (I1[k][i] - min) / den;
                }
            }
        }
        else
        {
            // copy the original images
            //28092026 Bugfix: I0/I1 sind Listen von Bildern (je Zeitschritt) - bisher I0n[i] = I0[i] ueber i < size
            //  (Pixelzahl) -> ArgumentOutOfRangeException, sobald beide Bilder kontrastlos sind
            for (int k = 0; k < I0.Count; k++)
            {
                for (int i = 0; i < I0[k].Count; i++)
                {
                    I0n[k][i] = I0[k][i];
                    I1n[k][i] = I1[k][i];
                }
            }
        }
        return (I0n, I1n);
    }
    /// <summary>
    /// Multiplies a list by a factor in place.
    /// </summary>
    /// <param name="mat">List.</param>
    /// <param name="factor">Factor.</param>
    /// <returns>The list.</returns>
    public List<float> scale_by(List<float> mat, float factor)
    {
        for (int i = 0; i < mat.Count; i++)
        {
            mat[i] *= factor;
        }

        return mat;
    }
    /// <summary>
    /// Normalises an image vector to 0..1.
    /// </summary>
    /// <param name="u">Values.</param>
    /// <param name="n_x">Width.</param>
    /// <param name="n_y">Height.</param>
    /// <returns>Normalised values.</returns>
    List<float> norm_floats(List<float> u, int n_x, int n_y)
    {
        //float min_val = 9999.;
        //float max_val = -9999.;
        //
        //// info (paul): find max and min
        //for (int i = 0; i < n_x*n_y; i++)
        //{
        //	float u_el =  u[i];
        //	if (u_el < min_val)
        //	{
        //		min_val = u_el;
        //	}
        //	if (u_el > max_val)
        //	{
        //		max_val = u_el;
        //	}
        //}
        //31082024 float min_val = find_min(u, n_x, n_y);
        //31082024 float max_val = find_max(u, n_x, n_y);
        (float min_val, float max_val) = find_min_max(u);

        // info (paul): norm, so that everything is positive
        for (int j = 0; j < n_x * n_y; j++)
        {
            u[j] = (u[j] - min_val) / (max_val - min_val);
            // perh. TODO: do a full normalization
        }
        return u;

    }

    /// <summary>
    /// Normalises each image vector of a list separately.
    /// </summary>
    /// <param name="u">Image vectors.</param>
    /// <param name="n_x">Width.</param>
    /// <param name="n_y">Height.</param>
    /// <returns>Normalised vectors.</returns>
    List<List<float>> norm_floatss(List<List<float>> u, int n_x, int n_y)
    {
        // info (paul): norm list of lists, each separately
        for (int i = 0; i < u.Count; i++)
        {
            u[i] = norm_floats(u[i], n_x, n_y);
        }
        return u;
    }

    /// <summary>
    /// Crops the centre of a texture.
    /// </summary>
    /// <param name="source">Texture.</param>
    /// <param name="target_w">Target width.</param>
    /// <param name="target_h">Target height.</param>
    /// <returns>Cropped texture.</returns>
    public Texture2D crop_tex(Texture2D source, int target_w, int target_h)
    {
        Texture2D cropped = new Texture2D(target_w, target_h, TextureFormat.ARGB32, false);

        int start_x = ((int)(source.width * 0.5f)) - target_w/2;
        int start_y = ((int)(source.height * 0.5f)) - target_h/2;
        
        cropped.SetPixels(source.GetPixels(start_x, start_y, target_w, target_h));

        return cropped;
    }
    //23092026 Pfad zeigt auf eine vorhandene Bilddatei
    /// <summary>
    /// Checks whether a path points to an existing image file.
    /// </summary>
    /// <param name="path">Path.</param>
    /// <returns>True if usable.</returns>
    static bool usable_image(string path)
    {
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
    }

    /// <summary>
    /// Reads an image file (PNG/JPG or TIFF) as gray values.
    /// </summary>
    /// <param name="im_path">Image file.</param>
    /// <returns>Image vector with width and height.</returns>
    im_dressed manage_read_im(string im_path)
    {
        byte[] bytes = File.ReadAllBytes(im_path);

        if (im_path.EndsWith(".tif") || im_path.EndsWith(".tiff"))
        {
            throw new ArgumentException("im_path must be .png");
        }

        (int width_pre, int height_pre) = bytes2res(bytes);
        Texture2D texture = new Texture2D(width_pre, height_pre, TextureFormat.ARGB32, false);
        texture.LoadImage(bytes);

        (int width, int height) = (get_render_res(), get_render_res());//25122024 (512, 512);//17122024 (1024, 1024);//12122024 (512, 512);
        Texture2D cropped = crop_tex(texture, width, height);

        //12022025 System.IO.File.WriteAllBytes("C:/Users/go73jem/Pictures/test111.png", cropped.EncodeToPNG());

        List<float> mat = tex2floats(cropped, with_switch_dims: false, color_channel: 1);
        //28092026 Nutzerwunsch: Belichtungsfaktor k fuer die normale Analyse (Feld "Analyse-k" bei Licht/Rauschen):
        //  lineare Kamera, g' = min(1, round(k*g*255)/255). Die gerenderten Originale bleiben unveraendert; eine
        //  Kopie des belichteten Bilds liegt zum Ansehen in Assets/analysis_results/exposure_images/.
        //29092026 Bugfix: in den Licht/Rauschen/Speckle-Sweeps kein Analyse-k (sonst verfaelscht ein uebrig
        //  gebliebenes k, z.B. 0.02, den ganzen Sweep: Bilder fast schwarz -> Fluss 0 -> Fehler 100 %)
        if (Math.Abs(analysis_exposure - 1f) > 1e-6f && !exposure_bypass && !analysis_sweep_running())
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (int q = 0; q < mat.Count; q++)
            {
                mat[q] = Mathf.Min(1f, Mathf.Round(analysis_exposure * mat[q] * 255f) / 255f);
                lo = Mathf.Min(lo, mat[q]);
                hi = Mathf.Max(hi, mat[q]);
            }
            save_exposed_preview(mat, width, height, im_path);
            //28092026 kontrastloses Bild (alles schwarz/weiss): nur Warnung - die Analyse laeuft weiter (Nutzerwunsch:
            //  Zusammenbruch sichtbar machen; image_normalization kommt mit kontrastlosen Bildern jetzt zurecht)
            if (hi - lo < 1.5f / 255f)
            {
                string msg = "Hinweis: Belichtung k = " + analysis_exposure.ToString("G4", CultureInfo.InvariantCulture)
                    + " macht das Kamerabild kontrastlos (" + (hi <= 0f ? "komplett schwarz" : hi >= 1f && lo >= 1f ? "komplett gesaettigt" : "nur ein Grauwert")
                    + ") - TV kann hier keinen Fluss mehr bestimmen.";
                Debug.LogWarning(msg);
                ExperimentImageGallery.SetResultsText(msg);
            }
        }
        List<float> mat_flipped = flip_floats(mat, width, height);
        List<float> mat_scaled = scale_by(mat_flipped, factor: 0.33f * 256f);

        (List<float> mat_lower_res, int width_new, int height_new) = sample_res_for_floats(
            mat_scaled, width, height, fac_x: 1f, fac_y: 1f);//17122024 , fac_x: 0.25f, fac_y: 0.25f);//12122024 fac_x: 0.5f, fac_y: 0.5f)

        im_dressed im_dressed_1 = new im_dressed(mat_lower_res, width_new, height_new); //(mat_scaled, width, height)
        return im_dressed_1;
    }

    /// <summary>
    /// Downsamples a matrix by nearest-neighbour sampling.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <param name="fac_x">Factor in x.</param>
    /// <param name="fac_y">Factor in y.</param>
    /// <returns>Smaller matrix.</returns>
    public List<List<float>> sample_res(List<List<float>> mat, float fac_x = float.NaN, float fac_y = float.NaN)
    {
        // info (paul): sampling down to lower resolution (currently this just done by
        //      taking the closest pixel, later you might implement a more sophisticated
        //      interpolation scheme)

        int width = mat.Count;
        int height = mat[0].Count;

        int width_new = (int)((float)width * fac_x);
        int height_new = (int)((float)height * fac_y);

        List<List<float>> smaller = zeros_of_size(width_new, height_new);

        for (int i = 0; i < width_new; i++)
        {
            for (int j = 0; j < height_new; j++)
            {
                int i_source = (int)((float)i / fac_x);
                int j_source = (int)((float)j / fac_y);

                smaller[i][j] = mat[i_source][j_source];
            }
        }

        return smaller;
    }

    /// <summary>
    /// Downsamples an image vector.
    /// </summary>
    /// <param name="floats">Values.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <param name="fac_x">Factor in x.</param>
    /// <param name="fac_y">Factor in y.</param>
    /// <returns>Tuple (values, width, height).</returns>
    public (List<float>, int, int) sample_res_for_floats(List<float> floats, int width, int height, float fac_x, float fac_y)
    {
        float[][] mat_ar = floats2matrix(floats.ToArray(), width, height);
        List<List<float>> mat = floats2_to_lists(mat_ar);

        List<List<float>> smaller = sample_res(mat, fac_x: fac_x, fac_y: fac_y);

        List<float> list = matrix2list(smaller);

        return (list, smaller.Count, smaller[0].Count);
    }

    /// <summary>
    /// Flips an image vector vertically.
    /// </summary>
    /// <param name="mat">Values.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <returns>Flipped values.</returns>
    public List<float> flip_floats(List<float> mat, int width, int height)
    {
        List<float> mat_new = zeros_of_size(mat.Count);

        // info (paul): interpret the floats as matrix and flip them
        for (int i = 0; i < mat.Count; i++)
        {
            int i_idx = i % width;
            int j_idx = (i - i_idx) / width;

            mat_new[(height - 1 - i_idx) * width + j_idx] = mat[i_idx * width + j_idx];
        }

        return mat_new;
    }

    string PAR_DEFAULT_OUTFLOW = "flow.flo";
    int PAR_DEFAULT_NPROC = 0;
    double PAR_DEFAULT_TAU = 0.25d;//0.25d;//5.0d;//27092024 0.25d;
    double PAR_DEFAULT_LAMBDA = 0.15d;//0.0001f;//C 0.0001d;//A 0.00001d;//B 0.0001d;//0.15d;//1.0d;//27092024 0.15d;
    double PAR_DEFAULT_THETA = 0.3d;//20d;//08102024 20d;//C 20d;//A 200d;//20d;//B 20d;//0.01d;//27092024 0.3d;
    int PAR_DEFAULT_NSCALES = 8;//4;//4;//2;//27092024 2;//25092024 3;//24092024 100;
    double PAR_DEFAULT_ZFACTOR = 0.5d;//0.5d;//27092024 0.5d;
    int PAR_DEFAULT_NWARPS = 5;//6;//6;//08102024 20;//27092024 6;//5//25092024 5;//Bc 20;//20;//5;//08052024 5;
    double PAR_DEFAULT_EPSILON = 0.01f;//08102024 0.0001f;//C 0.0001d;//A 0.00005d;//27092024 0.01d;
    int PAR_DEFAULT_VERBOSE = 0;

    //22092026 Nutzerwunsch: TV-Parameter zur Laufzeit einstellbar (Panel "TV-Parameter").
    //NaN / <= 0 bedeutet "Automatik" -> der bisherige, vom Modus abhaengige Wert bleibt.
    //Werte werden in PlayerPrefs gespeichert und beim Start wieder geladen.
    private double tv_lambda_override = double.NaN;
    private double tv_theta_override = double.NaN;
    private int tv_nscales_override = -1;
    private int tv_nwarps_override = -1;
    private int tv_iterations_override = -1;
    private double tv_epsilon_override = double.NaN;

    //23092026 TV-L1 auf der GPU (ComputeShader Resources/TVL1.compute, siehe TvL1Gpu.cs).
    //  Umschaltbar im TV-Parameter-Panel, gespeichert in PlayerPrefs "tv_use_gpu" (Default: an).
    //  Der CPU-Pfad bleibt unveraendert als Referenz und als Rueckfall bei GPU-Fehlern.
    private volatile bool tv_use_gpu = true;
    private volatile bool tv_gpu_failed = false;
    private volatile bool tv_gpu_shutdown = false;
    private TvL1Gpu tv_gpu = null;
    //27092026 TGV-Regularisierung (nur GPU), umschaltbar im TV-Parameter-Panel; TV bleibt Standard.
    //  PlayerPrefs "tv_use_tgv" (0/1) und "tgv_ratio" (alpha0/alpha1, Default 3).
    private volatile bool tv_use_tgv = false;
    private volatile bool tgv_failed = false;
    private float tgv_ratio = 3f;
    private TgvL1Gpu tgv_gpu = null;
    /// <summary>
    /// Returns whether TGV regularisation is selected.
    /// </summary>
    /// <returns>True for TGV.</returns>
    public bool get_tv_use_tgv() { return tv_use_tgv; }
    /// <summary>
    /// Returns the TGV weight ratio alpha0/alpha1.
    /// </summary>
    /// <returns>Ratio.</returns>
    public float get_tgv_ratio() { return tgv_ratio; }
    /// <summary>
    /// Selects TGV or TV regularisation and stores the choice.
    /// </summary>
    /// <param name="use_tgv">True for TGV.</param>
    public void set_tv_use_tgv(bool use_tgv)
    {
        tv_use_tgv = use_tgv;
        tgv_failed = false;
        PlayerPrefs.SetInt("tv_use_tgv", use_tgv ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("Regularisierung: " + (use_tgv ? "TGV (alpha0/alpha1 = " + tgv_ratio + ")" : "TV"));
    }
    /// <summary>
    /// Sets and stores the TGV weight ratio alpha0/alpha1 (invalid values give 3).
    /// </summary>
    /// <param name="ratio">Ratio.</param>
    public void set_tgv_ratio(float ratio)
    {
        tgv_ratio = (float.IsNaN(ratio) || ratio <= 0f) ? 3f : ratio;
        PlayerPrefs.SetFloat("tgv_ratio", tgv_ratio);
        PlayerPrefs.Save();
    }
    //27092026 Kurzbeschreibung der Regularisierung fuer Log und Genauigkeitsbericht
    /// <summary>
    /// Short description of the regularisation for log and accuracy report.
    /// </summary>
    /// <returns>Text.</returns>
    public string describe_regularization()
    {
        return tv_use_tgv
            ? "TGV (alpha0/alpha1 = " + tgv_ratio.ToString("0.##", CultureInfo.InvariantCulture) + ", nur GPU)"
            : "TV";
    }
    private long tv_stat_iterations = 0;
    private int main_thread_id = -1;
    private readonly System.Collections.Concurrent.ConcurrentQueue<Action> main_thread_jobs =
        new System.Collections.Concurrent.ConcurrentQueue<Action>();

    /// <summary>
    /// Returns whether the GPU solver is used.
    /// </summary>
    /// <returns>True for GPU.</returns>
    public bool get_tv_use_gpu() { return tv_use_gpu; }
    /// <summary>
    /// Selects GPU or CPU solver and stores the choice.
    /// </summary>
    /// <param name="use_gpu">True for GPU.</param>
    public void set_tv_use_gpu(bool use_gpu)
    {
        tv_use_gpu = use_gpu;
        tv_gpu_failed = false; // nach Umschalten neuen GPU-Versuch erlauben
        PlayerPrefs.SetInt("tv_use_gpu", use_gpu ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("TV-Rechenweg: " + (use_gpu ? "GPU" : "CPU"));
    }

    /// <summary>
    /// Returns the manual lambda (NaN = automatic).
    /// </summary>
    /// <returns>Value.</returns>
    public double get_tv_lambda_override() { return tv_lambda_override; }
    /// <summary>
    /// Returns the manual theta (NaN = automatic).
    /// </summary>
    /// <returns>Value.</returns>
    public double get_tv_theta_override() { return tv_theta_override; }
    /// <summary>
    /// Returns the manual number of scales (-1 = automatic).
    /// </summary>
    /// <returns>Value.</returns>
    public int get_tv_nscales_override() { return tv_nscales_override; }
    /// <summary>
    /// Returns the manual number of warps (-1 = automatic).
    /// </summary>
    /// <returns>Value.</returns>
    public int get_tv_nwarps_override() { return tv_nwarps_override; }
    /// <summary>
    /// Returns the manual number of iterations (-1 = automatic).
    /// </summary>
    /// <returns>Value.</returns>
    public int get_tv_iterations_override() { return tv_iterations_override; }
    /// <summary>
    /// Returns the manual stopping threshold (NaN = automatic).
    /// </summary>
    /// <returns>Value.</returns>
    public double get_tv_epsilon_override() { return tv_epsilon_override; }

    /// <summary>
    /// Sets and stores the manual solver parameters (NaN or -1 = automatic).
    /// </summary>
    /// <param name="lambda">Data weight.</param>
    /// <param name="theta">Coupling parameter.</param>
    /// <param name="nscales">Number of scales.</param>
    /// <param name="nwarps">Number of warps.</param>
    /// <param name="iterations">Number of iterations.</param>
    /// <param name="epsilon">Stopping threshold.</param>
    public void set_tv_overrides(double lambda, double theta, int nscales, int nwarps,
        int iterations, double epsilon)
    {
        tv_lambda_override = lambda;
        tv_theta_override = theta;
        tv_nscales_override = nscales;
        tv_nwarps_override = nwarps;
        tv_iterations_override = iterations;
        tv_epsilon_override = epsilon;
        PlayerPrefs.SetFloat("tv_lambda", (float)lambda);
        PlayerPrefs.SetFloat("tv_theta", (float)theta);
        PlayerPrefs.SetInt("tv_nscales", nscales);
        PlayerPrefs.SetInt("tv_nwarps", nwarps);
        PlayerPrefs.SetInt("tv_iterations", iterations);
        PlayerPrefs.SetFloat("tv_epsilon", (float)epsilon);
        PlayerPrefs.Save();
        Debug.Log("TV-Parameter gesetzt: " + describe_tv_overrides());
    }

    /// <summary>
    /// Loads the manual solver parameters from the PlayerPrefs.
    /// </summary>
    public void load_tv_overrides()
    {
        tv_lambda_override = PlayerPrefs.GetFloat("tv_lambda", float.NaN);
        tv_theta_override = PlayerPrefs.GetFloat("tv_theta", float.NaN);
        tv_nscales_override = PlayerPrefs.GetInt("tv_nscales", -1);
        tv_nwarps_override = PlayerPrefs.GetInt("tv_nwarps", -1);
        tv_iterations_override = PlayerPrefs.GetInt("tv_iterations", -1);
        tv_epsilon_override = PlayerPrefs.GetFloat("tv_epsilon", float.NaN);
        tv_use_gpu = PlayerPrefs.GetInt("tv_use_gpu", 1) == 1; //23092026
        strain_sigma = PlayerPrefs.GetFloat("strain_sigma", 12f); //27092026
        tv_use_tgv = PlayerPrefs.GetInt("tv_use_tgv", 0) == 1; //27092026 Standard: TV
        tgv_ratio = PlayerPrefs.GetFloat("tgv_ratio", 3f);
        speckle_texture_mode = Mathf.Clamp(PlayerPrefs.GetInt("speckle_texture_mode", 0), 0, 2); //27092026
        analysis_exposure = PlayerPrefs.GetFloat("analysis_exposure", 1f); //28092026
        if (float.IsNaN(analysis_exposure) || analysis_exposure < EXPOSURE_MIN || analysis_exposure > EXPOSURE_MAX)
        {
            Debug.LogWarning("Gespeicherte Belichtung k = " + analysis_exposure + " ungueltig -> 1");
            analysis_exposure = 1f;
        }
        ExperimentImageGallery.RefreshStateLabels(this);
        Debug.Log("TV-Parameter geladen: " + describe_tv_overrides());
    }

    /// <summary>
    /// Text of the manual solver parameters (auto for automatic).
    /// </summary>
    /// <returns>Text.</returns>
    public string describe_tv_overrides()
    {
        Func<double, string> d = value => (double.IsNaN(value) || value <= 0d)
            ? "auto" : value.ToString("G6", CultureInfo.InvariantCulture);
        Func<int, string> i = value => value <= 0 ? "auto" : value.ToString(CultureInfo.InvariantCulture);
        return "lambda=" + d(tv_lambda_override) + ", theta=" + d(tv_theta_override)
            + ", nscales=" + i(tv_nscales_override) + ", nwarps=" + i(tv_nwarps_override)
            + ", its=" + i(tv_iterations_override) + ", eps=" + d(tv_epsilon_override);
    }

    /// <summary>
    /// Solver parameters of the analysis (defaults, experiment-specific values, manual overrides).
    /// </summary>
    /// <param name="with_dt">True for time-series analysis.</param>
    /// <param name="exp_label">Experiment label.</param>
    /// <returns>Tuple (lambda, theta, scales, warps).</returns>
    public (double, double, int, int) set_up_pars(bool with_dt, string exp_label = null)
    {
        int dt_compare_l = 0;
        MAX_ITERATIONS = analysis_sweep_running() ? 80 : 300;

        if (with_dt)
        {
            dt_compare_l = get_dt_compare();
            PAR_DEFAULT_LAMBDA = 0.02d;//29032026 0.05d;//26102024 0.0001f;
            PAR_DEFAULT_THETA = 0.55d;//26102024 20d;
            //26022025 PAR_DEFAULT_LAMBDA = 0.3d;
            //26022025 PAR_DEFAULT_THETA = 0.3d;
            PAR_DEFAULT_NSCALES = analysis_sweep_running() ? 4 : 6;
            PAR_DEFAULT_NWARPS = analysis_sweep_running() ? 2 : 3;
        }
        else
        {
            PAR_DEFAULT_LAMBDA = 0.15f;//01032025 0.3d;//0.15d;
            PAR_DEFAULT_THETA = 0.3f;//01032025 0.3d;
            if (exp_label.EndsWith("_heights"))
            {
                PAR_DEFAULT_NSCALES = 8;//8;//18032025 1; //02032025 8;//11112024 8; //22102024 8;
                PAR_DEFAULT_NWARPS = 3;//3;//18032025 1; //02032025 3;//06122024 4;//11112024 4; //22102024 4;
            }
            else
            {
                PAR_DEFAULT_NSCALES = 8;//8;//18032025 1; //02032025 8;//11112024 8; //22102024 8;
                PAR_DEFAULT_NWARPS = 4;//4;//18032025 1; //02032025 4;//11112024 4; //22102024 4;
            }
        }

        // f?r debugging: ?berschreibe kurz: 
        //21012026 PAR_DEFAULT_NSCALES = 2;
        //21012026 PAR_DEFAULT_NWARPS = 2;
        //22092026 Benutzerwerte aus dem TV-Parameter-Panel haben Vorrang (siehe set_tv_overrides).
        if (!double.IsNaN(tv_lambda_override) && tv_lambda_override > 0d)
            PAR_DEFAULT_LAMBDA = tv_lambda_override;
        if (!double.IsNaN(tv_theta_override) && tv_theta_override > 0d)
            PAR_DEFAULT_THETA = tv_theta_override;
        if (tv_nscales_override > 0)
            PAR_DEFAULT_NSCALES = tv_nscales_override;
        if (tv_nwarps_override > 0)
            PAR_DEFAULT_NWARPS = tv_nwarps_override;
        if (tv_iterations_override > 0)
            MAX_ITERATIONS = tv_iterations_override;
        if (!double.IsNaN(tv_epsilon_override) && tv_epsilon_override > 0d)
            PAR_DEFAULT_EPSILON = tv_epsilon_override;

        Debug.Log("TV-Parameter aktiv: lambda=" + PAR_DEFAULT_LAMBDA.ToString("G6", CultureInfo.InvariantCulture)
            + ", theta=" + PAR_DEFAULT_THETA.ToString("G6", CultureInfo.InvariantCulture)
            + ", nscales=" + PAR_DEFAULT_NSCALES + ", nwarps=" + PAR_DEFAULT_NWARPS
            + ", its=" + MAX_ITERATIONS
            + ", eps=" + PAR_DEFAULT_EPSILON.ToString("G6", CultureInfo.InvariantCulture)
            + ", Rechenweg=" + (tv_use_gpu ? "GPU" : "CPU") //23092026
            + ", Regularisierung=" + describe_regularization() //27092026
            + " (Overrides: " + describe_tv_overrides() + ")");

        return (PAR_DEFAULT_LAMBDA, PAR_DEFAULT_THETA, PAR_DEFAULT_NSCALES, PAR_DEFAULT_NWARPS);
    }
    /// <summary>
    /// Frame indices of an image pair or triple.
    /// </summary>
    /// <param name="with_dt">True for time-series analysis.</param>
    /// <param name="t_idx_i">Index of the pair.</param>
    /// <returns>Tuple of three frame indices.</returns>
    public (int, int, int) fetch_t_idxs(bool with_dt, int t_idx_i)
    {
        int current_blade_idx = get_blade_idx();
        int t_idx_0 = -1;
        int t_idx_1 = -1;
        int t_idx_2 = -1;

        if (with_dt)
        {
            t_idx_0 = blade_idxs[t_idx_i];
            t_idx_1 = blade_idxs[t_idx_i + 1];
            t_idx_2 = blade_idxs[t_idx_i + 2];
        }
        if (!with_dt)
        {
            t_idx_0 = blade_idxs[t_idx_i];
            t_idx_1 = blade_idxs[t_idx_i];
        }
        if (with_our_idxs)
        {
            t_idx_0 = our_blade_idxs[0];
            t_idx_1 = our_blade_idxs[1];
        }

        return (t_idx_0, t_idx_1, t_idx_2);
    }

    /// <summary>
    /// Starts the flow computation of an image series (if no computation is running).
    /// </summary>
    /// <param name="paths">Image paths.</param>
    /// <param name="in_dir">Input folder.</param>
    /// <param name="out_dir">Output folder.</param>
    /// <param name="pars">Parameters.</param>
    /// <param name="series_idx">Index of the series.</param>
    /// <param name="is_started">True to start.</param>
    /// <returns>Tuple (index, started).</returns>
    public (int, bool) cv_series(List<string> paths, string in_dir = "", string out_dir = "",
        Params pars = null, int series_idx = 0, bool is_started = false)
    {
        if (!is_tv_running && is_started)
        {
            StartCoroutine(cv_series_coroutine(paths, in_dir, out_dir, pars));
        }

        return (this.series_idx, this.is_started);
    }

    /// <summary>
    /// Coroutine: computes the flow for consecutive image pairs of a series.
    /// </summary>
    /// <param name="paths">Image paths.</param>
    /// <param name="in_dir">Input folder.</param>
    /// <param name="out_dir">Output folder.</param>
    /// <param name="pars">Parameters.</param>
    /// <returns>Coroutine enumerator.</returns>
    public IEnumerator cv_series_coroutine(List<string> paths, string in_dir = "", string out_dir = "",
        Params pars = null)
    {
        if (pars is null)
        {
            pars = new Params();
        }

        List<string> out_paths = new List<string>();
        List<string> im0_paths = new List<string>();
        List<string> im1_paths = new List<string>();

        if (in_dir != "")
        {
            bool is_dir = Directory.Exists(in_dir);
            if (is_dir)
            {
                string[] paths_ar = Directory.GetFiles(in_dir);
                paths = paths_ar.ToList();
            }
        }
        if (out_dir != "")
        {
            bool is_dir = Directory.Exists(out_dir);
            if (is_dir)
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    string out_file = out_dir + "/flow_" + i.ToString() + ".png";
                    string im0_file = out_dir + "/ims0/im0_" + i.ToString() + ".png";
                    string im1_file = out_dir + "/ims1/im1_" + i.ToString() + ".png";
                    
                    out_paths.Add(out_file);
                    im0_paths.Add(im0_file);
                    im1_paths.Add(im1_file);
                }
            }
        }

        is_tv_running = true;
        int total_pairs = (out_paths.Count > 1) ? (out_paths.Count - 1) : 1;

        for (int s_idx = 0; s_idx < out_paths.Count - 1; s_idx++)
        {
            if (break_now || !get_is_started())
            {
                break;
            }

            series_idx = s_idx;
            set_series_progress_info(s_idx + 1, total_pairs);

            yield return StartCoroutine(cv_for_paths_coroutine(paths[s_idx], paths[s_idx + 1], render_act_idx: 0, d_cam: 0,
                with_dt: true, exp_label: null, save_path: out_paths[s_idx],
                im0_path: im0_paths[s_idx], im1_path: im1_paths[s_idx], pars: pars, series_idx: s_idx));

            yield return null;
        }

        finish_all_tv_progress();

        if (!break_now)
        {
            // info (paul): matched flow:
            List<List<float>> flow_mat_0;
            List<List<float>> flow_mat_1;
            List<List<float>> stream_z;
            List<List<(float, float)>> points_next;
            
            string path_flow_v_l = path_time_flow_v + "exp_normal/time_flow_v/";
            string path_flow_u_l = path_time_flow_u + "exp_normal/time_flow_u/";

            u_v_mode = "v";
            (flow_mat_0, res_x, res_y, stream_z) = load_and_construct_flow(
                path_flow_u_l, path_flow_v_l, res_x,
                res_y, im_cnt: im_cnt, t_idx: get_t_idx(), from_path: true);

            Transform display = canvas.transform.Find("Choose_panel").Find("maps").Find("u_panel");
            string time_flow_name_u = null;
            List<List<float>> flow_mat_norm = norm_mat(flow_mat_0);
            save_flow_u(flow_mat_norm, (path_flow_v_l + "matched.png"));
            display_from_path(display, time_flow_name_u, input: mat2tex(flow_mat_norm));
        }

        is_started = false;
        is_tv_running = false;
    }

    /// <summary>
    /// Starts the flow computation for two images.
    /// </summary>
    /// <param name="path0">First image folder or file.</param>
    /// <param name="path1">Second image folder or file.</param>
    /// <param name="render_act_idx">Index of the render action.</param>
    /// <param name="d_cam">Camera offset (0 = time pair, 1 = stereo).</param>
    /// <param name="with_dt">True for time-series analysis.</param>
    /// <param name="exp_label">Experiment label.</param>
    /// <param name="save_path">Output path.</param>
    /// <param name="im0_path">First image.</param>
    /// <param name="im1_path">Second image.</param>
    /// <param name="pars">Parameters.</param>
    /// <param name="series_idx">Index of the series.</param>
    public void cv_for_paths(string path0, string path1, int render_act_idx, int d_cam = 0, 
        bool with_dt = false, string exp_label = null, string save_path = null,
        string im0_path = null, string im1_path = null, Params pars = null, int series_idx = -1)
    {
        StartCoroutine(cv_for_paths_coroutine(path0, path1, render_act_idx, d_cam, with_dt, exp_label, save_path, im0_path, im1_path, pars, series_idx));
    }

    /// <summary>
    /// Coroutine: multi-scale TV-L1/TGV-L1 flow computation of two images and saving of the result.
    /// </summary>
    /// <param name="path0">First image folder or file.</param>
    /// <param name="path1">Second image folder or file.</param>
    /// <param name="render_act_idx">Index of the render action.</param>
    /// <param name="d_cam">Camera offset (0 = time pair, 1 = stereo).</param>
    /// <param name="with_dt">True for time-series analysis.</param>
    /// <param name="exp_label">Experiment label.</param>
    /// <param name="save_path">Output path.</param>
    /// <param name="im0_path">First image.</param>
    /// <param name="im1_path">Second image.</param>
    /// <param name="pars">Parameters.</param>
    /// <param name="series_idx">Index of the series.</param>
    /// <returns>Coroutine enumerator.</returns>
    public IEnumerator cv_for_paths_coroutine(string path0, string path1, int render_act_idx, int d_cam = 0, 
        bool with_dt = false, string exp_label = null, string save_path = null,
        string im0_path = null, string im1_path = null, Params pars = null, int series_idx = -1)
    {   
        (PAR_DEFAULT_LAMBDA, PAR_DEFAULT_THETA, PAR_DEFAULT_NSCALES, PAR_DEFAULT_NWARPS) = set_up_pars(
            with_dt, exp_label);

        int t_steps = 1;
        int t_idx_start = 0;
        int cam_idx_0 = 0;
        int cam_idx_1 = cam_idx_0 + d_cam;
        int max_idx = with_dt ? blade_idxs.Count - 2 : blade_idxs.Count;

        (string proj_dir, _) = find_dir_for_params(pars);

        for (int t_idx_i = 0; t_idx_i < max_idx; t_idx_i++)
        {
            (int t_idx_0, int t_idx_1, int t_idx_2) = fetch_t_idxs(with_dt, t_idx_i);

            string info = "render_act: " + render_act_idx.ToString() + " / " + render_acts.Count.ToString() +
                "; t_idx: " + (t_idx_i - t_idx_start).ToString() + " / " + t_steps.ToString();
            Debug.Log(info);

            string im_t_name = (path0 != null) ? path0 : "";
            string im_t_next_name = (path1 != null) ? path1 : "";
            string im_t_next_next_name = (path0 != null) ? path0 : "";

            // info (paul): output paths
            string time_flow_name_u = proj_dir +
                "/time_flow_u/time_flow_u_" + series_idx.ToString() + "_r" +
                 get_render_res().ToString() + ".png";
            string time_flow_name_v = proj_dir +
                "/time_flow_v/time_flow_v_" + series_idx.ToString() + "_r" +
                 get_render_res().ToString() + ".png";
            string strain_vx_name = proj_dir +
                "/time_flow_v/strain_vx_" + series_idx.ToString() + "_r" +
                get_render_res().ToString() + ".png";
            string strain_vy_name = proj_dir +
                "/time_flow_v/strain_vy_" + series_idx.ToString() + "_r" +
                 get_render_res().ToString() + ".png";
            string heights_name = proj_dir +
                "/heights/heights_" + series_idx.ToString() + "_r" +
                 get_render_res().ToString() + ".png";

            string min_max_u_file = proj_dir +
                "/min_max_u_" + series_idx.ToString() + "_r" + get_render_res().ToString() + ".txt";
            string min_max_v_file = proj_dir +
                "/min_max_v_" + series_idx.ToString() + "_r" + get_render_res().ToString() + ".txt";
            string min_max_heights_file = proj_dir +
                "/min_max_heights_" + series_idx.ToString() + ".txt";

            bool next_exists = File.Exists(im_t_next_name);
            bool next_next_exists = File.Exists(im_t_next_next_name);

            im_dressed imd_0 = manage_read_im(im_t_name);
            im_dressed imd_1 = manage_read_im(im_t_next_name);
            im_dressed imd_2 = manage_read_im(im_t_next_next_name);

            List<List<float>> I0 = new List<List<float>> { imd_0.im_vec, imd_0.im_vec };
            List<List<float>> I1 = new List<List<float>> { imd_1.im_vec, imd_1.im_vec };

            (List<List<List<float>>> u_mat, List<List<List<float>>> v_mat) = (null, null);
            List<float> min_val_u = new List<float>() { };
            List<float> min_val_v = new List<float>() { };
            List<float> max_val_u = new List<float>() { };
            List<float> max_val_v = new List<float>() { };

            int n_x = imd_0.width;
            int n_y = imd_1.height;

            if (next_exists && !break_now)
            {
                // Run compute_ims on background thread so main thread remains 100% interactive
                var computeTask = Task.Run(() => compute_ims(I0, I1, n_x, n_y, scale_fac: 1, with_dt: with_dt));
                while (!computeTask.IsCompleted)
                {
                    yield return null;
                }
                (u_mat, v_mat, min_val_u, min_val_v, max_val_u, max_val_v) = computeTask.Result;
            }

            if (break_now || u_mat == null || v_mat == null)
            {
                yield break;
            }

            save_flow_u(u_mat[0], time_flow_name_u);
            save_flow_v(v_mat[0], time_flow_name_v);
            
            List<List<float>> v_x = derive_x(v_mat[0], zeros_like(v_mat[0]));
            List<List<float>> v_y = derive_y(v_mat[0], zeros_like(v_mat[0]));

            float v_x_min = find_min(lists_to_floats2(v_x));
            float v_y_min = find_min(lists_to_floats2(v_y));
            float v_x_max = find_max(lists_to_floats2(v_x));
            float v_y_max = find_max(lists_to_floats2(v_y));

            save_flow_u(v_x, strain_vx_name);
            save_flow_v(v_y, strain_vy_name);

            manage_write_max_min(min_max_u_file, min_val_u[0], max_val_u[0]);
            manage_write_max_min(min_max_v_file, min_val_v[0], max_val_v[0]);

            if (save_path != null)
            {
                save_flow_u(u_mat[0], save_path);
            }

            // info (paul): display flow
            Transform display = canvas.transform.Find("Choose_panel").Find("maps").Find("u_panel");
            Transform display_im0 = canvas.transform.Find("Choose_panel").Find("maps").Find("im0_panel");
            Transform display_im1 = canvas.transform.Find("Choose_panel").Find("maps").Find("im1_panel");

            display_from_path(display, time_flow_name_u, input: mat2tex(v_mat[0]));

            List<float> I0_listsA = scale_by(I0[0], factor: 1f / (0.33f * 256f));
            List<float> I1_listsA = scale_by(I1[0], factor: 1f / (0.33f * 256f));
            int res_l = (int)(Mathf.Sqrt(I0_listsA.Count));
            List<List<float>> I0_lists = floats2_to_lists(floats2matrix(I0_listsA.ToArray(), res_x: res_l, res_y: res_l));
            List<List<float>> I1_lists = floats2_to_lists(floats2matrix(I1_listsA.ToArray(), res_x: res_l, res_y: res_l));
            display_from_path(display_im0, time_flow_name_u, input: mat2tex(I0_lists));
            display_from_path(display_im1, time_flow_name_u, input: mat2tex(I1_lists));
            if (save_path != null)
            {
                save_flow_u(I0_lists, im0_path);
            }
            if (save_path != null)
            {
                save_flow_u(I1_lists, im1_path);
            }

            // info (paul): save as floats if requested
            if (!with_dt)
            {
                manage_save_cv_heights(heights_name, min_max_u_file, min_max_v_file,
                    min_val_u[0], min_val_v[0], n_x, n_y, max_val_u[0], max_val_v[0],
                    u_mat[0], v_mat[0], I0, I1);
            }
        }
    }

    //27092026 proj_dir_override: Ergebnisordner direkt vorgeben (Parameterstudie rechnet nur TV neu auf den
    //  vorhandenen Bildern; find_dir_for_params braucht sonst die Render-Parameter des Laufs)
    /// <summary>
    /// Main flow computation of a render action: loads the image pairs, computes the flow over all scales, and saves the maps (PNG and .f32).
    /// </summary>
    /// <param name="render_act_idx">Index of the render action.</param>
    /// <param name="d_cam">Camera offset (0 = time pair, 1 = stereo).</param>
    /// <param name="with_dt">True for time-series analysis.</param>
    /// <param name="exp_label">Experiment label.</param>
    /// <param name="pars">Parameters.</param>
    /// <param name="proj_dir_override">Result folder given directly (parameter study).</param>
    /// <returns>Task.</returns>
    public async Task cv_main_async(int render_act_idx, int d_cam = 0, bool with_dt = false, string exp_label = null,
        Params pars = null, string proj_dir_override = null)
    {
        // info (paul): not used by "start" button

        (PAR_DEFAULT_LAMBDA, PAR_DEFAULT_THETA, PAR_DEFAULT_NSCALES, PAR_DEFAULT_NWARPS)  = set_up_pars(
            with_dt, exp_label);

        // perh. TODO: check, what is actually "right" and "left" (but I think it does not really matter)
        int t_steps = 1;//Bc 7;// 3;//19092024 3;//5//27072024 100;
        int t_idx_start = 0;//19092024 0;
        // info (paul): t_steps = blade_idx_max - blade_idx_min would be ideal;

        int cam_idx_0 = 0;
        int cam_idx_1 = cam_idx_0 + d_cam;

        //22102024 for (int t_idx_i = t_idx_start; t_idx_i < t_idx_start + t_steps; t_idx_i++)
        //04042025 int max_idx = with_dt ? blade_idxs.Count - 1 : blade_idxs.Count;
        int max_idx = with_dt ? blade_idxs.Count - 2 : blade_idxs.Count;

        //21022025 string proj_dir = path_dic + remove_dots(get_experiment());
        (string proj_dir, _) = proj_dir_override != null ? (proj_dir_override, null) : find_dir_for_params(pars);

        for (int t_idx_i = 0; t_idx_i < max_idx; t_idx_i++)//21112024 blade_idxs.Count - 1
        {
            (int t_idx_0, int t_idx_1, int t_idx_2) = fetch_t_idxs(with_dt, t_idx_i);

            string info = "render_act: " + render_act_idx.ToString() + " / " + render_acts.Count.ToString() +
                "; t_idx: " + (t_idx_i - t_idx_start).ToString() + " / " + t_steps.ToString();
            Debug.Log(info);

            string im_t_name = proj_dir +
                 "/cam_" + cam_idx_0.ToString() + "/uv/" + category + "im_" + t_idx_0.ToString() + "_r" + 
                 get_render_res().ToString() + ".png";//_256.png
            string im_t_next_name = proj_dir +
                 "/cam_" + cam_idx_1.ToString() + "/uv/" + category + "im_" + t_idx_1.ToString() + "_r" + 
                 get_render_res().ToString() + ".png";//_256.png
            string im_t_next_next_name = proj_dir +
                 "/cam_" + cam_idx_1.ToString() + "/uv/" + category + "im_" + t_idx_2.ToString() + "_r" + 
                 get_render_res().ToString() + ".png";//_256.png
            
            //23092026 Bugfix: Vom Nutzer gewaehlte Bildpfade (Choose_panel "Confirm", Explorer) nur
            //  verwenden, wenn die Dateien existieren. Leere Felder ergaben sonst "Path is empty"
            //  und die Analyse brach ab -> dann die gerenderten Bilder (oben) nehmen.
            if (im_path_0 != null)
            {
                if (usable_image(im_path_0) && usable_image(im_path_1))
                {
                    im_t_name = im_path_0;
                    im_t_next_name = im_path_1;
                    im_t_next_next_name = im_path_0; // perh. TODO: somehow change that later
                }
                else
                {
                    Debug.LogWarning("Gewaehlte Bildpfade ungueltig ('" + im_path_0 + "', '" + im_path_1
                        + "') -> verwende die gerenderten Bilder.");
                }
            }

            List<string> im_paths = this.get_im_paths();
            if (im_paths.Count >= max_idx && im_paths.Count > t_idx_i+2)
            {
                if (usable_image(im_paths[t_idx_i]) && usable_image(im_paths[t_idx_i + 1])
                    && usable_image(im_paths[t_idx_i + 2]))
                {
                    im_t_name = im_paths[t_idx_i];
                    im_t_next_name = im_paths[t_idx_i+1];
                    im_t_next_next_name = im_paths[t_idx_i+2];
                }
                else
                {
                    Debug.LogWarning("Bildliste (Choose_panel/Explorer) enthaelt leere oder fehlende Pfade ('"
                        + string.Join("', '", im_paths) + "') -> verwende die gerenderten Bilder.");
                }
            }

            if (!File.Exists(im_t_name) || !File.Exists(im_t_next_name) || !File.Exists(im_t_next_next_name))
            {
                string missing_im = !File.Exists(im_t_name) ? im_t_name
                    : (!File.Exists(im_t_next_name) ? im_t_next_name : im_t_next_next_name);
                string msg = "TV-Analyse: Eingabebild fehlt (" + missing_im
                    + "). Wurde mit dieser Aufloesung (r" + get_render_res() + ") gerendert?";
                Debug.LogError(msg);
                ExperimentImageGallery.SetResultsText(msg);
                ExperimentImageGallery.ShowResultsWindow();
                return;
            }

            // info (paul): output paths
            //20092026 Bugfix: Flussfelder wurden nach Blade-ID (t_idx_0, z.B. 3 bei
            //blade_idxs {3,15,3}) benannt, die Anzeige (find_accum_flow/match_all_to_start)
            //und die Ground-Truth-Dateien (0__d_xs...) arbeiten aber mit der Position
            //t_idx_i (0,1,2,...). Dadurch zeigte "value" alte Dateien mit Index 0 statt
            //des aktuellen Ergebnisses. Jetzt konsistent nach Position benennen.
            string time_flow_name_u = proj_dir +
                "/time_flow_u/time_flow_u_" + t_idx_i.ToString() + "_r" +
                 get_render_res().ToString() + ".png";
            string time_flow_name_v = proj_dir +
                "/time_flow_v/time_flow_v_" + t_idx_i.ToString() + "_r" +
                 get_render_res().ToString() + ".png";
            // string strain_vx_name = proj_dir +
            //  "/time_flow_u/strain_vx_" + t_idx_0.ToString() + "_r" +
            //     get_render_res().ToString() + ".png";
            // string strain_vy_name = proj_dir +
            //    "/time_flow_v/strain_vy_" + t_idx_0.ToString() + "_r" +
            //      get_render_res().ToString() + ".png";
            string heights_name = proj_dir +
                "/heights/heights_" + t_idx_0.ToString() + "_r" +
                 get_render_res().ToString() + ".png";
            
            string min_max_u_file = proj_dir +
                "/min_max_u_" + t_idx_i.ToString() + "_r" + get_render_res().ToString() + ".txt";
            string min_max_v_file = proj_dir +
                "/min_max_v_" + t_idx_i.ToString() + "_r" + get_render_res().ToString() + ".txt";
            string min_max_heights_file = proj_dir +
                "/min_max_heights_" + t_idx_0.ToString() + ".txt";

            bool next_exists = File.Exists(im_t_next_name);
            bool next_next_exists = File.Exists(im_t_next_next_name);

            im_dressed imd_0 = manage_read_im(im_t_name);
            im_dressed imd_1 = manage_read_im(im_t_next_name);
            im_dressed imd_2 = manage_read_im(im_t_next_next_name);
            
            List<List<float>> I0 = new List<List<float>>{imd_0.im_vec, imd_0.im_vec};//03042025 imd_1.im_vec};
            List<List<float>> I1 = new List<List<float>>{imd_1.im_vec, imd_1.im_vec};//03042025 imd_2.im_vec};

            (List<List<List<float>>> u_mat, List<List<List<float>>> v_mat) = (null, null);
            List<float> min_val_u = new List<float>(){};
            List<float> min_val_v = new List<float>(){};
            List<float> max_val_u = new List<float>(){};
            List<float> max_val_v = new List<float>(){};
            
            int n_x = imd_0.width;
            int n_y = imd_1.height;

            if (next_exists && !break_now)
            {
                bool with_plot = true; // what was this needed for?
                (u_mat, v_mat, min_val_u, min_val_v, max_val_u, max_val_v) = await Task.Run(() =>
                    compute_ims(I0, I1, n_x, n_y, scale_fac: 1, with_dt: with_dt));
            }
            
            save_flow_u(u_mat[0], time_flow_name_u);
            save_flow_v(v_mat[0], time_flow_name_v);
            manage_write_max_min(min_max_u_file, min_val_u[0], max_val_u[0]);
            manage_write_max_min(min_max_v_file, min_val_v[0], max_val_v[0]);

            // info (paul): getting the strain from heights_chosen or xy-flow in flow_tex
            //List<List<float>> flow_mat = manage_strain(v_mat[0], heights, stream_z);
            //List<List<float>> v_x = derive_x(v_mat[0], zeros_like(v_mat[0]));
            //List<List<float>> v_y = derive_y(v_mat[0], zeros_like(v_mat[0]));
            //save_flow_u(v_x, strain_vx_name);
            //save_flow_v(v_y, strain_vy_name);

            // info (paul): display flow
            Transform display = canvas.transform.Find("Choose_panel").Find("maps").Find("u_panel");
            Transform display_im0 = canvas.transform.Find("Choose_panel").Find("maps").Find("im0_panel");
            Transform display_im1 = canvas.transform.Find("Choose_panel").Find("maps").Find("im1_panel");

            display_from_path(display, time_flow_name_u, input: mat2tex(u_mat[0]));

            List<float> I0_listsA = scale_by(I0[0], factor: 1f/(0.33f * 256f));
            List<float> I1_listsA = scale_by(I1[0], factor: 1f/(0.33f * 256f));
            int res_l = (int)(Mathf.Sqrt(I0_listsA.Count));
            List<List<float>> I0_lists = floats2_to_lists(floats2matrix(I0_listsA.ToArray(), res_x: res_l, res_y: res_l));
            List<List<float>> I1_lists = floats2_to_lists(floats2matrix(I0_listsA.ToArray(), res_x: res_l, res_y: res_l));
            display_from_path(display_im0, time_flow_name_u, input: mat2tex(I0_lists));
            display_from_path(display_im1, time_flow_name_u, input: mat2tex(I1_lists));

            // info (paul): save as floats if requested
            if (!with_dt)
            {
                manage_save_cv_heights(heights_name, min_max_u_file, min_max_v_file, 
                    min_val_u[0], min_val_v[0], n_x, n_y, max_val_u[0], max_val_v[0],
                    u_mat[0], v_mat[0], I0, I1);
            }
        }
    }

    /// <summary>
    /// Reads numbers (one per line) from a text file.
    /// </summary>
    /// <param name="I0_path">File.</param>
    /// <returns>Values.</returns>
    public List<float> txt2floats(string I0_path)
    {
        using StreamReader reader = new(I0_path);
        string text = reader.ReadToEnd();
        string[] nums = text.Split("\r\n");
        List<float> floats = new List<float>();

        for (int i = 0; i < nums.Length; i++)
        {
            string nums_i = nums[i];
            if (nums_i != "")
            {
                floats.Add(float.Parse(nums_i));
            }
        }

        return floats;
    }

    (List<List<List<float>>>, List<List<List<float>>>, List<float>, List<float>,
        List<float>, List<float>) compute_ims(List<List<float>> I0,
        List<List<float>> I1, int n_x, int n_y, float scale_fac = -1f,
        bool with_dt = false)
    {
        string outfile = PAR_DEFAULT_OUTFLOW;
        int nproc = PAR_DEFAULT_NPROC;
        float tau = (float)PAR_DEFAULT_TAU;
        float lambda = (float)PAR_DEFAULT_LAMBDA;
        float theta = (float)PAR_DEFAULT_THETA;
        int nscales = PAR_DEFAULT_NSCALES;
        float zfactor = (float)PAR_DEFAULT_ZFACTOR;
        int nwarps = PAR_DEFAULT_NWARPS;//Bc 20;//20092024 (t_idx_l == -1 || t_idx_l == 0) ? PAR_DEFAULT_NWARPS: 2;
        float epsilon = (float)PAR_DEFAULT_EPSILON;
        int verbose = PAR_DEFAULT_VERBOSE;

        (List<List<List<float>>> u_mat, List<List<List<float>>> v_mat) = (null, null);
        List<float> min_val_u = new List<float>();
        List<float> min_val_v = new List<float>();
        List<float> max_val_u = new List<float>();
        List<float> max_val_v = new List<float>();
        
        // info (paul): compute the optical flow; note that we switched nx and ny as input 
        //		arguments, which seems to be necessary and right to arrange the pixels of non-quadratic
        //		images correctly.
        (u_mat, v_mat, min_val_u, min_val_v, max_val_u, max_val_v) = find_displ(I0, I1, n_x, n_y, tau,
            lambda, theta, nscales, zfactor, nwarps, epsilon,
            (verbose > 0), scale_fac: scale_fac, with_dt: with_dt);

        return (u_mat, v_mat, min_val_u, min_val_v, max_val_u, max_val_v);
    }
    /// <summary>
    /// Creates the folder of an output file if needed.
    /// </summary>
    /// <param name="out_file_name_u">Output file.</param>
    public void arrange_dir(string out_file_name_u)
    {
        int index = out_file_name_u.LastIndexOf("/");
        string dir_path = out_file_name_u.Substring(0, index);
        if (!File.Exists(dir_path))
        {
            Directory.CreateDirectory(dir_path);
        }
    }
    (List<List<List<float>>>, List<List<List<float>>>, List<float>,
        List<float>, List<float>,
        List<float>) find_displ(List<List<float>> I0,
        List<List<float>> I1, int nx, int ny, float tau,
        float lambda, float theta, int nscales, float zfactor,
        int nwarps, float epsilon, bool verbose, 
        float scale_fac = -1f, bool with_dt = false)
    {
        if (break_now)
        {
            //return (null, null, float.NaN, float.NaN, float.NaN, float.NaN);
            return (null, null, null, null, null, null);
        }

        //Set the number of scales according to the size of the
        //images.  The value N is computed to assure that the smaller
        //images of the pyramid don't have a size smaller than 16x16
        float N = (float)(1 + Math.Log(hypot((double)nx, (double)ny) / 16d) / Math.Log(1 / zfactor));
        if (N < nscales)
            nscales = (int)N;

        if (verbose)
            Debug.Log("some message");

        //allocate memory for the flow
        //09032025 List<float> u = (new float[2 * nx * ny]).ToList();
        //09032025 List<float> v = (new float[2 * nx * ny]).ToList();//27072024 u + nx * ny;//probably this is some kind of extension

        List<List<float>> u = new List<List<float>>(){(new float[2 * nx * ny]).ToList(), (new float[2 * nx * ny]).ToList()};
        List<List<float>> v = new List<List<float>>(){(new float[2 * nx * ny]).ToList(), (new float[2 * nx * ny]).ToList()};

        (u, v) = Dual_TVL1_optic_flow_multiscale(
            I0, I1, u, v, ny, nx, tau, lambda, theta,
            nscales, zfactor, nwarps, epsilon, verbose, with_dt: with_dt
        );

        //29092026 Warnung statt stillem "Fluss = 0": nicht-endliche Werte im TV-Ergebnis zaehlen
        int non_finite = 0;
        foreach (List<float> comp in new[] { u[0], v[0] })
            foreach (float val in comp)
                if (float.IsNaN(val) || float.IsInfinity(val)) non_finite++;
        if (non_finite > 0)
            Debug.LogWarning("TV-Ergebnis enthaelt " + non_finite + " NaN/Inf-Werte (" + get_experiment()
                + ") - Flussfeld unbrauchbar.");

        float scale_fac_used = scale_fac;
        if (scale_fac < 0f)
        {
            scale_fac_used = 100f;
        }

        // info (paul): min_max u
        List<float> min_val_u = new List<float>();
        List<float> max_val_u = new List<float>();
        for (int kt = 0; kt < u.Count; kt++)
        {
            (float min_val_u_l, float max_val_u_l) = find_min_max(u[kt]);
            min_val_u.Add(min_val_u_l);
            max_val_u.Add(max_val_u_l);
        }
        //21032025 manage_write_max_min(min_max_u_file, min_val_u, max_val_u);

        // info (paul): min_max v
        List<float> min_val_v = new List<float>();
        List<float> max_val_v = new List<float>();
        for (int kt = 0; kt < v.Count; kt++)
        {
            (float min_val_v_l, float max_val_v_l) = find_min_max(v[kt]);
            min_val_v.Add(min_val_v_l);
            max_val_v.Add(max_val_v_l);
        }

        //21032025 manage_write_max_min(min_max_v_file, min_val_v, max_val_v);

        List<List<float>> u_normed = norm_floatss(u, nx, ny);//02052024
        List<List<float>> v_normed = norm_floatss(v, nx, ny);

        // info (paul): save the optical flow; I think for saving we need to scale 
        //		the intensity by 256 or 100 or so, to see the same thing, that we see 
        //		in the plot panel in "show_floats", otherwise the .png-file looks 
        //		in the plot panel in "show_floats", otherwise the .png-file looks 
        //		more or less just black

        List<List<List<float>>> u_listss = new List<List<List<float>>>();
        List<List<List<float>>> v_listss = new List<List<List<float>>>();

        for (int kt = 0; kt < u_normed.Count; kt++)
        {
            float[][] u_mat = floats2matrix(u_normed[kt].ToArray(), nx, ny, with_switch_dims: false);
            float[][] v_mat = floats2matrix(v_normed[kt].ToArray(), nx, ny, with_switch_dims: false);

            List<List<float>> u_lists = floats2_to_lists(u_mat);
            List<List<float>> v_lists = floats2_to_lists(v_mat);

            u_listss.Add(u_lists);
            v_listss.Add(v_lists);
        }

        return (u_listss, v_listss, min_val_u, min_val_v, max_val_u, max_val_v);
        
    }

    /// <summary>
    /// Saves the stereo result as height data (maps and value ranges).
    /// </summary>
    /// <param name="heights_path">Output path.</param>
    /// <param name="min_max_u_file">Range file of u.</param>
    /// <param name="min_max_v_file">Range file of v.</param>
    /// <param name="min_val_u">Minimum of u.</param>
    /// <param name="min_val_v">Minimum of v.</param>
    /// <param name="nx">Width.</param>
    /// <param name="ny">Height.</param>
    /// <param name="max_val_u">Maximum of u.</param>
    /// <param name="max_val_v">Maximum of v.</param>
    /// <param name="u_mat">u map.</param>
    /// <param name="v_mat">v map.</param>
    /// <param name="I0">First image.</param>
    /// <param name="I1">Second image.</param>
    public void manage_save_cv_heights(string heights_path, string min_max_u_file,
        string min_max_v_file, float min_val_u, float min_val_v, int nx, int ny,
        float max_val_u, float max_val_v, List<List<float>> u_mat,
        List<List<float>> v_mat, List<List<float>> I0, List<List<float>> I1
        )
    {
            //application.persistentDataPath
            arrange_dir(heights_path);
            
            string min_max_u_file_heights = min_max_u_file.Replace(".txt", "_heights.txt");
            string min_max_v_file_heights = min_max_v_file.Replace(".txt", "_heights.txt");
            
            manage_write_max_min(min_max_u_file_heights, min_val_u, max_val_u);
            manage_write_max_min(min_max_v_file_heights, min_val_v, max_val_v);

            // info (paul): the horizontal u-displacement should be the difference
            if (category != "muc")
            {
                save_floats2(lists_to_floats2(u_mat), full_path: heights_path);
            }
            else
            {
                save_floats2(lists_to_floats2(u_mat), full_path: heights_path);
            }

            // info (paul): save normal png file
            List<List<float>> u_lists = u_mat;//floats2_to_lists(u_mat);
            Texture2D tex_u = mat2tex(u_lists, with_switch_dims: false);
            arrange_dir(heights_path.Replace(".png", "_p.png"));
            System.IO.File.WriteAllBytes(heights_path.Replace(".png", "_p.png"), tex_u.EncodeToPNG());

            // info (paul): the horizontal v-displacement should be the difference
            //18032025 save_floats2(v_mat, full_path: heights_path); - seems to overwrite our previous png-file

            // info (paul): save normal png file
            List<List<float>> v_lists = v_mat;//floats2_to_lists(v_mat);
            Texture2D tex_v = mat2tex(v_lists, with_switch_dims: false);
            arrange_dir(heights_path.Replace(".png", "_v.png"));
            System.IO.File.WriteAllBytes(heights_path.Replace(".png", "_v.png"), tex_v.EncodeToPNG());

            // info (paul): save I0 and I1
            List<float> I0_normed = norm_floats(I0[0], nx, ny);
            float[][] I0_ar = floats2matrix(I0_normed.ToArray(), nx, ny, with_switch_dims: false);
            List<List<float>> I0_lists = floats2_to_lists(I0_ar);
            Texture2D tex_I0 = mat2tex(I0_lists, with_switch_dims: false);
            arrange_dir(heights_path.Replace(".png", "_I0.png"));
            System.IO.File.WriteAllBytes(heights_path.Replace(".png", "_I0.png"), tex_I0.EncodeToPNG());
            
            List<float> I1_normed = norm_floats(I1[0], nx, ny);
            float[][] I1_ar = floats2matrix(I1_normed.ToArray(), nx, ny, with_switch_dims: false);
            List<List<float>> I1_lists = floats2_to_lists(I1_ar);
            Texture2D tex_I1 = mat2tex(I1_lists, with_switch_dims: false);
            arrange_dir(heights_path.Replace(".png", "_I1.png"));
            System.IO.File.WriteAllBytes(heights_path.Replace(".png", "_I1.png"), tex_I1.EncodeToPNG());

            // info (paul): max/min
            manage_write_max_min(min_max_u_file, min_val_u, max_val_u);
            manage_write_max_min(min_max_v_file, min_val_v, max_val_v);

            //12102024 save_floats2(u_mat, file_name: "heights_tv");
    }

    /// <summary>
    /// Saves the u map as PNG.
    /// </summary>
    /// <param name="u_mat">u map.</param>
    /// <param name="out_file_name_u">Output file.</param>
    public void save_flow_u(List<List<float>> u_mat, string out_file_name_u)
    {
        List<List<float>> u_lists = u_mat;//floats2_to_lists(u_mat);
        Texture2D tex_u = mat2tex(u_lists, with_switch_dims: false);
        arrange_dir(out_file_name_u);
        System.IO.File.WriteAllBytes(out_file_name_u, tex_u.EncodeToPNG());
        write_flow_float_copy(u_lists, out_file_name_u); //27092026 verlustfreie Kopie (PNG nur 8 Bit)
        if (analysis_sweep_running() && Path.GetFileName(out_file_name_u).StartsWith("time_flow_"))
            ExperimentImageGallery.AddRenderedImage(out_file_name_u);
    }
    /// <summary>
    /// Saves the v map as PNG.
    /// </summary>
    /// <param name="v_mat">v map.</param>
    /// <param name="out_file_name_v">Output file.</param>
    public void save_flow_v(List<List<float>> v_mat, string out_file_name_v)
    {
        List<List<float>> v_lists = v_mat;//floats2_to_lists(v_mat);
        Texture2D tex_v = mat2tex(v_lists, with_switch_dims: false);
        arrange_dir(out_file_name_v);
        System.IO.File.WriteAllBytes(out_file_name_v, tex_v.EncodeToPNG());
        write_flow_float_copy(v_lists, out_file_name_v); //27092026 verlustfreie Kopie (PNG nur 8 Bit)
        if (analysis_sweep_running() && Path.GetFileName(out_file_name_v).StartsWith("time_flow_"))
            ExperimentImageGallery.AddRenderedImage(out_file_name_v);
    }

    /// <summary>
    /// Deep copy of a nested list.
    /// </summary>
    /// <param name="u_input">Nested list.</param>
    /// <returns>Copy.</returns>
    public List<List<float>> copy_floats(List<List<float>> u_input)
    {
        List<List<float>> floats = new List<List<float>>();

        for (int i = 0; i < u_input.Count; i++)
        {
            List<float> floats_1d = new List<float>();
            for (int j = 0; j < u_input[i].Count; j++)
            {
                floats_1d.Add(u_input[i][j]);
            }
            floats.Add(floats_1d);
        }
        return floats;

    }
    /// <summary>
    /// Copy of a list.
    /// </summary>
    /// <param name="u_input">List.</param>
    /// <returns>Copy.</returns>
    public List<float> copy_floats(List<float> u_input)
    {
        List<float> floats = new List<float>();

        for (int i = 0; i < u_input.Count; i++)
        {
            floats.Add(u_input[i]);
        }
        return floats;

    }
    /// <summary>
    /// Normalises a list to positive values and scales it.
    /// </summary>
    /// <param name="u">Values.</param>
    /// <param name="n_x">Width.</param>
    /// <param name="n_y">Height.</param>
    /// <param name="scale">Factor.</param>
    /// <param name="offset">Offset.</param>
    /// <returns>Scaled values.</returns>
    public List<float> scale_floats(List<float> u, int n_x, int n_y, float scale, float offset)
    {
        float min_val = find_min(u, n_x, n_y);
        float max_val = find_max(u, n_x, n_y);

        // info (paul): norm, so that everything is positive
        for (int j = 0; j < n_x * n_y; j++)
        {
            u[j] = scale * u[j] + offset;
            // perh. TODO: do a full normalization
        }

        return u;
    }

    /// <summary>
    /// Scales and shifts a matrix in place.
    /// </summary>
    /// <param name="mat">Matrix.</param>
    /// <param name="factor">Factor.</param>
    /// <param name="offset">Offset.</param>
    /// <returns>The matrix.</returns>
    public List<List<float>> scale_mat(List<List<float>> mat, float factor, float offset)
    {
        for (int i = 0; i < mat.Count; i++)
        {
            for (int j = 0; j < mat[0].Count; j++)
            {
                mat[i][j] += offset;
                mat[i][j] *= factor;
            }
        }

        return mat;
    }

    /// <summary>
    /// Returns the frame distance of the time comparison.
    /// </summary>
    /// <returns>Frame distance.</returns>
    public int get_dt_compare()
    {
        return dt_compare;
    }
    /// <summary>
    /// Sets the frame distance of the time comparison.
    /// </summary>
    /// <param name="input">Frame distance.</param>
    public void set_dt_compare(int input)
    {
        dt_compare = input;
    }
    /// <summary>
    /// First half of a list.
    /// </summary>
    /// <param name="u_input">List.</param>
    /// <returns>First half.</returns>
    public List<float> first_half_of(List<float> u_input)
    {
        List<float> u_half = new List<float>();

        for (int i = 0; i < u_input.Count / 2; i++)
        {
            u_half.Add(u_input[i]);
        }

        return u_half;
    }
    /// <summary>
    /// Debugging helper: writes an array as image.
    /// </summary>
    /// <param name="u_input">Values.</param>
    /// <param name="scale">Factor.</param>
    public void write_for_debug(float[] u_input, float scale)
    {
        write_for_debug(u_input.ToList(), with_norm: false, scale: scale, offset: 0f);
    }
    /// <summary>
    /// Debugging helper: writes a list as image.
    /// </summary>
    /// <param name="u_input">Values.</param>
    /// <param name="scale">Factor.</param>
    public void write_for_debug(List<float> u_input, float scale)
    {
        write_for_debug(u_input, with_norm: false, scale: scale, offset: 0f);
    }
    /// <summary>
    /// Debugging helper: writes a list as image.
    /// </summary>
    /// <param name="u_input">Values.</param>
    public void write_for_debug(List<float> u_input)
    {
        write_for_debug(u_input, with_norm: false, scale: 1f, offset: 0f);
    }
    /// <summary>
    /// Debugging helper: writes a list (one or two images) as PNG.
    /// </summary>
    /// <param name="u_input">Values.</param>
    /// <param name="with_norm">True to normalise.</param>
    /// <param name="scale">Factor.</param>
    /// <param name="offset">Offset.</param>
    public void write_for_debug(List<float> u_input, bool with_norm = false, float scale = 1f, float offset = 0f)
    {
        List<float> u = copy_floats(u_input);

        // info (paul): if this is in case that a vector contains 2 ims
        if (u_input.Count == 2 * 16 * 16 || u_input.Count == 2 * 128 * 128 || u_input.Count == 2 * 256 * 256 || u_input.Count == 2 * 512 * 512)//12122024 added 256
        {
            u_input = first_half_of(u_input);
        }

        //26072025 string out_file_name_u = path_dic + "exp_normal/time_flow_v/debug_im.png";
        string out_file_name_u = path_dic + "/output/debug_im.png";
        
        int nx = (int)(Mathf.Sqrt(u_input.Count));//128;
        int ny = (int)(Mathf.Sqrt(u_input.Count));//128;

        if (with_norm)
        {
            u = norm_floats(u, nx, ny);
        }

        u = scale_floats(u, nx, ny, scale, offset);

        float[][] u_mat = floats2matrix(u.ToArray(), nx, ny, with_switch_dims: false);
        List<List<float>> u_lists = floats2_to_lists(u_mat);
        Texture2D tex_u = mat2tex(u_lists, with_switch_dims: false);
        System.IO.File.WriteAllBytes(out_file_name_u, tex_u.EncodeToPNG());
    }
    /// <summary>
    /// Debugging helper: writes a matrix as PNG.
    /// </summary>
    /// <param name="u_lists">Matrix.</param>
    /// <param name="scale">Factor.</param>
    /// <param name="offset">Offset.</param>
    public void write_mat_for_debug(List<List<float>> u_lists, float scale = 1f, float offset = 0f)
    {
        List<List<float>> u_copy = copy_mat(u_lists);
        u_copy = scale_mat(u_copy, scale, offset);

        // info (paul): write png file
        //26072025 string out_file_name_u = path_dic + "exp_normal/time_flow_v/debug_im.png";
        string out_file_name_u = path_dic + "/output/debug_im.png";
        Texture2D tex_u = mat2tex(u_copy, with_switch_dims: false);
        System.IO.File.WriteAllBytes(out_file_name_u, tex_u.EncodeToPNG());

        // info (paul): write csv file
        //26072025 string fileName = path_dic + "exp_normal/time_flow_v/debug_im.txt";
        string fileName = path_dic + "/output/debug_im.txt";
        using (StreamWriter file = new StreamWriter(fileName))
        {
            for (int i = 0; i < u_lists.Count; i++)
            {
                for (int j = 0; j < u_lists[i].Count; j++)
                {
                    float elem = u_lists[i][j];
                    file.Write(elem + "\t");
                }
                file.Write(";\n");
            }
        }

    }
    /// <summary>
    /// Writes the value range of a map to a text file.
    /// </summary>
    /// <param name="out_file_name_u">File.</param>
    /// <param name="min_val">Minimum.</param>
    /// <param name="max_val">Maximum.</param>
    void manage_write_max_min(string out_file_name_u, float min_val, float max_val)
    {
        //FILE* fptr;
        //
        //fopen_s(&fptr, out_file_name_u, "w");
        //
        //if (fptr == NULL)
        //{
        //    printf("pointer is null");
        //    exit(0);
        //}
        //
        //fprintf(fptr, "%f %f", min_val, max_val);
        //fclose(fptr);


        string path = out_file_name_u;//27072024string.Join("", out_file_name_u);

        string text = min_val.ToString() + " " + max_val.ToString();

        StreamWriter writer = new StreamWriter(path, false);

        writer.Write(text);
        writer.Close();



    }

    /// <summary>
    /// Writes or appends text to a file.
    /// </summary>
    /// <param name="path">File.</param>
    /// <param name="text">Text.</param>
    /// <param name="mode">append or overwrite.</param>
    public void write_to_txt(string path, string text, string mode = "append")
    {
        List<string> lines = new List<string>();
        bool file_exists = File.Exists(path);
        if (mode == "append" && file_exists)
        {
            string[] lines_ar = System.IO.File.ReadAllLines(path);
            lines = lines_ar.ToList();
        }
        lines.Add(text);
        arrange_dir(path);

        //StreamWriter writer = new StreamWriter(path, false);
        //writer.Write(lines.ToArray());
        //writer.Close();

        System.IO.File.WriteAllLines(path, lines);
    }
    /**
     *
     * Function to compute the optical flow using multiple scales
     *
     **/
            (List<List<float>>, List<List<float>>) Dual_TVL1_optic_flow_multiscale(
            List<List<float>> I0,           // source image
            List<List<float>> I1,           // target image
            List<List<float>> u1,           // x component of the optical flow
            List<List<float>> u2,           // y component of the optical flow
            int nxx,     // image width
            int nyy,     // image height
            float tau,     // time step
            float lambda,  // weight parameter for the data term
            float theta,   // weight parameter for (u - v)?
            int nscales, // number of scales
            float zfactor, // factor for building the image piramid
            int warps,   // number of warpings per scale
            float epsilon, // tolerance for numerical convergence
            bool verbose,  // enable/disable the verbose mode
            bool with_dt)
    {
        //nscales = 1; - in case you want back to the easy nscales=1 time
        int size = nxx * nyy;

        //23092026 Laufzeitmessung CPU vs. GPU; GPU-Fehlerzustand gilt nur fuer einen Lauf
        tv_gpu_failed = false;
        tgv_failed = false; //27092026
        if (tv_use_tgv && !tv_use_gpu)
            Debug.LogWarning("TGV ist nur auf der GPU verfuegbar - Rechenweg steht auf CPU, es wird TV gerechnet.");
        tv_stat_iterations = 0;
        Stopwatch tv_watch = Stopwatch.StartNew();

        // allocate memory for the pyramid structure
        //26072024 float** I0s = (float**)xmalloc(nscales * sizeof(float*));
        //26072024 float** I1s = (float**)xmalloc(nscales * sizeof(float*));
        //26072024 float** u1s = (float**)xmalloc(nscales * sizeof(float*));
        //26072024 float** u2s = (float**)xmalloc(nscales * sizeof(float*));

        List<List<List<float>>> I0s = zeros_of_size(nscales, I0.Count, size); //(float)xmalloc(nscales * sizeof(float*));
        List<List<List<float>>> I1s = zeros_of_size(nscales, I1.Count, size); //(float**)xmalloc(nscales * sizeof(float*));
        List<List<List<float>>> u1s = zeros_of_size(nscales, u1.Count, size); //(float**)xmalloc(nscales * sizeof(float*));
        List<List<List<float>>> u2s = zeros_of_size(nscales, u2.Count, size); //(float**)xmalloc(nscales * sizeof(float*));

        List<int> nx = ints_of_size(nscales);
        List<int> ny = ints_of_size(nscales);

        //I0s[0] = (float*)xmalloc(size * sizeof(float));
        //I1s[0] = (float*)xmalloc(size * sizeof(float));
        I0s[0][0] = (new float[size]).ToList();
        I1s[0][0] = (new float[size]).ToList();

        u1s[0] = u1;
        u2s[0] = u2;
        nx[0] = nxx;
        ny[0] = nyy;

        // normalize the images between 0 and 255
        //for (int kt = 0; kt < I0s.Count; kt++)
        {
            // TODO: perh. better normalize over full stack instead
            //      of in a for-loop for every image independently 
            (I0s[0], I1s[0]) = image_normalization(
                I0, I1, I0s[0], I1s[0], size);
        }

        // pre-smooth the original images
        for (int kt = 0; kt < I0s.Count; kt++)
        {
            // TODO: perh. better normalize over full stack instead
            //      of in a for-loop for every image independently
            I0s[0] = gaussian(I0s[0], nx[0], ny[0], PRESMOOTHING_SIGMA);
            I1s[0] = gaussian(I1s[0], nx[0], ny[0], PRESMOOTHING_SIGMA);
        }

        // create the scales
        for (int s = 1; s < nscales; s++)
        {
            (nx[s], ny[s]) = zoom_size(nx[s - 1], ny[s - 1], zfactor);
            int sizes = nx[s] * ny[s];

            // allocate memory
            for (int kt = 0; kt < I0s[s].Count; kt++)
            {
                I0s[s][kt] = (new float[sizes]).ToList();
                I1s[s][kt] = (new float[sizes]).ToList();
            }
            u1s[s] = zeros_of_size(u1.Count, sizes);//09032025 (new float[sizes]).ToList();
            u2s[s] = zeros_of_size(u2.Count, sizes);//09032025 (new float[sizes]).ToList();

            // zoom in the images to create the pyramidal structure
            I0s[s] = zoom_out(I0s[s - 1], I0s[s], nx[s - 1], ny[s - 1], zfactor);//(I0s[s - 1], I0s[s])
            I1s[s] = zoom_out(I1s[s - 1], I1s[s], nx[s - 1], ny[s - 1], zfactor);
        }

        // Start TV progress bar
        start_tv_progress(nscales, nx, ny, warps, MAX_ITERATIONS);

        // initialize the flow at the coarsest scale
        for (int i = 0; i < nx[nscales - 1] * ny[nscales - 1]; i++)
        {
            for (int im_idx = 0; im_idx < u2s[nscales - 1].Count; im_idx++)
            {
                u1s[nscales - 1][im_idx][i] = u2s[nscales - 1][im_idx][i] = 0f;
            }
        }

        // pyramidal structure for computing the optical flow
        for (int s = nscales - 1; s >= 0; s--)
        {
            if (break_now)
            {
                break;
            }

            if (verbose)
            {
                Debug.Log("Scale [...] who knows what");
            }

            // compute the optical flow at the current scale
            (u1s[s], u2s[s]) = Dual_TVL1_optic_flow_new(
                    I0s[s], I1s[s], u1s[s], u2s[s], nx[s], ny[s],
                    tau, lambda, theta, warps, epsilon, verbose,
                    scale_idx: s, total_scales: nscales
            );

            // if this was the last scale, finish now
            if (s == 0)//!s
            {
                break;
            }

            // otherwise, upsample the optical flow

            // zoom the optical flow for the next finer scale
            (u1s[s], u1s[s - 1]) = zoom_in(u1s[s], u1s[s - 1], nx[s],
                ny[s], nx[s - 1], ny[s - 1], with_dt: with_dt);
            (u2s[s], u2s[s - 1]) = zoom_in(u2s[s], u2s[s - 1], nx[s],
                ny[s], nx[s - 1], ny[s - 1], with_dt: with_dt);

            // scale the optical flow with the appropriate zoom factor
            for (int i = 0; i < nx[s - 1] * ny[s - 1]; i++)
            {
                for (int im_idx = 0; im_idx < u2s[nscales - 1].Count; im_idx++)
                {
                    u1s[s - 1][im_idx][i] *= (float)1.0 / zfactor;
                    u2s[s - 1][im_idx][i] *= (float)1.0 / zfactor;
                }
            }
        }

        tv_watch.Stop();
        string tv_backend = (tv_use_gpu && !tv_gpu_failed) ? "GPU" : (tv_use_gpu ? "CPU (GPU-Rueckfall)" : "CPU");
        //27092026 Regularisierung im Log (TGV nur auf der GPU; bei TGV-Fehler Rueckfall auf TV)
        string tv_reg = (tv_use_tgv && tv_backend == "GPU") ? (tgv_failed ? "TV (TGV-Rueckfall)" : "TGV") : "TV";
        Debug.Log("TV-L1 fertig: " + tv_backend + ", Regularisierung " + tv_reg + ", " + nxx + "x" + nyy + ", " + nscales + " Stufen, "
            + warps + " Warps, max " + MAX_ITERATIONS + " Its"
            + (tv_backend == "GPU" ? ", Iterationen gesamt " + tv_stat_iterations : "")
            + " -> " + (tv_watch.ElapsedMilliseconds / 1000.0).ToString("F2", CultureInfo.InvariantCulture) + " s");

        finish_tv_progress();

        return (u1, u2);
    }


    // info (paul): mask.c part from OpenCV

    const int BOUNDARY_CONDITION_DIRICHLET = 0;
    const int BOUNDARY_CONDITION_REFLECTING = 1;
    const int BOUNDARY_CONDITION_PERIODIC = 2;

    int DEFAULT_GAUSSIAN_WINDOW_SIZE = 5;
    int DEFAULT_BOUNDARY_CONDITION = BOUNDARY_CONDITION_REFLECTING;


    /**
     *
     * Details on how to compute the divergence and the grad(u) can be found in:
     * [2] A. Chambolle, "An Algorithm for Total Variation Minimization and
     * Applications", Journal of Mathematical Imaging and Vision, 20: 89-97, 2004
     *
     **/


    /**
     *
     * Function to compute the divergence with backward differences
     * (see [2] for details)
     *
     **/
    /// <summary>
    /// Divergence of a vector field (backward differences).
    /// </summary>
    /// <param name="v1">x component.</param>
    /// <param name="v2">y component.</param>
    /// <param name="div">Output.</param>
    /// <param name="nx">Width.</param>
    /// <returns>Divergence.</returns>
    List<List<float>> divergence(
            List<List<float>> v1, // x component of the vector field
            List<List<float>> v2, // y component of the vector field
            List<List<float>> div,      // output divergence
            int nx,    // image width
            int ny     // image height
               )
    {
        // compute the divergence on the central body of the image
#pragma omp parallel for schedule(dynamic)
        for (int kt = 0; kt < v1.Count; kt++)
        {
            for (int i = 1; i < ny - 1; i++)
            {
                for (int j = 1; j < nx - 1; j++)
                {
                    int p = i * nx + j;
                    int p1 = p - 1;
                    int p2 = p - nx;

                    float v1x = v1[kt][p] - v1[kt][p1];
                    float v2y = v2[kt][p] - v2[kt][p2];

                    div[kt][p] = v1x + v2y;
                }
            }
        }

        // compute the divergence on the first and last rows
        for (int kt = 0; kt < v1.Count; kt++)
        {
            for (int j = 1; j < nx - 1; j++)
            {
                int p = (ny - 1) * nx + j;

                div[kt][j] = v1[kt][j] - v1[kt][j - 1] + v2[kt][j];
                div[kt][p] = v1[kt][p] - v1[kt][p - 1] - v2[kt][p - nx];
            }
        }
        // compute the divergence on the first and last columns
        for (int kt = 0; kt < v1.Count; kt++)
        {
            for (int i = 1; i < ny - 1; i++)
            {
                int p1 = i * nx;
                int p2 = (i + 1) * nx - 1;

                div[kt][p1] = v1[kt][p1] + v2[kt][p1] - v2[kt][p1 - nx];
                div[kt][p2] = -v1[kt][p2 - 1] + v2[kt][p2] - v2[kt][p2 - nx];
            }
        }
        for (int kt = 0; kt < v1.Count; kt++)
        {
            div[kt][0] = v1[kt][0] + v2[kt][0];
            div[kt][nx - 1] = -v1[kt][nx - 2] + v2[kt][nx - 1];
            div[kt][(ny - 1) * nx] = v1[kt][(ny - 1) * nx] - v2[kt][(ny - 2) * nx];
            div[kt][ny * nx - 1] = -v1[kt][ny * nx - 2] - v2[kt][(ny - 1) * nx - 1];
        }
        return div;
    }


    /**
     *
     * Function to compute the gradient with forward differences
     * (see [2] for details)
     *
     **/
    /// <summary>
    /// Gradient with forward differences.
    /// </summary>
    /// <param name="f">Image.</param>
    /// <param name="fx">x derivative (output).</param>
    /// <param name="fy">y derivative (output).</param>
    /// <param name="nx">Width.</param>
    /// <returns>Tuple (fx, fy).</returns>
    (List<float>, List<float>) forward_gradient(
            List<float> f, //input image
            List<float> fx,      //computed x derivative
            List<float> fy,      //computed y derivative
            int nx,   //image width
            int ny    //image height
            )
    {
        // compute the gradient on the central body of the image

        for (int i = 0; i < ny - 1; i++)
        {
            for (int j = 0; j < nx - 1; j++)
            {
                int p = i * nx + j;
                int p1 = p + 1;
                int p2 = p + nx;

                fx[p] = f[p1] - f[p];
                fy[p] = f[p2] - f[p];
            }
        }

        // compute the gradient on the last row
        for (int j = 0; j < nx - 1; j++)
        {
            int p = (ny - 1) * nx + j;

            fx[p] = f[p + 1] - f[p];
            fy[p] = 0;
        }

        // compute the gradient on the last column
        for (int i = 1; i < ny; i++)
        {
            int p = i * nx - 1;

            fx[p] = 0;
            fy[p] = f[p + nx] - f[p];
        }

        fx[ny * nx - 1] = 0;
        fy[ny * nx - 1] = 0;

        return (fx, fy);
    }


    /**
     *
     * Function to compute the gradient with centered differences
     *
     **/

        /// <summary>
        /// Gradient with central differences.
        /// </summary>
        /// <param name="input">Image.</param>
        /// <param name="dx">x derivative (output).</param>
        /// <param name="dy">y derivative (output).</param>
        /// <param name="nx">Width.</param>
        void centered_gradient(
            List<float> input,  //input image
            List<float> dx,           //computed x derivative
            List<float> dy,           //computed y derivative
            int nx,        //image width
            int ny         //image height
            )
    {
        // compute the gradient on the center body of the image

        for (int i = 1; i < ny - 1; i++)
        {
            for (int j = 1; j < nx - 1; j++)
            {
                if (i == 40 && j == 52)
                {
                    ;
                }

                int k = i * nx + j;
                dx[k] = (float)(0.5 * (input[k + 1] - input[k - 1]));
                dy[k] = (float)(0.5 * (input[k + nx] - input[k - nx]));//(input[k + nx] - input[k - nx]));
            }
        }

        // compute the gradient on the first and last rows
        for (int j = 1; j < nx - 1; j++)
        {
            dx[j] = (float)(0.5 * (input[j + 1] - input[j - 1]));
            dy[j] = (float)(0.5 * (input[j + nx] - input[j]));

            int k = (ny - 1) * nx + j;

            dx[k] = (float)(0.5) * (input[k + 1] - input[k - 1]);
            dy[k] = (float)(0.5) * (input[k] - input[k - nx]);
        }

        // compute the gradient on the first and last columns
        for (int i = 1; i < ny - 1; i++)
        {
            int p = i * nx;
            dx[p] = (float)(0.5) * (input[p + 1] - input[p]);
            dy[p] = (float)(0.5) * (input[p + nx] - input[p - nx]);

            int k = (i + 1) * nx - 1;

            dx[k] = (float)(0.5) * (input[k] - input[k - 1]);
            dy[k] = (float)(0.5) * (input[k + nx] - input[k - nx]);
        }

        // compute the gradient at the four corners
        dx[0] = (float)(0.5) * (input[1] - input[0]);
        dy[0] = (float)(0.5) * (input[nx] - input[0]);

        dx[nx - 1] = (float)(0.5) * (input[nx - 1] - input[nx - 2]);
        dy[nx - 1] = (float)(0.5) * (input[2 * nx - 1] - input[nx - 1]);

        dx[(ny - 1) * nx] = (float)(0.5) * (input[(ny - 1) * nx + 1] - input[(ny - 1) * nx]);
        dy[(ny - 1) * nx] = (float)(0.5) * (input[(ny - 1) * nx] - input[(ny - 2) * nx]);

        dx[ny * nx - 1] = (float)(0.5) * (input[ny * nx - 1] - input[ny * nx - 1 - 1]);
        dy[ny * nx - 1] = (float)(0.5) * (input[ny * nx - 1] - input[(ny - 1) * nx - 1]);
    }

    /**
     *
     * In-place Gaussian smoothing of an image
     *
     */
    /// <summary>
    /// Gaussian smoothing of images (separable convolution).
    /// </summary>
    /// <param name="I">Images (input and output).</param>
    /// <param name="xdim">Width.</param>
    /// <param name="ydim">Height.</param>
    /// <param name="sigma">Standard deviation.</param>
    /// <returns>Smoothed images.</returns>
    List<List<float>> gaussian(
        List<List<float>> I,             // input/output image
        int xdim,       // image width
        int ydim,       // image height
        double sigma    // Gaussian sigma
    )
    {
        int boundary_condition = DEFAULT_BOUNDARY_CONDITION;
        int window_size = DEFAULT_GAUSSIAN_WINDOW_SIZE;

        double den = 2 * sigma * sigma;
        int size = (int)(window_size * sigma) + 1;
        int bdx = xdim + size;
        int bdy = ydim + size;

        if (boundary_condition > 0 && size > xdim)
        {
            Debug.Log("GaussianSmooth: sigma too large\n");
            //Debug.Log(stderr, "GaussianSmooth: sigma too large\n");
            //abort();
        }

        // compute the coefficients of the 1D convolution kernel
        //List<double> B = (double*)malloc(size * sizeof(double));
        List<double> B = (new double[size]).ToList();
        for (int i = 0; i < size; i++)
        {
            //B[i] = 1 / (sigma * Mathf.Sqrt(2f * 3.1415926f)) * Mathf.Exp(-i * i / (float)den);
            B[i] = 1 / (sigma * Math.Sqrt(2.0 * 3.1415926)) * Math.Exp(-i * i / den);
        }

        // normalize the 1D convolution kernel
        double norm = 0;
        for (int i = 0; i < size; i++)
            norm += B[i];
        norm *= 2;
        norm -= B[0];
        for (int i = 0; i < size; i++)
            B[i] /= norm;

        // convolution of each line of the input image
        //26072024 List<double> R = (double*)xmalloc((size + xdim + size) * sizeof*R);
        List<List<double>> R = new List<List<double>>();
        for (int kt = 0; kt < I.Count; kt++)
        {
            List<double> R_el = (new double[size + xdim + size]).ToList();
            R.Add(R_el);
        }

        for (int k = 0; k < ydim; k++)
        {
            int i, j;
            for (int kt = 0; kt < I.Count; kt++)
            {
                for (i = size; i < bdx; i++)
                {
                    R[kt][i] = I[kt][k * xdim + i - size];

                }
            }
            switch (boundary_condition)
            {
                case BOUNDARY_CONDITION_DIRICHLET:
                    for (int kt = 0; kt < I.Count; kt++)
                    {
                        for (i = 0, j = bdx; i < size; i++, j++)
                        {
                            R[kt][i] = R[kt][j] = 0;
                        }
                    }
                    break;

                case BOUNDARY_CONDITION_REFLECTING:
                    for (int kt = 0; kt < I.Count; kt++)
                    {
                        for (i = 0, j = bdx; i < size; i++, j++)
                        {
                            R[kt][i] = I[kt][k * xdim + size - i];
                            R[kt][j] = I[kt][k * xdim + xdim - i - 1];
                        }
                    }
                    break;

                case BOUNDARY_CONDITION_PERIODIC:
                    for (int kt = 0; kt < I.Count; kt++)
                    {
                        for (i = 0, j = bdx; i < size; i++, j++)
                        {
                            R[kt][i] = I[kt][k * xdim + xdim - size + i];
                            R[kt][j] = I[kt][k * xdim + i];
                        }
                    }
                    break;
            }
            
            for (int kt = 0; kt < I.Count; kt++)
            {
                for (i = size; i < bdx; i++)
                {
                    double sum = B[0] * R[kt][i];
                    for (j = 1; j < size; j++)
                    {
                        sum += B[j] * (R[kt][i - j] + R[kt][i + j]);
                    }
                    I[kt][k * xdim + i - size] = (float)sum;
                }
            }
        }
        
        // convolution of each column of the input image
        //List<double> T = (double*)xmalloc((size + ydim + size) * sizeof*T);
        //27032025 List<double> T = (new double[size + ydim + size]).ToList();
        List<List<double>> T = new List<List<double>>();
        for (int kt = 0; kt < I.Count; kt++)
        {
            List<double> T_el = (new double[size + xdim + size]).ToList();
            T.Add(T_el);
        }

        for (int k = 0; k < xdim; k++)
        {
            int i, j;

            for (int kt = 0; kt < I.Count; kt++)
            {
                for (i = size; i < bdy; i++)
                {
                    T[kt][i] = I[kt][(i - size) * xdim + k];
                }
            }
            switch (boundary_condition)
            {
                case BOUNDARY_CONDITION_DIRICHLET:
                    for (int kt = 0; kt < I.Count; kt++)
                    {
                        for (i = 0, j = bdy; i < size; i++, j++)
                        { 
                            T[kt][i] = T[kt][j] = 0;
                        }
                    }
                    break;

                case BOUNDARY_CONDITION_REFLECTING:
                    for (int kt = 0; kt < I.Count; kt++)
                    {
                        for (i = 0, j = bdy; i < size; i++, j++)
                        {
                            T[kt][i] = I[kt][(size - i) * xdim + k];
                            T[kt][j] = I[kt][(ydim - i - 1) * xdim + k];
                        }
                    }
                    break;

                case BOUNDARY_CONDITION_PERIODIC:
                    for (int kt = 0; kt < I.Count; kt++)
                    {
                        for (i = 0, j = bdx; i < size; i++, j++)
                        {
                            T[kt][i] = I[kt][(ydim - size + i) * xdim + k];
                            T[kt][j] = I[kt][i * xdim + k];
                        }
                    }
                    break;
            }

            for (int kt = 0; kt < I.Count; kt++)
            {
                for (i = size; i < bdy; i++)
                {
                    double sum = B[0] * T[kt][i];
                    for (j = 1; j < size; j++)
                    {
                        sum += B[j] * (T[kt][i - j] + T[kt][i + j]);
                    }
                    I[kt][(i - size) * xdim + k] = (float)sum;
                }
            }
        }
        return I;
    }

    /// <summary>
    /// Central-difference gradient of several images.
    /// </summary>
    /// <param name="input">Images.</param>
    /// <param name="dx">x derivatives (output).</param>
    /// <param name="dy">y derivatives (output).</param>
    /// <param name="nx">Width.</param>
    /// <returns>Tuple (dx, dy).</returns>
    (List<List<float>>, List<List<float>>) centered_gradient_new(
            List<List<float>> input,  //input image
            List<List<float>> dx,     //computed x derivative
            List<List<float>> dy,     //computed y derivative
            int nx,        //image width
            int ny         //image height
            )
    {
        for (int i = 0; i < input.Count; i++)
        {
            (dx[i], dy[i]) = centered_gradient_new(input[i], dx[i], dy[i], nx, ny);
        }

        return (dx, dy);
    }

    /// <summary>
    /// Central-difference gradient of one image.
    /// </summary>
    /// <param name="input">Image.</param>
    /// <param name="dx">x derivative (output).</param>
    /// <param name="dy">y derivative (output).</param>
    /// <param name="nx">Width.</param>
    /// <returns>Tuple (dx, dy).</returns>
    (List<float>, List<float>) centered_gradient_new(
            List<float> input,  //input image
            List<float> dx,     //computed x derivative
            List<float> dy,     //computed y derivative
            int nx,        //image width
            int ny         //image height
            )
    {
        // compute the gradient on the center body of the image

        for (int i = 1; i < ny - 1; i++)
        {
            for (int j = 1; j < nx - 1; j++)
            {
                int k = i * nx + j;
                dx[k] = (float)(0.5 * (input[k + 1] - input[k - 1]));
                dy[k] = (float)(0.5 * (input[k + nx] - input[k - nx]));//(input[k + nx] - input[k - nx]));
            }
        }

        // compute the gradient on the first and last rows
        for (int j = 1; j < nx - 1; j++)
        {
            dx[j] = (float)(0.5 * (input[j + 1] - input[j - 1]));
            dy[j] = (float)(0.5 * (input[j + nx] - input[j]));

            int k = (ny - 1) * nx + j;

            dx[k] = (float)(0.5) * (input[k + 1] - input[k - 1]);
            dy[k] = (float)(0.5) * (input[k] - input[k - nx]);
        }

        // compute the gradient on the first and last columns
        for (int i = 1; i < ny - 1; i++)
        {
            int p = i * nx;
            dx[p] = (float)(0.5) * (input[p + 1] - input[p]);
            dy[p] = (float)(0.5) * (input[p + nx] - input[p - nx]);

            int k = (i + 1) * nx - 1;

            dx[k] = (float)(0.5) * (input[k] - input[k - 1]);
            dy[k] = (float)(0.5) * (input[k + nx] - input[k - nx]);
        }

        // compute the gradient at the four corners
        dx[0] = (float)(0.5) * (input[1] - input[0]);
        dy[0] = (float)(0.5) * (input[nx] - input[0]);

        dx[nx - 1] = (float)(0.5) * (input[nx - 1] - input[nx - 2]);
        dy[nx - 1] = (float)(0.5) * (input[2 * nx - 1] - input[nx - 1]);

        dx[(ny - 1) * nx] = (float)(0.5) * (input[(ny - 1) * nx + 1] - input[(ny - 1) * nx]);
        dy[(ny - 1) * nx] = (float)(0.5) * (input[(ny - 1) * nx] - input[(ny - 2) * nx]);

        dx[ny * nx - 1] = (float)(0.5) * (input[ny * nx - 1] - input[ny * nx - 1 - 1]);
        dy[ny * nx - 1] = (float)(0.5) * (input[ny * nx - 1] - input[(ny - 1) * nx - 1]);

        return (dx, dy);
    }

    /**
     *
     * In-place Gaussian smoothing of an image
     *
     */
    //07032025altvoid gaussian(
    //07032025alt    List<float> I,             // input/output image
    //07032025alt    int xdim,       // image width
    //07032025alt    int ydim,       // image height
    //07032025alt    double sigma    // Gaussian sigma
    //07032025alt)
    //07032025alt{
    //07032025alt    int boundary_condition = DEFAULT_BOUNDARY_CONDITION;
    //07032025alt    int window_size = DEFAULT_GAUSSIAN_WINDOW_SIZE;
    //07032025alt
    //07032025alt    double den = 2 * sigma * sigma;
    //07032025alt    int size = (int)(window_size * sigma) + 1;
    //07032025alt    int bdx = xdim + size;
    //07032025alt    int bdy = ydim + size;
    //07032025alt
    //07032025alt    if (boundary_condition > 0 && size > xdim)
    //07032025alt    {
    //07032025alt        Debug.Log("GaussianSmooth: sigma too large\n");
    //07032025alt        //Debug.Log(stderr, "GaussianSmooth: sigma too large\n");
    //07032025alt        //abort();
    //07032025alt    }
    //07032025alt
    //07032025alt    // compute the coefficients of the 1D convolution kernel
    //07032025alt    //List<double> B = (double*)malloc(size * sizeof(double));
    //07032025alt    List<double> B = (new double[size]).ToList();
    //07032025alt    for (int i = 0; i < size; i++)
    //07032025alt    {
    //07032025alt        //B[i] = 1 / (sigma * Mathf.Sqrt(2f * 3.1415926f)) * Mathf.Exp(-i * i / (float)den);
    //07032025alt        B[i] = 1 / (sigma * Math.Sqrt(2.0 * 3.1415926)) * Math.Exp(-i * i / den);
    //07032025alt    }
    //07032025alt
    //07032025alt    // normalize the 1D convolution kernel
    //07032025alt    double norm = 0;
    //07032025alt    for (int i = 0; i < size; i++)
    //07032025alt        norm += B[i];
    //07032025alt    norm *= 2;
    //07032025alt    norm -= B[0];
    //07032025alt    for (int i = 0; i < size; i++)
    //07032025alt        B[i] /= norm;
    //07032025alt
    //07032025alt    // convolution of each line of the input image
    //07032025alt    //26072024 List<double> R = (double*)xmalloc((size + xdim + size) * sizeof*R);
    //07032025alt    List<double> R = (new double[size + xdim + size]).ToList();
    //07032025alt
    //07032025alt    for (int k = 0; k < ydim; k++)
    //07032025alt    {
    //07032025alt        int i, j;
    //07032025alt        for (i = size; i < bdx; i++)
    //07032025alt            R[i] = I[k * xdim + i - size];
    //07032025alt
    //07032025alt        switch (boundary_condition)
    //07032025alt        {
    //07032025alt            case BOUNDARY_CONDITION_DIRICHLET:
    //07032025alt                for (i = 0, j = bdx; i < size; i++, j++)
    //07032025alt                    R[i] = R[j] = 0;
    //07032025alt                break;
    //07032025alt
    //07032025alt            case BOUNDARY_CONDITION_REFLECTING:
    //07032025alt                for (i = 0, j = bdx; i < size; i++, j++)
    //07032025alt                {
    //07032025alt                    R[i] = I[k * xdim + size - i];
    //07032025alt                    R[j] = I[k * xdim + xdim - i - 1];
    //07032025alt                }
    //07032025alt                break;
    //07032025alt
    //07032025alt            case BOUNDARY_CONDITION_PERIODIC:
    //07032025alt                for (i = 0, j = bdx; i < size; i++, j++)
    //07032025alt                {
    //07032025alt                    R[i] = I[k * xdim + xdim - size + i];
    //07032025alt                    R[j] = I[k * xdim + i];
    //07032025alt                }
    //07032025alt                break;
    //07032025alt        }
    //07032025alt
    //07032025alt        for (i = size; i < bdx; i++)
    //07032025alt        {
    //07032025alt            double sum = B[0] * R[i];
    //07032025alt            for (j = 1; j < size; j++)
    //07032025alt                sum += B[j] * (R[i - j] + R[i + j]);
    //07032025alt            I[k * xdim + i - size] = (float)sum;
    //07032025alt        }
    //07032025alt    }
    //07032025alt
    //07032025alt    // convolution of each column of the input image
    //07032025alt    //List<double> T = (double*)xmalloc((size + ydim + size) * sizeof*T);
    //07032025alt    List<double> T = (new double[size + ydim + size]).ToList();
    //07032025alt
    //07032025alt    for (int k = 0; k < xdim; k++)
    //07032025alt    {
    //07032025alt        int i, j;
    //07032025alt        for (i = size; i < bdy; i++)
    //07032025alt            T[i] = I[(i - size) * xdim + k];
    //07032025alt
    //07032025alt        switch (boundary_condition)
    //07032025alt        {
    //07032025alt            case BOUNDARY_CONDITION_DIRICHLET:
    //07032025alt                for (i = 0, j = bdy; i < size; i++, j++)
    //07032025alt                    T[i] = T[j] = 0;
    //07032025alt                break;
    //07032025alt
    //07032025alt            case BOUNDARY_CONDITION_REFLECTING:
    //07032025alt                for (i = 0, j = bdy; i < size; i++, j++)
    //07032025alt                {
    //07032025alt                    T[i] = I[(size - i) * xdim + k];
    //07032025alt                    T[j] = I[(ydim - i - 1) * xdim + k];
    //07032025alt                }
    //07032025alt                break;
    //07032025alt
    //07032025alt            case BOUNDARY_CONDITION_PERIODIC:
    //07032025alt                for (i = 0, j = bdx; i < size; i++, j++)
    //07032025alt                {
    //07032025alt                    T[i] = I[(ydim - size + i) * xdim + k];
    //07032025alt                    T[j] = I[i * xdim + k];
    //07032025alt                }
    //07032025alt                break;
    //07032025alt        }
    //07032025alt
    //07032025alt        for (i = size; i < bdy; i++)
    //07032025alt        {
    //07032025alt            double sum = B[0] * T[i];
    //07032025alt            for (j = 1; j < size; j++)
    //07032025alt                sum += B[j] * (T[i - j] + T[i + j]);
    //07032025alt            I[(i - size) * xdim + k] = (float)sum;
    //07032025alt        }
    //07032025alt    }
    //07032025alt}









    // This program is free software: you can use, modify and/or redistribute it
    // under the terms of the simplified BSD License. You should have received a
    // copy of this license along this program. If not, see
    // <http://www.opensource.org/licenses/bsd-license.html>.
    //
    // Copyright (C) 2012, Javier S?nchez P?rez <jsanchez@dis.ulpgc.es>
    // All rights reserved.


    //# ifndef BICUBIC_INTERPOLATION_C
    //#define BICUBIC_INTERPOLATION_C
    //
    //# include <stdbool.h>

    int BOUNDARY_CONDITION = 0;
    //0 Neumann
    //1 Periodic
    //2 Symmetric

    /**
      *
      * Neumann boundary condition test
      *
    **/
    /// <summary>
    /// Neumann boundary condition: clamps an index to the image.
    /// </summary>
    /// <param name="x">Index.</param>
    /// <param name="nx">Size.</param>
    /// <param name="out_bool">Set to true if the index was outside.</param>
    /// <returns>Index.</returns>
    static int neumann_bc(int x, int nx, List<bool> out_bool)
    {
        if (x < 0)
        {
            x = 0;
            out_bool[0] = true;
        }
        else if (x >= nx)
        {
            x = nx - 1;
            out_bool[0] = true;
        }

        return x;
    }

    /**
      *
      * Periodic boundary condition test
      *
    **/
    /// <summary>
    /// Periodic boundary condition for an index.
    /// </summary>
    /// <param name="x">Index.</param>
    /// <param name="nx">Size.</param>
    /// <param name="out_bool">Set to true if the index was outside.</param>
    /// <returns>Index.</returns>
    static int periodic_bc(int x, int nx, List<bool> out_bool)
    {
        if (x < 0)
        {
            int n = 1 - (int)(x / (nx + 1));
            int ixx = x + n * nx;

            x = ixx % nx;
            out_bool[0] = true;
        }
        else if (x >= nx)
        {
            x = x % nx;
            out_bool[0] = true;
        }

        return x;
    }


    /**
      *
      * Symmetric boundary condition test
      *
    **/
    /// <summary>
    /// Symmetric (mirrored) boundary condition for an index.
    /// </summary>
    /// <param name="x">Index.</param>
    /// <param name="nx">Size.</param>
    /// <param name="out_bool">Set to true if the index was outside.</param>
    /// <returns>Index.</returns>
    static int symmetric_bc(int x, int nx, List<bool> out_bool)
    {
        if (x < 0)
        {
            int borde = nx - 1;
            int xx = -x;
            int n = (int)(xx / borde) % 2;

            if (n == 1)
            {
                x = borde - (xx % borde);
            }
            else
            {
                x = xx % borde;
            }
            out_bool[0] = true;
        }

        else if (x >= nx)
        {
            int borde = nx - 1;
            int n = (int)(x / borde) % 2;

            if (n == 1)
            {
                x = borde - (x % borde);
            }
            else
            {
                x = x % borde;
            }
            out_bool[0] = true;
        }

        return x;
    }


    /**
      *
      * Cubic interpolation in one dimension
      *
    **/
    /// <summary>
    /// Cubic interpolation of four points.
    /// </summary>
    /// <param name="v">Four interpolation points.</param>
    /// <returns>Interpolated value.</returns>
    static double cubic_interpolation_cell(
        List<double> v, //[4],  //interpolation points
        double x      //point to be interpolated
    )
    {
        return v[1] + 0.5 * x * (v[2] - v[0] +
            x * (2.0 * v[0] - 5.0 * v[1] + 4.0 * v[2] - v[3] +
            x * (3.0 * (v[1] - v[2]) + v[3] - v[0])));
    }


    /**
      *
      * Bicubic interpolation in two dimensions
      *
    **/
    /// <summary>
    /// Bicubic interpolation in a 4x4 cell.
    /// </summary>
    /// <param name="p">4x4 interpolation points.</param>
    /// <param name="x">x position.</param>
    /// <returns>Interpolated value.</returns>
    double bicubic_interpolation_cell(
        List<List<double>> p, //p[4][4], //array containing the interpolation points
        double x,       //x position to be interpolated
        double y        //y position to be interpolated
    )
    {
        List<double> v = doubles_of_size(4);//doubles_of_size(4);
        v[0] = cubic_interpolation_cell(p[0], y);
        v[1] = cubic_interpolation_cell(p[1], y);
        v[2] = cubic_interpolation_cell(p[2], y);
        v[3] = cubic_interpolation_cell(p[3], y);
        return cubic_interpolation_cell(v, x);
    }

    /**
      *
      * Compute the bicubic interpolation of a point in an image.
      * Detect if the point goes outside the image domain.
      *
    **/
    /// <summary>
    /// Bicubic interpolation of an image at a sub-pixel position.
    /// </summary>
    /// <param name="input">Image.</param>
    /// <param name="uu">x position.</param>
    /// <param name="vv">y position.</param>
    /// <param name="nx">Width.</param>
    /// <param name="ny">Height.</param>
    /// <returns>Value (0 outside if border_out).</returns>
    float bicubic_interpolation_at(

    List<float> input, //image to be interpolated
    float uu,    //x component of the vector field
    float vv,    //y component of the vector field
    int nx,    //image width
    int ny,    //image height
    bool border_out //if true, return zero outside the region
)
    {
        //int sx = (uu < 0) ? -1 : 1;
        //int sy = (vv < 0) ? -1 : 1;

        int sx, sy;
        if (uu < 0) { sx = -1; } else { sx = 1; }
        if (vv < 0) { sy = -1; } else { sy = 1; }

        int x, y, mx, my, dx, dy, ddx, ddy;
        List<bool> out_bool = new List<bool>() { false };

        //apply the corresponding boundary conditions
        switch (BOUNDARY_CONDITION)
        {
            case 0:
                {
                    x = neumann_bc((int)uu, nx, out_bool);
                    y = neumann_bc((int)vv, ny, out_bool);
                    mx = neumann_bc((int)uu - sx, nx, out_bool);
                    my = neumann_bc((int)vv - sy, ny, out_bool);//13082025 sy
                    dx = neumann_bc((int)uu + sx, nx, out_bool);
                    dy = neumann_bc((int)vv + sy, ny, out_bool);
                    ddx = neumann_bc((int)uu + 2 * sx, nx, out_bool);
                    ddy = neumann_bc((int)vv + 2 * sy, ny, out_bool);
                    break;
                }
            case 1:
                {
                    x = periodic_bc((int)uu, nx, out_bool);
                    y = periodic_bc((int)vv, ny, out_bool);
                    mx = periodic_bc((int)uu - sx, nx, out_bool);
                    my = periodic_bc((int)vv - sy, ny, out_bool);//13082025 sy
                    dx = periodic_bc((int)uu + sx, nx, out_bool);
                    dy = periodic_bc((int)vv + sy, ny, out_bool);
                    ddx = periodic_bc((int)uu + 2 * sx, nx, out_bool);
                    ddy = periodic_bc((int)vv + 2 * sy, ny, out_bool);
                    break;
                }
            case 2:
                {
                    x = symmetric_bc((int)uu, nx, out_bool);
                    y = symmetric_bc((int)vv, ny, out_bool);
                    mx = symmetric_bc((int)uu - sx, nx, out_bool);
                    my = symmetric_bc((int)vv - sy, ny, out_bool);//13082025 sy
                    dx = symmetric_bc((int)uu + sx, nx, out_bool);
                    dy = symmetric_bc((int)vv + sy, ny, out_bool);
                    ddx = symmetric_bc((int)uu + 2 * sx, nx, out_bool);
                    ddy = symmetric_bc((int)vv + 2 * sy, ny, out_bool);
                    break;
                }
            default:
                {
                    x = neumann_bc((int)uu, nx, out_bool);
                    y = neumann_bc((int)vv, ny, out_bool);
                    mx = neumann_bc((int)uu - sx, nx, out_bool);
                    my = neumann_bc((int)vv - sy, ny, out_bool);//13082025 sy
                    dx = neumann_bc((int)uu + sx, nx, out_bool);
                    dy = neumann_bc((int)vv + sy, ny, out_bool);
                    ddx = neumann_bc((int)uu + 2 * sx, nx, out_bool);
                    ddy = neumann_bc((int)vv + 2 * sy, ny, out_bool);
                    break;
                }
        }

        if (out_bool[0] && border_out)
        {
            return 0f;
        }
        else
        {
            //obtain the interpolation points of the image
            float p11 = input[mx + nx * my];
            float p12 = input[x + nx * my];
            float p13 = input[dx + nx * my];
            float p14 = input[ddx + nx * my];

            float p21 = input[mx + nx * y];
            float p22 = input[x + nx * y];
            float p23 = input[dx + nx * y];
            float p24 = input[ddx + nx * y];

            float p31 = input[mx + nx * dy];
            float p32 = input[x + nx * dy];
            float p33 = input[dx + nx * dy];
            float p34 = input[ddx + nx * dy];

            float p41 = input[mx + nx * ddy];
            float p42 = input[x + nx * ddy];
            float p43 = input[dx + nx * ddy];
            float p44 = input[ddx + nx * ddy];

            //create array
            //double pol[4][4] = {
            //    { p11, p21, p31, p41},
            //		{ p12, p22, p32, p42},
            //		{ p13, p23, p33, p43},
            //		{ p14, p24, p34, p44}
            //};

            List<List<double>> pol = doubles_of_size(4, 4);// new double[4][4];
            pol[0][0] = p11;//p11;
            pol[0][1] = p21;//p12;
            pol[0][2] = p31;//p13;
            pol[0][3] = p41;//p14;
            pol[1][0] = p12;//p21;
            pol[1][1] = p22;//p22;
            pol[1][2] = p32;//p23;
            pol[1][3] = p42;//p24;
            pol[2][0] = p13;//p31;
            pol[2][1] = p23;//p32;
            pol[2][2] = p33;//p33;
            pol[2][3] = p43;//p34;
            pol[3][0] = p14;//p41;
            pol[3][1] = p24;//p42;
            pol[3][2] = p34;//p43;
            pol[3][3] = p44;//p44;

            pol = nans2zero(pol);

            //return interpolation
            double interpol_val = bicubic_interpolation_cell(pol, uu - x, vv - y);
            return (float)interpol_val;
        }
    }


    /// <summary>
    /// Replaces NaN entries by zero.
    /// </summary>
    /// <param name="pol">Matrix.</param>
    /// <returns>The matrix.</returns>
    public List<List<double>> nans2zero(List<List<double>> pol)
    {
        for (int i = 0; i < pol.Count; i++)
        {
            for (int j = 0; j < pol[i].Count; j++)
            {
                double pol_el = pol[i][j];
                if (double.IsNaN(pol_el))
                {
                    pol[i][j] = 0f;
                }
            }
        }

        return pol;
    }

    /**
      *
      * Compute the bicubic interpolation of an image.
      *
    **/
    /// <summary>
    /// Warps images with a vector field using bicubic interpolation.
    /// </summary>
    /// <param name="input">Images.</param>
    /// <param name="u">x components.</param>
    /// <param name="v">y components.</param>
    /// <param name="output">Warped images.</param>
    /// <param name="nx">Width.</param>
    /// <param name="ny">Height.</param>
    /// <returns>Warped images.</returns>
    List<List<float>> bicubic_interpolation_warp_new(
        List<List<float>> input,     // image to be warped
        List<List<float>> u,         // x component of the vector field
        List<List<float>> v,         // y component of the vector field
        List<List<float>> output,    // image warped with bicubic interpolation
        int nx,        // image width
        int ny,        // image height
        bool border_out // if true, put zeros outside the region
    )
    {
        for (int im_idx = 0; im_idx < output.Count; im_idx++)
        {
            for (int i = 0; i < ny; i++)
            {
                for (int j = 0; j < nx; j++)
                {
                    int p = i * nx + j;
                    float uu = (float)(j + u[im_idx][p]);
                    float vv = (float)(i + v[im_idx][p]);
                    
                    // obtain the bicubic interpolation at position (uu, vv)
                    output[im_idx][p] = bicubic_interpolation_at(input[im_idx],
                            uu, vv, nx, ny, border_out);
                }
            }
        }
        return output;
    }



        // info (paul): zoom.c
        // This program is free software: you can use, modify and/or redistribute it
        // under the terms of the simplified BSD License. You should have received a
        // copy of this license along this program. If not, see
        // <http://www.opensource.org/licenses/bsd-license.html>.
        //
        // Copyright (C) 2012, Javier S?nchez P?rez <jsanchez@dis.ulpgc.es>
        // All rights reserved.

        double ZOOM_SIGMA_ZERO = 0.6;

        /**
          *
          * Compute the size of a zoomed image from the zoom factor
          *
        **/
        /// <summary>
        /// Image size after zooming by a factor.
        /// </summary>
        /// <param name="nx">Width.</param>
        /// <param name="ny">Height.</param>
        /// <returns>Tuple (new width, new height).</returns>
        (int, int) zoom_size(
            int nx,      // width of the orignal image
            int ny,      // height of the orignal image
                         //int nxx,    // width of the zoomed image
                         //int nyy,    // height of the zoomed image
            float factor // zoom factor between 0 and 1
        )
        {
            //compute the new size corresponding to factor
            //we add 0.5 for rounding off to the closest number
            int nxx = (int)((float)nx * factor + 0.5);
            int nyy = (int)((float)ny * factor + 0.5);
            return (nxx, nyy);
        }
        /**
          *
          * Downsample an image
          *
        **/
        /// <summary>
        /// Downsamples images (Gaussian pre-smoothing and bicubic interpolation).
        /// </summary>
        /// <param name="I">Images.</param>
        /// <param name="Iout">Output.</param>
        /// <param name="nx">Width.</param>
        /// <param name="ny">Height.</param>
        /// <returns>Downsampled images.</returns>
        List<List<float>> zoom_out(
        List<List<float>> I,    // input image
        List<List<float>> Iout,       // output image
        int nx,      // image width
        int ny,      // image height
        float factor // zoom factor between 0 and 1
    )
        {
            // temporary working image
            //float* Is = (float*)xmalloc(nx * ny * sizeof*Is);
            List<List<float>> Is = zeros_of_size(I.Count, nx * ny);//(new float[nx * ny]).ToList();

            for (int kt = 0; kt < I.Count; kt++)
            {
                for (int i = 0; i < nx * ny; i++)
                {
                    Is[kt][i] = I[kt][i];
                }
            }

            // compute the size of the zoomed image
            (int nxx, int nyy) = zoom_size(nx, ny, factor);
            
            // compute the Gaussian sigma for smoothing
            float sigma = (float)(ZOOM_SIGMA_ZERO * Math.Sqrt(1d / ((double)(factor * factor)) - 1d));
            
            // pre-smooth the image
            
            List<List<float>> Iss = I;//28032025 new List<List<float>>(){I};
            Is = gaussian(Iss, nx, ny, sigma);
            
            // re-sample the image using bicubic interpolation
            for (int kt = 0; kt < Is.Count; kt++)
            {
                for (int i1 = 0; i1 < nyy; i1++)
                {
                    for (int j1 = 0; j1 < nxx; j1++)
                    {
                        float i2 = (float)i1 / factor;
                        float j2 = (float)j1 / factor;

                        double g = bicubic_interpolation_at(Is[kt], j2, i2, nx, ny, false);
                        Iout[kt][i1 * nxx + j1] = (float)g;
                    }
                }
            }
            return Iout;
        }


    /**
      *
      * Function to upsample the image
      *
    **/

    int i_c = 126;
    int j_c = 166;
    
    /// <summary>
    /// Upsamples flow fields to a larger scale (bicubic interpolation).
    /// </summary>
    /// <param name="I">Fields.</param>
    /// <param name="Iout">Output.</param>
    /// <param name="nx">Original width.</param>
    /// <param name="ny">Original height.</param>
    /// <param name="nxx">New width.</param>
    /// <param name="nyy">New height.</param>
    /// <param name="with_dt">True for time-series analysis.</param>
    /// <returns>Tuple of upsampled fields.</returns>
    (List<List<float>>, List<List<float>>) zoom_in(
        List<List<float>> I, // input image
        List<List<float>> Iout,    // output image
        int nx,         // width of the original image
        int ny,         // height of the original image
        int nxx,        // width of the zoomed image
        int nyy,         // height of the zoomed image
        bool with_dt
    )
    {
        // compute the zoom factor
        float factorx = ((float)nxx / nx);
        float factory = ((float)nyy / ny);

        // re-sample the image using bicubic interpolation
        for (int kt = 0; kt < I.Count; kt++)
        {
            for (int i1 = 0; i1 < nyy; i1++)
            {
                for (int j1 = 0; j1 < nxx; j1++)
                {
                    float i2 = (float)i1 / factory;
                    float j2 = (float)j1 / factorx;

                    if (i1 == i_c && j1 == j_c && !with_dt)
                    {
                        ;
                    }

                    float g = (float)bicubic_interpolation_at(I[kt], j2, i2, nx, ny, false);
                    Iout[kt][i1 * nxx + j1] = g;
                }
            }
        }
        return (I, Iout);
    }
}
public class Actioner
{
    // info (paul): A class containing an action, 
    //      but also some other possibly useful parameters, 
    //      e.g. a label
    private string label;
    public Action act;
    private int cv_render_idx;

    public int val_0;
    public int val_1;

    public readonly Params pars;

    /// <summary>
    /// Step of the action list (action, label, flow index, parameters).
    /// </summary>
    /// <param name="act">Action.</param>
    /// <param name="label">Label.</param>
    /// <param name="cv_render_idx">Index of the flow action.</param>
    /// <param name="pars">Parameters.</param>
    public Actioner(Action act, string label = null, int cv_render_idx = -1,
        Params pars = null)
    {
        this.act = act;
        this.label = label;
        this.cv_render_idx = cv_render_idx;
        this.pars = pars;
    }
    /// <summary>
    /// Returns the label.
    /// </summary>
    /// <returns>Label.</returns>
    public string get_label()
    {
        return this.label;
    }
    /// <summary>
    /// Sets the label.
    /// </summary>
    /// <param name="input">Label.</param>
    public void set_label(string input)
    {
        this.label = input;
    }

    /// <summary>
    /// Sets the index of the flow action.
    /// </summary>
    /// <param name="input">Index.</param>
    public void set_cv_render_idx(int input)
    {
        this.cv_render_idx = input;
    }
    /// <summary>
    /// Returns the index of the flow action.
    /// </summary>
    /// <returns>Index.</returns>
    public int get_cv_render_idx()
    {
        return cv_render_idx;
    }

}

public class im_dressed
{
    public List<float> im_vec;
    public int width;
    public int height;

    /// <summary>
    /// Image vector with width and height.
    /// </summary>
    /// <param name="im_vec">Gray values.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    public im_dressed(List<float> im_vec, int width, int height)
    {
        this.im_vec = im_vec;
        this.width = width;
        this.height = height;
    }
}

public class Params
{
    private float speckle_size;
    private float lighting_intensity;
    private float lighting_pos_x; 
    private float lighting_pos_y; 
    private float lighting_pos_z; 
    private float gaussian_error; 
    private float poisson_error;
    private float lens_distortion;

    /// <summary>
    /// Parameters of an experiment (NaN = default).
    /// </summary>
    /// <param name="speckle_size">Speckle size.</param>
    /// <param name="lighting_intensity">Illumination factor.</param>
    /// <param name="lighting_pos_x">Light position x.</param>
    /// <param name="lighting_pos_y">Light position y.</param>
    /// <param name="lighting_pos_z">Light position z.</param>
    /// <param name="gaussian_error">Gaussian noise.</param>
    /// <param name="poisson_error">Poisson noise (N_peak).</param>
    /// <param name="lens_distortion">Lens distortion.</param>
    public Params(float speckle_size = float.NaN, float lighting_intensity = float.NaN,
        float lighting_pos_x = float.NaN, float lighting_pos_y = float.NaN,
        float lighting_pos_z = float.NaN, float gaussian_error = float.NaN,
        float poisson_error = float.NaN, float lens_distortion = float.NaN)
    {
        // info (paul): init with default params, whereever the user does not give an explicit input

        // info (paul): first just define vals
        set_speckle_size(speckle_size);
        set_lighting_intensity(lighting_intensity);
        set_lighting_pos_x(lighting_pos_x);
        set_lighting_pos_y(lighting_pos_y);
        set_lighting_pos_z(lighting_pos_z);
        set_gaussian_error(gaussian_error);
        set_poisson_error(poisson_error);
        set_lens_distortion(lens_distortion);

        float speckle_size_default = 0.070f;//0.035f;// or what's a good value
        float lighting_intensity_default = 0.0f;//0.7f;
        float lighting_pos_x_default = 0f;
        float lighting_pos_y_default = 100.6f;
        float lighting_pos_z_default = -162.4f;
        float gaussian_error_default = 0f; // or whatever
        float poisson_error_default = 0f; // or whatever
        float lens_distortion_default = 0f; // or whatever

        // info (paul): if sth was NaN, overwrite with default vals
        if (float.IsNaN(speckle_size))
        {
            set_speckle_size(speckle_size_default);
        }
        if (float.IsNaN(lighting_intensity))
        {
            set_lighting_intensity(lighting_intensity_default);
        }
        if (float.IsNaN(lighting_pos_x))
        {
            set_lighting_pos_x(lighting_pos_x_default);
        }
        if (float.IsNaN(lighting_pos_y))
        {
            set_lighting_pos_y(lighting_pos_y_default);
        }
        if (float.IsNaN(lighting_pos_z))
        {
            set_lighting_pos_z(lighting_pos_z_default);
        }
        if (float.IsNaN(gaussian_error))
        {
            set_gaussian_error(gaussian_error_default);
        }
        if (float.IsNaN(poisson_error))
        {
            set_poisson_error(poisson_error_default);
        }
        if (float.IsNaN(lens_distortion))
        {
            set_lens_distortion(lens_distortion_default);
        }
    }

    /// <summary>
    /// Returns the speckle size.
    /// </summary>
    /// <returns>Value.</returns>
    public float get_speckle_size()
    {
        return this.speckle_size;
    }
    /// <summary>
    /// Sets the speckle size.
    /// </summary>
    /// <param name="value">Value.</param>
    public void set_speckle_size(float value)
    {
        this.speckle_size = value;
    }
    /// <summary>
    /// Returns the illumination factor.
    /// </summary>
    /// <returns>Value.</returns>
    public float get_lighting_intensity()
    {
        return this.lighting_intensity;
    }
    /// <summary>
    /// Sets the illumination factor.
    /// </summary>
    /// <param name="value">Value.</param>
    public void set_lighting_intensity(float value)
    {
        this.lighting_intensity = value;
    }   
    /// <summary>
    /// Returns the light position x.
    /// </summary>
    /// <returns>Value.</returns>
    public float get_lighting_pos_x()
    {
        return this.lighting_pos_x;
    }
    /// <summary>
    /// Sets the light position x.
    /// </summary>
    /// <param name="value">Value.</param>
    public void set_lighting_pos_x(float value)
    {
        this.lighting_pos_x = value;
    }
    /// <summary>
    /// Returns the light position y.
    /// </summary>
    /// <returns>Value.</returns>
    public float get_lighting_pos_y()
    {
        return this.lighting_pos_y;
    }
    /// <summary>
    /// Sets the light position y.
    /// </summary>
    /// <param name="value">Value.</param>
    public void set_lighting_pos_y(float value)
    {
        lighting_pos_y = value;
    }
    /// <summary>
    /// Returns the light position z.
    /// </summary>
    /// <returns>Value.</returns>
    public float get_lighting_pos_z()
    {
        return this.lighting_pos_z;
    }
    /// <summary>
    /// Sets the light position z.
    /// </summary>
    /// <param name="value">Value.</param>
    public void set_lighting_pos_z(float value)
    {
        this.lighting_pos_z = value;
    }
    /// <summary>
    /// Returns the Gaussian noise.
    /// </summary>
    /// <returns>Value.</returns>
    public float get_gaussian_error()
    {
        return this.gaussian_error;
    }
    /// <summary>
    /// Sets the Gaussian noise.
    /// </summary>
    /// <param name="value">Value.</param>
    public void set_gaussian_error(float value)
    {
        this.gaussian_error = value;
    }
    /// <summary>
    /// Returns the Poisson noise (N_peak).
    /// </summary>
    /// <returns>Value.</returns>
    public float get_poisson_error()
    {
        return this.poisson_error;
    }
    /// <summary>
    /// Sets the Poisson noise (N_peak).
    /// </summary>
    /// <param name="value">Value.</param>
    public void set_poisson_error(float value)
    {
        this.poisson_error = value;
    }
    /// <summary>
    /// Returns the lens distortion.
    /// </summary>
    /// <returns>Value.</returns>
    public float get_lens_distortion()
    {
        return this.lens_distortion;
    }
    /// <summary>
    /// Sets the lens distortion.
    /// </summary>
    /// <param name="value">Value.</param>
    public void set_lens_distortion(float value)
    {
        this.lens_distortion = value;
    }
}

public class ExpConfig
{
    public float fov;
    public string label;
    public float diameter;
    float cam_angle;

    float[] light_pos = new float[3];
    float[] light_quat = new float[4];
    float ambient_intensity;

    float noise_val = float.NaN;

    List<float[]> cam_poss;

    // info (paul): path, where rendered images are saved
    string save_path;
    string blade_path;

    /// <summary>
    /// Configuration of an experiment (field of view, label, speckle diameter, cameras, light).
    /// </summary>
    /// <param name="fov">Field of view.</param>
    /// <param name="label">Label.</param>
    /// <param name="diameter">Speckle diameter.</param>
    public ExpConfig(float fov = float.NaN, string label = "NO_LABEL", float diameter = 0.070f)
    {
        this.fov = fov;
        this.label = label;
        this.diameter = diameter;
        this.ambient_intensity = float.NaN;

        float[] cam_poss_0 = new float[3] { 0f, 0f, 0f };
        float[] cam_poss_1 = new float[3] { 0f, 0f, 0f };
        cam_poss = new List<float[]>() { cam_poss_0, cam_poss_1 };
    }

    /// <summary>
    /// Sets the light position.
    /// </summary>
    /// <param name="val">Position {x, y, z}.</param>
    public void set_light_pos(float[] val)
    {
        this.light_pos = val;
    }
    /// <summary>
    /// Returns the light position.
    /// </summary>
    /// <returns>Position.</returns>
    public float[] get_light_pos()
    {
        return this.light_pos;
    }
    /// <summary>
    /// Sets the light rotation.
    /// </summary>
    /// <param name="val">Quaternion.</param>
    public void set_light_quat(float[] val)
    {
        this.light_quat = val;
    }
    /// <summary>
    /// Returns the light rotation.
    /// </summary>
    /// <returns>Quaternion.</returns>
    public float[] get_light_quat()
    {
        return this.light_quat;
    }

    /// <summary>
    /// Index of a coordinate name.
    /// </summary>
    /// <param name="coord">x, y, or z.</param>
    /// <returns>0, 1, 2 (or -1).</returns>
    public int coord2int(string coord)
    {
        int coord_idx = -1;

        if (coord == "x")
        {
            coord_idx = 0;
        }
        if (coord == "y")
        {
            coord_idx = 1;
        }
        if (coord == "z")
        {
            coord_idx = 2;
        }

        return coord_idx;
    }
    /// <summary>
    /// Sets one coordinate of a camera position.
    /// </summary>
    /// <param name="cam_idx">Camera index.</param>
    /// <param name="coord">x, y, or z.</param>
    /// <param name="val">Value.</param>
    public void set_cam_pos(int cam_idx, string coord, float val)
    {
        // info (paul): e.g. cam_idx = 0, coord="z", val="2.15"

        this.cam_poss[cam_idx][coord2int(coord)] = val;
    }
    /// <summary>
    /// Returns the camera positions.
    /// </summary>
    /// <returns>Positions.</returns>
    public List<float[]> get_cam_poss()
    {
        return this.cam_poss;
    }
    /// <summary>
    /// Sets the field of view.
    /// </summary>
    /// <param name="input">Degrees.</param>
    public void set_fov(float input)
    {
        this.fov = input;
    }
    /// <summary>
    /// Returns the field of view.
    /// </summary>
    /// <returns>Degrees.</returns>
    public float get_fov()
    {
        return this.fov;
    }
    /// <summary>
    /// Sets the label.
    /// </summary>
    /// <param name="label">Label.</param>
    public void set_label(string label)
    {
        this.label = label;
    }
    /// <summary>
    /// Returns the label.
    /// </summary>
    /// <returns>Label.</returns>
    public string get_label()
    {
        return label;
    }
    /// <summary>
    /// Sets the speckle diameter.
    /// </summary>
    /// <param name="diameter">Diameter.</param>
    public void set_diameter(float diameter)
    {
        this.diameter = diameter;
    }
    /// <summary>
    /// Returns the speckle diameter.
    /// </summary>
    /// <returns>Diameter.</returns>
    public float get_diameter()
    {
        return this.diameter;
    }

    /// <summary>
    /// Sets the camera tilt.
    /// </summary>
    /// <param name="cam_angle">Degrees.</param>
    public void set_cam_angle(float cam_angle)
    {
        this.cam_angle = cam_angle;
    }
    /// <summary>
    /// Returns the camera tilt.
    /// </summary>
    /// <returns>Degrees.</returns>
    public float get_cam_angle()
    {
        return cam_angle;
    }

    /// <summary>
    /// Sets the output path.
    /// </summary>
    /// <param name="val">Path.</param>
    public void set_save_path(string val)
    {
        this.save_path = val;
    }
    /// <summary>
    /// Returns the output path.
    /// </summary>
    /// <returns>Path.</returns>
    public string get_save_path()
    {
        return this.save_path;
    }

    /// <summary>
    /// Sets the path of the sample data.
    /// </summary>
    /// <param name="input">Path.</param>
    public void set_blade_path(string input)
    {
        this.blade_path = input;
    }

    /// <summary>
    /// Returns the path of the sample data.
    /// </summary>
    /// <returns>Path.</returns>
    public string get_blade_path()
    {
        return this.blade_path;
    }

    /// <summary>
    /// Sets the ambient intensity.
    /// </summary>
    /// <param name="input">Value.</param>
    public void set_ambient_intensity(float input)
    {
        this.ambient_intensity = input;
    }

    /// <summary>
    /// Returns the ambient intensity.
    /// </summary>
    /// <returns>Value.</returns>
    public float get_ambient_intensity()
    {
        return this.ambient_intensity;
    }
}
