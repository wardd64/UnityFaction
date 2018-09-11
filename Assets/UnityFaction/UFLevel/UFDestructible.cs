using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;

public class UFDestructible : MonoBehaviour {

    public float life;

    private float damage;

    public void Set(Brush brush) {
        life = brush.life;
    }

    public void DealDamage(float amount) {
        damage += amount;
        if(damage >= life)
            GetDestroyed();
    }

    private void GetDestroyed() {
        gameObject.SetActive(false);
    }

    public float currentLife{ get {
            return Mathf.Max(0f, life - damage);
    }}
	
}
