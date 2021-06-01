using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace cAlgo
{
    class Utils
    {
        public struct PeakTroughData
        {
            public int Index;
            public double Value;
            public Int64 Time;

            public PeakTroughData(int index, double value, Int64 time)
            {
                Index = index;
                Value = value;
                Time = time;
            }
        }

        public int ToUnixTimestamp(DateTime d)
        {
            var epoch = d - new DateTime(1970, 1, 1, 0, 0, 0);
            return (int)epoch.TotalSeconds;
        }

        public double distanceFromPointToLine(double x, double y, double slope, double b)
        {
            double distance = Math.Abs(slope * x - y + b) / Math.Sqrt(Math.Pow(slope, 2) + Math.Pow(-1, 2));
            return distance;
        }

        public void LinearRegression(double[] xVals, double[] yVals, out double rSquared, out double yIntercept, out double slope)
        {
            if (xVals.Length != yVals.Length)
            {
                throw new Exception("Input values should be with the same length.");
            }

            double sumOfX = 0;
            double sumOfY = 0;
            double sumOfXSq = 0;
            double sumOfYSq = 0;
            double sumCodeviates = 0;

            for (var i = 0; i < xVals.Length; i++)
            {
                var x = xVals[i];
                var y = yVals[i];
                sumCodeviates += x * y;
                sumOfX += x;
                sumOfY += y;
                sumOfXSq += x * x;
                sumOfYSq += y * y;
            }

            var count = xVals.Length;
            var ssX = sumOfXSq - ((sumOfX * sumOfX) / count);
            var ssY = sumOfYSq - ((sumOfY * sumOfY) / count);

            var rNumerator = (count * sumCodeviates) - (sumOfX * sumOfY);
            var rDenom = (count * sumOfXSq - (sumOfX * sumOfX)) * (count * sumOfYSq - (sumOfY * sumOfY));
            var sCo = sumCodeviates - ((sumOfX * sumOfY) / count);

            var meanX = sumOfX / count;
            var meanY = sumOfY / count;
            var dblR = rNumerator / Math.Sqrt(rDenom);

            rSquared = dblR * dblR;
            yIntercept = meanY - ((sCo / ssX) * meanX);
            slope = sCo / ssX;
        }

        public int FindLineThreshold(double price)
        {
            if (Math.Floor(price / 1000) > 0)
                return 1;
            if (Math.Floor(price / 100) > 0)
                return 100;
            return 10000;
        }

        public object this[string propertyName]
        {
            get
            {
                PropertyInfo property = GetType().GetProperty(propertyName);
                return property.GetValue(this, null);
            }
            set
            {
                PropertyInfo property = GetType().GetProperty(propertyName);
                property.SetValue(this, value, null);
            }
        }


    }
}
