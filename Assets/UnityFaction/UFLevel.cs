using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UFLevel {

    //general info
    public string name, author;

    //level properties
    public string geomodTexture;
    public int hardness;
    public Color ambientColor, fogColor;
    public float nearPlane, farPlane;
    public bool multiplayer;

    //static geometry
    public string[] textures;
    public Vector3[] vertices;
    public UFFace[] faces;

    public struct UFFace {
        public int texture;
        public bool showSky, mirrored, fullBright;
        public UFFaceVertex[] vertices;
    }

    public struct UFFaceVertex {
        public int id;
        public float u, v;
    }

    
}
