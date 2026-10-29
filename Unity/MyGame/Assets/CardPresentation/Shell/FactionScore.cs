// FactionScore.cs —— 原版 `FactionScore`（MonoBehaviour：阵营徽记 + 两颗评级）
//
// ============================ 这是什么 ============================
// 一行「某阵营的分数」：左边阵营徽记（节点 `icon`）+ 两颗 `AllianceRatingDisplay`
// （第一颗 = 当前分、第二颗 = 最高分）。两套 prefab，**结构不同、⛔ 别当成一份**（见 §变体）。
//
// ============================ 判据（三源现读，2026-10-29 B17）============================
//   · 字段/签名 = `d:/2/tools/il2cpp_out/dump.cs:82495-82512`
//       `[SerializeField] private Image factionImage;                         // 0x20` ← 节点 `icon`
//       `[SerializeField] private AllianceRatingDisplay ratingDisplay;        // 0x28` ← `Alliance Rating Display`
//       `[SerializeField] private AllianceRatingDisplay highestRatingDisplay; // 0x30` ← `Alliance Rating Display (1)` / `MaxRating`
//       `public void Initialize(CardArmy faction, int currentScore, int highestScore)`
//   · 方法体 = **VA 反汇编** `FactionScore$$Initialize`（VA `0x1807417A0`）＋ ILSpy 同一方法：
//       `factionImage.sprite = ArmyUtilities.Instance.GetArmyIcon(faction);`
//       `ratingDisplay.Initialize(currentScore,      null, disableIfZero: false, useSealSystem: false, zeroBasedRank: true);`
//       `highestRatingDisplay.Initialize(highestScore, null, false, false, true);`
//     🔴 那三个布尔 + `overrideIcon = null` 是**从指令流读出来的**（ILSpy 全丢成 `default`）：
//        `xor r9d, r9d` ⇒ `disableIfZero = false` ·
//        `mov byte [rsp+0x20], dil(=0)` ⇒ `useSealSystem = false` ·
//        `mov byte [rsp+0x28], 1` ⇒ `zeroBasedRank = true` · `xor r8d, r8d` ⇒ `overrideIcon = null`。
//   · 节点名 = 现读 `bundle_menus_assets_all` 12 个实例的 PPtr 反查（`factionImage→icon` 等），见 §变体。
//
// ============================ §变体（两套 prefab，节点名与几何都不同）============================
//   两套挂的都是 `FactionScore` 这一个类（原版靠「实例化哪一份 prefab」区分）：
//
//   | 变体 | 实例数 | 第二颗评级节点名 | 第三行节点名 |
//   |---|---:|---|---|
//   | `FactionScoreSmall` | 6（`AllFactions` 列表行 ×4 + `Ranked Division Info/footer` ×2） | `Alliance Rating Display (1)` | —— |
//   | `FactionScoreBig`   | 6（`Player Profile Window/…/Top4` 的 `#1`–`#4`） | `Alliance Rating Display` | **`MaxRating`** |
//
//   🔴 **`Big` 的第二颗评级就叫 `MaxRating`** —— 照 `Small` 抄成同一个名字会「看起来对、名字错」，
//      而名字是宿主/断言唯一能核的东西（铁律 5·c）。
//   🔴 几何**逐实例不同**（同一份 `Small` 在三个宿主里三套宽度：standalone `icon 212.63` ·
//      `AllFactions` 行 `143.1` · `Ranked Division Info/footer` `180`）⇒ 本类 `Build()` 用的是
//      **各自 prefab 出厂那一份**（判据见 <see cref="SmallSize"/> / <see cref="BigSize"/> 注释），
//      宿主有覆盖时由宿主自己改矩形（三套覆盖值 → `资料/完全复刻/报告/B94_B17.md` §五）。
using UnityEngine;

