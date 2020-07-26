using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class OverlordSuicideTask : Task
    {
        public static OverlordSuicideTask Task = new OverlordSuicideTask();
        public bool Suicide = false;

        public OverlordSuicideTask() : base(7)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.OVERLORD && units.Count == 0 && !Suicide;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            if (!Suicide && units.Count == 0)
                result.Add(new UnitDescriptor() { Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.OVERLORD } });
            return result;
        }

        public override bool IsNeeded()
        {
            return true;
        }

        public override void OnFrame(Bot bot)
        {
            Point2D target;
            Point2D enemyNatural = bot.MapAnalyzer.GetEnemyNatural().Pos;
            if (enemyNatural == null)
                return;

            /*
            if (Suicide)
            {
                PotentialHelper potential = new PotentialHelper(enemyNatural);
                potential.Magnitude = 5;
                potential.From(bot.MapAnalyzer.StartLocation, 1);
                target = potential.Get();
            }
            else
            {
                PotentialHelper potential = new PotentialHelper(enemyNatural);
                potential.Magnitude = 20;
                potential.From(bot.TargetManager.PotentialEnemyStartLocations[0], 1);
                target = potential.Get();
            }
            */
            if (Suicide)
            {
                target = bot.TargetManager.PotentialEnemyStartLocations[0];
            }
            else
            {
                PotentialHelper potential = new PotentialHelper(bot.TargetManager.PotentialEnemyStartLocations[0]);
                potential.Magnitude = 20;
                potential.From(enemyNatural, 1);
                target = potential.Get();
            }

            foreach (Agent agent in units)
            {
                bool closeEnemy = false;
                foreach (Unit enemy in bot.Enemies())
                {
                    if (Suicide && !UnitTypes.AirAttackTypes.Contains(enemy.UnitType))
                        continue;
                    if (agent.DistanceSq(enemy) <= (Suicide ? 6 * 6 : 15 * 15))
                    {
                        agent.Order(Abilities.MOVE, agent.From(enemy, 2));
                        closeEnemy = true;
                        break;
                    }
                }
                if (closeEnemy)
                    continue;

                if (SC2Util.DistanceSq(agent.Unit.Pos, target) >= 1)
                {
                    if (!Suicide)
                    {
                        float dist = agent.DistanceSq(bot.TargetManager.PotentialEnemyStartLocations[0]);
                        if (dist <= 40 * 40 && dist >= 6 * 6)
                        {
                            PotentialHelper helper = new PotentialHelper(agent.Unit.Pos, 4);
                            helper.To(target, 2);
                            helper.From(bot.TargetManager.PotentialEnemyStartLocations[0], 1);
                            agent.Order(Abilities.MOVE, helper.Get());
                            continue;
                        }
                        agent.Order(Abilities.MOVE, target);
                        continue;
                    }
                    agent.Order(Abilities.MOVE, target);
                }
            }
        }
    }
}
