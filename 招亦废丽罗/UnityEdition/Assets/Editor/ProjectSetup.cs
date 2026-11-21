// ProjectSetup.cs —— 编辑器自动初始化：创建场景、加入构建列表、设置产品信息
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class ProjectSetup
{
    static ProjectSetup()
    {
        EditorApplication.delayCall += EnsureProject;
    }

    static void EnsureProject()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        try
        {
            PlayerSettings.productName = "招亦废丽罗 · 律令之楔";
            PlayerSettings.companyName = "Zhaoyi Studio";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }
        catch { }

        string dir = "Assets/Scenes";
        string path = dir + "/Main.unity";
        string abs = Path.Combine(Application.dataPath, "Scenes/Main.unity");
        bool exists = File.Exists(abs);

        if (!exists)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(abs));
            // 以 Additive 方式创建空场景，避免打断用户当前打开的场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            // 场景可以为空：GameManager 会在运行时自动搭建世界
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();
            exists = true;
            Debug.Log("[ProjectSetup] 已创建 Assets/Scenes/Main.unity");
        }

        if (exists)
        {
            bool has = false;
            var scenes = EditorBuildSettings.scenes;
            for (int k = 0; k < scenes.Length; k++)
            {
                if (scenes[k].path == path && scenes[k].enabled) { has = true; break; }
            }
            if (!has)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
                list.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = list.ToArray();
                Debug.Log("[ProjectSetup] 已将 Main.unity 加入构建场景列表");
            }
        }
    }
}
#endif
