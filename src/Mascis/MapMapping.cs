using System.Reflection;

namespace Mascis
{
    public class MapMapping
    {
        public MapMapping(MapMappingConfiguration configuration)
        {
            ColumnName = configuration.ColumnName;
            IsReadOnly = configuration.IsReadOnly;
            Property = configuration.Property;
        }

        public PropertyInfo Property { get; set; }

        public bool IsReadOnly { get; set; }

        public string ColumnName { get; }
    }
}