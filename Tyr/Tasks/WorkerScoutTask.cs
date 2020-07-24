using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.MapAnalysis;
using Tyr.Util;

namespace Tyr.Tasks
{
    class WorkerScoutTask : Task
    {
        public static WorkerScoutTask Task = new WorkerScoutTask();
        public int StartFrame = 400;
        public bool Done;
        private float MaxDist = 8;
        public bool ScoutSent = false;
        public bool ScoutNatural = false;
        private int CheckNaturalTimeStart = 1466;
        private int CheckNaturalTimeEnd = 2016;
        private bool CheckedNatural = false;

        private List<Point2D> ScoutPoints;


        private BaseLocation EnemyNatural;

        public WorkerScoutTask() : base(8)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Bot.TaskManager.Add(Task);
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
            result.Add(new UnitDescriptor() { Pos = Bot.Bot.TargetManager.AttackTarget, Count = 1, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return Bot.Bot.Frame >= StartFrame && !ScoutSent;
        }

        public bool BaseCircled()
        {
            return ScoutPoints != null && ScoutPoints.Count == 0;
        }

        public override void OnFrame(Bot tyr)
        {
            if (tyr.TargetManager.PotentialEnemyStartLocations.Count == 1 && ScoutPoints == null)
            {
                ScoutPoints = new List<Point2D>();
                for (float dx = -15; dx <= 15; dx++)
                    for (float dy = -15; dy <= 15; dy++)
                    {
                        if (dx * dx + dy * dy <= 10 * 10)
                            continue;
                        if (dx * dx + dy * dy > 15 * 15)
                            continue;

                        ScoutPoints.Add(new Point2D() { X = tyr.TargetManager.PotentialEnemyStartLocations[0].X + dx, Y = tyr.TargetManager.PotentialEnemyStartLocations[0].Y + dy });
                    }
            }

            Point2D target = tyr.TargetManager.PotentialEnemyStartLocations[0];
            if (tyr.TargetManager.PotentialEnemyStartLocations.Count == 1 && units.Count > 0 && SC2Util.DistanceSq(units[0].Unit.Pos, target) <= 6 * 6)
                Done = true;
            
            if (!Done && tyr.EnemyManager.EnemyBuildings.Count > 0)
                Done = true;
            
            if (ScoutNatural && tyr.Frame > CheckNaturalTimeEnd)
            {
                Done = true;
                Clear();
                return;
            }

            bool scoutingNatural = ScoutNatural && tyr.Frame > CheckNaturalTimeStart && tyr.TargetManager.PotentialEnemyStartLocations.Count == 1;
            if (scoutingNatural)
            {
                GetEnemyNatural();
                target = EnemyNatural.Pos;
            }
            
            foreach (Agent agent in units)
            {
                float targetDist = (float)Math.Sqrt(SC2Util.DistanceSq(agent.Unit.Pos, target));
                if (ScoutNatural && targetDist <= 6 * 6)
                    CheckedNatural = true;
                
                ScoutSent = true;
                Point2D closest = null;
                if (Done)
                {
                    for (int i = ScoutPoints.Count - 1; i >= 0; i--)
                    {
                        Point2D scoutPoint = ScoutPoints[i];
                        if (agent.DistanceSq(scoutPoint) <= 6 * 6)
                            CollectionUtil.RemoveAt(ScoutPoints, i);
                    }
                    float dist = 1000000;
                    Point2D scoutTarget = null;
                    foreach (Point2D scoutPoint in ScoutPoints)
                    {
                        float newDist = agent.DistanceSq(scoutPoint);
                        if (newDist < dist)
                        {
                            dist = newDist;
                            scoutTarget = scoutPoint;
                        }
                    }

                    if (scoutTarget != null)
                    {
                        agent.Order(Abilities.MOVE, scoutTarget);
                        continue;
                    }
                    
                    float distance = 6 * 6;
                    foreach (Unit unit in tyr.Enemies())
                    {
                        if (!UnitTypes.CombatUnitTypes.Contains(unit.UnitType) && !UnitTypes.WorkerTypes.Contains(unit.UnitType))
                            continue;
                        float newDist = SC2Util.DistanceSq(unit.Pos, agent.Unit.Pos);
                        if (newDist < distance)
                        {
                            distance = newDist;
                            closest = SC2Util.To2D(unit.Pos);
                        }
                    }
                }
                if ((closest == null && (targetDist >= MaxDist + 1 || !Done)) || (scoutingNatural && !CheckedNatural))
                {
                    if (targetDist >= MaxDist + 1 || (scoutingNatural && !CheckedNatural))
                        agent.Order(Abilities.MOVE, target);
                }
                else
                {
                    PotentialHelper potential = new PotentialHelper(agent.Unit.Pos, 2);
                    if (targetDist <= MaxDist)
                        potential.From(target, Math.Min(1, MaxDist - targetDist));
                    else
                        potential.To(target, Math.Max(1, targetDist - MaxDist));

                    if (closest != null)
                        potential.From(closest, 2);
                    agent.Order(Abilities.MOVE, potential.Get());
                }
            }
        }

        private void GetEnemyNatural()
        {
            EnemyNatural = Bot.Bot.MapAnalyzer.GetEnemyNatural();
        }
    }
}
