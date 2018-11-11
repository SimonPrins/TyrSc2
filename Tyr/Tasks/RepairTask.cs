using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Tasks
{
    class RepairTask : Task
    {
        public static RepairTask Task = new RepairTask();
        public List<ulong> NeedsRepairing = new List<ulong>();
        Dictionary<ulong, int> AlreadyRepairing = new Dictionary<ulong, int>();
        private int NeedsRepairingUpdateFrame = -1;

        private Dictionary<ulong, ulong> RepairMap = new Dictionary<ulong, ulong>();

        public static void Enable()
        {
            Enable(Task);
        }

        public RepairTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            UpdateNeedsRepairing();
            return agent.Unit.UnitType == UnitTypes.SCV && Units.Count < NeedsRepairing.Count * 3;
        }

        public override bool IsNeeded()
        {
            UpdateNeedsRepairing();
            return NeedsRepairing.Count > 0;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            UpdateNeedsRepairing();
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            foreach (ulong building in NeedsRepairing)
            {
                result.Add(new UnitDescriptor() {
                    Pos = SC2Util.To2D(Tyr.Bot.UnitManager.Agents[building].Unit.Pos),
                    Count = AlreadyRepairing.ContainsKey(building) ? (3 - AlreadyRepairing[building]) : 3,
                    UnitTypes = new HashSet<uint>() { UnitTypes.SCV },
                    Marker = building,
                    MaxDist = 40
                });
            }
            return result;
        }

        public override void Add(Agent agent, UnitDescriptor descriptor)
        {
            base.Add(agent, descriptor);
            RepairMap.Add(agent.Unit.Tag, (ulong)descriptor.Marker);
        }

        private void UpdateNeedsRepairing()
        {
            if (NeedsRepairingUpdateFrame >= Tyr.Bot.Frame)
                return;
            NeedsRepairingUpdateFrame = Tyr.Bot.Frame;

            AlreadyRepairing = new Dictionary<ulong, int>();
            NeedsRepairing = new List<ulong>();
            foreach (Agent agent in Tyr.Bot.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType != UnitTypes.BUNKER
                    && agent.Unit.UnitType != UnitTypes.PLANETARY_FORTRESS)
                    continue;

                if (agent.Unit.BuildProgress < 0.99)
                    continue;

                if (agent.Unit.Health < agent.Unit.HealthMax)
                    NeedsRepairing.Add(agent.Unit.Tag);
            }

            foreach (Agent scv in Units)
                if (RepairMap.ContainsKey(scv.Unit.Tag))
                {
                    ulong repairedBuilding = RepairMap[scv.Unit.Tag];
                    if (AlreadyRepairing.ContainsKey(repairedBuilding))
                        AlreadyRepairing[repairedBuilding]++;
                    else
                        AlreadyRepairing.Add(repairedBuilding, 1);
                }

        }

        public override void OnFrame(Tyr tyr)
        {
            List<Agent> unassignedSCVs = new List<Agent>();
            Dictionary<ulong, int> alreadyRepairing = new Dictionary<ulong, int>();
            foreach (Agent agent in Units)
            {
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
                while (alreadyRepairing[tag] < 3
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
                agent.Order(316, RepairMap[agent.Unit.Tag]);
        }
    }
}
