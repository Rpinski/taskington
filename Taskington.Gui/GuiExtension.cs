using Taskington.Base.Extension;
using Taskington.Base.Service;
using Taskington.Base.TinyBus;
using Taskington.Gui.Extension;
using Taskington.Gui.UIProviders;
using Taskington.Gui.ViewModels;

[assembly: TaskingtonExtension(typeof(Taskington.Gui.GuiExtension))]

namespace Taskington.Gui
{
    class GuiExtension : ITaskingtonExtension
    {
        //public static void Bind(IAppServiceBinder binder)
        //{
        //    binder
        //        .Bind<MainWindowViewModel>()
        //        .Bind<ModelEventDispatcher>()
        //        .Bind<IStepTypeUI, SyncStepUI>();
        //}

        public void Initialize(IEventBus eventBus, IHandlerStore handlerStore)
        {
            handlerStore.Add(new ModelEventDispatcher(eventBus));
        }
    }
}
