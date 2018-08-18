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

namespace AutoChart
{
    /// <summary>
    /// MyChart.xaml 的交互逻辑
    /// </summary>
    public partial class MyChart : UserControl
    {
        #region 属性
        /// <summary>
        /// OwnType
        /// </summary>
        private static Type _thisType = typeof(MyChart);
        /// <summary>
        /// 数据集
        /// </summary>
        private Dictionary<string, DrawData> _data = new Dictionary<string, DrawData>();
        /// <summary>
        /// 坐标原点
        /// </summary>
        private Point _origPoint = new Point();
        /// <summary>
        /// X轴上的文字
        /// </summary>
        private List<TextBlock> _xTip = new List<TextBlock>();
        /// <summary>
        /// Y轴上的文字
        /// </summary>
        private List<TextBlock> _yTip = new List<TextBlock>();
        /// <summary>
        /// X轴的区间
        /// </summary>
        private VectorChart2 _xLimt = new VectorChart2(double.MaxValue, double.MinValue);
        /// <summary>
        /// Y轴的区间
        /// </summary>
        private VectorChart2 _yLimt = new VectorChart2(double.MaxValue, double.MinValue);
        /// <summary>
        /// 每个像素映射为多少距离
        /// </summary>
        private VectorChart2 _everyDisForPiexl = new VectorChart2();
        /// <summary>
        /// 缩放中心点的偏移
        /// </summary>
        private VectorChart2 _centerOffect = new VectorChart2(0, 0);
        /// <summary>
        /// 当前线的缩放比例
        /// </summary>
        private Vector4 _currentLinesScale = new Vector4(1, 1, 1, 1);
        /// <summary>
        /// 每个刻度之间的间隔
        /// </summary>
        private static double _everyDis = 30;
        #endregion
        public MyChart()
        {
            InitializeComponent();
            this.Loaded += MyChart_Loaded;
            this.SizeChanged += MyChart_SizeChanged;
        }
        #region 依赖项
        /// <summary>
        /// 是否允许X轴方向的缩放
        /// </summary>
        public static readonly DependencyProperty IsXZoomProperty = DependencyProperty.Register("IsXZoom", typeof(bool), _thisType);
        /// <summary>
        /// 是否允许Y轴方向的缩放
        /// </summary>
        public static readonly DependencyProperty IsYZoomProperty = DependencyProperty.Register("IsYZoom", typeof(bool), _thisType);
        /// <summary>
        /// 图表的标题
        /// </summary>
        public static readonly DependencyProperty ChartTitleProperty = DependencyProperty.Register("ChartTitle", typeof(string), _thisType);
        /// <summary>
        /// X轴的注释
        /// </summary>
        public static readonly DependencyProperty XAxisTitleProperty = DependencyProperty.Register("XAxisTitle", typeof(string), _thisType);
        /// <summary>
        /// Y轴的注释
        /// </summary>
        public static readonly DependencyProperty YAxisTitleProperty = DependencyProperty.Register("YAxisTitle", typeof(string), _thisType);
        /// <summary>
        /// X轴和Y轴的极值
        /// </summary>
        public static readonly DependencyProperty ScaleLimtProperty = DependencyProperty.Register("ScaleLimt", typeof(Vector4), _thisType, new PropertyMetadata(new Vector4(double.MinValue, double.MaxValue, double.MinValue, double.MaxValue)));
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
                VectorChart2 vec2 = new VectorChart2();
                vec2.Vec1 = w;
                h = double.IsNaN(DrawLineAndPoint.Height) ? DrawLineAndPoint.ActualHeight : DrawLineAndPoint.Height;
                vec2.Vec2 = h;
                return vec2;
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
                VectorChart2 vec2 = new VectorChart2();
                vec2.Vec1 = w;
                h = double.IsNaN(GridLines.Height) ? GridLines.ActualHeight : GridLines.Height;
                vec2.Vec2 = h;
                return vec2;
            }
        }
        /// <summary>
        /// 是否拥有线数据
        /// </summary>
        public bool IsHaveLine
        {
            get
            {
                return _data.Count > 0;
            }
        }
        /// <summary>
        /// X轴的极限
        /// </summary>
        protected VectorChart2 XLimt
        {
            get
            {
                if (IsXZoom)
                    return new VectorChart2(_xLimt.Vec1, _xLimt.Vec2 / _currentLinesScale.Vec4);
                else
                    return _xLimt;
            }

            set
            {
                _xLimt = value;
            }
        }
        /// <summary>
        /// Y轴的极限
        /// </summary>
        protected VectorChart2 YLimt
        {
            get
            {
                if (IsYZoom)
                    return new VectorChart2(_yLimt.Vec1, _yLimt.Vec2 / _currentLinesScale.Vec4);
                else
                    return _yLimt;
            }
        }
        #endregion
        #region 自定义函数
        /// <summary>
        /// 添加一条线
        /// </summary>
        /// <param name="ps">线上点的集合</param>
        /// <param name="name">线的名称</param>
        /// <param name="brush">线的画笔</param>
        /// <param name="Thicness">线的粗细</param>
        public void AddLine(ObservableCollection<Point> ps, string name, Brush brush, int Thicness, int PointThicness = 8)
        {
            ps.CollectionChanged += Data_CollectionChanged;
            ///存放极值
            Vector4 vec4 = new Vector4(0, 0, 0, 0);
            ///检测极值
            vec4.Vec2 = ps.Select(m => m.X).Max();
            vec4.Vec4 = ps.Select(m => m.Y).Max();
            vec4.Vec1 = ps.Select(m => m.X).Min();
            vec4.Vec3 = ps.Select(m => m.Y).Min();
            
            ///检测是否存在
            if (_data.Keys.Contains(name))
            {
                return;
            }
            ///创建画图数据
            DrawData data = new DrawData();
            ///保存数据源
            data.Ps = ps;
            ///添加极值
            data.Vec4 = vec4;
            ///添加提示
            data.LineTitle.ColorBrush = brush;
            data.LineTitle.LineName = name;
            ///添加到显示
            LineTitles.Children.Add(data.LineTitle);
            ///创建线
            Polyline ply = new Polyline();
            ply.Stroke = brush;
            ///加入到显示
            DrawLineAndPoint.Children.Add(ply);
            data.Line = ply;
            int cur = 0;
            ///创建点
            foreach (var item in ps)
            {
                Ellipse elp = new Ellipse();
                elp.Fill = brush;
                ///乘以当前的比例
                elp.Width = PointThicness;
                elp.Height = PointThicness;
                data.Points.Add(elp);
                DrawLineAndPoint.Children.Add(elp);
                elp.ToolTip = new TextBlock() { Text = ps[cur++].ToString(), };
                elp.MouseEnter += Elp_MouseEnter;
                elp.MouseLeave += Elp_MouseLeave;
            }
            _data.Add(name, data);
            AdjustLimt();
            AdjustLines();
            AdjustLinesAndPoints();
        }
        /// <summary>
        /// 删除这个集合的数据和线
        /// </summary>
        /// <param name="ps"></param>
        public void DelLine(string name)
        {
            if (_data.Keys.Contains(name))
            {
                DrawData data = _data[name];
                _data.Remove(name);
                DrawLineAndPoint.Children.Remove(data.Line);
                foreach (var item in data.Points)
                {
                    DrawLineAndPoint.Children.Remove(item);
                }
                LineTitles.Children.Remove(data.LineTitle);
                data.Dispose();
                AdjustLimt();
                Refesh();
            }
        }
        /// <summary>
        /// 删除目标位置的线数据
        /// </summary>
        /// <param name="index"></param>
        public void DelLine(int index)
        {
            string name = null;
            int pos = 0;
            foreach (var item in _data)
            {
                if(pos++ == index)
                {
                    name = item.Key;
                    break;
                }
            }
            if(name != null)
            {
                DelLine(name);
            }
        }
        /// <summary>
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
        /// 数据集改变的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Data_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ObservableCollection<Point> ps = sender as ObservableCollection<Point>;

            AdjustLines();
        }
        /// <summary>
        /// 转换源数据到绘图区的点
        /// </summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        public PointCollection ChangeToDrawPoints(ObservableCollection<Point> ps)
        {
            PointCollection newp = new PointCollection();
            foreach (var item in ps)
            {
                newp.Add(new Point(item.X / _everyDisForPiexl.Vec1, DrawArea.Vec2 - (item.Y / _everyDisForPiexl.Vec2)));
            }
            return newp;
        }
        /// <summary>
        /// 根据偏差移动绘图区
        /// </summary>
        /// <param name="offect"></param>
        /// <returns></returns>
        public bool MoveDrawLinesAndPoints(Point offect)
        {
            if (Math.Pow(offect.X, 2) + Math.Pow(offect.Y, 2) < 3 * 3) return false;
            _centerOffect.Vec1 += offect.X;
            _centerOffect.Vec2 += offect.Y;
            VectorChart2 offectValue = GetOffectValue(_centerOffect);
            if(XLimt.Vec1 + offectValue.Vec1 <= ScaleLimt.Vec1 || XLimt.Vec2 + offectValue.Vec1 >= ScaleLimt.Vec2 || YLimt.Vec1 + offectValue.Vec2 <= ScaleLimt.Vec3 || YLimt.Vec2 + offectValue.Vec2 >= ScaleLimt.Vec4)
            {
                _centerOffect.Vec1 -= offect.X;
                _centerOffect.Vec2 -= offect.Y;
                return false;
            }

            Point curp = new Point(Canvas.GetLeft(DrawLineAndPoint), Draw.Height - Canvas.GetBottom(DrawLineAndPoint));
            curp.X += offect.X;
            curp.Y += offect.Y;
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
            return new VectorChart2( -offectValue.Vec1 * _everyDisForPiexl.Vec1, offectValue.Vec2 * _everyDisForPiexl.Vec2);
        }
        /// <summary>
        /// 根据具体的值获取应该的偏移量
        /// </summary>
        /// <param name="offectValue"></param>
        /// <returns></returns>
        protected VectorChart2 GetOffectPoint(VectorChart2 offectValue)
        {
            return new VectorChart2(offectValue.Vec1 / _everyDisForPiexl.Vec1, offectValue.Vec2 / _everyDisForPiexl.Vec2);
        }
        #endregion
        #region 界面元素调整函数
        /// <summary>
        /// 调整曲线 刻度(直接修改)
        /// </summary>
        protected void AdjustLines()
        {
            if (!IsHaveLine) return;
            double xeveryone = (XLimt.Vec2 - XLimt.Vec1) / (_xTip.Count - 1);
            double yeveryone = (YLimt.Vec2 - YLimt.Vec1) / (_yTip.Count - 1);
            _everyDisForPiexl.Vec1 = (XLimt.Vec2 - XLimt.Vec1) / Draw.Width;
            _everyDisForPiexl.Vec2 = (YLimt.Vec2 - YLimt.Vec1) / Draw.Height;
            double cur = 0;
            ///计算偏移量
            VectorChart2 offect = GetOffectValue(_centerOffect);
            foreach (var item in _xTip)
            {
                //item.Text = (cur * xeveryone + _xLimt.Vec1).ToString("0.00");
                item.Text = (((double)item.GetValue(Canvas.LeftProperty) - _origPoint.X) * _everyDisForPiexl.Vec1 + offect.Vec1).ToString("0.0");
                cur++;
            }
            cur = 0;
            foreach (var item in _yTip)
            {
                //item.Text = (cur * yeveryone + _yLimt.Vec1).ToString("0.00");
                item.Text = (((double)item.GetValue(Canvas.BottomProperty) - _origPoint.Y) * _everyDisForPiexl.Vec2 + offect.Vec2).ToString("0.0");
                cur++;
            }
        }
        /// <summary>
        /// 调整所有绘图区的尺寸
        /// </summary>
        protected void AdjustSize()
        {
            PicAre.Width = this.ActualWidth - PicAre.Margin.Left - PicAre.Margin.Right;
            PicAre.Height = this.ActualHeight - PicAre.Margin.Top - PicAre.Margin.Bottom;

            X_Axis.Width = PicAre.Width;
            Y_Axis.Height = PicAre.Height;

            Draw.Width = PicAre.Width - Draw.Margin.Left - Draw.Margin.Right;
            Draw.Height = PicAre.Height - Draw.Margin.Top - Draw.Margin.Bottom;
            ///只初始化一次
            if (double.IsNaN(DrawLineAndPoint.Width))
            {
                DrawLineAndPoint.Width = Draw.Width;
                DrawLineAndPoint.Height = Draw.Height;

                Canvas.SetLeft(DrawLineAndPoint, 0);
                Canvas.SetBottom(DrawLineAndPoint, 0);
            }
            ///刷新原点坐标
            _origPoint.X = 33;
            _origPoint.Y = 33;
        }
        /// <summary>
        /// 调整极值
        /// </summary>
        protected void AdjustLimt()
        {
            if (_data.Count > 0)
            {
                _xLimt.Vec1 = _data.Select(m => m.Value.Vec4.Vec1).Min();
                _xLimt.Vec2 = _data.Select(m => m.Value.Vec4.Vec2).Max();
                _xLimt.Vec2 += _xLimt.Vec2 * 0.15;
                _yLimt.Vec1 = _data.Select(m => m.Value.Vec4.Vec3).Min();
                _yLimt.Vec2 = _data.Select(m => m.Value.Vec4.Vec4).Max();
                _yLimt.Vec2 += _yLimt.Vec2 * 0.15;
            }
        }

        /// <summary>
        /// 调整刻度\网格线(删除重新添加)
        /// </summary>
        protected void AdjustScale()
        {
            //if (!IsHaveLine) return;
            if (X_Axis.Width == 0 || X_Axis.Height == 0 || X_Axis.Width == double.NaN || X_Axis.Height == double.NaN) return;
            //if (GridLines.Width == 0 || GridLines.Height == 0 || GridLines.Width == double.NaN || double.IsNaN(GridLines.Height)) return;
            ///清除X轴的刻度
            if (X_Axis.Children.Count > 1)
            {
                X_Axis.Children.RemoveRange(1, X_Axis.Children.Count - 1);
            }
            ///清除Y轴的刻度
            if (Y_Axis.Children.Count > 1)
            {
                Y_Axis.Children.RemoveRange(1, Y_Axis.Children.Count - 1);
            }
            ///清除网格线
            GridLines.Children.Clear();
            ///清除刻度线的提示文字
            _xTip.Clear();
            _yTip.Clear();

            ///添加X轴的
            for (double i = _origPoint.X; i < X_Axis.Width; i += _everyDis)
            {
                Line x_scale = new Line();
                x_scale.StrokeThickness = 2;
                x_scale.Stroke = new SolidColorBrush(Colors.Black);
                x_scale.StrokeStartLineCap = PenLineCap.Round;
                x_scale.StrokeEndLineCap = PenLineCap.Round;
                x_scale.Width = 4;
                x_scale.Height = 20;
                x_scale.X1 = 2;
                x_scale.Y1 = 4;
                x_scale.X2 = 2;
                x_scale.Y2 = x_scale.Height;
                Canvas.SetLeft(x_scale, i);
                Canvas.SetTop(x_scale, 0);
                X_Axis.Children.Add(x_scale);
                ///网格线
                GridLines.Children.Add(GetGridLine(new Point(i - 2, 0), false, GridLinesArea.Vec2));
                //double.IsNaN(GridLines.Height) ? GridLines.ActualHeight : GridLines.Height
                ///添加提示文字
                TextBlock block = new TextBlock();
                block.FontSize = 10;
                block.Text = i.ToString();
                Canvas.SetLeft(block, i);
                Canvas.SetTop(block, 25);
                _xTip.Add(block);
                X_Axis.Children.Add(block);
            }
            ///添加Y轴的
            for (double i = _origPoint.Y; i < Y_Axis.Height; i += _everyDis)
            {
                Line y_scale = new Line();
                y_scale.StrokeThickness = 2;
                y_scale.Stroke = new SolidColorBrush(Colors.Black);
                y_scale.StrokeStartLineCap = PenLineCap.Round;
                y_scale.StrokeEndLineCap = PenLineCap.Round;
                y_scale.Width = 20;
                y_scale.Height = 4;
                y_scale.X1 = 4;
                y_scale.Y1 = 2;
                y_scale.X2 = y_scale.Width;
                y_scale.Y2 = 2;
                Canvas.SetBottom(y_scale, i + 1);
                Canvas.SetRight(y_scale, 2);
                Y_Axis.Children.Add(y_scale);
                ///网格线
                GridLines.Children.Add(GetGridLine(new Point(i - 2, 0), true, GridLinesArea.Vec1));
                //double.IsNaN(GridLines.Width) ? GridLines.ActualWidth : GridLines.Width)
                ///添加提示文字
                TextBlock block = new TextBlock();
                block.FontSize = 10;
                block.Text = i.ToString();
                Canvas.SetBottom(block, i);
                Canvas.SetRight(block, 10);
                _yTip.Add(block);
                Y_Axis.Children.Add(block);
            }
            
        }
        /// <summary>
        /// 调整线和点
        /// </summary>
        protected void AdjustLinesAndPoints()
        {
            if (!IsHaveLine) return;
            foreach (var item in _data)
            {
                item.Value.Line.Points = ChangeToDrawPoints(item.Value.Ps);
                for (int i = 0; i < item.Value.Line.Points.Count; i++)
                {
                    Canvas.SetLeft(item.Value.Points[i], item.Value.Line.Points[i].X - item.Value.Points[i].Width / 2);
                    Canvas.SetTop(item.Value.Points[i], item.Value.Line.Points[i].Y - item.Value.Points[i].Height / 2);
                }
            }
        }
        /// <summary>
        /// 调整线和点的大小比例
        /// </summary>
        /// <param name="roat"></param>
        protected void AdjustLinesAndPointsSize(double roat, Size scaleSize)
        {
            if (!IsHaveLine) return;
            _currentLinesScale.Vec2 = scaleSize.Width;
            _currentLinesScale.Vec3 = scaleSize.Height;
            _currentLinesScale.Vec1 = roat;
            ScaleTransform scale = null;
            foreach (var item in _data)
            {
                item.Value.Line.StrokeThickness *= roat;

                foreach (var itemp in item.Value.Points)
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
        protected Line GetGridLine(Point start,bool isHor, double len)
        {
            Line gridLine = new Line();
            gridLine.Stroke = new SolidColorBrush(Color.FromArgb(120, 100, 100, 100));
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
        #region 控件事件
        /// <summary>
        /// 尺寸改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyChart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Size newsize = e.NewSize;
            if(!e.WidthChanged)
            {
                this.Width = newsize.Width;
            }
            if(!e.HeightChanged)
            {
                this.Height = newsize.Height;
            }
            AdjustSize();
            AdjustScale();
            AdjustLines();
            AdjustLinesAndPoints();
        }

        /// <summary>
        /// 渲染事件
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }
        /// <summary>
        /// 加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyChart_Loaded(object sender, RoutedEventArgs e)
        {         
            AdjustScale();
        }
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
            if(e.Delta >= 120)
            {
                delta = 1.2;
            }
            else if(e.Delta <= -120)
            {
                delta = (double)5 / (double)6;
            }
            if (IsXZoom)
            {
                DrawLineAndPoint.Width *= delta;
                curPDraw.Vec1 *= delta;
                curPLinesAndPoint.Vec1 *= delta;
            }
            if (IsYZoom)
            {
                DrawLineAndPoint.Height *= delta;
                curPDraw.Vec2 *= delta;
                curPLinesAndPoint.Vec2 *= delta;
            }

            _currentLinesScale.Vec4 *= delta;
            //Canvas.SetLeft(DrawLineAndPoint, curPDraw.Vec1 - curPLinesAndPoint.Vec1);
            //Canvas.SetBottom(DrawLineAndPoint, -(DrawArea.Vec2 - (curPLinesAndPoint.Vec2 - curPDraw.Vec2)));

            ///调整刻度
            AdjustLines();

            AdjustLinesAndPoints();
        }
        /// <summary>
        /// 重置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Rest.IsEnabled = false;
            ///移除偏移
            _centerOffect.Vec1 = 0;
            _centerOffect.Vec2 = 0;
            ///移除缩放
            _currentLinesScale = new Vector4(1, 1, 1, 1);
            ///移除移动
            Canvas.SetLeft(DrawLineAndPoint, 0);
            Canvas.SetBottom(DrawLineAndPoint, 0);
            //AdjustLinesAndPointsSize(1, new Size(1, 1));
            Refesh();
            Rest.IsEnabled = true;
        }
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
            if(_isMoveDraw)
            {
                Point newp = e.GetPosition(this);
                Point offectp = new Point(newp.X - _forntPoint.X, newp.Y - _forntPoint.Y);
                if(MoveDrawLinesAndPoints(offectp))
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
    }
    /// <summary>
    /// 向量
    /// </summary>
    public struct VectorChart2
    {
        public double Vec1;

        public double Vec2;

        public VectorChart2(double v1, double v2)
        {
            this.Vec1 = v1;
            this.Vec2 = v2;
        }
        /// <summary>
        /// Point 转换VectorChart2
        /// </summary>
        /// <param name="p"></param>
        public static implicit operator VectorChart2(Point p)
        {
            return new VectorChart2(p.X, p.Y);
        }
        /// <summary>
        /// 乘法
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public static VectorChart2 operator * (VectorChart2 v1, double ratio)
        {
            return new VectorChart2(v1.Vec1 * ratio, v1.Vec2 * ratio);
        }
    }
    /// <summary>
    /// 4维向量
    /// </summary>
    public struct Vector4
    {
        /// <summary>
        /// 空
        /// </summary>
        public static Vector4 Empty = new Vector4() { _isEmpty = true };

        public double Vec1;

        public double Vec2;

        public double Vec3;

        public double Vec4;
        /// <summary>
        /// 是否为空数据
        /// </summary>
        private bool _isEmpty;

        public Vector4(double Vec1, double Vec2, double Vec3, double Vec4)
        {
            this.Vec1 = Vec1;
            this.Vec2 = Vec2;
            this.Vec3 = Vec3;
            this.Vec4 = Vec4;
            _isEmpty = false;
        }

        /// <summary>
        /// 重载等号运算符
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static bool operator ==(Vector4 v1, Vector4 v2)
        {
            if (v1.Vec1 == v2.Vec1 && v1.Vec2 == v2.Vec2 && v1.Vec3 == v2.Vec3 && v1.Vec4 == v2.Vec4 && v1._isEmpty == v2._isEmpty)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 重载
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static bool operator !=(Vector4 v1, Vector4 v2)
        {
            return !(v1 == v2);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    /// <summary>
    /// 绘图数据
    /// </summary>
    public class DrawData : IDisposable
    {
        /// <summary>
        /// 线
        /// </summary>
        public Polyline Line;
        /// <summary>
        /// 线的标记点
        /// </summary>
        public List<Ellipse> Points = new List<Ellipse>();
        /// <summary>
        /// 源数据集合
        /// </summary>
        public ObservableCollection<Point> Ps = null;
        /// <summary>
        /// 极值
        /// </summary>
        public Vector4 Vec4 = Vector4.Empty;
        /// <summary>
        /// 提示
        /// </summary>
        public LineTitle LineTitle = new LineTitle();


        public DrawData()
        {

        }

        ~DrawData()
        {
            this.Dispose();
        }
        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                    Line = null;
                    Ps = null;
                    Points = null;
                    LineTitle = null;
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~DrawData() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
