using SqlObjectCopy.DBActions;
using SqlObjectCopy.HelperActions;
using System;
using System.Collections.Generic;

namespace SqlObjectCopy.Pipelines
{
    internal class DefaultPipeline
    {
        private IDbAction actionQueue;
        private readonly IServiceProvider _serviceProvider;

        public DefaultPipeline(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            BuildActionQueue();
        }

        public void Start(Options options)
        {
            if (actionQueue != null)
            {
                actionQueue.Handle(new List<SqlObject>(), options);
            }
        }

        private void AttachToQueue(IDbAction actionQueue, Type T)
        {
            IDbAction latestAction = GetLatestAction(actionQueue);

            if (latestAction == null || !(_serviceProvider.GetService(T) is IDbAction attachAction))
            {
                throw new KeyNotFoundException("error on attaching action to queue");
            }

            latestAction.NextAction = attachAction;
        }

        private IDbAction GetLatestAction(IDbAction currentAction)
        {
            if (currentAction.NextAction == null)
            {
                return currentAction;
            } else
            {
                return GetLatestAction(currentAction.NextAction);
            }
        }

        private void BuildActionQueue() {
            // TODO: the first item should be the parameter output as info
            IDbAction EntryPoint;
            EntryPoint = _serviceProvider.GetService(typeof(ReadParameterObjectFile)) as IDbAction;
            AttachToQueue(EntryPoint, typeof(ReadObjectParameter));
            AttachToQueue(EntryPoint, typeof(SelectDatabaseConnection));
            AttachToQueue(EntryPoint, typeof(ReadSchemaParameter));
            AttachToQueue(EntryPoint, typeof(ReadObjectBaseInformation));
            AttachToQueue(EntryPoint, typeof(SortByDependencies));
            AttachToQueue(EntryPoint, typeof(CreateSchema));
            AttachToQueue(EntryPoint, typeof(DropConstraints));
            AttachToQueue(EntryPoint, typeof(DropSqlObjects));
            AttachToQueue(EntryPoint, typeof(CreateSqlObjects));
            AttachToQueue(EntryPoint, typeof(TransferData));
            AttachToQueue(EntryPoint, typeof(CreateConstraints));

            actionQueue = EntryPoint;
        }
    }
}
