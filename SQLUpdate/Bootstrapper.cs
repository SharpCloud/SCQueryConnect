using Autofac;
using Autofac.Core;
using SCQueryConnect.Common.Helpers;
using SCQueryConnect.Common.Interfaces;
using SCQueryConnect.Helpers;
using SCQueryConnect.Interfaces;
using SCQueryConnect.Logging;
using SCQueryConnect.ViewModels;
using System;
using SCQueryConnect.Services;

namespace SCQueryConnect
{
    public static class Bootstrapper
    {
        private static IContainer _rootScope;

        public static void Start()
        {
            if (_rootScope != null)
            {
                return;
            }

            var builder = CreateContainerBuilder();
            _rootScope = builder.Build();
        }

        public static void Stop()
        {
            _rootScope.Dispose();
        }

        public static T Resolve<T>()
        {
            if (_rootScope == null)
            {
                throw new Exception("Bootstrapper hasn't been started!");
            }

            return _rootScope.Resolve<T>(new Parameter[0]);
        }

        public static T Resolve<T>(Parameter[] parameters)
        {
            if (_rootScope == null)
            {
                throw new Exception("Bootstrapper hasn't been started!");
            }

            return _rootScope.Resolve<T>(parameters);
        }

        private static ContainerBuilder CreateContainerBuilder()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<MainWindow>().InstancePerLifetimeScope();
            builder.RegisterType<MainViewModel>().As<IMainViewModel>().InstancePerLifetimeScope();
            builder.RegisterType<ArchitectureDetector>().As<IArchitectureDetector>().InstancePerLifetimeScope();
            builder.RegisterType<BatchPublishHelper>().As<IBatchPublishHelper>().InstancePerLifetimeScope();
            builder.RegisterType<ConnectionStringHelper>().As<IConnectionStringHelper>().InstancePerLifetimeScope();
            builder.RegisterType<ItemDataChecker>().As<IItemDataChecker>().InstancePerLifetimeScope();
            builder.RegisterType<DbConnectionFactory>().As<IDbConnectionFactory>().InstancePerLifetimeScope();
            builder.RegisterType<DpapiHelper>().As<IEncryptionHelper>().InstancePerLifetimeScope();
            builder.RegisterType<ExcelWriter>().As<IExcelWriter>().InstancePerLifetimeScope();
            builder.RegisterType<IOService>().As<IIOService>().InstancePerLifetimeScope();
            builder.RegisterType<RelationshipsDataChecker>().As<IRelationshipsDataChecker>().InstancePerLifetimeScope();
            builder.RegisterType<SharpCloudApiFactory>().As<ISharpCloudApiFactory>().InstancePerLifetimeScope();
            builder.RegisterType<MessageService>().As<IMessageService>().InstancePerLifetimeScope();
            builder.RegisterType<MultiDestinationLogger>().As<ILog>().InstancePerLifetimeScope();
            builder.RegisterType<QueryConnectHelper>().As<IQueryConnectHelper>().InstancePerLifetimeScope();
            builder.RegisterType<PanelsDataChecker>().As<IPanelsDataChecker>().InstancePerLifetimeScope();
            builder.RegisterType<ResourceUrlDataChecker>().As<IResourceUrlDataChecker>().InstancePerLifetimeScope();

            return builder;
        }
    }
}
