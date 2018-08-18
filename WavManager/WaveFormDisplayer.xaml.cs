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
    /// WaveFormDisplayer.xaml 的交互逻辑
    /// </summary>
    public partial class WaveFormDisplayer : UserControl
    {
        private static Type thisType = typeof(WaveFormDisplayer);
        public List<short> SourceData = new List<short>();
        //private VectorChart2 selectedData = SelectedDataChangedFlag;
 //       public event DataHandler LoadData;
        public string DataPath = "";
        public WaveFormDisplayer()
        {
            InitializeComponent();
            this.Loaded += WaveFormDisplayer_Loaded;
            this.WaveFormChartBase.PassValuesEvent += this.WaveFormChart.OnPassValues;
            this.WaveFormChartBase.PassValuesEvent += this.WaveFormAnalizer.OnPassValues;
        }
        private void WaveFormDisplayer_Loaded(object sender, RoutedEventArgs e)
        {
            WaveFormAnalizer.SourceData = WaveFormChartBase.SourceData = WaveFormChart.SourceData = SourceData;
            WaveFormAnalizer.SelectedData = WaveFormChart.SelectedData = WaveFormChartBase.SelectedData;

            //NOTICE: add event handler here

        }
        protected void Flag_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //WaveFormChartBase.PassValuesEvent += WaveFormChartBase.PassValuesHandler(ReceiveValues);
            //WaveFormChart.SelectedData = WaveFormChartBase.SelectedData;
            //if(Flag.DataContext!="00")
            //{
            //    WaveFormChart.AppendData();
            //}


        }
    protected override void OnRender(DrawingContext drawingContext)
        {
            WaveFormChart.SelectedData = WaveFormChartBase.SelectedData;

            base.OnRender(drawingContext);
        }
    }
}
