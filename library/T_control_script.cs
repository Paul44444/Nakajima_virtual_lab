using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class T_control_script : MonoBehaviour
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
        vis.init_t_control(this);

        forward_but = transform.Find("forward_but");
        back_but = transform.Find("back_but");
        t_panel = transform.Find("t_panel");

        forward_but.GetComponent<Button>().onClick.AddListener(forward_action);
        back_but.GetComponent<Button>().onClick.AddListener(back_action);

        //vis.refresh_plane_with_params();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("a"))
        {
            back_action();
        }
        if (Input.GetKeyDown("d"))
        {
            forward_action();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            back_action();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            forward_action();
        }
    }

    public void refresh_t_panel(int t_idx)
    {
        Transform t_label = t_panel.Find("t_label");
        t_label.GetComponent<TextMeshProUGUI>().text = "t_idx: " + t_idx.ToString();

    }

    public void forward_action()
    {
        int t_idx_now = vis.get_t_idx();
        vis.set_t_idx(t_idx_now + 1);
        vis.refresh_plane_with_params();
    }

    public void back_action()
    {
        int t_idx_now = vis.get_t_idx();
        vis.set_t_idx(t_idx_now - 1);
        vis.refresh_plane_with_params();
    }
}
