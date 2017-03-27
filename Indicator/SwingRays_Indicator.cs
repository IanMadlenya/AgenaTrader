using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Diagnostics;
using AgenaTrader.API;
using AgenaTrader.Custom;
using AgenaTrader.Plugins;
using AgenaTrader.Helper;


/// <summary>
/// Version: in progress
/// -------------------------------------------------------------------------
/// Simon Pucher 2016
/// -------------------------------------------------------------------------
/// The indicator was taken from: http://ninjatrader.com/support/forum/showthread.php?t=37759
/// Code was generated by AgenaTrader conversion tool and modified by Simon Pucher.
/// -------------------------------------------------------------------------
/// ****** Important ******
/// To compile this script without any error you also need access to the utility indicator to use these global source code elements.
/// You will find this indicator on GitHub: https://raw.githubusercontent.com/simonpucher/AgenaTrader/master/Utilities/GlobalUtilities_Utility.cs
/// -------------------------------------------------------------------------
/// Namespace holds all indicators and is required. Do not change it.
/// </summary>
namespace AgenaTrader.UserCode
{
    /// <summary>
    /// Plots horizontal rays at swing highs and lows and removes them once broken. 
    /// </summary>
    [Description("Plots horizontal rays at swing highs and lows and removes them once broken. Returns 1 if a high has broken. Returns -1 if a low has broken. Returns 0 in all other cases.")]
    public class SwingRays : UserIndicator
    {
        /// <summary>
        /// Object is storing ray data
        /// </summary>
        private class RayObject
        {
            public RayObject(string tag, int anchor1BarsAgo, double anchor1Y, int anchor2BarsAgo, double anchor2Y) {
                this.Tag = tag;
                this.BarsAgo1 = anchor1BarsAgo;
                this.BarsAgo2 = anchor2BarsAgo;
                this.Y1 = anchor1Y;
                this.Y2 = anchor2Y;
            }

            public string Tag { get; set; }
            public int BarsAgo1 { get; set; }
            public int BarsAgo2 { get; set; }
            //Pen Pen { get; set; }
            //DateTime X1 { get; set; }
            //DateTime X2 { get; set; }
            public double Y1 { get; set; }
            public double Y2 { get; set; }
        }

        // Wizard generated variables
        private int strength = 5; // number of bars required to left and right of the pivot high/low
                                  // User defined variables (add any user defined variables below)
        private Color swingHighColor = Color.DarkCyan;
        private Color swingLowColor = Color.Magenta;

        private ArrayList lastHighCache;
        private ArrayList lastLowCache;
        private double lastSwingHighValue = double.MaxValue; // used when testing for price breaks
        private double lastSwingLowValue = double.MinValue;
        private Stack<RayObject> swingHighRays;    //	last entry contains nearest swing high; removed when swing is broken
        private Stack<RayObject> swingLowRays; // track swing lows in the same manner
        private bool enableAlerts = false;
        private bool keepBrokenLines = true;

        //input
        private Color _signal = Color.Orange;
        private int _plot0Width = Const.DefaultLineWidth;
        private DashStyle _dash0Style = Const.DefaultIndicatorDashStyle;

