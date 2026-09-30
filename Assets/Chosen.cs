using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chosen : MonoBehaviour
{
    public GameObject sphere;
    public vis_3D vis;
    GameObject explorer_obj;
    Explorer_paul explorer;
    string file_path;

    // Start is called before the first frame update
    /// <summary>
    /// Unity callback: looks up the lab controller and the file explorer.
    /// </summary>
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();

        explorer_obj = GameObject.Find("explorer");
        explorer = explorer_obj.GetComponent<Explorer_paul>();
    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {
        
    }

    /// <summary>
    /// Sets the file represented by this entry of the file list.
    /// </summary>
    /// <param name="file_path">Full path of the file.</param>
    public void set_file_path(string file_path)
    {
        this.file_path = file_path;
    }
    /// <summary>
    /// Returns the file represented by this entry.
    /// </summary>
    /// <returns>Full path of the file.</returns>
    public string get_file_path()
    {
        return this.file_path;
    }
}
