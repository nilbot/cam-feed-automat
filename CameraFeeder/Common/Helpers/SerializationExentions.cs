using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack.Text;

namespace Feeder.Common.Helpers
{
    internal static class AssemblyExtenstions
    {
        public static HashSet<Type> GetPublicEnums(this ICollection<Assembly> assemblies, Func<string, bool> enumNamespaceFilter)
        {
            if (assemblies.IsEmpty())
            {
                return new HashSet<Type>();
            }

            if (enumNamespaceFilter == null)
            {
                enumNamespaceFilter = EnumSerializerConfigurator.AlwaysTrueFilter;
            }

            var _enumTypes = new List<Type>();

            foreach (var _assembly in assemblies)
            {
                if (_assembly == null)
                {
                    continue;
                }
                var _assemblyPublicEnums =
                    (from publicEnumType in _assembly.GetTypes().GetPublicEnums()
                     where enumNamespaceFilter(publicEnumType.Namespace ?? string.Empty)
                     select publicEnumType
                    ).ToList();
                _enumTypes.AddRange(_assemblyPublicEnums);
            }
            return new HashSet<Type>(_enumTypes);
        }
    }
    internal static class CollectionExtensions
    {
        public static bool IsEmpty<T>(this ICollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }
    }
    internal static class EnumMemberAttributeExtensions
    {
        public static bool MatchesDescription(this EnumMemberAttribute attribute, string description)
        {
            return
                attribute != null
                && string.Equals(
                    attribute.Value, (description ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }    /// <summary>
    ///     Fluent configuration for the enum member enumeration serializer
    /// </summary>
    public sealed class EnumSerializerConfigurator : IEnumSerializerConfigurator
    {
        private readonly HashSet<Assembly> _assembliesToScan = new HashSet<Assembly>();
        private readonly HashSet<Type> _enumTypes = new HashSet<Type>();
        private readonly object _lockObject = new object();
        private bool _configureNullableEnumSerilalizers;
        private Func<string, bool> _enumNamespaceFilter = AlwaysTrueFilter;
        private IEnumSerializerInitializerProxy _jsConfigManager;

        internal static Func<string, bool> AlwaysTrueFilter
        {
            get { return s => true; }
        }

        internal IEnumSerializerInitializerProxy JsConfigProxy
        {
            get { return _jsConfigManager ?? (_jsConfigManager = new EnumSerializerInitializerProxy()); }
            set { _jsConfigManager = value; }
        }

        /// <summary>
        ///     Only configure enumerations that match the provided namespace filter.
        ///     This filter applies to the types found in the provided assembly list.
        /// </summary>
        /// <param name="enumNamespaceFilter">Returns true for an acceptable namespace.</param>
        public IEnumSerializerConfigurator WithNamespaceFilter(Func<string, bool> enumNamespaceFilter)
        {
            if (enumNamespaceFilter != null)
            {
                _enumNamespaceFilter = enumNamespaceFilter;
            }

            return this;
        }

        /// <summary>
        ///     Search the provided assemblies for enumerations to configure.
        ///     Multiple calls will add to the existing list.
        /// </summary>
        /// <param name="assembliesToScan"></param>
        public IEnumSerializerConfigurator WithAssemblies(ICollection<Assembly> assembliesToScan)
        {
            if (!assembliesToScan.IsEmpty())
            {
                foreach (var _assembly in assembliesToScan)
                {
                    if (_assembly != null)
                    {
                        _assembliesToScan.Add(_assembly);
                    }
                }
            }

            return this;
        }

        /// <summary>
        ///     Allows individual enumeration types to be specified.
        ///     Multiple calls will add to the existing list.
        /// </summary>
        public IEnumSerializerConfigurator WithEnumTypes(ICollection<Type> enumTypes)
        {
            if (!enumTypes.IsEmpty())
            {
                var _publicEnums = enumTypes.GetPublicEnums();
                _enumTypes.UnionWith(_publicEnums);
            }

            return this;
        }

        /// <summary>
        ///     This will configure the nullable enumeration as well as the non-nullable enumeration (recommended).
        /// </summary>
        public IEnumSerializerConfigurator WithNullableEnumSerializers()
        {
            _configureNullableEnumSerilalizers = true;
            return this;
        }

        /// <summary>
        ///     Configures ServiceStack JsConfig with the custom enumeration serializers.
        /// </summary>
        public void Configure()
        {
            lock (_lockObject)
            {
                var _assemblyPublicEnums = _assembliesToScan.GetPublicEnums(_enumNamespaceFilter);

                foreach (var _assemblyPublicEnum in _assemblyPublicEnums)
                {
                    _enumTypes.Add(_assemblyPublicEnum);
                }

                foreach (var _enumType in _enumTypes)
                {
                    if (_configureNullableEnumSerilalizers)
                    {
                        JsConfigProxy.ConfigEnumAndNullableEnumSerializers(_enumType);
                    }
                    else
                    {
                        JsConfigProxy.ConfigEnumSerializers(_enumType);
                    }
                }
            }
        }
    }
    internal class EnumSerializerInitializer<TEnum> where TEnum : struct
    {
        public EnumSerializerInitializer()
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("Type parameter must be an enum.");
            }
        }

        public void InitializeEnumSerializer()
        {
            JsConfig<TEnum>.Reset();
            JsConfig<TEnum>.SerializeFn = PrettyEnumHelpers<TEnum>.GetOptimalEnumDescription;
            JsConfig<TEnum>.DeSerializeFn = PrettyEnumHelpers<TEnum>.GetEnumFrom;
        }

        public void InitializeEnumAndNullableEnumSerializer()
        {
            InitializeEnumSerializer();
            //ServiceStack.Text will never use the serialize / deserialize fn if the value is null 
            //or the text is null or empty.
            JsConfig<TEnum?>.Reset();
            JsConfig<TEnum?>.SerializeFn = PrettyEnumHelpers<TEnum>.GetOptimalEnumDescription;
            JsConfig<TEnum?>.DeSerializeFn = PrettyEnumHelpers<TEnum>.GetNullableEnumFrom;
        }
    }
    internal class EnumSerializerInitializerProxy : IEnumSerializerInitializerProxy
    {
        //Hide the static class interaction as much as possible
        public void ConfigEnumSerializers(Type type)
        {
            var _mi = getMethodInfo<EnumSerializerInitializer<int>>(x => x.InitializeEnumSerializer());
            executeConfigureMethod(_mi, type);
        }

