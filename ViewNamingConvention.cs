using System;
using System.Collections.Generic;
using System.Linq;

namespace Dreamine.MVVM.Locators
{
    /// <summary>
    /// Provides shared naming rules for resolving View and ViewModel type names.
    /// </summary>
    public static class ViewNamingConvention
    {
        private const string ViewModelsToken = "ViewModels";
        private const string ViewModelToken = "ViewModel";
        private const string ViewsToken = "Views";
        private const string ViewToken = "View";
        private const string PagesToken = "Pages";
        private const string PageModelsToken = "PageModels";
        private const string PageModelToken = "PageModel";
        private const string PageToken = "Page";
        private const string DotViewModelToken = ".ViewModel";
        private const string EmptyToken = "";

        /// <summary>
        /// Determines whether the specified type is likely to be a View type.
        /// </summary>
        /// <param name="type">The type to inspect.</param>
        /// <returns>True if the type matches a known View naming pattern; otherwise false.</returns>
        public static bool IsLikelyViewType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (!type.IsClass || type.IsAbstract)
            {
                return false;
            }

            string name = type.Name;

            return name.EndsWith("Window", StringComparison.Ordinal)
                || name.EndsWith("Page", StringComparison.Ordinal)
                || name.EndsWith("View", StringComparison.Ordinal)
                || name.EndsWith("Control", StringComparison.Ordinal)
                || name.EndsWith("Dialog", StringComparison.Ordinal)
                || name.EndsWith("Screen", StringComparison.Ordinal)
                || name.EndsWith("Panel", StringComparison.Ordinal);
        }

        /// <summary>
        /// Creates full type name candidates for a ViewModel that corresponds to the specified View type.
        /// </summary>
        /// <param name="viewType">The View type.</param>
        /// <returns>Candidate ViewModel full names ordered from most specific to least specific.</returns>
        public static string[] GetViewModelTypeNameCandidates(Type viewType)
        {
            ArgumentNullException.ThrowIfNull(viewType);

            string viewName = viewType.Name;
            string viewNamespace = viewType.Namespace ?? string.Empty;
            string rootNamespace = GetRootNamespace(viewNamespace);
            string candidateViewModelName = $"{viewName}ViewModel";

            List<string> results = [];

            foreach (string ns in BuildViewModelCandidateNamespaces(viewNamespace, rootNamespace))
            {
                AddIfNotEmpty(results, string.IsNullOrWhiteSpace(ns)
                    ? candidateViewModelName
                    : $"{ns}.{candidateViewModelName}");
            }

            AddIfNotEmpty(results, candidateViewModelName);

            return [.. results.Distinct(StringComparer.Ordinal)];
        }

        /// <summary>
        /// Creates full type name candidates for a View that corresponds to the specified ViewModel type.
        /// </summary>
        /// <param name="viewModelType">The ViewModel type.</param>
        /// <returns>Candidate View full names ordered from most specific to least specific.</returns>
        public static string[] GetViewTypeNameCandidates(Type viewModelType)
        {
            ArgumentNullException.ThrowIfNull(viewModelType);

            string viewModelName = viewModelType.Name;
            if (!viewModelName.EndsWith(ViewModelToken, StringComparison.Ordinal))
            {
                return [];
            }

            string viewName = viewModelName[..^ViewModelToken.Length];
            string viewModelNamespace = viewModelType.Namespace ?? string.Empty;
            string rootNamespace = GetRootNamespace(viewModelNamespace);

            List<string> results = [];

            foreach (string ns in BuildViewCandidateNamespaces(viewModelNamespace, rootNamespace))
            {
                AddIfNotEmpty(results, string.IsNullOrWhiteSpace(ns)
                    ? viewName
                    : $"{ns}.{viewName}");
            }

            AddIfNotEmpty(results, viewName);

            return [.. results.Distinct(StringComparer.Ordinal)];
        }

