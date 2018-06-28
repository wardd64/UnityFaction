using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ragdoll : MonoBehaviour {

	//live body and ragdoll connections
	public GameObject animatedBody, ragdollBody;
	public Transform ragHip;

	//body parts of the ragdoll and their equivalents on the live body
	public Rigidbody[] ragParts;
	public Transform[] animatedParts;

	//recorded info on the live body
	Vector3[] partPos;
	Vector3[] partVel;
	Quaternion[] partRot;

	bool ragdolled;
	public bool isRagdolled { get { return ragdolled; } }

	void Start() {
		//make sure ragdoll doesn't get caught in character controller
		CharacterController cc = this.GetComponent<CharacterController>();
		if(cc != null) {
			foreach(Rigidbody body in ragParts) {
				Collider coll = body.GetComponent<Collider>();
				Physics.IgnoreCollision(coll, cc);
			}
		}

		//initialize position and rotation recording
		int nb = animatedParts.Length;
		partPos = new Vector3[nb];
		partRot = new Quaternion[nb];
		partVel = new Vector3[nb];

		//set alive state
		SetRagdollState(false);
	}

    private void LateUpdate() {
        if(ragdolled) {
            //check if the ragdoll is sleeping partially, which causes faulty physics
            bool allAwake = true, allAsleep = true;
            for(int i = 0; i < ragParts.Length; i++) {
                bool sleep = ragParts[i].IsSleeping();
                allAwake &= !sleep;
                allAsleep &= sleep;

                //mark stored values as outdated
                if(!sleep)
                    partPos[i] = new Vector3(float.NaN, 0f, 0f);
            }
            if(!allAwake && !allAsleep) {
                for(int i = 0; i < ragParts.Length; i++) {
                    if(ragParts[i].IsSleeping()) {
                        if(float.IsNaN(partPos[i].x)) {
                            //store best values to be maintained while sleeping
                            partPos[i] = ragParts[i].position;
                            partRot[i] = ragParts[i].rotation;
                        }
                        //use stored values while sleeping
                        ragParts[i].transform.position = partPos[i];
                        ragParts[i].transform.rotation = partRot[i];
                    }
                }
            }
        }
        else {
            //update part positions so we can initialize ragdoll when needed
            for(int i = 0; i < ragParts.Length; i++) {
                partVel[i] = (animatedParts[i].position - partPos[i]) / Time.deltaTime;
                partPos[i] = animatedParts[i].position;
                partRot[i] = animatedParts[i].rotation;
            }
        }
	}

    /// <summary>
    /// Activates or deactivates appropriate parts, corresponding to 
    /// the given ragdolization states
    /// </summary>
    private void SetRagdollState(bool state) {
		this.ragdolled = state;

		//switch animated character for ragdoll character
		animatedBody.SetActive(!state);
		ragdollBody.SetActive(state);

        //handle character controller, if it is available
        CharacterController cc = this.GetComponent<CharacterController>();
        if(cc != null) {
            cc.enabled = !state;
        }
	}

	/// <summary>
	/// Copy positions and velocities of animated body to the ragdoll, 
	/// to keep transition between the two nice and smooth.
	/// </summary>
	private void InitializeRagdoll() {
        int nbParts = ragParts.Length;

        ragdollBody.transform.position = animatedBody.transform.position;

        for(int i = 0; i < nbParts; i++) {
            ragParts[i].transform.position = partPos[i];
			ragParts[i].transform.rotation = partRot[i];
			if(IsLegalVector(partVel[i]))
				ragParts[i].velocity = partVel[i];
			else
				ragParts[i].velocity = Vector3.zero;
        }
	}

    private static bool IsLegalVector(Vector3 v) {
        if(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z))
            return false;
        if(float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z))
            return false;
        return true;
    }

    /// <summary>
    /// Deactivates animated body and initializes the ragdoll.
    /// </summary>
    public void Ragdollize() {
		SetRagdollState(true);
		InitializeRagdoll();
	}

    /// <summary>
    /// Deactivate ragdoll and reactivate the animated body.
    /// </summary>
    public void Reset() {
        SetRagdollState(false);
    }

    /// <summary>
    /// Limits velocity of all ragdoll parts, to prevent clipping
    /// </summary>
    public void Limit(float max) {
		for(int i = 0; i < ragParts.Length; i++) {
			Vector3 vel = Vector3.ClampMagnitude(ragParts[i].velocity, max);
			ragParts[i].velocity = vel;
		}
	}

	/// <summary>
	/// Apply random force impulse to all parts of the ragdoll.
	/// </summary>
	public void Spasm(float power) {
		for(int i = 0; i < ragParts.Length; i++) {
			Vector3 impulse = power * UnityEngine.Random.insideUnitSphere;
			ragParts[i].AddForce(impulse, ForceMode.Impulse);
		}
	}

	/// <summary>
	/// Applies damped harmonic force on each part of the ragdoll for this frame. 
	/// k is the spring constant and c the damping constant.
	/// </summary>
	public void Harmonic(Vector3 origin, float k, float c) {
		for(int i = 0; i < ragParts.Length; i++) {
			Vector3 springForce = k * (origin - ragParts[i].position);
			Vector3 dampForce = -c * ragParts[i].velocity;
			Vector3 impulse = (springForce + dampForce) * Time.deltaTime;
			ragParts[i].AddForce(impulse, ForceMode.Impulse);
		}
	}

    public void MuteSounds(bool muted) {
        AudioSource[] sounds = ragdollBody.GetComponentsInChildren<AudioSource>();
        foreach(AudioSource s in sounds)
            s.mute = muted;
    }
}
