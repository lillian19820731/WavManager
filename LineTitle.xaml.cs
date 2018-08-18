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

namespace AutoChart
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class LineTitle : UserControl
    {
        public LineTitle()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 所代表的的颜色
        /// </summary>
        public Brush ColorBrush
        {
            get
            {
                return Line.Stroke;
            }
            set
            {
                Line.Stroke = value;
                Title.Foreground = value;
            }
        }
        /// <summary>
        /// 线的注释
        /// </summary>
        public string LineName
        {
            get
            {
                return Title.Text;
            }
            set
            {
                Title.Text = value;
            }
        }
    }
}
