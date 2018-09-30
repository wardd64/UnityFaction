using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class VBMReader {

    /* A VBM is a RedFaction image file that encodes an animated image, like a gif.
     * (Volition BitMap?) The file consists of a short header which details the remaining contents.
     * The contents are a simple series of bitmap images, with r,g,b,a values, encoded in one of 
     * a series of formats.
     */

    [MenuItem("UnityFaction/Convert VBM")]
    public static void ConvertVBM() {

        //let user select vpp folder he would like to unpack
        string fileSearchMessage = "Select VBM file you would like to convert into a material.";
        string defaultPath = "Assets";
        if(!string.IsNullOrEmpty(lastVBMPath))
            defaultPath = Path.GetDirectoryName(lastVBMPath);
        string filePath = EditorUtility.OpenFilePanel(fileSearchMessage, defaultPath, "vbm");
        if(string.IsNullOrEmpty(filePath))
            return;
        if(!UFUtils.IsAssetPath(filePath)) {
            Debug.LogError("Can only convert files that are in an Asset folder. " +
                "Please move the file into your Unity project and try again.");
            return;
        }

        lastVBMPath = filePath;

        VBMReader reader = new VBMReader(filePath);
        reader.MakeMaterialAtAssetPath();
    }

    private static string lastVBMPath;

    //VBM properties
    const uint VBM_SIGNATURE = 0x6D62762E;
    const uint VBM_VERSION = 0x00000001;

    //file structure parameters
    private string assetPath;
    private int pointer;
    private ColorFormat format;
    private int width, height;
    private int nboFrames, nboMipMaps;
    float frameLength;

    //output parameters
    private string fileName;
    private Texture2D texture;

    public VBMReader(string path) {
        byte[] bytes = File.ReadAllBytes(path);
        fileName = Path.GetFileNameWithoutExtension(path);

        string inputDirectory;
        if(UFUtils.IsAssetPath(path))
            inputDirectory = UFUtils.GetRelativeUnityPath(Path.GetDirectoryName(path));
        else
            inputDirectory = Path.GetDirectoryName(path);

        assetPath = inputDirectory + "/" + VPPUnpacker.assetFolder + "/";

        if(!Directory.Exists(assetPath))
            AssetDatabase.CreateFolder(inputDirectory, VPPUnpacker.assetFolder);

        MakeTexture(bytes);
    }

    /// <summary>
    /// makes animated material loaded in this reader and exports it the default asset path.
    /// </summary>
    public void MakeMaterialAtAssetPath() {
        MakeMaterial(assetPath);
    }

    /// <summary>
    /// Makes animated material loaded in this reader and exports it to the export folder.
    /// </summary>
    private void MakeMaterial(string exportFolder) {
        string texAssetPath = exportFolder + fileName + ".png";
        string texPath = Application.dataPath + texAssetPath.Substring(6);
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(texPath, bytes);

        AssetDatabase.Refresh();

        texture = (Texture2D)AssetDatabase.LoadAssetAtPath(texAssetPath, typeof(Texture));

        string matPath = exportFolder + fileName + ".mat";
        Material mat = new Material(Shader.Find("UnityFaction/Animated"));
        mat.mainTexture = texture;
        mat.SetInt("_Cols", nboFrames);
        mat.SetInt("_Rows", 1);
        mat.SetFloat("_Frame", frameLength);
        AssetDatabase.CreateAsset(mat, matPath);
    }

    /// <summary>
    /// Reads data from the file and uses it to generate a new texture.
    /// This texture contains each frame of the animation in paralel, so 
    /// an animation can simply hop over the uv from one frame to the next 
    /// to play it.
    /// </summary>
    private void MakeTexture(byte[] bytes) {
        pointer = 0;
        ReadHeader(bytes);

        texture = new Texture2D(width * nboFrames, height);

        for(int i = 0; i < nboFrames; i++) {
            for(int j = 0; j < (nboMipMaps + 1); j++) {
                if(j == 0) {
                    int nboPixels = width * height;
                    for(int k = 0; k < nboPixels; k++) {
                        ushort raw = BitConverter.ToUInt16(bytes, pointer);
                        int x = i*width + (k % width);
                        int y = k / width;
                        texture.SetPixel(x, y, ReadPixel(raw));
                        pointer += 2;
                    }
                }
                else {
                    int mipFactor = Mathf.RoundToInt(Mathf.Pow(4, j));
                    int nboPixels = width * height / mipFactor;
                    pointer += nboPixels * 2;
                }
            }
        }
    }

    /// <summary>
    /// Converts given 16 bit integer to the Color it encodes.
    /// Several different encodings can be used, and should be 
    /// specified the parameter format.
    /// </summary>
    private Color ReadPixel(ushort raw) {
        byte red, green, blue, alpha;

        switch(format) {

        case ColorFormat.VBM_CF_1555:
        blue = (byte)(raw & 0x0000001F);
        green = (byte)((raw >> 5) & 0x0000001F);
        red = (byte)((raw >> 10) & 0x0000001F);
        alpha = (byte)((raw >> 15) & 0x00000001);
        return new Color(red / 32f, green / 32f, blue / 32f, alpha);

        case ColorFormat.VBM_CF_4444:
        blue = (byte)(raw & 0x0000000F);
        green = (byte)((raw >> 4) & 0x0000000F);
        red = (byte)((raw >> 8) & 0x0000000F);
        alpha = (byte)((raw >> 12) & 0x0000000F);
        return new Color(red / 16f, green / 16f, blue / 16f, alpha / 16f);

        case ColorFormat.VBM_CF_565:
        blue = (byte)(raw & 0x0000001F);
        green = (byte)((raw >> 5) & 0x0000003F);
        red = (byte)((raw >> 11) & 0x0000001F);
        return new Color(red / 5f, green / 6f, blue / 5f);

        default:
        Debug.LogError("Encountered unkown color format: " + format);
        return Color.white;
        }
    }

    /// <summary>
    /// Read header of the VBM file, which contains general data about the encoded texture;
    /// width, height, color coding format, fps, number of frames etc.
    /// </summary>
    private void ReadHeader(byte[] bytes) {
        uint signature = BitConverter.ToUInt32(bytes, pointer);
        uint version = BitConverter.ToUInt32(bytes, pointer + 4);

        if(signature != VBM_SIGNATURE)
            throw new Exception("File did not have Redfaction VBM signature");
        if(version != VBM_VERSION)
            Debug.LogWarning("VBM file had unkown version, trying to parse anyway...");

        width = BitConverter.ToInt32(bytes, pointer + 8);
        height = BitConverter.ToInt32(bytes, pointer + 12);
        format = (ColorFormat)BitConverter.ToInt32(bytes, pointer + 16);
        int fps = BitConverter.ToInt32(bytes, pointer + 20);
        frameLength = 1f / fps;
        nboFrames = BitConverter.ToInt32(bytes, pointer + 24);
        nboMipMaps = BitConverter.ToInt32(bytes, pointer + 28);
        pointer += 32;
    }

    private enum ColorFormat {
        VBM_CF_1555 = 0,
        VBM_CF_4444 = 1,
        VBM_CF_565 = 2,
    };

}
