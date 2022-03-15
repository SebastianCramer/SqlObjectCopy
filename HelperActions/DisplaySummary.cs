using Microsoft.Extensions.Logging;
using SqlObjectCopy.DBActions;
using System.Collections.Generic;
using System.Linq;

namespace SqlObjectCopy.HelperActions
{
    internal class DisplaySummary : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly ILogger logger;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public DisplaySummary(ILogger<DisplaySummary> logger)
        {
            this.logger = logger;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            logger.LogInformation("copied {successful}/{total} objects.", objects.Where(o => o.Valid).Count(), objects.Count);

            foreach (SqlObject o in objects.Where(o => !o.Valid))
            {
                logger.LogInformation("{Object} had error: {Error}", o.FullName, o.LastException.ToString());
            }

            NextAction?.Handle(objects, options);
        }
    }
}
