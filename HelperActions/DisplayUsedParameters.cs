using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.Contexts;
using SqlObjectCopy.DBActions;
using System.Collections.Generic;
using System.Linq;

namespace SqlObjectCopy.HelperActions
{
    internal class DisplayUsedParameters : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly ILogger logger;
        private readonly SocConfiguration configuration;

        public DisplayUsedParameters(ILogger<DisplayUsedParameters> logger, SocConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            
            var connection = configuration.Connections.Where(c => c.Selected).FirstOrDefault();

            if (connection != null)
            {
                logger.LogInformation("using source {SourceConnection}", connection.Source);
                logger.LogInformation("using target {TargetConnection}", connection.Target);
            }

            if (!string.IsNullOrEmpty(options.Schema)) { logger.LogInformation("using schema {Schema}", options.Schema); }
            if (!string.IsNullOrEmpty(options.ListFile)) { logger.LogInformation("using list file at {ListFilePath}", options.ListFile); }
            if (!string.IsNullOrEmpty(options.ObjectName)) { logger.LogInformation("using object {Object}", options.ObjectName); }
            if (!string.IsNullOrEmpty(options.DeltaColumnName)) { logger.LogInformation("using delta transport column {DeltaColumn}", options.DeltaColumnName); }
            if (options.Empty) { logger.LogInformation("using empty copy"); }
            if (!string.IsNullOrEmpty(options.TargetObjectName)) { logger.LogInformation("using target object name {TargetObject}", options.TargetObjectName); }

            NextAction?.Handle(objects, options);
        }
    }
}
