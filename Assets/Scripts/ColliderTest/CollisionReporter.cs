using UnityEngine;

public class CollisionReporter : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Столкновение с объектом: " + collision.gameObject.name);
    }
}