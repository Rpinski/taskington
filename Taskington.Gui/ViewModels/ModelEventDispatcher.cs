using System;
using System.Linq;
using Taskington.Base.Plans;

namespace Taskington.Gui.ViewModels
{
    class ModelEventDispatcher
    {
        private readonly MainWindowViewModel mainWindowViewModel;

        public ModelEventDispatcher(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel;

            PlanEvents.PlanCanExecuteUpdated.Subscribe((plan, canExecute) => UpdateViewModel(plan, m => m.CanExecute = canExecute));
            PlanEvents.PlanHasErrorsUpdated.Subscribe((plan, hasErrors, validationText) => UpdateViewModel(plan, m => m.HasErrors = hasErrors));
            PlanEvents.PlanIsRunningUpdated.Subscribe((plan, isRunning) => UpdateViewModel(plan, m => m.IsRunning = isRunning));
            PlanEvents.PlanProgressUpdated.Subscribe((plan, progress) => UpdateViewModel(plan, m => m.Progress = progress));
            PlanEvents.PlanStatusTextUpdated.Subscribe((plan, statusText) => UpdateViewModel(plan, m => m.StatusText = statusText));
        }

        private void UpdateViewModel(Plan plan, Action<PlanViewModel> updater)
        {
            var model = mainWindowViewModel.Plans.FirstOrDefault(model => model.ExecutablePlan.Plan == plan);
            if (model != null)
            {
                updater(model);
            }
        }
    }
}
