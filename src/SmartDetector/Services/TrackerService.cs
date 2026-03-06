using OpenCvSharp;
using SmartDetector.Models;

namespace SmartDetector.Services;

/// <summary>
/// SORT(Simple Online and Realtime Tracking) 기반 객체 트래커.
/// 칼만 필터 예측 + IoU 기반 헝가리안 매칭.
/// </summary>
public sealed class TrackerService
{
    private readonly List<TrackedObject> _tracks = new();

    /// <summary>매칭 실패로 판단할 최소 IoU</summary>
    public float IouThreshold { get; set; } = 0.3f;

    /// <summary>트랙 삭제까지 허용하는 미검출 프레임 수</summary>
    public int MaxAge { get; set; } = 5;

    /// <summary>현재 활성 트랙</summary>
    public IReadOnlyList<TrackedObject> ActiveTracks => _tracks.AsReadOnly();

    /// <summary>새 프레임의 검출 결과로 트래킹 업데이트</summary>
    public List<TrackedObject> Update(List<DetectionResult> detections)
    {
        // 1. 기존 트랙 예측
        foreach (var track in _tracks)
            track.Predict();

        // 2. IoU 코스트 매트릭스 생성
        int N = _tracks.Count;
        int M = detections.Count;

        if (N == 0)
        {
            // 모든 검출을 새 트랙으로
            foreach (var det in detections)
                _tracks.Add(new TrackedObject(det));
            return _tracks.ToList();
        }

        if (M == 0)
        {
            // 검출 없음 — 미검출 카운트 증가, 오래된 트랙 삭제
            _tracks.RemoveAll(t => t.TimeSinceUpdate > MaxAge);
            return _tracks.ToList();
        }

        // IoU 매트릭스 [tracks x detections]
        var iouMatrix = new float[N, M];
        for (int i = 0; i < N; i++)
            for (int j = 0; j < M; j++)
                iouMatrix[i, j] = IoU(_tracks[i].BoundingBox, detections[j].BoundingBox);

        // 3. 그리디 매칭 (헝가리안 근사)
        var matchedTracks = new HashSet<int>();
        var matchedDets = new HashSet<int>();
        var assignments = new List<(int trackIdx, int detIdx)>();

        // IoU가 높은 순서대로 매칭
        var candidates = new List<(int ti, int di, float iou)>();
        for (int i = 0; i < N; i++)
            for (int j = 0; j < M; j++)
                if (iouMatrix[i, j] >= IouThreshold)
                    candidates.Add((i, j, iouMatrix[i, j]));

        candidates.Sort((a, b) => b.iou.CompareTo(a.iou));

        foreach (var (ti, di, _) in candidates)
        {
            if (matchedTracks.Contains(ti) || matchedDets.Contains(di))
                continue;
            assignments.Add((ti, di));
            matchedTracks.Add(ti);
            matchedDets.Add(di);
        }

        // 4. 매칭된 트랙 업데이트
        foreach (var (ti, di) in assignments)
            _tracks[ti].Update(detections[di]);

        // 5. 매칭되지 않은 검출 → 새 트랙 생성
        for (int j = 0; j < M; j++)
        {
            if (!matchedDets.Contains(j))
                _tracks.Add(new TrackedObject(detections[j]));
        }

        // 6. 오래된 트랙 삭제
        _tracks.RemoveAll(t => t.TimeSinceUpdate > MaxAge);

        return _tracks.ToList();
    }

    /// <summary>트래커 리셋</summary>
    public void Reset()
    {
        _tracks.Clear();
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
}
