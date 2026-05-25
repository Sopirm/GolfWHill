using System.Collections.Generic;
using UnityEngine;

public class EnergyOrbSpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnergyOrbSettings
    {
        public EnergySystem.EnergyType energyType;
        [Min(1)] public int count = 5;
        [Min(1)] public int amountPerOrb = 10;
        [Min(0.25f)] public float scale = 1f;
        public Color color = Color.red;
    }

    [SerializeField] private Vector3 spawnArea = new Vector3(12f, 0f, 12f);
    [SerializeField] private float heightOffset = 1f;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private Transform container;
    [SerializeField] private List<EnergyOrbSettings> orbSettings = new List<EnergyOrbSettings>();

    private readonly List<GameObject> spawnedOrbs = new List<GameObject>();
    private bool hasSpawnedInPlayMode;

    private void Reset()
    {
        EnsureDefaultSettings();
    }

    private void OnValidate()
    {
        EnsureDefaultSettings();
    }

    private void OnEnable()
    {
        if (Application.isPlaying && spawnOnStart && !hasSpawnedInPlayMode)
        {
            SpawnAllOrbs();
            hasSpawnedInPlayMode = true;
        }
    }

    private void Start()
    {
        if (spawnOnStart && !hasSpawnedInPlayMode)
        {
            SpawnAllOrbs();
            hasSpawnedInPlayMode = true;
        }
    }

    [ContextMenu("Spawn All Orbs")]
    public void SpawnAllOrbs()
    {
        EnsureDefaultSettings();
        ClearSpawnedOrbs();

        int totalSpawned = 0;
        foreach (EnergyOrbSettings settings in orbSettings)
        {
            for (int i = 0; i < settings.count; i++)
            {
                SpawnSingleOrb(settings, i);
                totalSpawned++;
            }
        }

        Debug.Log($"EnergyOrbSpawner: spawned {totalSpawned} energy orbs on {gameObject.name}.");
    }

    [ContextMenu("Clear Spawned Orbs")]
    public void ClearSpawnedOrbs()
    {
        for (int i = spawnedOrbs.Count - 1; i >= 0; i--)
        {
            if (spawnedOrbs[i] != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(spawnedOrbs[i]);
                }
                else
                {
                    DestroyImmediate(spawnedOrbs[i]);
                }
            }
        }

        spawnedOrbs.Clear();
    }

    private void SpawnSingleOrb(EnergyOrbSettings settings, int index)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = $"{settings.energyType}EnergyOrb_{index + 1}";
        orb.transform.SetParent(container != null ? container : transform);
        orb.transform.position = GetRandomSpawnPosition();
        orb.transform.localScale = Vector3.one * settings.scale;

        Collider orbCollider = orb.GetComponent<Collider>();
        if (orbCollider != null)
        {
            orbCollider.isTrigger = true;
        }

        EnergyPickup pickup = orb.AddComponent<EnergyPickup>();
        pickup.Configure(settings.energyType, settings.amountPerOrb);

        Renderer renderer = orb.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader != null)
            {
                renderer.material = new Material(shader);
                renderer.material.color = settings.color;
            }
        }

        Rigidbody rigidbodyComponent = orb.AddComponent<Rigidbody>();
        rigidbodyComponent.isKinematic = true;
        rigidbodyComponent.useGravity = false;

        OrbFloatMotion floatMotion = orb.AddComponent<OrbFloatMotion>();
        floatMotion.Initialize(settings.scale);

        spawnedOrbs.Add(orb);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 halfArea = spawnArea * 0.5f;
        float x = Random.Range(-halfArea.x, halfArea.x);
        float z = Random.Range(-halfArea.z, halfArea.z);
        float y = heightOffset + Random.Range(-0.25f, 0.25f);
        return transform.position + new Vector3(x, y, z);
    }

    private void EnsureDefaultSettings()
    {
        if (orbSettings != null && orbSettings.Count > 0)
        {
            return;
        }

        orbSettings = new List<EnergyOrbSettings>
        {
            new EnergyOrbSettings
            {
                energyType = EnergySystem.EnergyType.Red,
                count = 5,
                amountPerOrb = 10,
                scale = 1f,
                color = new Color(0.9f, 0.2f, 0.2f)
            },
            new EnergyOrbSettings
            {
                energyType = EnergySystem.EnergyType.Blue,
                count = 5,
                amountPerOrb = 10,
                scale = 1f,
                color = new Color(0.2f, 0.4f, 1f)
            },
            new EnergyOrbSettings
            {
                energyType = EnergySystem.EnergyType.Green,
                count = 5,
                amountPerOrb = 10,
                scale = 1f,
                color = new Color(0.2f, 0.85f, 0.3f)
            }
        };
    }
}
