using System;
using System.Collections.Generic;
using System.Linq;

namespace ClosureExterns
{
    /// <summary>
    /// Represents a set of options to use when generating closure annotations (externs).
    /// The default constructor initializes all properties to default values.
    /// <para>See each property's doc for details and default values.</para>
    /// </summary>
    public class ClosureExternsOptions
    {
        public ClosureExternsOptions()
        {
            this.NamespaceVarName = "Types";
            this.NamespaceDefinitionExpression = x => String.Format("/** @const */" + Environment.NewLine + "var {0} = {{}};", x);
            this.ConstructorAnnotations = x => new string[0];
            this.SuffixToTrimFromTypeNames = "dto";
            this.ConstructorExpression = x => "function() {}";
            this.MapType = x => x;
            this.TryGetTypeName = x => null;
            this.TryGetPropertyName = (x, y) => null;
            this.TryGetDefaultJSValue = x => null;
        }

        /// <summary>
        /// The name to use for the javascript variable which will contain all the types.
        /// <para>Default is: "Types"</para>
        /// </summary>
        public string NamespaceVarName { get; set; }

        /// <summary>
        /// An expression to define the namespace var. Default is: x => String.Format("var {0};", x);
        /// </summary>
        public Func<string, string> NamespaceDefinitionExpression { get; set; }

        /// <summary>
        /// An expression to use for when defining constructors. Useful for creating derived types.
        /// <para>The parameter is the closure type name of the current class (so you can use it in the output you generate)</para>
        /// <para>Default is: x => "function() {}"</para>
        /// </summary>
        public Func<string, string> ConstructorExpression { get; set; }

        /// <summary>
        /// Additional annotations to apply to constructors. For example: ["@struct", "@final"]
        /// <para>The parameter is the closure type name of the current class (so you can use it in the output you generate)</para>
        /// <para>Default is: [] (empty array)</para>
        /// </summary>
        public Func<string, string[]> ConstructorAnnotations { get; set; }

        /// <summary>
        /// A string that should be trimmed from the end of all type names, case-insensitive. 
        /// <para>(e.g. "dto" will caused "MyDTO" to be trimmed to "My")</para>
        /// <para>Default is: "dto"</para>
        /// </summary>
        public string SuffixToTrimFromTypeNames { get; set; }

        /// <summary>
        /// A function to map input types to actual types to be outputted. Useful for converting some property types of an object
        /// into something else (e.g. your special ID type into a string) that represents how it will actually be serialized to 
        /// json.
        /// <para>Default is: x => x</para>
        /// </summary>
        public Func<Type, Type> MapType { get; set; }

        /// <summary>
        /// A function that optionally generates a specific closure type expression for the given type.
        /// <para>Returns null to signify that there is no special substitution, and that the generator should
        /// find the appropriate type expression.</para>
        /// <para>Default is: x => null</para>
        /// </summary>
        public Func<Type, string> TryGetTypeName { get; set; }

        /// <summary>
        /// A function that optionally generates a specific name for the given property.
        /// <para>Returns null to signify that there is no special substitution, and that the generator should
        /// generate a name based on the default behavior.</para>
        /// </summary>
        public Func<Type, string, string> TryGetPropertyName { get; set; }

        /// <summary>
        /// A function that optionally generates a default JS value for a specific type
        /// <para>Returns null to signify that there is no special substitution, and that the generator should
        /// generate a value based on the default behavior.</para>
        /// </summary>
        public Func<Type, string> TryGetDefaultJSValue { get; set; }
    }
}
