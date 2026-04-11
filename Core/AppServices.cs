using System;
using Microsoft.Extensions.DependencyInjection;

namespace ESCenter.Core
{
    internal static class AppServices
    {
        private static IServiceProvider? _provider;

        internal static IServiceProvider Provider =>
            _provider ?? throw new InvalidOperationException("AppServices not initialized.");

        internal static void Build(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();
            configure(services);
            _provider = services.BuildServiceProvider();
        }

        internal static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();
    }
}
