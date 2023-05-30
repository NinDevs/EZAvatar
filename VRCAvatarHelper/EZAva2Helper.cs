#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EZAvatar
{       
    public class Helper
    {
        //Creates a new entry in our list which stores the category name, the reference to the foldout bool, and the list of materials.
        public static void AddCategory(List<Category> dict, string categoryName, CategoryType type)
        {
            Category category = new Category();
            category.name = categoryName;
            category.foldout = true;
            category.slots = 0;
            category.type = type;
            dict.Add(category);
        }

        public static void RemoveCategory(bool execute, Category category)
        {
            if(execute)
                EzAvatar.categories.Remove(category);
        }

        public static bool DoesCategoryExist(List<Category> categories, string categoryName, CategoryType type)
        {
            bool result = new bool();
            foreach (var category in categories)
            {
                if (category.name.Equals(categoryName) && category.type == type)
                    result = true;
                else
                    result = false;
            }
            return result;
        }

        public static bool DoesCategoryExistAndHaveStates(AnimatorController controller, string categoryName)
        {
            var layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == categoryName)
                {
                    if (layers[i].stateMachine.states.Length >= 2)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
     
        public string MaterialToGUID(Material mat)
        {
            var mGUID = "";
            var mFileId = "";
            bool success = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mat, out string GUID, out long fileId);

            if (success)
            {
                mGUID = GUID;
                mFileId = fileId.ToString();
            }
            else
            {
                Debug.Log($"Failed to fetch the GUID of material {mat.name}.");
                return "";
            }

            var value = "fileID: " + $"{fileId}, guid: {GUID}, " + "type: 2";
            return value;
        }

        public static void DisplayCreationResults()
        {
            EzAvatar.debug = $"Finished without errors. Created {Algorithm.layersCompleted} new layers, new {Algorithm.statesCompleted} states, and {Algorithm.menusCompleted} new menus. :)";
            Debug.Log(EzAvatar.debug);
            Algorithm.layersCompleted = 0;
            Algorithm.statesCompleted = 0;
            Algorithm.menusCompleted = 0;
        }

        public static bool HasFXLayer(int arg)
        {
            if (EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>()?.baseAnimationLayers.ToList().Where
                (x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController != null)
            {
                EzAvatar.controller = EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().baseAnimationLayers.ToList().Where
                (x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController as AnimatorController;
               
            }

            if (EzAvatar.controller == null)
            {
                EzAvatar.debug = "There is no FX Layer on this avatar! FX Layer animator controller is required for this script!";
                Debug.Log(EzAvatar.debug);               
                return false;
            }

            else
            {
                if (arg != 0)
                {
                    EzAvatar.debug = "FX Layer found! Proceeding . . . ";
                    Debug.Log(EzAvatar.debug);
                }
                else
                {
                    EzAvatar.debug = "FX Layer found!";
                    Debug.Log(EzAvatar.debug);
                }
                return true;
            }
        }

        public static bool DoesMenuExist(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu menu, bool isSub)
        {
            if (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{menu.name}.asset") && !isSub)
                return true;
            
            else if (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus/{menu.name}.asset") && isSub)
                return true;

            else
                return false;
        }

    }
  
    public class VRCUtil
    {
        public static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter AddNewParameter(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters vrcExpressionParameters, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType valueType, float defaultValue, string name)
        {
            SerializedObject parameters_S = new SerializedObject(vrcExpressionParameters);
            parameters_S.FindProperty("parameters").arraySize++;
            parameters_S.ApplyModifiedProperties();

            vrcExpressionParameters.parameters[vrcExpressionParameters.parameters.Length - 1].valueType = valueType;
            vrcExpressionParameters.parameters[vrcExpressionParameters.parameters.Length - 1].defaultValue = defaultValue;
            vrcExpressionParameters.parameters[vrcExpressionParameters.parameters.Length - 1].name = name;
            vrcExpressionParameters.parameters[vrcExpressionParameters.parameters.Length - 1].saved = true;
            return vrcExpressionParameters.parameters[vrcExpressionParameters.parameters.Length - 1];
        }

        public static void DeleteParameter(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters vrcExpressionParameters, string parameterName)
        {
            SerializedObject parameters_S = new SerializedObject(vrcExpressionParameters);
            var parameters = parameters_S.FindProperty("parameters");
            for(int i = 0; i < parameters.arraySize; i++)
            {
                if (parameters.GetArrayElementAtIndex(i).name == parameterName)
                    parameters.DeleteArrayElementAtIndex(i); 
            }
            parameters_S.ApplyModifiedProperties();
        }
    }
}

#endif