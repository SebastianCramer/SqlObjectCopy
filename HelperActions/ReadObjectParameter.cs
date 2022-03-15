using SqlObjectCopy.DBActions;
using System;
using System.Collections.Generic;

namespace SqlObjectCopy.HelperActions
{
    internal class ReadObjectParameter : IDbAction
    {
        public IDbAction NextAction { get; set; }

        public void Handle(List<SqlObject> objects, Options options)
        {
            if (!string.IsNullOrEmpty(options.ObjectName))
            {
                string schemaName = options.ObjectName[..options.ObjectName.IndexOf('.')];
                string objectName = options.ObjectName.Replace(schemaName + '.', string.Empty);

                if (!string.IsNullOrEmpty(schemaName) && !string.IsNullOrEmpty(objectName))
                {
                    var obj = new SqlObject(schemaName, objectName, SqlObjectType.Unknown)
                    {
                        DeltaColumnName = options.DeltaColumnName ?? null
                    };
                    objects.Add(obj);
                }
            }
            
            NextAction?.Handle(objects, options);
        }
    }
}