namespace CardPresentation.ScoreBars
{
    /// <summary>原版 `FactionScore` 的**两套 prefab 的指代**。
    /// <para>🔶 自己设计（非原版）：原版**没有**这个枚举 —— 两套 prefab 挂同一个类，靠「实例化哪一份」区分；
    /// 我们建节点而不实例化 prefab ⇒ 需要一个显式档位形参。它只选**节点名与出厂矩形**，
    /// 不改变任何行为（两个变体的 `Initialize` 是同一段代码）。调度台 B94（B17）2026-10-29 定。</para></summary>
    public enum FactionScoreVariant
    {
        /// <summary>`FactionScoreSmall`：`icon` / `Alliance Rating Display` / `Alliance Rating Display (1)`。</summary>
        Small = 0,
        /// <summary>`FactionScoreBig`：`icon` / `Alliance Rating Display` / **`MaxRating`**。</summary>
        Big = 1,
    }

    /// <summary>原版 `FactionScore`：阵营徽记 + 当前分 + 最高分。</summary>
    public class FactionScore : MonoBehaviour
    {
        // 本件占渲染队列 **4200–4202**（4200–4216 现读无占用；下一个占用是 4217）。
        // ⚠️ 与 `AllianceEventScoreInfo`（3975–3981）/ `EnergySinglePlayerOnlyEventWindow`（3935–3945）不重叠。
        public const int QIcon = 4200;
        public const int QRating = 4201;

        // 🔴 字段名逐字照 `dump.cs`（断言按反射钉死）。
        [SerializeField] private ImageQuad factionImage;                     // 原版 `+0x20`（`Image`，节点 `icon`）
        [SerializeField] private AllianceRatingDisplay ratingDisplay;        // 原版 `+0x28`
        [SerializeField] private AllianceRatingDisplay highestRatingDisplay; // 原版 `+0x30`

        /// <summary>本实例按哪一套 prefab 建的（见 <see cref="FactionScoreVariant"/>）。</summary>
        public FactionScoreVariant Variant { get; private set; }

        // ============================================================ 出厂几何（判据：逐个实例现读）
        /// <summary>`FactionScoreSmall` **standalone prefab** 的根框（`438.74 × 117.71`）。
        /// 三行原版五元组（`m_AnchorMin` / `m_AnchorMax` / `m_Pivot` / `m_AnchoredPosition` / `m_SizeDelta`）：
        /// <code>
        /// icon                        A(0,1)重合  P(0.5,0.5)  pos( 106.32, -53.46)  sd(212.63, 128.50)
        /// Alliance Rating Display     A(1,0.5)重合 P(1.0,0.5) pos(   0.00,  18.70)  sd(448.89, 110.91)
        /// Alliance Rating Display (1) A(1,0.5)重合 P(1.0,0.5) pos(   0.00, -27.80)  sd(448.89,  72.34)
        /// </code>
        /// ⚠️ `icon` 比根高 10.79px（`128.5 &gt; 117.71`）—— 原版就溢出，⛔ 别「修」。</summary>
        public static readonly Vector2 SmallSize = new Vector2(438.74f, 117.71f);

        /// <summary>`FactionScoreBig` 的出厂那一份 —— ⚠️ **不是 standalone prefab**（那一份根是 `0 × 0`，全占位），
        /// 而是 `Player Profile Window/…/Top4` 里 `#1`–`#4` 的**真实格**（根 `190 × 230.45`）：
        /// <code>
        /// icon                    A(0,1)重合  P(0.5,0.5)  pos(95, -81.34)  sd(164, 145.80)
        /// Alliance Rating Display A(0,1)重合  P(0.5,0.5)  pos(95,-174.74)  sd(164,  41.00)
        /// MaxRating               A(0,1)重合  P(0.5,0.5)  pos(95,-208.62)  sd(164,  26.76)
        /// </code>
        /// 三行**首尾相接**：`8.44 → 154.24 → 195.24 → 222.00`，上下留白 8.44 / 8.45（逐值可验）。</summary>
        public static readonly Vector2 BigSize = new Vector2(190f, 230.45f);