        /// <summary>
        /// Creates full type name candidates for a View from a ViewModel full type name.
        /// </summary>
        /// <param name="viewModelFullName">The full type name of the ViewModel.</param>
        /// <returns>Candidate View full names ordered from most specific to least specific.</returns>
        public static string[] GetViewTypeNameCandidates(string viewModelFullName)
        {
            if (string.IsNullOrWhiteSpace(viewModelFullName))
            {
                throw new ArgumentException("ViewModel full name must not be empty.", nameof(viewModelFullName));
            }

            string[] candidates =
            [
                viewModelFullName.Replace(ViewModelsToken, ViewsToken).Replace(ViewModelToken, ViewToken),
                viewModelFullName.Replace(ViewModelsToken, ViewsToken).Replace(ViewModelToken, EmptyToken),
                viewModelFullName.Replace(ViewModelsToken, PagesToken).Replace(ViewModelToken, ViewToken),
                viewModelFullName.Replace(ViewModelsToken, PagesToken).Replace(ViewModelToken, EmptyToken),
                viewModelFullName.Replace(DotViewModelToken, EmptyToken),
                viewModelFullName.Replace(DotViewModelToken, ViewToken),
                viewModelFullName.Replace(PageModelsToken, PagesToken).Replace(PageModelToken, PageToken),
                viewModelFullName.Replace(PageModelsToken, PagesToken).Replace(PageModelToken, EmptyToken)
            ];

            return candidates
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
                .Where(candidate => !string.Equals(candidate, viewModelFullName, StringComparison.Ordinal))
                .Where(candidate => !candidate.EndsWith(ViewModelToken, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] BuildViewModelCandidateNamespaces(string viewNamespace, string rootNamespace)
        {
            List<string> results = [];

            AddIfNotEmpty(results, viewNamespace);

            foreach (string candidate in BuildNamespaceReplacementCandidates(viewNamespace, ViewModelsToken))
            {
                AddIfNotEmpty(results, candidate);
            }

            foreach (string parentNs in ExpandParentNamespaces(viewNamespace))
            {
                AddIfNotEmpty(results, $"{parentNs}.ViewModels");
                AddIfNotEmpty(results, parentNs);
            }

            AddIfNotEmpty(results, $"{rootNamespace}.ViewModels");
            AddIfNotEmpty(results, rootNamespace);

            return [.. results.Distinct(StringComparer.Ordinal)];
        }

        private static string[] BuildViewCandidateNamespaces(string viewModelNamespace, string rootNamespace)
        {
            List<string> results = [];

            AddIfNotEmpty(results, viewModelNamespace);

            foreach (string candidate in BuildViewNamespaceCandidatesFromViewModel(viewModelNamespace))
            {
                AddIfNotEmpty(results, candidate);
            }

            foreach (string parentNs in ExpandParentNamespaces(viewModelNamespace))
            {
                AddIfNotEmpty(results, $"{parentNs}.Views");
                AddIfNotEmpty(results, $"{parentNs}.View");
                AddIfNotEmpty(results, $"{parentNs}.Pages");
                AddIfNotEmpty(results, $"{parentNs}.GUI");
                AddIfNotEmpty(results, $"{parentNs}.GUIs");
                AddIfNotEmpty(results, $"{parentNs}.Dialogs");
                AddIfNotEmpty(results, $"{parentNs}.Screens");
                AddIfNotEmpty(results, parentNs);
            }

            AddIfNotEmpty(results, $"{rootNamespace}.Views");
            AddIfNotEmpty(results, $"{rootNamespace}.View");
            AddIfNotEmpty(results, $"{rootNamespace}.Pages");
            AddIfNotEmpty(results, $"{rootNamespace}.GUI");
            AddIfNotEmpty(results, $"{rootNamespace}.GUIs");
            AddIfNotEmpty(results, $"{rootNamespace}.Dialogs");
            AddIfNotEmpty(results, $"{rootNamespace}.Screens");
            AddIfNotEmpty(results, rootNamespace);

            return [.. results.Distinct(StringComparer.Ordinal)];
        }

        private static IEnumerable<string> BuildNamespaceReplacementCandidates(string sourceNamespace, string replacement)
        {
            if (string.IsNullOrWhiteSpace(sourceNamespace))
            {
                yield break;
            }

            string[] tokens =
            [
                "Views",
                "View",
                "Pages",
                "Page",
                "Windows",
                "Window",
                "Dialogs",
                "Dialog",
                "GUI",
                "GUIs",
                "Screens",
                "Screen",
                "Controls",
                "Control"
            ];

            foreach (string token in tokens)
            {
                foreach (string candidate in ReplaceNamespaceToken(sourceNamespace, token, replacement))
                {
                    yield return candidate;
                }
            }
        }

        private static IEnumerable<string> BuildViewNamespaceCandidatesFromViewModel(string sourceNamespace)
        {
            if (string.IsNullOrWhiteSpace(sourceNamespace))
            {
                yield break;
            }

            string[] replacements =
            [
                "Views",
                "View",
                "Pages",
                "Page",
                "Windows",
                "Window",
                "Dialogs",
                "Dialog",
                "GUI",
                "GUIs",
                "Screens",
                "Screen",
                "Controls",
                "Control"
            ];

            foreach (string replacement in replacements)
            {
                foreach (string candidate in ReplaceNamespaceToken(sourceNamespace, ViewModelsToken, replacement))
                {
                    yield return candidate;
                }
            }
        }

        private static IEnumerable<string> ReplaceNamespaceToken(string sourceNamespace, string from, string to)
        {
            string middleToken = $".{from}.";
            if (sourceNamespace.Contains(middleToken, StringComparison.Ordinal))
            {
                yield return sourceNamespace.Replace(middleToken, $".{to}.", StringComparison.Ordinal);
            }

            string endToken = $".{from}";
            if (sourceNamespace.EndsWith(endToken, StringComparison.Ordinal))
            {
                yield return sourceNamespace[..^from.Length] + to;
            }
        }

        private static IEnumerable<string> ExpandParentNamespaces(string? ns)
        {
            if (string.IsNullOrWhiteSpace(ns))
            {
                return Array.Empty<string>();
            }

            List<string> results = [];
            string current = ns;

            while (!string.IsNullOrWhiteSpace(current))
            {
                int lastDot = current.LastIndexOf('.');
                if (lastDot < 0)
                {
                    break;
                }

                current = current[..lastDot];
                if (!string.IsNullOrWhiteSpace(current))
                {
                    results.Add(current);
                }
            }

            return results;
        }

        private static string GetRootNamespace(string ns)
        {
            if (string.IsNullOrWhiteSpace(ns))
            {
                return string.Empty;
            }

            int index = ns.IndexOf('.');
            return index < 0 ? ns : ns[..index];
        }

        private static void AddIfNotEmpty(List<string> list, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                list.Add(value);
            }
        }
    }
}