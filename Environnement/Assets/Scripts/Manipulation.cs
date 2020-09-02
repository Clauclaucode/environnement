using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine.Events;

public class Manipulation : MonoBehaviour
{
#if MOUSE

    [SerializeField] private Transform coral_prefab;
    [SerializeField] private Camera cam;
    [SerializeField] private FirstPersonController fpc;

    private Transform coral;
    private Ray ray;
    private RaycastHit hit;
    private float scaling;

    private void Update()
    {
        if (Input.GetButton("Fire1"))
        {
            if (coral == null) // On click
            {
                fpc.enabled = false;

                // Create new coral at mouse target
                ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log(hit.transform.name);
                    if (hit.transform.CompareTag("Coral"))
                    {
                        coral = Instantiate(coral_prefab, hit.transform.position, Quaternion.identity);
                        Debug.Log("Contact made");
                    }
                }
            }
            else // Button held
            {
                scaling = Input.GetAxis("Mouse X");
                coral.transform.localScale += new Vector3(scaling, scaling, scaling);
            }   
        }        
        else if (Input.GetButtonUp("Fire1")) // On release
        {
            coral = null;
            fpc.enabled = true;
        }
    }

#else
    void Start()
    {
        Debug.LogError("VR not implemented");
    }
#endif
}