using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides an interface for the user input that facilitates
/// common functions, connects to the UI and can be rebound at run time.
/// </summary>
public class InputInterface : MonoBehaviour {

    public static InputInterface input { get {
            if(instance == null)
                instance = FindObjectOfType<InputInterface>();
            return instance;
    } }
    private static InputInterface instance;

    //values set in Unity editor are the default values
    public KeyBinding[] bindings;
    public AudioSource changeBindingSound;

    private void Awake() {
        LoadBindings();
    }

    private void LoadBindings() {
        for(int i = 0; i < bindings.Length; i++) {
            int oldValue = (int)bindings[i].code;
            bindings[i].code = (KeyCode)PlayerPrefs.GetInt(bindings[i].name, oldValue);
        }
    }

    /// <summary>
    /// Returns true if any player input (clicking or typing) has been recorded during this frame.
    /// </summary>
    public static bool AnyInput() {
        return Input.anyKey;
    }

    public void SaveBindings() {
        for(int i = 0; i < bindings.Length; i++) {
            PlayerPrefs.SetInt(bindings[i].name, (int)bindings[i].code);
        }
    }

    /// <summary>
    /// Returns true if player character should be able to receive input at this time.
    /// </summary>
    public static bool PlayerInput() {
        return true;
    }

    /// <summary>
    /// true if given key string exists and has been pressed during last frame.
    /// </summary>
    public bool GetKeyDown(string keyName) {
        KeyCode code = GetKeyCode(keyName);
        return Input.GetKeyDown(code) || Input.GetKeyDown(GetKeyTwin(code));
    }

    /// <summary>
    /// true if given key string exists and is currently being held down.
    /// </summary>
    public bool GetKey(string keyName) {
        KeyCode code = GetKeyCode(keyName);
        return Input.GetKey(code) || Input.GetKey(GetKeyTwin(code));
    }

    /// <summary>
    /// True if the given number corresponds to a valid number key (0-9)
    /// and has been pressed during the last frame.
    /// </summary>
    public static bool GetNumericKeyDown(int number) {
        if(number < 0 || number > 9)
            return false;
        return Input.GetKeyDown(KeyCode.Alpha0 + number) ||
            Input.GetKeyDown(KeyCode.Keypad0 + number);
    }

    /// <summary>
    /// True if the given number corresponds to a valid number key (0-9)
    /// and has is currently being held down.
    /// </summary>
    public static bool GetNumericKey(int number) {
        if(number < 0 || number > 9)
            return false;
        return Input.GetKey(KeyCode.Alpha0 + number) ||
            Input.GetKey(KeyCode.Keypad0 + number);
    }

    /// <summary>
    /// Return name of given key that player may recognize on his keyboard.
    /// </summary>
    public string GetKeyName(string keyName) {
        return GetKeyName(GetKeyCode(keyName));
    }

    /// <summary>
	/// Return name of given keyCode that player may recognize on his keyboard.
	/// </summary>
    public static string GetKeyName(KeyCode code) {
        if(code == KeyCode.LeftShift || code == KeyCode.RightShift)
            return "Shift";
        if(code == KeyCode.KeypadEnter || code == KeyCode.Return)
            return "Enter";
        if(code == KeyCode.LeftControl || code == KeyCode.RightControl)
            return "Control";
        return code.ToString();
    }

    public KeyCode GetKeyCode(string keyName) {
        if(keyName.StartsWith("Mouse", StringComparison.OrdinalIgnoreCase)) {
            return KeyCode.Mouse0 + int.Parse(keyName.Substring(5));
        }
        for(int i = 0; i < bindings.Length; i++) {
            if(keyName.Equals(bindings[i].name, StringComparison.OrdinalIgnoreCase))
                return bindings[i].code;
        }

        Debug.LogError("Looked for unknown key: " + keyName);
        return 0;
    }

    /// <summary>
    /// Inserts this binding into the binding list. Does nothing if given 
    /// keyName does not exist.
    /// </summary>
    public void SetBinding(KeyBinding binding, bool initialSet) {
        if(!initialSet && changeBindingSound != null)
            changeBindingSound.Play();
        for(int i = 0; i < bindings.Length; i++) {
            if(binding.name.Equals(bindings[i].name, StringComparison.OrdinalIgnoreCase)) {
                bindings[i] = binding;
                return;
            }
        }
    }

    /// <summary>
    /// Returns twin KeyCode of the given code if it exists.
    /// This is usefull for avoiding duplicates like KeypadEnter and Return.
    /// Returns KeyCode.None if the given code has no twin.
    /// </summary>
    private static KeyCode GetKeyTwin(KeyCode code) {
        switch(code) {
        case KeyCode.KeypadEnter:
        return KeyCode.Return;
        case KeyCode.Return:
        return KeyCode.KeypadEnter;
        case KeyCode.LeftShift:
        return KeyCode.RightShift;
        case KeyCode.RightShift:
        return KeyCode.LeftShift;
        case KeyCode.LeftControl:
        return KeyCode.RightControl;
        case KeyCode.RightControl:
        return KeyCode.LeftControl;
        default:
        return KeyCode.None;
        }
    }

    [Serializable]
    public struct KeyBinding {
        public string name;
        public KeyCode code;

        public KeyBinding(string name, KeyCode code) {
            this.name = name;
            this.code = code;
        }
    }

}
