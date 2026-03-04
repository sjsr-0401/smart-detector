using OpenCvSharp;

namespace SmartDetector.Models;

/// <summary>검출 결과 하나를 나타내는 모델</summary>
public class DetectionResult
{
    public Rect BoundingBox { get; init; }
    public string Label { get; init; } = string.Empty;
    public int ClassId { get; init; }
    public float Confidence { get; init; }
}
