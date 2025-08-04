using C4TX.SDL.KeyHandler;
using C4TX.SDL.Models;
using C4TX.SDL.Services;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static C4TX.SDL.Engine.GameEngine;
using static SDL2.SDL;
using static System.Formats.Asn1.AsnWriter;

namespace C4TX.SDL.Engine.Renderer
{
    public partial class RenderEngine
    {
        public static void RenderResults()
        {
            // Draw background
            DrawMenuBackground();

            // Create a main panel for results
            int panelWidth = (int)(_windowWidth * 0.95);
            int panelHeight = (int)(_windowHeight * 0.9);
            int panelX = (_windowWidth - panelWidth) / 2;
            int panelY = (_windowHeight - panelHeight) / 2;

            DrawPanel(panelX, panelY, panelWidth, panelHeight, Color._panelBgColor, Color._primaryColor);

            // Draw header with title
            RenderText("Results", _windowWidth / 2, panelY + 30, Color._accentColor, true, true);

            // Horizontal separator
            SDL_SetRenderDrawColor(_renderer, Color._primaryColor.r, Color._primaryColor.g, Color._primaryColor.b, 150);
            SDL_Rect separatorLine = new SDL_Rect
            {
                x = panelX + 50,
                y = panelY + 60,
                w = panelWidth - 100,
                h = 2
            };
            SDL_RenderFillRect(_renderer, ref separatorLine);

            // Check if we're displaying a replay or live results
            bool isReplay = _noteHits.Count == 0 && _selectedScore != null && _selectedScore.NoteHits.Count > 0;

            // Use the proper data source based on whether this is a replay or live results
            List<(double NoteTime, double HitTime, double Deviation)> hitData;
            if (isReplay && _selectedScore != null)
            {
                // Extract note hit data from the selected score
                hitData = _selectedScore.NoteHits.Select(nh => (nh.NoteTime, nh.HitTime, nh.Deviation)).ToList();

                // Draw replay indicator
                RenderText("REPLAY", _windowWidth / 2, panelY + 80, Color._highlightColor, false, true);
            }
            else
            {
                // Use current session data
                hitData = _noteHits;
            }

            // Get the current model name
            string accuracyModelName = _resultScreenAccuracyModel.ToString();

            // Calculate accuracy based on the selected model
            double displayAccuracy = _currentAccuracy; // Default to current accuracy

            if (hitData.Count > 0)
            {
                // Create temporary accuracy service with the selected model
                var tempAccuracyService = new AccuracyService(_resultScreenAccuracyModel);

                // Set the hit window explicitly
                tempAccuracyService.SetHitWindow(_hitWindowMs);

                // Recalculate accuracy using the selected model
                double totalAccuracy = 0;
                foreach (var hit in hitData)
                {
                    // Calculate accuracy for this hit using the selected model
                    double hitAccuracy = tempAccuracyService.CalculateAccuracy(Math.Abs(hit.Deviation));
                    totalAccuracy += hitAccuracy;
                }

                // Calculate average accuracy
                displayAccuracy = totalAccuracy / hitData.Count;
            }

            // Top panel layouts - Three columns with consistent spacing
            int contentY = panelY + 100;
            int contentHeight = (int)(panelHeight * 0.35);
            int panelSpacing = 20;

            // Left panel - Stats panel
            int leftPanelWidth = (int)(panelWidth * 0.2);
            int leftPanelX = panelX + PANEL_PADDING;

            // Middle panel - Graph
            int middlePanelWidth = (int)(panelWidth * 0.55);
            int middlePanelX = leftPanelX + leftPanelWidth + panelSpacing;

            // Right panel - Judgment breakdown
            int rightPanelWidth = (int)(panelWidth * 0.2);
            int rightPanelX = middlePanelX + middlePanelWidth + panelSpacing;

            // Calculate judgment counts - do this early as we need these values for multiple panels
            var judgmentCounts = CalculateJudgmentCounts(hitData);

            // Draw stats panel on the left
            DrawPanel(leftPanelX, contentY, leftPanelWidth, contentHeight,
                new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._primaryColor);

            // Draw overall stats with descriptions in the stats panel
            RenderText("Stats", leftPanelX + leftPanelWidth / 2, contentY + 20, Color._primaryColor, false, true);

            int labelX = leftPanelX + 20;
            int valueX = leftPanelX + leftPanelWidth - 20;
            int rowHeight = 30;
            int startY = contentY + 55;

            // Draw stats with nice formatting and appropriate colors - better aligned
            RenderText("Score", labelX, startY, Color._mutedTextColor, false, false);
            RenderText($"{_score}", valueX, startY, Color._textColor, false, true);

            RenderText("Max Combo", labelX, startY + rowHeight, Color._mutedTextColor, false, false);
            RenderText($"{_maxCombo}x", valueX, startY + rowHeight, Color._comboColor, false, true);

            RenderText("Accuracy", labelX, startY + rowHeight * 2, Color._mutedTextColor, false, false);

            // Format accuracy with consistent decimal places
            string accuracyText = $"{displayAccuracy:P2}";
            SDL_Color accuracyColor = displayAccuracy > 0.95 ? new SDL_Color { r = 255, g = 215, b = 0, a = 255 } : // Gold for ≥95%
                            displayAccuracy > 0.9 ? Color._successColor :  // Green for ≥90%
                            displayAccuracy > 0.8 ? new SDL_Color { r = 50, g = 205, b = 50, a = 255 } : // Light green for ≥80%
                            displayAccuracy > 0.6 ? Color._accentColor : // Orange for ≥60%
                            Color._errorColor; // Red for <60%

            RenderText(accuracyText, valueX, startY + rowHeight * 2, accuracyColor, false, true);

            RenderText("Model", labelX, startY + rowHeight * 3, Color._mutedTextColor, false, false);
            RenderText(accuracyModelName, valueX, startY + rowHeight * 3, Color._primaryColor, false, true);

            // Display the playback rate
            float displayRate = isReplay && _selectedScore != null ? _selectedScore.PlaybackRate : _currentRate;
            SDL_Color rateColor = displayRate == 1.0f ? Color._mutedTextColor : Color._accentColor;

            // Add rate below model
            RenderText("Rate", labelX, startY + rowHeight * 4, Color._mutedTextColor, false, false);
            RenderText($"{displayRate:F1}x", valueX, startY + rowHeight * 4, rateColor, false, true);

            // Draw judgment counts panel on the right
            DrawPanel(rightPanelX, contentY, rightPanelWidth, contentHeight,
                new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._primaryColor);

            // Draw judgment breakdown title
            RenderText("Judgment Breakdown", rightPanelX + rightPanelWidth / 2, contentY + 20, Color._primaryColor, false, true);

            // Draw judgment counts
            DrawJudgmentCounts(rightPanelX, contentY + 55, rightPanelWidth, rowHeight, judgmentCounts);

            // Draw graph in the middle
            if (hitData.Count > 0)
            {
                // Calculate graph dimensions
                int graphWidth = middlePanelWidth;
                int graphHeight = contentHeight;
                int graphX = middlePanelX;
                int graphY = contentY;

                // Draw graph panel
                DrawPanel(graphX, graphY, graphWidth, graphHeight,
                    new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._primaryColor);

                // Draw graph title and accuracy grades on the right
                RenderText("Note Timing Analysis", graphX + graphWidth / 2, graphY + 15, Color._primaryColor, false, true);

                int gradeX = graphX + graphWidth - 140;
                int gradeY = graphY + 40;

                // Calculate model-based judgment timing boundaries
                var judgmentTimings = CalculateJudgmentTimingBoundaries();

                // Draw judgment labels with their timing values based on the current model
                RenderText($"OK {judgmentTimings["OK"]}", gradeX, gradeY, Color._mutedTextColor, false, false);
                RenderText($"GOOD {judgmentTimings["Good"]}", gradeX, gradeY + 20, new SDL_Color { r = 180, g = 180, b = 255, a = 255 }, false, false);
                RenderText($"GREAT {judgmentTimings["Great"]}", gradeX, gradeY + 40, Color._accentColor, false, false);
                RenderText($"PERFECT {judgmentTimings["Perfect"]}", gradeX, gradeY + 60, Color._successColor, false, false);
                RenderText($"MARVELOUS {judgmentTimings["Marvelous"]}", gradeX, gradeY + 80, new SDL_Color { r = 255, g = 215, b = 0, a = 255 }, false, false);

                // Adjusted graph drawing area inside the panel
                int graphInnerPadding = 30;
                int graphInnerX = graphX + graphInnerPadding + 20; // Extra padding for ms labels
                int graphInnerY = graphY + graphInnerPadding + 10;
                int graphInnerWidth = graphWidth - graphInnerPadding * 2 - 150; // Account for grade display
                int graphInnerHeight = (int)(graphHeight - graphInnerPadding * 1.8);

                // Draw graph background
                SDL_SetRenderDrawColor(_renderer, 35, 35, 55, 255);
                SDL_Rect graphRect = new SDL_Rect
                {
                    x = graphInnerX,
                    y = graphInnerY,
                    w = graphInnerWidth,
                    h = graphInnerHeight
                };
                SDL_RenderFillRect(_renderer, ref graphRect);

                // Draw grid lines
                SDL_SetRenderDrawColor(_renderer, 60, 60, 80, 150);

                // Vertical grid lines (every 10 seconds)
                for (int i = 0; i <= 10; i++)
                {
                    int x = graphInnerX + i * graphInnerWidth / 10;
                    SDL_RenderDrawLine(_renderer, x, graphInnerY, x, graphInnerY + graphInnerHeight);

                    // Draw time labels - smaller and more subtle
                    int seconds = i * 10;
                    SDL_Color timeColor = new SDL_Color { r = 150, g = 150, b = 170, a = 255 };
                    RenderText($"{seconds}s", x, graphInnerY + graphInnerHeight + 8, timeColor, false, true);
                }

                // Define ms markers with corresponding y-positions
                var msMarkers = new[]
                {
                    (Label: $"+{_hitWindowMs}ms", YOffset: -1.0),
                    (Label: $"+{_hitWindowMs/2}ms", YOffset: -0.5),
                    (Label: "0ms", YOffset: 0.0),
                    (Label: $"-{_hitWindowMs/2}ms", YOffset: 0.5),
                    (Label: $"-{_hitWindowMs}ms", YOffset: 1.0),
                };

                // Draw horizontal grid lines with ms markers
                foreach (var marker in msMarkers)
                {
                    int y = graphInnerY + (int)(graphInnerHeight / 2 + marker.YOffset * graphInnerHeight / 2);
                    SDL_RenderDrawLine(_renderer, graphInnerX, y, graphInnerX + graphInnerWidth, y);

                    // Draw ms labels - aligned to the left of the graph
                    SDL_Color msColor = new SDL_Color { r = 150, g = 150, b = 170, a = 255 };
                    RenderText(marker.Label, graphInnerX - 40, y, msColor, false, true);
                }

                // Draw center line
                SDL_SetRenderDrawColor(_renderer, 180, 180, 180, 200);
                int centerY = graphInnerY + graphInnerHeight / 2;
                SDL_RenderDrawLine(_renderer, graphInnerX, centerY, graphInnerX + graphInnerWidth, centerY);

                // Draw accuracy model visualization
                DrawAccuracyModelVisualization(graphInnerX, graphInnerY, graphInnerWidth, graphInnerHeight, centerY);

                // Draw hit points with color coding
                double maxTime = hitData.Max(h => h.NoteTime);
                double minTime = hitData.Min(h => h.NoteTime);
                double timeRange = maxTime - minTime;

                foreach (var hit in hitData)
                {
                    // Calculate x position based on note time
                    double timeProgress = (hit.NoteTime - minTime) / timeRange;
                    int x = graphInnerX + (int)(timeProgress * graphInnerWidth);

                    // Calculate y position based on deviation
                    double maxDeviation = _hitWindowMs;
                    double yProgress = hit.Deviation / maxDeviation;
                    int y = centerY - (int)(yProgress * (graphInnerHeight / 2));

                    // Clamp y to graph bounds
                    y = Math.Clamp(y, graphInnerY, graphInnerY + graphInnerHeight);

                    // Color coding based on deviation
                    SDL_Color dotColor;

                    // Get the model-specific judgment boundaries
                    var accuracyService = new AccuracyService(_resultScreenAccuracyModel);
                    accuracyService.SetHitWindow(_hitWindowMs);

                    // Calculate timing thresholds for each judgment level
                    double marvelousThreshold = FindTimingForAccuracy(accuracyService, 0.95);
                    double perfectThreshold = FindTimingForAccuracy(accuracyService, 0.80);
                    double greatThreshold = FindTimingForAccuracy(accuracyService, 0.60);
                    double goodThreshold = FindTimingForAccuracy(accuracyService, 0.40);
                    double okThreshold = FindTimingForAccuracy(accuracyService, 0.20);

                    // Determine judgment color based on accuracy
                    double absDeviation = Math.Abs(hit.Deviation);

                    if (absDeviation <= marvelousThreshold)
                        dotColor = new SDL_Color { r = 255, g = 215, b = 0, a = 255 }; // Gold - Marvelous
                    else if (absDeviation <= perfectThreshold)
                        dotColor = Color._successColor; // Green - Perfect
                    else if (absDeviation <= greatThreshold)
                        dotColor = Color._accentColor; // Orange - Great
                    else if (absDeviation <= goodThreshold)
                        dotColor = new SDL_Color { r = 180, g = 180, b = 255, a = 255 }; // Blue - Good
                    else if (absDeviation <= okThreshold)
                        dotColor = Color._mutedTextColor; // Gray - OK
                    else
                        dotColor = new SDL_Color { r = 255, g = 50, b = 50, a = 255 }; // Red - Miss

                    SDL_SetRenderDrawColor(_renderer, dotColor.r, dotColor.g, dotColor.b, dotColor.a);

                    // Draw slightly larger dots for better visibility
                    SDL_Rect pointRect = new SDL_Rect
                    {
                        x = x - 3,
                        y = y - 3,
                        w = 6,
                        h = 6
                    };

                    SDL_RenderFillRect(_renderer, ref pointRect);
                }

                // Bottom section - hit statistics and details
                int bottomY = contentY + contentHeight + panelSpacing;
                int bottomHeight = panelHeight - contentHeight - PANEL_PADDING * 5;

                // Draw bottom panel
                DrawPanel(panelX + PANEL_PADDING, bottomY, panelWidth - PANEL_PADDING * 2, bottomHeight,
                    new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._primaryColor);

                // Draw color legend at the top of the bottom panel
                int legendY = bottomY + 20;

                // Draw colored squares for the legend with even spacing
                int legendWidth = 600;
                int legendX = panelX + PANEL_PADDING + (panelWidth - PANEL_PADDING * 2 - legendWidth) / 2;
                int legendItemWidth = legendWidth / 3;

                // Early hits (red)
                SDL_SetRenderDrawColor(_renderer, 255, 50, 50, 255);
                SDL_Rect earlyRect = new SDL_Rect { x = legendX + 40, y = legendY + 5, w = 10, h = 10 };
                SDL_RenderFillRect(_renderer, ref earlyRect);
                RenderText("Early hits", legendX + 100, legendY + 10, Color._mutedTextColor, false, false);

                // Perfect hits (white)
                SDL_SetRenderDrawColor(_renderer, 255, 255, 255, 255);
                SDL_Rect perfectRect = new SDL_Rect { x = legendX + legendItemWidth + 40, y = legendY + 5, w = 10, h = 10 };
                SDL_RenderFillRect(_renderer, ref perfectRect);
                RenderText("Perfect hits", legendX + legendItemWidth + 100, legendY + 10, Color._mutedTextColor, false, false);

                // Late hits (green)
                SDL_SetRenderDrawColor(_renderer, 50, 255, 50, 255);
                SDL_Rect lateRect = new SDL_Rect { x = legendX + 2 * legendItemWidth + 40, y = legendY + 5, w = 10, h = 10 };
                SDL_RenderFillRect(_renderer, ref lateRect);
                RenderText("Late hits", legendX + 2 * legendItemWidth + 100, legendY + 10, Color._mutedTextColor, false, false);

                // Draw model description 
                string modelDescription = GetAccuracyModelDescription();
                RenderText(modelDescription, panelX + panelWidth / 2, legendY + 40, Color._mutedTextColor, false, true);

                // Draw hit statistics section
                int statsY = legendY + 70;

                // Calculate statistics
                var earlyHits = hitData.Count(h => h.Deviation < 0);
                var lateHits = hitData.Count(h => h.Deviation > 0);
                var perfectHits = hitData.Count(h => h.Deviation == 0);
                var avgDeviation = hitData.Average(h => h.Deviation);

                // Draw statistics title
                RenderText("Hit Statistics", panelX + panelWidth / 2, statsY, Color._primaryColor, false, true);

                // Layout for stats - distribute evenly
                int statsWidth = 900;
                int statsX = panelX + PANEL_PADDING + (panelWidth - PANEL_PADDING * 2 - statsWidth) / 2;
                int statsItemWidth = statsWidth / 3;
                int statsRowY = statsY + 30;

                RenderText("Early Hits", statsX + statsItemWidth / 2, statsRowY, Color._mutedTextColor, false, true);
                RenderText($"{earlyHits}", statsX + statsItemWidth / 2, statsRowY + 30, new SDL_Color { r = 255, g = 80, b = 80, a = 255 }, false, true);

                RenderText("Perfect Hits", statsX + statsItemWidth + statsItemWidth / 2, statsRowY, Color._mutedTextColor, false, true);
                RenderText($"{perfectHits}", statsX + statsItemWidth + statsItemWidth / 2, statsRowY + 30, Color._textColor, false, true);

                RenderText("Late Hits", statsX + 2 * statsItemWidth + statsItemWidth / 2, statsRowY, Color._mutedTextColor, false, true);
                RenderText($"{lateHits}", statsX + 2 * statsItemWidth + statsItemWidth / 2, statsRowY + 30, new SDL_Color { r = 80, g = 255, b = 80, a = 255 }, false, true);

                // Draw average deviation 
                var deviationText = $"Average deviation: {avgDeviation:F1}ms";
                var deviationColor = Math.Abs(avgDeviation) < 10 ? Color._successColor : Color._mutedTextColor;
                RenderText(deviationText, panelX + panelWidth / 2, statsRowY + 70, deviationColor, false, true);

                // Draw accuracy model switch instructions
                RenderText("Press LEFT/RIGHT to change accuracy model", panelX + panelWidth / 2, statsRowY + 110, Color._accentColor, false, true);
            }

            // Draw action buttons at the bottom
            int buttonY = panelY + panelHeight - 70;
            int buttonWidth = 180;
            int buttonHeight = 40;
            int buttonPadding = 20;

            int retryButtonX = _windowWidth / 2 - buttonWidth - buttonPadding;
            int menuButtonX = _windowWidth / 2 + buttonPadding;

            DrawButton("Retry [SPACE]", retryButtonX, buttonY, buttonWidth, buttonHeight,
                new SDL_Color { r = 20, g = 20, b = 40, a = 255 }, Color._textColor);

            DrawButton("Return to Menu [ENTER]", menuButtonX, buttonY, buttonWidth, buttonHeight,
                new SDL_Color { r = 20, g = 20, b = 40, a = 255 }, Color._textColor);
        }

