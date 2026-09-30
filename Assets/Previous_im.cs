using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Previous_im : MonoBehaviour
{
    Transform selfparent;
    Transform previous_but;
    Transform next_but;
    Transform im0panel;
    Transform im1panel;

    Sprite im0 = null;
    Sprite im1 = null;
    
    // Start is called before the first frame update
    /// <summary>
    /// Unity callback: looks up the preview panels and the navigation buttons.
    /// </summary>
    void Start()
    {
        selfparent = gameObject.transform.parent;
        previous_but = selfparent.Find("previous_but");
        next_but = selfparent.Find("next_but");
        im0panel = selfparent.Find("im0_panel");
        im1panel = selfparent.Find("im1_panel");
        im0 = null;
        im1 = null;

        previous_but.GetComponent<Button>().onClick.AddListener(click_previous);
        next_but.GetComponent<Button>().onClick.AddListener(click_next);
    }

    // Update is called once per frame
    /// <summary>
    /// Unity callback (unused).
    /// </summary>
    void Update()
    {
        
    }

    /// <summary>
    /// Shows the first image of the pair in the preview.
    /// </summary>
    public void click_previous()
    {
        if (im1 == null)
        {
            im0 = im0panel.GetComponent<Image>().sprite;
            im1 = im1panel.GetComponent<Image>().sprite;
        }

        im0 = im0panel.GetComponent<Image>().sprite;
        im1 = im1panel.GetComponent<Image>().sprite;
        
        im1panel.GetComponent<Image>().sprite = im0;
    }
    /// <summary>
    /// Shows the second image of the pair in the preview.
    /// </summary>
    public void click_next()
    {
        if (im1 == null)
        {
            im0 = im0panel.GetComponent<Image>().sprite;
            im1 = im1panel.GetComponent<Image>().sprite;
        }
        im1panel.GetComponent<Image>().sprite = im1;

    }
}
