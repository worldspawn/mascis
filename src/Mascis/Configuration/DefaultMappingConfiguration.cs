using System;
using System.Reflection;

namespace Mascis.Configuration
{
    public abstract class DefaultMappingConfiguration : IMappingConfiguration
    {
        public virtual bool IsEntity(Type type)
        {
            return type.IsClass && !type.IsAbstract && type.IsPublic;
        }

        public virtual bool IsMap(PropertyInfo propertyInfo)
        {
            return propertyInfo.CanRead && propertyInfo.GetMethod.IsVirtual && propertyInfo.GetMethod.IsPublic &&
                   !propertyInfo.GetMethod.IsAbstract;
        }

        public abstract bool IsKey(PropertyInfo propertyInfo);

        public virtual void OnEntity(EntityMappingConfiguration mapping)
        {
            mapping.TableName = mapping.Type.Name;
        }

        public virtual void OnMap(MapMappingConfiguration mapping)
        {
            mapping.ColumnName = mapping.Property.Name;
            mapping.IsReadOnly = mapping.Property.CanWrite;
        }

        public virtual void OnKey(KeyMappingConfiguration mapping)
        {
        }
    }
}