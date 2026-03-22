using Dreamine.MVVM.Interfaces.Locators;
using System.Reflection;

namespace Dreamine.MVVM.Locators
{
    /// <summary>
    /// View 와 ViewModel 간의 타입 매핑 및 인스턴스 생성을 지원합니다.
    /// 플랫폼 종속 타입에 의존하지 않으며, 이름 및 네임스페이스 규칙 기반으로 동작합니다.
    /// </summary>
    public static class ViewModelLocator
    {
        private static readonly Dictionary<Type, Type> _map = new(32);
        private static IViewModelResolver? _resolver;

        /// <summary>
        /// View 와 ViewModel 타입 매핑을 수동 등록합니다.
        /// </summary>
        /// <param name="viewType">View 타입입니다.</param>
        /// <param name="viewModelType">ViewModel 타입입니다.</param>
        public static void Register(Type viewType, Type viewModelType)
        {
            ArgumentNullException.ThrowIfNull(viewType);
            ArgumentNullException.ThrowIfNull(viewModelType);

            if (!_map.ContainsKey(viewType))
            {
                _map[viewType] = viewModelType;
            }
        }

        /// <summary>
        /// 외부 ViewModel Resolver 를 등록합니다.
        /// </summary>
        /// <param name="resolver">Resolver 구현체입니다.</param>
        public static void RegisterResolver(IViewModelResolver resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            _resolver = resolver;
        }

        /// <summary>
        /// View 타입에 대응하는 ViewModel 인스턴스를 반환합니다.
        /// </summary>
        /// <param name="viewType">View 타입입니다.</param>
        /// <returns>생성된 ViewModel 인스턴스입니다. 찾지 못한 경우 null 입니다.</returns>
        public static object? Resolve(Type viewType)
        {
            ArgumentNullException.ThrowIfNull(viewType);

            if (_map.TryGetValue(viewType, out Type? mappedType))
            {
                return CreateInstance(mappedType);
            }

            Type? resolvedType = FindViewModelType(viewType);
            if (resolvedType == null)
            {
                return null;
            }

            Register(viewType, resolvedType);
            return CreateInstance(resolvedType);
        }

        /// <summary>
        /// ViewModel 타입에 대응하는 View 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="viewModelType">ViewModel 타입입니다.</param>
        /// <returns>생성된 View 인스턴스입니다. 찾지 못한 경우 null 입니다.</returns>
        public static object? ResolveView(Type viewModelType)
        {
            ArgumentNullException.ThrowIfNull(viewModelType);

            Type? viewType = FindViewType(viewModelType);
            return viewType != null
                ? Activator.CreateInstance(viewType)
                : null;
        }

        /// <summary>
        /// 지정된 어셈블리에서 View 와 ViewModel 매핑을 자동 등록합니다.
        /// </summary>
        /// <param name="assembly">검색 대상 어셈블리입니다.</param>
        public static void RegisterAll(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            Type[] allTypes = [.. assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract)];

            IEnumerable<Type> viewCandidates = allTypes.Where(IsLikelyViewType);

            foreach (Type viewType in viewCandidates)
            {
                if (_map.ContainsKey(viewType))
                {
                    continue;
                }

                Type? viewModelType = FindViewModelType(viewType, allTypes);
                if (viewModelType != null)
                {
                    Register(viewType, viewModelType);
                }
            }
        }

