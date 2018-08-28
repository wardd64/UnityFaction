using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;
using System.Collections.Generic;
using UFLevelStructure;
using UnityEditorInternal;

public class LevelBuilder : EditorWindow {

    private static string lastRFLPath;
    private LevelData level;

    //GUI variables
    private bool showContents, showGeneralContents, showGeometryContents, 
        showObjectContents, showMoverContents, showBuildOptions;

    //Build options
    public int levelLayer, playerLayer, skyLayer;
    public bool convexMovers;

    /// <summary>
    /// Read RFL file and build its contents into the current Unity scene.
    /// </summary>
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

    /// <summary>
    /// Level builder GUI
    /// </summary>
    private void OnGUI() {
        //title
        GUIStyle header = new GUIStyle();
        header.fontSize = 22;
        GUILayout.Label("UNITY FACTION LEVEL BUILDER", header);

        //button for loading level if none is available yet
        if(level == null) {
            if(GUILayout.Button("Load RFL file"))
                BuildLevel();
            return;
        }

        //general info and style
        string fileName = Path.GetFileNameWithoutExtension(lastRFLPath);
        GUILayout.Label("Loaded file: " + fileName, EditorStyles.largeLabel);
        GUIStyle contentFoldout = new GUIStyle("foldout");
        contentFoldout.fontStyle = FontStyle.Bold;
        contentFoldout.fontSize = 14;

        //show contents of the current level
        showContents = EditorGUILayout.Foldout(showContents, "View level contents", contentFoldout);

        if(showContents) {
            showGeneralContents = EditorGUILayout.Foldout(showGeneralContents, "   General content", contentFoldout);
            if(showGeneralContents) {
                level.name = EditorGUILayout.TextField("      Level name", level.name);
                level.author = EditorGUILayout.TextField("      Author name", level.author);
                level.multiplayer = EditorGUILayout.Toggle("      Multiplayer", level.multiplayer);
                level.playerStart.position = EditorGUILayout.Vector3Field("      Player start", level.playerStart.position);
            }

            showGeometryContents = EditorGUILayout.Foldout(showGeometryContents, "   Static geometry content", contentFoldout);
            if(showGeometryContents) {
                GUILayout.Label("      Vertices: " + level.staticGeometry.vertices.Length);
                GUILayout.Label("      Faces: " + level.staticGeometry.faces.Length);
                GUILayout.Label("      Rooms: " + level.staticGeometry.rooms.Length);
                GUILayout.Label("      Brushes: " + level.brushes.Length);
            }

            showObjectContents = EditorGUILayout.Foldout(showObjectContents, "   Object content", contentFoldout);
            if(showObjectContents) {
                GUILayout.Label("      Lights: " + level.lights.Length);
                GUILayout.Label("      Ambient sounds: " + level.ambSounds.Length);
                GUILayout.Label("      Events: " + level.events.Length);
                GUILayout.Label("      Multi spawn points: " + level.spawnPoints.Length);
                GUILayout.Label("      Particle emitters: " + level.particleEmitters.Length);
                GUILayout.Label("      Push regions: " + level.pushRegions.Length);
                GUILayout.Label("      Decals: " + level.decals.Length);
                GUILayout.Label("      Climbing regions : " + level.climbingRegions.Length);
                GUILayout.Label("      Bolt emitters: " + level.boltEmitters.Length);
                GUILayout.Label("      Targets: " + level.targets.Length);
                GUILayout.Label("      Entities: " + level.entities.Length);
                GUILayout.Label("      Items: " + level.items.Length);
                GUILayout.Label("      Clutter: " + level.clutter.Length);
                GUILayout.Label("      Triggers: " + level.triggers.Length);
            }

            showMoverContents = EditorGUILayout.Foldout(showMoverContents, "   Mover content", contentFoldout);
            if(showMoverContents) {
                GUILayout.Label("      Moving brushes: " + level.movingGeometry.Length);
                GUILayout.Label("      Moving groups: " + level.movingGroups.Length);
            }
        }

        //make root object to put the level under
        if(root == null) {
            if(GUILayout.Button("Make root"))
                MakeRoot();
            return;
        }

        //reload the root script with ID references (usefull after recompiling)
        if(GUILayout.Button("Refresh level")) {
            GameObject.DestroyImmediate(root.GetComponent<UFLevel>());
            UFLevel l = root.gameObject.AddComponent<UFLevel>();
            l.Set(level);
            l.Awake();
        }

        //Build all button, do everything at once
        GUIStyle bigButton = new GUIStyle("button");
        bigButton.fontSize = 26;
        if(GUILayout.Button("Build all", bigButton)) {
            BuildStaticGeometry();
            BuildLights();
            BuildPlayerInfo();
            BuildGeoModder();
            BuildMovers();
            BuildClutter();
            BuildItems();
            BuildTriggers();
            BuildForceRegions();
            BuildEvents();
            BuildEmitters();
            BuildAmbSounds();
            BuildDecals();
        }

        //Build one class of objects seperately + options for building
        showBuildOptions = EditorGUILayout.Foldout(showBuildOptions, "Build options", contentFoldout);

        if(showBuildOptions) {
            levelLayer = EditorGUILayout.LayerField("Level layer", levelLayer);
            skyLayer = EditorGUILayout.LayerField("Sky layer", skyLayer);
            if(GUILayout.Button("Build static geometry")) BuildStaticGeometry();
            if(GUILayout.Button("Build lights")) BuildLights();
            playerLayer = EditorGUILayout.LayerField("player layer", playerLayer);
            if(GUILayout.Button("Build player info")) BuildPlayerInfo();
            if(GUILayout.Button("Build geomodder")) BuildGeoModder();
            convexMovers = EditorGUILayout.Toggle("Mke mesh clldrs convex", convexMovers);
            if(GUILayout.Button("Build movers")) BuildMovers();
            if(GUILayout.Button("Build clutter")) BuildClutter();
            if(GUILayout.Button("Build items")) BuildItems();
            if(GUILayout.Button("Build triggers")) BuildTriggers();
            if(GUILayout.Button("Build force regions")) BuildForceRegions();
            if(GUILayout.Button("Build events")) BuildEvents();
            if(GUILayout.Button("Build emitters")) BuildEmitters();
            if(GUILayout.Button("Build ambient sounds")) BuildAmbSounds();
            if(GUILayout.Button("Build decals")) BuildDecals();

            //TODO entities?
        }
    }

