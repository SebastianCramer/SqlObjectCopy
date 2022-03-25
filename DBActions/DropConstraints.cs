using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SqlObjectCopy.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SqlObjectCopy.Extensions;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Configuration;

namespace SqlObjectCopy.DBActions
{
    class DropConstraints : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly SocConfiguration _configuration;
        private readonly ILogger _logger;

        public DropConstraints(SocConfiguration configuration, ILogger<DropConstraints> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            objects.Where(o => o.ObjectType == SqlObjectType.Table && o.Valid && o.Exists(_configuration)).ToList().ForEach(o =>
            {
                try
                {
                    DropTargetObjectConstraints(o);
                }
                catch (System.Exception ex)
                {
                    o.Valid = false;
                    o.LastException = ex;
                    _logger.LogError(ex, "{Object} an error occured on dropping constraints", o.TargetFullName);
                }
            });

            NextAction?.Handle(objects, options);
        }

        public void DropTargetObjectConstraints(SqlObject obj)
        {
            using ISocDbContext target = new TargetContext(_configuration);

            obj.ConstraintScripts.ForEach(c =>
            {
                var constraintName = GetConstraintNameFromCreationScript(c);
                if (ConstraintExists(constraintName, obj.TargetSchemaName))
                {
                    _logger.LogInformation("{Object} dropping constraint {ConstraintName}", obj.TargetFullName, constraintName);
                    target.Database.ExecuteSqlRaw("ALTER TABLE " + obj.TargetFullName + " DROP CONSTRAINT " + constraintName);
                }
            });
        }

        // TODO: This method in sql object extensions
        private bool ConstraintExists(string name, string schema)
        {
            ISocDbContext target = new TargetContext(_configuration);
            int constCount = (from c in target.Constraints
                              where c.CONSTRAINT_SCHEMA == schema && c.CONSTRAINT_NAME == name
                              select c).AsNoTracking().Count();

            return constCount > 0;
        }

        // TODO: This method in sql object extensions
        private string GetConstraintNameFromCreationScript(string script)
        {
            Match match = Regex.Match(script, @"CONSTRAINT \[(?'name'[\w]+)\]");
            if (match.Success)
            {
                return match.Groups["name"].Value;
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
