using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace EZAvatar
{
    public class Algorithm
    {
        public static void SetupMaterialToggles()
        {
            Animator animator = EzAvatar.avatar.GetComponent<Animator>();
            var controller = EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().baseAnimationLayers.ToList().Where
                (x => x.type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX).ToList()[0].animatorController as AnimatorController;
            var expressionParametersMenu = EzAvatar.avatar.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>().expressionParameters;
            int layersCompleted = 0;
            int statesCompleted = 0;

            if (controller == null)
            {
                EzAvatar.debug = "There is no FX Layer on this avatar! FX Layer animator controller is required for this script!";
                Debug.Log(EzAvatar.debug);
                return;
            }

            else
            {
                EzAvatar.debug = "FX Layer found! Proceeding . . . ";
                Debug.Log(EzAvatar.debug);
            }

            foreach (var category in EzAvatar.categories)
            {
                //Bool to make boolean transition logic
                bool isAdded = false;
                bool cleared = false;
                var layername = category.name;
                var clips = category.animClips;
                int clipcount = category.animClips.Count;
                AnimatorControllerLayer[] layers = new AnimatorControllerLayer[clipcount];
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
                        layers[i] = ControllerUtil.GetLayerByName(controller, layername);
                        //Sets the layer weight to 1
                        ControllerUtil.SetLayerWeight(controller, layers[i], 1);
                        var statemachine = layers[i].stateMachine;
                        //Removes states if we are not ignoring previous states
                        if (!EzAvatar.ignorePreviousStates && !cleared) {
                            ControllerUtil.RemoveStates(layers[i]);
                            cleared = true;
                        }
                        //Only adds the parameter if it does not already exist 
                        if (ControllerUtil.GetParameterByName(controller, parametername) == null)
                            controller.AddParameter(parametername, AnimatorControllerParameterType.Int);
                        //Adds parameter to expressions menu if it doesn't exist
                        if (expressionParametersMenu.FindParameter(parametername) == null)
                            VRCUtil.AddNewParameter(expressionParametersMenu, VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Int, 0, parametername);
                        //Creates a state in the layer if it does not already exist, along with transitions, and inserts clip as the state motion.
                        if (ControllerUtil.GetAnimatorStateInLayer(layers[i], statename) == null)
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
                        layers[i] = ControllerUtil.GetLayerByName(controller, layername);
                        ControllerUtil.SetLayerWeight(controller, layers[i], 1);
                        var statemachine = layers[i].stateMachine;
                        //Removes states if we are not ignoring previous states
                        if (!EzAvatar.ignorePreviousStates && !cleared)
                        {
                            ControllerUtil.RemoveStates(layers[i]);
                            cleared = true;
                        }
                        //If we are have a layer that already has 2 states with on and off logic, and we are adding to that layer, we need to change the parameter to int
                        if (EzAvatar.ignorePreviousStates)
                        {
                            if (statemachine.states.Count() == 2)
                            {
                                ControllerUtil.ChangeParameterToInt(controller, layers[i], expressionParametersMenu, parametername);
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
                       
                        if (ControllerUtil.GetAnimatorStateInLayer(layers[i], statename) == null && statemachine.states.Count() < 2)
                        {
                            states[i] = statemachine.AddState(statename, new Vector3(360, i * 55));
                            states[i].motion = clips[i];
                            statesCompleted++;
                        }

                        else if (ControllerUtil.GetAnimatorStateInLayer(layers[i], statename) == null && statemachine.states.Count() >= 2)
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
                                    Debug.Log($"There are {states.Count()} states right now.");
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

            EzAvatar.debug = $"Finished without errors. Created {layersCompleted} new layers and {statesCompleted} states. :)";
            Debug.Log(EzAvatar.debug);
        }
    }
}
