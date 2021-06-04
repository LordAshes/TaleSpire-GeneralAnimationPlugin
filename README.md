# General Animation Plugin

This unofficial TaleSpire plugin is for adding general animations to TaleSpire
which can then be applied to any character that has a compatible skeleton.

## Change Log

1.0.0: Initial Release

## Install

Install using R2ModMan or similar and place animation files in 

\Steam\steamapps\common\TaleSpire\TaleSpire_CustomData\Animations\

(The LoadAnimation can actually load animation files from any location but this is the recommended location)

## Usage

The General Animation Plugin is intended to be a dependency plugin used by other plugins to implement animations.
It does not currently have any triggers for triggering animations on it own. However, it does have the complete
animation engine for processing animations when they are applied.

There are two types of animation possible: scripted animation and manual animation. However, both animation style
(described below) require a common initial step. This step involves getting a reference to the rigged Game Object
and registering its bone structure.

```C#
CreatureBoardAsset asset;
CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
GameObject go = GameObject.Find("CustomContent:" + asset.Creature.CreatureId);
AddCharacter(go);
```

The above code assumes the character has been added using Custom Mini Plugin in which case the Rigged Object has a
name that starts with "CustomContent" and ends with the corresponding creature id. If animations are to be applied
to content that was not imported with Custom Mini Plugin then change the above code to determine the correct GameObject
Object in some other way. The last line *of AddCharacter(go)* registers the character bones which needs to be done
prior to any animation.

The process for scripted animation and manual animation is described below.

### Scriped Animation

Scripted animation is a two step process. First a AnimationSequence is loaded from a JSON file which contain a single
animation or a pose (see additional comments about poses below). The second step is to apply the AnimationSequence to
a specific character. Note that a character's bones have had to have been registered in order for this animation process
to work. To load an AnimationSequence we can use code similar to:

```C#
AnimationSequence animSequence = LoadAnimation(dir + "Animations/CanCan.json");
```

To apply the animation to an actual character, we can use code similar to:

```C#
animGuid = ApplyAnimation(animSequence, "CustomContent:" + asset.Creature.CreatureId);
```

Once again the above code assumes the mini content was loaded using Custom Mini Plugin. If that is not the case then
the second parameter needs to be modified to the name of the game object that is to be animated. This parameter should
match the name of a character that was registered using the AddCharacter() method (see above).

Once the animation completes, it will be automatically removed from the animator. If the animation needs to be stopped
before it has been completed, the following code can be used:

```C#
StopAnimation(animGuid);
```
 
### Manual Animation

The plugin provides two methods for manual animation: Bend() and Shift()

The Bend method is used to bend (rotate) a character's bone.

```C#
Bend(characterName, boneName, angles, delta)
```

The characterName parameter is the character that is to be animated and must match the name of a character whose bones 
have been registered. The boneName parameter is the name of the bone that is to be animated. The angles parameter is
a Vector3 parameter indicating the x, y and z angles. The delta parameter indicates if the angles are absolute values
or delta values. If delta is false (default) then the method will set the bone angles to the provided values. If the
delta parameter is true then the method will change the current bone angles by the amount specified in the angles property.

Note that this method is not a transition. It sets the value of the bone. To create a transition multiple calls of this
method would be necessary.

The Shift method is used to shift (translate) a character's bone.

```C#
Shift(characterName, boneName, position, delta)
```

The characterName parameter is the character that is to be animated and must match the name of a character whose bones 
have been registered. The boneName parameter is the name of the bone that is to be animated. The position parameter is
a Vector3 parameter indicating the x, y and z position. The delta parameter indicates if the position is an absolute value
or a delta value. If delta is false (default) then the method will set the bone position to the provided values. If the
delta parameter is true then the method will change the current bone position by the amount specified in the positon property.

Note that this method is not a transition. It sets the value of the bone. To create a transition multiple calls of this
method would be necessary.

## Animation JSON Files
  
I am hoping to release a conversion program that will take FBX files and convert the animation to JSON files that are
compatible with this plugin. However, for now, such files need to be made manually. The format of the JSON file is
as follows:

```JSON
{
	"0": {
			"LeftUpLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"LeftLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"RightUpLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"RightLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}}
		 },
	"30":{
			"LeftUpLeg": {"target": {"ax": -90, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"LeftLeg": {"target": {"ax": -90, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"RightUpLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"RightLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}}
		 },
	"60":{
			"LeftUpLeg": {"target": {"ax": -90, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"LeftLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"RightUpLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"RightLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}}
		 },
	...
}
```

The first key is the frame (update pass) on which bones are to change. Animations are automatically transitions from
one specificed frame to the next with all of the in between frames calculated automatically. To create periods of no
animation, the JSON file needs to specified frames with the came bone specifiations.

The first key then has an "list" of bone changes indexed by the name of the bone.

This key then has a targets key which contains the parameters ax, ay, az, px, py, and pz. The bone key does have the
ability to specify additional parameters but these parameters are typically not specified in the JSON file and are only
used internally.

The ax, ay, and az parameters specify the (Euler) angles of the bone. If any are "NaN" then angles are not used.
The px, py, and pz paremeters specify the position of the bones. If any are "NaN" then position is not used.

In most cases the ax, ay and az parameters will have haves while px, py, and pz will typically be "NaN".

### Limitation

All bones that are to be animated at any point in the animation sequence must be defined in each specified frame
even if they have not changed since the last specified frame. Failure to comply with this may produce errors and
will likely not result in animation or correct animation.

Currently there is no transition in and transition out frames. Thus if the current state of the mini is different from
the first frame in the animation sequence then the mini will snap to the initial position. One possible way to avoid
this is to start and end all animations with a idle pose.

### Poses

The General Animation Plugin requires at least two specified frames in order to work because it uses the difference
between the two specified frame indexes to determine the duration over which the animation is to be applied. Thus to
make a pose, create a frame at 0 and a frame at 1 with identical content. This will make a short 2 frame animation
that ends up applying the pose.

### Advanced Animation

Each bone key in the JSON animation file does actually have two additional properties which are set automatically
if not provided in the JSON file (which is the normal case). These two properties are character and bone.

By default the character get set to {General} which is replaced with the actual character id when the ApplyAnimation()
method is used. However, the ApplyAnimation() method can actually take a second character name (default "") to be used
for animations involving two characters. In such a case the bone keys needs to be made unique to differentiate the same
bone between the two characters and then the bone property can be used to correct the name of the bone.

```JSON
{
	"0": {
			"C1.LeftUpLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"C1.LeftLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"C1.RightUpLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"C1.RightLeg": {"target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"C2.LeftUpLeg": {"character": "{General2}", "bone": "LeftUpLeg", "target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"C2.LeftLeg": {"character": "{General2}", "bone": "LeftLeg", "target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"C2.RightUpLeg": {"character": "{General2}", "bone": "RightUpLeg", "target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}},
			"C2.RightLeg": {"character": "{General2}", "bone": "RightLeg", "target": {"ax": 0, "ay": 0, "az": 0, "px": "NaN", "py": "NaN", "pz": "NaN"}}
		 },
	...
}
```
