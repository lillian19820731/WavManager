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
using SASR;
namespace WavManager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string basePath = string.Empty;//AppDomain.CurrentDomain.BaseDirectory;
            string wavPath = basePath + ".\\Test\\speech.wav";
            string txtPath = basePath + ".\\Test\\speech.txt";
            DataGenerator dg = new DataGenerator();
            dg.WriteData(wavPath, dg.ParseFile(txtPath, 2));
            MessageBox.Show("Wave File created!");
        }
    }
}
