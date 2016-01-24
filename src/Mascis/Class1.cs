using System;
using System.CodeDom;
using System.Configuration;
using System.Linq.Expressions;
using System.Runtime.Remoting;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;

namespace Mascis
{
    

    public class Class1
    {
        public Class1()
        {
            var mapper = new Mapper(new [] {typeof(Foo)});
            mapper.MappingConfiguration = new FooMappingConfiguration();
            var mappings = mapper.Build();
            var mascisFactory = new MascisFactory(mappings);
            var mascisSession = mascisFactory.Start();
            var f = mascisSession.Create<Foo>();

            f.Test = "Zing";

            var q = mascisSession.Query<Foo>();
            var q1 = q.CreateTable<Foo>();
            var m = q1.Map(() => q1.Ex.Test + "FFF");
            q.FromTable.Join(q1, () => q.FromTable.Ex.Test == m.Value<string>());

            var boom = new
            {
                Thing = new
                {
                    Foo = "Ferret"
                }
            };

            //q.Map(() => q.FromTable.Ex.Test + " 666");

            var x = "Zombie";
            q.Where(() => q1.Ex.Test == "Foo");
            q.Where(() => q1.Ex.Test == x);
            q.Where(() => q1.Ex.Test.Contains(x));
            q.Where(() => x.Contains(q1.Ex.Test));
            q.Where(() => true);
            q.Where(() => q1.Ex.Test == boom.Thing.Foo);
            q.Where(() => q1.Ex.Test.Contains("zing" + "zoom"));
            q.Where(() => q1.Ex.Test.Contains(q.FromTable.Ex.Test + "zoom"));
            q.Where(() => q1.Ex.Test.Contains(q.FromTable.Ex.Test + "zap" + " " + "zoom"));

            var qp = new QueryParser(mascisSession);
            var s = qp.Parse(q);


            ToString();
        }
    }
}
