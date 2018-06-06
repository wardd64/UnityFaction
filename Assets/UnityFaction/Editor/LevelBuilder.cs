using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;

public class LevelBuilder {

    [MenuItem("UnityFaction/Build Level")]
    public static void BuildLevel() {
        //let user select rfl file that needs to be built into the scene
        string fileSearchMessage = "Select rfl file you would like to build";
        string rflPath = EditorUtility.OpenFilePanel(fileSearchMessage, "Assets", "rfl");
        if(string.IsNullOrEmpty(rflPath))
            return;

        ReadRFL(rflPath);
    }

    private static void ReadRFL(string rflPath) {
        byte[] bytes = File.ReadAllBytes(rflPath);

        
    }
}

