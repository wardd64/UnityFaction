using System;
using UnityEngine;

namespace UFLevelStructure {

    /* -----------------------------------------------------------------------------------------------
     * --------------------------------------- GENERAL TYPES -----------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    [Serializable]
    public struct PosRot {
        public Vector3 position;
        public Quaternion rotation;

        public PosRot(Vector3 position, Quaternion rotation) {
            this.position = position;
            this.rotation = rotation;
        }

        public PosRot(Vector3 position) : this(position, Quaternion.identity) {}
    }

    [Serializable]
    public struct UFTransform {

        public PosRot posRot;
        public int id;

        public UFTransform(PosRot posRot) : this(posRot, -1) { }

        public UFTransform(PosRot posRot, int id){
            this.posRot = posRot;
            this.id = id;
        }

        public UFTransform(Vector3 pos, Quaternion rot) : this(pos, rot, -1) { }

        public UFTransform(Vector3 pos, Quaternion rot, int id) {
            this.posRot = new PosRot(pos, rot);
            this.id = id;
        }

        public UFTransform(Vector3 pos) : this(pos, -1) { }

        public UFTransform(Vector3 pos, int id) {
            this.posRot = new PosRot(pos);
            this.id = id;
        }
    }

    [Serializable]
    public struct CenteredBox {
        public UFTransform transform;
        public Vector3 extents;

        public CenteredBox(UFTransform transform, Vector3 extents) {
            this.transform = transform;
            this.extents = extents;
        }

        public CenteredBox(PosRot posRot, int id, Vector3 extents) {
            this.transform = new UFTransform(posRot, id);
            this.extents = extents;
        }

        public CenteredBox(PosRot posRot, Vector3 extents) {
            this.transform = new UFTransform(posRot);
            this.extents = extents;
        }
    }

    [Serializable]
    public struct AxisAlignedBoundingBox {
        public Vector3 min, max;

        public AxisAlignedBoundingBox(Vector3 point1, Vector3 point2) {
            float xMin = Mathf.Min(point1.x, point2.x);
            float xMax = Mathf.Max(point1.x, point2.x);
            float yMin = Mathf.Min(point1.y, point2.y);
            float yMax = Mathf.Max(point1.y, point2.y);
            float zMin = Mathf.Min(point1.z, point2.z);
            float zMax = Mathf.Max(point1.z, point2.z);

            min = new Vector3(xMin, yMin, zMin);
            max = new Vector3(xMax, yMax, zMax);
        }

        public bool IsInside(Vector3 point) {
            bool toReturn = true;
            toReturn &= point.x > min.x;
            toReturn &= point.y > min.y;
            toReturn &= point.z > min.z;
            toReturn &= point.x < max.x;
            toReturn &= point.y < max.y;
            toReturn &= point.z < max.z;
            return toReturn;
        }
    }

    /* -----------------------------------------------------------------------------------------------
     * -------------------------------------- Geometry Types -----------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    [Serializable]
    public struct Brush {
        public UFTransform transform;

        public Geometry geometry;
        public bool isPortal, isAir, isDetail, emitsSteam;
        public int life;
    }

    [Serializable]
    public struct Geometry {
        public string[] textures;
        public Vector3[] vertices;
        public Face[] faces;
        public Room[] rooms;
    }

    [Serializable]
    public struct Face {
        public int texture;
        public bool showSky, mirrored, fullBright;
        public FaceVertex[] vertices;
    }

    [Serializable]
    public struct FaceVertex {
        public int vertexRef;
        public Vector2 uv;
    }

    [Serializable]
    public struct Room {
        public AxisAlignedBoundingBox aabb;
        public bool isSkyRoom, isCold, isOutside, isAirlock, hasLiquid, hasAmbientLight, isSubRoom;
        public float life; // -1 -> infinite
        public string eaxEffect;

        //only available when hasLiquid == true
        public LiquidProperties liquidProperties;
        [Serializable]
        public struct LiquidProperties {

            //liquid properties
            public float depth;
            public Color color;
            public string texture;
            public float visibility;
            public LiquidType type;

            public enum LiquidType {
                Undefined = 0, Water = 1, Lava = 2, Acid = 3
            }

            public int alpha; // 0-255
            public WaveForm waveForm;

            public enum WaveForm {
                None = 0xFFFFFFF, Undefined = 0, Calm = 1, Choppy = 2
            }

            public float scrollU, scrollV;

        }

        //only available when hasAmbientLight == true
        public Color ambientLightColor;

    }

    /* -----------------------------------------------------------------------------------------------
     * -------------------------------------- Movement types -----------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    [Serializable]
    public struct MovingGroup {
        public string name;
        public UFLevelStructure.Keyframe[] keys;
        public int startIndex;

        public bool isDoor, startsBackwards, rotateInPlace,
            useTravTimeAsSpd, forceOrient, noPlayerCollide;

        public MovementType type;
        public enum MovementType {
            Undefined = 0, OneWay = 1, PingPongOnce = 2, PingPongInfinite = 3,
            LoopOnce = 4, LoopInfinite = 5, Lift = 6
        }

        public string startClip, loopClip, stopClip, closeClip;
        public float startVol, loopVol, stopVol, closeVol;

        public int[] contents; //list of ID of brushes and objects moved in this group
    }

    [Serializable]
    public struct Keyframe {
        public UFTransform transform;

        public float pauseTime;
        public float departTravelTime, returnTravelTime;
        public float accelTime, decelTime;
        public int triggerID; //IDs are -1 if not used
        public int containID1, containID2;
        public float rotationAmount; //degrees
    }


    /* -----------------------------------------------------------------------------------------------
     * --------------------------------------- OBJECT TYPES ------------------------------------------
     * -----------------------------------------------------------------------------------------------
     */

