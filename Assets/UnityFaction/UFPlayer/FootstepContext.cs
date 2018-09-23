using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootstepContext : MonoBehaviour {

    public Type type;

    public enum Type {
        solid, metal, ice, water, gravel, glass, brokenGlass
    }
}
