using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using QuickGraph.Algorithms;

namespace ClosureExterns
{
    public class ClosureExternsGenerator
    {
        protected readonly Assembly[] _assemblies;
        protected readonly IDictionary<string, IEnumerable<Type>> _typesMap;
        protected readonly ClosureExternsOptions _options;
        protected IDictionary<Type, string> _typeNamespaceMap;

        protected ClosureExternsGenerator(IDictionary<string, IEnumerable<Type>> typesMap, ClosureExternsOptions options)
        {
            this._options = options;
            this._typesMap = (typesMap ?? new Dictionary<string, IEnumerable<Type>> { }).ToDictionary(p => p.Key, p => p.Value.Select(this.MapType));
            this._typeNamespaceMap = this._typesMap
                .SelectMany(p => p.Value
                    .Select(t => new KeyValuePair<Type, string>(t, p.Key)))
                .ToDictionary(x => x.Key, x => x.Value);
            this._assemblies = this._typesMap.SelectMany(p => p.Value)
                .GroupBy(x => x.Assembly)
                .Select(x => x.Key)
                .ToArray();
        }

        #region Public Methods

        /// <summary>
        /// Generates closure externs using the given options.
        /// </summary>
        /// <param name="typesMap">A set of base types to generate. Any dependencies will also be included recursively
        /// <para>(e.g. if a property of a type is of some type not in the list, that type will be generated as well)</para>
        /// </param>
        /// <param name="options"/>
        /// <returns></returns>
        public static string Generate(IDictionary<string, IEnumerable<Type>> typesMap, ClosureExternsOptions options)
        {
            var generator = new ClosureExternsGenerator(typesMap, options);
            return generator.Generate();
        }

        /// <summary>
        /// Generates closure externs using the default options.
        /// </summary>
        /// <param name="typesMap">A set of base types to generate. Any dependencies will also be included recursively
        /// <para>(e.g. if a property of a type is of some type not in the list, that type will be generated as well)</para>
        /// </param>
        /// <returns></returns>
        public static string Generate(IDictionary<string, IEnumerable<Type>> typesMap)
        {
            var generator = new ClosureExternsGenerator(typesMap, new ClosureExternsOptions());
            return generator.Generate();
        }

        /// <summary>
        /// Utility. Returns all the types in the same assembly and namespace as the given example type.
        /// </summary>
        public static IEnumerable<Type> GetTypesInNamespace(Type exampleType)
        {
            var targetNamespace = exampleType.Namespace;
            return exampleType.Assembly
                              .GetTypes()
                              // Anonymous types have null namespace
                              .Where(x => (null != x.Namespace) && x.Namespace.Equals(targetNamespace));
        }

        #endregion

        #region Protected Methods

        protected string Generate()
        {
            var resultBuilder = new StringBuilder();
            var typeDefinitions = new Dictionary<Type, string>();
            var graph = new QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>>(false);
            if (false == this._typesMap.Any())
            {
                resultBuilder.AppendLine(this._options.NamespaceDefinitionExpression(this._options.NamespaceVarName));
                resultBuilder.AppendLine();
            }
            foreach (var typePair in this._typesMap)
            {
                graph.AddVertexRange(typePair.Value.Except(graph.Vertices));
                var @namespace = (string.IsNullOrEmpty(typePair.Key) ? this._options.NamespaceVarName : typePair.Key);
                resultBuilder.AppendLine(this._options.NamespaceDefinitionExpression(@namespace));
                resultBuilder.AppendLine();

                GenerateTypeDefinitions(@namespace, graph, typeDefinitions);
            }
            foreach (var type in graph.TopologicalSort().Reverse())
            {
                string definition;
                if (typeDefinitions.TryGetValue(type, out definition))
                {
                    resultBuilder.Append(definition);
                }
                else if (false == type.Assembly.Equals(Assembly.GetAssembly(typeof(string))))
                {
                    resultBuilder.AppendLine("// unknown: " + type.Name + Environment.NewLine);
                }
            }

            return resultBuilder.ToString();
        }

        protected void GenerateTypeDefinitions(string @namespace, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph, IDictionary<Type, string> typeDefinitions)
        {
            // Every time we generate a type, we may find new type dependencies and they will be added to the graph.
            // We keep generating types as long as the graph contains items that were not generated.
            var processedTypes = new HashSet<Type>();
            while (true)
            {
                var unprocessedTypes = graph.Vertices.Except(processedTypes).ToArray();
                if (false == unprocessedTypes.Any())
                {
                    break;
                }
                foreach (var type in unprocessedTypes)
                {
                    processedTypes.Add(type);
                    if (false == InAllowedAssemblies(type))
                    {
                        continue;
                    }
                    if (typeDefinitions.ContainsKey(type))
                    {
                        continue;
                    }
                    GenerateTypeDefinition(@namespace, graph, typeDefinitions, type);
                }
            }
        }

