using System.IO;
using UnityEditor;
using UnityEngine;
using static ZonaPruebasBuilder;

// Estetica del "reino malvado": muros de ladrillo oscuro en los bloques gigantes de la montana, almenas,
// ventanas rojas y la cima del jefe final (anillo de columnas y muralla, alfombra, runas, estatuas).
// Textura propia: deja una imagen en Assets/Texturas/Muro.png (o .jpg) y se usa en lugar de la generada.
internal static class EvilKit
{
    const string TexDir = "Assets/Texturas/";
    static Material wall;

    internal static Material WallMat()
    {
        if (wall != null) return wall;
        EnsureFolder("Assets", "Texturas");
        Texture2D tex = null;
        foreach (var n in new[] { "Muro.png", "Muro.jpg", "Muro.jpeg" })
            if (File.Exists(TexDir + n)) { tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + n); break; }
        if (tex == null) tex = Bricks(TexDir + "Muro_Malvado.png", new Color(0.30f, 0.25f, 0.30f), 5);

        string path = "Assets/Materials/MuroMalvado.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = Color.white;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        m.SetTexture("_BaseMap", tex);
        m.SetTexture("_MainTex", tex);
        m.SetFloat("_Smoothness", 0.05f);
        EditorUtility.SetDirty(m);
        // copia en Resources para los cubos que se revisten en ejecucion (StoneSkin)
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Materiales");
        const string rp = "Assets/Resources/Materiales/MuroRuntime.mat";
        var rm = AssetDatabase.LoadAssetAtPath<Material>(rp);
        if (rm == null) { rm = new Material(m); AssetDatabase.CreateAsset(rm, rp); }
        rm.shader = m.shader;
        rm.CopyPropertiesFromMaterial(m);
        EditorUtility.SetDirty(rm);
        wall = m;
        return m;
    }

    internal static Texture2D Bricks(string path, Color baseC, int seed)
    {
        if (!File.Exists(path))
        {
            const int N = 256, rows = 8, rh = N / rows, bw = 64;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var r = new System.Random(seed);
            var tone = new float[rows * (N / bw)];
            for (int i = 0; i < tone.Length; i++) tone[i] = 0.78f + (float)r.NextDouble() * 0.4f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    int row = y / rh, off = (row % 2) * (bw / 2);
                    int xx = (x + off) % N, cx = xx / bw, lx = xx % bw, ly = y % rh;
                    float noise = (float)r.NextDouble() * 0.1f - 0.05f;
                    Color c;
                    if (lx < 2 || ly < 2) c = baseC * 0.3f;
                    else
                    {
                        float k = tone[row * (N / bw) + cx] + noise;
                        if (ly >= rh - 3) k *= 1.18f;      // arista superior mas clara
                        c = baseC * k;
                    }
                    c.a = 1f;
                    t.SetPixel(x, y, c);
                }
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
        }
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 512;
            ti.mipmapEnabled = true;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // Cubo unitario con UV en metros (tile = metros por repeticion). top: el techo (+Y) va en un segundo submesh.
    static Mesh BoxMesh(Vector3 size, float tile, bool topSeparate)
    {
        var verts = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var sides = new System.Collections.Generic.List<int>();
        var top = new System.Collections.Generic.List<int>();
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
            var list = (topSeparate && n == Vector3.up) ? top : sides;
            // Unity: la cara visible es la de normal cross(p1-p0, p2-p0)
            if (Vector3.Dot(Vector3.Cross(u, v), n) > 0) { list.Add(b); list.Add(b + 1); list.Add(b + 2); list.Add(b); list.Add(b + 2); list.Add(b + 3); }
            else { list.Add(b); list.Add(b + 2); list.Add(b + 1); list.Add(b); list.Add(b + 3); list.Add(b + 2); }
        }
        var m = new Mesh { name = "CuboMuro" };
        m.SetVertices(verts);
        m.SetUVs(0, uvs);
        if (topSeparate) { m.subMeshCount = 2; m.SetTriangles(sides, 0); m.SetTriangles(top, 1); }
        else m.SetTriangles(sides, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    // Reviste un cubo (usa su localScale) con el muro malvado. keepTop: el techo conserva el material que tenia.
    internal static GameObject Skin(GameObject go, float tile = 5f, bool keepTop = false, Material mat = null)
    {
        var mf = go.GetComponent<MeshFilter>();
        var mr = go.GetComponent<Renderer>();
        if (mf == null || mr == null) return go;
        var old = mr.sharedMaterial;
        mf.sharedMesh = BoxMesh(go.transform.localScale, tile, keepTop);
        var sm = mat != null ? mat : WallMat();
        mr.sharedMaterials = keepTop ? new[] { sm, old } : new[] { sm };
        return go;
    }

    // ---- Suelo de pasto con textura (Assets/Texturas/Pasto_Suelo.png, tileable; puedes reemplazar el png) ----
    static float WrapNoise(float x, float y, int period, int seed)
    {
        float fx = x * period, fy = y * period;
        int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
        float tx = fx - x0, ty = fy - y0;
        tx = tx * tx * (3f - 2f * tx); ty = ty * ty * (3f - 2f * ty);
        System.Func<int, int, float> H = (ix, iy) =>
        {
            ix = ((ix % period) + period) % period; iy = ((iy % period) + period) % period;
            int h = ix * 374761393 + iy * 668265263 + seed * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177; h ^= h >> 16;
            return (h & 0xffff) / 65535f;
        };
        float a = Mathf.Lerp(H(x0, y0), H(x0 + 1, y0), tx), b = Mathf.Lerp(H(x0, y0 + 1), H(x0 + 1, y0 + 1), tx);
        return Mathf.Lerp(a, b, ty);
    }

    internal static Material GrassMat()
    {
        EnsureFolder("Assets", "Texturas");
        const string tp = "Assets/Texturas/Pasto_Suelo.png";
        if (!File.Exists(tp))
        {
            const int N = 512;
            var px = new Color[N * N];
            var dark = new Color(0.16f, 0.30f, 0.10f); var mid = new Color(0.28f, 0.46f, 0.15f);
            var light = new Color(0.46f, 0.62f, 0.22f); var dry = new Color(0.52f, 0.48f, 0.22f); var dirt = new Color(0.33f, 0.25f, 0.14f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float n1 = WrapNoise(u, v, 6, 1), n2 = WrapNoise(u, v, 18, 2), n3 = WrapNoise(u, v, 64, 3);
                    float k = n1 * 0.5f + n2 * 0.3f + n3 * 0.2f;
                    Color c = k < 0.5f ? Color.Lerp(dark, mid, k * 2f) : Color.Lerp(mid, light, (k - 0.5f) * 2f);
                    float patch = WrapNoise(u, v, 4, 9);
                    if (patch > 0.68f) c = Color.Lerp(c, dry, Mathf.Clamp01((patch - 0.68f) * 4f) * 0.7f);
                    float soil = WrapNoise(u, v, 5, 11);
                    if (soil > 0.78f) c = Color.Lerp(c, dirt, Mathf.Clamp01((soil - 0.78f) * 6f) * 0.8f);
                    px[y * N + x] = c;
                }
            var rnd = new System.Random(7);
            for (int i = 0; i < 9000; i++)   // briznas finas
            {
                int x = rnd.Next(N), y = rnd.Next(N), len = 4 + rnd.Next(10);
                float lean = (float)(rnd.NextDouble() - 0.5) * 0.5f;
                bool bright = rnd.NextDouble() < 0.5;
                Color bc = bright ? new Color(0.55f, 0.72f, 0.26f) : new Color(0.12f, 0.24f, 0.08f);
                for (int j = 0; j < len; j++)
                {
                    int xx = ((x + Mathf.RoundToInt(lean * j)) % N + N) % N, yy = (y + j) % N;
                    px[yy * N + xx] = Color.Lerp(px[yy * N + xx], bc, 0.75f - 0.04f * j);
                }
            }
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            t.SetPixels(px); t.Apply();
            File.WriteAllBytes(tp, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(tp);
        }
        var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
        if (ti != null && ti.wrapMode != TextureWrapMode.Repeat)
        {
            ti.wrapMode = TextureWrapMode.Repeat; ti.mipmapEnabled = true; ti.maxTextureSize = 1024; ti.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
        string path = "Assets/Materials/Pasto_Suelo.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = Color.white;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        m.SetTexture("_BaseMap", tex); m.SetTexture("_MainTex", tex);
        m.SetFloat("_Smoothness", 0.02f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Piso de losas con textura (ladrillo generado en Assets/Texturas/Piso_<name>.png; puedes reemplazar el png)
    internal static Material FloorMat(string name, Color c, int seed)
    {
        EnsureFolder("Assets", "Texturas");
        var tex = Bricks("Assets/Texturas/Piso_" + name + ".png", c, seed);
        string path = "Assets/Materials/Piso_" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = Color.white;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        m.SetTexture("_BaseMap", tex);
        m.SetTexture("_MainTex", tex);
        m.SetFloat("_Smoothness", 0.05f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 size, float tile = 4f)
    {
        var g = Prim(PrimitiveType.Cube, name, pos, size, WallMat(), parent, false);
        return Skin(g, tile, false);
    }

    // Almenas sobre un muro recto a lo largo de X
    internal static void Battlements(Transform parent, float x0, float x1, float y, float z, float step, Vector3 size)
    {
        for (float x = x0; x <= x1; x += step)
            Block(parent, "Almena", new Vector3(x, y, z), size, 2f);
    }

    // Ventanas rojas, almenas y contrafuertes en los flancos gigantes de la montana
    internal static void Flanks(float x0, float x1, float topY)
    {
        var root = new GameObject("ReinoMalvado").transform;
        var glow = Mat("MVentanaMalvada", new Color(0.85f, 0.08f, 0.04f), true);
        var rnd = new System.Random(21);
        foreach (float sg in new[] { 1f, -1f })
        {
            // contrafuertes verticales cada 30 m
            for (float x = x0 + 15f; x < x1; x += 30f)
                Block(root, "Contrafuerte", new Vector3(x, topY * 0.5f, sg * 17.5f), new Vector3(4f, topY, 3f), 4f);
            // ventanas rojas
            for (float x = x0 + 8f; x < x1; x += 15f)
                foreach (float y in new[] { 26f, 40f, 54f, 68f, 80f })
                {
                    if (rnd.NextDouble() < 0.25) continue;
                    var w = Prim(PrimitiveType.Cube, "VentanaRoja", new Vector3(x + (float)rnd.NextDouble() * 4f, y, sg * 17.85f), new Vector3(1.2f, 4.5f, 0.25f), glow, root, false);
                }
            // almenas arriba
            Battlements(root, x0, x1, topY + 1.5f, sg * 19.5f, 7f, new Vector3(3.5f, 3f, 3f));
        }
        MarkStaticAll(root);
    }

    static void MarkStaticAll(Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>())
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
    }

    // ------------------------------------------------------------------ CIMA DEL JEFE FINAL (centrada en el origen, y = Y)
    internal static void Summit(float Y)
    {
        if (!DungeonKit.Available) return;
        var root = new GameObject("CimaMalvada").transform;
        const int T = 5;   // tema "malvado": piedra casi negra, fuego carmesi
        Vector3 Pol(float r, float deg) => new Vector3(r * Mathf.Cos(deg * Mathf.Deg2Rad), Y, r * Mathf.Sin(deg * Mathf.Deg2Rad));

        // columnas + torchas + muralla con estandartes (la entrada, en -X, queda abierta)
        for (int i = 0; i < 16; i++)
        {
            float a = i * 22.5f;
            if (i == 8) continue;
            var p = Pol(23f, a);
            DungeonKit.P(root, "A:Column", p.x, Y, p.z, 0f, 1.5f, "box", T);
            var q = Pol(21.4f, a + 3f);
            DungeonKit.P(root, "A:Torch", q.x, Y, q.z, 0f, 1.9f, null, T);
            bool gate = (i >= 6 && i <= 9);
            if (gate) continue;
            for (int k = 0; k < 3; k++)
            {
                float am = a + 22.5f * (k * 2 + 1) / 6f;
                var w = Pol(23.2f, am);
                float th = Mathf.Atan2(-Mathf.Cos(am * Mathf.Deg2Rad), -Mathf.Sin(am * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
                for (int row = 0; row < 2; row++)
                {
                    var g = DungeonKit.P(root, "A:Wall_Modular", w.x, Y + row * 3f, w.z, th, 1.5f, row == 0 ? "box" : null, T);
                }
                if (k == 1)
                {
                    var bp = Pol(22.7f, am);
                    DungeonKit.W(root, "A:Banner_wall", new Vector3(bp.x, Y + 5.6f, bp.z), -new Vector3(bp.x, 0f, bp.z).normalized, 1.5f, T);
                }
            }
        }

        // alfombra roja hacia el cráter + runas
        DungeonKit.P(root, "B:Carpet", -14.5f, Y, 0f, 90f, 2.4f, null, 0);
        var rune = Mat("MRunaJefe", new Color(0.9f, 0.1f, 0.05f), true);
        var floor = Mat("VCima", new Color(0.2f, 0.16f, 0.15f));
        Prim(PrimitiveType.Cylinder, "RunaExterior", new Vector3(0f, Y + 0.02f, 0f), new Vector3(26.4f, 0.02f, 26.4f), rune, root, false);
        Prim(PrimitiveType.Cylinder, "RunaExteriorHueco", new Vector3(0f, Y + 0.03f, 0f), new Vector3(24.6f, 0.02f, 24.6f), floor, root, false);
        Prim(PrimitiveType.Cylinder, "RunaInterior", new Vector3(0f, Y + 0.04f, 0f), new Vector3(17f, 0.02f, 17f), rune, root, false);
        Prim(PrimitiveType.Cylinder, "RunaInteriorHueco", new Vector3(0f, Y + 0.05f, 0f), new Vector3(15.4f, 0.02f, 15.4f), floor, root, false);
        for (int i = 0; i < 8; i++)   // rayos de la estrella
        {
            var spoke = Prim(PrimitiveType.Cube, "RunaRayo", new Vector3(0f, Y + 0.06f, 0f), new Vector3(0.35f, 0.02f, 16f), rune, root, false);
            spoke.transform.rotation = Quaternion.Euler(0f, i * 22.5f, 0f);
        }

        // candelabros alrededor del cráter, calaveras y huesos
        for (int i = 0; i < 6; i++)
        {
            var p = Pol(9f, 30f + i * 60f);
            DungeonKit.P(root, "B:Candelabrum_tall", p.x, Y, p.z, 0f, 1.7f, null, T);
        }
        var rnd = new System.Random(33);
        for (int i = 0; i < 12; i++)
        {
            var p = Pol(10f + (float)rnd.NextDouble() * 11f, (float)rnd.NextDouble() * 360f);
            string k = i % 3 == 0 ? "A:Skull" : (i % 3 == 1 ? "B:Bones" : "B:Bones2");
            DungeonKit.P(root, k, p.x, Y, p.z, (float)rnd.NextDouble() * 360f, 1.6f, null, T);
        }

        // estatuas flanqueando la entrada
        foreach (float a in new[] { 160f, 200f })
        {
            var p = Pol(18f, a);
            var ped = DungeonKit.P(root, "A:Pedestal", p.x, Y, p.z, 0f, 1.1f, "box", T);
            if (ped != null)
            {
                var b = ped.GetComponentsInChildren<Renderer>()[0].bounds;
                foreach (var rr in ped.GetComponentsInChildren<Renderer>()) b.Encapsulate(rr.bounds);
                DungeonKit.P(root, "A:Statue_Horse", p.x, b.max.y, p.z, a < 180f ? 270f : 90f, 1.1f, null, T);
            }
        }

        // luces carmesi (pocas, por rendimiento en celular)
        for (int i = 0; i < 4; i++)
        {
            var lg = new GameObject("LuzCarmesi");
            lg.transform.SetParent(root, false);
            lg.transform.position = Pol(19f, 45f + i * 90f) + Vector3.up * 4f;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.range = 22f; l.intensity = 6f; l.color = new Color(1f, 0.15f, 0.08f);
        }
    }
}
