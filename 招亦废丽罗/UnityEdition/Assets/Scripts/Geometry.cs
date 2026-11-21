// Geometry.cs —— 程序化图元、材质缓存、骨架节点、披风布料网格
using System.Collections.Generic;
using UnityEngine;

public static class Geo
{
    static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

    static Mesh Get(string key, System.Func<Mesh> make)
    {
        Mesh m;
        if (cache.TryGetValue(key, out m)) return m;
        m = make();
        m.name = key;
        cache[key] = m;
        return m;
    }

    public static void Clear()
    {
        foreach (var kv in cache) if (kv.Value != null) Object.Destroy(kv.Value);
        cache.Clear();
    }

    // ---- 长方体（尺寸 1×1×1，用 scale 控制大小）
    public static Mesh Box()
    {
        return Get("box", delegate
        {
            var m = new Mesh();
            float h = 0.5f;
            var v = new Vector3[]
            {
                new Vector3(-h,-h,-h), new Vector3(h,-h,-h), new Vector3(h,h,-h), new Vector3(-h,h,-h),
                new Vector3(-h,-h, h), new Vector3(h,-h, h), new Vector3(h,h, h), new Vector3(-h,h, h),
            };
            var n = new Vector3[]
            {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                Vector3.left, Vector3.left, Vector3.left, Vector3.left,
                Vector3.right, Vector3.right, Vector3.right, Vector3.right,
                Vector3.down, Vector3.down, Vector3.down, Vector3.down,
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
            };
            var vv = new Vector3[24];
            int k = 0;
            for (int face = 0; face < 6; face++) for (int q = 0; q < 4; q++) vv[k++] = v[quad[face * 4 + q]];
            var t = new int[36];
            k = 0;
            for (int face = 0; face < 6; face++)
            {
                int b = face * 4;
                t[k++] = b; t[k++] = b + 1; t[k++] = b + 2;
                t[k++] = b; t[k++] = b + 2; t[k++] = b + 3;
            }
            m.vertices = vv;
            m.normals = n;
            m.triangles = t;
            return m;
        });
    }

    static readonly int[] quad = { 0, 1, 2, 3, 4, 7, 6, 5, 4, 5, 1, 0, 5, 6, 2, 1, 3, 7, 4, 0, 7, 6, 2, 3 };

