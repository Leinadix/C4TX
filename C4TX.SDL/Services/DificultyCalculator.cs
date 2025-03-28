
using C4TX.SDL.Models;

namespace C4TX.SDL.Services {
    public class DifficultyCalculator
    {
        // Helper methods
        private static double[] CumulativeSum(double[] x, double[] f)
        {
            var F = new double[x.Length];
            for (int i = 1; i < x.Length; i++)
            {
                F[i] = F[i - 1] + f[i - 1] * (x[i] - x[i - 1]);
            }
            return F;
        }

        private static double QueryCumsum(double q, double[] x, double[] F, double[] f)
        {
            if (q <= x[0])
                return 0.0;
            if (q >= x[x.Length - 1])
                return F[F.Length - 1];
            
            // Find index i such that x[i] <= q < x[i+1]
            int i = Array.BinarySearch(x, q);
            if (i < 0)
                i = ~i - 1;
            
            return F[i] + f[i] * (q - x[i]);
        }

        private static double[] SmoothOnCorners(double[] x, double[] f, double window, double scale = 1.0, string mode = "sum")
        {
            var F = CumulativeSum(x, f);
            var g = new double[f.Length];
            
            for (int i = 0; i < x.Length; i++)
            {
                double s = x[i];
                double a = Math.Max(s - window, x[0]);
                double b = Math.Min(s + window, x[x.Length - 1]);
                double val = QueryCumsum(b, x, F, f) - QueryCumsum(a, x, F, f);
                
                if (mode == "avg")
                {
                    g[i] = (b - a) > 0 ? val / (b - a) : 0.0;
                }
                else
                {
                    g[i] = scale * val;
                }
            }
            
            return g;
        }

        private static double[] InterpValues(double[] newX, double[] oldX, double[] oldVals)
        {
            var newVals = new double[newX.Length];
            
            for (int i = 0; i < newX.Length; i++)
            {
                // Find position in oldX
                int pos = Array.BinarySearch(oldX, newX[i]);
                if (pos >= 0)
                {
                    // Exact match
                    newVals[i] = oldVals[pos];
                }
                else
                {
                    // Need to interpolate
                    pos = ~pos;
                    if (pos == 0)
                    {
                        newVals[i] = oldVals[0];
                    }
                    else if (pos >= oldX.Length)
                    {
                        newVals[i] = oldVals[oldX.Length - 1];
                    }
                    else
                    {
                        // Linear interpolation
                        double ratio = (newX[i] - oldX[pos - 1]) / (oldX[pos] - oldX[pos - 1]);
                        newVals[i] = oldVals[pos - 1] + ratio * (oldVals[pos] - oldVals[pos - 1]);
                    }
                }
            }
            
            return newVals;
        }

        private static double[] StepInterp(double[] newX, double[] oldX, double[] oldVals)
        {
            var result = new double[newX.Length];
            
            for (int i = 0; i < newX.Length; i++)
            {
                int pos = Array.BinarySearch(oldX, newX[i]);
                if (pos < 0)
                    pos = ~pos - 1;
                
                pos = Math.Max(0, Math.Min(pos, oldVals.Length - 1));
                result[i] = oldVals[pos];
            }
            
            return result;
        }

        private static double RescaleHigh(double sr)
        {
            if (sr <= 9)
                return sr;
            return 9 + (sr - 9) * (1 / 1.2);
        }

        private static (int, int, int) FindNextNoteInColumn(
            (int, int, int) note, 
            Dictionary<int, List<int>> timesByColumn,
            List<List<(int, int, int)>> noteSeqByColumn)
        {
            var (k, h, t) = note;
            var times = timesByColumn[k];
            int idx = times.BinarySearch(h);
            if (idx < 0) idx = ~idx;
            
            return (idx + 1 < noteSeqByColumn[k].Count) 
                ? noteSeqByColumn[k][idx + 1] 
                : (0, 1000000000, 1000000000);
        }

