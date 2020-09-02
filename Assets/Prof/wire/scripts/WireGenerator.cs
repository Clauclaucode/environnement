using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private MeshFilter meshFilter = null;
    private bool regenerate_request = false;
    // mesh buffers
    private List<Vector3> vertices;
    private List<int> faces;
    private List<Vector3> normals;
    private List<Vector2> uv0s;
    private List<Vector2> uv1s;

    Vector3[] mix_segments( Vector3[] s0, Vector3[] s1, float alpha ) {
        Vector3[] res = new Vector3[] { s0[0], s0[1], s0[2] };
        for ( int  i = 0; i < 3; ++i )
        {
            res[i] = ((s0[i] * (1.0f - alpha)) + (s1[i] * alpha)).normalized;
        }
        return res;
    }

    void add_loop( 
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

    void regenerate() {


        if (points == null) {
            points = new List<Vector3>();
        }
        if (points.Count < 2)
        {
            points.Clear();
            points.Add(new Vector3(0, 0, 0));
            points.Add(new Vector3(0, 0, 1));
        }
        if (definition < 2) {
            definition = 2;
        }
        if (subdivision < 0)
        {
            subdivision = 0;
        }

        List<Vector3> src_points = points;
        if (source != null && source.positionCount > 1) {
            src_points = new List<Vector3>();
            for ( int i = 0; i < source.positionCount; ++i )
            {
                src_points.Add(source.GetPosition(i));
            }
            
        }

        List<Vector3> _pts = new List<Vector3>();
        for (int i = 0; i < src_points.Count; ++i) {
            _pts.Add(src_points[i]);
            if (i < src_points.Count - 1 && subdivision > 0) {
                Vector3 diff = (src_points[i + 1] - src_points[i] ) / (subdivision +1);
                for (int j = 1; j <= subdivision; ++j) {
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

        float total_length = 0;

        List<Vector3[]> segments = new List<Vector3[]>();
        for (int i = 1; i < _pts.Count; ++i) {
            Vector3 up = (_pts[i] - _pts[i-1]).normalized;
            Vector3 front;
            Vector3 left;
            if (Mathf.Abs(Vector3.Dot(Vector3.up, up)) > 1e-5) {
                front = Vector3.Cross(Vector3.left, up);
                left = Vector3.Cross(front, up);
            } else {
                front = Vector3.Cross(Vector3.up, up);
                left = Vector3.Cross(front, up);
            }
            segments.Add(new Vector3[] { front, up, left });
        }

        Vector3[] curr_axis = segments[0];

        int pcount = _pts.Count;
        for (int i = 1; i < _pts.Count; ++i) {

            Vector3 prev = _pts[i-1];
            Vector3 curr = _pts[i];

            int prev_seg = i - 2;
            if (prev_seg < 0) { prev_seg = 0; }
            int curr_seg = i - 1;
            int next_seg = i;
            if (next_seg >= pcount-1) { next_seg = curr_seg; }

            float length = (curr - prev).magnitude;
            Vector3[] prev_axis;
                
            if (fix_intersections) { prev_axis = mix_segments(segments[prev_seg], segments[curr_seg], 0.5f); }
            else { prev_axis = segments[curr_seg]; }

            if (fix_intersections) { curr_axis = mix_segments(segments[curr_seg], segments[next_seg], 0.5f); }
            else { curr_axis = segments[curr_seg]; }
            
            add_loop(
                prev_axis, curr_axis,
                prev, curr,
                prev, curr,
                thickness, thickness,
                total_length, total_length + length);
            
            total_length += length;

        }

        if (close_top)
        {
            // creation of an hemisphere at the end of the tube
            int cap_def = Mathf.CeilToInt( definition * 0.5f );
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
                    total_length, total_length + length);
                total_length += length;
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
        else {
            mesh.uv = uv1s.ToArray();
            mesh.uv2 = uv0s.ToArray();
        }

        if (meshFilter == null) {
            meshFilter = GetComponent<MeshFilter>();
        }
        if (meshFilter == null) {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        meshFilter.mesh = mesh;

    }

    public void OnValidate() {
        regenerate_request = true;
    }
    private void LateUpdate() {
        if (regenerate_request)
        {
            regenerate_request = false;
            regenerate();
        }
    }

    // Use this for initialization
    void Start()
    {

        if (GetComponent<MeshRenderer>() == null) {
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
        }

        regenerate();

    }

    // Update is called once per frame
    void Update () {
		
	}
}
