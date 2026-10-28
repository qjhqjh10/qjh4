// GenericScoreBarLine.cs —— 原版 `GenericScoreBarLine`（MonoBehaviour：一行里程碑 = 分数 + 箱子 + 高亮）
//
// ============================ 这是什么 ============================
// `MilestoneScoreBar.scoringLines` 里的一行（prefab 里 5 行，节点名 `Score Bar Line Level 1..5`）。
// 一行上有四件：`Score`（数字）/ `Chest`（宝箱图）/ `Highlight Crate`（「可领」高亮）/ `Skull`（资源小图）。
//
// ============================ 判据（三源现读，2026-10-29 B17）============================
//   · 字段/签名 = `d:/2/tools/il2cpp_out/dump.cs:111168-111190`
//       `[SerializeField] private EverguildTextMeshPro score;   // 0x20`  ← 节点 `Score`
//       `[SerializeField] private EverguildButton chestReward;  // 0x28`  ← 节点 `Chest`
//       `[SerializeField] private Sprite chestOpen;             // 0x30`
//       `[SerializeField] private GameObject readyToCollect;    // 0x38`  ← 节点 `Highlight Crate`
//       `[SerializeField] private Image resourceIcon;           // 0x40`  ← 节点 `Skull`
//       `private Sprite originalChest;                          // 0x48`（**不序列化**）
//       `public void Initialize(GenericScoringMilestone milestone, Sprite resourceIcon)`
//     （节点名 = 现读 `bundle_menus_assets_all/MonoBehaviour/*.json` 里 15 个实例的 PPtr 反查，
//      逐实例一致：`score→Score` · `chestReward→Chest` · `readyToCollect→Highlight Crate` · `resourceIcon→Skull`）
//   · 方法体 = `d:/2/tools/decomp_full/GenericScoreBarLine__Initialize.c`（VA 级，**逐句实读**）：
//       ```
//       if (milestone == null) return;                                   // 原版这一支直接 return
//       if (originalChest == null) originalChest = chestReward.image.sprite;   // ★ 第一次调用时记下 prefab 出厂图
//       if (resourceIconSprite != null) resourceIcon.sprite = resourceIconSprite;
//       score.text = milestone.score.ToString();                          // +0x14 = score
//       switch (milestone.state /* +0x10 */) {
//         case Locked(0):    chestReward.image.sprite = originalChest; readyToCollect.SetActive(false); break;
//         case Unlocked(1):  chestReward.image.sprite = originalChest; readyToCollect.SetActive(true);  break;   // ★ 只差这一格
//         case Collected(2): chestReward.image.sprite = chestOpen;     readyToCollect.SetActive(false); break;
//         default: throw ArgumentOutOfRangeException;
//       }
//       chestReward.onClick.RemoveAllListeners();
//       chestReward.onClick.AddListener(() => RewardService.Preview(milestone.rewards, null, null,
//                                                                  isPremiumLocked: true, customTitle: default));
//       ```
//     🔴 **`Locked` 与 `Unlocked` 的箱子图是同一张 `originalChest`** —— 两档**只差 `readyToCollect` 的显隐**。
//        （凭直觉写成「Unlocked 换开箱图」是错的；`Collected` 才换 `chestOpen`。）
//
// ⚠️ 差异（如实标）：
//   ① `EverguildButton`（: `UnityEngine.UI.Button`）→ 我们**没有** uGUI 运行时 ⇒
//      `chestReward` 用 `ImageQuad`（图那一半），点击走 <see cref="ChestClicked"/>（语义 = 原版那个 lambda）。
//      原版 `onClick.RemoveAllListeners()/AddListener` 那两句在 <see cref="Initialize"/> 里**不需要**：
//      我们只有一个入口，不存在「旧监听残留」。
//   ② `Sprite` / `Image` → `Texture2D` / `ImageQuad`（本工程的图一路都是 Texture2D）。
//   ③ 原版 `chestReward == null` / `chestReward.image == null` 是 **NRE**；我们出声 + 跳过那一半。
using UnityEngine;

namespace CardPresentation.ScoreBars
{
    /// <summary>原版 `GenericScoreBarLine`：一行里程碑（数字 + 箱子 + 可领高亮 + 资源小图）。</summary>
    public class GenericScoreBarLine : MonoBehaviour
    {
        // 🔴 字段名逐字照 `dump.cs`（断言按反射钉死）。
        [SerializeField] private Label score;                 // 原版 `+0x20`（节点 `Score`）
        [SerializeField] private ImageQuad chestReward;       // 原版 `+0x28`（节点 `Chest`）
        [SerializeField] private Texture2D chestOpen;         // 原版 `+0x30`
        [SerializeField] private GameObject readyToCollect;   // 原版 `+0x38`（节点 `Highlight Crate`）
        [SerializeField] private ImageQuad resourceIcon;      // 原版 `+0x40`（节点 `Skull`）

        /// <summary>原版 `+0x48`（**不序列化**）：第一次 `Initialize` 时从 `chestReward` 上记下来的「出厂箱子图」。
        /// <para>🔴 它是**一次性快照**（`if (originalChest == null)` 那一句）⇒ 之后再换 `chestReward` 的图**不会**更新它。</para>
        /// <para>⚠️ 类型用 `Texture`（`ImageQuad.Texture` 就是这个类型；原版是 `Sprite`）—— `Texture2D` 派生自它，
        /// 需要 `Texture2D` 的调用点自行 `as`。</para></summary>
        [System.NonSerialized] private Texture originalChest;

