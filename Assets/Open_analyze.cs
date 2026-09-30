using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Open_analyze : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    public Transform canvas;
    public Transform analyze_panel;
    public Transform switch_panel;

    public Transform save_but;
    public Transform analyze_but;
    public Transform load_but;

    public Transform analyzer_but;
    public Transform switcher_but;
    public Transform designer_but;
    Open_analyze analyzer;
    Open_switch switcher;
    Design_Exp_Button designer;

    // Start is called before the first frame update
    /// <summary>
    /// Unity callback: looks up the analysis panel, its buttons, and the other panels; registers the click handler.
    /// </summary>
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        canvas = GameObject.Find("Canvas").transform;
        analyze_panel = canvas.Find("analyze_panel");
        switch_panel = canvas.Find("switch_panel");

        save_but = canvas.Find("save_but");
        analyze_but = canvas.Find("analyze_but");
        load_but = canvas.Find("load_but");

        analyze_panel.gameObject.SetActive(false);
        save_but.gameObject.SetActive(false);
        analyze_but.gameObject.SetActive(false);
        load_but.gameObject.SetActive(false);
        gameObject.GetComponent<Button>().onClick.AddListener(on_click);

        analyzer_but = canvas.Find("open_analyze_but");
        switcher_but = canvas.Find("open_switch_but");
        designer_but = canvas.Find("design_exp_but");
        analyzer = analyzer_but.GetComponent<Open_analyze>();
        switcher = switcher_but.GetComponent<Open_switch>();
        designer = designer_but.GetComponent<Design_Exp_Button>();
    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {
        
    }

    /// <summary>
    /// Toggles the analysis panel (with the save, analyze, and load buttons) and closes the other panels.
    /// </summary>
    public void on_click()
    {
        designer.close_my();
        switcher.close_my();
        save_but.gameObject.SetActive(!analyze_panel.gameObject.activeSelf);
        analyze_but.gameObject.SetActive(!analyze_panel.gameObject.activeSelf);
        load_but.gameObject.SetActive(!analyze_panel.gameObject.activeSelf);
        analyze_panel.gameObject.SetActive(!analyze_panel.gameObject.activeSelf);
    }
    /// <summary>
    /// Hides the analysis panel and its buttons.
    /// </summary>
    public void close_my()
    {
        save_but.gameObject.SetActive(false);
        analyze_but.gameObject.SetActive(false);
        load_but.gameObject.SetActive(false);
        analyze_panel.gameObject.SetActive(false);
    }
}
