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
    public class ControllerUtil
    {
        public static AnimatorControllerLayer GetLayerByName(AnimatorController ac, string name)
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

        public static void ApplyTransitionSettings(AnimatorStateTransition ast, bool hasExitTime, float exitTimeValue, bool hasFixedDuration, float duration)
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

        public static void TurnOnParameterBool(AnimatorController controller, string parameterName)
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

        public static void ChangeParameterToInt(AnimatorController controller, AnimatorControllerLayer layer, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters expressionParametersMenu, string parametername)
        {
            var statemachine = layer.stateMachine;
            if (statemachine.states.Count() == 2)
            {
                var vrcparam = expressionParametersMenu.parameters.Where(x => x.name == parametername).ToList()[0];
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
                            ApplyTransitionSettings(anyStateTransition, false, 0, false, 0);
                            anyStateTransition.AddCondition(AnimatorConditionMode.Equals, iteration++, parametername);
                        }
                    }
                }

            }
        }
    }

    public class AnimUtil
    {
        public static void ExportClip(AnimationClip clip, string meshName)
        {
            var savePath = $"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Animations/{meshName}";
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                AssetDatabase.Refresh();
            }
            if (EzAvatar.avatar != null)
            {
                if (!File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Animations/{meshName}/{clip.name}.anim"))
                {
                    AssetDatabase.CreateAsset(clip, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Animations/{meshName}/{clip.name}.anim");
                    Debug.Log($"Created {clip.name} at Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Animations/{meshName}!");
                }
                else
                    Debug.Log($"Animation clip {clip.name} already exists within Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Animations/{meshName}, skipping...");
            }
            else
            {
                EzAvatar.debug = "Avatar gameobject was not found.";
                Debug.Log(EzAvatar.debug);
            }
        }

        public static void MakeAnimationClips(List<Category> categories)
        {
            if (EzAvatar.createAnimationClips)
            {
                var allowed = false;
                foreach (var category in categories)
                {
                    if (category.type == CategoryType.Material)
                    {
                        var materials = category.materials;
                        var gameObj = category.objects.FirstOrDefault();

                        //Checks to see if a category exists with 2 or more states, allowing just one material through in the case of the feature "Ignore Previous States"
                        //(just adding one material as a toggle where the layer already exists and has states).
                        if (Helper.DoesCategoryExistAndHaveStates(EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().baseAnimationLayers.ToList().
                            Where(x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController 
                            as AnimatorController, category.name) == true)
                            allowed = true;

                        if (materials.Count() < 2 && !allowed)
                        {
                            EzAvatar.debug = "Must provide a minimum of two materials! Base material and the swap materials.";
                            Debug.Log(EzAvatar.debug);
                            return;
                        }

                        if (materials.Count() >= 2 || allowed && materials.Count() >= 1)
                        {
                            var render = gameObj?.GetComponent<SkinnedMeshRenderer>();
                            if (render == null)
                            {
                                EzAvatar.debug = "Mesh object was not found.";
                                Debug.Log(EzAvatar.debug);
                                return;
                            }
                            var index = 0;
                            SerializedObject matslotref = new SerializedObject(render);

                            /*Iterate through each material in the material array that is on the skinned mesh renderer, in order to find which material name matches the name
                            Of any of the materials in the category, which will allow us to find the proper element index of the material for reference.*/
                            for (int i = 0; i < matslotref.FindProperty("m_Materials.Array").arraySize; i++)
                            {
                                var material = render.sharedMaterials[i];
                                for (int j = 0; j < materials.Count(); j++)
                                {
                                    SerializedObject mat = new SerializedObject(materials[j]);
                                    if (mat.FindProperty("m_Name").stringValue == material.name)
                                    {
                                        index = i;
                                        break;
                                    }
                                }
                            }

                            //Binding allows us to create a curve that is binded to the gameobject and refers to the correct info like renderer slots.
                            EditorCurveBinding binding = new EditorCurveBinding();
                            binding.type = typeof(SkinnedMeshRenderer);
                            //Removes the avatar name from the front of the hierarchy path, as otherwise the animation references would be incorrect.
                            var path = render.gameObject.transform.GetHierarchyPath().Substring(EzAvatar.avatar.name.Length + 1);
                            binding.path = path;
                            binding.propertyName = $"m_Materials.Array.data[{index}]";

                            for (int i = 0; i < materials.Count(); i++)
                            {
                                //We create a new animationclip for each material, with the name the same as the material name.
                                var clip = new AnimationClip();
                                clip.name = materials[i].name;
                                ObjectReferenceKeyframe[] keyframe = new ObjectReferenceKeyframe[2];
                                keyframe[0].value = materials[i];
                                keyframe[0].time = 0;
                                keyframe[1].value = materials[i];
                                keyframe[1].time = 1 / clip.frameRate;
                                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframe);

                                category.animClips.Add(clip);
                                ExportClip(clip, gameObj.name);
                            }
                        }
                    } 
                    
                    if (category.type == CategoryType.GameObject)
                    {
                        var gameObj = category.objects;

                        if (category.objects[0] == null)
                        {
                            EzAvatar.debug = "Must provide a minimum of one gameobject.";
                            Debug.Log(EzAvatar.debug);
                            return;
                        }

                        if (gameObj.Count() >= 1)
                        {
                            var onClip = new AnimationClip();
                            var offClip = new AnimationClip();
                            onClip.name = $"{category.name}ON";
                            offClip.name = $"{category.name}OFF";

                            for (int i = 0; i < gameObj.Count(); i++)
                            {
                                
                                var path = gameObj[i].transform.GetHierarchyPath().Substring(EzAvatar.avatar.name.Length + 1);   
                                
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
                                category.animClips.Add(onClip);
                                ExportClip(onClip, gameObj[0].name);

                                offClip.name = $"{gameObj[0].name}OFF";                               
                                category.animClips.Add(offClip);
                                ExportClip(offClip, gameObj[0].name);
                            }
                            
                            else
                            {
                                category.animClips.Add(onClip);
                                //Creates a folder called "Mutli-Toggles" which will host all animations that toggle multiple things at once, for neat organization :)
                                ExportClip(onClip, "Multi-Toggles");

                                category.animClips.Add(offClip);
                                ExportClip(offClip, "Multi-Toggles");
                            }
                            
                        }
                    }
                }
            }
        }
    }
}

#endif