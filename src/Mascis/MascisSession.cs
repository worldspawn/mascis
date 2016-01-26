using System.Collections.Generic;
using System.Data;
using Castle.DynamicProxy;

namespace Mascis
{
    public class MascisSession
    {
        private readonly ProxyGenerator _generator;
        private readonly FooInterceptor _interceptor;
        private readonly Dictionary<object, EntityChangeTracker> _attachedObjects;
        public MascisFactory Factory { get; }

        public IDbConnection DbConnection { get; }//TODO:

        public MascisSession(MascisFactory factory, ProxyGenerator generator)
        {
            _generator = generator;
            Factory = factory;

            _attachedObjects = new Dictionary<object, EntityChangeTracker>();
            _interceptor = new FooInterceptor(this, _attachedObjects);
        }

        private void Attach<T>(T entity)
        {
            _attachedObjects.Add(entity, new EntityChangeTracker(entity));
        }

        public T Create<T>()
            where T: class
        {
            var proxy = _generator.CreateClassProxy<T>(_interceptor);
            Attach(proxy);
            return proxy;
        }

        public Query<TEntity> Query<TEntity>()
        {
            return Mascis.Query<TEntity>.From(this);
        } 
        
    }
}