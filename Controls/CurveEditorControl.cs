using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using ESCenter.Models;
using WPoint = System.Windows.Point;
using WVector = System.Windows.Vector;
using WFlowDirection = System.Windows.FlowDirection;
using WMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WMouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using WCursors = System.Windows.Input.Cursors;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;
using MediaPen = System.Windows.Media.Pen;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using DrawingContext = System.Windows.Media.DrawingContext;

namespace ESCenter.Controls
{
    public class CurveEditorControl : Canvas
    {
        public static readonly DependencyProperty CurvesProperty = DependencyProperty.Register(
            nameof(Curves),
            typeof(ObservableCollection<CurveSeries>),
            typeof(CurveEditorControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnCurvesChanged));

        public static readonly DependencyProperty IndicatorRatioProperty = DependencyProperty.Register(
            nameof(IndicatorRatio),
            typeof(double),
            typeof(CurveEditorControl),
            new FrameworkPropertyMetadata(0.5d, FrameworkPropertyMetadataOptions.AffectsRender, OnIndicatorRatioChanged));

        private readonly Path _curvePath = new();
        private readonly Canvas _overlayCanvas = new();
        private readonly Line _indicatorLine = new();

        private CurveSeries? _selectedSeries;
        private CurveNode? _selectedNode;
        private DragTarget _dragTarget = DragTarget.None;
        private WPoint _dragStart;
        private WPoint _nodeStart;
        private WPoint _handleStart;
        private double _zoom = 1.0;
        private double _hoverIndicatorRatio = 0.5;
        private bool _isMouseIndicatorActive;

        public ObservableCollection<CurveSeries>? Curves
        {
            get => (ObservableCollection<CurveSeries>?)GetValue(CurvesProperty);
            set => SetValue(CurvesProperty, value);
        }

        public double IndicatorRatio
        {
            get => (double)GetValue(IndicatorRatioProperty);
            set => SetValue(IndicatorRatioProperty, value);
        }

        public CurveEditorControl()
        {
            ClipToBounds = true;
            Background = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#0F1624"));
            MinHeight = 140;

            _curvePath.Stroke = MediaBrushes.White;
            _curvePath.StrokeThickness = 2.2;
            _curvePath.SnapsToDevicePixels = true;
            Children.Add(_curvePath);

            _indicatorLine.Stroke = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#6AF7EF"));
            _indicatorLine.StrokeThickness = 1.5;
            _indicatorLine.IsHitTestVisible = false;
            Children.Add(_indicatorLine);

            _overlayCanvas.IsHitTestVisible = true;
            Children.Add(_overlayCanvas);

            SizeChanged += (_, _) => RebuildVisuals();
            MouseMove += OnMouseMove;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            MouseRightButtonUp += OnMouseRightButtonUp;
            MouseWheel += OnMouseWheel;
            
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var majorPen = new MediaPen(new SolidColorBrush(MediaColor.FromArgb(65, 255, 255, 255)), 1);
            var minorPen = new MediaPen(new SolidColorBrush(MediaColor.FromArgb(25, 255, 255, 255)), 1);

            var minorStep = 12.0 * _zoom;
            var majorStep = 48.0 * _zoom;

            for (var x = 0.0; x <= ActualWidth; x += minorStep)
            {
                dc.DrawLine(minorPen, new WPoint(x, 0), new WPoint(x, ActualHeight));
            }

            for (var y = 0.0; y <= ActualHeight; y += minorStep)
            {
                dc.DrawLine(minorPen, new WPoint(0, y), new WPoint(ActualWidth, y));
            }

            for (var x = 0.0; x <= ActualWidth; x += majorStep)
            {
                dc.DrawLine(majorPen, new WPoint(x, 0), new WPoint(x, ActualHeight));
            }

            for (var y = 0.0; y <= ActualHeight; y += majorStep)
            {
                dc.DrawLine(majorPen, new WPoint(0, y), new WPoint(ActualWidth, y));
            }

            DrawAxisLabels(dc);
        }

