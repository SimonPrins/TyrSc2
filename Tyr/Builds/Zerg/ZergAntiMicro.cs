using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;
using SC2Sharp.Util;

namespace SC2Sharp.Builds.Zerg
{
    public class ZergAntiMicro : Build
    {
        public override string Name()
        {
            return "ZergAntiMicro";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            QueenInjectTask.Enable();
            QueenDefenseTask.Enable();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            SafeZerglingsFromReapersTask.Enable();
        }

        public override void OnStart(Bot bot)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new HitAndRunController());
            Set += ZergBuildUtil.Overlords();
            Set += Units();
            Set += MainBuild();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Morph(UnitTypes.ZERGLING, 14, () => Completed(UnitTypes.SPIRE) == 0 && !TimingAttackTask.Task.AttackSent);
            result.Morph(UnitTypes.MUTALISK, 8, () => Count(UnitTypes.DRONE) >= 20);
            result.Morph(UnitTypes.MUTALISK, 4, () => Count(UnitTypes.DRONE) >= 20 && Count(UnitTypes.HYDRALISK_DEN) > 0);
            result.Morph(UnitTypes.OVERSEER, 2, () => Count(UnitTypes.HYDRALISK) > 0);
            result.Morph(UnitTypes.HYDRALISK, 100, () => Count(UnitTypes.DRONE) >= 20);

            return result;
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            result.Building(UnitTypes.HATCHERY);
            result.Morph(UnitTypes.OVERLORD);
            result.Morph(UnitTypes.DRONE, 14);
            result.Building(UnitTypes.HATCHERY);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Train(UnitTypes.QUEEN, 2);
            result.Building(UnitTypes.EXTRACTOR);
            //result.Upgrade(UpgradeType.MetabolicBoost, () => Gas() >= 100);
            result.Train(UnitTypes.LAIR, 1, () => Gas() >= 100);
            result.Building(UnitTypes.SPORE_CRAWLER, Main, Main.MineralLinePos, () => Count(UnitTypes.LAIR) > 0);
            result.Building(UnitTypes.SPORE_CRAWLER, Natural, Natural.MineralLinePos, () => Count(UnitTypes.LAIR) > 0);
            result.Train(UnitTypes.QUEEN, 2, () => Count(UnitTypes.LAIR) > 0);
            result.Morph(UnitTypes.DRONE, 6);
            result.Building(UnitTypes.SPIRE);
            result.Building(UnitTypes.HYDRALISK_DEN, () => Count(UnitTypes.MUTALISK) >= 8);
            result.Building(UnitTypes.EXTRACTOR);
            result.Morph(UnitTypes.DRONE, 4);
            result.Building(UnitTypes.EXTRACTOR, 3);
            result.If(() => Count(UnitTypes.MUTALISK) >= 9);
            result.Morph(UnitTypes.DRONE, 16);
            result.If(() => Count(UnitTypes.MUTALISK) >= 12);
            result.Building(UnitTypes.HATCHERY);
            result.Morph(UnitTypes.DRONE, 20);
            return result;
        }

        public override void OnFrame(Bot bot)
        {
            //if (Count(UnitTypes.SPIRE) > 0)
                TimingAttackTask.Task.RequiredSize = 40;

            TimingAttackTask.Task.ExcludeUnitTypes.Add(UnitTypes.QUEEN);
            TimingAttackTask.Task.ExcludeUnitTypes.Add(UnitTypes.MUTALISK);
            TimingAttackTask.Task.ExcludeUnitTypes.Add(UnitTypes.ZERGLING);

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 15;

            //if (EnemyCount(UnitTypes.REAPER) == 0)
           //     SafeZerglingsFromReapersTask.Task.StopAndClear(TimingAttackTask.Task.AttackSent);
        }
    }
}
