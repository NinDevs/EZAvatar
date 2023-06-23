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
        private static Vector2 categoriesScrollView;
        public static List<Category> objCategories = new List<Category>();
        public static List<Category> matCategories = new List<Category>();
        private static string matEnterText;
        private static string objEnterText;
        private int count;
        public static string debug;
        private bool settings;
        public static bool completeAnimatorLogic = true;
        public static bool ignorePreviousStates = true;       
        public static bool autoCreateMenus = true;
        public static bool autoSelectFolderWhenRun = true;
        public static bool enableUnityDebugLogs = true;
        
        public static string Version = "v1.1.2";

        public enum CreationType
        {
            Material,
            GameObject
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
                ignorePreviousStates = GUILayout.Toggle(ignorePreviousStates, "Ignore Previously Created States");
                autoCreateMenus = GUILayout.Toggle(autoCreateMenus, "Automatically Create Menus");
                autoSelectFolderWhenRun = GUILayout.Toggle(autoSelectFolderWhenRun, "Select Avatar Folder When Run");
                enableUnityDebugLogs = GUILayout.Toggle(enableUnityDebugLogs, "Enable Unity Debug Console Logs");

                if (GUILayout.Button("Reset", GUILayout.Width(75)))
                {
                    ReInitializeUI();
                    completeAnimatorLogic = true;
                    ignorePreviousStates = true;
                    autoCreateMenus = true;
                    autoSelectFolderWhenRun = true;
                    enableUnityDebugLogs = true;
                    avatar = null;
                    controller = null;
                }
                
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Run"))
            {

                if (avatar != null && objCategories.Count() + matCategories.Count() > 0)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    AnimUtil.MakeAnimationClips(ref matCategories, ref objCategories);
                    if (completeAnimatorLogic) 
                    {
                        if (controller != null) {
                            Algorithm.SetupMaterialToggles(ref matCategories);
                            Algorithm.SetupGameObjectToggles(ref objCategories);                         
                        }
                    }
                    if (autoCreateMenus)
                        Algorithm.CreateMenus(ref matCategories, ref objCategories);

                    Algorithm.elaspedTime = watch.Elapsed.TotalSeconds;
                    Helper.DisplayCreationResults();                  
                    ReInitializeUI();
                    EditorSceneManager.SaveOpenScenes();
                    if (autoSelectFolderWhenRun)
                        Helper.SelectAssetAtPath<UnityEngine.Object>($"Assets/Nin/EZAvatar/{avatar.name}");                   
                }
                else if (objCategories.Count() + matCategories.Count() == 0)
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
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawMaterialUI()
        {           
            EditorGUILayout.BeginHorizontal("box");
            
            matEnterText = EditorGUILayout.TextField(matEnterText);

            if (GUILayout.Button("Create category"))
            {
                if (count == 0)
                    Helper.AddCategory(matCategories, matEnterText);

                //Prevents categories with the same names being made
                if (count > 0)
                {
                    var exists = Helper.DoesCategoryExist(matCategories, matEnterText);
                    if (!exists)
                        Helper.AddCategory(matCategories, matEnterText);
                    else
                    {
                        Debug.Log("<color=yellow>[EZAvatar]</color>: Category already exists! Try a different name.");
                        debug = Helper.SetTextColor("Category already exists! Try a different name.", "yellow");
                    }
                }
                matEnterText = "";
                count++;
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
            EditorGUILayout.BeginHorizontal("box");
            
            objEnterText = EditorGUILayout.TextField(objEnterText);

            if (GUILayout.Button("Create category"))
            {
                if (count == 0)
                    Helper.AddCategory(objCategories, objEnterText);

                //Prevents categories with the same names being made
                if (count > 0)
                {
                    var exists = Helper.DoesCategoryExist(objCategories, objEnterText);
                    if (!exists)
                        Helper.AddCategory(objCategories, objEnterText);
                    else
                    {
                        Debug.Log("<color=yellow>[EZAvatar]</color>: Category already exists! Try a different name.");
                        debug = Helper.SetTextColor("Category already exists! Try a different name.", "yellow");
                    }
                }
                objEnterText = "";
                count++;
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
        
                    EditorGUILayout.Space(1);
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
    
        public static void ReInitializeUI()
        {
            matEnterText = "";
            objEnterText = "";
            matCategories.Clear();
            objCategories.Clear();           
        }   
    }


#endif