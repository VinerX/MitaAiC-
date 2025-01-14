using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("UnityMainThreadDispatcher is not initialized. Make sure it's added to the scene.");
            }
            return _instance;
        }
    }

    private readonly ConcurrentQueue<Action> _actionQueue = new ConcurrentQueue<Action>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Сохраняем объект при смене сцен
        }
        else if (_instance != this)
        {
            Destroy(gameObject); // Уничтожаем дубликаты
        }
    }

    private void Update()
    {
        while (_actionQueue.TryDequeue(out var action))
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Exception while executing action on main thread: {ex}");
            }
        }
    }

    /// <summary>
    /// Добавляет действие для выполнения в главном потоке.
    /// </summary>
    public void Enqueue(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
        _actionQueue.Enqueue(action);
    }
}

