using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class MobAttack : MonoBehaviour
{
    [SerializeField]private Animator eff_attack;
    private MobData target;
    private AudioSource[] AudioSource;
    // Update is called once per frame
    private void Start()
    {
        target = transform.GetComponent<MobData>();
        AudioSource = GetComponents<AudioSource>();
    }
    void Update()
    {
        HandleAttack();
    }
    public void HandleAttack()
    {
        if (!target.attack)
            return;
        if (eff_attack == null || eff_attack.Equals(null))
            return;

        // Kiểm tra xem eff_attack có còn tồn tại không (tránh MissingReferenceException)
        if (eff_attack && HasParameter(eff_attack, "Attack", AnimatorControllerParameterType.Trigger))
        {
            eff_attack.SetTrigger("Attack");
        }

        target.attack = false;

        if (AudioSource != null && AudioSource.Length > 0 && AudioSource[0] != null)
            AudioSource[0].Play();
    }

    private bool HasParameter(Animator animator, string paramName, AnimatorControllerParameterType type)
    {
        if (!animator) // kiểm tra xem animator có còn sống không
            return false;

        foreach (var param in animator.parameters)
        {
            if (param.type == type && param.name == paramName)
                return true;
        }
        return false;
    }

}
