using SurfacePlot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SurfacePlot.ViewModels
{
    public class HomeViewModel
    {
        public Point AxisXStart { get; set; }
        public Point AxisXEnd { get; set; }
        public Point AxisYStart { get; set; }
        public Point AxisYEnd { get; set; }
        public Point AxisZStart { get; set; }
        public Point AxisZEnd { get; set; }
        public Point AxisXStartRotated { get; set; }
        public Point AxisXEndRotated { get; set; }
        public Point AxisYStartRotated { get; set; }
        public Point AxisYEndRotated { get; set; }
        public Point AxisZStartRotated { get; set; }
        public Point AxisZEndRotated { get; set; }
        public Point Origin { get; set; }
        public int PlotSizeX { get; set; }
        public int PlotSizeY { get; set; }
        public Point[,] Points { get; set; }
        public Point[,] PointsRotated { get; set; }
        public double AveragingCoeff { get; set; }
        public double CompressCoeff { get; set; }
        public int SmoothRounds { get; set; }
        public string Svg { get; set; }
        public double ViewingAngleX { get; set; }
        public double ViewingAngleY { get; set; }
        public double ViewingAngleZ { get; set; }
        public double XMax { get; set; }
        public double XMin { get; set; }
        public int Xn { get; set; }
        public double XStep { get; set; }
        public double YMax { get; set; }
        public double YMin { get; set; }
        public int Yn { get; set; }
        public double YStep { get; set; }
        public double ZDomain { get; set; }
        public double ZLowerBound { get; set; }
        public double ZMax { get; set; }
        public double ZMid { get; set; }
        public double ZMin { get; set; }
        public double ZRange { get; set; }
        public double ZUpperBound { get; set; }
    }
}