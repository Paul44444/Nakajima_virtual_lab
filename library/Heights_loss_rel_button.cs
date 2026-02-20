using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Heights_loss_rel_button : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;

    // Start is called before the first frame update
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        gameObject.GetComponent<Button>().onClick.AddListener(on_click);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void on_click()
    {
        vis.set_heights_mode("loss_rel");
        vis.refresh_plane_with_params();
    }
}