        private static MethodInfo getMethodInfo<T>(Expression<Action<T>> expression)
        {
            return ((MethodCallExpression) expression.Body).Method;
        }

        public void ConfigEnumAndNullableEnumSerializers(Type type)
        {
            var _mi = getMethodInfo<EnumSerializerInitializer<int>>(x => x.InitializeEnumAndNullableEnumSerializer());
            executeConfigureMethod(_mi, type);
        }

        private static void executeConfigureMethod(MethodInfo mi, Type type)
        {
            var _genericType = typeof(EnumSerializerInitializer<>).MakeGenericType(new[] { type });
            var _genericTypeMyMethodInfo = _genericType.GetMethod(mi.Name);

            _genericTypeMyMethodInfo.Invoke(Activator.CreateInstance(_genericType), null);
        }
    }
    internal static class EnumTypeExtensions
    {
        public static HashSet<Type> GetPublicEnums(this ICollection<Type> types)
        {
            if (types.IsEmpty())
            {
                return new HashSet<Type>();
            }

            var _enumTypes =
                (from type in types.AsParallel()
                 where
                     type != null
                     && type.IsEnum
                     && type.IsPublic
                 select type
                ).ToList();

            return new HashSet<Type>(_enumTypes);
        }
    }
    internal static class FieldInfoExtensions
    {
        public static bool MatchesDescription(this FieldInfo field, string description)
        {
            return string.Equals(field.Name, (description ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }    /// <summary>
    ///     Fluent configuration for the custom enumeration serializer.
    /// </summary>
    public interface IEnumSerializerConfigurator
    {
        /// <summary>
        ///     Filter to apply to namespaces.
        /// </summary>
        /// <param name="enumNamespaceFilter">Returns true for an acceptable namespace.</param>
        IEnumSerializerConfigurator WithNamespaceFilter(Func<string, bool> enumNamespaceFilter);

        /// <summary>
        ///     Specifies assemblies to search.
        /// </summary>
        /// <param name="assembliesToScan"></param>
        IEnumSerializerConfigurator WithAssemblies(ICollection<Assembly> assembliesToScan);

        /// <summary>
        ///     Specifies individual enumeration types.
        /// </summary>
        IEnumSerializerConfigurator WithEnumTypes(ICollection<Type> enumTypes);

        /// <summary>
        /// This will configure the nullable enumeration as well as the non-nullable enumeration (recommended).
        /// </summary>
        IEnumSerializerConfigurator WithNullableEnumSerializers();

        /// <summary>
        ///     Perform configuration action.
        /// </summary>
        void Configure();
    }
    internal interface IEnumSerializerInitializerProxy
    {
        void ConfigEnumSerializers(Type type);
        void ConfigEnumAndNullableEnumSerializers(Type type);
    }    /// <summary>
    ///     Serialize/Deserialize enumerations using EnumMember Attribute Value if present.
    /// </summary>
    public static class PrettyEnumExtensions
    {
        /// <summary>
        ///     Gets the optimal string representation of a nullable enumeration.
        /// </summary>
        /// <returns>Returns EnumMember Value if present, otherwise returns enumValue.ToString().</returns>
        /// <exception cref="InvalidOperationException">The type parameter is not an Enumeration.</exception>
        /// <typeparam name="TEnum">The type must be an enumeration.</typeparam>
        public static string GetOptimalEnumDescription<TEnum>(this TEnum? enumValue) where TEnum : struct
        {
            return PrettyEnumHelpers<TEnum>.GetOptimalEnumDescription(enumValue);
        }

        /// <summary>
        ///     Gets the optimal string representation of an enumeration.
        /// </summary>
        /// <returns>Returns EnumMember Value if present, otherwise returns enumValue.ToString().</returns>
        /// <exception cref="InvalidOperationException">The type parameter is not an Enumeration.</exception>
        /// <typeparam name="TEnum">The type must be an enumeration.</typeparam>
        public static string GetOptimalEnumDescription<TEnum>(this TEnum enumValue) where TEnum : struct
        {
            return PrettyEnumHelpers<TEnum>.GetOptimalEnumDescription(enumValue);
        }

        /// <summary>
        ///     Gets the enumeration for the given string representation.
        /// </summary>
        /// <param name="enumValue">The EnumMemberAttribute value, enumeration string value, or integer value (as string) of the enumeration.</param>
        /// <exception cref="InvalidOperationException">The type parameter is not an Enumeration.</exception>
        /// <typeparam name="TEnum">The type must be an enumeration.</typeparam>
        public static TEnum GetEnum<TEnum>(this string enumValue) where TEnum : struct
        {
            return PrettyEnumHelpers<TEnum>.GetEnumFrom(enumValue);
        }

        /// <summary>
        ///     Gets the nullable enumeration for the given string representation.
        /// </summary>
        /// <param name="enumValue">The EnumMemberAttribute value, enumeration string value, or integer value (as string) of the enumeration.</param>
        /// <exception cref="InvalidOperationException">The type parameter is not an Enumeration.</exception>
        /// <typeparam name="TEnum">The type must be an enumeration.</typeparam>
        public static TEnum? GetNullableEnum<TEnum>(this string enumValue) where TEnum : struct
        {
            return PrettyEnumHelpers<TEnum>.GetNullableEnumFrom(enumValue);
        }
    }
    internal static class PrettyEnumHelpers<TEnum> where TEnum : struct
    {
        //These have to be separate since multiple string values can deserialize to the same enum
        //i.e. "1", "MyEnum", "My Enum" can all resolve to the same enum value.
        internal static ConcurrentDictionary<TEnum, string> SerializeCache = new ConcurrentDictionary<TEnum, string>();
        internal static ConcurrentDictionary<string, TEnum> DeserializeCache = new ConcurrentDictionary<string, TEnum>();

        public static string GetOptimalEnumDescription(TEnum enumValue)
        {
            return SerializeEnum(enumValue, SerializeCache);
        }

        public static string GetOptimalEnumDescription(TEnum? enumValue)
        {
            return enumValue.HasValue ? SerializeEnum(enumValue.Value, SerializeCache) : null;
        }

        internal static string SerializeEnum(TEnum enumValue, ConcurrentDictionary<TEnum, string> cache)
        {
            return cache.GetOrAdd(enumValue, SerializeEnum);
        }

        internal static string SerializeEnum(TEnum enumValue)
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new InvalidOperationException();
            }

            EnumMemberAttribute _attribute = getEnumMemberAttribute(enumValue);
            string _attributeValue = _attribute == null ? string.Empty : _attribute.Value;

            string _stringValue = string.IsNullOrWhiteSpace(_attributeValue)
                                     ? enumValue.ToString()
                                     : _attributeValue;

            return _stringValue;
        }

        private static EnumMemberAttribute getEnumMemberAttribute(object enumVal)
        {
            Type _type = enumVal.GetType();
            MemberInfo[] _memberInfo = _type.GetMember(enumVal.ToString());

            if (_memberInfo.Length == 0)
            {
                return null;
            }

            object[] _attributes = _memberInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false);

            return _attributes.IsEmpty() ? null : (EnumMemberAttribute)_attributes[0];
        }

