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
     * Each section starts with a specific 4 byte header (detailed as the enum RFLSection)
     * The header is often (but not always) followed by an integer that show the number of elements in the section
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
        GUILayout.Label("Loaded file: " + fileName);

        level.name = EditorGUILayout.TextField("Level name", level.name);
        level.author = EditorGUILayout.TextField("Author name", level.author);
    }

    //----------------------------------------------- RFL reading --------------------------------------------

    //various info
    private bool signatureCheck;

    //file structure parameters
    private int pointer;
    private int playerStartOffset, levelInfoOffset, sectionsCount;
    private bool fileHasLightMaps;

    private void ReadRFL(Byte[] bytes) {

        fileHasLightMaps = false;

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

            case RFLSection.TGA:
            pointer += 20;
            SkipStringList(bytes);
            break;

            case RFLSection.VCM:
            pointer += 16;
            SkipStringList(bytes);
            SkipUntil(bytes, RFLSection.MVF);
            break;

            case RFLSection.MVF:
            pointer += 16;
            SkipStringList(bytes);
            SkipUntil(bytes, RFLSection.V3D);
            break;

            case RFLSection.V3D:
            pointer += 16;
            SkipStringList(bytes);
            SkipUntil(bytes, RFLSection.VFX);
            break;

            case RFLSection.VFX:
            pointer += 16;
            SkipStringList(bytes);
            SkipUntil(bytes, RFLSection.LevelProperties);
            break;

            case RFLSection.LevelProperties: ReadLevelProperties(bytes); break;

            case RFLSection.LightMaps:
            fileHasLightMaps = true;
            SkipLightMaps(bytes);
            break;

            case RFLSection.StaticGeometry: ReadGeometry(bytes); break;

            default: Debug.LogError("Encountered unknown section at " + 
                UFUtils.GetHex(pointer) + ": " + nextSection);
            PrintRemainingHeaderPossibilities(bytes, 5000);
            return;
            }
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

    /// <summary>
    /// The light maps section is a straight forward list of images.
    /// Each light map is headed by an id, width and height value
    /// followed by a list of 24-bit RGB values.
    /// These maps are useless to us, since Unity has a superior lighting engine.
    /// Therefore we qiuckly skip trough them.
    /// </summary>
    private void SkipLightMaps(Byte[] bytes) {
        pointer += 8;
        int nbLightMaps = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        
        for(int i = 0; i < nbLightMaps; i++) {
            int width = BitConverter.ToInt32(bytes, pointer);
            int height = BitConverter.ToInt32(bytes, pointer + 4);
            pointer += 8 + (width * height * 3); 
        }

    }

    private void ReadGeometry(Byte[] bytes) {
        pointer += 18;

        int nboTextures = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        level.textures = new string[nboTextures];
        for(int i = 0; i < nboTextures; i++)
            level.textures[i] = UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);

        int nboScrolls = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        if(nboScrolls > 0)
            Debug.LogError("Cannot yet ready files with scrolling textures");

        int nboRooms = BitConverter.ToInt32(bytes, pointer);
        pointer += 4 + (42 * nboRooms);

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
        level.vertices = new Vector3[nboVertices];
        for(int i = 0; i < nboVertices; i++) {
            float x = BitConverter.ToSingle(bytes, pointer);
            float y = BitConverter.ToSingle(bytes, pointer + 4);
            float z = BitConverter.ToSingle(bytes, pointer + 8);
            level.vertices[i] = new Vector3(x, y, z);
            pointer += 12;
        }

        int nboFaces = BitConverter.ToInt32(bytes, pointer);
        pointer += 4;
        level.vertices = new Vector3[nboFaces];
        for(int i = 0; i < nboFaces; i++) {
            UFLevel.UFFace nextFace;
            pointer += 16;
            nextFace.texture = BitConverter.ToInt32(bytes, pointer);
            pointer += 24;

            byte flags = bytes[pointer];
            nextFace.showSky = (flags & (byte)FaceFlags.ShowSky) != 0;
            nextFace.mirrored = (flags & (byte)FaceFlags.Mirrored) != 0;
            nextFace.fullBright = (flags & (byte)FaceFlags.FullBright) != 0;
            pointer += 12;

            int nboFaceVertices = BitConverter.ToInt32(bytes, pointer);
            bool hasExtraCoords = false;
            
            nextFace.vertices = new UFLevel.UFFaceVertex[nboFaceVertices];
            pointer += 4;
            for(int j = 0; j < nboFaceVertices; j++) {
                UFLevel.UFFaceVertex vertex;
                vertex.id = BitConverter.ToInt32(bytes, pointer);
                vertex.u = BitConverter.ToSingle(bytes, pointer + 4);
                vertex.v = BitConverter.ToSingle(bytes, pointer + 8);
                pointer += 12;

                if(j == 0)
                    hasExtraCoords = ProbablyHasExtraCoords(bytes, nboVertices);

                if(hasExtraCoords)
                    pointer += 8;
            }
        }

        int unknownCount2 = BitConverter.ToInt32(bytes, pointer);
        Debug.Log("u2: " + unknownCount2 + " @" + UFUtils.GetHex(pointer));
        pointer += 4 + (unknownCount2 * 96);
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

    private enum FaceFlags {
        ShowSky = 0x01,
        Mirrored = 0x02,
        FullBright = 0x20,
    }

    private void ReadLevelInfo(Byte[] bytes) {
        pointer += 8;

        //level name, author and date
        for(int i = 0; i < 3; i++)
            UFUtils.ReadStringWithLengthHeader(bytes, ref pointer);

        pointer += 1;
        level.multiplayer = BitConverter.ToBoolean(bytes, pointer);
        pointer += 221;
    }





    //--------------------------------------------- RFL reading helper methods ------------------------------------------------

    private void PrintRemainingHeaderPossibilities(byte[] bytes) {
        PrintRemainingHeaderPossibilities(bytes, bytes.Length - 3 - pointer);
    }

    /// <summary>
    /// Temporary method to help decode rfl binary
    /// </summary>
    private void PrintRemainingHeaderPossibilities(byte[] bytes, int limit) {
        List<RFLSection> exclusions = new List<RFLSection>() {
            RFLSection.End, RFLSection.Unkown1, RFLSection.Unkown2,
            RFLSection.TGA, RFLSection.VCM, RFLSection.MVF,
            RFLSection.V3D, RFLSection.VFX, RFLSection.LevelProperties,
            RFLSection.StaticGeometry, RFLSection.LightMaps,
            RFLSection.PlayerStart, RFLSection.LevelInfo
        };
        limit = Mathf.Min(pointer + limit, bytes.Length - 3);

        for(int i = pointer; i < limit; i++) {
            RFLSection section = (RFLSection)BitConverter.ToInt32(bytes, i);
            if(MatchesSectionHeader(bytes, i) && !exclusions.Contains(section))
                Debug.Log(UFUtils.GetHex(i) + " - " + section);
        }
    }

    /// <summary>
    /// Skips pointer trough list of strings
    /// </summary>
    private void SkipStringList(Byte[] bytes) {
        int nonReadableCount = 0;
        int readableCount = 0;

        while(true) {
            pointer++;
            if(UFUtils.IsReadable(bytes[pointer])) {
                readableCount++;
                nonReadableCount = 0;
            }
            else {
                //Found fake string, return
                if(nonReadableCount == 0 && readableCount < 3) {
                    pointer = pointer - readableCount - 1;
                    return;
                }

                readableCount = 0;
                nonReadableCount++;

                //Not finding more strings, return
                if(nonReadableCount > 2) {
                    pointer = pointer - nonReadableCount + 1;
                    return;
                }
            }
        }
    }

    private void SkipUntil(Byte[] bytes, RFLSection header) {
        SkipUntil(bytes, (int)header);
    }

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

    private void SkipUntil(Byte[] bytes, int value) {
        int candidate = BitConverter.ToInt32(bytes, pointer);
        while(candidate != value) {
            pointer++;
            candidate = BitConverter.ToInt32(bytes, pointer);
        }
    }

    private bool MatchesSectionHeader(Byte[] bytes, int pointer) {
        int value = BitConverter.ToInt32(bytes, pointer);
        return Enum.IsDefined(typeof(RFLSection), value);
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

        //unkown order

        GeoRegions = 0x00000200,
        Lights = 0x00000300,
        CutsceneCameras = 0x00000400,
        AmbientSounds = 0x00000500,
        Events = 0x00000600,
        MPRespawns = 0x00000700,
        Particlemitters = 0x00000A00,
        GasRegions = 0x00000B00,
        RoomEffects = 0x00000C00,
        BoltEmitters = 0x00000E00,
        Targets = 0x00000F00,
        Decals = 0x00001000,
        PushRegions = 0x00001100,
        Movers = 0x00002000,
        MovingGroups = 0x00003000,
        CutscenePathNodes = 0x00005000,
        Unkown1 = 0x00006000, // ?????
        EAXEffects = 0x00008000,
        Unkown2 = 0x00010000, // ?????
        NavPoints = 0x00020000,
        Entities = 0x00030000,
        Items = 0x00040000,
        Clutters = 0x00050000,
        Triggers = 0x00060000,
        PlayerStart = 0x00070000,
        LevelInfo = 0x01000000,
        Brushes = 0x02000000,
        Groups = 0x03000000,

        End = 0x00000000,

        
    };

}