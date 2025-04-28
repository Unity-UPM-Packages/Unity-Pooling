using UnityEngine;
using System.Collections; // Cần cho IEnumerator
using com.thelegends.unity.pooling; // Thay bằng namespace thực tế của thư viện pooling

public class SimpleSpawner : MonoBehaviour
{
    public GameObject objectPrefab; // Kéo PooledSpherePrefab vào đây
    public float spawnInterval = 0.1f; // Tần suất spawn
    public string poolKey = "SpherePool";

    private PoolManager poolManager;
    private bool usePooling = true; // Biến để chuyển đổi chế độ

    async void Start()
    {
        if (objectPrefab == null)
        {
            Debug.LogError("Chưa gán Prefab cho Spawner!");
            this.enabled = false;
            return;
        }

        poolManager = PoolManager.Instance;

        // --- Cấu hình Pool ---
        // Chỉ tạo pool nếu dùng chế độ pooling
        // Kế hoạch 7.0 có symbol OBJECT_POOLING, nhưng ở đây ta dùng biến bool cho dễ demo
        #if OBJECT_POOLING // Giả sử bạn có symbol này theo kế hoạch
        Debug.Log("Pooling is Enabled via Symbol.");
        PoolConfig config = new PoolConfig
        {
            initialSize = 10,
            allowGrowth = true,
            maxSize = 300
        };
        // Sử dụng prefab trực tiếp thay vì Addressables cho đơn giản
        await poolManager.CreatePoolAsync(poolKey, config);
        #else
        Debug.LogWarning("Pooling is Disabled (OBJECT_POOLING symbol not defined). Using Instantiate/Destroy.");
        usePooling = false; // Buộc không dùng pooling nếu symbol không được định nghĩa
        #endif

        // Bắt đầu coroutine spawn
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnObject();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnObject()
    {
        Vector3 spawnPosition = transform.position + Random.insideUnitSphere * 2f; // Vị trí ngẫu nhiên gần Spawner

        if (usePooling)
        {
            // --- Lấy từ Pool ---
            // Dùng GetAsync nhưng không await ngay lập tức để không block coroutine quá lâu
            // Trong demo đơn giản này, việc fire-and-forget là chấp nhận được
            _ = GetPooledObjectAsync(spawnPosition);
        }
        else
        {
            // --- Instantiate thông thường ---
            GameObject instance = Instantiate(objectPrefab, spawnPosition, Quaternion.identity);
            // Quan trọng: Gọi Destroy sau một khoảng thời gian để mô phỏng vòng đời ngắn
            // Lấy thời gian từ AutoReturnToPool component (nếu có) hoặc dùng giá trị mặc định
            float lifetime = 2.0f;
            Destroy(instance, lifetime);
        }
    }

    // Hàm async riêng để lấy object từ pool
    async System.Threading.Tasks.Task GetPooledObjectAsync(Vector3 position)
    {
#if OBJECT_POOLING
        GameObject pooledObject = await poolManager.GetAsync(poolKey);
        if (pooledObject != null)
        {
            // PoolManager (hoặc IPoolable.OnGetFromPool) đã gọi SetActive(true)
            // nên AnimateOnEnable.OnEnable() sẽ tự động được gọi.

            // Chỉ cần đặt vị trí và rotation
            pooledObject.transform.position = position;
            pooledObject.transform.rotation = Quaternion.identity;

        }
#else
            // Không làm gì nếu pooling bị tắt
            await System.Threading.Tasks.Task.CompletedTask;
#endif
    }

    // --- Hàm để gọi từ UI Button (Tùy chọn) ---
    public void SetPoolingMode(bool poolingEnabled)
    {
        #if OBJECT_POOLING
        usePooling = poolingEnabled;
        Debug.Log("Switched to " + (usePooling ? "Pooling" : "Instantiate/Destroy") + " mode.");
        #else
        usePooling = false; // Luôn là false nếu symbol không được định nghĩa
        Debug.LogWarning("Cannot enable pooling because OBJECT_POOLING symbol is not defined. Staying in Instantiate/Destroy mode.");
        #endif
    }
}