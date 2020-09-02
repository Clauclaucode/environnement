using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pointer : MonoBehaviour
{

    public GameObject pointer_gameobject;
    public string tag = "";
    protected MeshRenderer ptr;

    protected bool touch = false;
    protected Vector3 last_position = new Vector3();
    protected float last_distance = 0;

    // Start is called before the first frame update
    public void Start()
    {
        if (pointer_gameobject != null) {
            ptr = pointer_gameobject.GetComponent<MeshRenderer>();
        }
        show_pointer(false);
    }

    protected void move_pointer(Vector3 pos) {
        if (pointer_gameobject == null)
        {
            return;
        }
        pointer_gameobject.transform.position = pos;

    }

    protected void show_pointer(bool b) {
        if (ptr == null) {
            return;
        }
        ptr.enabled = b;
    }


    // Update is called once per frame
    public void Update()
    {
        // raycast in the middle of the screen
        Vector2 mid = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Ray ray = Camera.main.ScreenPointToRay(mid);
        RaycastHit hitData;
        bool valid = false;
        if (Physics.Raycast(ray, out hitData, 1000))
        {
            if (tag == "" || tag == hitData.collider.gameObject.tag) {
                touch = true;
                last_position = hitData.point;
                last_distance = Vector3.Distance(Camera.main.transform.position, last_position);
                move_pointer(hitData.point);
                show_pointer(true);
                valid = true;
            }
        }
        if ( !valid )
        {
            touch = false;
            show_pointer(false);
        }
    }
}
