# EZAvatar - 3.0 Helper for VRChat
This is a utility helper script that intends to automate some of the repetition in the process of creating avatars for the social VR app **VRChat**, and ease the barrier to entry for people aspiring to create their first avatars.

## Features: 
Current functionality of the script includes:

- ### Creation of animation clips for material swaps and gameobject toggles

  New animation clips are created and stored in a folder created for each avatar that is input into the script. <sub> *organization, woo!* <sub>

- ### Completion of animator logic for material and gameobject toggles 
  
  Automatically creates animator layers, states, transitions, and parameters
  
  - _Supports adding on toggles to existing layers_
  
- ### Option to automatically generate simple menus on execution
  
  Creates simple VRC expression menus based on the toggles that are created. 
  These are automatically added to the current avatar main menu, but it will create a new main menu
  if none are found.

## How to Use:

Add avatar gameobject onto script

![image](https://github.com/NinDevs/EZAvatar/assets/109317890/d73ae9d3-d5b9-40d8-b364-c7c3efb3c34d)
  
### Creation of categories
  
Create a new category with any name you want 
  - Material categories are for creation of material swap toggles, GameObject categories are for on/off object toggles
  
These categories help you organize toggles/swaps of different kinds, these are the equivalent of animator layers.
  
![image](https://github.com/NinDevs/EZAvatar/assets/109317890/4009ee76-f1f3-4337-8196-3829bd7bd62b)

  ### Material swaps
  
  After creating a category of the respective type, there is a field to input the mesh object. 
  Ensure that this is the gameobject that contains a Skinned Mesh Renderer, which contains the materials for that mesh. [like so](https://github.com/NinDevs/EZAvatar/assets/109317890/df196720-8b63-4285-afff-d8b643150744)

  ![image](https://github.com/NinDevs/EZAvatar/assets/109317890/c6e0377f-2d89-463f-a630-fad0efeed21c)

  The default material is the material already on that object, that you wish to swap out for a few others via toggles. The rest are the materials you'd like to use. 
  Input whichever materials you would like to use in those material fields.

  Use + and - buttons to add or remove material slots, and Del to delete that category.

  ### Gameobject toggles
  
  Upon creation of a category for gameobject toggles, you will see a + button that allows you to add slots for how many gameobjects you want to toggle on/off at once.

  ![image](https://github.com/NinDevs/EZAvatar/assets/109317890/f8008029-669b-450c-887b-596dfcf649e4)

  The **shoes** category as referenced above would just be an on/off toggle for that gameobject alone,
  while the **accessories** would be the toggling of *two* objects, since it has 2 slots. 

  Input whichever gameobjects you would like to see toggled in those object fields. Of course, these gameobjects need to be present in the **hierarchy** <sub>[ex](https://github.com/NinDevs/EZAvatar/assets/109317890/cf201fec-db9c-4289-a5dc-9154ebba3d53)
</sub>
  of your avatar.
  
  Use + and - buttons to add or remove gameobject slots, and Del to delete that category.

  ### Once finished setting up:
  Press run! And watch as the script will magically perform the bulk of these tasks :)
  


