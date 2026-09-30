using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
//using static UnityEditor.ShaderData;

public class Confirm_but : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    public Transform canvas;
    public Transform design_exp_panel;
    public Transform designer;
    
    // Start is called before the first frame update
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        canvas = GameObject.Find("Canvas").transform;
        design_exp_panel = canvas.Find("design_exp_panel");
        designer = canvas.transform.Find("design_exp_panel");

        gameObject.GetComponent<Button>().onClick.AddListener(on_click);

        //exp_config = vis.start_exp_from_config_now();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void on_click()
    {
        collect_vals();
    }

    public void collect_vals()
    {
        // info (paul): obtaining parameters from the inputs of all the specific input places

        // info (paul): collect camera positions
        Transform cam_panel_1 = designer.Find("cam_panel_1");
        Transform cam_panel_2 = designer.Find("cam_panel_2");

        Transform pos1_x_field = cam_panel_1.Find("pos_x_field");
        Transform pos1_y_field = cam_panel_1.Find("pos_y_field");
        Transform pos2_x_field = cam_panel_2.Find("pos_x_field");
        Transform pos2_y_field = cam_panel_2.Find("pos_y_field");
        string pos1_x_str = pos1_x_field.GetComponent<TMP_InputField>().text;
        string pos1_y_str = pos1_y_field.GetComponent<TMP_InputField>().text;
        string pos2_x_str = pos2_x_field.GetComponent<TMP_InputField>().text;
        string pos2_y_str = pos2_y_field.GetComponent<TMP_InputField>().text;

        float pos1_x = float.Parse(pos1_x_str, CultureInfo.InvariantCulture);
        float pos1_y = float.Parse(pos1_y_str, CultureInfo.InvariantCulture);
        float pos2_x = float.Parse(pos2_x_str, CultureInfo.InvariantCulture);
        float pos2_y = float.Parse(pos2_y_str, CultureInfo.InvariantCulture);

        vis.get_config_now().set_cam_pos(0, "x", pos1_x);
        vis.get_config_now().set_cam_pos(0, "y", pos1_y);
        vis.get_config_now().set_cam_pos(1, "x", pos2_x);
        vis.get_config_now().set_cam_pos(1, "y", pos2_y);

        // info (paul): assign fov, label and diameter, cam_angle
        Transform field_fov = designer.Find("info_fov").Find("field_fov");
        Transform field_label = designer.Find("info_label").Find("field_label");
        Transform field_diameter = designer.Find("info_diameter").Find("field_diameter");
        Transform field_cam_angle = designer.Find("info_cam_angle").Find("field_cam_angle");
        string fov_str = field_fov.GetComponent<TMP_InputField>().text;
        string label_str = field_label.GetComponent<TMP_InputField>().text;
        string diameter_str = field_diameter.GetComponent<TMP_InputField>().text;
        string cam_angle_str = field_cam_angle.GetComponent<TMP_InputField>().text;
        string ambient_str = "0.1";//ambient_str;
        float fov = float.Parse(fov_str, CultureInfo.InvariantCulture);
        float diameter = float.Parse(diameter_str, CultureInfo.InvariantCulture);
        float cam_angle = float.Parse(cam_angle_str, CultureInfo.InvariantCulture);
        float ambient = float.Parse(ambient_str, CultureInfo.InvariantCulture);

        vis.get_config_now().set_fov(fov);
        vis.get_config_now().set_diameter(diameter);
        vis.get_config_now().set_label(label_str);
        vis.get_config_now().set_cam_angle(cam_angle);
        vis.get_config_now().set_ambient_intensity(ambient);

        // info (paul): set save path:
        Transform change_path_label = designer.Find("change_path_label");
        Transform path_field = change_path_label.Find("path_field");
        string path_str = path_field.GetComponent<TMP_InputField>().text;
        if (path_str == null || path_str == "")
        {
            path_str = vis.get_path_dic() + "/output";
        }
        vis.get_config_now().set_save_path(path_str);

        // info (paul): assign new path to path_now_label, which shows the info of the current 
        //      path to the user
        Transform path_now_label = designer.Find("path_now_label");
        TextMeshProUGUI path_now = path_now_label.GetComponent<TextMeshProUGUI>();
        path_now.text = path_str;

        //ExpConfig pars = vis.get_config_now();

        // info (paul): assign blade_path
        Transform change_blade_label = designer.Find("change_blade_label");
        Transform blade_field = change_blade_label.Find("blade_field");
        string blade_path = blade_field.GetComponent<TMP_InputField>().text;
        if (blade_path == null || blade_path == "")
        {
            //A blade_path = "C:/Users/Paul/Desktop/DIC_2025_for_travel/play_blender_pycahrm/" + 
            //A     "write_mesh/" + "mucverts_29.txt";
            blade_path = vis.get_blade_path();
        }
        vis.get_config_now().set_blade_path(blade_path);

        Transform blade_now_label = designer.Find("blade_now_label");
        TextMeshProUGUI blade_now = blade_now_label.GetComponent<TextMeshProUGUI>();
        blade_now.text = blade_path;

        // info (paul): implement in the current lab:
        vis.start_exp_from_config_now();
        vis.clean_blades();

        string blade_path_1 = blade_path;
        string blade_path_2 = blade_path.Replace("_28.txt", "_29.txt");//("_30.txt", "_31.txt");

        //string blade_path_1 = "C:/Users/Paul/Desktop/DIC_2025_for_travel/unter2_Windows_native _october/"
        //    + "Assets/verts/mucverts_19.txt";//14
        //string blade_path_2 = "C:/Users/Paul/Desktop/DIC_2025_for_travel/unter2_Windows_native _october/" 
        //    + "Assets/verts/mucverts_20.txt";//15

        GameObject blade_1 = vis.load_blade_from_verts(blade_path: blade_path_1,
            with_uv_init: true, with_collider: true, with_speckles: true);
        blade_1.GetComponent<Vis_action>().check_vis = false;
        blade_1.layer = 7;
        GameObject blade_2 = vis.load_blade_from_verts(blade_path: blade_path_2,
            with_uv_init: true, with_collider: true, with_speckles: true);
        blade_2.layer = 7;
        blade_2.GetComponent<Vis_action>().check_vis = false;

        // info (paul): make sure to have always only one blade in view, otherwise the 
        //      cam raycast for determining the triangle idx might make mistakes
        (List<List<float>> stream_0_u, List<List<float>> stream_0_v) = vis.manage_truth_flow(
            blade_1, blade_2, vis.get_cam_for_uv_0());
        (List<List<float>> stream_1_u, List<List<float>> stream_1_v) = vis.manage_truth_flow(
            blade_1, blade_2, vis.get_cam_for_uv_1());

        vis.set_truth_0_u(stream_0_u);
        vis.set_truth_0_v(stream_0_v);
        vis.set_truth_1_u(stream_1_u);
        vis.set_truth_1_v(stream_1_v);
    }
}
