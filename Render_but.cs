using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.ShaderData;

public class Render_but : MonoBehaviour
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
        string save_path_1 = vis.path_dic + "test_im_july_1.png";
        string save_path_2 = vis.path_dic + "test_im_july_2.png";
        string truth_path_0 = null;
        string truth_path_1 = null;

        string path_str = vis.get_config_now().get_save_path();
        if (path_str != null && path_str != "")
        {
            save_path_1 = path_str + "/im1.png";
            save_path_2 = path_str + "/im2.png";
            truth_path_0 = path_str + "/truth0.png";
            truth_path_1 = path_str + "/truth1.png";
        }
        
        vis.take_pic_manual(save_path_1, cam_idx: 0);
        vis.take_pic_manual(save_path_2, cam_idx: 1);

        //vis.take_ref_pic_act();

        Transform designer = canvas.transform.Find("design_exp_panel");
        Transform im_1_panel = designer.Find("maps").Find("im_1_panel");
        Transform im_2_panel = designer.Find("maps").Find("im_2_panel");

        Transform truth_0_panel = designer.Find("maps").Find("truth_0_panel");
        Transform truth_1_panel = designer.Find("maps").Find("truth_1_panel");

        //A RenderTexture cam_0_tex = vis.cam_0.GetComponent<Camera>().targetTexture;
        //Sprite sprite = Sprite.Create(cam_0_tex, new Rect(0.0f, 0.0f, cam_0_tex.width, cam_0_tex.height),
        //    new Vector2(0.5f, 0.5f), 100.0f);
        //A Material mat1 = (Material)Resources.Load("Targets/fbx_files/cam_0_mat");

        // info (paul): ground truth flow:
        List<List<float>> truth_0_u_orig = vis.get_truth_0_u();
        List<List<float>> truth_1_u_orig = vis.get_truth_1_u();
        Texture2D tex_0_l = vis.mat2tex(truth_0_u_orig);
        Texture2D tex_1_l = vis.mat2tex(truth_1_u_orig);
        vis.save_png(tex_0_l, cam_idx: -1, save_path: truth_path_0, blade_idx: -1, pars: vis.get_params_now());
        vis.save_png(tex_1_l, cam_idx: -1, save_path: truth_path_1, blade_idx: -1, pars: vis.get_params_now());

        // info (paul): display on ground truth flow on panel
        float truth_map_scale = vis.get_truth_map_scale();
        truth_map_scale = 10f;
        List<List<float>> truth_0_u = vis.multiply_with_scalar(vis.get_truth_0_u(), scale: truth_map_scale);
        List<List<float>> truth_1_u = vis.multiply_with_scalar(vis.get_truth_1_u(), scale: truth_map_scale);
        
        Texture2D tex_0_u = vis.mat2tex(truth_0_u);
        Texture2D tex_1_u = vis.mat2tex(truth_1_u);

        display_from_path(im_1_panel, save_path_1, input: null);
        display_from_path(im_2_panel, save_path_2, input: null);

        display_from_path(truth_0_panel, save_path_1, input: tex_0_u);
        display_from_path(truth_1_panel, save_path_2, input: tex_1_u);

        //A (int width, int height) = (vis.get_render_res(), vis.get_render_res());//25122024 (512, 512);//17122024 (1024, 1024);//12122024 (512, 512);
        //Texture2D cropped = crop_tex(texture, width, height);

        //A Texture2D tex = new Texture2D(512, 512);
        //A tex.LoadImage(bytes);

        //GetComponent<Renderer>().material.mainTexture = tex;

        //A mat1.mainTexture = cam_0_tex;
        //A im_1_panel.GetComponent<UnityEngine.UI.Image>().material = mat1;
    }

    public Transform display_from_path(Transform im_1_panel, string save_path, Texture2D input)
    {
        // info (paul): assign to images to panels
        byte[] bytes = File.ReadAllBytes(save_path);

        (int width_pre, int height_pre) = vis.bytes2res(bytes);
        Texture2D texture = new Texture2D(width_pre, height_pre, TextureFormat.ARGB32, false);
        texture.LoadImage(bytes);

        if (input != null)
        {
            texture = input;
        }

        // info (paul): ;;
        Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), 100.0f);//texture

        // info (paul): ;;
        im_1_panel.GetComponent<Image>().sprite = sprite;
        return im_1_panel;
    }
}
