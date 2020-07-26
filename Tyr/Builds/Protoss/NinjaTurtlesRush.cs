using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.Micro;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class NinjaTurtlesRush : Build
    {
        private bool gatewayBuilt;
        private DefenseTask DefenseTask = new DefenseTask();

        private BuildingStep ReaperDefenseCannonStep;
        private bool ChasingLiftedBuildings = false;

        public bool VoidrayOnly = false;

        public override string Name()
        {
            return "NinjaTurtlesRush";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            DTAttackTask.Enable();
            FlyerAttackTask.Enable();
            ShieldBatteryTargetTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new VoidrayController());
            MicroControllers.Add(new StutterController());


            Set += BuildWallPylon();
            Set += ProtossBuildUtil.Pylons();
            Set += BuildReaperDefenseCannon();
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

            ReaperDefenseCannonStep = new BuildingStep(UnitTypes.PHOTON_CANNON, reaperBase);

            result.If(() => { return ReaperDefenseCannonStep.DesiredPos != null && Count(UnitTypes.FORGE) > 0; });
            result += ReaperDefenseCannonStep;

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            Point2D cannon1Pos = SC2Util.Point(Bot.Main.MapAnalyzer.building1.X + (Bot.Main.MapAnalyzer.building1.X - Bot.Main.MapAnalyzer.building2.X) / 2, Bot.Main.MapAnalyzer.building1.Y + (Bot.Main.MapAnalyzer.building1.Y - Bot.Main.MapAnalyzer.building2.Y) / 2);
            Point2D cannon2Pos = SC2Util.Point(Bot.Main.MapAnalyzer.building2.X + (Bot.Main.MapAnalyzer.building2.X - Bot.Main.MapAnalyzer.building1.X) / 2, Bot.Main.MapAnalyzer.building2.Y + (Bot.Main.MapAnalyzer.building2.Y - Bot.Main.MapAnalyzer.building1.Y) / 2);

            result.Building(UnitTypes.FORGE, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building1, true);
            result.Building(UnitTypes.GATEWAY, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building2, true);
            result.Building(UnitTypes.PHOTON_CANNON, Bot.Main.BaseManager.Main, cannon1Pos);
            result.Building(UnitTypes.PHOTON_CANNON, Bot.Main.BaseManager.Main, cannon2Pos);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.ASSIMILATOR);
            result.If(() => { return Completed(UnitTypes.CYBERNETICS_CORE) > 0; });
            result.Building(UnitTypes.SHIELD_BATTERY, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building1);
            result.Building(UnitTypes.SHIELD_BATTERY, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building2);
            result.Building(UnitTypes.PHOTON_CANNON, Bot.Main.BaseManager.Main, Bot.Main.MapAnalyzer.building3);
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

        public override void OnFrame(Bot bot)
        {
            FlyerAttackTask.Task.RequiredSize = 4;
            ConstructionTask.Task.OnlyWorkersFromMain = true;
            
            bot.buildingPlacer.BuildInsideMainOnly = true;
            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            bot.buildingPlacer.BuildCompact = true;

            if (StrategyAnalysis.CannonRush.Get().Detected)
                VoidrayOnly = true;

            if (VoidrayOnly)
                DefenseTask.Stopped = true;
            
            if ((bot.EnemyRace == Race.Terran || bot.EnemyRace == Race.Random)
                && ReaperDefenseCannonStep.DesiredPos == null)
            {
                foreach (Unit unit in bot.Enemies())
                {
                    if (unit.UnitType == UnitTypes.REAPER
                        && Bot.Main.MapAnalyzer.StartArea[(int)System.Math.Round(unit.Pos.X), (int)System.Math.Round(unit.Pos.Y)])
                    {
                        Point2D dir = SC2Util.Point(unit.Pos.X - bot.MapAnalyzer.StartLocation.X, unit.Pos.Y - bot.MapAnalyzer.StartLocation.Y);
                        float length = (float)System.Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
                        dir = SC2Util.Point(dir.X / length, dir.Y / length);

                        ReaperDefenseCannonStep.DesiredPos = SC2Util.Point(bot.MapAnalyzer.StartLocation.X + dir.X * 4f, bot.MapAnalyzer.StartLocation.Y + dir.Y * 4f);
                        break;
                    }
                }
            }
            
            if (Lifting.Get().Detected && bot.EnemyManager.EnemyBuildings.Count == 0 && !ChasingLiftedBuildings)
            {
                ChasingLiftedBuildings = true;
                bot.TaskManager.Add(new ElevatorChaserTask());
            }

            if (gatewayBuilt && Count(UnitTypes.GATEWAY) == 0)
                gatewayBuilt = false;
            else if (!gatewayBuilt)
            {
                foreach (Agent agent in bot.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.GATEWAY)
                    {
                        gatewayBuilt = true;
                        agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
                        break;
                    }
            }
        }

        public override void Produce(Bot bot, Agent agent)
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
