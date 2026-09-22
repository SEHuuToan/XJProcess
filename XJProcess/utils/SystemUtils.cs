using System;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

namespace XJProcess.utils
{
    public static class SystemUtils
    {
        public static void UpdateProgressArc(Path pathControl, int remainingSeconds, int totalSeconds)
        {
            if (pathControl == null || totalSeconds <= 0) return;
            double progress = (double)remainingSeconds / totalSeconds;
            if (progress < 0) progress = 0;
            if (progress > 1) progress = 1;
            double angle = progress * 360;
            double radius = 51;
            Point center = new Point(59, 59);
            double rad = (angle - 90) * (Math.PI / 180.0);
            Point endPoint = new Point(
                center.X + radius * Math.Cos(rad),
                center.Y + radius * Math.Sin(rad)
            );
            bool isLargeArc = angle > 180;
            PathFigure figure = new PathFigure();
            figure.StartPoint = new Point(center.X, center.Y - radius);
            ArcSegment arcSegment = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = isLargeArc
            };
            figure.Segments.Add(arcSegment);
            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            pathControl.Data = geometry;
        }
    }
}