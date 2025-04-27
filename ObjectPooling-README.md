# Hướng Dẫn Sử Dụng Thư Viện Object Pooling 7.0

## Mục lục

- [Giới thiệu](#giới-thiệu)
  - [Lợi ích chính](#lợi-ích-chính)
  - [Khi nào nên sử dụng Object Pooling?](#khi-nào-nên-sử-dụng-object-pooling)
- [Cài đặt](#cài-đặt)
  - [Thông qua Unity Package Manager (UPM)](#thông-qua-unity-package-manager-upm)
  - [Thủ công](#thủ-công)
  - [Yêu cầu](#yêu-cầu)
  - [Cấu hình ban đầu](#cấu-hình-ban-đầu)
- [Cấu trúc và Thành phần cốt lõi](#cấu-trúc-và-thành-phần-cốt-lõi)
- [Hướng dẫn sử dụng API chi tiết](#hướng-dẫn-sử-dụng-api-chi-tiết)
  - [1. Khởi tạo và Cấu hình PoolManager](#1-khởi-tạo-và-cấu-hình-poolmanager)
  - [2. Tạo và Sử dụng Pool Cơ bản (Non-UI)](#2-tạo-và-sử-dụng-pool-cơ-bản-non-ui)
  - [3. Tạo và Sử dụng UI Pool](#3-tạo-và-sử-dụng-ui-pool)
  - [4. Triển khai Interface IPoolable](#4-triển-khai-interface-ipoolable)
  - [5. Sử dụng AutoReturnToPool Component](#5-sử-dụng-autoreturntopool-component)
  - [6. Tối ưu hóa với Pool Prewarming](#6-tối-ưu-hóa-với-pool-prewarming)
  - [7. Xử lý lỗi Addressables](#7-xử-lý-lỗi-addressables)
  - [8. Tối ưu hóa với Trimming](#8-tối-ưu-hóa-với-trimming)
  - [9. Quản lý quá trình chuyển Scene](#9-quản-lý-quá-trình-chuyển-scene)
- [Công cụ PoolManagerEditor](#công-cụ-poolmanagereditor)
  - [1. Truy cập PoolManagerEditor](#1-truy-cập-poolmanagereditor)
  - [2. Tổng quan về Dashboard](#2-tổng-quan-về-dashboard)
  - [3. Phân tích chi tiết Pool](#3-phân-tích-chi-tiết-pool)
  - [4. Phân tích Hiệu suất Thông minh](#4-phân-tích-hiệu-suất-thông-minh)
  - [5. Control Panel](#5-control-panel)
  - [6. Lưu ý về Hiệu năng Editor](#6-lưu-ý-về-hiệu-năng-editor)
- [Ví dụ ứng dụng thực tế](#ví-dụ-ứng-dụng-thực-tế)
  - [Ví dụ 1: Hệ thống bắn đạn (Weapon System)](#ví-dụ-1-hệ-thống-bắn-đạn-weapon-system)
  - [Ví dụ 2: Infinite Scrolling UI List với UI Pooling](#ví-dụ-2-infinite-scrolling-ui-list-với-ui-pooling)
  - [Ví dụ 3: Particle System Manager với Auto-Trimming](#ví-dụ-3-particle-system-manager-với-auto-trimming)
- [Các lưu ý quan trọng và Best Practices](#các-lưu-ý-quan-trọng-và-best-practices)
  - [1. Hiệu năng và Bộ nhớ](#1-hiệu-năng-và-bộ-nhớ)
  - [2. Addressables và Memory Management](#2-addressables-và-memory-management)
  - [3. UI Pooling](#3-ui-pooling)
  - [4. Thread Safety](#4-thread-safety)
  - [5. Debugging và Troubleshooting](#5-debugging-và-troubleshooting)

## Giới thiệu

Object Pooling là một kỹ thuật tối ưu hóa hiệu năng quan trọng trong phát triển game, đặc biệt là trên nền tảng Unity. Thư viện Object Pooling 7.0 cung cấp giải pháp toàn diện, hiệu năng cao và dễ sử dụng cho việc quản lý vòng đời của các đối tượng game, từ prefab thông thường đến UI phức tạp, với sự tích hợp đặc biệt với hệ thống Addressables của Unity.

### Lợi ích chính

- **Hiệu năng vượt trội**: Giảm thiểu GC Allocation, tối ưu hóa CPU, tốc độ Get/Return cực nhanh
- **Tích hợp Addressables**: Quản lý vòng đời asset thông minh, ngăn ngừa memory leak
- **Hỗ trợ UI chuyên biệt**: Xử lý RectTransform, Canvas, Layout Groups, Graphic Raycaster
- **Xử lý lỗi toàn diện**: 4 chiến lược xử lý lỗi Addressables, khả năng phục hồi cao
- **Quản lý bộ nhớ thông minh**: Cơ chế trimming tự động cấu hình được
- **Công cụ Editor mạnh mẽ**: Dashboard trực quan, biểu đồ thời gian thực, phân tích thông minh
- **API dễ sử dụng và an toàn**: Generic type-safe, không cần key khi return
- **Linh hoạt và mở rộng**: Tùy chỉnh thông qua IPoolable, các cấu hình đa dạng
- **Tuân thủ SOLID**: Thiết kế mô-đun, dễ bảo trì và mở rộng

### Khi nào nên sử dụng Object Pooling?

Object Pooling đặc biệt hữu ích trong các trường hợp:

- **Đối tượng tạo/hủy thường xuyên**: Đạn bắn, hiệu ứng, kẻ địch, vật phẩm
- **Màn hình UI phức tạp**: Danh sách cuộn (scrolling list), inventory items, chat messages
- **Tối ưu hóa hiệu năng trên thiết bị di động**
- **Quản lý tài nguyên Addressables hiệu quả**
- **Giảm thiểu hiện tượng giật (stuttering) do GC**

## Cài đặt

### Thông qua Unity Package Manager (UPM)

1. Mở Unity Package Manager (Window > Package Manager)
2. Nhấp vào nút "+" và chọn "Add package from git URL..."
3. Nhập URL của repository: `https://github.com/your-username/Unity-Object-Pooling-7.0.git`
4. Nhấp "Add"

### Thủ công

1. Tải xuống phiên bản mới nhất từ [GitHub Releases](https://github.com/your-username/Unity-Object-Pooling-7.0/releases)
2. Giải nén và đặt vào thư mục `Packages` trong project Unity của bạn

### Yêu cầu

- Unity 2020.3 trở lên
- Package Addressables (com.unity.addressables)

### Cấu hình ban đầu

Để kích hoạt đầy đủ chức năng của thư viện, bạn cần:

1. Thêm symbol `OBJECT_POOLING` vào Player Settings:
   - Mở Project Settings (Edit > Project Settings)
   - Chọn Player
   - Trong mục "Scripting Define Symbols", thêm `OBJECT_POOLING`

## Cấu trúc và Thành phần cốt lõi

Thư viện Object Pooling 7.0 bao gồm các thành phần chính sau:

### 1. PoolManager

PoolManager là singleton/service chính quản lý tất cả các pool. Nó cung cấp API chính để tạo pool mới, lấy/trả đối tượng, và quản lý vòng đời của pool.

### 2. ObjectPool<TKey>

Lớp cơ sở cho việc pooling, quản lý các instance của một prefab cụ thể. Mỗi pool có các cấu hình riêng và có thể được tùy chỉnh với các chiến lược xử lý lỗi và trimming.

### 3. UIPool<TKey>

Lớp chuyên biệt kế thừa từ ObjectPool để xử lý các đối tượng UI, với các tối ưu hóa đặc thù cho UGUI (RectTransform, Canvas, LayoutElement, GraphicRaycaster).

### 4. PooledObject

Component được gắn tự động vào mỗi instance từ pool, lưu thông tin về pool gốc và cung cấp phương thức ReturnToPool() thuận tiện.

### 5. IPoolable

Interface tùy chọn cho các đối tượng muốn phản ứng với các sự kiện get/return từ pool.

### 6. Các cấu hình

- **PoolConfig**: Cấu hình cơ bản cho pool (kích thước ban đầu, phát triển, kích thước tối đa).
- **PoolTrimmingConfig**: Cấu hình cho cơ chế tự động dọn dẹp pool không sử dụng.
- **AddressableErrorConfig**: Cấu hình xử lý lỗi khi làm việc với Addressables.

### 7. PoolManagerEditor

Công cụ Editor mạnh mẽ để giám sát, phân tích và tối ưu hóa việc sử dụng pool trong thời gian thực. 

## Hướng dẫn sử dụng API chi tiết

### 1. Khởi tạo và Cấu hình PoolManager

PoolManager được thiết kế theo mẫu Singleton, nhưng cũng có thể được sử dụng như một service (DI). Dưới đây là cách cơ bản để khởi tạo PoolManager:

```csharp
// Sử dụng instance sẵn có (Singleton)
PoolManager poolManager = PoolManager.Instance;

// Hoặc tạo instance riêng (Service)
PoolManager customPoolManager = new PoolManager();
```

#### Cấu hình Error Handling và Trimming mặc định

```csharp
// Cấu hình xử lý lỗi Addressables mặc định
AddressableErrorConfig errorConfig = new AddressableErrorConfig
{
    errorHandlingStrategy = AddressableErrorHandling.LogAndReturnNull,
    maxRetries = 3,
    retryDelay = 1.0f,
    fallbackPrefab = null, // Có thể đặt một prefab dự phòng
    onAddressableLoadError = (exception, key) => Debug.LogError($"Lỗi load asset '{key}': {exception.Message}")
};
poolManager.DefaultAddressableErrorConfig = errorConfig;

// Cấu hình trimming mặc định
PoolTrimmingConfig trimmingConfig = new PoolTrimmingConfig
{
    enableAutoTrim = true,
    trimCheckInterval = 30.0f,        // Kiểm tra sau mỗi 30 giây
    inactiveTimeThreshold = 60.0f,    // Đối tượng không hoạt động sau 60 giây sẽ được xem xét
    targetRetainRatio = 0.5f,         // Giữ lại 50% số đối tượng không hoạt động
    minimumRetainCount = 5            // Luôn giữ ít nhất 5 đối tượng
};
poolManager.DefaultTrimmingConfig = trimmingConfig;

// Bật/tắt debug logging
poolManager.IsDebugLogEnabled = true; // Bật logging
```

### 2. Tạo và Sử dụng Pool Cơ bản (Non-UI)

#### Tạo pool với prefab trực tiếp

```csharp
// Tạo một pool với prefab được nạp sẵn
public GameObject enemyPrefab;  // Prefab được gán trong Inspector
private string enemyKey = "Enemy";

// Cấu hình tùy chỉnh cho pool này
PoolConfig poolConfig = new PoolConfig
{
    initialSize = 20,     // Số lượng khởi tạo ban đầu
    allowGrowth = true,   // Cho phép tăng kích thước khi cần
    maxSize = 50          // Kích thước tối đa
};

// Tạo pool đồng bộ với prefab được nạp sẵn
await poolManager.CreatePoolAsync(enemyKey, enemyPrefab, poolConfig);
```

#### Tạo pool với Addressables

```csharp
// Tạo pool với Addressable asset
string bulletAddressableKey = "Bullet";  // Addressable key của prefab trong Addressable system

// Tạo pool với Addressable asset
await poolManager.CreatePoolAsync(bulletAddressableKey, bulletAddressableKey, new PoolConfig 
{
    initialSize = 50,
    allowGrowth = true,
    maxSize = 200
});
```

#### Lấy và trả đối tượng từ pool

```csharp
// Lấy đối tượng từ pool theo key
GameObject enemy = await poolManager.GetAsync(enemyKey);
if (enemy != null)
{
    // Thiết lập vị trí, rotation, etc.
    enemy.transform.position = spawnPosition;
    enemy.transform.rotation = Quaternion.identity;
    
    // Khởi tạo các thuộc tính khác
    // ...
}

// Lấy đối tượng với kiểu xác định (type-safe)
EnemyController enemyController = await poolManager.Get<EnemyController>(enemyKey);
if (enemyController != null)
{
    enemyController.Initialize(spawnData);
    enemyController.Activate();
}

// Trả đối tượng về pool (không cần key)
// Phương thức 1: Sử dụng PoolManager
poolManager.Return(enemy);

// Phương thức 2: Sử dụng component PooledObject gắn trên đối tượng
enemy.GetComponent<PooledObject>().ReturnToPool();

// Phương thức 3: Sử dụng trong code của component trên đối tượng pool
// this.GetComponent<PooledObject>().ReturnToPool();
```

### 3. Tạo và Sử dụng UI Pool

UI Pool có các tối ưu hóa đặc biệt cho các đối tượng UI, xử lý RectTransform, Canvas, Graphic Raycaster và Layout Element.

```csharp
// Tạo UI Pool
string inventoryItemKey = "InventoryItem";
GameObject inventoryItemPrefab; // Prefab UI được gán trong Inspector

// Tạo UI Pool với prefab
await poolManager.CreateUIPoolAsync(inventoryItemKey, inventoryItemPrefab, new PoolConfig
{
    initialSize = 10,
    allowGrowth = true,
    maxSize = 50
});

// Lấy đối tượng UI với parent transform
RectTransform containerTransform; // RectTransform của container UI

// Lấy đối tượng UI từ pool
GameObject uiItem = await poolManager.GetUIAsync(inventoryItemKey, containerTransform);

// Hoặc lấy với component xác định
InventoryItemUI itemUI = await poolManager.GetUI<InventoryItemUI>(inventoryItemKey, containerTransform);
if (itemUI != null)
{
    itemUI.SetData(itemData);
}

// Trả đối tượng UI về pool
poolManager.Return(uiItem);
// hoặc
uiItem.GetComponent<PooledObject>().ReturnToPool();
```

### 4. Triển khai Interface IPoolable

IPoolable cho phép đối tượng phản ứng với các sự kiện get/return từ pool, rất hữu ích để khởi tạo/reset trạng thái.

```csharp
public class Enemy : MonoBehaviour, IPoolable
{
    private Rigidbody rb;
    private Renderer renderer;
    private AudioSource audioSource;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        renderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
    }
    
    public void OnGetFromPool()
    {
        // Kích hoạt đối tượng và thiết lập trạng thái ban đầu
        gameObject.SetActive(true);
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        renderer.enabled = true;
        audioSource.Play();
        
        // Khởi tạo các trạng thái khác
        health = maxHealth;
        isVulnerable = true;
    }
    
    public void OnReturnToPool()
    {
        // Reset trạng thái và vô hiệu hóa đối tượng
        gameObject.SetActive(false);
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        audioSource.Stop();
        
        // Hủy các subscribe event để tránh memory leak
        UnsubscribeFromEvents();
    }
}
```

### 5. Sử dụng AutoReturnToPool Component

Component `AutoReturnToPool` giúp tự động trả đối tượng về pool sau một khoảng thời gian hoặc khi một sự kiện xảy ra.

```csharp
// Lấy đối tượng từ pool
GameObject bullet = await poolManager.GetAsync("Bullet");

// Thêm component AutoReturnToPool nếu chưa có
AutoReturnToPool autoReturn = bullet.GetComponent<AutoReturnToPool>() ?? bullet.AddComponent<AutoReturnToPool>();

// Cấu hình
autoReturn.ReturnAfterSeconds = 5.0f;             // Trả về sau 5 giây
autoReturn.ReturnOnDisable = true;                // Trả về khi bị disable
autoReturn.ReturnOnCollision = true;              // Trả về khi va chạm
autoReturn.ReturnOnCollisionWithTag = "Enemy";    // Chỉ trả về khi va chạm với tag "Enemy"
autoReturn.PlayEffectBeforeReturn = true;         // Phát hiệu ứng trước khi trả về
autoReturn.effectPrefab = hitEffectPrefab;        // Prefab hiệu ứng
```

### 6. Tối ưu hóa với Pool Prewarming

Prewarm pool giúp khởi tạo trước các đối tượng để tránh lag khi cần sử dụng:

```csharp
// Prewarm một pool đã tạo
await poolManager.PrewarmAsync(enemyKey, 20); // Khởi tạo trước 20 instance

// Prewarm nhiều pool cùng lúc
Dictionary<string, int> poolsToPrewarm = new Dictionary<string, int>
{
    { "Enemy", 20 },
    { "Bullet", 50 },
    { "ExplosionEffect", 10 }
};
await poolManager.PrewarmMultipleAsync(poolsToPrewarm);

// Sử dụng PoolPreloader trong scene
PoolPreloader preloader = gameObject.AddComponent<PoolPreloader>();
preloader.poolsToPrewarm = new PoolPreloader.PrewarmInfo[]
{
    new PoolPreloader.PrewarmInfo { poolKey = "Enemy", count = 20 },
    new PoolPreloader.PrewarmInfo { poolKey = "Bullet", count = 50 }
};
preloader.prewarmOnStart = true;
```

### 7. Xử lý lỗi Addressables

Thư viện cung cấp 4 chiến lược xử lý lỗi Addressables:

#### LogAndReturnNull

```csharp
// Cấu hình xử lý lỗi cho một pool cụ thể
await poolManager.CreatePoolAsync("RareEnemy", "RareEnemy_AddressableKey", 
    new PoolConfig { initialSize = 5 },
    new AddressableErrorConfig 
    { 
        errorHandlingStrategy = AddressableErrorHandling.LogAndReturnNull,
        onAddressableLoadError = (ex, key) => 
        {
            Debug.LogError($"Không thể tải RareEnemy: {ex.Message}");
            AnalyticsManager.LogEvent("AssetLoadError", key);
        }
    }
);
```

#### ThrowException

```csharp
// Cấu hình để ném ngoại lệ khi lỗi
var errorConfig = new AddressableErrorConfig
{
    errorHandlingStrategy = AddressableErrorHandling.ThrowException
};

try 
{
    GameObject criticalObject = await poolManager.GetAsync("CriticalGameObject");
    // Xử lý đối tượng
}
catch (AddressableLoadException ex)
{
    // Xử lý lỗi
    UIManager.ShowErrorDialog($"Lỗi tải asset quan trọng: {ex.Message}");
    SceneManager.LoadScene("ErrorScene");
}
```

#### ReturnPlaceholder

```csharp
// Sử dụng prefab placeholder khi lỗi
var placeholderConfig = new AddressableErrorConfig
{
    errorHandlingStrategy = AddressableErrorHandling.ReturnPlaceholder,
    fallbackPrefab = missingAssetPrefab // Prefab được sử dụng khi gặp lỗi
};

await poolManager.CreatePoolAsync("Character", "Character_AddressableKey", 
    new PoolConfig { initialSize = 1 },
    placeholderConfig
);
```

#### RetryWithTimeout

```csharp
// Tự động thử lại khi lỗi
var retryConfig = new AddressableErrorConfig
{
    errorHandlingStrategy = AddressableErrorHandling.RetryWithTimeout,
    maxRetries = 3,         // Số lần thử lại tối đa
    retryDelay = 2.0f,      // Đợi 2 giây giữa các lần thử
    onAddressableLoadError = (ex, key) => 
    {
        Debug.LogWarning($"Đang thử lại tải asset {key}. Lỗi: {ex.Message}");
        NetworkManager.CheckConnection();
    }
};

await poolManager.CreatePoolAsync("NetworkDependent", "Network_Asset_Key", 
    new PoolConfig { initialSize = 1 },
    retryConfig
);
```

### 8. Tối ưu hóa với Trimming

Trimming giúp giải phóng bộ nhớ bằng cách giảm kích thước pool khi không cần thiết:

```csharp
// Cấu hình trimming cho một pool cụ thể
var aggressiveTrimmingConfig = new PoolTrimmingConfig
{
    enableAutoTrim = true,
    trimCheckInterval = 15.0f,        // Kiểm tra thường xuyên hơn (15s)
    inactiveTimeThreshold = 30.0f,    // Đối tượng không sử dụng sau 30s
    targetRetainRatio = 0.2f,         // Chỉ giữ lại 20%
    minimumRetainCount = 2            // Luôn giữ ít nhất 2 đối tượng
};

await poolManager.CreatePoolAsync("TemporaryEffect", effectPrefab, 
    new PoolConfig { initialSize = 20, maxSize = 50 },
    null, // Sử dụng cấu hình error mặc định
    aggressiveTrimmingConfig // Cấu hình trimming tùy chỉnh
);

// Trim thủ công khi cần
poolManager.TrimPool("TemporaryEffect");

// Trim tất cả các pool
poolManager.TrimExcessPools();
```

### 9. Quản lý quá trình chuyển Scene

Thư viện tự động xử lý việc chuyển scene, nhưng bạn cũng có thể kiểm soát thủ công:

```csharp
// Xóa một pool cụ thể
poolManager.ClearPool("LevelSpecificEffect");

// Xóa tất cả các pool
poolManager.ClearAllPools();

// Đăng ký xử lý sự kiện chuyển scene
UnityEngine.SceneManagement.SceneManager.sceneUnloaded += (scene) => 
{
    if (scene.name == "BattleArena")
    {
        // Xóa các pool chỉ sử dụng trong scene này
        poolManager.ClearPool("BattleEffect");
        poolManager.ClearPool("EnemyWave");
    }
};
```

## Công cụ PoolManagerEditor

PoolManagerEditor là công cụ mạnh mẽ giúp bạn giám sát, phân tích và tối ưu hóa việc sử dụng pool trong thời gian thực. Công cụ này chỉ hoạt động trong Editor và cung cấp nhiều tính năng hữu ích.

### 1. Truy cập PoolManagerEditor

```csharp
// Mở cửa sổ PoolManagerEditor
// Từ menu: Window > Object Pooling > Pool Manager
[MenuItem("Window/Object Pooling/Pool Manager")]
private static void ShowWindow()
{
    EditorWindow.GetWindow<PoolManagerEditor>("Pool Manager");
}
```

### 2. Tổng quan về Dashboard

Dashboard hiển thị tổng quan về tất cả các pool trong hệ thống:

- **Thống kê tổng quát**:
  - Tổng số pool
  - Tổng số đối tượng đang hoạt động/không hoạt động
  - Tổng bộ nhớ được tiết kiệm (ước tính)
  - Số lần Instantiate/Get/Return
  
- **Danh sách Pool**: Hiển thị tất cả các pool với:
  - Tên pool
  - Loại pool (Regular/UI)
  - Kích thước hiện tại
  - Số đối tượng đang hoạt động
  - Hiệu suất sử dụng (%)
  - Thời gian truy cập gần nhất

- **Biểu đồ thời gian thực**: Hiển thị biểu đồ về:
  - Số lượng đối tượng hoạt động theo thời gian
  - Số lượng Get/Return operation theo thời gian
  - Số lượng Instantiate operation theo thời gian

### 3. Phân tích chi tiết Pool

Khi chọn một pool cụ thể, bạn có thể xem thông tin chi tiết:

- **Thống kê Pool**:
  - Cấu hình chi tiết (initialSize, maxSize, allowGrowth)
  - Cấu hình trimming
  - Cấu hình xử lý lỗi Addressables
  - Biểu đồ phân phối đối tượng (active/inactive)
  
- **Chi tiết đối tượng**:
  - Danh sách đối tượng trong pool
  - Trạng thái (active/inactive)
  - Thời gian không hoạt động
  - Vị trí hiện tại
  - Thời gian sử dụng trung bình

### 4. Phân tích Hiệu suất Thông minh

PoolManagerEditor cung cấp phân tích thông minh về hiệu suất của pool thông qua phương thức `GetPoolEfficiencyAnalysis()`:

```csharp
// Vùng hiển thị phân tích hiệu suất
EditorGUILayout.BeginVertical(EditorStyles.helpBox);
EditorGUILayout.LabelField("Phân tích hiệu suất", EditorStyles.boldLabel);

var analysis = PoolManager.Instance.GetPoolEfficiencyAnalysis(selectedPoolKey);
foreach (var insight in analysis.insights)
{
    EditorGUILayout.HelpBox(insight.message, 
        insight.severity == PoolEfficiencyInsight.Severity.Warning 
            ? MessageType.Warning 
            : MessageType.Info);
}

// Hiển thị các đề xuất tối ưu hóa
if (analysis.optimizationSuggestions.Count > 0)
{
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Đề xuất tối ưu hóa", EditorStyles.boldLabel);
    
    foreach (var suggestion in analysis.optimizationSuggestions)
    {
        EditorGUILayout.HelpBox(suggestion, MessageType.Info);
        
        // Nút áp dụng đề xuất tự động (nếu có)
        if (suggestion.hasAutomaticFix && GUILayout.Button("Áp dụng"))
        {
            suggestion.ApplyFix();
        }
    }
}
EditorGUILayout.EndVertical();
```

Phân tích hiệu suất bao gồm:

- **Pool Size Analysis**: Phát hiện pool quá lớn hoặc quá nhỏ
- **Growth Pattern Analysis**: Phân tích mẫu tăng trưởng và sử dụng
- **Access Frequency Analysis**: Tần suất sử dụng pool
- **Memory Usage Analysis**: Phân tích việc sử dụng bộ nhớ
- **Trimming Efficiency Analysis**: Hiệu quả của cấu hình trimming

### 5. Control Panel

Control Panel cho phép bạn điều chỉnh pool trong thời gian chạy:

```csharp
// Khu vực control panel
EditorGUILayout.BeginVertical(EditorStyles.helpBox);
EditorGUILayout.LabelField("Control Panel", EditorStyles.boldLabel);

// Nút điều khiển
if (GUILayout.Button("Prewarm Pool"))
{
    PoolManager.Instance.PrewarmAsync(selectedPoolKey, 10);
}

if (GUILayout.Button("Trim Pool Now"))
{
    PoolManager.Instance.TrimPool(selectedPoolKey);
}

if (GUILayout.Button("Clear Pool"))
{
    if (EditorUtility.DisplayDialog("Clear Pool", 
        "Bạn có chắc chắn muốn xóa toàn bộ pool này?", "Có", "Không"))
    {
        PoolManager.Instance.ClearPool(selectedPoolKey);
    }
}

// Điều chỉnh cấu hình
EditorGUI.BeginChangeCheck();
var pool = PoolManager.Instance.GetPoolInfo(selectedPoolKey);
var newMaxSize = EditorGUILayout.IntField("Max Size", pool.config.maxSize);
if (EditorGUI.EndChangeCheck())
{
    PoolManager.Instance.UpdatePoolMaxSize(selectedPoolKey, newMaxSize);
}

EditorGUILayout.EndVertical();
```

### 6. Lưu ý về Hiệu năng Editor

PoolManagerEditor được thiết kế với hiệu năng cao trong mind:

- Tần suất làm mới dữ liệu được giới hạn (0.5-1 giây)
- Số lượng điểm dữ liệu lịch sử được giới hạn để tránh tràn bộ nhớ
- Chỉ repaint khi cần thiết
- Tối ưu hóa truy cập dữ liệu runtime

```csharp
// Cập nhật dữ liệu với tần suất giới hạn
private void Update()
{
    _timeSinceLastUpdate += Time.deltaTime;
    if (_timeSinceLastUpdate >= _updateInterval)
    {
        _timeSinceLastUpdate = 0f;
        UpdatePoolData();
        Repaint();
    }
}

// Giới hạn số lượng điểm dữ liệu lịch sử
private void AddDataPoint(float activeCount, float inactiveCount)
{
    _activeCountHistory.Add(activeCount);
    _inactiveCountHistory.Add(inactiveCount);
    
    // Giới hạn kích thước danh sách
    if (_activeCountHistory.Count > _maxHistorySize)
    {
        _activeCountHistory.RemoveAt(0);
        _inactiveCountHistory.RemoveAt(0);
    }
}

// Xóa dữ liệu lịch sử khi đóng cửa sổ
private void OnDestroy()
{
    _activeCountHistory.Clear();
    _inactiveCountHistory.Clear();
    _instantiateEvents.Clear();
    
    // Hủy đăng ký sự kiện
    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
}
```

## Ví dụ ứng dụng thực tế

Dưới đây là một số ví dụ ứng dụng thực tế của thư viện Object Pooling 7.0 trong các tình huống game phổ biến.

### Ví dụ 1: Hệ thống bắn đạn (Weapon System)

Một ví dụ điển hình về việc sử dụng object pooling là hệ thống vũ khí bắn đạn với tần suất cao.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WeaponSystem : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private string bulletPoolKey = "Bullet";
    [SerializeField] private string muzzleFlashPoolKey = "MuzzleFlash";
    [SerializeField] private string impactEffectPoolKey = "ImpactEffect";
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private Transform firePoint;
    
    [Header("Pooling Settings")]
    [SerializeField] private int bulletPoolSize = 50;
    [SerializeField] private int effectPoolSize = 10;
    
    private PoolManager _poolManager;
    private float _nextFireTime;
    private bool _isInitialized = false;
    
    private async void Start()
    {
        _poolManager = PoolManager.Instance;
        await InitializePoolsAsync();
    }
    
    private async Task InitializePoolsAsync()
    {
        // Cấu hình pool cho đạn
        PoolConfig bulletConfig = new PoolConfig
        {
            initialSize = bulletPoolSize,
            allowGrowth = true,
            maxSize = bulletPoolSize * 2
        };
        
        // Cấu hình pool cho hiệu ứng
        PoolConfig effectConfig = new PoolConfig
        {
            initialSize = effectPoolSize,
            allowGrowth = true,
            maxSize = effectPoolSize * 2
        };
        
        // Cấu hình trimming cho hiệu ứng (giải phóng nhanh hơn)
        PoolTrimmingConfig effectTrimmingConfig = new PoolTrimmingConfig
        {
            enableAutoTrim = true,
            trimCheckInterval = 10f,
            inactiveTimeThreshold = 20f,
            targetRetainRatio = 0.3f,
            minimumRetainCount = 3
        };
        
        // Tạo các pool
        await _poolManager.CreatePoolAsync(bulletPoolKey, bulletPoolKey, bulletConfig);
        await _poolManager.CreatePoolAsync(muzzleFlashPoolKey, muzzleFlashPoolKey, effectConfig, null, effectTrimmingConfig);
        await _poolManager.CreatePoolAsync(impactEffectPoolKey, impactEffectPoolKey, effectConfig, null, effectTrimmingConfig);
        
        _isInitialized = true;
        Debug.Log("Weapon system pools initialized successfully!");
    }
    
    private void Update()
    {
        if (!_isInitialized) return;
        
        // Kiểm tra input và fire rate
        if (Input.GetMouseButton(0) && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + fireRate;
            FireBullet();
        }
    }
    
    private async void FireBullet()
    {
        // Hiển thị hiệu ứng nòng súng
        var muzzleFlash = await _poolManager.GetAsync(muzzleFlashPoolKey);
        if (muzzleFlash != null)
        {
            muzzleFlash.transform.position = firePoint.position;
            muzzleFlash.transform.rotation = firePoint.rotation;
            
            // Tự động trả về sau 0.1 giây
            var autoReturn = muzzleFlash.GetComponent<AutoReturnToPool>() ?? muzzleFlash.AddComponent<AutoReturnToPool>();
            autoReturn.ReturnAfterSeconds = 0.1f;
        }
        
        // Bắn đạn
        var bullet = await _poolManager.Get<Bullet>(bulletPoolKey);
        if (bullet != null)
        {
            bullet.transform.position = firePoint.position;
            bullet.transform.rotation = firePoint.rotation;
            bullet.Initialize(bulletSpeed, firePoint.forward, OnBulletHit);
            
            // Tự động trả về sau thời gian quy định
            var autoReturn = bullet.GetComponent<AutoReturnToPool>() ?? bullet.AddComponent<AutoReturnToPool>();
            autoReturn.ReturnAfterSeconds = bulletLifetime;
        }
    }
    
    private async void OnBulletHit(Vector3 hitPoint, Vector3 hitNormal)
    {
        // Hiển thị hiệu ứng va chạm
        var impactEffect = await _poolManager.GetAsync(impactEffectPoolKey);
        if (impactEffect != null)
        {
            impactEffect.transform.position = hitPoint;
            impactEffect.transform.rotation = Quaternion.LookRotation(hitNormal);
            
            // Tự động trả về sau khi hoàn thành
            var autoReturn = impactEffect.GetComponent<AutoReturnToPool>() ?? impactEffect.AddComponent<AutoReturnToPool>();
            autoReturn.ReturnAfterSeconds = 1.5f;
        }
    }
    
    private void OnDisable()
    {
        // Không cần xóa pool vì có thể được sử dụng lại
        // Nếu muốn xóa khi scene unload, đăng ký vào event SceneManager.sceneUnloaded
    }
}

// Lớp đạn
public class Bullet : MonoBehaviour, IPoolable
{
    private float _speed;
    private Vector3 _direction;
    private System.Action<Vector3, Vector3> _onHitCallback;
    
    public void Initialize(float speed, Vector3 direction, System.Action<Vector3, Vector3> onHitCallback)
    {
        _speed = speed;
        _direction = direction;
        _onHitCallback = onHitCallback;
    }
    
    private void Update()
    {
        transform.Translate(_direction * _speed * Time.deltaTime, Space.World);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Gọi callback va chạm
        _onHitCallback?.Invoke(collision.contacts[0].point, collision.contacts[0].normal);
        
        // Tự động trả về pool (sẽ được gọi bởi AutoReturnToPool)
    }
    
    public void OnGetFromPool()
    {
        gameObject.SetActive(true);
    }
    
    public void OnReturnToPool()
    {
        gameObject.SetActive(false);
        _onHitCallback = null;
    }
}
```

### Ví dụ 2: Infinite Scrolling UI List với UI Pooling

Một ứng dụng hiệu quả khác của Object Pooling là trong UI động, đặc biệt là các danh sách cuộn vô hạn.

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;

public class InfiniteScrollList : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject loadingIndicator;
    
    [Header("Pool Settings")]
    [SerializeField] private string itemPoolKey = "ListItem";
    [SerializeField] private GameObject itemPrefab;  // Prefab item UI
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int visibleItemCount = 10;  // Số item hiển thị đồng thời
    [SerializeField] private float itemHeight = 100f;    // Chiều cao mỗi item
    
    [Header("Data Settings")]
    [SerializeField] private int loadBatchSize = 20;     // Số lượng item load mỗi lần
    [SerializeField] private int maxItems = 1000;        // Tổng số item tối đa
    
    private PoolManager _poolManager;
    private List<ItemData> _allItemsData = new List<ItemData>();
    private List<GameObject> _visibleItems = new List<GameObject>();
    private int _startIndex = 0;
    private float _scrollPosition = 0f;
    private bool _isInitialized = false;
    private bool _isLoading = false;
    
    // Class dữ liệu mẫu
    [System.Serializable]
    public class ItemData
    {
        public string title;
        public string description;
        public Sprite icon;
        
        public ItemData(int index)
        {
            title = $"Item {index}";
            description = $"This is description for item {index}";
        }
    }
    
    private async void Start()
    {
        _poolManager = PoolManager.Instance;
        await InitializePoolAsync();
        
        // Đăng ký sự kiện scroll
        scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        
        // Tạo dữ liệu demo ban đầu
        LoadMoreItems(0, loadBatchSize);
        
        // Khởi tạo hiển thị
        RefreshVisibleItems();
    }
    
    private async Task InitializePoolAsync()
    {
        // Cấu hình đặc biệt cho UI Pool
        PoolConfig config = new PoolConfig
        {
            initialSize = initialPoolSize,
            allowGrowth = true,
            maxSize = initialPoolSize * 2
        };
        
        // Tạo UI Pool với prefab
        await _poolManager.CreateUIPoolAsync(itemPoolKey, itemPrefab, config);
        _isInitialized = true;
    }
    
    private void LoadMoreItems(int startIndex, int count)
    {
        if (_isLoading || startIndex >= maxItems) return;
        _isLoading = true;
        
        loadingIndicator.SetActive(true);
        
        // Mô phỏng tải dữ liệu từ API
        for (int i = startIndex; i < Mathf.Min(startIndex + count, maxItems); i++)
        {
            _allItemsData.Add(new ItemData(i));
        }
        
        // Cập nhật kích thước của container
        contentContainer.sizeDelta = new Vector2(contentContainer.sizeDelta.x, _allItemsData.Count * itemHeight);
        
        loadingIndicator.SetActive(false);
        _isLoading = false;
        
        RefreshVisibleItems();
    }
    
    private void OnScrollValueChanged(Vector2 position)
    {
        if (!_isInitialized) return;
        
        _scrollPosition = 1 - position.y;  // Đảo ngược vì 1 là phía trên
        RefreshVisibleItems();
        
        // Kiểm tra nếu scroll gần cuối thì load thêm
        if (position.y < 0.1f && !_isLoading && _allItemsData.Count < maxItems)
        {
            LoadMoreItems(_allItemsData.Count, loadBatchSize);
        }
    }
    
    private async void RefreshVisibleItems()
    {
        if (!_isInitialized) return;
        
        // Tính toán vị trí bắt đầu hiển thị
        int itemCount = _allItemsData.Count;
        int newStartIndex = Mathf.FloorToInt(_scrollPosition * Mathf.Max(0, itemCount - visibleItemCount));
        newStartIndex = Mathf.Clamp(newStartIndex, 0, Mathf.Max(0, itemCount - visibleItemCount));
        
        // Nếu vị trí không thay đổi và số lượng item đã đủ, không cần làm gì
        if (newStartIndex == _startIndex && _visibleItems.Count == Mathf.Min(visibleItemCount, itemCount))
            return;
        
        _startIndex = newStartIndex;
        
        // Trả về pool các item không còn hiển thị
        foreach (var item in _visibleItems)
        {
            _poolManager.Return(item);
        }
        _visibleItems.Clear();
        
        // Lấy các item mới từ pool và cập nhật dữ liệu
        for (int i = 0; i < Mathf.Min(visibleItemCount, itemCount - _startIndex); i++)
        {
            int dataIndex = _startIndex + i;
            
            var uiItem = await _poolManager.GetUIAsync(itemPoolKey, contentContainer);
            if (uiItem != null)
            {
                // Cập nhật vị trí
                RectTransform rt = uiItem.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, -dataIndex * itemHeight);
                
                // Cập nhật dữ liệu
                ListItemUI itemUI = uiItem.GetComponent<ListItemUI>();
                if (itemUI != null)
                {
                    itemUI.SetData(_allItemsData[dataIndex]);
                }
                
                _visibleItems.Add(uiItem);
            }
        }
    }
    
    private void OnDestroy()
    {
        // Hủy đăng ký sự kiện
        scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        
        // Trả tất cả item về pool
        foreach (var item in _visibleItems)
        {
            _poolManager.Return(item);
        }
    }
}

// Lớp UI Item
public class ListItemUI : MonoBehaviour, IPoolable
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Image iconImage;
    
    public void SetData(InfiniteScrollList.ItemData data)
    {
        titleText.text = data.title;
        descriptionText.text = data.description;
        
        if (data.icon != null)
            iconImage.sprite = data.icon;
    }
    
    public void OnGetFromPool()
    {
        gameObject.SetActive(true);
    }
    
    public void OnReturnToPool()
    {
        gameObject.SetActive(false);
    }
}
```

### Ví dụ 3: Particle System Manager với Auto-Trimming

Quản lý hiệu ứng particle với cơ chế tự động dọn dẹp thông minh.

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ParticleSystemManager : MonoBehaviour
{
    [System.Serializable]
    public class ParticleEffectMapping
    {
        public string effectName;
        public string addressableKey;
        public int initialPoolSize = 5;
        public float autoTrimTime = 30f;
    }
    
    [Header("Particle Effect Settings")]
    [SerializeField] private ParticleEffectMapping[] particleEffects;
    
    [Header("Error Handling")]
    [SerializeField] private GameObject fallbackParticleEffect;
    
    private PoolManager _poolManager;
    private Dictionary<string, string> _effectKeyMap = new Dictionary<string, string>();
    
    private async void Awake()
    {
        _poolManager = PoolManager.Instance;
        await InitializeParticlePoolsAsync();
    }
    
    private async Task InitializeParticlePoolsAsync()
    {
        foreach (var effect in particleEffects)
        {
            // Lưu mapping giữa tên và key
            _effectKeyMap[effect.effectName] = effect.addressableKey;
            
            // Cấu hình pool
            PoolConfig config = new PoolConfig
            {
                initialSize = effect.initialPoolSize,
                allowGrowth = true,
                maxSize = effect.initialPoolSize * 3
            };
            
            // Cấu hình trimming - rất quan trọng cho hiệu ứng
            PoolTrimmingConfig trimmingConfig = new PoolTrimmingConfig
            {
                enableAutoTrim = true,
                trimCheckInterval = 15f,  // Kiểm tra sau mỗi 15 giây
                inactiveTimeThreshold = effect.autoTrimTime, // Thời gian không hoạt động trước khi trim
                targetRetainRatio = 0.3f, // Chỉ giữ lại 30%
                minimumRetainCount = 2    // Luôn giữ ít nhất 2 instance
            };
            
            // Cấu hình xử lý lỗi Addressable
            AddressableErrorConfig errorConfig = new AddressableErrorConfig
            {
                errorHandlingStrategy = AddressableErrorHandling.ReturnPlaceholder,
                fallbackPrefab = fallbackParticleEffect,
                onAddressableLoadError = (ex, key) => 
                {
                    Debug.LogWarning($"Không thể tải hiệu ứng '{key}'. Sử dụng hiệu ứng dự phòng. Lỗi: {ex.Message}");
                }
            };
            
            // Tạo pool
            await _poolManager.CreatePoolAsync(effect.addressableKey, effect.addressableKey, config, errorConfig, trimmingConfig);
        }
    }
    
    /// <summary>
    /// Phát hiệu ứng tại vị trí chỉ định
    /// </summary>
    public async Task<ParticleSystem> PlayEffectAsync(string effectName, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!_effectKeyMap.TryGetValue(effectName, out string key))
        {
            Debug.LogError($"Hiệu ứng '{effectName}' không tồn tại trong danh sách đã đăng ký.");
            return null;
        }
        
        GameObject effectObject = await _poolManager.GetAsync(key);
        if (effectObject != null)
        {
            // Thiết lập transform
            effectObject.transform.position = position;
            effectObject.transform.rotation = rotation;
            
            if (parent != null)
                effectObject.transform.SetParent(parent);
            
            // Gắn AutoReturnToPool và cấu hình
            EffectAutoReturn autoReturn = effectObject.GetComponent<EffectAutoReturn>();
            if (autoReturn == null)
                autoReturn = effectObject.AddComponent<EffectAutoReturn>();
            
            // Reset và play particle
            var particleSystem = effectObject.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Clear();
                particleSystem.Play();
            }
            
            return particleSystem;
        }
        
        return null;
    }
    
    /// <summary>
    /// Lớp mở rộng từ AutoReturnToPool để xử lý đặc biệt cho Particle System
    /// </summary>
    private class EffectAutoReturn : AutoReturnToPool
    {
        private ParticleSystem _particleSystem;
        
        protected override void Awake()
        {
            base.Awake();
            _particleSystem = GetComponent<ParticleSystem>();
            
            // Nếu có particle system, chỉ trả về khi hiệu ứng kết thúc
            if (_particleSystem != null)
            {
                ReturnAfterSeconds = 0; // Không dùng timer mặc định
                StartCoroutine(CheckParticleCompletion());
            }
            else
            {
                // Mặc định trả về sau 3 giây nếu không tìm thấy particle system
                ReturnAfterSeconds = 3f;
            }
        }
        
        private System.Collections.IEnumerator CheckParticleCompletion()
        {
            if (_particleSystem == null) yield break;
            
            // Đảm bảo đợi cho đến khi hiệu ứng bắt đầu
            yield return new WaitForSeconds(0.1f);
            
            // Đợi cho đến khi hiệu ứng kết thúc
            while (_particleSystem.IsAlive(true))
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            // Khi hiệu ứng kết thúc, trả về pool
            ReturnToPool();
        }
    }
    
    // Phương thức tiện ích để chơi hiệu ứng đồng bộ
    public void PlayEffect(string effectName, Vector3 position)
    {
        _ = PlayEffectAsync(effectName, position, Quaternion.identity);
    }
    
    // Quản lý bộ nhớ khi thay đổi scene
    private void OnDestroy()
    {
        // Không cần xóa pool, đã cấu hình trimming tự động
        // _poolManager.TrimExcessPools(); // Có thể gọi nếu muốn giải phóng bộ nhớ ngay lập tức
    }
}
```

## Các lưu ý quan trọng và Best Practices

### 1. Hiệu năng và Bộ nhớ

- **Prewarm Pools**: Luôn khởi tạo trước các pool cho đối tượng sử dụng thường xuyên vào thời điểm phù hợp (loading screen, scene start).
- **Cân nhắc kích thước ban đầu**: Đặt initialSize đủ lớn để tránh instantiate trong gameplay, nhưng không quá lớn gây lãng phí bộ nhớ.
- **Khôn ngoan với Trimming**: Cấu hình trimming phù hợp với từng loại đối tượng - aggressive cho hiệu ứng tạm thời, conservative cho đối tượng quan trọng.
- **Cẩn thận với `GetComponent<T>()`**: Tránh gọi lặp lại GetComponent, thay vào đó cache references trong OnGetFromPool.

### 2. Addressables và Memory Management

- **Theo dõi Reference Count**: Hiểu rằng thư viện quản lý reference count trong Addressables để tránh leak.
- **Xử lý Scene Transitions**: Đặc biệt chú ý đến việc quản lý pool khi chuyển scene để tránh memory leak.
- **Đừng tham lam prefab**: Chỉ tạo pool cho những prefab được sử dụng lặp lại nhiều lần, không pooling tất cả mọi thứ.

### 3. UI Pooling

- **Hạn chế Rebuild Layout**: Cẩn thận khi thay đổi nội dung UI để tránh LayoutRebuilder chạy quá nhiều lần.
- **Tránh Scale từ 0 đến 1**: Thay vì scale từ 0 đến 1 khi hiển thị UI, hãy sử dụng SetActive để tránh rebuild layout.
- **Kiểm soát Canvas**: Nếu pool nhiều UI element, hãy kiểm soát cẩn thận Canvas và GraphicRaycaster.

### 4. Thread Safety

- **Main Thread Only**: Nhớ rằng thư viện này chỉ an toàn trên main thread Unity, không sử dụng trực tiếp từ thread khác.
- **Job System**: Khi làm việc với Unity Job System, cần lên kế hoạch cẩn thận - lấy reference từ main thread trước khi chuyển sang job.

### 5. Debugging và Troubleshooting

- **Sử dụng PoolManagerEditor**: Thường xuyên kiểm tra dashboard để phát hiện vấn đề hiệu năng.
- **Bật Debug Logs**: Khi gặp vấn đề, bật IsDebugLogEnabled để xem chi tiết các hoạt động của pool.
- **Mô hình Mental**: Luôn hiểu rõ vòng đời của object trong pool (instantiate -> inactive -> active -> inactive -> ...) để dễ debug.
``` 