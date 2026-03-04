# SmartDetector — 실시간 객체 검출 + 트래킹 대시보드

## 개요
USB 카메라(고정초점 1080p)를 이용한 실시간 객체 검출/트래킹 WPF 애플리케이션.

## 기술 스택
- **언어**: C# (.NET 8)
- **UI**: WPF (MVVM 패턴)
- **영상처리**: OpenCvSharp4
- **객체 검출**: YOLOv8 via ONNX Runtime
- **차트**: LiveCharts2 또는 OxyPlot

## 카메라 스펙
- HD USB CAMERA, 1920x1080, YUY2
- 고정초점 (AF 없음), WB 4600K
- DirectShow 백엔드 사용 (CAP_DSHOW)

## 핵심 기능

### Phase 1 — 기본 검출 (MVP)
- [x] 카메라 연결 + 실시간 영상 표시
- [x] YOLOv8n ONNX 모델 로드 + 추론
- [x] 바운딩 박스 + 라벨 + 신뢰도 오버레이
- [x] FPS 표시

### Phase 2 — 트래킹
- [ ] 객체 ID 부여 (SORT/DeepSORT 알고리즘)
- [ ] 이동 경로 시각화 (꼬리 그리기)
- [ ] 카운팅 라인 설정 + 입출입 카운트

### Phase 3 — 대시보드
- [ ] 실시간 객체 리스트 패널
- [ ] 시간대별 카운트 차트
- [ ] 히트맵 생성
- [ ] 체류 시간 표시

### Phase 4 — 이벤트 + 알림
- [ ] 관심 영역(ROI) 설정
- [ ] 특정 객체 감지 시 이벤트 로그
- [ ] 이벤트 발생 시 클립 자동 저장
- [ ] 텔레그램 알림 연동 (선택)

### Phase 5 — 설정 + 고급
- [ ] 카메라 설정 패널 (해상도, WB, 노출 등)
- [ ] 소프트웨어 샤프닝/색보정 옵션
- [ ] 모델 선택 (yolov8n/s/m)
- [ ] 감지 임계값 슬라이더
- [ ] 녹화 on/off

## 프로젝트 구조
```
SmartDetector/
├── SmartDetector.sln
├── src/
│   ├── SmartDetector/              # WPF 메인 앱
│   │   ├── App.xaml
│   │   ├── MainWindow.xaml
│   │   ├── ViewModels/
│   │   ├── Views/
│   │   ├── Models/
│   │   ├── Services/
│   │   │   ├── CameraService.cs    # OpenCvSharp 카메라 캡처
│   │   │   ├── DetectorService.cs  # YOLO ONNX 추론
│   │   │   ├── TrackerService.cs   # SORT 트래킹
│   │   │   └── OverlayService.cs   # 바운딩박스/경로 그리기
│   │   └── Assets/
│   │       └── models/             # ONNX 모델 파일
│   └── SmartDetector.Core/         # 핵심 로직 라이브러리
├── tests/
└── README.md
```

## NuGet 패키지
- OpenCvSharp4 (4.10+)
- OpenCvSharp4.runtime.win (x64)
- Microsoft.ML.OnnxRuntime (1.17+)
- CommunityToolkit.Mvvm
- LiveChartsCore.SkiaSharpView.WPF (선택)

## 빌드/실행
```bash
dotnet build
dotnet run --project src/SmartDetector
```