    [Serializable]
    public struct Light {
        public UFTransform transform;

        public LightType type;
        public enum LightType {
            undefined, point, spotlight, tube
        }

        public bool dynamic, shadows, enabled;
        public Color color;

        public float range, fov, fovDropOff;
        public float intensityAtMax, tubeLength, intensity;

        //intensity at max is fractional multiplier for intensity
    }

    [Serializable]
    public struct AmbSound {
        public UFTransform transform;

        public string clip;
        public float minDist, volume, roloff, startDelay;
        //volume 0-1, delay in miliseconds
    }

    [Serializable]
    public struct SpawnPoint {
        public UFTransform transform;

        public int team;
        public bool redTeam, blueTeam, bot;
    }

    [Serializable]
    public struct ParticleEmiter {
        public UFTransform transform;

        public EmiterShape type;
        public enum EmiterShape {
            point = 0, plane = 1, sphere = 2
        }

        public float SphereRadius;
        public Vector2 planeExtents; //width, depth
        public string texture;
        public float spawnDelay, spawnRandomize;
        public float velocity, velocityRandomize;
        public float acceleration;
        public float decay, decayRandomize;
        public float radius, radiusRandomize;
        public float growthRate;
        public float gravityMultiplier;
        public float randomDirection; //degrees
        public Color particleColor, fadeColor;
        public byte bounciness, stickieness, swirliness, pushEffect;
        public bool loopAnim, randomOrient;
        public bool fade, glow, collidWithWorld, explodeOnImpact, dieOnImpact,
            collidWithLiquids, playCollisionSounds, gravity;
        public bool forceSpawnEveryFrame, directionDependentVelocity, emitterInitiallyOn;
        public float timeOn, timeOnRandomize, timeOff, timeOffRandomize;
        public float activeDistance;
    }

    [Serializable]
    public struct GeoRegion {
        public UFTransform transform;

        public GeoShape shape;
        public enum GeoShape {
            undefined = 0, sphere = 2, box = 4
        }

        public bool ice, shallow;
        public byte hardness; //100 is indestructible

        public Vector3 extents; //when box
        public float sphereRadius; //when sphere
    }

    [Serializable]
    public struct Decal {
        public CenteredBox cbTransform;

        public string texture;
        public TilingMode tiling;
        public enum TilingMode {
            None = 0, U = 1, V = 2
        }

        public int alpha; //0-255
        public bool selfIlluminated;
        public float scale;
    }

    [Serializable]
    public struct PushRegion {
        public UFTransform transform;

        public PushShape shape;

        public enum PushShape {
            undefined, sphere, alignedBox, orientedBox
        }

        public Vector3 extents;
        public float sphereRadius;
        public float strength;

        public int turbulence; // 0 - 15

        public bool massIndependent, radial, noPlayer, grounded, jumpPad;

        public Profile profile;

        public enum Profile {
            Constant, GrowsToBoundary, GrowsToCenter
        }
    }

    [Serializable]
    public struct ClimbingRegion {
        public CenteredBox cbTransform;

        public ClimbingType type;
        public enum ClimbingType {
            Undefined = 0, Ladder = 1, Fence = 2
        }
    }

    [Serializable]
    public struct BoltEmiter {
        public UFTransform transform;

        public int targetID;
        public float srcCtrlDist, trgCtrlDist;
        public float thickness, jitter;
        public int nboSegments;
        public float spawnDelay, spawnDelayRandomize;
        public float decay, decayRandomize;
        public Color color;
        public string texture;
        public bool fade, glow, srcDirLock, trgDirLock, initOn;
    }

    [Serializable]
    public struct Item {
        public UFTransform transform;

        public string name;

