using ClosureExterns.Extensions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ClosureExterns.MetadataHandlers
{
    public class RequiredMetadataHandler : MetadataHandlerBase<RequiredAttribute>
    {
        /// <summary>
        /// Generates:
        /// {
        ///     required: true
        /// }
        /// ---- OR ----
        /// {
        ///     required: {
        ///         allowEmptyStrings: true
        ///     }
        /// }
        /// </summary>
        protected override KeyValuePair<string, string> CreateMetadataImpl(RequiredAttribute attribute)
        {
            var attributeName = this.GetAttributeName(attribute);

            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine($"{Consts.SPACING_8}{(nameof(attribute.AllowEmptyStrings)).ToCamelCase()}: {attribute.AllowEmptyStrings.ToString().ToLowerInvariant()},");
            builder.AppendLine($"{Consts.SPACING_8}{(nameof(attribute.ErrorMessage)).ToCamelCase()}: '{attribute.ErrorMessage}'");
            builder.Append($"{Consts.SPACING_4}}}");

            return new KeyValuePair<string, string>(attributeName, builder.ToString());
        }
    }
}
