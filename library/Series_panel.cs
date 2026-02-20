using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Series_panel : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    public Transform canvas;
    Transform designer;

    // info (paul): evaluate as an image series

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void do_start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        canvas = GameObject.Find("Canvas").transform;
        designer = transform;

        string in_dir = "C:/Users/Paul/Downloads/geschwaerzt/muc/";//geschwaerzt_50/";//"C:/Users/Paul/Downloads/Im014600.png";//21012026 "C:/Users/Paul/Downloads/image-000250.png";
        Transform im0_label = designer.Find("in_label");
        TextMeshProUGUI im0_comp = im0_label.GetComponent<TextMeshProUGUI>();
        im0_comp.text = in_dir;

        //string im1_path = "C:/Users/Paul/Desktop/DIC_2025_for_travel/DIC_package/lighting_07/cam_0/uv/mucim_16_r128.png";//vis.path_dic;//vis.get_blade_path();
        string out_dir = "C:/Users/Paul/Downloads/geschwaerzt/flow/";//"C:/Users/Paul/Downloads/Im014700.png";//21012026 "C:/Users/Paul/Downloads/image-000260.png";
        Transform im1_label = designer.Find("out_label");
        TextMeshProUGUI im1_comp = im1_label.GetComponent<TextMeshProUGUI>();
        im1_comp.text = out_dir;
    }
}