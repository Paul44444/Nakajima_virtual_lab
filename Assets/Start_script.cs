using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Start_script : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    public Transform toggle;

    // Start is called before the first frame update
    /// <summary>
    /// Unity callback: registers the click handler of the Start button.
    /// </summary>
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        gameObject.GetComponent<Button>().onClick.AddListener(on_click);

        //toggle = gameObject.transform.parent;
        //Toggle toggle_comp = toggle.GetComponent<Toggle>();
        //toggle_comp.onValueChanged.AddListener(delegate { on_click(toggle_comp); });
    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {
        
    }

    /// <summary>
    /// Starts the standard analysis (rendering and/or optical flow according to with_exp and with_tv).
    /// </summary>
    void on_click()
    {
        //vis.set_with_exp(toggle_l.isOn);
        vis.set_is_started(true);
    }
}