        private Soundfile _soundfile = Soundfile.Blip;


        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void OnInit()
        {
            IsShowInDataBox = false;
            CalculateOnClosedBar = true;
            IsOverlay = false;
            PriceTypeSupported = false;

            lastHighCache = new ArrayList(); // used to identify swing points; from default Swing indicator
            lastLowCache = new ArrayList();
            swingHighRays = new Stack<RayObject>(); // LIFO buffer; last entry contains the nearest swing high
            swingLowRays = new Stack<RayObject>();

            Add(new Plot(new Pen(this.Signal, this.Plot0Width), PlotStyle.Line, "Signalline"));
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnCalculate()
        {
            int temp_signal_value = 0;

            // build up cache of recent High and Low values
            // code devised from default Swing Indicator by marqui@BMT, 10-NOV-2010 
            lastHighCache.Add(High[0]);
            if (lastHighCache.Count > (2 * strength) + 1)
                lastHighCache.RemoveAt(0); // if cache is filled, drop the oldest value
            lastLowCache.Add(Low[0]);
            if (lastLowCache.Count > (2 * strength) + 1)
                lastLowCache.RemoveAt(0);
            //
            if (lastHighCache.Count == (2 * strength) + 1) // wait for cache of Highs to be filled
            {
                // test for swing high 
                bool isSwingHigh = true;
                double swingHighCandidateValue = (double)lastHighCache[strength];
                for (int i = 0; i < strength; i++)
                    if ((double)lastHighCache[i] >= swingHighCandidateValue - double.Epsilon)
                        isSwingHigh = false; // bar(s) to right of candidate were higher
                for (int i = strength + 1; i < lastHighCache.Count; i++)
                    if ((double)lastHighCache[i] > swingHighCandidateValue - double.Epsilon)
                        isSwingHigh = false; // bar(s) to left of candidate were higher
                                             // end of test

                if (isSwingHigh)
                    lastSwingHighValue = swingHighCandidateValue;
                
                if (isSwingHigh) // if we have a new swing high then we draw a ray line on the chart
                {
                    AddChartRay("highRay" + (ProcessingBarIndex - strength), false, strength, lastSwingHighValue, 0, lastSwingHighValue, swingHighColor, DashStyle.Dot, 2);
                    RayObject newRayObject = new RayObject("highRay" + (ProcessingBarIndex - strength), strength, lastSwingHighValue, 0, lastSwingHighValue);
                    swingHighRays.Push(newRayObject); // store a reference so we can remove it from the chart later
                }
                else if (High[0] > lastSwingHighValue) // otherwise, we test to see if price has broken through prior swing high
                {
                    if (swingHighRays.Count > 0) // just to be safe 
                    {
                        //IRay currentRay = (IRay)swingHighRays.Pop(); // pull current ray from stack 
                        RayObject currentRay = (RayObject)swingHighRays.Pop(); // pull current ray from stack 
                        if (currentRay != null)
                        {
                            if (enableAlerts)
                            {
                                ShowAlert("Swing High at " + currentRay.Y1 + " broken", GlobalUtilities.GetSoundfile(this.Soundfile));
                            }
                            temp_signal_value = 1;
                            if (keepBrokenLines) // draw a line between swing point and break bar 
                            {
                                int barsAgo = currentRay.BarsAgo1;
                                ITrendLine newLine = AddChartLine("highLine" + (ProcessingBarIndex - barsAgo), false, barsAgo, currentRay.Y1, 0, currentRay.Y1, swingHighColor, DashStyle.Solid, 2);
                            }
                            RemoveChartDrawing(currentRay.Tag);
                            if (swingHighRays.Count > 0)
                            {
                                //IRay priorRay = (IRay)swingHighRays.Peek();
                                RayObject priorRay = (RayObject)swingHighRays.Peek();
                                lastSwingHighValue = priorRay.Y1; // needed when testing the break of the next swing high
                            }
                            else
                            {
                                lastSwingHighValue = double.MaxValue; // there are no higher swings on the chart; reset to default
                            }
                        }	
                    }
                }
            }

            if (lastLowCache.Count == (2 * strength) + 1) // repeat the above for the swing lows
            {
                // test for swing low 
                bool isSwingLow = true;
                double swingLowCandidateValue = (double)lastLowCache[strength];
                for (int i = 0; i < strength; i++)
                    if ((double)lastLowCache[i] <= swingLowCandidateValue + double.Epsilon)
                        isSwingLow = false; // bar(s) to right of candidate were lower

                for (int i = strength + 1; i < lastLowCache.Count; i++)
                    if ((double)lastLowCache[i] < swingLowCandidateValue + double.Epsilon)
                        isSwingLow = false; // bar(s) to left of candidate were lower
                                            // end of test for low

                if (isSwingLow)
                    lastSwingLowValue = swingLowCandidateValue;

                if (isSwingLow) // found a new swing low; draw it on the chart
                {
                    AddChartRay("lowRay" + (ProcessingBarIndex - strength), false, strength, lastSwingLowValue, 0, lastSwingLowValue, swingLowColor, DashStyle.Dot, 2);
                    RayObject newRayObject = new RayObject("lowRay" + (ProcessingBarIndex - strength), strength, lastSwingLowValue, 0, lastSwingLowValue);
                    swingLowRays.Push(newRayObject);
                }
                else if (Low[0] < lastSwingLowValue) // otherwise test to see if price has broken through prior swing low
                {
                    if (swingLowRays.Count > 0)
                    {
                        //IRay currentRay = (IRay)swingLowRays.Pop();
                        RayObject currentRay = (RayObject)swingLowRays.Pop();
                        if (currentRay != null)
                        {
                            if (enableAlerts)
                            {
                                ShowAlert("Swing Low at " + currentRay.Y1 + " broken", GlobalUtilities.GetSoundfile(this.Soundfile));
                            }
                            temp_signal_value = -1;
                            if (keepBrokenLines) // draw a line between swing point and break bar 
                            {
                                int barsAgo = currentRay.BarsAgo1;
                                ITrendLine newLine = AddChartLine("highLine" + (ProcessingBarIndex - barsAgo), false, barsAgo, currentRay.Y1, 0, currentRay.Y1, swingLowColor, DashStyle.Solid, 2);
                            }
                            RemoveChartDrawing(currentRay.Tag);

                            if (swingLowRays.Count > 0)
                            {
                                //IRay priorRay = (IRay)swingLowRays.Peek();
                                RayObject priorRay = (RayObject)swingLowRays.Peek();
                                lastSwingLowValue = priorRay.Y1; // price level of the prior swing low 
                            }
                            else
                            {
                                lastSwingLowValue = double.MinValue; // no swing lows present; set this to default value 
                            }
                        }
                    }
                }
            }


            SignalLine.Set(temp_signal_value);
        }


        public override string ToString()
        {
            return "SwingRays (I)";
        }

        public override string DisplayName
        {
            get
            {
                return "SwingRays (I)";
            }
        }


        #region InSeries Parameters

        [Description("Number of bars before/after each pivot bar")]
            [Category("Parameters")]
            [DisplayName("Strength")]
            public int Strength
            {
                get { return strength; }
                set { strength = Math.Max(2, value); }
            }

            [Description("Alert when swings are broken")]
            [Category("Parameters")]
            [DisplayName("Enable alerts")]
            public bool EnableAlerts
            {
                get { return enableAlerts; }
                set { enableAlerts = value; }
            }

            [Description("Show broken swing points")]
            [Category("Parameters")]
            [DisplayName("Keep broken lines")]
            public bool KeepBrokenLines
            {
                get { return keepBrokenLines; }
                set { keepBrokenLines = value; }
            }

            [XmlIgnore()]
            [Description("Color for swing highs")]
            [Category("Parameters")]
            [DisplayName("Swing high color")]
            public Color SwingHighColor
            {
                get { return swingHighColor; }
                set { swingHighColor = value; }
            }

            // Serialize our Color object
            [Browsable(false)]
            public string SwingHighColorSerialize
            {
                get { return SerializableColor.ToString(swingHighColor); }
                set { swingHighColor = SerializableColor.FromString(value); }
            }

            [XmlIgnore()]
            [Description("Color for swing lows")]
            [Category("Parameters")]
            [DisplayName("Swing low color")]
            public Color SwingLowColor
            {
                get { return swingLowColor; }
                set { swingLowColor = value; }
            }

            // Serialize our Color object	
            [Browsable(false)]
            public string SwingLowColorSerialize
            {
                get { return SerializableColor.ToString(swingLowColor); }
                set { swingLowColor = SerializableColor.FromString(value); }
            }

            [XmlIgnore()]
            [Description("Select the soundfile for the alert.")]
            [Category("Parameters")]
            [DisplayName("Soundfile name")]
            public Soundfile Soundfile
            {
                get { return _soundfile; }
                set { _soundfile = value; }
            }

        #endregion

        #region InSeries Drawings

        [XmlIgnore()]
        [Description("Select Color")]
        [Category("Drawings")]
        [DisplayName("Signalline")]
        public Color Signal
        {
            get { return _signal; }
            set { _signal = value; }
        }

        [Browsable(false)]
        public string SignalSerialize
        {
            get { return SerializableColor.ToString(_signal); }
            set { _signal = SerializableColor.FromString(value); }
        }

        /// <summary>
        /// </summary>
        [Description("Width for Priceline.")]
        [Category("Drawings")]
        [DisplayName("Line Width Priceline")]
        public int Plot0Width
        {
            get { return _plot0Width; }
            set { _plot0Width = Math.Max(1, value); }
        }


        /// <summary>
        /// </summary>
        [Description("DashStyle for Priceline.")]
        [Category("Drawings")]
        [DisplayName("Dash Style Priceline")]
        public DashStyle Dash0Style
        {
            get { return _dash0Style; }
            set { _dash0Style = value; }
        }

        #endregion

        #region Output properties

        [Browsable(false)]
        [XmlIgnore()]
        public DataSeries SignalLine
        {
            get { return Outputs[0]; }
        }

        //[Browsable(false)]  // this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        //    [XmlIgnore()]   // this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        //    public DataSeries HighRay
        //    {
        //        get { return Outputs[0]; }
        //    }

        //    [Browsable(false)]  // this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        //    [XmlIgnore()]   // this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        //    public DataSeries LowRay
        //    {
        //        get { return Outputs[1]; }
        //    }

        //    [Browsable(false)]  // this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        //    [XmlIgnore()]   // this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        //    public DataSeries HighLine
        //    {
        //        get { return Outputs[2]; }
        //    }

        //    [Browsable(false)]  // this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        //    [XmlIgnore()]   // this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        //    public DataSeries LowLine
        //    {
        //        get { return Outputs[3]; }
        //    }

        #endregion

    }
}




#region AgenaTrader Automaticaly Generated Code. Do not change it manualy

namespace AgenaTrader.UserCode
{
	#region Indicator

