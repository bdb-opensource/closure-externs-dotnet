using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClosureExterns.MetadataHandlers
{
    public class MaxLengthMetadataHandler : MetadataHandlerBase<MaxLengthAttribute>
    {
        /// <summary>
        /// Generates:
        /// {
        ///     maxLength: 15
        /// }
        /// </summary>
        protected override KeyValuePair<string, string> CreateMetadataImpl(MaxLengthAttribute attribute)
        {
            return new KeyValuePair<string, string>(this.GetAttributeName(attribute), attribute.Length.ToString());
        }
    }
}
