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
using static UnityEngine.UIElements.UxmlAttributeDescription;
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
    void Start()
    {
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
        default_dist = 300f;
        if (category == "muc")
        {
            fov_for_heights = 5f;
            heights_fov = fov_for_heights;
            default_dist = 300f;//18032025 900f;
        }
        uv_scale = 1f;//24112024 1f; //07112024 5f;//30102024 5f;
        blade_idxs = new List<int>() {3, 15, 3}; //13072025 15, 23, 23 };//{15, 25, 27, 28};//{15, 25, 27};//15032025 {15, 16, 17};//25022025 { 1, 20, 28 };//16122024 { 8, 9, 10};//12122024 //11112024 {1, 20, 28};//04112024 {1, 20, 28};//30102024 { 1, 20, 30, 98};//, 98 };//, 92, 93, 94 };
        //30112024A blade_idxs = new List<int>() { 28, 29};
        our_blade_idxs = new List<int>() { 80, 90 };
        speckle_mat = (Material)Resources.Load("Targets/speckle_mat");

        speckle_acts = new List<(Action, float, float)>();
        field_of_view_heights = 40f;//06122024 
        set_field_of_view(30);//15032025 (20f);//15012024 (field_of_view_heights);//02122024 20f
        im_paths = new List<string>();

        strain_x = new List<float>();
        strain_y = new List<float>();

        canvas = GameObject.Find("Canvas");

        truth_map_scale = 1f;

        path_base = "/Users/Paul/Desktop/DIC_2025_for_travel/";
        path_dic = path_base + "DIC_package/";
        root_path = path_dic + "exp_normal/time_flow_v/nice_pics/";
        path_project = "/Users/Paul/Desktop/DIC_2025_for_travel/unter2_Windows_native _october/";//14032025 "not_relevant";//13022024 "C:/Users/go73jem/unter2_Windows_native";

        //blade_path = "C:/Users/Paul/Desktop/DIC_2025_for_travel/play_blender_pycahrm/" +
        //    "write_mesh/" + "mucverts_29.txt";

        path_dic = UnityEngine.Application.dataPath;
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
    public Params get_params_now()
    {
        return params_now;
    }
    public float get_truth_map_scale()
    {
        return this.truth_map_scale;
    }
    public void set_truth_map_scale(float value)
    {
        this.truth_map_scale = value;
    }

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

    public List<float> blend_floats(List<float> minor_ref, List<float> minor, float add_share)
    {
        List<float> blend = new List<float>();

        for (int i = 0; i < minor.Count; i++)
        {
            blend.Add((1f - add_share) * minor_ref[i] + add_share * minor[i]);
        }

        return blend;
    }

    public Camera get_cam_for_uv_0()
    {
        return this.cam_for_uv_0;
    }
    public Camera get_cam_for_uv_1()
    {
        return this.cam_for_uv_1;
    }
    public void set_truth_0_u(List<List<float>> input)
    {
        this.truth_0_u = input;
    }
    public List<List<float>> get_truth_0_u()
    {
        return truth_0_u;
    }
    public void set_truth_1_u(List<List<float>> input)
    {
        this.truth_1_u = input;
    }
    public List<List<float>> get_truth_1_u()
    {
        return truth_1_u;
    }

    public void set_truth_0_v(List<List<float>> input)
    {
        this.truth_0_v = input;
    }
    public List<List<float>> get_truth_0_v()
    {
        return this.truth_0_v;
    }
    public void set_truth_1_v(List<List<float>> input)
    {
        this.truth_1_v = input;
    }
    public List<List<float>> get_truth_1_v()
    {
        return this.truth_1_v;
    }
    public Vector3 get_blades_pos()
    {
        return this.blades_pos;
    }
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
    public void render_speckles()
    {
        Texture2D tex = speckle2tex();

        // info (paul): save tex
        string label = "speckle_" + this.speckle_size.ToString() + ".png";
        string path_l = path_dic + "speckle_patterns/" + label;


        System.IO.File.WriteAllBytes(path_l, tex.EncodeToPNG());
    }

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

    public void set_blade_path(string input)
    {
        this.blade_path = input;
    }
    public string get_blade_path()
    {
        return blade_path;
    }

    public void set_path_dic(string input)
    {
        this.path_dic = input;
    }
    public string get_path_dic()
    {
        return path_dic;
    }
    public void add_to_pic_timer(float value)
    {
        float val_now = get_pic_timer();
        float val_new = val_now + value;
        set_pic_timer(val_new);

    }
    public void set_pic_timer(float value)
    {
        this.pic_timer = value;
    }
    public float get_pic_timer()
    {
        return pic_timer;
    }

    public bool get_blades_created()
    {
        return blades_created;
    }
    public void set_blades_created(bool input)
    {
        blades_created = input;
    }
    public void set_field_of_view(float input)
    {
        this.field_of_view = input;
    }
    public float get_field_of_view()
    {
        return this.field_of_view;
    }
    public int get_blade_idx()
    {
        return this.blade_idx_secret;
    }
    public void set_blade_idx(int input)
    {
        this.blade_idx_secret = input;
    }

    public void set_with_main(bool val)
    {
        this.with_main = val;
    }
    public bool get_with_main()
    {
        return this.with_main;
    }
    public void add_to_im_files(string file_path)
    {
        im_paths.Add(file_path);
        refresh_tv_files(get_im_paths());
    }
    public void remove_from_im_files(string file_path)
    {
        im_paths.Remove(file_path);
        refresh_tv_files(get_im_paths());
    }
    public List<string> get_im_paths()
    {
        return im_paths;
    }
    public void set_im_paths(List<string> input)
    {
        im_paths = input;
    }
    public void set_in_dir(string input)
    {
        this.in_dir = input;
    }
    public string get_in_dir()
    {
        return this.in_dir;
    }
    public void set_out_dir(string input)
    {
        this.out_dir = input;
    }
    public string get_out_dir()
    {
        return this.out_dir;
    }
    public void set_series(bool val)
    {
        series = val;
    }
    public bool get_series()
    {
        return series;
    }

    public void set_render_res(int input)
    {
        this.render_res = input;
    }
    public int get_render_res()
    {
        return this.render_res;
    }

    string im_path_0;
    string im_path_1;
    public void refresh_tv_files(List<string> im_paths)
    {
        if (im_paths.Count > 1)
        {
            im_path_0 = im_paths[0];
            im_path_1 = im_paths[1];
        }


        ;
    }
    public void refresh_files_list()
    {
        //this.explorer

        clean_children(explorer.Find("chosen"));

        for (int i = 0; i < get_im_paths().Count; i++)
        {
            add_chosen_block(i);
        }
    }
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

    public void set_im_files(List<string> files)
    {
        this.im_paths = files;
    }
    public List<string> get_im_files()
    {
        return this.im_paths;
    }
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
    public bool get_with_exp()
    {
        return this.with_exp;
    }
    public void set_is_started(bool val)
    {
        this.is_started = val;

        if (val)
        {
            // info (paul): prepare for start
            exp_cv_acts = set_up_render_list();
        }
    }
    public bool get_is_started()
    {
        return this.is_started;
    }
    //05092024 public void set_render_idx(int value)
    //05092024 {
    //05092024     this.render_idx = value;
    //05092024 }
    //05092024 public int get_render_idx()
    //05092024 {
    //05092024     return this.render_idx ;
    //05092024 }
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

    public void clean_blades()
    {
        // info (paul): remove previously generated blades from the sample
        GameObject blades = GameObject.Find("blades");
        remove_children(blades);

        this.set_visible(false);
    }
    public GameObject start_renders(string blade_path, int blade_idx = -1, bool with_uv_init = false)
    {
        // info (paul):
        clean_blades();

        // info (paul): Start the machinery of creating and rendering all the blades
        GameObject surface_obj = load_blade_from_verts(blade_path: blade_path,
            blade_idx: blade_idx, with_uv_init: with_uv_init, with_collider: false);
        return surface_obj;
    }

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

    public int get_match_steps()
    {
        return match_steps;
    }

    public void set_match_steps(int input)
    {
        match_steps = input;
        //dt_compare = match_steps;
    }

    public void set_ready_for_next_blade(bool input)
    {
        ready_for_next_blade = input;
    }
    public bool get_ready_for_next_blade()
    {
        return ready_for_next_blade;
    }

    public bool get_visible()
    {
        return is_visible;
    }
    public void set_visible(bool input)
    {
        is_visible = input;
    }

    public string blade_path_for_idx(int idx)
    {
        // info (paul): dir from lsdyna: C:\Users\go73jem\Desktop\nakajima_full\stl\
        // info (paul): "idx+1", weil 1 entspricht 0, 2 entspricht 1, etc. 
        string blade_dir = path_base + "/play_blender_pycahrm/write_mesh/verts_" +
            idx.ToString() + ".txt";
        //25102024 (idx+1).ToString() + ".txt";
        return blade_dir;
    }

    public void set_path_stereo(string new_path)
    {
        this.path_stereo = new_path;
    }

    public string get_path_stereo()
    {
        return path_stereo;
    }
    //public void set_path_time_flow(string new_path)
    //{
    //    this.path_time_flow_u = new_path;
    //}

    public string get_path_time_flow_u()
    {
        return path_time_flow_u;
    }

    public void set_path_time_flow_u(string new_path)
    {
        this.path_time_flow_u = new_path;
    }

    public string get_path_time_flow_v()
    {
        return path_time_flow_v;
    }

    public void set_path_time_flow_v(string new_path)
    {
        this.path_time_flow_v = new_path;
    }
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

    public void init_t_control(T_control_script t_control_input)
    {
        this.t_control = t_control_input;
    }
    public void init_match_control(Match_steps match_control)
    {
        this.match_control = match_control;
    }

    public void init_strain_control(Strain input)
    {
        this.strain_control = input;
    }
    public void init_u_v_control(U_V input)
    {
        this.u_v_control = input;
    }
    public void init_experiment_control(Experiment_Control input)
    {
        this.experiment_control = input;
    }
    public void init_strain_d_control(Strain_D input)
    {
        this.strain_d = input;
    }

    public Vector2[] get_uv_start()
    {
        return this.uv_start;
    }
    public void set_uv_start(Vector2[] input)
    {
        this.uv_start = input;
    }
    public void set_force_flat(bool force_flat_input)
    {
        force_flat = force_flat_input;
    }
    public void set_res_x(int res_x_input)
    {
        res_x = res_x_input;
    }
    public void set_res_y(int res_y_input)
    {
        res_y = res_y_input;
    }
    public void set_im_cnt(int im_cnt_input)
    {
        im_cnt = im_cnt_input;
    }
    public void set_t_idx(int t_idx_input)
    {
        t_idx = t_idx_input;
    }
    public void set_strain_mode(string input)
    {
        strain_mode = input;
    }
    public void set_u_v_mode(string input)
    {
        u_v_mode = input;
    }
    public void set_strain_d_mode(string input)
    {
        strain_d_mode = input;
    }

    public string get_u_v_mode()
    {
        return u_v_mode;
    }

    public bool get_force_flat()
    {
        return force_flat;
    }
    public int get_res_x()
    {
        return res_x;
    }
    public int get_res_y()
    {
        return res_y;
    }
    public int get_im_cnt()
    {
        return im_cnt;
    }
    public int get_t_idx()
    {
        return t_idx;
    }
    public string get_strain_mode()
    {
        return strain_mode;
    }
    public string get_strain_d_mode()
    {
        return strain_d_mode;
    }

    public void save_all()
    {
        // info (paul): save all images, that the paper may need, in the corresponding directories

        //plot_mode = "value_ref";////"loss_rel""loss_abs""value""value_ref"""
        //heights_mode = "value";//"value""value_ref""loss_abs""loss_rel"""
        //paint_with = "uv";//"uv";"heights"

        refresh_plane_with_params();
    }

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
    public void refresh_plane_with_params(
        string path_time_flow_v = null,
        int t_idx = -1, bool with_save = false)
    {
        // info (paul): init params
        (path_time_flow_v, t_idx) = refresh_params(path_time_flow_v, t_idx);
        (List<List<float>> heights, List<List<float>> heights_chosen, float scale_factor) = manage_heights();
        (Texture2D flow_tex, List<List<float>> mat) = manage_flow(path_time_flow_v, path_time_flow_v,
            heights, heights_chosen);

        GameObject platine_plane = null;
        (platine_plane, _) = make_platine_plane(heights_chosen, null,
            flow_tex, force_flat: force_flat, scale_factor: scale_factor,
            with_save: with_save);

        manage_assign(platine_plane, t_idx);
    }

    public void manage_assign(GameObject platine_plane, int t_idx)
    {
        assign_to_cam(platine_plane);

        // info (paul): assign to t_panel
        if (t_control != null)
        {
            match_control.refresh_panel(t_idx);
            t_control.refresh_t_panel(t_idx);
            strain_control.refresh_info(strain_mode);
            u_v_control.refresh_info(u_v_mode);
            strain_d.refresh_info(strain_d_mode);
            experiment_control.refresh_info(experiment);
        }
    }

    public (string, int) refresh_params(string path_time_flow_v, int t_idx)
    {
        //13012025 if (path_stereo == null) { path_stereo = this.get_path_stereo(); }
        //13012025 if (path_time_flow_u == null) { path_time_flow_u = this.get_path_time_flow_u(); }
        if (path_time_flow_v == null) { path_time_flow_v = this.get_path_time_flow_v(); }
        //if (res_x == -1) { res_x = this.res_x; }
        //if (res_y == -1) { res_y = this.res_y; }
        //13012025 if (im_cnt == -1) { im_cnt = this.im_cnt; }
        if (t_idx == -1) { t_idx = this.get_t_idx(); }
        if (get_blade_tris() == null) { set_blade_tris(init_tris_empty()); }
        return (path_time_flow_v, t_idx);
    }
    public (List<List<float>>, List<List<float>>, float) manage_heights()
    {
        // info (paul): heights/ heights_ref/ heights_diff
        bool force_flat = this.get_force_flat();

        //08102024 List<List<float>> heights = read_dists(path_stereo, t_idx);
        (List<List<float>> heights, List<List<float>> heights_uncut) = read_heights_tv();//26022025 trivial_heights();//11012024 read_heights_tv();
        (List<List<float>> heights_ref, List<List<float>> heights_ref_uncut) = load_heights_ref();//26022025 trivial_heights();//11012024 load_heights_ref();
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

    public (List<List<float>>, List<List<float>>) trivial_heights()
    {
        int render_res = get_render_res();
        List<List<float>> mat_0 = zeros_of_size(render_res, render_res);
        List<List<float>> mat_1 = zeros_of_size(render_res, render_res);
        return (mat_0, mat_1);
    }
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
        float[][] mat_u_pre_pre_pre = load_floats2(full_path: path_heights);
        List<List<float>> mat_u_pre_pre = floats2_to_lists(mat_u_pre_pre_pre);
        return mat_u_pre_pre;
    }
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
    public string get_heights_mode()
    {
        return this.heights_mode;
    }

    public void set_heights_mode(string input, bool is_internal = false)
    {
        this.heights_mode = input;
        if (!is_internal)
        {
            set_paint_with("heights");
            //set_plot_mode("", is_internal: true);
        }
    }

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

    public void clean_platine_plane()
    {
        GameObject plane = GameObject.Find("platine_plane");
        if (plane != null)
        {
            remove_obj(plane);
        }
    }
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
    public void remove_obj(GameObject obj)
    {
        // info (paul): destroy platine plane
        Destroy(obj);
    }

    public void assign_to_cam(GameObject platine_plane)
    {
        GameObject cam_obj = GameObject.Find("MainCamera");
        GameObject cam_exp_obj = GameObject.Find("exp_cam");
        cam_script = cam_obj.GetComponent<Cam_manager>();
        cam_exp = cam_exp_obj.GetComponent<Cam_manager>();

        // info (paul): assign the object to the camera:
        cam_script.platine_plane = platine_plane.transform;
        cam_exp.platine_plane = platine_plane.transform;
    }

    public void log(string text)
    {
        log_field.GetComponent<TextMeshProUGUI>().text += text;
    }

    // Update is called once per frame

    // info (paul): for the speckle rendering
    int speckle_idx = 0;
    private int reg_idx = 0;

    void Update()
    {
        if (is_started && !get_series())
        {
            //update_render_acts();

            if (speckle_idx < speckle_acts.Count)
            {
                this.speckle_size = speckle_acts[speckle_idx].Item2;
                this.speckle_dist = speckle_acts[speckle_idx].Item3;
                speckle_acts[speckle_idx].Item1.Invoke();
                speckle_idx += 1;
            }

            if (get_reg_idx() < exp_cv_acts.Count)
            {
                add_to_reg_idx(1);

                // info (paul): execute act from actioner
                exp_cv_acts[get_reg_idx() - 1].act.Invoke();
            }
        }
        else if (is_started && get_series())
        {
            List<string> im_paths = this.get_im_paths();
            //cv_for_paths(path0: im_paths[0], path1: im_paths[1], -1, d_cam: 0, with_dt: true, 
            //    pars: pars);
            string in_dir = this.get_in_dir();
            string out_dir = this.get_out_dir();

            (series_idx, is_started) = cv_series(im_paths, in_dir: in_dir, 
                out_dir: out_dir, series_idx: series_idx, is_started: is_started);
        }

        GameObject platine_plane = GameObject.Find("platine_plane");
        if (platine_plane != null)
        {
            platine_plane.GetComponent<MeshRenderer>().material.color = Color.white;
        }

    }

    public int get_reg_idx()
    {
        return reg_idx;
    }

    public void set_reg_idx(int input)
    {
        reg_idx = input;
    }

    public void add_to_reg_idx(int value)
    {
        int reg_idx = get_reg_idx();
        set_reg_idx(reg_idx + value);
    }

    public void update_blade_pics_if()
    {
        //23092024 bool blades_created = get_blades_created();
        //23092024 if (blades_created)
        //23092024 {
        //23092024     update_blade_pics();
        //23092024 }
    }

    public void start_speckle_0_035()
    {
        start_speckle(diameter: 0.035f);
    }
    public void start_speckle_0_07()
    {
        start_speckle(diameter: 0.07f);
    }
    public void start_speckle_0_35()
    {
        start_speckle(diameter: 0.35f);
    }
    public void start_speckle_0_175()
    {
        start_speckle(diameter: 0.175f);
    }
    public void start_speckle_0_7()
    {
        start_speckle(diameter: 0.7f);
    }
    public void start_speckle_1_4()
    {
        start_speckle(diameter: 1.4f);
    }
    public void start_speckle_2_1()
    {
        start_speckle(diameter: 2.1f);
    }
    public void start_speckle_2_8()
    {
        start_speckle(diameter: 2.8f);
    }
    public void start_speckle_7_0()
    {
        start_speckle(diameter: 7.0f);
    }
    public void start_speckle_14_0()
    {
        start_speckle(diameter: 14.0f);
    }

    public void start_speckle_05()
    {
        start_speckle(diameter: 0.5f);
    }
    public void start_speckle_1()
    {
        start_speckle(diameter: 1f);
    }
    public void start_speckle_2()
    {
        start_speckle(diameter: 2f);
    }
    public void start_speckle_4()
    {
        start_speckle(diameter: 4f);
    }
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
    public void clean_symbols()
    {
        GameObject symbols = GameObject.Find("cams_symbols");
        remove_children(symbols);
    }

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
        set_experiment("speckle_" + config.diameter.ToString("0.000") + config.label);//
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
        set_up_lighting(y_coord: 30f, intensity: config.get_ambient_intensity());

        // info (paul): set up cams (in the old system it was done in Start()
        set_up_cams(config);

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


    public void start_lighting_0001()
    {
        start_lighting(intensity: 0.001f);
    }
    public void start_lighting_001()
    {
        start_lighting(intensity: 0.01f);
    }
    public void start_lighting_002()
    {
        start_lighting(intensity: 0.02f);
    }
    public void start_lighting_003()
    {
        start_lighting(intensity: 0.03f);
    }
    public void start_lighting_0032()
    {
        start_lighting(intensity: 0.032f);
    }
    public void start_lighting_0034()
    {
        start_lighting(intensity: 0.034f);
    }
    public void start_lighting_00345()
    {
        start_lighting(intensity: 0.0345f);
    }
    public void start_lighting_0035()
    {
        start_lighting(intensity: 0.035f);
    }
    public void start_lighting_00355()
    {
        start_lighting(intensity: 0.0355f);
    }
    public void start_lighting_0036()
    {
        start_lighting(intensity: 0.036f);
    }
    public void start_lighting_0038()
    {
        start_lighting(intensity: 0.038f);
    }
    public void start_lighting_004()
    {
        start_lighting(intensity: 0.04f);
    }
    public void start_lighting_005()
    {
        start_lighting(intensity: 0.05f);
    }
    public void start_lighting_01()
    {
        start_lighting(intensity: 0.1f);
    }
    public void start_lighting_02()
    {
        start_lighting(intensity: 0.2f);
    }
    public void start_lighting_05()
    {
        start_lighting(intensity: 0.5f);
    }
    public void start_lighting_07()
    {
        start_lighting(intensity: 0.7f);
    }
    public void start_lighting_08()
    {
        start_lighting(intensity: 0.8f);
    }
    public void start_lighting_1()
    {
        start_lighting(intensity: 1f);
    }
    public void start_lighting_2()
    {
        start_lighting(intensity: 2f);
    }
    public void start_lighting_3()
    {
        start_lighting(intensity: 3f);
    }
    public void start_lighting_4()
    {
        start_lighting(intensity: 4f);
    }
    public void start_lighting_5()
    {
        start_lighting(intensity: 5f);
    }
    public void start_lighting_6()
    {
        start_lighting(intensity: 6f);
    }
    public void start_lighting_10()
    {
        start_lighting(intensity: 10f);
    }
    public void start_lighting_100()
    {
        start_lighting(intensity: 100f);
    }
    public void start_lighting_10000()
    {
        start_lighting(intensity: 10000f);
    }
    public void start_lighting_100000000()
    {
        start_lighting(intensity: 100000000f);
    }

    public void start_speckle_0_035_heights()
    {
        start_speckle(diameter: 0.035f, fov: heights_fov, label: "_heights");
    }
    public void start_speckle_0_07_heights()
    {
        start_speckle(diameter: 0.07f, fov: heights_fov, label: "_heights");
    }
    public void start_speckle_0_175_heights()
    {
        start_speckle(diameter: 0.175f, fov: heights_fov, label: "_heights");
    }
    public void start_speckle_0_35_heights()
    {
        start_speckle(diameter: 0.35f, fov: heights_fov, label: "_heights");
    }
    public void start_speckle_0_7_heights()
    {
        start_speckle(diameter: 0.7f, fov: heights_fov, label: "_heights");
    }


    public void start_lighting_001_heights()
    {
        start_lighting(intensity: 0.01f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_002_heights()
    {
        start_lighting(intensity: 0.02f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_003_heights()
    {
        start_lighting(intensity: 0.03f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_004_heights()
    {
        start_lighting(intensity: 0.04f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_005_heights()
    {
        start_lighting(intensity: 0.05f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_01_heights()
    {
        start_lighting(intensity: 0.1f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_02_heights()
    {
        start_lighting(intensity: 0.2f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_05_heights()
    {
        start_lighting(intensity: 0.5f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_07_heights()
    {
        start_lighting(intensity: 0.7f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_08_heights()
    {
        start_lighting(intensity: 0.8f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_1_heights()
    {
        start_lighting(intensity: 1f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_2_heights()
    {
        start_lighting(intensity: 2f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_3_heights()
    {
        start_lighting(intensity: 3f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_4_heights()
    {
        start_lighting(intensity: 4f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_5_heights()
    {
        start_lighting(intensity: 5f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_6_heights()
    {
        start_lighting(intensity: 6f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_10_heights()
    {
        start_lighting(intensity: 10f, fov: heights_fov, label: "_heights");
    }
    public void start_lighting_100_heights()
    {
        start_lighting(intensity: 100f, fov: heights_fov, label: "_heights");
    }


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
        string blade_path_first = blade_path_for_idx(blade_idxs[0]);//24102024A (blade_idx_min);

        // info (paul): set up lighting
        set_up_lighting(y_coord: pars.get_lighting_pos_y(), intensity: pars.get_lighting_intensity());

        // info (paul): set up speckle texture
        string speckle_file = "speckle_" + pars.get_speckle_size().ToString("0.000");//(diameter).ToString();
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
    public void set_below_max(bool input)
    {
        this.below_max = input;
    }
    public bool get_below_max()
    {
        return below_max;
    }


    public List<Actioner> set_up_render_list()
    {
        // info (paul): make the individual render actions as a list

        List<Actioner> acts = new List<Actioner>();
        acts = add_im_steps(acts);

        return acts;
    }
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

    public IEnumerator cv_routine()
    {
        manage_cv_act();
        yield return null;
    }
    public void manage_cv_act()
    {
        Actioner current_act = exp_cv_acts[get_reg_idx() - 1];
        int cv_idx = current_act.get_cv_render_idx();
        Params pars = current_act.pars;

        manage_cv(cv_idx, pars);
        //string info_l = "reg_idx - 1: " + (get_reg_idx() - 1).ToString();
        string info_l = "cv_idx: " + cv_idx.ToString();
        Debug.Log(info_l);
    }

    public void manage_cv(int cv_idx, Params pars)
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
                cv_main(cv_idx, d_cam: 0, with_dt: true, exp_label: exp_l, pars: pars);
            }

            //06122024 }
            set_experiment("exp_normal");

            //08102024 call_main_batch();// - seems to currently contain no matlab, only TV
            try
            {
                refresh_plane_with_params();
                cam_script.start_for_platine();
            }
            catch
            {
                ;
            }
        }

        done_ground_truth = true;
    }

    public void exe_render_acts()
    {
        this.render_acts[render_idx].act.Invoke();

        string progress_info = "progress: " + render_idx.ToString() + " / " +
            render_acts.Count.ToString();
        Debug.Log(progress_info);
        render_idx += 1;
    }
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

            bool with_proj = false;
            if (category_muc == "gom_curve")
            {
                with_proj = false;
            }
            if (category_muc == "simple")
            {
                with_proj = true;
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
    public void set_blades(List<GameObject> input)
    {
        this.blades = input;
    }
    public List<GameObject> get_blades()
    {
        return blades;
    }
    public void save_tris(int blade_idx)
    {
        // info (paul): save triangle idxs

        List<GameObject> blades = collect_blades();

        (int[,] tris, float[][][] barys) = im2triangles(cam: cam_for_uv_0);

        // info (paul): cam_idx is 0, because is irrelevant here anyway
        save_ints2_for_blade(tris, cam_idx: 0, blade_idx: blade_idx, label: "_tris", with_uv_mode: false);
        save_floats3_for_blade(barys, cam_idx: 0, blade_idx: blade_idx, label: "_barys", with_uv_mode: false);
    }
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
    public (float[], float[], float[]) load_distortion_ground_truth(int blade_idx)
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

        if (true)//13072024 (blade_tris.Count < blade_idx_max - blade_idx_min)
        {
            //24092024 blade_tris[blade_idx] = blade_tris_i;
            set_blade_tris_at(blade_idx, blade_tris_i);
        }

        set_experiment(current_exp);

        return (d_xs, d_ys, d_zs);
    }
    public void set_blade_tris_at(int blade_idx, int[] slice)
    {
        List<int[]> blade_tris = get_blade_tris();
        blade_tris[blade_idx] = slice;
        set_blade_tris(blade_tris);
    }
    public void set_blade_tris(List<int[]> input)
    {
        this.blade_tris = input;
    }
    public List<int[]> get_blade_tris()
    {
        return this.blade_tris;
    }
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

    public void aaaaa()
    {
        ;

        ;
    }

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

    public List<float> zeros_like(List<float> input)
    {
        List<float> vals = new List<float>();
        for (int i = 0; i < input.Count; i++)
        {
            vals.Add(0f);
        }
        return vals;
    }

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
    public List<float> zeros_of_size(int num)
    {
        List<float> empty = new List<float>();

        for (int i = 0; i < num; i++)
        {
            empty.Add(0f);
        }
        return empty;
    }
    public List<double> doubles_of_size(int num)
    {
        List<double> empty = new List<double>();

        for (int i = 0; i < num; i++)
        {
            empty.Add(0f);
        }
        return empty;
    }
    public List<int> ints_of_size(int num)
    {
        List<int> empty = new List<int>();

        for (int i = 0; i < num; i++)
        {
            empty.Add(0);
        }
        return empty;
    }
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


    public (float, float, float, float) el2vals(List<List<float>> mat, int width_idx, int height_idx)
    {
        float disp_ij = mat[width_idx][height_idx];

        float x_l = (float)width_idx;
        float x_r = (float)(x_l) + disp_ij;
        float span = (float)mat.Count;
        return (disp_ij, x_l, x_r, span);
    }


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



    public (GameObject, List<List<(float, float)>>, int, int) make_plane_with_mesh(Mesh mesh,
        List<List<(float, float)>> points, Texture2D flow_tex,
        bool with_save = false)
    {
        Material province_mat;
        (province_mat, points, res_x, res_y) = manage_material(
            points, flow_tex: flow_tex, with_save: with_save);

        GameObject plane2 = GameObject.Find("platine_2");
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
    public void set_speckle_file(string input)
    {
        this.speckle_file = input;
    }
    public string get_speckle_file()
    {
        return this.speckle_file;
    }

    public void apply_speckles(GameObject obj, string file_name = "speckle_pattern")
    {
        //23092024 string mat_file = "Targets/fbx_files/Materials/" + remove_dots(get_speckle_file());
        string mat_file = "Targets/fbx_files/Materials/" + get_speckle_file();
        Material speckle_mat = (Material)(Resources.Load(mat_file));

        obj.GetComponent<Renderer>().material = speckle_mat;
    }


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

    public bool in_boundaries(int idx_x, int idx_y, int padding = 10)
    {
        bool cond_1 = idx_x > padding;
        bool cond_2 = idx_x < get_render_res() - padding;
        bool cond_3 = idx_y > padding;
        bool cond_4 = idx_y < get_render_res() - padding;

        bool is_in = cond_1 && cond_2 && cond_3 && cond_4;

        return is_in;
    }

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

        var script = "/Users/Paul/Desktop/DIC_2025_for_travel/play_blender_pycahrm/write_mesh/txt2csv.py";//path_project + "Assets/txt2csv.py";
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
    public List<List<float>> manage_image_noise(List<List<float>> image, Params pars)
    {
        //if (exp_cv_acts[get_reg_idx() - 1].get_label().EndsWith("gauss_3"))
        //{
        //;
        //}

        List<List<float>> image_1 = manage_gauss_noise(image, gaussian_error: pars.get_gaussian_error());
        List<List<float>> image_2 = image_1;//23022025 manage_poisson_noise(image_1, poisson_error: pars.get_poisson_error());
        if (get_reg_idx() - 1 == 10)
        {
            write_mat_for_debug(image_2, scale: 1f);
        }
        return image_2;
    }

    public List<List<float>> manage_gauss_noise(List<List<float>> image, float gaussian_error = 1f)
    {
        if (gaussian_error > 2f)
        {
            ;
        }

        int len_val = image.Count;
        List<List<float>> image_1 = copy_mat(image);

        for (int i = 0; i < len_val; i++)
        {
            for (int j = 0; j < len_val; j++)
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

    public List<List<float>> manage_poisson_noise(List<List<float>> image, float poisson_error = 1f)
    {
        int len_val = image.Count;
        List<List<float>> image_1 = copy_mat(image);

        for (int i = 0; i < len_val; i++)
        {
            for (int j = 0; j < len_val; j++)
            {
                float rand = UnityEngine.Random.Range(-1f, 1f);
                image_1[i][j] = image[i][j] + rand * poisson_error * image[i][j];
            }
        }

        return image_1;
    }

    public void save_current_flow(Texture2D flow_tex, bool with_save)
    {
        if (with_save)
        {
            string active_mode = find_active_mode();
            string label_l = "_" + paint_with.ToString() + "_" + active_mode.ToString();

            save_tex(flow_tex);
            Actioner current_act = exp_cv_acts[get_reg_idx() - 1];//23022025 t_idx];
            Params pars = current_act.pars;
            save_png(flow_tex, cam_idx: 0, blade_idx: t_idx, label: label_l, pars: pars);
        }
    }

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

    public List<float> multiply(List<float> input, float factor)
    {
        for (int i = 0; i < input.Count; i++)
        {
            input[i] *= factor;
        }
        return input;
    }
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

    public (List<List<float>>, int, int, List<List<float>>) load_and_construct_flow(
        string path_time_flow_u, string path_time_flow_v,
        int res_x, int res_y, int im_cnt = -1, int t_idx = -1)
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
                    dir_info_v, im_cnt: im_cnt, t_idx: t_idx);
            }
        }

        return (flow_mat, res_x, res_y, stream_z);
    }

    public Dictionary<string, Dictionary<int, FileInfo>> select_flow_files(FileInfo[] dir_info_u,
        FileInfo[] dir_info_v)
    {
        // info (paul): We assume, that the datafile is 
        //      has a name like "time_flow_u_172.png"

        List<FileInfo> flow_files = new List<FileInfo>();
        Dictionary<string, Dictionary<int, FileInfo>> flow_dic = new Dictionary<string, Dictionary<int, FileInfo>>();
        flow_dic.Add("u", new Dictionary<int, FileInfo>());
        flow_dic.Add("v", new Dictionary<int, FileInfo>());

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

    public bool is_useful(float value)
    {
        bool is_useful = (value != 0f && !float.IsNaN(value) && !float.IsInfinity(value));
        return is_useful;
    }
    public bool is_meaningful(float value)
    {
        bool meaningful = (!float.IsNaN(value) && !float.IsInfinity(value));
        return meaningful;
    }
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

    public (List<List<float>>, int, int, List<List<float>>) find_total_match_tex(
        FileInfo[] dir_info_u, FileInfo[] dir_info_v,
        int im_cnt = -1, int t_idx = -1)
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

        (flow_mats_u, flow_mats_v, texs_albedo_u,
            texs_albedo_v, res_x, res_y) = find_flow_mats(
            flow_files, im_cnt: im_cnt);// info (paul): somewhat performance heavy

        // info (paul): scale flow mats for debugging reasons
        (flow_mats_u, flow_mats_v) = scale_flows(flow_mats_u, flow_mats_v,
            scale_fac: this.flow_scale_factor);

        (List<List<float>> flow_mat, List<List<float>> stream_z) =
            manage_match_to_start(flow_mats_u, flow_mats_v, t_idx,
            texs_albedo_u, texs_albedo_v);

        //tex = texs_albedo_u[0];

        // info (paul): average to smoothen
        // 17032025ABC
        List<List<float>> flow_mat_smoothed = smoothen_frame(flow_mat);
        List<List<float>> flow_mat_mean = filter_mean_comp(flow_mat_smoothed);

        return (flow_mat_mean, res_x, res_y, stream_z);
    }

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

    public List<List<float>> manage_load_csv()
    {
        string path = path_dic + "exp_normal/time_flow_v/flow_v_csv.csv";
        //15112024 string path = "C:/Users/go73jem/Desktop/DIC_package/exp_normal/time_flow_v/flow_v_csv_28_29.csv";

        List<List<float>> mat_val = read_dists(path, direct_access: true);

        return mat_val;
    }
    public (List<List<float>>, List<List<float>>) manage_truth_flow(GameObject sample_1, GameObject sample_2, Camera cam)
    {
        (List<List<float>> stream_u, List<List<float>> stream_v) = find_truth_flow(sample_1, sample_2, cam);
        return (stream_u, stream_v);
    }
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

                int idx_val = tri_idx[i, get_render_res() - j - 1];//11012024 512 - j - 1
                float[] bary = barys[i][get_render_res() - j - 1];//11012024 512 - j - 1

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

    public (List<List<float>>, List<List<float>>) manage_match_to_start(
        List<List<List<float>>> flow_mats_u, List<List<List<float>>> flow_mats_v,
        int t_idx, List<Texture2D> texs_albedo_u, List<Texture2D> texs_albedo_v)
    {
        // info (paul): find uv flow from start
        (List<List<float>> flow_u, List<List<float>> flow_v) = find_accum_flow(flow_mats_u,
            flow_mats_v, t_idx);

        // info (paul): add matlab ref flow:
        if (use_ncorr)//(get_plot_mode() == "value")
        {
            List<List<float>> loaded = manage_load_csv();
            List<List<float>> scaled = scale_res(loaded);
            List<List<float>> transposed = transpose_mat(scaled);
            flow_v = transposed;
        }

        (List<List<float>> stream_u, List<List<float>> stream_v, List<List<float>> stream_z, float coverage) =
            manage_flow_or_loss(flow_u, flow_v, t_idx);

        // info (paul): scale label
        (float stream_u_min, float stream_u_max) = find_min_max(stream_u, with_padding: true);//11112024 
        (float stream_v_min, float stream_v_max) = find_min_max(stream_v, with_padding: true);//11112024 
        (float stream_u_mean, float dev_u) = find_mean_in_all(stream_u, span: 20, coverage: coverage);
        (float stream_v_mean, float dev_v) = find_mean_in_all(stream_v, span: 20, coverage: coverage);
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
        List<List<float>> flow_mat = to_tex_if(flow_mats_chosen, texs_albedo_u,
            texs_albedo_v);


        (float min_val, float max_val) = find_min_max(flow_mat, with_padding: true);
        _ = 1 + 1;

        return (flow_mat, stream_z);
    }

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
    public (List<List<float>>, List<List<float>>) find_accum_flow(List<List<List<float>>> flow_mats_u,
        List<List<List<float>>> flow_mats_v, int t_idx)
    {
        // info (paul): match points
        List<List<(float, float)>> points_now = match_all_to_start(flow_mats_u, flow_mats_v,
                t_min: 0, t_max: t_idx);
        //14012025    t_min: t_idx, t_max: t_idx + get_match_steps());// info (paul): very performance heavy

        // info (paul): subtract mean, normalize etc. partly for nice visualization
        List<List<(float, float)>> points_normed = treat_nice(points_now);

        (List<List<float>> stream_u, List<List<float>> stream_v) = points2flow(points_normed);//12062024 points_normed);
        return (stream_u, stream_v);
    }

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

    public List<List<float>> get_ref_u()
    {
        return ref_u;
    }

    public List<List<float>> get_ref_v()
    {
        return ref_v;
    }

    public List<List<float>> get_u()
    {
        return value_u;
    }

    public List<List<float>> get_v()
    {
        return value_v;
    }
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
    public (List<List<float>>, List<List<float>>, List<List<float>>, float) manage_flow_or_loss(
        List<List<float>> flow_u, List<List<float>> flow_v, int t_idx)
    {
        // info (paul): wording: "flow" denotes the disparity components u/v of the optical flow, 
        //      while "stream" might also be the loss

        // info (paul): we assume, that a certain t_idx corresponds to a certain blade
        (float[] d_xs, float[] d_ys, float[] d_zs) = load_distortion_ground_truth(blade_idx: t_idx);
        //(float[] screen_x, float[] screen_y) = load_screen_poss();
        (int[,] tri_idx, float[][][] barys) = load_tris(blade_idx: 0);//Bt_idx);

        List<List<float>> tri_idx_floats = ints2floats(tri_idx);

        (List<List<float>> stream_u, List<List<float>> stream_v, List<List<float>> stream_z,
            float coverage) = choose_flow_or_loss(
            flow_u, flow_v, tri_idx, barys, d_xs, d_ys, d_zs);

        // info (paul): save value ref matrix, which is later needed for the ground truth
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

        return (stream_u, stream_v, stream_z, coverage);
    }

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

                int idx_val = tri_idx[i, get_render_res() - j - 1];//11012024 512 - j - 1
                float[] bary = barys[i][get_render_res() - j - 1];//11012024 512 - j - 1

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
                    (stream_u[i][j], stream_v[i][j], stream_z[i][j]) = (0f, 0f, 0f);
                }
            }
        }

        float coverage = (float)(nonzero_counter) / (float)20239;//(float)(counter);//((float)(counter))/((float)(i_max* j_max));
        return (stream_u, stream_v, stream_z, coverage);
    }

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

        set_v_mean(loss_v_mean);
        set_v_std(loss_v_std);
        set_v_min(loss_v_min);
        set_v_max(loss_v_max);

        write_info(loss_u_mean, loss_u_std, loss_u_min, loss_u_max,
            loss_v_mean, loss_v_std, loss_v_min, loss_v_max);
    }

    public void set_v_mean(float input)
    {
        v_mean_global = input;
    }
    public void set_v_std(float input)
    {
        v_std_global = input;
    }
    public void set_v_min(float input)
    {
        v_min_global = input;
    }
    public void set_v_max(float input)
    {
        v_max_global = input;
    }
    public float get_v_mean()
    {
        return v_mean_global;
    }
    public float get_v_std()
    {
        return v_std_global;
    }
    public float get_v_min()
    {
        return v_min_global;
    }
    public float get_v_max()
    {
        return v_max_global;
    }
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
    }

    int i_test = 330;
    int j_test = 230;

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
    public float curb_err(float value, float threshold)
    {
        value = Mathf.Clamp(value, value, threshold);
        return value;
    }

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

        float threshold = 1f;//14112024 10f; //12112024 2f;//11112024 0.1f;//27092024 0.1f;//0.1f;
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

    public string get_plot_mode()
    {
        return plot_mode;
    }
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
    public void set_paint_with(string value)
    {
        this.paint_with = value;
    }

    public string get_paint_with()
    {
        return this.paint_with;
    }

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

    public List<List<(float, float)>> norm_points(List<List<(float, float)>> points_fluc)
    {
        (List<List<float>> points_x, List<List<float>> points_y) = split_match(points_fluc);//points_fluc);
        points_x = norm_mat(points_x);
        points_y = norm_mat(points_y);
        List<List<(float, float)>> points_normed = merge(points_x, points_y);
        return points_normed;
    }

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
    public List<List<float>> filter_mean_comp(
    List<List<float>> points_now)
    {
        List<List<float>> points_next = zeros_of_size(points_now.Count,
            points_now.Count);

        int padding = 5;

        for (int i = padding; i < points_now.Count - padding; i++)
        {
            for (int j = padding; j < points_now[0].Count - padding; j++)
            {
                float item_1 = plain_conv_comp(points_now, i, j);

                points_next[i][j] = (item_1);
            }
        }

        return points_next;
    }
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

    public bool check_if_in_frame(List<List<float>> mat, (int, int) point)
    {
        int len_x = mat.Count;
        int len_y = mat[0].Count;

        bool x_in_range = (0 <= point.Item1 && point.Item1 < len_x);
        bool y_in_range = (0 <= point.Item2 && point.Item2 < len_y);

        bool in_frame = x_in_range && y_in_range;

        return in_frame;
    }

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

        //21102024 for (int t_idx = 0; t_idx < t_cnt; t_idx++)
        for (int idx = 0; idx < blade_idxs.Count - 1; idx++)
        {
            //22102024 set_t_idx(blade_idxs[idx]);
            //23102024 set_t_idx(idx);
            List<List<float>> mat_u = load_flow_u(flow_files, keys_u, texs_albedo_u, blade_idx: idx);

            //17072024 mat_u = filter_mean_comp(mat_u);//17072024 mean to smooth errors

            mats_u.Add(mat_u);

            List<List<float>> mat_v = load_flow_v(flow_files, keys_v, texs_albedo_v, blade_idx: idx);

            //17072024 mat_v = filter_mean_comp(mat_v);

            //mat_v = take_share(mat_v);
            mats_v.Add(mat_v);

        }

        return (mats_u, mats_v, texs_albedo_u, texs_albedo_v, res_x, res_y);
    }

    public List<List<float>> load_flow_u(Dictionary<string, Dictionary<int, FileInfo>> flow_files,
        List<int> keys_u, List<Texture2D> texs_albedo_u, int blade_idx = -1)
    {
        //23102024A int key_u = keys_u[blade_idx];
        int key_v = blade_idxs[blade_idx];

        string file_path_u = null;
        try
        {
            file_path_u = flow_files["u"][key_v].FullName;
        }
        catch
        {
            file_path_u = flow_files["u"][key_v].FullName;
        }

        // info (paul): new way to read out data:
        string base_path = path_dic + "exp_normal/";
        file_path_u = base_path + "time_flow_u/time_flow_u_" + key_v.ToString() + "_r" + get_render_res().ToString() + ".png";
        byte[] im_bytes_u = System.IO.File.ReadAllBytes(file_path_u);

        DirectoryInfo dir_v = new DirectoryInfo(path_time_flow_v);
        FileInfo[] dir_info_v = dir_v.GetFiles("*");

        // info (paul): assuming, that the resolution of the first image is 
        //      the resolution of all the images
        if (true)//20062024 (t_idx == 0)
        {
            (res_x, res_y) = bytes2res(im_bytes_u);
        }

        Texture2D tex_albedo_u = new Texture2D(res_x, res_y);
        texs_albedo_u.Add(tex_albedo_u);
        tex_albedo_u.LoadImage(im_bytes_u);
        List<List<float>> mat_u = tex2mat(tex_albedo_u);

        // info (paul): find min and max val
        string min_max_file = path_dic +
            remove_dots(get_experiment()) + "/min_max_u_" + key_v.ToString() + ".txt";//23102024 blade_idx //23102024 t_idx //22102024 blade_idx
        string min_max_str = load_txt_line(min_max_file);
        string[] strs = min_max_str.Split(" ");
        float min_val = float.Parse(strs[0]);
        float max_val = float.NaN;
        max_val = float.Parse(strs[1]);
        mat_u = unnorm_mat(mat_u, min_val, max_val);

        return mat_u;

    }

    public List<List<float>> load_flow_v(Dictionary<string, Dictionary<int, FileInfo>> flow_files,
        List<int> keys_v, List<Texture2D> texs_albedo_v, int blade_idx = -1)
    {
        //23102024A int key_v = keys_v[blade_idx];
        int key_v = blade_idxs[blade_idx];
        string file_path_v = flow_files["v"][key_v].FullName;

        // info (paul): new way to read out data:
        string base_path = path_dic + "exp_normal/";
        file_path_v = base_path + "time_flow_v/time_flow_v_" + key_v.ToString()
            + "_r" + get_render_res().ToString() + ".png";
        byte[] im_bytes_v = System.IO.File.ReadAllBytes(file_path_v);

        Texture2D tex_albedo_v = new Texture2D(res_x, res_y);
        texs_albedo_v.Add(tex_albedo_v);
        tex_albedo_v.LoadImage(im_bytes_v);
        List<List<float>> mat_v = tex2mat(tex_albedo_v);

        // info (paul): find min and max val
        string min_max_file = path_dic +
            remove_dots(get_experiment()) + "/min_max_v_" + key_v.ToString() + ".txt";//t_idx
        string min_max_str = load_txt_line(min_max_file);
        string[] strs = min_max_str.Split(" ");
        float min_val = float.Parse(strs[0]);
        float max_val = float.Parse(strs[1]);

        mat_v = unnorm_mat(mat_v, min_val, max_val);

        return mat_v;
    }

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
                    floats_ij = floats[i][j];
                }

                col_mat[i, j].r = floats_ij;
                col_mat[i, j].g = floats_ij;
                col_mat[i, j].b = floats_ij;
                col_mat[i, j].a = 1f;
            }
        }

        return col_mat;
    }

    public List<List<float>> cols_mat2floats_mat_for_im(UnityEngine.Color[,] cols_mat)
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

    public float[] vec2floats(Vector3 vec)
    {
        return new float[3] { vec.x, vec.y, vec.z };
    }
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

    public void main_render()
    {
        // info (paul): Kind of the main function for the rendering of the nakajima samples
        //      for validation purposes:

        ; ;
    }
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
        this.cam_angle_0 = 1f;//10f
        this.cam_angle_1 = cam_angle_0;
        if (category == "muc" && category_muc == "simple")
        {
            this.cam_angle_0 = 0f;
            this.cam_angle_1 = 2f;
        }
        this.cam_0 = set_up_cam("cam_0", "cam_prefab_0_" + get_render_res().ToString(), pos, angle: -cam_angle_0);//-10f
        this.cam_1 = set_up_cam("cam_1", "cam_prefab_1_" + get_render_res().ToString(), pos, angle: cam_angle_1);//10f
    }

    public void load_cam_light()
    {
        Vector3 pos = new Vector3(blades_pos.x, blades_pos.y + default_dist, blades_pos.z + 0f);

        // info (paul): set up two cameras
        cam_angle = 1f;//10f
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
    public void set_up_lighting(float y_coord = 30f, float intensity = 1f)
    {
        // info (paul): remove lights
        GameObject lightings_parent = GameObject.Find("lightings");
        remove_children(lightings_parent);

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
    public Camera set_up_cam(string cam_name, string prefab_name, Vector3 pos, float angle = 0f, ExpConfig config=null)
    {
        GameObject cam_prefab = (GameObject)Resources.Load("Targets/fbx_files/" + prefab_name);
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
        cam_obj.transform.RotateAround(blades_pos, new Vector3(0f, 0f, 1f), angle);//A new Vector3(1f, 0f, 0f)

        // info (paul): little final position adjustments
        if (category == "muc")
        {
            Vector3 pos_l = cam_obj.transform.position;
            //01032025 cam_obj.transform.position = new Vector3(pos_l.x + 20f, pos_l.y, pos_l.z - 20f);
            cam_obj.transform.position = new Vector3(pos_l.x - 0f, pos_l.y, pos_l.z + 0f);
        }

        GameObject cam_parent = GameObject.Find("cams_0_1_parent");
        cam_obj.transform.SetParent(cam_parent.transform);

        Camera cam = cam_obj.GetComponent<Camera>();

        if (config == null && category != "muc")
        {
            cam.fieldOfView = this.field_of_view; // standard: 60;
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
            cam.fieldOfView = config.get_fov();
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

    public void activate_blade(int blade_idx)
    {
        List<GameObject> blades = collect_blades();

        //11062024 for (int i = 0; i < blades.Count; i++)//blades.Count; i++)
        //11062024 {
        activate_blade_from(blades, blade_idx: blade_idx);
        //11062024 }
    }

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

    public void take_pic_manual(string save_path, int cam_idx = 0)
    {
        // info (paul): this is for taking pictures, when the user enters cam pos etc. over the design panels
        take_pic(blade_idx: -1, cam_idx: cam_idx, save_path: save_path, pars: params_now);
    }
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

    void take_pic(int blade_idx, int cam_idx, Params pars, string save_path = null)
    {
        Texture2D tex = cam2tex(cam_idx);
        Texture2D tex_post = post_proc(tex, pars: pars);
        // info (paul): save as png image
        save_png(tex, cam_idx, save_path: save_path, blade_idx: blade_idx, pars: pars);
    }

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

    public List<List<float>> lins_verzerr(List<List<float>> unverzerrt)
    {
        List<List<float>> verzerr = unverzerrt;// TODO: add the actual calculation
        return verzerr;
    }

    Stopwatch watch = null;

    public void tik()
    {
        // info (paul): Start stopwatch
        this.watch = new Stopwatch();
        this.watch.Start();
    }

    public long tok()
    {
        // info (paul): read out stop watch and restart it
        this.watch.Stop();
        long ticks = this.watch.ElapsedTicks;
        return ticks;
    }

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
    public void save_float_for_blade(float value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "float", with_uv_mode: with_uv_mode);
        save_float(value, full_path: path);
    }
    public float[] load_floats_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats", with_uv_mode: with_uv_mode);
        //17012025path = "C:/Users/go73jem/Desktop/DIC_package/exp_normal/cam_0/floats/0__d_xsfloats_r128";
        float[] value = load_floats(full_path: path);
        return value;
    }
    public int[] load_ints_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "ints", with_uv_mode: with_uv_mode);
        int[] value = load_ints(full_path: path);
        return value;
    }
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

    public float[][] lists_to_floats2(List<List<float>> lists)
    {
        float[][] floats = new float[lists.Count][];
        for (int i = 0; i < lists.Count; i++)
        {
            floats[i] = lists[i].ToArray();
        }

        return floats;
    }
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


    public Transform display_from_path(Transform im_1_panel, string save_path, Texture2D input)
    {
        // info (paul): assign to images to panels
        byte[] bytes = File.ReadAllBytes(save_path);

        (int width_pre, int height_pre) = bytes2res(bytes);
        Texture2D texture = new Texture2D(width_pre, height_pre, TextureFormat.ARGB32, false);
        texture.LoadImage(bytes);

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

    public void save_floats_for_blade(float[] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats", with_uv_mode: with_uv_mode);
        save_floats(value, full_path: path);
    }
    public void save_floats_list_2_for_blade(List<List<float>> lists, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        float[][] value = lists_to_floats2(lists);
        save_floats2_for_blade(value, cam_idx, blade_idx, label, with_uv_mode);
    }
    public void save_floats2_for_blade(float[][] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats2", with_uv_mode: with_uv_mode);
        save_floats2(value, full_path: path);
    }
    public void save_ints_for_blade(int[] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "ints", with_uv_mode: with_uv_mode);
        save_ints(value, full_path: path);
    }
    public void save_ints2_for_blade(int[,] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "ints2", with_uv_mode: with_uv_mode);
        save_ints2(value, full_path: path);
    }
    public void save_floats3_for_blade(float[][][] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats3", with_uv_mode: with_uv_mode);
        save_floats3(value, full_path: path);
    }

    public float load_float_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "float", with_uv_mode: with_uv_mode);
        float value = load_float(full_path: path);
        return value;
    }
    public List<List<float>> load_floats_list_2_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
    {
        //float[][] value = lists2floats2(lists);
        string exp_l = remove_dots(get_experiment());
        string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats2", experiment: exp_l, with_uv_mode: with_uv_mode);
        float[][] floats = load_floats2(full_path: path);
        List<List<float>> lists = floats2_to_lists(floats);
        return lists;
    }


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
        if (true)//(File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Create);

            formatter.Serialize(stream, value);
            stream.Close();
        }
    }
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
        if (true)//(File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Create);

            formatter.Serialize(stream, value);
            stream.Close();
        }
    }

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
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            data = (int[,])formatter.Deserialize(stream);
            stream.Close();
        }
        else
        {
            data = null; // or whatever a good default value is
        }

        return data;
    }
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
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            data = (float[][][])formatter.Deserialize(stream);
            stream.Close();
        }
        else
        {
            data = null; // or whatever a good default value is
        }

        return data;
    }
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

    public string get_experiment()
    {
        return this.experiment;
    }
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
            string file_name = "params_" + exp + "_" + paint_mode + "_" + strain_mode + "_" +
                plot_or_heights_mode + ".txt";
            string params_path = path_dic + "exp_normal/time_flow_v/nice_pics/" +
                file_name;

            string info_str = exp + "\t" + paint_mode + "\t" + strain_mode + "\t" +
                plot_or_heights_mode + "\t" + v_mean + "\t" + v_std + "\t" + v_min + "\t" + v_max;
            write_to_txt(params_path, info_str, mode: "replace");

            // info (paul): save im
            file_name = "im_" + exp + "_" + paint_mode + "_" + strain_mode + "_" + plot_or_heights_mode + ".png";
            string im_path = path_dic + "exp_normal/time_flow_v/nice_pics/" +
                file_name;
            path = im_path;
        }
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
    }
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


        System.IO.File.WriteAllBytes(path_l.Replace("uv/", "uv/" + category), tex.EncodeToPNG());
        save_params_file(path_l, pars);
        // e.g. "saved blade under: muc/Users/paulrichter/Desktop/DIC_2025_for_travel/DIC_package/exp_normal/cam_0/uv/2__uv_value.png_r512"
        string blade_info = "saved blade under: " + category + path_l;
        if (with_print_paths)
        {
            Debug.Log(blade_info);
        }
        set_ready_for_next_blade(true);
    }

    public void save_params_file(string blade_path, Params pars)
    {
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
    public (string, Params) find_dir_for_params(Params pars_ref)
    {
        Params pars_found = null;
        string proj_path_found = null;

        for (int i = 0; i < render_acts.Count; i++)
        {
            string exp_name = render_acts[i].get_label();
            string proj_path = path_dic + exp_name;
            string dir_path = proj_path + "/cam_0/uv/";
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

    public void convert_to_tif()
    {
        Process proc = new Process();
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = "png2tiff.py";//"script_paul.py";

        Process.Start("python", "Assets/png2tiff.py").WaitForExit();
    }

    public void hausdorff()
    {
        // info (paul): find hausdorff distance

        for (int i = 0; i < -1; i++)
        {
            // TODO: ...
        }

    }

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
        if (category == "muc")
        {
            surface_obj.transform.localScale = new Vector3(3f, 3f, 3f);
        }
        surface_obj.transform.RotateAround(new Vector3(),
             new Vector3(1f, 0f, 0f), angle: -90f);

        if (blade_name.StartsWith("muc"))
        {
            surface_obj.transform.RotateAround(new Vector3(),
                 new Vector3(0f, 1f, 0f), angle: 90f);
        }
        
        surface_obj.transform.position = blades_pos;

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
                    if (grad_i < GRAD_IS_ZERO && warpings < 1)
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
            bool verbose  // enable/disable the verbose mode
        )
    {
        if (break_now)
        {
            return (null, null);
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
            }

            if (verbose)
            {
                //27072024 string log_str = stderr + "Warping: " + warpings.ToString() + "Iterations: %d, " + n.ToString() + "Error: %f\n" + error;
                string log_str = "stderr";
            }
        }

        return (ux, uy);
    }

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
                if (grad_i < GRAD_IS_ZERO && warpings < 1)
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
    public void set_config_now(ExpConfig config_now)
    {
        this.config_now = config_now;
    }
    public ExpConfig get_config_now()
    {
        return config_now;
    }
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
            for (int i = 0; i < size; i++)
            {
                I0n[i] = I0[i];
                I1n[i] = I1[i];
            }
        }
        return (I0n, I1n);
    }
    public List<float> scale_by(List<float> mat, float factor)
    {
        for (int i = 0; i < mat.Count; i++)
        {
            mat[i] *= factor;
        }

        return mat;
    }
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

    List<List<float>> norm_floatss(List<List<float>> u, int n_x, int n_y)
    {
        // info (paul): norm list of lists, each separately
        for (int i = 0; i < u.Count; i++)
        {
            u[i] = norm_floats(u[i], n_x, n_y);
        }
        return u;
    }

    public Texture2D crop_tex(Texture2D source, int target_w, int target_h)
    {
        Texture2D cropped = new Texture2D(target_w, target_h, TextureFormat.ARGB32, false);

        int start_x = ((int)(source.width * 0.5f)) - target_w/2;
        int start_y = ((int)(source.height * 0.5f)) - target_h/2;

        cropped.SetPixels(source.GetPixels(start_x, start_y, target_w, target_h));

        return cropped;
    }
    im_dressed manage_read_im(string im_path)
    {
        byte[] bytes = File.ReadAllBytes(im_path);

        (int width_pre, int height_pre) = bytes2res(bytes);
        Texture2D texture = new Texture2D(width_pre, height_pre, TextureFormat.ARGB32, false);
        texture.LoadImage(bytes);

        (int width, int height) = (get_render_res(), get_render_res());//25122024 (512, 512);//17122024 (1024, 1024);//12122024 (512, 512);
        Texture2D cropped = crop_tex(texture, width, height);

        //12022025 System.IO.File.WriteAllBytes("C:/Users/go73jem/Pictures/test111.png", cropped.EncodeToPNG());

        List<float> mat = tex2floats(cropped, with_switch_dims: false, color_channel: 1);
        List<float> mat_flipped = flip_floats(mat, width, height);
        List<float> mat_scaled = scale_by(mat_flipped, factor: 0.33f * 256f);

        (List<float> mat_lower_res, int width_new, int height_new) = sample_res_for_floats(
            mat_scaled, width, height, fac_x: 1f, fac_y: 1f);//17122024 , fac_x: 0.25f, fac_y: 0.25f);//12122024 fac_x: 0.5f, fac_y: 0.5f)

        im_dressed im_dressed_1 = new im_dressed(mat_lower_res, width_new, height_new); //(mat_scaled, width, height)
        return im_dressed_1;
    }

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

    public (List<float>, int, int) sample_res_for_floats(List<float> floats, int width, int height, float fac_x, float fac_y)
    {
        float[][] mat_ar = floats2matrix(floats.ToArray(), width, height);
        List<List<float>> mat = floats2_to_lists(mat_ar);

        List<List<float>> smaller = sample_res(mat, fac_x: fac_x, fac_y: fac_y);

        List<float> list = matrix2list(smaller);

        return (list, smaller.Count, smaller[0].Count);
    }

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

    public (double, double, int, int) set_up_pars(bool with_dt, string exp_label = null)
    {
        int dt_compare_l = 0;

        if (with_dt)
        {
            dt_compare_l = get_dt_compare();
            PAR_DEFAULT_LAMBDA = 0.05d;//26102024 0.0001f;
            PAR_DEFAULT_THETA = 0.55d;//26102024 20d;
            //26022025 PAR_DEFAULT_LAMBDA = 0.3d;
            //26022025 PAR_DEFAULT_THETA = 0.3d;
            PAR_DEFAULT_NSCALES = 8;//02122024 8;//8;//8; //22102024 4;
            PAR_DEFAULT_NWARPS = 3;//02122024 6;// 6;//11112024 6;// 6; //20;//24102024 6; //22102024 6;
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
        return (PAR_DEFAULT_LAMBDA, PAR_DEFAULT_THETA, PAR_DEFAULT_NSCALES, PAR_DEFAULT_NWARPS);
    }
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

    public (int, bool) cv_series(List<string> paths, string in_dir = "", string out_dir = "",
        Params pars = null, int series_idx = 0, bool is_started = false)
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
            // TODO: load from paths
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

        int start_idx = 0;//12;
        int upper_lim = 9999;//is_started
        //for (int idx = 0; idx < paths.Count - 1; idx++)
        if (true)
        {
            if (start_idx <= series_idx && series_idx < upper_lim && series_idx < out_paths.Count - 1)
            {
                cv_for_paths(paths[series_idx], paths[series_idx + 1], render_act_idx: 0, d_cam: 0,
                    with_dt: true, exp_label: null, save_path: out_paths[series_idx],
                    im0_path: im0_paths[series_idx], im1_paths[series_idx], pars: pars);
            }
            if (series_idx >= upper_lim || series_idx >= out_paths.Count)
            {
                is_started = false;
            }
            series_idx += 1;
        }

        return (series_idx, is_started);
    }
    public void cv_for_paths(string path0, string path1, int render_act_idx, int d_cam = 0, 
        bool with_dt = false, string exp_label = null, string save_path = null,
        string im0_path = null, string im1_path = null, Params pars = null)
    {
        // info (paul): path0, path1: Path to the two .png images
        //          render_act_idx: 
        //          
        //          

        (PAR_DEFAULT_LAMBDA, PAR_DEFAULT_THETA, PAR_DEFAULT_NSCALES, PAR_DEFAULT_NWARPS) = set_up_pars(
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
        (string proj_dir, _) = find_dir_for_params(pars);

        for (int t_idx_i = 0; t_idx_i < max_idx; t_idx_i++)//21112024 blade_idxs.Count - 1
        {
            (int t_idx_0, int t_idx_1, int t_idx_2) = fetch_t_idxs(with_dt, t_idx_i);

            string info = "render_act: " + render_act_idx.ToString() + " / " + render_acts.Count.ToString() +
                "; t_idx: " + (t_idx_i - t_idx_start).ToString() + " / " + t_steps.ToString();
            Debug.Log(info);

            string im_t_name = "";
            string im_t_next_name = "";
            string im_t_next_next_name = "";
            
            if (path0 != null)
            {
                im_t_name = path0;
                im_t_next_name = path1;
                im_t_next_next_name = path0; // perh. TODO: somehow change that later
            }
            /*
            List<string> im_paths = this.get_im_paths();
            if (im_paths.Count >= max_idx && im_paths.Count > t_idx_i + 2)
            {
                im_t_name = im_paths[t_idx_i];
                im_t_next_name = im_paths[t_idx_i + 1];
                im_t_next_next_name = im_paths[t_idx_i + 2];
            }*/

            // info (paul): output paths
            string time_flow_name_u = proj_dir +
                "/time_flow_u/time_flow_u_" + t_idx_0.ToString() + "_r" +
                 get_render_res().ToString() + ".png";
            string time_flow_name_v = proj_dir +
                "/time_flow_v/time_flow_v_" + t_idx_0.ToString() + "_r" +
                 get_render_res().ToString() + ".png";
            string heights_name = proj_dir +
                "/heights/heights_" + t_idx_0.ToString() + "_r" +
                 get_render_res().ToString() + ".png";

            string min_max_u_file = proj_dir +
                "/min_max_u_" + t_idx_0.ToString() + ".txt";
            string min_max_v_file = proj_dir +
                "/min_max_v_" + t_idx_0.ToString() + ".txt";
            string min_max_heights_file = proj_dir +
                "/min_max_heights_" + t_idx_0.ToString() + ".txt";
            
            bool next_exists = File.Exists(im_t_next_name);
            bool next_next_exists = File.Exists(im_t_next_next_name);

            im_dressed imd_0 = manage_read_im(im_t_name);
            im_dressed imd_1 = manage_read_im(im_t_next_name);
            im_dressed imd_2 = manage_read_im(im_t_next_next_name);

            List<List<float>> I0 = new List<List<float>> { imd_0.im_vec, imd_0.im_vec };//03042025 imd_1.im_vec};
            List<List<float>> I1 = new List<List<float>> { imd_1.im_vec, imd_1.im_vec };//03042025 imd_2.im_vec};

            (List<List<List<float>>> u_mat, List<List<List<float>>> v_mat) = (null, null);
            List<float> min_val_u = new List<float>() { };
            List<float> min_val_v = new List<float>() { };
            List<float> max_val_u = new List<float>() { };
            List<float> max_val_v = new List<float>() { };

            int n_x = imd_0.width;
            int n_y = imd_1.height;

            if (next_exists && !break_now)
            {
                bool with_plot = true; // what was this needed for?
                (u_mat, v_mat, min_val_u, min_val_v, max_val_u, max_val_v) = compute_ims(
                    I0, I1, n_x, n_y, scale_fac: 1, with_dt: with_dt);
            }

            save_flow_u(u_mat[0], time_flow_name_u);
            save_flow_v(v_mat[0], time_flow_name_v);

            if (save_path != null)
            {
                save_flow_u(u_mat[0], save_path);
            }

            // info (paul): display flow
            Transform display = canvas.transform.Find("Choose_panel").Find("maps").Find("u_panel");
            Transform display_im0 = canvas.transform.Find("Choose_panel").Find("maps").Find("im0_panel");
            Transform display_im1 = canvas.transform.Find("Choose_panel").Find("maps").Find("im1_panel");

            display_from_path(display, time_flow_name_u, input: mat2tex(u_mat[0]));

            List<float> I0_listsA = scale_by(I0[0], factor: 1f / (0.33f * 256f));
            List<float> I1_listsA = scale_by(I1[0], factor: 1f / (0.33f * 256f));
            int res_l = (int)(Mathf.Sqrt(I0_listsA.Count));
            List<List<float>> I0_lists = floats2_to_lists(floats2matrix(I0_listsA.ToArray(), res_x: res_l, res_y: res_l));
            List<List<float>> I1_lists = floats2_to_lists(floats2matrix(I0_listsA.ToArray(), res_x: res_l, res_y: res_l));
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

    public void cv_main(int render_act_idx, int d_cam = 0, bool with_dt = false, string exp_label = null,
        Params pars = null)
    {
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
        (string proj_dir, _) = find_dir_for_params(pars);

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
            
            if (im_path_0 != null)
            {
                im_t_name = im_path_0;
                im_t_next_name = im_path_1;
                im_t_next_next_name = im_path_0; // perh. TODO: somehow change that later
            }

            List<string> im_paths = this.get_im_paths();
            if (im_paths.Count >= max_idx && im_paths.Count > t_idx_i+2)
            {
                im_t_name = im_paths[t_idx_i];
                im_t_next_name = im_paths[t_idx_i+1];
                im_t_next_next_name = im_paths[t_idx_i+2];
            }

            // info (paul): output paths
            string time_flow_name_u = proj_dir +
                "/time_flow_u/time_flow_u_" + t_idx_0.ToString() + "_r" +
                 get_render_res().ToString() + ".png";
            string time_flow_name_v = proj_dir +
                "/time_flow_v/time_flow_v_" + t_idx_0.ToString() + "_r" +
                 get_render_res().ToString() + ".png";
            string heights_name = proj_dir +
                "/heights/heights_" + t_idx_0.ToString() + "_r" +
                 get_render_res().ToString() + ".png";
            
            string min_max_u_file = proj_dir +
                "/min_max_u_" + t_idx_0.ToString() + ".txt";
            string min_max_v_file = proj_dir +
                "/min_max_v_" + t_idx_0.ToString() + ".txt";
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
                (u_mat, v_mat, min_val_u, min_val_v, max_val_u, max_val_v) = compute_ims(
                    I0, I1, n_x, n_y, scale_fac: 1, with_dt: with_dt);
            }

            save_flow_u(u_mat[0], time_flow_name_u);
            save_flow_v(v_mat[0], time_flow_name_v);

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

    public void save_flow_u(List<List<float>> u_mat, string out_file_name_u)
    {
        List<List<float>> u_lists = u_mat;//floats2_to_lists(u_mat);
        Texture2D tex_u = mat2tex(u_lists, with_switch_dims: false);
        arrange_dir(out_file_name_u);
        System.IO.File.WriteAllBytes(out_file_name_u, tex_u.EncodeToPNG());
    }
    public void save_flow_v(List<List<float>> v_mat, string out_file_name_v)
    {
        List<List<float>> v_lists = v_mat;//floats2_to_lists(v_mat);
        Texture2D tex_v = mat2tex(v_lists, with_switch_dims: false);
        arrange_dir(out_file_name_v);
        System.IO.File.WriteAllBytes(out_file_name_v, tex_v.EncodeToPNG());
    }

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
    public List<float> copy_floats(List<float> u_input)
    {
        List<float> floats = new List<float>();

        for (int i = 0; i < u_input.Count; i++)
        {
            floats.Add(u_input[i]);
        }
        return floats;

    }
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

    public int get_dt_compare()
    {
        return dt_compare;
    }
    public void set_dt_compare(int input)
    {
        dt_compare = input;
    }
    public List<float> first_half_of(List<float> u_input)
    {
        List<float> u_half = new List<float>();

        for (int i = 0; i < u_input.Count / 2; i++)
        {
            u_half.Add(u_input[i]);
        }

        return u_half;
    }
    public void write_for_debug(float[] u_input, float scale)
    {
        write_for_debug(u_input.ToList(), with_norm: false, scale: scale, offset: 0f);
    }
    public void write_for_debug(List<float> u_input, float scale)
    {
        write_for_debug(u_input, with_norm: false, scale: scale, offset: 0f);
    }
    public void write_for_debug(List<float> u_input)
    {
        write_for_debug(u_input, with_norm: false, scale: 1f, offset: 0f);
    }
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
                    tau, lambda, theta, warps, epsilon, verbose
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

    public Actioner(Action act, string label = null, int cv_render_idx = -1,
        Params pars = null)
    {
        this.act = act;
        this.label = label;
        this.cv_render_idx = cv_render_idx;
        this.pars = pars;
    }
    public string get_label()
    {
        return this.label;
    }
    public void set_label(string input)
    {
        this.label = input;
    }

    public void set_cv_render_idx(int input)
    {
        this.cv_render_idx = input;
    }
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

    public float get_speckle_size()
    {
        return this.speckle_size;
    }
    public void set_speckle_size(float value)
    {
        this.speckle_size = value;
    }
    public float get_lighting_intensity()
    {
        return this.lighting_intensity;
    }
    public void set_lighting_intensity(float value)
    {
        this.lighting_intensity = value;
    }   
    public float get_lighting_pos_x()
    {
        return this.lighting_pos_x;
    }
    public void set_lighting_pos_x(float value)
    {
        this.lighting_pos_x = value;
    }
    public float get_lighting_pos_y()
    {
        return this.lighting_pos_y;
    }
    public void set_lighting_pos_y(float value)
    {
        lighting_pos_y = value;
    }
    public float get_lighting_pos_z()
    {
        return this.lighting_pos_z;
    }
    public void set_lighting_pos_z(float value)
    {
        this.lighting_pos_z = value;
    }
    public float get_gaussian_error()
    {
        return this.gaussian_error;
    }
    public void set_gaussian_error(float value)
    {
        this.gaussian_error = value;
    }
    public float get_poisson_error()
    {
        return this.poisson_error;
    }
    public void set_poisson_error(float value)
    {
        this.poisson_error = value;
    }
    public float get_lens_distortion()
    {
        return this.lens_distortion;
    }
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

    public ExpConfig(float fov = float.NaN, string label = "NO_LABEL", float diameter = float.NaN)
    {
        this.fov = fov;
        this.label = label;
        this.diameter = diameter;
        this.ambient_intensity = float.NaN;

        float[] cam_poss_0 = new float[3] { 0f, 0f, 0f };
        float[] cam_poss_1 = new float[3] { 0f, 0f, 0f };
        cam_poss = new List<float[]>() { cam_poss_0, cam_poss_1 };
    }

    public void set_light_pos(float[] val)
    {
        this.light_pos = val;
    }
    public float[] get_light_pos()
    {
        return this.light_pos;
    }
    public void set_light_quat(float[] val)
    {
        this.light_quat = val;
    }
    public float[] get_light_quat()
    {
        return this.light_quat;
    }

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
    public void set_cam_pos(int cam_idx, string coord, float val)
    {
        // info (paul): e.g. cam_idx = 0, coord="z", val="2.15"

        this.cam_poss[cam_idx][coord2int(coord)] = val;
    }
    public List<float[]> get_cam_poss()
    {
        return this.cam_poss;
    }
    public void set_fov(float input)
    {
        this.fov = input;
    }
    public float get_fov()
    {
        return this.fov;
    }
    public void set_label(string label)
    {
        this.label = label;
    }
    public string get_label()
    {
        return label;
    }
    public void set_diameter(float diameter)
    {
        this.diameter = diameter;
    }
    public float get_diameter()
    {
        return this.diameter;
    }

    public void set_cam_angle(float cam_angle)
    {
        this.cam_angle = cam_angle;
    }
    public float get_cam_angle()
    {
        return cam_angle;
    }

    public void set_save_path(string val)
    {
        this.save_path = val;
    }
    public string get_save_path()
    {
        return this.save_path;
    }

    public void set_blade_path(string input)
    {
        this.blade_path = input;
    }

    public string get_blade_path()
    {
        return this.blade_path;
    }

    public void set_ambient_intensity(float input)
    {
        this.ambient_intensity = input;
    }

    public float get_ambient_intensity()
    {
        return this.ambient_intensity;
    }
}