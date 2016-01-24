using System;
using System.Linq;
using System.Linq.Expressions;

namespace Mascis
{
    public class QueryParser
    {
        private readonly MascisSession _session;
        private readonly ExpressionParser _binaryParser;

        public QueryParser(MascisSession session)
        {
            _session = session;
            _binaryParser = new ExpressionParser(_session);
        }

        public QueryTree.Select Parse(QueryTable queryTable)
        {
            var ex = new QueryTree.Select
            {
                Values = queryTable.Maps.Select(x => new QueryTree.AliasedExpression
                {
                    Alias = x.Alias,
                    Expression = _binaryParser.Parse((Expression<Func<object>>) x.Expression)
                }).ToList(),

            };

            ex.From = new QueryTree.From
            {
                Table = new QueryTree.TableExpression
                {
                    Table = queryTable
                }
            };

            foreach (var j in queryTable.Joins)
            {
                if (j.QueryTable.Maps.Count > 0 || j.QueryTable.Joins.Count > 0)
                {
                    ex.Join.Add(new QueryTree.Join
                    {
                        On = _binaryParser.Parse(j.On),
                        Table = Parse(j.QueryTable)
                    });
                }
                else
                {
                    ex.Join.Add(new QueryTree.Join
                    {
                        On = _binaryParser.Parse(j.On),
                        Table = new QueryTree.TableExpression
                        {
                            Table = j.QueryTable
                        }
                    });
                }


                foreach (var w in queryTable.Wheres)
                {
                    ex.Where.Add(new QueryTree.Where
                    {
                        Clause = _binaryParser.Parse(w.Where)
                    });
                }
            }

            return ex;

        }

        public QueryTree.Select Parse<TEntity>(Query<TEntity> query)
        {
            var ex = Parse(query.FromTable);
            
            return ex;
        }
    }
}