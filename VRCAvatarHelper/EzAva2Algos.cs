using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EZAvatar
{
    public class Algorithm
    {
        public static int layersCompleted = 0;
        public static int statesCompleted = 0;

        public static void SetupMaterialToggles()
        {
            var controller = EzAvatar.controller;
            var expressionParametersMenu = EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters;

            foreach (var category in EzAvatar.categories.Where(x => x.type == CategoryType.Material))
            {
                //Bool to make boolean transition logic
                bool isAdded = false;
                bool cleared = false;
                var layername = category.name;
                var clips = category.animClips;
                int clipcount = category.animClips.Count;
                var layer = new AnimatorControllerLayer();
                AnimatorState[] states = new AnimatorState[clipcount];

                EzAvatar.debug = $"Found {clipcount} animation clips for category {layername}...";
                Debug.Log(EzAvatar.debug);

                //Ignore Previous States bool is used as a setting, which decides if we will delete other states or not.

                for (int i = 0; i < clipcount; i++)
                {
                    var statename = clips[i].name;
                    var parametername = layername + "Mat";

                    //If there are more than two animation clips, we will use any state transitions
                    if (clipcount > 2)
                    {
                        //Creates new layer in the animator, if it doesn't exist
                        if (ControllerUtil.GetLayerByName(controller, layername) == null)
                        {
                            controller.AddLayer(layername);
                            layersCompleted++;
                        }
                        //Retrieves the layer that has just been created
                        layer = ControllerUtil.GetLayerByName(controller, layername);
                        category.layer = layer;
                        //Sets the layer weight to 1
                        ControllerUtil.SetLayerWeight(controller, layer, 1);
                        var statemachine = layer.stateMachine;
                        //Removes states if we are not ignoring previous states
                        if (!EzAvatar.ignorePreviousStates && !cleared)
                        {
                            ControllerUtil.RemoveStates(layer);
                            cleared = true;
                        }
                        //Only adds the parameter if it does not already exist 
                        if (ControllerUtil.GetParameterByName(controller, parametername) == null)
                            controller.AddParameter(parametername, AnimatorControllerParameterType.Int);
                        //Adds parameter to expressions menu if it doesn't exist
                        if (expressionParametersMenu.FindParameter(parametername) == null)
                            VRCUtil.AddNewParameter(expressionParametersMenu, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Int, 0, parametername);
                        //Creates a state in the layer if it does not already exist, along with transitions, and inserts clip as the state motion.
                        if (ControllerUtil.GetAnimatorStateInLayer(layer, statename) == null)
                        {
                            states[i] = statemachine.AddState(statename, new Vector3(360, i * 55));
                            states[i].motion = clips[i];
                            statesCompleted++;
                            //Creates any state transition to each state
                            var anyStateTransition = statemachine.AddAnyStateTransition(states[i]);
                            //Applies transition settings
                            ControllerUtil.ApplyTransitionSettings(anyStateTransition, false, 0, false, 0);
                            //Adds condition to newly created transition
                            anyStateTransition.AddCondition(AnimatorConditionMode.Equals, i, parametername);
                        }
                    }

                    //If there are only 2 animation clips, we will optimize the transitions and opt to use a bool amongst them instead, and transition from on to off.
                    //The logic here is different because two cases can happen: if we are not ignoring previous states, and we are simply adding in 1 or 2 anims as states to the controller,
                    //Then we must continue adding in any state transitions and change existing parameters to an int if they are a boolean. If we are creating the layer from scratch, then we must switch our logic to
                    //Use a boolean and transitions between each other.
                    else if (clipcount <= 2)
                    {
                        int increment = 0;
                        bool switchToInt = false;

                        if (ControllerUtil.GetLayerByName(controller, layername) == null)
                        {
                            controller.AddLayer(layername);
                            layersCompleted++;
                        }
                        layer = ControllerUtil.GetLayerByName(controller, layername);
                        category.layer = layer;
                        ControllerUtil.SetLayerWeight(controller, layer, 1);
                        var statemachine = layer.stateMachine;
                        //Removes states if we are not ignoring previous states
                        if (!EzAvatar.ignorePreviousStates && !cleared)
                        {
                            ControllerUtil.RemoveStates(layer);
                            cleared = true;
                        }
                        //If we are have a layer that already has 2 states with on and off logic, and we are adding to that layer, we need to change the parameter to int
                        if (EzAvatar.ignorePreviousStates)
                        {
                            if (statemachine.states.Count() >= 2 && ControllerUtil.GetParameterByName(controller, parametername).type == AnimatorControllerParameterType.Bool)
                            {
                                ControllerUtil.ChangeParameterToInt(controller, layer, expressionParametersMenu, parametername);
                                switchToInt = true;
                            }
                        }

                        if (ControllerUtil.GetParameterByName(controller, parametername) == null)
                        {
                            //Adds bool parameter if there are only two anims we are working with
                            controller.AddParameter(parametername, AnimatorControllerParameterType.Bool);
                            ControllerUtil.TurnOnParameterBool(controller, parametername);
                        }

                        //Adds new parameter to expressions menu if missing
                        if (expressionParametersMenu.FindParameter(parametername) == null)
                            VRCUtil.AddNewParameter(expressionParametersMenu, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool, 0, parametername);

                        if (ControllerUtil.GetAnimatorStateInLayer(layer, statename) == null && statemachine.states.Count() < 2)
                        {
                            states[i] = statemachine.AddState(statename, new Vector3(360, i * 55));
                            states[i].motion = clips[i];
                            statesCompleted++;
                        }

                        else if (ControllerUtil.GetAnimatorStateInLayer(layer, statename) == null && statemachine.states.Count() >= 2)
                        {
                            int switchToIntIncrementor = 0;
                            if (switchToInt)
                            {
                                states[i] = statemachine.AddState(statename, new Vector3(360, (statemachine.states.Count() + switchToIntIncrementor) * 55));
                                switchToIntIncrementor++;
                                statesCompleted++;
                                states[i].motion = clips[i];
                                increment = statemachine.states.Count() - 1;
                                //Creates any state transition to each state
                                var anyStateTransition = statemachine.AddAnyStateTransition(states[i]);
                                //Applies transition settings
                                ControllerUtil.ApplyTransitionSettings(anyStateTransition, false, 0, false, 0);
                                //Adds condition to newly created transition, we use state count for the value because we are adding on to the existing number of states
                                anyStateTransition.AddCondition(AnimatorConditionMode.Equals, increment, parametername);
                                isAdded = true;
                                increment++;
                            }
                            else
                                states[i] = statemachine.AddState(statename, new Vector3(360, i * 55));
                        }


                        //When both states have been created, and we are creating them from scratch in a new layer that lacks states
                        if (states.Count() == clipcount && clipcount == 2 && i == clipcount - 1)
                        {
                            //Checks to see if we are creating from scratch or keeping existing states. If from scratch, and we are working with 2 states, use bool logic.
                            //(One transition from off to on, and on to off.)
                            if (isAdded == false)
                            {
                                try
                                {
                                    //Creates a transition that will start from the first state to the second state
                                    AnimatorStateTransition idleToOnTransition = new AnimatorStateTransition();
                                    idleToOnTransition.destinationState = statemachine.states[1].state;
                                    ControllerUtil.ApplyTransitionSettings(idleToOnTransition, false, 0, false, 0);
                                    idleToOnTransition.AddCondition(AnimatorConditionMode.If, 1, parametername);
                                    statemachine.states[0].state.AddTransition(idleToOnTransition);

                                    //Creates a transition that will start from the second state to the first state
                                    AnimatorStateTransition onToIdleTransition = new AnimatorStateTransition();
                                    onToIdleTransition.destinationState = statemachine.states[0].state;
                                    ControllerUtil.ApplyTransitionSettings(onToIdleTransition, false, 0, false, 0);
                                    onToIdleTransition.AddCondition(AnimatorConditionMode.IfNot, 1, parametername);
                                    statemachine.states[1].state.AddTransition(onToIdleTransition);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
        }

        public static void SetupGameObjectToggles()
        {
            var controller = EzAvatar.controller;
            var expressionParametersMenu = EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters;

            foreach (var category in EzAvatar.categories.Where(x => x.type == CategoryType.GameObject))
            {
                var layername = $"Toggle {category.name}";
                var clips = category.animClips;
                var clipcount = clips.Count();
                var layer = new AnimatorControllerLayer();
                AnimatorState[] states = new AnimatorState[clipcount];

                EzAvatar.debug = $"Found {clipcount} animation clips for category {layername}...";
                Debug.Log(EzAvatar.debug);

                var cleared = false;

                for (int i = 0; i < clipcount; i++)
                {
                    var statename = clips[i].name;
                    var parametername = layername;

                    if (ControllerUtil.GetLayerByName(controller, layername) == null)
                    {
                        controller.AddLayer(layername);
                        layersCompleted++;
                    }

                    layer = ControllerUtil.GetLayerByName(controller, layername);
                    category.layer = layer;
                    ControllerUtil.SetLayerWeight(controller, layer, 1);
                    var statemachine = layer.stateMachine;

                    //Removes states if we are not ignoring previous states
                    if (!EzAvatar.ignorePreviousStates && !cleared)
                    {
                        ControllerUtil.RemoveStates(layer);
                        cleared = true;
                    }

                    //Adds bool parameter if there are only two anims we are working with
                    if (ControllerUtil.GetParameterByName(controller, parametername) == null)
                    {

                        controller.AddParameter(parametername, AnimatorControllerParameterType.Bool);
                        ControllerUtil.TurnOnParameterBool(controller, parametername);
                    }

                    //Adds new parameter to expressions menu if missing
                    if (expressionParametersMenu.FindParameter(parametername) == null)
                        VRCUtil.AddNewParameter(expressionParametersMenu, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool, 0, parametername);

                    states[i] = statemachine.AddState(statename, new Vector3(360, i * 55));
                    statesCompleted++;

                    //When both states have been created
                    if (states.Count() == 2 && i == clipcount - 1)
                    {
                        try
                        {
                            //Creates a transition that will start from the first state to the second state
                            AnimatorStateTransition onToOffTransition = new AnimatorStateTransition();
                            onToOffTransition.destinationState = statemachine.states[1].state;
                            ControllerUtil.ApplyTransitionSettings(onToOffTransition, false, 0, false, 0);
                            onToOffTransition.AddCondition(AnimatorConditionMode.IfNot, 1, parametername);
                            statemachine.states[0].state.AddTransition(onToOffTransition);
                            statemachine.states[0].state.motion = clips.Where(x => x.name.EndsWith("ON")).First();

                            //Creates a transition that will start from the second state to the first state
                            AnimatorStateTransition offToOnTransition = new AnimatorStateTransition();
                            offToOnTransition.destinationState = statemachine.states[0].state;
                            ControllerUtil.ApplyTransitionSettings(offToOnTransition, false, 0, false, 0);
                            offToOnTransition.AddCondition(AnimatorConditionMode.If, 1, parametername);
                            statemachine.states[1].state.AddTransition(offToOnTransition);
                            statemachine.states[1].state.motion = clips.Where(x => x.name.EndsWith("OFF")).First();
                        }
                        catch { }
                    }
                }
            }
        }

        public static void CreateMenus()
        {
            var controller = EzAvatar.controller;
            var expressionsMenu = EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionsMenu;

            VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu AccessoriesMainMenu = null;
            VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu ColorsMainMenu = null;

            //If an accessories/colors menu already exists for this avatar, we load that menu and add to it.
            //Otherwise we create new menus.
            if (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Accessories.asset"))
                AccessoriesMainMenu = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Accessories.asset", typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu));

            else
            {
                AccessoriesMainMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                AccessoriesMainMenu.name = "Accessories";
            }

            if (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Colors.asset"))
                ColorsMainMenu = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Colors.asset", typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu));

            else
            {
                ColorsMainMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                ColorsMainMenu.name = "Colors";
            }

            var ExtraMenus = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu[1];

            if (!Directory.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus"))
            {
                Directory.CreateDirectory($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus");
                AssetDatabase.Refresh();
            }

            if (expressionsMenu == null)
            {
                var newExMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                newExMenu.name = $"{EzAvatar.avatar.name}Main";

                AssetDatabase.CreateAsset(newExMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{newExMenu.name}.asset");
                EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionsMenu = newExMenu;

                EzAvatar.debug = "Missing expressions menu, created a new expressions menu...";
                Debug.Log(EzAvatar.debug);
            }

            // var layers = EzAvatar.selectedLayers;
            if (EzAvatar.autoCreateMenus)
            {
                //Count for extra menus
                var index = 0;
                var nextColorMenuName = "ColorsMore";
                var colornamecount = 0;
                var nextAccessoryMenuName = $"AccessoriesMore";
                var accnamecount = 0;
                var objCategoryCount = 0;
                var colCategoryCount = 0;

                foreach (var category in EzAvatar.categories)
                {
                    var currlayername = category.layer.name;
                    var states = category.layer.stateMachine.states;
                    //Creates menus for toggles 
                    if (category.type == CategoryType.GameObject)
                    {
                        var currentMenu = AccessoriesMainMenu;

                        if (currentMenu.controls.Count() < 7)
                        {
                            var controlname = currlayername.Substring(7);
                            currentMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                            {
                                name = controlname,
                                type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                                parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = currlayername },
                                value = 1
                            });
                            objCategoryCount++;
                            if (objCategoryCount == EzAvatar.categories.Where(x => x.type == CategoryType.GameObject).Count())
                            {
                                AssetDatabase.CreateAsset(currentMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{currentMenu.name}.asset");
                                AssetDatabase.Refresh();
                            }
                        }

                        //Once we reach 7 controls, we will create a new page to store additional toggles
                        else if (currentMenu.controls.Count() == 7 && objCategoryCount < EzAvatar.categories.Where(x => x.type == CategoryType.GameObject).Count())
                        {
                            ExtraMenus[index] = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                            ExtraMenus[index].name = nextAccessoryMenuName;

                            index++;
                            Array.Resize(ref ExtraMenus, index + 1);
                            nextAccessoryMenuName = $"AccessoriesMore{accnamecount}";
                            accnamecount++;
                            AssetDatabase.CreateAsset(ExtraMenus[index - 1], $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus/{ExtraMenus[index - 1].name}.asset");

                            currentMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                            {
                                name = "More",
                                type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                                subMenu = ExtraMenus[index - 1]
                            });
                            AssetDatabase.CreateAsset(currentMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{currentMenu.name}.asset");
                            currentMenu = ExtraMenus[index - 1];
                        }
                    }
                    //Creates menus for materials 
                    else if (category.type == CategoryType.Material)
                    {
                        var currentMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                        currentMenu.name = category.name;
                        int statecount = 0;

                        foreach (var state in states)
                        {

                            if (currentMenu.controls.Count() < 7)
                            {
                                currentMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                                {
                                    name = $"{state.state.name}",
                                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                                    parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = $"{currlayername}Mat" },
                                    value = statecount
                                });
                                statecount++;

                                //If there are no more states to iterate through for this category, that means the menu is finished, and we should export it and add to the main menu.
                                if (statecount == states.Count())
                                {
                                    AssetDatabase.CreateAsset(currentMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{currentMenu.name}.asset");
                                    AssetDatabase.Refresh();
                                    ColorsMainMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                                    {
                                        name = currentMenu.name,
                                        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                                        subMenu = currentMenu
                                    });
                                }

                            }

                            //If we reach the end of the current menu and there are still more states to consider
                            else if (currentMenu.controls.Count() == 7 && statecount < states.Count())
                            {
                                ExtraMenus[index] = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                                ExtraMenus[index].name = nextColorMenuName;

                                index++;
                                Array.Resize(ref ExtraMenus, index + 1);
                                nextColorMenuName = $"{currlayername}More{colornamecount}";
                                colornamecount++;
                                AssetDatabase.CreateAsset(ExtraMenus[index - 1], $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus/{ExtraMenus[index - 1].name}.asset");

                                currentMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                                {
                                    name = "More",
                                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                                    subMenu = ExtraMenus[index - 1]
                                });
                                //Export our current menu
                                AssetDatabase.CreateAsset(currentMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{currentMenu.name}.asset");
                                //Add this current full menu to the main menu
                                ColorsMainMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                                {
                                    name = currentMenu.name,
                                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                                    subMenu = currentMenu
                                });

                                currentMenu = ExtraMenus[index - 1];
                            }

                            if (ColorsMainMenu.controls.Count() == 7 && ColorsMainMenu.controls.Count() < EzAvatar.categories.Where(x => x.type == CategoryType.Material).Count())
                            {
                                var nextmain = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                                nextmain.name = "ColorsMore";
                                ColorsMainMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                                {
                                    name = "More",
                                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                                    subMenu = nextmain
                                });
                                AssetDatabase.CreateAsset(ColorsMainMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{ColorsMainMenu.name}.asset");
                                ColorsMainMenu = nextmain;
                            }
                        }

                        colCategoryCount++;
                        if (colCategoryCount == EzAvatar.categories.Where(x => x.type == CategoryType.Material).Count())
                            AssetDatabase.CreateAsset(ColorsMainMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{ColorsMainMenu.name}.asset");
                    }
                }

                //Add new menus to the main menu if they are not already present
                if (expressionsMenu.controls.Where(x => x.subMenu == ColorsMainMenu) != null)
                {
                    expressionsMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                    {
                        name = ColorsMainMenu.name,
                        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = ColorsMainMenu
                    });
                }

                if (expressionsMenu.controls.Where(x => x.subMenu == AccessoriesMainMenu) != null)
                {
                    expressionsMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                    {
                        name = AccessoriesMainMenu.name,
                        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = AccessoriesMainMenu
                    });
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}
