using static SDL2.SDL;

namespace C4TX.SDL.Engine.Renderer
{
    public partial class RenderEngine
    {
        // SDL related variables
        public static IntPtr _window;
        public static IntPtr _renderer;
        public static int _windowWidth = 800;
        public static int _windowHeight = 600;
        public static bool _isRunning = false;
        public static bool _isFullscreen = false;
        public static Dictionary<int, IntPtr> _textures = new Dictionary<int, IntPtr>();

        // UI Layout constants
        public const int PANEL_PADDING = 20;
        public const int PANEL_BORDER_RADIUS = 10;
        public const int ITEM_SPACING = 10;
        public const int PANEL_BORDER_SIZE = 2;

        // FPS counter tracking
        private static int _frameCount = 0;
        private static double _lastFpsUpdateTime = 0;
        private static double _currentFps = 0;
        private static double _currentFrameTime = 0;
        private static readonly double _fpsUpdateInterval = 1000; // Update FPS display every 1 second

        // For volume display
        public static double _volumeChangeTime = 0;
        public static bool _showVolumeIndicator = false;
        public static float _lastVolume = 0.7f;

        // Font and text rendering
        public static IntPtr _font;
        public static IntPtr _largeFont;
        public static Dictionary<string, IntPtr> _textTextures = new Dictionary<string, IntPtr>();

        // Dictionary to cache beatmap background textures
        public static Dictionary<string, IntPtr> _backgroundTextures = new Dictionary<string, IntPtr>();

        // Track previously loaded background texture for menu
        private static string _lastLoadedBackgroundKey = null;
        private static IntPtr _currentMenuBackgroundTexture = IntPtr.Zero;

        // Store song list items for navigation
        private static List<(int Index, int Type)> _cachedSongListItems = new List<(int Index, int Type)>();
    }
}
