using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class PlayerTool : MonoBehaviour {

    protected Animator anim;

    //0: necessary tool, 1: standard tool, 2: extra tool
    public int priority;
    public LayerMask targetMask;

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

        Ray forwardRay = new Ray(rayPoint.position, rayPoint.forward);
        RaycastHit[] forwardHits = Physics.RaycastAll(forwardRay, RAY_MAX, targetMask);
        foreach(RaycastHit hit in forwardHits) {
            if(IsValidHit(hit) && hit.distance < bestHit.distance)
                bestHit = hit;
        }

        /* if we find new hits traveling our ray backwards our raycast
         * must have started within a collider. The last and therefore 
         * innermost of those 'backHits' is the most desirable hit.
         */
        Vector3 rayEnd = rayPoint.position + rayPoint.forward * RAY_MAX;
        Ray backRay = new Ray(rayEnd, -rayPoint.forward);
        RaycastHit[] backHits = Physics.RaycastAll(backRay, RAY_MAX, targetMask);
        if(backHits.Length > forwardHits.Length) {
            foreach(RaycastHit backHit in backHits) {
                if(!HitsInclude(forwardHits, backHit) && IsValidHit(backHit))
                    bestHit = backHit;  
            }
        }

        Vector3 aimPoint = rayPoint.position + rayPoint.forward * RAY_MAX;
        float aimDist = RAY_MAX;
        float dist = bestHit.distance;
        if(dist < RAY_MAX) {
            aimPoint = bestHit.point;

            //keep aim point a minimum distance away from the camera
            if(bestHit.distance < 1f) {
                dist = (dist * dist + 1) / 2f;
                aimPoint = forwardRay.origin + dist * forwardRay.direction;
            }

            aimDist = Vector3.Distance(aimPoint, aimRig.position);
        }

        aimRig.localScale = new Vector3(1f, 1f, aimDist);
        aimRig.LookAt(aimPoint);

        return bestHit;
    }

    private bool HitsInclude(RaycastHit[] hits, RaycastHit hit) {
        foreach(RaycastHit r in hits) {
            if(r.collider == hit.collider)
                return true;
        }
        return false;
    }

    public void SetAmplified(bool value) {
        amplified = value;
    }

    private bool IsValidHit(RaycastHit hit) {
        if(hit.collider.isTrigger) {
            if(!this.ValidTrigger(hit.collider))
                return false;
        }

        if(hit.collider.GetComponentInParent<PlayerMovement>() != null)
            return false;

        return true;
    }

    protected virtual bool ValidTrigger(Collider trigger) {
        return false;
    }

    abstract public void DoUpdate(bool mainFire, bool alt);
    
}