	public partial class UserIndicator
	{
		/// <summary>
		/// Plots horizontal rays at swing highs and lows and removes them once broken. Returns 1 if a high has broken. Returns -1 if a low has broken. Returns 0 in all other cases.
		/// </summary>
		public SwingRays SwingRays(System.Int32 strength, System.Boolean enableAlerts, System.Boolean keepBrokenLines, Color swingHighColor, Color swingLowColor, Soundfile soundfile)
        {
			return SwingRays(InSeries, strength, enableAlerts, keepBrokenLines, swingHighColor, swingLowColor, soundfile);
		}

		/// <summary>
		/// Plots horizontal rays at swing highs and lows and removes them once broken. Returns 1 if a high has broken. Returns -1 if a low has broken. Returns 0 in all other cases.
		/// </summary>
		public SwingRays SwingRays(IDataSeries input, System.Int32 strength, System.Boolean enableAlerts, System.Boolean keepBrokenLines, Color swingHighColor, Color swingLowColor, Soundfile soundfile)
		{
			var indicator = CachedCalculationUnits.GetCachedIndicator<SwingRays>(input, i => i.Strength == strength && i.EnableAlerts == enableAlerts && i.KeepBrokenLines == keepBrokenLines && i.SwingHighColor == swingHighColor && i.SwingLowColor == swingLowColor && i.Soundfile == soundfile);

			if (indicator != null)
				return indicator;

			indicator = new SwingRays
						{
							RequiredBarsCount = RequiredBarsCount,
							CalculateOnClosedBar = CalculateOnClosedBar,
							InSeries = input,
							Strength = strength,
							EnableAlerts = enableAlerts,
							KeepBrokenLines = keepBrokenLines,
							SwingHighColor = swingHighColor,
							SwingLowColor = swingLowColor,
							Soundfile = soundfile
						};
			indicator.SetUp();

			CachedCalculationUnits.AddIndicator2Cache(indicator);

			return indicator;
		}
	}