        /*
        public ItemType type;
        public enum ItemType {
            __50cal_ammo, _10gauge_ammo, _12mm_ammo, _5_56mm_ammo, _7_62mm_ammo, _Assault_Rifle,
            _base_blue, _base_red, _Brainstem, _CTF_Banner_Blue, _CTF_Banner_Red, _Demo_K000,
            _Doctor_Uniform, _explosive_5_56mm_rounds, _First_Aid_Kit, _flag_blue, _flag_red,
            _flamethrower, grenades, Handgun, _heavy_machine_gun, _keycard, _Machine_Pistol,
            _Medical_Kit, _Miner_Envirosuit, _Multi_Damage_amplifier, _Multi_Invulnerability,
            _Multi_Super_Armor, _Multi_Super_Health, _Napalm, _rail_gun, _railgun_bolts,
            _Remote_Charges, _riot_shield, _Riot_Stick, _riot_stick_battery, _rocket_launcher,
            _rocket_launcher_ammo, _scope_assault_rifle, _Shotgun, _shoulder_cannon,
            _Silenced_12mm_Handgun, _Sniper_Rifle, _Suit_Repair
        }
        */

        public int count;
        public int respawnTime;  //-1 is infinite
        public int team;
    }

    [Serializable]
    public struct Clutter {
        public UFTransform transform;

        public string name;
        public int[] links; //list of object IDs
    }

    [Serializable]
    public struct Event {
        public UFTransform transform;

        public EventType type;
        public enum EventType {
            Nothing, StartTrigger, 
            Attack, Bolt_state, Continuous_Damage, Cyclic_Timer, Drop_Point_Marker, Explode, Follow_Player,
            Follow_Waypoints, Give_item_To_Player, Goal_Create, Goal_Check, Goal_Set, Goto, Goto_Player, Heal,
            Invert, Load_Level, Look_At, Make_Invulnerable, Make_Fly, Make_Walk, Message, Music_Start, Music_Stop,
            Particle_State, Play_Animation, Play_Sound, Slay_Object, Remove_Object, Set_AI_Mode, Set_Light_State,
            Set_Liquid_Depth, Set_Friendliness, Shake_Player, Shoot_At, Shoot_Once, Armor, Spawn_Object, Swap_Textures,
            Switch, Switch_Model, Teleport, When_Dead, Set_Gravity, Alarm, Alarm_Siren, Go_Undercover, Delay,
            Monitor_State, UnHide, Push_Region_State, When_Hit, Headlamp_State, Item_Pickup_State, Cutscene,
            Strip_Player_Weapons, Fog_State, Detach, Skybox_State, Force_Monitor_Update, Black_Out_Player,
            Turn_Off_Physics, Teleport_Player, Holster_Weapon, Holster_Player_Weapon, Modify_Rotating_Mover,
            Clear_Endgame_If_Killed, Win_PS2_Demo, Enable_Navpoint, Play_Vclip, Endgame, Mover_Pause, Countdown_begin,
            Countdown_End, When_Countdown_Over, Activate_Capek_Shield, When_Enter_Vehicle, When_Try_Exit_Vehicle,
            Fire_Weapon_No_Anim, Never_Leave_Vehicle, Drop_Weapon, Ignite_Entity, When_Cutscene_Over,
            When_Countdown_Reach, Display_Fullscreen_Image, Defuse_Nuke, When_Life_Reaches, When_Armor_Reaches,
            Reverse_Mover
        }

        public string name;

        public static bool HasRotation(EventType e) {
            return e == EventType.Alarm ||
                e == EventType.Teleport ||
                e == EventType.Teleport_Player ||
                e == EventType.Play_Vclip;
        }

        public float delay;
        public bool bool1, bool2;
        public int int1, int2;
        public float float1, float2;
        public string string1, string2;

        public int[] links;
        public Color color;
    }

    [Serializable]
    public struct Entity {
        public UFTransform transform;

        public int cooperation, friendliness, team;
        public string wayPointList, wayPointMethod;
        public bool boarded, readyToFire, onlyAttackPlayer, weaponIsHolstered, deaf;
        public float sweepMinAngle, sweepMaxAngle;
        public bool ignoreTerrainWhenFiring, startCrouched;
        public float life, armor;
        public int fov;
        public string primary, secondary; //weapons
        public string itemDrop, stateAnim, corpsePose, skin, deathAnim;
        public byte aiMode, aiAttackStyle;
        public int turretID, alertCameraID, alarmEventID;
        public bool run, hidden, helmet, endGameIfKilled, cowerFromWeapon,
            questionUnarmedPlayer, dontHum, noShadow, alwaysSimulate,
            perfectAim, permanentCorpse, neverFly, neverLeave,
            noPersonaMessages, fadeCorpseImmedate, neverCollideWithPlayer,
            useCustomAttackRange;
        public float customAttackRange;
        public string leftHandHolding, rightHandHolding;
    }

    [Serializable]
    public struct Trigger {
        public UFTransform transform;

        public bool box;
        public float resetDelay;
        public int resets; //negative -> infinity
        public bool useKey;
        public string keyName;
        public bool weaponActivates, isNPC, isAuto, inVehicle;

        //sphere properties (box == false)
        public float sphereRadius;

        //box properties (box == true)
        public Vector3 extents;
        public bool oneWay;

        public int airlockRoom, attachedTo, useClutter;
        public bool disabled;
        public float buttonActiveTime, insideTime;

        public int[] links;
    }
}