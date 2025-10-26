using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Base Stats")]
    public int maxHp = 1;
    protected int currentHp;

    protected virtual void Awake()
    {
        currentHp = maxHp;
    }

    public virtual void TakeDamage(int amount)
    {
        currentHp -= amount;

        if (currentHp <= 0)
            Die();
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
