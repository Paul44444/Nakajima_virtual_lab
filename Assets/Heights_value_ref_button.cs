using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Heights_value_ref_button : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;

    // Start is called before the first frame update
    /// <summary>
    /// Unity callback: registers the click handler.
    /// </summary>
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        gameObject.GetComponent<Button>().onClick.AddListener(on_click);
    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {

    }

    /// <summary>
    /// Shows the reference depth on the height display.
    /// </summary>
    void on_click()
    {
        vis.set_heights_mode("value_ref");
        vis.refresh_plane_with_params();
    }
}
