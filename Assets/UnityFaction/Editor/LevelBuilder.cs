using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;

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

    //----------------------- graphics ------------------------------

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

        lastRFLPath = UFUtils.GetRelativeUnityPath(rflPath);
        byte[] bytes = File.ReadAllBytes(rflPath);
        level = new UFLevel();

        LevelBuilder builder = (LevelBuilder)EditorWindow.GetWindow(typeof(LevelBuilder));
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

    //--------------------- RFL reading ---------------------------------

    //various info
    private bool signatureCheck;

    //file structure parameters
    private int pointer;
    private int playerStartOffset, levelInfoOffset, sectionsCount;

    private void ReadRFL(Byte[] bytes) {

        //read header
        ReadHeader(bytes);

        //loop over all the sections
        pointer = 32 + level.name.Length;
        for(int i = 0; i < sectionsCount; i++) {
            RFLSection nextSection = (RFLSection)BitConverter.ToInt32(bytes, pointer);
            pointer += 4;
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

        level.name = UFUtils.ReadStringUntilNull(bytes, 30);

        //immediately extract author name from level info
        int authorOffset = levelInfoOffset + level.name.Length + 16;
        level.author = UFUtils.ReadStringUntilNull(bytes, authorOffset);
    }

    private bool MatchesSectionHeader(Byte[] bytes, int pointer) {
        int value = BitConverter.ToInt32(bytes, pointer);
        return Enum.IsDefined(typeof(RFLSection), value);
    }

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
        EAX = 0x00008000,
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