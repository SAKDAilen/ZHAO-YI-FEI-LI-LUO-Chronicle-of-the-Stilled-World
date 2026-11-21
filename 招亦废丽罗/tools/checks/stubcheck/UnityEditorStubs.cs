// UnityEditorStubs.cs —— 仅用于无 Unity 环境下的静态编译检查（不是游戏运行时代码）
using System;
using UnityEngine;

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        public string path;
        public bool IsValid() { return true; }
    }
}

namespace UnityEditor
{
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }

    public class EditorApplication
    {
        public static Action delayCall;
        public static bool isPlayingOrWillChangePlaymode;
        public static bool isPlaying;
    }

    public class EditorBuildSettingsScene
    {
        public string path;
        public bool enabled;
        public EditorBuildSettingsScene(string path, bool enabled) { this.path = path; this.enabled = enabled; }
    }

    public class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[0];
    }

    public class PlayerSettings
    {
        public static string productName;
        public static string companyName;
        public static int defaultScreenWidth;
        public static int defaultScreenHeight;
        public static UnityEngine.FullScreenMode fullScreenMode;
        public static bool resizableWindow;
        public static UnityEngine.ColorSpace colorSpace;
    }

    public class AssetDatabase
    {
        public static void Refresh() { }
    }

    public class InitializeOnLoadAttribute : Attribute { }
    public class InitializeOnLoadMethodAttribute : Attribute { }
    public class MenuItemAttribute : Attribute { public MenuItemAttribute(string item) { } }
}

namespace UnityEditor.SceneManagement
{
    public class EditorSceneManager
    {
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup setup, NewSceneMode mode)
        {
            return new UnityEngine.SceneManagement.Scene();
        }

        public static bool SaveScene(UnityEngine.SceneManagement.Scene scene, string path) { return true; }
        public static bool CloseScene(UnityEngine.SceneManagement.Scene scene, bool removeScene) { return true; }
        public static UnityEngine.SceneManagement.Scene OpenScene(string path)
        {
            return new UnityEngine.SceneManagement.Scene();
        }
    }
}
