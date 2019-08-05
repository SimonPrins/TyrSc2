using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class ZealotRush : Build
    {
        public bool ProxyPylon = false;
        private bool PylonPlaced = false;
        public int RequiredSize = 8;
        public bool CancelWorkerRush = false;

        public int RushWorkers = 0;


        public override string Name()
        {
            return "ZealotRush";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Tyr.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (ProxyPylon)
                PlacePylonTask.Enable();
            WorkerRushTask.Enable();
            WorkerRushTask.Task.TakeWorkers = RushWorkers;
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new FleeCyclonesController());

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
                result.Building(UnitTypes.GATEWAY, 4);
            result.Building(UnitTypes.ASSIMILATOR, () => Tyr.Bot.EnemyStrategyAnalyzer.LiftingDetected);
            result.Building(UnitTypes.CYBERNETICS_CORE, () => Tyr.Bot.EnemyStrategyAnalyzer.LiftingDetected);
            result.Train(UnitTypes.STALKER, 5, () => Tyr.Bot.EnemyStrategyAnalyzer.LiftingDetected);
            result.If(() => { return Count(UnitTypes.ZEALOT) >= 8; });
            result.Building(UnitTypes.GATEWAY, 2);
            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            TimingAttackTask.Task.RequiredSize = RequiredSize;

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

        public override void Produce(Tyr tyr, Agent agent)
        {
            /*
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 16)
                agent.Order(1006);
                */
            if (agent.Unit.UnitType == UnitTypes.GATEWAY && Minerals() >= 100
                && (Minerals() >= 150 || PylonPlaced || !ProxyPylon)
                && Count(UnitTypes.STALKER) >= 5 || !tyr.EnemyStrategyAnalyzer.LiftingDetected)
                agent.Order(916);
        }
    }
}
