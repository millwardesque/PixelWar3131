using UnityEngine;
using System.Collections;

[RequireComponent (typeof(Collider2D))]
[RequireComponent (typeof(Rigidbody2D))]
[RequireComponent (typeof(Team))]
public class Bullet : MonoBehaviour {
	public int damage = 1;
	public float speed = 1f;
	public Vector2 direction;
	public Team myTeam;
	public float maxLifespan = 10f;
	private float currentLifespan;
	private Rigidbody2D rb;

	void Awake() {
		rb = GetComponent<Rigidbody2D>();
		myTeam = GetComponent<Team>();
	}

	void Start() {
		currentLifespan = maxLifespan;
		rb.velocity = speed * direction.normalized;
		gameObject.layer = LayerMask.NameToLayer("Team " + myTeam.teamNumber);
	}

	void Update() {
		currentLifespan -= Time.deltaTime;
		if (currentLifespan <= 0f) {
			DestroyMe ();
		}
	}

	void DestroyMe() {
		GameObject.Destroy(gameObject);
	}

	void OnCollisionEnter2D(Collision2D col) {
		if (col.collider.GetComponent<Bullet>() != null) {
			return;
		}

		if (col.collider.GetComponent<Health>()) {
			col.collider.GetComponent<Health>().AdjustHealth(-damage);
		}

		DestroyMe();
	}
}
