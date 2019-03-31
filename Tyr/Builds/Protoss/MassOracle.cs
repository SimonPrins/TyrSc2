using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Managers;
using Tyr.Micro;
using Tyr.Tasks;
using Tyr.Util;

namespace Tyr.Builds.Protoss
{
    public class MassOracle : Build
    {
        public int RequiredSize = 20;

        private bool OraclesDone = false;

        public override string Name()
        {
            return "MassOracle";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            HideUnitsTask.Enable();
            DefenseTask.Enable();
            TimingAttackTask.Enable();
            OracleHarassBasesTask.Enable();
            WorkerScoutTask.Enable();
            ArmyObserverTask.Enable();
            if (Tyr.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Tyr.Bot.BaseManager.Pocket.BaseLocation.Pos);
        }

        public override void OnStart(Tyr tyr)
        {
            MicroControllers.Add(new OracleController());
            MicroControllers.Add(new StutterController());

            foreach (WorkerDefenseTask task in WorkerDefenseTask.Tasks)
                task.OnlyDefendInsideMain = true;

            Set += ProtossBuildUtil.Pylons();
            Set += ExpandBuildings();
            Set += MainBuildList();
        }

        private BuildList ExpandBuildings()
        {
            BuildList result = new BuildList();

            result.If(() => { return Minerals() >= 550 || OraclesDone || Count(UnitTypes.ORACLE) >= 6; });
            foreach (Base b in Tyr.Bot.BaseManager.Bases)
            {
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95);
                result.Building(UnitTypes.GATEWAY, b, 2, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Completed(b, UnitTypes.PYLON) >= 1 && Minerals() >= 350);
                result.Building(UnitTypes.PYLON, b, () => b.ResourceCenter != null && b.ResourceCenter.Unit.BuildProgress >= 0.95 && Count(b, UnitTypes.GATEWAY) >= 2 && Minerals() >= 700);
            }

            return result;
        }
        
        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.NEXUS);
            if (Tyr.Bot.EnemyRace == Race.Zerg)
                result.Train(UnitTypes.ZEALOT, 1);
            else
                result.Train(UnitTypes.STALKER, 1);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.STARGATE, () => !OraclesDone);
            result.Train(UnitTypes.ORACLE, () => !OraclesDone);
            result.Building(UnitTypes.STARGATE, () => Count(UnitTypes.ORACLE) >= 1 && !OraclesDone);
            result.Building(UnitTypes.PYLON, Natural);
            result.If(() => Minerals() >= 550 || OraclesDone || Count(UnitTypes.ORACLE) >= 6);
            result.Building(UnitTypes.ROBOTICS_FACILITY);
            result.Train(UnitTypes.OBSERVER, 1);
            result.Train(UnitTypes.IMMORTAL);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR);
            if (Tyr.Bot.EnemyRace == Race.Zerg)
                result.Train(UnitTypes.ZEALOT);
            else
                result.Train(UnitTypes.STALKER, () => OraclesDone);

            return result;
        }

        public override void OnFrame(Tyr tyr)
        {
            TimingAttackTask.Task.RequiredSize = RequiredSize;

            tyr.NexusAbilityManager.PriotitizedAbilities.Add(TrainingType.LookUp[UnitTypes.ORACLE].Ability);

            HideUnitsTask.Task.UnitType = UnitTypes.ORACLE;
            if (Completed(UnitTypes.ORACLE) >= 6 || tyr.EnemyStrategyAnalyzer.TotalCount(UnitTypes.ZERGLING) >= 10)
            {
                HideUnitsTask.Task.Stopped = true;
                HideUnitsTask.Task.Clear();
            }
            if (Completed(UnitTypes.ORACLE) >= 6)
                OraclesDone = true;
            HideUnitsTask.Task.Target = SC2Util.To2D(tyr.MapAnalyzer.StartLocation);
        }

        public override void Produce(Tyr tyr, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && (Count(UnitTypes.PROBE) < 40 || Minerals() >= 250)
                && Count(UnitTypes.PROBE) < 50)
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
        }
    }
}
