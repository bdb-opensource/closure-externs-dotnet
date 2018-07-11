using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClosureExterns.MetadataHandlers
{
    public interface IMetadataHandler
    {
        KeyValuePair<string, string> CreateMetadata(ValidationAttribute attribute);
    }
}
