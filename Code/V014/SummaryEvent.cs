// Stock Day Summary — v0.14 event list entry (UDB + EventNotification, removed in v0.15).

using System;
using Game.Constants;
using Game.Data;

namespace StockDaySummary {
  /// <summary>
  /// Shows the summary in the v0.14 "Events" list and keeps only the latest one.
  /// </summary>
  internal sealed class SummaryEvent {
    private const string EventId = "StockDaySummary";
    /// <summary>Event stays in the list for one in-game day (ticks are minutes).</summary>
    private const long LifetimeTicks = 1440;
    /// <summary>Minimum real minutes the event stays visible.</summary>
    private const float MinMinutes = 15f;

    private EventNotification last;

    /// <summary>Replaces the previous summary event with a new one.</summary>
    /// <param name="s">Current game state.</param>
    /// <param name="icon">Icon path.</param>
    /// <param name="title">One-line text.</param>
    /// <param name="details">Tooltip text.</param>
    /// <param name="onClick">Called when the event icon is clicked.</param>
    public void Show(GameState s, string icon, string title, string details, Action onClick) {
      if (last != null) { s.Sig.RemoveEvent.Send(last); }

      var data = UDB.Create(EventId, UDBT.IEvent, icon, title)
        .WithTooltip(details)
        .WithIconClickFunction(onClick);
      last = EventNotification.Create(s.Clock.Ticks, data, Priority.Normal, false)
        .WithHideAfter(LifetimeTicks, MinMinutes);
      s.Sig.AddEvent.Send(last);
    }
  }
}
