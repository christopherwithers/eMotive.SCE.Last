//using System.Configuration;
//using Cache.Interfaces;
//using Cache.Objects;
//using eMotive.Managers.Interfaces;
//using eMotive.Managers.Objects;
//using eMotive.Repository.Interfaces;
//using eMotive.Repository.Objects;
//using eMotive.Search.Interfaces;
//using eMotive.Search.Objects;
//using eMotive.Services;
//using eMotive.Services.Interfaces;
//using eMotive.Services.Objects;
//using Ninject.Modules;
//using Ninject.Web.Common;

//namespace eMotive.IoCBindings.Ninject
//{
//    public class eMotiveModule : NinjectModule
//    {
//        public override void Load()
//        {
//            var connectionString = ConfigurationManager.ConnectionStrings["live"].ConnectionString ?? string.Empty;
//            var enableLogging = ConfigurationManager.AppSettings["Logging"] ?? "False";

//            Bind<ICache>().To<DevNullCache>().InRequestScope();

//            Bind<IUserRepository>().To<MySqlUserRepository>().InRequestScope().WithConstructorArgument("_connectionString", connectionString);
//            Bind<IRoleRepository>().To<MySqlRoleRepository>().InRequestScope().WithConstructorArgument("_connectionString", connectionString);
//            Bind<INewsRepository>().To<MySqlNewsRepository>().InRequestScope().WithConstructorArgument("_connectionString", connectionString);
//            Bind<IPageRepository>().To<MySqlPageRepository>().InRequestScope().WithConstructorArgument("_connectionString", connectionString);
//            Bind<ISessionRepository>().To<MySqlSessionRepository>().InRequestScope().WithConstructorArgument("_connectionString", connectionString);

//            Bind<IUserManager>().To<UserManager>().InRequestScope();
//            Bind<IRoleManager>().To<RoleManager>().InRequestScope();
//            Bind<INewsManager>().To<NewsManager>().InRequestScope();
//            Bind<IPageManager>().To<PageManager>().InRequestScope();
//            Bind<IPartialPageManager>().To<PartialPageManager>().InRequestScope();
//            Bind<ISessionManager>().To<SCESessionManager>().InRequestScope();
//            Bind<IGroupManager>().To<GroupManager>().InRequestScope();

//            //TODO: should the config be made into singleton?
//            Bind<IeMotiveConfigurationService>().To<eMotiveConfigurationServiceWebConfig>().InRequestScope();
//            Bind<INotificationService>().To<NotificationService>().InRequestScope().WithConstructorArgument("_connectionString", connectionString).WithConstructorArgument("_enableLogging", enableLogging);
//            Bind<IEmailService>().To<EmailService>().InRequestScope().WithConstructorArgument("_connectionString", connectionString);
//            Bind<IReportService>().To<ReportService>().InRequestScope().WithConstructorArgument("_connectionString", connectionString);
//            Bind<IDocumentManagerService>().To<DocumentManagerService>().InRequestScope().WithConstructorArgument("_connectionString", connectionString);
//            Bind<IAccountManager>().To<AccountManager>().InRequestScope().WithConstructorArgument("_connectionString", connectionString);
//            Bind<ISearchManager>().To<SearchManager>().InSingletonScope().WithConstructorArgument("_indexLocation", ConfigurationManager.AppSettings["LuceneIndex"]);
//        }
//    }
//}