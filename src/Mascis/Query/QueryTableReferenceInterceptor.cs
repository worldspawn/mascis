using System.Reflection;
using Castle.DynamicProxy;

namespace Mascis.Query
{
    public class QueryTableReferenceInterceptor : IInterceptor
    {
        private static readonly MethodInfo Getter;
        private readonly QueryTable _queryTable;

        static QueryTableReferenceInterceptor()
        {
            Getter = typeof (IQueryTableReference).GetProperty("QueryTable").GetMethod;
        }

        public QueryTableReferenceInterceptor(QueryTable queryTable)
        {
            _queryTable = queryTable;
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method == Getter)
            {
                invocation.ReturnValue = _queryTable;
            }
        }
    }
}