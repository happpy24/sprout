using UnityEngine;

/// <summary>
/// Placed in every scene to ensure EssentialObjects prefab is loaded.
/// On first scene load, instantiates the prefab. On subsequent scenes, finds existing instance and self-destructs.
/// This ensures there's only ever one EssentialObjects (with Global Light 2D, EventSystem, etc.)
/// </summary>
public class EssentialObjectsLoader : MonoBehaviour
{
    [Header("Prefab Reference")]
    public GameObject essentialObjectsPrefab;

    [Header("Configuration")]
    public string essentialObjectsTag = "EssentialObjects";

    void Awake()
    {
        GameObject existingEssentials = GameObject.FindGameObjectWithTag(essentialObjectsTag);

        if (existingEssentials != null)
        {

            Destroy(gameObject);
            return;
        }

        if (essentialObjectsPrefab != null)
        {
            GameObject instantiated = Instantiate(essentialObjectsPrefab);
            instantiated.name = "EssentialObjects";
        }

        Destroy(gameObject);
    }
}
