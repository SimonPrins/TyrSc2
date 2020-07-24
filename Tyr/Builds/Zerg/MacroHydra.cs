using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Zerg
{
    public class MacroHydra : Build
    {
        private bool SmellCheese = false;
        private bool ProxyCannons = false;
        private bool SuspectBanshees = false;
        private bool CannonDefenseDetected = false;
        private bool TempestDetected = false;
        private bool FourRaxSuspected = false;

        StutterController StutterController = new StutterController();
        StutterForwardController StutterForwardController = new StutterForwardController();

        public override string Name()
        {
            return "MacroHydra";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            GroupedAttackTask.Enable();
            WorkerScoutTask.Enable();
            QueenInjectTask.Enable();
            DefenseTask.Enable();
            QueenDefenseTask.Enable();
            ArmyOverseerTask.Enable();
            DefendingOverseerTask.Enable();
            QueenTumorTask.Enable();
            OverlordScoutTask.Enable();
            CreeperLordTask.Enable();
            OverlordAtNaturalTask.Enable();
            ParasitedBCTask.Enable();
            DefenseSquadTask.Enable(false, UnitTypes.QUEEN);
            MechDestroyExpandsTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new CorruptorController());
            MicroControllers.Add(new QueenTransfuseController());
            MicroControllers.Add(new InfestorController());
            MicroControllers.Add(new KillParasitedController());
            MicroControllers.Add(new InfestorController());
            MicroControllers.Add(StutterController);
            MicroControllers.Add(StutterForwardController);

            Set += ZergBuildUtil.Overlords();
            Set += DTSpores();
            Set += ReaperSpines();
            Set += BansheeSpores();
            Set += DefendFourRax();
            Set += Units();
            Set += AntiLifting();
        }

        private BuildList DTSpores()
        {
            BuildList result = new BuildList();

            result.If(() => Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.DARK_TEMPLAR) + Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.DARK_SHRINE) > 0);
            foreach (Base b in Bot.Main.BaseManager.Bases)
                if (b != Main && b != Natural)
                    result.Building(UnitTypes.SPORE_CRAWLER, b, () => b.ResourceCenterFinishedFrame >= 0 && Bot.Main.Frame - b.ResourceCenterFinishedFrame >= 224);

            return result;
        }

        private BuildList ReaperSpines()
        {
            BuildList result = new BuildList();

            result.If(() => SuspectBanshees);
            foreach (Base b in Bot.Main.BaseManager.Bases)
                result.Building(UnitTypes.SPINE_CRAWLER, b, b.MineralSide1, () => b.ResourceCenterFinishedFrame >= 0 && Bot.Main.Frame - b.ResourceCenterFinishedFrame >= 224);
            foreach (Base b in Bot.Main.BaseManager.Bases)
                result.Building(UnitTypes.SPINE_CRAWLER, b, b.MineralSide2, () => b.ResourceCenterFinishedFrame >= 0 && Bot.Main.Frame - b.ResourceCenterFinishedFrame >= 224);

            return result;
        }

        private BuildList BansheeSpores()
        {
            BuildList result = new BuildList();

            result.If(() => SuspectBanshees && Count(UnitTypes.OVERSEER) >= 2);
            foreach (Base b in Bot.Main.BaseManager.Bases)
                result.Building(UnitTypes.SPORE_CRAWLER, b, b.MineralLinePos, () => b.ResourceCenterFinishedFrame >= 0 && Bot.Main.Frame - b.ResourceCenterFinishedFrame >= 224);
            
            result.If(() => Completed(UnitTypes.DRONE) >= 16 && Count(UnitTypes.QUEEN) >= 8);
            foreach (Base b in Bot.Main.BaseManager.Bases)
                result.Building(UnitTypes.SPORE_CRAWLER, b, b.OppositeMineralLinePos, () => b.ResourceCenterFinishedFrame >= 0 && Bot.Main.Frame - b.ResourceCenterFinishedFrame >= 224);
            

            return result;
        }

        /**
         * Deprecated, as cannonRushes seem to be beatable without it.
         */
        private BuildList DefendCannonRush()
        {
            BuildList result = new BuildList();

            result.If(() => ProxyCannons);
            result.Morph(UnitTypes.OVERLORD);
            result.Morph(UnitTypes.DRONE, 14);
            result.Morph(UnitTypes.OVERLORD);
            result.Building(UnitTypes.EXTRACTOR);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Train(UnitTypes.QUEEN, 4);
            result.Morph(UnitTypes.DRONE, 4);
            result.Building(UnitTypes.ROACH_WARREN);
            result.Morph(UnitTypes.ROACH, 10);
            result.Train(UnitTypes.LAIR, 1);
            result.Building(UnitTypes.EXTRACTOR);
            result.Morph(UnitTypes.DRONE, 2);
            result.Building(UnitTypes.HYDRALISK_DEN);
            result.Morph(UnitTypes.HYDRALISK, 10);
            result.Upgrade(UpgradeType.GroovedSpines);
            result.Morph(UnitTypes.RAVAGER, 4);
            result.Morph(UnitTypes.HYDRALISK, 10);
            result.Morph(UnitTypes.ROACH, 10);
            result.Morph(UnitTypes.HYDRALISK, 10);
            result.Morph(UnitTypes.ROACH, 10);
            result.Morph(UnitTypes.HYDRALISK, 10);
            result.Morph(UnitTypes.ROACH, 10);
            result.Morph(UnitTypes.HYDRALISK, 10);
            result.Morph(UnitTypes.ROACH, 10);

            return result;
        }

        private BuildList DefendFourRax()
        {
            BuildList result = new BuildList();

            result.If(() => FourRaxSuspected && Count(UnitTypes.ROACH) + Count(UnitTypes.HYDRALISK) < 15);
            result.Building(UnitTypes.HATCHERY);
            result.Morph(UnitTypes.OVERLORD);
            result.Morph(UnitTypes.DRONE, 13);
            result.Morph(UnitTypes.DRONE, () => Count(UnitTypes.HATCHERY) < 2);
            result.Building(UnitTypes.HATCHERY);
            result.Morph(UnitTypes.OVERLORD);
            result.Morph(UnitTypes.DRONE);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Train(UnitTypes.QUEEN, 2);
            result.Morph(UnitTypes.DRONE, 6);
            result.Building(UnitTypes.EXTRACTOR);
            result.Building(UnitTypes.ROACH_WARREN);
            result.Morph(UnitTypes.DRONE, 6);
            result.Train(UnitTypes.QUEEN, 6);
            result.Morph(UnitTypes.ROACH, 10);
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, NaturalDefensePos, 2);
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, NaturalDefensePos, 2, () => Count(UnitTypes.ROACH) >= 10);
            result.Morph(UnitTypes.ROACH, 5);

            return result;
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.If(() => !FourRaxSuspected || Count(UnitTypes.ROACH) + Count(UnitTypes.HYDRALISK) >= 15);
            result.Building(UnitTypes.HATCHERY);
            result.Morph(UnitTypes.OVERLORD);
            result.Morph(UnitTypes.DRONE, 13);
            result.Morph(UnitTypes.DRONE, () => Count(UnitTypes.HATCHERY) < 2);
            result.Building(UnitTypes.HATCHERY);
            result.Morph(UnitTypes.OVERLORD);
            result.Morph(UnitTypes.DRONE);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, NaturalDefensePos, 2, () => SmellCheese);
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, NaturalDefensePos, 2, () => SmellCheese && !ProxyCannons);
            result.Train(UnitTypes.QUEEN, 2);
            result.Train(UnitTypes.QUEEN, 4, () => SuspectBanshees);
            result.Train(UnitTypes.QUEEN, 2, () => !SmellCheese);
            result.Train(UnitTypes.QUEEN, 20, () => TempestDetected && Minerals() >= 450);
            result.Morph(UnitTypes.ZERGLING, 10, () => SmellCheese && Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.PHOTON_CANNON) < 4);
            result.Morph(UnitTypes.ZERGLING, 10, () => SmellCheese && !ProxyCannons && Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.PHOTON_CANNON) < 4);
            result.Morph(UnitTypes.DRONE, 6, () => !SuspectBanshees || Count(UnitTypes.QUEEN) >= 6 || Minerals() >= 250);
            result.Building(UnitTypes.EXTRACTOR);
            result.Train(UnitTypes.QUEEN, 6, () => SuspectBanshees);
            result.Morph(UnitTypes.ZERGLING, 10, () => !ProxyCannons && (Bot.Main.EnemyRace == Race.Protoss || SmellCheese) && Bot.Main.EnemyStrategyAnalyzer.Count(UnitTypes.PHOTON_CANNON) < 4);
            result.Train(UnitTypes.QUEEN, 2, () => SmellCheese);
            result.Building(UnitTypes.EXTRACTOR, () => SuspectBanshees && Minerals() >= 300);
            result.Train(UnitTypes.LAIR, 1);
            result.Morph(UnitTypes.OVERSEER, 2, () => SuspectBanshees);
            result.Train(UnitTypes.QUEEN, 14, () => SuspectBanshees);
            result.Upgrade(UpgradeType.MetabolicBoost, () => SuspectBanshees && Completed(UnitTypes.QUEEN) >= 6 && Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.HELLION) == 0);
            result.Morph(UnitTypes.ZERGLING, 4, () => SuspectBanshees && Completed(UnitTypes.QUEEN) >= 6 && Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.HELLION) == 0 && UpgradeType.LookUp[UpgradeType.MetabolicBoost].Done());
            result.Building(UnitTypes.INFESTATION_PIT, () => SuspectBanshees);
            result.Upgrade(UpgradeType.PathogenGlands, () => SuspectBanshees);
            result.Morph(UnitTypes.INFESTOR, 4, () => Completed(UnitTypes.INFESTATION_PIT) > 0 && SuspectBanshees);
            result.Building(UnitTypes.HYDRALISK_DEN, () => SuspectBanshees);
            result.Upgrade(UpgradeType.GroovedSpines, () => SuspectBanshees);
            result.Upgrade(UpgradeType.MuscularAugments, () => SuspectBanshees);
            result.Morph(UnitTypes.HYDRALISK, 10, () => SuspectBanshees);
            result.Train(UnitTypes.QUEEN, 6);
            result.Morph(UnitTypes.DRONE, 10);
            result.Building(UnitTypes.HATCHERY, () => !SuspectBanshees || Completed(UnitTypes.HYDRALISK) >= 10);
            result.Building(UnitTypes.HYDRALISK_DEN, () => !SuspectBanshees);
            result.Building(UnitTypes.EXTRACTOR);
            result.Morph(UnitTypes.DRONE, 10);
            result.Morph(UnitTypes.HYDRALISK, 10);
            result.Upgrade(UpgradeType.GroovedSpines);
            result.Upgrade(UpgradeType.MuscularAugments);
            result.Morph(UnitTypes.DRONE, 10);
            result.Building(UnitTypes.HATCHERY);
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Morph(UnitTypes.DRONE, 10);
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Morph(UnitTypes.OVERSEER, 2, () => !SuspectBanshees);
            result.Building(UnitTypes.EVOLUTION_CHAMBER);
            result.Upgrade(UpgradeType.ZergMissileWeapons);
            result.Building(UnitTypes.EVOLUTION_CHAMBER);
            result.Upgrade(UpgradeType.ZergGroundArmor);
            result.Building(UnitTypes.INFESTATION_PIT, () => UpgradeType.LookUp[UpgradeType.ZergMissileWeapons1].Started() && !SuspectBanshees);
            result.Morph(UnitTypes.INFESTOR, 4, () => Completed(UnitTypes.INFESTATION_PIT) > 0 && !SuspectBanshees);
            result.Upgrade(UpgradeType.PathogenGlands);
            result.Upgrade(UpgradeType.NeuralParasite, () => Bot.Main.EnemyRace != Race.Terran || (Minerals() >= 600 && Gas() >= 600));
            result.Train(UnitTypes.HIVE, 1, () => UpgradeType.LookUp[UpgradeType.ZergMissileWeapons2].Started());
            result.Morph(UnitTypes.HYDRALISK, 15);
            result.Morph(UnitTypes.DRONE, 10);
            result.Morph(UnitTypes.INFESTOR, 2, () => Completed(UnitTypes.INFESTATION_PIT) > 0);
            result.Building(UnitTypes.SPIRE);
            result.Building(UnitTypes.HATCHERY);
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Morph(UnitTypes.HYDRALISK, 5);
            result.Morph(UnitTypes.CORRUPTOR, 10);
            result.Building(UnitTypes.HATCHERY, 2);
            result.Morph(UnitTypes.DRONE, 10);
            result.Building(UnitTypes.EXTRACTOR, 4);
            result.Morph(UnitTypes.HYDRALISK, 100);

            return result;
        }

        private BuildList AntiLifting()
        {
            BuildList result = new BuildList();
            result.If(() => { return Lifting.Get().Detected; });
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Building(UnitTypes.SPIRE);
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            if (tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BATTLECRUISER) > 0)
            {
                GroupedAttackTask.Task.RequiredSize = 40;
                GroupedAttackTask.Task.RetreatSize = 10;
            }
            else
            {
                GroupedAttackTask.Task.RequiredSize = 60;
                GroupedAttackTask.Task.RetreatSize = 20;
            }

            if (!CannonDefenseDetected && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.PHOTON_CANNON) + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.FORGE) >= 0 && tyr.Frame < 22.4 * 60 * 4)
                CannonDefenseDetected = true;

            if (!TempestDetected && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.TEMPEST) > 0)
                TempestDetected = true;

            MechDestroyExpandsTask.Task.RequiredSize = 10;
            MechDestroyExpandsTask.Task.MaxSize = 10;
            MechDestroyExpandsTask.Task.RetreatSize = 4;
            MechDestroyExpandsTask.Task.UnitType = UnitTypes.ZERGLING;
            MechDestroyExpandsTask.Task.Stopped = !CannonDefenseDetected
                || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.STALKER) 
                + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZEALOT) 
                + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.IMMORTAL) 
                + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.PHOENIX) 
                + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.VOID_RAY) 
                + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ORACLE) 
                + tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.CARRIER) > 0 
                || tyr.Frame <= 22.4 * 60 * 4.5;

            if (SuspectBanshees)
            {
                if (tyr.Frame % 22 == 0)
                    foreach (Agent agent in tyr.UnitManager.Agents.Values)
                        if (agent.Unit.UnitType == UnitTypes.OVERLORD || agent.Unit.UnitType == UnitTypes.OVERSEER)
                            agent.Order(1692);

                DefenseTask.AirDefenseTask.Priority = 8;

                IdleTask.Task.RetreatFarOverlords = false;

                OverlordAtNaturalTask.Task.Stopped = true;
                OverlordAtNaturalTask.Task.Clear();
                QueenTumorTask.Task.PlaceTumorsInMain = true;
                QueenTumorTask.Task.Priority = 9;
                StutterForwardController.Stopped = false;
                StutterController.Stopped = true;
                CreeperLordTask.Task.Stopped = false;
                /*
                foreach (DefenseSquadTask task in DefenseSquadTask.Tasks)
                {
                    task.Priority = 8;
                    task.Stopped = false;
                    task.MaxDefenders = Math.Max(0, Completed(UnitTypes.QUEEN) / Count(UnitTypes.HATCHERY) - 1);
                }
                */
            }
            else
            {
                StutterForwardController.Stopped = true;
                StutterController.Stopped = false;
                CreeperLordTask.Task.Stopped = true;
                foreach (DefenseSquadTask task in DefenseSquadTask.Tasks)
                    task.Stopped = true;
            }

            if (!SuspectBanshees &&
                (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.BANSHEE) > 0
                || (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.STARPORT) + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.STARPORT_TECH_LAB) > 0 && tyr.Frame < 22.4 * 60 * 4)))
                SuspectBanshees = true;

            if (FourRax.Get().Detected)
                FourRaxSuspected = true;
            if (!FourRaxSuspected)
            {
                Point2D enemyRamp = tyr.MapAnalyzer.GetEnemyRamp();
                int enemyBarrackWallCount = 0;
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (enemyRamp == null)
                        break;
                    if (enemy.UnitType == UnitTypes.BARRACKS && SC2Util.DistanceSq(enemy.Pos, enemyRamp) <= 5.5 * 5.5)
                        enemyBarrackWallCount++;
                }
                if (enemyBarrackWallCount >= 2)
                {
                    WorkerScoutTask.Task.Stopped = true;
                    WorkerScoutTask.Task.Clear();
                    FourRaxSuspected = true;
                }
            }

            if (SuspectBanshees && Completed(UnitTypes.HYDRALISK) < 10)
                QueenInjectTask.DefenseRadius = 12;
            else
                QueenInjectTask.DefenseRadius = 10;
            
            WorkerTask.Task.StopTransfers = SuspectBanshees && Completed(UnitTypes.HYDRALISK) < 10;
            
            if (SuspectBanshees)
            {
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 14;
                DefenseTask.AirDefenseTask.ExpandDefenseRadius = 14;
                DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
                DefenseTask.AirDefenseTask.MainDefenseRadius = 20;
            }
            else if (SmellCheese && Completed(UnitTypes.HYDRALISK) <= 20)
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 14;
            else
                DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 20;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 100;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 100;

            if (SuspectBanshees)
            {
                if (Natural.Owner == tyr.PlayerId)
                    IdleTask.Task.OverrideTarget = Natural.BaseLocation.Pos;
                else
                    IdleTask.Task.OverrideTarget = Main.BaseLocation.Pos;
            }

            if (SmellCheese
                && !ProxyCannons
                && Completed(UnitTypes.ZERGLING) < 20
                && Count(UnitTypes.HYDRALISK_DEN) == 0)
                GasWorkerTask.WorkersPerGas = 0;
            else
                BalanceGas();

            /*
            if ((ThreeGate.Get().Detected || (Tyr.Bot.EnemyStrategyAnalyzer.Count(UnitTypes.GATEWAY) == 0 && tyr.Frame >= 22.4 * 60 * 2))
                && !Expanded.Get().Detected
                && tyr.EnemyRace == Race.Protoss
                && Tyr.Bot.Frame <= 22.4 * 60 * 3.5)
                SmellCheese = true;
                */

            if (!ProxyCannons)
            {
                foreach (Unit enemy in tyr.Enemies())
                {
                    if (enemy.UnitType == UnitTypes.PHOTON_CANNON && SC2Util.DistanceSq(enemy.Pos, tyr.MapAnalyzer.StartLocation) <= 40 * 40)
                    {
                        ProxyCannons = true;
                        break;
                    }
                }
            }

            OverlordScoutTask.Task.ScoutMain = true;
            if (
                tyr.EnemyStrategyAnalyzer.Count(UnitTypes.MARINE)
                + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.VIKING_FIGHTER)
                + tyr.EnemyStrategyAnalyzer.Count(UnitTypes.STALKER) > 0
                || tyr.EnemyStrategyAnalyzer.Count(UnitTypes.PHOTON_CANNON) >= 3)
            {
                OverlordScoutTask.Task.Stopped = true;
                OverlordScoutTask.Task.Clear();
            }
            else if (SuspectBanshees)
            {
                OverlordScoutTask.Task.ScoutLocation = tyr.MapAnalyzer.GetEnemyRamp();
                if (OverlordScoutTask.Task.ScoutLocation != null)
                {
                    Unit supplyDepot = null;
                    int distances = 0;
                    foreach (Unit enemy in tyr.Enemies())
                    {
                        if (enemy.UnitType != UnitTypes.SUPPLY_DEPOT && enemy.UnitType != UnitTypes.SUPPLY_DEPOT_LOWERED)
                            continue;
                        if (SC2Util.DistanceSq(OverlordScoutTask.Task.ScoutLocation, enemy.Pos) > 5 * 5)
                            continue;
                        if (tyr.MapAnalyzer.WallDistances[(int)enemy.Pos.X, (int)enemy.Pos.Y] < distances)
                            continue;
                        supplyDepot = enemy;
                        distances = tyr.MapAnalyzer.WallDistances[(int)enemy.Pos.X, (int)enemy.Pos.Y];
                    }
                    if (supplyDepot != null)
                    {
                        OverlordScoutTask.Task.ScoutLocation = SC2Util.To2D(supplyDepot.Pos);
                        tyr.DrawText("SupplyDepot WallDistance: " + distances);
                    }
                }
            }
        }
    }
}