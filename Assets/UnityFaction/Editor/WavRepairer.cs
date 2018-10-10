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
    private string filePath;
    private static string lastWavPath;

    //data
    uint nboSamples, sampleRate;
    double[] left, right;

    //[MenuItem("UnityFaction/Repair wav")]
    public static void RepairWav() {

        //let user select vpp folder he would like to unpack
        string fileSearchMessage = "Select wav file that needs repairing.";
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

        WavRepairer repairer = new WavRepairer(filePath);
        repairer.Repair();
    }

    public WavRepairer(string path) {
        byte[] bytes = File.ReadAllBytes(path);
        filePath = path;
        ReadWav(bytes);
    }

    public void Repair() {
        string outputFile = filePath.Replace(".wav", "_RepairTest.wav");
        FileStream f = new FileStream(outputFile, FileMode.Create);
        BinaryWriter wr = new BinaryWriter(f);
        WriteWav(wr);
        wr.Close();
        f.Close();
        AssetDatabase.Refresh();
    }

    private void ReadWav(byte[] bytes) {
        int pos = 12;
        ushort channels = BitConverter.ToUInt16(bytes, 22);
        sampleRate = BitConverter.ToUInt32(bytes, 24);

        //this type of wav file has 4-bit sample values
        nboSamples = (BitConverter.ToUInt32(bytes, 4) - 36) * 2 / channels;

        // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
        while(!(bytes[pos] == 100 && bytes[pos + 1] == 97 && bytes[pos + 2] == 116 && bytes[pos + 3] == 97)) {
            pos += 4;
            int chunkSize = bytes[pos] + bytes[pos + 1] * 256 + bytes[pos + 2] * 65536 + bytes[pos + 3] * 16777216;
            pos += 4 + chunkSize;
        }
        pos += 8;

        // Pos is now positioned to start of actual sound data.

        // Allocate memory (right will be null if only mono sound)
        left = new double[nboSamples];
        if(channels == 2)
            right = new double[nboSamples];
        else
            right = null;


        // Write to double array/s:
        int i = 0;
        while(pos < bytes.Length) {
            bool firstNibble = i % 2 == 0;
            bool incrementPos = !firstNibble;

            byte rightNibble = (byte)(bytes[pos] & 0x0F);
            byte leftNibble = (byte)((bytes[pos] & 0xF0) >> 4);

            left[i] = GetSample(firstNibble ? leftNibble : rightNibble);
            if(channels == 2) {
                incrementPos = true;
                right[i] = GetSample(rightNibble);
            }

            i++;
            if(incrementPos)
                pos++;
        }
    }

    private void WriteWav(BinaryWriter wr) {
        ushort nboChannels = (ushort)(right == null ? 1 : 2);
        ushort samplelength = 2;

        wr.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); //0
        wr.Write(36 + nboSamples * nboChannels * samplelength); //4
        wr.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); //8
        wr.Write(16); //16
        wr.Write((ushort)1); //20
        wr.Write(nboChannels); //22
        wr.Write(sampleRate); //24
        wr.Write(sampleRate * samplelength * nboChannels); //28
        wr.Write((ushort)(samplelength * nboChannels)); //32
        wr.Write((ushort)(8 * samplelength)); //34
        wr.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        wr.Write(nboSamples * samplelength);

        for(uint i = 0; i < nboSamples; i++) {
            wr.Write(GetSample(left[i]));
            if(right != null)
                wr.Write(GetSample(right[i]));
        }
    }

    private short GetSample(double value) {
        return (short)(short.MaxValue * value);
    }

    private double GetSample(byte nibble) {
        //TODO propper decoding
        return (nibble - 8) / 8.0;
    }
}
