using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;
using AgenaTrader.API;
using AgenaTrader.Custom;
using AgenaTrader.Plugins;
using AgenaTrader.Helper;

/// <summary>
/// Version: 1.0
/// -------------------------------------------------------------------------
/// Simon Pucher 2016
/// -------------------------------------------------------------------------
/// The indicator was taken from: http://www.greattradingsystems.com/QQE-ninjatraderindicator
/// Code was generated by AgenaTrader conversion tool and modified by Simon Pucher.
/// -------------------------------------------------------------------------
/// Namespace holds all indicators and is required. Do not change it.
/// </summary>
namespace AgenaTrader.UserCode
{
    /// <summary>
    /// The anaMACDBBLines (Moving Average Convergence/Divergence) is a trend following momentum indicator that shows the relationship between two moving averages of prices.
    /// Optimized execution by predefining instances of external indicators (Zondor August 10 2010)
    /// </summary>
	[Description("The TDI (Moving Average Convergence/Divergence) is a trend following momentum indicator that shows the relationship between two moving averages of prices.")]
	public class TDI_Indicator : UserIndicator
	{

        //input
        private int _rsiPeriod = 13;
		private int _pricePeriod = 2;
		private int	_signalPeriod = 7;
		private int	_bandPeriod = 34;
		private double _stdDevNumber = 1.62;

		private Color _main = Color.Lime;
		private Color _signal = Color.Red;
        private Color _bbAverage = Color.Gold;
		private Color _bbUpper = Color.CornflowerBlue;
		private Color _bbLower = Color.CornflowerBlue;
		private Color _midPositive = Color.Olive;
		private Color _midNegative = Color.RosyBrown;

		private int _plot0Width = 2;
        private DashStyle _dash0Style = DashStyle.Solid;
        private int _plot1Width = 2;
        private DashStyle _dash1Style = DashStyle.Solid;
        private int _plot2Width = 2;
        private DashStyle _dash2Style = DashStyle.Solid;
        private int _plot3Width = 1;
        private DashStyle _dash3Style = DashStyle.Solid;

        //output

        //internal
        private DataSeries _RSI_List;


    
	

		/// <summary>
		/// This method is used to configure the indicator and is called once before any bar data is loaded.
		/// </summary>
		protected override void Initialize()
		{
            Add(new Plot(new Pen(this.Main, this.Plot0Width), PlotStyle.Line, "PriceLine"));
            Add(new Plot(new Pen(this.Signal, this.Plot1Width), PlotStyle.Line, "Signalline"));
            Add(new Plot(new Pen(this.BBAverage, this.Plot2Width), PlotStyle.Line, "Average"));
            Add(new Plot(new Pen(this.BBUpper, this.Plot3Width), PlotStyle.Line, "Upper"));
            Add(new Plot(new Pen(this.BBUpper, this.Plot3Width), PlotStyle.Line, "Lower"));
            Add(new Plot(new Pen(Color.Gray, this.Plot3Width), PlotStyle.Line, "MidLine"));

            CalculateOnBarClose = true;
		}


		/// <summary>
		/// Calculates the indicator value(s) at the current index.
		/// </summary>
		protected override void OnStartUp()
        {
            this._RSI_List = new DataSeries(this);
		}
		
