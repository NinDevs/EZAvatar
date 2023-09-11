#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using EZAva2;

namespace EZAva2
{
    public enum ControlType
    {
        Toggle = 0,
        RadialPuppet = 1
    }

    public enum MenuGenOption
    {
        Automatic = 0,
        AddToSelectedMenu = 1
    }

    public class Category
    {
        public string name;
        public bool foldout;
        public int slots { get; internal set; }
        public Material[] materials;
        public GameObject[] objects = new GameObject[1];
        public List<AnimationClip> animClips = new List<AnimationClip>();
        public AnimatorControllerLayer layer = null;
        public List<AnimatorState> states = new List<AnimatorState>();
        public bool switched { get; set; }
        public bool makeIdle { get; set; }
        public bool layerExists { get; set; }
        public BlendshapeValueData blendShapeData { get; set; }
        public ControlType menuControl { get; set; }
        public MenuGenOption menuGenerationType { get; set; }
        public VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu menu { get; set; }
    }    
}
  
    public class EZAvatar : EditorWindow
    {
        [MenuItem("Nin/EZAvatar/Open GitHub")]
        static void OpenGitHub()
        {
            Application.OpenURL("https://github.com/NinDevs/EZAvatar");
        }

        [MenuItem("Nin/EZAvatar/3.0 Helper")]

        static void Init()
        {
            //Creating a new editor window, and then shows it
            EZAvatar window = (EZAvatar)GetWindow(typeof(EZAvatar));
            window.Show();
            
            Updater.CheckForUpdates(true);
        }
        private static GameObject previousAvatar;
        private static GameObject back;
        public static GameObject avatar
        {
            get => back; set
            {
                back = value;
                if (previousAvatar != avatar)
                {
                    previousAvatar = avatar;
                    Helper.HasFXLayer();
                    ReInitializeUI();               
                }

            }
        }

        public static AnimatorController controller = null;
        private bool MaterialFoldout;
        private bool GameObjFoldout;
        private bool blendshapeFoldout;
        private static Vector2 categoriesScrollView;
        public static List<Category> objCategories = new List<Category>();
        public static List<Category> matCategories = new List<Category>();
        public static List<Category> blendCategories = new List<Category>();
        private static bool[] blendBools = new bool[1];
        private static string matEnterText;
        private static string objEnterText;
        private static string blendEnterText;
        public static string debug;
        private bool settings = true;
        public static bool completeAnimatorLogic = true;
        public static bool ignorePreviousStates = true;
        public static bool autoSelectFolderWhenRun = false;
        public static bool enableUnityDebugLogs = true;
        public static bool disableMenuCreation = false;
        public static bool writeDefaults = true;
        
        public static string Version = "v1.2.0";

        public enum CreationType
        {
            Material = 0,
            GameObject = 1,
            Blendshape = 2
        }
        public void OnGUI()
        {
            //Sets up a gameobject slot within the editor window.
            avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", avatar, typeof(GameObject), true);
            //Label field to display debug results at the top of this editor window, easily viewable by the user. Debug text is changed over execution
            EditorGUILayout.LabelField(debug, new GUIStyle()
            {
                richText = true,
                fontSize = 12,
                wordWrap = true,
                fontStyle = FontStyle.Normal
            });          
            EditorGUILayout.BeginVertical();

            //Creates the foldout which holds settings
            settings = EditorGUILayout.Foldout(settings, "Settings", true);
            if (settings)
            {
                EditorGUILayout.BeginVertical("box");
                GUILayout.Space(8);

                completeAnimatorLogic = GUILayout.Toggle(completeAnimatorLogic, "Complete Animator Logic");
                writeDefaults = GUILayout.Toggle(writeDefaults, "Write Defaults");
                ignorePreviousStates = GUILayout.Toggle(ignorePreviousStates, "Ignore Previously Created States");
                disableMenuCreation = GUILayout.Toggle(disableMenuCreation, "Disable Menu Generation / Modification");
                enableUnityDebugLogs = GUILayout.Toggle(enableUnityDebugLogs, "Enable Unity Debug Console Logs");
                autoSelectFolderWhenRun = GUILayout.Toggle(autoSelectFolderWhenRun, "Select Avatar Folder When Run");
              
                if (GUILayout.Button("Reset", GUILayout.Width(75)))
                {
                    ReInitializeUI();
                    completeAnimatorLogic = true;
                    writeDefaults = true;
                    ignorePreviousStates = true;
                    autoSelectFolderWhenRun = false;
                    enableUnityDebugLogs = true;
                    disableMenuCreation = false;
                    avatar = null;
                    controller = null;
                }
                
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Run"))
            {

                if (avatar != null && objCategories.Count + matCategories.Count + blendCategories.Count > 0)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    AnimUtil.MakeAnimationClips(ref matCategories, ref objCategories, ref blendCategories);

                    if (controller != null && completeAnimatorLogic) {
                        Algorithm.SetupMaterialToggles(ref matCategories);
                        Algorithm.SetupGameObjectToggles(ref objCategories);
                        Algorithm.SetupBlendshapeToggles(ref blendCategories);
                    }

                    if (!disableMenuCreation && completeAnimatorLogic)
                        Algorithm.CreateMenus(ref matCategories, ref objCategories, ref blendCategories);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    watch.Stop();
                    Algorithm.elaspedTime = watch.Elapsed.TotalSeconds;
                    Helper.DisplayCreationResults();                       
                    ReInitializeUI();
                    EditorSceneManager.SaveOpenScenes();
                    
                    if (autoSelectFolderWhenRun)
                        Helper.SelectAssetAtPath<UnityEngine.Object>($"Assets/Nin/EZAvatar/{avatar.name}");                   
                }
                else if (objCategories.Count + matCategories.Count + blendCategories.Count == 0)
                {
                    Debug.LogWarning("<color=yellow>[EZAvatar]</color>: Must create categories in order to run.");
                    debug = Helper.SetTextColor("Must create categories in order to run.", "yellow");
                }
                else
                {
                    Debug.LogWarning("<color=yellow>[EZAvatar]</color>: Missing avatar object.");
                    debug = Helper.SetTextColor("Missing avatar object.", "yellow");
                }
            }

            if (GUILayout.Button("Check for Updates"))
            {
                Updater.CheckForUpdates(false);
            }

            //Creates the foldout which holds material categories
            categoriesScrollView = EditorGUILayout.BeginScrollView(categoriesScrollView);
            MaterialFoldout = EditorGUILayout.Foldout(MaterialFoldout, "Material", true);
            if (MaterialFoldout)
            {
                DrawMaterialUI();
            }                      
            GameObjFoldout = EditorGUILayout.Foldout(GameObjFoldout, "GameObject", true);
            if (GameObjFoldout)
            {
                DrawGameObjUI();
            }
            blendshapeFoldout = EditorGUILayout.Foldout(blendshapeFoldout, "Blendshapes", true);
            if (blendshapeFoldout) 
            { 
                DrawBlendshapeUI();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawMaterialUI()
        {           
            EditorGUILayout.BeginHorizontal();
            
            matEnterText = EditorGUILayout.TextField(matEnterText);

            if (GUILayout.Button("Create category")) {
                Helper.CreateCategoryButton(ref matCategories, ref matEnterText);
            }

            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < matCategories.Count(); i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical("box");

                var name = matCategories[i].name;
                EditorGUILayout.BeginVertical();
                
                EditorGUILayout.BeginHorizontal();
                matCategories[i].foldout = EditorGUILayout.Foldout(matCategories[i].foldout, name, true);
                if (matCategories[i].layerExists) {
                    EditorGUILayout.LabelField("<color=008080ff> * </color>", new GUIStyle()
                    {
                        richText = true,
                        fontSize = 16,
                        alignment = TextAnchor.MiddleRight,
                        fontStyle = FontStyle.Bold
                    });
                }
                EditorGUILayout.EndHorizontal();

                //Logic for what will be under each category foldout, in this case it will be material object fields.
                if (matCategories[i].foldout)
                {                 
                    if (Helper.DoesCategoryExistAndHaveStates(controller, matCategories[i].name) && !matCategories[i].layerExists)
                        matCategories[i].layerExists = true;
                    
                    if (matCategories[i].layerExists)
                    {
                        EditorGUILayout.LabelField("<color=008080ff> Adding to existing layer </color>", new GUIStyle()
                        {
                            fontStyle = FontStyle.BoldAndItalic,
                            richText = true,
                        });
                    }
                                     
                    EditorGUILayout.BeginVertical();

                    if (!disableMenuCreation)
                    {
                        EditorGUILayout.BeginHorizontal();
                        matCategories[i].menuGenerationType = (MenuGenOption)EditorGUILayout.EnumPopup("Menu Generation", matCategories[i].menuGenerationType);
                        if (matCategories[i].menuGenerationType == MenuGenOption.AddToSelectedMenu)
                            matCategories[i].menu = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)EditorGUILayout.ObjectField(matCategories[i].menu, typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu), true);
                        EditorGUILayout.EndHorizontal();

                        if (matCategories[i].layerExists || matCategories[i].slots > 2)
                            matCategories[i].menuControl = (ControlType)EditorGUILayout.EnumPopup("Menu Control", matCategories[i].menuControl);
                        else if (matCategories[i].menuControl == ControlType.RadialPuppet)                       
                            matCategories[i].menuControl = ControlType.Toggle;
                    }

                    EditorGUILayout.Space(2);

                    matCategories[i].objects[0] = (GameObject)EditorGUILayout.ObjectField("Mesh Object", matCategories[i].objects[0], typeof(GameObject), true);

                    //Creates new object fields based on the value of matCount, which increments with the Add button seen below.
                    for (int y = 0; y < matCategories[i].slots; y++)
                    {
                        Array.Resize(ref matCategories[i].materials, matCategories[i].slots);
                        if (y == 0)
                            matCategories[i].materials[y] = (Material)EditorGUILayout.ObjectField("Default", matCategories[i].materials[y], typeof(Material), false);
                        else
                            matCategories[i].materials[y] = (Material)EditorGUILayout.ObjectField($"Mat {y}", matCategories[i].materials[y], typeof(Material), false);
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    //Adds a button that increments an int, which is used to create new material fields
                    if (GUILayout.Button("+", GUILayout.Width(35)))
                        matCategories[i].slots += 1;

                    if (matCategories[i].slots > 0) {
                        if (GUILayout.Button("-", GUILayout.Width(35)))
                            matCategories[i].slots -= 1;
                    }

                    if (GUILayout.Button("Del", GUILayout.Width(50)))
                        matCategories.Remove(matCategories[i]);

                    EditorGUILayout.EndHorizontal();                  
                }
                EditorGUILayout.EndVertical();

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }           
        }

        void DrawGameObjUI()
        {              
            EditorGUILayout.BeginHorizontal();
            
            objEnterText = EditorGUILayout.TextField(objEnterText);

            if (GUILayout.Button("Create category")) {
                Helper.CreateCategoryButton(ref objCategories, ref objEnterText);
            }

            EditorGUILayout.EndHorizontal();

            //Creates a foldout for each category made, which also holds an add button that will add a field

            for (int i = 0; i < objCategories.Count(); i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical("box");

                var name = objCategories[i].name;
                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                objCategories[i].foldout = EditorGUILayout.Foldout(objCategories[i].foldout, name, true);
                if (objCategories[i].layerExists) {
                    EditorGUILayout.LabelField("<color=008080ff> * </color>", new GUIStyle() {
                        richText = true,
                        fontSize = 16,
                        alignment = TextAnchor.MiddleRight,
                        fontStyle = FontStyle.Bold
                    });
                }
                EditorGUILayout.EndHorizontal();

                //Logic for what will be under each category foldout, in this case it will be material object fields.
                if (objCategories[i].foldout)
                {
                    if (Helper.DoesCategoryExistAndHaveStates(controller, $"Toggle {objCategories[i].name}") && !objCategories[i].layerExists)
                    {
                        objCategories[i].layerExists = true;
                        objCategories[i].makeIdle = true;
                    }

                    if (objCategories[i].layerExists) {
                        EditorGUILayout.LabelField("<color=008080ff> Adding to existing layer </color>", new GUIStyle() {
                            fontStyle = FontStyle.BoldAndItalic,
                            richText = true,
                        });
                    }

                    EditorGUILayout.BeginVertical();

                    if (!objCategories[i].layerExists && objCategories[i].slots > 1)
                        objCategories[i].makeIdle = GUILayout.Toggle(objCategories[i].makeIdle, "Toggle Objects Separately");
                    else if (!objCategories[i].layerExists && objCategories[i].slots <= 1)
                        objCategories[i].makeIdle = false;

                    if (!disableMenuCreation)
                    {
                        EditorGUILayout.BeginHorizontal();                     
                        objCategories[i].menuGenerationType = (MenuGenOption)EditorGUILayout.EnumPopup("Menu Generation", objCategories[i].menuGenerationType);
                        if (objCategories[i].menuGenerationType == MenuGenOption.AddToSelectedMenu)
                            objCategories[i].menu = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)EditorGUILayout.ObjectField(objCategories[i].menu, typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu), true);                      
                        EditorGUILayout.EndHorizontal();
                        
                        if (objCategories[i].makeIdle || objCategories[i].layerExists)
                            objCategories[i].menuControl = (ControlType)EditorGUILayout.EnumPopup("Menu Control", objCategories[i].menuControl);
                        else if (objCategories[i].menuControl == ControlType.RadialPuppet)
                            objCategories[i].menuControl = ControlType.Toggle;
                    }

                    EditorGUILayout.Space(2);
                    for (int j = 0; j < objCategories[i].slots; j++)
                    {
                        Array.Resize(ref objCategories[i].objects, objCategories[i].slots);
                        objCategories[i].objects[j] = (GameObject)EditorGUILayout.ObjectField($"Object {j}", objCategories[i].objects[j], typeof(GameObject), true);
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    //Adds a button that increments an int, which is used to create new material fields
                    if (GUILayout.Button("+", GUILayout.Width(35)))
                        objCategories[i].slots += 1;

                    if (objCategories[i].slots > 0) {
                        if (GUILayout.Button("-", GUILayout.Width(35)))
                            objCategories[i].slots -= 1;
                    }

                    if (GUILayout.Button("Del", GUILayout.Width(50)))
                        objCategories.Remove(objCategories[i]);

                    EditorGUILayout.EndHorizontal();
                }                
                EditorGUILayout.EndVertical();

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        void DrawBlendshapeUI()
        {      
            EditorGUILayout.BeginHorizontal();

            blendEnterText = EditorGUILayout.TextField(blendEnterText);

            if (GUILayout.Button("Create category"))
            {
                Helper.CreateCategoryButton(ref blendCategories, ref blendEnterText);
            }

            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < blendCategories.Count(); i++)
            {           
                if (blendCategories[i].blendShapeData == null)
                    blendCategories[i].blendShapeData = new BlendshapeValueData();
            
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.BeginVertical("box");

                var name = blendCategories[i].name;
                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                blendCategories[i].foldout = EditorGUILayout.Foldout(blendCategories[i].foldout, name, true);
                EditorGUILayout.EndHorizontal();

                if (blendCategories[i].foldout)
                {                   
                    EditorGUILayout.BeginVertical();

                    if (!disableMenuCreation)
                    {
                        EditorGUILayout.BeginHorizontal();
                        blendCategories[i].menuGenerationType = (MenuGenOption)EditorGUILayout.EnumPopup("Menu Generation", blendCategories[i].menuGenerationType);
                        if (blendCategories[i].menuGenerationType == MenuGenOption.AddToSelectedMenu)
                            blendCategories[i].menu = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)EditorGUILayout.ObjectField(blendCategories[i].menu, typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu), true);
                        EditorGUILayout.EndHorizontal();

                        blendCategories[i].menuControl = (ControlType)EditorGUILayout.EnumPopup("Menu Control", blendCategories[i].menuControl);
                    }

                    EditorGUILayout.Space(2);

                    for (int y = 0; y < blendCategories[i].slots; y++)
                    {
                        Array.Resize(ref blendCategories[i].objects, blendCategories[i].slots);
                        Array.Resize(ref blendBools, blendCategories[i].slots);
                        var defaultName = $"Object {y}";
                        blendBools[y] = EditorGUILayout.Foldout(blendBools[y], blendCategories[i].objects[y] != null ? blendCategories[i].objects[y].name : defaultName, true);
                        if (blendBools[y])
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(18);
                            EditorGUILayout.BeginVertical();
 
                            EditorGUI.BeginChangeCheck();
                            blendCategories[i].objects[y] = (GameObject)EditorGUILayout.ObjectField($"Mesh Object", blendCategories[i].objects[y], typeof(GameObject), true);
                            if (EditorGUI.EndChangeCheck())
                            {                                  
                                for (int b = 0; b < blendCategories[i].blendShapeData.GUIData[y].selected.Length; b++)
                                {
                                    blendCategories[i].blendShapeData.GUIData[y].selected[b] = false;
                                    if (blendCategories[i].blendShapeData.values.Exists(v => v.id == b)) {
                                        blendCategories[i].blendShapeData.values.Remove(blendCategories[i].blendShapeData.values.Where(v => v.id == b).ToList()[0]);
                                    } 
                                }
                            }
                                                                         
                            GUILayout.Space(5);

                            if (blendCategories[i].objects[y] != null)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.LabelField("Start Value", GUILayout.MaxWidth(65));
                                EditorGUILayout.LabelField("End Value", GUILayout.MaxWidth(65));
                                EditorGUILayout.EndHorizontal();
                            }

                            for (int x = 0; x < blendCategories[i].objects[y]?.GetComponent<SkinnedMeshRenderer>()?.sharedMesh.blendShapeCount; x++)
                            {
                                if (x == 0)
                                {
                                    Array.Resize(ref blendCategories[i].blendShapeData.GUIData[y].selected, Helper.FetchTotalBlendshapes(blendCategories[i]));
                                    Array.Resize(ref blendCategories[i].blendShapeData.GUIData[y].selectedMin, blendCategories[i].blendShapeData.GUIData[y].selected.Length);
                                    Array.Resize(ref blendCategories[i].blendShapeData.GUIData[y].selectedMax, blendCategories[i].blendShapeData.GUIData[y].selected.Length);
                                }
                                EditorGUILayout.BeginHorizontal("box");
                                
                                blendCategories[i].blendShapeData.GUIData[y].selected[x] = GUILayout.Toggle(blendCategories[i].blendShapeData.GUIData[y].selected[x], "Select");
                                string blendshapeName = blendCategories[i].objects[y].GetComponent<SkinnedMeshRenderer>().sharedMesh.GetBlendShapeName(x);
                                
                                EditorGUILayout.LabelField((blendCategories[i].blendShapeData.GUIData[y].selected[x] == true) ? $"<color=white>{blendshapeName}</color>" : blendshapeName, new GUIStyle()
                                {
                                    richText = true,
                                    fontSize = 16,
                                    fontStyle = FontStyle.Normal
                                });  
                            
                                blendCategories[i].blendShapeData.GUIData[y].selectedMin[x] = EditorGUILayout.IntField(blendCategories[i].blendShapeData.GUIData[y].selectedMin[x], GUILayout.MaxWidth(65));
                                blendCategories[i].blendShapeData.GUIData[y].selectedMax[x] = EditorGUILayout.IntField(blendCategories[i].blendShapeData.GUIData[y].selectedMax[x], GUILayout.MaxWidth(65));
                                                       
                                if (blendCategories[i].blendShapeData.GUIData[y].selected[x])
                                {                                                                      
                                    if (!blendCategories[i].blendShapeData.values.Exists(b => b.name == blendshapeName)) {
                                        blendCategories[i].blendShapeData.values.Add(new BlendshapeValuePair() { name = blendshapeName, id = x, guidataid = y});
                                        //Debug.Log($"Added {blendshapeName} to list of selected blendshapes! There is now {blendCategories[i].blendShapeData.values.Count} selected!");
                                    }  

                                }
                                
                                else if (!blendCategories[i].blendShapeData.GUIData[y].selected[x])
                                {                                  
                                    if (blendCategories[i].blendShapeData.values.Exists(b => b.name == blendshapeName)) {
                                        blendCategories[i].blendShapeData.values.Remove(blendCategories[i].blendShapeData.values.Where(b => b.name == blendshapeName).ToList()[0]);
                                        //Debug.Log($"Removed {blendshapeName} to list of selected blendshapes! There is now {blendCategories[i].blendShapeData.values.Count} selected!");
                                    } 
                                }
                                
                                EditorGUILayout.EndHorizontal();
                            }

                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.EndVertical();
                        }
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("+", GUILayout.Width(35)))
                    {
                        blendCategories[i].slots += 1;
                        blendCategories[i].blendShapeData.GUIData.Add(new BlendshapeGUIData());
                    }                     

                    if (blendCategories[i].slots > 0)
                    {
                        if (GUILayout.Button("-", GUILayout.Width(35)))
                        {
                            blendCategories[i].slots -= 1;
                            blendCategories[i].blendShapeData.GUIData.Remove(blendCategories[i].blendShapeData.GUIData.Last());
                        }
                    }

                    if (GUILayout.Button("Del", GUILayout.Width(50)))
                        blendCategories.Remove(blendCategories[i]);

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
    }
    
    public static void ReInitializeUI()
    {
        matEnterText = "";
        objEnterText = "";
        blendEnterText = "";
        matCategories.Clear();
        objCategories.Clear();
        blendCategories.Clear();
    }   
}

#endif