using UnityEngine;

public class HealthBarController : MonoBehaviour
{
    public float targetHP = 1f;
    public float lerpSpeed = 1f;

    private Transform barTransform;
    private float initialLocalPosX;

    private void Start()
    {
        barTransform = transform;
        initialLocalPosX = barTransform.localPosition.x;
    }


    private void Update()
    {
        if (barTransform == null || transform.parent == null) return;

        float originalScaleX = barTransform.localScale.x;
        float sign = Mathf.Sign(originalScaleX); // Lấy dấu: -1 hoặc 1

        float lerpedScaleX = Mathf.Lerp(Mathf.Abs(originalScaleX), targetHP, Time.deltaTime * lerpSpeed);

        // Cập nhật scale mới, giữ nguyên chiều ban đầu
        Vector3 scale = barTransform.localScale;
        scale.x = lerpedScaleX * sign;
        barTransform.localScale = scale;
    }




    public void SetHP(float hp)
    {
        targetHP = Mathf.Clamp01(hp);
        Debug.Log("%hp="+ hp);
    }
}
