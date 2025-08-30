using System.Threading.Tasks;
using UnityEngine;

namespace TD.UI.Panels
{
    /// <summary>
    /// ConfirmDialogPanel：通用确认对话框面板（最小占位）。
    /// </summary>
    public class ConfirmDialogPanel : UIPanel
    {
        public override Task OnShowAsync(object args)
        {
            gameObject.name = "[ConfirmDialogPanel]";
            IsModal = true;
            return base.OnShowAsync(args);
        }
    }
}
