using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Save_params_script : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;

    // Start is called before the first frame update
    /// <summary>
    /// Unity callback: connects the button to the analysis of the current parameters.
    /// </summary>
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        //14032025 gameObject.GetComponent<Button>().onClick.AddListener(vis.analyze_params);
        gameObject.GetComponent<Button>().onClick.AddListener(vis.analyze_params);
    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {
        
    }
}