        private void DrawAxisLabels(DrawingContext dc)
        {
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var typeface = new Typeface("Segoe UI");
            var textBrush = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#A0FFFFFF"));

            for (var i = 0; i <= 4; i++)
            {
                var x = (ActualWidth / 4.0) * i;
                var xLabel = new FormattedText($"{i * 25}%", CultureInfo.InvariantCulture, WFlowDirection.LeftToRight, typeface, 9, textBrush, dpi);
                dc.DrawText(xLabel, new WPoint(Math.Max(0, x - 10), Math.Max(0, ActualHeight - 14)));
            }

            for (var i = 0; i <= 4; i++)
            {
                var y = (ActualHeight / 4.0) * i;
                var yLabel = new FormattedText($"{100 - (i * 25)}", CultureInfo.InvariantCulture, WFlowDirection.LeftToRight, typeface, 9, textBrush, dpi);
                dc.DrawText(yLabel, new WPoint(2, Math.Max(0, y - 7)));
            }

            var axisPen = new MediaPen(new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#55FFFFFF")), 1);
            dc.DrawLine(axisPen, new WPoint(0, ActualHeight - 1), new WPoint(ActualWidth, ActualHeight - 1));
            dc.DrawLine(axisPen, new WPoint(0, 0), new WPoint(0, ActualHeight));
        }

        private static void OnCurvesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CurveEditorControl control)
            {
                if (e.OldValue is ObservableCollection<CurveSeries> oldCurves)
                {
                    oldCurves.CollectionChanged -= control.OnCurvesCollectionChanged;
                    control.DetachCurveHandlers(oldCurves);
                }

                if (e.NewValue is ObservableCollection<CurveSeries> newCurves)
                {
                    newCurves.CollectionChanged += control.OnCurvesCollectionChanged;
                    control.AttachCurveHandlers(newCurves);
                }

                control.RebuildVisuals();
            }
        }

