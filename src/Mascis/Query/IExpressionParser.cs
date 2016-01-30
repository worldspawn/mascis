namespace Mascis.Query
{
    public interface IExpressionParser
    {
        string ParseExpression(QueryTree.Expression expression);
    }
}