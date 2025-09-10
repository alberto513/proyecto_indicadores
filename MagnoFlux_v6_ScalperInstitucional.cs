using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel.DataAnnotations;
// Added for DisplayAttribute support
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Windows.Media;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class MagnoFlux_v6_ScalperInstitucional : Strategy
    {
        #region Parameters
        [NinjaScriptProperty]
        [Display(Name = "UseAtrTargets", Order = 1, GroupName = "Parameters")]
        public bool UseAtrTargets { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "TpMultiplier", Order = 2, GroupName = "Parameters")]
        public double TpMultiplier { get; set; } = 2.5;

        [NinjaScriptProperty]
        [Display(Name = "SlMultiplier", Order = 3, GroupName = "Parameters")]
        public double SlMultiplier { get; set; } = 1.2;

        [NinjaScriptProperty]
        [Display(Name = "DisableTrailingNearTP", Order = 4, GroupName = "Parameters")]
        public bool DisableTrailingNearTP { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "MinHertzToAllowTrade", Order = 5, GroupName = "Parameters")]
        public double MinHertzToAllowTrade { get; set; } = 0.01;

        [NinjaScriptProperty]
        [Display(Name = "Quantity", Order = 6, GroupName = "Parameters")]
        public int Quantity { get; set; } = 4;

        [NinjaScriptProperty]
        [Display(Name = "DailyProfitLimit", Order = 7, GroupName = "Parameters")]
        public double DailyProfitLimit { get; set; } = 500;

        [NinjaScriptProperty]
        [Display(Name = "DailyLossLimit", Order = 8, GroupName = "Parameters")]
        public double DailyLossLimit { get; set; } = 500;

        [NinjaScriptProperty]
        [Display(Name = "TrailingStopEnabled", Order = 9, GroupName = "Parameters")]
        public bool TrailingStopEnabled { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "MaxContracts", Order = 10, GroupName = "Parameters")]
        public int MaxContracts { get; set; } = 8;

        [NinjaScriptProperty]
        [Display(Name = "RequireBreakoutBar", Order = 11, GroupName = "Parameters")]
        public bool RequireBreakoutBar { get; set; } = false;

        [NinjaScriptProperty]
        [Display(Name = "EnableMarketOpenFilter", Order = 12, GroupName = "Parameters")]
        public bool EnableMarketOpenFilter { get; set; } = false;

        [NinjaScriptProperty]
        [Display(Name = "NoTradeZoneStart", Order = 13, GroupName = "Parameters")]
        public string NoTradeZoneStart { get; set; } = "00:00";

        [NinjaScriptProperty]
        [Display(Name = "NoTradeZoneEnd", Order = 14, GroupName = "Parameters")]
        public string NoTradeZoneEnd { get; set; } = "00:00";

        [NinjaScriptProperty]
        [Display(Name = "SmartExitRetrace", Order = 15, GroupName = "Parameters")]
        public double SmartExitRetrace { get; set; } = 0.5;

        [NinjaScriptProperty]
        [Display(Name = "BlockWeakShorts", Order = 16, GroupName = "Parameters")]
        public bool BlockWeakShorts { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "TestMode", Order = 17, GroupName = "Parameters")]
        public bool TestMode { get; set; } = false;

        [NinjaScriptProperty]
        [Display(Name = "TestSignalInterval", Order = 18, GroupName = "Parameters")]
        public int TestSignalInterval { get; set; } = 20;
        #endregion

        private ATR atr;
        private SMA atrAvg;
        private SMA volSma;
        private bool pendingLong;
        private bool pendingShort;
        private int signalBar;
        private DateTime signalTime;
        private DateTime entryExecTime;
        private double entryPrice;
        private double tpPrice;
        private double slPrice;
        private double tp1Price;
        private double tp2Price;
        private int entryBar;
        private List<DateTime> signalTimes = new List<DateTime>();
        private double currentHz;
        private bool lastTradeWin;
        private int consecutiveSL;
        private bool partial1Done;
        private bool partial2Done;
        private bool breakEvenDone;
        private bool trailingActive;
        private int tradeDirection;
        private double tradePnl;
        private int partial1Qty;
        private int partial2Qty;
        private int finalQty;
        private StreamWriter logWriter;
        private double dailyPnL;
        private DateTime currentDay;
        private double equityHigh;
        private double equityPeak;
        private double equityDrawdown;
        private bool tradingPaused;
        private int scoreAtEntry;
        private int currentQty;
        private double maxProfitRun;
        private int signalCount;
        private string tradeTag;
        private int tradeCounter;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "MagnoFlux_v6_ScalperInstitucional";
                Calculate = Calculate.OnPriceChange;
                IsOverlay = true;
            }
            else if (State == State.DataLoaded)
            {
                atr = ATR(14);
                atrAvg = SMA(atr, 14);
                volSma = SMA(Volume, 14);
                currentDay = Times[0][0].Date;
                double acc = 0;
                try { acc = Account.Get(AccountItem.CashValue, Currency.UsDollar); } catch { }
                equityHigh = acc;
                equityPeak = acc;
                currentQty = Quantity;
                tradeCounter = 0;
                try
                {
                    string logPath = System.IO.Path.Combine(NinjaTrader.Core.Globals.UserDataDir, "MagnoFlux_v6_log.csv");
                    logWriter = new StreamWriter(logPath, true);
                }
                catch
                {
                    logWriter = null;
                }
                if (logWriter != null && logWriter.BaseStream.Length == 0)
                    logWriter.WriteLine("SignalID,Time,Direction,Entry,Exit,Type,PNL,Score,Hz,EquityPeak,EquityDrawdown");
            }
            else if (State == State.Terminated)
            {
                logWriter?.Flush();
                logWriter?.Dispose();
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 5)
                return;

            UpdateHz();
            if (Times[0][0].Date != currentDay)
            {
                currentDay = Times[0][0].Date;
                dailyPnL = 0;
                consecutiveSL = 0;
                tradingPaused = false;
                double acc = 0; try { acc = Account.Get(AccountItem.CashValue, Currency.UsDollar); } catch { }
                equityHigh = acc;
                equityPeak = acc;
            }

            if (tradingPaused)
                return;

            TimeSpan now = Times[0][0].TimeOfDay;
            if (EnableMarketOpenFilter)
            {
                TimeSpan s = new TimeSpan(9, 29, 0);
                TimeSpan e = new TimeSpan(9, 36, 0);
                if (now >= s && now <= e)
                    return;
            }

            if (NoTradeZoneStart != "00:00" || NoTradeZoneEnd != "00:00")
            {
                try
                {
                    TimeSpan ns = TimeSpan.Parse(NoTradeZoneStart);
                    TimeSpan ne = TimeSpan.Parse(NoTradeZoneEnd);
                    if (ns < ne && now >= ns && now <= ne)
                        return;
                }
                catch { }
            }

            if (pendingLong || pendingShort)
            {
                if (CurrentBar == signalBar + 1 && IsFirstTickOfBar)
                {
                    if (pendingLong)
                        ExecuteEntry(true);
                    if (pendingShort)
                        ExecuteEntry(false);
                    pendingLong = pendingShort = false;
                }
            }
            else
            {
                DetectSignal();
            }

            ManageExit();
        }

        private void DetectSignal()
        {
            if (TestMode && CurrentBar % TestSignalInterval == 0)
            {
                pendingLong = true;
                signalBar = CurrentBar;
                signalTime = Time[0];
                scoreAtEntry = 10;
                tradeTag = "TEST" + CurrentBar.ToString();
                return;
            }

            if (currentHz < MinHertzToAllowTrade)
                return;
            if (DetectTrapCandle())
                return;

            int scoreL = GetInstitutionalScore(true);
            int scoreS = GetInstitutionalScore(false);

            int requiredScore = 4;
            if (signalCount < 2 && CurrentBar > 40)
                requiredScore = 3; // adaptative

            if (scoreL >= requiredScore && IsLongSignal())
            {
                pendingLong = true;
                signalBar = CurrentBar;
                signalTime = Time[0];
                scoreAtEntry = scoreL;
                tradeTag = "TRD" + (++tradeCounter).ToString();
            }
            else if (scoreS >= requiredScore && (BlockWeakShorts ? IsShortInstitutional() : IsShortSignal()))
            {
                pendingShort = true;
                signalBar = CurrentBar;
                signalTime = Time[0];
                scoreAtEntry = scoreS;
                tradeTag = "TRD" + (++tradeCounter).ToString();
            }
        }

        private void ExecuteEntry(bool isLong)
        {
            entryPrice = Open[0];
            entryBar = CurrentBar;
            signalCount++;

            if (UseAtrTargets)
            {
                tpPrice = isLong ? entryPrice + atr[0] * TpMultiplier : entryPrice - atr[0] * TpMultiplier;
                slPrice = isLong ? entryPrice - atr[0] * SlMultiplier : entryPrice + atr[0] * SlMultiplier;
            }
            else
            {
                tpPrice = isLong ? High[2] : Low[2];
                slPrice = isLong ? Low[2] : High[2];
            }

            double p1 = atr[0] > atrAvg[0] ? 0.6 : 0.5;
            double p2 = atr[0] > atrAvg[0] ? 0.9 : 0.8;
            tp1Price = isLong ? entryPrice + (tpPrice - entryPrice) * p1 : entryPrice - (entryPrice - tpPrice) * p1;
            tp2Price = isLong ? entryPrice + (tpPrice - entryPrice) * p2 : entryPrice - (entryPrice - tpPrice) * p2;

            partial1Qty = (int)Math.Max(1, Math.Round(currentQty * 0.5));
            partial2Qty = (int)Math.Max(1, Math.Round(currentQty * 0.25));
            finalQty = currentQty - partial1Qty - partial2Qty;

            partial1Done = partial2Done = false;
            breakEvenDone = false;
            trailingActive = false;
            maxProfitRun = 0;
            tradePnl = 0;
            tradeDirection = isLong ? 1 : -1;

            if (isLong)
                EnterLong(currentQty, "LongEntry");
            else
                EnterShort(currentQty, "ShortEntry");

            Draw.VerticalLine(this, "entry" + tradeTag, 0, isLong ? Brushes.Lime : Brushes.Red);
        }

        private void ManageExit()
        {
            if (Position.MarketPosition == MarketPosition.Flat)
                return;

            if (CurrentBar <= entryBar)
                return;

            bool isLong = Position.MarketPosition == MarketPosition.Long;
            double barHigh = High[1];
            double barLow = Low[1];
            double targetDist = Math.Abs(tpPrice - entryPrice);
            double progress = isLong ? barHigh - entryPrice : entryPrice - barLow;
            double favor = isLong ? barHigh - entryPrice : entryPrice - barLow;
            if (favor > maxProfitRun)
                maxProfitRun = favor;

            if (!breakEvenDone && progress >= targetDist * 0.6)
            {
                slPrice = entryPrice + (isLong ? TickSize : -TickSize);
                breakEvenDone = true;
            }

            if (progress >= targetDist * 0.8)
                trailingActive = true;

            if (trailingActive && TrailingStopEnabled)
            {
                if (DisableTrailingNearTP && Math.Abs(tpPrice - Close[0]) <= 5 * TickSize)
                { }
                else
                {
                    if (CurrentBar > 3)
                    {
                        double recentLow = Math.Min(Math.Min(Low[1], Low[2]), Low[3]);
                        double recentHigh = Math.Max(Math.Max(High[1], High[2]), High[3]);
                        if (isLong)
                            slPrice = Math.Max(slPrice, recentLow - atr[0]);
                        else
                            slPrice = Math.Min(slPrice, recentHigh + atr[0]);
                    }
                }
            }

            if (!partial1Done && ((isLong && barHigh >= tp1Price) || (!isLong && barLow <= tp1Price)))
            {
                if (isLong)
                    ExitLong(partial1Qty, "TP1", "");
                else
                    ExitShort(partial1Qty, "TP1", "");
                partial1Done = true;
            }

            if (!partial2Done && ((isLong && barHigh >= tp2Price) || (!isLong && barLow <= tp2Price)))
            {
                if (isLong)
                    ExitLong(partial2Qty, "TP2", "");
                else
                    ExitShort(partial2Qty, "TP2", "");
                partial2Done = true;
            }

            if (partial1Done)
            {
                if ((isLong && barLow <= entryPrice) || (!isLong && barHigh >= entryPrice))
                {
                    if (isLong)
                        ExitLong("SmartExit");
                    else
                        ExitShort("SmartExit");
                }

                if (!partial2Done && favor <= maxProfitRun * 0.7)
                {
                    if (isLong)
                        ExitLong(partial2Qty, "TP2Early", "");
                    else
                        ExitShort(partial2Qty, "TP2Early", "");
                    partial2Done = true;
                }

                if (favor <= maxProfitRun * (1 - SmartExitRetrace))
                {
                    if (isLong)
                        ExitLong("SmartExit");
                    else
                        ExitShort("SmartExit");
                }
            }

            if (isLong)
            {
                if (barHigh >= tpPrice && barLow <= slPrice)
                {
                    double distSl = Math.Abs(Open[1] - slPrice);
                    double distTp = Math.Abs(tpPrice - Open[1]);
                    if (distSl < distTp)
                    {
                        ExitLong("SL");
                        Draw.Text(this, "fail" + tradeTag, "X", 0, entryPrice, Brushes.Gray);
                        lastTradeWin = false;
                        consecutiveSL++;
                    }
                    else
                    {
                        ExitLong("TP");
                        Draw.Text(this, "win" + tradeTag, "✔", 0, entryPrice, Brushes.Green);
                        lastTradeWin = true;
                    }
                }
                else if (barLow <= slPrice)
                {
                    ExitLong("SL");
                    Draw.Text(this, "fail" + tradeTag, "X", 0, entryPrice, Brushes.Gray);
                    lastTradeWin = false;
                    consecutiveSL++;
                }
                else if (barHigh >= tpPrice)
                {
                    ExitLong("TP");
                    Draw.Text(this, "win" + tradeTag, "✔", 0, entryPrice, Brushes.Green);
                    lastTradeWin = true;
                }
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                if (barHigh >= slPrice && barLow <= tpPrice)
                {
                    double distSl = Math.Abs(slPrice - Open[1]);
                    double distTp = Math.Abs(Open[1] - tpPrice);
                    if (distSl < distTp)
                    {
                        ExitShort("SL");
                        Draw.Text(this, "fail" + tradeTag, "X", 0, entryPrice, Brushes.Red);
                        lastTradeWin = false;
                        consecutiveSL++;
                    }
                    else
                    {
                        ExitShort("TP");
                        Draw.Text(this, "win" + tradeTag, "✔", 0, entryPrice, Brushes.Green);
                        lastTradeWin = true;
                    }
                }
                else if (barHigh >= slPrice)
                {
                    ExitShort("SL");
                    Draw.Text(this, "fail" + tradeTag, "X", 0, entryPrice, Brushes.Red);
                    lastTradeWin = false;
                    consecutiveSL++;
                }
                else if (barLow <= tpPrice)
                {
                    ExitShort("TP");
                    Draw.Text(this, "win" + tradeTag, "✔", 0, entryPrice, Brushes.Green);
                    lastTradeWin = true;
                }
            }

            if (Position.MarketPosition == MarketPosition.Flat && !lastTradeWin && consecutiveSL >= 3)
            {
                tradingPaused = true;
                pendingLong = pendingShort = false;
            }
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (execution.Order.OrderState != OrderState.Filled)
                return;

            if (execution.Order.Name == "LongEntry" || execution.Order.Name == "ShortEntry")
            {
                entryExecTime = time;
            }
            else if (marketPosition == MarketPosition.Flat)
            {
                tradePnl += tradeDirection * (price - entryPrice) * quantity * (Instrument.MasterInstrument.PointValue == 0 ? 1 : Instrument.MasterInstrument.PointValue);
                double acc = 0; try { acc = Account.Get(AccountItem.CashValue, Currency.UsDollar); } catch { }
                if (acc > equityPeak) equityPeak = acc;
                equityDrawdown = equityPeak - acc;
                LogTrade(execution.Order.Name, price);
                dailyPnL += tradePnl;
                if (acc > equityHigh) equityHigh = acc;
                if (acc < equityHigh * 0.95) tradingPaused = true;
                if (dailyPnL >= DailyProfitLimit || dailyPnL <= -DailyLossLimit) tradingPaused = true;
                if (execution.Order.Name == "SL")
                    consecutiveSL++;
                else if (execution.Order.Name == "TP")
                    consecutiveSL = 0;
                if (consecutiveSL >= 3)
                    tradingPaused = true;
            }
            else
            {
                tradePnl += tradeDirection * (price - entryPrice) * quantity * (Instrument.MasterInstrument.PointValue == 0 ? 1 : Instrument.MasterInstrument.PointValue);
                if (execution.Order.Name == "TP1" || execution.Order.Name == "TP2")
                {
                    double acc = 0; try { acc = Account.Get(AccountItem.CashValue, Currency.UsDollar); } catch { }
                    if (acc > equityPeak) equityPeak = acc;
                    equityDrawdown = equityPeak - acc;
                    LogTrade(execution.Order.Name, price);
                }
            }
        }

        private void LogTrade(string type, double price)
        {
            string line = $"{tradeTag},{entryExecTime:yyyy-MM-dd HH:mm:ss},{(tradeDirection==1?"Long":"Short")},{entryPrice:F2},{price:F2},{type},{tradePnl:F2},{scoreAtEntry},{currentHz:F4},{equityPeak:F2},{equityDrawdown:F2}";
            try { logWriter.WriteLine(line); logWriter.Flush(); } catch { }
        }

        private void UpdateHz()
        {
            signalTimes.Add(Time[0]);
            while (signalTimes.Count > 30)
                signalTimes.RemoveAt(0);
            if (signalTimes.Count > 1)
            {
                double seconds = (signalTimes[signalTimes.Count - 1] - signalTimes[0]).TotalSeconds;
                currentHz = (signalTimes.Count - 1) / seconds;
            }
        }

        private int GetInstitutionalScore(bool isLong)
        {
            int score = 0;
            double body = Math.Abs(Close[0] - Open[0]);
            double range = High[0] - Low[0];
            bool breakout = isLong ? Close[0] > High[1] : Close[0] < Low[1];

            if (Volume[0] > volSma[0] * 1.5)
                score++;
            if (range > 0 && body / range > 0.7)
                score++;
            if (breakout)
                score++;
            if (range > atr[0])
                score++;

            if (Volume[1] > volSma[1] * 1.5 && ((Close[1] > Open[1]) != isLong))
                score--;

            if (breakout && Volume[0] > volSma[0] * 1.5 && body / range > 0.6)
                score++;

            if ((isLong && Close[0] > High[1] && Volume[0] > volSma[0]) || (!isLong && Close[0] < Low[1] && Volume[0] > volSma[0]))
                score++;

            if (Volume[0] > volSma[0] * 1.5 && range > 0 && body / range < 0.3)
                score--;

            bool engulfOpp = isLong ? (Close[1] < Open[1] && Open[0] < Close[1] && Close[0] < Open[1])
                                     : (Close[1] > Open[1] && Open[0] > Close[1] && Close[0] > Open[1]);
            if (engulfOpp)
                score -= 2;

            if (RequireBreakoutBar && !breakout)
                score = 0;

            if (range < atr[0] * 0.5)
                score -= 2;

            bool doubleRev = (Close[2] > Open[2] && Close[1] < Open[1] && isLong) || (Close[2] < Open[2] && Close[1] > Open[1] && !isLong);
            if (doubleRev)
                score -= 2;

            if (body / range > 0.6 && Volume[0] > volSma[0] * 1.8)
                score++;

            return score;
        }

        private bool IsLongSignal()
        {
            return Close[0] > Open[0];
        }
        private bool IsShortSignal()
        {
            return Close[0] < Open[0];
        }

        private bool IsShortInstitutional()
        {
            double body = Open[0] - Close[0];
            double range = High[0] - Low[0];
            bool volumeHigh = Volume[0] > volSma[0] * 1.5;
            bool bodyLarge = range > 0 && body / range > 0.7;
            bool pressure = Close[0] < Low[1];
            bool prevGreenTrap = Volume[1] > volSma[1] * 1.5 && Close[1] > Open[1];
            bool prevBounce = Close[1] < Open[1] && Low[0] > Low[1];

            if (!volumeHigh || !bodyLarge || !pressure)
                return false;
            if (prevGreenTrap || prevBounce)
                return false;
            return true;
        }

        private bool DetectTrapCandle()
        {
            double body = Math.Abs(Close[1] - Open[1]);
            double prevBody = Math.Abs(Close[2] - Open[2]);
            bool trap = body > prevBody * 1.5 && Math.Sign(Close[1] - Open[1]) != Math.Sign(Close[2] - Open[2]);
            bool reversal = Volume[1] > volSma[1] * 1.5 && body / (High[1] - Low[1]) > 0.6 && Math.Sign(Close[1] - Open[1]) != Math.Sign(Close[0] - Open[0]);
            return trap || reversal;
        }
    }
}
