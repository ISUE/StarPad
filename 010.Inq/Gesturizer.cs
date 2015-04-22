using starPadSDK.Geom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;

namespace starPadSDK.Inq {
 
    public interface Gesture {
        Gesturizer.Result Process(Gesturizer g, Stroq s, bool onlyPartial, List<Stroq> prev);
        void   Reset(Gesturizer g);
        void   Fire(Stroq[] strokes);
    }

    // defines an abstract one stroke Gesture
    public abstract class OneStrokeGesture : Gesture {
        public OneStrokeGesture() { }
        public abstract void Fire(Stroq[] strokes);
        public abstract bool Test(Stroq s);
        public void Reset(Gesturizer g) { }
        public Gesturizer.Result Process(Gesturizer g, Stroq s, bool onlyPartial, List<Stroq> prev) {
            if (onlyPartial)
                return Gesturizer.Result.Unrecognized;
            if (Test(s))
                return Gesturizer.Result.Recognized;

            return Gesturizer.Result.Unrecognized;
        }
    }
    // defines an abstract two stroke crop G
    public abstract class TwoStrokeGesture : Gesture {
        TextBox             _text = null;
        void AddText(Gesturizer g, double x, double y) {
            _text = new TextBox();
            _text.Text = Prompt;
            _text.IsHitTestVisible = false;
            _text.BorderBrush = Brushes.Transparent;
            _text.RenderTransform = new TranslateTransform(x, y);
            g.Children.Add(_text);
        }
        void ClearText(Gesturizer g) {
            g.Children.Remove(_text);
            _text = null;
        }

        public delegate void FiredHandler(Gesture g, Stroq[] strokes);
        public TwoStrokeGesture() {}
        public bool OneStroke { get; set; }
        public void Reset(Gesturizer g) { ClearText(g); }
        public abstract string Prompt { get; }
        public abstract void Fire(Stroq[] strokes);
        public abstract bool Test1(Stroq s);
        public abstract bool Test2(Stroq s, Stroq prev);
        public Gesturizer.Result Process(Gesturizer g, Stroq s, bool onlyPartial, List<Stroq> prev) {
            if (OneStroke) {
                if (onlyPartial)
                    return Gesturizer.Result.Unrecognized;
                if (Test1(s))
                    return Gesturizer.Result.Recognized;

                return Gesturizer.Result.Unrecognized;
            } else {
                if (_text != null)
                    ClearText(g);
                if (onlyPartial && prev.Count == 1 && Test1(prev[0]) && Test2(s, prev[0]))
                    return Gesturizer.Result.Recognized;
                if (!onlyPartial && Test1(s)) {
                    if (g.Canvas != null)
                        AddText(g, s.GetBounds().Left, s.GetBounds().Top);
                    return Gesturizer.Result.Partial;
                }
            }

            return Gesturizer.Result.Unrecognized;
        } 
    }
    // defines an abstract parameterized flick command
    public abstract class FlickCommand : TwoStrokeGesture {
        string _chars;
        public FlickCommand(string chars) { _chars = chars;  }
        public override bool Test1(Stroq s) { return s.IsFlick(); }
        public override bool Test2(Stroq s, Stroq prev) { return s.IsChar(_chars) && prev.BackingStroke.HitTest(s.Select((Pt p) => (Point)p), new RectangleStylusShape(1, 1)); }
        public override string Prompt { get { return "Write Mnemonic"; } }
    }

    public class Gesturizer : Canvas {
        public enum Result {
            Recognized,
            Partial,
            Unrecognized
        };

