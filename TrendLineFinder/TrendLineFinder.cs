using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class TrendLineFinder : Indicator
    {

        [Parameter("Period", DefaultValue = 6, Step = 1)]
        public int Period { get; set; }

        [Parameter("AllowBroken", DefaultValue = false)]
        public bool AllowBroken { get; set; }

        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }

        private PeakTroughtFinder peakTroughtFinder;
        private Utils utils;
        private ArrayList peaksData;
        private ArrayList troughtData;
        private int lastCalPeak = 0;
        private int lastCalTrough = 0;



        protected override void Initialize()
        {
            // Initialize and create nested indicators
            peakTroughtFinder = Indicators.GetIndicator<PeakTroughtFinder>(true, 10, 10);
            utils = new Utils();
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
                int inx = 0;
                foreach (PeakTroughtFinder.PeakTroughData _j in _o)
                {
                    try
                    {
                        xVals[inx] = _j.Index;
                        yVals[inx] = _j.Value;
                        inx++;
                    }
                    catch (Exception)
                    {

                    }
                    
                }
                lr(xVals, yVals, isPeak);
            }

        }

        public void lr(double[] xValues, double[] yValues, bool isPeak)
        {
            double rSquared, w, b;
            utils.LinearRegression(xValues, yValues, out rSquared, out b, out w);

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
            double baseThreshold = 0.15;
            // base on XAU
            double threshold = baseThreshold / utils.FindLineThreshold(Bars.OpenPrices[0]);
            // XAU has it's value > 1000
            if (sum - threshold <= 0)
            {
                if (!AllowBroken)
                {
                    if (isBrokenTrend(w, b, (int)xValues[0], (int)xValues[2], isPeak, threshold * 10))
                    {
                        return;
                    }
                }

                //foreach (double e in xValues)
                //{
                //    int index = (int)xValues[y];
                //    Chart.DrawIcon("icon_" + xValues[0].ToString(), ChartIconType.Diamond, index, yValues[y], isPeak ? Color.DarkCyan : Color.Olive);
                //}

                ChartObjects.DrawLine("a" + xValues[0].ToString(), (int) xValues[0], yValues[0], (int) xValues[2], yValues[2], isPeak ? Colors.DarkCyan : Colors.Olive, 2);
            }
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
                    if (brokenTrendCount >= 1)
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
