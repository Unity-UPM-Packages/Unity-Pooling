using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace com.thelegends.unity.pooling.Editor
{
    /// <summary>
    /// Advanced editor tool for monitoring and analyzing the PoolManager and all managed pools at runtime.
    /// Provides real-time statistics, visualization, and optimization recommendations.
    /// </summary>
    public class PoolManagerEditor : EditorWindow
    {
        #region Constants and Static Fields
        
        // Window title and dimensions
        private const string WINDOW_TITLE = "Object Pooling Dashboard";
        private const float MIN_WINDOW_WIDTH = 800f;
        private const float MIN_WINDOW_HEIGHT = 500f;
        
        // UI Constants
        private const float HEADER_HEIGHT = 40f;
        private const float PADDING = 10f;
        private const float TAB_HEIGHT = 25f;
        private const float TOOLBAR_HEIGHT = 20f;
        private const float SIDEBAR_WIDTH = 250f;
        
        // Refresh rate for data updates (seconds)
        private const float DEFAULT_REFRESH_RATE = 0.5f;
        
        // Data sampling
        private const int MAX_HISTORY_SAMPLES = 300; // 5 minutes @ 0.5s update
        
        // Tabs for the main window
        private static readonly string[] s_tabNames = {
            "Dashboard", "Pool Details", "Performance Analysis", "Settings"
        };
        
        // Colors for UI elements
        private static readonly Color s_headerColor = new Color(0.2f, 0.2f, 0.2f);
        private static readonly Color s_activeColor = new Color(0.3f, 0.7f, 0.3f);
        private static readonly Color s_inactiveColor = new Color(0.7f, 0.3f, 0.3f);
        private static readonly Color s_warningColor = new Color(0.9f, 0.6f, 0.1f);
        private static readonly Color s_instantiateColor = new Color(0.2f, 0.4f, 0.9f);
        
        // Get the Editor window through menu or shortcut
        [MenuItem("Window/Object Pooling/Pool Manager Dashboard", false, 2000)]
        public static void ShowWindow()
        {
            var window = GetWindow<PoolManagerEditor>(false, WINDOW_TITLE);
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
        }
        
        #endregion
        
        #region Instance Fields and Properties
        
        // Current tab selection
        private int _selectedTab = 0;
        
        // Refresh rate control
        private float _refreshRate = DEFAULT_REFRESH_RATE;
        private float _lastRefreshTime = 0f;
        private bool _autoRefresh = true;
        
        // Scroll positions for various views
        private Vector2 _dashboardScrollPosition;
        private Vector2 _poolDetailsScrollPosition;
        private Vector2 _analysisScrollPosition;
        private Vector2 _settingsScrollPosition;
        
        // Selected pool for details view
        private string _selectedPoolKey;
        
        // State tracking
        private bool _isPlaying = false;
        private bool _isPoolManagerInitialized = false;
        
        // Pool data caching
        private Dictionary<string, PoolStatistics> _poolStats = new Dictionary<string, PoolStatistics>();
        
        // Dashboard data structures
        private class PoolTimeSeriesData
        {
            public List<int> ActiveCounts = new List<int>();
            public List<int> InactiveCounts = new List<int>();
            public List<float> Timestamps = new List<float>();
            public List<int> InstantiateEvents = new List<int>(); // Track instantiate events over time
        }
        
        // Global stats over time
        private Dictionary<string, PoolTimeSeriesData> _poolTimeSeriesData = new Dictionary<string, PoolTimeSeriesData>();
        private List<int> _globalActiveCounts = new List<int>();
        private List<int> _globalInactiveCounts = new List<int>();
        private List<float> _globalTimestamps = new List<float>();
        private List<int> _globalInstantiateEvents = new List<int>();
        
        // Dashboard summary stats
        private int _totalActiveCount = 0;
        private int _totalInactiveCount = 0;
        private int _totalInstantiateCount = 0;
        private int _totalMissCount = 0;
        private float _globalEfficiency = 0f;
        private float _systemMemoryUsage = 0f; // In MB
        
        // Reflection cached items for accessing PoolManager at runtime
        private Type _poolManagerType;
        private object _poolManagerInstance;
        private MethodInfo _clearPoolMethod;
        private MethodInfo _trimExcessPoolsMethod;
        private PropertyInfo _poolsProperty;
        private PropertyInfo _isDebugLogEnabledProperty;
        private FieldInfo _instantiateCountField;
        private Dictionary<string, int> _previousInstantiateCount = new Dictionary<string, int>();
        
        #endregion
        
        #region Unity Event Functions
        
        private void OnEnable()
        {
            // Set initial state
            _isPlaying = EditorApplication.isPlaying;
            
            // Subscribe to editor events
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
        }
        
        private void OnDisable()
        {
            // Unsubscribe from editor events to prevent memory leaks
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void OnGUI()
        {
            if (!_isPlaying)
            {
                DrawNotPlayingMessage();
                return;
            }
            
            // Check if PoolManager is initialized
            CheckPoolManagerInitialization();
            
            if (!_isPoolManagerInitialized)
            {
                DrawNotInitializedMessage();
                return;
            }
            
            // Draw the main window content
            DrawHeader();
            DrawTabs();
            
            // Draw tab content based on selection
            switch (_selectedTab)
            {
                case 0:
                    DrawDashboard();
                    break;
                case 1:
                    DrawPoolDetails();
                    break;
                case 2:
                    DrawPerformanceAnalysis();
                    break;
                case 3:
                    DrawSettings();
                    break;
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            _isPlaying = EditorApplication.isPlaying;
            
            if (_isPlaying)
            {
                // Reset state when entering play mode
                _isPoolManagerInitialized = false;
                _selectedPoolKey = null;
            }
            else
            {
                // Clear data when exiting play mode to prevent memory leaks
                ClearAllData();
            }
            
            // Force repaint when play mode changes
            Repaint();
        }
        
        private void OnEditorUpdate()
        {
            if (!_isPlaying || !_autoRefresh)
                return;
                
            // Check if it's time to refresh data
            if (EditorApplication.timeSinceStartup >= _lastRefreshTime + _refreshRate)
            {
                // Update data from PoolManager
                RefreshPoolData();
                
                // Update the last refresh time
                _lastRefreshTime = (float)EditorApplication.timeSinceStartup;
                
                // Repaint the window to show updated data
                Repaint();
            }
        }
        
        #endregion
        
        #region UI Drawing Methods
        
        private void DrawNotPlayingMessage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.FlexibleSpace();
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Object Pool Dashboard is only available in Play Mode", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Enter Play Mode", GUILayout.Width(150), GUILayout.Height(30)))
            {
                EditorApplication.isPlaying = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawNotInitializedMessage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.FlexibleSpace();
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("PoolManager is not initialized or not found in the scene", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Make sure to create at least one pool before using this tool");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(TOOLBAR_HEIGHT));
            
            GUILayout.Label(WINDOW_TITLE, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            // Add refresh button
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshPoolData();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(TAB_HEIGHT));
            
            _selectedTab = GUILayout.Toolbar(_selectedTab, s_tabNames, EditorStyles.toolbarButton);
            
            EditorGUILayout.EndHorizontal();
        }
        
        // Placeholder methods for tab content - will be implemented in later parts
        private void DrawDashboard() 
        {
            EditorGUILayout.BeginVertical();
            
            // Begin scrollable area
            _dashboardScrollPosition = EditorGUILayout.BeginScrollView(_dashboardScrollPosition);
            
            // Draw header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Object Pooling Dashboard", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(PADDING);
            
            // Draw summary statistics
            DrawDashboardSummary();
            
            GUILayout.Space(PADDING);
            
            // Draw real-time graph
            DrawRealTimeGraph();
            
            GUILayout.Space(PADDING);
            
            // Draw pool list with quick stats
            DrawPoolQuickStats();
            
            GUILayout.Space(PADDING);
            
            // Draw instantiate events timeline
            DrawInstantiateEventsTimeline();
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPoolDetails() 
        {
            EditorGUILayout.BeginVertical();
            
            // Begin scrollable area
            _poolDetailsScrollPosition = EditorGUILayout.BeginScrollView(_poolDetailsScrollPosition);
            
            // Draw header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Pool Details", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            // Check if a pool is selected
            if (string.IsNullOrEmpty(_selectedPoolKey) || !_poolStats.ContainsKey(_selectedPoolKey))
            {
                // No pool selected, show selection UI
                DrawPoolSelectionUI();
            }
            else
            {
                // Draw the selected pool details
                DrawSelectedPoolDetails();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        // UI for selecting a pool when none is selected
        private void DrawPoolSelectionUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Select a Pool to View Details", 
                new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter });
            
            GUILayout.Space(10);
            
            if (_poolStats.Count == 0)
            {
                EditorGUILayout.HelpBox("No pools active. Create a pool to view details.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("Available Pools:", EditorStyles.boldLabel);
                GUILayout.Space(5);
                
                // List available pools as buttons
                foreach (var poolEntry in _poolStats)
                {
                    string poolName = poolEntry.Key;
                    var stats = poolEntry.Value;
                    
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    // Pool type icon/indicator
                    Rect iconRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
                    EditorGUI.DrawRect(iconRect, stats.PoolType == "UI" ? 
                        new Color(0.4f, 0.6f, 0.9f) : new Color(0.4f, 0.8f, 0.4f));
                    
                    // Pool name and basic info
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(poolName, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Type: {stats.PoolType} • Size: {stats.TotalSize} • Active: {stats.ActiveCount}");
                    EditorGUILayout.EndVertical();
                    
                    // Select button
                    if (GUILayout.Button("Select", GUILayout.Width(80), GUILayout.Height(30)))
                    {
                        _selectedPoolKey = poolName;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    GUILayout.Space(3);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // Details UI for the selected pool
        private void DrawSelectedPoolDetails()
        {
            var stats = _poolStats[_selectedPoolKey];
            
            // Pool header with navigation
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            // Back button
            if (GUILayout.Button("←", GUILayout.Width(30), GUILayout.Height(20)))
            {
                _selectedPoolKey = null;
                return;
            }
            
            // Pool title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };
            
            EditorGUILayout.LabelField(_selectedPoolKey, titleStyle);
            
            // Pool type badge
            GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            
            GUI.backgroundColor = stats.PoolType == "UI" ? 
                new Color(0.4f, 0.6f, 0.9f) : new Color(0.4f, 0.8f, 0.4f);
                
            GUILayout.Label(stats.PoolType, badgeStyle, GUILayout.Width(40), GUILayout.Height(18));
            
            GUI.backgroundColor = Color.white;
            
            GUILayout.FlexibleSpace();
            
            // Refresh button
            if (GUILayout.Button("Refresh", GUILayout.Width(70), GUILayout.Height(20)))
            {
                RefreshPoolData();
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(PADDING);
            
            // Pool configuration details
            DrawPoolConfigDetails(stats);
            
            GUILayout.Space(PADDING);
            
            // Pool objects
            DrawPoolObjectsList(stats);
            
            GUILayout.Space(PADDING);
            
            // Performance data
            DrawPoolPerformanceData(stats);
            
            GUILayout.Space(PADDING);
            
            // Actions section
            DrawPoolActionsSection();
        }
        
        // Pool configuration details
        private void DrawPoolConfigDetails(PoolStatistics stats)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Pool Configuration", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // Split into columns
            EditorGUILayout.BeginHorizontal();
            
            // Left column - basic settings
            EditorGUILayout.BeginVertical(GUILayout.Width(MIN_WINDOW_WIDTH / 2 - 20));
            
            DrawLabelField("Initial Size:", stats.TotalSize.ToString());
            DrawLabelField("Max Size:", GetPoolPropertyValue("_maxSize", "Unknown"));
            DrawLabelField("Allow Growth:", GetPoolPropertyValue("_allowGrowth", "Unknown"));
            
            EditorGUILayout.EndVertical();
            
            // Right column - trimming settings
            EditorGUILayout.BeginVertical(GUILayout.Width(MIN_WINDOW_WIDTH / 2 - 20));
            
            DrawLabelField("Auto Trim Enabled:", GetPoolPropertyValue("_enableAutoTrim", "Unknown"));
            DrawLabelField("Trim Check Interval:", GetPoolPropertyValue("_trimCheckInterval", "Unknown") + " sec");
            DrawLabelField("Inactive Threshold:", GetPoolPropertyValue("_inactiveTimeThreshold", "Unknown") + " sec");
            DrawLabelField("Retain Ratio:", GetPoolPropertyValue("_targetRetainRatio", "Unknown"));
            DrawLabelField("Min Retain Count:", GetPoolPropertyValue("_minimumRetainCount", "Unknown"));
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        // Helper method to draw label field with consistent styling
        private void DrawLabelField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField(value);
            EditorGUILayout.EndHorizontal();
        }
        
        // Get pool property value using reflection
        private string GetPoolPropertyValue(string fieldName, string defaultValue)
        {
            if (!_isPoolManagerInitialized || _poolManagerInstance == null || string.IsNullOrEmpty(_selectedPoolKey))
                return defaultValue;
                
            try
            {
                var poolsDict = _poolsProperty?.GetValue(_poolManagerInstance) as System.Collections.IDictionary;
                if (poolsDict == null || !poolsDict.Contains(_selectedPoolKey))
                    return defaultValue;
                    
                var poolObj = poolsDict[_selectedPoolKey];
                Type poolType = poolObj.GetType();
                
                // Try to get field first
                var field = poolType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    var value = field.GetValue(poolObj);
                    return value != null ? value.ToString() : defaultValue;
                }
                
                // Then try property
                string propertyName = fieldName.StartsWith("_") ? fieldName.Substring(1) : fieldName;
                var property = poolType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    var value = property.GetValue(poolObj);
                    return value != null ? value.ToString() : defaultValue;
                }
                
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
        
        // Pool objects list
        private void DrawPoolObjectsList(PoolStatistics stats)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header with counts
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pooled Objects", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Active: {stats.ActiveCount}", 
                new GUIStyle(EditorStyles.miniLabel) { 
                    normal = { textColor = s_activeColor },
                    fontStyle = FontStyle.Bold
                });
            EditorGUILayout.LabelField($"Inactive: {stats.InactiveCount}", 
                new GUIStyle(EditorStyles.miniLabel) { 
                    normal = { textColor = s_inactiveColor },
                    fontStyle = FontStyle.Bold
                });
            EditorGUILayout.LabelField($"Total: {stats.TotalSize}", 
                new GUIStyle(EditorStyles.miniLabel) {
                    fontStyle = FontStyle.Bold
                });
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Active vs Inactive bar
            Rect barRect = GUILayoutUtility.GetRect(MIN_WINDOW_WIDTH - 40, 20);
            DrawPoolUsageBar(barRect, stats.ActiveCount, stats.InactiveCount);
            
            // A note that we can't show individual objects since we're using reflection
            GUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Individual pooled objects can't be listed in this view due to reflection limitations. " +
                "Check the Performance Analysis tab for efficiency data.",
                MessageType.Info
            );
            
            EditorGUILayout.EndVertical();
        }
        
        // Draw a bar showing active vs inactive objects
        private void DrawPoolUsageBar(Rect rect, int active, int inactive)
        {
            int total = active + inactive;
            if (total == 0) total = 1; // Avoid division by zero
            
            // Draw background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
            
            // Calculate proportions
            float activeRatio = (float)active / total;
            float inactiveWidth = rect.width * (1f - activeRatio);
            
            // Draw active part
            Rect activeRect = new Rect(rect.x, rect.y, rect.width * activeRatio, rect.height);
            EditorGUI.DrawRect(activeRect, s_activeColor);
            
            // Draw inactive part
            Rect inactiveRect = new Rect(rect.x + rect.width * activeRatio, rect.y, inactiveWidth, rect.height);
            EditorGUI.DrawRect(inactiveRect, s_inactiveColor);
            
            // Draw text overlay
            string text = $"Active: {active} ({activeRatio:P0}) | Inactive: {inactive} ({1-activeRatio:P0})";
            EditorGUI.LabelField(rect, text, new GUIStyle(EditorStyles.boldLabel) { 
                alignment = TextAnchor.MiddleCenter, 
                normal = { textColor = Color.white } 
            });
        }
        
        // Performance data
        private void DrawPoolPerformanceData(PoolStatistics stats)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Performance Metrics", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // Efficiency metrics
            float hitRate = CalculateHitRate(stats);
            float efficiency = CalculateEfficiency(stats);
            
            EditorGUILayout.BeginHorizontal();
            
            // Left column
            EditorGUILayout.BeginVertical(GUILayout.Width(MIN_WINDOW_WIDTH / 2 - 20));
            
            DrawLabelField("Hit Count:", stats.HitCount.ToString());
            DrawLabelField("Miss Count:", stats.MissCount.ToString());
            DrawLabelField("Hit Rate:", hitRate.ToString("P1"));
            
            EditorGUILayout.EndVertical();
            
            // Right column
            EditorGUILayout.BeginVertical(GUILayout.Width(MIN_WINDOW_WIDTH / 2 - 20));
            
            DrawLabelField("Avg. Wait Time:", stats.AverageWaitTime.ToString("F3") + " ms");
            DrawLabelField("Overall Efficiency:", efficiency.ToString("P1"));
            DrawLabelField("Analysis:", GetPoolEfficiencyAnalysis(stats));
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Hit/Miss ratio bar
            if (stats.HitCount + stats.MissCount > 0)
            {
                GUILayout.Label("Hit/Miss Ratio:", EditorStyles.boldLabel);
                Rect hitMissRect = GUILayoutUtility.GetRect(MIN_WINDOW_WIDTH - 40, 20);
                DrawHitMissBar(hitMissRect, stats.HitCount, stats.MissCount);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // Draw a bar showing hit vs miss ratio
        private void DrawHitMissBar(Rect rect, int hits, int misses)
        {
            int total = hits + misses;
            if (total == 0) return;
            
            // Draw background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
            
            // Calculate proportions
            float hitRatio = (float)hits / total;
            
            // Draw hit part
            Rect hitRect = new Rect(rect.x, rect.y, rect.width * hitRatio, rect.height);
            EditorGUI.DrawRect(hitRect, s_activeColor);
            
            // Draw miss part
            Rect missRect = new Rect(rect.x + rect.width * hitRatio, rect.y, rect.width * (1 - hitRatio), rect.height);
            EditorGUI.DrawRect(missRect, s_warningColor);
            
            // Draw text overlay
            string text = $"Hits: {hits} ({hitRatio:P0}) | Misses: {misses} ({1-hitRatio:P0})";
            EditorGUI.LabelField(rect, text, new GUIStyle(EditorStyles.boldLabel) { 
                alignment = TextAnchor.MiddleCenter, 
                normal = { textColor = Color.white } 
            });
        }
        
        // Pool actions section
        private void DrawPoolActionsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Pool Actions", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            // Clear pool button
            if (GUILayout.Button("Clear Pool", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear Pool", 
                    $"Are you sure you want to clear pool '{_selectedPoolKey}'?\nThis will release all objects.", 
                    "Yes, Clear Pool", "Cancel"))
                {
                    ClearPool(_selectedPoolKey);
                }
            }
            
            // Find in scenes (for future implementation)
            GUI.enabled = false;
            if (GUILayout.Button("Find Objects in Scene", GUILayout.Height(30)))
            {
                // This would be implemented to find objects in scene
                // Requires integration with scene hierarchy
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSettings() 
        {
            EditorGUILayout.BeginVertical();
            
            // Begin scrollable area
            _settingsScrollPosition = EditorGUILayout.BeginScrollView(_settingsScrollPosition);
            
            // Draw header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Settings & Controls", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(PADDING);
            
            // Dashboard settings section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Dashboard Settings", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // Refresh rate control
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Refresh Rate (seconds):", GUILayout.Width(180));
            float newRefreshRate = EditorGUILayout.Slider(_refreshRate, 0.1f, 5.0f);
            if (newRefreshRate != _refreshRate)
            {
                _refreshRate = newRefreshRate;
            }
            EditorGUILayout.EndHorizontal();
            
            // Auto-refresh toggle
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Auto Refresh:", GUILayout.Width(180));
            bool newAutoRefresh = EditorGUILayout.Toggle(_autoRefresh);
            if (newAutoRefresh != _autoRefresh)
            {
                _autoRefresh = newAutoRefresh;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(PADDING);
            
            // PoolManager controls section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("PoolManager Controls", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // Debug logging toggle
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Debug Logging:", GUILayout.Width(180));
            
            // Get current debug logging state
            bool isDebugEnabled = false;
            try
            {
                if (_isPoolManagerInitialized && _poolManagerInstance != null && _isDebugLogEnabledProperty != null)
                {
                    isDebugEnabled = (bool)_isDebugLogEnabledProperty.GetValue(_poolManagerInstance);
                }
            }
            catch {}
            
            bool newDebugEnabled = EditorGUILayout.Toggle(isDebugEnabled);
            if (newDebugEnabled != isDebugEnabled)
            {
                SetDebugLogging(newDebugEnabled);
            }
            EditorGUILayout.EndHorizontal();
            
            // Trim pools button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Trim All Pools:", GUILayout.Width(180));
            if (GUILayout.Button("Trim Now", GUILayout.Width(100)))
            {
                TrimExcessPools();
            }
            EditorGUILayout.EndHorizontal();
            
            // Clear all pools button with warning
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Clear All Pools:", GUILayout.Width(180));
            
            GUI.color = Color.red;
            if (GUILayout.Button("Clear All", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Clear All Pools", 
                    "Are you sure you want to clear all pools? This will release all pooled objects.", 
                    "Yes", "Cancel"))
                {
                    ClearAllPools();
                }
            }
            GUI.color = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            GUILayout.Space(PADDING);
            
            // Pool-specific controls section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Individual Pool Controls", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // Draw a list of all pools with individual controls
            if (_poolStats.Count > 0)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                EditorGUILayout.LabelField("Pool Name", GUILayout.Width(250));
                EditorGUILayout.LabelField("Size", GUILayout.Width(60));
                EditorGUILayout.LabelField("Active", GUILayout.Width(60));
                EditorGUILayout.LabelField("Actions", GUILayout.Width(180));
                EditorGUILayout.EndHorizontal();
                
                foreach (var poolEntry in _poolStats)
                {
                    string poolKey = poolEntry.Key;
                    var stats = poolEntry.Value;
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    // Pool name (truncated if needed)
                    EditorGUILayout.LabelField(TruncateName(poolKey, 40), GUILayout.Width(250));
                    
                    // Size
                    EditorGUILayout.LabelField(stats.TotalSize.ToString(), GUILayout.Width(60));
                    
                    // Active count
                    EditorGUILayout.LabelField(stats.ActiveCount.ToString(), GUILayout.Width(60));
                    
                    // Action buttons
                    if (GUILayout.Button("Details", GUILayout.Width(80)))
                    {
                        _selectedPoolKey = poolKey;
                        _selectedTab = 1; // Switch to Pool Details tab
                    }
                    
                    if (GUILayout.Button("Clear", GUILayout.Width(80)))
                    {
                        if (EditorUtility.DisplayDialog("Clear Pool", 
                            $"Are you sure you want to clear the pool '{TruncateName(poolKey, 40)}'?", 
                            "Yes", "Cancel"))
                        {
                            ClearPool(poolKey);
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No active pools found.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        #endregion
        
        #region Data Management Methods
        
        /// <summary>
        /// Check if PoolManager is initialized at runtime using reflection
        /// </summary>
        private void CheckPoolManagerInitialization()
        {
            // Reset state
            _isPoolManagerInitialized = false;
            _poolManagerInstance = null;
            
            try
            {
                // Get PoolManager type from the runtime assembly
                if (_poolManagerType == null)
                {
                    // Find the PoolManager type in any loaded assembly
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        _poolManagerType = assembly.GetType("com.thelegends.unity.pooling.PoolManager");
                        if (_poolManagerType != null)
                            break;
                    }
                    
                    if (_poolManagerType == null)
                    {
                        Debug.LogWarning("PoolManager type not found in loaded assemblies.");
                        return;
                    }
                }
                
                // Get instance through Instance property (assumed to be a singleton)
                PropertyInfo instanceProperty = _poolManagerType.GetProperty("Instance", 
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                
                if (instanceProperty == null)
                {
                    Debug.LogWarning("PoolManager.Instance property not found.");
                    return;
                }
                
                _poolManagerInstance = instanceProperty.GetValue(null);
                
                if (_poolManagerInstance == null)
                {
                    Debug.LogWarning("PoolManager.Instance is null. Make sure to create at least one pool first.");
                    return;
                }
                
                // Cache reflection information for methods and properties
                if (_poolsProperty == null)
                {
                    _poolsProperty = _poolManagerType.GetProperty("Pools", 
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                }
                
                if (_isDebugLogEnabledProperty == null)
                {
                    _isDebugLogEnabledProperty = _poolManagerType.GetProperty("IsDebugLogEnabled", 
                        BindingFlags.Public | BindingFlags.Instance);
                }
                
                if (_clearPoolMethod == null)
                {
                    _clearPoolMethod = _poolManagerType.GetMethod("ClearPool", 
                        BindingFlags.Public | BindingFlags.Instance);
                }
                
                if (_trimExcessPoolsMethod == null)
                {
                    _trimExcessPoolsMethod = _poolManagerType.GetMethod("TrimExcessPools", 
                        BindingFlags.Public | BindingFlags.Instance);
                }
                
                if (_instantiateCountField == null)
                {
                    // This is an assumption, we might need to adjust based on actual implementation
                    _instantiateCountField = _poolManagerType.GetField("_totalInstantiateCount", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                }
                
                _isPoolManagerInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                _isPoolManagerInitialized = false;
            }
        }
        
        /// <summary>
        /// Fetch pool data from PoolManager at runtime using reflection
        /// </summary>
        private void RefreshPoolData()
        {
            if (!_isPoolManagerInitialized || _poolManagerInstance == null)
                return;
            
            try
            {
                // Get the Pools dictionary from PoolManager
                var poolsDict = _poolsProperty?.GetValue(_poolManagerInstance) as System.Collections.IDictionary;
                
                if (poolsDict == null)
                    return;
                
                // Clear any pools that no longer exist
                List<string> keysToRemove = new List<string>();
                foreach (var key in _poolStats.Keys)
                {
                    if (!poolsDict.Contains(key))
                    {
                        keysToRemove.Add(key);
                    }
                }
                
                foreach (var key in keysToRemove)
                {
                    _poolStats.Remove(key);
                    _poolTimeSeriesData.Remove(key);
                    _previousInstantiateCount.Remove(key);
                }
                
                // Update/add stats for each pool
                foreach (var key in poolsDict.Keys)
                {
                    string poolKey = key.ToString();
                    var poolObj = poolsDict[key];
                    
                    // Get pool statistics through reflection
                    PoolStatistics stats = GetPoolStatistics(poolObj);
                    
                    if (stats != null)
                    {
                        _poolStats[poolKey] = stats;
                        
                        // Track instantiate counts for each pool
                        int currentInstantiateCount = stats.HitCount + stats.MissCount;
                        
                        if (!_previousInstantiateCount.TryGetValue(poolKey, out int previousCount))
                        {
                            previousCount = 0;
                        }
                        
                        // Calculate new instantiates since last update
                        int newInstantiates = currentInstantiateCount - previousCount;
                        if (newInstantiates < 0) newInstantiates = 0; // In case of pool clearing
                        
                        // Store for timeline
                        if (!_poolTimeSeriesData.TryGetValue(poolKey, out PoolTimeSeriesData timeSeriesData))
                        {
                            timeSeriesData = new PoolTimeSeriesData();
                            _poolTimeSeriesData[poolKey] = timeSeriesData;
                        }
                        
                        _previousInstantiateCount[poolKey] = currentInstantiateCount;
                    }
                }
                
                // After fetching pool data, update dashboard data
                UpdateDashboardData();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        
        /// <summary>
        /// Extract pool statistics from a pool object using reflection
        /// </summary>
        private PoolStatistics GetPoolStatistics(object poolObject)
        {
            if (poolObject == null)
                return null;
                
            try
            {
                Type poolType = poolObject.GetType();
                
                PoolStatistics stats = new PoolStatistics();
                
                // Get total size - usually available as a property or field
                var totalSizeProperty = poolType.GetProperty("TotalSize");
                if (totalSizeProperty != null)
                {
                    stats.TotalSize = (int)totalSizeProperty.GetValue(poolObject);
                }
                else
                {
                    // Try as a field
                    var totalSizeField = poolType.GetField("_totalSize", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (totalSizeField != null)
                    {
                        stats.TotalSize = (int)totalSizeField.GetValue(poolObject);
                    }
                }
                
                // Get active count
                var activeCountProperty = poolType.GetProperty("ActiveCount");
                if (activeCountProperty != null)
                {
                    stats.ActiveCount = (int)activeCountProperty.GetValue(poolObject);
                }
                else
                {
                    // Try as a field or calculate it
                    var activeCountField = poolType.GetField("_activeCount", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (activeCountField != null)
                    {
                        stats.ActiveCount = (int)activeCountField.GetValue(poolObject);
                    }
                    else
                    {
                        // Try to get from all objects list if exists
                        var allObjectsField = poolType.GetField("_allObjects", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (allObjectsField != null)
                        {
                            var allObjects = allObjectsField.GetValue(poolObject) as System.Collections.ICollection;
                            if (allObjects != null)
                            {
                                stats.TotalSize = allObjects.Count;
                            }
                        }
                    }
                }
                
                // Get inactive count
                var inactiveCountProperty = poolType.GetProperty("InactiveCount");
                if (inactiveCountProperty != null)
                {
                    stats.InactiveCount = (int)inactiveCountProperty.GetValue(poolObject);
                }
                else
                {
                    // Try as a field
                    var inactiveObjectsField = poolType.GetField("_inactiveObjects", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (inactiveObjectsField != null)
                    {
                        var inactiveObjects = inactiveObjectsField.GetValue(poolObject) as System.Collections.ICollection;
                        if (inactiveObjects != null)
                        {
                            stats.InactiveCount = inactiveObjects.Count;
                        }
                    }
                    
                    // If unable to get inactive count directly, calculate from total and active
                    if (stats.InactiveCount == 0 && stats.TotalSize > 0)
                    {
                        stats.InactiveCount = stats.TotalSize - stats.ActiveCount;
                    }
                }
                
                // Get hit count
                var hitCountField = poolType.GetField("_hitCount", BindingFlags.NonPublic | BindingFlags.Instance);
                if (hitCountField != null)
                {
                    stats.HitCount = (int)hitCountField.GetValue(poolObject);
                }
                
                // Get miss count
                var missCountField = poolType.GetField("_missCount", BindingFlags.NonPublic | BindingFlags.Instance);
                if (missCountField != null)
                {
                    stats.MissCount = (int)missCountField.GetValue(poolObject);
                }
                
                // Get average wait time if available
                var avgWaitTimeField = poolType.GetField("_averageWaitTime", BindingFlags.NonPublic | BindingFlags.Instance);
                if (avgWaitTimeField != null)
                {
                    stats.AverageWaitTime = (float)avgWaitTimeField.GetValue(poolObject);
                }
                
                // Determine pool type
                bool isUIPool = poolType.Name.Contains("UIPool");
                stats.PoolType = isUIPool ? "UI" : "Standard";
                
                return stats;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }
        
        /// <summary>
        /// Clear all pool data to prevent memory leaks in editor
        /// </summary>
        private void ClearAllData()
        {
            _poolStats.Clear();
            _poolTimeSeriesData.Clear();
            _globalActiveCounts.Clear();
            _globalInactiveCounts.Clear();
            _globalTimestamps.Clear();
            _globalInstantiateEvents.Clear();
            _previousInstantiateCount.Clear();
        }
        
        /// <summary>
        /// Updates dashboard data based on current pool statistics
        /// </summary>
        private void UpdateDashboardData()
        {
            // Reset total counters
            _totalActiveCount = 0;
            _totalInactiveCount = 0;
            _totalMissCount = 0;
            
            // Get current timestamp
            float currentTime = (float)EditorApplication.timeSinceStartup;
            
            // Track global instantiate events
            int newInstantiateEvents = 0;
            
            // Process each pool's statistics
            foreach (var poolEntry in _poolStats)
            {
                string poolKey = poolEntry.Key;
                var stats = poolEntry.Value;
                
                // Update totals
                _totalActiveCount += stats.ActiveCount;
                _totalInactiveCount += stats.InactiveCount;
                _totalMissCount += stats.MissCount;
                
                // Track instantiate events (assume they're reported in stats)
                int instantiateEventsSinceLastUpdate = 0; // This would come from PoolManager
                newInstantiateEvents += instantiateEventsSinceLastUpdate;
                
                // Ensure we have a time series entry for this pool
                if (!_poolTimeSeriesData.TryGetValue(poolKey, out PoolTimeSeriesData timeSeriesData))
                {
                    timeSeriesData = new PoolTimeSeriesData();
                    _poolTimeSeriesData[poolKey] = timeSeriesData;
                }
                
                // Add current data point
                timeSeriesData.ActiveCounts.Add(stats.ActiveCount);
                timeSeriesData.InactiveCounts.Add(stats.InactiveCount);
                timeSeriesData.Timestamps.Add(currentTime);
                timeSeriesData.InstantiateEvents.Add(instantiateEventsSinceLastUpdate);
                
                // Trim if we have too many samples
                if (timeSeriesData.Timestamps.Count > MAX_HISTORY_SAMPLES)
                {
                    timeSeriesData.ActiveCounts.RemoveAt(0);
                    timeSeriesData.InactiveCounts.RemoveAt(0);
                    timeSeriesData.Timestamps.RemoveAt(0);
                    timeSeriesData.InstantiateEvents.RemoveAt(0);
                }
            }
            
            // Update global instantiate count
            _totalInstantiateCount += newInstantiateEvents;
            
            // Update global time series
            _globalActiveCounts.Add(_totalActiveCount);
            _globalInactiveCounts.Add(_totalInactiveCount);
            _globalTimestamps.Add(currentTime);
            _globalInstantiateEvents.Add(newInstantiateEvents);
            
            // Trim global data if needed
            if (_globalTimestamps.Count > MAX_HISTORY_SAMPLES)
            {
                _globalActiveCounts.RemoveAt(0);
                _globalInactiveCounts.RemoveAt(0);
                _globalTimestamps.RemoveAt(0);
                _globalInstantiateEvents.RemoveAt(0);
            }
            
            // Calculate global efficiency
            if (_totalActiveCount + _totalInactiveCount > 0)
            {
                int totalHits = 0;
                int totalRequests = 0;
                
                foreach (var stats in _poolStats.Values)
                {
                    totalHits += stats.HitCount;
                    totalRequests += stats.HitCount + stats.MissCount;
                }
                
                if (totalRequests > 0)
                {
                    _globalEfficiency = (float)totalHits / totalRequests;
                }
            }
            
            // Estimate memory usage - this is a rough approximation
            // In a real implementation, this would get data from PoolManager
            _systemMemoryUsage = EstimatePoolMemoryUsage();
        }
        
        /// <summary>
        /// Estimates memory usage of all pools (simplified for now)
        /// </summary>
        private float EstimatePoolMemoryUsage()
        {
            // This is a placeholder - in a real implementation, we would 
            // get more accurate memory data from the PoolManager
            float estimatedMB = 0f;
            
            foreach (var stats in _poolStats.Values)
            {
                // Rough estimate assuming average GameObject size
                float averageObjectSizeKB = 100f; // Arbitrary value for demonstration
                estimatedMB += (stats.TotalSize * averageObjectSizeKB) / 1024f;
            }
            
            return estimatedMB;
        }
        
        // Draw summary statistics panel
        private void DrawDashboardSummary()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("System Overview", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // Grid layout for stats
            EditorGUILayout.BeginHorizontal();
            
            // Total pools
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(MIN_WINDOW_WIDTH / 4 - 15));
            EditorGUILayout.LabelField("Total Pools:", EditorStyles.boldLabel);
            GUILayout.Label(_poolStats.Count.ToString(), new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 24 });
            EditorGUILayout.EndVertical();
            
            // Active objects
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(MIN_WINDOW_WIDTH / 4 - 15));
            EditorGUILayout.LabelField("Active Objects:", EditorStyles.boldLabel);
            GUILayout.Label(_totalActiveCount.ToString(), new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 24, normal = { textColor = s_activeColor } });
            EditorGUILayout.EndVertical();
            
            // Inactive objects
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(MIN_WINDOW_WIDTH / 4 - 15));
            EditorGUILayout.LabelField("Inactive Objects:", EditorStyles.boldLabel);
            GUILayout.Label(_totalInactiveCount.ToString(), new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 24, normal = { textColor = s_inactiveColor } });
            EditorGUILayout.EndVertical();
            
            // System efficiency
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(MIN_WINDOW_WIDTH / 4 - 15));
            EditorGUILayout.LabelField("Hit Rate:", EditorStyles.boldLabel);
            GUILayout.Label(_globalEfficiency.ToString("P0"), new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 24 });
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            // Second row of stats
            EditorGUILayout.BeginHorizontal();
            
            // Total missed hits
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(MIN_WINDOW_WIDTH / 4 - 15));
            EditorGUILayout.LabelField("Total Misses:", EditorStyles.boldLabel);
            GUILayout.Label(_totalMissCount.ToString(), new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 24, normal = { textColor = s_warningColor } });
            EditorGUILayout.EndVertical();
            
            // Instantiate count
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(MIN_WINDOW_WIDTH / 4 - 15));
            EditorGUILayout.LabelField("Total Instantiates:", EditorStyles.boldLabel);
            GUILayout.Label(_totalInstantiateCount.ToString(), new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 24, normal = { textColor = s_instantiateColor } });
            EditorGUILayout.EndVertical();
            
            // Estimated memory
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(MIN_WINDOW_WIDTH / 4 - 15));
            EditorGUILayout.LabelField("Est. Memory Usage:", EditorStyles.boldLabel);
            GUILayout.Label(_systemMemoryUsage.ToString("F2") + " MB", new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 24 });
            EditorGUILayout.EndVertical();
            
            // Last update time
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(MIN_WINDOW_WIDTH / 4 - 15));
            EditorGUILayout.LabelField("Update Rate:", EditorStyles.boldLabel);
            GUILayout.Label(_refreshRate.ToString("F1") + "s", new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 24 });
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        // Draw real-time graph with active and inactive objects over time
        private void DrawRealTimeGraph()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Real-Time Object Allocation", EditorStyles.boldLabel);
            
            // Create rect for graph
            Rect graphRect = GUILayoutUtility.GetRect(MIN_WINDOW_WIDTH - 40, 200);
            
            if (Event.current.type == EventType.Repaint && _globalTimestamps.Count > 0)
            {
                // Draw background
                EditorGUI.DrawRect(graphRect, new Color(0.15f, 0.15f, 0.15f, 1f));
                
                // Draw grid lines
                DrawGraphGrid(graphRect);
                
                // Find max value for scaling
                int maxValue = 1; // Avoid division by zero
                
                foreach (var count in _globalActiveCounts)
                {
                    maxValue = Mathf.Max(maxValue, count);
                }
                
                foreach (var count in _globalInactiveCounts)
                {
                    maxValue = Mathf.Max(maxValue, count);
                }
                
                // Round max value up to nearest multiple of 10 for cleaner scale
                maxValue = ((maxValue + 9) / 10) * 10;
                
                // Draw active objects line
                DrawGraphLine(graphRect, _globalTimestamps, _globalActiveCounts, s_activeColor, maxValue);
                
                // Draw inactive objects line
                DrawGraphLine(graphRect, _globalTimestamps, _globalInactiveCounts, s_inactiveColor, maxValue);
                
                // Draw legend
                DrawGraphLegend(graphRect, maxValue);
            }
            else if (_globalTimestamps.Count == 0)
            {
                // If no data is available
                EditorGUI.LabelField(graphRect, "No data available yet. Waiting for pool usage...", 
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { 
                        fontSize = 14, 
                        alignment = TextAnchor.MiddleCenter 
                    });
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // Draw grid lines for the graph
        private void DrawGraphGrid(Rect rect)
        {
            // Draw vertical time markers
            int timeMarkers = 6; // Number of vertical time markers
            
            for (int i = 0; i < timeMarkers; i++)
            {
                float x = rect.x + (rect.width * i / (timeMarkers - 1));
                
                // Draw vertical line
                EditorGUI.DrawRect(
                    new Rect(x, rect.y, 1, rect.height), 
                    new Color(0.3f, 0.3f, 0.3f, 0.5f)
                );
                
                // Calculate time label
                float time = i * (MAX_HISTORY_SAMPLES * _refreshRate) / (timeMarkers - 1);
                string timeLabel = "-" + time.ToString("F0") + "s";
                
                if (i == 0)
                {
                    timeLabel = "Now";
                }
                
                // Draw time label
                EditorGUI.LabelField(
                    new Rect(x - 20, rect.y + rect.height - 15, 40, 15), 
                    timeLabel,
                    new GUIStyle(EditorStyles.miniLabel) { 
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                    }
                );
            }
            
            // Draw horizontal count markers
            int countMarkers = 5; // Number of horizontal count markers
            
            for (int i = 0; i < countMarkers; i++)
            {
                float y = rect.y + rect.height - (rect.height * i / (countMarkers - 1));
                
                // Skip the top line since it overlaps with the border
                if (i > 0)
                {
                    // Draw horizontal line
                    EditorGUI.DrawRect(
                        new Rect(rect.x, y, rect.width, 1), 
                        new Color(0.3f, 0.3f, 0.3f, 0.5f)
                    );
                }
            }
        }
        
        // Draw a line on the graph
        private void DrawGraphLine(Rect rect, List<float> timestamps, List<int> values, Color color, int maxValue)
        {
            if (timestamps.Count < 2 || values.Count < 2)
                return;
                
            int count = Mathf.Min(timestamps.Count, values.Count);
            
            // Calculate time range
            float latestTime = timestamps[timestamps.Count - 1];
            float timeRange = MAX_HISTORY_SAMPLES * _refreshRate; // Timespan to show
            
            // Draw line segments
            for (int i = 1; i < count; i++)
            {
                float time1 = timestamps[i - 1];
                float time2 = timestamps[i];
                
                float timeOffset1 = latestTime - time1;
                float timeOffset2 = latestTime - time2;
                
                float x1 = rect.x + rect.width - (timeOffset1 / timeRange) * rect.width;
                float x2 = rect.x + rect.width - (timeOffset2 / timeRange) * rect.width;
                
                // Only draw if within visible range
                if (x1 < rect.x + rect.width && x2 > rect.x)
                {
                    float y1 = rect.y + rect.height - (values[i - 1] / (float)maxValue) * rect.height;
                    float y2 = rect.y + rect.height - (values[i] / (float)maxValue) * rect.height;
                    
                    // Clamp to graph bounds
                    x1 = Mathf.Clamp(x1, rect.x, rect.x + rect.width);
                    x2 = Mathf.Clamp(x2, rect.x, rect.x + rect.width);
                    y1 = Mathf.Clamp(y1, rect.y, rect.y + rect.height);
                    y2 = Mathf.Clamp(y2, rect.y, rect.y + rect.height);
                    
                    // Draw line segment
                    Handles.BeginGUI();
                    Handles.color = color;
                    Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2));
                    Handles.EndGUI();
                }
            }
        }
        
        // Draw graph legend and value labels
        private void DrawGraphLegend(Rect rect, int maxValue)
        {
            float padding = 5f;
            
            // Draw legend box
            Rect legendRect = new Rect(rect.x + padding, rect.y + padding, 140, 50);
            EditorGUI.DrawRect(legendRect, new Color(0.1f, 0.1f, 0.1f, 0.7f));
            
            // Draw active objects legend item
            Rect activeIconRect = new Rect(legendRect.x + 5, legendRect.y + 10, 15, 15);
            EditorGUI.DrawRect(activeIconRect, s_activeColor);
            EditorGUI.LabelField(
                new Rect(activeIconRect.x + 20, activeIconRect.y, 100, 15),
                "Active Objects",
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } }
            );
            
            // Draw inactive objects legend item
            Rect inactiveIconRect = new Rect(legendRect.x + 5, legendRect.y + 30, 15, 15);
            EditorGUI.DrawRect(inactiveIconRect, s_inactiveColor);
            EditorGUI.LabelField(
                new Rect(inactiveIconRect.x + 20, inactiveIconRect.y, 100, 15),
                "Inactive Objects",
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } }
            );
            
            // Draw value labels on y-axis
            int countMarkers = 5;
            for (int i = 0; i < countMarkers; i++)
            {
                float y = rect.y + rect.height - (rect.height * i / (countMarkers - 1));
                
                // Calculate value at this marker
                int value = (int)(maxValue * i / (countMarkers - 1));
                
                // Draw value label
                EditorGUI.LabelField(
                    new Rect(rect.x + 5, y - 8, 40, 15),
                    value.ToString(),
                    new GUIStyle(EditorStyles.miniLabel) { 
                        alignment = TextAnchor.MiddleRight,
                        normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                    }
                );
            }
        }
        
        // Draw instantiate events timeline with markers
        private void DrawInstantiateEventsTimeline()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Instantiate Events", EditorStyles.boldLabel);
            
            // Create rect for timeline
            Rect timelineRect = GUILayoutUtility.GetRect(MIN_WINDOW_WIDTH - 40, 80);
            
            if (Event.current.type == EventType.Repaint && _globalTimestamps.Count > 0)
            {
                // Draw background
                EditorGUI.DrawRect(timelineRect, new Color(0.15f, 0.15f, 0.15f, 1f));
                
                // Draw grid lines
                float latestTime = _globalTimestamps[_globalTimestamps.Count - 1];
                float timeRange = MAX_HISTORY_SAMPLES * _refreshRate;
                
                // Draw time markers
                int timeMarkers = 6;
                for (int i = 0; i < timeMarkers; i++)
                {
                    float x = timelineRect.x + (timelineRect.width * i / (timeMarkers - 1));
                    
                    // Draw vertical line
                    EditorGUI.DrawRect(
                        new Rect(x, timelineRect.y, 1, timelineRect.height),
                        new Color(0.3f, 0.3f, 0.3f, 0.5f)
                    );
                    
                    // Calculate time label
                    float time = i * timeRange / (timeMarkers - 1);
                    string timeLabel = "-" + time.ToString("F0") + "s";
                    
                    if (i == 0)
                    {
                        timeLabel = "Now";
                    }
                    
                    // Draw time label
                    EditorGUI.LabelField(
                        new Rect(x - 20, timelineRect.y + timelineRect.height - 15, 40, 15),
                        timeLabel,
                        new GUIStyle(EditorStyles.miniLabel) {
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                        }
                    );
                }
                
                // Draw instantiate events as markers
                int count = Mathf.Min(_globalTimestamps.Count, _globalInstantiateEvents.Count);
                
                for (int i = 0; i < count; i++)
                {
                    float time = _globalTimestamps[i];
                    int instantiateCount = _globalInstantiateEvents[i];
                    
                    if (instantiateCount > 0)
                    {
                        float timeOffset = latestTime - time;
                        float x = timelineRect.x + timelineRect.width - (timeOffset / timeRange) * timelineRect.width;
                        
                        // Only draw if within visible range
                        if (x >= timelineRect.x && x <= timelineRect.x + timelineRect.width)
                        {
                            // Draw marker based on instantiate count
                            float markerSize = Mathf.Clamp(5 + Mathf.Log10(instantiateCount) * 5, 5, 15);
                            
                            // Draw instantiate marker
                            Rect markerRect = new Rect(
                                x - markerSize / 2,
                                timelineRect.y + timelineRect.height / 2 - markerSize / 2,
                                markerSize,
                                markerSize
                            );
                            
                            EditorGUI.DrawRect(markerRect, s_instantiateColor);
                            
                            // Show tooltip for larger instantiate events
                            if (instantiateCount > 3 && markerRect.Contains(Event.current.mousePosition))
                            {
                                GUIStyle tooltipStyle = new GUIStyle(EditorStyles.helpBox) {
                                    alignment = TextAnchor.MiddleCenter,
                                    normal = { textColor = Color.white },
                                    padding = new RectOffset(5, 5, 5, 5)
                                };
                                
                                string tooltipText = instantiateCount + " object" + (instantiateCount > 1 ? "s" : "") + " instantiated";
                                Vector2 tooltipSize = tooltipStyle.CalcSize(new GUIContent(tooltipText));
                                
                                Rect tooltipRect = new Rect(
                                    Mathf.Clamp(Event.current.mousePosition.x - tooltipSize.x / 2, timelineRect.x, timelineRect.x + timelineRect.width - tooltipSize.x),
                                    markerRect.y - tooltipSize.y - 5,
                                    tooltipSize.x,
                                    tooltipSize.y
                                );
                                
                                EditorGUI.DrawRect(tooltipRect, new Color(0.1f, 0.1f, 0.1f, 0.9f));
                                EditorGUI.LabelField(tooltipRect, tooltipText, tooltipStyle);
                            }
                        }
                    }
                }
                
                // Draw legend
                Rect legendRect = new Rect(timelineRect.x + 5, timelineRect.y + 5, 100, 25);
                EditorGUI.DrawRect(legendRect, new Color(0.1f, 0.1f, 0.1f, 0.7f));
                
                Rect iconRect = new Rect(legendRect.x + 5, legendRect.y + 5, 15, 15);
                EditorGUI.DrawRect(iconRect, s_instantiateColor);
                
                EditorGUI.LabelField(
                    new Rect(iconRect.x + 20, iconRect.y, 80, 15),
                    "Instantiate",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } }
                );
            }
            else if (_globalTimestamps.Count == 0)
            {
                // If no data is available
                EditorGUI.LabelField(timelineRect, "No instantiate events recorded yet", 
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { 
                        fontSize = 14, 
                        alignment = TextAnchor.MiddleCenter 
                    });
            }
            
            EditorGUILayout.EndVertical();
        }
        
        // Draw pool quick stats
        private void DrawPoolQuickStats()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Pool Quick Stats", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // Draw table header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Pool Name", GUILayout.Width(200));
            EditorGUILayout.LabelField("Type", GUILayout.Width(60));
            EditorGUILayout.LabelField("Size", GUILayout.Width(60));
            EditorGUILayout.LabelField("Active", GUILayout.Width(60));
            EditorGUILayout.LabelField("Hit Rate", GUILayout.Width(80));
            EditorGUILayout.LabelField("Status", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
            
            // Draw a row for each pool
            foreach (var poolEntry in _poolStats)
            {
                var stats = poolEntry.Value;
                string poolName = poolEntry.Key;
                
                EditorGUILayout.BeginHorizontal();
                
                // Make the pool name clickable to select it in the Pool Details tab
                if (GUILayout.Button(TruncateName(poolName, 30), EditorStyles.label, GUILayout.Width(200)))
                {
                    _selectedPoolKey = poolName;
                }
                
                // Pool type
                EditorGUILayout.LabelField(stats.PoolType, GUILayout.Width(60));
                
                // Size
                EditorGUILayout.LabelField(stats.TotalSize.ToString(), GUILayout.Width(60));
                
                // Active count
                EditorGUILayout.LabelField(stats.ActiveCount.ToString(), GUILayout.Width(60));
                
                // Hit rate
                float hitRate = CalculateHitRate(stats);
                EditorGUILayout.LabelField(hitRate.ToString("P0"), GUILayout.Width(80));
                
                // Status - based on efficiency
                float efficiency = CalculateEfficiency(stats);
                string status = "Unknown";
                Color statusColor = Color.gray;
                
                if (efficiency >= 0.8f)
                {
                    status = "Optimal";
                    statusColor = s_activeColor;
                }
                else if (efficiency >= 0.5f)
                {
                    status = "Good";
                    statusColor = Color.yellow;
                }
                else if (efficiency >= 0.3f)
                {
                    status = "Needs Attention";
                    statusColor = s_warningColor;
                }
                else
                {
                    status = "Poor";
                    statusColor = s_inactiveColor;
                }
                
                GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
                statusStyle.normal.textColor = statusColor;
                EditorGUILayout.LabelField(status, statusStyle, GUILayout.Width(120));
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        #endregion
        
        #region Action Methods
        
        /// <summary>
        /// Calls ClearPool on the PoolManager for a specific pool
        /// </summary>
        public void ClearPool(string poolKey)
        {
            if (!_isPoolManagerInitialized || _poolManagerInstance == null || string.IsNullOrEmpty(poolKey))
                return;
                
            try
            {
                _clearPoolMethod?.Invoke(_poolManagerInstance, new object[] { poolKey });
                
                // Update immediately after clearing
                RefreshPoolData();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        
        /// <summary>
        /// Calls TrimExcessPools on the PoolManager
        /// </summary>
        public void TrimExcessPools()
        {
            if (!_isPoolManagerInitialized || _poolManagerInstance == null)
                return;
                
            try
            {
                _trimExcessPoolsMethod?.Invoke(_poolManagerInstance, null);
                
                // Update immediately after trimming
                RefreshPoolData();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        
        /// <summary>
        /// Sets the debug logging state on the PoolManager
        /// </summary>
        public void SetDebugLogging(bool enabled)
        {
            if (!_isPoolManagerInitialized || _poolManagerInstance == null)
                return;
                
            try
            {
                _isDebugLogEnabledProperty?.SetValue(_poolManagerInstance, enabled);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        
        /// <summary>
        /// Calls ClearAllPools on the PoolManager
        /// </summary>
        public void ClearAllPools()
        {
            if (!_isPoolManagerInitialized || _poolManagerInstance == null)
                return;
                
            try
            {
                var clearAllPoolsMethod = _poolManagerType.GetMethod("ClearAllPools", 
                    BindingFlags.Public | BindingFlags.Instance);
                    
                if (clearAllPoolsMethod != null)
                {
                    clearAllPoolsMethod.Invoke(_poolManagerInstance, null);
                    
                    // Update immediately after clearing
                    RefreshPoolData();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        
        #endregion
        
        #region Performance Analysis Methods
        
        private void DrawPerformanceAnalysis() 
        {
            EditorGUILayout.BeginVertical();
            
            // Begin scrollable area
            _analysisScrollPosition = EditorGUILayout.BeginScrollView(_analysisScrollPosition);
            
            // Draw header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Performance Analysis", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(PADDING);
            
            // Global efficiency stats
            DrawGlobalEfficiencyStats();
            
            GUILayout.Space(PADDING);
            
            // Draw efficiency analysis section
            DrawEfficiencyAnalysisSection();
            
            GUILayout.Space(PADDING);
            
            // Draw comparative charts
            DrawComparativeEfficiencyChart();
            
            GUILayout.Space(PADDING);
            
            // Draw memory usage analysis
            DrawMemoryUsageAnalysis();
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawGlobalEfficiencyStats()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Global Pooling Efficiency", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // Display global stats here (will be implemented)
            EditorGUILayout.LabelField("Total Active Pools: " + _poolStats.Count.ToString());
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawEfficiencyAnalysisSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Pool Efficiency Analysis", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            // Draw table header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Pool Name", GUILayout.Width(200));
            EditorGUILayout.LabelField("Efficiency", GUILayout.Width(80));
            EditorGUILayout.LabelField("Hit Rate", GUILayout.Width(80));
            EditorGUILayout.LabelField("Size", GUILayout.Width(60));
            EditorGUILayout.LabelField("Active", GUILayout.Width(60));
            EditorGUILayout.LabelField("Analysis", GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();
            
            // For each pool, display efficiency metrics and analysis
            foreach (var poolEntry in _poolStats)
            {
                string poolName = poolEntry.Key;
                var stats = poolEntry.Value;
                
                // Efficiency rating (example calculation)
                float efficiency = CalculateEfficiency(stats);
                float hitRate = CalculateHitRate(stats);
                
                // Dynamically determine color based on efficiency
                Color barColor = efficiency > 0.8f ? s_activeColor : (efficiency > 0.4f ? s_warningColor : s_inactiveColor);
                
                // Draw each pool row
                EditorGUILayout.BeginHorizontal();
                
                // Pool name (truncated if needed)
                EditorGUILayout.LabelField(TruncateName(poolName, 30), GUILayout.Width(200));
                
                // Efficiency bar
                DrawHorizontalBar(efficiency, 80, barColor);
                
                // Hit Rate
                EditorGUILayout.LabelField(hitRate.ToString("P0"), GUILayout.Width(80));
                
                // Size and active counts
                EditorGUILayout.LabelField(stats.TotalSize.ToString(), GUILayout.Width(60));
                EditorGUILayout.LabelField(stats.ActiveCount.ToString(), GUILayout.Width(60));
                
                // Analysis and recommendations
                EditorGUILayout.LabelField(GetPoolEfficiencyAnalysis(stats), GUILayout.Width(200));
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private string GetPoolEfficiencyAnalysis(PoolStatistics stats)
        {
            // Return empty string if stats are null
            if (stats == null)
                return string.Empty;
            
            // Perform analysis based on pool statistics
            float usage = (float)stats.ActiveCount / stats.TotalSize;
            float hitRate = CalculateHitRate(stats);
            
            if (stats.TotalSize == 0)
                return "Empty pool";
                
            if (usage < 0.2f && stats.TotalSize > 20)
                return "Oversized pool";
                
            if (usage > 0.9f && stats.MissCount > 0)
                return "Consider increasing size";
                
            if (hitRate < 0.5f)
                return "Low hit rate efficiency";
                
            if (usage >= 0.4f && usage <= 0.8f && hitRate > 0.8f)
                return "Optimal configuration";
                
            return "Requires more usage data";
        }
        
        private void DrawComparativeEfficiencyChart()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Comparative Efficiency Chart", EditorStyles.boldLabel);
            
            // This will be implemented in a future version
            EditorGUILayout.LabelField("Efficiency chart will be implemented in a future part");
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawMemoryUsageAnalysis()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Memory Usage Analysis", EditorStyles.boldLabel);
            
            // This will be implemented in a future version
            EditorGUILayout.LabelField("Memory analysis will be implemented in a future part");
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawHorizontalBar(float value, float width, Color color)
        {
            Rect rect = GUILayoutUtility.GetRect(width, 18, GUILayout.ExpandWidth(false), GUILayout.Width(width));
            
            // Draw background
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            
            // Draw filled part
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height);
            EditorGUI.DrawRect(fillRect, color);
            
            // Draw text
            string percentage = (value * 100).ToString("0") + "%";
            EditorGUI.LabelField(rect, percentage, new GUIStyle(EditorStyles.boldLabel) { 
                alignment = TextAnchor.MiddleCenter, 
                normal = { textColor = Color.white } 
            });
        }
        
        private string TruncateName(string name, int maxLength)
        {
            if (string.IsNullOrEmpty(name))
                return name;
                
            return name.Length <= maxLength ? name : name.Substring(0, maxLength - 3) + "...";
        }
        
        private float CalculateEfficiency(PoolStatistics stats)
        {
            if (stats == null || stats.TotalSize == 0)
                return 0f;
                
            // Efficiency formula factors:
            // 1. Pool utilization (active/total)
            // 2. Hit rate (hits/total requests)
            // 3. Memory utilization factor
            
            float utilization = Mathf.Clamp01((float)stats.ActiveCount / stats.TotalSize);
            float hitRatio = CalculateHitRate(stats);
            
            // Ideal utilization is between 40-80%
            // Less than 40% = pool is too large
            // More than 80% = risk of misses
            float utilizationScore;
            if (utilization < 0.4f)
                utilizationScore = utilization / 0.4f; // Scale up to 1.0 at 40%
            else if (utilization <= 0.8f)
                utilizationScore = 1.0f; // Perfect range
            else
                utilizationScore = 1.0f - (utilization - 0.8f) / 0.2f; // Scale down from 1.0 to 0.0
                
            // Combine factors (hit rate is most important, then utilization)
            return (hitRatio * 0.6f) + (utilizationScore * 0.4f);
        }
        
        private float CalculateHitRate(PoolStatistics stats)
        {
            if (stats == null)
                return 0f;
                
            int totalRequests = stats.HitCount + stats.MissCount;
            if (totalRequests == 0)
                return 1.0f; // No misses yet
                
            return (float)stats.HitCount / totalRequests;
        }
        
        #endregion
    }

    // Helper class to store pool statistics
    [Serializable]
    public class PoolStatistics
    {
        public int TotalSize;        // Total pool capacity
        public int ActiveCount;      // Currently active objects
        public int InactiveCount;    // Objects in the pool
        public int HitCount;         // Successful retrievals from pool
        public int MissCount;        // Misses (when pool was empty)
        public float AverageWaitTime; // Average time waiting for object retrieval
        public string PoolType;      // Type of the pool (regular or UI)
    }
}