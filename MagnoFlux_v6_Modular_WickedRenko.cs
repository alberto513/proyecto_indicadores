// ===============================================================
//  MagnoFlux_v6_Modular_WickedRenko – ARQUITECTURA BASE
// ===============================================================
//  Objetivo: construir una estrategia NinjaTrader 8 totalmente modular
//  para gráficos Wicked Renko, con activación por frecuencia (Hz),
//  ScoreInstitucional dinámico, Trailing 2.0 y seis módulos de señal.
//
//  Módulos a implementar (EnableX bool por parámetro):
//    1. TrendRider_Guppy
//    2. DualRenko_KC_Break
//    3. SuperTrend_Scalper
//    4. VWAP_Bounce
//    5. Imbalance_Fade
//    6. SpeedTape_Momo
//
//  REQUISITOS GLOBALES
//  • Entrada real en Open[1] después de la flecha/condición.
//  • TP/SL configurables en ticks; Trailing 2.0 con parciales.
//  • Calcular ScoreInstitucional sumando puntos (+) y restando (−).
//  • Ejecutar trade solo si (Score ≥ GlobalThreshold) Y (MarketRhythm dentro de rango módulo).
//  • Logging CSV: hh:mm:ss sin fecha, formato
//      LONG=hh:mm:ss - Señal 18520,25 Entrada=18522,25 - TP=… - SL=… - Resultado=TP
//  • No eliminar ninguna línea existente cuando edites.
//  • Código limpio, métodos auxiliares; Calculate = OnPriceChange.
//  • Compatible con TickReplay & WickedRenko.
// ===============================================================

