using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;
using starPadSDK.UnicodeNs;
using starPadSDK.MathExpr;
using starPadSDK.Geom;
using starPadSDK.MathExpr.ExprWPF.Boxes;
using System.Windows.Media;

namespace starPadSDK.MathExpr.ExprWPF {
    // TJC: New class to support getting rectangle bounds for all our subboxes
    public class BoxRct
    {
        public Box box;
        public Rct rect;
        public BoxRct(Box b, Rct r)
        {
            box = b;
            r = rect;
        }
    }
    public class EWPF : GenericOutput<Box> {
        public static float Opacity = 1.0f; //TJC: expose opacity

        /// <summary>
        /// Draw a typeset Expr. Note that the y component of where is the math axis, midway between the baseline and the x-height.
        /// </summary>
        /// <param name="fsize">Em-height of font to use. (This is essentially the font size.)</param>
        /// <param name="where">Note that the y component of this is the math axis, midway between the baseline and the x-height.</param>
        /// <param name="showinvisibles">Whether to draw parentheses indicating function application slightly darker</param>
        /// <returns>nominal bounding box of resulting typeset as drawn</returns>
        // TJC:
        static public BoxRct Draw(Expr e, double fsize, DrawingContext dc, Color color, Pt where, bool showinvisibles) {
            return Draw(e, fsize, dc, color, (Rct r) => where, showinvisibles);
        }
        /// <summary>
        /// Draw a typeset Expr. Note that where should be the top left of the bounding box of the typeset expression.
        /// </summary>
        /// <param name="fsize">Em-height of font to use. (This is essentially the font size.)</param>
        /// <param name="where">This should be the top left of the bounding box of the typeset expression.</param>
        /// <param name="showinvisibles">Whether to draw parentheses indicating function application slightly darker</param>
        /// <returns>nominal bounding box of resulting typeset as drawn</returns>
        // TJC:
        static public BoxRct DrawTop(Expr e, double fsize, DrawingContext dc, Color color, Pt where, bool showinvisibles) {
            return Draw(e, fsize, dc, color, (Rct r) => where+r.Top*Vec.Up, showinvisibles);
        }
#if CSharpEverHasRealLambdaFunctions
        /// <summary>
        /// Draw a typeset Expr. Note that where should be the top left of the bounding box of the typeset expression. Additionally returns where the math
        /// axis turned out to be (for drawing additional math next to it).
        /// </summary>
        /// <param name="fsize">Em-height of font to use. (This is essentially the font size.)</param>
        /// <param name="where">This should be the top left of the bounding box of the typeset expression.</param>
        /// <param name="showinvisibles">Whether to draw parentheses indicating function application slightly darker</param>
        /// <returns>nominal bounding box of resulting typeset as drawn</returns>
        static public Rct DrawTop(Expr e, double fsize, DrawingContext dc, Color color, Pt where, out double mathaxis, bool showinvisibles) {
            return Draw(e, fsize, dc, color, (Rct r) => { Pt p = where + r.Top*Vec.Up; mathaxis = p.Y; return p; }, showinvisibles);
        }
#endif
        /// <summary>
        /// Draw a typeset Expr. Note that where should be where the baseline of the base font should wind up.
        /// </summary>
        /// <param name="fsize">Em-height of font to use. (This is essentially the font size.)</param>
        /// <param name="where">This should be where the baseline of the base font should be, at the left edge of the math.</param>
        /// <param name="showinvisibles">Whether to draw parentheses indicating function application slightly darker</param>
        /// <returns>nominal bounding box of resulting typeset as drawn</returns>
        // TJC:
        static public BoxRct DrawBaseline(Expr e, double fsize, DrawingContext dc, Color color, Pt where, bool showinvisibles) {
            return Draw(e, fsize, dc, color, (Rct r, EDrawingContext edc) => where + edc.Midpt*Vec.Up, showinvisibles);
        }
#if CSharpEverHasRealLambdaFunctions
        /// <summary>
        /// Draw a typeset Expr. Note that where should be where the baseline of the base font should wind up. Additionally returns where the math
        /// axis turned out to be (for drawing additional math next to it).
        /// </summary>
        /// <param name="fsize">Em-height of font to use. (This is essentially the font size.)</param>
        /// <param name="where">This should be where the baseline of the base font should be, at the left edge of the math.</param>
        /// <param name="showinvisibles">Whether to draw parentheses indicating function application slightly darker</param>
        /// <returns>nominal bounding box of resulting typeset as drawn</returns>
        static public Rct DrawBaseline(Expr e, double fsize, DrawingContext dc, Color color, Pt where, out double mathaxis, bool showinvisibles) {
            Rct nomr = Draw(e, fsize, dc, color, (Rct r, EDrawingContext edc) => { Pt p = where + edc.Midpt*Vec.Up; mathaxis = p.Y; return p; }, showinvisibles);
        }
#endif
        /// <summary>
        /// Note that the y component of the point where returns is the math axis, midway between the baseline and the x-height.
        /// </summary>
        /// <param name="fsize">Em-height of font to use. (This is essentially the font size.)</param>
        /// <param name="where">Note that the y component of the point this returns is the math axis, midway between the baseline and the x-height.</param>
        /// <param name="showinvisibles">Whether to draw parentheses indicating function application slightly darker</param>
        /// <returns>nominal bounding box of resulting typeset as drawn</returns>
        // TJC:
        static public BoxRct Draw(Expr e, double fsize, DrawingContext dc, Color color, Func<Rct, Pt> where, bool showinvisibles) {
            return Draw(e, fsize, dc, color, (Rct r, EDrawingContext edc) => where(r), showinvisibles);
        }
        /// <summary>
        /// Note that the y component of the point where returns is the math axis, midway between the baseline and the x-height.
        /// </summary>
        /// <param name="fsize">Em-height of font to use. (This is essentially the font size.)</param>
        /// <param name="where">Note that the y component of the point this returns is the math axis, midway between the baseline and the x-height.</param>
        /// <param name="showinvisibles">Whether to draw parentheses indicating function application slightly darker</param>
        /// <returns>nominal bounding box of resulting typeset as drawn</returns>
        // TJC:
        static public BoxRct Draw(Expr e, double fsize, DrawingContext dc, Color color, Func<Rct, EDrawingContext, Pt> where, bool showinvisibles) {
            EDrawingContext edc = new EDrawingContext(fsize, dc, color);
            EWPF us = new EWPF(showinvisibles);
            edc.Brush.Opacity = Opacity; // TJC: support opacity change
            Box b = us.Compose(e);
            b.Measure(edc);
            Pt loc = where(b.nombbox, edc);
            b.Draw(edc, loc);
            // TJC:
            return new BoxRct(b, b.nombbox + (Vec)loc);
        }
        // TJC: special accessor to draw single box
        static public BoxRct Draw(Box b, double fsize, DrawingContext dc, Color color)
        {
            int xOffset = b.Expr.Annotations.ContainsKey("Force Parentheses") ? -21 : 0;
            int yOffset = b.Expr.Annotations.ContainsKey("Force Parentheses") ? 6 : 6;
            Pt where;
            if (b is DelimitedBox)
            {
                where = new Pt((b as DelimitedBox).Contents.NomBBoxRefOrigin.Left + xOffset, (b as DelimitedBox).Contents.NomBBoxRefOrigin.Center.Y + yOffset);
            }
            else
            {
                if (b is HBox && (b as HBox).Boxes[0] is DelimitedBox)
                {
                    where = new Pt(b.NomBBoxRefOrigin.Left + xOffset, b.NomBBoxRefOrigin.Center.Y);
                }
                else if (b is HBox && (b as HBox).Boxes[0] is CharBox)
                {
                    // special handling for complement
                    if (((b as HBox).Boxes[0] as CharBox).C == '¬') // || (((b as HBox).Boxes[0] as CharBox).C == '∉')))
                    {
                        where = new Pt(b.NomBBoxRefOrigin.Left, b.NomBBoxRefOrigin.Center.Y + yOffset);
                    }
                    else
                    {
                        where = new Pt(b.NomBBoxRefOrigin.Left, b.NomBBoxRefOrigin.Center.Y + yOffset);
                    }
                }
                else
                {
                    where = new Pt(b.NomBBoxRefOrigin.Left, b.NomBBoxRefOrigin.Center.Y + yOffset);
                }
            }
            return Draw(b.Expr, fsize, dc, color, (Rct r) => where, true);
        }
        static public Box Measure(Expr e, double fsize, DrawingContext dc) {
            Box b = new EWPF(false).Compose(e);
            b.Measure(new EDrawingContext(fsize, dc, Colors.Black));
            return b;
        }

