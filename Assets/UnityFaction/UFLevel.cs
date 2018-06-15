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
        public Vector2 uv;
    }

    public UFRoom[] rooms;

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

}
