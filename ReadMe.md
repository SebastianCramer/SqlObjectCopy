# SQL object copy

Sql Object copy is a console application that enables you to clone SQL objects from one database to another including data transfer.

## What it can do

SOC can clone
  - Schemas
  - Tables
    - Constraints
    - Indices
    - Primary/Foreign Keys
    - Data/Content
  - Views
  - Procedures
  - Functions
  - Types

It can do this using
   - a whole Schema
   - a list of objects
   - single objects

Transports dependencies in the right order to prevent missing-key data.
Has delta support if the data supports it.
Can use differing schema/object names in the target system.
Transports in N threads where possible.

It is configurable through a appsettings.json configuration file.

### Installation
For the moment you have to build this project yourself.

### How to use
Open a console.
Navigate to the folder containing the soc.exe
```sh
$ cd c:/sqlobjectcopy
```
start the .exe using the required parameters (e.g. schema)
```sh
$ soc.exe -s iqs
```
In this example, iqs schema is the one we want to copy.

## Parameters
```
soc.exe [options]

  -s, --schema			The Schema to be copied to the test system
  -l, --list			Full path to a json file containing the full information of objects to copy
  -o, --object			Single file to be copied to the test system
  -e, --empty			Copy tables without content
  -d, --delta			DeltaColumn name for delta data transport
  -t, --targetobjectname	Target schema or schema.object name to use.
  --help			Display this help screen.
  --version			Display version information.
```

## List file
For the parameter `-l, --list` the list file has to contain the full object information in json syntax. 
```json
[
	{
		"SourceObject": "dbo.testtable",
		"TargetObject": "tst.testtable2",
		"DeltaColumn": "ID"
	}
]
```
The only mandatory field is `SourceObject`.
The other attributes can be removed when not used.
Since the root object is an array, you can add multiple objects in this file, each with it's own configuration attributes.

## Delta transfer
The SOC supports delta transfers. This means that it scans for the latest data in the target system and uses the defined key to get only new data and insert it.
> Delta transfer uses "SourceValue.ToString() > LatestTargetValue.ToString()" as rule.

In the following example the table my.Table is transported using the delta column "ID"
`soc.exe -o my.Table ID`

Using a file, delta transport is possible by adding the delta column name in each line after the object as described in the previous section.

## Error cases
Most of the time SOC will notify about errors in the log but will keep on cloning all other objects. A object that encounters an error will be rendered invalid and will no longer be processed.

## Configuration
The Soc is configured through a appsettings.json file that is in the same directory like the .exe. It looks like this:
```json

{
  "Connections": {
    "Source": "Server=mySourceServer;Database=mySourceDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;",
    "Target": "Server=myTargetServer;Database=myTargetDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;"
  },
  "Configuration": {
    "MaxParallelTransferThreads":  "5"
  }
}
```

Source - SQL connection string. The source of your objects to copy from.
Target - SQL connection string. The target of your objects to copy to.

MaxParallelTransferThreads - Number. The maximum amount of Threads copying at the same time. 5 is recommended.
