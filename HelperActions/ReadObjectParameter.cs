using SqlObjectCopy.DBActions;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlObjectCopy.HelperActions
{
    internal class ReadObjectParameter : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private const string TARGETOBJECT_REGEX = "[\\w]+";

        public void Handle(List<SqlObject> objects, Options options)
        {
            if (!string.IsNullOrEmpty(options.ObjectName))
            {
                string schemaName = options.ObjectName[..options.ObjectName.IndexOf('.')];
                string objectName = options.ObjectName.Replace(schemaName + '.', string.Empty);

                MatchCollection targetObjectMatches = Regex.Matches(options.TargetObjectName, TARGETOBJECT_REGEX);
                string targetSchemaName = null;
                string targetObjectName = null;

                if (targetObjectMatches.Count == 2)
                {
                    targetSchemaName = targetObjectMatches[0].Value;
                    targetObjectName = targetObjectMatches[1].Value;
                }

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

            NextAction?.Handle(objects, options);
        }
    }
}
