using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mascis.Query
{
    public class ExpressionParser
    {
        private readonly List<QueryTree.ConstantExpression> _constantExpressions =
            new List<QueryTree.ConstantExpression>();

        private readonly MethodInfo _mapValue = typeof (QueryMap).GetMethod("Value",
            BindingFlags.Instance | BindingFlags.Public);

        private readonly IMascisSession _session;

        private readonly MethodInfo _stringContains = typeof (string).GetMethod("Contains",
            BindingFlags.Instance | BindingFlags.Public);

        public ExpressionParser(IMascisSession session)
        {
            _session = session;
        }

        public QueryTree.Expression Parse<T>(Expression<Func<T>> expression)
        {
            return ParseExpression(expression.Body);
        }

        //public QueryTree.Expression Parse(Expression<Func<object>> expression)
        //{
        //    return ParseExpression(expression.Body);
        //}

        public QueryTree.Expression Parse(Expression<Func<bool>> expression)
        {
            return ParseExpression(expression.Body);
        }

        public QueryTree.ConstantExpression[] GetConstantExpressionsAndClear()
        {
            try
            {
                return _constantExpressions.ToArray();
            }
            finally
            {
                _constantExpressions.Clear();
            }
        }

        private QueryTree.Expression ParseMethodCallExpression(MethodCallExpression expression)
        {
            if (expression.Method == _stringContains)
            {
                var searchFor = ParseExpression(expression.Arguments[0]);
                var source = ParseExpression(expression.Object);

                return new QueryTree.FunctionExpression
                {
                    Name = "CHARINDEXOF",
                    Arguments = new[]
                    {
                        source,
                        searchFor
                    }
                };
            }

            if (expression.Method.IsGenericMethod && expression.Method.GetGenericMethodDefinition() == _mapValue)
            {
                var map = ParseExpression(expression.Object) as QueryTree.ConstantExpression;
                var value = map.Value as QueryMap;

                return new QueryTree.AliasReferenceExpression
                {
                    Alias = value.Alias,
                    TableAlias = value.Table.Alias
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
                return ex;
            }

            var memberInitExpression = expression as MemberInitExpression;
            if (memberInitExpression != null)
            {
                var ex = ParseMemberInitExpression(memberInitExpression);
                return ex;
            }

            var newExpression = expression as NewExpression;
            if (newExpression != null)
            {
                var ex = ParseNewExpression(newExpression);
                return ex;
            }

            throw new UnknownExpressionException();
        }

        private QueryTree.ProjectionAliasesExpression ParseNewExpression(NewExpression expression)
        {
            var index = 0;
            var aliasIndex = 0;
            var arguments = expression.Arguments.Select(x => new QueryTree.ProjectionConstructorArgumentExpression
            {
                Index = index++,
                Expression = new QueryTree.AliasedExpression { Expression = ParseExpression(x), Alias = "proj_" + aliasIndex++ }
            }).ToList();

            return new QueryTree.ProjectionAliasesExpression
            {
                Constructor = expression.Constructor,
                ConstructorArguments = arguments,
                MemberAssignments = new List<QueryTree.ProjectionMemberAssignmentExpression>()
            };
        }

        private QueryTree.ProjectionAliasesExpression ParseMemberInitExpression(MemberInitExpression expression)
        {
            var index = 0;
            var aliasIndex = 0;
            var arguments = expression.NewExpression.Arguments.Select(x=> new QueryTree.ProjectionConstructorArgumentExpression
            {
                Index = index++,
                Expression = new QueryTree.AliasedExpression { Expression = ParseExpression(x), Alias = "proj_" + aliasIndex++}
            }).ToList();
            var bindings = expression.Bindings.Cast<MemberAssignment>().Select(x => 
                new QueryTree.ProjectionMemberAssignmentExpression
                {
                    Member = x.Member, Expression = new QueryTree.AliasedExpression {Expression = ParseExpression(x.Expression), Alias = "proj_" + aliasIndex++ }
                }).ToList();

            return new QueryTree.ProjectionAliasesExpression
            {
                Constructor = expression.NewExpression.Constructor,
                ConstructorArguments = arguments,
                MemberAssignments = bindings
            };
        }

        private QueryTree.ConstantExpression ParseConstantExpression(ConstantExpression expression)
        {
            return ParseConstantExpression(expression.Value);
        }

        private QueryTree.ConstantExpression ParseConstantExpression(object value)
        {
            var ex = new QueryTree.ConstantExpression
            {
                Value = value
            };

            _constantExpressions.Add(ex);

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

            _constantExpressions.Add(ex);

            return ex;
        }

        private object GetValueFromExpression(Expression expression)
        {
            var objectMember = Expression.Convert(expression, typeof (object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        private QueryTree.Expression ParseColumnExpression(MemberExpression expression)
        {
            var entityMapping = _session.Factory.Mappings.MappingsByType[expression.Member.DeclaringType];
            var property = (PropertyInfo) expression.Member;
            var propertyMap = entityMapping.InterceptPropertyDictionary[property];

            var fex = (Expression) expression;
            var refType = typeof (IQueryTableReference);

            while (true)
            {
                var fexType = GetTypeFromExpression(fex);
                if (_session.Factory.Mappings.MappingsByType.ContainsKey(fexType) || _session.Factory.Mappings.MappingsByType.ContainsKey(fexType.BaseType))
                {
                    var entity = GetValueFromExpression(fex);
                    if (refType.IsInstanceOfType(entity))
                    {
                        var qt = (IQueryTableReference) entity;
                        return new QueryTree.ColumnExpression
                        {
                            Column = propertyMap.ColumnName,
                            TableAlias = qt.QueryTable.Alias
                        };
                    }

                    return ParseConstantExpression(GetValueFromExpression(expression));
                }

                fex = ((MemberExpression) fex).Expression;
            }
        }

        private static Type GetTypeFromExpression(Expression expression)
        {
            if (expression is MemberExpression)
            {
                return ((MemberExpression) expression).Type;
            }

            if (expression is ConstantExpression)
            {
                return ((ConstantExpression) expression).Type;
            }

            return null;
        }

        private QueryTree.Expression ParseMemberExpression(MemberExpression expression)
        {
            var startedWith = expression;
            while (!_session.Factory.Mappings.MappingsByType.ContainsKey(expression.Member.DeclaringType))
            {
                expression = expression.Expression as MemberExpression;
                if (expression == null)
                {
                    var objectMember = Expression.Convert(startedWith, typeof (object));
                    var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                    var getter = getterLambda.Compile();
                    var value = getter();

                    var ex = new QueryTree.ConstantExpression
                    {
                        Value = value
                    };
                    _constantExpressions.Add(ex);

                    return ex;
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
                case ExpressionType.GreaterThan:
                    return QueryTree.BooleanOperator.GreaterThan;
                case ExpressionType.GreaterThanOrEqual:
                    return QueryTree.BooleanOperator.GreaterThanOrEqualTo;
                case ExpressionType.LessThan:
                    return QueryTree.BooleanOperator.LessThan;
                case ExpressionType.LessThanOrEqual:
                    return QueryTree.BooleanOperator.LessThanOrEqualTo;
                default:
                    throw new UnknownNodeTypeException();
            }
        }
    }
}