using UnityEngine;

public class OrbFloatMotion : MonoBehaviour
{
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float rotationSpeed = 45f;

    private Vector3 startPosition;

    public void Initialize(float scale)
    {
        floatAmplitude *= Mathf.Max(0.5f, scale);
        rotationSpeed *= Mathf.Max(0.75f, scale);
    }

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = startPosition + new Vector3(0f, offset, 0f);
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}
