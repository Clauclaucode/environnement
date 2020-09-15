using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class CoralGeneration : MonoBehaviour
{
    public SteamVR_Action_Boolean trig_action;
    public SteamVR_Input_Sources handType;
    
    [HideInInspector] public bool coralStarted = false;
    [HideInInspector] public int generated_count = 0;

    [SerializeField] private CoralPlacement placement;
    [SerializeField] private bool animation;

    private bool contact = false;
    private bool triggerDown = false;

    public LineRenderer line = null;
    private GameObject line_obj = null;
    public bool line_autoremove = false;

    public GameObject collider = null;
    public float collider_radius = 0.2f;

    public WireGenerator wire_generator = null;
    public bool wire_animated = false;
    private WireGenerator tmp_wire = null;
    private int wire_segments = 0;

    private GameObject origin = null;
    private GameObject tmp_obj = null;
    private LineRenderer tmp_line = null;

    private bool pressed = false;
    private Vector3 pick_position = new Vector3();
    private Vector3 last_point;
    private List<Vector3> points = null;

    [Range(0.0f, 10.0f)]
    public float sampling = 0.5f;

    public void Start()
    {
        trig_action.AddOnStateDownListener(TriggerDown, handType);
        trig_action.AddOnStateUpListener(TriggerUp, handType);
        if (line != null)
        {
            line_obj = line.gameObject;
            line.enabled = false;
        }
        reset_line();
    }

    public void TriggerUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        triggerDown = false;
        placement.paused = false;
        //generate_wire();
        if (tmp_wire != null)
        {
            tmp_wire.play = false;
        }
        reset_line();
    }

    public void TriggerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (contact)
        {
            triggerDown = true;
            placement.paused = true;

            pick_position = transform.position;
            if (!coralStarted)
            {
                coralStarted = true;
                start_line();
                origin = tmp_obj;
            }
            else 
            {
                start_line();
            }
            
            //Debug.Log("Trigger down");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!coralStarted && other.CompareTag("sphere"))
        {
            contact = true;
            Debug.Log("Sphere");
            
        }
            
        else if (coralStarted && other.CompareTag("coral"))
        {
            contact = true;
            Debug.Log("Corail");
            
        }

    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!coralStarted && other.CompareTag("sphere"))
        {
            contact = false;
            
        }

        else if (coralStarted && other.CompareTag("coral"))
        {
            contact = false;
            
        }
    }
    
    private void reset_line()
    {
        tmp_obj = null;
        tmp_line = null;
        tmp_wire = null;
        pick_position = new Vector3();
        last_point = new Vector3();
        points = new List<Vector3>();
        wire_segments = 0;
    }

    private void start_line()
    {

        tmp_obj = new GameObject();
        if (generated_count > 0)
        {
            tmp_obj.name = "branch_" + generated_count;
        }
        else
        {
            tmp_obj.name = "coral";
            placement.coral = tmp_obj;
        }
        

        if (origin != null)
        {
            tmp_obj.transform.parent = origin.transform;
        }
        if (line != null)
        {
            GameObject l = Instantiate(line_obj);
            l.transform.parent = tmp_obj.transform;
            l.transform.position = new Vector3();
            tmp_line = l.GetComponent<LineRenderer>();
            tmp_line.positionCount = 1;
            tmp_line.SetPosition(0, last_point);
            tmp_line.enabled = true;
        }
        else
        {
            tmp_line = null;
        }
        tmp_obj.transform.position = pick_position;
        generated_count += 1;
        points.Add(last_point);

        if (wire_animated)
        {
            generate_wire();
        }

    }

    private void add_collider()
    {

        if (tmp_obj == null || collider == null || points.Count < 2)
        {
            return;
        }

        

        GameObject tmp_coll = Instantiate(collider);
        tmp_coll.transform.parent = tmp_obj.transform;
        Vector3 pt0 = points[points.Count - 2];
        Vector3 pt1 = points[points.Count - 1];
        Vector3 diff = pt1 - pt0;

        //// Set collider as coral origin
        //if (!coralStarted && points.Count == 2)
        //{
        //    coralStarted = true;
        //}

        Quaternion q = new Quaternion();
        q.SetLookRotation(diff.normalized, Vector3.up);
        // rotate it 90* on X
        Quaternion corrq = new Quaternion();
        float theta = Mathf.PI * 0.5f;
        Vector3 axis = Vector3.right;
        corrq.SetAxisAngle(axis, theta);

        tmp_coll.transform.rotation = q * corrq;
        tmp_coll.transform.position = tmp_obj.transform.position + pt0 + diff * 0.5f;
        tmp_coll.transform.localScale = new Vector3(collider_radius, diff.magnitude * 0.5f, collider_radius);

        /*
        if (tag != "")
        {
            tmp_coll.tag = tag;
        }
        */
        tmp_coll.tag = "coral";

    }

    private void add_collider(WireSegment ws)
    {
        if (tmp_obj == null || collider == null)
        {
            return;
        }

        GameObject tmp_coll = Instantiate(collider);
        tmp_coll.transform.parent = tmp_obj.transform;

        Quaternion q = new Quaternion();
        q.SetLookRotation(ws.diff.normalized, Vector3.up);
        // rotate it 90* on X
        Quaternion corrq = new Quaternion();
        float theta = Mathf.PI * 0.5f;
        Vector3 axis = Vector3.right;
        corrq.SetAxisAngle(axis, theta);

        tmp_coll.transform.rotation = q * corrq;
        tmp_coll.transform.position = tmp_obj.transform.position + ws.start + ws.diff * 0.5f;
        tmp_coll.transform.localScale = new Vector3(collider_radius, ws.diff.magnitude * 0.5f, collider_radius);

        if (tag != "")
        {
            tmp_coll.tag = tag;
        }
    }


    private void add_position()
    {

        if (tmp_obj == null)
        {
            return;
        }
        Vector3 newp = transform.position - pick_position;
        int pcount = tmp_line.positionCount;
        if (Vector3.Distance(newp, last_point) > sampling)
        {
            last_point = newp;
            points.Add(last_point);
            add_collider();
            pcount += 1;
            if (tmp_line != null)
            {
                tmp_line.positionCount = pcount;
                tmp_line.SetPosition(pcount - 1, newp);
            }
            if (wire_animated && tmp_wire != null)
            {
                tmp_wire.points = new List<Vector3>(points);
                tmp_wire.request_regeneration();
            }
            else
            {
                add_collider();
            }
        }
        else if (tmp_line != null)
        {
            if (pcount == 1)
            {
                pcount += 1;
                tmp_line.positionCount = pcount;
            }
            tmp_line.SetPosition(pcount - 1, newp);
        }
    }

    private void generate_wire()
    {
        if (tmp_obj == null || wire_generator == null)
        {
            return;
        }
        GameObject tw = Instantiate(wire_generator.gameObject);
        tw.name = "wire";
        tw.transform.parent = tmp_obj.transform;
        tw.transform.localPosition = new Vector3();
        tmp_wire = tw.GetComponent<WireGenerator>();

        if (wire_animated)
        {
            tmp_wire.animated = true;
            tmp_wire.play = true;
        }
    }

    public void Update()
    {

        if (!triggerDown)
        {
            return;
        }

        if (wire_animated && tmp_wire != null && collider != null)
        {
            int curr_segs = tmp_wire.get_segment_count();
            for (int i = wire_segments; i < curr_segs; ++i)
            {
                add_collider(tmp_wire.get_segment(i));
            }
            wire_segments = curr_segs;
        }

        if (pressed)
        {
            add_position();
        }

        //add_position();

        //// /base.Update();
        // if (Input.GetMouseButtonDown(0))
        // {
        //     //pressed = true;
        //     //if (base.touch)
        //     //{
        //     //    pick_position = base.last_position;
        //     //    pick_distance = base.last_distance;
        //     //    start_line();
        //     //}
        //     //else
        //     //{
        //     //    tmp_obj = null;
        //     //}
        // }
        // else if (Input.GetMouseButtonUp(0))
        // {
        //     generate_wire();
        //     reset_line();
        // }

        // if (pressed)
        // {
        //     
        // }

    }


}