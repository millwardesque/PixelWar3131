using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof(Health))]
public class HealthEditor : Editor {
	public override void OnInspectorGUI() {
		Health health = (Health) target;
		DrawDefaultInspector();
		
		EditorGUILayout.LabelField("Current HP", health.CurrentHealth.ToString());
	}
}