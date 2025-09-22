using CommunityToolkit.Mvvm.ComponentModel;

namespace baseis.ViewModels
{
    public partial class DetailsViewModel : ObservableObject
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";

        [ObservableProperty]
        private double fontSize = 12;
    }
}
