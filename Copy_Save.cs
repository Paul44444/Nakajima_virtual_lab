using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Copy_Save : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    public Transform canvas;
    public Transform design_exp_panel;
    public Transform designer;

    // Start is called before the first frame update
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        canvas = GameObject.Find("Canvas").transform;
        design_exp_panel = canvas.Find("design_exp_panel");
        designer = canvas.transform.Find("design_exp_panel");

        gameObject.GetComponent<Button>().onClick.AddListener(on_click);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void on_click()
    {
        Transform path_now_label = designer.Find("path_now_label");
        string text = path_now_label.GetComponent<TextMeshProUGUI>().text;
        GUIUtility.systemCopyBuffer = text;
    }
}
