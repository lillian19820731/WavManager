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

namespace SASR
{
   public partial class WaveFormChartBase : UserControl
    {
        #region 属性
        /// <summary>
        /// OwnType
        /// </summary>
        private static Type thisType = typeof(WaveFormChartBase);
        /// <summary>
        //源数据集
        /// </summary>
        public List<short> SourceData   = new List<short>();
        public VectorChart2 SelectedData = new VectorChart2();
        /// <summary>
        /// 数据集
        /// </summary>
        private DrawData drawData = new DrawData();
        /// <summary>
        /// 坐标原点
        /// </summary>
        private Point origPoint = new Point();
        /// <summary>
        /// 每个像素映射为多少距离
        /// </summary>
        private VectorChart2 everyDisForPiexl = new VectorChart2();
        /// <summary>
        /// X轴的区间
        /// </summary>
        private VectorChart2 xExtremeValue = new VectorChart2(0, 0);
        /// <summary>
        /// Y轴的区间
        /// </summary>
        private VectorChart2 yExtremeValue = new VectorChart2(0,0);

        Brush brush = new SolidColorBrush(Colors.LightBlue);
        #endregion
        public WaveFormChartBase()
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


        #region 自定义函数
        /// <summary>
        /// 添加一条线
        /// </summary>
        /// <param name="ps">线上点的集合</param>
        /// <param name="name">线的名称</param>
        /// <param name="brush">线的画笔</param>
        /// <param name="Thicness">线的粗细</param>
        public void AppendData(List<short> source)
        {
            this.AppendData(this.GenerateDrawData(source), brush,1,1);
        }
        public void AppendData(List<Point> ps,Brush brush, int Thicness, int PointThicness = 8)
        {
            if(drawData.Ps!=null && drawData.Ps.Count>0)
                drawData.Ps.Clear();
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
            AdjustLinesAndPoints();
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

        /// <summary>
        /// 调整所有绘图区的尺寸
        /// </summary>
        protected void AdjustSize()
        {
            Main.Width = this.Width;
            DrawLineAndPoint.Width = WaveFormArea.Width = Main.Width - Main.Margin.Left - Main.Margin.Right;
            WaveFormArea.Height = DrawLineAndPoint.Height = WaveFormArea.MaxHeight;
            Main.Height = WaveFormArea.Height + Main.Margin.Top + Margin.Bottom;
            origPoint.X = 0;
            origPoint.Y = DrawLineAndPoint.Height / 2;
        }
        protected void AdjustLinesAndPoints()
        {
            drawData.Line.Points = ChangeToDrawPoints(this.drawData);
            for (int i = 0; i < drawData.Ps.Count; i++)
            {
                //Canvas.SetLeft(item.Value.Points[i], item.Value.Line.Points[i].X - item.Value.Points[i].Width / 2);
                //Canvas.SetTop(item.Value.Points[i], item.Value.Line.Points[i].Y - item.Value.Points[i].Height / 2);
                Canvas.SetLeft(drawData.Points[i], drawData.Line.Points[i].X - drawData.Points[i].Width / 2);
                Canvas.SetTop(drawData.Points[i], drawData.Line.Points[i].Y - drawData.Points[i].Height / 2);
            }
        }
        /// 刷新控件
        /// </summary>
        public void Refesh()
        {
            //AdjustLines();
            AdjustLinesAndPoints();
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

            this.Width = newsize.Width;
            this.Height = newsize.Height;
            AdjustSize();
            if (SourceData.Count>0)
            {
                AppendData(SourceData);

            }
        }

        ///// <summary>
        ///// 渲染事件
        ///// </summary>
        ///// <param name="drawingContext"></param>
        //protected override void OnRender(DrawingContext drawingContext)
        //{
        //    base.OnRender(drawingContext);
        //}
        /// <summary>
        /// 加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyChart_Loaded(object sender, RoutedEventArgs e)
        {
            var layer = AdornerLayer.GetAdornerLayer(DrawLineAndPoint);
            //foreach (UIElement ui in DrawLineAndPoint.Children)
            layer.Add(new ResizeAdorner(ResizableRect));
            this.xExtremeValue.Vect1 = 0;
            this.xExtremeValue.Vect2 = SourceData.Count;
            this.yExtremeValue.Vect1 = SourceData.Min();
            this.yExtremeValue.Vect2 = SourceData.Max();
            double xx = this.xExtremeValue.Vect2 - this.xExtremeValue.Vect1;
            double yy = this.yExtremeValue.Vect2 - this.yExtremeValue.Vect1 + 50000;

            this.everyDisForPiexl = new VectorChart2(xx / DrawLineAndPoint.Width, yy / DrawLineAndPoint.Height);

            Point p = ResizableRect.TranslatePoint(o, DrawLineAndPoint);
            SelectedData.Vect1 = p.X * this.everyDisForPiexl.Vect1;
            SelectedData.Vect2 = (p.X + this.ResizableRect.Width) * this.everyDisForPiexl.Vect1;
            this.AppendData(SourceData);
        }
        protected List<Point> GenerateDrawData(List<short> data)
        {
            List<Point> ps = new List<Point>();
            int offset = (int)this.everyDisForPiexl.Vect1/5;
            //skip offset
            for (int i = 0; i < data.Count; i += offset)
            {
                ps.Add(new Point(i, SourceData[i]));
            }
            //double sum = 0;
            //int count = 0;
            //double everage = 0;
            //for (int i = 0; i < data.Count; i++)
            //{
            //    count++;
            //    sum += data[i];
            //    if (i % offset == 0)
            //    {
            //        everage = sum / count;
            //        count = 0;
            //        sum = 0;
            //        ps.Add(new Point(i, everage));
            //        everage = 0;
            //    }
            //}
            return ps;
        }
        #endregion

        #region events

        private Point o = new Point(0, 0);

        private void ResizableRect_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Point p = ResizableRect.TranslatePoint(o, DrawLineAndPoint);
            double w = ResizableRect.Width;
            if (p.X < 0)
            {
                //this.SetValue(ResizableRect,
                Canvas.SetLeft(ResizableRect, 0);
                ResizableRect.Width += p.X;
                return;
            }
            else if (p.X + ResizableRect.Width > this.DrawLineAndPoint.Width)
            {
                Canvas.SetRight(ResizableRect, this.DrawLineAndPoint.Width);
                ResizableRect.Width -= ResizableRect.Width - this.DrawLineAndPoint.Width + p.X;
                return;
            }
            Size newsize = e.NewSize;
            SelectedData.Vect1 = p.X * this.everyDisForPiexl.Vect1;
            SelectedData.Vect2 = (p.X + newsize.Width) * this.everyDisForPiexl.Vect1;
            //NOTICE: invoke event here!
            PassValuesEventArgs args = new PassValuesEventArgs(SelectedData);
            this.PassValuesEvent(this, args);
        }

