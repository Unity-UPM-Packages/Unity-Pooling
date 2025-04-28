using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace com.thelegends.unity.pooling.Editor
{
    /// <summary>
    /// Editor window for monitoring and managing object pools
    /// Provides visualization, statistics, and performance analysis for the pooling system
    /// </summary>
    public class PoolManagerEditor : EditorWindow
    {
        [MenuItem("Tools/Object Pooling/Pool Manager")]
        public static void ShowWindow()
        {
            GetWindow<PoolManagerEditor>("Pool Manager");
        }

        // Main tabs of the editor window
        private enum Tab
        {
            Dashboard,
            PoolDetails,
            PerformanceAnalysis,
            Settings
        }
        private Tab _currentTab = Tab.Dashboard;

        // UI state variables
        private Vector2 _scrollPosition;
        private string _selectedPoolId;
        private bool _isPlaying => EditorApplication.isPlaying;
        
        // Chart and statistics data
        private List<PoolStatSnapshot> _poolStatHistory = new List<PoolStatSnapshot>();
        private float _lastDataCollectionTime;
        private const float DATA_COLLECTION_INTERVAL = 0.5f; // Data collection frequency (0.5s)
        private const int MAX_HISTORY_POINTS = 120; // Keep 60s of history (with 0.5s interval)
        
        // Display settings
        private bool _showActiveObjects = true;
        private bool _showInactiveObjects = true;
        private int _historyTimeRange = 60; // Default display 60s
        private bool _autoScrollToBottom = true;

        /// <summary>
        /// Called when the window is enabled
        /// </summary>
        private void OnEnable()
        {
            // Register callback for editor play mode changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // Register update to refresh data at intervals
            EditorApplication.update += OnEditorUpdate;
            
            // Initialize data if in Play mode
            if (_isPlaying)
            {
                InitializeData();
            }
        }

        /// <summary>
        /// Called when the window is disabled
        /// </summary>
        private void OnDisable()
        {
            // Unregister callbacks
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            
            // Clear data when window is closed to avoid memory leaks
            ClearData();
        }

        /// <summary>
        /// Initialize data collection
        /// </summary>
        private void InitializeData()
        {
            // Initialize data at start or when entering play mode
            _poolStatHistory.Clear();
            _lastDataCollectionTime = (float)EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// Clear stored data
        /// </summary>
        private void ClearData()
        {
            // Clear stored data
            _poolStatHistory.Clear();
        }

        /// <summary>
        /// Handle play mode state changes
        /// </summary>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                InitializeData();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ClearData();
            }
            
            // Ensure window is repainted
            Repaint();
        }

        /// <summary>
        /// Called on editor update cycle
        /// </summary>
        private void OnEditorUpdate()
        {
            // Only collect data in play mode
            if (!_isPlaying) return;
            
            float currentTime = (float)EditorApplication.timeSinceStartup;
            if (currentTime - _lastDataCollectionTime >= DATA_COLLECTION_INTERVAL)
            {
                CollectPoolData();
                _lastDataCollectionTime = currentTime;
                
                // Only repaint when new data is available
                Repaint();
            }
        }

        /// <summary>
        /// Collect pool statistics from runtime
        /// </summary>
        private void CollectPoolData()
        {
            // TODO: Implement data collection from PoolManager runtime
            // Add a new snapshot to history
            
            // Limit history size to prevent memory leaks
            if (_poolStatHistory.Count > MAX_HISTORY_POINTS)
            {
                _poolStatHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Draw the editor UI
        /// </summary>
        private void OnGUI()
        {
            if (!_isPlaying)
            {
                DrawNotPlayingUI();
                return;
            }

            DrawToolbar();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            switch (_currentTab)
            {
                case Tab.Dashboard:
                    DrawDashboard();
                    break;
                case Tab.PoolDetails:
                    DrawPoolDetails();
                    break;
                case Tab.PerformanceAnalysis:
                    DrawPerformanceAnalysis();
                    break;
                case Tab.Settings:
                    DrawSettings();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Draw the toolbar with main tabs
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Toggle(_currentTab == Tab.Dashboard, "Dashboard", EditorStyles.toolbarButton))
                _currentTab = Tab.Dashboard;
                
            if (GUILayout.Toggle(_currentTab == Tab.PoolDetails, "Pool Details", EditorStyles.toolbarButton))
                _currentTab = Tab.PoolDetails;
                
            if (GUILayout.Toggle(_currentTab == Tab.PerformanceAnalysis, "Performance Analysis", EditorStyles.toolbarButton))
                _currentTab = Tab.PerformanceAnalysis;
                
            if (GUILayout.Toggle(_currentTab == Tab.Settings, "Settings", EditorStyles.toolbarButton))
                _currentTab = Tab.Settings;
                
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw UI for when editor is not in play mode
        /// </summary>
        private void DrawNotPlayingUI()
        {
            EditorGUILayout.HelpBox("Pool Manager Editor is only available in Play Mode.\nPlease enter Play Mode to view and analyze pool data.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Enter Play Mode", GUILayout.Height(30)))
            {
                EditorApplication.isPlaying = true;
            }
        }
        
        #region Dashboard
        /// <summary>
        /// Draw the Dashboard tab
        /// </summary>
        private void DrawDashboard()
        {
            // Title
            EditorGUILayout.LabelField("Object Pooling Dashboard", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // System-wide statistics
            DrawSystemStats();
            EditorGUILayout.Space();
            
            // Activity graph
            DrawActivityGraph();
            EditorGUILayout.Space();
            
            // List of pools
            DrawPoolsList();
        }
        
        /// <summary>
        /// Draw system-wide statistics
        /// </summary>
        private void DrawSystemStats()
        {
            // TODO: Implement system statistics display
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("System Statistics", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Pools: ", GUILayout.Width(120));
            EditorGUILayout.LabelField("0");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Active Objects: ", GUILayout.Width(120));
            EditorGUILayout.LabelField("0");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Inactive Objects: ", GUILayout.Width(120));
            EditorGUILayout.LabelField("0");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Memory Saved: ", GUILayout.Width(120));
            EditorGUILayout.LabelField("0 KB");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Get Operations: ", GUILayout.Width(120));
            EditorGUILayout.LabelField("0");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Return Operations: ", GUILayout.Width(120));
            EditorGUILayout.LabelField("0");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw activity graph
        /// </summary>
        private void DrawActivityGraph()
        {
            // TODO: Implement activity graph
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Activity Graph", EditorStyles.boldLabel);
            
            Rect graphRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
            
            GUI.Box(graphRect, "");
            if (_poolStatHistory.Count > 0)
            {
                // Placeholder for actual graph implementation
                GUI.Label(new Rect(graphRect.center.x - 50, graphRect.center.y - 10, 100, 20), "Graph placeholder");
            }
            else
            {
                GUI.Label(new Rect(graphRect.center.x - 75, graphRect.center.y - 10, 150, 20), "No data available");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw list of all pools
        /// </summary>
        private void DrawPoolsList()
        {
            // TODO: Implement pools list
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Pools", EditorStyles.boldLabel);
            
            // Headers
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Pool Name", EditorStyles.toolbarButton, GUILayout.Width(150));
            EditorGUILayout.LabelField("Type", EditorStyles.toolbarButton, GUILayout.Width(80));
            EditorGUILayout.LabelField("Active", EditorStyles.toolbarButton, GUILayout.Width(60));
            EditorGUILayout.LabelField("Inactive", EditorStyles.toolbarButton, GUILayout.Width(60));
            EditorGUILayout.LabelField("Total", EditorStyles.toolbarButton, GUILayout.Width(60));
            EditorGUILayout.LabelField("Efficiency", EditorStyles.toolbarButton, GUILayout.Width(100));
            EditorGUILayout.LabelField("Get Ops", EditorStyles.toolbarButton, GUILayout.Width(60));
            EditorGUILayout.LabelField("Return Ops", EditorStyles.toolbarButton, GUILayout.Width(80));
            EditorGUILayout.LabelField("", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();
            
            // Draw placeholder row for demonstration - Fixed version
            bool selected = _selectedPoolId == "example_pool";
            
            EditorGUILayout.BeginHorizontal(selected ? EditorStyles.helpBox : EditorStyles.label);
            
            // Toggle selection on click (simulating the toggle group behavior)
            bool newSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(20));
            if (newSelected != selected && newSelected)
            {
                _selectedPoolId = "example_pool";
                _currentTab = Tab.PoolDetails;
            }
            
            EditorGUILayout.LabelField("Example Pool", GUILayout.Width(130));
            EditorGUILayout.LabelField("Regular", GUILayout.Width(80));
            EditorGUILayout.LabelField("10", GUILayout.Width(60));
            EditorGUILayout.LabelField("20", GUILayout.Width(60));
            EditorGUILayout.LabelField("30", GUILayout.Width(60));
            EditorGUILayout.LabelField("75%", GUILayout.Width(100));
            EditorGUILayout.LabelField("50", GUILayout.Width(60));
            EditorGUILayout.LabelField("40", GUILayout.Width(80));
            if (GUILayout.Button("Details", GUILayout.Width(60)))
            {
                _selectedPoolId = "example_pool";
                _currentTab = Tab.PoolDetails;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        #endregion
        
        #region PoolDetails
        /// <summary>
        /// Draw the Pool Details tab
        /// </summary>
        private void DrawPoolDetails()
        {
            if (string.IsNullOrEmpty(_selectedPoolId))
            {
                EditorGUILayout.HelpBox("No pool selected. Please select a pool from the Dashboard.", MessageType.Info);
                if (GUILayout.Button("Back to Dashboard"))
                {
                    _currentTab = Tab.Dashboard;
                }
                return;
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Pool Details: {_selectedPoolId}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Back to Dashboard", GUILayout.Width(130)))
            {
                _currentTab = Tab.Dashboard;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Configuration panel
            DrawPoolConfiguration();
            EditorGUILayout.Space();
            
            // Usage statistics
            DrawPoolUsageStats();
            EditorGUILayout.Space();
            
            // Objects list
            DrawPoolObjectsList();
            EditorGUILayout.Space();
            
            // Control panel
            DrawPoolControlPanel();
        }
        
        /// <summary>
        /// Draw pool configuration details
        /// </summary>
        private void DrawPoolConfiguration()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            
            // Display configuration properties
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Initial Size:", GUILayout.Width(120));
            EditorGUILayout.LabelField("10");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max Size:", GUILayout.Width(120));
            EditorGUILayout.LabelField("30");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Allow Growth:", GUILayout.Width(120));
            EditorGUILayout.LabelField("True");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Auto Trimming:", GUILayout.Width(120));
            EditorGUILayout.LabelField("Enabled");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Trim Threshold:", GUILayout.Width(120));
            EditorGUILayout.LabelField("15");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Trim Age:", GUILayout.Width(120));
            EditorGUILayout.LabelField("30s");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw pool usage statistics
        /// </summary>
        private void DrawPoolUsageStats()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Usage Statistics", EditorStyles.boldLabel);
            
            // Draw usage graph
            Rect graphRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
            GUI.Box(graphRect, "");
            GUI.Label(new Rect(graphRect.center.x - 75, graphRect.center.y - 10, 150, 20), "Usage graph placeholder");
            
            // Stats below graph
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Average Usage Time:", GUILayout.Width(150));
            EditorGUILayout.LabelField("2.5s");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Peak Active Objects:", GUILayout.Width(150));
            EditorGUILayout.LabelField("15");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Total Get Operations:", GUILayout.Width(150));
            EditorGUILayout.LabelField("50");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw list of objects in the selected pool
        /// </summary>
        private void DrawPoolObjectsList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Objects", EditorStyles.boldLabel);
            
            // Filter options
            EditorGUILayout.BeginHorizontal();
            _showActiveObjects = EditorGUILayout.ToggleLeft("Show Active", _showActiveObjects, GUILayout.Width(100));
            _showInactiveObjects = EditorGUILayout.ToggleLeft("Show Inactive", _showInactiveObjects, GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // Headers
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("ID", EditorStyles.toolbarButton, GUILayout.Width(80));
            EditorGUILayout.LabelField("Status", EditorStyles.toolbarButton, GUILayout.Width(80));
            EditorGUILayout.LabelField("Inactive Time", EditorStyles.toolbarButton, GUILayout.Width(100));
            EditorGUILayout.LabelField("Position", EditorStyles.toolbarButton, GUILayout.Width(150));
            EditorGUILayout.LabelField("", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();
            
            // Objects list (placeholder)
            for (int i = 0; i < 5; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"#{i+1}", GUILayout.Width(80));
                EditorGUILayout.LabelField(i % 2 == 0 ? "Active" : "Inactive", GUILayout.Width(80));
                EditorGUILayout.LabelField(i % 2 == 0 ? "0s" : "10s", GUILayout.Width(100));
                EditorGUILayout.LabelField(i % 2 == 0 ? "(1.0, 2.0, 3.0)" : "---", GUILayout.Width(150));
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    // Select object in scene
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw control panel for selected pool
        /// </summary>
        private void DrawPoolControlPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Control Panel", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Prewarm Pool", GUILayout.Height(30)))
            {
                // Prewarm pool action
            }
            
            if (GUILayout.Button("Trim Pool Now", GUILayout.Height(30)))
            {
                // Trim pool action
            }
            
            if (GUILayout.Button("Clear Pool", GUILayout.Height(30)))
            {
                // Clear pool action
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        #endregion
        
        #region PerformanceAnalysis
        /// <summary>
        /// Draw the Performance Analysis tab
        /// </summary>
        private void DrawPerformanceAnalysis()
        {
            EditorGUILayout.LabelField("Performance Analysis", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Efficiency analysis
            DrawEfficiencyAnalysis();
            EditorGUILayout.Space();
            
            // Optimization suggestions
            DrawOptimizationSuggestions();
            EditorGUILayout.Space();
            
            // Warning panel
            DrawPotentialIssues();
        }
        
        /// <summary>
        /// Draw efficiency analysis section
        /// </summary>
        private void DrawEfficiencyAnalysis()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Efficiency Analysis", EditorStyles.boldLabel);
            
            // Efficiency indicator (placeholder)
            Rect progressRect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(progressRect, 0.75f, "System Efficiency: 75%");
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Most efficient pool:", GUILayout.Width(150));
            EditorGUILayout.LabelField("Example Pool (95%)");
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                _selectedPoolId = "example_pool";
                _currentTab = Tab.PoolDetails;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Least efficient pool:", GUILayout.Width(150));
            EditorGUILayout.LabelField("Another Pool (35%)");
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                _selectedPoolId = "another_pool";
                _currentTab = Tab.PoolDetails;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Memory saved:", GUILayout.Width(150));
            EditorGUILayout.LabelField("2.5 MB");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw optimization suggestions
        /// </summary>
        private void DrawOptimizationSuggestions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Optimization Suggestions", EditorStyles.boldLabel);
            
            // Placeholder suggestions
            EditorGUILayout.HelpBox("Consider increasing initial size of 'Example Pool' to improve performance during peak usage.", MessageType.Info);
            
            EditorGUILayout.HelpBox("'Another Pool' has an excessive max size. Consider reducing it to save memory.", MessageType.Info);
            
            EditorGUILayout.HelpBox("Enable trimming for 'Third Pool' to reclaim unused memory.", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw potential issues section
        /// </summary>
        private void DrawPotentialIssues()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Potential Issues", EditorStyles.boldLabel);
            
            // Placeholder issues
            EditorGUILayout.HelpBox("'Example Pool' reaches max size frequently. Consider increasing max size or investigating high usage pattern.", MessageType.Warning);
            
            EditorGUILayout.HelpBox("'Another Pool' objects are being kept active for too long (avg: 25s). Check return logic.", MessageType.Warning);
            
            EditorGUILayout.EndVertical();
        }
        #endregion
        
        #region Settings
        /// <summary>
        /// Draw the Settings tab
        /// </summary>
        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Display settings
            DrawDisplaySettings();
            EditorGUILayout.Space();
            
            // Data collection settings
            DrawDataCollectionSettings();
            EditorGUILayout.Space();
            
            // Interface settings
            DrawInterfaceSettings();
        }
        
        /// <summary>
        /// Draw display settings
        /// </summary>
        private void DrawDisplaySettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Display Settings", EditorStyles.boldLabel);
            
            _showActiveObjects = EditorGUILayout.Toggle("Show Active Objects", _showActiveObjects);
            _showInactiveObjects = EditorGUILayout.Toggle("Show Inactive Objects", _showInactiveObjects);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("History Time Range:");
            _historyTimeRange = EditorGUILayout.IntSlider(_historyTimeRange, 10, 120);
            EditorGUILayout.LabelField($"Showing {_historyTimeRange} seconds of history");
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw data collection settings
        /// </summary>
        private void DrawDataCollectionSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Data Collection Settings", EditorStyles.boldLabel);
            
            // These would eventually be actual settings that affect behavior
            EditorGUILayout.Slider("Collection Frequency", 0.5f, 0.1f, 2.0f);
            EditorGUILayout.IntSlider("Max History Points", 120, 60, 300);
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw interface settings
        /// </summary>
        private void DrawInterfaceSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Interface Settings", EditorStyles.boldLabel);
            
            _autoScrollToBottom = EditorGUILayout.Toggle("Auto-scroll to Bottom", _autoScrollToBottom);
            
            EditorGUILayout.EndVertical();
        }
        #endregion
        
        #region Utility Classes
        /// <summary>
        /// Class for storing a snapshot of pool statistics at a point in time
        /// </summary>
        [Serializable]
        private class PoolStatSnapshot
        {
            public float timestamp;
            public Dictionary<string, PoolStats> poolStats = new Dictionary<string, PoolStats>();
            public int totalActive;
            public int totalInactive;
            public int instantiateCount;
            public int getCount;
            public int returnCount;
        }
        
        /// <summary>
        /// Class for storing statistics about a single pool
        /// </summary>
        [Serializable]
        private class PoolStats
        {
            public string poolId;
            public string prefabName;
            public bool isUIPool;
            public int activeCount;
            public int inactiveCount;
            public int maxSize;
            public int instantiateCount;
            public int getCount;
            public int returnCount;
            public float efficiency; // 0-1 efficiency value
            public List<ObjectStats> objectStats; // Details about each object
        }
        
        /// <summary>
        /// Class for storing statistics about a single pooled object
        /// </summary>
        [Serializable]
        private class ObjectStats
        {
            public int instanceId;
            public bool isActive;
            public float inactiveTime; // Time spent inactive
            public Vector3 position; // Current position
        }
        #endregion
    }
}