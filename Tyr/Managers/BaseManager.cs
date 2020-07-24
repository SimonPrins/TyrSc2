using System.Collections.Generic;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.MapAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Managers
{
    public class BaseManager : Manager
    {
        public List<Base> Bases { get; internal set; } = new List<Base>();
        public int AvailableGasses { get; internal set; }
        public Point2D NaturalDefensePos { get; private set; }
        public Point2D MainDefensePos { get; private set; }
        public Base Main { get; private set; }
        public Base Natural { get; private set; }
        public Base Pocket { get; private set; }

        public void OnStart(Bot tyr)
        {
            int[,] distances = tyr.MapAnalyzer.Distances(SC2Util.To2D(tyr.MapAnalyzer.StartLocation));
            BaseLocation natural = null;
            int dist = 1000000000;
            foreach (BaseLocation loc in tyr.MapAnalyzer.BaseLocations)
            {
                int distanceToMain = distances[(int)loc.Pos.X, (int)loc.Pos.Y];
                Base newBase = new Base() { BaseLocation = loc, DistanceToMain = distanceToMain };
                Bases.Add(newBase);

                if (distanceToMain <= 5)
                {
                    Main = newBase;
                }
                else if (tyr.MapAnalyzer.MainAndPocketArea[loc.Pos])
                {
                    Pocket = newBase;
                    DebugUtil.WriteLine("Found pocket base at: " + Pocket.BaseLocation.Pos);
                }
                else if (distanceToMain < dist)
                {
                    natural = loc;
                    dist = distanceToMain;
                    Natural = newBase;
                }

                Point2D mineralPos = new Point2D() { X = 0, Y = 0 };
                foreach (MineralField field in loc.MineralFields)
                {
                    mineralPos.X += field.Pos.X;
                    mineralPos.Y += field.Pos.Y;
                }
                mineralPos.X /= loc.MineralFields.Count;
                mineralPos.Y /= loc.MineralFields.Count;

                newBase.MineralLinePos = mineralPos;
                newBase.OppositeMineralLinePos = new Point2D() { X = 2 * loc.Pos.X - mineralPos.X , Y = 2 * loc.Pos.Y - mineralPos.Y };

                Point2D furthest = null;
                float mineralDist = -1;
                foreach (MineralField field in loc.MineralFields)
                {
                    float newDist = SC2Util.DistanceSq(mineralPos, field.Pos);
                    if (newDist > mineralDist)
                    {
                        mineralDist = newDist;
                        furthest = SC2Util.To2D(field.Pos);
                    }
                }
                foreach (Gas field in loc.Gasses)
                {
                    float newDist = SC2Util.DistanceSq(mineralPos, field.Pos);
                    if (newDist > mineralDist)
                    {
                        mineralDist = newDist;
                        furthest = SC2Util.To2D(field.Pos);
                    }
                }
                newBase.MineralSide1 = furthest;

                furthest = null;
                mineralDist = -1;
                foreach (MineralField field in loc.MineralFields)
                {
                    float newDist = SC2Util.DistanceSq(newBase.MineralSide1, field.Pos);
                    if (newDist > mineralDist)
                    {
                        mineralDist = newDist;
                        furthest = SC2Util.To2D(field.Pos);
                    }
                }
                foreach (Gas field in loc.Gasses)
                {
                    float newDist = SC2Util.DistanceSq(newBase.MineralSide1, field.Pos);
                    if (newDist > mineralDist)
                    {
                        mineralDist = newDist;
                        furthest = SC2Util.To2D(field.Pos);
                    }
                }
                newBase.MineralSide2 = furthest;

                newBase.MineralSide1 = new Point2D() { X = (newBase.MineralSide1.X + newBase.BaseLocation.Pos.X) / 2f, Y = (newBase.MineralSide1.Y + newBase.BaseLocation.Pos.Y) / 2f };
                newBase.MineralSide2 = new Point2D() { X = (newBase.MineralSide2.X + newBase.BaseLocation.Pos.X) / 2f, Y = (newBase.MineralSide2.Y + newBase.BaseLocation.Pos.Y) / 2f };
            }

            NaturalDefensePos = tyr.MapAnalyzer.Walk(natural.Pos, tyr.MapAnalyzer.EnemyDistances, 10);
            int distToEnemy = tyr.MapAnalyzer.EnemyDistances[(int)NaturalDefensePos.X, (int)NaturalDefensePos.Y];
            int wallDist = tyr.MapAnalyzer.WallDistances[(int)NaturalDefensePos.X, (int)NaturalDefensePos.Y];
            for (int x = (int)NaturalDefensePos.X - 5; x <= NaturalDefensePos.X + 5; x++)
                for (int y = (int)NaturalDefensePos.Y - 5; y <= NaturalDefensePos.Y + 5; y++)
                {
                    if (SC2Util.DistanceSq(SC2Util.Point(x, y), natural.Pos) <= 7 * 7
                        || SC2Util.DistanceSq(SC2Util.Point(x, y), natural.Pos) >= 10 * 10)
                        continue;
                    if (tyr.MapAnalyzer.EnemyDistances[x, y] > distToEnemy)
                        continue;
                    int newDist = tyr.MapAnalyzer.WallDistances[x, y];
                    if (newDist > wallDist)
                    {
                        wallDist = newDist;
                        NaturalDefensePos = SC2Util.Point(x, y);
                    }
                }
            MainDefensePos = tyr.MapAnalyzer.Walk(SC2Util.To2D(tyr.MapAnalyzer.StartLocation), tyr.MapAnalyzer.EnemyDistances, 10);

        }

        public void OnFrame(Bot tyr)
        {
            tyr.DrawSphere(SC2Util.Point(NaturalDefensePos.X, NaturalDefensePos.Y, 10));
            AvailableGasses = 0;
            foreach (Base b in Bases)
            {
                if (b.ResourceCenter != null && !tyr.UnitManager.Agents.ContainsKey(b.ResourceCenter.Unit.Tag))
                {
                    b.ResourceCenter = null;
                    b.Owner = -1;
                }

                if (b.ResourceCenter == null)
                {
                    foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    {
                        if (!agent.IsResourceCenter)
                            continue;

                        if (SC2Util.DistanceGrid(b.BaseLocation.Pos, agent.Unit.Pos) <= 10)
                        {
                            b.ResourceCenter = agent;
                            b.Owner = b.ResourceCenter.Unit.Owner;
                            agent.Base = b;
                            break;
                        }
                    }
                }
                
                if (b.ResourceCenter == null)
                {
                    if (b.Owner == (int)tyr.PlayerId)
                        b.Owner = -1;
                    foreach (BuildRequest request in ConstructionTask.Task.BuildRequests)
                        if (UnitTypes.ResourceCenters.Contains(request.Type) && SC2Util.DistanceSq(b.BaseLocation.Pos, request.Pos) <= 2 * 2)
                        {
                            b.Owner = (int)tyr.PlayerId;
                            break;
                        }
                }

                if (b.Owner != tyr.PlayerId)
                {
                    b.Owner = -1;
                    foreach (Unit unit in tyr.Enemies())
                    {
                        // If an enemy has built near this base we set it as its owner.
                        if (UnitTypes.BuildingTypes.Contains(unit.UnitType) 
                                && SC2Util.DistanceGrid(unit.Pos, b.BaseLocation.Pos) <= 10)
                            b.Owner = unit.Owner;
                    }
                }

                b.BaseLocation.MineralFields = new List<MineralField>();
                b.BaseLocation.Gasses = new List<Gas>();
                foreach (Unit unit in tyr.Observation.Observation.RawData.Units)
                {
                    if (UnitTypes.MineralFields.Contains(unit.UnitType)
                        && SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos) <= 10 * 10)
                        b.BaseLocation.MineralFields.Add(new MineralField() { Pos = unit.Pos, Tag = unit.Tag });
                    else if (UnitTypes.GasGeysers.Contains(unit.UnitType)
                            && SC2Util.DistanceSq(unit.Pos, b.BaseLocation.Pos) <= 10 * 10)
                    {
                        bool available = b.ResourceCenter != null
                                && unit.UnitType != UnitTypes.ASSIMILATOR
                                && unit.UnitType != UnitTypes.REFINERY
                                && unit.UnitType != UnitTypes.EXTRACTOR;
                        
                        b.BaseLocation.Gasses.Add(new Gas { Pos = unit.Pos, Tag = unit.Tag, Available = available, CanBeGathered = !available, Unit = unit });
                    }
                }

                foreach (Gas gas1 in b.BaseLocation.Gasses)
                {
                    foreach (Gas gas2 in b.BaseLocation.Gasses)
                    {
                        if (gas1.Tag == gas2.Tag)
                            continue;
                        if (gas1.Pos.X == gas2.Pos.X && gas1.Pos.Y == gas2.Pos.Y)
                        {
                            gas1.Available = false;
                            gas2.Available = false;
                        }
                    }
                }
                foreach (Gas gas in b.BaseLocation.Gasses)
                    if (gas.Available)
                        AvailableGasses++;

                b.UnderAttack = false;
                b.Evacuate = false;
                int attackerCount = 0;
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (!UnitTypes.CombatUnitTypes.Contains(enemy.UnitType))
                        continue;
                    if (SC2Util.DistanceSq(enemy.Pos, b.BaseLocation.Pos) >= 20 * 20)
                        continue;
                    if (enemy.UnitType != UnitTypes.ORACLE
                        && SC2Util.DistanceSq(enemy.Pos, b.BaseLocation.Pos) >= 15 * 15)
                        continue;

                    if (enemy.UnitType == UnitTypes.ORACLE
                        || enemy.UnitType == UnitTypes.LIBERATOR_AG)
                        attackerCount += 8;

                    b.UnderAttack = true;
                    if (UnitTypes.CanAttackGround(enemy.UnitType))
                        attackerCount++;
                }
                if (attackerCount >= 8)
                    b.Evacuate = true;

                if (b.ResourceCenter == null || b.ResourceCenter.Unit.BuildProgress < 0.99)
                    b.ResourceCenterFinishedFrame = -1;
                else if (b.ResourceCenterFinishedFrame == -1)
                    b.ResourceCenterFinishedFrame = tyr.Frame;

            }


            foreach (Base b in Bases)
                if (b.Owner == -1)
                    Bot.Main.DrawSphere(b.BaseLocation.Pos);

            CheckBlockedBases();
        }

        private void CheckBlockedBases()
        {
            foreach (Base b in Bases)
            {
                if (b.Owner == Bot.Main.PlayerId
                    && b.ResourceCenter != null)
                {
                    b.Blocked = false;
                    continue;
                }

                if (b.Blocked)
                {
                    foreach (Agent agent in Bot.Main.UnitManager.Agents.Values)
                        if (agent.DistanceSq(b.BaseLocation.Pos) <= 2 * 2)
                        {
                            b.Blocked = false;
                            break;
                        }

                    if (b.Blocked)
                        continue;
                }

                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.WIDOW_MINE_BURROWED
                        && enemy.UnitType != UnitTypes.ZERGLING_BURROWED)
                        continue;

                    if (SC2Util.DistanceSq(enemy.Pos, b.BaseLocation.Pos) <= 10 * 10)
                    {
                        b.Blocked = true;
                        break;
                    }
                }
            }
            foreach (Base b in Bases)
                if (b.Blocked)
                    Bot.Main.DrawSphere(b.BaseLocation.Pos);

        }
    }
}
