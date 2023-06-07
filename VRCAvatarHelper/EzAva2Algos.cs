#if UNITY_EDITOR

using System;
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
        public static int menusCompleted = 0;
        public static double elaspedTime = 0;
        
        public static void SetupMaterialToggles(ref List<Category> matCategories)
        {
            var controller = EzAvatar.controller;
            var expressionParametersMenu = EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters;
            //Create parameters menu if it doesn't exist
            if (expressionParametersMenu == null) {
                var newParametersMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>();
                newParametersMenu.name = $"{EzAvatar.avatar.name}Parameters";
                if (!Directory.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}"))
                    Directory.CreateDirectory($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}");
                AssetDatabase.CreateAsset(newParametersMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/{newParametersMenu.name}.asset");
                EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters = newParametersMenu;
                AssetDatabase.SaveAssets();
            }

            for (int i = 0; i < matCategories.Count; i++)
            {
                //Bool to make boolean transition logic
                bool isAdded = false;
                bool cleared = false;
                var layername = matCategories[i].name;
                var clips = matCategories[i].animClips;
                int clipcount = matCategories[i].animClips.Count;
                var layer = new AnimatorControllerLayer();
                AnimatorState[] states = new AnimatorState[clipcount];

                EzAvatar.debug = $"Found {clipcount} animation clips for category {layername}...";
                Debug.Log(EzAvatar.debug);

                //Ignore Previous States bool is used as a setting, which decides if we will delete other states or not.

                for (int y = 0; y < clipcount; y++)
                {
                    var statename = clips[y].name;
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
                        matCategories[i].layer = layer;
                        //Sets the layer weight to 1
                        ControllerUtil.SetLayerWeight(controller, layer, 1);
                        var statemachine = layer.stateMachine;
                        //Removes states if we are not ignoring previous states
                        if (!EzAvatar.ignorePreviousStates && !cleared) {
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
                            states[y] = statemachine.AddState(statename, new Vector3(360, y * 55));
                            states[y].motion = clips[y];
                            statesCompleted++;
                            //Creates any state transition to each state
                            var anyStateTransition = statemachine.AddAnyStateTransition(states[y]);
                            //Applies transition settings
                            ControllerUtil.ApplyTransitionSettings(anyStateTransition, false, 0, false, 0);
                            //Adds condition to newly created transition
                            anyStateTransition.AddCondition(AnimatorConditionMode.Equals, y, parametername);
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
                        matCategories[i].layer = layer;
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
                            states[y] = statemachine.AddState(statename, new Vector3(360, y * 55));
                            states[y].motion = clips[y];
                            statesCompleted++;
                        }

                        else if (ControllerUtil.GetAnimatorStateInLayer(layer, statename) == null && statemachine.states.Count() >= 2)
                        {
                            int switchToIntIncrementor = 0;
                            if (switchToInt)
                            {
                                states[y] = statemachine.AddState(statename, new Vector3(360, (statemachine.states.Count() + switchToIntIncrementor) * 55));
                                switchToIntIncrementor++;
                                statesCompleted++;
                                states[y].motion = clips[y];
                                increment = statemachine.states.Count() - 1;
                                //Creates any state transition to each state
                                var anyStateTransition = statemachine.AddAnyStateTransition(states[y]);
                                //Applies transition settings
                                ControllerUtil.ApplyTransitionSettings(anyStateTransition, false, 0, false, 0);
                                //Adds condition to newly created transition, we use state count for the value because we are adding on to the existing number of states
                                anyStateTransition.AddCondition(AnimatorConditionMode.Equals, increment, parametername);
                                isAdded = true;
                                increment++;
                            }
                            else
                                states[y] = statemachine.AddState(statename, new Vector3(360, y * 55));
                        }


                        //When both states have been created, and we are creating them from scratch in a new layer that lacks states
                        if (states.Count() == clipcount && clipcount == 2 && y == clipcount - 1)
                        {
                            //Checks to see if we are creating from scratch or keeping existing states. If from scratch, and we are working with 2 states, use bool logic.
                            //(One transition from off to on, and on to off.)
                            if (isAdded == false)
                            {
                                try
                                {
                                    //Creates a transition that will start from the first state to the second state
                                    AnimatorStateTransition onToOffTransition = new AnimatorStateTransition();
                                    onToOffTransition.destinationState = statemachine.states[1].state;
                                    ControllerUtil.ApplyTransitionSettings(onToOffTransition, false, 0, false, 0);
                                    onToOffTransition.AddCondition(AnimatorConditionMode.IfNot, 1, parametername);
                                    statemachine.states[0].state.AddTransition(onToOffTransition);

                                    //Creates a transition that will start from the second state to the first state
                                    AnimatorStateTransition offToOnTransition = new AnimatorStateTransition();
                                    offToOnTransition.destinationState = statemachine.states[0].state;
                                    ControllerUtil.ApplyTransitionSettings(offToOnTransition, false, 0, false, 0);
                                    offToOnTransition.AddCondition(AnimatorConditionMode.If, 1, parametername);
                                    statemachine.states[1].state.AddTransition(offToOnTransition);
                                }
                                catch { }
                            }
                        }
                    }
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void SetupGameObjectToggles(ref List<Category> objCategories)
        {
            var controller = EzAvatar.controller;
            var expressionParametersMenu = EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters;
            //Create parameters menu if it doesn't exist
            if (expressionParametersMenu == null) {
                var newParametersMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>();
                newParametersMenu.name = $"{EzAvatar.avatar.name}Parameters";
                if (!Directory.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}"))
                    Directory.CreateDirectory($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}");
                AssetDatabase.CreateAsset(newParametersMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/{newParametersMenu.name}.asset");
                EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters = newParametersMenu;
                AssetDatabase.SaveAssets();
            }

            for (int i = 0; i < objCategories.Count(); i++)
            {
                var layername = $"Toggle {objCategories[i].name}";
                var clips = objCategories[i].animClips;
                var clipcount = clips.Count();
                var layer = new AnimatorControllerLayer();
                AnimatorState[] states = new AnimatorState[clipcount];

                EzAvatar.debug = $"Found {clipcount} animation clips for category {layername}...";
                Debug.Log(EzAvatar.debug);

                var cleared = false;

                for (int y = 0; y < clipcount; y++)
                {
                    var statename = clips[y].name;
                    var parametername = layername;

                    if (ControllerUtil.GetLayerByName(controller, layername) == null)
                    {
                        controller.AddLayer(layername);
                        layersCompleted++;
                    }
                    
                    layer = ControllerUtil.GetLayerByName(controller, layername);
                    objCategories[i].layer = layer;
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
                 
                    states[y] = statemachine.AddState(statename, new Vector3(360, y * 55));
                    statesCompleted++;

                    //When both states have been created
                    if (states.Count() ==  2 && y == clipcount - 1)
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
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void CreateMenus(ref List<Category> matCategories, ref List<Category> objCategories)
        {
            var controller = EzAvatar.controller;
            var expressionsMenu = EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionsMenu;

            VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu AccessoriesMainMenu = null;
            VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu ColorsMainMenu = null;

            var mCategoryCount = matCategories.Count();
            var oCategoryCount = objCategories.Count();

            //Creates directory for menus
            if (!Directory.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus"))
            {
                Directory.CreateDirectory($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus");
                AssetDatabase.Refresh();
            }

            //If an accessories/colors menu already exists for this avatar, we load that menu and add to it.
            //Otherwise we create new menus.
            if (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Accessories.asset") && oCategoryCount > 0)
                AccessoriesMainMenu = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Accessories.asset", typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu));

            else
            {
                AccessoriesMainMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();          
                AccessoriesMainMenu.name = "Accessories";
                AssetDatabase.CreateAsset(AccessoriesMainMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Accessories.asset");
                menusCompleted++;
            }

            if (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Colors.asset") && mCategoryCount > 0)
                ColorsMainMenu = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Colors.asset", typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu));

            else
            {
                ColorsMainMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                ColorsMainMenu.name = "Colors";
                AssetDatabase.CreateAsset(ColorsMainMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Colors.asset");
                menusCompleted++;
            }

            //Instantiate array that will hold extra menus that are created
            var ExtraMenus = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu[1];
         
            //Creates expression menu and add it to the avatar descriptor if it is missing
            if (expressionsMenu == null)
            {
                var newExMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                newExMenu.name = $"{EzAvatar.avatar.name}Main";
                         
                AssetDatabase.CreateAsset(newExMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{newExMenu.name}.asset");
                EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionsMenu = newExMenu;
                expressionsMenu = newExMenu;
                
                EzAvatar.debug = "Missing expressions menu, created a new expressions menu...";
                Debug.Log(EzAvatar.debug);
                menusCompleted++;
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            //Add these newly created menus (accessory/colors) to the main menu if they are not already present
            if (expressionsMenu.controls.Find(x => x.name == ColorsMainMenu.name) == null && mCategoryCount > 0)
            {
                expressionsMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                {
                    name = ColorsMainMenu.name,
                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = ColorsMainMenu
                });

                AssetDatabase.SaveAssets();
            }

            if (expressionsMenu.controls.Find(x => x.name == AccessoriesMainMenu.name) == null && oCategoryCount > 0)
            {
                expressionsMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                {
                    name = AccessoriesMainMenu.name,
                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = AccessoriesMainMenu
                });

                AssetDatabase.SaveAssets();
            }

            if (EzAvatar.autoCreateMenus)
            {
                //Count for extra menus
                var index = 0;

                var currentAccessoryMain = AccessoriesMainMenu;

                for (int i = 0; i < oCategoryCount; i++)
                {
                    var currlayername = objCategories[i].layer.name;
                    var states = objCategories[i].layer.stateMachine.states;
                    var controlname = currlayername.Substring(7);
                    
                    objmenustart:

                    //Add toggle controls to the current menu until it reaches 8, in which the last control will be a new menu to continue iterating
                    if (currentAccessoryMain.controls.Count() < 8)
                    {
                        currentAccessoryMain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = controlname,
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                            parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = currlayername },
                            value = 1
                        });

                        AssetDatabase.SaveAssets();                    
                    }

                    //Once we reach 8 controls, we will create a new menu to store additional toggles
                    else if (currentAccessoryMain.controls.Count() == 8 && i + 1 <= oCategoryCount)
                    {
                        ExtraMenus[index] = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();                      
                        index++;
                        Array.Resize(ref ExtraMenus, index + 1);

                        var accMenuNameCount = 0;

                        while (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/AccessoriesMore{accMenuNameCount}.asset") != false){
                            accMenuNameCount++;
                        }

                        ExtraMenus[index - 1].name = $"AccessoriesMore{accMenuNameCount}";
                        AssetDatabase.CreateAsset(ExtraMenus[index - 1], $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{ExtraMenus[index - 1].name}.asset");
                        menusCompleted++;

                        currentAccessoryMain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = "More",
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                            subMenu = ExtraMenus[index - 1]
                        });

                        AssetDatabase.SaveAssets();
                        
                        if (!Helper.DoesMenuExist(currentAccessoryMain.name, false)) {
                            AssetDatabase.CreateAsset(currentAccessoryMain, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{currentAccessoryMain.name}.asset");
                            menusCompleted++;
                        }

                        currentAccessoryMain = ExtraMenus[index - 1];

                        currentAccessoryMain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = controlname,
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                            parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = currlayername },
                            value = 1
                        });
                    }
                    //Fetch last nested main menu if current main menu is full
                    else if (currentAccessoryMain.controls.Count() == 9 && currentAccessoryMain.controls.Last().subMenu != null)
                    {
                        while (currentAccessoryMain.controls.Last().subMenu != null)
                        {
                            currentAccessoryMain = currentAccessoryMain.controls.Last().subMenu;
                        }
                        goto objmenustart;
                    }
                    //When we have reached the end
                    if (i == oCategoryCount - 1)
                    {                       
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        if (!File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{currentAccessoryMain.name}.asset")) 
                        {
                            AssetDatabase.CreateAsset(currentAccessoryMain, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{currentAccessoryMain.name}.asset");
                            menusCompleted++;
                        }

                    }

                }

                //Creates menus for materials 
                for (int i = 0; i < mCategoryCount; i++)
                {
                    var currentColorMain = ColorsMainMenu;
                    var currlayername = matCategories[i].layer.name;
                    var states = matCategories[i].layer.stateMachine.states;

                    var currentMenu = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                    currentMenu.name = matCategories[i].name;
                    
                    if (Helper.DoesMenuExist(currentMenu.name, true))
                        currentMenu = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus/{currentMenu.name}.asset", typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu));

                    matmenustart:

                    //If the material toggle is a bool instead of int, we don't need to iterate through each state, we just need to make one control toggle
                    if (ControllerUtil.GetParameterByName(controller, $"{currlayername}Mat").type == AnimatorControllerParameterType.Bool)
                    {
                        currentMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = currlayername,
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                            parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = $"{currlayername}Mat" },
                            value = 1
                        });

                        currentColorMain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = currentMenu.name,
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                            subMenu = currentMenu
                        });

                        if (!Helper.DoesMenuExist(currentMenu.name, true)) {
                            AssetDatabase.CreateAsset(currentMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus/{currentMenu.name}.asset");
                            menusCompleted++;
                        }
                        //Skip to next category
                        AssetDatabase.SaveAssets();
                        continue;
                    }

                    //If the main menu reaches 8 control limit and there are more layers to go through, we create a new main menu to continue iterating
                    if (currentColorMain.controls.Count() == 8 && i + 1 <= mCategoryCount)
                    {
                        var nextmain = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                        var matMenuNameCount = 0;

                        while (File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus/ColorsMore{matMenuNameCount}.asset") != false) {
                            matMenuNameCount++;
                        }
                       
                        AssetDatabase.CreateAsset(nextmain, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/ColorsMore{matMenuNameCount}.asset");
                        menusCompleted++;

                        currentColorMain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = "More",
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                            subMenu = nextmain
                        });

                        if (!Helper.DoesMenuExist(currentColorMain.name, false))
                        {
                            AssetDatabase.CreateAsset(currentColorMain, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{currentColorMain.name}.asset");
                            menusCompleted++;
                        }

                        currentColorMain = nextmain;
                        AssetDatabase.SaveAssets();
                    }
                    //Fetch last nested main menu 
                    else if (currentColorMain.controls.Count() == 9 && currentColorMain.controls.Last().subMenu != null)
                    {
                        while (currentColorMain.controls.Last().subMenu != null)
                        {
                            currentColorMain = currentColorMain.controls.Last().subMenu;
                            goto matmenustart;
                        }
                    }

                    for (int y = 0; y < states.Count(); y++)
                    {
                        //Add new control per state in the current layer, until we reach 8 controls, in which the last one will be an additional menu for further iteration
                        if (currentMenu.controls.Count() < 8)
                        {
                            currentMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                            {
                                name = $"{states[y].state.name}",
                                type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                                parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = $"{currlayername}Mat" },
                                value = y
                            });

                            //If there are no more states to iterate through for this category, that means the menu is finished, and we should export it and add to the main menu.
                            if (y == states.Count() - 1 && !currentMenu.name.Contains("More"))
                            {
                                currentColorMain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                                {
                                    name = currentMenu.name,
                                    type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                                    subMenu = currentMenu
                                });

                                if (!Helper.DoesMenuExist(currentMenu.name, true)) {
                                    AssetDatabase.CreateAsset(currentMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus/{currentMenu.name}.asset");
                                    menusCompleted++;
                                }

                                AssetDatabase.SaveAssets();
                            }

                        }

                        //If we reach the end of the current menu and there are still more states to consider, create a new menu
                        else if (currentMenu.controls.Count() == 8 && y + 1 <= states.Count())
                        {
                            var colornamecount = 0;
                            ExtraMenus[index] = ScriptableObject.CreateInstance<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>();
                            ExtraMenus[index].name = $"{currlayername}More{colornamecount}";

                            index++;
                            Array.Resize(ref ExtraMenus, index + 1);

                            colornamecount++;

                            if (!Helper.DoesMenuExist(ExtraMenus[index - 1].name, true)) {
                                AssetDatabase.CreateAsset(ExtraMenus[index - 1], $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus/{ExtraMenus[index - 1].name}.asset");
                                menusCompleted++;
                            }

                            else
                                ExtraMenus[index - 1] = (VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu)AssetDatabase.LoadAssetAtPath($"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus/{ExtraMenus[index - 1].name}.asset", typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu));

                            currentMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                            {
                                name = "More",
                                type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                                subMenu = ExtraMenus[index - 1]
                            });
                            //Export our current menu
                            if (!Helper.DoesMenuExist(currentMenu.name, true)) {
                                AssetDatabase.CreateAsset(currentMenu, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/Submenus/{currentMenu.name}.asset");
                                menusCompleted++;
                            }

                            //Add this current full menu to the main menu
                            currentColorMain.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                            {
                                name = currentMenu.name,
                                type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                                subMenu = currentMenu
                            });

                            AssetDatabase.SaveAssets();

                            currentMenu = ExtraMenus[index - 1];
                        }                                          
                    }
                    //When we have reached the end
                    if (i == mCategoryCount - 1) {
                        AssetDatabase.Refresh();

                        if (!File.Exists($"{Application.dataPath}/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{currentColorMain.name}.asset")) {
                            AssetDatabase.CreateAsset(currentColorMain, $"Assets/Nin/EZAvatar/{EzAvatar.avatar.name}/Menus/{currentColorMain.name}.asset");
                            AssetDatabase.SaveAssets();
                            menusCompleted++;
                        }
                    }
                }
            
                //Destroys unused menus
                if (mCategoryCount == 0 && ColorsMainMenu.controls.Count() == 0)
                    UnityEngine.Object.DestroyImmediate(ColorsMainMenu);
                if (oCategoryCount == 0 && AccessoriesMainMenu.controls.Count() == 0)
                    UnityEngine.Object.DestroyImmediate(AccessoriesMainMenu);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }                                                
        }
    }
}

#endif