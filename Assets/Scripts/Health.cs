﻿using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour {
	public GameObject bloodPrefab;
	public int maxHealth;
	private int currentHealth;

	public int CurrentHealth {
		get { return currentHealth; }
	}

	void Awake() {
		currentHealth = maxHealth;

		if (bloodPrefab == null) {
			Debug.LogError (string.Format("Unable to awaken {0}: No blood prefab set.", name));
		}
	}

	public void AdjustHealth(int change) {
		currentHealth += change;

		OnHealthChanged(change);

		if (currentHealth <= 0) {
			OnDeath ();
		}
	}

	protected virtual void OnHealthChanged(int change) {
		if (bloodPrefab != null) {
			GameObject blood = Instantiate<GameObject>(bloodPrefab);
			blood.transform.position = transform.position;
			Vector2 direction = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
			blood.GetComponent<Rigidbody2D>().AddForce(direction * 0.3f, ForceMode2D.Impulse);
		}
		Debug.Log (string.Format ("{0}'s HP changed from {1} to {2}.", this.name, currentHealth - change, currentHealth));
	}

	protected virtual void OnDeath() {
		Debug.Log (string.Format ("{0} just died.", this.name));
		Destroy(gameObject);
	}
}
