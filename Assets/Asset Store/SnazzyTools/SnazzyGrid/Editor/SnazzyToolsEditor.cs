using UnityEditor;
using UnityEngine;
#if UNITY_4_6 || UNITY_5_0
using UnityEngine.UI;
#endif 
using System.Collections.Generic;

public static class MenuItemSnazzyOffset {
	
	[MenuItem("GameObject/SnazzyGrid/Offset Object", false, 0)]
	static void SnazzyGridOffset() {
		if (SnazzyToolsEditor.snazzy != null) {
			SnazzyToolsEditor.snazzy.offsetObject = Selection.activeTransform; 
			EditorApplication.RepaintHierarchyWindow();
		}
		else {
			Debug.Log ("The SnazzyGrid window needs to be open for this to work");
		}
	}
	
	[MenuItem("GameObject/SnazzyGrid/Offset Reset", false, 0)]
	static void SnazzyGridOffsetReset() {
		if (SnazzyToolsEditor.snazzy != null) {
			SnazzyToolsEditor.snazzy.offsetObject = null; 
			EditorApplication.RepaintHierarchyWindow();
		}
		else {
			Debug.Log ("The SnazzyGrid window needs to be open for this to work");
		}
	}
}

public class SnazzyToolsEditor: EditorWindow
{
	static public string installPath = ""; 
	static MeshRenderer snazzyGridRenderer;
	static GameObject snazzyGrid;
	static GameObject camReference;
	static GameObject snazzySettings;
	static GameObject _parent;
	static public SnazzySettings snazzy;
	static public SnazzyScene snazzyS;

	static bool s;
	static Event key;
	static SnazzyToolsEditor window;

	static int controlID;
	static bool rotHandle = false;
	static bool scaleHandler = false;
	static bool snapLock = false;
	static string notify = "";
	static Camera sceneCamera;
	static float cameraXAxis;
	static Vector2 rotationOld;
	static bool keySnap;	

	static bool uGUISelect = false;
	static Vector2 pivotOld;
	static Vector3 gridOffset;

	#if UNITY_4_6 || UNITY_5_0
	static RectTransform rectT;
	#endif
	
	float tipX = 0;
	int screenWidthOld = 0;
	Texture Button;
	string tooltip = "";
	bool tooltipOld;
	bool resizeWindow = false;
	
	[MenuItem("Window/SnazzyTools/SnazzyGrid2")]
	public static void Init()
	{
		window = GetWindow(typeof(SnazzyToolsEditor)) as SnazzyToolsEditor;
		window.title = "SG";
		window.minSize = new Vector2(771,566);
		window.minSize = new Vector2(66,110);
		window.Show();
		window.LoadButtons();
	}

	void OnEnable()
	{
		// Debug.Log("Enable");
		GetInstallPath();
		InitReferences();
		SceneView.onSceneGUIDelegate -= OnScene;
		EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchy;
		EditorApplication.projectWindowItemOnGUI -= OnProject;

		SceneView.onSceneGUIDelegate += OnScene;
		EditorApplication.hierarchyWindowItemOnGUI += OnHierarchy;
		EditorApplication.projectWindowItemOnGUI += OnProject;
		GetOldPSR();;
	}
	
	void OnDisable()
	{
		// Debug.Log("Disable");
		// ClearSnazzyGrid();
		HideGrid ();

		SceneView.onSceneGUIDelegate -= OnScene;
		EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchy;
		EditorApplication.projectWindowItemOnGUI -= OnProject;
	}
	
	public void OnDestroy()
	{
		// Debug.Log ("Destroy");
		// ClearSnazzyGrid();
		SceneView.onSceneGUIDelegate -= OnScene;
		EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchy;
		EditorApplication.projectWindowItemOnGUI -= OnProject;
	}

	public void GetInstallPath()
	{
		installPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
		int index = installPath.LastIndexOf("/Editor");
		
		installPath = installPath.Substring(0,index);
		// Debug.Log (installPath);
		// TC.fullInstallPath = Application.dataPath.Replace("Assets","")+TC.installPath;
	}
	
	static void OnScene(SceneView sceneView)
	{
		if (snazzy == null || snazzyGrid == null) {InitReferences();}
		key = Event.current;

		sceneCamera = Camera.current;
//		if (sceneCamera != null) 
//			Handles.Label (sceneCamera.transform.position+(sceneCamera.transform.forward*1),new GUIContent(sceneCamera.transform.rotation.eulerAngles.ToString()));

		Key(key,true);

		controlID = GUIUtility.hotControl;

		// Debug.Log (controlID);

		if (snazzy.lookAtSelect) {
			if (Selection.activeTransform != null) {
				// Debug.Log ("dfd");
				if (controlID == 0) sceneView.pivot = Selection.activeTransform.position;
				if (key.type == EventType.MouseDown) {
					if (Mathf.Abs (sceneCamera.transform.localEulerAngles.z) > 175 && Mathf.Abs (sceneCamera.transform.localEulerAngles.z) < 185) 
						rotationOld = new Vector2(-sceneCamera.transform.localEulerAngles.x-sceneCamera.transform.localEulerAngles.z,sceneCamera.transform.localEulerAngles.y+sceneCamera.transform.localEulerAngles.z);
					else 
						rotationOld = new Vector2(sceneCamera.transform.localEulerAngles.x,sceneCamera.transform.localEulerAngles.y);
				}
				else if (key.button == 1) {
					sceneCamera.transform.localEulerAngles = new Vector3(DeltaAngle(sceneCamera.transform.localEulerAngles.x),DeltaAngle (sceneCamera.transform.localEulerAngles.y),DeltaAngle (sceneCamera.transform.localEulerAngles.z));
					rotationOld += new Vector2(key.delta.y/10,key.delta.x/10);
					Quaternion rotation = Quaternion.Euler(rotationOld.x,rotationOld.y,0.0f);
					Vector3 position = rotation * new Vector3(0,0,-2) + Selection.activeTransform.position;
					
					sceneCamera.transform.rotation = rotation;
					sceneCamera.transform.position = position;
					// sceneView.Repaint ();
				}	
			}
			if (key.button == 2) {
				snazzy.lookAtSelect = false;
				SnazzyWindowRepaint();
			}
		}

		// Display Scene Notification
		if (notify.Length > 0) { 
			sceneView.ShowNotification(new GUIContent(notify));
			notify = "";
		}

		if (Selection.activeTransform != null) {
			snazzyGrid.transform.position = Selection.activeTransform.position;//gridOffset;
			if (Selection.activeTransform != snazzy.oldTransform) {
				snazzy.oldPosition = Selection.activeTransform.position;
			}
		}

		if (snazzy.lockChild) {
			if (Selection.activeTransform != snazzy.oldTransform) ParentChildren();	
		}

		if (snazzy.showGrid) {
			if (!snazzy.gridIsEnabled) ShowGrid();
		}
		else {
			if (snazzy.gridIsEnabled) HideGrid();
		}
		if (Selection.transforms.Length == 0) {
			if (snazzy.gridIsEnabled) HideGrid();
		}
		
		if (snazzyGrid != null)	{
			if (snazzy.mov2) snazzyGridRenderer.sharedMaterial.SetFloat("_Scale", snazzy.gridIncrement2*snazzy.gridSize);
				else snazzyGridRenderer.sharedMaterial.SetFloat("_Scale", snazzy.gridIncrement*snazzy.gridSize);
			
			snazzyGrid.transform.localScale = new Vector3(snazzy.objectScale*snazzy.moveAmount,snazzy.objectScale*snazzy.moveAmount,snazzy.objectScale*snazzy.moveAmount);
			snazzyGridRenderer.sharedMaterial.SetFloat("_SnapAreaSize", snazzy.objectScale/(snazzy.objectScale*snazzy.snapAreaSize));
			snazzyGridRenderer.sharedMaterial.SetFloat("_ObjectScale", snazzy.objectScale*snazzy.moveAmount);
			snazzyGridRenderer.sharedMaterial.SetFloat("_GridSteps", snazzy.gridSize);
			Vector3 gridOffset2 = -(gridOffset/snazzy.gridSize)+new Vector3(snazzy.snapOffset,snazzy.snapOffset,snazzy.snapOffset);
			snazzyGridRenderer.sharedMaterial.SetVector("_SnapOffset",gridOffset2);
			snazzyGridRenderer.sharedMaterial.SetVector("_GridOffset",gridOffset2);

			if (Camera.current != null) {
				// Vector3 cameraForward = Camera.current.transform.forward;
				//snazzyGridRenderer.sharedMaterial.SetFloat("_X", cameraForward.x);
				//snazzyGridRenderer.sharedMaterial.SetFloat("_Y", cameraForward.y);
				//snazzyGridRenderer.sharedMaterial.SetFloat("_Z", cameraForward.z);
				snazzyGridRenderer.sharedMaterial.SetFloat("_Fresnel", snazzy.viewDependency);
			}
		}
	}

	static public void CheckSnap()
	{
		if (key == null) return;
		
		controlID = GUIUtility.hotControl;
		
		if (Selection.activeTransform != null) 
		{
			if (key.keyCode == KeyCode.V) snapLock = true;
			
			if (snazzy.autoSnap) {
				if (Selection.activeTransform == snazzy.oldTransform && !snapLock) {
					if (snazzy.autoSnapRot) {
						if (controlID != 0 && !rotHandle) {
							if (Selection.activeTransform.eulerAngles != snazzy.oldRotation) rotHandle = true;
						}
						else if (controlID == 0 && rotHandle) {
							SnapRot(snazzy.rotAmount,snazzy.autoSnapRotX,snazzy.autoSnapRotY,snazzy.autoSnapRotZ);
							rotHandle = false;
						}
					}
					
					if (snazzy.autoSnapPos)
					{
						if (Selection.activeTransform.position != snazzy.oldPosition && !rotHandle && (controlID != 0 || keySnap)) {
							// Debug.Log (snazzy.oldPosition+" : "+Selection.activeTransform.position);
							if (uGUISelect == false) {
								// Debug.Log ("!");
								SnapPos(snazzy.autoSnapPosX,snazzy.autoSnapPosY,snazzy.autoSnapPosZ);
							}
						}
					}
					
					if (snazzy.autoSnapScale) {
						if (Selection.activeTransform.localScale != snazzy.oldScale && (controlID != 0 || keySnap)) {
							if (Selection.transforms.Length == 1) {
								SnapScale(snazzy.autoSnapScaleX,snazzy.autoSnapScaleY,snazzy.autoSnapScaleZ);
								scaleHandler = false;
							}
							else if (controlID != 0 && !scaleHandler) scaleHandler = true;
						}
						else if (controlID == 0 && scaleHandler) {
							SnapScale(snazzy.autoSnapScaleX,snazzy.autoSnapScaleY,snazzy.autoSnapScaleZ);
							scaleHandler = false;
						}
					}
				}
				GetOldPSR();
			}
			
			snazzy.oldTransform = Selection.activeTransform;
		}
	}
	
