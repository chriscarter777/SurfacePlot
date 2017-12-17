using SurfacePlot.Models;
using SurfacePlot.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace SurfacePlot.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public ViewResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ViewResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ViewResult Index()
        {
            HomeViewModel model = new HomeViewModel
            {
                ViewingAngleX = -0.30,
                ViewingAngleY = 0.20,
                ViewingAngleZ = 0.90,
                PlotSizeX = 1800,
                PlotSizeY = 1000,
                SmoothRounds = 15,
                AveragingCoeff = 1,
                CompressCoeff = 0.25,
                XMin = 0,
                Xn = 60,
                XStep = 15,
                YMin = 0,
                Yn = 30,
                YStep = 15,
                ZLowerBound = 0,
                ZUpperBound = 500,
            };
            return View("Index", model);
        }

        [HttpPost]
        public ViewResult Draw(HomeViewModel model)
        {
            SetDomain(model);
            SetAxes(model);
            GenerateDataWithStatistics(model);

            //Perform Rotation
            model.AxisXStartRotated = RotatePoint(model.AxisXStart, model.ViewingAngleX, model.ViewingAngleY, model.ViewingAngleZ);
            model.AxisXEndRotated = RotatePoint(model.AxisXEnd, model.ViewingAngleX, model.ViewingAngleY, model.ViewingAngleZ);
            model.AxisYStartRotated = RotatePoint(model.AxisYStart, model.ViewingAngleX, model.ViewingAngleY, model.ViewingAngleZ);
            model.AxisYEndRotated = RotatePoint(model.AxisYEnd, model.ViewingAngleX, model.ViewingAngleY, model.ViewingAngleZ);
            model.AxisZStartRotated = RotatePoint(model.AxisZStart, model.ViewingAngleX, model.ViewingAngleY, model.ViewingAngleZ);
            model.AxisZEndRotated = RotatePoint(model.AxisZEnd, model.ViewingAngleX, model.ViewingAngleY, model.ViewingAngleZ);
            RotatePoints(model);

            //Display
            GenerateSvg(model);
            return View("Index", model);
        }  //Draw

        [HttpPost]
        public ViewResult Redraw(HomeViewModel model)
        {
            SetDomain(model);
            SetAxes(model);

            //Retrieve data
            model.Points = (Point[,])Session["Points"];
            model.ZMax = (double)Session["ZMax"];
            model.ZMid = (double)Session["ZMid"];
            model.ZMin = (double)Session["ZMin"];
            model.ZRange = (double)Session["ZRange"];

            //Perform rotation
            model.AxisXStartRotated = RotatePoint(model.AxisXStart, model.ViewingAngleX, model.ViewingAngleY, model.ViewingAngleZ);
            model.AxisXEndRotated = RotatePoint(model.AxisXEnd, model.ViewingAngleX, model.ViewingAngleY, model.ViewingAngleZ);
            model.AxisYStartRotated = RotatePoint(model.AxisYStart, model.ViewingAngleX, model.ViewingAngleY, model.ViewingAngleZ);
            model.AxisYEndRotated = RotatePoint(model.AxisYEnd, model.ViewingAngleX, model.ViewingAngleY, model.ViewingAngleZ);
            model.AxisZStartRotated = RotatePoint(model.AxisZStart, model.ViewingAngleX, model.ViewingAngleY, model.ViewingAngleZ);
            model.AxisZEndRotated = RotatePoint(model.AxisZEnd, model.ViewingAngleX, model.ViewingAngleY, model.ViewingAngleZ);
            RotatePoints(model);

            //Display
            GenerateSvg(model);
            return View("Index", model);
        }  //Redraw

        private void GenerateDataWithStatistics(HomeViewModel model)
        {
            model.Points = new Point[model.Xn, model.Yn];

            //create random dataset
            Random rnd = new Random();
            for(int i = 0; i < model.Xn; i++)
            {
                for (int j = 0; j < model.Yn; j++)
                {
                    double x = model.XMin + (i * model.XStep);
                    double y = model.YMin + (j * model.YStep);
                    double z = (rnd.NextDouble() * model.ZDomain) + model.ZLowerBound;
                    model.Points[i,j] = new Point
                    {
                        X = x,
                        Y = y,
                        Z = z
                    };
                }
            }

            //Get the statistics of the dataset just generated
            GetStatistics(model.Points, out double minValue, out double midValue, out double maxValue, out double valueSpan);
            model.ZMax = maxValue;
            model.ZMid = midValue;
            model.ZMin = minValue;
            model.ZRange = valueSpan;

            //run it through smoothing rounds;
            //the highest and lowest point are excluded, to avoid excessive flattening, so Zmax/ZMin/ZRange are unaffected
            for (int t = 0; t < model.SmoothRounds; t++)
            {
                for (int i = 0; i < model.Xn; i++)
                {
                    for (int j = 0; j < model.Yn; j++)
                    {
                        //if (model.Points[i, j].Z > model.ZMin && model.Points[i, j].Z < model.ZMax)
                        //{
                        //average adjacent points
                        double sigmaDifference = 0;
                        int denominator = 0;
                        if (i > 0)
                        {
                            sigmaDifference += model.Points[i-1, j].Z - model.Points[i, j].Z;
                            denominator++;
                        }
                        if (j > 0)
                        {
                            sigmaDifference += model.Points[i, j-1].Z - model.Points[i, j].Z;
                            denominator++;
                        }
                        if(i > 0 && j > 0)
                        {
                            sigmaDifference += model.Points[i-1, j - 1].Z - model.Points[i, j].Z;
                            denominator++;
                        }
                        if (i < (model.Xn - 1))
                        {
                            sigmaDifference += model.Points[i+1, j].Z - model.Points[i, j].Z;
                            denominator++;
                        }
                        if (j < (model.Yn - 1))
                        {
                            sigmaDifference += model.Points[i, j+1].Z - model.Points[i, j].Z;
                            denominator++;
                        }
                        if (i < (model.Xn - 1) && j < (model.Yn - 1))
                        {
                            sigmaDifference += model.Points[i+1, j+1].Z - model.Points[i, j].Z;
                            denominator++;
                        }
                        if(denominator > 0)
                        {
                            model.Points[i, j].Z = model.Points[i, j].Z + ((sigmaDifference * model.AveragingCoeff) / denominator);
                        }
                        //}
                    }  //j
                }  //i

                //compress to reverse the flattening which accompanies averaging
                for (int i = 0; i < model.Xn; i++)
                {
                    for (int j = 0; j < model.Yn; j++)
                    {
                        double positionRelativeToMax = (model.Points[i, j].Z - model.ZMin) / model.ZRange;
                        double distanceToMax = model.ZMax - model.Points[i, j].Z;
                        double positionRelativeToMin = (model.ZMax - model.Points[i, j].Z) / model.ZRange;
                        double distanceToMin = model.Points[i, j].Z - model.ZMin;
                        if (positionRelativeToMax > 0.5)
                        {
                            model.Points[i, j].Z += (Math.Pow(positionRelativeToMax, 4) * distanceToMax * model.CompressCoeff);
                        }
                        if (positionRelativeToMin > 0.5)
                        {
                            model.Points[i, j].Z -= (Math.Pow(positionRelativeToMin, 4) * distanceToMin * model.CompressCoeff);
                        }
                    }  //j
                }  //i
            }  //t

            //Get the statistics of the dataset just generated
            GetStatistics(model.Points, out minValue, out midValue, out maxValue, out valueSpan);
            model.ZMax = maxValue;
            model.ZMid = midValue;
            model.ZMin = minValue;
            model.ZRange = valueSpan;

            Session["Points"] = model.Points as Point[,];
            Session["ZMax"] = model.ZMax as double?;
            Session["ZMid"] = model.ZMid as double?;
            Session["ZMin"] = model.ZMin as double?;
            Session["ZRange"] = model.ZRange as double?;
        }  //GenerateData

        private void RotatePoints(HomeViewModel model)
        {
            double[,] xRotationalMatrix = new double[3, 3];
            xRotationalMatrix[0, 0] = 1;
            xRotationalMatrix[0, 1] = 0;
            xRotationalMatrix[0, 2] = 0;
            xRotationalMatrix[1, 0] = 0;
            xRotationalMatrix[1, 1] = Math.Cos(model.ViewingAngleX);
            xRotationalMatrix[1, 2] = Math.Sin(model.ViewingAngleX) * (-1);
            xRotationalMatrix[2, 0] = 0;
            xRotationalMatrix[2, 1] = Math.Sin(model.ViewingAngleX);
            xRotationalMatrix[2, 2] = Math.Cos(model.ViewingAngleX);
            double[,] yRotationalMatrix = new double[3, 3];
            yRotationalMatrix[0, 0] = Math.Cos(model.ViewingAngleY);
            yRotationalMatrix[0, 1] = 0;
            yRotationalMatrix[0, 2] = Math.Sin(model.ViewingAngleY);
            yRotationalMatrix[1, 0] = 0;
            yRotationalMatrix[1, 1] = 1;
            yRotationalMatrix[1, 2] = 0;
            yRotationalMatrix[2, 0] = Math.Sin(model.ViewingAngleY) * (-1);
            yRotationalMatrix[2, 1] = 0;
            yRotationalMatrix[2, 2] = Math.Cos(model.ViewingAngleY);
            double[,] zRotationalMatrix = new double[3, 3];
            zRotationalMatrix[0, 0] = Math.Cos(model.ViewingAngleZ);
            zRotationalMatrix[0, 1] = Math.Sin(model.ViewingAngleZ) * (-1);
            zRotationalMatrix[0, 2] = 0;
            zRotationalMatrix[1, 0] = Math.Sin(model.ViewingAngleZ);
            zRotationalMatrix[1, 1] = Math.Cos(model.ViewingAngleZ);
            zRotationalMatrix[1, 2] = 0;
            zRotationalMatrix[2, 0] = 0;
            zRotationalMatrix[2, 1] = 0;
            zRotationalMatrix[2, 2] = 1;


            model.PointsRotated = new Point[model.Xn, model.Yn];

            for (int i = 0; i < model.Xn; i++)
            {
                for (int j = 0; j < model.Yn; j++)
                {
                    double[,] pointVector = new double[3, 1];
                    pointVector[0, 0] = model.Points[i, j].X;
                    pointVector[1, 0] = model.Points[i, j].Y;
                    pointVector[2, 0] = model.Points[i, j].Z;

                    double[,] xRotatedPointVector = MultiplyMatrices(xRotationalMatrix, pointVector);
                    double[,] xyRotatedPointVector = MultiplyMatrices(yRotationalMatrix, xRotatedPointVector);
                    double[,] xyzRotatedPointVector = MultiplyMatrices(zRotationalMatrix, xyRotatedPointVector);

                    model.PointsRotated[i, j] = new Point
                    {
                        X = xyzRotatedPointVector[0, 0],
                        Y = xyzRotatedPointVector[1, 0],
                        Z = xyzRotatedPointVector[2, 0],
                    };
                }  //j
            }  //i
        }  //RotateData

        private Point RotatePoint(Point point, double xAngle, double yAngle, double zAngle)
        {
            double[,] xRotationalMatrix = new double[3, 3];
            xRotationalMatrix[0, 0] = 1;
            xRotationalMatrix[0, 1] = 0;
            xRotationalMatrix[0, 2] = 0;
            xRotationalMatrix[1, 0] = 0;
            xRotationalMatrix[1, 1] = Math.Cos(xAngle);
            xRotationalMatrix[1, 2] = Math.Sin(xAngle) * (-1);
            xRotationalMatrix[2, 0] = 0;
            xRotationalMatrix[2, 1] = Math.Sin(xAngle);
            xRotationalMatrix[2, 2] = Math.Cos(xAngle);
            double[,] yRotationalMatrix = new double[3, 3];
            yRotationalMatrix[0, 0] = Math.Cos(yAngle);
            yRotationalMatrix[0, 1] = 0;
            yRotationalMatrix[0, 2] = Math.Sin(yAngle);
            yRotationalMatrix[1, 0] = 0;
            yRotationalMatrix[1, 1] = 1;
            yRotationalMatrix[1, 2] = 0;
            yRotationalMatrix[2, 0] = Math.Sin(yAngle) * (-1);
            yRotationalMatrix[2, 1] = 0;
            yRotationalMatrix[2, 2] = Math.Cos(yAngle);
            double[,] zRotationalMatrix = new double[3, 3];
            zRotationalMatrix[0, 0] = Math.Cos(zAngle);
            zRotationalMatrix[0, 1] = Math.Sin(zAngle) * (-1);
            zRotationalMatrix[0, 2] = 0;
            zRotationalMatrix[1, 0] = Math.Sin(zAngle);
            zRotationalMatrix[1, 1] = Math.Cos(zAngle);
            zRotationalMatrix[1, 2] = 0;
            zRotationalMatrix[2, 0] = 0;
            zRotationalMatrix[2, 1] = 0;
            zRotationalMatrix[2, 2] = 1;

            double[,] pointVector = new double[3, 1];
            pointVector[0, 0] = point.X;
            pointVector[1, 0] = point.Y;
            pointVector[2, 0] = point.Z;

            double[,] xRotatedPointVector = MultiplyMatrices(xRotationalMatrix, pointVector);
            double[,] xyRotatedPointVector = MultiplyMatrices(yRotationalMatrix, xRotatedPointVector);
            double[,] xyzRotatedPointVector = MultiplyMatrices(zRotationalMatrix, xyRotatedPointVector);

            Point rotatedPoint = new Point
            {
                X = xyzRotatedPointVector[0, 0],
                Y = xyzRotatedPointVector[1, 0],
                Z = xyzRotatedPointVector[2, 0],
            };
            return rotatedPoint;
        }  //RotatePoint

        private void GenerateSvg(HomeViewModel model)
        {
            double PlotMiddleX = model.PlotSizeX / 2;
            double PlotMiddleY = model.PlotSizeY / 2;
            StringBuilder sb = new StringBuilder();
            sb.Append("<svg width = \""+ model.PlotSizeX + "\" height = \"" + model.PlotSizeY + "\">");
            //axes
            sb.Append("<line x1=\"" + (PlotMiddleX + model.AxisXStartRotated.X) + "\" y1=\"" + (PlotMiddleY - model.AxisXStartRotated.Z) + "\" x2=\"" + (PlotMiddleX + model.AxisXEndRotated.X) + "\" y2=\"" + (PlotMiddleY - model.AxisXEndRotated.Z) + "\" style=\"stroke:rgb(0, 0, 0); stroke-width:1\" />");
            sb.Append("<line x1=\"" + (PlotMiddleX + model.AxisYStartRotated.X) + "\" y1=\"" + (PlotMiddleY - model.AxisYStartRotated.Z) + "\" x2=\"" + (PlotMiddleX + model.AxisYEndRotated.X) + "\" y2=\"" + (PlotMiddleY - model.AxisYEndRotated.Z) + "\" style=\"stroke:rgb(0, 0, 0); stroke-width:1\" />");
            sb.Append("<line x1=\"" + (PlotMiddleX + model.AxisZStartRotated.X) + "\" y1=\"" + (PlotMiddleY - model.AxisZStartRotated.Z) + "\" x2=\"" + (PlotMiddleX + model.AxisZEndRotated.X) + "\" y2=\"" + (PlotMiddleY - model.AxisZEndRotated.Z) + "\" style=\"stroke:rgb(0, 0, 0); stroke-width:1\" />");
            sb.Append("<text x=\"" + (PlotMiddleX + model.AxisXEndRotated.X) + "\" y=\"" + (PlotMiddleY - model.AxisXEndRotated.Z) + "\" font-size=\"12\">x</text>");
            sb.Append("<text x=\"" + (PlotMiddleX + model.AxisYEndRotated.X) + "\" y=\"" + (PlotMiddleY - model.AxisYEndRotated.Z) + "\" font-size=\"12\">y</text>");
            sb.Append("<text x=\"" + (PlotMiddleX + model.AxisZEndRotated.X) + "\" y=\"" + (PlotMiddleY - model.AxisZEndRotated.Z) + "\" font-size=\"12\">z</text>");

            //plot each datapoint
            for (int i = 0; i < model.Xn; i++)
            {
                for (int j = 0; j < model.Yn; j++)
                {
                    double positionRelativeToMax = (model.Points[i, j].Z - model.ZMin) / model.ZRange;
                    double positionRelativeToMid = 1 - Math.Abs((model.Points[i, j].Z - model.ZMid) * 2 / model.ZRange);
                    double positionRelativeToMin = (model.ZMax - model.Points[i, j].Z) / model.ZRange;
                    int red = (int)(positionRelativeToMax * positionRelativeToMax * 255);
                    int green = (int)(positionRelativeToMid * positionRelativeToMid * positionRelativeToMid * positionRelativeToMid * 255);
                    int blue = (int)(positionRelativeToMin * positionRelativeToMin * 255);
                    //sb.Append("<circle cx=\"" + (PlotMiddleX + rotatedPoints[i, j].X) + "\" cy=\"" + (PlotMiddleY - rotatedPoints[i, j].Z) + "\" r=\"2\" stroke=rgb(" + red + ", " + green + ", " + blue + ") stroke-width=\"1\" fill=rgb(" + red + ", " + green + ", " + blue + ") />");
                    if (i > 0 && j > 0)
                    {
                        sb.Append("<polygon points=\"" + (PlotMiddleX + model.PointsRotated[i, j].X) + ", " + (PlotMiddleY - model.PointsRotated[i, j].Z) + " " + (PlotMiddleX + model.PointsRotated[i-1, j].X) + ", " + (PlotMiddleY - model.PointsRotated[i-1, j].Z) + " " + (PlotMiddleX + model.PointsRotated[i-1, j-1].X) + ", " + (PlotMiddleY - model.PointsRotated[i-1, j-1].Z) + " " + (PlotMiddleX + model.PointsRotated[i, j-1].X) + ", " + (PlotMiddleY - model.PointsRotated[i, j-1].Z) + "\" style=\"fill:rgb(" + red + ", " + green + ", " + blue + "); stroke: black; stroke-width:1 \" />");
                    }
                    if (i > 0)
                    {
                        sb.Append("<line x1=\"" + (PlotMiddleX + model.PointsRotated[i, j].X) + "\" y1=\"" + (PlotMiddleY - model.PointsRotated[i, j].Z) + "\" x2=\"" + (PlotMiddleX + model.PointsRotated[i - 1, j].X) + "\" y2=\"" + (PlotMiddleY - model.PointsRotated[i - 1, j].Z) + "\" style=\"stroke:rgb(" + red + ", " + green + ", " + blue + "); stroke-width:1\" />");
                    }
                    if (j > 0)
                    {
                        sb.Append("<line x1=\"" + (PlotMiddleX + model.PointsRotated[i, j].X) + "\" y1=\"" + (PlotMiddleY - model.PointsRotated[i, j].Z) + "\" x2=\"" + (PlotMiddleX + model.PointsRotated[i, j - 1].X) + "\" y2=\"" + (PlotMiddleY - model.PointsRotated[i, j - 1].Z) + "\" style=\"stroke:rgb(" + red + ", " + green + ", " + blue + "); stroke-width:1\" />");
                    }                    
                }
            }
            sb.Append("</svg>");
            model.Svg = sb.ToString();
        }  //GenerateSvg

        private double[,] MultiplyMatrices(double[,] first, double[,] second)
        {
            int firstRows = first.GetUpperBound(0) + 1;
            int firstColumns = first.GetUpperBound(1) + 1;
            int secondRows = second.GetUpperBound(0) + 1;
            int secondColumns = second.GetUpperBound(1) + 1;

            if(firstColumns != secondRows) { throw new InvalidOperationException("These matrices cannot be multiplied."); }
            double[,] result = new double[firstRows, secondColumns];

            //(AB)_ij = Sigma(k=0 to m) A_ik B_kj  where m = columns in first matrix = rows in second matrix
            //i over rows in first matrix, j over columns in second matrix
            for (int i = 0; i < firstRows; i++)
            {
                for (int j = 0; j < secondColumns; j++)
                {
                    for (int k = 0; k < firstColumns; k++)
                    {
                        result[i, j] += first[i, k] * second[k, j];
                    }
                }
            }
            return result;
        }  //MultiplyMatrices

        private void GetStatistics(Point[,] points, out double minValue, out double midValue, out double maxValue, out double valueSpan)
        {
            int xN = points.GetUpperBound(0);
            int yN = points.GetUpperBound(1);

            minValue = double.MaxValue;
            maxValue = 0;
            for (int i = 0; i < xN; i++)
            {
                for (int j = 0; j < yN; j++)
                {
                    if (points[i, j].Z < minValue) { minValue = points[i, j].Z; }
                    if (points[i, j].Z > maxValue) { maxValue = points[i, j].Z; }
                }
            }
            valueSpan = maxValue - minValue;
            midValue = (minValue + maxValue) / 2;
        }

        private void SetDomain(HomeViewModel model)
        {
            model.XMax = model.XMin + (model.XStep * model.Xn);
            model.YMax = model.YMin + (model.YStep * model.Yn);
            model.ZDomain = model.ZUpperBound - model.ZLowerBound;
        }  //SetDomain

        private void SetAxes(HomeViewModel model)
        {
            model.Origin = new Point { X = 0, Y = 0, Z = 0 };
            model.AxisXStart = new Point { X = model.XMin, Y = 0, Z = 0 };
            model.AxisXEnd = new Point { X = model.XMax, Y = 0, Z = 0 };
            model.AxisYStart = new Point { X = 0, Y = model.YMin, Z = 0 };
            model.AxisYEnd = new Point { X = 0, Y = model.YMax, Z = 0 };
            model.AxisZStart = new Point { X = 0, Y = 0, Z = model.ZLowerBound };
            model.AxisZEnd = new Point { X = 0, Y = 0, Z = model.ZUpperBound };
        }  //SetAxes
    }  //class
}  //namespace