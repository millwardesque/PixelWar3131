using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum UnitSelectionState {
	Idle,
	Dragging
}

[RequireComponent (typeof(Team))]
public class UnitController : MonoBehaviour {
	public float minDragThreshold = 0.01f;
	public Image selectionBox;
	public float clusterOffsetRadius = 0.5f;

	UnitSelectionState selectionState;
	public UnitSelectionState SelectionState {
		get { return selectionState; }
		set {
			if (value == UnitSelectionState.Dragging) {
				onSelectionUpdate = OnDraggingUpdate;
			}
			else if (value == UnitSelectionState.Idle) {
				onSelectionUpdate = OnIdleUpdate;
			}

			selectionState = value;
		}
	}

	delegate void UnitControllerSelectionUpdateFunction();
	UnitControllerSelectionUpdateFunction onSelectionUpdate;

	List<MoveableUnit> selectedUnits = new List<MoveableUnit>();
	Team myTeam;
	bool isDragging = false;
	Vector2 dragStart;

	void Awake() {
		if (selectionBox == null) {
			Debug.LogError("Unable to awaken unit controller: No selection box is present");
		}

		// Set the selection box to be anchored to the bottom left.
		selectionBox.rectTransform.anchorMin = Vector2.zero;
		selectionBox.rectTransform.anchorMax = Vector2.zero;
		selectionBox.enabled = false;
		
		myTeam = GetComponent<Team>();

		SelectionState = UnitSelectionState.Idle;
	}

	void Start() {
		ClearSelectedUnits();
		SelectionState = UnitSelectionState.Idle;
	}