        // Helper method to calculate judgment counts
        private static Dictionary<string, int> CalculateJudgmentCounts(List<(double NoteTime, double HitTime, double Deviation)> hitData)
        {
            var counts = new Dictionary<string, int>
            {
                { "Marvelous", 0 },
                { "Perfect", 0 },
                { "Great", 0 },
                { "Good", 0 },
                { "OK", 0 },
                { "Miss", 0 }
            };

            if (hitData.Count == 0)
                return counts;

            // Get the model-specific judgment boundaries
            var accuracyService = new AccuracyService(_resultScreenAccuracyModel);
            accuracyService.SetHitWindow(_hitWindowMs);

            // Calculate timing thresholds for each judgment level
            double marvelousThreshold = FindTimingForAccuracy(accuracyService, 0.95);
            double perfectThreshold = FindTimingForAccuracy(accuracyService, 0.80);
            double greatThreshold = FindTimingForAccuracy(accuracyService, 0.60);
            double goodThreshold = FindTimingForAccuracy(accuracyService, 0.40);
            double okThreshold = FindTimingForAccuracy(accuracyService, 0.20);

            foreach (var hit in hitData)
            {
                double absDeviation = Math.Abs(hit.Deviation);

                // Use timing thresholds directly instead of calculating accuracy again
                if (absDeviation <= marvelousThreshold)
                    counts["Marvelous"]++;
                else if (absDeviation <= perfectThreshold)
                    counts["Perfect"]++;
                else if (absDeviation <= greatThreshold)
                    counts["Great"]++;
                else if (absDeviation <= goodThreshold)
                    counts["Good"]++;
                else if (absDeviation <= okThreshold)
                    counts["OK"]++;
                else
                    counts["Miss"]++;
            }

            return counts;
        }

