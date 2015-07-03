using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor (typeof(UnitController))]
public class UnitControllerEditor : Editor {
	public override void OnInspectorGUI() {
		UnitController controller = (UnitController) target;
		DrawDefaultInspector();
		
		EditorGUILayout.LabelField("Selection State", controller.SelectionState.ToString());
	}
}