        static readonly Vector2 A01 = new Vector2(0f, 1f);      // 左上
        static readonly Vector2 A10 = new Vector2(1f, 0f);      // 右中
        static readonly Vector2 P50 = new Vector2(0.5f, 0.5f);  // 居中
        static readonly Vector2 P10 = new Vector2(1f, 0.5f);    // 右中

        /// <summary>`icon` 节点（自检读口）。</summary>
        public Transform IconNode { get; private set; }
        public AllianceRatingDisplay RatingDisplay { get { return ratingDisplay; } }
        public AllianceRatingDisplay HighestRatingDisplay { get { return highestRatingDisplay; } }

        /// <summary>按变体建出这一件并挂上组件。<paramref name="at"/> = 根框（设计 px、左上原点 y 向下）。
        /// <para>用 <see cref="SmallSize"/> / <see cref="BigSize"/> 之一作 `at` 即可拿到出厂几何；
        /// 宿主有自己的框（例：`AllFactions` 行）时照自己的传。</para></summary>
        public static FactionScore Create(Transform parent, PxRect at, FactionScoreVariant variant,
                                          string name = null)
        {
            if (name == null) name = variant == FactionScoreVariant.Small ? "FactionScoreSmall" : "FactionScoreBig";
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.ApplyPxRect(go.transform, parent, at);

            var c = go.AddComponent<FactionScore>();
            c.Variant = variant;
            c.Build(at);
            return c;
        }

        void Build(PxRect at)
        {
            // 出厂那一下还不知道是哪个阵营（`Initialize(faction)` 才拿到）⇒ 先铺**全透明**：
            // 「节点在、画面无」（同 `RankedTab.FactionIconQuad` 的空态口径），⛔ 别留纯白块
            // （2026-10-24 E12 那四处白块就是「`art = null` 落到 `CardArt.Solid()` 纯白」踩出来的）。
            Color empty = new Color(1f, 1f, 1f, 0f);

            if (Variant == FactionScoreVariant.Small)
            {
                IconNode = IconNodeAt(UguiRect.Child(at, A01, A01, P50, new Vector2(106.32f, -53.46f),
                                                     new Vector2(212.63f, 128.50f)), empty);
                ratingDisplay = ArdAt("Alliance Rating Display",
                    UguiRect.Child(at, A10, A10, P10, new Vector2(0f, 18.70f), new Vector2(448.89f, 110.91f)));
                highestRatingDisplay = ArdAt("Alliance Rating Display (1)",
                    UguiRect.Child(at, A10, A10, P10, new Vector2(0f, -27.80f), new Vector2(448.89f, 72.34f)));
            }
            else
            {
                // 🔴 第三行是 `MaxRating`（⛔ 别抄成 `Alliance Rating Display (1)`）
                IconNode = IconNodeAt(UguiRect.Child(at, A01, A01, P50, new Vector2(95f, -81.34f),
                                                     new Vector2(164f, 145.80f)), empty);
                ratingDisplay = ArdAt("Alliance Rating Display",
                    UguiRect.Child(at, A01, A01, P50, new Vector2(95f, -174.74f), new Vector2(164f, 41f)));
                highestRatingDisplay = ArdAt("MaxRating",
                    UguiRect.Child(at, A01, A01, P50, new Vector2(95f, -208.62f), new Vector2(164f, 26.76f)));
            }
        }

