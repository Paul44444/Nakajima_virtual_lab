// A using System.Collections;
// A using System.Collections.Generic;
// A using UnityEngine;
// A using System;
// A using System.Linq;
// A using System.Text;
// A using System.Threading.Tasks;
// A using System.IO;
// A using System.Globalization;
// A using TMPro;
// A using Unity.VisualScripting;
// A using System.Collections.Specialized;
// A using System.Text.RegularExpressions;
// A using static UnityEngine.UIElements.UxmlAttributeDescription;
// A using JetBrains.Annotations;
// A using System.Numerics;
// A using Vector3 = UnityEngine.Vector3;
// A using Vector2 = UnityEngine.Vector2;
// A using UnityEngine.UIElements;
// A using UnityEngine.Windows;
// A using File = System.IO.File;
// A using UnityEditor.ShaderKeywordFilter;
// A using static UnityEditor.PlayerSettings;
// A 
// A //using System.Windows.Controls.Image;
// A //using System.Windows.Media.Imaging;
// A //using System.Drawing.Image;
// A using System.Drawing;
// A using UnityEngine.XR;
// A using Color = UnityEngine.Color;
// A using System.Reflection;
// A using System.Diagnostics;
// A using Debug = UnityEngine.Debug;
// A using Stopwatch = System.Diagnostics.Stopwatch;
// A using System.Runtime.Serialization.Formatters.Binary;
// A using System.Reflection.Emit;
// A using System.Runtime.InteropServices.WindowsRuntime;
// A using Unity.IO.LowLevel.Unsafe;
// A using Directory = System.IO.Directory;
// A using static UnityEditor.Progress;
// A using System.Security.Cryptography;
// A using UnityEditor.Build.Player;
// A using System.Linq.Expressions;
// A using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
// A using UnityEditor.Experimental.GraphView;
// A using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
// A using System.Xml;
// A using static UnityEngine.Random;
// A using UnityEngine.TestTools;
// A 
// A //05062024 using System.Runtime.InteropServices.WindowsRuntime;
// A //05062024 using Palmmedia.ReportGenerator.Core.Reporting.Builders.Rendering;
// A //05062024 using JetBrains.Annotations;
// A //05062024 using UnityEngine.UIElements;
// A //05062024 using UnityEngine.Timeline;
// A //05062024 using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
// A //05062024 using System.Diagnostics.Contracts;
// A //05062024 using System.Drawing;
// A //05062024 using System.Reflection;
// A 
// A public class vis_3D : MonoBehaviour
// A {
// A     Vector2[] uv_start;
// A 
// A     GameObject canvas;
// A     Transform log_field;
// A     Cam_manager cam_script;
// A     Camera cam_for_uv_0;
// A     Camera cam_for_uv_1;
// A     Match_steps match_control;
// A     T_control_script t_control;
// A     Strain strain_control;
// A     U_V u_v_control;
// A     Experiment_Control experiment_control;
// A     Strain_D strain_d;
// A 
// A     bool force_flat = true;
// A 
// A     string path_stereo = null;//08062024 "C:/Users/go73jem/Pictures/displacements_u.csv";// info (paul): this will be overwritten anyway, see init_params()
// A     //string path_stereo = "C:/Users/go73jem/Pictures/displacements_u.csv";
// A     //10052024 string path_time_flow = "C:/Users/go73jem/unter2_Windows_native/Assets/Resources/Targets/flow_159_160.png";
// A     //16052024 string path_time_flow = "C:/Users/go73jem/Desktop/DIC_package/time_flow.png";//12052024 
// A     //05062024 string path_time_flow = "C:/Users/go73jem/Desktop/DIC_package/time_flow/";
// A     string path_time_flow_u = null;//08062024 "C:/Users/go73jem/Desktop/DIC_package_pre_05062024/time_flow/";// info (paul): this will be overwritten anyway, see init_params()
// A     string path_time_flow_v = null;
// A 
// A     // info (paul): This will be overwritten in the init_params() function; for more params also look at init_params()
// A     int res_x = 1024;//120;//200;//600;
// A     int res_y = 1024;//192;//576;// 1728;
// A     int im_cnt = 20;
// A     int t_idx = -1;
// A 
// A     int blade_idx_min = 92;
// A     int blade_idx_max = 100;//96//100;
// A 
// A     // info (paul): "normal": no derivative
// A     //      "derivative_1": first derivative
// A     string strain_mode = "normal";
// A 
// A     // info (paul): Whether to plot u or t
// A     string u_v_mode = "v";//"u";
// A 
// A     // info (paul): whether to take the derivative in x or y direction
// A     string strain_d_mode = "x";
// A 
// A     bool is_visible = false;
// A     bool ready_for_next_blade = true;
// A     private float pic_timer = 0f; // info (paul): value will be changed over time
// A     float pic_time = 0.5f;
// A 
// A     // info (paul): the index for the "current" blade
// A     int blade_idx_secret;
// A     private bool blades_created = false; // info (paul): whether all blades were created successfully
// A     public bool with_match_tex;
// A 
// A     // info (paul): blades_pos is the central position for the blades rendering
// A     Vector3 blades_pos;
// A     int match_steps = -1;
// A 
// A     bool with_main = true;
// A 
// A     // info (paul): whether to include experiments, rendering etc.
// A     bool with_exp = false;//true;
// A 
// A     private int dt_compare = 4;//4;//27092024 1;//6;//03092024 1;
// A 
// A     bool done_normal_exps = false;
// A     bool done_normal_ims = false;//05072024 false;
// A     bool done_render_acts = false;//done_lighting_ims = false;//05072024 false;
// A     bool done_speckle_ims = false;//05072024 false;
// A     bool done_ground_truth = false;//05072024 false;
// A 
// A     List<GameObject> blades;
// A     List<int[]> blade_tris;
// A 
// A     // info (paul): camera parameters for the validation
// A     float field_of_view = 40f;//03072024 10f;//21062024 5f;//30f;//default: 60f
// A 
// A     // info (paul): If you change that, you should also manually change the size []x[] of
// A     //      "cam_tex_0" and "cam_tex_1" under Assets/Resources/Targets/fbx_files/cam_tex_0
// A     int render_res = 512;//04072024 256;// default: 1024;
// A 
// A     // info (paul): scale factor for the flow
// A     float flow_scale_factor = 1.0f;//0.6f
// A 
// A     // info (paul): plot_mode: possible values: 
// A     string plot_mode = "loss_rel";//"value_ref";////"loss_rel""loss_abs""value""value_ref"""
// A     string heights_mode = "value";//"value""value_ref""loss_abs""loss_rel"""
// A     string paint_with = "uv";//"uv";"heights"
// A 
// A     float cam_angle = 10f;
// A     string experiment = "exp_normal";
// A 
// A     // info (paul): the actions of what is rendered
// A     public List<Actioner> render_acts;
// A     bool ready_for_next_act = true;
// A     int render_idx = 0;
// A 
// A     // info (paul): for making the speckle patterns; (action; speckle_size; speckle_dist)
// A     public List<(Action, float, float)> speckle_acts;
// A 
// A     bool with_print_paths = false;
// A 
// A     float speckle_size = -1f;
// A     float speckle_dist = -1f;
// A 
// A     GameObject speckle_parent;
// A 
// A     Material speckle_mat;
// A     List<Actioner> exp_cv_acts;
// A     int cam_idx_for_pic = 0;
// A 
// A     // Start is called before the first frame update
// A     void Start()
// A     {
// A         speckle_mat = (Material)Resources.Load("Targets/speckle_mat");
// A 
// A         //cv_main();
// A         speckle_acts = new List<(Action, float, float)>();
// A         //speckle_acts.Add((make_speckle_tex, 0.7f * 1f, 1f * 1f));
// A         //speckle_acts.Add((render_speckles, 0.7f * 1f, 1f * 1f));
// A         //speckle_acts.Add((make_speckle_tex, 0.7f * 1f, 1f * 1f));
// A         //speckle_acts.Add((render_speckles, 0.7f * 1f, 1f * 1f));0.7f
// A 
// A         //speckle_acts.Add((make_speckle_tex, 0.7f * 6f, 1f * 6f));
// A         //speckle_acts.Add((render_speckles, 0.7f *  6f, 1f * 6f));
// A         //speckle_acts.Add((make_speckle_tex, 0.7f * 5f, 1f * 5f));
// A         //speckle_acts.Add((render_speckles, 0.7f * 5f, 1f * 5f));
// A         //Bspeckle_acts.Add((make_speckle_tex, 0.7f * 4f, 1f * 4f));
// A         //Bspeckle_acts.Add((render_speckles, 0.7f * 4f, 1f * 4f));
// A         //Aspeckle_acts.Add((make_speckle_tex, 0.7f * 3f, 1f * 3f));
// A         //Aspeckle_acts.Add((render_speckles, 0.7f * 3f, 1f * 3f));
// A         //Aspeckle_acts.Add((make_speckle_tex, 0.7f * 2f, 1f * 2f));
// A         //Aspeckle_acts.Add((render_speckles, 0.7f * 2f, 1f * 2f));
// A         //Aspeckle_acts.Add((make_speckle_tex, 0.7f * 1f, 1f * 1f));
// A         //Aspeckle_acts.Add((render_speckles, 0.7f * 1f, 1f * 1f));
// A         //Aspeckle_acts.Add((make_speckle_tex, 0.7f * 0.5f, 1f * 0.5f));
// A         //Aspeckle_acts.Add((render_speckles, 0.7f * 0.5f, 1f * 0.5f));
// A         //Aspeckle_acts.Add((make_speckle_tex, 0.7f * 0.25f, 1f * 0.25f));
// A         //Aspeckle_acts.Add((render_speckles, 0.7f * 0.25f, 1f * 0.25f));
// A         //Aspeckle_acts.Add((make_speckle_tex, 0.7f * 0.1f, 1f * 0.1f));
// A         //Aspeckle_acts.Add((render_speckles, 0.7f * 0.1f, 1f * 0.1f));
// A         //Aspeckle_acts.Add((make_speckle_tex, 0.7f * 0.05f, 1f * 0.05f));
// A         //Aspeckle_acts.Add((render_speckles, 0.7f * 0.05f, 1f * 0.05f));
// A         //Bspeckle_acts.Add((make_speckle_tex, 0.7f * 0.02f, 1f * 0.02f));
// A         //Bspeckle_acts.Add((render_speckles, 0.7f * 0.02f, 1f * 0.02f));
// A         //Bspeckle_acts.Add((make_speckle_tex, 0.7f * 0.01f, 1f * 0.01f));
// A         //Bspeckle_acts.Add((render_speckles, 0.7f * 0.01f, 1f * 0.01f));
// A 
// A         speckle_parent = GameObject.Find("speckle_parent");
// A 
// A         if (true)//20092024
// A         {
// A             manage_lighting_settings();
// A 
// A             with_match_tex = true;
// A 
// A             blades_pos = new Vector3(0f, 0f, -200f);
// A             init_params();
// A 
// A             // info (paul): set up params
// A             canvas = GameObject.Find("Canvas");
// A             log_field = canvas.transform.Find("log_field");
// A             TextMeshProUGUI tmpro = log_field.GetComponent<TextMeshProUGUI>();
// A 
// A             // info (paul): validation
// A             load_obj_file();
// A             string blade_path_first = blade_path_for_idx(blade_idx_min);
// A 
// A             render_acts = init_render_acts(render_acts);
// A             //25042024 GameObject surface_obj = start_renders(blade_path: blade_path_first, blade_idx: blade_idx_min, with_uv_init: true);
// A 
// A             set_ready_for_next_blade(true);
// A             set_blade_idx(-1);
// A             set_pic_timer(0f);
// A             if (!with_exp)
// A             {
// A                 done_normal_exps = true;
// A                 done_normal_ims = true;
// A                 done_render_acts = true;
// A                 done_speckle_ims = true;
// A                 done_ground_truth = true;
// A                 ready_for_next_act = false;
// A                 set_blade_idx(blade_idx_max);
// A             }
// A             exp_cv_acts = set_up_render_list();
// A         }
// A     }
// A 
// A     public void make_speckle_tex()
// A     {
// A         remove_children(speckle_parent);
// A 
// A         float speckle_scale = this.speckle_size;//0.7f;
// A         float speckle_dist = this.speckle_dist;//1f;
// A 
// A         // info (paul): make a plane
// A         float plane_side_len = 10f;
// A         Vector3 plane_pos = new Vector3(0f, 0f, 100f);
// A 
// A         GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
// A         plane.transform.position = plane_pos;
// A         plane.transform.localScale = new Vector3(0.1f * plane_side_len,
// A             0.1f * plane_side_len, 0.1f * plane_side_len);
// A         plane.name = "speckle_plane";
// A         string mat_file = "Targets/fbx_files/Materials/perfect_white";
// A         Material perfect_white = (Material)(Resources.Load(mat_file));
// A         plane.GetComponent<Renderer>().material = perfect_white;
// A         plane.transform.SetParent(speckle_parent.transform);
// A 
// A         // info (paul): make speckle discs as very flat cylinders
// A 
// A         int num_per_side = (int)(plane_side_len / speckle_dist) + 1;//10;
// A         for (int i = 0; i < num_per_side; i++)
// A         {
// A             for (int j = 0; j < num_per_side; j++)
// A             {
// A                 add_single_speckle(i, j, speckle_dist, speckle_scale,
// A                     plane_pos, plane_side_len);
// A             }
// A         }
// A     }
// A     public void render_speckles()
// A     {
// A         Texture2D tex = speckle2tex();
// A 
// A         // info (paul): save tex
// A         string label = "speckle_" + this.speckle_size.ToString() + ".png";
// A         string path_l = "C:/Users/go73jem/Desktop/DIC_package/speckle_patterns/" + label;
// A         System.IO.File.WriteAllBytes(path_l, tex.EncodeToPNG());
// A     }
// A 
// A     public Texture2D speckle2tex()
// A     {
// A         GameObject cam_obj = GameObject.Find("speckle_cam");
// A         Camera cam = cam_obj.GetComponent<Camera>();
// A 
// A         RenderTexture render_tex = cam.targetTexture;
// A         int our_height = render_tex.height;
// A 
// A         Texture2D tex = new Texture2D(our_height, our_height);
// A         RenderTexture.active = render_tex;
// A         tex.ReadPixels(new Rect(0, 0, our_height, our_height), 0, 0);
// A         tex.Apply();
// A         return tex;
// A     }
// A     public void add_single_speckle(int i_idx, int j_idx, float speckle_dist, float speckle_scale,
// A         Vector3 plane_pos, float plane_side_len)
// A     {
// A         // info (paul): make speckle object
// A         GameObject speckle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
// A         speckle.GetComponent<Renderer>().material = speckle_mat;
// A         //speckle.GetComponent<Renderer>().material.color = Color.black;
// A 
// A         // info (paul): position, scale
// A         float pos_x = (float)(j_idx) * speckle_dist;//((float)i) / ((float)num_per_side) * plane_side_len;
// A         float pos_z = (float)(i_idx) * speckle_dist;//((float)j) / ((float)num_per_side) * plane_side_len;
// A         speckle.transform.localScale = new Vector3(speckle_scale, 0.01f, speckle_scale);
// A 
// A         float rand_x = UnityEngine.Random.Range(0f, 1f) * speckle_dist;
// A         float rand_y = UnityEngine.Random.Range(0f, 1f) * speckle_dist;
// A 
// A         float speckle_x = plane_pos.x - 0.5f * plane_side_len + pos_x + rand_x;
// A         float speckle_z = plane_pos.z - 0.5f * plane_side_len + pos_z + rand_y;
// A         speckle.transform.position = new Vector3(speckle_x, plane_pos.y,
// A             speckle_z);
// A 
// A         speckle.transform.SetParent(speckle_parent.transform);
// A 
// A 
// A     }
// A     public List<Actioner> init_render_acts(List<Actioner> acts)
// A     {
// A         acts = new List<Actioner>();
// A         //acts.Add(start_renders);
// A         acts.Add(new Actioner(start_exp_normal, "exp_normal"));
// A 
// A         //acts.Add(new Actioner(start_lighting_05, "lighting_05"));
// A         //acts.Add(new Actioner(start_lighting_0001, "lighting_0001"));
// A 
// A         //B acts.Add(new Actioner(start_lighting_001, "lighting_001"));
// A         //B acts.Add(new Actioner(start_lighting_002, "lighting_002"));
// A         //B acts.Add(new Actioner(start_lighting_003, "lighting_003"));
// A         //B acts.Add(new Actioner(start_lighting_004, "lighting_004"));
// A         //B acts.Add(new Actioner(start_lighting_005, "lighting_005"));
// A         //B acts.Add(new Actioner(start_lighting_01, "lighting_01"));
// A         //B acts.Add(new Actioner(start_lighting_02, "lighting_02"));
// A         //B acts.Add(new Actioner(start_lighting_05, "lighting_05"));
// A         //B acts.Add(new Actioner(start_lighting_07, "lighting_07"));
// A         //B acts.Add(new Actioner(start_lighting_08, "lighting_08"));
// A         //B acts.Add(new Actioner(start_lighting_1, "lighting_1"));
// A         //B acts.Add(new Actioner(start_lighting_2, "lighting_2"));
// A         //B acts.Add(new Actioner(start_lighting_3, "lighting_3"));
// A         //B acts.Add(new Actioner(start_lighting_4, "lighting_4"));
// A 
// A         //acts.Add(new Actioner(start_lighting_5, "lighting_5"));
// A         //acts.Add(new Actioner(start_lighting_6, "lighting_6"));
// A         //acts.Add(new Actioner(start_lighting_10, "lighting_10"));
// A         //acts.Add(new Actioner(start_lighting_100, "lighting_100"));
// A         //acts.Add(new Actioner(start_lighting_10000, "lighting_10000"));
// A         //acts.Add(new Actioner(start_lighting_100000000, "lighting_100000000"));
// A 
// A         //
// A 
// A         //acts.Add(new Actioner(start_speckle_0_035, "speckle_0.035"));
// A         //acts.Add(new Actioner(start_speckle_0_07, "speckle_0.070"));
// A         //acts.Add(new Actioner(start_speckle_0_175, "speckle_0.175"));
// A         //27092024 acts.Add(new Actioner(start_speckle_0_35, "speckle_0.350"));
// A         //27092024 acts.Add(new Actioner(start_speckle_0_7, "speckle_0.700"));
// A         //27092024 acts.Add(new Actioner(start_speckle_1_4, "speckle_1.000"));
// A         //27092024 acts.Add(new Actioner(start_speckle_2_1, "speckle_2.000"));
// A         //27092024 acts.Add(new Actioner(start_speckle_2_8, "speckle_3.000"));
// A 
// A         //Aacts.Add(new Actioner(start_speckle_05, "speckle_05"));
// A         //Aacts.Add(new Actioner(start_speckle_1, "speckle_1"));
// A         //Aacts.Add(new Actioner(start_speckle_2, "speckle_2"));
// A         //Aacts.Add(new Actioner(start_speckle_4, "speckle_4"));
// A 
// A         return acts;
// A     }
// A     public void add_to_pic_timer(float value)
// A     {
// A         float val_now = get_pic_timer();
// A         float val_new = val_now + value;
// A         set_pic_timer(val_new);
// A 
// A     }
// A     public void set_pic_timer(float value)
// A     {
// A         this.pic_timer = value;
// A     }
// A     public float get_pic_timer()
// A     {
// A         return pic_timer;
// A     }
// A 
// A     public bool get_blades_created()
// A     {
// A         return blades_created;
// A     }
// A     public void set_blades_created(bool input)
// A     {
// A         blades_created = input;
// A     }
// A 
// A     public int get_blade_idx()
// A     {
// A         return this.blade_idx_secret;
// A     }
// A     public void set_blade_idx(int input)
// A     {
// A         this.blade_idx_secret = input;
// A     }
// A     //05092024 public void set_render_idx(int value)
// A     //05092024 {
// A     //05092024     this.render_idx = value;
// A     //05092024 }
// A     //05092024 public int get_render_idx()
// A     //05092024 {
// A     //05092024     return this.render_idx ;
// A     //05092024 }
// A     public void manage_lighting_settings()
// A     {
// A         //26062024 // Create an instance of LightingSettings
// A         //26062024 LightingSettings lightingSettings = new LightingSettings();
// A         //26062024 
// A         //26062024 // Configure the LightingSettings object
// A         //26062024 lightingSettings.realtimeEnvironmentLighting.e
// A         //26062024 
// A         //26062024 // Assign the LightingSettings object to the active Scene
// A         //26062024 Lightmapping.lightingSettings = lightingSettings;
// A         //26062024 return Lightmapping;
// A     }
// A 
// A     public GameObject start_renders(string blade_path, int blade_idx = -1, bool with_uv_init = false)
// A     {
// A         // info (paul): Start the machinery of creating and rendering all the blades
// A         GameObject surface_obj = load_obj_from_verts(blade_path: blade_path,
// A             blade_idx: blade_idx, with_uv_init: with_uv_init);
// A         return surface_obj;
// A     }
// A 
// A     public void png2tiff()
// A     {
// A         // info (paul): load png files in DIC_package directory, convert them to tiffs and save that again
// A 
// A         //Process proc = new Process();
// A 
// A         ProcessStartInfo psi = new ProcessStartInfo();
// A         //psi.FileName = "C:/Users/go73jem/AppData/Local/Microsoft/WindowsApps/PythonSoftwareFoundation.Python.3.12_qbz5n2kfra8p0";
// A         psi.FileName = "python";
// A 
// A         var script = "C:/Users/go73jem/unter2_Windows_native/Assets/script_paul.py";
// A         psi.Arguments = $"\"{script}\"";
// A 
// A         psi.UseShellExecute = false;
// A         psi.CreateNoWindow = true;
// A         psi.RedirectStandardOutput = true;
// A         psi.RedirectStandardError = true;
// A 
// A         var errors = "";
// A         var results = "";
// A 
// A         using (var process = Process.Start(psi))
// A         {
// A             errors = process.StandardError.ReadToEnd();
// A             results = process.StandardOutput.ReadToEnd();
// A         }
// A 
// A         //Process.Start("python", "script_paul21.py").WaitForExit();
// A     }
// A     public void call_main_batch()
// A     {
// A         // info (paul): call the main.bat file
// A 
// A         // info (paul): load png files in DIC_package directory, convert them to tiffs and save that again
// A 
// A         //Process proc = new Process();
// A 
// A         ProcessStartInfo psi = new ProcessStartInfo();
// A         //psi.FileName = "C:/Users/go73jem/AppData/Local/Microsoft/WindowsApps/PythonSoftwareFoundation.Python.3.12_qbz5n2kfra8p0";
// A         psi.FileName = "C:/Users/go73jem/Desktop/DIC_package/main_remote.bat";
// A 
// A         //var script = "C:/Users/go73jem/unter2_Windows_native/Assets/script_paul.py";
// A         psi.Arguments = $"\"\"";
// A 
// A         psi.UseShellExecute = false;
// A         psi.CreateNoWindow = true;
// A         psi.RedirectStandardOutput = true;
// A         psi.RedirectStandardError = true;
// A 
// A         var errors = "";
// A         var results = "";
// A 
// A         using (var process = Process.Start(psi))
// A         {
// A             errors = process.StandardError.ReadToEnd();
// A             results = process.StandardOutput.ReadToEnd();
// A         }
// A 
// A         Debug.Log("ERRORS: ");
// A         Debug.Log(errors.ToString());
// A         Debug.Log("Results: ");
// A         Debug.Log(results);
// A 
// A         //Process.Start("python", "script_paul21.py").WaitForExit();
// A     }
// A 
// A     public int get_match_steps()
// A     {
// A         return match_steps;
// A     }
// A 
// A     public void set_match_steps(int input)
// A     {
// A         match_steps = input;
// A         //dt_compare = match_steps;
// A     }
// A 
// A     public void set_ready_for_next_blade(bool input)
// A     {
// A         ready_for_next_blade = input;
// A     }
// A     public bool get_ready_for_next_blade()
// A     {
// A         return ready_for_next_blade;
// A     }
// A 
// A     public bool get_visible()
// A     {
// A         return is_visible;
// A     }
// A     public void set_visible(bool input)
// A     {
// A         is_visible = input;
// A     }
// A 
// A     public string blade_path_for_idx(int idx)
// A     {
// A         string blade_dir = "C:/Users/go73jem/Desktop/play_blender_pycahrm/write_mesh/verts_" + idx.ToString() + ".txt";
// A         return blade_dir;
// A     }
// A 
// A     public void set_path_stereo(string new_path)
// A     {
// A         this.path_stereo = new_path;
// A     }
// A 
// A     public string get_path_stereo()
// A     {
// A         return path_stereo;
// A     }
// A     //public void set_path_time_flow(string new_path)
// A     //{
// A     //    this.path_time_flow_u = new_path;
// A     //}
// A 
// A     public string get_path_time_flow_u()
// A     {
// A         return path_time_flow_u;
// A     }
// A 
// A     public void set_path_time_flow_u(string new_path)
// A     {
// A         this.path_time_flow_u = new_path;
// A     }
// A 
// A     public string get_path_time_flow_v()
// A     {
// A         return path_time_flow_v;
// A     }
// A 
// A     public void set_path_time_flow_v(string new_path)
// A     {
// A         this.path_time_flow_v = new_path;
// A     }
// A     public void init_params()
// A     {
// A         // info (paul): set default values for parameters; 
// A         //      they may be changed later during the "game"
// A 
// A         this.force_flat = true;
// A 
// A         //03072024 this.set_path_stereo("C:/Users/go73jem/Desktop/DIC_package/unter2_Windows_native_Data/");//08062024 "C:/Users/go73jem/Pictures/displacements_u.csv";
// A         this.set_path_stereo("C:/Users/go73jem/Desktop/DIC_package/");
// A 
// A         //string path_stereo = "C:/Users/go73jem/Pictures/displacements_u.csv";
// A         //10052024 string path_time_flow = "C:/Users/go73jem/unter2_Windows_native/Assets/Resources/Targets/flow_159_160.png";
// A         //16052024 string path_time_flow = "C:/Users/go73jem/Desktop/DIC_package/time_flow.png";//12052024 
// A         //05062024 this.path_time_flow = "C:/Users/go73jem/Desktop/DIC_package_pre_05062024/time_flow/";
// A         this.set_path_time_flow_u("C:/Users/go73jem/Desktop/DIC_package/");//08062024 "C:/Users/go73jem/Desktop/DIC_package/time_flow/";
// A         this.set_path_time_flow_v("C:/Users/go73jem/Desktop/DIC_package/");//08062024 "C:/Users/go73jem/Desktop/DIC_package/time_flow/";
// A 
// A         this.res_x = -1;//13062024 1026; //120;//200;//600;
// A         this.res_y = -1;//13062024 1031; //192;//576;// 1728;
// A         this.im_cnt = 20;
// A         this.t_idx = 0;//2;//14062024 2;
// A 
// A         set_match_steps(1);//1//2//15062024 6);
// A     }
// A 
// A     public void init_t_control(T_control_script t_control_input)
// A     {
// A         this.t_control = t_control_input;
// A     }
// A     public void init_match_control(Match_steps match_control)
// A     {
// A         this.match_control = match_control;
// A     }
// A 
// A     public void init_strain_control(Strain input)
// A     {
// A         this.strain_control = input;
// A     }
// A     public void init_u_v_control(U_V input)
// A     {
// A         this.u_v_control = input;
// A     }
// A     public void init_experiment_control(Experiment_Control input)
// A     {
// A         this.experiment_control = input;
// A     }
// A     public void init_strain_d_control(Strain_D input)
// A     {
// A         this.strain_d = input;
// A     }
// A 
// A     public Vector2[] get_uv_start()
// A     {
// A         return this.uv_start;
// A     }
// A     public void set_uv_start(Vector2[] input)
// A     {
// A         this.uv_start = input;
// A     }
// A     public void set_force_flat(bool force_flat_input)
// A     {
// A         force_flat = force_flat_input;
// A     }
// A     public void set_res_x(int res_x_input)
// A     {
// A         res_x = res_x_input;
// A     }
// A     public void set_res_y(int res_y_input)
// A     {
// A         res_y = res_y_input;
// A     }
// A     public void set_im_cnt(int im_cnt_input)
// A     {
// A         im_cnt = im_cnt_input;
// A     }
// A     public void set_t_idx(int t_idx_input)
// A     {
// A         t_idx = t_idx_input;
// A     }
// A     public void set_strain_mode(string input)
// A     {
// A         strain_mode = input;
// A     }
// A     public void set_u_v_mode(string input)
// A     {
// A         u_v_mode = input;
// A     }
// A     public void set_strain_d_mode(string input)
// A     {
// A         strain_d_mode = input;
// A     }
// A 
// A     public string get_u_v_mode()
// A     {
// A         return u_v_mode;
// A     }
// A 
// A     public bool get_force_flat()
// A     {
// A         return force_flat;
// A     }
// A     public int get_res_x()
// A     {
// A         return res_x;
// A     }
// A     public int get_res_y()
// A     {
// A         return res_y;
// A     }
// A     public int get_im_cnt()
// A     {
// A         return im_cnt;
// A     }
// A     public int get_t_idx()
// A     {
// A         return t_idx;
// A     }
// A     public string get_strain_mode()
// A     {
// A         return strain_mode;
// A     }
// A     public string get_strain_d_mode()
// A     {
// A         return strain_d_mode;
// A     }
// A 
// A     public void save_all()
// A     {
// A         // info (paul): save all images, that the paper may need, in the corresponding directories
// A 
// A         //plot_mode = "value_ref";////"loss_rel""loss_abs""value""value_ref"""
// A         //heights_mode = "value";//"value""value_ref""loss_abs""loss_rel"""
// A         //paint_with = "uv";//"uv";"heights"
// A 
// A         refresh_plane_with_params();
// A     }
// A 
// A     public List<int[]> init_tris_empty()
// A     {
// A         List<int[]> blade_tris = new List<int[]>();
// A 
// A         for (int i = blade_idx_min; i < blade_idx_max; i++)
// A         {
// A             blade_tris.Add(null);
// A         }
// A         return blade_tris;
// A     }
// A     public void refresh_plane_with_params(
// A         string path_stereo = null,
// A         string path_time_flow_u = null, string path_time_flow_v = null,
// A         int res_x = -1, int res_y = -1, int im_cnt = -1, int t_idx = -1, bool with_save = false)
// A     {
// A         // info (paul): init params
// A         if (path_stereo == null) { path_stereo = this.get_path_stereo(); }
// A         if (path_time_flow_u == null) { path_time_flow_u = this.get_path_time_flow_u(); }
// A         if (path_time_flow_v == null) { path_time_flow_v = this.get_path_time_flow_v(); }
// A         //if (res_x == -1) { res_x = this.res_x; }
// A         //if (res_y == -1) { res_y = this.res_y; }
// A         if (im_cnt == -1) { im_cnt = this.im_cnt; }
// A         if (t_idx == -1) { t_idx = this.get_t_idx(); }
// A         if (blade_tris == null) { set_blade_tris(init_tris_empty()); }
// A 
// A         // info (paul): heights/ heights_ref/ heights_diff
// A         bool force_flat = this.get_force_flat();
// A 
// A         //08102024 List<List<float>> heights = read_dists(path_stereo, t_idx);
// A         (List<List<float>> heights, List<List<float>> heights_uncut) = read_heights_tv();
// A         (List<List<float>> heights_ref, List<List<float>> heights_ref_uncut) = load_heights_ref();
// A         List<List<float>> diff_im = find_diff(heights, heights_ref, mode: heights_mode);//23062024 "relative");
// A 
// A         // info (paul): save diffs as png:
// A         Texture2D diff_tex = mat2tex(diff_im, with_switch_dims: true);
// A         //16072024 save_png(diff_tex, cam_idx: 0, blade_idx: 0, label: "_diff");
// A 
// A         (List<List<float>> heights_chosen, float scale_factor) = choose_heights(heights, heights_ref, diff_im);//diff_im
// A 
// A         manage_heights_loss(heights_uncut, heights_ref_uncut, scale_factor);
// A 
// A         // info (paul): make the actual plane object
// A         clean_platine_plane();
// A         GameObject platine_plane;
// A         write_mat_for_debug(heights);
// A         (platine_plane, _, res_x, res_y) = make_platine_plane(heights_chosen, null,
// A             path_time_flow_u, path_time_flow_v, im_cnt: im_cnt,
// A             force_flat: force_flat, t_idx: t_idx, scale_factor: scale_factor, with_save: with_save);
// A         (this.res_x, this.res_y) = (res_x, res_y);
// A         assign_to_cam(platine_plane);
// A 
// A         // info (paul): assign to t_panel
// A         if (t_control != null)
// A         {
// A             match_control.refresh_panel(t_idx);
// A             t_control.refresh_t_panel(t_idx);
// A             strain_control.refresh_info(strain_mode);
// A             u_v_control.refresh_info(u_v_mode);
// A             strain_d.refresh_info(strain_d_mode);
// A             experiment_control.refresh_info(experiment);
// A         }
// A     }
// A     public void aaaaaa()
// A     {
// A         ;
// A     }
// A     public (List<List<float>>, List<List<float>>) read_heights_tv()
// A     {
// A         //C:/Users/go73jem/Desktop/DIC_package/exp_normal/time_flow_v/heights_tv
// A         //09102024 float[][] floats = load_floats2(file_name: "heights_tv");
// A         //09102024 List<List<float>> lists = floats2_to_lists(floats);
// A 
// A         // ----------------------------------------------------------
// A         List<List<float>> mat_u_pre_pre = load_tex_to_mat("C:/Users/go73jem/Desktop/DIC_package/exp_normal/tv_heights.png", with_switch_dims: false);
// A         List<List<float>> mat_u_pre = switch_mat(mat_u_pre_pre);
// A 
// A         // info (paul): find min and max val
// A         string min_max_file = "C:/Users/go73jem/Desktop/DIC_package/min_max_u_2.txt";
// A         string min_max_str = load_txt_line(min_max_file);
// A         string[] strs = min_max_str.Split(" ");
// A         float min_val = float.Parse(strs[0]);
// A         float max_val = float.Parse(strs[1]);
// A         List<List<float>> mat_u = unnorm_mat(mat_u_pre, min_val, max_val);
// A 
// A         // info (paul): reconstruct the distance map in 3d space from disparities
// A         List<List<float>> mat = mat_raw2dists(mat_u);
// A 
// A         // info (paul): cut off floor
// A         mat = transpose_mat(mat);//24062024
// A         List<List<float>> mat_cut = cut_off(mat);
// A 
// A         return (mat_cut, mat);
// A     }
// A     public List<List<float>> load_tex_to_mat(string path, bool with_switch_dims = false)
// A     {
// A         // info (paul): load a tex and convvert it into mat
// A 
// A         string file_path_u = path;//"C:/Users/go73jem/Desktop/DIC_package/exp_normal/time_flow_v/debug_im_cv.png";
// A         byte[] im_bytes_u = System.IO.File.ReadAllBytes(file_path_u);
// A 
// A         // info (paul): assuming, that the resolution of the first image is 
// A         //      the resolution of all the images
// A         if (true)//20062024 (t_idx == 0)
// A         {
// A             (res_x, res_y) = bytes2res(im_bytes_u);
// A         }
// A 
// A         Texture2D tex_albedo_u = new Texture2D(res_x, res_y);
// A         tex_albedo_u.LoadImage(im_bytes_u);
// A         List<List<float>> mat_u = tex2mat(tex_albedo_u, with_switch_dims: with_switch_dims);
// A         return mat_u;
// A     }
// A 
// A     public void manage_heights_loss(List<List<float>> heights, List<List<float>> heights_ref, float scale_factor)
// A     {
// A         // info (paul): new parts for heightsList<List<float>>
// A         if (get_paint_with() == "heights")
// A         {
// A             (List<List<float>> stream_heights, float coverage_l) = choose_heights_or_loss(heights, heights_ref, scale_factor);
// A             //A (List<List<float>> stream_u, List<List<float>> stream_v, float coverage) = manage_flow_or_loss(heights_chosen, heights_chosen, t_idx);
// A 
// A             // info (paul): scale label
// A             //(float stream_u_min, float stream_u_max) = find_min_max(stream_u);
// A             //(float stream_v_min, float stream_v_max) = find_min_max(stream_v);
// A             (float stream_u_mean, float dev_u) = find_mean_in_all(stream_heights, span: 20, coverage: coverage_l);//find_mean_in_span(stream_u, span: 20, j_off: 50);//find_mean_in_all(stream_u, span: 20);//find_mean_in_span(stream_u, span: 20, j_off: 50);//find_mean_in_span(stream_u, span: 20);
// A             (float stream_v_mean, float dev_v) = find_mean_in_all(stream_heights, span: 20, coverage: coverage_l);//find_mean_in_span(stream_v, span: 20, j_off: 50);//find_mean_in_all(stream_v, span: 20);//find_mean_in_span(stream_v, span: 20, j_off: 50);//find_mean_in_span(stream_v, span: 20);
// A             update_scale_label(stream_u_mean, dev_u, stream_v_mean, dev_v);
// A         }
// A     }
// A     public List<List<float>> cut_off(List<List<float>> heights_chosen_input)
// A     {
// A         List<List<float>> heights_chosen = copy_mat(heights_chosen_input);
// A 
// A         (float min_val, float max_val) = find_min_max(heights_chosen, with_padding: true);
// A         float floor = min_val + 1f;//14072024 max_val - 1000f;//10072024 30f;
// A 
// A         //24062024 // info (paul): cut off values below floor
// A         //24062024 heights_chosen = cut_off_below_floor(heights_chosen, floor);
// A 
// A         // info (paul): finding the "meaningful" min
// A         float min_meaningful = find_min_meaningful(heights_chosen, floor);
// A 
// A         // info (paul): cut off values below floor
// A         heights_chosen = cut_off_below_floor(heights_chosen, floor: min_meaningful);
// A 
// A         return heights_chosen;
// A     }
// A 
// A     public float find_min_meaningful(List<List<float>> heights_chosen, float floor)
// A     {
// A         float min_val_meaningful = 9999f;
// A         for (int i = 0; i < heights_chosen.Count; i++)
// A         {
// A             for (int j = 0; j < heights_chosen[0].Count; j++)
// A             {
// A                 float height_ij = heights_chosen[i][j];
// A                 if (floor < height_ij)//floor+1 < height_ij
// A                 {
// A                     min_val_meaningful = Mathf.Min(height_ij, min_val_meaningful);
// A                 }
// A                 //24062024 heights_chosen[i][j] = height_ij;
// A             }
// A         }
// A         return min_val_meaningful;
// A     }
// A 
// A     public List<List<float>> cut_off_below_floor(List<List<float>> heights_chosen, float floor)
// A     {
// A         // info (paul): cut off below floor
// A         for (int i = 0; i < heights_chosen.Count; i++)
// A         {
// A             for (int j = 0; j < heights_chosen[0].Count; j++)
// A             {
// A                 float height_ij = heights_chosen[i][j];
// A                 if (height_ij < floor)
// A                 {
// A                     height_ij = floor;
// A                 }
// A                 heights_chosen[i][j] = height_ij;
// A                 heights_chosen[i][j] -= floor;
// A             }
// A         }
// A 
// A         // info (paul): subtract floor
// A         /*for (int i = 0; i < heights_chosen.Count; i++)
// A         {
// A             for (int j = 0; j < heights_chosen[0].Count; j++)
// A             {
// A                 float height_ij = heights_chosen[i][j];
// A                 if (height_ij < floor)
// A                 {
// A                     height_ij = floor;
// A                 }
// A                 heights_chosen[i][j] = height_ij;
// A 
// A                 //heights_chosen[i][j] -= floor;
// A             }
// A         }*/
// A 
// A         return heights_chosen;
// A     }
// A 
// A     public (List<List<float>>, float) choose_heights(List<List<float>> heights, List<List<float>> heights_ref, List<List<float>> diff_im)
// A     {
// A         // info (paul): Choose heights based on the mode, what should be plotted as heightmap
// A 
// A 
// A         List<List<float>> chosen = null;
// A         float scale_factor = 1f;
// A 
// A         string heights_mode_l = this.get_heights_mode();
// A         if (heights_mode_l == "value")
// A         {
// A             chosen = heights;
// A             scale_factor = 1f;//08102024 3f;
// A         }
// A         if (heights_mode_l == "value_ref")
// A         {
// A             chosen = heights_ref;
// A             scale_factor = 1f;
// A         }
// A         if (heights_mode_l == "loss_abs")
// A         {
// A             chosen = diff_im;//TODO: rel/abs
// A             scale_factor = 1f;//08102024 3f;
// A         }
// A         if (heights_mode_l == "loss_rel")
// A         {
// A             chosen = diff_im; // TODO: rel/abs
// A             scale_factor = 1f;//08102024 3f;
// A         }
// A 
// A         return (chosen, scale_factor);
// A     }
// A     public string get_heights_mode()
// A     {
// A         return this.heights_mode;
// A     }
// A 
// A     public void set_heights_mode(string input, bool is_internal = false)
// A     {
// A         this.heights_mode = input;
// A         if (!is_internal)
// A         {
// A             set_paint_with("heights");
// A             //set_plot_mode("", is_internal: true);
// A         }
// A     }
// A 
// A     public List<List<float>> find_diff(List<List<float>> heights, List<List<float>> heights_ref, string mode = "absolute")
// A     {
// A         // info (paul): mode: "absolute": the normal difference is used
// A         //                    "relative": difference/value (the relative difference) is used
// A         //              heights_ref has not the same resolution as heights, due to the NCorr scale down,
// A         //              be aware of that.
// A 
// A         int length = heights.Count;
// A         int height = heights[0].Count;
// A 
// A         List<List<float>> diffs = zeros_of_size(length, height);
// A         (float min_height, float max_height) = find_min_max(heights, with_padding: true);
// A 
// A         for (int i = 0; i < length; i++)
// A         {
// A             for (int j = 0; j < height; j++)
// A             {
// A                 if ((i < heights.Count) && (j < heights[0].Count))
// A                 {
// A                     int scale_fac = 1;//08102024 3;
// A                     float heights_ij = heights[i][j];
// A                     float heights_ref_ij = heights_ref[scale_fac * i][scale_fac * j];
// A                     float diff = Mathf.Abs(heights_ij - heights_ref_ij);
// A 
// A                     // info (paul): if the heights_ij is close to floor/ heights_min, 
// A                     //      it is apparently out of the roi, i.e. we se the diff just to zero, 
// A                     //      because it would be meaningless to calculate it.
// A                     if (heights_ij < min_height + 1f)
// A                     {
// A                         diff = 0f;
// A                     }
// A 
// A                     // info (paul): relative difference with somehow the maximum of the
// A                     //      two value as "value". Perhaps there is a better solution for "value", who knows. 
// A                     float value = Mathf.Max(heights_ij, heights_ref_ij);
// A                     float frac = diff / value;
// A 
// A                     if (frac != 0f && !float.IsNaN(frac) && !float.IsInfinity(frac))
// A                     {
// A                         ;
// A                     }
// A                     if (diff > value)
// A                     {
// A                         frac = 0f; // info (paul): if diff bigger than value, 
// A                         //      than probably value is de facto 0, therefore
// A                         //      we ignore this.
// A                     }
// A 
// A                     if (mode == "loss_rel") //"relative"
// A                     {
// A                         diffs[i][j] = frac;//diff;//frac;
// A                     }
// A                     if (mode == "loss_abs") // "absolute"
// A                     {
// A                         diffs[i][j] = diff; //value;//diff;//frac;
// A                     }
// A                 }
// A             }
// A         }
// A 
// A         return diffs;
// A     }
// A 
// A     public void clean_platine_plane()
// A     {
// A         GameObject plane = GameObject.Find("platine_plane");
// A         if (plane != null)
// A         {
// A             remove_obj(plane);
// A         }
// A     }
// A 
// A     public void remove_obj(GameObject obj)
// A     {
// A         // info (paul): destroy platine plane
// A         Destroy(obj);
// A     }
// A 
// A     public void assign_to_cam(GameObject platine_plane)
// A     {
// A         GameObject cam_obj = GameObject.Find("MainCamera");
// A         cam_script = cam_obj.GetComponent<Cam_manager>();
// A 
// A         // info (paul): assign the object to the camera:
// A         cam_script.platine_plane = platine_plane.transform;
// A 
// A 
// A     }
// A 
// A     public void log(string text)
// A     {
// A         log_field.GetComponent<TextMeshProUGUI>().text += text;
// A     }
// A 
// A     // Update is called once per frame
// A 
// A     // info (paul): for the speckle rendering
// A     int speckle_idx = 0;
// A     int reg_idx = 0;
// A 
// A     void Update()
// A     {
// A         //23092024 update_blade_pics_if();
// A         //update_render_acts();
// A 
// A         if (speckle_idx < speckle_acts.Count)
// A         {
// A             this.speckle_size = speckle_acts[speckle_idx].Item2;
// A             this.speckle_dist = speckle_acts[speckle_idx].Item3;
// A             speckle_acts[speckle_idx].Item1.Invoke();
// A             speckle_idx += 1;
// A         }
// A         if (reg_idx < exp_cv_acts.Count)
// A         {
// A             exp_cv_acts[reg_idx].act.Invoke();
// A             reg_idx += 1;
// A         }
// A     }
// A 
// A     //05092024public void update_render_acts() - seems to be never used
// A     //05092024{
// A     //05092024    if (ready_for_next_act && render_idx < render_acts.Count)
// A     //05092024    {
// A     //05092024        this.render_acts[render_idx].act.Invoke();
// A     //05092024        set_render_idx(render_idx + 1);
// A     //05092024        ready_for_next_act = true;
// A     //05092024    }
// A     //05092024}
// A 
// A     public void update_blade_pics_if()
// A     {
// A         //23092024 bool blades_created = get_blades_created();
// A         //23092024 if (blades_created)
// A         //23092024 {
// A         //23092024     update_blade_pics();
// A         //23092024 }
// A     }
// A 
// A     public void start_speckle_0_035()
// A     {
// A         start_speckle(diameter: 0.035f);
// A     }
// A     public void start_speckle_0_07()
// A     {
// A         start_speckle(diameter: 0.07f);
// A     }
// A     public void start_speckle_0_35()
// A     {
// A         start_speckle(diameter: 0.35f);
// A     }
// A     public void start_speckle_0_175()
// A     {
// A         start_speckle(diameter: 0.175f);
// A     }
// A     public void start_speckle_0_7()
// A     {
// A         start_speckle(diameter: 0.7f);
// A     }
// A     public void start_speckle_1_4()
// A     {
// A         start_speckle(diameter: 1.4f);
// A     }
// A     public void start_speckle_2_1()
// A     {
// A         start_speckle(diameter: 2.1f);
// A     }
// A     public void start_speckle_2_8()
// A     {
// A         start_speckle(diameter: 2.8f);
// A     }
// A 
// A     public void start_speckle_05()
// A     {
// A         start_speckle(diameter: 0.5f);
// A     }
// A     public void start_speckle_1()
// A     {
// A         start_speckle(diameter: 1f);
// A     }
// A     public void start_speckle_2()
// A     {
// A         start_speckle(diameter: 2f);
// A     }
// A     public void start_speckle_4()
// A     {
// A         start_speckle(diameter: 4f);
// A     }
// A     public void start_speckle(float diameter = -1f)
// A     {
// A         // info (paul): do speckle analysis, since lighting is finished
// A         ready_for_next_act = true;
// A         done_render_acts = true;
// A         set_ready_for_next_blade(true);
// A         set_blade_idx(-1);
// A         set_pic_timer(0f);
// A         set_experiment("speckle_" + diameter.ToString("0.000"));//
// A         bool below_max = (get_blade_idx() + 1) < (blade_idx_max - blade_idx_min);
// A         string blade_path_first = blade_path_for_idx(blade_idx_min);
// A         set_up_lighting(y_coord: 30f);
// A 
// A         string speckle_file = "speckle_" + diameter.ToString("0.000");//(diameter).ToString();
// A         set_speckle_file(speckle_file);
// A         //04092024 set_speckle_file("checkerboard");
// A 
// A         List<GameObject> blades = collect_blades();
// A         for (int i = 0; i < blades.Count; i++)
// A         {
// A             apply_speckles(blades[i]);
// A         }
// A         start_renders(blade_path: blade_path_first, blade_idx: blade_idx_min, with_uv_init: true);
// A         set_below_max(below_max);
// A         //18072024 return below_max;
// A     }
// A     public void start_lighting_0001()
// A     {
// A         start_lighting(intensity: 0.001f);
// A     }
// A     public void start_lighting_001()
// A     {
// A         start_lighting(intensity: 0.01f);
// A     }
// A     public void start_lighting_002()
// A     {
// A         start_lighting(intensity: 0.02f);
// A     }
// A     public void start_lighting_003()
// A     {
// A         start_lighting(intensity: 0.03f);
// A     }
// A     public void start_lighting_004()
// A     {
// A         start_lighting(intensity: 0.04f);
// A     }
// A     public void start_lighting_005()
// A     {
// A         start_lighting(intensity: 0.05f);
// A     }
// A     public void start_lighting_01()
// A     {
// A         start_lighting(intensity: 0.1f);
// A     }
// A     public void start_lighting_02()
// A     {
// A         start_lighting(intensity: 0.2f);
// A     }
// A     public void start_lighting_05()
// A     {
// A         start_lighting(intensity: 0.5f);
// A     }
// A     public void start_lighting_07()
// A     {
// A         start_lighting(intensity: 0.7f);
// A     }
// A     public void start_lighting_08()
// A     {
// A         start_lighting(intensity: 0.8f);
// A     }
// A     public void start_lighting_1()
// A     {
// A         start_lighting(intensity: 1f);
// A     }
// A     public void start_lighting_2()
// A     {
// A         start_lighting(intensity: 2f);
// A     }
// A     public void start_lighting_3()
// A     {
// A         start_lighting(intensity: 3f);
// A     }
// A     public void start_lighting_4()
// A     {
// A         start_lighting(intensity: 4f);
// A     }
// A     public void start_lighting_5()
// A     {
// A         start_lighting(intensity: 5f);
// A     }
// A     public void start_lighting_6()
// A     {
// A         start_lighting(intensity: 6f);
// A     }
// A     public void start_lighting_10()
// A     {
// A         start_lighting(intensity: 10f);
// A     }
// A     public void start_lighting_100()
// A     {
// A         start_lighting(intensity: 100f);
// A     }
// A     public void start_lighting_10000()
// A     {
// A         start_lighting(intensity: 10000f);
// A     }
// A     public void start_lighting_100000000()
// A     {
// A         start_lighting(intensity: 100000000f);
// A     }
// A     public string remove_dots(string val)
// A     {
// A         try
// A         {
// A             return val.Replace(".", "");
// A         }
// A         catch
// A         {
// A             return val.Replace(".", "");
// A         }
// A     }
// A     public void start_lighting(float intensity = -1f)
// A     {
// A         // info (paul): do lighting analysis, since "normal" is finished
// A         done_normal_ims = true;
// A         ready_for_next_act = true;
// A         set_ready_for_next_blade(true);
// A         set_blade_idx(-1);
// A         set_pic_timer(0f);
// A         set_experiment("lighting_" + remove_dots(intensity.ToString()));
// A         bool below_max = (get_blade_idx() + 1) < (blade_idx_max - blade_idx_min);
// A         string blade_path_first = blade_path_for_idx(blade_idx_min);
// A         set_up_lighting(y_coord: -30f, intensity: intensity);
// A         start_renders(blade_path: blade_path_first, blade_idx: blade_idx_min, with_uv_init: true);
// A         set_below_max(below_max);
// A         //return below_max;
// A     }
// A 
// A     public void start_exp_normal()
// A     {
// A         done_normal_exps = true;
// A         set_ready_for_next_blade(true);
// A         set_blade_idx(-1);
// A         set_pic_timer(0f);
// A         set_experiment("exp_normal");
// A         string blade_path_first = blade_path_for_idx(blade_idx_min);
// A         start_renders(blade_path: blade_path_first, blade_idx: blade_idx_min, with_uv_init: true);
// A     }
// A 
// A     public int update_render(bool below_max, int blade_idx_l)
// A     {
// A         if (get_ready_for_next_blade() && below_max)
// A         {
// A             //set_blade_idx(blade_idx_l + 1);
// A             blade_idx_l += 1;
// A             set_ready_for_next_blade(false);
// A             activate_blade(blade_idx: blade_idx_l);
// A         }
// A 
// A         if (!get_ready_for_next_blade())
// A         {
// A             add_to_pic_timer(Time.deltaTime);
// A         }
// A 
// A         if (get_pic_timer() > pic_time && below_max)
// A         {
// A             add_to_pic_timer(-pic_time);
// A             take_pic(blade_idx_l, cam_idx: 0);
// A             take_pic(blade_idx_l, cam_idx: 1);
// A 
// A             take_ref_pic(blade_idx_l, cam_idx: 0);
// A         }
// A         return blade_idx_l;
// A     }
// A     bool below_max = true;
// A     public void set_below_max(bool input)
// A     {
// A         this.below_max = input;
// A     }
// A     public bool get_below_max()
// A     {
// A         return below_max;
// A     }
// A 
// A 
// A     public List<Actioner> set_up_render_list()
// A     {
// A         // info (paul): make the individual render actions as a list
// A 
// A         List<Actioner> acts = new List<Actioner>();
// A         acts = add_im_steps(acts);
// A 
// A         return acts;
// A 
// A     }
// A     public List<Actioner> add_im_steps(List<Actioner> acts)
// A     {
// A         if (with_exp)
// A         {
// A             for (int render_idx = 0; render_idx < render_acts.Count; render_idx++)
// A             {
// A                 acts.Add(new Actioner(exe_render_acts, "exe_render_acts"));
// A                 for (int i = 0; i < blade_idx_max - blade_idx_min; i++)
// A                 {
// A                     acts.Add(new Actioner(activate_blade_act, "activate_blade_act"));
// A                     acts.Add(new Actioner(take_pic_act, "take_pic_act"));
// A                     acts.Add(new Actioner(take_pic_act, "take_pic_act"));
// A                     acts.Add(new Actioner(take_ref_pic_act, "take_ref_pic_act"));
// A                 }
// A 
// A                 //25092024 acts.Add(new Actioner(exe_render_acts, "exe_render_acts"));
// A             }
// A         }
// A         acts.Add(new Actioner(manage_cv, "manage_cv"));
// A         return acts;
// A     }
// A 
// A     public void manage_cv()
// A     {
// A         set_experiment("exp_normal");
// A 
// A         // info (paul): The refresh part
// A         if (with_exp)//25092024 
// A         {
// A             manage_distortion_ground_truth();//05092024 //24092024
// A         }
// A 
// A         if (this.with_main)
// A         {
// A             // info (paul): The call DIC remotely part (22062024)
// A             //30082024 png2tiff();
// A             for (int i = 0; i < render_acts.Count; i++)
// A             {
// A                 string exp_l = render_acts[i].label;
// A                 set_experiment(exp_l);
// A                 cv_main(i, d_cam: 1, with_dt: false);
// A                 cv_main(i, d_cam: 0, with_dt: true);
// A             }
// A             set_experiment("exp_normal");
// A 
// A             //08102024 call_main_batch();// - seems to currently contain no matlab, only TV
// A             refresh_plane_with_params();
// A         }
// A 
// A         done_ground_truth = true;
// A     }
// A 
// A     public void exe_render_acts()
// A     {
// A         this.render_acts[render_idx].act.Invoke();
// A         render_idx += 1;
// A     }
// A     public void activate_blade_act()
// A     {
// A         int blade_idx_l = get_blade_idx();
// A         blade_idx_l += 1;
// A         set_blade_idx(blade_idx_l);
// A         activate_blade(blade_idx: blade_idx_l);
// A     }
// A     //public void take_ref_pic_act()
// A     //{
// A     //    int blade_idx_l = get_blade_idx();
// A     //    take_ref_pic(blade_idx_l, cam_idx: 0);
// A     //}
// A     public void update_blade_pics()
// A     {
// A         //23092024 // blades times
// A         //23092024 activate_blade(blade_idx: blade_idx_l);
// A         //23092024 
// A         //23092024 take_pic(blade_idx_l, cam_idx: 0);
// A         //23092024 take_pic(blade_idx_l, cam_idx: 1);
// A         //23092024 take_ref_pic(blade_idx_l, cam_idx: 0);
// A         //23092024 
// A         //23092024 // one time
// A         //23092024 this.render_acts[render_idx].act.Invoke();
// A         //23092024 render_idx += 1;
// A 
// A 
// A     }
// A     public void manage_distortion_ground_truth()
// A     {
// A         List<GameObject> blades = collect_blades();
// A         this.set_blades(blades);
// A         this.init_blade_tris(blades);
// A         for (int i = 0; i < blades.Count; i++)
// A         {
// A             // info (paul): We always match the blade i to the first blade at idx 0; 
// A             //      This means, that for i = 0, obviously all values will be zero
// A 
// A             int idx_other = Mathf.Min((i + get_dt_compare()), blades.Count - 1);
// A             (float[] d_xs, float[] d_ys, float[] d_zs) = construct_distortions(blades[i], blades[idx_other]);//0; i//24092024 1 oder so
// A 
// A             // info (paul): save values (actually, cam_idx doesn't make sense here, so we just set it to 0)
// A             save_floats_for_blade(d_xs, cam_idx: 0, blade_idx: i, label: "_d_xs", with_uv_mode: false);
// A             save_floats_for_blade(d_ys, cam_idx: 0, blade_idx: i, label: "_d_ys", with_uv_mode: false);
// A             save_floats_for_blade(d_zs, cam_idx: 0, blade_idx: i, label: "_d_zs", with_uv_mode: false);
// A 
// A             // info (paul): mesh tris
// A             Mesh current_mesh = blades[i].GetComponent<MeshFilter>().mesh;
// A             int[] tris = current_mesh.triangles;
// A             save_ints_for_blade(tris, cam_idx: 0, blade_idx: i, label: "_blade_tris", with_uv_mode: false);
// A 
// A             save_tris(blade_idx: i);
// A         }
// A     }
// A     public void init_blade_tris(List<GameObject> blades)
// A     {
// A         List<int[]> blade_tris = init_tris_empty();//25092024 new List<int[]>();
// A 
// A         for (int i = 0; i < blades.Count; i++)
// A         {
// A             Mesh mesh = blades[i].GetComponent<MeshFilter>().sharedMesh;
// A             blade_tris.Add(mesh.triangles);
// A         }
// A 
// A         set_blade_tris(blade_tris);
// A     }
// A     public void set_blades(List<GameObject> input)
// A     {
// A         this.blades = input;
// A     }
// A     public List<GameObject> get_blades()
// A     {
// A         return blades;
// A     }
// A     public void save_tris(int blade_idx)
// A     {
// A         // info (paul): save triangle idxs
// A 
// A         int width = cam_for_uv_0.pixelWidth;
// A         int height = cam_for_uv_0.pixelHeight;
// A 
// A         int[,] tris = new int[width, height];
// A 
// A         List<GameObject> blades = collect_blades();
// A 
// A         for (int i = 0; i < width; i++)
// A         {
// A             for (int j = 0; j < height; j++)
// A             {
// A                 int tri_idx = find_triangle(i, j);
// A                 if (tri_idx != -1)
// A                 {
// A                     ;
// A                 }
// A                 tris[i, j] = tri_idx;
// A             }
// A         }
// A 
// A         // info (paul): cam_idx is 0, because is irrelevant here anyway
// A         save_ints2_for_blade(tris, cam_idx: 0, blade_idx: blade_idx, label: "_tris", with_uv_mode: false);
// A     }
// A 
// A     public int[,] load_tris(int blade_idx)
// A     {
// A         string current_exp = get_experiment();
// A         set_experiment("exp_normal");
// A 
// A         // info (paul): cam_idx is 0, because is irrelevant here anyway
// A         int[,] tris = load_ints2_for_blade(cam_idx: 0, blade_idx: blade_idx, label: "_tris", with_uv_mode: false);
// A         set_experiment(current_exp);
// A 
// A         return tris;
// A     }
// A 
// A     public float[] add_floats(float[] floats_a, float[] floats_b)
// A     {
// A         float[] floats_c = new float[floats_a.Length];
// A 
// A         if (floats_b == null)
// A         {
// A             floats_b = new float[floats_a.Length];
// A         }
// A 
// A         for (int i = 0; i < floats_a.Length; i++)
// A         {
// A             floats_c[i] = floats_a[i] + floats_b[i];
// A         }
// A 
// A 
// A         return floats_c;
// A     }
// A     public float[] find_dxs_acc(int t_idx, string label)
// A     {
// A         float[] d_xs = null;
// A 
// A         for (int i_idx = t_idx; i_idx < t_idx + get_match_steps(); i_idx++)
// A         {
// A             float[] d_xs_l = load_floats_for_blade(cam_idx: 0, blade_idx: i_idx, label: label, with_uv_mode: false);
// A             d_xs = add_floats(d_xs_l, d_xs);
// A         }
// A         return d_xs;
// A     }
// A     public (float[], float[], float[]) load_distortion_ground_truth(int blade_idx)
// A     {
// A         List<GameObject> blades = collect_blades();
// A         //for (int i = 0; i < blades.Count; i++)
// A         //{
// A         // info (paul): We always match the blade i to the first blade at idx 0; 
// A         //      This means, that for i = 0, obviously all values will be zero
// A         //(float[] d_xs, float[] d_ys, float[] d_zs) = map_distortions(blades[0], blades[i]);
// A 
// A         // info (paul): save values (actually, cam_idx doesn't make sense here, so we just set it to 0)
// A 
// A         string current_exp = get_experiment();
// A         set_experiment("exp_normal");
// A 
// A         float[] d_xs = find_dxs_acc(blade_idx, "_d_xs");
// A         float[] d_ys = find_dxs_acc(blade_idx, "_d_ys");
// A         float[] d_zs = find_dxs_acc(blade_idx, "_d_zs");
// A 
// A         //float[] d_xs = load_floats_for_blade(cam_idx: 0, blade_idx: blade_idx, label: "_d_xs", with_uv_mode: false);
// A         //float[] d_ys = load_floats_for_blade(cam_idx: 0, blade_idx: blade_idx, label: "_d_ys", with_uv_mode: false);
// A         //float[] d_zs = load_floats_for_blade(cam_idx: 0, blade_idx: blade_idx, label: "_d_zs", with_uv_mode: false);
// A 
// A         int[] blade_tris_i = load_ints_for_blade(cam_idx: 0, blade_idx: blade_idx + get_match_steps(), label: "_blade_tris", with_uv_mode: false);
// A 
// A         if (true)//13072024 (blade_tris.Count < blade_idx_max - blade_idx_min)
// A         {
// A             //24092024 blade_tris[blade_idx] = blade_tris_i;
// A             set_blade_tris_at(blade_idx, blade_tris_i);
// A         }
// A 
// A         set_experiment(current_exp);
// A         //}
// A 
// A         return (d_xs, d_ys, d_zs);
// A     }
// A     public void set_blade_tris_at(int blade_idx, int[] slice)
// A     {
// A         List<int[]> blade_tris = get_blade_tris();
// A         blade_tris[blade_idx] = slice;
// A         set_blade_tris(blade_tris);
// A     }
// A     public void set_blade_tris(List<int[]> input)
// A     {
// A         this.blade_tris = input;
// A     }
// A     public List<int[]> get_blade_tris()
// A     {
// A         return this.blade_tris;
// A     }
// A     public (Vector3[], Vector3[]) pos2uvs(Vector3[] now, Vector3[] next)
// A     {
// A         // info (paul): map 3d coordinates to 2d position
// A 
// A         Vector3[] now_proj = new Vector3[now.Length];
// A         Vector3[] next_proj = new Vector3[next.Length];
// A 
// A         for (int i = 0; i < now.Length; i++)
// A         {
// A             now_proj[i] = cam_for_uv_0.WorldToScreenPoint(now[i]);
// A             next_proj[i] = cam_for_uv_0.WorldToScreenPoint(next[i]);
// A         }
// A 
// A         //10062024 UnityEngine.Vector3 vert = verts[i];
// A         //10062024 UnityEngine.Vector3 vec_proj = cam.WorldToScreenPoint(vert);
// A         //10062024 Vector3 uv_3d = new Vector3(vec_proj.x/Screen.width, vec_proj.y/Screen.height, 0f);
// A 
// A         return (now_proj, next_proj);
// A     }
// A 
// A     public Vector3[] transform_to_world(Vector3[] local, GameObject blade)
// A     {
// A         // info (paul): transform local verts to global, scaled, rotated etc. verts
// A 
// A         Vector3[] globals = new Vector3[local.Length];
// A 
// A         for (int i = 0; i < local.Length; i++)
// A         {
// A             Vector3 local_i = local[i];
// A             Vector3 global_i = blade.transform.TransformPoint(local_i);
// A             globals[i] = global_i;
// A         }
// A 
// A         return globals;
// A     }
// A 
// A     public (float[], float[], float[]) construct_distortions(GameObject current_blade, GameObject next_blade)
// A     {
// A         Mesh current_mesh = current_blade.GetComponent<MeshFilter>().mesh;
// A         Mesh next_mesh = next_blade.GetComponent<MeshFilter>().mesh;
// A 
// A         Vector3[] now_local = current_mesh.vertices;
// A         Vector3[] next_local = next_mesh.vertices;
// A 
// A         Vector3[] now = transform_to_world(now_local, current_blade);
// A         Vector3[] next = transform_to_world(next_local, next_blade);
// A 
// A         (Vector3[] now_proj, Vector3[] next_proj) = pos2uvs(now, next);
// A 
// A         float[] d_xs = new float[now_proj.Length];
// A         float[] d_ys = new float[now_proj.Length];
// A         float[] d_zs = new float[now_proj.Length];
// A 
// A         for (int i = 0; i < now_proj.Length; i++)
// A         {
// A             float d_x = next_proj[i].x - now_proj[i].x;
// A             float d_y = next_proj[i].y - now_proj[i].y;
// A             float d_z = next_proj[i].z - now_proj[i].z;
// A 
// A             if (d_x != 0f)
// A             {
// A                 ;
// A             }
// A             if (d_y != 0f)
// A             {
// A                 ;
// A             }
// A             if (d_z != 0f)
// A             {
// A                 ;
// A             }
// A 
// A             d_xs[i] = d_x;
// A             d_ys[i] = d_y;
// A             d_zs[i] = d_z;
// A         }
// A 
// A         float min_dx = d_xs.Min();
// A         float max_dx = d_xs.Max();
// A 
// A         return (d_xs, d_ys, d_zs);
// A 
// A     }
// A 
// A     public List<List<float>> read_dists(string filePath, int t_idx)
// A     {
// A         // info (paul): read out file into strings at path
// A         List<string[]> lines = read_lines_from(filePath, t_idx);
// A 
// A         // info (paul): convert strings to numbers:
// A         List<List<float>> mat_raw = strs2mat(lines);
// A 
// A         // info (paul): reconstruct the distance map in 3d space from disparities
// A         List<List<float>> mat = mat_raw;//07102024 mat_raw2dists(mat_raw);
// A 
// A         // info (paul): cut off floor
// A         mat = transpose_mat(mat);//24062024
// A         mat = cut_off(mat);
// A 
// A         return mat;
// A     }
// A 
// A     public List<string[]> read_lines_from(string filePath, int t_idx)
// A     {
// A         // info (paul): open the file and read the lines into strings
// A 
// A         //03072024 string full_path = filePath + "/displacements_u_" + t_idx + ".csv";
// A         //05072024 string full_path = filePath + "/" + get_experiment() + "/cam_0/uv/displacements_u_" + t_idx + ".csv";
// A         //26072024 string full_path = filePath + "/" + get_experiment() + "/stereo/displacements_u_" + t_idx + ".csv";
// A         string full_path = filePath + "/" + "exp_normal" + "/stereo/displacements_u_" + t_idx + ".csv";
// A 
// A         List<string[]> lines = new List<string[]>();
// A         try
// A         {
// A             using (StreamReader reader = new StreamReader(full_path))
// A             {
// A                 string line;
// A                 while ((line = reader.ReadLine()) != null)
// A                 {
// A                     string[] els = line.Split(',');
// A                     lines.Add(els);
// A                 }
// A             }
// A         }
// A         catch (Exception e)
// A         {
// A             Console.WriteLine(e.Message);
// A         }
// A         log("\n 1B");
// A         return lines;
// A     }
// A 
// A     public List<List<float>> mat_raw2dists(List<List<float>> mat)
// A     {
// A         // info (paul): converted the float value matrix from the file, 
// A         //          to the correct distance matrix (including trigonometrics etc.)
// A         float min_val = float.NaN;
// A 
// A         mat = transpose_mat(mat);
// A         mat = invert_sign_of_mat(mat);
// A         (mat, min_val) = set_zeros_to_min(mat);
// A         mat = disp2dist(mat, min_val);//16052024
// A         return mat;
// A     }
// A 
// A     public List<List<float>> strs2mat(List<string[]> lines)
// A     {
// A         // info (paul): convert lines of strings to matrix of floats
// A 
// A         List<List<float>> mat = new List<List<float>>();
// A         log("\n lines: " + lines.Count.ToString());
// A 
// A         for (int i_idx = 0; i_idx < lines.Count; i_idx++)
// A         {
// A             mat.Add(new List<float>());
// A             for (int j_idx = 0; j_idx < lines[0].Length; j_idx++)
// A             {
// A                 string line_el = lines[i_idx][j_idx];
// A                 float value = float.Parse(line_el, CultureInfo.InvariantCulture);
// A                 if (value != 0)
// A                 {
// A                     ;
// A                 }
// A                 mat[i_idx].Add(value);
// A             }
// A         }
// A 
// A         return mat;
// A     }
// A 
// A     public (List<List<float>>, float) set_zeros_to_min(List<List<float>> mat)
// A     {
// A         // info (paul): set the zero values to the min value of mat,
// A         //      in order to achieve, that the surrounding un-analyzed area
// A         //      is not above the lowest part of the analyzed area of the
// A         //      displacement/stereo optical flow
// A 
// A         // Produktionsmanagement im Nutzfahrzeugbau, im Mai
// A 
// A         (float min_val, float max_val) = find_max_2d(mat);
// A 
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             for (int j = 0; j < mat[0].Count; j++)
// A             {
// A                 bool is_zero = (mat[i][j] == 0f);
// A 
// A                 if (is_zero)
// A                 {
// A                     mat[i][j] = min_val;
// A                 }
// A             }
// A         }
// A 
// A         return (mat, min_val);
// A     }
// A 
// A     public List<List<float>> overwrite(List<List<float>> mat)
// A     {
// A         // info (paul): overwrite values for debugging reasons
// A 
// A         int length = mat[0].Count;
// A         float len_mid = (float)length / 2f;
// A 
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             for (int j = 0; j < length; j++)
// A             {
// A                 float new_val = -0.001f * (i - 100) * (i - 100) - 0.001f * (j - len_mid) * (j - len_mid);
// A                 float p_fac = 1.0f;
// A                 mat[i][j] = (1f - p_fac) * mat[i][j] + p_fac * (new_val);
// A             }
// A         }
// A 
// A         return mat;
// A     }
// A     public List<List<float>> disp2dist(List<List<float>> mat, float min_val)
// A     {
// A         //List<List<float>> dists = copy_mat(mat);
// A         //
// A         //for (int width_idx = 0; width_idx < mat.Count; width_idx++)
// A         //{
// A         //    //for (int j = 0; j < mat[0].Count; j++)
// A         //    for (int height_idx = 0; height_idx < mat[0].Count; height_idx++)
// A         //    {
// A         //        if (true)//(disp_ij > min_val + 0.1f)
// A         //        {
// A         //            dists[width_idx][height_idx] = disp2dist_ij_new(x_l,
// A         //                  x_r, span, width_idx);
// A         //        }
// A         //    }
// A         //}
// A 
// A         (List<List<float>> xi_val_mat, List<List<float>> xi_p_val_mat) = act_5(mat);//disp2dist_ij_new(mat, act_5);
// A         List<List<float>> dists = act_6(xi_val_mat, xi_p_val_mat);//disp2dist_ij_new(mat, act_6);
// A 
// A         return dists;
// A     }
// A     public List<List<float>> disp2dist_old(List<List<float>> mat, float min_val)
// A     {
// A         //mat = overwrite(mat);
// A 
// A         List<List<float>> dists = copy_mat(mat);
// A 
// A         for (int width_idx = 0; width_idx < mat.Count; width_idx++)
// A         {
// A             //for (int j = 0; j < mat[0].Count; j++)
// A             for (int height_idx = 0; height_idx < mat[0].Count; height_idx++)
// A             {
// A                 float disp_ij = mat[width_idx][height_idx];
// A                 float x_l = (float)width_idx;
// A                 float x_r = (float)(x_l) + disp_ij;
// A                 float span = (float)mat.Count;
// A 
// A                 if (disp_ij > min_val + 0.1f)
// A                 {
// A                     //dists[width_idx][height_idx] = disp2dist_ij(x_l,
// A                     //      x_r, span, width_idx);
// A                     dists[width_idx][height_idx] = disp2dist_ij(disp_ij);
// A                 }
// A             }
// A         }
// A 
// A         return dists;
// A     }
// A 
// A     public List<List<(int, int)>> zeros_like(List<List<(int, int)>> mat)
// A     {
// A         List<List<(int, int)>> empty = new List<List<(int, int)>>();
// A 
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             empty.Add(new List<(int, int)>());
// A             for (int j = 0; j < mat[0].Count; j++)
// A             {
// A                 empty[i].Add((0, 0));
// A             }
// A         }
// A         return empty;
// A     }
// A 
// A     public List<List<float>> zeros_like(List<List<(int, int)>> mat, string return_type = "floats")
// A     {
// A         List<List<float>> empty = new List<List<float>>();
// A 
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             empty.Add(new List<float>());
// A             for (int j = 0; j < mat[0].Count; j++)
// A             {
// A                 empty[i].Add(0f);
// A             }
// A         }
// A         return empty;
// A     }
// A     public List<List<float>> zeros_like(List<List<(float, float)>> mat, string return_type = "floats")
// A     {
// A         List<List<float>> empty = new List<List<float>>();
// A 
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             empty.Add(new List<float>());
// A             for (int j = 0; j < mat[0].Count; j++)
// A             {
// A                 empty[i].Add(0f);
// A             }
// A         }
// A         return empty;
// A     }
// A     public List<List<float>> copy_mat(List<List<float>> mat)//zeros_like
// A     {
// A         List<List<float>> empty = new List<List<float>>();
// A 
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             empty.Add(new List<float>());
// A             for (int j = 0; j < mat[0].Count; j++)
// A             {
// A                 empty[i].Add(mat[i][j]);
// A             }
// A         }
// A         return empty;
// A     }
// A     //public List<List<float>> zeros_of_size(int size_x, int size_y)
// A     //{
// A     //    List<List<float>> empty = new List<List<float>>();
// A     //
// A     //    for (int i = 0; i < size_x; i++)
// A     //    {
// A     //        empty.Add(new List<float>());
// A     //        for (int j = 0; j < size_y; j++)
// A     //        {
// A     //            empty[i].Add(0f);
// A     //        }
// A     //    }
// A     //    return empty;
// A     //}
// A     public List<float> zeros_of_size(int num)
// A     {
// A         List<float> empty = new List<float>();
// A 
// A         for (int i = 0; i < num; i++)
// A         {
// A             empty.Add(0f);
// A         }
// A         return empty;
// A     }
// A     public List<double> doubles_of_size(int num)
// A     {
// A         List<double> empty = new List<double>();
// A 
// A         for (int i = 0; i < num; i++)
// A         {
// A             empty.Add(0f);
// A         }
// A         return empty;
// A     }
// A     public List<int> ints_of_size(int num)
// A     {
// A         List<int> empty = new List<int>();
// A 
// A         for (int i = 0; i < num; i++)
// A         {
// A             empty.Add(0);
// A         }
// A         return empty;
// A     }
// A     public List<List<(float, float)>> zero_tuples_like(List<List<float>> mat)
// A     {
// A         List<List<(float, float)>> empty = new List<List<(float, float)>>();
// A 
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             empty.Add(new List<(float, float)>());
// A             for (int j = 0; j < mat[0].Count; j++)
// A             {
// A                 empty[i].Add((0f, 0f));
// A             }
// A         }
// A         return empty;
// A     }
// A 
// A     public float disp2dist_ij(float d_x)
// A     {
// A         // info (paul): disp is d_x
// A 
// A         // info (paul): Parameter TODO: replace by actual values
// A         float alpha = 22f / 180f; // info (paul): rotation angle between the cameras
// A         float gamma_span = 0.2f * Mathf.PI; // info (paul): span of screen
// A         float d_x_span = 20f;
// A         float D_p = 10f; // probably the reference distance; D_p kind of corresponds to gamma_pp
// A         float d_x_pp = 200f; // info (paul): basically this is a ref offset, which is the distance g between the camera
// A         float dist_forward = 3f; // info (paul): how much the cams are different in there parallel distance
// A 
// A         // info (paul): for non-small angles we would have
// A         //      float gamma = Mathf.Atan(d_x/d_x_span * Mathf.Tan(gamma_span));
// A         //      float gamma_pp = Mathf.Atan(d_x_pp/d_x_span * Mathf.Tan(gamma_span));
// A         //      float D_val = (D_p * Mathf.Tan(gamma_pp + alpha))/(Mathf.Tan(gamma));
// A 
// A         // info (paul): get angle from d_x pixel position on screen
// A         float gamma = d_x / d_x_span * gamma_span;
// A 
// A         // info (paul): adjust for dist_forward:
// A         //23042024 float tan_beta = Mathf.Atan(1/(1/Mathf.Tan(gamma) + 1/d_x_pp));
// A         //23042024 float beta = Mathf.Atan(tan_beta);
// A         float beta = gamma;
// A 
// A         // info (paul): adjust for camera rotation (quite simple)
// A         float epsilon = beta + alpha;
// A 
// A         // info (paul): calculate the distance with the parallaxe
// A         float gamma_pp = d_x_pp / d_x_span * gamma_span;
// A         float D_val = (D_p * (beta + epsilon)) / gamma_pp; // I just switched gamma and gamma_pp
// A 
// A         return D_val;
// A     }
// A 
// A     public (float, float) pix2xi(float x_l, float x_r, float span, float gamma_span)
// A     {
// A         float x_l_centric = (x_l - 0.5f * span) / (0.5f * span);
// A         float x_r_centric = (x_r - 0.5f * span) / (0.5f * span);
// A 
// A         float xi_val_tan = x_r_centric * Mathf.Tan(gamma_span);
// A         float xi_p_val_tan = x_l_centric * Mathf.Tan(gamma_span);
// A         float xi_val = Mathf.Atan(xi_val_tan);
// A         float xi_p_val = Mathf.Atan(xi_p_val_tan);
// A         return (xi_val, xi_p_val);
// A     }
// A     public float disp2dist_new(float x_l, float x_r, float span)
// A     {
// A         // info (paul): - xi is the angle from the one camera (further behind and on the right
// A         //          site, assuming the object is on the point side)
// A         //              - xi_p: angle from the other camera
// A         //              - alpha: rotation angle from the other camera
// A 
// A         // info (paul): nakajima-close parameters
// A         float gamma_span = 0.5f * cam_for_uv_0.fieldOfView * Mathf.PI / 180f; // info (paul): span of screen (I think half of it)
// A         float alpha = 2 * cam_angle * Mathf.PI / 180f; // info (paul): rotation angle between the cameras
// A         float g_val = (cam_for_uv_1.transform.position.x - cam_for_uv_0.transform.position.x); // info (paul): horizontal distance of the cameras
// A         float h_val = (cam_for_uv_1.transform.position.y - cam_for_uv_0.transform.position.y); // info (paul): depth distance of the cameras
// A 
// A         // info (paul): getting xi from d_x or so
// A         (float xi_val, float xi_p_val) = pix2xi(x_l, x_r, span, gamma_span);
// A 
// A         // info (paul): doing all the rest
// A         float dist_val = find_dist_val(g_val, h_val, alpha, xi_val, xi_p_val);
// A 
// A         return dist_val;//d_val;
// A     }
// A 
// A 
// A     public (float, float, float, float) el2vals(List<List<float>> mat, int width_idx, int height_idx)
// A     {
// A         float disp_ij = mat[width_idx][height_idx];
// A 
// A         float x_l = (float)width_idx;
// A         float x_r = (float)(x_l) + disp_ij;
// A         float span = (float)mat.Count;
// A         return (disp_ij, x_l, x_r, span);
// A     }
// A 
// A 
// A     public (float, float) act_5_ij(float disp_ij, float x_l, float x_r, float span)
// A     {
// A         // info (paul): nakajima-close parameters
// A         float gamma_span = 0.5f * cam_for_uv_0.fieldOfView * Mathf.PI / 180f; // info (paul): span of screen (I think half of it)
// A         float alpha = 2 * cam_angle * Mathf.PI / 180f; // info (paul): rotation angle between the cameras
// A         float g_val = (cam_for_uv_1.transform.position.x - cam_for_uv_0.transform.position.x); // info (paul): horizontal distance of the cameras
// A         float h_val = (cam_for_uv_1.transform.position.y - cam_for_uv_0.transform.position.y); // info (paul): depth distance of the cameras
// A 
// A         (float xi_val, float xi_p_val) = pix2xi(x_l, x_r, span, gamma_span);
// A         return (xi_val, xi_p_val);
// A     }
// A 
// A     public (List<List<float>>, List<List<float>>) act_5(List<List<float>> mat)
// A     {
// A         List<List<float>> xi_val_mat = copy_mat(mat);
// A         List<List<float>> xi_p_val_mat = copy_mat(mat);
// A 
// A         for (int width_idx = 0; width_idx < mat.Count; width_idx++)
// A         {
// A             for (int height_idx = 0; height_idx < mat[width_idx].Count; height_idx++)
// A             {
// A                 (float disp_ij, float x_l, float x_r, float span) = el2vals(mat, width_idx, height_idx);
// A                 (float xi_val, float xi_p_val) = act_5_ij(disp_ij, x_l, x_r, span);
// A                 (xi_val_mat[width_idx][height_idx], xi_p_val_mat[width_idx][height_idx]) = (xi_val, xi_p_val);
// A             }
// A         }
// A         return (xi_val_mat, xi_p_val_mat);
// A     }
// A 
// A     public List<List<float>> act_6(
// A         List<List<float>> xi_val_mat, List<List<float>> xi_p_val_mat)
// A     {
// A         List<List<float>> dist_mat = copy_mat(xi_val_mat);
// A 
// A         for (int i = 0; i < xi_val_mat.Count; i++)
// A         {
// A             for (int j = 0; j < xi_val_mat[i].Count; j++)
// A             {
// A                 // info (paul): nakajima-close parameters
// A                 float gamma_span = 0.5f * cam_for_uv_0.fieldOfView * Mathf.PI / 180f; // info (paul): span of screen (I think half of it)
// A                 float alpha = 2 * cam_angle * Mathf.PI / 180f; // info (paul): rotation angle between the cameras
// A                 float g_val = (cam_for_uv_1.transform.position.x - cam_for_uv_0.transform.position.x); // info (paul): horizontal distance of the cameras
// A                 float h_val = (cam_for_uv_1.transform.position.y - cam_for_uv_0.transform.position.y); // info (paul): depth distance of the cameras
// A 
// A                 float dist_val = find_dist_val(g_val, h_val, alpha, xi_val_mat[i][j], xi_p_val_mat[i][j]);
// A                 dist_mat[i][j] = dist_val;
// A             }
// A         }
// A         return (dist_mat);
// A     }
// A 
// A     public List<List<float>> disp2dist_ij_new(List<List<float>> mat, Func<float, float, float, float, float> act)
// A     {
// A         for (int width_idx = 0; width_idx < mat.Count; width_idx++)
// A         {
// A             for (int height_idx = 0; height_idx < mat[width_idx].Count; height_idx++)
// A             {
// A                 (float disp_ij, float x_l, float x_r, float span) = el2vals(mat, width_idx, height_idx);
// A                 mat[width_idx][height_idx] = act(disp_ij, x_l, x_r, span);
// A             }
// A         }
// A         return mat;
// A 
// A         //A // info (paul): - xi is the angle from the one camera (further behind and on the right
// A         //A //          site, assuming the object is on the point side)
// A         //A //              - xi_p: angle from the other camera
// A         //A //              - alpha: rotation angle from the other camera
// A         //A 
// A         //A // info (paul): nakajima-close parameters
// A         //A float gamma_span = 0.5f * cam_for_uv_0.fieldOfView * Mathf.PI / 180f; // info (paul): span of screen (I think half of it)
// A         //A float alpha = 2 * cam_angle * Mathf.PI / 180f; // info (paul): rotation angle between the cameras
// A         //A float g_val = (cam_for_uv_1.transform.position.x - cam_for_uv_0.transform.position.x); // info (paul): horizontal distance of the cameras
// A         //A float h_val = (cam_for_uv_1.transform.position.y - cam_for_uv_0.transform.position.y); // info (paul): depth distance of the cameras
// A         //A 
// A         //A // info (paul): getting xi from d_x or so
// A         //A (float xi_val, float xi_p_val) = pix2xi(x_l, x_r, span, gamma_span);
// A         //A 
// A         //A // info (paul): doing all the rest
// A         //A float dist_val = find_dist_val(g_val, h_val, alpha, xi_val, xi_p_val);
// A         //A 
// A         //A return dist_val;//d_val;
// A     }
// A 
// A     public float find_dist_val(float g_val, float h_val, float alpha, float xi_val, float xi_p_val)
// A     {
// A         float c_val = Mathf.Sqrt(g_val * g_val + h_val * h_val);
// A 
// A         float alpha_1 = Mathf.Atan(h_val / g_val);
// A         float alpha_2 = Mathf.Atan(g_val / h_val);//c_val);
// A         float alpha_3 = Mathf.PI - alpha_2 - alpha - xi_p_val;//yep
// A         float alpha_4 = Mathf.PI / 2f - alpha_1;//yep
// A         float alpha_5 = Mathf.PI - alpha_4 - alpha_3 - xi_val;//yep//02052024
// A         float alpha_6 = Mathf.PI / 2f - xi_val;//yep
// A 
// A         float d_val_p = c_val * Mathf.Sin(alpha_3) / Mathf.Sin(alpha_5);
// A         float d_val = d_val_p * Mathf.Sin(alpha_6);
// A 
// A         float dist_val = d_val;//xi_val - xi_p_val;//d_val;// - alpha_5;//Mathf.Sin(alpha_3) / Mathf.Sin(alpha_5);
// A         return dist_val;
// A     }
// A 
// A     public List<List<float>> transpose_mat(List<List<float>> mat)
// A     {
// A         //log("\n 1CA");
// A         List<List<float>> mat_t = new List<List<float>>();
// A         //log("\n 1CB");
// A         //log("\n 1CBmat_cnt: " + mat.Count);
// A         //try
// A         //{
// A         //    log("\n 1CBmat[0]_cnt: " + (mat[0].Count).ToString());
// A         //}
// A         //catch (Exception e)
// A         //{
// A         //    log("\n error: " + e.ToString());
// A         //}
// A 
// A         int mat_cnt = -1;
// A         try
// A         {
// A             mat_cnt = mat[0].Count;
// A         }
// A         catch
// A         {
// A             mat_cnt = mat[0].Count;
// A         }
// A         for (int i = 0; i < mat_cnt; i++)
// A         {
// A             if (i == 0)
// A             { log("\n 1CB" + i.ToString() + "A"); }
// A             mat_t.Add(new List<float>());
// A             if (i == 0)
// A             { log("\n 1CB" + i.ToString() + "B"); }
// A             for (int j = 0; j < mat.Count; j++)
// A             {
// A                 if (i == 0)
// A                 { log("\n 1CB" + i.ToString() + j.ToString() + "C"); }
// A                 mat_t[i].Add(mat[j][i]);
// A                 if (i == 0)
// A                 { log("\n 1CB" + i.ToString() + j.ToString() + "D"); }
// A             }
// A         }
// A         log("\n 1CC");
// A         return mat_t;
// A     }
// A 
// A     public List<List<float>> invert_sign_of_mat(List<List<float>> mat)
// A     {
// A         for (int i_idx = 0; i_idx < mat.Count; i_idx++)
// A         {
// A             for (int j_idx = 0; j_idx < mat[0].Count; j_idx++)
// A             {
// A                 mat[i_idx][j_idx] = -mat[i_idx][j_idx];
// A             }
// A         }
// A         return mat;
// A     }
// A 
// A     public (GameObject, List<List<(float, float)>>, int, int) make_platine_plane(List<List<float>> heights,
// A         List<List<(float, float)>> points, string path_time_flow_u, string path_time_flow_v,
// A         int im_cnt = -1, bool force_flat = false, int t_idx = -1, float scale_factor = -1f, bool with_save = false)
// A     {
// A         Mesh mesh;
// A         (mesh, heights) = make_mesh(heights, force_flat: force_flat, scale_factor: scale_factor);
// A         (GameObject crossing_obj, int res_x, int res_y) = (null, -1, -1);
// A         (crossing_obj, points, res_x, res_y) = make_plane_with_mesh(mesh, points, path_time_flow_u, path_time_flow_v,
// A             im_cnt: im_cnt, t_idx: t_idx, heights_chosen: heights, with_save: with_save);
// A 
// A         return (crossing_obj, points, res_x, res_y);
// A     }
// A 
// A 
// A 
// A     public (GameObject, List<List<(float, float)>>, int, int) make_plane_with_mesh(Mesh mesh,
// A         List<List<(float, float)>> points, string path_time_flow_u, string path_time_flow_v,
// A         int im_cnt = -1, int t_idx = -1, List<List<float>> heights_chosen = null, bool with_save = false)
// A     {
// A         GameObject surface_obj = setup_surface_obj(mesh);
// A         Material province_mat;
// A         (province_mat, points, res_x, res_y) = manage_material(
// A             points, path_time_flow_u, path_time_flow_v, im_cnt: im_cnt,
// A             t_idx: t_idx, heights_chosen: heights_chosen, with_save: with_save);
// A 
// A         // info (paul): assign material
// A         surface_obj.GetComponent<MeshRenderer>().material = province_mat;
// A 
// A         return (surface_obj, points, res_x, res_y);
// A     }
// A 
// A     public GameObject setup_surface_obj(Mesh mesh, string obj_name = "platine_plane")
// A     {
// A         // info (paul): setup the object, to which the mesh
// A         //      will be applied, which is supposed to reconstruct 
// A         //      the object from the experiment
// A 
// A         GameObject surface_obj = new GameObject();
// A         surface_obj.name = obj_name;
// A         surface_obj.transform.parent = null;
// A         surface_obj.AddComponent<MeshFilter>();
// A         surface_obj.GetComponent<MeshFilter>().mesh = mesh;
// A         surface_obj.AddComponent<MeshRenderer>();
// A         surface_obj.transform.position = new UnityEngine.Vector3(0.0f, 0f,
// A                 0.0f);
// A 
// A         if (surface_obj.GetComponent<MeshRenderer>() == null)
// A         {
// A             surface_obj.AddComponent<MeshRenderer>();
// A         }
// A         return surface_obj;
// A     }
// A 
// A     public List<List<float>> init_test_mat(int len_x, int len_y)
// A     {
// A         List<List<float>> mat_test = zeros_of_size(len_x, len_y);
// A 
// A         for (int i = 0; i < len_x; i++)
// A         {
// A             for (int j = 0; j < len_y; j++)
// A             {
// A                 //mat_test[i][j] = ((float)(i + j)) / ((float)(len_x + len_y));// j % 2;//((float)(i + j))/((float)(len_x+len_y));
// A                 mat_test[i][j] = j % 2;
// A             }
// A         }
// A 
// A         return mat_test;
// A     }
// A 
// A     string speckle_file = "speckle_5.000";//25092024 "speckle_5";//11072024 "speckle_pattern";
// A     public void set_speckle_file(string input)
// A     {
// A         this.speckle_file = input;
// A     }
// A     public string get_speckle_file()
// A     {
// A         return this.speckle_file;
// A     }
// A 
// A     public void apply_speckles(GameObject obj, string file_name = "speckle_pattern")
// A     {
// A         //23092024 string mat_file = "Targets/fbx_files/Materials/" + remove_dots(get_speckle_file());
// A         string mat_file = "Targets/fbx_files/Materials/" + get_speckle_file();
// A         Material speckle_mat = (Material)(Resources.Load(mat_file));
// A 
// A         obj.GetComponent<Renderer>().material = speckle_mat;
// A     }
// A 
// A 
// A     public (List<List<float>>, List<List<float>>) load_heights_ref()
// A     {
// A         int res_x = -1;
// A         int res_y = -1;
// A 
// A         //05072024 string file_path_u = "C:/Users/go73jem/AppData/LocalLow/DefaultCompany/unter2_Windows_native/blade_0_cam_0_depth.png";
// A         string file_path_u = "C:/Users/go73jem/Desktop/DIC_package/speckle/cam_0/uv/1__depth.png";
// A         byte[] im_bytes_u = System.IO.File.ReadAllBytes(file_path_u);
// A         if ((t_idx == 0) || true)
// A         {
// A             (res_x, res_y) = bytes2res(im_bytes_u);
// A         }
// A         Texture2D tex_albedo_u = new Texture2D(res_x, res_y);
// A         tex_albedo_u.LoadImage(im_bytes_u);
// A         List<List<float>> mat_u = tex2mat(tex_albedo_u);
// A 
// A         // info (paul): load camera values
// A         float depth_min = load_float_for_blade(cam_idx: 0, blade_idx: 0, label: "_depth_min", with_uv_mode: false);
// A         float depth_max = load_float_for_blade(cam_idx: 0, blade_idx: 0, label: "_depth_max", with_uv_mode: false);
// A 
// A         mat_u = load_floats_list_2_for_blade(cam_idx: 0, blade_idx: 0, label: "_depth_mat", with_uv_mode: false);
// A         mat_u = unnorm_mat(mat_u, depth_min, depth_max);
// A         mat_u = transpose_mat(mat_u);
// A 
// A         List<List<float>> mat_cut = cut_off(mat_u);
// A 
// A         return (mat_cut, mat_u);
// A     }
// A 
// A     public (Material, List<List<(float, float)>>, int, int) manage_material(List<List<(float, float)>> points,
// A         string dir_time_flow_u, string dir_time_flow_v, int im_cnt = -1, int t_idx = -1,
// A         List<List<float>> heights_chosen = null, bool with_save = false)
// A     {
// A         string province_mat_name = "Targets/mat_1";
// A         Material province_mat = (Material)(Resources.Load(province_mat_name));
// A 
// A         // info (paul): load texture from image, if only one image
// A         string path_time_flow_u = dir_time_flow_u + remove_dots(get_experiment()) + "/time_flow_u/";
// A         string path_time_flow_v = dir_time_flow_v + remove_dots(get_experiment()) + "/time_flow_v/";//05072024
// A         Texture2D tex_albedo;
// A 
// A         bool is_dir = path_time_flow_u.EndsWith("/");
// A         if (!is_dir)
// A         {
// A             tex_albedo = new Texture2D(res_x, res_y);
// A             byte[] im_bytes = File.ReadAllBytes(path_time_flow_u);
// A             tex_albedo.LoadImage(im_bytes);
// A         }
// A 
// A         // info (paul): if multiple images loaded, load them all
// A         Texture2D tex;
// A         (tex, points, res_x, res_y) = load_and_construct_flow(points,
// A             path_time_flow_u, path_time_flow_v, res_x,
// A             res_y, im_cnt: im_cnt, t_idx: t_idx);
// A 
// A         (tex, tex_albedo) = manage_strain(tex, heights_chosen);
// A 
// A         // info (paul): overwrite with chosen height map:
// A         if (get_paint_with() == "heights")
// A         {
// A             //07102024 (float loss_heights_min, float loss_heights_max) = find_min_max(heights_chosen, with_padding: true);
// A             //07102024 update_scale_label(loss_heights_min, loss_heights_max, loss_heights_min, loss_heights_max);
// A             //AFA
// A 
// A             //manage_heights_loss(heights, heights_ref, scale_factor);
// A 
// A             List<List<float>> heights_l = norm_mat(heights_chosen);
// A             (float loss_heights_min_1, float loss_heights_max_1) = find_min_max(heights_l, with_padding: true);
// A             tex = mat2tex(heights_l, with_switch_dims: true);
// A         }
// A 
// A         province_mat.SetTexture("_MainTex", tex);
// A         province_mat.SetTexture("_EmissionMap", tex);
// A         province_mat.EnableKeyword("_EMISSION");
// A         province_mat.SetTexture("_EmissionMap", tex);
// A         province_mat.SetColor("_EmissiveColor", Color.green);
// A         province_mat.SetColor("_EmissionColor ", Color.green);
// A 
// A         //_EmissiveColor
// A         // info (paul): We say cam_idx = 0, since cam_idx makes no sense anyway
// A         if (with_save)
// A         {
// A             //24062024 string label_l = "_" + paint_with.ToString() + "_" + plot_mode.ToString() + "_" + heights_mode.ToString();
// A             //string label_l = "_" + paint_with.ToString() + "_" + plot_mode.ToString() + "_" + heights_mode.ToString();
// A 
// A             string active_mode = find_active_mode();
// A             string label_l = "_" + paint_with.ToString() + "_" + active_mode.ToString();
// A             save_png(tex, cam_idx: 0, blade_idx: t_idx, label: label_l);
// A         }
// A         //ABA
// A 
// A         return (province_mat, points, res_x, res_y);
// A     }
// A 
// A     public string find_active_mode()
// A     {
// A         string active_mode = null;
// A 
// A         if (paint_with == "uv")
// A         {
// A             active_mode = plot_mode;
// A         }
// A         if (paint_with == "heights")
// A         {
// A             active_mode = heights_mode;
// A         }
// A 
// A         return active_mode;
// A     }
// A 
// A     public (Texture2D, Texture2D) manage_strain(Texture2D tex, List<List<float>> heights_chosen)
// A     {
// A         Texture2D tex_albedo = null;
// A         // info (paul): make some dummy matrix for debugging
// A         //List<List<float>> mat_test = init_test_mat(res_x, res_y);
// A         //Texture2D tex_test = mat2tex(mat_test, with_switch_dims: false);
// A 
// A         if (this.strain_mode == "derivative_1")//29052024 (this.strain_mode == "derivative_1")
// A         {
// A             tex_albedo = calc_strain(tex, heights_chosen); //16052024 tex_albedo
// A 
// A             // info (paul): the thing we want to debug or so: 
// A             List<List<float>> mat = tex2mat(tex_albedo, with_switch_dims: false);//29052024
// A                                                                                  //mat = randomize_mat(mat);
// A             Texture2D tex_out = mat2tex(mat, with_switch_dims: true);//29052024
// A 
// A             //tex = tex_out;
// A 
// A             tex = tex_out;//31052024 tex_albedo;
// A         }
// A         else
// A         {
// A             //12062024 List<List<float>> mat = tex2mat(tex, with_switch_dims: false);//29052024
// A             //mat = randomize_mat(mat);
// A             //12062024 tex = mat2tex(mat);//29052024
// A         }
// A 
// A         //tex = overwrite_tex(tex);// for debugging
// A         return (tex, tex_albedo);
// A     }
// A 
// A     public Texture2D overwrite_tex(Texture2D tex)
// A     {
// A         Color[] cols = new Color[tex.width * tex.height];
// A         for (int i = 0; i < tex.width; i++)
// A         {
// A             for (int j = 0; j < tex.height; j++)
// A             {
// A                 float col_x = (float)(i % 2);
// A                 float col_y = 0f;// (float)(j%tex.height);
// A                 cols[i * tex.height + j] = new Color(0f, 0.7f, 0f, 1f);
// A             }
// A         }
// A         tex.SetPixels(cols);
// A         tex.Apply();
// A 
// A         return tex;
// A     }
// A 
// A     public (Texture2D, List<List<(float, float)>>, int, int) load_and_construct_flow(List<List<(float, float)>> points,
// A         string path_time_flow_u, string path_time_flow_v, int res_x, int res_y,
// A         int im_cnt = -1, int t_idx = -1)
// A     {
// A         Texture2D tex = null;// = new Texture2D(res_x, res_y);
// A 
// A         bool is_dir = path_time_flow_u.EndsWith("/");
// A         if (is_dir)
// A         {
// A             DirectoryInfo dir_u = new DirectoryInfo(path_time_flow_u);
// A             FileInfo[] dir_info_u = dir_u.GetFiles("*.*");
// A 
// A             DirectoryInfo dir_v = new DirectoryInfo(path_time_flow_v);
// A             FileInfo[] dir_info_v = dir_v.GetFiles("*.*");
// A 
// A             if (with_match_tex) // info (paul): This is supposed to be always true, even if you don't do the long-range matching
// A             {
// A                 (tex, points, res_x, res_y) = find_total_match_tex(points, dir_info_u, dir_info_v, im_cnt: im_cnt, t_idx: t_idx);
// A             }
// A         }
// A 
// A         return (tex, points, res_x, res_y);
// A     }
// A 
// A     public Dictionary<string, Dictionary<int, FileInfo>> select_flow_files(FileInfo[] dir_info_u, FileInfo[] dir_info_v)
// A     {
// A         // info (paul): We assume, that the datafile is 
// A         //      has a name like "time_flow_u_172.png"
// A 
// A         List<FileInfo> flow_files = new List<FileInfo>();
// A         Dictionary<string, Dictionary<int, FileInfo>> flow_dic = new Dictionary<string, Dictionary<int, FileInfo>>();
// A         flow_dic.Add("u", new Dictionary<int, FileInfo>());
// A         flow_dic.Add("v", new Dictionary<int, FileInfo>());
// A 
// A         for (int i = 0; i < dir_info_u.Length; i++)
// A         {
// A             FileInfo info = dir_info_u[i];
// A             bool is_flow_u = info.Name.StartsWith("time_flow_u_");
// A 
// A             if (is_flow_u)
// A             {
// A                 string time_str = Regex.Match(info.Name, @"\d+").Value;
// A                 int time_idx = int.Parse(time_str);
// A 
// A                 //flow_files.Add(info);
// A                 try
// A                 {
// A                     flow_dic["u"].Add(time_idx, info);
// A                 }
// A                 catch
// A                 {
// A                     flow_dic["u"].Add(time_idx, info);
// A                 }
// A             }
// A         }
// A         for (int i = 0; i < dir_info_v.Length; i++)
// A         {
// A             FileInfo info = dir_info_v[i];
// A             bool is_flow_v = info.Name.StartsWith("time_flow_v_");
// A 
// A             if (is_flow_v)
// A             {
// A                 string time_str = Regex.Match(info.Name, @"\d+").Value;
// A                 int time_idx = int.Parse(time_str);
// A                 try
// A                 {
// A                     flow_dic["v"].Add(time_idx, info);
// A                 }
// A                 catch
// A                 {
// A                     flow_dic["v"].Add(time_idx, info);
// A                 }
// A             }
// A         }
// A 
// A         return flow_dic;
// A     }
// A 
// A     public (List<List<float>>, List<List<float>>) points2flow(List<List<(float, float)>> points)
// A     {
// A         List<List<float>> flow_x = new List<List<float>>();
// A         List<List<float>> flow_y = new List<List<float>>();
// A 
// A         flow_x = zeros_like(points, return_type: "float");
// A         flow_y = zeros_like(points, return_type: "float");
// A 
// A         for (int i = 0; i < points.Count; i++)
// A         {
// A             for (int j = 0; j < points[i].Count; j++)
// A             {
// A                 flow_x[i][j] = points[i][j].Item1;
// A                 flow_y[i][j] = points[i][j].Item2;
// A             }
// A         }
// A 
// A         return (flow_x, flow_y);
// A     }
// A 
// A     public List<List<(float, float)>> subtract_mean(List<List<(float, float)>> points_pos)
// A     {
// A         // info (paul): getting mean
// A         float sum_1 = 0f;
// A         float sum_2 = 0f;
// A 
// A         int len_x = points_pos.Count;
// A         int len_y = points_pos[0].Count;
// A         for (int i = 0; i < len_x; i++)
// A         {
// A             for (int j = 0; j < len_y; j++)
// A             {
// A                 sum_1 += points_pos[i][j].Item1;
// A                 sum_2 += points_pos[i][j].Item2;
// A             }
// A         }
// A 
// A         float cnt_float = (float)(len_x * len_y);
// A         float mean_1 = sum_1 / cnt_float;
// A         float mean_2 = sum_2 / cnt_float;
// A 
// A         // info (paul): subtracting mean
// A         for (int i = 0; i < len_x; i++)
// A         {
// A             for (int j = 0; j < len_y; j++)
// A             {
// A                 float item1 = points_pos[i][j].Item1;
// A                 float item2 = points_pos[i][j].Item2;
// A 
// A                 points_pos[i][j] = (item1 - mean_1, item2 - mean_2);
// A             }
// A         }
// A         return points_pos;
// A     }
// A 
// A     public bool is_useful(float value)
// A     {
// A         bool is_useful = (value != 0f && !float.IsNaN(value) && !float.IsInfinity(value));
// A         return is_useful;
// A     }
// A     public bool is_meaningful(float value)
// A     {
// A         bool meaningful = (!float.IsNaN(value) && !float.IsInfinity(value));
// A         return meaningful;
// A     }
// A     public List<List<(float, float)>> subtract_grid_pos(List<List<(float, float)>> points_pos)
// A     {
// A         for (int i = 0; i < points_pos.Count; i++)
// A         {
// A             for (int j = 0; j < points_pos[i].Count; j++)
// A             {
// A                 float val_x = points_pos[i][j].Item1;
// A                 float val_y = points_pos[i][j].Item2;
// A                 float adjusted_x = val_x - (float)i;
// A                 float adjusted_y = val_y - (float)j;
// A                 points_pos[i][j] = (adjusted_x, adjusted_y);
// A                 if (i == 10 && j == 350)
// A                 {
// A                     ;
// A                 }
// A                 if (is_useful(adjusted_x))
// A                 {
// A                     ;
// A                 }
// A                 if (is_useful(adjusted_y))
// A                 {
// A                     ;
// A                 }
// A             }
// A         }
// A 
// A         return points_pos;
// A     }
// A 
// A     public (Texture2D, List<List<(float, float)>>, int, int) find_total_match_tex(List<List<(float, float)>> points, FileInfo[] dir_info_u,
// A         FileInfo[] dir_info_v, int im_cnt = -1, int t_idx = -1)
// A     {
// A         // info (paul): get flow files
// A         Dictionary<string, Dictionary<int, FileInfo>> flow_files = select_flow_files(dir_info_u, dir_info_v);
// A 
// A         // info (paul): load files
// A         if (flow_files != null)
// A         {
// A             im_cnt = flow_files["u"].Count;
// A         }
// A 
// A         (List<List<List<float>>> flow_mats_u, List<List<List<float>>> flow_mats_v, List<Texture2D> texs_albedo_u,
// A             List<Texture2D> texs_albedo_v, int res_x, int res_y) = (null, null, null, null, -1, -1);
// A 
// A         (flow_mats_u, flow_mats_v, texs_albedo_u,
// A             texs_albedo_v, res_x, res_y) = find_flow_mats(
// A             flow_files, im_cnt: im_cnt);// info (paul): somewhat performance heavy
// A 
// A         // info (paul): scale flow mats for debugging reasons
// A         (flow_mats_u, flow_mats_v) = scale_flows(flow_mats_u, flow_mats_v, scale_fac: this.flow_scale_factor);
// A 
// A         (Texture2D tex, List<List<(float, float)>> points_now) = manage_match_to_start(flow_mats_u,
// A             flow_mats_v, points, t_idx, texs_albedo_u, texs_albedo_v);
// A 
// A         //tex = texs_albedo_u[0];
// A 
// A         return (tex, points_now, res_x, res_y);
// A     }
// A 
// A     public (List<List<List<float>>>, List<List<List<float>>>) scale_flows(List<List<List<float>>> flow_mats_u,
// A         List<List<List<float>>> flow_mats_v, float scale_fac)
// A     {
// A         int len_t = flow_mats_u.Count;
// A         int len_x = flow_mats_u[0].Count;
// A         int len_y = flow_mats_u[0][0].Count;
// A 
// A         for (int t_idx = 0; t_idx < len_t; t_idx++)
// A         {
// A             for (int i = 0; i < len_x; i++)
// A             {
// A                 for (int j = 0; j < len_y; j++)
// A                 {
// A                     flow_mats_u[t_idx][i][j] = flow_mats_u[t_idx][i][j] * scale_fac;
// A                     flow_mats_v[t_idx][i][j] = flow_mats_v[t_idx][i][j] * scale_fac;
// A                 }
// A             }
// A         }
// A         return (flow_mats_u, flow_mats_v);
// A     }
// A 
// A     public (Texture2D, List<List<(float, float)>>) manage_match_to_start(List<List<List<float>>> flow_mats_u,
// A         List<List<List<float>>> flow_mats_v, List<List<(float, float)>> points, int t_idx,
// A         List<Texture2D> texs_albedo_u, List<Texture2D> texs_albedo_v)
// A     {
// A         // info (paul): find uv flow from start
// A         (List<List<float>> flow_u, List<List<float>> flow_v) = find_accum_flow(flow_mats_u, flow_mats_v, points, t_idx);
// A         (List<List<float>> stream_u, List<List<float>> stream_v, float coverage) = manage_flow_or_loss(flow_u, flow_v, t_idx);
// A 
// A         // info (paul): scale label
// A         //(float stream_u_min, float stream_u_max) = find_min_max(stream_u);
// A         //(float stream_v_min, float stream_v_max) = find_min_max(stream_v);
// A         (float stream_u_mean, float dev_u) = find_mean_in_all(stream_u, span: 20, coverage: coverage);//find_mean_in_span(stream_u, span: 20, j_off: 50);//find_mean_in_all(stream_u, span: 20);//find_mean_in_span(stream_u, span: 20, j_off: 50);//find_mean_in_span(stream_u, span: 20);
// A         (float stream_v_mean, float dev_v) = find_mean_in_all(stream_v, span: 20, coverage: coverage);//find_mean_in_span(stream_v, span: 20, j_off: 50);//find_mean_in_all(stream_v, span: 20);//find_mean_in_span(stream_v, span: 20, j_off: 50);//find_mean_in_span(stream_v, span: 20);
// A         if (get_paint_with() == "uv")
// A         {
// A             update_scale_label(stream_u_mean, dev_u, stream_v_mean, dev_v);
// A         }
// A         //17072024 update_scale_label(stream_u_min, stream_u_max, stream_v_min, stream_v_max);
// A 
// A         save_means(stream_u_mean, stream_v_mean,
// A             blade_idx: get_blade_idx());
// A 
// A         // info (paul): choose between u, v and z coord/comp
// A         List<List<float>> flow_mats_chosen = choose_coord(stream_u, stream_v, u_v_mode);
// A 
// A         // info (paul): convert to cols, apply to tex; also for debugging overwriting is also possible
// A         Texture2D tex = to_tex_if(flow_mats_chosen, texs_albedo_u, texs_albedo_v);
// A         return (tex, points);
// A     }
// A     public void save_means(float u_mean, float v_mean,
// A         int blade_idx)
// A     {
// A         string u_path = construct_blade_path(cam_idx: 0,
// A             blade_idx, label: get_plot_mode() + "_u", type: "float",
// A             with_uv_mode: false);
// A         string v_path = construct_blade_path(cam_idx: 0,
// A             blade_idx, label: get_plot_mode() + "_v", type: "float",
// A             with_uv_mode: false);
// A 
// A         save_float(u_mean, full_path: u_path);
// A         save_float(v_mean, full_path: v_path);
// A     }
// A     public (List<List<float>>, List<List<float>>) find_accum_flow(List<List<List<float>>> flow_mats_u,
// A         List<List<List<float>>> flow_mats_v, List<List<(float, float)>> points, int t_idx)
// A     {
// A         // info (paul): match points
// A         List<List<(float, float)>> points_now = match_all_to_start(flow_mats_u, flow_mats_v, points,
// A             t_min: t_idx, t_max: t_idx + get_match_steps());// info (paul): very performance heavy
// A 
// A         // info (paul): subtract mean, normalize etc. partly for nice visualization
// A         List<List<(float, float)>> points_normed = treat_nice(points_now);
// A 
// A         (List<List<float>> stream_u, List<List<float>> stream_v) = points2flow(points_normed);//12062024 points_normed);
// A         return (stream_u, stream_v);
// A     }
// A 
// A     public List<List<(float, float)>> treat_nice(List<List<(float, float)>> points_now)
// A     {
// A         List<List<(float, float)>> points_rel = subtract_grid_pos(points_now);
// A 
// A         bool with_visualize_nice = false;//true
// A         if (with_visualize_nice)
// A         {
// A             List<List<(float, float)>> points_fluc = subtract_mean(points_rel);
// A 
// A             // info (paul): norm
// A             List<List<(float, float)>> points_normed = norm_points(points_fluc);//14062024 points_fluc);
// A         }
// A         return points_rel;//points_normed;
// A     }
// A 
// A     public (List<List<float>>, List<List<float>>, float) manage_flow_or_loss(
// A         List<List<float>> flow_u, List<List<float>> flow_v, int t_idx)
// A     {
// A         // info (paul): wording: "flow" denotes the disparity components u/v of the optical flow, 
// A         //      while "stream" might also be the loss
// A 
// A         // info (paul): we assume, that a certain t_idx corresponds to a certain blade
// A         (float[] d_xs, float[] d_ys, float[] d_zs) = load_distortion_ground_truth(blade_idx: t_idx);
// A         //(float[] screen_x, float[] screen_y) = load_screen_poss();
// A         int[,] tri_idx = load_tris(blade_idx: t_idx);
// A 
// A         (List<List<float>> stream_u, List<List<float>> stream_v, float coverage) = choose_flow_or_loss(
// A             flow_u, flow_v, tri_idx, d_xs, d_ys, d_zs);
// A 
// A         return (stream_u, stream_v, coverage);
// A     }
// A 
// A     public (List<List<float>>, float) choose_heights_or_loss(List<List<float>> heights, List<List<float>> heights_ref, float scale_factor)
// A     {
// A         List<List<float>> stream_u = copy_mat(heights);
// A         //A List<List<float>> stream_v = copy_mat(flow_v);
// A 
// A         int i_center = (int)Mathf.Floor((float)(0.5f * heights.Count)); //07102024 256;
// A         int j_center = (int)Mathf.Floor((float)(0.5f * heights.Count)); //07102024 256;
// A         int i_span = i_center - 6;//07102024 250;//20;
// A         int j_span = i_center - 6;//07102024 250;//20;
// A 
// A         int i_off = 0;
// A         int j_off = 0;
// A 
// A         int i_min = i_center + i_off - i_span;
// A         int i_max = i_center + i_off + i_span;
// A         int j_min = j_center + j_off - j_span;
// A         int j_max = j_center + j_off + j_span;
// A 
// A         (float u_min, float u_max) = find_min_max(stream_u, with_padding: true);
// A         //A (float v_min, float v_max) = find_min_max(stream_v, with_padding: true);
// A 
// A         int counter = 0;
// A         int nonzero_counter = 0;
// A 
// A         for (int i = i_min; i < i_max; i++)//17072024 stream_u[0].Count; i++)
// A         {
// A             for (int j = j_min; j < j_max; j++)//17072024 stream_u.Count; j++)
// A             {
// A                 (stream_u[i][j], _) = heights_or_loss_ij(stream_u, heights_ref, i, j, u_min, u_max, scale_factor, threshold: 10f);
// A             }
// A         }
// A 
// A         float coverage = 1f;
// A         return (stream_u, coverage);
// A     }
// A 
// A     public (List<List<float>>, List<List<float>>, float) choose_flow_or_loss(List<List<float>> flow_u, List<List<float>> flow_v,
// A         int[,] tri_idx, float[] d_xs, float[] d_ys, float[] d_zs)
// A     {
// A         List<List<float>> stream_u = copy_mat(flow_u);
// A         List<List<float>> stream_v = copy_mat(flow_v);
// A 
// A         int i_center = 256;
// A         int j_center = 256;
// A         int i_span = 250;//20;
// A         int j_span = 250;//20;
// A 
// A         int i_off = 0;
// A         int j_off = 0;
// A 
// A         int i_min = i_center + i_off - i_span;
// A         int i_max = i_center + i_off + i_span;
// A         int j_min = j_center + j_off - j_span;
// A         int j_max = j_center + j_off + j_span;
// A 
// A         (float u_min, float u_max) = find_min_max(stream_u, with_padding: true);
// A         (float v_min, float v_max) = find_min_max(stream_v, with_padding: true);
// A 
// A         int counter = 0;
// A         int nonzero_counter = 0;
// A 
// A         for (int i = i_min; i < i_max; i++)//17072024 stream_u[0].Count; i++)
// A         {
// A             for (int j = j_min; j < j_max; j++)//17072024 stream_u.Count; j++)
// A             {
// A                 // info (paul): If a triangle was found, which is close to the pixel, then load and assign 
// A                 //      the corresponding flow values
// A 
// A                 int idx_val = tri_idx[i, j];
// A                 bool pos_idx = idx_val > 0;
// A                 if (pos_idx)
// A                 {
// A                     bool isNaN = false;
// A                     (stream_u[i][j], stream_v[i][j], isNaN) = flow_or_loss_ij(stream_u, stream_v, d_xs,
// A                        d_ys, d_zs, i, j, tri_idx[i, j], t_idx, u_min, u_max, v_min, v_max);
// A                     nonzero_counter += (isNaN ? 0 : 1);
// A                     counter += 1;
// A                 }
// A                 else
// A                 {
// A                     (stream_u[i][j], stream_v[i][j]) = (0f, 0f);
// A                 }
// A             }
// A         }
// A 
// A         float coverage = 1f;//27092024 (float)(nonzero_counter) / (float)20239;//(float)(counter);//((float)(counter))/((float)(i_max* j_max));
// A         return (stream_u, stream_v, coverage);
// A     }
// A 
// A     public void update_scale_label(float loss_u_min, float loss_u_max, float loss_v_min, float loss_v_max)
// A     {
// A         Transform scale_label = canvas.transform.Find("scale_label");
// A         TextMeshProUGUI tmpro = scale_label.GetComponent<TextMeshProUGUI>();
// A 
// A         if (this.u_v_mode == "u")
// A         {
// A             tmpro.text = "min: " + loss_u_min.ToString() + "; max: " + loss_u_max.ToString();
// A         }
// A 
// A         if (this.u_v_mode == "v")
// A         {
// A             tmpro.text = "min: " + loss_v_min.ToString() + "; max: " + loss_v_max.ToString();
// A         }
// A     }
// A 
// A     int i_test = 330;
// A     int j_test = 230;
// A 
// A     public (float, bool) heights_or_loss_ij(List<List<float>> stream_u, List<List<float>> stream_ref,
// A         int i, int j, float u_min, float u_max, float scale_factor, float threshold = 0.1f,
// A         float threshold_up = 0.01f)
// A     {
// A         //A int node_idx = find_node(i, j, tri_idx, t_idx);
// A 
// A         // info (paul): These are the ground truth values
// A         //A float d_x_ref = d_xs[node_idx];
// A         //A float d_y_ref = -d_ys[node_idx];// info (paul): The "-" turns around the picture (hopefully)
// A         //A float d_z_ref = d_zs[node_idx];
// A         float d_x_ref = stream_ref[(int)(scale_factor * i)][(int)(scale_factor * j)];
// A 
// A         // TODO: I think, actually stream_u and stream_v are only equal to d_x and d_y, if the 
// A         //      camera has infinite distance. So it would be more precise to project d_x and d_y to 
// A         //      the camera somehow and this would then probably also include d_z
// A         float u_ij = float.NaN;
// A         try
// A         {
// A             u_ij = stream_u[i][j];
// A         }
// A         catch
// A         {
// A             u_ij = stream_u[i][j];
// A         }
// A         //A float v_ij = stream_v[i][j];
// A 
// A         // info (paul): absolute error
// A         float err_x_abs = Mathf.Abs(Mathf.Abs(u_ij) - Mathf.Abs(d_x_ref));//for debugging to not look at sign
// A         //A float err_y_abs = Mathf.Abs(Mathf.Abs(v_ij) - Mathf.Abs(d_y_ref));//for debugging
// A 
// A         if (i == i_test && j == j_test)
// A         {
// A             ;
// A         }
// A 
// A         if (Mathf.Abs(d_x_ref) < threshold || Mathf.Abs(u_ij) < threshold || u_ij < u_min + threshold_up || u_ij > u_max - threshold_up)
// A         {
// A             err_x_abs = float.NaN;//0.3f;//float.NaN;
// A         }
// A         //A if (Mathf.Abs(d_y_ref) < threshold || Mathf.Abs(v_ij) < threshold || v_ij < v_min + threshold_up || v_ij > v_max - threshold_up)
// A         //A {
// A         //A     err_y_abs = float.NaN;//0.3f;//float.NaN;
// A         //A }
// A 
// A         //17072024 float err_x_abs = Mathf.Abs(u_ij - d_x_ref);
// A         //17072024 float err_y_abs = Mathf.Abs(v_ij - d_y_ref);
// A 
// A         if (!float.IsNaN(err_x_abs))
// A         {
// A             ;
// A         }
// A 
// A         // info (paul): relative error
// A         float err_x_rel = err_x_abs / d_x_ref;
// A         //A float err_y_rel = err_y_abs / d_y_ref;
// A 
// A         //err_x_rel = threshold_err(err_x_rel);
// A         //err_y_rel = threshold_err(err_y_rel);
// A 
// A         (float val_u, float val_v) = (float.NaN, float.NaN);
// A         string plot_mode = this.get_plot_mode();
// A 
// A         //A if (plot_mode == "loss_rel")
// A         //A {
// A         //A     //A (val_u, val_v) = (err_x_rel, err_y_rel);
// A         //A     (val_u, val_v) = (err_x_rel, float.NaN);
// A         //A }
// A         //A if (plot_mode == "loss_abs")
// A         //A {
// A         //A     //A (val_u, val_v) = (err_x_abs, err_y_abs);
// A         //A     (val_u, val_v) = (err_x_abs, float.NaN);
// A         //A }
// A         //A if (plot_mode == "value")
// A         //A {
// A         //A     //A (val_u, val_v) = (u_ij, v_ij);
// A         //A     (val_u, val_v) = (u_ij, float.NaN);
// A         //A }
// A         //A if (plot_mode == "value_ref")
// A         //A {
// A         //A     (val_u, val_v) = (d_x_ref, float.NaN);
// A         //A }
// A         (val_u, val_v) = (err_x_rel, float.NaN);
// A 
// A         return (val_u, float.IsNaN(val_u)); //(err_x_rel, err_y_rel);// (err_x_rel, err_y_rel);//(d_x_ref, d_y_ref);//17062024 (err_x_rel, err_y_rel);
// A     }
// A 
// A 
// A     public (float, float, bool) flow_or_loss_ij(List<List<float>> stream_u, List<List<float>> stream_v,
// A         float[] d_xs, float[] d_ys, float[] d_zs, int i, int j, int tri_idx, int t_idx, float u_min,
// A         float u_max, float v_min, float v_max)
// A     {
// A         int node_idx = find_node(i, j, tri_idx, t_idx);
// A 
// A         // info (paul): These are the ground truth values
// A         float d_x_ref = d_xs[node_idx];
// A         float d_y_ref = -d_ys[node_idx];// info (paul): The "-" turns around the picture (hopefully)
// A         float d_z_ref = d_zs[node_idx];
// A 
// A         // TODO: I think, actually stream_u and stream_v are only equal to d_x and d_y, if the 
// A         //      camera has infinite distance. So it would be more precise to project d_x and d_y to 
// A         //      the camera somehow and this would then probably also include d_z
// A 
// A         float u_ij = stream_u[i][j];
// A         float v_ij = stream_v[i][j];
// A 
// A         // info (paul): absolute error
// A         float err_x_abs = Mathf.Abs(Mathf.Abs(u_ij) - Mathf.Abs(d_x_ref));//for debugging to not look at sign
// A         float err_y_abs = Mathf.Abs(Mathf.Abs(v_ij) - Mathf.Abs(d_y_ref));//for debugging
// A 
// A         float threshold = 0.1f;//27092024 0.1f;//0.1f;
// A         float threshold_up = 0.01f;//27092024 0.01f;
// A 
// A         if (i == i_test && j == j_test)
// A         {
// A             ;
// A         }
// A 
// A         if (Mathf.Abs(d_x_ref) < threshold || Mathf.Abs(u_ij) < threshold || u_ij < u_min + threshold_up || u_ij > u_max - threshold_up)
// A         {
// A             err_x_abs = float.NaN;//0.3f;//float.NaN;
// A         }
// A         if (Mathf.Abs(d_y_ref) < threshold || Mathf.Abs(v_ij) < threshold || v_ij < v_min + threshold_up || v_ij > v_max - threshold_up)
// A         {
// A             err_y_abs = float.NaN;//0.3f;//float.NaN;
// A         }
// A 
// A         //17072024 float err_x_abs = Mathf.Abs(u_ij - d_x_ref);
// A         //17072024 float err_y_abs = Mathf.Abs(v_ij - d_y_ref);
// A 
// A         // info (paul): relative error
// A         float err_x_rel = err_x_abs / d_x_ref;
// A         float err_y_rel = err_y_abs / d_y_ref;
// A 
// A         //err_x_rel = threshold_err(err_x_rel);
// A         //err_y_rel = threshold_err(err_y_rel);
// A 
// A         (float val_u, float val_v) = (float.NaN, float.NaN);
// A         string plot_mode = this.get_plot_mode();
// A         if (plot_mode == "loss_rel")
// A         {
// A             (val_u, val_v) = (err_x_rel, err_y_rel);
// A         }
// A         if (plot_mode == "loss_abs")
// A         {
// A             (val_u, val_v) = (err_x_abs, err_y_abs);
// A         }
// A         if (plot_mode == "value")
// A         {
// A             (val_u, val_v) = (u_ij, v_ij);
// A         }
// A         if (plot_mode == "value_ref")
// A         {
// A             (val_u, val_v) = (d_x_ref, d_y_ref);
// A         }
// A 
// A         return (val_u, val_v, float.IsNaN(val_v)); //(err_x_rel, err_y_rel);// (err_x_rel, err_y_rel);//(d_x_ref, d_y_ref);//17062024 (err_x_rel, err_y_rel);
// A     }
// A 
// A     public float threshold_err(float err_x_rel)
// A     {
// A         float threshold = 1.0f;
// A         if (Mathf.Abs(err_x_rel) > threshold)
// A         {
// A             err_x_rel = 10f;
// A         }
// A         else
// A         {
// A             err_x_rel = -10f;
// A         }
// A         return err_x_rel;
// A     }
// A 
// A     public string get_plot_mode()
// A     {
// A         return plot_mode;
// A     }
// A     public void set_plot_mode(string val, bool is_internal = false)
// A     {
// A         this.plot_mode = val;
// A         this.plot_mode = val;
// A         if (!is_internal)
// A         {
// A             set_paint_with("uv");
// A             //set_plot_mode("", is_internal: true);
// A         }
// A     }
// A     public void set_paint_with(string value)
// A     {
// A         this.paint_with = value;
// A     }
// A 
// A     public string get_paint_with()
// A     {
// A         return this.paint_with;
// A     }
// A 
// A     public int find_node(int i, int j, int tri_idx, int t_idx)
// A     {
// A         //int tri_idx = ;//find_triangle(i, j);
// A 
// A         float t_1 = -1f;
// A         float t_2 = -1f;
// A         float t_3 = -1f;
// A         float t_4 = -1f;
// A 
// A         if (i == 231 && j > 634) { tik(); }
// A         List<GameObject> blades = this.get_blades();
// A         if (i == 231 && j > 634) { t_1 = tok(); }
// A         if (i == 231 && j > 634) { tik(); }
// A 
// A         Mesh blade_mesh = null;
// A         //blade_mesh = blades[t_idx].GetComponent<MeshFilter>().sharedMesh;
// A         if (i == 231 && j > 634) { t_2 = tok(); }
// A         if (i == 231 && j > 634) { tik(); }
// A 
// A         //try
// A         //{
// A         //    blade_mesh = blades[t_idx].GetComponent<MeshFilter>().sharedMesh;
// A         //}
// A         //catch
// A         //{
// A         //    blade_mesh = blades[t_idx].GetComponent<MeshFilter>().sharedMesh;
// A         //}
// A 
// A         // info (paul): get the 3 point idxs of the triangle
// A         //17062024 int[] tri_0 = blade_mesh.triangles;
// A         if (i == 231 && j > 634) { t_3 = tok(); }
// A         if (i == 231 && j > 634) { tik(); }
// A 
// A         int node_0 = -1;
// A         try
// A         {
// A             node_0 = blade_tris[t_idx][3 * tri_idx + 0];
// A         }
// A         catch
// A         {
// A             node_0 = blade_tris[t_idx][3 * tri_idx + 0];
// A         }
// A 
// A         //13072024 int node_1 = blade_tris[t_idx][3 * tri_idx + 1];
// A         //13072024 int node_2 = blade_tris[t_idx][3 * tri_idx + 2];
// A         if (i == 231 && j > 634) {
// A             t_4 = tok();
// A         }
// A 
// A         // info (paul): for now we just pick the first idx
// A         return node_0;
// A     }
// A 
// A     public int find_triangle(int i, int j)
// A     {
// A         // TODO: Pick the closest vertex or interpolate would be even better, instead of just picking some vertex of the triangle
// A         if (i == 500)
// A         {
// A             if (j == 500)
// A             {
// A                 ;
// A             }
// A         }
// A 
// A 
// A         for (int idx = 0; idx < blades.Count; idx++)
// A         {
// A             blades[idx].SetActive(true);
// A         }
// A 
// A         int tri_idx = -1;
// A         Ray ray_ij = cam_for_uv_0.ScreenPointToRay(new Vector3(i, j));
// A 
// A         RaycastHit hit;
// A         bool has_hit = Physics.Raycast(cam_for_uv_0.transform.position, ray_ij.direction, out hit, Mathf.Infinity);
// A         if (has_hit)
// A         {
// A             tri_idx = hit.triangleIndex;
// A         }
// A         return tri_idx;
// A     }
// A 
// A     public List<List<float>> choose_coord(List<List<float>> stream_u, List<List<float>> stream_v, string u_v_mode)
// A     {
// A         List<List<float>> flow_mats_chosen = new List<List<float>>();
// A         if (u_v_mode == "u")
// A         {
// A             flow_mats_chosen = stream_u; //27052024 flow_mats_u[t_idx];
// A         }
// A         if (u_v_mode == "v")
// A         {
// A             flow_mats_chosen = stream_v; //27052024 flow_mats_v[t_idx];
// A         }
// A         if (u_v_mode == "z")
// A         {
// A             flow_mats_chosen = stream_v; // just to have it not empty
// A         }
// A         return flow_mats_chosen;
// A     }
// A 
// A     public List<List<(float, float)>> norm_points(List<List<(float, float)>> points_fluc)
// A     {
// A         (List<List<float>> points_x, List<List<float>> points_y) = split_match(points_fluc);//points_fluc);
// A         points_x = norm_mat(points_x);
// A         points_y = norm_mat(points_y);
// A         List<List<(float, float)>> points_normed = merge(points_x, points_y);
// A         return points_normed;
// A     }
// A 
// A     public Texture2D to_tex_if(List<List<float>> flow_mats_chosen, List<Texture2D> texs_albedo_u,
// A         List<Texture2D> texs_albedo_v)
// A     {
// A         bool overwrite_for_simple = false;
// A         Texture2D tex = null;
// A         if (overwrite_for_simple)
// A         {
// A             tex = mat2tex(flow_mats_chosen, with_switch_dims: false);//30082024 texs_albedo_u[t_idx];//12062024 mat2tex(flow_mats_chosen, with_switch_dims: false);
// A             if (u_v_mode == "v") { tex = texs_albedo_v[t_idx]; }
// A         }
// A         else
// A         {
// A             flow_mats_chosen = norm_mat(flow_mats_chosen, lower: -3f, upper: 3f);
// A             tex = mat2tex(flow_mats_chosen, with_switch_dims: false);
// A         }
// A         return tex;
// A     }
// A 
// A     public (List<List<float>>, List<List<float>>) split_match(
// A         List<List<(int, int)>> match_mat)
// A     {
// A         List<List<float>> match_u = zeros_like(match_mat, return_type: "floats");
// A         List<List<float>> match_v = zeros_like(match_mat, return_type: "floats");
// A 
// A         for (int i = 0; i < match_mat.Count; i++)
// A         {
// A             for (int j = 0; j < match_mat[0].Count; j++)
// A             {
// A                 match_u[i][j] = match_mat[i][j].Item1;
// A                 match_v[i][j] = match_mat[i][j].Item2;
// A             }
// A         }
// A 
// A         return (match_u, match_v);
// A     }
// A 
// A     public (List<List<float>>, List<List<float>>) split_match(
// A     List<List<(float, float)>> match_mat)
// A     {
// A         List<List<float>> match_u = zeros_like(match_mat, return_type: "floats");
// A         List<List<float>> match_v = zeros_like(match_mat, return_type: "floats");
// A 
// A         for (int i = 0; i < match_mat.Count; i++)
// A         {
// A             for (int j = 0; j < match_mat[0].Count; j++)
// A             {
// A                 match_u[i][j] = match_mat[i][j].Item1;
// A                 match_v[i][j] = match_mat[i][j].Item2;
// A             }
// A         }
// A 
// A         return (match_u, match_v);
// A     }
// A 
// A     public List<List<(float, float)>> merge(List<List<float>> mat_1, List<List<float>> mat_2)
// A     {
// A         List<List<(float, float)>> merged = zero_tuples_like(mat_1);
// A 
// A         for (int i = 0; i < mat_1.Count; i++)
// A         {
// A             for (int j = 0; j < mat_1[0].Count; j++)
// A             {
// A                 merged[i][j] = (mat_1[i][j], mat_2[i][j]);
// A             }
// A         }
// A 
// A         return merged;
// A     }
// A 
// A     public List<List<(float, float)>> make_grid(List<List<(float, float)>> points_now, int i_min, int j_min)
// A     {
// A         for (int i = 0; i < points_now.Count; i++)
// A         {
// A             for (int j = 0; j < points_now[0].Count; j++)
// A             {
// A                 float val_1 = (float)(i);//18062024  + i_min);
// A                 float val_2 = (float)(j);//18062024  + j_min);
// A                 points_now[i][j] = (val_1, val_2);
// A             }
// A         }
// A 
// A         return points_now;
// A     }
// A 
// A     public List<List<(float, float)>> match_all_to_start(List<List<List<float>>> flow_mats_u,
// A     List<List<List<float>>> flow_mats_v, List<List<(float, float)>> points, int t_min = -1, int t_max = -1)
// A     {
// A         // TODO: correct to match a lot of points and not just a single point
// A 
// A         int padding = 5;
// A 
// A         int i_min = 0 + padding;
// A         int i_max = flow_mats_u[0].Count - padding;
// A 
// A         int j_min = 0 + padding;
// A         int j_max = flow_mats_u[0][0].Count - padding;
// A 
// A         List<List<(float, float)>> points_now = zero_tuples_of_size(i_max - i_min + 2 * padding, j_max - j_min + 2 * padding, type: "float");
// A         points_now = make_grid(points_now, i_min, j_min);
// A 
// A         for (int t = t_min; t < t_max; t++)
// A         {
// A             List<List<(float, float)>> points_next = match_slice(padding, i_min, i_max, j_min,
// A                 j_max, t, flow_mats_u, flow_mats_v, points_now);
// A             points_now = points_next;
// A         }
// A 
// A         //14072024 for (int i = 0; i < 2; i++)
// A         //14072024 {
// A         //14072024     points_now = filter_mean(points_now);
// A         //14072024 }
// A 
// A         //14072024 points_now = paint_for_debug(points_now);
// A 
// A         return points_now;
// A     }
// A     public List<List<float>> filter_mean_comp(
// A     List<List<float>> points_now)
// A     {
// A         List<List<float>> points_next = zeros_of_size(points_now.Count,
// A             points_now.Count);
// A 
// A         int padding = 5;
// A 
// A         for (int i = padding; i < points_now.Count - padding; i++)
// A         {
// A             for (int j = padding; j < points_now[0].Count - padding; j++)
// A             {
// A                 float item_1 = plain_conv_comp(points_now, i, j);
// A 
// A                 points_next[i][j] = (item_1);
// A             }
// A         }
// A 
// A         return points_next;
// A     }
// A     public List<List<(float, float)>> filter_mean(
// A         List<List<(float, float)>> points_now)
// A     {
// A         List<List<(float, float)>> points_next = zero_tuples_of_size(points_now.Count,
// A             points_now.Count, type: "float");
// A 
// A         int padding = 5;
// A 
// A         for (int i = padding; i < points_now.Count - padding; i++)
// A         {
// A             for (int j = padding; j < points_now[0].Count - padding; j++)
// A             {
// A                 (float item_1, float item_2) = plain_conv(points_now, i, j);
// A 
// A                 points_next[i][j] = (item_1, item_2);
// A             }
// A         }
// A 
// A         return points_next;
// A     }
// A     public float plain_conv_comp(List<List<float>>
// A         points_now, int i, int j, int plaquette_size = 5)
// A     {
// A         float sum_1 = 0f;
// A         float sum_2 = 0f;
// A         int cnt = 0;
// A 
// A         for (int k = -plaquette_size; k < plaquette_size; k++)
// A         {
// A             for (int l = -plaquette_size; l < plaquette_size; l++)
// A             {
// A 
// A                 if (i > 10 && j > 10)
// A                 {
// A                     ;
// A                 }
// A                 int idx_i = i + k;
// A                 int idx_j = j + l;
// A 
// A                 bool above_lower_i = (idx_i > 0);
// A                 bool below_upper_i = (idx_i < points_now.Count);
// A 
// A                 bool above_lower_j = (idx_j > 0);
// A                 bool below_upper_j = (idx_j < points_now.Count);
// A 
// A                 bool in_frame = above_lower_i && below_upper_i && above_lower_j && below_upper_j;
// A                 if (in_frame)
// A                 {
// A                     float summand_1 = points_now[idx_i][idx_j];
// A 
// A                     sum_1 += summand_1;
// A                     cnt += 1;
// A                 }
// A             }
// A         }
// A 
// A         float item_1 = sum_1 / ((float)(cnt));
// A 
// A         //float item_1 = (1 / 5f) * (points_now[i][j].Item1 + points_now[i][j - 1].Item1
// A         //    + points_now[i][j + 1].Item1 + points_now[i - 1][j].Item1 +
// A         //    points_now[i + 1][j].Item1);
// A         //float item_2 = (1 / 5f) * (points_now[i][j].Item2 + points_now[i][j - 1].Item2
// A         //    + points_now[i][j + 1].Item2 + points_now[i - 1][j].Item2 +
// A         //    points_now[i + 1][j].Item2);
// A         return item_1;
// A     }
// A     public (float, float) plain_conv(List<List<(float, float)>>
// A         points_now, int i, int j, int plaquette_size = 1)
// A     {
// A         float sum_1 = 0f;
// A         float sum_2 = 0f;
// A         int cnt = 0;
// A 
// A         for (int k = -plaquette_size; k < plaquette_size; k++)
// A         {
// A             for (int l = -plaquette_size; l < plaquette_size; l++)
// A             {
// A 
// A                 if (i > 10 && j > 10)
// A                 {
// A                     ;
// A                 }
// A                 int idx_i = i + k;
// A                 int idx_j = j + l;
// A 
// A                 bool above_lower_i = (idx_i > 0);
// A                 bool below_upper_i = (idx_i < points_now.Count);
// A 
// A                 bool above_lower_j = (idx_j > 0);
// A                 bool below_upper_j = (idx_j < points_now.Count);
// A 
// A                 bool in_frame = above_lower_i && below_upper_i && above_lower_j && below_upper_j;
// A                 if (in_frame)
// A                 {
// A                     float summand_1 = points_now[idx_i][idx_j].Item1;
// A                     float summand_2 = points_now[idx_i][idx_j].Item2;
// A 
// A                     sum_1 += summand_1;
// A                     sum_2 += summand_2;
// A                     cnt += 1;
// A                 }
// A             }
// A         }
// A 
// A         float item_1 = sum_1 / ((float)(cnt));
// A         float item_2 = sum_2 / ((float)(cnt));
// A 
// A         //float item_1 = (1 / 5f) * (points_now[i][j].Item1 + points_now[i][j - 1].Item1
// A         //    + points_now[i][j + 1].Item1 + points_now[i - 1][j].Item1 +
// A         //    points_now[i + 1][j].Item1);
// A         //float item_2 = (1 / 5f) * (points_now[i][j].Item2 + points_now[i][j - 1].Item2
// A         //    + points_now[i][j + 1].Item2 + points_now[i - 1][j].Item2 +
// A         //    points_now[i + 1][j].Item2);
// A         return (item_1, item_2);
// A     }
// A     public List<List<(float, float)>> paint_for_debug(
// A         List<List<(float, float)>> points)
// A     {
// A         for (int i = 0; i < points.Count; i++)
// A         {
// A             points[256][i] = (10f, 10f);
// A             points[256][i] = (10f, 10f);
// A         }
// A 
// A         return points;
// A     }
// A 
// A 
// A     public List<List<(float, float)>> match_slice(int padding, int i_min, int i_max, int j_min, int j_max, int t,
// A         List<List<List<float>>> flow_mats_u, List<List<List<float>>> flow_mats_v, List<List<(float, float)>> points_now)
// A     {
// A         List<List<(float, float)>> points_next = zero_tuples_of_size(i_max - i_min + 2 * padding, j_max - j_min + 2 * padding, type: "float");
// A 
// A         for (int i = i_min; i < i_max; i++)
// A         {
// A             for (int j = j_min; j < j_max; j++)
// A             {
// A                 (float, float) vals_l = match_to_start(flow_mats_u,
// A                     flow_mats_v, points_now[i][j], t_min: t, t_max: t + 1, i: i, j: j);
// A 
// A                 points_next[i][j] = vals_l;//(vals_l[0].Item1 + (float)(j % 2), vals_l[0].Item2 + (float)(j % 2));//((float)(i % 2), (float)(i % 2));//vals_l[0];//(flow_mats_u[t_idx][i][j], flow_mats_v[t_idx][i][j]);//27052024 vals_l[0];
// A             }
// A         }
// A         return points_next;
// A     }
// A     public (float, float) match_to_start(List<List<List<float>>> flow_mats_u,
// A     List<List<List<float>>> flow_mats_v, (float, float) point, int t_min = -1,
// A     int t_max = -1, int i = -1, int j = -1)
// A     {
// A         // info (paul): find match matrices
// A         //(float, float) point = (3.5f, 5.2f);// info (paul): or whatever your startpoint is
// A         Dictionary<float, (float, float)> points_dic = new Dictionary<float, (float, float)>();
// A 
// A         if (t_min < 0)
// A         {
// A             t_min = 0;
// A             t_max = flow_mats_u.Count;
// A             if (t_max > 4)
// A             {
// A                 t_max = 0;
// A             }
// A         }
// A 
// A         for (int t_idx = t_min; t_idx < t_max; t_idx++)
// A         {
// A             try
// A             {
// A                 point = find_next_point(flow_mats_u[t_idx], flow_mats_v[t_idx], point, i: i, j: j);
// A             }
// A             catch
// A             {
// A                 point = find_next_point(flow_mats_u[t_idx], flow_mats_v[t_idx], point, i: i, j: j);
// A             }
// A             points_dic.Add((float)(t_idx), point);
// A         }
// A 
// A         List<(float, float)> vals_l = points_dic.Values.ToList();
// A 
// A         return vals_l[0];
// A     }
// A 
// A     public List<List<(int, int)>> match_to_start_old(List<List<List<float>>> flow_mats_u,
// A         List<List<List<float>>> flow_mats_v, int im_cnt = -1)
// A     {
// A         // info (paul): find match matrices
// A         //25052024 List<List<List<(int, int)>>> match_mats = new List<List<List<(int, int)>>>();
// A         (float, float) point = (3.5f, 5.2f);// info (paul): or whatever your startpoint is
// A 
// A         if (im_cnt < 0)
// A         {
// A             im_cnt = flow_mats_u.Count;
// A         }
// A         for (int t_idx = 0; t_idx < im_cnt; t_idx++)
// A         {
// A             //25052024 List<List<(int, int)>> match_mat = match_at_t(flow_mats_u, flow_mats_v, t_idx);
// A             //25052024 match_mats.Add(match_mat);
// A             point = find_next_point(flow_mats_u[t_idx], flow_mats_v[t_idx], point);
// A         }
// A 
// A         List<List<(int, int)>> total_match = null; //25052024  find_total_match(match_mats);
// A 
// A         return total_match;
// A     }
// A 
// A     public (float, float, float, float, float, float) find_interpolate_square(
// A         List<List<float>> mat, float point_x, float point_y)
// A     {
// A         // info (paul): find the points of the square around the point (point_x, point_y)
// A         // we assume, that point_x and point_y are > 0;
// A 
// A         float lower_x_f = (float)Math.Floor(point_x);
// A         float upper_x_f = (float)Math.Ceiling(point_x);
// A         float lower_y_f = (float)Math.Floor(point_y);
// A         float upper_y_f = (float)Math.Ceiling(point_y);
// A 
// A         int lower_x = (int)lower_x_f;//29052024 (int)(lower_x_f);
// A         int upper_x = (int)upper_x_f;//29052024 (int)(lower_x_f);
// A         int lower_y = (int)lower_y_f;//29052024 (int)(lower_x_f);
// A         int upper_y = (int)upper_y_f;//29052024 (int)(lower_x_f);
// A 
// A         int len_x = mat.Count;
// A         int len_y = mat[0].Count;
// A         bool x_in_frame = (0 <= lower_x) && (upper_x < len_x);
// A         bool y_in_frame = (0 <= lower_y) && (upper_y < len_y);
// A 
// A         (float val_00, float val_01, float val_10, float val_11)
// A             = (0f, 0f, 0f, 0f);
// A         (float rest_x, float rest_y) = (0f, 0f);
// A         if (x_in_frame && y_in_frame)
// A         {
// A             rest_x = point_x - lower_x_f;
// A             rest_y = point_y - lower_y_f;//29052024 point_x - lower_y_f;
// A 
// A             val_00 = mat[lower_x][lower_y];
// A             val_01 = mat[lower_x][upper_y];
// A             val_10 = mat[upper_x][lower_y];
// A             val_11 = mat[upper_x][upper_y];
// A         }
// A         else
// A         {
// A             float val_mean = extrapolate(mat, lower_x, upper_x, lower_y, upper_y);
// A             val_00 = -0.2f;//val_mean;
// A             val_01 = -0.2f;//val_mean;
// A             val_10 = -0.2f;//val_mean;
// A             val_11 = -0.2f;//val_mean;
// A         }
// A 
// A         return (val_00, val_01, val_10, val_11, rest_x, rest_y);
// A     }
// A 
// A     public bool check_if_in_frame(List<List<float>> mat, (int, int) point)
// A     {
// A         int len_x = mat.Count;
// A         int len_y = mat[0].Count;
// A 
// A         bool x_in_range = (0 <= point.Item1 && point.Item1 < len_x);
// A         bool y_in_range = (0 <= point.Item2 && point.Item2 < len_y);
// A 
// A         bool in_frame = x_in_range && y_in_range;
// A 
// A         return in_frame;
// A     }
// A 
// A     public float extrapolate(List<List<float>> mat, int lower_x, int upper_x, int lower_y, int upper_y)
// A     {
// A         // info (paul): a very rough way to interpolate, I am too lazy now to make it more complex, 
// A         //      probably also not so important.
// A 
// A         int len_x = mat.Count;
// A         int len_y = mat[0].Count;
// A 
// A         float val = float.NaN;
// A 
// A         bool in_frame_00 = check_if_in_frame(mat, (lower_x, lower_y));
// A         bool in_frame_01 = check_if_in_frame(mat, (lower_x, upper_y));
// A         bool in_frame_10 = check_if_in_frame(mat, (upper_x, lower_y));
// A         bool in_frame_11 = check_if_in_frame(mat, (upper_x, upper_y));
// A 
// A         if (in_frame_00)
// A         {
// A             val = mat[lower_x][lower_y];
// A         }
// A         if (in_frame_01)
// A         {
// A             val = mat[lower_x][upper_y];
// A         }
// A         if (in_frame_10)
// A         {
// A             val = mat[upper_x][lower_y];
// A         }
// A         if (in_frame_11)
// A         {
// A             val = mat[upper_x][upper_y];
// A         }
// A 
// A         /*if (lower_x < 0)
// A         {
// A             if (lower_y < 0)
// A             {
// A                 ;
// A             }
// A 
// A             if (0 <= lower_y && lower_y < len_y)
// A             {
// A                 ;
// A             }
// A 
// A             if (len_y <= lower_y)
// A             {
// A                 ;
// A             }
// A         }
// A 
// A         if (len_x < lower_x)
// A         {
// A             if (lower_y < 0)
// A             {
// A                 ;
// A             }
// A 
// A             if (0 <= lower_y && lower_y < len_x)
// A             {
// A                 ;
// A             }
// A 
// A             if (len_x <= lower_y)
// A             {
// A                 ;
// A             }
// A         }*/
// A 
// A         return val;
// A 
// A     }
// A     public float interpolate_at(List<List<float>> mat, float point_x, float point_y)
// A     {
// A         // info (paul): get the linearly interpolated value at a point (point_x, point_y)
// A 
// A         (float val_00, float val_01, float val_10, float val_11,
// A             float rest_x, float rest_y) =
// A             find_interpolate_square(mat, point_x, point_y);
// A 
// A         float weight_00 = (1 - rest_x) * (1 - rest_y);
// A         float weight_01 = (1 - rest_x) * (rest_y);
// A         float weight_10 = (rest_x) * (1 - rest_y);
// A         float weight_11 = (rest_x) * (rest_y);
// A 
// A         float result = val_00 * weight_00 + val_01 * weight_01 + val_10 * weight_10 + val_11 * weight_11;
// A         return result;
// A     }
// A 
// A     public (float, float) find_next_point(List<List<float>> mat_u, List<List<float>> mat_v, (float, float) point,
// A         int i = -1, int j = -1)
// A     {
// A         // info (paul): find next point by interpolation with u and v and so on
// A 
// A         // info (paul): find the next values by interpolating u and v
// A         float u_val = interpolate_at(mat_u, point.Item1, point.Item2);
// A         float v_val = interpolate_at(mat_v, point.Item1, point.Item2);
// A 
// A         float point_i = point.Item1;
// A         float point_j = point.Item2;
// A 
// A         float x_next = point_i + u_val;
// A         float y_next = point_j + v_val;
// A 
// A         return (x_next, y_next);
// A     }
// A 
// A     public List<List<(int, int)>> find_total_match(List<List<List<(int, int)>>> match_mats)
// A     {
// A         // info (paul): calculate the global match_matrix and assign initial values
// A         List<List<(int, int)>> total_match = init_total_match_mat(match_mats);
// A 
// A 
// A         // info (paul): for each time step add this component
// A         for (int t = 0; t < match_mats.Count; t++)
// A         {
// A             total_match = make_next_match(match_mats, total_match, t);
// A 
// A         }
// A 
// A         return total_match;
// A     }
// A 
// A     public List<List<(int, int)>> make_next_match(
// A         List<List<List<(int, int)>>> match_mats,
// A         List<List<(int, int)>> total_match, int t)
// A     {
// A 
// A         for (int i = 0; i < match_mats[0].Count; i++)
// A         {
// A             for (int j = 0; j < match_mats[0][0].Count; j++)
// A             {
// A                 int i_now = total_match[i][j].Item1;
// A                 int j_now = total_match[i][j].Item2;
// A                 (int i_next, int j_next) = match_mats[t][i_now][j_now];
// A                 total_match[i][j] = (i_next, j_next);
// A             }
// A         }
// A         return total_match;
// A     }
// A 
// A     public List<List<(int, int)>> init_total_match_mat(List<List<List<(int, int)>>> match_mats)
// A     {
// A         List<List<(int, int)>> total_match = zeros_like(match_mats[0]);
// A         for (int i = 0; i < match_mats[0].Count; i++)
// A         {
// A             for (int j = 0; j < match_mats[0][0].Count; j++)
// A             {
// A                 total_match[i][j] = (i, j);
// A             }
// A         }
// A         return total_match;
// A     }
// A     public List<List<(float, float)>> zero_tuples_of_size(int len_x, int len_y, string type = "float")
// A     {
// A         List<List<(float, float)>> zeros = new List<List<(float, float)>>();
// A         for (int i = 0; i < len_x; i++)
// A         {
// A             zeros.Add(new List<(float, float)>());
// A 
// A             for (int j = 0; j < len_y; j++)
// A             {
// A                 zeros[i].Add((0, 0));
// A             }
// A         }
// A 
// A         return zeros;
// A     }
// A     public List<List<List<(float, float)>>> zero_tuples_of_size(int len_t, int len_x, int len_y, string type = "float")
// A     {
// A         List<List<List<(float, float)>>> tuples = new List<List<List<(float, float)>>>();
// A 
// A         for (int t = 0; t < len_t; t++)
// A         {
// A             List<List<(float, float)>> zeros = new List<List<(float, float)>>();
// A             tuples.Add(zeros);
// A 
// A             for (int i = 0; i < len_x; i++)
// A             {
// A                 zeros.Add(new List<(float, float)>());
// A 
// A                 for (int j = 0; j < len_y; j++)
// A                 {
// A                     zeros[i].Add((0, 0));
// A                 }
// A             }
// A         }
// A 
// A         return tuples;
// A     }
// A     public List<List<(int, int)>> zero_tuples_of_size(int len_x, int len_y)
// A     {
// A         List<List<(int, int)>> zeros = new List<List<(int, int)>>();
// A         for (int i = 0; i < len_x; i++)
// A         {
// A             zeros.Add(new List<(int, int)>());
// A 
// A             for (int j = 0; j < len_y; j++)
// A             {
// A                 zeros[i].Add((0, 0));
// A             }
// A         }
// A 
// A         return zeros;
// A     }
// A     public List<List<float>> zeros_of_size(int len_x, int len_y)
// A     {
// A         List<List<float>> zeros = new List<List<float>>();
// A         for (int i = 0; i < len_x; i++)
// A         {
// A             zeros.Add(new List<float>());
// A 
// A             for (int j = 0; j < len_y; j++)
// A             {
// A                 zeros[i].Add(0f);
// A             }
// A         }
// A 
// A         return zeros;
// A     }
// A     public List<List<double>> doubles_of_size(int len_x, int len_y)
// A     {
// A         List<List<double>> zeros = new List<List<double>>();
// A         for (int i = 0; i < len_x; i++)
// A         {
// A             zeros.Add(new List<double>());
// A 
// A             for (int j = 0; j < len_y; j++)
// A             {
// A                 zeros[i].Add(0f);
// A             }
// A         }
// A 
// A         return zeros;
// A     }
// A     public List<List<(int, int)>> match_at_t(List<List<List<float>>> mats_u, List<List<List<float>>> mats_v, int t_idx)
// A     {
// A         // info (paul): 
// A 
// A         int width = mats_u[0].Count;
// A         int height = mats_v[0][0].Count;
// A 
// A         List<List<(int, int)>> match_t = zero_tuples_of_size(width, height);//new List<List<(int, int)>>();
// A 
// A         for (int i = 0; i < width; i++)
// A         {
// A             for (int j = 0; j < height; j++)
// A             {
// A                 float u_val = mats_u[t_idx][i][j];
// A                 float v_val = mats_v[t_idx][i][j];
// A 
// A                 if (u_val > 0.5f)
// A                 {
// A                     ;
// A                 }
// A                 if (v_val > 0.5f)
// A                 {
// A                     ;
// A                 }
// A 
// A                 float i_pos_next = (float)i + u_val;
// A                 float j_pos_next = (float)j + v_val;
// A 
// A                 int i_idx_next = i;//(int)(i_pos_next);
// A                 int j_idx_next = j;//(int)(j_pos_next);
// A 
// A                 match_t[i][j] = (i_idx_next, j_idx_next);
// A             }
// A         }
// A 
// A         //match_step();
// A         return match_t;
// A     }
// A 
// A     public void disp2match(List<List<float>> disps)
// A     {
// A         // info (paul): convert the disparity matrix into a matrix, 
// A         //      which matches each pixel to the corresponding other pixel
// A 
// A 
// A         for (int i = 0; i < disps.Count; i++)
// A         {
// A             for (int j = 0; j < disps[0].Count; j++)
// A             {
// A                 ;
// A             }
// A         }
// A     }
// A 
// A 
// A     public (List<List<List<float>>>, List<List<List<float>>>, List<Texture2D>, List<Texture2D>,
// A         int, int) find_flow_mats(Dictionary<string, Dictionary<int, FileInfo>> flow_files,
// A         int im_cnt = -1)
// A     {
// A         List<List<List<float>>> mats_u = new List<List<List<float>>>();
// A         List<List<List<float>>> mats_v = new List<List<List<float>>>();
// A 
// A         int t_cnt = im_cnt;//16052024 flow_files["u"].Count;
// A         List<int> keys_u = flow_files["u"].Keys.ToList();
// A         List<int> keys_v = flow_files["v"].Keys.ToList();
// A 
// A         List<Texture2D> texs_albedo_u = new List<Texture2D>();// (res_x, res_y);
// A         List<Texture2D> texs_albedo_v = new List<Texture2D>();// (res_x, res_y);
// A 
// A         (int res_x, int res_y) = (-1, -1);
// A 
// A         for (int t_idx = 0; t_idx < t_cnt; t_idx++)
// A         {
// A             List<List<float>> mat_u = load_flow_u(flow_files, keys_u, texs_albedo_u, blade_idx: t_idx);
// A 
// A             //17072024 mat_u = filter_mean_comp(mat_u);//17072024 mean to smooth errors
// A 
// A             mats_u.Add(mat_u);
// A 
// A             List<List<float>> mat_v = load_flow_v(flow_files, keys_v, texs_albedo_v, blade_idx: t_idx);
// A 
// A             //17072024 mat_v = filter_mean_comp(mat_v);
// A 
// A             //mat_v = take_share(mat_v);
// A             mats_v.Add(mat_v);
// A 
// A         }
// A 
// A         return (mats_u, mats_v, texs_albedo_u, texs_albedo_v, res_x, res_y);
// A     }
// A 
// A     public List<List<float>> load_flow_u(Dictionary<string, Dictionary<int, FileInfo>> flow_files,
// A         List<int> keys_u, List<Texture2D> texs_albedo_u, int blade_idx = -1)
// A     {
// A         int key_u = keys_u[t_idx];
// A 
// A         string file_path_u = flow_files["u"][key_u].FullName;
// A         byte[] im_bytes_u = System.IO.File.ReadAllBytes(file_path_u);
// A 
// A         DirectoryInfo dir_v = new DirectoryInfo(path_time_flow_v);
// A         FileInfo[] dir_info_v = dir_v.GetFiles("*");
// A 
// A         // info (paul): assuming, that the resolution of the first image is 
// A         //      the resolution of all the images
// A         if (true)//20062024 (t_idx == 0)
// A         {
// A             (res_x, res_y) = bytes2res(im_bytes_u);
// A         }
// A 
// A         Texture2D tex_albedo_u = new Texture2D(res_x, res_y);
// A         texs_albedo_u.Add(tex_albedo_u);
// A         tex_albedo_u.LoadImage(im_bytes_u);
// A         List<List<float>> mat_u = tex2mat(tex_albedo_u);
// A 
// A         // info (paul): find min and max val
// A         string min_max_file = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(get_experiment()) + "/min_max_u_" + blade_idx.ToString() + ".txt";
// A         string min_max_str = load_txt_line(min_max_file);
// A         string[] strs = min_max_str.Split(" ");
// A         float min_val = float.Parse(strs[0]);
// A         float max_val = float.NaN;
// A         max_val = float.Parse(strs[1]);
// A         mat_u = unnorm_mat(mat_u, min_val, max_val);
// A 
// A         return mat_u;
// A 
// A     }
// A 
// A     public List<List<float>> load_flow_v(Dictionary<string, Dictionary<int, FileInfo>> flow_files,
// A         List<int> keys_v, List<Texture2D> texs_albedo_v, int blade_idx = -1)
// A     {
// A         int key_v = keys_v[t_idx];
// A         string file_path_v = flow_files["v"][key_v].FullName;
// A         byte[] im_bytes_v = System.IO.File.ReadAllBytes(file_path_v);
// A         Texture2D tex_albedo_v = new Texture2D(res_x, res_y);
// A         texs_albedo_v.Add(tex_albedo_v);
// A         tex_albedo_v.LoadImage(im_bytes_v);
// A         List<List<float>> mat_v = tex2mat(tex_albedo_v);
// A 
// A         // info (paul): find min and max val
// A         string min_max_file = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(get_experiment()) + "/min_max_v_" + blade_idx.ToString() + ".txt";
// A         string min_max_str = load_txt_line(min_max_file);
// A         string[] strs = min_max_str.Split(" ");
// A         float min_val = float.Parse(strs[0]);
// A         float max_val = float.Parse(strs[1]);
// A 
// A         mat_v = unnorm_mat(mat_v, min_val, max_val);
// A 
// A         return mat_v;
// A     }
// A 
// A     public (int, int) bytes2res(byte[] im_bytes_u)
// A     {
// A         // info (paul): test ints:
// A         // int intValue = 256;
// A         // byte[] intBytes = BitConverter.GetBytes(intValue);
// A         // Array.Reverse(intBytes);
// A         // byte[] result = intBytes;
// A 
// A         byte[] bytes_4 = { im_bytes_u[0], im_bytes_u[1], im_bytes_u[2], im_bytes_u[3] };
// A 
// A         if (BitConverter.IsLittleEndian)
// A         {
// A             Array.Reverse(bytes_4);
// A         }
// A 
// A         int result_2 = BitConverter.ToInt32(bytes_4);
// A 
// A         byte[] width_bytes = { im_bytes_u[16], im_bytes_u[17], im_bytes_u[18], im_bytes_u[19] };
// A         byte[] height_bytes = { im_bytes_u[20], im_bytes_u[21], im_bytes_u[22], im_bytes_u[23] };
// A 
// A         if (BitConverter.IsLittleEndian)
// A         {
// A             Array.Reverse(width_bytes);
// A             Array.Reverse(height_bytes);
// A         }
// A 
// A         int res_x = BitConverter.ToInt32(width_bytes);
// A         int res_y = BitConverter.ToInt32(height_bytes);
// A         return (res_x, res_y);
// A     }
// A 
// A     public Texture2D calc_strain(Texture2D tex_input, List<List<float>> heights_chosen)
// A     {
// A         // info (paul): uncut version is calc_strain_copy
// A 
// A         // info (paul): calculate e.g. the strain field from the
// A         //      disparities or whatever is most convenient
// A 
// A         Texture2D tex = new Texture2D(tex_input.width, tex_input.height);
// A 
// A         int res_x = tex_input.width;
// A         int res_y = tex_input.height;
// A 
// A         // info (paul): getting texture pixels as float-matrix (red colorchannel)
// A         //15052024 Color[] cols = tex.GetPixels(0, 0, res_x, res_y);
// A         //15052024 Color[,] cols_mat = list2matrix(cols, res_x, res_y);
// A         //15052024 List<List<float>> mat = cols_mat2floats_mat(cols_mat);
// A         List<List<float>> mat = tex2mat(tex_input, with_switch_dims: false);//true(?)//15052024 , res_x, res_y);
// A         //A /*A 29052024
// A 
// A         // info (paul): If we look for 3rd coordinate strain, use heights instead of mat:
// A         if (get_u_v_mode() == "z")
// A         {
// A             mat = heights_chosen;
// A         }
// A 
// A         // info (paul): Do physics filter, e.g. strain field etc. (mat_1, mat_2 are strain derivatives)
// A         (List<List<float>> mat_1, List<List<float>> mat_2) = find_physics_filter(mat);
// A 
// A         // info (paul): converting back floats-matrix to colors and assign to texture
// A 
// A         // info (paul): choose, whether to display u or v
// A 
// A         List<List<float>> mat_displayed = mat_1;
// A         if (strain_d_mode == "x")
// A         {
// A             mat_displayed = mat_1;
// A         }
// A         if (strain_d_mode == "y")
// A         {
// A             mat_displayed = mat_2;
// A         }
// A         //A */
// A 
// A         //List<List<float>> mat_rand = randomize_mat(mat);
// A         //List<List<float>> mat_2 = tex2mat(tex_out, with_switch_dims: true);
// A 
// A         for (int i = 0; i < 1; i++)
// A         {
// A             mat_displayed = filter_mean_comp(mat_displayed);
// A         }
// A 
// A         Texture2D tex_out = mat2tex(mat_displayed, with_switch_dims: false);
// A 
// A         return tex_out;//tex
// A     }
// A 
// A     //12062024 public List<List<float>> randomize_mat(List<List<float>> mat)
// A     //12062024 {
// A     //12062024     res_x = mat.Count;
// A     //12062024     res_y = mat[0].Count;
// A     //12062024     for (int i = 0; i < res_x; i++)
// A     //12062024     {
// A     //12062024         for (int j = 0; j < res_y; j++)
// A     //12062024         {
// A     //12062024             mat[i][j] = UnityEngine.Random.Range(0f, 1f);
// A     //12062024         }
// A     //12062024     }
// A     //12062024     return mat;
// A     //12062024 }
// A     //public Texture2D mat2tex(List<List<float>> mat)
// A     //{
// A     //    int res_x = mat.Count;
// A     //    int res_y = mat[0].Count;
// A     //    
// A     //    Texture2D tex = new Texture2D(res_x, res_y);
// A     //
// A     //    UnityEngine.Color[,] cols_mat = floats2col_mat(mat);//22052024 mat_displayed//mat_1
// A     //    UnityEngine.Color[] cols_1d = matrix2list(cols_mat, res_x, res_y, marker: "tex");
// A     //    tex.SetPixels(cols_1d);
// A     //    tex.Apply();
// A     //    return tex;
// A     //}
// A 
// A     public Texture2D calc_strain_copy(Texture2D tex_input)
// A     {
// A         // info (paul): calculate e.g. the strain field from the
// A         //      disparities or whatever is most convenient
// A 
// A         Texture2D tex = new Texture2D(tex_input.width, tex_input.height);
// A         int res_x = tex_input.width;
// A         int res_y = tex_input.height;
// A 
// A         // info (paul): getting texture pixels as float-matrix (red colorchannel)
// A         //15052024 Color[] cols = tex.GetPixels(0, 0, res_x, res_y);
// A         //15052024 Color[,] cols_mat = list2matrix(cols, res_x, res_y);
// A         //15052024 List<List<float>> mat = cols_mat2floats_mat(cols_mat);
// A         List<List<float>> mat = tex2mat(tex_input);//15052024 , res_x, res_y);
// A 
// A         // info (paul): Do physics filter, e.g. strain field etc. (mat_1, mat_2 are strain derivatives)
// A         (List<List<float>> mat_1, List<List<float>> mat_2) = find_physics_filter(mat);
// A 
// A         // info (paul): converting back floats-matrix to colors and assign to texture
// A 
// A         // info (paul): choose, whether to display u or v
// A         List<List<float>> mat_displayed = mat_1;
// A         if (strain_d_mode == "x")
// A         {
// A             mat_displayed = mat_1;
// A         }
// A         if (strain_d_mode == "y")
// A         {
// A             mat_displayed = mat_2;
// A         }
// A 
// A         UnityEngine.Color[,] cols_mat = floats2col_mat(mat_2);//22052024 mat_displayed//mat_1
// A         UnityEngine.Color[] cols_1d = matrix2list(cols_mat, res_x, res_y, marker: "tex", with_switch_dims: true);
// A         tex.SetPixels(cols_1d);
// A         tex.Apply();
// A 
// A         return tex;
// A     }
// A 
// A     public Texture2D mat2tex(List<List<float>> mat_1, bool with_switch_dims = false)
// A     {
// A         // perh. TODO: at some point make dim_switch false by default and not true
// A 
// A         int res_x = mat_1.Count;
// A         int res_y = mat_1[0].Count;
// A 
// A         Texture2D tex = new Texture2D(res_x, res_y);
// A         UnityEngine.Color[,] cols_mat = floats2col_mat(mat_1);
// A         UnityEngine.Color[] cols_1d = matrix2list(cols_mat, res_x, res_y, with_switch_dims: with_switch_dims);//15052024 res_x, res_y
// A         tex.SetPixels(cols_1d);
// A         tex.Apply();
// A         return tex;
// A     }
// A 
// A     public Texture2D floats2tex(List<float> floats, int width, int height)
// A     {
// A         Color[] cols = new Color[width * height];
// A 
// A         for (int i = 0; i < floats.Count; i++)
// A         {
// A             cols[i] = new Color(floats[i] / 255f, 0f, 0f, 1f);
// A         }
// A 
// A         Texture2D tex = new Texture2D(width, height);
// A         tex.SetPixels(0, 0, width, height, cols);
// A 
// A         return tex;
// A     }
// A 
// A     public List<float> tex2floats(Texture2D tex, bool with_switch_dims = true, int color_channel = 0)//15052024 , int res_x, int res_y)
// A     {
// A         int res_x = tex.width;
// A         int res_y = tex.height;
// A 
// A         UnityEngine.Color[] cols = tex.GetPixels(0, 0, res_x, res_y);
// A         if (with_switch_dims)
// A         {
// A             cols = switch_dims(cols, res_y, res_x);
// A         }
// A 
// A         // info (paul): cols to floats:
// A         List<float> floats = new List<float>();
// A         for (int i = 0; i < cols.Length; i++)
// A         {
// A             //floats.Add(cols[i].r);
// A             floats.Add(cols[i][color_channel]);
// A         }
// A 
// A         return floats;
// A     }
// A     public List<List<float>> tex2mat(Texture2D tex, bool with_switch_dims = true)//15052024 , int res_x, int res_y)
// A     {
// A         int res_x = tex.width;
// A         int res_y = tex.height;
// A 
// A         UnityEngine.Color[] cols = tex.GetPixels(0, 0, res_x, res_y);
// A         if (with_switch_dims)
// A         {
// A             cols = switch_dims(cols, res_y, res_x);
// A         }
// A 
// A         UnityEngine.Color[,] cols_mat = list2matrix(cols, res_x, res_y);
// A         //float col_sum = sum_cols_r(cols_mat);
// A         //if (col_sum > 0)
// A         //{
// A         //    ;
// A         //}
// A         List<List<float>> mat = cols_mat2floats_mat(cols_mat);
// A         return mat;
// A     }
// A     public float sum_cols_r(UnityEngine.Color[,] cols)
// A     {
// A         float sum = 0f;
// A         for (int i = 0; i < cols.GetLength(0); i++)
// A         {
// A             for (int j = 0; j < cols.GetLength(1); j++)
// A             {
// A                 sum += cols[i, j].r;
// A             }
// A         }
// A 
// A         return sum;
// A     }
// A 
// A     public (List<List<float>>, List<List<float>>) find_physics_filter(List<List<float>> mat)
// A     {
// A         // info (paul): AA AA
// A         List<List<float>> mat_1 = derive_x(mat);
// A         List<List<float>> mat_2 = derive_y(mat);
// A 
// A         // info (paul): norm:
// A         mat_1 = norm_mat(mat_1);
// A         mat_2 = norm_mat(mat_2);
// A 
// A         return (mat_1, mat_2);
// A     }
// A 
// A     public (float, float) find_mean_in_all(List<List<float>> mat, int span = 20, bool only_meaningful = true, float coverage = float.NaN)
// A     {
// A         // info (paul): "only_meaningful" says, that Infinity or NaN values will
// A         //      be excluded from min-max calculation (as it would usually make sense,
// A         //      except you have some special situation perhaps)
// A 
// A         float max_val = -999999f;
// A         float min_val = 999999f;
// A         float sum = 0f;
// A         int cnt = 0;
// A 
// A         int center_x = mat.Count / 2;
// A         int center_y = mat.Count / 2;
// A 
// A         int i_off = 0;
// A         int j_off = 50;
// A         int padding = 20;
// A 
// A         int i_min = 0 + padding;//23092024 center_x + i_off - span;
// A         int i_max = mat.Count - padding;//23092024 center_x + i_off + span;
// A         int j_min = 0 + padding;//23092024 center_y + j_off - span;
// A         int j_max = mat.Count - padding;//23092024 center_y + j_off + span;
// A 
// A         int strange_cnt = 0;
// A 
// A         List<List<float>> plaquette = zeros_of_size(i_max - i_min, j_max - j_min);
// A         for (int i = i_min; i < i_max; i++)
// A         {
// A             for (int j = j_min; j < j_max; j++)
// A             {
// A                 bool use_val = !only_meaningful || (only_meaningful && is_meaningful(mat[i][j]));
// A                 if (use_val)
// A                 {
// A                     float mat_ij = mat[i][j];
// A                     plaquette[i - i_min][j - j_min] = mat[i][j];
// A                     if (Mathf.Abs(mat_ij) > 0.001f)//23092024 (Mathf.Abs(mat_ij) < 2f)
// A                     {
// A                         float mat_ij_abs = Math.Abs(mat[i][j]);
// A                         sum += mat_ij_abs;//Math.Abs(mat[i][j])
// A                         cnt += 1;
// A 
// A                         if (mat_ij_abs > 2f)
// A                         {
// A                             ;
// A                         }
// A                         //mat[i][j] = 1f;//for debugging
// A                     }
// A                     else
// A                     {
// A                         strange_cnt += 1;
// A                     }
// A                 }
// A             }
// A         }
// A 
// A         float mean = sum / ((float)(cnt));
// A         float sq_mean = find_mat_std_2(mat, mean, only_meaningful, i_min, i_max, j_min, j_max);
// A 
// A         // info (paul): take into account the coverage thing
// A         float mean_covered = mean / coverage;
// A         float sq_mean_covered = sq_mean / coverage;
// A 
// A         return (mean_covered, sq_mean_covered);
// A     }
// A     public (float, float) find_mean_in_span(List<List<float>> mat, int span = 20, bool only_meaningful = true, int j_off = 50)
// A     {
// A         // info (paul): "only_meaningful" says, that Infinity or NaN values will
// A         //      be excluded from min-max calculation (as it would usually make sense,
// A         //      except you have some special situation perhaps)
// A 
// A         float max_val = -999999f;
// A         float min_val = 999999f;
// A         float sum = 0f;
// A         int cnt = 0;
// A 
// A         int center_x = mat.Count / 2;
// A         int center_y = mat.Count / 2;
// A 
// A         int i_off = 0;
// A         //int j_off = 50;
// A 
// A         int i_min = center_x + i_off - span;
// A         int i_max = center_x + i_off + span;
// A         int j_min = center_y + j_off - span;
// A         int j_max = center_y + j_off + span;
// A 
// A         int strange_cnt = 0;
// A 
// A         List<List<float>> plaquette = zeros_of_size(i_max - i_min, j_max - j_min);
// A         for (int i = i_min; i < i_max; i++)
// A         {
// A             for (int j = j_min; j < j_max; j++)
// A             {
// A                 bool use_val = !only_meaningful || (only_meaningful && is_meaningful(mat[i][j]));
// A                 if (use_val)
// A                 {
// A                     float mat_ij = mat[i][j];
// A                     plaquette[i - i_min][j - j_min] = mat[i][j];
// A                     if (mat_ij < 2f)
// A                     {
// A                         sum += Math.Abs(mat[i][j]);
// A                         cnt += 1;
// A                         //mat[i][j] = 0.3f;//for debugging
// A                     }
// A                     else
// A                     {
// A                         strange_cnt += 1;
// A                     }
// A                 }
// A             }
// A         }
// A 
// A         float mean = sum / ((float)(cnt));
// A         float sq_mean = find_mat_std_2(mat, mean, only_meaningful, i_min, i_max, j_min, j_max);
// A 
// A         return (mean, sq_mean);
// A     }
// A 
// A     public float find_mat_std_1(List<List<float>> mat, float mean, bool only_meaningful,
// A         int i_min, int i_max, int j_min, int j_max)
// A     {
// A         float sq_sum = 0f;
// A         int cnt = 0;
// A 
// A         for (int i = i_min; i < i_max; i++)
// A         {
// A             for (int j = j_min; j < j_max; j++)
// A             {
// A                 bool use_val = !only_meaningful || (only_meaningful && is_meaningful(mat[i][j]));
// A                 if (use_val)
// A                 {
// A                     // info (paul): We assume that mean is always positive, since during 
// A                     //      its calculation we used mean
// A                     sq_sum += Math.Abs(Mathf.Abs(mat[i][j]) - mean);
// A                     cnt += 1;
// A                 }
// A             }
// A         }
// A 
// A         float sq_mean = sq_sum / ((float)(cnt));
// A         return sq_mean;
// A     }
// A     public float find_mat_std_2(List<List<float>> mat, float mean, bool only_meaningful,
// A     int i_min, int i_max, int j_min, int j_max)
// A     {
// A         float sq_sum = 0f;
// A         int cnt = 0;
// A 
// A         for (int i = i_min; i < i_max; i++)
// A         {
// A             for (int j = j_min; j < j_max; j++)
// A             {
// A                 bool use_val = !only_meaningful || (only_meaningful && is_meaningful(mat[i][j]));
// A                 if (use_val)
// A                 {
// A                     // info (paul): We assume that mean is always positive, since during 
// A                     //      its calculation we used mean
// A                     sq_sum += Mathf.Pow(Mathf.Abs(mat[i][j]) - mean, 2);
// A                     cnt += 1;
// A                 }
// A             }
// A         }
// A 
// A         float sq_mean = sq_sum / ((float)(cnt));
// A         float std = Mathf.Sqrt(sq_mean);
// A         return sq_mean;
// A     }
// A     float find_min(List<float> u, int n_x = -1, int n_y = -1, int offset = 0)
// A     {
// A         List<float> u_taken = u;
// A         if (n_x >= 0 || n_y >= 0)
// A         {
// A             u_taken = u.Skip(offset).Take(n_x * n_y).ToList();
// A         }
// A         float min_val = u_taken.Min();
// A         return min_val;
// A     }
// A 
// A     float find_max(List<float> u, int n_x = -1, int n_y = -1, int offset = 0)
// A     {
// A         List<float> u_taken = u;
// A         if (n_x >= 0 || n_y >= 0)
// A         {
// A             u_taken = u.Skip(offset).Take(n_x * n_y).ToList();
// A         }
// A         float max_val = u_taken.Max();
// A         return max_val;
// A     }
// A     public (float, float) find_min_max(List<float> mat, bool only_meaningful = true)
// A     {
// A         // info (paul): "only_meaningful" says, that Infinity or NaN values will
// A         //      be excluded from min-max calculation (as it would usually make sense,
// A         //      except you have some special situation perhaps)
// A 
// A         float max_val = -999999f;
// A         float min_val = 999999f;
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             float mat_ij = mat[i];
// A             bool use_val = !only_meaningful || (only_meaningful && is_meaningful(mat[i]));
// A             if (use_val)
// A             {
// A                 max_val = Mathf.Max(max_val, mat_ij);
// A             }
// A             if (use_val)
// A             {
// A                 min_val = Mathf.Min(min_val, mat_ij);
// A             }
// A         }
// A 
// A         return (min_val, max_val);
// A     }
// A     public (float, float) find_min_max(List<List<float>> mat, bool only_meaningful = true, bool with_padding = false)
// A     {
// A         // info (paul): "only_meaningful" says, that Infinity or NaN values will
// A         //      be excluded from min-max calculation (as it would usually make sense,
// A         //      except you have some special situation perhaps)
// A 
// A         int padding = 0;
// A         if (with_padding)
// A         {
// A             padding = 10;//25092024
// A         }
// A 
// A         float max_val = -999999f;
// A         float min_val = 999999f;
// A         for (int i = padding; i < mat.Count - padding; i++)
// A         {
// A             for (int j = padding; j < mat[0].Count - padding; j++)
// A             {
// A                 float mat_ij = mat[i][j];
// A                 bool use_val = !only_meaningful || (only_meaningful && is_meaningful(mat[i][j]));
// A                 if (use_val)
// A                 {
// A                     max_val = Mathf.Max(max_val, mat_ij);
// A                 }
// A                 if (use_val)
// A                 {
// A                     min_val = Mathf.Min(min_val, mat_ij);
// A                 }
// A                 //float val = (mat[i][j + 1] - mat[i][j - 1]) / 2f;
// A                 //mat_new[i][j] = val;
// A             }
// A         }
// A 
// A         return (min_val, max_val);
// A     }
// A 
// A     public List<List<float>> unnorm_mat(List<List<float>> mat, float depth_min, float depth_max)
// A     {
// A         // info (paul): kind of the reverse of norming a matrix: We scale it up again to the scale from depth_min to depth_max
// A         //          But: This does only the normal scaling, not this special +/- thing, which norm_mat includes
// A 
// A         List<List<float>> mat_new = mat_like(mat);
// A 
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             for (int j = 0; j < mat[0].Count; j++)
// A             {
// A                 float mat_el = mat[i][j];
// A                 if (i == 256)
// A                 {
// A                     if (j == 256)
// A                     {
// A                         ;
// A                     }
// A                     if (j > 200)
// A                     {
// A                         ;
// A                     }
// A                 }
// A 
// A                 // info (paul): normalization, so that 0 remains 0 and values of the one sign are just cut off
// A                 float mat_el_new = -1f;
// A                 mat_el_new = mat_el * (depth_max - depth_min) + depth_min;
// A                 mat_new[i][j] = mat_el_new;
// A             }
// A         }
// A         return mat_new;
// A     }
// A 
// A     public void aaaaa()
// A     {
// A         ;
// A     }
// A 
// A     public List<List<float>> norm_mat(List<List<float>> mat, float lower = float.NaN,
// A         float upper = float.NaN)
// A     {
// A         // info (paul): lower and upper are a possibility to overwrite min_val and 
// A         //      and max_val and "norm" with respect to custom scale
// A 
// A         List<List<float>> mat_new = mat_like(mat);
// A 
// A         // info (paul): getting min_val and max_val; overwrite if input not NaN
// A         (float min_val, float max_val) = find_min_max(mat, with_padding: true);
// A         if (!float.IsNaN(lower))
// A         {
// A             min_val = lower;
// A         }
// A         if (!float.IsNaN(upper))
// A         {
// A             max_val = upper;
// A         }
// A 
// A         // info (paul): If they are 0, then avoid division-by-zero, 
// A         //      by giving a value
// A         if (max_val == 0f)
// A         {
// A             max_val = 1f;
// A         }
// A         if (min_val == 0f)
// A         {
// A             min_val = 0.1f;//14072024 1f;
// A         }
// A 
// A         //min_val = 0f; // for debugging
// A 
// A         // info (paul): actual norming
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             for (int j = 0; j < mat[0].Count; j++)
// A             {
// A                 float mat_el = mat[i][j];
// A                 //13052024 max_val = Mathf.Max(max_val, mat[i][j]);
// A                 //float val = (mat[i][j + 1] - mat[i][j - 1]) / 2f;
// A 
// A                 // info (paul): normal normalization
// A                 //float mat_el_new = (mat_el - min_val) / (max_val - min_val);
// A                 //mat_new[i][j] = (1 - mat_el_new); // info (paul): Let's just scale it up by sth, for debugging reasons
// A 
// A                 // info (paul): normalization, so that 0 remains 0 and values of the one sign are just cut off
// A                 float mat_el_new = -1f;
// A 
// A                 if (min_val < 0 && max_val > 0)
// A                 {
// A                     // info (paul): If there are positive and neg. values, use this special norming strategy
// A                     if (mat_el >= 0)
// A                     {
// A                         mat_el_new = -(mat_el) / (max_val);
// A                     }
// A                     else
// A                     {
// A                         mat_el_new = -(mat_el) / (-min_val);
// A                     }
// A                 }
// A                 else
// A                 {
// A                     // info (paul): Else, use a more "normal" norming strategy
// A 
// A                     mat_el_new = (mat_el - min_val) / (max_val - min_val);
// A                 }
// A 
// A                 mat_new[i][j] = mat_el_new;
// A             }
// A         }
// A 
// A         return mat_new;
// A     }
// A 
// A     public List<List<float>> mat_like(List<List<float>> mat)
// A     {
// A         List<List<float>> floats = new List<List<float>>();
// A 
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             floats.Add(new List<float>());
// A             for (int j = 0; j < mat[0].Count; j++)
// A             {
// A                 floats[i].Add(0f);
// A             }
// A         }
// A 
// A         return floats;
// A     }
// A 
// A     public List<List<float>> derive_x(List<List<float>> mat)
// A     {
// A         List<List<float>> mat_new = mat_like(mat);
// A 
// A         int res_x = mat.Count;
// A         int res_y = mat[0].Count;
// A 
// A         for (int i = 0; i < res_x; i++)
// A         {
// A             for (int j = 0; j < res_y; j++)
// A             {
// A                 bool j_lower = (j - 1 >= 0);
// A                 bool j_upper = (j + 1 <= mat[0].Count - 1);
// A                 bool j_ok = j_lower && j_upper;
// A 
// A                 if (j_ok)
// A                 {
// A                     float log_val = find_d_x_val(mat, i, j);
// A                     float i_share_2 = (float)(i % 2);
// A                     float i_share = (float)(i) / ((float)res_x);
// A                     float j_share = (float)(j) / ((float)res_y);
// A 
// A                     mat_new[i][j] = log_val;//22052024 j_share;//22052024 log_val;
// A                 }
// A             }
// A         }
// A 
// A         return mat_new;
// A     }
// A 
// A     public List<List<float>> derive_y(List<List<float>> mat)
// A     {
// A         List<List<float>> mat_new = mat_like(mat);
// A 
// A         int res_x = mat.Count;
// A         int res_y = mat[0].Count;
// A 
// A         for (int i = 0; i < res_x; i++)
// A         {
// A             for (int j = 0; j < res_y; j++)
// A             {
// A                 bool i_lower = (i - 1 >= 0);
// A                 bool i_upper = (i + 1 <= mat.Count - 1);
// A                 bool i_ok = i_lower && i_upper;
// A 
// A                 if (i_ok)//22052024 (i_ok)
// A                 {
// A                     float i_share_2 = (float)(i % 2);
// A                     float i_share = (float)(i) / ((float)res_x);
// A                     float j_share = (float)(j) / ((float)res_y);
// A 
// A                     //25052024 mat_new[i][j] = (mat[i + 1][j] - mat[i - 1][j]) / 2f;//i_share;//;//22052024 j_share;//22052024 (mat[i + 1][j] - mat[i - 1][j]) / 2f;
// A                     mat_new[i][j] = find_d_y_val(mat, i, j);
// A 
// A                 }
// A             }
// A         }
// A 
// A         return mat_new;
// A     }
// A     public float find_d_y_val(List<List<float>> mat, int i, int j)
// A     {
// A         float val = (mat[i + 1][j] - mat[i - 1][j]) / 2f;
// A 
// A         // info (paul): take logarithm
// A         //mat_new[i][j] = Mathf.Log(mat_new[i][j]);
// A         float log_val = 0f;
// A         if (val < 0)
// A         {
// A             log_val = -Mathf.Log(-val);//Mathf.Log(-val)
// A         }
// A         else if (val > 0)
// A         {
// A             log_val = Mathf.Log(val);//17052024 0f;
// A         }
// A         else
// A         {
// A             log_val = 0f;
// A         }
// A 
// A         return log_val;
// A     }
// A     public float find_d_x_val(List<List<float>> mat, int i, int j)
// A     {
// A         float val = (mat[i][j + 1] - mat[i][j - 1]) / 2f;
// A 
// A         // info (paul): take logarithm
// A         //mat_new[i][j] = Mathf.Log(mat_new[i][j]);
// A         float log_val = 0f;
// A         if (val < 0)
// A         {
// A             log_val = -Mathf.Log(-val);//Mathf.Log(-val)
// A         }
// A         else if (val > 0)
// A         {
// A             log_val = Mathf.Log(val);//17052024 0f;
// A         }
// A         else
// A         {
// A             log_val = 0f;
// A         }
// A 
// A         return log_val;
// A     }
// A 
// A     public List<List<float>> take_share(List<List<float>> floats)
// A     {
// A         int res_x = floats.Count; //25052024 
// A         int res_y = floats[0].Count; //25052024 
// A 
// A         for (int i = 0; i < res_x; i++)
// A         {
// A             for (int j = 0; j < res_y; j++)
// A             {
// A                 float i_share_2 = (float)(i % 2);
// A                 float i_share = ((float)(i)) / ((float)(res_x));
// A                 float j_share = ((float)(j)) / ((float)(res_y));
// A                 floats[i][j] = i_share_2;
// A             }
// A         }
// A 
// A         return floats;
// A     }
// A 
// A     public UnityEngine.Color[,] floats2col_mat(List<List<float>> floats)
// A     {
// A         int res_x = floats.Count;
// A         int res_y = floats[0].Count;
// A 
// A         UnityEngine.Color[,] col_mat = new UnityEngine.Color[res_x, res_y];
// A 
// A         for (int i = 0; i < res_x; i++)
// A         {
// A             for (int j = 0; j < res_y; j++)
// A             {
// A 
// A                 float floats_ij = floats[i][j];
// A                 if (!is_meaningful(floats_ij))
// A                 {
// A                     floats_ij = floats[i][j];
// A                 }
// A 
// A                 col_mat[i, j].r = floats_ij;
// A                 col_mat[i, j].g = floats_ij;
// A                 col_mat[i, j].b = floats_ij;
// A                 col_mat[i, j].a = 1f;
// A             }
// A         }
// A 
// A         return col_mat;
// A     }
// A 
// A     public List<List<float>> cols_mat2floats_mat(UnityEngine.Color[,] cols_mat)
// A     {
// A         List<List<float>> floats = new List<List<float>>();
// A 
// A         for (int i = 0; i < cols_mat.GetLength(0); i++)
// A         {
// A             floats.Add(new List<float>());
// A             for (int j = 0; j < cols_mat.GetLength(1); j++)
// A             {
// A                 float red_val = cols_mat[i, j].r;
// A                 float green_val = cols_mat[i, j].g;
// A                 float val_chosen = Mathf.Max(red_val, green_val);
// A                 if (green_val > red_val)
// A                 {
// A                     val_chosen = -val_chosen;
// A                 }
// A                 floats[i].Add(val_chosen);
// A             }
// A         }
// A         return floats;
// A     }
// A 
// A     public UnityEngine.Color[,] list2matrix(UnityEngine.Color[] cols, int res_x, int res_y)
// A     {
// A         // info (paul): convert array of colors into matrix of colors (we call it list2...,
// A         //      because of convenience)
// A 
// A         UnityEngine.Color[,] col_mat = new UnityEngine.Color[res_x, res_y];
// A 
// A         for (int i = 0; i < res_x; i++)
// A         {
// A             for (int j = 0; j < res_y; j++)
// A             {
// A                 col_mat[i, j] = cols[i * res_y + j];
// A             }
// A         }
// A 
// A         return col_mat;
// A     }
// A 
// A     public float[][] floats2matrix(float[] cols, int res_x, int res_y, bool with_switch_dims = false)
// A     {
// A         // info (paul): convert array of colors into matrix of colors (we call it list2...,
// A         //      because of convenience)
// A 
// A         //float[,] col_mat = new float[res_x, res_y];
// A         float[][] col_mat = new float[res_x][];
// A         for (int i = 0; i < res_x; i++)
// A         {
// A             col_mat[i] = new float[res_y];
// A         }
// A 
// A         for (int i = 0; i < res_x; i++)
// A         {
// A             for (int j = 0; j < res_y; j++)
// A             {
// A                 col_mat[i][j] = cols[i * res_y + j];
// A             }
// A         }
// A 
// A         if (with_switch_dims)
// A         {
// A             col_mat = switch_dims(col_mat, res_x, res_y);
// A         }
// A 
// A         return col_mat;
// A     }
// A 
// A     public List<List<float>> switch_mat(List<List<float>> mat)
// A     {
// A         // info (paul): switch dims
// A         List<List<float>> mat_1 = copy_mat(mat);
// A 
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             for (int j = 0; j < mat[0].Count; j++)
// A             {
// A                 mat_1[i][j] = mat[j][i];
// A             }
// A         }
// A 
// A         return mat;
// A     }
// A 
// A     public UnityEngine.Color[] matrix2list(UnityEngine.Color[,] col_mat, int res_x, int res_y,
// A         string marker = null, bool with_switch_dims = false)
// A     {
// A         // info (paul): convert array of colors into matrix of colors (we call it list2...,
// A         //      because of convenience)
// A         //
// A         //      marker: just helpful marker for debugging
// A 
// A         //Color[,] col_mat = new Color[res_x, res_y];
// A 
// A         UnityEngine.Color[] cols = new UnityEngine.Color[res_x * res_y];
// A 
// A         res_x = col_mat.GetLength(0);
// A         res_y = col_mat.GetLength(1);
// A 
// A         //22052024debugging int res_y_play = res_y - 72;//ideal: res_y - 72
// A         for (int i = 0; i < res_x; i++)
// A         {
// A             for (int j = 0; j < res_y; j++)
// A             {
// A                 UnityEngine.Color col_l = col_mat[i, j];//[i, j]
// A 
// A                 if (col_l.r >= 0)
// A                 {
// A                     //cols[j * res_y + i] = new Color(col_l.r, 0f, 0f, 1f);//16052024 col_l
// A 
// A                     //22052024debugging //cols[i * res_y_play + j] = new Color(j_share, 0f, 0f, 1f);//verm.22052024 col_l.r//16052024 col_l
// A                     cols[i * res_y + j] = col_l;//22052024 new Color(j_share, 0f, 0f, 1f);
// A                 }
// A                 else
// A                 {
// A                     cols[i * res_y + j] = new UnityEngine.Color(0f, -col_l.r, 0f, 1f);//16052024 col_l
// A                 }
// A 
// A                 if (col_l.r != 0f && !float.IsNaN(col_l.r) && !float.IsInfinity(col_l.r))
// A                 {
// A                     ;
// A                 }
// A             }
// A         }
// A 
// A         if (with_switch_dims)
// A         {
// A             cols = switch_dims(cols, res_x, res_y);
// A         }
// A         return cols;
// A     }
// A     public float[][] switch_dims(float[][] cols, int res_x, int res_y)
// A     {
// A         // info (paul): kind of switch cols dims (cols is 1d but represents
// A         //          the flattened version of a 2d array for a texture)
// A 
// A         float[][] cols_new = new float[res_y][];
// A 
// A         for (int j = 0; j < res_y; j++)
// A         {
// A             cols_new[j] = new float[res_x];
// A 
// A             for (int i = 0; i < res_x; i++)
// A             {
// A                 cols_new[j][res_x - 1 - i] = cols[i][j];
// A             }
// A         }
// A 
// A         return cols_new;
// A     }
// A     public UnityEngine.Color[] switch_dims(UnityEngine.Color[] cols, int res_x, int res_y)
// A     {
// A         // info (paul): kind of switch cols dims (cols is 1d but represents
// A         //          the flattened version of a 2d array for a texture)
// A 
// A         UnityEngine.Color[] cols_new = new UnityEngine.Color[res_x * res_y];
// A 
// A         for (int i = 0; i < res_x; i++)
// A         {
// A             for (int j = 0; j < res_y; j++)
// A             {
// A                 cols_new[j * res_x + i] = cols[i * res_y + j];
// A             }
// A         }
// A 
// A         return cols_new;
// A     }
// A 
// A     public UnityEngine.Color[] matrix2list_copy(UnityEngine.Color[,] col_mat, int res_x, int res_y, string marker = null)
// A     {
// A         // info (paul): convert array of colors into matrix of colors (we call it list2...,
// A         //      because of convenience)
// A         //
// A         //      marker: just helpful marker for debugging
// A 
// A         //Color[,] col_mat = new Color[res_x, res_y];
// A 
// A         UnityEngine.Color[] cols = new UnityEngine.Color[res_x * res_y];
// A 
// A         List<(int, int)> nan_idxs = new List<(int, int)>();
// A 
// A         if (marker == "tex")
// A         {
// A             ;
// A         }
// A 
// A         // info (paul): overwrite matrix for debugging:
// A         /*for (int i = 0; i < res_x; i++)
// A         {
// A             for (int j = 0; j < res_y; j++)
// A             {
// A                 float i_share = ((float)(i)) / ((float)(res_x));
// A                 float j_share = ((float)(j)) / ((float)(res_y));
// A 
// A                 col_mat[i, j] = new Color(i_share, 0f, 0f, 1f);
// A             }
// A         }*/
// A 
// A 
// A 
// A 
// A         //22052024debugging int res_y_play = res_y - 72;//ideal: res_y - 72
// A         for (int i = 0; i < res_y; i++)
// A         {
// A             for (int j = 0; j < res_x; j++)
// A             {
// A                 UnityEngine.Color col_l = col_mat[j, i];//[i, j]
// A                 //17052024 if (col_l.r == 0f)
// A                 //17052024 {
// A                 //17052024     col_l.r = 1f;
// A                 //17052024 }
// A                 //17052024 else if (col_l.r > 0f)
// A                 //17052024 {
// A                 //17052024     col_l.r = 1f;
// A                 //17052024 }
// A                 //17052024 else if (col_l.r < 0f)
// A                 //17052024 {
// A                 //17052024     col_l.r = 1f;
// A                 //17052024 }
// A                 //17052024 else
// A                 //17052024 {
// A                 //17052024     ;//col_l.r = 1f;
// A                 //17052024     nan_idxs.Add((i, j));
// A                 //17052024 }
// A 
// A                 if (col_l.r >= 0)
// A                 {
// A                     //cols[j * res_y + i] = new Color(col_l.r, 0f, 0f, 1f);//16052024 col_l
// A 
// A                     float i_share = ((float)(i)) / ((float)(res_y));
// A                     float j_share = ((float)(j)) / ((float)(res_x));
// A                     //22052024debugging //cols[i * res_y_play + j] = new Color(j_share, 0f, 0f, 1f);//verm.22052024 col_l.r//16052024 col_l
// A                     cols[i * res_x + j] = col_l;//22052024 new Color(j_share, 0f, 0f, 1f);
// A                 }
// A                 else
// A                 {
// A                     cols[j * res_y + i] = new UnityEngine.Color(0f, -col_l.r, 0f, 1f);//16052024 col_l
// A                 }
// A             }
// A         }
// A 
// A         if (marker == "tex")
// A         {
// A             ;
// A         }
// A 
// A         return cols;
// A     }
// A 
// A     public List<List<float>> force_heights(List<List<float>> heights)
// A     {
// A         // info (paul): I assume, that res_x and res_y are just the dimensions of the heights array, right?
// A         res_x = heights.Count;
// A         res_y = heights[0].Count;
// A 
// A         heights = zeros_of_size(res_x, res_y);
// A 
// A         for (int i = 0; i < res_x; i++)
// A         {
// A             for (int j = 0; j < res_y; j++)
// A             {
// A                 heights[i][j] = 0f;
// A             }
// A         }
// A 
// A         return heights;
// A     }
// A 
// A     public (Mesh, List<List<float>>) make_mesh(List<List<float>> heights, bool force_flat = false, float scale_factor = -1f)
// A     {
// A         Mesh mesh = new Mesh();
// A         mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
// A 
// A         if (false)//21062024 (force_flat)
// A         {
// A             heights = force_heights(heights);
// A         }
// A 
// A         int len_x = heights.Count;//08052024 200;
// A         int len_y = heights[0].Count;//08052024 576;
// A 
// A         mesh.vertices = init_verts(heights, len_x: len_x, len_y: len_y,
// A             force_flat: force_flat, scale_factor: scale_factor);
// A         mesh.triangles = init_tris(mesh.vertices, len_x, len_y);
// A         UnityEngine.Vector2[] uvs = init_uvs(mesh, len_x, len_y);
// A 
// A         //A mesh.vertices = new Vector3[] {
// A         //A     Vector3.zero, Vector3.right, Vector3.up
// A         //A };
// A 
// A         //mesh.triangles = new int[] {
// A         //    0, 1, 2
// A         //};
// A 
// A         //A
// A         //Amesh.normals = new Vector3[] {
// A         //A    Vector3.back, Vector3.back, Vector3.back
// A         //A};
// A 
// A         //mesh.uv = new Vector2[] {
// A         //     Vector2.zero, Vector2.right, Vector2.up
// A         //};
// A         mesh.uv = uvs;
// A 
// A         //Amesh.tangents = new Vector4[] {
// A         //A    new Vector4(1f, 0f, 0f, -1f),
// A         //A    new Vector4(1f, 0f, 0f, -1f),
// A         //A    new Vector4(1f, 0f, 0f, -1f)
// A         //A};
// A 
// A         //12042024A // TODO: set up uvs
// A         //12042024A Vector2[] uvs = new Vector2[4];
// A         //12042024A uvs[0] = new Vector2(0, 0);
// A         //12042024A uvs[1] = new Vector2(0, 1);
// A         //12042024A uvs[2] = new Vector2(1, 0);
// A         //12042024A uvs[3] = new Vector2(1, 1);
// A         //12042024A mesh.uv = uvs;
// A         //12042024A 
// A         //12042024A // info (paul): set normals:
// A         //12042024A Vector3[] normals = new Vector3[]{Vector3.up, Vector3.up , Vector3.up , Vector3.up };
// A         //12042024A mesh.normals = normals;
// A 
// A         return (mesh, heights);
// A     }
// A 
// A     public UnityEngine.Vector2[] init_uvs(Mesh mesh, int len_x, int len_y)
// A     {
// A         UnityEngine.Vector2[] uvs = new UnityEngine.Vector2[mesh.vertices.Length];
// A         int counter = 0;
// A 
// A         for (int i = 0; i < len_x + 1; i++)
// A         {
// A             for (int j = 0; j < len_y + 1; j++)
// A             {
// A                 float frac_x = ((float)i) / ((float)len_x);
// A                 float frac_y = ((float)j) / ((float)len_y);
// A                 uvs[counter] = new UnityEngine.Vector2(frac_x, frac_y);
// A                 counter += 1;
// A             }
// A         }
// A 
// A         return uvs;
// A     }
// A 
// A     public int[] init_tris(UnityEngine.Vector3[] verts, int len_x, int len_y)
// A     {
// A         List<int> tris = new List<int>();
// A 
// A         for (int i_idx = 0; i_idx < len_x; i_idx++)
// A         {
// A             for (int j_idx = 0; j_idx < len_y; j_idx++)
// A             {
// A                 // info (paul): first triangle
// A                 int vert_1_idx = i_idx * (len_y + 1) + j_idx;
// A                 int vert_2_idx = (i_idx + 1) * (len_y + 1) + j_idx;
// A                 int vert_3_idx = i_idx * (len_y + 1) + j_idx + 1;
// A 
// A                 tris.Add(vert_3_idx);
// A                 tris.Add(vert_2_idx);
// A                 tris.Add(vert_1_idx);
// A 
// A                 // info (paul): second triangle
// A                 int vert_4_idx = (i_idx + 1) * (len_y + 1) + j_idx;
// A                 int vert_5_idx = (i_idx + 1) * (len_y + 1) + j_idx + 1;
// A                 int vert_6_idx = i_idx * (len_y + 1) + j_idx + 1;
// A 
// A                 tris.Add(vert_6_idx);
// A                 tris.Add(vert_5_idx);
// A                 tris.Add(vert_4_idx);
// A             }
// A         }
// A 
// A         int[] tris_array = tris.ToArray();
// A 
// A         return tris_array;
// A     }
// A 
// A 
// A     public UnityEngine.Vector3[] init_verts(List<List<float>> mat, int len_x = 10, int len_y = 10,
// A         bool force_flat = false, float scale_factor = 1f)
// A     {
// A         UnityEngine.Vector3[,] vecs = new UnityEngine.Vector3[len_x + 1, len_y + 1];
// A 
// A         float d_x = 10f * scale_factor;
// A         float d_z = 10f * scale_factor;
// A 
// A         (float mat_min, float mat_max) = find_max_2d(mat);
// A 
// A         for (int i_idx = 0; i_idx < len_x + 1; i_idx++)
// A         {
// A             for (int j_idx = 0; j_idx < len_y + 1; j_idx++)
// A             {
// A                 // info (paul): get height val
// A                 float height_val = 0f;
// A 
// A                 int off_x = 0;
// A                 int off_y = 0;
// A 
// A                 bool in_bounds_up = (i_idx - off_x < mat.Count) && (j_idx - off_y < mat[0].Count);
// A 
// A                 bool in_bounds_down_x = (i_idx - off_x >= 0);
// A                 bool in_bounds_down_y = (j_idx - off_y >= 0);
// A                 bool in_bounds_down = in_bounds_down_x && in_bounds_down_y;
// A 
// A                 bool in_bounds = in_bounds_down && in_bounds_up;
// A 
// A                 if (in_bounds_down_x)
// A                 {
// A                     ;
// A                 }
// A 
// A                 if (in_bounds_down_y)
// A                 {
// A                     ;
// A                 }
// A 
// A                 if (in_bounds_down)
// A                 {
// A                     ;
// A                 }
// A 
// A                 if (in_bounds_up)
// A                 {
// A                     ;
// A                 }
// A 
// A                 if (in_bounds)
// A                 {
// A                     float height_raw = mat[i_idx - off_x][j_idx - off_y];
// A                     if (height_raw != 0f)
// A                     {
// A                         height_val = (height_raw - mat_min) / (mat_max - mat_min);
// A                         if (height_val > 0f)
// A                         {
// A                             height_val *= 500f;
// A                         }
// A                     }
// A                 }
// A 
// A                 if (float.IsNaN(height_val) || float.IsInfinity(height_val))
// A                 {
// A                     ;
// A                 }
// A 
// A                 if (force_flat)
// A                 {
// A                     height_val = 0f;
// A                 }
// A 
// A                 float pos_x = d_x * (float)i_idx;
// A                 float pos_z = d_z * (float)j_idx;
// A                 vecs[i_idx, j_idx] = new UnityEngine.Vector3(pos_x, height_val, pos_z);
// A                 UnityEngine.Vector3 vec_l = vecs[i_idx, j_idx];
// A 
// A                 //make_sphere_at(vec_l.x, vec_l.y, vec_l.z, size: 5f);
// A             }
// A         }
// A 
// A         UnityEngine.Vector3[] flat = flatten_vec_2d(vecs);
// A 
// A         return flat;
// A     }
// A 
// A     public (float, float) find_max_2d(List<List<float>> matrix)
// A     {
// A         float max_val = -9999999f;
// A         float min_val = 9999999f;
// A 
// A         for (int i = 0; i < matrix.Count; i++)
// A         {
// A             for (int j = 0; j < matrix[0].Count; j++)
// A             {
// A                 float mat_val_l = matrix[i][j];
// A                 min_val = Mathf.Min(min_val, mat_val_l);
// A                 max_val = Mathf.Max(max_val, mat_val_l);
// A             }
// A         }
// A         return (min_val, max_val);
// A     }
// A     public UnityEngine.Vector3[] flatten_vec_2d(UnityEngine.Vector3[,] vecs_2d)
// A     {
// A         int l_x = vecs_2d.GetLength(0);
// A         int l_y = vecs_2d.GetLength(1);
// A 
// A         UnityEngine.Vector3[] flat = new UnityEngine.Vector3[l_x * l_y];
// A         int counter = 0;
// A 
// A         for (int i_idx = 0; i_idx < l_x; i_idx++)
// A         {
// A             for (int j_idx = 0; j_idx < l_y; j_idx++)
// A             {
// A                 flat[counter] = vecs_2d[i_idx, j_idx];
// A                 counter += 1;
// A             }
// A         }
// A 
// A         return flat;
// A     }
// A 
// A     public GameObject make_sphere_at(float x, float y, float z, float size = 1f)
// A     {
// A         //(GameObject sphere_local, _) = but1.build_object(new Vector3(x, y, z), 
// A         //        Quaternion.identity, -1, "Targets/symbols/street_line_symbol");
// A 
// A         GameObject sphere_local = GameObject.CreatePrimitive(PrimitiveType.Sphere);
// A         sphere_local.transform.position = new UnityEngine.Vector3(x, y, z);
// A         sphere_local.transform.localScale = new UnityEngine.Vector3(size, size, size);
// A         sphere_local.name = "sphere_marker";
// A         //12042024 set_layer(sphere_local, 11);
// A         return sphere_local;
// A     }
// A 
// A     public void main_render()
// A     {
// A         // info (paul): Kind of the main function for the rendering of the nakajima samples
// A         //      for validation purposes:
// A 
// A         ; ;
// A     }
// A 
// A     public void load_obj_file()
// A     {
// A         Vector3 pos = new Vector3(blades_pos.x, blades_pos.y + 300f, blades_pos.z + 0f);
// A 
// A         // info (paul): set up two cameras
// A         set_up_cam("cam_0", "cam_prefab_0", pos, angle: -cam_angle);//-10f
// A         set_up_cam("cam_1", "cam_prefab_1", pos, angle: cam_angle);//10f
// A 
// A         set_up_lighting(y_coord: 30f);
// A 
// A         // info (paul): set up blade object
// A         //10062024 GameObject blade_90 = load_blade(cam);
// A     }
// A     public void remove_children(GameObject game_obj)
// A     {
// A         int child_cnt = game_obj.transform.childCount;
// A         for (int i = child_cnt - 1; i >= 0; i--)
// A         {
// A             Transform child = game_obj.transform.GetChild(i);
// A             child.SetParent(null);
// A             Destroy(child.gameObject);
// A         }
// A     }
// A     public void set_up_lighting(float y_coord = 30f, float intensity = 1f)
// A     {
// A         // info (paul): remove lights
// A         GameObject lightings_parent = GameObject.Find("lightings");
// A         remove_children(lightings_parent);
// A 
// A         // info (paul): create new light
// A         GameObject light_obj = new GameObject("light");
// A         if (light_obj.GetComponent<Light>() == null)
// A         {
// A             light_obj.AddComponent<Light>();
// A         }
// A         Light light = light_obj.GetComponent<Light>();
// A         light.type = LightType.Directional;
// A         light.color = new Color(255f / 255f, 244f / 255f, 214f / 255f, 1f);
// A         light.intensity = intensity;
// A         light_obj.transform.position = new Vector3(0f, 100.6f, -162.4f);
// A 
// A         // info (paul): eulerAngles A
// A         light_obj.transform.eulerAngles = new Vector3(50f, y_coord, 0f);
// A         light_obj.transform.SetParent(lightings_parent.transform);
// A 
// A         // info (paul): eulerAngles B
// A         //light_obj.transform.eulerAngles = new Vector3(50f, y_coord, 0f);
// A 
// A         //lightComp.color = Color.blue;
// A         //lightGameObject.transform.position = new Vector3(0, 5, 0);
// A         ;
// A     }
// A 
// A     public Camera set_up_cam(string cam_name, string prefab_name, Vector3 pos, float angle = 0f)
// A     {
// A         GameObject cam_prefab = (GameObject)Resources.Load("Targets/fbx_files/" + prefab_name);
// A         GameObject cam_obj = Instantiate(cam_prefab);
// A         //11062024 cam_obj.transform.position = new UnityEngine.Vector3(-3.1f, 306f, 1f);//10062024 (-3.1f, 106f, 1f);
// A         //11062024 cam_obj.transform.LookAt(new UnityEngine.Vector3(0f, 0f, 0f));
// A         cam_obj.transform.position = pos;//new UnityEngine.Vector3(blades_pos.x, blades_pos.y + 300f, blades_pos.z + 0f);
// A         cam_obj.transform.LookAt(blades_pos);
// A         cam_obj.name = cam_name;
// A 
// A         //cam_obj.transform.RotateAround(blades_pos, Vector3.forward, 0.3f*angle);
// A         cam_obj.transform.RotateAround(blades_pos, Vector3.forward, angle);
// A 
// A         Camera cam = cam_obj.GetComponent<Camera>();
// A         cam.fieldOfView = this.field_of_view; // standard: 60;
// A 
// A         if (cam_name == "cam_0")
// A         {
// A             this.cam_for_uv_0 = cam;
// A         }
// A         if (cam_name == "cam_1")
// A         {
// A             this.cam_for_uv_1 = cam;
// A         }
// A 
// A         return cam;
// A     }
// A 
// A     public void create_other_blades()//init_blades
// A     {
// A         for (int i = blade_idx_min + 1; i < blade_idx_max; i++)
// A         {
// A             string file_path = blade_path_for_idx(i);
// A             GameObject blade_new = load_obj_from_verts(file_path, blade_idx: i, with_uv_init: false);
// A             apply_speckles(blade_new);
// A         }
// A     }
// A 
// A     public void activate_blade(int blade_idx)
// A     {
// A         List<GameObject> blades = collect_blades();
// A 
// A         //11062024 for (int i = 0; i < blades.Count; i++)//blades.Count; i++)
// A         //11062024 {
// A         activate_blade_from(blades, blade_idx: blade_idx);
// A         //11062024 }
// A     }
// A 
// A     public void activate_blade_from(List<GameObject> blades, int blade_idx)
// A     {
// A         // info (paul): deactivate all blades
// A         for (int i = 0; i < blades.Count; i++)
// A         {
// A             GameObject blade = blades[i];
// A             blade.SetActive(false);
// A         }
// A 
// A         // info (paul): activate blade at idx "idx"
// A         blades[blade_idx].SetActive(true);
// A 
// A         // info (paul): take picture:
// A         //if (ready_for_next_blade)
// A         //{
// A         //set_ready_for_next_blade(false);
// A         //}
// A     }
// A 
// A     void take_pic_act()
// A     {
// A         int blade_idx = get_blade_idx();
// A         take_pic(blade_idx, cam_idx_for_pic);
// A 
// A         cam_idx_for_pic += 1;
// A         cam_idx_for_pic = cam_idx_for_pic % 2;
// A     }
// A 
// A     void take_pic(int blade_idx, int cam_idx)
// A     {
// A         Texture2D tex = cam2tex(cam_idx);
// A 
// A         // info (paul): save as png image
// A         save_png(tex, cam_idx, blade_idx);
// A     }
// A 
// A     Stopwatch watch = null;
// A 
// A     public void tik()
// A     {
// A         // info (paul): Start stopwatch
// A         this.watch = new Stopwatch();
// A         this.watch.Start();
// A     }
// A 
// A     public long tok()
// A     {
// A         // info (paul): read out stop watch and restart it
// A         this.watch.Stop();
// A         long ticks = this.watch.ElapsedTicks;
// A         return ticks;
// A     }
// A 
// A     public void take_ref_pic_act()//(int blade_idx, int cam_idx)
// A     {
// A         // info (paul): get params
// A         int blade_idx = get_blade_idx();
// A         int cam_idx = 0;
// A 
// A         // info (paul): assign and activate mesh collider
// A         List<GameObject> blades = collect_blades();
// A         Mesh mesh_l = blades[blade_idx].GetComponent<MeshFilter>().sharedMesh;
// A 
// A         blades[blade_idx].AddComponent<MeshCollider>();
// A         blades[blade_idx].GetComponent<MeshCollider>().sharedMesh = mesh_l;
// A         //blades[cam_idx].GetComponent<MeshCollider>().collider.convex = true;
// A 
// A         Stopwatch watch = new Stopwatch();
// A         watch.Start();
// A         List<List<float>> dists = find_dists(500000000);
// A         watch.Stop();
// A         long secs = watch.ElapsedMilliseconds;
// A 
// A         (float depth_min, float depth_max) = find_min_max(dists, with_padding: true);
// A         List<List<float>> dists_normed = norm_mat(dists);
// A         Texture2D depth_tex = mat2tex(dists_normed, with_switch_dims: true);
// A         save_png(depth_tex, cam_idx, blade_idx, "_depth");
// A 
// A         save_floats_list_2_for_blade(dists_normed, cam_idx, blade_idx, label: "_depth_mat", with_uv_mode: false);//07102024re
// A 
// A         save_float_for_blade(depth_min, cam_idx, blade_idx, label: "_depth_min", with_uv_mode: false);
// A         save_float_for_blade(depth_max, cam_idx, blade_idx, label: "_depth_max", with_uv_mode: false);
// A 
// A         // info (paul): deactivate mesh collider
// A 
// A     }
// A     public void take_ref_pic(int blade_idx, int cam_idx)
// A     {
// A         // info (paul): assign and activate mesh collider
// A         List<GameObject> blades = collect_blades();
// A         Mesh mesh_l = blades[blade_idx].GetComponent<MeshFilter>().sharedMesh;
// A 
// A         blades[blade_idx].AddComponent<MeshCollider>();
// A         blades[blade_idx].GetComponent<MeshCollider>().sharedMesh = mesh_l;
// A         //blades[cam_idx].GetComponent<MeshCollider>().collider.convex = true;
// A 
// A         Stopwatch watch = new Stopwatch();
// A         watch.Start();
// A         List<List<float>> dists = find_dists(500000000);
// A         watch.Stop();
// A         long secs = watch.ElapsedMilliseconds;
// A 
// A         (float depth_min, float depth_max) = find_min_max(dists, with_padding: true);
// A         List<List<float>> dists_normed = norm_mat(dists);
// A         Texture2D depth_tex = mat2tex(dists_normed, with_switch_dims: true);
// A         save_png(depth_tex, cam_idx, blade_idx, "_depth");
// A 
// A         //15072024 save_floats_list_2_for_blade(dists_normed, cam_idx, blade_idx, label: "_depth_mat", with_uv_mode: false);
// A 
// A         save_float_for_blade(depth_min, cam_idx, blade_idx, label: "_depth_min", with_uv_mode: false);
// A         save_float_for_blade(depth_max, cam_idx, blade_idx, label: "_depth_max", with_uv_mode: false);
// A 
// A         // info (paul): deactivate mesh collider
// A 
// A     }
// A     public void save_float_for_blade(float value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
// A     {
// A         string path = construct_blade_path(cam_idx, blade_idx, label, type: "float", with_uv_mode: with_uv_mode);
// A         save_float(value, full_path: path);
// A     }
// A     public float[] load_floats_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
// A     {
// A         string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats", with_uv_mode: with_uv_mode);
// A         float[] value = load_floats(full_path: path);
// A         return value;
// A     }
// A     public int[] load_ints_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
// A     {
// A         string path = construct_blade_path(cam_idx, blade_idx, label, type: "ints", with_uv_mode: with_uv_mode);
// A         int[] value = load_ints(full_path: path);
// A         return value;
// A     }
// A     public int[,] load_ints2_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
// A     {
// A         string path = construct_blade_path(cam_idx, blade_idx, label, type: "ints2", with_uv_mode: with_uv_mode);
// A         int[,] value = load_ints2(full_path: path);
// A         if (value == null)
// A         {
// A             ;
// A         }
// A         return value;
// A     }
// A 
// A     public float[][] lists_to_floats2(List<List<float>> lists)
// A     {
// A         float[][] floats = new float[lists.Count][];
// A         for (int i = 0; i < lists.Count; i++)
// A         {
// A             floats[i] = lists[i].ToArray();
// A         }
// A 
// A         return floats;
// A     }
// A     public List<List<float>> floats2_to_lists(float[][] lists)
// A     {
// A         List<List<float>> floats = new List<List<float>>();
// A         for (int i = 0; i < lists.Length; i++)
// A         {
// A             floats.Add(new List<float>());
// A             floats[i] = lists[i].ToList();
// A         }
// A 
// A         return floats;
// A     }
// A 
// A 
// A     public void save_floats_for_blade(float[] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
// A     {
// A         string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats", with_uv_mode: with_uv_mode);
// A         save_floats(value, full_path: path);
// A     }
// A     public void save_floats_list_2_for_blade(List<List<float>> lists, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
// A     {
// A         float[][] value = lists_to_floats2(lists);
// A         save_floats2_for_blade(value, cam_idx, blade_idx, label, with_uv_mode);
// A     }
// A     public void save_floats2_for_blade(float[][] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
// A     {
// A         string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats2", with_uv_mode: with_uv_mode);
// A         save_floats2(value, full_path: path);
// A     }
// A     public void save_ints_for_blade(int[] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
// A     {
// A         string path = construct_blade_path(cam_idx, blade_idx, label, type: "ints", with_uv_mode: with_uv_mode);
// A         save_ints(value, full_path: path);
// A     }
// A     public void save_ints2_for_blade(int[,] value, int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
// A     {
// A         string path = construct_blade_path(cam_idx, blade_idx, label, type: "ints2", with_uv_mode: with_uv_mode);
// A         save_ints2(value, full_path: path);
// A     }
// A     public float load_float_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
// A     {
// A         string path = construct_blade_path(cam_idx, blade_idx, label, type: "float", with_uv_mode: with_uv_mode);
// A         float value = load_float(full_path: path);
// A         return value;
// A     }
// A     public List<List<float>> load_floats_list_2_for_blade(int cam_idx, int blade_idx, string label = "", bool with_uv_mode = false)
// A     {
// A         //float[][] value = lists2floats2(lists);
// A         string path = construct_blade_path(cam_idx, blade_idx, label, type: "floats2", experiment: "exp_normal", with_uv_mode: with_uv_mode);
// A         float[][] floats = load_floats2(full_path: path);
// A         List<List<float>> lists = floats2_to_lists(floats);
// A         return lists;
// A     }
// A 
// A 
// A     public void save_float(float value, string file_name = null, string full_path = null)
// A     {
// A         string path = null;
// A         if (file_name != null)
// A         {
// A             path = Application.persistentDataPath + "/" + file_name;
// A         }
// A         if (full_path != null)
// A         {
// A             path = full_path;
// A         }
// A         if (true)//(File.Exists(path))
// A         {
// A             BinaryFormatter formatter = new BinaryFormatter();
// A             FileStream stream = new FileStream(path, FileMode.Create);
// A 
// A             formatter.Serialize(stream, value);
// A             stream.Close();
// A         }
// A     }
// A 
// A     public void save_floats(float[] value, string file_name = null, string full_path = null)
// A     {
// A         string path = null;
// A         if (file_name != null)
// A         {
// A             path = Application.persistentDataPath + "/" + file_name;
// A         }
// A         if (full_path != null)
// A         {
// A             path = full_path;
// A         }
// A         if (true)//(File.Exists(path))
// A         {
// A             BinaryFormatter formatter = new BinaryFormatter();
// A             FileStream stream = new FileStream(path, FileMode.Create);
// A 
// A             formatter.Serialize(stream, value);
// A             stream.Close();
// A         }
// A     }
// A     public void save_floats2(float[][] value, string file_name = null, string full_path = null)
// A     {
// A         string path = null;
// A         if (file_name != null)
// A         {
// A             path = Application.persistentDataPath + "/" + file_name;
// A         }
// A         if (full_path != null)
// A         {
// A             path = full_path;
// A         }
// A         if (true)//(File.Exists(path))
// A         {
// A             BinaryFormatter formatter = new BinaryFormatter();
// A             FileStream stream = new FileStream(path, FileMode.Create);
// A 
// A             formatter.Serialize(stream, value);
// A             stream.Close();
// A         }
// A     }
// A 
// A     public void save_ints(int[] value, string file_name = null, string full_path = null)
// A     {
// A         string path = null;
// A         if (file_name != null)
// A         {
// A             path = Application.persistentDataPath + "/" + file_name;
// A         }
// A         if (full_path != null)
// A         {
// A             path = full_path;
// A         }
// A         if (true)//(File.Exists(path))
// A         {
// A             BinaryFormatter formatter = new BinaryFormatter();
// A             FileStream stream = new FileStream(path, FileMode.Create);
// A 
// A             formatter.Serialize(stream, value);
// A             stream.Close();
// A         }
// A     }
// A     public void save_ints2(int[,] value, string file_name = null, string full_path = null)
// A     {
// A         string path = null;
// A         if (file_name != null)
// A         {
// A             path = Application.persistentDataPath + "/" + file_name;
// A         }
// A         if (full_path != null)
// A         {
// A             path = full_path;
// A         }
// A         if (true)//(File.Exists(path))
// A         {
// A             BinaryFormatter formatter = new BinaryFormatter();
// A             FileStream stream = new FileStream(path, FileMode.Create);
// A 
// A             formatter.Serialize(stream, value);
// A             stream.Close();
// A         }
// A     }
// A 
// A     public float load_float(string file_name = null, string full_path = null)
// A     {
// A         string path = null;
// A         if (file_name != null)
// A         {
// A             path = Application.persistentDataPath + "/" + file_name;
// A         }
// A         if (full_path != null)
// A         {
// A             path = full_path;
// A         }
// A 
// A         float data = float.NaN;
// A         if (File.Exists(path))
// A         {
// A             BinaryFormatter formatter = new BinaryFormatter();
// A             FileStream stream = new FileStream(path, FileMode.Open);
// A 
// A             data = (float)formatter.Deserialize(stream);
// A             stream.Close();
// A         }
// A         else
// A         {
// A             data = 0f; // or whatever a good default value is
// A         }
// A 
// A         return data;
// A     }
// A     public float[][] load_floats2(string file_name = null, string full_path = null)
// A     {
// A         string path = null;
// A         if (file_name != null)
// A         {
// A             path = Application.persistentDataPath + "/" + file_name;
// A         }
// A         if (full_path != null)
// A         {
// A             path = full_path;
// A         }
// A 
// A         float[][] data = null;
// A         if (File.Exists(path))
// A         {
// A             BinaryFormatter formatter = new BinaryFormatter();
// A             FileStream stream = new FileStream(path, FileMode.Open);
// A 
// A             data = (float[][])formatter.Deserialize(stream);
// A             stream.Close();
// A         }
// A         else
// A         {
// A             data = null; // or whatever a good default value is
// A         }
// A 
// A         return data;
// A     }
// A 
// A     public string load_txt_line(string file_name)
// A     {
// A         //File file = null;
// A         StreamReader inp_stm = null;
// A         try
// A         {
// A             inp_stm = new StreamReader(file_name);
// A         }
// A         catch
// A         {
// A             inp_stm = new StreamReader(file_name);
// A         }
// A         string line = inp_stm.ReadLine();
// A 
// A         return line;
// A     }
// A 
// A     public float[] load_floats(string file_name = null, string full_path = null)
// A     {
// A         string path = null;
// A         if (file_name != null)
// A         {
// A             path = Application.persistentDataPath + "/" + file_name;
// A         }
// A         if (full_path != null)
// A         {
// A             path = full_path;
// A         }
// A 
// A         float[] data = null;
// A         if (File.Exists(path))
// A         {
// A             BinaryFormatter formatter = new BinaryFormatter();
// A             FileStream stream = new FileStream(path, FileMode.Open);
// A 
// A             data = (float[])formatter.Deserialize(stream);
// A             stream.Close();
// A         }
// A         else
// A         {
// A             data = null; // or whatever a good default value is
// A         }
// A 
// A         return data;
// A     }
// A     public int[] load_ints(string file_name = null, string full_path = null)
// A     {
// A         string path = null;
// A         if (file_name != null)
// A         {
// A             path = Application.persistentDataPath + "/" + file_name;
// A         }
// A         if (full_path != null)
// A         {
// A             path = full_path;
// A         }
// A 
// A         int[] data = null;
// A         if (File.Exists(path))
// A         {
// A             BinaryFormatter formatter = new BinaryFormatter();
// A             FileStream stream = new FileStream(path, FileMode.Open);
// A 
// A             data = (int[])formatter.Deserialize(stream);
// A             stream.Close();
// A         }
// A         else
// A         {
// A             data = null; // or whatever a good default value is
// A         }
// A 
// A         return data;
// A     }
// A     public int[,] load_ints2(string file_name = null, string full_path = null)
// A     {
// A         string path = null;
// A         if (file_name != null)
// A         {
// A             path = Application.persistentDataPath + "/" + file_name;
// A         }
// A         if (full_path != null)
// A         {
// A             path = full_path;
// A         }
// A 
// A         int[,] data = null;
// A         if (File.Exists(path))
// A         {
// A             BinaryFormatter formatter = new BinaryFormatter();
// A             FileStream stream = new FileStream(path, FileMode.Open);
// A 
// A             data = (int[,])formatter.Deserialize(stream);
// A             stream.Close();
// A         }
// A         else
// A         {
// A             data = null; // or whatever a good default value is
// A         }
// A 
// A         return data;
// A     }
// A     public List<List<float>> find_dists(int curb = 99999999)
// A     {
// A         // info (paul): curb is to curb the number of operations, in case 
// A         //          that the resolution is high, so that for debugging 
// A         //          unity does not get stuck in an almost indefinite loop
// A 
// A         int width = cam_for_uv_0.pixelWidth;
// A         int height = cam_for_uv_1.pixelHeight;
// A 
// A         List<List<float>> dists = zeros_of_size(len_x: width, len_y: height);
// A 
// A         for (int i = 0; i < width; i++)
// A         {
// A             for (int j = 0; j < height; j++)
// A             {
// A                 if (curb > 0)
// A                 {
// A                     Ray ray_ij = cam_for_uv_0.ScreenPointToRay(new Vector3(i, j));
// A 
// A                     RaycastHit hit;
// A                     bool has_hit = Physics.Raycast(cam_for_uv_0.transform.position, ray_ij.direction, out hit, Mathf.Infinity);
// A                     if (has_hit)
// A                     {
// A                         dists[i][j] = hit.distance;
// A                     }
// A                     curb -= 1;
// A                 }
// A             }
// A         }
// A 
// A         return dists;
// A     }
// A 
// A     public Texture2D cam2tex(int cam_idx)
// A     {
// A         int our_height = this.render_res;//11062024 256
// A         // info (paul): crate render_tex
// A         RenderTexture render_tex = null;
// A 
// A         if (cam_idx == 0)
// A         {
// A             render_tex = cam_for_uv_0.targetTexture;
// A         }
// A         if (cam_idx == 1)
// A         {
// A             render_tex = cam_for_uv_1.targetTexture;
// A         }
// A 
// A         // info (paul): create texture2D
// A         Texture2D tex = new Texture2D(our_height, our_height);
// A         RenderTexture.active = render_tex;
// A         tex.ReadPixels(new Rect(0, 0, our_height, our_height), 0, 0);
// A         tex.Apply();
// A         return tex;
// A     }
// A 
// A     public string get_experiment()
// A     {
// A         return this.experiment;
// A     }
// A     public void set_experiment(string input)
// A     {
// A         if (input == null)
// A         {
// A             ;
// A         }
// A         if (input == "exp_normal")
// A         {
// A             ;
// A         }
// A 
// A         this.experiment = input;
// A     }
// A 
// A     public string construct_blade_path(int cam_idx, int blade_idx, string label,
// A         string type = ".png", string experiment = null, bool with_uv_mode = false)
// A     {
// A         //25062024 string path = Application.persistentDataPath + "/blade_" + blade_idx.ToString() + "_cam_" + cam_idx.ToString() + label + type;
// A 
// A         //03072024 string dir_path = Application.persistentDataPath + "/" + this.get_experiment().ToString() + "/cam_" + cam_idx + "/";
// A         //26072024 string dir_path = "C:/Users/go73jem/Desktop/DIC_package/" + this.get_experiment().ToString() + "/cam_" + cam_idx + "/";
// A         //05092024 string dir_path = "C:/Users/go73jem/Desktop/DIC_package/" + "exp_normal" + "/cam_" + cam_idx + "/";
// A 
// A         if (experiment == null)
// A         {
// A             experiment = remove_dots(get_experiment());
// A         }
// A 
// A         string dir_path = "C:/Users/go73jem/Desktop/DIC_package/" + experiment + "/cam_" + cam_idx + "/";
// A 
// A         if (with_uv_mode)
// A         {
// A             dir_path += (this.paint_with + "/");
// A             //uv_mode = "uv";
// A             //uv_mode = "heights";
// A         }
// A         if (type == "float" || type == "floats" || type == "floats2" || type == "ints2")
// A         {
// A             dir_path += (type + "/");
// A         }
// A 
// A         System.IO.Directory.CreateDirectory(dir_path);
// A         string path = dir_path + blade_idx.ToString() + "_" + label + type;
// A 
// A         return path;
// A     }
// A     public void save_png(Texture2D tex, int cam_idx, int blade_idx, string label = "")
// A     {
// A         // info (paul): "label" is sth, that you can add, to give a special name
// A 
// A         //16062024 string path_l = Application.persistentDataPath + "/blade_" + blade_idx.ToString() + "_cam_" + cam_idx.ToString() + label + ".png";
// A 
// A         // info (paul): save in the DIC-package directory
// A         if (label == "")
// A         {
// A             write_in_dic(tex, cam_idx, blade_idx);
// A         }
// A 
// A         // info (paul): save in the unity directory
// A         //25062024 string path_l = construct_blade_path(cam_idx, blade_idx, label);
// A         string path_l = construct_blade_path(cam_idx, blade_idx, label, with_uv_mode: true);
// A         System.IO.File.WriteAllBytes(path_l, tex.EncodeToPNG());
// A 
// A         string blade_info = "saved blade under: " + path_l;
// A         if (with_print_paths)
// A         {
// A             Debug.Log(blade_info);
// A         }
// A         set_ready_for_next_blade(true);
// A     }
// A 
// A     public string choose_dir(int cam_idx)
// A     {
// A         string dir_l = "C:/Users/go73jem/Desktop/DIC_package/left";
// A         string dir_r = "C:/Users/go73jem/Desktop/DIC_package/right";
// A         string dir_chosen = null;
// A         if (cam_idx == 0)
// A         {
// A             dir_chosen = dir_l;
// A         }
// A         if (cam_idx == 1)
// A         {
// A             dir_chosen = dir_r;
// A         }
// A         return dir_chosen;
// A     }
// A 
// A     public void write_in_dic(Texture2D tex, int cam_idx, int blade_idx)
// A     {
// A         //02072024 string dir_chosen = choose_dir(cam_idx);
// A 
// A         // info (paul): save png under path
// A         string dir_path = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(this.get_experiment().ToString()) + "/cam_" + cam_idx + "/uv/";
// A         if (this.get_experiment() != "exp_normal")
// A         {
// A             ;
// A         }
// A         string path_png = dig_path(dir_path, blade_idx);
// A         System.IO.File.WriteAllBytes(path_png, tex.EncodeToPNG());
// A 
// A         // info (paul): convert to .tif via Python file
// A         //30082024 convert_to_tif();
// A     }
// A 
// A     public string dig_path(string dir_path, int blade_idx)
// A     {
// A         //application.persistentDataPath
// A         bool dir_exists = Directory.Exists(dir_path);
// A         if (!dir_exists)
// A         {
// A             System.IO.Directory.CreateDirectory(dir_path);
// A             bool dir_exists_test = Directory.Exists(dir_path);
// A         }
// A         string path_png = dir_path + "im_" + blade_idx.ToString() + ".png";
// A         return path_png;
// A     }
// A 
// A     public void convert_to_tif()
// A     {
// A         Process proc = new Process();
// A         ProcessStartInfo start = new ProcessStartInfo();
// A         start.FileName = "png2tiff.py";//"script_paul.py";
// A 
// A         Process.Start("python", "Assets/png2tiff.py").WaitForExit();
// A 
// A     }
// A 
// A     public void take_pic_old()
// A     {
// A         int our_height = 256;
// A 
// A         // info (paul): crate render_tex
// A         //10062024 RenderTexture render_tex = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
// A         //10062024 render_tex.Create();
// A         //10062024 cam_for_uv.Render();
// A         //10062024 
// A         //10062024 
// A         //10062024 cam_for_uv.targetTexture = render_tex;
// A         //10062024 
// A         //10062024 
// A         RenderTexture render_tex = cam_for_uv_0.targetTexture;//12062024 cam_for_uv.targetTexture;
// A 
// A         // info (paul): create texture2D
// A         Texture2D tex = new Texture2D(our_height, our_height);
// A         //RenderTexture.active = render_tex;
// A         tex.ReadPixels(new Rect(0, 0, render_tex.width, render_tex.height), 0, 0);
// A         tex.Apply();
// A 
// A 
// A         // info (paul): render to tex
// A         //cam_for_uv.Render();
// A 
// A 
// A         // info (paul): save as png image
// A 
// A         string path_l = Application.persistentDataPath + "/ABC.png";
// A         File.WriteAllBytes(path_l, tex.EncodeToPNG());
// A 
// A         // info (paul): release render_tex
// A         render_tex.Release();
// A     }
// A     public List<GameObject> collect_blades()
// A     {
// A         GameObject blades = GameObject.Find("blades");
// A         int child_cnt = blades.transform.childCount;
// A         List<GameObject> children = new List<GameObject>();
// A 
// A         for (int i = 0; i < child_cnt; i++)
// A         {
// A             Transform child = blades.transform.GetChild(i);
// A             children.Add(child.gameObject);
// A         }
// A 
// A         return children;
// A     }
// A 
// A     public Mesh load_mesh_from_verts(string blade_path)
// A     {
// A         string[][] verts_strs = load_vert_strings(blade_path: blade_path);
// A         float[][] verts_coords = strs2floats(verts_strs, blade_idx: get_blade_idx());
// A 
// A         // info (paul): set up the mesh
// A         Mesh mesh = new Mesh();
// A         mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
// A         mesh.vertices = init_verts_from_coords(verts_coords);
// A         mesh.triangles = tris_from_coords(verts_coords);
// A         return mesh;
// A     }
// A 
// A     public GameObject load_obj_from_verts(string blade_path, int blade_idx = -1, bool with_uv_init = false)
// A     {
// A         Mesh mesh = load_mesh_from_verts(blade_path);
// A 
// A         // info (paul): create the object
// A         string obj_name = "verts_mesh_" + blade_idx.ToString();
// A         GameObject surface_obj = setup_surface_obj(mesh, obj_name: obj_name);
// A         surface_obj.transform.RotateAround(new Vector3(),
// A              new Vector3(1f, 0f, 0f), angle: -90f);
// A         surface_obj.transform.position = blades_pos;
// A 
// A         // info (paul): set parent
// A         if (blade_idx != -1)
// A         {
// A             GameObject blades = GameObject.Find("blades");
// A             surface_obj.transform.SetParent(blades.transform);
// A         }
// A 
// A         // info (paul): set uvs
// A         if (with_uv_init)
// A         {
// A             surface_obj.AddComponent<Vis_action>();
// A         }
// A         else
// A         {
// A             surface_obj.GetComponent<MeshFilter>().mesh.uv = uv_start;
// A         }
// A 
// A         return surface_obj;
// A     }
// A 
// A     public int[] tris_from_coords(float[][] verts)
// A     {
// A         int scale_fac = 1;//08102024 3;
// A         int[] tris = new int[scale_fac * verts.Length];
// A 
// A         for (int i = 0; i < verts.Length; i++)
// A         {
// A             // info (paul): As you see, it is the super simple version
// A             tris[i] = i;
// A         }
// A 
// A         return tris;
// A     }
// A 
// A     public Vector3[] init_verts_from_coords(float[][] verts_coords)
// A     {
// A         Vector3[] vecs = new Vector3[verts_coords.Length];
// A 
// A         for (int i = 0; i < verts_coords.Length; i++)
// A         {
// A             float[] coords = verts_coords[i];
// A             //20062024 Vector3 vec = new Vector3(coords[0], coords[1], coords[2]);
// A             Vector3 vec = new Vector3(coords[0], coords[1], coords[2]);
// A             vecs[i] = vec;
// A         }
// A 
// A         return vecs;
// A     }
// A 
// A     public float[][] strs2floats(string[][] verts_strs, int blade_idx = -1)
// A     {
// A         float[][] verts_cos = new float[verts_strs.Length][];
// A 
// A         for (int i = 0; i < verts_strs.Length; i++)
// A         {
// A             verts_cos[i] = new float[verts_strs[i].Length];
// A 
// A             for (int j = 0; j < verts_strs[i].Length; j++)
// A             {
// A                 string str_l = verts_strs[i][j];
// A                 float float_l = float.Parse(str_l);
// A 
// A                 verts_cos[i][j] = float_l;
// A             }
// A 
// A             verts_cos[i][0] += (float)0f * (blade_idx - this.blade_idx_min);
// A         }
// A 
// A         ;
// A 
// A         return verts_cos;
// A     }
// A 
// A     public string[][] load_vert_strings(string blade_path)
// A     {
// A         //string file_path = "C:/Users/go73jem/Desktop/play_blender_pycahrm/write_mesh/verts_92.txt";
// A 
// A         StreamReader inp_stm = new StreamReader(blade_path);
// A         List<string[]> lines = new List<string[]>();
// A 
// A         while (!inp_stm.EndOfStream)
// A         {
// A             string inp_ln = inp_stm.ReadLine();
// A             string[] splits = inp_ln.Split(" ");
// A             lines.Add(splits);
// A 
// A             // Do Something with the input. 
// A         }
// A 
// A         inp_stm.Close();
// A         return lines.ToArray();
// A     }
// A 
// A     public double hypot(double x, double y)
// A     {
// A         double result = Math.Sqrt(x * x + y * y);
// A         return result;
// A     }
// A 
// A     // info (paul): OpenCV TV part
// A     /**
// A     *
// A     * Function to compute the optical flow in one scale
// A     *
// A     **/
// A 
// A     int MAX_ITERATIONS = 300;//1800;//900;//27092024 300;
// A     double PRESMOOTHING_SIGMA = 0.8d;
// A     double GRAD_IS_ZERO = 1E-10d;//1E-10d;
// A 
// A     void Dual_TVL1_optic_flow(
// A             List<float> I0,           // source image
// A             List<float> I1,           // target image
// A             List<float> u1,           // x component of the optical flow
// A             List<float> u2,           // y component of the optical flow
// A             int nx,      // image width
// A             int ny,      // image height
// A             float tau,     // time step
// A             float lambda,  // weight parameter for the data term
// A             float theta,   // weight parameter for (u - v)²
// A             int warps,   // number of warpings per scale
// A             float epsilon, // tolerance for numerical convergence
// A             bool verbose  // enable/disable the verbose mode
// A         )
// A     {
// A 
// A         int size = nx * ny;
// A         float l_t = lambda * theta;
// A 
// A         List<float> I1x = zeros_of_size(size);
// A         List<float> I1y = zeros_of_size(size);
// A         List<float> I1w = zeros_of_size(size);
// A         List<float> I1wx = zeros_of_size(size);
// A         List<float> I1wy = zeros_of_size(size);
// A 
// A         List<float> rho_b = zeros_of_size(size);
// A         List<float> rho_c = zeros_of_size(size);
// A         List<float> rho_d = zeros_of_size(size);
// A         List<float> rho_e = zeros_of_size(size);
// A         List<float> rho_f = zeros_of_size(size);
// A         List<float> rho_g = zeros_of_size(size);
// A 
// A         List<float> v1 = zeros_of_size(size);
// A         List<float> v2 = zeros_of_size(size);
// A         List<float> p11 = zeros_of_size(size);
// A         List<float> p12 = zeros_of_size(size);
// A         List<float> p21 = zeros_of_size(size);
// A         List<float> p22 = zeros_of_size(size);
// A         List<float> div = zeros_of_size(size);
// A         List<float> grad = zeros_of_size(size);
// A         List<float> div_p1 = zeros_of_size(size);
// A         List<float> div_p2 = zeros_of_size(size);
// A         List<float> u1x = zeros_of_size(size);
// A         List<float> u1y = zeros_of_size(size);
// A         List<float> u2x = zeros_of_size(size);
// A         List<float> u2y = zeros_of_size(size);
// A 
// A         // info (paul): debugging quantities
// A         List<float> d2_debug = zeros_of_size(size);
// A         List<float> fi_debug = zeros_of_size(size);
// A         List<float> rho_debug = zeros_of_size(size);
// A         List<float> decs_debug = zeros_of_size(size);
// A 
// A         centered_gradient(I1, I1x, I1y, nx, ny);
// A 
// A         // for debugging reasons:
// A         //29082024 for (int i = 0; i < I1.Count; i++)
// A         //29082024 {
// A         //29082024     I1[i] = i;
// A         //29082024 }
// A 
// A         // initialization of p
// A         for (int i = 0; i < size; i++)
// A         {
// A             p11[i] = 0f;
// A             p12[i] = 0f;
// A             p21[i] = 0f;
// A             p22[i] = 0f;
// A         }
// A 
// A         for (int warpings = 0; warpings < warps; warpings++)
// A         {
// A             if (warpings == warps - 1)
// A             {
// A                 ;
// A             }
// A             // compute the warping of the target image and its derivatives
// A             bicubic_interpolation_warp(I1, u1, u2, I1w, nx, ny, true);
// A             bicubic_interpolation_warp(I1x, u1, u2, I1wx, nx, ny, true);
// A             bicubic_interpolation_warp(I1y, u1, u2, I1wy, nx, ny, true);
// A 
// A             //write_for_debug(u2, with_norm: false);
// A 
// A             for (int i = 0; i < size; i++)
// A             {
// A                 float Ix2 = I1wx[i] * I1wx[i];
// A                 float Iy2 = I1wy[i] * I1wy[i];
// A 
// A                 // store the |Grad(I1)|^2
// A                 grad[i] = (Ix2 + Iy2);
// A 
// A                 // compute the constant part of the rho function
// A                 rho_c[i] = (I1w[i] - I1wx[i] * u1[i]
// A                     - I1wy[i] * u2[i] - I0[i]);
// A                 rho_g[i] = (I1w[i] - I0[i]);
// A                 rho_b[i] = (I1w[i] - I1wx[i] * u1[i]);
// A                 rho_d[i] = (I1w[i]);
// A                 rho_e[i] = (I1wx[i] * u1[i]);
// A                 rho_f[i] = (I1wx[i]);
// A 
// A                 byte[] bytes_l = BitConverter.GetBytes(rho_c[i]);
// A             }
// A 
// A             int n = 0;
// A             float error = Mathf.Infinity;
// A             float eps_sq = epsilon * epsilon;
// A             while (error > eps_sq && n < MAX_ITERATIONS)
// A             {
// A                 n++;
// A 
// A                 (error, p11, p12, p21, p22, u1, u2, v1, v2) = inner_optim_step(rho_c,
// A                     size, u1, u2, I1wx, I1wy, rho_debug, grad, l_t, warpings, fi_debug,
// A                     decs_debug, v1, v2, d2_debug, div_p1, div_p2, nx, ny, theta, p11, p12,
// A                     p21, p22, error, u1x, u1y, u2x, u2y, tau);
// A             }
// A 
// A             if (verbose)
// A             {
// A                 //27072024 string log_str = stderr + "Warping: " + warpings.ToString() + "Iterations: %d, " + n.ToString() + "Error: %f\n" + error;
// A                 string log_str = "stderr";
// A             }
// A         }
// A     }
// A 
// A     public (float, List<float>, List<float>, List<float>,
// A         List<float>, List<float>, List<float>, List<float>, List<float>
// A         ) inner_optim_step(
// A         List<float> rho_c, int size, List<float> u1, List<float> u2, List<float> I1wx,
// A         List<float> I1wy, List<float> rho_debug, List<float> grad, float l_t,
// A         int warpings, List<float> fi_debug, List<float> decs_debug, List<float> v1,
// A         List<float> v2, List<float> d2_debug, List<float> div_p1, List<float> div_p2,
// A         int nx, int ny, float theta, List<float> p11, List<float> p12, List<float> p21, List<float> p22,
// A         float error, List<float> u1x, List<float> u1y, List<float> u2x, List<float> u2y, float tau)
// A     {
// A         for (int i = 0; i < size; i++)
// A         {
// A             float rho = rho_c[i]
// A                 + (I1wx[i] * u1[i] + I1wy[i] * u2[i]);
// A             rho_debug[i] = rho;
// A 
// A             float d1, d2;
// A             float grad_i = grad[i];
// A             if (rho < -l_t * grad_i)
// A             {
// A                 d1 = l_t * I1wx[i];
// A                 d2 = l_t * I1wy[i];
// A                 decs_debug[i] = 0f;
// A             }
// A             else
// A             {
// A                 if (rho > l_t * grad_i)
// A                 {
// A                     d1 = -l_t * I1wx[i];
// A                     d2 = -l_t * I1wy[i];
// A                     decs_debug[i] = 0.5f;
// A                 }
// A                 else
// A                 {
// A                     if (grad_i < GRAD_IS_ZERO && warpings < 1)
// A                     {
// A                         d1 = d2 = 0;
// A                         decs_debug[i] = 0.75f;
// A                     }
// A                     else
// A                     {
// A                         float fi = -rho / grad_i;
// A                         fi_debug[i] = fi;
// A                         d1 = fi * I1wx[i];
// A                         d2 = fi * I1wy[i];
// A                         decs_debug[i] = 1f;
// A                     }
// A                 }
// A             }
// A 
// A             d2_debug[i] = d2;
// A 
// A             v1[i] = u1[i] + d1;
// A             v2[i] = u2[i] + d2;
// A         }
// A         if (warpings == 1)
// A         {
// A             int a = 1 + 1;
// A         }
// A         // compute the divergence of the dual variable (p1, p2)
// A         divergence(p11, p12, div_p1, nx, ny);
// A         divergence(p21, p22, div_p2, nx, ny);
// A 
// A         // estimate the values of the optical flow (u1, u2)
// A         error = (float)0.0;
// A         for (int i = 0; i < size; i++)
// A         {
// A             float u1k = u1[i];
// A             float u2k = u2[i];
// A 
// A             float prod_u1_l = theta * div_p1[i];
// A             float sum_u1_l = v1[i] + prod_u1_l;
// A             u1[i] = sum_u1_l;
// A 
// A             float prod_u2_l = theta * div_p2[i];
// A             float sum_u2_l = v2[i] + prod_u2_l;
// A             u2[i] = sum_u2_l;
// A 
// A             //05082024 u1[i] = v1[i] + theta * div_p1[i];
// A             //05082024 u2[i] = v2[i] + theta * div_p2[i];
// A 
// A             error += (u1[i] - u1k) * (u1[i] - u1k) +
// A                 (u2[i] - u2k) * (u2[i] - u2k);
// A         }
// A         error /= size;
// A 
// A         // compute the gradient of the optical flow (Du1, Du2)
// A         forward_gradient(u1, u1x, u1y, nx, ny);
// A         forward_gradient(u2, u2x, u2y, nx, ny);
// A 
// A         // estimate the values of the dual variable (p1, p2)
// A         for (int i = 0; i < size; i++)
// A         {
// A             float taut = tau / theta;
// A             float g1 = (float)hypot(u1x[i], u1y[i]);
// A             float g2 = (float)hypot(u2x[i], u2y[i]);
// A             float ng1 = 1f + taut * g1;
// A             float ng2 = 1f + taut * g2;
// A 
// A             p11[i] = (p11[i] + taut * u1x[i]) / ng1;
// A             p12[i] = (p12[i] + taut * u1y[i]) / ng1;
// A             p21[i] = (p21[i] + taut * u2x[i]) / ng2;
// A             p22[i] = (p22[i] + taut * u2y[i]) / ng2;
// A         }
// A 
// A         return (error, p11, p12, p21, p22, u1, u2, v1, v2);
// A     }
// A 
// A     /**
// A      *
// A      * Compute the max and min of an array
// A      *
// A      **/
// A 
// A     // info (paul): an image pointer vector with the corresponding width and height information
// A     //26072024 typedef struct {
// A     //26072024         List<float> im_vec;
// A     //26072024         int width;
// A     //26072024         int height;
// A     //26072024     }
// A     //26072024     im_dressed;
// A 
// A     static (float, float) getminmax(
// A         List<float> x, // input array
// A         int x_cnt           // array size
// A     )
// A     {
// A         float min_val = x[0];
// A         float max_val = x[0];
// A 
// A         //int sizeof_x = sizeof(x);
// A         //int sizeof_float = sizeof(float);
// A         //int cnt_l = sizeof_x / sizeof_float;
// A 
// A         for (int i = 1; i < x_cnt; i++)
// A         {
// A 
// A             //int cnt_l2 = cnt_l + 1;
// A 
// A             float x_el_pre = x[i - 1];
// A             float x_el = x[i];
// A             if (x_el < min_val)
// A                 min_val = x[i];
// A             if (x_el > max_val)
// A                 max_val = x[i];
// A         }
// A 
// A         return (min_val, max_val);
// A 
// A     }
// A 
// A     /**
// A      *
// A      * Function to normalize the images between 0 and 255
// A      *
// A      **/
// A     void image_normalization(
// A             List<float> I0,  // input image0
// A             List<float> I1,  // input image1
// A             List<float> I0n,       // normalized output image0
// A             List<float> I1n,       // normalized output image1
// A             int size          // size of the image
// A             )
// A     {
// A         //float max0, max1, min0, min1;
// A 
// A         // obtain the max and min of each image
// A         //02042024 getminmax(&min0, &max0, I0, size);
// A         //02042024 getminmax(&min1, &max1, I1, size);
// A         (float min0, float max0) = getminmax(I0, size);
// A         (float min1, float max1) = getminmax(I1, size);
// A 
// A         // obtain the max and min of both images
// A         float max = Mathf.Max((float)max0, (float)max1);//(max0 > max1) ? max0 : max1;
// A         float min = Mathf.Min((float)min0, (float)min1); //(min0 < min1) ? min0 : min1;
// A         float den = max - min;
// A 
// A         if (den > 0)
// A             // normalize both images
// A             for (int i = 0; i < size; i++)
// A             {
// A                 I0n[i] = 255f * (I0[i] - min) / den;
// A                 I1n[i] = 255f * (I1[i] - min) / den;
// A             }
// A 
// A         else
// A             // copy the original images
// A             for (int i = 0; i < size; i++)
// A             {
// A                 I0n[i] = I0[i];
// A                 I1n[i] = I1[i];
// A             }
// A     }
// A     public List<float> scale_by(List<float> mat, float factor)
// A     {
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             mat[i] *= factor;
// A         }
// A 
// A         return mat;
// A     }
// A     List<float> norm_floats(List<float> u, int n_x, int n_y)
// A     {
// A         //float min_val = 9999.;
// A         //float max_val = -9999.;
// A         //
// A         //// info (paul): find max and min
// A         //for (int i = 0; i < n_x*n_y; i++)
// A         //{
// A         //	float u_el =  u[i];
// A         //	if (u_el < min_val)
// A         //	{
// A         //		min_val = u_el;
// A         //	}
// A         //	if (u_el > max_val)
// A         //	{
// A         //		max_val = u_el;
// A         //	}
// A         //}
// A         //31082024 float min_val = find_min(u, n_x, n_y);
// A         //31082024 float max_val = find_max(u, n_x, n_y);
// A         (float min_val, float max_val) = find_min_max(u);
// A 
// A         // info (paul): norm, so that everything is positive
// A         for (int j = 0; j < n_x * n_y; j++)
// A         {
// A             u[j] = (u[j] - min_val) / (max_val - min_val);
// A             // perh. TODO: do a full normalization
// A         }
// A         return u;
// A 
// A     }
// A 
// A     //27072024 List<float> rgbs2floats(Vec3b* vec_3bs, int channel_idx = 0, int pointer_len = -1)
// A     //27072024 {
// A     //27072024     // info (paul): extract a float pointer vector, which contains the values of one color channel, 
// A     //27072024     //		from the total rgb-pointer vector
// A     //27072024 
// A     //27072024     int size = sizeof(vec_3bs);
// A     //27072024     int size_per_el = sizeof(vec_3bs[0]);//sizeof(*floats);
// A     //27072024     int num_els = pointer_len;//size / size_per_el;
// A     //27072024 
// A     //27072024     List<float> floats = (new float[num_els]).ToList();
// A     //27072024 
// A     //27072024     for (int i = 0; i < num_els; i++)
// A     //27072024     {
// A     //27072024         Vec3b vec_3b_el = vec_3bs[i];
// A     //27072024         uchar channel_val = vec_3b_el.val[channel_idx];
// A     //27072024         float float_val = (float)channel_val;
// A     //27072024         floats[i] = float_val;
// A     //27072024     }
// A     //27072024 
// A     //27072024     //print_some(floats, "floats");
// A     //27072024     return floats;
// A     //27072024 }
// A     im_dressed manage_read_im(string idx_label)
// A     {
// A         string path = idx_label;
// A 
// A         byte[] bytes = File.ReadAllBytes(path);
// A         //byte[] bytes_tiff = File.ReadAllBytes("C:/Users/go73jem/Desktop/DIC_package/exp_normal/cam_0/uv/im_03.tif");
// A         //19092024 byte[] bytes_tiff = File.ReadAllBytes("C:/Users/go73jem/Desktop/DIC_package/exp_normal/cam_0/uv/simple1.tif");
// A 
// A         (int width, int height) = bytes2res(bytes);
// A         Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
// A         texture.LoadImage(bytes);
// A 
// A         System.IO.File.WriteAllBytes("C:/Users/go73jem/Pictures/test111.png", texture.EncodeToPNG());
// A 
// A         List<float> mat = tex2floats(texture, with_switch_dims: false, color_channel: 1);
// A         List<float> mat_flipped = flip_floats(mat, width, height);
// A         List<float> mat_scaled = scale_by(mat_flipped, factor: 0.33f * 256f);
// A 
// A         im_dressed im_dressed_1 = new im_dressed(mat_scaled, width, height);
// A         return im_dressed_1;
// A     }
// A 
// A     public List<float> flip_floats(List<float> mat, int width, int height)
// A     {
// A         List<float> mat_new = zeros_of_size(mat.Count);
// A 
// A         // info (paul): interpret the floats as matrix and flip them
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             int i_idx = i % width;
// A             int j_idx = (i - i_idx) / width;
// A 
// A             mat_new[(height - 1 - i_idx) * width + j_idx] = mat[i_idx * width + j_idx];
// A         }
// A 
// A         return mat_new;
// A     }
// A 
// A     string PAR_DEFAULT_OUTFLOW = "flow.flo";
// A     int PAR_DEFAULT_NPROC = 0;
// A     double PAR_DEFAULT_TAU = 0.25d;//0.25d;//5.0d;//27092024 0.25d;
// A     double PAR_DEFAULT_LAMBDA = 0.15d;//0.0001f;//C 0.0001d;//A 0.00001d;//B 0.0001d;//0.15d;//1.0d;//27092024 0.15d;
// A     double PAR_DEFAULT_THETA = 0.3d;//20d;//08102024 20d;//C 20d;//A 200d;//20d;//B 20d;//0.01d;//27092024 0.3d;
// A     int PAR_DEFAULT_NSCALES = 8;//4;//4;//2;//27092024 2;//25092024 3;//24092024 100;
// A     double PAR_DEFAULT_ZFACTOR = 0.5d;//27092024 0.5d;
// A     int PAR_DEFAULT_NWARPS = 5;//6;//6;//08102024 20;//27092024 6;//5//25092024 5;//Bc 20;//20;//5;//08052024 5;
// A     double PAR_DEFAULT_EPSILON = 0.01f;//08102024 0.0001f;//C 0.0001d;//A 0.00005d;//27092024 0.01d;
// A     int PAR_DEFAULT_VERBOSE = 0;
// A 
// A     public void cv_main(int render_act_idx, int d_cam = 0, bool with_dt = false)
// A     {
// A         int dt_compare_l = 0;
// A         if (with_dt)
// A         {
// A             dt_compare_l = get_dt_compare();
// A             PAR_DEFAULT_LAMBDA = 0.0001f;
// A             PAR_DEFAULT_THETA = 20d;
// A             PAR_DEFAULT_NSCALES = 4;
// A             PAR_DEFAULT_NWARPS = 6;
// A         }
// A         else
// A         {
// A             PAR_DEFAULT_LAMBDA = 0.3d;//0.15d;
// A             PAR_DEFAULT_THETA = 0.3d;
// A             PAR_DEFAULT_NSCALES = 8;
// A             PAR_DEFAULT_NWARPS = 4;
// A         }
// A 
// A         // perh. TODO: check, what is actually "right" and "left" (but I think it does not really matter)
// A         int t_steps = 1;//Bc 7;// 3;//19092024 3;//5//27072024 100;
// A         int t_idx_start = 0;//19092024 0;
// A         // info (paul): t_steps = blade_idx_max - blade_idx_min would be ideal;
// A 
// A         int cam_idx_0 = 0;
// A         int cam_idx_1 = cam_idx_0 + d_cam;
// A 
// A         for (int t_idx_i = t_idx_start; t_idx_i < t_idx_start + t_steps; t_idx_i++)
// A         {
// A             int t_idx_0 = t_idx_i;
// A             int t_idx_1 = t_idx_0 + dt_compare_l;
// A 
// A             string info = "render_act: " + render_act_idx.ToString() + " / " + render_acts.Count.ToString() +
// A                 "; t_idx: " + (t_idx_i - t_idx_start).ToString() + " / " + t_steps.ToString();
// A             Debug.Log(info);
// A 
// A             string im_t_name = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(get_experiment()) +
// A                 "/cam_" + cam_idx_0.ToString() + "/uv/im_" + t_idx_0.ToString() + ".png";
// A             string im_t_next_name = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(get_experiment()) +
// A                 "/cam_" + cam_idx_1.ToString() + "/uv/im_" + t_idx_1.ToString() + ".png";
// A 
// A             // info (paul): output paths
// A             //08102024 string I0_path = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(get_experiment()) + "/time_flow_v/floats_0.txt";
// A             //08102024 string I1_path = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(get_experiment()) +"/time_flow_v/floats_1.txt";
// A             string time_flow_name_u = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(get_experiment()) +
// A                 "/time_flow_u/time_flow_u_" + t_idx_0.ToString() + ".png";
// A             string time_flow_name_v = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(get_experiment()) +
// A                 "/time_flow_v/time_flow_v_" + t_idx_0.ToString() + ".png";
// A             string heights_name = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(get_experiment()) +
// A                 "/heights/heights_" + t_idx_0.ToString() + ".png";
// A 
// A             string min_max_u_file = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(get_experiment()) +
// A                 "/min_max_u_" + t_idx_0.ToString() + ".txt";
// A             string min_max_v_file = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(get_experiment()) +
// A                 "/min_max_v_" + t_idx_0.ToString() + ".txt";
// A             string min_max_heights_file = "C:/Users/go73jem/Desktop/DIC_package/" + remove_dots(get_experiment()) +
// A                 "/min_max_heights_" + t_idx_0.ToString() + ".txt";
// A 
// A             bool next_exists = File.Exists(im_t_next_name);
// A 
// A             if (next_exists)
// A             {
// A                 bool with_plot = true;
// A                 comp_ims(im_t_name, im_t_next_name, time_flow_name_u,
// A                     time_flow_name_v, heights_name, min_max_u_file, min_max_v_file, 
// A                     min_max_heights_file, argc: -1, argv: null, 1, with_plot: with_plot, 
// A                     I0_path: null, I1_path: null, t_idx_l: t_idx_i, with_dt: with_dt);
// A             }
// A         }
// A     }
// A 
// A     public List<float> txt2floats(string I0_path)
// A     {
// A         using StreamReader reader = new(I0_path);
// A         string text = reader.ReadToEnd();
// A         string[] nums = text.Split("\r\n");
// A         List<float> floats = new List<float>();
// A 
// A         for (int i = 0; i < nums.Length; i++)
// A         {
// A             string nums_i = nums[i];
// A             if (nums_i != "")
// A             {
// A                 floats.Add(float.Parse(nums_i));
// A             }
// A         }
// A 
// A         return floats;
// A     }
// A 
// A     int comp_ims(string im_0_label, string im_1_label, string out_file_name_u,
// A         string out_file_name_v, string heights_name, string min_max_u_file, 
// A         string min_max_v_file, string min_max_heights_file, int argc, List<char> argv, float scale_fac = -1f, 
// A         bool with_plot = false, string I0_path = null, string I1_path = null, 
// A         int t_idx_l = -1, bool with_dt = false)
// A     {
// A         string outfile = PAR_DEFAULT_OUTFLOW;
// A         int nproc = PAR_DEFAULT_NPROC;
// A         float tau = (float)PAR_DEFAULT_TAU;
// A         float lambda = (float)PAR_DEFAULT_LAMBDA;
// A         float theta = (float)PAR_DEFAULT_THETA;
// A         int nscales = PAR_DEFAULT_NSCALES;
// A         float zfactor = (float)PAR_DEFAULT_ZFACTOR;
// A         int nwarps = PAR_DEFAULT_NWARPS;//Bc 20;//20092024 (t_idx_l == -1 || t_idx_l == 0) ? PAR_DEFAULT_NWARPS: 2;
// A         float epsilon = (float)PAR_DEFAULT_EPSILON;
// A         int verbose = PAR_DEFAULT_VERBOSE;
// A 
// A 
// A         // read the input images
// A         int nx, ny, nx2, ny2;
// A 
// A         im_dressed imd_0 = manage_read_im(im_0_label);
// A         im_dressed imd_1 = manage_read_im(im_1_label);
// A 
// A         List<float> I0 = imd_0.im_vec;
// A         List<float> I1 = imd_1.im_vec;
// A 
// A         Texture2D tex_0 = floats2tex(imd_0.im_vec, imd_0.width, imd_0.height);
// A         Texture2D tex_1 = floats2tex(imd_1.im_vec, imd_1.width, imd_1.height);
// A         System.IO.File.WriteAllBytes("C:/Users/go73jem/Desktop/DIC_package/exp_normal/time_flow_u/time_flow_u_3.png", tex_0.EncodeToPNG());
// A         System.IO.File.WriteAllBytes("C:/Users/go73jem/Desktop/DIC_package/exp_normal/time_flow_u/time_flow_u_4.png", tex_1.EncodeToPNG());
// A 
// A         nx = imd_0.width;
// A         ny = imd_1.height;
// A         nx2 = imd_0.width;
// A         ny2 = imd_1.height;
// A 
// A         //read the images and compute the optical flow
// A         if (nx == nx2 && ny == ny2)
// A         {
// A             // info (paul): compute the optical flow; note that we switched nx and ny as input 
// A             //		arguments, which seems to be necessary and right to arrange the pixels of non-quadratic
// A             //		images correctly.
// A             find_displ(I0, I1, nx, ny, tau,
// A                 lambda, theta, nscales, zfactor, nwarps, epsilon,
// A                 (verbose > 0), nproc, out_file_name_u, out_file_name_v,
// A                 heights_name, min_max_u_file, min_max_v_file, min_max_heights_file, 
// A                 scale_fac: scale_fac, with_plot: with_plot, with_dt: with_dt);
// A         }
// A         else
// A         {
// A             Debug.Log("ERROR: input images size mismatch ");//fprintf(stderr, "ERROR: input images size mismatch "
// A             //
// A             //    "%dx%d != %dx%d\n", nx, ny, nx2, ny2);
// A             //return EXIT_FAILURE;
// A         }
// A 
// A         return 0;
// A     }
// A     public void arrange_dir(string out_file_name_u)
// A     {
// A         int index = out_file_name_u.LastIndexOf("/");
// A         string dir_path = out_file_name_u.Substring(0, index);
// A         if (!File.Exists(dir_path))
// A         {
// A             Directory.CreateDirectory(dir_path);
// A         }
// A     }
// A     void find_displ(List<float> I0, List<float> I1, int nx, int ny, float tau,
// A         float lambda, float theta, int nscales, float zfactor,
// A         int nwarps, float epsilon, bool verbose, int nproc,
// A         string out_file_name_u, string out_file_name_v, string heights_name, 
// A         string min_max_u_file, string min_max_v_file, string min_max_heights_file, 
// A         float scale_fac = -1f, bool with_plot = false, bool with_dt = false)
// A     {
// A         //Set the number of scales according to the size of the
// A         //images.  The value N is computed to assure that the smaller
// A         //images of the pyramid don't have a size smaller than 16x16
// A         float N = (float)(1 + Math.Log(hypot((double)nx, (double)ny) / 16d) / Math.Log(1 / zfactor));
// A         if (N < nscales)
// A             nscales = (int)N;
// A 
// A         if (verbose)
// A             Debug.Log("some message");
// A         //fprintf(stderr,
// A         //    "nproc=%d tau=%f lambda=%f theta=%f nscales=%d "
// A         //    "zfactor=%f nwarps=%d epsilon=%g\n",
// A         //    nproc, tau, lambda, theta, nscales,
// A         //    zfactor, nwarps, epsilon);
// A 
// A         //allocate memory for the flow
// A         List<float> u = (new float[2 * nx * ny]).ToList();
// A         List<float> v = (new float[2 * nx * ny]).ToList();//27072024 u + nx * ny;//probably this is some kind of extension
// A 
// A         Dual_TVL1_optic_flow_multiscale(
// A             I0, I1, u, v, ny, nx, tau, lambda, theta,
// A             nscales, zfactor, nwarps, epsilon, verbose, with_dt: with_dt
// A         );
// A 
// A         float scale_fac_used = scale_fac;
// A         if (scale_fac < 0f)
// A         {
// A             scale_fac_used = 100f;
// A         }
// A 
// A         // info (paul): min_max u
// A         //30082024 float min_val_u = find_min(u, nx, ny, offset: nx*ny);
// A         //30082024 float max_val_u = find_max(u, nx, ny, offset: nx*ny);
// A         (float min_val_u, float max_val_u) = find_min_max(u);
// A         manage_write_max_min(min_max_u_file, min_val_u, max_val_u);
// A 
// A         // info (paul): min_max v
// A         //30082024 float min_val_v = find_min(v, nx, ny, offset: nx*ny);
// A         //30082024 float max_val_v = find_max(v, nx, ny, offset: nx*ny);
// A         (float min_val_v, float max_val_v) = find_min_max(v);
// A         manage_write_max_min(min_max_v_file, min_val_v, max_val_v);
// A 
// A         u = norm_floats(u, nx, ny);//02052024
// A         v = norm_floats(v, nx, ny);
// A 
// A         if (with_plot)
// A         {
// A             //02052024 show_floats(u, nx, ny, scale_fac_used, out_file_name_u);
// A             //02052024 show_floats(v, nx, ny, scale_fac_used, out_file_name_v);
// A 
// A             //02052024 print_floats_as_mat(u, nx, ny);
// A             //02052024 print_floats_as_mat(v, nx, ny);
// A         }
// A 
// A         //-----------------------
// A 
// A         // info (paul): save the optical flow; I think for saving we need to scale 
// A         //		the intensity by 256 or 100 or so, to see the same thing, that we see 
// A         //		in the plot panel in "show_floats", otherwise the .png-file looks 
// A         //		in the plot panel in "show_floats", otherwise the .png-file looks 
// A         //		more or less just black
// A         //iio_save_image_float_split(outfile, u, nx, ny, 2);
// A 
// A         //27072024 cv::Mat u_im = floats2mat(u, nx, ny, 256.);
// A         //27072024 manage_write(u_im, out_file_name_u);
// A         //27072024 
// A         //27072024 cv::Mat v_im = floats2mat(v, nx, ny, 256.);
// A         //27072024 manage_write(v_im, out_file_name_v);
// A 
// A         float[][] u_mat = floats2matrix(u.ToArray(), nx, ny, with_switch_dims: false);
// A         List<List<float>> u_lists = floats2_to_lists(u_mat);
// A         Texture2D tex_u = mat2tex(u_lists, with_switch_dims: false);
// A         arrange_dir(out_file_name_u);
// A         System.IO.File.WriteAllBytes(out_file_name_u, tex_u.EncodeToPNG());
// A 
// A         float[][] v_mat = floats2matrix(v.ToArray(), nx, ny, with_switch_dims: false);
// A         List<List<float>> v_lists = floats2_to_lists(v_mat);
// A         Texture2D tex_v = mat2tex(v_lists, with_switch_dims: false);
// A         arrange_dir(out_file_name_v);
// A         System.IO.File.WriteAllBytes(out_file_name_v, tex_v.EncodeToPNG());
// A 
// A         //save_png(tex, cam_idx, blade_idx, "_depth");
// A 
// A         // info (paul): save as floats if requested
// A         if (!with_dt)
// A         {
// A             // info (paul): the horizontal u-displacement should be the difference
// A             save_floats2(u_mat, file_name: "heights_tv", heights_name);
// A         }
// A 
// A         // TODO: save stream
// A     }
// A     public List<List<float>> copy_floats(List<List<float>> u_input)
// A     {
// A         List<List<float>> floats = new List<List<float>>();
// A 
// A         for (int i = 0; i < u_input.Count; i++)
// A         {
// A             List<float> floats_1d = new List<float>();
// A             for (int j = 0; j < u_input[i].Count; j++)
// A             {
// A                 floats_1d.Add(u_input[i][j]);
// A             }
// A             floats.Add(floats_1d);
// A         }
// A         return floats;
// A 
// A     }
// A     public List<float> copy_floats(List<float> u_input)
// A     {
// A         List<float> floats = new List<float>();
// A 
// A         for (int i = 0; i < u_input.Count; i++)
// A         {
// A             floats.Add(u_input[i]);
// A         }
// A         return floats;
// A 
// A     }
// A     public List<float> scale_floats(List<float> u, int n_x, int n_y, float scale, float offset)
// A     {
// A         float min_val = find_min(u, n_x, n_y);
// A         float max_val = find_max(u, n_x, n_y);
// A 
// A         // info (paul): norm, so that everything is positive
// A         for (int j = 0; j < n_x * n_y; j++)
// A         {
// A             u[j] = scale * u[j] + offset;
// A             // perh. TODO: do a full normalization
// A         }
// A 
// A         return u;
// A     }
// A 
// A     public List<List<float>> scale_mat(List<List<float>> mat, float factor)
// A     {
// A         for (int i = 0; i < mat.Count; i++)
// A         {
// A             for (int j = 0; j < mat[0].Count; j++)
// A             {
// A                 mat[i][j] *= factor;
// A             }
// A         }
// A 
// A         return mat;
// A     }
// A 
// A     public int get_dt_compare()
// A     {
// A         return dt_compare;
// A     }
// A     public void set_dt_compare(int input)
// A     {
// A         dt_compare = input;
// A     }
// A     public List<float> first_half_of(List<float> u_input)
// A     {
// A         List<float> u_half = new List<float>();
// A 
// A         for (int i = 0; i < u_input.Count / 2; i++)
// A         {
// A             u_half.Add(u_input[i]);
// A         }
// A 
// A         return u_half;
// A     }
// A     public void write_for_debug(List<float> u_input, float scale)
// A     {
// A         write_for_debug(u_input, with_norm: false, scale: scale, offset: 0f);
// A     }
// A     public void write_for_debug(List<float> u_input)
// A     {
// A         write_for_debug(u_input, with_norm: false, scale: 1f, offset: 0f);
// A     }
// A     public void write_for_debug(List<float> u_input, bool with_norm = false, float scale = 1f, float offset = 0f)
// A     {
// A         List<float> u = copy_floats(u_input);
// A 
// A         // info (paul): if this is in case that a vector contains 2 ims
// A         if (u_input.Count == 2*16*16 || u_input.Count == 2*128*128 || u_input.Count == 2*512*512)
// A         {
// A             u_input = first_half_of(u_input);
// A         }
// A 
// A         string out_file_name_u = "C:/Users/go73jem/Desktop/DIC_package/exp_normal/time_flow_v/debug_im.png";
// A         int nx = (int)(Mathf.Sqrt(u_input.Count));//128;
// A         int ny = (int)(Mathf.Sqrt(u_input.Count));//128;
// A 
// A         if (with_norm)
// A         {
// A             u = norm_floats(u, nx, ny);
// A         }
// A         
// A         u = scale_floats(u, nx, ny, scale, offset);
// A 
// A         float[][] u_mat = floats2matrix(u.ToArray(), nx, ny, with_switch_dims: false);
// A         List<List<float>> u_lists = floats2_to_lists(u_mat);
// A         Texture2D tex_u = mat2tex(u_lists, with_switch_dims: false);
// A         System.IO.File.WriteAllBytes(out_file_name_u, tex_u.EncodeToPNG());
// A     }
// A     public void write_mat_for_debug(List<List<float>> u_lists, float scale = 1f)
// A     {
// A         List<List<float>> u_copy = copy_mat(u_lists);
// A         u_copy = scale_mat(u_copy, scale);
// A 
// A         // info (paul): write png file
// A         string out_file_name_u = "C:/Users/go73jem/Desktop/DIC_package/exp_normal/time_flow_v/debug_im.png";
// A         Texture2D tex_u = mat2tex(u_copy, with_switch_dims: false);
// A         System.IO.File.WriteAllBytes(out_file_name_u, tex_u.EncodeToPNG());
// A 
// A         // info (paul): write csv file
// A         string fileName = "C:/Users/go73jem/Desktop/DIC_package/exp_normal/time_flow_v/debug_im.txt";
// A         using (StreamWriter file = new StreamWriter(fileName))
// A         {
// A             for (int i = 0; i < u_lists.Count; i++)
// A             {
// A                 for (int j = 0; j < u_lists[i].Count; j++)
// A                 {
// A                     float elem = u_lists[i][j];
// A                     file.Write(elem + "\t");
// A                 }
// A                 file.Write(";\n");
// A             }
// A         }
// A 
// A     }
// A     void manage_write_max_min(string out_file_name_u, float min_val, float max_val)
// A     {
// A         //FILE* fptr;
// A         //
// A         //fopen_s(&fptr, out_file_name_u, "w");
// A         //
// A         //if (fptr == NULL)
// A         //{
// A         //    printf("pointer is null");
// A         //    exit(0);
// A         //}
// A         //
// A         //fprintf(fptr, "%f %f", min_val, max_val);
// A         //fclose(fptr);
// A 
// A 
// A         string path = out_file_name_u;//27072024string.Join("", out_file_name_u);
// A 
// A         string text = min_val.ToString() + " " + max_val.ToString();
// A 
// A         StreamWriter writer = new StreamWriter(path, false);
// A         
// A         writer.Write(text);
// A         writer.Close();
// A         
// A 
// A 
// A     }
// A 
// A     /**
// A      *
// A      * Function to compute the optical flow using multiple scales
// A      *
// A      **/
// A     void Dual_TVL1_optic_flow_multiscale(
// A             List<float> I0,           // source image
// A             List<float> I1,           // target image
// A             List<float> u1,           // x component of the optical flow
// A             List<float> u2,           // y component of the optical flow
// A             int nxx,     // image width
// A             int nyy,     // image height
// A             float tau,     // time step
// A             float lambda,  // weight parameter for the data term
// A             float theta,   // weight parameter for (u - v)²
// A             int nscales, // number of scales
// A             float zfactor, // factor for building the image piramid
// A             int warps,   // number of warpings per scale
// A             float epsilon, // tolerance for numerical convergence
// A             bool verbose,  // enable/disable the verbose mode
// A             bool with_dt)
// A     {
// A         //nscales = 1; - in case you want back to the easy nscales=1 time
// A         int size = nxx * nyy;
// A 
// A         // allocate memory for the pyramid structure
// A         //26072024 float** I0s = (float**)xmalloc(nscales * sizeof(float*));
// A         //26072024 float** I1s = (float**)xmalloc(nscales * sizeof(float*));
// A         //26072024 float** u1s = (float**)xmalloc(nscales * sizeof(float*));
// A         //26072024 float** u2s = (float**)xmalloc(nscales * sizeof(float*));
// A 
// A         List<List<float>> I0s = zeros_of_size(nscales, size); //(float)xmalloc(nscales * sizeof(float*));
// A         List<List<float>> I1s = zeros_of_size(nscales, size); //(float**)xmalloc(nscales * sizeof(float*));
// A         List<List<float>> u1s = zeros_of_size(nscales, size); //(float**)xmalloc(nscales * sizeof(float*));
// A         List<List<float>> u2s = zeros_of_size(nscales, size); //(float**)xmalloc(nscales * sizeof(float*));
// A 
// A         List<int> nx = ints_of_size(nscales);
// A         List<int> ny = ints_of_size(nscales);
// A 
// A         //I0s[0] = (float*)xmalloc(size * sizeof(float));
// A         //I1s[0] = (float*)xmalloc(size * sizeof(float));
// A         I0s[0] = (new float[size]).ToList();
// A         I1s[0] = (new float[size]).ToList();
// A 
// A         u1s[0] = u1;
// A         u2s[0] = u2;
// A         nx[0] = nxx;
// A         ny[0] = nyy;
// A 
// A         // normalize the images between 0 and 255
// A         image_normalization(I0, I1, I0s[0], I1s[0], size);
// A 
// A         // pre-smooth the original images
// A         gaussian(I0s[0], nx[0], ny[0], PRESMOOTHING_SIGMA);
// A         gaussian(I1s[0], nx[0], ny[0], PRESMOOTHING_SIGMA);
// A 
// A         // create the scales
// A         for (int s = 1; s < nscales; s++)
// A         {
// A             (nx[s], ny[s]) = zoom_size(nx[s - 1], ny[s - 1], zfactor);
// A             int sizes = nx[s] * ny[s];
// A 
// A             // allocate memory
// A             //I0s[s] = (float*)xmalloc(sizes * sizeof(float));
// A             //I1s[s] = (float*)xmalloc(sizes * sizeof(float));
// A             //u1s[s] = (float*)xmalloc(sizes * sizeof(float));
// A             //u2s[s] = (float*)xmalloc(sizes * sizeof(float));
// A             I0s[s] = (new float[sizes]).ToList();
// A             I1s[s] = (new float[sizes]).ToList();
// A             u1s[s] = (new float[sizes]).ToList();
// A             u2s[s] = (new float[sizes]).ToList();
// A 
// A             // zoom in the images to create the pyramidal structure
// A             zoom_out(I0s[s - 1], I0s[s], nx[s - 1], ny[s - 1], zfactor);
// A             zoom_out(I1s[s - 1], I1s[s], nx[s - 1], ny[s - 1], zfactor);
// A         }
// A 
// A         // initialize the flow at the coarsest scale
// A         for (int i = 0; i < nx[nscales - 1] * ny[nscales - 1]; i++)
// A         {
// A             u1s[nscales - 1][i] = u2s[nscales - 1][i] = 0f;
// A         }
// A 
// A         if (!with_dt)
// A         {
// A             ;
// A         }
// A 
// A         // pyramidal structure for computing the optical flow
// A         for (int s = nscales - 1; s >= 0; s--)
// A         {
// A             if (verbose)
// A             {
// A                 //27072024 Debug.Log(stderr, "Scale %d: %dx%d\n", s, nx[s], ny[s]);
// A                 //Debug.Log("Scale %d: %dx%d\n", s, nx[s], ny[s]);
// A                 Debug.Log("Scale [...] who knows what");
// A             }
// A 
// A             // compute the optical flow at the current scale
// A             Dual_TVL1_optic_flow(
// A                     I0s[s], I1s[s], u1s[s], u2s[s], nx[s], ny[s],
// A                     tau, lambda, theta, warps, epsilon, verbose
// A             );
// A 
// A             // if this was the last scale, finish now
// A             if (s == 0)//!s
// A             {
// A                 break;
// A             }
// A 
// A             // otherwise, upsample the optical flow
// A 
// A             // zoom the optical flow for the next finer scale
// A             zoom_in(u1s[s], u1s[s - 1], nx[s], ny[s], nx[s - 1], ny[s - 1], with_dt: with_dt);
// A             zoom_in(u2s[s], u2s[s - 1], nx[s], ny[s], nx[s - 1], ny[s - 1], with_dt: with_dt);
// A 
// A             // scale the optical flow with the appropriate zoom factor
// A             for (int i = 0; i < nx[s - 1] * ny[s - 1]; i++)
// A             {
// A                 u1s[s - 1][i] *= (float)1.0 / zfactor;
// A                 u2s[s - 1][i] *= (float)1.0 / zfactor;
// A             }
// A         }
// A     }
// A 
// A 
// A     // info (paul): mask.c part from OpenCV
// A 
// A     const int BOUNDARY_CONDITION_DIRICHLET = 0;
// A     const int BOUNDARY_CONDITION_REFLECTING = 1;
// A     const int BOUNDARY_CONDITION_PERIODIC = 2;
// A 
// A     int DEFAULT_GAUSSIAN_WINDOW_SIZE = 5;
// A     int DEFAULT_BOUNDARY_CONDITION = BOUNDARY_CONDITION_REFLECTING;
// A 
// A 
// A     /**
// A      *
// A      * Details on how to compute the divergence and the grad(u) can be found in:
// A      * [2] A. Chambolle, "An Algorithm for Total Variation Minimization and
// A      * Applications", Journal of Mathematical Imaging and Vision, 20: 89-97, 2004
// A      *
// A      **/
// A 
// A 
// A     /**
// A      *
// A      * Function to compute the divergence with backward differences
// A      * (see [2] for details)
// A      *
// A      **/
// A     void divergence(
// A             List<float> v1, // x component of the vector field
// A             List<float> v2, // y component of the vector field
// A             List<float> div,      // output divergence
// A             int nx,    // image width
// A             int ny     // image height
// A                )
// A     {
// A         // compute the divergence on the central body of the image
// A #pragma omp parallel for schedule(dynamic)
// A         for (int i = 1; i < ny - 1; i++)
// A         {
// A             for (int j = 1; j < nx - 1; j++)
// A             {
// A                 int p = i * nx + j;
// A                 int p1 = p - 1;
// A                 int p2 = p - nx;
// A 
// A                 float v1x = v1[p] - v1[p1];
// A                 float v2y = v2[p] - v2[p2];
// A 
// A                 div[p] = v1x + v2y;
// A             }
// A         }
// A 
// A         // compute the divergence on the first and last rows
// A         for (int j = 1; j < nx - 1; j++)
// A         {
// A             int p = (ny - 1) * nx + j;
// A 
// A             div[j] = v1[j] - v1[j - 1] + v2[j];
// A             div[p] = v1[p] - v1[p - 1] - v2[p - nx];
// A         }
// A 
// A         // compute the divergence on the first and last columns
// A         for (int i = 1; i < ny - 1; i++)
// A         {
// A             int p1 = i * nx;
// A             int p2 = (i + 1) * nx - 1;
// A 
// A             div[p1] = v1[p1] + v2[p1] - v2[p1 - nx];
// A             div[p2] = -v1[p2 - 1] + v2[p2] - v2[p2 - nx];
// A 
// A         }
// A 
// A         div[0] = v1[0] + v2[0];
// A         div[nx - 1] = -v1[nx - 2] + v2[nx - 1];
// A         div[(ny - 1) * nx] = v1[(ny - 1) * nx] - v2[(ny - 2) * nx];
// A         div[ny * nx - 1] = -v1[ny * nx - 2] - v2[(ny - 1) * nx - 1];
// A     }
// A 
// A 
// A     /**
// A      *
// A      * Function to compute the gradient with forward differences
// A      * (see [2] for details)
// A      *
// A      **/
// A     void forward_gradient(
// A             List<float> f, //input image
// A             List<float> fx,      //computed x derivative
// A             List<float> fy,      //computed y derivative
// A             int nx,   //image width
// A             int ny    //image height
// A             )
// A     {
// A         // compute the gradient on the central body of the image
// A 
// A         for (int i = 0; i < ny - 1; i++)
// A         {
// A             for (int j = 0; j < nx - 1; j++)
// A             {
// A                 int p = i * nx + j;
// A                 int p1 = p + 1;
// A                 int p2 = p + nx;
// A 
// A                 fx[p] = f[p1] - f[p];
// A                 fy[p] = f[p2] - f[p];
// A             }
// A         }
// A 
// A         // compute the gradient on the last row
// A         for (int j = 0; j < nx - 1; j++)
// A         {
// A             int p = (ny - 1) * nx + j;
// A 
// A             fx[p] = f[p + 1] - f[p];
// A             fy[p] = 0;
// A         }
// A 
// A         // compute the gradient on the last column
// A         for (int i = 1; i < ny; i++)
// A         {
// A             int p = i * nx - 1;
// A 
// A             fx[p] = 0;
// A             fy[p] = f[p + nx] - f[p];
// A         }
// A 
// A         fx[ny * nx - 1] = 0;
// A         fy[ny * nx - 1] = 0;
// A     }
// A 
// A 
// A     /**
// A      *
// A      * Function to compute the gradient with centered differences
// A      *
// A      **/
// A     void centered_gradient(
// A             List<float> input,  //input image
// A             List<float> dx,           //computed x derivative
// A             List<float> dy,           //computed y derivative
// A             int nx,        //image width
// A             int ny         //image height
// A             )
// A     {
// A         // compute the gradient on the center body of the image
// A 
// A         for (int i = 1; i < ny - 1; i++)
// A         {
// A             for (int j = 1; j < nx - 1; j++)
// A             {
// A                 if (i == 40 && j == 52)
// A                 {
// A                     ;
// A                 }
// A 
// A                 int k = i * nx + j;
// A                 dx[k] = (float)(0.5 * (input[k + 1] - input[k - 1]));
// A                 dy[k] = (float)(0.5 * (input[k + nx] - input[k - nx]));//(input[k + nx] - input[k - nx]));
// A             }
// A         }
// A 
// A         // compute the gradient on the first and last rows
// A         for (int j = 1; j < nx - 1; j++)
// A         {
// A             dx[j] = (float)(0.5 * (input[j + 1] - input[j - 1]));
// A             dy[j] = (float)(0.5 * (input[j + nx] - input[j]));
// A 
// A             int k = (ny - 1) * nx + j;
// A 
// A             dx[k] = (float)(0.5) * (input[k + 1] - input[k - 1]);
// A             dy[k] = (float)(0.5) * (input[k] - input[k - nx]);
// A         }
// A 
// A         // compute the gradient on the first and last columns
// A         for (int i = 1; i < ny - 1; i++)
// A         {
// A             int p = i * nx;
// A             dx[p] = (float)(0.5) * (input[p + 1] - input[p]);
// A             dy[p] = (float)(0.5) * (input[p + nx] - input[p - nx]);
// A 
// A             int k = (i + 1) * nx - 1;
// A 
// A             dx[k] = (float)(0.5) * (input[k] - input[k - 1]);
// A             dy[k] = (float)(0.5) * (input[k + nx] - input[k - nx]);
// A         }
// A 
// A         // compute the gradient at the four corners
// A         dx[0] = (float)(0.5) * (input[1] - input[0]);
// A         dy[0] = (float)(0.5) * (input[nx] - input[0]);
// A 
// A         dx[nx - 1] = (float)(0.5) * (input[nx - 1] - input[nx - 2]);
// A         dy[nx - 1] = (float)(0.5) * (input[2 * nx - 1] - input[nx - 1]);
// A 
// A         dx[(ny - 1) * nx] = (float)(0.5) * (input[(ny - 1) * nx + 1] - input[(ny - 1) * nx]);
// A         dy[(ny - 1) * nx] = (float)(0.5) * (input[(ny - 1) * nx] - input[(ny - 2) * nx]);
// A 
// A         dx[ny * nx - 1] = (float)(0.5) * (input[ny * nx - 1] - input[ny * nx - 1 - 1]);
// A         dy[ny * nx - 1] = (float)(0.5) * (input[ny * nx - 1] - input[(ny - 1) * nx - 1]);
// A     }
// A 
// A     /**
// A      *
// A      * In-place Gaussian smoothing of an image
// A      *
// A      */
// A     void gaussian(
// A         List<float> I,             // input/output image
// A         int xdim,       // image width
// A         int ydim,       // image height
// A         double sigma    // Gaussian sigma
// A     )
// A     {
// A         int boundary_condition = DEFAULT_BOUNDARY_CONDITION;
// A         int window_size = DEFAULT_GAUSSIAN_WINDOW_SIZE;
// A 
// A         double den = 2 * sigma * sigma;
// A         int size = (int)(window_size * sigma) + 1;
// A         int bdx = xdim + size;
// A         int bdy = ydim + size;
// A 
// A         if (boundary_condition > 0 && size > xdim)
// A         {
// A             Debug.Log("GaussianSmooth: sigma too large\n");
// A             //Debug.Log(stderr, "GaussianSmooth: sigma too large\n");
// A             //abort();
// A         }
// A 
// A         // compute the coefficients of the 1D convolution kernel
// A         //List<double> B = (double*)malloc(size * sizeof(double));
// A         List<double> B = (new double[size]).ToList();
// A         for (int i = 0; i < size; i++)
// A         {
// A             //B[i] = 1 / (sigma * Mathf.Sqrt(2f * 3.1415926f)) * Mathf.Exp(-i * i / (float)den);
// A             B[i] = 1 / (sigma * Math.Sqrt(2.0 * 3.1415926)) * Math.Exp(-i * i / den);
// A         }
// A 
// A         // normalize the 1D convolution kernel
// A         double norm = 0;
// A         for (int i = 0; i < size; i++)
// A             norm += B[i];
// A         norm *= 2;
// A         norm -= B[0];
// A         for (int i = 0; i < size; i++)
// A             B[i] /= norm;
// A 
// A         // convolution of each line of the input image
// A         //26072024 List<double> R = (double*)xmalloc((size + xdim + size) * sizeof*R);
// A         List<double> R = (new double[size + xdim + size]).ToList();
// A 
// A         for (int k = 0; k < ydim; k++)
// A         {
// A             int i, j;
// A             for (i = size; i < bdx; i++)
// A                 R[i] = I[k * xdim + i - size];
// A 
// A             switch (boundary_condition)
// A             {
// A                 case BOUNDARY_CONDITION_DIRICHLET:
// A                     for (i = 0, j = bdx; i < size; i++, j++)
// A                         R[i] = R[j] = 0;
// A                     break;
// A 
// A                 case BOUNDARY_CONDITION_REFLECTING:
// A                     for (i = 0, j = bdx; i < size; i++, j++)
// A                     {
// A                         R[i] = I[k * xdim + size - i];
// A                         R[j] = I[k * xdim + xdim - i - 1];
// A                     }
// A                     break;
// A 
// A                 case BOUNDARY_CONDITION_PERIODIC:
// A                     for (i = 0, j = bdx; i < size; i++, j++)
// A                     {
// A                         R[i] = I[k * xdim + xdim - size + i];
// A                         R[j] = I[k * xdim + i];
// A                     }
// A                     break;
// A             }
// A 
// A             for (i = size; i < bdx; i++)
// A             {
// A                 double sum = B[0] * R[i];
// A                 for (j = 1; j < size; j++)
// A                     sum += B[j] * (R[i - j] + R[i + j]);
// A                 I[k * xdim + i - size] = (float)sum;
// A             }
// A         }
// A 
// A         // convolution of each column of the input image
// A         //List<double> T = (double*)xmalloc((size + ydim + size) * sizeof*T);
// A         List<double> T = (new double[size + ydim + size]).ToList();
// A 
// A         for (int k = 0; k < xdim; k++)
// A         {
// A             int i, j;
// A             for (i = size; i < bdy; i++)
// A                 T[i] = I[(i - size) * xdim + k];
// A 
// A             switch (boundary_condition)
// A             {
// A                 case BOUNDARY_CONDITION_DIRICHLET:
// A                     for (i = 0, j = bdy; i < size; i++, j++)
// A                         T[i] = T[j] = 0;
// A                     break;
// A 
// A                 case BOUNDARY_CONDITION_REFLECTING:
// A                     for (i = 0, j = bdy; i < size; i++, j++)
// A                     {
// A                         T[i] = I[(size - i) * xdim + k];
// A                         T[j] = I[(ydim - i - 1) * xdim + k];
// A                     }
// A                     break;
// A 
// A                 case BOUNDARY_CONDITION_PERIODIC:
// A                     for (i = 0, j = bdx; i < size; i++, j++)
// A                     {
// A                         T[i] = I[(ydim - size + i) * xdim + k];
// A                         T[j] = I[i * xdim + k];
// A                     }
// A                     break;
// A             }
// A 
// A             for (i = size; i < bdy; i++)
// A             {
// A                 double sum = B[0] * T[i];
// A                 for (j = 1; j < size; j++)
// A                     sum += B[j] * (T[i - j] + T[i + j]);
// A                 I[(i - size) * xdim + k] = (float)sum;
// A             }
// A         }
// A     }
// A 
// A 
// A 
// A 
// A 
// A 
// A 
// A 
// A 
// A     // This program is free software: you can use, modify and/or redistribute it
// A     // under the terms of the simplified BSD License. You should have received a
// A     // copy of this license along this program. If not, see
// A     // <http://www.opensource.org/licenses/bsd-license.html>.
// A     //
// A     // Copyright (C) 2012, Javier Sánchez Pérez <jsanchez@dis.ulpgc.es>
// A     // All rights reserved.
// A 
// A 
// A     //# ifndef BICUBIC_INTERPOLATION_C
// A     //#define BICUBIC_INTERPOLATION_C
// A     //
// A     //# include <stdbool.h>
// A 
// A     int BOUNDARY_CONDITION = 0;
// A     //0 Neumann
// A     //1 Periodic
// A     //2 Symmetric
// A 
// A     /**
// A       *
// A       * Neumann boundary condition test
// A       *
// A     **/
// A     static int neumann_bc(int x, int nx, List<bool> out_bool)
// A     {
// A         if (x < 0)
// A         {
// A             x = 0;
// A             out_bool[0] = true;
// A         }
// A         else if (x >= nx)
// A         {
// A             x = nx - 1;
// A             out_bool[0] = true;
// A         }
// A 
// A         return x;
// A     }
// A 
// A     /**
// A       *
// A       * Periodic boundary condition test
// A       *
// A     **/
// A     static int periodic_bc(int x, int nx, List<bool> out_bool)
// A     {
// A         if (x < 0)
// A         {
// A             int n = 1 - (int)(x / (nx + 1));
// A             int ixx = x + n * nx;
// A 
// A             x = ixx % nx;
// A             out_bool[0] = true;
// A         }
// A         else if (x >= nx)
// A         {
// A             x = x % nx;
// A             out_bool[0] = true;
// A         }
// A 
// A         return x;
// A     }
// A 
// A 
// A     /**
// A       *
// A       * Symmetric boundary condition test
// A       *
// A     **/
// A     static int symmetric_bc(int x, int nx, List<bool> out_bool)
// A     {
// A         if (x < 0)
// A         {
// A             int borde = nx - 1;
// A             int xx = -x;
// A             int n = (int)(xx / borde) % 2;
// A 
// A             if (n == 1)
// A             {
// A                 x = borde - (xx % borde);
// A             }
// A             else
// A             {
// A                 x = xx % borde;
// A             }
// A             out_bool[0] = true;
// A         }
// A 
// A         else if (x >= nx)
// A         {
// A             int borde = nx - 1;
// A             int n = (int)(x / borde) % 2;
// A 
// A             if (n == 1)
// A             {
// A                 x = borde - (x % borde);
// A             }
// A             else
// A             {
// A                 x = x % borde;
// A             }
// A             out_bool[0] = true;
// A         }
// A 
// A         return x;
// A     }
// A 
// A 
// A     /**
// A       *
// A       * Cubic interpolation in one dimension
// A       *
// A     **/
// A     static double cubic_interpolation_cell(
// A         List<double> v, //[4],  //interpolation points
// A         double x      //point to be interpolated
// A     )
// A     {
// A         return v[1] + 0.5 * x * (v[2] - v[0] +
// A             x * (2.0 * v[0] - 5.0 * v[1] + 4.0 * v[2] - v[3] +
// A             x * (3.0 * (v[1] - v[2]) + v[3] - v[0])));
// A     }
// A 
// A 
// A     /**
// A       *
// A       * Bicubic interpolation in two dimensions
// A       *
// A     **/
// A     double bicubic_interpolation_cell(
// A         List<List<double>> p, //p[4][4], //array containing the interpolation points
// A         double x,       //x position to be interpolated
// A         double y        //y position to be interpolated
// A     )
// A     {
// A         List<double> v = doubles_of_size(4);//doubles_of_size(4);
// A         v[0] = cubic_interpolation_cell(p[0], y);
// A         v[1] = cubic_interpolation_cell(p[1], y);
// A         v[2] = cubic_interpolation_cell(p[2], y);
// A         v[3] = cubic_interpolation_cell(p[3], y);
// A         return cubic_interpolation_cell(v, x);
// A     }
// A 
// A     /**
// A       *
// A       * Compute the bicubic interpolation of a point in an image.
// A       * Detect if the point goes outside the image domain.
// A       *
// A     **/
// A     float bicubic_interpolation_at(
// A 
// A     List<float> input, //image to be interpolated
// A     float uu,    //x component of the vector field
// A     float vv,    //y component of the vector field
// A     int nx,    //image width
// A     int ny,    //image height
// A     bool border_out //if true, return zero outside the region
// A )
// A     {
// A         //int sx = (uu < 0) ? -1 : 1;
// A         //int sy = (vv < 0) ? -1 : 1;
// A 
// A         int sx, sy;
// A         if (uu < 0) { sx = -1; } else { sx = 1; }
// A         if (vv < 0) { sy = -1; } else { sy = 1; }
// A 
// A         int x, y, mx, my, dx, dy, ddx, ddy;
// A         List<bool> out_bool = new List<bool>() { false };
// A 
// A         //apply the corresponding boundary conditions
// A         switch (BOUNDARY_CONDITION)
// A         {
// A             case 0:
// A                 {
// A                     x = neumann_bc((int)uu, nx, out_bool);
// A                     y = neumann_bc((int)vv, ny, out_bool);
// A                     mx = neumann_bc((int)uu - sx, nx, out_bool);
// A                     my = neumann_bc((int)vv - sx, ny, out_bool);
// A                     dx = neumann_bc((int)uu + sx, nx, out_bool);
// A                     dy = neumann_bc((int)vv + sy, ny, out_bool);
// A                     ddx = neumann_bc((int)uu + 2 * sx, nx, out_bool);
// A                     ddy = neumann_bc((int)vv + 2 * sy, ny, out_bool);
// A                     break;
// A                 }
// A             case 1:
// A                 {
// A                     x = periodic_bc((int)uu, nx, out_bool);
// A                     y = periodic_bc((int)vv, ny, out_bool);
// A                     mx = periodic_bc((int)uu - sx, nx, out_bool);
// A                     my = periodic_bc((int)vv - sx, ny, out_bool);
// A                     dx = periodic_bc((int)uu + sx, nx, out_bool);
// A                     dy = periodic_bc((int)vv + sy, ny, out_bool);
// A                     ddx = periodic_bc((int)uu + 2 * sx, nx, out_bool);
// A                     ddy = periodic_bc((int)vv + 2 * sy, ny, out_bool);
// A                     break;
// A                 }
// A             case 2:
// A                 {
// A                     x = symmetric_bc((int)uu, nx, out_bool);
// A                     y = symmetric_bc((int)vv, ny, out_bool);
// A                     mx = symmetric_bc((int)uu - sx, nx, out_bool);
// A                     my = symmetric_bc((int)vv - sx, ny, out_bool);
// A                     dx = symmetric_bc((int)uu + sx, nx, out_bool);
// A                     dy = symmetric_bc((int)vv + sy, ny, out_bool);
// A                     ddx = symmetric_bc((int)uu + 2 * sx, nx, out_bool);
// A                     ddy = symmetric_bc((int)vv + 2 * sy, ny, out_bool);
// A                     break;
// A                 }
// A             default:
// A                 {
// A                     x = neumann_bc((int)uu, nx, out_bool);
// A                     y = neumann_bc((int)vv, ny, out_bool);
// A                     mx = neumann_bc((int)uu - sx, nx, out_bool);
// A                     my = neumann_bc((int)vv - sx, ny, out_bool);
// A                     dx = neumann_bc((int)uu + sx, nx, out_bool);
// A                     dy = neumann_bc((int)vv + sy, ny, out_bool);
// A                     ddx = neumann_bc((int)uu + 2 * sx, nx, out_bool);
// A                     ddy = neumann_bc((int)vv + 2 * sy, ny, out_bool);
// A                     break;
// A                 }
// A         }
// A 
// A         if (out_bool[0] && border_out)
// A         {
// A             return 0f;
// A         }
// A         else
// A         {
// A             //obtain the interpolation points of the image
// A             float p11 = input[mx + nx * my];
// A             float p12 = input[x + nx * my];
// A             float p13 = input[dx + nx * my];
// A             float p14 = input[ddx + nx * my];
// A 
// A             float p21 = input[mx + nx * y];
// A             float p22 = input[x + nx * y];
// A             float p23 = input[dx + nx * y];
// A             float p24 = input[ddx + nx * y];
// A 
// A             float p31 = input[mx + nx * dy];
// A             float p32 = input[x + nx * dy];
// A             float p33 = input[dx + nx * dy];
// A             float p34 = input[ddx + nx * dy];
// A 
// A             float p41 = input[mx + nx * ddy];
// A             float p42 = input[x + nx * ddy];
// A             float p43 = input[dx + nx * ddy];
// A             float p44 = input[ddx + nx * ddy];
// A 
// A             //create array
// A             //double pol[4][4] = {
// A             //    { p11, p21, p31, p41},
// A             //		{ p12, p22, p32, p42},
// A             //		{ p13, p23, p33, p43},
// A             //		{ p14, p24, p34, p44}
// A             //};
// A 
// A             List<List<double>> pol = doubles_of_size(4, 4);// new double[4][4];
// A             pol[0][0] = p11;//p11;
// A             pol[0][1] = p21;//p12;
// A             pol[0][2] = p31;//p13;
// A             pol[0][3] = p41;//p14;
// A             pol[1][0] = p12;//p21;
// A             pol[1][1] = p22;//p22;
// A             pol[1][2] = p32;//p23;
// A             pol[1][3] = p42;//p24;
// A             pol[2][0] = p13;//p31;
// A             pol[2][1] = p23;//p32;
// A             pol[2][2] = p33;//p33;
// A             pol[2][3] = p43;//p34;
// A             pol[3][0] = p14;//p41;
// A             pol[3][1] = p24;//p42;
// A             pol[3][2] = p34;//p43;
// A             pol[3][3] = p44;//p44;
// A 
// A             pol = nans2zero(pol);
// A 
// A             //return interpolation
// A             double interpol_val = bicubic_interpolation_cell(pol, uu - x, vv - y);
// A             return (float)interpol_val;
// A         }
// A     }
// A 
// A 
// A     public List<List<double>> nans2zero(List<List<double>> pol)
// A     {
// A         for (int i = 0; i < pol.Count; i++)
// A         {
// A             for (int j = 0; j < pol[i].Count; j++)
// A             {
// A                 double pol_el = pol[i][j];
// A                 if (double.IsNaN(pol_el))
// A                 {
// A                     pol[i][j] = 0f;
// A                 }
// A             }
// A         }
// A 
// A         return pol;
// A     }
// A 
// A     /**
// A       *
// A       * Compute the bicubic interpolation of an image.
// A       *
// A     **/
// A     void bicubic_interpolation_warp(
// A         List<float> input,     // image to be warped
// A         List<float> u,         // x component of the vector field
// A         List<float> v,         // y component of the vector field
// A         List<float> output,    // image warped with bicubic interpolation
// A         int nx,        // image width
// A         int ny,        // image height
// A         bool border_out // if true, put zeros outside the region
// A     )
// A     {
// A         for (int i = 0; i < ny; i++)
// A         {
// A             for (int j = 0; j < nx; j++)
// A             {
// A                 int p = i * nx + j;
// A                 float uu = (float)(j + u[p]);
// A                 float vv = (float)(i + v[p]);
// A                 if (p == 17)
// A                 {
// A                     int a_l = 1 + 1;
// A                 }
// A                 if (p == 129)
// A                 {
// A                     ;
// A                 }
// A                 // obtain the bicubic interpolation at position (uu, vv)
// A                 output[p] = bicubic_interpolation_at(input,
// A                         uu, vv, nx, ny, border_out);
// A             }
// A         }
// A     }
// A 
// A 
// A 
// A         // info (paul): zoom.c
// A         // This program is free software: you can use, modify and/or redistribute it
// A         // under the terms of the simplified BSD License. You should have received a
// A         // copy of this license along this program. If not, see
// A         // <http://www.opensource.org/licenses/bsd-license.html>.
// A         //
// A         // Copyright (C) 2012, Javier Sánchez Pérez <jsanchez@dis.ulpgc.es>
// A         // All rights reserved.
// A 
// A         double ZOOM_SIGMA_ZERO = 0.6;
// A 
// A         /**
// A           *
// A           * Compute the size of a zoomed image from the zoom factor
// A           *
// A         **/
// A         (int, int) zoom_size(
// A             int nx,      // width of the orignal image
// A             int ny,      // height of the orignal image
// A                          //int nxx,    // width of the zoomed image
// A                          //int nyy,    // height of the zoomed image
// A             float factor // zoom factor between 0 and 1
// A         )
// A         {
// A             //compute the new size corresponding to factor
// A             //we add 0.5 for rounding off to the closest number
// A             int nxx = (int)((float)nx * factor + 0.5);
// A             int nyy = (int)((float)ny * factor + 0.5);
// A             return (nxx, nyy);
// A         }
// A         /**
// A           *
// A           * Downsample an image
// A           *
// A         **/
// A         void zoom_out(
// A         List<float> I,    // input image
// A         List<float> Iout,       // output image
// A         int nx,      // image width
// A         int ny,      // image height
// A         float factor // zoom factor between 0 and 1
// A     )
// A         {
// A             // temporary working image
// A             //float* Is = (float*)xmalloc(nx * ny * sizeof*Is);
// A             List<float> Is = (new float[nx * ny]).ToList();
// A             for (int i = 0; i < nx * ny; i++)
// A             {
// A                 Is[i] = I[i];
// A             }
// A 
// A             // compute the size of the zoomed image
// A             (int nxx, int nyy) = zoom_size(nx, ny, factor);
// A             
// A             // compute the Gaussian sigma for smoothing
// A             float sigma = (float)(ZOOM_SIGMA_ZERO * Math.Sqrt(1d / ((double)(factor * factor)) - 1d));
// A             
// A             // pre-smooth the image
// A             gaussian(Is, nx, ny, sigma);
// A             
// A             // re-sample the image using bicubic interpolation
// A             for (int i1 = 0; i1 < nyy; i1++)
// A             {
// A                 for (int j1 = 0; j1 < nxx; j1++)
// A                 {
// A                     float i2 = (float)i1 / factor;
// A                     float j2 = (float)j1 / factor;
// A 
// A                     double g = bicubic_interpolation_at(Is, j2, i2, nx, ny, false);
// A                     Iout[i1 * nxx + j1] = (float)g;
// A                 }
// A             }
// A         }
// A 
// A 
// A     /**
// A       *
// A       * Function to upsample the image
// A       *
// A     **/
// A 
// A     int i_c = 126;
// A     int j_c = 166;
// A     void zoom_in(
// A         List<float> I, // input image
// A         List<float> Iout,    // output image
// A         int nx,         // width of the original image
// A         int ny,         // height of the original image
// A         int nxx,        // width of the zoomed image
// A         int nyy,         // height of the zoomed image
// A         bool with_dt
// A     )
// A     {
// A         // compute the zoom factor
// A         float factorx = ((float)nxx / nx);
// A         float factory = ((float)nyy / ny);
// A 
// A         // re-sample the image using bicubic interpolation
// A         for (int i1 = 0; i1 < nyy; i1++)
// A         {
// A             for (int j1 = 0; j1 < nxx; j1++)
// A             {
// A                 float i2 = (float)i1 / factory;
// A                 float j2 = (float)j1 / factorx;
// A 
// A                 if (i1 == i_c && j1 == j_c && !with_dt)
// A                 {
// A                     ;
// A                 }
// A 
// A                 float g = (float)bicubic_interpolation_at(I, j2, i2, nx, ny, false);
// A                 Iout[i1 * nxx + j1] = g;
// A             }
// A         }
// A     }
// A }
// A public class Actioner
// A {
// A     // info (paul): A class containing an action, 
// A     //      but also some other possibly useful parameters, 
// A     //      e.g. a label
// A     public string label;
// A     public Action act;
// A 
// A     public int val_0;
// A     public int val_1;
// A 
// A     public Actioner(Action act, string label)
// A     {
// A         this.act = act;
// A         this.label = label;
// A     }
// A 
// A }
// A 
// A public class im_dressed
// A {
// A     public List<float> im_vec;
// A     public int width;
// A     public int height;
// A 
// A     public im_dressed(List<float> im_vec, int width, int height)
// A     {
// A         this.im_vec = im_vec;
// A         this.width = width;
// A         this.height = height;
// A     }
// A }