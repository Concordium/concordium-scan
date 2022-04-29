using System.Linq.Expressions;
using Application.Api.GraphQL.Accounts;

namespace Application.Api.GraphQL.Pagination;

public class DescendingValueCursorPagingAlgorithm<T> : CursorPagingAlgorithmBase<T>
{
    private readonly ICursorSerializer _cursorSerializer;
    private readonly Expression<Func<T, long>> _valueSelectorExpression;
    private readonly Func<T,long> _valueSelector;

    public DescendingValueCursorPagingAlgorithm(ICursorSerializer cursorSerializer, Expression<Func<T, long>> valueSelector)
    {
        _cursorSerializer = cursorSerializer;
        _valueSelectorExpression = valueSelector;
        _valueSelector = valueSelector.Compile();
    }

    protected override IQueryable<T> ApplyAfterFilter(IQueryable<T> query, string serializedCursor) => 
        query.Where(CreateExpression(serializedCursor, Expression.LessThan));

    protected override IQueryable<T> ApplyBeforeFilter(IQueryable<T> query, string serializedCursor) =>
        query.Where(CreateExpression(serializedCursor, Expression.GreaterThan));

    protected override string GetSerializedCursor(T entity) => _cursorSerializer.Serialize(_valueSelector(entity));

    private Expression<Func<T, bool>> CreateExpression(string serializedCursor, Func<Expression, Expression, BinaryExpression> comparisonExpression)
    {
        var entityValue = (MemberExpression)_valueSelectorExpression.Body;
        var deserializedCursorValue = Expression.Constant(_cursorSerializer.Deserialize(serializedCursor), typeof(long));
        var lessThan = comparisonExpression(entityValue, deserializedCursorValue);

        var lambda = Expression.Lambda<Func<T, bool>>(lessThan, _valueSelectorExpression.Parameters.Single());
        return lambda;
    }
}