﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.Contexts;
using SqlObjectCopy.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SqlObjectCopy.DBActions
{

    /// <summary>
    /// Reads sql objects and 
    /// </summary>
    internal class ReadObjectBaseInformation : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly SocConfiguration _configuration;
        private readonly ScriptProvider _scriptProvider;
        private readonly ILogger _logger;

        private readonly string CONSTRAINT_PATTERN = @"ALTER TABLE [\[\].\w]+ WITH CHECK ADD CONSTRAINT [\[\]\w. ()]+";

        public ReadObjectBaseInformation(SocConfiguration configuration, ScriptProvider scriptProvider, 
            ILogger<ReadObjectBaseInformation> logger)
        {
            _configuration = configuration;
            _scriptProvider = scriptProvider;
            _logger = logger;
        }

        // We're assuming that we get a bunch of sql objects with just the name and schema
        public void Handle(List<SqlObject> objects, Options options)
        {
            foreach (SqlObject o in objects.Where(o => o.Valid))
            {
                try
                {
                    _logger.LogInformation("{Object} reading base information", o.FullName);

                    if (o.ObjectType == SqlObjectType.Unknown)
                    {
                        SqlObject readObject = GetObjectIdentification(o);
                        o.ObjectType = readObject.ObjectType;
                    }

                    if (o.ObjectType == SqlObjectType.Table)
                    {
                        o.DeltaColumnType = this.GetDeltaColumnType(o);
                    }
                }
                catch (Exception ex)
                {
                    o.Valid = false;
                    o.LastException = ex;
                    _logger.LogError(ex, "{Object} an error occured while reading base information", o.FullName);
                }
            }

            // if there are any valid objects, proceed
            if (objects.Where(o => o.Valid).Any())
            {
                objects = GetScripts(objects);
                NextAction?.Handle(objects, options);
            }
            else
            {
                _logger.LogInformation("no more objects valid. ending process.");
            }
        }

        public SqlObject GetObjectIdentification(SqlObject obj)
        {
            if (string.IsNullOrEmpty(obj.SchemaName))
            {
                throw new ArgumentException("Paramter may not be null or empty", nameof(obj));
            }

            using ISocDbContext sourceContext = new SourceContext(_configuration);

            // Add tables
            SqlObject tableObject = (from t in sourceContext.Tables
                                     where t.TABLE_SCHEMA == obj.SchemaName && (obj.ObjectName == null || t.TABLE_NAME == obj.ObjectName)
                                     select new SqlObject(t.TABLE_SCHEMA, t.TABLE_NAME, t.TABLE_TYPE == "BASE TABLE" ? SqlObjectType.Table : SqlObjectType.View, obj.TargetSchemaName, obj.TargetFullName)).AsNoTracking().FirstOrDefault();

            if (tableObject != null)
            {
                return tableObject;
            }

            // Add procedures
            SqlObject procedureObject = (from r in sourceContext.Routines
                                         where r.ROUTINE_SCHEMA == obj.SchemaName && (obj.ObjectName == null || r.ROUTINE_NAME == obj.ObjectName)
                                         select new SqlObject(r.ROUTINE_SCHEMA, r.ROUTINE_NAME, SqlObjectType.Procedure, obj.TargetSchemaName, obj.TargetFullName)).AsNoTracking().FirstOrDefault();

            if (procedureObject != null)
            {
                return procedureObject;
            }

            // Add user defined table types
            return (from d in sourceContext.Domains
                    where d.DOMAIN_SCHEMA == obj.SchemaName && (obj.ObjectName == null || d.DOMAIN_NAME == obj.ObjectName)
                    select new SqlObject(d.DOMAIN_SCHEMA, d.DOMAIN_NAME, SqlObjectType.Type, obj.TargetSchemaName, obj.TargetFullName)).AsNoTracking().FirstOrDefault();
        }

        private List<SqlObject> GetScripts(List<SqlObject> objList)
        {
            objList.ForEach(o =>
            {
                SetCreateScript(o);
                SetDeleteScript(o);

                for (int i = 0; i < o.ConstraintScripts.Count; i++)
                {
                    o.ConstraintScripts[i] = ReplaceReferencedTable(objList, o.ConstraintScripts[i]);
                }
            });

            return objList;
        }

        private void SetCreateScript(SqlObject obj)
        {
            string script = obj.ObjectType switch
            {
                SqlObjectType.Procedure => _scriptProvider.GetProcedureCreateScript(obj),
                SqlObjectType.Table => _scriptProvider.GetTableCreateScript(obj),
                SqlObjectType.View => _scriptProvider.GetProcedureCreateScript(obj),
                SqlObjectType.Type => _scriptProvider.GetTypeCreateScript(obj),
                SqlObjectType.Function => string.Empty,
                _ => string.Empty,
            };

            List<string> constraints = new();

            // all constraints 
            foreach (string c in script.Split("\r").Where(s => s.StartsWith("ALTER TABLE")).ToList())
            {
                // remove constraint from create script
                script = script.Replace(c, string.Empty);

                // if constraint is a create and no check, add it
                if (Regex.IsMatch(c, CONSTRAINT_PATTERN))
                {
                    string constraint = string.Empty;

                    if (c.Contains("FOREIGN KEY"))
                    {
                        // Add no check to avoid complications with data integrity
                        // due to synchronous copy tasks
                        constraint = c.Replace("WITH CHECK", string.Empty).Replace("ADD", "WITH NOCHECK ADD");
                    }
                    else
                    {
                        constraint = c;
                    }

                    constraints.Add(constraint);
                }
            }

            // add constraint script
            obj.ConstraintScripts = constraints;

            // only the create script should be left
            obj.CreateScript = script;
        }

        private static string ReplaceReferencedTable(List<SqlObject> objects, string foreignKeyConstraint)
        {
            foreach (SqlObject obj in objects)
            {
                foreignKeyConstraint = foreignKeyConstraint.Replace(obj.SafeName, obj.TargetSafeName);
            }

            return foreignKeyConstraint;
        }

        private static void SetDeleteScript(SqlObject obj)
        {
            obj.DeleteScript = FormattableStringFactory.Create("DROP {0} {1}", obj.ObjectType.ToString(), obj.TargetSafeName).ToString();
        }

        private string GetDeltaColumnType(SqlObject obj)
        {
            using SourceContext sourceContext = new(this._configuration);
            return (from t in sourceContext.Types
                          join c in sourceContext.Columns on t.SystemTypeId equals c.SystemTypeId
                          select t.Name).AsNoTracking().FirstOrDefault();
        }
    }
}
