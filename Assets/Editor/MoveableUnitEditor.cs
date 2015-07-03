using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof(MoveableUnit))]
public class MoveableUnitEditor : Editor {
	public override void OnInspectorGUI() {
		MoveableUnit unit = (MoveableUnit) target;
		DrawDefaultInspector();

		EditorGUILayout.LabelField("State", unit.State.ToString());
	}
}
