using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Open_switch : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    public Transform canvas;
    public Transform analyze_panel;
    public Transform switch_panel;

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
        analyze_panel = canvas.Find("analyze_panel");
        switch_panel = canvas.Find("switch_panel");

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
        
    }
    
    public void on_click()
    {
        analyzer.close_my();
        designer.close_my();
        switch_panel.gameObject.SetActive(!switch_panel.gameObject.activeSelf);
    }
    public void close_my()
    {
        switch_panel.gameObject.SetActive(false);
    }
}
