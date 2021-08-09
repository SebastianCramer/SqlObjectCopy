using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Contexts;
using SqlObjectCopy.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace SqlObjectCopy.DBActions
{
    class CreateSchema : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ScriptProvider _scriptProvider;

        public CreateSchema(IConfiguration configuration, ILogger<CreateSchema> logger, ScriptProvider scriptProvider)
        {
            _configuration = configuration;
            _logger = logger;
            _scriptProvider = scriptProvider;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            if (!string.IsNullOrEmpty(options.Schema))
            {
                CreateSchemaIfNotExists(options.Schema);
            }
            else
            {
                // get all schemas from object list
                objects.Select(o => o.SchemaName).Distinct().ToList().ForEach(o =>
                {
                    CreateSchemaIfNotExists(o);
                });
            }

            NextAction?.Handle(objects, options);
        }

        private void CreateSchemaIfNotExists(string schemaName)
        {
            using ISocDbContext targetContext = new TargetContext(_configuration);

            var targetSchema = (from s in targetContext.Schemata
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
