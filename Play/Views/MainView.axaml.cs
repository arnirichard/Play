using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Play.Airline;
using Play.ViewModels;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Threading;

namespace Play.Views;

public partial class MainView : UserControl
{
    bool isControlPressed;
    double? pointerPressedRatio;
    DateTime startPressed;

    public static readonly StyledProperty<DateTime> StartDateProperty =
            AvaloniaProperty.Register<Gantt, DateTime>(nameof(StartDate));

    public DateTime StartDate
    {
        get { return GetValue(StartDateProperty); }
        set { SetValue(StartDateProperty, value); }
    }

    public static readonly StyledProperty<DateTime> EndDateProperty =
        AvaloniaProperty.Register<Gantt, DateTime>(nameof(EndDate));

    public DateTime EndDate
    {
        get { return GetValue(EndDateProperty); }
        set { SetValue(EndDateProperty, value); }
    }

    public MainView()
    {
        InitializeComponent();

        Loaded += MainView_Loaded;
        DataContextChanged += MainView_DataContextChanged;
        
        aircraftsDataGrid.PointerWheelChanged += DataGrid_PointerWheelChanged;
        crewMembersDataGrid.PointerWheelChanged += DataGrid_PointerWheelChanged;

        aircraftsDataGrid.AddHandler(PointerWheelChangedEvent, DataGrid_PointerWheelChanged, RoutingStrategies.Tunnel);
        crewMembersDataGrid.AddHandler(PointerWheelChangedEvent, DataGrid_PointerWheelChanged, RoutingStrategies.Tunnel);

        KeyDownEvent.AddClassHandler<TopLevel>(OnGlobalKeyDown, handledEventsToo: true);
        KeyUpEvent.AddClassHandler<TopLevel>(OnGlobalKeyUp, handledEventsToo: true);

        AddHandler(PointerPressedEvent, DataGrid_PointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, DataGrid_PointerReleased, RoutingStrategies.Tunnel);

        SizeChanged += MainView_SizeChanged;
        PointerMoved += AircraftsDataGrid_PointerMoved;
    }