        public static double Calculate(HitObject[] hitObjects, double rate = 1.0)
        {
            try
            {
                // Convert HitObjects to the tuple format used in the algorithm
                var noteSeq = new List<(int, int, int)>();
                foreach (var obj in hitObjects)
                {
                    int column = obj.Column;
                    int startTime = (int)obj.StartTime;
                    int endTime = (int)obj.EndTime;

                    startTime = (int)Math.Floor(startTime / rate);
                    if (endTime >= 0)
                        endTime = (int)Math.Floor(endTime / rate);

                    noteSeq.Add((column, startTime, endTime));
                }

                // Sort notes by time and then column
                noteSeq = noteSeq.OrderBy(n => n.Item2).ThenBy(n => n.Item1).ToList();

                // Basic setup
                int K = noteSeq.Select(n => n.Item1).Max() + 1; // Number of columns
                int T = Math.Max(
                    noteSeq.Max(n => n.Item2),
                    noteSeq.Where(n => n.Item3 >= 0).Select(n => n.Item3).DefaultIfEmpty(0).Max()
                ) + 1;

                // Hit leniency x
                double x = 0.3 * Math.Pow((64.5 - Math.Ceiling((double)(K * 3))) / 500, 0.5);
                x = Math.Min(x, 0.6 * (x - 0.09) + 0.09);

                // Group notes by column
                var noteDict = new Dictionary<int, List<(int, int, int)>>();
                foreach (var note in noteSeq)
                {
                    int col = note.Item1;
                    if (!noteDict.ContainsKey(col))
                        noteDict[col] = new List<(int, int, int)>();
                    noteDict[col].Add(note);
                }

                var noteSeqByColumn = noteDict.OrderBy(kvp => kvp.Key)
                                            .Select(kvp => kvp.Value)
                                            .ToList();

                // Long notes
                var lnSeq = noteSeq.Where(n => n.Item3 >= 0).ToList();
                var tailSeq = lnSeq.OrderBy(n => n.Item3).ToList();

                var lnDict = new Dictionary<int, List<(int, int, int)>>();
                foreach (var note in lnSeq)
                {
                    int col = note.Item1;
                    if (!lnDict.ContainsKey(col))
                        lnDict[col] = new List<(int, int, int)>();
                    lnDict[col].Add(note);
                }

                var lnSeqByColumn = lnDict.OrderBy(kvp => kvp.Key)
                                        .Select(kvp => kvp.Value)
                                        .ToList();

                // Get corners for different components
                var (allCorners, baseCorners, aCorners) = GetCorners(T, noteSeq);

                // Key usage calculations
                var keyUsage = GetKeyUsage(K, T, noteSeq, baseCorners);
                var activeColumns = new List<int>[baseCorners.Length];
                for (int i = 0; i < baseCorners.Length; i++)
                {
                    activeColumns[i] = new List<int>();
                    for (int k = 0; k < K; k++)
                    {
                        if (keyUsage[k][i])
                            activeColumns[i].Add(k);
                    }
                }

                var keyUsage400 = GetKeyUsage400(K, T, noteSeq, baseCorners);
                var anchor = ComputeAnchor(K, keyUsage400, baseCorners);

                var (deltaKs, jbar) = ComputeJbar(K, T, x, noteSeqByColumn, baseCorners);
                jbar = InterpValues(allCorners, baseCorners, jbar);

                var xbar = ComputeXbar(K, T, x, noteSeqByColumn, activeColumns, baseCorners);
                xbar = InterpValues(allCorners, baseCorners, xbar);

                // LN bodies sparse representation
                var lnRep = LnBodiesCountSparseRepresentation(lnSeq, T);

                var pbar = ComputePbar(K, T, x, noteSeq, lnRep, anchor, baseCorners);
                pbar = InterpValues(allCorners, baseCorners, pbar);

                var abar = ComputeAbar(K, T, x, noteSeqByColumn, activeColumns, deltaKs, aCorners, baseCorners);
                abar = InterpValues(allCorners, aCorners, abar);

                var rbar = ComputeRbar(K, T, x, noteSeqByColumn, tailSeq, baseCorners);
                rbar = InterpValues(allCorners, baseCorners, rbar);

                var (cStep, ksStep) = ComputeCAndKs(K, T, noteSeq, keyUsage, baseCorners);
                var cArr = StepInterp(allCorners, baseCorners, cStep);
                var ksArr = StepInterp(allCorners, baseCorners, ksStep);

                // Final Calculations
                var sAll = new double[allCorners.Length];
                var tAll = new double[allCorners.Length];
                var dAll = new double[allCorners.Length];

                for (int i = 0; i < allCorners.Length; i++)
                {
                    double term1 = Math.Pow(abar[i], 3.0 / ksArr[i]) * Math.Min(jbar[i], 8 + 0.85 * jbar[i]);
                    double term2 = Math.Pow(abar[i], 2.0 / 3.0) * (0.8 * pbar[i] + rbar[i] * 35 / (cArr[i] + 8));

                    sAll[i] = Math.Pow(0.4 * Math.Pow(term1, 1.5) + (1 - 0.4) * Math.Pow(term2, 1.5), 2.0 / 3.0);
                    tAll[i] = (Math.Pow(abar[i], 3.0 / ksArr[i]) * xbar[i]) / (xbar[i] + sAll[i] + 1);
                    dAll[i] = 2.7 * Math.Pow(sAll[i], 0.5) * Math.Pow(tAll[i], 1.5) + sAll[i] * 0.27;
                }

                // Calculate gaps for weighting
                var gaps = new double[allCorners.Length];
                gaps[0] = (allCorners[1] - allCorners[0]) / 2.0;
                gaps[allCorners.Length - 1] = (allCorners[allCorners.Length - 1] - allCorners[allCorners.Length - 2]) / 2.0;

                for (int i = 1; i < allCorners.Length - 1; i++)
                {
                    gaps[i] = (allCorners[i + 1] - allCorners[i - 1]) / 2.0;
                }

                // Effective weights and sorting
                var effectiveWeights = new double[allCorners.Length];
                for (int i = 0; i < allCorners.Length; i++)
                {
                    effectiveWeights[i] = cArr[i] * gaps[i];
                }

                // Sort difficulties and weights
                var sorted = Enumerable.Range(0, dAll.Length)
                                    .OrderBy(i => dAll[i])
                                    .ToArray();
                var dSorted = sorted.Select(i => dAll[i]).ToArray();
                var wSorted = sorted.Select(i => effectiveWeights[i]).ToArray();

                // Cumulative weights
                var cumWeights = new double[wSorted.Length];
                cumWeights[0] = wSorted[0];
                for (int i = 1; i < wSorted.Length; i++)
                {
                    cumWeights[i] = cumWeights[i - 1] + wSorted[i];
                }

                double totalWeight = cumWeights[cumWeights.Length - 1];
                var normCumWeights = cumWeights.Select(w => w / totalWeight).ToArray();

                // Target percentiles
                var targetPercentiles = new[] { 0.945, 0.935, 0.925, 0.915, 0.845, 0.835, 0.825, 0.815 };
                var indices = new int[targetPercentiles.Length];

                for (int i = 0; i < targetPercentiles.Length; i++)
                {
                    indices[i] = Array.BinarySearch(normCumWeights, targetPercentiles[i]);
                    if (indices[i] < 0)
                        indices[i] = ~indices[i];
                    indices[i] = Math.Min(indices[i], dSorted.Length - 1);
                }

                // Calculate percentiles
                double percentile93 = 0;
                for (int i = 0; i < 4; i++)
                {
                    percentile93 += dSorted[indices[i]];
                }
                percentile93 /= 4;

                double percentile83 = 0;
                for (int i = 4; i < 8; i++)
                {
                    percentile83 += dSorted[indices[i]];
                }
                percentile83 /= 4;

                // Weighted mean calculation
                double numerator = 0;
                double denominator = 0;
                for (int i = 0; i < dSorted.Length; i++)
                {
                    numerator += Math.Pow(dSorted[i], 5) * wSorted[i];
                    denominator += wSorted[i];
                }
                double weightedMean = Math.Pow(numerator / denominator, 1.0 / 5.0);

                // Final SR calculation
                double sr = (0.88 * percentile93) * 0.25 + (0.94 * percentile83) * 0.2 + weightedMean * 0.55;
                sr = Math.Pow(sr, 1.0) / Math.Pow(8, 1.0) * 8;

                double totalNotes = noteSeq.Count + 0.5 * lnSeq.Sum(n => Math.Min(n.Item3 - n.Item2, 1000) / 200.0);
                sr *= totalNotes / (totalNotes + 60);

                sr = RescaleHigh(sr);
                sr *= 0.975;

                return sr;
            }
            catch
            {
                return -1.0;
            }
        }

