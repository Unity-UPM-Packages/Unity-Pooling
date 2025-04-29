using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))] // Đảm bảo có Renderer để đổi màu
public class AnimateOnEnable : MonoBehaviour
{
    [Header("Scaling")]
    public float scaleDuration = 0.3f; // Thời gian để scale lên kích thước đầy đủ
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Đường cong scale mượt mà

    [Header("Color")]
    public Color[] randomColors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta }; // Mảng màu ngẫu nhiên

    private Vector3 originalScale;
    private Renderer objectRenderer;
    private Coroutine scaleCoroutine;

    void Awake()
    {
        // Lưu lại scale gốc để biết scale đích là bao nhiêu
        originalScale = transform.localScale;
        objectRenderer = GetComponent<Renderer>();

        // Đảm bảo object bắt đầu với scale nhỏ hoặc 0 để thấy hiệu ứng
        transform.localScale = Vector3.zero;
    }

    void OnEnable()
    {
        // 1. Đặt màu ngẫu nhiên
        SetRandomColor();

        // 2. Bắt đầu hiệu ứng scale
        // Dừng coroutine cũ nếu có (phòng trường hợp bị enable/disable nhanh)
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleUp());
    }

    void OnDisable()
    {
        // Dừng coroutine nếu object bị disable giữa chừng
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        // Reset scale về 0 để chuẩn bị cho lần enable tiếp theo
        transform.localScale = Vector3.zero;
    }

    /// <summary>
    /// Phương thức này cho phép khởi động lại hiệu ứng scale up và đổi màu từ bên ngoài,
    /// ngay cả khi object đã active và OnEnable không được gọi lại.
    /// Đặc biệt hữu ích khi sử dụng với Object Pooling.
    /// </summary>
    public void ResetAndPlayAnimation()
    {
        // Reset scale về 0
        transform.localScale = Vector3.zero;
        
        // Dừng coroutine cũ nếu đang chạy
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        
        // Đặt màu ngẫu nhiên
        SetRandomColor();
        
        // Bắt đầu hiệu ứng scale
        scaleCoroutine = StartCoroutine(ScaleUp());
    }

    void SetRandomColor()
    {
        if (objectRenderer != null && randomColors.Length > 0)
        {
            // Lấy một màu ngẫu nhiên từ mảng
            Color chosenColor = randomColors[Random.Range(0, randomColors.Length)];
            // Áp dụng màu vào material. Lưu ý: cái này sẽ tạo instance material mới.
            // Để tối ưu hơn nữa (tránh tạo material instances), bạn có thể dùng MaterialPropertyBlock.
            // Nhưng với demo đơn giản thì cách này ổn.
            objectRenderer.material.color = chosenColor;
        }
    }

    IEnumerator ScaleUp()
    {
        float timer = 0f;
        Vector3 startScale = transform.localScale; // Lấy scale hiện tại (có thể là 0 hoặc một giá trị nhỏ)

        while (timer < scaleDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / scaleDuration);
            float curveValue = scaleCurve.Evaluate(progress); // Áp dụng đường cong

            transform.localScale = Vector3.LerpUnclamped(startScale, originalScale, curveValue);

            yield return null; // Chờ frame tiếp theo
        }

        // Đảm bảo scale cuối cùng chính xác là originalScale
        transform.localScale = originalScale;
        scaleCoroutine = null; // Đánh dấu coroutine đã hoàn thành
    }
}