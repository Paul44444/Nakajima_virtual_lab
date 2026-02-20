using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U_V : MonoBehaviour
{
    GameObject sphere;
    vis_3D vis;

    Transform forward_but;
    Transform back_but;
    Transform t_panel;

    // Start is called before the first frame update
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        vis.init_u_v_control(this);

        forward_but = transform.Find("forward_but");
        back_but = transform.Find("back_but");
        t_panel = transform.Find("info_panel");

        forward_but.GetComponent<Button>().onClick.AddListener(forward_action);
        back_but.GetComponent<Button>().onClick.AddListener(back_action);
    }

    // Update is called once per frame
    void Update()
    {
        ; ;
        ;
    }

    public void refresh_info(string u_v_mode)
    {
        Transform t_label = t_panel.Find("t_label");
        t_label.GetComponent<TextMeshProUGUI>().text = "" + u_v_mode;

    }

    public void forward_action()
    {
        string u_v_mode_now = vis.get_u_v_mode();
        string u_v_mode_next = null;

        if (u_v_mode_now == "u")
        {
            u_v_mode_next = "v";
        }

        if (u_v_mode_now == "v")
        {
            u_v_mode_next = "z";
        }

        if (u_v_mode_now == "z")
        {
            u_v_mode_next = "z";
        }

        vis.set_u_v_mode(u_v_mode_next);
        vis.refresh_plane_with_params();
    }

    public void back_action()
    {
        //forward_action();
        string u_v_mode_now = vis.get_u_v_mode();
        string u_v_mode_next = null;

        if (u_v_mode_now == "z")
        {
            u_v_mode_next = "v";
        }

        if (u_v_mode_now == "v")
        {
            u_v_mode_next = "u";
        }

        if (u_v_mode_now == "u")
        {
            u_v_mode_next = "u";
        }

        vis.set_u_v_mode(u_v_mode_next);
        vis.refresh_plane_with_params();
    }
}
