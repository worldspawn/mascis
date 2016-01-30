using Mascis.Configuration;

namespace Mascis
{
    public class MascisFactory
    {
        public MascisFactory(Mapping mappings, string connectionString)
        {
            ConnectionString = connectionString;
            Mappings = mappings;
        }

        public string ConnectionString { get; }

        public Mapping Mappings { get; }

        public MascisSession Start()
        {
            return new MascisSession(this);
        }
    }
}