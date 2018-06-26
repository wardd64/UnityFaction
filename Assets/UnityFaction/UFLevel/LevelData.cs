using System.Collections.Generic;
using UFLevelStructure;
using UnityEngine;

public class LevelData {

    public LevelData() {
        brushes = new Brush[0];
        lights = new UFLevelStructure.Light[0];
        ambSounds = new AmbSound[0];
        events = new UFLevelStructure.Event[0];
        spawnPoints = new SpawnPoint[0];
        particleEmiters = new UFLevelStructure.ParticleEmiter[0];
        decals = new Decal[0];
        climbingRegions = new ClimbingRegion[0];
        boltEmiters = new BoltEmiter[0];
        targets = new UFTransform[0];
        entities = new Entity[0];
        items = new Item[0];
        clutter = new Clutter[0];
        triggers = new Trigger[0];
        movingGeometry = new Brush[0];
        movingGroups = new MovingGroup[0];
        geoRegions = new GeoRegion[0];
    }

    //general info
    public string name, author;
    public PosRot playerStart;

    //level properties
    public string geomodTexture;
    public int hardness;
    public Color ambientColor, fogColor;
    public float nearPlane, farPlane;
    public bool multiplayer;

    //static geometry
    public Geometry staticGeometry;
    public Brush[] brushes;

    //objects
    public UFLevelStructure.Light[] lights;
    public AmbSound[] ambSounds;
    public UFLevelStructure.Event[] events;
    public SpawnPoint[] spawnPoints;
    public ParticleEmiter[] particleEmiters;
    public Decal[] decals;
    public ClimbingRegion[] climbingRegions;
    public BoltEmiter[] boltEmiters;
    public UFTransform[] targets;
    public Entity[] entities;
    public Item[] items;
    public Clutter[] clutter;
    public Trigger[] triggers;
    public GeoRegion[] geoRegions;

    //moving stuff
    public Brush[] movingGeometry;
    public MovingGroup[] movingGroups;
}
