using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HudManager : MonoBehaviour
{
    public GameObject health;
    public GameObject healthPrefab;
    public List<GameObject> healthSlots = new List<GameObject>();
    public GameObject refillBar;
    public GameObject dashIndicator;
    public GameObject doubleJumpIndicator;
    public GameObject[] playerStatus;

    [SerializeField]
    private float cachedPlayerHealth = 5;

    [SerializeField]
    private float refillBarProgress = 0;

    private List<Vector3> shakeBasePositions = new List<Vector3>();

    private Animator hudAnimation;

    private Coroutine lowHealthShakeCoroutine;
    private HorizontalLayoutGroup layoutGroup;

    private void Start()
    {
        hudAnimation = GetComponent<Animator>();
        layoutGroup = health.GetComponent<HorizontalLayoutGroup>();

        foreach (GameObject playerStat in playerStatus)
        {
            if (playerStat.name == "DamageTaken")
            {
                playerStat.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        if (cachedPlayerHealth <= 1)
        {
            if (lowHealthShakeCoroutine == null)
            {
                lowHealthShakeCoroutine = StartCoroutine(LowHealthShakeRoutine());
            }
        }
        else
        {
            if (lowHealthShakeCoroutine != null)
            {
                StartCoroutine(VignetteController(1f, 0.5f, 1.5f));
                StartCoroutine(GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>().DeactivateHeartbeat());
                StopCoroutine(lowHealthShakeCoroutine);
                lowHealthShakeCoroutine = null;

                if (layoutGroup != null)
                {
                    layoutGroup.enabled = true;
                }
            }
        }
    }

    private IEnumerator LowHealthShakeRoutine()
    {
        yield return new WaitForEndOfFrame();
        StartCoroutine(GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>().ActivateHeartbeat());
        StartCoroutine(VignetteController(0.5f, 1f, 0.1f));
        yield return new WaitForSeconds(0.1f);

        shakeBasePositions.Clear();
        foreach (GameObject heart in healthSlots)
        {
            RectTransform rt = heart.GetComponent<RectTransform>();
            shakeBasePositions.Add(rt.localPosition);
        }

        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        const float magnitude = 1f;

        while (true)
        {
            for (int i = 0; i < healthSlots.Count; i++)
            {
                RectTransform rt = healthSlots[i].GetComponent<RectTransform>();
                Vector3 basePosition = shakeBasePositions[i];

                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                rt.localPosition = basePosition + new Vector3(x, y, 0);
            }

            yield return null;
        }
    }

    private IEnumerator VignetteController(float startAlpha, float endAlpha, float duration)
    {
        Image vignette = GameObject.FindGameObjectWithTag("Vignette").GetComponent<Image>();
        Color c = vignette.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            vignette.color = c;
            yield return null;
        }
    }

    public void DashIndicator(bool status)
    {
        dashIndicator.SetActive(status);
    }

    public void DoubleJumpIndicator(bool status)
    {
        doubleJumpIndicator.SetActive(status);
    }

    public void AddLife()
    {
        hudAnimation.SetTrigger("Grow");
        GameObject newHealth = Instantiate(healthPrefab, health.transform);
        healthSlots.Add(newHealth);
        UpdateHealthUI(healthSlots.Count);
    }

    public void UpdateHealthUI(float currentHealth)
    {
        const float shakeDuration = 0.1f;
        const float shakeMagnitude = 2f;

        for (int i = 0; i < healthSlots.Count; i++)
        {
            Animator heartAnim = healthSlots[i].GetComponent<Animator>();
            GameObject heart = healthSlots[i];

            if (i >= currentHealth)
            {
                heartAnim.ResetTrigger("Restore");
                heartAnim.SetTrigger("Damage");

                if (cachedPlayerHealth > currentHealth)
                    StartCoroutine(ShakeRectTransform(heart, shakeDuration, shakeMagnitude));
            }
            else
            {
                heartAnim.ResetTrigger("Damage");
                heartAnim.SetTrigger("Restore");
            }
        }

        cachedPlayerHealth = currentHealth;

        while (shakeBasePositions.Count < healthSlots.Count)
        {
            shakeBasePositions.Add(Vector3.zero);
        }

        foreach (GameObject playerStat in playerStatus)
        {
            bool isFullHP = currentHealth == healthSlots.Count;
            bool isLowHP = currentHealth < (healthSlots.Count / 2);

            if (playerStat.name == "FullHP") { playerStat.SetActive(isFullHP); }
            if (playerStat.name == "Neutral") { playerStat.SetActive(!isFullHP && !isLowHP); }
        }
    }

    IEnumerator ShakeRectTransform(GameObject heart, float duration, float magnitude)
    {
        RectTransform rt = heart.GetComponent<RectTransform>();

        if (lowHealthShakeCoroutine != null)
        {
            StopCoroutine(lowHealthShakeCoroutine);
            lowHealthShakeCoroutine = null;
        }

        yield return new WaitForEndOfFrame();

        Vector3 originalPosition = rt.localPosition;

        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        float elapsed = 0f;

        foreach (GameObject playerStat in playerStatus)
        {
            if (playerStat.name == "DamageTaken")
            {
                playerStat.SetActive(true);
            }
        }

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            rt.localPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;

            yield return null;
        }

        foreach (GameObject playerStat in playerStatus)
        {
            if (playerStat.name == "DamageTaken")
            {
                playerStat.SetActive(false);
            }
        }

        rt.localPosition = originalPosition;

        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
        }
    }

    public void FillRefillBar(float rfBar)
    {
        StartCoroutine(RefillBarAnimator(rfBar));
    }

    IEnumerator RefillBarAnimator(float rfBar)
    {
        Animator refillAnim = refillBar.GetComponent<Animator>();

        while (refillBarProgress < rfBar)
        {
            refillBarProgress++;
            refillAnim.SetTrigger("Fill");
            yield return new WaitForSeconds(0.15f);
        }
    }

    public void EmptyRefillBar()
    {
        StopAllCoroutines();
        refillBarProgress = 0;
        Animator refillAnim = refillBar.GetComponent<Animator>();
        refillAnim.SetTrigger("Consume");
    }
}