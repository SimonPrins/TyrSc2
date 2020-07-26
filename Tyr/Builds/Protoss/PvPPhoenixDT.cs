using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Protoss
{
    public class PvPPhoenixDT : Build
    {
        private StutterController StutterController = new StutterController();


        public override string Name()
        {
            return "PvPPhoenixDT";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();

            DefenseTask.Enable();
            TimingAttackTask.Enable();
            //if (Tyr.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
            WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ArmyObserverTask.Enable();
            ArmyOracleTask.Enable();
            TimedObserverTask.Enable();
            SaveWorkersTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new DTController());
            MicroControllers.Add(new VoidrayController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new StalkerController());
            MicroControllers.Add(new GravitonBeamController());
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, UnitTypes.STALKER, 6));
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(StutterController);

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += ProtossBuildUtil.Pylons(() =>
            (Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0 && Count(UnitTypes.STALKER) > 0) && Count(UnitTypes.PYLON) < 3 || bot.Frame >= 22.4 * 60 * 3.5);
            Set += ExpandBuildings();
            Set += ExtraAssimilators();
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();
            
            /*
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 450);
            }
            */

            return result;
        }

        private BuildList ExtraAssimilators()
        {
            BuildList result = new BuildList();

            result.If(() => Minerals() >= 600 && Completed(UnitTypes.NEXUS) >= 3 && Gas() < 100 && Bot.Main.Frame % 10 == 0);
            result.Building(UnitTypes.ASSIMILATOR, 6);
            result.If(() => Minerals() >= 800);
            result.Building(UnitTypes.ASSIMILATOR, 7);
            result.If(() => Minerals() >= 900);
            result.Building(UnitTypes.ASSIMILATOR, 8);
            result.If(() => Minerals() >= 1000);
            result.Building(UnitTypes.ASSIMILATOR, 10);

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.PROBE, 60, () => Count(UnitTypes.NEXUS) >= 3 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.PROBE, 70, () => Count(UnitTypes.NEXUS) >= 4 && Count(UnitTypes.PYLON) > 2);
            result.Train(UnitTypes.STALKER, 1);
            result.Train(UnitTypes.VOID_RAY, 2);
            result.Train(UnitTypes.ORACLE, 1);
            result.Train(UnitTypes.VOID_RAY, 6);
            result.Train(UnitTypes.ORACLE, 3);
            result.Train(UnitTypes.VOID_RAY, 10);
            result.Train(UnitTypes.ORACLE, 4);
            result.Train(UnitTypes.DARK_TEMPLAR, 20);
            result.Train(UnitTypes.TEMPEST, 20);
            result.Train(UnitTypes.PHOENIX, 20);
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.PHOENIX) < 30 || Count(UnitTypes.NEXUS) >= 3);
            result.If(() => Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.STALKER) < 15);
            result.If(() => Count(UnitTypes.NEXUS) >= 3 || Count(UnitTypes.STALKER) < 20);
            result.Train(UnitTypes.STALKER, 5);
            result.Train(UnitTypes.STALKER, 15, () => Completed(UnitTypes.DARK_SHRINE) == 0);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.PYLON, Natural, NaturalDefensePos);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.DARK_SHRINE);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, () => Count(UnitTypes.DISRUPTOR) > 0 || Count(UnitTypes.STALKER) >= 12);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.FLEET_BEACON);
            result.Upgrade(UpgradeType.Blink);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ASSIMILATOR, () => Minerals() >= 600);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.ASSIMILATOR);
            result.If(() => Completed(UnitTypes.STALKER) + Completed(UnitTypes.PHOENIX) >= 30);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 3);
            result.Building(UnitTypes.GATEWAY, 2);
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            BalanceGas();

            if (WorkerScoutTask.Task.BaseCircled())
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }
            

            bot.NexusAbilityManager.Stopped = Completed(UnitTypes.PYLON) == 0;
            bot.NexusAbilityManager.PriotitizedAbilities.Add(1006);


            SaveWorkersTask.Task.Stopped = bot.Frame >= 22.4 * 60 * 7 || EnemyCount(UnitTypes.CYCLONE) == 0 || !Natural.UnderAttack;
            if (SaveWorkersTask.Task.Stopped)
                SaveWorkersTask.Task.Clear();

            DefenseTask.GroundDefenseTask.IncludePhoenixes = true;

            WorkerTask.Task.EvacuateThreatenedBases = true;
            

            TimingAttackTask.Task.DefendOtherAgents = false;

                TimingAttackTask.Task.RetreatSize = 15;
                TimingAttackTask.Task.RequiredSize = 35;

            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 120;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;

        }
    }
}
