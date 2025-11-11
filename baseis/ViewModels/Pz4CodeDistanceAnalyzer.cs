using System;

namespace baseis.ViewModels
{
    /// <summary>
    /// Практическое занятие 4. Формирование массива кодовых расстояний.
    /// </summary>
    public class Pz4CodeDistanceAnalyzer
    {
        private readonly MainWindowViewModel _viewModel;

        public Pz4CodeDistanceAnalyzer(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        /// <summary>
        /// Строит массив кодовых расстояний SK.
        /// </summary>
        public void Compute()
        {
            var X = _viewModel.GetXMatrix();
            var xm = _viewModel.GetXmMatrix();

            int classCount = X.GetLength(0);
            int featureCount = X.GetLength(1);
            int realizationCount = X.GetLength(2);

            var skMatrix = new int[classCount, classCount, realizationCount];

            for (int classIndex = 0; classIndex < classCount; classIndex++)
            {
                for (int classNeighbors = 0; classNeighbors < classCount; classNeighbors++)
                {
                    for (int realization = 0; realization < realizationCount; realization++)
                    {
                        int different = 0;
                        for (int feature = 0; feature < featureCount; feature++)
                        {
                            if (xm[classIndex, feature] != X[classNeighbors, feature, realization])
                            {
                                different++;
                            }
                        }

                        skMatrix[classIndex, classNeighbors, realization] = different;
                    }
                }
            }

            _viewModel.SetSkMatrix(skMatrix);
        }
    }
}
