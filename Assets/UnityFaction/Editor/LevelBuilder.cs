using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;
using System.Collections.Generic;

public class LevelBuilder : EditorWindow {

    private static string lastRFLPath;
    private static UFLevel level;

    /* an RFL file is a binary file that holds data of a redfaction level.
     * The file starts with standard header that holds general info about the level 
     * and the data structure.
     * The rest of the file is split into a series of sections that hold the various level data itself.
     * Each section starts with a specific 4 byte header (detailed in the enum RFLSection)
     * The header is followed by an integer that shows the byte size of the section, 
     * this can be used to skip ahead to the next section. After that, the section data itself 
     * starts. Very often the first next value is an integer showing the number of elements.
     */

    //RFL properties
    const uint RFL_SIGNATURE = 0xD4BADA55;
    const uint RFL_LAST_KNOWN_VERSION = 0x000000C8;

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

        lastRFLPath = UFUtils.GetRelativeUnityPath(rflPath);
        byte[] bytes = File.ReadAllBytes(rflPath);
        level = new UFLevel();

        builder.ReadRFL(bytes);
        builder.Show();
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
        GUILayout.Label("Found static geometry: ", EditorStyles.largeLabel);
        GUILayout.Label("Vertices: " + level.staticGeometry.vertices.Length);
        GUILayout.Label("Faces: " + level.staticGeometry.faces.Length);
        GUILayout.Label("");
        GUILayout.Label("Found objects: ", EditorStyles.largeLabel);
        GUILayout.Label("Lights: " + level.lights.Length);
        GUILayout.Label("Ambient sounds: " + level.ambSounds.Length);
    }

    //----------------------------------------------- RFL reading --------------------------------------------

    //various info
    private bool signatureCheck;

    //file structure parameters
    private int pointer;
    private int playerStartOffset, levelInfoOffset, sectionsCount;

    private void ReadRFL(Byte[] bytes) {

        //read header
        ReadHeader(bytes);

        Debug.Log("Level info at " + UFUtils.GetHex(levelInfoOffset) + 
            ", player start at " + UFUtils.GetHex(playerStartOffset));

        //loop over all the sections
        pointer = 32 + level.name.Length;
        for(int i = 0; i < sectionsCount; i++) {
            RFLSection nextSection = (RFLSection)BitConverter.ToInt32(bytes, pointer);
            Debug.Log("At " + UFUtils.GetHex(pointer) + " looking at section " + nextSection);

            switch(nextSection) {

            case RFLSection.TGA: SkipSection(bytes); break;
            case RFLSection.VCM: SkipSection(bytes); break;
            case RFLSection.MVF: SkipSection(bytes); break;
            case RFLSection.V3D: SkipSection(bytes); break;
            case RFLSection.VFX: SkipSection(bytes); break;

            case RFLSection.LevelProperties: ReadLevelProperties(bytes); break;

            case RFLSection.LightMaps: SkipSection(bytes); break;

            case RFLSection.StaticGeometry: ReadStaticGeometry(bytes); break;

            case RFLSection.Unkown1: SkipSection(bytes); break;

            case RFLSection.LevelInfo: ReadLevelInfo(bytes); break;

            case RFLSection.GeoRegions: ReadGeoRegions(bytes); break;

            case RFLSection.AlternateLights: ReadLights(bytes, true); break;
            case RFLSection.Lights: ReadLights(bytes); break;

            case RFLSection.CutsceneCameras: SkipSection(bytes); break;
            case RFLSection.CutscenePathNodes: SkipSection(bytes); break;

            case RFLSection.AmbientSounds: ReadAmbientSounds(bytes); break;

            case RFLSection.Events: ReadEvents(bytes); break;

            case RFLSection.MPRespawns: ReadSpawnPoints(bytes); break;

            case RFLSection.Particlemitters: ReadParticleEmitters(bytes); break;

            case RFLSection.GasRegions: SkipSection(bytes); break;

            case RFLSection.Decals: ReadDecalls(bytes); break;

            case RFLSection.PushRegions: SkipSection(bytes); break;

            case RFLSection.RoomEffects: SkipSection(bytes); break;

            case RFLSection.EAXEffects: SkipSection(bytes); break;

            case RFLSection.ClimbingRegions: ReadClimbingRegions(bytes); break;

            case RFLSection.BoltEmitters: ReadBoltEmitters(bytes); break;

            case RFLSection.Targets: ReadTargets(bytes); break;

            case RFLSection.MovingGeometry: ReadMovingGeometry(bytes); break;

            case RFLSection.MovingGroups: ReadMovingGroups(bytes); break;

            case RFLSection.PlayerStart: ReadPlayerStart(bytes); break;

            case RFLSection.Unkown2: SkipSection(bytes); break;

            case RFLSection.NavPoints: SkipSection(bytes); break;

            case RFLSection.Entities: ReadEntities(bytes); break;

            case RFLSection.Items: ReadItems(bytes); break;

            case RFLSection.Clutter: ReadClutter(bytes); break;

            case RFLSection.Triggers: ReadTriggers(bytes); break;

            case RFLSection.Brushes: ReadBrushes(bytes); break;

            case RFLSection.Groups: SkipSection(bytes); break;

            default: Debug.LogError("Encountered unknown section at " + 
                UFUtils.GetHex(pointer) + ": " + nextSection);
            Debug.Log("Remaining sections: " + (sectionsCount - i - 1));
            return;
            }
        }

        long end = BitConverter.ToInt64(bytes, pointer);
        pointer += 8;
        if(end == 0 && (bytes.Length + 1) == pointer)
            Debug.LogError("Failed to read RFL file properly. Was it corrupted?");
        else {
            //log success
            int fileSize = bytes.Length / 1024;
            Debug.Log("Parsed RFL file successfully! (" + fileSize + "kb)");
        }
        
    }

    private void ReadHeader(Byte[] bytes) {

        uint signature = BitConverter.ToUInt32(bytes, 0);
        uint version = BitConverter.ToUInt32(bytes, 4);
        uint timeStamp = BitConverter.ToUInt32(bytes, 8);
        playerStartOffset = BitConverter.ToInt32(bytes, 12);
        levelInfoOffset = BitConverter.ToInt32(bytes, 16);
        sectionsCount = BitConverter.ToInt32(bytes, 20);
        // Bits 24-28 have unknown purpose...

        bool signatureCheck = signature == RFL_SIGNATURE;
        signatureCheck &= version <= RFL_LAST_KNOWN_VERSION;

        level.name = UFUtils.ReadNullTerminatedString(bytes, 30);

        //immediately extract author name from level info
        int authorOffset = levelInfoOffset + level.name.Length + 16;
        level.author = UFUtils.ReadNullTerminatedString(bytes, authorOffset);
    }

    private void ReadLevelProperties(Byte[] bytes) {
        pointer += 8;

        string defaultTexture = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
        defaultTexture.Remove(defaultTexture.Length - 1);
        level.geomodTexture = defaultTexture;

        level.hardness = BitConverter.ToInt32(bytes, pointer);
        level.ambientColor = UFUtils.GetRGBAColor(bytes, pointer + 4);
        level.fogColor = UFUtils.GetRGBAColor(bytes, pointer + 9);
        level.nearPlane = BitConverter.ToSingle(bytes, pointer + 13);
        level.farPlane = BitConverter.ToSingle(bytes, pointer + 17);

        pointer += 21;

    }




    /*
    
    /// <summary>
    /// The light maps section is a straight forward list of images.
    /// Each light map is headed by an id, width and height value
    /// followed by a list of 24-bit RGB values.
    /// </summary>
    private void ReadLightMaps(Byte[] bytes) {
        pointer += 8;
        int nbLightMaps = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        
        for(int i = 0; i < nbLightMaps; i++) {
            int width = BitConverter.ToInt32(bytes, pointer);
            int height = BitConverter.ToInt32(bytes, pointer + 4);

            //size of the light map, plus width and height header
            pointer += 8 + (width * height * 3); 
        }

    }

    */

    private void ReadStaticGeometry(Byte[] bytes) {
        pointer += 18;

        level.staticGeometry = ReadGeometry(bytes);
    }

    private void ReadLevelInfo(Byte[] bytes) {
        pointer += 12;

        //level name, author and date
        for(int i = 0; i < 3; i++)
            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);

        pointer += 1;
        level.multiplayer = BitConverter.ToBoolean(bytes, pointer);
        pointer += 221;
    }

    private void ReadGeoRegions(byte[] bytes) {
        pointer += 8;

        int nboGeoRegions = BitConverter.ToInt32(bytes, pointer);
        pointer += 4 + (nboGeoRegions * 68);
        //TODO actually read info (starts with UID)
    }

    private void ReadLights(byte[] bytes, bool alternate = false) {
        pointer += 8;

        int nboLights = BitConverter.ToInt32(bytes, pointer);
        level.lights = new UFLevel.UFLight[nboLights];
        for(int i = 0; i < nboLights; i++) {
            UFLevel.UFLight nextLight;
            pointer += 4; //UID

            if(alternate)
                pointer += 4;

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //"Light"
            nextLight.transform = UFUtils.GetTransform(bytes, pointer);
            pointer += 48;
            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); // script file name
            //first 5 bytes: light type plus flags??
            nextLight.color = UFUtils.GetRGBAColor(bytes, pointer + 5);
            /*
            range
            fov
            fovDropOff
            intensityAtMaxRange
            ?? null
            tubeLightSize
            intensity
            ?? 1f
            ?? 0
            ?? 0
            ?? 1f
            ?? 0
            */
            pointer += 57;
            level.lights[i] = nextLight;
        }
    }

    private void ReadAmbientSounds(byte[] bytes) {
        pointer += 8;
        int nboAmbSounds = BitConverter.ToInt32(bytes, pointer);
        level.ambSounds = new UFLevel.UFAmbSound[nboAmbSounds];
        pointer += 4;
        for(int i = 0; i < nboAmbSounds; i++) {
            UFLevel.UFAmbSound nextSound;

            pointer += 4; //UID
            nextSound.position = UFUtils.Getvector3(bytes, pointer);
            pointer += 13; //position + editor relevant flags
            nextSound.clip = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            nextSound.minDist = BitConverter.ToSingle(bytes, pointer);
            nextSound.volume = BitConverter.ToSingle(bytes, pointer + 4);
            nextSound.roloff = BitConverter.ToSingle(bytes, pointer + 8);
            nextSound.startDelay = BitConverter.ToInt32(bytes, pointer + 12);
            pointer += 16;

            level.ambSounds[i] = nextSound;
        }
    }

    private void ReadEvents(byte[] bytes) {
        pointer += 8;
        int nboEvents = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        for(int i = 0; i < nboEvents; i++) {

            int uid = BitConverter.ToInt32(bytes, pointer);
            pointer += 4;

            string name = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //event name
            RFEvent eventType = (RFEvent)Enum.Parse(typeof(RFEvent), name);

            Vector3 position = UFUtils.Getvector3(bytes, pointer);
            pointer += 12;
            Quaternion rotation = Quaternion.identity;

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //object name

            float delay = BitConverter.ToSingle(bytes, pointer + 1);
            pointer += 5;

            //read event data (value meaning depend on event type, not all values are used)
            bool bool1 = BitConverter.ToBoolean(bytes, pointer);
            bool bool2 = BitConverter.ToBoolean(bytes, pointer + 1);
            int int1 = BitConverter.ToInt32(bytes, pointer + 2);
            int int2 = BitConverter.ToInt32(bytes, pointer + 6);
            float float1 = BitConverter.ToSingle(bytes, pointer + 10);
            float float2 = BitConverter.ToSingle(bytes, pointer + 14);
            pointer += 18;

            string string1 = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            string string2 = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);

            int linkCount = BitConverter.ToInt32(bytes, pointer);
            pointer += 4;
            int[] links = new int[linkCount];
            for(int j = 0; j < linkCount; j++) {
                links[j] = BitConverter.ToInt32(bytes, pointer);
                pointer += 4;
            }

            if(EventHasRotation(eventType)) {
                rotation = UFUtils.GetRotation(bytes, pointer);
                pointer += 36;
            }

            Color color = UFUtils.GetRGBAColor(bytes, pointer);
            pointer += 4;

        }
    }

    //TODO move this to seperate file
    private enum RFEvent {
        Attack, Bolt_state, Continuous_Damage, Cyclic_Timer, Drop_Point_Marker, Explode, Follow_Player,
        Follow_Waypoints, Give_item_To_Player, Goal_Create, Goal_Check, Goal_Set, Goto, Goto_Player, Heal,
        Invert, Load_Level, Look_At, Make_Invulnerable, Make_Fly, Make_Walk, Message, Music_Start, Music_Stop, 
        Particle_State, Play_Animation, Play_Sound, Slay_Object, Remove_Object, Set_AI_Mode, Set_Light_State, 
        Set_Liquid_Depth, Set_Friendliness, Shake_Player, Shoot_At, Shoot_Once, Armor, Spawn_Object, Swap_Textures,
        Switch, Switch_Model, Teleport, When_Dead, Set_Gravity, Alarm, Alarm_Siren, Go_Undercover, Delay,
        Monitor_State, UnHide, Push_Region_State, When_Hit, Headlamp_State, Item_Pickup_State, Cutscene, 
        Strip_Player_Weapons, Fog_State, Detach, Skybox_State, Force_Monitor_Update, Black_Out_Player, 
        Turn_Off_Physics, Teleport_Player, Holster_Weapon, Holster_Player_Weapon, Modify_Rotating_Mover, 
        Clear_Endgame_If_Killed, Win_PS2_Demo, Enable_Navpoint, Play_Vclip, Endgame, Mover_Pause, Countdown_begin,
        Countdown_End, When_Countdown_Over, Activate_Capek_Shield, When_Enter_Vehicle, When_Try_Exit_Vehicle,
        Fire_Weapon_No_Anim, Never_Leave_Vehicle, Drop_Weapon, Ignite_Entity, When_Cutscene_Over, 
        When_Countdown_Reach, Display_Fullscreen_Image, Defuse_Nuke, When_Life_Reaches, When_Armor_Reaches,
        Reverse_Mover
    }

    private bool EventHasRotation(RFEvent e){
        return e == RFEvent.Alarm ||
            e == RFEvent.Teleport ||
            e == RFEvent.Teleport_Player ||
            e == RFEvent.Play_Vclip;
    }

    private void ReadSpawnPoints(byte[] bytes) {
        int nboSpawnPoints = BitConverter.ToInt32(bytes, pointer + 8);
        level.spawnPoints = new UFLevel.UFSpawnPoint[nboSpawnPoints];
        pointer += 12;

        for(int i = 0; i < nboSpawnPoints; i++) {
            UFLevel.UFSpawnPoint nextPoint;
            pointer += 4; //UID

            nextPoint.transform = UFUtils.GetTransform(bytes, pointer);
            pointer += 48;
            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);

            nextPoint.team = BitConverter.ToInt32(bytes, pointer + 1);
            nextPoint.redTeam = BitConverter.ToBoolean(bytes, pointer + 5);
            nextPoint.blueTeam = BitConverter.ToBoolean(bytes, pointer + 6);
            nextPoint.bot = BitConverter.ToBoolean(bytes, pointer + 7);
            pointer += 8;

            level.spawnPoints[i] = nextPoint;
        }
    }

    private void ReadParticleEmitters(byte[] bytes) {
        int nboEmitters = BitConverter.ToInt32(bytes, pointer + 8);
        level.emitters = new UFLevel.UFParticleEmitter[nboEmitters];
        pointer += 12;

        for(int i = 0; i < nboEmitters; i++) {
            UFLevel.UFParticleEmitter nextEmitter;

            pointer += 4; //UID

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //"Particle Emitter"
            nextEmitter.transform = UFUtils.GetTransform(bytes, pointer);
            pointer += 48;
            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //custom script name
            pointer += 1;
            nextEmitter.type = (UFLevel.UFParticleEmitter.EmitterType)BitConverter.ToInt32(bytes, pointer);

            nextEmitter.SphereRadius = BitConverter.ToSingle(bytes, pointer + 4);
            nextEmitter.planeWidth = BitConverter.ToSingle(bytes, pointer + 8);
            nextEmitter.planeDepth = BitConverter.ToSingle(bytes, pointer + 12);
            pointer += 16;

            nextEmitter.texture = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);

            nextEmitter.spawnDelay = BitConverter.ToSingle(bytes, pointer);
            nextEmitter.spawnRandomize = BitConverter.ToSingle(bytes, pointer + 4);
            nextEmitter.velocity = BitConverter.ToSingle(bytes, pointer + 8);
            nextEmitter.velocityRandomize = BitConverter.ToSingle(bytes, pointer + 12);
            nextEmitter.acceleration = BitConverter.ToSingle(bytes, pointer + 16);
            nextEmitter.decay = BitConverter.ToSingle(bytes, pointer + 20);
            nextEmitter.decayRandomize = BitConverter.ToSingle(bytes, pointer + 24);
            nextEmitter.radius = BitConverter.ToSingle(bytes, pointer + 28);
            nextEmitter.radiusRandomize = BitConverter.ToSingle(bytes, pointer + 32);
            nextEmitter.growthRate = BitConverter.ToSingle(bytes, pointer + 36);
            nextEmitter.gravityMultiplier = BitConverter.ToSingle(bytes, pointer + 40);
            nextEmitter.randomDirection = BitConverter.ToSingle(bytes, pointer + 44);
            nextEmitter.particleColor = UFUtils.GetRGBAColor(bytes, pointer + 48);
            nextEmitter.fadeColor = UFUtils.GetRGBAColor(bytes, pointer + 52);
            pointer += 56;

            nextEmitter.emitterInitiallyOn = UFUtils.GetFlag(bytes, pointer, 4);
            nextEmitter.directionDependentVelocity = UFUtils.GetFlag(bytes, pointer, 3);
            nextEmitter.forceSpawnEveryFrame = UFUtils.GetFlag(bytes, pointer, 2);

            nextEmitter.explodeOnImpact = UFUtils.GetFlag(bytes, pointer + 4, 7);
            nextEmitter.collidWithWorld = UFUtils.GetFlag(bytes, pointer + 4, 4);
            nextEmitter.gravity = UFUtils.GetFlag(bytes, pointer + 4, 3);
            nextEmitter.fade = UFUtils.GetFlag(bytes, pointer + 4, 2);
            nextEmitter.glow = UFUtils.GetFlag(bytes, pointer + 4, 1);

            nextEmitter.playCollisionSounds = UFUtils.GetFlag(bytes, pointer + 5, 4);
            nextEmitter.dieOnImpact = UFUtils.GetFlag(bytes, pointer + 5, 3);
            nextEmitter.collidWithLiquids = UFUtils.GetFlag(bytes, pointer + 5, 2);
            nextEmitter.randomOrient = UFUtils.GetFlag(bytes, pointer + 5, 1);
            nextEmitter.loopAnim = UFUtils.GetFlag(bytes, pointer + 5, 0);

            nextEmitter.bounciness = UFUtils.GetNibble(bytes, pointer + 6, true);
            nextEmitter.stickieness = UFUtils.GetNibble(bytes, pointer + 6, false);
            nextEmitter.swirliness = UFUtils.GetNibble(bytes, pointer + 7, true);
            nextEmitter.pushEffect = UFUtils.GetNibble(bytes, pointer + 7, false);

            pointer += 9; 

            nextEmitter.timeOn = BitConverter.ToSingle(bytes, pointer);
            nextEmitter.timeOnRandomize = BitConverter.ToSingle(bytes, pointer);
            nextEmitter.timeOff = BitConverter.ToSingle(bytes, pointer);
            nextEmitter.timeOffRandomize = BitConverter.ToSingle(bytes, pointer);
            nextEmitter.activeDistance = BitConverter.ToSingle(bytes, pointer);
            pointer += 20;

            level.emitters[i] = nextEmitter;
        }
    }

    private void ReadDecalls(byte[] bytes) {
        int nboDecalls = BitConverter.ToInt32(bytes, pointer + 8);
        level.decals = new UFLevel.UFDecal[nboDecalls];
        pointer += 12;

        for(int i = 0; i < nboDecalls; i++) {
            UFLevel.UFDecal nextDecal;
            pointer += 4; //UID

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //"Decall"
            UFLevel.PosRot transform = UFUtils.GetTransform(bytes, pointer);
            pointer += 48;

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //custom script name
            Vector3 extents = UFUtils.Getvector3(bytes, pointer + 1);
            nextDecal.cbTransform = new UFLevel.CenteredBox(transform, extents);
            pointer += 13;

            nextDecal.texture = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);

            nextDecal.alpha = BitConverter.ToInt32(bytes, pointer);
            nextDecal.selfIlluminated = BitConverter.ToBoolean(bytes, pointer + 4);
            nextDecal.tiling = (UFLevel.UFDecal.TilingMode) bytes[pointer + 5];
            nextDecal.scale = BitConverter.ToSingle(bytes, pointer + 9);
            pointer += 13;

            level.decals[i] = nextDecal;
        }
    }

    /*
    
    private void ReadRoomEffects(byte[] bytes) {
        pointer += 8;
        int nboObjects = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        for(int i = 0; i < nboObjects; i++) {
            pointer += 15; //UID + random crap
            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            pointer += 48; //transform
            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            pointer += 1; //null
        }
    }

    private void ReadEAXEffects(byte[] bytes) {
        pointer += 8;
        int nboObjects = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        for(int i = 0; i < nboObjects; i++) {
            pointer += 6; //UID + type enum
            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            pointer += 48; //transform
            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            pointer += 1; //null
        }
    }

    */

    private void ReadClimbingRegions(byte[] bytes) {
        int nboClimbingRegions = BitConverter.ToInt32(bytes, pointer + 8);
        level.climbingRegions = new UFLevel.UFClimbingRegion[nboClimbingRegions];
        pointer += 12;

        for(int i = 0; i < nboClimbingRegions; i++) {
            UFLevel.UFClimbingRegion nextRegion;
            pointer += 4; //UID

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //"ClimbingRegion"
            UFLevel.PosRot transform = UFUtils.GetTransform(bytes, pointer);
            pointer += 48;

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //custom script name

            nextRegion.type = (UFLevel.UFClimbingRegion.ClimbingType)BitConverter.ToInt32(bytes, pointer + 1);
            Vector3 extents = UFUtils.Getvector3(bytes, pointer + 5);
            nextRegion.cbTransform = new UFLevel.CenteredBox(transform, extents);
            pointer += 17;

            level.climbingRegions[i] = nextRegion;
        }
    }

    private void ReadBoltEmitters(byte[] bytes) {

        int nboBoltEmiters = BitConverter.ToInt32(bytes, pointer + 8);
        level.boltEmiters = new UFLevel.UFBoltEmiter[nboBoltEmiters];
        pointer += 12;

        for(int i = 0; i < nboBoltEmiters; i++) {
            UFLevel.UFBoltEmiter nextEmiter;
            pointer += 4; //UID

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //"Bolt Emiter"
            nextEmiter.transform = UFUtils.GetTransform(bytes, pointer);
            pointer += 48;

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //custom script name
            pointer += 1;

            nextEmiter.targetID = BitConverter.ToInt32(bytes, pointer);
            nextEmiter.srcCtrlDist = BitConverter.ToSingle(bytes, pointer + 4);
            nextEmiter.trgCtrlDist = BitConverter.ToSingle(bytes, pointer + 8);
            nextEmiter.thickness = BitConverter.ToSingle(bytes, pointer + 12);
            nextEmiter.jitter = BitConverter.ToSingle(bytes, pointer + 16);
            nextEmiter.nboSegments = BitConverter.ToInt32(bytes, pointer + 20);
            nextEmiter.spawnDelay = BitConverter.ToSingle(bytes, pointer + 24);
            nextEmiter.spawnDelayRandomize = BitConverter.ToSingle(bytes, pointer + 28);
            nextEmiter.decay = BitConverter.ToSingle(bytes, pointer + 32);
            nextEmiter.decayRandomize = BitConverter.ToSingle(bytes, pointer + 36);
            nextEmiter.color = UFUtils.GetRGBAColor(bytes, pointer + 40); //includes alpha
            pointer += 44;

            nextEmiter.texture = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);

            nextEmiter.fade = UFUtils.GetFlag(bytes, pointer, 1);
            nextEmiter.glow = UFUtils.GetFlag(bytes, pointer, 2);
            nextEmiter.srcDirLock = UFUtils.GetFlag(bytes, pointer, 3);
            nextEmiter.trgDirLock = UFUtils.GetFlag(bytes, pointer, 4);
            nextEmiter.initOn = BitConverter.ToBoolean(bytes, pointer + 4);
            pointer += 5;

            level.boltEmiters[i] = nextEmiter;
        }

    }

    public void ReadTargets(byte[] bytes) {
        int nboTargets = BitConverter.ToInt32(bytes, pointer + 8);
        level.targets = new UFLevel.UFTarget[nboTargets];
        pointer += 12;

        for(int i = 0; i < nboTargets; i++) {
            UFLevel.UFTarget nextTarget;
            nextTarget.id = BitConverter.ToInt32(bytes, pointer);
            pointer += 4; //UID

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //"Bolt Emiter"
            nextTarget.transform = UFUtils.GetTransform(bytes, pointer);
            pointer += 48;

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //custom script name
            pointer += 1;

            level.targets[i] = nextTarget;
        }
    }

    public void ReadMovingGeometry(byte[] bytes) {
        int nboMovingBrushes = BitConverter.ToInt32(bytes, pointer + 8);
        level.movingGeometry = new UFLevel.MovingBrush[nboMovingBrushes];
        pointer += 12;

        for(int i = 0; i < nboMovingBrushes; i++) {
            UFLevel.MovingBrush nextBrush;

            nextBrush.id = BitConverter.ToInt32(bytes, pointer);
            pointer += 4;

            nextBrush.transform = UFUtils.GetTransform(bytes, pointer);
            pointer += 48;

            pointer += 10; // all null ???
            nextBrush.geometry = ReadGeometry(bytes);

            nextBrush.isPortal = UFUtils.GetFlag(bytes, pointer, 0);
            nextBrush.isDetail = UFUtils.GetFlag(bytes, pointer, 2);
            nextBrush.emitsSteam = UFUtils.GetFlag(bytes, pointer, 4);
            pointer += 4;

            nextBrush.life = BitConverter.ToInt32(bytes, pointer);
            pointer += 8;
        }
    }

    public void ReadMovingGroups(byte[] bytes) {
        int nboMovingGroups = BitConverter.ToInt32(bytes, pointer + 8);
        level.movingGroups = new UFLevel.MovingGroup[nboMovingGroups];
        pointer += 12;

        for(int i = 0; i < nboMovingGroups; i++) {
            UFLevel.MovingGroup nextGroup;
            Debug.Log("Moving group @" + UFUtils.GetHex(pointer));

            nextGroup.name = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            pointer += 2; // null + 1 ???

            int nboKeyFrames = BitConverter.ToInt32(bytes, pointer);
            pointer += 4;

            for(int j = 0; j < nboKeyFrames; j++) {
                UFLevel.MovingGroup.KeyFrame key;

                Debug.Log(UFUtils.GetHex(pointer));
                key.id = BitConverter.ToInt32(bytes, pointer);
                pointer += 4;

                key.transform = UFUtils.GetTransform(bytes, pointer);
                pointer += 48;
                
                UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //custom script name
                pointer += 1;

                key.pauseTime = BitConverter.ToSingle(bytes, pointer);
                key.departTravelTime = BitConverter.ToSingle(bytes, pointer + 4);
                key.returnTravelTime = BitConverter.ToSingle(bytes, pointer + 8);
                key.accelTime = BitConverter.ToSingle(bytes, pointer + 12);
                key.decelTime = BitConverter.ToSingle(bytes, pointer + 16);
                key.triggerID = BitConverter.ToInt32(bytes, pointer + 20);
                key.containID1 = BitConverter.ToInt32(bytes, pointer + 24);
                key.containID2 = BitConverter.ToInt32(bytes, pointer + 28);
                key.rotationAmount = BitConverter.ToSingle(bytes, pointer + 32); //degrees
                pointer += 36;
            }

            Debug.Log("Unkown count @" + UFUtils.GetHex(pointer));
            int unkownCount1 = BitConverter.ToInt32(bytes, pointer);
            pointer += 4 + (unkownCount1 * 52); //clearly id, pos, rot; but why?

            nextGroup.isDoor = BitConverter.ToBoolean(bytes, pointer);
            nextGroup.rotateInPlace = BitConverter.ToBoolean(bytes, pointer + 1);
            nextGroup.startsBackwards = BitConverter.ToBoolean(bytes, pointer + 2);
            nextGroup.useTravTimeAsSpd = BitConverter.ToBoolean(bytes, pointer + 3);
            nextGroup.forceOrient = BitConverter.ToBoolean(bytes, pointer + 4);
            nextGroup.noPlayerCollide = BitConverter.ToBoolean(bytes, pointer + 5);
            pointer += 6;

            nextGroup.type = (UFLevel.MovingGroup.MovementType)BitConverter.ToInt32(bytes, pointer);
            pointer += 8; //null ???

            Debug.Log("Clips @" + UFUtils.GetHex(pointer));
            nextGroup.startClip = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            nextGroup.startVol = BitConverter.ToSingle(bytes, pointer);
            pointer += 4;

            nextGroup.loopClip = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            nextGroup.loopVol = BitConverter.ToSingle(bytes, pointer);
            pointer += 4;

            nextGroup.stopClip = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            nextGroup.stopVol = BitConverter.ToSingle(bytes, pointer);
            pointer += 4;

            nextGroup.closeClip = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            nextGroup.closeVol = BitConverter.ToSingle(bytes, pointer);
            pointer += 4;

            int unkownCount2 = BitConverter.ToInt32(bytes, pointer);
            pointer += 4 + (unkownCount2 * 4); //seems to be ID, but with what purpose?

            nextGroup.contents = ReadIntList(bytes);
        }
    }

    private void ReadPlayerStart(byte[] bytes) {
        pointer += 8;
        level.playerStart = UFUtils.GetTransform(bytes, pointer);
        pointer += 48;
    }

    private void ReadEntities(byte[] bytes) {
        //TODO
        SkipSection(bytes);

        /*
        int nboEntities = BitConverter.ToInt32(bytes, pointer + 8);
        level.entities = new UFLevel.Entity[nboEntities];
        pointer += 12;

        for(int i = 0; i < nboEntities; i++) {
            UFLevel.Entity entity;

            entity.type = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            entity.transform = UFUtils.GetTransform(bytes, pointer);
            pointer += 48;

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //custom script name

    uint8_t unknown;
    uint32_t cooperation;
    uint32_t friendliness;
    uint32_t team_id;
    rfl_string waypoint_list;
    rfl_string waypoint_method;
    char unknown2;
    uint8_t boarded; // 1 or 0
    uint8_t ready_to_fire_state; // 1 or 0
    uint8_t only_attack_player; // 1 or 0
    uint8_t weapon_is_holstered; // 1 or 0
    uint8_t deaf; // 1 or 0
    uint32_t sweep_min_angle;
    uint32_t sweep_max_angle;
    uint8_t ignore_terrain_when_firing; // 1 or 0
    char unknown3;
    uint8_t start_crouched; // 1 or 0
    float life;
    float armor;
    uint32_t fov;
    rfl_string default_primary_weapon;
    rfl_string default_secondary_weapon;
    rfl_string item_drop;
    rfl_string state_anim;
    rfl_string corpse_pose;
    rfl_string skin;
    rfl_string death_anim;
    uint8_t ai_mode;
    uint8_t ai_attack_style;
    char unknown4[4];
    uint32_t turret_uid;
    uint32_t alert_camera_uid;
    uint32_t alarm_event_uid;
    uint8_t run; // 1 or 0
    uint8_t start_hidden; // 1 or 0
    uint8_t wear_helmet; // 1 or 0
    uint8_t end_game_if_killed; // 1 or 0
    uint8_t cower_from_weapon; // 1 or 0
    uint8_t question_unarmed_player; // 1 or 0
    uint8_t dont_hum; // 1 or 0
    uint8_t no_shadow; // 1 or 0
    uint8_t always_simulate; // 1 or 0
    uint8_t perfect_aim; // 1 or 0
    uint8_t permanent_corpse; // 1 or 0
    uint8_t never_fly; // 1 or 0
    uint8_t never_leave; // 1 or 0
    uint8_t no_persona_messages; // 1 or 0
    uint8_t fade_corpse_immediately; // 1 or 0
    uint8_t never_collide_with_player; // 1 or 0
    uint8_t use_custom_attack_range; // 1 or 0
    float custom_attack_range; //if use_custom_attack_range==1
    rfl_string left_hand_holding;
    rfl_string right_hand_holding;




        }


        */

    }

    private void ReadItems(byte[] bytes) {
        int nboItems = BitConverter.ToInt32(bytes, pointer + 8);
        level.items = new UFLevel.UFItem[nboItems];
        pointer += 12;

        for(int i = 0; i < nboItems; i++) {
            UFLevel.UFItem item;

            item.id = BitConverter.ToInt32(bytes, pointer);
            pointer += 4;

            item.name = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            //name = name.Replace('.', '_').Replace(' ', '_').Insert(0, "_");
            //item.type = (UFLevel.UFItem.ItemType)Enum.Parse(typeof(UFLevel.UFItem.ItemType), name);
            
            item.transform = UFUtils.GetTransform(bytes, pointer);
            pointer += 48;

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //again item name
            pointer += 1; // ???

            item.count = BitConverter.ToInt32(bytes, pointer);
            item.respawnTime = BitConverter.ToInt32(bytes, pointer + 4);
            item.team = BitConverter.ToInt32(bytes, pointer + 8);
            pointer += 12;
        }
    }

    private void ReadClutter(byte[] bytes) {
        int nboClutter = BitConverter.ToInt32(bytes, pointer + 8);
        level.clutter = new UFLevel.UFClutter[nboClutter];
        pointer += 12;

        for(int i = 0; i < nboClutter; i++) {
            UFLevel.UFClutter clutter;

            clutter.id = BitConverter.ToInt32(bytes, pointer);
            pointer += 4;

            clutter.name = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);
            //name = name.Replace('.', '_').Replace(' ', '_').Insert(0, "_");
            //item.type = (UFLevel.UFItem.ItemType)Enum.Parse(typeof(UFLevel.UFItem.ItemType), name);

            clutter.transform = UFUtils.GetTransform(bytes, pointer);
            pointer += 48;

            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer); //again clutter name
            pointer += 7; // all null ???

            clutter.links = ReadIntList(bytes);
        }
    }

    private void ReadTriggers(byte[] bytes) {
        //TODO
        SkipSection(bytes);
    }

    private void ReadBrushes(byte[] bytes) {
        //TODO
        SkipSection(bytes);
    }

    //--------------------------------------------- RFL reading helper methods ------------------------------------------------

    private UFLevel.Geometry ReadGeometry(byte[] bytes) {
        UFLevel.Geometry geometry;

        int nboTextures = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        geometry.textures = new string[nboTextures];
        for(int i = 0; i < nboTextures; i++)
            geometry.textures[i] = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);

        int nboScrolls = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        if(nboScrolls > 0)
            Debug.LogError("Cannot yet ready files with scrolling textures");

        int nboRooms = BitConverter.ToInt32(bytes, pointer);
        geometry.rooms = new UFLevel.UFRoom[nboRooms];
        pointer += 4;
        for(int i = 0; i < nboRooms; i++) {
            UFLevel.UFRoom nextRoom;

            pointer += 4;

            Vector3 aabb1 = UFUtils.Getvector3(bytes, pointer);
            Vector3 aabb2 = UFUtils.Getvector3(bytes, pointer + 12);
            nextRoom.aabb = new UFLevel.AxisAlignedBoundingBox(aabb1, aabb2);
            pointer += 24;

            nextRoom.isSkyRoom = BitConverter.ToBoolean(bytes, pointer);
            nextRoom.isCold = BitConverter.ToBoolean(bytes, pointer + 1);
            nextRoom.isOutside = BitConverter.ToBoolean(bytes, pointer + 2);
            nextRoom.isAirlock = BitConverter.ToBoolean(bytes, pointer + 3);
            nextRoom.hasLiquid = BitConverter.ToBoolean(bytes, pointer + 4);
            nextRoom.hasAmbientLight = BitConverter.ToBoolean(bytes, pointer + 5);
            nextRoom.isSubRoom = BitConverter.ToBoolean(bytes, pointer + 6);
            pointer += 8;

            nextRoom.life = BitConverter.ToSingle(bytes, pointer);
            pointer += 4;

            nextRoom.eaxEffect = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);

            if(nextRoom.hasLiquid) {
                UFLevel.UFRoom.LiquidProperties liquid;

                liquid.depth = BitConverter.ToSingle(bytes, pointer);
                liquid.color = UFUtils.GetRGBAColor(bytes, pointer + 4);
                pointer += 8;

                liquid.texture = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);

                liquid.visibility = BitConverter.ToSingle(bytes, pointer);
                liquid.type = (UFLevel.UFRoom.LiquidProperties.LiquidType)BitConverter.ToInt32(bytes, pointer + 4);
                liquid.alpha = BitConverter.ToInt32(bytes, pointer + 8);
                liquid.waveForm = (UFLevel.UFRoom.LiquidProperties.WaveForm)BitConverter.ToInt32(bytes, pointer + 12);
                liquid.scrollU = BitConverter.ToSingle(bytes, pointer + 16);
                liquid.scrollV = BitConverter.ToSingle(bytes, pointer + 20);
                pointer += 24;

                nextRoom.liquidProperties = liquid;
            }
            else
                nextRoom.liquidProperties = default(UFLevel.UFRoom.LiquidProperties);

            if(nextRoom.hasAmbientLight) {
                nextRoom.ambientLightColor = UFUtils.GetRGBAColor(bytes, pointer);
                pointer += 4;
            }
            else
                nextRoom.ambientLightColor = default(Color);
        }


        int unkown1Count = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        for(int i = 0; i < unkown1Count; i++) {
            int unkown1SubCount = BitConverter.ToInt32(bytes, pointer + 4);
            pointer += 8 + (4 * unkown1SubCount);
        }

        int unkown2Count = BitConverter.ToInt32(bytes, pointer);
        pointer += 4 + (unkown2Count * 32);

        int nboVertices = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        geometry.vertices = new Vector3[nboVertices];
        for(int i = 0; i < nboVertices; i++) {
            geometry.vertices[i] = UFUtils.Getvector3(bytes, pointer);
            pointer += 12;
        }

        int nboFaces = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        geometry.faces = new UFLevel.UFFace[nboFaces];
        for(int i = 0; i < nboFaces; i++) {
            UFLevel.UFFace nextFace;
            pointer += 16;
            nextFace.texture = BitConverter.ToInt32(bytes, pointer);
            pointer += 24;

            nextFace.showSky = UFUtils.GetFlag(bytes, pointer, 0);
            nextFace.mirrored = UFUtils.GetFlag(bytes, pointer, 1);
            nextFace.fullBright = UFUtils.GetFlag(bytes, pointer, 5);
            pointer += 12;

            int nboFaceVertices = BitConverter.ToInt32(bytes, pointer);
            bool hasExtraCoords = false;

            nextFace.vertices = new UFLevel.UFFaceVertex[nboFaceVertices];
            pointer += 4;
            for(int j = 0; j < nboFaceVertices; j++) {
                UFLevel.UFFaceVertex vertex;
                vertex.id = BitConverter.ToInt32(bytes, pointer);
                vertex.uv = UFUtils.Getvector2(bytes, pointer + 4);
                pointer += 12;

                if(j == 0)
                    hasExtraCoords = ProbablyHasExtraCoords(bytes, nboVertices);

                if(hasExtraCoords)
                    pointer += 8;
            }
            geometry.faces[i] = nextFace;
        }

        int unknownCount2 = BitConverter.ToInt32(bytes, pointer);
        pointer += 4 + (unknownCount2 * 96);

        return geometry;
    }

    /// <summary>
    /// Call this function when pointer is at a section header and this method will skip
    /// ahead to the following section.
    /// </summary>
    private void SkipSection(Byte[] bytes) {
        int sectionLength = BitConverter.ToInt32(bytes, pointer + 4);
        pointer += 8 + sectionLength;
    }


    /// <summary>
    /// Skip pointer up to first byte after the first, next occurence of the given string
    /// </summary>
    private void SkipUntil(Byte[] bytes, string text) {
        byte[] match = Encoding.UTF8.GetBytes(text);
        int matchIndex = 0;
        while(matchIndex < match.Length) {
            if(bytes[pointer++] == match[matchIndex])
                matchIndex++;
            else
                matchIndex = 0;
        }
        matchIndex++;
    }

    /// <summary>
    /// Skip pointer untill it is pointing at the given value
    /// </summary>
    private void SkipUntil(Byte[] bytes, uint value) {
        uint candidate = BitConverter.ToUInt32(bytes, pointer);
        while(candidate != value) {
            pointer++;
            candidate = BitConverter.ToUInt32(bytes, pointer);
        }
    }

    private int[] ReadIntList(Byte[] bytes) {
        int nb = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;

        int[] toReturn = new int[nb];
        for(int i = 0; i < nb; i++) {
            toReturn[i] = BitConverter.ToInt32(bytes, pointer);
            pointer += 4;
        }

        return toReturn;
    }

    private bool MatchesSectionHeader(Byte[] bytes, int pointer) {
        int value = BitConverter.ToInt32(bytes, pointer);
        return Enum.IsDefined(typeof(RFLSection), value);
    }

    /// <summary>
    /// Helper function for reading texture vertex coords (UV).
    /// Returns true if the data structure probably contains 2 extra coordinates.
    /// Appearantly this can change from texture to texture, necessetating this method.
    /// </summary>
    private bool ProbablyHasExtraCoords(byte[] bytes, int nboVertices) {
        int evidence = 0;

        if(!UFUtils.IsPlausibleIndex(bytes, pointer + 12, nboVertices))
            evidence++;
        if(!UFUtils.IsPlausibleIndex(bytes, pointer + 24, nboVertices))
            evidence++;

        return evidence > 0;
    }

    //-------------------------------------- RFL section enum -----------------------------------------------------

    private enum RFLSection {
        TGA = 0x00007000, //required, list of files
        VCM = 0x00007001, //required, list of files
        MVF = 0x00007002, //required, list of files
        V3D = 0x00007003, //required, list of files
        VFX = 0x00007004, //required, list of files
        LevelProperties = 0x00000900, //required, set amount of data
        LightMaps = 0x00001200, //optional, list of uncompressed images
        StaticGeometry = 0x00000100, //required (player spawn must be in level to save), complex
        GeoRegions = 0x00000200, //optional, object list
        AlternateLights = 0x04000000, //optional, object list (might be older version)
        Lights = 0x00000300, //optional, object list

        CutsceneCameras = 0x00000400, //optional, object list
        AmbientSounds = 0x00000500, //optional, object list
        Events = 0x00000600, //optional, object list
        MPRespawns = 0x00000700, //optional, object list
        Particlemitters = 0x00000A00, //optional, object list
        GasRegions = 0x00000B00, //optional, object list
        RoomEffects = 0x00000C00, //optional, object list
        ClimbingRegions = 0x00000D00, //optional, object list

        CutscenePathNodes = 0x00005000, //optional, object list
        Unkown1 = 0x00006000, //unkown, might contain list of 4 bools.
        EAXEffects = 0x00008000,

        LevelInfo = 0x01000000,

        BoltEmitters = 0x00000E00, //optional, object list
        Targets = 0x00000F00, //optional, object list
        Decals = 0x00001000, //optional, object list
        PushRegions = 0x00001100, //optional, object list

        //unkown order





        MovingGeometry = 0x00002000,
        MovingGroups = 0x00003000,
        
        Unkown2 = 0x00010000, // ?????
        NavPoints = 0x00020000,
        Entities = 0x00030000,
        Items = 0x00040000,
        Clutter = 0x00050000,
        Triggers = 0x00060000,
        PlayerStart = 0x00070000,
        
        Brushes = 0x02000000,
        Groups = 0x03000000,

        End = 0x00000000,

        
    };

}