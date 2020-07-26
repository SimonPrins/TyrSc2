using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;

namespace SC2Sharp.Tasks
{
    class RunbyTask : Task
    {
        public static RunbyTask Task = new RunbyTask();

        private HashSet<ulong> CloseUnits = new HashSet<ulong>();
        private HashSet<ulong> DoneUnits = new HashSet<ulong>();

        public int RequiredSize { get; set; } = 6;
        public bool Done = false;

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public RunbyTask() : base(10)
        { }

        public override bool DoWant(Agent agent)
        {
            if (DoneUnits.Contains(agent.Unit.Tag) && agent.DistanceSq(Bot.Main.TargetManager.PotentialEnemyStartLocations[0]) >= 16 * 16)
            {
                DoneUnits.Remove(agent.Unit.Tag);
                return true;
            }
            if (Done)
                return false;
            return CloseUnits.Contains(agent.Unit.Tag) && !DoneUnits.Contains(agent.Unit.Tag);
        }

        public override bool IsNeeded()
        {
            int count = 0;
            CloseUnits = new HashSet<ulong>();
            foreach (Agent agent in Bot.Main.Units())
            {
                if (!agent.IsCombatUnit)
                    continue;
                foreach (Unit enemy in Bot.Main.Enemies())
                {
                    if (enemy.UnitType != UnitTypes.BUNKER
                        && enemy.UnitType != UnitTypes.PHOTON_CANNON
                        && enemy.UnitType != UnitTypes.SPINE_CRAWLER)
                        continue;
                    if (agent.DistanceSq(enemy) < 14 * 14)
                    {
                        count++;
                        CloseUnits.Add(agent.Unit.Tag);
                        break;
                    }
                }
            }
            return count >= RequiredSize || Done;
        }

        public override void OnFrame(Bot bot)
        {
            if (units.Count >= RequiredSize)
                Done = true;
            if (units.Count <= 0)
                return;

            foreach (Agent agent in units)
            {
                agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
                if (agent.DistanceSq(bot.TargetManager.PotentialEnemyStartLocations[0]) <= 8 * 8)
                    DoneUnits.Add(agent.Unit.Tag);
            }

            for (int i = Units.Count - 1; i >= 0; i--)
                if (DoneUnits.Contains(Units[i].Unit.Tag))
                    ClearAt(i);
        }
    }
}
