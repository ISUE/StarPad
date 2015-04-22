using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml;
using Microsoft.Ink;
using starPadSDK.CharRecognizer;
using starPadSDK.Geom;
using starPadSDK.Inq;
using starPadSDK.Inq.MSInkCompat;
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;
using starPadSDK.MathUI;
using starPadSDK.UnicodeNs;
using starPadSDK.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CuspDetector = starPadSDK.Inq.BobsCusps.FeaturePointDetector;
using SystemGesture = System.Windows.Input.SystemGesture;

namespace MathRecoScaffold
{
    public partial class Window1 : Window
    {
        private StroqCollection _mathStroqs = new StroqCollection();
        private MathRecognition _mrec;
        private InkColorizer _colorizer = new InkColorizer();

        public Window1()
        {
            InitializeComponent();
            _mrec = new MathRecognition( _mathStroqs ); 
           
            _mrec.ParseUpdated += _mrec_ParseUpdated;

            _altsMenuCrea = new AlternatesMenuCreator( alternatesMenu, _mrec );

            inqCanvas.StroqCollected += inqCanvas_StroqCollected;
            inqCanvas.PreviewStylusDown += inqCanvas_PreviewStylusDown;
            inqCanvas.PreviewMouseLeftButtonDown += inqCanvas_PreviewMouseLeftButtonDown;
            inqCanvas.PreviewMouseMove += inqCanvas_PreviewMouseMove;
            inqCanvas.PreviewMouseLeftButtonUp += inqCanvas_PreviewMouseLeftButtonUp;

        }

     
        #region Stroke Selection and Move

        private bool _moving = false;
        void inqCanvas_PreviewMouseLeftButtonUp( object sender, MouseButtonEventArgs e )
        {
            if ( _moving )
            {
                _moving = false;
                using ( _mrec.BatchEditNoRecog( true ) )
                {
                    Selected.Contents.MoveTo( e.GetPosition( inqCanvas ) );
                }
                Selected.Contents.EndMove();
                if ( _movingLock != null )
                {
                    _movingLock.Dispose(); // this will call Parse itself
                    _movingLock = null;
                }
                else
                    Selected.Contents.Reparse( _mrec ); 
                Deselect();
                Mouse.Capture( null );
                e.Handled = true;
                inqCanvas.InkEnabled = true;
            }
        }

        void inqCanvas_PreviewMouseMove( object sender, MouseEventArgs e )
        {
            if ( _moving )
            {
                using ( _mrec.BatchEditNoRecog( false ) )
                {
                    Selected.Contents.MoveTo( e.GetPosition( inqCanvas ) );
                }
                e.Handled = true;
            }
        }

        void inqCanvas_PreviewMouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            if ( _moving == true )
            { // could be set by stylus going down
                Mouse.Capture( inqCanvas ); // stylus doesn't capture mouse
                e.Handled = true;
                return;
            }
            if ( Selected.Contents != null && Selected.Contents.Outline != null && Selected.Contents.Outline.GetBounds().Contains( e.GetPosition( inqCanvas ) ) )
            {
                Mouse.Capture( inqCanvas );
                StartMove( e.GetPosition( inqCanvas ) );
                e.Handled = true;
            }
        }

        void inqCanvas_PreviewStylusDown( object sender, StylusDownEventArgs e )
        {
            if ( Selected.Contents != null && Selected.Contents.Outline != null && Selected.Contents.Outline.GetBounds().Contains( e.GetPosition( inqCanvas ) ) )
            {
                StartMove( e.GetPosition( inqCanvas ) );
                inqCanvas.InkEnabled = false;
                e.Handled = true;
            }
        }

        private BatchLock _movingLock = null;
        void StartMove( Pt p )
        {
            _moving = true;
            StroqSel ss = Selected.Contents as StroqSel;
            if ( ss != null && ss.AllStroqs.Count > 10 ) _movingLock = _mrec.BatchEdit();
            Selected.Contents.StartMove( p );
            inqCanvas.Stroqs.Remove( Selected.Contents.Outline );
        }

        public Selection Selected = new Selection();
        public void Deselect()
        {
            Selected.Contents = null;
            hideSidebarAlts();
        }

        #endregion

        #region Stroke Analysis

