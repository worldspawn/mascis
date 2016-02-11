
## Why?

Mascis trys to add a bit more flexibility to how you write your ORM queries. By using references to mapped columns and tables (entity collections) you can write your expressions pretty much how you want.

## It Does Not

- Resolve relationships for you. You can't map OrderItems from Orders and have it auto hydrate that relationship for you. This may come one day but I regard other ORMs implementation of this feature as pretty sucky - mostly because there is no great way to achieve it.

## How it works

- You can build queries by joining tables together, you can either return hydrated entities or return your results as a projection. Each table that is part of the query is represented as an instance of `QueryTable<TEntity>`. This class has a property, `Ex` which is of type `TEntity`. You can refer to this in your linq expressions eg. `query.Where(() => table1.Ex.Id == someId)`. You can also pass around the value of Ex directly. Its a proxy class that the query parser recognises as a reference to a table rather than just a member expression that should resolve to a value. Eg:

``` csharp
var order = query.FromTable.Ex;
query.Where(() => order.Amount > 100m);
```

## Projections

- Projections build on top of the query system to provide a way to select values into any class (including anonymous classes).

Eg:

``` csharp
var q = mascisSession.Query<Order>();
var projection = q.Project(() => new {
  Amount = q.FromTable.Ex.Amount
});

var results = mascisSession.Execute(projection);
```

You can also use classes with constructors. Eg:

``` csharp
var q = mascisSession.Query<Order>();
var projection = q.Project(() => new OrderProjection(q.FromTable.Ex.Id, q.FromTable.Ex.Amount) {
  DatePaid = q.FromTable.Ex.DatePaid
  });

var results = mascisSession.Execute(projection);
```

## Aggregates

Yeah don't have support for these yet, first need the ability to _group by_ which will probably be a feature used only in projections.

_most of what is below is wrong/changed_

## Expressions Supported

- Member expressions e.g. `someObject.SomeProperty`
- Member expressions from a table `queryTable.Ex.SomeProperty`
  - `queryTable` is an instance of `QueryTable`. The `Ex` property is a null reference typed to the entity the query table represents. This is how the expression parser knows what table alias to target and what column to target.
- Expressions targeting a map. `map.GetValue<string>()`. `map` is a mapped expression.

## Examples

### Creating a proxied instance
``` cs
var foo = mascisSession.Create<Foo>();
```

### Creating a query

``` cs
var query = mascisSession.Query<Foo>();
```

### Join a table to query
``` cs
var table = query.CreateTable<Bar>();
query.FromTable.Join(table, () => query.FromTable.Ex.Id == table.Ex.FooId);
```

### Join a subquery to query
``` cs
var table = query.CreateTable<Bar>();
var tableFields = {
  fooId = table.Map(()=>table.Ex.FooId);//adding a map causes a table to become a sub query
};
query.FromTable.Join(table, () => query.FromTable.Ex.Id == tableFields.fooId.Value<Guid>());
```
