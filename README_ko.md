# Dreamine.MVVM.Locators

Dreamine MVVM 프레임워크에서 사용하는  
View ↔ ViewModel 자동 연결 인프라 라이브러리입니다.

이 라이브러리는 다음 두 가지 방식을 지원합니다.

* 규칙 기반 ViewModel 자동 생성
* DI Container 연동

\[➡️ English Version](README.md)

## 주요 기능

* View ↔ ViewModel 규칙 기반 매핑
* DI Container 연동 지원
* 수동 ViewModel 등록
* Assembly 자동 스캔 매핑
* ViewModel → View 역방향 생성

## 매핑 규칙

.Views.  →  .ViewModels.  
View     →  ViewModel

예시

DreamineApp.Views.Login.MainWindow  
→  
DreamineApp.ViewModels.Login.MainWindowViewModel

## DI 지원

Resolver가 존재하면

resolver.Resolve(vmType)

없으면

Activator.CreateInstance(vmType)

## 사용 방법

Resolver 등록

ViewModelLocator.RegisterResolver(new MyResolver());

수동 매핑

ViewModelLocator.Register(typeof(MainWindow), typeof(MainWindowViewModel));

ViewModel 생성

var vm = ViewModelLocator.Resolve(typeof(MainWindow));

View 생성

var view = ViewModelLocator.ResolveView(typeof(MainWindowViewModel));

자동 등록

ViewModelLocator.RegisterAll(Assembly.GetExecutingAssembly());

## License

MIT License

