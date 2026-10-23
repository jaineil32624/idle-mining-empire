using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Waits one frame after a reset (so destroyed objects are gone) and boots a fresh game.</summary>
    public class Rebooter : MonoBehaviour
    {
        int frames;

        void Update()
        {
            if (++frames < 2) return;
            GameManager.Boot();
            Destroy(gameObject);
        }
    }
}
