using System;
using System.Collections.Generic;

namespace SC2Sharp.Builds.BuildLists
{
    public class BuildListState
    {
        public Dictionary<uint, int> Desired = new Dictionary<uint, int>();
        public Dictionary<BuildingAtBase, int> DesiredPerBase = new Dictionary<BuildingAtBase, int>();
        public Dictionary<uint, int> Training = new Dictionary<uint, int>();
        public bool BuiltThisFrame;

        public void AddDesired(uint key, int val)
        {
            if (!Desired.ContainsKey(key))
                Desired.Add(key, 0);
            Desired[key] += val;
        }

        public void AddTraining(uint key, int val)
        {
            if (!Desired.ContainsKey(key))
                Desired.Add(key, 0);
            Desired[key] += val;
        }

        public void AddDesiredPerBase(BuildingAtBase key, int val)
        {
            if (!DesiredPerBase.ContainsKey(key))
                DesiredPerBase.Add(key, 0);
            DesiredPerBase[key] += val;
        }

        public int GetTraining(uint key)
        {
            if (!Training.ContainsKey(key))
                return 0;
            else return Training[key];
        }
    }
}
