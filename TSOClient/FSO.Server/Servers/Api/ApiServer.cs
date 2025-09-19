using FSO.Server.Common;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Ninject;
using Nancy.Configuration;
using Nancy.Hosting.Self;
using Ninject;
using NLog;

namespace FSO.Server.Servers.Api
{
    public class ApiServer : AbstractServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private ApiServerConfiguration Config;
        private IKernel Kernel;
        private NancyHost Nancy;

        public event APIRequestShutdownDelegate OnRequestShutdown;
        public event APIBroadcastMessageDelegate OnBroadcastMessage;

        public delegate void APIRequestShutdownDelegate(uint time, ShutdownType type);
        public delegate void APIBroadcastMessageDelegate(string sender, string title, string message);

        public ApiServer(ApiServerConfiguration config, IKernel kernel)
        {
            Config = config;
            Kernel = kernel;

            Kernel.Bind<ApiServer>().ToConstant(this);
            Kernel.Bind<ApiServerConfiguration>().ToConstant(config);
        }

        public override void Start()
        {
            LOG.Info("Starting API server");

            var configuration = new HostConfiguration
            {
                UrlReservations = { CreateAutomatically = true }
            };

            var uris = new List<Uri>();
            foreach (var path in Config.Bindings)
                uris.Add(new Uri(path));

            Nancy = new NancyHost(new CustomNancyBootstrap(Kernel), configuration, uris.ToArray());
            Nancy.Start();
        }

        public override void Shutdown()
        {
            Nancy?.Stop();
        }

        public void RequestShutdown(uint time, ShutdownType type)
        {
            OnRequestShutdown?.Invoke(time, type);
        }

        public void BroadcastMessage(string sender, string title, string message)
        {
            OnBroadcastMessage?.Invoke(sender, title, message);
        }

        public override void AttachDebugger(IServerDebugger debugger) { }
    }

    class CustomNancyBootstrap : NinjectNancyBootstrapper
    {
        private readonly IKernel Kernel;

        public CustomNancyBootstrap(IKernel kernel)
        {
            Kernel = kernel;
        }

        protected override IKernel GetApplicationContainer() => Kernel;

        protected override void ApplicationStartup(IKernel container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            pipelines.AfterRequest.AddItemToEndOfPipeline(x =>
                x.Response.WithHeader("Access-Control-Allow-Origin", "*")
                          .WithHeader("Access-Control-Allow-Methods", "DELETE, GET, HEAD, POST, PUT, OPTIONS, PATCH")
                          .WithHeader("Access-Control-Allow-Headers", "Content-Type, Authorization")
                          .WithHeader("Access-Control-Expose-Headers", "X-Total-Count")
            );
        }

        protected override void RegisterNancyEnvironment(IKernel container, INancyEnvironment environment)
        {
        }

        public override INancyEnvironment GetEnvironment()
        {
            return new DefaultNancyEnvironment();
        }

        protected override INancyEnvironmentConfigurator GetEnvironmentConfigurator()
        {
            var factory = new DefaultNancyEnvironmentFactory();
            var providers = new INancyDefaultConfigurationProvider[0]; // empty array if no custom providers
            return new DefaultNancyEnvironmentConfigurator(factory, providers);
        }
    }
}