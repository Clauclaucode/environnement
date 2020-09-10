using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct WireSegment
{
    public Vector3 start;
    public Vector3 end;
    public Vector3 diff; // vector from start to end
};

[ExecuteInEditMode]
public class WireGenerator : MonoBehaviour {

    public float thickness = 1;
    public float definition = 5;
    public float subdivision = 0;
    public List<Vector3> points = null;
    public bool fix_intersections = true;
    public bool per_segment_uv = true;
    public bool close_top = true;
    public LineRenderer source = null;

    public bool animated = false;
    private bool animated_prev = false;
    public bool play = false;
    [Range(0.0f, 10.0f)]
    public float growing_speed = 1;
    private float current_length = 0;
    private float total_length = 0;

    private List<Vector3> src_points;
    private MeshFilter meshFilter = null;
    private bool regenerate_request = false;
    private bool render_request = false;
    // mesh buffers
    private List<Vector3> vertices;
    private List<int> faces;
    private List<Vector3> normals;
    private List<Vector2> uv0s;
    private List<Vector2> uv1s;

    // usefull
    private List<WireSegment> segments;

    public int get_segment_count() {
        if (segments == null) {
            return 0;
        }
        return segments.Count;
    }

    public WireSegment get_segment(int i) {
        return segments[i];
    }

    public void request_regeneration() {
        regenerate_request = true;
    }

    // NOT to be used with WireSegments!
    Vector3[] mix_segments( Vector3[] s0, Vector3[] s1, float alpha ) {
        Vector3[] res = new Vector3[] { s0[0], s0[1], s0[2] };
        for ( int  i = 0; i < 3; ++i )
        {
            res[i] = ((s0[i] * (1.0f - alpha)) + (s1[i] * alpha)).normalized;
        }
        return res;
    }
    
    private void add_loop( 
        Vector3[] prev_axis, 
        Vector3[] curr_axis, 
        Vector3 prev, 
        Vector3 curr,
        Vector3 prev_center,
        Vector3 curr_center,
        float prev_thickness, 
        float curr_thickness,
        float prev_length,
        float curr_length) {

        float agap = Mathf.PI * 2 / definition;

        for (int j = 1; j < definition + 1; ++j)
        {

            float pa = agap * (j - 1);
            float ca = agap * j;
            float cpa = Mathf.Cos(pa);
            float spa = Mathf.Sin(pa);
            float cca = Mathf.Cos(ca);
            float sca = Mathf.Sin(ca);

            Vector3[] vs = new Vector3[4] {
                prev + (prev_axis[0] * cpa + prev_axis[2] * spa) * prev_thickness,
                curr + (curr_axis[0] * cpa + curr_axis[2] * spa) * curr_thickness,
                curr + (curr_axis[0] * cca + curr_axis[2] * sca) * curr_thickness,
                prev + (prev_axis[0] * cca + prev_axis[2] * sca) * prev_thickness
            };

            vertices.Add(vs[0]);
            vertices.Add(vs[1]);
            vertices.Add(vs[2]);
            vertices.Add(vs[3]);

            normals.Add((vs[0] - prev_center).normalized);
            normals.Add((vs[1] - curr_center).normalized);
            normals.Add((vs[2] - curr_center).normalized);
            normals.Add((vs[3] - prev_center).normalized);

            uv0s.Add(new Vector2((j - 1.0f) / definition, 0));
            uv0s.Add(new Vector2((j - 1.0f) / definition, 1));
            uv0s.Add(new Vector2((j * 1.0f) / definition, 1));
            uv0s.Add(new Vector2((j * 1.0f) / definition, 0));

            uv1s.Add(new Vector2((j - 1.0f) / definition, prev_length));
            uv1s.Add(new Vector2((j - 1.0f) / definition, curr_length));
            uv1s.Add(new Vector2((j * 1.0f) / definition, curr_length));
            uv1s.Add(new Vector2((j * 1.0f) / definition, prev_length));

            int vnum = vertices.Count;
            if ((vs[0] - vs[2]).sqrMagnitude < (vs[1] - vs[3]).sqrMagnitude)
            {
                faces.Add(vnum - 4);
                faces.Add(vnum - 3);
                faces.Add(vnum - 2);

                faces.Add(vnum - 2);
                faces.Add(vnum - 1);
                faces.Add(vnum - 4);
            }
            else
            {
                faces.Add(vnum - 4);
                faces.Add(vnum - 3);
                faces.Add(vnum - 1);

                faces.Add(vnum - 1);
                faces.Add(vnum - 3);
                faces.Add(vnum - 2);
            }

        }
    }

    private void render_length() {
        total_length = 0;
        for (int i = 1; i < points.Count; ++i) {
            total_length += (points[i] - points[i - 1]).magnitude;
        }
    }

