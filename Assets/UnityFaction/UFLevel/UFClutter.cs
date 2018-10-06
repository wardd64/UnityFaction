using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFClutter : MonoBehaviour {

    public bool isSwitch;

    //destruction
    public float life;
    public GameObject rubblePrefab;
    public bool rubbleIsSolid; //if true rubble has colliders and remains permanently

    public void Set(Clutter clutter) {
        //double check if this piece of clutter is a switch
        string name = clutter.name.ToLower();
        if(!isSwitch && IsSwitch(name))
            isSwitch = true;

        EnsureColliders();
    }

    private void EnsureColliders() {
        bool hasPrefabColliders = GetComponentInChildren<Collider>() != null;
        if(!hasPrefabColliders) {
            foreach(MeshFilter mf in GetComponentsInChildren<MeshFilter>())
                mf.gameObject.AddComponent<MeshCollider>();
        }
    }

    private static bool IsSwitch(string name) {
        return name.Contains("switch") ||
            name.Contains("console button") ||
            name.Contains("valve wheel");
    }

	public void Activate(bool positive) {
        if(isSwitch) {
            AudioSource sound = GetComponent<AudioSource>();
            if(sound != null)
                sound.Play();
        }
        else
            Debug.LogError("Clutter with unknown functionality was activated: " + name);
    }

    public void Damage(float amount) {
        if(life <= 0f)
            return;

        life -= amount;
        if(life <= 0f)
            Die();
    }

    private void Die() {
        gameObject.SetActive(false);
        if(rubblePrefab != null) {
            Transform parent = transform.parent;
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;
            GameObject g = Instantiate(rubblePrefab, pos, rot, parent);
            UFClutter rubble = g.GetComponent<UFClutter>();
            if(rubble == null)
                rubble = g.AddComponent<UFClutter>();
            rubble.life = 0f;

            if(rubbleIsSolid)
                rubble.EnsureColliders();
            else {
                foreach(Collider c in rubble.GetComponentsInChildren<Collider>())
                    c.enabled = false;
                g.AddComponent<SelfDestruct>().time = 5f;
            }
        }
    }
}
