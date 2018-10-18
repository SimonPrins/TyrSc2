
namespace Tyr.Builds.BuildLists
{
    public class ConditionalStep : BuildStep
    {
        public Test Condition;
        public ConditionalStep(Test condition)
        {
            Condition = condition;
        }

        public delegate bool Test();

        public bool Check()
        {
            return Condition.Invoke();
        }

        public override string ToString()
        {
            return "Conditional";
        }
    }
}
