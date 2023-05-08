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
        public GameObject[] objectRef;
        public List<AnimationClip> animClips = new List<AnimationClip>();
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
        private static Vector2 scrollview;
        private List<string> categoryFields = new List<string>();
        private List<bool> categoryFoldouts = new List<bool>();
        public static List<Category> categories = new List<Category>();
        private string enterText;
        private int count;
        public static string debug;
        private bool materialSettings;
        public static bool completeAnimatorLogic = true;
        public static bool createAnimationClips = true;
        public static bool ignorePreviousStates = true;

        public enum CreationType
        {
            Material,
            GameObject
        }

        void DrawMaterialUI()
        {
            //Creates a foldout for each category made, which also holds an add button that will add a field
            Rect lastRect = new Rect();
            for (int i = 0; i < categories.Count; i++)
            {
                var name = categoryFields[i];
                EditorGUILayout.BeginVertical();
                categoryFoldouts[i] = EditorGUILayout.Foldout(categoryFoldouts[i], name, true);
                //Logic for what will be under each category foldout, in this case it will be material object fields.
                if (categoryFoldouts[i])
                {
                    int hash = categoryFoldouts[i].GetHashCode();
                    Array.Resize(ref categories.ElementAt(i).objectRef, categories.Count);
                    categories.ElementAt(i).objectRef[0] = (GameObject)EditorGUILayout.ObjectField("Mesh Object", categories.ElementAt(i).objectRef[0], typeof(GameObject), true);
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
                        lastRect = GUILayoutUtility.GetLastRect();
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

        public void OnGUI()
        {
            //Sets up a gameobject slot within the editor window.
            avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", avatar, typeof(GameObject), true);
            EditorGUILayout.LabelField(debug);
            MaterialFoldout = EditorGUILayout.Foldout(MaterialFoldout, "Material", true);
            EditorGUILayout.BeginScrollView(scrollview);
            EditorGUILayout.BeginVertical();
            //Creates the foldout which holds material categories
            if (MaterialFoldout)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8);
                materialSettings = EditorGUILayout.Foldout(materialSettings, "Settings", true);
                if (materialSettings)
                {
                    completeAnimatorLogic = GUILayout.Toggle(completeAnimatorLogic, "Complete Animator Logic");
                    createAnimationClips = GUILayout.Toggle(createAnimationClips, "Create Animation Clips for Materials");
                    ignorePreviousStates = GUILayout.Toggle(ignorePreviousStates, "Ignore Previously Created States");
                }
                EditorGUILayout.EndHorizontal();

                enterText = EditorGUILayout.TextField(enterText);
                //Creates 'create category' button
                if (GUILayout.Button("Create category"))
                {
                    var newMatList = new List<Material>();
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

        public void InitializeUI()
        {
            enterText = "";
            categories.Clear();
            categoryFields.Clear();
            categoryFoldouts.Clear();
        }

    }
}


