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
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
        gameObject.GetComponent<Button>().onClick.AddListener(add_to_list);
    }

    // Update is called once per frame
    void Update()
    {
        ;
    }
    public void set_file_path(string file_path)
    {
        this.file_path = file_path;
    }
    public string get_file_path()
    {
        return this.file_path;
    }
    void add_to_list()
    {
        vis.add_to_im_files(file_path);
        vis.refresh_files_list();
    }

}
