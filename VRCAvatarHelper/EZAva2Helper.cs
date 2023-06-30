#if UNITY_EDITOR
#if VRC_SDK_VRCSDK3

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EZAva2
{
    public class Helper
    {
        //Creates a new entry in our list which stores the category name, the reference to the foldout bool, and the list of materials.
        public static void AddCategory(List<Category> dict, string categoryName)
        {
            Category category = new Category();
            category.name = categoryName;
            category.foldout = true;
            category.slots = 0;
            dict.Add(category);
        }

        public static bool DoesCategoryExist(List<Category> categories, string categoryName)
        {
            bool result = false;
            for (int i = 0; i < categories.Count(); i++)
            {
                if (categories[i].name.Equals(categoryName))
                    result = true;
                else
                    result = false;
            }
            return result;
        }

        public static bool DoesCategoryExistAndHaveStates(AnimatorController controller, string categoryName)
        {
            var layers = controller?.layers;
            for (int i = 0; i < layers?.Length; i++)
            {
                if (layers[i]?.name == categoryName)
                {
                    if (layers[i]?.stateMachine.states.Length >= 2)
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
            EZAvatar.debug = SetTextColor($"Finished without errors in {string.Format("{0:0.000}", Algorithm.elaspedTime)}s. " +
                $"Created {Algorithm.animsCreated} new anims, {Algorithm.layersCompleted} new layers, {Algorithm.statesCompleted} new states, and {Algorithm.menusCompleted} new menus at <a href='Assets/Nin/EZAvatar/{EZAvatar.avatar.name}'>Assets/Nin/EZAvatar/{EZAvatar.avatar.name}</a>. :)", "#1bfa53");
            Debug.Log($"<color=cyan>[EZAvatar]</color>: Finished without errors in {string.Format("{0:0.000}", Algorithm.elaspedTime)}s. Created {Algorithm.animsCreated} new anims, {Algorithm.layersCompleted} new layers, {Algorithm.statesCompleted} new states, and {Algorithm.menusCompleted} new menus. :)");
            Algorithm.layersCompleted = 0;
            Algorithm.statesCompleted = 0;
            Algorithm.menusCompleted = 0;
            Algorithm.elaspedTime = 0;
            Algorithm.animsCreated = 0;
        }

        public static bool HasFXLayer()
        {
            EZAvatar.controller = null;
            
            if (EZAvatar.avatar?.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>()?.baseAnimationLayers.ToList().Where
                (x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController != null)
            {
                EZAvatar.controller = EZAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().baseAnimationLayers.ToList().Where
                (x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController as AnimatorController;                             
                
                if (EZAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().customExpressions != true)
                    EZAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().customExpressions = true;

                EditorUtility.SetDirty(EZAvatar.controller);
            }

            if (EZAvatar.controller == null && EZAvatar.avatar != null)
            {
                EZAvatar.debug = SetTextColor("There is no <b>FX Layer</b> on this avatar! FX Layer animator controller is required for this script!", "yellow");
                Debug.Log("<color=yellow>[EZAvatar]</color>: There is no FX Layer on this avatar! FX Layer animator controller is required for this script!");
                return false;
            }

            else if (EZAvatar.controller != null && EZAvatar.avatar != null)
            {
                EZAvatar.debug = SetTextColor("FX Layer found!", "#1bfa53");
                Debug.Log("<color=green>[EZAvatar]</color>: FX Layer found!");
                return true;
            }

            else
            {
                EZAvatar.debug = "";
            }

            return false;
        }

        public static bool DoesMenuExist(string menu, bool isSub)
        {
            if (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/{menu}.asset") && !isSub)
                return true;
            
            else if (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{menu}.asset") && isSub)
                return true;

            else
                return false;
        }

        public static string SetTextColor(string debug, string color)
        {
            return $"<color={color}>{debug}</color>";
        }

        public static void SelectAssetAtPath<T>(string path)
        {
            var folder = AssetDatabase.LoadAssetAtPath(path, typeof(T));
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
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
            for (int i = 0; i < parameters.arraySize; i++)
            {
                if (parameters.GetArrayElementAtIndex(i).name == parameterName)
                    parameters.DeleteArrayElementAtIndex(i); 
            }
            parameters_S.ApplyModifiedProperties();
        }
    }
}

#endif
#endif