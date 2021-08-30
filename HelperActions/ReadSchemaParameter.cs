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

        private readonly SocConfiguration _configuration;
        private readonly ILogger _logger;

        public ReadSchemaParameter(SocConfiguration configuration, ILogger<ReadSchemaParameter> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            if (!string.IsNullOrWhiteSpace(options.Schema) && string.IsNullOrWhiteSpace(options.ObjectName) && string.IsNullOrWhiteSpace(options.ListFile))
            {
                objects = GetSchemaObjects(options.Schema);
            }

            NextAction?.Handle(objects, options);
        }

        private List<SqlObject> GetSchemaObjects (string schemaName)
        {
            List<SqlObject> resultList = new List<SqlObject>();

            using ISocDbContext sourceContext = new SourceContext(_configuration);

            // Add tables
            resultList.AddRange((from t in sourceContext.Tables
                                 where t.TABLE_SCHEMA == schemaName
                                 select new SqlObject(t.TABLE_SCHEMA, t.TABLE_NAME, t.TABLE_TYPE == "BASE TABLE" ? SqlObjectType.Table : SqlObjectType.View)).ToList());

            // Add procedures
            resultList.AddRange((from r in sourceContext.Routines
                                 where r.ROUTINE_SCHEMA == schemaName
                                 select new SqlObject(r.ROUTINE_SCHEMA, r.ROUTINE_NAME, SqlObjectType.Procedure)).ToList());

            // Add user defined table types
            resultList.AddRange((from d in sourceContext.Domains
                                 where d.DOMAIN_SCHEMA == schemaName
                                 select new SqlObject(d.DOMAIN_SCHEMA, d.DOMAIN_NAME, SqlObjectType.Type)).ToList());

            return resultList;
        }
    }
}