		protected override void OnBarUpdate()
		{
            double RSI_value = RSI(this.RSIPeriod, 1)[0];
            this._RSI_List.Set(RSI_value);

            double PRICE_value = SMA(this._RSI_List, this.PricePeriod)[0];
            PriceLine.Set(PRICE_value);

            double SIGNAL_value = SMA(this._RSI_List, this.SignalPeriod)[0];
            SignalLine.Set(SIGNAL_value);

            double AVG_value = SMA(this._RSI_List, this.BandPeriod)[0];
            Average.Set(AVG_value);
            MidLine.Set(50);

            double stdDevValue = StdDev(this._RSI_List, this.BandPeriod)[0];

            Upper.Set(AVG_value + this.StdDevNumber * stdDevValue);
            Lower.Set(AVG_value - this.StdDevNumber * stdDevValue);

            PlotColors[0][0] = this.Main;
            PlotColors[1][0] = this.Signal;
            PlotColors[2][0] = this.BBAverage;
            PlotColors[3][0] = this.BBUpper;
            PlotColors[4][0] = this.BBLower;
            
            if (AVG_value > 50)
                PlotColors[5][0] = this.MidPositive;
            else
                PlotColors[5][0] = this.MidNegative;

            Plots[0].PenStyle = this.Dash0Style;
            Plots[0].Pen.Width = this.Plot0Width;
            Plots[1].PenStyle = this.Dash1Style;
            Plots[1].Pen.Width = this.Plot1Width;
            Plots[2].PenStyle = this.Dash2Style;
            Plots[2].Pen.Width = this.Plot2Width;

            Plots[3].PenStyle = this.Dash3Style;
            Plots[3].Pen.Width = this.Plot3Width;
            Plots[4].PenStyle = this.Dash3Style;
            Plots[4].Pen.Width = this.Plot3Width;
            Plots[5].PenStyle = this.Dash3Style;
            Plots[5].Pen.Width = this.Plot3Width;
 
		}

        public override string ToString()
        {
            return "TDI";
        }

        public override string DisplayName
        {
            get
            {
                return "TDI";
            }
        }

        #region Properties


        #region Input




        #region Colors

        [XmlIgnore()]
        [Description("Select Color")]
        [Category("Colors")]
        [DisplayName("Pricline")]
        public Color Main
        {
            get { return _main; }
            set { _main = value; }
        }

        [Browsable(false)]
        public string MainSerialize
        {
            get { return SerializableColor.ToString(_main); }
            set { _main = SerializableColor.FromString(value); }
        }


        [XmlIgnore()]
        [Description("Select Color")]
        [Category("Colors")]
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


        [XmlIgnore()]
        [Description("Select Color")]
        [Category("Colors")]
        [DisplayName("Bollinger Average")]
        public Color BBAverage
        {
            get { return _bbAverage; }
            set { _bbAverage = value; }
        }

        [Browsable(false)]
        public string BBAverageSerialize
        {
            get { return SerializableColor.ToString(_bbAverage); }
            set { _bbAverage = SerializableColor.FromString(value); }
        }


        [XmlIgnore()]
        [Description("Select Color")]
        [Category("Colors")]
        [DisplayName("Bollinger Upper Band")]
        public Color BBUpper
        {
            get { return _bbUpper; }
            set { _bbUpper = value; }
        }

        [Browsable(false)]
        public string BBUpperSerialize
        {
            get { return SerializableColor.ToString(_bbUpper); }
            set { _bbUpper = SerializableColor.FromString(value); }
        }


        [XmlIgnore()]
        [Description("Select Color")]
        [Category("Colors")]
        [DisplayName("Bollinger Lower Band")]
        public Color BBLower
        {
            get { return _bbLower; }
            set { _bbLower = value; }
        }

        [Browsable(false)]
        public string BBLowerSerialize
        {
            get { return SerializableColor.ToString(_bbLower); }
            set { _bbLower = SerializableColor.FromString(value); }
        }


        [XmlIgnore()]
        [Description("Select Color")]
        [Category("Colors")]
        [DisplayName("Midline Positive")]
        public Color MidPositive
        {
            get { return _midPositive; }
            set { _midPositive = value; }
        }

        [Browsable(false)]
        public string MidPositiveSerialize
        {
            get { return SerializableColor.ToString(_midPositive); }
            set { _midPositive = SerializableColor.FromString(value); }
        }


        [XmlIgnore()]
        [Description("Select Color")]
        [Category("Colors")]
        [DisplayName("Midline Negative")]
        public Color MidNegative
        {
            get { return _midNegative; }
            set { _midNegative = value; }
        }

