using Microsoft.Extensions.Logging;
using SqlObjectCopy.DBActions;
using System;
using System.Collections.Generic;

namespace SqlObjectCopy.HelperActions
{
    internal class AskSecurityQuestion : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly ILogger logger;

        public AskSecurityQuestion(ILogger<AskSecurityQuestion> logger)
        {
            this.logger = logger;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            if (!options.Unattended)
            {

                logger.LogInformation("Copying with this tool potentially drops and recreates the database objects. Do you really want to do that? y/n:");
                ConsoleKeyInfo answer = Console.ReadKey();
                Console.Write(Environment.NewLine);

                if (answer.Key != ConsoleKey.Y)
                {
                    logger.LogInformation("aborting...");
                    objects.ForEach(o => o.Valid = false);
                }
            }

            NextAction?.Handle(objects, options);
        }
    }
}
