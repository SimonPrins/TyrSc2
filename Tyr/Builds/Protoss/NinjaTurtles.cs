using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class NinjaTurtles : Build
    {
        private DTAttackTask DTAttackTask = new DTAttackTask();
        private bool gatewayBuilt;
        private DefenseTask DefenseTask = new DefenseTask();

        private BuildingStep ReaperDefenseCannonStep;
        private bool ChasingLiftedBuildings = false;

        public bool VoidrayOnly = false;

        public override string Name()
        {
            return "NinjaTurtles";
        }

        public override void OnStart(Tyr tyr)
        {
            tyr.TaskManager.Add(DefenseTask);
            tyr.TaskManager.Add(DTAttackTask);
            tyr.TaskManager.Add(new FlyerAttackTask() { RequiredSize = 4 });
            //tyr.TaskManager.Add(new RemoveLostWorkersTask());
            tyr.TaskManager.Add(new ShieldBatteryTargetTask());
            if (tyr.BaseManager.Pocket != null)
                tyr.TaskManager.Add(new ScoutProxyTask(tyr.BaseManager.Pocket.BaseLocation.Pos));
            MicroControllers.Add(new VoidrayController());
            MicroControllers.Add(new StutterController());
            ConstructionTask.Task.OnlyWorkersFromMain = true;
            tyr.buildingPlacer.BuildInsideMainOnly = true;
            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += BuildWallPylon();
            Set += ProtossBuildUtil.Pylons();
            Set += BuildReaperDefenseCannon();
            Set += MainBuild();
            Set += StargatesAgainstCannons();
        }

        private BuildList BuildWallPylon()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON, Tyr.Bot.BaseManager.Main, Tyr.Bot.MapAnalyzer.building3, true);

            return result;
        }
        
        private BuildList BuildReaperDefenseCannon()
        {
            BuildList result = new BuildList();

            Base reaperBase = null;
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
                if (b != Tyr.Bot.BaseManager.Main)
                    reaperBase = b;

            ReaperDefenseCannonStep = new BuildingStep(UnitTypes.PHOTON_CANNON, reaperBase);

            result.If(() => { return ReaperDefenseCannonStep.DesiredPos != null && Count(UnitTypes.FORGE) > 0; });
            result += ReaperDefenseCannonStep;

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            Point2D cannon1Pos = SC2Util.Point(Tyr.Bot.MapAnalyzer.building1.X + (Tyr.Bot.MapAnalyzer.building1.X - Tyr.Bot.MapAnalyzer.building2.X) / 2, Tyr.Bot.MapAnalyzer.building1.Y + (Tyr.Bot.MapAnalyzer.building1.Y - Tyr.Bot.MapAnalyzer.building2.Y) / 2);
            Point2D cannon2Pos = SC2Util.Point(Tyr.Bot.MapAnalyzer.building2.X + (Tyr.Bot.MapAnalyzer.building2.X - Tyr.Bot.MapAnalyzer.building1.X) / 2, Tyr.Bot.MapAnalyzer.building2.Y + (Tyr.Bot.MapAnalyzer.building2.Y - Tyr.Bot.MapAnalyzer.building1.Y) / 2);

            result.Building(UnitTypes.FORGE, Tyr.Bot.BaseManager.Main, Tyr.Bot.MapAnalyzer.building1, true);
            result.Building(UnitTypes.GATEWAY, Tyr.Bot.BaseManager.Main, Tyr.Bot.MapAnalyzer.building2, true);
            result.Building(UnitTypes.PHOTON_CANNON, Tyr.Bot.BaseManager.Main, cannon1Pos);
            result.Building(UnitTypes.PHOTON_CANNON, Tyr.Bot.BaseManager.Main, cannon2Pos);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.ASSIMILATOR);
            result.If(() => { return Completed(UnitTypes.CYBERNETICS_CORE) > 0; });
            result.Building(UnitTypes.SHIELD_BATTERY, Tyr.Bot.BaseManager.Main, Tyr.Bot.MapAnalyzer.building1);
            result.Building(UnitTypes.SHIELD_BATTERY, Tyr.Bot.BaseManager.Main, Tyr.Bot.MapAnalyzer.building2);
            result.Building(UnitTypes.PHOTON_CANNON, Tyr.Bot.BaseManager.Main, Tyr.Bot.MapAnalyzer.building3);
            result.Building(UnitTypes.PYLON, 3);
            result.If(() => { return !VoidrayOnly; });
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.DARK_SHRINE);

            return result;
        }

        private BuildList StargatesAgainstCannons()
        {
            BuildList result = new BuildList();
            
            result.If(() => { return VoidrayOnly; });
            result.Building(UnitTypes.STARGATE, 2);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            tyr.buildingPlacer.BuildCompact = true;

            if (tyr.EnemyStrategyAnalyzer.CannonRushDetected)
                VoidrayOnly = true;

            if (VoidrayOnly)
                DefenseTask.Stopped = true;
            
            if ((tyr.EnemyRace == Race.Terran || tyr.EnemyRace == Race.Random)
                && ReaperDefenseCannonStep.DesiredPos == null)
            {
                foreach (Unit unit in tyr.Enemies())
                {
                    if (unit.UnitType == UnitTypes.REAPER
                        && Tyr.Bot.MapAnalyzer.StartArea[(int)System.Math.Round(unit.Pos.X), (int)System.Math.Round(unit.Pos.Y)])
                    {
                        Point2D dir = SC2Util.Point(unit.Pos.X - tyr.MapAnalyzer.StartLocation.X, unit.Pos.Y - tyr.MapAnalyzer.StartLocation.Y);
                        float length = (float)System.Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
                        dir = SC2Util.Point(dir.X / length, dir.Y / length);

                        ReaperDefenseCannonStep.DesiredPos = SC2Util.Point(tyr.MapAnalyzer.StartLocation.X + dir.X * 4f, tyr.MapAnalyzer.StartLocation.Y + dir.Y * 4f);
                        break;
                    }
                }
            }
            
            if (tyr.EnemyStrategyAnalyzer.LiftingDetected && tyr.EnemyManager.EnemyBuildings.Count == 0 && !ChasingLiftedBuildings)
            {
                ChasingLiftedBuildings = true;
                tyr.TaskManager.Add(new ElevatorChaserTask());
            }

            if (gatewayBuilt && Count(UnitTypes.GATEWAY) == 0)
                gatewayBuilt = false;
            else if (!gatewayBuilt)
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.GATEWAY)
                    {
                        gatewayBuilt = true;
                        agent.Order(Abilities.MOVE, tyr.TargetManager.PotentialEnemyStartLocations[0]);
                        break;
                    }
            }
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 22 - Completed(UnitTypes.ASSIMILATOR))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.GATEWAY)
            {
                if (Completed(UnitTypes.DARK_SHRINE) > 0 && !VoidrayOnly)
                {
                    if (Minerals() >= 125
                        && Gas() >= 125)
                        agent.Order(920);
                }
            }
            else if (agent.Unit.UnitType == UnitTypes.STARGATE
                && Minerals() >= 250
                && Gas() >= 150
                && (Minerals() >= 300 || VoidrayOnly)
                && (Gas() >= 250 || VoidrayOnly)
                && FoodUsed() + 4 <= 200)
                agent.Order(950);
        }
    }
}
