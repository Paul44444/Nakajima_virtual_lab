using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
//using Palmmedia.ReportGenerator.Core.Common;

public class Design_Exp_Button : MonoBehaviour
{
    // info (paul): If this button is clicked, the corresponding experiment 
    //      is opened.

    public GameObject sphere;
    public vis_3D vis;
    public Transform canvas;
    public Transform design_exp_panel;
    //public ExpConfig exp_config;

    public Transform analyzer_but;
    public Transform switcher_but;
    public Transform designer_but;
    Open_analyze analyzer;
    Open_switch switcher;
    Design_Exp_Button designer;

    // Start is called before the first frame update
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        canvas = GameObject.Find("Canvas").transform;
        design_exp_panel = canvas.Find("design_exp_panel");
        design_exp_panel.gameObject.SetActive(false);

        Transform switch_panel = canvas.Find("switch_panel");
        switch_panel.gameObject.SetActive(false);

        gameObject.GetComponent<Button>().onClick.AddListener(on_click);

        analyzer_but = canvas.Find("open_analyze_but");
        switcher_but = canvas.Find("open_switch_but");
        designer_but = canvas.Find("design_exp_but");
        analyzer = analyzer_but.GetComponent<Open_analyze>();
        switcher = switcher_but.GetComponent<Open_switch>();
        designer = designer_but.GetComponent<Design_Exp_Button>();
    }

    // Update is called once per frame
    void Update()
    {
        ;
    }

    public void on_click()
    {
        analyzer.close_my();
        switcher.close_my();
        design_exp_panel.gameObject.SetActive(!design_exp_panel.gameObject.activeSelf);
    }

    public void close_my()
    {
        design_exp_panel.gameObject.SetActive(false);
    }
}
