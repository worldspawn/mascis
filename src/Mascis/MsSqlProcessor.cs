using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Mascis.Query;

namespace Mascis
{
    public class MsSqlProcessor : IExpressionParser
    {
        private readonly Dictionary<Type, IExpressionParser> _parsers;

        public MsSqlProcessor()
        {
            _parsers = new Dictionary<Type, IExpressionParser>
            {
                {typeof (QueryTree.ConstantExpression), new ConstantExpressionParser()},
                {typeof (QueryTree.AliasedExpression), new AliasedExpressionParser(this)},
                {typeof (QueryTree.SelectExpression), new SelectExpressionParser(this)},
                {typeof (QueryTree.BinaryExpression), new BinaryExpressionParser(this)},
                {typeof (QueryTree.AliasReferenceExpression), new AliasReferenceExpressionParser()},
                {typeof (QueryTree.ColumnExpression), new ColumnExpressionParser()},
                {typeof (QueryTree.FromExpression), new FromExpressionParser(this)},
                {typeof (QueryTree.TableExpression), new TableExpressionParser()},
                {typeof (QueryTree.TableAliasExpression), new TableAliasExpressionParser()},
                {typeof (QueryTree.JoinExpression), new JoinExpressionParser(this)},
                {typeof (QueryTree.FunctionExpression), new FunctionExpressionParser(this)},
                {typeof (QueryTree.InsertExpression), new InsertExpressionParser(this)},
                {typeof (QueryTree.UnAliasedColumnExpression), new UnAliasedColumnExpressionParser()},
                {typeof (QueryTree.UnAliasedTableExpression), new UnAliasedTableExpressionParser()},
                {typeof (QueryTree.ValueGroupExpression), new ValueGroupExpressionParser(this)},
                {typeof (QueryTree.UpdateExpression), new UpdateExpressionParser(this)}
            };
        }

        public string ParseExpression(QueryTree.Expression expression)
        {
            var parser = _parsers[expression.GetType()];
            return parser.ParseExpression(expression);
        }

        public IDbCommand Process(ParsedQuery queryPlan, MascisSession session)
        {
            var queryText = ParseExpression(queryPlan.Expression);

            var command = new SqlCommand(queryText);
            command.CommandType = CommandType.Text;
            foreach (var parameter in queryPlan.Parameters)
            {
                command.Parameters.Add(new SqlParameter
                {
                    Value = parameter.Value,
                    ParameterName = parameter.ParameterName
                });
            }

            return command;
        }

        public class FunctionExpressionParser : IExpressionParser
        {
            private readonly IExpressionParser _parser;

            public FunctionExpressionParser(IExpressionParser parser)
            {
                _parser = parser;
            }

            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.FunctionExpression);
            }

