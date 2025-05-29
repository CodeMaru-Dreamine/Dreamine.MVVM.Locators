using Dreamine.MVVM.Interfaces.Locators;
using System;
using System.Reflection;
using System.Windows;

namespace Dreamine.MVVM.Locators
{
	/// <summary>
	/// View ↔ ViewModel 자동 연결 기능을 제공하며,
	/// DI Container 연동을 위한 Resolver 전략도 지원합니다.
	/// </summary>
	public static class ViewModelLocator
	{
		private static IViewModelResolver? _resolver;
		private static readonly Dictionary<Type, Type> _map = new();

		/// <summary>
		/// View ↔ ViewModel 타입 매핑을 수동 등록합니다.
		/// </summary>
		/// <param name="viewType">View 타입 (예: MainWindow.xaml)</param>
		/// <param name="viewModelType">ViewModel 타입 (예: MainWindowViewModel)</param>
		public static void Register(Type viewType, Type viewModelType)
		{
			if (!_map.ContainsKey(viewType))
				_map[viewType] = viewModelType;
		}

		/// <summary>
		/// 외부 DI Resolver를 등록합니다.
		/// </summary>
		/// <param name="resolver">IViewModelResolver 구현체</param>
		public static void RegisterResolver(IViewModelResolver resolver)
		{
			_resolver = resolver;
		}

		/// <summary>
		/// View 타입을 기반으로 ViewModel 인스턴스를 반환합니다.
		/// DI Resolver가 등록되어 있으면 우선 사용하며, 없을 경우 규칙 기반 생성 수행
		/// </summary>
		/// <param name="viewType">View 타입</param>
		/// <returns>ViewModel 인스턴스 (또는 null)</returns>
		public static object? Resolve(Type viewType)
		{
			if (_map.TryGetValue(viewType, out var mappedType))
			{
				return _resolver?.Resolve(mappedType) ?? Activator.CreateInstance(mappedType);
			}

			var viewName = viewType.FullName;
			if (viewName == null)
				return null;

			var asm = viewType.Assembly.FullName;

			// 🧠 View → ViewModel 매핑 규칙: .Views → .ViewModels, + "ViewModel" suffix
			if (!viewName.Contains(".Views."))
				return null;

			// 예: DreamineApp.Views.Login.MainWindow → DreamineApp.ViewModels.Login.MainWindowViewModel
			var viewModelName = viewName.Replace(".Views.", ".ViewModels.") + "ViewModel";

			var vmType = Type.GetType($"{viewModelName}, {asm}");
			return vmType != null
				? _resolver?.Resolve(vmType) ?? Activator.CreateInstance(vmType)
				: null;
		}

		/// <summary>
		/// 주어진 어셈블리에서 View ↔ ViewModel 매핑을 자동 등록합니다.
		/// 구조는 다음과 같은 규칙을 따릅니다:
		/// - .Views → .ViewModels + "ViewModel"
		/// - .Pages → .xaml.ViewModel
		/// - 동일 네임스페이스 내 View + ViewModel
		/// </summary>
		/// <param name="assembly">검색 대상 어셈블리</param>
		public static void RegisterAll(Assembly assembly)
		{
			var viewTypes = assembly.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract &&
					(t.FullName?.Contains(".Views.") == true || t.FullName?.Contains(".Pages.") == true));

			foreach (var viewType in viewTypes)
			{
				var viewName = viewType.FullName!;
				var candidateNames = new[]
									{
										viewName.Replace(".Views.", ".ViewModels.") + "ViewModel",          // Views, ViewModels, Models 등 폴더 분리 구조
										viewName + ".xaml.ViewModel",                                       // MainWindow.xaml, MainWindow.xaml.ViewModel.cs 등 묶음 구조
										viewName + "ViewModel",                                             // 네임스페이스 상 View와 ViewModel이 같은 위치에 있는 구조
									};

				foreach (var candidate in candidateNames)
				{
					var vmType = assembly.GetType(candidate);
					if (vmType != null)
					{
						ViewModelLocator.Register(viewType, vmType);
						System.Diagnostics.Debug.WriteLine($"[ViewModelLocator] 연결됨: {viewType.Name} ↔ {vmType.Name}");
						break;
					}
				}
			}
		}

	}
}
