using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LordAshes.StatesPlugin;

namespace LordAshes
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<int, Dictionary<string, AnimationChange>> animation = new Dictionary<int, Dictionary<string, AnimationChange>>();
            AnimationChange ac1 = new AnimationChange() { target = new Target() { ax = 1, ay = 2, az = 3, px = 4, py = 5, pz = 6 } };
            AnimationChange ac2 = new AnimationChange() { target = new Target() { ax = 1, ay = 2, az = 3, px = 4, py = 5, pz = 6 } };
            Dictionary<string, AnimationChange> animChanges = new Dictionary<string, AnimationChange>();
            animChanges.Add("Bone1", ac1);
            animChanges.Add("Bone2", ac2);
            animation.Add(0, animChanges);
            animation.Add(30, animChanges);
            string json = JsonConvert.SerializeObject(animation, Formatting.Indented);
            System.IO.File.WriteAllText("Test.json", json);
        }
    }
}
