using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;

namespace SC2Sharp.Builds.Protoss
{
    public class OneBaseAdept : Build
    {
        private TimingAttackTask attackTask = new TimingAttackTask() { RequiredSize = 8 };
        private AdeptPhaseEnemyMainController AdeptPhaseEnemyMainController = new AdeptPhaseEnemyMainController();
        private FearEnemyController FearSpinesController = new FearEnemyController(UnitTypes.ADEPT, UnitTypes.SPINE_CRAWLER, 12) { CourageCount = 30 };

        public override string Name()
        {
            return "OneBaseAdept";
        }

        public override void OnStart(Bot bot)
        {
            bot.TaskManager.Add(new DefenseTask());
            bot.TaskManager.Add(attackTask);
            bot.TaskManager.Add(new WorkerScoutTask());
            if (bot.BaseManager.Pocket != null)
                bot.TaskManager.Add(new ScoutProxyTask(bot.BaseManager.Pocket.BaseLocation.Pos));

            MicroControllers.Add(AdeptPhaseEnemyMainController);
            MicroControllers.Add(FearSpinesController);
            MicroControllers.Add(new StutterController());

            
            Set += ProtossBuildUtil.Pylons();
            Set += MainBuildList();
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result += new BuildingStep(UnitTypes.GATEWAY);
            result += new BuildingStep(UnitTypes.ASSIMILATOR);
            result += new BuildingStep(UnitTypes.GATEWAY);
            result += new BuildingStep(UnitTypes.CYBERNETICS_CORE);
            result += new BuildingStep(UnitTypes.GATEWAY, 2);

            return result;
        }

        public override void OnFrame(Bot bot)
        { }

        public override void Produce(Bot bot, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 19 - Completed(UnitTypes.ASSIMILATOR))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.GATEWAY)
            {
                if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                    && Minerals() >= 100
                    && Gas() >= 25)
                    agent.Order(922);
            }
        }
    }
}
