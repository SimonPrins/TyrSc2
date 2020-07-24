using SC2APIProtocol;
using System;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class WorkerRush : Build
    {
        private WorkerRushTask WorkerRushTask;
        private int LastReinforcementsFrame = 0;
        private bool MessageSent = false;
        public bool CounterJensiii = false;
        public bool Recalled = false;
        public bool BuildStalkers = false;


        public override string Name()
        {
            return "WorkerRush";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            WorkerRushTask = CounterJensiii ? new WorkerRushJensiiTask() : new WorkerRushTask();
            Bot.Main.TaskManager.Add(WorkerRushTask);
            Bot.Main.TaskManager.Add(new FlyerAttackTask() { RequiredSize = 3 });
            Bot.Main.TaskManager.Add(new ElevatorChaserTask());
            TimingAttackTask.Enable();
            RecallTask.Enable();
            DefenseTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            Set += ProtossBuildUtil.Pylons();
            Set += BuildPylonForPower();
            Set += BuildStargatesAgainstLifters();
        }

        private BuildList BuildPylonForPower()
        {
            BuildList result = new BuildList();

            result.If(() => { return Lifting.Get().Detected; });
            result.If(() => { return Minerals() >= 300
                    && Count(UnitTypes.STARGATE) < 2
                    && Completed(UnitTypes.CYBERNETICS_CORE) > 0
                    && Count(UnitTypes.PYLON) == Completed(UnitTypes.PYLON); });
            result += new BuildingStep(UnitTypes.PYLON, 5);

            return result;
        }

        private BuildList BuildStargatesAgainstLifters()
        {
            BuildList result = new BuildList();

            result.If(() => Lifting.Get().Detected || Bot.Main.Frame >= 22.4 * 60 * 10 || CounterWorkerRush.Get().Detected);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Train(UnitTypes.STALKER, 10, () => BuildStalkers);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.STALKER) > 0 || !BuildStalkers);
            result.Building(UnitTypes.STARGATE, 2, () => Count(UnitTypes.STALKER) > 0 || !BuildStalkers);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            if (Count(UnitTypes.STALKER) > 0)
                BalanceGas();
            else if (Gas() < 50)
                GasWorkerTask.WorkersPerGas = 3;
            else
                GasWorkerTask.WorkersPerGas = 1;

            TimingAttackTask.Task.RequiredSize = 1;
            TimingAttackTask.Task.RetreatSize = 0;
            TimingAttackTask.Task.ExcludeUnitTypes.Add(UnitTypes.VOID_RAY);

            if (Lifting.Get().Detected)
            {
                int surfaceEnemies = 0;
                foreach (Unit unit in tyr.Enemies())
                    if (!unit.IsFlying)
                        surfaceEnemies++;

                if (surfaceEnemies < 3 && WorkerTask.Task.Units.Count < 16)
                {
                    WorkerRushTask.Clear();
                    WorkerRushTask.Stopped = true;
                }
            }

            if (!MessageSent)
                    if (tyr.Enemies().Count > 0)
                    {
                        MessageSent = true;
                        tyr.Chat("Prepare to be TICKLED! :D");
                    }

            if (tyr.Frame - LastReinforcementsFrame >= 100
                && WorkerTask.Task.Units.Count >= (Lifting.Get().Detected ? 22 : 12)
                && !Lifting.Get().Detected
                && (!CounterWorkerRush.Get().Detected || tyr.Frame >= 22.4 * 120)
                && (!CounterWorkerRush.Get().Detected || !BuildStalkers))
            {
                LastReinforcementsFrame = tyr.Frame;
                WorkerRushTask.TakeWorkers += 6;
            }
            if (UseRecall())
            {
                RecallTask.Task.Location = new PotentialHelper(tyr.TargetManager.PotentialEnemyStartLocations[0], 8).To(Main.BaseLocation.Pos).Get();
                Recalled = true;
            }
        }

        private bool UseRecall()
        {
            if (Recalled)
                return false;
            int enemyDefendingWorkers = 0;
            int enemyAttackingWorkers = 0;
            foreach (Unit enemy in Bot.Main.Enemies())
            {
                if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;
                if (SC2Util.DistanceSq(enemy.Pos, Bot.Main.MapAnalyzer.StartLocation) <= 30 * 30)
                    enemyAttackingWorkers++;
                if (SC2Util.DistanceSq(enemy.Pos, Bot.Main.TargetManager.PotentialEnemyStartLocations[0]) <= 30 * 30)
                    enemyDefendingWorkers++;
            }
            if (Lifting.Get().Detected && enemyDefendingWorkers == 0)
                return true;
            if (CounterWorkerRush.Get().Detected 
                && enemyDefendingWorkers == 0
                && Bot.Main.Frame >= 22.4 * 60)
                return true;

            if (enemyAttackingWorkers >= 5)
                return true;
            return false;

        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && (!WorkerRushTask.Stopped || Count(UnitTypes.PROBE) < 20))
                agent.Order(1006);

            if (agent.Unit.UnitType == UnitTypes.STARGATE
                && Minerals() >= 250
                && Gas() >= 150
                && FoodUsed() + 4 <= 200)
                agent.Order(950);
        }
    }
}
