using FSO.Client.UI.Framework;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace FSO.Client.Utils
{
    public static class ControllerUtils
    {
        public static T BindController<T>(UIElement target)
        {
            var needsTarget = typeof(T).GetConstructors()
                .Any(c => c.GetParameters().Any(p => p.ParameterType.IsAssignableFrom(target.GetType())));
            var controllerInstance = needsTarget
                ? ActivatorUtilities.CreateInstance<T>(FSOFacade.Kernel, target)
                : ActivatorUtilities.CreateInstance<T>(FSOFacade.Kernel);
            target.Controller = controllerInstance;
            return controllerInstance;
        }
    }
}
