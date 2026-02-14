#nullable enable
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;

namespace FSO.Common.DependencyInjection
{
    public interface IKernel : IServiceProvider, IDisposable
    {
        T Get<T>() where T : notnull;
        object Get(Type type);
        BindingBuilder<T> Bind<T>();
        IKernel CreateChildKernel(Action<IServiceCollection>? configure = null);
        void Load(Action<IServiceCollection>? configure = null);
    }

    public class BindingBuilder<T>
    {
        private readonly FSOKernel _kernel;
        private Type? _impl;

        internal BindingBuilder(FSOKernel kernel) => _kernel = kernel;

        public BindingBuilder<T> ToSelf() { _impl = typeof(T); return this; }
        public BindingBuilder<T> To<TImpl>() where TImpl : T { _impl = typeof(TImpl); return this; }
        public BindingBuilder<T> ToConstant(T value) { _kernel.Set(typeof(T), value!); return this; }
        public BindingBuilder<T> InSingletonScope() => Register(true);
        public BindingBuilder<T> InTransientScope() => Register(false);
        public BindingBuilder<T> InScopedScope() => Register(true);

        private BindingBuilder<T> Register(bool singleton)
        {
            if (_impl == null) throw new InvalidOperationException($"No implementation type for {typeof(T)}");
            _kernel.Bind(typeof(T), _impl, singleton);
            return this;
        }
    }

    /// <summary>
    /// Thin Ninject-compatible wrapper over Microsoft.Extensions.DependencyInjection.
    /// Resolution: cache → bindings → root provider → auto-create.
    /// </summary>
    public sealed class FSOKernel : IKernel, IKeyedServiceProvider
    {
        private readonly IServiceProvider _root;
        private readonly ConcurrentDictionary<Type, object> _cache = new();
        private readonly ConcurrentDictionary<Type, (Func<FSOKernel, object> create, bool singleton)> _bindings = new();

        public FSOKernel(IServiceProvider root) => _root = root ?? throw new ArgumentNullException(nameof(root));

        internal void Set(Type t, object instance) { _cache[t] = instance; _bindings.TryRemove(t, out _); }

        internal void Bind(Type t, Type impl, bool singleton)
        {
            _bindings[t] = (k => ActivatorUtilities.CreateInstance(k, impl), singleton);
            _cache.TryRemove(t, out _);
        }

        internal void Bind(Type t, Func<IServiceProvider, object> factory, bool singleton)
        {
            _bindings[t] = (k => factory(k), singleton);
            _cache.TryRemove(t, out _);
        }

        public void Load(Action<IServiceCollection>? configure = null)
        {
            if (configure == null) return;
            var temp = new ServiceCollection();
            configure(temp);
            foreach (var d in temp)
            {
                var singleton = d.Lifetime != ServiceLifetime.Transient;
                if (d.ImplementationInstance != null) Set(d.ServiceType, d.ImplementationInstance);
                else if (d.ImplementationType != null) Bind(d.ServiceType, d.ImplementationType, singleton);
                else if (d.ImplementationFactory != null) Bind(d.ServiceType, d.ImplementationFactory, singleton);
            }
        }

        public IKernel CreateChildKernel(Action<IServiceCollection>? configure = null)
        {
            var child = new FSOKernel(_root);
            foreach (var kv in _cache) child._cache.TryAdd(kv.Key, kv.Value);
            foreach (var kv in _bindings) child._bindings.TryAdd(kv.Key, kv.Value);
            child.Load(configure);
            child._cache[typeof(IKernel)] = child;
            return child;
        }

        public BindingBuilder<T> Bind<T>() => new(this);
        public T Get<T>() where T : notnull => (T)GetService(typeof(T))!;
        public object Get(Type type) => GetService(type) ?? throw new InvalidOperationException($"Unable to resolve service for type {type}.");

        public object? GetService(Type serviceType)
        {
            if (_cache.TryGetValue(serviceType, out var cached))
                return cached;

            if (_bindings.TryGetValue(serviceType, out var binding))
                return binding.singleton ? _cache.GetOrAdd(serviceType, _ => binding.create(this)) : binding.create(this);

            var instance = _root.GetService(serviceType);
            if (instance != null) return instance;

            if (serviceType.IsClass && !serviceType.IsAbstract)
                return ActivatorUtilities.CreateInstance(this, serviceType);

            return null;
        }

        public object? GetKeyedService(Type serviceType, object? serviceKey)
            => (_root as IKeyedServiceProvider)?.GetKeyedService(serviceType, serviceKey);

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
            => _root is IKeyedServiceProvider keyed
                ? keyed.GetRequiredKeyedService(serviceType, serviceKey)
                : throw new InvalidOperationException("Keyed services not supported.");

        public void Dispose() { }
    }

    public static class KernelExtensions
    {
        public static T Get<T>(this IServiceProvider sp) where T : notnull => sp.GetRequiredService<T>();
        public static object Get(this IServiceProvider sp, Type type) => sp.GetRequiredService(type);
    }
}
