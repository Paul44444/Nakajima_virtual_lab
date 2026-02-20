using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Match_steps : MonoBehaviour
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
        vis.init_match_control(this);

        forward_but = transform.Find("forward_but");
        back_but = transform.Find("back_but");
        t_panel = transform.Find("info_panel");

        forward_but.GetComponent<Button>().onClick.AddListener(forward_action);
        back_but.GetComponent<Button>().onClick.AddListener(back_action);

        //vis.refresh_plane_with_params();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            back_action();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            forward_action();
        }
    }

    public void refresh_panel(int t_idx)
    {
        Transform t_label = t_panel.Find("t_label");
        int match_steps = vis.get_match_steps();
        t_label.GetComponent<TextMeshProUGUI>().text = "match_steps: " + match_steps.ToString();
    }

    public void forward_action()
    {
        int match_steps_now = vis.get_match_steps();
        vis.set_match_steps(match_steps_now + 1);
        vis.refresh_plane_with_params();
    }

    public void back_action()
    {
        int match_steps_now = vis.get_match_steps();
        vis.set_match_steps(match_steps_now - 1);
        vis.refresh_plane_with_params();
    }
}
