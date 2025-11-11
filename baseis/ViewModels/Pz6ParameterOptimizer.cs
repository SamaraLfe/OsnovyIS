using System;
using System.Collections.Generic;

namespace baseis.ViewModels
{
    /// <summary>
    /// Перебор по delta и уровню селекции для поиска лучших значений критерия Шеннона.
    /// </summary>
    public sealed class Pz6ParameterOptimizer
    {
        private readonly MainWindowViewModel _viewModel;

        public Pz6ParameterOptimizer(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public Pz6OptimizationResult? Optimize(Pz6OptimizationSettings settings)
        {
            if (_viewModel.GetTrainingImages().Count < 2)
            {
                return null;
            }

            if (!_viewModel.TryRunFullCalculation())
            {
                return null;
            }

            var originalSnapshot = CaptureSnapshot();
            double bestScore = EvaluateScore(originalSnapshot);
            int bestDelta = originalSnapshot.Delta;
            double bestSelec = originalSnapshot.Selec;

            foreach (var delta in settings.DeltaCandidates)
            {
                foreach (var selec in settings.SelecCandidates)
                {
                    if (delta == bestDelta && Math.Abs(selec - bestSelec) < 1e-9)
                    {
                        continue;
                    }

                    _viewModel.Delta = delta;
                    _viewModel.Selec = selec;

                    if (!_viewModel.TryRunFullCalculation())
                    {
                        continue;
                    }

                    var snapshot = CaptureSnapshot();
                    double score = EvaluateScore(snapshot);
                    if (score > bestScore + settings.ScoreTolerance)
                    {
                        bestScore = score;
                        bestDelta = delta;
                        bestSelec = selec;
                    }
                }
            }

            _viewModel.Delta = bestDelta;
            _viewModel.Selec = bestSelec;

            if (!_viewModel.TryRunFullCalculation())
            {
                // Возвращаем исходные значения в случае неудачи.
                _viewModel.Delta = originalSnapshot.Delta;
                _viewModel.Selec = originalSnapshot.Selec;
                _viewModel.TryRunFullCalculation();
                return null;
            }

            var optimizedSnapshot = CaptureSnapshot();
            return new Pz6OptimizationResult(originalSnapshot, optimizedSnapshot);
        }

        private Pz6OptimizationSnapshot CaptureSnapshot()
        {
            var metricsSnapshot = _viewModel.GetPz5MetricsSnapshot();
            return new Pz6OptimizationSnapshot(_viewModel.Delta, _viewModel.Selec, metricsSnapshot);
        }

        private static double EvaluateScore(Pz6OptimizationSnapshot snapshot)
        {
            double score = 0.0;
            bool hasValue = false;

            foreach (var value in snapshot.BestShannonByClass.Values)
            {
                if (double.IsFinite(value))
                {
                    score += value;
                    hasValue = true;
                }
            }

            return hasValue ? score : double.NegativeInfinity;
        }
    }
}
