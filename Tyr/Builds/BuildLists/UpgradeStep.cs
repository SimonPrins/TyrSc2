using Tyr.Agents;
using Tyr.Tasks;
using static Tyr.Builds.BuildLists.ConditionalStep;

namespace Tyr.Builds.BuildLists
{
    public class UpgradeStep : BuildStep
    {
        public uint UpgradeId;
        public int Number = 1000000;
        public Test Condition = () => { return true; };

        public UpgradeStep(uint upgradeID)
        {
            UpgradeId = upgradeID;
        }

        public UpgradeStep(uint upgradeID, int number)
        {
            UpgradeId = upgradeID;
            Number = number;
        }

        public UpgradeStep(uint upgradeID, Test condition)
        {
            UpgradeId = upgradeID;
            Condition = condition;
        }

        public UpgradeStep(uint upgradeID, int number, Test condition)
        {
            UpgradeId = upgradeID;
            Number = number;
            Condition = condition;
        }

        public override string ToString()
        {
            return "Upgrade " + UpgradeId + " " + Number;
        }

        public StepResult Perform(BuildListState state)
        {
            if (!Condition.Invoke())
                return new NextItem();

            if (Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(UpgradeId))
                return new NextItem();
            UpgradeType upgradeType = UpgradeType.LookUp[UpgradeId];
            if (Bot.Bot.UnitManager.ActiveOrders.Contains(upgradeType.Ability))
                return new NextItem();

            while (upgradeType.Previous > 0 && !Bot.Bot.Observation.Observation.RawData.Player.UpgradeIds.Contains(upgradeType.Previous))
            {
                upgradeType = UpgradeType.LookUp[upgradeType.Previous];
                if (Bot.Bot.UnitManager.ActiveOrders.Contains(upgradeType.Ability))
                    return new NextItem();
            }


            foreach (Agent agent in ProductionTask.Task.Units)
            {
                if (!upgradeType.ProducingUnits.Contains(agent.Unit.UnitType))
                    continue;

                if (agent.Unit.Orders != null && agent.Unit.Orders.Count > 0)
                    continue;
                
                if (Bot.Bot.Frame - agent.LastOrderFrame < 5)
                    continue;

                Bot.Bot.ReservedGas += upgradeType.Gas;
                Bot.Bot.ReservedMinerals += upgradeType.Minerals;

                if (Bot.Bot.Build.Gas() < 0)
                    new NextItem();
                if (Bot.Bot.Build.Minerals() < 0)
                    return new NextList();

                agent.Order((int)upgradeType.Ability);

                return new NextItem();
            }

            return new NextItem();
        }
    }
}