	// Update is called once per frame
	void Update () {
		if (null != onSelectionUpdate) {
			onSelectionUpdate();
		}

		if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0) {
			Collider2D collider = GetMouseHit();
			
			if (collider != null) {
				Vector2 colliderPosition = collider.transform.position;
				foreach (MoveableUnit unit in selectedUnits) {

					if (IsShootable (unit.gameObject, collider.gameObject)) {
						unit.Target = collider.gameObject;
					}
					else {
						Vector2 randomOffset = new Vector2(Random.Range(-clusterOffsetRadius, clusterOffsetRadius), Random.Range(-clusterOffsetRadius, clusterOffsetRadius));
						unit.MoveToLocation(colliderPosition + randomOffset);
					}
				}
			}
			else {
				Vector3 target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				foreach (MoveableUnit unit in selectedUnits) {
					Vector2 randomOffset = new Vector2(Random.Range(-clusterOffsetRadius, clusterOffsetRadius), Random.Range(-clusterOffsetRadius, clusterOffsetRadius));
					unit.MoveToLocation((Vector2)target + randomOffset);
				}
			}
		}
	}

	bool IsShootable(GameObject shooter, GameObject target) {
		Team shooterTeam = shooter.GetComponent<Team>();
		return (target.GetComponent<Health>() && !shooterTeam.IsOnMyTeam(target.GetComponent<Team>()));
	}

	void OnIdleUpdate() {
		Vector2 mouseWorldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		if (Input.GetMouseButtonDown(0)) {
			StartDragging(mouseWorldPoint);
		}
		else if (Input.GetMouseButton(0) && IsDragging(mouseWorldPoint)) {
			SelectionState = UnitSelectionState.Dragging;
		}
		else if (Input.GetMouseButtonUp(0)) {
			Collider2D other = GetMouseHit();
			
			if (other != null) {
				MoveableUnit otherUnit = other.GetComponent<MoveableUnit>();
				if (otherUnit != null && myTeam.IsOnMyTeam(otherUnit.GetComponent<Team>())) {
					ClearSelectedUnits();
					AddUnitToSelection(otherUnit);
				}
				else {
					ClearSelectedUnits();
				}
			}
			else {
				ClearSelectedUnits();
			}
		}
	}

	void OnDraggingUpdate() {
		Vector2 mouseWorldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		if (Input.GetMouseButtonUp(0)) {
			if (IsDragging(mouseWorldPoint)) {
				EndDragging(mouseWorldPoint);
			}
			else {
				CancelDragging();
			}
			SelectionState = UnitSelectionState.Idle;
		}
		else if (Input.GetMouseButton(0)) {
			UpdateDragging(mouseWorldPoint);
		}
	}

	void StartDragging(Vector2 dragStart) {
		this.isDragging = true;
		this.dragStart = dragStart;
		this.selectionBox.enabled = true;
		selectionBox.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
		selectionBox.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
	}

	bool IsDragging(Vector2 currentPos) {
		if (!isDragging) {
			return false;
		}

		return (currentPos - dragStart).magnitude > minDragThreshold;
	}

	void UpdateDragging(Vector2 mouseWorldPoint) {
		Vector2 bottomLeft, topRight;
		GetDragBounds(dragStart, mouseWorldPoint, out bottomLeft, out topRight);

		// Convert to screen-space points.
		bottomLeft = Camera.main.WorldToScreenPoint(bottomLeft);
		topRight = Camera.main.WorldToScreenPoint(topRight);

		selectionBox.rectTransform.anchoredPosition = bottomLeft;
		selectionBox.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, topRight.x - bottomLeft.x);
		selectionBox.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, topRight.y - bottomLeft.y);
	}

	void EndDragging(Vector2 endDrag) {
		isDragging = false;
		selectionBox.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
		selectionBox.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
		selectionBox.enabled = false;

		Vector2 bottomLeft, topRight;
		GetDragBounds(dragStart, endDrag, out bottomLeft, out topRight);

		MoveableUnit[] units = GameObject.FindObjectsOfType<MoveableUnit>();
		Rect selectionRect = new Rect(bottomLeft, (topRight - bottomLeft));
		ClearSelectedUnits();
		foreach (MoveableUnit unit in units) {
			if (myTeam.IsOnMyTeam(unit.GetComponent<Team>()) && selectionRect.Contains(unit.transform.position)) {
				AddUnitToSelection(unit);
			}
		}
	}

	void CancelDragging() {
		isDragging = false;
		selectionBox.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
		selectionBox.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
		selectionBox.enabled = false;
	}

	void GetDragBounds(Vector2 startWorldPoint, Vector2 endWorldPoint, out Vector2 bottomLeft, out Vector2 topRight) {
		if (endWorldPoint.x < startWorldPoint.x) {
			bottomLeft.x = endWorldPoint.x;
			topRight.x = startWorldPoint.x;
		}
		else {
			bottomLeft.x = startWorldPoint.x;
			topRight.x = endWorldPoint.x;
		}
		
		if (endWorldPoint.y < startWorldPoint.y) {
			bottomLeft.y = endWorldPoint.y;
			topRight.y = startWorldPoint.y;
		}
		else {
			bottomLeft.y = startWorldPoint.y;
			topRight.y = endWorldPoint.y;
		}
	}

	public void AddUnitToSelection(MoveableUnit unit) {
		if (unit == null || selectedUnits.Contains(unit)) {
			return;
		}

		if (unit.GetComponent<SpriteRenderer>()) {
			unit.GetComponent<SpriteRenderer>().color = new Color(0f, 1f, 0f);
		}
		selectedUnits.Add(unit);
	}

	public void ClearSelectedUnits() {
		foreach (MoveableUnit unit in selectedUnits) {
			if (unit != null) {
				unit.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f);
			}
		}

		selectedUnits.Clear();
	}

	Collider2D GetMouseHit() {
		Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		
		string[] interactionLayers = {
			"Team 1",
			"Team 2",
		};
		RaycastHit2D hit = Physics2D.GetRayIntersection(mouseRay, 999f, LayerMask.GetMask(interactionLayers));
		return hit.collider;
	}
}
