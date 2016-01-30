using System.Reflection;

namespace Mascis.Configuration
{
    public class MapMappingConfiguration
    {
        public MapMappingConfiguration(PropertyInfo property)
        {
            Property = property;
        }

        public PropertyInfo Property { get; }
        public string ColumnName { get; set; }
        public bool IsReadOnly { get; set; }
    }
}