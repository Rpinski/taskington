using Avalonia.Threading;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Taskington.Base.Config;
using Taskington.Base.Plans;
using Taskington.Gui.Extension;

namespace Taskington.Gui.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<PlanViewModel> Plans { get; }
        public ObservableCollection<AppNotification> AppNotifications { get; }

        public ReactiveCommand<Unit, Unit> AddPlanCommand { get; }
        public ReactiveCommand<PlanViewModel, Unit> ExecutePlanCommand { get; }
        public Interaction<EditPlanViewModel, bool> ShowPlanEditDialog { get; }
        public ReactiveCommand<PlanViewModel, Unit> EditPlanCommand { get; }
        public ReactiveCommand<PlanViewModel, Unit> RemovePlanCommand { get; }
        public ReactiveCommand<PlanViewModel, Unit> UndoPlanRemovalCommand { get; }

        public MainWindowViewModel()
        {
            AppNotifications = new ObservableCollection<AppNotification>();

            Plans = new ObservableCollection<PlanViewModel>();
            ConfigurationEvents.ConfigurationReloaded.Subscribe(UpdatePlanViewModels);
            UpdatePlanViewModels();

            AddPlanCommand = ReactiveCommand.CreateFromTask(AddPlan);
            ExecutePlanCommand = ReactiveCommand.CreateFromTask<PlanViewModel>(ExecutePlan);
            ShowPlanEditDialog = new();
            EditPlanCommand = ReactiveCommand.CreateFromTask<PlanViewModel>(EditPlan);
            RemovePlanCommand = ReactiveCommand.Create<PlanViewModel>(RemovePlan);
            UndoPlanRemovalCommand = ReactiveCommand.Create<PlanViewModel>(UndoPlanRemoval);

            AppNotifications.Add(new AppNotification()
            {
                NotificationType = AppNotificationType.AppInfo,
                LeftText = AppInfo.Copyright,
                RightText = $"v{AppInfo.Version}"
            });
        }

        private void UpdatePlanViewModels()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var removedPlanModels = Plans.Where(plan => plan.IsRemoved);

                Plans.Clear();
                var plans = ConfigurationEvents.GetPlans.Request();
                foreach (var plan in plans)
                {
                    Plans.Add(CreatePlanViewModel(plan));
                }
                foreach (var plan in removedPlanModels)
                {
                    Plans.Insert(plan.PreviousIndex, plan);
                }
                application.NotifyInitialStates();
                PlanEvents.NotifyInitialPlanStates.Push();
            });
        }

        private PlanViewModel CreatePlanViewModel(ExecutablePlan executablePlan) =>
            new(executablePlan, ExecutePlanCommand, EditPlanCommand, RemovePlanCommand, UndoPlanRemovalCommand);

        private async Task ExecutePlan(PlanViewModel planViewModel)
        {
            await planViewModel.Execution.Execute();
        }

        private async Task AddPlan()
        {
            var newPlan = new Plan(Plan.OnSelectionRunType) { Name = "New plan" };
            ConfigurationEvents.InsertPlan.Push(Plans.Count, newPlan);
            ConfigurationEvents.SaveConfiguration.Push();
            var newPlanViewModel = CreatePlanViewModel(newExecutablePlan);
            Plans.Add(newPlanViewModel);
            PlanEvents.NotifyInitialPlanStates.Push(newPlan);
            await EditPlan(newPlanViewModel);
        }

        private async Task EditPlan(PlanViewModel planViewModel)
        {
            var editPlanViewModel = new EditPlanViewModel(application.ServiceProvider, planViewModel);
            var shouldSave = await ShowPlanEditDialog.Handle(editPlanViewModel);
            if (shouldSave)
            {
                ConfigurationEvents.ReplacePlan.Push(planViewModel.ExecutablePlan, editPlanViewModel.ConvertToPlan());
                int existingIndex = Plans.IndexOf(planViewModel);
                if (existingIndex >= 0)
                {
                    Plans[existingIndex] = new PlanViewModel(newExecutablePlan, ExecutePlanCommand, EditPlanCommand, RemovePlanCommand, UndoPlanRemovalCommand);
                    newExecutablePlan.Execution.NotifyInitialStates();
                }
                ConfigurationEvents.SaveConfiguration.Push();
            }
        }

        private void RemovePlan(PlanViewModel planViewModel)
        {
            planViewModel.IsRemoved = true;
            planViewModel.PreviousIndex = Plans.IndexOf(planViewModel);
            ConfigurationEvents.RemovePlan.Push(planViewModel.ExecutablePlan);
            ConfigurationEvents.SaveConfiguration.Push();
        }

        private void UndoPlanRemoval(PlanViewModel planViewModel)
        {
            Plans.Remove(planViewModel);
            ConfigurationEvents.InsertPlan.Push(planViewModel.PreviousIndex, planViewModel.ExecutablePlan.Plan);
            Plans.Insert(
                planViewModel.PreviousIndex,
                new PlanViewModel(newExecutablePlan, ExecutePlanCommand, EditPlanCommand, RemovePlanCommand, UndoPlanRemovalCommand));
            newExecutablePlan.Execution.NotifyInitialStates();
            ConfigurationEvents.SaveConfiguration.Push();
        }
    }
}
