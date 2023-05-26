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
    public class Category
    {
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
        private List<string> categoryFields = new List<string>();
        private List<bool> categoryFoldouts = new List<bool>();
        public static List<Category> categories = new List<Category>();
        private string enterText;
        private int count;
        public static string debug;
        private bool settings;
        public static bool completeAnimatorLogic = true;
        public static bool createAnimationClips = true;
        public static bool ignorePreviousStates = true;
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

                EditorGUILayout.EndHorizontal();
            }

            //Creates the foldout which holds material categories
            MaterialFoldout = EditorGUILayout.Foldout(MaterialFoldout, "Material", true);
            if (MaterialFoldout)
            {
                enterText = EditorGUILayout.TextField(enterText);

                if (GUILayout.Button("Create category"))
                {
                    if (count == 0)
                    {
                        categoryFoldouts.Add(true);
                        categoryFields.Add(enterText);
                        Helper.AddCategory(categories, enterText, categoryFoldouts.Last());
                    }

                    //Prevents categories with the same names being made
                    if (count > 0)
                    {
                        var exists = Helper.DoesCategoryExist(categories, enterText);
                        if (!exists)
                        {
                            categoryFoldouts.Add(true);
                            categoryFields.Add(enterText);
                            Helper.AddCategory(categories, enterText, categoryFoldouts.Last());
                        }
                        else
                        {
                            Debug.Log("Category already exists! Try a different name.");
                            debug = "Category already exists! Try a different name.";
                        }
                    }
                    enterText = "";
                    count++;
                }

                if (GUILayout.Button("Run"))
                {
                    if (createAnimationClips)
                        AnimUtil.MakeAnimationClips(categories, CreationType.Material);
                    if (completeAnimatorLogic)
                        Algorithm.SetupMaterialToggles();
                    InitializeUI();
                }
                DrawMaterialUI();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawMaterialUI()
        {
            //Creates a foldout for each category made, which also holds an add button that will add a field
            for (int i = 0; i < categories.Count; i++)
            {
                var name = categoryFields[i];
                EditorGUILayout.BeginVertical();
                categoryFoldouts[i] = EditorGUILayout.Foldout(categoryFoldouts[i], name, true);

                //Logic for what will be under each category foldout, in this case it will be material object fields.
                if (categoryFoldouts[i])
                {
                    int hash = categoryFoldouts[i].GetHashCode();
                    categories.ElementAt(i).objects[0] = (GameObject)EditorGUILayout.ObjectField("Mesh Object", categories.ElementAt(i).objects[0], typeof(GameObject), true);

                    if (hash == categories.ElementAt(i).foldout.GetHashCode())
                    {
                        //Creates new object fields based on the value of matCount, which increments with the Add button seen below.
                        for (int j = 0; j < categories.ElementAt(i).slots; j++)
                        {
                            Array.Resize(ref categories.ElementAt(i).materials, categories.ElementAt(i).slots);
                            EditorGUILayout.BeginVertical();
                            categories.ElementAt(i).materials[j] = (Material)EditorGUILayout.ObjectField($"Mat {j}", categories.ElementAt(i).materials[j], typeof(Material), false);
                            EditorGUILayout.EndVertical();
                        }

                    }

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    //Adds a button that increments an int, which is used to create new material fields
                    if (GUILayout.Button("+", GUILayout.Width(35)))
                    {
                        categories.ElementAt(i).slots += 1;
                    }

                    if (categories.ElementAt(i).slots > 0)
                    {
                        if (GUILayout.Button("-", GUILayout.Width(35)))
                        {
                            categories.ElementAt(i).slots -= 1;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
        }

        void DrawGameObjUI()
        {

        }

        public void InitializeUI()
        {
            enterText = "";
            categories.Clear();
            categoryFields.Clear();
            categoryFoldouts.Clear();
        }

    }
}


