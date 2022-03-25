using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.Contexts;
using SqlObjectCopy.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace SqlObjectCopy.DBActions
{
    internal class CreateSchema : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly SocConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ScriptProvider _scriptProvider;

        public CreateSchema(SocConfiguration configuration, ILogger<CreateSchema> logger, ScriptProvider scriptProvider)
        {
            _configuration = configuration;
            _logger = logger;
            _scriptProvider = scriptProvider;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            IEnumerable<string> targetSchemas = (from o in objects
                                                 select o.TargetSchemaName).Distinct();

            // get all schemas from object list
            objects.Select(o => o.TargetSchemaName).Distinct().ToList().ForEach(o =>
            {
                CreateSchemaIfNotExists(o);
            });

            NextAction?.Handle(objects, options);
        }

        private void CreateSchemaIfNotExists(string schemaName)
        {
            using ISocDbContext targetContext = new TargetContext(_configuration);

            Models.Schemata targetSchema = (from s in targetContext.Schemata
                                            where s.SCHEMA_NAME == schemaName
                                            select s).AsNoTracking().FirstOrDefault();

            if (targetSchema == null)
            {
                _logger.LogInformation("creating schema {Schema}", schemaName);
                targetContext.Database.ExecuteSqlRaw(_scriptProvider.GetSchemaCreateScript(schemaName));
            }
        }
    }
}
