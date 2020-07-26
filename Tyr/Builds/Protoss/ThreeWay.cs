using SC2APIProtocol;
using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Managers;
using SC2Sharp.MapAnalysis;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;
using SC2Sharp.Util;
using System;

namespace SC2Sharp.Builds.Protoss
{
    public class ThreeWay : Build
    {
        private StutterController StutterController = new StutterController();
        private StutterForwardController StutterForwardController = new StutterForwardController();

        Random Random = new Random();

        private bool IntroductionSent = false;
        private bool RevealSent = false;

        public override string Name()
        {
            return "ThreeWay";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();

            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Bot.Main.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Main.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Main.BaseManager.Pocket.BaseLocation.Pos);
            ArmyObserverTask.Enable();
            SaveWorkersTask.Enable();
            ForceFieldRampTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            BuildingPlacement.ReservedBuilding reservedNatural = new BuildingPlacement.ReservedBuilding();
            reservedNatural.Pos = Natural.BaseLocation.Pos;
            reservedNatural.Type = UnitTypes.NEXUS;
            bot.buildingPlacer.ReservedLocation.Add(reservedNatural);
            StutterController.Range = 15;

            MicroControllers.Add(new SoftLeashController(UnitTypes.COLOSUS, UnitTypes.IMMORTAL, 12));
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new TempestController());
            MicroControllers.Add(new DisruptorController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new FallBackController() { ReturnFire = true, MainDist = 40 });
            MicroControllers.Add(new GravitonBeamController());
            MicroControllers.Add(new FearEnemyController(UnitTypes.PHOENIX, UnitTypes.MISSILE_TURRET, 11));
            MicroControllers.Add(new AttackEnemyController(UnitTypes.PHOENIX, UnitTypes.BANSHEE, 15, true));
            MicroControllers.Add(StutterController);
            MicroControllers.Add(StutterForwardController);

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += ProtossBuildUtil.Pylons(() => (Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0 && Count(UnitTypes.STALKER) > 0) || bot.Frame >= 22.4 * 60 * 3.5);
            Set += Units();
            Set += ExpandBuildings();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.STALKER, 1);
            result.Train(UnitTypes.SENTRY, 1);
            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2 && Count(UnitTypes.PYLON) >= 2);
            result.Train(UnitTypes.PROBE, 60, () => Count(UnitTypes.NEXUS) >= 3 && Count(UnitTypes.PYLON) >= 2);
            result.Train(UnitTypes.PROBE, 70, () => Count(UnitTypes.NEXUS) >= 4 && Count(UnitTypes.PYLON) >= 2);
            result.Upgrade(UpgradeType.WarpGate, () => Count(UnitTypes.STALKER) > 0);
            result.If(() => Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) + Count(UnitTypes.PHOENIX) < 30 || Count(UnitTypes.NEXUS) >= 3);
            result.If(() => Count(UnitTypes.NEXUS) >= 2 || Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) < 15);
            result.If(() => Count(UnitTypes.NEXUS) >= 3 || Count(UnitTypes.STALKER) + Count(UnitTypes.IMMORTAL) < 20);
            result.Train(UnitTypes.IMMORTAL, 2);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.TEMPEST);
            result.Train(UnitTypes.IMMORTAL);
            result.Train(UnitTypes.STALKER, 5);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();
            result.If(() => Minerals() >= 400);
            
            foreach (Base b in Bot.Main.BaseManager.Bases)
            {
                if (b == Main)
                    continue;
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
            }

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON);
            //result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.NEXUS);
            result.If(() => Count(UnitTypes.NEXUS) >= 2);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.PYLON);
            result.If(() => Count(UnitTypes.STALKER) > 5);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Count(UnitTypes.STARGATE) > 0);
            result.Building(UnitTypes.FLEET_BEACON);
            result.Building(UnitTypes.ROBOTICS_FACILITY, () => TotalEnemyCount(UnitTypes.REAPER) < 2);
            result.If(() => TimingAttackTask.Task.AttackSent);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Count(UnitTypes.IMMORTAL) >= 3);
            result.Building(UnitTypes.ASSIMILATOR, 2, () => Count(UnitTypes.IMMORTAL) >= 3 && Minerals() >= 300);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.STARGATE);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.FORGE, 2);
            result.Upgrade(UpgradeType.ProtossGroundWeapons);
            result.Upgrade(UpgradeType.ProtossGroundArmor);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.TWILIGHT_COUNSEL);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.GATEWAY, 2);
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (bot.Frame >= 22.4 * (LogLabel.FoundJensii ? 60 : 30) && !IntroductionSent)
            {
                IntroductionSent = true;
                bot.Chat("This is ThreeWayLover. GLHF!");
            }
            if (bot.Frame == (int)(22.4 * 60 * 6))
            {
                bot.Chat("You have fallen for my deception!");
            }
            if (bot.Frame == (int)(22.4 * (60 * 6 + 5)))
            {
                bot.Chat("I'm not ThreeWayLover.");
            }
            if (bot.Frame == (int)(22.4 * (60 * 6 + 10)))
            {
                bot.Chat("I AM TYR!");
                RevealSent = true;
            }

            if (Count(UnitTypes.STARGATE) == 0)
                GasWorkerTask.WorkersPerGas = 2;
            else
                BalanceGas();

            if (Count(UnitTypes.NEXUS) >= 2)
                bot.buildingPlacer.ReservedLocation.Clear();

            if (WorkerScoutTask.Task.BaseCircled())
            {
                WorkerScoutTask.Task.Stopped = true;
                WorkerScoutTask.Task.Clear();
            }

            StutterController.Stopped = Completed(UnitTypes.STALKER) < 12;

            foreach (Base b in bot.BaseManager.Bases)
            {
                if (bot.Frame % 2 == 0)
                    break;
                if (b.ResourceCenter == null)
                    continue;
                if (SC2Util.DistanceSq(b.BaseLocation.Pos, bot.MapAnalyzer.StartLocation) <= 4)
                    continue;
                int mineral = Random.Next(b.BaseLocation.MineralFields.Count);
                if (b.BaseLocation.MineralFields[mineral] != null)
                    b.ResourceCenter.Order(Abilities.MOVE, b.BaseLocation.MineralFields[mineral].Tag);
            }

            foreach (Agent agent in bot.UnitManager.Agents.Values)
            {
                if (bot.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 3 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Main.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, bot.TargetManager.PotentialEnemyStartLocations[0]);
            }

            bot.NexusAbilityManager.Stopped = Count(UnitTypes.STALKER) == 0;
            bot.NexusAbilityManager.PriotitizedAbilities.Add(917);

            SaveWorkersTask.Task.Stopped = bot.Frame >= 22.4 * 60 * 7 || EnemyCount(UnitTypes.CYCLONE) == 0 || !Natural.UnderAttack;
            if (SaveWorkersTask.Task.Stopped)
                SaveWorkersTask.Task.Clear();

            WorkerTask.Task.EvacuateThreatenedBases = true;
            
            TimingAttackTask.Task.DefendOtherAgents = false;

            if (TotalEnemyCount(UnitTypes.REAPER) >= 3)
            {
                TimingAttackTask.Task.RequiredSize = 10;
                TimingAttackTask.Task.RetreatSize = 0;
            }
            else
            {
                TimingAttackTask.Task.RequiredSize = 15;
                TimingAttackTask.Task.RetreatSize = 0;
            }
            
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = Completed(UnitTypes.STALKER) + Completed(UnitTypes.IMMORTAL) >= 15 ? 120 : 20;
            DefenseTask.GroundDefenseTask.DrawDefenderRadius = 80;

            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 30;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = Completed(UnitTypes.STALKER) + Completed(UnitTypes.IMMORTAL) >= 15 ? 120 : 20;
            DefenseTask.AirDefenseTask.DrawDefenderRadius = 80;

            bot.TargetManager.SkipPlanetaries = true;
        }
    }
}