	#endregion

	#region Strategy

	public partial class UserStrategy
	{
		/// <summary>
		/// Plots horizontal rays at swing highs and lows and removes them once broken. Returns 1 if a high has broken. Returns -1 if a low has broken. Returns 0 in all other cases.
		/// </summary>
		public SwingRays SwingRays(System.Int32 strength, System.Boolean enableAlerts, System.Boolean keepBrokenLines, Color swingHighColor, Color swingLowColor, Soundfile soundfile)
		{
			return LeadIndicator.SwingRays(InSeries, strength, enableAlerts, keepBrokenLines, swingHighColor, swingLowColor, soundfile);
		}

		/// <summary>
		/// Plots horizontal rays at swing highs and lows and removes them once broken. Returns 1 if a high has broken. Returns -1 if a low has broken. Returns 0 in all other cases.
		/// </summary>
		public SwingRays SwingRays(IDataSeries input, System.Int32 strength, System.Boolean enableAlerts, System.Boolean keepBrokenLines, Color swingHighColor, Color swingLowColor, Soundfile soundfile)
		{
			if (IsInInit && input == null)
				throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

			return LeadIndicator.SwingRays(input, strength, enableAlerts, keepBrokenLines, swingHighColor, swingLowColor, soundfile);
		}
	}

