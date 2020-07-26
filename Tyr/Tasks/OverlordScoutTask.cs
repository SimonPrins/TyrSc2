using SC2APIProtocol;
using System;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class OverlordScoutTask : Task
    {
        public static OverlordScoutTask Task = new OverlordScoutTask();
        public bool Done;
        public bool ScoutSent = false;
        public bool ScoutMain = false;
        public Point2D ScoutLocation = null;

        private BaseLocation EnemyNatural;

        public OverlordScoutTask() : base(8)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return agent.Unit.UnitType == UnitTypes.OVERLORD && units.Count == 0 && !Done;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { Pos = Bot.Main.TargetManager.AttackTarget, Count = 1, UnitTypes = new HashSet<uint>() { UnitTypes.OVERLORD } });
            return result;
        }

        public override bool IsNeeded()
        {
            return !ScoutSent;
        }

        public override void OnFrame(Bot bot)
        {
            Point2D target = bot.TargetManager.PotentialEnemyStartLocations[0];
            bool scoutingNatural = bot.TargetManager.PotentialEnemyStartLocations.Count == 1 && !ScoutMain;

            if (ScoutLocation != null)
                target = ScoutLocation;
            else if (scoutingNatural)
            {
                GetEnemyNatural();
                target = EnemyNatural.Pos;
            }

            foreach (Agent agent in units)
            {
                if (agent.Unit.Health < agent.Unit.HealthMax - 20)
                    Done = true;

                if (SC2Util.DistanceSq(agent.Unit.Pos, target) >= 3 * 3)
                    agent.Order(Abilities.MOVE, target);
            }
        }

        private void GetEnemyNatural()
        {
            EnemyNatural = Bot.Main.MapAnalyzer.GetEnemyNatural();
        }
    }
}
