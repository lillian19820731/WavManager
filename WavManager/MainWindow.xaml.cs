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
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            WaveData waveData = new WaveData(".\\Test\\pinyin-lillian.wav");
            WaveFormDisplayer.SourceData = waveData.Data;

            //WaveFormDisplayer.SourceData = data.Data;

            //string txtPath = basePath + ".\\Test\\speech.txt";
            ////DataGenerator dg = new DataGenerator();
            //dg.WriteData(wavPath, dg.ParseFile(txtPath, 2));
            //MessageBox.Show("Wave File created!");
            //WaveFormDisplayerControl c = new WaveFormDisplayerControl();
            //this.WaveFormDisplayer.Children.Add(c);
            //MessageBox.Show("end");


        }
    }
}
