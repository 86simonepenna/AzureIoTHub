using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using Ninject;
using System.Web;
using Ninject.Web.Common;
using AzureIoTHub.Web.Services;
using Microsoft.Azure.Devices;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Ninject.Web.Common.OwinHost;
using Microsoft.AspNet.SignalR;
using Ninject.Web.Mvc;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;

[assembly: OwinStartup(typeof(AzureIoTHub.Web.Startup))]

namespace AzureIoTHub.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            CreateKernel();
            app.UseNinjectMiddleware(() => Kernel);            
            DependencyResolver.SetResolver(new NinjectDependencyResolver(Kernel));
            GlobalHost.DependencyResolver = new SignalRNinjectDependencyResolver(Kernel);
            app.MapSignalR();            
        }

        public static IKernel Kernel; 
        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        public static IKernel CreateKernel()
        {
            Kernel = new StandardKernel();
            try
            {
                Kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
                Kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

                string IotHubOwnerConnectionString = ConfigurationManager.AppSettings["IotHubOwnerConnectionString"];
                string IotHubServiceConnectionString = ConfigurationManager.AppSettings["IotHubServiceConnectionString"];
                string blobConnectionString = ConfigurationManager.AppSettings["BlobConnectionString"];

                Kernel.Bind<IBlobService>().To<BlobService>()
                    .InRequestScope()                    
                    .WithConstructorArgument("connectionString", blobConnectionString)
                    .WithConstructorArgument("containerName", "demo");

                Kernel.Bind<IServerIoTService>().To<ServerIoTService>()
                   .InRequestScope()                   
                   .WithConstructorArgument("serviceConnectionString", IotHubServiceConnectionString)
                   .WithConstructorArgument("registryConnectionString", IotHubOwnerConnectionString)
                   .WithConstructorArgument("transport", TransportType.Amqp);
                
                return Kernel;
            }
            catch(Exception ex)
            {
                Kernel.Dispose();
                throw;
            }
        }

       
    }

    public class SignalRNinjectDependencyResolver : DefaultDependencyResolver, Microsoft.AspNet.SignalR.IDependencyResolver
    {
        private readonly IKernel _kernel;

        public SignalRNinjectDependencyResolver(IKernel kernel)
        {
            _kernel = kernel;
        }

        public override object GetService(Type serviceType)
        {
            if (typeof(IConnection).Assembly == serviceType.Assembly) // Push DI for SignalR types to base
            {
                return base.GetService(serviceType);
            }
            else
            {
                return _kernel.TryGet(serviceType);
            }
        }

        public override IEnumerable<object> GetServices(Type serviceType)
        {
            if (typeof(IConnection).Assembly == serviceType.Assembly) // Push DI for SignalR types to base
            {
                return base.GetServices(serviceType);
            }
            else
            {
                return _kernel.GetAll(serviceType);
            }
        }
    }

}
