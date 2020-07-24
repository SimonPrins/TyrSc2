using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Util;

namespace Tyr.Tasks
{
    class DenyScoutTask : Task
    {
        public static DenyScoutTask Task = new DenyScoutTask();
        public int StartFrame = (int)(50 * 22.4);
        public bool Done;
        private Point2D Enemy;

        public DenyScoutTask() : base(8)
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
            return agent.IsWorker && units.Count < 3 && !Done;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (Units.Count < 3)
                result.Add(new UnitDescriptor() { Pos = Bot.Bot.TargetManager.AttackTarget, Count = 3 - Units.Count, UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return Bot.Bot.Frame >= StartFrame && !Done;
        }

        public override void OnFrame(Bot tyr)
        {
            if (Enemy == null)
                Enemy = tyr.TargetManager.AttackTarget;


            if (Done)
            {
                Clear();
                return;
            }

            Unit proxyPylon = null;
            float pylonDist = 100 * 100;
            Unit probe = null;
            float dist = 1000000;
            foreach (Unit enemy in tyr.Enemies())
            {
                float newDist = SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation);
                if (enemy.UnitType == UnitTypes.PYLON && newDist < pylonDist)
                {
                    proxyPylon = enemy;
                    pylonDist = newDist;
                }
                if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;
                
                if (newDist > dist)
                    continue;
                dist = newDist;
                probe = enemy;
            }


            foreach (RecentlyDeceased deceased in tyr.EnemyManager.RecentlyDeceased)
            {
                if (deceased.UnitType == UnitTypes.PYLON)
                    Done = true;
                if (proxyPylon == null && deceased.UnitType == UnitTypes.PROBE)
                {
                    if (!Done)
                        Bot.Bot.Chat("ERROR 403: ACCESS DENIED");
                    Done = true;
                }
            }

            Point2D Ramp = tyr.MapAnalyzer.GetMainRamp();
            Point2D natural = tyr.BaseManager.Natural.BaseLocation.Pos;
            float dx = natural.X - Ramp.X;
            float dy = natural.Y - Ramp.Y;
            float size = (float)Math.Sqrt(dx * dx + dy * dy);
            float dxNormal = dy / size;
            float dyNormal = -dx / size;

            if (proxyPylon != null)
            {
                foreach (Agent agent in units)
                {
                    if (agent.DistanceSq(proxyPylon) <= 5 * 5)
                        agent.Order(Abilities.ATTACK, proxyPylon.Tag);
                    else
                        agent.Order(Abilities.MOVE, proxyPylon.Pos);
                }
                return;
            }
            int i = 0;
            foreach (Agent agent in units)
            {
                Point2D target;
                if (i == 0)
                    target = new Point2D() { X = Ramp.X + dxNormal, Y = Ramp.Y + dyNormal };
                else if (i == 1)
                    target = Ramp;
                else
                    target = new Point2D() { X = Ramp.X - dxNormal, Y = Ramp.Y - dyNormal };

                if (agent.DistanceSq(target) <= 0.25 && probe != null && agent.DistanceSq(probe) <= 1)
                    agent.Order(Abilities.ATTACK, probe.Tag);
                else agent.Order(Abilities.MOVE, target);

                i++;
            }
        }
    }
}
