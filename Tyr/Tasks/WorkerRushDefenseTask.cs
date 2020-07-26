using System.Collections.Generic;
using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Util;

namespace SC2Sharp.Tasks
{
    class WorkerRushDefenseTask : Task
    {
        public static WorkerRushDefenseTask Task = new WorkerRushDefenseTask();
        private MineralField mineral = null;

        private int WorkerRushHappeningFrame = 0;
        public bool WorkerRushHappening = false;

        private int State;
        private static int GatherDefenders = 0;
        private static int Defend = 1;

        private int GatherDefendersStartFrame = 0;

        HashSet<ulong> SurroundingWorkers = new HashSet<ulong>();

        private bool Chasing = false;

        public WorkerRushDefenseTask() : base(10)
        { }

        public static void Enable()
        {
            Task.Stopped = false;
            Bot.Main.TaskManager.Add(Task);
        }

        public override bool DoWant(Agent agent)
        {
            return true;
        }

        private bool GetWorkerRushHappening()
        {
            if (Bot.Main.Frame == WorkerRushHappeningFrame)
                return WorkerRushHappening;

            WorkerRushHappeningFrame = Bot.Main.Frame;

            int invadingWorkers = 0;
            int dist = WorkerRushHappening ? 80 * 80 : 30 * 30;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;

                if (SC2Util.DistanceSq(enemy.Pos, Bot.Main.MapAnalyzer.StartLocation) <= dist)
                    invadingWorkers++;
            }
            if (!WorkerRushHappening && invadingWorkers >= 8)
            {
                WorkerRushHappening = true;
                State = GatherDefenders;
                GatherDefendersStartFrame = Bot.Main.Frame;
            }

            if (WorkerRushHappening && invadingWorkers == 0)
                WorkerRushHappening = false;

            Bot.Main.DrawText("Invading workers: " + invadingWorkers);
            Bot.Main.DrawText("Workerrush happening: " + WorkerRushHappening);

            return WorkerRushHappening;
        }

        public override List<UnitDescriptor> GetDescriptors()
        {
            List<UnitDescriptor> result = new List<UnitDescriptor>();
            result.Add(new UnitDescriptor() { UnitTypes = UnitTypes.WorkerTypes });
            return result;
        }

        public override bool IsNeeded()
        {
            return GetWorkerRushHappening();
        }

        public override void OnFrame(Bot bot)
        {
            if (!GetWorkerRushHappening())
            {
                Clear();
                return;
            }

            if (State == GatherDefenders)
                ExecuteGatherDefenders(bot);
            else
                ExecuteDefend(bot);
        }

        public void ExecuteGatherDefenders(Bot bot)
        {
            if (mineral == null && bot.BaseManager.Main.BaseLocation.MineralFields.Count > 0)
                mineral = bot.BaseManager.Main.BaseLocation.MineralFields[0];

            if (mineral == null)
            {
                State = Defend;
                return;
            }

            foreach (Agent agent in Units)
                agent.Order(Abilities.MOVE, mineral.Tag);

            if (bot.Frame >= GatherDefendersStartFrame + 112)
                State = Defend;
        }

        public void ExecuteDefend(Bot bot)
        {
            bool surround = (bot.Frame - GatherDefendersStartFrame - 110) % 23 < 8;

            int surroundingWorkersCount = 0;
            foreach (Agent agent in units)
                if (SurroundingWorkers.Contains(agent.Unit.Tag))
                    surroundingWorkersCount++;

            int desiredSurroundingWorkers = units.Count / 3 + 1;
            foreach (Agent agent in units)
            {
                if (surroundingWorkersCount == desiredSurroundingWorkers)
                    break;


                if (surroundingWorkersCount < desiredSurroundingWorkers
                    && !SurroundingWorkers.Contains(agent.Unit.Tag))
                    SurroundingWorkers.Add(agent.Unit.Tag);
                else if (surroundingWorkersCount > desiredSurroundingWorkers
                    && SurroundingWorkers.Contains(agent.Unit.Tag))
                    SurroundingWorkers.Remove(agent.Unit.Tag);
            }
            
            Unit closestEnemy = null;
            float distance = Chasing ? 80 * 80 : 70 * 70;
            float hp = 1000;
            foreach (Unit enemy in bot.Enemies())
            {
                float newDist = SC2Util.DistanceSq(bot.MapAnalyzer.StartLocation, enemy.Pos);
                
                if (newDist < distance)
                {
                    closestEnemy = enemy;
                    distance = newDist;
                    Chasing = true;
                }
            }
            foreach (Agent agent in Units)
            {
                if (surround && SurroundingWorkers.Contains(agent.Unit.Tag))
                    agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
                if (closestEnemy != null && agent.Unit.WeaponCooldown <= 6)
                    agent.Order(Abilities.ATTACK, SC2Util.To2D(closestEnemy.Pos));
                else if (mineral != null)
                    agent.Order(Abilities.MOVE, mineral.Tag);
                else agent.FleeEnemies(false);
            }
        }
    }
}
