using UnityEngine;
using System.Collections;

public class LoadingAnimation : MonoBehaviour
{
    [SerializeField] private GameObject loading_0;
    [SerializeField] private GameObject loading_1;
    [SerializeField] private GameObject loading_2;
    [SerializeField] private GameObject loading_3;
    [SerializeField] private GameObject loading_4;
    [SerializeField] private float switchInterval = 0.2f;

    private GameObject[] loadingObjects;
    private int currentIndex = 0;
    private Coroutine animationCoroutine;

    void Awake()
    {
        loadingObjects = new GameObject[] { loading_0, loading_1, loading_2, loading_3, loading_4 };

        for (int i = 0; i < loadingObjects.Length; i++)
        {
            if (loadingObjects[i] == null)
            {
                Debug.LogError($"LoadingAnimation: loading_{i} chưa được gán trong Inspector!");
                enabled = false;
                return;
            }
        }
    }

    void OnEnable()
    {
        SetAllInactive();
        animationCoroutine = StartCoroutine(LoadingSequence());
    }

    void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private void SetAllInactive()
    {
        foreach (GameObject obj in loadingObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    private IEnumerator LoadingSequence()
    {
        yield return null; // Đợi 1 frame để đảm bảo các GameObject đã active

        while (true)
        {
            SetAllInactive();
            loadingObjects[currentIndex].SetActive(true);
            currentIndex = (currentIndex + 1) % loadingObjects.Length;
            yield return new WaitForSeconds(switchInterval);
        }
    }

}