        /// <summary>
        /// Given an Expr, this returns a typeset Box which you can then Measure() and Draw() to render to the screen. This is used only if you want finer
        /// control over positioning than the EWPF.Draw() routines give you.
        /// </summary>
        public Box Compose(Expr e) {
            Box b = Translate(e);
            FixupAll(b);
            return b;
        }
        
        static public Geometry ComputeGeometry(Expr e, double fsize, out Rct nombbox) {
            EDrawingContext edc = new EDrawingContext(fsize, null, Colors.Black);
            EWPF us = new EWPF(false);
            Box b = us.Compose(e);
            b.Measure(edc);
            nombbox = b.nombbox;
            return b.ComputeGeometry(edc);
        }

        /// <summary>
        /// This represents a link in the chain from the root. The link is from the Box B to some other box specified by subclasses.
        /// </summary>
        public abstract class BoxLink {
            private Box _b; public Box B { get { return _b; } }
            public BoxLink(Box b) { _b = b; }
            abstract public Box Target { get; set; }
        }
        /// <summary>
        /// This represents a link from box B (which is an HBox) to the Ith box of that HBox.
        /// </summary>
        public class HBoxLink : BoxLink {
            public HBox HB { get { return (HBox)B; } }
            private int _i; public int I { get { return _i; } }
            public HBoxLink(HBox hb, int i) : base(hb) { _i = i; }
            public override Box Target { get { return HB.Boxes[I]; } set { HB.Boxes[I] = value; } }
        }
        /// <summary>
        /// This is a link from a delimited box to the left delimiter, contents, or right delimiter.
        /// </summary>
        public class DelimitedBoxLink : BoxLink {
            public DelimitedBox DB { get { return (DelimitedBox)B; } }
            public enum Which { Left, Contents, Right };
            private Which _piece; public Which Piece { get { return _piece; } }
            public DelimitedBoxLink(DelimitedBox db, Which piece) : base(db) { _piece = piece; }
            public override Box Target {
                get {
                    switch(Piece) {
                        case Which.Left: return DB.Left;
                        case Which.Contents: return DB.Contents;
                        case Which.Right: return DB.Right;
                    }
                    throw new Exception("Code broken in DelimitedBoxLink.Target get! _piece is an invalid value");
                }
                set {
                    switch(Piece) {
                        case Which.Left: DB.Left = value; break;
                        case Which.Contents: DB.Contents = new HBox(value); break;
                        case Which.Right: DB.Right = value; break;
                    }
                }
            }
        }
        /// <summary>
        /// This represents a path from the root down the hierarchy to a specific box.
        /// </summary>
        public class BoxPath {
            public List<BoxLink> P;
            public BoxLink Final { get { return P[P.Count - 1]; } }
            public BoxPath(IEnumerable<BoxLink> p) { P = new List<BoxLink>(p); }
            public BoxPath() { P = new List<BoxLink>(); }
            public BoxPath Clone() { return new BoxPath(P); }
        }
        /// <summary>
        /// This is a kind of box which can modify the adjacent leaves of the box tree.
        /// </summary>
        public interface LeafModifierBox {
            void ModifyLeft(BoxPath left, BoxPath self);
            void ModifyRight(BoxPath self, BoxPath right);
        }
        private class FixupTraverser {
            /// <summary>
            /// The path to the previous node visited.
            /// </summary>
            private BoxPath A = null;
            /// <summary>
            /// The path to the node being visited.
            /// </summary>
            private BoxPath B = new BoxPath();
            public enum TraverseType { TType, Spacing, Modifiers };
            private TraverseType _traverseType;
            public FixupTraverser(TraverseType tt) {
                switch(tt) {
                    case TraverseType.Modifiers:
                        ConsiderPair = ConsiderPairModifiers;
                        break;
                    case TraverseType.Spacing:
                        ConsiderPair = ConsiderPairSpacing;
                        break;
                    case TraverseType.TType:
                        ConsiderPair = ConsiderPairTType;
                        break;
                }
                _traverseType = tt;
            }
            private Box _toplevel = null;
            private Dictionary<List<Box>, SortedDictionary<int, Box>> _listBoxInserts = new Dictionary<List<Box>,SortedDictionary<int,Box>>();
            public void TraverseTop(Box box) {
                _toplevel = box;
                Traverse(box);
                // then do the end boundary case
                A = B; // Probably unnecessary
                B = null;
                ConsiderPair();

                if(_traverseType == TraverseType.Spacing) {
                    foreach(KeyValuePair<List<Box>,SortedDictionary<int,Box>> kvp in _listBoxInserts) {
                        int offset = 0;
                        foreach(KeyValuePair<int, Box> kvp2 in kvp.Value) {
                            kvp.Key.Insert(kvp2.Key + offset, kvp2.Value);
                            offset++;
                        }
                    }
                }
            }
            private void Traverse(Box box) {
                typeof(FixupTraverser).InvokeMember("_Traverse", BindingFlags.InvokeMethod|BindingFlags.NonPublic|BindingFlags.Instance,
                    null, this, new object[] { box });
            }
            private void _Traverse(HBox hb) {
                BoxPath hbpath = B;
                for(int i = 0; i < hb.Boxes.Count; i++) {
                    B = hbpath.Clone();
                    B.P.Add(new HBoxLink(hb, i));
                    Traverse(hb.Boxes[i]);
                }
            }
            private void _Traverse(DelimitedBox db) {
                // FIXME: this is wrong according to TeXbook: whole delimitedbox should become one atom of type Inner. But that only applies to \left \right
                // formulations; not clear should apply to all uses of parens, and right now there may even be issues with function call spacing. Need to
                // look at this more.
                BoxPath dbpath = B;

                B = dbpath.Clone();
                B.P.Add(new DelimitedBoxLink(db, DelimitedBoxLink.Which.Left));
                Traverse(db.Left);

                B = dbpath.Clone();
                B.P.Add(new DelimitedBoxLink(db, DelimitedBoxLink.Which.Contents));
                Traverse(db.Contents);

                B = dbpath.Clone();
                B.P.Add(new DelimitedBoxLink(db, DelimitedBoxLink.Which.Right));
                Traverse(db.Right);
            }
            private void _Traverse(AtomBox ab) {
                EWPF.Fixup(ab.Sub, _traverseType);
                if(!(ab.Nucleus is CharBox || ab.Nucleus is StringBox)) EWPF.Fixup(ab.Nucleus, _traverseType);
                EWPF.Fixup(ab.Sup, _traverseType);

                Leaf();
            }
            private void _Traverse(VBox vb) {
                foreach(Box b in vb.Boxes) EWPF.Fixup(b, _traverseType);

                Leaf();
            }
            private void _Traverse(AlignmentBox ab) {
                foreach(Box b in ab.Boxes) EWPF.Fixup(b, _traverseType);

                Leaf();
            }
            private void _Traverse(RootBox rb) {
                EWPF.Fixup(rb.Index, _traverseType);
                EWPF.Fixup(rb.Radicand, _traverseType);

                Leaf();
            }
            private void _Traverse(AtopBox ab) {
                EWPF.Fixup(ab.Top, _traverseType);
                EWPF.Fixup(ab.Bot, _traverseType);

                Leaf();
            }
            /* don't consider skips to be leaves. that makes our TeX imitation correspond to what TeX does, and is probably right for user-provided pair
             * things like highlighting syntax errors */
            private void _Traverse(MuSkipBox msb) {
            }
            private void _Traverse(DTMuSkipBox dtmsb) {
            }
            private void _Traverse(Box b) {
                Leaf();
            }

