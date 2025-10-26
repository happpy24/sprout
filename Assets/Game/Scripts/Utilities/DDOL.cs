using UnityEngine;
using UnityEngine.SceneManagement;

public class DDOL : MonoBehaviour
{
    void Awake()
    {
        DDOL[] ddol = FindObjectsByType<DDOL>(0);
        
        foreach (var obj in ddol)
        {
            if (obj != this && obj.name == name)
            {
                Destroy(gameObject);
                return;
            }
        }

        DontDestroyOnLoad(gameObject);
    }
}
