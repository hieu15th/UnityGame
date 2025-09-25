using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectCondition
{
    public GameObject obj;   // GameObject cần kiểm tra
    public bool Active_True;    // Trạng thái mong muốn (true = bật, false = tắt)
}

public class ActiveJoystick : MonoBehaviour
{
    [Header("Danh sách GameObject + Điều kiện")]
    public List<ObjectCondition> conditions = new List<ObjectCondition>();

    [Header("Joystick cần bật/tắt")]
    public GameObject joystick;

    void Update()
    {
        if (joystick == null || conditions.Count == 0) return;

        bool allMatched = true;

        foreach (var cond in conditions)
        {
            if (cond.obj == null) continue;

            if (cond.obj.activeSelf != cond.Active_True)
            {
                allMatched = false;
                break;
            }
        }

        joystick.SetActive(allMatched);
    }
}
