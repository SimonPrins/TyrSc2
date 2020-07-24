using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class ReplenishBuildingSCVTask : Task
    {
        public static ReplenishBuildingSCVTask Task = new ReplenishBuildingSCVTask();
        public List<ulong> NeedsRepairing = new List<ulong>();
        private int NeedsRepairingUpdateFrame = -1;
        private HashSet<ulong> AlreadyRepairing = new HashSet<ulong>();

        private Dictionary<ulong, float> ProgressMap = new Dictionary<ulong, float>();

        private Dictionary<ulong, ulong> RepairMap = new Dictionary<ulong, ulong>();

        public static void Enable()
        {
            Enable(Task);
        }

        public ReplenishBuildingSCVTask() : base(10)
        { }

        public override List<UnitDescriptor> GetDescriptors()
        {
            UpdateNeedsRepairing();
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            foreach (ulong building in NeedsRepairing)
            {
                if (AlreadyRepairing.Contains(building))
                    continue;
                result.Add(new UnitDescriptor()
                {
                    Pos = SC2Util.To2D(Bot.Main.UnitManager.Agents[building].Unit.Pos),
                    Count = 1,
                    UnitTypes = new HashSet<uint>() { UnitTypes.SCV },
                    Marker = building
                });
            }
            return result;
        }

        public override bool DoWant(Agent agent)
        {
            if (BuildingType.BuildingAbilities.Contains((int)agent.CurrentAbility()))
                return false;
            UpdateNeedsRepairing();
            return agent.Unit.UnitType == UnitTypes.SCV && Units.Count < NeedsRepairing.Count;
        }

        public override bool IsNeeded()
        {
            UpdateNeedsRepairing();
            return NeedsRepairing.Count > 0;
        }

        public override void Add(Agent agent, UnitDescriptor descriptor)
        {
            base.Add(agent, descriptor);
            RepairMap.Add(agent.Unit.Tag, (ulong)descriptor.Marker);
        }

        private void UpdateNeedsRepairing()
        {
            if (NeedsRepairingUpdateFrame >= Bot.Main.Frame)
                return;
            NeedsRepairingUpdateFrame = Bot.Main.Frame;

            AlreadyRepairing = new HashSet<ulong>();
            NeedsRepairing = new List<ulong>();
            foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
            {
                if (!UnitTypes.BuildingTypes.Contains(agent.Unit.UnitType))
                    continue;
                if (agent.Unit.BuildProgress > 0.9999)
                    continue;

                bool progressChanged = !ProgressMap.ContainsKey(agent.Unit.Tag) || ProgressMap[agent.Unit.Tag] != agent.Unit.BuildProgress;
                if (ProgressMap.ContainsKey(agent.Unit.Tag))
                    ProgressMap[agent.Unit.Tag] = agent.Unit.BuildProgress;
                else
                    ProgressMap.Add(agent.Unit.Tag, agent.Unit.BuildProgress);

                bool alreadyBeingBuilt = false;
                foreach (Agent scv in Units)
                {
                    if (RepairMap.ContainsKey(scv.Unit.Tag)
                        && RepairMap[scv.Unit.Tag] == agent.Unit.Tag)
                    {
                        NeedsRepairing.Add(agent.Unit.Tag);
                        alreadyBeingBuilt = true;
                        break;
                    }
                }
                if (!alreadyBeingBuilt && !progressChanged)
                    NeedsRepairing.Add(agent.Unit.Tag);
            }

            foreach (Agent scv in Units)
                if (RepairMap.ContainsKey(scv.Unit.Tag))
                    AlreadyRepairing.Add(RepairMap[scv.Unit.Tag]);
        }

        public override void OnFrame(Bot tyr)
        {
            tyr.DrawText("Replenishing scvs: " + Units.Count);
            List<Agent> unassignedSCVs = new List<Agent>();
            Dictionary<ulong, int> alreadyRepairing = new Dictionary<ulong, int>();
            foreach (Agent agent in Units)
            {
                tyr.DrawSphere(agent.Unit.Pos);
                if (RepairMap.ContainsKey(agent.Unit.Tag)
                    && !NeedsRepairing.Contains(RepairMap[agent.Unit.Tag]))
                    RepairMap.Remove(agent.Unit.Tag);

                if (!RepairMap.ContainsKey(agent.Unit.Tag))
                    unassignedSCVs.Add(agent);
                else
                {
                    if (!alreadyRepairing.ContainsKey(RepairMap[agent.Unit.Tag]))
                        alreadyRepairing.Add(RepairMap[agent.Unit.Tag], 1);
                    else
                        alreadyRepairing[RepairMap[agent.Unit.Tag]]++;
                }
            }

            foreach (ulong tag in NeedsRepairing)
            {
                if (!alreadyRepairing.ContainsKey(tag))
                    alreadyRepairing[tag] = 0;
                while (alreadyRepairing[tag] == 0
                    && unassignedSCVs.Count > 0)
                {
                    alreadyRepairing[tag]++;
                    RepairMap.Add(unassignedSCVs[unassignedSCVs.Count - 1].Unit.Tag, tag);
                    unassignedSCVs.RemoveAt(unassignedSCVs.Count - 1);
                }
            }

            for (int i = unassignedSCVs.Count - 1; i >= 0; i--)
            {
                IdleTask.Task.Add(unassignedSCVs[i]);
                Units.Remove(unassignedSCVs[i]);
            }

            foreach (Agent agent in Units)
            {
                tyr.DrawLine(agent, tyr.UnitManager.Agents[RepairMap[agent.Unit.Tag]].Unit.Pos);
                agent.Order(Abilities.MOVE, RepairMap[agent.Unit.Tag]);
            }
        }
    }
}
