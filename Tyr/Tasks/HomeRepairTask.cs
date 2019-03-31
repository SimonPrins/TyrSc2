using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class HomeRepairTask : Task
    {
        public static HomeRepairTask Task = new HomeRepairTask();
        private Agent RepairTarget;
        private int NeedsRepairingUpdateFrame = 0;
        public int Range = 40;

        public static void Enable()
        {
            Enable(Task);
        }

        public HomeRepairTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            if (BuildingType.BuildingAbilities.Contains((int)agent.CurrentAbility()))
                return false;
            UpdateNeedsRepairing();
            return agent.Unit.UnitType == UnitTypes.SCV && Units.Count < 3;
        }

        public override bool IsNeeded()
        {
            UpdateNeedsRepairing();
            return RepairTarget != null && Tyr.Bot.UnitManager.Completed(UnitTypes.SCV) >= 15;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            UpdateNeedsRepairing();
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor()
            {
                Pos = SC2Util.To2D(RepairTarget.Unit.Pos),
                Count = 3,
                UnitTypes = new HashSet<uint>() { UnitTypes.SCV },
                MaxDist = 40
            });
            return result;
        }

        private void UpdateNeedsRepairing()
        {
            if (NeedsRepairingUpdateFrame >= Tyr.Bot.Frame)
                return;
            NeedsRepairingUpdateFrame = Tyr.Bot.Frame;
            
            if (RepairTarget != null)
            {
                if (!Tyr.Bot.UnitManager.Agents.ContainsKey(RepairTarget.Unit.Tag))
                    RepairTarget = null;
                else if (RepairTarget.Unit.Health == RepairTarget.Unit.HealthMax)
                    RepairTarget = null;
                else if (RepairTarget.DistanceSq(Tyr.Bot.MapAnalyzer.StartLocation) <= (Range + 10) * (Range + 10))
                    RepairTarget = null;
            }
            if (RepairTarget != null)
                return;

            foreach (Agent agent in Tyr.Bot.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType == UnitTypes.BUNKER
                    || agent.Unit.UnitType == UnitTypes.PLANETARY_FORTRESS
                    || agent.Unit.UnitType == UnitTypes.SCV
                    || (agent.IsBuilding && agent.Unit.Health >= agent.Unit.Health * 0.75))
                    continue;

                if (agent.Unit.BuildProgress < 0.99)
                    continue;

                if (!UnitTypes.LookUp[agent.Unit.UnitType].Attributes.Contains(SC2APIProtocol.Attribute.Mechanical))
                    continue;

                if (agent.DistanceSq(Tyr.Bot.MapAnalyzer.StartLocation) >= Range * Range)
                    continue;

                if (agent.Unit.Health < agent.Unit.HealthMax)
                {
                    RepairTarget = agent;
                    break;
                }
            }
        }


        public override void OnFrame(Tyr tyr)
        {
            if (RepairTarget == null)
            {
                Clear();
                return;
            }
            List<Agent> unassignedSCVs = new List<Agent>();
            Dictionary<ulong, int> alreadyRepairing = new Dictionary<ulong, int>();

            foreach (Agent agent in Units)
                agent.Order(316, RepairTarget.Unit.Tag);
        }
    }
}