        void inqCanvas_StroqCollected( object sender, InqCanvas.StroqCollectedEventArgs e )
        {
            /* filter out gestures before taking everything else as math */

            /* If we get here, it's a real stroke (not movement), so deselect any selection */
            Deselect();

            #region Scribble Gesture Recognition

            StroqCollection stroqCollection;
            /* check for scribble delete */
            if (ScribbleDelete(e.Stroq, out stroqCollection))
            {
                _mathStroqs.Remove(stroqCollection);
                return;
            }

            #endregion

            /* check for lassos/circles around stuff */
            if (LassoSelect(e.Stroq)) return;

            _mathStroqs.Add(e.Stroq);
        }


        private bool LassoSelect(Stroq stroq)
        {
            if (stroq.OldPolylineCusps().Length <= 4 && stroq.Count > 4)
            {
                Stroq estroq = stroq;
                CuspDetector.CuspSet cs = CuspDetector.FeaturePoints(estroq);

                Pt[] first = new Pt[cs.pts.Count / 2];
                for (int i = 0; i < first.Length; i++)
                    if (cs.distances[i] > cs.dist / 2)
                        break;
                    else first[i] = cs.pts[i];
                Pt[] second = new Pt[cs.pts.Count - first.Length];
                for (int j = 0; j < second.Length; j++) second[j] = cs.pts[first.Length + j];
                Stroq s1 = new Stroq(first);
                Stroq s2 = new Stroq(second);
                float d1, d2;
                s1.OldNearestPoint(s2[-1], out d1);
                s2.OldNearestPoint(s1[0], out d2);
                if (Math.Min(d1, d2) / Math.Max(estroq.GetBounds().Width, estroq.GetBounds().Height) < 0.3f)
                {
                    StroqCollection stqs = _mathStroqs.HitTest(estroq, 50);
                    StroqCollection stqs2 = _mathStroqs.HitTest(estroq.Reverse1(), 50);
                    if (stqs2.Count > stqs.Count)
                        stqs = stqs2;
                    stqs.Remove(estroq);
                    StroqCollection stqs3 = new StroqCollection(stqs.Where((Stroq s) => _mrec.Charreco.Classification(_mrec.Sim[s]) != null));
                    stqs = stqs3;
                    Recognition rtemp = _mrec.ClassifyOneTemp(estroq);
                    if (stqs.Count > 0 && (rtemp == null || !rtemp.alts.Contains(new Recognition.Result(Unicode.S.SQUARE_ROOT))))
                    {
                        if (rtemp != null) Console.WriteLine("select recognized for " + rtemp.allograph);

                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            Deselect();
                            stroq.BackingStroke.DrawingAttributes.Color = Colors.Purple;
                            Selected.Contents = new StroqSel(stqs, stroq, (Stroq s) => _mrec.Charreco.Classification(_mrec.Sim[s]),
                                (Recognition r) => _mrec.Sim[r.strokes], inqCanvas.Stroqs);
                            StroqSel Sel = (StroqSel)Selected.Contents;
                            HashSet<Recognition> recogs = new HashSet<Recognition>(Sel.AllStroqs.Select((Stroq s) => _mrec.Charreco.Classification(_mrec.Sim[s]))
                                .Where((Recognition r) => r != null));
                            if (recogs.Count != 0) showSidebarAlts(recogs, Sel.AllStroqs);
                        }));

                        return true;
                    }
                    else
                    {
                        // Generic additional selections would be called here.
                        return false;
                    }
                }
            }
            return false;
        }

        private bool IsLassoSelect(Stroq stroq)
        {
            if (stroq.OldPolylineCusps().Length <= 4 && stroq.Count > 4)
            {
                Stroq estroq = stroq;
                CuspDetector.CuspSet cs = CuspDetector.FeaturePoints(estroq);

                Pt[] first = new Pt[cs.pts.Count / 2];
                for (int i = 0; i < first.Length; i++)
                    if (cs.distances[i] > cs.dist / 2)
                        break;
                    else first[i] = cs.pts[i];
                Pt[] second = new Pt[cs.pts.Count - first.Length];
                for (int j = 0; j < second.Length; j++) second[j] = cs.pts[first.Length + j];
                Stroq s1 = new Stroq(first);
                Stroq s2 = new Stroq(second);
                float d1, d2;
                s1.OldNearestPoint(s2[-1], out d1);
                s2.OldNearestPoint(s1[0], out d2);
                if (Math.Min(d1, d2) / Math.Max(estroq.GetBounds().Width, estroq.GetBounds().Height) < 0.3f)
                {
                    StroqCollection stqs = _mathStroqs.HitTest(estroq, 50);
                    StroqCollection stqs2 = _mathStroqs.HitTest(estroq.Reverse1(), 50);
                    if (stqs2.Count > stqs.Count)
                        stqs = stqs2;
                    stqs.Remove(estroq);
                    StroqCollection stqs3 = new StroqCollection(stqs.Where((Stroq s) => _mrec.Charreco.Classification(_mrec.Sim[s]) != null));
                    stqs = stqs3;
                    Recognition rtemp = _mrec.ClassifyOneTemp(estroq);
                    if (stqs.Count > 0 && (rtemp == null || !rtemp.alts.Contains(new Recognition.Result(Unicode.S.SQUARE_ROOT))))
                    {
                        if (rtemp != null) Console.WriteLine("select recognized for " + rtemp.allograph);
                        return true;
                    }
                    else
                    {
                        // Generic additional selections would be called here.
                        return false;
                    }
                }
            }
            return false;
        }

        private void DoLassoSelect(Stroq stroq)
        {
            StroqCollection stqs = _mathStroqs.HitTest(stroq, 50);           
            Deselect();
            stroq.BackingStroke.DrawingAttributes.Color = Colors.Purple;
            Selected.Contents = new StroqSel(stqs, stroq, (Stroq s) => _mrec.Charreco.Classification(_mrec.Sim[s]),
                (Recognition r) => _mrec.Sim[r.strokes], inqCanvas.Stroqs);
            StroqSel Sel = (StroqSel)Selected.Contents;
            HashSet<Recognition> recogs = new HashSet<Recognition>(Sel.AllStroqs.Select((Stroq s) => _mrec.Charreco.Classification(_mrec.Sim[s]))
                .Where((Recognition r) => r != null));
            if (recogs.Count != 0) showSidebarAlts(recogs, Sel.AllStroqs);           
        }

        private bool ScribbleDelete(Stroq stroq, out StroqCollection erasedStroqs)
        {
            bool canBeScribble = stroq.OldPolylineCusps().Length > 4;
            if (stroq.OldPolylineCusps().Length == 4)
            {
                int[] pcusps = stroq.OldPolylineCusps();
                Deg a1 = fpdangle(stroq[0], stroq[pcusps[1]], stroq[pcusps[2]] - stroq[pcusps[1]]);
                Deg a2 = fpdangle(stroq[pcusps[1]], stroq[pcusps[1]], stroq[pcusps[3]] - stroq[pcusps[1]]);
                if (a1 < 35 && a2 < 35)
                    canBeScribble = stroq.BackingStroke.HitTest(stroq.ConvexHull().First(), 1);
            }
            if (canBeScribble)
            {
                IEnumerable<Pt> hull = stroq.ConvexHull();
                StroqCollection stqs = inqCanvas.Stroqs.HitTest(hull, 1);
                if (stqs.Count > 1)
                {
                    inqCanvas.Stroqs.Remove(stqs);
                    erasedStroqs = stqs; 
                    inqCanvas.Stroqs.Remove(stroq);
                    return true;
                }
            }
            erasedStroqs = null;
            return false;
        }

        Deg fpdangle(Pt a, Pt b, Vec v)
        {
            return (a - b).Normalized().UnsignedAngle(v.Normalized());
        }

        public Rct bbox(Strokes stks)
        {
            return _mrec.Sim[stks].Aggregate(Rct.Null, (Rct r, Stroq s) => r.Union(s.GetBounds()));
        }

        private void _mrec_ParseUpdated(MathRecognition source, Recognition chchanged, bool updateMath)
        {
            /* Evaluate math if necessary */
            if (updateMath)
                try
                {
                    Evaluator.UpdateMath(_mrec.Ranges.Select((Parser.Range r) => r.Parse));
                }
                catch { }

            /* reset geometry displayed: range displays, etc */
            underlay.Children.Clear();
            inqCanvas.Children.Clear();

            /* set up to draw background yellow thing for range displays */
            Brush fill3 = new SolidColorBrush(Color.FromArgb(50, 255, 255, 180));
            Brush fill2 = new SolidColorBrush(Color.FromArgb(75, 255, 255, 180));
            Brush fill1 = new SolidColorBrush(Color.FromArgb(100, 255, 255, 180));
            Brush sqr3 = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0));
            Brush sqr2 = new SolidColorBrush(Color.FromArgb(75, 0, 255, 0));
            Brush sqr1 = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
            foreach (Parser.Range rrr in _mrec.Ranges)
            {
                Rct rangebbox = bbox(rrr.Strokes);
                Rct box = rangebbox.Inflate(8, 8);

                /* draw yellow box */
                DrawingVisual dv = new DrawingVisual();
                DrawingContext dc = dv.RenderOpen();
                dc.DrawRoundedRectangle(fill3, null, box, 4, 4);
                dc.DrawRoundedRectangle(fill2, null, box.Inflate(-4, -4), 4, 4);
                dc.DrawRoundedRectangle(fill1, null, box.Inflate(-8, -8), 4, 4);
                dc.Close();
                underlay.Children.Add(dv);

                if (rrr.Parse != null)
                {
                    /* draw interpretation of entry */
                    if (rrr.Parse.expr != null)
                    {
                        dv = new DrawingVisual();
                        dc = dv.RenderOpen();
                        // this is an example of normal drawing of an expr
                        Rct nombb = starPadSDK.MathExpr.ExprWPF.EWPF.DrawTop(rrr.Parse.expr, 22, dc, Colors.Blue, new Pt(box.Left, box.Bottom + 24), true).rect;

                        dc.Close();
                        underlay.Children.Add(dv);
                    }

                    /* draw result of computation, if any */
                    if (rrr.Parse.finalSimp != null)
                    {
                        Rct nombb;
                        Expr result = rrr.Parse.matrixOperationResult == null ? rrr.Parse.finalSimp : rrr.Parse.matrixOperationResult;
                        // this is an example of drawing an expr by getting a geometry of it first, so can be used for special effects, etc.
                        Geometry g = starPadSDK.MathExpr.ExprWPF.EWPF.ComputeGeometry(result, 22, out nombb);
                        System.Windows.Shapes.Path p = new System.Windows.Shapes.Path();
                        p.Data = g;
                        p.Stroke = Brushes.Red;
                        p.Fill = Brushes.Transparent;
                        p.StrokeThickness = 1;
                        p.RenderTransform = new TranslateTransform(box.Right + 10, box.Center.Y);
                        inqCanvas.Children.Add(p);
                    }

                    /* colorize ink. Ideally we would have kept track of which ink strokes had changes and only update colorization in those ranges affected
                     * by the changes. */
                    if (rrr.Parse.root != null) _colorizer.Colorize(rrr.Parse.root, rrr.Strokes, _mrec);
                }
            }

            /* Update alternates menu if user wrote a char */
            if (chchanged != null)
            {
                showSidebarAlts(new[] { chchanged }, new StroqCollection(_mrec.Sim[chchanged.strokes]));
            }
        }

        #endregion

        #region Alternative Menu

        AlternatesMenuCreator _altsMenuCrea;

        private void hideSidebarAlts()
        {
            _altsMenuCrea.Clear();
        }

        private void showSidebarAlts( ICollection<Recognition> recogs, StroqCollection stroqs )
        {
            _altsMenuCrea.Populate( recogs, stroqs );
        }

        #endregion        

        #region Menu Controls

        private void clearMenu_Click( object sender, RoutedEventArgs e )
        {
            _mathStroqs.Clear();
            inqCanvas.Stroqs.Clear();
            inqCanvas.Children.Clear();
            underlay.Children.Clear();
            _colorizer.Reset();
        }

        private void quitMenu_Click( object sender, RoutedEventArgs e )
        {
            Application.Current.Shutdown();
        }

        private void newMenu_Click( object sender, RoutedEventArgs e )
        {
            ( new Window1() ).Show();
        }

        private void reparseMenu_Click(object sender, RoutedEventArgs e)
        {
            _mrec.ForceParse();
        }

        #endregion 

    }
}
