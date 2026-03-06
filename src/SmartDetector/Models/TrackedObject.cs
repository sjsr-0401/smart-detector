using OpenCvSharp;

namespace SmartDetector.Models;

/// <summary>트래킹 중인 객체</summary>
public class TrackedObject
{
    private static int _nextId;

    public int Id { get; }
    public Rect BoundingBox { get; set; }
    public string Label { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public float Confidence { get; set; }
    public int Age { get; set; }
    public int TimeSinceUpdate { get; set; }
    public List<Point> Trail { get; } = new();

    // 칼만 필터 상태: [cx, cy, w, h, vx, vy, vw, vh]
    public float[] State { get; set; } = new float[8];

    public TrackedObject(DetectionResult detection)
    {
        Id = Interlocked.Increment(ref _nextId);
        Update(detection);
        InitKalman();
    }

    /// <summary>검출 결과로 상태 업데이트</summary>
    public void Update(DetectionResult detection)
    {
        BoundingBox = detection.BoundingBox;
        Label = detection.Label;
        ClassId = detection.ClassId;
        Confidence = detection.Confidence;
        TimeSinceUpdate = 0;
        Age++;

        // 중심점 트레일 기록 (최대 50점)
        var center = new Point(
            BoundingBox.X + BoundingBox.Width / 2,
            BoundingBox.Y + BoundingBox.Height / 2);
        Trail.Add(center);
        if (Trail.Count > 50)
            Trail.RemoveAt(0);

        // 칼만 상태 보정
        float cx = BoundingBox.X + BoundingBox.Width / 2f;
        float cy = BoundingBox.Y + BoundingBox.Height / 2f;
        State[0] = cx;
        State[1] = cy;
        State[2] = BoundingBox.Width;
        State[3] = BoundingBox.Height;
    }

    /// <summary>칼만 예측 — 다음 프레임 위치 추정</summary>
    public Rect Predict()
    {
        // 등속 모델: 위치 += 속도
        State[0] += State[4]; // cx += vx
        State[1] += State[5]; // cy += vy
        State[2] += State[6]; // w += vw
        State[3] += State[7]; // h += vh

        // 크기가 음수가 되지 않도록
        State[2] = Math.Max(State[2], 1);
        State[3] = Math.Max(State[3], 1);

        TimeSinceUpdate++;

        int x = (int)(State[0] - State[2] / 2);
        int y = (int)(State[1] - State[3] / 2);
        BoundingBox = new Rect(x, y, (int)State[2], (int)State[3]);
        return BoundingBox;
    }

    private void InitKalman()
    {
        float cx = BoundingBox.X + BoundingBox.Width / 2f;
        float cy = BoundingBox.Y + BoundingBox.Height / 2f;
        State[0] = cx;
        State[1] = cy;
        State[2] = BoundingBox.Width;
        State[3] = BoundingBox.Height;
        // 속도 초기값 = 0
        State[4] = 0;
        State[5] = 0;
        State[6] = 0;
        State[7] = 0;
    }
}
