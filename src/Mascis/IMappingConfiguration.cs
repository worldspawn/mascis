using System;
using System.Reflection;

namespace Mascis
{
    public interface IMappingConfiguration
    {
        bool IsEntity(Type type);
        bool IsMap(PropertyInfo propertyInfo);
        bool IsKey(PropertyInfo propertyInfo);
        void OnEntity(EntityMappingConfiguration mapping);
        void OnMap(MapMappingConfiguration mapping);
        void OnKey(KeyMappingConfiguration mapping);
    }
}