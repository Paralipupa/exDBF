using exDBF.ViewModel;
using System;
using System.Windows;

namespace exDBF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            DataContext = new MainViewModel();

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            exDBF.Properties.Settings.Default.Save();
        }
    }
}
