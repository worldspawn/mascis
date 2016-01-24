using System.Collections.Generic;
using Castle.DynamicProxy;

namespace Mascis
{
    public class FooInterceptor : IInterceptor
    {
        private readonly MascisSession _session;
        private readonly Dictionary<object, EntityChangeTracker> _attachedObjects;

        public FooInterceptor(MascisSession session, Dictionary<object, EntityChangeTracker> attachedObjects)
        {
            _session = session;
            _attachedObjects = attachedObjects;
        }

        public void Intercept(IInvocation invocation)
        {
            var mapping = _session.Factory.Mappings.MappingsByType[invocation.TargetType];
            if (mapping != null && mapping.InterceptDictionary.ContainsKey(invocation.Method))
            {
                var map = mapping.InterceptDictionary[invocation.Method];
                var isWrite = map.Property.CanWrite && map.Property.SetMethod == invocation.Method;
                if (isWrite)
                {
                    var ct = _attachedObjects[invocation.Proxy];
                    if (ct != null)
                    {
                        object originalValue = null;
                        if (!ct.HasChangeFor(map))
                        {
                            originalValue = map.Property.GetMethod.Invoke(invocation.InvocationTarget, null);
                        }
                        ct.MapChanged(map, originalValue, invocation.Arguments[0]);
                    }
                }
            }
            
            invocation.Proceed();
        }
    }
}