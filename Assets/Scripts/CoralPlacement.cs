using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class CoralPlacement : MonoBehaviour
{
    public SteamVR_Action_Boolean trig_action;
    public SteamVR_Input_Sources handType;
    public GameObject coral;
    public CoralGeneration generation;

    [SerializeField] private GameObject highlight;
    private MeshRenderer highlight_mr;
    private bool triggerDown;
    private RaycastHit hit;
    private Vector3 ray_vec;

    private void Start()
    {
        trig_action.AddOnStateDownListener(TriggerDown, handType);
        trig_action.AddOnStateUpListener(TriggerUp, handType);

        highlight_mr = highlight.GetComponent<MeshRenderer>();
        if (generation == null) { generation = GetComponent<CoralGeneration>(); }
    }
    void Update()
    {

        ray_vec = transform.position + transform.forward * 1000;
        if (coral != null && Physics.Raycast(transform.position, ray_vec, out hit) && hit.collider.CompareTag("sandbox"))
        {
            highlight.transform.position = hit.point;
            highlight_mr.enabled = true;

            // On click
            if (triggerDown)
            {
                Transform origin;
                origin = coral.transform.GetChild(0).transform;
                if (origin.GetComponent<Collider>() == null)
                { 
                    origin = coral.transform.GetChild(1);
                    Debug.Log("Taking second child");
                }
                else { Debug.Log("Taking firstborn"); }


                coral.transform.position = hit.point; //+ origin.position
                coral = null;
                generation.coralStarted = false;
                generation.generated_count = 0;
                Debug.Log("placing");
            }
        }
        else
        {
            highlight_mr.enabled = false;
        }

    }
    public void TriggerUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        triggerDown = false;
        //Debug.Log("Trigger up");
    }
    public void TriggerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        triggerDown = true;
        //Debug.Log("Trigger down");
    }
}


