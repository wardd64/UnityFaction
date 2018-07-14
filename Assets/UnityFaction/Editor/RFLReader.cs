using System;
using System.IO;
using UnityEngine;
using UFLevelStructure;

public class RFLReader {

    public LevelData level;

    /* 
     * an RFL file is a binary file that holds data of a redfaction level.
     * The file starts with standard header that holds general info about the level 
     * and the data structure.
     * The rest of the file is split into a series of sections that hold the various level data itself.
     * Each section starts with a specific 4 byte header (detailed in the enum RFLSection)
     * The header is followed by an integer that shows the byte size of the section, 
     * this can be used to skip ahead to the next section. After that, the section data itself 
     * starts. The structure of this dat depends entirely on the section in question and is detailed
     * further below.
     */

    /// <summary>
    /// sections in the enum are given in the order they appear in an RFL file.
    /// </summary>
    private enum RFLSection {
        TGA = 0x00007000,
        VCM = 0x00007001,
        MVF = 0x00007002,
        V3D = 0x00007003,
        VFX = 0x00007004,
        LevelProperties = 0x00000900,
        LightMaps = 0x00001200,
        StaticGeometry = 0x00000100,
        GeoRegions = 0x00000200,
        AlternateLights = 0x04000000,
        Lights = 0x00000300,
        CutsceneCameras = 0x00000400,
        CutscenePathNodes = 0x00005000,
        AmbientSounds = 0x00000500,
        Events = 0x00000600,
        Unkown1 = 0x00006000,
        MPSpawnPoints = 0x00000700,
        Particlemiters = 0x00000A00,
        GasRegions = 0x00000B00,
        Decals = 0x00001000,
        PushRegions = 0x00001100,
        RoomEffects = 0x00000C00,
        EAXEffects = 0x00008000,
        ClimbingRegions = 0x00000D00,
        BoltEmiters = 0x00000E00,
        Targets = 0x00000F00,
        MovingGeometry = 0x00002000,
        MovingGroups = 0x00003000,
        PlayerStart = 0x00070000,
        Unkown2 = 0x00010000,
        NavPoints = 0x00020000,
        Entities = 0x00030000,
        Items = 0x00040000,
        Clutter = 0x00050000,
        Triggers = 0x00060000,
        LevelInfo = 0x01000000,
        Brushes = 0x02000000,
        Groups = 0x03000000,
        End = 0x00000000
    }

    //RFL properties
    const uint RFL_SIGNATURE = 0xD4BADA55;
    const uint RFL_LAST_KNOWN_VERSION = 0x000000C8;

    //file structure parameters
    private int pointer;
    private int playerStartOffset, levelInfoOffset, sectionsCount;

    public RFLReader(string path) {
        byte[] bytes = File.ReadAllBytes(path);
        level = new LevelData();
        ReadRFL(bytes);
    }

    private void ReadRFL(byte[] bytes) {

        //read header
        ReadHeader(bytes);

        //loop over all the sections
        for(int i = 0; i < sectionsCount; i++) {
            RFLSection nextSection = (RFLSection)BitConverter.ToInt32(bytes, pointer);
            ReadSection(nextSection, bytes);
        }

        //check if everything has wrapped up correctly.
        long end = BitConverter.ToInt64(bytes, pointer);
        pointer += 8;
        if(end == 0 && bytes.Length == pointer) {
            //log success
            int fileSize = bytes.Length / 1024;
            Debug.Log("RFL file was read successfully. Read total of " + fileSize + "kb.");
        }
        else
            throw new RFLReadException("RFL parsing ended in erronous state!");

    }

    /// <summary>
    /// Extract general info of the RFL contained in its header.
    /// </summary>
    private void ReadHeader(Byte[] bytes) {

        uint signature = BitConverter.ToUInt32(bytes, 0);
        uint version = BitConverter.ToUInt32(bytes, 4);
        //uint timeStamp = BitConverter.ToUInt32(bytes, 8);
        playerStartOffset = BitConverter.ToInt32(bytes, 12);
        levelInfoOffset = BitConverter.ToInt32(bytes, 16);
        sectionsCount = BitConverter.ToInt32(bytes, 20);
        // Bits 24-28 have unknown purpose...

        bool signatureCheck = signature == RFL_SIGNATURE;
        signatureCheck &= version <= RFL_LAST_KNOWN_VERSION;

        if(!signatureCheck)
            throw new RFLReadException("File did not have correct RFL signature or a known version number.");

        level.name = UFUtils.ReadNullTerminatedString(bytes, 30);

        pointer = 32 + level.name.Length;

        //immediately extract author name from level info
        int authorOffset = levelInfoOffset + level.name.Length + 16;
        level.author = UFUtils.ReadNullTerminatedString(bytes, authorOffset);
    }

    /// <summary>
    /// Switches between every possible RFL section and reads its contents.
    /// When called, we expect the pointer to be at a section header.
    /// When the method returns, all needed information should be extracted 
    /// and the pointer will have moved on the the next section header.
    /// </summary>
    private void ReadSection(RFLSection section, byte[] bytes) {
        switch(section) {

        //Sections that do not appear to contain any useful info to UnityFaction
        case RFLSection.TGA:
        case RFLSection.VCM:
        case RFLSection.MVF:
        case RFLSection.V3D:
        case RFLSection.VFX:
        case RFLSection.LightMaps:
        case RFLSection.Unkown1:
        case RFLSection.CutsceneCameras:
        case RFLSection.CutscenePathNodes:
        case RFLSection.RoomEffects:
        case RFLSection.EAXEffects:
        case RFLSection.Unkown2:
        case RFLSection.NavPoints:
        case RFLSection.Groups:
        SkipSection(bytes);
        break;

        //Useful sections
        case RFLSection.LevelProperties: ReadLevelProperties(bytes); break;
        case RFLSection.StaticGeometry: ReadStaticGeometry(bytes); break;
        case RFLSection.LevelInfo: ReadLevelInfo(bytes); break;
        case RFLSection.GeoRegions: ReadGeoRegions(bytes); break;
        case RFLSection.AlternateLights: ReadLights(bytes, true); break;
        case RFLSection.Lights: ReadLights(bytes); break;
        case RFLSection.AmbientSounds: ReadAmbientSounds(bytes); break;
        case RFLSection.Events: ReadEvents(bytes); break;
        case RFLSection.MPSpawnPoints: ReadMPspawnPoints(bytes); break;
        case RFLSection.Particlemiters: ReadParticleEmiters(bytes); break;
        case RFLSection.GasRegions: SkipSection(bytes); break;
        case RFLSection.Decals: ReadDecalls(bytes); break;
        case RFLSection.PushRegions: ReadPushRegions(bytes); break;
        case RFLSection.ClimbingRegions: ReadClimbingRegions(bytes); break;
        case RFLSection.BoltEmiters: ReadBoltEmiters(bytes); break;
        case RFLSection.Targets: ReadTargets(bytes); break;
        case RFLSection.MovingGeometry: ReadMovingGeometry(bytes); break;
        case RFLSection.MovingGroups: ReadMovingGroups(bytes); break;
        case RFLSection.PlayerStart: ReadPlayerStart(bytes); break;
        case RFLSection.Entities: ReadEntities(bytes); break;
        case RFLSection.Items: ReadItems(bytes); break;
        case RFLSection.Clutter: ReadClutter(bytes); break;
        case RFLSection.Triggers: ReadTriggers(bytes); break;
        case RFLSection.Brushes: ReadBrushes(bytes); break;

        //exception cases
        case RFLSection.End:
        throw new RFLReadException("Encountered RFL end too soon!");

        default:
        throw new RFLReadException("Encountered unknown RFL section!");
        }
    }