        public static TEnum GetEnumFrom(string enumValue)
        {
            return DeserializeEnum(enumValue, DeserializeCache);
        }

        public static TEnum? GetNullableEnumFrom(string enumValue)
        {
            return DeserializeNullableEnum(enumValue, DeserializeCache);
        }

        internal static TEnum DeserializeEnum(string enumValue, ConcurrentDictionary<string, TEnum> cache)
        {
            return enumValue == null ? default(TEnum) : cache.GetOrAdd(enumValue, DeserializeEnum);
        }

        internal static TEnum? DeserializeNullableEnum(string enumValue, ConcurrentDictionary<string, TEnum> cache)
        {
            return enumValue == null ? default(TEnum?) : cache.GetOrAdd(enumValue, DeserializeEnum);
        }

        internal static TEnum DeserializeEnum(string enumValue)
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new InvalidOperationException();
            }

            TEnum _enumObject;
            if (tryGetValueFromDescription(enumValue, out _enumObject))
                return _enumObject;

            Enum.TryParse(enumValue, true, out _enumObject);
            return _enumObject;
        }

        private static bool tryGetValueFromDescription(string description, out TEnum enumObject)
        {
            Type _type = typeof(TEnum);

            foreach (FieldInfo _field in _type.GetFields())
            {
                var _attribute =
                    Attribute.GetCustomAttribute(_field, typeof(EnumMemberAttribute)) as EnumMemberAttribute;

                if (_attribute.MatchesDescription(description) || _field.MatchesDescription(description))
                {
                    enumObject = (TEnum)_field.GetValue(null);
                    return true;
                }
            }
            enumObject = default(TEnum);
            return false;
        }
    }
}