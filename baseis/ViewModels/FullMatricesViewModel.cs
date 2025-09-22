using CommunityToolkit.Mvvm.ComponentModel;

namespace baseis.ViewModels
{
    public class FullMatricesViewModel : ObservableObject
    {
        public string SourceMatrix { get; set; } = "";
        public string SecondImageMatrix { get; set; } = "";
        public string TrainingMatrix { get; set; } = "";
        public string ReferenceVectors { get; set; } = "";
    }
}