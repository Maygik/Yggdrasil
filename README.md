# Yggdrasil

Yggdrasil is a tool designed to help simplify porting of source engine models. This tools attempts to replicate the VRChat workflow using rig slots and a built in material editor with preview.

# How to Use
## 1: Create a new project
Select a directory to save the project to, and give it a name
## 2: Configure project settings
Configure the project settings

Animation profile represents which animations the model will use when compiled. To leave the exact bone structure intact, select None. To convert to ValveBiped, select Ragdoll Only. Otherwise, pick the correct animation profile for your use.

Make sure to set the paths for the project.
This includes the model path, export directory for compileables (.qc, .smd, etc.) and a final addon directory (.vmt, .vtf, .mdl, etc.). 
## 3: Import and edit the model
Select a model file to import. You can optionally have Yggdrasil attempt to automatically map bones to rig slots.
If the model is not upright, use the controls at the bottom of this page to edit it until it looks correct. You can use the Viewport page to preview changes.
## (OPTIONAL) 3a: Edit the materials
You can optionally edit the materials for a model, then export them separately.
Exporting a material will automatically convert any used textures to VTF and build the VMT.
Files will be exported to the first set material path in the defined addon folder from step 2.
## 4: Bind bones to rig slots
To bind a bone to a slot, click on the armature bone in the hierarchy on the left, then click "Bind" next to the target rig slot.
Each slot represents a logical bone for the ValveBiped conversion.
## 5: Export the model
You can export the model to SMD and .QC, this default to "[project file directory]/output". DMX and VTA support are planned for the future.
For animation profiles outside of "None" and "Ragdoll Only", proportion trick will be ran creating the required delta animations.

# Current Known Problems
1. Proportion trick animations looks a little hunched over
2. Viewport preview is very dark
3. Rimlight and Envmap material properties have no effect
4. Player/NPC export does not include physics and hitboxes

# Credits
Developing Yggdrasil - Maygik  
Original Proportion Trick Method - CaptainBigButt  
Original Blender Proportion Trick Scripts - B L A Z E  
Test Dummy - An0nymooose  
