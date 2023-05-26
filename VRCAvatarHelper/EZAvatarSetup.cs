using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;


namespace EZAvatar
{
    public enum CategoryType
    {
        Material,
        GameObject
    }
    public class Category
    {
        public CategoryType type;
        public string name;
        public bool foldout;
        public int slots { get; internal set; }
        public Material[] materials;
        public GameObject[] objects = new GameObject[1];
        public List<AnimationClip> animClips = new List<AnimationClip>();
        //public List<Category> subcategories = new List<Category>();
    }

    public class EzAvatar : EditorWindow
    {
        [MenuItem("Nin/Utilities/EzAvatar")]

        static void Init()
        {
            //Creating a new editor window, and then shows it
            EzAvatar window = (EzAvatar)EditorWindow.GetWindow(typeof(EzAvatar));
            window.Show();

        }
        public static GameObject avatar;
        private bool MaterialFoldout;
        private bool GameObjFoldout;
        private static Vector2 scrollview;
        public static List<Category> categories = new List<Category>();
        private string matEnterText;
        private string objEnterText;
        private int count;
        public static string debug;
        private bool settings;
        public static bool completeAnimatorLogic = true;
        public static bool createAnimationClips = true;
        public static bool ignorePreviousStates = true;
        public static bool multiToggleGameObj = false;
        private int subcount;

        public enum CreationType
        {
            Material,
            GameObject
        }

        public void OnGUI()
        {
            //Sets up a gameobject slot within the editor window.
            avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", avatar, typeof(GameObject), true);
            EditorGUILayout.LabelField(debug);
            EditorGUILayout.BeginScrollView(scrollview);
            EditorGUILayout.BeginVertical();

            //Creates the foldout which holds settings
            settings = EditorGUILayout.Foldout(settings, "Settings", true);
            if (settings)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8);

                completeAnimatorLogic = GUILayout.Toggle(completeAnimatorLogic, "Complete Animator Logic");
                createAnimationClips = GUILayout.Toggle(createAnimationClips, "Create Animation Clips");
                ignorePreviousStates = GUILayout.Toggle(ignorePreviousStates, "Ignore Previously Created States");
                //multiToggleGameObj = GUILayout.Toggle(multiToggleGameObj, "Create Multi-Toggle GameObject Toggles");

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Run"))
            {
                if (createAnimationClips)
                    AnimUtil.MakeAnimationClips(categories);
                if (completeAnimatorLogic)
                {
                    if (Helper.HasFXLayer() == true)
                    {
                        Algorithm.SetupMaterialToggles();
                        Algorithm.SetupGameObjectToggles();
                        Helper.DisplayCreationResults();
                    }

                }
                InitializeUI();
            }

            //Creates the foldout which holds material categories
            MaterialFoldout = EditorGUILayout.Foldout(MaterialFoldout, "Material", true);
            if (MaterialFoldout)
                DrawMaterialUI();
            GUILayout.Space(4);
            GameObjFoldout = EditorGUILayout.Foldout(GameObjFoldout, "GameObject", true);
            if (GameObjFoldout)
                DrawGameObjUI();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawMaterialUI()
        {
            matEnterText = EditorGUILayout.TextField(matEnterText);

            if (GUILayout.Button("Create category"))
            {
                if (count == 0)
                {
                    Helper.AddCategory(categories, matEnterText, true);
                    categories.Last().type = CategoryType.Material;
                }

                //Prevents categories with the same names being made
                if (count > 0)
                {
                    var exists = Helper.DoesCategoryExist(categories, matEnterText);
                    if (!exists)
                        Helper.AddCategory(categories, matEnterText, true);
                    else
                    {
                        Debug.Log("Category already exists! Try a different name.");
                        debug = "Category already exists! Try a different name.";
                    }
                }
                matEnterText = "";
                count++;
            }

            //Creates a foldout for each category made, which also holds an add button that will add a field
            foreach (var category in categories.Where(x => x.type == CategoryType.Material))
            {
                var name = category.name;
                EditorGUILayout.BeginVertical();
                category.foldout = EditorGUILayout.Foldout(category.foldout, name, true);

                //Logic for what will be under each category foldout, in this case it will be material object fields.
                if (category.foldout)
                {
                    category.objects[0] = (GameObject)EditorGUILayout.ObjectField("Mesh Object", category.objects[0], typeof(GameObject), true);

                    //Creates new object fields based on the value of matCount, which increments with the Add button seen below.
                    for (int i = 0; i < category.slots; i++)
                    {
                        Array.Resize(ref category.materials, category.slots);
                        EditorGUILayout.BeginVertical();
                        category.materials[i] = (Material)EditorGUILayout.ObjectField($"Mat {i}", category.materials[i], typeof(Material), false);
                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    //Adds a button that increments an int, which is used to create new material fields
                    if (GUILayout.Button("+", GUILayout.Width(35)))
                    {
                        category.slots += 1;
                    }

                    if (category.slots > 0)
                    {
                        if (GUILayout.Button("-", GUILayout.Width(35)))
                        {
                            category.slots -= 1;
                        }
                    }

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
                {
                    Helper.AddCategory(categories, objEnterText, true);
                    categories.Last().type = CategoryType.GameObject;
                }

                //Prevents categories with the same names being made
                if (count > 0)
                {
                    var exists = Helper.DoesCategoryExist(categories, objEnterText);
                    if (!exists)
                        Helper.AddCategory(categories, objEnterText, true);
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
            foreach (var category in categories.Where(x => x.type == CategoryType.GameObject))
            {
                var name = category.name;
                EditorGUILayout.BeginVertical();
                category.foldout = EditorGUILayout.Foldout(category.foldout, name, true);

                //Logic for what will be under each category foldout, in this case it will be material object fields.
                if (category.foldout)
                {
                    for (int j = 0; j < category.slots; j++)
                    {
                        Array.Resize(ref category.objects, category.slots);
                        EditorGUILayout.BeginVertical();
                        category.objects[j] = (GameObject)EditorGUILayout.ObjectField($"Object {j}", category.objects[j], typeof(GameObject), true);
                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    //Adds a button that increments an int, which is used to create new material fields
                    if (GUILayout.Button("+", GUILayout.Width(35)))
                    {
                        category.slots += 1;
                    }

                    if (category.slots > 0)
                    {
                        if (GUILayout.Button("-", GUILayout.Width(35)))
                        {
                            category.slots -= 1;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
        }

        public void InitializeUI()
        {
            matEnterText = "";
            categories.Clear();
        }

    }
}


