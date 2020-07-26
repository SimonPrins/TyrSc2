using SC2Sharp.Agents;
using SC2Sharp.Tasks;
using static SC2Sharp.Builds.BuildLists.ConditionalStep;

namespace SC2Sharp.Builds.BuildLists
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

            if (Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(UpgradeId))
                return new NextItem();
            UpgradeType upgradeType = UpgradeType.LookUp[UpgradeId];
            if (Bot.Main.UnitManager.ActiveOrders.Contains(upgradeType.Ability))
                return new NextItem();

            while (upgradeType.Previous > 0 && !Bot.Main.Observation.Observation.RawData.Player.UpgradeIds.Contains(upgradeType.Previous))
            {
                upgradeType = UpgradeType.LookUp[upgradeType.Previous];
                if (Bot.Main.UnitManager.ActiveOrders.Contains(upgradeType.Ability))
                    return new NextItem();
            }


            foreach (Agent agent in ProductionTask.Task.Units)
            {
                if (!upgradeType.ProducingUnits.Contains(agent.Unit.UnitType))
                    continue;

                if (agent.Unit.Orders != null && agent.Unit.Orders.Count > 0)
                    continue;
                
                if (Bot.Main.Frame - agent.LastOrderFrame < 5)
                    continue;

                Bot.Main.ReservedGas += upgradeType.Gas;
                Bot.Main.ReservedMinerals += upgradeType.Minerals;

                if (Bot.Main.Build.Gas() < 0)
                    new NextItem();
                if (Bot.Main.Build.Minerals() < 0)
                    return new NextList();

                agent.Order((int)upgradeType.Ability);

                return new NextItem();
            }

            return new NextItem();
        }
    }
}
