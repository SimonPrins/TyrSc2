using SC2APIProtocol;
using Tyr.Agents;
using Tyr.Managers;
using Tyr.MapAnalysis;
using Tyr.Tasks;
using Tyr.Util;
using static Tyr.Builds.BuildLists.ConditionalStep;

namespace Tyr.Builds.BuildLists
{
    public class BuildingStep : BuildStep
    {
        public uint UnitType;
        public Base DesiredBase;
        public Point2D DesiredPos;
        public bool Exact;
        public int Number = 1;
        public Test Condition = () => { return true; };

        public BuildingStep(uint unitType)
        {
            UnitType = unitType;
        }

        public BuildingStep(uint unitType, Base desiredBase)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, bool exact)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Exact = exact;
        }

        public BuildingStep(uint unitType, int number)
        {
            UnitType = unitType;
            Number = number;
        }

        public BuildingStep(uint unitType, Base desiredBase, int number)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            Number = number;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, int number)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Number = number;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, bool exact, int number)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Exact = exact;
            Number = number;
        }

        public BuildingStep(uint unitType, Test condition)
        {
            UnitType = unitType;
            Condition = condition;
        }

        public BuildingStep(uint unitType, Base desiredBase, Test condition)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            Condition = condition;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, Test condition)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Condition = condition;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, bool exact, Test condition)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Exact = exact;
            Condition = condition;
        }

        public BuildingStep(uint unitType, int number, Test condition)
        {
            UnitType = unitType;
            Number = number;
            Condition = condition;
        }

        public BuildingStep(uint unitType, Base desiredBase, int number, Test condition)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            Number = number;
            Condition = condition;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, int number, Test condition)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Number = number;
            Condition = condition;
        }

        public BuildingStep(uint unitType, Base desiredBase, Point2D desiredPos, bool exact, int number, Test condition)
        {
            UnitType = unitType;
            DesiredBase = desiredBase;
            DesiredPos = desiredPos;
            Exact = exact;
            Number = number;
            Condition = condition;
        }

        public override string ToString()
        {
            if (!BuildingType.LookUp.ContainsKey(UnitType))
                throw new System.Exception("Unable to find building with type: " + UnitType  + ". Please add it to the BuildingType class.");
            return "Building " + BuildingType.LookUp[UnitType].Name + " " + Number + (Exact ? " exact" : "");
        }

        public StepResult Perform(BuildListState state)
        {
            if (!Condition.Invoke())
                return new NextItem();

            if (UnitTypes.LookUp[UnitType].TechRequirement != 0
                && Bot.Bot.UnitManager.Completed(UnitTypes.LookUp[UnitType].TechRequirement) == 0
                && UnitTypes.LookUp[UnitType].TechRequirement != UnitTypes.HATCHERY
                && UnitType != UnitTypes.GATEWAY)
            {

                bool almostReady = false;
                if (UnitType == UnitTypes.CYBERNETICS_CORE)
                {
                    foreach (Agent agent in Bot.Bot.Units())
                    {
                        if (agent.Unit.UnitType == UnitTypes.GATEWAY
                            && agent.Unit.BuildProgress >= 0.8)
                            almostReady = true;
                    }
                }
                if (!almostReady)
                {
                    Bot.Bot.DrawText("Skipping list. Build tech for " + UnitTypes.LookUp[UnitType].Name + " not available: " + UnitTypes.LookUp[UnitType].TechRequirement);
                    return new NextList();
                }
            }

            state.AddDesired(UnitType, Number);
            if (DesiredPos != null)
            {
                bool built = false;
                foreach (BuildRequest request in ConstructionTask.Task.BuildRequests)
                    if (request.Type == UnitType && request.Exact == Exact && request.AroundLocation == DesiredPos)
                    {
                        built = true;
                        break;
                    }
                foreach (BuildRequest request in ConstructionTask.Task.UnassignedRequests)
                    if (request.Type == UnitType && request.Exact == Exact && request.AroundLocation == DesiredPos)
                    {
                        built = true;
                        break;
                    }
                if (!built)
                    foreach (Agent agent in Bot.Bot.UnitManager.Agents.Values)
                        if (CheckTypeMatches(UnitType, agent.Unit.UnitType) && agent.Exact == Exact && agent.AroundLocation == DesiredPos)
                        {
                            built = true;
                            break;
                        }
                if (!built)
                {
                    if (!Construct(state, 1))
                        return new WaitForResources();
                }
            }

            if (DesiredBase == null && state.Desired[UnitType] > Bot.Bot.UnitManager.Count(UnitType)
                && !Exact)
                if (!Construct(state, state.Desired[UnitType] - Bot.Bot.UnitManager.Count(UnitType)))
                    return new WaitForResources();
            if (DesiredBase != null)
            {
                BuildingAtBase key = new BuildingAtBase(UnitType, DesiredBase);
                state.AddDesiredPerBase(key, Number);

                if (state.DesiredPerBase[key] > Build.Count(DesiredBase, UnitType)
                    && !Exact)
                {
                    if (!Construct(state, state.DesiredPerBase[key] - Build.Count(DesiredBase, UnitType)))
                        return new WaitForResources();
                }
            }

            return new NextItem();
        }

        private bool CheckTypeMatches(uint requiredType, uint foundType)
        {
            if (requiredType == foundType)
                return true;
            return UnitTypes.EquivalentTypes.ContainsKey(foundType) && UnitTypes.EquivalentTypes[foundType].Contains(requiredType);
        }

        private bool Construct(BuildListState state, int number)
        {
            for (int i = 0; i < number; i++)
            {
                bool isCenter = UnitTypes.ResourceCenters.Contains(UnitType);

                int requiredMinerals;
                if (isCenter)
                    requiredMinerals = BuildingType.LookUp[UnitType].Minerals - (UnitType == UnitTypes.HATCHERY ? 75 : 90);
                else if (DesiredBase != null
                    && DesiredBase == Bot.Bot.BaseManager.Natural
                    && (Bot.Bot.BaseManager.Natural.ResourceCenter == null || Bot.Bot.BaseManager.Natural.ResourceCenter.Unit.BuildProgress >= 0.99))
                    requiredMinerals = BuildingType.LookUp[UnitType].Minerals - 50;
                else if (UnitType == UnitTypes.PYLON && Bot.Bot.UnitManager.Completed(UnitTypes.PYLON) == 0)
                    requiredMinerals = BuildingType.LookUp[UnitType].Minerals - 45;
                else if (UnitType == UnitTypes.GATEWAY && Bot.Bot.UnitManager.Completed(UnitTypes.GATEWAY) == 0)
                    requiredMinerals = BuildingType.LookUp[UnitType].Minerals - 70;
                else
                    requiredMinerals = BuildingType.LookUp[UnitType].Minerals - 25;
                int requiredGas = BuildingType.LookUp[UnitType].Gas - 16;
                if (Bot.Bot.Minerals() < requiredMinerals
                    || (Bot.Bot.Gas() < requiredGas && BuildingType.LookUp[UnitType].Gas > 0))
                {
                    Bot.Bot.DrawText("Not enough resources for " + UnitTypes.LookUp[UnitType].Name + ".");
                    return false;
                }

                if (UnitTypes.GasGeysers.Contains(UnitType))
                {
                    if (Bot.Bot.BaseManager.AvailableGasses == 0)
                    {
                        Bot.Bot.DrawText("No gasses available.");
                        return true;
                    }
                    if (DesiredBase != null)
                    {
                        bool available = false;
                        foreach (Gas gas in DesiredBase.BaseLocation.Gasses)
                            if (gas.Available)
                            {
                                available = true;
                                break;
                            }
                        if (!available)
                            return true;
                    }
                }

                if (DesiredPos == null && DesiredBase == null)
                {
                    state.BuiltThisFrame = true;
                    if (!Build.Construct(UnitType))
                    {
                        if (isCenter
                            && Bot.Bot.UnitManager.Count(UnitTypes.COMMAND_CENTER) + Bot.Bot.UnitManager.Count(UnitTypes.NEXUS) + Bot.Bot.UnitManager.Count(UnitTypes.HATCHERY) > Bot.Bot.UnitManager.Completed(UnitTypes.COMMAND_CENTER) + Bot.Bot.UnitManager.Completed(UnitTypes.NEXUS) + Bot.Bot.UnitManager.Completed(UnitTypes.HATCHERY))
                            return true;
                        return !UnitTypes.DefensiveBuildingsTypes.Contains(UnitType);
                    }
                }
                else if (DesiredPos == null)
                {
                    state.BuiltThisFrame = true;
                    if (!Build.Construct(UnitType, DesiredBase))
                        return !UnitTypes.DefensiveBuildingsTypes.Contains(UnitType);
                }
                else
                {
                    state.BuiltThisFrame = true;
                    if (!Build.Construct(UnitType, DesiredBase, DesiredPos, Exact))
                        return !UnitTypes.DefensiveBuildingsTypes.Contains(UnitType);
                }
            }
            return true;
        }
    }
}
