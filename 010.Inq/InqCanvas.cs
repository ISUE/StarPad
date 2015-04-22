using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using starPadSDK.Geom;
using starPadSDK.Utils;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Shapes;

namespace starPadSDK.Inq {
    public class MyRend : DynamicRenderer
    {
        // TJC: define stylus id so only take ink/inq from stylus input, not multitouch
        public const int STYLUS_ID = 2;
        
        RawStylusInput rs = null;

        public delegate void ForcedMoveHandler(object sender, Point p);
        public event ForcedMoveHandler ForcedMove;
        public MyRend() {  }
        protected override void OnStylusDown(RawStylusInput rawStylusInput) {
            rs = rawStylusInput;
            try {
                // TJC: only take in stylus ink, not multitouch
                // this prevents drawing the ink as we move our touches (e.g. scale/pan)
                bool isStylus = rs.StylusDeviceId == STYLUS_ID;
                if (isStylus)
                {
                    base.OnStylusDown(rawStylusInput);
                }
            }
            catch (Exception) { }
        }
        public override void Reset(StylusDevice stylusDevice, StylusPointCollection stylusPoints) {
            try {
                base.Reset(stylusDevice, stylusPoints);
            }
            catch (Exception e) {
            }
        }
        public void Move(Point p) {
            if (rs == null)
                return;
            Point offset = this.Element.PointToScreen(new Point());
            p = new Point(p.X - offset.X, p.Y - offset.Y);
            if (ForcedMove != null)
                ForcedMove(this, p);
            rs.SetStylusPoints(new StylusPointCollection(new StylusPoint[] { new StylusPoint(p.X , p.Y, 0.5f, rs.GetStylusPoints().First().Description, new int[] { 0, 1, 0 }) }));
            OnStylusMove(rs);
        }
    }
    public class InqCanvas : Canvas {

        // TJC: define stylus id so only take ink/inq from stylus input, not multitouch
        public const int STYLUS_ID = MyRend.STYLUS_ID;
        
        // TJC: define touch count to track multi vs single touch
        private int _touchCount = 0;
        private int _maxTouches = 0;

        private MyRend _dynamicRenderer = new MyRend();
        private InkPresenter           _inkPresenter = new InkPresenter();
        private StylusPointCollection  _stylusPoints = null;
        private StroqCollection        _stroqs = new StroqCollection();
        private bool                   _inkEnabled = true;
        protected bool                _drawStroqs = true;
        private IEnumerable<UIElement> _allChildren {
            get {
                foreach (UIElement e in Children) yield return e;
                yield return _inkPresenter;
            }
        }

        public      MyRend                 DynamicRenderer { get { return _dynamicRenderer; } set { _dynamicRenderer = value; } }
        protected InkPresenter         InkPresenter    { get { return _inkPresenter; } }

        protected override Visual      GetVisualChild(int index) {
            if (index == Children.Count) return _inkPresenter;
            else return Children[index];
        }
        protected override int         VisualChildrenCount { get { return 1 + Children.Count; } }
        protected override IEnumerator LogicalChildren { get { return _allChildren.GetEnumerator(); } }

        protected void          stroqs_Changed(object sender, StroqCollection.ChangedEventArgs e) {
            if (_drawStroqs)
                switch (e.Action) {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        foreach (Stroq s in e.NewItems) 
                            _inkPresenter.Strokes.Add(s.BackingStroke);
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        foreach (Stroq s in e.OldItems) 
                            _inkPresenter.Strokes.Remove(s.BackingStroke);
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        _inkPresenter.Strokes.Clear();
                        if (e.NewItems != null) 
                            _inkPresenter.Strokes.Add(new StrokeCollection(e.NewItems.Select((Stroq s) => s.BackingStroke)));
                        break;
                    default:
                        throw new NotImplementedException();
                }
        }

