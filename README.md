# DialogueCalendarApp

## 프로젝트 개요
DialogueCalendarApp은 **DialogueEditor에서 만든 다이얼로그**와 **EventList.csv** 기반으로 이원화된 이벤트 및 다이얼로그를 효율적으로 관리하기 위한 **크로스 플랫폼 데스크탑 애플리케이션**입니다.  
UI는 **Avalonia**를 기반으로 제작되었으며, **MVVM 디자인 패턴**으로 구조화되어 있습니다.  

샘플용 **EventList**와 **다이얼로그 파일**, 그리고 **DialogueEditor**도 함께 업로드되어 있습니다.  

> ⚠️ 다이얼로그는 노드가 5개 이상일 경우 세이브가 제한되어 있습니다.  

---

## 주요 기능
- 이벤트 및 다이얼로그 관리
- 다이얼로그 에디터 연동
- 노드 기반 다이얼로그 편집
- MVVM 구조로 UI와 로직 분리
- 크로스 플랫폼 지원 (Windows, Linux, macOS)

---

## 프로젝트 구조
DialogueCalendarApp/
├─ Views/ # XAML UI 파일
│ ├─ MainWindow.axaml
│ ├─ EventEditWindow.axaml
├─ ViewModels/ # ViewModel 클래스
│ ├─ MainWindowViewModel.cs
│ ├─ EventEditViewModel.cs
├─ Models/ # 데이터 모델
├─ Assets/ # 이미지 및 리소스
├─ SampleData/ # 샘플 EventList.csv 및 다이얼로그
├─ Program.cs # 앱 진입점
└─ DialogueCalendarApp.csproj

## 실행 방법
1. .NET SDK 8.0 이상 설치
2. 프로젝트 클론
   ```bash
   git clone https://github.com/사용자/Avalonia_DialogueCalendarApp.git

참고 사항
현재 제공되는 EventList 및 다이얼로그는 샘플용입니다. 
다이얼로그 에디터와의 연동을 테스트하기 위해 포함되어 있으며, 노드 5개 이상인 다이얼로그는 저장되지 않습니다.
(업로드 예정)

향후 이벤트 수정 시 이벤트리스트에 적용 기능 업로드 예정
