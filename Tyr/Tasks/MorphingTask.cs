using SC2APIProtocol;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    public class MorphingTask : Task
    {
        public static MorphingTask Task = new MorphingTask();
        private HashSet<uint> UnitsDesired = new HashSet<uint>();
        private HashSet<uint> UnitsMorphing = new HashSet<uint>();
        private Dictionary<ulong, uint> MorphingUnits = new Dictionary<ulong, uint>();

        public MorphingTask() : base(3)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Tyr.Bot.TaskManager.Add(Task);
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            foreach (uint desired in UnitsDesired)
            {
                if (UnitsMorphing.Contains(desired))
                    continue;
                if (desired != UnitTypes.DRONE)
                {
                    result.Add(new UnitDescriptor() { Pos = SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation), Count = 1, UnitTypes = new HashSet<uint>() { MorphingType.LookUpToType[desired].FromType }, Marker = desired });
                    continue;
                }

                Point2D pos = SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation);
                int alreadyMining = 100;
                foreach (Base b in Tyr.Bot.BaseManager.Bases)
                {
                    if (b.ResourceCenter == null)
                        continue;
                    if (b.ResourceCenter.Unit.AssignedHarvesters >= alreadyMining)
                        continue;
                    pos = b.BaseLocation.Pos;
                    alreadyMining = b.ResourceCenter.Unit.AssignedHarvesters;
                }
                result.Add(new UnitDescriptor() { Pos = pos, Count = 1, UnitTypes = new HashSet<uint>() { MorphingType.LookUpToType[desired].FromType }, Marker = desired });
            }
            return result;
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        public override void Add(Agent agent, UnitDescriptor descriptor)
        {
            base.Add(agent, descriptor);
            MorphingUnits.Add(agent.Unit.Tag, (uint)descriptor.Marker);
            UnitsMorphing.Add((uint)descriptor.Marker);
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Tyr tyr)
        {
            for (int i = units.Count - 1; i >= 0; i--)
            {
                Agent agent = units[i];
                MorphingType morphingType = MorphingType.LookUpToType[MorphingUnits[agent.Unit.Tag]];
                if (tyr.Observation.Observation.PlayerCommon.Minerals < morphingType.Minerals
                    || tyr.Observation.Observation.PlayerCommon.Vespene < morphingType.Gas)
                    continue;
                if (agent.Unit.UnitType != morphingType.FromType
                    || agent.CurrentAbility() == morphingType.Ability)
                {
                    UnitsDesired.Remove(morphingType.ToType);
                    UnitsMorphing.Remove(morphingType.ToType);
                    MorphingUnits.Remove(agent.Unit.Tag);
                    IdleTask.Task.Add(agent);
                    units[i] = units[units.Count - 1];
                    units.RemoveAt(units.Count - 1);
                    continue;
                }
                
                agent.Order(morphingType.Ability);
            }

            List<ulong> deadUnits = new List<ulong>();
            foreach (ulong tag in MorphingUnits.Keys)
            {
                bool stillExists = false;
                foreach (Agent agent in Units)
                    if (agent.Unit.Tag == tag)
                        stillExists = true;
                if (!stillExists)
                    deadUnits.Add(tag);
            }

            foreach (ulong tag in deadUnits)
            {
                UnitsMorphing.Remove(MorphingType.LookUpToType[MorphingUnits[tag]].ToType);
                MorphingUnits.Remove(tag);
            }
        }

        public void Morph(uint unitType)
        {
            MorphingType morphingType = MorphingType.LookUpToType[unitType];
            if (Tyr.Bot.Gas() >= morphingType.Gas && Tyr.Bot.Minerals() >= morphingType.Minerals)
                UnitsDesired.Add(unitType);
        }
    }
}
