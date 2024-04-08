using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Utils
{
    class TextData
    {
        public string Text;
        public int X, Y;
    }

    internal static class WriteableBitmapExt
    {
        public static bool ShowDayLines(DateTime startDate, DateTime endDate, int width)
        {
            return width / (endDate - startDate).TotalDays > 30;
        }

        public static bool ShowDayLabels(DateTime startDate, DateTime endDate, int width)
        {
            return width / (endDate - startDate).TotalDays > 100;
        }

        internal static void DrawMonthLines(this WriteableBitmap writeableBitmap,
            DateTime startDate,
            DateTime endDate,
            int width,
            List<TextData> textBlocks,
            int color,
            bool showLabels)
        {
            var startOfMonth = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(1);
            var lengthTicks = (endDate - startDate).Ticks;
            int x;

            while (startOfMonth < endDate)
            {
                x = (int)(width * (startOfMonth - startDate).Ticks / (double)lengthTicks);
                writeableBitmap.PaintVLine(color, x);
                if (showLabels)
                {
                    textBlocks.Add(new TextData()
                    {
                        Text = startOfMonth.ToString("MMMM"),
                        X = x + 10,
                        Y = 5
                    });
                }
                startOfMonth = startOfMonth.AddMonths(1);
            }
        }

        internal static void DrawDayLines(this WriteableBitmap writeableBitmap,
            DateTime startDate,
            DateTime endDate,
            int width,
            List<TextData> textBlocks,
            int color,
            bool addLabels)
        {
            var startOfDay = new DateTime(startDate.Year, startDate.Month, startDate.Day).AddDays(1);
            var lengthTicks = (endDate - startDate).Ticks;
            int x;

            while (startOfDay < endDate)
            {
                x = (int)(width * (startOfDay - startDate).Ticks / (double)lengthTicks);
                writeableBitmap.PaintVLine(color, x);
                if (addLabels)
                {
                    textBlocks.Add(new TextData()
                    {
                        Text = startOfDay.ToString("dd/MM"),
                        X = x + 10,
                        Y = 5
                    });
                }
                startOfDay = startOfDay.AddDays(1);
            }
        }

        internal static unsafe void PaintRect(this WriteableBitmap writeableBitmap, int color, int x, int y, int width, int height)
        {
            x = Math.Max(0, x);
            y = Math.Max(0, y);

            int x2 = Math.Min(x + width, writeableBitmap.PixelSize.Width);
            int y2 = Math.Min(y + height, writeableBitmap.PixelSize.Height);

            height = y2 - y;
            width = x2 - x;

            if (width <= 0 || height <= 0)
                return;

            using (var buf = writeableBitmap.Lock())
            {
                var ptr = (int*)buf.Address;

                ptr += y * buf.Size.Width + x;

                for (int _y = 0; _y < height; _y++)
                {
                    for (int _x = 0; _x < width; _x++)
                    {
                        *ptr = color;
                        ptr += 1;
                    }
                    ptr += buf.Size.Width - width;
                }
            }
        }

        internal static unsafe void PaintVLine(this WriteableBitmap writeableBitmap, int color, int x)
        {
            writeableBitmap.PaintRect(color, x, 0, 1, (int)writeableBitmap.Size.Height);
        }
    }
}
