using System.Collections.Generic;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.Micro;
using SC2Sharp.Tasks;

namespace SC2Sharp.Builds.Protoss
{
    public class PvZAdeptIntoVoidray : Build
    {
        private TimingAttackTask attackTask = new TimingAttackTask() { RequiredSize = 14 };
        private FearEnemyController FearSpinesController = new FearEnemyController(UnitTypes.ADEPT, UnitTypes.SPINE_CRAWLER, 12) { CourageCount = 30 };

        public override string Name()
        {
            return "PvZAdeptIntoVoidray";
        }

        public override void OnStart(Bot bot)
        {
            bot.TaskManager.Add(new DefenseTask());
            bot.TaskManager.Add(attackTask);

            BlockExpandTask.Enable();
            if (bot.BaseManager.Pocket != null)
                bot.TaskManager.Add(new ScoutProxyTask(bot.BaseManager.Pocket.BaseLocation.Pos));

            MicroControllers.Add(FearSpinesController);
            MicroControllers.Add(new StutterController());


            Set += ProtossBuildUtil.Pylons();
            Set += Units();
            Set += MainBuildList();
        }

        private BuildList Units()
        {
            BuildList result = new BuildList();

            result.Train(UnitTypes.PROBE, 20);
            result.Train(UnitTypes.PROBE, 40, () => Count(UnitTypes.NEXUS) >= 2);
            result.Train(UnitTypes.ADEPT, 25);
            result.Train(UnitTypes.VOID_RAY, 25);

            return result;
        }

        private BuildList MainBuildList()
        {
            BuildList result = new BuildList();

            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.GATEWAY);
            result.Building(UnitTypes.CYBERNETICS_CORE);
            result.Building(UnitTypes.GATEWAY);
            result.If(() => Count(UnitTypes.ADEPT) >= 8);
            result.Building(UnitTypes.NEXUS);
            result.Building(UnitTypes.ASSIMILATOR, 2);
            result.Building(UnitTypes.STARGATE, 2);
            result.Building(UnitTypes.ASSIMILATOR);
            result.Building(UnitTypes.STARGATE, () => Count(UnitTypes.VOID_RAY) >= 2);

            return result;
        }

        public override void OnFrame(Bot bot)
        { }
    }
}