        /// <summary>建 `icon` 节点：**`RectTransform` 容器** + 铺满它的 `Image` 图块（全透明纯色占位，
        /// <see cref="Initialize"/> 拿到阵营图后覆盖）。🔴 原版矩形写在**节点自己**身上 —— 判据 `报告/B94_B17.md` §5·2 五元组。</summary>
        Transform IconNodeAt(PxRect r, Color emptyTint)
        {
            // 🔴 2026-10-29（B95_A2B17）：原来直接 `MenuDraw.Rect(...)` ⇒ `ImageQuad.Create` 建的是**裸 `Transform`**
            //    （`Battle/ImageQuad.cs:124` `new GameObject(name)`）⇒ `icon` 节点没有 `RectTransform`、矩形表达不了
            //    ⇒ B17 自检 F2a/F4a 红（`GetChild(0) as RectTransform` = null）。同族先例 = `PurchasePremiumWindow` 的
            //    `Army Icon`（`:797` 容器）+ `:509` 图块 —— 本处照它的形状：容器承载矩形、图块铺满容器。
            var node = MenuDraw.Node(transform, "icon", r);      // 连纯色块都拿不到时**节点也要留**（宿主/断言按名字找它）
            var q = MenuDraw.Rect(node, CardArt.Solid(), r, "Image", QIcon, emptyTint);   // 同一份 `r` ⇒ 图块 localPosition = 0；`tex == null` 时 `Rect` 自己返 null
            if (q != null) factionImage = q;
            else Debug.LogWarning("[FactionScore] `icon` 的占位图建不出来（`CardArt.Solid()` 为空）⇒ 只留空节点");
            return node;
        }

        /// <summary>建一颗评级节点并挂 <see cref="AllianceRatingDisplay"/>。
        /// ⚠️ 它自己那三个子件（`Secondary Icon` / `Main Icon` / `Individual rating value`）**不由本类建** ——
        /// 它们的尺寸**逐实例不同**（standalone 那两份 `Main Icon` / `Individual rating value` 甚至是 `0 × 0` 占位，
        /// 真实宽度只出现在宿主实例里）⇒ 由宿主建好后 `AllianceRatingDisplay.Bind(...)`（见报告 §五那张表）。</summary>
        AllianceRatingDisplay ArdAt(string nodeName, PxRect r)
        {
            var t = MenuDraw.Node(transform, nodeName, r);
            return t.gameObject.AddComponent<AllianceRatingDisplay>();
        }

        /// <summary>原版 `public void Initialize(CardArmy faction, int currentScore, int highestScore)`。
        /// <para>三步（判据见文件头）：徽记 ← `ArmyUtilities.GetArmyIcon(faction)`；两颗评级 ← 当前分 / 最高分。</para></summary>
        public void Initialize(CardArmy faction, int currentScore, int highestScore)
        {
            if (factionImage != null)
            {
                var tex = FactionIconTexture(faction);
                if (tex != null)
                {
                    factionImage.SetTexture(tex);
                    factionImage.SetTint(Color.white);      // 出厂那张是透明占位 ⇒ 有图之后必须转回不透明
                }
                else
                {
                    // 取不到图 ⇒ 保持透明（⛔ 不拿纯色块顶替 —— 同 RankedTab 那条口径）
                    Debug.LogWarning("[FactionScore] 阵营徽记没画：`" + faction + "`（id " + (int)faction
                                   + "）没有对应图（`ArmyIcons.IconName` 给的图名取不到）");
                }
            }
            else Debug.LogWarning("[FactionScore] `factionImage` 没绑 ⇒ 阵营徽记不画（原版写 `Image.sprite`）");

            // 🔴 三个布尔是从指令流读出来的：`disableIfZero=false` / `useSealSystem=false` / `zeroBasedRank=true`
            if (ratingDisplay != null) ratingDisplay.Initialize(currentScore, null, false, false, true);
            else Debug.LogWarning("[FactionScore] `ratingDisplay` 没绑 ⇒ 当前分不写");
            if (highestRatingDisplay != null) highestRatingDisplay.Initialize(highestScore, null, false, false, true);
            else Debug.LogWarning("[FactionScore] `highestRatingDisplay` 没绑 ⇒ 最高分不写");
        }

