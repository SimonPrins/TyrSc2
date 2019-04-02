using SC2APIProtocol;
using System;
using Tyr.Agents;
using Tyr.BuildingPlacement;
using Tyr.Tasks;
using Tyr.Util;
using static Tyr.Builds.BuildLists.ConditionalStep;

namespace Tyr.Builds.BuildLists
{
    public class TrainStep : BuildStep
    {
        public uint UnitType;
        public int Number = 1000000;
        public Test Condition = () => { return true; };

        public TrainStep(uint unitType)
        {
            UnitType = unitType;
        }

        public TrainStep(uint unitType, int number)
        {
            UnitType = unitType;
            Number = number;
        }

        public TrainStep(uint unitType, Test condition)
        {
            UnitType = unitType;
            Condition = condition;
        }

        public TrainStep(uint unitType, int number, Test condition)
        {
            UnitType = unitType;
            Number = number;
            Condition = condition;
        }

        public override string ToString()
        {
            if (!UnitTypes.LookUp.ContainsKey(UnitType))
                throw new System.Exception("Unable to find building with type: " + UnitType  + ". Please add it to the BuildingType class.");
            return "Train " + UnitTypes.LookUp[UnitType].Name + " " + Number;
        }

        public StepResult Perform(BuildListState state)
        {
            if (!Condition.Invoke())
                return new NextItem();

            if (UnitTypes.LookUp[UnitType].TechRequirement != 0
                && Tyr.Bot.UnitManager.Completed(UnitTypes.LookUp[UnitType].TechRequirement) == 0
                && UnitTypes.LookUp[UnitType].TechRequirement != UnitTypes.HATCHERY
                && UnitTypes.LookUp[UnitType].TechRequirement != UnitTypes.TECH_LAB)
            {
                Tyr.Bot.DrawText("Tech requirement not met to train " + UnitTypes.LookUp[UnitType].Name + " requires: " + UnitTypes.LookUp[UnitType].TechRequirement);
                return new NextItem();
            }

            int alreadyTrained = Tyr.Bot.UnitManager.Count(UnitType) + state.GetTraining(UnitType);
            TrainingType trainType = TrainingType.LookUp[UnitType];
            foreach (Agent agent in ProductionTask.Task.Units)
            {
                if (Build.FoodLeft() < trainType.Food)
                {
                    Tyr.Bot.DrawText("Not enough food to train " + UnitTypes.LookUp[UnitType].Name);
                    break;
                }
                if (alreadyTrained >= Number)
                    break;

                if (agent.Unit.BuildProgress < 0.99)
                    continue;

                if (trainType.IsAddOn && agent.GetAddOn() != null)
                    continue;

                if (!trainType.ProducingUnits.Contains(agent.Unit.UnitType))
                    continue;

                if (agent.Unit.Orders != null && agent.Unit.Orders.Count >= 2)
                    continue;

                if (agent.CurrentAbility() != 0 && (agent.GetAddOn() == null || !IsReactor(agent.GetAddOn().Unit.UnitType)) )
                    continue;

                if (agent.Command != null)
                    continue;

                if (Tyr.Bot.Frame - agent.LastOrderFrame < 5)
                    continue;

                Tyr.Bot.ReservedGas += trainType.Gas;
                Tyr.Bot.ReservedMinerals += trainType.Minerals;

                if (Tyr.Bot.Build.Minerals() < 0)
                    return new NextList();
                if (Tyr.Bot.Build.Gas() < 0)
                    return new NextItem();

                if (agent.Unit.UnitType == UnitTypes.WARP_GATE)
                {
                    bool success = WarpIn(agent, trainType);
                    if (!success)
                        continue;
                }
                else
                    agent.Order((int)trainType.Ability);

                state.AddTraining(UnitType, 1);
                alreadyTrained++;
            }

            return new NextItem();
        }

        private bool WarpIn(Agent warpGate, TrainingType trainType)
        {
            Point2D aroundTile;
            Point2D placement;
            if (Tyr.Bot.BaseManager.Natural.Owner == Tyr.Bot.PlayerId)
            {
                aroundTile = Tyr.Bot.BaseManager.Natural.OppositeMineralLinePos;
                placement = WarpInPlacer.FindPlacement(aroundTile, trainType.UnitType);
                if (placement != null)
                {
                    warpGate.Order((int)trainType.WarpInAbility, placement);
                    return true;
                }
            }

            aroundTile = SC2Util.To2D(Tyr.Bot.MapAnalyzer.StartLocation);
            placement = WarpInPlacer.FindPlacement(aroundTile, trainType.UnitType);
            if (placement != null)
            {
                warpGate.Order((int)trainType.WarpInAbility, placement);
                return true;
            }
            return false;
        }

        private bool IsReactor(uint unitType)
        {
            return unitType == UnitTypes.REACTOR
                || unitType == UnitTypes.BARRACKS_REACTOR
                || unitType == UnitTypes.FACTORY_REACTOR
                || unitType == UnitTypes.STARPORT_REACTOR;
        }

        private bool CheckTypeMatches(uint requiredType, uint foundType)
        {
            if (requiredType == foundType)
                return true;
            return UnitTypes.EquivalentTypes.ContainsKey(foundType) && UnitTypes.EquivalentTypes[foundType].Contains(requiredType);
        }
    }
}
