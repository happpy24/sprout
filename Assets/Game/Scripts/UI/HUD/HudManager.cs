using UnityEngine;

public class HudManager : MonoBehaviour
{
    public GameObject health;
    public GameObject[] healthSlots;
    public GameObject refillBar;
    public GameObject dashIndicator;
    public GameObject doubleJumpIndicator;
    public GameObject[] playerStatus;

    private float refillBarProgress = 0;

    public void DashIndicator(bool status)
    {
        if (status)
        {
            dashIndicator.SetActive(true);
        } else
            dashIndicator.SetActive(false);
    }

    public void DoubleJumpIndicator(bool status)
    {
        if (status)
        {
            doubleJumpIndicator.SetActive(true);
        }
        else
            doubleJumpIndicator.SetActive(false);
    }

    public void UpdateHealthUI(float currentHealth)
    {
        for (int i = 0; i < healthSlots.Length; i++)
        {
            Animator heartAnim = healthSlots[i].GetComponent<Animator>();

            if (i >= currentHealth)
            {
                heartAnim.ResetTrigger("Restore");
                heartAnim.SetTrigger("Damage");
            }
            else
            {
                heartAnim.ResetTrigger("Damage");
                heartAnim.SetTrigger("Restore");
            }
        }
    }

    public void FillRefillBar(float rfBar)
    {
        Animator refillAnim = refillBar.GetComponent<Animator>();
        while (refillBarProgress <= rfBar)
        {
            refillBarProgress++;
            refillAnim.SetTrigger("Fill");
        }
    }

    public void EmptyRefillBar()
    {
        refillBarProgress = 0;
        Animator refillAnim = refillBar.GetComponent<Animator>();
        refillAnim.SetTrigger("Consume");
    }
}
