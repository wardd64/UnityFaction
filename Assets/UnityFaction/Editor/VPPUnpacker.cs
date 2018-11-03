using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;
using System.Collections.Generic;

public class VPPUnpacker {

    /* A VPP file is an archive that holds the level file (.rfl) and any additional music and texturs.
     * The file is split into 2kb (2048 byte) chunks. 
     * 
     * The first chunks contains a signature and the number of files.
     * A few following chunks are dedicated to the names of the files in the .vpp and show how many bytes belong to each file.
     * The remaining chunks consist of the files themselves. 
     * Note that a file always fills it chunks, so it will most likely have a few trailing zeroes to pad out the last one.
     * 
     * This script will read the files in the .vpp and unpack them into a folder, to be used by unity.
     */

    //VPP properties
    const int VPP_SIGNATURE = 0x51890ace; //Standard vpp signature
    const int VPP_WORKING_VERSION = 0x00000001; //only version 1 will work!
    const int VPP_MAX_ENTRIES = 65536; //limit to number of files in vpp
    const int BYTES_IN_STRING = 60; //Every string is allowed 60 characters
    const int BLOCK_SIZE = 0x0800; //vpp file is split in 2kB chunks

    private static string lastVPPPath;

    [MenuItem("UnityFaction/Unpack VPP")]
    public static void UnpackVPP() {

        if(string.IsNullOrEmpty(lastVPPPath))
            lastVPPPath = "Assets";

        //let user select vpp folder he would like to unpack
        string fileSearchMessage = "Select vpp file you would like to unpack";
        string vppPath = EditorUtility.OpenFilePanel(fileSearchMessage, lastVPPPath, "vpp");
        if(string.IsNullOrEmpty(vppPath))
            return;

        string vppName = Path.GetFileNameWithoutExtension(vppPath);
        lastVPPPath = Path.GetDirectoryName(UFUtils.GetRelativeUnityPath(vppPath));

        //let user select folder to unpack the vpp to
        fileSearchMessage = "Select folder where you would like to unpack " + vppName + ".vpp";
        string exportFolder = EditorUtility.OpenFolderPanel(fileSearchMessage, lastVPPPath, vppName);
        if(string.IsNullOrEmpty(exportFolder))
            return;

        AlertProblem(UFUtils.IsAssetPath(exportFolder), "Can only export files into an Asset folder!");

        ReadFile(vppPath, exportFolder);

        Debug.Log("Content of " + Path.GetFileName(vppPath) + " has been unpacked succesfully!");
        AssetDatabase.Refresh();
    }

    [MenuItem("UnityFaction/AutoUnpack")]
    public static void UnpackMultiVPP() {
        // let user select folder to unpack the vpp to
        string fileSearchMessage = "Select folder in which to look for vpp files to unpack";
        string importFolder = EditorUtility.OpenFolderPanel(fileSearchMessage, "Assets", "");
        if(string.IsNullOrEmpty(importFolder))
            return;

        string[] files = Directory.GetFiles(importFolder);
        foreach(string file in files) {
            if(Path.GetExtension(file).ToLower() == ".vpp") {
                string vppName = Path.GetFileNameWithoutExtension(file);
                string exportFolder = importFolder + "/" + vppName;
                if(!Directory.Exists(exportFolder)) {
                    string relImpPath = UFUtils.GetRelativeUnityPath(importFolder);
                    AssetDatabase.CreateFolder(relImpPath, vppName);
                }
                ReadFile(file, exportFolder);
            }
        }
    }

    [MenuItem("UnityFaction/Import RF Source")]
    public static void ReadSource() {
        //let user select source folder with all the vpp goodies
        string fileSearchMessage = "Select RF source folder (contains .exe and multiple VPP files)";
        string importFolder = EditorUtility.OpenFolderPanel(fileSearchMessage, "..", "");
        if(string.IsNullOrEmpty(importFolder))
            return;

        List<string> vppPaths = new List<string>();
        DirectoryInfo info = new DirectoryInfo(importFolder);
        FileInfo[] fileInfo = info.GetFiles();
        foreach(FileInfo file in fileInfo) {
            if(file.Extension == ".vpp")
                vppPaths.Add(file.ToString());
        }

        AlertProblem(vppPaths.Count > 0, "Selected folder did not contain any .vpp files. Please select proper source folder!");

        string sourcePath = ufPath + "/" + sourceFolder;
        string assetPath = sourcePath + "/" + assetFolder;
        if(!Directory.Exists(sourcePath))
            AssetDatabase.CreateFolder(ufPath, sourceFolder);
        if(!Directory.Exists(assetPath))
            AssetDatabase.CreateFolder(sourcePath, assetFolder);

        foreach(string vppPath in vppPaths)
            ReadFile(vppPath, GetRFSourcePath());

        Debug.Log("All contents have been moved succesfully!");
        AssetDatabase.Refresh();
    }

