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
