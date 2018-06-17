using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;
using System.Collections.Generic;
using UFLevelStructure;

public class LevelBuilder : EditorWindow {

    private static string lastRFLPath;
    private UFLevel level;

    //GUI variables
    bool showGeneralContents, showGeometryContents, showObjectContents, showMoverContents;

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
        if(!Directory.Exists(builder.assetPath))
            AssetDatabase.CreateFolder(Path.GetDirectoryName(lastRFLPath), "_UFAssets");
    }

    private void OnGUI() {
        GUIStyle header = new GUIStyle();
        header.fontSize = 22;
        GUILayout.Label("UNITY FACTION LEVEL BUILDER", header);
        if(level == null) {
            if(GUILayout.Button("Load RFL file"))
                BuildLevel();
            return;
        }

        string fileName = Path.GetFileNameWithoutExtension(lastRFLPath);
        GUILayout.Label("Loaded file: " + fileName, EditorStyles.largeLabel);
        GUIStyle contentFoldout = new GUIStyle("foldout");
        contentFoldout.fontStyle = FontStyle.Bold;
        contentFoldout.fontSize = 14;

        showGeneralContents = EditorGUILayout.Foldout(showGeneralContents, "General content", contentFoldout);
        if(showGeneralContents) {
            level.name = EditorGUILayout.TextField("   Level name", level.name);
            level.author = EditorGUILayout.TextField("   Author name", level.author);
            level.multiplayer = EditorGUILayout.Toggle("   Multiplayer", level.multiplayer);
            level.playerStart.position = EditorGUILayout.Vector3Field("   Player start", level.playerStart.position);
        }

        showGeometryContents = EditorGUILayout.Foldout(showGeometryContents, "Static geometry content", contentFoldout);
        if(showGeometryContents) {
            GUILayout.Label("   Vertices: " + level.staticGeometry.vertices.Length);
            GUILayout.Label("   Faces: " + level.staticGeometry.faces.Length);
            GUILayout.Label("   Rooms: " + level.staticGeometry.rooms.Length);
        }

        showObjectContents = EditorGUILayout.Foldout(showObjectContents, "Object content", contentFoldout);
        if(showObjectContents) {
            GUILayout.Label("   Lights: " + level.lights.Length);
            GUILayout.Label("   Ambient sounds: " + level.ambSounds.Length);
            GUILayout.Label("   Events: " + level.events.Length);
            GUILayout.Label("   Multi spawn points: " + level.spawnPoints.Length);
            GUILayout.Label("   Particle emiters: " + level.particleEmiters.Length);
            GUILayout.Label("   Decals: " + level.decals.Length);
            GUILayout.Label("   Climbing regions : " + level.climbingRegions.Length);
            GUILayout.Label("   Bolt emiters: " + level.boltEmiters.Length);
            GUILayout.Label("   Targets: " + level.targets.Length);
            GUILayout.Label("   Entities: " + level.entities.Length);
            GUILayout.Label("   Items: " + level.items.Length);
            GUILayout.Label("   Clutter: " + level.clutter.Length);
            GUILayout.Label("   Triggers: " + level.triggers.Length);
        }

        showMoverContents = EditorGUILayout.Foldout(showMoverContents, "Mover content", contentFoldout);
        if(showMoverContents) {
            GUILayout.Label("   Moving brushes: " + level.movingGeometry.Length);
            GUILayout.Label("   Moving groups: " + level.movingGroups.Length);
        }

        if(root == null) {
            if(GUILayout.Button("Make root"))
                MakeRoot();
            return;
        }

        GUIStyle bigButton = new GUIStyle("button");
        bigButton.fontSize = 26;
        if(GUILayout.Button("Build all", bigButton)) {
            //TODO
            Debug.LogWarning("not yet implemented");
        }

        if(GUILayout.Button("Build static geometry")) {
            BuildStaticGeometry();
        }
    }

    /* -----------------------------------------------------------------------------------------------
     * ---------------------------------- SPECIFIC BUILD METHODS -------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    private void BuildStaticGeometry() {
        //make object
        GameObject g = new GameObject("StaticGeometry");
        g.transform.SetParent(root);
        Mesh mesh = new Mesh();

        //mesh
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int>[] triangles = new List<int>[level.staticGeometry.textures.Length];
        for(int i = 0; i < triangles.Length; i++)
            triangles[i] = new List<int>();

        foreach(Face face in level.staticGeometry.faces) {
            //extract required data
            int nboPoints = face.vertices.Length;
            Vector3[] faceVertices = new Vector3[nboPoints];
            Vector2[] faceUVs = new Vector2[nboPoints];
            for(int i = 0; i < nboPoints; i++) {
                faceVertices[i] = level.staticGeometry.vertices[face.vertices[i].vertexRef];
                faceUVs[i] = new Vector2(face.vertices[i].uv.x, -face.vertices[i].uv.y);
            }

            //do triangulation
            int[] faceTriangles = Triangulator.BasePoint(faceVertices);

            //shift vertex references to match those in the mesh list
            for(int i = 0; i < faceTriangles.Length; i++)
                faceTriangles[i] += vertices.Count;

            //add results to mesh
            vertices.AddRange(faceVertices);
            uvs.AddRange(faceUVs);
            int subIdx = face.texture;
            if(subIdx < 0)
                subIdx = 0;
            triangles[subIdx].AddRange(faceTriangles);
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = triangles.Length;
        for(int i = 0; i < triangles.Length; i++)
            mesh.SetTriangles(triangles[i], i);
        
        mesh.RecalculateNormals();
        mesh.name = level.name + "StaticGeometry";
        mesh.RecalculateBounds();

        (g.AddComponent<MeshFilter>()).sharedMesh = mesh;

        //materials
        Material[] materials = new Material[level.staticGeometry.textures.Length];
        for(int i = 0; i < materials.Length; i++)
            materials[i] = GetMaterial(level.staticGeometry.textures[i]);
        (g.AddComponent<MeshRenderer>()).materials = materials;
    }

    /* -----------------------------------------------------------------------------------------------
     * -------------------------------------- HELPER METHODS -----------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    private string assetPath {  get { return Path.GetDirectoryName(lastRFLPath) + "/_UFAssets/"; } }
    private string[] searchFolders { get { return new string[] { assetPath }; } }

    private Transform root
    {
        get
        {
            if(level == null)
                return null;
            GameObject toReturn = GameObject.Find(level.name);
            if(toReturn != null)
                return toReturn.transform;
            return null;
        }
    }

    private void MakeRoot() {
        if(root == null)
            new GameObject(level.name);
    }

    private Material GetMaterial(string texture) {
        string textureName = Path.GetFileNameWithoutExtension(texture);
        string materialName = textureName + ".mat";
        string[] results = AssetDatabase.FindAssets(textureName, searchFolders);

        string texPath = null;
        foreach(string result in results) {
            string resultPath = AssetDatabase.GUIDToAssetPath(result);
            string resultName = Path.GetFileName(resultPath);
            if(resultName == materialName)
                return (Material)AssetDatabase.LoadAssetAtPath(resultPath, typeof(Material));
            if(resultName == texture)
                texPath = resultPath;
        }
           
        if(texPath != null) {
            //material doesn't exist, but the texture does, so we can make a new material
            Material mat = GenerateDefaultMat();
            mat.mainTexture = (Texture)AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture));
            AssetDatabase.CreateAsset(mat, assetPath + materialName);
            return mat;
        }

        //neither material nor texture exists
        Debug.LogWarning("Could not find texture: " + texture);
        return GenerateDefaultMat();
    }

    private Material GenerateDefaultMat() {
        return new Material(Shader.Find("Standard"));
    }
}

