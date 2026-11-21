// Terrain.cs —— 世界流式地形 / 环境道具 / 湖面 / 阴影贴片
using System.Collections.Generic;
using UnityEngine;

public class TerrainStreamer
{
    public Transform root;
    readonly Dictionary<long, GameObject> chunks = new Dictionary<long, GameObject>();
    readonly List<long> pending = new List<long>();
    public int radius = 4;
    Mesh waterMesh;
    Transform water;
    public Material waterMat;

    public int Count { get { return chunks.Count; } }

    static long Key(int cx, int cz)
    {
        return ((long)cx << 32) ^ (uint)cz;
    }

    public void Init(Transform parent)
    {
        root = Make.Node("世界", parent, Vector3.zero);
        waterMat = Mats.Water;
        BuildWater(parent);
    }

    void BuildWater(Transform parent)
    {
        // 泪湖：椭圆水面 y=7
        int segs = 64;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        verts.Add(new Vector3(0f, 0f, 0f));
        for (int k = 0; k <= segs; k++)
        {
            float a = (float)k / segs * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(a) * 205f, 0f, Mathf.Sin(a) * 205f * 1.35f));
        }
        for (int k = 1; k <= segs; k++)
        {
            tris.Add(0); tris.Add(k + 1); tris.Add(k);
        }
        var m = new Mesh();
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        waterMesh = m;
        var go = Make.MeshObject("泪湖", parent, m, waterMat, false);
        go.transform.localPosition = new Vector3(0f, 7f, -540f);
        water = go.transform;
    }

    public void Update(Vector3 playerPos, int maxBuild)
    {
        if (root == null) return;
        int pcx = Mathf.FloorToInt((playerPos.x + GameData.WORLD_HALF) / GameData.CHUNK);
        int pcz = Mathf.FloorToInt((playerPos.z + GameData.WORLD_HALF) / GameData.CHUNK);

        // 卸载过远
        List<long> drop = null;
        foreach (var kv in chunks)
        {
            int cx = (int)(kv.Key >> 32);
            int cz = (int)(kv.Key & 0xFFFFFFFF);
            if (Mathf.Abs(cx - pcx) > radius + 1 || Mathf.Abs(cz - pcz) > radius + 1)
            {
                if (drop == null) drop = new List<long>();
                drop.Add(kv.Key);
            }
        }
        if (drop != null)
        {
            for (int k = 0; k < drop.Count; k++)
            {
                GameObject go = chunks[drop[k]];
                chunks.Remove(drop[k]);
                if (go != null) Object.Destroy(go);
            }
        }

        // 补建（每帧限制数量，避免卡顿）
        int built = 0;
        for (int dz = -radius; dz <= radius && built < maxBuild; dz++)
        {
            for (int dx = -radius; dx <= radius && built < maxBuild; dx++)
            {
                int cx = pcx + dx, cz = pcz + dz;
                long key = Key(cx, cz);
                if (chunks.ContainsKey(key)) continue;
                BuildChunk(cx, cz, key);
                built++;
            }
        }

        if (water != null)
            water.localPosition = new Vector3(0f, 7f, -540f);
    }

    void BuildChunk(int cx, int cz, long key)
    {
        float x0 = cx * GameData.CHUNK - GameData.WORLD_HALF;
        float z0 = cz * GameData.CHUNK - GameData.WORLD_HALF;
        int segs = GameData.CHUNK_SEGS;
        int stride = segs + 1;
        int n = stride * stride;
        var verts = new Vector3[n];
        var normals = new Vector3[n];
        var colors = new Color[n];
        var tris = new int[segs * segs * 6];
        float dx = GameData.CHUNK / segs;

        for (int j = 0; j < stride; j++)
        {
            for (int i = 0; i < stride; i++)
            {
                int idx = j * stride + i;
                float x = x0 + i * dx;
                float z = z0 + j * dx;
                float y = GameData.HeightAt(x, z);
                verts[idx] = new Vector3(x - x0, y, z - z0);
            }
        }

        // 法线（中心差分）
        for (int j = 0; j < stride; j++)
        {
            for (int i = 0; i < stride; i++)
            {
                int idx = j * stride + i;
                float hl = verts[j * stride + Mathf.Max(0, i - 1)].y;
                float hr = verts[j * stride + Mathf.Min(stride - 1, i + 1)].y;
                float hd = verts[Mathf.Max(0, j - 1) * stride + i].y;
                float hu = verts[Mathf.Min(stride - 1, j + 1) * stride + i].y;
                normals[idx] = new Vector3(-(hr - hl) / (2f * dx), 1f, -(hu - hd) / (2f * dx)).normalized;
            }
        }

        // 配色：区域色 + 噪声 + 坡度 + 高海拔暖色
        for (int j = 0; j < stride; j++)
        {
            for (int i = 0; i < stride; i++)
            {
                int idx = j * stride + i;
                float wx = x0 + i * dx;
                float wz = z0 + j * dx;
                Color baseCol = GameData.RegionColor(GameData.RegionAt(wx, wz));
                float n1 = GameData.Noise2(wx * 0.045f, wz * 0.045f);
                float n2 = GameData.Noise2(wx * 0.012f + 91f, wz * 0.012f - 17f);
                float slope = Mathf.Sqrt(normals[idx].x * normals[idx].x + normals[idx].z * normals[idx].z);
                float k = (0.82f + 0.22f * n1 + 0.12f * n2) * (1f - 0.30f * Mathf.Clamp01(slope));
                float hnorm = Mathf.Clamp01((verts[idx].y - 10f) / 120f);
                Color warm = new Color(0.34f, 0.22f, 0.10f);
                Color c = baseCol * k + warm * (hnorm * 0.45f);
                c.r = Mathf.Clamp01(c.r); c.g = Mathf.Clamp01(c.g); c.b = Mathf.Clamp01(c.b);
                colors[idx] = c;
            }
        }

        int t = 0;
        for (int j = 0; j < segs; j++)
        {
            for (int i = 0; i < segs; i++)
            {
                int a = j * stride + i;
                int b = a + 1;
                int c = a + stride;
                int d = c + 1;
                tris[t++] = a; tris[t++] = c; tris[t++] = b;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
            }
        }

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(new List<Vector3>(verts));
        mesh.SetNormals(new List<Vector3>(normals));
        mesh.SetColors(new List<Color>(colors));
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();

        GameObject go = Make.MeshObject("区块_" + cx + "_" + cz, root, mesh, Mats.VertexColor, false);
        go.transform.localPosition = new Vector3(x0, 0f, z0);
        chunks[key] = go;

        // 道具（合并进单 mesh）
        var props = BuildProps(cx, cz, x0, z0);
        if (props != null)
        {
            GameObject pg = Make.MeshObject("道具_" + cx + "_" + cz, root, props, Mats.VertexColor, false);
            pg.transform.localPosition = new Vector3(x0, 0f, z0);
            propsHolder[Key(cx, cz)] = pg;
        }
    }

    readonly Dictionary<long, GameObject> propsHolder = new Dictionary<long, GameObject>();

    Mesh BuildProps(int cx, int cz, float x0, float z0)
    {
        var rng = new System.Random((int)(((uint)cx * 73856093u) ^ ((uint)cz * 19349663u) ^ 0x5EED2026u));
        var meshes = new List<Mesh>();
        var mats = new List<Matrix4x4>();
        var cols = new List<Color>();
        int count = 10 + rng.Next(10);
        for (int k = 0; k < count; k++)
        {
            float lx = (float)rng.NextDouble() * GameData.CHUNK;
            float lz = (float)rng.NextDouble() * GameData.CHUNK;
            float wx = x0 + lx, wz = z0 + lz;
            if (GameData.InLake(wx, wz)) continue;
            float y = GameData.HeightAt(wx, wz);
            float pick = (float)rng.NextDouble();
            if (pick < 0.55f)
            {
                float s = 0.5f + (float)rng.NextDouble() * 1.4f;
                var mtx = Matrix4x4.TRS(new Vector3(lx, y - s * 0.25f, lz),
                    Quaternion.Euler((float)rng.NextDouble() * 40f, (float)rng.NextDouble() * 360f, 0f),
                    new Vector3(s * (0.8f + (float)rng.NextDouble() * 0.8f), s * 0.7f, s));
                meshes.Add(Geo.Octa());
                mats.Add(mtx);
                float g = 0.30f + (float)rng.NextDouble() * 0.10f;
                cols.Add(new Color(g + 0.04f, g, g - 0.03f));
            }
            else if (pick < 0.92f)
            {
                int clumps = 2 + rng.Next(3);
                for (int c = 0; c < clumps; c++)
                {
                    float ox = ((float)rng.NextDouble() - 0.5f) * 1.2f;
                    float oz = ((float)rng.NextDouble() - 0.5f) * 1.2f;
                    float h = 0.24f + (float)rng.NextDouble() * 0.22f;
                    var mtx = Matrix4x4.TRS(new Vector3(lx + ox, y + h * 0.4f, lz + oz),
                        Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f),
                        new Vector3(0.05f, h, 0.05f));
                    meshes.Add(Geo.Cone(4));
                    mats.Add(mtx);
                    cols.Add(new Color(0.24f + (float)rng.NextDouble() * 0.10f, 0.30f, 0.13f));
                }
            }
            else
            {
                float s = 1.6f + (float)rng.NextDouble() * 1.6f;
                var mtx = Matrix4x4.TRS(new Vector3(lx, y + s * 0.5f, lz),
                    Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f),
                    new Vector3(0.14f, s, 0.14f));
                meshes.Add(Geo.Cylinder(6, true));
                mats.Add(mtx);
                cols.Add(new Color(0.20f, 0.16f, 0.13f));
                for (int b = 0; b < 2; b++)
                {
                    var bm = Matrix4x4.TRS(new Vector3(lx, y + s * (0.55f + 0.16f * b), lz),
                        Quaternion.Euler(18f + b * 26f, (float)rng.NextDouble() * 360f, 28f - b * 40f),
                        new Vector3(0.06f, s * 0.55f, 0.06f));
                    meshes.Add(Geo.Cylinder(5, true));
                    mats.Add(bm);
                    cols.Add(new Color(0.18f, 0.15f, 0.12f));
                }
            }
        }
        if (meshes.Count == 0) return null;
        return Geo.Merge(meshes, mats, cols, true);
    }
}

// ---------------------------------------------------------------- 阴影贴片
public class BlobShadow
{
    public GameObject go;
    public Transform tf;

    public static BlobShadow Create(Transform parent, float size)
    {
        var b = new BlobShadow();
        var quad = Geo.Quad();
        var mat = new Material(Mats.Blob);
        mat.color = new Color(1f, 1f, 1f, 0.55f);
        b.go = Make.MeshObject("阴影", parent, quad, mat, false);
        b.go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        b.go.transform.localScale = new Vector3(size, size, 1f);
        b.go.GetComponent<MeshRenderer>().sharedMaterial.renderQueue = 2900;
        b.tf = b.go.transform;
        return b;
    }

    public void Follow(Vector3 pos)
    {
        float y = GameData.HeightAt(pos.x, pos.z);
        tf.position = new Vector3(pos.x, y + 0.03f, pos.z);
    }
}
