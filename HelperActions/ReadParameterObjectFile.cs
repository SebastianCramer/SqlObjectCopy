using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.DBActions;
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
        private const string REGEX_DELTA_COLUMN = @"\t(?'column'[A-Za-z0-9]+$)";

        public void Handle(List<SqlObject> objects, Options options)
        {
            if (!string.IsNullOrWhiteSpace(options.ListFile))
            {
                FileInfo info = new(options.ListFile);
                objects = GetSqlObjects(info);
            }

            NextAction?.Handle(objects, options);
        }

        /// <summary>
        /// Gets a list of SQL objects from a textfile
        /// </summary>
        /// <param name="file">The File to read from</param>
        /// <returns>A list of SQL objects from the file</returns>
        public static List<SqlObject> GetSqlObjects(FileInfo file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (!file.Exists)
            {
                throw new ArgumentException("file not found");
            }

            // the output list
            List<SqlObject> objectList = new();

            // to find schema and name in regex groups
            Regex objectRegex = new(REGEX_SQL_OBJECT);
            // to find delta columns
            Regex deltaRegex = new(REGEX_DELTA_COLUMN);


            foreach (string s in ReadObjectListFile(file))
            {
                // empty lines
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }

                string schema = objectRegex.Match(s).Groups["schema"].Value;
                string name = objectRegex.Match(s).Groups["object"].Value;

                if (string.IsNullOrEmpty(schema) && string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("schema or name not found in source file");
                }

                // The sql object to add
                SqlObject obj = new(schema, name, SqlObjectType.Unknown);

                // add a delta column name if one is given in the file
                string deltaColumn = deltaRegex.Match(s).Groups["column"].Value;
                if (!string.IsNullOrEmpty(deltaColumn))
                {
                    obj.DeltaColumnName = deltaColumn;
                }

                objectList.Add(obj);
            }

            return objectList;
        }

        private static string[] ReadObjectListFile(FileInfo listFile)
        {
            string content = File.ReadAllText(listFile.FullName);
            return content.Split(Environment.NewLine);
        }
    }
}
