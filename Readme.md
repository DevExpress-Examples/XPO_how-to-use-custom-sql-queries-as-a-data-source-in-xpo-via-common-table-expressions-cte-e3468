# How to use custom SQL queries as a data source in XPO via Common Table Expressions (CTE)

Many modern SQL servers support [Common Table Expressions (CTE)](https://en.wikipedia.org/wiki/Hierarchical_and_recursive_queries_in_SQL#Common_table_expression) feature that allows you to declare a temporary named result set derived from a query and use it in the from part of another query. This feature can be employed in XPO via patching SQL queries it generates. We will demonstrate this functionality in an example with MSSQL Server.

To accomplish this task, we create a custom connection provider derived from MSSqlConnectionProvider and add a dictionary of user-defined CTE definitions. We override the FormatSelect and FormatTable methods to insert CTE definitions into generated queries.

In an application, we declare a persistent class mapped to a CTE definition, and call our connection provider methods to register an SQL query as a CTE definition before using it and unregister it afterwards. This way we can dynamically change SQL queries mapped to a persistent object during application lifetime. An example attached to this article demonstrates how this persistent class can be used in typical scenarios of expression evaluation, collection filtering and LINQ queries.

