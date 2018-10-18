using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneBuilder {

    private const string pbwTitle = "Scene rebuilder";
    private static string pbwMessage;
    private static float pbwProgress;

    /// <summary>
    /// Get all scene files in the build settings and rebuild them.
    /// </summary>
    [MenuItem("UnityFaction/Rebuild UF Scenes")]
    public static void RebuildAllScenes() {

        List<string> sceneFiles = new List<string>();
        foreach(EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            sceneFiles.Add(scene.path);

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
            " scenes. Note that this process will take " + timeEstimate + ".", 
            "Continue", "Cancel"))
            return;

        for(int i = 0; i < sceneFiles.Count; i++) {
            string sceneName = Path.GetFileNameWithoutExtension(sceneFiles[i]);
            pbwProgress = (float)i / sceneFiles.Count;
            pbwMessage = "Rebuilding " + sceneName + ": ";
            EditorUtility.DisplayProgressBar(pbwTitle, pbwMessage + "initializing.", pbwProgress);
            try {
                TryBuildScene(sceneFiles[i]);
            }
            catch(System.Exception e) {
                Debug.LogError("Failed to rebuild scene " + sceneName + ". Cause:\n" + e);
            }
        }

        EditorUtility.ClearProgressBar();
    }

    private static void TryBuildScene(string sceneFile) {
        string sceneName = Path.GetFileNameWithoutExtension(sceneFile);

        EditorUtility.DisplayProgressBar(pbwTitle, pbwMessage + "opening scene...", pbwProgress);
        EditorSceneManager.OpenScene(sceneFile, OpenSceneMode.Single);
        UFLevel level = Object.FindObjectOfType<UFLevel>();
        if(level == null) {
            Debug.LogWarning("Could not rebuild scene " + sceneName + 
                " since it did not contain any valid UF level structure.");
            return;
        }

        EditorUtility.DisplayProgressBar(pbwTitle, pbwMessage + "opening map builder...", pbwProgress);
        LevelBuilder builder = EditorWindow.GetWindow<LevelBuilder>();
        if(builder == null || !builder.TryLoadRefRFL()) {
            Debug.LogWarning("Could not rebuild scene " + sceneName + 
                " since builder could not load it.");
            return;
        }

        EditorUtility.DisplayProgressBar(pbwTitle, pbwMessage + "building UF level...", pbwProgress);
        builder.RefreshLevel();
        builder.BuildAll();

        EditorUtility.DisplayProgressBar(pbwTitle, pbwMessage + "baking lightmaps...", pbwProgress);
        BakeLightMaps();

        EditorUtility.DisplayProgressBar(pbwTitle, pbwMessage + "saving scene...", pbwProgress);
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene);
    }

    private static void BakeLightMaps() {
        LightmapSettings.lightmapsMode = LightmapsMode.CombinedDirectional;
        LightmapEditorSettings.prioritizeView = false;
        LightmapEditorSettings.directSampleCount = 16;
        LightmapEditorSettings.indirectSampleCount = 64;
        LightmapEditorSettings.lightmapper = LightmapEditorSettings.Lightmapper.ProgressiveCPU;
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
