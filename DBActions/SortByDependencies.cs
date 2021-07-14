using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlObjectCopy.Extensions;
using SqlObjectCopy.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlObjectCopy.DBActions
{
    internal class SortByDependencies : IDbAction
    {
        public IDbAction NextAction { get; set; }

        private readonly ScriptProvider _scriptProvider;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public SortByDependencies(ScriptProvider scriptProvider, ILogger<SortByDependencies> logger, IConfiguration configuration)
        {
            _scriptProvider = scriptProvider;
            _logger = logger;
            _configuration = configuration;
        }

        public void Handle(List<SqlObject> objects, Options options)
        {
            try
            {
                _logger.LogInformation("sorting objects by dependencies");
                objects = SortObjects(objects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while sorting objects");
                throw;
            }
            
            NextAction?.Handle(objects, options);
        }

        /// <summary>
        /// Sort the sql object to avoid FK and reference errors
        /// </summary>
        private List<SqlObject> SortObjects(List<SqlObject> obj)
        {
            List<SqlObject> sortedResult = new List<SqlObject>();
            AddDependendObjects(obj, sortedResult);

            return sortedResult;
        }

        // keeps us from stack overflows in recursive stuff
        private int iteration = 0;

        /// <summary>
        /// This function adds objects in the correct order for sql database creation
        /// taking account of foreign key references
        /// </summary>
        /// <param name="objectsToSort">The list of objects to sort</param>
        /// <param name="sortedObjects">The list that should contain the sorted result</param>
        private void AddDependendObjects(List<SqlObject> objectsToSort, List<SqlObject> sortedObjects)
        {
            iteration++;
            if (iteration == 100)
            {
                string openRefs = string.Empty;
                objectsToSort.ForEach(o => openRefs += string.Concat(o.GetReferencedObjectNames(_configuration, _scriptProvider, _logger), Environment.NewLine));

                _logger.LogWarning("Iteration {0} has been reached while sorting objects. Stopping operation.", iteration);
                _logger.LogWarning("This could most likely be due to referenced tables from other schemes. Please create those first. Missing references:");
                _logger.LogWarning(openRefs);

                return;
            }

            // go through each object that has not yet been added
            foreach (SqlObject o in objectsToSort.Except(sortedObjects))
            {
                // check if object has references
                List<string> refs = o.GetReferencedObjectNames(_configuration, _scriptProvider, _logger);

                // if this object doesn't have any references, just add it to the collection of sorted items and continue to the next
                if (refs.Count() == 0)
                {
                    sortedObjects.Add(o);
                    continue;
                }
                else // if the object has references
                {
                    int missingRefCount = 0;
                    int externalRefCount = 0;

                    // check if the references are all part of the sorted list already
                    foreach (string r in refs)
                    {
                        if (sortedObjects.Where(o => o.FullName == r).Count() == 0)
                        {
                            missingRefCount++;

                            // also check if this missing ref is part of the set at all
                            if (objectsToSort.Where(o => o.FullName == r).Count() == 0)
                            {
                                externalRefCount += 1;
                            }
                        }
                    }

                    // if all references are already in the sorted list, add this object and continue to the next
                    if (missingRefCount == 0)
                    {
                        sortedObjects.Add(o);
                        continue;
                    }
                    else if (missingRefCount == externalRefCount) // if all missing refs are not part of the list
                    {
                        // if this is the case let the user know
                        _logger.LogWarning("{0} has references that are not part of the object list. Program will try to create the object anyways.", o.FullName);
                        // and add the object anyways and pray
                        sortedObjects.Add(o);
                        continue;
                    }

                    // if there are missing refs that are not external, we'll take care in the next iteration

                }
            }

            // now that we've checked/added all objects in this iteration
            // check if there are objects left
            IEnumerable<SqlObject> rest = objectsToSort.Except(sortedObjects);

            // if there is noting left, just return
            if (rest.Count() == 0)
            {
                return;
            }
            else // if we have leftovers, we need the next iteration
            {
                AddDependendObjects(objectsToSort, sortedObjects);
            }
        }
    }
}
