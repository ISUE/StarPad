using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Utils;
using starPadSDK.Inq;
using starPadSDK.Geom;

namespace starPadSDK.WPFHelp {
    static public class WPFUtil {
        public static TextBox MakeText(string text, Rct rct) {
            TextBox tb = new TextBox();
            Pt loc = rct.BottomLeft;
            double fontHeight = rct.Height / 2;
            tb.Text = text;
            tb.FontSize = fontHeight;
            tb.AcceptsReturn = true;
            tb.BorderThickness = new Thickness(0);
            tb.RenderTransform = new TranslateTransform(loc.X, loc.Y - tb.FontSize * 2);
            tb.Focusable = true;
            return tb;
        }
        public static void EnumerateAncestors(FrameworkElement elt, List<FrameworkElement> output) {
            if (elt == null)
                return;

            output.Add(elt);

            try { EnumerateAncestors((FrameworkElement)elt.Parent, output); }
            catch { }
        }
        public static FrameworkElement GetCommonAncestor(FrameworkElement eltA, FrameworkElement eltB) {
            List<FrameworkElement> ancestorsA = new List<FrameworkElement>();
            List<FrameworkElement> ancestorsB = new List<FrameworkElement>();

            EnumerateAncestors(eltA, ancestorsA);
            EnumerateAncestors(eltB, ancestorsB);

            foreach (FrameworkElement elt in ancestorsA)
                if (ancestorsB.Contains(elt))
                    return elt;

            return null;
        }
        public static Pt TransformFromAtoB(Pt pt, FrameworkElement srcElt, FrameworkElement destElt) {
            FrameworkElement eltParent = GetCommonAncestor(srcElt, destElt);
            GeneralTransform transToParent = srcElt.TransformToAncestor(eltParent);
            GeneralTransform transToDest = eltParent.TransformToDescendant(destElt);

            return (Pt)transToDest.Transform(transToParent.Transform(pt));
        }
        public static Rct GetBounds(FrameworkElement elt) { return GetBounds(elt, (FrameworkElement)elt.Parent); }
        public static Rct GetBounds(FrameworkElement elt, FrameworkElement parent) {
            if (parent == null)
                return new Rct(0, 0, elt.Width, elt.Height);

            if (elt.ActualHeight == 0 && elt.ActualWidth == 0)
                elt.UpdateLayout();
            GeneralTransform trans = elt.TransformToAncestor(parent);

            Rct result = trans.TransformBounds(new Rect(new Point(0, 0), new Size(elt.ActualWidth, elt.ActualHeight)));

            return result;
        }
        static public Stroq PolygonOutline(Polygon p) {
            List<Pt> pts = new List<Pt>();
            Mat rmat = (Mat)p.RenderTransform.Value;
            foreach (Point pt in p.Points)
                pts.Add(rmat * (Pt)pt);
            return new Stroq(pts);
        }
        public static PathGeometry Geometry(IEnumerable<Pt> pts) {
            PathGeometry geom = new PathGeometry();
            PathFigure pf = new PathFigure();
            pf.StartPoint = pts.First();
            geom.Figures = new PathFigureCollection(new PathFigure[] { pf });
            List<Point> points = new List<Point>();
            for (int i = 1; i < pts.Count(); i++)
                points.Add(pts.ElementAt(i));
            PolyLineSegment ps = new PolyLineSegment(points.ToArray(), true);
            pf.Segments.Add(ps);

            return geom;
        }
        public static bool GeometryContains(Geometry hull, Stroq s) {
            for (int i = 0; i < s.Count(); i++)
                if (!hull.FillContains(s[i]))
                    return false;
            return true;
        }
        static public LnSeg LineSeg(Line l) {
            Mat rmat = (Mat)l.RenderTransform.Value;
            Pt p1 = new Pt(l.X1, l.Y1);
            Pt p2 = new Pt(l.X2, l.Y2);
            return new LnSeg(rmat * p1, rmat * p2);
        }
        public static Pt[] GetOutline(FrameworkElement elt, FrameworkElement parent) {
            if (parent == null)
                return new Pt[0];

            elt.UpdateLayout();
            GeneralTransform trans = new MatrixTransform(Mat.Identity);

            try {
                trans = elt.TransformToAncestor(parent);
            }
            catch (System.InvalidOperationException ex) {
            }

            Pt[] bounds = new Pt[] { new Pt(), new Pt(elt.ActualWidth, 0), new Pt(elt.ActualWidth, elt.ActualHeight), new Pt(0, elt.ActualHeight) };
            for (int i = 0; i < bounds.Length; i++)
                bounds[i] = trans.Transform(bounds[i]);
            return bounds;
        }

