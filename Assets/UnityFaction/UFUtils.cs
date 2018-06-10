using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System;

public class UFUtils {

    /// <summary>
    /// True if given string is an absolute path that leads into the assets folder.
    /// </summary>
    public static bool IsAssetPath(string path) {
        return path.StartsWith(Application.dataPath);
    }

    /// <summary>
    /// Converts given absolute path to a relative path, starting with Assets/
    /// </summary>
    public static string GetRelativeUnityPath(string path) {
        return "Assets" + path.Substring(Application.dataPath.Length);
    }

    /// <summary>
    /// Starts reading string encoded in the given bytes until we hit a null byte.
    /// Also moves the pointer passed this null terminator
    /// </summary>
    public static string ReadNullTerminatedString(byte[] bytes, ref int pointer) {
        StringBuilder toReturn = new StringBuilder();
        char nextChar = Encoding.UTF8.GetString(bytes, pointer++, 1).ToCharArray()[0];
        while(nextChar != '\0') {
            toReturn.Append(nextChar);
            nextChar = Encoding.UTF8.GetString(bytes, pointer++, 1).ToCharArray()[0];
        }
        return toReturn.ToString();
    }

    /// <summary>
    /// Starts reading string encoded in the given bytes until we hit a null byte.
    /// </summary>
    public static string ReadNullTerminatedString(byte[] bytes, int pointer) {
        return ReadNullTerminatedString(bytes, ref pointer);
    }

    /// <summary>
    /// Reads and returns string with 2 byte number denoting its length.
    /// Moves the pointer to the first byte after the string.
    /// </summary>
    public static string ReadStringWithLengthHeader(byte[] bytes, ref int pointer) {
        short length = BitConverter.ToInt16(bytes, pointer);
        pointer += 2;
        string toReturn = Encoding.UTF8.GetString(bytes, pointer, length);
        pointer += length;
        return toReturn;
    }

    public static Color GetRGBColor(byte[] bytes, int start) {
        float f = 1f / 255;
        float r = f * bytes[start];
        float g = f * bytes[start + 1];
        float b = f * bytes[start + 2];
        return new Color(r, g, b);
    }

    public static Color GetRGBAColor(byte[] bytes, int start) {
        float f = 1f / 255;
        float r = f * bytes[start];
        float g = f * bytes[start + 1];
        float b = f * bytes[start + 2];
        float a = f * bytes[start + 3];
        return new Color(r, g, b, a);
    }

    public static bool IsPlausibleIndex(byte[] bytes, int start, int max) {
        int value = BitConverter.ToInt32(bytes, start);
        return value >= 0 && value < max;
    }

    public static bool IsPlausibleFloat(byte[] bytes, int start) {
        float value = Mathf.Abs(BitConverter.ToSingle(bytes, start));
        return value == 0f || (value > 1e-10f && value < 1e+10f);
    }

    public static string GetHex(int pointer) {
        return pointer.ToString("X");
    }

    public static string GetHex(byte[] bytes, int start, int length) {
        StringBuilder toReturn = new StringBuilder();
        for(int i = 0; i < length; i++)
            toReturn.Append(BitConverter.ToString(bytes, start + i, 1));
        return toReturn.ToString();
    }

    /// <summary>
    /// Returns true if given byte encodes a readable character
    /// </summary>
    public static bool IsReadable(Byte b) {
        return b >= 32;
    }
}