        private bool InAllowedAssemblies(Type type)
        {
            return this._assemblies.Any(x => x.Equals(type.Assembly));
        }

        protected void GenerateTypeDefinition(string @namespace, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph, IDictionary<Type, string> typeDefinitions, Type type)
        {
            if (type.IsClass)
            {
                var typeResultBuilder = GenerateClassDefinition(@namespace, type, graph);
                typeDefinitions.Add(type, typeResultBuilder.ToString());
            }

            if (type.IsEnum)
            {
                var typeResultBuilder = GenerateEnumDefinition(@namespace, type, graph);
                typeDefinitions.Add(type, typeResultBuilder.ToString());
            }
        }

        protected Type MapType(Type type)
        {
            return this._options.MapType(type);
        }

        protected StringBuilder GenerateClassDefinition(string @namespace, Type type, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph)
        {
            Console.Error.WriteLine("Generating Class: " + type.Name);
            var typeResultBuilder = new StringBuilder();
            var className = this.GetFullTypeName(@namespace, type);
            AppendTypeComment(typeResultBuilder, type);

            typeResultBuilder.AppendLine("/** @constructor");
            if (type.IsGenericTypeDefinition)
            {
                typeResultBuilder.AppendLine(" * @template " +
                    String.Join(", ", type.GetGenericArguments().Select(x => x.Name)));
            }
            foreach (var annotation in this._options.ConstructorAnnotations(className))
            {
                typeResultBuilder.AppendLine(" * " + annotation);
            }
            typeResultBuilder.AppendLine(" */");
            typeResultBuilder.AppendLine(className + " = " + this._options.ConstructorExpression(className) + ";");

            var properties = type.GetProperties();
            foreach (var propertyPair in WithIsLast(properties))
            {
                var property = propertyPair.Item1;
                string propertyName = this.GetPropertyName(type, property.Name);
                var mappedType = MapType(property.PropertyType);
                var jsTypeName = GetJSTypeName(type, @namespace, mappedType, graph);
                typeResultBuilder.AppendLine(String.Format("/** @type {{{0}}} */", jsTypeName));
                typeResultBuilder.AppendLine(String.Format("{0}.prototype.{1} = {2};", className, propertyName, GetDefaultJSValue(@namespace, mappedType)));
            }

            typeResultBuilder.AppendLine();
            return typeResultBuilder;
        }

        private string GetPropertyName(Type type, string propertyName)
        {
            var customPropertyName = this._options.TryGetPropertyName(type, propertyName);
            if (false == string.IsNullOrEmpty(customPropertyName))
            {
                return customPropertyName;
            }

            if (false == type.IsEnum)
            {
                // TODO: Improve the behavior, it should be able to change casing to lower based on words.
                return propertyName.Substring(0, 1).ToLowerInvariant() + propertyName.Substring(1);
            }

            return propertyName;
        }

        private object GetDefaultJSValue(string @namespace, Type type)
        {
            var customValue = this._options.TryGetDefaultJSValue(type);
            if (null != customValue)
            {
                return customValue;
            }
            if (type.IsGenericType && IsGenericTypeNullable(type))
            {
                return "null";
            }
            if ((type == typeof(string)) ||
                (type == typeof(char)) ||
                (type == typeof(Guid)))
            {
                return "''";
            }
            if (type == typeof(bool))
            {
                return "false";
            }
            if (type.IsEnum)
            {
                return this.GetFullTypeName(@namespace, type) + "." + Enum.GetName(type, Activator.CreateInstance(type));
            }
            if (type.IsValueType && (type != typeof(DateTime)))
            {
                return Activator.CreateInstance(type);
            }
            return "null";
        }

        protected static void AppendTypeComment(StringBuilder typeResultBuilder, Type type)
        {
            typeResultBuilder.AppendLine("// " + type.FullName);
        }

        protected static IEnumerable<Tuple<T, bool>> WithIsLast<T>(T[] items)
        {
            return items.Select((p, i) => new Tuple<T, bool>(p, i + 1 == items.Count()));
        }

