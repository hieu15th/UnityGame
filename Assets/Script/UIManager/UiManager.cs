using UnityEngine;

public class UiManager : MonoBehaviour
{
    public GameObject[] UI_LV1;
    public GameObject[] UI_LV2;

    void Update()
    {
        HideUI_1();
        OpenUI_1();
    }
    void HideUI_1()
    {
        foreach (GameObject go in UI_LV2)
        {
            if (go.activeSelf)
            {
                foreach (GameObject go2 in UI_LV1)
                {
                    go2.SetActive(false);
                }
            }
        }
    }
    void OpenUI_1()
    {
        foreach(GameObject go in UI_LV1)
        {
            if (go.activeSelf)
            {
                foreach( GameObject go2 in UI_LV1)
                {
                    go2.SetActive(true);
                }
            }
        }
    }
}