        /// <summary>
        /// 현재 타입이 View 로 간주될 가능성이 높은지 판별합니다.
        /// 공용 라이브러리이므로 플랫폼 타입 상속 여부 대신 이름 규칙을 사용합니다.
        /// </summary>
        /// <param name="type">검사 대상 타입입니다.</param>
        /// <returns>View 후보이면 true, 아니면 false 입니다.</returns>
        private static bool IsLikelyViewType(Type type)
        {
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
        /// View 타입에 대응하는 ViewModel 타입을 찾습니다.
        /// </summary>
        /// <param name="viewType">View 타입입니다.</param>
        /// <param name="cachedTypes">미리 확보한 타입 목록입니다.</param>
        /// <returns>대응되는 ViewModel 타입이며, 찾지 못한 경우 null 입니다.</returns>
        private static Type? FindViewModelType(Type viewType, Type[]? cachedTypes = null)
        {
            Assembly assembly = viewType.Assembly;
            Type[] allTypes = cachedTypes ?? assembly.GetTypes();

            string viewName = viewType.Name;
            string viewNamespace = viewType.Namespace ?? string.Empty;
            string rootNamespace = GetRootNamespace(viewNamespace);

            string candidateViewModelName = $"{viewName}ViewModel";

            IReadOnlyList<string> candidateNamespaces =
                BuildViewModelCandidateNamespaces(viewNamespace, rootNamespace);

            foreach (string ns in candidateNamespaces)
            {
                string fullName = string.IsNullOrWhiteSpace(ns)
                    ? candidateViewModelName
                    : $"{ns}.{candidateViewModelName}";

                Type? match = allTypes.FirstOrDefault(t => t.FullName == fullName);
                if (match != null)
                {
                    return match;
                }
            }

            // 마지막 fallback: 이름만 일치하는 타입 검색
            return allTypes.FirstOrDefault(t => t.Name == candidateViewModelName);
        }

        /// <summary>
        /// ViewModel 타입에 대응하는 View 타입을 찾습니다.
        /// </summary>
        /// <param name="viewModelType">ViewModel 타입입니다.</param>
        /// <returns>대응되는 View 타입이며, 찾지 못한 경우 null 입니다.</returns>
        private static Type? FindViewType(Type viewModelType)
        {
            string viewModelName = viewModelType.Name;
            if (!viewModelName.EndsWith("ViewModel", StringComparison.Ordinal))
            {
                return null;
            }

            Assembly assembly = viewModelType.Assembly;
            Type[] allTypes = assembly.GetTypes();

            string viewName = viewModelName[..^"ViewModel".Length];
            string viewModelNamespace = viewModelType.Namespace ?? string.Empty;
            string rootNamespace = GetRootNamespace(viewModelNamespace);

            IReadOnlyList<string> candidateNamespaces =
                BuildViewCandidateNamespaces(viewModelNamespace, rootNamespace);

            foreach (string ns in candidateNamespaces)
            {
                string fullName = string.IsNullOrWhiteSpace(ns)
                    ? viewName
                    : $"{ns}.{viewName}";

                Type? match = allTypes.FirstOrDefault(t => t.FullName == fullName);
                if (match != null)
                {
                    return match;
                }
            }

            // 마지막 fallback: 이름만 일치하는 타입 검색
            return allTypes.FirstOrDefault(t => t.Name == viewName);
        }

        /// <summary>
        /// View 네임스페이스를 기준으로 ViewModel 후보 네임스페이스 목록을 구성합니다.
        /// 루트와 부모 네임스페이스까지 포함하여 폭넓게 탐색합니다.
        /// </summary>
        /// <param name="viewNamespace">View 네임스페이스입니다.</param>
        /// <param name="rootNamespace">루트 네임스페이스입니다.</param>
        /// <returns>탐색 후보 네임스페이스 목록입니다.</returns>
        private static string[] BuildViewModelCandidateNamespaces(
            string viewNamespace,
            string rootNamespace)
        {
            List<string> results = [];

            // 1. 동일 네임스페이스
            AddIfNotEmpty(results, viewNamespace);

            // 2. 현재 네임스페이스에서 View 계열 토큰을 ViewModels 로 치환
            foreach (string candidate in BuildNamespaceReplacementCandidates(viewNamespace, "ViewModels"))
            {
                AddIfNotEmpty(results, candidate);
            }

            // 3. 부모 네임스페이스들 기반 후보
            foreach (string parentNs in ExpandParentNamespaces(viewNamespace))
            {
                AddIfNotEmpty(results, $"{parentNs}.ViewModels");
                AddIfNotEmpty(results, parentNs);
            }

            // 4. 루트 기준 후보
            AddIfNotEmpty(results, $"{rootNamespace}.ViewModels");
            AddIfNotEmpty(results, rootNamespace);

            return [.. results.Distinct(StringComparer.Ordinal)];
            //return results.Distinct(StringComparer.Ordinal).ToArray();
        }

        /// <summary>
        /// ViewModel 네임스페이스를 기준으로 View 후보 네임스페이스 목록을 구성합니다.
        /// </summary>
        /// <param name="viewModelNamespace">ViewModel 네임스페이스입니다.</param>
        /// <param name="rootNamespace">루트 네임스페이스입니다.</param>
        /// <returns>탐색 후보 네임스페이스 목록입니다.</returns>
        private static string[] BuildViewCandidateNamespaces(
            string viewModelNamespace,
            string rootNamespace)
        {
            List<string> results = [];

            // 1. 동일 네임스페이스
            AddIfNotEmpty(results, viewModelNamespace);

            // 2. ViewModels -> 여러 View 계열 토큰으로 치환
            foreach (string candidate in BuildViewNamespaceCandidatesFromViewModel(viewModelNamespace))
            {
                AddIfNotEmpty(results, candidate);
            }

            // 3. 부모 네임스페이스들 기반 후보
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

            // 4. 루트 기준 후보
            AddIfNotEmpty(results, $"{rootNamespace}.Views");
            AddIfNotEmpty(results, $"{rootNamespace}.View");
            AddIfNotEmpty(results, $"{rootNamespace}.Pages");
            AddIfNotEmpty(results, $"{rootNamespace}.GUI");
            AddIfNotEmpty(results, $"{rootNamespace}.GUIs");
            AddIfNotEmpty(results, $"{rootNamespace}.Dialogs");
            AddIfNotEmpty(results, $"{rootNamespace}.Screens");
            AddIfNotEmpty(results, rootNamespace);

            return [.. results.Distinct(StringComparer.Ordinal)];
            //return results.Distinct(StringComparer.Ordinal).ToArray();
        }

        /// <summary>
        /// View 관련 네임스페이스 토큰을 ViewModels 로 치환한 후보를 생성합니다.
        /// </summary>
        /// <param name="sourceNamespace">원본 네임스페이스입니다.</param>
        /// <param name="replacement">치환 대상 문자열입니다.</param>
        /// <returns>치환된 후보 네임스페이스 목록입니다.</returns>
        private static IEnumerable<string> BuildNamespaceReplacementCandidates(
            string sourceNamespace,
            string replacement)
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

        /// <summary>
        /// ViewModel 네임스페이스에서 View 계열 후보 네임스페이스를 생성합니다.
        /// </summary>
        /// <param name="sourceNamespace">원본 ViewModel 네임스페이스입니다.</param>
        /// <returns>View 후보 네임스페이스 목록입니다.</returns>
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
                foreach (string candidate in ReplaceNamespaceToken(sourceNamespace, "ViewModels", replacement))
                {
                    yield return candidate;
                }
            }
        }

        /// <summary>
        /// 네임스페이스 내 특정 토큰을 다른 토큰으로 치환한 후보를 생성합니다.
        /// </summary>
        /// <param name="sourceNamespace">원본 네임스페이스입니다.</param>
        /// <param name="from">기존 토큰입니다.</param>
        /// <param name="to">치환할 토큰입니다.</param>
        /// <returns>치환 결과 목록입니다.</returns>
        private static IEnumerable<string> ReplaceNamespaceToken(
            string sourceNamespace,
            string from,
            string to)
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

        /// <summary>
        /// 부모 네임스페이스 후보들을 루트 방향으로 확장합니다.
        /// 예: A.B.C.D -> A.B.C, A.B, A
        /// </summary>
        /// <param name="ns">원본 네임스페이스입니다.</param>
        /// <returns>부모 네임스페이스 목록입니다.</returns>
        private static IEnumerable<string> ExpandParentNamespaces(string ns)
        {
            if (string.IsNullOrWhiteSpace(ns))
            {
                yield break;
            }

            string current = ns;
            while (true)
            {
                int lastDot = current.LastIndexOf('.');
                if (lastDot < 0)
                {
                    yield break;
                }

                current = current[..lastDot];
                if (!string.IsNullOrWhiteSpace(current))
                {
                    yield return current;
                }
            }
        }

        /// <summary>
        /// 네임스페이스 문자열에서 루트 네임스페이스를 반환합니다.
        /// </summary>
        /// <param name="ns">네임스페이스 문자열입니다.</param>
        /// <returns>루트 네임스페이스입니다.</returns>
        private static string GetRootNamespace(string ns)
        {
            if (string.IsNullOrWhiteSpace(ns))
            {
                return string.Empty;
            }

            int index = ns.IndexOf('.');
            return index < 0 ? ns : ns[..index];
        }

        /// <summary>
        /// 지정한 타입의 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="type">생성 대상 타입입니다.</param>
        /// <returns>생성된 인스턴스입니다.</returns>
        private static object? CreateInstance(Type type)
        {
            return _resolver?.Resolve(type) ?? Activator.CreateInstance(type);
        }

        /// <summary>
        /// 값이 비어 있지 않을 때만 목록에 추가합니다.
        /// </summary>
        /// <param name="list">대상 목록입니다.</param>
        /// <param name="value">추가할 값입니다.</param>
        private static void AddIfNotEmpty(List<string> list, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                list.Add(value);
            }
        }
    }
}