    private const string ufPath = "Assets/UnityFaction";
    private const string sourceFolder = "RFSource";
    public const string assetFolder = "_UFAssets";

    public static string GetRFSourcePath() {
        return ufPath + "/" + sourceFolder;
    }

    private static void ReadFile(string vppPath, string exportFolder){
        byte[] bytes = File.ReadAllBytes(vppPath);
        SplitIntoSubFiles(bytes, exportFolder);
    }

    private static void SplitIntoSubFiles(byte[] bytes, string exportFolder) {

        //VPP files have specific signature, check if it is correct!
        int signature = BitConverter.ToInt32(bytes, 0);
        AlertProblem(signature == VPP_SIGNATURE, 
            "File did not have valid signature.");

        //Check if working with version 1
        int version = BitConverter.ToInt32(bytes, 4);
        AlertProblem(version == VPP_WORKING_VERSION, 
            "Can only process vpp files of version 1. Version was " + version);

        //exctract size data
        int nFiles = BitConverter.ToInt32(bytes, 8);
        int fileSize = BitConverter.ToInt32(bytes, 12);

        //We got all the info from the header, move on with file list
        int pointer = BLOCK_SIZE;
        int offset = RoundToBlockSize(pointer + 64 * nFiles);

        //iterate over all the files in the VPP and export their contents
        for(int i = 0; i < nFiles; i++) {

            string subFileName = Encoding.UTF8.GetString(bytes, pointer, BYTES_IN_STRING);
            pointer += BYTES_IN_STRING;

            int subFileSize = BitConverter.ToInt32(bytes, pointer);
            pointer += 4;

            ExportFile(subFileName.Trim('\0'), exportFolder, bytes, offset, subFileSize);
            offset += RoundToBlockSize(subFileSize);
        }

        AlertProblem(fileSize == RoundToBlockSize(offset), 
            "Actual file size did not match reported size. File may be corrupted.");
    }

    private static void ExportFile(string name, string exportFolder, byte[] bytes, int start, int length) {
        Byte[] fileBytes = new Byte[length];
        for(int i = 0; i < length; i++)
            fileBytes[i] = bytes[start + i];
        string exportPath = exportFolder + '/' + name;
        try {
            File.WriteAllBytes(exportPath, fileBytes);
        }
        catch(Exception e) {
            Debug.LogError("Failed to write to " + exportPath + "\n" + e);
        }

        //handle extentions that require further processing 
        string ext = Path.GetExtension(name).TrimStart('.').ToLower();
        switch(ext) {
        case "v3d": case "v3m": case "v3c":
        V3DReader modelReader = new V3DReader(exportPath);
        modelReader.MakePrefabAtAssetPath();
        break;

        case "vbm":
        VBMReader texReader = new VBMReader(exportPath);
        texReader.MakeMaterialAtAssetPath();
        break;

        case "wav":
        new WavRepairer(exportPath);
        break;
        }
    }

    /// <summary>
    /// Round x to nearest multiple of BLOCK_SIZE, greater than it
    /// </summary>
    private static int RoundToBlockSize(int x) {
        if(x % BLOCK_SIZE == 0)
            return x;
        x -= x % BLOCK_SIZE;
        x += BLOCK_SIZE;
        return x;
    }

    /// <summary>
    /// Throw an exception if given check fails. Unity exception handling will show 
    /// the given problem to the user.
    /// </summary>
    private static void AlertProblem(bool confirmationCheck, string problem) {
        if(!confirmationCheck)
            throw new Exception(problem);
    }

}

