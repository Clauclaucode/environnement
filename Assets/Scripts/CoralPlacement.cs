using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;

public class CoralPlacement : MonoBehaviour
{
    public SteamVR_Action_Boolean trig_action;
    public SteamVR_Input_Sources handType;
    public GameObject coral;
    //public CoralGeneration generation;
    public UnityEvent coralPlaced;

    public bool paused = false;

    [SerializeField] private GameObject highlight;


    private MeshRenderer highlight_mr;
    private bool triggerDown;
    private bool contact;
    private RaycastHit hit;
    private Vector3 ray_vec;

    private void Start()
    {
        trig_action.AddOnStateDownListener(TriggerDown, handType);
        trig_action.AddOnStateUpListener(TriggerUp, handType);

        highlight_mr = highlight.GetComponent<MeshRenderer>();
       // if (generation == null) { generation = GetComponent<CoralGeneration>(); }
    }
    void Update()
    {
        if (paused) { return; }

        ray_vec = transform.position + transform.forward * 1000;
        if (coral != null && Physics.Raycast(transform.position, ray_vec, out hit) && hit.collider.CompareTag("sandbox"))
        {
            highlight.transform.position = hit.point;
            highlight_mr.enabled = true;

            // On click
            if (triggerDown)
            {
                // Set the coral’s origin at the beginning of the first branch
                Transform origin;
                origin = coral.transform.GetChild(0).transform;
                if (origin.GetComponent<Collider>() == null)
                { 
                    origin = coral.transform.GetChild(1);
                    Debug.Log("Taking second child");
                }
                else { Debug.Log("Taking firstborn"); }

                coral.transform.position = hit.point;
                coral = null;
                //generation.end();
                coralPlaced.Invoke();

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
        paused = false;
    }
    public void TriggerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        triggerDown = true;
        if (contact) { paused = true; }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("coral"))
        {
            contact = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("coral"))
        {
            contact = false;
        }
    }
}