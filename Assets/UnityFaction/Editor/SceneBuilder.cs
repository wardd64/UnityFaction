using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneBuilder {

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
        List<string> sceneFiles = new List<string>();

        foreach(string file in files) {
            if(Path.GetExtension(file).ToLower() == ".unity")
                sceneFiles.Add(file);
        }

        int nboScenes = sceneFiles.Count;
        string timeEstimate = "";
        int lowHour = Mathf.FloorToInt(nboScenes / 60f);
        int highHour = Mathf.CeilToInt(nboScenes / 12f);

        if(nboScenes < 30)
            timeEstimate = nboScenes + " to " + (nboScenes * 5) + " minutes";
        else if(nboScenes < 60)
            timeEstimate = nboScenes + " minutes to " + highHour + " hours";
        else
            timeEstimate = lowHour + " to " + highHour + " hours";

        if(!EditorUtility.DisplayDialog("Confirm Rebuild", 
            "You are about to rebuild " + nboScenes + 
            " scenes. Note that this process will take " + timeEstimate + 
            ", and unity will freeze until it is completed.", 
            "Continue", "Cancel"))
            return;

        foreach(string file in sceneFiles) 
            TryBuildScene(file);
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
