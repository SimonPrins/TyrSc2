using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class NinjaTurtles : Build
    {
        private bool gatewayBuilt;

        private BuildingStep ReaperDefenseCannonStep;
        private bool ChasingLiftedBuildings = false;

        public bool VoidrayOnly = false;

        public bool Expand = false;
        private bool StartExpanding = false;

        public override string Name()
        {
            return "NinjaTurtles";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            DTAttackTask.Enable();
            Bot.Main.TaskManager.Add(new FlyerAttackTask() { RequiredSize = 4 });
            Bot.Main.TaskManager.Add(new ShieldBatteryTargetTask());
            KillOwnUnitTask.Enable();


            if (Bot.Main.BaseManager.Pocket != null)
                Bot.Main.TaskManager.Add(new ScoutProxyTask(Bot.Main.BaseManager.Pocket.BaseLocation.Pos));
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new VoidrayController());
            MicroControllers.Add(new StutterController());
            ConstructionTask.Task.OnlyWorkersFromMain = true;
            tyr.buildingPlacer.BuildInsideMainOnly = true;
            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += BuildWallPylon();
            Set += ProtossBuildUtil.Pylons();
            Set += BuildReaperDefenseCannon();
            Set += Units();
            Set += MainBuild();
            Set += StargatesAgainstCannons();
        }

        private BuildList BuildWallPylon()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building3, true);

            return result;
        }

        private BuildList BuildReaperDefenseCannon()
        {
            BuildList result = new BuildList();

            Base reaperBase = null;
            foreach (Base b in Bot.Main.BaseManager.Bases)
                if (b != Bot.Main.BaseManager.Main)
                    reaperBase = b;

            ReaperDefenseCannonStep = new BuildingStep(UnitTypes.PHOTON_CANNON, reaperBase, () => !StartExpanding);

            result.If(() => { return ReaperDefenseCannonStep.DesiredPos != null && Count(UnitTypes.FORGE) > 0; });
            result += ReaperDefenseCannonStep;

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.TEMPEST, () => Gas() >= 175);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            Point2D cannon1Pos = SC2Util.Point(Bot.Main.MapAnalyzer.building1.X + (Bot.Main.MapAnalyzer.building1.X - Bot.Main.MapAnalyzer.building2.X) / 2, Bot.Main.MapAnalyzer.building1.Y + (Bot.Main.MapAnalyzer.building1.Y - Bot.Main.MapAnalyzer.building2.Y) / 2);
            Point2D cannon2Pos = SC2Util.Point(Bot.Main.MapAnalyzer.building2.X + (Bot.Main.MapAnalyzer.building2.X - Bot.Main.MapAnalyzer.building1.X) / 2, Bot.Main.MapAnalyzer.building2.Y + (Bot.Main.MapAnalyzer.building2.Y - Bot.Main.MapAnalyzer.building1.Y) / 2);

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.FORGE, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building1, true, () => !StartExpanding);
            result.Building(UnitTypes.GATEWAY, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building2, true);
            result.Building(UnitTypes.PHOTON_CANNON, Bot.Main.BaseManager.Main, cannon1Pos, () => !StartExpanding);
            result.Building(UnitTypes.PHOTON_CANNON, Bot.Main.BaseManager.Main, cannon2Pos, () => !StartExpanding);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.ASSIMILATOR);
            result.If(() => { return Completed(UnitTypes.CYBERNETICS_CORE) > 0; });
            result.Building(UnitTypes.SHIELD_BATTERY, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building1);
            result.Building(UnitTypes.SHIELD_BATTERY, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building2);
            result.Building(UnitTypes.PHOTON_CANNON, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building3, () => !StartExpanding);
            result.Building(UnitTypes.PYLON, 3);
            result.If(() => { return !VoidrayOnly; });
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.DARK_SHRINE);
            result.If(() => StartExpanding);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.FLEET_BEACON);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.If(() => Count(UnitTypes.TEMPEST) > 0);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);

            return result;
        }

        private BuildList StargatesAgainstCannons()
        {
            BuildList result = new BuildList();
            
            result.If(() => { return VoidrayOnly; });
            result.Building(UnitTypes.STARGATE, 2);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            if (Expand && Main.BaseLocation.MineralFields.Count < 8)
                StartExpanding = true;

            if (StartExpanding)
            {
                foreach (Agent agent in tyr.Units())
                    if (agent.Unit.UnitType == UnitTypes.FORGE
                        && agent.DistanceSq(Bot.Main.MapAnalyzer.building1) <= 2 * 2)
                    {
                        KillOwnUnitTask.Task.TargetTag = agent.Unit.Tag;
                        break;
                    }
            }


            tyr.buildingPlacer.BuildCompact = true;

            if (StrategyAnalysis.CannonRush.Get().Detected)
                VoidrayOnly = true;

            if (VoidrayOnly)
            {
                DefenseTask.AirDefenseTask.Stopped = true;
                DefenseTask.GroundDefenseTask.Stopped = true;
            }
            
            if ((tyr.EnemyRace == Race.Terran || tyr.EnemyRace == Race.Random)
                && ReaperDefenseCannonStep.DesiredPos == null)
            {
                foreach (Unit unit in tyr.Enemies())
                {
                    if (unit.UnitType == UnitTypes.REAPER
                        && Bot.Main.MapAnalyzer.StartArea[(int)System.Math.Round(unit.Pos.X), (int)System.Math.Round(unit.Pos.Y)])
                    {
                        Point2D dir = SC2Util.Point(unit.Pos.X - tyr.MapAnalyzer.StartLocation.X, unit.Pos.Y - tyr.MapAnalyzer.StartLocation.Y);
                        float length = (float)System.Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
                        dir = SC2Util.Point(dir.X / length, dir.Y / length);

                        ReaperDefenseCannonStep.DesiredPos = SC2Util.Point(tyr.MapAnalyzer.StartLocation.X + dir.X * 4f, tyr.MapAnalyzer.StartLocation.Y + dir.Y * 4f);
                        break;
                    }
                }
            }
            
            if (Lifting.Get().Detected && tyr.EnemyManager.EnemyBuildings.Count == 0 && !ChasingLiftedBuildings)
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

        public override void Produce(Bot tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && (Count(UnitTypes.PROBE) < 22 - Completed(UnitTypes.ASSIMILATOR) || Count(UnitTypes.NEXUS) >= 2)
                && (Count(UnitTypes.PROBE) < 44 - Completed(UnitTypes.ASSIMILATOR) || Count(UnitTypes.NEXUS) >= 3)
                && Count(UnitTypes.PROBE) < 66 - Completed(UnitTypes.ASSIMILATOR))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
                return;
            }

            if (StartExpanding
                && (Count(UnitTypes.NEXUS) < 2 || Count(UnitTypes.FLEET_BEACON) == 0))
                return;

            if (agent.Unit.UnitType == UnitTypes.GATEWAY)
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
                && FoodUsed() + 4 <= 200
                && !StartExpanding)
                agent.Order(950);
        }
    }
}