        private static (double[], double[], double[]) GetCorners(int T, List<(int, int, int)> noteSeq)
        {
            var cornersBase = new HashSet<double>();
            foreach (var (_, h, t) in noteSeq)
            {
                cornersBase.Add(h);
                if (t >= 0)
                    cornersBase.Add(t);
            }

            var additionalCorners = new HashSet<double>();
            foreach (var s in cornersBase)
            {
                additionalCorners.Add(s + 501);
                additionalCorners.Add(s - 499);
                additionalCorners.Add(s + 1); // To resolve the Dirac-Delta additions exactly at notes
            }
            cornersBase.UnionWith(additionalCorners);
            cornersBase.Add(0);
            cornersBase.Add(T);
            
            var baseCorners = cornersBase.Where(s => 0 <= s && s <= T).OrderBy(s => s).ToArray();
            
            // For Abar, unsmoothed values (KU and A) usually change at ±500 relative to note boundaries, hence ±1000 overall.
            var cornersA = new HashSet<double>();
            foreach (var (_, h, t) in noteSeq)
            {
                cornersA.Add(h);
                if (t >= 0)
                    cornersA.Add(t);
            }

            var additionalCornersA = new HashSet<double>();
            foreach (var s in cornersA)
            {
                additionalCornersA.Add(s + 1000);
                additionalCornersA.Add(s - 1000);
            }
            cornersA.UnionWith(additionalCornersA);
            cornersA.Add(0);
            cornersA.Add(T);
            
            var aCorners = cornersA.Where(s => 0 <= s && s <= T).OrderBy(s => s).ToArray();
            
            // Take the union of all corners for final interpolation
            var allCorners = baseCorners.Union(aCorners).OrderBy(s => s).ToArray();
            
            return (allCorners, baseCorners, aCorners);
        }

