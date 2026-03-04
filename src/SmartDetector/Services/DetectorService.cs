using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using SmartDetector.Models;

namespace SmartDetector.Services;

/// <summary>YOLOv8 ONNX 추론 서비스</summary>
public sealed class DetectorService : IDisposable
{
    private InferenceSession? _session;
    private bool _disposed;

    // YOLOv8 입력 크기
    private const int InputWidth = 640;
    private const int InputHeight = 640;

    // 신뢰도 임계값
    public float ConfidenceThreshold { get; set; } = 0.5f;
    public float NmsThreshold { get; set; } = 0.45f;

    public bool IsLoaded => _session != null;

    /// <summary>ONNX 모델 로드</summary>
    public void LoadModel(string modelPath)
    {
        _session?.Dispose();
        var options = new SessionOptions();
        options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        _session = new InferenceSession(modelPath, options);
    }

    /// <summary>프레임에서 객체 검출</summary>
    public List<DetectionResult> Detect(Mat frame)
    {
        if (_session == null)
            return new List<DetectionResult>();

        // 전처리: BGR → RGB, 리사이즈, 정규화
        var inputTensor = Preprocess(frame);

        // 추론
        var inputName = _session.InputNames[0];
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
        };

        using var results = _session.Run(inputs);
        var output = results.First().AsTensor<float>();

        // 후처리: YOLOv8 출력 파싱
        return Postprocess(output, frame.Width, frame.Height);
    }

    /// <summary>전처리: 이미지 → 텐서</summary>
    private DenseTensor<float> Preprocess(Mat frame)
    {
        // 리사이즈
        using var resized = new Mat();
        Cv2.Resize(frame, resized, new Size(InputWidth, InputHeight));

        // BGR → RGB + 정규화 (0~1)
        using var rgb = new Mat();
        Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

        var tensor = new DenseTensor<float>(new[] { 1, 3, InputHeight, InputWidth });

        unsafe
        {
            var ptr = (byte*)rgb.Data.ToPointer();
            for (int y = 0; y < InputHeight; y++)
            {
                for (int x = 0; x < InputWidth; x++)
                {
                    int idx = (y * InputWidth + x) * 3;
                    tensor[0, 0, y, x] = ptr[idx] / 255f;     // R
                    tensor[0, 1, y, x] = ptr[idx + 1] / 255f; // G
                    tensor[0, 2, y, x] = ptr[idx + 2] / 255f; // B
                }
            }
        }

        return tensor;
    }

    /// <summary>후처리: YOLOv8 출력 → DetectionResult 리스트</summary>
    private List<DetectionResult> Postprocess(Tensor<float> output, int origWidth, int origHeight)
    {
        // YOLOv8 출력: [1, 84, 8400] — 84 = 4(bbox) + 80(classes)
        var results = new List<DetectionResult>();
        var boxes = new List<Rect>();
        var confidences = new List<float>();
        var classIds = new List<int>();

        int numDetections = output.Dimensions[2]; // 8400
        int numClasses = output.Dimensions[1] - 4; // 80

        float scaleX = (float)origWidth / InputWidth;
        float scaleY = (float)origHeight / InputHeight;

        for (int i = 0; i < numDetections; i++)
        {
            // 클래스별 신뢰도 중 최대값 찾기
            float maxConf = 0;
            int maxClassId = 0;

            for (int c = 0; c < numClasses; c++)
            {
                float conf = output[0, c + 4, i];
                if (conf > maxConf)
                {
                    maxConf = conf;
                    maxClassId = c;
                }
            }

            if (maxConf < ConfidenceThreshold)
                continue;

            // bbox: cx, cy, w, h → x, y, w, h
            float cx = output[0, 0, i] * scaleX;
            float cy = output[0, 1, i] * scaleY;
            float w = output[0, 2, i] * scaleX;
            float h = output[0, 3, i] * scaleY;

            int x = (int)(cx - w / 2);
            int y = (int)(cy - h / 2);

            boxes.Add(new Rect(x, y, (int)w, (int)h));
            confidences.Add(maxConf);
            classIds.Add(maxClassId);
        }

        // NMS (Non-Maximum Suppression)
        if (boxes.Count > 0)
        {
            var indices = NmsFilter(boxes, confidences, NmsThreshold);

            foreach (int idx in indices)
            {
                results.Add(new DetectionResult
                {
                    BoundingBox = boxes[idx],
                    Confidence = confidences[idx],
                    ClassId = classIds[idx],
                    Label = CocoLabels.GetLabel(classIds[idx])
                });
            }
        }

        return results;
    }

    /// <summary>NMS 구현</summary>
    private static List<int> NmsFilter(List<Rect> boxes, List<float> scores, float iouThreshold)
    {
        var indices = Enumerable.Range(0, boxes.Count)
            .OrderByDescending(i => scores[i]).ToList();
        var result = new List<int>();
        var suppressed = new HashSet<int>();

        foreach (int i in indices)
        {
            if (suppressed.Contains(i)) continue;
            result.Add(i);

            for (int j = indices.IndexOf(i) + 1; j < indices.Count; j++)
            {
                int k = indices[j];
                if (suppressed.Contains(k)) continue;
                if (IoU(boxes[i], boxes[k]) > iouThreshold)
                    suppressed.Add(k);
            }
        }
        return result;
    }

    private static float IoU(Rect a, Rect b)
    {
        int x1 = Math.Max(a.X, b.X);
        int y1 = Math.Max(a.Y, b.Y);
        int x2 = Math.Min(a.X + a.Width, b.X + b.Width);
        int y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);
        int inter = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        int union = a.Width * a.Height + b.Width * b.Height - inter;
        return union > 0 ? (float)inter / union : 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session?.Dispose();
    }
}
