using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using NLog;
#if GLOBAL_NADEKO
using NadekoBot.Common;
#endif


namespace NadekoBot.Core.Services
{
    public interface INServiceProvider : IServiceProvider, IEnumerable<object>
    {
        T GetService<T>();
        IEnumerable<Type> LoadFrom(Assembly assembly);
        INServiceProvider AddManual<T>(T obj);
        object Unload(Type t);
    }

    public class NServiceProvider : INServiceProvider
    {
        private readonly object _locker = new object();
        private readonly Logger _log;

        public readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        public IReadOnlyDictionary<Type, object> Services => _services;

        public NServiceProvider()
        {
            _log = LogManager.GetCurrentClassLogger();
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

        public INServiceProvider AddManual<T>(T obj)
        {
            lock (_locker)
            {
                _services.TryAdd(typeof(T), obj);
            }
            return this;
        }

        public INServiceProvider UpdateManual<T>(T obj)
        {
            lock (_locker)
            {
                _services.Remove(typeof(T));
                _services.TryAdd(typeof(T), obj);
            }
            return this;
        }

        public IEnumerable<Type> LoadFrom(Assembly assembly)
        {
            List<Type> addedTypes = new List<Type>();

            Type[] allTypes;
            try
            {
                allTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Console.WriteLine(ex.LoaderExceptions[0]);
                return Enumerable.Empty<Type>();
            }
            var services = new Queue<Type>(allTypes
                    .Where(x => x.GetInterfaces().Contains(typeof(INService))
                        && !x.GetTypeInfo().IsInterface && !x.GetTypeInfo().IsAbstract
#if GLOBAL_NADEKO
                        && x.GetTypeInfo().GetCustomAttribute<NoPublicBot>() == null
#endif
                            )
                    .ToArray());

            addedTypes.AddRange(services);

            var interfaces = new HashSet<Type>(allTypes
                    .Where(x => x.GetInterfaces().Contains(typeof(INService))
                        && x.GetTypeInfo().IsInterface));

            var alreadyFailed = new Dictionary<Type, int>();
            lock (_locker)
            {
                var sw = Stopwatch.StartNew();
                var swInstance = new Stopwatch();
                while (services.Count > 0)
                {
                    var type = services.Dequeue(); //get a type i need to make an instance of

                    if (_services.TryGetValue(type, out _)) // if that type is already instantiated, skip
                        continue;

                    var ctor = type.GetConstructors()[0];
                    var argTypes = ctor
                        .GetParameters()
                        .Select(x => x.ParameterType)
                        .ToArray(); // get constructor argument types i need to pass in

                    var args = new List<object>(argTypes.Length);
                    foreach (var arg in argTypes) //get constructor arguments from the dictionary of already instantiated types
                    {
                        if (_services.TryGetValue(arg, out var argObj)) //if i got current one, add it to the list of instances and move on
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

                    swInstance.Restart();
                    var instance = ctor.Invoke(args.ToArray());
                    swInstance.Stop();
                    if (swInstance.Elapsed.TotalSeconds > 5)
                        _log.Info($"{type.Name} took {swInstance.Elapsed.TotalSeconds:F2}s to load.");
                    var interfaceType = interfaces.FirstOrDefault(x => instance.GetType().GetInterfaces().Contains(x));
                    if (interfaceType != null)
                    {
                        addedTypes.Add(interfaceType);
                        _services.TryAdd(interfaceType, instance);
                    }

                    _services.TryAdd(type, instance);
                }
                sw.Stop();
                _log.Info($"All services loaded in {sw.Elapsed.TotalSeconds:F2}s");
            }
            return addedTypes;
        }

        public object Unload(Type t)
        {
            lock (_locker)
            {
                if (_services.TryGetValue(t, out var obj))
                {
                    _services.Remove(t);
                    return obj;
                }
            }
            return null;
        }

        IEnumerator IEnumerable.GetEnumerator() => _services.Values.GetEnumerator();

        public IEnumerator<object> GetEnumerator() => _services.Values.GetEnumerator();
    }
}