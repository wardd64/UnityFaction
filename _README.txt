 __    __  .__   __.  __  .___________.____    ____     _______      ___        ______ .___________. __    ______   .__   __. 
|  |  |  | |  \ |  | |  | |           |\   \  /   /    |   ____|    /   \      /      ||           ||  |  /  __  \  |  \ |  | 
|  |  |  | |   \|  | |  | `---|  |----` \   \/   /     |  |__      /  ^  \    |  ,----'`---|  |----`|  | |  |  |  | |   \|  | 
|  |  |  | |  . `  | |  |     |  |       \_    _/      |   __|    /  /_\  \   |  |         |  |     |  | |  |  |  | |  . `  | 
|  `--'  | |  |\   | |  |     |  |         |  |        |  |      /  _____  \  |  `----.    |  |     |  | |  `--'  | |  |\   | 
 \______/  |__| \__| |__|     |__|         |__|        |__|     /__/     \__\  \______|    |__|     |__|  \______/  |__| \__| 

Made by
 __          __             _____        _       _ 
 \ \        / /     /\     |  __ \      | |     | |
  \ \  /\  / /     /  \    | |__) |   __| |   __| |
   \ \/  \/ /     / /\ \   |  _  /   / _` |  / _` |
    \  /\  /     / ____ \  | | \ \  | (_| | | (_| |
     \/  \/     /_/    \_\ |_|  \_\  \__,_|  \__,_|

Special thanks to Rafalh for the reverse engineering work on Redfaction!



----------------------------------------------------------------------------------------
----------------------------------- General Info ---------------------------------------
----------------------------------------------------------------------------------------

UnityFaction is a Video game project made in Unity3D it aims to faithfully recreate the experience 
of Redfaction, in particular the online mode and platforming runmaps.
Built in are custom made tools for importing RFL levels and accompanying file types, converting 
them to a standard Unity format for the game.
These tools are freely available to be adapted, expanded upon and used for your own projects.

All tools in UnityFaction and added to them, by whomever, are freely available to anyone. 
UnityFaction may also be used for personal or commercial projects by anyone, as long as any 
content added to UnityFaction itself is made available for others.



----------------------------------------------------------------------------------------
--------------------------------------- TODO -------------------------------------------
----------------------------------------------------------------------------------------



>>>>>>>>>>> DEBUGGING & GENERAL FEATURES
Make leaves and trees double sided again
Set key defaults to qwerty keys
Include entities
Implement Vclips
water splash effects

>>>>>>>>>>> GRAPHICS
Original appears more simple and more saturated
Ambient lighting is now fixed using directional lights. These are currently global, but could be made room by room using layers. Doing so would allow to implement room ambient lights. The downside is that this requires lots of layers, which have to be properly dealt with in the rest of the game's code.


>>>>>>>>>>> MAPS
Go over all instantiated materials and fix them (transparancy, missing textures, wrong values for tiling etc.)
Check all maps for missing files, prefabs or implementations
Playtest every map to see if they can be finished or if more features need to be implemented first
	( Set map validation to possible )
Get confirmation from map creators if their maps have been faithfully recreated
	( set map validation to verified )
New maps
	AbcRun (FIGHT run)


>>>>>>>>>>> MULTIPLAYER
Late joiners get a redundant player prefab for the host
players should have name tags to recognize them
Synchronize weapons and damage tool shots
Improve 3rd person player model, more detailed animations, 3rd person weapon animations, ragdollization and the death wisp
Sync match timer
take non master map votes into account


>>>>>>>>>>> TUTORIAL
Make a detailed tutorial along with a professionaly made tutorial level.
In Menu:
	- Difficulties
	- Options
	- Single player
In game
	- platforming
	- moving platforms
	- spinners
	- tilted surfaces
	- crouching
	- jump pads and ladders

	- Damage tool (triggers and glass panes)
	- Mining tool

	- Detector tool
	- invisible walls
	- death triggers
	- Custom checkpoints

	- Cheating tool

	- checkpoints
	- Finish
Back in menu:
	- Map list
	- Multiplayer
	