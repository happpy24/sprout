using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic singleton script that ensures only one instance of the attached GameObject exists across scenes.
/// Can be attached to any GameObject (Player, EssentialObjects, etc.).
/// Each unique GameObject name will have its own singleton instance.
/// </summary>
public class DDOL : MonoBehaviour
{
    // Dictionary to track instances by GameObject name
    private static Dictionary<string, DDOL> instances = new Dictionary<string, DDOL>();

    [Header("Configuration")]
    public string instanceID = "";

    private string instanceKey;

    void Awake()
    {
        instanceKey = string.IsNullOrEmpty(instanceID) ? gameObject.name : instanceID;

        if (!instances.ContainsKey(instanceKey))
        {
            instances[instanceKey] = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        if (instances.ContainsKey(instanceKey) && instances[instanceKey] == this)
        {
            instances.Remove(instanceKey);
        }
    }

    /// <summary>
    /// Optional: Get the singleton instance by key (useful for debugging or external access)
    /// </summary>
    public static DDOL GetInstance(string key)
    {
        return instances.ContainsKey(key) ? instances[key] : null;
    }

    /// <summary>
    /// Optional: Check if a singleton instance exists for a given key
    /// </summary>
    public static bool HasInstance(string key)
    {
        return instances.ContainsKey(key);
    }
}
