using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.Contexts;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SqlObjectCopy.Utilities
{
    public class ScriptProvider
    {
        private readonly string ScriptPath = Path.Combine(Directory.GetParent(AppContext.BaseDirectory).FullName, "Scripts/");
        private readonly SocConfiguration _configuration;

        public ScriptProvider(SocConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetTableCreateScript(SqlObject Table)
        {
            string fileName = "GetTableCreate.sql";

            if (Table.ObjectType != SqlObjectType.Table)
            {
                throw new ArgumentException("Object parameter must be a table");
            }

            if (File.Exists(Path.Combine(ScriptPath, fileName)))
            {
                string command = File.ReadAllText(Path.Combine(ScriptPath, fileName)).Replace("[%SourceTableName%]", Table.FullName).Replace("[%TargetTableName%]", Table.TargetFullName);
                using ISocDbContext sourceContext = new SourceContext(_configuration);
                return sourceContext.Scripts.FromSqlInterpolated(FormattableStringFactory.Create(command)).AsEnumerable().FirstOrDefault().CommandText;
            }
            else
            {
                throw new FileNotFoundException("Script file not found");
            }
        }

        public string GetProcedureCreateScript(SqlObject Procedure)
        {
            string fileName = "GetProcedureCreate.sql";

            if (Procedure.ObjectType != SqlObjectType.Procedure && Procedure.ObjectType != SqlObjectType.View)
            {
                throw new ArgumentException("Object parameter must be a procedure");
            }

            if (File.Exists(Path.Combine(ScriptPath, fileName)))
            {
                string command = File.ReadAllText(Path.Combine(ScriptPath, fileName)).Replace("[%ProcedureName%]", Procedure.TargetFullName);

                using ISocDbContext sourceContext = new SourceContext(_configuration);
                Models.Script obj = sourceContext.Scripts.FromSqlRaw(command).FirstOrDefault();
                if (obj != null) {
                    return obj.CommandText;
                } else
                {
                    return string.Empty;
                }
            }
            else
            {
                throw new FileNotFoundException("Script file not found");
            }
        }

        public string GetSchemaCreateScript(string Schema)
        {
            string fileName = "GetSchemaCreate.sql";

            if (string.IsNullOrEmpty(Schema))
            {
                throw new ArgumentException("Schema name may not be empty");
            }

            if (File.Exists(Path.Combine(ScriptPath, fileName)))
            {
                return File.ReadAllText(Path.Combine(ScriptPath, fileName)).Replace("[%SchemaName%]", Schema);
            }
            else
            {
                throw new FileNotFoundException("Script file not found");
            }
        }

        public string GetReferenceScript(string objectName)
        {
            string fileName = "GetReferenceScript.sql";

            if (string.IsNullOrEmpty(objectName))
            {
                throw new ArgumentException("Object name may not be empty");
            }

            if (File.Exists(Path.Combine(ScriptPath, fileName)))
            {
                return File.ReadAllText(Path.Combine(ScriptPath, fileName)).Replace("[%ObjectName%]", objectName);
            }
            else
            {
                throw new FileNotFoundException("Script file not found");
            }
        }

        public string GetTypeCreateScript(SqlObject Type)
        {
            string fileName = "GetTypeCreate.sql";

            if (Type.ObjectType != SqlObjectType.Type)
            {
                throw new ArgumentException("Object parameter must be a user defined table type");
            }

            if (File.Exists(Path.Combine(ScriptPath, fileName)))
            {
                string command = File.ReadAllText(Path.Combine(ScriptPath, fileName)).Replace("[%TypeName%]", Type.TargetFullName);

                using ISocDbContext sourceContext = new SourceContext(_configuration);
                return sourceContext.Scripts.FromSqlInterpolated(FormattableStringFactory.Create(command)).AsEnumerable().FirstOrDefault().CommandText;
            }
            else
            {
                throw new FileNotFoundException("Script file not found");
            }
        }
    }
}
