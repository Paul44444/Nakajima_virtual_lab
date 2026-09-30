using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Res_script : MonoBehaviour
{
    // info (paul): Usually part of the "res_panel", deals with setting
    //          the resolution of the images for tv analysis;
    //          This also controls the toggle buttons;

    public GameObject sphere;
    public vis_3D vis;

    Transform canvas;
    Transform res_panel;
    Transform toggle_128_trans;
    Transform toggle_256_trans;
    Transform toggle_512_trans;
    Transform toggle_1600_trans;
    Toggle toggle_128;
    Toggle toggle_256;
    Toggle toggle_512;
    Toggle toggle_1600;

    // Start is called before the first frame update
    /// <summary>
    /// Unity callback: looks up the resolution toggles of the scene and registers their handlers.
    /// </summary>
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        
        canvas = GameObject.Find("Canvas").transform;
        res_panel = canvas.Find("res_panel");
        toggle_128_trans = res_panel.Find("toggle_128");
        toggle_256_trans = res_panel.Find("toggle_256");
        toggle_512_trans = res_panel.Find("toggle_512");
        toggle_1600_trans = res_panel.Find("toggle_1600");
        toggle_128 = toggle_128_trans.GetComponent<Toggle>();
        toggle_256 = toggle_256_trans.GetComponent<Toggle>();
        toggle_512 = toggle_512_trans.GetComponent<Toggle>();
        toggle_1600 = toggle_1600_trans.GetComponent<Toggle>();

        //21092026 Toggle-Zustand an die (aus PlayerPrefs) geladene Aufloesung angleichen,
        //BEVOR die Listener registriert werden (sonst wuerde refresh_cams ausgeloest).
        int res_now = vis.get_render_res();
        toggle_128.SetIsOnWithoutNotify(res_now == 128);
        toggle_256.SetIsOnWithoutNotify(res_now == 256);
        toggle_512.SetIsOnWithoutNotify(res_now == 512);
        toggle_1600.SetIsOnWithoutNotify(res_now == 1600);

        toggle_128.onValueChanged.AddListener(delegate { to_128(toggle_128); });
        toggle_256.onValueChanged.AddListener(delegate { to_256(toggle_256); });
        toggle_512.onValueChanged.AddListener(delegate { to_512(toggle_512); });
        toggle_1600.onValueChanged.AddListener(delegate { to_1600(toggle_1600); });
    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {
        
    }

    /// <summary>
    /// Sets the render resolution to 128 px and clears the other toggles.
    /// </summary>
    /// <param name="toggle">Toggle that triggered the change.</param>
    void to_128(Toggle toggle)
    {
        vis.set_render_res(128);
        vis.refresh_cams();

        toggle_256.isOn = false;
        toggle_512.isOn = false;
        toggle_1600.isOn = false;
    }
    /// <summary>
    /// Sets the render resolution to 256 px and clears the other toggles.
    /// </summary>
    /// <param name="toggle">Toggle that triggered the change.</param>
    void to_256(Toggle toggle)
    {
        vis.set_render_res(256);
        vis.refresh_cams();
        toggle_128.isOn = false;
        toggle_512.isOn = false;
        toggle_1600.isOn = false;
    }
    /// <summary>
    /// Sets the render resolution to 512 px and clears the other toggles.
    /// </summary>
    /// <param name="toggle">Toggle that triggered the change.</param>
    void to_512(Toggle toggle)
    {
        vis.set_render_res(512);
        vis.refresh_cams();
        toggle_128.isOn = false;
        toggle_256.isOn = false;
        toggle_1600.isOn = false;
    }
    /// <summary>
    /// Sets the render resolution to 1600 px and clears the other toggles.
    /// </summary>
    /// <param name="toggle">Toggle that triggered the change.</param>
    void to_1600(Toggle toggle)
    {
        vis.set_render_res(1600);
        vis.refresh_cams();
        toggle_128.isOn = false;
        toggle_256.isOn = false;
        toggle_512.isOn = false;
    }




}
