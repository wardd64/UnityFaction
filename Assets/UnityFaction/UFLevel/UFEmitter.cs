using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFEmitter : MonoBehaviour {

    float timer;
    float randOnTime, randOffTime;
    public float timeOn, timeOnRandomize, timeOff, timeOffRandomize;

    public void Set(BoltEmitter emit) {
        //TODO
    }

    public void Set(UFLevelStructure.ParticleEmitter emit) {
        ParticleSystem ps = gameObject.AddComponent<ParticleSystem>();

        this.timeOn = emit.timeOn;
        this.timeOnRandomize = emit.timeOnRandomize;
        this.timeOff = emit.timeOff;
        this.timeOffRandomize = emit.timeOffRandomize;

        ParticleSystem.MainModule psMain = ps.main;
        psMain.loop = true;
        psMain.playOnAwake = emit.emitterInitiallyOn;

        ParticleSystem.ShapeModule psShape = ps.shape;
        switch(emit.type) {

        case UFLevelStructure.ParticleEmitter.EmitterShape.plane:
        psShape.shapeType = ParticleSystemShapeType.Box;
        psShape.scale = new Vector3(emit.planeExtents.x, emit.planeExtents.y, 0.1f);
        psShape.randomDirectionAmount = 0.1f * emit.randomDirection / 90f;
        break;

        case UFLevelStructure.ParticleEmitter.EmitterShape.sphere:
        psShape.shapeType = ParticleSystemShapeType.Cone;
        psShape.angle = emit.randomDirection;
        psShape.radius = emit.SphereRadius;
        break;

        default:
        psShape.enabled = false;
        break;

        }

        float rMin = emit.radius - emit.radiusRandomize;
        float rMax = emit.radius + emit.radiusRandomize;
        psMain.startSize = new ParticleSystem.MinMaxCurve(rMin, rMax);

        float decMin = emit.decay - emit.decayRandomize;
        float decMax = emit.decay + emit.decayRandomize;
        psMain.startLifetime = new ParticleSystem.MinMaxCurve(decMin, decMax);

        float velMin = emit.velocity - emit.velocityRandomize;
        float velMax = emit.velocity + emit.velocityRandomize;
        psMain.startLifetime = new ParticleSystem.MinMaxCurve(velMin, velMax);

        ParticleSystem.EmissionModule psEmission = ps.emission;
        float spwMin = 1f / (emit.spawnDelay + emit.spawnRandomize);
        float spwMax = 1f / (emit.spawnDelay - emit.spawnRandomize);
        psEmission.rateOverTime = new ParticleSystem.MinMaxCurve(spwMin, spwMax);

        psMain.startColor = emit.particleColor;

        float time = emit.decay;

        if(emit.gravity)
            psMain.gravityModifierMultiplier = emit.gravityMultiplier;
        else
            psMain.gravityModifierMultiplier = 0f;

        ParticleSystem.SizeOverLifetimeModule psSolt = ps.sizeOverLifetime;
        if(emit.growthRate != 0f) {
            psSolt.enabled = true;
            float finalSize = (emit.radius + emit.growthRate * time) / emit.radius;
            AnimationCurve curve = AnimationCurve.Linear(0f, 1f, time, finalSize);
            psSolt.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        ParticleSystem.VelocityOverLifetimeModule psVolt = ps.velocityOverLifetime;
        if(emit.acceleration != 0f) {
            psVolt.enabled = true;
            float finalVel = (emit.velocity + emit.acceleration * time) / emit.velocity;
            AnimationCurve curve = AnimationCurve.Linear(0f, 1f, time, finalVel);
            psVolt.speedModifier = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        ParticleSystem.ColorOverLifetimeModule psColt = ps.colorOverLifetime;
        if(emit.fadeColor != emit.particleColor) {
            psColt.enabled = true;
            Gradient gradient = UFUtils.GetLinearGradient(emit.particleColor, emit.fadeColor);
            psColt.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        if(emit.randomOrient)
            psMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f);

        ParticleSystem.CollisionModule psCollision = ps.collision;
        if(emit.collidWithWorld) {
            psCollision.enabled = true;
            psCollision.type = ParticleSystemCollisionType.World;
            psCollision.quality = ParticleSystemCollisionQuality.Medium;
            //TODO: explodeOnImpact, dieOnImpact, collidWithLiquids, playCollisionSounds
            //bounciness, stickieness
        }
        //TODO: swirliness, pushEffect, loopAnim, fade, glow
        // forceSpawnEveryFrame, directionDependentVelocity;

       
    }

    public void SetMaterial(Material material) {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if(ps != null) {
            Renderer psRenderer = ps.GetComponent<Renderer>();
            psRenderer.material = material;
        }

        LineRenderer lr = GetComponent<LineRenderer>();
        if(lr != null)
            lr.material = material;
    }

    private void Start() {
        timer = 0f;
        if(usesCycles)
            GenerateOnOffTime();
    }

    private void Update() {
        if(usesCycles)
            CycleUpdate();
    }

    private bool usesCycles { get { return timeOn > 0f && timeOff > 0f; } }

    private void CycleUpdate() {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        timer += Time.deltaTime;
        if(timer < randOnTime)
            ps.Play();
        else if(timer < randOnTime + randOffTime)
            ps.Stop();
        else {
            timer -= randOnTime + randOffTime;
            GenerateOnOffTime();
            CycleUpdate();
        }
    }

    private void GenerateOnOffTime() {
        randOnTime = Random.Range(timeOn - timeOnRandomize, timeOn + timeOnRandomize);
        randOnTime = Mathf.Max(0f, randOnTime);
        randOffTime = Random.Range(timeOff - timeOffRandomize, timeOff + timeOffRandomize);
        randOffTime = Mathf.Max(0f, randOffTime);
    }

}