    // ---- 圆柱 / 圆锥
    public static Mesh Cylinder(int segs, bool caps)
    {
        string key = "cyl" + segs + (caps ? "c" : "");
        return Get(key, delegate
        {
            var m = new Mesh();
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i <= segs; i++)
            {
                float a = (float)i / segs * Mathf.PI * 2f;
                float cx = Mathf.Cos(a), sz = Mathf.Sin(a);
                v.Add(new Vector3(cx * 0.5f, -0.5f, sz * 0.5f));
                n.Add(new Vector3(cx, 0f, sz));
                v.Add(new Vector3(cx * 0.5f, 0.5f, sz * 0.5f));
                n.Add(new Vector3(cx, 0f, sz));
            }
            for (int i = 0; i < segs; i++)
            {
                int b = i * 2;
                t.Add(b); t.Add(b + 2); t.Add(b + 1);
                t.Add(b + 1); t.Add(b + 2); t.Add(b + 3);
            }
            if (caps)
            {
                int cb = v.Count;
                v.Add(new Vector3(0f, 0.5f, 0f)); n.Add(Vector3.up);
                for (int i = 0; i <= segs; i++)
                {
                    float a = (float)i / segs * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0.5f, Mathf.Sin(a) * 0.5f)); n.Add(Vector3.up);
                }
                for (int i = 0; i < segs; i++) { t.Add(cb); t.Add(cb + 1 + i); t.Add(cb + 2 + i); }
                int cb2 = v.Count;
                v.Add(new Vector3(0f, -0.5f, 0f)); n.Add(Vector3.down);
                for (int i = 0; i <= segs; i++)
                {
                    float a = (float)i / segs * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * 0.5f, -0.5f, Mathf.Sin(a) * 0.5f)); n.Add(Vector3.down);
                }
                for (int i = 0; i < segs; i++) { t.Add(cb2); t.Add(cb2 + 2 + i); t.Add(cb2 + 1 + i); }
            }
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTriangles(t, 0);
            return m;
        });
    }

    public static Mesh Cone(int segs)
    {
        string key = "cone" + segs;
        return Get(key, delegate
        {
            var m = new Mesh();
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i <= segs; i++)
            {
                float a = (float)i / segs * Mathf.PI * 2f;
                float cx = Mathf.Cos(a), sz = Mathf.Sin(a);
                v.Add(new Vector3(cx * 0.5f, -0.5f, sz * 0.5f));
                var nv = new Vector3(cx * 0.8f, 0.6f, sz * 0.8f).normalized;
                n.Add(nv);
            }
            for (int i = 0; i < segs; i++) { t.Add(i); t.Add(i + 1); t.Add(segs + 1); }
            v.Add(new Vector3(0f, 0.5f, 0f)); n.Add(Vector3.up);
            // 底面
            int cb = v.Count;
            v.Add(new Vector3(0f, -0.5f, 0f)); n.Add(Vector3.down);
            for (int i = 0; i <= segs; i++)
            {
                float a = (float)i / segs * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Cos(a) * 0.5f, -0.5f, Mathf.Sin(a) * 0.5f)); n.Add(Vector3.down);
            }
            for (int i = 0; i < segs; i++) { t.Add(cb); t.Add(cb + 2 + i); t.Add(cb + 1 + i); }
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTriangles(t, 0);
            return m;
        });
    }

    // ---- 球 / 八面体
    public static Mesh Sphere(int segs, int rings)
    {
        string key = "sph" + segs + "_" + rings;
        return Get(key, delegate
        {
            var m = new Mesh();
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float phi = (float)r / rings * Mathf.PI;
                float y = Mathf.Cos(phi) * 0.5f;
                float rad = Mathf.Sin(phi) * 0.5f;
                for (int s = 0; s <= segs; s++)
                {
                    float th = (float)s / segs * Mathf.PI * 2f;
                    var p = new Vector3(Mathf.Cos(th) * rad, y, Mathf.Sin(th) * rad);
                    v.Add(p);
                    n.Add(p.normalized);
                }
            }
            int stride = segs + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segs; s++)
                {
                    int a = r * stride + s, b = a + 1, c = a + stride, d = c + 1;
                    t.Add(a); t.Add(c); t.Add(b);
                    t.Add(b); t.Add(c); t.Add(d);
                }
            }
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTriangles(t, 0);
            return m;
        });
    }

    public static Mesh Octa()
    {
        return Get("octa", delegate
        {
            var m = new Mesh();
            var v = new Vector3[]
            {
                new Vector3(0f,0.5f,0f), new Vector3(0.42f,0f,0f), new Vector3(0f,0f,0.42f),
                new Vector3(-0.42f,0f,0f), new Vector3(0f,0f,-0.42f), new Vector3(0f,-0.5f,0f),
            };
            var n = new Vector3[v.Length];
            for (int k = 0; k < v.Length; k++) n[k] = v[k].normalized;
            var t = new int[] { 0,1,2, 0,2,3, 0,3,4, 0,4,1, 5,2,1, 5,3,2, 5,4,3, 5,1,4 };
            m.vertices = v;
            m.normals = n;
            m.triangles = t;
            return m;
        });
    }

    // ---- 单面四边形（宽 1×高 1，正面朝 -Z）
    public static Mesh Quad()
    {
        return Get("quad", delegate
        {
            var m = new Mesh();
            m.vertices = new Vector3[]
            {
                new Vector3(-0.5f,-0.5f,0f), new Vector3(0.5f,-0.5f,0f),
                new Vector3(0.5f,0.5f,0f), new Vector3(-0.5f,0.5f,0f),
            };
            m.normals = new Vector3[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            m.uv = new Vector2[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
            m.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            return m;
        });
    }

    // ---- 合并图元（顶点色）到单 Mesh：items = (mesh, matrix, color)
    public static Mesh Merge(List<Mesh> meshes, List<Matrix4x4> mats, List<Color> colors, bool withColor)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var cols = new List<Color>();
        var tris = new List<int>();
        for (int k = 0; k < meshes.Count; k++)
        {
            var mesh = meshes[k];
            var mtx = mats[k];
            var col = colors[k];
            int baseV = verts.Count;
            var mv = mesh.vertices;
            var mn = mesh.normals;
            var tr = mesh.triangles;
            var nmat = mtx.inverse.transpose;
            for (int i = 0; i < mv.Length; i++)
            {
                verts.Add(mtx.MultiplyPoint3x4(mv[i]));
                {
                    Vector3 nn = nmat.MultiplyVector(mn.Length > i ? mn[i] : Vector3.up);
                    norms.Add(nn.normalized);
                }
                cols.Add(col);
            }
            for (int i = 0; i < tr.Length; i++) tris.Add(baseV + tr[i]);
        }
        var outMesh = new Mesh();
        if (verts.Count > 65000) outMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        outMesh.SetVertices(verts);
        outMesh.SetNormals(norms);
        if (withColor) outMesh.SetColors(cols);
        outMesh.SetTriangles(tris, 0);
        outMesh.RecalculateBounds();
        return outMesh;
    }
}

