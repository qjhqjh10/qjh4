// SurvivalEventWindowEnemyMarker.cs — **生存事件窗的「敌方标记」**（原版同名类的等价物）
//
// ============================ 出处（唯一正本）============================
// 原版类 = `SurvivalEventWindowEnemyMarker : MonoBehaviour`（`dump.cs:66202`，TypeDefIndex 1648）
//   · 字段：`EnemyNameText`(TMP_Text, +0x20) · `HiddenEnemyVisual`(GameObject, +0x28) ·
//           `RevealedEnemyVisual`(GameObject, +0x30) —— 三个都是 `[SerializeField] protected`。
//   · 方法体（第一权威 = `decomp_full/SurvivalEventWindowEnemyMarker__{Awake,SetEnemyName,SetHiddenEnemy}.c`，
//     可读版 = `cpp2il/out_cpp2il_dev_ilspy/SurvivalEventWindowEnemyMarker.cs:12-29`，两份**逐句一致**）：
//       `Awake()`            = `HiddenEnemyVisual.SetActive(true);  RevealedEnemyVisual.SetActive(false);`
//       `SetEnemyName(name)` = `EnemyNameText.text = name; HiddenEnemyVisual.SetActive(false); HiddenEnemyVisual.SetActive(true);`
//       `SetHiddenEnemy()`   = `HiddenEnemyVisual.SetActive(true);  RevealedEnemyVisual.SetActive(false);`
//   ⚠️ `Awake` 与 `SetHiddenEnemy` 在 `dump.cs` 里**同一个 RVA `0x6C8360`**（ICF 折叠/同体），体确实一样。
//   ⚠️ `SetEnemyName` 那两句 `SetActive(false) → SetActive(true)` **不是笔误**：原版靠「关再开」让
//      `HiddenEnemyVisual` 上挂的动画/特效**从头重播**，然后写名字。⛔ 别「优化」成一句。
//
// ============================ 我们这一侧（如实标注边界）============================
// 🔴 **本机既没有生存事件窗，也没有这套 prefab**（2026-10-28 现查：`assets_full` 全盘
//   `find -iname "*Survival*"` = **0 命中** · `_tmp_view/apk135`（安卓 1.35.0 远端包）的
//   `catalog_strings.txt` / `monoscripts.json` 里 `survival` 也是 **0 命中** ·
//   `assetripper_out/ExportedProject` 里只有**这个类的脚本桩**、没有任何 prefab 实例引用它）。
//   ⇒ **三个视觉节点（名字文本 / 隐藏标记 / 已揭示标记）的几何、sprite、动画全部拿不到**，
//     所以本文件**只复刻行为**（三句 SetActive + 一次写文本），视觉件由**将来那扇生存事件窗**注入。
//
// 🔴 **今天没有调用方**（真缺口，不是省事）：唯一该造它的是「生存事件窗」，而本工程**没有**这条线
//   （`Shell/` 下 `grep -i survival` = 0）。⇒ 本类是**已备好的宿主**，接线方 = 建生存事件窗时
//   `SurvivalEventWindowEnemyMarker.Bind(nameText, hiddenVisual, revealedVisual)`。
//   ⛔ 不许因为「没人用」就把它删掉（铁律 11：与原版的差都要补齐，只有先后之分）。
//
// ⚠️ 本类**故意不是 `MonoBehaviour`**：Unity 只给「类名 == 文件名」的类建脚本资产，而
//   `AddComponent<T>` 对「有脚本资产、且在编辑器程序集里」的类才成功（本项目踩过整月，见 CLAUDE.md）。
//   这里用**普通 C# 类 + 三个句柄**，与 `RankedDivisionInfo` / `MissionInfoDisplays` 同一套做法。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `SurvivalEventWindowEnemyMarker` —— 生存事件窗里那个「敌方是隐藏的 / 是谁」的标记。
    /// <para>三条行为逐句照原版（见文件头）；视觉件（名字文本 + 两个 GameObject）由外部注入。</para>
    /// <para>判据 → `decomp_full/SurvivalEventWindowEnemyMarker__Awake.c` · `__SetEnemyName.c` · `__SetHiddenEnemy.c`。</para></summary>
    public sealed class SurvivalEventWindowEnemyMarker
    {
        /// <summary>原版 `EnemyNameText`（TMP_Text）；我们没有那棵 prefab ⇒ 用 `Label`（它有 TMP/点阵两档后端）。</summary>
        Label _nameText;
        /// <summary>原版 `HiddenEnemyVisual` —— 「敌将未揭示」那一版（头像剪影 + 动画）。</summary>
        GameObject _hiddenVisual;
        /// <summary>原版 `RevealedEnemyVisual` —— 「已揭示」那一版。</summary>
        GameObject _revealedVisual;

        /// <summary>三个句柄都接上、并跑一次原版 `Awake()`。返回可用实例；任一句柄为 null ⇒ 出声后仍返回
        /// （⛔ 不静默：少一个件就是「原版那块不会动」，必须看得见）。</summary>
        public static SurvivalEventWindowEnemyMarker Bind(Label nameText, GameObject hiddenVisual, GameObject revealedVisual)
        {
            var m = new SurvivalEventWindowEnemyMarker
            {
                _nameText = nameText,
                _hiddenVisual = hiddenVisual,
                _revealedVisual = revealedVisual,
            };
            if (nameText == null) Debug.LogWarning("[Survival] `EnemyNameText` 没给 ⇒ `SetEnemyName` 只会切标记、**不写名字**");
            if (hiddenVisual == null) Debug.LogWarning("[Survival] `HiddenEnemyVisual` 没给 ⇒ 原版那句 `SetActive(true)` **没有落点**");
            if (revealedVisual == null) Debug.LogWarning("[Survival] `RevealedEnemyVisual` 没给 ⇒ 原版那句 `SetActive(false)` **没有落点**");
            m.Awake();
            return m;
        }

        /// <summary>= 原版 `Awake()`：**隐藏标记开、已揭示标记关**（敌人是未知的那一个）。</summary>
        public void Awake()
        {
            SetActive(_hiddenVisual, true, "HiddenEnemyVisual");
            SetActive(_revealedVisual, false, "RevealedEnemyVisual");
        }

        /// <summary>= 原版 `SetEnemyName(string name)`：写名字 + 把隐藏标记**关再开**（重播它自己的动画）。
        /// <para>⚠️ 原版这**两句**（false→true）是一次「重启」，⛔ 别合成一句；它**不**碰 `RevealedEnemyVisual`。</para></summary>
        public void SetEnemyName(string name)
        {
            if (_nameText != null) _nameText.SetText(name ?? string.Empty);
            SetActive(_hiddenVisual, false, "HiddenEnemyVisual");
            SetActive(_hiddenVisual, true, "HiddenEnemyVisual");
        }

        /// <summary>= 原版 `SetHiddenEnemy()`：回到「未揭示」那一态（隐藏标记开、已揭示标记关）。</summary>
        public void SetHiddenEnemy()
        {
            SetActive(_hiddenVisual, true, "HiddenEnemyVisual");
            SetActive(_revealedVisual, false, "RevealedEnemyVisual");
        }

        /// <summary>自检读口：隐藏标记此刻开着吗（`null` ⇒ `false`）。</summary>
        public bool HiddenVisible { get { return _hiddenVisual != null && _hiddenVisual.activeSelf; } }
        /// <summary>自检读口：已揭示标记此刻开着吗。</summary>
        public bool RevealedVisible { get { return _revealedVisual != null && _revealedVisual.activeSelf; } }
        /// <summary>自检读口：名字文本（没有文本件 ⇒ `null`）。</summary>
        public string EnemyName { get { return _nameText != null ? _nameText.Text : null; } }

        /// <summary>`SetActive` 的唯一出口（原版三句都是这一个调用面）。件缺失时**出声一次**而不是空转。</summary>
        static void SetActive(GameObject go, bool on, string who)
        {
            if (go == null)
            {
                Debug.LogWarning("[Survival] `" + who + "` 是 null ⇒ 原版那句 `SetActive(" + on + ")` **没执行**（⛔ 不静默）");
                return;
            }
            if (go.activeSelf == on) return;
            go.SetActive(on);
        }
    }
}
