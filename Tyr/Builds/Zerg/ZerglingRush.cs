﻿using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Zerg
{
    public class ZerglingRush : Build
    {
        private WorkerRushTask WorkerRushTask = new WorkerRushTask() { TakeWorkers = 0 };
        private bool WorkersSent = false;
        private WorkerScoutTask WorkerScoutTask = new WorkerScoutTask();
        public override string Name()
        {
            return "ZerglingRush";
        }

        public override void OnStart(Bot bot)
        {
            bot.TaskManager.Add(new TimingAttackTask() { RequiredSize = 6 });
            bot.TaskManager.Add(WorkerScoutTask);
            bot.TaskManager.Add(WorkerRushTask);
            WorkerRushDefenseTask.Enable();
            MicroControllers.Add(new FleeBroodlingsController());
            MicroControllers.Add(new TargetFireController(GetPrioritiesCloseRange()) { MoveWhenNoTarget = false });
            MicroControllers.Add(new TargetFireController(GetPriorities()));
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                QueenInjectTask queenInjectTask = new QueenInjectTask(b);
                bot.TaskManager.Add(queenInjectTask);
            }
            Set += MainBuild();
        }

        public PriorityTargetting GetPrioritiesCloseRange()
        {
            PriorityTargetting priorities = new PriorityTargetting();

            priorities.DefaultPriorities.MaxRange = 1.5f;
            priorities.DefaultPriorities[UnitTypes.LARVA] = -1;
            priorities.DefaultPriorities[UnitTypes.EGG] = -1;
            priorities.DefaultPriorities[UnitTypes.OVERLORD] = -1;

            foreach (uint t in UnitTypes.CombatUnitTypes)
                priorities.DefaultPriorities[t] = 1;
            foreach (uint t in UnitTypes.WorkerTypes)
                priorities.DefaultPriorities[t] = 1;

            return priorities;
        }

        public PriorityTargetting GetPriorities()
        {
            PriorityTargetting priorities = new PriorityTargetting();

            priorities.DefaultPriorities.MaxRange = 10;
            priorities.DefaultPriorities[UnitTypes.LARVA] = -1;
            priorities.DefaultPriorities[UnitTypes.EGG] = -1;
            priorities.DefaultPriorities[UnitTypes.OVERLORD] = -1;

            foreach (uint t in UnitTypes.CombatUnitTypes)
                priorities.DefaultPriorities[t] = 1;
            foreach (uint t in UnitTypes.WorkerTypes)
                priorities.DefaultPriorities[t] = 1;

            return priorities;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            result.Building(UnitTypes.SPAWNING_POOL);
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (bot.TargetManager.PotentialEnemyStartLocations.Count <= 1)
            {
                WorkerScoutTask.Clear();
                WorkerScoutTask.Stopped = true;
            }

            if (Completed(UnitTypes.ZERGLING) >= 6
                && !WorkersSent)
            {
                WorkerRushTask.TakeWorkers = 10;
                WorkersSent = true;
            }
            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (agent.Unit.UnitType == UnitTypes.LARVA)
                {
                    if (Minerals() >= 50 && ExpectedAvailableFood() > FoodUsed() + 2
                        && Completed(UnitTypes.SPAWNING_POOL) > 0)
                        agent.Order(1343);
                    else if (Minerals() >= 100
                        && Count(UnitTypes.SPAWNING_POOL) > 0
                        && Count(UnitTypes.OVERLORD) < 2)
                    {
                        agent.Order(1344);
                        break;
                    }
                }
            }
        }

        public override void Produce(Bot bot, Agent agent)
        {
            /*
            if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Minerals() >= 150
                    && Completed(UnitTypes.QUEEN) == 0
                    && Completed(UnitTypes.SPAWNING_POOL) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(bot.UnitManager.Counts, UnitTypes.QUEEN);
                }
            }
            */
        }
    }
}
