using System;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.StrategyAnalysis;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Zerg
{
    public class LingUltra : Build
    {
        public override string Name()
        {
            return "LingUltra";
        }

        private bool GoingUltras = false;

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new StutterController());
            MicroControllers.Add(new TargetFireController(GetPriorities()));

            TimingAttackTask.Enable();
            WorkerScoutTask.Enable();
            QueenInjectTask.Enable();
            DefenseTask.Enable();
            QueenDefenseTask.Enable();
            ArmyOverseerTask.Enable();

            Set += ZergBuildUtil.Overlords();
            Set += Spores();
            Set += Zerglings();
            Set += Ultras();
            Set += MainBuild();
            Set += AntiLifting();
        }

        public PriorityTargetting GetPriorities()
        {
            PriorityTargetting priorities = new PriorityTargetting();

            priorities.DefaultPriorities.MaxRange = 10;
            priorities.DefaultPriorities[UnitTypes.LARVA] = -1;
            priorities.DefaultPriorities[UnitTypes.EGG] = -1;

            foreach (uint t in UnitTypes.CombatUnitTypes)
                priorities[UnitTypes.HYDRALISK][t] = 1;

            return priorities;
        }

        private BuildList Spores()
        {
            BuildList result = new BuildList();

            result.If(() => Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.DARK_TEMPLAR)
                + Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.DARK_SHRINE)
                + Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.MEDIVAC)
                + Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.LIBERATOR)
                + Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.LIBERATOR_AG)
                + Bot.Bot.EnemyStrategyAnalyzer.TotalCount(UnitTypes.BANSHEE) > 0);
            foreach (Base b in Bot.Bot.BaseManager.Bases)
                if (b != Main && b != Natural)
                    result.Building(UnitTypes.SPORE_CRAWLER, b, () => b.ResourceCenterFinishedFrame >= 0 && Bot.Bot.Frame - b.ResourceCenterFinishedFrame >= 224);

            return result;
        }

        private BuildList Zerglings()
        {
            BuildList result = new BuildList();

            result.If(() => !GoingUltras);
            result.If(() => Count(UnitTypes.DRONE) >= 55 && Count(UnitTypes.HATCHERY) >= 4 && Count(UnitTypes.EVOLUTION_CHAMBER) >= 2);
            result.Morph(UnitTypes.ZERGLING, 130);
            result.If(() => !Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(UpgradeType.AdrenalGlands));
            result.Morph(UnitTypes.ZERGLING, 30);

            return result;
        }

        private BuildList Ultras()
        {
            BuildList result = new BuildList();

            result.If(() => GoingUltras);
            result.If(() => Count(UnitTypes.DRONE) >= 55 && Count(UnitTypes.HATCHERY) >= 4);
            result.Morph(UnitTypes.ULTRALISK, 2);
            result.Upgrade(UpgradeType.AnabolicSynthesis);
            result.Upgrade(UpgradeType.ChitinousPlating);
            result.Upgrade(UpgradeType.ZergGroundArmor);
            result.Upgrade(UpgradeType.ZergMeleeWeapons);
            result.Morph(UnitTypes.ULTRALISK, 14);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.HATCHERY);
            result.Morph(UnitTypes.OVERLORD);
            result.Morph(UnitTypes.DRONE, 14);
            result.Building(UnitTypes.HATCHERY);
            result.Morph(UnitTypes.OVERLORD);
            result.Building(UnitTypes.EXTRACTOR);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Train(UnitTypes.QUEEN, 4);
            result.Morph(UnitTypes.DRONE, 6);
            result.Train(UnitTypes.LAIR, 1);
            result.Morph(UnitTypes.ZERGLING, 10, () => !GoingUltras);
            result.Morph(UnitTypes.DRONE, 10);
            result.Upgrade(UpgradeType.MetabolicBoost);
            result.Upgrade(UpgradeType.AdrenalGlands);
            result.Building(UnitTypes.HATCHERY);
            result.Building(UnitTypes.INFESTATION_PIT);
            result.Morph(UnitTypes.DRONE, 10);
            result.Morph(UnitTypes.DRONE, 10);
            result.Morph(UnitTypes.ZERGLING, 10, () => !GoingUltras);
            result.Building(UnitTypes.EVOLUTION_CHAMBER);
            result.Building(UnitTypes.EVOLUTION_CHAMBER);
            result.Upgrade(UpgradeType.ZergGroundArmor);
            result.Upgrade(UpgradeType.ZergMeleeWeapons);
            result.Building(UnitTypes.HATCHERY);
            result.Train(UnitTypes.HIVE, 1);
            result.Train(UnitTypes.QUEEN, 6);
            result.Morph(UnitTypes.ZERGLING, 10, () => !GoingUltras);
            result.Building(UnitTypes.EXTRACTOR);
            result.Morph(UnitTypes.DRONE, 10);
            result.Morph(UnitTypes.OVERSEER, 2);
            result.Building(UnitTypes.EXTRACTOR, 4, () => GoingUltras);
            result.Morph(UnitTypes.DRONE, 10);
            result.Building(UnitTypes.ULTRALISK_CAVERN);
            result.Upgrade(UpgradeType.AnabolicSynthesis, () => GoingUltras);
            result.Upgrade(UpgradeType.ChitinousPlating, () => GoingUltras);
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.If(() => Count(UnitTypes.ULTRALISK) >= 12);
            result.Building(UnitTypes.HATCHERY);
            result.Building(UnitTypes.EXTRACTOR, 2);
            result.Building(UnitTypes.HATCHERY, 3);
            result.Morph(UnitTypes.DRONE, 10);
            result.Building(UnitTypes.EXTRACTOR, 6);

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
            DefenseTask.AirDefenseTask.MaxDefenseRadius = 100;
            DefenseTask.AirDefenseTask.ExpandDefenseRadius = 25;
            DefenseTask.GroundDefenseTask.MaxDefenseRadius = 100;

            BalanceGas();

            TimingAttackTask.Task.DefendOtherAgents = false;

            if (TimingAttackTask.Task.AttackSent)
                GoingUltras = true;

            if (TimingAttackTask.Task.AttackSent && Completed(UnitTypes.ULTRALISK) >= 12)
            {
                TimingAttackTask.Task.RequiredSize = 12;
                TimingAttackTask.Task.RetreatSize = 4;
            }
            else if (!Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(UpgradeType.AdrenalGlands))
            {
                TimingAttackTask.Task.RequiredSize = 160;
                TimingAttackTask.Task.RetreatSize = 0;
            }
            else
            {
                TimingAttackTask.Task.RequiredSize = 130;
                TimingAttackTask.Task.RetreatSize = 0;
            }
        }
    }
}