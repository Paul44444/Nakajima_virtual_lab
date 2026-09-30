using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Save_command : MonoBehaviour
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
        //21092026 u- und v-Karte exportieren (statt nur der aktuell gewaehlten Komponente)
        vis.save_maps_u_and_v();
    }
}
