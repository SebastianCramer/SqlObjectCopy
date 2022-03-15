using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.Contexts;
using SqlObjectCopy.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlObjectCopy.DBActions
{
    internal class CreateConstraints : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly SocConfiguration _configuration;
        private readonly ILogger _logger;

        public CreateConstraints(SocConfiguration configuration, ILogger<CreateConstraints> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
                objects.Where(o => o.ObjectType == SqlObjectType.Table && o.Valid).ToList().ForEach(o =>
                {
                    try
                    {
                        CreateTargetObjectConstraints(o);
                    }
                    catch (System.Exception ex)
                    {
                        o.Valid = false;
                        o.LastException = ex;
                    }
                });

            NextAction?.Handle(objects, options);
        }

        /// <summary>
        /// Create the foreign keys and other constraints on the object in the target database
        /// </summary>
        /// <param name="obj">The Object to create the constraints on</param>
        public void CreateTargetObjectConstraints(SqlObject obj)
        {
            ISocDbContext target = new TargetContext(_configuration);

            if (obj.Exists(_configuration))
            {
                _logger.LogInformation("{Object} constraint creation", obj.FullName);
                obj.ConstraintScripts.ForEach(c =>
                {
                    string constName = GetConstraintNameFromCreationScript(c);
                    if (!string.IsNullOrEmpty(constName))
                    {
                        if (!ConstraintExists(constName, obj.SchemaName))
                        {
                            try
                            {
                                target.Database.ExecuteSqlRaw(c);
                            }
                            catch (System.Exception ex)
                            {
                                _logger.LogError(ex, "{Object} An error occured while creating the constraint {0}", obj.FullName ,constName);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("{Object} constraint {0} does already exist on target. skipping creation.", obj.FullName ,constName);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("{Object} could not create constraint. Please check afterwards", obj.FullName);
                    }
                });
            }
        }

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

        // TODO: Constraint exists check global
        private bool ConstraintExists(string name, string schema)
        {
            ISocDbContext target = new TargetContext(_configuration);
            int constCount = (from c in target.Constraints
                              where c.CONSTRAINT_SCHEMA == schema && c.CONSTRAINT_NAME == name
                              select c).AsNoTracking().Count();

            return constCount > 0;
        }
    }
}
