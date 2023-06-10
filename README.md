# EZAvatar - 3.0 Helper for VRChat
This is a utility helper script that intends to automate some of the repetition in the process of creating avatars for the social VR app **VRChat**, and ease the barrier to entry for people aspiring to create their first avatars.

## Requirements
VRCSDK3

## Features 
Current functionality of the script includes:

- ### Creation of animation clips for material swaps and gameobject toggles

  New animation clips are created and stored in a folder created for each avatar that is input into the script. <sub> *organization, woo!* <sub>

- ### Completion of animator logic for material and gameobject toggles 
  
  Automatically creates animator layers, states, transitions, and parameters
  
  - _Supports adding on toggles to existing layers (simply create a category with the same name as existing layer)_

  ![image](https://i.imgur.com/EE85xSg.png)
  
- ### Option to automatically generate simple menus on execution
  
  Creates simple VRC expression menus based on the toggles that are created. 
  These are automatically added to the current avatar main menu, but it will create a new main menu
  if none are found.
  
  ![image](https://i.imgur.com/xasNsBZ.png)

## How to Use

Add avatar gameobject onto script, create categories, press run :D
  
![image](https://i.imgur.com/cDdTjbQ.png)
  
### Creation of categories
  
Create a new category with any name you want 
  - Material categories are for creation of material swap toggles, GameObject categories are for on/off object toggles
  
These categories help you organize toggles/swaps of different kinds, these are the equivalent of animator layers.
  
  ### Material swaps
  
  After creating a category of the respective type, there is a field to input the mesh object. 
  Ensure that this is the gameobject that contains a Skinned Mesh Renderer, which contains the materials for that mesh. [like so](https://i.imgur.com/OIMxn9u.png)
  
  ![image](https://i.imgur.com/CjxBa5h.png)

  The default material is the material already on that object, that you wish to swap out for a few others via toggles. The rest are the materials you'd like to use. 
  Input whichever materials you would like to use in those material fields.

  Use + and - buttons to add or remove material slots, and Del to delete that category.

  ### Gameobject toggles
  
  Upon creation of a category for gameobject toggles, you will see a + button that allows you to add slots for how many gameobjects you want to toggle on/off at once.

  ![image](https://i.imgur.com/mqGsThn.png)

  The **shoes** category as referenced above would just be an on/off toggle for that gameobject alone,
  while the **accessories** would be the toggling of *two* objects, since it has 2 slots. 

  Input whichever gameobjects you would like to see toggled in those object fields. Of course, these gameobjects need to be present in the **hierarchy** of your avatar.
  
  Use + and - buttons to add or remove gameobject slots, and Del to delete that category.
  
  ## Settings 
  ### Complete Animator Logic
  **ON by default**

  When turned on, it allows the script to create add to / create new layers, states, parameters, and transitions.
  If this is off, the script will only create animation clips for you.
  
  ### Ignore Previous States
  **ON by default**

  When turned on, if you are adding on to existing layers (by having categories with names equal to existing layers in the animator), 
  it will ignore states that already exist within the layer and simply add new states based on what has been input into each category.
  
  If this is turned **off**, for any existing layer you are trying to add to with the script, it will delete all the previous states and make new states only based on what has been input into the script. This allows   you as the user to quickly "redo" layers if you so please.
  
  ### Auto Create Menus
  **ON by default**

  Automatically creates menus based on user input in categories, and adds onto existing menus when adding onto layers.
  If this is off, menus are not created, nor new control options added to existing menus.

  ## Once finished setting up
  ### [Usage example](https://streamable.com/b6q1cr)
  Press run! And watch as the script will magically perform the bulk of these tasks :)
  


