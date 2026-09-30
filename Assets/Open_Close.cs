using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Open_Close : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    private string file_path;

    // Start is called before the first frame update
    /// <summary>
    /// Unity callback: registers the click handler.
    /// </summary>
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        gameObject.GetComponent<Button>().onClick.AddListener(open_close);
    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {
        ;
        ;
    }

    /// <summary>
    /// Toggles the visibility of the parent panel.
    /// </summary>
    void open_close()
    {
        Transform parent = gameObject.transform.parent;
        bool is_active = parent.gameObject.activeSelf;
        parent.gameObject.SetActive(!is_active);
    }
}
