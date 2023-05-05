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
        public GameObject avatar;
        public Material[] material = new Material[0];
        private bool MaterialFoldout;
        public List<Material> materials = new List<Material>();
        public Material matslot;
        public List<Material> matslots = new List<Material>();
        private static Vector2 scrollview;
        private List<string> categoryFields = new List<string>();
        private List<bool> categoryFoldouts = new List<bool>();
        Dictionary<Category, List<Material>> categories = new Dictionary<Category, List<Material>>();
        private string enterText;
        private int count;
        private string debug;

        //Creates a new entry in our dictionary which stores the category name, the reference to the foldout bool, and the list of materials.
        public void AddCategory(Dictionary<Category, List<Material>> dict, string categoryName, bool foldoutBool, List<Material> dictMatList)
        {
            Category category = new Category();
            category.name = categoryName;
            category.foldout = foldoutBool;
            category.slots = dictMatList.Count;
            dict.Add(category, dictMatList);
        }

        public bool DoesKeyExist(Dictionary<Category, List<Material>> dict, string categoryName, bool foldoutBool)
        {
            bool result = new bool();
            foreach(var kvp in dict)
            {
                if (kvp.Key.name.Equals(categoryName) && kvp.Key.foldout.Equals(foldoutBool))
                {
                    result = true;
                }
                else if (!kvp.Key.name.Equals(categoryName) && !kvp.Key.foldout.Equals(foldoutBool))
                {
                    result = false;
                }
            }
            return result;
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
                    Array.Resize(ref categories.ElementAt(i).Key.objectRef, categories.Count);
                    categories.ElementAt(i).Key.objectRef[i] = (GameObject)EditorGUILayout.ObjectField("Mesh Object", categories.ElementAt(i).Key.objectRef[i], typeof(GameObject), true);
                    if (hash == categories.Keys.ElementAt(i).foldout.GetHashCode())
                    {
                        //Creates new object fields based on the value of matCount, which increments with the Add button seen below.
                        for (int j = 0; j < categories.Keys.ElementAt(i).slots; j++)
                        {
                            Array.Resize(ref categories.Keys.ElementAt(i).materials, categories.Keys.ElementAt(i).slots);
                            EditorGUILayout.BeginVertical();
                            categories.Keys.ElementAt(i).materials[j] = (Material)EditorGUILayout.ObjectField($"Mat {j}", categories.Keys.ElementAt(i).materials[j], typeof(Material), false);
                            //Only adds the material to the list if the material is not already in the list.
                            if (!categories.Values.ElementAt(i).Contains(categories.Keys.ElementAt(i).materials[j]) && categories.Keys.ElementAt(i).materials[j] != null)
                                categories.Values.ElementAt(i).Add(categories.Keys.ElementAt(i).materials[j]);
                            EditorGUILayout.EndVertical();
                        }
                    }
                }
                //Adds a button that increments an int, which is used to create new material fields
                if (GUILayout.Button("Add"))
                {
                    categories.ElementAt(i).Key.slots += 1;
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
            //Creates the foldout which holds material categories
            if (MaterialFoldout)
            {
                enterText = EditorGUILayout.TextField(enterText);
                //Creates 'create category' button
                if (GUILayout.Button("Create category"))
                {
                    var newMatList = new List<Material>();
                    if (count == 0)
                    {
                        categoryFoldouts.Add(true);
                        categoryFields.Add(enterText);
                        AddCategory(categories, enterText, categoryFoldouts.Last(), newMatList);
                    }
                    //Prevents categories with the same names being made
                    if (count > 0)
                    {
                        var exists = DoesKeyExist(categories, enterText, categoryFoldouts.ElementAt(categoryFoldouts.Count - 1));
                        if (!exists)
                        {
                            categoryFoldouts.Add(true);
                            categoryFields.Add(enterText);
                            AddCategory(categories, enterText, categoryFoldouts.Last(), newMatList);
                        }
                        else {
                            Debug.Log("Category already exists! Try a different name.");
                            debug = "Category already exists! Try a different name.";
                        }
                    }
                    enterText = "";
                    count++;
                }
                if (GUILayout.Button("test"))
                    MakeAnimationClips(categories, Type.Material);
                DrawMaterialUI();
               
            }
            EditorGUILayout.EndScrollView();
        }

        public enum Type
        {
            Material,
            GameObject
        }
        public string MaterialToGUID(Material mat)
        {
            var mGUID = "";
            var mFileId = ""; 
            bool success = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mat, out string GUID, out long fileId);          
            
            if (success){
                mGUID = GUID;
                mFileId = fileId.ToString();
            }
            else{
                Debug.Log($"Failed to fetch the GUID of material {mat.name}.");
                return "";
            }

            var value = "fileID: " + $"{fileId}, guid: {GUID}, " + "type: 2";
            return value;
        }

        public void ExportClip(AnimationClip clip, string meshName)
        {
            var savePath = $"{Application.dataPath}/Nin/EZAvatar/{avatar.name}/Animations/{meshName}";
            if (!Directory.Exists(savePath)){
                Directory.CreateDirectory(savePath);
                AssetDatabase.Refresh();
            }
            if (avatar != null) {
                AssetDatabase.CreateAsset(clip, $"Assets/Nin/EZAvatar/{avatar.name}/Animations/{meshName}/{clip.name}.anim");
                Debug.Log($"Created {clip.name} at Assets/Nin/EZAvatar/{avatar.name}/Animations/{meshName}!");
            }
            else
                debug = "Avatar gameobject was not found.";
                Debug.Log(debug);
        }

        public SkinnedMeshRenderer FindRenderer(GameObject gameObj)
        {
            if (gameObj.GetComponent<SkinnedMeshRenderer>() != null)
                return gameObj.GetComponent<SkinnedMeshRenderer>();
            else 
                debug = "Failed to retrieve skinned mesh renderer from the gameobject.";
                Debug.Log(debug);

            return null;
        }

        public void MakeAnimationClips(Dictionary<Category, List<Material>> categories, Type type)
        {
            var clips = new List<Material>();
            if (type == Type.GameObject)
            {

            }
            if (type == Type.Material)
            {
                foreach (var category in categories)
                {
                    var count = 0;
                    var layerName = category.Key.name;
                    var materials = category.Value;
                    var gameObj = category.Key.objectRef[count];

                    if (materials.Count < 2) {
                        debug = "Must provide a minimum of two materials! Base material and the swap materials.";
                        Debug.Log(debug);
                        return;
                    }
                    
                    if (materials.Count >= 2)
                    {
                        var render = gameObj?.GetComponent<SkinnedMeshRenderer>();
                        if (render == null) {
                            debug = "Mesh object was not found.";
                            Debug.Log(debug);
                        }
                        var index = 0;
                        SerializedObject matslotref = new SerializedObject(render);
                        /*Iterate through each material in the material array that is on the skinned mesh renderer, in order to find which material name matches the name
                        Of any of the materials in the category, which will allow us to find the proper element index of the material in which we will keyframe to be replaced to
                        another.*/
                        for (int i = 0; i < matslotref.FindProperty("m_Materials.Array").arraySize; i++)
                        {
                            var material = render.sharedMaterials[i];
                            for (int j = 0; j < materials.Count; j++)
                            {
                                SerializedObject mat = new SerializedObject(materials[j]);
                                if(mat.FindProperty("m_Name").stringValue == material.name)
                                {
                                    index = i;
                                    break;
                                }
                            }
                        }
                        
                        //Binding allows us to create a curve that is binded to the gameobject and refers to the correct info like renderer slots.
                        EditorCurveBinding binding = new EditorCurveBinding();
                        binding.type = typeof(SkinnedMeshRenderer);
                        //Removes the avatar name from the front of the hierarchy path, as then the animation references would be incorrect.
                        var path = render.gameObject.transform.GetHierarchyPath().Substring(avatar.name.Length + 1);
                        binding.path = path;
                        binding.propertyName = $"m_Materials.Array.data[{index}]";

                        for (int i = 0; i < materials.Count; i++)
                        {
                            //We create a new animationclip for each material, with the name the same as the material name.
                            var clip = new AnimationClip();
                            clip.name = materials[i].name;           
                            ObjectReferenceKeyframe[] keyframe = new ObjectReferenceKeyframe[1];
                            keyframe[0].value = materials[i];
                            keyframe[0].time = 0;
                            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframe);
                            ExportClip(clip, gameObj.name);
                        } 
                    }
                }
            }
        }

    }
}



