using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mascis
{
    public class BooleanExpressionParser
    {
        private readonly MascisSession _session;

        public BooleanExpressionParser(MascisSession session)
        {
            _session = session;
        }

        public QueryTree.Expression Parse(Expression<Func<object>> expression)
        {
            return ParseExpression(expression.Body);
        }

        public QueryTree.Expression Parse(Expression<Func<bool>> expression)
        {
            return ParseExpression(expression.Body);
        }

        private QueryTree.Expression ParseMethodCallExpression(MethodCallExpression expression)
        {
            var stringContains = typeof (String).GetMethod("Contains", BindingFlags.Instance | BindingFlags.Public);
            var mapValue = typeof (QueryMap).GetMethod("Value", BindingFlags.Instance | BindingFlags.Public);

            if (expression.Method == stringContains)
            {
                var searchFor = ParseExpression(expression.Arguments[0]);
                var source = ParseExpression(expression.Object);

                return new QueryTree.FunctionExpression()
                {
                    Name = "CHARINDEXOF",
                    Arguments = new[]
                    {
                        source,
                        searchFor
                    }
                };
            }

            if (expression.Method.IsGenericMethod && expression.Method.GetGenericMethodDefinition() == mapValue)
            {
                var map = ParseExpression(expression.Object) as QueryTree.ConstantExpression;
                var value = map.Value as QueryMap;

                return new QueryTree.AliasReference
                {
                    Alias = value.Alias,
                    Table = value.Table
                };
            }

            throw new UnknownFunctionException();
        }

        private QueryTree.Expression ParseExpression(Expression expression)
        {
            var binaryExpression = expression as BinaryExpression;
            if (binaryExpression != null)
            {
                var ex = ParseBinaryExpression(binaryExpression);
                return ex;
            }

            var memberExpression = expression as MemberExpression;
            if (memberExpression != null)
            {
                if (memberExpression.Member is FieldInfo)
                {
                    var ex = ParseFieldExpression(memberExpression);
                    return ex;
                }
                if (memberExpression.Member is PropertyInfo)
                {
                    var ex = ParseMemberExpression(memberExpression);
                    return ex;
                }
                
            }

            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null)
            {
                var ex = ParseConstantExpression(constantExpression);
                return ex;
            }

            var methodExpression = expression as MethodCallExpression;
            if (methodExpression != null)
            {
                var ex = ParseMethodCallExpression(methodExpression);

                return ex; ;
            }

            throw new UnknownExpression();
        }

        private QueryTree.ConstantExpression ParseConstantExpression(ConstantExpression expression)
        {
            var ex = new QueryTree.ConstantExpression
            {
                Value = expression.Value
            };
            return ex;
        }

        private QueryTree.ConstantExpression ParseFieldExpression(MemberExpression expression)
        {
            var fieldInfo = expression.Member as FieldInfo;
            var constantExpression = expression.Expression as ConstantExpression;

            var ex = new QueryTree.ConstantExpression
            {
                Value = fieldInfo.GetValue(constantExpression.Value)
            };
            return ex;
        }

        private QueryTree.ColumnExpression ParseColumnExpression(MemberExpression expression)
        {
            var entityMapping = _session.Factory.Mappings.MappingsByType[expression.Member.DeclaringType];
            var property = (PropertyInfo)expression.Member;
            var propertyMap = entityMapping.InterceptPropertyDictionary[property];

            var fex = expression.Expression as MemberExpression;

            while (!fex.Type.IsGenericType || fex.Type.GetGenericTypeDefinition() != typeof(QueryTable<>))
            {
                fex = fex.Expression as MemberExpression;
            }

            var expressions = new List<MemberExpression>();
            var cex = fex.Expression as ConstantExpression;
            while (cex == null)
            {
                expressions.Add(fex);
                fex = fex.Expression as MemberExpression;
                cex = fex.Expression as ConstantExpression;
            }

            var fld = fex.Member as FieldInfo;
            var expressionWalker = fld.GetValue(cex.Value);
            expressions.Reverse();
            expressionWalker = expressions.Aggregate(expressionWalker, (current, ex1) => (ex1.Member as PropertyInfo).GetValue(current));

            var queryTable = (QueryTable)expressionWalker;
            var ex = new QueryTree.ColumnExpression
            {
                Table = queryTable,
                Column = propertyMap.ColumnName
            };

            return ex;
        }

        private QueryTree.Expression ParseMemberExpression(MemberExpression expression)
        {
            var startedWith = expression;
            while (!_session.Factory.Mappings.MappingsByType.ContainsKey(expression.Member.DeclaringType))
            {
                expression = expression.Expression as MemberExpression;
                if (expression == null)
                {
                    var value = ParseExpression(startedWith.Expression) as QueryTree.ConstantExpression;
                    var propertyInfo = (PropertyInfo) startedWith.Member;
                    return new QueryTree.ConstantExpression
                    {
                        Value = propertyInfo.GetValue(value.Value)
                    };
                }
            }

            return ParseColumnExpression(expression);
        }

        private QueryTree.BinaryExpression ParseBinaryExpression(BinaryExpression binaryExpression)
        {

            var ex = new QueryTree.BinaryExpression
            {
                Left = ParseExpression(binaryExpression.Left),
                Operator = MapNodeType(binaryExpression.NodeType),
                Right = ParseExpression(binaryExpression.Right)
            };

            return ex;
        }

        private QueryTree.BooleanOperator MapNodeType(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return QueryTree.BooleanOperator.And;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return QueryTree.BooleanOperator.Or;
                case ExpressionType.Equal:
                    return QueryTree.BooleanOperator.Equal;
                case ExpressionType.Add:
                    return QueryTree.BooleanOperator.Add;
                default:
                    throw new UnknownNodeType();
            }
        }
    }
}