using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Globalization;
//using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class Designer : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    public Transform canvas;
    Transform designer;

    // Start is called before the first frame update
    void Start()
    {
        ;
    }

    public void do_start()
    {   
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        canvas = GameObject.Find("Canvas").transform;
        designer = transform;

        Transform cam_panel_1 = designer.Find("cam_panel_1");
        Transform cam_panel_2 = designer.Find("cam_panel_2");

        Transform cam_header_1 = cam_panel_1.Find("cam_header");
        Transform cam_header_2 = cam_panel_2.Find("cam_header");

        cam_header_1.GetComponent<TextMeshProUGUI>().text = "Cam 1";
        cam_header_2.GetComponent<TextMeshProUGUI>().text = "Cam 2";
        
        // info (paul): default values:
        Transform pos1_x_field = cam_panel_1.Find("pos_x_field");
        Transform pos1_y_field = cam_panel_1.Find("pos_y_field");
        Transform pos2_x_field = cam_panel_2.Find("pos_x_field");
        Transform pos2_y_field = cam_panel_2.Find("pos_y_field");
        pos1_x_field.GetComponent<TMP_InputField>().text = "0";
        pos1_y_field.GetComponent<TMP_InputField>().text = "500";
        pos2_x_field.GetComponent<TMP_InputField>().text = "0";
        pos2_y_field.GetComponent<TMP_InputField>().text = "500";
        
        // info (paul): assign fov, label and diameter, cam_angle
        Transform field_fov = designer.Find("info_fov").Find("field_fov");
        Transform field_label = designer.Find("info_label").Find("field_label");
        Transform field_diameter = designer.Find("info_diameter").Find("field_diameter");
        Transform field_cam_angle = designer.Find("info_cam_angle").Find("field_cam_angle");
        field_fov.GetComponent<TMP_InputField>().text = "30";
        field_label.GetComponent<TMP_InputField>().text = "Setup_1";
        field_diameter.GetComponent<TMP_InputField>().text = "0.07";
        field_cam_angle.GetComponent<TMP_InputField>().text = "10";

        // info (paul): implement in the current lab:
        //ExpConfig pars = vis.get_config_now();

        string path_str = vis.path_dic;
        Transform path_now_label = designer.Find("path_now_label");
        TextMeshProUGUI path_now = path_now_label.GetComponent<TextMeshProUGUI>();
        path_now.text = path_str;

        string blade_path = vis.get_blade_path();
        Transform blade_now_label = designer.Find("blade_now_label");
        TextMeshProUGUI blade_now = blade_now_label.GetComponent<TextMeshProUGUI>();
        blade_now.text = blade_path;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
