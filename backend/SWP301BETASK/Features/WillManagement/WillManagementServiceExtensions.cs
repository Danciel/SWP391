using Microsoft.Extensions.DependencyInjection.Extensions;
using SWP301BETASK.Features.WillManagement.Beneficiaries;
using SWP301BETASK.Features.WillManagement.Contracts;
using SWP301BETASK.Features.WillManagement.Dms;
using SWP301BETASK.Features.WillManagement.Executor;
using SWP301BETASK.Features.WillManagement.Services;

namespace SWP301BETASK.Features.WillManagement
{
    public static class WillManagementServiceExtensions
    {
        /// <summary>
        /// One line in Program.cs: builder.Services.AddWillManagement();
        /// Cross-module pieces use TryAdd, so Nhiên / Kiệt can register the real ICurrentUser / IAssetLookup
        /// BEFORE this call and theirs win.
        /// </summary>
        public static IServiceCollection AddWillManagement(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            services.TryAddScoped<ICurrentUser, DevHeaderCurrentUser>();
            services.TryAddScoped<IAssetLookup, PendingAssetLookup>();
            services.TryAddScoped<IAccountLookup, AccountLookup>();

            services.AddScoped<IWillService, WillService>();
            services.AddScoped<IBeneficiaryService, BeneficiaryService>();
            services.AddScoped<IWillSubmissionService, WillSubmissionService>();
            services.AddScoped<IExecutorService, ExecutorService>();
            services.AddScoped<IDmsService, DmsService>();

            services.AddHostedService<DmsTriggerWorker>();
            return services;
        }
    }
}
