using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Switch_exp : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    public Transform toggle;

    // Start is called before the first frame update
    /// <summary>
    /// Unity callback: registers the handler of the with_exp toggle.
    /// </summary>
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        toggle = gameObject.transform.parent;
        Toggle toggle_comp = toggle.GetComponent<Toggle>();
        toggle_comp.onValueChanged.AddListener(delegate { on_click(toggle_comp); });
    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {
        ;
    }
    /// <summary>
    /// Enables or disables rendering of the camera images in the standard analysis (with_exp).
    /// </summary>
    /// <param name="toggle_l">The with_exp toggle.</param>
    void on_click(Toggle toggle_l)
    {
        vis.set_with_exp(toggle_l.isOn);
    }
}