        private static bool[][] GetKeyUsage(int K, int T, List<(int, int, int)> noteSeq, double[] baseCorners)
        {
            var keyUsage = new bool[K][];
            for (int k = 0; k < K; k++)
            {
                keyUsage[k] = new bool[baseCorners.Length];
            }
            
            foreach (var (k, h, t) in noteSeq)
            {
                double startTime = Math.Max(h - 150, 0);
                double endTime = (t < 0) ? (h + 150) : Math.Min(t + 150, T - 1);
                
                int leftIdx = Array.BinarySearch(baseCorners, startTime);
                if (leftIdx < 0) leftIdx = ~leftIdx;
                
                int rightIdx = Array.BinarySearch(baseCorners, endTime);
                if (rightIdx < 0) rightIdx = ~rightIdx;
                
                for (int idx = leftIdx; idx < rightIdx; idx++)
                {
                    keyUsage[k][idx] = true;
                }
            }
            
            return keyUsage;
        }

        private static double[][] GetKeyUsage400(int K, int T, List<(int, int, int)> noteSeq, double[] baseCorners)
        {
            var keyUsage400 = new double[K][];
            for (int k = 0; k < K; k++)
            {
                keyUsage400[k] = new double[baseCorners.Length];
            }
            
            foreach (var (k, h, t) in noteSeq)
            {
                double startTime = Math.Max(h, 0);
                double endTime = (t < 0) ? h : Math.Min(t, T - 1);
                
                int left400Idx = Array.BinarySearch(baseCorners, startTime - 400);
                if (left400Idx < 0) left400Idx = ~left400Idx;
                
                int leftIdx = Array.BinarySearch(baseCorners, startTime);
                if (leftIdx < 0) leftIdx = ~leftIdx;
                
                int rightIdx = Array.BinarySearch(baseCorners, endTime);
                if (rightIdx < 0) rightIdx = ~rightIdx;
                
                int right400Idx = Array.BinarySearch(baseCorners, endTime + 400);
                if (right400Idx < 0) right400Idx = ~right400Idx;
                
                // Add constant value for notes/holds
                for (int idx = leftIdx; idx < rightIdx; idx++)
                {
                    keyUsage400[k][idx] += 3.75 + Math.Min(endTime - startTime, 1500) / 150;
                }
                
                // Add quadratic falloff before start
                for (int idx = left400Idx; idx < leftIdx; idx++)
                {
                    double dist = baseCorners[idx] - startTime;
                    keyUsage400[k][idx] += 3.75 - 3.75 / (400 * 400) * (dist * dist);
                }
                
                // Add quadratic falloff after end
                for (int idx = rightIdx; idx < right400Idx; idx++)
                {
                    double dist = baseCorners[idx] - endTime;
                    keyUsage400[k][idx] += 3.75 - 3.75 / (400 * 400) * (Math.Abs(dist) * Math.Abs(dist));
                }
            }
            
            return keyUsage400;
        }

        private static double[] ComputeAnchor(int K, double[][] keyUsage400, double[] baseCorners)
        {
            var anchor = new double[baseCorners.Length];
            
            for (int idx = 0; idx < baseCorners.Length; idx++)
            {
                // Collect the counts for each group at this base corner
                var counts = new double[K];
                for (int k = 0; k < K; k++)
                {
                    counts[k] = keyUsage400[k][idx];
                }
                
                // Sort in descending order
                Array.Sort(counts);
                Array.Reverse(counts);
                
                // Filter out zeros
                var nonzeroCounts = counts.Where(c => c != 0).ToArray();
                
                if (nonzeroCounts.Length > 1)
                {
                    double walk = 0;
                    double maxWalk = 0;
                    
                    for (int i = 0; i < nonzeroCounts.Length - 1; i++)
                    {
                        double ratio = nonzeroCounts[i + 1] / nonzeroCounts[i];
                        walk += nonzeroCounts[i] * (1 - 4 * Math.Pow(0.5 - ratio, 2));
                        maxWalk += nonzeroCounts[i];
                    }
                    
                    anchor[idx] = walk / maxWalk;
                }
                else
                {
                    anchor[idx] = 0;
                }
            }
            
            // Apply transformation
            for (int i = 0; i < anchor.Length; i++)
            {
                anchor[i] = 1 + Math.Min(anchor[i] - 0.18, 5 * Math.Pow(anchor[i] - 0.22, 3));
            }
            
            return anchor;
        }

