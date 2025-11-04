using UnityEngine;

public abstract class BaseSingletonClass<T> : MonoBehaviour where T : Component
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance != null) return instance;
            
            instance = FindObjectOfType<T>();
            if (instance != null) return instance;
            
            var gameObject = new GameObject
            {
                name = typeof(T).Name
            };
            instance = gameObject.AddComponent<T>();
            //DontDestroyOnLoad(gameObject);
            return instance;
        }
    }

    public virtual void Awake()
    {
        if (instance)
            Destroy(this);
        DontDestroyOnLoad(this);
        //if (instance == null)
        //{
        //    instance = this as T;
        //    DontDestroyOnLoad(gameObject);
        //}
        //else
        //{
        //    Destroy(gameObject);
        //}
    }
}