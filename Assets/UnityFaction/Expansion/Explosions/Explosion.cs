using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour {

	void Start () {
        float scale = Vector3.Dot(Vector3.one, transform.localScale) / 3f;
        float size = scale / 5f;
        this.GetComponent<Animator>().SetFloat("Size", size);
        transform.localScale = Vector3.one;
	}
}
