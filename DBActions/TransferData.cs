using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Configuration;
using SqlObjectCopy.Contexts;
using SqlObjectCopy.Extensions;
using SqlObjectCopy.Models;
using SqlObjectCopy.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SqlObjectCopy.DBActions
{    internal class TransferData : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly SocConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ScriptProvider _scriptProvider;

        // for multithreading
        readonly SemaphoreSlim throttler;

        public TransferData(SocConfiguration configuration, ILogger<TransferData> logger, ScriptProvider scriptProvider)
        {
            throttler = new SemaphoreSlim(configuration.MaxParallelTransferThreads);

            _configuration = configuration;
            _logger = logger;
            _scriptProvider = scriptProvider;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            // skip if the empty parameter was used
            if (!options.Empty)
            {
                var asyncTransportList = new List<Task>();
                var allTransports = objects.Where(o => o.Valid && o.ObjectType == SqlObjectType.Table).ToList();

                // enqueue all transports that don't have references
                allTransports.Where(t => t.GetReferencedObjectNames(_configuration, _scriptProvider, _logger).Count == 0).ToList().ForEach(o =>
                {
                    try
                    {
                        throttler.Wait();
                        asyncTransportList.Add(TransferAsync(o));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{Object} an error occured while transferring data", o.FullName);
                        o.Valid = false;
                        o.LastException = ex;
                    }
                });

                // wait for all non-referenced to finish
                Task.WhenAll(asyncTransportList.ToArray()).Wait();

                // now all referenced objects in order
                allTransports.Where(t => t.GetReferencedObjectNames(_configuration, _scriptProvider, _logger).Count > 0).ToList().ForEach(o =>
                {
                    try
                    {
                        TransferAsync(o).Wait();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{Object} an error occured while transferring data", o.FullName);
                        o.Valid = false;
                        o.LastException = ex;
                    }
                });
            }

            NextAction?.Handle(objects, options);
        }

        public Task TransferAsync(SqlObject obj)
        {
            return Task.Run(() =>
            {
                _logger.LogInformation("{Object} starting transfer", obj.FullName);

                ISocDbContext sourceContext = new SourceContext(_configuration);
                ISocDbContext target = new TargetContext(_configuration);

                // copy command
                // source stuff
                SqlConnection con = new(sourceContext.Database.GetDbConnection().ConnectionString);

                // now the bulk copy
                string command = GetBulkStatement(obj);
                SqlCommand scmd = new(command.ToString(), con);

                // target stuff
                SqlBulkCopy copy = new(target.Database.GetDbConnection().ConnectionString, SqlBulkCopyOptions.KeepIdentity)
                {
                    BatchSize = 10000,
                    DestinationTableName = obj.FullName,
                    EnableStreaming = true
                };

                // for the status updates
                var totalRowCount = obj.SourceRowCount(_configuration);
                var startTime = DateTime.Now;

                copy.NotifyAfter = copy.BatchSize;
                copy.SqlRowsCopied += delegate (object sender, SqlRowsCopiedEventArgs args)
                {
                    float percentCopied = (float)100 / totalRowCount * args.RowsCopied;
                    var elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
                    double estimate = (elapsedSeconds / args.RowsCopied) * (totalRowCount - args.RowsCopied);

                    _logger.LogInformation("{Object} copied {RowsCopied}/{TotalRowCount} rows - {PercentCopied}% - eta {Estimate}", obj.FullName, args.RowsCopied, totalRowCount, Math.Round(percentCopied, 3), new TimeSpan(0, 0, (int)estimate));
                };

                try
                {
                    con.Open();
                    copy.WriteToServer(scmd.ExecuteReader());
                    _logger.LogInformation("{Object} transfer complete", obj.FullName);
                }
                catch (Exception ex)
                {
                    obj.Valid = false;
                    obj.LastException = ex;
                    _logger.LogError(ex, "{Object} an error occured on transferring data", obj.FullName);
                }
                finally
                {
                    copy.Close();
                    con.Close();
                    throttler.Release();
                }

            });
        }

        /// <summary>
        /// This builds the bulk statment with column names and ignores e.g. computed columns
        /// </summary>
        /// <param name="obj">The sql Object to copy</param>
        /// <returns>An select sql statement for bulk operation</returns>
        private string GetBulkStatement(SqlObject obj)
        {
            string fallbackCommand;

            if (obj.IsDeltaTransport)
            {
                fallbackCommand = FormattableStringFactory.Create("SELECT * FROM {0} WHERE {1} > {2}", obj.SafeName, obj.DeltaColumnName, obj.GetLastDeltaValue(_configuration)).ToString();
            }
            else
            {
                fallbackCommand = FormattableStringFactory.Create("SELECT * FROM {0}", obj.SafeName).ToString();
            }

            int objectID = obj.GetObjectID(_configuration);
            if (objectID == -1)
            {
                // you're our last hope
                return fallbackCommand;
            }

            ISocDbContext targetContext = new TargetContext(_configuration);
            IQueryable<Columns> cols = (from c in targetContext.Columns
                        where !c.IsComputed && c.ObjectId == objectID
                        select c).AsNoTracking();

            if (cols == null)
            {
                return fallbackCommand;
            }

            StringBuilder sb = new("SELECT ");

            foreach (Columns c in cols.ToList())
            {
                sb.Append('[');
                sb.Append(c.Name);
                sb.Append(']');
                sb.Append(',');
            }
            sb.Append("FROM ");
            sb.Append(obj.SafeName);

            if (obj.IsDeltaTransport)
            {
                sb.Append(" WHERE ");
                sb.Append(obj.DeltaColumnName);
                sb.Append(" > ");
                sb.Append(obj.GetLastDeltaValue(_configuration));
            }

            // get rid of the last comma while outputting
            return sb.ToString().Replace(",FROM", " FROM");
        }
    }
}
