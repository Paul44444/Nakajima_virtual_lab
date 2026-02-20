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
    void Start()
    {
        gameObject.GetComponent<Toggle>().onValueChanged.AddListener(
            (value) => { toggle_action(value); });

        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();


    }

    // Update is called once per frame
    void Update()
    {

    }

    public void toggle_action(bool value)
    {
        vis.set_force_flat(value);
        vis.refresh_plane_with_params();
    }
}
