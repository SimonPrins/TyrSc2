using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;

namespace SC2Sharp.Builds.Protoss
{
    public class TwoBaseAdept : Build
    {
        private TimingAttackTask attackTask = new TimingAttackTask() { RequiredSize = 20 };
        public override string Name()
        {
            return "TwoBaseAdept";
        }

        public override void OnStart(Bot bot)
        {
            bot.TaskManager.Add(new DefenseTask());
            bot.TaskManager.Add(attackTask);
            bot.TaskManager.Add(new WorkerScoutTask());
            if (bot.BaseManager.Pocket != null)
                bot.TaskManager.Add(new ScoutProxyTask(bot.BaseManager.Pocket.BaseLocation.Pos));
            MicroControllers.Add(new StutterController());

            Set += ProtossBuildUtil.Nexus(2);
            Set += ProtossBuildUtil.Pylons();
            Set += MainBuildList();
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result += new BuildingStep(UnitTypes.GATEWAY);
            result += new BuildingStep(UnitTypes.ASSIMILATOR);
            result += new BuildingStep(UnitTypes.CYBERNETICS_CORE);
            result += new BuildingStep(UnitTypes.GATEWAY, 3);
            result += new BuildingStep(UnitTypes.TWILIGHT_COUNSEL);
            result += new BuildingStep(UnitTypes.GATEWAY, 2);
            result.If(() => { return Minerals() >= 250; });
            result += new BuildingStep(UnitTypes.GATEWAY, 4);

            return result;
        }

        public override void OnFrame(Bot bot)
        { }

        public override void Produce(Bot bot, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.NEXUS
                && Minerals() >= 50
                && Count(UnitTypes.PROBE) < 35 - Completed(UnitTypes.ASSIMILATOR))
            {
                if (Count(UnitTypes.PROBE) < 13 || Count(UnitTypes.PYLON) > 0)
                    agent.Order(1006);
            }
            else if (agent.Unit.UnitType == UnitTypes.GATEWAY)
            {
                if (Minerals() >= 100
                    && (Completed(UnitTypes.CYBERNETICS_CORE) == 0 || Count(UnitTypes.ZEALOT) <= Count(UnitTypes.ADEPT)))
                    agent.Order(916);
                else if (Completed(UnitTypes.CYBERNETICS_CORE) > 0
                    && Minerals() >= 100
                    && Gas() >= 25)
                    agent.Order(922);
            }
            else if (agent.Unit.UnitType == UnitTypes.TWILIGHT_COUNSEL)
            {
                if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(130)
                    && Minerals() >= 100
                    && Gas() >= 100)
                    agent.Order(1594);
                else if (!Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(86)
                     && Minerals() >= 100
                     && Gas() >= 100)
                    agent.Order(1592);
            }
        }
    }
}
