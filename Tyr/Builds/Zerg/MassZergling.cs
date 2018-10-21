using Tyr.Agents;
using Tyr.Builds.BuildLists;
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
        }

        public override Build OverrideBuild()
        {
            return ZergBuildUtil.GetDefenseBuild();
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new ZerglingController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new QueenTransfuseController());

            Set += ZergBuildUtil.Overlords();
            Set += Tech();
            Set += MainBuild();
        }

        private BuildList Tech()
        {
            BuildList result = new BuildList();
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

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            result.Morph(UnitTypes.DRONE, 14);
            result.Building(UnitTypes.HATCHERY, 2);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Morph(UnitTypes.OVERLORD, 2);
            result.Morph(UnitTypes.DRONE, 6);
            result.Building(UnitTypes.SPINE_CRAWLER, Natural, NaturalDefensePos, 2, () => { return Completed(UnitTypes.HATCHERY) >= 2; });
            result.Morph(UnitTypes.ZERGLING, 4);
            result.Building(UnitTypes.EXTRACTOR);
            result.Morph(UnitTypes.ZERGLING, 4);
            result.Building(UnitTypes.HATCHERY, () => { return AvailableMineralPatches() <= 12; });
            result.If(() =>
            {
                return Gas() < 100
                || Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(66)
                || Tyr.Bot.UnitManager.ActiveOrders.Contains(1253);
            });
            result.Morph(UnitTypes.ZERGLING, 12);
            result.If(() =>
            {
                return Gas() < 150
                || Count(UnitTypes.HIVE) > 0
                || Completed(UnitTypes.INFESTATION_PIT) == 0;
            });
            result.If(() => { return !TimingAttackTask.Task.AttackSent || Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) > 0; });
            result.Morph(UnitTypes.ZERGLING, 20);
            result.If(() => { return Completed(UnitTypes.EVOLUTION_CHAMBER) < 2 || ResearchingUpgrades + (MeleeUpgrade / 3) + (ArmorUpgrade / 3) == 2; });
            result.Morph(UnitTypes.ZERGLING, 400);
            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            if (MeleeUpgrade == 0 && Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(53))
                MeleeUpgrade = 1;
            else if (MeleeUpgrade == 1 && Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(54))
                MeleeUpgrade = 2;
            else if (MeleeUpgrade == 2 && Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(55))
                MeleeUpgrade = 3;

            if (ArmorUpgrade == 0 && Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(56))
                ArmorUpgrade = 1;
            else if (ArmorUpgrade == 1 && Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(57))
                ArmorUpgrade = 2;
            else if (ArmorUpgrade == 2 && Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(58))
                ArmorUpgrade = 3;

            ResearchingUpgrades = 0;
            for (uint ability = 1186; ability <= 1191; ability++)
                if (Tyr.Bot.UnitManager.ActiveOrders.Contains(ability))
                    ResearchingUpgrades++;
            
            if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(66)
                && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1253))
            {
                if (Gas() < 92)
                    BaseWorkers.WorkersPerGas = 3;
                else if (Gas() < 96)
                    BaseWorkers.WorkersPerGas = 2;
                else if (Gas() < 100)
                    BaseWorkers.WorkersPerGas = 1;
                else if (Gas() >= 100)
                    BaseWorkers.WorkersPerGas = 0;
            }
            else if (TimingAttackTask.Task.AttackSent)
                BaseWorkers.WorkersPerGas = 3;
            else
                BaseWorkers.WorkersPerGas = 0;

            //if (Completed(UnitTypes.ZERGLING) >= 24)
            TimingAttackTask.Task.RequiredSize = 40;
            //else
            //    TimingAttackTask.Task.RequiredSize = Math.Max(25, TimingAttackTask.Task.RequiredSize);
            TimingAttackTask.Task.RetreatSize = 10;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 18;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 55;
            DefenseTask.AirDefenseTask.MainDefenseRadius = 20;
            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 18;
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 55;
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (UnitTypes.ResourceCenters.Contains(agent.Unit.UnitType))
            {
                if (Minerals() >= 150
                    && Completed(UnitTypes.QUEEN) < Count(UnitTypes.HATCHERY) + Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE)
                    && Completed(UnitTypes.SPAWNING_POOL) > 0)
                {
                    agent.Order(1632);
                    CollectionUtil.Increment(tyr.UnitManager.Counts, UnitTypes.QUEEN);
                }
                else if (Minerals() >= 150 && Gas() >= 100
                    && Count(UnitTypes.LAIR) + Count(UnitTypes.HIVE) == 0
                    && TimingAttackTask.Task.AttackSent)
                    agent.Order(1216);
                else if (agent.Unit.UnitType == UnitTypes.LAIR
                    && Completed(UnitTypes.INFESTATION_PIT) > 0
                    && Minerals() >= 200 && Gas() >= 150
                    && Count(UnitTypes.ZERGLING) >= 20)
                    agent.Order(1218);
            }
            else if (agent.Unit.UnitType == UnitTypes.SPAWNING_POOL)
            {
                if (Minerals() >= 100
                    && Gas() >= 100
                    && !Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(66))
                    agent.Order(1253);
                else if (Minerals() >= 200
                    && Gas() >= 200
                    && !Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(65))
                    agent.Order(1252);
            }
            else if (agent.Unit.UnitType == UnitTypes.EVOLUTION_CHAMBER)
            {
                if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(53)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1186))
                    agent.Order(1186);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(56)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1189))
                    agent.Order(1189);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(54)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1187))
                    agent.Order(1187);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(57)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1190))
                    agent.Order(1190);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(55)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1188))
                    agent.Order(1188);
                else if (!Tyr.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(58)
                    && !Tyr.Bot.UnitManager.ActiveOrders.Contains(1191))
                    agent.Order(1191);
            }
        }
    }
}