#region Usings
using System;
using System.Collections.Generic;
using System.IO;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.Data;
using System.Windows.Media;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class MagnoFlux_v6_Modular_WickedRenko : Strategy
    {
        // === ENUM & DATA ===
        private enum Module { TrendRider, DualBreak, STScalp, VWAPRev, ImbFade, SpeedMomo }
        private Dictionary<Module, bool> moduleEnabled;
        private Dictionary<Module, int> moduleScore;
        private double currentHz;

        private EMA ema130;
        private ATR atr14;
        private SMA volSma;
        private SuperTrend stScalp;
        private ATR atr50m;
        private KeltnerChannel kcFast;
        private Stochastics stochFast;
        private SMA sma50Slow;
        private VWAP vwap;
        private double cvd;

        #region Parameters
        [NinjaScriptProperty] public bool EnableTrendRider { get; set; } = true;
        [NinjaScriptProperty] public bool EnableDualBreak  { get; set; } = true;
        [NinjaScriptProperty] public bool EnableSTScalp    { get; set; } = true;
        [NinjaScriptProperty] public bool EnableVWAPRev    { get; set; } = true;
        [NinjaScriptProperty] public bool EnableImbFade    { get; set; } = true;
        [NinjaScriptProperty] public bool EnableSpeedMomo  { get; set; } = true;

        [NinjaScriptProperty] public int  GlobalThreshold  { get; set; } = 4;
        [NinjaScriptProperty] public int  TicksTP          { get; set; } = 40;   // 10 pts NQ
        [NinjaScriptProperty] public int  TicksSL          { get; set; } = 20;   // 5 pts NQ
        [NinjaScriptProperty] public bool UseTrailing      { get; set; } = true;

        // TODO: expone parámetros específicos de cada módulo si lo deseás
        #endregion

        // === STATE MANAGEMENT ===
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name       = "MagnoFlux_v6_Modular_WickedRenko";
                Calculate  = Calculate.OnPriceChange;
                IsOverlay  = true;
            }
            else if (State == State.Configure)
            {
                moduleEnabled = new()
                {
                    { Module.TrendRider, EnableTrendRider },
                    { Module.DualBreak , EnableDualBreak  },
                    { Module.STScalp   , EnableSTScalp    },
                    { Module.VWAPRev   , EnableVWAPRev    },
                    { Module.ImbFade   , EnableImbFade    },
                    { Module.SpeedMomo , EnableSpeedMomo  },
                };
                moduleScore = new();

                AddDataSeries(BarsPeriodType.Renko, 18); // BrickFast
                AddDataSeries(BarsPeriodType.Renko, 40); // BrickSlow
                AddDataSeries(BarsPeriodType.Minute, 50); // ATR filter timeframe
            }
            else if (State == State.DataLoaded)
            {
                ema130  = EMA(130);
                atr14   = ATR(14);
                volSma  = SMA(Volume, 20);
                kcFast  = KeltnerChannel(BarsArray[1], 20, 1.5);
                stochFast = Stochastics(BarsArray[1], 7, 3, 3);
                sma50Slow = SMA(BarsArray[2], 50);

                stScalp = SuperTrend(14, 3);
                atr50m  = ATR(BarsArray[3], 14);
                vwap    = VWAP();
            }
            // TODO: DataLoaded – inicializa indicadores comunes (EMA, ATR, VWAP, etc.)
        }

        protected override void OnBarUpdate()
        {
            if (BarsInProgress != 0)
                return;

            if (IsFirstTickOfBar && CurrentBar > 0)
                cvd += (Close[1] - Open[1]) * Volume[1];

            if (CurrentBar < 150) return;
            UpdateMarketRhythm();
            moduleScore.Clear();

            // === LLAMAR A CADA MÓDULO PARA OBTENER SCORE ===
            if (moduleEnabled[Module.TrendRider]) EvaluateTrendRider();
            if (moduleEnabled[Module.DualBreak ]) EvaluateDualBreak();
            if (moduleEnabled[Module.STScalp   ]) EvaluateSTScalp();
            if (moduleEnabled[Module.VWAPRev   ]) EvaluateVWAPBounce();
            if (moduleEnabled[Module.ImbFade   ]) EvaluateImbalanceFade();
            if (moduleEnabled[Module.SpeedMomo ]) EvaluateSpeedTape();

            // === SELECCIONAR MEJOR MÓDULO ===
            Module? best = null;
            int bestScore = int.MinValue;
            foreach (var kv in moduleScore)
                if (kv.Value > bestScore) { bestScore = kv.Value; best = kv.Key; }

            if (best != null && bestScore >= GlobalThreshold && Position.MarketPosition == MarketPosition.Flat)
                ExecuteTrade(best.Value, bestScore);
            
            ManageOpenPosition();
        }

        // === MÉTODOS VACÍOS A IMPLEMENTAR POR CADA SUB-PROMPT ===
        private void EvaluateTrendRider()
        {
            if (CurrentBar < 131)
                return;

            bool biasUp   = ema130[0] > ema130[1] && Close[0] > ema130[0];
            bool biasDown = ema130[0] < ema130[1] && Close[0] < ema130[0];

            double dist = Math.Abs(Close[0] - ema130[0]);
            bool pullbackLong  = biasUp && dist < 0.5 * atr14[0];
            bool pullbackShort = biasDown && dist < 0.5 * atr14[0];

            double range = High[0] - Low[0];
            double body  = Math.Abs(Close[0] - Open[0]);
            bool bigBody = range > 0 && body / range > 0.7;
            bool volSpike = Volume[0] > volSma[0] * 1.5;

            int score = 0;
            if (volSpike) score += 2;
            if (bigBody)  score += 1;

            if (biasUp && pullbackLong && Close[0] > Open[0])
            {
                if (Close[0] > High[1]) score += 1;
                moduleScore[Module.TrendRider] = score;
            }
            else if (biasDown && pullbackShort && Close[0] < Open[0])
            {
                if (Close[0] < Low[1]) score += 1;
                moduleScore[Module.TrendRider] = score;
            }
        }
        private void EvaluateDualBreak()
        {
            if (CurrentBars[1] < 20 || CurrentBars[2] < 50)
                return;

            double closeFast = Closes[1][0];
            bool breakoutLong  = closeFast > kcFast.Upper[0];
            bool breakoutShort = closeFast < kcFast.Lower[0];
            bool momentumLong  = stochFast.K[0] > stochFast.D[0];
            bool momentumShort = stochFast.K[0] < stochFast.D[0];
            bool trendUp   = Closes[2][0] > sma50Slow[0] && Closes[2][0] > Opens[2][0];
            bool trendDown = Closes[2][0] < sma50Slow[0] && Closes[2][0] < Opens[2][0];

            int score = 0;

            if (breakoutLong && momentumLong && trendUp)
            {
                score = 0;
                if (breakoutLong)  score += 2;
                if (momentumLong)  score += 1;
                if (trendUp)       score += 1;
                moduleScore[Module.DualBreak] = score;
            }
            else if (breakoutShort && momentumShort && trendDown)
            {
                score = 0;
                if (breakoutShort) score += 2;
                if (momentumShort) score += 1;
                if (trendDown)     score += 1;
                moduleScore[Module.DualBreak] = score;
            }
        }
        private void EvaluateSTScalp()
        {
            if (CurrentBars[3] < 10)
                return;

            if (currentHz < 0.02 || currentHz > 0.05)
                return;

            double atrValue = atr50m[0];
            if (atrValue <= TickSize * 3)
                return;

            bool upNow   = !double.IsNaN(stScalp.UpTrend[0]);
            bool upPrev  = !double.IsNaN(stScalp.UpTrend[1]);
            bool turnUp  = upNow && !upPrev;
            bool turnDn  = !upNow && upPrev;

            if (!turnUp && !turnDn)
                return;

            bool volSpike = Volume[0] > volSma[0] * 1.5;
            double delta = Close[0] - Close[1];
            bool deltaOk = turnUp ? delta > 0 : delta < 0;

            int score = 2; // Cambio de color
            if (volSpike) score += 1;
            if (deltaOk)  score += 1;

            moduleScore[Module.STScalp] = score;
        }
        private void EvaluateVWAPBounce()
        {
            if (CurrentBar < 20)
                return;

            double price = Close[0];
            bool volSpike = Volume[0] > volSma[0] * 1.5;

            // --- LONG SETUP ---
            int redCount = 0;
            for (int i = 1; i <= 5; i++)
            {
                if (Close[i] < Open[i])
                    redCount++;
                else
                    break;
            }
            bool firstGreen = Close[0] > Open[0] && Close[1] < Open[1] && redCount >= 5;
            if (price < vwap.Lower2[0] && firstGreen && volSpike)
            {
                int score = 2; // Desviación
                if (volSpike) score += 1;
                bool strongDown = ema130[0] < ema130[1] && Close[0] < ema130[0];
                if (strongDown) score -= 1;
                moduleScore[Module.VWAPRev] = score;
                return;
            }

            // --- SHORT SETUP ---
            int greenCount = 0;
            for (int i = 1; i <= 5; i++)
            {
                if (Close[i] > Open[i])
                    greenCount++;
                else
                    break;
            }
            bool firstRed = Close[0] < Open[0] && Close[1] > Open[1] && greenCount >= 5;
            if (price > vwap.Upper2[0] && firstRed && volSpike)
            {
                int score = 2; // Desviación
                if (volSpike) score += 1;
                bool strongUp = ema130[0] > ema130[1] && Close[0] > ema130[0];
                if (strongUp) score -= 1;
                moduleScore[Module.VWAPRev] = score;
            }
        }
        private void EvaluateImbalanceFade()
        {
            if (CurrentBar < 10)
                return;

            double buyVolPrev  = Close[1] > Open[1] ? Volume[1] : 0;
            double sellVolPrev = Volume[1] - buyVolPrev;
            double denom       = buyVolPrev + sellVolPrev;
            double imbalance   = denom > 0 ? Math.Abs(buyVolPrev - sellVolPrev) / denom * 100.0 : 100.0;

            bool extremeHigh = High[1] >= MAX(High, 5)[2];
            bool extremeLow  = Low[1]  <= MIN(Low, 5)[2];
            bool oppColor    = (Close[0] > Open[0]) != (Close[1] > Open[1]);

            double deltaNow  = (Close[0] - Open[0]) * Volume[0];
            bool priceUp     = Close[0] > Close[1];
            bool deltaUp     = deltaNow > 0;
            bool cvdDiv      = priceUp != deltaUp;

            if (imbalance < 5 && oppColor && cvdDiv && (extremeHigh || extremeLow))
            {
                int score = 2; // Imbalance
                if (cvdDiv) score += 1;
                moduleScore[Module.ImbFade] = score;
            }
        }
        private void EvaluateSpeedTape()    { /* TODO */ }

        private void ExecuteTrade(Module m, int score) { /* TODO: entrada, TP/SL, logging */ }
        private void ManageOpenPosition()              { /* TODO: parciales, trailing 2.0 */ }
        private void UpdateMarketRhythm()              { /* TODO: calcular Hz con ventana 30 señales */ }
    }
}
