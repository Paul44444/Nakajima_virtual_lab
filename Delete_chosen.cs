using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Delete_chosen : MonoBehaviour
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
        gameObject.GetComponent<Button>().onClick.AddListener(on_click);

        explorer_obj = GameObject.Find("explorer");
        explorer = explorer_obj.GetComponent<Explorer_paul>();

        Transform chosen_block = gameObject.transform.parent;
        file_path = chosen_block.GetComponent<Chosen>().get_file_path();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void on_click()
    {
        // info (paul): remove box
        vis.remove_from_im_files(file_path);
        vis.refresh_files_list();
    }
}
