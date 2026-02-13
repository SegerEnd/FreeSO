using Microsoft.Extensions.DependencyInjection;

namespace FSO.Common.DependencyInjection
{
    public interface IKernel : IServiceProvider
    {
        T Get<T>() where T : notnull;
        object Get(Type type);
        BindingBuilder<T> Bind<T>();
        IKernel CreateChildKernel(Action<IServiceCollection> configure);
        void Load(Action<IServiceCollection> configure);
    }

    public class BindingBuilder<T>
    {
        private readonly FSOKernel _kernel;
        private Type _implType;

        internal BindingBuilder(FSOKernel kernel) => _kernel = kernel;

        public BindingBuilder<T> ToSelf() { _implType = typeof(T); return this; }
        public BindingBuilder<T> To<TImpl>() where TImpl : T { _implType = typeof(TImpl); return this; }
        public BindingBuilder<T> ToConstant(T value) { _kernel.Register(typeof(T), value); return this; }
        public BindingBuilder<T> Named(string name) => this;

        public BindingBuilder<T> InSingletonScope()
        {
            var t = _implType;
            _kernel.Register(typeof(T), sp => ActivatorUtilities.CreateInstance(sp, t));
            return this;
        }
    }

    public sealed class FSOKernel : IKernel, IKeyedServiceProvider
    {
        private readonly IServiceProvider _root;
        private readonly Dictionary<Type, object> _singletons = new();
        private readonly Dictionary<Type, Func<IServiceProvider, object>> _factories = new();

        public FSOKernel(IServiceProvider root) => _root = root;

        internal void Register(Type type, object instance) => _singletons[type] = instance;
        internal void Register(Type type, Func<IServiceProvider, object> factory) => _factories[type] = factory;

        public BindingBuilder<T> Bind<T>() => new(this);

        public void Load(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();
            configure(services);
            foreach (var d in services)
            {
                if (d.ImplementationInstance != null)
                    _singletons[d.ServiceType] = d.ImplementationInstance;
                else if (d.ImplementationType != null)
                {
                    var t = d.ImplementationType;
                    _factories[d.ServiceType] = sp => ActivatorUtilities.CreateInstance(sp, t);
                }
                else if (d.ImplementationFactory != null)
                    _factories[d.ServiceType] = d.ImplementationFactory;
            }
        }

        public IKernel CreateChildKernel(Action<IServiceCollection> configure)
        {
            var container = _root.GetRequiredService<ServiceContainer>();
            var child = (FSOKernel)container.CreateChild(_ => { }).Kernel;

            foreach (var kv in _singletons) child._singletons[kv.Key] = kv.Value;
            foreach (var kv in _factories) child._factories[kv.Key] = kv.Value;
            child.Load(configure);
            child._singletons[typeof(IKernel)] = child;
            return child;
        }

        public T Get<T>() where T : notnull => (T)GetService(typeof(T))!;

        public object Get(Type type)
            => GetService(type) ?? throw new InvalidOperationException($"Unable to resolve {type}.");

        public object? GetService(Type type)
        {
            if (_singletons.TryGetValue(type, out var instance))
                return instance;
            if (_factories.Remove(type, out var factory))
                return _singletons[type] = factory(this);

            return _root.GetService(type) ?? AutoCreate(type);
        }

        public object? GetKeyedService(Type type, object? key)
            => (_root as IKeyedServiceProvider)?.GetKeyedService(type, key);

        public object GetRequiredKeyedService(Type type, object? key)
            => _root is IKeyedServiceProvider ksp
                ? ksp.GetRequiredKeyedService(type, key)
                : throw new InvalidOperationException("Keyed services not supported.");

        // Matches Ninject's implicit self-binding: auto-create unregistered concrete types.
        private object? AutoCreate(Type type)
            => type.IsClass && !type.IsAbstract ? ActivatorUtilities.CreateInstance(this, type) : null;
    }

    public static class KernelExtensions
    {
        public static T Get<T>(this IServiceProvider sp) where T : notnull
            => sp.GetRequiredService<T>();

        public static object Get(this IServiceProvider sp, Type type)
            => sp.GetRequiredService(type);
    }
}