            private void Leaf() {
                ConsiderPair();
                A = B;
            }
            private Syntax.TType SetTType(Box b, Syntax.TType tt) {
                Trace.Assert(b != null);
                if(b is CharBox) ((CharBox)b).TeXType = tt;
                else if(b is StringBox) ((StringBox)b).TeXType = tt;
                else if(b is AtomBox) ((AtomBox)b).TeXType = tt;
                else Trace.Assert(false, "unknown box type to set TType on");
                return tt;
            }
            private delegate void ConsiderPairFunc();
            private ConsiderPairFunc ConsiderPair;
            private void ConsiderPairTType() {
                Box a = A == null ? null : A.P.Count == 0 ? _toplevel : A.Final.Target;
                Box b = B == null ? null : B.P.Count == 0 ? _toplevel : B.Final.Target;

                /* Do TeX atom type rules from p 170 and appendix G */
                Syntax.TType atype = GetTType(a), btype = GetTType(b);

                /* appendix G, p 442- */
                /* rule 1, 2, 3, 4: nothing for us */
                /* rule 5 */
                if(btype == Syntax.TType.Op) {
                    if(a == null || atype == Syntax.TType.Op || atype == Syntax.TType.LargeOp || atype == Syntax.TType.Rel || atype == Syntax.TType.Open
                        || atype == Syntax.TType.Punct) {
                        btype = SetTType(b, Syntax.TType.Ord);
                        goto rule14;
                    } else goto rule17;
                }
                /* rule 6 */
                if((btype == Syntax.TType.Rel || btype == Syntax.TType.Close || btype == Syntax.TType.Punct) && atype == Syntax.TType.Op) {
                    atype = SetTType(a, Syntax.TType.Ord);
                    goto rule17;
                }
                /* rule 7 */
                if(btype == Syntax.TType.Open || btype == Syntax.TType.Inner) goto rule17;
            /* rule 8: (Vcent) done in type inference above */
            /* rule 9: change Over to Ord and go to 17: we have no Over use; if we do, just consider it Ord to start with above */
            /* rule 10: change Under to Ord and go to 17: we have no Under use; if we do, just consider it Ord to start with above */
            /* rule 11: (Rad) done in type inference above */
            /* rule 12: change Acc to Ord and go to 17: we have no Acc use; if we do, just consider it Ord to start with above */
            /* rule 13, 13a (Op) */
                /* limits handling done in AtomBox */
            // FIXME: should make operators like integral and summation slightly larger in display style according to TeXbook
            /* rule 14: (Ord) ligatures and kerns here too; we ignore for now */
            rule14:
                // FIXME should do ligatures and kerns?
                if(btype == Syntax.TType.Ord) goto rule17;
            /* rule 15, 15a-e: generalized fraction (before becoming Inner): nothing for us (should have been done in AtopBox) */
            // Not clear if things matching rule15 should fall through, as makes little sense to change type to Inner right before changing to Ord
            /* rule 16: change current type to Ord: folded in to previous jumps to rule16 including changing to be jump to rule 17 */
            /* rule 17: various things which are either done elsewhere or which we don't handle */
            rule17:
                /* rule 18, 18a-f: done in AtomBox */
                ;
                /* rule (unnumbered, between 18f and 19) */
                if(atype == Syntax.TType.Op && btype == Syntax.TType.None) SetTType(a, Syntax.TType.Ord);
            }
            private void ConsiderPairModifiers() {
                if(A == null || B == null) return;
                Box a = A.Final.Target;
                Box b = B.Final.Target;
                /* Run any outside modifications, such as undersquiggling for syntax errors */
                if(a is LeafModifierBox) ((LeafModifierBox)a).ModifyRight(A, B);
                if(b is LeafModifierBox) ((LeafModifierBox)b).ModifyLeft(A, B);
            }
            /// <summary>
            /// This table is taken straight from the TeXBook, chapter 18, p 170. "5" is an error (represents
            /// "*" in the TexBook). 0=none, 1=thin, 2=med, 3=thick; negative numbers mean apply the value
            /// corresp to their negative, but only in display and text styles, not script or scriptscript.
            /// Indexing is IBSpacing[left, right].
            /// </summary>
            private static readonly int[,] IBSpacing = new int[8, 8] {
                /* Ord  */ { 0,  1, -2, -3,  0,  0,  0, -1},
                /* Op   */ { 1,  1,  5, -3,  0,  0,  0, -1},
                /* Bin  */ {-2, -2,  5,  5, -2,  5,  5, -2},
                /* Rel  */ {-3, -3,  5,  0, -3,  0,  0, -3},
                /* Open */ { 0,  0,  5,  0,  0,  0,  0,  0},
                /* Close */{ 0,  1, -2, -3,  0,  0,  0, -1},
                /* Punct */{-1, -1,  5, -1, -1, -1, -1, -1},
                /* Inner */{-1,  1, -2, -3, -1,  0, -1, -1}
            };
            private void ConsiderPairSpacing() {
                if(A == null || B == null) return;
                Box a = A.Final.Target;
                Box b = B.Final.Target;

                /* Do TeX atom type and spacing fixup rules from p 170 and appendix G */
                Syntax.TType atype = GetTType(a), btype = GetTType(b);
                /* rule 20 */
                int spacing = IBSpacing[(int)atype, (int)btype];
                Trace.Assert(spacing != 5);
                Box space = null;
                switch(spacing) {
                    case 0:
                        // all the code for this case is *not* part of TeX's rule 20. It comes from p 169 and applies specifically to factorial
                        // operators, so won't be correct for other possible uses of '!'
                        if(atype == Syntax.TType.Ord && a is CharBox && ((CharBox)a).C == '!') {
                            CharBox cb = b as CharBox;
                            StringBox sb = b as StringBox;
                            AtomBox ab = b as AtomBox;
                            if((btype == Syntax.TType.Ord && ((cb != null && (Char.IsLetter(cb.C) || Char.IsDigit(cb.C)))
                                                           || (sb != null && (Char.IsLetter(sb.S, 0) || Char.IsDigit(sb.S, 0)))))
                                || btype == Syntax.TType.Open) {
                                space = ThinSkip();
                            } else if(btype == Syntax.TType.Ord && ab != null) {
                                cb = ab.Nucleus as CharBox;
                                sb = ab.Nucleus as StringBox;
                                if((cb != null && (Char.IsLetter(cb.C) || Char.IsDigit(cb.C)))
                                   || (sb != null && (Char.IsLetter(sb.S, 0) || Char.IsDigit(sb.S, 0)))) {
                                    space = ThinSkip();
                                }
                            }
                        }
                        break;
                    case 1:
                        space = ThinSkip();
                        break;
                    case 2:
                        space = MedSkip();
                        break;
                    case 3:
                        space = ThickSkip();
                        break;
                    case -1:
                        space = DTThinSkip();
                        break;
                    case -2:
                        space = DTMedSkip();
                        break;
                    case -3:
                        space = DTThickSkip();
                        break;
                }

                /* apply the spacing */
                if(space != null) {
                    /* Find the lowest common ancestor */
                    int found = -1;
                    for(int i = 0; i < A.P.Count && i < B.P.Count; i++) {
                        if(A.P[i].Target != B.P[i].Target) {
                            found = i;
                            break;
                        }
                    }
                    Trace.Assert(found != -1, "adjacent boxes are the same?");
                    Trace.Assert(A.P[found].B == B.P[found].B);
                    Trace.Assert(A.P[found].GetType() == B.P[found].GetType());
                    List<Box> boxes;
                    int ix;
                    if(A.P[found] is HBoxLink) {
                        HBoxLink hba = (HBoxLink)A.P[found];
                        HBoxLink hbb = (HBoxLink)B.P[found];
                        Trace.Assert(hba.I < hbb.I);
                        for(int i = hba.I+1; i < hbb.I; i++) Trace.Assert(hba.HB.Boxes[i] is MuSkipBox || hba.HB.Boxes[i] is DTMuSkipBox);
                        boxes = hba.HB.Boxes;
                        ix = hbb.I;
                    } else {
                        DelimitedBoxLink dba = (DelimitedBoxLink)A.P[found];
                        DelimitedBoxLink dbb = (DelimitedBoxLink)B.P[found];
                        boxes = dba.DB.Contents.Boxes;
                        if(dba.Piece == DelimitedBoxLink.Which.Left) {
                            Trace.Assert(dbb.Piece != DelimitedBoxLink.Which.Left);
                            ix = 0;
                        } else {
                            Trace.Assert(dba.Piece == DelimitedBoxLink.Which.Contents);
                            Trace.Assert(dbb.Piece == DelimitedBoxLink.Which.Right);
                            ix = boxes.Count;
                        }
                    }
                    SortedDictionary<int,Box> inserts;
                    if(!_listBoxInserts.TryGetValue(boxes, out inserts)) {
                        inserts = new SortedDictionary<int,Box>();
                        _listBoxInserts[boxes] = inserts;
                    }
                    inserts.Add(ix, space);
                }
            }
        }
        private static Syntax.TType GetTType(Box b) {
            if(b == null) return Syntax.TType.None;
            else if(b is CharBox) return ((CharBox)b).TeXType;
            else if(b is StringBox) return ((StringBox)b).TeXType;
            else if(b is AtomBox) return ((AtomBox)b).TeXType;
            else if(b is VBox) return Syntax.TType.Ord; // ??? factors in rule 8 if Vcent (or should it be Inner?)
            else if(b is AlignmentBox) return Syntax.TType.Ord; // ??? factors in rule 8 if Vcent (or should it be Inner?)
            else if(b is RootBox) return Syntax.TType.Ord; // factors in rule 11: Rad -> Ord */
            else if(b is AtopBox) return Syntax.TType.Inner;
            else return Syntax.TType.Ord; // FIXME? Trace.Assert(false, "unknown box B type to consider");
        }
        private static void Fixup(Box box, FixupTraverser.TraverseType tt) {
            (new FixupTraverser(tt)).TraverseTop(box);
        }
        private static void FixupAll(Box box) {
            Fixup(box, FixupTraverser.TraverseType.TType);
            Fixup(box, FixupTraverser.TraverseType.Spacing);
            Fixup(box, FixupTraverser.TraverseType.Modifiers);
        }

