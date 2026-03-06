using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using SmartDetector.Services;

namespace SmartDetector.ViewModels;

/// <summary>메인 뷰모델 — 카메라 + 검출 루프</summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly CameraService _camera = new();
    private readonly DetectorService _detector = new();
    private readonly TrackerService _tracker = new();
    private readonly CountingService _counter = new();
    private readonly Stopwatch _fpsTimer = new();
    private CancellationTokenSource? _cts;
    private bool _disposed;

    [ObservableProperty] private ImageSource? _frameImage;
    [ObservableProperty] private double _fps;
    [ObservableProperty] private int _objectCount;
    [ObservableProperty] private string _statusText = "대기 중";
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private float _confidenceThreshold = 0.5f;
    [ObservableProperty] private bool _trackingEnabled = true;
    [ObservableProperty] private bool _countingEnabled = true;
    [ObservableProperty] private int _trackedCount;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string _cameraInfo = "";

    /// <summary>모델 경로</summary>
    private string ModelPath => System.IO.Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "Assets", "models", "yolov8n.onnx");

    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning) return;

        // 카메라 열기
        StatusText = "카메라 연결 중...";
        if (!_camera.Open(0, 1280, 720))
        {
            StatusText = "카메라 연결 실패!";
            return;
        }

        CameraInfo = $"{_camera.FrameWidth}x{_camera.FrameHeight}";

        // 모델 로드
        StatusText = "모델 로딩 중...";
        if (!System.IO.File.Exists(ModelPath))
        {
            StatusText = $"모델 없음: {ModelPath}";
            return;
        }

        try
        {
            _detector.LoadModel(ModelPath);
        }
        catch (Exception ex)
        {
            StatusText = $"모델 로드 실패: {ex.Message}";
            return;
        }

        // 검출 루프 시작
        IsRunning = true;
        StatusText = "실행 중";
        _cts = new CancellationTokenSource();
        await Task.Run(() => DetectionLoop(_cts.Token));
    }

    [RelayCommand]
    private void Stop()
    {
        if (!IsRunning) return;
        _cts?.Cancel();
        _tracker.Reset();
        _counter.Reset();
        IsRunning = false;
        StatusText = "정지됨";
    }

    /// <summary>메인 검출 루프</summary>
    private void DetectionLoop(CancellationToken ct)
    {
        _fpsTimer.Restart();
        int frameCount = 0;

        while (!ct.IsCancellationRequested)
        {
            var frame = _camera.ReadFrame();
            if (frame == null) continue;

            // 검출
            _detector.ConfidenceThreshold = ConfidenceThreshold;
            var detections = _detector.Detect(frame);

            // 트래킹 또는 단순 검출 오버레이
            if (TrackingEnabled)
            {
                var tracked = _tracker.Update(detections);
                OverlayService.DrawTrackedObjects(frame, tracked);

                // 카운팅 라인
                if (CountingEnabled)
                {
                    _counter.Update(tracked, frame.Height);
                    _counter.DrawOverlay(frame);
                }
            }
            else
            {
                OverlayService.DrawDetections(frame, detections);
            }

            // FPS 계산
            frameCount++;
            double elapsed = _fpsTimer.Elapsed.TotalSeconds;
            if (elapsed >= 0.5)
            {
                double currentFps = frameCount / elapsed;
                _fpsTimer.Restart();
                frameCount = 0;

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Fps = currentFps;
                    ObjectCount = detections.Count;
                    TrackedCount = _tracker.ActiveTracks.Count;
                    TotalCount = _counter.TotalCount;
                });
            }

            OverlayService.DrawFps(frame, Fps);
            OverlayService.DrawCount(frame, detections.Count);

            // WPF 이미지로 변환 (UI 스레드)
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    FrameImage = frame.ToBitmapSource();
                }, DispatcherPriority.Render);
            }
            catch (TaskCanceledException) { break; }
        }
    }

    partial void OnConfidenceThresholdChanged(float value)
    {
        if (_detector.IsLoaded)
            _detector.ConfidenceThreshold = value;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts?.Cancel();
        _camera.Dispose();
        _detector.Dispose();
    }
}
