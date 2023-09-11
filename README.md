# EZAvatar - 3.0 Helper for VRChat
This is a utility helper script that intends to automate some of the repetition in the process of creating avatars for the social VR app **VRChat**, and ease the barrier to entry for people aspiring to create their first avatars.

## Requirements
VRCSDK3

## Features 
Current functionality of the script includes:

- ### Creation of animation clips: supports material swaps, gameobject toggles, and blendshape toggles

  New animation clips are created and stored in a folder created for each avatar that is input into the script. <sub> *organization, woo!* <sub>

- ### Completion of animator logic
  
  Automatically creates animator layers, states, transitions, and parameters based on user input.
  
  - _Supports adding on toggles to existing layers (simply create a category with the same name as existing layer)_

  ![image](https://i.imgur.com/EE85xSg.png)
  
- ### Automatically generate menus on execution or choose a menu to add controls to
  
  Creates simple VRC expression menus based on the toggles that are created. 
  These are automatically added to the current avatar main menu, but it will create a new main menu
  if none are found. Additionally, you can choose any menu of your choice if you would like to add to an existing one.
  
  ![image](https://i.imgur.com/xasNsBZ.png)

## How to Use

TLDR: Add avatar gameobject onto script, create categories, press run :D

This script currently supports creation of three things: material swap toggles, gameobject toggles, and blendshape toggles/blend-tree sliders.
Each of these options have their own foldout menu which hosts a text field and button next to it, allowing you to create a 'category'. 
These categories translate directly into animator layers that will be added to the current FX layer of your avatar, each of which containing states and transitions that are created. 

Step by step process:

1. **Input your avatar gameobject into the "Avatar" gameobject field at the top of the editor window. Make sure it has an avatar descriptor component attached to it**.
     - ![image](https://i.imgur.com/cDdTjbQ.png)

3. **Create categories with any name you'd like (i.e: Sweater, Clothes, Hat, Shoes, Hair Length, etc)**
    - Refer below for more in-depth information about categories!
    - **You can add to existing animator layers that were created by the script previously! To do so, simply create a category with the same name.**
      - This allows you to add new toggles in as you go :)
4. **Fill out each category as desired**
    - There are additional settings on a per category basis. 
        - You can choose whether to add this control in the menu as a toggle or slider (radial puppet).  
        - You can choose whether to have the menus automatically generated in pre-made main menus, or choose to add controls to an existing menu.  
5. **Press run!**
    - Ta-daa! You are done. Enjoy your new avatar.   

  
## Creation of categories
  
Create a new category with any name you want 
  > Material categories are for creation of material swap toggles, gameobject categories are for on/off object toggles, and blendshapes categories are for blendshape value toggles/sliders.

- Material categories have a 'Mesh Object' field. This is simply the gameobject that has the mesh renderer/skinned mesh renderer attached with the material that you would like to swap. Default material is the current material that is on the mesh, hence the          name default. This is required in order to fetch the proper index of the material position in the materials array of the renderer, as well as used as the default material in toggles (default animator state). Add any materials you would wish to swap with.
- Gameobject categories simply have object fields. Slot in objects you would like to be toggled. You can choose to either toggle objects alone/together in bulk (bool toggle), or toggle objects separately (int toggle). This option is shown in the UI when there       are 2 or more objects present, and you can enable toggling the objects separately there. Note: this means you can only have one of those objects on at once, as you are toggling them using an int parameter, with a default state in which all are off.
- Blendshape categories have a 'Mesh Object' field, where you input the object with blendshapes you would like to create toggles/sliders of for your avatar. You can toggle multiple blendshapes at once, across objects even, in the same category and animation         clips. After inputting an object, it will list off all the blendshapes that are on the skinned mesh renderer of that object. You can select which ones you would like to have modified as toggles/sliders, and input the start and end values for that blendshape.

  ![image](https://imgur.com/anlRdyq.png)
  
  These categories help you organize toggles/swaps of different kinds, these are the equivalent of animator layers.
   
  ### Material Swaps
  
  After creating a category of the respective type, there is a field to input the mesh object. 
  Ensure that this is the gameobject that contains a Skinned Mesh Renderer or Mesh Renderer, which contains the materials for that mesh. [like so](https://i.imgur.com/OIMxn9u.png)
  
  ![image](https://i.imgur.com/CjxBa5h.png)

  The default material is the material already on that object, that you wish to swap out for a few others via toggles. The rest are the materials you'd like to use. 
  Input whichever materials you would like to use in those material fields.

  Use + and - buttons to add or remove material slots, and Del to delete that category.

  ### Gameobject Toggles
  
  Upon creation of a category for gameobject toggles, you will see a + button that allows you to add slots for how many gameobjects you want to toggle on/off at once.

  ![image](https://imgur.com/3185prD.png)

  The **shoes** category as referenced above would just be an on/off toggle for that gameobject alone,
  while the **accessories** category would be the toggling of *two* objects, since it has 2 slots. 

  Input whichever gameobjects you would like to see toggled in those object fields. Of course, these gameobjects need to be present in the **hierarchy** of your avatar.
  
  Use + and - buttons to add or remove gameobject slots, and Del to delete that category.

  ### Blendshape Value Toggles/Sliders
  
  Upon creation of a category for blendshapes, you will see a + button that allows you to add object slots for the objects containing blendshapes.
  Each category can have multiple objects, and you can select blendshapes from several skinned mesh renderers in order to change their values via toggles. 
  Keep in mind each category creates 2 animation clips, one for the start value, and another for the end value that you input.

  ![image](https://imgur.com/M5TIboN.png)

  Select whichever blendshapes you would like to, and change the start and end values as you'd wish.
  
  If the menu control is set to toggle, it will make a toggle (2 states with transitions between them) for the blendshape(s). 
  Otherwise, it will create a blendtree with the two animations input in it.
  
  ## Settings 

  ### Write Defaults
  **ON by default**
  
  When turned on, any states that the script creates in your animator will have Write Defaults set to on. 
  This should be set in accordance with your preference. If you're unsure as to what this is, just leave it on.

  ### Complete Animator Logic
  **ON by default**

  When turned on, it allows the script to create add to / create new layers, states, parameters, and transitions.

  If this is off, the script will only create animation clips for you.

  *Note: This setting needs to be on in order to create menus*
  
  ### Ignore Previous States
  **ON by default**

  When turned on, if you are adding on to existing layers (by having categories with names equal to existing layers in the animator), 
  it will ignore states that already exist within the layer and simply add new states based on what has been input into each category.
  
  If this is turned **off**, for any existing layer you are trying to add to with the script, it will delete all the previous states and make new states only based on what has been input into the script. This allows you as the user to quickly "redo" layers if you   so please.
  
  ### Disable Menu Generation / Modification
  **OFF by default**

  Disables ability to generate or add to menus.
  
  If this is on, menus are not created, nor new control options added to existing menus.

  ## Once finished setting up
  ### [Usage example](https://streamable.com/b6q1cr)
  Press run! And watch as the script will magically perform the bulk of these tasks :)
  


