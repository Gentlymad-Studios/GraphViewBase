using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace GraphViewBase {
    public class Edge : BaseEdge {
        public const float k_MinEdgeWidth = 1.75f;
        private const float k_EndPointRadius = 4.0f;
        private const float k_InterceptWidth = 6.0f;
        private const float k_EdgeLengthFromPort = 12.0f;
        private const float k_EdgeTurnDiameter = 20.0f;
        private const int k_DefaultEdgeWidth = 2;
        private const int k_DefaultEdgeWidthSelected = 2;
        private static readonly Color s_DefaultSelectedColor = new(240 / 255f, 240 / 255f, 240 / 255f);
        private static readonly Color s_DefaultColor = new(146 / 255f, 146 / 255f, 146 / 255f);

        //private static readonly Gradient s_Gradient = new();
        private static readonly Stack<VisualElement> s_CapPool = new();
        private readonly List<Vector2> m_LastLocalControlPoints = new();

        // The points that will be rendered. Expressed in coordinates local to the element.
        private readonly List<Vector2> m_RenderPoints = new();
        private float m_CapRadius = 5;
        private bool m_ControlPointsDirty = true;

        private int m_EdgeWidth = 2;
        private VisualElement m_FromCap;
        private Color m_FromCapColor;
        private Color m_InputColor = Color.grey;
        //private Orientation m_InputOrientation;
        private Color m_OutputColor = Color.grey;
        //private Orientation m_OutputOrientation;
        private bool m_RenderPointsDirty = true;
        private VisualElement m_ToCap;
        private Color m_ToCapColor;

        #region Static Helpers
        private static bool Approximately(Vector2 v1, Vector2 v2) =>
            Mathf.Approximately(v1.x, v2.x) && Mathf.Approximately(v1.y, v2.y);

        private static void RecycleCap(VisualElement cap) { s_CapPool.Push(cap); }

        private static VisualElement GetCap() {
            VisualElement result;
            if (s_CapPool.Count > 0) { result = s_CapPool.Pop(); } else {
                result = new();
                result.AddToClassList("edge-cap");
            }

            return result;
        }
        #endregion

        #region Constructor
        public Edge() {
            ClearClassList();
            AddToClassList("edge");
            m_FromCap = null;
            m_ToCap = null;
            CapRadius = k_EndPointRadius;
            InterceptWidth = k_InterceptWidth;
            generateVisualContent = OnGenerateVisualContent;
        }
        #endregion

        #region Properties
        public override bool Selected {
            get => base.Selected;
            set {
                if (base.Selected == value) { return; }
                base.Selected = value;
                if (value) {
                    InputColor = ColorSelected;
                    OutputColor = ColorSelected;
                    EdgeWidth = EdgeWidthSelected;
                } else {

                    InputColor = ColorUnselected;
                    OutputColor = ColorUnselected;
                    EdgeWidth = EdgeWidthUnselected;
                }
            }
        }

        public Color InputColor {
            get => m_InputColor;
            set {
                if (m_InputColor != value) {
                    m_InputColor = value;
                    MarkDirtyRepaint();
                }
            }
        }

        public Color OutputColor {
            get => m_OutputColor;
            set {
                if (m_OutputColor != value) {
                    m_OutputColor = value;
                    MarkDirtyRepaint();
                }
            }
        }

        public Color FromCapColor {
            get => m_FromCapColor;
            set {
                if (m_FromCapColor == value) { return; }
                m_FromCapColor = value;

                if (m_FromCap != null) { m_FromCap.style.backgroundColor = m_FromCapColor; }
                MarkDirtyRepaint();
            }
        }

        public Color ToCapColor {
            get => m_ToCapColor;
            set {
                if (m_ToCapColor == value) { return; }
                m_ToCapColor = value;

                if (m_ToCap != null) { m_ToCap.style.backgroundColor = m_ToCapColor; }
                MarkDirtyRepaint();
            }
        }

        public float CapRadius {
            get => m_CapRadius;
            set {
                if (Mathf.Approximately(m_CapRadius, value)) { return; }
                m_CapRadius = value;
                MarkDirtyRepaint();
            }
        }

        public int EdgeWidth {
            get => m_EdgeWidth;
            set {
                if (m_EdgeWidth == value) { return; }
                m_EdgeWidth = value;
                UpdateLayout(); // The layout depends on the edges width
            }
        }

        public bool DrawFromCap {
            get => m_FromCap != null;
            set {
                if (!value) {
                    if (m_FromCap != null) {
                        m_FromCap.RemoveFromHierarchy();
                        RecycleCap(m_FromCap);
                        m_FromCap = null;
                    }
                } else {
                    if (m_FromCap == null) {
                        m_FromCap = GetCap();
                        m_FromCap.style.backgroundColor = m_FromCapColor;
                        Add(m_FromCap);
                    }
                }
            }
        }

        public bool DrawToCap {
            get => m_ToCap != null;
            set {
                if (!value) {
                    if (m_ToCap != null) {
                        m_ToCap.RemoveFromHierarchy();
                        RecycleCap(m_ToCap);
                        m_ToCap = null;
                    }
                } else {
                    if (m_ToCap == null) {
                        m_ToCap = GetCap();
                        m_ToCap.style.backgroundColor = m_ToCapColor;
                        Add(m_ToCap);
                    }
                }
            }
        }

        public virtual int EdgeWidthUnselected { get; } = k_DefaultEdgeWidth;
        public virtual int EdgeWidthSelected { get; } = k_DefaultEdgeWidthSelected;
        public virtual Color ColorSelected { get; } = s_DefaultSelectedColor;
        public virtual Color ColorUnselected { get; } = s_DefaultColor;
        public float InterceptWidth { get; set; } = 5f;
        public Vector2[] ControlPoints { get; private set; }
        #endregion

        #region Rendering
        private void UpdateEdgeCaps() {
            if (m_FromCap != null) {
                Vector2 size = m_FromCap.layout.size;
                if (size.x > 0 && size.y > 0) {
                    Rect rect = new(From - size / 2f, size);
                    m_FromCap.style.left = rect.x;
                    m_FromCap.style.top = rect.y;
                    m_FromCap.style.width = rect.width;
                    m_FromCap.style.height = rect.height;
                }
            }
            if (m_ToCap != null) {
                Vector2 size = m_ToCap.layout.size;
                if (size.x > 0 && size.y > 0) {
                    Rect rect = new(To - size / 2f, size);
                    m_ToCap.style.left = rect.x;
                    m_ToCap.style.top = rect.y;
                    m_ToCap.style.width = rect.width;
                    m_ToCap.style.height = rect.height;
                }
            }
        }

        public virtual void UpdateLayout() {
            if (Graph == null) { return; }
            if (m_ControlPointsDirty) {
                ComputeControlPoints(); // Computes the control points in parent ( graph ) coordinates
                ComputeLayout(); // Update the element layout based on the control points.
                m_ControlPointsDirty = false;
            }
            UpdateEdgeCaps();
            MarkDirtyRepaint();
        }

        protected virtual void UpdateRenderPoints() {
            ComputeControlPoints(); // This should have been updated before : make sure anyway.

            if (m_RenderPointsDirty == false && ControlPoints != null) return;
            if(ControlPoints == null) return;

            Vector2[] points = new Vector2[ControlPoints.Length];
            for (int i = 0; i < ControlPoints.Length; i++)
                points[i] = Graph.ContentContainer.ChangeCoordinatesTo(this, ControlPoints[i]);;

            // Only compute this when the "local" points have actually changed
            if (m_LastLocalControlPoints.Count == ControlPoints.Length) {
                bool changed = false;
                for (int i = 0; i < points.Length; i++)
                {
                    if (m_LastLocalControlPoints[i] == points[i]) continue;
                    changed = true;
                    break;
                }
                m_RenderPointsDirty = changed;
                if(!changed) return;
            }
            
            Profiler.BeginSample("EdgeControl.UpdateRenderPoints");
            m_LastLocalControlPoints.Clear();
            foreach (var point in points)
                m_LastLocalControlPoints.Add(point);

            m_RenderPointsDirty = false;
            m_RenderPoints.Clear();

            m_RenderPoints.Add(points[0]);

            for (int i = 0; i < points.Length - 2; i++)
                m_RenderPoints.AddRange(GetRoundedCornerPoints(points[i], points[i+1], points[i+2]));
                
            
            m_RenderPoints.Add(points[^1]);
            Profiler.EndSample();
        }

        private Vector2[] GetRoundedCornerPoints(Vector2 point1, Vector2 cornerPoint, Vector2 point2)
        {
            // Calculate the direction vectors from the corner point to the start and end points.
            Vector2 direction1 = (point1 - cornerPoint).normalized;
            Vector2 direction2 = (point2 - cornerPoint).normalized;

            // Calculate the angle between the two edges.
            float angle = Vector2.Angle(direction1, direction2);

            // Adjust the edge turn diameter based on the angle.
            float adjustedEdgeTurnDiameter = k_EdgeTurnDiameter * Mathf.Sin(Mathf.Deg2Rad * angle / 2);

            // Calculate the distances from the corner point to the start and end points.
            float distance1 = Vector2.Distance(point1, cornerPoint);
            float distance2 = Vector2.Distance(point2, cornerPoint);

            distance1 = Mathf.Pow(distance1 / 10, 2f) * 1.9f;
            distance2 = Mathf.Pow(distance2 / 10, 2f) * 1.9f;

            // Adjust the start and end points based on the adjusted edge turn diameter, but do not exceed the original distances.
            Vector2 adjustedPoint1 = cornerPoint + direction1 * Mathf.Min(adjustedEdgeTurnDiameter, distance1);
            Vector2 adjustedPoint2 = cornerPoint + direction2 * Mathf.Min(adjustedEdgeTurnDiameter, distance2);

            // The number of points to generate along the curve.
            int numPoints = Mathf.RoundToInt(adjustedEdgeTurnDiameter * 10); // Adjust this as needed.

            Vector2[] curvePoints = new Vector2[numPoints];

            for (int i = 0; i < numPoints; i++)
            {
                float t = i / (float)(numPoints - 1); // Normalized parameter along the curve.

                // Calculate the coordinates of the point on the curve at parameter t using the quadratic Bezier curve formula.
                curvePoints[i] = (1 - t) * (1 - t) * adjustedPoint1 + 2 * (1 - t) * t * cornerPoint + t * t * adjustedPoint2;
            }

            return curvePoints;
        }

        private void AssignControlPoint(ref Vector2 destination, Vector2 newValue) {
            if (!Approximately(destination, newValue)) {
                destination = newValue;
                m_RenderPointsDirty = true;
            }
        }

        protected float GetOffset()
        {
            float offset = k_EdgeLengthFromPort + k_EdgeTurnDiameter;
            float distance = (To - From).magnitude;
            return offset * Mathf.Clamp01(distance / 150f);
        } 
        
        protected bool IsReverse()
        {
            float offset = GetOffset();
            float fromX = OutputOrientation == Orientation.Horizontal ? From.x + offset : From.x;
            float toX = InputOrientation == Orientation.Horizontal ? To.x - offset : To.x;
            return fromX > toX;
        }

        protected virtual void ComputeControlPoints() {
            if (m_ControlPointsDirty == false) { return; }

            Profiler.BeginSample("EdgeControl.ComputeControlPoints");
            
            float offset = GetOffset();
            bool isReverse = IsReverse();
            int numberOfControlPoints = isReverse ? 6 : 4;

            if (ControlPoints == null || ControlPoints.Length != numberOfControlPoints) { ControlPoints = new Vector2[numberOfControlPoints]; }

            //Assign Start and End Point
            AssignControlPoint(ref ControlPoints[0], From);
            AssignControlPoint(ref ControlPoints[^1], To);
            
            //Assign Offset Points
            float fromX = OutputOrientation == Orientation.Horizontal ? From.x + offset : From.x;
            float fromY = OutputOrientation == Orientation.Horizontal ?  From.y : From.y + offset;
            float toX = InputOrientation == Orientation.Horizontal ? To.x - offset : To.x;
            float toY = InputOrientation == Orientation.Horizontal ? To.y : To.y - offset;
            AssignControlPoint(ref ControlPoints[1], new(fromX, fromY));
            AssignControlPoint(ref ControlPoints[^2], new(toX, toY));

            //Assign Middle Points
            if (isReverse)
            {
                float startHeight = From.y;
                float endHeight = To.y;
                float middleY = (startHeight + endHeight) / 2;
                if (GetInputPort() != null && GetOutputPort() != null)
                {
                    VisualElement highestPort = GetInputPort().GetGlobalCenter().y < GetOutputPort().GetGlobalCenter().y ? GetInputPort() : GetOutputPort();
                    VisualElement lowestPort = highestPort == GetInputPort() ? GetOutputPort() : GetInputPort();

                    VisualElement highestNode = highestPort.GetFirstAncestorOfType<GraphElement>();
                    VisualElement lowestNode = lowestPort.GetFirstAncestorOfType<GraphElement>();

                    middleY = (highestNode.transform.position.y + highestNode.resolvedStyle.height + lowestNode.transform.position.y) / 2;
                    middleY = Mathf.Clamp(middleY, highestNode.transform.position.y + 20, lowestNode.transform.position.y + 40);
                } 

                AssignControlPoint(ref ControlPoints[2], new(fromX, middleY));
                AssignControlPoint(ref ControlPoints[3], new(toX, middleY));
            }

            Profiler.EndSample();
        }

        private void ComputeLayout() {
            Profiler.BeginSample("EdgeControl.ComputeLayout");
            Vector2 to = ControlPoints[^1];
            Vector2 from = ControlPoints[0];

            Rect rect = new(Vector2.Min(to, from), new(Mathf.Abs(from.x - to.x), Mathf.Abs(from.y - to.y)));

            // Make sure any control points (including tangents, are included in the rect)
            for (int i = 1; i < ControlPoints.Length - 1; ++i)
            {
                if (rect.Contains(ControlPoints[i])) continue;
                Vector2 pt = ControlPoints[i];
                rect.xMin = Math.Min(rect.xMin, pt.x);
                rect.yMin = Math.Min(rect.yMin, pt.y);
                rect.xMax = Math.Max(rect.xMax, pt.x);
                rect.yMax = Math.Max(rect.yMax, pt.y);
            }

            //Make sure that we have the place to display Edges with EdgeControl.k_MinEdgeWidth at the lowest level of zoom.
            // float margin = Mathf.Max(EdgeWidth * 0.5f + 1, k_MinEdgeWidth / Graph.minScale);
            float
                margin = EdgeWidth /
                         Graph.CurrentScale; //Mathf.Max(EdgeWidth * 0.5f + 1, k_MinEdgeWidth / Graph.minScale);
            rect.xMin -= margin;
            rect.yMin -= margin;
            rect.width += margin;
            rect.height += margin;

            if (layout != rect) {
                transform.position = new Vector2(rect.x, rect.y);
                style.width = rect.width;
                style.height = rect.height;
                m_RenderPointsDirty = true;
            }
            Profiler.EndSample();
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc) {
            if (EdgeWidth <= 0 || Graph == null) { return; }

            UpdateRenderPoints();
            if (m_RenderPoints.Count == 0) {
                return; // Don't draw anything
            }

            // Color outColor = this.outputColor;
            Color inColor = InputColor;

            int cpt = m_RenderPoints.Count;
            Painter2D painter2D = mgc.painter2D;

            float width = EdgeWidth;

            // float alpha = 1.0f;
            float zoom = Graph.CurrentScale;

            if (EdgeWidth * zoom < k_MinEdgeWidth) {
                // alpha = edgeWidth * zoom / k_MinEdgeWidth;
                width = k_MinEdgeWidth / zoom;
            }

            // k_Gradient.SetKeys(new[]{ new GradientColorKey(outColor, 0),new GradientColorKey(inColor, 1)},new []{new GradientAlphaKey(alpha, 0)});
            painter2D.BeginPath();

            // painter2D.strokeGradient = k_Gradient;
            painter2D.strokeColor = inColor;
            painter2D.lineWidth = width;
            painter2D.MoveTo(m_RenderPoints[0]);

            for (int i = 1; i < cpt; ++i) { painter2D.LineTo(m_RenderPoints[i]); }

            painter2D.Stroke();
        }
        #endregion

        #region Intersection
        public override bool ContainsPoint(Vector2 localPoint) {
            Profiler.BeginSample("EdgeControl.ContainsPoint");

            if (!base.ContainsPoint(localPoint)) {
                Profiler.EndSample();
                return false;
            }

            // bounding box check succeeded, do more fine grained check by measuring distance to bezier points
            // exclude endpoints

            float capMaxDist = 4 * CapRadius * CapRadius; //(2 * CapRadius)^2
            if ((From - localPoint).sqrMagnitude <= capMaxDist || (To - localPoint).sqrMagnitude <= capMaxDist) {
                Profiler.EndSample();
                return false;
            }

            List<Vector2> allPoints = m_RenderPoints;
            if (allPoints.Count > 0) {
                //we use squareDistance to avoid sqrts
                float distance = (allPoints[0] - localPoint).sqrMagnitude;
                float interceptWidth2 = InterceptWidth * InterceptWidth;
                for (int i = 0; i < allPoints.Count - 1; i++) {
                    Vector2 currentPoint = allPoints[i];
                    Vector2 nextPoint = allPoints[i + 1];

                    Vector2 next2Current = nextPoint - currentPoint;
                    float distanceNext = (nextPoint - localPoint).sqrMagnitude;
                    float distanceLine = next2Current.sqrMagnitude;

                    // if the point is somewhere between the two points
                    if (distance < distanceLine && distanceNext < distanceLine) {
                        //https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
                        float d = next2Current.y * localPoint.x -
                                  next2Current.x * localPoint.y + nextPoint.x * currentPoint.y -
                                  nextPoint.y * currentPoint.x;
                        if (d * d < interceptWidth2 * distanceLine) {
                            Profiler.EndSample();
                            return true;
                        }
                    }

                    distance = distanceNext;
                }
            }

            Profiler.EndSample();
            return false;
        }

        public override bool Overlaps(Rect rect) {
            if (base.Overlaps(rect)) {
                for (int a = 0; a < m_RenderPoints.Count - 1; a++) {
                    if (RectUtils.IntersectsSegment(rect, m_RenderPoints[a], m_RenderPoints[a + 1])) { return true; }
                }
            }
            return false;
        }
        #endregion

        #region Event Handlers
        protected override void OnAddedToGraphView() {
            base.OnAddedToGraphView();
            //Graph.OnViewTransformChanged += MarkDirtyOnTransformChanged;
            OnEdgeChanged();
        }

        protected override void OnRemovedFromGraphView() {
            base.OnRemovedFromGraphView();
            //Graph.OnViewTransformChanged -= MarkDirtyOnTransformChanged;
            UpdateEdgeCaps();
        }

        private void MarkDirtyOnTransformChanged(GraphElementContainer contentContainer) { MarkDirtyRepaint(); }

        protected override void OnEdgeChanged() {
            DrawFromCap = Output == null;
            DrawToCap = Input == null;
            m_ControlPointsDirty = true;
            UpdateLayout();
        }
        #endregion

        #region Helper Classes/Structs
        private struct EdgeCornerSweepValues {
            public Vector2 circleCenter;
            public double sweepAngle;
            public double startAngle;
            public double endAngle;
            public Vector2 crossPoint1;
            public Vector2 crossPoint2;
            public float radius;
        }
        #endregion

        public override string ToString() => $"Output({Output}) -> Input({Input})";
    }
}