    /* -----------------------------------------------------------------------------------------------
     * --------------------------------- SECTION SPECIFIC METHODS ------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    
    /// <summary>
    /// SECTION: Level properties
    /// INCLUDED: Always
    /// CONTAINS: general level info such as default hardness and ambient color
    /// NOTES:
    /// </summary>
    private void ReadLevelProperties(Byte[] bytes) {
        pointer += 8;

        string defaultTexture = UFUtils.ReadRFLString(bytes, ref pointer);
        defaultTexture.Remove(defaultTexture.Length - 1);
        level.geomodTexture = defaultTexture;

        level.hardness = BitConverter.ToInt32(bytes, pointer);
        level.ambientColor = UFUtils.GetRGBAColor(bytes, pointer + 4);
        level.fogColor = UFUtils.GetRGBAColor(bytes, pointer + 9);
        level.nearPlane = BitConverter.ToSingle(bytes, pointer + 13);
        level.farPlane = BitConverter.ToSingle(bytes, pointer + 17);

        pointer += 21;

    }

    /// <summary>
    /// SECTION: Light maps
    /// INCLUDED: If level has calcuted lighting
    /// CONTAINS: Simple RGB images that encode baked lighting
    /// NOTES: UnityFaction relies on Unity's lighting engine, so this info is useless to us.
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

            //...
        }

    }

    /// <summary>
    /// SECTION: Static geometry
    /// INCLUDED: Always (player start is required to be inside air, necessetating static geometry)
    /// CONTAINS: Non-moving geometry data (vertices, faces, UV unwrapping, textures etc).
    /// NOTES: 
    /// </summary>
    private void ReadStaticGeometry(Byte[] bytes) {
        pointer += 18;
        level.staticGeometry = ReadGeometry(bytes);
    }

    /// <summary>
    /// SECTION: Level info
    /// INCLUDED: Always
    /// CONTAINS: Additional general info, such as the name, author and mutiplayer flag.
    /// NOTES: Contains ton of unknown data that requires further investigation.
    /// </summary>
    private void ReadLevelInfo(Byte[] bytes) {
        pointer += 12;

        //level name, author and date
        for(int i = 0; i < 3; i++)
            UFUtils.ReadRFLString(bytes, ref pointer);

        pointer += 1;
        level.multiplayer = BitConverter.ToBoolean(bytes, pointer);
        pointer += 221;
    }

    /// <summary>
    /// SECTION: Geo regions
    /// INCLUDED: Optionally
    /// CONTAINS: Geo region objects, which determine varying level hardness.
    /// NOTES: 
    /// </summary>
    private void ReadGeoRegions(byte[] bytes) {
        pointer += 8;

        int nboGeoRegions = BitConverter.ToInt32(bytes, pointer);
        level.geoRegions = new GeoRegion[nboGeoRegions];
        pointer += 4;
        for(int i = 0; i < nboGeoRegions; i++) {
            GeoRegion nextRegion;
            int id = BitConverter.ToInt32(bytes, pointer);

            nextRegion.ice = UFUtils.GetFlag(bytes, pointer + 4, 6);
            nextRegion.shallow = UFUtils.GetFlag(bytes, pointer + 4, 5);

            byte shapeByte = UFUtils.GetNibble(bytes, pointer + 4, true);
            nextRegion.shape = (GeoRegion.GeoShape)shapeByte;
            nextRegion.hardness = bytes[pointer + 6];
            pointer += 8;

            switch(nextRegion.shape) {

            case GeoRegion.GeoShape.box:
            PosRot posRot = UFUtils.GetPosRot(bytes, pointer);
            nextRegion.transform = new UFTransform(posRot, id);

            nextRegion.extents = UFUtils.Getvector3(bytes, pointer + 48);
            nextRegion.sphereRadius = -1f;

            pointer += 60;
            break;

            case GeoRegion.GeoShape.sphere:
            Vector3 pos = UFUtils.Getvector3(bytes, pointer);
            nextRegion.transform = new UFTransform(pos, id);

            nextRegion.extents = Vector3.zero;
            nextRegion.sphereRadius = BitConverter.ToSingle(bytes, pointer + 12);
            pointer += 16;
            break;

            default:
            throw new RFLReadException("Encountered geo region with unknown shape: " + nextRegion.shape);
            
            }

            level.geoRegions[i] = nextRegion;
        }
    }

    /// <summary>
    /// SECTION: Lights
    /// INCLUDED: Optionally
    /// CONTAINS: "Take a guess."
    /// NOTES: There appears to be an alternate version of this section which is used in newer RFL files.
    ///        This newer version differs only in an extra 4 bytes included in each light object.
    /// </summary>
    private void ReadLights(byte[] bytes, bool alternate = false) {
        pointer += 8;

        int nboLights = BitConverter.ToInt32(bytes, pointer);
        level.lights = new UFLevelStructure.Light[nboLights];
        for(int i = 0; i < nboLights; i++) {
            UFLevelStructure.Light nextLight;

            int id = BitConverter.ToInt32(bytes, pointer);
            pointer += 4;

            if(alternate)
                pointer += 4;

            UFUtils.ReadRFLString(bytes, ref pointer); //"Light"
            PosRot posRot = UFUtils.GetPosRot(bytes, pointer);
            pointer += 48;
            nextLight.transform = new UFTransform(posRot, id);

            UFUtils.ReadRFLString(bytes, ref pointer); // script file name
            pointer += 1;

            byte typeByte = UFUtils.GetNibble(bytes, pointer, false);
            nextLight.type = (UFLevelStructure.Light.LightType)typeByte;
            nextLight.dynamic = UFUtils.GetFlag(bytes, pointer + 1, 0);
            nextLight.shadows = UFUtils.GetFlag(bytes, pointer + 1, 2);
            nextLight.enabled = UFUtils.GetFlag(bytes, pointer + 1, 3);
            nextLight.color = UFUtils.GetRGBAColor(bytes, pointer + 4);
            nextLight.range = BitConverter.ToSingle(bytes, pointer + 8);
            nextLight.fov = BitConverter.ToSingle(bytes, pointer + 12);
            nextLight.fovDropOff = BitConverter.ToSingle(bytes, pointer + 16);
            nextLight.intensityAtMax = BitConverter.ToSingle(bytes, pointer + 20); //1 for max
            nextLight.tubeLength = BitConverter.ToSingle(bytes, pointer + 28);
            nextLight.intensity = BitConverter.ToSingle(bytes, pointer + 32);
            //very strange sequence of floats, 1, 0, 0, 1, 0 ???
            pointer += 56;

            level.lights[i] = nextLight;
        }
    }

    /// <summary>
    /// SECTION: Ambient Sounds
    /// INCLUDED: Optionally
    /// CONTAINS:
    /// NOTES: 
    /// </summary>
    private void ReadAmbientSounds(byte[] bytes) {
        pointer += 8;
        int nboAmbSounds = BitConverter.ToInt32(bytes, pointer);
        level.ambSounds = new AmbSound[nboAmbSounds];
        pointer += 4;
        for(int i = 0; i < nboAmbSounds; i++) {
            AmbSound nextSound;

            int id = BitConverter.ToInt32(bytes, pointer + 12);
            pointer += 4;
            Vector3 pos = UFUtils.Getvector3(bytes, pointer);
            nextSound.transform = new UFTransform(pos, id);
            pointer += 13; //position + editor relevant flags

            nextSound.clip = UFUtils.ReadRFLString(bytes, ref pointer);
            nextSound.minDist = BitConverter.ToSingle(bytes, pointer);
            nextSound.volume = BitConverter.ToSingle(bytes, pointer + 4);
            nextSound.roloff = BitConverter.ToSingle(bytes, pointer + 8);
            nextSound.startDelay = BitConverter.ToInt32(bytes, pointer + 12);
            pointer += 16;

            level.ambSounds[i] = nextSound;
        }
    }

    /// <summary>
    /// SECTION: Events
    /// INCLUDED: Optionally
    /// CONTAINS: Event objects that encode custom level mechanics such as switches, doors and teleporters.
    /// NOTES: 
    /// </summary>
    private void ReadEvents(byte[] bytes) {

        pointer += 8;

        int nboEvents = BitConverter.ToInt32(bytes, pointer);
        level.events = new UFLevelStructure.Event[nboEvents];
        pointer += 4;
        for(int i = 0; i < nboEvents; i++) {
            UFLevelStructure.Event nextEvent;

            int id = BitConverter.ToInt32(bytes, pointer);
            pointer += 4;

            string name = UFUtils.ReadRFLString(bytes, ref pointer); //event name
            object typeParse = Enum.Parse(typeof(UFLevelStructure.Event.EventType), name);
            nextEvent.type = (UFLevelStructure.Event.EventType)typeParse;

            Vector3 position = UFUtils.Getvector3(bytes, pointer);
            pointer += 12;
            nextEvent.transform = new UFTransform(position, id);

            nextEvent.name = UFUtils.ReadRFLString(bytes, ref pointer); //object name
            pointer += 1;

            nextEvent.delay = BitConverter.ToSingle(bytes, pointer);
            pointer += 4;

            //read event data (value meaning depend on event type, not all values are used)
            nextEvent.bool1 = BitConverter.ToBoolean(bytes, pointer);
            nextEvent.bool2 = BitConverter.ToBoolean(bytes, pointer + 1);
            nextEvent.int1 = BitConverter.ToInt32(bytes, pointer + 2);
            nextEvent.int2 = BitConverter.ToInt32(bytes, pointer + 6);
            nextEvent.float1 = BitConverter.ToSingle(bytes, pointer + 10);
            nextEvent.float2 = BitConverter.ToSingle(bytes, pointer + 14);
            pointer += 18;

            nextEvent.string1 = UFUtils.ReadRFLString(bytes, ref pointer);
            nextEvent.string2 = UFUtils.ReadRFLString(bytes, ref pointer);

            nextEvent.links = ReadIntList(bytes);

            if(UFLevelStructure.Event.HasRotation(nextEvent.type)) {
                Quaternion rotation = UFUtils.GetRotation(bytes, pointer);
                pointer += 36;
                nextEvent.transform = new UFTransform(position, rotation, id);
            }

            nextEvent.color = UFUtils.GetRGBAColor(bytes, pointer);
            pointer += 4;

            level.events[i] = nextEvent;
        }
    }

    /// <summary>
    /// SECTION: Multi player spawn points
    /// INCLUDED: Optionally, not required in single player levels
    /// CONTAINS:
    /// NOTES:
    /// </summary>
    private void ReadMPspawnPoints(byte[] bytes) {
        int nboSpawnPoints = BitConverter.ToInt32(bytes, pointer + 8);
        level.spawnPoints = new SpawnPoint[nboSpawnPoints];
        pointer += 12;

        for(int i = 0; i < nboSpawnPoints; i++) {
            SpawnPoint nextPoint;

            int id = BitConverter.ToInt32(bytes, pointer);
            pointer += 4;

            PosRot posRot = UFUtils.GetPosRot(bytes, pointer);
            pointer += 48;

            UFUtils.ReadRFLString(bytes, ref pointer);
            nextPoint.transform = new UFTransform(posRot, id);
            pointer += 1;

            nextPoint.team = BitConverter.ToInt32(bytes, pointer);
            nextPoint.redTeam = BitConverter.ToBoolean(bytes, pointer + 4);
            nextPoint.blueTeam = BitConverter.ToBoolean(bytes, pointer + 5);
            nextPoint.bot = BitConverter.ToBoolean(bytes, pointer + 6);
            pointer += 7;

            level.spawnPoints[i] = nextPoint;
        }
    }

    /// <summary>
    /// SECTION: Particle emiters
    /// INCLUDED: Optionally
    /// CONTAINS:
    /// NOTES:
    /// </summary>
    private void ReadParticleEmiters(byte[] bytes) {
        int nboEmitters = BitConverter.ToInt32(bytes, pointer + 8);
        level.particleEmiters = new UFLevelStructure.ParticleEmiter[nboEmitters];
        pointer += 12;

        for(int i = 0; i < nboEmitters; i++) {
            UFLevelStructure.ParticleEmiter nextEmitter;

            nextEmitter.transform = ReadFullTransform(bytes);

            int typeInt = BitConverter.ToInt32(bytes, pointer);
            nextEmitter.type = (UFLevelStructure.ParticleEmiter.EmiterShape)typeInt;

            nextEmitter.SphereRadius = BitConverter.ToSingle(bytes, pointer + 4);
            nextEmitter.planeExtents = UFUtils.Getvector2(bytes, pointer + 8);
            pointer += 16;

            nextEmitter.texture = UFUtils.ReadRFLString(bytes, ref pointer);

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

            level.particleEmiters[i] = nextEmitter;
        }
    }

    /// <summary>
    /// SECTION: Decalls
    /// INCLUDED: Optionally
    /// CONTAINS:
    /// NOTES:
    /// </summary>
    private void ReadDecalls(byte[] bytes) {
        int nboDecalls = BitConverter.ToInt32(bytes, pointer + 8);
        level.decals = new Decal[nboDecalls];
        pointer += 12;

        for(int i = 0; i < nboDecalls; i++) {
            Decal nextDecal;

            UFTransform transform = ReadFullTransform(bytes);

            Vector3 extents = UFUtils.Getvector3(bytes, pointer);
            nextDecal.cbTransform = new CenteredBox(transform, extents);
            pointer += 12;

            nextDecal.texture = UFUtils.ReadRFLString(bytes, ref pointer);

            nextDecal.alpha = BitConverter.ToInt32(bytes, pointer);
            nextDecal.selfIlluminated = BitConverter.ToBoolean(bytes, pointer + 4);
            nextDecal.tiling = (Decal.TilingMode)bytes[pointer + 5];
            nextDecal.scale = BitConverter.ToSingle(bytes, pointer + 9);
            pointer += 13;

            level.decals[i] = nextDecal;
        }
    }


    /// <summary>
    /// SECTION: Room Effects
    /// INCLUDED: Optionally
    /// CONTAINS: RFL editor objects that control properties of individual rooms
    /// NOTES: These properties are saved in static geometry, so there is no need to read them here.
    /// </summary>
    private void ReadRoomEffects(byte[] bytes) {
        pointer += 8;
        int nboObjects = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        for(int i = 0; i < nboObjects; i++) {
            pointer += 15; //UID + random crap
            UFUtils.ReadRFLString(bytes, ref pointer);
            pointer += 48; //transform
            UFUtils.ReadRFLString(bytes, ref pointer);
            pointer += 1; //null
        }
    }

    /// <summary>
    /// SECTION: EAX Effects
    /// INCLUDED: Optionally
    /// CONTAINS: Sound effects such as reverb, that change the way audio is perceived in a room.
    /// NOTES: Same as Room effects, saved elsewhere, not needed.
    /// </summary>
    private void ReadEAXEffects(byte[] bytes) {
        pointer += 8;
        int nboObjects = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        for(int i = 0; i < nboObjects; i++) {
            pointer += 6; //UID + type enum
            UFUtils.ReadRFLString(bytes, ref pointer);
            pointer += 48; //transform
            UFUtils.ReadRFLString(bytes, ref pointer);
            pointer += 1; //null
        }
    }

    /// <summary>
    /// SECTION: Climbing regions
    /// INCLUDED: Optionally
    /// CONTAINS:
    /// NOTES: Length depends on box/sphere shape
    /// </summary>
    private void ReadPushRegions(byte[] bytes) {
        int nboPushRegions = BitConverter.ToInt32(bytes, pointer + 8);
        level.pushRegions = new PushRegion[nboPushRegions];
        pointer += 12;

        for(int i = 0; i < nboPushRegions; i++) {
            PushRegion nextRegion;

            nextRegion.transform = ReadFullTransform(bytes);

            nextRegion.shape = (PushRegion.PushShape)BitConverter.ToInt32(bytes, pointer);
            pointer += 4;

            switch(nextRegion.shape) {

            case PushRegion.PushShape.alignedBox: case PushRegion.PushShape.orientedBox:
            nextRegion.extents = UFUtils.Getvector3(bytes, pointer);
            nextRegion.sphereRadius = 0f;
            pointer += 12;
            break;

            case PushRegion.PushShape.sphere:
            nextRegion.extents = Vector3.zero;
            nextRegion.sphereRadius = BitConverter.ToSingle(bytes, pointer);
            pointer += 4;
            break;

            default:
            nextRegion.extents = Vector3.zero;
            nextRegion.sphereRadius = 0f;
            break;
            }
            
            nextRegion.strength = BitConverter.ToSingle(bytes, pointer);

            bool profile1 = UFUtils.GetFlag(bytes, pointer + 4, 2);
            bool profile2 = UFUtils.GetFlag(bytes, pointer + 4, 3);
            int profileInt = (profile1 ? 1 : 0) + (profile2 ? 2 : 0);
            nextRegion.profile = (PushRegion.Profile)profileInt;

            nextRegion.jumpPad = UFUtils.GetFlag(bytes, pointer + 4, 6);
            nextRegion.massIndependent = UFUtils.GetFlag(bytes, pointer + 4, 0);
            nextRegion.noPlayer = UFUtils.GetFlag(bytes, pointer + 4, 5);
            nextRegion.radial = UFUtils.GetFlag(bytes, pointer + 4, 4);
            nextRegion.grounded = UFUtils.GetFlag(bytes, pointer + 4, 1);

            nextRegion.turbulence = BitConverter.ToInt16(bytes, pointer + 6);

            pointer += 8;

            level.pushRegions[i] = nextRegion;
        }
    }

    /// <summary>
    /// SECTION: Climbing regions
    /// INCLUDED: Optionally
    /// CONTAINS:
    /// NOTES:
    /// </summary>
    private void ReadClimbingRegions(byte[] bytes) {
        int nboClimbingRegions = BitConverter.ToInt32(bytes, pointer + 8);
        level.climbingRegions = new ClimbingRegion[nboClimbingRegions];
        pointer += 12;

        for(int i = 0; i < nboClimbingRegions; i++) {
            ClimbingRegion nextRegion;

            UFTransform transform = ReadFullTransform(bytes);

            nextRegion.type = (ClimbingRegion.ClimbingType)BitConverter.ToInt32(bytes, pointer);

            Vector3 extents = UFUtils.Getvector3(bytes, pointer + 4);
            nextRegion.cbTransform = new CenteredBox(transform, extents);
            pointer += 16;

            level.climbingRegions[i] = nextRegion;
        }
    }

    /// <summary>
    /// SECTION: Bolt emiters
    /// INCLUDED: Optionally
    /// CONTAINS:
    /// NOTES:
    /// </summary>
    private void ReadBoltEmiters(byte[] bytes) {

        int nboBoltEmiters = BitConverter.ToInt32(bytes, pointer + 8);
        level.boltEmiters = new BoltEmiter[nboBoltEmiters];
        pointer += 12;

        for(int i = 0; i < nboBoltEmiters; i++) {
            BoltEmiter nextEmiter;
            nextEmiter.transform = ReadFullTransform(bytes);

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

            nextEmiter.texture = UFUtils.ReadRFLString(bytes, ref pointer);

            nextEmiter.fade = UFUtils.GetFlag(bytes, pointer, 1);
            nextEmiter.glow = UFUtils.GetFlag(bytes, pointer, 2);
            nextEmiter.srcDirLock = UFUtils.GetFlag(bytes, pointer, 3);
            nextEmiter.trgDirLock = UFUtils.GetFlag(bytes, pointer, 4);
            nextEmiter.initOn = BitConverter.ToBoolean(bytes, pointer + 4);
            pointer += 5;

            level.boltEmiters[i] = nextEmiter;
        }

    }

    /// <summary>
    /// SECTION: Targets
    /// INCLUDED: Optionally
    /// CONTAINS: Target objects which are used mostly to aim bolt emiters
    /// NOTES: These objects are just blank transforms, with no extra information.
    /// </summary>
    public void ReadTargets(byte[] bytes) {
        int nboTargets = BitConverter.ToInt32(bytes, pointer + 8);
        level.targets = new UFTransform[nboTargets];
        pointer += 12;

        for(int i = 0; i < nboTargets; i++)
            level.targets[i] = ReadFullTransform(bytes);
    }

    /// <summary>
    /// SECTION: Moving geometry
    /// INCLUDED: If level has moving groups
    /// CONTAINS: Brushes that are part of moving groups.
    /// NOTES:
    /// </summary>
    public void ReadMovingGeometry(byte[] bytes) {
        int nboMovingBrushes = BitConverter.ToInt32(bytes, pointer + 8);
        level.movingGeometry = new Brush[nboMovingBrushes];
        pointer += 12;

        for(int i = 0; i < nboMovingBrushes; i++)
            level.movingGeometry[i] = ReadBrush(bytes);
    }

    /// <summary>
    /// SECTION: Moving groups
    /// INCLUDED: If level has moving groups
    /// CONTAINS: moving groups and keyframes specifying their movement
    /// NOTES:
    /// </summary>
    public void ReadMovingGroups(byte[] bytes) {
        int nboMovingGroups = BitConverter.ToInt32(bytes, pointer + 8);
        level.movingGroups = new MovingGroup[nboMovingGroups];
        pointer += 12;

        for(int i = 0; i < nboMovingGroups; i++) {
            MovingGroup nextGroup;

            nextGroup.name = UFUtils.ReadRFLString(bytes, ref pointer);
            pointer += 2; // null + 1 ???

            int nboKeyFrames = BitConverter.ToInt32(bytes, pointer);
            nextGroup.keys = new UFLevelStructure.Keyframe[nboKeyFrames];
            pointer += 4;

            for(int j = 0; j < nboKeyFrames; j++) {
                UFLevelStructure.Keyframe nextKey;

                int id = BitConverter.ToInt32(bytes, pointer);
                pointer += 4;

                PosRot posRot = UFUtils.GetPosRot(bytes, pointer);
                pointer += 48;
                nextKey.transform = new UFTransform(posRot, id);

                UFUtils.ReadRFLString(bytes, ref pointer);
                pointer += 1;

                nextKey.pauseTime = BitConverter.ToSingle(bytes, pointer);
                nextKey.departTravelTime = BitConverter.ToSingle(bytes, pointer + 4);
                nextKey.returnTravelTime = BitConverter.ToSingle(bytes, pointer + 8);
                nextKey.accelTime = BitConverter.ToSingle(bytes, pointer + 12);
                nextKey.decelTime = BitConverter.ToSingle(bytes, pointer + 16);
                nextKey.triggerID = BitConverter.ToInt32(bytes, pointer + 20);
                nextKey.containID1 = BitConverter.ToInt32(bytes, pointer + 24);
                nextKey.containID2 = BitConverter.ToInt32(bytes, pointer + 28);
                nextKey.rotationAmount = BitConverter.ToSingle(bytes, pointer + 32); //degrees
                pointer += 36;

                nextGroup.keys[j] = nextKey;
            }

            int unkownCount1 = BitConverter.ToInt32(bytes, pointer);
            pointer += 4 + (unkownCount1 * 52); //clearly id, pos, rot; but why?

            nextGroup.isDoor = BitConverter.ToBoolean(bytes, pointer);
            nextGroup.rotateInPlace = BitConverter.ToBoolean(bytes, pointer + 1);
            nextGroup.startsBackwards = BitConverter.ToBoolean(bytes, pointer + 2);
            nextGroup.useTravTimeAsSpd = BitConverter.ToBoolean(bytes, pointer + 3);
            nextGroup.forceOrient = BitConverter.ToBoolean(bytes, pointer + 4);
            nextGroup.noPlayerCollide = BitConverter.ToBoolean(bytes, pointer + 5);
            pointer += 6;

            nextGroup.type = (MovingGroup.MovementType)BitConverter.ToInt32(bytes, pointer);
            nextGroup.startIndex = BitConverter.ToInt32(bytes, pointer + 4);
            pointer += 8;

            nextGroup.startClip = UFUtils.ReadRFLString(bytes, ref pointer);
            nextGroup.startVol = BitConverter.ToSingle(bytes, pointer);
            pointer += 4;

            nextGroup.loopClip = UFUtils.ReadRFLString(bytes, ref pointer);
            nextGroup.loopVol = BitConverter.ToSingle(bytes, pointer);
            pointer += 4;

            nextGroup.stopClip = UFUtils.ReadRFLString(bytes, ref pointer);
            nextGroup.stopVol = BitConverter.ToSingle(bytes, pointer);
            pointer += 4;

            nextGroup.closeClip = UFUtils.ReadRFLString(bytes, ref pointer);
            nextGroup.closeVol = BitConverter.ToSingle(bytes, pointer);
            pointer += 4;

            int unkownCount2 = BitConverter.ToInt32(bytes, pointer);
            pointer += 4 + (unkownCount2 * 4); //seems to be ID, but with what purpose?

            nextGroup.contents = ReadIntList(bytes);

            level.movingGroups[i] = nextGroup;
        }
    }

    /// <summary>
    /// SECTION: Player start
    /// INCLUDED: Always
    /// CONTAINS: position and rotation at which player 
    ///           should spawn in the level in single player.
    /// NOTES:
    /// </summary>
    private void ReadPlayerStart(byte[] bytes) {
        if(pointer != playerStartOffset)
            Debug.LogWarning("Found player start section at wrong location, continuing parsing anyway...");
        pointer += 8;
        level.playerStart = UFUtils.GetPosRot(bytes, pointer);
        pointer += 48;
    }

    /// <summary>
    /// SECTION: Entities
    /// INCLUDED: Optionally
    /// CONTAINS: 
    /// NOTES:
    /// </summary>
    private void ReadEntities(byte[] bytes) {

        int nboEntities = BitConverter.ToInt32(bytes, pointer + 8);
        level.entities = new Entity[nboEntities];
        pointer += 12;

        for(int i = 0; i < nboEntities; i++) {
            Entity entity;

            entity.transform = ReadFullTransform(bytes);

            entity.cooperation = BitConverter.ToInt32(bytes, pointer);
            entity.friendliness = BitConverter.ToInt32(bytes, pointer + 4);
            entity.team = BitConverter.ToInt32(bytes, pointer + 8);
            pointer += 12;

            entity.wayPointList = UFUtils.ReadRFLString(bytes, ref pointer);
            entity.wayPointMethod = UFUtils.ReadRFLString(bytes, ref pointer);
            pointer += 1;

            entity.boarded = BitConverter.ToBoolean(bytes, pointer);
            entity.readyToFire = BitConverter.ToBoolean(bytes, pointer + 1);
            entity.onlyAttackPlayer = BitConverter.ToBoolean(bytes, pointer + 2);
            entity.weaponIsHolstered = BitConverter.ToBoolean(bytes, pointer + 3);
            entity.deaf = BitConverter.ToBoolean(bytes, pointer + 4);
            pointer += 5;

            entity.sweepMinAngle = BitConverter.ToInt32(bytes, pointer);
            entity.sweepMaxAngle = BitConverter.ToInt32(bytes, pointer + 4);
            pointer += 8;

            entity.ignoreTerrainWhenFiring = BitConverter.ToBoolean(bytes, pointer);
            entity.startCrouched = BitConverter.ToBoolean(bytes, pointer + 2);
            pointer += 3;

            entity.life = BitConverter.ToSingle(bytes, pointer);
            entity.armor = BitConverter.ToSingle(bytes, pointer + 4);
            entity.fov = BitConverter.ToInt32(bytes, pointer + 8);
            pointer += 12;

            entity.primary = UFUtils.ReadRFLString(bytes, ref pointer);
            entity.secondary = UFUtils.ReadRFLString(bytes, ref pointer);
            entity.itemDrop = UFUtils.ReadRFLString(bytes, ref pointer);
            entity.stateAnim = UFUtils.ReadRFLString(bytes, ref pointer);
            entity.corpsePose = UFUtils.ReadRFLString(bytes, ref pointer);
            entity.skin = UFUtils.ReadRFLString(bytes, ref pointer);
            entity.deathAnim = UFUtils.ReadRFLString(bytes, ref pointer);

            entity.aiMode = bytes[pointer];
            entity.aiAttackStyle = bytes[pointer + 1];
            pointer += 6;

            entity.turretID = BitConverter.ToInt32(bytes, pointer);
            entity.alertCameraID = BitConverter.ToInt32(bytes, pointer + 4);
            entity.alarmEventID = BitConverter.ToInt32(bytes, pointer + 8);
            pointer += 12;

            entity.run = BitConverter.ToBoolean(bytes, pointer);
            entity.hidden = BitConverter.ToBoolean(bytes, pointer + 1);
            entity.helmet = BitConverter.ToBoolean(bytes, pointer + 2);
            entity.endGameIfKilled = BitConverter.ToBoolean(bytes, pointer + 3);
            entity.cowerFromWeapon = BitConverter.ToBoolean(bytes, pointer + 4);
            entity.questionUnarmedPlayer = BitConverter.ToBoolean(bytes, pointer + 5);
            entity.dontHum = BitConverter.ToBoolean(bytes, pointer + 6);
            entity.noShadow = BitConverter.ToBoolean(bytes, pointer + 7);
            entity.alwaysSimulate = BitConverter.ToBoolean(bytes, pointer + 8);
            entity.perfectAim = BitConverter.ToBoolean(bytes, pointer + 9);
            entity.permanentCorpse = BitConverter.ToBoolean(bytes, pointer + 10);
            entity.neverFly = BitConverter.ToBoolean(bytes, pointer + 11);
            entity.neverLeave = BitConverter.ToBoolean(bytes, pointer + 12);
            entity.noPersonaMessages = BitConverter.ToBoolean(bytes, pointer + 13);
            entity.fadeCorpseImmedate = BitConverter.ToBoolean(bytes, pointer + 14);
            entity.neverCollideWithPlayer = BitConverter.ToBoolean(bytes, pointer + 15);
            entity.useCustomAttackRange = BitConverter.ToBoolean(bytes, pointer + 16);
            pointer += 17;

            if(entity.useCustomAttackRange) {
                entity.customAttackRange = BitConverter.ToSingle(bytes, pointer);
                pointer += 4;
            }
            else
                entity.customAttackRange = -1f;

            entity.leftHandHolding = UFUtils.ReadRFLString(bytes, ref pointer);
            entity.rightHandHolding = UFUtils.ReadRFLString(bytes, ref pointer);

            level.entities[i] = entity;
        }
    }

    /// <summary>
    /// SECTION: Items
    /// INCLUDED: Optionally
    /// CONTAINS: Objects the player can pick up
    /// NOTES: 
    /// </summary>
    private void ReadItems(byte[] bytes) {
        int nboItems = BitConverter.ToInt32(bytes, pointer + 8);
        level.items = new Item[nboItems];
        pointer += 12;

        for(int i = 0; i < nboItems; i++) {
            Item nextItem;

            nextItem.transform = ReadFullTransform(bytes, out nextItem.name);

            nextItem.count = BitConverter.ToInt32(bytes, pointer);
            nextItem.respawnTime = BitConverter.ToInt32(bytes, pointer + 4);
            nextItem.team = BitConverter.ToInt32(bytes, pointer + 8);
            pointer += 12;

            level.items[i] = nextItem;
        }
    }

    /// <summary>
    /// SECTION: Clutter
    /// INCLUDED: Optionally
    /// CONTAINS: Decorations, garbage, destructibles, switches etc.
    /// NOTES: 
    /// </summary>
    private void ReadClutter(byte[] bytes) {
        int nboClutter = BitConverter.ToInt32(bytes, pointer + 8);
        level.clutter = new Clutter[nboClutter];
        pointer += 12;

        for(int i = 0; i < nboClutter; i++) {
            Clutter nextClutter;

            nextClutter.transform = ReadFullTransform(bytes, out nextClutter.name);

            pointer += 6; // all null ???

            nextClutter.links = ReadIntList(bytes);

            level.clutter[i] = nextClutter;
        }
    }

    /// <summary>
    /// SECTION: Triggers
    /// INCLUDED: Optionally
    /// CONTAINS: Automatic and player activated trigger regions
    /// NOTES: Length depends on sphere/box shape
    /// </summary>
    private void ReadTriggers(byte[] bytes) {
        int nboTriggers = BitConverter.ToInt32(bytes, pointer + 8);
        level.triggers = new Trigger[nboTriggers];
        pointer += 12;

        for(int i = 0; i < nboTriggers; i++) {
            Trigger nextTrigger;

            int id = BitConverter.ToInt32(bytes, pointer);
            pointer += 4;

            UFUtils.ReadRFLString(bytes, ref pointer);
            pointer += 1;

            nextTrigger.box = BitConverter.ToBoolean(bytes, pointer);
            nextTrigger.resetDelay = BitConverter.ToSingle(bytes, pointer + 4);
            nextTrigger.resets = BitConverter.ToInt32(bytes, pointer + 8);
            nextTrigger.useKey = BitConverter.ToBoolean(bytes, pointer + 12);
            pointer += 13;

            nextTrigger.keyName = UFUtils.ReadRFLString(bytes, ref pointer);

            nextTrigger.weaponActivates = BitConverter.ToBoolean(bytes, pointer);
            nextTrigger.isNPC = BitConverter.ToBoolean(bytes, pointer + 2);
            nextTrigger.isAuto = BitConverter.ToBoolean(bytes, pointer + 3);
            nextTrigger.inVehicle = BitConverter.ToBoolean(bytes, pointer + 4);
            pointer += 5;

            Vector3 position = UFUtils.Getvector3(bytes, pointer);
            nextTrigger.transform = new UFTransform(position, id);
            pointer += 12;

            if(nextTrigger.box) {
                Quaternion rotation = UFUtils.GetRotation(bytes, pointer);
                Vector3 wdh = UFUtils.Getvector3(bytes, pointer + 36);
                nextTrigger.extents = new Vector3(wdh.y, wdh.z, wdh.x);
                nextTrigger.oneWay = BitConverter.ToBoolean(bytes, pointer + 48);
                nextTrigger.transform = new UFTransform(position, rotation, id);
                pointer += 49;

                nextTrigger.sphereRadius = -1f;
            }
            else {
                nextTrigger.sphereRadius = BitConverter.ToSingle(bytes, pointer);
                pointer += 4;

                nextTrigger.extents = Vector3.zero;
                nextTrigger.oneWay = false;
            }

            nextTrigger.airlockRoom = BitConverter.ToInt32(bytes, pointer);
            nextTrigger.attachedTo = BitConverter.ToInt32(bytes, pointer + 4);
            nextTrigger.useClutter = BitConverter.ToInt32(bytes, pointer + 8);
            nextTrigger.disabled = BitConverter.ToBoolean(bytes, pointer + 12);
            nextTrigger.buttonActiveTime = BitConverter.ToSingle(bytes, pointer + 13);
            nextTrigger.insideTime = BitConverter.ToSingle(bytes, pointer + 17);
            pointer += 25;

            nextTrigger.links = ReadIntList(bytes);

            level.triggers[i] = nextTrigger;
        }
    }

    /// <summary>
    /// SECTION: Brushes
    /// INCLUDED: Always (same reason as static geometry)
    /// CONTAINS: List of ALL brushes and the geometry they contain.
    /// NOTES: All geometric info was already included in static geometry
    ///        and moving geometry. Even so, the brushes themselves provide 
    ///        extra info about destructibility , portals and more, making them
    ///        usefull to UnityFaction anyway.
    /// </summary>
    private void ReadBrushes(byte[] bytes) {
        int nboBrushes = BitConverter.ToInt32(bytes, pointer + 8);
        level.brushes = new Brush[nboBrushes];
        pointer += 12;

        for(int i = 0; i < nboBrushes; i++)
            level.brushes[i] = ReadBrush(bytes);
        
}

    /* -----------------------------------------------------------------------------------------------
     * -------------------------------------- HELPER METHODS -----------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    /// <summary>
    /// Reads a brush and returns it. Brushes appear in the brushes section (duh) and 
    /// in the moving geometry section, they contain geometry info as well as a 
    /// handfull of flags and a life amount for destructibility.
    /// </summary>
    private Brush ReadBrush(byte[] bytes) {
        Brush brush;

        int id = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;

        PosRot posRot = UFUtils.GetPosRot(bytes, pointer);
        pointer += 48;
        brush.transform = new UFTransform(posRot, id);

        pointer += 10; // all null ???
        brush.geometry = ReadGeometry(bytes);

        brush.isPortal = UFUtils.GetFlag(bytes, pointer, 0);
        brush.isAir = UFUtils.GetFlag(bytes, pointer, 1);
        brush.isDetail = UFUtils.GetFlag(bytes, pointer, 2);
        brush.emitsSteam = UFUtils.GetFlag(bytes, pointer, 4);
        pointer += 4;

        brush.life = BitConverter.ToInt32(bytes, pointer);
        pointer += 8;

        return brush;
    }


    /// <summary>
    /// Reads geometry section and returns it.
    /// Pointer should be at the start of number of textures int and will 
    /// move until right after the geometry info.
    /// Geometry sections appear firstly as static geometry and 
    /// secondly inside moving geometry brushes
    /// </summary>
    private Geometry ReadGeometry(byte[] bytes) {
        Geometry geometry;

        geometry.textures = ReadStringList(bytes);

        int nboScrolls = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        if(nboScrolls > 0)
            throw new RFLReadException("Cannot yet read scrolling textures");

        int nboRooms = BitConverter.ToInt32(bytes, pointer);
        geometry.rooms = new Room[nboRooms];
        pointer += 4;
        for(int i = 0; i < nboRooms; i++) {
            Room nextRoom;

            pointer += 4;

            Vector3 aabb1 = UFUtils.Getvector3(bytes, pointer);
            Vector3 aabb2 = UFUtils.Getvector3(bytes, pointer + 12);
            nextRoom.aabb = new AxisAlignedBoundingBox(aabb1, aabb2);
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

            nextRoom.eaxEffect = UFUtils.ReadRFLString(bytes, ref pointer);

            if(nextRoom.hasLiquid) {
                Room.LiquidProperties liquid;

                liquid.depth = BitConverter.ToSingle(bytes, pointer);
                liquid.color = UFUtils.GetRGBAColor(bytes, pointer + 4);
                pointer += 8;

                liquid.texture = UFUtils.ReadRFLString(bytes, ref pointer);

                liquid.visibility = BitConverter.ToSingle(bytes, pointer);
                liquid.type = (Room.LiquidProperties.LiquidType)BitConverter.ToInt32(bytes, pointer + 4);
                liquid.alpha = BitConverter.ToInt32(bytes, pointer + 8);
                liquid.waveForm = (Room.LiquidProperties.WaveForm)BitConverter.ToInt32(bytes, pointer + 12);
                liquid.scrollU = BitConverter.ToSingle(bytes, pointer + 16);
                liquid.scrollV = BitConverter.ToSingle(bytes, pointer + 20);
                pointer += 24;

                nextRoom.liquidProperties = liquid;
            }
            else
                nextRoom.liquidProperties = default(Room.LiquidProperties);

            if(nextRoom.hasAmbientLight) {
                nextRoom.ambientLightColor = UFUtils.GetRGBAColor(bytes, pointer);
                pointer += 4;
            }
            else
                nextRoom.ambientLightColor = default(Color);

            geometry.rooms[i] = nextRoom;
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
        geometry.faces = new Face[nboFaces];
        for(int i = 0; i < nboFaces; i++) {
            Face nextFace;
            pointer += 16;
            nextFace.texture = BitConverter.ToInt32(bytes, pointer);
            pointer += 24;

            nextFace.showSky = UFUtils.GetFlag(bytes, pointer, 0);
            nextFace.mirrored = UFUtils.GetFlag(bytes, pointer, 1);
            nextFace.fullBright = UFUtils.GetFlag(bytes, pointer, 5);
            pointer += 12;

            int nboFaceVertices = BitConverter.ToInt32(bytes, pointer);
            bool hasExtraCoords = false;

            nextFace.vertices = new FaceVertex[nboFaceVertices];
            pointer += 4;
            for(int j = 0; j < nboFaceVertices; j++) {
                FaceVertex vertex;
                vertex.vertexRef = BitConverter.ToInt32(bytes, pointer);
                vertex.uv = UFUtils.Getvector2(bytes, pointer + 4);
                pointer += 12;

                if(j == 0)
                    hasExtraCoords = ProbablyHasExtraCoords(bytes, nboVertices);

                if(hasExtraCoords)
                    pointer += 8;

                nextFace.vertices[j] = vertex;
            }
            geometry.faces[i] = nextFace;
        }

        int unknownCount2 = BitConverter.ToInt32(bytes, pointer);
        pointer += 4 + (unknownCount2 * 96);

        return geometry;
    }

    /// <summary>
    /// Use provided section length to simply skip until the next section.
    /// </summary>
    private void SkipSection(Byte[] bytes) {
        int sectionLength = BitConverter.ToInt32(bytes, pointer + 4);
        pointer += 8 + sectionLength;
    }

    /// <summary>
    /// Read number of ints and the following list and returns it, moving pointer along.
    /// Usefull for reading UID links.
    /// </summary>
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

    /// <summary>
    /// Read number of strings and the following list and returns it, moving pointer along.
    /// </summary>
    private string[] ReadStringList(Byte[] bytes) {
        int nb = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;

        string[] toReturn = new string[nb];
        for(int i = 0; i < nb; i++)
            toReturn[i] = UFUtils.ReadRFLString(bytes, ref pointer);
        

        return toReturn;
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

    /// <summary>
    /// start pointer at UID and read until an extra byte after the 
    /// custom script name. Returns a transform objects wich encodes
    /// position, rotation and id. Also returns name string.
    /// </summary>
    private UFTransform ReadFullTransform(byte[] bytes, out string name) {
        int id = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;

        name = UFUtils.ReadRFLString(bytes, ref pointer);
        PosRot posRot = UFUtils.GetPosRot(bytes, pointer);
        pointer += 48;

        UFUtils.ReadRFLString(bytes, ref pointer);
        pointer += 1;

        return new UFTransform(posRot, id);
    }

    /// <summary>
    /// start pointer at UID and read until an extra byte after the 
    /// custom script name. Returns a transform objects wich encodes
    /// position, rotation and id.
    /// </summary>
    private UFTransform ReadFullTransform(byte[] bytes) {
        string dummy;
        return ReadFullTransform(bytes, out dummy);
    }

    public class RFLReadException : Exception {
        public RFLReadException() {
        }

        public RFLReadException(string message)
            : base(message) {
        }

        public RFLReadException(string message, Exception inner)
            : base(message, inner) {
        }
    }

}