    private void render_mesh()
    {

        // checking meshfilter
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if ( points.Count < 2 )
        {
            meshFilter.mesh = new Mesh();
            return;
        }

        // reseting lists
        segments = new List<WireSegment>();
        src_points = new List<Vector3>();

        // different behaviour if wire is animated or not
        if (!animated)
        {
            src_points = points;
            for (int i = 1; i < src_points.Count; ++i) {
                WireSegment ws;
                ws.start = src_points[i - 1];
                ws.end = src_points[i];
                ws.diff = ws.end - ws.start;
                segments.Add(ws);
            }
        }
        else
        {
            // pushing points into src_points until it reached current_length
            if (current_length == 0)
            {
                // stopping here, pushing empty mesh
                meshFilter.mesh = new Mesh();
                return;
            }
            else
            {
                float l = 0;
                for (int i = 0; i < points.Count; ++i)
                {
                    if (i == 0)
                    {
                        src_points.Add(points[i]);
                        continue;
                    }
                    Vector3 diff = points[i] - points[i - 1];
                    float segl = diff.magnitude;
                    if (l + segl < current_length)
                    {
                        src_points.Add(points[i]);
                        WireSegment ws;
                        ws.start = src_points[i - 1];
                        ws.end = src_points[i];
                        ws.diff = ws.end - ws.start;
                        segments.Add(ws);
                    }
                    else
                    {
                        float part = (l + segl) - current_length;
                        Vector3 newp = points[i] - diff.normalized * part;
                        src_points.Add(newp);
                        break;
                    }
                    l += segl;
                }
            }
        }

        if (definition < 2)
        {
            definition = 2;
        }
        if (subdivision < 0)
        {
            subdivision = 0;
        }

        List<Vector3> _pts = new List<Vector3>();
        for (int i = 0; i < src_points.Count; ++i)
        {
            _pts.Add(src_points[i]);
            if (i < src_points.Count - 1 && subdivision > 0)
            {
                Vector3 diff = (src_points[i + 1] - src_points[i]) / (subdivision + 1);
                for (int j = 1; j <= subdivision; ++j)
                {
                    _pts.Add(src_points[i] + diff * j);
                }
            }
        }

        Mesh mesh = new Mesh();

        vertices = new List<Vector3>();
        faces = new List<int>();
        normals = new List<Vector3>();
        uv0s = new List<Vector2>();
        uv1s = new List<Vector2>();

        float tmp_length = 0;

        // sub segments
        List<Vector3[]> segs = new List<Vector3[]>();
        for (int i = 1; i < _pts.Count; ++i)
        {
            Vector3 up = (_pts[i] - _pts[i - 1]).normalized;
            Vector3 front;
            Vector3 left;
            if (Mathf.Abs(Vector3.Dot(Vector3.up, up)) > 1e-5)
            {
                front = Vector3.Cross(Vector3.left, up);
                left = Vector3.Cross(front, up);
            }
            else
            {
                front = Vector3.Cross(Vector3.up, up);
                left = Vector3.Cross(front, up);
            }
            segs.Add(new Vector3[] { front, up, left });
        }

        Vector3[] curr_axis = segs[0];

        int pcount = _pts.Count;
        for (int i = 1; i < _pts.Count; ++i)
        {

            Vector3 prev = _pts[i - 1];
            Vector3 curr = _pts[i];

            int prev_seg = i - 2;
            if (prev_seg < 0) { prev_seg = 0; }
            int curr_seg = i - 1;
            int next_seg = i;
            if (next_seg >= pcount - 1) { next_seg = curr_seg; }

            float length = (curr - prev).magnitude;
            Vector3[] prev_axis;

            if (fix_intersections) { prev_axis = mix_segments(segs[prev_seg], segs[curr_seg], 0.5f); }
            else { prev_axis = segs[curr_seg]; }

            if (fix_intersections) { curr_axis = mix_segments(segs[curr_seg], segs[next_seg], 0.5f); }
            else { curr_axis = segs[curr_seg]; }

            add_loop(
                prev_axis, curr_axis,
                prev, curr,
                prev, curr,
                thickness, thickness,
                tmp_length, tmp_length + length);

            tmp_length += length;

        }

        if (close_top)
        {
            // creation of an hemisphere at the end of the tube
            int cap_def = Mathf.CeilToInt(definition * 0.5f);
            float agap = Mathf.PI * 0.5f / cap_def;
            Vector3 center = _pts[pcount - 1];
            for (int j = 1; j < cap_def + 1; ++j)
            {
                float prev_m = Mathf.Cos(agap * (j - 1));
                float curr_m = Mathf.Cos(agap * j);
                float prev_s = Mathf.Sin(agap * (j - 1));
                float curr_s = Mathf.Sin(agap * j);
                float length = (curr_s - prev_s) * thickness;
                add_loop(
                    curr_axis, curr_axis,
                    center + curr_axis[1] * prev_s * thickness,
                    center + curr_axis[1] * curr_s * thickness,
                    center, center,
                    thickness * prev_m, thickness * curr_m,
                    tmp_length, tmp_length + length);
                tmp_length += length;
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = faces.ToArray();
        mesh.normals = normals.ToArray();
        if (per_segment_uv)
        {
            mesh.uv = uv0s.ToArray();
            mesh.uv2 = uv1s.ToArray();
        }
        else
        {
            mesh.uv = uv1s.ToArray();
            mesh.uv2 = uv0s.ToArray();
        }

        meshFilter.mesh = mesh;

    }

    public void regenerate() {

        // checking points
        if (points == null) {
            points = new List<Vector3>();
        }
        if (source != null && source.positionCount > 1)
        {
            points = new List<Vector3>();
            for (int i = 0; i < source.positionCount; ++i)
            {
                points.Add(source.GetPosition(i));
            }

        }

        render_length();
        render_mesh();

    }

    public void OnValidate() {
        regenerate_request = true;
    }

    private void LateUpdate() {

        if (animated_prev != animated) {
            current_length = 0;
            animated_prev = animated;
        }

        if (regenerate_request)
        {
            regenerate_request = false;
            render_request = false;
            regenerate();
        }
        else if (render_request) {
            render_request = false;
            render_mesh();
        }
    }

    // Use this for initialization
    void Start()
    {

        if (GetComponent<MeshRenderer>() == null) {
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
        }

        if (animated)
        {
            current_length = 0;
        }

        regenerate();
    }

    void Update () {

        if (animated && play) {
            if (current_length >= total_length)
            {
                return;
            }
            else
            {
                current_length += Time.deltaTime * growing_speed;
                if (current_length >= total_length) {
                    current_length = total_length;
                }
                render_request = true;
            }
        }
	}
}