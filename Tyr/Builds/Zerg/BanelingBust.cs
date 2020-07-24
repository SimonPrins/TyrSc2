using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Zerg
{
    public class BanelingBust : Build
    {
        public override string Name()
        {
            return "BanelingBust";
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

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new FleeCyclonesController());
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
            result.Morph(UnitTypes.DRONE, 2);
            result.Upgrade(UpgradeType.MetabolicBoost);
            result.Morph(UnitTypes.ZERGLING, 16);
            //result.Building(UnitTypes.BANELING_NEST);
            result.Morph(UnitTypes.ZERGLING, 80);


            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            if (Gas() >= 100 || UpgradeType.LookUp[UpgradeType.MetabolicBoost].Started())
                GasWorkerTask.WorkersPerGas = 0;
            
            if (TimingAttackTask.Task.AttackSent)
                TimingAttackTask.Task.RequiredSize = 10;
            else
                TimingAttackTask.Task.RequiredSize = 30;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;
            DefenseTask.GroundDefenseTask.ExpandDefenseRadius = 15;

            if (EnemyCount(UnitTypes.REAPER) == 0)
                SafeZerglingsFromReapersTask.Task.StopAndClear(TimingAttackTask.Task.AttackSent);
        }
    }
}
