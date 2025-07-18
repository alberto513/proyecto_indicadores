#region Using declarations
using System;
using System.Windows.Media;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Data;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class MagnoFluxScalperLongIndicator : Indicator
    {
        // ————— Parámetros de la lógica —————
        [NinjaScriptProperty]
        [Display(Name="Cooldown Bars", GroupName="Parameters", Order=1)]
        public int CooldownBars       { get; set; } = 1;

        [NinjaScriptProperty]
        [Display(Name="Min Conditions", GroupName="Parameters", Order=2)]
        public int MinConditions      { get; set; } = 3;

        [NinjaScriptProperty]
        [Display(Name="Volume Threshold", GroupName="Parameters", Order=3)]
        public double VolumeThreshold { get; set; } = 1.3;

        [NinjaScriptProperty]
        [Display(Name="Speed Ticks", GroupName="Parameters", Order=4)]
        public int SpeedTicks         { get; set; } = 18;

        [NinjaScriptProperty]
        [Display(Name="Body Context Ratio", GroupName="Parameters", Order=5)]
        public double BodyContextRatio{ get; set; } = 0.4;

        [NinjaScriptProperty]
        [Display(Name="Slope Min", GroupName="Parameters", Order=6)]
        public double SlopeMin        { get; set; } = 0.035;

        [NinjaScriptProperty]
        [Display(Name="Session Start", GroupName="Parameters", Order=7)]
        public string SessionStart    { get; set; } = "10:30";

        [NinjaScriptProperty]
        [Display(Name="Session End", GroupName="Parameters", Order=8)]
        public string SessionEnd      { get; set; } = "13:00";

        [NinjaScriptProperty]
        [Display(Name="Stop Loss Ticks", GroupName="Parameters", Order=9)]
        public int StopLossTicks      { get; set; } = 36;

        [NinjaScriptProperty]
        [Display(Name="Profit Target Ticks", GroupName="Parameters", Order=10)]
        public int ProfitTargetTicks  { get; set; } = 30;


        // ————— Pinceles configurables para flechas —————
        [NinjaScriptProperty]
        [Display(Name="Bullish Arrow Brush", GroupName="Appearance", Order=20)]
        public Brush BullishArrowBrush { get; set; }

        [NinjaScriptProperty]
        [Display(Name="Bearish Arrow Brush", GroupName="Appearance", Order=21)]
        public Brush BearishArrowBrush { get; set; }


        // Variables internas
        private int      lastSignalBar = -1000;
        private TimeSpan sessionStart, sessionEnd;


        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name             = "MagnoFluxScalperLongIndicator";
                Description      = "Indicador con pinceles configurables para flechas LONG/SHORT";
                Calculate        = Calculate.OnBarClose;
                IsOverlay        = true;
                DisplayInDataBox = false;

                // Valores por defecto de los brushes
                BullishArrowBrush = Brushes.WhiteSmoke;
                BearishArrowBrush = Brushes.Tomato;
            }
            else if (State == State.Configure)
            {
                sessionStart = TimeSpan.Parse(SessionStart);
                sessionEnd   = TimeSpan.Parse(SessionEnd);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 20 || CurrentBar <= lastSignalBar + CooldownBars)
                return;

            // Filtro horario
            var now = Time[0].TimeOfDay;
            if (now < sessionStart || now > sessionEnd)
                return;

            // Cálculo de condiciones
            double avgVol   = SMA(Volume, 10)[0];
            bool   volSpike = Volume[0] > avgVol * VolumeThreshold;
            double fastMove = Math.Abs(Close[0] - Close[2]) / TickSize;
            bool   speedOk  = fastMove >= SpeedTicks;
            double bodySize = Math.Abs(Close[0] - Open[0]);
            bool   contextOk= bodySize > (High[0] - Low[0]) * BodyContextRatio;

            int passCount = (volSpike ? 1 : 0) 
                          + (speedOk   ? 1 : 0)
                          + (contextOk ? 1 : 0);

            double slope = (EMA(14)[0] - EMA(14)[3]) / (3 * TickSize);
            bool   upTrend = slope > SlopeMin;

            // Señal LONG
            if (upTrend && passCount >= MinConditions && Close[0] > Open[0])
            {
                Draw.ArrowUp(
                    this,
                    "mfscalpLong_" + CurrentBar,
                    true,
                    0,
                    Low[0] - TickSize,
                    BullishArrowBrush
                );
                lastSignalBar = CurrentBar;
            }

            // (Si luego quieres agregar SHORT, usa BearishArrowBrush con Draw.ArrowDown)
        }
    }
}