        protected override void OnStylusDown(StylusDownEventArgs e) {
            base.OnStylusDown(e);

            // TJC: only take in stylus ink, not multitouch
            bool isStylus = e.StylusDevice.Id == MyRend.STYLUS_ID || e.StylusDevice.StylusButtons.Count == 2; // tip and barrel 
            if (!isStylus) 
            { 
                _touchCount++;
                if (_touchCount > _maxTouches) _maxTouches = _touchCount;
                //System.Console.WriteLine("DOWN TC: " + _touchCount + " " + _maxTouches);
                //if (_touchCount > 0) return; // don't capture ink if more than one touch // TJC TEST
            }

            if (InkEnabled) {
                _stylusPoints = new StylusPointCollection();
                // Capture the stylus so all stylus input is routed to this control.
                Stylus.Capture(this);
                _stylusPoints.Add(e.GetStylusPoints(this, _stylusPoints.Description));
            }
        }
        protected override void OnStylusMove(StylusEventArgs e) {
            base.OnStylusMove(e);

            // TJC: only take in stylus ink, not multitouch
            bool isStylus = e.StylusDevice.Id == MyRend.STYLUS_ID || e.StylusDevice.StylusButtons.Count == 2; // tip and barrel
            if (!isStylus)
            {
                //System.Console.WriteLine("MOVE TC: " + _touchCount + " " + _maxTouches);
                //if (_touchCount > 0) return; // don't capture ink if more than one touch // TJC: test
            }

            if (_stylusPoints == null) return;
            _stylusPoints.Add(e.GetStylusPoints(this, _stylusPoints.Description));
        }
        protected override void OnStylusUp(StylusEventArgs e) {
            base.OnStylusUp(e);

            // TJC: only take in stylus ink, not multitouch
            bool isStylus = e.StylusDevice.Id == MyRend.STYLUS_ID || e.StylusDevice.StylusButtons.Count == 2; // tip and barrel
            if (!isStylus)
            {
                _touchCount--;
                //System.Console.WriteLine("UP TC: " + _touchCount + " " + _maxTouches);
                //if (_maxTouches > 0) return; // TJC: test

                // reset max touch once we hit 0
                if (_touchCount <= 0)
                {
                    _maxTouches = 0;
                }
            }

            // Release stylus capture.
            if (Stylus.Captured == this) // bcz: uncapturing the stylus will uncapture the mouse.  However, widgets like SelectionFeedback may have Captured just the Mouse and won't get their Up events - so don't uncapture unless the captured object is 'this'
                Stylus.Capture(null);
            if (_stylusPoints == null) return;
            _stylusPoints.Add(e.GetStylusPoints(this, _stylusPoints.Description));
            Stroke stroke = new Stroke(_stylusPoints);
            stroke.DrawingAttributes = _dynamicRenderer.DrawingAttributes.Clone();
            Stroq s = new Stroq(stroke);
            if(KeepStroqs) _stroqs.Add(s);
            _stylusPoints = null;

            RaiseStroqCollectedEvent(s, !isStylus || e.StylusDevice.SwitchState(InqUtils.BarrelSwitch) == StylusButtonState.Down);
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e) {
            base.OnMouseRightButtonUp(e);
            Mouse.Capture(null);
            e.Handled = true; // need this or the context menu pops up after right-button dragging
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonDown(e);
            // If a stylus generated this event, return.
            if (e.StylusDevice != null) return;
            Mouse.Capture(this);
            if (InkEnabled) {
                _stylusPoints = new StylusPointCollection();
                Point pt = e.GetPosition(this);
                _stylusPoints.Add(new StylusPoint(pt.X, pt.Y));
            }
        }

        void DynamicRenderer_ForcedMove(object sender, Point pt) {
            if (_stylusPoints != null)
                _stylusPoints.Add(new StylusPoint(pt.X, pt.Y));
        }
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            // If a stylus generated this event, return.
            if (e.StylusDevice != null) return;
            if (e.LeftButton == MouseButtonState.Released || _stylusPoints == null) return;
            Point pt = e.GetPosition(this);
            _stylusPoints.Add(new StylusPoint(pt.X, pt.Y));
        }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonUp(e);
            Mouse.Capture(null);
            // If a stylus generated this event, return.
            if (e.StylusDevice != null || _stylusPoints == null || _stylusPoints.Count == 0) return;
            Point pt = e.GetPosition(this);
            //_stylusPoints.Add(new StylusPoint(pt.X, pt.Y));

