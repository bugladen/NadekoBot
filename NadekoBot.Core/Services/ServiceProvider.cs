using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using NLog;
#if GLOBAL_NADEKO
using NadekoBot.Common;
#endif


namespace NadekoBot.Services
{
    public interface INServiceProvider : IServiceProvider, IEnumerable<object>
    {
        T GetService<T>();
    }

    public class NServiceProvider : INServiceProvider
    {
        public class ServiceProviderBuilder
        {
            private ConcurrentDictionary<Type, object> _dict = new ConcurrentDictionary<Type, object>();
            private readonly Logger _log;

            public ServiceProviderBuilder()
            {
                _log = LogManager.GetCurrentClassLogger();
            }

            public ServiceProviderBuilder AddManual<T>(T obj)
            {
                _dict.TryAdd(typeof(T), obj);
                return this;
            }

            public NServiceProvider Build()
            {
                return new NServiceProvider(_dict);
            }

            public ServiceProviderBuilder LoadFrom(Assembly assembly)
            {
                var allTypes = assembly.GetTypes();
                var services = new Queue<Type>(allTypes
                        .Where(x => x.GetInterfaces().Contains(typeof(INService)) 
                            && !x.GetTypeInfo().IsInterface && !x.GetTypeInfo().IsAbstract

#if GLOBAL_NADEKO
                            && x.GetTypeInfo().GetCustomAttribute<NoPublicBot>() == null
#endif
                            )
                        .ToArray());

                var interfaces = new HashSet<Type>(allTypes
                        .Where(x => x.GetInterfaces().Contains(typeof(INService)) 
                            && x.GetTypeInfo().IsInterface));

                var alreadyFailed = new Dictionary<Type, int>();

                var sw = Stopwatch.StartNew();
                var swInstance = new Stopwatch();
                while (services.Count > 0)
                {
                    var type = services.Dequeue(); //get a type i need to make an instance of

                    if (_dict.TryGetValue(type, out _)) // if that type is already instantiated, skip
                        continue;

                    var ctor = type.GetConstructors()[0];
                    var argTypes = ctor
                        .GetParameters()
                        .Select(x => x.ParameterType)
                        .ToArray(); // get constructor argument types i need to pass in

                    var args = new List<object>(argTypes.Length);
                    foreach (var arg in argTypes) //get constructor arguments from the dictionary of already instantiated types
                    {
                        if (_dict.TryGetValue(arg, out var argObj)) //if i got current one, add it to the list of instances and move on
                            args.Add(argObj);
                        else //if i failed getting it, add it to the end, and break
                        {
                            services.Enqueue(type);
                            if (alreadyFailed.ContainsKey(type))
                            {
                                alreadyFailed[type]++;
                                if (alreadyFailed[type] > 3)
                                    _log.Warn(type.Name + " wasn't instantiated in the first 3 attempts. Missing " + arg.Name + " type");
                            }
                            else
                                alreadyFailed.Add(type, 1);
                            break;
                        }
                    }
                    if (args.Count != argTypes.Length)
                        continue;
                    // _log.Info("Loading " + type.Name);
                    swInstance.Restart();
                    var instance = ctor.Invoke(args.ToArray());
                    swInstance.Stop();
                    if (swInstance.Elapsed.TotalSeconds > 5)
                        _log.Info($"{type.Name} took {swInstance.Elapsed.TotalSeconds:F2}s to load.");
                    var interfaceType = interfaces.FirstOrDefault(x => instance.GetType().GetInterfaces().Contains(x));
                    if (interfaceType != null)
                        _dict.TryAdd(interfaceType, instance);

                    _dict.TryAdd(type, instance);
                }
                sw.Stop();
                _log.Info($"All services loaded in {sw.Elapsed.TotalSeconds:F2}s");

                return this;
            }
        }

        private readonly ImmutableDictionary<Type, object> _services;

        private NServiceProvider() { }
        public NServiceProvider(IDictionary<Type, object> services)
        {
            this._services = services.ToImmutableDictionary();
        }

        public T GetService<T>()
        {
            return (T)((IServiceProvider)(this)).GetService(typeof(T));
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            _services.TryGetValue(serviceType, out var toReturn);
            return toReturn;
        }

        IEnumerator IEnumerable.GetEnumerator() => _services.Values.GetEnumerator();

        public IEnumerator<object> GetEnumerator() => _services.Values.GetEnumerator();
    }
}
