using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SqlObjectCopy.DBActions;
using SqlObjectCopy.HelperActions;
using SqlObjectCopy.Pipelines;
using SqlObjectCopy.Utilities;
using System;
using System.IO;

namespace SqlObjectCopy
{
    class Program
    {
        public static IConfiguration Configuration;
        public static IServiceProvider ServiceProvider;

        static void Main(string[] args)
        {
            // show super fancy intro screen
            ShowIntro();

            // try parse the arguments
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    // check parsed arguments
                    if (!OptionsValid(options))
                    {
                        return;
                    }

                    // internal setup
                    CreateLogger();
                    BuildConfiguration();
                    ConfigureServices();

                    // give the user output on params
                    WriteParamInfo(options);

                    if (!options.Unattended)
                    {
                        Log.Logger.Information("Copying with this tool potentially drops and recreates the database objects. Do you really want to do that? y/n:");
                        ConsoleKeyInfo answer = Console.ReadKey();

                        if (answer.Key != ConsoleKey.Y)
                        {
                            return;
                        }
                    }

                    // The action starts here
                    DefaultPipeline pipeline = new DefaultPipeline(ServiceProvider);
                    pipeline.Start(options);

                })
                .WithNotParsed(options =>
                {
                    Log.Logger.Error("error while parsing {0}", options.ToString());
                });

            Log.Logger.Information("soc finished");
#if DEBUG
            Console.ReadKey();
#endif
        }

        private static bool OptionsValid(Options options)
        {
            if (string.IsNullOrEmpty(options.Schema) && string.IsNullOrEmpty(options.ListFile) && string.IsNullOrEmpty(options.ObjectName))
            {
                Console.WriteLine("No arguments found, please use --help to see what arguments you can use.");
                return false;
            }
            else if (!string.IsNullOrEmpty(options.Schema) && options.Schema == "dbo")
            {
                Console.WriteLine("Cloning of dbo schema not allowed because it contains system objects. \r\n Please use an objectlist for cloning dbo objects.");
                return false;
            }
            else if (!string.IsNullOrEmpty(options.Schema) && options.Schema == "sys")
            {
                Console.WriteLine("Cloning of sys schema not allowed because it contains system objects. \r\n Please use an objectlist for cloning sys objects.");
                return false;
            }


            return true;
        }

        private static void ShowIntro()
        {
            string filepath = Path.Combine(Directory.GetParent(AppContext.BaseDirectory).FullName, "soc.txt");
            if (File.Exists(filepath))
            {
                Console.WriteLine(System.IO.File.ReadAllText(filepath));
            }
        }

        private static void WriteParamInfo(Options opt)
        {
            if (!string.IsNullOrEmpty(opt.Schema)) { Log.Information("using schema {0}", opt.Schema); }
            if (!string.IsNullOrEmpty(opt.ListFile)) { Log.Information("using list file at {0}", opt.ListFile); }
            if (!string.IsNullOrEmpty(opt.ObjectName)) { Log.Information("using object {0}", opt.ObjectName); }
            if (!string.IsNullOrEmpty(opt.DeltaColumnName)) { Log.Information("using delta transport column {0}", opt.DeltaColumnName); }
            if (opt.Empty) { Log.Information("using empty copy"); }
        }

        private static void BuildConfiguration()
        {
            Console.Write("building configuration...");
            // Build configuration
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();
            Console.WriteLine("done");
        }

        private static void ConfigureServices()
        {
            Console.Write("configuring services...");

            ServiceCollection services = new ServiceCollection();
            services.AddSingleton(Configuration);
            services.AddLogging(configure => configure.AddSerilog());
            
            services.AddScoped<ReadParameterObjectFile>();
            services.AddScoped<ScriptProvider>();
            services.AddScoped<ReadObjectBaseInformation>();
            services.AddScoped<DropSqlObjects>();
            services.AddScoped<SortByDependencies>();
            services.AddScoped<CreateSqlObjects>();
            services.AddScoped<TransferData>();
            services.AddScoped<CreateConstraints>();
            services.AddScoped<DropConstraints>();
            services.AddScoped<ReadObjectParameter>();
            services.AddScoped<ReadSchemaParameter>();
            services.AddScoped<CreateSchema>();

            ServiceProvider = services.BuildServiceProvider();
            Console.WriteLine("done");
        }

        private static void CreateLogger()
        {
            Log.Logger = new Serilog.LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Information()
                .CreateLogger();
        }
    }
}
