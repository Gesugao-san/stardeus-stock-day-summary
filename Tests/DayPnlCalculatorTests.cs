// Stock Day Summary — synthetic checks for DayPnlCalculator.
// Exit code is the number of failed checks.
using System;
using System.Collections.Generic;
using System.Linq;
using StockDaySummary.Core;

static class Tests {
  const int A = 1, B = 2;
  const long DayStart = 1000;
  static int failed;

  static void Check(string name, double got, double want) {
    bool ok = Math.Abs(got - want) < 1e-6;
    if (!ok) failed++;
    Console.WriteLine($"{(ok ? "OK  " : "FAIL")} {name}: got {got}, want {want}");
  }

  static int Main() {
    var start = new Dictionary<int, double> { [A] = 100, [B] = 50 };
    var end = new Dictionary<int, double> { [A] = 110, [B] = 40 };
    Func<int, double> sp = id => start[id], ep = id => end[id];
    var none = new List<SoldChunk>();

    // 1. Held overnight: 30 shares of A, 100 -> 110 => +300
    var s1 = DayPnlCalculator.Calculate(new[] { new Lot(A, 30, 80, 10) }, none, sp, ep, DayStart);
    Check("held pnl", s1.Pnl, 300);
    Check("held percent", s1.Percent, 10);
    Check("held value", s1.PortfolioValue, 3300);
    Check("held unrealized", s1.Unrealized, 30 * (110 - 80));

    // 2. Bought today at 105, closes at 110 => +50 for 10 shares (not vs open price)
    var s2 = DayPnlCalculator.Calculate(new[] { new Lot(A, 10, 105, DayStart + 5) }, none, sp, ep, DayStart);
    Check("bought today pnl", s2.Pnl, 50);

    // 3. Held B overnight and sold 5 of 20 at 45 today; rest closes at 40.
    //    held 15: 15*(40-50) = -150; sold 5: 5*(45-50) = -25 => -175
    var s3 = DayPnlCalculator.Calculate(
      new[] { new Lot(B, 15, 30, 10) },
      new[] { new SoldChunk(B, 5, 45, 50, 30) }, sp, ep, DayStart);
    Check("partial sell pnl", s3.Pnl, -175);
    Check("partial sell realized", s3.Realized, 5 * (45 - 30));
    Check("partial sell asset sold qty", s3.Assets.Single().SoldQty, 5);

    // 4. Bought and fully sold the same day: bought 4 @ 100 (start price = cost), sold @ 120 => +80
    var s4 = DayPnlCalculator.Calculate(new Lot[0], new[] { new SoldChunk(A, 4, 120, 100, 100) }, sp, ep, DayStart);
    Check("round trip pnl", s4.Pnl, 80);
    Check("round trip value", s4.PortfolioValue, 0);

    // 5. Mixed: A +300 (case 1) and B -175 (case 3) => +125, sorted best first
    var s5 = DayPnlCalculator.Calculate(
      new[] { new Lot(A, 30, 80, 10), new Lot(B, 15, 30, 10) },
      new[] { new SoldChunk(B, 5, 45, 50, 30) }, sp, ep, DayStart);
    Check("mixed pnl", s5.Pnl, 125);
    Check("mixed first is best", s5.Assets[0].AssetId, A);

    // 6. Empty portfolio
    var s6 = DayPnlCalculator.Calculate(new Lot[0], none, sp, ep, DayStart);
    Check("empty is empty", s6.IsEmpty ? 1 : 0, 1);
    Check("empty percent", s6.Percent, 0);

    // 7. Screenshot sanity: 30 RogueAI, avg cost 2.16K, price 2.19K, unchanged day => 0 day pnl
    var s7 = DayPnlCalculator.Calculate(new[] { new Lot(A, 30, 2160, 10) }, none, _ => 2190, _ => 2190, DayStart);
    Check("flat day pnl", s7.Pnl, 0);
    Check("flat day unrealized", s7.Unrealized, 900);

    Console.WriteLine(failed == 0 ? "ALL PASSED" : $"{failed} FAILED");
    return failed;
  }
}