    /* -----------------------------------------------------------------------------------------------
     * ---------------------------------- SPECIFIC BUILD METHODS -------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    /// <summary>
    /// Builds all the non-moving geometry in a set of objects:
    /// Standard static geometry
    /// Invisible static geometry (colliders but no renderers)
    /// Destructible brushes (glass)
    /// Liquids; surfaces and volume triggers
    /// Faces with scrolling textures
    /// Sky room; dynamic skybox in the level
    /// </summary>
    private void BuildStaticGeometry() {
        //set up split
        int split = 5;
        List<Face>[] faceSplit = new List<Face>[split];
        for(int i = 0; i < split; i++)
            faceSplit[i] = new List<Face>();

        List<Brush> excepted = GetSpecialBrushes();

        List<int> scrollers = new List<int>();
        foreach(FaceScroll scroll in level.staticGeometry.scrolls)
            scrollers.Add(scroll.faceRef);

        List<Room> liquids = new List<Room>();
        bool hasSkyRoom = false; Room skyRoom = default(Room);
        foreach(Room room in level.staticGeometry.rooms) {
            if(room.hasLiquid)
                liquids.Add(room);

            if(room.isSkyRoom) {
                hasSkyRoom = true;
                skyRoom = room;
            }
        }

        //split faces
        int nboFaces = level.staticGeometry.faces.Length;
        for(int i = 0; i < nboFaces; i++){
            Face face = level.staticGeometry.faces[i];

            bool excludedFace = face.showSky;
            excludedFace |= FaceIsContainedInBrushes(level.staticGeometry, face, excepted);
            if(excludedFace)
                continue;

            int sort;
            if(FaceIsContainedInLiquidSurface(level.staticGeometry, liquids, face))
                sort = 2;
            else if(hasSkyRoom && FaceIsInRoom(level.staticGeometry, skyRoom, face))
                sort = 4;
            else if(scrollers.Contains(face.id))
                sort = 3;
            else {
                int texIdx = Mathf.Max(0, face.texture);
                string nextTex = level.staticGeometry.textures[texIdx];
                bool invis = nextTex.ToLower().Contains("invis");
                sort = invis ? 1 : 0;
            }

            faceSplit[sort].Add(face);
        }

        //make objects
        Transform p = MakeParent("StaticGeometry");

        //static visible geometry; all standard stuff
        GameObject visG = MakeMeshObject(level.staticGeometry, faceSplit[0], "StaticVisible");
        visG.isStatic = true;
        visG.transform.SetParent(p);
        visG.AddComponent<MeshCollider>();
        visG.layer = levelLayer;

        //static invisible; disabled renderers, but still active colliders
        GameObject invisG = MakeMeshObject(level.staticGeometry, faceSplit[1], "StaticInvisible");
        invisG.isStatic = true;
        invisG.transform.SetParent(p);
        invisG.GetComponent<MeshRenderer>().enabled = false;
        invisG.AddComponent<MeshCollider>();
        invisG.layer = levelLayer;

        //destructible geometry: brushes that can be shot and shattered (glass)
        GameObject destrG = new GameObject("Destructible");
        destrG.transform.SetParent(p);
        List<Brush> brushes = GetDestructibleBrushes();
        foreach(Brush b in brushes) {
            string name = "Brush_" + GetIdString(b.transform);
            Transform brush = (MakeMeshObject(b.geometry, name)).transform;
            brush.SetParent(destrG.transform);
            UFUtils.SetTransform(brush, b.transform);
            UFDestructible destr = brush.gameObject.AddComponent<UFDestructible>();
            destr.Set(b);
            brush.gameObject.AddComponent<MeshCollider>();
        }

        bool foundAirPortals = false;

        //portal geometry: sometimes needed sometimes not
        GameObject portalG = new GameObject("PortalGeometry");
        portalG.transform.SetParent(p);
        brushes = GetExceptedBrushes();
        foreach(Brush b in brushes) {
            if(!b.isAir) {
                //solid portal brushes can be safely ignored
                continue;
            }

            /*
             * Air portals lead to faulty geometry: 
             * build them and warn the player about their presence.
             */

