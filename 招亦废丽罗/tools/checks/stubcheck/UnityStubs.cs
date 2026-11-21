// UnityStubs.cs —— 仅用于「无 Unity 环境下的 C# 编译检查」的 UnityEngine API 桩。
// 不参与 Unity 工程；由 tools/checks/stubcheck 项目引用，只为让 csc/Roslyn 做类型检查。
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator *(Vector2 a, float s) => new Vector2(a.x * s, a.y * s);
        public override string ToString() => "(" + x + ", " + y + ")";
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; this.z = 0f; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 left => new Vector3(-1, 0, 0);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 back => new Vector3(0, 0, -1);
        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z);
        public float sqrMagnitude => x * x + y * y + z * z;
        public Vector3 normalized => this / magnitude;
        public void Normalize() { }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.x * s, a.y * s, a.z * s);
        public static Vector3 operator *(float s, Vector3 a) => a * s;
        public static Vector3 operator /(Vector3 a, float s) => new Vector3(a.x / s, a.y / s, a.z / s);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * Mathf.Clamp01(t);
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => a + (b - a) * t;
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float d) => b;
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3 ClampMagnitude(Vector3 a, float m) => a;
        public static Vector3 SmoothDamp(Vector3 a, Vector3 b, ref Vector3 v, float t) => b;
        public override string ToString() => "(" + x + ", " + y + ", " + z + ")";
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);
        public static Quaternion Euler(float x, float y, float z) => identity;
        public static Quaternion Euler(Vector3 e) => identity;
        public static Quaternion LookRotation(Vector3 f) => identity;
        public static Quaternion LookRotation(Vector3 f, Vector3 up) => identity;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => b;
        public static Quaternion Lerp(Quaternion a, Quaternion b, float t) => b;
        public static Quaternion RotateTowards(Quaternion a, Quaternion b, float d) => b;
        public static Quaternion operator *(Quaternion a, Quaternion b) => a;
        public static Vector3 operator *(Quaternion q, Vector3 v) => v;
        public Vector3 eulerAngles => Vector3.zero;
        public static float Angle(Quaternion a, Quaternion b) => 0f;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1, 1);
        public static Color black => new Color(0, 0, 0, 1);
        public static Color clear => new Color(0, 0, 0, 0);
        public static Color red => new Color(1, 0, 0, 1);
        public static Color green => new Color(0, 1, 0, 1);
        public static Color blue => new Color(0, 0, 1, 1);
        public static Color yellow => new Color(1, 1, 0, 1);
        public static Color cyan => new Color(0, 1, 1, 1);
        public static Color magenta => new Color(1, 0, 1, 1);
        public static Color gray => new Color(.5f, .5f, .5f, 1);
        public static Color operator *(Color c, float s) => new Color(c.r * s, c.g * s, c.b * s, c.a * s);
        public static Color operator *(float s, Color c) => c * s;
        public static Color operator *(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        public static Color operator +(Color a, Color b) => new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);
        public static Color Lerp(Color a, Color b, float t) => new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t);
        public static implicit operator Color32(Color c) => new Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), (byte)(c.a * 255));
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color32 operator *(Color32 c, byte s) => c;
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
    }

    public struct Matrix4x4
    {
        public static Matrix4x4 identity => default(Matrix4x4);
        public static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s) => identity;
        public Vector3 MultiplyPoint3x4(Vector3 p) => p;
        public Vector3 MultiplyVector(Vector3 p) => p;
        public Matrix4x4 inverse => identity;
        public Matrix4x4 transpose => identity;
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b) => a;
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public float xMin => x;
        public float yMin => y;
        public float xMax => x + width;
        public float yMax => y + height;
    }

    public class RectOffset
    {
        public RectOffset() { }
        public RectOffset(int l, int r, int t, int b) { }
    }

    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public const float Rad2Deg = 57.29578f;
        public const float Deg2Rad = 0.0174533f;
        public static float Abs(float v) => Math.Abs(v);
        public static int Abs(int v) => Math.Abs(v);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Clamp(float v, float a, float b) => Math.Min(b, Math.Max(a, v));
        public static int Clamp(int v, int a, int b) => Math.Min(b, Math.Max(a, v));
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float LerpAngle(float a, float b, float t) => a;
        public static float Sin(float v) => (float)Math.Sin(v);
        public static float Cos(float v) => (float)Math.Cos(v);
        public static float Tan(float v) => (float)Math.Tan(v);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Sqrt(float v) => (float)Math.Sqrt(v);
        public static float Pow(float a, float b) => (float)Math.Pow(a, b);
        public static float Exp(float v) => (float)Math.Exp(v);
        public static float Floor(float v) => (float)Math.Floor(v);
        public static int FloorToInt(float v) => (int)Math.Floor(v);
        public static int CeilToInt(float v) => (int)Math.Ceiling(v);
        public static int RoundToInt(float v) => (int)Math.Round(v);
        public static float Round(float v) => (float)Math.Round(v);
        public static float Repeat(float t, float len) => t - (float)Math.Floor(t / len) * len;
        public static float DeltaAngle(float a, float b) => 0f;
        public static float InverseLerp(float a, float b, float v) => b == a ? 0f : Clamp01((v - a) / (b - a));
        public static float SmoothStep(float a, float b, float t) => Lerp(a, b, t * t * (3f - 2f * t));
        public static float Sign(float v) => v < 0f ? -1f : 1f;
        public static bool Approximately(float a, float b) => Math.Abs(a - b) < 1e-5f;
    }

    public class Object
    {
        public string name;
        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float t) { }
        public static void DestroyImmediate(Object o) { }
        public static implicit operator bool(Object o) => !ReferenceEquals(o, null);
        public static bool operator ==(Object a, Object b) => ReferenceEquals(a, b);
        public static bool operator !=(Object a, Object b) => !ReferenceEquals(a, b);
        public override bool Equals(object o) => ReferenceEquals(this, o);
        public override int GetHashCode() => base.GetHashCode();
    }

    public class Component : Object
    {
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public T GetComponent<T>() where T : Component => null;
        public T GetComponentInChildren<T>() where T : Component => null;
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
        public bool isActiveAndEnabled { get; }
    }

    public class MonoBehaviour : Behaviour { }

    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 right => Vector3.right;
        public Vector3 up => Vector3.up;
        public Vector3 forward => Vector3.forward;
        public Transform parent { get; set; }
        public int childCount => 0;
        public Transform GetChild(int i) => null;
        public void SetParent(Transform p) { }
        public void SetParent(Transform p, bool worldPositionStays) { }
        public void LookAt(Vector3 worldPos) { }
        public void LookAt(Transform t) { }
        public void LookAt(Vector3 worldPos, Vector3 up) { }
        public Vector3 TransformPoint(Vector3 p) => p;
        public Vector3 InverseTransformPoint(Vector3 p) => p;
        public Vector3 TransformDirection(Vector3 d) => d;
        public Vector3 InverseTransformDirection(Vector3 d) => d;
        public void Rotate(Vector3 e) { }
        public void Rotate(Vector3 axis, float angle) { }
        public void RotateAround(Vector3 point, Vector3 axis, float angle) { }
    }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { this.name = name; }
        public Transform transform { get; }
        public bool activeSelf { get; }
        public string tag { get; set; }
        public int layer { get; set; }
        public void SetActive(bool v) { }
        public T AddComponent<T>() where T : Component => null;
        public T GetComponent<T>() where T : Component => null;
        public T GetComponentInChildren<T>() where T : Component => null;
        public static GameObject CreatePrimitive(PrimitiveType t) => null;
        public static GameObject Find(string n) => null;
    }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }

    public enum Space { World, Self }

    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }

    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { }
    }

    public class Mesh : Object
    {
        public Vector3[] vertices { get; set; }
        public Vector3[] normals { get; set; }
        public Vector2[] uv { get; set; }
        public Color[] colors { get; set; }
        public Color32[] colors32 { get; set; }
        public int[] triangles { get; set; }
        public Rendering.IndexFormat indexFormat { get; set; }
        public Bounds bounds { get; set; }
        public void SetVertices(List<Vector3> v) { }
        public void SetVertices(Vector3[] v) { }
        public void SetNormals(List<Vector3> v) { }
        public void SetNormals(Vector3[] v) { }
        public void SetColors(List<Color> v) { }
        public void SetColors(Color[] v) { }
        public void SetColors(List<Color32> v) { }
        public void SetTriangles(List<int> v, int sub) { }
        public void SetTriangles(int[] v, int sub) { }
        public void RecalculateNormals() { }
        public void RecalculateBounds() { }
        public void MarkDynamic() { }
        public void Clear() { }
    }

    public struct Bounds
    {
        public Vector3 center, size, extents;
        public Bounds(Vector3 c, Vector3 s) { center = c; size = s; extents = s * 0.5f; }
        public void Expand(Bounds b) { }
    }

    public class Renderer : Component
    {
        public Material material { get; set; }
        public Material sharedMaterial { get; set; }
        public Material[] materials { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
        public bool enabled { get; set; }
    }

    public class MeshRenderer : Renderer { }

    public class MeshFilter : Component
    {
        public Mesh mesh { get; set; }
        public Mesh sharedMesh { get; set; }
    }

    public class Shader : Object
    {
        public static Shader Find(string name) => null;
        public static int PropertyToID(string name) => 0;
    }

    public class Texture : Object { }

    public enum TextureFormat { RGBA32, ARGB32, RGB24 }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp }

    public class Texture2D : Texture
    {
        public int width { get; }
        public int height { get; }
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public Texture2D(int w, int h) { }
        public Texture2D(int w, int h, TextureFormat f, bool mip) { }
        public void SetPixel(int x, int y, Color c) { }
        public void SetPixels(Color[] cols) { }
        public void SetPixels32(Color32[] cols) { }
        public void Apply() { }
    }

    public class Font : Object
    {
        public static Font CreateDynamicFontFromOSFont(string name, int size) => null;
    }

    public class Material : Object
    {
        public Material(Shader s) { }
        public Material(Material m) { }
        public Shader shader { get; set; }
        public Color color { get; set; }
        public Texture mainTexture { get; set; }
        public Vector2 mainTextureScale { get; set; }
        public int renderQueue { get; set; }
        public bool doubleSidedGI { get; set; }
        public MaterialGlobalIlluminationFlags globalIlluminationFlags { get; set; }
        public void SetColor(string n, Color c) { }
        public void SetFloat(string n, float v) { }
        public void SetInt(string n, int v) { }
        public void SetVector(string n, Vector4 v) { }
        public void SetTexture(string n, Texture t) { }
        public Color GetColor(string n) => Color.white;
        public float GetFloat(string n) => 0f;
        public void EnableKeyword(string k) { }
        public void DisableKeyword(string k) { }
        public bool HasProperty(string n) => true;
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    public enum MaterialGlobalIlluminationFlags { None, RealtimeEmissive, BakedEmissive, EmissiveIsBlack, AnyEmissive }

    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public LightShadows shadows { get; set; }
        public float shadowStrength { get; set; }
        public float range { get; set; }
        public float spotAngle { get; set; }
    }

    public enum LightType { Spot, Directional, Point, Area }
    public enum LightShadows { None, Hard, Soft }

    public class Camera : Behaviour
    {
        public static Camera main => null;
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public float aspect { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public Vector3 WorldToScreenPoint(Vector3 p) => p;
        public Vector3 ScreenToWorldPoint(Vector3 p) => p;
    }

    public enum CameraClearFlags { Skybox = 1, Color = 2, SolidColor = 2, Depth = 3, Nothing = 4 }

    public class AudioListener : Behaviour { }

    public class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public bool loop { get; set; }
        public float volume { get; set; }
        public void Play() { }
        public void Stop() { }
    }

    public class AudioClip : Object { }

    public class Font2 { }

    public static class Input
    {
        public static bool GetKey(KeyCode k) => false;
        public static bool GetKeyDown(KeyCode k) => false;
        public static bool GetKeyUp(KeyCode k) => false;
        public static bool GetMouseButton(int b) => false;
        public static bool GetMouseButtonDown(int b) => false;
        public static bool GetMouseButtonUp(int b) => false;
        public static float GetAxis(string n) => 0f;
        public static float GetAxisRaw(string n) => 0f;
        public static Vector3 mousePosition => Vector3.zero;
    }

    public enum KeyCode
    {
        None = 0, Backspace = 8, Delete = 127, Tab = 9, Return = 13, Escape = 27, Space = 32,
        Alpha0 = 48, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
        Keypad0 = 256, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
        A = 97, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        UpArrow = 273, DownArrow, RightArrow, LeftArrow, Insert = 277, Home = 278, End = 279, PageUp = 280, PageDown = 281,
        F1 = 282, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
        LeftShift = 304, RightShift = 303, LeftControl = 306, RightControl = 305, LeftAlt = 308, RightAlt = 307,
        Mouse0 = 323, Mouse1 = 324, Mouse2 = 325
    }

    public static class Time
    {
        public static float deltaTime => 0.016f;
        public static float unscaledDeltaTime => 0.016f;
        public static float time => 0f;
        public static float timeScale { get; set; }
        public static float fixedDeltaTime => 0.02f;
        public static int frameCount => 0;
    }

    public static class Screen
    {
        public static int width => 1280;
        public static int height => 720;
        public static bool fullScreen { get; set; }
    }

    public static class Application
    {
        public static string streamingAssetsPathOverride = "";
        public static string persistentDataPathOverride = "";
        public static string dataPathOverride = "";
        public static string streamingAssetsPath => streamingAssetsPathOverride;
        public static string persistentDataPath => persistentDataPathOverride;
        public static string dataPath => dataPathOverride;
        public static int targetFrameRate { get; set; }
        public static string productName => "";
        public static bool isPlaying => true;
        public static void Quit() { }
    }

    public static class Debug
    {
        public static void Log(object o) { }
        public static void LogWarning(object o) { }
        public static void LogError(object o) { }
    }

    public static class SystemInfo
    {
        public static string graphicsDeviceName => "";
        public static string deviceName => "";
    }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object => null;
    }

    public static class Random
    {
        public static float value => 0.5f;
        public static float Range(float a, float b) => a;
        public static int Range(int a, int b) => a;
        public static void InitState(int seed) { }
    }

    public static class RenderSettings
    {
        public static Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientLight { get; set; }
        public static float ambientIntensity { get; set; }
        public static bool fog { get; set; }
        public static FogMode fogMode { get; set; }
        public static Color fogColor { get; set; }
        public static float fogStartDistance { get; set; }
        public static float fogEndDistance { get; set; }
        public static Material skybox { get; set; }
    }

    public enum FogMode { Linear = 1, Exponential = 2, ExponentialSquared = 3 }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }

    public class GUIStyleState
    {
        public Color textColor { get; set; }
        public Texture2D background { get; set; }
    }

    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public TextAnchor alignment { get; set; }
        public bool wordWrap { get; set; }
        public bool richText { get; set; }
        public GUIStyleState normal { get; } = new GUIStyleState();
        public RectOffset padding { get; set; }
        public RectOffset margin { get; set; }
    }

    public class GUISkin
    {
        public GUIStyle label { get; } = new GUIStyle();
        public GUIStyle box { get; } = new GUIStyle();
        public GUIStyle button { get; } = new GUIStyle();
    }

    public static class GUI
    {
        public static GUISkin skin { get; set; } = new GUISkin();
        public static Color color { get; set; }
        public static void Label(Rect r, string text, GUIStyle style) { }
        public static void Label(Rect r, string text) { }
        public static void DrawTexture(Rect r, Texture t) { }
        public static void Box(Rect r, string text, GUIStyle style) { }
        public static void Box(Rect r, string text) { }
    }

    public static class Cursor
    {
        public static CursorLockMode lockState { get; set; }
        public static bool visible { get; set; }
    }

    public enum CursorLockMode { None, Locked, Confined }

    public enum FullScreenMode { ExclusiveFullScreen = 0, FullScreenWindow = 1, MaximizedWindow = 2, Windowed = 3 }

    public enum ColorSpace { Uninitialized = -1, Gamma = 0, Linear = 1 }
}

namespace UnityEngine.Rendering
{
    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3, Custom = 4 }
    public enum IndexFormat { UInt16, UInt32 }
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
}
