using Tyr.Agents;
using Tyr.Builds.BuildLists;
using Tyr.Micro;
using Tyr.Tasks;

namespace Tyr.Builds.Protoss
{
    public class DefensiveSentries : Build
    {
        public int RequiredSize = 10;
        private bool TyckleFightChatSent = false;
        private bool MessageSent = false;
        public bool DelayAttacking = false;

        public override string Name()
        {
            return "DefensiveSentries" + (DelayAttacking ? "DelayAttacking" : "");
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            DefenseTask.Enable();
            MassSentriesTask.Enable();
            if (Bot.Bot.TargetManager.PotentialEnemyStartLocations.Count > 1)
                WorkerScoutTask.Enable();
            if (Bot.Bot.BaseManager.Pocket != null)
                ScoutProxyTask.Enable(Bot.Bot.BaseManager.Pocket.BaseLocation.Pos);
            HallucinationAttackTask.Enable();
            WorkerRushDefenseTask.Enable();
            ForceFieldRampTask.Enable();
        }

        public override void OnStart(Bot tyr)
        {
            MicroControllers.Add(new FleeCyclonesController());
            MicroControllers.Add(new SentryController() { UseHallucaination = true, FleeEnemies = false });
            MicroControllers.Add(new DodgeBallController());

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

            result.Train(UnitTypes.PROBE, 16);
            result.Train(UnitTypes.SENTRY, 3);
            result.Upgrade(UpgradeType.WarpGate);
            result.Train(UnitTypes.SENTRY);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.PYLON, Main);
            result.If(() => Completed(UnitTypes.PYLON) > 0);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.CYBERNETICS_CORE, Main);
            result.Building(UnitTypes.GATEWAY, Main);
            result.Building(UnitTypes.SHIELD_BATTERY, Main, MainDefensePos);

            return result;
        }

        public override void OnFrame(Bot tyr)
        {
            if (DelayAttacking && tyr.Frame < 22.4 * 60 * 30)
            {
                MassSentriesTask.Task.Stopped = true;
                MassSentriesTask.Task.Clear();
            }
            else MassSentriesTask.Task.Stopped = false;

            tyr.TaskManager.CombatSimulation.SimulationLength = 0;
            MassSentriesTask.Task.RequiredSize = RequiredSize;
            MassSentriesTask.Task.RetreatSize = 6;

            DefenseTask.GroundDefenseTask.MainDefenseRadius = 20;

            if (MassSentriesTask.Task.AttackSent)
            {
                ForceFieldRampTask.Task.Stopped = true;
                ForceFieldRampTask.Task.Clear();
            }

            if (!TyckleFightChatSent && StrategyAnalysis.WorkerRush.Get().Detected)
            {
                TyckleFightChatSent = true;
                tyr.Chat("TICKLE FIGHT! :D");
            }

            if (!MessageSent)
                if (MassSentriesTask.Task.AttackSent)
                {
                    MessageSent = true;
                    tyr.Chat("Prepare to be TICKLED! :D");
                }

            foreach (Agent agent in tyr.UnitManager.Agents.Values)
            {
                if (tyr.Frame % 224 != 0)
                    break;
                if (agent.Unit.UnitType != UnitTypes.GATEWAY)
                    continue;

                if (Count(UnitTypes.NEXUS) < 2 && TimingAttackTask.Task.Units.Count == 0)
                    agent.Order(Abilities.MOVE, Main.BaseLocation.Pos);
                else
                    agent.Order(Abilities.MOVE, tyr.TargetManager.PotentialEnemyStartLocations[0]);
            }
        }
    }
}
