using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Zerg
{
    public class MassZergling : Build
    {
        private int MeleeUpgrade = 0;
        private int ArmorUpgrade = 0;
        private int ResearchingUpgrades = 0;
        public bool EnableDefense = false;

        public bool SpineDefense = false;

        public bool AllowHydraTransition = false;
        private int HydraTransitionFrame = 1000000000;

        private DefenseSquadTask DefendEnemyNaturalTask;
        
        public override string Name()
        {
            return "MassZergling";
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
            OverlordSuicideTask.Enable();
            SafeZerglingsFromReapersTask.Enable();

            BaseLocation enemyNatural = Bot.Main.MapAnalyzer.GetEnemyNatural();
            if (enemyNatural != null)
            {
                Base enemyNaturalBase = null;
                foreach (Base b in Bot.Main.BaseManager.Bases)
                    if (SC2Util.DistanceSq(b.BaseLocation.Pos, enemyNatural.Pos) <= 2 * 2)
                    {
                        enemyNaturalBase = b;
                        break;
                    }
                DefendEnemyNaturalTask = new DefenseSquadTask(enemyNaturalBase, UnitTypes.ZERGLING);
                DefendEnemyNaturalTask.MaxDefenders = 100;
                DefendEnemyNaturalTask.AlwaysNeeded = true;
                DefendEnemyNaturalTask.DraftFromFarAway = true;
                DefendEnemyNaturalTask.DefendRange = 12;
                DefendEnemyNaturalTask.RetreatMoveCommand = true;

                PotentialHelper potential = new PotentialHelper(enemyNatural.Pos);
                potential.Magnitude = 10;
                potential.From(Bot.Main.MapAnalyzer.GetEnemyRamp());
                DefendEnemyNaturalTask.OverrideIdleLocation = potential.Get();

                potential = new PotentialHelper(enemyNatural.Pos);
                potential.Magnitude = 5;
                potential.From(Bot.Main.MapAnalyzer.GetEnemyRamp());
                DefendEnemyNaturalTask.OverrideDefenseLocation = potential.Get();
                DefenseSquadTask.Enable(DefendEnemyNaturalTask);
            }
        }
        
        public override Build OverrideBuild()
        {
            if (EnableDefense)
                return ZergBuildUtil.GetDefenseBuild();
            else
                return null;
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new ZerglingController());
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new InfestorController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new QueenTransfuseController());
            TimingAttackTask.Task.BeforeControllers.Add(new SoftLeashController(UnitTypes.ZERGLING, UnitTypes.HYDRALISK, 12));

            Set += ZergBuildUtil.Overlords();
            Set += WorkerRushDefense();
            Set += Tech();
            Set += Hydralisks();
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

        private BuildList Tech()
        {
            BuildList result = new BuildList();
            result.If(() => !AllowHydraTransition);
            result.If(() => { return Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) > 0; });
            result.Building(UnitTypes.EVOLUTION_CHAMBER, 2);
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Morph(UnitTypes.DRONE, 34);
            result.If(() => { return Completed(UnitTypes.LAIR) + Completed(UnitTypes.HIVE) > 0; });
            result.Morph(UnitTypes.OVERSEER, 2);
            result.If(() => { return MeleeUpgrade + ArmorUpgrade + ResearchingUpgrades >= 4; });
            result.Building(UnitTypes.INFESTATION_PIT);
            return result;
        }

        private BuildList Hydralisks()
        {
            BuildList result = new BuildList();
            result.If(() => Bot.Main.Frame >= HydraTransitionFrame);
            result.Morph(UnitTypes.DRONE, 14);
            result.Building(UnitTypes.HATCHERY, 2);
            result.Morph(UnitTypes.DRONE, 6);
            result.Train(UnitTypes.QUEEN, 2);
            result.Building(UnitTypes.EXTRACTOR);
            result.Building(UnitTypes.EXTRACTOR, () => Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 2);
            result.Train(UnitTypes.LAIR, 1);
            result.Building(UnitTypes.EXTRACTOR, () => Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) < 2);
            result.Morph(UnitTypes.DRONE, 6, () => Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 2);
            result.Building(UnitTypes.HYDRALISK_DEN, () => Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 2 && Completed(UnitTypes.LAIR) > 0);
            result.Morph(UnitTypes.HYDRALISK, 5, () => Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 2 && Completed(UnitTypes.HYDRALISK_DEN) > 0);
            result.Building(UnitTypes.HATCHERY);
            result.Train(UnitTypes.QUEEN, 3);
            result.Morph(UnitTypes.DRONE, 5);
            result.Building(UnitTypes.HYDRALISK_DEN, () => Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) < 2);
            result.Morph(UnitTypes.DRONE, 8);
            result.Morph(UnitTypes.HYDRALISK, 5, () => Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) < 2);
            result.Morph(UnitTypes.OVERSEER, 2);
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Morph(UnitTypes.DRONE, 5);
            result.Morph(UnitTypes.HYDRALISK, 10);
            result.Morph(UnitTypes.INFESTOR, 4, () => Completed(UnitTypes.INFESTATION_PIT) > 0);
            result.Morph(UnitTypes.DRONE, 20);
            result.Upgrade(UpgradeType.GroovedSpines);
            result.Upgrade(UpgradeType.MuscularAugments);
            result.Building(UnitTypes.INFESTATION_PIT, () => Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.COLOSUS) > 0);
            result.Building(UnitTypes.EXTRACTOR, 2, () => Bot.Main.EnemyStrategyAnalyzer.TotalCount(UnitTypes.COLOSUS) > 0);
            result.Upgrade(UpgradeType.PathogenGlands);
            result.Upgrade(UpgradeType.NeuralParasite);
            result.Morph(UnitTypes.HYDRALISK, 10);
            result.Building(UnitTypes.EVOLUTION_CHAMBER, 2);
            result.Upgrade(UpgradeType.ZergMissileWeapons);
            result.Upgrade(UpgradeType.ZergGroundArmor);
            result.Building(UnitTypes.HATCHERY);
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Train(UnitTypes.QUEEN, 4);
            result.Morph(UnitTypes.HYDRALISK, 50);
            return result;
        }

        private BuildList Zerglings()
        {
            BuildList result = new BuildList();
            result.If(() => Bot.Main.Frame < HydraTransitionFrame);
            result.If(() => Count(UnitTypes.HATCHERY) >= 2 && Count(UnitTypes.EXTRACTOR) > 0 && Count(UnitTypes.DRONE) >= 20 && Count(UnitTypes.QUEEN) >= 2 && (!SpineDefense || Count(UnitTypes.SPINE_CRAWLER) >= 2));
            result.Morph(UnitTypes.ZERGLING, 8);
            result.If(() => AvailableMineralPatches() > 12 || Count(UnitTypes.HATCHERY) >= 3);
            result.Morph(UnitTypes.ZERGLING, 12);
            result.If(() =>
            {
                return Gas() < 150
                || Count(UnitTypes.HIVE) > 0
                || Completed(UnitTypes.INFESTATION_PIT) == 0
                || AllowHydraTransition;
            });
            result.If(() => { return !TimingAttackTask.Task.AttackSent || Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) > 0 || AllowHydraTransition; });
            result.Morph(UnitTypes.ZERGLING, 20);
            result.If(() => { return Completed(UnitTypes.EVOLUTION_CHAMBER) < 2 || ResearchingUpgrades + (MeleeUpgrade / 3) + (ArmorUpgrade / 3) == 2 || AllowHydraTransition ;});
            result.Morph(UnitTypes.ZERGLING, 400);
            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            result.Morph(UnitTypes.DRONE, 14);
            result.Building(UnitTypes.HATCHERY, 2);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Morph(UnitTypes.OVERLORD, 2);
            result.Morph(UnitTypes.DRONE, 3);
            result.Train(UnitTypes.QUEEN, 2);
            result.Building(UnitTypes.EXTRACTOR);
            result.Morph(UnitTypes.DRONE, 3);
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, NaturalDefensePos, 2, () => Completed(UnitTypes.HATCHERY) >= 2 && SpineDefense);
            //result.Morph(UnitTypes.ZERGLING, 4);
            //result.Morph(UnitTypes.ZERGLING, 4);
            result.Building(UnitTypes.HATCHERY, () => { return AvailableMineralPatches() <= 12; });
            //result.If(() =>
            //{
            //    return Gas() < 100
            //    || Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(66)
            //    || Tyr.Bot.UnitManager.ActiveOrders.Contains(1253);
            //});
            //result.Morph(UnitTypes.ZERGLING, 12);
            //result.If(() =>
            //{
            //    return Gas() < 150
            //    || Count(UnitTypes.HIVE) > 0
            //    || Completed(UnitTypes.INFESTATION_PIT) == 0;
            //});
            //result.If(() => { return !TimingAttackTask.Task.AttackSent || Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) > 0; });
            //result.Morph(UnitTypes.ZERGLING, 20);
            //result.If(() => { return Completed(UnitTypes.EVOLUTION_CHAMBER) < 2 || ResearchingUpgrades + (MeleeUpgrade / 3) + (ArmorUpgrade / 3) == 2; });
            //result.Morph(UnitTypes.ZERGLING, 400);
            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            tyr.DrawText("HydraTransitionFrame: " + HydraTransitionFrame);
            int enemyNaturalWorkerCount = 0;
            BaseLocation enemyNatural = tyr.MapAnalyzer.GetEnemyNatural();
            bool enemyNaturalRemains = enemyNatural == null;
            foreach (Unit enemy in tyr.Enemies())
            {
                if (enemyNatural == null)
                    break;
                if (UnitTypes.ResourceCenters.Contains(enemy.UnitType) && !enemy.IsFlying && SC2Util.DistanceSq(enemy.Pos, enemyNatural.Pos) < 2 * 2)
                {
                    enemyNaturalRemains = true;
                    continue;
                }
                if (!UnitTypes.WorkerTypes.Contains(enemy.UnitType))
                    continue;
                if (SC2Util.DistanceSq(enemy.Pos, enemyNatural.Pos) > 14 * 14)
                    continue;
                enemyNaturalWorkerCount++;
            }

            bool zerglingInEnemyNatural = false;
            foreach (Agent agent in tyr.UnitManager.Agents.Values)
                if (agent.Unit.UnitType == UnitTypes.ZERGLING && agent.DistanceSq(enemyNatural.Pos) <= 10 * 10)
                    zerglingInEnemyNatural = true;

            if (AllowHydraTransition && HydraTransitionFrame >= 1000000000 && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.PHOTON_CANNON) >= 3)
                HydraTransitionFrame = tyr.Frame;
            if (AllowHydraTransition && HydraTransitionFrame >= 1000000000 && tyr.Frame >= 22.4 * 60 * 5.5)
                HydraTransitionFrame = tyr.Frame;
            if (AllowHydraTransition && HydraTransitionFrame >= 1000000000 && Count(UnitTypes.ZERGLING) >= 25 && zerglingInEnemyNatural && enemyNaturalWorkerCount == 0 && !enemyNaturalRemains)
                HydraTransitionFrame = (int)(tyr.Frame + 22.4 * 10);
            if (AllowHydraTransition && HydraTransitionFrame >= 1000000000 && Count(UnitTypes.ZERGLING) >= 25 && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.REAPER) >= 2)
                HydraTransitionFrame = (int)(tyr.Frame + 22.4 * 10);

            if (DefendEnemyNaturalTask != null)
            {
                DefendEnemyNaturalTask.Stopped = tyr.EnemyStrategyAnalyzer.Count(UnitTypes.PHOTON_CANNON) < 3 || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.COLOSUS) > 0;
                if (DefendEnemyNaturalTask.Stopped)
                    DefendEnemyNaturalTask.Clear();
            }

            if (HydraTransitionFrame < 1000000000 || tyr.EnemyRace != Race.Terran)
            {
                OverlordSuicideTask.Task.Stopped = true;
                OverlordSuicideTask.Task.Clear();
            }
            else
                OverlordSuicideTask.Task.Stopped = false;

            if (TimingAttackTask.Task.AttackSent && !OverlordSuicideTask.Task.Suicide)
            {
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.ZERGLING && agent.DistanceSq(tyr.TargetManager.PotentialEnemyStartLocations[0]) <= 80 * 80)
                OverlordSuicideTask.Task.Suicide = true;
            }

            if (tyr.TargetManager.PotentialEnemyStartLocations.Count <= 1)
                WorkerScoutTask.Task.Stopped = true;

            if (UpgradeType.LookUp[UpgradeType.MetabolicBoost].Done())
            {
                SafeZerglingsFromReapersTask.Task.Stopped = true;
                SafeZerglingsFromReapersTask.Task.Clear();
            }

            if (MeleeUpgrade == 0 && Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(53))
                MeleeUpgrade = 1;
            else if (MeleeUpgrade == 1 && Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(54))
                MeleeUpgrade = 2;
            else if (MeleeUpgrade == 2 && Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(55))
                MeleeUpgrade = 3;

            if (ArmorUpgrade == 0 && Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(56))
                ArmorUpgrade = 1;
            else if (ArmorUpgrade == 1 && Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(57))
                ArmorUpgrade = 2;
            else if (ArmorUpgrade == 2 && Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(58))
                ArmorUpgrade = 3;

            ResearchingUpgrades = 0;
            for (uint ability = 1186; ability <= 1191; ability++)
                if (Bot.Main.UnitManager.ActiveOrders.Contains(ability))
                    ResearchingUpgrades++;
            
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
            else if (TimingAttackTask.Task.AttackSent || tyr.Frame >= HydraTransitionFrame)
                GasWorkerTask.WorkersPerGas = 3;
            else
                GasWorkerTask.WorkersPerGas = 0;

            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.PHOTON_CANNON) >= 3 && Completed(UnitTypes.HYDRALISK) < 20 && Completed(UnitTypes.ZERGLING) >= 40)
            {
                TimingAttackTask.Task.Clear();
                TimingAttackTask.Task.Stopped = true;
            }
            else
                TimingAttackTask.Task.Stopped = false;

            if (tyr.EnemyStrategyAnalyzer.Count(UnitTypes.PHOTON_CANNON) >= 3 && Completed(UnitTypes.ZERGLING) >= 40)
            {
                TimingAttackTask.Task.RequiredSize = 70;
                TimingAttackTask.Task.RetreatSize = 5;
            }
            else if (tyr.Frame >= HydraTransitionFrame && Count(UnitTypes.HYDRALISK) < 20 && !UpgradeType.LookUp[UpgradeType.MetabolicBoost].Done())
            {
                TimingAttackTask.Task.RequiredSize = 50;
                TimingAttackTask.Task.RetreatSize = 5;
            }
            else if (tyr.Frame >= HydraTransitionFrame && tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.COLOSUS) > 0)
            {
                TimingAttackTask.Task.RequiredSize = 40;
                TimingAttackTask.Task.RetreatSize = 8;
            }
            else if (tyr.Frame >= HydraTransitionFrame)
            {
                TimingAttackTask.Task.RequiredSize = 30;
                TimingAttackTask.Task.RetreatSize = 5;
            }
            else if (TimingAttackTask.Task.AttackSent)
            {
                TimingAttackTask.Task.RequiredSize = 20;
                TimingAttackTask.Task.RetreatSize = 0;
            }
            else
            {
                TimingAttackTask.Task.RequiredSize = 50;
                TimingAttackTask.Task.RetreatSize = 0;
            }

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 18;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 55;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 20;
            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 18;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 55;
        }

        public override void Produce(Bot tyr, Agent agent)
        {
            if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Minerals() >= 150 && Gas() >= 100
                    && Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) == 0
                    && TimingAttackTask.Task.AttackSent
                    && !AllowHydraTransition)
                    agent.Order(1216);
                else if (agent.Unit.UnitType == UnitTypes.LAIR
                    && Completed(UnitTypes.INFESTATION_PIT) > 0
                    && Minerals() >= 200 && Gas() >= 150
                    && Count(UnitTypes.ZERGLING) >= 20)
                    agent.Order(1218);
            }
            else if (agent.Unit.UnitType == UnitTypes.SPAWNING_POOL)
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
            else if (agent.Unit.UnitType == UnitTypes.EVOLUTION_CHAMBER)
            {
                if (HydraTransitionFrame < tyr.Frame)
                    return;
                if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(53)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1186))
                    agent.Order(1186);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(56)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1189))
                    agent.Order(1189);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(54)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1187))
                    agent.Order(1187);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(57)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1190))
                    agent.Order(1190);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(55)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1188))
                    agent.Order(1188);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(58)
                    && !Bot.Main.UnitManager.ActiveOrders.Contains(1191))
                    agent.Order(1191);
            }
        }
    }
}
