using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Rotates (speed in degrees/sec) or gently wobbles (speed 0) a UI element.</summary>
    public class Spin : MonoBehaviour
    {
        public float speed;
        float t;

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (Mathf.Abs(speed) > 0.01f) transform.Rotate(0, 0, speed * Time.unscaledDeltaTime);
            else transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2.5f) * 8f);
        }
    }
}
