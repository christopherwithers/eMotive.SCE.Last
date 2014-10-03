using System.Web.Mvc;
using eMotive.SCE.Controllers;
using eMotive.IoCBindings.Funq;
using eMotive.Models.Validation.Account;
using FluentValidation.Mvc;
using Funq;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common;
using ServiceStack.Configuration;
using ServiceStack.Mvc;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Validation;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;


namespace eMotive.SCE
{
    public class AppHost : AppHostBase
    {
        public AppHost() : base("MMI Web Services", typeof(Api.SessionService).Assembly) { }

        public override void Configure(Container container)
        {
            FunqBindings.Configure(container);

            container.Register<ICacheClient>(new MemoryCacheClient());
            container.Register<ISessionFactory>(c => new SessionFactory(c.Resolve<ICacheClient>()));

            
            JsConfig.DateHandler = JsonDateHandler.ISO8601;


       //     SetConfig(new EndpointHostConfig
           // {
              //  ServiceStackHandlerFactoryPath = "services",
              //  EnableFeatures = Feature.All.Remove(Feature.Metadata)
          //  });
          //  SetConfig(new EndpointHostConfig
           // {
             //   
           // });
        //    AuthService.Init(() => new AuthUserSession(), new IAuthProvider[] {new CredentialsAuthProvider()});


          //  Plugins.Add(new ValidationFeature());
            FluentValidationModelValidatorProvider.Configure();
        //    container.RegisterValidators(typeof(Models.Validation.User.UserValidator).Assembly);
            //container.Register(new LoginValidator());
            /*Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                    new IAuthProvider[] { 
                    new BasicAuthProvider(), //Sign-in with Basic Auth
                    new CredentialsAuthProvider(), //HTML Form post of UserName/Password credentials
                  }));*/
            /*    var appSettings = new AppSettings();
            Plugins.Add(new AuthFeature(this, new CustomUserSession(), 
                new IAuthProvider[]
                {
                    new CredentialsAuthProvider(appSettings), 
                }));*/

            ControllerBuilder.Current.SetControllerFactory(new FunqControllerFactory(container));
            ServiceStackController.CatchAllController = reqCtx => container.TryResolve<AccountController>();
        }
    }
    /*
    public class CustomCredentialsAuthProvider : CredentialsAuthProvider
    {
        public override bool TryAuthenticate(IServiceBase authService,
            string userName, string password)
        {
            return userName == "james" && password == "test";
        }
    }*/
}