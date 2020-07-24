using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Util;

namespace Tyr.Tasks
{
    class HuntProxyTask : Task
    {
        public static HuntProxyTask Task = new HuntProxyTask();
        public int StartFrame = (int)(10 * 22.4);
        public bool Done;
        public bool AddMidwayPoint = true;
        public bool CloseBasesFirst = true;
        public List<Point2D> ScoutBases;
        private List<Point2D> NextRoundBases = new List<Point2D>();
        private Point2D Enemy;
        private ulong EnemyTag;
        private int EnemyFrame = -200;
        public bool KeepCycling = false;

        public HuntProxyTask() : base(8)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            if (BuildingType.BuildingAbilities.Contains((int)agent.CurrentAbility()))
                return false;
            return agent.IsWorker && units.Count == 0 && !Done;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (Units.Count == 0)
                result.Add(new UnitDescriptor() { Pos = Bot.Main.TargetManager.AttackTarget, Count = 1, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return Bot.Main.Frame >= StartFrame && !Done;
        }

        public override void OnFrame(Bot tyr)
        {
            foreach (RecentlyDeceased deceased in tyr.EnemyManager.RecentlyDeceased)
                if (deceased.UnitType == UnitTypes.PYLON)
                    Done = true;

            if (Done)
            {
                Clear();
                return;
            }
            if (ScoutBases == null)
            {
                ScoutBases = new List<Point2D>();
                foreach (Base b in tyr.BaseManager.Bases)
                {
                    float mainDist = SC2Util.DistanceSq(b.BaseLocation.Pos, tyr.MapAnalyzer.StartLocation);
                    if (mainDist >= 60 * 60 || mainDist <= 8 * 8)
                        continue;
                    ScoutBases.Add(b.BaseLocation.Pos);
                }
                if (CloseBasesFirst)
                    ScoutBases.Sort((Point2D a, Point2D b) => tyr.MapAnalyzer.MainDistances[(int)a.X, (int)a.Y] - tyr.MapAnalyzer.MainDistances[(int)b.X, (int)b.Y]);
                else
                    ScoutBases.Sort((Point2D a, Point2D b) => tyr.MapAnalyzer.EnemyDistances[(int)a.X, (int)a.Y] - tyr.MapAnalyzer.EnemyDistances[(int)b.X, (int)b.Y]);
                if (AddMidwayPoint)
                    ScoutBases.Insert(0, new PotentialHelper(tyr.MapAnalyzer.StartLocation, 60).To(tyr.TargetManager.PotentialEnemyStartLocations[0]).Get());
            }

            if (ScoutBases.Count == 0)
            {
                if (KeepCycling)
                {
                    ScoutBases = NextRoundBases;
                    NextRoundBases = new List<Point2D>();
                }
                else
                {
                    Done = true;
                    Clear();
                    return;
                }
            }
            Unit proxyPylon = null;
            float pylonDist = 100 * 100;
            Unit enemyWorker = null;
            float dist = 10000;
            if (EnemyTag != 0 && tyr.Frame - EnemyFrame >= 10)
                EnemyTag = 0;
            foreach (Unit enemy in tyr.Enemies())
            {
                float newDist = SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation);
                if (enemy.UnitType == UnitTypes.PYLON && newDist < pylonDist)
                {
                    proxyPylon = enemy;
                    pylonDist = newDist;
                    ScoutBases = new List<Point2D>();
                }
                if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;

                if (newDist < 100 * 100
                    && enemy.Tag == EnemyTag)
                    newDist = 0;

                if (newDist > dist)
                    continue;
                bool proxyClose = false;
                foreach (Unit proxy in tyr.Enemies())
                {
                    if (proxy.UnitType != UnitTypes.BARRACKS
                        && proxy.UnitType != UnitTypes.FACTORY)
                        continue;
                    if (proxy.BuildProgress >= 0.95
                        || proxy.Health >= 0.95 * proxy.HealthMax)
                    {
                        continue;
                    }
                    if (SC2Util.DistanceSq(proxy.Pos, enemy.Pos) >= 3 * 3)
                        continue;
                    proxyClose = true;
                    break;
                }
                if (!proxyClose)
                    continue;

                dist = newDist;
                enemyWorker = enemy;
                EnemyFrame = tyr.Frame;
                if (Units.Count > 0)
                    Enemy = new PotentialHelper(enemyWorker.Pos, 2).From(Units[0].Unit.Pos).Get();
                else
                    Enemy = SC2Util.To2D(enemyWorker.Pos);
            }
            if (enemyWorker != null)
                EnemyTag = enemyWorker.Tag;

            foreach (Agent agent in units)
            {
                if (enemyWorker != null)
                {
                    //if (agent.DistanceSq(enemyWorker) <= 4 * 4 && Tyr.Bot.Build.TotalEnemyCount(UnitTypes.BARRACKS) < 2)
                    //    agent.Order(Abilities.MOVE, new PotentialHelper(enemyWorker.Pos, 3).To(agent.Unit.Pos).Get());
                    //else
                        agent.Order(Abilities.ATTACK, enemyWorker.Tag);
                }
                else if (tyr.Frame - EnemyFrame <= 45 && Enemy != null)
                    agent.Order(Abilities.MOVE, Enemy);
                else if (proxyPylon != null)
                    agent.Order(Abilities.ATTACK, proxyPylon.Tag);
                else
                {
                    if (agent.DistanceSq(ScoutBases[0]) <= 4 * 4)
                    {
                        NextRoundBases.Add(ScoutBases[0]);
                        ScoutBases.RemoveAt(0);
                        if (ScoutBases.Count == 0)
                        {
                            if (KeepCycling)
                            {
                                ScoutBases = NextRoundBases;
                                NextRoundBases = new List<Point2D>();
                            }
                            else
                            {
                                Done = true;
                                Clear();
                                return;
                            }
                        }
                    }
                    agent.Order(Abilities.MOVE, ScoutBases[0]);
                }
            }
        }
        public void ClearNextRoundBases()
        {
            NextRoundBases = new List<Point2D>();
        }
    }
}