        static Hashtable mappings = null;
        static Hashtable ShiftMappings = null;
        public static char GetCharFromKey(KeyEventArgs key) {
            if (mappings == null) {
                mappings = new Hashtable();
                ShiftMappings = new Hashtable();

                mappings.Add(Key.Tab, '\t');

                mappings.Add(Key.A, 'a');
                mappings.Add(Key.B, 'b');
                mappings.Add(Key.C, 'c');
                mappings.Add(Key.D, 'd');
                mappings.Add(Key.E, 'e');
                mappings.Add(Key.F, 'f');
                mappings.Add(Key.G, 'g');
                mappings.Add(Key.H, 'h');
                mappings.Add(Key.I, 'i');
                mappings.Add(Key.J, 'j');
                mappings.Add(Key.K, 'k');
                mappings.Add(Key.L, 'l');
                mappings.Add(Key.M, 'm');
                mappings.Add(Key.N, 'n');
                mappings.Add(Key.O, 'o');
                mappings.Add(Key.P, 'p');
                mappings.Add(Key.Q, 'q');
                mappings.Add(Key.R, 'r');
                mappings.Add(Key.S, 's');
                mappings.Add(Key.T, 't');
                mappings.Add(Key.U, 'u');
                mappings.Add(Key.V, 'v');
                mappings.Add(Key.W, 'w');
                mappings.Add(Key.X, 'x');
                mappings.Add(Key.Y, 'y');
                mappings.Add(Key.Z, 'z');



                ShiftMappings.Add(Key.A, 'A');
                ShiftMappings.Add(Key.B, 'B');
                ShiftMappings.Add(Key.C, 'C');
                ShiftMappings.Add(Key.D, 'D');
                ShiftMappings.Add(Key.E, 'E');
                ShiftMappings.Add(Key.F, 'F');
                ShiftMappings.Add(Key.G, 'G');
                ShiftMappings.Add(Key.H, 'H');
                ShiftMappings.Add(Key.I, 'I');
                ShiftMappings.Add(Key.J, 'J');
                ShiftMappings.Add(Key.K, 'K');
                ShiftMappings.Add(Key.L, 'L');
                ShiftMappings.Add(Key.M, 'M');
                ShiftMappings.Add(Key.N, 'N');
                ShiftMappings.Add(Key.O, 'O');
                ShiftMappings.Add(Key.P, 'P');
                ShiftMappings.Add(Key.Q, 'Q');
                ShiftMappings.Add(Key.R, 'R');
                ShiftMappings.Add(Key.S, 'S');
                ShiftMappings.Add(Key.T, 'T');
                ShiftMappings.Add(Key.U, 'U');
                ShiftMappings.Add(Key.V, 'V');
                ShiftMappings.Add(Key.W, 'W');
                ShiftMappings.Add(Key.X, 'X');
                ShiftMappings.Add(Key.Y, 'Y');
                ShiftMappings.Add(Key.Z, 'Z');




                mappings.Add(Key.D0, '0');
                mappings.Add(Key.D1, '1');
                mappings.Add(Key.D2, '2');
                mappings.Add(Key.D3, '3');
                mappings.Add(Key.D4, '4');
                mappings.Add(Key.D5, '5');
                mappings.Add(Key.D6, '6');
                mappings.Add(Key.D7, '7');
                mappings.Add(Key.D8, '8');
                mappings.Add(Key.D9, '9');
                mappings.Add(Key.NumPad0, '0');
                mappings.Add(Key.NumPad1, '1');
                mappings.Add(Key.NumPad2, '2');
                mappings.Add(Key.NumPad3, '3');
                mappings.Add(Key.NumPad4, '4');
                mappings.Add(Key.NumPad5, '5');
                mappings.Add(Key.NumPad6, '6');
                mappings.Add(Key.NumPad7, '7');
                mappings.Add(Key.NumPad8, '8');
                mappings.Add(Key.NumPad9, '9');


                mappings.Add(Key.Oem3, '`');
                ShiftMappings.Add(Key.Oem3, '~');



                ShiftMappings.Add(Key.D1, '!');
                ShiftMappings.Add(Key.D2, '@');
                ShiftMappings.Add(Key.D3, '#');
                ShiftMappings.Add(Key.D4, '$');
                ShiftMappings.Add(Key.D5, '%');
                ShiftMappings.Add(Key.D6, '^');
                ShiftMappings.Add(Key.D7, '&');
                ShiftMappings.Add(Key.D8, '*');
                ShiftMappings.Add(Key.D9, '(');
                ShiftMappings.Add(Key.D0, ')');
                ShiftMappings.Add(Key.OemMinus, '_');
                ShiftMappings.Add(Key.OemPlus, '+');
                mappings.Add(Key.OemPlus, '=');
                mappings.Add(Key.OemMinus, '-');

                mappings.Add(Key.Add, '+');
                mappings.Add(Key.Subtract, '-');



                /*            ShiftMappings.Add(Key.Oem4, '{');
                            ShiftMappings.Add(Key.Oem6, '}');
                            mappings.Add(Key.Oem4, '[');
                            mappings.Add(Key.Oem6, ']');*/


                ShiftMappings.Add(Key.OemOpenBrackets, '{');
                ShiftMappings.Add(Key.OemCloseBrackets, '}');
                mappings.Add(Key.OemOpenBrackets, '[');
                mappings.Add(Key.OemCloseBrackets, ']');



                mappings.Add(Key.Oem5, '\\');
                ShiftMappings.Add(Key.Oem5, '|');

                mappings.Add(Key.OemBackslash, '\\');
                ShiftMappings.Add(Key.OemBackslash, '|');

                mappings.Add(Key.Oem1, ';');
                ShiftMappings.Add(Key.Oem1, ':');

                //mappings.Add(Key.OemSemicolon, ';');
                //ShiftMappings.Add(Key.OemSemicolon, ':');

                //ShiftMappings.Add(Key.Oem7, '\"');
                //mappings.Add(Key.Oem7, '\'');
                ShiftMappings.Add(Key.OemQuotes, '\"');
                mappings.Add(Key.OemQuotes, '\'');


                mappings.Add(Key.OemComma, ',');
                ShiftMappings.Add(Key.OemComma, '<');

                mappings.Add(Key.OemPeriod, '.');
                ShiftMappings.Add(Key.OemPeriod, '>');

                mappings.Add(Key.Oem2, '/');
                ShiftMappings.Add(Key.Oem2, '?');
            }

            try {
                if (key.KeyboardDevice.IsKeyDown(Key.LeftShift) || key.KeyboardDevice.IsKeyDown(Key.RightShift)) {
                    return (char)ShiftMappings[key.Key];
                }
                else {
                    return (char)mappings[key.Key];
                }
            }
            catch {
                return '\0';
            }

        }
        static public BitmapSource MakeBitmapSourceFromBitmap(System.Drawing.Bitmap b) {
            System.Drawing.Imaging.BitmapData bd = b.LockBits(new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), b.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, b.PixelFormat);// System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try {
                BitmapSource bs = BitmapSource.Create(bd.Width, bd.Height, b.HorizontalResolution, b.HorizontalResolution, System.Windows.Media.PixelFormats.Pbgra32, null,
                    bd.Scan0, bd.Width * bd.Height * 4, bd.Stride);
                return bs;
            }
            catch (Exception e) {
                return null;
            }
            finally {
                b.UnlockBits(bd);
            }
        }
        static public Image ConvertBitmapToWPFImage(double initialWidth, System.Drawing.Bitmap b) {
            Image img = new Image();
            img.VerticalAlignment = VerticalAlignment.Top;
            img.Width = b.Width;
            img.Height = b.Height;
            img.Source = MakeBitmapSourceFromBitmap(b);
            if (initialWidth > 0)
                img.RenderTransform = new ScaleTransform(initialWidth / img.Width, initialWidth / img.Width);
            return img;
        }
    }
}
