using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using BitQIndicator;
namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BitQTrendLine : Indicator
    {

        [Parameter("Period", DefaultValue = 6, Step = 1)]
        public int Period { get; set; }

        [Parameter("AllowBroken", DefaultValue = false)]
        public bool AllowBroken { get; set; }
        [Parameter("Threshold", DefaultValue = -10)]
        public double Threshold { get; set; }
        [Parameter("BrokenTrendThreshold", DefaultValue = -10)]
        public double BrokenTrendThreshold { get; set; }
        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }

        private BitQPeakTrough peakTroughtFinder;
        private Utils.Func helperFunc = new Utils.Func();
        private Utils.Base utils = new Utils.Base();
        private ArrayList peaksData;
        private ArrayList troughtData;
        private Tick tick;
        private AverageTrueRange atr;

        private int lastCalPeak = 0;
        private int lastCalTrough = 0;




        protected override void Initialize()
        {
            Print("TimeFrame: " + TimeFrame);
            Print("SymbolName:" + Symbol.Name);
            // Initialize and create nested indicators
            Chart.RemoveAllObjects();
            peakTroughtFinder = Indicators.GetIndicator<BitQPeakTrough>(true, 10, 10);
            peakTroughtFinder.reset();

            atr = Indicators.AverageTrueRange(6, MovingAverageType.Exponential);
        }


        public override void Calculate(int index)
        {
            // Calculate value at specified index
            //Result[index] = index;
            peakTroughtFinder.Calculate(index);
            peaksData = peakTroughtFinder.getPeakData();
            //Print("counter peakData: ", peaksData.Count);
            troughtData = peakTroughtFinder.getTroughData();
            // find trend line;
            if (peaksData.Count >= Period)
            {
                try
                {
                    findTrendLine(peaksData.GetRange(lastCalPeak, Period), true);
                    lastCalPeak = lastCalPeak + 1;

                } catch (Exception e)
                {
                    //Print("Exception", e);
                }
            }
            if (troughtData.Count >= Period)
            {
                try
                {
                    findTrendLine(troughtData.GetRange(lastCalTrough, Period), false);
                    lastCalTrough = lastCalTrough + 1;

                } catch (Exception)
                {

                }
            }
        }
        public void findTrendLine(ArrayList array, bool isPeak)
        {
            ArrayList paired = new ArrayList();


            for (int a = 0; a < array.Count - 2; a++)
            {
                for (int b = a + 1; b < array.Count - 1; b++)
                {
                    for (int c = b + 1; c < array.Count; c++)
                    {
                        ArrayList arr = new ArrayList();
                        arr.Add(array[a]);
                        arr.Add(array[b]);
                        arr.Add(array[c]);
                        paired.Add(arr);
                    }
                }
            }

            foreach (ArrayList _o in paired)
            {
                double[] xVals = new double[3];
                double[] yVals = new double[3];
                DateTime[] timeVals = new DateTime[3];
                int inx = 0;
                foreach (Utils.Base.Point _j in _o)
                {
                    try
                    {
                        xVals[inx] = _j.barIndex;
                        yVals[inx] = _j.yValue;
                        timeVals[inx] = _j.dateTime;
                        inx++;
                    } catch (Exception)
                    {

                    }

                }
                lr(xVals, yVals, timeVals, isPeak);
            }

        }

        public double findThresholdBaseOnATR(int startIndex, int endIndex)
        {
            double sumATR = 0;
            int max = Math.Max(startIndex, endIndex);
            int min = Math.Min(startIndex, endIndex);
            for(int i = min; i <= max; i++)
            {
                if(atr.Result[i] != null)
                {
                    sumATR += atr.Result[i];
                }
            }
            double averageATR = sumATR / (endIndex - startIndex);
            return averageATR;
        }

        public void lr(double[] xValues, double[] yValues, DateTime[] times, bool isPeak)
        {
            if (xValues[0] * xValues[1] * xValues[2] == 0) return;
            double rSquared, w, b;
            helperFunc.LinearRegression(xValues, yValues, out rSquared, out b, out w);

            //Print("Line: y = ", w, "x + ", b);
            //Print("R-squared = ", rSquared);
            int o = 0;
            double sum = 0;

            foreach (double e in xValues)
            {
                var dist = utils.distanceFromPointToLine(xValues[o], yValues[o], w, b);
                sum += dist;
                o++;
            }
            double threshold2, brokenthreshold;
            getThreshold(out threshold2, out brokenthreshold);
            threshold2 = findThresholdBaseOnATR((int) xValues[0],(int) xValues[2]);
            // base on XAU
            double threshold = Threshold > 0 ? Threshold : threshold2;
            // XAU has it's value > 1000
            if (sum - threshold <= 0)
            {
                if (!AllowBroken)
                {
                    if (isBrokenTrend(w, b, (int)xValues[0], (int)xValues[2], isPeak, BrokenTrendThreshold > 0 ? BrokenTrendThreshold : threshold2/ 2))
                    {
                        return;
                    }
                }
                var startY = w * xValues[0] + b;
                var endY = w * xValues[2] + b;

                Chart.DrawTrendLine("TrendLine_" + xValues[0].ToString() + "_" + xValues[2].ToString(), (int)xValues[0], startY, (int)xValues[2], endY, isPeak ? Color.DarkCyan : Color.Olive, 2);
                //ChartObjects.DrawLine("a" + xValues[0].ToString(), (int)xValues[0], yValues[0], (int)xValues[2], yValues[2], isPeak ? Colors.DarkCyan : Colors.Olive, 2);
            }
        }

        public void getThreshold(out double threshold, out double brokenThreshold)
        {
            threshold = 0.1;
            brokenThreshold = 0;
            if (Symbol.Name == "XAUUSD")
            {
                if (TimeFrame == TimeFrame.Minute15)
                {
                    threshold = 0.5;
                    brokenThreshold = 0.1;
                }
                else if (TimeFrame == TimeFrame.Minute30)
                {
                    threshold = 1;
                    brokenThreshold = 1;
                }
                else if (TimeFrame == TimeFrame.Hour)
                {
                    threshold = 1.2;
                    brokenThreshold = 1.2;
                }
                else if (TimeFrame == TimeFrame.Hour4)
                {
                    threshold = 1.2;
                    brokenThreshold = 1.2;
                }
                else if (TimeFrame == TimeFrame.Daily)
                {
                    threshold = 3;
                    brokenThreshold = 3;
                }
                else if (TimeFrame == TimeFrame.Monthly)
                {
                    threshold = 3;
                    brokenThreshold = 3;
                }
                else if (TimeFrame == TimeFrame.Weekly)
                {
                    threshold = 3;
                    brokenThreshold = 3;
                }
            }

        }

        public double getBrokenThreshold()
        {
            return 0;
        }

        public bool isBrokenTrend(double slope, double b, int range1, int range2, bool isPeak, double minRange)
        {

            bool isBroken = false;
            int brokenTrendCount = 0;
            bool isDownTrend;
            int counter = range1 + 1;
            var openPrice = Bars.OpenPrices[counter];
            var closePrice = Bars.ClosePrices[counter];
            var high = Math.Max(openPrice, closePrice);
            var low = Math.Min(openPrice, closePrice);
            var yCal = slope * counter + b;
            var yReal = isPeak ? high : low;

            for (int i = counter + 1; i < range2; i++)
            {
                openPrice = Bars.OpenPrices[i];
                closePrice = Bars.ClosePrices[i];
                high = Math.Max(openPrice, closePrice);
                low = Math.Min(openPrice, closePrice);
                yCal = slope * i + b;
                yReal = isPeak ? high - minRange : low + minRange;
                if (yCal - yReal < 0 && isPeak)
                {
                    brokenTrendCount++;
                    if (brokenTrendCount >= 1)
                    {
                        isBroken = true;
                        break;
                    }
                }
                if (yCal - yReal > 0 && !isPeak)
                {
                    brokenTrendCount++;
                    if (brokenTrendCount >= 0)
                    {
                        isBroken = true;
                        break;
                    }
                }
            }
            return isBroken;
        }
    }
}
