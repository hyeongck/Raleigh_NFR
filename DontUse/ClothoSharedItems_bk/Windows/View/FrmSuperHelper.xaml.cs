using System.Windows;

namespace ClothoSharedItems.View
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new ViewModel.MainViewModel();
        }
    }
}