        private static (double[][], double[]) ComputeJbar(int K, int T, double x, List<List<(int, int, int)>> noteSeqByColumn, double[] baseCorners)
        {
            var JKs = new double[K][];
            var deltaKs = new double[K][];
            
            for (int k = 0; k < K; k++)
            {
                JKs[k] = new double[baseCorners.Length];
                deltaKs[k] = new double[baseCorners.Length];
                
                // Initialize deltaKs with a high value
                for (int i = 0; i < baseCorners.Length; i++)
                {
                    deltaKs[k][i] = 1e9;
                }
            }
            
            Func<double, double> jackNerfer = delta => 1 - 7e-5 * Math.Pow(0.15 + Math.Abs(delta - 0.08), -4);
            
            for (int k = 0; k < K; k++)
            {
                var notes = noteSeqByColumn[k];
                for (int i = 0; i < notes.Count - 1; i++)
                {
                    double start = notes[i].Item2;
                    double end = notes[i + 1].Item2;
                    
                    // Find indices in base_corners that lie in [start, end)
                    int leftIdx = Array.BinarySearch(baseCorners, start);
                    if (leftIdx < 0) leftIdx = ~leftIdx;
                    
                    int rightIdx = Array.BinarySearch(baseCorners, end);
                    if (rightIdx < 0) rightIdx = ~rightIdx;
                    
                    if (leftIdx >= rightIdx)
                        continue;
                    
                    double delta = 0.001 * (end - start);
                    double val = Math.Pow(delta, -1) * Math.Pow(delta + 0.11 * Math.Pow(x, 1.0/4.0), -1);
                    double JVal = val * jackNerfer(delta);
                    
                    for (int idx = leftIdx; idx < rightIdx; idx++)
                    {
                        JKs[k][idx] = JVal;
                        deltaKs[k][idx] = delta;
                    }
                }
            }
            
            // Smooth each column's JKs
            var JbarKs = new double[K][];
            for (int k = 0; k < K; k++)
            {
                JbarKs[k] = SmoothOnCorners(baseCorners, JKs[k], 500, 0.001, "sum");
            }
            
            // Aggregate across columns using weighted average
            var Jbar = new double[baseCorners.Length];
            for (int i = 0; i < baseCorners.Length; i++)
            {
                var vals = new double[K];
                var weights = new double[K];
                
                for (int k = 0; k < K; k++)
                {
                    vals[k] = JbarKs[k][i];
                    weights[k] = 1 / deltaKs[k][i];
                }
                
                double num = 0;
                double den = 0;
                
                for (int k = 0; k < K; k++)
                {
                    num += Math.Pow(Math.Max(vals[k], 0), 5) * weights[k];
                    den += weights[k];
                }
                
                Jbar[i] = num / Math.Max(1e-9, den);
                Jbar[i] = Math.Pow(Jbar[i], 1.0/5.0);
            }
            
            return (deltaKs, Jbar);
        }

