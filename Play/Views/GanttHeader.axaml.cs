using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Play.Airline;
using Play.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Play.Views
{
    public partial class GanttHeader : UserControl
    {
        public static int Blue = int.Parse("FF0000FF", System.Globalization.NumberStyles.HexNumber);
        public static int Black = int.Parse("FF000000", System.Globalization.NumberStyles.HexNumber);
        public static int Grey = int.Parse("AA999999", System.Globalization.NumberStyles.HexNumber);
        public static int Green = int.Parse("FF00FF00", System.Globalization.NumberStyles.HexNumber);
        public static int Orange = int.Parse("FFFF6A00", System.Globalization.NumberStyles.HexNumber);
        public static int LightBlue = int.Parse("FF3AC4FF", System.Globalization.NumberStyles.HexNumber);
        

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

        public bool ShowLabels { get; set; } = true;

        public GanttHeader()
        {
            InitializeComponent();

            this.GetObservable(StartDateProperty).Subscribe(value =>
            {
                _ = ReDraw();
            });

            this.GetObservable(EndDateProperty).Subscribe(value =>
            {
                _ = ReDraw();
            });

            grid.GetObservable(BoundsProperty).Subscribe(value =>
            {
                _ = ReDraw();
            });

            
        }

        CancellationTokenSource createBitmapCancellationTokenSource = new CancellationTokenSource();

        async Task ReDraw()
        {
            DateTime startDate = StartDate;
            DateTime endDate = EndDate;
            int width = (int)grid.Bounds.Width;
            int height = (int)grid.Bounds.Height;

            if (width == 0 || height == 0)
            {
                return;
            }

            createBitmapCancellationTokenSource.Cancel();
            CancellationTokenSource cancellationTokenSource = createBitmapCancellationTokenSource = new CancellationTokenSource();

            (WriteableBitmap? writeableBitmap, List<TextData> textBlocks) = await CreateBitmap(
                startDate, endDate, width, height, createBitmapCancellationTokenSource);

            if(cancellationTokenSource.IsCancellationRequested ||
                startDate != StartDate || endDate != EndDate || 
                writeableBitmap == null)
            {
                return;
            }

            image.Source = writeableBitmap;
            if (ShowLabels)
            {
                canvas.Children.Clear();
                foreach (var td in textBlocks)
                {
                    var tb = new TextBlock()
                    {
                        Text = td.Text,
                        Foreground = Brushes.Black,
                        FontSize = 18
                    };
                    Canvas.SetLeft(tb, td.X);
                    Canvas.SetTop(tb, td.Y);
                    canvas.Children.Add(tb);
                }
            }
        }

        async Task<(WriteableBitmap?, List<TextData>)> CreateBitmap(DateTime startDate,
            DateTime endDate, 
            int width, int height, CancellationTokenSource cancellationTokenSource)
        {
            TaskCompletionSource<(WriteableBitmap?, List<TextData>)> taskCompletionSource =
                new TaskCompletionSource<(WriteableBitmap?, List<TextData>)>();

            _ = Task.Run(delegate
            {
                List<TextData> textBlocks = new List<TextData>();

                try
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        taskCompletionSource.SetResult((null, textBlocks));
                        return;
                    }

                    WriteableBitmap writeableBitmap = new WriteableBitmap(
                        new PixelSize(width, height),
                        new Vector(96, 96),
                        Avalonia.Platform.PixelFormat.Bgra8888,
                        Avalonia.Platform.AlphaFormat.Unpremul);

                    if (ShowLabels)
                    {
                        bool showDayLabels = WriteableBitmapExt.ShowDayLabels(startDate, endDate, width);
                        if (WriteableBitmapExt.ShowDayLines(startDate, endDate, width))
                        {
                            writeableBitmap.DrawDayLines(startDate, endDate, width, textBlocks, Grey, showDayLabels);
                        }
                        writeableBitmap.DrawMonthLines(startDate, endDate, width, textBlocks, Black, !showDayLabels);
                    }

                    taskCompletionSource.SetResult((writeableBitmap, textBlocks));
                }
                catch
                {
                    taskCompletionSource.SetResult((null, textBlocks));
                }
            });

            return await taskCompletionSource.Task;
        }    
    }
}
