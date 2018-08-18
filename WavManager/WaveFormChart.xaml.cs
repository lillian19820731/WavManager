using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SASR
{
    public partial class WaveFormChart : UserControl
    {
        #region 属性
        /// <summary>
        /// OwnType
        /// </summary>
        private static Type thisType = typeof(WaveFormChart);
        /// <summary>
        //源数据集
        /// </summary>
        public List<short> SourceData = new List<short>();
        public VectorChart2 SelectedData = new VectorChart2();
        private DrawData drawData = new DrawData();
        private List<Point> filteredData = new List<Point>();
        /// <summary>
        /// 坐标原点
        /// </summary>
        private Point origPoint = new Point();
        /// <summary>
        /// X轴上的文字
        /// </summary>
        private List<TextBlock> xTip = new List<TextBlock>();
        /// <summary>
        /// Y轴上的文字
        /// </summary>
        private List<TextBlock> yTip = new List<TextBlock>();
        /// <summary>
        /// X轴的区间
        /// </summary>
        private VectorChart2 xExtremeValue = new VectorChart2(0, 0);
        /// <summary>
        /// Y轴的区间
        /// </summary>
        private VectorChart2 yExtremeValue = new VectorChart2(0, 0);
        /// <summary>
        /// 每个像素映射为多少距离
        /// </summary>
        private VectorChart2 everyDisForPiexl = new VectorChart2();
        /// <summary>
        /// 缩放中心点的偏移
        /// </summary>
        private VectorChart2 centerOffect = new VectorChart2(0, 0);
        /// <summary>
        /// 当前线的缩放比例
        /// </summary>
        private Vector4 currentLinesScale = new Vector4(1, 1, 1, 1);
        Brush brush = new SolidColorBrush(Colors.LightBlue);

        #endregion
        #region 构造函数
        public WaveFormChart()
        {
            InitializeComponent();
            ///创建线
            Polyline ply = new Polyline();
            ply.Stroke = brush;
            ///加入到显示
            DrawLineAndPoint.Children.Add(ply);
            drawData.Line = ply;
            this.Loaded += MyChart_Loaded;
            this.SizeChanged += MyChart_SizeChanged;
        }
        #endregion
        #region 控件事件
        /// <summary>
        /// 尺寸改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyChart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size newsize = e.NewSize;
            //if (this.Height == newsize.Height && this.Width == newsize.Width)
            //    return;

            this.Width = newsize.Width;
            this.Height = newsize.Height;
            AdjustSize();
            if (this.SourceData.Count > 0)
            {
                this.AppendData(SelectedData);
            }
        }

        /// <summary>
        /// 加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyChart_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.SelectedData.Vect2 == 0)
            {
                SelectedData.Vect2 = (SourceData.Count > 180000) ? 180000 : SourceData.Count;
            }
            this.yExtremeValue.Vect1 = SourceData.Min();
            this.yExtremeValue.Vect2 = SourceData.Max();
            //double xx = this.xExtremeValue.Vect2 - this.xExtremeValue.Vect1;
            //double yy = this.yExtremeValue.Vect2 - this.yExtremeValue.Vect1 + 10000;

            //this.everyDisForPiexl = new VectorChart2(xx / DrawLineAndPoint.Width, yy / DrawLineAndPoint.Height);
            this.AppendData(this.SelectedData);

            //this.AddLine(SourceData, everyDisForPiexl.Vect1, "Wave Form");
        }

        public void AppendData()
        {
            this.AppendData(SelectedData);
        }
        protected void AppendData(VectorChart2 selectedData)
        {
            if (SourceData.Count > 0)
            {
                this.AppendData(this.GenerateDrawData(selectedData), brush, 1, 1);

            }
        }
        private void AppendData(List<Point> ps, Brush brush, int Thicness, int PointThicness = 8)
        {
            //if (drawData.Ps != null && drawData.Ps.Count > 0)
            //    drawData.Ps.Clear();
            drawData.Ps = ps;
            ///添加极值
            drawData.xExtremeValue = this.xExtremeValue;
            drawData.yExtremeValue = this.yExtremeValue;
            if (drawData.Points != null && drawData.Points.Count > 0)
                drawData.Points.Clear();

            int cur = 0;
            ///创建点

            foreach (Point item in ps)
            {
                Ellipse elp = new Ellipse();
                elp.Fill = brush;
                elp.Width = PointThicness;
                elp.Height = PointThicness;
                drawData.Points.Add(elp);
                DrawLineAndPoint.Children.Add(elp);
                elp.ToolTip = new TextBlock() { Text = ps[cur++].ToString(), };
            }
            AdjustScale();
            AdjustLines();
            AdjustLinesAndPoints();
        }

        private int CalcXOffset()
        {
            this.xExtremeValue.Vect1 = SelectedData.Vect1;
            this.xExtremeValue.Vect2 = SelectedData.Vect2;
            this.everyDisForPiexl.Vect1 = (this.xExtremeValue.Vect2 - this.xExtremeValue.Vect1) / DrawLineAndPoint.Width;
            this.everyDisForPiexl.Vect2 = (this.yExtremeValue.Vect2 - this.yExtremeValue.Vect1) / DrawLineAndPoint.Height;
            int offset = (int)(this.everyDisForPiexl.Vect1 / 2);
            offset = offset > 1 ? offset : 1;
            return offset;
        }
        private List<Point> GenerateDrawData(VectorChart2 selectedData)
        {
            this.xExtremeValue.Vect1 = SelectedData.Vect1;
            this.xExtremeValue.Vect2 = SelectedData.Vect2;
            this.everyDisForPiexl.Vect1 = (this.xExtremeValue.Vect2 - this.xExtremeValue.Vect1) / DrawLineAndPoint.Width;
            this.everyDisForPiexl.Vect2 = (this.yExtremeValue.Vect2 - this.yExtremeValue.Vect1) / DrawLineAndPoint.Height;
            if (filteredData.Count > 0)
                filteredData.Clear();
            int offset = (int)(this.everyDisForPiexl.Vect1 / 2);
            offset = offset > 1 ? offset : 1;
            for (int i = (int)SelectedData.Vect1; i < SelectedData.Vect2; i += offset)
            {
                filteredData.Add(new Point(i, SourceData[i]));
            }
            //int start = (int)(SelectedData.Vect1);
            //int end =(int)(SelectedData.Vect2);
            //int datacount = end-start + 1;
            //double sum = 0;
            //int count = 0;
            //double everage = 0;
            //for (int i = start; i <= end; i++)
            //{
            //    count++;
            //    sum += SourceData[i];
            //    if (i > 0 && i % offset == 0)
            //    {
            //        everage = sum / count;
            //        count = 0;
            //        sum = 0;
            //        filteredData.Add(new Point(i, everage));
            //        everage = 0;
            //    }
            //}
            return filteredData;
        }

        #endregion
        #region 界面元素调整函数
        /// <summary>
        /// 调整曲线 刻度(直接修改)
        /// </summary>
        protected void AdjustLines()
        {
            if (!(xTip.Count >= 1 && yTip.Count >= 1))
                return;
            double xeveryone = (xExtremeValue.Vect2 - xExtremeValue.Vect1) / (xTip.Count - 1);
            double yeveryone = (yExtremeValue.Vect2 - yExtremeValue.Vect1) / (yTip.Count - 1);
            if (Draw.Width > 0)
            {
                double xSize = xExtremeValue.Vect2 - xExtremeValue.Vect1;
                //everyDisForPiexl.Vect1 = xSize / Draw.Width;
                if (everyDisForPiexl.Vect1 < 1)
                {
                    everyDisForPiexl.Vect1 = 1;
                }
                //XSkipOffset = Draw.Width/20;
            }
            if (Draw.Height > 0)
            {
                double ySize = yExtremeValue.Vect2 - yExtremeValue.Vect1;

                // everyDisForPiexl.Vect2 = ySize / Draw.Height;
                if (everyDisForPiexl.Vect2 < 1)
                {
                    everyDisForPiexl.Vect2 = 1;
                }
                //YSkipOffset = Draw.Height/20;
            }
            double cur = 0;
            ///计算偏移量
            VectorChart2 offect = GetOffectValue(centerOffect);
            foreach (var item in xTip)
            {
                //item.Text = (cur * everyDisForPiexl.Vect1 + offect.Vect1).ToString("0.00");
                //item.Text = (((int)item.GetValue(Canvas.LeftProperty) - origPoint.X) * everyDisForPiexl.Vect1 + offect.Vect1).ToString("0.0");
                cur++;
            }
            cur = 0;
            foreach (var item in yTip)
            {
                //item.Text = (cur * everyDisForPiexl.Vect2 + offect.Vect2).ToString("0.00");
                //item.Text = (((int)item.GetValue(Canvas.BottomProperty) - origPoint.Y) * everyDisForPiexl.Vect2 + offect.Vect2).ToString("0.0");
                cur++;
            }
        }
        /// <summary>
        /// 调整所有绘图区的尺寸
        /// </summary>
        protected void AdjustSize()
        {
            Main.Width = this.Width;
            Main.Height = this.Height;
            PicArea.Width = this.Width - Main.Margin.Left - Main.Margin.Right;
            PicArea.Height = PicArea.MinHeight;

            GridLines.Width = DrawLineAndPoint.Width = Draw.Width = PicArea.Width - Y_Axis.Width;
            GridLines.Height = DrawLineAndPoint.Height = Draw.Height = PicArea.Height - X_Axis.Height;


            X_Axis.Width = DrawLineAndPoint.Width;
            Y_Axis.Height = DrawLineAndPoint.Height;

            origPoint.X = 0;
            origPoint.Y = DrawLineAndPoint.Height / 2;
        }
        private int yGridLineCount = 0;
        /// <summary>
        /// 调整刻度\网格线(删除重新添加)
        /// </summary>
        protected void AdjustScale()
        {
            //if (X_Axis.Width == 0 || X_Axis.Height == 0 || X_Axis.Width == double.NaN || X_Axis.Height == double.NaN)
            //    return;

            bool b = false;
            //Y 轴无需更改
            if (Y_Axis.Children.Count == 0)
            {
                ///添加Y轴的
                for (double i = 0; i <= Y_Axis.Height; i += Y_Axis.Height / 10/*YSkipOffset*/)
                {
                    Line y_scale = new Line();
                    y_scale.StrokeThickness = 2;
                    y_scale.Stroke = new SolidColorBrush(Colors.Gray);
                    y_scale.StrokeStartLineCap = PenLineCap.Round;
                    y_scale.StrokeEndLineCap = PenLineCap.Round;
                    y_scale.Width = (b) ? 3 : 2;
                    y_scale.Height = 2;
                    y_scale.X1 = (b) ? -3 : -2;
                    y_scale.Y1 = 0;
                    y_scale.X2 = 0;
                    y_scale.Y2 = 0;
                    Canvas.SetTop(y_scale, i);
                    Canvas.SetRight(y_scale, origPoint.X - 2);
                    Y_Axis.Children.Add(y_scale);
                    ///网格线
                    GridLines.Children.Add(GetGridLine(new Point(i, origPoint.X), true, GridLinesArea.Vect1));
                    //double.IsNaN(GridLines.Width) ? GridLines.ActualWidth : GridLines.Width)
                    ///添加提示文字
                    TextBlock block = new TextBlock();
                    block.FontSize = 10;
                    if (b)
                    {
                        block.Text = ((int)((origPoint.Y - i) * this.everyDisForPiexl.Vect2)).ToString();
                    }
                    else
                    {
                        block.Text = "";
                    }

                    Canvas.SetTop(block, i - 5);
                    Canvas.SetRight(block, origPoint.X + 5);
                    yTip.Add(block);
                    Y_Axis.Children.Add(block);
                    b = !b;
                    yGridLineCount++;
                }

            }

            ///清除X轴的刻度
            if (X_Axis.Children.Count > 0)
            {
                X_Axis.Children.Clear();
            }
            ///清除网格线
            GridLines.Children.RemoveRange(yGridLineCount,GridLines.Children.Count-1);
            ///清除刻度线的提示文字
            xTip.Clear();

            ///添加X轴的
            b = false;
            for (double i = 0; i <= X_Axis.Width; i += X_Axis.Width / 20)
            {
                Line x_scale = new Line();
                x_scale.Stroke = new SolidColorBrush(Colors.Gray);
                x_scale.StrokeThickness = 3;
                x_scale.StrokeStartLineCap = PenLineCap.Round;
                x_scale.StrokeEndLineCap = PenLineCap.Round;
                x_scale.Width = 3;
                x_scale.Height = (b) ? 6 : 4;
                x_scale.X1 = 0;
                x_scale.Y1 = 0;
                x_scale.X2 = 0;
                x_scale.Y2 = (b) ? 6 : 4;
                Canvas.SetLeft(x_scale, i);
                Canvas.SetTop(x_scale, -2);
                X_Axis.Children.Add(x_scale);
                ///网格线
                GridLines.Children.Add(GetGridLine(new Point(i, 0), false, GridLinesArea.Vect2));
                //double.IsNaN(GridLines.Height) ? GridLines.ActualHeight : GridLines.Height
                ///添加提示文字
                TextBlock block = new TextBlock();
                block.FontSize = 10;
                block.Height = 10;
                if (b)
                {
                    block.Text = ((int)(i * this.everyDisForPiexl.Vect1)).ToString();
                }
                else
                {
                    block.Text = "";
                }
                Canvas.SetTop(block, 5);
                Canvas.SetLeft(block, i - 10);
                xTip.Add(block);
                X_Axis.Children.Add(block);
                b = !b;
            }
        }

        /// <summary>
        /// 转换源数据到绘图区的点
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        public PointCollection ChangeToDrawPoints(DrawData data)
        {
            PointCollection newp = new PointCollection();
            foreach (Point item in data.Ps)
            {
                newp.Add(new Point(item.X / everyDisForPiexl.Vect1, this.origPoint.Y - (item.Y / everyDisForPiexl.Vect2)));
            }
            return newp;
        }

        public void ChangeToDrawPoints(Point p)
        {
            p.X = p.X / everyDisForPiexl.Vect1;
            p.Y = this.origPoint.Y - (p.Y / everyDisForPiexl.Vect2);
        }
        /// <summary>
        /// 调整线和点
        /// </summary>
        protected void AdjustLinesAndPoints()
        {
            drawData.Line.Points = ChangeToDrawPoints(drawData);
            for (int i = 0; i < drawData.Ps.Count; i++)
            {
                Canvas.SetLeft(drawData.Points[i], drawData.Line.Points[i].X - drawData.Points[i].Width / 2);
                Canvas.SetTop(drawData.Points[i], drawData.Line.Points[i].Y - drawData.Points[i].Height / 2);
                //Canvas.SetLeft(drawData.Points[i], drawData.Line.Points[i].X - drawData.Points[i].Width / 2);
                //Canvas.SetTop(drawData.Points[i], drawData.Line.Points[i].Y - drawData.Points[i].Height / 2);
            }
        }
        /// <summary>
        /// 调整线和点的大小比例
        /// </summary>
        /// <param name="roat"></param>
        protected void AdjustLinesAndPointsSize(double roat, Size scaleSize)
        {
            currentLinesScale.Vect2 = scaleSize.Width;
            currentLinesScale.Vect3 = scaleSize.Height;
            currentLinesScale.Vect1 = roat;
            ScaleTransform scale = null;
            drawData.Line.StrokeThickness *= roat;

            foreach (var itemp in drawData.Points)
            {
                if (itemp.RenderTransform as ScaleTransform == null)
                {
                    itemp.RenderTransform = new ScaleTransform(1, 1);
                }
                scale = itemp.RenderTransform as ScaleTransform;
                scale.ScaleX = scaleSize.Width;
                scale.ScaleY = scaleSize.Height;
            }
        }
        /// <summary>
        /// 刷新控件
        /// </summary>
        public void Refesh()
        {
            AdjustScale();
            AdjustLines();
            AdjustLinesAndPoints();
        }
        /// <summary>
        /// 获取一条直线
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        protected Line GetGridLine(Point start, bool isHor, double len)
        {
            Line gridLine = new Line();
            gridLine.Stroke = new SolidColorBrush(Colors.Gray);
            gridLine.StrokeThickness = 1;
            if (!isHor)
            {
                gridLine.X1 = 0.5;
                gridLine.Y1 = 0;
                gridLine.X2 = 0.5;
                gridLine.Y2 = len;
                Canvas.SetLeft(gridLine, start.X);
                Canvas.SetTop(gridLine, start.Y);
            }
            else
            {
                gridLine.X1 = 0;
                gridLine.Y1 = 0.5;
                gridLine.X2 = len;
                gridLine.Y2 = 0.5;
                Canvas.SetBottom(gridLine, start.X);
                Canvas.SetLeft(gridLine, start.Y);
            }

            return gridLine;
        }
        #endregion

        #region 移动
        /// <summary>
        /// 是否移动
        /// </summary>
        private bool _isMoveDraw = false;
        /// <summary>
        /// 前一个点
        /// </summary>
        private Point _forntPoint;

        /// <summary>
        /// 鼠标滚轮事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            VectorChart2 curPDraw = (VectorChart2)e.GetPosition(Draw);
            VectorChart2 curPLinesAndPoint = (VectorChart2)e.GetPosition(DrawLineAndPoint);

            double delta = 1;
            if (e.Delta >= 120)
            {
                delta = 1.2;
            }
            else if (e.Delta <= -120)
            {
                delta = (double)5 / (double)6;
            }
            if (IsXZoom)
            {
                DrawLineAndPoint.Width *= delta;
                curPDraw.Vect1 *= delta;
                curPLinesAndPoint.Vect1 *= delta;
            }
            if (IsYZoom)
            {
                DrawLineAndPoint.Height *= delta;
                curPDraw.Vect2 *= delta;
                curPLinesAndPoint.Vect2 *= delta;
            }

            currentLinesScale.Vect4 *= delta;
            //Canvas.SetLeft(DrawLineAndPoint, curPDraw.Vect1 - curPLinesAndPoint.Vect1);
            //Canvas.SetBottom(DrawLineAndPoint, -(DrawArea.Vect2 - (curPLinesAndPoint.Vect2 - curPDraw.Vect2)));

            ///调整刻度
            AdjustLines();
            AdjustLinesAndPoints();
        }

        /// <summary>
        /// 绘图区鼠标点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawLineAndPoint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Draw.Cursor = Cursors.SizeAll;
            _isMoveDraw = true;
            _forntPoint = e.GetPosition(this);
        }
        /// <summary>
        /// 绘图区鼠标移动事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawLineAndPoint_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMoveDraw)
            {
                Point newp = e.GetPosition(this);
                Point offset = new Point(newp.X - _forntPoint.X, newp.Y - _forntPoint.Y);
                if (MoveDrawLinesAndPoints(offset))
                {
                    _forntPoint = newp;
                }
            }
        }
        /// <summary>
        /// 绘图区鼠标抬起事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawLineAndPoint_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Draw.Cursor = Cursors.Arrow;
            _isMoveDraw = false;
        }
        /// <summary>
        /// 绘图区鼠标进入事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawLineAndPoint_MouseEnter(object sender, MouseEventArgs e)
        {

        }
        /// <summary>
        /// 绘图区鼠标离开事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawLineAndPoint_MouseLeave(object sender, MouseEventArgs e)
        {
            _isMoveDraw = false;
        }
        #endregion
        #region 依赖项
        /// <summary>
        /// 是否允许X轴方向的缩放
        /// </summary>
        public static readonly DependencyProperty IsXZoomProperty = DependencyProperty.Register("IsXZoom", typeof(bool), thisType);
        /// <summary>
        /// 是否允许Y轴方向的缩放
        /// </summary>
        public static readonly DependencyProperty IsYZoomProperty = DependencyProperty.Register("IsYZoom", typeof(bool), thisType);
        /// <summary>
        /// 图表的标题
        /// </summary>
        public static readonly DependencyProperty ChartTitleProperty = DependencyProperty.Register("ChartTitle", typeof(string), thisType);
        /// <summary>
        /// X轴的注释
        /// </summary>
        public static readonly DependencyProperty XAxisTitleProperty = DependencyProperty.Register("XAxisTitle", typeof(string), thisType);
        /// <summary>
        /// Y轴的注释
        /// </summary>
        public static readonly DependencyProperty YAxisTitleProperty = DependencyProperty.Register("YAxisTitle", typeof(string), thisType);
        /// <summary>
        /// X轴和Y轴的极值
        /// </summary>
        public static readonly DependencyProperty ScaleLimtProperty = DependencyProperty.Register("ScaleLimt", typeof(Vector4), thisType, new PropertyMetadata(new Vector4(double.MinValue, double.MaxValue, double.MinValue, double.MaxValue)));
        #endregion
        #region 访问器
        /// <summary>
        /// 是否允许X轴方向的缩放
        /// </summary>
        public bool IsXZoom
        {
            get
            {
                return (bool)GetValue(IsXZoomProperty);
            }
            set
            {
                SetValue(IsXZoomProperty, value);
            }
        }
        /// <summary>
        /// 是否允许Y轴方向的缩放
        /// </summary>
        public bool IsYZoom
        {
            get
            {
                return (bool)GetValue(IsYZoomProperty);
            }
            set
            {
                SetValue(IsYZoomProperty, value);
            }
        }
        /// <summary>
        /// 图标的标题
        /// </summary>
        public string ChartTitle
        {
            get
            {
                return (string)GetValue(ChartTitleProperty);
            }
            set
            {
                SetValue(ChartTitleProperty, value);
            }
        }
        /// <summary>
        /// X轴和Y轴的极值
        /// </summary>
        public Vector4 ScaleLimt
        {
            get
            {
                return (Vector4)GetValue(ScaleLimtProperty);
            }
            set
            {
                SetValue(ScaleLimtProperty, value);
            }
        }
        /// <summary>
        /// X轴的注释
        /// </summary>
        public string XAxisTitle
        {
            get
            {
                return (string)GetValue(XAxisTitleProperty);
            }
            set
            {
                SetValue(XAxisTitleProperty, value);
            }
        }
        /// <summary>
        /// Y轴的注释
        /// </summary>
        public string YAxisTitle
        {
            get
            {
                return (string)GetValue(YAxisTitleProperty);
            }
            set
            {
                SetValue(YAxisTitleProperty, value);
            }
        }
        /// <summary>
        /// 绘制线和点的区域大小
        /// </summary>
        public VectorChart2 DrawArea
        {
            get
            {
                double w, h;
                w = double.IsNaN(DrawLineAndPoint.Width) ? DrawLineAndPoint.ActualWidth : DrawLineAndPoint.Width;
                VectorChart2 Vect2 = new VectorChart2();
                Vect2.Vect1 = w;
                h = double.IsNaN(DrawLineAndPoint.Height) ? DrawLineAndPoint.ActualHeight : DrawLineAndPoint.Height;
                Vect2.Vect2 = h;
                return Vect2;
            }
        }
        /// <summary>
        /// 网格线区域
        /// </summary>
        public VectorChart2 GridLinesArea
        {
            get
            {
                double w, h;
                w = double.IsNaN(GridLines.Width) ? GridLines.ActualWidth : GridLines.Width;
                VectorChart2 Vect2 = new VectorChart2();
                Vect2.Vect1 = w;
                h = double.IsNaN(GridLines.Height) ? GridLines.ActualHeight : GridLines.Height;
                Vect2.Vect2 = h;
                return Vect2;
            }
        }
        #endregion
        #region 测试
        /// <summary>
        /// 创建测试数据
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<Point> CreateTestDatas(int max)
        {
            ObservableCollection<Point> ps = new ObservableCollection<Point>();
            Random rand = new Random();
            for (int i = 0; i < 250; i++)
            {
                ps.Add(new Point(i, rand.NextDouble() * max));
            }
            return ps;
        }
        #endregion
        #region 自定义函数
        /// 鼠标离开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Elp_MouseLeave(object sender, MouseEventArgs e)
        {
            Ellipse elp = sender as Ellipse;
            elp.Cursor = Cursors.Arrow;
        }
        /// <summary>
        /// 鼠标进入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Elp_MouseEnter(object sender, MouseEventArgs e)
        {
            Ellipse elp = sender as Ellipse;
            elp.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// 根据偏差移动绘图区
        /// </summary>
        /// <param name="offect"></param>
        /// <returns></returns>
        public bool MoveDrawLinesAndPoints(Point offset)
        {
            if (Math.Pow(offset.X, 2) + Math.Pow(offset.Y, 2) < 3 * 3) return false;
            centerOffect.Vect1 += offset.X;
            centerOffect.Vect2 += offset.Y;
            VectorChart2 offectValue = GetOffectValue(centerOffect);
            if (xExtremeValue.Vect1 + offectValue.Vect1 <= ScaleLimt.Vect1 || xExtremeValue.Vect2 + offectValue.Vect1 >= ScaleLimt.Vect2 || yExtremeValue.Vect1 + offectValue.Vect2 <= ScaleLimt.Vect3 || yExtremeValue.Vect2 + offectValue.Vect2 >= ScaleLimt.Vect4)
            {
                centerOffect.Vect1 -= offset.X;
                centerOffect.Vect2 -= offset.Y;
                return false;
            }

            Point curp = new Point(Canvas.GetLeft(DrawLineAndPoint), Draw.Height - Canvas.GetBottom(DrawLineAndPoint));
            curp.X += offset.X;
            curp.Y += offset.Y;
            Canvas.SetLeft(DrawLineAndPoint, curp.X);
            Canvas.SetBottom(DrawLineAndPoint, Draw.Height - curp.Y);
            AdjustLines();
            return true;
        }
        /// <summary>
        /// 获取偏移量
        /// </summary>
        /// <returns></returns>
        protected VectorChart2 GetOffectValue(VectorChart2 offectValue)
        {
            return new VectorChart2(-offectValue.Vect1 * everyDisForPiexl.Vect1, offectValue.Vect2 * everyDisForPiexl.Vect2);
        }
        /// <summary>
        /// 根据具体的值获取应该的偏移量
        /// </summary>
        /// <param name="offectValue"></param>
        /// <returns></returns>
        protected VectorChart2 GetOffectPoint(VectorChart2 offectValue)
        {
            return new VectorChart2(offectValue.Vect1 / everyDisForPiexl.Vect1, offectValue.Vect2 / everyDisForPiexl.Vect2);
        }
        #endregion


        public void OnPassValues(object sender, PassValuesEventArgs e)
        {
            SelectedData =e.vect; 
            if (this.SelectedData.Vect1 != SelectedData.Vect2)
                this.AppendData();
            //NOTICE: values are passed here
        }

    }
}
 