        [Browsable(false)]
        public string MidNegativeSerialize
        {
            get { return SerializableColor.ToString(_midNegative); }
            set { _midNegative = SerializableColor.FromString(value); }
        }

        #endregion


        [Description("Period for RSI")]
        [Category("Values")]
        [DisplayName("Period for RSI")]
        public int RSIPeriod
        {
            get { return _rsiPeriod; }
            set { _rsiPeriod = Math.Max(1, value); }
        }



        [Description("Period for Priceline")]
        [Category("Values")]
        [DisplayName("Period for Priceline")]
        public int PricePeriod
        {
            get { return _pricePeriod; }
            set { _pricePeriod = Math.Max(1, value); }
        }

        /// <summary>
        /// </summary>
        [Description("Period for Signalline")]
        [Category("Values")]
        [DisplayName("Period for Signalline")]
        public int SignalPeriod
        {
            get { return _signalPeriod; }
            set { _signalPeriod = Math.Max(1, value); }
        }

        /// <summary>
        /// </summary>
        [Description("Band Period for Bollinger Band")]
        [Category("Values")]
        [DisplayName("Period for VolaBands")]
        public int BandPeriod
        {
            get { return _bandPeriod; }
            set { _bandPeriod = Math.Max(1, value); }
        }

        /// <summary>
        /// </summary>
        [Description("Number of standard deviations")]
        [Category("Values")]
        [DisplayName("# of Std. Dev.")]
        public double StdDevNumber
        {
            get { return _stdDevNumber; }
            set { _stdDevNumber = Math.Max(0, value); }
        }


        /// <summary>
        /// </summary>
        [Description("Width for Priceline.")]
        [Category("Plots")]
        [DisplayName("Line Width Priceline")]
        public int Plot0Width
        {
            get { return _plot0Width; }
            set { _plot0Width = Math.Max(1, value); }
        }


        /// <summary>
        /// </summary>
        [Description("DashStyle for Priceline.")]
        [Category("Plots")]
        [DisplayName("Dash Style Priceline")]
        public DashStyle Dash0Style
        {
            get { return _dash0Style; }
            set { _dash0Style = value; }
        }

        /// <summary>
        /// </summary>
        [Description("Width for Signalline.")]
        [Category("Plots")]
        [DisplayName("Line Width Signal")]
        public int Plot1Width
        {
            get { return _plot1Width; }
            set { _plot1Width = Math.Max(1, value); }
        }

        /// <summary>
        /// </summary>
        [Description("DashStyle for Signalline.")]
        [Category("Plots")]
        [DisplayName("Dash Style Signal")]
        public DashStyle Dash1Style
        {
            get { return _dash1Style; }
            set { _dash1Style = value; }
        }

        /// <summary>
        /// </summary>
        [Description("Width for Midband.")]
        [Category("Plots")]
        [DisplayName("Line Width Midband")]
        public int Plot2Width
        {
            get { return _plot2Width; }
            set { _plot2Width = Math.Max(1, value); }
        }


        /// <summary>
        /// </summary>
        [Description("DashStyle for Bollinger Bands.")]
        [Category("Plots")]
        [DisplayName("Dash Style BBands")]
        public DashStyle Dash2Style
        {
            get { return _dash2Style; }
            set { _dash2Style = value; }
        }

        /// <summary>
        /// </summary>
        [Description("Width for Bollinger Bands.")]
        [Category("Plots")]
        [DisplayName("Line Width BBAnds")]
        public int Plot3Width
        {
            get { return _plot3Width; }
            set { _plot3Width = Math.Max(1, value); }
        }


        /// <summary>
        /// </summary>
        [Description("DashStyle for Trigger Average Line.")]
        [Category("Plots")]
        [DisplayName("Dash Style Average")]
        public DashStyle Dash3Style
        {
            get { return _dash3Style; }
            set { _dash3Style = value; }
        }

        #endregion



        #region Output

