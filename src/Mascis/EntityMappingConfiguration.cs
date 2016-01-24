using System;

namespace Mascis
{
    public class EntityMappingConfiguration
    {
        public EntityMappingConfiguration(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
        public string TableName { get; set; }
        public bool IsReadOnly { get; set; }
    }
}