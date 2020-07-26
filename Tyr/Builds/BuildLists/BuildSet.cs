using System.Collections.Generic;

namespace SC2Sharp.Builds.BuildLists
{
    public class BuildSet
    {
        public List<BuildList> BuildLists = new List<BuildList>();
        
        public static BuildSet operator +(BuildSet set, BuildList list)
        {
            set.BuildLists.Add(list);
            return set;
        }

        public void OnFrame()
        {
            int i = 0;
            foreach (BuildList list in BuildLists)
            {
                if (!list.Construct())
                {
                    Bot.Main.DrawText("Final list: " + i);
                    break;
                }
                i++;
                if (i == BuildLists.Count)
                    Bot.Main.DrawText("Final list: " + (i - 1));
            }
        }
    }
}