        List<Gesture> _gestures = new List<Gesture>();
        List<Stroq>   _pending = new List<Stroq>();
        Canvas        _canvas = null;
        void   fireGesture(Gesture g) {
            if (GestureRecognizedEvent != null)
                GestureRecognizedEvent(this, g, _pending.ToArray());
            Console.WriteLine("Fired " + g.ToString());
            g.Fire(_pending.ToArray());
            _pending.Clear();
            Children.Clear();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender">the Gesturizer recognizer that generate this event</param>
        /// <param name="g">the recognized Gesture</param>
        /// <param name="strokes">the strokes that comprise the Gesture</param>
        public delegate void GestureRecognizedHandler (object sender, Gesture g, Stroq[] strokes);
        /// <summary>
        /// </summary>
        /// <param name="sender">the Gesturizer recognizer that generate this event</param>
        /// <param name="s">the Stroq that is not a gesture</param>
        /// <returns>whether the callback function has handled the event</returns>
        public delegate bool StrokeUnrecognizedHandler(object sender, Stroq s);
        /// <summary>
        /// Event called when any Gesture has been recognized
        /// </summary>
        public event GestureRecognizedHandler  GestureRecognizedEvent;
        /// <summary>
        /// Event called when a stroke has been determined not to be a gesture stroke
        /// </summary>
        public event StrokeUnrecognizedHandler StrokeUnrecognizedEvent;
        public Gesturizer() {   }
        public Gesturizer(Canvas c) { _canvas = c; }
        /// <summary>
        /// Canvas that gestures are being collected on and where feedback is displayed
        /// </summary>
        public Canvas      Canvas {
            get { return _canvas; }
            set { _canvas = value; }
        }
        public Gesture[]   Gestures { get { return _gestures.ToArray(); } }
        public List<Stroq> Pending {
            get { return _pending; }
            set { _pending = value; }
        }

        public void Clear()        { _gestures.Clear(); }
        public void Add(Gesture g) { _gestures.Add(g); }
        public void Rem(Gesture g) { _gestures.Remove(g); }
        public void Reset() {
            foreach (Gesture g in _gestures)
                g.Reset(this);
            // the current policy is that we can have no more than two-stroke gestures.
            // so if we have a partial match or no match at all, we need to flush out
            // all the pending strokes (which for now can be at most 1 stroke).
            foreach (Stroq ps in _pending)
                if (StrokeUnrecognizedEvent != null)
                    StrokeUnrecognizedEvent(this, ps);
            _pending.Clear();
            Children.Clear();
        }

        /// <summary>
        /// The temporary color of strokes that may turn into multi-stroke Gestures
        /// </summary>
        public Color GestureStrokeIntermediateColor = Colors.Red;

        /// <summary>
        /// Process a stroke sequentially through each gesture recognizer, terminating only
        /// if a gesture is completely recognized.  An event is fired when a gesture is matched.
        /// The method returns whether the stroke completed a gesture, is potentially part of a
        /// multi-stroke gesture, or if it is not part of any gesture.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Result Process(Stroq s) {
            //  try to find a completed gesture
            foreach (Gesture g in Gestures){
                Result r = g.Process(this, s, true, _pending);
                if (r == Result.Recognized) {
                    _pending.Add(s);
                    fireGesture(g);
                    return Result.Recognized;
                }
            }

            // pending gesture strokes -> real strokes if they weren't completed
            // the current policy is that we can have no more than two-stroke gestures.
            // so if we have a partial match or no match at all, we need to flush out
            // all the pending strokes (which for now can be at most 1 stroke).
            foreach (Stroq ps in _pending)
                if (StrokeUnrecognizedEvent != null)
                    StrokeUnrecognizedEvent(this, ps);
            _pending.Clear();

            // try to find a new (or new partial)gesture
            foreach (Gesture g in Gestures){
                Result r = g.Process(this, s, false, _pending);
                if (r != Result.Unrecognized) {
                    // save the stroke if a multi-stroke gesture is pending.
                    s.BackingStroke.DrawingAttributes.Color = GestureStrokeIntermediateColor;
                    _pending.Add(s);
                    if (r == Result.Recognized)
                        fireGesture(g);
                    else
                       Children.Add(s);
                    return r;
                }
            }

            // gesture stroke -> real stroke if nothing matched it
            Children.Clear();
            if (StrokeUnrecognizedEvent != null)
                StrokeUnrecognizedEvent(this, s);
            return Result.Unrecognized;
        }

    }
}