        [Browsable(false)]
        [XmlIgnore()]
        public DataSeries PriceLine
        {
            get { return Values[0]; }
        }


        [Browsable(false)]
        [XmlIgnore()]
        public DataSeries SignalLine
        {
            get { return Values[1]; }
        }


        [Browsable(false)]
        [XmlIgnore()]
        public DataSeries Average
        {
            get { return Values[2]; }
        }

        [Browsable(false)]
        [XmlIgnore()]
        public DataSeries Upper
        {
            get { return Values[3]; }
        }

        [Browsable(false)]
        [XmlIgnore()]
        public DataSeries Lower
        {
            get { return Values[4]; }
        }

        [Browsable(false)]
        [XmlIgnore()]
        public DataSeries MidLine
        {
            get { return Values[5]; }
        }
        #endregion



        #endregion




	
	}
}

#region AgenaTrader Automaticaly Generated Code. Do not change it manualy

namespace AgenaTrader.UserCode
{
	#region Indicator

	public partial class UserIndicator : Indicator
	{
		/// <summary>
		/// The TDI (Moving Average Convergence/Divergence) is a trend following momentum indicator that shows the relationship between two moving averages of prices.
		/// </summary>
		public TDI_Indicator TDI_Indicator()
        {
			return TDI_Indicator(Input);
		}

		/// <summary>
		/// The TDI (Moving Average Convergence/Divergence) is a trend following momentum indicator that shows the relationship between two moving averages of prices.
		/// </summary>
		public TDI_Indicator TDI_Indicator(IDataSeries input)
		{
			var indicator = CachedCalculationUnits.GetCachedIndicator<TDI_Indicator>(input);

			if (indicator != null)
				return indicator;

			indicator = new TDI_Indicator
						{
							BarsRequired = BarsRequired,
							CalculateOnBarClose = CalculateOnBarClose,
							Input = input
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
		/// The TDI (Moving Average Convergence/Divergence) is a trend following momentum indicator that shows the relationship between two moving averages of prices.
		/// </summary>
		public TDI_Indicator TDI_Indicator()
		{
			return LeadIndicator.TDI_Indicator(Input);
		}

		/// <summary>
		/// The TDI (Moving Average Convergence/Divergence) is a trend following momentum indicator that shows the relationship between two moving averages of prices.
		/// </summary>
		public TDI_Indicator TDI_Indicator(IDataSeries input)
		{
			if (InInitialize && input == null)
				throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

			return LeadIndicator.TDI_Indicator(input);
		}
	}

	#endregion

	#region Column

	public partial class UserColumn
	{
		/// <summary>
		/// The TDI (Moving Average Convergence/Divergence) is a trend following momentum indicator that shows the relationship between two moving averages of prices.
		/// </summary>
		public TDI_Indicator TDI_Indicator()
		{
			return LeadIndicator.TDI_Indicator(Input);
		}

		/// <summary>
		/// The TDI (Moving Average Convergence/Divergence) is a trend following momentum indicator that shows the relationship between two moving averages of prices.
		/// </summary>
		public TDI_Indicator TDI_Indicator(IDataSeries input)
		{
			return LeadIndicator.TDI_Indicator(input);
		}
	}

	#endregion

	#region Scripted Condition

	public partial class UserScriptedCondition
	{
		/// <summary>
		/// The TDI (Moving Average Convergence/Divergence) is a trend following momentum indicator that shows the relationship between two moving averages of prices.
		/// </summary>
		public TDI_Indicator TDI_Indicator()
		{
			return LeadIndicator.TDI_Indicator(Input);
		}

		/// <summary>
		/// The TDI (Moving Average Convergence/Divergence) is a trend following momentum indicator that shows the relationship between two moving averages of prices.
		/// </summary>
		public TDI_Indicator TDI_Indicator(IDataSeries input)
		{
			return LeadIndicator.TDI_Indicator(input);
		}
	}

	#endregion

}

#endregion
