using System;

namespace Mascis.Tests
{
    public class OrderItem
    {
        public virtual Guid Id { get; set; }
        public virtual Guid OrderId { get; set; }
        public virtual string Product { get; set; }
        public virtual int Amount { get; set; }
    }
}