using System.Reflection;
using Mascis.Configuration;

namespace Mascis.Tests
{
    public class FooMappingConfiguration : DefaultMappingConfiguration
    {
        public override bool IsKey(PropertyInfo propertyInfo)
        {
            if (propertyInfo.Name == "Id")
            {
                return true;
            }

            return false;
        }
    }
}