        // Helper method to draw judgment counts panel
        private static void DrawJudgmentCounts(int x, int y, int width, int rowHeight, Dictionary<string, int> judgmentCounts)
        {
            int labelX = x + 20;
            int countX = x + width - 40;

            // Define judgment grades with their respective colors
            var judgmentGrades = new[]
            {
                ("Marvelous", new SDL_Color { r = 255, g = 215, b = 0, a = 255 }),      // Gold
                ("Perfect", Color._successColor),                                       // Green
                ("Great", Color._accentColor),                                          // Orange
                ("Good", new SDL_Color { r = 180, g = 180, b = 255, a = 255 }),         // Blue-ish
                ("OK", Color._mutedTextColor),                                          // Gray
                ("Miss", Color._errorColor)                                             // Red
            };

            // Draw each judgment count
            for (int i = 0; i < judgmentGrades.Length; i++)
            {
                var (grade, color) = judgmentGrades[i];
                int count = judgmentCounts.ContainsKey(grade) ? judgmentCounts[grade] : 0;

                // Draw grade label with its appropriate color
                RenderText(grade, labelX, y + i * rowHeight, color, false, false);

                // Draw count with the same color
                RenderText(count.ToString(), countX, y + i * rowHeight, color, false, true);
            }
        }

        // Helper method to get accuracy model description
        private static string GetAccuracyModelDescription()
        {
            switch (_resultScreenAccuracyModel)
            {
                case AccuracyModel.Linear:
                    return "Linear: Equal accuracy weight across entire hit window";
                case AccuracyModel.Quadratic:
                    return "Quadratic: Accuracy drops with square of distance from center";
                case AccuracyModel.Stepwise:
                    return "Stepwise: Distinct accuracy zones with no gradation";
                case AccuracyModel.Exponential:
                    return "Exponential: Accuracy drops exponentially with distance";
                case AccuracyModel.osuOD8:
                    return "osu!: Based on osu! OD8 judgment windows";
                case AccuracyModel.osuOD8v1:
                    return "osu! v1: Early osu! style with OD8 windows";
                default:
                    return "Unknown accuracy model";
            }
        }

