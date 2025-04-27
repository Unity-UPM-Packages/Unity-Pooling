using System;
using System.Collections.Generic;
using System.Linq;
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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Dashboard will be implemented in the next part", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPoolDetails() 
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Pool Details will be implemented in a future part", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
        }
        
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
        
        private void DrawSettings() 
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Settings will be implemented in a future part", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
        }
        
        #endregion
        
        #region Data Management Methods
        
        private void CheckPoolManagerInitialization()
        {
            // This will be implemented to check if PoolManager exists at runtime
            _isPoolManagerInitialized = true; // Temporary placeholder
        }
        
        private void RefreshPoolData()
        {
            // This will be implemented to fetch data from PoolManager
            // For now, this is just a placeholder
        }
        
        private void ClearAllData()
        {
            // This will be implemented to clear all stored data when exiting play mode
            // to prevent memory leaks in the editor
            _poolStats.Clear();
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