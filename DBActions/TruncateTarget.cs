using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SqlObjectCopy.DBActions
{
    internal class TruncateTarget : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public TruncateTarget(IConfiguration configuration, ILogger<TruncateTarget> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            objects.Where(o => o.ObjectType == SqlObjectType.Table && !o.IsDeltaTransport).ToList().ForEach(o =>
            {
                try
                {
                    TruncateTable(o);
                }
                catch (Exception ex)
                {
                    o.Valid = false;
                    _logger.LogError(ex, "{Object} an error occured on truncating", o.FullName);
                }
            });

            NextAction?.Handle(objects, options);
        }

        private void TruncateTable(SqlObject obj)
        {
            ISocDbContext target = new TargetContext(_configuration);
            FormattableString command = FormattableStringFactory.Create("TRUNCATE TABLE {0}", obj.SafeName);

            _logger.LogInformation("{Object} truncating");
            target.Database.ExecuteSqlRaw(command.ToString());
        }
    }
}
