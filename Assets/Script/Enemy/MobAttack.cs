using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class MobAttack : MonoBehaviour
{
    [SerializeField]private GameObject eff_attack;
    private MobData target;
    // Update is called once per frame
    private void Start()
    {
        target = transform.GetComponent<MobData>();
    }
    void Update()
    {
        HandleAttack();
    }
    public void HandleAttack()
    {
        if (target.attack != false)
        {
            eff_attack.SetActive(true);
            target.attack = false;

            // Start the coroutine to set attack to false after 0.25 seconds
            StartCoroutine(SetAttackFalseAfterDelay(0.1f));
        }
    }

    private IEnumerator SetAttackFalseAfterDelay(float delay)
    {
        // Wait for the specified time
        yield return new WaitForSeconds(delay);

        // After 0.25 seconds, set the attack to false
        eff_attack.SetActive(false);
        Debug.Log("Target attack set to false after 0.25 seconds.");
    }
}
