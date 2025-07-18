// AceRanger - Indicador profesional para MNQ/NQ en gráficos Wicked Renko (22r)
// Autor: BetaSensei - Desarrollado para Magno
// Descripción: Señales LONG/SHORT precisas con TP/SL, formateadas para análisis humano/IA.

using System;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.Indicators;
using System.Globalization;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class AceRanger : Indicator
    {
        private double entryPrice, tpPrice, slPrice;
        private int signalBarIndex = -1;
        private string signalDirection = "";
        private bool tradeEvaluated = false;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        public int SignalPoints { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "AceRanger - Señales profesionales en gráficos Wicked Renko 22r MNQ/NQ";
                Name = "AceRanger";
                Calculate = Calculate.OnEachTick;
                IsOverlay = false;
                SignalPoints = 40; // 40 ticks = 10 puntos en MNQ/NQ
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 20) return;

            if (signalBarIndex == -1)
            {
                if (CrossAbove(SMA(14), SMA(50), 1))
                {
                    signalBarIndex = CurrentBar;
                    signalDirection = "LONG";
                    PrintSignal();
                }
                else if (CrossBelow(SMA(14), SMA(50), 1))
                {
                    signalBarIndex = CurrentBar;
                    signalDirection = "SHORT";
                    PrintSignal();
                }
            }
            else if (CurrentBar == signalBarIndex + 1)
            {
                entryPrice = Open[0];
                tpPrice = signalDirection == "LONG" ? entryPrice + TickSize * SignalPoints : entryPrice - TickSize * SignalPoints;
                slPrice = signalDirection == "LONG" ? entryPrice - TickSize * SignalPoints : entryPrice + TickSize * SignalPoints;
            }
            else if (!tradeEvaluated && CurrentBar > signalBarIndex + 1)
            {
                string resultado = "";

                if (signalDirection == "LONG")
                {
                    if (High[0] >= tpPrice)
                        resultado = "TP";
                    else if (Low[0] <= slPrice)
                        resultado = "SL";
                }
                else if (signalDirection == "SHORT")
                {
                    if (Low[0] <= tpPrice)
                        resultado = "TP";
                    else if (High[0] >= slPrice)
                        resultado = "SL";
                }

                if (resultado != "")
                {
                    string hora = Time[signalBarIndex].ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                    string linea = $"{signalDirection}={hora} - Señal {Close[signalBarIndex]:0.00} Entrada={entryPrice:0.00} - TP={tpPrice:0.00} - SL={slPrice:0.00} - Resultado={resultado}";
                    Print(linea);
                    tradeEvaluated = true;
                    signalBarIndex = -1;
                }
            }
        }
    }
}