// ---------------------------------------------------------------- 材质
public static class Mats
{
    static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();
    public static Material VertexColor;
    public static Material Unlit;
    public static Material Water;
    public static Material Blob;
    public static Texture2D BlobTex;

    public static void Init()
    {
        Shader lit = Shader.Find("Zhaoyi/VertexColorLit");
        if (lit == null) lit = Shader.Find("Standard");
        VertexColor = new Material(lit);
        VertexColor.name = "地形材质";

        Shader sky = Shader.Find("Zhaoyi/Sky");
        if (sky != null)
        {
            Unlit = new Material(sky);
            Unlit.name = "天空材质";
        }

        Shader waterShader = Shader.Find("Zhaoyi/Water");
        if (waterShader != null)
        {
            Water = new Material(waterShader);
            Water.name = "水面材质";
        }
        else
        {
            Water = new Material(Shader.Find("Standard"));
            Water.color = new Color(0.10f, 0.20f, 0.26f, 0.82f);
        }

        BlobTex = MakeBlobTex();
        Blob = new Material(Shader.Find("Sprites/Default"));
        Blob.color = new Color(1f, 1f, 1f, 1f);
        Blob.mainTexture = BlobTex;
    }

    static Texture2D MakeBlobTex()
    {
        int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a * 0.9f;
                px[y * n + x] = new Color32(8, 8, 12, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    public static Material Get(Color c, Color emissive)
    {
        bool em = (emissive.r + emissive.g + emissive.b) > 0.01f;
        int key = (Mathf.RoundToInt(c.r * 255f) << 24) | (Mathf.RoundToInt(c.g * 255f) << 16)
                  | (Mathf.RoundToInt(c.b * 255f) << 8) | (em ? 1 : 0);
        Material m;
        if (cache.TryGetValue(key, out m)) return m;
        var shader = Shader.Find("Standard");
        m = new Material(shader);
        m.color = c;
        m.SetFloat("_Glossiness", 0.22f);
        m.SetFloat("_Metallic", 0.35f);
        if (em)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emissive);
            float lum = emissive.r * 0.3f + emissive.g * 0.6f + emissive.b * 0.1f;
            m.SetFloat("_Glossiness", 0.55f);
            m.SetFloat("_Metallic", 0.15f);
            if (lum > 0.6f) m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        cache[key] = m;
        return m;
    }
}

// ---------------------------------------------------------------- 骨架节点
public class Part
{
    public Transform tf;
    public MeshRenderer renderer;
    public Vector3 basePos, baseScale;
}

public static class Make
{
    public static Transform Node(string name, Transform parent, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    public static Part PartOn(Transform node, string name, Mesh mesh, Material mat,
                               Vector3 pos, Vector3 euler, Vector3 scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(node, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = scale;
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        var p = new Part();
        p.tf = go.transform;
        p.renderer = mr;
        p.basePos = pos;
        p.baseScale = scale;
        return p;
    }

    public static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material mat, bool shadows)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On
                                       : UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return go;
    }
}

// ---------------------------------------------------------------- 披风布料（6×5 verlet）
public class CapeSim
{
    public int rows, cols;
    public Vector3[] pts;
    public Vector3[] prev;
    public int[] idx;
    public Mesh mesh;
    public Vector3[] display;
    public float[] restH, restV, restD;

