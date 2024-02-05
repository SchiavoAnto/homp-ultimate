using System;
using System.Windows;
using System.Globalization;
using System.Windows.Media;

namespace FuckingAroundInWPF.Elements
{
    class LyricsViewer : FrameworkElement
    {
        private Point location;

        private string Text = "";
        private FormattedText formattedText = null!;
        private int position = 0;
        private double dpi;

        public LyricsViewer(string text, double dpi)
        {
            Margin = new Thickness(0d);
            location = TranslatePoint(new Point(0, 0), null);
            this.dpi = dpi;
            SetText(text);
        }

        public void Advance(int step)
        {
            position += step;
            formattedText.SetForegroundBrush(Brushes.Blue, 0, Math.Min(Text.Length, position));
            if (position > Text.Length)
            {
                formattedText.SetForegroundBrush(Brushes.WhiteSmoke, 0, Text.Length);
                position = 0;
            }
            InvalidateVisual();
        }

        public void AdvanceTo(int pos)
        {
            if (pos <= position) return;
            Advance(pos - position);
        }

        public void SetText(string text)
        {
            Text = text;
            formattedText = new FormattedText(
                Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                12,
                Brushes.WhiteSmoke,
                new NumberSubstitution(),
            dpi);
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            formattedText.MaxTextWidth = availableSize.Width;
            formattedText.MaxTextHeight = availableSize.Height;

            return new Size(formattedText.Width, formattedText.Height);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawText(formattedText, location);
        }
    }
}
