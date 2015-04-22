using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using starPadSDK.Geom;
using starPadSDK.Utils;
using System.Windows;
using System.Windows.Media;

namespace starPadSDK.Inq {
    public class StroqElement : FrameworkElement {
        protected DrawingVisual _dv;
        protected Stroq _stroq;
        public Stroq Stroq { get { return _stroq; } }
        public StroqElement(Stroq stroq) {
            _dv = new DrawingVisual();
            _stroq = stroq;
            _stroq.PointChanged += _stroq_PointChanged;
            _stroq.PointsCleared += _stroq_PointsCleared;
            _stroq.PointsModified += _stroq_PointsModified;
            _stroq.BackingStroke.DrawingAttributes.AttributeChanged += new System.Windows.Ink.PropertyDataChangedEventHandler(DrawingAttributes_AttributeChanged);
            Redraw();
            AddVisualChild(_dv);
        }

        void DrawingAttributes_AttributeChanged(object sender, System.Windows.Ink.PropertyDataChangedEventArgs e) {
            Redraw();
        }

        protected void _stroq_PointsModified(Stroq s, Mat? m) {
            Redraw();
        }
        protected void _stroq_PointsCleared(Stroq s) {
            Redraw();
        }
        protected void _stroq_PointChanged(Stroq s, int i) {
            Redraw();
        }

        protected override int VisualChildrenCount { get { return 1; } }
        protected override Visual GetVisualChild(int index) {
            if(index != 0) throw new ArgumentOutOfRangeException("index", "StroqElements only have one visual child");
            return _dv;
        }

        protected void Redraw() {
            DrawingContext dc = _dv.RenderOpen();
            _stroq.BackingStroke.Draw(dc);
            dc.Close();
        }
    }
}
