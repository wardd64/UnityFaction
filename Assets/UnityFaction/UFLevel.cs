using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UFLevel {

    //helper structs
    public struct PosRot {
        public Vector3 position;
        public Quaternion rotation;

        public PosRot(Vector3 position, Quaternion rotation) {
            this.position = position;
            this.rotation = rotation;
        }
    }

    public struct CenteredBox {
        public PosRot transform;
        public Vector3 extents;

        public CenteredBox(PosRot transform, Vector3 extents) {
            this.transform = transform;
            this.extents = extents;
        }
    }

    public struct AxisAlignedBoundingBox {
        public Vector3 point1, point2;

        public AxisAlignedBoundingBox(Vector3 point1, Vector3 point2) {
            this.point1 = point1;
            this.point2 = point2;
        }
    }

    //---------------------------------------------------------------------------------------------

    //general info
    public string name, author;

    //level properties
    public string geomodTexture;
    public int hardness;
    public Color ambientColor, fogColor;
    public float nearPlane, farPlane;
    public bool multiplayer;

    //static geometry
    public Geometry staticGeometry;

    public struct Geometry {
        public string[] textures;
        public Vector3[] vertices;
        public UFFace[] faces;
        public UFRoom[] rooms;
    }

    public struct UFFace {
        public int texture;
        public bool showSky, mirrored, fullBright;
        public UFFaceVertex[] vertices;
    }

    public struct UFFaceVertex {
        public int id;
        public Vector2 uv;
    }

    public struct UFRoom {
        public AxisAlignedBoundingBox aabb;
        public bool isSkyRoom, isCold, isOutside, isAirlock, hasLiquid, hasAmbientLight, isSubRoom;
        public float life; // -1 -> infinite
        public string eaxEffect;

        //only available when hasLiquid == true
        public LiquidProperties liquidProperties;
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

    //objects
    public UFLight[] lights;

    public struct UFLight {
        public PosRot transform;
        public Color color;

    }

    public UFAmbSound[] ambSounds;

    public struct UFAmbSound {
        public Vector3 position;
        public string clip;
        public float minDist, volume, roloff, startDelay;
        //volume 0-1, delay in miliseconds
    }

    public UFSpawnPoint[] spawnPoints;

    public struct UFSpawnPoint {
        public PosRot transform;
        public int team;
        public bool redTeam, blueTeam, bot;
    }

    public UFParticleEmitter[] emitters;

    public struct UFParticleEmitter {
        public PosRot transform;
        public EmitterType type;

        public enum EmitterType {
            point = 0, plane = 1, sphere = 2

        }

        public float SphereRadius;
        public float planeWidth, planeDepth;
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

    public UFDecal[] decals;

    public struct UFDecal {
        public CenteredBox cbTransform;
        public string texture;
        public TilingMode tiling;
        public int alpha; //0-255
        public bool selfIlluminated;
        public float scale;

        public enum TilingMode {
            None = 0, U = 1, V = 2
        }
    }

    public UFClimbingRegion[] climbingRegions;

    public struct UFClimbingRegion {
        public CenteredBox cbTransform;
        public ClimbingType type;

        public enum ClimbingType {
            Undefined = 0, Ladder = 1, Fence = 2
        }
    }

    public UFBoltEmiter[] boltEmiters;

    public struct UFBoltEmiter {
        public PosRot transform;
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

    public UFTarget[] targets;

    public struct UFTarget {
        public int id;
        public PosRot transform;
    }

    //moving stuff
    public MovingBrush[] movingGeometry;

    public struct MovingBrush {
        public int id;
        public PosRot transform;
        public Geometry geometry;
        public bool isPortal, isDetail, emitsSteam;
        public int life;
    }

    public MovingGroup[] movingGroups;

    public struct MovingGroup {
        public string name;
        public Keyframe[] keys;

        public struct KeyFrame {
            public int id;
            public PosRot transform;
            public float pauseTime;
            public float departTravelTime, returnTravelTime;
            public float accelTime, decelTime;
            public int triggerID;
            public int containID1, containID2;
            public float rotationAmount; //degrees
        }

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

    //others
    public PosRot playerStart;

    public UFItem[] items;

    public struct UFItem {
        public int id;
        public PosRot transform;

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

    public UFClutter[] clutter;

    public struct UFClutter {
        public int id;
        public PosRot transform;
        public string name;
        public int[] links; //list of object ID
    }

}
