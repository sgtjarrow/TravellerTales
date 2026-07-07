using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TravellerTales.Controls;

public sealed class PolygonButton : Button
{
    public static readonly DependencyProperty ShapeDataProperty =
        DependencyProperty.Register(
            nameof(ShapeData),
            typeof(Geometry),
            typeof(PolygonButton),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty HoverFillProperty =
        DependencyProperty.Register(
            nameof(HoverFill),
            typeof(Brush),
            typeof(PolygonButton),
            new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty HoverStrokeProperty =
        DependencyProperty.Register(
            nameof(HoverStroke),
            typeof(Brush),
            typeof(PolygonButton),
            new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PressedFillProperty =
        DependencyProperty.Register(
            nameof(PressedFill),
            typeof(Brush),
            typeof(PolygonButton),
            new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(
            nameof(StrokeThickness),
            typeof(double),
            typeof(PolygonButton),
            new FrameworkPropertyMetadata(2.0, FrameworkPropertyMetadataOptions.AffectsRender));

    static PolygonButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(PolygonButton),
            new FrameworkPropertyMetadata(typeof(PolygonButton)));
    }

    public Geometry? ShapeData
    {
        get => (Geometry?)GetValue(ShapeDataProperty);
        set => SetValue(ShapeDataProperty, value);
    }

    public Brush HoverFill
    {
        get => (Brush)GetValue(HoverFillProperty);
        set => SetValue(HoverFillProperty, value);
    }

    public Brush HoverStroke
    {
        get => (Brush)GetValue(HoverStrokeProperty);
        set => SetValue(HoverStrokeProperty, value);
    }

    public Brush PressedFill
    {
        get => (Brush)GetValue(PressedFillProperty);
        set => SetValue(PressedFillProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        var shape = ShapeData;
        if (shape is null)
        {
            return null;
        }

        return shape.FillContains(hitTestParameters.HitPoint)
            ? new PointHitTestResult(this, hitTestParameters.HitPoint)
            : null;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var shape = ShapeData;
        if (shape is null || !IsEnabled || (!IsMouseOver && !IsKeyboardFocused))
        {
            return;
        }

        var fill = IsPressed ? PressedFill : HoverFill;
        var pen = new Pen(HoverStroke, StrokeThickness);
        drawingContext.DrawGeometry(fill, pen, shape);
    }
}
