﻿using System;
using System.Collections.Generic;

namespace SqlObjectCopy
{
    public class SqlObject
    {
        private string targetSchemaName;
        private string targetObjectName;

        /// <summary>
        /// Basics
        /// </summary>
        public string SchemaName { get; set; }
        public string ObjectName { get; set; }
        public string FullName => SchemaName + "." + ObjectName;
        public SqlObjectType ObjectType { get; set; }

        public string TargetSchemaName { get => string.IsNullOrEmpty(targetSchemaName) ? SchemaName : targetSchemaName; set => targetSchemaName = value; }
        public string TargetObjectName { get => string.IsNullOrEmpty(targetObjectName) ? ObjectName : targetObjectName; set => targetObjectName = value; }
        public string TargetSafeName => "[" + TargetSchemaName + "].[" + TargetObjectName + "]";
        public string TargetFullName => TargetSchemaName + "." + TargetObjectName;

        /// <summary>
        /// Delta logic
        /// </summary>
        public string DeltaColumnName { get; set; }

        // TODO: is this a extension?
        public bool IsDeltaTransport => !string.IsNullOrWhiteSpace(DeltaColumnName) && ObjectType == SqlObjectType.Table;
        public string LastDeltaValue { get; set; }
        // the type of the delta value column
        public string DeltaColumnType { get; set; }

        /// <summary>
        /// The save name with [] for reserved SQL keyword protection
        /// </summary>
        public string SafeName => "[" + SchemaName + "].[" + ObjectName + "]";

        /// <summary>
        /// False if this object had some errors and is not valid anymore
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// Contains the last exception that occured for this object
        /// </summary>
        public Exception LastException { get; set; }


        // Scripts for DBOperations
        public string CreateScript { get; set; }
        public string DeleteScript { get; set; }
        public List<string> ConstraintScripts { get; set; }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="schemaName"></param>
        /// <param name="objectName"></param>
        /// <param name="type"></param>
        public SqlObject(string schemaName, string objectName, SqlObjectType type, string targetSchemaName, string targetObjectName)
        {
            _ = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
            _ = objectName ?? throw new ArgumentNullException(nameof(objectName));

            SchemaName = schemaName;
            ObjectName = objectName;
            ObjectType = type;
            Valid = true;
            TargetSchemaName = targetSchemaName ?? schemaName;
            TargetObjectName = targetObjectName ?? objectName;
        }
    }
}
