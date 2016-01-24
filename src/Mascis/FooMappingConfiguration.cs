using System.Reflection;

namespace Mascis
{
    public class FooMappingConfiguration : DefaultMappingConfiguration
    {
        public override bool IsKey(PropertyInfo propertyInfo)
        {
            return false;
        }
    }
}