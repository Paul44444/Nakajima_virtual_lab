using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam_manager : MonoBehaviour
{
    public float speedH = 2.0f;
    public float speedV = 2.0f;

    private float yaw = 0.0f;
    private float pitch = 0.0f;

    public Transform platine_plane;

    private float zoomSpeed = 100f;//15072025 2f;//2.0f;
    Vector3 pivot;
    Vector3 pivot_map;
    Vector3 pivot_exp;

    // info (paul): inital cam position for both setups
    Vector3 pos_map_0;
    Vector3 pos_exp_0;
    Vector3 pos_cam;

    float dist;

    GameObject main_cam_obj;
    GameObject exp_cam_obj;
    GameObject active_cam;

    public vis_3D vis;
    public GameObject sphere;
    
    // info (paul): a value, which determines, how the normal distance is changed by scrolling
    //          compared to the standard distance
    float scrolled_dist;

    // Start is called before the first frame update
    /// <summary>
    /// Initialises the interactive camera: references to the lab controller, the cameras, and the default position.
    /// </summary>
    public void do_start()
    {
        scrolled_dist = 0f;
        rot_counter = 0;

        sphere = GameObject.Find("sphere");
        vis = sphere.GetComponent<vis_3D>();

        main_cam_obj = GameObject.Find("MainCamera");
        exp_cam_obj = GameObject.Find("exp_cam");

        active_cam = main_cam_obj;

        //17052024 pivot = new Vector3(1429f, 4f, 933f);
        pivot_map = new Vector3(2705f, 0f, 2598f);//17062025 (852.9918f, 47.87537f, 349.2668f);
        pivot_exp = vis.get_blades_pos();//new Vector3(-1000f, 0f, -200f);//new Vector3(-1000f, 0f, -200f);//17062025 new Vector3(0f, 24.91755f, -200f);
        pivot = pivot_map;

        pos_map_0 = new Vector3(2705f, 5073f, 2598f);//new Vector3(2705f, 5073f, 2598f);//17062025 (-677f - 500f, 0f, 229.9586f);
        //14072025 pos_exp_0 = new Vector3(pivot_exp.x, pivot_exp.y + 50f, pivot_exp.z - 1000f);//new Vector3(-841f, 162f, -1207f);//-2607f);//new Vector3(-1000f, 0f, -200f);//17062025 new Vector3(852.9918f, 247.87537f, 349.2668f);
        pos_exp_0 = new Vector3(pivot_exp.x, pivot_exp.y + 50f, pivot_exp.z - 2000f);

        pos_cam = pos_map_0;

        // info (paul): set initial position
        main_cam_obj.transform.position = pos_map_0;
        exp_cam_obj.transform.position = pos_exp_0;

        // info (paul): exp view, as if "e" was printed
        exp_cam_obj.GetComponent<Camera>().enabled = true;
        main_cam_obj.GetComponent<Camera>().enabled = false;
        active_cam = exp_cam_obj;
        pivot = pivot_exp;
        pos_cam = pos_exp_0;
    }
    /// <summary>
    /// Applies position, rotation, and zoom of the active camera for the sample (platine) view.
    /// </summary>
    public void start_for_platine()
    {
        manage_pos(active_cam);
        manage_rot(active_cam);
        manage_scroll(active_cam);
    }

    /// <summary>
    /// Unity callback: updates position, rotation, and mouse-wheel zoom of the active camera; ignores the mouse wheel while the pointer is over the UI panels.
    /// </summary>
    void Update()
    {
        if (false)//13072025 (Input.GetKey("u"))
        {
            // info (paul): switch cam
            if (active_cam == exp_cam_obj)
            {
                active_cam = main_cam_obj;
                pivot = pivot_map;
                exp_cam_obj.GetComponent<Camera>().enabled = false;
                //exp_cam_obj.SetActive(false);

                main_cam_obj.GetComponent<Camera>().enabled = true;
                //main_cam_obj.SetActive(true);
            }
            else
            {
                active_cam = exp_cam_obj;
                pivot = pivot_exp;
                exp_cam_obj.GetComponent<Camera>().enabled = true;
                //exp_cam_obj.SetActive(true);

                main_cam_obj.GetComponent<Camera>().enabled = false;
                //main_cam_obj.SetActive(false);
            }
        }
        //if (Input.GetKey("c"))
        //28092026 ueber der Bildergalerie zoomt das Mausrad das Bild, nicht die Kamera
        if ((Input.GetMouseButton(2) || Input.mouseScrollDelta.y != 0f) && !ExperimentImageGallery.PointerOverGallery())
        {
            manage_pos(active_cam);
            manage_rot(active_cam);
            manage_scroll(active_cam);
        }

        if (Input.GetKey("e"))
        {
            // info (paul): switching to experimental cam

            // TODO: later replace "e" by a button or sth
            exp_cam_obj.GetComponent<Camera>().enabled = true;
            main_cam_obj.GetComponent<Camera>().enabled = false;
            active_cam = exp_cam_obj;
            pivot = pivot_exp;
            pos_cam = pos_exp_0;
        }
        if (false)//13072025 (Input.GetKey("q"))
        {
            // info (paul): switching to mapping cam

            // TODO: later replace "e" by a button or sth
            exp_cam_obj.GetComponent<Camera>().enabled = false;
            main_cam_obj.GetComponent<Camera>().enabled = true;
            active_cam = main_cam_obj;
            pivot = pivot_map;
            pos_cam = pos_map_0;
        }

    }

    /// <summary>
    /// Places the camera relative to the sample plane.
    /// </summary>
    /// <param name="cam_obj">Camera object to place.</param>
    public void manage_pos(GameObject cam_obj)
    {
        if (!platine_plane)
        {
            GameObject platine_plane_obj = GameObject.Find("platine_plane");
            if (platine_plane_obj)
            {
                platine_plane = platine_plane_obj.transform;
            }
        }

        if (true)//29062025 (platine_plane != null)
        {
            //17052024 gameObject.transform.position = platine_plane.position;
            //03072025 cam_obj.transform.position = pos_cam;

            cam_obj.transform.position = new Vector3(pos_cam.x, pos_cam.y, pos_cam.z + scrolled_dist);
        }
    }
    int rot_counter;
    /// <summary>
    /// Rotates the camera from the mouse movement (yaw and pitch).
    /// </summary>
    /// <param name="cam_obj">Camera object to rotate.</param>
    public void manage_rot(GameObject cam_obj)
    {
        rot_counter += 1;

        yaw += speedH * Input.GetAxis("Mouse X");
        pitch -= speedV * Input.GetAxis("Mouse Y");
        string info = "cnt: " + rot_counter.ToString() + "yaw: " + yaw.ToString() + "; pitch: " + pitch.ToString();
        //Debug.Log(info);
        //make_sphere_at(pivot.x, pivot.y, pivot.z, size: 10f);

        cam_obj.transform.rotation = Quaternion.identity;
        cam_obj.transform.LookAt(pivot);
        cam_obj.transform.RotateAround(pivot, cam_obj.transform.right, pitch);
        cam_obj.transform.RotateAround(pivot, Vector3.up, yaw);

        //transform.eulerAngles = new Vector3(pitch, -90f - yaw, 0.0f);
    }

    /// <summary>
    /// Creates a small marker sphere at a position (debugging aid).
    /// </summary>
    /// <param name="x">x coordinate.</param>
    /// <param name="y">y coordinate.</param>
    /// <param name="z">z coordinate.</param>
    /// <param name="size">Diameter of the sphere.</param>
    /// <returns>The created sphere.</returns>
    public GameObject make_sphere_at(float x, float y, float z, float size = 1f)
    {
        //(GameObject sphere_local, _) = but1.build_object(new Vector3(x, y, z), 
        //        Quaternion.identity, -1, "Targets/symbols/street_line_symbol");

        GameObject sphere_local = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere_local.transform.position = new Vector3(x, y, z);
        sphere_local.transform.localScale = new Vector3(size, size, size);
        sphere_local.name = "sphere_marker";
        //12042024 set_layer(sphere_local, 11);
        return sphere_local;
    }

    /// <summary>
    /// Moves the camera along its viewing direction according to the mouse wheel.
    /// </summary>
    /// <param name="cam">Camera object to move.</param>
    public void manage_scroll(GameObject cam)
    {
        float scroll = Input.mouseScrollDelta.y;//150722025 Input.GetAxis("Mouse ScrollWheel");
        //float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            string info = "scroll: " + scroll.ToString();
            //11062024 Debug.Log(info);
        }
        //transform.Translate(0, 0, scroll * zoomSpeed, Space.World);
        cam.transform.position += cam.transform.forward * scroll * zoomSpeed;

        scrolled_dist += scroll * zoomSpeed;

        //Vector3 vec_pos = transform.position;
        //Vector3 vec_diff = find_vec_diff(vec_pos, pivot);
        //transform.position = find_vec_sum(pivot, 0.99f*vec_diff);
    }

    /// <summary>
    /// Adds two vectors component-wise.
    /// </summary>
    /// <param name="vec_a">First vector.</param>
    /// <param name="vec_b">Second vector.</param>
    /// <returns>Sum of both vectors.</returns>
    public Vector3 find_vec_sum(Vector3 vec_a, Vector3 vec_b)
    {
        Vector3 vec_c = new Vector3();
        vec_c.x = vec_a.x + vec_b.x;
        vec_c.y = vec_a.y + vec_b.y;
        vec_c.z = vec_a.z + vec_b.z;
        return vec_c;
    }

    /// <summary>
    /// Subtracts two vectors.
    /// </summary>
    /// <param name="vec_a">Minuend.</param>
    /// <param name="vec_b">Subtrahend.</param>
    /// <returns>Difference vec_a - vec_b.</returns>
    public Vector3 find_vec_diff(Vector3 vec_a, Vector3 vec_b)
    {
        Vector3 vec_c = find_vec_sum(vec_a, -vec_b);
        return vec_c;
    }


}