        public EWPF(bool showinvisibles) : base(showinvisibles) { }

        internal static Box ThinSkip() { return new MuSkipBox(3); }
        internal static Box MedSkip() { return new MuSkipBox(4); }
        internal static Box ThickSkip() { return new MuSkipBox(5); }
        internal static Box DTThinSkip() { return new DTMuSkipBox(3); }
        internal static Box DTMedSkip() { return new DTMuSkipBox(4); }
        internal static Box DTThickSkip() { return new DTMuSkipBox(5); }
        internal static Box NegThinSkip() { return new MuSkipBox(-3); }

        protected override Box __Translate(NullExpr e) {
            return new NullBox();
        }
        protected override Box __Translate(ErrorMsgExpr e) {
            return new StringBox(e, e.Msg, Syntax.TType.Ord);
        }

        protected override Box __TranslateOperator(Expr expr, object exprix, Syntax.WOrC op, Syntax.TType type) {
            if(op.Word == null) {
                return new CharBox(expr, exprix, op.Character, type);
            } else {
                Trace.Assert(exprix == null);
                return new StringBox(expr, op.Word, type);
            }
        }

        protected static readonly string[] limitwordops = new string[] { "lim", /*"lim\\,sup", "lim\\,inf",*/ "max", "min", "sup", "inf", "det", "Pr", "gcd" };
        static EWPF() {
            Array.Sort(limitwordops);
        }
        protected override Box __TranslateWord(Expr expr, string op, Syntax.TType type) {
            /* This is really a hack :-( */
            StringBox sb = new StringBox(expr, op, type);
            if(type == Syntax.TType.LargeOp && Array.BinarySearch(limitwordops, op) < 0) {
                return new AtomBox(null, sb, null, null, Syntax.TType.LargeOp, AtomBox.LimitsType.NoLimits);
            } else return sb;
        }

