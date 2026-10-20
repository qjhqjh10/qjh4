// BlockingOverlay.cs — 从 `ShellParts.cs` 拆出来的独立文件（原处是**次要类**，Unity 建不出 MonoScript）。
//
// 🔴 **为什么必须单开一个文件**（2026-10-11 · `A1506` / `A1481`）：Unity 只给**文件名 == 类名**
//   的那个类建 `MonoScript` 资产；次要类没有 MonoScript ⇒ 存场景时 `m_Script` 指不到资产 ⇒
//   Unity **当场烘一块内联 `!u!115` 存根**写进 level 文件 ⇒ player 加载时 `Position out of bounds!`
//   ⇒ `The file '.../levelN' is corrupted!` + 崩溃。判据与实测 → `资料/交接_第十七会话.md` §三 ·
//   `资料/任务_卡组卡牌战斗.md` 的 `A1506`。
//
// 遮罩层（挡住点击、不画东西）。
//
// 🔴 **2026-10-17（T16 · 壳侧加载）定位变更：本件是【降级路径】，不是原版件。**
//   原版那两件已经照判据建在 `Shell/LoadingMenu.cs`：
//     · `LoadingMenu`        = 全屏黑场（`Safe area Only Horizontal/LoadingMenu`，`LoadingFade(bool, Action)`）；
//     · `LoadingController`  = 转圈件（`MenuLoadingController`：暗底 α0.4667 + 骷髅 α0.7 + 齿轮 `-20°/s`，270.438995²）。
//   本件留着的原因有两条，**都不是「原版长这样」**：
//     ① `Editor/ShellScene.cs` 的自检直接调 `StartSpinning/StopSpinning` 探 `IsBlocking`；
//     ② 图取不到时 `LoadingController` 会报错，此时它是「只剩一块暗底 + 旧圈」的那条兜底。
//   ⚠️ **现读全仓没有任何运行时调用点**（`grep "BlockingOverlay"` 只命中本文件 / `ShellRuntime.Build`（只建不调）/
//      `PointerLayer`（只读 `IsBlocking`）/ `ShellScene` 自检 / 注释）—— 别以为大厅断线在用它（那条走 `NetRuntime.Notice` 那套）。
//   ⚠️ 它画的圈是旧图 `40K_menu_loading`、128px、转速我们挑的 —— **与原版不符**，只当兜底。
//
// 🔴 2026-10-17：渲染队列 3060/3061 → 4070/4071（当时的判据 = 压在全部 shell UI 之上、黑场 4090 之下）。
// 🔴 **2026-10-20（W58 · 遗留 1）：4070/4071 再降到 3338/3339 —— 上面那条判据已过期**（铁律 5）。
//   原版加载遮罩那两件（`MenuLoadingController` = `Default`(0) 层 · `LoadingMenu` = `Blocking` 层）
//   **不在 toast（`PopUps`/900）之上**；真正那两件已建在 `Shell/LoadingMenu.cs`
//   （转圈 `QDark/QSkull/QCog = 3327–3329` · 黑场 `QPanel = 4090`）⇒ 本件作为**备用加载遮罩**跟着落回同一档。
//   取值 = `GenericOptionsPanel` 带尾（`QHit = 3337`）与 `AllianceMemberOptionsPopup` 带首
//   （`QShade = 3340`）之间**唯一的双槽空隙 3338/3339** —— 与转圈件 3327–3329 同族、不抢它的号、
//   也不落进任何窗的号段（再往上 3340→3487 是连续弹窗带，只剩单槽空隙、容不下 dim+spinner 两件）。
//   ⚠️ **已知残留（如实记）**：它因此高于 `GenericOptionsPanel` 那一带弹窗 —— 与转圈件「高于
//   `TrophyInfoPopup` 带」是同一类倒置（那张表里那个空隙是 2 槽，容不下第三、四件）；
//   本件**零运行时调用点**、只在降级路径被手动打开，与任何弹窗都不同屏 ⇒ 今天不可见。

using System.Collections;
using UnityEngine;

namespace CardPresentation
{
    public class BlockingOverlay : MonoBehaviour
    {
        /// <summary>有没有东西在挡输入。菜单的点击/拖拽入口都要问它一句。
        /// 消费者 = `PointerLayer.InputBlocked`（2026-10-17 `N8` 接上）+ 自检；
        /// ⚠️ **`StartSpinning` 现读全仓仍零运行时调用点** —— 它只在降级路径/自检里被手动打开。</summary>
        public static bool IsBlocking { get; private set; }

        /// <summary>渲染队列（⚠️ **[我们挑的]梯子**，判据与取舍 → 文件头 2026-10-20 那一段）。
        /// 取 `GenericOptionsPanel` 带尾 3337 与 `AllianceMemberOptionsPopup` 带首 3340 之间的空隙。</summary>
        public const int QDim = 3338, QSpinner = 3339;

        ImageQuad _dim, _spinner;

        void Awake() { Build(); SetActiveState(false); }

        void Build()
        {
            if (_dim != null) return;
            _dim = ImageQuad.Create(transform, CardArt.Solid(), Vector3.zero, LayoutSpace.DesignHeight,
                                    new Vector2(0.5f, 0.5f), "BlockingOverlay");
            if (_dim != null)
            {
                _dim.SetAspect(1f);
                _dim.SetTint(new Color(0f, 0f, 0f, 0.35f));
                _dim.SetRenderQueue(QDim);      // 备用加载遮罩档（降级件）：与转圈件 3327–3329 同族，见文件头
            }

            var tex = CardArt.MenuUi("40K_menu_loading");
            if (tex != null)
                _spinner = ImageQuad.Create(transform, tex, Vector3.zero, 128f / 108f,
                                            new Vector2(0.5f, 0.5f), "Spinner");
            if (_spinner != null) _spinner.SetRenderQueue(QSpinner);
        }

        void SetActiveState(bool on)
        {
            IsBlocking = on;
            Build();
            if (_dim != null) _dim.gameObject.SetActive(on);
            if (_spinner != null) _spinner.gameObject.SetActive(on);
        }

        public void StartSpinning()
        {
            SetActiveState(true);
            if (_spinner == null)
                Debug.Log("[Shell] 转了圈但**没有转圈图**（`40K_menu_loading` 取不到）" +
                          "—— 现在只挡输入、不画圈。导出器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
        }

        public void StopSpinning() { SetActiveState(false); }

        /// <summary>照原版 `AutoStopSpinner`：`sec` 秒后自己停。</summary>
        public void AutoStopSpinner(float sec) { StartCoroutine(AutoStop(sec)); }

        IEnumerator AutoStop(float sec)
        {
            yield return new WaitForSecondsRealtime(sec);
            StopSpinning();
        }

        void Update()
        {
            if (_spinner != null && _spinner.gameObject.activeSelf)
                _spinner.transform.Rotate(0f, 0f, -180f * Time.unscaledDeltaTime);   // ⚠️ 转速我们挑的（原版查不到）
        }
    }
}