        private static double[] ComputeXbar(int K, int T, double x, List<List<(int, int, int)>> noteSeqByColumn, List<int>[] activeColumns, double[] baseCorners)
        {
            // Cross matrix from the Python code
            var crossMatrix = new List<double[]> {
                new double[] { -1 },
                new double[] { 0.075, 0.075 },
                new double[] { 0.125, 0.05, 0.125 },
                new double[] { 0.125, 0.125, 0.125, 0.125 },
                new double[] { 0.175, 0.25, 0.05, 0.25, 0.175 },
                new double[] { 0.175, 0.25, 0.175, 0.175, 0.25, 0.175 },
                new double[] { 0.225, 0.35, 0.25, 0.05, 0.25, 0.35, 0.225 },
                new double[] { 0.225, 0.35, 0.25, 0.225, 0.225, 0.25, 0.35, 0.225 },
                new double[] { 0.275, 0.45, 0.35, 0.25, 0.05, 0.25, 0.35, 0.45, 0.275 },
                new double[] { 0.275, 0.45, 0.35, 0.25, 0.275, 0.275, 0.25, 0.35, 0.45, 0.275 },
                new double[] { 0.325, 0.55, 0.45, 0.35, 0.25, 0.05, 0.25, 0.35, 0.45, 0.55, 0.325 }
            };
            
            var XKs = new double[K + 1][];
            var fastCross = new double[K + 1][];
            
            for (int k = 0; k <= K; k++)
            {
                XKs[k] = new double[baseCorners.Length];
                fastCross[k] = new double[baseCorners.Length];
            }
            
            double[] crossCoeff = crossMatrix[K];
            
            for (int k = 0; k <= K; k++)
            {
                List<(int, int, int)> notesInPair;
                
                if (k == 0)
                    notesInPair = noteSeqByColumn[0];
                else if (k == K)
                    notesInPair = noteSeqByColumn[K - 1];
                else
                    notesInPair = noteSeqByColumn[k - 1].Concat(noteSeqByColumn[k])
                                                    .OrderBy(n => n.Item2)
                                                    .ToList();
                
                for (int i = 1; i < notesInPair.Count; i++)
                {
                    double start = notesInPair[i - 1].Item2;
                    double end = notesInPair[i].Item2;
                    
                    int idxStart = Array.BinarySearch(baseCorners, start);
                    if (idxStart < 0) idxStart = ~idxStart;
                    
                    int idxEnd = Array.BinarySearch(baseCorners, end);
                    if (idxEnd < 0) idxEnd = ~idxEnd;
                    
                    if (idxStart >= idxEnd)
                        continue;
                    
                    double delta = 0.001 * (notesInPair[i].Item2 - notesInPair[i - 1].Item2);
                    double val = 0.16 * Math.Pow(Math.Max(x, delta), -2);
                    
                    bool startHasPrevCol = k > 0 && activeColumns[idxStart].Contains(k - 1);
                    bool startHasThisCol = k < K && activeColumns[idxStart].Contains(k);
                    bool endHasPrevCol = k > 0 && activeColumns[idxEnd].Contains(k - 1);
                    bool endHasThisCol = k < K && activeColumns[idxEnd].Contains(k);
                    
                    if ((!startHasPrevCol && !endHasPrevCol) || (!startHasThisCol && !endHasThisCol))
                    {
                        val *= (1 - crossCoeff[k]);
                    }
                    
                    for (int idx = idxStart; idx < idxEnd; idx++)
                    {
                        XKs[k][idx] = val;
                        fastCross[k][idx] = Math.Max(0, 0.4 * Math.Pow(Math.Max(Math.Max(delta, 0.06), 0.75 * x), -2) - 80);
                    }
                }
            }
            
            var XBase = new double[baseCorners.Length];
            for (int i = 0; i < baseCorners.Length; i++)
            {
                // Sum XKs weighted by crossCoeff
                for (int k = 0; k <= K; k++)
                {
                    XBase[i] += XKs[k][i] * crossCoeff[k];
                }
                
                // Add cross-column effects
                for (int k = 0; k < K; k++)
                {
                    XBase[i] += Math.Sqrt(fastCross[k][i] * crossCoeff[k] * fastCross[k + 1][i] * crossCoeff[k + 1]);
                }
            }
            
            var Xbar = SmoothOnCorners(baseCorners, XBase, 500, 0.001, "sum");
            return Xbar;
        }

        private static (double[], double[], double[]) LnBodiesCountSparseRepresentation(List<(int, int, int)> lnSeq, int T)
        {
            var diff = new Dictionary<double, double>();
            
            foreach (var (k, h, t) in lnSeq)
            {
                double t0 = Math.Min(h + 60, t);
                double t1 = Math.Min(h + 120, t);
                
                if (!diff.ContainsKey(t0))
                    diff[t0] = 0;
                diff[t0] += 1.3;
                
                if (!diff.ContainsKey(t1))
                    diff[t1] = 0;
                diff[t1] += (-1.3 + 1);  // net change at t1: -1.3 from first part, then +1
                
                if (!diff.ContainsKey(t))
                    diff[t] = 0;
                diff[t] -= 1;
            }
            
            // The breakpoints are the times where changes occur
            var points = new HashSet<double> { 0, T };
            points.UnionWith(diff.Keys);
            var pointsArray = points.OrderBy(p => p).ToArray();
            
            // Build piecewise constant values and a cumulative sum
            var values = new double[pointsArray.Length - 1];
            var cumsum = new double[pointsArray.Length];
            double curr = 0.0;
            
            for (int i = 0; i < pointsArray.Length - 1; i++)
            {
                double t = pointsArray[i];
                
                // If there is a change at t, update the running value
                if (diff.ContainsKey(t))
                    curr += diff[t];
                
                double v = Math.Min(curr, 2.5 + 0.5 * curr);
                values[i] = v;
                
                // Compute cumulative sum on the interval [points[i], points[i+1])
                double segLength = pointsArray[i + 1] - pointsArray[i];
                cumsum[i + 1] = cumsum[i] + segLength * v;
            }
            
            return (pointsArray, cumsum, values);
        }

        private static double LnSum(double a, double b, (double[], double[], double[]) lnRep)
        {
            var (points, cumsum, values) = lnRep;
            
            // Locate the segments that contain a and b
            int i = Array.BinarySearch(points, a);
            if (i < 0) i = ~i - 1;
            i = Math.Max(0, i);
            
            int j = Array.BinarySearch(points, b);
            if (j < 0) j = ~j - 1;
            j = Math.Max(0, j);
            
            double total = 0.0;
            
            if (i == j)
            {
                // Both a and b lie in the same segment
                total = (b - a) * values[i];
            }
            else
            {
                // First segment: from a to the end of the i-th segment
                total += (points[i + 1] - a) * values[i];
                
                // Full segments between i+1 and j-1
                total += cumsum[j] - cumsum[i + 1];
                
                // Last segment: from start of segment j to b
                total += (b - points[j]) * values[j];
            }
            
            return total;
        }

