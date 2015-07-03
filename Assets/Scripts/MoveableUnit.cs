using UnityEngine;
using System.Collections;

public enum MovementState {
	Standing,
	Walking,
	Tracking
};

[RequireComponent (typeof(Rigidbody2D))]
[RequireComponent (typeof(Team))]
public class MoveableUnit : MonoBehaviour {
	public float speed = 1f; // Speed in game units / second
	public Vector2 destination;
	public float attackRadius = 1f;

	private GameObject target;
	public GameObject Target {
		get { return target; }
		set {
			target = value;

			if (target != null) {
				State = MovementState.Tracking;
			}
			else if (State == MovementState.Tracking) {
				State = MovementState.Standing;
			}
		}
	}

	MovementState state = MovementState.Standing;
	public MovementState State {
		get { return state; }
		set {
			if (value == MovementState.Walking) {
				onUpdateFunction = OnWalkingUpdate;
			}
			else if (value == MovementState.Standing) {
				onUpdateFunction = OnStandingUpdate;
			}
			else if (value == MovementState.Tracking) {
				onUpdateFunction = OnTrackingUpdate;
			}
			state = value;
		}
	}

	delegate void MoveableUnitUpdateFunction();
	MoveableUnitUpdateFunction onUpdateFunction;
	Rigidbody2D rb;
	Team myTeam;

	void Awake() {
		rb = GetComponent<Rigidbody2D>();
		myTeam = GetComponent<Team>();
	}

	void Start() {
		State = MovementState.Standing;
	}

	// Update is called once per frame
	void FixedUpdate () {
		if (onUpdateFunction != null) {
			onUpdateFunction();
		}
	}

	GameObject FindNextTarget() {
		MoveableUnit[] units = GameObject.FindObjectsOfType<MoveableUnit>();
		GameObject closest = null;
		float closestDistance = attackRadius;
		foreach (MoveableUnit unit in units) {
			if (unit == this || myTeam.IsOnMyTeam(unit.GetComponent<Team>())) {
				continue;
			}

			float distance = (transform.position - unit.transform.position).magnitude;
			if (distance < closestDistance) {
				closest = unit.gameObject;
				closestDistance = distance;
			}
		}

		return closest;
	}

	public void MoveToLocation(Vector2 location) {
		this.destination = location;
		State = MovementState.Walking;
	}

	void OnStandingUpdate() {
		if (target == null) {
			Target = FindNextTarget();
		}
		else {
			GetComponent<UnitWeapon>().Fire(target.transform.position);
		}
	}

	void OnWalkingUpdate() {
		Vector2 toDestination = destination - (Vector2)transform.position;
		Vector2 newPosition = toDestination.normalized * speed * Time.deltaTime;
		if (toDestination.magnitude < newPosition.magnitude) {
			this.rb.MovePosition((Vector2)this.transform.position + toDestination);

			State = MovementState.Standing;
		}
		else {
			this.rb.MovePosition((Vector2)this.transform.position + newPosition);
		}
	}

	void OnTrackingUpdate() {
		if (target == null) { // In case the target gets destroyed elsewhere, clean up my own state.
			Target = null;
			return;
		}
		Vector2 targetPosition = target.transform.position;
		if ((targetPosition - (Vector2)transform.position).magnitude < attackRadius) {
			GetComponent<UnitWeapon>().Fire(targetPosition);
		}
		else {
			Vector2 toDestination = targetPosition - (Vector2)transform.position;
			Vector2 newPosition = toDestination.normalized * speed * Time.deltaTime;
			this.rb.MovePosition((Vector2)this.transform.position + newPosition);
		}
	}
}
