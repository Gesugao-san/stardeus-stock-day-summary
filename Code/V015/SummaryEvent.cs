// Stock Day Summary — v0.15 notification (Game.Systems.Notify).

using System;
using Game.Data;
using Game.Systems.Notify;

namespace StockDaySummary {
  /// <summary>
  /// Shows the summary as a v0.15 notification and keeps only the latest one.
  /// </summary>
  internal sealed class SummaryEvent {
    /// <summary>Notification stays for one in-game day (ticks are minutes).</summary>
    private const int LifetimeTicks = 1440;

    private NotificationHandle last;

    /// <summary>Replaces the previous summary notification with a new one.</summary>
    /// <param name="s">Current game state (unused in v0.15, kept for a common signature).</param>
    /// <param name="icon">Icon path.</param>
    /// <param name="title">One-line text.</param>
    /// <param name="details">Tooltip text.</param>
    /// <param name="onClick">Called when the notification is clicked.</param>
    public void Show(GameState s, string icon, string title, string details, Action onClick) {
      if (last.IsValid) { Notification.Remove(ref last); }

      last = Notification.Build(icon, title, LifetimeTicks, NotificationType.Info)
        .WithTooltip(details)
        .WithOnClick(onClick)
        .Show();
    }
  }
}
