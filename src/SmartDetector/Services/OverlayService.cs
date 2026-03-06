using OpenCvSharp;
using SmartDetector.Models;

namespace SmartDetector.Services;

/// <summary>검출 결과를 프레임 위에 오버레이</summary>
public static class OverlayService
{
    // 클래스별 색상 (보기 좋게)
    private static readonly Scalar[] Colors = GenerateColors(80);

    /// <summary>바운딩 박스 + 라벨 + 신뢰도 그리기</summary>
    public static void DrawDetections(Mat frame, List<DetectionResult> detections)
    {
        foreach (var det in detections)
        {
            var color = Colors[det.ClassId % Colors.Length];
            var box = det.BoundingBox;

            // 바운딩 박스
            Cv2.Rectangle(frame, box, color, 2);

            // 라벨 배경
            string label = $"{det.Label} {det.Confidence:P0}";
            var textSize = Cv2.GetTextSize(label, HersheyFonts.HersheySimplex, 0.6, 1, out int baseline);
            var labelBg = new Rect(box.X, box.Y - textSize.Height - 8, textSize.Width + 8, textSize.Height + 8);

            // 배경 위치 클램핑 (화면 밖 방지)
            if (labelBg.Y < 0) labelBg.Y = box.Y;

            Cv2.Rectangle(frame, labelBg, color, -1); // filled
            Cv2.PutText(frame, label,
                new Point(labelBg.X + 4, labelBg.Y + textSize.Height + 2),
                HersheyFonts.HersheySimplex, 0.6, Scalar.White, 1, LineTypes.AntiAlias);
        }
    }

    /// <summary>트래킹 결과 오버레이 (ID + 이동 경로)</summary>
    public static void DrawTrackedObjects(Mat frame, List<TrackedObject> tracks)
    {
        foreach (var track in tracks)
        {
            var color = Colors[track.Id % Colors.Length];
            var box = track.BoundingBox;

            // 바운딩 박스
            Cv2.Rectangle(frame, box, color, 2);

            // ID + 라벨
            string label = $"[{track.Id}] {track.Label} {track.Confidence:P0}";
            var textSize = Cv2.GetTextSize(label, HersheyFonts.HersheySimplex, 0.6, 1, out _);
            var labelBg = new Rect(box.X, box.Y - textSize.Height - 8, textSize.Width + 8, textSize.Height + 8);
            if (labelBg.Y < 0) labelBg.Y = box.Y;

            Cv2.Rectangle(frame, labelBg, color, -1);
            Cv2.PutText(frame, label,
                new Point(labelBg.X + 4, labelBg.Y + textSize.Height + 2),
                HersheyFonts.HersheySimplex, 0.6, Scalar.White, 1, LineTypes.AntiAlias);

            // 이동 경로 (Trail)
            if (track.Trail.Count >= 2)
            {
                for (int i = 1; i < track.Trail.Count; i++)
                {
                    // 최근일수록 진하게
                    int alpha = (int)(255.0 * i / track.Trail.Count);
                    var trailColor = new Scalar(color.Val0, color.Val1, color.Val2);
                    int thickness = i == track.Trail.Count - 1 ? 3 : 1;
                    Cv2.Line(frame, track.Trail[i - 1], track.Trail[i], trailColor, thickness, LineTypes.AntiAlias);
                }
            }
        }
    }

    /// <summary>FPS 표시</summary>
    public static void DrawFps(Mat frame, double fps)
    {
        string text = $"FPS: {fps:F1}";
        Cv2.PutText(frame, text, new Point(10, 30),
            HersheyFonts.HersheySimplex, 1.0, new Scalar(0, 255, 0), 2, LineTypes.AntiAlias);
    }

    /// <summary>검출 개수 표시</summary>
    public static void DrawCount(Mat frame, int count)
    {
        string text = $"Objects: {count}";
        Cv2.PutText(frame, text, new Point(10, 65),
            HersheyFonts.HersheySimplex, 0.8, new Scalar(0, 255, 255), 2, LineTypes.AntiAlias);
    }

    private static Scalar[] GenerateColors(int count)
    {
        var colors = new Scalar[count];
        for (int i = 0; i < count; i++)
        {
            // HSV → BGR (골고루 분포된 색상)
            using var hsv = new Mat(1, 1, MatType.CV_8UC3, new Scalar(i * 180 / count, 255, 200));
            using var bgr = new Mat();
            Cv2.CvtColor(hsv, bgr, ColorConversionCodes.HSV2BGR);
            var pixel = bgr.At<Vec3b>(0, 0);
            colors[i] = new Scalar(pixel.Item0, pixel.Item1, pixel.Item2);
        }
        return colors;
    }
}