        private static double[] ComputePbar(int K, int T, double x, List<(int, int, int)> noteSeq, (double[], double[], double[]) lnRep, double[] anchor, double[] baseCorners)
        {
            Func<double, double> streamBooster = delta => 
                (160 < (7.5 / delta) && (7.5 / delta) < 360) ? 
                1 + 1.7e-7 * ((7.5 / delta) - 160) * Math.Pow((7.5 / delta) - 360, 2) : 
                1;
            
            var PStep = new double[baseCorners.Length];
            
            for (int i = 0; i < noteSeq.Count - 1; i++)
            {
                double hL = noteSeq[i].Item2;
                double hR = noteSeq[i + 1].Item2;
                double deltaTime = hR - hL;
                
                if (deltaTime < 1e-9)
                {
                    // Dirac delta case: when notes occur at the same time
                    double spike = 1000 * Math.Pow(0.02 * (4 / x - 24), 1.0/4.0);
                    
                    int idx1 = Array.BinarySearch(baseCorners, hL);
                    if (idx1 < 0) idx1 = ~idx1;
                    
                    int idx2 = Array.BinarySearch(baseCorners, hL);
                    if (idx2 < 0) idx2 = ~idx2;
                    
                    for (int idx = idx1; idx < idx2; idx++)
                    {
                        if (idx >= 0 && idx < PStep.Length)
                            PStep[idx] += spike;
                    }
                    
                    // Continue so that we add a spike for each additional simultaneous note
                    continue;
                }
                
                // For the regular case where deltaTime > 0
                int start = Array.BinarySearch(baseCorners, hL);
                if (start < 0) start = ~start;
                
                int end = Array.BinarySearch(baseCorners, hR);
                if (end < 0) end = ~end;
                
                if (start >= end)
                    continue;
                
                double delta = 0.001 * deltaTime;
                double v = 1 + 6 * 0.001 * LnSum(hL, hR, lnRep);
                double bVal = streamBooster(delta);
                double inc;
                
                if (delta < 2 * x / 3)
                {
                    inc = 1.0 / delta * Math.Pow(0.08 * Math.Pow(x, -1) * (1 - 24 * Math.Pow(x, -1) * Math.Pow(delta - x/2, 2)), 1.0/4.0) * bVal * v;
                }
                else
                {
                    inc = 1.0 / delta * Math.Pow(0.08 * Math.Pow(x, -1) * (1 - 24 * Math.Pow(x, -1) * Math.Pow(x/6, 2)), 1.0/4.0) * bVal * v;
                }
                
                for (int idx = start; idx < end; idx++)
                {
                    PStep[idx] += Math.Min(inc * anchor[idx], Math.Max(inc, inc * 2 - 10));
                }
            }
            
            var Pbar = SmoothOnCorners(baseCorners, PStep, 500, 0.001, "sum");
            return Pbar;
        }

        private static double[] ComputeAbar(int K, int T, double x, List<List<(int, int, int)>> noteSeqByColumn, 
                                        List<int>[] activeColumns, double[][] deltaKs, double[] aCorners, double[] baseCorners)
        {
            var dks = new double[K - 1][];
            for (int k = 0; k < K - 1; k++)
            {
                dks[k] = new double[baseCorners.Length];
            }
            
            for (int i = 0; i < baseCorners.Length; i++)
            {
                var cols = activeColumns[i];
                for (int j = 0; j < cols.Count - 1; j++)
                {
                    int k0 = cols[j];
                    int k1 = cols[j + 1];
                    
                    if (k0 < K - 1)
                    {
                        dks[k0][i] = Math.Abs(deltaKs[k0][i] - deltaKs[k1][i]) + 
                                    0.4 * Math.Max(0, Math.Max(deltaKs[k0][i], deltaKs[k1][i]) - 0.11);
                    }
                }
            }
            
            var AStep = new double[aCorners.Length];
            for (int i = 0; i < aCorners.Length; i++)
            {
                AStep[i] = 1.0;
            }
            
            for (int i = 0; i < aCorners.Length; i++)
            {
                double s = aCorners[i];
                int idx = Array.BinarySearch(baseCorners, s);
                if (idx < 0) idx = ~idx - 1;
                if (idx >= baseCorners.Length) idx = baseCorners.Length - 1;
                
                var cols = activeColumns[idx];
                for (int j = 0; j < cols.Count - 1; j++)
                {
                    int k0 = cols[j];
                    int k1 = cols[j + 1];
                    
                    if (k0 >= K - 1) continue;
                    
                    double dVal = dks[k0][idx];
                    if (dVal < 0.02)
                    {
                        AStep[i] *= Math.Min(0.75 + 0.5 * Math.Max(deltaKs[k0][idx], deltaKs[k1][idx]), 1);
                    }
                    else if (dVal < 0.07)
                    {
                        AStep[i] *= Math.Min(0.65 + 5 * dVal + 0.5 * Math.Max(deltaKs[k0][idx], deltaKs[k1][idx]), 1);
                    }
                    // Otherwise leave AStep[i] unchanged
                }
            }
            
            var Abar = SmoothOnCorners(aCorners, AStep, 250, 1.0, "avg");
            return Abar;
        }

