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
            EZAvatar.debug = $"Finished without errors in {string.Format("{0:0.000}", Algorithm.elaspedTime)}s. Created {Algorithm.layersCompleted} new layers, {Algorithm.statesCompleted} new states, and {Algorithm.menusCompleted} new menus. :)";
            Debug.Log(EZAvatar.debug);
            Algorithm.layersCompleted = 0;
            Algorithm.statesCompleted = 0;
            Algorithm.menusCompleted = 0;
            Algorithm.elaspedTime = 0;
        }

        public static bool HasFXLayer(int arg)
        {
            if (EZAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>()?.baseAnimationLayers.ToList().Where
                (x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController != null)
            {
                EZAvatar.controller = EZAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().baseAnimationLayers.ToList().Where
                (x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController as AnimatorController;                             
                if (EZAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().customExpressions != true)
                    EZAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().customExpressions = true;
                EditorUtility.SetDirty(EZAvatar.controller);
            }

            if (EZAvatar.controller == null)
            {
                EZAvatar.debug = "There is no FX Layer on this avatar! FX Layer animator controller is required for this script!";
                Debug.Log(EZAvatar.debug);               
                return false;
            }

            else
            {
                if (arg != 0)
                {
                    EZAvatar.debug = "FX Layer found! Proceeding . . . ";
                    Debug.Log(EZAvatar.debug);
                }
                else
                {
                    EZAvatar.debug = "FX Layer found!";
                    Debug.Log(EZAvatar.debug);
                }
                return true;
            }
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

        public static void SwitchedParameter(ref VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu menu, ref AnimatorControllerLayer layer, List<AnimatorState> states)
        {
            int count = 0;
            menu.controls.Clear();
            foreach (var state in layer.stateMachine.states)
            {
                foreach (var newstate in states)
                {
                    if (state.state != newstate)
                    {
                        var control = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = $"{state.state.name}",
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                            parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = $"{layer.name}Mat" },
                            value = count
                        };
                        menu.controls.Add(control);
                        count++;
                    }
                }               
            }
            AssetDatabase.SaveAssets();
        }
    }
}

#endif
#endif