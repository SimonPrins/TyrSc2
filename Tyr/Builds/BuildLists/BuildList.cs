using SC2APIProtocol;
using System;
using System.Collections.Generic;
using Tyr.Managers;
using static Tyr.Builds.BuildLists.ConditionalStep;

namespace Tyr.Builds.BuildLists
{
    public class BuildList
    {
        private List<BuildStep> Steps = new List<BuildStep>();

        public static BuildList operator +(BuildList list, BuildStep step)
        {
            list.Steps.Add(step);
            return list;
        }

        public bool Construct()
        {
            BuildListState state = new BuildListState();

            for (int pos = 0; pos < Steps.Count; pos++)
            {
                BuildStep step = null;
                try
                {
                    StepResult result = Steps[pos].Perform(state);
                    if (result is WaitForResources)
                        return false;
                    else if (result is NextList)
                        return true;
                    else if (result is NextItem)
                        continue;
                    else if (result is ToLine)
                    {
                        int line = ((ToLine)result).Line;
                        if (pos < 0)
                            pos += line;
                        else
                            pos = line - 1;
                    }

                }
                catch (Exception e)
                {
                    throw new Exception("Exception in buildlist at step " + pos + ": " + step, e);
                }
            }
            return true;
        }

        #region Step adding methods

        public void Building(uint unitType)
        {
            Steps.Add(new BuildingStep(unitType));
        }

        public void Building(uint unitType, Test condition)
        {
            Steps.Add(new BuildingStep(unitType, condition));
        }

        public void Building(uint unitType, Base desiredBase)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase));
        }

        public void Building(uint unitType, Base desiredBase, Test condition)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, condition));
        }

        public void Building(uint unitType, int number)
        {
            Steps.Add(new BuildingStep(unitType, number));
        }

        public void Building(uint unitType, int number, Test condition)
        {
            Steps.Add(new BuildingStep(unitType, number, condition));
        }

        public void Building(uint unitType, Base desiredBase, int number)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, number));
        }

        public void Building(uint unitType, Base desiredBase, Point2D desiredPos)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, desiredPos));
        }

        public void Building(uint unitType, Base desiredBase, int number, Test condition)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, number, condition));
        }

        public void Building(uint unitType, Base desiredBase, Point2D desiredPos, Test condition)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, desiredPos, condition));
        }

        public void Building(uint unitType, Base desiredBase, Point2D desiredPos, int number)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, desiredPos, number));
        }

        public void Building(uint unitType, Base desiredBase, Point2D desiredPos, int number, Test condition)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, desiredPos, number, condition));
        }

        public void Building(uint unitType, Base desiredBase, Point2D desiredPos, bool exact)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, desiredPos, exact));
        }

        public void Building(uint unitType, Base desiredBase, Point2D desiredPos, bool exact, Test condition)
        {
            Steps.Add(new BuildingStep(unitType, desiredBase, desiredPos, exact, condition));
        }

        public void Goto(int pos)
        {
            Steps.Add(new GotoStep(pos));
        }

        public void If(Test condition)
        {
            Steps.Add(new ConditionalStep(condition));
        }

        public void Morph(uint unitType)
        {
            Steps.Add(new MorphStep(unitType));
        }

        public void Morph(uint unitType, int number)
        {
            Steps.Add(new MorphStep(unitType, number));
        }

        public void Morph(uint unitType, Test condition)
        {
            Steps.Add(new MorphStep(unitType, condition));
        }

        public void Morph(uint unitType, int number, Test condition)
        {
            Steps.Add(new MorphStep(unitType, number, condition));
        }

        public void Train(uint unitType)
        {
            Steps.Add(new TrainStep(unitType));
        }

        public void Train(uint unitType, int number)
        {
            Steps.Add(new TrainStep(unitType, number));
        }

        public void Train(uint unitType, Test test)
        {
            Steps.Add(new TrainStep(unitType, test));
        }

        public void Train(uint unitType, int number, Test test)
        {
            Steps.Add(new TrainStep(unitType, number, test));
        }

        public void Upgrade(uint upgradeId)
        {
            Steps.Add(new UpgradeStep(upgradeId));
        }

        public void Upgrade(uint upgradeId, int number)
        {
            Steps.Add(new UpgradeStep(upgradeId, number));
        }

        public void Upgrade(uint upgradeId, Test test)
        {
            Steps.Add(new UpgradeStep(upgradeId, test));
        }

        public void Upgrade(uint upgradeId, int number, Test test)
        {
            Steps.Add(new UpgradeStep(upgradeId, number, test));
        }
        #endregion
    }
}