        public delegate void PassValuesHandler(object sender, PassValuesEventArgs e);

        public event PassValuesHandler PassValuesEvent;

        #endregion
    }

    public class PassValuesEventArgs : EventArgs
    {
        public VectorChart2 vect;


        public PassValuesEventArgs(VectorChart2 v)
        {
            vect = v;
        }
        public override string ToString()
        {
            return vect.Vect1.ToString()+vect.Vect2.ToString();
        }

    }

    /// <summary>
    /// 向量
    /// </summary>
    public struct VectorChart2
    {
        public double Vect1;

        public double Vect2;

        public VectorChart2(double vect1, double vect2)
        {
            this.Vect1 = vect1;
            this.Vect2 = vect2;
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
            return new VectorChart2(v1.Vect1 * ratio, v1.Vect2 * ratio);
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

        public double Vect1;

        public double Vect2;

        public double Vect3;

        public double Vect4;
        /// <summary>
        /// 是否为空数据
        /// </summary>
        private bool _isEmpty;

        public Vector4(double Vect1, double Vect2, double Vect3, double Vect4)
        {
            this.Vect1 = Vect1;
            this.Vect2 = Vect2;
            this.Vect3 = Vect3;
            this.Vect4 = Vect4;
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
            if (v1.Vect1 == v2.Vect1 && v1.Vect2 == v2.Vect2 && v1.Vect3 == v2.Vect3 && v1.Vect4 == v2.Vect4 && v1._isEmpty == v2._isEmpty)
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
        public List<Point> Ps = new List<Point>();
        /// <summary>
        /// X轴的区间
        /// </summary>
        public VectorChart2 xExtremeValue;
        /// <summary>
        /// Y轴的区间
        /// </summary>
        public VectorChart2 yExtremeValue;
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
