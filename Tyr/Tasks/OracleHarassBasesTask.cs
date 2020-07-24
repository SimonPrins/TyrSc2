using System;
using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.Util;

namespace Tyr.Tasks
{
    class OracleHarassBasesTask : Task
    {
        public static OracleHarassBasesTask Task = new OracleHarassBasesTask();

        public int RequiredSize { get; set; } = 6;

        Point2D Target = null;

        bool MoveToSide = true;
        bool MoveToMain = true;
        Point2D SideTarget;

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Bot.TaskManager.Add(Task);
        }

        public OracleHarassBasesTask() : base(8)
        { }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.ORACLE;
        }

        public override bool IsNeeded()
        {
            return Bot.Bot.UnitManager.Completed(UnitTypes.ORACLE) >= RequiredSize;
        }

        public override void OnFrame(Bot tyr)
        {
            if (Target == null)
                Target = tyr.TargetManager.PotentialEnemyStartLocations[0];

            if (MoveToSide)
            {
                GetSideTarget();
                if (SideTarget != null)
                {
                    foreach (Agent agent in Units)
                    {
                        agent.Order(Abilities.MOVE, SideTarget);
                        if (agent.DistanceSq(SideTarget) <= 4 * 4)
                            MoveToSide = false;
                    }
                }
                return;
            }

            if (MoveToMain)
            {
                foreach (Agent agent in Units)
                {
                    agent.Order(Abilities.MOVE, Target);
                    if (agent.DistanceSq(Target) <= 6 * 6)
                        MoveToMain = false;
                }
                return;
            }

            int enemyCount = 0;
            foreach (Unit enemy in tyr.Enemies())
            {
                if (!UnitTypes.AirAttackTypes.Contains(enemy.UnitType))
                    continue;

                if (SC2Util.DistanceSq(enemy.Pos, Target) <= 12 * 12)
                    enemyCount++;
            }
            foreach (UnitLocation mine in tyr.EnemyMineManager.Mines)
                if (SC2Util.DistanceSq(mine.Pos, Target) <= 12 * 12)
                    enemyCount++;

            if (enemyCount >= 6)
            {
                DebugUtil.WriteLine("Switching targets.");
                foreach (Base b in tyr.BaseManager.Bases)
                {
                    if (SC2Util.DistanceSq(b.BaseLocation.Pos, Target) <= 4)
                        continue;

                    if (b.Owner == tyr.PlayerId || b.Owner == -1)
                        continue;

                    Target = b.BaseLocation.Pos;
                    DebugUtil.WriteLine("Target chosen: " + Target);
                    break;
                }
            }

            foreach (Agent agent in units)
                Attack(agent, tyr.TargetManager.AttackTarget);
        }

        private void GetSideTarget()
        {
            if (SideTarget != null)
                return;

            Point2D enemyNatural = Bot.Bot.MapAnalyzer.GetEnemyNatural().Pos;
            if (enemyNatural == null)
                return;
            Point2D target = null;
            float dist = 0;
            foreach (Base b in Bot.Bot.BaseManager.Bases)
            {
                if (SC2Util.DistanceSq(Bot.Bot.TargetManager.PotentialEnemyStartLocations[0], b.BaseLocation.Pos) >= 60 * 60)
                    continue;
                float newDist = SC2Util.DistanceSq(enemyNatural, b.BaseLocation.Pos);
                if (newDist > dist)
                {
                    dist = newDist;
                    target = b.BaseLocation.Pos;
                }
            }
            
            SideTarget = target;
        }
    }
}
