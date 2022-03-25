using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.Contexts;
using SqlObjectCopy.Models;
using SqlObjectCopy.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SqlObjectCopy.Extensions
{
    public static class SqlObjectExtensions
    {
        public static bool HasData(this SqlObject obj, SocConfiguration configuration)
        {
            if (obj.ObjectType == SqlObjectType.Table)
            {
                try
                {
                    using ISocDbContext targetContext = new TargetContext(configuration);

                    Scripts result = targetContext.Scripts.FromSqlRaw(
                            FormattableStringFactory.Create("SELECT CAST(COUNT(*) AS NVARCHAR) AS CommandText FROM {0}", obj.FullName).ToString()).FirstOrDefault();
                    if (result != null && !string.IsNullOrEmpty(result.CommandText))
                    {
                        if (int.TryParse(result.CommandText, out int count))
                        {
                            return count > 0;
                        }
                    }
                }
                catch (Exception)
                {
                    return false;
                }

                return false;
            }
            else
            {
                return false;
            }
        }

        public static bool Exists(this SqlObject obj, SocConfiguration configuration)
        {
            using ISocDbContext targetContext = new TargetContext(configuration);

            bool result = false;
            switch (obj.ObjectType)
            {
                case SqlObjectType.Procedure:
                    result = (from r in targetContext.Routines
                              where r.ROUTINE_NAME == obj.TargetObjectName && r.ROUTINE_SCHEMA == obj.TargetSchemaName
                              select r).Count() == 1;
                    break;
                case SqlObjectType.Table:
                case SqlObjectType.View:
                    result = (from t in targetContext.Tables
                              where t.TABLE_NAME == obj.TargetObjectName && t.TABLE_SCHEMA == obj.TargetSchemaName
                              select t).Count() == 1;
                    break;
                default:
                    break;
            }

            return result;
        }

        public static List<string> GetReferencedObjectNames(this SqlObject obj, SocConfiguration configuration, ScriptProvider provider, ILogger logger)
        {
            List<string> refList = new();

            if (obj.ObjectType == SqlObjectType.Table)
            {
                obj.ConstraintScripts.ForEach(s =>
                {
                    MatchCollection matches = Regex.Matches(s, @"REFERENCES \[[a-zA-Z]{3}\].\[[a-zA-Z]+\]");

                    foreach (Match m in matches)
                    {
                        refList.Add(m.Value.Replace("REFERENCES ", string.Empty)
                         .Replace("[", string.Empty)
                         .Replace("]", string.Empty)
                         );
                    }
                });
            }
            else if (obj.ObjectType == SqlObjectType.View || obj.ObjectType == SqlObjectType.Procedure)
            {
                using ISocDbContext sourceContext = new SourceContext(configuration);
                string command = provider.GetReferenceScript(obj.FullName);

                try
                {
                    sourceContext.Scripts.FromSqlRaw(command).ToList().ForEach(r =>
                    {
                        if (r != null && !string.IsNullOrEmpty(r.CommandText))
                        {
                            refList.Add(r.CommandText);
                        }
                    });
                }
                catch (Exception)
                {
                    obj.Valid = false;
                    logger.LogWarning("{Object} points to a reference that does not exist. This object will likely not complile on target and will be skipped", obj.FullName);
                }
            }

            return refList;
        }

        public static long SourceRowCount(this SqlObject obj, SocConfiguration configuration)
        {
            using SourceContext sourceContext = new(configuration);
            SqlConnection con = new(sourceContext.Database.GetDbConnection().ConnectionString);

            try
            {
                // get rowcount of table
                SqlCommand countCommand;

                if (obj.IsDeltaTransport)
                {
                    var deltaValue = obj.GetLastDeltaValue(configuration);
                    countCommand = new SqlCommand("SELECT COUNT(*) FROM " + obj.FullName + " WHERE CAST(" + obj.DeltaColumnName + " AS NVARCHAR) > '" + (string.IsNullOrWhiteSpace(deltaValue) ? Char.MinValue.ToString() : deltaValue) + "'", con);
                } else
                {
                    countCommand = new SqlCommand("SELECT COUNT(*) FROM " + obj.FullName, con);
                }

                con.Open();
                return long.Parse(countCommand.ExecuteScalar().ToString());
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        /// <summary>
        /// Get the objectID from a database object
        /// </summary>
        /// <param name="obj">The object of whom to get the id from</param>
        /// <param name="configuration">configuration for instancing a new db connection</param>
        /// <returns></returns>
        public static int GetObjectID(this SqlObject obj, SocConfiguration configuration)
        {
            using ISocDbContext targetContext = new TargetContext(configuration);
            FormattableString command = FormattableStringFactory.Create("SELECT CAST(OBJECT_ID('{0}') AS NVARCHAR) AS CommandText", obj.TargetFullName);
            Scripts result = targetContext.Scripts.FromSqlRaw(command.ToString()).FirstOrDefault();
            if (int.TryParse(result.CommandText, out int objectID))
            {
                return objectID;
            }

            return -1;
        }

        /// <summary>
        /// Get the last delta value in the target system as starting point for our datatransfer
        /// </summary>
        /// <param name="obj">A sql object</param>
        /// <param name="configuration">configuration to create context class</param>
        /// <returns>the last delta value or empty, if object is no table or has no data</returns>
        public static string GetLastDeltaValue(this SqlObject obj, SocConfiguration configuration)
        {
            if (obj.ObjectType != SqlObjectType.Table || string.IsNullOrWhiteSpace(obj.DeltaColumnName))
            {
                return string.Empty;
            }

            // check last delta on target system
            using ISocDbContext targetContext = new TargetContext(configuration);
            FormattableString command = FormattableStringFactory.Create("SELECT CAST(MAX({0}) AS NVARCHAR) AS CommandText FROM {1}", obj.DeltaColumnName, obj.TargetFullName);

            Scripts result = targetContext.Scripts.FromSqlRaw(command.ToString()).FirstOrDefault();

            return result.CommandText ?? Char.MinValue.ToString();
        }
    }
}
