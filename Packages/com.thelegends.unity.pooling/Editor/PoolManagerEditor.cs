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
        
        // Search functionality
        private string _poolSearchString;
        private Vector2 _poolListScrollPosition;

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
            // Search for the PoolManager type in loaded assemblies
            Type poolManagerType = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes();
                    poolManagerType = types.FirstOrDefault(t => t.FullName == "com.thelegends.unity.pooling.PoolManager");
                    if (poolManagerType != null)
                        break;
                }
                catch (ReflectionTypeLoadException) 
                {
                    // Skip assemblies that can't be loaded
                    continue;
                }
            }
                
            if (poolManagerType == null)
            {
                Debug.LogWarning("[PoolManagerEditor] Could not find PoolManager type. Make sure the assembly is loaded.");
                return;
            }
            
            // Get the instance property via reflection
            var instanceProperty = poolManagerType.GetProperty("Instance", 
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                
            if (instanceProperty == null)
            {
                Debug.LogWarning("[PoolManagerEditor] Could not find Instance property on PoolManager.");
                return;
            }
            
            var poolManager = instanceProperty.GetValue(null);
            if (poolManager == null)
            {
                // No PoolManager instance exists yet, probably because no pools have been created
                // Create an empty snapshot to avoid errors
                var emptySnapshot = new PoolStatSnapshot
                {
                    timestamp = (float)EditorApplication.timeSinceStartup
                };
                _poolStatHistory.Add(emptySnapshot);
                return;
            }
            
            // Create a new snapshot
            var snapshot = new PoolStatSnapshot
            {
                timestamp = (float)EditorApplication.timeSinceStartup,
                poolStats = new Dictionary<string, PoolStats>(),
                totalActive = 0,
                totalInactive = 0,
                getCount = 0,
                returnCount = 0,
                instantiateCount = 0
            };
            
            // Get all pools via reflection
            var getAllPoolsMethod = poolManagerType.GetMethod("GetAllPools", 
                BindingFlags.Public | BindingFlags.Instance);
                
            // If GetAllPools doesn't exist, try to access the pools field/property directly
            if (getAllPoolsMethod == null)
            {
                // First try to get _pools as a field
                var poolsField = poolManagerType.GetField("_pools", BindingFlags.NonPublic | BindingFlags.Instance);
                if (poolsField != null)
                {
                    var poolsDict = poolsField.GetValue(poolManager);
                    if (poolsDict != null)
                    {
                        // Get the Values property of the dictionary
                        var valuesProperty = poolsDict.GetType().GetProperty("Values");
                        if (valuesProperty != null)
                        {
                            ProcessPools(valuesProperty.GetValue(poolsDict) as System.Collections.IEnumerable, snapshot);
                        }
                    }
                }
                else
                {
                    // If field doesn't exist, try to get Pools as a property
                    var poolsProperty = poolManagerType.GetProperty("Pools", BindingFlags.Public | BindingFlags.Instance);
                    if (poolsProperty != null)
                    {
                        var poolsDict = poolsProperty.GetValue(poolManager);
                        if (poolsDict != null)
                        {
                            // Get the Values property of the dictionary
                            var valuesProperty = poolsDict.GetType().GetProperty("Values");
                            if (valuesProperty != null)
                            {
                                ProcessPools(valuesProperty.GetValue(poolsDict) as System.Collections.IEnumerable, snapshot);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[PoolManagerEditor] Could not find pools in PoolManager.");
                        _poolStatHistory.Add(snapshot); // Add empty snapshot
                    }
                }
            }
            else
            {
                var allPools = getAllPoolsMethod.Invoke(poolManager, null) as System.Collections.IEnumerable;
                if (allPools != null)
                {
                    ProcessPools(allPools, snapshot);
                }
                else
                {
                    _poolStatHistory.Add(snapshot); // Add empty snapshot
                }
            }
            
            // Add the snapshot to history
            _poolStatHistory.Add(snapshot);
            
            // Limit history size to prevent memory leaks
            if (_poolStatHistory.Count > MAX_HISTORY_POINTS)
            {
                _poolStatHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Process each pool to gather statistics
        /// </summary>
        private void ProcessPools(System.Collections.IEnumerable allPools, PoolStatSnapshot snapshot)
        {
            if (allPools == null) return;
            
            // Process each pool
            foreach (var pool in allPools)
            {
                // Get pool information via reflection
                Type poolType = pool.GetType();
                
                // Get key/ID for the pool
                string poolId = "unknown";
                var keyProperty = poolType.GetProperty("Key");
                if (keyProperty != null)
                {
                    var keyValue = keyProperty.GetValue(pool);
                    if (keyValue != null)
                    {
                        poolId = keyValue.ToString();
                    }
                }
                else
                {
                    var keyField = poolType.GetField("_key", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (keyField != null)
                    {
                        var keyValue = keyField.GetValue(pool);
                        if (keyValue != null)
                        {
                            poolId = keyValue.ToString();
                        }
                    }
                }
                
                // Check if this is a UI pool
                bool isUIPool = poolType.Name.Contains("UIPool");
                
                // Get prefab name
                string prefabName = "Unknown Prefab";
                var prefabField = poolType.GetField("_prefab", BindingFlags.NonPublic | BindingFlags.Instance);
                if (prefabField != null)
                {
                    var prefab = prefabField.GetValue(pool) as UnityEngine.Object;
                    if (prefab != null)
                    {
                        prefabName = prefab.name;
                    }
                }
                
                // Get key statistics
                int activeCount = GetValueFromPropertyOrField<int>(poolType, pool, "ActiveCount", "_activeCount");
                int inactiveCount = GetValueFromPropertyOrField<int>(poolType, pool, "InactiveCount", "_inactiveCount");
                int maxSize = GetValueFromPropertyOrField<int>(poolType, pool, "MaxSize", "_maxSize");
                int instantiateCount = GetValueFromPropertyOrField<int>(poolType, pool, "InstantiateCount", "_instantiateCount");
                int getCount = GetValueFromPropertyOrField<int>(poolType, pool, "GetCount", "_getCount");
                int returnCount = GetValueFromPropertyOrField<int>(poolType, pool, "ReturnCount", "_returnCount");
                
                // Calculate efficiency (reuse ratio)
                float efficiency = 0;
                if (getCount > 0)
                {
                    efficiency = Mathf.Max(0, 1 - (float)instantiateCount / getCount);
                }
                
                // Get all object details if available
                List<ObjectStats> objectDetails = new List<ObjectStats>();
                
                // Try to access the _allObjects and _inactiveObjects collections
                var allObjectsField = poolType.GetField("_allObjects", BindingFlags.NonPublic | BindingFlags.Instance);
                var inactiveObjectsField = poolType.GetField("_inactiveObjects", BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (allObjectsField != null && inactiveObjectsField != null)
                {
                    var allObjects = allObjectsField.GetValue(pool) as System.Collections.IList;
                    var inactiveObjects = inactiveObjectsField.GetValue(pool) as System.Collections.ICollection;
                    
                    if (allObjects != null && inactiveObjects != null)
                    {
                        // Get inactive object IDs into a HashSet for fast lookup
                        HashSet<int> inactiveIds = new HashSet<int>();
                        foreach (GameObject inactiveObj in inactiveObjects)
                        {
                            if (inactiveObj != null)
                            {
                                inactiveIds.Add(inactiveObj.GetInstanceID());
                            }
                        }
                        
                        // Process all objects
                        foreach (GameObject obj in allObjects)
                        {
                            if (obj != null)
                            {
                                int instanceId = obj.GetInstanceID();
                                bool isActive = !inactiveIds.Contains(instanceId);
                                
                                // Calculate inactive time if we can find the _lastAccessTime field on the object
                                float inactiveTime = 0;
                                if (!isActive)
                                {
                                    var pooledObjComponent = obj.GetComponent("PooledObject");
                                    if (pooledObjComponent != null)
                                    {
                                        var lastAccessField = pooledObjComponent.GetType().GetField("_lastAccessTime", 
                                            BindingFlags.NonPublic | BindingFlags.Instance);
                                        
                                        if (lastAccessField != null)
                                        {
                                            float lastAccessTime = (float)lastAccessField.GetValue(pooledObjComponent);
                                            inactiveTime = Time.time - lastAccessTime;
                                        }
                                    }
                                }
                                
                                // Add object stats to the list
                                objectDetails.Add(new ObjectStats
                                {
                                    instanceId = instanceId,
                                    isActive = isActive,
                                    inactiveTime = inactiveTime,
                                    position = obj.transform.position
                                });
                            }
                        }
                    }
                }
                
                // Create pool stats object
                var poolStats = new PoolStats
                {
                    poolId = poolId,
                    prefabName = prefabName,
                    isUIPool = isUIPool,
                    activeCount = activeCount,
                    inactiveCount = inactiveCount,
                    maxSize = maxSize,
                    instantiateCount = instantiateCount,
                    getCount = getCount,
                    returnCount = returnCount,
                    efficiency = efficiency,
                    objectStats = objectDetails
                };
                
                // Add to snapshot
                snapshot.poolStats[poolId] = poolStats;
                
                // Add to totals
                snapshot.totalActive += activeCount;
                snapshot.totalInactive += inactiveCount;
                snapshot.instantiateCount += instantiateCount;
                snapshot.getCount += getCount;
                snapshot.returnCount += returnCount;
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
            // Dashboard header with refresh button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Object Pooling Dashboard", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button(new GUIContent("Refresh", "Force refresh pool data"), GUILayout.Width(80)))
            {
                CollectPoolData();
                Repaint();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // System-wide statistics section
            DrawSystemStats();
            EditorGUILayout.Space(10);
            
            // Activity graphs section
            DrawActivityGraphs();
            EditorGUILayout.Space(10);
            
            // List of all pools
            DrawPoolsList();
        }
        
        /// <summary>
        /// Draw system-wide statistics
        /// </summary>
        private void DrawSystemStats()
        {
            // Get the latest statistics snapshot, or use default values if no data
            PoolStatSnapshot latestStats = _poolStatHistory.Count > 0 
                ? _poolStatHistory[_poolStatHistory.Count - 1] 
                : new PoolStatSnapshot();
                
            // Calculate total pools
            int totalPools = latestStats.poolStats?.Count ?? 0;
            
            // Memory savings estimation (rough estimate based on GameObject overhead)
            // Average GameObject overhead is about 3KB
            long memorySavedBytes = latestStats.totalInactive * 3 * 1024;
            string memorySavedText = FormatMemorySize(memorySavedBytes);
            
            // Statistics box
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Title with legend
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("System Statistics", EditorStyles.boldLabel);
            
            // Usage legend
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("■", new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.2f, 0.7f, 0.2f) } }, GUILayout.Width(15));
            EditorGUILayout.LabelField("Active", GUILayout.Width(50));
            EditorGUILayout.LabelField("■", new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.7f, 0.2f, 0.2f) } }, GUILayout.Width(15));
            EditorGUILayout.LabelField("Inactive", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Main stats in two columns
            EditorGUILayout.BeginHorizontal();
            
            // Left column
            EditorGUILayout.BeginVertical();
            DrawStatField("Total Pools", totalPools.ToString());
            DrawStatField("Active Objects", latestStats.totalActive.ToString());
            DrawStatField("Inactive Objects", latestStats.totalInactive.ToString());
            EditorGUILayout.EndVertical();
            
            // Right column
            EditorGUILayout.BeginVertical();
            DrawStatField("Memory Saved", memorySavedText);
            DrawStatField("Get Operations", latestStats.getCount.ToString());
            DrawStatField("Return Operations", latestStats.returnCount.ToString());
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            // Object distribution bar
            if (latestStats.totalActive + latestStats.totalInactive > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Object Distribution", EditorStyles.boldLabel);
                
                Rect barRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                float totalObjects = latestStats.totalActive + latestStats.totalInactive;
                float activeRatio = latestStats.totalActive / totalObjects;
                
                // Draw background
                EditorGUI.DrawRect(barRect, new Color(0.3f, 0.3f, 0.3f));
                
                // Draw active part (green)
                Rect activeRect = new Rect(barRect.x, barRect.y, barRect.width * activeRatio, barRect.height);
                EditorGUI.DrawRect(activeRect, new Color(0.2f, 0.7f, 0.2f));
                
                // Draw inactive part (red)
                Rect inactiveRect = new Rect(barRect.x + barRect.width * activeRatio, barRect.y, 
                                         barRect.width * (1 - activeRatio), barRect.height);
                EditorGUI.DrawRect(inactiveRect, new Color(0.7f, 0.2f, 0.2f));
                
                // Draw percentages
                string activePercent = $"{activeRatio * 100:0}%";
                string inactivePercent = $"{(1 - activeRatio) * 100:0}%";
                
                GUIStyle centeredLabel = new GUIStyle(EditorStyles.boldLabel) 
                { 
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white } 
                };
                
                // Only draw text if there's enough space
                if (activeRect.width > 40)
                    GUI.Label(activeRect, activePercent, centeredLabel);
                    
                if (inactiveRect.width > 40)
                    GUI.Label(inactiveRect, inactivePercent, centeredLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw activity graphs with time-based visualization
        /// </summary>
        private enum GraphView
        {
            Objects,
            Operations,
            PoolSize
        }

        private GraphView _currentGraphView = GraphView.Objects;

        private void DrawActivityGraphs()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Tab selection for different graph views
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Activity Monitor", EditorStyles.boldLabel, GUILayout.Width(120));
            
            GUILayout.FlexibleSpace();
            
            // Graph view selection buttons
            GUIStyle tabButtonStyle = new GUIStyle(EditorStyles.toolbarButton) { fontStyle = FontStyle.Bold };
            
            if (GUILayout.Toggle(_currentGraphView == GraphView.Objects, "Objects", tabButtonStyle))
                _currentGraphView = GraphView.Objects;
                
            if (GUILayout.Toggle(_currentGraphView == GraphView.Operations, "Operations", tabButtonStyle))
                _currentGraphView = GraphView.Operations;
                
            if (GUILayout.Toggle(_currentGraphView == GraphView.PoolSize, "Pool Size", tabButtonStyle))
                _currentGraphView = GraphView.PoolSize;
                
            EditorGUILayout.EndHorizontal();
            
            // Draw the selected graph
            Rect graphRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
            
            if (_poolStatHistory.Count > 1)
            {
                // Background
                EditorGUI.DrawRect(graphRect, new Color(0.2f, 0.2f, 0.2f));
                DrawGraphGrid(graphRect);
                
                switch (_currentGraphView)
                {
                    case GraphView.Objects:
                        DrawObjectsGraph(graphRect);
                        break;
                    case GraphView.Operations:
                        DrawOperationsGraph(graphRect);
                        break;
                    case GraphView.PoolSize:
                        DrawPoolSizeGraph(graphRect);
                        break;
                }
                
                // Draw timestamp labels
                DrawTimeLabels(graphRect);
            }
            else
            {
                EditorGUI.DrawRect(graphRect, new Color(0.2f, 0.2f, 0.2f));
                GUI.Label(new Rect(graphRect.center.x - 75, graphRect.center.y - 10, 150, 20), 
                          "Collecting data...", 
                          new GUIStyle(EditorStyles.label) { normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter });
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Draw grid lines for the graph background
        /// </summary>
        private void DrawGraphGrid(Rect rect)
        {
            // Draw vertical grid lines
            int verticalLines = 6;
            float step = rect.width / verticalLines;
            
            for (int i = 1; i < verticalLines; i++)
            {
                float x = rect.x + step * i;
                EditorGUI.DrawRect(new Rect(x, rect.y, 1, rect.height), new Color(0.3f, 0.3f, 0.3f));
            }
            
            // Draw horizontal grid lines
            int horizontalLines = 4;
            step = rect.height / horizontalLines;
            
            for (int i = 1; i < horizontalLines; i++)
            {
                float y = rect.y + step * i;
                EditorGUI.DrawRect(new Rect(rect.x, y, rect.width, 1), new Color(0.3f, 0.3f, 0.3f));
            }
        }
        
        /// <summary>
        /// Draw list of all pools with summary information
        /// </summary>
        private void DrawPoolsList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header with search box
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Pool Inventory", EditorStyles.boldLabel, GUILayout.Width(100));
            
            // Search functionality
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            _poolSearchString = EditorGUILayout.TextField(_poolSearchString ?? "", GUILayout.Width(150));
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                _poolSearchString = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Column headers
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("", GUILayout.Width(28)); // Selection column
            EditorGUILayout.LabelField("Pool Name", EditorStyles.toolbarButton, GUILayout.Width(150));
            EditorGUILayout.LabelField("Type", EditorStyles.toolbarButton, GUILayout.Width(70));
            EditorGUILayout.LabelField("Active", EditorStyles.toolbarButton, GUILayout.Width(50));
            EditorGUILayout.LabelField("Inactive", EditorStyles.toolbarButton, GUILayout.Width(50));
            EditorGUILayout.LabelField("Total", EditorStyles.toolbarButton, GUILayout.Width(50));
            EditorGUILayout.LabelField("Efficiency", EditorStyles.toolbarButton, GUILayout.Width(110));
            EditorGUILayout.LabelField("Get Ops", EditorStyles.toolbarButton, GUILayout.Width(60));
            EditorGUILayout.LabelField("Return Ops", EditorStyles.toolbarButton, GUILayout.Width(70));
            EditorGUILayout.LabelField("", EditorStyles.toolbarButton); // For action buttons column
            EditorGUILayout.EndHorizontal();
            
            // Get the most recent snapshot
            PoolStatSnapshot latestSnapshot = _poolStatHistory.Count > 0 
                ? _poolStatHistory[_poolStatHistory.Count - 1] 
                : new PoolStatSnapshot();
            
            // Draw pool list items
            if (latestSnapshot.poolStats != null && latestSnapshot.poolStats.Count > 0)
            {
                // Use a scrollable list for many pools
                _poolListScrollPosition = EditorGUILayout.BeginScrollView(_poolListScrollPosition, 
                    GUILayout.ExpandHeight(true), GUILayout.MaxHeight(300));
                
                // Filter by search if needed
                var filteredPools = string.IsNullOrEmpty(_poolSearchString) 
                    ? latestSnapshot.poolStats 
                    : latestSnapshot.poolStats.Where(p => 
                        p.Key.IndexOf(_poolSearchString, StringComparison.OrdinalIgnoreCase) >= 0 || 
                        p.Value.prefabName.IndexOf(_poolSearchString, StringComparison.OrdinalIgnoreCase) >= 0);
                
                // Draw each pool row
                foreach (var poolEntry in filteredPools)
                {
                    string poolId = poolEntry.Key;
                    PoolStats stats = poolEntry.Value;
                    bool selected = _selectedPoolId == poolId;
                    
                    EditorGUILayout.BeginHorizontal(selected ? 
                        new GUIStyle { normal = { background = EditorGUIUtility.whiteTexture } } : new GUIStyle());
                    
                    // Apply tint to selected row
                    if (selected)
                    {
                        GUI.color = new Color(0.8f, 0.9f, 1f);
                    }
                    
                    // Toggle selection
                    bool newSelected = EditorGUILayout.Toggle(selected, GUILayout.Width(20));
                    if (newSelected != selected)
                    {
                        _selectedPoolId = newSelected ? poolId : null;
                        
                        // Switch to details tab if selecting a pool
                        if (newSelected)
                        {
                            _currentTab = Tab.PoolDetails;
                        }
                    }
                    
                    // Pool name - make it clickable
                    if (GUILayout.Button(stats.prefabName, new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft }, GUILayout.Width(150)))
                    {
                        _selectedPoolId = poolId;
                        _currentTab = Tab.PoolDetails;
                    }
                    
                    // Pool type
                    EditorGUILayout.LabelField(stats.isUIPool ? "UI" : "Regular", GUILayout.Width(70));
                    
                    // Active/Inactive/Total counts
                    EditorGUILayout.LabelField(stats.activeCount.ToString(), GUILayout.Width(50));
                    EditorGUILayout.LabelField(stats.inactiveCount.ToString(), GUILayout.Width(50));
                    EditorGUILayout.LabelField((stats.activeCount + stats.inactiveCount).ToString(), GUILayout.Width(50));
                    
                    // Efficiency bar
                    Rect barRect = GUILayoutUtility.GetRect(100, 16, GUILayout.Width(100));
                    EditorGUI.DrawRect(barRect, new Color(0.3f, 0.3f, 0.3f));
                    
                    // Calculate efficiency
                    float efficiency = stats.efficiency;
                    
                    // Color based on efficiency
                    Color barColor = Color.Lerp(Color.red, Color.green, efficiency);
                    
                    // Draw efficiency bar
                    EditorGUI.DrawRect(
                        new Rect(barRect.x, barRect.y, barRect.width * efficiency, barRect.height),
                        barColor
                    );
                    
                    // Draw efficiency percentage text
                    string efficiencyText = $"{efficiency * 100:0}%";
                    EditorGUI.LabelField(
                        barRect, 
                        efficiencyText,
                        new GUIStyle(EditorStyles.miniBoldLabel) { 
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = Color.white }
                        }
                    );
                    
                    // Operation counts
                    EditorGUILayout.LabelField(stats.getCount.ToString(), GUILayout.Width(60));
                    EditorGUILayout.LabelField(stats.returnCount.ToString(), GUILayout.Width(70));
                    
                    // Action buttons
                    if (GUILayout.Button("Details", GUILayout.Width(60)))
                    {
                        _selectedPoolId = poolId;
                        _currentTab = Tab.PoolDetails;
                    }
                    
                    // Reset color
                    GUI.color = Color.white;
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                // No pools available
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("No pools have been created yet.", MessageType.Info);
                EditorGUILayout.Space(10);
            }
            
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
        
        #region Helper Methods
        /// <summary>
        /// Helper method to format memory size with appropriate units
        /// </summary>
        private string FormatMemorySize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
                
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F2} KB";
                
            if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024f * 1024f):F2} MB";
                
            return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
        }

        /// <summary>
        /// Helper method to draw a labeled statistics field
        /// </summary>
        private void DrawStatField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label + ":", GUILayout.Width(120));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Helper method to get a value from either a property or a field via reflection
        /// </summary>
        private T GetValueFromPropertyOrField<T>(Type type, object obj, string propertyName, string fieldName)
        {
            // Check for property first
            var property = type.GetProperty(propertyName);
            if (property != null)
            {
                try
                {
                    return (T)Convert.ChangeType(property.GetValue(obj), typeof(T));
                }
                catch
                {
                    return default(T);
                }
            }
            
            // Then check for field
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                try
                {
                    return (T)Convert.ChangeType(field.GetValue(obj), typeof(T));
                }
                catch
                {
                    return default(T);
                }
            }
            
            // Return default if not found
            return default(T);
        }
        #endregion

        /// <summary>
        /// Draw time labels along the x-axis
        /// </summary>
        private void DrawTimeLabels(Rect rect)
        {
            if (_poolStatHistory.Count < 2)
                return;
                    
            float lastTimestamp = _poolStatHistory[_poolStatHistory.Count - 1].timestamp;
            float firstTimestamp = Mathf.Max(lastTimestamp - _historyTimeRange, _poolStatHistory[0].timestamp);
            float timeSpan = lastTimestamp - firstTimestamp;
            
            if (timeSpan <= 0)
                return;
                
            // Get style for labels
            GUIStyle timeLabel = new GUIStyle(EditorStyles.miniLabel) 
            { 
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) },
                alignment = TextAnchor.UpperCenter
            };
            
            // Draw time labels
            int labelCount = 6;
            float step = rect.width / labelCount;
            
            for (int i = 0; i <= labelCount; i++)
            {
                float x = rect.x + step * i;
                float time = firstTimestamp + (timeSpan * i / labelCount);
                float relativeTime = lastTimestamp - time;
                
                // Format label
                string label = relativeTime < 60 ? 
                    $"{relativeTime:0}s" : 
                    $"{relativeTime / 60:0}m {relativeTime % 60:00}s";
                    
                if (i == labelCount)
                    label = "Now";
                else if (i == 0)
                    label = $"-{_historyTimeRange}s";
                
                // Draw label
                GUI.Label(new Rect(x - 20, rect.y + rect.height + 2, 40, 15), label, timeLabel);
            }
        }

        /// <summary>
        /// Draw active/inactive objects graph
        /// </summary>
        private void DrawObjectsGraph(Rect rect)
        {
            // Get visible time range for graph
            float lastTimestamp = _poolStatHistory.Count > 0 ? _poolStatHistory[_poolStatHistory.Count - 1].timestamp : 0;
            float firstTimestamp = Mathf.Max(lastTimestamp - _historyTimeRange, _poolStatHistory.Count > 0 ? _poolStatHistory[0].timestamp : 0);
            
            // Find max value for scaling
            int maxValue = 1; // Avoid division by zero
            
            foreach (var snapshot in _poolStatHistory)
            {
                if (snapshot.timestamp < firstTimestamp)
                    continue;
                    
                int total = snapshot.totalActive + snapshot.totalInactive;
                if (total > maxValue)
                    maxValue = total;
            }
            
            // Add headroom for better visualization
            maxValue = (int)(maxValue * 1.1f); 
            
            // Get points for active objects (green)
            List<Vector2> activePoints = GetGraphPoints(rect, firstTimestamp, lastTimestamp, maxValue, 
                snapshot => snapshot.totalActive);
                
            // Get points for total objects (active + inactive)
            List<Vector2> totalPoints = GetGraphPoints(rect, firstTimestamp, lastTimestamp, maxValue, 
                snapshot => snapshot.totalActive + snapshot.totalInactive);
            
            // Get points for inactive objects (red) by converting from total and active
            List<Vector2> inactivePoints = new List<Vector2>();
            if (totalPoints.Count > 0 && activePoints.Count > 0)
            {
                for (int i = 0; i < totalPoints.Count && i < activePoints.Count; i++)
                {
                    // Use the same x coordinate, but calculate y as the difference between total and active
                    float x = totalPoints[i].x;
                    float y = activePoints[i].y - (totalPoints[i].y - activePoints[i].y);
                    inactivePoints.Add(new Vector2(x, y));
                }
            }
            
            // Draw the lines with appropriate thickness
            DrawGraphLine(totalPoints, new Color(0.7f, 0.2f, 0.2f), 2.0f); // Total line (red)
            DrawGraphLine(activePoints, new Color(0.2f, 0.7f, 0.2f), 2.0f); // Active line (green)
            
            // Draw legend
            DrawGraphLegend(rect, new Dictionary<string, Color> {
                { "Active Objects", new Color(0.2f, 0.7f, 0.2f) },
                { "Total Pool Size", new Color(0.7f, 0.2f, 0.2f) }
            }, maxValue);
        }

        /// <summary>
        /// Draw operations graph showing get/return/instantiate operations
        /// </summary>
        private void DrawOperationsGraph(Rect rect)
        {
            // Get visible time range for graph
            float lastTimestamp = _poolStatHistory.Count > 0 ? _poolStatHistory[_poolStatHistory.Count - 1].timestamp : 0;
            float firstTimestamp = Mathf.Max(lastTimestamp - _historyTimeRange, _poolStatHistory.Count > 0 ? _poolStatHistory[0].timestamp : 0);
            
            // For operations, we need the delta between snapshots
            Dictionary<float, int> getOps = new Dictionary<float, int>();
            Dictionary<float, int> returnOps = new Dictionary<float, int>();
            Dictionary<float, int> instantiateOps = new Dictionary<float, int>();
            
            int prevGet = 0;
            int prevReturn = 0;
            int prevInstantiate = 0;
            
            float maxValue = 1; // Default min scale
            
            // Prepare points for our line graphs
            List<Vector2> getPoints = new List<Vector2>();
            List<Vector2> returnPoints = new List<Vector2>();
            List<Vector2> instantiatePoints = new List<Vector2>();
            
            // Add first point at left edge with zeros
            Vector2 leftEdgePoint = new Vector2(rect.x, rect.y + rect.height);
            getPoints.Add(leftEdgePoint);
            returnPoints.Add(leftEdgePoint);
            instantiatePoints.Add(leftEdgePoint);
            
            foreach (var snapshot in _poolStatHistory)
            {
                if (snapshot.timestamp < firstTimestamp)
                    continue;
                
                // Calculate delta operations
                int getDelta = snapshot.getCount - prevGet;
                int returnDelta = snapshot.returnCount - prevReturn;
                int instantiateDelta = snapshot.instantiateCount - prevInstantiate;
                
                // Keep track of max value for scaling
                maxValue = Mathf.Max(maxValue, getDelta, returnDelta, instantiateDelta);
                
                // Store values for visualization
                getOps[snapshot.timestamp] = getDelta;
                returnOps[snapshot.timestamp] = returnDelta;
                instantiateOps[snapshot.timestamp] = instantiateDelta;
                
                // Update previous values
                prevGet = snapshot.getCount;
                prevReturn = snapshot.returnCount;
                prevInstantiate = snapshot.instantiateCount;
            }
            
            // Add headroom to max value for better visualization
            maxValue *= 1.2f;
            
            // Convert operation deltas to graph points
            float timeSpan = lastTimestamp - firstTimestamp;
            if (timeSpan > 0)
            {
                foreach (var timestamp in getOps.Keys.OrderBy(t => t))
                {
                    float x = rect.x + ((timestamp - firstTimestamp) / timeSpan) * rect.width;
                    
                    // Get operations (blue)
                    if (getOps.TryGetValue(timestamp, out int getValue))
                    {
                        float normalizedValue = getValue / maxValue;
                        float y = rect.y + rect.height - (normalizedValue * rect.height);
                        getPoints.Add(new Vector2(x, y));
                    }
                    
                    // Return operations (orange)
                    if (returnOps.TryGetValue(timestamp, out int returnValue))
                    {
                        float normalizedValue = returnValue / maxValue;
                        float y = rect.y + rect.height - (normalizedValue * rect.height);
                        returnPoints.Add(new Vector2(x, y));
                    }
                    
                    // Instantiate operations (purple)
                    if (instantiateOps.TryGetValue(timestamp, out int instantiateValue))
                    {
                        float normalizedValue = instantiateValue / maxValue;
                        float y = rect.y + rect.height - (normalizedValue * rect.height);
                        instantiatePoints.Add(new Vector2(x, y));
                    }
                }
            }
            
            // Add right edge point
            Vector2 rightEdgePoint = new Vector2(rect.x + rect.width, rect.y + rect.height);
            getPoints.Add(rightEdgePoint);
            returnPoints.Add(rightEdgePoint);
            instantiatePoints.Add(rightEdgePoint);
            
            // Draw each line with appropriate colors and thicknesses
            Color getColor = new Color(0.3f, 0.7f, 0.9f); // Blue
            Color returnColor = new Color(0.9f, 0.7f, 0.3f); // Orange
            Color instantiateColor = new Color(0.9f, 0.3f, 0.9f); // Purple
            
            // Draw the lines
            DrawGraphLine(getPoints, getColor, 2.0f);
            DrawGraphLine(returnPoints, returnColor, 2.0f);
            DrawGraphLine(instantiatePoints, instantiateColor, 2.0f);
            
            // Draw legend
            DrawGraphLegend(rect, new Dictionary<string, Color> {
                { "Get Operations", getColor },
                { "Return Operations", returnColor },
                { "New Instantiations", instantiateColor }
            }, (int)maxValue);
        }

        /// <summary>
        /// Draw pool size graph showing total pool capacity vs objects in use
        /// </summary>
        private void DrawPoolSizeGraph(Rect rect)
        {
            // Get visible time range for graph
            float lastTimestamp = _poolStatHistory.Count > 0 ? _poolStatHistory[_poolStatHistory.Count - 1].timestamp : 0;
            float firstTimestamp = Mathf.Max(lastTimestamp - _historyTimeRange, _poolStatHistory.Count > 0 ? _poolStatHistory[0].timestamp : 0);
            
            // Find max value for scaling
            int maxValue = 1; // Avoid division by zero
            
            foreach (var snapshot in _poolStatHistory)
            {
                if (snapshot.timestamp < firstTimestamp)
                    continue;
                    
                int total = snapshot.totalActive + snapshot.totalInactive;
                // Estimated capacity (total + 20%)
                int estimatedCapacity = (int)(total * 1.2f);
                if (estimatedCapacity > maxValue)
                    maxValue = estimatedCapacity;
            }
            
            // Add some headroom for better visualization
            maxValue = (int)(maxValue * 1.1f);
            
            // Get points for active objects (green)
            List<Vector2> activePoints = GetGraphPoints(rect, firstTimestamp, lastTimestamp, maxValue, 
                snapshot => snapshot.totalActive);
                
            // Get points for total objects (blue) - active + inactive
            List<Vector2> totalPoints = GetGraphPoints(rect, firstTimestamp, lastTimestamp, maxValue, 
                snapshot => snapshot.totalActive + snapshot.totalInactive);
            
            // Get points for capacity (yellow) - estimated max size
            List<Vector2> capacityPoints = GetGraphPoints(rect, firstTimestamp, lastTimestamp, maxValue, 
                snapshot => (int)((snapshot.totalActive + snapshot.totalInactive) * 1.2f));
            
            // Draw the lines with appropriate thickness
            DrawGraphLine(capacityPoints, new Color(0.9f, 0.9f, 0.3f), 1.5f); // Capacity line (yellow)
            DrawGraphLine(totalPoints, new Color(0.5f, 0.5f, 0.9f), 2.0f); // Total line (blue)
            DrawGraphLine(activePoints, new Color(0.3f, 0.9f, 0.3f), 2.0f); // Active line (green)
            
            // Draw legend
            DrawGraphLegend(rect, new Dictionary<string, Color> {
                { "Active Objects", new Color(0.3f, 0.9f, 0.3f) },
                { "Total Objects", new Color(0.5f, 0.5f, 0.9f) },
                { "Maximum Capacity", new Color(0.9f, 0.9f, 0.3f) }
            }, maxValue);
        }

        /// <summary>
        /// Helper method to generate graph points from pool statistics
        /// </summary>
        private List<Vector2> GetGraphPoints(
            Rect rect, float firstTimestamp, float lastTimestamp, 
            float maxValue, Func<PoolStatSnapshot, float> valueSelector)
        {
            List<Vector2> points = new List<Vector2>();
            float timeSpan = lastTimestamp - firstTimestamp;
            
            if (timeSpan <= 0)
                return points;
                
            // Start with a point at the left edge
            bool foundFirstPoint = false;
            
            foreach (var snapshot in _poolStatHistory)
            {
                if (snapshot.timestamp < firstTimestamp)
                    continue;
                    
                // Calculate position
                float x = rect.x + ((snapshot.timestamp - firstTimestamp) / timeSpan) * rect.width;
                float normalizedValue = valueSelector(snapshot) / maxValue;
                float y = rect.y + rect.height - (normalizedValue * rect.height);
                
                // Add left edge point if this is the first visible point
                if (!foundFirstPoint)
                {
                    points.Add(new Vector2(rect.x, y));
                    foundFirstPoint = true;
                }
                
                points.Add(new Vector2(x, y));
            }
            
            // Add right edge point
            Vector2 rightEdgePoint = new Vector2(rect.x + rect.width, rect.y + rect.height);
            points.Add(rightEdgePoint);
            
            return points;
        }

        /// <summary>
        /// Draw a filled area under a graph line
        /// </summary>
        private void DrawGraphArea(List<Vector2> points, Color color)
        {
            if (points.Count < 2) return;
            
            // Create a polygon by adding points at the bottom
            List<Vector2> polygon = new List<Vector2>(points);
            polygon.Add(new Vector2(points[points.Count - 1].x, points[0].y + 30)); // Bottom right
            polygon.Add(new Vector2(points[0].x, points[0].y + 30)); // Bottom left
            
            // Convert Vector2[] to Vector3[] for Handles.DrawAAConvexPolygon
            Vector3[] polygonV3 = polygon.Select(v => new Vector3(v.x, v.y, 0)).ToArray();
            
            // Draw filled area
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawAAConvexPolygon(polygonV3);
            Handles.EndGUI();
        }

        /// <summary>
        /// Draw a line connecting graph points
        /// </summary>
        private void DrawGraphLine(List<Vector2> points, Color color, float thickness = 1.0f)
        {
            if (points.Count < 2) return;
            
            // Convert Vector2[] to Vector3[] for Handles.DrawAAPolyLine
            Vector3[] pointsV3 = points.Select(v => new Vector3(v.x, v.y, 0)).ToArray();
            
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawAAPolyLine(thickness, pointsV3);
            Handles.EndGUI();
        }

        /// <summary>
        /// Draw legend for the graph
        /// </summary>
        private void DrawGraphLegend(Rect graphRect, Dictionary<string, Color> items, float maxValue)
        {
            float boxWidth = 120f;
            float boxHeight = items.Count * 20f + 40f;
            float boxX = graphRect.x + 10;
            float boxY = graphRect.y + 10;
            
            // Draw semi-transparent background
            EditorGUI.DrawRect(
                new Rect(boxX, boxY, boxWidth, boxHeight), 
                new Color(0.1f, 0.1f, 0.1f, 0.7f)
            );
            
            // Draw max value
            GUI.Label(
                new Rect(boxX + 10, boxY + 10, boxWidth - 20, 20), 
                $"Max: {maxValue:F0}",
                new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white } }
            );
            
            // Draw items with color swatches
            int i = 0;
            foreach (var item in items)
            {
                float y = boxY + 30 + (i * 20);
                
                // Draw color swatch
                EditorGUI.DrawRect(
                    new Rect(boxX + 10, y + 3, 14, 14),
                    item.Value
                );
                
                // Draw label
                GUI.Label(
                    new Rect(boxX + 30, y, boxWidth - 40, 20),
                    item.Key,
                    new GUIStyle(EditorStyles.label) { normal = { textColor = Color.white } }
                );
                
                i++;
            }
        }

        /// <summary>
        /// Draw operation bars for the Operations graph
        /// </summary>
        private void DrawOperationBars(
            Rect rect, float firstTimestamp, float lastTimestamp, float maxValue,
            Dictionary<float, int> getOps, Dictionary<float, int> returnOps, 
            Dictionary<float, int> instantiateOps)
        {
            float timeSpan = lastTimestamp - firstTimestamp;
            if (timeSpan <= 0) return;
            
            // Calculate bar sizing
            int barCount = getOps.Count;
            if (barCount == 0) return;
            
            float barWidth = Mathf.Min(10f, rect.width / barCount);
            float barSpacing = (rect.width - (barWidth * barCount)) / (barCount + 1);
            
            // Colors
            Color getColor = new Color(0.3f, 0.7f, 0.9f, 0.8f);
            Color returnColor = new Color(0.9f, 0.7f, 0.3f, 0.8f);
            Color instantiateColor = new Color(0.9f, 0.3f, 0.9f, 0.8f);
            
            // Draw bars for each timestamp
            int index = 0;
            foreach (var timestamp in getOps.Keys.OrderBy(t => t))
            {
                float x = rect.x + barSpacing + (index * (barWidth + barSpacing));
                
                // Get operation bar
                if (getOps.TryGetValue(timestamp, out int getValue))
                {
                    float height = (getValue / maxValue) * rect.height;
                    Rect barRect = new Rect(x, rect.y + rect.height - height, barWidth / 3, height);
                    EditorGUI.DrawRect(barRect, getColor);
                }
                
                // Return operation bar
                if (returnOps.TryGetValue(timestamp, out int returnValue))
                {
                    float height = (returnValue / maxValue) * rect.height;
                    Rect barRect = new Rect(x + barWidth/3, rect.y + rect.height - height, barWidth / 3, height);
                    EditorGUI.DrawRect(barRect, returnColor);
                }
                
                // Instantiate operation bar
                if (instantiateOps.TryGetValue(timestamp, out int instantiateValue))
                {
                    float height = (instantiateValue / maxValue) * rect.height;
                    Rect barRect = new Rect(x + 2*barWidth/3, rect.y + rect.height - height, barWidth / 3, height);
                    EditorGUI.DrawRect(barRect, instantiateColor);
                }
                
                index++;
            }
        }
    }
}