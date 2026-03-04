using OpenCvSharp;

namespace SmartDetector.Services;

/// <summary>USB 카메라 캡처 서비스 (DirectShow)</summary>
public sealed class CameraService : IDisposable
{
    private VideoCapture? _capture;
    private readonly Mat _frame = new();
    private bool _disposed;

    public int FrameWidth { get; private set; }
    public int FrameHeight { get; private set; }
    public bool IsOpened => _capture?.IsOpened() == true;

    /// <summary>카메라 열기</summary>
    public bool Open(int cameraIndex = 0, int width = 1280, int height = 720)
    {
        _capture?.Dispose();
        _capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.DSHOW);

        if (!_capture.IsOpened())
            return false;

        _capture.Set(VideoCaptureProperties.FrameWidth, width);
        _capture.Set(VideoCaptureProperties.FrameHeight, height);

        FrameWidth = (int)_capture.Get(VideoCaptureProperties.FrameWidth);
        FrameHeight = (int)_capture.Get(VideoCaptureProperties.FrameHeight);

        return true;
    }

    /// <summary>프레임 하나 읽기</summary>
    public Mat? ReadFrame()
    {
        if (_capture == null || !_capture.IsOpened())
            return null;

        if (!_capture.Read(_frame) || _frame.Empty())
            return null;

        return _frame;
    }

    /// <summary>카메라 속성 조회</summary>
    public Dictionary<string, double> GetProperties()
    {
        if (_capture == null) return new();

        return new Dictionary<string, double>
        {
            ["Width"] = _capture.Get(VideoCaptureProperties.FrameWidth),
            ["Height"] = _capture.Get(VideoCaptureProperties.FrameHeight),
            ["FPS"] = _capture.Get(VideoCaptureProperties.Fps),
            ["Brightness"] = _capture.Get(VideoCaptureProperties.Brightness),
            ["Contrast"] = _capture.Get(VideoCaptureProperties.Contrast),
            ["Exposure"] = _capture.Get(VideoCaptureProperties.Exposure),
            ["Gain"] = _capture.Get(VideoCaptureProperties.Gain),
            ["WB_Temp"] = _capture.Get((VideoCaptureProperties)45),  // WB Temperature
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _capture?.Dispose();
        _frame.Dispose();
    }
}
