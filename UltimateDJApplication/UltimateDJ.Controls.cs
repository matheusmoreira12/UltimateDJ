using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.ComponentModel;
using System.Globalization;

using NAudio;

using UltimateDJ.Fonts;
using UltimateDJ.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace UltimateDJ.Controls
{
    static class ThemeManager
    {
    }

    class CustomBufferedGraphicsManager
    {
        public static CustomBufferedGraphicsManager Current = new CustomBufferedGraphicsManager();

        /// <summary>
        /// Allocates a byte array for managed bitmaps.
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public CustomBufferedGraphics Allocate(int bitmapWidth, int bitmapHeight, System.Drawing.Imaging.PixelFormat bitmapPixelFormat)
        {
            //return the allocated buffered graphics
            return new CustomBufferedGraphics(bitmapWidth, bitmapHeight, bitmapPixelFormat);
        }
    }

    class CustomBufferedGraphics : IDisposable
    {
        /// <summary>
        /// Gets the number of bits per pixel for a specific PixelFormat.
        /// </summary>
        /// <param name="bitmapPixelFormat">The specified PixelFormat.</param>
        /// <returns>An Int indicating the number of bits per pixel</returns>
        public static int GetBitsPerPixel(System.Drawing.Imaging.PixelFormat bitmapPixelFormat)
        {
            return ((int)bitmapPixelFormat & 0xff00) >> 8;
        }

        /// <summary>
        /// Returns the stride for the specified pixel format and width.
        /// </summary>
        /// <param name="bitmapPixelFormat">The desired pixel format.</param>
        /// <param name="width">The desired width.</param>
        /// <returns>The resulting stride.</returns>
        public static int GetStride(System.Drawing.Imaging.PixelFormat bitmapPixelFormat, int bitmapWidth)
        {
            int bytesPerPixel = (GetBitsPerPixel(bitmapPixelFormat) + 7) / 8;
            return 4 * ((bitmapWidth * bytesPerPixel + 3) / 4);
        }

        /// <summary>
        /// Returns the final allocated space in memory in bytes.
        /// </summary>
        /// <param name="control">The final control to which the content will be rendered.</param>
        /// <param name="bitmapPixelFormat">The desired pixel format.</param>
        /// <returns></returns>
        public static int GetPreviewLength(int bitmapWidth, int bitmapHeight, System.Drawing.Imaging.PixelFormat bitmapPixelFormat)
        {
            //get stride and output the total length in bits
            return GetStride(bitmapPixelFormat, bitmapWidth) * bitmapHeight;
        }

        public bool Disposed { get; private set; } = false;

        public byte[] PixelBuffer { get; private set; }
        private GCHandle pixelBufferHandle;

        private Image managedBitmap = null;

        internal CustomBufferedGraphics(int bitmapWidth, int bitmapHeight, System.Drawing.Imaging.PixelFormat bitmapPixelFormat)
        {
            //preview the buffer length
            int prevLength = GetPreviewLength(bitmapWidth, bitmapHeight, bitmapPixelFormat);
            PixelBuffer = new byte[prevLength];

            //allocate a pointer for the new buffer
            pixelBufferHandle = GCHandle.Alloc(PixelBuffer, GCHandleType.Pinned);

            int stride = GetStride(bitmapPixelFormat, bitmapWidth); //stride for the managed bitmap

            //create the managed bitmap
            managedBitmap = new Bitmap(bitmapWidth, bitmapHeight, stride, bitmapPixelFormat, pixelBufferHandle.AddrOfPinnedObject());
        }

        public Graphics GetNewGraphicsContext()
        {
            //associate graphics with the managed bitmap
            return Graphics.FromImage(managedBitmap);
        }

        /// <summary>
        /// Renders this buffer to Graphics.
        /// </summary>
        /// <param name="graphics">Destination Graphics</param>
        /// <param name="dest">Destination coordinates</param>
        public void Render(Graphics graphics, PointF dest)
        {
            Object renderLock = new Object();
            lock (renderLock)
            {
                if (graphics == null || managedBitmap == null)
                    return;

                graphics.DrawImage(managedBitmap, dest);
            }
        }

        public void Dispose()
        {
            Disposed = true;
            pixelBufferHandle.Free();
        }
    }

    class DataFormatter
    {
        public static CultureInfo CultureInfo = CultureInfo.InvariantCulture;
        /// <summary>
        /// Convert millisseconds to the UltimateDJ default interface time format.
        /// </summary>
        /// <param name="millis">Amount of time in millisseconds to be converted.</param>
        /// <returns>The value converted into formatted string.</returns>
        public static string DefaultTimeFormat(int millis)
        {
            //get a flag indicating wether time is negative or not
            bool negative = millis < 0;
            //set time to its absolute
            millis = Math.Abs(millis);
            //get secs
            int secs = (millis / 1000);
            //get mins
            int mins = secs / 60;
            //limit millis
            millis = millis % 1000;
            //limit secs
            secs = secs % 60;
            string fmt = "00";
            return (negative ? "-" : "") + mins.ToString(fmt, CultureInfo) + ":" + secs.ToString(fmt, CultureInfo) + "." + millis / 100;
        }
        /// <summary>
        /// Convert pitch percentage to the UltimateDJ default interface pitch format.
        /// </summary>
        /// <param name="pitch">Pitch shift in positive or negative % between original and set pitches.</param>
        /// <returns>The value converted into formatted string.</returns>
        public static string DefaultPitchFormat(float pitch)
        {
            var result = "";
            if (pitch >= 10 || pitch <= -10)
                result = pitch.ToString("0", CultureInfo);
            else
                result = pitch.ToString("0.0", CultureInfo);
            if (pitch > 0)
                result = "+" + result;
            return result;
        }
    }

    static class CustomControlShapes
    {
        public static GraphicsPath GetRectShape(float width, float height, float margin = 0)
        {
            var path = new GraphicsPath();
            float m = margin,
                m2 = m * 2;
            path.AddRectangle(new RectangleF(0 - m, 0 - m, width + m2, height + m2));
            return path;
        }

        public static GraphicsPath GetRoundShape(float width, float height, float margin = 0)
        {
            var path = new GraphicsPath();
            float m = margin,
                m2 = m * 2;
            path.AddEllipse(0 - m, 0 - m, width + m2, height + m2);
            return path;
        }

        public static GraphicsPath GetRingShape(float width, float height, float margin = 0)
        {
            var path = new GraphicsPath();
            float m = margin,
                m2 = m * 2;
            path.AddEllipse(0 - m, 0 - m, width + m2, height + m2);
            path.AddEllipse(width * .083f + m, height * .083f + m, width * .834f - m2, height * .834f - m2);
            return path;
        }

        public static GraphicsPath GetRoundRectShape(float width, float height, float rx, float margin = 0)
        {
            var path = new GraphicsPath();
            float m = margin,
                m2 = m * 2;
            if (height > 0 && width > 0)
            {
                path.AddArc(0 - m, 0 - m, rx + m2, height + m2, -90, -180);
                path.AddArc(width - rx, 0 - m, height + m2, height + m2, 90, -180);
            }
            return path;
        }

        public static PathGradientBrush GetRadialGradientBrush(RectangleF rect, Color[] colors, Color centerColor)
        {
            // Create a path that consists of a single ellipse.
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(rect);

            // Use the path to construct a brush.
            PathGradientBrush pthGrBrush = new PathGradientBrush(path);

            // Set the color along the entire boundary 
            // of the path to aqua.
            pthGrBrush.SurroundColors = colors;

            pthGrBrush.CenterColor = centerColor;

            return pthGrBrush;
        }
    }

    static class ControlExtension
    {
        public static bool RendererShouldSkip(this Control control, Rectangle screenBounds, Rectangle? clipBounds = null)
        {
            Rectangle controlScreenBounds = new Rectangle(control.PointToScreen(Point.Empty), control.Size);

            bool sizeIsZero = control.Width == 0 || control.Height == 0,
                insideScreenBounds = controlScreenBounds.IntersectsWith(screenBounds),
                insideClipBounds = clipBounds == null ? true : controlScreenBounds.IntersectsWith((Rectangle)clipBounds);

            return !control.Visible || sizeIsZero || !insideScreenBounds || !insideClipBounds;
        }

        internal static void RenderToGraphics(this Control control, Graphics destGraphics, Point offset, int index = -1)
        {
            destGraphics.SetClip(new Rectangle(offset, control.Size));

            var custControl = control as CustomControl;
            Rectangle controlScreenBounds = new Rectangle(control.PointToScreen(Point.Empty), control.Size);

            if (custControl != null)
                custControl.RenderingBufferedGraphics.Render(destGraphics, offset);

            for (int i = control.Controls.Count - 1; i > index; i--)
            {
                var childControl = control.Controls[i];
                Point newOffset = new Point(offset.X + childControl.Left, offset.Y + childControl.Top);
                if (!childControl.RendererShouldSkip(Screen.GetBounds(childControl), controlScreenBounds))
                    childControl.RenderToGraphics(destGraphics, newOffset);
            }
        }
    }

    enum HorizontalCoordinateMode
    {
        Left,
        Right
    }

    enum VerticalCoordinateMode
    {
        Top,
        Bottom
    }

    internal class CustomControl : Control
    {
        /// <summary>
        /// Tests a Control object instance against type compatibility.
        /// </summary>
        /// <param name="instance">The tested Control object instance.</param>
        /// <returns></returns>
        public static Boolean Test(object instance)
        {
            return instance as CustomControl != null;
        }

        internal CustomBufferedGraphics RenderingBufferedGraphics;

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public float FontSize
        {
            get
            {
                return Font.Size;
            }
            set
            {
                Font = new Font(Font.FontFamily, value);
                OnPropertyChanged("FontSize");
            }
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get
            {
                return Font.Style;
            }
            set
            {
                Font = new Font(Font.FontFamily, Font.Size, value);
                OnPropertyChanged("FontStyle");
            }
        }

        [Category("Position")]
        public new int Left
        {
            get
            {
                return Location.X;
            }
            set
            {
                horizontalCoordinateMode = HorizontalCoordinateMode.Left;
                Location = new Point(value, Location.Y);
            }
        }

        [Category("Position")]
        public new int Top
        {
            get
            {
                return Location.Y;
            }
            set
            {
                horizontalCoordinateMode = HorizontalCoordinateMode.Left;
                Location = new Point(Location.X, value);
            }
        }

        private void computeSecondaryCoordinates()
        {
            if (horizontalCoordinateMode == HorizontalCoordinateMode.Right)
                Location = new Point(Parent.Width - (Width + Right), Location.Y);

            if (verticalCoordinateMode == VerticalCoordinateMode.Bottom)
                Location = new Point(Location.X, Parent.Height - (Height + Bottom));
        }

        /// <summary>
        /// Gets or sets the Right coordinate relative to the Parent element Bottom side.
        /// </summary>
        [Category("Position")]
        public new int Right
        {
            get
            {
                return _right;
            }
            set
            {
                _right = value;
                horizontalCoordinateMode = HorizontalCoordinateMode.Right;
                computeSecondaryCoordinates();
            }
        }
        private int _right = 0;

        /// <summary>
        /// Gets or sets the Bottom coordinate relative to the Parent element Bottom side.
        /// </summary>
        [Category("Position")]
        public new int Bottom
        {
            get
            {
                return _bottom;
            }
            set
            {
                _bottom = value;
                verticalCoordinateMode = VerticalCoordinateMode.Bottom;
                computeSecondaryCoordinates();
            }
        }
        private int _bottom = 0;

        private HorizontalCoordinateMode horizontalCoordinateMode = HorizontalCoordinateMode.Left;
        private VerticalCoordinateMode verticalCoordinateMode = VerticalCoordinateMode.Top;

        /// <summary>
        /// Gets a new rendering shape.
        /// </summary>
        /// <param name="margin"></param>
        /// <returns>A GraphicsPath representing the control shape.</returns>
        protected virtual GraphicsPath GetRenderShape(float margin = 0)
        {
            return CustomControlShapes.GetRectShape(Width, Height, margin);
        }

        /// <summary>
        /// Control's rendering shape.
        /// </summary>
        private GraphicsPath renderShape = null;

        /// <summary>
        /// Get rendering region for this control according to renderShape.
        /// </summary>
        /// <returns>The region to which render the control image.</returns>
        protected virtual Region GetRenderRegion()
        {
            renderShape = GetRenderShape();
            return new Region(GetRenderShape(1));
        }

        //MANAGE PROPERTY CHANGES
        // Declare the event
        /// <summary>
        /// Event for property change.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        /// <summary>
        /// Trigger for the property change event.
        /// </summary>
        /// <param name="name">Property name.</param>
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Handles the property changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void PropertyChangedHandler(object sender, PropertyChangedEventArgs e) { }

        //BRING UP INTERNAL PROPERTY CHANGES
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            OnPropertyChanged("Text");
        }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            OnPropertyChanged("Font");
        }
        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            OnPropertyChanged("ForeColor");
        }
        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            OnPropertyChanged("BackColor");
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            DoDragDrop(this, DragDropEffects.All);
        }

        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            base.OnDragEnter(drgevent);

            if (drgevent.Data == (object)this)
                drgevent.Effect = DragDropEffects.All;

            UpdateBounds();
        }

        /// <summary>
        /// This event must be overriden instead of the native OnPaint event.
        /// Repaints the CustomControl using accelerated graphics.
        /// </summary>
        /// <param name="e">New PaintEventArgs with Graphics drawing directly to the internal render Bitmap.</param>
        protected virtual void PreRendering(Graphics targetGraphics)
        {
            targetGraphics.SetClip(Region, CombineMode.Replace);
            targetGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            targetGraphics.TextRenderingHint = TextRenderingHint.AntiAlias;

            targetGraphics.FillPath(new SolidBrush(BackColor), renderShape);

            if (Focused)
            {
                var pen = new Pen(Color.FromArgb(127, Color.DarkOrange), 5);
                pen.Alignment = PenAlignment.Inset;
                targetGraphics.DrawPath(pen, renderShape);
            }
        }

        //intercept invalidation and render next frame
        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            base.OnInvalidated(e);

            //if there's no rendering region or the invalid rect is not touching it, return here.
            if (Region == null || !DisplayRectangle.IntersectsWith(e.InvalidRect))
                return;

            Graphics bufferGraphics = RenderingBufferedGraphics.GetNewGraphicsContext();

            bufferGraphics.Clear(Color.Transparent);

            PreRendering(bufferGraphics);

            bufferGraphics.Dispose();
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);

            if (Parent != null)
                Parent.RenderToGraphics(pevent.Graphics, new Point(-Left, -Top), Parent.Controls.IndexOf(this));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            RenderingBufferedGraphics.Render(e.Graphics, PointF.Empty);
        }

        private void reallocateRenderingBuffer()
        {
            //if the dimensions are invalid, do nothing and return here
            if (Width <= 0 || Height <= 0)
                return;

            //if there was already a buffered graphics object, dispose it
            if (RenderingBufferedGraphics != null)
                RenderingBufferedGraphics.Dispose();

            //allocate a new buffered graphics object
            RenderingBufferedGraphics = CustomBufferedGraphicsManager.Current.Allocate(this.Width, this.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            //if control was already created, invalidate it
            if (Created)
                UpdateBounds();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Region = GetRenderRegion();

            reallocateRenderingBuffer();

            computeSecondaryCoordinates();
        }

        /// <summary>
        /// Initializes an instance of the CustomControl class.
        /// </summary>
        protected CustomControl() : base()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;

            Width = 100;
            Height = 100;

            AllowDrop = true;
            TabStop = false;
            ResizeRedraw = true;

            PropertyChanged += new PropertyChangedEventHandler(PropertyChangedHandler);

            reallocateRenderingBuffer();
        }
    }

    internal class LevelIndicator : CustomControl
    {
        public int Minimum
        {
            get { return _minimum; }
            set
            {
                if (value < _maximum)
                    _minimum = value;
                if (_value < _minimum)
                    _value = _minimum;
                OnPropertyChanged("Minimum");
            }
        }
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                if (value > 0)
                    _maximum = value;
                if (_value > _maximum)
                    _value = _maximum;
                OnPropertyChanged("Maximum");
            }
        }
        public int Value
        {
            get { return _value; }
            set
            {
                if (value >= _minimum && value <= _maximum)
                    _value = value;
                OnPropertyChanged("Value");
                Refresh();
            }
        }
        public int Step
        {
            get { return _step; }
            set
            {
                if (value > 0 && value <= Maximum) _step = value;
                OnPropertyChanged("Step");
            }
        }
        private int _minimum = 0;
        private int _maximum = 100;
        private int _value = 0;
        private int _step = 1;
        public float ValueAsFloat
        {
            get
            {
                return (float)(Value - Minimum) / (Maximum - Minimum);
            }
        }
        private void step(bool upDown)
        {
            if (upDown)
                Value += Step;
            else
                Value -= Step;
        }
        public void StepUp()
        {
            step(true);
        }
        public void StepDown()
        {
            step(false);
        }
        protected override void PreRendering(Graphics targetGraphics)
        {
            base.PreRendering(targetGraphics);
        }
        protected LevelIndicator() : base()
        {
            ForeColor = Color.DarkOrange;
            BackColor = Color.Black;
        }
    }

    class LinearLevelIndicator : LevelIndicator
    {
        protected override void PreRendering(Graphics targetGraphics)
        {
            base.PreRendering(targetGraphics);
            if (Height > 0 && Width > 0)
            {
                var path = new GraphicsPath();
                path.AddArc(0, 0, Height, Height, -90, -180);
                path.AddArc((Width - Height) * ValueAsFloat, 0, Height, Height, 90, -180);
                targetGraphics.FillPath(new SolidBrush(ForeColor), path);
            }
        }

        protected override GraphicsPath GetRenderShape(float margin = 0)
        {
            return CustomControlShapes.GetRoundRectShape(Width, Height, Height / 2);
        }

        public LinearLevelIndicator() : base()
        {
            Width = 150;
            Height = 6;
        }
    }

    class ProgressBar : LinearLevelIndicator
    {
        public ProgressBar() : base()
        {

        }
    }

    class RadialLevelIndicator : LevelIndicator
    {
        protected override GraphicsPath GetRenderShape(float margin = 0)
        {
            return CustomControlShapes.GetRingShape(Width, Height, margin);
        }
        protected override void PreRendering(Graphics targetGraphics)
        {
            base.PreRendering(targetGraphics);
            var path = new GraphicsPath();
            float angle = ValueAsFloat * 360;
            path.AddPie(0, 0, Width, Height, -90, angle);
            path.AddPie(Width * .085f, Height * .085f, Width * .83f, Height * .83f, -90, angle);
            targetGraphics.FillPath(new SolidBrush(ForeColor), path);
        }
        public RadialLevelIndicator() : base() { }
    }

    class RadialProgressBar : RadialLevelIndicator
    {
        public RadialProgressBar() : base() { }
    }

    class RadialVynilSpinner : CustomControl
    {
        public int Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
            }
        }
        private int _value = 0;
        public int Revolution
        {
            get
            {
                return _revolution;
            }
            set
            {
                _revolution = value;
                OnPropertyChanged("Revolution");
            }
        }
        private int _revolution = 10;
        public float ValueAsFloat
        {
            get
            {
                return Value / (float)Revolution;
            }
        }

        protected override GraphicsPath GetRenderShape(float margin = 0)
        {
            return base.GetRenderShape(margin);
        }

        protected override void PreRendering(Graphics targetGraphics)
        {
            base.PreRendering(targetGraphics);
            float angle = (ValueAsFloat - 1 / 4f) * (float)Math.PI * 2,
                width = Width,
                height = Height,
                size = Math.Min(Width, Height),
                cx = width / 2,
                cy = height / 2,
                pw = size * .08f,
                pw2 = pw / 2f;

            targetGraphics.DrawLine(new Pen(new SolidBrush(ForeColor.Blend(Color.Black, .6)), size * .06f), new PointF(cx, cy), new PointF(cx + (float)Math.Cos(-angle) * cx, cy - (float)Math.Sin(-angle) * cy));

            var pen = new Pen(Color.FromArgb(51, 0, 0, 0), pw);
            pen.EndCap = LineCap.Round;
            targetGraphics.DrawLine(pen, new PointF(cx, cy), new PointF(cx + pw / 4f, cy + pw / 4f));
            pen.Color = Color.FromArgb(151, 0, 0, 0);
            targetGraphics.DrawLine(pen, new PointF(cx, cy), new PointF(cx - pw2, cy + pw2));

            var rect = new RectangleF(cx - pw2, cy - pw2, pw, pw);
            var lgrad = new LinearGradientBrush(rect, Color.DimGray, Color.Gainsboro, -45f);
            targetGraphics.FillEllipse(lgrad, rect);

            var rgrad = CustomControlShapes.GetRadialGradientBrush(rect, new Color[] { Color.FromArgb(71, 0, 0, 0) }, Color.White);
            targetGraphics.FillEllipse(rgrad, rect);
        }

        public RadialVynilSpinner() : base()
        {
            BackColor = Color.Transparent;
            ForeColor = Color.Black;
        }
    }

    class Turntable : LevelIndicator
    {
        /// <summary>
        /// RadialProgressBar that shows the overall playback progress.
        /// </summary>
        protected RadialProgressBar TimeRadialProgressBar = new RadialProgressBar();
        /// <summary>
        /// Spinner that gives users notion of time.
        /// </summary>
        protected RadialVynilSpinner VynilRadialVynilSpinner = new RadialVynilSpinner();
        /// <summary>
        /// Label that shows the elapsed amount of time.
        /// </summary>
        protected Label ElapsedLabel = new Label();
        /// <summary>
        /// Label that shows the remaining amount of time.
        /// </summary>
        protected Label RemainingLabel = new Label();
        /// <summary>
        /// BPMIndicator for BPM value.
        /// </summary>
        protected BPMIndicator BPMBPMIndicator = new BPMIndicator();
        /// <summary>
        /// PitchIndicator for pitch value.
        /// </summary>
        protected PitchIndicator PitchPitchIndicator = new PitchIndicator();
        /// <summary>
        /// The order of turntables being displayed. Changes affect spinner color.
        /// </summary>
        public int TurntableNumber
        {
            get
            {
                return _turntableNumber;
            }
            set
            {
                _turntableNumber = value;
                OnPropertyChanged("TurntableNumber");
            }
        }

        private int _turntableNumber = 0;
        public float BeatsPerMinute
        {
            get
            {
                return _beatsPerMinute;
            }
            set
            {
                _beatsPerMinute = value;
                OnPropertyChanged("BeatsPerMinute");
            }
        }

        private float _beatsPerMinute = 0;
        public float Pitch
        {
            get
            {
                return _pitch;
            }
            set
            {
                _pitch = value;
                OnPropertyChanged("Pitch");
            }
        }

        private float _pitch;

        protected override GraphicsPath GetRenderShape(float margin = 0)
        {
            return CustomControlShapes.GetRoundShape(Width, Height, margin);
        }

        protected override void PreRendering(Graphics targetGraphics)
        {
            base.PreRendering(targetGraphics);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);

            TimeRadialProgressBar.Size = Size;

            VynilRadialVynilSpinner.Size = Size;
        }

        private void updateTimeControls()
        {
            ElapsedLabel.Text = DataFormatter.DefaultTimeFormat(Value - Minimum);
            RemainingLabel.Text = DataFormatter.DefaultTimeFormat(Value - Maximum);
        }

        private void updateMaximum()
        {
            TimeRadialProgressBar.Maximum = Maximum;
            VynilRadialVynilSpinner.Revolution = 2000;
        }

        private void updateMinimum()
        {
            TimeRadialProgressBar.Minimum = Minimum;
        }

        private void updateValue()
        {
            TimeRadialProgressBar.Value = Value;
            VynilRadialVynilSpinner.Value = Value;
        }

        protected override void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            base.PropertyChangedHandler(sender, e);
            switch (e.PropertyName)
            {
                case "Value":
                    updateValue();
                    updateTimeControls();
                    break;
                case "Maximum":
                    updateMaximum();
                    updateTimeControls();
                    break;
                case "Minimum":
                    updateMinimum();
                    updateTimeControls();
                    break;
                case "BeatsPerMinute":
                    BPMBPMIndicator.Value = BeatsPerMinute;
                    break;
                case "Pitch":
                    PitchPitchIndicator.Value = Pitch;
                    break;
            }
        }

        public Turntable() : base()
        {
            BackColor = Color.White;

            Width = 150;
            Height = 150;

            Controls.Add(VynilRadialVynilSpinner);
            Controls.Add(TimeRadialProgressBar);
            Controls.Add(ElapsedLabel);
            Controls.Add(RemainingLabel);
            Controls.Add(BPMBPMIndicator);
            Controls.Add(PitchPitchIndicator);

            /*"TurntableBackground"
            "TurntableProgressBackground"
              "TurntableProgressForeground"
              "TurntableSpinnerPointer"
              "TurntableSpinnerPitch"*/


            //
            // VynilRadialVynilSpinner
            //
            VynilRadialVynilSpinner.BackColor = Color.Transparent;
            //
            // TimeRadialProgressBar
            //
            TimeRadialProgressBar.BackColor = ThemeManager.SelectedTheme["TurntableProgressBackground"];
            TimeRadialProgressBar.ForeColor = ThemeManager.SelectedTheme["TurntableProgressForeground"];
            RemainingLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;
            //
            // ElapsedLabel
            //
            ElapsedLabel.TextAlign = StringAlignment.Center;
            ElapsedLabel.Location = new Point(0, Height / 2 + 13);
            ElapsedLabel.Anchor = AnchorStyles.Bottom;
            ElapsedLabel.Size = new Size(Width, 18);
            ElapsedLabel.FontSize = 14;
            ElapsedLabel.Text = "Elapsed";
            //
            // RemainingLabel
            //
            RemainingLabel.TextAlign = StringAlignment.Center;
            RemainingLabel.Location = new Point(0, Height / 2 + 31);
            RemainingLabel.Anchor = AnchorStyles.Bottom;
            RemainingLabel.Size = new Size(Width, 18);
            RemainingLabel.FontSize = 14;
            RemainingLabel.ForeColor = Color.DarkRed;
            RemainingLabel.Text = "-Remaining";
            //
            // BPMBPMIndicator
            //
            BPMBPMIndicator.Location = new Point(10, 52);
            RemainingLabel.Anchor = AnchorStyles.Left;
            //
            // PitchIndicator
            //
            PitchPitchIndicator.Location = new Point(59, 52);
            RemainingLabel.Anchor = AnchorStyles.Right;

            BeatsPerMinute = 0;
            Pitch = 0;
        }
    }

    class Label : CustomControl
    {
        [Category("Behavior")]
        public StringAlignment TextAlign
        {
            get
            {
                return _textAlign;
            }
            set
            {
                _textAlign = value;
                OnPropertyChanged("TextAlign");
            }
        }

        [Category("Behavior")]
        public StringAlignment VerticalAlign
        {
            get
            {
                return _verticalAlign;
            }
            set
            {
                _verticalAlign = value;
                OnPropertyChanged("VerticalAlign");
            }
        }

        [Category("Behavior")]
        public bool AutoSize
        {
            get
            {
                return _autoSize;
            }
            set
            {
                _autoSize = value;
                OnPropertyChanged("AutoSizeMode");
            }
        }

        [Category("Behavior")]
        public AutoSizeMode AutoSizeMode
        {
            get
            {
                return _autoSizeMode;
            }
            set
            {
                _autoSizeMode = value;
                OnPropertyChanged("AutoSizeMode");
            }
        }

        protected override void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            base.PropertyChangedHandler(sender, e);

            if (e.PropertyName == "Text")
                computeAutoSize();
        }

        private StringAlignment _textAlign;
        private StringAlignment _verticalAlign;
        private bool _autoSize;
        private AutoSizeMode _autoSizeMode;

        private void computeAutoSize()
        {
            if (AutoSize == false)
                return;

            SizeF size = Graphics.FromHwnd(this.Handle).MeasureString(Text, Font, MaximumSize.Width);

            bool sizeIsGreaterOrEqual = size.Width >= Size.Width && size.Height >= Size.Height;

            if (sizeIsGreaterOrEqual || AutoSizeMode == AutoSizeMode.GrowAndShrink)
            {
                Width = (int)size.Width;
                Height = (int)size.Height;
            }
        }

        protected override void PreRendering(Graphics targetGraphics)
        {
            base.PreRendering(targetGraphics);
            var format = new StringFormat();
            format.Alignment = TextAlign;
            format.LineAlignment = VerticalAlign;
            targetGraphics.DrawString(Text, Font, new SolidBrush(ForeColor), ClientRectangle, format);
        }

        public Label() : base()
        {
            TextAlign = StringAlignment.Near;
            VerticalAlign = StringAlignment.Center;
            ForeColor = Color.Black;
            BackColor = Color.Transparent;
            Font = new Font(Fonts.Fonts.Collection.ByName("Arial Rounded MT Bold"), 10);
            Text = this.Name;

            //let auto-size grow the label
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowOnly;
            Width = 1;
            Height = 1;
        }
    }

    internal class DecimalIndicator : CustomControl
    {
        /// <summary>
        /// Sets the BPM value
        /// </summary>
        public float Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
            }
        }
        private float _value;
    }

    class BPMIndicator : DecimalIndicator
    {
        /// <summary>
        /// Label that indicates music BPM.
        /// </summary>
        public Label BPMLabel = new Label();
        /// <summary>
        /// Label that serves as caption for BPM.
        /// </summary>
        public Label BPMLabelLabel = new Label();

        protected override void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            base.PropertyChangedHandler(sender, e);
            switch (e.PropertyName)
            {
                case "Value":
                    BPMLabel.Text = Value.ToString();
                    break;
            }
        }

        public BPMIndicator() : base()
        {
            Width = 67;
            Height = 15;

            Controls.Add(BPMLabel);
            Controls.Add(BPMLabelLabel);
            //
            // BPMLabel
            //
            BPMLabel.Text = "N/A";
            BPMLabel.TextAlign = StringAlignment.Far;
            BPMLabel.FontSize = 12;
            BPMLabel.Left = 0;
            BPMLabel.Height = 15;
            //
            // BPMLabelLabel
            //
            BPMLabelLabel.Text = "bpm";
            BPMLabelLabel.TextAlign = StringAlignment.Center;
            BPMLabelLabel.FontSize = 6;
            BPMLabel.Right = 0;
            BPMLabel.Height = 15;
        }
    }

    class PitchIndicator : DecimalIndicator
    {
        /// <summary>
        /// Label that indicates the pitch
        /// </summary>
        protected Label PitchLabel = new Label();
        /// <summary>
        /// Label that serves as caption for pitch.
        /// </summary>
        protected Label PitchLabelLabel = new Label();

        private void updateColor()
        {
            if (Value != 0)
                PitchLabelLabel.ForeColor = PitchLabel.ForeColor = Color.DarkRed;
            else
                PitchLabelLabel.ForeColor = PitchLabel.ForeColor = Color.Black;
        }

        protected override void PropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            base.PropertyChangedHandler(sender, e);
            switch (e.PropertyName)
            {
                case "Value":
                    PitchLabel.Text = DataFormatter.DefaultPitchFormat(Value);
                    updateColor();
                    break;
            }
        }

        public PitchIndicator() : base()
        {
            Controls.Add(PitchLabel);
            Controls.Add(PitchLabelLabel);
            //
            // PitchLabel
            //
            PitchLabel.Location = new Point(0, 0);
            PitchLabel.Size = new Size(62, 15);
            PitchLabel.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            PitchLabel.FontSize = 12;
            PitchLabel.Text = "N/A";
            PitchLabel.TextAlign = StringAlignment.Far;
            //
            // PitchLabelLabel
            //
            PitchLabelLabel.Location = new Point(62, 0);
            PitchLabelLabel.Size = new Size(11, 15);
            PitchLabelLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            PitchLabelLabel.Text = "%";
            PitchLabelLabel.TextAlign = StringAlignment.Center;

            Width = 73;
            Height = 15;
        }
    }
    class Logo : CustomControl
    {
        protected override void PreRendering(Graphics targetGraphics)
        {
            base.PreRendering(targetGraphics);
            var font = new Font(Fonts.Fonts.Collection.ByName("PRIMETIME"), Height * .5f);
            targetGraphics.DrawString("UltimateDJ", font, Brushes.White, Width * .01f, Height * .1f);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Width = (int)(Height / 32f * 150f);
        }

        public Logo() : base()
        {
            Height = 25;
        }
    }

    class CustomTitleBar : CustomControl
    {

        public CustomTitleBar()
        {
            Dock = DockStyle.Top;
            Visible = false;
            TabStop = false;
        }
    }
}