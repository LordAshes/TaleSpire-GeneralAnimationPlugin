using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using BepInEx;
using BepInEx.Configuration;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LordAshes
{
    using AnimationSequence = List<Dictionary<string,StatesPlugin.AnimationChange>>;

    [BepInPlugin(Guid, Name, Version)]
    public class StatesPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "General Animation Plug-In";
        public const string Guid = "org.lordashes.plugins.genanim";
        public const string Version = "1.0.0.0";

        public enum DiagnosticMode
        {
            none = 0,
            basic,
            detailed
        }

        /// Allows setting of the disgnostic mode level
        public static DiagnosticMode diagnostics = DiagnosticMode.basic;

        // Content directory
        public static string dir = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("/")) + "/TaleSpire_CustomData/";

        // Transforms indexed by character and then bone name
        private static Dictionary<string, Dictionary<string, Transform>> bones = new Dictionary<string, Dictionary<string, Transform>>();

        private ConfigEntry<KeyboardShortcut> trigger;

        private enum AnimationChangeAction
        {
            bend = 1,
            shift
        };

        public class Target
        {
            public float ax {get; set;} = 0f;
            public float ay { get; set; } = 0f;
            public float az { get; set; } = 0f;
            public float px { get; set; } = 0f;
            public float py { get; set; } = 0f;
            public float pz { get; set; } = 0f;

            public static Target operator +(Target t1, Target t2)
            {
                return new Target() { ax = (t1.ax + t2.ax), ay = (t1.ay + t2.ay), az = (t1.az + t2.az), px = (t1.px + t2.px), py = (t1.py + t2.py), pz = (t1.pz + t2.pz) };
            }

            public static Target operator *(Target t, float c)
            {
                return new Target() { ax = (t.ax * c), ay = (t.ay * c), az = (t.az * c), px = (t.px * c), py = (t.py * c), pz = (t.pz * c) };
            }

            public static Target operator *(float c, Target t)
            {
                return new Target() { ax = (t.ax * c), ay = (t.ay * c), az = (t.az * c), px = (t.px * c), py = (t.py * c), pz = (t.pz * c) };
            }
        }

        public class AnimationChange
        {
            public string character { get; set; } = "{General}";
            public string bone { get; set; }
            public Target target { get; set; }
        }

        private static Dictionary<string, AnimationSequence> animations = new Dictionary<string, AnimationSequence>();

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            UnityEngine.Debug.Log("Lord Ashes General Animation Plugin Active.");

            trigger = Config.Bind("Shortcuts", "Test Animation", new KeyboardShortcut(KeyCode.P));
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        void Update()
        {
            List<string> wackStack = new List<string>();
            foreach (KeyValuePair<string,AnimationSequence> anim in animations)
            {
                if (diagnostics >= DiagnosticMode.detailed) { Debug.Log("Processing Animation '" + anim.Key + "'..."); }
                // Get next animation step
                Dictionary<string,AnimationChange> frame = anim.Value.ElementAt(0);
                // Rmove this animation step from sequence
                anim.Value.RemoveAt(0);
                // Remove this animation if complete
                if (anim.Value.Count == 0)
                {
                    if (diagnostics >= DiagnosticMode.detailed) { Debug.Log("Queuing Removal Of Animation '" + anim.Key + "'..."); }
                    wackStack.Add(anim.Key);
                }
                // Process animation change
                foreach(AnimationChange change in frame.Values)
                {
                    if (diagnostics >= DiagnosticMode.detailed) { Debug.Log("Change '" + change.character + "' bone '" + change.bone + "'..."); }
                    if(bones.ContainsKey(change.character))
                    {
                        if(bones[change.character].ContainsKey(change.bone))
                        {
                            if (diagnostics >= DiagnosticMode.detailed) { Debug.Log("Animating '" + change.character + "' bone '" + change.bone + "' to a(" + change.target.ax + "," + change.target.ay + "," + change.target.az + "), P(" + change.target.px + "," + change.target.py + "," + change.target.pz + ")"); }
                            if (!float.IsNaN(change.target.ax) && !float.IsNaN(change.target.ay) && !float.IsNaN(change.target.az))
                            {
                                bones[change.character][change.bone].rotation = Quaternion.Euler(change.target.ax, change.target.ay, change.target.az);
                            }
                            if (!float.IsNaN(change.target.px) && !float.IsNaN(change.target.py) && !float.IsNaN(change.target.pz))
                            {
                                bones[change.character][change.bone].position = new Vector3(change.target.px, change.target.py, change.target.pz);
                            }
                        }
                        else
                        {
                            Debug.LogWarning(change.character + " does not have a '"+change.bone+"' bone");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("I have no idea who '" + change.character + "' is");
                        foreach(KeyValuePair<string, Dictionary<string, Transform>> chars in bones)
                        {
                            Debug.Log(" I only know '" + chars.Key + "'");
                        }
                    }
                }
            }

            // Remove any completed animations from the animator dictionary
            foreach(string completed in wackStack)
            {
                if (diagnostics >= DiagnosticMode.basic) { Debug.Log("Removing Animation '" + completed + "'..."); }
                animations.Remove(completed);
            }

            // Testing trigger - To Be Removed
            if(trigger.Value.IsUp())
            {
                CreatureBoardAsset asset;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                GameObject go = GameObject.Find("CustomContent:" + asset.Creature.CreatureId);
                AddCharacter(go);
                AnimationSequence animSequence = LoadAnimation(dir + "Animations/CanCan.json");
                ApplyAnimation(animSequence, "CustomContent:" + asset.Creature.CreatureId);
            }
        }

        /// <summary>
        /// Method for getting references to all of the character's bones
        /// </summary>
        /// <param name="character">Name of the character</param>
        /// <param name="replace">Boolean indicating if data should be replaced if character is already registered</param>
        /// <returns>Boolean indicating of registration was successful</returns>
        public static bool AddCharacter(GameObject character, bool replace = false)
        {
            if (diagnostics >= DiagnosticMode.basic) { Debug.Log("Loading bone structure for character '" + character + "'"); }
            if(bones.ContainsKey(character.name))
            {
                if (replace)
                {
                    // Remove character from bones dictionary
                    bones.Remove(character.name);
                }
                else
                {
                    Debug.LogWarning("Character already registered. Replace is false.");
                    return false;
                }
            }
            // Add character to bones dictionary
            bones.Add(character.name, new Dictionary<string, Transform>());
            // Add bones
            AddBones(character.name, character.transform);
            return true;   
        }

        /// <summary>
        /// Method used to load an animation from file and apply it to a character
        /// </summary>
        /// <param name="character">Name of character to which the animation should be applied</param>
        /// <param name="animationFile">Path and file name of the JSON animation file</param>
        public static AnimationSequence LoadAnimation(string animationFile)
        {
            if (diagnostics >= DiagnosticMode.basic) { Debug.Log("Loading Animation '" + animationFile + "'"); }
            AnimationSequence changes = new AnimationSequence();
            Dictionary<int, Dictionary<string,AnimationChange>> frames = JsonConvert.DeserializeObject<Dictionary<int,Dictionary<string,AnimationChange>>>(System.IO.File.ReadAllText(animationFile));
            if (diagnostics >= DiagnosticMode.detailed) { Debug.Log("AnimationSeqeunce deserialized"); }
            // Process each specified frame
            for (int i = 0; i < (frames.Count - 1); i++)
            {
                if (diagnostics >= DiagnosticMode.detailed) { Debug.Log("Processing Frame Span " + frames.Keys.ElementAt(i) + " To " + frames.Keys.ElementAt(i + 1)); }
                // Determine number of frames between specified frames
                int duration = frames.Keys.ElementAt(i + 1) - frames.Keys.ElementAt(i);
                if (diagnostics >= DiagnosticMode.detailed) { Debug.Log("Frame Span Is " + duration); }
                // Store the frame position for insertion
                int index = changes.Count;
                if (diagnostics >= DiagnosticMode.detailed) { Debug.Log("Next Element In Sequence Is " + index); }
                // Add blank entries for each frame
                for (int p=0; p<duration; p++) { changes.Add(new Dictionary<string,AnimationChange>()); }
                if (diagnostics >= DiagnosticMode.detailed) { Debug.Log("Sequence Now Has " + changes.Count + " Frame Slots"); }
                // Reach the changes for the particular specified frame
                Dictionary<string, AnimationChange> frameChangesStart = frames.Values.ElementAt(i);
                Dictionary<string, AnimationChange> frameChangesEnd = frames.Values.ElementAt(i+1);
                // Process all changes for the specified frame
                foreach(string bone in frameChangesStart.Keys)
                {
                    // Build the individual changes
                    BuildSteps(ref changes, index, frameChangesStart[bone].character, bone, frameChangesStart[bone].target, frameChangesEnd[bone].target, duration);
                }
            }
            // Add the animation sequence to the animations dictionary for processing
            return changes;
        }

        /// <summary>
        /// Method to add a AnimationSequence into the animator
        /// </summary>
        /// <param name="characterName">String indicating the character to which the animation should be applied</param>
        /// <param name="animationSequence"></param>
        /// <returns></returns>
        public static System.Guid ApplyAnimation(AnimationSequence animationSequence, string characterName, string characterName2 = "")
        {
            if (diagnostics >= DiagnosticMode.basic) { Debug.Log("Applying Animation To '" + characterName + "' (secondary '" + characterName2 + "')"); }
            string json = JsonConvert.SerializeObject(animationSequence);
            json = json.Replace("{General}", characterName);
            if (characterName2 != "") { json = json.Replace("{General2}", characterName); }
            AnimationSequence characterSpecificAnimationSequence = JsonConvert.DeserializeObject<AnimationSequence>(json);
            System.Guid guid = System.Guid.NewGuid();
            animations.Add(guid.ToString(), characterSpecificAnimationSequence);
            return guid;
        }

        /// <summary>
        /// Method to stop a non-completed animation
        /// </summary>
        /// <param name="animationGuid">Animation guid obtained when applying animation</param>
        public static void StopAnimation(string animationGuid)
        {
            if (diagnostics >= DiagnosticMode.basic) { Debug.Log("Removing Animation '" + animationGuid + "' by request"); }
            animations.Remove(animationGuid.ToString());
        }

        /// <summary>
        /// Method for building animation sequence
        /// </summary>
        /// <param name="changes">List of changes</param>
        /// <param name="character">Name of the character which is being animated </param>
        /// <param name="bone">Name of the bone which is being animated</param>
        /// <param name="action">Action being taken (0=bend, 1=shift)</param>
        /// <param name="start">Vectro3 starting values</param>
        /// <param name="end">Vector3 ending values</param>
        /// <param name="cycles">Number of Update() cycles for the animation</param>
        private static void BuildSteps(ref AnimationSequence changes, int index, string character, string bone, Target start, Target end, int cycles)
        {
            // Determine change per cycle
            Target delta = new Target()
            {
                ax = (end.ax - start.ax) / cycles,
                ay = (end.ay - start.ay) / cycles,
                az = (end.az - start.az) / cycles,
                px = (end.px - start.px) / cycles,
                py = (end.py - start.py) / cycles,
                pz = (end.pz - start.pz) / cycles
            };
            // Cycle through each cycle of the animation duration
            for (int c = 0; c < cycles; c++)
            {
                // Write individual cycle values
                try
                {
                    if (diagnostics >= DiagnosticMode.detailed) { Debug.Log("Writing AnimationSequence Frame Slot (" + index + "+" + c + ") = " + (index + c)); }
                    changes.ElementAt(index + c).Add(bone, new AnimationChange()
                    {
                        character = character,
                        bone = bone,
                        target = start + (delta * ((float)c))
                    });
                }
                catch(System.Exception e)
                {
                    Debug.Log("Error: " + e);
                }
            }
        }

        /// <summary>
        /// Method used to bend a bone
        /// </summary>
        /// <param name="characterName">Name of the character whose bone is being bent</param>
        /// <param name="boneName">Name of the bone being bent</param>
        /// <param name="angles">Bend angles</param>
        /// <param name="delta">Indicates if the values are added to the current values (true) or are absolute values (false)</param>
        /// <returns>Boolean indicating if the character name and bone was found</returns>
        public static bool Bend(string characterName, string boneName, Vector3 angles, bool delta = false)
        {
            // Locate the bone
            if (!bones.ContainsKey(characterName)) { return false; }
            if (!bones[characterName].ContainsKey(boneName)) { return false; }
            // Add current values if delta is true
            if (delta)
            {
                angles.x = bones[characterName][boneName].eulerAngles.x + angles.x;
                angles.y = bones[characterName][boneName].eulerAngles.y + angles.y;
                angles.z = bones[characterName][boneName].eulerAngles.z + angles.z;
            }
            // Apply the bend
            bones[characterName][boneName].rotation = Quaternion.Euler(angles);
            return true;
        }

        /// <summary>
        /// Method used to shift a bone
        /// </summary>
        /// <param name="characterName">Name of the character whose bone is being bent</param>
        /// <param name="boneName">Name of the bone being bent</param>
        /// <param name="position">Shift position</param>
        /// <param name="delta">Indicates if the values are added to the current values (true) or are absolute values (false)</param>
        /// <returns>Boolean indicating if the character name and bone was found</returns>
        public static bool Shift(string characterName, string boneName, Vector3 position, bool delta = false)
        {
            // Locate the bone
            if (!bones.ContainsKey(characterName)) { return false; }
            if (!bones[characterName].ContainsKey(boneName)) { return false; }
            // Add current values if delta is true
            if (delta)
            {
                position.x = bones[characterName][boneName].position.x + position.x;
                position.y = bones[characterName][boneName].position.y + position.y;
                position.z = bones[characterName][boneName].position.z + position.z;
            }
            // Apply the shift
            bones[characterName][boneName].position = position;
            return true;
        }

        /// <summary>
        /// Method used to add character bones to the bones dictionary
        /// </summary>
        /// <param name="characterName"></param>
        /// <param name="node"></param>
        private static void AddBones(string characterName, Transform node)
        {
            bones[characterName].Add(node.name, node);
            if (diagnostics >= DiagnosticMode.detailed) { Debug.Log(characterName + " has bone '" + node.name + "'"); }
            foreach(Transform child in node.Children())
            {
                AddBones(characterName, child);
            }
        }
    }
}
