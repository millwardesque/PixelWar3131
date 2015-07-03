using UnityEngine;
using System.Collections;

public class UnitWeapon : MonoBehaviour {
	public Bullet bulletPrefab;
	public GameObject firingPoint;
	public float fireCooldown = 1f;
	private float fireCooldownRemaining;

	void Awake() {
		if (bulletPrefab == null) {
			Debug.Log (string.Format("Weapon for {0} created with no bullet prefab set.", this.name));
		}

		if (firingPoint == null) {
			Debug.Log (string.Format("Weapon for {0} created with no firing point set.", this.name));
		}
	}

	void Update() {
		if (fireCooldownRemaining > 0f) {
			fireCooldownRemaining -= Time.deltaTime;
		}
	}

	public virtual void Fire(Vector2 target) {
		if (CanFire()) {
			Bullet newBullet = Instantiate<Bullet>(bulletPrefab);
			newBullet.transform.position = firingPoint.transform.position;
			newBullet.direction = target - (Vector2)firingPoint.transform.position;
			newBullet.myTeam = GetComponent<Team>();
			fireCooldownRemaining = fireCooldown;
		}
	}

	public bool CanFire() {
		return (bulletPrefab != null && firingPoint != null && fireCooldownRemaining <= 0f);
	}
}
