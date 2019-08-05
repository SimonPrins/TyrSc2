using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class OneBaseStalker : Build
    {
        public int RequiredSize = 8;
        public bool ProxyPylon = false;
        private bool PylonPlaced = false;
        public bool HallucinationScout = false;
        private bool PhoenixScoutSent = false;
        private bool MainScouted = false;
        private bool ThirdScouted = false;
        private Point2D EnemyNatural = null;
        private Point2D EnemyThird = null;

        public bool VoidrayTransition = false;

        public override string Name()
        {
            return "OneBaseStalker";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            if (Tyr.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Tyr.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Tyr.Bot.BaseManager.Pocket.BaseLocation.Pos);
            if (ProxyPylon && !PylonPlaced)
                PlacePylonTask.Enable();
            ScoutTask.Enable();
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController());
            MicroControllers.Add(new DodgeBallController());
            MicroControllers.Add(new TargetUnguardedBuildingsController());
            MicroControllers.Add(new AdvanceController());
            MicroControllers.Add(new StutterController());

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            tyr.TargetManager.PrefferDistant = false;

            Set += ProtossBuildUtil.Pylons(() => Count(UnitTypes.PYLON) > 0 && Count(UnitTypes.CYBERNETICS_CORE) > 0);
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.STALKER, 3);
            result.Train(UnitTypes.VOID_RAY);
            result.Upgrade(UpgradeType.WarpGate);
            result.If(() => Count(UnitTypes.STARGATE) > 0 || TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) == 0 || !VoidrayTransition);
            result.Train(UnitTypes.STALKER, 8);
            result.Train(UnitTypes.SENTRY, 1, () => HallucinationScout && !PhoenixScoutSent);
            result.Train(UnitTypes.STALKER);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.PYLON);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.STARGATE, () => TotalEnemyCount(UnitTypes.ROBOTICS_FACILITY) > 0 && VoidrayTransition);
            result.Building(UnitTypes.GATEWAY, () => Count(UnitTypes.STALKER) >= 2);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY, () => Minerals() >= 250 && Count(UnitTypes.STALKER) >= 8);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            TimingAttackTask.Task.DefendOtherAgents = false;
            TimingAttackTask.Task.RequiredSize = RequiredSize;
            if (!PylonPlaced)
                foreach (Agent agent in tyr.UnitManager.Agents.Values)
                    if (agent.Unit.UnitType == UnitTypes.PYLON && SC2Util.DistanceSq(agent.Unit.Pos, tyr.MapAnalyzer.StartLocation) >= 40 * 40)
                    {
                        PylonPlaced = true;
                        PlacePylonTask.Task.Clear();
                        PlacePylonTask.Task.Stopped = true;
                    }

            
            foreach (Agent agent in tyr.Units())
            {
                if (agent.Unit.UnitType != UnitTypes.PHOENIX)
                    continue;
                if (EnemyThird != null && agent.DistanceSq(EnemyThird) <= 8 * 8)
                    ThirdScouted = true;
                if (agent.DistanceSq(tyr.TargetManager.PotentialEnemyStartLocations[0]) <= 8 * 8)
                    MainScouted = true;
            }
            ScoutTask.Task.ScoutType = UnitTypes.PHOENIX;
            if (EnemyNatural == null)
                EnemyNatural = tyr.MapAnalyzer.GetEnemyNatural().Pos;
            if (EnemyThird == null)
                EnemyThird = tyr.MapAnalyzer.GetEnemyThird().Pos;
            if (!ThirdScouted)
                ScoutTask.Task.Target = EnemyThird;
            else if (!MainScouted)
                ScoutTask.Task.Target = tyr.TargetManager.PotentialEnemyStartLocations[0];
            else
            ScoutTask.Task.Target = EnemyNatural;

            if (Count(UnitTypes.PHOENIX) > 0)
                PhoenixScoutSent = true;

            if (!PhoenixScoutSent)
            {
                foreach (Agent agent in tyr.Units())
                {
                    if (agent.Unit.UnitType != UnitTypes.SENTRY)
                        continue;
                    if (agent.Unit.Energy < 75)
                        continue;
                    // Hallucinate scouting phoenix.
                    agent.Order(154);
                }
            }
        }
    }
}
