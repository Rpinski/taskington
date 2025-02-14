using System;
using System.Collections.Generic;
using System.Linq;
using Taskington.Base.Events;
using Taskington.Base.Plans;
using Taskington.Base.Service;
using Taskington.Base.Steps;

namespace Taskington.Base.Config
{
    public class ConfigurationManager : IAutoInitializable
    {
        private static readonly object configurationLock = new();

        private bool isInitialized = false;
        private bool reloadDelayed = false;
        private readonly HashSet<Plan> runningPlans = new();

        private readonly IAppServiceProvider serviceProvider;
        private readonly ApplicationEvents applicationEvents;
        private readonly YamlConfigurationReader configurationReader;
        private readonly YamlConfigurationWriter configurationWriter;

        public ConfigurationManager(
            IAppServiceProvider serviceProvider,
            ApplicationEvents applicationEvents,
            YamlConfigurationReader configurationReader,
            YamlConfigurationWriter configurationWriter)
        {
            this.serviceProvider = serviceProvider;
            this.applicationEvents = applicationEvents;
            this.configurationReader = configurationReader;
            this.configurationWriter = configurationWriter;

            applicationEvents.ConfigurationChanged += OnConfigurationChanged;
            applicationEvents.PlanIsRunningUpdated += OnPlanIsRunningUpdated;
        }

        private readonly List<ExecutablePlan> executablePlans = new();
        public IEnumerable<ExecutablePlan> ExecutablePlans => executablePlans;

        private readonly Dictionary<string, string?> configValues = new();

        public void Initialize()
        {
            if (!isInitialized)
            {
                lock (configurationLock)
                {
                    ReadConfiguration();
                }
                isInitialized = true;
            }
        }

        private void OnConfigurationChanged(object? sender, EventArgs e)
        {
            bool configReloaded = false;
            lock (configurationLock)
            {
                configReloaded = TryReloadConfiguration();
            }

            if (configReloaded)
            {
                applicationEvents.OnConfigurationReloaded();
            }
        }

        private bool TryReloadConfiguration()
        {
            if (runningPlans.Count == 0)
            {
                foreach (var executablePlan in executablePlans)
                {
                    (executablePlan.Execution as IDisposable)?.Dispose();
                }
                executablePlans.Clear();

                ReadConfiguration();
                return true;
            }
            else
            {
                if (!reloadDelayed)
                {
                    applicationEvents.OnConfigurationReloadDelayed(true);
                }
                reloadDelayed = true;
                return false;
            }
        }

        private void ReadConfiguration()
        {
            var configuration = configurationReader.Read();
            foreach (var (key, value) in configuration.ConfigValues)
            {
                configValues.Add(key, value);
            }
            executablePlans.AddRange(configuration.Plans.Select(CreateExecutablePlan));
        }

        private ExecutablePlan CreateExecutablePlan(Plan plan)
        {
            if (plan.Steps.OfType<InvalidPlanStep>().Any())
            {
                return new ExecutablePlan(
                    plan,
                    new InvalidPlanExecution(plan, applicationEvents, "Plan contains invalid steps."));
            }
            else
            {
                var planExecutionCreator = serviceProvider.Get<IPlanExecutionCreator>(
                    execution => execution.RunType == plan.RunType);
                if (planExecutionCreator == null)
                {
                    return new ExecutablePlan(
                        plan,
                        new InvalidPlanExecution(plan, applicationEvents, $"Unknown plan run type '{plan.RunType}'"));
                }
                else
                {
                    return new ExecutablePlan(
                         plan,
                         planExecutionCreator.Create(plan));
                }
            }
        }

        private void OnPlanIsRunningUpdated(object? sender, PlanIsRunningUpdatedEventArgs e)
        {
            if (e.IsRunning)
            {
                lock (configurationLock)
                {
                    runningPlans.Add(e.Plan);
                }
            }
            else
            {
                bool configReloaded = false;
                lock (configurationLock)
                {
                    runningPlans.Remove(e.Plan);
                    if (runningPlans.Count == 0 && reloadDelayed)
                    {
                        applicationEvents.OnConfigurationReloadDelayed(false);
                        reloadDelayed = false;
                        configReloaded = TryReloadConfiguration();
                    }
                }
                if (configReloaded)
                {
                    applicationEvents.OnConfigurationReloaded();
                }
            }
        }

        public void SaveConfiguration()
        {
            configurationWriter.Write(new Configuration(
                configValues.Select(entry => (entry.Key, entry.Value)),
                executablePlans.Select(ep => ep.Plan)));
        }

        public ExecutablePlan InsertPlan(int index, Plan newPlan)
        {
            var newExecutablePlan = CreateExecutablePlan(newPlan);
            if (index > -1)
            {
                executablePlans.Insert(index, newExecutablePlan);
            }
            else
            {
                executablePlans.Add(newExecutablePlan);
            }

            return newExecutablePlan;
        }

        public void RemovePlan(ExecutablePlan executablePlan)
        {
            int index = executablePlans.IndexOf(executablePlan);
            if (index > -1)
            {
                (executablePlan.Execution as IDisposable)?.Dispose();
                executablePlans.RemoveAt(index);
            }
        }

        public ExecutablePlan ReplacePlan(ExecutablePlan executablePlan, Plan newPlan)
        {
            var newExecutablePlan = CreateExecutablePlan(newPlan);
            int index = executablePlans.IndexOf(executablePlan);
            if (index > -1)
            {
                (executablePlan.Execution as IDisposable)?.Dispose();
                executablePlans.RemoveAt(index);
                executablePlans.Insert(index, newExecutablePlan);
            }
            else
            {
                executablePlans.Add(newExecutablePlan);
            }

            return newExecutablePlan;
        }

        public void SetValue(string key, string value)
        {
            configValues[key] = value;
        }

        public string? GetValue(string key)
        {
            if (configValues.TryGetValue(key, out string? configValue))
            {
                return configValue;
            }

            return null;
        }
    }
}
