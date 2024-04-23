using UnityEngine;

public class MonoBehaviourSingleton<T> : MonoBehaviour
	where T : Component
{
	private static T _instance;
	public static T Instance
	{
		get
		{
            if (_instance != null) return _instance;

            var instances = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            if (instances.Length > 0)
                _instance = instances[0];

            if (instances.Length > 1)
                Debug.LogError("There is more than one " + typeof(T).Name + " in the scene.");

            return _instance;
		}
	}
}


