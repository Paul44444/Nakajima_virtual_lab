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

        //20092026 GameObject.Find findet keine inaktiven Objekte -> ueber den Canvas suchen
        //(wie in vis_3D.Start), sonst NullReferenceException beim Aktivieren dieses Buttons.
        explorer_obj = GameObject.Find("explorer");
        if (explorer_obj == null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            Transform explorer_tf = canvas != null ? canvas.transform.Find("explorer") : null;
            explorer_obj = explorer_tf != null ? explorer_tf.gameObject : null;
        }
        explorer = explorer_obj != null ? explorer_obj.GetComponent<Explorer_paul>() : null;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void on_click()
    {
        if (explorer_obj == null || explorer == null)
        {
            Debug.LogWarning("Load: 'explorer'-Objekt nicht gefunden.");
            return;
        }
        explorer_obj.SetActive(true);
        explorer.set_up_files();
    }
}
