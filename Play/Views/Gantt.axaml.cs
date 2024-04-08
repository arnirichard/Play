using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Play.Airline;
using Play.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Play.Views
{
    public partial class Gantt : UserControl
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

        public static readonly StyledProperty<List<TaskItem>?> TasksProperty =
            AvaloniaProperty.Register<Gantt, List<TaskItem>?>(nameof(Tasks));

        public List<TaskItem>? Tasks
        {
            get { return GetValue(TasksProperty); }
            set { SetValue(TasksProperty, value); }
        }

        public Gantt()
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

            this.GetObservable(TasksProperty).Subscribe(value =>
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
            List<TaskItem>? tasks = Tasks;
            int width = (int)grid.Bounds.Width;
            int height = (int)grid.Bounds.Height;

            if (width == 0 || height == 0)
            {
                return;
            }

            createBitmapCancellationTokenSource.Cancel();
            CancellationTokenSource cancellationTokenSource = createBitmapCancellationTokenSource = new CancellationTokenSource();

            WriteableBitmap? writeableBitmap = await CreateBitmap(
                startDate, endDate, tasks, width, height, createBitmapCancellationTokenSource);

            if(cancellationTokenSource.IsCancellationRequested ||
                startDate != StartDate || endDate != EndDate || tasks != Tasks || 
                writeableBitmap == null)
            {
                return;
            }

            image.Source = writeableBitmap;
        }

        async Task<WriteableBitmap?> CreateBitmap(DateTime startDate,
            DateTime endDate, List<TaskItem>? tasks,
            int width, int height, CancellationTokenSource cancellationTokenSource)
        {
            TaskCompletionSource<WriteableBitmap?> taskCompletionSource =
                new TaskCompletionSource<WriteableBitmap?>();

            _ = Task.Run(delegate
            {
                try
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        taskCompletionSource.SetResult(null);
                        return;
                    }

                    WriteableBitmap writeableBitmap = new WriteableBitmap(
                        new PixelSize(width, height),
                        new Vector(96, 96),
                        Avalonia.Platform.PixelFormat.Bgra8888,
                        Avalonia.Platform.AlphaFormat.Unpremul);

                    int blockHeight = height - 4;
                    List<TextData> textBlocks = new List<TextData>();

                    if (tasks != null && startDate > DateTime.MinValue && endDate > startDate && endDate < DateTime.MaxValue)
                    {
                        TaskItem taskItem;
                        int left, right;
                        double period = (endDate - startDate).TotalSeconds;

                        for (int i = 0; i < tasks.Count; i++)
                        {
                            taskItem = tasks[i];

                            if(cancellationTokenSource.IsCancellationRequested)
                            {
                                taskCompletionSource.SetResult(null);
                                return;
                            }

                            if (taskItem.EndTime < startDate ||
                                taskItem.StartTime > endDate)
                            {
                                continue;
                            }

                            left = (int)((taskItem.StartTime - startDate).TotalSeconds*width/period);   
                            right = (int)((taskItem.EndTime - startDate).TotalSeconds*width/period);

                            if(left < 0)
                            {
                                left = 0;
                            }
                            if(right > width-1)
                            {
                                right = width-1;
                            }

                            writeableBitmap.PaintRect(GetColor(taskItem.Type), left, 2, right - left, blockHeight);
                        }
                    }

                    if (WriteableBitmapExt.ShowDayLines(startDate, endDate, width))
                    {
                        writeableBitmap.DrawDayLines(startDate, endDate, width, textBlocks, Grey,
                            false);
                    }
                    writeableBitmap.DrawMonthLines(startDate, endDate, width, textBlocks, Black, false);

                    taskCompletionSource.SetResult(writeableBitmap);
                }
                catch
                {
                    taskCompletionSource.SetResult(null);
                }
            });

            return await taskCompletionSource.Task;
        }

        static int GetColor(TaskType taskType)
        {
            switch(taskType)
            {
                case TaskType.Flight:
                    return Blue;
                case TaskType.StandBy:
                    return LightBlue;
                case TaskType.Vacation:
                    return Green;
                case TaskType.Training:
                default:
                    return Orange;
            }
        }
    }
}
