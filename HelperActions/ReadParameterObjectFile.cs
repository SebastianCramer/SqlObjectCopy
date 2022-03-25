using SqlObjectCopy.DBActions;
using SqlObjectCopy.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SqlObjectCopy.HelperActions
{
    internal class ReadParameterObjectFile : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private const string REGEX_SQL_OBJECT = @"^(?'schema'[A-Za-z0-9]+)\.(?'object'[A-Za-z0-9_]+)";

        public void Handle(List<SqlObject> objects, Options options)
        {
            if (!string.IsNullOrWhiteSpace(options.ListFile))
            {
                FileInfo info = new(options.ListFile); 
                var parameters = DeserializeJson(File.ReadAllText(info.FullName));

                objects = GetSqlObjects(parameters);
            }

            NextAction?.Handle(objects, options);
        }

        private ParameterFileObject[] DeserializeJson(string json) {
            return System.Text.Json.JsonSerializer.Deserialize<ParameterFileObject[]>(json);
        }

        private static List<SqlObject> GetSqlObjects(ParameterFileObject[] parameters)
        {
            List<SqlObject> sqlObjects = new();

            foreach (var parameter in parameters)
            {
                if (parameter.SourceObject == null || string.IsNullOrWhiteSpace(parameter.SourceObject))
                {
                    throw new ArgumentException("source schema or name missing for at least one parameter");
                }

                var sourceObjectMatches = new Regex(REGEX_SQL_OBJECT).Matches(parameter.SourceObject)[0];
                var targetObjectMatches = new Regex(REGEX_SQL_OBJECT).Matches(parameter.TargetObject ?? parameter.SourceObject)[0];

                var obj = new SqlObject(sourceObjectMatches.Groups["schema"].Value, sourceObjectMatches.Groups["object"].Value,
                    SqlObjectType.Unknown,
                    targetObjectMatches.Groups["schema"].Value, targetObjectMatches.Groups["object"].Value);

                if (parameter.DeltaColumn != null && !string.IsNullOrWhiteSpace(parameter.DeltaColumn))
                {
                    obj.DeltaColumnName = parameter.DeltaColumn;
                }

                sqlObjects.Add(obj);
            }

            return sqlObjects;
        }
    }
}
