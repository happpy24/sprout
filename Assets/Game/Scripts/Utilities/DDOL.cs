using UnityEngine;
using UnityEngine.SceneManagement;

public class DDOL : MonoBehaviour
{
    private static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PreSceneLoadCleanup()
    {
        var ddols = FindObjectsByType<DDOL>(FindObjectsSortMode.None);
        for (int i = 0; i < ddols.Length; i++)
        {
            var obj = ddols[i];
            var sameName = System.Array.FindAll(ddols, o => o.name == obj.name);
            if (sameName.Length > 1)
            {
                // Destroy all but the first
                for (int j = 1; j < sameName.Length; j++)
                    DestroyImmediate(sameName[j].gameObject);
            }
        }
    }

    void Awake()
    {
        DDOL[] ddol = FindObjectsByType<DDOL>(FindObjectsSortMode.None);
        
        foreach (var obj in ddol)
        {
            if (obj != this && obj.name == name)
            {
                DestroyImmediate(gameObject);
                return;
            }
        }

        DontDestroyOnLoad(gameObject);
    }
}
