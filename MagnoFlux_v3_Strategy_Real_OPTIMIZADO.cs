
// -------------------------------------------
// MagnoFlux_v3_Strategy_Real – Estrategia automática con la misma lógica
// -------------------------------------------
using System;
using System.Windows.Media;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class MagnoFlux_v3_Strategy_Real : Strategy
    {
        private double avgVolume;
        private double speedScore;
        private double prevSpeedScore;
        private int lastSignalBar = -1000;
        private SignalData sig;

        [NinjaScriptProperty] public int SignalPoints { get; set; } = 40;
        [NinjaScriptProperty] public int CooldownBars { get; set; } = 5;
        [NinjaScriptProperty] public int MinConditions { get; set; } = 3;
        [NinjaScriptProperty] public double VolumeThreshold { get; set; } = 1.8;
        [NinjaScriptProperty] public int SpeedTicks { get; set; } = 25;
        [NinjaScriptProperty] public double BodyContextRatio { get; set; } = 0.4;
        [NinjaScriptProperty] public double SlopeMin { get; set; } = 0.05;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "MagnoFlux_v3_Strategy_Real";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 30)
                return;

            if (sig != null && !sig.Done && sig.EntryBar == CurrentBar && double.IsNaN(sig.EntryPrice))
            {
                sig.EntryPrice = Open[0];
                sig.TP = sig.IsLong ? sig.EntryPrice + SignalPoints * TickSize : sig.EntryPrice - SignalPoints * TickSize;
                sig.SL = sig.IsLong ? sig.EntryPrice - SignalPoints * TickSize : sig.EntryPrice + SignalPoints * TickSize;

                if (sig.IsLong)
                    EnterLong();
                else
                    EnterShort();

                Print($"CONFIRM={sig.Time:HH:mm:ss} - {(sig.IsLong ? "LONG" : "SHORT")} Entrada={sig.EntryPrice:F2} TP={sig.TP:F2} SL={sig.SL:F2}");
            }

            if (sig != null && !sig.Done && CurrentBar > sig.EntryBar)
            {
                if (sig.IsLong && Low[0] <= sig.SL)
                {
                    ExitLong();
                    EndTrade("SL");
                }
                else if (sig.IsLong && High[0] >= sig.TP)
                {
                    ExitLong();
                    EndTrade("TP");
                }
                else if (!sig.IsLong && High[0] >= sig.SL)
                {
                    ExitShort();
                    EndTrade("SL");
                }
                else if (!sig.IsLong && Low[0] <= sig.TP)
                {
                    ExitShort();
                    EndTrade("TP");
                }
            }

            if (CurrentBar <= lastSignalBar + CooldownBars || (sig != null && !sig.Done))
                return;

            avgVolume = SMA(Volume, 14)[0];
            bool volumeSpike = Volume[0] > avgVolume * VolumeThreshold;

            speedScore = Math.Abs(Close[0] - Close[3]) / TickSize;
            prevSpeedScore = Math.Abs(Close[1] - Close[4]) / TickSize;
            bool fastMove = speedScore >= SpeedTicks;

            bool imbalance = (Close[0] > High[1] && Low[0] > Low[1]) || (Close[0] < Low[1] && High[0] < High[1]);
            bool trap = (High[1] > High[2] && Close[0] < Low[1]) || (Low[1] < Low[2] && Close[0] > High[1]);

            double body = Math.Abs(Close[0] - Open[0]);
            bool contextOk = body > (High[0] - Low[0]) * BodyContextRatio;

            int instFiltros = 0;
            if (volumeSpike) instFiltros++;
            if (fastMove) instFiltros++;
            if (imbalance) instFiltros++;
            if (trap) instFiltros++;
            if (contextOk) instFiltros++;

            double emaSlope = (EMA(20)[0] - EMA(20)[5]) / (5 * TickSize);
            double trNow = ATR(1)[0] / TickSize;
            double trAvg = ATR(14)[0] / TickSize;
            bool etapaExpansiva = emaSlope > SlopeMin && trNow > trAvg * 1.1;
            bool posibleShark = trNow < trAvg * 0.7 && speedScore < prevSpeedScore;
            bool vxAlto = speedScore >= 60;
            bool filtroFPLEME_OK = etapaExpansiva && vxAlto && !posibleShark;

            bool bullish = Close[0] > Open[0];
            bool bearish = Close[0] < Open[0];
            bool longCond = filtroFPLEME_OK && instFiltros >= MinConditions && bullish;
            bool shortCond = filtroFPLEME_OK && instFiltros >= MinConditions && bearish;

            if (longCond)
            {
                EnterSignal(true);
                lastSignalBar = CurrentBar;
            }
            else if (shortCond)
            {
                EnterSignal(false);
                lastSignalBar = CurrentBar;
            }
        }

        private void EnterSignal(bool isLong)
        {
            sig = new SignalData
            {
                Time = Time[0],
                EntryBar = CurrentBar + 1,
                EntryPrice = double.NaN,
                TP = 0,
                SL = 0,
                IsLong = isLong,
                Done = false
            };

            if (isLong)
                Draw.TriangleUp(this, "L" + CurrentBar, false, 0, Low[0] - TickSize, Brushes.Lime);
            else
                Draw.TriangleDown(this, "S" + CurrentBar, false, 0, High[0] + TickSize, Brushes.Red);

            Print($"SIGNAL={sig.Time:HH:mm:ss} - {(isLong ? "LONG" : "SHORT")} generada – entrada en próxima barra");
        }

        private void EndTrade(string result)
        {
            Print($"{result} en {Time[0]:HH:mm:ss}");
            sig.Done = true;
        }

        private class SignalData
        {
            public DateTime Time;
            public int EntryBar;
            public double EntryPrice;
            public double TP;
            public double SL;
            public bool IsLong;
            public bool Done;
        }
    }
}
