using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;
using System.Collections.Generic;
using UFLevelStructure;

public class LevelBuilder : EditorWindow {

    private static string lastRFLPath;
    private LevelData level;

    //GUI variables
    bool showGeneralContents, showGeometryContents, 
        showObjectContents, showMoverContents;

    //Build options
    //...

    [MenuItem("UnityFaction/Build Level")]
    public static void BuildLevel() {
        //let user select rfl file that needs to be built into the scene
        string fileSearchMessage = "Select rfl file you would like to build";
        string defaultPath = "Assets";
        if(!string.IsNullOrEmpty(lastRFLPath))
            defaultPath = lastRFLPath;

        string rflPath = EditorUtility.OpenFilePanel(fileSearchMessage, defaultPath, "rfl");
        if(string.IsNullOrEmpty(rflPath))
            return;

        LevelBuilder builder = (LevelBuilder)EditorWindow.GetWindow(typeof(LevelBuilder));
        builder.Show();

        lastRFLPath = UFUtils.GetRelativeUnityPath(rflPath);
        RFLReader reader = new RFLReader(rflPath);

        builder.level = reader.level;
        if(!Directory.Exists(assetPath))
            AssetDatabase.CreateFolder(Path.GetDirectoryName(lastRFLPath), VPPUnpacker.assetSubFolder);
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

        if(GUILayout.Button("Build static geometry"))
            BuildStaticGeometry();

        if(GUILayout.Button("Build player info"))
            BuildPlayerInfo();

        if(GUILayout.Button("Build geomodder"))
            BuildGeoModer();

        if(GUILayout.Button("Build movers"))
            BuildMovers();

        if(GUILayout.Button("Build triggers"))
            BuildTriggers();
    }

