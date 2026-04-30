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

            Type[] allTypes = GetAllLoadableTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .ToArray();

            IEnumerable<Type> viewCandidates = allTypes.Where(ViewNamingConvention.IsLikelyViewType);

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
        /// View 타입에 대응하는 ViewModel 타입을 찾습니다.
        /// </summary>
        /// <param name="viewType">View 타입입니다.</param>
        /// <param name="cachedTypes">미리 확보한 타입 목록입니다.</param>
        /// <returns>대응되는 ViewModel 타입이며, 찾지 못한 경우 null 입니다.</returns>
        private static Type? FindViewModelType(Type viewType, Type[]? cachedTypes = null)
        {
            Type[] allTypes = cachedTypes ?? GetAllLoadableTypes();
            string[] candidateNames = ViewNamingConvention.GetViewModelTypeNameCandidates(viewType);

            foreach (string candidateName in candidateNames)
            {
                Type? match = allTypes.FirstOrDefault(t =>
                    t.FullName == candidateName ||
                    t.Name == candidateName);

                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        /// <summary>
        /// 현재 AppDomain에 로드된 모든 Assembly에서 로드 가능한 타입을 가져옵니다.
        /// </summary>
        /// <returns>로드 가능한 타입 목록입니다.</returns>
        private static Type[] GetAllLoadableTypes()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(GetLoadableTypes)
                .ToArray();
        }

        /// <summary>
        /// 지정한 Assembly에서 로드 가능한 타입만 가져옵니다.
        /// </summary>
        /// <param name="assembly">검색 대상 Assembly입니다.</param>
        /// <returns>로드 가능한 타입 목록입니다.</returns>
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null)!;
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        /// <summary>
        /// ViewModel 타입에 대응하는 View 타입을 찾습니다.
        /// </summary>
        /// <param name="viewModelType">ViewModel 타입입니다.</param>
        /// <returns>대응되는 View 타입이며, 찾지 못한 경우 null 입니다.</returns>
        private static Type? FindViewType(Type viewModelType)
        {
            Type[] allTypes = GetAllLoadableTypes();
            string[] candidateNames = ViewNamingConvention.GetViewTypeNameCandidates(viewModelType);

            foreach (string candidateName in candidateNames)
            {
                Type? match = allTypes.FirstOrDefault(t =>
                    t.FullName == candidateName ||
                    t.Name == candidateName);

                if (match != null)
                {
                    return match;
                }
            }

            return null;
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
    }
}