            public string ParseExpression(QueryTree.FunctionExpression expression)
            {
                switch (expression.Name.ToLower())
                {
                    case "charindexof":
                        return
                            $"(CHARINDEXOF({_parser.ParseExpression(expression.Arguments[0])}, {_parser.ParseExpression(expression.Arguments[1])}) > -1)";
                }

                throw new UnknownFunctionException();
            }
        }

        public class JoinExpressionParser : IExpressionParser
        {
            private readonly IExpressionParser _parser;

            public JoinExpressionParser(IExpressionParser parser)
            {
                _parser = parser;
            }

            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.JoinExpression);
            }

            public string ParseExpression(QueryTree.JoinExpression expression)
            {
                var includeAlias = expression.Table is QueryTree.SelectExpression;
                var alias = includeAlias ? _parser.ParseExpression(expression.Alias) : null;
                return
                    $"JOIN {_parser.ParseExpression(expression.Table)} {alias} ON {_parser.ParseExpression(expression.On)} ";
            }
        }

        public class TableAliasExpressionParser : IExpressionParser
        {
            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.TableAliasExpression);
            }

            public string ParseExpression(QueryTree.TableAliasExpression expression)
            {
                return $"[{expression.Alias}]";
            }
        }

        public class TableExpressionParser : IExpressionParser
        {
            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.TableExpression);
            }

            public string ParseExpression(QueryTree.TableExpression expression)
            {
                return $"[{expression.Table}] [{expression.TableAlias}]";
            }
        }

        public class FromExpressionParser : IExpressionParser
        {
            private readonly IExpressionParser _parser;

            public FromExpressionParser(IExpressionParser parser)
            {
                _parser = parser;
            }

            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.FromExpression);
            }

            public string ParseExpression(QueryTree.FromExpression expression)
            {
                return $"FROM {_parser.ParseExpression(expression.Table)} ";
            }
        }

        public class ColumnExpressionParser : IExpressionParser
        {
            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.ColumnExpression);
            }

            public string ParseExpression(QueryTree.ColumnExpression expression)
            {
                return $"[{expression.TableAlias}].[{expression.Column}]";
            }
        }

        public class AliasReferenceExpressionParser : IExpressionParser
        {
            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.AliasReferenceExpression);
            }

            public string ParseExpression(QueryTree.AliasReferenceExpression expression)
            {
                return $"[{expression.TableAlias}].[{expression.Alias}]";
            }
        }

        public class BinaryExpressionParser : IExpressionParser
        {
            private readonly IExpressionParser _parser;

            public BinaryExpressionParser(IExpressionParser parser)
            {
                _parser = parser;
            }

            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.BinaryExpression);
            }

            private string ParseExpression(QueryTree.BinaryExpression expression)
            {
                string op;
                switch (expression.Operator)
                {
                    case QueryTree.BooleanOperator.Add:
                        op = " + ";
                        break;
                    case QueryTree.BooleanOperator.And:
                        op = " AND ";
                        break;
                    case QueryTree.BooleanOperator.Equal:
                        op = " = ";
                        break;
                    case QueryTree.BooleanOperator.GreaterThan:
                        op = " > ";
                        break;
                    case QueryTree.BooleanOperator.GreaterThanOrEqualTo:
                        op = " >= ";
                        break;
                    case QueryTree.BooleanOperator.LessThan:
                        op = " < ";
                        break;
                    case QueryTree.BooleanOperator.LessThanOrEqualTo:
                        op = " <= ";
                        break;
                    case QueryTree.BooleanOperator.Or:
                        op = " OR ";
                        break;
                    default:
                        throw new UnknownOperatorException();
                }
                return $"({_parser.ParseExpression(expression.Left)} {op} {_parser.ParseExpression(expression.Right)})";
            }
        }

        public class UpdateExpressionParser : IExpressionParser
        {
            private readonly IExpressionParser _parser;

            public UpdateExpressionParser(IExpressionParser parser)
            {
                _parser = parser;
            }

            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.UpdateExpression);
            }

            private string ParseExpression(QueryTree.UpdateExpression expression)
            {
                var sb = new StringBuilder();
                sb.Append("UPDATE " + _parser.ParseExpression(expression.Update));
                sb.Append(" SET ");
                sb.Append(string.Join(", ",
                    expression.Set.Select(x => $"{_parser.ParseExpression(x.Key)} = {_parser.ParseExpression(x.Value)}")
                        .ToArray()));
                sb.Append(" WHERE ");
                sb.Append(string.Join(" AND ", expression.Where.Select(x => _parser.ParseExpression(x))));
                return sb.ToString();
            }
        }

        public class InsertExpressionParser : IExpressionParser
        {
            private readonly IExpressionParser _parser;

            public InsertExpressionParser(IExpressionParser parser)
            {
                _parser = parser;
            }

            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.InsertExpression);
            }

            private string ParseExpression(QueryTree.InsertExpression expression)
            {
                var qb = new StringBuilder();
                qb.Append("INSERT INTO ");
                qb.Append(_parser.ParseExpression(expression.Into));
                qb.Append("(");
                qb.Append(string.Join(",", expression.Columns.Select(x => _parser.ParseExpression(x)).ToArray()));
                qb.Append(")");
                qb.Append(_parser.ParseExpression(expression.From));
                return qb.ToString();
            }
        }

        public class UnAliasedTableExpressionParser : IExpressionParser
        {
            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.UnAliasedTableExpression);
            }

            private string ParseExpression(QueryTree.UnAliasedTableExpression expression)
            {
                return $"[{expression.Table}]";
            }
        }

        public class UnAliasedColumnExpressionParser : IExpressionParser
        {
            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.UnAliasedColumnExpression);
            }

            private string ParseExpression(QueryTree.UnAliasedColumnExpression expression)
            {
                return $"[{expression.Column}]";
            }
        }

        public class ValueGroupExpressionParser : IExpressionParser
        {
            private readonly IExpressionParser _parser;

            public ValueGroupExpressionParser(IExpressionParser parser)
            {
                _parser = parser;
            }

            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.ValueGroupExpression);
            }

            private string ParseExpression(QueryTree.ValueGroupExpression expression)
            {
                return
                    $"VALUES ({string.Join(",", expression.Values.Select(x => _parser.ParseExpression(x)).ToArray())})";
            }
        }

        public class SelectExpressionParser : IExpressionParser
        {
            private readonly IExpressionParser _parser;

            public SelectExpressionParser(IExpressionParser parser)
            {
                _parser = parser;
            }

            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.SelectExpression);
            }

            private string ParseExpression(QueryTree.SelectExpression expression)
            {
                var qb = new StringBuilder();
                qb.Append("SELECT ");
                foreach (var column in expression.Values)
                {
                    qb.Append(_parser.ParseExpression(column));
                    if (expression.Values.IndexOf(column) + 1 < expression.Values.Count)
                    {
                        qb.Append(", ");
                    }
                    else
                    {
                        qb.Append(" ");
                    }
                }

                if (expression.From != null)
                {
                    qb.Append(_parser.ParseExpression(expression.From));
                    foreach (var join in expression.Join)
                    {
                        qb.Append(_parser.ParseExpression(join));
                    }

                    if (expression.Where.Any())
                    {
                        qb.Append("WHERE ");
                        foreach (var where in expression.Where)
                        {
                            qb.Append(_parser.ParseExpression(where));
                            if (expression.Where.IndexOf(where) + 1 < expression.Where.Count)
                            {
                                qb.Append(" AND ");
                            }
                            else
                            {
                                qb.Append(" ");
                            }
                        }
                    }
                }

                return qb.ToString();
            }
        }

        public class AliasedExpressionParser : IExpressionParser
        {
            private readonly IExpressionParser _parser;

            public AliasedExpressionParser(IExpressionParser parser)
            {
                _parser = parser;
            }

            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.AliasedExpression);
            }

            private string ParseExpression(QueryTree.AliasedExpression expression)
            {
                return $"{_parser.ParseExpression(expression.Expression)} [{expression.Alias}]";
            }
        }

        public class ConstantExpressionParser : IExpressionParser
        {
            public string ParseExpression(QueryTree.Expression expression)
            {
                return ParseExpression(expression as QueryTree.ConstantExpression);
            }

            private string ParseExpression(QueryTree.ConstantExpression expression)
            {
                if (expression.Value == null)
                {
                    return "NULL";
                }

                return $"@{expression.ParameterName}";
            }
        }
    }
}