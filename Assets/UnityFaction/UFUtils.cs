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
    /// </summary>
    public static string ReadStringUntilNull(byte[] bytes, int start) {
        StringBuilder toReturn = new StringBuilder();
        char nextChar = Encoding.UTF8.GetString(bytes, start, 1).ToCharArray()[0];
        while(nextChar != '\0') {
            toReturn.Append(nextChar);
            nextChar = Encoding.UTF8.GetString(bytes, ++start, 1).ToCharArray()[0];
        }
        return toReturn.ToString();
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
