using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.Aspects;
using Microsoft.Extensions.Logging;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Utils
{
    /// <summary>
    /// Start-up checks over a built container.
    /// </summary>
    public static class NavigationServiceProviderExtensions
    {
        #region Constants

        private const string LOGGER_CATEGORY = "OutWit.Common.MVVM.Navigation";

        #endregion

        #region Functions

        /// <summary>
        /// Checks, after the modules have initialized, that every route targets a declared
        /// outlet and has a view, and that every contribution points at a registered route
        /// and a declared outlet. Problems are logged as warnings and returned; with
        /// <paramref name="throwOnProblems"/> they also throw — use that in Debug so a missing
        /// view shows up at start-up and not at the user's first click.
        /// </summary>
        /// <param name="provider">The built container.</param>
        /// <param name="throwOnProblems">Throw when anything is wrong.</param>
        /// <returns>The problems found; empty when all is well.</returns>
        /// <exception cref="InvalidOperationException">Problems were found and <paramref name="throwOnProblems"/> is set.</exception>
        public static IReadOnlyList<string> ValidateNavigation(this IServiceProvider provider, bool throwOnProblems = false)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var routes = provider.GetRequiredService<IRouteRegistry>();
            var navigation = provider.GetRequiredService<INavigationService>();
            var views = provider.GetRequiredService<IViewRegistry>();
            var viewFactory = provider.GetService<IViewFactory>();
            var contributions = provider.GetService<IContributionRegistry>();

            var problems = new List<string>();

            foreach (var route in routes.Routes)
            {
                if (!navigation.HasOutlet(route.Outlet))
                    problems.Add($"Route '{route.Key}' targets outlet '{route.Outlet}', which is not declared.");

                var hasView = viewFactory?.CanBuild(route.ViewModelType) ?? views.Contains(route.ViewModelType);
                if (!hasView)
                    problems.Add($"Route '{route.Key}': no view is known for {route.ViewModelType.FullName}.");

                if (NotifiesNothing(route.ViewModelType, out var property))
                    problems.Add($"Route '{route.Key}': {route.ViewModelType.FullName} marks '{property}' with [Notify] but does not implement INotifyPropertyChanged, so the property will never reach a binding. Derive from NotifyPropertyChangedBase or ModelBase.");
            }

            foreach (var group in routes.Groups)
            {
                if (!navigation.HasOutlet(group.Outlet))
                    problems.Add($"Group '{group.Key}' targets outlet '{group.Outlet}', which is not declared.");

                foreach (var routeKey in group.RouteKeys)
                {
                    if (!routes.Contains(routeKey))
                        problems.Add($"Group '{group.Key}' lists route '{routeKey}', which is not registered.");
                }
            }

            if (contributions != null)
            {
                foreach (var zoneName in contributions.Zones)
                {
                    foreach (var item in Flatten(contributions.Zone(zoneName).Items))
                    {
                        if (item.RouteKey != null && !routes.Contains(item.RouteKey) && !routes.ContainsGroup(item.RouteKey))
                            problems.Add($"Contribution '{item.Zone}/{item.Key}' navigates to '{item.RouteKey}', which is neither a route nor a group.");

                        if (item.Outlet != null && !navigation.HasOutlet(item.Outlet))
                            problems.Add($"Contribution '{item.Zone}/{item.Key}' targets outlet '{item.Outlet}', which is not declared.");
                    }
                }
            }

            if (problems.Count > 0)
            {
                var logger = provider.GetService<ILoggerFactory>()?.CreateLogger(LOGGER_CATEGORY);
                foreach (var problem in problems)
                    logger?.LogWarning("Navigation validation: {Problem}", problem);

                if (throwOnProblems)
                    throw new InvalidOperationException("Navigation validation failed:" + Environment.NewLine + string.Join(Environment.NewLine, problems));
            }

            return problems;
        }

        /// <summary>
        /// [Notify] raises through INotifyPropertyChanged; on a class that does not implement
        /// it the aspect finds nothing to raise on and does nothing at all. The property
        /// compiles, binds once to its initial value and then goes quiet — a failure with no
        /// error anywhere, which is exactly why it is worth a start-up check.
        /// </summary>
        private static bool NotifiesNothing(Type viewModelType, out string? property)
        {
            property = null;

            if (typeof(INotifyPropertyChanged).IsAssignableFrom(viewModelType))
                return false;

            var notifying = viewModelType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(candidate => candidate.GetCustomAttribute<NotifyAttribute>() != null);

            property = notifying?.Name;

            return notifying != null;
        }

        private static IEnumerable<ContributionItem> Flatten(IEnumerable<ContributionItem> items)
        {
            foreach (var item in items)
            {
                yield return item;

                foreach (var child in Flatten(item.Children))
                    yield return child;
            }
        }

        #endregion
    }
}
