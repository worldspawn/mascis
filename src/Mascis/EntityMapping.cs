using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mascis
{
    public class EntityMapping
    {
        private readonly EntityMappingConfiguration _configuration;

        public EntityMapping(EntityMappingConfiguration configuration, IEnumerable<MapMappingConfiguration> maps, KeyMappingConfiguration key)
        {
            var keyMaps = key.Keys.Select(x => new MapMapping(x)).ToArray();
            Maps = maps.Where(x=>!key.Keys.Contains(x)).Select(x => new MapMapping(x)).Union(keyMaps).ToArray();
            Key = key;
            TableName = configuration.TableName;
            Type = configuration.Type;

            InterceptList =
                Maps.Select(x => x.Property.GetMethod)
                    .Union(Maps.Where(x => x.Property.CanWrite).Select(x => x.Property.SetMethod))
                    .ToArray();
            InterceptDictionary = Maps.Select(x => new {Method = x.Property.GetMethod, x})
                .Union(Maps.Where(x => x.Property.CanWrite).Select(x => new {Method = x.Property.SetMethod, x}))
                .ToDictionary(x => x.Method, x => x.x);
            InterceptPropertyDictionary = Maps.ToDictionary(x => x.Property, x => x);
        }

        public Dictionary<PropertyInfo, MapMapping> InterceptPropertyDictionary { get; }

        public Dictionary<MethodInfo, MapMapping> InterceptDictionary { get; }

        public MethodInfo[] InterceptList { get; }

        public Type Type { get; }

        public string TableName { get; }

        public KeyMappingConfiguration Key { get; }

        public MapMapping[] Maps { get; }
    }
}