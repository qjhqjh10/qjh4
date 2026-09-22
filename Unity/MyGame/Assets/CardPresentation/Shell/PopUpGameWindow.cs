// PopUpGameWindow.cs — 通用弹窗（1~2 个按钮）
//
// ============================ 出处 / 诚实标注 ============================
// 行为照原版：`WindowsManager.ShowPopUp(text, localizeTexts, closeOnEsc, …)`（3 个重载）→
//   `LoadPopUpAndShow` / `LoadPopup(twoButtons)` 协程 → `popupWindowOneButton` / `popupWindowTwoButtons`
//   → `HidePopUp(forceHide)`；按钮 = `GameWindowButton{Text, OnPress}`。
//   （`资料/阶段二_Shell_原版规格.md` §三 第 8 条）
//
// 🔴 **版式不是照抄的**：原版那两个 popup prefab（`popupWindowOneButton/TwoButtons`）**本地没有**
//   —— 全 `assets_full` grep 那两个字段名**零命中**（正本 §五 第 2 条）。
//   ⚠️ 但**同族的 `GenericPromptWindow [1139]` 在 `bundle_menus_assets_all` 里、参数齐全**
//   （它被点名当「暂无服务器」的宿主）⇒ **0b 批次拿到它的表之后，这里的版面要换成它**。
//   在那之前：底板用 `40k_popup.png`（原版图，359×336），按钮用 `40k_general_bt_yellow_confirm/close`（71²）。
using UnityEngine;

namespace CardPresentation
{
    public class PopUpGameWindow : GameWindow
    {
        public const int PanelW = 640;      // ⚠️ 我们挑的（原版值待 `GenericPromptWindow` 表）
        public const int PanelH = 360;
        public const int BtnSize = 71;      // 实证：`40k_general_bt_yellow_*` 图就是 71×71

        string _text, _okText, _cancelText;
        System.Action _onOk, _onCancel;
        Label _label;

        public static PopUpGameWindow Create(WindowsManager mgr, string text, string okText, System.Action onOk,
                                             string cancelText, System.Action onCancel)
        {
            var go = new GameObject("PopUpWindow");
            var win = go.AddComponent<PopUpGameWindow>();
            win.type = WindowType.Popup;
            win.placement = WindowsPlacement.Popup;   // 实证：典型窗口 `RatePopup` / `RewardWindow` 都是 place 15
            win.closeOnEsc = true;                    // ⚠️ 逐窗不同（`RatePopup` 实证 = 0）—— 通用弹窗按 true
            win._text = text; win._okText = okText ?? "确认"; win._cancelText = cancelText;
            win._onOk = onOk; win._onCancel = onCancel;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            Build();
        }

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--)
#if UNITY_EDITOR
                DestroyImmediate(root.GetChild(i).gameObject);
#else
                Destroy(root.GetChild(i).gameObject);
#endif

            var panel = ImageQuad.Create(root, CardArt.DeckUi("40k_popup"),
                                         LayoutSpace.ToWorld(0.5f, 0.5f), PanelH / 108f,
                                         new Vector2(0.5f, 0.5f), "Panel");
            if (panel != null) panel.SetRenderQueue(3020);

            bool two = !string.IsNullOrEmpty(_cancelText);
            float btnY = -PanelH * 0.5f + BtnSize * 0.5f + 24f;      // 面板底边往上留 24px
            float dx = BtnSize * 0.75f;
            if (two)
            {
                MakeButton(root, "40k_general_bt_yellow_confirm", -dx, btnY, _okText, () => { Choose(true); });
                MakeButton(root, "40k_general_bt_yellow_close", dx, btnY, _cancelText, () => { Choose(false); });
            }
            else
            {
                MakeButton(root, "40k_general_bt_yellow_confirm", 0f, btnY, _okText, () => { Choose(true); });
            }

            // 文案（原版是本地化词条；我们**不造词条**，直接用传进来的字符串 —— 查不到语言表，见正本）
            _label = Label.Create(root, _text, Pixel(0f, PanelH * 0.5f - 120f), 6,
                                  Color.white, new Vector2(0.5f, 0.5f), "Text");
            if (_label != null)
            {
                _label.SetRenderQueue(3021);
                _label.SetWrapWidth(PanelW * 0.82f / 108f);
            }
        }

        ImageQuad MakeButton(Transform root, string art, float dx, float y, string text, System.Action act)
        {
            var q = ImageQuad.Create(root, CardArt.DeckUi(art), Pixel(dx, y), BtnSize / 108f,
                                     new Vector2(0.5f, 0.5f), art);
            if (q == null) return null;
            q.SetRenderQueue(3021);
            var hit = q.gameObject.AddComponent<WindowButton>();
            hit.onClick = act;

            // 按钮文案：原版 `GameWindowButton{Text, OnPress}` **是带文案的** ⇒ 别只画一个光秃秃的图标
            //（玩家看不出那颗勾是「确定」还是「关闭」）。文案画在圆钮下方。
            if (!string.IsNullOrEmpty(text))
            {
                var lb = Label.Create(root, text, Pixel(dx, y - BtnSize * 0.5f - 18f), 5,
                                      Color.white, new Vector2(0.5f, 0.5f), art + " Text");
                if (lb != null) lb.SetRenderQueue(3021);
            }
            return q;
        }

        void Choose(bool ok)
        {
            var a = ok ? _onOk : _onCancel;
            Close();
            a?.Invoke();     // ⚠️ 先关再回调（照原版 `HidePopUp` 在 `OnPress` 之后）—— 回调里可能再开窗
        }

        /// <summary>面板局部像素 → 世界坐标（锚点在屏幕中心）。</summary>
        static Vector3 Pixel(float x, float y)
        {
            var c = LayoutSpace.ToWorld(0.5f, 0.5f);
            return new Vector3(c.x + x / 108f, c.y + y / 108f, c.z);
        }
    }

    /// <summary>弹窗按钮的点击接收（原版 `GameWindowButton` 的最小等价物）。</summary>
    public class WindowButton : MonoBehaviour
    {
        public System.Action onClick;

        void OnMouseUpAsButton() { onClick?.Invoke(); }

        /// <summary>自检用：批处理里没有鼠标事件，直调这条路（**和 `OnMouseUpAsButton` 同一个 action**）。</summary>
        public void ClickForTest() { onClick?.Invoke(); }
    }
}
