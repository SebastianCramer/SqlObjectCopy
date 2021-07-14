SELECT DISTINCT referenced_schema_name + '.' + referenced_entity_name as commandText
FROM sys.dm_sql_referenced_entities ('[%ObjectName%]', 'OBJECT')
WHERE referenced_minor_name IS NULL;