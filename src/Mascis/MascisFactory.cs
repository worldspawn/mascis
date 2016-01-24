using Castle.DynamicProxy;

namespace Mascis
{
    public class MascisFactory
    {
        private readonly ProxyGenerator _generator = new ProxyGenerator();
        public MascisFactory(Mapping mappings)
        {
            Mappings = mappings;
        }

        public Mapping Mappings { get; }

        public MascisSession Start()
        {
            return new MascisSession(this, _generator);
        }
    }
}