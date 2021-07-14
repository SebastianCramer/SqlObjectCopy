using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Contexts;
using SqlObjectCopy.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace SqlObjectCopy.DBActions
{
    internal class CreateSqlObjects : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public CreateSqlObjects(IConfiguration configuration, ILogger<CreateSqlObjects> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            objects.Where(o => o.Valid && !o.IsDeltaTransport).ToList().ForEach(o => {
                try
                {
                    CreateTargetObjectIfNotExists(o);
                }
                catch (System.Exception ex)
                {
                    o.Valid = false;
                    _logger.LogError(ex, "{Object} an error occured on creating object");
                }
                
            });

            NextAction?.Handle(objects, options);
        }

        /// <summary>
        /// Creates an object in the target database if the object does not exist
        /// </summary>
        /// <param name="obj">The object to create</param>
        /// <returns>True, if the object was created by this function</returns>
        public bool CreateTargetObjectIfNotExists(SqlObject obj)
        {
            // new context for thread safety
            ISocDbContext target = new TargetContext(_configuration);
            // create only when not exists
            if (!obj.Exists(_configuration))
            {
                try
                {
                    _logger.LogInformation("{Object} creating object", obj.FullName);
                    target.Database.ExecuteSqlRaw(obj.CreateScript);
                    return true;
                }
                catch (System.Exception)
                {
                    _logger.LogWarning("{Object} could not be created. there are probably referenced objects missing in the target database", obj.FullName);
                    obj.Valid = false;
                    return true;
                }
            }
            else
            {
                return false;
            }

        }
    }
}
