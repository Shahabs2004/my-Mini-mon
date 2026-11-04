using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SmartViewCompanion;

public partial class RegionSelectionWindow : Window
{
    private Point _startDip;
    private Point _startScreen;
    private bool _isDragging;

    public Rect? SelectedScreenRegion { get; private set; }

    public RegionSelectionWindow()
    {
        InitializeComponent();
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        Loaded += (_, _) => SelectionCanvas.Focus();
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startDip = e.GetPosition(SelectionCanvas);
        _startScreen = SelectionCanvas.PointToScreen(_startDip);
        _isDragging = true;
        SelectionRectangle.Visibility = Visibility.Visible;
        UpdateRectangle(e.GetPosition(SelectionCanvas));
        SelectionCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
            UpdateRectangle(e.GetPosition(SelectionCanvas));
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
            return;

        Point endScreen = SelectionCanvas.PointToScreen(e.GetPosition(SelectionCanvas));
        _isDragging = false;
        SelectionCanvas.ReleaseMouseCapture();

        double left = Math.Min(_startScreen.X, endScreen.X);
        double top = Math.Min(_startScreen.Y, endScreen.Y);
        double width = Math.Abs(endScreen.X - _startScreen.X);
        double height = Math.Abs(endScreen.Y - _startScreen.Y);

        if (width >= 16 && height >= 16)
            SelectedScreenRegion = new Rect(left, top, width, height);

        DialogResult = SelectedScreenRegion.HasValue;
        Close();
    }

    private void UpdateRectangle(Point current)
    {
        double left = Math.Min(_startDip.X, current.X);
        double top = Math.Min(_startDip.Y, current.Y);
        SelectionRectangle.Width = Math.Abs(current.X - _startDip.X);
        SelectionRectangle.Height = Math.Abs(current.Y - _startDip.Y);
        Canvas.SetLeft(SelectionRectangle, left);
        Canvas.SetTop(SelectionRectangle, top);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedScreenRegion = null;
            DialogResult = false;
            Close();
        }
    }
}