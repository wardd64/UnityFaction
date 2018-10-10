using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneBuilder {

    private static int sceneCount;
    private static int nboScenes;

    /// <summary>
    /// Read RFL file and build its contents into the current Unity scene.
    /// </summary>
    [MenuItem("UnityFaction/Rebuild UF Scenes")]
    public static void RebuildAllScenes() {
        //let user select rfl file that needs to be built into the scene
        string fileSearchMessage = "Select folder in which to look for scenes";
        string defaultPath = "Assets";

        string sceneFolder = EditorUtility.OpenFolderPanel(fileSearchMessage, defaultPath, "");
        if(string.IsNullOrEmpty(sceneFolder))
            return;

        string[] files = Directory.GetFiles(sceneFolder);
        nboScenes = files.Length;
        sceneCount = 0;

        foreach(string file in files) {
            if(Path.GetExtension(file).ToLower() == ".unity") {
                TryBuildScene(file);
                return;
            }
        }
    }

    private static void TryBuildScene(string sceneFile) {
        string sceneName = Path.GetFileNameWithoutExtension(sceneFile);
        EditorSceneManager.OpenScene(sceneFile, OpenSceneMode.Single);
        UFLevel level = Object.FindObjectOfType<UFLevel>();
        if(level == null) {
            Debug.LogWarning("Could not rebuild scene " + sceneName + 
                " since it did not contain any valid UF level structure.");
            return;
        }

        LevelBuilder builder = EditorWindow.GetWindow<LevelBuilder>();
        if(builder == null || !builder.TryLoadRefRFL()) {
            Debug.LogWarning("Could not rebuild scene " + sceneName + 
                " since builder could not load it.");
            return;
        }

        builder.RefreshLevel();
        builder.BuildAll();

        BakeLightMaps();

        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);

        sceneCount++;
        Debug.Log("Done rebuilding scene: " + sceneName + 
            " (" + sceneCount + "/" + nboScenes + ")");
    }

    private static void BakeLightMaps() {
        LightmapSettings.lightmapsMode = LightmapsMode.CombinedDirectional;
        LightmapEditorSettings.lightmapper = LightmapEditorSettings.Lightmapper.Enlighten;
        LightmapEditorSettings.realtimeResolution = .2f;
        LightmapEditorSettings.bakeResolution = 2f;
        LightmapEditorSettings.maxAtlasSize = 1024;
        LightmapEditorSettings.textureCompression = true;
        LightmapEditorSettings.enableAmbientOcclusion = true;
        Lightmapping.bakedGI = true;
        Lightmapping.realtimeGI = true;

        Lightmapping.Bake();
    }

    private LightmapParameters GetLMP() {
        string fileName = "UFLighting";
        string fullName = fileName + ".giparams";
        string[] results = AssetDatabase.FindAssets(fileName);

        foreach(string result in results) {
            string resultPath = AssetDatabase.GUIDToAssetPath(result);
            string resultName = Path.GetFileName(resultPath);

            if(resultName == fullName)
                return (LightmapParameters)AssetDatabase.LoadAssetAtPath(resultPath, typeof(LightmapParameters));
        }

        return new LightmapParameters();
    }
}
