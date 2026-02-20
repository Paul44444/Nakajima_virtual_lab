using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using System.Windows.Forms;

public class Load_button : MonoBehaviour
{
    System.Diagnostics.Process p;

    public GameObject sphere;
    public vis_3D vis;
    GameObject explorer_obj;
    Explorer_paul explorer;

    // Start is called before the first frame update
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        gameObject.GetComponent<Button>().onClick.AddListener(on_click);

        explorer_obj = GameObject.Find("explorer");
        explorer = explorer_obj.GetComponent<Explorer_paul>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void on_click()
    {
        explorer_obj.SetActive(true);
        explorer.set_up_files();
    }
}
