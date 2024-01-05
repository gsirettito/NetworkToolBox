using System.Xml;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows;
using System;
using System.Windows.Media;
using System.Windows.Controls;

namespace STPlayerWPF {
    public partial class Spectrum : UserControl {
        PointCollection points;
        double inicio = 0D;
        private bool firstTime;

        public Spectrum() {
            InitializeComponent();
            points = new PointCollection();
        }

        public void Set(double value, double maximum) {
            if (!firstTime) {
                firstTime = true;
                points.Add(new Point(ActualWidth, 0));
            }
            points.Add(new Point(ActualWidth, (value / maximum) * this.ActualHeight));
            polyline.Points = points;
            var copyPoints = new PointCollection();
            for (int i = 0; i < points.Count; i++) {
                if (points[i].X - 10 < 0) continue;
                copyPoints.Add(new Point(points[i].X - 10, points[i].Y));
            }
            points = copyPoints;
        }
    }
}