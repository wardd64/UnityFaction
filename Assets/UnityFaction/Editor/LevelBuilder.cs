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

    [MenuItem("UnityFaction/Build Level (RFL)")]
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
            AssetDatabase.CreateFolder(Path.GetDirectoryName(lastRFLPath), VPPUnpacker.assetFolder);
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
            GUILayout.Label("   Brushes: " + level.brushes.Length);
        }

        showObjectContents = EditorGUILayout.Foldout(showObjectContents, "Object content", contentFoldout);
        if(showObjectContents) {
            GUILayout.Label("   Lights: " + level.lights.Length);
            GUILayout.Label("   Ambient sounds: " + level.ambSounds.Length);
            GUILayout.Label("   Events: " + level.events.Length);
            GUILayout.Label("   Multi spawn points: " + level.spawnPoints.Length);
            GUILayout.Label("   Particle emitters: " + level.particleEmitters.Length);
            GUILayout.Label("   Push regions: " + level.pushRegions.Length);
            GUILayout.Label("   Decals: " + level.decals.Length);
            GUILayout.Label("   Climbing regions : " + level.climbingRegions.Length);
            GUILayout.Label("   Bolt emitters: " + level.boltEmitters.Length);
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

        if(GUILayout.Button("Refresh level")) {
            GameObject.DestroyImmediate(root.GetComponent<UFLevel>());
            UFLevel l = root.gameObject.AddComponent<UFLevel>();
            l.Set(level);
            l.Awake();
        }

        if(GUILayout.Button("Build static geometry"))
            BuildStaticGeometry();

        if(GUILayout.Button("Build player info"))
            BuildPlayerInfo();

        if(GUILayout.Button("Build geomodder"))
            BuildGeoModder();

        if(GUILayout.Button("Build movers"))
            BuildMovers();

        if(GUILayout.Button("Build clutter"))
            BuildClutter();

        if(GUILayout.Button("Build triggers"))
            BuildTriggers();

        if(GUILayout.Button("Build force regions"))
            BuildForceRegions();

        if(GUILayout.Button("Build events"))
            BuildEvents();

        if(GUILayout.Button("Build emitters"))
            BuildEmitters();
    }

    /* -----------------------------------------------------------------------------------------------
     * ---------------------------------- SPECIFIC BUILD METHODS -------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    private void BuildStaticGeometry() {
        //set up split
        int split = 2;
        List<Face>[] faceSplit = new List<Face>[split];
        for(int i = 0; i < split; i++)
            faceSplit[i] = new List<Face>();

        List<Brush> excepted = GetSpecialBrushes();

        //split faces
        int nboFace = level.staticGeometry.faces.Length;
        foreach(Face face in level.staticGeometry.faces) { 
            int texIdx = Mathf.Max(0, face.texture);
            string nextTex = level.staticGeometry.textures[texIdx];
            bool invis = nextTex.ToLower().Contains("invis");
            int sort = invis ? 1 : 0;

            if(!face.showSky && !FaceIsContainedIn(level.staticGeometry, face, excepted))
                faceSplit[sort].Add(face);
        }
        
        //TODO option to include culled faces in seperate object

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

        GameObject destrG = new GameObject("Destructible");
        destrG.transform.SetParent(p);
        List<Brush> brushes = GetDestructibleBrushes();
        foreach(Brush b in brushes) {
            string name = "Brush_" + GetIdString(b.transform);
            Transform brush = (MakeMeshObject(b.geometry, name)).transform;
            brush.SetParent(destrG.transform);
            UFUtils.SetTransform(brush, b.transform);
        }
    }

    private void BuildPlayerInfo() {
        Transform p = MakeParent("PlayerInfo");
        UFPlayerInfo info = p.gameObject.AddComponent<UFPlayerInfo>();
        info.Set(level);
    }

    private void BuildGeoModder() {
        Transform p = MakeParent("GeoModder");
        UFGeoModder gm = p.gameObject.AddComponent<UFGeoModder>();
        Material geoMat = GetMaterial(level.geomodTexture, assetPath);
        gm.Set(level, geoMat);
    }

    private void BuildMovers() {
        Transform p = MakeParent("Movers");

        //build moving geometry
        Transform geomHolder = (new GameObject("Moving geometry")).transform;
        geomHolder.SetParent(p);

        movingMeshColliders = 0;
        foreach(Brush b in level.movingGeometry) {
            string name = "Brush_" + GetIdString(b.transform);
            Transform brush = (MakeMeshObject(b.geometry, name)).transform;
            GiveBrushCollider(brush);
            brush.SetParent(geomHolder);
            UFUtils.SetTransform(brush, b.transform);
            UFLevel.SetObject(b.transform.id, brush.gameObject);
        }
        if(movingMeshColliders > 0)
            Debug.LogWarning("A number of moving brushes are using mesh colliders: " + movingMeshColliders + 
                ". Consider giving them compound colliders for efficiency.");

        Transform groupHolder = (new GameObject("Moving groups")).transform;
        groupHolder.SetParent(p);

        for(int i = 0; i < level.movingGroups.Length; i++) {
            MovingGroup group = level.movingGroups[i];

            //make new gameobject
            GameObject g = new GameObject(group.name);
            g.transform.SetParent(groupHolder);

            //attach mover script and initialize
            UFMover mov = g.gameObject.AddComponent<UFMover>();
            mov.Set(group);

            //retrieve and assign voice clips
            mov.startClip = GetClip(group.startClip);
            mov.loopClip = GetClip(group.loopClip);
            mov.closeClip = GetClip(group.closeClip);
            mov.stopClip = GetClip(group.stopClip);

            mov.AddAudio();
        }
    }

    private void BuildClutter() {
        Transform p = MakeParent("Clutter");
        foreach(Clutter clutter in level.clutter) {
            GameObject prefab = GetPrefab(clutter.name);
            if(prefab == null)
                continue;
            GameObject g = GameObject.Instantiate(prefab, p);
            g.name = "Clutter_" + GetIdString(clutter.transform) + "_" + clutter.name;
            g.AddComponent<UFClutter>();
            UFLevel.SetObject(clutter.transform.id, g);
            UFUtils.SetTransform(g.transform, clutter.transform);
        }
    }

    private void BuildTriggers() {
        Transform p = MakeParent("Triggers");
        foreach(Trigger trigger in level.triggers) {
            string name = "Trigger_" + GetIdString(trigger.transform);
            UFTrigger t = MakeUFObject<UFTrigger>(name, p, trigger.transform);
            t.Set(trigger);
        }
    }

    private void BuildForceRegions() {
        Transform p = MakeParent("ForceRegions");
        foreach(PushRegion region in level.pushRegions) {
            string name = "PushRegion_" + GetIdString(region.transform);
            UFForceRegion r = MakeUFObject<UFForceRegion>(name, p, region.transform);
            r.Set(region);
        }
        foreach(ClimbingRegion region in level.climbingRegions) {
            string name = "ClimbRegion_" + GetIdString(region.cbTransform.transform);
            UFForceRegion r = MakeUFObject<UFForceRegion>(name, p, region.cbTransform.transform);
            r.Set(region);
        }
    }

    private void BuildEvents() {
        Transform p = MakeParent("Events");
        foreach(UFLevelStructure.Event e in level.events) {
            string name = "Event_" + GetIdString(e.transform) + "_" + e.name;
            UFEvent ufe = MakeUFObject<UFEvent>(name, p, e.transform);
            ufe.Set(e);
            if(IsValidAudioClipName(e.string1))
                ufe.SetAudio(GetClip(e.string1));
        }
    }

    private void BuildEmitters() {
        Transform p = MakeParent("Emitters");

        Transform ptclParent = (new GameObject("ParticleEmitters")).transform;
        ptclParent.SetParent(p);
        foreach(UFLevelStructure.ParticleEmitter e in level.particleEmitters) {
            string name = "ParticleEmitter_" + GetIdString(e.transform);
            UFParticleEmitter emit = MakeUFObject<UFParticleEmitter>(name, ptclParent, e.transform);
            emit.Set(e);
            Material particleMat = GetMaterial(e.texture, assetPath, GetParticleShader(e.fade, e.glow));
            emit.SetMaterial(particleMat);
        }

        Transform trgtParent = (new GameObject("BoltTargets")).transform;
        trgtParent.SetParent(p);
        foreach(UFTransform t in level.targets)
            MakeUFObject<Transform>("target_" + GetIdString(t), trgtParent, t);

        Transform boltParent = (new GameObject("BoltEmitters")).transform;
        boltParent.SetParent(p);
        foreach(UFLevelStructure.BoltEmitter e in level.boltEmitters) {
            string name = "BoltEmitter_" + GetIdString(e.transform);
            UFBoltEmitter emit = MakeUFObject<UFBoltEmitter>(name, boltParent, e.transform);
            emit.Set(e);
            Material particleMat = GetMaterial(e.texture, assetPath, GetParticleShader(e.fade, e.glow));
            emit.SetMaterial(particleMat);
        }
    }

    /* -----------------------------------------------------------------------------------------------
     * -------------------------------------- HELPER METHODS -----------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    private T MakeUFObject<T>(string name, Transform parent, UFTransform transform) where T : Component {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent);
        UFUtils.SetTransform(g.transform, transform);
        UFLevel.SetObject(transform.id, g);
        if(typeof(T) == typeof(Transform))
            return g.GetComponent<T>();
        return g.AddComponent<T>();
    }

    private static string assetPath {  get { return Path.GetDirectoryName(lastRFLPath) + "/" + VPPUnpacker.assetFolder + "/"; } }
    private static string[] searchFolders { get { return new string[] {
        assetPath.TrimEnd('/'),
        Path.GetDirectoryName(lastRFLPath),
        VPPUnpacker.GetRFSourcePath().TrimEnd('/')
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
        l.Awake();
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

    private static string GetIdString(UFTransform t) {
        int nboIDs = UFLevel.singleton.idDictionary.Count;
        int idLength = Mathf.CeilToInt(Mathf.Log10(nboIDs + 1));
        return t.id.ToString().PadLeft(idLength, '0');
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

        bool fullyInvis = true;
        foreach(String tex in usedTextures)
            fullyInvis &= tex.ToLower().Contains("invis");

        mr.enabled = !fullyInvis;

        return g;
    }

    public static Material[] GetMaterials(string[] textures, string assetPath) {
        Material[] materials = new Material[textures.Length];
        for(int i = 0; i < materials.Length; i++)
            materials[i] = GetMaterial(textures[i], assetPath);
        return materials;
    }

    public static Material[] GetMaterials(List<string> textures, string assetPath) {
        return GetMaterials(textures.ToArray(), assetPath);
    }

    public static Material GetMaterial(string texture, string assetPath) {
        return GetMaterial(texture, assetPath, "Standard");
    }

    private static Material GetMaterial(string texture, string assetPath, string shader) {
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
            Texture2D tex = (Texture2D)AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture2D));

            Material mat = new Material(Shader.Find(shader));
            
            mat.mainTexture = tex;
            AssetDatabase.CreateAsset(mat, assetPath + materialName);
            
            return mat;
        }

        //neither material nor texture exists
        Debug.LogWarning("Could not find texture: " + texture);
        return new Material(Shader.Find(shader));
    }

    private static string GetParticleShader(bool fade, bool glow) {
        if(glow)
            return "Particles/Additive";
        else if(fade)
            return "Particles/Alpha Blended";
        else
            return "Particles/Standard Unlit";
    }

    private static Material GetStandardMaterialFor(Texture2D tex) {
        Material mat = new Material(Shader.Find("Standard"));
        float minAlpha = 1f, avgAlpha = 0f;
        for(int x = 0; x < tex.width; x++) {
            for(int y = 0; y < tex.height; y++) {
                float value = tex.GetPixel(x, y).a;
                if(value < minAlpha)
                    minAlpha = value;
                avgAlpha += value;
            }
        }
        avgAlpha /= (tex.width * tex.height);
        if(avgAlpha == 0f)
            mat.SetFloat("_Mode", 1f); //completely transparant
        else if(minAlpha == 0f)
            mat.SetFloat("_Mode", 2f); //fade to transparant
        else if(avgAlpha < 1f)
            mat.SetFloat("_Mode", 3f); //semi transparant
        else
            mat.SetFloat("_Mode", 0f); //opaque

        return mat;
    }

    /// <summary>
    /// Brushes that do not need to appear in the standard static geometry
    /// </summary>
    private List<Brush> GetSpecialBrushes() {
        List<Brush> toReturn = new List<Brush>();
        toReturn.AddRange(GetDestructibleBrushes());
        toReturn.AddRange(GetExceptedBrushes());
        return toReturn;
    }

    /// <summary>
    /// Solid, detailed brushes with life value > 0; these are destructible glass panes
    /// </summary>
    private List<Brush> GetDestructibleBrushes() {
        List<Brush> toReturn = new List<Brush>();

        foreach(Brush b in level.brushes) {
            if(!b.isPortal && !b.isAir && b.isDetail && b.life > 0) {
                if(!IsMover(b))
                    toReturn.Add(b);
            }
        }

        return toReturn;
    }

    /// <summary>
    /// Solid, portal brushes; these divide up rooms only, no renderer or collider needed
    /// </summary>
    private List<Brush> GetExceptedBrushes() {
        List<Brush> toReturn = new List<Brush>();

        foreach(Brush b in level.brushes) {
            if(!b.isAir && b.isPortal) {
                if(!IsMover(b))
                    toReturn.Add(b);
            }
        }

        return toReturn;
    }

    private bool IsMover(Brush brush) {
        foreach(Brush mb in level.movingGeometry) {
            if(mb.transform.id == brush.transform.id)
                return true;
        }
        return false;
    }

    private static bool FaceIsContainedIn(Geometry geometry, Face face, List<Brush> brushes) {
        foreach(Brush b in brushes) {
            if(FaceIsContainedIn(geometry, face, b))
                return true;
        }
        return false;
    }

    /// <summary>
    /// True if every vertex of the given face lies in one of the faces of the given brush
    /// </summary>
    private static bool FaceIsContainedIn(Geometry geometry, Face face, Brush brush) {
        foreach(FaceVertex v in face.vertices) {
            Vector3 vertex = geometry.vertices[v.vertexRef];
            bool foundMatch = false;

            foreach(Face f in brush.geometry.faces) {
                int nboPoints = f.vertices.Length;
                Vector3[] faceVertices = new Vector3[nboPoints];
                Vector3 bPos = brush.transform.posRot.position;
                Quaternion bRot = brush.transform.posRot.rotation;
                Vector3[] bGeom = brush.geometry.vertices;
                for(int i = 0; i < nboPoints; i++) 
                    faceVertices[i] = bPos + bRot * bGeom[f.vertices[i].vertexRef];

                int[] faceTriangles = Triangulator.BasePoint(faceVertices);
                int triangles = faceTriangles.Length / 3;
                for(int i = 0; i < triangles; i++) { 
                    Vector3 v1 = faceVertices[faceTriangles[i * 3]];
                    Vector3 v2 = faceVertices[faceTriangles[(i * 3) + 1]];
                    Vector3 v3 = faceVertices[faceTriangles[(i * 3) + 2]];
                    foundMatch |= Triangulator.VertexInTriangle(vertex, v1, v2, v3, GEOM_DELTA);
                }
            }

            if(!foundMatch)
                return false;
        }

        return true;
    }

    private const float GEOM_DELTA = 1e-4f;
    private static int movingMeshColliders;

    private static void GiveBrushCollider(Transform brush) {
        Vector3[] verts = brush.GetComponent<MeshFilter>().sharedMesh.vertices;

        //check if mesh is a simple, axis aligned box
        int nboVerts = verts.Length;
        bool validBox = nboVerts == 8 || nboVerts == 24;
        if(validBox) {
            for(int coord = 0; coord < 3; coord++) {
                List<float> values = new List<float>();
                for(int i = 0; i < nboVerts; i++)
                    values.Add(verts[i][coord]);
                values.Sort();

                for(int i = 1; i < nboVerts - 1; i++) {
                    float limit = (i < (nboVerts / 2)) ? values[0] : values[nboVerts - 1];
                    float distance = Mathf.Abs(values[i] - limit);
                    if(distance > GEOM_DELTA)
                        validBox = false;
                }
            }
        }

        //if so, use a box collider
        if(validBox) {
            brush.gameObject.AddComponent<BoxCollider>();
            return;
        }

        //if not, use a mesh collider in stead (and warn the user)
        MeshCollider mc = brush.gameObject.AddComponent<MeshCollider>();
        mc.inflateMesh = true;
        mc.convex = true;
        movingMeshColliders++;
    }

    public static GameObject GetPrefab(string name) {
        string prefabName = name + ".prefab";
        string[] results = AssetDatabase.FindAssets(name);
        foreach(string result in results) {
            string resultPath = AssetDatabase.GUIDToAssetPath(result);
            string resultName = Path.GetFileName(resultPath);
            if(resultName == prefabName)
                return (GameObject)AssetDatabase.LoadAssetAtPath(resultPath, typeof(GameObject));
        }

        Debug.LogWarning("Could not find prefab for " + name);
        return null;
    }

    public static bool IsValidAudioClipName(string clip) {
        if(string.IsNullOrEmpty(clip))
            return false;
        Path.GetExtension(clip);
        string ext = Path.GetExtension(clip).TrimStart('.').ToLower();
        return new List<string> { "wav", "mp3", "ogg" }.Contains(ext);
    }

    public static AudioClip GetClip(string clip) {
        if(string.IsNullOrEmpty(clip))
            return null;

        string clipName = Path.GetFileNameWithoutExtension(clip);
        string[] results = AssetDatabase.FindAssets(clipName);

        foreach(string result in results) {
            
            string resultPath = AssetDatabase.GUIDToAssetPath(result);
            string resultName = Path.GetFileName(resultPath);
            if(resultName == clip)
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

