using UnityEngine;

/// <summary>
/// Rotates the transform continuously around Z axis.
/// Attach to any GameObject that should spin (e.g. saw blade tower).
/// </summary>
public class SpinForever : MonoBehaviour
{
    [SerializeField] private float _degreesPerSecond = 540f;

    private void Update()
    {
        transform.Rotate(0f, 0f, _degreesPerSecond * Time.deltaTime);
    }
}
