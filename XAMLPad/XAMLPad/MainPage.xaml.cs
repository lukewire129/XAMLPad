using LazyVoom.OpenSilver;
using System.Windows;
using System.Windows.Controls;
using XAMLPad.Core;

namespace XAMLPad
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.SetAutoWireViewModel(this, true);
            //var initializer = new XamlEditorInitializer ();
            //initializer.SetupAutoCompletion ();
        }
    }
}
