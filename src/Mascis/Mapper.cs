using System;
using System.Collections.Generic;
using System.Linq;

namespace Mascis
{
    public class Mapper
    {
        private readonly IEnumerable<Type> _types;
        public Mapper(IEnumerable<Type> types)
        {
            _types = types;
        }

        public IMappingConfiguration MappingConfiguration { get; set; }

        public Mapping Build()
        {
            return new Mapping(Process());
        }

        private IEnumerable<EntityMapping> Process()
        {
            var entityMaps = WalkTypes(_types);
            foreach (var em in entityMaps)
            {
                MappingConfiguration.OnEntity(em);
                var maps = WalkType(em.Type).ToList();
                foreach (var m in maps)
                {
                    MappingConfiguration.OnMap(m);
                }
                var key = WalkKeys(maps);
                MappingConfiguration.OnKey(key);

                yield return new EntityMapping(em, maps, key);
            }
        }

        private IEnumerable<EntityMappingConfiguration> WalkTypes(IEnumerable<Type> types)
        {
            return from type in types
                where MappingConfiguration.IsEntity(type)
                select new EntityMappingConfiguration(type);
        }

        private IEnumerable<MapMappingConfiguration> WalkType(Type type)
        {
            return from property in type.GetProperties()
                where MappingConfiguration.IsMap(property)
                select new MapMappingConfiguration(property);
        }

        private KeyMappingConfiguration WalkKeys(IEnumerable<MapMappingConfiguration> maps)
        {
            return new KeyMappingConfiguration
            {
                Keys = from map in maps
                    where MappingConfiguration.IsKey(map.Property)
                    select map
            };
        } 
    }
}