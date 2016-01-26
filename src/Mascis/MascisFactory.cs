using Castle.DynamicProxy;

namespace Mascis
{
    public class MascisFactory
    {
        public string ConnectionString { get; }
        private readonly ProxyGenerator _generator = new ProxyGenerator();
        public MascisFactory(Mapping mappings, string connectionString)
        {
            ConnectionString = connectionString;
            Mappings = mappings;
        }

        public Mapping Mappings { get; }

        public MascisSession Start()
        {
            return new MascisSession(this, _generator);
        }
    }
}