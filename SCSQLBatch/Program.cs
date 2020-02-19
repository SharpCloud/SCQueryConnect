using Autofac;
using SCQueryConnect.Common;
using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Common.Services;
using System.Configuration;

namespace SCSQLBatch
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = CreateContainerBuilder();
            using (var iocContainer = builder.Build())
            {
                var logic = iocContainer.Resolve<BatchLogic>();
                logic.Run().Wait();
            }
        }

        private static ContainerBuilder CreateContainerBuilder()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<ArchitectureDetector>().As<IArchitectureDetector>();
            builder.RegisterType<BatchLogic>();
            builder.RegisterType<CDataLicenceService>().As<ICDataLicenceService>();
            builder.RegisterType<ConfigurationReader>().As<IConfigurationReader>();
            builder.RegisterType<ConnectionStringHelper>().As<IConnectionStringHelper>();
            builder.RegisterType<ItemDataChecker>().As<IItemDataChecker>();
            builder.RegisterType<PanelsDataChecker>().As<IPanelsDataChecker>();
            builder.RegisterType<DpapiHelper>().As<IEncryptionHelper>();
            builder.RegisterType<DbConnectionFactory>().As<IDbConnectionFactory>();
            builder.RegisterType<ExcelWriter>().As<IExcelWriter>();
            builder.RegisterType<RelationshipsDataChecker>().As<IRelationshipsDataChecker>();
            builder.RegisterType<RelationshipsBuilder>().As<IRelationshipsBuilder>();
            builder.RegisterType<ResourceUrlDataChecker>().As<IResourceUrlDataChecker>();
            builder.RegisterType<SharpCloudApiFactory>().As<ISharpCloudApiFactory>();
            
            var logFile = ConfigurationManager.AppSettings[Constants.BatchLogFileKey];
            builder.RegisterType<ConsoleLogger>().As<ILog>().WithParameter("logFile", logFile);
            
            builder.RegisterType<QueryConnectHelper>().As<IQueryConnectHelper>();

            return builder;
        }
    }
}
