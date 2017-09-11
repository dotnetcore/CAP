using System.Reflection;
using DotNetCore.CAP.Dashboard.Pages;
using DotNetCore.CAP.Processor.States;

namespace DotNetCore.CAP.Dashboard
{
    public static class DashboardRoutes
    {
        private static readonly string[] Javascripts =
        {
            "jquery-2.1.4.min.js",
            "bootstrap.min.js",
            "moment.min.js",
            "moment-with-locales.min.js",
            "d3.min.js",
            "d3.layout.min.js",
            "rickshaw.min.js",
            "jsonview.min.js",
            "cap.js"
        };

        private static readonly string[] Stylesheets =
        {
            "bootstrap.min.css",
            "rickshaw.min.css",
            "jsonview.min.css",
            "cap.css"
        };

        static DashboardRoutes()
        {
            Routes = new RouteCollection();
            Routes.AddRazorPage("/", x => new HomePage());
            Routes.Add("/stats", new JsonStats());

            #region Embedded static content

            Routes.Add("/js[0-9]+", new CombinedResourceDispatcher(
                "application/javascript",
                GetExecutingAssembly(),
                GetContentFolderNamespace("js"),
                Javascripts));

            Routes.Add("/css[0-9]+", new CombinedResourceDispatcher(
                "text/css",
                GetExecutingAssembly(),
                GetContentFolderNamespace("css"),
                Stylesheets));

            Routes.Add("/fonts/glyphicons-halflings-regular/eot", new EmbeddedResourceDispatcher(
                "application/vnd.ms-fontobject",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.eot")));

            Routes.Add("/fonts/glyphicons-halflings-regular/svg", new EmbeddedResourceDispatcher(
                "image/svg+xml",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.svg")));

            Routes.Add("/fonts/glyphicons-halflings-regular/ttf", new EmbeddedResourceDispatcher(
                "application/octet-stream",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.ttf")));

            Routes.Add("/fonts/glyphicons-halflings-regular/woff", new EmbeddedResourceDispatcher(
                "font/woff",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.woff")));

            Routes.Add("/fonts/glyphicons-halflings-regular/woff2", new EmbeddedResourceDispatcher(
                "font/woff2",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.woff2")));

            #endregion

            #region Razor pages and commands


            Routes.AddJsonResult("/published/message/(?<Id>.+)", x =>
            {
                var id = int.Parse(x.UriMatch.Groups["Id"].Value);
                var message = x.Storage.GetConnection().GetPublishedMessageAsync(id).GetAwaiter().GetResult();
                return message.Content;
            });
            Routes.AddJsonResult("/received/message/(?<Id>.+)", x =>
            {
                var id = int.Parse(x.UriMatch.Groups["Id"].Value);
                var message = x.Storage.GetConnection().GetReceivedMessageAsync(id).GetAwaiter().GetResult();
                return message.Content;
            });
            //Routes.AddRazorPage("/jobs/enqueued", x => new QueuesPage());
            //Routes.AddRazorPage(
            //    "/jobs/enqueued/fetched/(?<Queue>.+)",
            //     x => new FetchedJobsPage(x.Groups["Queue"].Value));

            //Routes.AddClientBatchCommand("/jobs/enqueued/delete", (client, jobId) => client.ChangeState(jobId, CreateDeletedState()));
            //Routes.AddClientBatchCommand("/jobs/enqueued/requeue", (client, jobId) => client.ChangeState(jobId, CreateEnqueuedState()));

            //Routes.AddRazorPage(
            //    "/jobs/enqueued/(?<Queue>.+)",
            //    x => new EnqueuedJobsPage(x.Groups["Queue"].Value));

            //Routes.AddRazorPage("/jobs/processing", x => new ProcessingJobsPage());
            //Routes.AddClientBatchCommand(
            //    "/jobs/processing/delete", 
            //    (client, jobId) => client.ChangeState(jobId, CreateDeletedState(), ProcessingState.StateName));

            //Routes.AddClientBatchCommand(
            //    "/jobs/processing/requeue",
            //    (client, jobId) => client.ChangeState(jobId, CreateEnqueuedState(), ProcessingState.StateName));

            //Routes.AddRazorPage("/jobs/scheduled", x => new ScheduledJobsPage());

            //Routes.AddClientBatchCommand(
            //    "/jobs/scheduled/enqueue", 
            //    (client, jobId) => client.ChangeState(jobId, CreateEnqueuedState(), ScheduledState.StateName));

            //Routes.AddClientBatchCommand(
            //    "/jobs/scheduled/delete",
            //    (client, jobId) => client.ChangeState(jobId, CreateDeletedState(), ScheduledState.StateName));

            Routes.AddPublishBatchCommand(
               "/published/requeue",
               (client, messageId) => client.Storage.GetConnection().ChangePublishedState(messageId, new ScheduledState()));
            Routes.AddPublishBatchCommand(
               "/received/requeue",
               (client, messageId) => client.Storage.GetConnection().ChangeReceivedState(messageId, new ScheduledState()));

            Routes.AddRazorPage(
                "/published/(?<StatusName>.+)",
                 x => new PublishedPage(x.Groups["StatusName"].Value));
            Routes.AddRazorPage(
               "/received/(?<StatusName>.+)",
                x => new ReceivedPage(x.Groups["StatusName"].Value));
            Routes.AddRazorPage("/subscribers", x => new SubscriberPage());
            //Routes.AddRazorPage("/jobs/failed", x => new FailedJobsPage());

            //Routes.AddClientBatchCommand(
            //    "/jobs/failed/requeue",
            //    (client, jobId) => client.ChangeState(jobId, CreateEnqueuedState(), FailedState.StateName));

            //Routes.AddClientBatchCommand(
            //    "/jobs/failed/delete",
            //    (client, jobId) => client.ChangeState(jobId, CreateDeletedState(), FailedState.StateName));

            //Routes.AddRazorPage("/jobs/deleted", x => new DeletedJobsPage());

            //Routes.AddClientBatchCommand(
            //    "/jobs/deleted/requeue",
            //    (client, jobId) => client.ChangeState(jobId, CreateEnqueuedState(), DeletedState.StateName));

            //Routes.AddRazorPage("/jobs/awaiting", x => new AwaitingJobsPage());
            //Routes.AddClientBatchCommand("/jobs/awaiting/enqueue", (client, jobId) => client.ChangeState(
            //    jobId, CreateEnqueuedState(), AwaitingState.StateName));
            //Routes.AddClientBatchCommand("/jobs/awaiting/delete", (client, jobId) => client.ChangeState(
            //    jobId, CreateDeletedState(), AwaitingState.StateName));

            //Routes.AddCommand(
            //    "/jobs/actions/requeue/(?<JobId>.+)",
            //    context =>
            //    {
            //        var client = new BackgroundJobClient(context.Storage);
            //        return client.ChangeState(context.UriMatch.Groups["JobId"].Value, CreateEnqueuedState());
            //    });

            //Routes.AddCommand(
            //    "/jobs/actions/delete/(?<JobId>.+)",
            //    context =>
            //    {
            //        var client = new BackgroundJobClient(context.Storage);
            //        return client.ChangeState(context.UriMatch.Groups["JobId"].Value, CreateDeletedState());
            //    });

            //Routes.AddRazorPage("/jobs/details/(?<JobId>.+)", x => new JobDetailsPage(x.Groups["JobId"].Value));

            //Routes.AddRazorPage("/recurring", x => new RecurringJobsPage());
            //Routes.AddRecurringBatchCommand(
            //    "/recurring/remove", 
            //    (manager, jobId) => manager.RemoveIfExists(jobId));

            //Routes.AddRecurringBatchCommand(
            //    "/recurring/trigger", 
            //    (manager, jobId) => manager.Trigger(jobId));

            //Routes.AddRazorPage("/servers", x => new ServersPage());
            //Routes.AddRazorPage("/retries", x => new RetriesPage());

            #endregion
        }

        public static RouteCollection Routes { get; }

        internal static string GetContentFolderNamespace(string contentFolder)
        {
            return $"{typeof(DashboardRoutes).Namespace}.Content.{contentFolder}";
        }

        internal static string GetContentResourceName(string contentFolder, string resourceName)
        {
            return $"{GetContentFolderNamespace(contentFolder)}.{resourceName}";
        }

        //private static DeletedState CreateDeletedState()
        //{
        //    return new DeletedState { Reason = "Triggered via Dashboard UI" };
        //}

        private static EnqueuedState CreateEnqueuedState()
        {
            return new EnqueuedState();// { Reason = "Triggered via Dashboard UI" };
        }

        private static Assembly GetExecutingAssembly()
        {
            return typeof(DashboardRoutes).GetTypeInfo().Assembly;
        }
    }
}
