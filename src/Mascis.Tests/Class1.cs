using System;
using Mascis.Configuration;
using Mascis.Query;

namespace Mascis.Tests
{
    public class Class1
    {
        public Class1()
        {
            var mapper = new Mapper(new [] {typeof(Order), typeof(OrderItem)});
            mapper.MappingConfiguration = new FooMappingConfiguration();
            var mappings = mapper.Build();
            var connectionString = "data source=.;integrated security=sspi;initial catalog=mascis";
            var mascisFactory = new MascisFactory(mappings, connectionString);
            var mascisSession = mascisFactory.Start();
            

            var q = mascisSession.Query<Order>();
            var q1 = q.CreateTable<OrderItem>();
            q.FromTable.Join(q1, () => q.FromTable.Ex.Id == q1.Ex.OrderId);

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