        protected override Box __TranslateDelims(Expr e, bool emph, object lexprix, char l, Box t, object rexprix, char r) {
            return new DelimitedBox(e, emph, lexprix, l, t, rexprix, r);
        }

        protected override Box __WrapTranslatedExpr(Expr expr, List<Box> lt) {
            return new HBox(expr, lt);
        }

        protected override Box __TranslateVerticalFraction(Expr e, Expr divlineexpr, Box num, Box den) {
            return new AtopBox(e, divlineexpr, num, den, true);
        }

        protected override Box __TranslateBigOp(Expr wholeexpr, Expr opexpr, char op, Box lowerlimit, Box upperlimit, Box contents) {
            DelimBox db;
            if(op == Unicode.I.INTEGRAL) db = new IntegralDelimBox(opexpr, Unicode.I.INTEGRAL);
            else db = new DelimBox(opexpr, op);
            AtomBox.LimitsType lt = op == Unicode.I.INTEGRAL || op == Unicode.N.N_ARY_SUMMATION ? AtomBox.LimitsType.Limits : AtomBox.LimitsType.DisplayLimits;
            return new DelimitedBox(wholeexpr, db, new AtomBox(null, db, lowerlimit, upperlimit, Syntax.TType.LargeOp, lt), contents, null, new NullBox());
        }

