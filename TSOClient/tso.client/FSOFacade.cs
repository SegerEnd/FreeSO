using FSO.Client.Network;
using FSO.Client.UI.Hints;
using FSO.Client.UI.Panels;
using FSO.Common.DependencyInjection;

namespace FSO.Client
{
    public class FSOFacade
    {
        public static ServiceContainer Container;
        public static IKernel Kernel => Container?.Kernel;
        public static GameController Controller;
        public static UIMessageController MessageController = new UIMessageController();
        public static NetworkStatus NetStatus = new NetworkStatus();

        public static UIHintManager Hints;
    }
}