        private static void OnIndicatorRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CurveEditorControl control)
            {
                control._hoverIndicatorRatio = Clamp((double)e.NewValue, 0, 1);
                control.UpdateIndicator();
            }
        }

        private void OnCurvesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (CurveSeries series in e.OldItems)
                {
                    series.PropertyChanged -= OnSeriesPropertyChanged;
                    series.Nodes.CollectionChanged -= OnNodesChanged;
                    foreach (var node in series.Nodes)
                    {
                        node.PropertyChanged -= OnNodePropertyChanged;
                    }
                }
            }

            if (e.NewItems is not null)
            {
                foreach (CurveSeries series in e.NewItems)
                {
                    series.PropertyChanged += OnSeriesPropertyChanged;
                    series.Nodes.CollectionChanged += OnNodesChanged;
                    foreach (var node in series.Nodes)
                    {
                        node.PropertyChanged += OnNodePropertyChanged;
                    }
                }
            }

            RebuildVisuals();
        }

        private void AttachCurveHandlers(ObservableCollection<CurveSeries> curves)
        {
            foreach (var series in curves)
            {
                series.PropertyChanged += OnSeriesPropertyChanged;
                series.Nodes.CollectionChanged += OnNodesChanged;
                foreach (var node in series.Nodes)
                {
                    node.PropertyChanged += OnNodePropertyChanged;
                }
            }
        }

        private void DetachCurveHandlers(ObservableCollection<CurveSeries> curves)
        {
            foreach (var series in curves)
            {
                series.PropertyChanged -= OnSeriesPropertyChanged;
                series.Nodes.CollectionChanged -= OnNodesChanged;
                foreach (var node in series.Nodes)
                {
                    node.PropertyChanged -= OnNodePropertyChanged;
                }
            }
        }

        private void OnSeriesPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurveSeries.IsVisible))
            {
                RebuildVisuals();
            }
        }

        private void OnNodesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (CurveNode node in e.OldItems)
                {
                    node.PropertyChanged -= OnNodePropertyChanged;
                }
            }

            if (e.NewItems is not null)
            {
                foreach (CurveNode node in e.NewItems)
                {
                    node.PropertyChanged += OnNodePropertyChanged;
                }
            }

            RebuildVisuals();
        }

        private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e) => RebuildVisuals();

        private void RebuildVisuals()
        {
            BuildPathGeometry();
            BuildNodeVisuals();
            UpdateIndicator();
            InvalidateVisual();
        }

        private void BuildPathGeometry()
        {
            var geometry = new PathGeometry();
            if (Curves is null)
            {
                _curvePath.Data = geometry;
                return;
            }

            var visible = Curves.Where(c => c.IsVisible).ToList();
            var group = new GeometryGroup();

            foreach (var series in visible)
            {
                var nodes = series.Nodes.OrderBy(n => n.Position.X).ToList();
                if (nodes.Count == 0)
                {
                    continue;
                }

                var figure = new PathFigure { StartPoint = ToCanvas(nodes[0].Position), IsClosed = false, IsFilled = false };

                for (var i = 1; i < nodes.Count; i++)
                {
                    var prev = nodes[i - 1];
                    var current = nodes[i];
                    figure.Segments.Add(new BezierSegment(
                        ToCanvas(prev.HandleOut),
                        ToCanvas(current.HandleIn),
                        ToCanvas(current.Position), true));
                }

                var seriesGeometry = new PathGeometry();
                seriesGeometry.Figures.Add(figure);
                group.Children.Add(seriesGeometry);
            }

            _curvePath.Stroke = MediaBrushes.White;
            _curvePath.Data = group;
        }

        private void BuildNodeVisuals()
        {
            _overlayCanvas.Children.Clear();
            if (Curves is null)
            {
                return;
            }

            foreach (var series in Curves.Where(c => c.IsVisible))
            {
                foreach (var node in series.Nodes)
                {
                    var anchor = CreateHandleEllipse(node, DragTarget.Anchor, 10, node == _selectedNode ? "#00E5FF" : "#FFD800");
                    var handleIn = CreateHandleEllipse(node, DragTarget.HandleIn, 6, "#FFD800");
                    var handleOut = CreateHandleEllipse(node, DragTarget.HandleOut, 6, "#FFD800");

                    var connectorPen = new MediaPen(new SolidColorBrush(MediaColor.FromArgb(120, 255, 216, 0)), 1);
                    var inLine = new Line
                    {
                        X1 = ToCanvas(node.Position).X,
                        Y1 = ToCanvas(node.Position).Y,
                        X2 = ToCanvas(node.HandleIn).X,
                        Y2 = ToCanvas(node.HandleIn).Y,
                        Stroke = connectorPen.Brush,
                        StrokeThickness = 1
                    };
                    var outLine = new Line
                    {
                        X1 = ToCanvas(node.Position).X,
                        Y1 = ToCanvas(node.Position).Y,
                        X2 = ToCanvas(node.HandleOut).X,
                        Y2 = ToCanvas(node.HandleOut).Y,
                        Stroke = connectorPen.Brush,
                        StrokeThickness = 1
                    };

                    _overlayCanvas.Children.Add(inLine);
                    _overlayCanvas.Children.Add(outLine);
                    _overlayCanvas.Children.Add(handleIn);
                    _overlayCanvas.Children.Add(handleOut);
                    _overlayCanvas.Children.Add(anchor);

                    anchor.Tag = (series, node, DragTarget.Anchor);
                    handleIn.Tag = (series, node, DragTarget.HandleIn);
                    handleOut.Tag = (series, node, DragTarget.HandleOut);
                }
            }
        }

        private Ellipse CreateHandleEllipse(CurveNode node, DragTarget target, double size, string colorHex)
        {
            var point = target switch
            {
                DragTarget.Anchor => ToCanvas(node.Position),
                DragTarget.HandleIn => ToCanvas(node.HandleIn),
                _ => ToCanvas(node.HandleOut)
            };

            var ellipse = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString(colorHex)),
                Stroke = MediaBrushes.Black,
                StrokeThickness = 1,
                Cursor = WCursors.Hand
            };

            SetLeft(ellipse, point.X - size / 2);
            SetTop(ellipse, point.Y - size / 2);
            return ellipse;
        }

        private void OnMouseLeftButtonDown(object sender, WMouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                OnMouseDoubleClick(sender, e);
                return;
            }

            if (e.OriginalSource is Ellipse ellipse && ellipse.Tag is ValueTuple<CurveSeries, CurveNode, DragTarget> tag)
            {
                _selectedSeries = tag.Item1;
                _selectedNode = tag.Item2;
                _dragTarget = tag.Item3;
                _dragStart = e.GetPosition(this);
                _nodeStart = _selectedNode.Position;
                _handleStart = _dragTarget switch
                {
                    DragTarget.HandleIn => _selectedNode.HandleIn,
                    DragTarget.HandleOut => _selectedNode.HandleOut,
                    _ => _selectedNode.Position
                };

                CaptureMouse();
                RebuildVisuals();
            }
        }

        private void OnMouseMove(object sender, WMouseEventArgs e)
        {
            var mouse = e.GetPosition(this);

            if (_dragTarget == DragTarget.None || _selectedNode is null)
            {
                _isMouseIndicatorActive = true;
                _hoverIndicatorRatio = Clamp(mouse.X / Math.Max(1, ActualWidth), 0, 1);
                UpdateIndicator();
                return;
            }

            var delta = new WVector((mouse.X - _dragStart.X) / Math.Max(1, ActualWidth) / _zoom, (mouse.Y - _dragStart.Y) / Math.Max(1, ActualHeight));
            if (_dragTarget == DragTarget.Anchor)
            {
                var newPos = new WPoint(Clamp(_nodeStart.X + delta.X, 0, 1), Clamp(_nodeStart.Y + delta.Y, 0, 1));
                var move = new WVector(newPos.X - _selectedNode.Position.X, newPos.Y - _selectedNode.Position.Y);
                _selectedNode.Position = newPos;
                _selectedNode.HandleIn = new WPoint(Clamp(_selectedNode.HandleIn.X + move.X, 0, 1), Clamp(_selectedNode.HandleIn.Y + move.Y, 0, 1));
                _selectedNode.HandleOut = new WPoint(Clamp(_selectedNode.HandleOut.X + move.X, 0, 1), Clamp(_selectedNode.HandleOut.Y + move.Y, 0, 1));
            }
            else if (_dragTarget == DragTarget.HandleIn)
            {
                _selectedNode.HandleIn = new WPoint(Clamp(_handleStart.X + delta.X, 0, 1), Clamp(_handleStart.Y + delta.Y, 0, 1));
            }
            else if (_dragTarget == DragTarget.HandleOut)
            {
                _selectedNode.HandleOut = new WPoint(Clamp(_handleStart.X + delta.X, 0, 1), Clamp(_handleStart.Y + delta.Y, 0, 1));
            }
        }

        private void OnMouseLeftButtonUp(object sender, WMouseButtonEventArgs e)
        {
            _dragTarget = DragTarget.None;
            ReleaseMouseCapture();
        }

        private void OnMouseRightButtonUp(object sender, WMouseButtonEventArgs e)
        {
            if (e.OriginalSource is Ellipse ellipse && ellipse.Tag is ValueTuple<CurveSeries, CurveNode, DragTarget> tag)
            {
                var series = tag.Item1;
                var node = tag.Item2;
                if (series.Nodes.Count > 2)
                {
                    series.Nodes.Remove(node);
                }
            }
        }

        private void OnMouseDoubleClick(object sender, WMouseButtonEventArgs e)
        {
            if (Curves is null || Curves.Count == 0)
            {
                return;
            }

            var targetSeries = _selectedSeries ?? Curves.FirstOrDefault(c => c.IsVisible) ?? Curves[0];
            var pos = e.GetPosition(this);
            var normalized = new WPoint(Clamp(pos.X / Math.Max(1, ActualWidth), 0, 1), Clamp(pos.Y / Math.Max(1, ActualHeight), 0, 1));
            targetSeries.Nodes.Add(new CurveNode(normalized));
        }

        private void OnMouseWheel(object sender, WMouseWheelEventArgs e)
        {
            var step = e.Delta > 0 ? 0.1 : -0.1;
            _zoom = Clamp(_zoom + step, 0.6, 2.2);
            RebuildVisuals();
        }

        private void UpdateIndicator()
        {
            var ratio = _isMouseIndicatorActive ? _hoverIndicatorRatio : Clamp(IndicatorRatio, 0, 1);
            var x = ratio * ActualWidth;
            _indicatorLine.X1 = x;
            _indicatorLine.X2 = x;
            _indicatorLine.Y1 = 0;
            _indicatorLine.Y2 = ActualHeight;
        }

        private WPoint ToCanvas(WPoint normalized)
        {
            var x = normalized.X * ActualWidth * _zoom;
            var y = normalized.Y * ActualHeight;
            return new WPoint(x, y);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private enum DragTarget
        {
            None,
            Anchor,
            HandleIn,
            HandleOut
        }
    }
}
