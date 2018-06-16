using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;
using System.Collections.Generic;

public class LevelBuilder : EditorWindow {

    private static string lastRFLPath;

    private UFLevel level;

    //----------------------------------------- Editor window ---------------------------------------------

    [MenuItem("UnityFaction/Build Level")]
    public static void BuildLevel() {
        //let user select rfl file that needs to be built into the scene
        string fileSearchMessage = "Select rfl file you would like to build";
        string defaultPath = "Assets";
        if(!string.IsNullOrEmpty(lastRFLPath))
            defaultPath = lastRFLPath;

        string rflPath = EditorUtility.OpenFilePanel(fileSearchMessage, "Assets", "rfl");
        if(string.IsNullOrEmpty(rflPath))
            return;

        LevelBuilder builder = (LevelBuilder)EditorWindow.GetWindow(typeof(LevelBuilder));
        builder.Show();

        lastRFLPath = UFUtils.GetRelativeUnityPath(rflPath);
        RFLReader reader = new RFLReader(rflPath);

        builder.level = reader.level;
    }

    private void OnGUI() {
        GUILayout.Label("UNITY FACTION LEVEL BUILDER", EditorStyles.boldLabel);
        if(level == null) {
            if(GUILayout.Button("Load RFL file"))
                BuildLevel();
            return;
        }

        string fileName = Path.GetFileNameWithoutExtension(lastRFLPath);
        GUILayout.Label("Loaded file: " + fileName, EditorStyles.largeLabel);

        level.name = EditorGUILayout.TextField("Level name", level.name);
        level.author = EditorGUILayout.TextField("Author name", level.author);
        level.multiplayer = EditorGUILayout.Toggle("Multiplayer", level.multiplayer);
        level.playerStart.position = EditorGUILayout.Vector3Field("Player start", level.playerStart.position);
        GUILayout.Label("");

        GUILayout.Label("Found static geometry: ", EditorStyles.largeLabel);
        GUILayout.Label("Vertices: " + level.staticGeometry.vertices.Length);
        GUILayout.Label("Faces: " + level.staticGeometry.faces.Length);
        GUILayout.Label("Rooms: " + level.staticGeometry.rooms.Length);
        GUILayout.Label("");

        GUILayout.Label("Found objects: ", EditorStyles.largeLabel);
        GUILayout.Label("Lights: " + level.lights.Length);
        GUILayout.Label("Ambient sounds: " + level.ambSounds.Length);
        GUILayout.Label("Events: " + level.events.Length);
        GUILayout.Label("Multi spawn points: " + level.spawnPoints.Length);
        GUILayout.Label("Particle emiters: " + level.particleEmiters.Length);
        GUILayout.Label("Decals: " + level.decals.Length);
        GUILayout.Label("Climbing regions : " + level.climbingRegions.Length);
        GUILayout.Label("Bolt emiters: " + level.boltEmiters.Length);
        GUILayout.Label("Targets: " + level.targets.Length);
        GUILayout.Label("Entities: " + level.entities.Length);
        GUILayout.Label("Items: " + level.items.Length);
        GUILayout.Label("Clutter: " + level.clutter.Length);
        GUILayout.Label("Triggers: " + level.triggers.Length);
        GUILayout.Label("");

        GUILayout.Label("Found movers: ", EditorStyles.largeLabel);
        GUILayout.Label("Moving brushes: " + level.movingGeometry.Length);
        GUILayout.Label("Moving groups: " + level.movingGroups.Length);
    }
}

    