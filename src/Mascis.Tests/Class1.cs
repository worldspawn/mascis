using System;
using Mascis.Configuration;
using Mascis.Query;

namespace Mascis.Tests
{

    public class Zap
    {
        public string Foo { get; set; }
    }
    public class Class1
    {
        public Class1()
        {
            var mapper = new Mapper(new[] {typeof (Order), typeof (OrderItem)});
            mapper.MappingConfiguration = new FooMappingConfiguration();
            var mappings = mapper.Build();
            var connectionString = "data source=.;integrated security=sspi;initial catalog=mascis";
            var mascisFactory = new MascisFactory(mappings, connectionString);
            var mascisSession = mascisFactory.Start();

            var o = new Order
            {
                Id = Guid.NewGuid()
            };

            var q = mascisSession.Query<Order>();
            var f = q.FromTable.Ex;
            //var q1 = q.CreateTable<OrderItem>((oi) => f.Id == oi.OrderId);
            var q1 = q.CreateTable<OrderItem>(oi => o.Id == oi.OrderId || f.Id == oi.OrderId);
            //q.FromTable.Join(q1, () => q.FromTable.Ex.Id == q1.Ex.OrderId);

            var p = q.Project(() => new
            {
                Test = f.Name + "ggg"
            });

            var p2 = q.Project(() => new Zap
            {
                Foo = f.Name + "ggg"
            });

            var xx = mascisSession.Execute(p);
            var xxx = mascisSession.Execute(p2);

            var list = q.Execute();
            list[0].Name = "Zong";

            var myFoo = new Order();
            myFoo.Name = "La bamba";
            myFoo.Id = Guid.NewGuid();

            mascisSession.Save(myFoo);
            mascisSession.SaveChanges();

            ToString();
        }
    }
}