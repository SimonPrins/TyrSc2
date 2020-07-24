using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class ZealotRush : Build
    {
        public bool ProxyPylon = false;
        private bool PylonPlaced = false;
        public int RequiredSize = 6;
        public bool CancelWorkerRush = false;
        public bool MovePastSpineCrawlers = false;

        public int RushWorkers = 0;
        private FearCannonsController FearCannonsController = new FearCannonsController();


        public override string Name()
        {
            return "ZealotRush";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (ProxyPylon)
                PlacePylonTask.Enable();
            WorkerRushTask.Enable();
            WorkerRushTask.Task.TakeWorkers = RushWorkers;
            WorkerRushDefenseTask.Enable();
            if (MovePastSpineCrawlers)
                RunbyTask.Enable();
            MineGoldenWallMineralsTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(FearCannonsController);

            Set += ProtossBuildUtil.Pylons();
            Set += Units();
            Set += BuildGateways();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();
            result.Train(UnitTypes.PROBE, 16);
            return result;
        }

        private BuildList BuildGateways()
        {
            BuildList result = new BuildList();
            if (RushWorkers > 3)
            {
                result.Building(UnitTypes.GATEWAY, 2);
                result.Building(UnitTypes.GATEWAY, 2, () => Count(UnitTypes.ZEALOT) >= 2);
            }
            else
            {
                result.Building(UnitTypes.GATEWAY, 3);
                result.Building(UnitTypes.GATEWAY, 1, () => Count(UnitTypes.ZEALOT) >= 4);
            }
            result.Building(UnitTypes.ASSIMILATOR, () => Lifting.Get().Detected);
            result.Building(UnitTypes.CYBERNETICS_CORE, () => Lifting.Get().Detected);
            result.Train(UnitTypes.STALKER, 5, () => Lifting.Get().Detected);
            //result.If(() => { return Count(UnitTypes.ZEALOT) >= 8; });
            //result.Building(UnitTypes.GATEWAY, 2);
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            TimingAttackTask.Task.RequiredSize = RequiredSize;

            if (Completed(UnitTypes.ZEALOT) >= 25)
            {
                FearCannonsController.Stopped = true;
                RunbyTask.Task.StopAndClear(true);
            }
            if (Completed(UnitTypes.ZEALOT) < 15)
                FearCannonsController.Stopped = false;

            tyr.buildingPlacer.BuildCompact = true;

            if (TotalEnemyCount(UnitTypes.PROBE) + TotalEnemyCount(UnitTypes.SCV) + TotalEnemyCount(UnitTypes.DRONE) >= 4
                && CancelWorkerRush)
            {
                WorkerRushTask.Task.Stopped = true;
                WorkerRushTask.Task.Clear();
            }

            if (!PylonPlaced)
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.PYLON && SC2Util.DistanceSq(agent.Unit.Pos, tyr.MapAnalyzer.StartLocation) >= 40 * 40)
                    {
                        PylonPlaced = true;
                        PlacePylonTask.Task.Clear();
                        PlacePylonTask.Task.Stopped = true;
                    }
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            /*
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 16)
                agent.Order(1006);
                */
            if (agent.Unit.UnitType == UnitTypes.GATEWAY && Minerals() >= 100
                && (Minerals() >= 150 || PylonPlaced || !ProxyPylon)
                && Count(UnitTypes.STALKER) >= 5 || !Lifting.Get().Detected)
                agent.Order(916);
        }
    }
}
