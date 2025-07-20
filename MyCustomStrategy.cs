#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    /// <summary>
    /// MyCustomStrategy: adapta ProbSignalPro a una estrategia para backtest y gráfico.
    /// </summary>
    public class MyCustomStrategy : Strategy
    {
        private EMA ema;
        private RSI rsi;
        private ATR atr;
        private int lastSignalBar = -1000;

        private class SigInfo 
        { 
            public int Bar; 
            public bool IsLong; 
            public double Entry; 
            public bool Done; 
        }
        private List<SigInfo> pending = new List<SigInfo>();

        #region Parámetros
        [NinjaScriptProperty, Display(Name="Beta0", GroupName="Coefficients")]
        public double Beta0 { get; set; } = -1.234;
        [NinjaScriptProperty, Display(Name="BetaEmaSlope", GroupName="Coefficients")]
        public double BetaEmaSlope { get; set; } = 0.567;
        [NinjaScriptProperty, Display(Name="BetaRsiNorm", GroupName="Coefficients")]
        public double BetaRsiNorm { get; set; } = 0.345;
        [NinjaScriptProperty, Display(Name="BetaAtrNorm", GroupName="Coefficients")]
        public double BetaAtrNorm { get; set; } = -0.123;
        [NinjaScriptProperty, Display(Name="BetaDistEmaNorm", GroupName="Coefficients")]
        public double BetaDistEmaNorm { get; set; } = 0.789;
        [NinjaScriptProperty, Display(Name="BetaMomNorm", GroupName="Coefficients")]
        public double BetaMomNorm { get; set; } = 0.456;

        [NinjaScriptProperty, Range(0,1), Display(Name="Threshold", GroupName="Settings")]
        public double Threshold { get; set; } = 0.60;
        [NinjaScriptProperty, Range(1,100), Display(Name="CooldownBars", GroupName="Settings")]
        public int CooldownBars { get; set; } = 5;
        [NinjaScriptProperty, Range(1,100), Display(Name="TpTicks", GroupName="Settings")]
        public int TpTicks { get; set; } = 40;
        [NinjaScriptProperty, Range(1,100), Display(Name="SlTicks", GroupName="Settings")]
        public int SlTicks { get; set; } = 40;
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name                            = "MyCustomStrategy";
                Calculate                       = Calculate.OnBarClose;
                EntriesPerDirection            = 1;
                EntryHandling                  = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy   = true;
                // Opcional: TP/SL automático
                // SetProfitTarget(CalculationMode.Ticks, TpTicks);
                // SetStopLoss(CalculationMode.Ticks, SlTicks);
            }
            else if (State == State.DataLoaded)
            {
                ema = EMA(14);
                rsi = RSI(14, 1);
                atr = ATR(14);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 20) return;

            // 1) Calcula variables
            double emaSlope  = (ema[0]  - ema[1])  / TickSize;
            double rsiNorm   =  rsi[0]  / 100.0;
            double atrTicks  =  atr[0]  / TickSize;
            double distTicks = Math.Abs(Close[0] - ema[0]) / TickSize;
            double momTicks  = (Close[0] - Close[1]) / TickSize;

            // 2) Probabilidad via regresión logística
            double lin = Beta0
                       + BetaEmaSlope    * emaSlope
                       + BetaRsiNorm     * rsiNorm
                       + BetaAtrNorm     * atrTicks
                       + BetaDistEmaNorm * distTicks
                       + BetaMomNorm     * momTicks;
            double prob = 1 / (1 + Math.Exp(-lin));

            // 3) Señal si supera umbral y respeta cooldown
            bool sigL = prob >= Threshold && Close[0] > ema[0] && CurrentBar >= lastSignalBar + CooldownBars;
            bool sigS = prob >= Threshold && Close[0] < ema[0] && CurrentBar >= lastSignalBar + CooldownBars;

            if (sigL || sigS)
            {
                bool isLong     = sigL;
                double entryPr  = Open[0];
                pending.Add(new SigInfo { Bar = CurrentBar, IsLong = isLong, Entry = entryPr });
                lastSignalBar = CurrentBar;

                // ** Ejecuta la entrada real para backtest **
                if (isLong)
                    EnterLong(1, $"L{CurrentBar}");
                else
                    EnterShort(1, $"S{CurrentBar}");

                // Dibuja flecha en gráfico
                #if CHART
                if (isLong)
                    Draw.ArrowUp(this, $"L{CurrentBar}", false, 0, Low[0]  - TickSize*2, Brushes.LimeGreen);
                else
                    Draw.ArrowDown(this, $"S{CurrentBar}", false, 0, High[0] + TickSize*2, Brushes.Red);
                #endif
            }

            // 4) Evaluar TP/SL en pendientes y ejecutar salida
            foreach (var s in pending)
            {
                if (s.Done) continue;

                bool tp = s.IsLong
                          ? High[0] >= s.Entry + TpTicks * TickSize
                          : Low[0]  <= s.Entry - SlTicks * TickSize;

                bool sl = s.IsLong
                          ? Low[0]  <= s.Entry - SlTicks * TickSize
                          : High[0] >= s.Entry + TpTicks * TickSize;

                // cierra si TP / SL o expiró cooldown
                if (tp || sl || CurrentBar >= s.Bar + CooldownBars)
                {
                    s.Done = true;
                    string res = tp ? "TP" : "SL";
                    DateTime sigTime = Time[s.Bar]; // hora original de la señal
                    Print($"{sigTime:HH:mm:ss} | {(s.IsLong?"LONG":"SHORT")} | P(TP)={prob:P1} | EMAtk={emaSlope:F1} | RSI={rsiNorm:F2} | ATRtk={atrTicks:F1} | DistTk={distTicks:F1} | MomTk={momTicks:F1} | {res}");

                    // Ejecuta la salida real
                    if (s.IsLong)
                        ExitLong($"L{s.Bar}", $"XLong{s.Bar}");
                    else
                        ExitShort($"S{s.Bar}", $"XShort{s.Bar}");
                }
            }
        }
    }
}
