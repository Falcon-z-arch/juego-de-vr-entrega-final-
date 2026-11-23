using UnityEngine;

// Reviste un cubo en ejecucion con la textura de muro (Resources/Materiales/MuroRuntime, la crea el editor).
// UV en metros: el ladrillo no se estira. Si el material no existe, el cubo queda como estaba.
public static class StoneSkin
{
    static Material mat;

    public static GameObject Apply(GameObject go, float tile = 3f)
    {
        if (mat == null) mat = Resources.Load<Material>("Materiales/MuroRuntime");
        var mf = go.GetComponent<MeshFilter>();
        var mr = go.GetComponent<Renderer>();
        if (mat == null || mf == null || mr == null) return go;
        mf.sharedMesh = BoxMesh(go.transform.localScale, tile);
        mr.sharedMaterial = mat;
        return go;
    }

    public static Mesh BoxMesh(Vector3 size, float tile)
    {
        var verts = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var tris = new System.Collections.Generic.List<int>();
        Vector3[] N = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var n in N)
        {
            Vector3 u, v;
            if (Mathf.Abs(n.x) > 0.5f) { u = Vector3.forward; v = Vector3.up; }
            else if (Mathf.Abs(n.y) > 0.5f) { u = Vector3.right; v = Vector3.forward; }
            else { u = Vector3.right; v = Vector3.up; }
            float su = Mathf.Abs(Vector3.Dot(size, u)) / tile, sv = Mathf.Abs(Vector3.Dot(size, v)) / tile;
            Vector3 c = n * 0.5f;
            int b = verts.Count;
            verts.Add(c - u * 0.5f - v * 0.5f); verts.Add(c + u * 0.5f - v * 0.5f);
            verts.Add(c + u * 0.5f + v * 0.5f); verts.Add(c - u * 0.5f + v * 0.5f);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(su, 0)); uvs.Add(new Vector2(su, sv)); uvs.Add(new Vector2(0, sv));
            if (Vector3.Dot(Vector3.Cross(u, v), n) > 0) { tris.Add(b); tris.Add(b + 1); tris.Add(b + 2); tris.Add(b); tris.Add(b + 2); tris.Add(b + 3); }
            else { tris.Add(b); tris.Add(b + 2); tris.Add(b + 1); tris.Add(b); tris.Add(b + 3); tris.Add(b + 2); }
        }
        var m = new Mesh { name = "CuboMuroRT" };
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}
