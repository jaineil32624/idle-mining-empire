using UnityEngine;

namespace IdleMiningEmpire
{
    /// <summary>Fades a toast notification out after a few seconds.</summary>
    public class ToastLife : MonoBehaviour
    {
        public float life = 3.2f;
        CanvasGroup group;

        void Awake()
        {
            group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        void Update()
        {
            life -= Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(life * 2f);
            if (life <= 0) Destroy(gameObject);
        }
    }
}
