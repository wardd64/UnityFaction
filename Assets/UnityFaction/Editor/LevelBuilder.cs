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

            case RFLSection.TGA:  
            pointer += 20;
            SkipStringList(bytes);
            break;

            case RFLSection.VCM:
            pointer += 16;
            SkipStringList(bytes);
            pointer += 4;
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

            case RFLSection.LevelProperties:
            PrintRemainingHeaderPossibilities(bytes, 50000);
            ReadLevelProperties(bytes);
            
            break;

            case RFLSection.StaticGeometry:

            
            break;

            

            default: Debug.LogError("Encountered unknown section at " + 
                UFUtils.GetHex(pointer) + ": " + UFUtils.GetHex(bytes, pointer, 4)); 
            return;
            }
        }
    }

    //--------------------------------------------- RFL reading helper methods ------------------------------------------------

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

        level.name = UFUtils.ReadStringUntilNull(bytes, 30);

        //immediately extract author name from level info
        int authorOffset = levelInfoOffset + level.name.Length + 16;
        level.author = UFUtils.ReadStringUntilNull(bytes, authorOffset);
    }

    private void ReadLevelProperties(Byte[] bytes) {
        pointer += 10;
        string defaultTexture = UFUtils.ReadStringUntilNull(bytes, pointer);
        defaultTexture.Remove(defaultTexture.Length - 1);
        level.defaultTexture = defaultTexture;

        //...
    }

    private void PrintRemainingHeaderPossibilities(byte[] bytes) {
        PrintRemainingHeaderPossibilities(bytes, int.MaxValue);
    }

    private void PrintRemainingHeaderPossibilities(byte[] bytes, int limit) {
        List<RFLSection> exclusions = new List<RFLSection>() {
            RFLSection.End, RFLSection.Unkown1, RFLSection.Unkown2,
            RFLSection.TGA, RFLSection.VCM, RFLSection.MVF,
            RFLSection.V3D, RFLSection.VFX, RFLSection.LevelProperties
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
        //correct order
        TGA = 0x00007000,
        VCM = 0x00007001,
        MVF = 0x00007002,
        V3D = 0x00007003,
        VFX = 0x00007004,
        LevelProperties = 0x00000900,


        //unkown order
        StaticGeometry = 0x00000100,
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
        LightMaps = 0x00001200,
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