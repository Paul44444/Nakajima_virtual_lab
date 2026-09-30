using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Force_flat : MonoBehaviour
{
    GameObject sphere;
    vis_3D vis;

    bool force_flat = true;

    // Start is called before the first frame update
    /// <summary>
    /// Unity callback: registers the toggle handler and looks up the lab controller.
    /// </summary>
    void Start()
    {
        gameObject.GetComponent<Toggle>().onValueChanged.AddListener(
            (value) => { toggle_action(value); });

        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();


    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {

    }

    /// <summary>
    /// Switches the flat display of the sample (map on a plane instead of the 3D surface) and refreshes the view.
    /// </summary>
    /// <param name="value">True for the flat display.</param>
    public void toggle_action(bool value)
    {
        vis.set_force_flat(value);
        vis.refresh_plane_with_params();
    }
}
