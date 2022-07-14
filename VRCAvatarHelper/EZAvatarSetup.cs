
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class EZAvatarSetup : EditorWindow
{
    [MenuItem("Nin/Utilities/3.0 Helper")]
    static void Init()
    {
        //Creating a new editor window, and then shows it
        EZAvatarSetup window = (EZAvatarSetup)EditorWindow.GetWindow(typeof(EZAvatarSetup));
        window.Show();

    }

    public void OnGUI()
    {
        //Sets up a gameobject slot within the editor window.
        avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", avatar, typeof(GameObject), true);
        //Creates a foldout (dropdown) and then buttons under it.
        TogglesMenu = EditorGUILayout.Foldout(TogglesMenu, "Setup Toggles", true);
        if (TogglesMenu)
        {
            if (GUILayout.Button("Setup Accessory (Bool) Toggles"))
            {
                FindAcessoryToggles();
            }

            if (GUILayout.Button("Setup Material (Int) Toggles"))
            {
                FindMaterialToggles();

            }

            if (GUILayout.Button("Setup Radial Puppets (Floats)"))
            {
                FindFloats();
            }
        }

        SetupMenus = EditorGUILayout.Foldout(SetupMenus, "Setup Menus", true);
        if (SetupMenus)
        {
            if (GUILayout.Button("Setup Accessory Menus"))
            {
                SetupAccessoryToggleMenus();
            }

            if (GUILayout.Button("Setup Material Menus"))
            {
                SetupMaterialMenus();
            }

            if (GUILayout.Button("Setup Radial Puppets"))
            {
                SetupFloatMenus();
            }
        }

        howToUse = EditorGUILayout.Foldout(howToUse, "How to Use", true); 
        if (howToUse)
        {
            GUIStyle helpStyle = new GUIStyle(GUI.skin.box);
            helpStyle.wordWrap = true; 
            helpStyle.alignment = TextAnchor.UpperLeft;
            Color c = Color.white;
            c.a = 0.75f;
            helpStyle.normal.textColor = c;
            GUILayout.Label(
            "Requirements: FX Layer (with correct naming of animations), Parameters Expressions Menu."
            , helpStyle
            , GUILayout.ExpandWidth(true));
            GUILayout.Label(
            "For every bool toggle animation clip you create, make sure the FX Layer (Animator Controller) of the avatar has those animations, and that the names of those animations end in 'ON' and 'OFF'. Ex: HatON, ButtonON, SweaterON, etc." 
            , helpStyle
            , GUILayout.ExpandWidth(true));
            GUILayout.Label(
            "For every int toggle animation clip you create, make sure the FX Layer (Animator Controller) of the avatar has those animations, and that the names of those animations end in 'Mat' and a following number.. Ex: SweaterMat1, SweaterMat2, HairMat1, HairMat2, etc."
            , helpStyle
            , GUILayout.ExpandWidth(true));
            GUILayout.Label(
            "For every float animation clip you create, make sure the FX Layer (Animator Controller) of the avatar has those animations, and that the names of those animations end in 'Slider'. Ex: HueSlider0, HueSlider1, SweaterSlider0, SweaterSlider1, etc."
            , helpStyle
            , GUILayout.ExpandWidth(true));
        }

    }

    public GameObject avatar;
    private Animator animator;
    public List<Motion> clips;
    private bool TogglesMenu;
    private bool SetupMenus;
    private bool howToUse;
    private int integerExample;

    public void FindAcessoryToggles()
    {
        
        animator = avatar.GetComponent<Animator>();

        //Finds the FX layer of the avatar as an animator controller we can edit.
        var controller = avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().baseAnimationLayers.ToList().Where(x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController as AnimatorController;

        if (controller == null)
        {
            Debug.Log("There is no FX Layer on this avatar! FX Layer controller is required for this script!");
            return;
        }

        else
            Debug.Log("FX Layer found! Proceeding . . . ");
        //Gets all of the animation clips on the animator in order to sort through later.
        var getClips = controller.animationClips;
        var sortClips = new List<AnimationClip>();
        var clipSuffixs = new List<string>();
        var listsOfClips = new List<List<AnimationClip>>();

        //Iterates through each animationclip in the controller to add to a sorted list. Helps setup our other lists.
        foreach (var clip in getClips)
        {
            if (clip.name.EndsWith("on", StringComparison.OrdinalIgnoreCase) || clip.name.EndsWith("off", StringComparison.OrdinalIgnoreCase))
            {
                sortClips.Add(clip);
            }

        }

        sortClips.OrderBy(x => x.name);

        for (int i = 0; i < sortClips.Count; i++)
        {
            var temp = sortClips[i].name;
            var suffix = temp.Substring(0, temp.LastIndexOf("O", StringComparison.OrdinalIgnoreCase));
            clipSuffixs.Add(suffix);
            //Compares the suffix of the current clip name with the last clip to see if not the same. If so, it will make a new list and add ON && OFF clips.
            if (i > 1 && clipSuffixs[i] == clipSuffixs[i - 1])
            {
                var tempTwoClips = new List<AnimationClip>();
                tempTwoClips.Add(sortClips[i]);
                tempTwoClips.Add(sortClips[(i - 1)]);
                if(tempTwoClips.Count == 2)
                {
                    listsOfClips.Add(tempTwoClips);        
                }
            }

        }
        //Iterates through each list of animation clips. Per list (pair of 2 animations) it will create a new layer.
        for(int i = 0; i < listsOfClips.Count; i++)
        {
            var clipname = listsOfClips[i].First().name;
            var nameBeforeONOFF = clipname.Substring(0, clipname.LastIndexOf('O'));
            var layerName = "Toggle " + nameBeforeONOFF;
            var parameterName = $"{nameBeforeONOFF}_Toggle";
            var parameterBool = AnimatorControllerParameterType.Bool;
            var doesLayerExist = false;
            var doesParameterExist = false;
            var doesParamatereExistName = "";

            /* ********************************** */
            // IGNORE - these are checks to make sure nothing extra is created if not needed.
            if (GetLayerByName(controller, layerName) != null)
            {
                doesLayerExist = true;
            }
            
            if(GetParameterByName(controller, parameterName) != null)
            {
                doesParameterExist = true;
            }
            
            //Do nothing if layers and parameters already exist for the toggle.
            if (doesLayerExist == true)
            {
               
            }
            
            if(doesParameterExist == true)
            {
                doesParamatereExistName = GetParameterByName(controller, parameterName).name;
            }
            
            /* ********************************** */

            //Creating of layers logic.
            if (doesLayerExist == false)
            {
                controller.AddLayer(layerName);
                //Only adds the parameter to our animator controller if it doesn't exist.
                if (doesParameterExist == false)
                {
                    controller.AddParameter(parameterName, parameterBool);
                }
                //If parameter exists, it will use that parameter to setup anything it needs to. Aka if other parts don't exist but the parameter does.
                if(doesParameterExist == true)
                {
                    parameterName = doesParamatereExistName;
                }
                //Retrieves the new layer we just made based on its name (controller.addlayer is essentially stored in newLayer, pog!)
                var newLayer = GetLayerByName(controller, layerName);
                SetLayerWeight(controller, newLayer, 1);
                //Stores statemachine of our new layer
                var stateMachine = newLayer.stateMachine;
                var check1 = false;
                var check2 = false;
                var idleState = new AnimatorState();
                var onState = new AnimatorState();

                foreach (var clip in listsOfClips[i])
                {
                    var clipName = clip.name;
                    //Adds new statemachine to our layers
                    if (clipName.EndsWith("off", StringComparison.OrdinalIgnoreCase))
                    {
                        idleState = stateMachine.AddState("OFF");
                        check1 = true;
                    }
                    if (clipName.EndsWith("on", StringComparison.OrdinalIgnoreCase))
                    {
                        onState = stateMachine.AddState("ON");
                        check2 = true;
                    }

                }
                //This will happen only after an off and on state has been created inside the current layer.
                if (check1 == true && check2 == true)
                {
                    //Creating transition from on to off, and vice versa. 
                    var idleToON = idleState.AddTransition(onState);
                    var onToIdle = onState.AddTransition(idleState);
                    //This just adds our bool and checks to see if its true or false.
                    idleToON.AddCondition(AnimatorConditionMode.If, 1, parameterName);
                    onToIdle.AddCondition(AnimatorConditionMode.IfNot, 1, parameterName);
                    //These lines will turn exit mode to 0 and etc like 3.0 setup be
                    ApplyTransitionSettings(idleToON);
                    ApplyTransitionSettings(onToIdle);
                    //Adds parameters to parameter menu           
                    if (avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters.FindParameter(parameterName) == null)
                    {
                        AddNewParameter(avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool, 0, parameterName);
                    } 
                    //Puts the correct animation clip in the corresponding states.
                    foreach (var clip in listsOfClips[i])
                    {
                        if (clip.name.EndsWith("off", StringComparison.OrdinalIgnoreCase))
                        {
                            idleState.motion = clip;
                        }
                        if (clip.name.EndsWith("on", StringComparison.OrdinalIgnoreCase))
                        {
                            onState.motion = clip;
                        }
                    }
                    check1 = false;
                    check2 = false;
                    doesLayerExist = false;
                    doesParameterExist = false;
                }

            }
        }
        Debug.Log("Your avatar is now setup for accessory toggles! Woo!");
    }


    public AnimatorControllerLayer GetLayerByName(AnimatorController ac, string name)
    {
        foreach (var currLayer in ac.layers)
        {
            if (currLayer.name == name)
                return currLayer;
        }
        return null;
    }

    public AnimatorControllerParameter GetParameterByName(AnimatorController p, string name)
    {
        foreach (var parameter in p.parameters)
        {
            if (parameter.name == name)
                return parameter;
        }
        return null;
    } 

    //UHHHHHH, unity is annoying, so u gotta do like 500 lines of code to set the layer weight.
    public void SetLayerWeight(AnimatorController ac, AnimatorControllerLayer acl, float newWeight)
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

    //Literally just takes a transition and applies optimal settings to it.
    public static void ApplyTransitionSettings(AnimatorStateTransition ast)
    {
        ast.hasExitTime = false;
        ast.exitTime = 0;
        ast.hasFixedDuration = false;
        ast.duration = 0;
    }

    //UHHHHHHH... it doesnt need an explanation. it just does what it do.
    public VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter AddNewParameter(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters vrcExpressionParameters, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType valueType, float defaultValue, string name)
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

    public void ForceAnimatorStatePosition(AnimatorState animatorState, Vector3 newPos)
    {
        SerializedObject so = new SerializedObject(animatorState);
        var posParameter = so.FindProperty("m_Position");
        posParameter.vector3Value = newPos;
        so.ApplyModifiedProperties();
    }


    public void FindMaterialToggles()
    {

        var controller = avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().baseAnimationLayers.ToList().Where(x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController as AnimatorController;

        if (controller == null)
        {
            Debug.Log("There is no FX Layer on this avatar! FX Layer controller is required for this script!");
            return;
        }

        else
            Debug.Log("FX Layer found! Proceeding . . . ");
        var getClips1 = controller.animationClips;
        //Iterates through each animationclip in the controller     
        foreach (var clip1 in getClips1)
        {
            /*Checks to see if the animation clip names contain ON, and if so, it will take whatever is to the left of that,
            and add layers and parameters based on the name.*/
            var lastChar = clip1.name.Last();
            var endsWithNum = lastChar >= '0' && lastChar <= '9';
            if (clip1.name.EndsWith($"mat{lastChar}", StringComparison.OrdinalIgnoreCase) || clip1.name.EndsWith("mat", StringComparison.OrdinalIgnoreCase))
            {
                var temp1 = clip1.name;
                //Fetches the name of the clip from t and to the left, ignoring whether t is capital or not.
                var nameBeforeONOFF1 = temp1.Substring(0, temp1.LastIndexOf('t') + 1);
                var layerName = nameBeforeONOFF1;
                Debug.Log("Found material switch clip " + clip1.name + ". Creating layers and parameters ...");
                var parameterName = nameBeforeONOFF1;
                //This is used to get the name of the current layer being worked with. If it doesn't exist, we create a layer.
                var tempNewLayer = GetLayerByName(controller, layerName);
                var doesLayerExist = false;
                var doesParameterExist = false;
                var doesParamatereExistName = "";

                /* ********************************** */
                // IGNORE - these are checks to make sure nothing extra is created if not needed.
                if (GetLayerByName(controller, layerName) != null)
                {
                    doesLayerExist = true;
                }

                if (GetParameterByName(controller, parameterName) != null)
                {
                    doesParameterExist = true;
                }

                if (doesLayerExist == true)
                {

                }

                if (doesParameterExist == true)
                {
                    doesParamatereExistName = GetParameterByName(controller, parameterName).name;
                }
                /* ********************************** */

                if (avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters.FindParameter(parameterName) == null)
                {
                    AddNewParameter(avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Int, 0, parameterName);
                }

                //Checks to see if a layer already exists with the name of the anim clip, if it does, it will simply add parameters to THAT layer. IF not, it will make the layer first,
                //And then do its job. This way, we are able to organize mat toggles by the different type of mat toggle. Think: suffix. Ex: SweaterMat, all statemachines are made in that new layer.
                //Then HatMat, all statemachines are made in that new layer.


                if (doesLayerExist == false)
                {
                    if (tempNewLayer == null)
                    {
                        controller.AddLayer(layerName);
                        var newLayer1 = GetLayerByName(controller, layerName);
                        SetLayerWeight(controller, newLayer1, 1);
                        int i = 0;
                        var y = 0;
                        if (doesParameterExist == false)
                        {
                            controller.AddParameter(parameterName, AnimatorControllerParameterType.Int);
                        }
                        //If parameter exists, it will use that parameter to setup anything it needs to. Aka if other parts don't exist but the parameter does.
                        if (doesParameterExist == true)
                        {
                            parameterName = doesParamatereExistName;
                        }
                        var tempData = newLayer1.ToString();
                        var stateMachine1 = newLayer1.stateMachine;
                        var clipsCheck = newLayer1.name;
                        //Works on the current layer in order to add states and transitions for you. It needs a foreach statement in order to apply these for each layer.
                        var currentIteration = 0;
                        foreach (var layerCheck in clipsCheck)
                        {

                            var onState = stateMachine1.AddState($"{parameterName}" + i++, new Vector3(360, currentIteration * 55));
                            var onStateName = onState.name;
                            var stateLastChar = onStateName.Last();
                            var transitionFromAnyState = stateMachine1.AddAnyStateTransition(onState);
                            transitionFromAnyState.AddCondition(AnimatorConditionMode.Equals, y++, parameterName);
                            ApplyTransitionSettings(transitionFromAnyState);
                            //Checks to see if the statename and current clip name are the same, if so, it'll add the corresponding animationclip.
                            if (onStateName == clip1.name)
                            {
                                onState.motion = clip1;
                            }
                            //If the statename and clip name are not the same, it will check the clip names until it is the same, then put it in
                            else if (onStateName != clip1.name)
                            {
                                foreach (var clip2 in getClips1)
                                {
                                    var lastChar2 = clip2.name.Last();
                                    if (clip2.name == onStateName)
                                    {
                                        onState.motion = clip2;
                                    }

                                }

                                if (onState.motion == null)
                                {
                                    stateMachine1.RemoveState(onState);
                                }
                            }
                            currentIteration++;
                        }
                    }
                }
            }
        }


        Debug.Log("Your avatar is now set up for material toggles! WAOW");


    }


    public void FindFloats()
    {
        var controller = avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().baseAnimationLayers.ToList().Where(x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController as AnimatorController;

        if (controller == null)
        {
            Debug.Log("There is no FX Layer on this avatar! FX Layer controller is required for this script!");
            return;
        }

        else
            Debug.Log("FX Layer found! Proceeding . . . ");
        var getClips1 = controller.animationClips;
        //Iterates through each animationclip in the controller     
        foreach (var clip1 in getClips1)
        {
            
            var lastChar = clip1.name.Last();
            var endsWithNum = lastChar >= '0' && lastChar <= '9';
            if (clip1.name.EndsWith($"slider{lastChar}", StringComparison.OrdinalIgnoreCase))
            {
                var temp1 = clip1.name;
                //Fetches the name of the clip from r and to the left, ignoring whether r is capital or not.
                var nameBeforeONOFF1 = temp1.Substring(0, temp1.LastIndexOf('r') + 1);
                var suffix = temp1.Substring(0, temp1.LastIndexOf('s'));
                var clipList = nameBeforeONOFF1.ToList();
                var layerName = nameBeforeONOFF1;
                Debug.Log("Found float clip " + clip1.name + ". Creating layers and parameters ...");
                var parameterName = nameBeforeONOFF1;
                //This is used to get the name of the current layer being worked with. If it doesn't exist, we create a layer.
                var tempNewLayer = GetLayerByName(controller, layerName);
                var doesLayerExist = false;
                var doesParameterExist = false;
                var doesParamatereExistName = "";

                /* ********************************** */
                // IGNORE - these are checks to make sure nothing extra is created if not needed.
                if (GetLayerByName(controller, layerName) != null)
                {
                    doesLayerExist = true;
                }

                if (GetParameterByName(controller, parameterName) != null)
                {
                    doesParameterExist = true;
                }

                if (doesLayerExist == true)
                {

                }

                if (doesParameterExist == true)
                {
                    doesParamatereExistName = GetParameterByName(controller, parameterName).name;
                }
                /* ********************************** */

                if (avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters.FindParameter(parameterName) == null)
                {
                    AddNewParameter(avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float, 0, parameterName);
                }
                
                if(doesLayerExist == false)
                {
                    if (tempNewLayer == null)
                    {
                        controller.AddLayer(layerName);
                        var newLayer1 = GetLayerByName(controller, layerName);
                        SetLayerWeight(controller, newLayer1, 1);
                        controller.AddParameter(parameterName, AnimatorControllerParameterType.Float);
                        if (doesParameterExist == false)
                        {
                            controller.AddParameter(parameterName, AnimatorControllerParameterType.Float);
                        }
                        //If parameter exists, it will use that parameter to setup anything it needs to. Aka if other parts don't exist but the parameter does.
                        if (doesParameterExist == true)
                        {
                            parameterName = doesParamatereExistName;
                        }
                        var tempData = newLayer1.ToString();
                        //Blendtree logic
                        var blendtree = new BlendTree();
                        blendtree.name = layerName;
                        var stateMachine1 = newLayer1.stateMachine;
                        var clipsCheck = newLayer1.name;
                        var onState = stateMachine1.AddState($"{parameterName}");
                        var blendtreename = blendtree.name;
                        var rootStateMachine = newLayer1.stateMachine;
                        var stateWithBlendTree = rootStateMachine.states[0].state;
                        //Works on the current layer in order to add states and transitions for you. It needs a foreach statement in order to apply these for each layer.
                        var onStateName = onState.name;
                        //Checks to see if the suffix of clips are the same, and adds clips to the blendtree.
                        foreach (var clip2 in getClips1)
                        {
                            var temp2 = clip2.name;
                            var clipsuffix = temp2.Substring(0, temp2.LastIndexOf('r') + 1);
                            var blendsuffix = blendtreename.Substring(0, blendtreename.LastIndexOf('r') + 1);
                            //If the blendtree name and clip name are the same, it will add that animation clip to the blendtree.
                            if (blendsuffix == clipsuffix)
                            {
                                blendtree.AddChild(clip2);
                                Debug.Log($"Added motion {clip2} to the blendtree {blendtree.name}");

                            }

                        }
                        blendtree.blendParameter = parameterName;
                        stateWithBlendTree.motion = blendtree;
                        AssetDatabase.AddObjectToAsset(blendtree, stateWithBlendTree);

                    }
                }
                
            }

        }
        Debug.Log("Your avatar is now set up for floats! WAOW");
    }

    public void SetupAccessoryToggleMenus()
    {
        //Checks to see if there is a directory already for menus created with this script. If not, it'll make it.


        var savePath = $"{Application.dataPath}/Nin/EZAvatarSetup/{avatar.name}/Menus";
        Debug.Log(savePath);
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }
      
        var controller = avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().baseAnimationLayers.ToList().Where(x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController as AnimatorController;
        var parametersMenu = avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters;
        var expressionsMenu = avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionsMenu;
        var parametersNames = parametersMenu.parameters.ToList().Select(x => x.name).ToList();
        VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu currentPage = null;
        var mainPage = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu();
        expressionsMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
        {
            name = $"Toggles",
            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
            subMenu = mainPage
        }); 
        AssetDatabase.CreateAsset(mainPage, $"Assets/Nin/EZAvatarSetup/{avatar.name}/Menus/Toggles {avatar.name}.asset");
        var currentIteration = 1;
        foreach (var parameter in parametersMenu.parameters)
        {
           
            if (currentPage == null || currentPage.controls.Count == 8)
            {
                
                var previousPage = currentPage;
                currentPage = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu();
                if (previousPage != null)
                {
                    AssetDatabase.CreateAsset(previousPage, $"Assets/Nin/EZAvatarSetup/{avatar.name}/Menus/Toggles Page{currentIteration}.asset");
                    mainPage.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                    {
                        name = $"Page {currentIteration}",
                        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = previousPage
                    });
                    
                    currentIteration++;
                }

            }
            
            switch(parameter.valueType)
            {
                case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool:
                    currentPage.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                    {
                        name = parameter.name,
                        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                        parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() {name = parameter.name},


                    });
                    break;

                 
                default: break;
            }
             
                   
        }

        if (currentPage.controls.Count != 8)
        {
            
            AssetDatabase.CreateAsset(currentPage, $"Assets/Nin/EZAvatarSetup/{avatar.name}/Menus/Toggles Page{currentIteration}.asset");
            mainPage.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
            {
                name = $"Page {currentIteration}",
                type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = currentPage
            }); 
        }
        
        
 
        

    }

    public void SetupMaterialMenus ()
    {
        //Checks to see if there is a directory already for menus created with this script. If not, it'll make it.


        var savePath = $"{Application.dataPath}/Nin/EZAvatarSetup/{avatar.name}/Menus";
        Debug.Log(savePath);
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        var controller = avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().baseAnimationLayers.ToList().Where(x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController as AnimatorController;
        var parametersMenu = avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters;
        var expressionsMenu = avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionsMenu;
        var parametersNames = parametersMenu.parameters.ToList().Select(x => x.name).ToList();
        VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu currentPage = null;
        var mainPage = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu();
        expressionsMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
        {
            name = $"Mat Toggles",
            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
            subMenu = mainPage
        });
        AssetDatabase.CreateAsset(mainPage, $"Assets/Nin/EZAvatarSetup/{avatar.name}/Menus/Mat Toggles {avatar.name}.asset");
        var currentIteration = 1;
        //var parameterValue = 0;
        //var parameterNameValue = 0;
        var clips = controller.animationClips.Distinct().ToList().OrderBy(x => x.name);
        var clipnames = clips.Select(y => y.name).ToList();
        AnimationClip previousClip = null;


        foreach (var clip in clips)
        {
            Debug.Log(clip.name);
            var lastChar = clip.name.Last();
            var parameterName = clip.name.Substring(0, clip.name.LastIndexOf('t') + 1);
            if (clip.name.EndsWith($"mat{lastChar}", StringComparison.OrdinalIgnoreCase))
            {
                var clipssuffix = clip.name.Substring(0, clip.name.LastIndexOf('m') + 1);
                var currentClip = clipnames.Where(x => x.Contains(clipssuffix)).FirstOrDefault();
                var currentSuffix = currentClip.ToString().Substring(0, currentClip.LastIndexOf('m') + 1);
                if (currentPage == null || currentPage.controls.Count == 8 || previousClip.name.Substring(0, previousClip.name.LastIndexOf("t") + 1) != parameterName)
                {

                    var previousPage = currentPage;
                    currentPage = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu();
                    if (previousPage != null)
                    {
                        AssetDatabase.CreateAsset(previousPage, $"Assets/Nin/EZAvatarSetup/{avatar.name}/Menus/Mat Toggles Page{currentIteration}.asset");
                        mainPage.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                        {
                            name = $"Page {currentIteration}",
                            type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                            subMenu = previousPage
                        });
                        
                        currentIteration++;
                    }

                }

                if (clipssuffix == currentSuffix)
                {
                    currentPage.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                    {
                        name = parameterName + $"{lastChar}",
                        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.Toggle,
                        parameter = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter() { name = parameterName },
                        value = lastChar - 48

                    });

                }

            }
           
            previousClip = clip;

        }

        if (currentPage.controls.Count != 8)
        {

            AssetDatabase.CreateAsset(currentPage, $"Assets/Nin/EZAvatarSetup/{avatar.name}/Menus/Mat Toggles Page{currentIteration}.asset");
            mainPage.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
            {
                name = $"Page {currentIteration}",
                type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = currentPage
            });
        }


    }

    public void SetupFloatMenus ()
    {
    //Checks to see if there is a directory already for menus created with this script. If not, it'll make it.


    var savePath = $"{Application.dataPath}/Nin/EZAvatarSetup/{avatar.name}/Menus";
    Debug.Log(savePath);
    if (!Directory.Exists(savePath))
    {
        Directory.CreateDirectory(savePath);
        AssetDatabase.Refresh();
    }

    var controller = avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().baseAnimationLayers.ToList().Where(x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController as AnimatorController;
    var parametersMenu = avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters;
    var expressionsMenu = avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionsMenu;
    var parametersNames = parametersMenu.parameters.ToList().Select(x => x.name).ToList();
    VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu currentPage = null;
    var mainPage = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu();
    expressionsMenu.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
    {
        name = $"Radial Puppets",
        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
        subMenu = mainPage
    });
    AssetDatabase.CreateAsset(mainPage, $"Assets/Nin/EZAvatarSetup/{avatar.name}/Menus/Radial Puppets {avatar.name}.asset");
    var currentIteration = 1;
       

        foreach (var parameter in parametersMenu.parameters)
        {

            if (currentPage == null || currentPage.controls.Count == 8)
            {

                var previousPage = currentPage;
                currentPage = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu();
                if (previousPage != null)
                {
                    AssetDatabase.CreateAsset(previousPage, $"Assets/Nin/EZAvatarSetup/{avatar.name}/Menus/Radial Puppets Page{currentIteration}.asset");
                    mainPage.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                    {
                        name = $"{parameter.name}",
                        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = previousPage
                    });

                    currentIteration++;
                }

            }

            switch (parameter.valueType)
            {
                case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float:
                    currentPage.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
                    {
                        name = parameter.name,
                        type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.RadialPuppet,
                        subParameters = new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter[] { new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.Parameter { name = parameter.name } },


                    });
                    break;


                default: break;
            }

        }


        if (currentPage.controls.Count != 8)
        {

            AssetDatabase.CreateAsset(currentPage, $"Assets/Nin/EZAvatarSetup/{avatar.name}/Menus/Radial Puppets Page{currentIteration}.asset");
            mainPage.controls.Add(new VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control()
            {
                name = $"Page {currentIteration}",
                type = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = currentPage
            });

        }   
    }

}