            Stroke stroke = new Stroke(_stylusPoints);
            stroke.DrawingAttributes = _dynamicRenderer.DrawingAttributes.Clone();
            Stroq s = new Stroq(stroke);
            if(KeepStroqs) _stroqs.Add(s);
            _stylusPoints = null;
            RaiseStroqCollectedEvent(s, false);
        }

        
        static InqCanvas() {
            // Allow ink to be drawn only within the bounds of the control.
            Type owner = typeof(InqCanvas);
            ClipToBoundsProperty.OverrideMetadata(owner, new FrameworkPropertyMetadata(true));
        }
        public InqCanvas(): this(true) {}
        public InqCanvas(bool keepstroqs) : base() {
            KeepStroqs = keepstroqs;
            _inkPresenter.AttachVisuals(_dynamicRenderer.RootVisual, _dynamicRenderer.DrawingAttributes);
            StylusPlugIns.Add(_dynamicRenderer);
            AddLogicalChild(_inkPresenter);
            AddVisualChild(_inkPresenter);
            InvalidateMeasure();
            _stroqs.Changed += new EventHandler<StroqCollection.ChangedEventArgs>(stroqs_Changed);
            DynamicRenderer.ForcedMove += new MyRend.ForcedMoveHandler(DynamicRenderer_ForcedMove);
        }
        public    bool              KeepStroqs { get; private set; }
        public    bool              InkEnabled               { get { return _inkEnabled; }
            set { _inkEnabled = value;  _inkPresenter.IsEnabled = _inkEnabled; _dynamicRenderer.Enabled = _inkEnabled; }
        }
        public    DrawingAttributes DefaultDrawingAttributes { get { return _dynamicRenderer.DrawingAttributes; } 
                                                               set { _dynamicRenderer.DrawingAttributes = value; } }
        public    StroqCollection   Stroqs                   { get { return _stroqs; } }
        public    void              StartDrawing(StylusDevice stylus) {
            if (stylus != null && stylus.InAir)
                return;
            _dynamicRenderer.Reset(stylus, _stylusPoints);
            _dynamicRenderer.Enabled = true;
            _dynamicRenderer.DrawingAttributes.Color = Colors.Black;
            // Capture the stylus so all stylus input is routed to this control.
            Stylus.Capture(this);
            _stylusPoints = new StylusPointCollection();
        }

        public class StroqCollectedEventArgs : RoutedEventArgs {
            public Stroq Stroq { get; private set; }
            public bool  RightButton { get; private set; }
            public StroqCollectedEventArgs(RoutedEvent revt, Stroq s, bool rightbutton) : base(revt) {
                Stroq = s;
                RightButton = rightbutton;
            }
            protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget) {
                StroqCollectedEventHandler hdlr = (StroqCollectedEventHandler)genericHandler;
                hdlr(genericTarget, this);
            }
        }
        public delegate void StroqCollectedEventHandler(object sender, StroqCollectedEventArgs e);
        public static readonly RoutedEvent StroqCollectedEvent = EventManager.RegisterRoutedEvent("StroqCollected", RoutingStrategy.Bubble,
            typeof(StroqCollectedEventHandler), typeof(InqCanvas));
        public event StroqCollectedEventHandler StroqCollected { 
            add { AddHandler(StroqCollectedEvent, value); } 
            remove { RemoveHandler(StroqCollectedEvent, value); } }
        protected void RaiseStroqCollectedEvent(Stroq s, bool rightButton) {
            StroqCollectedEventArgs evtargs = new StroqCollectedEventArgs(StroqCollectedEvent, s, rightButton);
            RaiseEvent(evtargs);
        }
    }
}