        protected override Box __TranslateFunctionApplication(Expr e, Box fn, Box args) {
            return new HBox(e, fn, args);
        }

        protected override Box __TranslateOperatorApplication(Expr e, Box op, Box args) {
            return new HBox(e, op, args);
        }

        protected override Box __AddSuperscript(Expr e, Box nuc, Box sup) {
            // XXX Having to edit existing boxes this way here means that the box system is almost certainly misdesigned.
            List<Box> lb = new List<Box>(new Box[] { nuc });
            /* Find parent of last real atomic thing (which can have a superscript) */
            List<Box> pb = lb;
            while(pb[pb.Count - 1] is HBox) {
                pb = ((HBox)pb[pb.Count - 1]).Boxes;
            }
            /* the last real thing itself */
            Box b = pb[pb.Count - 1];
            AtomBox ab = b as AtomBox;
            if(ab != null) {
                Trace.Assert(ab.Sup is NullBox); // shouldn't happen given 1st if of our caller
                ab.Sup = sup;
            } else {
                Box bb = new AtomBox(null, b, new NullBox(), sup, GetTType(b));
                pb[pb.Count - 1] = bb;
            }
            return new HBox(e, lb);
        }

        protected override Box __AddSubscript(Expr e, Box nuc, Box sub) {
            if(nuc is StringBox || nuc is CharBox) return new AtomBox(null, nuc, sub, new NullBox(), GetTType(nuc)); // for 'log' function's base, really
            else {
                // XXX Having to edit existing boxes this way here means that the box system is almost certainly misdesigned.
                List<Box> lb = new List<Box>(new Box[] { nuc });
                /* Find parent of last real atomic thing */
                List<Box> pb = lb;
                while(pb[pb.Count - 1] is HBox) {
                    pb = ((HBox)pb[pb.Count - 1]).Boxes;
                }
                /* the last real thing itself */
                Box b = pb[pb.Count - 1];
                AtomBox ab = b as AtomBox;
                if(ab != null) {
                    Trace.Assert(ab.Sup is NullBox && ab.Sub is NullBox); // shouldn't happen given 1st if of our caller
                    ab.Sub = sub;
                } else {
                    Box bb = new AtomBox(null, b, sub, new NullBox(), GetTType(b));
                    pb[pb.Count - 1] = bb;
                }
                return new HBox(e, lb);
            }
        }

