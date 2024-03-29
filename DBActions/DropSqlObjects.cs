﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.Contexts;
using SqlObjectCopy.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlObjectCopy.DBActions
{
    internal class DropSqlObjects : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly SocConfiguration _configuration;
        private readonly ILogger _logger;

        public DropSqlObjects(SocConfiguration configuration, ILogger<DropSqlObjects> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            // drop target objects
            // dropping is the reverse of creating
            objects.Reverse();

            objects.Where(o => o.Valid && !o.IsDeltaTransport).ToList().ForEach(o =>
            {
                try
                {
                    DropTargetObjectIfExists(o);
                }
                catch (Exception ex)
                {
                    o.Valid = false;
                    o.LastException = ex;
                    _logger.LogError(ex, "{Object} an error occured during dropping the object", o.TargetFullName);
                }
                
            });

            // back to creation mode sorting
            objects.Reverse();

            NextAction?.Handle(objects, options);
        }

        public void DropTargetObjectIfExists(SqlObject obj)
        {
            // new context for thread safety
            ISocDbContext targetContext = new TargetContext(_configuration);

            if (obj.Exists(_configuration))
            {
                _logger.LogInformation("{Object} dropping object", obj.TargetFullName);
                targetContext.Database.ExecuteSqlRaw(obj.DeleteScript);
            }
        }
    }
}
