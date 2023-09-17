using EZAva2;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static EZAvatar;

public class UI
{
    private static bool[] blendBools = new bool[1];
    private static string matCategoryFieldText;
    private static string objCategoryFieldText;
    private static string blendCategoryFieldText;
    
    public static void DrawMaterialUI()
    {           
        EditorGUILayout.BeginHorizontal();
            
        matCategoryFieldText = EditorGUILayout.TextField(matCategoryFieldText);

        if (GUILayout.Button("Create category")) {
            Helper.CreateCategoryButton(ref matCategories, ref matCategoryFieldText);
            matCategories.Last().slots = 2;
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
                if (!matCategories[i].layerExists && Helper.DoesCategoryExistAndHaveStates(controller, matCategories[i].name)) 
                {
                    matCategories[i].layerExists = true;
                    matCategories[i].slots = 1;
                }
                    
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

                if (!matCategories[i].layerExists) {matCategories[i].objects[0] = (GameObject)EditorGUILayout.ObjectField("Mesh Object", matCategories[i].objects[0], typeof(GameObject), true);}

                //Creates new object fields based on the value of matCount, which increments with the Add button seen below.
                for (int y = 0; y < matCategories[i].slots; y++)
                {
                    Array.Resize(ref matCategories[i].materials, matCategories[i].slots);
                    if (y == 0 && !matCategories[i].layerExists)
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

    public static void DrawGameObjUI()
    {              
        EditorGUILayout.BeginHorizontal();
            
        objCategoryFieldText = EditorGUILayout.TextField(objCategoryFieldText);

        if (GUILayout.Button("Create category")) {
            Helper.CreateCategoryButton(ref objCategories, ref objCategoryFieldText);
            objCategories.Last().slots = 1;
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
                if (Helper.DoesCategoryExistAndHaveStates(controller, ref objCategories[i].name) && !objCategories[i].layerExists)
                {
                    objCategories[i].layerExists = true;
                    objCategories[i].makeIdle = true;
                    objCategories[i].slots = 1;
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

    public static void DrawBlendshapeUI()
    {      
        EditorGUILayout.BeginHorizontal();

        blendCategoryFieldText = EditorGUILayout.TextField(blendCategoryFieldText);

        if (GUILayout.Button("Create category"))
        {
            Helper.CreateCategoryButton(ref blendCategories, ref blendCategoryFieldText);
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

    public static void ReInitialize()
    {
        matCategoryFieldText = "";
        objCategoryFieldText = "";
        blendCategoryFieldText = "";
        matCategories.Clear();
        objCategories.Clear();
        blendCategories.Clear();
    }
}
