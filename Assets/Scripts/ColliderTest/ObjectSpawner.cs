using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject[] objectPrefabs; // Массив префабов объектов для спавна
    public float spawnInterval = 1f; // Интервал между спавном объектов
    public float spawnHeight = 10f; // Высота, на которой будут появляться объекты
    public float spawnRadius = 5f; // Радиус вокруг спавнера, в котором будут появляться объекты

    private void Start()
    {
        // Запускаем повторяющийся вызов функции SpawnObject
        // InvokeRepeating("SpawnObject", 0f, spawnInterval);
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnObject();
        }
    }

    void SpawnObject()
    {
        Debug.Log("Spawning an object..."); // Добавлено для отладки
        if (objectPrefabs.Length == 0)
        {
            Debug.LogWarning("ObjectPrefabs array is empty. Please assign prefabs in the Inspector.");
            return;
        }

        // Выбираем случайный префаб из массива
        int randomIndex = Random.Range(0, objectPrefabs.Length);
        GameObject objectToSpawn = objectPrefabs[randomIndex];

        // Генерируем случайную позицию в пределах spawnRadius вокруг спавнера
        Vector3 randomSpawnPosition = transform.position + new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            spawnHeight,
            Random.Range(-spawnRadius, spawnRadius)
        );

        // Создаем объект
        Instantiate(objectToSpawn, randomSpawnPosition, Quaternion.identity);
    }
}
