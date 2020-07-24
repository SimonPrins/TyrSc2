using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Zerg
{
    public class Muukzor : Build
    {
        public bool Hydras = false;
        public override string Name()
        {
            return "Muukzor";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            QueenInjectTask.Enable();
            QueenDefenseTask.Enable();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            SafeZerglingsFromReapersTask.Enable();
            ArmyOverseerTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new FearEnemyController(UnitTypes.MUTALISK, UnitTypes.MISSILE_TURRET, 10) { CourageCount = 20 });
            MicroControllers.Add(new HitAndRunController());
            Set += ZergBuildUtil.Overlords();
            Set += MainBuild();
        }

        private BuildList MainBuild()
        {
            BuildList result = new BuildList();
            result.Building(UnitTypes.HATCHERY);
            result.Morph(UnitTypes.OVERLORD);
            result.Morph(UnitTypes.DRONE, 14);
            result.Building(UnitTypes.SPAWNING_POOL);
            result.Building(UnitTypes.EXTRACTOR);
            result.Train(UnitTypes.QUEEN, 1);
            result.Morph(UnitTypes.ZERGLING, 6);
            result.Upgrade(UpgradeType.MetabolicBoost, () => Gas() >= 100);
            result.Train(UnitTypes.LAIR, () => Gas() >= 100 && UpgradeType.LookUp[UpgradeType.MetabolicBoost].Started());
            if (Hydras)
                result.Building(UnitTypes.HYDRALISK_DEN, () => Completed(UnitTypes.LAIR) > 0 && TimingAttackTask.Task.AttackSent);
            else
                result.Building(UnitTypes.SPIRE, () => Completed(UnitTypes.LAIR) > 0 && TimingAttackTask.Task.AttackSent);
            result.Building(UnitTypes.EXTRACTOR, () => Count(UnitTypes.SPIRE) + Count(UnitTypes.HYDRALISK_DEN) > 0);
            result.Morph(UnitTypes.DRONE, 3, () => Count(UnitTypes.SPIRE) + Count(UnitTypes.HYDRALISK_DEN) > 0);
            result.Morph(UnitTypes.DRONE, 2);
            result.Building(UnitTypes.SPORE_CRAWLER, Main, Main.MineralLinePos, () => TotalEnemyCount(UnitTypes.BANSHEE) > 0);
            result.Morph(UnitTypes.OVERSEER, 1, () => Count(UnitTypes.HYDRALISK) > 0);
            if (Hydras)
                result.Morph(UnitTypes.HYDRALISK, 100, () => Completed(UnitTypes.HYDRALISK_DEN) > 0);
            else
                result.Morph(UnitTypes.MUTALISK, 100, () => Completed(UnitTypes.SPIRE) > 0);
            result.Morph(UnitTypes.ZERGLING, 16);
            //result.Building(UnitTypes.BANELING_NEST);
            result.Morph(UnitTypes.ZERGLING, 12);


            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            ArmyOverseerTask.Task.IgnoreUnitTypes.Add(UnitTypes.ZERGLING);
            if (tyr.Frame < 10)
            {
                TimingAttackTask.Task.ExcludeUnitTypes.Add(UnitTypes.MUTALISK);
                TimingAttackTask.Task.ExcludeUnitTypes.Add(UnitTypes.HYDRALISK);
            }
            else if (Completed(UnitTypes.MUTALISK) >= 8)
                TimingAttackTask.Task.ExcludeUnitTypes.Remove(UnitTypes.MUTALISK);
            else if (Completed(UnitTypes.HYDRALISK) >= 8)
                TimingAttackTask.Task.ExcludeUnitTypes.Remove(UnitTypes.HYDRALISK);
            //if (Gas() >= 100 || UpgradeType.LookUp[UpgradeType.MetabolicBoost].Started())
            //    GasWorkerTask.WorkersPerGas = 0;

            if (TimingAttackTask.Task.AttackSent)
                TimingAttackTask.Task.RequiredSize = 10;
            else
                TimingAttackTask.Task.RequiredSize = 30;

            tyr.TargetManager.IgnoreFlyingBuildings = Completed(UnitTypes.MUTALISK) + Completed(UnitTypes.HYDRALISK) < 8;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 15;

            if (EnemyCount(UnitTypes.REAPER) == 0)
                SafeZerglingsFromReapersTask.Task.StopAndClear(TimingAttackTask.Task.AttackSent);
        }
    }
}
