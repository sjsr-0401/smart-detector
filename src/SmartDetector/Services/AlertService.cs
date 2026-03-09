using SmartDetector.Models;

namespace SmartDetector.Services;

/// <summary>이벤트 알림 서비스 — 조건 기반 알림 트리거</summary>
public class AlertService
{
    private DateTime _lastAlertTime = DateTime.MinValue;
    private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(3);

    /// <summary>알림 쿨다운 (초)</summary>
    public double CooldownSeconds
    {
        get => _cooldown.TotalSeconds;
        init => _cooldown = TimeSpan.FromSeconds(value);
    }

    /// <summary>최대 객체 수 초과 알림 임계값</summary>
    public int MaxObjectThreshold { get; set; } = 10;

    /// <summary>특정 클래스 검출 시 알림할 클래스 목록</summary>
    public HashSet<string> AlertClasses { get; set; } = [];

    /// <summary>알림 이벤트</summary>
    public event EventHandler<AlertEventArgs>? AlertTriggered;

    /// <summary>검출 결과를 검사하여 알림 조건 확인</summary>
    public void Check(List<DetectionResult> detections, int totalCount)
    {
        if (DateTime.Now - _lastAlertTime < _cooldown) return;

        // 1. 객체 수 초과
        if (detections.Count >= MaxObjectThreshold)
        {
            TriggerAlert(AlertType.MaxObjectExceeded,
                $"Object count ({detections.Count}) exceeded threshold ({MaxObjectThreshold})");
            return;
        }

        // 2. 특정 클래스 검출
        if (AlertClasses.Count > 0)
        {
            var detected = detections
                .Where(d => AlertClasses.Contains(d.Label))
                .Select(d => d.Label)
                .Distinct()
                .ToList();

            if (detected.Count > 0)
            {
                TriggerAlert(AlertType.TargetClassDetected,
                    $"Target class detected: {string.Join(", ", detected)}");
                return;
            }
        }
    }

    private void TriggerAlert(AlertType type, string message)
    {
        _lastAlertTime = DateTime.Now;
        AlertTriggered?.Invoke(this, new AlertEventArgs(type, message, DateTime.Now));
    }

    public void Reset()
    {
        _lastAlertTime = DateTime.MinValue;
    }
}

public enum AlertType
{
    MaxObjectExceeded,
    TargetClassDetected,
    ZoneIntrusion
}

public record AlertEventArgs(AlertType Type, string Message, DateTime Timestamp);
