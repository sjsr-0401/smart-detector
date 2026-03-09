using OpenCvSharp;
using SmartDetector.Models;

namespace SmartDetector.Services;

/// <summary>검출 위치 기반 히트맵 생성 + 시각화</summary>
public class HeatmapService
{
    private Mat? _accumulator;
    private int _frameCount;
    private readonly object _lock = new();

    /// <summary>히트맵 누적 프레임 수</summary>
    public int FrameCount => _frameCount;

    /// <summary>검출 결과를 히트맵에 누적</summary>
    public void Accumulate(List<DetectionResult> detections, int frameWidth, int frameHeight)
    {
        lock (_lock)
        {
            if (_accumulator == null || _accumulator.Width != frameWidth || _accumulator.Height != frameHeight)
            {
                _accumulator?.Dispose();
                _accumulator = Mat.Zeros(frameHeight, frameWidth, MatType.CV_32FC1);
                _frameCount = 0;
            }

            foreach (var det in detections)
            {
                var box = det.BoundingBox;
                int cx = Math.Clamp(box.X + box.Width / 2, 0, frameWidth - 1);
                int cy = Math.Clamp(box.Y + box.Height / 2, 0, frameHeight - 1);

                // 가우시안 스플랫: 중심에서 퍼지는 원형 가중치
                int radius = Math.Max(box.Width, box.Height) / 2;
                radius = Math.Clamp(radius, 15, 100);

                // ROI 영역 계산
                int x1 = Math.Max(cx - radius, 0);
                int y1 = Math.Max(cy - radius, 0);
                int x2 = Math.Min(cx + radius, frameWidth);
                int y2 = Math.Min(cy + radius, frameHeight);

                if (x2 <= x1 || y2 <= y1) continue;

                // 가우시안 스플랫 생성
                int roiW = x2 - x1;
                int roiH = y2 - y1;
                using var gaussian = new Mat(roiH, roiW, MatType.CV_32FC1);

                for (int y = 0; y < roiH; y++)
                {
                    for (int x = 0; x < roiW; x++)
                    {
                        double dx = (x1 + x - cx) / (double)radius;
                        double dy = (y1 + y - cy) / (double)radius;
                        double dist = dx * dx + dy * dy;
                        float weight = (float)(det.Confidence * Math.Exp(-dist * 2.0));
                        gaussian.Set(y, x, weight);
                    }
                }

                // 누적
                var roi = new Mat(_accumulator, new Rect(x1, y1, roiW, roiH));
                Cv2.Add(roi, gaussian, roi);
            }

            _frameCount++;
        }
    }

    /// <summary>히트맵을 컬러맵으로 변환하여 프레임에 오버레이</summary>
    public void DrawOverlay(Mat frame, double opacity = 0.4)
    {
        lock (_lock)
        {
            if (_accumulator == null || _frameCount == 0) return;

            // 정규화 (0~255)
            using var normalized = new Mat();
            Cv2.Normalize(_accumulator, normalized, 0, 255, NormTypes.MinMax);

            using var normalized8 = new Mat();
            normalized.ConvertTo(normalized8, MatType.CV_8UC1);

            // 컬러맵 적용 (JET: 파랑→초록→빨강)
            using var colormap = new Mat();
            Cv2.ApplyColorMap(normalized8, colormap, ColormapTypes.Jet);

            // 0인 부분은 투명하게 (마스크)
            using var mask = new Mat();
            Cv2.Threshold(normalized8, mask, 5, 255, ThresholdTypes.Binary);

            // 블렌딩
            using var blended = new Mat();
            Cv2.AddWeighted(frame, 1.0 - opacity, colormap, opacity, 0, blended);

            // 마스크 영역만 적용
            blended.CopyTo(frame, mask);
        }
    }

    /// <summary>히트맵 이미지를 독립적으로 생성 (저장/표시용)</summary>
    public Mat? GenerateHeatmapImage()
    {
        lock (_lock)
        {
            if (_accumulator == null || _frameCount == 0) return null;

            using var normalized = new Mat();
            Cv2.Normalize(_accumulator, normalized, 0, 255, NormTypes.MinMax);

            using var normalized8 = new Mat();
            normalized.ConvertTo(normalized8, MatType.CV_8UC1);

            var colormap = new Mat();
            Cv2.ApplyColorMap(normalized8, colormap, ColormapTypes.Jet);

            return colormap;
        }
    }

    /// <summary>히트맵 초기화</summary>
    public void Reset()
    {
        lock (_lock)
        {
            _accumulator?.Dispose();
            _accumulator = null;
            _frameCount = 0;
        }
    }

    /// <summary>히트맵 이미지를 파일로 저장</summary>
    public bool SaveHeatmap(string path)
    {
        var img = GenerateHeatmapImage();
        if (img == null) return false;

        try
        {
            Cv2.ImWrite(path, img);
            return true;
        }
        finally
        {
            img.Dispose();
        }
    }
}
