#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Animations;

namespace EZAvatar
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
    }

    public class EzAvatar : EditorWindow
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
            EzAvatar window = (EzAvatar)GetWindow(typeof(EzAvatar));
            window.Show();
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
                    Helper.HasFXLayer(0);
                    ReInitializeUI();               
                }

            }
        }

        public static AnimatorController controller = null;
        private bool MaterialFoldout;
        private bool GameObjFoldout;
        private static Vector2 matScrollView;
        private static Vector2 objScrollView;
        public static List<Category> objCategories = new List<Category>();
        public static List<Category> matCategories = new List<Category>();
        private static string matEnterText;
        private static string objEnterText;
        private int count;
        public static string debug;
        private bool settings;
        public static bool completeAnimatorLogic = true;
        public static bool createAnimationClips = true;
        public static bool ignorePreviousStates = true;       
        public static bool autoCreateMenus = true;

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
            EditorGUILayout.LabelField(debug);             
            EditorGUILayout.BeginVertical();

            //Creates the foldout which holds settings
            settings = EditorGUILayout.Foldout(settings, "Settings", true);
            if (settings)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.Space(8);

                completeAnimatorLogic = GUILayout.Toggle(completeAnimatorLogic, "Complete Animator Logic");
                createAnimationClips = GUILayout.Toggle(createAnimationClips, "Create Animation Clips");
                ignorePreviousStates = GUILayout.Toggle(ignorePreviousStates, "Ignore Previously Created States");
                autoCreateMenus = GUILayout.Toggle(autoCreateMenus, "Automatically Create Menus");

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Run"))
            {

                if (avatar != null && objCategories.Count() + matCategories.Count() > 0)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    if (createAnimationClips)
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

                    watch.Stop();
                    Algorithm.elaspedTime = watch.Elapsed.TotalSeconds;
                    Helper.DisplayCreationResults();
                   
                    ReInitializeUI();
                }
                else if (objCategories.Count() + matCategories.Count() == 0)
                {
                    debug = "Must create categories in order to run.";
                    Debug.LogWarning(debug);
                }
                else
                {
                    debug = "Missing avatar object.";
                    Debug.LogWarning(debug);
                }
            }

            //Creates the foldout which holds material categories
            MaterialFoldout = EditorGUILayout.Foldout(MaterialFoldout, "Material", true);
            if (MaterialFoldout)
            {
                matScrollView = EditorGUILayout.BeginScrollView(matScrollView);
                DrawMaterialUI();
                EditorGUILayout.EndScrollView();
            }                      
            GameObjFoldout = EditorGUILayout.Foldout(GameObjFoldout, "GameObject", true);
            if (GameObjFoldout)
            {
                objScrollView = EditorGUILayout.BeginScrollView(objScrollView);
                DrawGameObjUI();
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
        }

        void DrawMaterialUI()
        {           
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
                        Debug.Log("Category already exists! Try a different name.");
                        debug = "Category already exists! Try a different name.";
                    }
                }
                matEnterText = "";
                count++;
            }
           
            //Odd implementation, but you can't just remove items in a foreach loop while it is running - this is for the functionality of deleting categories
            //We go to this region when the delete button is pressed, which will essentially delete the category we want and restart the loop, redisplaying everything else

            for (int i = 0; i < matCategories.Count(); i++)
            {
                var name = matCategories[i].name;
                EditorGUILayout.BeginVertical();
                matCategories[i].foldout = EditorGUILayout.Foldout(matCategories[i].foldout, name, true);

                //Logic for what will be under each category foldout, in this case it will be material object fields.
                if (matCategories[i].foldout)
                {
                    matCategories[i].objects[0] = (GameObject)EditorGUILayout.ObjectField("Mesh Object", matCategories[i].objects[0], typeof(GameObject), true);

                    //Creates new object fields based on the value of matCount, which increments with the Add button seen below.
                    for (int y = 0; y < matCategories[i].slots; y++)
                    {
                        Array.Resize(ref matCategories[i].materials, matCategories[i].slots);
                        EditorGUILayout.BeginVertical();
                        if (y == 0)
                            matCategories[i].materials[y] = (Material)EditorGUILayout.ObjectField("Default", matCategories[i].materials[y], typeof(Material), false);
                        else
                            matCategories[i].materials[y] = (Material)EditorGUILayout.ObjectField($"Mat {y}", matCategories[i].materials[y], typeof(Material), false);
                        EditorGUILayout.EndVertical();
                    }

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
            }           
        }

        void DrawGameObjUI()
        {           
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
                        Debug.Log("Category already exists! Try a different name.");
                        debug = "Category already exists! Try a different name.";
                    }
                }
                objEnterText = "";
                count++;
            }

            //Creates a foldout for each category made, which also holds an add button that will add a field

            for (int i = 0; i < objCategories.Count(); i++)
            {
                var name = objCategories[i].name;
                EditorGUILayout.BeginVertical();
                objCategories[i].foldout = EditorGUILayout.Foldout(objCategories[i].foldout, name, true);

                //Logic for what will be under each category foldout, in this case it will be material object fields.
                if (objCategories[i].foldout)
                {
                    for (int j = 0; j < objCategories[i].slots; j++)
                    {
                        Array.Resize(ref objCategories[i].objects, objCategories[i].slots);
                        EditorGUILayout.BeginVertical();
                        objCategories[i].objects[j] = (GameObject)EditorGUILayout.ObjectField($"Object {j}", objCategories[i].objects[j], typeof(GameObject), true);
                        EditorGUILayout.EndVertical();
                    }

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
}

#endif 
