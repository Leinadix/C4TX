using C4TX.SDL.KeyHandler;
using C4TX.SDL.Models;
using Clay_cs;
using static C4TX.SDL.Engine.GameEngine;
using SDL;
using static SDL.SDL3;
using System.Numerics;
using System.Runtime.InteropServices;
using C4TX.SDL.LUI;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using C4TX.SDL.Services;

namespace C4TX.SDL.Engine.Renderer
{
    public partial class RenderEngine
    {
        private static ClayStringCollection _clayString = new ClayStringCollection();

        static Vector2 deltaScroll = new Vector2(0, 0);

        public static Clay_ElementId publicId;

        public static void PushScrollDeltaX(float amount)
        {
            deltaScroll.X = amount;
        }

        public static void RenderMenu()
        {
            PerformanceMonitor.StartFrame();
            PerformanceMonitor.StartTiming("UIRender");
            
            // Periodically cleanup difficulty cache to prevent memory leaks
            if (DateTime.Now.Millisecond % 100 == 0) // Cleanup every ~100ms
            {
                CleanupCaches();
            }
            
            Clay.SetLayoutDimensions(new Clay_Dimensions(RenderEngine._windowWidth, RenderEngine._windowHeight));
            Clay.SetPointerState(mousePosition, mouseDown);
            Clay.UpdateScrollContainers(true, mouseScroll, (float)_deltaTime);

            deltaScroll = mouseScroll * 25 * 1000f * (float)_deltaTime;
            mouseScroll = new Vector2();

            var _contentBackgroundColor = new Clay_Color(30, 30, 40, 255);

            publicId = Clay.Id("OuterContainer");

            using (Clay.Element(new()
            {
                id = publicId,
                backgroundColor = new Clay_Color(43, 41, 51),
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                    padding = Clay_Padding.All(16),
                    childGap = 16,
                },
            }))
            {
                if (_availableBeatmapSets.Count > 0)
                {
                    using (Clay.Element(new()
                    {
                        id = Clay.Id("LRContainer"),
                        layout = new()
                        {
                            layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                            sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            padding = Clay_Padding.All(16),
                            childGap = 16,
                        }
                    }))
                    {
                        NDrawSongSelectionPanel();
                        NDrawSongInfoPanel();
                    }
                }
                else
                {
                    Clay.OpenTextElement("No beatmaps found. You can import your osu! Songs by directly copying the folders from osu!/Songs into appdata/local/c4tx/Songs/ !", new Clay_TextElementConfig
                    {
                        fontSize = 24,
                        textColor = new Clay_Color(255, 255, 255),
                    });
                }
                NDrawInstructionPanel();
            }


            RenderText("<insert new UI>", _windowWidth / 2, _windowHeight / 2, new SDL_Color()
            {
                r = 255,
                g = 255,
                b = 255,
                a = 255
            }, true, true);
            
            PerformanceMonitor.EndTiming("UIRender");
            PerformanceMonitor.EndFrame();
        }



       class ScrollState
        {
            public Vector2 scroll;
            public bool hover;
        }

        static Vector2 scrollVelocity;

        static Dictionary<Clay_ElementId, ScrollState> scrollStates = new ();
        
        // Old difficulty calculation variables removed - now handled by BackgroundProcessor		
		// Virtualization/cache
		static List<float> _setHeights = new();
		static float _estimatedCollapsedSetHeight = 64f; // tune after you see real sizes
		static float _estimatedMapRowHeight = 86f;       // tune after you see real sizes
		static float _listItemGap = 1f;                  // childGap used in the list panel
		public static bool _pendingScrollToSelection = false;   // wait one frame after expansion
		static float _scrollAnimationStartTime = 0f;           // when the animation started
		static float _scrollAnimationStartY = 0f;              // scroll position when animation started
		static float _scrollAnimationTargetY = 0f;             // target scroll position
		        static int _lastRenderedCount = 0;                     // debug: track rendered items
        
        // State tracking to prevent per-frame operations
        public static string _lastBackgroundRequest = "";             // track last background request
        public static double _lastDifficultyRate = 0;                 // track last difficulty rate
        
        // Cache cleanup to prevent memory leaks
        private static void CleanupCaches()
        {
            // Cleanup optimization caches
            OptimizationHelpers.CleanupPreloadingTasks();
            
            // Cleanup background processor caches
            BackgroundProcessor.CleanupCaches();
        }

        // Old difficulty calculation methods removed - now handled by BackgroundProcessor

