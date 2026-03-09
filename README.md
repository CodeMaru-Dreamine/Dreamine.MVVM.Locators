# Dreamine.MVVM.Locators

View ↔ ViewModel binding infrastructure for WPF applications using the Dreamine MVVM framework.

This library provides a lightweight ViewModel locator implementation that supports both:

* Convention-based ViewModel resolution
* Dependency Injection integration

It allows Views to automatically obtain their corresponding ViewModel instances without breaking the MVVM pattern.

[➡️ 한국어 문서 보기](README\_ko.md)

## Features

* View ↔ ViewModel convention mapping
* Optional DI container integration
* Manual ViewModel registration
* Automatic assembly scanning
* Reverse View resolution from ViewModel

## Mapping Convention

.Views.  →  .ViewModels.
View     →  ViewModel

Example:

DreamineApp.Views.Login.MainWindow  
→  
DreamineApp.ViewModels.Login.MainWindowViewModel

## Dependency Injection Support

If a resolver exists:

resolver.Resolve(vmType)

Otherwise:

Activator.CreateInstance(vmType)

## Usage

Register Resolver

ViewModelLocator.RegisterResolver(new MyResolver());

Manual Mapping

ViewModelLocator.Register(typeof(MainWindow), typeof(MainWindowViewModel));

Resolve ViewModel

var vm = ViewModelLocator.Resolve(typeof(MainWindow));

Resolve View

var view = ViewModelLocator.ResolveView(typeof(MainWindowViewModel));

Auto Register

ViewModelLocator.RegisterAll(Assembly.GetExecutingAssembly());

## License

MIT License