            foundAirPortals = true;
            string name = "Brush_" + GetIdString(b.transform);
            Transform brush = (MakeMeshObject(b.geometry, name)).transform;
            brush.SetParent(portalG.transform);
            UFUtils.SetTransform(brush, b.transform);
            brush.gameObject.SetActive(false);
            
        }
        if(foundAirPortals)
            Debug.LogWarning("Portal air brushes may lead to faulty geometry! " +
                "These brushes were built under the StaticGeometry/PortalGeometry GameObject. " +
                "Please return to the RED level editor and replace these brushes with solid plane portals.");

        //liquid surfaces
        GameObject surf = new GameObject("Liquids");
        surf.transform.SetParent(p);
        List<Face>[] liquidFaces = new List<Face>[liquids.Count];
        for(int i = 0; i < liquidFaces.Length; i++)
            liquidFaces[i] = new List<Face>();

        foreach(Face f in faceSplit[2]) {
            int i = GetLiquidRoomOfFace(level.staticGeometry, liquids, f);
            liquidFaces[i].Add(f);
        }

        for(int i = 0; i < liquidFaces.Length; i++){
            List<Face> faces = liquidFaces[i];
            GameObject surface = MakeMeshObject(level.staticGeometry, faces, "Surface_" + i);
            surface.transform.SetParent(surf.transform);
            UFLiquid liquid = surface.AddComponent<UFLiquid>();
            liquid.Set(liquids[i]);
            MeshRenderer mr = surface.GetComponent<MeshRenderer>();
            Room.LiquidProperties liqProp = liquids[i].liquidProperties;
            Vector2 scroll = new Vector2(liqProp.scrollU, liqProp.scrollV);
            mr.material = GetScrollingTexture(liqProp.texture, scroll);
        }

        //static scrolling textures
        GameObject scrol = new GameObject("Scrollers");
        scrol.isStatic = true;
        scrol.transform.SetParent(p);
        for(int i = 0; i < faceSplit[3].Count; i++){
            Face face = faceSplit[3][i];

            int scrollID = -1;
            for(int j = 0; j < scrollers.Count; j++) {
                if(scrollers[j] == face.id)
                    scrollID = j;
            }
            if(scrollID < 0)
                continue;

            FaceScroll scroll = level.staticGeometry.scrolls[scrollID];
            List<Face> scrollFaceInList = new List<Face> { face };
            GameObject mesh = MakeMeshObject(level.staticGeometry, scrollFaceInList, "ScrolFace_" + scrollID);
            mesh.transform.SetParent(scrol.transform);
            MeshRenderer mr = mesh.GetComponent<MeshRenderer>();
            string tex = level.staticGeometry.textures[face.texture];
            mr.material = GetScrollingTexture(tex, scroll.scrollVelocity);
        }

        GameObject sky = MakeMeshObject(level.staticGeometry, faceSplit[4], "SkyRoom");
        sky.isStatic = true;
        sky.transform.SetParent(p);
        sky.layer = skyLayer;
    }

    /// <summary>
    /// Builds standard Unity lights into the scene with 
    /// properties as close as possible to the Redfaction types
    /// </summary>
    private void BuildLights() {
        Transform p = MakeParent("Lights");
        foreach(UFLevelStructure.Light l in level.lights) {
            string name = l.type + "_" + GetIdString(l.transform);
            UnityEngine.Light light = MakeUFObject<UnityEngine.Light>(name, p, l.transform);

            if(l.type == UFLevelStructure.Light.LightType.spotlight)
                light.type = LightType.Spot;
            else
                light.type = LightType.Point;

            if(l.dynamic) {
                light.lightmapBakeType = LightmapBakeType.Realtime;
            }
            else {
                light.lightmapBakeType = LightmapBakeType.Baked;
                light.gameObject.isStatic = true;
            }

            light.color = l.color;
            light.areaSize = new Vector2(l.tubeLength, 0.1f);
            light.enabled = l.enabled;
            light.spotAngle = l.fov;
            light.intensity = l.intensity;
            light.range = l.range;
            light.shadows = l.shadows ? LightShadows.Soft : LightShadows.None;
        }
    }

    /// <summary>
    /// Build specialized player info object that handles parameters such as 
    /// global lighting, fog, skyroom etc.
    /// </summary>
    private void BuildPlayerInfo() {
        Transform p = MakeParent("PlayerInfo");
        UFPlayerInfo info = p.gameObject.AddComponent<UFPlayerInfo>();
        info.Set(level, levelLayer, playerLayer, skyLayer);
    }

    /// <summary>
    /// Build specialized geomodder object that encodes level strength
    /// and allows player to breach static geometry when needed.
    /// </summary>
    private void BuildGeoModder() {
        Transform p = MakeParent("GeoModder");
        UFGeoModder gm = p.gameObject.AddComponent<UFGeoModder>();
        Material geoMat = GetMaterial(level.geomodTexture, assetPath);
        gm.Set(level, geoMat);
    }

    /// <summary>
    /// Build moving groups and their associated moving geometry.
    /// Moving groups can be linked to any number of UnityFaction
    /// objects in the scene and will drag them along their motion.
    /// Moving geometry consist of individual brushes, with simple colliders attached.
    /// Colliders cannot be assigned accurately in general, so extra work may be 
    /// required to get them in proper working condition.
    /// </summary>
    private void BuildMovers() {
        Transform p = MakeParent("Movers");

        //make moving groups
        List<int> ghostMovers = new List<int>();
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

            if(mov.noPlayerCollide)
                ghostMovers.AddRange(mov.links);
        }

        //build moving geometry
        Transform geomHolder = (new GameObject("Moving geometry")).transform;
        geomHolder.SetParent(p);

        movingMeshColliders = 0;
        foreach(Brush b in level.movingGeometry) {
            string name = "Brush_" + GetIdString(b.transform);
            Transform brush = (MakeMeshObject(b.geometry, name)).transform;
            if(!ghostMovers.Contains(b.transform.id))
                GiveBrushCollider(brush);
            brush.SetParent(geomHolder);
            UFUtils.SetTransform(brush, b.transform);
            UFLevel.SetObject(b.transform.id, brush.gameObject);
        }
        if(movingMeshColliders > 0)
            Debug.LogWarning("A number of moving brushes are using mesh colliders: " + movingMeshColliders + 
                ". Consider giving them compound colliders for efficiency.");
    }

    /// <summary>
    /// Builds geometric objects into the scene including 
    /// things like furniture, machines, switches and plants.
    /// </summary>
    private void BuildClutter() {
        Transform p = MakeParent("Clutter");
        foreach(Clutter clutter in level.clutter) {
            string modelName = TableReader.FindClutterModel(clutter.name);
            GameObject prefab = GetPrefab(modelName);
            if(prefab == null)
                continue;
            GameObject g = GameObject.Instantiate(prefab, p);
            g.name = "Clutter_" + GetIdString(clutter.transform) + "_" + clutter.name;
            UFClutter c = g.AddComponent<UFClutter>();
            c.Set(clutter);
            UFLevel.SetObject(clutter.transform.id, g);
            UFUtils.SetTransform(g.transform, clutter.transform);
        }
    }

    /// <summary>
    /// Build items into the scene that players can pick up.
    /// Includes health packs, powerups, weapons and ammo.
    /// </summary>
    private void BuildItems() {
        Transform p = MakeParent("Items");
        foreach(Item item in level.items) {
            string modelName = TableReader.FindItemModel(item.name);
            GameObject prefab = GetPrefab(modelName);
            if(prefab == null)
                continue;
            GameObject g = GameObject.Instantiate(prefab, p);
            g.name = "Item_" + GetIdString(item.transform) + "_" + item.name;
            UFItem i = g.AddComponent<UFItem>();
            i.Set(item);
            UFLevel.SetObject(item.transform.id, g);
            UFUtils.SetTransform(g.transform, item.transform);
        }
    }

    /// <summary>
    /// Build Triggers into the scene that respond to the player's actions
    /// and activate other UnityFaction objects in response (mainly events and movers!)
    /// </summary>
    private void BuildTriggers() {
        Transform p = MakeParent("Triggers");
        foreach(Trigger trigger in level.triggers) {
            string name = "Trigger_" + GetIdString(trigger.transform);
            UFTrigger t = MakeUFObject<UFTrigger>(name, p, trigger.transform);
            t.Set(trigger);
        }
    }

    /// <summary>
    /// Build ladder and push regions that work with UFPlayerMovement
    /// </summary>
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

    /// <summary>
    /// Builds special event objects that have unique effects when activated.
    /// Events are to be activated mainly by triggers and other events.
    /// </summary>
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

    /// <summary>
    /// Build particle and bolt emitters into the scene. 
    /// </summary>
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

    /// <summary>
    /// Build ambient sound objects that constantly emit faint 3D noise.
    /// </summary>
    private void BuildAmbSounds() {
        Transform p = MakeParent("AmbSounds");
        foreach(AmbSound s in level.ambSounds) {
            string name = "Ambient_" + GetIdString(s.transform);
            AudioSource sound = MakeUFObject<AudioSource>(name, p, s.transform);
            sound.clip = GetClip(s.clip);
            sound.volume = s.volume;

            sound.rolloffMode = AudioRolloffMode.Linear;
            sound.minDistance = s.minDist;
            sound.maxDistance = s.minDist + (10f * s.minDist * s.volume / s.roloff);

            //TODO make script so delay can be taken into account

            sound.playOnAwake = true;
            sound.loop = true;
        }
    }

    /// <summary>
    /// Build decals, which are small images that are pasted onto nearby geometry.
    /// UnityFaction implements these as seperate faces hovering slightly over
    /// their intended location.
    /// </summary>
    private void BuildDecals() {
        Transform p = MakeParent("Decals");
        foreach(Decal d in level.decals) {
            string name = "Decall_" + GetIdString(d.cbTransform.transform);
            MeshFilter mf = MakeUFObject<MeshFilter>(name, p, d.cbTransform.transform);
            mf.sharedMesh = UFUtils.MakeQuad(d.cbTransform.extents);
            MeshRenderer mr = mf.gameObject.AddComponent<MeshRenderer>();
            mr.material = GetMaterial(d.texture, assetPath);
            SnapToGeometry(mf.transform, d.cbTransform.extents.z);
        }
    }

    /* -----------------------------------------------------------------------------------------------
     * -------------------------------------- HELPER METHODS -----------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    /// <summary>
    /// Handles set of repeated operations needed for nearly all objects created by the LevelBuilder.
    /// This includes: creating a gameObject with the given name, setting its parent,
    /// setting its location, linking it to UFLevel id's and adding its functional component.
    /// </summary>
    private T MakeUFObject<T>(string name, Transform parent, UFTransform transform) where T : Component {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent);
        UFUtils.SetTransform(g.transform, transform);
        UFLevel.SetObject(transform.id, g);
        if(typeof(T) == typeof(Transform))
            return g.GetComponent<T>();
        return g.AddComponent<T>();
    }

    /// <summary>
    /// Returns asset folder in which to put/find additional assets associated with the last readed RFL.
    /// </summary>
    private static string assetPath {  get { return Path.GetDirectoryName(lastRFLPath) + "/" + VPPUnpacker.assetFolder + "/"; } }

    /// <summary>
    /// Returns name to be used for the root object of the current level.
    /// </summary>
    private string rootName { get { return "UF_<" + level.name + ">"; } }

    /// <summary>
    /// Returns or generates root object for the currently loaded level.
    /// </summary>
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

    /// <summary>
    /// Make root object for the current level. The root object contains a UFLevel script
    /// that handles ID references within its children.
    /// </summary>
    private void MakeRoot() {
        GameObject r = new GameObject(rootName);
        UFLevel l = r.AddComponent<UFLevel>();
        l.Set(level);
        l.Awake();
    }

    /// <summary>
    /// Makes a parent object that is parented to the level root.
    /// This parent object should hold one type of child objects,
    /// such as the static geometry or events.
    /// </summary>
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

    /// <summary>
    /// Returns consistent string format for the given id.
    /// String is padded to the left with '0' so to make sure that 
    /// consecutive ids are always sorted alphabetically.
    /// </summary>
    private static string GetIdString(UFTransform t) {
        int nboIDs = UFLevel.singleton.idDictionary.Count;
        int idLength = Mathf.CeilToInt(Mathf.Log10(nboIDs + 1));
        return t.id.ToString().PadLeft(idLength, '0');
    }

    /// <summary>
    /// Makes a single object containing mesh data (and renderer) of the given geometry.
    /// </summary>
    private static GameObject MakeMeshObject(Geometry geometry, string name) {
        List<Face> faces = new List<Face>(geometry.faces);
        return MakeMeshObject(geometry, faces, name);
    }

    /// <summary>
    /// Makes a single object containing mesh data (and renderer) of the given faces.
    /// This only works correctly when all given faces are part of the given geometry.
    /// </summary>
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

    /// <summary>
    /// Return or generate standard materials for each of the given texture names.
    /// Appropriate files are searched for in the given assetPath.
    /// </summary>
    public static Material[] GetMaterials(string[] textures, string assetPath) {
        Material[] materials = new Material[textures.Length];
        for(int i = 0; i < materials.Length; i++)
            materials[i] = GetMaterial(textures[i], assetPath);
        return materials;
    }

    /// <summary>
    /// Return or generate standard materials for each of the given texture names.
    /// Appropriate files are searched for in the given assetPath.
    /// </summary>
    public static Material[] GetMaterials(List<string> textures, string assetPath) {
        return GetMaterials(textures.ToArray(), assetPath);
    }

    /// <summary>
    /// Return or generate a standard material for the given texture name.
    /// Appropriate files are searched for in the given assetPath.
    /// </summary>
    public static Material GetMaterial(string texture, string assetPath) {
        return GetMaterial(texture, assetPath, "Standard");
    }

    /// <summary>
    /// Return or generate a material with a custom shader for the given texture name.
    /// Appropriate files are searched for in the given assetPath.
    /// </summary>
    private static Material GetMaterial(string texture, string assetPath, string shader) {
        string textureName = Path.GetFileNameWithoutExtension(texture);
        string materialName = textureName + ".mat";
        string[] results = AssetDatabase.FindAssets(textureName);

        string texPath = null;
        foreach(string result in results) {
            string resultPath = AssetDatabase.GUIDToAssetPath(result);
            string resultName = Path.GetFileName(resultPath);
            
            if(string.Equals(resultName, materialName, StringComparison.OrdinalIgnoreCase))
                return (Material)AssetDatabase.LoadAssetAtPath(resultPath, typeof(Material));
            if(string.Equals(resultName, texture, StringComparison.OrdinalIgnoreCase))
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

    /// <summary>
    /// Find and return texture file with the given name
    /// </summary>
    private static Texture2D GetTexture(string texture) {
        string textureName = Path.GetFileNameWithoutExtension(texture);
        string[] results = AssetDatabase.FindAssets(textureName);

        foreach(string result in results) {
            string resultPath = AssetDatabase.GUIDToAssetPath(result);
            string resultName = Path.GetFileName(resultPath);

            if(string.Equals(resultName, texture, StringComparison.OrdinalIgnoreCase))
                return (Texture2D)AssetDatabase.LoadAssetAtPath(resultPath, typeof(Texture2D));
        }

        return null;
    }

    /// <summary>
    /// Returns name of the shader to be used with particles with the given properties.
    /// </summary>
    private static string GetParticleShader(bool fade, bool glow) {
        if(glow)
            return "Particles/Additive";
        else if(fade)
            return "Particles/Alpha Blended";
        else
            return "Particles/Standard Unlit";
    }

    /// <summary>
    /// Finds or generates a special scrolling material 
    /// with the given texture and scrollspeed.
    /// Note that each scrollspeed will lead to a new material being generated.
    /// </summary>
    private static Material GetScrollingTexture(string texture, Vector2 scroll) {
        string scrollStr = UFUtils.GetVecStr(scroll);
        string matName = Path.GetFileNameWithoutExtension(texture) + "_scroll_" + scrollStr;
        string fullMatName = matName + ".mat";

        string[] results = AssetDatabase.FindAssets(matName);

        foreach(string result in results) {
            string resultPath = AssetDatabase.GUIDToAssetPath(result);
            string resultName = Path.GetFileName(resultPath);

            if(string.Equals(resultName, fullMatName, StringComparison.OrdinalIgnoreCase))
                return (Material)AssetDatabase.LoadAssetAtPath(resultPath, typeof(Material));
        }

        Material mat = new Material(Shader.Find("UnityFaction/UVScroll"));
        mat.mainTexture = GetTexture(texture);
        mat.SetFloat("_ScrollXSpeed", scroll.x);
        mat.SetFloat("_ScrollYSpeed", scroll.y);

        mat.name = matName;

        AssetDatabase.CreateAsset(mat, assetPath + fullMatName);

        return mat;
    }

    /// <summary>
    /// DOES NOT WORK AS OF YET
    /// Returns material for the given texture which automatically has the appropriate
    /// render settings (e.g. being transparant, cutout, fade or opaque).
    /// </summary>
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
            if(b.isPortal && !IsMover(b)) {
                toReturn.Add(b);
            }
        }

        return toReturn;
    }

    /// <summary>
    /// Returns true if the given brush is contained in any of this level's moving geometry.
    /// </summary>
    private bool IsMover(Brush brush) {
        foreach(Brush mb in level.movingGeometry) {
            if(mb.transform.id == brush.transform.id)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the given face is embedded in one of the given brushes.
    /// </summary>
    private static bool FaceIsContainedInBrushes(Geometry geometry, Face face, List<Brush> brushes) {
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

                    if(foundMatch)
                        break;
                }
            }

            if(!foundMatch)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Small delta value used to detect corresponding pieces of geometry.
    /// </summary>
    private const float GEOM_DELTA = 1e-4f;

    /// <summary>
    /// parameter to keep count of moving brushes that have mesh colliders attached (and may be optimized)
    /// </summary>
    private static int movingMeshColliders;

    /// <summary>
    /// True if the given face lies lies (horizontally) in one of the liquid surfaces of the level.
    /// This usually means that the face is part of the liquid surface.
    /// </summary>
    /// <param name="geometry">(Static) geometry that contains the given face.</param>
    /// <param name="liquids">List of liquid rooms in which to search for the face.</param>
    private static bool FaceIsContainedInLiquidSurface(Geometry geometry, List<Room> liquids, Face face) {
        return GetLiquidRoomOfFace(geometry, liquids, face) >= 0;
    }

    /// <summary>
    /// Returns first index of the room in the given list of liquid rooms that 
    /// contains the given face in its liquid surface.
    /// </summary>
    /// <param name="geometry">(Static) geometry that contains the given face.</param>
    /// <param name="liquids">List of liquid rooms in which to search for the face.</param>
    private static int GetLiquidRoomOfFace(Geometry geometry, List<Room> liquids, Face face) {
        for(int i = 0; i < liquids.Count; i++){
            Room room = liquids[i];
            bool onSurface = true;
            float y = room.aabb.min.y + room.liquidProperties.depth;

            foreach(FaceVertex v in face.vertices) {
                Vector3 vert = geometry.vertices[v.vertexRef];
                onSurface &= Mathf.Abs(y - vert.y) < GEOM_DELTA;
                onSurface &= vert.x > room.aabb.min.x - GEOM_DELTA;
                onSurface &= vert.z > room.aabb.min.z - GEOM_DELTA;
                onSurface &= vert.x < room.aabb.max.x + GEOM_DELTA;
                onSurface &= vert.z < room.aabb.max.z + GEOM_DELTA;
                if(!onSurface)
                    break;
            }

            if(onSurface)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Returns true if the given face lies completely in the given room.
    /// To be used to detect and seperate skyroom geometry.
    /// </summary>
    /// <param name="geometry">(Static) geometry that contains the given face.</param>
    private static bool FaceIsInRoom(Geometry geometry, Room room, Face face) {
        foreach(FaceVertex v in face.vertices) {
            Vector3 vert = geometry.vertices[v.vertexRef];
            for(int i = 0; i < 3; i++) {
                bool vertInRoom = vert[i] > room.aabb.min[i] - GEOM_DELTA;
                vertInRoom &= vert[i] < room.aabb.max[i] + GEOM_DELTA;
                if(!vertInRoom)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Attaches an appropriate (moving) collider to the given brush.
    /// This brush is expected to have a MeshFilter component.
    /// </summary>
    private void GiveBrushCollider(Transform brush) {
        //collapse vertices close to eachother
        Vector3[] originalVerts = brush.GetComponent<MeshFilter>().sharedMesh.vertices;
        List<Vector3> verts = new List<Vector3>();

        foreach(Vector3 v in originalVerts) {
            bool overlap = false;
            foreach(Vector3 ov in verts) {
                if((v - ov).sqrMagnitude < GEOM_DELTA) {
                    overlap = true;
                    break;
                }
            }
            if(!overlap)
                verts.Add(v);
        }

        //check if mesh is at least a plane
        if(verts.Count < 3)
            return;

        //check if mesh is a simple, axis aligned box
        int nboVerts = verts.Count;
        bool validBox = nboVerts == 4 || nboVerts == 8;
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
        movingMeshColliders++;

        if(convexMovers) {
            //check if points are nearly co-planar
            bool coplanar = verts.Count == 3;
            if(!coplanar) {
                coplanar = true;
                Plane plane = new Plane(verts[0], verts[1], verts[2]);
                for(int i = 3; i < verts.Count; i++) {
                    float d = Mathf.Abs(plane.GetDistanceToPoint(verts[i]));
                    if(d > GEOM_DELTA) {
                        coplanar = false;
                        break;
                    }
                }
            }

            //if not make mesh convex
            if(!coplanar) {
                mc.inflateMesh = true;
                mc.convex = true;
            }
        }
    }

    /// <summary>
    /// Find and returns prefab with the given name.
    /// To be used to find clutter and item objects that were made by the VPPUnpacker.
    /// </summary>
    public static GameObject GetPrefab(string name) {
        string prefabName = name + ".prefab";
        string[] results = AssetDatabase.FindAssets(name);
        foreach(string result in results) {
            string resultPath = AssetDatabase.GUIDToAssetPath(result);
            string resultName = Path.GetFileName(resultPath);
            if(string.Equals(resultName, prefabName, StringComparison.OrdinalIgnoreCase))
                return (GameObject)AssetDatabase.LoadAssetAtPath(resultPath, typeof(GameObject));
        }

        Debug.LogWarning("Could not find prefab for " + name);
        return null;
    }

    /// <summary>
    /// Returns true if the given string likely encodes a valid audioclip,
    /// since it is not empty and has the appropriate extension.
    /// This is usefull to detect audio events ahead of time.
    /// </summary>
    public static bool IsValidAudioClipName(string clip) {
        if(string.IsNullOrEmpty(clip))
            return false;
        Path.GetExtension(clip);
        string ext = Path.GetExtension(clip).TrimStart('.').ToLower();
        return new List<string> { "wav", "mp3", "ogg" }.Contains(ext);
    }

    /// <summary>
    /// Finds and returns an AudioClip with the given name.
    /// Spits out warnings if the clip does not exist or is unreadable.
    /// </summary>
    public static AudioClip GetClip(string clip) {
        if(string.IsNullOrEmpty(clip))
            return null;

        string clipName = Path.GetFileNameWithoutExtension(clip);
        string[] results = AssetDatabase.FindAssets(clipName);

        foreach(string result in results) {
            string resultPath = AssetDatabase.GUIDToAssetPath(result);
            string resultName = Path.GetFileName(resultPath);
            if(string.Equals(resultName, clip, StringComparison.OrdinalIgnoreCase)) {
                AudioClip toReturn = (AudioClip)AssetDatabase.LoadAssetAtPath(resultPath, typeof(AudioClip));
                if(toReturn == null)
                    Debug.LogWarning("Failed to load AudioClip " + resultName +
                        ". This problem might be fixed by importing and reexporting the clip in an audio editor.");
                return toReturn;
            }
        }

        //audio clip does not exist
        Debug.LogWarning("Could not audio clip: " + clip);
        return null;
    }

    /// <summary>
    /// Helper method for making a unity mesh out of a RedFaction geometry.
    /// </summary>
    /// <param name="geometry">Geometry that contains the given faces.</param>
    /// <param name="faces">Faces to be built</param>
    /// <param name="texMap">Maps RedFaction texture index to the index of textures actually contained within the list of faces. 
    /// This is needed since the given list of faces may not contain all textures in the given geometry.</param>
    /// <param name="texCount">Number of different textures contained in the given list of faces.</param>
    /// <returns></returns>
    private static Mesh MakeMesh(Geometry geometry, List<Face> faces, int[] texMap, int texCount) {
        Mesh mesh = new Mesh();

        //mesh
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int>[] triangles = new List<int>[texCount];
        for(int i = 0; i < triangles.Length; i++)
            triangles[i] = new List<int>();

        foreach(Face face in faces) {
            if(face.showSky)
                continue;

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

    /// <summary>
    /// Distance that a decall will hover above the geometry it is intended to snap on to.
    /// </summary>
    const float DECALL_SNAP_DIST = 1e-2f;

    /// <summary>
    /// Takes the transform and attempts to snap it to a nearby collider which is within the 
    /// range of the directions forward or backward of the current transform position.
    /// Very useful for snapping decals to their intended geometry.
    /// </summary>
    /// <param name="range">Maximum forward/backward distance in which the transform is allowed to move</param>
    private void SnapToGeometry(Transform t, float range) {
        Vector3 dir = t.forward;
        Vector3 start = t.position - 0.5f * range * dir;
        Ray ray = new Ray(start, dir);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, range)) {
            Vector3 newPos = start + (hit.distance - DECALL_SNAP_DIST) * dir;
            t.position = newPos;
            t.rotation = Quaternion.LookRotation(-hit.normal);
        }
        else
            Debug.LogWarning("Could not snap decall " + t.name + " to any geometry.");
    }
}

