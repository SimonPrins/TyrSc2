using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Zerg
{
    public class OneBaseZergling : Build
    {
        public bool EnableDefense = false;
        
        public override string Name()
        {
            return "OneBaseZergling";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            QueenInjectTask.Enable();
            QueenDefenseTask.Enable();
            ArmyOverseerTask.Enable();
            QueenTumorTask.Enable();
            DefenseTask.Enable();
            WorkerRushDefenseTask.Enable();
            SafeZerglingsFromReapersTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new ZerglingController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new InfestorController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new QueenTransfuseController());

            Set += ZergBuildUtil.Overlords();
            Set += WorkerRushDefense();
            Set += Zerglings();
            Set += MainBuild();
        }

        private BuildList WorkerRushDefense()
        {
            BuildList result = new BuildList();
            result.If(() => StrategyAnalysis.WorkerRush.Get().Detected);
            result.Morph(UnitTypes.OVERLORD, 2, () => ExpectedAvailableFood() <= FoodUsed() - 2 || Minerals() >= 500);
            result.Morph(UnitTypes.DRONE, 8);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Morph(UnitTypes.ZERGLING, 4);
            result.Morph(UnitTypes.ZERGLING, 10);
            result.Morph(UnitTypes.OVERLORD);
            return result;
        }

        private BuildList Zerglings()
        {
            BuildList result = new BuildList();
            result.Morph(UnitTypes.ZERGLING, 400);
            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            result.Building(UnitTypes.HATCHERY);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Morph(UnitTypes.DRONE, 14);
            result.Morph(UnitTypes.OVERLORD, 2);
            result.If(() => Completed(UnitTypes.SPAWNING_POOL) > 0 && Count(UnitTypes.ZERGLING) >= 10);
            result.Morph(UnitTypes.DRONE, 2);
            result.Train(UnitTypes.QUEEN, 2);
            result.Building(UnitTypes.EXTRACTOR);
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            if (tyr.TargetManager.PotentialEnemyStartLocations.Count <= 1)
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            if (UpgradeType.LookUp[UpgradeType.MetabolicBoost].Done())
            {
                SafeZerglingsFromReapersTask.Task.Stopped = true;
                SafeZerglingsFromReapersTask.Task.Clear();
            }

            if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(66)
                && !Bot.Main.UnitManager.ActiveOrders.Contains(1253))
            {
                if (Gas() < 92)
                    GasWorkerTask.WorkersPerGas = 3;
                else if (Gas() < 96)
                    GasWorkerTask.WorkersPerGas = 2;
                else if (Gas() < 100)
                    GasWorkerTask.WorkersPerGas = 1;
                else if (Gas() >= 100)
                    GasWorkerTask.WorkersPerGas = 0;
            }
            else
                GasWorkerTask.WorkersPerGas = 0;

            TimingAttackTask.Task.RequiredSize = 6;
            TimingAttackTask.Task.RetreatSize = 0;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 18;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 55;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 20;
            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 18;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 55;
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.SPAWNING_POOL)
            {
                if (Count(UnitTypes.QUEEN) < 2)
                    return;
                if (Minerals() >= 100
                    && Gas() >= 100
                    && !Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(66))
                    agent.Order(1253);
                else if (Minerals() >= 200
                    && Gas() >= 200
                    && !Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(65))
                    agent.Order(1252);
            }
        }
    }
}
