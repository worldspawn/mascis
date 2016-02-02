using Castle.DynamicProxy;
using Mascis.Configuration;

namespace Mascis
{
    public class MascisFactory
    {
        public MascisFactory(Mapping mappings, string connectionString)
        {
            ConnectionString = connectionString;
            Mappings = mappings;
            Generator = new ProxyGenerator();
        }

        internal ProxyGenerator Generator { get; }

        public string ConnectionString { get; }

        public Mapping Mappings { get; }

        public MascisSession Start()
        {
            return new MascisSession(this);
        }
    }
}