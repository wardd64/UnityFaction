using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;

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

    [MenuItem("UnityFaction/Unpack VPP")]
    public static void UnpackVPP() {

        //let user select vpp folder he would like to unpack
        string fileSearchMessage = "Select vpp file you would like to unpack";
        string vppPath = EditorUtility.OpenFilePanel(fileSearchMessage, "Assets", "vpp");
        if(string.IsNullOrEmpty(vppPath))
            return;

        string vppName = Path.GetFileNameWithoutExtension(vppPath);

        //let user select folder to unpack the vpp to
        fileSearchMessage = "Select folder where you would like to unpack " + vppName + ".vpp";
        string exportFolder = EditorUtility.OpenFolderPanel(fileSearchMessage, "Assets", vppName);
        if(string.IsNullOrEmpty(exportFolder))
            return;

        AlertProblem(UFUtils.IsAssetPath(exportFolder), "Can only export files into an Asset folder!");

        TryReadFile(vppPath, exportFolder);

        Debug.Log("Unpacked successfully! Refresh the assets folder (Ctrl-R) to see its contents.");
    }

    private static void TryReadFile(string vppPath, string exportFolder){
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
        File.WriteAllBytes(exportFolder + '/' + name, fileBytes);
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

