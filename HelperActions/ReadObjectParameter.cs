using Microsoft.Extensions.Logging;
using SqlObjectCopy.DBActions;
using System.Collections.Generic;

namespace SqlObjectCopy.HelperActions
{
    internal class ReadObjectParameter : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly ILogger logger;

        public ReadObjectParameter(ILogger<ReadObjectParameter> logger)
        {
            this.logger = logger;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            if (!string.IsNullOrEmpty(options.SourceObjectFullName))
            {
                string schemaName = options.SourceSchemaName;
                string objectName = options.SourceObjectName;

                if (string.IsNullOrWhiteSpace(schemaName) || string.IsNullOrWhiteSpace(objectName))
                {
                    logger.LogError("Invalid object parameter. Must contain schema.Name");
                }
                else
                {
                    string targetSchemaName = options.TargetSchemaName;
                    string targetObjectName = options.TargetObjectName;

                    if (!string.IsNullOrEmpty(schemaName) && !string.IsNullOrEmpty(objectName))
                    {
                        SqlObject obj = new(schemaName, objectName, SqlObjectType.Unknown, targetSchemaName, targetObjectName)
                        {
                            DeltaColumnName = options.DeltaColumnName ?? null,
                            TargetObjectName = targetObjectName,
                            TargetSchemaName = targetSchemaName
                        };

                        objects.Add(obj);
                    }
                }
            }

            NextAction?.Handle(objects, options);
        }
    }
}
