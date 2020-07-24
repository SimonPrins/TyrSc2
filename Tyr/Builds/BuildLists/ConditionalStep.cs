
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

        public StepResult Perform(BuildListState state)
        {
            if (Check())
                return new NextItem();
            else
            {
                Bot.Main.DrawText("Skipping list. Condition not met.");
                return new NextList();
            }
        }
    }
}
