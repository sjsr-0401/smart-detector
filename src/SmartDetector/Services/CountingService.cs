using OpenCvSharp;
using SmartDetector.Models;

namespace SmartDetector.Services;

/// <summary>
/// 카운팅 라인을 지나는 객체를 카운트.
/// 객체의 중심점이 라인을 상→하 또는 하→상으로 통과하면 카운트 증가.
/// </summary>
public sealed class CountingService
{
    private readonly Dictionary<int, int> _lastY = new(); // trackId → 이전 프레임 중심 Y
    private readonly HashSet<int> _countedIds = new();

    /// <summary>카운팅 라인 Y 위치 (0.0~1.0, 화면 비율)</summary>
    public float LinePositionRatio { get; set; } = 0.5f;

    /// <summary>상→하 통과 카운트</summary>
    public int CountDown { get; private set; }

    /// <summary>하→상 통과 카운트</summary>
    public int CountUp { get; private set; }

    /// <summary>총 카운트</summary>
    public int TotalCount => CountDown + CountUp;

    /// <summary>트래킹 결과를 받아 카운팅 업데이트</summary>
    public void Update(List<TrackedObject> tracks, int frameHeight)
    {
        int lineY = (int)(frameHeight * LinePositionRatio);

        foreach (var track in tracks)
        {
            int cy = track.BoundingBox.Y + track.BoundingBox.Height / 2;

            if (_lastY.TryGetValue(track.Id, out int prevY))
            {
                // 이미 카운트된 객체는 스킵
                if (_countedIds.Contains(track.Id))
                {
                    _lastY[track.Id] = cy;
                    continue;
                }

                // 상→하 통과: 이전 Y < lineY && 현재 Y >= lineY
                if (prevY < lineY && cy >= lineY)
                {
                    CountDown++;
                    _countedIds.Add(track.Id);
                }
                // 하→상 통과: 이전 Y >= lineY && 현재 Y < lineY
                else if (prevY >= lineY && cy < lineY)
                {
                    CountUp++;
                    _countedIds.Add(track.Id);
                }
            }

            _lastY[track.Id] = cy;
        }

        // 삭제된 트랙 정리
        var activeIds = new HashSet<int>(tracks.Select(t => t.Id));
        var staleIds = _lastY.Keys.Where(id => !activeIds.Contains(id)).ToList();
        foreach (var id in staleIds)
        {
            _lastY.Remove(id);
            _countedIds.Remove(id);
        }
    }

    /// <summary>카운팅 라인 + 카운트 표시</summary>
    public void DrawOverlay(Mat frame)
    {
        int lineY = (int)(frame.Height * LinePositionRatio);

        // 카운팅 라인 (초록 점선)
        Cv2.Line(frame, new Point(0, lineY), new Point(frame.Width, lineY),
            new Scalar(0, 255, 0), 2, LineTypes.AntiAlias);

        // 라인 양쪽에 화살표 표시
        int midX = frame.Width / 2;
        Cv2.ArrowedLine(frame, new Point(midX - 40, lineY - 20), new Point(midX - 40, lineY + 20),
            new Scalar(0, 200, 255), 2); // ↓
        Cv2.ArrowedLine(frame, new Point(midX + 40, lineY + 20), new Point(midX + 40, lineY - 20),
            new Scalar(255, 200, 0), 2); // ↑

        // 카운트 표시
        string downText = $"DOWN: {CountDown}";
        string upText = $"UP: {CountUp}";
        string totalText = $"TOTAL: {TotalCount}";

        Cv2.PutText(frame, downText, new Point(frame.Width - 200, lineY + 30),
            HersheyFonts.HersheySimplex, 0.7, new Scalar(0, 200, 255), 2, LineTypes.AntiAlias);
        Cv2.PutText(frame, upText, new Point(frame.Width - 200, lineY - 15),
            HersheyFonts.HersheySimplex, 0.7, new Scalar(255, 200, 0), 2, LineTypes.AntiAlias);
        Cv2.PutText(frame, totalText, new Point(10, frame.Height - 20),
            HersheyFonts.HersheySimplex, 0.9, new Scalar(255, 255, 255), 2, LineTypes.AntiAlias);
    }

    /// <summary>카운트 리셋</summary>
    public void Reset()
    {
        CountDown = 0;
        CountUp = 0;
        _lastY.Clear();
        _countedIds.Clear();
    }
}
