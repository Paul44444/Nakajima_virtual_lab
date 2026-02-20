using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Strain_D : MonoBehaviour
{
    GameObject sphere;
    vis_3D vis;

    Transform forward_but;
    Transform back_but;
    Transform t_panel;

    // info (paul): This script controls the 
    //      switch, from where you decide whether to 
    //      plot the x- or y- derivative of the strain

    // Start is called before the first frame update
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        vis.init_strain_d_control(this);

        forward_but = transform.Find("forward_but");
        back_but = transform.Find("back_but");
        t_panel = transform.Find("info_panel");

        forward_but.GetComponent<Button>().onClick.AddListener(forward_action);
        back_but.GetComponent<Button>().onClick.AddListener(back_action);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void refresh_info(string strain_mode)
    {
        Transform t_label = t_panel.Find("t_label");
        t_label.GetComponent<TextMeshProUGUI>().text = "sm: " + strain_mode;

    }


    public void forward_action()
    {
        string strain_mode_now = vis.get_strain_d_mode();
        string strain_mode_next = null;

        if (strain_mode_now == "x")
        {
            strain_mode_next = "y";
        }

        if (strain_mode_now == "y")
        {
            strain_mode_next = "x";
        }

        vis.set_strain_d_mode(strain_mode_next);
        vis.refresh_plane_with_params();
    }

    public void back_action()
    {
        forward_action();
        vis.refresh_plane_with_params();
    }
}
