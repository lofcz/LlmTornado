using System;
using System.Linq;

namespace BlazorWorker.Core
{
    internal class DependencyHintAttribute : Attribute
    {
        public DependencyHintAttribute(Type dependsOn, params Type[] dependsOnList)
        {
            DependsOn = new[] { dependsOn }.Concat(dependsOnList).ToArray();
        }

        public Type[] DependsOn { get; }
    }
}