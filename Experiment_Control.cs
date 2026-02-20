using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Experiment_Control : MonoBehaviour
{
    GameObject sphere;
    vis_3D vis;

    Transform forward_but;
    Transform back_but;
    Transform t_panel;

    Button but1;
    Button but2;

    // Start is called before the first frame update
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        vis.init_experiment_control(this);

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

    public void refresh_info(string experiment)
    {
        Transform t_label = t_panel.Find("t_label");
        t_label.GetComponent<TextMeshProUGUI>().text = "" + experiment;

    }

    public void forward_action()
    {
        string experiment = vis.get_experiment();
        string experiment_next = null;
        
        for (int i = 0; i < vis.render_acts.Count; i++)
        {
            string label_i = vis.render_acts[i].get_label();
            if (experiment == label_i)
            {
                if (i + 1 < vis.render_acts.Count)
                {
                    experiment_next = vis.render_acts[i + 1].get_label();
                }
                else
                {
                    experiment_next = vis.render_acts[i].get_label();
                }
                
                break;
            }
        }

        //26072024 if (experiment.StartsWith("exp_normal"))
        //26072024 {
        //26072024     experiment_next = "lighting";
        //26072024 }
        //26072024 
        //26072024 if (experiment.StartsWith("lighting"))
        //26072024 {
        //26072024     experiment_next = "speckle";
        //26072024 }
        //26072024 
        //26072024 if (experiment.StartsWith("speckle"))
        //26072024 {
        //26072024     experiment_next = "speckle";
        //26072024 }

        vis.set_experiment(experiment_next);
        vis.refresh_plane_with_params();
    }

    public void back_action()
    {
        string experiment = vis.get_experiment();
        string experiment_next = null;

        for (int i = 0; i < vis.render_acts.Count; i++)
        {
            string label_i = vis.render_acts[i].get_label();
            if (experiment == label_i)
            {
                if (i - 1 >= 0)
                {
                    experiment_next = vis.render_acts[i - 1].get_label();
                }
                else
                {
                    experiment_next = vis.render_acts[i].get_label();
                }
                break;
            }
        }

        //26072024 if (experiment.StartsWith("speckle"))
        //26072024 {
        //26072024     experiment_next = "lighting";
        //26072024 }
        //26072024 
        //26072024 if (experiment.StartsWith("lighting"))
        //26072024 {
        //26072024     experiment_next = "exp_normal";
        //26072024 }
        //26072024 
        //26072024 if (experiment.StartsWith("exp_normal"))
        //26072024 {
        //26072024     experiment_next = "exp_normal";
        //26072024 }

        vis.set_experiment(experiment_next);
        vis.refresh_plane_with_params();
    }
}
