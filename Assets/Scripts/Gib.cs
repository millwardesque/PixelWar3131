using UnityEngine;
using System.Collections;

public class Gib : MonoBehaviour {
	public float lifetime = 5f;
	private float lifetimeRemaining;

	// Use this for initialization
	void Start () {
		lifetimeRemaining = lifetime;
	}
	
	// Update is called once per frame
	void Update () {
		lifetimeRemaining -= Time.deltaTime;

		if (lifetimeRemaining <= 0f) {
			Destroy (gameObject);
		}
	}
}
