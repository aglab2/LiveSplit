using LiveSplit.Model;
using LiveSplit.TimeFormatters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public class Timer : IComponent
    {
        public SimpleLabel BigTextLabel { get; set; }
        public SimpleLabel SmallTextLabel { get; set; }
        protected SimpleLabel BigMeasureLabel { get; set; }
        protected ShortTimeFormatter Formatter { get; set; }

        private DeathCountLabel DeathsLabel { get; set; }
        private DeathCountLabel MeasureDeathsLabel { get; set; }

        protected Font TimerDecimalPlacesFont { get; set; }
        protected Font TimerFont { get; set; }
        protected float PreviousDecimalsSize { get; set; }

        public Color TimerColor = Color.Transparent;
        public Color DeathCountColor = Color.Transparent;

        protected TimeAccuracy CurrentAccuracy { get; set; }
        protected TimeFormat CurrentTimeFormat { get; set; }

        public GraphicsCache Cache { get; set; }

        public TimerSettings Settings { get; set; }
        public float ActualWidth { get; set; }

        public string ComponentName => "Timer";

        public float VerticalHeight => Settings.TimerHeight;

        public float MinimumWidth => 20;

        public float HorizontalWidth => Settings.TimerWidth;

        public float MinimumHeight => 20;

        public float PaddingTop => 0f;
        public float PaddingLeft => 7f;
        public float PaddingBottom => 0f;
        public float PaddingRight => 7f;

        public IDictionary<string, Action> ContextMenuControls => null;

        public Timer()
        {
            BigTextLabel = new SimpleLabel()
            {
                HorizontalAlignment = StringAlignment.Far,
                VerticalAlignment = StringAlignment.Near,
                Width = 493,
                Text = "0",
            };

            SmallTextLabel = new SimpleLabel()
            {
                HorizontalAlignment = StringAlignment.Near,
                VerticalAlignment = StringAlignment.Near,
                Width = 257,
                Text = "0",
            };

            DeathsLabel = new DeathCountLabel()
            {
                Text = "0",
                Width = 400,
                IsMonospaced = true,
                HorizontalAlignment = StringAlignment.Near,
                VerticalAlignment = StringAlignment.Near
            };

            MeasureDeathsLabel = new DeathCountLabel()
            {
                Text = "9999",
                IsMonospaced = true
            };

            BigMeasureLabel = new SimpleLabel()
            {
                Text = "88:88:88",
                IsMonospaced = true
            };

            Formatter = new ShortTimeFormatter();
            Settings = new TimerSettings();
            UpdateTimeFormat();
            Cache = new GraphicsCache();
            TimerColor = Color.Transparent;
            DeathCountColor = Color.Transparent;
        }

        public static void DrawBackground(Graphics g, Color timerColor, Color settingsColor1, Color settingsColor2, 
            float width, float height, DeltasGradientType gradientType)
        {
            var background1 = settingsColor1;
            var background2 = settingsColor2;
            if (gradientType == DeltasGradientType.PlainWithDeltaColor
                || gradientType == DeltasGradientType.HorizontalWithDeltaColor
                || gradientType == DeltasGradientType.VerticalWithDeltaColor)
            {
                double h, s, v;
                timerColor.ToHSV(out h, out s, out v);
                var newColor = ColorExtensions.FromHSV(h, s * 0.5, v * 0.25);

                if (gradientType == DeltasGradientType.PlainWithDeltaColor)
                {
                    background1 = Color.FromArgb(timerColor.A * 7 / 12, newColor);
                }
                else
                {
                    background1 = Color.FromArgb(timerColor.A / 6, newColor);
                    background2 = Color.FromArgb(timerColor.A, newColor);
                }
            }
            if (background1.A > 0
            || gradientType != DeltasGradientType.Plain
            && background2.A > 0)
            {
                var gradientBrush = new LinearGradientBrush(
                            new PointF(0, 0),
                            gradientType == DeltasGradientType.Horizontal
                            || gradientType == DeltasGradientType.HorizontalWithDeltaColor
                            ? new PointF(width, 0)
                            : new PointF(0, height),
                            background1,
                            gradientType == DeltasGradientType.Plain
                            || gradientType == DeltasGradientType.PlainWithDeltaColor
                            ? background1
                            : background2);
                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }
        }

        private void DrawGeneral(Graphics g, LiveSplitState state, float width, float height)
        {
            DrawBackground(g, TimerColor, Settings.BackgroundColor, Settings.BackgroundColor2, width, height, Settings.BackgroundGradient);

            if (state.LayoutSettings.TimerFont != TimerFont || Settings.DecimalsSize != PreviousDecimalsSize)
            {
                TimerFont = state.LayoutSettings.TimerFont;
                TimerDecimalPlacesFont = new Font(TimerFont.FontFamily.Name, (TimerFont.Size / 50f) * (Settings.DecimalsSize), TimerFont.Style, GraphicsUnit.Pixel);
                PreviousDecimalsSize = Settings.DecimalsSize;
            }

            BigTextLabel.Font = BigMeasureLabel.Font = TimerFont;
            DeathsLabel.Font = MeasureDeathsLabel.Font = SmallTextLabel.Font = TimerDecimalPlacesFont;
            DeathsLabel.DeathIcon = state.Settings.DeathIcon;

            BigMeasureLabel.SetActualWidth(g);
            SmallTextLabel.SetActualWidth(g);
            MeasureDeathsLabel.SetActualWidth(g);
            MeasureDeathsLabel.DeathIcon = state.Settings.DeathIcon;
            MeasureDeathsLabel.ActualWidth += state.Settings.DeathIcon.Width;

            var oldMatrix = g.Transform;
            var unscaledWidth = Math.Max(10, MeasureDeathsLabel.ActualWidth + BigMeasureLabel.ActualWidth + SmallTextLabel.ActualWidth + 17);
            var unscaledHeight = 45f;
            var widthFactor = (width - 14) / (unscaledWidth - 14);
            var heightFactor = height / unscaledHeight;
            var adjustValue = !Settings.CenterTimer ? 7f : 0f;
            var scale = Math.Min(widthFactor, heightFactor);
            g.TranslateTransform(width - adjustValue, height / 2);
            g.ScaleTransform(scale, scale);
            g.TranslateTransform(-unscaledWidth + adjustValue, -0.5f * unscaledHeight);
            if (Settings.CenterTimer)
                g.TranslateTransform((-(width - unscaledWidth * scale) / 2f) / scale, 0);
            DrawUnscaled(g, state, unscaledWidth, unscaledHeight, oldMatrix);
            ActualWidth = scale * (SmallTextLabel.ActualWidth + BigTextLabel.ActualWidth + DeathsLabel.ActualWidth);
        }

        private void GetGradientColors( Color baseColor, out Color topColor, out Color bottomColor )
        {
            double h, s, v;
            baseColor.ToHSV(out h, out s, out v);

            bottomColor = ColorExtensions.FromHSV(h, s, 0.8 * v);
            topColor = ColorExtensions.FromHSV(h, 0.5 * s, Math.Min(1, 1.5 * v + 0.1));
        }

        public void DrawUnscaled(Graphics g, LiveSplitState state, float width, float height, Matrix oldMatrix)
        {
            BigTextLabel.ShadowColor = state.LayoutSettings.ShadowsColor;
            BigTextLabel.OutlineColor = state.LayoutSettings.TextOutlineColor;
            BigTextLabel.HasShadow = state.LayoutSettings.DropShadows;
            SmallTextLabel.ShadowColor = state.LayoutSettings.ShadowsColor;
            SmallTextLabel.OutlineColor = state.LayoutSettings.TextOutlineColor;
            SmallTextLabel.HasShadow = state.LayoutSettings.DropShadows;
            DeathsLabel.ShadowColor = state.LayoutSettings.ShadowsColor;
            DeathsLabel.OutlineColor = state.LayoutSettings.TextOutlineColor;
            DeathsLabel.HasShadow = state.LayoutSettings.DropShadows;

            UpdateTimeFormat();

            var smallFont = TimerDecimalPlacesFont;
            var bigFont = TimerFont;
            var sizeMultiplier = bigFont.Size / bigFont.FontFamily.GetEmHeight(bigFont.Style);
            var smallSizeMultiplier = smallFont.Size / bigFont.FontFamily.GetEmHeight(bigFont.Style);
            var ascent = sizeMultiplier * bigFont.FontFamily.GetCellAscent(bigFont.Style);
            var descent = sizeMultiplier * bigFont.FontFamily.GetCellDescent(bigFont.Style);
            var smallAscent = smallSizeMultiplier * smallFont.FontFamily.GetCellAscent(smallFont.Style);
            var shift = (height - ascent - descent) / 2f;

            BigTextLabel.X = width - 499 - SmallTextLabel.ActualWidth;
            SmallTextLabel.X = width - SmallTextLabel.ActualWidth - 6;
            DeathsLabel.X = 8;
            BigTextLabel.Y = shift;
            SmallTextLabel.Y = shift + ascent - smallAscent;
            DeathsLabel.Y = shift + ascent - smallAscent - 4;
            BigTextLabel.Height = 150f;
            SmallTextLabel.Height = 150f;
            DeathsLabel.Height = 37f;

            BigTextLabel.IsMonospaced = true;
            SmallTextLabel.IsMonospaced = true;
            DeathsLabel.IsMonospaced = true;

            if (Settings.ShowGradient && BigTextLabel.Brush is SolidBrush && DeathsLabel.Brush is SolidBrush)
            {
                Color topColor;
                Color bottomColor;

                var originalTimeColor = (BigTextLabel.Brush as SolidBrush).Color;
                GetGradientColors( originalTimeColor, out topColor, out bottomColor );
                var bigTimerGradiantBrush = new LinearGradientBrush(
                    new PointF(BigTextLabel.X, BigTextLabel.Y),
                    new PointF(BigTextLabel.X, BigTextLabel.Y + ascent + descent),
                    topColor,
                    bottomColor);
                var smallTimerGradiantBrush = new LinearGradientBrush(
                    new PointF(SmallTextLabel.X, SmallTextLabel.Y),
                    new PointF(SmallTextLabel.X, SmallTextLabel.Y + ascent + descent + smallFont.Size - bigFont.Size),
                    topColor,
                    bottomColor);

                BigTextLabel.Brush = bigTimerGradiantBrush;
                SmallTextLabel.Brush = smallTimerGradiantBrush;

                var originalDeathsColor = (DeathsLabel.Brush as SolidBrush).Color;
                GetGradientColors( originalDeathsColor, out topColor, out bottomColor );
                var deathsGradientBrush = new LinearGradientBrush(
                    new PointF(DeathsLabel.X, DeathsLabel.Y),
                    new PointF(DeathsLabel.X, DeathsLabel.Y + ascent + descent + smallFont.Size - bigFont.Size),
                    topColor,
                    bottomColor);

                DeathsLabel.Brush = deathsGradientBrush;
            }

            BigTextLabel.Draw(g);
            SmallTextLabel.Draw(g);
            g.TranslateTransform( -g.Transform.OffsetX / g.Transform.Elements[0], 0 );
            DeathsLabel.Draw(g);
            g.Transform = oldMatrix;
        }

        protected void UpdateTimeFormat()
        {
            if (Settings.DigitsFormat == "1")
                CurrentTimeFormat = TimeFormat.Seconds;
            else if (Settings.DigitsFormat == "00:01")
                CurrentTimeFormat = TimeFormat.Minutes;
            else if (Settings.DigitsFormat == "0:00:01")
                CurrentTimeFormat = TimeFormat.Hours;
            else
                CurrentTimeFormat = TimeFormat.TenHours;

            if (Settings.Accuracy == ".23")
                CurrentAccuracy = TimeAccuracy.Hundredths;
            else if (Settings.Accuracy == ".2")
                CurrentAccuracy = TimeAccuracy.Tenths;
            else
                CurrentAccuracy = TimeAccuracy.Seconds;
        }

        public virtual TimeSpan? GetTime(LiveSplitState state, TimingMethod method)
        {
            if (state.CurrentPhase == TimerPhase.NotRunning)
                return state.Run.Offset;
            else
                return state.CurrentTime[method];
        }

        private int GetDeathCount(LiveSplitState state)
        {
            return state.Run.CurrentDeathCount;
        }

        private int GetPbDeathCount(LiveSplitState state)
        {
            int result = 0;
            
            ISegment currentParent = null;
            if (state.CurrentSplitIndex < state.Run.Count )
            {
                if (state.CurrentSplit.PersonalBestDeathCount != -1)
                {
                    result += state.CurrentSplit.PersonalBestDeathCount;
                }
                else
                {
                    return -1;
                }
                currentParent = state.CurrentSplit.Parent;
            }
            
            for (int i = 0; i < state.CurrentSplitIndex; i++) {
                var split = state.Run[i];
                if (( split.Parent == currentParent || split.Parent == null ) && split.PersonalBestDeathCount != -1) {
                    result += split.PersonalBestDeathCount;
                }
            }

            return result;
        }

        private int GetBestDeathCount(LiveSplitState state)
        {
            return state.Run.BestDeathCount;
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            DrawGeneral(g, state, width, VerticalHeight);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            DrawGeneral(g, state, HorizontalWidth, height);
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        private Color GetActiveDeathColor( LiveSplitState state, int deaths, int pbDeaths, Color pbColor )
        {
            return ( pbDeaths == -1 || pbDeaths >= deaths ) ? pbColor : state.LayoutSettings.BehindLosingTimeColor;
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            Cache.Restart();

            var timingMethod = state.CurrentTimingMethod;
            if (Settings.TimingMethod == "Real Time")
                timingMethod = TimingMethod.RealTime;
            else if (Settings.TimingMethod == "Game Time")
                timingMethod = TimingMethod.GameTime;

            var timeValue = GetTime(state, timingMethod);

            if (timeValue == null && timingMethod == TimingMethod.GameTime)
                timeValue = GetTime(state, TimingMethod.RealTime);

            if (timeValue != null)
            {
                var timeString = Formatter.Format(timeValue, CurrentTimeFormat);
                int dotIndex = timeString.IndexOf(".");
                BigTextLabel.Text = timeString.Substring(0, dotIndex);
                if (CurrentAccuracy == TimeAccuracy.Hundredths)
                    SmallTextLabel.Text = timeString.Substring(dotIndex);
                else if (CurrentAccuracy == TimeAccuracy.Tenths)
                    SmallTextLabel.Text = timeString.Substring(dotIndex, 2);
                else
                    SmallTextLabel.Text = "";
            }
            else
            {
                SmallTextLabel.Text = TimeFormatConstants.DASH;
                BigTextLabel.Text = "";
            }

            var deathCount = GetDeathCount( state );
            DeathsLabel.Text = deathCount.ToString();

            if ( state.CurrentPhase == TimerPhase.NotRunning ) {
                DeathCountColor = state.LayoutSettings.NotRunningColor;
            } else if( state.CurrentPhase == TimerPhase.Ended ) {
                var pbDeathCount = GetPbDeathCount(state);
                var bestDeathCount = GetBestDeathCount(state);
                DeathCountColor = ( bestDeathCount == -1 || bestDeathCount > deathCount ) 
                    ? LiveSplitStateHelper.GetBestSegmentColor( state ) 
                    : GetActiveDeathColor(state, deathCount, pbDeathCount, state.LayoutSettings.PersonalBestColor);
            } else {
                var pbDeathCount = GetPbDeathCount(state);
                DeathCountColor = GetActiveDeathColor(state, deathCount, pbDeathCount, state.LayoutSettings.AheadGainingTimeColor);
            }

            if (state.CurrentPhase == TimerPhase.NotRunning || state.CurrentTime[timingMethod] < TimeSpan.Zero)
            {
                TimerColor = state.LayoutSettings.NotRunningColor;
            }
            else if (state.CurrentPhase == TimerPhase.Paused)
            {
                TimerColor = state.LayoutSettings.PausedColor;
            }
            else if (state.CurrentPhase == TimerPhase.Ended)
            {
                if (state.Run.Last().Comparisons[state.CurrentComparison][timingMethod] == null || state.CurrentTime[timingMethod] < state.Run.Last().Comparisons[state.CurrentComparison][timingMethod])
                {
                    TimerColor = state.LayoutSettings.PersonalBestColor;
                }
                else
                {
                    TimerColor = state.LayoutSettings.BehindLosingTimeColor;
                }
            }
            else if (state.CurrentPhase == TimerPhase.Running)
            {
                if (state.CurrentSplit.Comparisons[state.CurrentComparison][timingMethod] != null)
                {
                    TimerColor = LiveSplitStateHelper.GetSplitColor(state, state.CurrentTime[timingMethod] - state.CurrentSplit.Comparisons[state.CurrentComparison][timingMethod],
                        state.CurrentSplitIndex, true, false, state.CurrentComparison, timingMethod)
                        ?? state.LayoutSettings.AheadGainingTimeColor;
                }
                else
                    TimerColor = state.LayoutSettings.AheadGainingTimeColor;
            }

            if (Settings.OverrideSplitColors)
            {
                BigTextLabel.ForeColor = Settings.TimerColor;
                SmallTextLabel.ForeColor = Settings.TimerColor;
                DeathsLabel.ForeColor = Settings.TimerColor;
            }
            else
            {
                BigTextLabel.ForeColor = TimerColor;
                SmallTextLabel.ForeColor = TimerColor;
                DeathsLabel.ForeColor = DeathCountColor;
            }

            Cache["TimerText"] = BigTextLabel.Text + SmallTextLabel.Text;
            if (BigTextLabel.Brush != null && invalidator != null)
            {
                Cache["TimerColor"] = BigTextLabel.ForeColor.ToArgb();
            }

            if (invalidator != null && Cache.HasChanged)
            {
                invalidator.Invalidate(0, 0, width, height);
            }
        }

        public void Dispose()
        {
        }

        public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
    }
}
