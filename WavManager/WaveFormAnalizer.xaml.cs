using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// <summary>
    /// WaveFormAnalizer.xaml 的交互逻辑
    /// </summary>
    public partial class WaveFormAnalizer : UserControl
    {
        private static Type thisType = typeof(WaveFormChart);
        public List<short> SourceData = new List<short>();
        public VectorChart2 SelectedData = new VectorChart2();
        private List<Point> filteredData = new List<Point>();

        public float[] frame = null;

        private Point origPoint = new Point();
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
        Brush brush = new SolidColorBrush(Color.FromRgb(0, 0, 0));


        private DrawData drawData = new DrawData();

        public WaveFormAnalizer()
        {
            InitializeComponent();
            this.Loaded += MyChart_Loaded;
            this.SizeChanged += MyChart_SizeChanged;
        }
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
            //AdjustSize();
            if (this.SourceData.Count > 0)
            {
                this.AppendData(SelectedData);
            }
        }

        public void ChangeToDrawPoints(Point p)
        {
            p.X = p.X / everyDisForPiexl.Vect1;
            p.Y = this.origPoint.Y - (p.Y / everyDisForPiexl.Vect2);
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

        protected void AdjustSize()
        {
            Main.Width = this.Width;
            Main.Height = this.Height;
            PicArea.Width = this.Width - Main.Margin.Left - Main.Margin.Right;
            PicArea.Height = PicArea.MinHeight;

            Draw.Width = PicArea.Width - Y_Axis.Width;
            Draw.Height = PicArea.Height;


            Y_Axis.Height = Draw.Height;

            origPoint.X = 0;
            origPoint.Y = Draw.Height;
        }

        private int yGridLineCount = 0;
        /// <summary>
        /// 调整刻度\网格线(删除重新添加)
        /// </summary>
        protected void AdjustScale()
        {
            bool b = false;
            //Y 轴无需更改
            if (Y_Axis.Children.Count == 0)
            {
                ///添加Y轴的
                for (double i = 0; i <= Y_Axis.Height; i += Y_Axis.Height / 20/*YSkipOffset*/)
                {
                    Line y_scale = new Line();
                    y_scale.StrokeThickness = 2;
                    y_scale.Stroke = new SolidColorBrush(Colors.Black);
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
                    //Draw.Children.Add(GetGridLine(new Point(i, origPoint.X), true, GridLinesArea.Vect1));
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

            this.AppendData(this.SelectedData);
        }

        public void AppendData()
        {
            this.AppendData(SelectedData);
        }
        protected void AppendData(VectorChart2 selectedData)
        {
            if (SourceData.Count > 0)
            {
                this.AppendData(GenerateDrawData(selectedData));
            }

        }

        private void AppendData(List<Point> ps)
        {
            this.everyDisForPiexl.Vect2 = (FrequencyMax - FrequencyMin) / Draw.Height;
            this.everyDisForPiexl.Vect1 = ps.Count;

            int PointThicness = 8;
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
                //DrawLineAndPoint.Children.Add(elp);
                elp.ToolTip = new TextBlock() { Text = ps[cur++].ToString(), };
            }
            AdjustScale();
            //AdjustLines();
            //AdjustLinesAndPoints();
        }

        private float FrequencyMin = 0;
        private float FrequencyMax = 20000;
        private List<Point> GenerateDrawData(VectorChart2 selectedData)
        {
            if (filteredData.Count > 0)
                filteredData.Clear();

            int begin = (int)this.SelectedData.Vect1;
            int end = (int)this.SelectedData.Vect2;
            int length = (end - begin)/256;
            float min = 0, max = 0;
            float frequency;
            //MFCC mfcc = new MFCC();
            float[][] results = new float[length][];
            MFCC mfcc = new MFCC(44100, 512, 512, 0, 4000, 24, 12);
            float[] frames = new float[512];
            for (int i = begin; i <= end - 512; i += 256)
            {
                for (int j = 0; j < 512; j++)
                {
                    frames[j] = (float)this.SourceData[i + j];
                }

                float[] r = mfcc.ProcessFrame(frames);
                //min = r.Min();
                //max = r.Max();
                //rMin = rMin < min ? rMin : min;
                //rMax = rMax > max ? rMax : max;
                results[i/256] = r;
                for(int j = 0; j < 256;j++)
                {
                    frequency = results[i][j];
                    if(frequency<FrequencyMax)
                    {
                        filteredData.Add(new Point(i, j));
                    }

                }
            }
            return filteredData;
        }

        private Ellipse GeneratePoint(float x,float y,Brush b,float PointThicness)
        {
            Ellipse elp = new Ellipse();
            elp.Fill = b;
            elp.Width = PointThicness;
            elp.Height = PointThicness;
            return elp;
        }


            private Color GetColorBy(double percent)
        {
            Color sourceColor = Colors.Blue;
            Color destColor = Colors.Red;
            int redSpace = destColor.R - sourceColor.R;
            int greenSpace = destColor.G - sourceColor.G;
            int blueSpace = destColor.B - sourceColor.B;
           return Color.FromRgb(
            (byte)(sourceColor.R + percent * redSpace),
            (byte)(sourceColor.G + percent * greenSpace),
            (byte)(sourceColor.B + percent * blueSpace)
            );
        }

        public void OnPassValues(object sender, PassValuesEventArgs e)
        {
            SelectedData = e.vect;
            if (this.SelectedData.Vect1 != SelectedData.Vect2)
                this.AppendData();
            //NOTICE: values are passed here
        }




    }
}
