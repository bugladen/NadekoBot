using CommandLine;
using CommandLine.Text;
using CSharpx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace NadekoBot.Core._Extensions
{
    public sealed class OptionSpecification : Specification
    {
        private readonly string shortName;
        private readonly string longName;
        private readonly char separator;
        private readonly string setName;

        public OptionSpecification(string shortName, string longName, bool required, string setName, Maybe<int> min, Maybe<int> max,
            char separator, Maybe<object> defaultValue, string helpText, string metaValue, IEnumerable<string> enumValues,
            Type conversionType, TargetType targetType, bool hidden = false)
            : base(SpecificationType.Option, required, min, max, defaultValue, helpText, metaValue, enumValues, conversionType, targetType, hidden)
        {
            this.shortName = shortName;
            this.longName = longName;
            this.separator = separator;
            this.setName = setName;
        }

        public static OptionSpecification FromAttribute(OptionAttribute attribute, Type conversionType, IEnumerable<string> enumValues)
        {
            return new OptionSpecification(
                attribute.ShortName,
                attribute.LongName,
                attribute.Required,
                attribute.SetName,
                attribute.Min == -1 ? Maybe.Nothing<int>() : Maybe.Just(attribute.Min),
                attribute.Max == -1 ? Maybe.Nothing<int>() : Maybe.Just(attribute.Max),
                attribute.Separator,
                attribute.Default.ToMaybe(),
                attribute.HelpText,
                attribute.MetaValue,
                enumValues,
                conversionType,
                conversionType.ToTargetType(),
                attribute.Hidden);
        }

        public static OptionSpecification NewSwitch(string shortName, string longName, bool required, string helpText, string metaValue, bool hidden = false)
        {
            return new OptionSpecification(shortName, longName, required, string.Empty, Maybe.Nothing<int>(), Maybe.Nothing<int>(),
                '\0', Maybe.Nothing<object>(), helpText, metaValue, Enumerable.Empty<string>(), typeof(bool), TargetType.Switch, hidden);
        }

        public string ShortName
        {
            get { return shortName; }
        }

        public string LongName
        {
            get { return longName; }
        }

        public char Separator
        {
            get { return separator; }
        }

        public string SetName
        {
            get { return setName; }
        }
    }
    public sealed class ValueSpecification : Specification
    {
        private readonly int index;
        private readonly string metaName;

        public ValueSpecification(int index, string metaName, bool required, Maybe<int> min, Maybe<int> max, Maybe<object> defaultValue,
            string helpText, string metaValue, IEnumerable<string> enumValues,
            Type conversionType, TargetType targetType, bool hidden = false)
            : base(SpecificationType.Value, required, min, max, defaultValue, helpText, metaValue, enumValues, conversionType, targetType, hidden)
        {
            this.index = index;
            this.metaName = metaName;
        }

        public static ValueSpecification FromAttribute(ValueAttribute attribute, Type conversionType, IEnumerable<string> enumValues)
        {
            return new ValueSpecification(
                attribute.Index,
                attribute.MetaName,
                attribute.Required,
                attribute.Min == -1 ? Maybe.Nothing<int>() : Maybe.Just(attribute.Min),
                attribute.Max == -1 ? Maybe.Nothing<int>() : Maybe.Just(attribute.Max),
                attribute.Default.ToMaybe(),
                attribute.HelpText,
                attribute.MetaValue,
                enumValues,
                conversionType,
                conversionType.ToTargetType(),
                attribute.Hidden);
        }

        public int Index
        {
            get { return index; }
        }

        public string MetaName
        {
            get { return metaName; }
        }
    }

    public enum SpecificationType
    {
        Option,
        Value
    }

    public enum TargetType
    {
        Switch,
        Scalar,
        Sequence
    }

    public abstract class Specification
    {
        private readonly SpecificationType tag;
        private readonly bool required;
        private readonly bool hidden;
        private readonly Maybe<int> min;
        private readonly Maybe<int> max;
        private readonly Maybe<object> defaultValue;
        private readonly string helpText;
        private readonly string metaValue;
        private readonly IEnumerable<string> enumValues;
        /// This information is denormalized to decouple Specification from PropertyInfo.
        private readonly Type conversionType;
        private readonly TargetType targetType;

        protected Specification(SpecificationType tag, bool required, Maybe<int> min, Maybe<int> max,
            Maybe<object> defaultValue, string helpText, string metaValue, IEnumerable<string> enumValues,
            Type conversionType, TargetType targetType, bool hidden = false)
        {
            this.tag = tag;
            this.required = required;
            this.min = min;
            this.max = max;
            this.defaultValue = defaultValue;
            this.conversionType = conversionType;
            this.targetType = targetType;
            this.helpText = helpText;
            this.metaValue = metaValue;
            this.enumValues = enumValues;
            this.hidden = hidden;
        }

        public SpecificationType Tag
        {
            get { return tag; }
        }

        public bool Required
        {
            get { return required; }
        }

        public Maybe<int> Min
        {
            get { return min; }
        }

        public Maybe<int> Max
        {
            get { return max; }
        }

        public Maybe<object> DefaultValue
        {
            get { return defaultValue; }
        }

        public string HelpText
        {
            get { return helpText; }
        }

        public string MetaValue
        {
            get { return metaValue; }
        }

        public IEnumerable<string> EnumValues
        {
            get { return enumValues; }
        }

        public Type ConversionType
        {
            get { return conversionType; }
        }

        public TargetType TargetType
        {
            get { return targetType; }
        }

        public bool Hidden
        {
            get { return hidden; }
        }

        public static Specification FromProperty(PropertyInfo property)
        {
            var attrs = property.GetCustomAttributes(true);
            var oa = attrs.OfType<OptionAttribute>();
            if (oa.Count() == 1)
            {
                var spec = OptionSpecification.FromAttribute(oa.Single(), property.PropertyType,
                    property.PropertyType.GetTypeInfo().IsEnum
                        ? Enum.GetNames(property.PropertyType)
                        : Enumerable.Empty<string>());
                if (spec.ShortName.Length == 0 && spec.LongName.Length == 0)
                {
                    return spec.WithLongName(property.Name.ToLowerInvariant());
                }
                return spec;
            }

            var va = attrs.OfType<ValueAttribute>();
            if (va.Count() == 1)
            {
                return ValueSpecification.FromAttribute(va.Single(), property.PropertyType,
                    property.PropertyType.GetTypeInfo().IsEnum
                        ? Enum.GetNames(property.PropertyType)
                        : Enumerable.Empty<string>());
            }

            throw new InvalidOperationException();
        }
    }

    public static class OptionsExtensions
    {
        private static HelpText AddOptionsImpl(
            IEnumerable<Specification> specifications,
            string requiredWord,
            int maximumLength)
        {
            var maxLength = int.MaxValue / 2;

            optionsHelp = new StringBuilder();

            specifications.ForEach(
                option =>
                    AddOption(requiredWord, maxLength, option, int.MaxValue / 2));

            return this;
        }

        public static HelpText AddOptions<T>(Type t)
        {

            return AddOptionsImpl(
                GetSpecificationsFromType(t),
                SentenceBuilder.RequiredWord(),
                80);
        }

        public static IEnumerable<Specification> GetSpecificationsFromType(this Type type)
        {
            var specs = type.GetSpecifications(Specification.FromProperty);
            var optionSpecs = specs
                .OfType<OptionSpecification>()
                .Concat(new[] { MakeHelpEntry() });
            var valueSpecs = specs
                .OfType<ValueSpecification>()
                .OrderBy(v => v.Index);
            return Enumerable.Empty<Specification>()
                .Concat(optionSpecs)
                .Concat(valueSpecs);
        }

        private static OptionSpecification MakeHelpEntry()
        {
            return OptionSpecification.NewSwitch(
                string.Empty,
                "help",
                false,
                SentenceBuilder.Create().HelpCommandText(true),
                string.Empty,
                false);
        }

        public static bool IsOption(this Specification specification)
        {
            return specification.Tag == SpecificationType.Option;
        }

        public static bool IsValue(this Specification specification)
        {
            return specification.Tag == SpecificationType.Value;
        }

        public static OptionSpecification WithLongName(this OptionSpecification specification, string newLongName)
        {
            return new OptionSpecification(
                specification.ShortName,
                newLongName,
                specification.Required,
                specification.SetName,
                specification.Min,
                specification.Max,
                specification.Separator,
                specification.DefaultValue,
                specification.HelpText,
                specification.MetaValue,
                specification.EnumValues,
                specification.ConversionType,
                specification.TargetType,
                specification.Hidden);
        }

        public static string UniqueName(this OptionSpecification specification)
        {
            return specification.ShortName.Length > 0 ? specification.ShortName : specification.LongName;
        }

        public static IEnumerable<Specification> ThrowingValidate(this IEnumerable<Specification> specifications, IEnumerable<Tuple<Func<Specification, bool>, string>> guardsLookup)
        {
            foreach (var guard in guardsLookup)
            {
                if (specifications.Any(spec => guard.Item1(spec)))
                {
                    throw new InvalidOperationException(guard.Item2);
                }
            }

            return specifications;
        }

        public static bool HavingRange(this Specification specification, Func<int, int, bool> predicate)
        {
            int min;
            int max;
            if (specification.Min.MatchJust(out min) && specification.Max.MatchJust(out max))
            {
                return predicate(min, max);
            }
            return false;
        }

        public static bool HavingMin(this Specification specification, Func<int, bool> predicate)
        {
            int min;
            if (specification.Min.MatchJust(out min))
            {
                return predicate(min);
            }
            return false;
        }

        public static bool HavingMax(this Specification specification, Func<int, bool> predicate)
        {
            int max;
            if (specification.Max.MatchJust(out max))
            {
                return predicate(max);
            }
            return false;
        }
        public static IEnumerable<T> GetSpecifications<T>(this Type type, Func<PropertyInfo, T> selector)
        {
            return from pi in type.FlattenHierarchy().SelectMany(x => x.GetTypeInfo().GetProperties())
                   let attrs = pi.GetCustomAttributes(true)
                   where
                       attrs.OfType<OptionAttribute>().Any() ||
                       attrs.OfType<ValueAttribute>().Any()
                   group pi by pi.Name into g
                   select selector(g.First());
        }

        public static Maybe<VerbAttribute> GetVerbSpecification(this Type type)
        {
            return
                (from attr in
                 type.FlattenHierarchy().SelectMany(x => x.GetTypeInfo().GetCustomAttributes(typeof(VerbAttribute), true))
                 let vattr = (VerbAttribute)attr
                 select vattr)
                    .SingleOrDefault()
                    .ToMaybe();
        }

        public static Maybe<Tuple<PropertyInfo, UsageAttribute>> GetUsageData(this Type type)
        {
            return
                (from pi in type.FlattenHierarchy().SelectMany(x => x.GetTypeInfo().GetProperties())
                 let attrs = pi.GetCustomAttributes(true)
                 where attrs.OfType<UsageAttribute>().Any()
                 select Tuple.Create(pi, (UsageAttribute)attrs.First()))
                        .SingleOrDefault()
                        .ToMaybe();
        }

        private static IEnumerable<Type> FlattenHierarchy(this Type type)
        {
            if (type == null)
            {
                yield break;
            }
            yield return type;
            foreach (var @interface in type.SafeGetInterfaces())
            {
                yield return @interface;
            }
            foreach (var @interface in FlattenHierarchy(type.GetTypeInfo().BaseType))
            {
                yield return @interface;
            }
        }

        private static IEnumerable<Type> SafeGetInterfaces(this Type type)
        {
            return type == null ? Enumerable.Empty<Type>() : type.GetTypeInfo().GetInterfaces();
        }

        public static TargetType ToTargetType(this Type type)
        {
            return type == typeof(bool)
                       ? TargetType.Switch
                       : type == typeof(string)
                             ? TargetType.Scalar
                             : type.IsArray || typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(type)
                                   ? TargetType.Sequence
                                   : TargetType.Scalar;
        }

        private static T SetValue<T>(this PropertyInfo property, T instance, object value)
        {
            Action<Exception> fail = inner => {
                throw new InvalidOperationException("Cannot set value to target instance.", inner);
            };

            try
            {
                property.SetValue(instance, value, null);
            }
#if !PLATFORM_DOTNET
            catch (TargetException e)
            {
                fail(e);
            }
#endif
            catch (TargetParameterCountException e)
            {
                fail(e);
            }
            catch (MethodAccessException e)
            {
                fail(e);
            }
            catch (TargetInvocationException e)
            {
                fail(e);
            }

            return instance;
        }

        public static object CreateEmptyArray(this Type type)
        {
            return Array.CreateInstance(type, 0);
        }

        public static object GetDefaultValue(this Type type)
        {
            var e = Expression.Lambda<Func<object>>(
                Expression.Convert(
                    Expression.Default(type),
                    typeof(object)));
            return e.Compile()();
        }

        public static bool IsMutable(this Type type)
        {
            Func<bool> isMutable = () => {
                var props = type.GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => p.CanWrite);
                var fields = type.GetTypeInfo().GetFields(BindingFlags.Public | BindingFlags.Instance).Any();
                return props || fields;
            };
            return type != typeof(object) ? isMutable() : true;
        }

        public static object CreateDefaultForImmutable(this Type type)
        {
            if (type == typeof(string))
            {
                return string.Empty;
            }
            if (type.GetTypeInfo().IsGenericType && type.GetTypeInfo().GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GetTypeInfo().GetGenericArguments()[0].CreateEmptyArray();
            }
            return type.GetDefaultValue();
        }

        public static object StaticMethod(this Type type, string name, params object[] args)
        {
#if NETSTANDARD1_5
            MethodInfo method = type.GetTypeInfo().GetDeclaredMethod(name);
            return method.Invoke(null, args);
#else
            return type.GetTypeInfo().InvokeMember(
                name,
                BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static,
                null,
                null,
                args);
#endif
        }

        public static object StaticProperty(this Type type, string name)
        {
#if NETSTANDARD1_5
            PropertyInfo property = type.GetTypeInfo().GetDeclaredProperty(name);
            return property.GetValue(null);
#else
            return type.GetTypeInfo().InvokeMember(
                name,
                BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Static,
                null,
                null,
                new object[] { });
#endif
        }

        public static object InstanceProperty(this Type type, string name, object target)
        {
#if NETSTANDARD1_5
            PropertyInfo property = type.GetTypeInfo().GetDeclaredProperty(name);
            return property.GetValue(target);
#else
            return type.GetTypeInfo().InvokeMember(
                name,
                BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance,
                null,
                target,
                new object[] { });
#endif
        }

        public static bool IsPrimitiveEx(this Type type)
        {
            return
                   (type.GetTypeInfo().IsValueType && type != typeof(Guid))
                || type.GetTypeInfo().IsPrimitive
                || new[] {
                     typeof(string)
                    ,typeof(decimal)
                    ,typeof(DateTime)
                    ,typeof(DateTimeOffset)
                    ,typeof(TimeSpan)
                   }.Contains(type)
                || Convert.GetTypeCode(type) != TypeCode.Object;
        }

        
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }

        public static Attribute[] GetCustomAttributes(this Type type, Type attributeType, bool inherit)
        {
            return type.GetTypeInfo().GetCustomAttributes(attributeType, inherit).OfType<Attribute>().ToArray();
        }

        public static Attribute[] GetCustomAttributes(this Assembly assembly, Type attributeType, bool inherit)
        {
            return assembly.GetCustomAttributes(attributeType).ToArray();
        }
    }
}