	public void OnGUI()
	{
		if (snazzy == null) InitReferences ();
		if (AngleLabel == null) LoadButtons ();
		EditorGUI.DrawPreviewTexture(new Rect(0,0,Screen.width,Screen.height),BackGround);
		// Debug.Log (Screen.width+","+Screen.height);

		// Debug.Log (gridOffset);

		key = Event.current;

		if (Screen.width > 275) {
			GUI.color = new Color(1,1,1,2f-(tipX/135f));
			
			GUI.DrawTexture(new Rect(tipX,0,765+6,662),Quicktips);
			
			if (Vector2.Distance(new Vector2(491+tipX,42),key.mousePosition) < 40) {
				GUI.DrawTexture(new Rect(451+tipX,2,80,80),ManualHover);
				if (key.type == EventType.mouseDown) Application.OpenURL(Application.dataPath.Replace ("Assets","")+installPath+"/SnazzyGridManual.pdf");
			}
			
			if (Vector2.Distance(new Vector2(706+tipX,213),key.mousePosition) < 40) {
				GUI.DrawTexture(new Rect(666+tipX,173,81,80),ForumHover);
				if (key.type == EventType.mouseDown) Application.OpenURL ("http://forum.unity3d.com/threads/snazzytools-snazzygrid-much-more-than-just-a-grid.253799/");
			}
			
			Repaint ();
			
			GUI.color = Color.white;
			GUI.DrawTexture(new Rect(Screen.width-40,0,40,Screen.height),GradientFade);
			
			if (screenWidthOld != Screen.width) {
				if (Screen.width > 70 || tipX > 0) {
					
					tipX = (400f-(((float)Screen.width/1.1f))/2f);
					if (tipX < 64) tipX = 64;
					Repaint();
				}
			}
		}
		
		if (screenWidthOld != Screen.width) {
			tooltipOld = snazzy.tooltip;
			snazzy.tooltip = false;
			resizeWindow = true;
		}
		else {
			if (resizeWindow) {
				snazzy.tooltip = tooltipOld;	
				resizeWindow = false;
			}
		}
		
		Key(key,false);
			
		screenWidthOld = Screen.width;

		int y = 0;
		
		if (snazzy.tooltip) tooltip = "Double-Click this logo to open the manual.\n\nRight-Click to go to the SnazzyGrid forum.\n\nHi, SnazzyGrid is designed to save you some time and make the work with Unity generally more enjoyable. If you find SnazzyTools helpful we would appreciate if you spend some of your saved time to leave a rating/review on the Assetstore. Also, if you have ideas on how to improve SnazzyTools please feel encouraged to visit us on the Unity Forum thread (Link in Settings/Help). We wish you a snazzy workflow and good luck with your work.";
			else tooltip = "";
		EditorGUI.LabelField(new Rect(-1,0+y,64+6,43+6),new GUIContent(SnazzyTitle,tooltip));
		if (new Rect(-1,0+y,64+6,43+6).Contains(key.mousePosition)) {
			if (key.clickCount == 2) Application.OpenURL(Application.dataPath.Replace ("Assets","")+installPath+"/SnazzyGridManual.pdf");
			if (key.button == 1 && key.type == EventType.mouseDown) {
				Application.OpenURL ("http://forum.unity3d.com/threads/snazzytools-snazzygrid-much-more-than-just-a-grid.253799/");
			}
		}
		/*
		if (GUILayout.Button ("Display")) {
			GameObject[] test = Sources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
			foreach (GameObject test1 in test) {
				if (test1.name == "##SnazzyGrid##" || test1.name == "##SnazzySettings##") {
					Debug.Log("found: "+test1.name);p
					test1.hideFlags = HideFlags.None;
				}
			}
		}
		if (GUILayout.Button ("Destroy")) {
			GameObject[] test = Sources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
			foreach (GameObject test1 in test) {
				if (test1.name == "##SnazzyGrid##" || test1.name == "##SnazzySettings##") {
					Debug.Log("found: "+test1.name);
					DestroyImmediate(test1);
				}
			}
		}
		*/

		if (snazzy.showGrid) Button = GridToggleOn; else Button = GridToggleOff;
		if (snazzy.tooltip) tooltip = "Shows/hides the grid.\n<Hotkey: "+GetSpecialKey(snazzy.gridKeySpecial,snazzy.gridKey,false)+">";
		if (GUI.Button (new Rect(5-1,43+y,54+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			snazzy.showGrid = !snazzy.showGrid;
			SceneView.RepaintAll();
		}
		
		GUI.changed = false;
		snazzy.viewDependency = GUI.HorizontalSlider (new Rect(5+2,59+y,54-4,17),snazzy.viewDependency,1,10);
		y -= 2;
		snazzy.snapAreaSize = GUI.HorizontalSlider (new Rect(5+2,76+y,54-4,17),snazzy.snapAreaSize,0,1);
		
		y += 17;
		
		#region Grid
		// offset object
		//GUI.changed = false;
		//snazzy.offsetObject = EditorGUI.ObjectField (new Rect(5+2,76+1+y,54-4,16),snazzy.offsetObject,typeof(Transform),true) as Transform;
		//if (GUI.changed) EditorApplication.RepaintHierarchyWindow();
		//y += 18;

		if (snazzy.tooltip) tooltip = "Cycles through some grid size preset values.\n\nRight-Click to cycle backwards, Left/Middle-Click to cycle forward.";
		if (GUI.Button (new Rect(5-1,76+y,16+6,16+6),new GUIContent(GridSizePreset,tooltip),EditorStyles.label)) {
			if (key.button == 1 && key.button != 2) --snazzy.gridSizeIndex; else ++snazzy.gridSizeIndex;
			if (snazzy.gridSizeIndex < 0) snazzy.gridSizeIndex = snazzy.gridSizePresets.Count-1;
			else if (snazzy.gridSizeIndex > snazzy.gridSizePresets.Count-1) {snazzy.gridSizeIndex = 0;}
			snazzy.gridSize = snazzy.gridSizePresets[snazzy.gridSizeIndex];
			SceneView.RepaintAll();
		}

		GUI.SetNextControlName("GridSize");
		snazzy.gridSize = EditorGUI.FloatField(new Rect(21+4,76+1+y,38-3,16),snazzy.gridSize);
		if (GUI.GetNameOfFocusedControl() == "GridSize") {
			if (key.keyCode == KeyCode.Return) {GUIUtility.keyboardControl = 0;Repaint ();}
			else snazzy.lockKeys = true;
		}
		if (snazzy.gridSize < 0.0001f) snazzy.gridSize = 0.0001f;		
		if (snazzy.tooltip) tooltip = "The increments define how many grid units the selected object/s will move when you use the Move-Buttons/Hotkeys.";
		GUI.Label(new Rect(-1,92+y,64+6,16+6),new GUIContent(IncrementLabel,tooltip));
		
		if (snazzy.mov2) Button = IncrementPreset1ToggleOff; else Button = IncrementPreset1;
		if (snazzy.tooltip) tooltip = "Left-Click toggles between first and second move increment.\n\nLeft/Middle-Click cycles forward through some preset values, Right-Click to cycle backwards.";
		if (GUI.Button (new Rect(5-1,108+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 0 && snazzy.mov2) snazzy.mov2 = false;
			else {
				if (key.button == 1) --snazzy.gridIncrementIndex; 
				else ++snazzy.gridIncrementIndex;
				
				if (snazzy.gridIncrementIndex < 0) snazzy.gridIncrementIndex = snazzy.gridIncrementPresets.Count-1;
				else if (snazzy.gridIncrementIndex > snazzy.gridIncrementPresets.Count-1) {snazzy.gridIncrementIndex = 0;}
				snazzy.gridIncrement = snazzy.gridIncrementPresets[snazzy.gridIncrementIndex];	
			}
			SceneView.RepaintAll();
		}
		if (!snazzy.mov2) Button = IncrementPreset2ToggleOff; else Button = IncrementPreset2;
		GUI.SetNextControlName("GridIncrement");
		snazzy.gridIncrement = EditorGUI.FloatField(new Rect(21+4,108+1+y,38-3,16),snazzy.gridIncrement);
		if (GUI.GetNameOfFocusedControl() == "GridIncrement") {
			if (key.keyCode == KeyCode.Return) {GUIUtility.keyboardControl = 0;Repaint ();}
			else snazzy.lockKeys = true;
		}
		if (snazzy.gridIncrement < 1) snazzy.gridIncrement = 1;
		if (snazzy.tooltip) tooltip = "Left-Click toggles between first and second move increment.\n\nLeft/Middle-Click cycles forward through some preset values, Right-Click to cycle backwards.";
		if (GUI.Button (new Rect(5-1,127+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 0 && !snazzy.mov2) snazzy.mov2 = true;
			else {
				if (key.button == 1) --snazzy.gridMultiIndex;
				else ++snazzy.gridMultiIndex;
				
				if (snazzy.gridMultiIndex < 0) snazzy.gridMultiIndex = snazzy.gridMultiPresets.Count-1;
				else if (snazzy.gridMultiIndex > snazzy.gridMultiPresets.Count-1) {snazzy.gridMultiIndex = 0;}
				snazzy.gridIncrement2 = snazzy.gridMultiPresets[snazzy.gridMultiIndex];
			}
			SceneView.RepaintAll();
		}
		GUI.SetNextControlName("GridIncrement2");
		snazzy.gridIncrement2 = EditorGUI.FloatField(new Rect(21+4,127+1+y,38-3,16),snazzy.gridIncrement2);
		if (GUI.GetNameOfFocusedControl() == "GridIncrement2") {
			if (key.keyCode == KeyCode.Return) {GUIUtility.keyboardControl = 0;Repaint ();}
			else snazzy.lockKeys = true;
		}
		if (snazzy.gridIncrement2 < 1) snazzy.gridIncrement2 = 1;
	
		if (GUI.changed) SceneView.RepaintAll();	
		
		if (snazzy.tooltip) tooltip = "The angles define how many degrees the selected object/s will move when you use the Rotate-Buttons/Hotkeys";
		GUI.Label(new Rect(-1,143+y,39+6,16+6),new GUIContent(AngleLabel,tooltip));
		if (snazzy.rotationMode) Button = RotationModeToggleOn; else Button = RotationModeToggleOff;

		if (snazzy.tooltip) tooltip = "When enabled rotates each selected object around its own pivot (using the rotation keys).";
		if (GUI.Button (new Rect(38,144+y,22+6,14+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			snazzy.rotationMode = !snazzy.rotationMode;
		}
		#endregion
		
		#region Angle
		if (snazzy.angle2) Button = AnglePreset1ToggleOff; else Button = AnglePreset1;
		if (snazzy.tooltip) tooltip = "Left-Click toggles between first and second rotation angle.\n\nLeft/Middle-Click cycles forward through some preset values, Right-Click to cycle backwards.";
		if (GUI.Button (new Rect(5-1,159+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 0 && snazzy.angle2) snazzy.angle2 = false;
			else {
				if (key.button == 1) --snazzy.rotIndex;
				else ++snazzy.rotIndex;
				
				if (snazzy.rotIndex < 0) snazzy.rotIndex = snazzy.rotPresets.Count-1;
				else if (snazzy.rotIndex > snazzy.rotPresets.Count-1) snazzy.rotIndex = 0;
				snazzy.rotIncrement = snazzy.rotPresets[snazzy.rotIndex];
			}
		}
		GUI.SetNextControlName("RotIncrement");
		snazzy.rotIncrement = EditorGUI.FloatField(new Rect(21+4,159+1+y,38-3,16),snazzy.rotIncrement);
		if (GUI.GetNameOfFocusedControl() == "RotIncrement") {
			if (key.keyCode == KeyCode.Return) {GUIUtility.keyboardControl = 0;Repaint ();}
			else snazzy.lockKeys = true;
		}
		if (snazzy.rotIncrement < 1) snazzy.rotIncrement = 1;		
		
		if (!snazzy.angle2) Button = AnglePreset2ToggleOff; else Button = AnglePreset2;
		if (snazzy.tooltip) tooltip = "Left-Click toggles between first and second rotation angle.\n\nLeft/Middle-Click cycles forward through some preset values, Right-Click to cycle backwards.";
		if (GUI.Button (new Rect(5-1,178+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 0 && !snazzy.angle2) snazzy.angle2 = true;
			else {
				if (key.button == 1) --snazzy.rotMultiIndex;
				else ++snazzy.rotMultiIndex;
				
				if (snazzy.rotMultiIndex < 0) snazzy.rotMultiIndex = snazzy.rotMultiPresets.Count-1;
				else if (snazzy.rotMultiIndex > snazzy.rotMultiPresets.Count-1) snazzy.rotMultiIndex = 0;	
				snazzy.rotIncrement2 = snazzy.rotMultiPresets[snazzy.rotMultiIndex];
			}
		}
		GUI.SetNextControlName("RotIncrement2");
		snazzy.rotIncrement2 = EditorGUI.FloatField(new Rect(21+4,178+1+y,38-3,16),snazzy.rotIncrement2);
		if (GUI.GetNameOfFocusedControl() == "RotIncrement2") {
			if (key.keyCode == KeyCode.Return) {GUIUtility.keyboardControl = 0;Repaint ();}
			else snazzy.lockKeys = true;
		}
		if (snazzy.rotIncrement2 < 1) snazzy.rotIncrement2 = 1;	

		if (snazzy.rotAxis == 0) Button = RotationXToggleOn; else Button = RotationXToggleOff;
		if (snazzy.tooltip) tooltip = "Rotate the selected object/s around the X axis. \n<Axis select hotkey: "+GetSpecialKey (snazzy.rotXYZKeySpecial,snazzy.rotXYZKey,false)+">";
		if (GUI.Button (new Rect(5-1,197+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			snazzy.rotAxis = 0;
		}
		
		if (snazzy.rotAxis == 1) Button = RotationYToggleOn; else Button = RotationYToggleOff;
		if (snazzy.tooltip) tooltip = "Rotate the selected object/s around the Y axis.\n<Axis select hotkey: "+GetSpecialKey (snazzy.rotXYZKeySpecial,snazzy.rotXYZKey,false)+">";
		if (GUI.Button (new Rect(5-1+19,197+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			snazzy.rotAxis = 1;
		}
		
		if (snazzy.rotAxis == 2) Button = RotationZToggleOn; else Button = RotationZToggleOff;
		if (snazzy.tooltip) tooltip = "Rotate the selected object/s around the Z axis.\n<Axis select hotkey: "+GetSpecialKey (snazzy.rotXYZKeySpecial,snazzy.rotXYZKey,false)+">";
		if (GUI.Button (new Rect(5-1+38,197+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			snazzy.rotAxis = 2;
		}
		
		GUI.Label(new Rect(-1,213+y,64+6,13+6),new GUIContent(Spacer));
		#endregion
		
		#region MoveKeys
		if (snazzy.tooltip) tooltip = "Click to rotate the selected object/s to the left.\n<Hotkeys: "+GetSpecialKey(snazzy.rotLeftKeySpecial,snazzy.rotLeftKey,true)+">";
		if (GUI.Button (new Rect(5-1,226+y,16+6,16+6),new GUIContent(RotateLeft,tooltip),EditorStyles.label)) {
			RotateSelectionLeft();
		}
		
		if (snazzy.tooltip) tooltip = "Click to move the selected object/s up.\n<Hotkeys: "+GetSpecialKey(snazzy.upKeySpecial,snazzy.upKey,true)+">";		
		if (GUI.Button (new Rect(24-1,226+y,16+6,16+6),new GUIContent(MoveUp,tooltip),EditorStyles.label)) {
			MoveSelectionUp();
		}
		
		if (snazzy.tooltip) tooltip = "Click to rotate the selected object/s to the right.\n<Hotkeys: "+GetSpecialKey (snazzy.rotRightKeySpecial,snazzy.rotRightKey,true)+">";
		if (GUI.Button (new Rect(43-1,226+y,16+6,16+6),new GUIContent(RotateRight,tooltip),EditorStyles.label)) {
			RotateSelectionRight();
		}
		
		if (snazzy.tooltip) tooltip = "Click to move the selected object/s to the left.\n<Hotkeys: "+GetSpecialKey (snazzy.leftKeySpecial,snazzy.leftKey,true)+">";
		if (GUI.Button (new Rect(5-1,245+y,16+6,16+6),new GUIContent(MoveLeft,tooltip),EditorStyles.label)) {
			MoveSelectionLeft();
		}
		
		if (snazzy.tooltip) tooltip = "Click to move the selected object/s down.\n<Hotkeys: "+GetSpecialKey(snazzy.downKeySpecial,snazzy.downKey,true)+">";
		if (GUI.Button (new Rect(24-1,245+y,16+6,16+6),new GUIContent(MoveDown,tooltip),EditorStyles.label)) {
			MoveSelectionDown();
		}
		
		if (snazzy.tooltip) tooltip = "Click to move the selected object/s to the right.\n<Hotkeys: "+GetSpecialKey (snazzy.rightKeySpecial,snazzy.rightKey,true)+">";
		if (GUI.Button (new Rect(43-1,245+y,16+6,16+6),new GUIContent(MoveRight,tooltip),EditorStyles.label)) {
			MoveSelectionRight();
		}
		
		if (snazzy.tooltip) tooltip = "Click to move the selected object/s forward.\n<Hotkeys: "+GetSpecialKey (snazzy.frontKeySpecial,snazzy.frontKey,true)+">";
		if (GUI.Button (new Rect(5-1,264+y,16+6,16+6),new GUIContent(MoveForward,tooltip),EditorStyles.label)) {
			MoveSelectionForward();
		}
		
		if (snazzy.tooltip) tooltip = "Click to move the selected object/s back.\n<Hotkeys: "+GetSpecialKey (snazzy.backKeySpecial,snazzy.backKey,true)+">";
		if (GUI.Button (new Rect(24-1,264+y,16+6,16+6),new GUIContent(MoveBack,tooltip),EditorStyles.label)) {
			MoveSelectionBack();
		}
		
		if (snazzy.tooltip) tooltip = "Duplicates the selected object/s.\n<Hotkey: "+GetSpecialKey (snazzy.duplicateKeySpecial,snazzy.duplicateKey,false)+">";
		if (GUI.Button (new Rect(43-1,264+y,16+6,16+6),new GUIContent(Duplicate,tooltip),EditorStyles.label)) {
			DuplicateSelection(false);
		}	
		
		if (snazzy.lookAtSelect) Button = FocusToggleOn; else Button = FocusToggleOff;
		if (snazzy.tooltip) tooltip = "Keeps the selected object in camera focus.\n<Hotkey: "+GetSpecialKey(snazzy.focusKeySpecial,snazzy.focusKey,false)+">\n\nMiddle-Click to disable focus.\n\nRight-Click and drag to rotate around the selected object.";
		if (GUI.Button (new Rect(5-1,283+y,54+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			snazzy.lookAtSelect = !snazzy.lookAtSelect;
			SceneView.RepaintAll();
		}
	
		y += 22;
			
		GUI.changed = false;
		if (GetHorizontalMode()) GUI.color = Color.green; else GUI.color = Color.white;
		snazzy.cameraAngleOffset = GUI.HorizontalSlider (new Rect(7,277+y,54-4,17),snazzy.cameraAngleOffset,45,0);
		if (snazzy.cameraAngleOffset >= 45) snazzy.cameraAngleOffset = 90;
		
		if (GUI.changed) {
			if (key.button == 1) {
				if (sceneCamera != null) {
					snazzy.cameraAngleOffset = Mathf.Abs(Mathf.DeltaAngle(0,sceneCamera.transform.localEulerAngles.x));
				}
			}
			notify = "Free- to Horizontal Mode switching angle "+Mathf.Round (snazzy.cameraAngleOffset).ToString()+(char)176;
			SceneView.RepaintAll();
		}
		GUI.color = Color.white;
		
		y += 2;
		// GUI.Label(new Rect(-1,280+y,64+6,13+6),new GUIContent(Spacer));
		#endregion
		
		#region Snap
		
		// Transform Snap
		if (snazzy.autoSnap) Button = SnapToggleOn; else Button = SnapToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables auto snapping. Single tap hotkey will toggle, hold will toggle auto snapping back upon releasing hotkey.\nSingle tap <HotKey: "+GetSpecialKey(snazzy.snapKeySpecial,snazzy.snapKey,false)+">\n\nMiddle-Click will snap the transform of the selected object/s according to the snap settings.\nDouble tap <Hotkey: "+GetSpecialKey(snazzy.snapKeySpecial,snazzy.snapKey,false)+">\n\nRight-Click will reset the transform of the selected object/s\n<Hotkey: "+GetSpecialKey(snazzy.resetTransformKeySpecial,snazzy.resetTransformKey,false)+">";
		if (GUI.Button (new Rect(5-1,293+y,54+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 1) {
				ResetPosition (true,true,true);	
				ResetRotation (true,true,true);
				ResetScale (true,true,true);
			}
			else if (key.button == 2){
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.RecordObjects(Selection.transforms,"Selection Snap Transform");
				#else
				Undo.RegisterUndo(Selection.transforms,"Selection Snap Transform");
				#endif
				SnapTransform();
			}
			else ToggleSnap();
		}
		// Position Snap
		if (snazzy.autoSnapPos) Button = PositionToggleOn; else Button = PositionToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables position snapping. Single tap hotkey will toggle, hold will toggle position snapping back upon releasing hotkey.\nSingle tap <HotKey: "
			+GetSpecialKey(snazzy.snapPosKeySpecial,snazzy.snapPosKey,false)+">\n\nMiddle-Click will snap the position of the selected object/s according to the selected axis.\nDouble tap <HotKey: "
				+GetSpecialKey(snazzy.snapPosKeySpecial,snazzy.snapPosKey,false)+">\n\nRight-Click will reset the position of the selected object/s.\n<Hotkey: "
				+GetSpecialKey(snazzy.resetPositionKeySpecial,snazzy.resetPositionKey,false)+">";
		if (GUI.Button (new Rect(5-1,312+y,54+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 1) {
				ResetPosition (true,true,true);	
			}
			else if (key.button == 2) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.RecordObjects(Selection.transforms,"Selection Snap Position");
				#else
				Undo.RegisterUndo(Selection.transforms,"Selection Snap Position");
				#endif
				SnapPos(snazzy.autoSnapPosX,snazzy.autoSnapPosY,snazzy.autoSnapPosZ);
			}
			else {
				snazzy.autoSnapPos = !snazzy.autoSnapPos;
				if (snazzy.autoSnapPos) GetOldPSR();;
			}
		}
		
		if (snazzy.autoSnapPosX) Button = PositionXToggleOn; else Button = PositionXToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables position snapping for X axis.\n\nMiddle-Click will snap Position-X for selected object/s.\n\nRight-Click will reset only Position-X of the selected object/s.";
		if (GUI.Button (new Rect(5-1,331+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 1) ResetPosition (true,false,false);
			else if (key.button == 2) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.RecordObjects(Selection.transforms,"Selection Snap Position X Axis");
				#else
				Undo.RegisterUndo(Selection.transforms,"Selection Snap Position X Axis");
				#endif
				SnapPos (true,false,false);
			}				
 			else snazzy.autoSnapPosX = !snazzy.autoSnapPosX;
		}
		
		if (snazzy.autoSnapPosY) Button = PositionYToggleOn; else Button = PositionYToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables position snapping for Y axis.\n\nMiddle-Click will snap Position-Y for selected object/s.\n\nRight-Click will reset only Position-Y of the selected object/s.";
		if (GUI.Button (new Rect(5-1+19,331+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 1) ResetPosition (false,true,false); 
			else if (key.button == 2) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.RecordObjects(Selection.transforms,"Selection Snap Position Y Axis");
				#else
				Undo.RegisterUndo(Selection.transforms,"Selection Snap Position Y Axis");
				#endif
				SnapPos(false,true,false);
			}
			else snazzy.autoSnapPosY = !snazzy.autoSnapPosY;
		}
		
		if (snazzy.autoSnapPosZ) Button = PositionZToggleOn; else Button = PositionZToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables position snapping for Z axis.\n\nMiddle-Click will snap Position-Z for selected object/s.\n\nRight-Click will reset only Position-Z of the selected object/s.";
		if (GUI.Button (new Rect(5-1+38,331+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 1) ResetPosition (false,false,true); 
			else if (key.button == 2) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.RecordObjects(Selection.transforms,"Selection Snap Position Z Axis");
				#else
				Undo.RegisterUndo(Selection.transforms,"Selection Snap Position Z Axis");
				#endif
				SnapPos (false,false,true);
			}
			else snazzy.autoSnapPosZ = !snazzy.autoSnapPosZ;
		}
		
		if (!snazzy.autoSnapPos) {
			GUI.Label(new Rect(5-1,331+y,57+6,16+6),new GUIContent(XYZMask));
		}
		
		// Rotation Snap
		if (snazzy.autoSnapRot) Button = RotationToggleOn; else Button = RotationToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables rotation snapping.\nSingle tap hotkey will toggle, hold will toggle rotation snapping back upon releasing hotkey.\nSingle tap <HotKey: "
			+GetSpecialKey(snazzy.snapRotKeySpecial,snazzy.snapRotKey,false)+">\n\nMiddle-Click will snap the rotation of the selected object/s according to the selected axis.\nDouble tap <HotKey: "
				+GetSpecialKey(snazzy.snapRotKeySpecial,snazzy.snapRotKey,false)+">\n\nRight-Click will reset the rotation of the selected object/s.\n<Hotkey: "+
				GetSpecialKey(snazzy.resetRotationKeySpecial,snazzy.resetRotationKey,false)+">";
		if (GUI.Button (new Rect(5-1,350+y,54+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 1) ResetRotation (true,true,true);	
			else if (key.button == 2) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.RecordObjects(Selection.transforms,"Selection Snap Rotation");
				#else
				Undo.RegisterUndo(Selection.transforms,"Selection Snap Rotation");
				#endif
				SnapRot(snazzy.rotAmount,snazzy.autoSnapRotX,snazzy.autoSnapRotY,snazzy.autoSnapRotZ);
			}
			else {
				snazzy.autoSnapRot = !snazzy.autoSnapRot;
				if (snazzy.autoSnapRot) GetOldPSR();;
			}
		}
		
		if (snazzy.autoSnapRotX) Button = RotationXToggleOn; else Button = RotationXToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables rotation snapping for X axis.\n\nMiddle-Click will snap Rotation-X for selected object/s.\n\nRight-Click will reset only Rotation-X of the selected object/s.";
		if (GUI.Button (new Rect(5-1,369+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 1) ResetRotation(true,false,false); 
			else if (key.button == 2) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.RecordObjects(Selection.transforms,"Selection Snap Rotation X Axis");
				#else
				Undo.RegisterUndo(Selection.transforms,"Selection Snap Rotation X Axis");
				#endif
				SnapRot (snazzy.rotAmount,true,false,false);
			}
			else snazzy.autoSnapRotX = !snazzy.autoSnapRotX;
		}
		
		if (snazzy.autoSnapRotY) Button = RotationYToggleOn; else Button = RotationYToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables rotation snapping for Y axis.\n\nMiddle-Click will snap Rotation-Y for selected object/s.\n\nRight-Click will reset only Rotation-Y of the selected object/s.";
		if (GUI.Button (new Rect(5-1+19,369+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 1) ResetRotation(false,true,false); 
			else if (key.button == 2) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.RecordObjects(Selection.transforms,"Selection Snap Rotation Y Axis");
				#else
				Undo.RegisterUndo(Selection.transforms,"Selection Snap Rotation Y Axis");
				#endif
				SnapRot (snazzy.rotAmount,false,true,false);
			}
			else snazzy.autoSnapRotY = !snazzy.autoSnapRotY;
		}
		
		if (snazzy.autoSnapRotZ) Button = RotationZToggleOn; else Button = RotationZToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables rotation snapping for Z axis.\n\nMiddle-Click will snap Rotation-Z for selected object/s.\n\nRight-Click will reset only Rotation-Z of the selected object/s.";
		if (GUI.Button (new Rect(5-1+38,369+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.button == 1) ResetRotation(false,false,true); 
			else if (key.button == 2) SnapRot(snazzy.rotAmount,false,false,true);
			else snazzy.autoSnapRotZ = !snazzy.autoSnapRotZ;
		}
		
		if (!snazzy.autoSnapRot) GUI.Label(new Rect(5-1,369+y,57+6,16+6),new GUIContent(XYZMask));
		
		// Scale Snap
		if (snazzy.autoSnapScale) Button = ScaleToggleOn; else Button = ScaleToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables scale snapping. Single tap hotkey will toggle, hold will toggle scale snapping back upon releasing hotkey.\nSingle tap <HotKey: "
			+GetSpecialKey(snazzy.snapScaleKeySpecial,snazzy.snapScaleKey,false)+">\n\nMiddle-Click will snap the scale of the selected object/s according to the selected axis.\nDouble tap <HotKey: "
				+GetSpecialKey(snazzy.snapScaleKeySpecial,snazzy.snapScaleKey,false)+"> \n\nRight-Click will reset the scale of the selected object/s.\n<Hotkey: "
				+GetSpecialKey (snazzy.resetScaleKeySpecial,snazzy.resetScaleKey,false)+">\n\nShift-Click will flip the scale of the selected object/s.\n\nControl-Left/Right-Click will double/half the scale of the selected object/s.";
		if (GUI.Button (new Rect(5-1,388+y,54+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.control) {
				if (key.button == 0) MultiplyScale(2,2,2);
				else MultiplyScale(0.5f,0.5f,0.5f);
			}
			else if (key.shift) {
				FlipScale(1,1,1);
			}
			else if (key.button == 1) ResetScale (true,true,true);	
			else if (key.button == 2) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.RecordObjects(Selection.transforms,"Selection Snap Scale");
				#else
				Undo.RegisterUndo(Selection.transforms,"Selection Snap Scale");
				#endif
				SnapScale(snazzy.autoSnapScaleX,snazzy.autoSnapScaleY,snazzy.autoSnapScaleZ);
			}
			else {
				snazzy.autoSnapScale = !snazzy.autoSnapScale;
				if (snazzy.autoSnapScale) GetOldPSR();;
			}
		}
		
		if (snazzy.autoSnapScaleX) Button = ScaleXToggleOn; else Button = ScaleXToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables scale snapping for X axis.\n\nMiddle-Click will snap Scale-X for selected object/s.\n\nRight-Click will reset only Scale-X of the selected object/s.\n\nShift-Click will flip the Scale-X of the selected object/s.\n\nControl-Left/Right-Click will double/half the Scale-X of the selected object/s.";
		if (GUI.Button (new Rect(5-1,407+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.control) {
				if (key.button == 0) MultiplyScale(2,1,1);
				else MultiplyScale(0.5f,1f,1f);
			}
			else if (key.shift) {
				FlipScale (1,-1,-1);
			}
			else if (key.button == 1) ResetScale (true,false,false);
			else if (key.button	== 2) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.RecordObjects(Selection.transforms,"Selection Snap Scale X Axis");
				#else
				Undo.RegisterUndo(Selection.transforms,"Selection Snap Scale X Axis");
				#endif
				SnapScale(true,false,false);
			}
			else snazzy.autoSnapScaleX = !snazzy.autoSnapScaleX;
		}
		
		if (snazzy.autoSnapScaleY) Button = ScaleYToggleOn; else Button = ScaleYToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables scale snapping for Y axis.\n\nMiddle-Click will snap Scale-Y for selected object/s.\n\nRight-Click will reset only Scale-Y of the selected object/s.\n\nShift-Click will flip the Scale-Y of the selected object/s.\n\nControl-Left/Right-Click will double/half the Scale-Y of the selected object/s.";
		if (GUI.Button (new Rect(5-1+19,407+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.control) {
				if (key.button == 0) MultiplyScale(1,2,1);
				else MultiplyScale(1f,0.5f,1f);
			}
			else if (key.shift) {
				FlipScale (-1,1,-1);
			}
			else if (key.button == 1) ResetScale (false,true,false); 
			else if (key.button == 2) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.RecordObjects(Selection.transforms,"Selection Snap Scale Y Axis");
				#else
				Undo.RegisterUndo(Selection.transforms,"Selection Snap Scale Y Axis");
				#endif
				SnapScale (false,true,false);
			}
			else snazzy.autoSnapScaleY = !snazzy.autoSnapScaleY;
		}
		
		if (snazzy.autoSnapScaleZ) Button = ScaleZToggleOn; else Button = ScaleZToggleOff;
		if (snazzy.tooltip) tooltip = "Left-Click enables/disables scale snapping for Z axis.\n\nMiddle-Click will snap Scale-Z for selected object/s.\n\nRight-Click will reset only Scale-Z of the selected object/s.Right-Click will reset only Scale-X of the selected object/s.\n\nShift-Click will flip the Scale-Z of the selected object/s.\n\nControl-Left/Right-Click will double/half the Scale-Z of the selected object/s.";
		if (GUI.Button (new Rect(5-1+38,407+y,16+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (key.control) {
				if (key.button == 0) MultiplyScale(1,1,2);
				else MultiplyScale(1f,1f,0.5f);
			}
			else if (key.shift) {
				FlipScale (-1,-1,1);
			}
			else if (key.button == 1) ResetScale (false,false,true); 
			else if (key.button == 2) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.RecordObjects(Selection.transforms,"Selection Snap Scale Z Axis");
				#else
				Undo.RegisterUndo(Selection.transforms,"Selection Snap Scale Z Axis");
				#endif
				SnapScale (false,false,true);
			}
			else snazzy.autoSnapScaleZ = !snazzy.autoSnapScaleZ;
		}
		
		if (!snazzy.autoSnapScale) {
			GUI.Label(new Rect(5-1,407+y,57+6,16+6),new GUIContent(XYZMask));
		}
		
		if (!snazzy.autoSnap) {
			GUI.Label(new Rect(5-1,312+y,57+6,115+6),new GUIContent(SnapMask));
		}
		
		GUI.Label(new Rect(-1,423+y,64+6,13+6),new GUIContent(Spacer));
		#endregion
		
		#region Parent/Child
		if (snazzy.lockChild) Button = CCompToggleOn; else Button = CCompToggleOff;
		if (snazzy.tooltip) tooltip = "When enabled the Children of the selected object wont get affected by moving/rotating/scaling the Parent.\n<Hotkey: "+GetSpecialKey(snazzy.childCompensationKeySpecial,snazzy.childCompensationKey,false)+">";
		if (GUI.Button (new Rect(5-1,436+y,54+6,16+6),new GUIContent(Button,tooltip),EditorStyles.label)) {
			if (!snazzy.lockChild) UnparentChildren(); else ParentChildren();
		}
		if (snazzy.tooltip) tooltip = "Left-Click creates a new Parent for the selected object/s at the center of the selection.\n<Hotkey: "+GetSpecialKey(snazzy.parentToCenterKeySpecial,snazzy.parentToCenterKey,false)+">\n\nMiddle-Click creates a new Parent for the selected object/s at the Scene origin.\n<Hotkey: "+GetSpecialKey (snazzy.parentKeySpecial,snazzy.parentKey,false)+">\n\nRight-Click moves the parent of the selected object to the center position of the selection.\n<Hotkey: "+GetSpecialKey (snazzy.centerCurrentParentKeySpecial,snazzy.centerCurrentParentKey,false)+">";
		if (GUI.Button (new Rect(5-1,455+y,54+6,16+6),new GUIContent(Parent,tooltip),EditorStyles.label)) {
			if (key.button == 1) MoveParentToCenter(Selection.activeTransform.parent);
			else if (key.button == 2) ParentSelection();	
			else ParentSelectionToCenter();
		}
		
		if (snazzy.tooltip) tooltip = "Selects the Parent of the selected object/s.\n<Hotkey: "+GetSpecialKey(snazzy.hierarchyUpKeySpecial,snazzy.hierarchyUpKey,false)+">";
		if (GUI.Button (new Rect(5-1,474+y,16+6,16+6),new GUIContent(HierarchyUp,tooltip),EditorStyles.label)) SelectParent();
		
		if (snazzy.tooltip) tooltip = "Selects the Children of the selected object/s.\n<Hotkey: "+GetSpecialKey (snazzy.hierarchyDownKeySpecial,snazzy.hierarchyDownKey,false)+">";
		if (GUI.Button (new Rect(24-1,474+y,16+6,16+6),new GUIContent(HierarchyDown,tooltip),EditorStyles.label)) SelectChildren();
		
		if (snazzy.tooltip) tooltip = "Left-Click unparent the selected object/s.\n<HotKey: "+GetSpecialKey (snazzy.unparentKeySpecial,snazzy.unparentKey,false)+">";//+
			//"\n\nRight-Click unparent the selected object/s and delete the parent.\n<HotKey: "+GetSpecialKey(snazzy.unparentDeleteKeySpecial,snazzy.unparentDeleteKey,false)+">";
		if (GUI.Button (new Rect(43-1,474+y,16+6,16+6),new GUIContent(Unparent,tooltip),EditorStyles.label)) {
			//if (key.button == 1) UnparentSelectionDelete(); else UnparentSelection();
			UnparentSelection();
		}
		// Selection Previous/Next
		if (snazzy.tooltip) tooltip = "Left-Click to cycle your selections backward.\n<HotKey: "+GetSpecialKey (snazzy.unparentKeySpecial,snazzy.unparentKey,false)+">";
		if (GUI.Button (new Rect(5-1,493+y,25+6,16+6),new GUIContent(SelectionPrev,tooltip),EditorStyles.label)) {
			SelectPrevious();
		}
		if (snazzy.tooltip) tooltip = "Left-Click to cycle your selections forward.\n<HotKey: "+GetSpecialKey (snazzy.unparentKeySpecial,snazzy.unparentKey,false)+">";
		if (GUI.Button (new Rect(34-1,493+y,25+6,16+6),new GUIContent(SelectionNext,tooltip),EditorStyles.label)) {
			SelectNext();
		}
		// Selection Sets
		for (int i = 0;i < 12;++i) {
			SelectionSetButton(i, (int)Mathf.Repeat (i,3)*19, Mathf.FloorToInt(i/3)*19, y);
		}

		int offset = 19+19+19+19+19;

		GUI.Label(new Rect(-1,490+y+offset,64+6,13+6),new GUIContent(Spacer));
		
		#endregion
		
		#region Settings
		if (snazzy.settings) Button = SettingsToggleOn; else Button = SettingsToggleOff;
		if (GUI.Button (new Rect(5-1,503+y+offset,54+6,16+6),new GUIContent(Button),EditorStyles.label)) {
			snazzy.settings = !snazzy.settings;
			if (snazzy.settings) {
				SnazzySettingsEditor editWindow = ScriptableObject.CreateInstance<SnazzySettingsEditor>();
				editWindow.title = "Snazzy Tools Settings";
				editWindow.minSize = new Vector3(415,272);
				editWindow.snazzyToolsEditor = this;
				editWindow.ShowUtility();
				editWindow.InitSnazzySettings();
			}
			else {
				SnazzySettingsEditor editWindow = GetWindow(typeof(SnazzySettingsEditor)) as SnazzySettingsEditor;
				if (editWindow != null) editWindow.Close();
			}
		}
		#endregion
	}

	void SelectionSetButton(int number, int offsetX, int offsetY, int y){
		if (snazzy.tooltip) tooltip = 
			"View/Selection Set "+(number+1)+"\n\n"+
				"Left-Click is for both view & selection.\nMiddle-Click is for view only.\nRight-Click is for selection only.\n\n" +
				"Regular Left/Right/Middle Click to load.\n" +
				"Shift + Left/Right/Middle Click to save.\n" +
				"Control + Left/Right/Middle Click to clear.\n\n" +
				"Hotkeys:\n"+
				"Load View & Selection: <F"+(number+1)+">\n"+
				"Load View only: <Shift + F"+(number+1)+">\n"+
				"Load Selection only: <Control + F"+(number+1)+">";//\n"+

//				"Save/overwrite View: <Alt+Shift + F"+number+">\n"+
//				"Save/overwrite Selection: <Alt+Control + F"+number+">\n"+
//				"Save/overwrite both: <Alt+Control+Shift + F"+number+">"; 
		//=========================================================================================================================================
		if (snazzyS.sceneCameras[number].isSaved && snazzyS.selectionSet[number].isSaved){
			if (GUI.Button (new Rect(5-1+offsetX,512+y+offsetY,16+6,16+6),new GUIContent(SaveToggleCamOnSelOn,tooltip),EditorStyles.label)) {

				if (!key.control && !key.shift && key.button == 2) LoadSceneCameraSet(number);
				else if (!key.control && !key.shift && key.button == 1) LoadSelectionSet(number);
				else if (!key.control && !key.shift && key.button == 0) { LoadSelectionSet(number); LoadSceneCameraSet(number); }

				else if (!key.control && key.shift && key.button == 2) SaveSceneCamera(number);
				else if (!key.control && key.shift && key.button == 1) SaveSelectionSet(number);
				else if (!key.control && key.shift && key.button == 0) { SaveSelectionSet(number); SaveSceneCamera(number); }

				else if (key.control && key.button == 2) ClearSceneCamera(number);
				else if (key.control && key.button == 1) ClearSelectionSet(number);
				else if (key.control && key.button == 0) { ClearSelectionSet(number); ClearSceneCamera(number); }
			}
		}//=========================================================================================================================================
		else if (!snazzyS.sceneCameras[number].isSaved && !snazzyS.selectionSet[number].isSaved){
			if (GUI.Button (new Rect(5-1+offsetX,512+y+offsetY,16+6,16+6),new GUIContent(SaveToggleCamOffSelOff,tooltip),EditorStyles.label)) {

				if (!key.control && key.shift && key.button == 2) SaveSceneCamera(number);

				else if (!key.control && key.shift && key.button == 1) SaveSelectionSet(number);
				else if (!key.control && key.shift && key.button == 0) { SaveSelectionSet(number); SaveSceneCamera(number); }
			}
		}//=========================================================================================================================================
		else if (!snazzyS.sceneCameras[number].isSaved && snazzyS.selectionSet[number].isSaved){
			if (GUI.Button (new Rect(5-1+offsetX,512+y+offsetY,16+6,16+6),new GUIContent(SaveToggleCamOffSelOn,tooltip),EditorStyles.label)) {

				if (!key.control && !key.shift && key.button == 0) LoadSelectionSet(number);
				else if (!key.control && !key.shift && key.button == 1) LoadSelectionSet(number);

				else if (!key.control && key.shift && key.button == 2) SaveSceneCamera(number);
				else if (!key.control && key.shift && key.button == 1) SaveSelectionSet(number);
				else if (!key.control && key.shift && key.button == 0) { SaveSelectionSet(number); SaveSceneCamera(number); }

				else if (key.control && key.button == 1) ClearSelectionSet(number);
				else if (key.control && key.button == 0) { ClearSelectionSet(number); ClearSceneCamera(number); }
			}
		}//=========================================================================================================================================
		else if (snazzyS.sceneCameras[number].isSaved && !snazzyS.selectionSet[number].isSaved){
			if (GUI.Button (new Rect(5-1+offsetX,512+y+offsetY,16+6,16+6),new GUIContent(SaveToggleCamOnSelOff,tooltip),EditorStyles.label)) {

				if (!key.control && !key.shift && key.button == 0) LoadSceneCameraSet(number);
				else if (!key.control && !key.shift && key.button == 2) LoadSceneCameraSet(number);

				else if (!key.control && key.shift && key.button == 2) SaveSceneCamera(number);
				else if (!key.control && key.shift && key.button == 1) SaveSelectionSet(number);
				else if (!key.control && key.shift && key.button == 0) { SaveSelectionSet(number); SaveSceneCamera(number); }

				else if (key.control && key.button == 2) ClearSceneCamera(number);
				else if (key.control && key.button == 0) { ClearSelectionSet(number); ClearSceneCamera(number); }
			}
		}//=========================================================================================================================================
	}

	static void ToggleSnap()
	{
		snazzy.autoSnap = !snazzy.autoSnap;
		if (snazzy.autoSnap) GetOldPSR();;
		SnazzyWindowRepaint();
	}

	static void ToggleMovSnap()
	{
		snazzy.autoSnapPos = !snazzy.autoSnapPos;
		if (snazzy.autoSnap && snazzy.autoSnapPos) GetOldPSR();;
		SnazzyWindowRepaint();
	}

	static void ToggleRotSnap()
	{
		snazzy.autoSnapRot = !snazzy.autoSnapRot;
		if (snazzy.autoSnap && snazzy.autoSnapRot) GetOldPSR();;
		SnazzyWindowRepaint();
	}

	static void ToggleScaleSnap()
	{
		snazzy.autoSnapScale = !snazzy.autoSnapScale;
		if (snazzy.autoSnap && snazzy.autoSnapScale) GetOldPSR();;
		SnazzyWindowRepaint();
	}
	

	static string GetSpecialKey(int index,KeyCode key,bool mov2RotKey)
	{
		string specialKey = "";
		if (key != KeyCode.None) specialKey += GetSpecialKey (index);		
		
		return specialKey+key;
	}

	static string GetSpecialKey(int index)
	{
		string specialKey = "";
		
		if ((index & 1) != 0) specialKey += "Shift-";
		if ((index & 2) != 0) specialKey += "Control-";	
		if ((index & 4) != 0) specialKey += "Alt-";
		
		return specialKey;
	}

	static void SnazzyWindowRepaint()
	{
		if (window == null) {window = GetWindow(typeof(SnazzyToolsEditor)) as SnazzyToolsEditor;}
		window.Repaint();	
	}
	
	static void GetOldPSR()
	{
		if (Selection.activeTransform != null) {
			if (!rotHandle) {
				snazzy.oldPosition = Selection.activeTransform.position;
				// if (Tools.current == Tool.Rect) snazzy.oldHandleRect = Tools.handleRect;
			}
			snazzy.oldScale = Selection.activeTransform.localScale; 
			snazzy.oldRotation = Selection.activeTransform.eulerAngles;	
		}
	}
	
	static Vector3 GetCenterPosition()
	{
		Vector3 center = new Vector3(0,0,0);
		
		foreach (Transform transform in Selection.transforms) {
			center += transform.position;	
		}
		center /= Selection.transforms.Length;
		
		return center;
	}

//	static Vector3 getDirection(Vector3 direction,float rotation)
//	{
//		if (sceneCamera != null) {
//			camReference.transform.eulerAngles = sceneCamera.transform.eulerAngles;
//		}
//		Vector3 localRotation = camReference.transform.localEulerAngles;
//		localRotation.x = Mathf.DeltaAngle(0,localRotation.x);
//		notify = localRotation.x.ToString();
//		if (localRotation.x >= 0) {
//			if (localRotation.x >= rotation) {
//				direction.z *= -1; 
//			}
//			
//			if (localRotation.x < 45) localRotation.x += 45-rotation;
//		}
//		else {
//			if (localRotation.x <= -rotation) {
//				direction.z *= -1; 
//			}
//			if (localRotation.x > -45) localRotation.x -= 45-rotation;
//		}
//		camReference.transform.localEulerAngles = localRotation;
//		
//		SnapRot(camReference.transform,90,true,true,true);
//		return camReference.transform.TransformDirection(direction);
//	}
	
	static Vector3 getDirection(Vector3 direction,float rotation,float amount)
	{
		if (sceneCamera != null) {
			camReference.transform.eulerAngles = sceneCamera.transform.eulerAngles;
		}
		Vector3 localRotation = camReference.transform.localEulerAngles;
		bool yAxis = false;
		localRotation.x = Mathf.DeltaAngle(0,localRotation.x);
		
		if (Mathf.Abs (localRotation.x)-rotation > 0) {
			if (direction.y != 0) {direction.z = direction.y;direction.y = 0;}
			else if (direction.z != 0) {direction.y = direction.z;direction.z = 0;yAxis = true;}
			localRotation.x = 0;
			camReference.transform.localEulerAngles = localRotation;
			SnapRot(camReference.transform,Vector3.zero,90,false,true,false);
		}
		else {
			camReference.transform.localEulerAngles = localRotation;
			SnapRot(camReference.transform,Vector3.zero,90,true,true,true);
		}
		
		if (!yAxis) {
			Vector3 newDirection = SnapVector3 (camReference.transform.TransformDirection(direction),true,true,true,amount);
			return newDirection;
		}
		else 
			return direction;
	}

	bool GetHorizontalMode()
	{
		if (sceneCamera != null) {
			Vector3 localRotation = sceneCamera.transform.localEulerAngles;
			localRotation.x = Mathf.DeltaAngle(0,localRotation.x);
			
			if (localRotation.x >= 0) {
				if (localRotation.x >= snazzy.cameraAngleOffset) return true;
				else return false;
			}
			else {
				if (localRotation.x <= -snazzy.cameraAngleOffset) return true;
				else return false;
			}
		}
		return false;
	}	

	static void Move(Vector3 direction)
	{
		foreach (Transform transform in Selection.transforms) {
			transform.position += direction;	
		}
	}
	
	static void Rotate (float rotation)
	{
		if (Tools.pivotRotation == PivotRotation.Global) {
			foreach (Transform transform in Selection.transforms) 
			{
				switch (snazzy.rotAxis)	{
					case 0:
						transform.Rotate(new Vector3(rotation, 0, 0),Space.World);
						break;
					case 1:
						transform.Rotate(new Vector3(0, rotation, 0),Space.World);
						break;
					case 2:
						transform.Rotate(new Vector3(0, 0, rotation),Space.World);
						break;
				}
			}
		}
		else {
			foreach (Transform transform in Selection.transforms) 
			{
				switch (snazzy.rotAxis)
				{
					case 0:
						transform.Rotate(new Vector3(rotation, 0, 0),Space.Self);
						break;
					case 1:
						transform.Rotate(new Vector3(0, rotation, 0),Space.Self);
						break;
					case 2:
						transform.Rotate(new Vector3(0, 0, rotation),Space.Self);
						break;
				}
			}
		}
		ClampRotation();
	}
	
	static void SnapRot(float rotation,bool x,bool y,bool z)
	{
		Vector3 offset = Vector3.zero;
		if (snazzy.offsetObject != null) offset = snazzy.offsetObject.eulerAngles;

		foreach (Transform transform in Selection.transforms) {
			SnapRot(transform,offset,rotation,x,y,z);
		}
		ResetHandler ();
	}

	static void UnityRotate(float rotation)
	{	
		Vector3 rotPoint = new Vector3(0,0,0);
		Vector3 rotAxis = new Vector3(0,0,0);

		if (Tools.pivotRotation == PivotRotation.Global) {
			if (snazzy.rotAxis == 0) rotAxis = Vector3.right;
			else if (snazzy.rotAxis == 1) rotAxis = Vector3.up;
			else if (snazzy.rotAxis == 2) rotAxis = Vector3.forward;
		}
		else {
			if (snazzy.rotAxis == 0) rotAxis = Selection.activeTransform.right;
			else if (snazzy.rotAxis == 1) rotAxis = Selection.activeTransform.up;
			else if (snazzy.rotAxis == 2) rotAxis = Selection.activeTransform.forward;
		}

		rotPoint = Tools.handlePosition;
		
		foreach (Transform transform in Selection.transforms) {
			transform.RotateAround(rotPoint,rotAxis,rotation);
		}
		ClampRotation();
	}

	static void FlipScale(int x,int y,int z)
	{
		foreach (Transform transform in Selection.transforms) {
			transform.localScale = new Vector3(-transform.localScale.x*x,-transform.localScale.y*y,-transform.localScale.z*z);
		}
	}

	static void MultiplyScale(float x,float y,float z)
	{
		foreach (Transform transform in Selection.transforms) {
			transform.localScale = new Vector3(transform.localScale.x*x,transform.localScale.y*y,transform.localScale.z*z);
		}
	}

	static void ClampRotation()
	{
		Vector3 rotation;
		foreach (Transform transform in Selection.transforms) {
			rotation = transform.localEulerAngles;
			rotation.x = Mathf.DeltaAngle(0,rotation.x);
			rotation.y = Mathf.DeltaAngle(0,rotation.y);
			rotation.z = Mathf.DeltaAngle(0,rotation.z);
			transform.localEulerAngles = rotation;
		}
	}

	static float DeltaAngle(float angle)
	{
		if (angle > 0) while (angle > 360) angle -= 360;
		else while (angle < -360) angle += 360;
		return angle;
	}
	
	static void SnapScale(bool x,bool y,bool z)
	{
		// Debug.Log ("Scale "+x+","+y+","+z);
		Vector3 scale;
		
		foreach (Transform transform in Selection.transforms) 
		{
			scale = SnapVector3(transform.localScale,x,y,z,snazzy.gridSize);
			if (x && scale.x < snazzy.gridSize) {scale.x = snazzy.gridSize;}
			if (y && scale.y < snazzy.gridSize) {scale.y = snazzy.gridSize;}
			if (z && scale.z < snazzy.gridSize) {scale.z = snazzy.gridSize;}
			transform.localScale = scale;
		}
	}

	static void SnapRot(Transform transform,Vector3 offset,float rotation,bool snapX,bool snapY,bool snapZ)
	{
		Vector3 snapRotation = new Vector3();
		
		if (snapX) snapRotation.x = (Mathf.Round((transform.eulerAngles.x-offset.x)/rotation)*rotation)+offset.x; else snapRotation.x = transform.eulerAngles.x;
		if (snapY) snapRotation.y = (Mathf.Round((transform.eulerAngles.y-offset.y)/rotation)*rotation)+offset.y; else snapRotation.y = transform.eulerAngles.y;
		if (snapZ) snapRotation.z = (Mathf.Round((transform.eulerAngles.z-offset.z)/rotation)*rotation)+offset.z; else snapRotation.z = transform.eulerAngles.z;
		
		transform.localEulerAngles = snapRotation;
	}

	static Vector3 SnapVector3(Vector3 snapVector3,bool snapX,bool snapY,bool snapZ,float amount)
	{
		if (snapX) snapVector3.x = Mathf.Round(snapVector3.x/amount)*amount; 
		if (snapY) snapVector3.y = Mathf.Round(snapVector3.y/amount)*amount; 
		if (snapZ) snapVector3.z = Mathf.Round(snapVector3.z/amount)*amount; 
		
		return snapVector3;
	}

	static Vector3 GetSnapRot(Quaternion rotation)
	{
		Vector3 snapRotation = new Vector3();

		snapRotation.x = Mathf.Round(rotation.eulerAngles.x/snazzy.rotIncrement)*snazzy.rotIncrement;
		snapRotation.y = Mathf.Round(rotation.eulerAngles.y/snazzy.rotIncrement)*snazzy.rotIncrement;
		snapRotation.z = Mathf.Round(rotation.eulerAngles.z/snazzy.rotIncrement)*snazzy.rotIncrement;

		return snapRotation;
	}

	static void ResetPosition (bool x,bool y,bool z)
	{
		Vector3 position = new Vector3();
		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Selection Reset Position");
		#else
		Undo.RegisterUndo(Selection.transforms,"Selection Reset Position");
		#endif
		
		foreach (Transform transform in Selection.transforms) {
			position = transform.position;
			if (x) position.x = 0; 
			if (y) position.y = 0; 
			if (z) position.z = 0; 
			transform.localPosition = position;
		}
	}
	
	static void ResetRotation (bool x,bool y,bool z)
	{
		Vector3 rotation = new Vector3();
		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Selection Reset Rotation");
		#else
		Undo.RegisterUndo(Selection.transforms,"Selection Reset Rotation");		
		#endif

		foreach (Transform transform in Selection.transforms) {
			rotation = transform.localEulerAngles;
			if (x) rotation.x = 0; 
			if (y) rotation.y = 0; 
			if (z) rotation.z = 0; 
			transform.localEulerAngles = rotation;
		}
		
		ResetHandler ();
	}
	
	static void ResetHandler ()
	{
		if (Selection.activeTransform != null) {
			if (Tools.pivotRotation == PivotRotation.Global) 
				Tools.handleRotation = Quaternion.identity;
			else Tools.handleRotation = Selection.activeTransform.rotation;	
		}
	}
	
	static void ResetScale (bool x,bool y,bool z)
	{
		Vector3 scale = new Vector3();
		
		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Selection Reset Scale");
		#else
		Undo.RegisterUndo(Selection.transforms,"Selection Reset Scale");
		#endif
		foreach (Transform transform in Selection.transforms) {
			scale = transform.localScale;
			if (x) scale.x = 1; 
			if (y) scale.y = 1; 
			if (z) scale.z = 1; 
			
			transform.localScale = scale;
		}
	}

	static void SnapTransform()
	{
		if (snazzy.autoSnapPos) SnapPos(snazzy.autoSnapPosX,snazzy.autoSnapPosY,snazzy.autoSnapPosZ);
		if (snazzy.autoSnapRot) SnapRot(snazzy.rotAmount,snazzy.autoSnapRotX,snazzy.autoSnapRotY,snazzy.autoSnapRotZ);
		if (snazzy.autoSnapScale) SnapScale(snazzy.autoSnapScaleX,snazzy.autoSnapScaleY,snazzy.autoSnapScaleZ);
	}
	
	static void SnapPos(bool x,bool y,bool z)
	{
		if (uGUISelect == true) {
			if (Tools.handlePosition.x != pivotOld.x || Tools.handlePosition.y != pivotOld.y) {pivotOld = Tools.handlePosition;return;}
			pivotOld = Tools.handlePosition;
		}

		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Selection Snap Transform");
		#else
		Undo.RegisterUndo(Selection.transforms,"Selection Snap Transform");
		#endif

		foreach (Transform transform in Selection.transforms) {
			if (transform == snazzy.offsetObject) continue;
			transform.position = SnapVector3(transform.position-gridOffset,x,y,z,snazzy.gridSize)+gridOffset;
			// Debug.Log ("h");
		}
	}

	static void MoveSelectionLeft()
	{
		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Selection Move Left");
		#else
		Undo.RegisterUndo(Selection.transforms,"Selection Move Left");
		#endif
		Move (getDirection(new Vector3(-snazzy.moveAmount,0,0),0,snazzy.moveAmount));
	}
	
	static void MoveSelectionRight()
	{
		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Selection Move Right");
		#else
		Undo.RegisterUndo(Selection.transforms,"Selection Move Right");
		#endif
		Move (getDirection(new Vector3(snazzy.moveAmount,0,0),0,snazzy.moveAmount));
	}
	
	static void MoveSelectionUp()
	{
		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Selection Move Up");
		#else
		Undo.RegisterUndo(Selection.transforms,"Selection Move Up");
		#endif
		Move (getDirection(new Vector3(0,snazzy.moveAmount,0),snazzy.cameraAngleOffset,snazzy.moveAmount));	
	}
	
	static void MoveSelectionDown()
	{
		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Selection Move Down");
		#else
		Undo.RegisterUndo(Selection.transforms,"Selection Move Down");
		#endif
		Move (getDirection(new Vector3(0,-snazzy.moveAmount,0),snazzy.cameraAngleOffset,snazzy.moveAmount));
	}
	
	static void MoveSelectionForward()
	{
		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Selection Move Forward");
		#else
		Undo.RegisterUndo(Selection.transforms,"Selection Move Forward");
		#endif
		Move (getDirection(new Vector3(0,0,snazzy.moveAmount),snazzy.cameraAngleOffset,snazzy.moveAmount));
	}
	
	static void MoveSelectionBack()
	{
		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Selection Move Back");
		#else
		Undo.RegisterUndo(Selection.transforms,"Selection Move Back");
		#endif
		Move (getDirection(new Vector3(0,0,-snazzy.moveAmount),snazzy.cameraAngleOffset,snazzy.moveAmount));
	}
	
	static void RotateSelectionLeft()
	{
		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Selection Rotate Left");
		#else
		Undo.RegisterUndo(Selection.transforms,"Selection Rotate Left");
		#endif
		if (!snazzy.rotationMode) UnityRotate (-snazzy.rotAmount); else Rotate (-snazzy.rotAmount); // RotateLeft
		if (snazzy.autoSnap && snazzy.autoSnapRot) {
			SnapRot(snazzy.rotAmount,snazzy.autoSnapRotX,snazzy.autoSnapRotY,snazzy.autoSnapRotZ);
		}		
	}
	
	static void RotateSelectionRight()
	{
		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Selection Rotate Right");
		#else
		Undo.RegisterUndo(Selection.transforms,"Selection Rotate Right");
		#endif
		if (!snazzy.rotationMode) UnityRotate (snazzy.rotAmount); else Rotate (snazzy.rotAmount); // RotateRight	
		if (snazzy.autoSnap && snazzy.autoSnapRot) {
			SnapRot(snazzy.rotAmount,snazzy.autoSnapRotX,snazzy.autoSnapRotY,snazzy.autoSnapRotZ);
		}	
	}
	
	static void DuplicateSelection(bool scene)
	{
		if (!scene) EditorApplication.ExecuteMenuItem("Window/Hierarchy");
		EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Duplicate"));
	}

	static void SelectParent()
	{
		if (Selection.activeTransform != null) {
			#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
			Undo.RecordObjects(Selection.transforms,"Select Parent");
			#else
			Undo.RegisterUndo(Selection.transforms,"Select Parent");
			#endif
			List<GameObject> parents = new List<GameObject>();
			bool add = false;

			foreach (Transform transform in Selection.transforms) {
				if (transform.parent != null) {
					add = true;
					foreach (GameObject parent in parents) {
						if (parent == transform.parent.gameObject) {add = false;break;}
					}
					if (add) parents.Add(transform.parent.gameObject);
				}
				else parents.Add (transform.gameObject);
			}
			Selection.objects = parents.ToArray();
		}	
	}
	
	static void ParentSelection()
	{
		if (Selection.transforms.Length > 0) { 
			GameObject obj = new GameObject();
			obj.name = "GameObject";
			Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.name); 	 
			obj.transform.parent = Selection.activeTransform.parent;
			
			foreach (Transform transform in Selection.transforms) {  
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.SetTransformParent (transform, obj.transform,"Change Parent "+transform.name);  
				#else
				Undo.RegisterSetTransformParentUndo (transform, obj.transform,"Change Parent "+transform.name);  
				transform.parent = obj.transform;
				#endif
			}
		
			Selection.activeTransform = obj.transform;	
		}
	}

	static void ParentSelectionToCenter()
	{
		if (Selection.transforms.Length > 0) { 
			GameObject obj = new GameObject();
			obj.name = "GameObject";
			Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.name); 	 
			obj.transform.parent = Selection.activeTransform.parent;
			
			foreach (Transform transform in Selection.transforms) {  
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.SetTransformParent (transform, obj.transform,"Change Parent "+transform.name);  
				#else
				Undo.RegisterSetTransformParentUndo (transform, obj.transform,"Change Parent "+transform.name);  
				transform.parent = obj.transform;
				#endif
			}
			MoveParentToCenter(obj.transform);
			
			Selection.activeTransform = obj.transform;	
		}
	}

	static void SelectChildren()
	{
		if (Selection.activeTransform != null) {
			#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
			Undo.RecordObjects(Selection.transforms,"Select Children");
			#else
			Undo.RegisterUndo(Selection.transforms,"Select Children");
			#endif
			List<GameObject> transforms = new List<GameObject>();
			foreach (Transform transform in Selection.transforms) {
				foreach (Transform transform1 in transform) {
					transforms.Add(transform1.gameObject);
				}
			}
			if (transforms.Count > 0) Selection.objects = transforms.ToArray();
		}	
	}
	
	static void UnparentSelection()
	{
		if (Selection.activeTransform != null) {
			foreach (Transform transform in Selection.transforms) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.SetTransformParent (transform, null,"Parent change "+transform.name);  
				#else
				Undo.RegisterSetTransformParentUndo (transform, null,"Parent change "+transform.name);  
				transform.parent = null;
				#endif
			}
		}
	}

//	static void UnparentSelectionDelete()
//	{
//		if (Selection.activeTransform != null) {
//			#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
//			#else
//			Undo.RegisterSceneUndo("Parent Change");
//			#endif
//			
//			Transform parent = null;
//			if (Selection.activeTransform.parent != null) {
//				parent = Selection.activeTransform.parent;
//			}
//			else {
//				notify = "Selected object has no parent";
//				SceneView.RepaintAll();
//				return;
//			}	
//			foreach (Transform transform in Selection.transforms) {
//				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
//				Undo.SetTransformParent (transform, null,"Parent change "+transform.name);  
//				#else
//				transform.parent = null;
//				#endif
//			}
//			if (parent != null) {
//				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
//				GameObject tempObj;
//				
////				for (int i = 0;i < parent.childCount;++i) {
////					tempObj = parent.GetChild (0).gameObject;
////					//Undo.SetTransformParent (tempObj, null,"Parent change "); 
////					Undo.DestroyObjectImmediate(tempObj);
////					--i;
////				}
//				Undo.SetTransformParent (parent,Selection.activeTransform,"Parent change "+Selection.activeTransform.name);
//				//Undo.DestroyObjectImmediate(parent);
//				#else
//				DestroyImmediate(parent.gameObject);
//				#endif
//			}  
//		}
//	}

	static void MoveParentToCenter(Transform parent)
	{
		if (Selection.activeTransform == null) {
			notify = "Nothing selected";
			return;
		}
		if (parent == null) {
			notify = "Active selection does not have a parent";
			return;
		}
		
		#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		Undo.RecordObjects(Selection.transforms,"Move parent to center");
		#else
		Undo.RegisterUndo(Selection.transforms,"Move parent to center");
		#endif
		Vector3 center = new Vector3(0,0,0);
		List<Vector3> oldPositions = new List<Vector3>();
		
		foreach (Transform transform in parent) {
			oldPositions.Add(transform.position);
		}
		
		center = GetCenterPosition ();
		
		parent.position = center;
		if (snazzy.autoSnapPos) {
			parent.position = SnapVector3(parent.position,snazzy.autoSnapPosX,snazzy.autoSnapPosY,snazzy.autoSnapPosY,snazzy.moveAmount);	
		}
		
		int i = 0;
		foreach (Transform transform in parent) {
			transform.position = oldPositions[i++];
		}	

		notify = parent.name+" moved to center of selection";	
	}
	
	static void UnparentChildren()
	{
		if (Selection.activeTransform != null) {
			if (Selection.transforms.Length > 1) {notify = "ChildCompensation works only with 1 selected Parent";}
			if (Selection.activeTransform.childCount > 0) {	
				_parent = new GameObject();  
				_parent.name = "##TempParent##";
				Undo.RegisterCreatedObjectUndo(_parent, "Create " + _parent.name); 
				
				int childCount = Selection.activeTransform.childCount;				

				for (int i = 0;i < childCount;++i) {
					#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
					Undo.SetTransformParent (Selection.activeTransform.GetChild(childCount-i-1),_parent.transform,"Change Parent "+Selection.activeTransform.GetChild(childCount-i-1).name);	
					#else
					Undo.RegisterSetTransformParentUndo (Selection.activeTransform.GetChild(childCount-i-1),_parent.transform,"Change Parent "+Selection.activeTransform.GetChild(childCount-i-1).name);	
					Selection.activeTransform.GetChild(childCount-i-1).parent = _parent.transform;
					#endif
				}
				snazzy.lockChild = true;
			}
		}
	}
	
	static void ParentChildren()
	{
		if (_parent != null) {
			#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
			#else		
			Undo.RegisterSceneUndo ("Change Parent");
			#endif
			int childCount =_parent.transform.childCount;				
			
			for (int i = 0;i < childCount;++i) {
				#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				Undo.SetTransformParent (_parent.transform.GetChild(childCount-i-1),snazzy.oldTransform,"Change Parent "+_parent.transform.GetChild(childCount-i-1).name);
				#else
				_parent.transform.GetChild(childCount-i-1).parent = snazzy.oldTransform;
				#endif
			}
		
			#if !UNITY_3_4 && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
			Undo.DestroyObjectImmediate(_parent);
			#else
			DestroyImmediate(_parent);
			#endif
		}
		snazzy.lockChild = false;
		SnazzyWindowRepaint();
	}

	static public void Key(Event key,bool fromScene)
	{
		if (Application.isPlaying) return;
		//Debug.Log("Do something special here!");
		if (snazzy.offsetObject != null) {
			gridOffset = new Vector3(Mathf.Repeat ( snazzy.offsetObject.position.x,snazzy.gridSize),Mathf.Repeat (snazzy.offsetObject.position.y,snazzy.gridSize),Mathf.Repeat (snazzy.offsetObject.position.z,snazzy.gridSize));
		}
		else {
			gridOffset = Vector3.zero; 
		}

		keySnap = false;
		if (!key.isKey && key.type == EventType.Repaint) snapLock = false;
		if (key.type == EventType.KeyUp) {
			snazzy.keyDown = false;
			snazzy.lockKeys = false;
			snazzy.snapKeyDown = false;
			snazzy.snapPosKeyDown = false;
			snazzy.snapRotKeyDown = false;
			snazzy.snapScaleKeyDown = false;
			// Debug.Log ("keyup");
		}

		int specialKeyPressed = 0;
		if (key.shift) specialKeyPressed += 1;
		if (key.control) specialKeyPressed += 2;
		if (key.alt) specialKeyPressed += 4;

		if (snazzy.mov2) snazzy.moveAmount = snazzy.gridIncrement2*snazzy.gridSize; else snazzy.moveAmount = snazzy.gridIncrement*snazzy.gridSize;
		if (snazzy.angle2) snazzy.rotAmount = snazzy.rotIncrement2; else snazzy.rotAmount = snazzy.rotIncrement;
				
		if (key.type == EventType.keyDown) {

			// Repeat Keys
			if (!snazzy.lockKeys) {
				if (key.keyCode == snazzy.leftKey && snazzy.leftKey != KeyCode.None && snazzy.leftKeySpecial == specialKeyPressed) {
					MoveSelectionLeft();	
					key.Use();
					keySnap = true;
				}
				if (key.keyCode == snazzy.rightKey && snazzy.rightKey != KeyCode.None && snazzy.rightKeySpecial == specialKeyPressed) {
					MoveSelectionRight();
					key.Use();
					keySnap = true;
				}
				if (key.keyCode == snazzy.upKey && snazzy.upKey != KeyCode.None && snazzy.upKeySpecial == specialKeyPressed) {
					MoveSelectionUp();
					key.Use();
					keySnap = true;
				}
				if (key.keyCode == snazzy.downKey && snazzy.downKey != KeyCode.None && snazzy.downKeySpecial == specialKeyPressed) {
					MoveSelectionDown();
					key.Use();
					keySnap = true;
				}
				if (key.keyCode == snazzy.frontKey && snazzy.frontKey != KeyCode.None && snazzy.frontKeySpecial == specialKeyPressed) {
					MoveSelectionForward();
					key.Use();
					keySnap = true;
				}
				if (key.keyCode == snazzy.backKey && snazzy.backKey != KeyCode.None && snazzy.backKeySpecial == specialKeyPressed) {
					MoveSelectionBack();
					key.Use();
					keySnap = true;
				}
				if (key.keyCode == snazzy.rotLeftKey && snazzy.rotLeftKey != KeyCode.None && snazzy.rotLeftKeySpecial == specialKeyPressed) {
					RotateSelectionLeft();
					key.Use();
					keySnap = true;
				}
				if (key.keyCode == snazzy.rotRightKey && snazzy.rotRightKey != KeyCode.None && snazzy.rotRightKeySpecial == specialKeyPressed) {
					RotateSelectionRight();
					key.Use();
					keySnap = true;
				}
			
				// Non repeat Keys
				if (snazzy.keyDown) return;
				snazzy.keyDown = true;
				// Debug.Log ("keydown");	

				//if (!key.control) {
				if (key.keyCode == snazzy.selectionPreviousKey && snazzy.selectionPreviousKey != KeyCode.None && snazzy.selectionPreviousKeySpecial == specialKeyPressed) SelectPrevious();
				if (key.keyCode == snazzy.selectionNextKey && snazzy.selectionNextKey != KeyCode.None && snazzy.selectionNextKeySpecial == specialKeyPressed) SelectNext ();

				if (key.keyCode == KeyCode.F1) {LoadSelectionCamera (0,key.control,key.shift);}
				if (key.keyCode == KeyCode.F2) {LoadSelectionCamera (1,key.control,key.shift);}
				if (key.keyCode == KeyCode.F3) {LoadSelectionCamera (2,key.control,key.shift);}
				if (key.keyCode == KeyCode.F4) {LoadSelectionCamera (3,key.control,key.shift);}
				if (key.keyCode == KeyCode.F5) {LoadSelectionCamera (4,key.control,key.shift);}
				if (key.keyCode == KeyCode.F6) {LoadSelectionCamera (5,key.control,key.shift);}
				if (key.keyCode == KeyCode.F7) {LoadSelectionCamera (6,key.control,key.shift);}
				if (key.keyCode == KeyCode.F8) {LoadSelectionCamera (7,key.control,key.shift);}
				if (key.keyCode == KeyCode.F9) {LoadSelectionCamera (8,key.control,key.shift);}
				if (key.keyCode == KeyCode.F10) {LoadSelectionCamera (9,key.control,key.shift);}
				if (key.keyCode == KeyCode.F11) {LoadSelectionCamera (10,key.control,key.shift);}
				if (key.keyCode == KeyCode.F12) {LoadSelectionCamera (11,key.control,key.shift);}

//				if (key.keyCode == snazzy.loadSelect0Key && snazzy.loadSelect0Key != KeyCode.None && snazzy.loadSelect0KeySpecial == specialKeyPressed) LoadSelectionSet (0);
//				if (key.keyCode == snazzy.loadSelect1Key && snazzy.loadSelect1Key != KeyCode.None && snazzy.loadSelect1KeySpecial == specialKeyPressed) LoadSelectionSet (1);
//				if (key.keyCode == snazzy.loadSelect2Key && snazzy.loadSelect2Key != KeyCode.None && snazzy.loadSelect2KeySpecial == specialKeyPressed) LoadSelectionSet (2);
//				if (key.keyCode == snazzy.loadSelect3Key && snazzy.loadSelect3Key != KeyCode.None && snazzy.loadSelect3KeySpecial == specialKeyPressed) LoadSelectionSet (3);
//				if (key.keyCode == snazzy.loadSelect4Key && snazzy.loadSelect4Key != KeyCode.None && snazzy.loadSelect4KeySpecial == specialKeyPressed) LoadSelectionSet (4);
//				if (key.keyCode == snazzy.loadSelect5Key && snazzy.loadSelect5Key != KeyCode.None && snazzy.loadSelect5KeySpecial == specialKeyPressed) LoadSelectionSet (5);
//				if (key.keyCode == snazzy.loadSelect6Key && snazzy.loadSelect6Key != KeyCode.None && snazzy.loadSelect6KeySpecial == specialKeyPressed) LoadSelectionSet (6);
//				if (key.keyCode == snazzy.loadSelect7Key && snazzy.loadSelect7Key != KeyCode.None && snazzy.loadSelect7KeySpecial == specialKeyPressed) LoadSelectionSet (7);
//				if (key.keyCode == snazzy.loadSelect8Key && snazzy.loadSelect8Key != KeyCode.None && snazzy.loadSelect8KeySpecial == specialKeyPressed) LoadSelectionSet (8);
//				if (key.keyCode == snazzy.loadSelect9Key && snazzy.loadSelect9Key != KeyCode.None && snazzy.loadSelect9KeySpecial == specialKeyPressed) LoadSelectionSet (9);
//				if (key.keyCode == snazzy.loadSelect10Key && snazzy.loadSelect10Key != KeyCode.None && snazzy.loadSelect10KeySpecial == specialKeyPressed) LoadSelectionSet (10);
//				if (key.keyCode == snazzy.loadSelect11Key && snazzy.loadSelect11Key != KeyCode.None && snazzy.loadSelect11KeySpecial == specialKeyPressed) LoadSelectionSet (11);
//
//				if (key.keyCode == snazzy.loadCamera0Key && snazzy.loadCamera0Key != KeyCode.None && snazzy.loadCamera0KeySpecial == specialKeyPressed) LoadSceneCameraSet (0);
//				if (key.keyCode == snazzy.loadCamera1Key && snazzy.loadCamera1Key != KeyCode.None && snazzy.loadCamera1KeySpecial == specialKeyPressed) LoadSceneCameraSet (1);
//				if (key.keyCode == snazzy.loadCamera2Key && snazzy.loadCamera2Key != KeyCode.None && snazzy.loadCamera2KeySpecial == specialKeyPressed) LoadSceneCameraSet (2);
//				if (key.keyCode == snazzy.loadCamera3Key && snazzy.loadCamera3Key != KeyCode.None && snazzy.loadCamera3KeySpecial == specialKeyPressed) LoadSceneCameraSet (3);
//				if (key.keyCode == snazzy.loadCamera4Key && snazzy.loadCamera4Key != KeyCode.None && snazzy.loadCamera4KeySpecial == specialKeyPressed) LoadSceneCameraSet (4);
//				if (key.keyCode == snazzy.loadCamera5Key && snazzy.loadCamera5Key != KeyCode.None && snazzy.loadCamera5KeySpecial == specialKeyPressed) LoadSceneCameraSet (5);
//				if (key.keyCode == snazzy.loadCamera6Key && snazzy.loadCamera6Key != KeyCode.None && snazzy.loadCamera6KeySpecial == specialKeyPressed) LoadSceneCameraSet (6);
//				if (key.keyCode == snazzy.loadCamera7Key && snazzy.loadCamera7Key != KeyCode.None && snazzy.loadCamera7KeySpecial == specialKeyPressed) LoadSceneCameraSet (7);
//				if (key.keyCode == snazzy.loadCamera8Key && snazzy.loadCamera8Key != KeyCode.None && snazzy.loadCamera8KeySpecial == specialKeyPressed) LoadSceneCameraSet (8);
//				if (key.keyCode == snazzy.loadCamera9Key && snazzy.loadCamera9Key != KeyCode.None && snazzy.loadCamera9KeySpecial == specialKeyPressed) LoadSceneCameraSet (9);
//				if (key.keyCode == snazzy.loadCamera10Key && snazzy.loadCamera10Key != KeyCode.None && snazzy.loadCamera10KeySpecial == specialKeyPressed) LoadSceneCameraSet (10);
//				if (key.keyCode == snazzy.loadCamera11Key && snazzy.loadCamera11Key != KeyCode.None && snazzy.loadCamera11KeySpecial == specialKeyPressed) LoadSceneCameraSet (11);
//
//				if (key.keyCode == snazzy.loadSelectCamera0Key && snazzy.loadSelectCamera0Key != KeyCode.None && snazzy.loadSelectCamera0KeySpecial == specialKeyPressed) {LoadSceneCameraSet (0);LoadSelectionSet(0);}
//				if (key.keyCode == snazzy.loadSelectCamera1Key && snazzy.loadSelectCamera1Key != KeyCode.None && snazzy.loadSelectCamera1KeySpecial == specialKeyPressed) {LoadSceneCameraSet (1);LoadSelectionSet(1);}
//				if (key.keyCode == snazzy.loadSelectCamera2Key && snazzy.loadSelectCamera2Key != KeyCode.None && snazzy.loadSelectCamera2KeySpecial == specialKeyPressed) {LoadSceneCameraSet (2);LoadSelectionSet(2);}
//				if (key.keyCode == snazzy.loadSelectCamera3Key && snazzy.loadSelectCamera3Key != KeyCode.None && snazzy.loadSelectCamera3KeySpecial == specialKeyPressed) {LoadSceneCameraSet (3);LoadSelectionSet(3);}
//				if (key.keyCode == snazzy.loadSelectCamera4Key && snazzy.loadSelectCamera4Key != KeyCode.None && snazzy.loadSelectCamera4KeySpecial == specialKeyPressed) {LoadSceneCameraSet (4);LoadSelectionSet(4);}
//				if (key.keyCode == snazzy.loadSelectCamera5Key && snazzy.loadSelectCamera5Key != KeyCode.None && snazzy.loadSelectCamera5KeySpecial == specialKeyPressed) {LoadSceneCameraSet (5);LoadSelectionSet(5);}
//				if (key.keyCode == snazzy.loadSelectCamera6Key && snazzy.loadSelectCamera6Key != KeyCode.None && snazzy.loadSelectCamera6KeySpecial == specialKeyPressed) {LoadSceneCameraSet (6);LoadSelectionSet(6);}
//				if (key.keyCode == snazzy.loadSelectCamera7Key && snazzy.loadSelectCamera7Key != KeyCode.None && snazzy.loadSelectCamera7KeySpecial == specialKeyPressed) {LoadSceneCameraSet (7);LoadSelectionSet(7);}
//				if (key.keyCode == snazzy.loadSelectCamera8Key && snazzy.loadSelectCamera8Key != KeyCode.None && snazzy.loadSelectCamera8KeySpecial == specialKeyPressed) {LoadSceneCameraSet (8);LoadSelectionSet(8);}
//				if (key.keyCode == snazzy.loadSelectCamera9Key && snazzy.loadSelectCamera9Key != KeyCode.None && snazzy.loadSelectCamera9KeySpecial == specialKeyPressed) {LoadSceneCameraSet (9);LoadSelectionSet(9);}
//				if (key.keyCode == snazzy.loadSelectCamera10Key && snazzy.loadSelectCamera10Key != KeyCode.None && snazzy.loadSelectCamera10KeySpecial == specialKeyPressed) {LoadSceneCameraSet (10);LoadSelectionSet(10);}
//				if (key.keyCode == snazzy.loadSelectCamera11Key && snazzy.loadSelectCamera11Key != KeyCode.None && snazzy.loadSelectCamera11KeySpecial == specialKeyPressed) {LoadSceneCameraSet (11);LoadSelectionSet(11);}
		
				// if (key.keyCode == KeyCode.R) {snazzyS.selectionList.Clear ();Debug.Log ("Clear");}

				if (key.keyCode == snazzy.gridKey && snazzy.gridKey != KeyCode.None && snazzy.gridKeySpecial == specialKeyPressed) {
					snazzy.showGrid = !snazzy.showGrid;
					SceneView.RepaintAll();
					SnazzyWindowRepaint();
				}
				if (key.keyCode == snazzy.movToggleKey && snazzy.movToggleKey != KeyCode.None && snazzy.movToggleKeySpecial == specialKeyPressed) {
					snazzy.mov2 = !snazzy.mov2;
					SnazzyWindowRepaint();
				}
				if (key.keyCode == snazzy.angleToggleKey && snazzy.angleToggleKey != KeyCode.None && snazzy.angleToggleKeySpecial == specialKeyPressed) {
					snazzy.angle2 = !snazzy.angle2;
					SnazzyWindowRepaint();
				}
				if (key.keyCode == snazzy.rotXYZKey && snazzy.rotXYZKey != KeyCode.None && snazzy.rotXYZKeySpecial == specialKeyPressed) {
					++snazzy.rotAxis;
					if (snazzy.rotAxis > 2) snazzy.rotAxis = 0;
					SnazzyWindowRepaint();
				}
				if (key.keyCode == snazzy.duplicateKey && snazzy.duplicateKey != KeyCode.None && snazzy.duplicateKeySpecial == specialKeyPressed) {
					DuplicateSelection(fromScene);
				}
				if (key.keyCode == snazzy.focusKey && snazzy.focusKey != KeyCode.None && snazzy.focusKeySpecial == specialKeyPressed) {
					snazzy.lookAtSelect = !snazzy.lookAtSelect;
					SnazzyWindowRepaint();
				}

				if (key.keyCode == snazzy.snapKey && snazzy.snapKey != KeyCode.None && snazzy.snapKeySpecial == specialKeyPressed) {
					if (snazzy.snapKeyTapped == 0) ToggleSnap();
					++snazzy.snapKeyTapped;
					if (snazzy.snapKeyTapped > 1) {
						SnapTransform ();
						
						ToggleSnap ();
						snazzy.snapKeyTapped = 0;
					} else {
						snazzy.snapKeyDown = true;
						snazzy.snapKeyDownStart = Time.realtimeSinceStartup;		
					}
				}
				
				if (key.keyCode == snazzy.snapPosKey && snazzy.snapPosKey != KeyCode.None && snazzy.snapPosKeySpecial == specialKeyPressed) {
					if (snazzy.snapPosKeyTapped == 0) ToggleMovSnap();
					++snazzy.snapPosKeyTapped;
					if (snazzy.snapPosKeyTapped > 1) {
						SnapPos(snazzy.autoSnapPosX,snazzy.autoSnapPosY,snazzy.autoSnapPosZ);
						
						ToggleMovSnap ();
						snazzy.snapPosKeyTapped = 0;
					} else {
						snazzy.snapPosKeyDown = true;
						snazzy.snapPosKeyDownStart = Time.realtimeSinceStartup;		
					}
				}
				if (key.keyCode == snazzy.snapRotKey && snazzy.snapRotKey != KeyCode.None && snazzy.snapRotKeySpecial == specialKeyPressed) {
					if (snazzy.snapRotKeyTapped == 0) ToggleRotSnap();
					++snazzy.snapRotKeyTapped;
					if (snazzy.snapRotKeyTapped > 1) {
						SnapRot(snazzy.rotAmount,snazzy.autoSnapRotX,snazzy.autoSnapRotY,snazzy.autoSnapRotZ);
						
						ToggleRotSnap ();
						snazzy.snapRotKeyTapped = 0;
					} else {
						snazzy.snapRotKeyDown = true;
						snazzy.snapRotKeyDownStart = Time.realtimeSinceStartup;		
					}
				}
				if (key.keyCode == snazzy.snapScaleKey && snazzy.snapScaleKey != KeyCode.None && snazzy.snapScaleKeySpecial == specialKeyPressed) {
					if (snazzy.snapScaleKeyTapped == 0) ToggleScaleSnap();
					++snazzy.snapScaleKeyTapped;
					if (snazzy.snapScaleKeyTapped > 1) {
						SnapScale(snazzy.autoSnapScaleX,snazzy.autoSnapScaleY,snazzy.autoSnapScaleZ);
						
						ToggleScaleSnap ();
						snazzy.snapScaleKeyTapped = 0;
					} else {
						snazzy.snapScaleKeyDown = true;
						snazzy.snapScaleKeyDownStart = Time.realtimeSinceStartup;		
					}
				}
				if (key.keyCode == snazzy.resetTransformKey && snazzy.resetTransformKey != KeyCode.None && snazzy.resetTransformKeySpecial == specialKeyPressed) {
					ResetPosition (true,true,true);
					ResetRotation (true,true,true);
					ResetScale (true,true,true);
				}
				if (key.keyCode == snazzy.resetPositionKey && snazzy.resetPositionKey != KeyCode.None && snazzy.resetPositionKeySpecial == specialKeyPressed) {
					ResetPosition (true,true,true);
				}
				if (key.keyCode == snazzy.resetRotationKey && snazzy.resetRotationKey != KeyCode.None && snazzy.resetRotationKeySpecial == specialKeyPressed) {
					ResetRotation (true,true,true);
				}
				if (key.keyCode == snazzy.resetScaleKey && snazzy.resetScaleKey != KeyCode.None && snazzy.resetScaleKeySpecial == specialKeyPressed) {
					ResetScale (true,true,true);
				}
				if (key.keyCode == snazzy.childCompensationKey && snazzy.childCompensationKey != KeyCode.None && snazzy.childCompensationKeySpecial == specialKeyPressed) {  
					if (!snazzy.lockChild) UnparentChildren(); else ParentChildren();
				}
				if (key.keyCode == snazzy.parentKey && snazzy.parentKey != KeyCode.None && snazzy.parentKeySpecial == specialKeyPressed) {  
					ParentSelection();
				}
	
				if (key.keyCode == snazzy.parentToCenterKey && snazzy.parentToCenterKey != KeyCode.None && snazzy.parentToCenterKeySpecial == specialKeyPressed) {
					ParentSelectionToCenter();
				}
				
				if (key.keyCode == snazzy.centerCurrentParentKey && snazzy.centerCurrentParentKey != KeyCode.None && snazzy.parentKeySpecial == snazzy.centerCurrentParentKeySpecial) {  
					if (Selection.activeTransform.parent != null) MoveParentToCenter(Selection.activeTransform.parent);
				}
				
				if (key.keyCode == snazzy.hierarchyUpKey && snazzy.hierarchyUpKey != KeyCode.None && snazzy.hierarchyUpKeySpecial == specialKeyPressed) {
					SelectParent();
				} 
				
				if (key.keyCode == snazzy.hierarchyDownKey && snazzy.hierarchyDownKey != KeyCode.None && snazzy.hierarchyDownKeySpecial == specialKeyPressed) {
					SelectChildren();
				}

				if (key.keyCode == snazzy.unparentKey && snazzy.unparentKey != KeyCode.None && snazzy.unparentKeySpecial == specialKeyPressed) {
					UnparentSelection();
				}

//				if (key.keyCode == snazzy.unparentDeleteKey && snazzy.unparentDeleteKey != KeyCode.None && snazzy.unparentDeleteKeySpecial == specialKeyPressed) {
//					UnparentSelectionDelete();
//				}
			}
		}
		#if UNITY_4_6 || UNITY_5_0
		UGUI();
		#endif
		CheckSnap();
	}

	public Texture BackGround;
	Texture AngleLabel,AnglePreset1,AnglePreset2,CCompToggleOff,CCompToggleOn,Duplicate,GridSizePreset,GridToggleOff,GridToggleOn,HierarchyDown,HierarchyUp,IncrementLabel,IncrementPreset1,ManualHover,ForumHover,
		IncrementPreset2,MoveBack,MoveDown,MoveForward,MoveLeft,MoveRight,MoveUp,Parent,PositionToggleOff,PositionToggleOn,PositionXToggleOff,PositionXToggleOn,PositionYToggleOff,PositionYToggleOn,
		PositionZToggleOff,PositionZToggleOn,RotateLeft,RotateRight,RotationToggleOff,RotationToggleOn,RotationXToggleOff,RotationXToggleOn,RotationYToggleOn,RotationYToggleOff,RotationZToggleOff,RotationZToggleOn,ScaleToggleOff,
		ScaleToggleOn,ScaleXToggleOff,ScaleXToggleOn,ScaleYToggleOff,ScaleYToggleOn,ScaleZToggleOff,ScaleZToggleOn,SettingsToggleOff,SettingsToggleOn,SnapToggleOff,SnapToggleOn,SnazzyTitle,Spacer,Unparent,
		SnapMask,XYZMask,FocusToggleOff,FocusToggleOn,IncrementPreset1ToggleOff,IncrementPreset2ToggleOff,AnglePreset1ToggleOff,AnglePreset2ToggleOff,RotationModeToggleOn,RotationModeToggleOff,Quicktips,GradientFade,SelectionNext,
		SelectionPrev,SaveToggleOn,SaveToggleOff,SaveToggleCamOnSelOn,SaveToggleCamOffSelOff,SaveToggleCamOnSelOff,SaveToggleCamOffSelOn;
	
	public void LoadButtons()
	{
		string path = installPath+"/Sources/GUI/";

		if (snazzy.greyScale) path += "UIGrey/"; else path += "UIColor/";

		BackGround = AssetDatabase.LoadAssetAtPath(path+"BG.jpg",typeof(Texture)) as Texture;
		ManualHover = AssetDatabase.LoadAssetAtPath(path+"ManualHover.psd",typeof(Texture)) as Texture;
		ForumHover = AssetDatabase.LoadAssetAtPath(path+"ForumHover.psd",typeof(Texture)) as Texture;
		
		IncrementPreset1ToggleOff = AssetDatabase.LoadAssetAtPath(path+"IncrementPreset1ToggleOff.png",typeof(Texture)) as Texture;
		IncrementPreset2ToggleOff = AssetDatabase.LoadAssetAtPath(path+"IncrementPreset2ToggleOff.png",typeof(Texture)) as Texture;
		AnglePreset1ToggleOff = AssetDatabase.LoadAssetAtPath(path+"AnglePreset1ToggleOff.png",typeof(Texture)) as Texture;
		AnglePreset2ToggleOff = AssetDatabase.LoadAssetAtPath(path+"AnglePreset2ToggleOff.png",typeof(Texture)) as Texture;
		
		AngleLabel = AssetDatabase.LoadAssetAtPath(path+"AngleLabel.png",typeof(Texture)) as Texture;
		AnglePreset1 = AssetDatabase.LoadAssetAtPath(path+"AnglePreset1.png",typeof(Texture)) as Texture;
		AnglePreset2 = AssetDatabase.LoadAssetAtPath(path+"AnglePreset2.png",typeof(Texture)) as Texture;
		CCompToggleOff = AssetDatabase.LoadAssetAtPath(path+"CCompToggleOff.png",typeof(Texture)) as Texture;
		CCompToggleOn = AssetDatabase.LoadAssetAtPath(path+"CCompToggleOn.png",typeof(Texture)) as Texture;
		Duplicate = AssetDatabase.LoadAssetAtPath(path+"Duplicate.png",typeof(Texture)) as Texture;
		GridSizePreset = AssetDatabase.LoadAssetAtPath(path+"GridSizePreset.png",typeof(Texture)) as Texture;
		GridToggleOff = AssetDatabase.LoadAssetAtPath(path+"GridToggleOff.png",typeof(Texture)) as Texture;
		GridToggleOn = AssetDatabase.LoadAssetAtPath(path+"GridToggleOn.png",typeof(Texture)) as Texture;
		HierarchyDown = AssetDatabase.LoadAssetAtPath(path+"HierarchyDown.png",typeof(Texture)) as Texture;
		HierarchyUp = AssetDatabase.LoadAssetAtPath(path+"HierarchyUp.png",typeof(Texture)) as Texture;
		IncrementLabel = AssetDatabase.LoadAssetAtPath(path+"IncrementLabel.png",typeof(Texture)) as Texture;
		IncrementPreset1 = AssetDatabase.LoadAssetAtPath(path+"IncrementPreset1.png",typeof(Texture)) as Texture;
		IncrementPreset2 = AssetDatabase.LoadAssetAtPath(path+"IncrementPreset2.png",typeof(Texture)) as Texture;
		MoveBack = AssetDatabase.LoadAssetAtPath(path+"MoveBack.png",typeof(Texture)) as Texture;
		MoveDown = AssetDatabase.LoadAssetAtPath(path+"MoveDown.png",typeof(Texture)) as Texture;
		MoveForward = AssetDatabase.LoadAssetAtPath(path+"MoveForward.png",typeof(Texture)) as Texture;
		MoveLeft = AssetDatabase.LoadAssetAtPath(path+"MoveLeft.png",typeof(Texture)) as Texture;
		MoveRight = AssetDatabase.LoadAssetAtPath(path+"MoveRight.png",typeof(Texture)) as Texture;
		MoveUp = AssetDatabase.LoadAssetAtPath(path+"MoveUp.png",typeof(Texture)) as Texture;
		Parent = AssetDatabase.LoadAssetAtPath(path+"Parent.png",typeof(Texture)) as Texture;
		PositionToggleOff = AssetDatabase.LoadAssetAtPath(path+"PositionToggleOff.png",typeof(Texture)) as Texture;
		PositionToggleOn = AssetDatabase.LoadAssetAtPath(path+"PositionToggleOn.png",typeof(Texture)) as Texture;
		PositionXToggleOff = AssetDatabase.LoadAssetAtPath(path+"PositionXToggleOff.png",typeof(Texture)) as Texture;
		PositionXToggleOn = AssetDatabase.LoadAssetAtPath(path+"PositionXToggleOn.png",typeof(Texture)) as Texture;
		PositionYToggleOff = AssetDatabase.LoadAssetAtPath(path+"PositionYToggleOff.png",typeof(Texture)) as Texture;
		PositionYToggleOn = AssetDatabase.LoadAssetAtPath(path+"PositionYToggleOn.png",typeof(Texture)) as Texture;
		PositionZToggleOff = AssetDatabase.LoadAssetAtPath(path+"PositionZToggleOff.png",typeof(Texture)) as Texture;
		PositionZToggleOn = AssetDatabase.LoadAssetAtPath(path+"PositionZToggleOn.png",typeof(Texture)) as Texture;
		RotateLeft = AssetDatabase.LoadAssetAtPath(path+"RotateLeft.png",typeof(Texture)) as Texture;
		RotateRight = AssetDatabase.LoadAssetAtPath(path+"RotateRight.png",typeof(Texture)) as Texture;
		RotationToggleOff = AssetDatabase.LoadAssetAtPath(path+"RotationToggleOff.png",typeof(Texture)) as Texture;
		RotationToggleOn = AssetDatabase.LoadAssetAtPath(path+"RotationToggleOn.png",typeof(Texture)) as Texture;
		RotationXToggleOff = AssetDatabase.LoadAssetAtPath(path+"RotationXToggleOff.png",typeof(Texture)) as Texture;
		RotationXToggleOn = AssetDatabase.LoadAssetAtPath(path+"RotationXToggleOn.png",typeof(Texture)) as Texture;
		RotationYToggleOn = AssetDatabase.LoadAssetAtPath(path+"RotationYToggleOn.png",typeof(Texture)) as Texture;
		RotationYToggleOff = AssetDatabase.LoadAssetAtPath(path+"RotationYToggleOff.png",typeof(Texture)) as Texture;
		RotationZToggleOff = AssetDatabase.LoadAssetAtPath(path+"RotationZToggleOff.png",typeof(Texture)) as Texture;
		RotationZToggleOn = AssetDatabase.LoadAssetAtPath(path+"RotationZToggleOn.png",typeof(Texture)) as Texture;
		ScaleToggleOff = AssetDatabase.LoadAssetAtPath(path+"ScaleToggleOff.png",typeof(Texture)) as Texture;
		ScaleToggleOn = AssetDatabase.LoadAssetAtPath(path+"ScaleToggleOn.png",typeof(Texture)) as Texture;
		ScaleXToggleOff = AssetDatabase.LoadAssetAtPath(path+"ScaleXToggleOff.png",typeof(Texture)) as Texture;
		ScaleXToggleOn = AssetDatabase.LoadAssetAtPath(path+"ScaleXToggleOn.png",typeof(Texture)) as Texture;
		ScaleYToggleOff = AssetDatabase.LoadAssetAtPath(path+"ScaleYToggleOff.png",typeof(Texture)) as Texture;
		ScaleYToggleOn = AssetDatabase.LoadAssetAtPath(path+"ScaleYToggleOn.png",typeof(Texture)) as Texture;
		ScaleZToggleOff = AssetDatabase.LoadAssetAtPath(path+"ScaleZToggleOff.png",typeof(Texture)) as Texture;
		ScaleZToggleOn = AssetDatabase.LoadAssetAtPath(path+"ScaleZToggleOn.png",typeof(Texture)) as Texture;
		SettingsToggleOn = AssetDatabase.LoadAssetAtPath(path+"SettingsToggleOn.png",typeof(Texture)) as Texture;
		SettingsToggleOff = AssetDatabase.LoadAssetAtPath(path+"SettingsToggleOff.png",typeof(Texture)) as Texture;
		SnapToggleOff = AssetDatabase.LoadAssetAtPath(path+"SnapToggleOff.png",typeof(Texture)) as Texture;
		SnapToggleOn = AssetDatabase.LoadAssetAtPath(path+"SnapToggleOn.png",typeof(Texture)) as Texture;
		SnazzyTitle = AssetDatabase.LoadAssetAtPath(path+"SnazzyTitle.png",typeof(Texture)) as Texture;
		Spacer = AssetDatabase.LoadAssetAtPath(path+"Spacer.png",typeof(Texture)) as Texture;
		Unparent = AssetDatabase.LoadAssetAtPath(path+"Unparent.png",typeof(Texture)) as Texture;
		FocusToggleOff = AssetDatabase.LoadAssetAtPath(path+"FocusToggleOff.png",typeof(Texture)) as Texture;
		FocusToggleOn = AssetDatabase.LoadAssetAtPath(path+"FocusToggleOn.png",typeof(Texture)) as Texture;
		SelectionNext = AssetDatabase.LoadAssetAtPath(path+"SelectionNext.png",typeof(Texture)) as Texture;
		SelectionPrev = AssetDatabase.LoadAssetAtPath(path+"SelectionPrev.png",typeof(Texture)) as Texture;
		//SaveToggleOn = AssetDatabase.LoadAssetAtPath(path+"SaveToggleOn.png",typeof(Texture)) as Texture;
		//SaveToggleOff = AssetDatabase.LoadAssetAtPath(path+"SaveToggleOff.png",typeof(Texture)) as Texture;
		SaveToggleCamOnSelOn = AssetDatabase.LoadAssetAtPath(path+"SaveToggleCamOnSelOn.png",typeof(Texture)) as Texture;
		SaveToggleCamOffSelOff = AssetDatabase.LoadAssetAtPath(path+"SaveToggleCamOffSelOff.png",typeof(Texture)) as Texture;
		SaveToggleCamOnSelOff = AssetDatabase.LoadAssetAtPath(path+"SaveToggleCamOnSelOff.png",typeof(Texture)) as Texture;
		SaveToggleCamOffSelOn = AssetDatabase.LoadAssetAtPath(path+"SaveToggleCamOffSelOn.png",typeof(Texture)) as Texture;
		
		SnapMask = AssetDatabase.LoadAssetAtPath(path+"SnapMask.png",typeof(Texture)) as Texture;
		XYZMask = AssetDatabase.LoadAssetAtPath(path+"XYZMask.png",typeof(Texture)) as Texture;
		RotationModeToggleOn = AssetDatabase.LoadAssetAtPath(path+"RotationModeToggleOn.png",typeof(Texture)) as Texture;
		RotationModeToggleOff = AssetDatabase.LoadAssetAtPath(path+"RotationModeToggleOff.png",typeof(Texture)) as Texture;
		Quicktips = AssetDatabase.LoadAssetAtPath(path+"SnazzyToolsGridQuicktips.jpg",typeof(Texture)) as Texture;
		GradientFade = AssetDatabase.LoadAssetAtPath(path+"GradientFade.png",typeof(Texture)) as Texture;
	}

	static void InitReferences()    
	{
		snazzyGrid = ObjectSearch.Find("##SnazzyGrid##(Clone)");
		camReference = ObjectSearch.Find("##SnazzyCameraReference##(Clone)");
		snazzySettings = GameObject.Find("##SnazzySettings##");

		// DestroyImmediate (snazzyGrid); 
		if (snazzyGrid != null) {
			if (snazzyGrid.hideFlags == HideFlags.HideAndDontSave) DestroyImmediate(snazzyGrid);
		}

		if (snazzyGrid == null)
		{
		  // Debug.Log("instantiage SnazzyGrid");
			snazzyGrid = Instantiate(AssetDatabase.LoadAssetAtPath(installPath+"/Sources/##SnazzyGrid##.prefab", typeof (GameObject)), Vector3.zero, Quaternion.identity) as GameObject;
		  	snazzyGrid.name = "##SnazzyGrid##(Clone)";
			snazzyGrid.hideFlags = HideFlags.HideInHierarchy;
		}

		if (snazzyGrid != null) {
			snazzyGridRenderer = snazzyGrid.GetComponent<MeshRenderer>() as MeshRenderer;
			snazzyS = snazzyGrid.GetComponent<SnazzyScene>() as SnazzyScene;
			if (snazzyS != null) snazzyS.Init();
		}

		if (camReference == null)
		{
			// Debug.Log("instantiage SnazzyCameraReference");
			camReference = new GameObject();
			camReference.name = "##SnazzyCameraReference##(Clone)";
			camReference.hideFlags = HideFlags.HideAndDontSave;
		}
		if (snazzySettings == null) {
			// Debug.Log("instantiage SnazzySettings");
			snazzySettings = AssetDatabase.LoadAssetAtPath(installPath+"/Sources/SnazzySettings.prefab",typeof (GameObject)) as GameObject;
			snazzySettings.name = "##SnazzySettings##";
		}

		// Debug.Log ("i");
		if (snazzySettings != null) {
			snazzy = snazzySettings.GetComponent<SnazzySettings>() as SnazzySettings;
			// snazzy.gridIsEnabled = true;
			
			// CheckSnazzyGrid();
		}


	}
	
	static void DisableSnazzyGrid()
	{
		DestroyImmediate (snazzyGrid);
		DestroyImmediate (camReference);
		snazzy.gridIsEnabled = false;
	}
	
	static void ClearSnazzyGrid()
	{
		// Debug.Log ("ClearSnazzyGrid");
		snazzyGrid = ObjectSearch.Find("##SnazzyGrid##(Clone)");

		snazzy.gridIsEnabled = false;
		DestroyImmediate (snazzyGrid);
		// DestroyImmediate (camReference);
	}
	
	static void CheckSnazzyGrid()
	{
		if (snazzy != null) {
			if (snazzy.showGrid && !snazzy.gridIsEnabled) {
				ShowGrid();					
			}
		}
	}
	
	static void ShowGrid() 
	{
		// Debug.Log ("s");
		#if !UNITY_3_4 && !UNITY_3_5
		if (snazzyGrid != null) snazzyGrid.SetActive(true);
		#else
		if (snazzyGrid != null) snazzyGrid.active = true;
		#endif
		snazzy.gridIsEnabled = true;
	}
	
	static void HideGrid()
	{
		#if !UNITY_3_4 && !UNITY_3_5
		if (snazzyGrid != null) snazzyGrid.SetActive(false);
		#else
		if (snazzyGrid != null) snazzyGrid.active = false;
		#endif
		snazzy.gridIsEnabled = false;
	}

	static void LoadSelectionCamera(int index,bool select,bool camera)
	{
		if ((select && camera) || (!select && !camera)) {LoadSceneCameraSet(index);LoadSelectionSet(index);}
		else if (select) LoadSelectionSet(index);
		else LoadSceneCameraSet(index);
	}

	static void SaveSceneCamera(int index)
	{
		ClearSceneCamera(index);
		snazzyS.sceneCameras[index].isSaved = true;
		snazzyS.sceneCameras[index].pivot = SceneView.lastActiveSceneView.pivot;
		snazzyS.sceneCameras[index].size = SceneView.lastActiveSceneView.size;
		snazzyS.sceneCameras[index].rotation = SceneView.lastActiveSceneView.rotation;
		snazzyS.sceneCameras[index].orthographic = SceneView.lastActiveSceneView.orthographic;
		snazzyS.sceneCameras[index].in2DMode = snazzyS.sceneCameras[index].in2DMode;

		//Debug.Log ("Save camera "+index.ToString());
	}
	static void ClearSceneCamera(int index)
	{
		snazzyS.sceneCameras[index].isSaved = false;
		snazzyS.sceneCameras[index].pivot = Vector3.zero;
		snazzyS.sceneCameras[index].size = 0;
		snazzyS.sceneCameras[index].rotation = Quaternion.identity;
		snazzyS.sceneCameras[index].orthographic = false;
		snazzyS.sceneCameras[index].in2DMode = false;
		
		//Debug.Log ("Clear camera "+index.ToString());
	}
	static void LoadSceneCameraSet(int index)
	{
		if (snazzyS.sceneCameras[index].isSaved){
			SceneView.lastActiveSceneView.pivot = snazzyS.sceneCameras[index].pivot;
			SceneView.lastActiveSceneView.size = snazzyS.sceneCameras[index].size;
			SceneView.lastActiveSceneView.rotation = snazzyS.sceneCameras[index].rotation;
			SceneView.lastActiveSceneView.orthographic = snazzyS.sceneCameras[index].orthographic;
			SceneView.lastActiveSceneView.in2DMode = snazzyS.sceneCameras[index].in2DMode;

			//Debug.Log ("Load camera "+index.ToString ());
		}
	}

	static void SaveSelectionSet(int index){
		if (Selection.objects.Length != 0){
			ClearSelectionSet(index);
			snazzyS.selectionSet[index].isSaved = true;
			snazzyS.selectionSet[index].objects = Selection.objects;
			//Debug.Log ("Save Selection Set "+index.ToString ());
		}
		else notify = "You have to select something first.";
	}

	static void ClearSelectionSet(int index){
		snazzyS.selectionSet[index].isSaved = false;
		snazzyS.selectionSet[index].objects = null;
		//Debug.Log ("Clear Selection Set "+index.ToString ());
	}

	static void LoadSelectionSet(int index){
		if (snazzyS.selectionSet[index].isSaved){
			Selection.objects = snazzyS.selectionSet[index].objects;
			//Debug.Log ("Load Selection Set "+index.ToString ());
		}
	}
	
	static void SelectNext()
	{
		// Debug.Log("Select Next"); 
		if (snazzyS.selectionList.Count == 0) return;
		CleanSelectionList();
		++snazzy.selectionListIndex;
		if (snazzy.selectionListIndex > snazzyS.selectionList.Count-1) snazzy.selectionListIndex = snazzyS.selectionList.Count-1; 
		notify = snazzy.selectionListIndex.ToString ();
		Selection.objects = snazzyS.selectionList[snazzy.selectionListIndex].objects;
		snazzy.oldActiveObject = Selection.activeObject;
		SceneView.RepaintAll();
	}

	static void SelectPrevious()
	{
		// Debug.Log ("Select Previous");
		if (snazzyS.selectionList.Count == 0) return;
		CleanSelectionList(); 
		--snazzy.selectionListIndex;
		if (snazzy.selectionListIndex < 0) snazzy.selectionListIndex = 0;
		if (snazzy.selectionListIndex > snazzyS.selectionList.Count-1) snazzy.selectionListIndex = snazzyS.selectionList.Count-1; 

		notify = snazzy.selectionListIndex.ToString ();
		Selection.objects = snazzyS.selectionList[snazzy.selectionListIndex].objects;
		snazzy.oldActiveObject = Selection.activeObject;
		SceneView.RepaintAll();
	}

	static void CleanSelectionList()
	{
		for (int i = 0;i < snazzyS.selectionList.Count;++i) {
			if (CheckObjectsNull (snazzyS.selectionList[i].objects)) {
				snazzyS.selectionList.RemoveAt (i);
				--i;
			}
		}
		// Debug.Log (snazzyS.selectionList.Count);
	}

	static bool CheckObjectsNull(Object[] objects)
	{
		bool isNull = true;
		for (int i = 0;i < objects.Length;++i) {
			if (objects[i] != null) {isNull = false;break;}
		}
		return isNull;
	}
	
	void OnInspectorUpdate()
	{
		// Debug.Log (Selection.activeObject.name);

		if (Selection.activeObject != null) {
			if (Selection.objects.Length != snazzy.oldObjectsLength && Selection.objects.Length > 1 && snazzy.selectionListIndex == snazzyS.selectionList.Count-1) {
				if (snazzyS.selectionList.Count == 0) snazzyS.selectionList.Add (new SelectedObjects(Selection.objects));
				else snazzyS.selectionList[snazzyS.selectionList.Count-1].objects = Selection.objects;
				snazzy.oldObjectsLength = Selection.objects.Length;
				snazzy.oldActiveObject = Selection.activeObject;
			}
			else if (Selection.activeObject != snazzy.oldActiveObject) {
				snazzyS.selectionList.Add (new SelectedObjects(Selection.objects));
				if (snazzyS.selectionList.Count > snazzy.selectionListLength) snazzyS.selectionList.RemoveAt(0);

				snazzy.oldActiveObject = Selection.activeObject;
				snazzy.selectionListIndex = snazzyS.selectionList.Count-1;
			}
		}

		if (Selection.activeTransform != null) {
			if (Selection.activeTransform != snazzy.oldSelectedTransform || Selection.transforms.Length != snazzy.oldSelectedLength) {
				snazzy.oldSelectedTransform = Selection.activeTransform;
				snazzy.oldSelectedLength = Selection.transforms.Length;
			}
		}
		
		if (EditorApplication.isPlaying) HideGrid();
		else CheckSnazzyGrid();
		
		if (AngleLabel == null) {
			LoadButtons (); 
			Repaint ();
		}
		
		// Repainting Snazzy Window if Camera X-Axis changes
		if (sceneCamera != null) {
			if (sceneCamera.transform.localEulerAngles.x != cameraXAxis) Repaint ();
			cameraXAxis = sceneCamera.transform.localEulerAngles.x;
		}

		if (snazzy.snapKeyTapped == 1 && !snazzy.snapKeyDown) {
			if (Time.realtimeSinceStartup-snazzy.snapKeyDownStart > 0.7f) {ToggleSnap();snazzy.snapKeyTapped = 0;}
			if (Time.realtimeSinceStartup-snazzy.snapKeyDownStart > 0.5f) snazzy.snapKeyTapped = 0;
			
		}
		if (snazzy.snapPosKeyTapped == 1 && !snazzy.snapPosKeyDown) {
			if (Time.realtimeSinceStartup-snazzy.snapPosKeyDownStart > 0.7f) {ToggleMovSnap();snazzy.snapPosKeyTapped = 0;}
			if (Time.realtimeSinceStartup-snazzy.snapPosKeyDownStart > 0.5f) snazzy.snapPosKeyTapped = 0;
			
		}
		if (snazzy.snapRotKeyTapped == 1 && !snazzy.snapRotKeyDown) {
			if (Time.realtimeSinceStartup-snazzy.snapRotKeyDownStart > 0.7f) {ToggleRotSnap();snazzy.snapRotKeyTapped = 0;}
			if (Time.realtimeSinceStartup-snazzy.snapRotKeyDownStart > 0.5f) snazzy.snapRotKeyTapped = 0;
			
		}
		if (snazzy.snapScaleKeyTapped == 1 && !snazzy.snapScaleKeyDown) {
			if (Time.realtimeSinceStartup-snazzy.snapScaleKeyDownStart > 0.7f) {ToggleScaleSnap();snazzy.snapScaleKeyTapped = 0;}
			if (Time.realtimeSinceStartup-snazzy.snapScaleKeyDownStart > 0.5f) snazzy.snapScaleKeyTapped = 0;
			
		}
	}
	
	#if UNITY_4_6 || UNITY_5_0
	static void UGUI()
	{
		// Debug.Log (Tools.handlePosition);

		// Tools.handlePosition = new Vector2(0,0);

		if (Selection.activeTransform == null) return;
		// if (Event.current.type == EventType.MouseDrag) return;

		uGUISelect = false;

		// Debug.Log (Tools.handleRect+", "+snazzy.oldHandleRect);
		if (Selection.activeTransform != snazzy.uGUIoldTransform) {
			if (Tools.current == Tool.Rect) snazzy.oldHandleRect = Tools.handleRect;
			snazzy.uGUIoldTransform = Selection.activeTransform;
			snazzy.uGUIoldPosition = Selection.activeTransform.position;
		}

		if (Tools.current != Tool.Rect) return;
		bool uGUIsnap = ((Event.current.type == EventType.MouseUp || keySnap) && (Selection.activeTransform.position != snazzy.uGUIoldPosition || Tools.handleRect.width != snazzy.oldHandleRect.width || Tools.handleRect.height != snazzy.oldHandleRect.height));

		for (int i = 0;i < Selection.transforms.Length;++i) {
			rectT = Selection.transforms [i].GetComponent<RectTransform> ();
			if (rectT != null) {
				uGUISelect = true;
				// Debug.Log (keySnap);
				if (snazzy.autoSnapPos && snazzy.autoSnap && uGUIsnap) {
					pivotOld = Tools.handlePosition;
					rectT.sizeDelta = new Vector2 (Mathf.Round(rectT.sizeDelta.x/snazzy.gridSize)*snazzy.gridSize,Mathf.Round(rectT.sizeDelta.y/snazzy.gridSize)*snazzy.gridSize);
					// Debug.Log (Tools.handlePosition);
					SnapPos(snazzy.autoSnapPosX,snazzy.autoSnapPosY,snazzy.autoSnapPosZ);
					// Debug.Log (typeof(Tools).GetField ("handlePosition").Name);//SetValue (Tools,pivotOld);
				}
			}
		}
	}
	#endif

	static void OnHierarchy (int instanceID, Rect selectionRect)
	{
		if (snazzy.offsetObject != null) {
			if (snazzy.offsetObject.gameObject.GetInstanceID() == instanceID) {
				GUI.color = Color.green;
				selectionRect.y += 1;
				#if UNITY_4_3 || UNITY_4_4
				selectionRect.x -= 2;
				#endif	
				EditorGUI.LabelField(selectionRect,snazzy.offsetObject.name);
				if (GUI.Button (new Rect(selectionRect.xMax-24-42,selectionRect.y-1,20,15),"R",EditorStyles.miniButtonMid)) {
					snazzy.offsetObject = null;
				}
				GUI.color = Color.white;
			}
		}

		if (selectionRect.y < 10) {
			// Debug.Log (selectionRect);
			Key (Event.current,false);
		}
	}

	static void OnProject(string instanceID, Rect selectionRect)
	{
		if (selectionRect.y < 10) {
			// Debug.Log (selectionRect);
			Key (Event.current,false);
		}
	}
}

	