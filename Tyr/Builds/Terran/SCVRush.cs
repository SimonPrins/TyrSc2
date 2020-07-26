using SC2APIProtocol;
using SC2Sharp.Agents;
using SC2Sharp.Builds.BuildLists;
using SC2Sharp.StrategyAnalysis;
using SC2Sharp.Tasks;

namespace SC2Sharp.Builds.Protoss
{
    public class SCVRush : Build
    {
        private SCVRushTask WorkerRushTask = new SCVRushTask();
        private int LastReinforcementsFrame = 0;
        private bool MessageSent = false;

        public override string Name()
        {
            return "SCVRush";
        }

        public override void InitializeTasks()
        {
            base.InitializeTasks();
            Bot.Main.TaskManager.Add(WorkerRushTask);
            WorkerRushTask.Stopped = false;
        }

        public override void OnStart(Bot bot)
        {
            Set += SupplyDepots();
        }

        public BuildList SupplyDepots()
        {
            BuildList result = new BuildList();
            
            result.If(() =>
            {
                return Build.FoodUsed()
                    + Bot.Main.UnitManager.Count(UnitTypes.COMMAND_CENTER)
                    >= Build.ExpectedAvailableFood() - 1
                    && Build.ExpectedAvailableFood() < 200;
            });
            result += new BuildingStep(UnitTypes.SUPPLY_DEPOT);
            result.Goto(0);

            return result;
        }

        public override void OnFrame(Bot bot)
        {
            if (!MessageSent)
                    if (bot.Enemies().Count > 0)
                    {
                        MessageSent = true;
                        bot.Chat("Prepare to be TICKLED! :D");
                    }

            if (bot.Frame - LastReinforcementsFrame >= 100
                && WorkerTask.Task.Units.Count >= (Lifting.Get().Detected ? 22 : 12)
                && !Lifting.Get().Detected)
            {
                LastReinforcementsFrame = bot.Frame;
                WorkerRushTask.TakeWorkers += 6;
            }
        }

        public override void Produce(Bot bot, Agent agent)
        {
            if (agent.Unit.UnitType == UnitTypes.COMMAND_CENTER
                && Minerals() >= 50
                && (!WorkerRushTask.Stopped || Count(UnitTypes.SCV) < 20))
                agent.Order(524);
        }
    }
}