	#endregion

	#region Column

	public partial class UserColumn
	{
		/// <summary>
		/// Plots horizontal rays at swing highs and lows and removes them once broken. Returns 1 if a high has broken. Returns -1 if a low has broken. Returns 0 in all other cases.
		/// </summary>
		public SwingRays SwingRays(System.Int32 strength, System.Boolean enableAlerts, System.Boolean keepBrokenLines, Color swingHighColor, Color swingLowColor, Soundfile soundfile)
		{
			return LeadIndicator.SwingRays(InSeries, strength, enableAlerts, keepBrokenLines, swingHighColor, swingLowColor, soundfile);
		}

		/// <summary>
		/// Plots horizontal rays at swing highs and lows and removes them once broken. Returns 1 if a high has broken. Returns -1 if a low has broken. Returns 0 in all other cases.
		/// </summary>
		public SwingRays SwingRays(IDataSeries input, System.Int32 strength, System.Boolean enableAlerts, System.Boolean keepBrokenLines, Color swingHighColor, Color swingLowColor, Soundfile soundfile)
		{
			return LeadIndicator.SwingRays(input, strength, enableAlerts, keepBrokenLines, swingHighColor, swingLowColor, soundfile);
		}
	}

	#endregion

	#region Scripted Condition

	public partial class UserScriptedCondition
	{
		/// <summary>
		/// Plots horizontal rays at swing highs and lows and removes them once broken. Returns 1 if a high has broken. Returns -1 if a low has broken. Returns 0 in all other cases.
		/// </summary>
		public SwingRays SwingRays(System.Int32 strength, System.Boolean enableAlerts, System.Boolean keepBrokenLines, Color swingHighColor, Color swingLowColor, Soundfile soundfile)
		{
			return LeadIndicator.SwingRays(InSeries, strength, enableAlerts, keepBrokenLines, swingHighColor, swingLowColor, soundfile);
		}

		/// <summary>
		/// Plots horizontal rays at swing highs and lows and removes them once broken. Returns 1 if a high has broken. Returns -1 if a low has broken. Returns 0 in all other cases.
		/// </summary>
		public SwingRays SwingRays(IDataSeries input, System.Int32 strength, System.Boolean enableAlerts, System.Boolean keepBrokenLines, Color swingHighColor, Color swingLowColor, Soundfile soundfile)
		{
			return LeadIndicator.SwingRays(input, strength, enableAlerts, keepBrokenLines, swingHighColor, swingLowColor, soundfile);
		}
	}

	#endregion

}

#endregion