        /// <summary>原版 `ArmyUtilities.Instance.GetArmyIcon(faction)` 的本地等价：
        /// `ArmyIcons.IconName(armyId)` 给图名 → `CardArt.MenuUi`（菜单那三批兜底）→ `CardArt.Ui`（UI 图集）。</summary>
        public static Texture2D FactionIconTexture(CardArmy faction)
        {
            string art = ArmyIcons.IconName((int)faction);
            if (string.IsNullOrEmpty(art)) return null;
            var tex = CardArt.MenuUi(art);
            return tex != null ? tex : CardArt.Ui(art);
        }

        // ============================================================ 🆕 2026-10-29（B97_W2）：宿主接线口
        // 🔴 纪律：追加在**类尾**（本文件被账按行号引用 `:103` / `:172` ⇒ 插中间会让下游行号整体漂移）。

        /// <summary>把宿主**已经建好**的三件绑进来（⛔ 不重建、不改几何）。
        /// <para>为什么需要它：`FactionScoreSmall` / `FactionScoreBig` 的三件几何**逐实例不同**（同一份 `Small`
        /// 在三个宿主里三套宽度、`Big` 的 `#1..#4` 各自被覆盖 —— 见 <see cref="SmallSize"/> 的注释与
        /// `资料/完全复刻/报告/B94_B17.md` §五那张表），而 <see cref="Create"/> 只能给「**出厂那一份**」；
        /// 宿主（`RankedTab.BuildRow`）按**自己那份**建好三件之后用本方法交进来 ⇒
        /// <see cref="Initialize"/> 才是原版那三句的唯一实现（原版这三件本来就是 prefab 的序列化字段）。</para>
        /// <para>⚠️ `null` 是允许的（那一半跳过并出声）—— ⛔ 别拿 `Create` 再建一份来「补」。</para></summary>
        public void Bind(ImageQuad icon, AllianceRatingDisplay rating, AllianceRatingDisplay highest)
        {
            factionImage = icon;
            ratingDisplay = rating;
            highestRatingDisplay = highest;
        }

        /// <summary>🔶 自己设计（非原版）：**没有分数**那一档的接入口。
        /// <para>为什么必须自己定：原版 `Initialize(int, int, int)` 只吃真分数 —— 分数由**服务器**下发
        /// （关服 ⇒ 判据在远端、本地恒 `null`），而「`AllFactions` 一行都不建」又是可观测的功能缺失
        /// ⇒ 用 `null` 表达「没有数据」，落到原版那颗 `AllianceRatingDisplay` 自己的空态串
        /// <paramref name="emptyText"/>（= `RankedTab.ScoreEmpty` 的 `------`）。
        /// 这是铁律 18·0 的「**与服务器有关**」例外；调度台 2026-10-29 裁定（`B97_W2`）。</para>
        /// <para>**有值没值都先走 <see cref="Initialize"/>**（那才是原版三句的唯一实现），没值的那颗再把文本改回空态串
        /// —— 原版「没有分数时会印什么」本地**永远不可观测**（那几行只由服务器数据驱动），⛔ 别把它当成原版判据。</para></summary>
        public void InitializeOrEmpty(CardArmy faction, int? currentScore, int? highestScore, string emptyText)
        {
            Initialize(faction, currentScore ?? 0, highestScore ?? 0);
            if (!currentScore.HasValue) ResetToEmpty(ratingDisplay, emptyText);
            if (!highestScore.HasValue) ResetToEmpty(highestRatingDisplay, emptyText);
        }

        /// <summary><see cref="InitializeOrEmpty"/> 的「改回空态串」那半步
        /// （`AllianceRatingDisplay.Initialize` 只会写 `value.ToString()`，⛔ 别去改它的签名 —— 那是照 `dump.cs` 的）。
        /// ⚠️ 只写宿主建的那颗 `Label`（<see cref="AllianceRatingDisplay.RatingText"/>），⛔ 不碰结构。</summary>
        static void ResetToEmpty(AllianceRatingDisplay d, string emptyText)
        {
            if (d == null) return;
            var t = d.RatingText;
            if (t != null) t.SetText(emptyText ?? "");
        }
    }
}
