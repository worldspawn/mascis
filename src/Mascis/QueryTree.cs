using System.Collections.Generic;

namespace Mascis
{
    public static class QueryTree
    {
        public class ConstantExpression: Expression
        {
            public object Value { get; set; }
        }

        public class Select : Expression
        {
            public Expression From { get; set; }
            public IList<AliasedExpression> Values { get; set; } 
            public IList<Join> Join { get; } = new List<Join>();
            public IList<Where> Where { get; } = new List<Where>();
        }

        public class From: Expression
        {
            public TableExpression Table { get; set; }
        }

        public class Join: Expression
        {
            public Expression Table { get; set; }
            public Expression On { get; set; }
        }

        public class TableExpression: Expression
        {
            public QueryTable Table { get; set; } 
        }

        public class Where
        {
            public Expression Clause { get; set; }
        }

        public class BinaryExpression : Expression
        {
            public BooleanOperator Operator { get; set; }
            public Expression Left { get; set; }
            public Expression Right { get; set; }
        }

        public class ColumnExpression : Expression
        {
            public QueryTable Table { get; set; }
            public string Column { get; set; } 
        }

        public class FunctionExpression : Expression
        {
            public string Name { get; set; }
            public Expression[] Arguments { get; set; }
        }

        public class AliasReference : Expression
        {
            public string Alias { get; set; }
            public QueryTable Table { get; set; }
        }

        public class AliasedExpression : Expression
        {
            public string Alias { get; set; }
            public Expression Expression { get; set; }
        }

        public enum BooleanOperator
        {
            And,
            Or,
            Equal,
            GreaterThan,
            Add
        }

        public class Expression
        {
            
        }
    }
}