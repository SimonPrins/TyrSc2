using System;
using Tyr.Agents;
using Tyr.Tasks;
using static Tyr.Builds.BuildLists.ConditionalStep;

namespace Tyr.Builds.BuildLists
{
    public class MorphStep : BuildStep
    {
        public uint UnitType;
        public int Number = 1;
        public Test Condition = () => { return true; };

        public MorphStep(uint unitType)
        {
            UnitType = unitType;
        }

        public MorphStep(uint unitType, int number)
        {
            UnitType = unitType;
            Number = number;
        }

        public MorphStep(uint unitType, Test condition)
        {
            UnitType = unitType;
            Condition = condition;
        }

        public MorphStep(uint unitType, int number, Test condition)
        {
            UnitType = unitType;
            Number = number;
            Condition = condition;
        }

        public StepResult Perform(BuildListState state)
        {
            if (!Condition.Invoke())
                return new NextItem();

            if (UnitTypes.LookUp[UnitType].TechRequirement != 0
                && Tyr.Bot.UnitManager.Completed(UnitTypes.LookUp[UnitType].TechRequirement) == 0
                && UnitTypes.LookUp[UnitType].TechRequirement != UnitTypes.HATCHERY)
            {
                Tyr.Bot.DrawText("Skipping list. Morph tech for " + UnitTypes.LookUp[UnitType].Name + " not available: " + UnitTypes.LookUp[UnitType].TechRequirement);
                return new NextList();
            }

            if (Build.FoodLeft() < UnitTypes.LookUp[UnitType].FoodRequired)
                return new NextItem();

            state.AddDesired(UnitType, Number);

            if (state.Desired[UnitType] > Tyr.Bot.UnitManager.Count(UnitType))
                if (!Morph(state, state.Desired[UnitType] - Tyr.Bot.UnitManager.Count(UnitType)))
                    return new WaitForResources();
            return new NextItem();
        }

        private bool Morph(BuildListState state, int number)
        {
            if (!MorphingType.LookUpToType.ContainsKey(UnitType))
                throw new ArgumentException("There is no MorphingType defined for UnitType: " + UnitType);

            if (number <= 0)
                return true;


            if (Tyr.Bot.UnitManager.Count(UnitTypes.LARVA) < 0)
                return true;

            if (Tyr.Bot.Minerals() < MorphingType.LookUpToType[UnitType].Minerals
                || Tyr.Bot.Gas() < MorphingType.LookUpToType[UnitType].Gas)
                return false;

            state.BuiltThisFrame = true;
            Tyr.Bot.DrawText("Morphing: " + UnitTypes.LookUp[UnitType].Name);
            MorphingTask.Task.Morph(UnitType);

            return number == 1;
        }

        public override string ToString()
        {
            return "Morphing " + UnitType + " " + Number;
        }
    }
}
