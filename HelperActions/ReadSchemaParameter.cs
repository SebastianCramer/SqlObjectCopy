using Microsoft.Extensions.Logging;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.Contexts;
using SqlObjectCopy.DBActions;
using System.Collections.Generic;
using System.Linq;

namespace SqlObjectCopy.HelperActions
{
    internal class ReadSchemaParameter : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly SocConfiguration configuration;

        public ReadSchemaParameter(SocConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            if (!string.IsNullOrWhiteSpace(options.SourceSchema) && string.IsNullOrWhiteSpace(options.SourceObjectFullName) && string.IsNullOrWhiteSpace(options.ListFile))
            {
                objects = GetSchemaObjects(options.SourceSchema, options.TargetSchemaName);
            }

            NextAction?.Handle(objects, options);
        }

        private List<SqlObject> GetSchemaObjects (string schemaName, string targetSchemaName)
        {
            List<SqlObject> resultList = new();

            using ISocDbContext sourceContext = new SourceContext(configuration);

            // Add tables
            resultList.AddRange((from t in sourceContext.Tables
                                 where t.TABLE_SCHEMA == schemaName
                                 select new SqlObject(t.TABLE_SCHEMA, t.TABLE_NAME, t.TABLE_TYPE == "BASE TABLE" ? SqlObjectType.Table : SqlObjectType.View, targetSchemaName, t.TABLE_NAME)).ToList());

            // Add procedures
            resultList.AddRange((from r in sourceContext.Routines
                                 where r.ROUTINE_SCHEMA == schemaName
                                 select new SqlObject(r.ROUTINE_SCHEMA, r.ROUTINE_NAME, SqlObjectType.Procedure, targetSchemaName, r.ROUTINE_NAME)).ToList());

            // Add user defined table types
            resultList.AddRange((from d in sourceContext.Domains
                                 where d.DOMAIN_SCHEMA == schemaName
                                 select new SqlObject(d.DOMAIN_SCHEMA, d.DOMAIN_NAME, SqlObjectType.Type, targetSchemaName, d.DOMAIN_NAME)).ToList());

            return resultList;
        }
    }
}