        protected override Box __TranslateRadical(Expr e, Box radicand, Box index) {
            return index == null ? new RootBox(e, radicand) : new RootBox(e, radicand, index);
        }

        protected override Box __TranslateIntegralInternals(Box integrand, Box dxthing) {
            return new HBox(integrand, ThinSkip(), dxthing);
        }

        protected override Box __Translate(DoubleNumber n) {
            if(Double.IsNaN(n.Num)) {
                return new StringBox(n, "NaN", Syntax.TType.Ord);
            } else if(Double.IsNegativeInfinity(n.Num)) {
                return new HBox(n, new CharBox(Unicode.M.MINUS_SIGN, Syntax.TType.Op), new CharBox(Unicode.I.INFINITY, Syntax.TType.Ord));
            } else if(Double.IsPositiveInfinity(n.Num)) {
                return new CharBox(n, Unicode.I.INFINITY, Syntax.TType.Ord);
            } else {
                string num = n.Num.ToString("R");

                // TJC: limit to 3 decimal points for display

                if (num.Contains("."))
                {
                    int idx = num.IndexOf(".");

                    int length = num.ToCharArray().Length - idx;
                    if (length > 2)
                    {
                        num = num.Substring(0, idx + 2) + "...";    
                    }                    
                }

                if(num[0] == '-') num = Unicode.M.MINUS_SIGN.ToString() + num.Substring(1, num.Length-1);
                int e = num.IndexOfAny(new char[] { 'e', 'E' });
                string significand, exponent;
                if(e == -1) {
                    significand = num;
                    exponent = null;
                } else {
                    significand = num.Substring(0, e);
                    exponent = num.Substring(e+1, num.Length-(e+1));
                    if(exponent[0] == '+') exponent = exponent.Substring(1, exponent.Length-1);
                }
                if(significand.IndexOf('.') == -1) significand = significand + ".";
                if(exponent == null) return new StringBox(n, significand, Syntax.TType.Ord);
                else {
                    return new HBox(n, new StringBox(significand, Syntax.TType.Ord), new CharBox(Unicode.M.MULTIPLICATION_SIGN, Syntax.TType.Op), new AtomBox(null, new StringBox("10", Syntax.TType.Ord), null, new StringBox(exponent, Syntax.TType.Ord), Syntax.TType.Ord));
                }
            }
        }

