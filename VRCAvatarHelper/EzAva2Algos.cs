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
    public class Algorithm
    {
        public static int layersCompleted = 0;
        public static int statesCompleted = 0;
        public static int menusCompleted = 0;
        public static double elaspedTime = 0;
        public static int animsCreated = 0;
        
        public static void SetupMaterialToggles(ref List<Category> matCategories)
        {
            var controller = EZAvatar.controller;
            var expressionParametersMenu = EZAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters;
            //Create parameters menu if it doesn't exist
            if (expressionParametersMenu == null) {
                VRCUtil.CreateParametersMenu(ref expressionParametersMenu);
            }

            for (int i = 0; i < matCategories.Count; i++)
            {
                var layername = matCategories[i].name;
                var parametername = layername + "Mat";
                var clips = matCategories[i].animClips;
                int clipcount = matCategories[i].animClips.Count;
                AnimatorState[] states = new AnimatorState[clipcount];

                int counter = matCategories[i].layerExists ? ControllerUtil.GetLayerByName(ref controller, layername).stateMachine.states.Count() : 0;
                int conditioncount = 0; //Variable for animator transition condition threshold values

                if (!EZAvatar.ignorePreviousStates && matCategories[i].layerExists)
                    ControllerUtil.RemoveStates(ControllerUtil.GetLayerByName(ref controller, layername));

                //Creates new layer in the animator, if it doesn't exist
                if (ControllerUtil.GetLayerByName(ref controller, layername) == null)
                {
                    controller.AddLayer(layername);
                    layersCompleted++;
                }
                //Retrieves the layer that has just been created
                var layer = ControllerUtil.GetLayerByName(ref controller, layername);
                matCategories[i].layer = layer;
                //Sets the layer weight to 1
                ControllerUtil.SetLayerWeight(controller, layer, 1);
                var statemachine = layer.stateMachine;

                for (int y = 0; y < clipcount; y++)
                {
                    var statename = clips[y].name;

                    //Opt for on/off logic if we have 2 animclips and the layer is yet to be made, to optimize the transitions
                    if (clipcount == 2 && !matCategories[i].layerExists)
                    {
                        if (ControllerUtil.GetParameterByName(controller, parametername) == null) {
                            controller.AddParameter(parametername, AnimatorControllerParameterType.Bool);
                            ControllerUtil.TurnOnParameterBool(ref controller, parametername);
                        }

                        if (expressionParametersMenu.FindParameter(parametername) == null)
                            VRCUtil.AddNewParameter(expressionParametersMenu, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool, 0, parametername);

                        if (matCategories[i].states.Count() != 2)
                        {
                            states[y] = statemachine.AddState(statename, new Vector3(360, y * 55));
                            statemachine.states.Where(x => x.state == states[y]).ToList()[0].state.motion = AnimUtil.LoadAnimClip(clips[y].name, matCategories[i].objects[0].name);
                            statesCompleted++;
                            matCategories[i].states.Add(states[y]);
                        }

                        if (matCategories[i].states.Count() == 2)
                        {
                            //Creates a transition that will start from the first state to the second state
                            layer.stateMachine.states[0].state.AddTransition(layer.stateMachine.states[1].state);
                            ControllerUtil.ApplyTransitionSettings(ref layer.stateMachine.states[0].state.transitions[0], false, 0, false, 0);
                            layer.stateMachine.states[0].state.transitions[0].AddCondition(AnimatorConditionMode.IfNot, 1, parametername);

                            //Creates a transition that will start from the second state to the first state
                            layer.stateMachine.states[1].state.AddTransition(layer.stateMachine.states[0].state);
                            ControllerUtil.ApplyTransitionSettings(ref layer.stateMachine.states[1].state.transitions[0], false, 0, false, 0);
                            layer.stateMachine.states[1].state.transitions[0].AddCondition(AnimatorConditionMode.If, 1, parametername);
                        }

                    }

                    //Detect that a layer already has 2 states and change the bool on/off transitions to any state transitions
                    else if (matCategories[i].layerExists && layer.stateMachine.states.Count() == 2)
                    {                   
                        if (statemachine.states.Count() >= 2 && ControllerUtil.GetParameterByName(controller, parametername).type == AnimatorControllerParameterType.Bool)
                        {
                            ControllerUtil.ChangeParameterToInt(controller, layer, expressionParametersMenu, parametername);                               
                            matCategories[i].switched = true;
                            foreach(var state in layer.stateMachine.states)
                                matCategories[i].states.Add(state.state);
                        }

                        if (ControllerUtil.GetAnimatorStateInLayer(layer, statename) == null)
                        {
                            states[y] = statemachine.AddState(statename, new Vector3(360, counter * 55));
                            statemachine.states.Where(x => x.state == states[y]).ToList()[0].state.motion = AnimUtil.LoadAnimClip(clips[y].name, matCategories[i].objects[0].name);
                            matCategories[i].states.Add(states[y]);
                            statesCompleted++;                               
                            var anyStateTransition = statemachine.AddAnyStateTransition(states[y]);
                            ControllerUtil.ApplyTransitionSettings(ref anyStateTransition, false, 0, false, 0);
                            anyStateTransition.AddCondition(AnimatorConditionMode.Equals, counter, parametername);
                            counter++;
                        } 

                    }                  

                    //Add new states normally
                    else if (clipcount >= 1)
                    {                      
                        //If we are adding states to an existing layer that already has states, set transition condition value variable to state count (ensures no overlapping values, int adds from where it left off in previous transitions)
                        if (statemachine.states.Count() > 0 && y == 0)
                            conditioncount = statemachine.states.Count();
                        //Only adds the parameter if it does not already exist 
                        if (ControllerUtil.GetParameterByName(controller, parametername) == null)
                            controller.AddParameter(parametername, AnimatorControllerParameterType.Int);
                        //Adds parameter to expressions menu if it doesn't exist
                        if (expressionParametersMenu.FindParameter(parametername) == null)
                            VRCUtil.AddNewParameter(expressionParametersMenu, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Int, 0, parametername);
                        //Creates a state in the layer if it does not already exist, along with transitions, and inserts clip as the state motion.
                        if (ControllerUtil.GetAnimatorStateInLayer(layer, statename) == null)
                        {
                            states[y] = statemachine.AddState(statename, new Vector3(360, conditioncount * 55));
                            //Set state motion to current anim clip
                            statemachine.states.Where(x => x.state == states[y]).ToList()[0].state.motion = AnimUtil.LoadAnimClip(clips[y].name, matCategories[i].objects[0].name);
                            statesCompleted++;
                            matCategories[i].states.Add(states[y]);
                            //Creates any state transition to each state
                            var anyStateTransition = statemachine.AddAnyStateTransition(states[y]);
                            //Applies transition settings
                            ControllerUtil.ApplyTransitionSettings(ref anyStateTransition, false, 0, false, 0);
                            //Adds condition to newly created transition
                            anyStateTransition.AddCondition(AnimatorConditionMode.Equals, conditioncount, parametername);
                            conditioncount++;
                        }
                    }  
                }
                //Apply desired write defaults value to each state
                foreach (var state in layer.stateMachine.states) state.state.writeDefaultValues = EZAvatar.writeDefaults;
            }
        }

        public static void SetupGameObjectToggles(ref List<Category> objCategories)
        {
            var controller = EZAvatar.controller;
            var expressionParametersMenu = EZAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters;
            //Create parameters menu if it doesn't exist
            if (expressionParametersMenu == null) {
                VRCUtil.CreateParametersMenu(ref expressionParametersMenu);
            }

            for (int i = 0; i < objCategories.Count; i++)
            {
                var layername = $"Toggle {objCategories[i].name}";
                var parametername = layername;
                var clips = objCategories[i].animClips;
                var clipcount = clips.Count();
                AnimatorState[] states = new AnimatorState[clipcount];

                if (!EZAvatar.ignorePreviousStates && objCategories[i].layerExists)
                    ControllerUtil.RemoveStates(ControllerUtil.GetLayerByName(ref controller, layername));

                if (ControllerUtil.GetLayerByName(ref controller, layername) == null)
                {
                    controller.AddLayer(layername);
                    layersCompleted++;
                }

                var layer = ControllerUtil.GetLayerByName(ref controller, layername);
                objCategories[i].layer = layer;
                ControllerUtil.SetLayerWeight(controller, layer, 1);

                // ON/OFF regular bool toggles 
                if (!objCategories[i].makeIdle)
                {
                    //Adds bool parameter if there are only two anims we are working with
                    if (ControllerUtil.GetParameterByName(controller, parametername) == null)
                    {
                        controller.AddParameter(parametername, AnimatorControllerParameterType.Bool);
                        ControllerUtil.TurnOnParameterBool(ref controller, parametername);
                    }

                    //Adds new parameter to expressions menu if missing
                    if (expressionParametersMenu.FindParameter(parametername) == null)
                        VRCUtil.AddNewParameter(expressionParametersMenu, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool, 0, parametername);
                    
                    for (int y = 0; y < clipcount; y++)
                    {
                        var statename = clips[y].name;                       
                        
                        states[y] = layer.stateMachine.AddState(statename, new Vector3(360, y * 55));
                        objCategories[i].states.Add(states[y]);
                        statesCompleted++;

                        //When both states have been created
                        if (states.Count() == 2 && y == clipcount - 1)
                        {
                            //Creates a transition that will start from the first state to the second state
                            layer.stateMachine.states[0].state.AddTransition(layer.stateMachine.states[1].state);
                            ControllerUtil.ApplyTransitionSettings(ref layer.stateMachine.states[0].state.transitions[0], false, 0, false, 0);
                            layer.stateMachine.states[0].state.transitions[0].AddCondition(AnimatorConditionMode.IfNot, 1, parametername);
                            //Set state motion to ON anim clip
                            layer.stateMachine.states[0].state.motion = AnimUtil.LoadAnimClip(clips.Where(x => x.name.Contains("ON")).First().name, objCategories[i].objects[0].name) != null ?
                                AnimUtil.LoadAnimClip(clips.Where(x => x.name.Contains("ON")).First().name, objCategories[i].objects[0].name) :
                                AnimUtil.LoadAnimClip(clips.Where(x => x.name.Contains("ON")).First().name, "Multi-Toggles");

                            //Creates a transition that will start from the second state to the first state
                            layer.stateMachine.states[1].state.AddTransition(layer.stateMachine.states[0].state);
                            ControllerUtil.ApplyTransitionSettings(ref layer.stateMachine.states[1].state.transitions[0], false, 0, false, 0);
                            layer.stateMachine.states[1].state.transitions[0].AddCondition(AnimatorConditionMode.If, 1, parametername);
                            //Set state motion to OFF anim clip
                            layer.stateMachine.states[1].state.motion = AnimUtil.LoadAnimClip(clips.Where(x => x.name.Contains("OFF")).First().name, objCategories[i].objects[0].name) != null ?
                                AnimUtil.LoadAnimClip(clips.Where(x => x.name.Contains("OFF")).First().name, objCategories[i].objects[0].name) :
                                AnimUtil.LoadAnimClip(clips.Where(x => x.name.Contains("OFF")).First().name, "Multi-Toggles");
                        }
                    }
                }
                // Idle logic 
                else
                {                  
                    int paramCounter = objCategories[i].layerExists ? ControllerUtil.GetLayerByName(ref controller, layername).stateMachine.states.Count() : 0;

                    for (int y = 0; y < clipcount; y++)
                    {
                        var statename = clips[y].name;
                        bool createdState = false;

                        if (layer.stateMachine.states.Count() >= 2 && ControllerUtil.GetParameterByName(controller, parametername).type == AnimatorControllerParameterType.Bool)
                        {                          
                            var previousStateName = layer.stateMachine.states[0].state.name;
                            var previousStateClip = layer.stateMachine.states[0].state.motion.name;
                            ControllerUtil.RemoveStates(layer);

                            states[y] = layer.stateMachine.AddState("Toggles Idle", new Vector3(360, 0));                           
                            objCategories[i].states.Add(states[y]);
                            layer.stateMachine.states[0].state.motion = AnimUtil.LoadAnimClip($"{objCategories[i].name}Idle", objCategories[i].name);

                            //Readd the state with the previous clip. This is just so that the default state is always the idle state, we delete the previous state and readd after idle state creation
                            layer.stateMachine.AddState(previousStateName, new Vector3(360, 55));
                            objCategories[i].states.Add(layer.stateMachine.states.Where(x => x.state.name == previousStateName).ToList()[0].state);
                            layer.stateMachine.states[1].state.motion = AnimUtil.LoadAnimClip(previousStateClip, "Switched");

                            ControllerUtil.ChangeParameterToInt(controller, layer, expressionParametersMenu, parametername);
                            objCategories[i].switched = true;

                            var anyStateTransition = layer.stateMachine.AddAnyStateTransition(layer.stateMachine.states[0].state);
                            ControllerUtil.ApplyTransitionSettings(ref anyStateTransition, false, 0, false, 0);
                            anyStateTransition.AddCondition(AnimatorConditionMode.Equals, 0, parametername);
                                               
                            var anyStateTransition2 = layer.stateMachine.AddAnyStateTransition(layer.stateMachine.states[1].state);
                            ControllerUtil.ApplyTransitionSettings(ref anyStateTransition2, false, 0, false, 0);
                            anyStateTransition2.AddCondition(AnimatorConditionMode.Equals, 1, parametername);
                        
                            paramCounter = 2;
                            EditorUtility.SetDirty(layer.stateMachine);
                        }

                        if (ControllerUtil.GetParameterByName(controller, parametername) == null)
                        {
                            controller.AddParameter(parametername, AnimatorControllerParameterType.Int);
                        }

                        if (expressionParametersMenu.FindParameter(parametername) == null)
                            VRCUtil.AddNewParameter(expressionParametersMenu, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Int, 0, parametername);

                        //Creates idle state if it does not exist
                        if (layer.stateMachine.states.Count() == 0)
                        {
                            states[y] = layer.stateMachine.AddState("Toggles Idle", new Vector3(31, -45));
                            objCategories[i].states.Add(states[y]);
                            layer.stateMachine.states[0].state.motion = AnimUtil.LoadAnimClip($"{objCategories[i].name}Idle", objCategories[i].name);
                            var anyStateTransition = layer.stateMachine.AddAnyStateTransition(layer.stateMachine.states[0].state);
                            ControllerUtil.ApplyTransitionSettings(ref anyStateTransition, false, 0, false, 0);
                            anyStateTransition.AddCondition(AnimatorConditionMode.Equals, 0, parametername);
                            paramCounter = 1;
                        }

                        if (ControllerUtil.GetAnimatorStateInLayer(layer, statename) == null)
                        {
                            states[y] = layer.stateMachine.AddState(statename, new Vector3(360, paramCounter * 55));
                            objCategories[i].states.Add(states[y]);
                            statesCompleted++;
                            createdState = true;
                        }

                        //When both states have been created
                        if (layer.stateMachine.states.Count() >= 2 && createdState)
                        {
                            //Creates a transition that will start from the idle state to the new state
                            var statecount = layer.stateMachine.states.Count() - 1;
                            var anyStateTransition = layer.stateMachine.AddAnyStateTransition(layer.stateMachine.states[statecount].state);
                            ControllerUtil.ApplyTransitionSettings(ref anyStateTransition, false, 0, false, 0);
                            anyStateTransition.AddCondition(AnimatorConditionMode.Equals, paramCounter, parametername);
                            //Set new state motion to ON anim clip
                            layer.stateMachine.states[statecount].state.motion = AnimUtil.LoadAnimClip(clips[y].name, objCategories[i].name) != null ?
                                AnimUtil.LoadAnimClip(clips[y].name, objCategories[i].name) :
                                AnimUtil.LoadAnimClip(clips[y].name, "Multi-Toggles");

                            paramCounter++;
                            createdState = false;
                        }
                    }              
                }
                //Apply desired write defaults value to each state
                foreach (var state in layer.stateMachine.states) state.state.writeDefaultValues = EZAvatar.writeDefaults;
            }
        }

        public static void SetupBlendshapeToggles(ref List<Category> blendshapeCategory)
        {
            var controller = EZAvatar.controller;
            var expressionParametersMenu = EZAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters;
            if (expressionParametersMenu == null) {
                VRCUtil.CreateParametersMenu(ref expressionParametersMenu);
            }

            for (int i = 0; i < blendshapeCategory.Count; i++)
            {
                var layername = ((blendshapeCategory[i].menuControl == ControlType.Toggle) == true) ? $"Toggle {blendshapeCategory[i].name}" : blendshapeCategory[i].name;
                var parametername = layername;
                var clips = blendshapeCategory[i].animClips;
                int clipcount = blendshapeCategory[i].animClips.Count;
                AnimatorState[] states = new AnimatorState[clipcount];

                if (ControllerUtil.GetLayerByName(ref controller, layername) == null)
                {
                    controller.AddLayer(layername);
                    layersCompleted++;
                }
                var layer = ControllerUtil.GetLayerByName(ref controller, layername);
                var statemachine = layer.stateMachine;
                blendshapeCategory[i].layer = layer;
                ControllerUtil.SetLayerWeight(controller, layer, 1);

                if (blendshapeCategory[i].menuControl == ControlType.Toggle)
                {
                    if (ControllerUtil.GetParameterByName(controller, parametername) == null) 
                    {
                        controller.AddParameter(parametername, AnimatorControllerParameterType.Bool);
                        ControllerUtil.TurnOnParameterBool(ref controller, parametername);
                    }

                    if (expressionParametersMenu.FindParameter(parametername) == null)
                        VRCUtil.AddNewParameter(expressionParametersMenu, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool, 0, parametername);
                    
                    for (int y = 0; y < clipcount; y++)
                    {
                        var statename = clips[y].name;
                                                                                    
                        if (blendshapeCategory[i].states.Count() != 2)
                        {
                            states[y] = statemachine.AddState(statename, new Vector3(360, y * 55));
                            statemachine.states.Where(x => x.state == states[y]).ToList()[0].state.motion = AnimUtil.LoadAnimClip(clips[y].name, $"Blendshapes/{blendshapeCategory[i].name}");
                            statesCompleted++;
                            blendshapeCategory[i].states.Add(states[y]);
                        }

                        if (blendshapeCategory[i].states.Count() == 2)
                        {
                            //Creates a transition that will start from the first state to the second state
                            layer.stateMachine.states[0].state.AddTransition(layer.stateMachine.states[1].state);
                            ControllerUtil.ApplyTransitionSettings(ref layer.stateMachine.states[0].state.transitions[0], false, 0, false, 0);
                            layer.stateMachine.states[0].state.transitions[0].AddCondition(AnimatorConditionMode.IfNot, 1, parametername);

                            //Creates a transition that will start from the second state to the first state
                            layer.stateMachine.states[1].state.AddTransition(layer.stateMachine.states[0].state);
                            ControllerUtil.ApplyTransitionSettings(ref layer.stateMachine.states[1].state.transitions[0], false, 0, false, 0);
                            layer.stateMachine.states[1].state.transitions[0].AddCondition(AnimatorConditionMode.If, 1, parametername);
                        }
                    }                                   
                }
                             
                else if (blendshapeCategory[i].menuControl == ControlType.RadialPuppet)
                {
                    if (ControllerUtil.GetParameterByName(controller, parametername) == null) 
                        controller.AddParameter(parametername, AnimatorControllerParameterType.Float);

                    if (expressionParametersMenu.FindParameter(parametername) == null)
                        VRCUtil.AddNewParameter(expressionParametersMenu, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float, 0, parametername);

                    var blendtree = new BlendTree();
                    blendtree.name = $"{layername} Blendtree";
                    blendtree.blendParameter = parametername;
                    blendtree.blendType = BlendTreeType.Simple1D;
                    blendtree.AddChild(AnimUtil.LoadAnimClip(clips.Where(x => x.name.Contains("OFF")).ToList()[0].name, $"Blendshapes/{blendshapeCategory[i].name}"));
                    blendtree.AddChild(AnimUtil.LoadAnimClip(clips.Where(x => x.name.Contains("ON")).ToList()[0].name, $"Blendshapes/{blendshapeCategory[i].name}"));

                    layer.stateMachine.AddState(layername);
                    layer.stateMachine.states.Last().state.motion = blendtree;
                    AssetDatabase.AddObjectToAsset(blendtree, layer.stateMachine.states.Last().state);
                    statesCompleted++;
                }
            }       
        }

        public static void CreateMenus(ref List<Category> matCategories, ref List<Category> objCategories, ref List<Category> blendCategories)
        {
            var controller = EZAvatar.controller;
            var expressionsMenu = EZAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionsMenu;

            VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu TogglesMainMenu = null;
            VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu ColorsMainMenu = null;
            VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu CustomizationMainMenu = null;

            var mCategoryCount = matCategories.Count();
            var oCategoryCount = objCategories.Count();
            var bCategoryCount = blendCategories.Count();

            //Creates directory for menus
            if (!Directory.Exists($"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus"))
            {
                Directory.CreateDirectory($"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus");
                if (EZAvatar.enableUnityDebugLogs)
                    Debug.Log($"<color=green>[EZAvatar]</color>: Created directory 'Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus'");
                AssetDatabase.Refresh();
            }
                              
            //If an toggles/colors/customization menu already exists for this avatar, we load that menu and add to it.
            //Otherwise we create new menus.
            if (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Toggles.asset") && oCategoryCount > 0)
            {
                TogglesMainMenu = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Toggles.asset", typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu));
                EditorUtility.SetDirty(TogglesMainMenu);
            }

            else if (oCategoryCount > 0)
            {
                TogglesMainMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                TogglesMainMenu.name = "Toggles";
                AssetDatabase.CreateAsset(TogglesMainMenu, $"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Toggles.asset");
                if (EZAvatar.enableUnityDebugLogs)
                    Debug.Log($"<color=green>[EZAvatar]</color>: Created main menu for toggles at Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Toggles.asset");
                EditorUtility.SetDirty(TogglesMainMenu);
                menusCompleted++;
            }

            if (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Colors.asset") && mCategoryCount > 0)
            {
                ColorsMainMenu = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Colors.asset", typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu));
                EditorUtility.SetDirty(ColorsMainMenu);
            }

            else if (mCategoryCount > 0)
            {
                ColorsMainMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                ColorsMainMenu.name = "Colors";
                AssetDatabase.CreateAsset(ColorsMainMenu, $"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Colors.asset");
                if (EZAvatar.enableUnityDebugLogs)
                    Debug.Log($"<color=green>[EZAvatar]</color>: Created main menu for material swaps at Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Colors.asset");
                EditorUtility.SetDirty(ColorsMainMenu);
                menusCompleted++;
            }

            if (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Customization.asset") && bCategoryCount > 0)
            {
                CustomizationMainMenu = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Customization.asset", typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu));
                EditorUtility.SetDirty(CustomizationMainMenu);
            }

            else if (bCategoryCount > 0)
            {
                CustomizationMainMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                CustomizationMainMenu.name = "Customization";
                AssetDatabase.CreateAsset(CustomizationMainMenu, $"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Customization.asset");
                if (EZAvatar.enableUnityDebugLogs)
                    Debug.Log($"<color=green>[EZAvatar]</color>: Created main menu for blendshapes at Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Customization.asset");
                EditorUtility.SetDirty(CustomizationMainMenu);
                menusCompleted++;
            }

            //Creates expression menu and add it to the avatar descriptor if it is missing
            if (expressionsMenu == null)
            {
                var newExMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                newExMenu.name = $"{EZAvatar.avatar.name}Main";

                AssetDatabase.CreateAsset(newExMenu, $"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/{newExMenu.name}.asset");
                EZAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionsMenu = newExMenu;
                expressionsMenu = newExMenu;

                if (EZAvatar.enableUnityDebugLogs)
                    Debug.Log($"<color=green>[EZAvatar]</color>: Avatar is missing expressions menu, created a new expressions menu at Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/{newExMenu.name}.asset");
                menusCompleted++;

            }

            //Add these newly created menus i.e: toggles/colors/customization(blendshapes) to the main menu if they are not already present
            if (expressionsMenu.controls.Find(x => x.name == ColorsMainMenu?.name) == null && mCategoryCount > 0)
            {
                expressionsMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                {
                    name = ColorsMainMenu.name,
                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = ColorsMainMenu
                });
            }

            if (expressionsMenu.controls.Find(x => x.name == TogglesMainMenu?.name) == null && oCategoryCount > 0)
            {
                expressionsMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                {
                    name = TogglesMainMenu.name,
                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = TogglesMainMenu
                });
            }

            if (expressionsMenu.controls.Find(x => x.name == CustomizationMainMenu?.name) == null && bCategoryCount > 0)
            {
                expressionsMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                {
                    name = CustomizationMainMenu.name,
                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = CustomizationMainMenu
                });
            }

            EditorUtility.SetDirty(expressionsMenu);
                                
            if (!EZAvatar.disableMenuCreation)
            {
                //Creates menus for gameobjects
                if (oCategoryCount > 0)
                    GenerateMenus(ref oCategoryCount, ref objCategories, ref TogglesMainMenu, EZAvatar.CreationType.GameObject);
             
                //Creates menus for materials 
                if (mCategoryCount > 0)
                    GenerateMenus(ref mCategoryCount, ref matCategories, ref ColorsMainMenu, EZAvatar.CreationType.Material);
                
                //Creates menus for blendshapes
                if (bCategoryCount > 0)
                    GenerateMenus(ref bCategoryCount, ref blendCategories, ref CustomizationMainMenu, EZAvatar.CreationType.Blendshape);
            }
        }

        public static void GenerateMenus(ref int categoryCount, ref List<Category> category, ref VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu mainMenu, EZAvatar.CreationType type)
        {           
            for (int i = 0; i < categoryCount; i++)
            {
                if (category[i].layer == null) {
                    Debug.LogWarning("<color=yellow>[EZAvatar]</color>: Unable to generate or add on to menus.");
                    return;
                }  
                var currentMain = (category[i].menuGenerationType == MenuGenOption.AddToSelectedMenu && category[i].menu != null) ? category[i].menu : mainMenu;
                var currlayername = category[i].layer.name;
                var newStates = category[i].states;
                
                var totalStatesCount = category[i].layer.stateMachine.states.Count();
                var newStatesCount = newStates.Count();

                string parameterName = (type == EZAvatar.CreationType.Material) ? $"{currlayername}Mat" : $"{currlayername}";             
                string toggleControlName = (type == EZAvatar.CreationType.Material) ? $"{currlayername}" : (((type == EZAvatar.CreationType.Blendshape && category[i].menuControl == ControlType.Toggle || type == EZAvatar.CreationType.GameObject) == true) ? currlayername.Substring(7) : currlayername);   

                var currentMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                currentMenu.name = category[i].name;

                bool isLoaded = false;

                if (Helper.DoesMenuExist(currentMenu.name, true) || Helper.DoesMenuExist(currentMenu.name, false))
                {
                    currentMenu = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{currentMenu.name}.asset", typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)) != null ? (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{currentMenu.name}.asset", typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)) : (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/{currentMenu.name}.asset", typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu));
                    //Get latest nested menu
                    while (currentMenu.controls.Last().subMenu != null)
                    {
                        currentMenu = currentMenu.controls.Last().subMenu;
                    }
                    isLoaded = true;
                    //If we switched from bool parameter with 2 toggles to int, we will redo the menu as to include the prior states as toggles
                    if (category[i].switched == true && currentMenu.controls.Count() == 1)
                    {
                        currentMenu.controls.Clear();
                    }
                }

                if (category[i].menuGenerationType == MenuGenOption.AddToSelectedMenu) { EditorUtility.SetDirty(currentMain); }
                
                EditorUtility.SetDirty(currentMenu);
                EditorUtility.SetDirty(currentMain);

                creationstart:

                //If the main menu reaches 8 control limit and there are more layers to go through, we create a new main menu to continue iterating
                if (currentMain.controls.Count() == 8 && i + 1 <= categoryCount)
                {
                    var nextmain = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                    EditorUtility.SetDirty(nextmain);
                    var namecount = 0;

                    var currentMainName = currentMain.name.Contains("More") == true ? currentMain.name.Substring(0, currentMain.name.LastIndexOf('M')) : currentMain.name;

                    while (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{currentMainName}More{namecount}.asset") != false)
                    {
                        namecount++;                      
                    }

                    nextmain.name = currentMain.name.Contains("More") == true ? nextmain.name = $"{currentMain.name.Substring(0, currentMain.name.LastIndexOf('M'))}More{namecount}" : $"{currentMain.name}More{namecount}";

                    AssetDatabase.CreateAsset(nextmain, $"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{nextmain.name}.asset");
                    if (EZAvatar.enableUnityDebugLogs)
                        Debug.Log($"<color=green>[EZAvatar]</color>: Created a new menu page for {currentMain.name} at Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{nextmain.name}.asset");
                    menusCompleted++;
                    
                    currentMain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                    {
                        name = "More",
                        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = nextmain
                    });

                    if (!AssetDatabase.Contains(currentMain))
                    {
                        AssetDatabase.CreateAsset(currentMain, $"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/{currentMain.name}.asset");
                        if (EZAvatar.enableUnityDebugLogs)
                            Debug.Log($"<color=green>[EZAvatar]</color>: Created {currentMain.name} menu at Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/{currentMain.name}.asset");
                        menusCompleted++;
                    }

                    if (currentMenu.controls.Count() > 0)
                    {
                        nextmain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = currentMenu.name,
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                            subMenu = currentMenu
                        });
                    }
                 
                    currentMain = nextmain;
                }
                //Fetch last nested main menu 
                else if (currentMain.controls.Count() == 9 && currentMain.controls.Where(x => x.name == "More") != null)
                {
                    while (currentMain.controls.Where(x => x.name == "More") != null)
                    {
                        currentMain = currentMain.controls.Where(x => x.name == "More").ToList()[0].subMenu;
                        goto creationstart;
                    }
                }

                if (category[i].menuControl == ControlType.RadialPuppet)
                {
                    if (currentMain.controls.Where(x => x.name == toggleControlName && x.parameter.name == parameterName && x.type == VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.RadialPuppet) == null)
                    {
                        currentMain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = toggleControlName,
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.RadialPuppet,
                            parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = parameterName },
                            value = 0
                        });
                    }

                    continue;
                }

                //If the toggle is a bool instead of int, we don't need to iterate through each state, we just need to make one control toggle
                else if (type == EZAvatar.CreationType.Material && ControllerUtil.GetParameterByName(EZAvatar.controller, parameterName).type == AnimatorControllerParameterType.Bool)
                {                  
                    currentMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                    {
                        name = category[i].states.First().name,
                        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                        parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = parameterName },
                        value = 1
                    });

                    if (!AssetDatabase.Contains(currentMenu))
                    {
                        AssetDatabase.CreateAsset(currentMenu, $"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/{currentMenu.name}.asset");
                        if (EZAvatar.enableUnityDebugLogs)
                            Debug.Log($"<color=green>[EZAvatar]</color>: Created {currentMenu.name} menu at Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{currentMenu.name}.asset");
                        menusCompleted++;
                    }
                    
                    currentMain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                    {
                        name = currentMenu.name,
                        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = currentMenu
                    });
              
                    //Skip to next category
                    continue;
                }

                else if (type == EZAvatar.CreationType.GameObject || type == EZAvatar.CreationType.Blendshape && ControllerUtil.GetParameterByName(EZAvatar.controller, parameterName).type == AnimatorControllerParameterType.Bool)
                {                  
                    currentMain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                    {
                        name = toggleControlName,
                        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                        parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = parameterName },
                        value = 1
                    });
              
                    //Skip to next category
                    continue;
                }
                
                for (int y = 0; y < newStatesCount; y++)
                {
                    if (type == EZAvatar.CreationType.GameObject && newStates[y].name.Equals("Toggles Idle"))
                        continue;

                    //Add new control per state in the current layer, until we reach 8 controls, in which the last one will be an additional menu for further iteration
                    if (currentMenu.controls.Count() < 8)
                    {                          
                        var control = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = (type == EZAvatar.CreationType.Material) == true ? $"{newStates[y].name}" : newStates[y].name.Substring(0, newStates[y].name.LastIndexOf('O')).ToString(),
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                            parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = parameterName },
                            value = isLoaded ? totalStatesCount - newStatesCount + y : y
                        };

                        currentMenu.controls.Add(control);
                        
                        //If there are no more states to iterate through for this category, that means the menu is finished, and we should export it and add to the main menu.
                        if (y == newStates.Count() - 1 && !currentMenu.name.Contains("More"))
                        {
                            var maincontrol = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                            {
                                name = currentMenu.name,
                                type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                                subMenu = currentMenu
                            };

                            if (currentMain.controls.Any(x => x.name == currentMenu.name) != true)
                                currentMain.controls.Add(maincontrol);

                            if (!AssetDatabase.Contains(currentMenu))
                            {
                                AssetDatabase.CreateAsset(currentMenu, $"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/{currentMenu.name}.asset");
                                if (EZAvatar.enableUnityDebugLogs)
                                    Debug.Log($"<color=green>[EZAvatar]</color>: Created {currentMenu.name} menu at Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{currentMenu.name}.asset");
                                menusCompleted++;
                            }

                        }

                    }

                    //If we reach the end of the current menu and there are still more states to consider, create a new menu
                    else if (currentMenu.controls.Count() == 8 && y + 1 <= newStates.Count())
                    {
                        var nextMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                        var namecount = 0;

                        while (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{currentMain.name}More{namecount}.asset") != false)
                        {
                            namecount++;
                        }

                        nextMenu.name = $"{currlayername}More{namecount}";

                        if (!AssetDatabase.Contains(nextMenu))
                        {
                            AssetDatabase.CreateAsset(nextMenu, $"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{nextMenu.name}.asset");
                            if (EZAvatar.enableUnityDebugLogs)
                                Debug.Log($"<color=green>[EZAvatar]</color>: Created a new menu page for {currentMenu.name} at Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{nextMenu.name}.asset");
                            menusCompleted++;
                        }
                    
                        currentMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = "More",
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                            subMenu = nextMenu
                        });

                        //Export our current menu
                        if (!AssetDatabase.Contains(currentMenu))
                        {
                            AssetDatabase.CreateAsset(currentMenu, $"Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{currentMenu.name}.asset");
                            if (EZAvatar.enableUnityDebugLogs)
                                Debug.Log($"<color=green>[EZAvatar]</color>: Created {currentMenu.name} menu at Assets/Nin/EZAvatar/{EZAvatar.avatar.name}/Menus/Submenus/{currentMenu.name}.asset");
                            menusCompleted++;
                        }

                        //Add this current full menu to the main menu
                        currentMain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = currentMenu.name,
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                            subMenu = currentMenu
                        });

                        AssetDatabase.SaveAssets();

                        currentMenu = nextMenu;
                    }
                }
            }
        }
    }
}

#endif
#endif