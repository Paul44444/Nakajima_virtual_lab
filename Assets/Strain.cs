using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Strain : MonoBehaviour
{
    GameObject sphere;
    vis_3D vis;

    Transform forward_but;
    Transform back_but;
    Transform t_panel;

    // Start is called before the first frame update
    /// <summary>
    /// Unity callback: registers this control at the lab controller and wires the forward/back buttons.
    /// </summary>
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        vis.init_strain_control(this);

        forward_but = transform.Find("forward_but");
        back_but = transform.Find("back_but");
        t_panel = transform.Find("info_panel");

        forward_but.GetComponent<Button>().onClick.AddListener(forward_action);
        back_but.GetComponent<Button>().onClick.AddListener(back_action);
    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {
        
    }
    
    /// <summary>
    /// Shows the current strain display mode in the label.
    /// </summary>
    /// <param name="strain_mode">Strain mode to display.</param>
    public void refresh_info(string strain_mode)
    {
        Transform t_label = t_panel.Find("t_label");
        t_label.GetComponent<TextMeshProUGUI>().text = "sm: " + strain_mode;

    }

    /// <summary>
    /// Cycles to the next strain display mode (normal, derivatives) and refreshes the view.
    /// </summary>
    public void forward_action()
    {
        string strain_mode_now = vis.get_strain_mode();
        string strain_mode_next = null;

        if (strain_mode_now == "normal")
        {
            strain_mode_next = "derivative_1";
        }

        if (strain_mode_now == "derivative_1")
        {
            strain_mode_next = "strain_rate";
        }

        if (strain_mode_now == "strain_rate")
        {
            strain_mode_next = "normal";
        }
        _ = 1 + 1;
        vis.set_strain_mode(strain_mode_next);
        vis.refresh_plane_with_params();
    }

    /// <summary>
    /// Cycles the strain display mode (same as forward) and refreshes the view.
    /// </summary>
    public void back_action()
    {
        forward_action();
        vis.refresh_plane_with_params();
    }
}
