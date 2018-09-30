using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;
using System;

public class UFBoltEmitter : MonoBehaviour {

    //general
    private float timer, nextSpawnDelay;
    private bool emitting;
    public Bolt[] bolts;

    //bolt timekeeping
    public float decay, decayRandomize, delay, delayRandomize;

    //bolt animation variables
    public bool initOn;
    public int targetID;
    public float jitter;
    public AnimationCurve boltShape;

    public void Set(BoltEmitter emit) {
        LineRenderer lr = gameObject.AddComponent<LineRenderer>();

        this.initOn = emit.initOn;
        jitter = emit.jitter;

        lr.positionCount = Mathf.Max(2, emit.nboSegments + 1);
        lr.startWidth = emit.thickness;
        lr.endWidth = emit.thickness;
        lr.startColor = emit.color;
        lr.endColor = emit.color;

        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.useWorldSpace = false;
        lr.enabled = false;

        targetID = emit.targetID;

        decay = emit.decay;
        decayRandomize = emit.decayRandomize;
        delay = emit.spawnDelay;
        delayRandomize = emit.spawnDelayRandomize;

        float maxDecay = decay + decayRandomize;
        float minDelay = delay - delayRandomize;
        int maxBolts = 10;
        if(minDelay > 0f)
            maxBolts = Mathf.Min(maxBolts, Mathf.CeilToInt(maxDecay / minDelay));
        bolts = new Bolt[maxBolts];

        List<UnityEngine.Keyframe> boltShapeKeys = new List<UnityEngine.Keyframe>();
        if(emit.srcDirLock) {
            boltShapeKeys.Add(new UnityEngine.Keyframe(0f, 0f, 0f, 0f));
            boltShapeKeys.Add(new UnityEngine.Keyframe(.25f, emit.srcCtrlDist, 0f, 0f));
        }
        else
            boltShapeKeys.Add(new UnityEngine.Keyframe(0f, 0f, 0f, 2f* emit.srcCtrlDist));
        if(emit.trgDirLock) {
            boltShapeKeys.Add(new UnityEngine.Keyframe(.75f, emit.trgCtrlDist, 0f, 0f));
            boltShapeKeys.Add(new UnityEngine.Keyframe(0f, 0f, 0f, 0f));
        }
        else
            boltShapeKeys.Add(new UnityEngine.Keyframe(0f, 0f, -2f * emit.trgCtrlDist, 0f));

        boltShape = new AnimationCurve(boltShapeKeys.ToArray());

        lr.numCapVertices = 3;
        lr.numCornerVertices = 1;
        
    }

    public void SetMaterial(Material material) {
        LineRenderer lr = GetComponent<LineRenderer>();
        if(lr != null)
            lr.material = material;
    }

    private void Start() {
        timer = 0f;
        nextSpawnDelay = GetRandomTime(delay, delayRandomize);

        LineRenderer boltTemplate = this.GetComponent<LineRenderer>();

        //initialize bolts
        for(int i = 0; i < bolts.Length; i++) {
            GameObject g = new GameObject("Bolt_" + i.ToString().PadLeft(2, '0'));
            g.transform.SetParent(transform);
            g.transform.localPosition = Vector3.zero;
            g.transform.localRotation = Quaternion.identity;
            bolts[i] = new Bolt(g, boltTemplate);
        }

        Destroy(boltTemplate);

        this.emitting = this.initOn;
    }

    private void LateUpdate() {
        //update existing bolts
        foreach(Bolt bolt in bolts) {
            if(!bolt.DecayTick()) {
                Transform boltTarget = UFLevel.GetByID(targetID).objectRef.transform;
                bolt.Animate(boltTarget, jitter, boltShape);
            }
        }

        //emit new bolts
        if(emitting)
            EmitUpdate();
    }

    private void EmitUpdate() {
        timer += Time.deltaTime;
        if(timer > nextSpawnDelay) {
            FireNewBolt();
            timer -= Mathf.Max(Time.deltaTime, nextSpawnDelay);
            nextSpawnDelay = GetRandomTime(delay, delayRandomize);
        }
    }

    private void FireNewBolt() {
        float nextDecay = GetRandomTime(decay, decayRandomize);
        Bolt oldestBolt = bolts[0];
        float oldestBoltTime = 0f;

        foreach(Bolt bolt in bolts) {
            if(bolt.decayed) {
                bolt.Fire(nextDecay);
                return;
            }
            else if(bolt.time > oldestBoltTime){
                oldestBolt = bolt;
                oldestBoltTime = bolt.time;
            }
        }

        //if no decayed bolts exist, refresh the oldest one
        oldestBolt.Fire(nextDecay);
    }


    public void Activate(bool positive) {
        this.emitting = positive;
    }

    private float GetRandomTime(float baseTime, float randomizeTime) {
        float min = Mathf.Max(0f, baseTime - randomizeTime);
        float max = baseTime + randomizeTime;
        return UnityEngine.Random.Range(min, max);
    }

    [Serializable]
    public class Bolt {

        //decay
        public float time;
        public float lifeTime;

        //reference
        public LineRenderer lr;

        public bool decayed { get { return time >= lifeTime; } }

        public Bolt(GameObject g, LineRenderer boltTemplate) {
            lr = UFUtils.AddComponent(g, boltTemplate);
            lr.material = boltTemplate.material;
            lr.enabled = false;
        }

        public void Fire(float time) {
            this.time = 0f;
            this.lifeTime = time;
            lr.enabled = true;
        }

        public bool DecayTick() {
            this.time += Time.deltaTime;
            if(this.time >= lifeTime) {
                lr.enabled = false;
                this.time = lifeTime;
                return true;
            }
            return false;
        }

        public void Animate(Transform target, float jitter, AnimationCurve boltShape) {
            Vector3 basePos = lr.transform.position;
            Vector2 randGlobal = UnityEngine.Random.insideUnitCircle;
            lr.transform.LookAt(target);
            float distance = (target.position - basePos).magnitude;
            int nboPts = lr.positionCount;

            for(int i = 0; i < nboPts; i++) {
                float t = (float)i / (nboPts - 1);
                float r = boltShape.Evaluate(t);
                float z = distance * t;
                Vector2 randLocal = UnityEngine.Random.insideUnitCircle;

                Vector2 pos = randGlobal * r + randLocal * jitter;
                lr.SetPosition(i, new Vector3(pos.x, pos.y, z));
            }
        }
    }
}
