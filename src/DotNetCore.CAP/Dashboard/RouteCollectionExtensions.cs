using System;
using System.Text.RegularExpressions;

namespace DotNetCore.CAP.Dashboard
{
    public static class RouteCollectionExtensions
    {
        public static void AddRazorPage(
            this RouteCollection routes,
             string pathTemplate,
              Func<Match, RazorPage> pageFunc)
        {
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));
            if (pageFunc == null) throw new ArgumentNullException(nameof(pageFunc));

            routes.Add(pathTemplate, new RazorPageDispatcher(pageFunc));
        }

        public static void AddCommand(
             this RouteCollection routes,
             string pathTemplate,
              Func<DashboardContext, bool> command)
        {
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));
            if (command == null) throw new ArgumentNullException(nameof(command));

            routes.Add(pathTemplate, new CommandDispatcher(command));
        }

        public static void AddBatchCommand(
            this RouteCollection routes,
             string pathTemplate,
             Action<DashboardContext, string> command)
        {
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null) throw new ArgumentNullException(nameof(pathTemplate));
            if (command == null) throw new ArgumentNullException(nameof(command));

            routes.Add(pathTemplate, new BatchCommandDispatcher(command));
        }

        //public static void AddClientBatchCommand(
        //    this RouteCollection routes,
        //    string pathTemplate,
        //    [NotNull] Action<IBackgroundJobClient, string> command)
        //{
        //    if (command == null) throw new ArgumentNullException(nameof(command));

        //    routes.AddBatchCommand(pathTemplate, (context, jobId) =>
        //    {
        //        var client = new BackgroundJobClient(context.Storage);
        //        command(client, jobId);
        //    });
        //}

        //public static void AddRecurringBatchCommand(
        //    this RouteCollection routes,
        //    string pathTemplate,
        //    Action<RecurringJobManager, string> command)
        //{
        //    if (command == null) throw new ArgumentNullException(nameof(command));

        //    routes.AddBatchCommand(pathTemplate, (context, jobId) =>
        //    {
        //        var manager = new RecurringJobManager(context.Storage);
        //        command(manager, jobId);
        //    });
        //}
    }
}
