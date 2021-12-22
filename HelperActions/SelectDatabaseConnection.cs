using Microsoft.Extensions.Logging;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.DBActions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SqlObjectCopy.HelperActions
{
    class SelectDatabaseConnection : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly SocConfiguration _configuration;
        private readonly ILogger<SelectDatabaseConnection> _logger;

        public SelectDatabaseConnection(SocConfiguration configuration, ILogger<SelectDatabaseConnection> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            if (_configuration.Connections.Length > 1)
            {
                _logger.LogInformation("found more than one database connection pair. Please select the correct one using the respective number");

                for (int i = 0; i < _configuration.Connections.Length; i++)
                {
                    _logger.LogInformation("{0}:\t{1}", i, _configuration.Connections[i].Source.ToString());
                    _logger.LogInformation(" \t{1}", _configuration.Connections[i].Target.ToString());
                    Console.WriteLine(string.Empty);
                }

                var choice = Console.ReadLine();

                if (int.TryParse(choice, out int conChoice) && conChoice >= 0 && conChoice < _configuration.Connections.Length)
                {
                    _configuration.Connections[conChoice].Selected = true;
                }
                else
                {
                    _logger.LogError("there is no connection with that number");
                    throw new ArgumentOutOfRangeException("invalid connection id chosen");
                }
            } else if (_configuration.Connections.Length == 1) {
                _configuration.Connections[0].Selected = true;
            } else
            {
                // error case
                _logger.LogError("no connections found. please check configuration file");
                throw new ArgumentNullException(nameof(_configuration.Connections));
            }

            NextAction?.Handle(objects, options);
        }
    }
}