        /// <summary>上一次喂进来的里程碑（原版 lambda 闭包捕获的那一份；我们换成字段）。</summary>
        [System.NonSerialized] private GenericScoringMilestone _milestone;

        /// <summary>`HighLight Crate` 现在是不是亮着（自检读口；= 原版 `readyToCollect.activeSelf`）。</summary>
        public bool ReadyToCollectVisible { get { return readyToCollect != null && readyToCollect.activeSelf; } }

        /// <summary>`Chest` 现在的图（自检读口）。</summary>
        public Texture ChestTexture { get { return chestReward != null ? chestReward.Texture : null; } }

        /// <summary>第一次 `Initialize` 记下来的出厂箱子图（自检读口）。</summary>
        public Texture OriginalChest { get { return originalChest; } }

        /// <summary>绑宿主建好的五件（原版由 prefab 序列化给）。</summary>
        public void Bind(Label scoreLabel, ImageQuad chest, Texture2D openTexture,
                         GameObject readyNode, ImageQuad icon)
        {
            score = scoreLabel;
            chestReward = chest;
            chestOpen = openTexture;
            readyToCollect = readyNode;
            resourceIcon = icon;
        }

        /// <summary>原版 `public void Initialize(GenericScoringMilestone milestone, Sprite resourceIcon)`
        /// —— 逐句见文件头。`milestone == null` ⇒ **什么都不做并返回**（原版那一支就是 `return`）。</summary>
        public void Initialize(GenericScoringMilestone milestone, Texture2D resourceIconSprite = null)
        {
            if (milestone == null) return;
            _milestone = milestone;

            // ① `if (originalChest == null) originalChest = chestReward.image.sprite`（一次性快照）
            if (originalChest == null)
            {
                if (chestReward != null) originalChest = chestReward.Texture;
                else Debug.LogWarning("[GenericScoreBarLine] `chestReward` 没绑 ⇒ 记不到出厂箱子图"
                                    + "（原版这一跳是 NRE；我们出声继续）");
            }

            // ② `if (resourceIconSprite != null) resourceIcon.sprite = resourceIconSprite`
            if (resourceIconSprite != null)
            {
                if (resourceIcon != null) resourceIcon.SetTexture(resourceIconSprite);
                else Debug.LogWarning("[GenericScoreBarLine] `resourceIcon` 没绑 ⇒ 资源小图不换（原版写 `Image.sprite`）");
            }

            // ③ `score.text = milestone.score.ToString()`（`+0x14`）
            if (score != null) score.SetText(milestone.score.ToString());
            else Debug.LogWarning("[GenericScoreBarLine] `score` 没绑 ⇒ 档位数字不写（原版写 `EverguildTextMeshPro.text`）");

            // ④ 三档开关（🔴 Locked 与 Unlocked 的箱子图**同一张**，只差高亮的显隐）
            switch (milestone.state)
            {
                case GenericScoringMilestoneState.Locked:       // 0
                    SetChest(originalChest);
                    SetReady(false);
                    break;
                case GenericScoringMilestoneState.Unlocked:     // 1
                    SetChest(originalChest);
                    SetReady(true);                            // ★ 与 Locked 的唯一区别
                    break;
                case GenericScoringMilestoneState.Collected:    // 2
                    SetChest(chestOpen);
                    SetReady(false);
                    break;
                default:
                    // 原版这一支是 `ArgumentOutOfRangeException`；我们出声 + 不动画面（不让坏数据崩游戏）
                    Debug.LogError("[GenericScoreBarLine] `milestone.state` = " + (int)milestone.state
                                 + " 不在 Locked/Unlocked/Collected 里（原版抛 `ArgumentOutOfRangeException`）⇒ 画面不动");
                    return;
            }

            // ⑤ 原版末尾把 lambda 挂到 `chestReward.onClick` 上；我们的等价入口 = `ChestClicked()`
            //    （宿主在 `MenuDraw.Hit` 的回调里调它）。见 `ChestClicked`。
        }

        /// <summary>原版那个 lambda（`GenericScoreBarLine.__c__DisplayClass6_0___Initialize_b__0`，VA 级可读）：
        /// `RewardService.Preview(milestone.rewards, null, null, isPremiumLocked: true, customTitle: default)`。
        /// <para>⚠️ `isPremiumLocked` **恒 true**（原版那里传的就是 `1`）—— 预览态一律按「可能是付费档」显示。</para></summary>
        public void ChestClicked()
        {
            if (_milestone == null)
            {
                Debug.LogWarning("[GenericScoreBarLine] `ChestClicked` 在 `Initialize` 之前被调 ⇒ 没有里程碑可预览（不出声就会变成「点了没反应」）");
                return;
            }
            RewardWindow.ShowPreview(_milestone.rewards, null, true, null);
        }

        private void SetChest(Texture tex)
        {
            if (chestReward == null) return;
            // 原版 `chestReward.image.sprite = x`；`x == null` 也照写（原版就是直接赋 null ⇒ 图消失）
            chestReward.SetTexture(tex);
        }

        private void SetReady(bool on)
        {
            if (readyToCollect == null) return;
            readyToCollect.SetActive(on);
        }
    }
}
