using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace baseis.Views
{
    public partial class FullMatricesWindow : Window
    {
        public FullMatricesWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}