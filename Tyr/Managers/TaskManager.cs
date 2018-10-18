using System.Collections.Generic;
using System.Diagnostics;
using Tyr.Agents;
using Tyr.Tasks;

namespace Tyr.Managers
{
    public class TaskManager : Manager
    {
        private List<Task> tasks = new List<Task>();
        
        public void OnFrame(Tyr tyr)
        {
            foreach(Task task in tasks)
                task.Cleanup(tyr);
            
            List<Task> orderedTasks = tasks.FindAll((task) => { return task.IsNeeded(); } );
            orderedTasks.Sort((a, b) => { return a.Priority.CompareTo(b.Priority); });


            for (int i = orderedTasks.Count - 1; i >- 1 ; i--)
            {
                Task to = orderedTasks[i];
                if (to.Stopped)
                    continue;
                List<UnitDescriptor> unitDescriptors = to.GetDescriptors();

                foreach (UnitDescriptor descriptor in unitDescriptors)
                {
                    int count = 0;
                    for (int j = 0; j < i; j++)
                    {
                        if (descriptor.Count >= 0 && count >= descriptor.Count)
                            break;

                        Task from = orderedTasks[j];
                        if (to.Priority <= from.Priority)
                            break;
                        List<int> fromAgents = new List<int>();
                        int k = 0;
                        foreach (Agent agent in from.Units)
                        {
                            fromAgents.Add(k);
                            k++;
                        }
                        List<float> distances = new List<float>();
                        if (descriptor.Pos != null)
                        {
                            foreach (Agent agent in from.Units)
                                distances.Add(agent.DistanceSq(descriptor.Pos));
                            fromAgents.Sort((a, b) => { return distances[a].CompareTo(distances[b]); });
                        }
                        List<int> doWant = new List<int>();
                        foreach (int a in fromAgents)
                        {
                            Agent agent = from.Units[a];
                            if (descriptor.UnitTypes != null && !descriptor.UnitTypes.Contains(agent.Unit.UnitType))
                                continue;
                            if (descriptor.MaxDist < 1000000 && distances[a] > descriptor.MaxDist * descriptor.MaxDist)
                                break;
                            
                            if (to.DoWant(agent))
                            {
                                doWant.Add(a);
                                count++;
                                if (descriptor.Count >= 0 && count >= descriptor.Count)
                                    break;
                            }
                        }

                        doWant.Sort();
                        for (int w = doWant.Count - 1; w >= 0; w--)
                        {
                            if (descriptor.Marker != null)
                                to.Add(from.PopAt(doWant[w]), descriptor);
                            else
                                to.Add(from.PopAt(doWant[w]));
                        }
                    }
                }
            }
            
            foreach (Task task in tasks)
                task.OnFrame(tyr);
        }

        public void StopAll()
        {
            foreach (Task task in tasks)
                task.Stopped = true;
        }

        public void ClearStopped()
        {
            foreach (Task task in tasks)
                if (task.Stopped)
                    task.Clear();
        }

        public void Add(Task task)
        {
            if (!tasks.Contains(task))
            tasks.Add(task);
        }

        public void NewAgent(Agent agent)
        {
            IdleTask.Task.Add(agent);
        }
    }
}
