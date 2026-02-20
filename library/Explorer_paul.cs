using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Explorer_paul : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //set_up_files();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void set_up_files()
    {
        //04062025 string path = "C:/Users/go73jem/Desktop/DIC_package/exp_normal/time_flow_v/play_5";
        //string path = "/Users/paulrichter/Desktop/DIC_2025_for_travel/DIC_package/exp_normal/time_flow_v/play5/";
        string path ="/Users/Paul/Desktop/DIC_2025_for_travel/DIC_package/exp_normal/time_flow_v/ims/";
        set_up_files_for_path(path);
    }

    public void set_up_files_for_path(string path)
    {
        bool exists = File.Exists(path);
        bool is_dir = Directory.Exists(path);

        if (is_dir)
        {
            DirectoryInfo dir_info = new DirectoryInfo(path);
            FileInfo[] files = dir_info.GetFiles();

            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Name.EndsWith(".png"))
                {
                    string file_name = files[i].Name;
                    add_entry_with_name(path, file_name); ;
                }
            }
        }
    }

    public void add_entry_with_name(string file_path, string file_name)
    {
        GameObject block_template = Resources.Load("save_block") as GameObject;
        GameObject block = Instantiate(block_template);

        GameObject canvas = GameObject.Find("Canvas");
        Transform explorer = canvas.transform.Find("explorer").transform;
        block.transform.SetParent(explorer.Find("files"));
        block.name = file_name.Replace(".png", "");

        // info (paul): set position:
        int children_prev = explorer.Find("files").childCount - 1;
        float width = block.GetComponent<RectTransform>().sizeDelta.x;
        float height = block.GetComponent<RectTransform>().sizeDelta.y;
        float pos_y = -((float)children_prev + 0.5f) * (height + 5);
        block.GetComponent<RectTransform>().anchoredPosition = new Vector2(width/2f, pos_y);
        
        // info (paul): assign params:
        block.transform.Find("but").GetComponent<Load_add>().set_file_path(file_path + "/" + file_name);
        block.transform.Find("label").GetComponent<Text>().text = file_name;
    }

}
