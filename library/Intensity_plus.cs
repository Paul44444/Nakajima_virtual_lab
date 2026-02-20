using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Intensity_plus : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    public Transform canvas;

    // Start is called before the first frame update
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        canvas = GameObject.Find("Canvas").transform;
        GetComponent<Button>().onClick.AddListener(plus);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void plus()
    {
        float scale = vis.get_truth_map_scale();
        vis.set_truth_map_scale(scale*2f);



    }
}
