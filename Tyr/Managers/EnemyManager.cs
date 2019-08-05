using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Util;

namespace Tyr.Managers
{
    public class EnemyManager : Manager
    {
        public Dictionary<ulong, BuildingLocation> EnemyBuildings = new Dictionary<ulong, BuildingLocation>();
        private List<Unit> Enemies;
        private int EnemiesFrame = -1;
        public Queue<RecentlyDeceased> RecentlyDeceased = new Queue<RecentlyDeceased>();
        public Dictionary<ulong, Unit> LastSeen = new Dictionary<ulong, Unit>();
        public Dictionary<ulong, int> LastSeenFrame = new Dictionary<ulong, int>();

        public List<Unit> GetEnemies()
        {
            Update();
            return Enemies;
        }

        public Queue<RecentlyDeceased> GetRecentlyDeceased()
        {
            Update();
            return RecentlyDeceased;
        }

        private void Update()
        {
            if (Tyr.Bot.Frame > EnemiesFrame)
            {
                if (Tyr.Bot.Observation.Observation.RawData.Event != null
                    && Tyr.Bot.Observation.Observation.RawData.Event.DeadUnits != null)
                    foreach (ulong tag in Tyr.Bot.Observation.Observation.RawData.Event.DeadUnits)
                    {
                        foreach (Unit unit in Enemies)
                            if (unit.Tag == tag)
                            {
                                RecentlyDeceased.Enqueue(new RecentlyDeceased() { UnitType = unit.UnitType, Pos = unit.Pos, Frame = Tyr.Bot.Frame });
                                break;
                            }
                    }

                while (RecentlyDeceased.Count > 0 && RecentlyDeceased.Peek().Frame < Tyr.Bot.Frame - 66)
                    RecentlyDeceased.Dequeue();

                EnemiesFrame = Tyr.Bot.Frame;

                Enemies = new List<Unit>();

                foreach (Unit unit in Tyr.Bot.Observation.Observation.RawData.Units)
                    if (unit.Alliance == Alliance.Enemy)
                        Enemies.Add(unit);
            }
        }

        public void OnFrame(Tyr tyr)
        {
            if (Tyr.Bot.Observation.Observation.RawData.Event != null
                && Tyr.Bot.Observation.Observation.RawData.Event.DeadUnits != null)
                foreach (ulong tag in Tyr.Bot.Observation.Observation.RawData.Event.DeadUnits)
                {
                    if (LastSeen.ContainsKey(tag))
                        LastSeen.Remove(tag);
                    if (LastSeenFrame.ContainsKey(tag))
                        LastSeenFrame.Remove(tag);
                }
            List<ulong> destroyedBuildings = new List<ulong>();
            foreach (BuildingLocation location in EnemyBuildings.Values)
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (SC2Util.DistanceSq(agent.Unit.Pos, SC2Util.To2D(location.Pos)) <= 6 * 6)
                        destroyedBuildings.Add(location.Tag);
            foreach (ulong tag in destroyedBuildings)
                EnemyBuildings.Remove(tag);

            foreach (Unit unit in Tyr.Bot.Enemies())
            {
                CollectionUtil.Add(LastSeen, unit.Tag, unit);
                CollectionUtil.Add(LastSeenFrame, unit.Tag, tyr.Frame);

                if (!UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    continue;

                if (EnemyBuildings.ContainsKey(unit.Tag))
                    EnemyBuildings[unit.Tag] = new BuildingLocation() { Tag = unit.Tag, Pos = unit.Pos, Type = unit.UnitType, LastSeen = Tyr.Bot.Frame };
                else
                    EnemyBuildings.Add(unit.Tag, new BuildingLocation() { Tag = unit.Tag, Pos = unit.Pos, Type = unit.UnitType, LastSeen = Tyr.Bot.Frame });
            }
        }
    }
}
