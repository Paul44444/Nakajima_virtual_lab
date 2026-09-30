using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Load_add : MonoBehaviour
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
        gameObject.GetComponent<Button>().onClick.AddListener(add_to_list);
    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {
        ;
    }
    /// <summary>
    /// Sets the file that this entry adds to the list.
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
    /// <summary>
    /// Adds the file to the list of image files and refreshes the list.
    /// </summary>
    void add_to_list()
    {
        vis.add_to_im_files(file_path);
        vis.refresh_files_list();
    }

}