        protected override Box __TranslateNumber(Expr e, string n) {
            return new StringBox(e, n, Syntax.TType.Ord);
        }

        protected override Box __Translate(ArrayExpr e) {
            if(e.Elts.Rank == 2) {
                int h = e.Elts.GetLength(0);
                int w = e.Elts.GetLength(1);
                Box[,] mtx = new Box[h, w];
                for(int i = 0; i < h; i++) {
                    for(int j = 0; j < w; j++) {
                        mtx[i, j] = Translate(e[i, j]);
                    }
                }
                return new AlignmentBox(e, mtx);
            } else if(e.Elts.Rank == 1) {
                // Hacky temp thing
                int w = e.Elts.GetLength(0);
                Box[,] mtx = new Box[1, w];
                for(int j = 0; j < w; j++) {
                    mtx[0, j] = Translate(e[j]);
                }
                return new DelimitedBox(null, '<', new AlignmentBox(e, mtx), '>');
            } else {
                throw new NotImplementedException();
            }
        }

        protected override Box __Translate(LetterSym s) {
            /* TODO: need to handle accent, format */
            char c = s.Letter;
            CharBox cb = new CharBox(s, c, false, (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= 'α' && c <= 'ω') || c == Unicode.G.GREEK_PHI_SYMBOL, Syntax.TType.Ord);
            if(s.Subscript != new NullExpr()) {
                return new AtomBox(null, cb, Translate(s.Subscript), null, Syntax.TType.Ord);
            } else return cb;
        }

        protected override Box __Translate(WordSym s) {
            /* TODO: need to handle accent, format */
            StringBox sb = new StringBox(s, s.Word, Syntax.TType.Ord);
            if(s.Subscript != new NullExpr()) {
                return new AtomBox(null, sb, Translate(s.Subscript), null, Syntax.TType.Ord);
            } else return sb;
        }
    }
}
