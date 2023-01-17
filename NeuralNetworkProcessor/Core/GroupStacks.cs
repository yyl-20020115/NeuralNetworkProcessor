using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetworkProcessor.Core
{
    public record Group(Trend Starter,HashSet<Trend> RecursiveMembers);

    public class GroupStacks
    {
        public List<Stack<Group>> Stacks { get; } = new();
        
        public void Enter(Trend Starter, HashSet<Trend> RecursiveMembers)
        {
            var g = new Group(Starter, RecursiveMembers);
             
        }
    }
}