        protected string GetJSTypeName(Type sourceType, string @namespace, Type propertyType, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph)
        {
            if (propertyType.IsGenericParameter)
            {
                return propertyType.Name;
            }
            var overrideTypeName = this._options.TryGetTypeName(propertyType);
            if (String.IsNullOrWhiteSpace(overrideTypeName))
            {
                if (propertyType.IsGenericType)
                {
                    if (IsGenericTypeNullable(propertyType))
                    {
                        return "?" + GetJSTypeName(sourceType, @namespace, Nullable.GetUnderlyingType(propertyType), graph);
                    }
                    var dictionaryType = propertyType.GetInterfaces().SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>));
                    if (null != dictionaryType)
                    {
                        var typeArgs = dictionaryType.GetGenericArguments().ToArray();
                        var keyType = typeArgs[0];
                        var valueType = typeArgs[1];
                        return GetJSObjectTypeName(sourceType, @namespace, keyType, valueType, graph);
                    }
                    var enumerableType = propertyType.GetInterfaces().SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                    if (null != enumerableType)
                    {
                        return GetJSArrayTypeName(sourceType, @namespace, enumerableType.GetGenericArguments().Single(), graph);
                    }
                }
                if (propertyType.IsArray)
                {
                    var elementType = propertyType.GetElementType();
                    return GetJSArrayTypeName(sourceType, @namespace, elementType, graph);
                }
            }
            graph.AddVertex(propertyType);
            graph.AddVertex(sourceType);
            if (InAllowedAssemblies(propertyType))
            {
                graph.AddEdge(new QuickGraph.Edge<Type>(sourceType, propertyType));
                return this.GetFullTypeName(@namespace, propertyType);
            }
            return overrideTypeName ?? GetTypeName(propertyType);
        }

        private static bool IsGenericTypeNullable(Type propertyType)
        {
            return propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private string GetJSArrayTypeName(Type sourceType, string @namespace, Type propertyType, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph)
        {
            return "Array.<" + GetJSTypeName(sourceType, @namespace, propertyType, graph) + ">";
        }

        private string GetJSObjectTypeName(Type sourceType, string @namespace, Type keyType, Type valueType, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph)
        {
            return "Object.<" + GetJSTypeName(sourceType, @namespace, keyType, graph) + ", " + GetJSTypeName(sourceType, @namespace, valueType, graph) + ">";
        }

        protected string GetTypeName(Type type)
        {
            // Strip `1 (or `2, etc.) suffix from generic types
            var typeName = new string(type.Name.TakeWhile(char.IsLetterOrDigit).ToArray());
            if (InAllowedAssemblies(type))
            {
                if (false == string.IsNullOrWhiteSpace(this._options.SuffixToTrimFromTypeNames))
                {
                    return TrimSuffix(typeName, this._options.SuffixToTrimFromTypeNames);
                }
                return typeName;
            }
            switch (typeName.ToLowerInvariant())
            {
                case "string": return "string";
                case "boolean": return "boolean";
                case "decimal": return "number";
                case "double": return "number";
                case "float": return "number";
                case "int32": return "number";
                case "datetime": return "Date";
                case "char": return "string";
                case "guid": return "string";
                default: return "?";
            }
        }

        private static string TrimSuffix(string stringToTrim, string suffix)
        {
            if (stringToTrim.EndsWith(suffix, StringComparison.InvariantCultureIgnoreCase))
            {
                return stringToTrim.Substring(0, stringToTrim.Length - suffix.Length);
            }
            return stringToTrim;
        }

        protected StringBuilder GenerateEnumDefinition(string @namespace, Type type, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph)
        {
            Console.Error.WriteLine("Generating Enum: " + type.Name);
            var typeResultBuilder = new StringBuilder();
            AppendTypeComment(typeResultBuilder, type);
            typeResultBuilder.AppendLine("/** @enum {string} */");
            typeResultBuilder.AppendLine(GetFullTypeName(@namespace, type) + " = {");
            foreach (var pair in WithIsLast(Enum.GetNames(type)))
            {
                string propertyValue = pair.Item1;
                string propertyName = this.GetPropertyName(type, propertyValue);
                var spacing = pair.Item2 ? String.Empty : ",";
                typeResultBuilder.AppendFormat("    '{0}': '{1}'{2}{3}", propertyName, propertyValue, spacing, Environment.NewLine);
            }
            typeResultBuilder.AppendLine("};");
            typeResultBuilder.AppendLine();
            return typeResultBuilder;
        }

        private string GetFullTypeName(string @namespace, Type type)
        {
            string typeNamespace;
            if (false == this._typeNamespaceMap.TryGetValue(type, out typeNamespace))
            {
                typeNamespace = @namespace;
            }
            var overrideTypeName = this._options.TryGetTypeName(type);
            return typeNamespace + "." + (overrideTypeName ?? GetTypeName(type));
        }

        #endregion
    }
}
