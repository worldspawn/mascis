using System;
using System.Collections.Generic;
using System.Linq;

namespace Mascis
{
    public class Mapping
    {
        public Mapping(IEnumerable<EntityMapping> mappings)
        {
            MappingsByType = mappings.ToDictionary(m => m.Type);
        }

        public Dictionary<Type, EntityMapping> MappingsByType { get; }
    }
}