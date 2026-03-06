# SmartDetector 🎯

**Real-time Object Detection & Tracking Dashboard**

실시간 객체 검출 대시보드 — USB 카메라 + YOLOv8 + OpenCvSharp + WPF

![C#](https://img.shields.io/badge/C%23-.NET_8-239120?style=flat-square&logo=csharp)
![WPF](https://img.shields.io/badge/WPF-MVVM-68217A?style=flat-square&logo=windows)
![YOLO](https://img.shields.io/badge/YOLOv8-ONNX-00FFFF?style=flat-square)
![OpenCV](https://img.shields.io/badge/OpenCvSharp4-5C3EE8?style=flat-square&logo=opencv)

---

## Overview

SmartDetector는 USB 카메라에서 실시간으로 영상을 캡처하고, YOLOv8 모델로 객체를 검출하여 바운딩 박스와 클래스 레이블을 오버레이하는 WPF 데스크톱 앱입니다.

SmartDetector captures real-time video from a USB camera, runs YOLOv8 inference via ONNX Runtime, and overlays bounding boxes with class labels on a WPF dashboard.

## Architecture

```
┌─────────────┐     ┌──────────────┐     ┌────────────────┐     ┌──────────────┐
│ CameraService│────▶│DetectorService│────▶│OverlayService  │────▶│  WPF UI      │
│ (Capture)    │     │ (YOLOv8 ONNX)│     │ (Draw BBox)    │     │ (MVVM)       │
└─────────────┘     └──────────────┘     └────────────────┘     └──────────────┘
```

| Service | Responsibility |
|---------|---------------|
| **CameraService** | USB 카메라 캡처, 해상도/FPS 설정 |
| **DetectorService** | YOLOv8 ONNX 추론, NMS, 클래스 필터링 |
| **OverlayService** | 바운딩 박스, 레이블, 신뢰도 오버레이 |
| **TrackerService** | SORT 트래킹 — IoU 매칭, 칼만 예측, 트랙 관리 |
| **CountingService** | 카운팅 라인 통과 감지 (↑/↓ 방향별 카운트) |
| **MainViewModel** | MVVM 바인딩, 파이프라인 조율 |

## Tech Stack

- **Language:** C# / .NET 8
- **UI:** WPF / MVVM
- **Vision:** OpenCvSharp4, ONNX Runtime
- **Model:** YOLOv8n (80-class COCO)
- **Camera:** USB (DirectShow via OpenCV)

## Getting Started

### Prerequisites
- .NET 8 SDK
- USB Camera (any DirectShow-compatible)
- YOLOv8n ONNX model file

### Run
```bash
# Clone
git clone https://github.com/sjsr-0401/smart-detector.git
cd smart-detector

# Place ONNX model
# Download yolov8n.onnx → models/yolov8n.onnx

# Build & Run
dotnet run --project src/SmartDetector
```

## Roadmap

- [x] **Phase 1** — Real-time detection (YOLOv8 + USB camera)
- [x] **Phase 2** — SORT object tracking + counting line (↑↓ direction)
- [ ] **Phase 3** — Heatmap visualization, event alerts
- [ ] **Phase 4** — YOLO11/YOLO26 upgrade (NMS-free)

## Project Structure

```
smart-detector/
├── SmartDetector.sln
├── src/
│   ├── SmartDetector/
│   │   ├── Models/           # DetectionResult, CocoLabels
│   │   ├── Services/         # CameraService, DetectorService, OverlayService
│   │   ├── ViewModels/       # MainViewModel (MVVM)
│   │   ├── Converters/       # WPF value converters
│   │   ├── MainWindow.xaml   # UI layout
│   │   └── App.xaml
│   └── SmartDetector.Core/   # Shared abstractions
└── models/                   # ONNX model files
```

## License

MIT

---

*Built by [@sjsr-0401](https://github.com/sjsr-0401)*
