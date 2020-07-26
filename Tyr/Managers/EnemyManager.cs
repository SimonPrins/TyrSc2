using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Managers
{
    public class EnemyManager : Manager
    {
        public Dictionary<ulong, BuildingLocation> EnemyBuildings = new Dictionary<ulong, BuildingLocation>();
        private List<Unit> Enemies;
        private List<Unit> CloakedEnemies;
        private int EnemiesFrame = -1;
        public Queue<RecentlyDeceased> RecentlyDeceased = new Queue<RecentlyDeceased>();
        public Dictionary<ulong, Unit> LastSeen = new Dictionary<ulong, Unit>();
        public Dictionary<ulong, int> LastSeenFrame = new Dictionary<ulong, int>();

        public List<Unit> GetEnemies()
        {
            Update();
            return Enemies;
        }
        public List<Unit> GetCloakedEnemies()
        {
            Update();
            return CloakedEnemies;
        }

        public Queue<RecentlyDeceased> GetRecentlyDeceased()
        {
            Update();
            return RecentlyDeceased;
        }

        private void Update()
        {
            if (Bot.Main.Frame > EnemiesFrame)
            {
                if (Bot.Main.Observation.Observation.RawData.Event != null
                    && Bot.Main.Observation.Observation.RawData.Event.DeadUnits != null)
                    foreach (ulong tag in Bot.Main.Observation.Observation.RawData.Event.DeadUnits)
                    {
                        foreach (Unit unit in Enemies)
                            if (unit.Tag == tag)
                            {
                                RecentlyDeceased.Enqueue(new RecentlyDeceased() { UnitType = unit.UnitType, Pos = unit.Pos, Frame = Bot.Main.Frame });
                                break;
                            }
                    }

                while (RecentlyDeceased.Count > 0 && RecentlyDeceased.Peek().Frame < Bot.Main.Frame - 66)
                    RecentlyDeceased.Dequeue();

                EnemiesFrame = Bot.Main.Frame;

                Enemies = new List<Unit>();
                CloakedEnemies = new List<Unit>();

                foreach (Unit unit in Bot.Main.Observation.Observation.RawData.Units)
                {
                    if (unit.Alliance == Alliance.Enemy)
                    {
                        if (unit.Cloak == CloakState.Cloaked || unit.Cloak == CloakState.CloakedDetected)
                            CloakedEnemies.Add(unit);
                        if (unit.Cloak != CloakState.Cloaked)
                            Enemies.Add(unit);
                    }
                }
            }
        }

        public void OnFrame(Bot bot)
        {
            if (Bot.Main.Observation.Observation.RawData.Event != null
                && Bot.Main.Observation.Observation.RawData.Event.DeadUnits != null)
                foreach (ulong tag in Bot.Main.Observation.Observation.RawData.Event.DeadUnits)
                {
                    if (LastSeen.ContainsKey(tag))
                        LastSeen.Remove(tag);
                    if (LastSeenFrame.ContainsKey(tag))
                        LastSeenFrame.Remove(tag);
                    if (EnemyBuildings.ContainsKey(tag))
                        EnemyBuildings.Remove(tag);
                }
            List<ulong> destroyedBuildings = new List<ulong>();
            foreach (BuildingLocation location in EnemyBuildings.Values)
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                    if (SC2Util.DistanceSq(agent.Unit.Pos, SC2Util.To2D(location.Pos)) <= 6 * 6)
                        destroyedBuildings.Add(location.Tag);
            foreach (ulong tag in destroyedBuildings)
                EnemyBuildings.Remove(tag);

            foreach (Unit unit in Bot.Main.Enemies())
            {
                CollectionUtil.Add(LastSeen, unit.Tag, unit);
                CollectionUtil.Add(LastSeenFrame, unit.Tag, bot.Frame);

                if (!UnitTypes.BuildingTypes.Contains(unit.UnitType))
                    continue;

                if (EnemyBuildings.ContainsKey(unit.Tag))
                    EnemyBuildings[unit.Tag] = new BuildingLocation() { Tag = unit.Tag, Pos = unit.Pos, Type = unit.UnitType, LastSeen = Bot.Main.Frame, Flying = unit.IsFlying };
                else
                    EnemyBuildings.Add(unit.Tag, new BuildingLocation() { Tag = unit.Tag, Pos = unit.Pos, Type = unit.UnitType, LastSeen = Bot.Main.Frame, Flying = unit.IsFlying });
            }
        }
    }
}
