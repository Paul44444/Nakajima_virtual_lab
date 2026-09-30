using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
//using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;
//using static UnityEngine.UIElements.UxmlAttributeDescription;

public class Choose_panel : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    public Transform canvas;
    Transform designer;

    // Start is called before the first frame update
    /// <summary>
    /// Unity callback (unused; initialisation happens in do_start).
    /// </summary>
    void Start()
    {
    }

    /// <summary>
    /// Initialises the image-path panel (references to the lab controller and the canvas).
    /// </summary>
    public void do_start()
    {   
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        canvas = GameObject.Find("Canvas").transform;
        designer = transform;
        /*
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
        */

        // info (paul): implement in the current lab:
        //ExpConfig pars = vis.get_config_now();
        //string im0_path = "C:/Users/Paul/Desktop/DIC_2025_for_travel/DIC_package/lighting_07/cam_0/uv/mucim_15_r128.png";//vis.path_dic;//vis.path_dic;
        string im0_path = "C:/Users/Paul/Downloads/geschwaerzt/geschwaerzt_50/overlay_with_masks_001433.png";//"C:/Users/Paul/Downloads/Im014600.png";//21012026 "C:/Users/Paul/Downloads/image-000250.png";
        Transform im0_label = designer.Find("im0_label");
        TextMeshProUGUI im0_comp = im0_label.GetComponent<TextMeshProUGUI>();
        im0_comp.text = im0_path;

        //string im1_path = "C:/Users/Paul/Desktop/DIC_2025_for_travel/DIC_package/lighting_07/cam_0/uv/mucim_16_r128.png";//vis.path_dic;//vis.get_blade_path();
        string im1_path = "C:/Users/Paul/Downloads/geschwaerzt/geschwaerzt_50/overlay_with_masks_003366.png";//"C:/Users/Paul/Downloads/Im014700.png";//21012026 "C:/Users/Paul/Downloads/image-000260.png";
        Transform im1_label = designer.Find("im1_label");
        TextMeshProUGUI im1_comp = im1_label.GetComponent<TextMeshProUGUI>();
        im1_comp.text = im1_path;
    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {
        
    }
}
