#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SnazzyScene : MonoBehaviour {

	[HideInInspector] public SceneCameraTransform[] sceneCameras = new SceneCameraTransform[12];
	[HideInInspector] public SelectionSet[] selectionSet = new SelectionSet[12];
	[HideInInspector] public List<SelectedObjects> selectionList = new List<SelectedObjects>();

	public void Init()
	{
		for (int i = 0;i < 12;++i) {
			if (sceneCameras[i] == null) sceneCameras[i] = new SceneCameraTransform();
			if (selectionSet[i] == null) selectionSet[i] = new SelectionSet();
		}
	}
}
#endif