using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Util;

namespace SC2Sharp.Managers
{
    public class TargetManager : Manager
    {
        public List<Point2D> PotentialEnemyStartLocations = new List<Point2D>();
        private ulong targetUnitTag = 0;
        bool enemyMainFound = false;
        public bool PrefferDistant { get; set; } = true;
        public bool IncludeAllEnemies = true;

        public bool TargetAllBuildings = false;
        public bool TargetCannons = false;
        public bool TargetGateways = false;
        public bool SkipPlanetaries = false;
        public bool IgnoreFlyingBuildings = false;
        public Point2D CloseTo;

        public void OnFrame(Bot bot)
        {
            if (PotentialEnemyStartLocations.Count > 1 && !enemyMainFound)
            {
                for (int i = PotentialEnemyStartLocations.Count - 1; i >= 0; i--)
                    foreach (Unit unit in bot.Enemies())
                        if (SC2Util.DistanceSq(unit.Pos, PotentialEnemyStartLocations[i]) <= 6 * 6)
                        {
                            for (; i > 0; i--)
                                PotentialEnemyStartLocations.RemoveAt(0);
                            while (PotentialEnemyStartLocations.Count > 1)
                                PotentialEnemyStartLocations.RemoveAt(PotentialEnemyStartLocations.Count - 1);
                        }
            }
            if (PotentialEnemyStartLocations.Count > 1)
            {
                for (int i = PotentialEnemyStartLocations.Count - 1; i >= 0; i--)
                    foreach (Unit unit in bot.Observation.Observation.RawData.Units)
                        if (unit.Owner == bot.PlayerId && SC2Util.DistanceGrid(unit.Pos, PotentialEnemyStartLocations[i]) <= 5)
                        {
                            PotentialEnemyStartLocations.RemoveAt(i);
                            break;
                        }
            }

            if (PotentialEnemyStartLocations.Count == 1)
                enemyMainFound = true;

            if (PotentialEnemyStartLocations.Count == 1)
            {
                float dist = PrefferDistant ? -1 : 1000000;
                BuildingLocation target = null;
                foreach (BuildingLocation building in bot.EnemyManager.EnemyBuildings.Values)
                {
                    if (SkipPlanetaries && building.Type == UnitTypes.PLANETARY_FORTRESS)
                        continue;
                    if (UnitTypes.ResourceCenters.Contains(building.Type)
                        || (TargetCannons && building.Type == UnitTypes.PHOTON_CANNON)
                        || (TargetGateways && building.Type == UnitTypes.GATEWAY)
                        || (TargetGateways && building.Type == UnitTypes.WARP_GATE)
                        || TargetAllBuildings)
                    {
                        float newDist = SC2Util.DistanceSq(building.Pos, CloseTo == null ? PotentialEnemyStartLocations[0] : CloseTo);
                        if ((PrefferDistant && newDist > dist)
                            || (!PrefferDistant && newDist < dist))
                        {
                            dist = newDist;
                            target = building;
                        }
                    }
                }

                if (target != null)
                {
                    AttackTarget = SC2Util.To2D(target.Pos);
                    targetUnitTag = target.Tag;
                }
            }

            Point2D lastTarget = AttackTarget;

            if (!bot.EnemyManager.EnemyBuildings.ContainsKey(targetUnitTag) || bot.EnemyManager.EnemyBuildings[targetUnitTag].Flying)
            {
                AttackTarget = null;
                targetUnitTag = 0;
                foreach (BuildingLocation enemyBuilding in bot.EnemyManager.EnemyBuildings.Values)
                {
                    if (enemyBuilding.Flying && IgnoreFlyingBuildings)
                        continue;
                    AttackTarget = SC2Util.To2D(enemyBuilding.Pos);
                    targetUnitTag = enemyBuilding.Tag;
                    break;
                }

                if (AttackTarget == null)
                {
                    float dist = 1000000;
                    foreach (Point2D location in PotentialEnemyStartLocations)
                    {
                        if (lastTarget == null)
                        {
                            AttackTarget = location;
                            break;
                        }

                        float newDist = SC2Util.DistanceSq(lastTarget, location);
                        if (newDist < dist)
                        {
                            dist = newDist;
                            AttackTarget = location;
                        }
                    }
                }
            }
            else AttackTarget = SC2Util.To2D(bot.EnemyManager.EnemyBuildings[targetUnitTag].Pos);

            if (IncludeAllEnemies && CloseTo != null)
            {
                float dist = 15 * 15;
                foreach (Unit enemy in bot.Enemies())
                {
                    float newDist = SC2Util.DistanceSq(enemy.Pos, CloseTo);
                    if (newDist >= dist)
                        continue;
                    dist = newDist;
                    AttackTarget = SC2Util.To2D(enemy.Pos);
                }
            }

            if (bot.EnemyManager.EnemyBuildings.Count == 0 && PotentialEnemyStartLocations.Count == 1)
            {
                bool cleared = false;
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                {
                    if (SC2Util.DistanceSq(agent.Unit.Pos, PotentialEnemyStartLocations[0]) <= 6 * 6)
                    {
                        cleared = true;
                        break;
                    }
                }
                if (cleared)
                {
                    PotentialEnemyStartLocations.RemoveAt(0);
                    foreach (Base b in bot.BaseManager.Bases)
                        PotentialEnemyStartLocations.Add(b.BaseLocation.Pos);
                }
            }
        }

        public void OnStart(Bot bot)
        {
            foreach (Point2D location in bot.GameInfo.StartRaw.StartLocations)
                if (SC2Util.DistanceGrid(bot. MapAnalyzer.StartLocation, location) > 20)
                    PotentialEnemyStartLocations.Add(location);
            DebugUtil.WriteLine("Enemy locations: " + PotentialEnemyStartLocations.Count);
        }

        public Point2D AttackTarget { get; internal set; }
    }
}
