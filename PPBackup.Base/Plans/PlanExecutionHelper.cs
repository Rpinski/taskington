﻿using PPBackup.Base.Service;
using PPBackup.Base.Steps;
using PPBackup.Base.SystemOperations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PPBackup.Base.Plans
{
    public class PlanExecutionHelper
    {
        private readonly IAppServiceProvider serviceProvider;
        private readonly ISystemOperations systemOperations;

        public PlanExecutionHelper(IAppServiceProvider serviceProvider, ISystemOperations systemOperations)
        {
            this.serviceProvider = serviceProvider;
            this.systemOperations = systemOperations;
        }

        public bool CanExecute(BackupPlan plan)
        {
            var placeholders = new Placeholders();
            systemOperations.LoadSystemPlaceholders(placeholders);

            var stepTypes = new HashSet<string>(plan.Steps.Select(step => step.StepType));
            return
                !stepTypes.Select(type => serviceProvider.Get<IStepExecution>(s => s.Type == type)
                    ?.CanExecuteSupportedSteps(plan.Steps, placeholders))
                .Any(result => !(result ?? true));
        }

        public async Task ExecuteAsync(BackupPlan plan, PlanExecutionEvents events)
        {
            await Task.Run(() =>
            {
                try
                {
                    events.OnIsRunning(true);

                    var placeholders = new Placeholders();
                    systemOperations.LoadSystemPlaceholders(placeholders);

                    int stepsFinished = 0;
                    events.OnProgress(0);
                    int planProgress = 0;
                    foreach (var step in plan.Steps)
                    {
                        var stepExecution = serviceProvider.Get<IStepExecution>(s => s.Type == step.StepType);
                        if (stepExecution != null)
                        {
                            var stepEvents = new StepExecutionEvents();
                            stepEvents.ProgressUpdated += (o, e) => events.OnProgress(planProgress + e.Progress / plan.Steps.Count());
                            stepEvents.StatusTextUpdated += (o, e) => events.OnStatusText(e.StatusText);
                            stepExecution.Execute(step, placeholders, stepEvents);
                            stepsFinished++;
                            planProgress = stepsFinished * 100 / plan.Steps.Count();
                            events.OnProgress(planProgress);
                        }
                        else
                        {
                            events.OnHasErrors(true, $"Unknown execution step '{step.StepType}'");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    events.OnHasErrors(true, ex.Message);
                }
                finally
                {
                    events.OnIsRunning(false);
                    events.OnStatusText("Finished successfully");
                }
            });
        }
    }
}
