using ClosureExterns.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClosureExterns.MetadataHandlers
{
    public abstract class MetadataHandlerBase<T> : IMetadataHandler
        where T : ValidationAttribute
    {
        public KeyValuePair<string, string> CreateMetadata(ValidationAttribute attribute)
        {
            return this.CreateMetadataImpl((T)attribute);
        }

        public string GetAttributeName(Attribute attribute)
        {
            return attribute.GetType().Name.TrimEnd(nameof(Attribute).ToCharArray()).ToCamelCase();
        }

        protected abstract KeyValuePair<string, string> CreateMetadataImpl(T attribute);
    }
}
