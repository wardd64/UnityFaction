using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class PlayerTool : MonoBehaviour {

    protected Animator anim;

    //0: necessary tool, 1: standard tool, 2: extra tool
    public int priority;
    public LayerMask targetMask;
    public bool rcDetectsTriggers;

    protected const float RAY_MAX = 1e+3f;
    protected bool amplified;
    protected PlayerMovement player { get { return GetComponentInParent<PlayerMovement>(); } }
    protected Camera fpCamera { get { return GetComponentInParent<Camera>(); } }
    protected AudioSource sound;

    protected virtual void Awake() {
        anim = this.GetComponent<Animator>();
        sound = this.GetComponent<AudioSource>();
    }

    public bool CanUse() {
        int diff = Global.save.difficulty;
        return priority + diff < 3;
    }

    protected RaycastHit Raycast(Transform aimRig) {
        RaycastHit bestHit = new RaycastHit();
        bestHit.distance = RAY_MAX;

        Transform rayPoint = fpCamera.transform;

        Ray ray = new Ray(rayPoint.position, rayPoint.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, RAY_MAX, targetMask);

        foreach(RaycastHit hit in hits) {
            if(!rcDetectsTriggers && hit.collider.isTrigger)
                continue;

            if(hit.collider.GetComponentInParent<PlayerMovement>() != null)
                continue;

            if(hit.distance < bestHit.distance)
                bestHit = hit;
        }

        
        Vector3 aimPoint = rayPoint.position + rayPoint.forward * RAY_MAX;
        float aimDist = RAY_MAX;
        float dist = bestHit.distance;
        if(dist < RAY_MAX) {
            aimPoint = bestHit.point;

            //keep aim point a minimum distance away from the camera
            if(bestHit.distance < 1f) {
                dist = (dist * dist + 1) / 2f;
                aimPoint = ray.origin + dist * ray.direction;
            }

            aimDist = Vector3.Distance(aimPoint, aimRig.position);
        }

        aimRig.localScale = new Vector3(1f, 1f, aimDist);
        aimRig.LookAt(aimPoint);

        return bestHit;
    }

    public void SetAmplified(bool value) {
        amplified = value;
    }

    abstract public void DoUpdate(bool mainFire, bool alt);
    
}
