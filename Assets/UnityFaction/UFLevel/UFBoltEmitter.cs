using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFBoltEmitter : MonoBehaviour {

    //general
    float timer, nextSpawnDelay;
    bool emitting;
    Bolt[] bolts;

    //bolt timekeeping
    public float decay, decayRandomize, delay, delayRandomize;

    //bolt animation variables
    public Transform boltTarget;
    public float jitter;
    public AnimationCurve boltShape;

    public void Set(BoltEmitter emit) {
        //TODO
        LineRenderer lr = gameObject.AddComponent<LineRenderer>();

        this.emitting = emit.initOn;

        lr.positionCount = Mathf.Max(2, emit.nboSegments + 1);
        lr.startWidth = emit.thickness;
        lr.endWidth = emit.thickness;
        lr.startColor = emit.color;
        lr.endColor = emit.color;

        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        boltTarget = UFLevel.GetByID(emit.targetID).objectRef.transform;

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
    }

    public void SetMaterial(Material material) {
        LineRenderer lr = GetComponent<LineRenderer>();
        if(lr != null)
            lr.material = material;
    }

    private void Start() {
        timer = 0f;
        nextSpawnDelay = GetRandomTime(delay, delayRandomize);

        //initialize bolts
        for(int i = 0; i < bolts.Length; i++) {
            GameObject g = new GameObject("Bolt_" + i.ToString().PadLeft('0'));
            g.transform.SetParent(transform);
            g.transform.localPosition = Vector3.zero;
            g.transform.localRotation = Quaternion.identity;
            bolts[i] = new Bolt(g, GetComponent<LineRenderer>());
        }
    }

    private void LateUpdate() {
        if(emitting)
            EmitUpdate();

        foreach(Bolt bolt in bolts) {
            if(!bolt.DecayTick())
                bolt.Animate(boltTarget, jitter, boltShape);
        }
    }

    private void EmitUpdate() {
        timer += Time.deltaTime;
        while(timer > nextSpawnDelay) {
            FireNewBolt();
            timer -= nextSpawnDelay;
            nextSpawnDelay = GetRandomTime(delay, delayRandomize);
        }
    }

    private void FireNewBolt() {
        float nextDecay = GetRandomTime(decay, decayRandomize);
        foreach(Bolt bolt in bolts) {
            if(bolt.decayed) {
                bolt.Fire(nextDecay);
                return;
            }
        }
    }


    public void Activate(bool positive) {
        this.emitting = positive;

    }

    private float GetRandomTime(float baseTime, float randomizeTime) {
        float min = Mathf.Max(0f, baseTime - randomizeTime);
        float max = baseTime + randomizeTime;
        return Random.Range(min, max);
    }

    private class Bolt {

        //decay
        float time;
        float lifeTime;

        //reference
        LineRenderer lr;

        public bool decayed { get { return time >= lifeTime; } }

        public Bolt(GameObject g, LineRenderer boltTemplate) {
            lr = UFUtils.AddComponent(g, boltTemplate);
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

            Vector2 randGlobal = Random.insideUnitCircle;
            lr.transform.LookAt(target);
            float distance = (target.position - lr.transform.position).magnitude;
            int nboPts = lr.positionCount;
            
            for(int i = 0; i < nboPts; i++) {
                float t = i / (nboPts - 1);
                float r = boltShape.Evaluate(t);
                float z = distance * t;
                Vector2 randLocal = Random.insideUnitCircle;

                Vector2 pos = randGlobal * r + randLocal * jitter;
                lr.SetPosition(i, new Vector3(pos.x, pos.y, z));
            }
        }
    }
}
