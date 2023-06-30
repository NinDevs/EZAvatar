#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EZAva2
{
    public class ControllerUtil
    {
        public static AnimatorControllerLayer GetLayerByName(ref AnimatorController ac, string name)
        {
            foreach (var currLayer in ac.layers)
            {
                if (currLayer.name == name)
                    return currLayer;
            }
            return null;
        }

        public static AnimatorControllerParameter GetParameterByName(AnimatorController controller, string name)
        {
            foreach (var parameter in controller.parameters)
            {
                if (parameter.name == name)
                    return parameter;
            }
            return null;
        }

        public static void SetLayerWeight(AnimatorController ac, AnimatorControllerLayer acl, float newWeight)
        {
            SerializedObject so = new SerializedObject(ac);
            var layers = so.FindProperty("m_AnimatorLayers");
            foreach (SerializedProperty currLayer in layers)
            {
                if (currLayer.FindPropertyRelative("m_Name").stringValue == acl.name)
                {
                    currLayer.FindPropertyRelative("m_DefaultWeight").floatValue = newWeight;
                }
            }
            so.ApplyModifiedProperties();
        }

        public static void ApplyTransitionSettings(ref AnimatorStateTransition ast, bool hasExitTime, float exitTimeValue, bool hasFixedDuration, float duration)
        {
            ast.hasExitTime = hasExitTime;
            ast.exitTime = exitTimeValue;
            ast.hasFixedDuration = hasFixedDuration;
            ast.duration = duration;
        }

        public static void ForceAnimatorStatePosition(AnimatorState animatorState, Vector3 newPos)
        {
            SerializedObject so = new SerializedObject(animatorState);
            var posParameter = so.FindProperty("m_Position");
            posParameter.vector3Value = newPos;
            so.ApplyModifiedProperties();
        }

        public static void TurnOnParameterBool(ref AnimatorController controller, string parameterName)
        {
            SerializedObject animator = new SerializedObject(controller);
            var parameters = animator.FindProperty("m_AnimatorParameters");
            for (int i = 0; i < parameters.arraySize; i++)
            {
                if (parameters.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue == parameterName)
                    parameters.GetArrayElementAtIndex(i).FindPropertyRelative("m_DefaultBool").boolValue = true;
            }
            animator.ApplyModifiedProperties();
        }

        public static AnimatorState GetAnimatorStateInLayer(AnimatorControllerLayer layer, string stateName)
        {
            for (int i = 0; i < layer.stateMachine.states.Length; i++)
            {
                if (layer.stateMachine.states[i].state.name == stateName)
                    return layer.stateMachine.states[i].state;
            }
            return null;
        }

        public static void RemoveStates(AnimatorControllerLayer layer)
        {
            var statemachine = layer.stateMachine;
            var count = statemachine.states.Count();
            if (count > 0)
            {
                foreach (var state in statemachine.states)
                    statemachine.RemoveState(state.state);
            }         
        }
        
#if VRC_SDK_VRCSDK3
        public static void ChangeParameterToInt(AnimatorController controller, AnimatorControllerLayer layer, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters expressionParametersMenu, string parametername)
        {
            var statemachine = layer.stateMachine;
            if (statemachine.states.Count() == 2)
            {
                var vrcparam = expressionParametersMenu.FindParameter(parametername);
                vrcparam.defaultValue = 0;
                vrcparam.valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Int;
                controller.RemoveParameter(GetParameterByName(controller, parametername));
                controller.AddParameter(parametername, AnimatorControllerParameterType.Int);
                GetParameterByName(controller, parametername).defaultInt = 0;
                int iteration = 0;

                for (int g = 0; g < statemachine.states.Count(); g++)
                {
                    var state = statemachine.states[g].state;
                    var transitions = state.transitions;
                    for (int r = 0; r < transitions.Count(); r++)
                    {
                        //Deletes any previous transitions between both states and replaces them with an anystate transition
                        if (transitions[r].conditions.Where(x => x.mode == AnimatorConditionMode.If) != null ||
                            transitions[r].conditions.Where(x => x.mode == AnimatorConditionMode.IfNot) != null)
                        {
                            state.RemoveTransition(transitions[r]);
                            var anyStateTransition = statemachine.AddAnyStateTransition(state);
                            ApplyTransitionSettings(ref anyStateTransition, false, 0, false, 0);
                            anyStateTransition.AddCondition(AnimatorConditionMode.Equals, iteration++, parametername);
                        }
                    }
                }

            }
        }
#endif
    }

    public class AnimUtil
    {
        public static void ExportClip(AnimationClip clip, string meshName)
        {
            var savePath = $"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Animations/{meshName}";
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                AssetDatabase.Refresh();
            }
            if (EZAvatar.avatar != null)
            {
                if (!File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Animations/{meshName}/{clip.name}.anim"))
                {
                    AssetDatabase.CreateAsset(clip, $"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Animations/{meshName}/{clip.name}.anim");
                    if (EZAvatar.enableUnityDebugLogs)
                        Debug.Log($"<color=green>[EZAvatar]</color>: Created {clip.name}.anim at Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Animations/{meshName}!");
                    Algorithm.animsCreated++;
                }
                else
                {
                    if (EZAvatar.enableUnityDebugLogs)
                        Debug.Log($"<color=green>[EZAvatar]</color>: Animation clip {clip.name} already exists within Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Animations/{meshName}, skipping...");
                }
            }
            else
            {
                Debug.LogWarning("<color=yellow>[EZAvatar]</color>: Avatar gameobject was not found.");
                EZAvatar.debug = Helper.SetTextColor("Avatar gameobject was not found.", "orange");
            }
        }

        public static int FindRendererMaterialIndex(ref dynamic renderer, Material[] materialsToSearch)
        {
            var index = 0;
            SerializedObject render = new SerializedObject(renderer);

            /*Iterate through each material in the material array that is on the skinned mesh renderer, in order to find which material name matches the name
            Of any of the materials in the category (mat list), which will allow us to find the proper element index of the material for reference.*/
            for (int y = 0; y < render.FindProperty("m_Materials.Array").arraySize; y++)
            {
                var material = renderer.sharedMaterials[y];
                for (int j = 0; j < materialsToSearch.Count(); j++)
                {
                    SerializedObject mat = new SerializedObject(materialsToSearch[j]);
                    if (mat.FindProperty("m_Name").stringValue == material.name)
                    {
                        index = y;
                        break;
                    }
                }
            }
            
            return index;
        }

        public static dynamic FetchRenderer(GameObject obj, ref System.Type type)
        {
            if (obj?.GetComponent<SkinnedMeshRenderer>() != null)
            {
                type = typeof(SkinnedMeshRenderer);
                return obj.GetComponent<SkinnedMeshRenderer>();
            }
            else if (obj?.GetComponent<MeshRenderer>() != null) 
            {
                type = typeof(MeshRenderer);
                return obj.GetComponent<MeshRenderer>();
            } 
            else return null;
        }

        public static void MakeAnimationClips(ref List<Category> matCategories, ref List<Category> objCategories)
        {
            for (int i = 0; i < matCategories.Count(); i++)
            {

                var materials = matCategories[i].materials;
                var gameObj = matCategories[i].objects.FirstOrDefault();
               
                matCategories[i].name.Trim();
                gameObj.name.Trim();

                if (materials.Count() < 2)
                {
                    EZAvatar.debug = Helper.SetTextColor($"Must provide a minimum of two materials! Base material and the swap material(s). Skipping over {matCategories[i].name}...", "yellow");
                    Debug.LogWarning($"<color=yellow>[EZAvatar]</color>: Must provide a minimum of two materials! Base material and the swap material(s). Skipping over {matCategories[i].name}...");
                    continue;
                }

                if (materials.Count() >= 2)
                {
                    System.Type rendertype = null;
                    var render = FetchRenderer(gameObj, ref rendertype);                   
                    if (render == null)
                    {
                        EZAvatar.debug = Helper.SetTextColor($"Mesh object was not found in {matCategories[i].name}. Skipping...", "yellow");
                        Debug.LogWarning($"<color=yellow>[EZAvatar]</color>: Mesh object was not found in {matCategories[i].name}. Skipping...");
                        continue;
                    }

                    var index = FindRendererMaterialIndex(ref render, materials);
                    
                    //Binding allows us to create a curve that is binded to the gameobject and refers to the correct info like renderer slots.
                    EditorCurveBinding binding = new EditorCurveBinding();
                    binding.type = rendertype;
                    //Removes the avatar name from the front of the hierarchy path, as otherwise the animation references would be incorrect.
                    var path = rendertype == typeof(SkinnedMeshRenderer) ? ((SkinnedMeshRenderer)render).gameObject.transform.GetHierarchyPath().Substring(EZAvatar.avatar.name.Length + 1) : ((MeshRenderer)render).gameObject.transform.GetHierarchyPath().Substring(EZAvatar.avatar.name.Length + 1);
                    binding.path = path;
                    binding.propertyName = $"m_Materials.Array.data[{index}]";

                    for (int x = 0; x < materials.Count(); x++)
                    {
                        //We create a new animationclip for each material, with the name the same as the material name.
                        var clip = new AnimationClip();
                        clip.name = materials[x].name;
                        ObjectReferenceKeyframe[] keyframe = new ObjectReferenceKeyframe[2];
                        keyframe[0].value = materials[x];
                        keyframe[0].time = 0;
                        keyframe[1].value = materials[x];
                        keyframe[1].time = 1 / clip.frameRate;
                        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframe);

                        matCategories[i].animClips.Add(clip);
                        ExportClip(clip, matCategories[i].objects[0].name);
                    }
                }
            }
                
            for (int i = 0; i < objCategories.Count(); i++)
            {

                var gameObj = objCategories[i].objects;
               
                foreach (var obj in gameObj)
                    obj.name.Trim();
             
                objCategories[i].name.Trim();

                if (objCategories[i].objects[0] == null)
                {
                    EZAvatar.debug = Helper.SetTextColor($"Must provide a minimum of one gameobject. Skipping over {objCategories[i].name}...", "yellow");
                    Debug.LogWarning($"<color=yellow>[EZAvatar]</color>: Must provide a minimum of one gameobject. Skipping over {objCategories[i].name}...");
                    continue;
                }

                //Allows automatic conversion from on off to any state int toggles if we add to an existing layer that had previously two states and toggled via bool
                bool wasOnOffLayer = (objCategories[i].layerExists == true && (ControllerUtil.GetParameterByName(EZAvatar.controller, $"Toggle {objCategories[i].name}")?.type == AnimatorControllerParameterType.Bool)) == true ? true : false;

                //If we are using any state transitions for objects toggles instead of on/off
                if (objCategories[i].makeIdle)
                {
                    var idleClip = File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Animations/{objCategories[i].name}/{objCategories[i].name}Idle.anim") ? LoadAnimClip($"{objCategories[i].name}Idle", objCategories[i].name) : null;
                    if (idleClip != null)
                        EditorUtility.SetDirty(idleClip);
                    if (wasOnOffLayer)
                    {
                        var onStateClip = ControllerUtil.GetLayerByName(ref EZAvatar.controller, $"Toggle {objCategories[i].name}").stateMachine.states.Where(x => x.state.name.Contains("ON")).ToList()[0].state.motion as AnimationClip;
#pragma warning disable CS0618 // Type or member is obsolete
                        var onStateCurve = AnimationUtility.GetAllCurves(onStateClip)[0];
#pragma warning restore CS0618 // Type or member is obsolete

                        var newCurve = new AnimationCurve();
                        newCurve.AddKey(0, 0);
                        newCurve.AddKey(1 / onStateClip.frameRate, 0);

                        idleClip = new AnimationClip();
                        idleClip.name = $"{objCategories[i].name}Idle";
                        idleClip.SetCurve(onStateCurve.path, typeof(GameObject), "m_IsActive", newCurve);
                    }

                    for (int y = 0; y < gameObj.Count(); y++)
                    {
                        var onClip = new AnimationClip();
                        var path = gameObj[y].transform.GetHierarchyPath().Substring(EZAvatar.avatar.name.Length + 1);

                        //Creates curves/keys for gameobject active, per object
                        var onCurve = new AnimationCurve();
                        onCurve.AddKey(0, 1);
                        onCurve.AddKey(1 / onClip.frameRate, 1);
                        onClip.SetCurve(path, typeof(GameObject), "m_IsActive", onCurve);

                        onClip.name = $"{gameObj[y].name}ON";
                        objCategories[i].animClips.Add(onClip);
                        ExportClip(onClip, objCategories[i].name);

                        if (idleClip != null)
                        {
                            var idleOffCurve = new AnimationCurve();
                            idleOffCurve.AddKey(0, 0);
                            idleOffCurve.AddKey(1 / idleClip.frameRate, 0);
                            idleClip.SetCurve(path, typeof(GameObject), "m_IsActive", idleOffCurve);
                        }
                        else
                        {
                            idleClip = new AnimationClip();
                            idleClip.name = $"{objCategories[i].name}Idle";
                            var idleOffCurve = new AnimationCurve();
                            idleOffCurve.AddKey(0, 0);
                            idleOffCurve.AddKey(1 / idleClip.frameRate, 0);
                            idleClip.SetCurve(path, typeof(GameObject), "m_IsActive", idleOffCurve);
                            
                            ExportClip(idleClip, objCategories[i].name);
                        }

                    }
                    
                    if (!File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Animations/{objCategories[i].name}/{objCategories[i].name}Idle.anim"))
                        ExportClip(idleClip, objCategories[i].name);
                }
                else
                {
                    var onClip = new AnimationClip();
                    var offClip = new AnimationClip();
                    onClip.name = $"{objCategories[i].name}ON";
                    offClip.name = $"{objCategories[i].name}OFF";

                    for (int y = 0; y < gameObj.Count(); y++)
                    {

                        var path = gameObj[y].transform.GetHierarchyPath().Substring(EZAvatar.avatar.name.Length + 1);

                        //Creates curves/keys for gameobject active, per object
                        var onCurve = new AnimationCurve();
                        onCurve.AddKey(0, 1);
                        onCurve.AddKey(1 / onClip.frameRate, 1);
                        onClip.SetCurve(path, typeof(GameObject), "m_IsActive", onCurve);

                        //Creates curves/keys for gameobject inactive, per object
                        var offCurve = new AnimationCurve();
                        offCurve.AddKey(0, 0);
                        offCurve.AddKey(1 / onClip.frameRate, 0);
                        offClip.SetCurve(path, typeof(GameObject), "m_IsActive", offCurve);

                    }
                    //Creates the clip
                    if (gameObj.Count() == 1)
                    {
                        onClip.name = $"{gameObj[0].name}ON";
                        objCategories[i].animClips.Add(onClip);
                        ExportClip(onClip, gameObj[0].name);

                        offClip.name = $"{gameObj[0].name}OFF";
                        objCategories[i].animClips.Add(offClip);
                        ExportClip(offClip, gameObj[0].name);
                    }

                    else
                    {
                        objCategories[i].animClips.Add(onClip);
                        //Creates a folder called "Mutli-Toggles" which will host all animations that toggle multiple things at once, for neat organization :)
                        ExportClip(onClip, "Multi-Toggles");

                        objCategories[i].animClips.Add(offClip);
                        ExportClip(offClip, "Multi-Toggles");
                    }                 
                }            
            }
        }

        public static AnimationClip LoadAnimClip(string clipname, string meshName)
        {
           return (AnimationClip)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Animations/{meshName}/{clipname}.anim", typeof(AnimationClip));
        }
    }
}

#endif