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
		public static void Register(Type viewType, Type viewModelType)
		{
			if (!_map.ContainsKey(viewType))
				_map[viewType] = viewModelType;
		}

		/// <summary>
		/// 외부 DI Resolver를 등록합니다.
		/// </summary>
		public static void RegisterResolver(IViewModelResolver resolver)
		{
			_resolver = resolver;
		}

		/// <summary>
		/// View 타입을 기반으로 ViewModel 인스턴스를 반환합니다.
		/// DI Resolver가 등록되어 있으면 우선 사용하며, 없을 경우 규칙 기반 생성 수행
		/// </summary>
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
		/// 네임스페이스와 클래스 네이밍 컨벤션이 일치해야 합니다.
		/// </summary>
		public static void RegisterAll(Assembly assembly)
		{
			var viewTypes = assembly.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && t.FullName?.Contains(".Views.") == true);

			foreach (var viewType in viewTypes)
			{
				var viewModelName = viewType.FullName!
					.Replace(".Views.", ".ViewModels.")
					+ "ViewModel";

				var viewModelType = assembly.GetType(viewModelName);
				if (viewModelType != null)
				{
					ViewModelLocator.Register(viewType, viewModelType);
				}
			}
		}
	}
}