        // Draw visualization of the current accuracy model
        public static void DrawAccuracyModelVisualization(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Set up visualization properties
            SDL_SetRenderDrawBlendMode(_renderer, SDL_BlendMode.SDL_BLENDMODE_BLEND);

            // Draw judgment boundary lines based on the current model
            switch (_resultScreenAccuracyModel)
            {
                case AccuracyModel.Linear:
                    DrawLinearJudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.Quadratic:
                    DrawQuadraticJudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.Stepwise:
                    DrawStepwiseJudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.Exponential:
                    DrawExponentialJudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.osuOD8:
                    DrawOsuOD8JudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
                case AccuracyModel.osuOD8v1:
                    DrawOsuOD8V1JudgmentBoundaries(graphX, graphY, graphWidth, graphHeight, centerY);
                    break;
            }
        }

        // Draw Linear model judgment boundaries
        public static void DrawLinearJudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Linear model judgment thresholds (as percentage of hit window)
            double[] thresholds = {
                0.05,  // 95% accuracy - Marvelous threshold
                0.20,  // 80% accuracy - Perfect threshold
                0.40,  // 60% accuracy - Great threshold 
                0.60,  // 40% accuracy - Good threshold
                0.80   // 20% accuracy - OK threshold
            };

            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous
                new SDL_Color { r = 255, g = 255, b = 100, a = 100 }, // Yellow - Perfect
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - Great
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - Good
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - OK
            };

            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Calculate pixel positions for positive/negative thresholds
                int pixelOffset = (int)(thresholds[i] * _hitWindowMs * graphHeight / 2 / _hitWindowMs);

                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);

                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }

            // Draw explanation
            RenderText("Linear: Equal accuracy weight across entire hit window", graphX + graphWidth / 2, graphY + graphHeight + 70, Color._textColor, false, true);
        }

        // Draw Quadratic model judgment boundaries
        public static void DrawQuadraticJudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Quadratic model has different judgment thresholds (uses normalized = sqrt(accuracy))
            double[] thresholds = {
                0.22,  // sqrt(0.95) ≈ 0.22 - Marvelous threshold
                0.32,  // sqrt(0.90) ≈ 0.32 - Perfect threshold
                0.55,  // sqrt(0.70) ≈ 0.55 - Great threshold
                0.71,  // sqrt(0.50) ≈ 0.71 - Good threshold
                1.0    // Any hit - OK threshold
            };

            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous
                new SDL_Color { r = 255, g = 255, b = 100, a = 100 }, // Yellow - Perfect
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - Great
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - Good
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - OK
            };

            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Calculate pixel positions for positive/negative thresholds 
                int pixelOffset = (int)(thresholds[i] * graphHeight / 2);

                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);

                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }

            // Draw explanation
            RenderText("Quadratic: Accuracy decreases more rapidly as timing deviation increases", graphX + graphWidth / 2, graphY + graphHeight + 70, Color._textColor, false, true);
        }

        // Draw Stepwise model judgment boundaries 
        public static void DrawStepwiseJudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Stepwise model has exact judgment thresholds (percentage of hit window)
            double[] thresholds = {
                0.2,  // Perfect: 0-20% of hit window
                0.5,  // Great: 20-50% of hit window
                0.8,  // Good: 50-80% of hit window
                1.0   // OK: 80-100% of hit window
            };

            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous/Perfect
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - Great
                new SDL_Color { r = 255, g = 255, b = 0, a = 100 },   // Yellow - Good
                new SDL_Color { r = 255, g = 100, b = 0, a = 100 }    // Orange - OK
            };

            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Calculate pixel positions (scaled to hit window)
                int pixelOffset = (int)(thresholds[i] * _hitWindowMs * graphHeight / 2 / _hitWindowMs);

                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);

                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }

            // Draw explanation
            RenderText("Stepwise: Discrete accuracy bands with clear thresholds", graphX + graphWidth / 2, graphY + graphHeight + 70, Color._textColor, false, true);
        }

        // Draw Exponential model judgment boundaries
        public static void DrawExponentialJudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // Exponential model judgment thresholds
            // Solving for Math.Exp(-5.0 * x) = threshold
            // x = -ln(threshold) / 5.0
            double[] accuracyThresholds = { 0.90, 0.85, 0.65, 0.4, 0.0 };
            double[] thresholds = new double[accuracyThresholds.Length];

            for (int i = 0; i < accuracyThresholds.Length; i++)
            {
                // Calculate normalized position where accuracy falls below threshold
                if (accuracyThresholds[i] > 0)
                    thresholds[i] = -Math.Log(accuracyThresholds[i]) / 5.0;
                else
                    thresholds[i] = 1.0; // Maximum value
            }

            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - Marvelous
                new SDL_Color { r = 255, g = 255, b = 100, a = 100 }, // Yellow - Perfect
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - Great
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - Good
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - OK
            };

            // Draw judgment boundaries
            for (int i = 0; i < thresholds.Length; i++)
            {
                // Scale thresholds to graph height
                int pixelOffset = (int)(thresholds[i] * graphHeight / 2);

                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);

                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }

            // Draw explanation
            RenderText("Exponential: Accuracy decreases exponentially with timing deviation", graphX + graphWidth / 2, graphY + graphHeight + 70, Color._textColor, false, true);
        }

        // Draw osu! OD8 judgment boundaries
        public static void DrawOsuOD8JudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // osu! OD8 hit window thresholds in milliseconds
            double[] msThresholds = {
                13.67, // SS - 300g (OD8+)
                19.51, // 300 - Great (OD8)
                39.02, // 200 - Good (from wiki)
                58.53, // 100 - Ok (from wiki)
                78.03  // 50 - Meh (from wiki)
            };

            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - SS/300g
                new SDL_Color { r = 255, g = 220, b = 100, a = 100 }, // Yellow - 300
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - 200
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - 100
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - 50
            };

            // Draw judgment boundaries
            for (int i = 0; i < msThresholds.Length; i++)
            {
                // Scale ms threshold to graph coordinates
                int pixelOffset = (int)(msThresholds[i] * graphHeight / 2 / _hitWindowMs);

                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);

                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }

            // Draw explanation
            RenderText("osu! OD8: Based on osu! standard timing windows", graphX + graphWidth / 2, graphY + graphHeight + 70, Color._textColor, false, true);
        }

        // Draw osu! OD8 v1 judgment boundaries (early implementation)
        public static void DrawOsuOD8V1JudgmentBoundaries(int graphX, int graphY, int graphWidth, int graphHeight, int centerY)
        {
            // osu! v1 OD8 hit window thresholds in milliseconds
            double[] msThresholds = {
                16.0, // SS
                40.0, // 300
                70.0, // 200
                100.0, // 100
                130.0 // 50
            };

            SDL_Color[] colors = {
                new SDL_Color { r = 255, g = 255, b = 255, a = 100 }, // White - SS
                new SDL_Color { r = 255, g = 220, b = 100, a = 100 }, // Yellow - 300
                new SDL_Color { r = 100, g = 255, b = 100, a = 100 }, // Green - 200
                new SDL_Color { r = 100, g = 100, b = 255, a = 100 }, // Blue - 100
                new SDL_Color { r = 255, g = 100, b = 100, a = 100 }  // Red - 50
            };

            // Draw judgment boundaries
            for (int i = 0; i < msThresholds.Length; i++)
            {
                // Scale ms threshold to graph coordinates
                int pixelOffset = (int)(msThresholds[i] * graphHeight / 2 / _hitWindowMs);

                // Draw positive threshold line (late hits)
                int posY = centerY - pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, posY, graphX + graphWidth, posY);

                // Draw negative threshold line (early hits)
                int negY = centerY + pixelOffset;
                SDL_SetRenderDrawColor(_renderer, colors[i].r, colors[i].g, colors[i].b, colors[i].a);
                SDL_RenderDrawLine(_renderer, graphX, negY, graphX + graphWidth, negY);
            }

            // Draw explanation
            RenderText("osu! OD8 v1: Early osu! timing implementation", graphX + graphWidth / 2, graphY + graphHeight + 70, Color._textColor, false, true);
        }

        private static Dictionary<string, string> CalculateJudgmentTimingBoundaries()
        {
            var timings = new Dictionary<string, string>();
            var accuracyService = new AccuracyService(_resultScreenAccuracyModel);
            accuracyService.SetHitWindow(_hitWindowMs);

            // Calculate the timing values at each judgment boundary
            double marvelousMs = FindTimingForAccuracy(accuracyService, 0.95);
            double perfectMs = FindTimingForAccuracy(accuracyService, 0.80);
            double greatMs = FindTimingForAccuracy(accuracyService, 0.60);
            double goodMs = FindTimingForAccuracy(accuracyService, 0.40);
            double okMs = FindTimingForAccuracy(accuracyService, 0.20);

            // Format the values nicely
            timings["Marvelous"] = $"(±{marvelousMs:0}ms)";
            timings["Perfect"] = $"(±{perfectMs:0}ms)";
            timings["Great"] = $"(±{greatMs:0}ms)";
            timings["Good"] = $"(±{goodMs:0}ms)";
            timings["OK"] = $"(±{okMs:0}ms)";

            return timings;
        }

        private static double FindTimingForAccuracy(AccuracyService service, double targetAccuracy)
        {
            // For most models, we can calculate this directly
            double baseHitWindow = _hitWindowMs;

            // Special case handling for models with discrete steps
            if (_resultScreenAccuracyModel == AccuracyModel.Stepwise)
            {
                // Stepwise model has fixed accuracy bands
                if (targetAccuracy >= 0.95) return baseHitWindow * 0.2;  // 20% of hit window
                if (targetAccuracy >= 0.80) return baseHitWindow * 0.4;  // 40% of hit window
                if (targetAccuracy >= 0.60) return baseHitWindow * 0.6;  // 60% of hit window
                if (targetAccuracy >= 0.40) return baseHitWindow * 0.8;  // 80% of hit window
                if (targetAccuracy >= 0.20) return baseHitWindow;        // 100% of hit window
                return baseHitWindow + 1;  // Miss
            }
            else if (_resultScreenAccuracyModel == AccuracyModel.osuOD8 ||
                     _resultScreenAccuracyModel == AccuracyModel.osuOD8v1)
            {
                // osu! models have specific timing windows
                double perfect = 19.5;  // ±19.5ms for OD8
                double great = 43;      // ±43ms for OD8
                double good = 76;       // ±76ms for OD8
                double ok = 106;        // ±106ms for OD8

                if (targetAccuracy >= 0.95) return perfect * 0.7;  // Slightly tighter for marvelous
                if (targetAccuracy >= 0.80) return perfect;
                if (targetAccuracy >= 0.60) return great;
                if (targetAccuracy >= 0.40) return good;
                if (targetAccuracy >= 0.20) return ok;
                return baseHitWindow;
            }

            // For continuous models, use binary search
            double minTiming = 0;
            double maxTiming = baseHitWindow;
            double timing = baseHitWindow / 2;
            double accuracy;

            // 10 iterations of binary search should be enough precision
            for (int i = 0; i < 10; i++)
            {
                accuracy = service.CalculateAccuracy(timing);

                if (Math.Abs(accuracy - targetAccuracy) < 0.001)
                    break;

                if (accuracy > targetAccuracy)
                {
                    minTiming = timing;
                    timing = (timing + maxTiming) / 2;
                }
                else
                {
                    maxTiming = timing;
                    timing = (timing + minTiming) / 2;
                }
            }

            return timing;
        }
    }
}
