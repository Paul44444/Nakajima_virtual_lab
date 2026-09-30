using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
//using static UnityEditor.ShaderData;

public class Confirm_ims_but : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    public Transform canvas;
    public Transform design_exp_panel;
    public Transform designer;

    public Transform choose_panel;

    // Start is called before the first frame update
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        canvas = GameObject.Find("Canvas").transform;
        design_exp_panel = canvas.Find("design_exp_panel");
        //designer = canvas.transform.Find("design_exp_panel");

        choose_panel = canvas.transform.Find("Choose_panel");

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
        // info (paul): set save path:
        Transform input_field_im0 = choose_panel.Find("input_field_im0");//13122025 ("input_field_im0");
        string im0_str = input_field_im0.GetComponent<TMP_InputField>().text;//13122025 <TMP_InputField>().text;
        if (im0_str == null || im0_str == "")
        {
            //13122025 im0_str = vis.get_path_dic() + "/output";
            input_field_im0 = choose_panel.Find("im0_label");
            im0_str = input_field_im0.GetComponent<TextMeshProUGUI>().text;
        }
        vis.get_config_now().set_save_path(im0_str);

        // info (paul): assign new path to im0_label, which shows the info of the current 
        //      path to the user
        Transform im0_label = choose_panel.Find("im0_label");
        TextMeshProUGUI im0_comp = im0_label.GetComponent<TextMeshProUGUI>();
        im0_comp.text = im0_str;

        // info (paul): assign blade_path
        Transform input_field_im1 = choose_panel.Find("input_field_im1");//13122025 ("input_field_im1");
        string im1_path = input_field_im1.GetComponent<TMP_InputField>().text;
        if (im1_path == null || im1_path == "")
        {
            //A blade_path = "C:/Users/Paul/Desktop/DIC_2025_for_travel/play_blender_pycahrm/" + 
            //A     "write_mesh/" + "mucverts_29.txt";
            //13122025 im1_path = vis.get_blade_path();
            input_field_im1 = choose_panel.Find("im1_label");
            im1_path = input_field_im1.GetComponent<TextMeshProUGUI>().text;
        }
        vis.get_config_now().set_blade_path(im1_path);
        Transform im1_label = choose_panel.Find("im1_label");
        TextMeshProUGUI im1_comp = im1_label.GetComponent<TextMeshProUGUI>();
        im1_comp.text = im1_path;

        List<string> ims = new List<string>() { im0_str, im1_path, im0_str };
        vis.set_im_paths(ims);
    }
}
