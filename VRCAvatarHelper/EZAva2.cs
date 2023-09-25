#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
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
        public int slots { get; set; }
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
                    UI.ReInitialize();               
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
        public static string debug;
        private bool settings = true;
        public static bool completeAnimatorLogic = true;
        public static bool ignorePreviousStates = true;
        public static bool autoSelectFolderWhenRun = false;
        public static bool enableUnityDebugLogs = true;
        public static bool disableMenuCreation = false;
        public static bool writeDefaults = true;
        
        public static string Version = "v1.2.3";

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
                    UI.ReInitialize();
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
                    UI.ReInitialize();
                    EditorSceneManager.SaveOpenScenes();
                    
                    if (autoSelectFolderWhenRun)
                        Helper.SelectAssetAtPath<Object>($"Assets/Nin/EZAvatar/{avatar.name}");                   
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
                UI.DrawMaterialUI();
            }                      
            GameObjFoldout = EditorGUILayout.Foldout(GameObjFoldout, "GameObject", true);
            if (GameObjFoldout)
            {
                UI.DrawGameObjUI();
            }
            blendshapeFoldout = EditorGUILayout.Foldout(blendshapeFoldout, "Blendshapes", true);
            if (blendshapeFoldout) 
            { 
                UI.DrawBlendshapeUI();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
          
    }

#endif