    private void MainView_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        aircraftsDataGrid.MaxHeight = Bounds.Height / 2;
    }

    private void AircraftsDataGrid_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (pointerPressedRatio != null && DataContext is MainViewModel vm)
        {
            var currentRatio = e.GetPosition(aircraftsDataGrid).X / aircraftsDataGrid.Bounds.Width;

            if(Math.Abs(pointerPressedRatio.Value - currentRatio) < 0.006)
            {
                return;
            }

            long diff = (long)((pointerPressedRatio - currentRatio) * (EndDate.Ticks - StartDate.Ticks));

            if (Math.Abs(diff) < 100000000)
            {
                return;
            }

            long period = EndDate.Ticks - StartDate.Ticks;

            StartDate = new DateTime(startPressed.Ticks + diff);
            EndDate = new DateTime(StartDate.Ticks + period);

            if (StartDate < vm.EarliestDate)
            {
                StartDate = vm.EarliestDate;
                EndDate = new DateTime(StartDate.Ticks + period);
            }
            else if (EndDate > vm.LatestDate)
            {
                EndDate = vm.LatestDate;
                StartDate = new DateTime(EndDate.Ticks - period);
            }

            AdjustDates(vm);
        }
    }

    private void GanttItem_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is Gantt g && g.DataContext is ScheduledResource ac)
        {
            double currentRatio = e.GetPosition(g).X / g.Bounds.Width;
            DateTime time = StartDate + (EndDate - StartDate) * currentRatio;
            TaskItem? task = ac.GetTaskAtTime(time);
            var positionRelativeToWindow = g.TranslatePoint(new Point(0, 0), this);

            if (task != null && positionRelativeToWindow != null)
            {
                MyPopup.PlacementTarget = g;
                tooltipTextBlock.Text = task.ToString();
                MyPopup.IsOpen = true;
            }
            else
            {
                MyPopup.IsOpen = false;
            }
        }
        else
        {
            MyPopup.IsOpen = false;
        }

        //Debug.WriteLine("GanttItem_PointerMoved");
    }

    void AdjustDates(MainViewModel vm)
    {
        if (StartDate < vm.EarliestDate)
        {
            StartDate = vm.EarliestDate;
            if (EndDate - StartDate < TimeSpan.FromHours(10))
            {
                EndDate = StartDate.AddHours(12);
            }
        }
        if (EndDate > vm.LatestDate)
        {
            EndDate = vm.LatestDate;
            if (EndDate - StartDate < TimeSpan.FromHours(10))
            {
                StartDate = EndDate.AddHours(-12);
            }
        }
    }

    private void DataGrid_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        pointerPressedRatio = e.GetPosition(aircraftsDataGrid).X / aircraftsDataGrid.Bounds.Width;
        startPressed = StartDate;
        //Debug.WriteLine("DataGrid_PointerPressed");
    }

    private void DataGrid_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        pointerPressedRatio = null;
        //Debug.WriteLine("DataGrid_PointerReleased");
    }

    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
        {
            isControlPressed = true;
            Debug.WriteLine("Control key down");
        }
    }

    private void OnGlobalKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
        {
            isControlPressed = false;
            Debug.WriteLine("Control key up");
        }
    }

    private void MainView_DataContextChanged(object? sender, System.EventArgs e)
    {
        if(DataContext is MainViewModel vm)
        {
            StartDate = vm.EarliestDate;
            EndDate = vm.LatestDate;

            aircraftsDataGrid.Height = vm.Aircrafts.Count * aircraftsDataGrid.RowHeight + 35;
        }
    }

    private void DataGrid_PointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        if(isControlPressed && DataContext is MainViewModel vm)
        {
            var zoomIn = e.Delta.Y > 0;
            double x_ratio = e.GetPosition(ganttHeader).X / ganttHeader.Bounds.Width;
            
            x_ratio = Math.Min(1, Math.Max(0, x_ratio));
            
            long widthTicks = EndDate.Ticks - StartDate.Ticks;
            long x_ticks = StartDate.Ticks + (long)(widthTicks * x_ratio);

            if (zoomIn)
            {
                widthTicks = (long)(widthTicks/ 1.3);
                if(widthTicks < TimeSpan.FromHours(12).Ticks)
                {
                    widthTicks = TimeSpan.FromHours(12).Ticks;
                }
            }
            else
            {
                widthTicks *= 2;
            }

            long startTicks = (long)(x_ticks - x_ratio * widthTicks);

            StartDate = new DateTime(startTicks);
            EndDate = new DateTime(startTicks+widthTicks);
            AdjustDates(vm);

            pointerPressedRatio = null;

            e.Handled = true;
        }

        //Debug.WriteLine("DataGrid_PointerWheelChanged");
    }

    private void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mvm)
        {
            aircraftsDataGrid.ItemsSource = mvm.Aircrafts.Values;
            crewMembersDataGrid.ItemsSource = mvm.CrewMembers;
        }
        SyncColumnWidths(aircraftsDataGrid, crewMembersDataGrid);
    }

    private void DataGrid1_ColumnWidthChanged(object sender, DataGridColumnEventArgs e)
    {
        SyncColumnWidths(aircraftsDataGrid, crewMembersDataGrid);
    }

    private void DataGrid2_ColumnWidthChanged(object sender, DataGridColumnEventArgs e)
    {
        SyncColumnWidths(aircraftsDataGrid, crewMembersDataGrid);
    }

    private void SyncColumnWidths(DataGrid sourceGrid, DataGrid targetGrid)
    {
        for (int i = 0; i < sourceGrid.Columns.Count-1 && i < targetGrid.Columns.Count-1; i++)
        {
            targetGrid.Columns[i].Width = new DataGridLength(sourceGrid.Columns[i].ActualWidth);
        }
    }
}
