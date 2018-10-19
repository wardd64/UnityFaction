using NAudio.Wave;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class WavRepairer {

    /* Redfaction source material and maps tend to hold a format of wav file 
     * that Unity is not familiar with, which is why this repairer class is 
     * necessary.
     */

    //output parameters
    private static string lastWavPath;

    [MenuItem("UnityFaction/Repair wav")]
    public static void RepairWav() {

        //let user select vpp folder he would like to unpack
        string fileSearchMessage = "Select wav file that needs repairing";
        string defaultPath = "Assets";
        if(!string.IsNullOrEmpty(lastWavPath))
            defaultPath = Path.GetDirectoryName(lastWavPath);
        string filePath = EditorUtility.OpenFilePanel(fileSearchMessage, defaultPath, "wav");
        if(string.IsNullOrEmpty(filePath))
            return;
        if(!UFUtils.IsAssetPath(filePath)) {
            Debug.LogError("Can only convert files that are in an Asset folder. " +
                "Please move the file into your Unity project and try again.");
            return;
        }

        lastWavPath = filePath;

        new WavRepairer(filePath);
    }

    public WavRepairer(string path) {

        if(!UFUtils.IsAssetPath(path))
            path = UFUtils.GetAbsoluteUnityPath(path);

        if(!path.Contains(".wav")) {
            Debug.LogError("Cannot apply wav repair to non wav files: " + path);
            return;
        }

        string encodingInfo = "";
        string backUpPath = path.Replace(".wav", "_backup.wav");
        string tempPath = path.Replace(".wav", "_temp.wav");
        File.Copy(path, backUpPath, true);

        try {
            WaveFileReader reader = new WaveFileReader(path);
            WaveFormat inputFormat = reader.WaveFormat;
            encodingInfo += inputFormat.Encoding;

            int sampleRate = inputFormat.SampleRate;
            int bits = 16;
            int channels = inputFormat.Channels;

            WaveFormat outputFormat = new WaveFormat(sampleRate, bits, channels);
            encodingInfo += " -> " + outputFormat;
            WaveStream convertedStream = new WaveFormatConversionStream(outputFormat, reader);

            WaveFileWriter.CreateWaveFile(tempPath, convertedStream);
            convertedStream.Close();

            File.Copy(tempPath, path, true);
        }
        catch(Exception e) {
            Debug.LogError("Could not repair audio file " + Path.GetFileNameWithoutExtension(path) +
                " automatically, please repair it manually using an audio editor.\n" + 
                "Additional encoding info: " + encodingInfo + ", and source exception:\n" + e);

            File.Copy(backUpPath, path, true);
        }

        File.Delete(tempPath);
        File.Delete(backUpPath);

        AssetDatabase.Refresh();
    }
}
