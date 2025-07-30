using UnityEngine;

public class ZoneViewEnemy : MonoBehaviour
{
    public string enemyTag = "Enemy";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(enemyTag))
        {

            Transform healthBar = FindHealthBar(other.transform);
            if (healthBar != null)
            {
                healthBar.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(enemyTag))
        {

            Transform healthBar = FindHealthBar(other.transform);
            if (healthBar != null)
            {
                healthBar.gameObject.SetActive(false);
            }
        }
    }

    private Transform FindHealthBar(Transform root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Heath_Bar")
                return child;
        }
        return null;
    }
}
