using UnityEngine;

namespace TD.Core
{
    /// <summary>
    /// InputBackRouter：监听 ESC/Back 并路由给 GameController。
    /// </summary>
    [DefaultExecutionOrder(-8000)]
    public class InputBackRouter : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameController.Instance?.OnBackPressed();
            }
        }
    }
}
