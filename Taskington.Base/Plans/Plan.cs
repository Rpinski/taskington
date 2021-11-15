using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Taskington.Base.Steps;

namespace Taskington.Base.Plans
{
    public class Plan : Model.Model
    {
        public const string OnSelectionRunType = "selection";

        public Plan(string type) : base()
        {
            RunType = type;
        }

        public Plan(string type, IEnumerable<KeyValuePair<string, string>> initialProperties)
            : base(initialProperties)
        {
            RunType = type;
        }

        public string? Name { get; set; }

        public string RunType { get; set; }

        public IEnumerable<PlanStep> Steps { get; set; } = Enumerable.Empty<PlanStep>();

        public bool IsValid => Steps.OfType<InvalidPlanStep>().Any();

        public string? ValidationMessage => !IsValid ? "Plan contains invalid steps." : null;
    }
}
