using System.Text;
using UnityEngine;
using System;
using UFLevelStructure;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
    /// Converts given relative path (inside assets folder) to an absolute path
    /// </summary>
    public static string GetAbsoluteUnityPath(string path) {
        if(path.StartsWith("Assets"))
            return Application.dataPath + path.Substring(6);
        return Application.dataPath + "/" + path;
    }

    /* -----------------------------------------------------------------------------------------------
     * -------------------------------------- BINARY READING -----------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

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
    public static string ReadRFLString(byte[] bytes, ref int pointer) {
        short length = BitConverter.ToInt16(bytes, pointer);
        pointer += 2;
        string toReturn = Encoding.UTF8.GetString(bytes, pointer, length);
        pointer += length;
        return toReturn;
    }

    /// <summary>
    /// Reads 3 bytes and returns the color they would encode (with max alpha)
    /// </summary>
    public static Color GetRGBColor(byte[] bytes, int start) {
        float f = 1f / 255;
        float r = f * bytes[start];
        float g = f * bytes[start + 1];
        float b = f * bytes[start + 2];
        return new Color(r, g, b);
    }

    /// <summary>
    /// Reads 4 bytes and returns the color they would encode (including alpha value)
    /// </summary>
    public static Color GetRGBAColor(byte[] bytes, int start) {
        float f = 1f / 255;
        float r = f * bytes[start];
        float g = f * bytes[start + 1];
        float b = f * bytes[start + 2];
        float a = f * bytes[start + 3];
        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Reads 3 consecutive floats and returns them inside a vector.
    /// </summary>
    public static Vector3 Getvector3(byte[] bytes, int start) {
        float x = BitConverter.ToSingle(bytes, start);
        float y = BitConverter.ToSingle(bytes, start + 4);
        float z = BitConverter.ToSingle(bytes, start + 8);
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Reads 2 consecutive floats and returns them inside a vector.
    /// </summary>
    public static Vector3 Getvector2(byte[] bytes, int start) {
        float x = BitConverter.ToSingle(bytes, start);
        float y = BitConverter.ToSingle(bytes, start + 4);
        return new Vector2(x, y);
    }

    /// <summary>
    /// Read 4 consecutive floats and returns them inside a Quaternion
    /// </summary>
    public static Quaternion GetQuaternion(byte[] bytes, int start) {
        float x = BitConverter.ToSingle(bytes, start);
        float y = BitConverter.ToSingle(bytes, start + 4);
        float z = BitConverter.ToSingle(bytes, start + 8);
        float w = BitConverter.ToSingle(bytes, start + 12);
        return new Quaternion(x, y, z, w);
    }

    /// <summary>
    /// Rotations are stored as 3x3 matrices of floats, so they consist of 4x3x3=36 bytes.
    /// the entries of this matrix are stored in an awkard order, so make sure to refer to this method.
    /// </summary>
    public static Quaternion GetRotation(byte[] bytes, int start) {
        /* RFL matrix  ->  Unity matrix
         *     x y z       A B C
         *  1: a b c       d g a :1
         *  2: d e f   ->  e h b :2
         *  3: g h i       f i c :3
         */

        Vector3 row1 = Getvector3(bytes, start);
        Vector3 row2 = Getvector3(bytes, start + 12);
        Vector3 row3 = Getvector3(bytes, start + 24);

        Vector4 col1 = new Vector4(row2.x, row2.y, row2.z);
        Vector4 col2 = new Vector4(row3.x, row3.y, row3.z);
        Vector4 col3 = new Vector4(row1.x, row1.y, row1.z);
        Vector4 col4 = new Vector4(0f, 0f, 0f, 1f);

        Matrix4x4 mat = new Matrix4x4(col1, col2, col3, col4);
        return mat.rotation;
    }

    /// <summary>
    /// Reads position followed by rotation: 48 bytes total
    /// </summary>
    public static PosRot GetPosRot(byte[] bytes, int start) {
        Vector3 position = Getvector3(bytes, start);
        Quaternion rotation = GetRotation(bytes, start + 12);
        return new PosRot(position, rotation);
    }

    /// <summary>
    /// True if the given bytes can be reasonably expected to encode
    /// an index for an array with the size 'max'
    /// </summary>
    public static bool IsPlausibleIndex(byte[] bytes, int start, int max) {
        int value = BitConverter.ToInt32(bytes, start);
        return value >= 0 && value < max;
    }

    /// <summary>
    /// True if the given bytes can be reasonably expected to encode
    /// a floating point value such as a coordinate or matrix element.
    /// </summary>
    public static bool IsPlausibleFloat(byte[] bytes, int start) {
        float value = Mathf.Abs(BitConverter.ToSingle(bytes, start));
        return value == 0f || (value > 1e-10f && value < 1e+10f);
    }

    /// <summary>
    /// Return hexadecimal form of the given number
    /// </summary>
    public static string GetHex(int value) {
        return value.ToString("X");
    }

    /// <summary>
    /// Return hexadecimal form of the given number
    /// </summary>
    public static string GetHex(float value) {
        return value.ToString("X");
    }

    /// <summary>
    /// Return given byte as a string containing its binary form
    /// </summary>
    public static string GetBinary(byte[] bytes, int pointer) {
        return Convert.ToString(bytes[pointer], 2).PadLeft(8, '0');
    }

    /// <summary>
    /// Return given length of bytes as a string containg hexadecimal
    /// </summary>
    public static string GetHex(byte[] bytes, int start, int length) {
        StringBuilder toReturn = new StringBuilder();
        for(int i = 0; i < length; i++)
            toReturn.Append(BitConverter.ToString(bytes, start + i, 1));
        return toReturn.ToString();
    }

    /// <summary>
    /// Return value of a specific bit
    /// </summary>
    public static bool GetFlag(byte[] bytes, int pointer, int bit) {
        byte flags = bytes[pointer];
        byte match = (byte)(1 << bit);
        return (flags & match) != 0;
    }

    /// <summary>
    /// Return 4-bit integer encoded as a half of a byte
    /// </summary>
    public static byte GetNibble(byte[] bytes, int pointer, bool first) {
        byte value = bytes[pointer];

        if(first)
            return (byte)(value % 16);
        else
            return (byte)(value / 16);
    }

    /// <summary>
    /// Returns true if given byte encodes a readable character
    /// </summary>
    public static bool IsReadable(Byte b) {
        return b >= 32;
    }

    /// <summary>
    /// Returns true only if all bytes in the given range encode reabable characters
    /// </summary>
    public static bool IsReadable(byte[] bytes, int start, int length) {
        for(int i = start; i < start + length; i++) {
            if(!IsReadable(bytes[i]))
                return false;
        }
        return true;
    }

    /* -----------------------------------------------------------------------------------------------
     * ------------------------------------------ OTHERS ---------------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    /// <summary>
    /// Returns number of "true" values in the given array
    /// </summary>
    public static int Count(bool[] values) {
        int toReturn = 0;
        for(int i = 0; i < values.Length; i++)
            if(values[i])
                toReturn++;
        return toReturn;
    }

    /// <summary>
    /// return an element of the given array at random.
    /// </summary>
    public static T GetRandom<T>(T[] list) {
        int index = UnityEngine.Random.Range(0, list.Length);
        return list[index];
    }

    /// <summary>
    /// return an element of the given list at random.
    /// </summary>
    public static T GetRandom<T>(List<T> list) {
        int index = UnityEngine.Random.Range(0, list.Count);
        return list[index];
    }

    /// <summary>
    /// Returns lerp factor needed to perform an exponential decay time step, 
    /// using the given half life time and Time.deltaTime.
    /// Use as follows: value = Lerp(value, goalValue, factor);
    /// </summary>
    public static float LerpExpFactor(float halfLife) {
        return LerpExpFactor(halfLife, Time.deltaTime);
    }

    /// <summary>
    /// Returns lerp factor needed to perform an exponential decay time step, 
    /// using the given half life time and time step.
    /// Use as follows: value = Lerp(value, goalValue, factor);
    /// </summary>
    public static float LerpExpFactor(float halfLife, float timeStep) {
        if(halfLife <= 0f)
            return 1f;
        else if(float.IsNaN(halfLife) || float.IsPositiveInfinity(halfLife))
            return 0f;

        float a = timeStep * Mathf.Log(2f) / (2 * halfLife);
        return 2 * a / (1 + a);
    }

    /// <summary>
    /// Moves color from start to target, by at most the given amount of delta.
    /// if delta is 1; it takes 1 second to move from say; black to red.
    /// </summary>
    public static Color MoveTowards(Color startColor, Color targetColor, float delta) {
        Vector4 vecStart = new Vector4(startColor.r, startColor.g, startColor.b, startColor.a);
        Vector4 vecTarget = new Vector4(targetColor.r, targetColor.g, targetColor.b, targetColor.a);
        Vector4 vecReturn = Vector4.MoveTowards(vecStart, vecTarget, delta);
        return new Color(vecReturn.x, vecReturn.y, vecReturn.z, vecReturn.w);
    }

    /// <summary>
    /// Returns true if the given point lies in a box or sphere with the given parameters.
    /// </summary>
    public static bool Inside(Vector3 point, PosRot center, float radius, Vector3 extents, bool box) {
        if(box)
            return InsideBox(point, new CenteredBox(center, extents));
        else
            return InsideSphere(point, center.position, radius);
    }

    /// <summary>
    /// Returns true if the given points lies in the given sphere.
    /// </summary>
    public static bool InsideSphere(Vector3 point, Vector3 center, float radius) {
        return (point - center).sqrMagnitude < radius * radius;
    }

    /// <summary>
    /// Returns true if the given points list in the given box.
    /// </summary>
    public static bool InsideBox(Vector3 point, CenteredBox cb) {
        Vector3 relativePoint = point - cb.transform.posRot.position;
        Quaternion rot = Quaternion.Inverse(cb.transform.posRot.rotation);
        relativePoint = rot * relativePoint;

        bool inside = true;
        inside &= Mathf.Abs(relativePoint.x) < cb.extents.x / 2f;
        inside &= Mathf.Abs(relativePoint.y) < cb.extents.y / 2f;
        inside &= Mathf.Abs(relativePoint.z) < cb.extents.z / 2f;

        return inside;
    }

    /// <summary>
    /// Resets the local transform parameters to their default values.
    /// </summary>
    public static void LocalReset(Transform transform) {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Sets the given Unity transform to correspond to the given UnityFaction transform.
    /// Affects global transforms, not local ones.
    /// </summary>
    public static void SetTransform(Transform transform, UFTransform ufTransform) {
        SetTransform(transform, ufTransform.posRot);
    }

    /// <summary>
    /// Sets the given Unity transform to correspond to the given position and rotation.
    /// Affects global transforms, not local ones.
    /// </summary>
    public static void SetTransform(Transform transform, PosRot pr) {
        transform.position = pr.position;
        transform.rotation = pr.rotation;
    }

    /// <summary>
    /// Returns position of the given point after rotating it around the pivot with the given rotation.
    /// </summary>
    public static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation) {
        Vector3 dir = point - pivot;
        Vector3 rotatedDir = rotation * dir;
        return pivot + rotatedDir;
    }

    /// <summary>
    /// returns true if the given array contains any hits with solid terrain
    /// </summary>
    public static bool collidesWithTerrain(RaycastHit[] hits, out RaycastHit actualHit) {
        foreach(RaycastHit hit in hits) {
            actualHit = hit;

            //triggers
            if(hit.collider.isTrigger)
                continue;

            //characters (including player)
            if(hit.collider.GetComponent<CharacterController>())
                continue;

            //non kinematic rigid bodies
            Rigidbody rb = hit.collider.transform.GetComponentInParent<Rigidbody>();
            if(rb != null && !rb.isKinematic)
                continue;

            return true;
        }
        actualHit = new RaycastHit();
        return false;
    }

    /// <summary>
	/// returns true if the given array contains any hits with solid terrain
	/// </summary>
	public static bool collidesWithTerrain(RaycastHit[] hits) {
        RaycastHit hit;
        return collidesWithTerrain(hits, out hit);
    }

    /// <summary>
    /// Returns a simple color gradient that progresses from the given startColor to
    /// the given endColor. This effect includes alpha values.
    /// </summary>
    public static Gradient GetLinearGradient(Color startColor, Color endColor) {
        Gradient toReturn = new Gradient();
        GradientAlphaKey startAlpha = new GradientAlphaKey(startColor.a, 0f);
        GradientAlphaKey endAlpha = new GradientAlphaKey(startColor.a, 1f);
        GradientColorKey startKey = new GradientColorKey(startColor, 0f);
        GradientColorKey endKey = new GradientColorKey(endColor, 1f);
        toReturn.alphaKeys = new GradientAlphaKey[] { startAlpha, endAlpha };
        toReturn.colorKeys = new GradientColorKey[] { startKey, endKey };
        return toReturn;
    }

    /// <summary>
    /// Copies component values of other to the component comp.
    /// </summary>
    public static T GetCopyOf<T>(Component comp, T other) where T : Component {
        Type type = comp.GetType();
        if(type != other.GetType())
            return null; // type mis-match
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        PropertyInfo[] pinfos = type.GetProperties(flags);
        foreach(var pinfo in pinfos) {
            if(pinfo.CanWrite) {
                try {
                    pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                }
                catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }
        }
        FieldInfo[] finfos = type.GetFields(flags);
        foreach(var finfo in finfos) {
            finfo.SetValue(comp, finfo.GetValue(other));
        }
        return comp as T;
    }

    /// <summary>
    /// Adds a copy of the component toAdd to the gameObject go.
    /// The copy will be a seperate component of the same type with the same parameters set.
    /// </summary>
    public static T AddComponent<T>(GameObject go, T toAdd) where T : Component {
        go.AddComponent<T>();
        return GetCopyOf(go.GetComponent<T>(), toAdd);
    }

    /// <summary>
    /// Returns given string set to title case.
    /// If this is a text string without spaces, the first letter will be capitalized.
    /// </summary>
    public static string Capitalize(string input) {
        CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
        TextInfo textInfo = cultureInfo.TextInfo;
        return textInfo.ToTitleCase(input);
    }

    /// <summary>
    /// Returns a centered rectangle mesh with the given x, y extents.
    /// </summary>
    public static Mesh MakeQuad(Vector3 extents) {
        float width = extents.x;
        float height = extents.y;
        return MakeQuad(width, height);
    }

    /// <summary>
    /// Returns a centered rectangle mesh (a quad) with the given width and height.
    /// </summary>
    public static Mesh MakeQuad(float width, float height) {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4];
        float x = width / 2f;
        float y = height / 2f;
        vertices[0] = new Vector3(-x, -y, 0);
        vertices[1] = new Vector3(x, -y, 0);
        vertices[2] = new Vector3(-x, y, 0);
        vertices[3] = new Vector3(x, y, 0);

        mesh.vertices = vertices;
        int[] tri = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.triangles = tri;

        Vector3[] normals = new Vector3[4];
        for(int i = 0; i < 4; i++)
            normals[i] = -Vector3.forward;
        mesh.normals = normals;

        Vector2[] uv = new Vector2[4];
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);
        mesh.uv = uv;

        return mesh;
    }

    /// <summary>
    /// Returns a simple string that roughly encodes the given vector.
    /// </summary>
    public static string GetVecStr(Vector2 value) {
        return value.x.ToString("n2") + "_" + value.y.ToString("n2");
    }

    /// <summary>
    /// Converts the given rotation (delta) to an angleAxis format.
    /// The given vector will point in the right-hand-rule direction of the rotation,
    /// and have magnitude corresponding the the angle covered.
    /// </summary>
    public static Vector3 GetAxis(Quaternion q) {
        Vector3 v = Vector3.forward;
        float angle = GetQuatAngle(q);
        return angle * Vector3.Cross(v, q*v).normalized;
    }

    /// <summary>
    /// Returns the smallest angle covered by the given quaternion, 
    /// relative to the identity quaternion. This method has improved accuracy 
    /// when dealing with small angles, when compared the the Unity standard method.
    /// </summary>
    public static float GetQuatAngle(Quaternion q) {
        float angle = Quaternion.Angle(Quaternion.identity, q);

        if(angle < 45f) {
            Vector3 v = Vector3.forward;
            float sinRad = Vector3.Cross(v, q * v).magnitude;
            return Mathf.Asin(sinRad) * Mathf.Rad2Deg;
        }
        else
            return angle;
    }

    /// <summary>
    /// Returns string that represents given floating point value as well as possible.
    /// The string will consist of the given amount of charcaters at most.
    /// </summary>
    public static string GetShortFormat(float value, int charCount) {
        if(charCount <= 0)
            return "";

        if(charCount >= 16f)
            return value.ToString();

        float max = Mathf.Pow(10f, charCount);
        int decCount = Mathf.Max(0, charCount - 2);
        float min = Mathf.Pow(0.1f, decCount);

        if(value > max) {
            if(charCount < 4)
                return max.ToString();
            int pwr = Mathf.FloorToInt(Mathf.Log10(value));
            value = Mathf.Round(value / Mathf.Pow(10f, pwr));
            return value + "e+" + pwr;
        }
        
        if(value < min) {
            if(charCount < 4)
                return min.ToString();
            int pwr = -Mathf.FloorToInt(Mathf.Log10(value));
            value = Mathf.Round(value * Mathf.Pow(10f, pwr));
            return value + "e-" + pwr;

        }

        if(value > max / 100f)
            return Mathf.Round(value).ToString();
        return value.ToString("F" + (charCount - 2));
    }

    /// <summary>
    /// If value is true, sets cursor hidden, locked in the middle of the screen.
    /// When false, the cursor is released and visible.
    /// </summary>
    /// <param name="value"></param>
    public static void SetFPSCursor(bool value) {
        if(value) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Returns string representing the given number of seconds in an 
    /// hours:minutes:seconds.frac format where frac is a fractional value
    /// consisting of prec amount of digits.
    /// </summary>
    public static string GetTimeString(float seconds, int prec) {
        return GetTimeString(seconds, prec, seconds);
    }

    /// <summary>
    /// Returns string representing the given number of seconds in an 
    /// hours:minutes:seconds.frac format where frac is a fractional value
    /// consisting of prec amount of digits. The format will be the same as 
    /// it would be for the value refSeconds.
    /// </summary>
    public static string GetTimeString(float seconds, int prec, float refSeconds) {
        int secInt = Mathf.FloorToInt(seconds);
        int refSecInt = Mathf.FloorToInt(refSeconds);

        float frac = seconds % 1f;
        string fracString = frac.ToString("F" + prec);
        if(fracString.Length <= 2)
            fracString = ".".PadRight(prec + 1, '0');
        else
            fracString = fracString.Substring(1).PadRight(prec + 1, '0');
        return GetTimeString(secInt, refSecInt) + fracString;
    }

    /// <summary>
    /// Returns string representing the given number of seconds in an 
    /// hours:minutes:seconds format
    /// </summary>
    public static string GetTimeString(int seconds) {
        return GetTimeString(seconds, seconds);
    }

    /// <summary>
    /// Returns string representing the given number of seconds in an 
    /// hours:minutes:seconds format, making sure the format is the same 
    /// as that of refSeconds
    /// </summary>
    public static string GetTimeString(int seconds, int refSeconds) {
        if(refSeconds < 60)
            return seconds.ToString();
        int minutes = seconds / 60;
        seconds = seconds % 60;
        if(refSeconds < 3600)
            return minutes + ":" + seconds.ToString().PadLeft(2, '0');
        int hours = minutes / 60;
        minutes = minutes % 60;
        return hours + ":" + minutes.ToString().PadLeft(2, '0') + ":" + seconds.ToString().PadLeft(2, '0');
    }

    /// <summary>
    /// Write given object to the given file path (uncompressed).
    /// </summary>
    public static void Save(string path, object obj) {
        if(string.IsNullOrEmpty(path))
            throw new System.ArgumentException("Trying to save to undefined path");
        else if(obj == null)
            throw new System.ArgumentNullException("Trying to save null object to " + path);

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(path);
        bf.Serialize(file, obj);
        file.Close();
    }

    /// <summary>
    /// Reads (uncompressed) object at the given file path and returns it.
    /// Returns null if file could not be loaded for whatever reason.
    /// </summary>
    public static object Load(string path) {
        FileStream file = null;
        object toReturn = null;
        try {
            BinaryFormatter bf = new BinaryFormatter();
            file = File.Open(path, FileMode.Open);
            toReturn = bf.Deserialize(file);
        }
        catch(System.Exception) { }

        if(file != null)
            file.Close();
        return toReturn;

    }

    /// <summary>
    /// Returns vector2 containint width and height of the near clip 
    /// plane of the given camera. Useful for matching a quad to this
    /// plane.
    /// </summary>
    public static Vector2 GetNearClipExtents(Camera camera) {
        float halfFOV = camera.fieldOfView * .5f * Mathf.Deg2Rad;
        float aspect = camera.aspect;

        float height = Mathf.Tan(halfFOV);
        float width = height * aspect;

        return new Vector2(width, height);
    }

    /// <summary>
    /// Sets given object and all of its children to be static or not.
    /// </summary>
    public static void SetStaticRecursively(GameObject g, bool value) {
        for(int i = 0; i < g.transform.childCount; i++)
            SetStaticRecursively(g.transform.GetChild(i).gameObject, value);
        g.isStatic = value;
    }

    /// <summary>
    /// Sets given object and all of its children to the given layer.
    /// </summary>
    public static void SetLayerRecursively(GameObject g, int layer) {
        for(int i = 0; i < g.transform.childCount; i++)
            SetLayerRecursively(g.transform.GetChild(i).gameObject, layer);
        g.layer = layer;
    }

    /// <summary>
    /// Sets all of the children of the given transform
    /// to the default local transform.
    /// </summary>
    public static void ResetChildrenRecursively(Transform t) {
        for(int i = 0; i < t.childCount; i++)
            ResetTransformRecursively(t.GetChild(i));
    }

    /// <summary>
    /// Sets given transform and all of its children to default 
    /// local transform.
    /// </summary>
    public static void ResetTransformRecursively(Transform t) {
        for(int i = 0; i < t.childCount; i++)
            ResetTransformRecursively(t.GetChild(i));
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }
}