        private static void NRenderMapItem(BeatmapInfo map, int index)
        {
            var sid = Clay.Id($"MapItem#{map.GetHashCode()}");
            using (Clay.Element(new()
            {
                id = sid,
                backgroundColor = index == _selectedDifficultyIndex ? new Clay_Color(53, 51, 61) : new Clay_Color(23, 21, 31),
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow(54f, 100f)),
                    padding = Clay_Padding.All(16),
                    childGap = 0,
                }
            }))
            {

                if (index == _selectedDifficultyIndex)
                {
                    var a = Clay.GetElementData(sid).boundingBox;
                    selectedBox = new(a.x, a.y);
                }

                Clay.OpenTextElement(map.Difficulty, new Clay_TextElementConfig
                {
                    fontSize = 20,
                    textColor = new Clay_Color(255, (Wrapper.IsHovered(sid, mousePosition) || _selectedDifficultyIndex == index ? 255.0f : 0.0f), 255),
                });

                if (Wrapper.IsHovered(sid, mousePosition) && mouseDown && !mouseDownLastframe)
                {
                    if (index == _selectedDifficultyIndex)
                    {
                        TriggerEnterGame();
                    					} else
					{
						_selectedDifficultyIndex = index;
						TriggerMapReload();
						_pendingScrollToSelection = true; // defer scroll until after layout update
						
						// Reset state tracking for new difficulty
						_lastBackgroundRequest = "";
						_lastDifficultyRate = 0;
					}
                }
            }
        }

        private static float songListUiSize = 0;
        private static Vector2 selectedBox = new(0,0);

        		private static void NRenderSetItem(BeatmapSet set, int index)
		{
			var sid = Clay.Id($"SetItem#{set.GetHashCode()}");
			using (Clay.Element(new()
			{
				id = sid,
				backgroundColor =  index == _selectedSetIndex ? new Clay_Color(63, 61, 71) : 
					Wrapper.IsHovered(sid, mousePosition) ? new Clay_Color(43, 41, 51) : new Clay_Color(23, 21, 31),
				layout = new()
				{
					layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
					sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
					padding = Clay_Padding.All(16),
					childGap = 16,
				}
			}))
			{
				Clay.OpenTextElement(set.Title, new Clay_TextElementConfig
				{
					fontSize = 20,
					textColor = new Clay_Color(255, 255, 255),
				});

				if (index != _selectedSetIndex)
				{

					if (Wrapper.IsHovered(sid, mousePosition) && mouseDown)
					{
						_selectedSetIndex = index;
						_selectedDifficultyIndex = 0;
						TriggerMapReload();
						_pendingScrollToSelection = true; // defer scroll until after expansion is measured
					}

					// No expanded content, exit early
					goto RecordHeightAndExit;
				}

				int bmc = _availableBeatmapSets![_selectedSetIndex].Beatmaps.Count;

				int startIndex = 0;

				int endIndex = bmc;

				for (int i = startIndex; i < endIndex; i++)
				{
					var map = _availableBeatmapSets[_selectedSetIndex].Beatmaps[i];
					if (map == null) continue;
					NRenderMapItem(map, i);
				}
			}

		RecordHeightAndExit:
			// capture real height for virtualization calculations
			var measured = Clay.GetElementData(sid).boundingBox.height;
			while (_setHeights.Count <= index) _setHeights.Add(0);
			_setHeights[index] = measured;
		}
        
        		private static unsafe void NDrawSongSelectionPanel()
		{
			songListUiSize = 0;
			Clay_ElementId sId = Clay.Id("SondSelectionPanel");
			if (!scrollStates.ContainsKey(sId))
			{
				scrollStates.Add(sId, new());
			}
			using (Clay.Element(new()
			{
				id = sId,
				backgroundColor = new Clay_Color(23, 21, 31),
				layout = new()
				{
					layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
					sizing = new Clay_Sizing(Clay_SizingAxis.Percent(0.5f), Clay_SizingAxis.Grow()),
					padding = Clay_Padding.All(16),
					childGap = 1,
				},
				clip = new()
				{
					vertical = true,
					horizontal = false,
					childOffset = scrollStates[Clay.Id("SondSelectionPanel")].scroll
				}
			}))
			{
				if (Wrapper.IsHovered(sId, mousePosition))
				{
					scrollVelocity += deltaScroll;
					scrollStates[sId].scroll += scrollVelocity;
					scrollStates[sId].hover = true;
				}

				if (scrollVelocity.LengthSquared() > 1)
				{
					scrollVelocity *= Math.Min(0.1f, (float)_deltaTime * 10f);
				}
				else
				{
					scrollVelocity = new Vector2(0, 0);
				}

				// Virtualization helpers
				float ViewportHeight() => Clay.GetElementData(sId).boundingBox.height;

				float EstimateSetHeight(int idx)
				{
					// Use cached height if available
					if (idx < _setHeights.Count && _setHeights[idx] > 0f) 
						return _setHeights[idx];

					// Fast path for non-selected (collapsed) sets
					if (idx != _selectedSetIndex) 
						return _estimatedCollapsedSetHeight;

					// Only calculate expanded height for selected set
					if (_availableBeatmapSets != null && idx < _availableBeatmapSets.Count)
					{
						int bmc = _availableBeatmapSets[idx].Beatmaps.Count;
						return _estimatedCollapsedSetHeight + 16f + Math.Max(0, bmc) * (_estimatedMapRowHeight + 16f);
					}
					
					return _estimatedCollapsedSetHeight;
				}

				// Total content height (approx)
				int count = _availableBeatmapSets.Count;
				if (_setHeights.Count < count) { while (_setHeights.Count < count) _setHeights.Add(0); }

				float totalHeight = 0f;
				for (int i = 0; i < count; i++)
				{
					totalHeight += EstimateSetHeight(i);
					if (i < count - 1) totalHeight += _listItemGap;
				}
				songListUiSize = totalHeight; // debug/readout if needed

				// Clamp scroll (note: our childOffset uses negative-for-up convention)
				float viewportH = ViewportHeight();
				float maxScrollY = Math.Max(0f, totalHeight - viewportH);
				float contentScrollY = -scrollStates[sId].scroll.Y;
				contentScrollY = Math.Max(0f, Math.Min(maxScrollY, contentScrollY));
				scrollStates[sId].scroll.Y = -contentScrollY;

				// Handle pending scroll to selection after expansion is measured
				if (_pendingScrollToSelection && count > 0)
				{
					// Check if the selected set has been properly measured (expanded)
					bool selectedSetMeasured = (_selectedSetIndex < _setHeights.Count && _setHeights[_selectedSetIndex] > 0f);
					
					if (selectedSetMeasured)
					{
						float targetTop = 0f;
						for (int i = 0; i < Math.Min(_selectedSetIndex, count); i++)
						{
							targetTop += EstimateSetHeight(i);
							targetTop += _listItemGap;
						}

						// Add offset for selected difficulty within the expanded set using ACTUAL measured height
						if (_selectedSetIndex < count && _selectedSetIndex < _setHeights.Count)
						{
							float actualSetHeight = _setHeights[_selectedSetIndex];
							float headerHeight = _estimatedCollapsedSetHeight;
							
							// Calculate how many difficulties are in this set
							int diffCount = _availableBeatmapSets[_selectedSetIndex].Beatmaps.Count;
							
							// If we have an actual measured height, calculate the per-difficulty height more accurately
							if (diffCount > 0)
							{
								float contentHeight = actualSetHeight - headerHeight - 32f; // subtract padding
								float actualDiffHeight = contentHeight / diffCount;
								float diffOffset = headerHeight + (_selectedDifficultyIndex * actualDiffHeight);
								targetTop += diffOffset;
							}
						}

						// Center the target position in the middle of the viewport
						float targetScroll = targetTop - (viewportH * 0.5f);
						targetScroll = Math.Max(0f, Math.Min(maxScrollY, targetScroll));

						// Initialize animation if not started yet
						if (_scrollAnimationStartTime == 0f)
						{
							_scrollAnimationStartTime = (float)_currentTime;
							_scrollAnimationStartY = contentScrollY;
							_scrollAnimationTargetY = targetScroll;
						}

						// Calculate animation progress (0 to 1 over 1 second)
						float animationDuration = 10.0f; // Always 1 second
						float elapsed = (float)_currentTime - _scrollAnimationStartTime;
						float progress = Math.Min(1.0f, elapsed / animationDuration);

						// Use smooth easing function (ease-out cubic)
						float easedProgress = 1f - (float)Math.Pow(1f - progress, 3f);

						// Interpolate between start and target
						float newScrollY = _scrollAnimationStartY + (_scrollAnimationTargetY - _scrollAnimationStartY) * easedProgress;
						newScrollY = Math.Max(0f, Math.Min(maxScrollY, newScrollY));
						scrollStates[sId].scroll.Y = -newScrollY;

						// Complete animation when progress reaches 1
						if (progress >= 1.0f)
						{
							_pendingScrollToSelection = false;
							_scrollAnimationStartTime = 0f; // Reset for next animation
						}
					}
					// If not measured yet, keep the flag and try again next frame
				}

				// Compute visible window with minimal overscan for performance
				float overscan = Math.Min(50f, _estimatedCollapsedSetHeight * 2f); // Very tight overscan - just 2 items worth
				int startIndex = 0;
				float y = 0f;
				
				// Find first visible item (binary search would be even faster for 1000+ items)
				while (startIndex < count)
				{
					float h = EstimateSetHeight(startIndex);
					if (y + h >= contentScrollY - overscan) break;
					y += h + _listItemGap;
					startIndex++;
				}

				// Find last visible item
				int endIndex = startIndex;
				float y2 = y;
				while (endIndex < count)
				{
					float h = EstimateSetHeight(endIndex);
					if (y2 > contentScrollY + viewportH + overscan) break;
					y2 += h + _listItemGap;
					endIndex++;
				}
				
				// Clamp to reasonable bounds for very large lists
				endIndex = Math.Min(endIndex, startIndex + 50); // Never render more than 50 items at once

				// Top spacer
				if (y > 0f)
				{
					using (Clay.Element(new()
					{
						layout = new()
						{
							layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
							sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Fixed(y)),
						}
					})) { }
				}

				// Render only visible sets
				_lastRenderedCount = Math.Max(0, endIndex - startIndex);
				for (int i = startIndex; i < endIndex; i++)
				{
					var set = _availableBeatmapSets[i];
					if (set == null) continue;
					NRenderSetItem(set, i);
					if (i < endIndex - 1)
					{
						// emulate list gap; using empty spacer avoids draw cost
						using (Clay.Element(new()
						{
							layout = new()
							{
								layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
								sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Fixed(_listItemGap)),
							}
						})) { }
					}
				}

				// Bottom spacer
				float bottomSpacer = Math.Max(0f, totalHeight - y2);
				if (bottomSpacer > 0f)
				{
					using (Clay.Element(new()
					{
						layout = new()
						{
							layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
							sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Fixed(bottomSpacer)),
						}
					})) { }
				}
			}
		}
        
        private unsafe static void NDrawSongInfoPanel()
        {
            var sid = Clay.Id("SongInfoPanel");
            var width = 0.0f;
            var height = 0.0f;
            using (Clay.Element(new()
            {
                id = sid,
                backgroundColor = new Clay_Color(23, 21, 31),
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Percent(0.5f), Clay_SizingAxis.Grow()),
                    padding = new Clay_Padding
                    {
                        left = 16,
                        right = 16,
                        top = 16,
                        bottom = 16
                    },
                    childGap = 16,
                }
            }))
            {
                width = Clay.GetElementData(sid).boundingBox.width;
                height = Clay.GetElementData(sid).boundingBox.height * 0.5f;

                IntPtr backgroundTexture = IntPtr.Zero;

                #region loadingShenanigans

                // First try from loaded beatmap background if available
                if (_currentBeatmap != null && !string.IsNullOrEmpty(_currentBeatmap.BackgroundFilename))
                {
                    var beatmapInfo = _availableBeatmapSets[_selectedSetIndex].Beatmaps[_selectedDifficultyIndex];
                    string beatmapDir = Path.GetDirectoryName(beatmapInfo.Path) ?? string.Empty;

                    // If we haven't loaded this background yet, or it's a different one
                    string cacheKey = $"{beatmapDir}_{_currentBeatmap.BackgroundFilename}";
                    if (_lastLoadedBackgroundKey != cacheKey || _currentMenuBackgroundTexture == IntPtr.Zero)
                    {
                        // Check if texture is ready before trying to load it
                        if (BackgroundProcessor.IsTextureReady(beatmapDir, _currentBeatmap.BackgroundFilename, width, height))
                        {
                            _currentMenuBackgroundTexture = BackgroundProcessor.GetBackgroundTexture(beatmapDir, _currentBeatmap.BackgroundFilename, width, height);
                            _lastLoadedBackgroundKey = cacheKey;
                        }
                        else
                        {
                            // Only start preloading if not already requested
                            string requestKey = $"{beatmapDir}_{_currentBeatmap.BackgroundFilename}_{width}x{height}";
                            if (_lastBackgroundRequest != requestKey)
                            {
                                BackgroundProcessor.PreloadBackgroundTexture(beatmapDir, _currentBeatmap.BackgroundFilename, width, height);
                                _lastBackgroundRequest = requestKey;
                            }
                        }
                    }

                    backgroundTexture = _currentMenuBackgroundTexture;
                }

                // Fallback to using set background if needed
                if (backgroundTexture == IntPtr.Zero && !string.IsNullOrEmpty(_availableBeatmapSets[_selectedSetIndex].BackgroundPath))
                {
                    // Try to load directly from BackgroundPath
                    string bgDir = Path.GetDirectoryName(_availableBeatmapSets[_selectedSetIndex].BackgroundPath) ?? string.Empty;
                    string bgFilename = Path.GetFileName(_availableBeatmapSets[_selectedSetIndex].BackgroundPath);

                    // Only get texture if it's ready, otherwise start preloading
                    if (BackgroundProcessor.IsTextureReady(bgDir, bgFilename, width, height))
                    {
                        backgroundTexture = BackgroundProcessor.GetBackgroundTexture(bgDir, bgFilename, width, height);
                    }
                    else
                    {
                        // Only start preloading if not already requested
                        string requestKey = $"{bgDir}_{bgFilename}_{width}x{height}";
                        if (_lastBackgroundRequest != requestKey)
                        {
                            BackgroundProcessor.PreloadBackgroundTexture(bgDir, bgFilename, width, height);
                            _lastBackgroundRequest = requestKey;
                        }
                    }
                }

                // Additional fallback - search in the song directory
                if (backgroundTexture == IntPtr.Zero && !string.IsNullOrEmpty(_availableBeatmapSets[_selectedSetIndex].DirectoryPath))
                {
                    // Try to find any image file in the song directory
                                            try
                        {
                            PerformanceMonitor.StartTiming("FileIO");
                            string[] imageExtensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
                            foreach (var ext in imageExtensions)
                            {
                                var imageFiles = OptimizationHelpers.GetFilesCached(_availableBeatmapSets[_selectedSetIndex].DirectoryPath, ext);
                                PerformanceMonitor.EndTiming("FileIO");
                            if (imageFiles.Length > 0)
                            {
                                string imageFile = Path.GetFileName(imageFiles[0]);

                                // Only get texture if it's ready, otherwise start preloading
                                if (BackgroundProcessor.IsTextureReady(_availableBeatmapSets[_selectedSetIndex].DirectoryPath, imageFile, width, height))
                                {
                                    backgroundTexture = BackgroundProcessor.GetBackgroundTexture(_availableBeatmapSets[_selectedSetIndex].DirectoryPath, imageFile, width, height);
                                    if (backgroundTexture != IntPtr.Zero)
                                        break;
                                }
                                else
                                {
                                    // Only start preloading if not already requested
                                    string requestKey = $"{_availableBeatmapSets[_selectedSetIndex].DirectoryPath}_{imageFile}_{width}x{height}";
                                    if (_lastBackgroundRequest != requestKey)
                                    {
                                        BackgroundProcessor.PreloadBackgroundTexture(_availableBeatmapSets[_selectedSetIndex].DirectoryPath, imageFile, width, height);
                                        _lastBackgroundRequest = requestKey;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error searching for image files: {ex.Message}");
                    }
                }
                #endregion

                Wrapper.UserData data = new() { w = (int)width - 32, h = (int)height -32 };
                IntPtr dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<LUI.Wrapper.UserData>());
                Marshal.StructureToPtr(data, dataPtr, false);


                using (Clay.Element(new()
                {
                    id = Clay.Id("SongInfoPanelHeader"),
                    backgroundColor = new Clay_Color(0, 0, 0),
                    layout = new()
                    {
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                        sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Percent(0.5f)),
                        padding = Clay_Padding.All(16),
                        childGap = 16,
                    },
                    image = new Clay_ImageElementConfig()
                    {
                        imageData = (void*)backgroundTexture
                    },
                    userData = (void*)dataPtr,
                    
                }))
                { }

                using (Clay.Element(new()
                {
                    id = Clay.Id("Organizer"),
                    layout = new()
                    {
                        layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                        sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                        childGap = 16
                    }
                }))
                {
                    var sicId = Clay.Id("SongInfoContent");
                    using (Clay.Element(new()
                    {
                        id = sicId,
                        backgroundColor = new Clay_Color(18, 16, 26),
                        layout = new()
                        {
                            layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                            sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            padding = Clay_Padding.All(16),
                            childGap = 16,
                        }
                    }))
                    {

                        Wrapper.DrawClayText("Song info", 40, System.Drawing.Color.Yellow, 0, 0, 10, Clay_TextAlignment.CLAY_TEXT_ALIGN_RIGHT, Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE, sicId);

                        var currentBeatmap = _availableBeatmapSets?[_selectedSetIndex].Beatmaps[_selectedDifficultyIndex] ?? null;


                        string creatorName = "";
                        double bpmValue = 0;
                        double lengthValue = 0;

                        if (GameEngine._hasCachedDetails)
                        {
                            creatorName = GameEngine._cachedCreator;
                            bpmValue = GameEngine._cachedBPM;
                            lengthValue = GameEngine._cachedLength;
                        }
                        else
                        {
                            var dbDetails = GameEngine._beatmapService.DatabaseService.GetBeatmapDetails(currentBeatmap.Id, _availableBeatmapSets?[_selectedSetIndex].Id);
                            creatorName = dbDetails.Creator;
                            bpmValue = dbDetails.BPM;
                            lengthValue = dbDetails.Length;

                            // Cache these values for future renders
                            GameEngine._cachedCreator = creatorName;
                            GameEngine._cachedBPM = bpmValue;
                            GameEngine._cachedLength = lengthValue;
                            GameEngine._hasCachedDetails = true;
                        }

                        // Fall back to in-memory values if we couldn't get data from the database
                        if (string.IsNullOrEmpty(creatorName))
                            creatorName = _availableBeatmapSets?[_selectedSetIndex].Creator;
                        if (bpmValue <= 0)
                            bpmValue = currentBeatmap.BPM;
                        if (lengthValue <= 0)
                            lengthValue = currentBeatmap.Length;

                        // Fall back to placeholders if all values are empty
                        if (string.IsNullOrEmpty(creatorName))
                            creatorName = "Unknown";
                        if (bpmValue <= 0 && _currentBeatmap != null && _currentBeatmap.BPM > 0)
                            bpmValue = _currentBeatmap.BPM;
                        if (lengthValue <= 0 && _currentBeatmap != null && _currentBeatmap.Length > 0)
                            lengthValue = _currentBeatmap.Length;


                        string fullTitle = $"{currentBeatmap.Difficulty}";
                        Wrapper.DrawClayText(
                            fullTitle,
                            10,
                            System.Drawing.Color.White,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NEWLINES,
                            sicId);

                        Wrapper.DrawClayText(
                            _availableBeatmapSets?[_selectedSetIndex].Artist + " - " + _availableBeatmapSets?[_selectedSetIndex].Title,
                            10,
                            System.Drawing.Color.White,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NEWLINES,
                            sicId);

                        var creatorText = "Mapped by " + (string.IsNullOrEmpty(creatorName) ? "Unknown" : creatorName);
                        Wrapper.DrawClayText(
                            creatorText,
                            10,
                            System.Drawing.Color.White,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NEWLINES,
                            sicId);

                        string lengthText = lengthValue > 0 ? MillisToTime(lengthValue / GameEngine._currentRate).ToString() : "--:--";
                        Wrapper.DrawClayText(
                            lengthText,
                            10,
                            System.Drawing.Color.White,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NEWLINES,
                            sicId);

                        string bpmText = bpmValue > 0 ? (bpmValue * GameEngine._currentRate).ToString("F2") + " BPM" : "--- BPM";
                        Wrapper.DrawClayText(
                            bpmText,
                            10,
                            System.Drawing.Color.White,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NEWLINES,
                            sicId); 

                        // Get cached difficulty rating (no per-frame calculation!)
                        double difficultyRating = 0;
                        if (_currentBeatmap != null)
                        {
                            string mapHash = _currentBeatmap.MapHash ?? _currentBeatmap.Id;
                            if (BackgroundProcessor.IsDifficultyReady(mapHash, GameEngine._currentRate))
                            {
                                difficultyRating = BackgroundProcessor.GetDifficultyRating(
                                    _currentBeatmap, 
                                    GameEngine._currentRate, 
                                    mapHash
                                );
                            }
                            else
                            {
                                // Only start calculation if rate changed or not already requested
                                if (_lastDifficultyRate != GameEngine._currentRate)
                                {
                                    BackgroundProcessor.PreloadDifficultyCalculation(_currentBeatmap, GameEngine._currentRate, mapHash);
                                    _lastDifficultyRate = GameEngine._currentRate;
                                }
                            }
                        }
                        string diffText = "No difficulty rating";
                        
                        if (difficultyRating > 0)
                        {
                            // Display the difficulty rating with rate applied
                            diffText = $"{difficultyRating:F2} *";
                        }

                        var ratingColor = GetRatingColor(difficultyRating);

                        Wrapper.DrawClayText(
                            diffText,
                            10,
                            ratingColor,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
                            sicId);

                    }

                    NDrawScoresPanel();
                }
            }
        }

        public static System.Drawing.Color Lerp(System.Drawing.Color a, System.Drawing.Color b, double t)
        {
            t = Math.Clamp(t, 0, 1);
            int A = (int)Math.Round(a.A + (b.A - a.A) * t);
            int R = (int)Math.Round(a.R + (b.R - a.R) * t);
            int G = (int)Math.Round(a.G + (b.G - a.G) * t);
            int B = (int)Math.Round(a.B + (b.B - a.B) * t);
            return System.Drawing.Color.FromArgb(A, R, G, B);
        }

        public static System.Drawing.Color GetRatingColor(double rating)
        {
            rating = Math.Clamp(rating, 0, 10);

            var stops = new[]
            {
            System.Drawing.Color.White,
            System.Drawing.Color.FromArgb(255,  0,255,255),
            System.Drawing.Color.FromArgb(255,  0,  0,255),
            System.Drawing.Color.FromArgb(255,  0,255,  0),
            System.Drawing.Color.FromArgb(255,255,255,  0),
            System.Drawing.Color.FromArgb(255,255,127,  0),
            System.Drawing.Color.FromArgb(255,255,  0,  0),
            System.Drawing.Color.FromArgb(255, 75,  0,130),
            System.Drawing.Color.FromArgb(255,148,  0,211),
            System.Drawing.Color.FromArgb(255,255,  0,255),
            System.Drawing.Color.Black,
        };

            int lo = (int)Math.Floor(rating);
            if (lo >= 10) return stops[10];
            double t = rating - lo;
            return Lerp(stops[lo], stops[lo + 1], t);
        }

        private static void NDrawScoreSelectionItem(ScoreData score, bool isSelected, int index)
        {
            var sicId = Clay.Id($"ScoreItem#{index}");
            using (Clay.Element(new()
            {
                id = sicId,
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                    sizing = new()
                    {
                        width = Clay_SizingAxis.Grow(),
                        height = Clay_SizingAxis.Fixed(64)
                    }
                },
                backgroundColor = new()
                {
                    a = 255,
                    r = 0,
                    g = 50,
                    b = 50,
                }
            }))
            {
                var bgC = new Clay_Color()
                {
                    a = 50,
                    r = 255,
                    g = 255,
                    b = 255,
                };
                if (isSelected)
                {
                    bgC.a = 255;
                }

                // Choose row color
                System.Drawing.Color rowColor;
                if (index == 0)
                    rowColor = System.Drawing.Color.Gold;
                else if (index == 1)
                    rowColor = System.Drawing.Color.Silver;
                else if (index == 2)
                    rowColor = System.Drawing.Color.Brown;
                else
                    rowColor = System.Drawing.Color.White;

                var sr = score.starRating;

                // Format data
                string date = score.DatePlayed.ToString("yyyy-MM-dd:HH:mm:ss");
                string scoreText = (100 * sr * 4 * Math.Max(0, score.Accuracy - 0.8)).ToString("F4");
                string accuracy = score.Accuracy.ToString("P2");
                string combo = $"{score.MaxCombo}x";
                string rate = $"{score.PlaybackRate}x";

                // Draw row
                using (Clay.Element(new()
                {
                    layout = new()
                    {
                        sizing = new()
                        {
                            width = Clay_SizingAxis.Grow(),
                            height = Clay_SizingAxis.Grow()
                        }
                    },
                    backgroundColor = bgC
                }))
                {
                    //  Username                |   100.00%
                    //                          |
                    //                          |    combo
                    //  1234-12-12-12-12-12     |   12345pp
                    //

                    using (Clay.Element(new()
                    {
                        layout = new()
                        {
                            layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                            sizing = new()
                            {
                                width = Clay_SizingAxis.Grow(),
                                height = Clay_SizingAxis.Grow()
                            }
                        }
                    }))
                    {
                        using (Clay.Element(new()
                        {
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                                sizing = new()
                                {
                                    width = Clay_SizingAxis.Percent(0.666666f),
                                    height = Clay_SizingAxis.Grow()
                                }
                            },
                        }))
                        {
                            using (Clay.Element(new()
                            {
                                layout = new()
                                {
                                    layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                    sizing = new()
                                    {
                                        width = Clay_SizingAxis.Grow(),
                                        height = Clay_SizingAxis.Grow()
                                    }
                                }
                            }))
                            {
                                Clay.OpenTextElement(score.Username, new()
                                {
                                    fontId = 0,
                                    fontSize = 10,
                                });
                            }

                            using (Clay.Element(new()
                            {
                                layout = new()
                                {
                                    layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                    sizing = new()
                                    {
                                        width = Clay_SizingAxis.Grow(),
                                        height = Clay_SizingAxis.Grow()
                                    }
                                }
                            }))
                            {
                                Clay.OpenTextElement(scoreText, new()
                                {
                                    fontId = 0,
                                    fontSize = 10,
                                });
                            }
                            using (Clay.Element(new()
                            {
                                layout = new()
                                {
                                    layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                    sizing = new()
                                    {
                                        width = Clay_SizingAxis.Grow(),
                                        height = Clay_SizingAxis.Grow(16)
                                    }
                                }
                            }))
                            {
                                Clay.OpenTextElement(date, new()
                                {
                                    fontId = 0,
                                    fontSize = 10,
                                });
                            }
                        }
                        using (Clay.Element(new()
                        {
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                                sizing = new()
                                {
                                    width = Clay_SizingAxis.Grow(),
                                    height = Clay_SizingAxis.Grow()
                                }
                            }
                        }))
                        {
                            using (Clay.Element(new()
                            {
                                layout = new()
                                {
                                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                                    sizing = new()
                                    {
                                        width = Clay_SizingAxis.Grow(),
                                        height = Clay_SizingAxis.Grow()
                                    }
                                }
                            }))
                            {
                                using (Clay.Element(new()
                                {
                                    layout = new()
                                    {
                                        sizing = new()
                                        {
                                            width = Clay_SizingAxis.Grow(),
                                            height = Clay_SizingAxis.Grow()
                                        }
                                    }
                                }))
                                {
                                    Clay.OpenTextElement(accuracy, new()
                                    {
                                        fontId = 0,
                                        fontSize = 10,
                                    });
                                }
                                    
                                using (Clay.Element(new()
                                {
                                    layout = new()
                                    {
                                        sizing = new()
                                        {
                                            width = Clay_SizingAxis.Grow(),
                                            height = Clay_SizingAxis.Grow()
                                        }
                                    }
                                }))
                                {
                                    Clay.OpenTextElement(rate, new()
                                    {
                                        fontId = 0,
                                        fontSize = 10,
                                    });
                                }
                                    
                            }
                            using (Clay.Element(new()
                            {
                                layout = new()
                                {
                                    layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                    sizing = new()
                                    {
                                        width = Clay_SizingAxis.Grow(),
                                        height = Clay_SizingAxis.Grow(16)
                                    }
                                }
                            }))
                            {
                                Clay.OpenTextElement(combo, new()
                                {
                                    fontId = 0,
                                    fontSize = 10,
                                });
                            }
                        }
                    }

                }
            }
        }

        private static void NDrawScoresPanel()
        {
            // TODO: Scrolling
            var sicId = Clay.Id("ScoresPanel");
            using (Clay.Element(new()
            {
                id = sicId,
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                    padding = Clay_Padding.All(16),
                    childGap = 16,
                },
            }))
            {
                Wrapper.DrawClayText(
                    "Previous Scores",
                    40,
                    System.Drawing.Color.White,
                    0,
                    0,
                    10,
                    Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
                    Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
                    sicId);

                if (string.IsNullOrWhiteSpace(_username))
                {
                    Wrapper.DrawClayText(
                    "Set username to view scores",
                    40,
                    System.Drawing.Color.Red,
                    0,
                    0,
                    10,
                    Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
                    Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
                    sicId);

                    return;
                }

                if (_availableBeatmapSets == null || _selectedSetIndex >= _availableBeatmapSets.Count)
                    return;

                var currentMapset = _availableBeatmapSets[_selectedSetIndex];

                if (_selectedDifficultyIndex >= currentMapset.Beatmaps.Count)
                    return;

                var currentBeatmap = currentMapset.Beatmaps[_selectedDifficultyIndex];

                try
                {
                    string mapHash = string.Empty;

                    if (_currentBeatmap != null && !string.IsNullOrEmpty(_currentBeatmap.MapHash))
                    {
                        mapHash = _currentBeatmap.MapHash;
                    }
                    else
                    {
                        mapHash = _beatmapService.CalculateBeatmapHash(currentBeatmap.Path);
                    }

                    if (string.IsNullOrEmpty(mapHash))
                    {
                        Wrapper.DrawClayText(
                            "Cannot load scores: Map hash unavailable",
                            40,
                            System.Drawing.Color.Red,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
                            sicId);
                        return;
                    }

                    // Get scores for this beatmap using the hash
                    if (mapHash != _cachedScoreMapHash || !_hasCheckedCurrentHash)
                    {
                        // Cache miss - fetch scores from service
                        Console.WriteLine($"[DEBUG] Cache miss - fetching scores for map hash: {mapHash}");
                        _cachedScores = _scoreService.GetBeatmapScoresByHash(_username, mapHash);
                        // Sort scores by cached difficulty (or base rating) to avoid blocking UI
                        _cachedScores = _cachedScores.OrderByDescending(s => 
                        {
                            // Use cached difficulty if available, otherwise use base rating
                            var currentBeatmapInfo = GameEngine._availableBeatmapSets[_selectedSetIndex].Beatmaps[_selectedDifficultyIndex];
                            if (currentBeatmapInfo?.CachedDifficultyRating.HasValue == true)
                            {
                                return currentBeatmapInfo.CachedDifficultyRating.Value * s.Accuracy;
                            }
                            return s.Accuracy; // Fallback to just accuracy if no difficulty rating
                        }).ToList();
                        _cachedScoreMapHash = mapHash;
                        _hasLoggedCacheHit = false;
                        _hasCheckedCurrentHash = true;
                    }
                    else if (!_hasLoggedCacheHit)
                    {
                        Console.WriteLine($"[DEBUG] Using cached scores for map hash: {mapHash} (found {_cachedScores.Count})");
                        _hasLoggedCacheHit = true;
                    }

                    var scores = _cachedScores.OrderByDescending(obj => obj.starRating * 4 * Math.Max(0, obj.Accuracy - 0.8)).ToList();


                    if (scores.Count == 0)
                    {
                        Wrapper.DrawClayText(
                            "No replays found!",
                            40,
                            System.Drawing.Color.Yellow,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
                            sicId);
                        return;
                    }

                    for (int i = 0; i < _cachedScores.Count; i++)
                    {
                        var score = scores[i];
                        // Determine if this row is selected in the scores section
                        bool isScoreSelected = _isScoreSectionFocused && i == _selectedScoreIndex;

                        NDrawScoreSelectionItem(score, isScoreSelected, i);
                    }
                }
                catch (Exception ex)
                {
                    Wrapper.DrawClayText(
                            $"Error: {ex.Message}",
                            40,
                            System.Drawing.Color.Yellow,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
                            sicId);
                }
            }
        }

        private static unsafe void NDrawInstructionPanel()
        {
            using (Clay.Element(new()
            {
                id = Clay.Id("InstructionSeperator"),
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                }
            }))
            {
                using (Clay.Element(new()
                {
                    id = Clay.Id("InstructionSeperatorTop"),
                    layout = new()
                    {
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                        sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                    }
                }))
                {
                    using (Clay.Element(new()
                    {
                        id = Clay.Id("InstructionFooter"),
                        backgroundColor = new Clay_Color(23, 21, 31),
                        layout = new()
                        {
                            layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                            sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Fixed(100)),
                            padding = Clay_Padding.All(16),
                            childGap = 16,
                        }
                    }))
                    {
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("↑/↓: Change Set", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                                textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
                            });
                        }
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("←/→: Change difficulty", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                            });
                        }

                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("Enter: Play", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                            });
                        }
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("Tab: Switch Menu", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                            });
                        }

                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("P: Switch Profile", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                                textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
                            });
                        }
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("1/2: Change Rate", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                                textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
                            });
                        }
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("S: Settings", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                                textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
                            });
                        }
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("F5: Reload Maps", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                                textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
                            });
                        }
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement($"U: Check for updates", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                            });
                        }

                    }
                }
                using (Clay.Element(new()
                {
                    id = Clay.Id("InstructionSeperatorBot"),
                    layout = new()
                    {
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                        sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                    }
                }))
                {

                    using (Clay.Element(new()
                    {

                    }))
                    {
                        Clay.OpenTextElement($"Rendered: {_lastRenderedCount}/{_availableBeatmapSets?.Count ?? 0} sets | {PerformanceMonitor.GetPerformanceSummary()} | {BackgroundProcessor.GetCacheStats()}", new Clay_TextElementConfig
                        {
                            fontSize = 20,
                            textColor = new Clay_Color(255, 255, 255),
                        });
                    }

                    string text = $"v{GameEngine.Version}";
                    int size, width, height;
                    var bytes = StringToUtf8(text, out size);

                    SDL3_ttf.TTF_GetStringSize((TTF_Font*)_font, (byte*)&bytes, (nuint)size, &width, &height);

                    using (Clay.Element(new()
                    {
                        layout = new()
                        {
                            layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                            sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            padding = Clay_Padding.Hor((ushort)((ushort)(RenderEngine._windowWidth / 2) - width)),
                        }
                    }))
                    {
                        Clay.OpenTextElement(text, new()
                        {
                            fontId = 0,
                            fontSize = 32
                        });
                    }
                        
                }

            }

            
        }

        private static void NDrawProfilePanel()
        {
            const int panelWidth = 300;
            const int panelHeight = 300;
            int panelX = _windowWidth - panelWidth - PANEL_PADDING;
            int panelY = PANEL_PADDING;

            DrawPanel(panelX, panelY, panelWidth, panelHeight, Color._panelBgColor, Color._accentColor);

            // Draw header
            SDL_Color titleColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
            SDL_Color subtitleColor = new SDL_Color() { r = 200, g = 200, b = 255, a = 255 };
            RenderText("C4TX", panelX + panelWidth / 2, panelY + 50, titleColor, true, true);
            RenderText("A 4k Rhythm Game", panelX + panelWidth / 2, panelY + 80, subtitleColor, false, true);

            // Draw current profile
            if (!string.IsNullOrWhiteSpace(_username))
            {
                // Show current profile
                SDL_Color profileColor = new SDL_Color() { r = 150, g = 200, b = 255, a = 255 };
                RenderText("Current Profile:", panelX + panelWidth / 2, panelY + 130, Color._textColor, false, true);
                RenderText(_username, panelX + panelWidth / 2, panelY + 155, profileColor, false, true);
                RenderText("Press P to switch profile", panelX + panelWidth / 2, panelY + 180, Color._mutedTextColor, false, true);
            }
            else
            {
                // Prompt to select a profile
                SDL_Color warningColor = new SDL_Color() { r = 255, g = 150, b = 150, a = 255 };
                RenderText("No profile selected", panelX + panelWidth / 2, panelY + 130, warningColor, false, true);
                RenderText("Press P to select a profile", panelX + panelWidth / 2, panelY + 155, Color._textColor, false, true);
            }

            // Draw menu instructions
            RenderText("Press S for Settings", panelX + panelWidth / 2, panelY + 210, Color._mutedTextColor, false, true);
            RenderText("Press F11 for Fullscreen", panelX + panelWidth / 2, panelY + 235, Color._mutedTextColor, false, true);
        }

        #region old

        public static unsafe void DrawHeader(string title, string subtitle)
        {
            // Draw game logo/title
            RenderText(title, _windowWidth / 2, 50, Color._accentColor, true, true);

            // Draw subtitle
            RenderText(subtitle, _windowWidth / 2, 90, Color._mutedTextColor, false, true);

            // Draw a horizontal separator line
            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._primaryColor.r, Color._primaryColor.g, Color._primaryColor.b, 150);
            SDL_FRect separatorLine = new SDL_FRect
            {
                x = _windowWidth / 4,
                y = 110,
                w = _windowWidth / 2,
                h = 2
            };
            SDL_RenderFillRect((SDL_Renderer*)_renderer, & separatorLine);
        }
        public static unsafe void DrawSearchPanel(int x, int y, int width, int height)
        {
            // Title
            RenderText("Song Search", x + width / 2, y, Color._primaryColor, true, true);
            
            // Draw panel for search and results
            DrawPanel(x, y + 20, width, height - 20, new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._panelBgColor, 0);
            
            // Draw search input field
            int inputFieldY = y + 40;
            SDL_Color inputBgColor = new SDL_Color { r = 20, g = 20, b = 40, a = 255 };
            SDL_Color inputBorderColor = GameEngine._isSearchInputFocused
                ? new SDL_Color { r = 100, g = 200, b = 255, a = 255 }
                : new SDL_Color { r = 100, g = 100, b = 255, a = 255 };
                
            DrawPanel(x + 20, inputFieldY, width - 40, 40, inputBgColor, inputBorderColor);
            
            // Draw search query with cursor if focused
            string displayQuery = GameEngine._isSearchInputFocused ? GameEngine._searchQuery + "_" : GameEngine._searchQuery;
            if (string.IsNullOrEmpty(displayQuery))
            {
                displayQuery = GameEngine._isSearchInputFocused ? "_" : "Search...";
            }
            
            RenderText(displayQuery, x + 40, inputFieldY + 20, Color._textColor, false, false);
            
            // Draw help text
            SDL_Color helpColor = new SDL_Color { r = 180, g = 180, b = 180, a = 255 };
            RenderText("Press Enter to search, Escape to exit", x + width / 2, inputFieldY + 50, helpColor, false, true);
            
            // Draw results if search has been performed
            if (GameEngine._showSearchResults && GameEngine._searchResults != null)
            {
                // Draw results header
                int resultsY = inputFieldY + 70;
                int resultsCount = 0;
                
                // Count total beatmaps in results
                foreach (var set in GameEngine._searchResults)
                {
                    if (set.Beatmaps != null)
                    {
                        resultsCount += set.Beatmaps.Count;
                    }
                }
                
                if (resultsCount > 0)
                {
                    RenderText($"Found {resultsCount} beatmaps", x + width / 2, resultsY, Color._primaryColor, false, true);
                    
                    // Constants for item heights and padding
                    int itemHeight = 50; // Height for each beatmap
                    int headerHeight = 40; // Height for mapset headers
                    
                    // Calculate the absolute boundaries of the visible area
                    int viewAreaTop = resultsY + 50; 
                    int viewAreaHeight = height - (viewAreaTop - y) - 10; // Height of the visible area
                    int viewAreaBottom = viewAreaTop + viewAreaHeight; // Bottom boundary
                    
                    // Calculate the flat index positions with headers
                    List<(int SetIndex, int DiffIndex, int StartY, int Height, bool IsHeader)> itemPositions = new List<(int, int, int, int, bool)>();
                    int totalContentHeight = 0;
                    
                    // Create a flat representation with headers for each set
                    for (int setIndex = 0; setIndex < GameEngine._searchResults.Count; setIndex++)
                    {
                        var set = GameEngine._searchResults[setIndex];
                        
                        if (set.Beatmaps == null || set.Beatmaps.Count == 0)
                            continue;
                            
                        // Add a header for this set
                        itemPositions.Add((setIndex, -1, totalContentHeight, headerHeight, true));
                        totalContentHeight += headerHeight;
                        
                        // Add all beatmaps in this set
                        for (int diffIndex = 0; diffIndex < set.Beatmaps.Count; diffIndex++)
                        {
                            itemPositions.Add((setIndex, diffIndex, totalContentHeight, itemHeight, false));
                            totalContentHeight += itemHeight;
                        }
                    }
                    
                    // Calculate max possible scroll
                    int maxScroll = Math.Max(0, totalContentHeight - viewAreaHeight);
                    
                    // Find the currently selected beatmap in flat representation
                    int selectedItemY = 0;
                    
                    // Get the set and diff index from flat index
                    var setDiffPosition = SearchKeyhandler.GetSetAndDiffFromFlatIndex(GameEngine._selectedSetIndex);
                    
                    if (setDiffPosition.SetIndex >= 0 && setDiffPosition.DiffIndex >= 0)
                    {
                        // Find the corresponding position in our itemPositions list
                        for (int i = 0; i < itemPositions.Count; i++)
                        {
                            var item = itemPositions[i];
                            if (!item.IsHeader && item.SetIndex == setDiffPosition.SetIndex && item.DiffIndex == setDiffPosition.DiffIndex)
                            {
                                selectedItemY = item.StartY;
                                break;
                            }
                        }
                    }
                    
                    // Center the selected item in the view
                    int targetScrollPos = selectedItemY + (itemHeight / 2) - (viewAreaHeight / 2);
                    targetScrollPos = Math.Max(0, Math.Min(maxScroll, targetScrollPos));
                    
                    // Final scroll offset
                    int scrollOffset = targetScrollPos;
                    
                    // Draw each item (header or beatmap)
                    for (int i = 0; i < itemPositions.Count; i++)
                    {
                        var item = itemPositions[i];
                        
                        // Calculate the actual screen Y position after applying scroll
                        int screenY = viewAreaTop + item.StartY - scrollOffset;
                        
                        // Skip items completely outside the view area
                        if (screenY + item.Height < viewAreaTop - 50 || screenY > viewAreaBottom + 50)
                        {
                            continue;
                        }
                        
                        if (item.IsHeader)
                        {
                            // Draw header
                            var setInfo = GameEngine._searchResults[item.SetIndex];
                            string headerText = $"{setInfo.Artist} - {setInfo.Title}";
                            
                            // Draw header background
                            SDL_Color headerBgColor = new SDL_Color { r = 40, g = 40, b = 70, a = 255 };
                            SDL_Color headerTextColor = new SDL_Color { r = 220, g = 220, b = 255, a = 255 };
                            
                            // Calculate proper panel height for better alignment
                            int actualHeaderHeight = headerHeight - 5;
                            DrawPanel(x + 5, screenY, width - 10, actualHeaderHeight, headerBgColor, headerBgColor, 0);
                            
                            // Truncate header text if too long
                            if (headerText.Length > 40) headerText = headerText.Substring(0, 38) + "...";
                            
                            // Draw header text
                            RenderText(headerText, x + 20, screenY + actualHeaderHeight / 2, headerTextColor, false, false);
                        }
                        else
                        {
                            // Draw beatmap item
                            var set = GameEngine._searchResults[item.SetIndex];
                            var beatmap = set.Beatmaps[item.DiffIndex];
                            
                            // Check if this is the currently selected beatmap
                            bool isSelected = (setDiffPosition.SetIndex == item.SetIndex && setDiffPosition.DiffIndex == item.DiffIndex);
                            
                            // Draw beatmap background
                            SDL_Color bgColor = isSelected ? Color._primaryColor : Color._panelBgColor;
                            SDL_Color textColor = isSelected ? Color._textColor : Color._mutedTextColor;
                            
                            // Calculate proper panel height for better alignment
                            int actualItemHeight = itemHeight - 5;
                            DrawPanel(x + 20, screenY, width - 25, actualItemHeight, bgColor, isSelected ? Color._accentColor : Color._panelBgColor, isSelected ? 2 : 0);
                            
                            // Create display text for difficulty
                            string difficultyText = $"[{beatmap.Difficulty}]";
                            if (difficultyText.Length > 15) difficultyText = difficultyText.Substring(0, 13) + "...]";
                            
                            // Render difficulty name
                            RenderText(difficultyText, x + 35, screenY + actualItemHeight / 2, textColor, false, false);
                            
                            // Show star rating if available
                            if (beatmap.CachedDifficultyRating.HasValue && beatmap.CachedDifficultyRating.Value > 0)
                            {
                                string starRatingText = $"{beatmap.CachedDifficultyRating.Value:F2}★";
                                RenderText(starRatingText, x + width - 50, screenY + actualItemHeight / 2, textColor, false, true);
                            }
                        }
                    }
                }
                else
                {
                    // No results found
                    RenderText("No matching beatmaps found", x + width / 2, resultsY + 40, Color._errorColor, false, true);
                }
            }
        }

        #endregion
    }
}
