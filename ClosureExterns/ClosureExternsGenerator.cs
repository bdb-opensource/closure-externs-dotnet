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
        protected readonly Type[] _types;
        protected readonly ClosureExternsOptions _options;

        protected ClosureExternsGenerator(IEnumerable<Type> types, ClosureExternsOptions options)
        {
            this._options = options;
            this._types = (types ?? new Type[] { }).Select(MapType).ToArray();
            this._assemblies = this._types.GroupBy(x => x.Assembly)
                                          .Select(x => x.Key)
                                          .ToArray();
        }

        #region Public Methods

        /// <summary>
        /// Generates closure externs using the given options.
        /// </summary>
        /// <param name="types">A set of base types to generate. Any dependencies will also be included recursively
        /// <para>(e.g. if a property of a type is of some type not in the list, that type will be generated as well)</para>
        /// </param>
        /// <returns></returns>
        public static string Generate(IEnumerable<Type> types, ClosureExternsOptions options)
        {
            var generator = new ClosureExternsGenerator(types, options);
            return generator.Generate();
        }


        /// <summary>
        /// Generates closure externs using the default options.
        /// </summary>
        /// <param name="types">A set of base types to generate. Any dependencies will also be included recursively
        /// <para>(e.g. if a property of a type is of some type not in the list, that type will be generated as well)</para>
        /// </param>
        /// <returns></returns>
        public static string Generate(IEnumerable<Type> types)
        {
            var generator = new ClosureExternsGenerator(types, new ClosureExternsOptions());
            return generator.Generate();
        }

        /// <summary>
        /// Utility. Returns all the types in the same assembly and namespace (or deeper) as the given example type.
        /// </summary>
        public static IEnumerable<Type> GetTypesInNamespace(Type exampleType)
        {
            var targetNamespace = exampleType.Namespace;
            return exampleType.Assembly
                              .GetTypes()
                // Anonymous types have null namespace
                              .Where(x => (null != x.Namespace) && x.Namespace.StartsWith(targetNamespace));
        }

        #endregion

        #region Protected Methods

        protected string Generate()
        {
            var resultBuilder = new StringBuilder();

            var graph = new QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>>(false);
            graph.AddVertexRange(_types);

            var typeDefinitions = new Dictionary<Type, String>();
            resultBuilder.AppendLine(this._options.NamespaceDefinitionExpression(this._options.NamespaceVarName));
            resultBuilder.AppendLine();

            GenerateTypeDefinitions(this._options.NamespaceVarName, graph, typeDefinitions);

            foreach (var type in graph.TopologicalSort().Reverse())
            {
                string definition;
                if (typeDefinitions.TryGetValue(type, out definition))
                {
                    resultBuilder.Append(definition);
                }
                else if (false == type.Assembly.Equals(Assembly.GetAssembly(typeof(string))))
                {
                    resultBuilder.AppendLine("// unknown: " + type.Name + "\n");
                }
            }

            return resultBuilder.ToString();
        }

        protected void GenerateTypeDefinitions(string closureNamespaceVar, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph, Dictionary<Type, string> typeDefinitions)
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
                    GenerateTypeDefinition(closureNamespaceVar, graph, typeDefinitions, type);
                }
            }
        }

        private bool InAllowedAssemblies(Type type)
        {
            return this._assemblies.Any(x => x.Equals(type.Assembly));
        }

        protected void GenerateTypeDefinition(string closureNamespaceVar, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph, Dictionary<Type, string> typeDefinitions, Type type)
        {
            if (type.IsClass)
            {
                var typeResultBuilder = GenerateClassDefinition(type, graph);
                typeDefinitions.Add(type, typeResultBuilder.ToString());
            }

            if (type.IsEnum)
            {
                var typeResultBuilder = GenerateEnumDefinition(type, graph);
                typeDefinitions.Add(type, typeResultBuilder.ToString());
            }
        }

        protected Type MapType(Type type)
        {
            return this._options.MapType(type);
        }

        protected StringBuilder GenerateClassDefinition(Type type, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph)
        {
            Console.Error.WriteLine("Generating Class: " + type.Name);
            var typeResultBuilder = new StringBuilder();
            var className = this.GetFullTypeName(type);
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
                var isLast = propertyPair.Item2;
                string propertyName = property.Name.Substring(0, 1).ToLowerInvariant() + property.Name.Substring(1);
                var mappedType = MapType(property.PropertyType);
                var jsTypeName = GetJSTypeName(type, mappedType, graph);
                typeResultBuilder.AppendLine(String.Format("/** @type {{{0}}} */", jsTypeName));
                typeResultBuilder.AppendLine(String.Format("{0}.prototype.{1} = {2};", className, propertyName, GetDefaultJSValue(mappedType)));
            }

            typeResultBuilder.AppendLine();
            return typeResultBuilder;
        }

        private object GetDefaultJSValue(Type type)
        {
            if (type.IsGenericType && IsGenericTypeNullable(type))
            {
                return "null";
            }
            if (type.Equals(typeof(string)))
            {
                return "''";
            }
            if (type.Equals(typeof(bool)))
            {
                return "false";
            }
            if (type.IsEnum)
            {
                return this.GetFullTypeName(type) + "." + Enum.GetName(type, Activator.CreateInstance(type));
            }
            if (type.IsValueType && (false == type.Equals(typeof(DateTime))))
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

        protected string GetJSTypeName(Type sourceType, Type propertyType, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph)
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
                        return "?" + GetJSTypeName(sourceType, Nullable.GetUnderlyingType(propertyType), graph);
                    }
                    var dictionaryType = propertyType.GetInterfaces().SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition().Equals(typeof(IDictionary<,>)));
                    if (null != dictionaryType)
                    {
                        var typeArgs = dictionaryType.GetGenericArguments().ToArray();
                        var keyType = typeArgs[0];
                        var valueType = typeArgs[1];
                        return GetJSObjectTypeName(sourceType, keyType, valueType, graph);
                    }
                    var enumerableType = propertyType.GetInterfaces().SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)));
                    if (null != enumerableType)
                    {
                        return GetJSArrayTypeName(sourceType, enumerableType.GetGenericArguments().Single(), graph);
                    }
                }
                if (propertyType.IsArray)
                {
                    var elementType = propertyType.GetElementType();
                    return GetJSArrayTypeName(sourceType, elementType, graph);
                }
            }
            graph.AddVertex(propertyType);
            graph.AddVertex(sourceType);
            if (InAllowedAssemblies(propertyType))
            {
                graph.AddEdge(new QuickGraph.Edge<Type>(sourceType, propertyType));
                return this.GetFullTypeName(propertyType);
            }
            return overrideTypeName ?? GetTypeName(propertyType);
        }

        private static bool IsGenericTypeNullable(Type propertyType)
        {
            return propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private string GetJSArrayTypeName(Type sourceType, Type propertyType, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph)
        {
            return "Array.<" + GetJSTypeName(sourceType, propertyType, graph) + ">";
        }

        private string GetJSObjectTypeName(Type sourceType, Type keyType, Type valueType, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph)
        {
            return "Object.<" + GetJSTypeName(sourceType, keyType, graph) + ", " + GetJSTypeName(sourceType, valueType, graph) + ">";
        }

        protected string GetTypeName(Type type)
        {
            // Strip `1 (or `2, etc.) suffix from generic types
            var typeName = new String(type.Name.TakeWhile(Char.IsLetterOrDigit).ToArray());
            if (InAllowedAssemblies(type))
            {
                if (false == String.IsNullOrWhiteSpace(this._options.SuffixToTrimFromTypeNames))
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

        protected StringBuilder GenerateEnumDefinition(Type type, QuickGraph.AdjacencyGraph<Type, QuickGraph.Edge<Type>> graph)
        {
            Console.Error.WriteLine("Generating Enum: " + type.Name);
            var typeResultBuilder = new StringBuilder();
            AppendTypeComment(typeResultBuilder, type);
            typeResultBuilder.AppendLine("/** @enum {string} */");
            typeResultBuilder.AppendLine(GetFullTypeName(type) + " = {");
            foreach (var pair in WithIsLast(Enum.GetNames(type)))
            {
                typeResultBuilder.AppendFormat("    {0}: '{0}'{1}\n", pair.Item1, pair.Item2 ? String.Empty : ",");
            }
            typeResultBuilder.AppendLine("};");
            typeResultBuilder.AppendLine();
            return typeResultBuilder;
        }

        private string GetFullTypeName(Type type)
        {
            var overrideTypeName = this._options.TryGetTypeName(type);
            return this._options.NamespaceVarName + "." + (overrideTypeName ?? GetTypeName(type));
        }

        #endregion
    }
}