    /* -----------------------------------------------------------------------------------------------
     * ---------------------------------- SPECIFIC BUILD METHODS -------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    private void BuildStaticGeometry() {
        int split = 2;
        List<Face>[] faceSplit = new List<Face>[split];

        for(int i = 0; i < split; i++)
            faceSplit[i] = new List<Face>();

        //split faces
        int nboFace = level.staticGeometry.faces.Length;
        for(int i = 0; i < nboFace; i++) {
            Face nextFace = level.staticGeometry.faces[i];
            int texIdx = Mathf.Max(0, nextFace.texture);
            string nextTex = level.staticGeometry.textures[texIdx];
            bool invis = nextTex.ToLower().Contains("invis");
            int sort = invis ? 1 : 0;

            faceSplit[sort].Add(nextFace);
        }


        //make objects
        Transform p = MakeParent("StaticGeometry");
        GameObject visG = MakeMeshObject(level.staticGeometry, faceSplit[0], "StaticVisible");
        visG.transform.SetParent(p);
        UFUtils.LocalReset(visG.transform);
        visG.AddComponent<MeshCollider>();

        GameObject invisG = MakeMeshObject(level.staticGeometry, faceSplit[1], "StaticInvisible");
        invisG.transform.SetParent(p);
        UFUtils.LocalReset(invisG.transform);
        invisG.GetComponent<MeshRenderer>().enabled = false;
        invisG.AddComponent<MeshCollider>();
    }

    private void BuildPlayerInfo() {
        Transform p = MakeParent("PlayerInfo");
        UFPlayerInfo info = p.gameObject.AddComponent<UFPlayerInfo>();
        info.Set(level);
    }

    private void BuildGeoModer() {
        Transform p = MakeParent("GeoModder");
        UFGeoModder gm = p.gameObject.AddComponent<UFGeoModder>();
        Material geoMat = GetMaterial(level.geomodTexture, assetPath);
        gm.Set(level, geoMat);
    }

    private void BuildMovers() {
        Transform p = MakeParent("Movers");
        for(int i = 0; i < level.movingGroups.Length; i++) {
            MovingGroup group = level.movingGroups[i];

            //make new gameobject
            GameObject g = new GameObject("Mover_<" + group.name + ">");
            g.transform.SetParent(p);

            //attach mover script and initialize
            UFMover mov = g.gameObject.AddComponent<UFMover>();
            mov.Set(group);

            //Retrieve geometry contained in this mover
            List<Brush> brushes = new List<Brush>();
            foreach(int id in group.contents) {
                //TODO: improve efficiency by making id lookup table
                foreach(Brush b in level.movingGeometry) {
                    if(b.transform.id == id) {
                        brushes.Add(b);
                        break;
                    }
                }
            }

            //Build the geometry and attach it to the mover
            for(int j = 0; j < brushes.Count; j++) {
                string name = "Brush_" + brushes[j].transform.id.ToString().PadLeft(4, '0');
                Transform brush = (MakeMeshObject(brushes[j].geometry, name)).transform;
                mov.AddAt(brush, brushes[j].transform.posRot);
            }

            //retrieve and assign voice clips
            mov.startClip = GetClip(group.startClip);
            mov.loopClip = GetClip(group.loopClip);
            mov.closeClip = GetClip(group.closeClip);
            mov.stopClip = GetClip(group.stopClip);

            mov.AddAudio();
        }
    }

    private void BuildTriggers() {
        Transform p = MakeParent("Triggers");
        foreach(Trigger trigger in level.triggers) {
            GameObject g = new GameObject("Trigger_" + trigger.transform.id);
            g.transform.SetParent(p);
            UFTrigger t = g.AddComponent<UFTrigger>();
            UFUtils.SetTransform(t.transform, trigger.transform);
            t.Set(trigger);
        }
    }

    /* -----------------------------------------------------------------------------------------------
     * -------------------------------------- HELPER METHODS -----------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    private static string assetPath {  get { return Path.GetDirectoryName(lastRFLPath) + "/" + VPPUnpacker.assetSubFolder + "/"; } }
    private static string[] searchFolders { get { return new string[] {
        assetPath.TrimEnd('/'),
        Path.GetDirectoryName(lastRFLPath),
        VPPUnpacker.GetRFSourceFolder().TrimEnd('/')
    }; } }
    private string rootName { get { return "UF_<" + level.name + ">"; } }

    private Transform root
    {
        get
        {
            if(level == null)
                return null;
            GameObject toReturn = GameObject.Find(rootName);
            if(toReturn != null)
                return toReturn.transform;
            return null;
        }
    }

    private void MakeRoot() {
        GameObject r = new GameObject(rootName);
        UFLevel l = r.AddComponent<UFLevel>();
        l.Set(level);
    }

    private Transform MakeParent(string name) {
        for(int i = 0; i < root.childCount; i++) {
            if(name == root.GetChild(i).name)
                DestroyImmediate(root.GetChild(i).gameObject);
        }
        GameObject parent = new GameObject(name);
        parent.transform.SetParent(root);
        UFUtils.LocalReset(parent.transform);
        return parent.transform;
    }

    private static GameObject MakeMeshObject(Geometry geometry, string name) {
        List<Face> faces = new List<Face>(geometry.faces);
        return MakeMeshObject(geometry, faces, name);
    }

    public static GameObject MakeMeshObject(Geometry geometry, List<Face> faces, string name) {
        //make object
        GameObject g = new GameObject(name);

        //materials
        List<String> usedTextures = new List<string>();
        int[] texMap = new int[geometry.textures.Length];

        foreach(Face face in faces) {
            int texIdx = Mathf.Max(0, face.texture);
            string nextTex = geometry.textures[texIdx];
            if(!usedTextures.Contains(nextTex)) {
                texMap[face.texture] = usedTextures.Count;
                usedTextures.Add(nextTex);
            }
        }

        //mesh
        Mesh mesh = MakeMesh(geometry, faces, texMap, usedTextures.Count);
        mesh.name = name;
        MeshFilter mf = g.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = g.AddComponent<MeshRenderer>();
        mr.materials = GetMaterials(usedTextures, assetPath);

        return g;
    }

    public static Material[] GetMaterials(List<string> textures, string assetPath) {
        Material[] materials = new Material[textures.Count];
        for(int i = 0; i < materials.Length; i++)
            materials[i] = GetMaterial(textures[i], assetPath);
        return materials;
    }

    public static Material GetMaterial(string texture, string assetPath) {
        string textureName = Path.GetFileNameWithoutExtension(texture);
        string materialName = textureName + ".mat";
        string[] results = AssetDatabase.FindAssets(textureName);

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

    public static Material GenerateDefaultMat() {
        return new Material(Shader.Find("Standard"));
    }

    public static AudioClip GetClip(string clip) {
        if(string.IsNullOrEmpty(clip))
            return null;

        string clipName = Path.GetFileNameWithoutExtension(clip);
        string[] results = AssetDatabase.FindAssets(clipName);

        foreach(string result in results) {
            string resultPath = AssetDatabase.GUIDToAssetPath(result);
            string resultName = Path.GetFileName(resultPath);
            if(resultName == clipName)
                return (AudioClip)AssetDatabase.LoadAssetAtPath(resultPath, typeof(AudioClip));
        }

        //audio clip does not exist
        Debug.LogWarning("Could not audio clip: " + clip);
        return null;
    }

    private static Mesh MakeMesh(Geometry geometry, List<Face> faces, int[] texMap, int texCount) {
        Mesh mesh = new Mesh();

        //mesh
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int>[] triangles = new List<int>[texCount];
        for(int i = 0; i < triangles.Length; i++)
            triangles[i] = new List<int>();

        foreach(Face face in faces) {
            //extract required data
            int nboPoints = face.vertices.Length;
            Vector3[] faceVertices = new Vector3[nboPoints];
            Vector2[] faceUVs = new Vector2[nboPoints];
            for(int i = 0; i < nboPoints; i++) {
                faceVertices[i] = geometry.vertices[face.vertices[i].vertexRef];
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

            int texRef = Mathf.Max(0, face.texture);
            texRef = texMap[texRef];

            triangles[texRef].AddRange(faceTriangles);
        }

        //mesh data is ready, insert all of it
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = triangles.Length;

        for(int i = 0; i < triangles.Length; i++)
            mesh.SetTriangles(triangles[i], i);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}

