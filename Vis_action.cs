using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class Vis_action : MonoBehaviour
{

    //public Camera cam;

    public GameObject sphere;
    public vis_3D vis;
    public bool check_vis;

    // Start is called before the first frame update
    void Start()
    {
        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();
    }

    // Update is called once per frame
    void Update()
    {
        ;
    }

    private void OnBecameVisible()
    {
        set_up_blade();
    }

    public void set_up_blade()
    {
        if (!vis.get_visible() || !check_vis)
        {
            vis.set_visible(true);

            // info (paul): get the projected uv coordinates
            UnityEngine.Vector3[] uvs = obj2uvs(gameObject);

            // info (paul): assign the uv coordinates to this obj:
            uvs2obj(gameObject, uvs);
            vis.apply_speckles(gameObject);
            vis.create_other_blades();

            vis.set_blades_created(true);
            check_vis = true;
        }
    }

    public GameObject uvs2obj(GameObject blade, Vector3[] projs)
    {
        Mesh mesh = blade.GetComponent<MeshFilter>().mesh;

        Vector2[] uvs = proj2uv(projs);
        vis.set_uv_start(uvs);
        
        mesh.uv = uvs;
        return blade;
    }

    public Vector2[] proj2uv(Vector3[] projs)
    {
        // info (paul): convert the 3ds to 2ds

        UnityEngine.Vector2[] uvs = new Vector2[projs.Length];
        for (int i = 0; i < projs.Length; i++)
        {
            try
            {
                uvs[i] = new UnityEngine.Vector2(projs[i].x, projs[i].y);
            }
            catch
            {
                uvs[i] = new UnityEngine.Vector2(projs[i].x, projs[i].y);
            }
        }

        return uvs;
    }

    public UnityEngine.Vector3[] obj2uvs(GameObject blade)
    {
        Mesh mesh = blade.GetComponent<MeshFilter>().mesh;

        UnityEngine.Vector3[] verts = mesh.vertices;
        UnityEngine.Vector3[] projs = new UnityEngine.Vector3[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            //10062024 UnityEngine.Vector3 vert = verts[i];
            //10062024 UnityEngine.Vector3 vec_proj = cam.WorldToScreenPoint(vert);
            //10062024 Vector3 uv_3d = new Vector3(vec_proj.x/Screen.width, vec_proj.y/Screen.height, 0f);
            UnityEngine.Vector3 vert = verts[i];
            Vector3 uv_3d = new Vector3(vert.x, vert.y, 0f);
            projs[i] = uv_3d;
        }

        norm_projs(projs);

        return projs;
    }

    public Vector3[] norm_projs(UnityEngine.Vector3[] projs)
    {
        // info (paul): finding mins and maxs
        float min_x =  9999999f;
        float max_x = -9999999f;
        
        float min_y =  9999999f;
        float max_y = -9999999f;

        for (int i = 0; i < projs.Length; i++)
        {
            min_x = Mathf.Min(projs[i].x, min_x);
            max_x = Mathf.Max(projs[i].x, max_x);
            
            min_y = Mathf.Min(projs[i].y, min_y);
            max_y = Mathf.Max(projs[i].y, max_y);
        }

        // info (paul): normalize:
        for (int i = 0; i < projs.Length; i++)
        {
            float x_new = vis.uv_scale * projs[i].x / (max_x - min_x);
            float y_new = vis.uv_scale * projs[i].y / (max_y - min_y);
            projs[i] = new Vector3(x_new, y_new, 0f);
        }
        
        return projs;
    }
}


