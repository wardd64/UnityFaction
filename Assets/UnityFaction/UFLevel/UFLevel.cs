using UFLevelStructure;

public class UFLevel {

    public UFLevel() {
        brushes = new Brush[0];
        lights = new Light[0];
        ambSounds = new AmbSound[0];
        events = new Event[0];
        spawnPoints = new SpawnPoint[0];
        particleEmiters = new ParticleEmitter[0];
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
    }

    //general info
    public string name, author;
    public PosRot playerStart;

    //level properties
    public string geomodTexture;
    public int hardness;
    public UnityEngine.Color ambientColor, fogColor;
    public float nearPlane, farPlane;
    public bool multiplayer;

    //static geometry
    public Geometry staticGeometry;
    public Brush[] brushes;

    //objects
    public Light[] lights;
    public AmbSound[] ambSounds;
    public Event[] events;
    public SpawnPoint[] spawnPoints;
    public ParticleEmitter[] particleEmiters;
    public Decal[] decals;
    public ClimbingRegion[] climbingRegions;
    public BoltEmiter[] boltEmiters;
    public UFTransform[] targets;
    public Entity[] entities;
    public Item[] items;
    public Clutter[] clutter;
    public Trigger[] triggers;

    //moving stuff
    public Brush[] movingGeometry;
    public MovingGroup[] movingGroups;

}