        private static double[] ComputeRbar(int K, int T, double x, List<List<(int, int, int)>> noteSeqByColumn, 
                                        List<(int, int, int)> tailSeq, double[] baseCorners)
        {
            var IArr = new double[baseCorners.Length];
            var RStep = new double[baseCorners.Length];
            
            var timesByColumn = new Dictionary<int, List<int>>();
            for (int i = 0; i < noteSeqByColumn.Count; i++)
            {
                timesByColumn[i] = noteSeqByColumn[i].Select(note => note.Item2).ToList();
            }
            
            // Release Index
            var IList = new double[tailSeq.Count];
            for (int i = 0; i < tailSeq.Count; i++)
            {
                var (k, hI, tI) = tailSeq[i];
                var nextNote = FindNextNoteInColumn((k, hI, tI), timesByColumn, noteSeqByColumn);
                double hJ = nextNote.Item2;
                
                double IH = 0.001 * Math.Abs(tI - hI - 80) / x;
                double IT = 0.001 * Math.Abs(hJ - tI - 80) / x;
                
                IList[i] = 2 / (2 + Math.Exp(-5 * (IH - 0.75)) + Math.Exp(-5 * (IT - 0.75)));
            }
            
            // For each interval between successive tail times, assign I and R
            for (int i = 0; i < tailSeq.Count - 1; i++)
            {
                double tStart = tailSeq[i].Item3;
                double tEnd = tailSeq[i + 1].Item3;
                
                int leftIdx = Array.BinarySearch(baseCorners, tStart);
                if (leftIdx < 0) leftIdx = ~leftIdx;
                
                int rightIdx = Array.BinarySearch(baseCorners, tEnd);
                if (rightIdx < 0) rightIdx = ~rightIdx;
                
                if (leftIdx >= rightIdx)
                    continue;
                
                for (int idx = leftIdx; idx < rightIdx; idx++)
                {
                    IArr[idx] = 1 + IList[i];
                }
                
                double deltaR = 0.001 * (tailSeq[i + 1].Item3 - tailSeq[i].Item3);
                double rValue = 0.08 * Math.Pow(deltaR, -0.5) * Math.Pow(x, -1) * (1 + 0.8 * (IList[i] + IList[i + 1]));
                
                for (int idx = leftIdx; idx < rightIdx; idx++)
                {
                    RStep[idx] = rValue;
                }
            }
            
            var Rbar = SmoothOnCorners(baseCorners, RStep, 500, 0.001, "sum");
            return Rbar;
        }

        private static (double[], double[]) ComputeCAndKs(int K, int T, List<(int, int, int)> noteSeq, 
                                                    bool[][] keyUsage, double[] baseCorners)
        {
            // C(s): count of notes within 500 ms
            var noteHitTimes = noteSeq.Select(n => (double)n.Item2).OrderBy(t => t).ToArray();
            var CStep = new double[baseCorners.Length];
            
            for (int i = 0; i < baseCorners.Length; i++)
            {
                double s = baseCorners[i];
                double low = s - 500;
                double high = s + 500;
                
                // Use binary search on noteHitTimes
                int lowIdx = Array.BinarySearch(noteHitTimes, low);
                if (lowIdx < 0) lowIdx = ~lowIdx;
                
                int highIdx = Array.BinarySearch(noteHitTimes, high);
                if (highIdx < 0) highIdx = ~highIdx;
                
                CStep[i] = highIdx - lowIdx;
            }
            
            // Ks: local key usage count (minimum 1)
            var KsStep = new double[baseCorners.Length];
            for (int i = 0; i < baseCorners.Length; i++)
            {
                int keyCount = 0;
                for (int k = 0; k < K; k++)
                {
                    if (keyUsage[k][i])
                        keyCount++;
                }
                KsStep[i] = Math.Max(keyCount, 1);
            }
            
            return (CStep, KsStep);
        }
    } 
}