    public CapeSim(CapeCfg cfg)
    {
        rows = Mathf.Max(2, cfg.rows);
        cols = Mathf.Max(2, cfg.cols);
        int n = rows * cols;
        pts = new Vector3[n];
        prev = new Vector3[n];
        display = new Vector3[n];
        for (int r = 0; r < rows; r++)
        {
            float v = (float)r / (rows - 1);
            for (int c = 0; c < cols; c++)
            {
                float u = (float)c / (cols - 1);
                float flare = 0.74f + 0.34f * v;
                pts[r * cols + c] = new Vector3((u - 0.5f) * cfg.w * flare, -v * cfg.len, 0f);
                prev[r * cols + c] = pts[r * cols + c];
                display[r * cols + c] = pts[r * cols + c];
            }
        }
        var tris = new List<int>();
        for (int r = 0; r < rows - 1; r++)
        {
            for (int c = 0; c < cols - 1; c++)
            {
                int a = r * cols + c, b = a + 1, d = a + cols, e = d + 1;
                tris.Add(a); tris.Add(d); tris.Add(b);
                tris.Add(b); tris.Add(d); tris.Add(e);
            }
        }
        idx = tris.ToArray();
        mesh = new Mesh();
        mesh.MarkDynamic();
        mesh.vertices = pts;
        mesh.triangles = idx;
    }

    public void Step(float dt, Vector3 windVel, Vector3 gravityLocal, float stiffness)
    {
        if (dt <= 0f) return;
        int n = rows * cols;
        float damp = 0.965f;
        for (int r = 1; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int i = r * cols + c;
                Vector3 vel = pts[i] - prev[i];
                Vector3 acc = gravityLocal + windVel * (0.4f + 0.6f * ((float)r / (rows - 1)));
                Vector3 next = pts[i] + vel * damp + acc * dt * dt;
                prev[i] = pts[i];
                pts[i] = next;
            }
        }
        float k = Mathf.Clamp(stiffness, 0.05f, 0.6f);
        for (int it = 0; it < 2; it++)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int i = r * cols + c;
                    if (r > 0)
                    {
                        int j = (r - 1) * cols + c;
                        float rest = Mathf.Abs(pts[i].y - pts[j].y) + 0.001f;
                        Vector3 d = pts[i] - pts[j];
                        float len = d.magnitude;
                        if (len > 1e-5f)
                        {
                            float corr = (len - rest) / len * k;
                            if (r != rows - 1) pts[j] += d * corr * 0.5f;
                            pts[i] -= d * corr * 0.5f;
                        }
                    }
                    if (c > 0)
                    {
                        int j = i - 1;
                        Vector3 d = pts[i] - pts[j];
                        float len = d.magnitude;
                        float rest = Mathf.Abs(pts[i].x - pts[j].x) + 0.001f;
                        if (len > 1e-5f)
                        {
                            float corr = (len - rest) / len * k;
                            pts[j] += d * corr * 0.5f;
                            pts[i] -= d * corr * 0.5f;
                        }
                    }
                }
            }
            // 顶行钉住
            for (int c = 0; c < cols; c++)
            {
                int i = c;
                pts[i] = new Vector3((float)c / (cols - 1) * 0.52f - 0.26f, 0f, 0f);
            }
        }
        for (int i = 0; i < n; i++) display[i] = pts[i];
        mesh.vertices = display;
        mesh.RecalculateNormals();
    }
}
