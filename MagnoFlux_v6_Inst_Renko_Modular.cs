using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Windows.Media;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class MagnoFlux_v6_Inst_Renko_Modular : Strategy
    {
        private class SignalData
        {
            public int SignalBar;
            public int EntryBar;
            public DateTime Time;
            public bool IsLong;
            public double EntryPrice;
            public double TP;
            public double SL;
            public int Quantity;
            public int BarsHeld;
            public double DurationSeconds;
            public double TradeEfficiency;
            public bool Done = false;
        }

        #region Parameters
        [NinjaScriptProperty]
        [Display(Name = "UseAtrTargets", Order = 1, GroupName = "Trade")]
        public bool UseAtrTargets { get; set; }

        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "TpMultiplier", Order = 2, GroupName = "Trade")]
        public double TpMultiplier { get; set; } = 2.5;

        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "SlMultiplier", Order = 3, GroupName = "Trade")]
        public double SlMultiplier { get; set; } = 1.1;

        [NinjaScriptProperty]
        [Display(Name = "DisableTrailingNearTP", Order = 4, GroupName = "Trade")]
        public bool DisableTrailingNearTP { get; set; } = true;

        [NinjaScriptProperty]
        [Range(0.0, double.MaxValue)]
        [Display(Name = "MinHertzToAllowTrade", Order = 5, GroupName = "Filters")]
        public double MinHertzToAllowTrade { get; set; } = 0.015;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Quantity", Order = 6, GroupName = "Trade")]
        public int Quantity { get; set; } = 4;

        [NinjaScriptProperty]
        [Range(0.0, double.MaxValue)]
        [Display(Name = "DailyProfitLimit", Order = 7, GroupName = "Risk")]
        public double DailyProfitLimit { get; set; } = 500;

        [NinjaScriptProperty]
        [Range(0.0, double.MaxValue)]
        [Display(Name = "DailyLossLimit", Order = 8, GroupName = "Risk")]
        public double DailyLossLimit { get; set; } = 500;

        [NinjaScriptProperty]
        [Range(0.0, double.MaxValue)]
        [Display(Name = "MaxDailyDrawdown", Order = 9, GroupName = "Risk")]
        public double MaxDailyDrawdown { get; set; } = 250;

        [NinjaScriptProperty]
        [Display(Name = "TrailingStopEnabled", Order = 10, GroupName = "Risk")]
        public bool TrailingStopEnabled { get; set; } = true;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "MaxContracts", Order = 11, GroupName = "Risk")]
        public int MaxContracts { get; set; } = 8;

        [NinjaScriptProperty]
        [Display(Name = "RequireBreakoutBar", Order = 12, GroupName = "Filters")]
        public bool RequireBreakoutBar { get; set; } = false;

        [NinjaScriptProperty]
        [Display(Name = "EnableMarketOpenFilter", Order = 13, GroupName = "Filters")]
        public bool EnableMarketOpenFilter { get; set; } = false;

        [NinjaScriptProperty]
        [Display(Name = "NoTradeZoneStart", Order = 14, GroupName = "Filters")]
        public string NoTradeZoneStart { get; set; } = "00:00";

        [NinjaScriptProperty]
        [Display(Name = "NoTradeZoneEnd", Order = 15, GroupName = "Filters")]
        public string NoTradeZoneEnd { get; set; } = "00:00";

        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "SmartExitRetrace", Order = 16, GroupName = "Risk")]
        public double SmartExitRetrace { get; set; } = 0.3;

        [NinjaScriptProperty]
        [Display(Name = "BlockWeakShorts", Order = 17, GroupName = "Filters")]
        public bool BlockWeakShorts { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "UseInstitutionalBias", Order = 18, GroupName = "Filters")]
        public bool UseInstitutionalBias { get; set; } = false;

        [NinjaScriptProperty]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "TrailingAtrMult", Order = 19, GroupName = "Risk")]
        public double TrailingAtrMult { get; set; } = 1.5;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "FirstPauseMinutes", Order = 20, GroupName = "Risk")]
        public int FirstPauseMinutes { get; set; } = 30;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "SecondPauseMinutes", Order = 21, GroupName = "Risk")]
        public int SecondPauseMinutes { get; set; } = 60;

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "ReentryAtrBuffer", Order = 22, GroupName = "Filters")]
        public double ReentryAtrBuffer { get; set; } = 0.75;

        [NinjaScriptProperty]
        [Display(Name = "UseDailyTrendFilter", Order = 23, GroupName = "Filters")]
        public bool UseDailyTrendFilter { get; set; } = false;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "DailyTrendPeriod", Order = 24, GroupName = "Filters")]
        public int DailyTrendPeriod { get; set; } = 200;

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "TpFactorTrending", Order = 25, GroupName = "Trade")]
        public double TpFactorTrending { get; set; } = 1.1;

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "TpFactorRanging", Order = 26, GroupName = "Trade")]
        public double TpFactorRanging { get; set; } = 0.9;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "MinShortScore", Order = 27, GroupName = "Filters")]
        public int MinShortScore { get; set; } = 5;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "EmergencyTicks", Order = 28, GroupName = "Risk")]
        public int EmergencyTicks { get; set; } = 40;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "BEThresholdTicks", Order = 29, GroupName = "Risk")]
        public int BEThresholdTicks { get; set; } = 10;

        [NinjaScriptProperty]
        [Range(0.5, 3.0)]
        [Display(Name = "AtrMultiplier", Order = 30, GroupName = "Risk")]
        public double AtrMultiplier { get; set; } = 1.2;

        [NinjaScriptProperty]
        [Range(1, 20)]
        [Display(Name = "SwingLookBack", Order = 31, GroupName = "Risk")]
        public int SwingLookBack { get; set; } = 10;

        [NinjaScriptProperty]
        [Display(Name = "MaxDailyDrawdownEnabled", Order = 32, GroupName = "Risk")]
        public bool MaxDailyDrawdownEnabled { get; set; } = false;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "SignalPoints", Order = 999, GroupName = "Trade")]
        public int SignalPoints { get; set; } = 40;

        // New parameters v6
        [NinjaScriptProperty]
        [Display(Name = "EnableFrequencyFilter", Order = 40, GroupName = "Filters")]
        public bool EnableFrequencyFilter { get; set; } = true;

        [NinjaScriptProperty]
        [Range(0.005, 0.1)]
        [Display(Name = "MinHz", Order = 41, GroupName = "Filters", Description="Hz mínimo para permitir señal")]
        public double MinHz { get; set; } = 0.015;

        [NinjaScriptProperty]
        [Range(0,7)]
        [Display(Name = "MinInstitutionalScore", Order = 42, GroupName = "Filters")]
        public int MinInstitutionalScore { get; set; } = 5;

        [NinjaScriptProperty]
        [Display(Name = "EnableConsolidationFilter", Order = 43, GroupName = "Filters")]
        public bool EnableConsolidationFilter { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "EnableTimeFilters", Order = 44, GroupName = "Filters")]
        public bool EnableTimeFilters { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "BlockMorningOpen", Order = 45, GroupName = "Filters")]
        public bool BlockMorningOpen { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "BlockAfternoonChaos", Order = 46, GroupName = "Filters")]
        public bool BlockAfternoonChaos { get; set; } = true;
        #endregion

        private ATR atr;
        private SMA atrAvg;
        private SMA volSma;
        private SMA biasSma;
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
        private DateTime pauseUntilTime;
        private int pauseStage;
        private int scoreAtEntry;
        private int currentQty;
        private double maxProfitRun;
        private double maxFavorAfterTP1;
        private bool strictTrailing;
        private bool tp1Hit;
        private bool tp2Hit;
        private bool smartExitUsed;
        private double maxFavorableTicks;
        private double maxUnfavorableTicks;
        private double swingStop;
        private double atrStop;
        private double atrEntry;
        private bool atrStopDominant;
        private double hybridSL;
        private int barsHeld;
        private double tradeDuration;
        private string entryTimeOnly;
        private string exitTimeOnly;
        private int entryBarIndex;
        private int exitBarIndex;
        private int entryQty;
        private int netContractsClosed;
        private double tradeEfficiency;
        private SMA atrSlow;
        private SMA atrTen;
        private EMA emaDaily;
        private string marketCycleMode;
        private double lastExitPrice;
        private List<SignalData> pendingSignals = new List<SignalData>();
        private double sessionPV = 0.0;
        private double sessionVol = 0.0;
        private double vwap = 0.0;
        private EMA ema50;
        private double cumDelta5m;
        private bool lastTimeBlocked;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "MagnoFlux_v6_Inst_Renko_Modular";
                Calculate = Calculate.OnPriceChange;
                IsOverlay = true;
                UseInstitutionalBias = false;
            }
            else if (State == State.Configure)
            {
                AddDataSeries(BarsPeriodType.Minute, 5);
            }
            else if (State == State.DataLoaded)
            {
                atr = ATR(14);
                atrAvg = SMA(atr, 14);
                volSma = SMA(Volume, 14);
                atrSlow = SMA(ATR(14), 50);
                atrTen = SMA(ATR(14), 10);
                biasSma = SMA(20);
                emaDaily = EMA(DailyTrendPeriod);
                ema50 = EMA(50);
                sessionPV = 0.0;
                sessionVol = 0.0;
                vwap = 0.0;
                currentDay = Times[0][0].Date;
                double acc = 0;
                try { acc = Account.Get(AccountItem.CashValue, Currency.UsDollar); } catch { }
                equityHigh = acc;
                equityPeak = acc;
                equityDrawdown = 0;
                currentQty = Quantity;
                pauseUntilTime = DateTime.MinValue;
                pauseStage = 0;
                strictTrailing = false;
                marketCycleMode = "TRENDING";
                lastExitPrice = double.NaN;
                cumDelta5m = 0;
                try
                {
                    string logDir = System.IO.Path.Combine(NinjaTrader.Core.Globals.UserDataDir, "MagnoFluxLogs");
                    System.IO.Directory.CreateDirectory(logDir);
                    string logPath = System.IO.Path.Combine(logDir, $"MagnoFlux_v6_log_{DateTime.Now:yyyyMMdd}.csv");
                    logWriter = new StreamWriter(logPath, true) { AutoFlush = true };
                }
                catch (Exception ex)
                {
                    Print("Error al abrir logWriter: " + ex.Message);
                    logWriter = null;
                }
                if (logWriter != null && logWriter.BaseStream.Length == 0)
                    logWriter.WriteLine("Time,Direction,Entry,Exit,Type,PNL,Score,Hz,EquityPeak,EquityDrawdown,TP1Hit,TP2Hit,SmartExitUsed,MaxFavorableTicks,MaxUnfavorableTicks,BarsHeld,TradeDuration,TP1Price,TP2Price,TPFinalPrice,SLPrice,EntryTimeOnly,ExitTimeOnly,EntryBarIndex,ExitBarIndex,Partial1Qty,Partial2Qty,FinalQty,NetContractsClosed,TradeEfficiency,AccountName,Instrument,MarketCycle,PauseMinutesRemaining,HybridSL,SwingStop,AtrStop,UnrealizedTicks,HzAtEntry,InstScore,MarketCycleMode,Consolidating,FrequencyOK,TimeBlocked");
            }
            else if (State == State.Terminated)
            {
                try { logWriter?.FlushAsync().Wait(); } catch { }
                try { logWriter?.Dispose(); } catch { }
            }
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress == 1)
            {
                if (CurrentBars[1] < 1)
                    return;
                cumDelta5m += (Closes[1][0] - Closes[1][1]) * Volumes[1][0];
                return;
            }
            if (CurrentBar < 2)
                return;

            if (Bars.IsFirstBarOfSession)
            {
                sessionPV = Close[0] * Volume[0];
                sessionVol = Volume[0];
            }
            else
            {
                sessionPV += Close[0] * Volume[0];
                sessionVol += Volume[0];
            }
            vwap = sessionVol > 0 ? sessionPV / sessionVol : Close[0];

            UpdateHz();
            UpdateMarketCycleMode();

            if (tradingPaused && Time[0] >= pauseUntilTime && Position.MarketPosition == MarketPosition.Flat)
                tradingPaused = false;
            if (Times[0][0].Date != currentDay)
            {
                currentDay = Times[0][0].Date;
                dailyPnL = 0;
                consecutiveSL = 0;
                tradingPaused = false;
                pauseUntilTime = DateTime.MinValue;
                pauseStage = 0;
                cumDelta5m = 0;
                double acc = 0; try { acc = Account.Get(AccountItem.CashValue, Currency.UsDollar); } catch { }
                equityHigh = acc;
                equityPeak = acc;
            }

            double unrealized = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]);
            double dayPnl = dailyPnL + unrealized;
            if (MaxDailyDrawdownEnabled && dayPnl <= -MaxDailyDrawdown && !tradingPaused)
            {
                if (Position.MarketPosition == MarketPosition.Long)
                    ExitLong("DailyLossExit");
                else if (Position.MarketPosition == MarketPosition.Short)
                    ExitShort("DailyLossExit");
                tradingPaused = true;
                pauseUntilTime = currentDay.AddDays(1);
                Print($"STOP-DAY 🔴 Pérdida diaria = {dayPnl:C} → trading suspendido");
                return;
            }

            TimeSpan now = Times[0][0].TimeOfDay;
            bool timeBlocked = false;
            if (EnableTimeFilters)
            {
                if (BlockMorningOpen)
                {
                    TimeSpan s = new TimeSpan(9, 29, 0);
                    TimeSpan e = new TimeSpan(9, 36, 0);
                    if (now >= s && now <= e)
                        timeBlocked = true;
                }
                if (BlockAfternoonChaos)
                {
                    TimeSpan s = new TimeSpan(15, 0, 0);
                    TimeSpan e = new TimeSpan(15, 30, 0);
                    if (now >= s && now <= e)
                        timeBlocked = true;
                }
            }
            if (EnableMarketOpenFilter)
            {
                TimeSpan s = new TimeSpan(9,29,0);
                TimeSpan e = new TimeSpan(9,36,0);
                if (now >= s && now <= e)
                    timeBlocked = true;
            }

            if (NoTradeZoneStart != "00:00" || NoTradeZoneEnd != "00:00")
            {
                try
                {
                    TimeSpan ns = TimeSpan.Parse(NoTradeZoneStart);
                    TimeSpan ne = TimeSpan.Parse(NoTradeZoneEnd);
                    if (ns < ne && now >= ns && now <= ne)
                        timeBlocked = true;
                }
                catch { }
            }

            lastTimeBlocked = timeBlocked;

            if (tradingPaused && Position.MarketPosition == MarketPosition.Flat)
                return;
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
                bool freqOK = !EnableFrequencyFilter || currentHz >= MinHz;
                if (!freqOK || timeBlocked)
                    return;
                if (!double.IsNaN(lastExitPrice) && Math.Abs(Close[0] - lastExitPrice) < ReentryAtrBuffer * atr[0])
                    return;
                double rangeBar = High[0] - Low[0];
                double bodyBar = Math.Abs(Close[0] - Open[0]);
                if (rangeBar == 0 || bodyBar / rangeBar < 0.6)
                    return;
                if (EnableConsolidationFilter && IsConsolidating())
                    return;
                int scoreL = GetInstitutionalScore(true);
                int scoreS = GetInstitutionalScore(false);
                int bias = GetInstitutionalBias();

                int bestScore = Math.Max(scoreL, scoreS);
                if (currentHz > 0.06 && bestScore >= 5)
                    currentQty = Math.Min(currentQty + 1, MaxContracts);
                else if (currentHz < 0.02 && bestScore <= 4)
                    currentQty = Math.Max(1, currentQty - 1);

                bool dailyUp = !UseDailyTrendFilter || Close[0] >= emaDaily[0];
                bool dailyDown = !UseDailyTrendFilter || Close[0] <= emaDaily[0];

                if (scoreL >= MinInstitutionalScore && IsStrongLongSignal() && (!UseInstitutionalBias || bias == 1) && dailyUp)
                {
                    pendingLong = true;
                    signalBar = CurrentBar;
                    signalTime = Time[0];
                    scoreAtEntry = scoreL;
                }
                else if (scoreS >= Math.Max(MinInstitutionalScore, MinShortScore) && (BlockWeakShorts ? IsShortInstitutional() : IsStrongShortSignal()) && (!UseInstitutionalBias || bias == -1) && dailyDown)
                {
                    pendingShort = true;
                    signalBar = CurrentBar;
                    signalTime = Time[0];
                    scoreAtEntry = scoreS;
                }
            }

            ManagePosition();

            foreach (var sig in pendingSignals)
            {
                if (sig.Done || CurrentBar <= sig.EntryBar)
                    continue;

                bool hitTP = sig.IsLong ? High[0] >= sig.TP : Low[0] <= sig.TP;
                bool hitSL = sig.IsLong ? Low[0] <= sig.SL : High[0] >= sig.SL;

                if (hitTP || hitSL)
                {
                    string dir = sig.IsLong ? "LONG" : "SHORT";
                    string result = hitTP ? "TP" : "SL";
                    sig.BarsHeld = CurrentBar - sig.EntryBar;
                    sig.DurationSeconds = (Time[0] - sig.Time).TotalSeconds;
                    double denom = Math.Abs(sig.TP - sig.EntryPrice);
                    if (denom != 0)
                    {
                        double ratio = Math.Abs(Close[0] - sig.EntryPrice) / denom;
                        sig.TradeEfficiency = Math.Min(1.0, ratio) * 100.0;
                    }
                    else
                    {
                        sig.TradeEfficiency = 0;
                    }
                    string log = $"{dir}={sig.Time:HH:mm:ss} - Señal {sig.EntryPrice:F2} Entrada={sig.EntryPrice:F2} - TP={sig.TP:F2} - SL={sig.SL:F2} - Resultado={result} - Qty={sig.Quantity} - Bars={sig.BarsHeld} - Duration={sig.DurationSeconds:F0}s - Efficiency={sig.TradeEfficiency:F0}%";
                    Print(log.Replace('.', ','));
                    sig.Done = true;
                    Draw.VerticalLine(this, "exit" + CurrentBar.ToString(), 0, Brushes.Gold);
                }
            }
        }

        private void ExecuteEntry(bool isLong)
        {
            entryPrice = Open[0];
            entryBar = CurrentBar;
            entryBarIndex = CurrentBar;
            lastExitPrice = double.NaN;
            if (UseAtrTargets)
            {
                if (marketCycleMode == "RANGING")
                {
                    tpPrice = isLong ? entryPrice + atr[0] * 0.8 : entryPrice - atr[0] * 0.8;
                    slPrice = isLong ? entryPrice - atr[0] : entryPrice + atr[0];
                }
                else
                {
                    double factor = TpFactorTrending;
                    tpPrice = isLong ? entryPrice + atr[0] * TpMultiplier * factor : entryPrice - atr[0] * TpMultiplier * factor;
                    slPrice = isLong ? entryPrice - atr[0] * SlMultiplier : entryPrice + atr[0] * SlMultiplier;
                }
            }
            else
            {
                tpPrice = isLong ? High[2] : Low[2];
                slPrice = isLong ? Low[2] : High[2];
            }
            double p1 = 0.6;
            double p2 = 0.9;
            tp1Price = isLong ? entryPrice + (tpPrice - entryPrice) * p1 : entryPrice - (entryPrice - tpPrice) * p1;
            tp2Price = isLong ? entryPrice + (tpPrice - entryPrice) * p2 : entryPrice - (entryPrice - tpPrice) * p2;

            int swingLookBack = SwingLookBack;
            double lastSwingLow = MIN(Low, swingLookBack)[1];
            double lastSwingHigh = MAX(High, swingLookBack)[1];
            swingStop = isLong ? lastSwingLow - 2*TickSize : lastSwingHigh + 2*TickSize;
            atrEntry = atr[0];
            atrStop = isLong ? entryPrice - atrEntry*AtrMultiplier : entryPrice + atrEntry*AtrMultiplier;
            atrStopDominant = isLong ? atrStop > swingStop : atrStop < swingStop;
            hybridSL = isLong ? Math.Max(swingStop, atrStop) : Math.Min(swingStop, atrStop);
            slPrice = hybridSL;

            partial1Qty = (int)Math.Max(1, Math.Round(currentQty * 0.5));
            partial2Qty = (int)Math.Max(1, Math.Round(currentQty * 0.25));
            finalQty = currentQty - partial1Qty - partial2Qty;

            partial1Done = partial2Done = false;
            breakEvenDone = false;
            trailingActive = false;
            maxProfitRun = 0;
            tradePnl = 0;
            tradeDirection = isLong ? 1 : -1;
            tp1Hit = false;
            tp2Hit = false;
            smartExitUsed = false;
            maxFavorableTicks = 0;
            maxUnfavorableTicks = 0;
            barsHeld = 0;
            tradeDuration = 0;
            entryTimeOnly = Time[0].ToString("HH:mm:ss");
            exitTimeOnly = string.Empty;
            exitBarIndex = 0;
            entryQty = currentQty;
            netContractsClosed = 0;
            tradeEfficiency = 0;

            if (isLong)
                EnterLong(currentQty, "LongEntry");
            else
                EnterShort(currentQty, "ShortEntry");
            Draw.VerticalLine(this, "entry" + CurrentBar.ToString(), 0, isLong ? Brushes.Lime : Brushes.Red);
            Print($"{(isLong ? "LONG" : "SHORT") }={signalTime:HH:mm:ss} - Entrada={entryPrice:F2} - TP={tpPrice:F2} - SL={slPrice:F2}");

            double tp = isLong ? entryPrice + SignalPoints * TickSize : entryPrice - SignalPoints * TickSize;
            double sl = isLong ? entryPrice - SignalPoints * TickSize : entryPrice + SignalPoints * TickSize;

            pendingSignals.Add(new SignalData
            {
                SignalBar = CurrentBar - 1,
                EntryBar = CurrentBar,
                Time = signalTime,
                IsLong = isLong,
                EntryPrice = entryPrice,
                TP = tp,
                SL = sl,
                Quantity = currentQty
            });
        }

        private void ManagePosition()
        {
            if (Position.MarketPosition == MarketPosition.Flat)
                return;

            if (CurrentBar <= entryBar)
                return;

            bool isLong = Position.MarketPosition == MarketPosition.Long;
            double barHigh = High[1];
            double barLow = Low[1];
            double favor = isLong ? barHigh - entryPrice : entryPrice - barLow;
            double currentFavor = isLong ? High[1] - entryPrice : entryPrice - Low[1];
            double currentDraw = isLong ? entryPrice - Low[1] : High[1] - entryPrice;
            double favorTicks = favor / TickSize;
            int maeTicks = isLong ? (int)((entryPrice - Low[0]) / TickSize) : (int)((High[0] - entryPrice) / TickSize);
            if (currentFavor > maxFavorableTicks) maxFavorableTicks = currentFavor;
            if (currentDraw > maxUnfavorableTicks) maxUnfavorableTicks = currentDraw;
            if (favor > maxProfitRun)
                maxProfitRun = favor;

            double oldSl = slPrice;
            double newSwingStop = isLong ? MIN(Low, SwingLookBack)[0] - 2 * TickSize
                                         : MAX(High, SwingLookBack)[0] + 2 * TickSize;
            if (isLong)
            {
                swingStop = Math.Max(swingStop, newSwingStop);
                slPrice = Math.Max(slPrice, swingStop);
            }
            else
            {
                swingStop = Math.Min(swingStop, newSwingStop);
                slPrice = Math.Min(slPrice, swingStop);
            }

            if (atrStopDominant && atr[0] <= atrEntry * 0.8)
            {
                double newAtrStop = isLong ? entryPrice - atr[0] * AtrMultiplier : entryPrice + atr[0] * AtrMultiplier;
                if (isLong)
                {
                    if (newAtrStop > atrStop)
                        atrStop = newAtrStop;
                }
                else
                {
                    if (newAtrStop < atrStop)
                        atrStop = newAtrStop;
                }
            }

            if (isLong)
                slPrice = Math.Max(slPrice, atrStop);
            else
                slPrice = Math.Min(slPrice, atrStop);

            hybridSL = isLong ? Math.Max(swingStop, atrStop) : Math.Min(swingStop, atrStop);
            if (slPrice != oldSl)
                Print($"SL tight: {slPrice:F2}");
            if (!breakEvenDone && favorTicks >= BEThresholdTicks)
            {
                slPrice = entryPrice + (isLong ? TickSize : -TickSize);
                breakEvenDone = true;
            }

            bool structureBroken = isLong ? Low[0] <= newSwingStop : High[0] >= newSwingStop;
            if (structureBroken || maeTicks >= EmergencyTicks)
            {
                if (isLong)
                    ExitLong("EmergencyExit");
                else
                    ExitShort("EmergencyExit");
                smartExitUsed = true;
                return;
            }

            if (partial1Done && TrailingStopEnabled)
            {
                double retrLevel = 0.35;
                if (isLong)
                    slPrice = Math.Max(slPrice, entryPrice + maxProfitRun * (1 - retrLevel));
                else
                    slPrice = Math.Min(slPrice, entryPrice - maxProfitRun * (1 - retrLevel));
            }

            if (!partial1Done && ((isLong && barHigh >= tp1Price) || (!isLong && barLow <= tp1Price)))
            {
                if (isLong)
                    ExitLong(partial1Qty, "TP1", "");
                else
                    ExitShort(partial1Qty, "TP1", "");
                partial1Done = true;
                tp1Hit = true;
                maxFavorAfterTP1 = favor;
            }

            if (!partial2Done && ((isLong && barHigh >= tp2Price) || (!isLong && barLow <= tp2Price)))
            {
                if (isLong)
                    ExitLong(partial2Qty, "TP2", "");
                else
                    ExitShort(partial2Qty, "TP2", "");
                partial2Done = true;
                tp2Hit = true;
                trailingActive = true;
            }

            if (partial1Done)
            {
                if (favor > maxFavorAfterTP1)
                    maxFavorAfterTP1 = favor;
                if (favor <= maxFavorAfterTP1 * (1 - SmartExitRetrace))
                {
                    if (isLong)
                        ExitLong("SmartExit");
                    else
                        ExitShort("SmartExit");
                    smartExitUsed = true;
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
                        lastTradeWin = false;
                        consecutiveSL++;
                    }
                    else
                    {
                        ExitLong("TP");
                        lastTradeWin = true;
                    }
                }
                else if (barLow <= slPrice)
                {
                    ExitLong("SL");
                    lastTradeWin = false;
                    consecutiveSL++;
                }
                else if (barHigh >= tpPrice)
                {
                    ExitLong("TP");
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
                        lastTradeWin = false;
                        consecutiveSL++;
                    }
                    else
                    {
                        ExitShort("TP");
                        lastTradeWin = true;
                    }
                }
                else if (barHigh >= slPrice)
                {
                    ExitShort("SL");
                    lastTradeWin = false;
                    consecutiveSL++;
                }
                else if (barLow <= tpPrice)
                {
                    ExitShort("TP");
                    lastTradeWin = true;
                }
            }

            if (Position.MarketPosition == MarketPosition.Flat && !lastTradeWin && consecutiveSL >= 3)
            {
                pendingLong = pendingShort = false;
            }
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (execution.Order.OrderState != OrderState.Filled)
                return;

            bool isLong = tradeDirection == 1;

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
                barsHeld = CurrentBar - entryBar;
                tradeDuration = (time - entryExecTime).TotalSeconds;
                exitTimeOnly = time.ToString("HH:mm:ss");
                exitBarIndex = CurrentBar;
                int unrealTicks = (isLong ? (int)((Close[0] - entryPrice)/TickSize) : (int)((entryPrice - Close[0])/TickSize));
                netContractsClosed += quantity;
                double targetDist = Math.Abs(tpPrice - entryPrice);
                double exitDist = Math.Abs(price - entryPrice);
                tradeEfficiency = targetDist > 0 ? Math.Min(1.0, exitDist / targetDist) * 100.0 : 0;
                lastExitPrice = price;
                bool consolidating = IsConsolidating();
                bool freqOK = !EnableFrequencyFilter || currentHz >= MinHz;
                string line = $"{entryExecTime:yyyy-MM-dd HH:mm:ss},{(isLong ?"Long":"Short")},{entryPrice:F2},{price:F2},{execution.Order.Name},{tradePnl:F2},{scoreAtEntry},{currentHz:F4},{equityPeak:F2},{equityDrawdown:F2}";
                line += $",{tp1Hit},{tp2Hit},{smartExitUsed},{maxFavorableTicks:F2},{maxUnfavorableTicks:F2}";
                line += $",{barsHeld},{tradeDuration:F0},{tp1Price:F2},{tp2Price:F2},{tpPrice:F2},{slPrice:F2},{entryTimeOnly},{exitTimeOnly},{entryBarIndex},{exitBarIndex},{partial1Qty},{partial2Qty},{finalQty},{entryQty},{tradeEfficiency:F2}";
                int remain = tradingPaused ? (int)Math.Max(0, (pauseUntilTime - Time[0]).TotalMinutes) : 0;
                line += $",{Account?.Name},{Instrument?.FullName},{marketCycleMode},{remain},{hybridSL:F2},{swingStop:F2},{atrStop:F2},{unrealTicks},{currentHz:F4},{scoreAtEntry},{marketCycleMode},{consolidating},{freqOK},{lastTimeBlocked}";
                try { logWriter?.WriteLine(line); logWriter?.Flush(); } catch { }
                dailyPnL += tradePnl;
                if (acc > equityHigh) equityHigh = acc;
                bool lost = execution.Order.Name == "SL" || execution.Order.Name == "EmergencyExit";
                if (lost)
                {
                    if (pauseStage == 1)
                    {
                        StartPauseMinutes(SecondPauseMinutes);
                        pauseStage = 2;
                    }
                    consecutiveSL++;
                    if (consecutiveSL == 2)
                        currentQty = Math.Max(1, currentQty / 2);
                    if (consecutiveSL >= 3 && pauseStage == 0)
                    {
                        StartPauseMinutes(FirstPauseMinutes);
                        pauseStage = 1;
                        consecutiveSL = 0;
                    }
                }
                else if (execution.Order.Name == "TP")
                {
                    consecutiveSL = 0;
                    pauseStage = 0;
                }
            }
            else
            {
                tradePnl += tradeDirection * (price - entryPrice) * quantity * (Instrument.MasterInstrument.PointValue == 0 ? 1 : Instrument.MasterInstrument.PointValue);
                netContractsClosed += quantity;
                if (execution.Order.Name == "TP1" || execution.Order.Name == "TP2")
                {
                    double acc = 0; try { acc = Account.Get(AccountItem.CashValue, Currency.UsDollar); } catch { }
                    if (acc > equityPeak) equityPeak = acc;
                    equityDrawdown = equityPeak - acc;
                    barsHeld = CurrentBar - entryBar;
                    tradeDuration = (time - entryExecTime).TotalSeconds;
                    exitTimeOnly = time.ToString("HH:mm:ss");
                    exitBarIndex = CurrentBar;
                    int unrealTicks = (isLong ? (int)((Close[0] - entryPrice)/TickSize) : (int)((entryPrice - Close[0])/TickSize));
                    double targetDist = Math.Abs(tpPrice - entryPrice);
                    double exitDist = Math.Abs(price - entryPrice);
                    tradeEfficiency = targetDist > 0 ? Math.Min(1.0, exitDist / targetDist) * 100.0 : 0;
                    bool consolidating = IsConsolidating();
                    bool freqOK = !EnableFrequencyFilter || currentHz >= MinHz;
                    string line = $"{entryExecTime:yyyy-MM-dd HH:mm:ss},{(isLong ?"Long":"Short")},{entryPrice:F2},{price:F2},{execution.Order.Name},{tradePnl:F2},{scoreAtEntry},{currentHz:F4},{equityPeak:F2},{equityDrawdown:F2}";
                    int remain = tradingPaused ? (int)Math.Max(0, (pauseUntilTime - Time[0]).TotalMinutes) : 0;
                    line += $",{tp1Hit},{tp2Hit},{smartExitUsed},{maxFavorableTicks:F2},{maxUnfavorableTicks:F2},{barsHeld},{tradeDuration:F0},{tp1Price:F2},{tp2Price:F2},{tpPrice:F2},{slPrice:F2},{entryTimeOnly},{exitTimeOnly},{entryBarIndex},{exitBarIndex},{partial1Qty},{partial2Qty},{finalQty},{entryQty},{tradeEfficiency:F2},{Account?.Name},{Instrument?.FullName},{marketCycleMode},{remain},{hybridSL:F2},{swingStop:F2},{atrStop:F2},{unrealTicks},{currentHz:F4},{scoreAtEntry},{marketCycleMode},{consolidating},{freqOK},{lastTimeBlocked}";
                    try { logWriter?.WriteLine(line); logWriter?.Flush(); } catch { }
                }
            }
        }

        private void UpdateHz()
        {
            signalTimes.Add(Time[0]);
            DateTime threshold = Time[0].AddSeconds(-30);
            signalTimes.RemoveAll(t => t < threshold);
            if (signalTimes.Count > 1)
            {
                double seconds = (signalTimes[signalTimes.Count - 1] - signalTimes[0]).TotalSeconds;
                if (seconds <= 0)
                {
                    currentHz = 0;
                    return;
                }
                currentHz = (signalTimes.Count - 1) / seconds;
            }
        }

        private int GetInstitutionalBias()
        {
            int bull = 0;
            int bear = 0;

            if (Close[0] > Close[1] && Close[1] > Close[2])
                bull++;
            else if (Close[0] < Close[1] && Close[1] < Close[2])
                bear++;

            if (Volume[0] > volSma[0])
            {
                if (Close[0] > Open[0])
                    bull++;
                else if (Close[0] < Open[0])
                    bear++;
            }

            if (Close[0] > biasSma[0])
                bull++;
            else if (Close[0] < biasSma[0])
                bear++;

            if (bull >= 2)
                return 1;
            if (bear >= 2)
                return -1;
            return 0;
        }

        private int GetTrendBias()
        {
            if (CurrentBar < ema50.Period)
                return 0;
            int bias = 0;
            double vwapValue = vwap;
            if (Close[0] > vwapValue) bias++;
            else if (Close[0] < vwapValue) bias--;
            double emaSlope = ema50[0] - ema50[1];
            if (emaSlope > 0) bias++; else if (emaSlope < 0) bias--;
            if (cumDelta5m > 0) bias++; else if (cumDelta5m < 0) bias--;
            if (bias >= 2) return 1;
            if (bias <= -2) return -1;
            return 0;
        }

        private int GetInstitutionalScore(bool isLong)
        {
            int score = 0;
            double range = High[0] - Low[0];
            double body = Math.Abs(Close[0] - Open[0]);
            bool breakout = isLong ? Close[0] > High[1] : Close[0] < Low[1];
            if (Volume[0] > volSma[0] * 1.5) score++;
            if (range > atr[0]) score++;
            if (range > 0 && body / range > 0.7) score++;
            if (breakout) score++;
            bool prevOppHighVol = Volume[1] > volSma[1] * 1.5 && ((Close[1] > Open[1]) != isLong);
            if (prevOppHighVol) score++;
            bool momentum = true;
            for (int i = 0; i < 3; i++)
            {
                bool bull = Close[i] > Open[i];
                if ((isLong && !bull) || (!isLong && bull))
                {
                    momentum = false;
                    break;
                }
            }
            if (momentum) score++;
            return score;
        }

        private bool IsStrongLongSignal()
        {
            return GetInstitutionalScore(true) >= MinInstitutionalScore;
        }
        private bool IsStrongShortSignal()
        {
            return GetInstitutionalScore(false) >= MinInstitutionalScore;
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

        private bool IsConsolidating()
        {
            if (CurrentBar < 10)
                return false;
            int changes = 0;
            bool prevBull = Close[1] > Open[1];
            double maxHigh = High[1];
            double minLow = Low[1];
            for (int i = 2; i <= 10; i++)
            {
                bool bull = Close[i] > Open[i];
                if (bull != prevBull)
                    changes++;
                prevBull = bull;
                if (High[i] > maxHigh) maxHigh = High[i];
                if (Low[i] < minLow) minLow = Low[i];
            }
            bool condColor = changes >= 3;
            bool condRange = (maxHigh - minLow) < 3 * TickSize;
            return condColor || condRange;
        }

        private void UpdateMarketCycleMode()
        {
            if (CurrentBar < Math.Max(atrSlow.Period, atrTen.Period))
                return;
            double ratio = atr[0] / atrSlow[0];
            double bodyRatio = 0.0;
            for (int i = 0; i < 10; i++)
            {
                double r = High[i] - Low[i];
                if (r > 0)
                    bodyRatio += Math.Abs(Close[i] - Open[i]) / r;
            }
            bodyRatio /= 10.0;
            if (ratio > 1.1)
                marketCycleMode = "TRENDING";
            else if (bodyRatio < 0.35 && atr[0] < atrTen[0] * 0.8)
                marketCycleMode = "RANGING";
        }

        private void StartPauseMinutes(int minutes)
        {
            pauseUntilTime = Time[0].AddMinutes(minutes);
            tradingPaused = true;
            currentQty = 1;
            Print($"⏸ Pausa de trading por {minutes} minutos");
        }
    }
}
