using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.MapAnalysis;
using Tyr.Util;

namespace Tyr.Tasks
{
    class RepairTask : Task
    {
        public static RepairTask Task = new RepairTask();
        public List<ulong> NeedsRepairing = new List<ulong>();
        public HashSet<ulong> NeedsExtraRepairing = new HashSet<ulong>();
        Dictionary<ulong, int> AlreadyRepairing = new Dictionary<ulong, int>();
        private int NeedsRepairingUpdateFrame = -1;

        public bool RepairTurrets = false;

        private Dictionary<ulong, ulong> RepairMap = new Dictionary<ulong, ulong>();

        public WallInCreator WallIn;

        public static void Enable()
        {
            Enable(Task);
        }

        public RepairTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            if (BuildingType.BuildingAbilities.Contains((int)agent.CurrentAbility()))
                return false;
            UpdateNeedsRepairing();
            return agent.Unit.UnitType == UnitTypes.SCV && Units.Count < NeedsRepairing.Count * 3 + NeedsExtraRepairing.Count * 2;
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
                int needed = NeedsExtraRepairing.Contains(building) ? 5 : 3;
                result.Add(new UnitDescriptor() {
                    Pos = SC2Util.To2D(Bot.Main.UnitManager.Agents[building].Unit.Pos),
                    Count = AlreadyRepairing.ContainsKey(building) ? (needed - AlreadyRepairing[building]) : needed,
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
            if (NeedsRepairingUpdateFrame >= Bot.Main.Frame)
                return;
            NeedsRepairingUpdateFrame = Bot.Main.Frame;

            AlreadyRepairing = new Dictionary<ulong, int>();
            NeedsRepairing = new List<ulong>();

            List<ulong> noLongerRepairing = new List<ulong>();
            foreach (ulong tag in NeedsExtraRepairing)
                if (!Bot.Main.UnitManager.Agents.ContainsKey(tag) || Bot.Main.UnitManager.Agents[tag].Unit.Health == Bot.Main.UnitManager.Agents[tag].Unit.HealthMax)
                    noLongerRepairing.Add(tag);
            foreach (ulong tag in noLongerRepairing)
                NeedsExtraRepairing.Remove(tag);

            bool wallCompleted = WallCompleted();

            foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType != UnitTypes.BUNKER
                    && agent.Unit.UnitType != UnitTypes.PLANETARY_FORTRESS
                    && (!PartOfWall(agent) || !wallCompleted)
                    && (agent.Unit.UnitType != UnitTypes.MISSILE_TURRET || !RepairTurrets))
                    continue;

                if (agent.Unit.BuildProgress < 0.99)
                    continue;

                if (agent.Unit.Health < agent.Unit.HealthMax)
                    NeedsRepairing.Add(agent.Unit.Tag);
                if (agent.Unit.Health <= agent.Unit.HealthMax / 2f)
                    NeedsExtraRepairing.Add(agent.Unit.Tag);
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

        private bool WallCompleted()
        {
            if (WallIn == null)
                return false;
            int count = 0;
            foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
            {
                if (PartOfWall(agent))
                    count++;
                if (count == WallIn.Wall.Count)
                    return true;
            }
            return false;
        }

        private bool PartOfWall(Agent agent)
        {
            if (WallIn == null)
                return false;
            foreach (WallBuilding building in WallIn.Wall)
                if (agent.Unit.UnitType == building.Type && agent.DistanceSq(building.Pos) < 2)
                    return true;
            return false;
        }


        public override void OnFrame(Bot tyr)
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
                while (alreadyRepairing[tag] < (NeedsExtraRepairing.Contains(tag) ? 5 : 3)
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
