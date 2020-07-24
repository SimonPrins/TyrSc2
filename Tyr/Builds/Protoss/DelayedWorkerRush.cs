using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class DelayedWorkerRush : Build
    {
        private WorkerRushTask WorkerRushTask;
        private bool MessageSent = false;
        private bool EnemyMainInvaded = false;
        private MoveWhenSafeController MoveWhenSafeController = new MoveWhenSafeController();


        public override string Name()
        {
            return "DelayedWorkerRush";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            WorkerRushTask = new WorkerRushTask() { TakeWorkers = 0};
            TimingAttackTask.Enable();
            Bot.Bot.TaskManager.Add(WorkerRushTask);
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new FearEnemyController(UnitTypes.ZEALOT, UnitTypes.BROODLING, 11) { MoveToMain = true });
            MicroControllers.Add(MoveWhenSafeController);

            if (tyr.TargetManager.PotentialEnemyStartLocations.Count == 1)
            {
                tyr.EnemyManager.EnemyBuildings.Add(ulong.MaxValue, new Managers.BuildingLocation()
                {
                    LastSeen = 0,
                    Pos = new Point() { X = tyr.TargetManager.PotentialEnemyStartLocations[0].X, Y = tyr.TargetManager.PotentialEnemyStartLocations[0].Y, Z = 0 },
                    Tag = ulong.MaxValue,
                    Type = tyr.EnemyRace == SC2APIProtocol.Race.Zerg ? UnitTypes.HATCHERY : (tyr.EnemyRace == SC2APIProtocol.Race.Protoss ? UnitTypes.NEXUS : UnitTypes.COMMAND_CENTER)
                });
            }

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

            result.Train(UnitTypes.PROBE, 16);
            result.Building(UnitTypes.PYLON);
            result.Building(UnitTypes.GATEWAY);
            result.Train(UnitTypes.ZEALOT);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            tyr.TargetManager.PrefferDistant = false;
            if (!MessageSent)
                    if (tyr.Enemies().Count > 0)
                    {
                        MessageSent = true;
                        tyr.Chat("Prepare to be TICKLED! :D");
                    }

            TimingAttackTask.Task.RequiredSize = 1;

            if (Completed(UnitTypes.ZEALOT) > 0)
                WorkerRushTask.TakeWorkers = 20;
            
            /*
            if (!EnemyMainInvaded)
                foreach (Agent unit in tyr.Units())
                    if (tyr.TargetManager.PotentialEnemyStartLocations.Count > 0
                        && unit.DistanceSq(tyr.TargetManager.PotentialEnemyStartLocations[0]) <= 10 * 10)
                        EnemyMainInvaded = true;

            tyr.DrawText("Enemy main invaded: " + EnemyMainInvaded);

            WorkerRushTask.MoveCommandWhenSafe = !EnemyMainInvaded;
            MoveWhenSafeController.Stopped = EnemyMainInvaded;
            */
        }
    }
}
