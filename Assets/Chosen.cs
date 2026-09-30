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
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();

        explorer_obj = GameObject.Find("explorer");
        explorer = explorer_obj.GetComponent<Explorer_paul>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void set_file_path(string file_path)
    {
        this.file_path = file_path;
    }
    public string get_file_path()
    {
        return this.file_path;
    }
}
