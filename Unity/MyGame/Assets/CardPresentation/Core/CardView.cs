// CardView.cs — 一张卡的视觉载体
//
// **分层**，和原版一样：
//
//     ┌─ info  ── 数值 / 名字 / 关键词（我们画的，透明底）        z 最小 = 最前
//     ├─ frame ── 卡框（原版 PNG，`Resources/Art/cards/frame_<阵营>.png`）
//     └─ art   ── 美术窗口里的画（现在是占位图，换成你自己的立绘）
//
// 没有卡框 PNG 时**退回单层程序生成的黑卡**（`CardArt.Available == false`）——
// 所以删掉 `Resources/Art/` 整个目录，游戏照样跑，只是回到占位美术。
//
// 数值位置是**照原版卡框量的**（见下面 `xxxAt` 常量）：近战/远程在左下、生命在右下、
// 费用在右上。别挪 —— 手牌是「左卡压右卡」，露出来的正好是每张牌的右半边。
//
// 文字分两条路（2026-09-12 起）：
//   · **卡名 / 关键词 → TextMeshPro**（世界空间那版），中文才画得出来。和原版同构 ——
//     原版卡预制体上挂的就是 `NameTextUnit` / `DescTextUnit` 这些 TMP 组件，不是烘进贴图的。
//   · **数值 / 费用 → 仍是自写 5×7 点阵**（`TextCanvas`）。就几个数字，点阵够用，
//     而且不依赖字体资产。**拿不到 TMP 字体资产时卡名也退回点阵**（只画得出 ASCII 英文）。
// 见 `Core/TmpFont.cs`（字体从哪来）和 `Core/CardText.cs`（中文文案表）。
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一张卡要显示的东西。由 `BattleDriver` 从规则引擎的卡定义转过来。</summary>
    public struct CardData
    {
        public string id;
        public string title;
        public int cost;
        public int melee;        // 近战攻击力
        public int ranged;       // 远程攻击力（0 = 没有）
        public int health;
        public int armor;        // 护甲（原版卡面右侧那面盾。0 = 没有）
        public string keywords;  // 已格式化的关键词串，直接画在卡面上
        public bool isUnit;      // 单位还是战术（战术不上战场）
        public Color frame;      // 阵营色 —— 占位卡面的边框、费用框用它
        public string faction;   // 阵营 key，用来找 `Art/cards/frame_<faction>[_tierN].png`
        /// <summary>稀有度（`common` / `rare` / `epic` / `legendary` / `special`）——
        /// **卡框按稀有度分四档**（原版就是），空的按 common 处理。见 `CardArt.Frame`。</summary>
        public string rarity;
        /// <summary>兵种（`Infantry` / `Vehicle` / `Drone`…）—— 卡面下方那一行（原版 `RaceText`）。
        /// ⚠️ **不是所有卡都印这一行**，判据见 `SubtypeLine`。
        /// 🔴 2026-10-17（D5）**就地更正**：这里原来写「**我们还没有中文对照表**，照原样显示」—— 表**已建**
        /// （键 = `Card_Race/&lt;本串&gt;`，见 <see cref="RaceTermPrefix"/> / `Core/Loc.cs`）。
        /// **本字段仍然是英文原值**，中文只发生在**显示那一步**（`RaceTerm`）
        /// ⇒ ⛔ 别往这个字段里写中文（`TacticSubtypeShown` / 引擎 / 卡表都按英文原值判）。</summary>
        public string subtype;
        /// <summary>卡类（`unit` / `tactic` / `hero` / `defence`）—— **只有卡面的兵种行用它**
        /// （见 `SubtypeLine` 的判据）。⚠️ 没填的构造点走老路径（按 `isUnit` 判），不会突然少一行。</summary>
        public string type;

        /// <summary>🆕 立绘 / 抠图清单的**文件键**（= 引擎卡 id，例 `UM82`）。
        /// 🔴 2026-09-15 起立绘**按 id 命名**（原来是卡名）—— 同名跨阵营的卡（`Terminator` /
        /// `Aggressor` / `Maulerfiend` / `Bladeguard Veteran` / `Terminator Champion`）原来会
        /// **互相覆盖**，实测 `art_aggressor.png` 是太空野狼那张、暗黑天使那张挂着别人的画。
        /// 见 `资料/PnP卡图_逐张对账_0915.md` §五。⚠️ 我们自己设计的那 26 张没有引擎 id，仍用**卡名**。
        /// ⚠️ **2026-10-17 订正**：上面那串名单是 **2026-09-15 当天**的口径，现在只剩 3 组
        /// （`Terminator` / `Terminator Champion` / `Maulerfiend`）—— `Aggressor` 那组是 `DA12` 的
        /// 重复行（09-16 已从数据删掉），`Bladeguard Veteran` 那组的「同名」是 09-13 一次错改名的
        /// 产物（`UM34` 被照 PnP 卡图文件名改名）、10-17 已撤回成 `Bladeguard Lieutenant`
        /// （判据 `资料/普查产出_1017/W_B16_教程数据缺口.md` §①）。**按 id 取名这条规矩不受影响**。</summary>
        public string artId;

        /// <summary>🆕 **立绘覆盖**（2026-09-24 加，给卡组线 Styles 页的**异画**用）。
        /// 非空时卡面用它、**不再按 `artId` 取**（`ArtTexture`/抠图判据两处都看它）。
        /// ⚠️ 判「这张覆盖图有没有抠图 alpha」用的是**另一份清单**
        /// （`CardArt.AltArtHasCutout` ← `Art/altarts/alt_cutouts.json`），**不是** `card_cutouts.json`
        /// —— 后者只记 `art_&lt;id>` 那批，拿它去问 `alt_*` 会恒假 ⇒ 异画会**少画一层**（角色不越出卡框）。</summary>
        public Texture2D artOverride;

        /// <summary>🆕 棋盘单位卡身上的 buff/debuff 徽标（原版 `BattleCardUI.boardTraitIcons`，最多 7 个）。
        /// **只有场上的单位画它** —— 手牌不画（理由与出处见 `Badges.cs` 文件头）。
        /// `null` 或空 = 一个不画，和原版「先把 7 个位全部 Toggle(false)」一致。</summary>
        public System.Collections.Generic.List<Badge> badges;

        public static CardData Simple(string title, int cost, int melee, int health)
        {
            return new CardData
            {
                id = title, title = title, cost = cost,
                melee = melee, ranged = 0, health = health, armor = 0,
                keywords = "", isUnit = true, frame = new Color(0.55f, 0.55f, 0.62f),
                faction = null,
            };
        }

        /// <summary>布局自检（`CardBaseDemo`）用的假卡 —— 跟真数据无关，只填满版面。
        /// `id` 借几张**有原版立绘**的卡名，这样演示截图里能看到真立绘。</summary>
        public static CardData Placeholder(int i)
        {
            var colors = new[]
            {
                new Color(0.78f, 0.24f, 0.22f),   // 红
                new Color(0.22f, 0.45f, 0.78f),   // 蓝
                new Color(0.28f, 0.60f, 0.32f),   // 绿
                new Color(0.72f, 0.62f, 0.20f),   // 金
                new Color(0.52f, 0.30f, 0.66f),   // 紫
            };
            string[] ids = { "Scavenger", "Wave Rider", "Ironclad", "Siren", "War Drake", "Leviathan" };
            return new CardData
            {
                id = ids[i % ids.Length],
                title = "CARD " + (i + 1),
                cost = (i % 7) + 1,
                melee = (i * 3) % 9 + 1,
                ranged = (i % 4 == 0) ? 2 : 0,
                health = (i * 5) % 9 + 1,
                keywords = (i % 3 == 0) ? "VANGUARD" : "",
                isUnit = true,
                frame = colors[i % colors.Length],
                faction = (i % 2 == 0) ? "Ember" : "Tide",
            };
        }
    }

    /// <summary>
    /// 卡面左下那两颗攻击数值格 —— 「这个单位能被选中」的底光点亮哪一颗。
    /// （原版 `Base Attack Counters` 下就两个 `Highlight`：近战一个、远程一个，**没有技能那一档**。）
    /// </summary>
    public enum TargetGem { None, Melee, Ranged }

    /// <summary>
    /// **这张卡按哪个展示场景组装** —— 原版是**三条彼此独立的判据**，不是一个统一的「显示模式」枚举
    /// （2026-09-19 全量反编译复核，出处 `资料/战斗UI_原版对账表.md` 与本节注释）：
    ///
    ///   · **(a) 场上**：`BattleCardUI.SetObjectVisibility` 在 inPlay 类状态调
    ///     `Card2DController.Toggle(false)` —— **整张 `2DCard` 关掉**（`Card2DController__Toggle.c:5-8`），
    ///     连**插图 / 卡框 / 费用 / 稀有度宝石 / 卡名 / 阵营行 / 兵种行 / 效果底板 / 效果文字**
    ///     一起没有，只剩 `Board Elements` 那棵子树：**攻 / 血 / 护甲 + 7 槽关键词徽标**。
    ///   · **(b) 手牌 / 放大窗**：`2DCard` 开着 —— 上面那些层都在。
    ///     ⚠️ **阵营行是例外**：手牌那一档不印它（**用户 2026-09-22 裁定**，见下面第三档那条）。
    ///   · **(c) 立绘溢出卡框 ×1.27**：**逐卡字段** `RawCardScript.useOverDraw`（`CardDisplayWindow__ChangeCardPosition.c:75-79`），
    ///     只有**放大窗的前台那张**用；手牌和场上都不溢出。
    ///
    /// ⚠️ **我们把上面那几条收成一个档位枚举**（2026-10-17 起是**三档**，见下）：原版场上是
    /// **一个 3D 模型**（`3DBody`），我们拿不到那套模型 ⇒ 场上用**平面立绘**顶替 3D 体
    /// （`_art` 照画），其余层按上表**全部不画**。这是一处**明写的偏离**（体量差在模型，不在信息量）。
    ///
    /// 🆕 **2026-10-17：加了第三档 `Hand`（手牌）** —— 拆它的原因**只有一条**：
    /// **阵营行按展示场景分档**。判据 = **用户 2026-09-22 的裁定**（2026-10-17 由用户指认实拍复核过）：
    ///
    ///   > **「手牌 + 场上不印，其余都印」**
    ///   > · **手牌** = 卡框 + 名字 + 费用 + 数值 + 效果文字（**无阵营行**）
    ///   > · **场上** = 更简（**无卡框、无效果文本**）
    ///   > · **其余全印**：放大窗 · 卡池 / 收藏 / 卡组编辑 · 选牌面板
    ///
    /// 🔴 **实拍复核（用户 2026-10-17 指认，我逐像素看过）**：`点击卡片查看详情的参考.png` 里
    ///    **卡名（白）下面那行【橙色】的 `萨姆-罕` 就是阵营行、印着**；`战斗截图参考.png` 里
    ///    **手牌与场上都没有那一行**。⇒ 裁定成立。
    ///
    /// ⇒ `Hand` 与 `Full` **只差阵营行这一层**，别的层一模一样；判据函数只有
    /// <see cref="CardView.FaceShowsArmy"/> 一处。`Full` 仍是**默认档**（非手牌那些调用点一个都没动）。
    ///
    /// 📌 **本条来回过一次、别再来回**：2026-10-17 我一度把它改成「哪都不印」（误读了用户对「卡名颜色」
    ///    的一句回答，且拿**本地 PC 版 bundle** 的读数去反推原版 —— 而那台客户端与本地不是同一版），
    ///    **用户当天指认实拍后改回**。再要动它，**先去看那两张实拍**（铁律 3：实拍在这一层是有效证据）。
    /// </summary>
    public enum CardFace
    {
        /// <summary>放大窗 / 卡池 / 收藏 / 卡组编辑 / 选牌面板……**除手牌外的一切**：
        /// 整张卡都画（原版 `2DCard` 开着的那一套，**含阵营行**）。也是默认档。</summary>
        Full = 0,
        /// <summary>场上：**只有立绘 + 攻/血/甲 + 徽标**（原版把整张 `2DCard` 关掉）。</summary>
        Board = 1,
        /// <summary>🆕 **手牌**：和 `Full` 同一套层，**只少阵营行**（用户 2026-09-22 裁定）。
        /// ⚠️ 值显式写 **2**：枚举值一律显式赋值、不插队（插队会悄悄改掉既有值的含义）。</summary>
        Hand = 2,
    }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class CardView : MonoBehaviour
    {
        // 设计尺寸（世界单位）—— 卡牌是 1 : 1.4 的竖卡
        // ⚠️ **卡本体**尺寸，出处：`资料/战斗规格/战斗重建_0827/子代理读报_2dcard_0827.md` 的
        //    `2DCard (GO 1075, RT 2876, size 2.0927×3.3313)` —— **卡单位**（不是 px）。
        //    为什么用它而不是 `CardFrame` 的 2.2452×3.2572：后者的 rect 虽然更宽，但**它左右各约 18%
        //    是透明留白**（框的金属实宽只有 ~1.33–1.42 卡单位），而且**铺到卡片左右边缘的是立绘**。
        //    「卡本体」这个值有两个独立来源交叉验证 ——
        //      · `2.0927 × 0.36 × 182.14 = 137.2 px`、`3.3313 × 0.36 × 182.14 = 218.4 px`
        //        （k=182.14 px/单位，见 `审查更正清单_0827.md:136`）—— 正是战场上量到的卡尺寸；
        //      · 同上 ×0.73 → 手牌 165×263 px。
        //    （2026-09-12 改：原来是我们自己挑的 1.45×2.03，比例 0.714，比原版的 0.628 宽 13.7%，
        //      表现为「卡又矮又胖」，连带着所有卡面元素的位置全偏。）
        public const float Width = 2.0927f;
        public const float Height = 3.3313f;

        // 贴图尺寸（像素）。比例 256:371 = 0.690，**跟着原版卡框外框走**（实测 647:936 = 0.691），
        // 这样卡框铺满整块 quad 时不会被拉变形
        // 贴图缓冲的宽高比要**跟着卡本体走**（256 / 0.6282 = 407）——
        // 原来是 256×371（0.690，按卡框比例配的），画到卡本体上会被横向压扁。
        const int FaceW = 256, FaceH = 407;

        // ---- 原版卡框上各元素的位置（在「卡外框」归一化坐标里量的，见资料/原版复刻_场景与美术.md）----
        // x 从左边 0 到右边 1，y 从**顶部** 0 到底部 1
        // ⚠️ **下面这 7 个位置全部来自原版 JSON**（`子代理读报_2dcard_0827.md` A2 表），不是量的截图。
        //    换算：卡本体 W=2.0927、H=3.3313，中心为原点，
        //          x01 = 0.5 + px/W，y01 = 0.5 − py/H（y01 从**顶部**数）。
        //    原版那 7 个 container 的 (x,y)（卡单位）：
        //      Cost (0.788,+0.666)  Armour (0.899,−0.991)  Melee (−0.836,−1.19)
        //      Range (−0.595,−1.40) Health (0.74,−1.36)    Rarity (0,−1.458)
        static readonly Vector2 CostAt   = new Vector2(0.8766f, 0.3001f);   // 右上的费用宝石（原版在卡上部 30%）
        static readonly Vector2 MeleeAt  = new Vector2(0.1005f, 0.8572f);   // 左下红圆
        static readonly Vector2 RangedAt = new Vector2(0.2157f, 0.9203f);   // 左下偏右的紫圆
        static readonly Vector2 HealthAt = new Vector2(0.8536f, 0.9083f);   // 右下绿
        static readonly Vector2 ArmourAt = new Vector2(0.9296f, 0.7975f);   // 右侧盾牌（原来**根本没画**）

        // 🔴 **场上（`CardFace.Board`）用的不是上面这一套**（2026-09-20 补）。
        //    场上是**原版 3D 卡体**，数值由原版挂在 `3DBody` 下的**四个独立世界空间 TMP 节点**画：
        //    `Melee AttackText` / `Range Attack Text` / `HealthText` / `Armour Text`。
        //    它们的位置和 2D 卡面那一套**不一样**（2D 那套更散、更靠下 ⇒ 3D 体一上来就露馅，
        //    自检截图里数值整组浮在卡体下方）。
        //    原版真值（**3DBody 空间，y=0 = 卡底**；出处 = 各节点 `m_AnchoredPosition`，
        //    字段 `body3D{Melee,Range,Health}Anchor` = `dump.cs:26179/26178/26180`；
        //    合成链与实测过程见 `资料/3DBody_原版场上卡体规格.md` §四）：
        //        Melee (−0.680, 0.740)   Range (−0.434, 0.406)
        //        Health( 0.566, 0.357)   Armour( 0.678, 0.772)   （z 都在 −0.06…−0.08）
        //    ⇒ 换到我们的卡单位（**减半卡高**，因为我们的原点是卡中心）再换成 0..1（y 从上）。
        //    ⚠️ **不要乘 0.88586** —— 那是 `Card 3D`（网格自己）的缩放，这些文本节点与它平级。
        static readonly Vector2 BoardMeleeAt  = new Vector2(0.1751f, 0.7779f);
        static readonly Vector2 BoardRangedAt = new Vector2(0.2926f, 0.8781f);
        static readonly Vector2 BoardHealthAt = new Vector2(0.7705f, 0.8928f);
        static readonly Vector2 BoardArmourAt = new Vector2(0.8240f, 0.7683f);

        // ---- 卡名 / 效果文字的位置：**单位卡和战术卡不是同一套**（A3 表）----
        // 原版的文字都挂在 `Name and description` 这个块（1.3×0.68 @(0,−0.7745) 卡单位）底下，
        // 子节点自己的 pos 是相对**那个块**的、且都带 scale 0.01。把两者相加再用
        // x01 = 0.5 + cx/W、y01 = 0.5 − cy/H 换算：
        //   单位卡 `NameTextUnit`  (0,+0.501)   → 卡名 (0,      −0.2735) → y01 **0.5821**
        //   单位卡 `DescTextUnit`  (0.0164,+.0008) → 效果 (0.0164, −0.7737) → y01 **0.7323**
        //   战术卡 `NameTextTactic`(0,+0.3135)  → 卡名 (0,      −0.4610) → y01 **0.6384**
        //   战术卡 `DescTextTactic`(0,−0.1836)  → 效果 (0,      −0.9581) → y01 **0.7876**
        // ⚠️ **2026-09-12 改**：原来这两个位置写的是「取文字块的上下缘」——
        //    那是**我们挑的**（旧注释自己写着），比原版的卡名低了 0.21 卡单位（≈14 px @手牌尺寸），
        //    效果文字也偏。现在按上面四条原版值来，单位/战术分开。
        static readonly Vector2 UnitNameAt   = new Vector2(0.5000f, 0.5821f);
        static readonly Vector2 UnitDescAt   = new Vector2(0.5078f, 0.7323f);
        static readonly Vector2 TacticNameAt = new Vector2(0.5000f, 0.6384f);
        static readonly Vector2 TacticDescAt = new Vector2(0.5000f, 0.7876f);

        // ---- 阵营行 / 兵种行（对照 `D:/2/Warpforge部队卡片/<阵营>/…png` 的原版成品卡图补的）----
        // 原版卡面从上到下是：**卡名 → 阵营（橙）→ 效果文字 → 兵种（橙）**，我们原来只有卡名和效果。
        // 位置出处 A3 表（文字块 −0.7745 加子节点自己的偏移）：
        //   单位卡 `ArmyTextUnit` (0,+0.31) → y01 **0.6394**；`RaceText` (0,−0.452) → y01 **0.8682**
        //   战术卡 `ArmyTextTactc`(0,+0.13) → y01 **0.6935**（战术卡**没有**兵种行 —— 成品卡图上就没有）
        // ⚠️ 颜色：A3 表把阵营行记成**白**、兵种行记成**橙**；但两张成品卡（`Warpforge_02_Heavy-Intercessor-2.png`、
        //    `pariah.png`）上**两行都是橙**（(0.9922,0.6039,0.3451) 同一支）。这里按**成品卡**走，
        //    要是以后拿到数字版实况发现阵营行是白的，改这一处即可。
        static readonly Vector2 UnitArmyAt   = new Vector2(0.5000f, 0.6394f);
        static readonly Vector2 UnitRaceAt   = new Vector2(0.5000f, 0.8682f);
        static readonly Vector2 TacticArmyAt = new Vector2(0.5000f, 0.6935f);
        /// <summary>效果文字那块**版面框的顶边**（y01）—— 中心减半高：
        /// 单位 0.7323 − 0.7616/2/3.3313 = 0.6180；战术 0.7876 − 0.7898/2/3.3313 = 0.6691</summary>
        const float UnitDescTop01 = 0.6180f;
        const float TacticDescTop01 = 0.6691f;
        /// <summary>版面框的**底边**（中心 + 半高）：单位 0.7323+0.1143、战术 0.7876+0.1185。
        /// 效果文字**上沿**可能被阵营行往下推（见 `BuildTextLayers`），那时可用的高度只剩
        /// 「底边 − 上沿」—— 不这么算的话战术卡三行描述会直接压到最底下那颗稀有度宝石上。</summary>
        const float UnitDescBottom01 = 0.8466f;
        const float TacticDescBottom01 = 0.9061f;

        /// <summary>**卡面上要印兵种行/类型行**时，效果文字那块的**下沿**（y01，从卡顶算）。
        /// 比 `UnitDescBottom01` 再往上让一点 —— 让出来那点是**量出来的，不是抄来的**：
        /// 🔴 2026-09-15 逐条比对 `_tmp_view/cardface/Lychguard.png` 与 PnP 成品卡：
        ///   原值 0.8466 时，**效果文字的可见下沿在 0.808、类型行的可见上沿在 0.803 ⇒ 叠字**；
        ///   而 PnP 那边同样是「效果文字 + 类型行」，两个值分别是 0.799 / 0.818，**不叠**。
        ///   让到 0.834 后我们的可见下沿约 0.795，留出 ~0.008 的间隙。
        /// ⚠️ **别只按公式推**：`PlaceBottomAt` 对齐的是 `textBounds`（含基线以下那一段），
        ///   和肉眼看到的字形下沿差约 **0.039 卡高**；纯按公式算会得出「本来就不该叠」的错误结论。
        /// ⚠️ 这条一旦再改，`UnitDescBottom01` / `TacticDescBottom01` 也跟着失去意义 —— 三个是一组。</summary>
        const float RaceDescBottom01 = 0.834f;

        /// <summary>两行的字号：A3 表 `ArmyTextUnit` fs15×0.01、`RaceText` fs18×0.01（卡单位）</summary>
        const float ArmyEmOfCard = 0.1500f / 3.3313f;   // 4.50% 卡高
        const float RaceEmOfCard = 0.1800f / 3.3313f;   // 5.40% 卡高

        // ---- 文字版面框（原版 A3 表，px × 0.01 = 卡单位）----
        // `NameTextUnit`  160 × 29.73 px → 1.6    × 0.2973；`DescTextUnit`  152.74 × 76.16 → 1.5274 × 0.7616
        // `NameTextTactic`157.79 × 26.35 → 1.5779 × 0.2635；`DescTextTactic`160 × 78.98 → 1.60   × 0.7898
        // ⚠️ 2026-09-12 改：原来是 0.86×卡宽 / 0.80×卡宽（**没写出来处的我们自己挑的**），
        //    比原版宽 12% / 10%，字因此铺得比原版满。
        const float NameBoxW = 1.6000f, NameBoxH = 0.2973f;
        const float DescBoxW = 1.5274f, DescBoxH = 0.7616f;
        const float TacticNameBoxW = 1.5779f, TacticNameBoxH = 0.2635f;
        const float TacticDescBoxW = 1.6000f, TacticDescBoxH = 0.7898f;
        /// <summary>效果文字的**行距 / 段距**（原版 `DescTextUnit` / `DescTextTactic` 的
        /// `m_lineSpacing = 5.0`、`m_paragraphSpacing` 单位卡 0 / 战术卡 8.0；文字节点 `scale = 0.01`
        /// → ×0.01 = 卡单位）。卡名的 `m_lineSpacing = 0`（原版本来就不加行距）。
        /// 出处：`MB_3927:118,120`（单位）/ `MB_3730`（战术），见 `资料/规则引擎_进度与交接.md` 第二十三轮。</summary>
        const float DescLineSpacing = 0.05f, DescParaSpacing = 0.08f;


        // ---- 卡上各层在原版里的**真实尺寸**（单位是「卡单位」，卡本体 = 2.0927 × 3.3313）----
        // 出处：`资料/战斗规格/战斗重建_0827/子代理读报_2dcard_0827.md` A2 全树规格表。
        // ⚠️ **它们不是同一块矩形**：卡框比卡本体**宽**（2.2452 vs 2.0927）、比卡本体**矮**
        //    （3.2572 vs 3.3313），而且还偏下 0.03；Front 则比卡本体高。
        //    所以每一层都要按**自己的**矩形画 —— 把各自的 bbox 拉满整张卡是错的
        //    （我们原来就这么干：卡框被横向压扁 7.3%，框上的宝石跟着内移，数值就落到宝石外面了）。
        // ⚠️ **2026-09-12 更正**：原来这里写「框比卡宽所以两侧塔楼探出去」——**不成立**。
        //    卡框 PNG 的**不透明实宽只有 ~608–647 / 1024 px（≈1.33–1.42 卡单位）**，比卡本体还窄；
        //    rect 左右那 ~18% 是**透明留白**。**真正铺到卡片左右边缘的是立绘**，不是卡框。
        public const float CardUnitW = 2.0927f, CardUnitH = 3.3313f;
        const float FrameUnitW = 2.2452f, FrameUnitH = 3.2572f, FrameUnitY = -0.03f;

        // 占位卡面（没有原版卡框时）的**插图位**——和 `FaceTexture` 里那个 ③ 号框是**同一处**，
        // 归一化是 x[0.06,0.94] y[0.20,0.58]（y 从顶算）。⚠️ 改一处要两处一起改。
        // 换算到卡单位（卡本体 2.0927×3.3313 居中）：宽 0.88×卡宽、高 0.38×卡高、中心 y=+0.11×卡高。
        const float WellW = 0.88f * CardUnitW;
        const float WellH = 0.38f * CardUnitH;
        const float WellY = 0.11f * CardUnitH;

        // 卡面**底部中央那颗稀有度宝石**。
        // ⚠️ **2026-09-12 改**：原来按 `Rarity` **容器**的 0.4 × 0.4 画（还画成了正方形）。
        //    容器不等于图 —— 里面那张 `Card Rarity Sprite` 自己的 rect 是
        //    **0.258 × 0.2335 @(0,−0.0055)**（相对容器），sprite 是 84×76 px（宽高比 1.105）。
        //    出处：`子代理读报_2dcard_0827.md` A2 表 `Rarity / Card Rarity Sprite`。
        //    按容器画 = 宽 1.55×、高 1.71×，还把 1.105 的比例压成了 1.0（对照图 `gems.png` 能看出来）。
        const float RarityGemW = 0.258f, RarityGemH = 0.2335f;
        /// <summary>宝石中心 = 容器 y −1.458 + 图自己的 −0.0055</summary>
        const float RarityGemY = -1.4635f;

        // ---- 原版立绘区（`CardImage`，A1 表）----
        // 2.7484 × 2.7484 @ (0, −0.044) 卡单位 —— **正方形、比卡本体大**，故意外溢，
        // 由卡框的 alpha 遮罩切掉（`子代理读报_2dcard_0827.md` D2）。
        // ⚠️ prefab 里那张 Image 是 `m_PreserveAspect = 0`（照字面 = 把立绘横向拉 1.53×成方形）。
        //    立绘 sprite 的 textureRect 宽高比是 0.6548，拉成 1.0 会明显变形；
        //    而**按比例 fit 进这个方形**得到 1.80 × 2.7484，正好盖住卡框拱窗（实测 1.69 × 2.76）——
        //    所以这里按 **preserveAspect 装进方形** 来做（见 `ArtMesh`）。
        //    ⚠️ 这一条**与原版字段不一致**，跑得了实况时要复核（原版关服，战斗界面进不去）。
        const float ArtUnitSquare = 2.7484f, ArtUnitY = -0.044f;

        /// <summary>立绘板在「装进方形」的基础上再放大一点（4%）。
        /// 为什么要有余量：原版的拱窗（实测 1.69 × 2.76 卡单位）和 fit 出来的立绘
        /// （0.6548 的画 → 1.80 × 2.7484）**纵向只差 0.012**，留一点余量免得拱顶露出背景。
        /// ⚠️ 不能靠「把板拉到卡框那么大」来解决 —— 那会让 UV 出界、贴图 Clamp 把边上的像素
        ///    拉成一圈白边（踩过，见 `ArtMeshInFrame`）。</summary>
        const float ArtCoverMargin = 1.04f;

        /// <summary>整张图（立绘、占位卡面这类「UV 就是全图」的层用）</summary>
        static readonly Vector2[] FULL_UV =
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
        };

        // ---- 费用底板 / 护甲盾牌：原版是**两张真图**，不是画出来的形状（A2 表）----
        // `Cost Container/Cost Background`  0.4891 × 0.4809，sprite `Card Frame Cost Icon`（248×244）
        // `Armour Container/Image`          0.3067 × 0.3784 @(−0.005,−0.032)，sprite `pedestal_icon_armor`（248×306）
        // ⚠️ 2026-09-12 改：费用原来画的是**我们自己拼的蓝色六边形**（比原版小 18%、没花纹），
        //    护甲干脆只有数字、没有那面盾。两张图当时就躺在 `Resources/Art/ui_deck/` 里。
        const float CostBgW = 0.4891f, CostBgH = 0.4809f;
        const float ArmourIconW = 0.3067f, ArmourIconH = 0.3784f;

        // ==================================================================
        //  🆕 卡面**文字底板**（原版 `Front/Textbackgrounds/TextBackground Big|Small UI`）
        //
        //  🔴 2026-09-15 加。**出处**（主对话自己逐字段读的，铁律 3；不是转述）：
        //     `d:/2/解包整理/07_场景/battlearena1/`（GameObject / RectTransform / MonoBehaviour，
        //     按 GO 名 → `m_Component[].m_PathID` → RT/MB 查；⚠️ RT 的 PathID 在**文件名**里）
        //       · RT `TextBackground Big UI`  sizeDelta **1.7766 × 1.55**、anchoredPos **(−0.001, −0.65)**、
        //         anchor/pivot 居中、scale 1；父 `Front` anchoredPos y = **+0.08**
        //         ⇒ 底板中心相对卡中心 y = 0.08 − 0.65 = **−0.57**
        //       · RT `TextBackground Small UI` sizeDelta **1.8635 × 0.9621**、anchoredPos (0, **−1.02**)
        //       · Image（两块同款）：sprite **`Card Text smooth background`**（32×32、border 7/0/7/9）、
        //         `m_Color` **=(0,0,0,0.647)**、`m_Type=1`(Sliced)、`m_PixelsPerUnitMultiplier=14.7`
        //
        //  **渲染序**（原版 `Front` 的子序，底→顶）：
        //     Card Highlight And Shadow → CardImage → **Textbackgrounds** → CardFrame → Card Info
        //     ⇒ 我们的 z 取 **+0.015**：比 `art`(+0.03) 近、比 `frame`(0) 远。
        //
        //  ⚠️ **别被模板态骗了**：`Textbackgrounds` 那个 GO 在场景里 `m_IsActive = false`，
        //     但 `CardTextsController` 的 `objectsToActivate`（state 1..8）**每一条都含它**
        //     ⇒ 运行时只要卡面显示文字，这块黑底就会被打开。
        //     旧文档写的「原版无黑幕、sd_card 已按无黑幕处理 ✅」是**只看静态标志**得出的错结论，已更正。
        // ==================================================================
        static readonly Vector2 TextBgAt      = new Vector2(0.49952f, 0.67111f);  // 0.5+(−0.001)/2.0927 · 0.5+0.57/3.3313
        static readonly Vector2 TextBgSmallAt = new Vector2(0.50000f, 0.80621f);  // 0.5+0/2.0927       · 0.5+1.02/3.3313
        const float TextBgW = 1.7766f, TextBgH = 1.55f;
        const float TextBgSmallW = 1.8635f, TextBgSmallH = 0.9621f;
        const float TextBgAlpha = 0.647f;          // Image `m_Color.a`，原值 0.64705884
        /// <summary>底板的 z。
        /// ⚠️ **不能照原版的「卡框之下」摆**：原版 `Front` 的子序里卡框在底板之上，
        /// 但**原版没有「立绘抠图层」**（我们是自己加了一层 `_artFront`，z = −0.008，
        /// 角色抠图盖在卡框上做破框）。底板要是摆在卡框之下(+0.015)，
        /// 就被那层抠图整个盖住了 —— 实测渲出来**完全看不见**。
        /// ⇒ 取 **−0.012**：在 `_artFront`(−0.008) **之前**、在数值层 `_info`(−0.02) **之后**。
        ///   视觉效果与原版一致（底板压住立绘、文字再压在底板上）。</summary>
        const float TextBgZ = -0.012f;
        /// <summary>护甲盾的中心：容器 (0.899,−0.991) + 图自己的 (−0.005,−0.032)，换算成 x01/y01</summary>
        static readonly Vector2 ArmourIconAt = new Vector2(0.92721f, 0.80710f);

        // ---- 🆕 「临时卡（Ephemeral）」角标 —— 2026-09-13 第三十二轮 ----
        //
        // **为什么是图标而不是原版那套**：原版的卡面标记是
        // `BattleCardUI.ShowEphemeral()`（`BattleCardUI__ShowEphemeral.c`，77 行 —— 在卡的位置
        // 实例化一个状态图标 prefab）+ `Card2DController.ToggleGlitch()`（把卡面材质换成
        // `glitchMaterial`）。⚠️ **glitch 素材本地没有**（`d:/2` 全盘 `*glitch*` 零命中，
        // 我们的特效导出报告里也零命中）⇒ 按工程既有做法（77 个原版 shader 逐个自建替代）
        // 本该自建一个 glitch shader；本轮**先用原版真有的那张关键词图标顶上**
        // （`Atlas_trait_icon_ephemeral.png`，80×80，就在本地图集里）。
        //
        // ⚠️ **位置和大小是「我们挑的」**：原版的 `ShowEphemeral` 是拿组件在**运行时**实例化
        //    一个 prefab，dump 里看不到它的 rect；那个 prefab 本身也不在我们手上。
        //    挑左上角是因为**卡面那儿是空的**（右上角是费用宝石、左下两圆、右下盾+绿框、
        //    中间是卡名/效果/兵种），不会压到任何原版元素。
        const float EphemeralIconW = 0.30f, EphemeralIconH = 0.30f;      // 卡单位
        static readonly Vector2 EphemeralIconAt = new Vector2(0.115f, 0.082f);   // 左上角（x01/y01）

        // ---- 立绘：原版怎么装的，我们怎么跟 ----
        // 原版的立绘矩形（预制体 JSON `RectTransform_2364194910465924032`，战斗预制体）：
        //   2.7484 × 2.7484 @ (0, −0.065)，Image `m_PreserveAspect=0`
        //   —— **比卡本体大**，是**故意**要外溢的；由卡框上的 SoftMask（`MaskArea`=立绘的 RT、
        //   `FlipAlphaMask=1`）拿**卡框的 alpha** 把它切掉。
        //   出处：`子代理读报_2dcard_0827.md:310`「卡图 2.7484² 外溢…外溢部分被框贴图 Alpha 掩掉」
        //   + `CardImage_-4679357769477678144.json` / `CardFrame_7138976253573503936.json` 的组件字段。
        // 立绘图本身：纹理 1024²、sprite `textureRect = 670.5 × 1024`（宽高比 0.6548，卡本体 0.628，
        //   卡框矩形 0.689）—— 出处 `Sprite/SM_UM_inf_Aggressor Sergeant.json` 的 `m_RD.textureRect`
        //   （⚠️ 这个字段**旧解包没有**，见 `资料/资源使用手册.md` 顶部那条更正）。
        //
        // ⚠️ **2026-09-12 改**：原来这里有一组 `WinMinX/MaxX/MinY/MaxY` 常量 ——「对卡框 alpha 做膨胀 +
        //    flood fill 量出闭合透明区」当成立绘的裁剪框。那个区是**拱形**，拿它当**矩形**框用有两个后果：
        //    立绘的直角**顶出拱形**（症状就是「卡框没包住插图」），而且窗口以外根本不画立绘。
        //    现在的做法（见 `ArtMesh`）：**立绘铺满卡框那块矩形** + 卡框画在上面 ——
        //    卡框自己就是那个遮罩（不透明处盖住立绘、透空处露出立绘），和原版等价、少一层 mask。

        /// <summary>TMP 文字层的 z。比 `_info`(-0.02) 再靠前一点 —— **z 越小离相机越近**</summary>
        const float TextZ = -0.03f;

        public CardData Data { get; private set; }

        /// <summary>
        /// 🆕 **这张视图画的是手牌里的哪一份**（待办第 7 行 · 第 4 步，2026-09-18）。
        ///
        /// 为什么要有它：`BattleDriver.SyncHand` 原来按**卡名**把视图和引擎手牌配对 ——
        /// 手里两张同名卡时，配对结果纯粹看顺序（谁配谁都一样，所以看不出来）。
        /// 有了实例身份之后，配对、反查下标（`HandIndexOf`）都走**对象身份**，
        /// 表现层和引擎说的是同一张牌。**不是手牌里的视图时是 null**（场上/墓地/展示窗那些）。
        /// </summary>
        public RuleEngine.CardInstance Inst;

        /// <summary>自检用：卡框那一层现在用的是哪张贴图。
        /// **截图看不出「稀有度分档对不对」**（四档都是同一个形状的框），只能这么断言。</summary>
        public Texture2D FrameTexture
        {
            get
            {
                return (_frame != null && _frame.sharedMaterial != null)
                     ? _frame.sharedMaterial.mainTexture as Texture2D : null;
            }
        }

        /// <summary>🆕 2026-09-29：**卡上的立绘贴图**（阵亡爆散体要从卡上借它 —— 原版
        /// `_CardImage = rawCard.cardSprite.texture`）。优先取 3D 体材质上那一份，退回 2D 立绘层的。</summary>
        public Texture2D BodyArtTexture
        {
            get
            {
                if (_bodyMat != null && _bodyMat.HasProperty("_CardImage"))
                {
                    var t = _bodyMat.GetTexture("_CardImage") as Texture2D;
                    if (t != null) return t;
                }
                return ArtLayerTexture;
            }
        }

        /// <summary>自检用：立绘那一层现在用的是哪张贴图（同上，截图看不出用的是不是真插图）。</summary>
        public Texture2D ArtLayerTexture
        {
            get
            {
                return (_art != null && _art.sharedMaterial != null)
                     ? _art.sharedMaterial.mainTexture as Texture2D : null;
            }
        }

        /// <summary>自检/量尺用：卡框那一层**实际画出来多大**（世界单位，宽×高）。
        /// ⚠️ 截图看这个看不出来 —— 卡框贴图四周是透明的，肉眼量的是「金属」，量不到这一层真正的 quad。</summary>
        public Vector2 FrameDrawSize
        {
            get
            {
                var mf = _frame != null ? _frame.GetComponent<MeshFilter>() : null;
                if (mf == null || mf.sharedMesh == null) return Vector2.zero;
                var b = mf.sharedMesh.bounds.size;
                return new Vector2(b.x, b.y);
            }
        }
        /// <summary>卡框那层用的 UV 矩形（= 贴图上不透明金属的范围）</summary>
        public Rect FrameUvRect
        {
            get
            {
                var t = FrameTexture;
                return t != null ? FrameUv(t) : new Rect(0f, 0f, 1f, 1f);
            }
        }
        /// <summary>自检用：前景层（角色抠图）用的贴图 —— 没有前景层就是 null</summary>
        public Texture2D ArtFrontTexture
        {
            get
            {
                return (_artFront != null && _artFront.sharedMaterial != null)
                     ? _artFront.sharedMaterial.mainTexture as Texture2D : null;
            }
        }
        /// <summary>自检用：立绘底层是不是走了「忽略 alpha」那条 shader</summary>
        public string ArtShaderName
        {
            get
            {
                return (_art != null && _art.sharedMaterial != null && _art.sharedMaterial.shader != null)
                     ? _art.sharedMaterial.shader.name : "<无>";
            }
        }
        /// <summary>自检用：前景层的 z（要比卡框(0)靠前、比数值层(−0.02)靠后）</summary>
        public float ArtFrontZ { get { return _artFront != null ? _artFront.transform.localPosition.z : float.NaN; } }
        /// <summary>卡框那层的 z（判据用，恒为 0）</summary>
        public float FrameZ { get { return _frame != null ? _frame.transform.localPosition.z : float.NaN; } }

        /// <summary>立绘那一层**实际画出来多大**（世界单位）</summary>
        public Vector2 ArtDrawSize
        {
            get
            {
                var mf = _art != null ? _art.GetComponent<MeshFilter>() : null;
                if (mf == null || mf.sharedMesh == null) return Vector2.zero;
                var b = mf.sharedMesh.bounds.size;
                return new Vector2(b.x, b.y);
            }
        }

        /// <summary>自检用：底部那颗稀有度宝石用的是哪张图（颜色差别小，截图不好断言）。</summary>
        public Texture2D GemTexture
        {
            get
            {
                return (_gem != null && _gem.sharedMaterial != null)
                     ? _gem.sharedMaterial.mainTexture as Texture2D : null;
            }
        }

        MeshRenderer _face;      // 没有卡框时的整张卡面（= 根节点那个 `MeshRenderer`；**只有 `frameTex == null`
                                 // 那条兜底支路才非 null** ⇒ `SetData` 里换它材质那一段为什么要保留 `renderQueue`、
                                 // 以及「今天谁走得到这条支路」，都写在 `SetData` 那一段的注释里）
        MeshRenderer _frame;     // 原版卡框
        MeshRenderer _textBg;    // 🆕 文字底板（原版 `Front/Textbackgrounds/TextBackground * UI`）
        MeshRenderer _art;       // 立绘：**完整插图**（忽略 alpha，垫在卡框下）
        MeshRenderer _artFront;  // 立绘：**角色抠图**（真 alpha，盖在卡框上）—— 没有抠图的卡是 null
        MeshRenderer _gem;       // 卡面底部那颗稀有度宝石（原版 `Rarity`）
        MeshRenderer _costBg;    // 费用底板（原版 `Cost Container/Cost Background`，图 `Card_Frame_Cost_Icon`）
        MeshRenderer _armourIcon;// 护甲盾牌底（原版 `Armour Container/Image`，图 `pedestal_icon_armor`）
        MeshRenderer _info;      // 数值层
        MeshRenderer _rim;       // 状态描边
        /// <summary>场上那四个数值**各占一个图层**（索引 `StatMelee..StatHealth` = 1..4，0 空着不用）。
        /// 为什么要拆：原版「受击时**那一个**数值变色 + bump」是按 counter 分别做的
        /// （`CardTextCountersController.DoColorChange` + `cardTextBumpSize/Time`），
        /// 四个数字烘在一张贴图里就动不了单个。判据 → `资料/待办判据_战场与战斗视图.md` §26 第 3 条。
        /// ⚠️ **只在场上（`CardFace.Board`）建与显示** —— 手牌那个 2D 卡面的数值仍旧烘在 `_info` 里
        /// （那边数值不随战斗变，且拆了要重做一套几何）。两条路互斥，判据是 `_faceMode`。</summary>
        readonly MeshRenderer[] _stats = new MeshRenderer[5];
        /// <summary>四个数值各自的「闪现色」（`CardFeel.StatUpColor` / `StatDownColor`；白 = 没在闪）。
        /// 🔴 **必须存一份** —— `ApplyTint` 会把 `_layers` 里每一层刷成状态色，不重贴的话
        /// 受击变色撑不过一帧（和徽标那个 `_badgeAlpha` 是同一个坑）。</summary>
        readonly Color[] _statFlash = new Color[5];
        MeshRenderer _ephemeral; // 🆕 「临时卡」角标（原版关键词图标；默认关掉）

        // ---- 🆕 棋盘单位卡的 buff / debuff 徽标（原版 `BattleCardUI.boardTraitIcons`）--------
        // 判据（哪些关键词进位、认不认得出图、角标画不画）**全在 `Core/Badges.cs`**；
        // 这里只管画。位子/大小取自原版预制体 `TraitIconContainer*.json` 的 Transform，
        // 换算过程写在 `Badges.SlotAt` 上面 —— **不在这里写第二份**。
        readonly List<MeshRenderer> _badgePlates = new List<MeshRenderer>();
        readonly List<MeshRenderer> _badgeIcons = new List<MeshRenderer>();
        readonly List<TextMeshPro> _badgeCounters = new List<TextMeshPro>();
        /// <summary>现在画着的那几位（`SetBadges` 进来的那一份）—— 「按关键词脉冲」要拿它配对。</summary>
        readonly List<Badge> _badges = new List<Badge>();
        TextMeshPro _title;      // 卡名（TMP。没有字体资产时为 null，字烘在 _info 里）
        TextMeshPro _keywords;   // 关键词（同上）
        TextMeshPro _army;       // 阵营行（原版 `ArmyTextUnit` / `ArmyTextTactc`）
        TextMeshPro _race;       // 兵种行（原版 `RaceText`；战术卡没有）
        /// <summary>🆕 2026-10-18（`A985④`）**「由谁造出来的」那行字**（原版 `CreatedByText` 节点）。
        /// `null` = 没这一层（没有来源 / 没字体资产 / 场上形态）。判据见 <see cref="CreatedByAt01"/> 那一段。</summary>
        TextMeshPro _createdBy;
        /// <summary>这张视图现在要显示**哪个创建者**的卡名（`null` = 不显示）。
        /// 🔴 **必须存一份**：`BuildTextLayers` 会被 `SetData` 反复调（战场每刷新一次都走）
        ///    —— 不存的话每次刷新这行字都会被 `Fill(空)` 拆掉（`EphemeralShown` 那类状态同理）。</summary>
        string _createdByName;
        static Material _quadMat;

        readonly List<MeshRenderer> _layers = new List<MeshRenderer>();

        // ---- 卡面那一层「内层节点」+ 整卡倾摆（原版 `2DCard` / `AutoCardRotation`）----
        // 原版是**两层节点**：外层 `CardUI Reference`（布局摆位、写扇形角）→ 内层 `2DCard`
        // （`AutoCardRotation` 挂这里，写它**自己的** `localRotation`）。
        // 我们照抄这个结构：**本组件的根** = 外层（`SetPose` 写扇形角），`_tiltRoot` = 内层。
        // ⚠️ 合成一层就会把扇形角抹平 —— 倾摆写的是绝对 `localRotation`，见 `AutoCardRotation` 文件头。
        Transform _tiltRoot;
        AutoCardRotation _tilt;

        /// <summary>卡面各层的父节点 = 原版 `2DCard` 那一层（**倾摆就发生在它身上**）。
        /// 还没建（`Build` 之前）时退回根节点。</summary>
        internal Transform FaceRoot { get { return _tiltRoot != null ? _tiltRoot : transform; } }

        /// <summary>整卡倾摆组件（原版 `AutoCardRotation`；场上形态下**关掉**，见 `SetFace`）—— 自检用。</summary>
        public AutoCardRotation Tilt { get { return _tilt; } }

        /// <summary>造一张卡。parent 传手牌容器或战场容器。
        /// ⚠️ **默认 `CardFace.Full`**（= 放大窗 / 卡池 / 收藏 / 卡组编辑 / 选牌那一套，**含阵营行**）。
        /// 🆕 **手牌要显式传 `CardFace.Hand`**（同一套层、只少阵营行）—— 判据见 <see cref="CardFace"/>；
        /// 场上要用 `CardFace.Board`。</summary>
        /// <param name="clip">🆕 2026-10-08（A181）：**可选**视口裁切（画布像素 · 左上原点；`null` = 不裁 = 旧行为逐字不变）。
        /// 判据 / 边界 / 代价全在下面「视口裁切」那一大段。⚠️ 裁切要把画布矩形换算到卡的局部系 ⇒
        /// **位姿是那个换算的输入**：`Create` 时还没摆位，真正定下来的是后面的 `SetPose`
        /// （它会自动重裁），所以两条路给的值必须一致 —— 只给 `Create` 不给 `SetPose` 也能跑（第二次会修正）。
        /// 🔴 **2026-10-13（A435 辛 · A774）：这个形参是【显式覆盖】的口子**（生产代码今天一个都不传）。
        /// 「跟视口节点走」请用 <see cref="SetClipFromTree"/>（在 `SetPose` 之后调一次）——
        /// 传 `clip: null` **不等于**跟节点走（那是「不裁」，见 `SetClipFromTree` 的注释）。</param>
        public static CardView Create(Transform parent, CardData d, string name = null,
                                      CardFace face = CardFace.Full, PxRect? clip = null)
        {
            var go = new GameObject(name ?? d.title);
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<CardView>();
            v._faceMode = face;
            v.Build(d);
            if (clip.HasValue) v.SetClip(clip);
            return v;
        }

        /// <summary>这张卡的展示场景。</summary>
        public CardFace Face { get { return _faceMode; } }
        CardFace _faceMode = CardFace.Full;

        /// <summary>**这个展示场景印不印阵营行** —— 判据只有这一处：
        /// 建层那处（`BuildTextLayers` ③）与开关层那处（`SetFace`）都调它。
        /// ⚠️ 两处各写一遍 = 迟早不一致（本项目的老坑），别把判据抄出去。
        ///
        /// 🔴 **口径 = 用户 2026-09-22 裁定：「手牌 + 场上不印，其余都印」**（三档定义见 <see cref="CardFace"/>）。
        /// 🔴 **2026-10-17 由用户亲自指认实拍复核过**（`资料/原版参照图/用户实拍_1017/`）：
        ///    · `点击卡片查看详情的参考.png` —— **放大卡卡名（白）下面那行【橙色】的 `萨姆-罕` 就是阵营行**，
        ///      它**印着**；后景卡池同样「白名 + 橙阵营行」。
        ///    · `战斗截图参考.png` —— **手牌与场上都没有那一行**（场上还更简）。
        ///    ⇒ **裁定成立，本条即它的落地。**
        ///
        /// ⚠️ **为什么本地 prefab 里查出来是「哪都不印」**（`CardTextsController` 全方法表无开关 ·
        ///    170 个实例激活 **0 次**（对照 `RaceText` 153 次）· 全库 GO `m_IsActive` **153/153 全 false** ·
        ///    无第二条引用者）—— 那说的是**我们手上这份 PC 版 bundle（2026-08）**，而**用户实拍那台客户端
        ///    与它不是同一版**（同一张照片里「卡名/阵营行」两行颜色对调，手机色偏是全局的、不可能只翻这两行）。
        ///    ⇒ **判据以实拍（用户那台在跑的客户端）为准**，本地 bundle 的读数不能反推「原版就是这样」（铁律 5·c）。
        ///
        /// 📌 **本条来回过一次，别再来回**：2026-10-17 我曾把它改成恒 `false`（误读了用户对「卡名颜色」
        ///    的一句回答），**用户当天指认实拍后改回**。再要动它，**先去看那两张实拍**。</summary>
        public static bool FaceShowsArmy(CardFace f)
        {
            return f != CardFace.Board && f != CardFace.Hand;
        }

        /// <summary>**该不该印阵营行**（口径；与字体资产拿不拿得到无关）—— 自检用。
        /// 与 <see cref="ArmyShown"/> 的分工：这条断「**档位选对了**」，那条断「**真印出来了**」。</summary>
        public bool ArmyLineWanted { get { return FaceShowsArmy(_faceMode); } }

        /// <summary>**阵营行现在是不是真印着的**（实况：层建出来了**且**开着）—— 自检用。
        /// ⚠️ 取不到字体资产（`TmpFont.Available == false`）时**恒 false**（那条路根本不让挂 TMP）
        /// ⇒ 断它之前先断 `TmpFont.Available`，否则「没验到」会伪装成「验过了」。</summary>
        public bool ArmyShown { get { return _army != null && _army.gameObject.activeSelf; } }

        /// <summary>🆕 2026-10-17（D5）：**兵种行现在印的是什么字**（实况）—— 自检用。
        /// 与 <see cref="SubtypeLine"/> 的分工：那条是**口径**（该不该印、印哪个词），
        /// 这条是`_race` 上**真挂着的那个串**（没这一层 / 没字 ⇒ `null`）。
        /// ⚠️ 取不到字体资产时**恒 `null`** ⇒ 断它之前先断 `TmpFont.Available`（同 <see cref="ArmyShown"/>）。</summary>
        public string RaceText { get { return _race != null ? _race.text : null; } }

        /// <summary>🆕 2026-10-17（D5）：**兵种行现在是不是真印着的**（层建出来了**且**开着）—— 自检用。
        /// ⚠️ 同 <see cref="ArmyShown"/>：字体资产不在就恒 `false`（假绿陷阱）。</summary>
        public bool RaceShown { get { return _race != null && _race.gameObject.activeSelf; } }

        /// <summary>**整张卡换 layer**（含所有子节点）。
        /// 🔴 2026-09-20 加：**场上的卡要搬进 3D 那一层**（`ArenaSlots.ArenaLayer`），
        /// 由透视相机画；手牌仍留在正交相机那层。出牌是「搬视图不重建视图」⇒ 这一步必须显式做，
        /// 漏了的话那张卡**两台相机都不画**（正交的 cullingMask 把它摘了、透视的又不认这一层）
        /// —— 静默消失，正是最坏的那种失败。
        /// ⚠️ 只递归**子节点**、不改自己以外的任何东西。</summary>
        public void SetLayer(int layer)
        {
            gameObject.layer = layer;
            foreach (Transform t in transform) SetLayerRec(t, layer);
        }

        static void SetLayerRec(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform c in t) SetLayerRec(c, layer);
        }

        /// <summary>**换展示场景**（手牌 → 场上那条路要用）。
        ///
        /// 为什么要有它：出牌时视图是**搬过去**的、**不重建**（重建会丢落位动画 ——
        /// 见 `BattleDriver.DeployCard` 的注释），所以那张卡出生时是**手牌那一套**，
        /// 到了场上得把「场上不画的层」关掉。实测漏了这一步的后果：自己打出去的兵
        /// **在场上仍然带着卡框和效果文字**，而督军（出生就在场上）是对的 —— 一眼就看得出不一致。
        ///
        /// 实现上**只关 GameObject、不销毁**（省得动画/布局被弄丢），数值层重烘成 `statsOnly` 那一版。
        /// </summary>
        public void SetFace(CardFace f)
        {
            if (_faceMode == f) return;
            _faceMode = f;
            bool board = f == CardFace.Board;

            // 🔴 整卡倾摆跟着形态开关（原版：场上把 `2DCard` 整个 `Toggle(false)` ⇒ 倾摆也停）。
            //    入口**只有这一处**（铁律 10 第 5 条）—— 手牌打出去走的就是这里。
            if (_tilt != null) _tilt.enabled = !board;

            // 🔴 **2026-09-20 补：3D 卡体也属于「场上那一套」，必须在这一层里一起换。**
            //    出生就在场上的卡（督军 / AI 出的牌）走 `Create(face: Board)` ⇒ `Build` 里已经建好；
            //    而**手牌打出去是「搬视图不是重建视图」**（`BattleDriver.DoPlay`，为了不丢落位动画）
            //    ⇒ 只能在这儿补建。原来漏了这一步的后果：**自己打出去的兵在场上是一张平面贴纸**
            //    （没有 3D 体、还留着 2D 立绘层），而督军是对的 ——
            //    和卡框那次是**同一个「多个出生/迁移入口」坑**（`CLAUDE.md` 铁律 10 第 5 条），
            //    3D 卡体是后加的，于是又踩了一遍。
            if (board && _body3D == null) _body3D = BuildBody3D(ArtTexture(Data));
            Show(_body3D, board);
            // 2D 立绘层与 3D 体**二选一** —— 口径和 `Build` 里那条一致。
            // ⚠️ **只有真建出 3D 体才去关它**：网格/shader 取不到时，正当的退回就是「照画 2D 立绘」
            //    （`BuildBody3D` 已经报警告），无条件关掉会变成**场上什么都没有**（静默）。
            if (_body3D != null) Show(_art, !board);
            else if (!board && _art == null)
                // ⚠️ 这条**现在不会触发了**：2026-09-29 起场上形态也建 `_art`（回手的「2D 卡面淡回来」
                //    那一半要用它），所以「场上建的卡没有 2D 立绘层」这个前提不再成立。
                //    留着是防**将来**再有人改回「只在退回 2D 时建」那一版。
                Debug.LogWarning("[CardView] 从场上形态退回手牌形态，但这张卡是按场上形态建的"
                               + "（没有 2D 立绘层）⇒ 会没有立绘。本工程目前没有这条路径"
                               + "（`SetFace` 只用于「手牌 → 场上」），出现了就是有新的入口，要补层。");

            Show(_frame, !board);
            Show(_artFront, !board);
            Show(_textBg, !board);       // 效果文字底板
            Show(_costBg, !board);       // 费用六边形
            Show(_gem, !board);          // 稀有度宝石
            Show(_title, !board);
            Show(_keywords, !board);
            // 阵营行**不跟 `!board` 走**：它按 `FaceShowsArmy` 分档（手牌与场上**都不印**）。
            // 🔴 这里的判据与 `BuildTextLayers` ③ 那条**必须是同一个函数** —— 换档是唯一的入口
            //    （铁律 10 第 5 条：手牌打出去走的就是这里），两处不一致 = 出了牌阵营行还挂在场上。
            Show(_army, FaceShowsArmy(f));   // 阵营行
            // 🆕 2026-09-29：**那圈状态环的摆位也跟着换** —— 它圈的是「看得见的那张卡」，
            //   而场上（3D 卡体）与手牌（2D 卡）两张壳的几何不同，见 `PlaceRim`。
            //   ⚠️ `CLAUDE.md` 铁律 10 第 5 条：换场景的入口**只有这一处** ⇒ 必须在这里显式设一次
            //   （漏了就是「形态切了、环还停在旧位置」—— 和 3D 卡体那次是同一个坑）。
            if (_rim != null) PlaceRim(_rim.transform);
            Show(_race, !board);         // 兵种行
            // 🆕 2026-10-18（`A985④`）「由谁造出来的」**跟着形态一起关** ——
            //    🔴 铁律 10 第 5 条：换形态的入口**只有这一处**，不在这里显式设一次就是
            //    「手牌打出去的兵在场上还挂着这行字」（原版 `DisplayCreatedByText` 在 inPlay 那几档
            //    是**先把节点关掉再 return** 的 ⇒ 场上不印）。
            Show(_createdBy, !board);
            // ⚠️ **`_armourIcon` 不关**：护甲是数值，原版场上也显示（`Board Elements` 里有它）
            // 🔴 但**位置要跟着换**（2026-09-20）：2D 卡面与 3D 卡体是两套坐标，盾牌不跟着走
            //    就会和它上面那个数字**分家**（`SpriteQuad` 把位置烘进 mesh ⇒ 换 mesh，不是换 transform）。
            if (_armourIcon != null)
            {
                var amf = _armourIcon.GetComponent<MeshFilter>();
                if (amf != null) amf.sharedMesh = SpriteQuad(board ? "armourIconBoard" : "armourIcon",
                                                             board ? BoardArmourAt : ArmourIconAt,
                                                             ArmourIconW, ArmourIconH);
                // z 也要换：场上是 3D 卡体，覆盖层必须落在**卡体正面之前**（见 `BoardInfoZ` 那段注释）
                var ap = _armourIcon.transform.localPosition; ap.z = board ? BoardArmourIconZ : ArmourIconZ;
                _armourIcon.transform.localPosition = ap;
            }
            // 数值层同理（它是**整卡大小的一张 quad**，场上的数字靠它画）
            if (_info != null)
            {
                var ip = _info.transform.localPosition; ip.z = board ? BoardInfoZ : InfoZ;
                _info.transform.localPosition = ip;
            }
            // 徽标的三个层 z 是常量（`BadgePlateZ`/`BadgeIconZ`/`BadgeCounterZ`）——
            // 徽标**只在场上出现**，那些常量本身已经改到卡体正面之前了。
            if (_info != null) _info.sharedMaterial.mainTexture = InfoTexture(Data, board, SplitStats(Data));

            // 🆕 2026-09-29：四个数值层跟着形态一起开关。🔴 **这里的 `skipStats` 与 `Build` 那条
            //   必须是同一个判据**（`SplitStats`）—— 不然手牌→场上那一步会把数字**画两遍**
            //    （`_info` 里烘一份 + 四个独立层再画一份）。
            BuildStatLayers();
            SyncStatLayers();
            // 🆕 2026-10-08（A181）：换形态 = 换掉/开关了一堆层 ⇒ **重裁一次**（只在已裁过时走）
            //    🔴 2026-10-13（A435 辛 · A774）：判据 = `Clipping`（显式矩形**或**跟随节点），理由见 `SetData` 那一处。
            if (Clipping) ApplyClip();
        }

        /// <summary>**场上要不要把四个数值拆成独立图层** —— 判据只有这一处（`Build` 与 `SetFace`
        /// 的 `_info` 重烘都要用它，两处不一致就会把数字画两遍）。</summary>
        bool SplitStats(CardData d)
        {
            return _faceMode == CardFace.Board && d.isUnit && PragatiDigits.Available;
        }

        static void Show(MeshRenderer r, bool on) { if (r != null) r.gameObject.SetActive(on); }
        static void Show(TextMeshPro t, bool on) { if (t != null) t.gameObject.SetActive(on); }

        // ==================================================================
        //  残骸体（`RemnantBody3D <阵营>`）—— 引擎里「这一格是残骸」的画法
        // ==================================================================

        /// <summary>
        /// **把这一格画成残骸**（灵族 = 一枚漂浮的灵魂石 · 死灵 = 一张碎裂的卡）。
        ///
        /// 【读】原版 `BattleCardUI.CreateRemnantBody` 三步（`资料/查证_useWaystone_语义.md` §六）：
        ///   ① 在**卡的 transform 下**实例化 `RemnantBody3D &lt;阵营>`（Addressable）；
        ///   ② `Initialize(card)` + `CardHighlight.SetRemnantHighlight`；
        ///   ③ `RemnantBody.BodyVisibilityToggle → BattleCardUI.ToggleBody3D(false)`
        ///      —— **把原卡的 3D 卡身关掉**。
        /// ⇒ 形态 = 「**盖一具残骸体 + 关掉原卡卡身**」。**不是翻面、更不是卡背**
        ///   （全量签名桩 grep 不到 flip/faceDown；`ShowCardBack` 只给手牌用）。
        ///
        /// 🔴 **两个必须做对、否则静默出错的地方**：
        ///   ① 那两个 prefab 的**根节点停在 `x = 100`**（原版那套「后台位置」——
        ///      同族症状见 `WarpforgeEffectPlayer.Play` 的注释）⇒ 实例化后**必须把
        ///      `localPosition` 归零**，不归零就是整具残骸体画在场景外面，**一声不响**。
        ///   ② **必须走 `WarpforgeEffectPlayer.Play`**（而不是裸 `Instantiate`）——
        ///      原版那批 prefab 的材质是**运行时绑**的（`WarpforgeEffectBinder` 按名找原版 shader、
        ///      换掉引用），裸 `Instantiate` 会把它整具渲成**黑块**。
        ///      ⚠️ 这条 2026-09-25 **实拍踩过**：第一版就是裸 `Instantiate`，截图里两具残骸体
        ///         都是黑乎乎的一块（`_tmp_view/battle/29_残骸体_灵族与死灵.png` 的旧版）。
        ///      寿命用 `float.PositiveInfinity` 交给调用方 —— 残骸体要**一直挂着**，
        ///      直到被收集（灵族）或被摧毁，**不能用原版那个 `destroyTime` 自毁**。
        ///
        /// ⚠️ **根缩放 = 复制原卡卡身的 localScale**（`_body3D.transform.localScale`，我们这边 = `Body3DScale`
        ///    0.88586）。**出处（反编译逐行读的）**：`BattleCardUI__CreateRemnantBody.c` 末尾那段 ——
        ///    取 `*(card + 0x180)`（= `BattleCardUI.minion3DRenderer`，也就是 **`Card 3D` 那个节点**，
        ///    见 `资料/3DBody_原版场上卡体规格.md:171/59`）的 `transform.localScale`，
        ///    再 `set_localScale(remnantRoot, 它)`。卡身缩放是 0.88586（同资料 `:40`）⇒ 残骸体根也是 0.88586。
        ///    🔴 **第一版这里我写成「取 1」**（理由是 prefab 内部那枚 `To remnant` 节点也正好是 0.8858599，
        ///    以为缩放已经烘进去了）—— 那是**看 prefab 数值猜的**，不是读代码。已按反编译改回。
        ///    ⚠️ 两种取法的视觉差约 13%，**没有跟原版实况并排核过**（原版已关服）。
        /// </summary>
        /// <param name="on">true = 盖残骸体（并关掉原卡卡身）；false = 撤掉、恢复原卡</param>
        /// <param name="prefabName">残骸体 prefab 名（= 效果库里的键），如 `RemnantBody3D Aeldari`</param>
        public void SetRemnantBody(bool on, string prefabName)
        {
            if (!on)
            {
                if (_remnantBody != null) _remnantBody.SetActive(false);
                // 原卡卡身跟着回来。**两条路都要管**：
                //  · 有 3D 卡体（正常情形）⇒ 按形态开关它（`SetFace` 里那条同一口径）；
                //  · **没有 3D 卡体**（网格/shader 取不到，`BuildBody3D` 已报警告的那条退回路径）⇒
                //    卡身就是 **2D 立绘层** —— 上面那句 `Show(_art, false)` 把它关了，
                //    这里不恢复的话，那一格会**只剩残骸体、撤掉之后变成空白**（静默）。
                //    ⚠️ 2026-09-25 复查时发现的。
                //    🔴 **2026-10-15 更正**：这里原来把触发条件写成「`use3DBoard == false` 那种配置下才现形」
                //       —— **不成立**。这一支的判据是 **`_body3D == null`**，而 `BuildBody3D` 在
                //       `Build:1429` 与 `SetFace:640` **都是无条件调**的（不看 `use3DBoard`：那个开关只管
                //       `PoseFor` 走哪套坐标 / 挂不挂 `ArenaLayer`）⇒ 把 3D 战场关掉**照样**会建出 3D 卡体，
                //       这一支**不会**因此现形。错因 = 按「哪套坐标」猜了「哪套卡身」。
                //       真触发它的是**网格或 shader 取不到**那一档（`BuildBody3D` 的三条 `return null`
                //       之一，会被它自己那条 warning 报出来）；自检里借 `DebugNoBody3D` 造这一态。
                if (_body3D != null) Show(_body3D, _faceMode == CardFace.Board);
                else if (_faceMode == CardFace.Board && _art != null) Show(_art, true);
                return;
            }

            if (_remnantBody == null)
            {
                var lib = WarpforgeVFX.WarpforgeEffectLibrary.Instance;
                WarpforgeVFX.WFEffectEntry entry;
                if (lib == null || !lib.TryGet(prefabName, out entry) || entry.prefab == null)
                {
                    // 不许静默失败：点明是谁、以及修法（效果库要重生成）
                    Debug.LogWarning($"[CardView] 残骸体 '{prefabName}' 取不到 —— 效果库里没有它，"
                                   + "或者 prefab 引用是空的（重导过 prefab 就要跑一次 "
                                   + "Tools > Warpforge > 生成效果库）。这一格会照旧显示原卡，"
                                   + "看起来像「残骸没画出来」。");
                    return;
                }
                // ⚠️ `Play` 会把根 `localPosition` 设成我们传的那个值（见它的注释），
                //    所以这里直接传 `Vector3.zero` —— 那族 prefab 停的 `x = 100` 就被归位了。
                //    `scale = 1` 时它**不动** prefab 自己的缩放（下一步我们才自己设）。
                var player = WarpforgeVFX.WarpforgeEffectPlayer.Play(
                    entry, transform, Vector3.zero, 1f, float.PositiveInfinity);
                if (player == null) return;
                _remnantBody = player.gameObject;
                // 缩放照抄原版：**卡身那个节点的 localScale**（见上面注释里的出处）。
                _remnantBody.transform.localScale = _body3D != null
                    ? _body3D.transform.localScale
                    : Vector3.one * Body3DScale;
                // 残骸体**不参与整卡着色**（`_layers` 是「整卡着色」名单：高亮/置灰/淡出都会
                // 把每一层刷成同一个色）。残骸体有自己的材质与光照，进去会被刷坏。
                // ⚠️ 别把它 `_layers.Add` —— 同族的坑见 `CLAUDE.md` 铁律 10 第 4 条。
            }
            else _remnantBody.SetActive(true);

            // 关掉原卡卡身（原版 `ToggleBody3D(false)`）。2D 立绘此时一定已经是关的（场上是 3D 形态）
            if (_body3D != null) Show(_body3D, false);
            if (_art != null) Show(_art, false);
            // 🆕 2026-09-29：原版 `CreateRemnantBody` **就在这一步**调 `SetRemnantHighlight` ⇒ 出生时先把
            // 当前那一档的状态色刷上去（不刷的话它会停在内建的 `(0,1,1,0)` 上 = 看着像「永远不亮」）。
            SetRemnantLight(CardHighlight.FrameColorOf(State));
        }

        /// <summary>残骸体现在显示着吗（没建过 / 已撤掉都是 false）。自检用它 ——
        /// 比的是「残骸体在不在」，不是我们自己的常量。</summary>
        public bool RemnantBodyVisible { get { return _remnantBody != null && _remnantBody.activeSelf; } }

        /// <summary>残骸体根节点（没建过是 null）—— 自检量它的**渲染真值**（位置 / 缩放 / 材质）用。</summary>
        public GameObject RemnantBodyRoot { get { return _remnantBody; } }

        // ==================================================================
        //  残骸体那圈光（原版 `FrameHighlightRemnant` = `RemnantBody3D <阵营>/RemnantLight`）
        // ==================================================================
        //
        // 🔴 **2026-09-29 接线**（原来记的是「`m_Sprite` 是空的 ⇒ 这层永远是黑的」）：
        //  · 原版那颗 sprite = **`Glow UI W40K`**（**123×123 · PPU 100 · pivot .5/.5**，实读
        //    `bundle_duplicateassetisolation_assets_all/Sprite/Glow UI W40K.json`）；
        //    材质 = 内建 `Sprites-Default`；prefab 里的初始色 `(0,1,1,0)`。
        //  · **谁驱动它**（反编译实读 `CardHighlight__SetRemnantHighlight.c` 全文 30 行）：
        //    把 `+0x70`（**主状态环**那个 `SpriteRenderer`）的 `color` **原样拷给** `+0x78`（残骸那具）
        //    —— 也就是**它跟着状态色走**（`regular` 那档是 alpha 0 ⇒ 平时不亮；成为合法目标/被选中才亮）。
        //    唯一调用点 = `BattleCardUI__CreateRemnantBody.c`（残骸体出生的那一步）。
        //  · 🔴 **为什么在运行时建 sprite、不去改 prefab**（**如实标注：这是我们挑的做法** ——
        //    原版是资产里就挂好的）：那两个 prefab 在 `Assets/WarpforgeVFX/Prefabs/`，
        //    整个目录在 `.gitignore` 里（1.8 GB 大件）⇒ 手工改 prefab **重导一次就静默没了**；
        //    代码这条路可复现、可自检、取不到会出声。
        static Sprite _remnantGlowSprite;
        SpriteRenderer _remnantLight;

        SpriteRenderer RemnantLightRenderer()
        {
            if (_remnantLight != null) return _remnantLight;
            if (_remnantBody == null) return null;
            foreach (var sr in _remnantBody.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr != null && sr.gameObject.name == "RemnantLight") { _remnantLight = sr; break; }
            return _remnantLight;
        }

        /// <summary>原版 `CardHighlight.SetRemnantHighlight` —— 残骸体那圈光的状态色（跟着主状态环走）。</summary>
        public void SetRemnantLight(Color c)
        {
            if (_remnantBody == null || !_remnantBody.activeSelf) return;
            var sr = RemnantLightRenderer();
            if (sr == null)
            {
                Debug.LogWarning("[CardView] 残骸体里找不到 `RemnantLight` 那个 SpriteRenderer"
                               + "（prefab 结构变了？）⇒ 残骸体不会有状态色（不静默）");
                return;
            }
            if (sr.sprite == null)
            {
                var sp = RemnantGlowSprite();
                if (sp == null) return;                  // 图取不到 —— `RemnantGlowSprite` 已经出过声
                sr.sprite = sp;
            }
            sr.color = c;
        }

        /// <summary>原版那颗 `Glow UI W40K`（123×123 @PPU100 · pivot .5/.5）。见上面「为什么运行时建」。</summary>
        static Sprite RemnantGlowSprite()
        {
            if (_remnantGlowSprite != null) return _remnantGlowSprite;
            var tex = CardArt.MenuUi("Glow_UI_W40K");
            if (tex == null)
            {
                Debug.LogWarning("[CardView] 残骸体那圈光：取不到贴图 `Resources/Art/ui_menu/Glow_UI_W40K.png`"
                               + "（`Resources/Art/` 整个在 `.gitignore` 里 ⇒ 新克隆没有）⇒ 那圈**不亮**。"
                               + "重建：把原版那张 `Glow UI W40K` 拷成那个路径即可");
                return null;
            }
            var sp = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sp.name = "Glow UI W40K";                    // 自检按这个名比
            _remnantGlowSprite = sp;
            return sp;
        }

        /// <summary>自检用：残骸体那圈光**挂上 sprite 了吗**（应当是 `Glow UI W40K`；没建/取不到是 null）。</summary>
        public string RemnantLightSpriteName
        {
            get
            {
                var sr = RemnantLightRenderer();
                return (sr != null && sr.sprite != null) ? sr.sprite.name : null;
            }
        }
        /// <summary>自检用：残骸体那圈光**现在的颜色**（应当 = 主状态环那一档的状态色）。</summary>
        public Color RemnantLightColor
        {
            get { var sr = RemnantLightRenderer(); return sr != null ? sr.color : Color.clear; }
        }

        /// <summary>场上那张 3D 卡体的**世界缩放**（没建过是 zero）。自检拿它比残骸体 ——
        /// 原版 `CreateRemnantBody` 就是把卡身那个值复制到残骸体根上的（出处见 `SetRemnantBody`）。</summary>
        public Vector3 Body3DWorldScale
        {
            get { return _body3D != null ? _body3D.transform.lossyScale : Vector3.zero; }
        }

        // ---- 自检用 ----
        /// <summary>卡框那一层现在可见吗（**场上必须为 false**）。</summary>
        public bool FrameVisible { get { return _frame != null && _frame.gameObject.activeSelf; } }
        /// <summary>效果文字底板那一层现在可见吗（场上为 false）。</summary>
        public bool TextBgVisible { get { return _textBg != null && _textBg.gameObject.activeSelf; } }
        /// <summary>费用六边形可见吗（场上为 false）。</summary>
        public bool CostVisible { get { return _costBg != null && _costBg.gameObject.activeSelf; } }
        /// <summary>稀有度宝石可见吗（场上为 false）。</summary>
        public bool GemVisible { get { return _gem != null && _gem.gameObject.activeSelf; } }

        /// <summary>2D 立绘那块 quad 可见吗。
        /// 🔴 **场上且 3D 体建出来了 ⇒ 必须为 false**（两者二选一，见 `SetFace`）——
        /// 2026-09-20 那条真 bug 的症状就是「场上为 true」（手牌打出去的兵还露着 2D 立绘）。</summary>
        public bool ArtVisible { get { return _art != null && _art.gameObject.activeSelf; } }

        /// <summary>场上那张 3D 卡体现在可见吗（**手牌打出去之后也必须为 true**）。</summary>
        public bool Body3DVisible { get { return _body3D != null && _body3D.gameObject.activeSelf; } }

        /// <summary>🆕 2026-09-29：**开关 3D 卡体**（原版 `BattleCardUI.ToggleBody3D(bool)` 那一支）。
        /// 阵亡时第一件事就是把它关掉（`CardScript.UnitDeath`：`battleCardUI.body3D.SetActive(false)`），
        /// 画面上的「卡」随即换成在卡位另生成的那个爆散体 —— 判据 → `CardFeel.DeathExplosion`。</summary>
        public void SetBody3DVisible(bool on) { Show(_body3D, on); }

        /// <summary>🆕 自检用：卡底那枚软阴影（没有 3D 卡体时是 null）。断言比它的**渲染真值**
        /// （`WorldDiameter` / `CurrentColor` / `HeightT`），别抄我们自己的常量。</summary>
        public BlobShadow Shadow { get { return _blobShadow; } }

        /// <summary>自检用：那层材质**模板**上 `_Outline` 的 alpha。
        /// 原版材质是 **0**（平时不描边）—— 不是 0 的话每张卡会多一圈**白框**（shader 默认就是不透明的白，实测踩过）。
        /// ⚠️ 看的是**模板**不是某张卡：卡上那个值现在会被状态驱动（打得出去就点亮）。</summary>
        public static float SdfTemplateOutlineAlpha
        {
            get
            {
                var m = SdfMaterial();
                return (m != null && m.HasProperty("_Outline")) ? m.GetColor("_Outline").a : -1f;
            }
        }

        /// <summary>那圈高亮描边现在的颜色（自检用；场上恒为透明 —— 场上是 3D 卡体，原版那层 SDF 关着）。</summary>
        public Color OutlineColor
        {
            get
            {
                var m = _shadowLayer != null ? _shadowLayer.sharedMaterial : null;
                return (m != null && m.HasProperty("_Outline")) ? m.GetColor("_Outline") : new Color(0f, 0f, 0f, 0f);
            }
        }

        /// <summary>描边的诊断串（自检失败时打出来用）</summary>
        public string OutlineDebug
        {
            get
            {
                string tw = _outlineTween == null ? "无补间"
                          : (_outlineTween.IsActive() ? $"补间跑着 {_outlineTween.Elapsed():F3}s" : "补间已停");
                return $"{tw}·目标={_outlineTarget}·现在={OutlineColor}·alpha={_alpha:F2}·层={(_shadowLayer != null)}";
            }
        }

        /// <summary>换卡面（同一张卡换数据时用，比如手牌换牌）。</summary>
        public void SetData(CardData d)
        {
            // 🔴 **必须赶在 `Data = d` 之前** —— 之后就看不到旧值了，而「涨了还是落了」要旧值。
            CaptureStatChanges(d);
            Data = d;
            // 🔴 **2026-10-03（A87①）换这张兜底卡面的材质要【保留】换之前那份显式分好的 `renderQueue`** ——
            //    做法与 `Battle/ImageQuad.cs` 的 `SetMaterial`（A85 的通用修）**逐字同一条**：先读旧队列、
            //    `q >= 0` 才写回。为什么这里也要有：`FaceMaterial(d)` 是 `new Material(_quadMat)`，基材
            //    `Sprites/Default` 的 SubShader 标签是 `QUEUE: Transparent` = **3000**
            //    ⇒ 不写回就把 `CardFan.SetCardQueue` 显式分好的那一档**静默**抹掉。
            //  ⚠️ **判「有没有谁真的踩到」（铁律 5·c：先把「哪条支路才会走到」理清）**：
            //    · `_face` **只在 `frameTex == null` 那条兜底支路**才非 null（见下面 `Build` 的 `else`；
            //      有卡框时走 `_frame`，而 `SetData` 不碰 `_frame`）⇒ 出事的前提 = **这张卡没有阵营卡框图**。
            //    · **主路永远有框**：`frameTex` 只认 `d.faction`，而 `ToCardData` 那条主路喂的是卡池
            //      （`cards_engine.json` 的 1126 张**逐张核过 `faction`**，只有 13 个）+ `StarterCards`
            //      自设计的 Ember / Tide = **15 个**；`Resources/Art/cards/` 里这 15 个阵营 × (troop +
            //      stratagem) × tier1–4 = **150 张一张不缺**（2026-10-03 逐张核过）。
            //    · **那条兜底支路今天仍有活读者**（别把它当死路删）：凡**不经过 `ToCardData` 的 `new CardData`** ——
            //      ① `BattleDriver` 选牌面板的「选项卡」（`BattleDriver.cs` 里选牌面板选项卡那处 `faction = source != null ? source.Faction : null` 一带：
            //         `faction = source != null ? source.Faction : null` ⇒ **没有源卡的选项就是 null**）；
            //      ② `CardData.Simple(…)`（`faction = null`，自检探针）；③ `Editor/FontProbe.cs`。
            //    · **而真要「掉档」还得多一个条件** = 那张卡先被 `CardFan.SetCardQueue` 分过档、之后又 `SetData`。
            //      全工程这个顺序**只有一处**：`Battle/BattleDriver.cs` 里那处 `CardFan.SetCardQueue(_logCard, …)` → 之后 `SetData`（日志悬停卡，
            //      `SetCardQueue(…, OverlayQ+1)`，之后**每次**换卡都会再 `SetData` 一次）——
            //      而它喂的**恒是 `ToCardData` 主路的真卡**（有框）⇒ **今天一次都踩不到**
            //      （修前修后行为相同，都是空操作）。
            //    · **但它是「删掉 `Resources/Art/`」那一档的活缺陷**（`CardArt.cs:8-10` 明写这是**支持的模式**：
            //      删目录就退占位卡面）—— 那时**每一张**卡（含上面那张日志卡）都走兜底支路 ⇒ 卡面掉回 3000、
            //      被**整块日志面板（4000）盖住**，画面上就是「日志里点出来的那张卡是空的」，而且
            //      **没有断言盯着它**（灰化那类只看 shader 名）。这是**静默失败**（`CLAUDE.md` §三）
            //      ⇒ 照修、不照留。
            //  ⚠️ `q < 0`（旧材质读不出队列）**才不写** —— 与 `ImageQuad.SetMaterial` 同口径；对「新旧都是 3000」
            //    的调用是**恒等变换**（`Material.renderQueue` 对没显式设过队列的材质返回的正是 SubShader 标签）。
            //  ⛔ **下面 `Build` 的 `else` 支路那次【首次赋值】不要跟着改** —— `Create` 就是 `AddComponent` +
            //    `Build`，那时**没有任何调用点碰过 `SetCardQueue`**、没有「显式分好的档」可保留，
            //    而渲染器出厂那份材质**不是我们建的**（它的队列值不是我们的口径）⇒ 照抄「保留」只会
            //    引进一个我们没定义过的数（可能把占位卡面塞进不透明队列）。
            //  📌 **复核办法**（本次没跑 Unity，留给自检）：造一张 `CardData.Simple(…)`（`faction = null`
            //    ⇒ 必走兜底支路）→ `CardFan.SetCardQueue(v, 4444)` → `v.SetData(同一份)` →
            //    断 `v.GetComponent<MeshRenderer>().sharedMaterial.renderQueue == 4444`。
            //    ✅ **2026-10-05（A90②）已落进自检** = `Editor/BattleScene.cs` 的「⑤·c」那一段
            //    （三条：① 前提=确实走了兜底支路 ② `SetData` 真换了新材质 ③ 队列仍是 4444）
            //    —— 删掉上面那句 `if (qFace >= 0) fm.renderQueue = qFace;` ⇒ 那条**必红**（退回 3000）。
            if (_face != null)
            {
                int qFace = _face.sharedMaterial != null ? _face.sharedMaterial.renderQueue : -1;
                var fm = FaceMaterial(d);
                if (qFace >= 0) fm.renderQueue = qFace;
                _face.sharedMaterial = fm;
            }
            if (_info != null)
            {
                _info.sharedMaterial.mainTexture = InfoTexture(d, _faceMode == CardFace.Board);
                if (_art != null) _art.sharedMaterial.mainTexture = ArtTexture(d);
                // 场上那份立绘在 **3D 卡体**上（材质的 `_CardImage`），不是 `_art` 那块 quad ⇒ 单独跟一遍
                SetCardImageTo3DBase();
            }

            // TMP 那两层跟数据走（名字/关键词可能整条换掉）。
            // ⚠️ **原地更新，不要销毁重建** —— 这条路在「战场每刷新一次」时都会走
            //    （`BattleDriver.cs` 的 `RefreshAll` 那一支，掉血/疲劳都要反映到卡面），而 Play 模式下
            //    `Destroy` 要等帧末，重建的话**那一帧新旧两份字会叠在一起**。
            // 🔴 **2026-09-19 补判据（真 bug）**：这里原来**无条件**调 —— 而 `Build` 那条（本文件 :884）
            //    是带 `_faceMode != CardFace.Board` 的。后果：**场上卡每刷新一次就把名字/关键词/兵种行
            //    重新建出来**（掉血、加 buff、每回合刷新都会走 `SetData`）⇒ `SetFace(Board)` 那次隐藏
            //    被这一步**覆盖回去**，场上卡上一直挂着「名字 + 技能 + Warlord」三行字。
            //    表现：卡下面露出一行字（原来贴纸式的 2D 立绘正好被它盖在卡内，**看不出来**；
            //    3D 卡体变矮之后才明显）。判据与 `Build` 那条**必须是同一个**。
            if (_faceMode != CardFace.Board) BuildTextLayers(d);

            // 🆕 徽标跟着走：掉血/中 buff/获得关键词都会走到这条路（`BattleDriver` 每刷新一次都调）
            SetBadges(d.badges);
            // 🆕 2026-09-29 四个数值层同理（贴图/位置/闪现色一起刷）。
            // ⚠️ **`BuildStatLayers` 也在这儿补一次**：`Build` 那条只在「卡框贴图取得到」的分支里
            //    调得到（`_info` 建不出来时整块跳过），而数值层**不依赖卡框** ——
            //    漏了这一步就会出现「场上这张卡没有数字」这种静默缺件。
            BuildStatLayers();
            SyncStatLayers();
            // 🆕 2026-10-08（A181）：**重裁一次**（换卡 = 换网格/换文字 ⇒ 上一次那一刀作废）。
            //    ⚠️ 只在**已经裁过一次**时走（`_clipOn`）—— 没设过裁切的调用点（战斗/手牌那一族）一个字节都不动。
            //    🔴 2026-10-13（A435 辛 · A774）：判据是 `Clipping`（显式矩形**或**跟随节点），
            //       ⛔ 不是 `_clip.HasValue` —— 后者在「已开跟随、但这一刻没有节点」时漏掉后面挂上来的节点。
            if (Clipping) ApplyClip();
        }

        /// <summary>原版 `CardTextCountersController.DoColorChange(old, new)` 那两下：
        /// ① **每一次刷新先把四个数值的颜色刷回原色**（原版就是 `old == new` 那一支）；
        /// ② 涨/落了的**才**涂色 + bump。
        /// ⚠️ 只对**场上**那张卡做 —— 手牌那个 2D 脸的数值没拆层（见 `_stats` 的注释）。
        /// ⚠️ 颜色要等 `SetData` 末了那次 `SyncStatLayers` 才画上新值 —— 这里只记「该涂什么色」。</summary>
        void CaptureStatChanges(CardData d)
        {
            ClearStatFlashes();
            if (_faceMode != CardFace.Board || _stats[1] == null) return;
            // ⚠️ `CardData` 是 **struct**（没有 `== null` 这回事）——「还没数据」的判据是 `id` 为空串。
            if (!Data.isUnit || string.IsNullOrEmpty(Data.id)) return;
            if (!d.isUnit) return;
            for (int s = StatMelee; s <= StatHealth; s++)
            {
                int oldV = StatValue(Data, s), newV = StatValue(d, s);
                if (oldV != newV) FlashStat(s, newV > oldV);
            }
        }

        /// <summary>
        /// **这张卡是不是「临时卡（Ephemeral）」的角标**（规则书 `:183`/`:229`）。
        ///
        /// 规则书说临时卡「回合结束若在手牌则移除」—— **玩家得看得出哪张是临时的**，
        /// 否则牌忽然没了就是「静默失败」。原版的标记是 `BattleCardUI.ShowEphemeral()` +
        /// `ToggleGlitch()`（glitch 材质，⚠️ 素材本地没有），我们改用原版的**关键词图标**
        /// （`Atlas_trait_icon_ephemeral.png`）。取舍与出处见 `EphemeralIconAt` 上面的注释。
        ///
        /// ⚠️ 图标没导进来时这个方法**什么都不做**（不画错的），
        ///    自检里有一条断言盯着 `CardArt.Trait("ephemeral")` 非空。
        /// </summary>
        public void ShowEphemeral(bool on)
        {
            if (_ephemeral != null) _ephemeral.enabled = on;
        }

        /// <summary>现在画着临时角标吗（自检用）</summary>
        public bool EphemeralShown { get { return _ephemeral != null && _ephemeral.enabled; } }

        // ---- 🆕 2026-10-18（`A985④`）「由谁造出来的」（原版 `CreatedByText`）--------------

        /// <summary>
        /// **这张牌是谁造出来的** —— 传 `null` = 不是被造出来的（初始牌库 / 抽到的牌 / 督军…）。
        ///
        /// **判据**（反编译亲读，逐条）：
        ///   · 消费面 = `BattleCardUI.DisplayCreatedByText(CardStateOptions state)`
        ///     （`BattleCardUI__DisplayCreatedByText.c`，另一处同判据在 `BattleCardUI__SetObjectVisibility.c:100-133`）：
        ///     **先把节点关掉**，只有 `card.createdBy != null`（`card = BattleCardUI+0x248`，
        ///     `createdBy = card+0x268`）**且**状态 ∈ {`inHandShowing=8`, `inHandPlaying=10`} 时才点亮；
        ///     状态 10 那一支还有个 `card + 0x40`（= `isPlayer`）判据 —— **本地方**就 `return`（不点亮）。
        ///   · 文案 = `SupportMethods.GetCreatedByText(创建者卡名)`
        ///     （`SupportMethods__GetCreatedByText.c`：`GetTranslation("Battle/HUD/CreatedBy").Replace("{0}", 名字)`），
        ///     而「创建者卡名」= `RawCardScript.GetLocalizedCardName(creator.rawCard)`
        ///     ⇒ **吃的是创建者那张【卡】**，不是玩家名。
        ///   · 清掉的时机：`_DisplayOffensiveCardInCenter_d__264` 的 state 3 把那个节点
        ///     `SetActive(false)`（`…MoveNext.c:79-80`）—— 也就是**那一段大图演完就收**。
        ///
        /// ⇒ 本方法**只吃「创建者那张卡」**：卡名走 `CardText.Name`（= 原版那个
        ///   `GetLocalizedCardName`，与卡面别的字同一条按语言取名的路）。
        ///   ⚠️ 表现层只负责**内容与显隐**；什么时候调由战斗表现层定（`BattleDriver.SyncHand` 每轮推一次）。
        ///
        /// ⚠️ **两处如实标注**：
        ///   ① 原版那两档「状态」判据（8/10 + `isPlayer`）在我们这边**没有对应物** ——
        ///      我们的卡视图没有 `CardStateOptions` 状态机，所以**只要这张视图有来源就亮**。
        ///      这是**近似**（近似在一档：原版「自己打出去的那张」在 inHandPlaying 时是不亮的）。
        ///   ② 文案见 `CreatedByLine`（那个本地化键的值在远端，中文那半是我们写的）。
        /// </summary>
        public void SetCreatedBy(RuleEngine.CardDef creator)
        {
            string name = creator != null ? CardText.Name(creator.Name, creator.NameZh) : null;
            if (name == _createdByName) return;           // 同一张卡重复推（每帧都走的那条路）不动手
            _createdByName = name;
            // 场上形态没有这一层（原版那里整张 `2DCard` 关掉；`BuildTextLayers` 也走不到）
            if (_faceMode != CardFace.Board) FillCreatedBy();
        }

        /// <summary>现在这行字印的是什么（`null` = 没印 / 印着但这一档关着）—— 自检用。
        /// ⚠️ 判据取**真的 `activeSelf`**（不是「层建出来没有」）：`SetFace(Board)` 会把这一层关掉
        /// 而对象还在 —— 正是那种「建起来了、该藏的时候没藏」的缺陷要靠这条断言抓。</summary>
        public string CreatedByShown
        {
            get { return _createdBy != null && _createdBy.gameObject.activeSelf ? _createdBy.text : null; }
        }

        // ---- 🆕 棋盘单位卡的 buff/debuff 徽标（原版 `boardTraitIcons`）------------------

        /// <summary>角标数字的字号：字形高 ≈ 半个图标（图标 0.456 卡单位 ⇒ 0.16）。</summary>
        static float BadgeCounterFontSize
        {
            get { return TmpFont.FontSizeForGlyphHeight(Badges.IconSize * 0.35f); }
        }

        /// <summary>
        /// 画 / 更新 / 关掉那 7 个徽标位（原版 `BattleCardUI.UpdateTraitIcons` 的动作对偶：
        /// **先全部关掉，再按当前拥有的关键词逐个打开**）。
        /// 返回**真正画出来的个数**（自检拿它断言；`CardData.badges` 为空时是 0）。
        ///
        /// ⚠️ 取不到图（`CardArt.Trait` 返回 null）时**这一位空着**，不画错的 —— 红线。
        /// ⚠️ 层是**懒建**的：没有徽标的卡一个对象都不多建（手牌、战术卡都不建）。
        /// </summary>
        public int SetBadges(List<Badge> badges)
        {
            // 记一份当前这几位（「刚触发的是哪个词条」要按 `key` 找到**这一位**再脉冲，见 `PulseBadgeByKeyword`）
            _badges.Clear();
            if (badges != null)
                for (int i = 0; i < badges.Count && i < Badges.MaxSlots; i++) _badges.Add(badges[i]);

            int want = badges != null ? Mathf.Min(badges.Count, Badges.MaxSlots) : 0;
            for (int i = 0; i < Badges.MaxSlots; i++)
            {
                bool on = i < want;
                if (on && _badgeIcons.Count <= i) BuildBadgeSlot(i, badges[i]);
                if (_badgeIcons.Count <= i) continue;          // 建不出来（缺图/缺底板）⇒ 空着

                _badgePlates[i].enabled = on;
                _badgeIcons[i].enabled = on;
                if (!on)
                {
                    if (_badgeCounters[i] != null) _badgeCounters[i].text = "";
                    continue;
                }
                var b = badges[i];
                SetBadgeIconTexture(i, CardArt.Trait(b.sprite));
                // 🆕 2026-09-29 未激活态：**同一张图换材质**（每回合状态会变，所以每次刷新都要重设）
                SetBadgeGrey(_badgeIcons[i], b.active);
                if (_badgeCounters[i] != null)
                {
                    // 原版带角标的那一支叫 `With counter`，不带的那支叫 `Without counter`
                    // —— 两支是**并排的两套 renderer**，我们按 `b.counter` 决定显不显示。
                    _badgeCounters[i].text = b.counter > 0 ? b.counter.ToString() : "";
                    if (b.counter > 0) PlaceBadgeCounter(i);
                }
            }
            return Mathf.Min(want, _badgeIcons.Count);
        }

        /// <summary>现在画着几个徽标（自检用）。</summary>
        public int BadgesShown
        {
            get
            {
                int n = 0;
                foreach (var m in _badgeIcons) if (m != null && m.enabled) n++;
                return n;
            }
        }

        /// <summary>自检用：第 i 个位现在用的是哪张图标贴图。**截图看不出「画的是不是该画的那枚」**，只能这么断言。</summary>
        public Texture2D BadgeTexture(int i)
        {
            if (i < 0 || i >= _badgeIcons.Count || _badgeIcons[i] == null) return null;
            var m = _badgeIcons[i].sharedMaterial;
            return m != null ? m.mainTexture as Texture2D : null;
        }

        /// <summary>自检用：第 i 个位的角标文字（不含角标时是空串）。</summary>
        public string BadgeCounter(int i)
        {
            if (i < 0 || i >= _badgeCounters.Count || _badgeCounters[i] == null) return "";
            return _badgeCounters[i].text;
        }

        /// <summary>自检用：第 i 个位现在是不是**灰的**（未激活态）。
        /// 判据 = 材质上的 `_GreyScale`（1 = 灰），不是我们自己记的布尔 —— 截屏看不出「灰没灰」的归因。</summary>
        public bool BadgeGreyed(int i)
        {
            if (i < 0 || i >= _badgeIcons.Count || _badgeIcons[i] == null) return false;
            var m = _badgeIcons[i].sharedMaterial;
            return m != null && m.HasProperty("_GreyScale") && m.GetFloat("_GreyScale") > 0.5f;
        }

        /// <summary>卡单位坐标 → `SpriteQuad` 要的 0..1（左上原点）。</summary>
        static Vector2 ToAt01(Vector2 cardUnit)
        {
            return new Vector2(cardUnit.x / CardUnitW + 0.5f, 0.5f - cardUnit.y / CardUnitH);
        }

        /// <summary>**图标本体**的位置 —— 原版的图标是容器下的 `Container` 节点，比容器再往卡外偏
        /// `Badges.IconOutward`（0.287 × 0.750）。🔴 2026-09-20 补：以前图标直接画在**容器**位置上，
        /// 整体偏内 0.215 卡单位（≈卡宽的 10%）。角标文字跟着图标走，所以也用它。</summary>
        static Vector2 BadgeIconAt01(int i)
        {
            var at = Badges.SlotAt(i);
            return ToAt01(new Vector2(at.x + (at.x < 0f ? -Badges.IconOutward : Badges.IconOutward), at.y));
        }

        const float BadgePlateZ = -0.095f;    // 在数值层**之前**（见下面那条 z 的说明）
        const float BadgeIconZ = -0.10f;
        const float BadgeCounterZ = -0.105f;
        // 🔴 **2026-09-20：徽标/数值的 z 必须「在 3D 卡体的正面之前」**。
        //    卡体的网格是一块薄板：正面在 **card 空间 z ≈ −0.08**（mesh 正面 z≈+0.09 × 0.88586，
        //    再被 `Card 3D` 的 yaw 180° 翻到 −z）。原来这几个层在 −0.02…−0.055 ⇒ **整块被卡体挡住**
        //    （症状：场卡上数字/徽标**直接看不见**，而三色彩钮还在）。原版也是这个相对关系：
        //    那几个文本节点挂在 `3DBody` 的 z ≈ **−0.06…−0.08**（就是贴在卡面上）。
        //    ⚠️ 这几个层**只在场上出现**，所以直接改常量；手牌那个 2D 卡面不受影响。
        const float BoardInfoZ = -0.09f;      // 场上的数值层（`_info`）
        const float BoardArmourIconZ = -0.088f; // 场上的护甲盾（在数值层**之后**、卡体正面之前）
        const float InfoZ = -0.02f;           // 2D 卡面的数值层 z（原来写死在 `AddLayer` 那一行）
        const float ArmourIconZ = -0.015f;    // 2D 卡面的护甲盾 z
        /// <summary>徽标角标的字色 —— **原版 = 暖白 (1.0, 0.9729, 0.9104)**。
        /// 🔴 **2026-09-17 照原版改回**：原来是深褐 `(46,36,28)`，注释还写着「原版用什么颜色**本地查不到**」——
        /// **查得到，是当时没查到**。原版 `TraitCounter` 的 TMP 组件：
        /// `08_预制体特效/战斗预制体/GameObject/TraitCounter_1160489930360069056.json` →
        /// `MonoBehaviour_159564208624016320.json`：`m_fontColor = (1.0, 0.9729, 0.9104)`、
        /// `m_fontStyle = 1`（**Bold**）、`m_fontSize = 5.5` + autoSize(0.225~5.5)。</summary>
        static readonly Color32 BadgeCounterInk = new Color32(255, 248, 232, 255);

        /// <summary>建一个位：底板 + 图标 +（按需）角标文字。**底板或图标缺一张就不建**。</summary>
        void BuildBadgeSlot(int i, Badge b)
        {
            var plate = CardArt.Ui("Base3d_Trait_Background");
            var icon = CardArt.Trait(b.sprite);
            if (plate == null || icon == null)
            {
                Debug.LogWarning($"[CardView] 徽标位 {i} 缺图（底板={plate != null}, 图标 {b.sprite}={icon != null}）—— 这一位空着");
                return;
            }
            var at = Badges.SlotAt(i);
            // 底板 = 容器位置 + 往卡外 `PlateOutward`、再下移 `PlateDy`（原版 `IconBackground` 的局部值
            // × 容器 scale 0.750）。⚠️ 图标在**另一个**位置（`BadgeIconAt01`，见它的注释）。
            var plateAt = ToAt01(new Vector2(at.x + (at.x < 0f ? -Badges.PlateOutward : Badges.PlateOutward),
                                             at.y + Badges.PlateDy));

            _badgePlates.Add(AddLayer("badgePlate" + i, plate, BadgePlateZ, plate,
                                      FitQuad("badgePlate" + i, plateAt, Badges.PlateW, Badges.PlateH, plate)));
            // 图标层走**自建灰化 shader**（原版 `disabledMaterial` = `Sprite Greyscale`，见 `TraitDisabled.shader`）：
            // 徽标的「未激活态」**不是换图、也不是缩小**，而是**同一张图换材质**（`BoardTraitIcon.Initialize`）。
            // ⇒ 一个 shader 两态共存，激活不激活只差 `_GreyScale`（0 / 1），重建视图时不用换层。
            // ⚠️ **它和别的层不一样**：见 `AddBadgeIconLayer` 的注释（网格居中，好让脉冲绕自己缩放）。
            _badgeIcons.Add(AddBadgeIconLayer(i, icon, b.active));

            // 🔴 **2026-09-17 更正**：这里原来写「角标数字**用深色**……原版那个计数器用的什么颜色
            //    本地查不到 ⇒ **这一条是我们挑的**」—— **错的**：原版查得到（见 `BadgeCounterInk` 的注释），
            //    是**暖白 + Bold**，已经改回。压在浅色底板上读不出来那件事，原版就是这么印的。
            var t = TmpFont.NewText(transform, "badgeCounter" + i, "", BadgeCounterFontSize, BadgeCounterInk);
            // 🔴 **2026-10-18（`A848` 的补齐件 · `A994③` 同一轮）**：角标数字那层的纵向对齐 =
            //   原版 **`TraitCounter`** 的 **`Midline`(4096)** —— 现读
            //   `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/`：
            //   `GameObject/TraitCounter*.json` 的 `m_Component` 里那颗 TMP，**6 份实例逐份同值**：
            //   `m_VerticalAlignment = 4096` · `m_HorizontalAlignment = 2` · `m_fontSize = 5.5` ·
            //   `m_fontStyle = 1`(Bold) · `m_fontColor = (1, 0.9729, 0.9104)`（= 上面 `BadgeCounterInk` 那三格）
            //   （`MonoBehaviour_159564208624016320` / `_-2512500826360931392` / `_-1953590729870173248` /
            //    `_-3450971320567358528` / `_5561894497802492864` / `_7597200136860769216`）。
            //   ⚠️ **档位不在这里设** —— 它在**摆位那一步**设（见 `PlaceBadgeCounter`：次序是判据的一部分）。
            _badgeCounters.Add(t);
        }

        /// <summary>角标数字摆位 —— **次序本身就是判据**（⛔ 别把这两句合并/调换）。
        /// <para>① **先按出厂 `Middle` 摆**：`PlaceAt` 摆的是 `textBounds`（= 行盒 AABB，
        /// `TMP_Text.GetTextBounds` 取 `characterInfo[].ascender/descender`），**它随档位整体平移**。
        /// ② **再把档位设成原版那一档（`Geometry` = `Midline`）**：整块字平移 `c_G`
        /// （= 行盒心 ↔ 墨心 的距离）⇒ **墨心落在目标点** —— 这就是原版 `Midline` 的定义。</para>
        /// <para>🔴 **为什么每次刷新都要复位成 `Middle`**：`SetBadges` 每刷新一次都走这里
        /// （掉血/中 buff/每回合都会走 `SetData`），而上一次已经把 TMP 留在 `Geometry` 上了；
        /// 不复位的话第二次 `PlaceAt` 会带着新档位去摆 ⇒ 位置与第一次差一个 `c_G`（静默、≈1.25px）。
        /// 同一族的次序守卫在卡名那层（`BuildTextLayers` ①）。</para></summary>
        void PlaceBadgeCounter(int i)
        {
            var t = _badgeCounters[i];
            if (t == null) return;
            t.verticalAlignment = VerticalAlignmentOptions.Middle;          // ①
            PlaceAt(t, BadgeIconAt01(i), BadgeCounterZ);
            t.verticalAlignment = VerticalAlignmentOptions.Geometry;        // ② 原版 `TraitCounter` 的 4096
            t.ForceMeshUpdate();                                            // 批处理没有帧循环 ⇒ 手动重排一次
        }

        /// <summary>
        /// 徽标**图标**那一层。🔴 **它和 `AddLayer` 建出来的层不同**，两个理由：
        /// ① 网格**居中在原点**、GameObject 落在徽标中心 ⇒ `localScale` 绕**徽标自己**缩放。
        ///    脉冲高亮（原版 `BoardTraitIcon.HighlightIcon` 的 `DOScale`）要的就是这个；
        ///    用 `AddLayer` 的话 transform 在卡中心，缩放会**绕卡中心**，7 个位的图标互相错开。
        /// ② 材质走 `TraitDisabledMaterial()`（灰化 shader），不是 `Sprites/Default`。
        /// ⚠️ 尺寸仍按贴图自身比例内接进 0.456 的方框（和 `FitQuad` 同一条规矩，别把图拉变形），
        ///    所以网格名字里带实绘尺寸；换贴图时要 `SetBadgeIconTexture` 重设一次网格。
        /// </summary>
        MeshRenderer AddBadgeIconLayer(int i, Texture2D icon, bool active)
        {
            var at = BadgeIconAt01(i);
            var go = new GameObject("badgeIcon" + i);
            go.transform.SetParent(FaceRoot, false);      // 徽标也是卡面的一层 ⇒ 跟着倾摆
            go.transform.localPosition = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height, BadgeIconZ);
            go.AddComponent<MeshFilter>().sharedMesh = CenteredQuad("badgeIcon", Badges.IconSize, Badges.IconSize, icon);
            var mr = go.AddComponent<MeshRenderer>();
            var m = new Material(TraitDisabledMaterial());
            m.mainTexture = icon;
            mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _layers.Add(mr);
            SetBadgeGrey(mr, active);
            return mr;
        }

        /// <summary>换这一位的图标贴图：**材质 + 网格一起换**（网格是按贴图比例装进方框的，
        /// 只换 `mainTexture` 会让横图被竖框拉伸 —— 原来就漏了这一半）。</summary>
        void SetBadgeIconTexture(int i, Texture2D icon)
        {
            if (_badgeIcons[i] == null) return;
            var mf = _badgeIcons[i].GetComponent<MeshFilter>();
            if (mf != null)
                mf.sharedMesh = CenteredQuad("badgeIcon", Badges.IconSize, Badges.IconSize, icon);
            if (_badgeIcons[i].sharedMaterial != null) _badgeIcons[i].sharedMaterial.mainTexture = icon;
        }

        static readonly Dictionary<string, Mesh> _centeredQuads = new Dictionary<string, Mesh>();

        /// <summary>**以原点为中心**的 quad（`SpriteQuad` 是以 `at01` 为中心的那一款）。
        /// 尺寸按贴图自身比例内接进 (w × h) —— 和 `FitQuad` 同一条规矩。名字里带实绘尺寸，别只按 `key` 缓存。</summary>
        static Mesh CenteredQuad(string name, float w, float h, Texture2D tex)
        {
            float a = (tex != null && tex.height > 0) ? (float)tex.width / tex.height : w / h;
            float bw = w, bh = h;
            if (a < w / h) bw = h * a; else bh = w / a;
            string key = $"{name}_{bw:F3}x{bh:F3}";

            Mesh m;
            if (_centeredQuads.TryGetValue(key, out m) && m != null) return m;

            float s = Width / CardUnitW;
            float hw = bw * s * 0.5f, hh = bh * s * 0.5f;
            m = new Mesh { name = "CenteredQuad_" + key };
            m.vertices = new[]
            {
                new Vector3(-hw, -hh, 0f), new Vector3(hw, -hh, 0f),
                new Vector3(hw, hh, 0f), new Vector3(-hw, hh, 0f),
            };
            m.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f),
            };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _centeredQuads[key] = m;
            return m;
        }

        /// <summary>
        /// **脉冲高亮** —— 原版 `BoardTraitIcon.HighlightIcon()`：
        /// 先把上一次的补间杀掉、`localScale` **归位**，再
        /// `DOScale(原始尺寸 × highlightAnimScale(1.6), highlightAnimTime(0.5s))`
        /// `.SetLoops(2, Yoyo).SetEase(OutCubic)`（`BoardTraitIcon__HighlightIcon.c` 逐句照写）。
        /// 调用点 = 「这个词条刚触发了」（原版 `CardScript.ActivateTriggerTraitAnim` →
        /// `BattleCardUI.HighlightTraitIcon(traitId)`）。
        /// ⚠️ 归位那一步别省：连点两下时没有它，第二次是从「已经放大 1.6 倍」再乘，越点越大。
        /// </summary>
        public void PulseBadge(int i)
        {
            if (i < 0 || i >= _badgeIcons.Count || _badgeIcons[i] == null) return;
            var tr = _badgeIcons[i].transform;

            Tweener old;
            if (_badgePulses.TryGetValue(i, out old) && old != null && old.IsActive()) old.Kill();
            tr.localScale = Vector3.one;                 // 归位（原版 `set_localScale(originalIconSize)`）

            var tw = tr.DOScale(Vector3.one * Badges.HighlightAnimScale, Badges.HighlightAnimTime)
                       .SetLoops(Badges.HighlightAnimLoops, LoopType.Yoyo)
                       .SetEase(Ease.OutCubic)
                       .SetUpdate(CardTween.Mode);
            _badgePulses[i] = tw;
            PulseCount++;
        }

        /// <summary>自检用：徽标脉冲播过几次（截图看不出「那一下有没有播」）。</summary>
        public int PulseCount { get; private set; }

        readonly Dictionary<int, Tweener> _badgePulses = new Dictionary<int, Tweener>();

        /// <summary>按关键词脉冲（`BattleEvent.Keyword` 那种写法，例 `huntmark` / `Hunt Mark`）。
        /// 认不出这个关键词在这个单位身上 ⇒ **什么都不做**（不猜哪一位）。返回是否命中了某一位。</summary>
        public bool PulseBadgeByKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return false;
            string want = Badges.Norm(keyword);
            for (int i = 0; i < _badges.Count && i < _badgeIcons.Count; i++)
            {
                if (_badges[i].key == null) continue;
                if (Badges.Norm(_badges[i].key) != want) continue;
                PulseBadge(i);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 按**贴图自身比例**装进 (w × h) 的格子 —— 和卡框那条规矩一样（别把图拉变形）。
        /// ⚠️ 网格按「名字 + 实绘尺寸」缓存：`SpriteQuad` 只按名字缓存，同一位换了比例会拿到旧网格。
        /// </summary>
        static Mesh FitQuad(string name, Vector2 at01, float w, float h, Texture2D tex)
        {
            float a = (tex != null && tex.height > 0) ? (float)tex.width / tex.height : w / h;
            float bw = w, bh = h;
            if (a < w / h) bw = h * a; else bh = w / a;
            return SpriteQuad($"{name}_{bw:F3}x{bh:F3}", at01, bw, bh);
        }

        void Build(CardData d)
        {
            Data = d;

            // ---- 内层节点 `2DCard`：卡面所有层都挂在它底下，`AutoCardRotation` 也挂在它身上 ----
            // 原版两级（`CardUI Reference / 2DCard`，出处 `子代理读报_2dcard_0827.md` A0 全树）：
            //   外层由布局摆位/转扇形角（我们 = 本组件的根，写 `localRotation` 的只有 `SetPose` /
            //   `BattleDriver` 那两处），内层由倾摆写 `localRotation`。
            //   ⚠️ **不能只有一层**：倾摆写的是**绝对** `localRotation`，合层的话布局摆好的扇形角
            //   会被它一路 `Lerp` 回 identity（手牌那 12.75° 的倾角 2 秒内自己站直）——
            //   所以卡面层从这里开始往下挂。
            // 放在最前面：`AddLayer` 等所有建层的地方都读 `FaceRoot`，晚建一层就会挂错父节点。
            if (_tiltRoot == null)
            {
                var tiltGo = new GameObject("2DCard");
                tiltGo.transform.SetParent(transform, false);
                _tiltRoot = tiltGo.transform;
                _tilt = tiltGo.AddComponent<AutoCardRotation>();     // 数值 = 原版 prefab 的 10/10/10
                // 场上形态**不倾摆**：原版在场上把整个 `2DCard` 节点 `Toggle(false)`
                // （`Card2DController.Toggle`，`BattleCardUI.ChangeCardToMinion` 调）——
                // 那是 `AutoCardRotation` **所在的那个节点**，连组件一起不跑；
                // 场上的立体感来自 3D 卡体，2D 那套（含四个数值层）只是叠在上面的一层壳，
                // 它一歪就和卡体**对不上**。⇒ 等价地关掉组件（不是关节点：数值层还挂在它底下）。
                _tilt.enabled = _faceMode != CardFace.Board;
            }

            var mf = GetComponent<MeshFilter>();
            mf.sharedMesh = Quad();

            CardArt.Load();
            // 战术卡用**另一套框**（原版 troop / stratagem 分开）—— `isUnit=false` 就是战术卡
            var frameTex = CardArt.Frame(d.faction, d.rarity, !d.isUnit);

            if (frameTex != null)
            {
                // ---- 三层：立绘（后）→ 卡框（中）→ 数值（前）----
                // 根节点自带的那个 MeshRenderer 用不上 —— 不关掉它会拿默认材质渲出一块品红
                GetComponent<MeshRenderer>().enabled = false;
                var artTex = ArtTexture(d);
                var artMesh = ArtMeshInFrame(artTex, FrameQuad(frameTex));
                // ---- 立绘：原版是**画两次**的（2026-09-13 查实，用户看出来的「3DLit 立体感」）----
                //   ① 底层「完整插图」：**忽略贴图的 alpha**（那张图的 alpha 是角色抠图，不是不透明信息），
                //      垫在卡框下面 —— 卡框拱窗自己也是透明的（实测 alpha=0），窗里的背景就靠这一层。
                //   ② 前景层「角色抠图」：同一张贴图 + **真 alpha**，盖在**卡框上面** ⇒ 角色越出卡框。
                //   判据是**清单**（`card_cutouts.json`，张数看该文件的 `count` —— ⚠️ 2026-10-06 更正：原写 665 张，实际 **668 张**）：单位卡基本都有、战术卡基本都没有；
                //   没有立绘（回退占位图）的卡不在清单里 —— 给它加前景层会把卡框整个盖住。
                bool cut = d.artOverride != null
                           ? CardArt.AltArtHasCutout(d.artId)      // 异画走它自己那份清单（见 `CardData.artOverride`）
                           : CardArt.HasCutout(d.artId);           // ⚠️ 立绘按 **id** 取名，不是卡名（见 `CardData.artId`）
                // 🔴 **场上（`Board`）：整张 `2DCard` 都不画** —— 见 `CardFace` 的注释。
                //    原版在 inPlay 类状态调 `Card2DController.Toggle(false)`，插图/框/费用/宝石/
                //    卡名/阵营行/兵种行/效果底板/效果文字**一起没有**，只剩攻/血/甲 + 徽标。
                bool board = _faceMode == CardFace.Board;
                // ---- 最底层：软光/影（原版 `Card Highlight And Shadow`，4.4281² @ y −0.0126）----
                // 「落地感」全靠它；原版 shader + 原版 SDF 贴图，取不到就整层不画（见上面那一节注释）。
                var sdfMat = SdfMaterial();
                var sdfTex = CardArt.Sdf(d.faction, d.rarity, !d.isUnit);
                if (sdfMat != null && sdfTex != null)
                    _shadowLayer = AddLayer("shadow", sdfTex, ShadowZ, sdfTex, ShadowMesh(), mat: sdfMat);
                // ---- 场上：换**原版那张 3D 卡体**（`3DBody` → `Card 3D`）----
                // 原版在 inPlay 类状态把整张 `2DCard` 关掉、换成 3D 体（`BattleCardUI.ChangeCardToMinion`），
                // 立绘由 `SetCardImageTo3DBase` 灌进材质的 `_CardImage` —— 我们照做。
                // ⚠️ 取不到网格/材质时**退回 2D 立绘**（`BuildBody3D` 里打 warning，不静默）。
                if (board) _body3D = BuildBody3D(artTex);
                if (_body3D == null)
                    _art = AddLayer("art", frameTex, 0.03f, artTex, artMesh, opaque: true);
                // 🆕 2026-09-29：**场上卡也建那一层 2D 立绘**（默认关着，只有回手时才演）。
                //   原版 `Card Hand To Board` 那条 clip 是「**3D 体溶解出、2D 卡面淡回来**」的交接
                //   （`2DCard.m_Alpha` 与 `m_IsActive` 那两条曲线）—— 回手就是把它倒放。
                //   我们原来只在「建不出 3D 体」时才建 `_art`，于是 `SetArtAlpha` 在场上卡上
                //   **永远是空操作**（`ArtVisible` 恒 false），倒放那一半就演不出来。
                //   ⚠️ 平时一直关着（`ApplyFace` 在 `CardFace.Board` 下会把它关掉），**不会多画一层**。
                else if (_art == null)
                {
                    _art = AddLayer("art", frameTex, 0.03f, artTex, artMesh, opaque: true);
                    // ⚠️ **建完当场关掉** —— `AddLayer` 建出来的 GameObject 默认是**开着**的，
                    //    而 `ApplyFace` 只在这张卡**再 `SetData` 一次**时才会把它关掉
                    //    （新建的那条路不经过 `SetData`）⇒ 不关的话场上会「3D 体 + 2D 立绘」同时露出来
                    //    （**既有断言**「场上 3D 体与 2D 立绘不同时出现」当场抓到）。
                    Show(_art, false);
                }
                if (!board && cut && UseFrontLayer && !DebugNoArtFront)
                    _artFront = AddLayer("artFront", frameTex, -0.008f, artTex, artMesh);
                if (!board) _frame = AddLayer("frame", frameTex, 0f, null);
                // 🆕 文字底板（原版 `Front/Textbackgrounds`）—— 让卡名/效果文字在黑底上读得清。
                //    原版开哪一块由 `CardTextsController` 的 state 表决定：
                //    **有描述 → `TextBackground Big UI`；没有描述 → `TextBackground Small UI`**（state1 / state2）。
                // ⚠️ 取不到图就**整块不画**（不静默失败：文字照旧、只是少了黑底）——
                //    和 `CardArt` 的其它层一个路子，删掉 `Resources/Art/` 游戏照样跑。
                var textBgTex = CardArt.DeckUi("Card_Text_smooth_background");
                if (board)
                {
                    // 场上不画底线（原版整张 `2DCard` 关掉）—— 什么都不做，**不是**静默失败
                }
                else if (textBgTex != null)
                {
                    bool hasDesc = !string.IsNullOrEmpty(d.keywords);
                    _textBg = AddLayer("textBg", textBgTex, TextBgZ, textBgTex,
                                       hasDesc ? SpriteQuad("textBg", TextBgAt, TextBgW, TextBgH)
                                               : SpriteQuad("textBgSmall", TextBgSmallAt,
                                                            TextBgSmallW, TextBgSmallH));
                    _textBg.sharedMaterial.color = new Color(0f, 0f, 0f, TextBgAlpha);
                }
                else
                {
                    // 不许静默失败：少一层黑底要说出来（文字仍有描边，不至于读不出来）
                    Debug.LogWarning("[CardView] 文字底板图 `Card_Text_smooth_background` 取不到 —— " +
                                     "卡面少一层黑底。重建：python Unity/工具/sync_battle_ui_art.py");
                }
                if (!board && cut && !UseFrontLayer && artTex != null)   // ⚠️ 这条路没调通，见 `UseFrontLayer` 的注释
                {
                    // **破框走「卡框挖洞」那条路**（默认）：立绘照常画一次，框在角色处被挖开
                    var fm = new Material(FrameCutoutMaterial())
                    {
                        mainTexture = frameTex,
                    };
                    fm.SetTexture("_CutMask", artTex);
                    fm.SetFloat("_CutAmount", 1f);
                    _frame.sharedMaterial = fm;
                }
                // UV 裁到卡外框；**uv2 = 立绘的 UV**（破框 shader 要用同一套坐标去采立绘的 alpha）
                if (_frame != null) _frame.GetComponent<MeshFilter>().sharedMesh = FrameMesh(frameTex, artTex);
                // 费用底板 / 护甲盾：原版的**两张真图**（A2 表），垫在数值底下 ——
                // z 比 `_info`(−0.02) 远、比卡框(0) 近，所以数字盖在它们上面，和原版层级一致。
                var costTex = CardArt.DeckUi("Card_Frame_Cost_Icon");
                if (!board && costTex != null)
                    _costBg = AddLayer("costBg", costTex, -0.015f, costTex,
                                       SpriteQuad("costBg", CostAt, CostBgW, CostBgH));
                // ⚠️ 只有**单位卡且有护甲**才画那面盾：战术卡的卡面上没有数值格
                //    （对照 `D:/2/Warpforge部队卡片/…/4计策/*.png` —— 战术卡只有费用和稀有度）
                var armourTex = d.isUnit && d.armor > 0 ? CardArt.DeckUi("pedestal_icon_armor") : null;
                if (armourTex != null)
                    _armourIcon = AddLayer("armourIcon", armourTex, ArmourIconZ, armourTex,
                                           SpriteQuad("armourIcon", ArmourIconAt, ArmourIconW, ArmourIconH));
                // ⚠️ 数值层：场上传 `statsOnly` —— **费用/卡名/效果文字都不烘进去**
                //    （原版场上那些层整棵关掉；只有攻/血/甲留着）
                // 🆕 2026-09-29：**场上那四个数字改由四个独立图层画**（`BuildStatLayers`）——
                //    原版「受击时那一个数值变色 + bump」是按 counter 做的，烘在一张图里动不了单个。
                //    ⇒ 这一张 `_info` 在场上就**只剩底**（`skipStats`），拆不出来时（字形表不在）
                //    照旧走老路把数字烘进来。
                _info = AddLayer("info", frameTex, InfoZ, InfoTexture(d, board, SplitStats(d)));
                if (SplitStats(d)) { BuildStatLayers(); SyncStatLayers(); }
                // 稀有度宝石：叠在卡框那颗**暗色凹槽**上（原版 `Rarity` 节点）。
                // 卡框分档改的是框的形制，**光靠它看不出稀有度** —— 颜色在这颗宝石上。
                // 图取不到就不画（不静默失败：卡框和数值照常）。
                var gemTex = RarityGem(d.rarity);
                if (!board && gemTex != null) _gem = AddLayer("gem", gemTex, -0.01f, gemTex, GemMesh());

                // ---- 🆕 「临时卡」角标（默认关掉，由 `ShowEphemeral` 打开）----
                // z 取 −0.03：**在数值层(−0.02)前面**（更靠近相机）—— 它在左上角，
                // 和数值层其实不重叠，但万一以后调位置，压在上面更安全。
                var ephTex = CardArt.Trait("ephemeral");
                if (ephTex != null)
                {
                    _ephemeral = AddLayer("ephemeral", ephTex, -0.03f, ephTex,
                                          SpriteQuad("ephemeral", EphemeralIconAt,
                                                     EphemeralIconW, EphemeralIconH));
                    _ephemeral.enabled = false;
                }
                // ⚠️ 取不到图标就**整块不画**（不是画个错的）：`CardArt.Trait` 找不到时返回 null。
                //    这时卡照常出，只是没有临时角标 —— 自检里有一条断言盯着「图标导进来了没有」。
            }
            else
            {
                // ---- 没有原版卡框：退回单层占位卡面 ----
                // ⚠️ 这条路**以前不画立绘**（原版插图白导进来了）—— 没导卡框的阵营
                //    在卡组编辑器里就是一片空白。现在补上：立绘画在**插图位**，
                //    占位卡面那块是透明的（见 `FaceTexture` ③），立绘正好从那里露出来。
                // 🔴 **2026-10-03 就地更正（铁律 5 · A87①）**：本行原来写「**9 个**没导卡框的阵营」——
                //    这个数**早就不是了**：`Resources/Art/cards/` 里 15 个阵营 × (troop + stratagem)
                //    × tier1–4 = **150 张全在**（逐张核过，每个阵营两套框都不缺），而 `ToCardData` 那条主路
                //    喂的阵营恰好也只有 **15 个**（卡池 13 个 —— `cards_engine.json` 的 1126 张逐张核过
                //    `faction`；+ `StarterCards` 自设计的 Ember / Tide）⇒ **主路的卡今天一张都走不到这个 `else`**。
                //    它现在的活读者是**不经过 `ToCardData` 的那几处 `new CardData`**：
                //    ① 选牌面板的「选项卡」（`BattleDriver.cs` 里选牌面板选项卡那处 `faction = source != null ? source.Faction : null`，没有源卡时 `faction` 就是 `null`）
                //    ② `CardData.Simple(…)`（自检探针）③ `Editor/FontProbe.cs`
                //    ④ **删掉 `Resources/Art/` 那一档**（`CardArt.cs:8-10` 明写是支持的模式）—— 那时**每张卡**都走这里。
                //    ⛔ 别把它当「已经用不上的老路」删掉：上面 `SetData` 里换 `_face` 材质那一段
                //    （连同它保留 `renderQueue` 的修）就只服务这条支路。
                _face = GetComponent<MeshRenderer>();
                _face.sharedMaterial = FaceMaterial(d);
                _layers.Add(_face);
                var wellArt = ArtTexture(d);
                _art = AddLayer("art", null, 0.03f, wellArt, ArtMeshInWell(wellArt, WellW, WellH, WellY));
            }

            // 状态环：比卡面大一圈、贴在后面（z 大一点 = 更远），默认关掉。
            // 🔴 **2026-09-29 换成【原版那一层】**（原来是我们自造的羽化图 + `Shader.Find("Sprites/Default")`）：
            //   原版它是卡体上 `MinionLight` 那个 **SpriteRenderer**，sprite = **`Card board frame SDF`**
            //   （79×107 @(25,11) · PPU 100 · pivot .5/.5），材质 = **`Card board Frame SDF`**，
            //   shader = **`Everguild/FX/Card Highlight And Shadow`**，`SortingOrder = −1`。
            //   🔴 **那个 shader 没有 `_Color`**（属性表 21 个逐条核过；`SpriteRenderer.color` 走的是 URP 的
            //   `_RendererColor`，而我们是 **MeshRenderer**、根本没有那条通道）⇒ **状态色只能写 `_Outline`**
            //   （它就是这个 shader 里那圈的颜色；原版材质 `Card board Frame SDF` 的 `_Outline = (1,1,1,0.447)`）。
            var rimGo = new GameObject("rim");
            rimGo.transform.SetParent(FaceRoot, false);   // 环圈的是「看得见的那张卡」⇒ 跟着倾摆
            PlaceRim(rimGo.transform);            // 🆕 2026-09-29：位置与尺寸照原版（见 `PlaceRim`）
            rimGo.AddComponent<MeshFilter>().sharedMesh = Quad();
            _rim = rimGo.AddComponent<MeshRenderer>();
            _rim.sharedMaterial = RimMaterial();
            _rim.enabled = false;

            // 卡名/关键词走 TMP（拿不到字体资产时它自己会跳过，字仍旧烘在 _info 里）
            // ⚠️ **场上不挂**（原版整张 `2DCard` 关掉，卡名/阵营行/兵种行/效果文字一起没有）
            if (_faceMode != CardFace.Board) BuildTextLayers(d);

            // 🆕 棋盘徽标（`CardData.badges` 为空时这一句什么都不建 —— 手牌/战术卡都是空的）
            SetBadges(d.badges);

            // 🆕 2026-09-29 四个数值层：**在这里兜一次**。
            //    ⚠️ 上面 `if (frameTex != null)` 那条分支里虽然也调了，但**卡框贴图取不到时整块会跳过**
            //    （真实卡都有框，可探针卡/占位数据没有）—— 而数值层**不依赖卡框**，
            //    只靠那条路就会出现「这张卡没有数字」这种**静默缺件**（自检就是这么抓到的：
            //    `face=Board · isUnit=True · Pragati=True` 却是 `[-1,-1,-1,-1]`）。
            BuildStatLayers();
            SyncStatLayers();
        }

        /// <summary>
        /// 🆕 2026-10-18（`A985④`）**「由谁造出来的」**（原版卡根下那个 `CreatedByText` 节点）。
        ///
        /// **判据全是解包字段（不是截图）** —— 出处逐条：
        ///   · **它是卡根的【第一个子件】**：`bundle_menus_assets_all/GameObject/CreatedByText.json`
        ///     + 父 `RectTransform_-4981929636261473480.json` 的 `m_Children` 首项
        ///     （顺序 = `[CreatedByText, 2DCard, Card Ready for level up, New Card Badge, Ban Icon]`）。
        ///   · **位姿**（`RectTransform_4803967432083755832.json`）：`m_AnchoredPosition = (0, 1.65)`、
        ///     `m_SizeDelta = (2.5, 0.38)`、锚点与轴心全 `(0.5, 0.5)`；父（卡根）尺寸 **2.5437×3.3686**。
        ///     运行期实况 dump 逐位吻合：`runtime_ui_dump_Battle_Arena_1.tsv` 那一行 `0.0,1.6` / `2.5,0.4`。
        ///     ⇒ 折到**卡单位**：中心 `(0, +1.65)`、框 `2.5×0.38`（**比卡宽 2.0927 还宽** ⇒ 本来就允许溢出卡外）。
        ///   · **文字**（`MonoBehaviour_4833287377952691000.json`）：`m_fontColor` = **纯白**；
        ///     `m_fontSize = 2.45` 且 **开着自动缩放** `[0.3, 3.0]`（`m_overflowMode = 0` = Overflow）
        ///     ⇒ 真字号是**运行时缩出来的**；`m_HorizontalAlignment = 2`(Center) · `512`(Middle) · 不折行。
        ///     材质 = `Asar-Regular White w outline`（`Material_6389513473928706012.json`：
        ///     `_OutlineWidth = 0.2` · `_FaceDilate = 0.2` · 黑描边 · **无 underlay**）。
        ///   · 出厂 `m_IsActive = false` —— 原版只在 `BattleCardUI.DisplayCreatedByText` 里点亮
        ///     （且只在 `card.createdBy != null` + 手牌那两档状态时），见 `SetCreatedBy` 的注释。
        ///
        /// ⚠️ **我们这一处是「挑的」，不是原版**（如实标着，待真 Play 核）：
        ///   ① **压谁**：`m_Children` 首项 ⇒ 按 uGUI 次序它画在 `2DCard` **底下**；可它是
        ///      **居中压在卡顶沿上**的（中心 1.65 vs 卡顶 1.6657）—— 画在底下就只看得见露在卡外的那
        ///      一小条。我们按**可见**放在**卡面之前**（我们这层是 TMP、不受 `2DCard` 的倾摆与
        ///      渲染顺序约束），并且**不照搬** `m_fontSizeMin = 0.3` 那个下限（照搬的话长卡名会
        ///      横向溢出到 2 倍卡宽）—— 这两条等真 Play 看到原版那行字多大再定。
        ///   ② 文案：原版是个**本地化键** `Battle/HUD/CreatedBy`（值在**远端 I2 表**，本地没有），
        ///      见 `CreatedByLine`。
        /// </summary>
        static readonly Vector2 CreatedByAt01 = ToAt01(new Vector2(0f, 1.65f));
        const float CreatedByBoxW = 2.5f;                    // 卡单位（= 原版 `m_SizeDelta.x`）
        const float CreatedByBoxH = 0.38f;                   // 卡单位（= 原版 `m_SizeDelta.y`）
        /// <summary>基准字号 = 原版 `m_fontSize` 2.45 卡单位 ⇒ 折成「em 对卡高比」，再按框回缩
        /// （我们的 `FitToBox` 与它那条 autosize 同语义：先给上限、量着缩）。</summary>
        const float CreatedByEmOfCard = 2.45f / CardUnitH;
        /// <summary>字色 —— **原版是纯白**（组件 `m_fontColor = (1,1,1,1)`、材质 `_FaceColor` 也是纯白）。</summary>
        static readonly Color32 CreatedByInk = new Color32(255, 255, 255, 255);

        /// <summary>那行字在 `Core/Loc.cs` 里的键 —— 原版 `SupportMethods.GetCreatedByText`
        /// 用的就是它（`d:/2/tools/decomp_full/SupportMethods__GetCreatedByText.c` 亲读）。
        /// 📌 自检要核「表里有这条键」时用它：`Loc.HasEntry(CardView.CreatedByTerm)`。
        /// ⛔ 别在别处再写一遍这个字面量（同族先例 = `BattleDriver.HandCountTerm` 那种 `…Term` 常量）。</summary>
        public const string CreatedByTerm = "Battle/HUD/CreatedBy";

        /// <summary>「表里没有这条键」那句 `Debug.LogWarning` **只出声一次** —— `CreatedByLine` 由
        /// `FillCreatedBy` **每张卡每次刷新**调（手牌 / 场上 / 展示窗都走它），逐次出声会把日志刷爆
        /// （同 `Core/Loc.cs` 的 `_warnedMissing` 那条理由）。</summary>
        static bool _warnedCreatedByKey;

        /// <summary>那行字的**文案**。🔴 原版 = `SupportMethods.GetCreatedByText(创建者卡名)` =
        /// `GetTranslation("Battle/HUD/CreatedBy").Replace("{0}", 名字)`（`SupportMethods__GetCreatedByText.c` 亲读；
        /// 键字面量在 `d:/2/tools/il2cpp_out/stringliteral.json`）。
        /// ⚠️ **那个键的值在远端 I2 表里**（84 个本地 bundle 里没有 `localization_assets_all`）⇒
        /// 英文那半还有旁证（原版预制体里那个 TMP 的占位串是 `Created by someone fancy`
        /// ⇒ 模板形态 = `Created by {0}`），**中文那半是我们自己写的**。
        /// ✅ **2026-10-08（第六会话 · `Core双语-第一步`）已接上** —— 键 `Battle/HUD/CreatedBy` 就在
        ///    `Core/Loc.cs:813`（值 = `由 {0} 创建` / `Created by {0}`，与原来那两句**逐字相同**
        ///    ⇒ 这次接线**不改变界面上的字**，只把「按 `Loc.Current` 二选一拼」换成走语言表）。
        ///    ⛔ 别留两套口径（原来那句「等它收了这条键就改走 `Loc.T`」已兑现，就地删掉）。</summary>
        static string CreatedByLine(string creatorName)
        {
            if (string.IsNullOrEmpty(creatorName)) return null;
            // 🔴 **先判 `Loc.HasEntry`、再取词条** —— 这一跳是 `A985④` 点名要的：
            //    少了它，`Loc.cs` 那条一旦被删/改名，`Loc.T` 会把**键名本身**（`Battle/HUD/CreatedBy`）
            //    印到卡面上，而**卡面看不出这是缺陷**（它就是一行白字）⇒ 正是本工程点名的静默失败。
            if (Loc.HasEntry(CreatedByTerm))
                return Loc.T(CreatedByTerm).Replace("{0}", creatorName);   // `{0}` = 创建者的卡名
            // 键不在表里 ⇒ **退回改之前那两句硬拼**（中文档 / 英文档都保持原样）+ **出声**（⛔ 不静默）
            if (!_warnedCreatedByKey)
            {
                _warnedCreatedByKey = true;
                Debug.LogWarning($"[CardView] `Loc` 表里**没有** `{CreatedByTerm}` ⇒ 卡面那行「谁造成的」"
                               + "退回**写死的中文/英文**（⛔ 绝不让它去印键名）。补词条 → `Core/Loc.cs`；"
                               + "同一条**只出声这一次**（这张卡每次刷新都会走这里）。");
            }
            return Loc.Current == AvailableLanguages.Chinese
                 ? $"由 {creatorName} 创建"
                 : $"Created by {creatorName}";
        }

        /// <summary>那行字自己的描边 —— 原版**在材质里**（`Asar-Regular White w outline`:
        /// `_OutlineWidth 0.2` / `_FaceDilate 0.2` / 黑 / 无 underlay）。
        /// ⚠️ `TmpFont.TextOutline` 只有 `Name`(0.15+大阴影) 与 `Body`(0.05) 两档，都不是 0.2/0.2，
        ///    而那个枚举在 `Core/TmpFont.cs`（**不在本笔白名单**）⇒ 这里按原版数值就地设一次；
        ///    将来若给它加一档 `CreatedBy`，这一段应当整体搬过去（⛔ 别两处各留一份）。</summary>
        static void ApplyCreatedByOutline(TextMeshPro t)
        {
            if (t == null) return;
            var mat = t.fontMaterial;                    // ⚠️ 实例材质，别碰共享的
            if (mat == null) return;
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.2f);
            t.UpdateMeshPadding();                       // 描边会画到 quad 之外 ⇒ 重算留白（同 `TmpFont.ApplyOutline`）
        }

        /// <summary>刷那一层。**只在非场上形态调**（原版场上整张 `2DCard` 关掉，自然也没有它）。
        /// 没来源时 `Fill` 收到空串 ⇒ 把对象拆掉、返回 null（= 原版 `SetActive(false)`）。</summary>
        void FillCreatedBy()
        {
            if (!TmpFont.Available) return;
            float k = Width / CardUnitW;                  // 卡单位 → 世界（当前 = 1）
            _createdBy = Fill(_createdBy, "createdBy", CreatedByLine(_createdByName),
                              CreatedByAt01, CreatedByBoxW * k, CreatedByFontSize,
                              CreatedByInk, false, CreatedByBoxH * k);
            ApplyCreatedByOutline(_createdBy);
        }

        /// <summary>那行字的基准字号（em = 2.45 卡单位 ⇒ 折成我们这套字号）</summary>
        static float CreatedByFontSize
        {
            get { return TmpFont.FontSizeForGlyphHeight(Height * CreatedByEmOfCard); }
        }

        /// <summary>
        /// 卡名 / 关键词挂 TMP。**和原版同构** —— 原版卡预制体上就是 `NameTextUnit` /
        /// `DescTextUnit` 这些 TMP 组件，运行时由 `CardTextsController.SetTexts()` 灌字，
        /// 不是烘在卡面贴图里的。走这条路中文才画得出来。
        /// 拿不到字体资产（`TmpFont.Available == false`）就**不挂**，
        /// 由 `InfoTexture` 里那两条 `if` 用点阵字库顶上（只画得出 ASCII）。
        /// </summary>
        void BuildTextLayers(CardData d)
        {
            if (!TmpFont.Available) return;

            // 单位卡 / 战术卡**两套模板**：位置和版面框都不一样（见上面那两组常量，A3 表）
            bool unit = d.isUnit;
            float k = Width / CardUnitW;                  // 卡单位 → 世界（当前 = 1）
            var nameAt = unit ? UnitNameAt : TacticNameAt;
            var descAt = unit ? UnitDescAt : TacticDescAt;
            var armyAt = unit ? UnitArmyAt : TacticArmyAt;
            float nameW = (unit ? NameBoxW : TacticNameBoxW) * k;
            float nameH = (unit ? NameBoxH : TacticNameBoxH) * k;
            float descW = (unit ? DescBoxW : TacticDescBoxW) * k;
            float descH = (unit ? DescBoxH : TacticDescBoxH) * k;

            // ① 卡名：字号来自原版实测（见 `TitleFontSize`），太长/太高再回缩进版面
            // 🔴 **2026-10-18（`A994③` 那一轮补的次序守卫）**：**`Fill` 之前必须把档位复位成出厂
            //   `Middle`** —— `Fill` 末句是 `PlaceAt`，而 `PlaceAt` 摆的是 `textBounds`
            //   （= 行盒 AABB，`TMP_Text.GetTextBounds` 取的是 `characterInfo[].ascender/descender`），
            //   **那个盒子随档位整体平移**。所以：
            //     · 首次建：`_title == null` ⇒ TMP 出厂是 `Middle` ⇒ 「先摆后设档」；
            //     · **再看一次**（`SetData` → `BuildTextLayers`，战场每刷新一次都走）：
            //       若这一句不复位，`Fill` 会**带着上一轮的 `Geometry`** 去 `PlaceAt`
            //       ⇒ 摆出来的位置与首次建**差整整一个 `c_G`**（= 行盒心 ↔ 墨心 的距离；
            //       卡名上实测 **0.0116 卡单位 ≈ 1.25 px**）—— 静默、只在刷新后现形。
            //   ⇒ 复位 → `Fill`（按 `Middle` 摆）⇒ 再设真档位（整块字平移 `c_G`，**墨心落在目标点**）。
            //   ⚠️ 本句**不是**为了「清掉之前的效果」而写的漂亮话：删掉它上面那条断言就会红。
            if (_title != null) _title.verticalAlignment = VerticalAlignmentOptions.Middle;
            _title = Fill(_title, "title", d.title, nameAt, nameW, TitleFontSize, InkName, false, nameH);
            // 🔴 **2026-10-18（`A848` 那笔账的落点）**：**卡名那层的纵向对齐 = 原版的 `Midline`(4096)**。
            //   判据：原版卡预制体里那 6 个互斥变体（`NameTextUnit` / `…Unit Big` / `…Unit No description` /
            //   `…Unit No Description Big` / `NameTextTactic` / `NameTextTacticBig`）**全是 `m_VerticalAlignment
            //   = 4096`**（`资料/普查产出_1018/R4_卡面与手牌现核.md`；**`A994③` 这一轮我又自己逐颗读了一遍**
            //   `d:/2/新解包资源/assets_full/bundle_staticgeneralassets_assets_all/` —— 同值）；
            //   而 `TmpFont.NewText` 出厂是 `Middle`(512) ⇒ 我们**一直差这一档**。
            //   ⚠️ **另三层（`keywords`/`army`/`race`）原版本来就是 `Middle`** ⇒ ⛔ 只有这一层要改。
            //   🔑 **枚举名对不上、数值对得上**：我们这版 TMP 的 `VerticalAlignmentOptions` **没有 `Midline` 这个名字**
            //   （成员是 `Top/Middle/Bottom/Baseline/`**`Geometry=0x1000`**`/Capline` —— 见
            //   `Library/PackageCache/com.unity.ugui@…/Runtime/TMP/TMP_Text.cs:82-85`），
            //   而 `TextAlignmentOptions.Midline = Center | VerticalAlignmentOptions.Geometry`（同文件 `:55`）
            //   ⇒ **`Midline` 的竖半就是 `Geometry` = `0x1000` = 4096**，与原版序列化值**逐位相同**。
            //   **实测读数**（`CardFaceProbe` 的 `vAlign`/`inkCenterY` 两列，2026-10-18）：改之前 `vAlign=512`、
            //   `inkCenterY=-0.0116`（`Heavy Intercessor`，卡单位）；改之后 `vAlign=4096`、`inkCenterY=0.0000`
            //   ⇒ **墨心正好落在原版那个框心**（这就是 `Midline` 的定义）。
            if (_title != null)
            {
                _title.verticalAlignment = VerticalAlignmentOptions.Geometry;
                // 批处理没有帧循环 ⇒ 上面那次改档位要**手动重排一次**才落到网格上
                // （与 `PlaceAt` 里那句 `ForceMeshUpdate` 同一个理由；⛔ 别删）。
                _title.ForceMeshUpdate();
            }

            // ② 效果文字：名字下面，超宽折行；行数多了按**原版那块版面框**的高度回缩。
            //    ⚠️ **上沿对齐**（不是居中）：我们的块会随行数长高，居中的话行数一多就往上长、
            //       撞到阵营行（2026-09-12 实测战术卡 3 行描述把 `ULTRAMARINES` 盖住了）。
            //    顶边取版面框顶边；战术卡那边阵营行（原版 `ArmyTextTactc` y01 0.6935）本来就压在
            //    框顶(0.6691)下面，所以再让一行的高度，保证「阵营行在效果文字上面」。
            //    **下沿**贴着版面框底边（原版那块框的下沿就在最底下那颗稀有度宝石上方一点点）
            //    🔴 2026-09-15：**卡面上有兵种行/类型行时，效果文字的下沿要再往上让一点** ——
            //       原来是 `unit ? 0.8466 : 0.9061` 一刀切，而「战术/防御卡印不印那一行」是
            //       2026-09-15 才按 PnP 补上的（见 `SubtypeLine`）⇒ 补完之后那些卡的效果文字
            //       **整块压在类型行上**（`Dark Pact of Excess` / `Alien Idol` 实测）。
            //       判据：印那一行 ⇒ 底边用 `RaceDescBottom01`（见它的注释，是量出来的）。
            bool hasRace = !string.IsNullOrEmpty(SubtypeLine(d));
            float descBot01 = hasRace ? RaceDescBottom01 : (unit ? UnitDescBottom01 : TacticDescBottom01);
            //    可用高度 = 底边 − **阵营行下沿**：原版那块框的顶边本来就压在阵营行下面
            //    （0.6691 vs 阵营行 0.6935），照框高给足的话三行描述会长到框顶、把阵营行盖住。
            float avail01 = descBot01 - (armyAt.y + ArmyEmOfCard * 0.5f + 0.008f);
            descH = Mathf.Min(descH, Mathf.Max(0.2f, avail01 * Height));
            var descBotAt = new Vector2(descAt.x, descBot01);

            // ②·图标：`d.keywords` 里的 `[Attack]` 这类**记号**已经在 `BattleDriver.FaceTextFull`
            //    里换成了 TMP 行内 sprite（**只换那一处**，见那里的注释）。
            //    `FontScaleFor` 现在**恒返回 1** —— 试过按标高放大字号，反而把字号缩死
            //    （行更宽 ⇒ 折行更多 ⇒ 整块更高 ⇒ `FitToBox` 往死里缩），原因写在那个函数的注释里。
            float kwScale = CardIcons.FontScaleFor(d.keywords);
            _keywords = Fill(_keywords, "keywords", d.keywords, descBotAt, descW, KeywordFontSize * kwScale,
                             InkDesc, true, descH, true, DescLineSpacing, unit ? 0f : DescParaSpacing);

            // ③ 🔴 **阵营行（原版 `ArmyTextUnit` / `ArmyTextTactc`）：按展示场景分档** ——
            //    **手牌 + 场上不印，其余都印**（**用户 2026-09-22 裁定**；三档定义见 `CardFace` 的注释）。
            //  🔴 **2026-10-17 由用户指认实拍复核过（我逐像素看过）**：`点击卡片查看详情的参考.png` 里
            //    **卡名（白）下面那行【橙色】的 `萨姆-罕` 就是阵营行、它印着**；`战斗截图参考.png` 里
            //    **手牌与场上都没有那一行** ⇒ **裁定成立、本条即它的落地**。
            //    📌 同日我曾误改成「哪都不印」（拿**本地 PC 版 bundle** 的读数去反推原版）—— **当天改回**；
            //       **别再按下面那份「本地读数」翻案**：它说的是**我们手上这份件**，不是用户那台客户端。
            //  ✅ **本地读数（留着，不必重查 —— 它证明的是「本地这份件哪都不印」）**：`CardTextsController` 的 9 个 state
            //    （`08_预制体特效/战斗预制体/MonoBehaviour/MonoBehaviour_-8207081529520448576.json`）
            //    里，`ArmyTextUnit`(PathID −8059568047133123648) 在 **state0–3 被列进 `objectsToDeactivate`、
            //    state4–8 两个列表都不在**；`ArmyTextTactc`(−3202616875737179200) 在 **state4–7 被停用、
            //    state0–3/8 不在列表** —— **没有任何一个 state 把它放进 `objectsToActivate`**；
            //    而 `ToggleState`（`CardTextsController.CardNameDescriptionToggleState__ToggleState.c:13-35`）
            //    是「**先把 activate 列表全开、再把 deactivate 列表全关**」。
            //  🔴 **2026-10-17 补齐**（这里原来写「**那个字段没人查过**」—— **已查**）：
            //    `ArmyTextUnit` / `ArmyTextTactc` / `Army No Desciption` 的 GameObject **`m_IsActive` 全是 `false`**；
            //    而且更强：**28 个 bundle、170 个 `CardTextsController` 实例**里，那三个 GO 被**激活 0 次**
            //    （被停用 136 次；**对照** `RaceText` 被激活 **153 次** ⇒ 扫描器**测得出**激活）；
            //    全库 GameObject `m_IsActive` **153/153 全 false**；整个 `assets_full` 里**只有
            //    `CardTextsController` 引用它们** ⇒ **没有第二条激活路径**。
            //  ⚠️ **手牌档也照样建这一层、只是建完立刻关掉**（下一句 `Show`）。为什么不是「不建」：
            //    ① 层的集合只由 `FaceShowsArmy` 一个判据决定 ⇒ `SetFace` 换档时不会撞上「层根本没建过」
            //       的空档（`CLAUDE.md` 铁律 10 第 5 条：多个出生/迁移入口，每个都要显式设置）；
            //    ② 效果文字那块版面**本来就给阵营行留了位置**（上面 `avail01` 用 `armyAt.y` 算的）
            //       ⇒ 关掉它只是空出那一行，**别的层一个都不动**（手牌版面与这一行启用之前逐字相同）。
            // 对照组：`RaceText`（兵种行）在 **state0/2 是被 `objectsToActivate` 激活的** ⇒ 兵种行照印（见 ④）。
            _army = Fill(_army, "army", CardText.Faction(d.faction), armyAt, nameW, ArmyFontSize, InkArmy, false);
            Show(_army, FaceShowsArmy(_faceMode));

            // ④ 兵种行 —— 在卡面下部。**印不印不是「只有单位卡」那么简单**，判据见 `SubtypeLine`
            //    🆕 2026-10-17（D5）：这一格的字现在是**按当前语言**取的
            //    （`SubtypeLine` → `RaceTerm` → `Loc.T("Card_Race/<race>")`）。
            //    ⚠️ **版面一个字都没动** —— 位置 / 字号 / 颜色 / 折行照旧（实拍与 prefab 都已收口）。
            _race = Fill(_race, "race", SubtypeLine(d), UnitRaceAt, nameW, RaceFontSize, InkArmy, false);

            // ⑤ 🆕 描边 —— **原版在材质里，不在 TMP 组件上**，见 `TmpFont.ApplyOutline` 的注释。
            //    卡名 0.15 + 一团软黑影；效果/阵营/兵种 0.05 细描边。
            //    这是「文字压在亮色立绘上读不出来」那条的正面修法（原版就是这么保证可读性的）。
            TmpFont.ApplyOutline(_title, TmpFont.TextOutline.Name);
            TmpFont.ApplyOutline(_keywords, TmpFont.TextOutline.Body);
            TmpFont.ApplyOutline(_army, TmpFont.TextOutline.Body);
            TmpFont.ApplyOutline(_race, TmpFont.TextOutline.Body);

            // ⑥ 🆕 2026-10-18（`A985④`）「由谁造出来的」（原版 `CreatedByText`；判据见上面那组常量）。
            //    ⚠️ **原地刷、不重建**：`BuildTextLayers` 每次 `SetData` 都走（战场每刷新一次都走）
            //    ⇒ 内容从 `_createdByName` 现取（那个字段由 `SetCreatedBy` 维护），没来源时这一层被拆掉。
            FillCreatedBy();
        }

        /// <summary>卡面最下面那行橙字要印什么；**不印返回 null**。
        ///
        /// 🔴 2026-09-15 改。原来这里是 `unit ? d.subtype : null`，注释写着
        /// 「战术卡成品卡图上没有这一行」—— **那句话是错的**。
        /// 规则是**逐张开图**从 70 张 PnP 成品卡（13 阵营全跨）读出来的（出处：
        /// `资料/卡牌基座_进度与交接.md` §四·五）：
        ///
        ///   · `unit`    → **必印**，印 `subtype` 原值（Infantry / Vehicle / Monster / Beast /…）
        ///   · `hero`    → **必印 `Warlord`**（恒定的，不看 subtype）
        ///   · `defence` → **必印 `Defence`**（也是恒定的 —— 实测有 3 张防御卡的 subtype 数据
        ///                 写的是 Stratagem/Spell/Structure，卡面上**照样印 Defence**）
        ///   · `tactic`  → **只有 subtype 落在下面那 9 个「具名系列」里才印**，其余一律不印。
        ///                 所以「战术卡印不印」**不能只看空/非空** —— 非空的一大堆（Spell/Tactic/
        ///                 Ability/Event/Support/Order/Trick/Talent/… 约 370 张）卡面上都没有这行。
        ///
        /// ⚠️ **排除了的假设**（别再试）：①「只单位/督军/防御印」被 9 类计策的反例推翻；
        /// ②「不泛用的词就印」被 Structure/Order/Trick/Talent/Mission/Relic 推翻（这些词也不泛用，照样不印）；
        /// ③「按卡框模板」被同阵营同框反例推翻（UM 天赋里 `Primarch of the XIII` 印 Codicil，
        /// 而 `Master of Arms` / `Indomitus Crusade` 不印）。</summary>
        static string SubtypeLine(CardData d)
        {
            // ⚠️ 没填 `type` 的构造点（自检里的 `CardData.Simple` / `Placeholder` 之类）走**老路径**，
            //    免得它们突然少一行。填了 type 的都走新规则。
            // 🔴 每个出口都过一遍 `RaceTerm`（= 按当前语言取显示名）—— **印不印**由这里判，**印哪个词**由它给。
            if (string.IsNullOrEmpty(d.type)) return d.isUnit ? RaceTerm(d.subtype) : null;
            switch (d.type)
            {
                case "unit":    return RaceTerm(d.subtype);
                case "hero":    return RaceTerm("Warlord");
                case "defence": return RaceTerm("Defence");
                case "tactic":  return TacticSubtypeShown.Contains(d.subtype) ? RaceTerm(d.subtype) : null;
                default:        return null;
            }
        }

        /// <summary>🆕 2026-10-17（D5）：兵种行的**词条键前缀**。
        /// <para>🔴 **这是原版自己拼的，不是我们自拟的**：`GameStaticData.CardRaceToString` /
        /// `MinionRaceToString`（`d:/2/tools/decomp_full/` 两个方法体实读）都是
        /// `GetTranslation(String.Concat(&lt;前缀&gt;, &lt;种族串&gt;))`；两条 `.rdata` 字面量已按 RVA 读出
        /// （`0x1842cdf40` = **`"Card_Race/"`** · `0x1842ce040` = **`"Card_Race/Warlord"`**，
        /// 与 `d:/2/tools/all_strings.txt` 的表偏移逐位对上）。⛔ 别改成 `Card/Subtype/…`。</para></summary>
        public const string RaceTermPrefix = "Card_Race/";

        /// <summary>兵种行的**显示名**（`Card_Race/&lt;race&gt;` → 当前语言）。
        /// <para>取不到就**照原样返回那个英文串**（= 加 D5 之前的行为）并**出声** —— ⛔ 不静默。
        /// ⚠️ 同一个值**只出声一次**：卡面每次刷新都会走这条路，逐次出声会把日志刷爆。
        /// ⚠️ 卡池里 43 个非空 `subtype` **全在表里** ⇒ 这条兜底正常**不会触发**；
        /// 一旦日志里出现它，就是数据侧冒出了新 `subtype`。</para></summary>
        static string RaceTerm(string race)
        {
            if (string.IsNullOrEmpty(race)) return race;
            string key = RaceTermPrefix + race;
            if (Loc.HasEntry(key)) return Loc.T(key);
            if (UnknownRace.Add(race))
                Debug.LogWarning($"[CardView] 兵种行 `{race}` 在语言表里查不到词条（键 `{key}`）⇒ **卡面照原样印英文**。"
                               + "加词条见 `Core/Loc.cs`（键名照原版 `Card_Race/<race>`）。");
            return race;
        }
        static readonly HashSet<string> UnknownRace = new HashSet<string>();

        /// <summary>会印在卡面底部那一行的 tactic subtype（**白名单**，实测推出来的）。
        /// ⚠️ 加新卡系时看它卡面上有没有这一行：**有就加进来，没有就别加** ——
        /// 这不是「引擎支持的 subtype 列表」，是「卡面印这一行的 subtype 列表」。</summary>
        static readonly HashSet<string> TacticSubtypeShown = new HashSet<string>
        {
            "Dark Pact", "Overlord Power", "Combat Elixir", "Codicil", "Psychic Power",
            "Rune", "Invocation", "Genomic Enhancement", "Sabotage",
            // 🆕 2026-09-15 补：`Secret`（DA 的 5 张 `6秘密` 卡）—— 卡面底部橙字印 `Secret`。
            //    ⚠️ 它**不在**当初那 70 张抽样里（那批是 `.png`，而 DA `6秘密/` 全是**手机翻拍 `.jpg`**），
            //    是清「认不出的关键词」那一轮逐张开图时补上的。
            "Secret",
        };

        /// <summary>阵营行字号（原版 `ArmyTextUnit` fs15 × scale0.01 = 0.15 卡单位）</summary>
        public static float ArmyFontSize
        {
            get { return TmpFont.FontSizeForGlyphHeight(Height * ArmyEmOfCard); }
        }

        /// <summary>兵种行字号（原版 `RaceText` fs18 × scale0.01 = 0.18 卡单位）</summary>
        public static float RaceFontSize
        {
            get { return TmpFont.FontSizeForGlyphHeight(Height * RaceEmOfCard); }
        }

        /// <summary>
        /// 建或**原地更新**一层 TMP 文字，返回它（文字为空时返回 null 并把旧的清掉）。
        /// 原地更新是必须的 —— 这条路径每次战场刷新都会走，见 `SetData` 的注释。
        /// </summary>
        TextMeshPro Fill(TextMeshPro t, string name, string text, Vector2 at, float maxWidth,
                         float fontSize, Color32 baseColor, bool wrap, float maxHeight = 0f,
                         bool bottomAlign = false, float lineSpacing = 0f, float paragraphSpacing = 0f)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (t != null)
                {
                    t.text = "";                     // 先清字再销毁：Play 模式下 Destroy 要等帧末，
                    TmpFont.Kill(t.gameObject);      // 不清的话那一帧还留着上一张卡的名字
                }
                return null;
            }

            if (t == null)
            {
                t = TmpFont.NewText(transform, name, "", fontSize, baseColor);
                // 图标 sprite asset 由 `TmpFont.NewText` 统一挂上（全工程只那一处设）。
                // 原地更新的那条路上它已经在 TMP 里了，不用重设。
            }

            t.text = text;
            t.fontSize = fontSize;                   // `FitToWidth` 会改字号，每次得先重置回基准
            t.color = (Color)baseColor * _tint;      // 高亮/置灰的状态色要跟着走，不然刷新一次就白了
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            // 行距/段距**必须在 `FitToBox` 之前设** —— 它按文本高度回缩字号，间距会改变高度
            t.lineSpacing = lineSpacing;
            t.paragraphSpacing = paragraphSpacing;

            if (wrap) TmpFont.SetWrapWidth(t, maxWidth);
            else FitToWidth(t, maxWidth);
            if (maxHeight > 0f) FitToBox(t, maxWidth, maxHeight, wrap);

            if (bottomAlign) PlaceBottomAt(t, at, TextZ);
            else PlaceAt(t, at, TextZ);
            return t;
        }

        /// <summary>
        /// 字号整体回缩到**装得进原版那块版面框**（宽 × 高）。
        /// 原版的 `DescTextUnit` 开着 `m_enableAutoSizing`（`m_fontSizeMin/Max` = 1 / 24），
        /// 长文本会自动缩到刚好装下 —— 这就是本函数要做的事。
        ///
        /// 🔴 **2026-09-15 改成二分查找。** 原来是「量一次 → 按 `maxHeight / b.y` 等比缩 → 再来一轮」，
        ///    三轮收敛。**那个做法会严重缩过头**：折行文字的高度对字号**不是线性的** ——
        ///    字小了每行能排更多字、行数还变少，高度掉得比等比快。
        ///    **实测**（`Heavy Intercessor`，框 1.5274×0.5466）：缩完 `bounds.y` 只剩 **0.27**，
        ///    连框的一半都没用上，字号停在 0.951（名义 2.485 的 38%）——
        ///    这就是「我们的效果文字比原版小一大截」的直接原因（`资料/PnP卡图_逐张对账_0915.md` A4）。
        ///    改成在 `[nominal×0.05, nominal]` 上二分「仍然装得下的最大字号」，与 TMP 自带
        ///    auto-size 的语义一致，8 次量测就收敛。
        /// </summary>
        static void FitToBox(TextMeshPro t, float maxWidth, float maxHeight, bool wrap)
        {
            if (t == null) return;
            float nominal = t.fontSize;
            if (FitsBox(t, maxWidth, maxHeight, wrap)) return;      // 装得下就别缩

            float lo = nominal * 0.05f, hi = nominal;               // lo = 已知能装下（极小），hi = 装不下
            for (int i = 0; i < 8; i++)
            {
                float mid = (lo + hi) * 0.5f;
                t.fontSize = mid;
                if (FitsBox(t, maxWidth, maxHeight, wrap)) lo = mid; else hi = mid;
            }
            t.fontSize = lo;
            FitsBox(t, maxWidth, maxHeight, wrap);                  // 把字号定下来的那次量测留下
        }

        /// <summary>量一次：当前字号装得进框吗？（顺带把折行宽度和网格更新做掉）</summary>
        static bool FitsBox(TextMeshPro t, float maxWidth, float maxHeight, bool wrap)
        {
            if (wrap) TmpFont.SetWrapWidth(t, maxWidth);
            t.ForceMeshUpdate();
            var b = t.textBounds.size;
            const float eps = 1.002f;                               // 一点点余量，别卡在边界上来回跳
            return b.x <= maxWidth * eps && (maxHeight <= 0f || b.y <= maxHeight * eps);
        }

        // 字号（= 一个 em 有多高）对卡高的比值。**出处是原版，不是我们挑的**：
        //   原版卡预制体 `08_预制体特效/战斗预制体/` 上的 `NameTextUnit`：
        //     `m_fontSize = 19`、`m_LocalScale = (0.01, 0.01, 0.01)`  → em = 19 × 0.01 = 0.19 世界单位
        //     卡面高 `2DCard.m_SizeDelta.y = 3.3313`（自它往上到根 `CardPrefab` 的 scale 全是 1）
        //   → 0.19 ÷ 3.3313 = **5.70%**
        //   （场景那份 `battlearena1` 里链上多一层 `CardUI` scale 150，对分子分母同时生效、约掉。）
        // ⚠️ 这是**字号（em）**对卡高，不是文字框对卡高 —— 文字框是 29.7308 × 0.01 = 0.2973，
        //    比值 8.92%，那是留给卡名的**版面空间**，不是字实际多大。
        // ⚠️ 原版这句开着 `m_enableAutoSizing`（`m_fontSizeMax` = 19）—— 名字**长**的时候会自动缩，
        //    所以 5.70% 是**上限**。我们这边没做自动缩放，只有 `FitToWidth` 的固定回缩。
        const float TitleEmOfCard = 0.0570f;

        /// <summary>卡名的字号。汉字约占 1 em，所以「em 高度」直接喂给 `FontSizeForGlyphHeight`</summary>
        public static float TitleFontSize
        {
            get { return TmpFont.FontSizeForGlyphHeight(Height * TitleEmOfCard); }
        }

        /// <summary>
        /// 效果文字的字号。**2026-09-12 改：之前这条是我们挑的，现在按原版来。**
        /// 原版 `DescTextUnit`：`m_fontSize = 23.55`、`m_LocalScale = 0.01` → em = 0.2355 卡单位
        /// → 0.2355 ÷ 3.3313 = **7.07%** 卡高（旧值 3.75% 只有它一半，字明显偏小）。
        /// ⚠️ 原版同时开着 `m_enableAutoSizing`（`m_fontSizeMax = 24`），长描述运行时会被缩下来 ——
        ///    所以 7.07% 是**上限**；我们靠 `FitToBox` 按 `DescBoxH`（原版那块版面框高）收缩，同构。
        /// 出处：`子代理读报_2dcard_0827.md` A3 表 `DescTextUnit`。
        /// </summary>
        public static float KeywordFontSize
        {
            get { return TmpFont.FontSizeForGlyphHeight(Height * DescEmOfCard); }
        }

        /// <summary>0.2355 / 3.3313 —— 效果文字的 em 对卡高比</summary>
        const float DescEmOfCard = 0.0707f;

        /// <summary>
        /// 把整块文字**居中**摆到卡面归一化坐标 (x 从左、y 从**顶部**) 上。
        /// 用 `textBounds` 反推而不是「把原点放上去」：TMP 的原点在**第一行的基线**上，
        /// 折成两行之后整块会往下长、盖住底下的宝石（而且行数还不固定）。
        /// </summary>
        static void PlaceAt(TextMeshPro t, Vector2 at, float z)
        {
            if (t == null) return;
            t.ForceMeshUpdate();                 // 批处理没有帧循环，尺寸得手动推出来
            var center = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height, z);
            t.rectTransform.localPosition = center - (Vector3)t.textBounds.center;
        }

        /// <summary>
        /// 把整块文字**下沿对齐**摆到 (x01, y01) 那个点上（y 从顶算）。
        /// 效果文字用这个而不是 `PlaceAt`（居中）：原版 TMP 是「固定框 + 对齐」，
        /// 我们的块会随行数长高 —— **下沿对齐**才能保证「行数多少都不会往上长、撞到阵营行/卡名」，
        /// 同时最后一行永远贴着版面框底边（原版那块框的下沿就在最底下那颗宝石上方）。
        /// </summary>
        static void PlaceBottomAt(TextMeshPro t, Vector2 at, float z)
        {
            if (t == null) return;
            t.ForceMeshUpdate();
            float botY = (0.5f - at.y) * Height;
            var center = new Vector3((at.x - 0.5f) * Width, botY + t.textBounds.size.y * 0.5f, z);
            t.rectTransform.localPosition = center - (Vector3)t.textBounds.center;
        }

        /// <summary>太宽就把字号整体回缩到装得下（原来点阵那条路也是「缩号到放得下」）</summary>
        static void FitToWidth(TextMeshPro t, float maxWidth)
        {
            if (t == null) return;
            t.ForceMeshUpdate();
            float w = t.textBounds.size.x;
            if (w <= maxWidth || w <= 0f) return;
            t.fontSize *= maxWidth / w;
            t.ForceMeshUpdate();
        }

        /// <param name="opaque">true = 用 `CardPresentation/ArtOpaque` 画（**只取 RGB、忽略贴图 alpha**）。
        /// 只有立绘底层要它 —— 立绘的 alpha 是角色抠图，直接按它混合的话底层就没法铺满拱窗。</param>
        MeshRenderer AddLayer(string name, Texture2D basis, float z, Texture2D tex, Mesh mesh = null,
                              bool opaque = false, Material mat = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(FaceRoot, false);      // 卡面层挂在 `2DCard` 内层（倾摆跟着它走）
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh != null ? mesh : Quad();
            var mr = go.AddComponent<MeshRenderer>();
            // ⚠️ 每层一份**新材质**：直接用共享材质再改 mainTexture 会让所有卡共用同一张贴图（踩过）
            var m = mat != null ? new Material(mat)
                                : new Material(opaque ? OpaqueMaterial() : BaseMaterial());
            m.mainTexture = tex != null ? tex : basis;
            mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _layers.Add(mr);
            return mr;
        }

        /// <summary>`CardPresentation/ArtOpaque`：只取 RGB、把 alpha 当 1 —— 立绘底层用它（见 `AddLayer`）。</summary>
        static Material OpaqueMaterial()
        {
            if (_artOpaqueMat == null)
            {
                var sh = Shader.Find("CardPresentation/ArtOpaque");
                if (sh == null)
                {
                    Debug.LogError("[CardView] 找不到 `CardPresentation/ArtOpaque` shader —— "
                                 + "立绘底层会退回 Sprites/Default（角色抠图会把拱窗打成透的）");
                    return BaseMaterial();
                }
                _artOpaqueMat = new Material(sh);
            }
            return _artOpaqueMat;
        }
        static Material _artOpaqueMat;

        /// <summary>诊断开关：关掉前景层（角色抠图），单独看底层——用来分辨「杂色是底层带来的还是双层引起的」</summary>
        public static bool DebugNoArtFront;

        /// <summary>诊断 / 自检开关：**让 <see cref="BuildBody3D"/> 直接失败** —— 即造出
        /// 「网格或 shader 取不到」那条**退回 2D 立绘**的路径。
        ///
        /// 🔴 **为什么非要留这么一个口**：批处理自检里 `CardArt.Card3DMesh()` 与
        ///    `CardPresentation/Card3D` shader **永远都在** ⇒ 不借它**根本造不出**「场上没有 3D 卡体」
        ///    那一态，而 `SetRemnantBody` 的 **2D 退回支**（`else if (_faceMode == CardFace.Board
        ///    &amp;&amp; _art != null) Show(_art, true);`）**只有那一态才走到** ——
        ///    `:759` 的注释自陈「八条自检跑的都是 3D 那套 ⇒ 一次都没走到」（2026-10-15 复查：确实零断言）。
        ///
        /// ⚠️ 它是 **`static`** ⇒ **用完必须关回去**（`CardView.DebugNoBody3D = false;`），
        ///    否则那之后新建的**每一张场上卡**都会退回 2D 立绘。默认 **false** ⇒ 产品零影响。
        /// （同族的诊断开关见 `DebugNoArtFront`；`BuildBody3D` 那条 warning 会把「是谁开的」写进日志，
        ///   不静默。）</summary>
        public static bool DebugNoBody3D;

        // ==================================================================
        //  最底层：软光/影（原版 `Card Highlight And Shadow`）—— 2026-09-19 接上
        // ==================================================================
        //
        // 原版卡面 `Front` 的**最底下**有这一层，**4.4281×4.4281**、中心 y = **−0.0126**（比卡本体
        // 2.09×3.33 大得多 —— 露在卡外的那圈就是「落地感」的来源）。出处 `资料/战斗UI_原版对账表.md:187`。
        // 🔴 **它的美术是原版预生成的 SDF 距离场**（`card_sdf/<阵营>_tier<N>`，见 `CardArt.Sdf`），
        //    shader 用**原版的** `Everguild/FX/Card Highlight And Shadow` —— 那个 shader **就在我们随包的
        //    `wf_shaders.bundle` 里**（2026-09-19 实读），和 E 组那 8 个内置 shader 同一条路：直接取原件。
        // ⚠️ **这一层的颜色是它自己说了算**（`_ShadowColor` / `_Outline`），**不参与 `ApplyTint` 的整卡着色** ——
        //    见 `ApplyTint` 里那段「同理」注释（`_textBg` 踩过同一个坑）。
        const float ShadowSize = 4.4281f;      // 原版节点尺寸（**不是**卡本体尺寸）
        const float ShadowY    = -0.0126f;     // 原版节点的 y
        const float ShadowZ    = 0.06f;        // 比立绘（0.03）还靠后 ⇒ 在所有层之下
        static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.604f);   // 原版材质 `_ShadowColor` 默认值

        static Material _sdfMat;
        static Mesh _shadowMesh;
        MeshRenderer _shadowLayer;

        /// <summary>软光/影那层的材质 = **原版 shader**（拿不到就返回 null，那一层整个不画、并说明原因）</summary>
        static Material SdfMaterial()
        {
            if (_sdfMat != null) return _sdfMat;
            const string Name = "Everguild/FX/Card Highlight And Shadow";
            if (!WarpforgeVFX.WarpforgeShaderMap.TryResolve(Name, out var sh, out var src) || sh == null)
            {
                Debug.LogWarning($"[CardView] 解析不到原版 shader `{Name}` ⇒ 卡面那层软光/影不建"
                               + "（随包 shader bundle 在不在？见 `资料/特效还原_进度与交接.md` §三）");
                return null;
            }
            _sdfMat = new Material(sh) { name = "CardSdf" };
            // 🔴 **必须把原版材质上的值抄进来，不能用 shader 默认值**（2026-09-19 实测踩到）：
            //    shader 默认的 `_Outline` 是**不透明的白** ⇒ 每张卡会多出一圈**白框**
            //    （而原版材质把它的 alpha 设成了 **0** = 平时不描边）。
            //    值出处 = `bundle_duplicateassetisolation_assets_all/Material/Material_-3316280387615011577.json`
            //    （`m_Name = "Card Frame SDF"`，2026-09-19 实读；逐值与 `m_Floats`/`m_Colors` 对上）。
            void C(string p, Color v) { if (_sdfMat.HasProperty(p)) _sdfMat.SetColor(p, v); }
            void F(string p, float v) { if (_sdfMat.HasProperty(p)) _sdfMat.SetFloat(p, v); }
            C("_Outline",        new Color(0.3585f, 0.0592f, 0.3268f, 0f));   // alpha 0 = 平时不描边
            C("_ShadowColor",    ShadowColor);
            C("_Offset_Outline", Color.clear);
            C("_Offset_Shadow",  Color.clear);
            C("_Scale",          new Color(1f, 1f, 0f, 0f));
            C("_NoiseColor",     new Color(1f, 0.6726f, 0f, 1f));
            F("_Outer_Edge",             0.465f);
            F("_Outer_Fallof",           1.053f);
            F("_Inner_Edge",             0.333f);
            F("_Inner_Fallof",           0.224f);
            F("_SpriteAlphaEdge_Outer",  0.484f);
            F("_SpriteAlphaEdge_Inner",  0.043f);
            F("_NoiseIntensity",         1.15f);
            F("_NoiseMaskScale",         1f);
            _sdfMat.EnableKeyword("_ALPHACHANNEL_R");     // 原版 `m_ValidKeywords`
            _sdfMat.EnableKeyword("_NOISE_CHANNEL_B");
            _sdfMat.renderQueue = 3000;                   // 原版 `m_CustomRenderQueue`
            Debug.Log($"[CardView] 卡面软光/影：`{Name}` ← {src}（材质值照原版 `Card Frame SDF`）");
            return _sdfMat;
        }

        // ==================================================================
        //  **卡背**那层 SDF（原版材质 `Card Backs SDF`）—— 🆕 2026-09-26
        // ==================================================================
        // 和卡面那层**同一个 shader**（`Everguild/FX/Card Highlight And Shadow`，UnityPy 读
        // `Carbdback Shadow SDF` 的 Image `m_Material` = pathID `6996605638394099752`
        // → `bundle_duplicateassetisolation_assets_all/Material/` → shader name 实读），
        // **只是属性值与关键字不同** ⇒ 两条路各一份材质，**别共用**（共用就是「一个值当全部情况」）。
        //
        // 逐值出处 = `Material_6996605638394099752.json`（`m_Name = "Card Backs SDF"`，2026-09-26 实读）。
        // 🔴 **必须把原版材质上的值抄进来，不能用 shader 默认值** —— 和卡面那条同一个坑：
        //    shader 默认的 `_Outline` 是**不透明的白** ⇒ 每张卡背会多出一圈**白框**。
        //    （原版这份把 `_Outline` 的 alpha 设成 **0** = 平时不描边。）
        // ⚠️ 材质上还躺着一大批 Standard / TMP 的**残留属性**（`_FaceDilate` / `_GradientScale` /
        //    `_ScaleRatioA` / `_Metallic` / `_Parallax` …）—— 那是**死值**，**不要抄**
        //    （`CLAUDE.md` §三那条：接了死值会把自建 shader 坑掉；用原版 shader 时它们本来也不被读）。
        static Material _cbSdfMat;

        /// <summary>**卡背** SDF 那层的基材质（**调用方要 `new Material(...)` 克隆一份**再设 `_MainTex`
        /// —— 共享同一份会让所有格共用最后一张掩码，`AddLayer` 那条注释记的就是这个坑）。
        /// 拿不到 shader 就返回 null ⇒ 那一层整层不画、并说明原因。</summary>
        public static Material CardbackSdfMaterialBase()
        {
            if (_cbSdfMat != null) return _cbSdfMat;
            const string Name = "Everguild/FX/Card Highlight And Shadow";
            if (!WarpforgeVFX.WarpforgeShaderMap.TryResolve(Name, out var sh, out var src) || sh == null)
            {
                Debug.LogWarning($"[CardView] 解析不到原版 shader `{Name}` ⇒ **卡背那层 SDF 不建**"
                               + "（随包 shader bundle 在不在？见 `资料/特效还原_进度与交接.md` §三）");
                return null;
            }
            var m = new Material(sh) { name = "CardbackSdf" };
            void C(string p, Color v) { if (m.HasProperty(p)) m.SetColor(p, v); }
            void F(string p, float v) { if (m.HasProperty(p)) m.SetFloat(p, v); }
            // 逐值 = 原版 `Card Backs SDF`
            C("_Outline",     new Color(0.09901961f, 0.59727448f, 0.83962184f, 0f));  // alpha 0 = 平时不描边
            C("_ShadowColor", new Color(0f, 0f, 0f, 0.53333336f));
            C("_Offset_Outline", Color.clear);
            C("_Offset_Shadow",  Color.clear);
            C("_Scale",       new Color(1f, 1f, 0f, 0f));
            C("_NoiseColor",  new Color(1f, 0.67255884f, 0f, 1f));
            C("_SDF_Offset",  new Color(0.0003f, -0.001f, 0f, 0f));
            C("_InnerGlowColor", new Color(0.22981f, 1.04509f, 0.04925f, 1f));
            C("_Noise_Speed_Scale_StepLow_StepHigh", new Color(0.0025f, 7f, 0.06f, 0.16f));
            C("_Speed_And_Scale", new Color(0.3f, 5.02f, 0f, 0f));
            F("_Outer_Edge",            0.369f);
            F("_Outer_Fallof",          2.0f);
            F("_Inner_Edge",            0.629f);
            F("_Inner_Fallof",          0.706f);
            F("_SpriteAlphaEdge",       0.397f);
            F("_SpriteAlphaEdge_Inner", 0.143f);
            F("_SpriteAlphaEdge_Outer", 0.446f);
            F("_NoiseIntensity",        0.2f);
            F("_NoiseMaskScale",        1f);
            F("_NOISE_CHANNEL",         2f);
            F("_ALPHACHANNEL",          0f);
            F("_TextureWidth",          512f);
            F("_TextureHeight",         512f);
            F("_TexelModifier",         128f);
            F("_TexelModifierX",        2048f);
            F("_TexelModifierY",        1024f);
            F("_Cull",                  2f);      // 原版 `_Cull` = 2（背面剔除），与卡面那层不同
            F("_ZTest",                 4f);
            F("_SrcBlend",              5f);
            F("_DstBlend",              10f);
            F("_ZWrite",                0f);
            F("_Surface",               1f);
            F("_Blend",                 0f);
            F("_CastShadows",           0f);
            F("_ReceiveShadows",        1f);
            var noise = CardArt.CardbackSdfNoise();
            if (noise != null && m.HasProperty("_Noise")) m.SetTexture("_Noise", noise);
            else Debug.LogWarning("[CardView] 卡背 SDF：`Noise Combined` 取不到 ⇒ 少一层颗粒"
                                + "（跑 `工具/import_original_card_sdf.py` 补）");
            m.EnableKeyword("_ALPHACHANNEL_R");     // 原版 `m_ValidKeywords`
            m.EnableKeyword("_NOISE_CHANNEL_B");
            m.renderQueue = 3000;                   // 原版 `m_CustomRenderQueue`
            Debug.Log($"[CardView] 卡背 SDF：`{Name}` ← {src}（材质值照原版 `Card Backs SDF`）");
            _cbSdfMat = m;
            return _cbSdfMat;
        }

        /// <summary>软光/影那层的网格：**4.4281²**、中心 y −0.0126（原版节点尺寸，逐值照抄）</summary>
        static Mesh ShadowMesh()
        {
            if (_shadowMesh != null) return _shadowMesh;
            float w = ShadowSize * 0.5f;
            var m = new Mesh { name = "CardShadowQuad" };
            m.vertices = new[]
            {
                new Vector3(-w, -w + ShadowY, 0f), new Vector3(w, -w + ShadowY, 0f),
                new Vector3(w,  w + ShadowY, 0f),  new Vector3(-w, w + ShadowY, 0f),
            };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _shadowMesh = m;
            return m;
        }

        // ==================================================================
        //  场上那张 **3D 卡体**（原版 `3DBody` → `Card 3D`）—— 2026-09-19
        // ==================================================================
        //
        // 原版场上的卡**不是一块平面立绘**，是一张「薄板 + 滚圆底边」的厚 3D 卡，靠 **matcap 假光照**
        // 出立体感、材质 Unlit（`资料/3DBody_原版场上卡体规格.md` §一）。
        // 资源早就在工程里（`WarpforgeVFX/Meshes|Textures`），缺的只是接线 —— 这一节就是那一段接线。
        //
        // 🔴 **两条逐值照抄原版的**：
        //   · `Card 3D` 的 `localRotation` = **绕 Y 转 180°**、`localScale` = **0.88586**
        //     ⇒ 世界尺寸 ≈ **1.852 × 2.622**（`3DBody` 那一节的实读值）。
        //     **徽标位置**（`Core/Badges.cs:47-51`）当初就是按这个 bbox 换算的 ⇒ 两边对得上，别各改一半。
        //   · **立绘吃 mesh 的 UV1**、外面套一个编译期写死的 mask —— 在 shader 里，见 `Shaders/Card3D.shader`。
        const float Body3DScale = 0.88586f;

        // ==================================================================
        //  卡底那枚**软阴影**（原版 `BlobShadowController`）—— 🆕 2026-09-25
        // ==================================================================
        // 逐值出处与行为 = `Core/BlobShadow.cs` 的文件头（那里是**唯一出处**，这里不抄第二份）。
        // 这一节只负责「材质」与「精灵」两样资源。

        static Material _blobMat;

        /// <summary>软阴影的材质 = **原版 shader** `Everguild/Cards/BlobShadow`
        /// （属性表只有 `_MainTex`；颜色靠 `SpriteRenderer.color` 染，见 `BlobShadow.cs`）。
        /// 拿不到就返回 null ⇒ 那一层不建，并说明原因（**不许静默画个白的**）。</summary>
        public static Material BlobShadowMaterial()
        {
            if (_blobMat != null) return _blobMat;
            const string Name = "Everguild/Cards/BlobShadow";
            if (!WarpforgeVFX.WarpforgeShaderMap.TryResolve(Name, out var sh, out var src) || sh == null)
            {
                // 退而用 `Sprites/Default`：它**肯定**吃 `SpriteRenderer.color`（原版那个 shader 也吃，
                // 因为它是白图 + alpha 掩码，黑全靠渲染器染）。⚠️ 这条是**兜底**，日志里出声。
                sh = Shader.Find("Sprites/Default");
                if (sh == null) return null;
                _blobMat = new Material(sh) { name = "BlobShadow(fallback)" };
                _blobMat.renderQueue = 3000;               // 原版材质 `m_CustomRenderQueue`
                Debug.LogWarning($"[CardView] 解析不到原版 shader `{Name}` ⇒ 卡底软阴影退回 `Sprites/Default`"
                               + "（随包 shader bundle 在不在？见 `资料/特效还原_进度与交接.md` §三）");
                return _blobMat;
            }
            _blobMat = new Material(sh) { name = "BlobShadow" };
            _blobMat.renderQueue = 3000;                   // 原版材质 `m_CustomRenderQueue = 3000`
            Debug.Log($"[CardView] 卡底软阴影：`{Name}` ← {src}");
            return _blobMat;
        }

        static Sprite _blobSprite;

        /// <summary>软阴影的精灵。原版：矩形 **128×128** · `m_PixelsToUnits = 100` · pivot (0.5,0.5)；
        /// 我们导出的是**裁掉透明边**的内容（原版 `textureRect` = 124.848² @ (2.076,1.076)，我们存成 125²）
        /// ⇒ 按 **PPU 100** 建，内容尺寸与原版差 **0.12%**（原版整块 1.28、可见内容 1.2485）。</summary>
        public static Sprite BlobShadowSprite()
        {
            if (_blobSprite != null) return _blobSprite;
            var tex = CardArt.Card3DBlobShadow();
            if (tex == null) return null;
            _blobSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                        new Vector2(0.5f, 0.5f), 100f);
            _blobSprite.name = "Card blob shadow";
            return _blobSprite;
        }

        MeshRenderer _body3D;

        // ==================================================================
        //  🆕 2026-09-29：**卡体溶解**（原版「换材质 + 曲线驱动 `_DissolveAmount`」那一套）
        // ==================================================================
        //  原版机制（`BattleCardUI`）：`SetCardMaterial(minion3DRenderer, dissolveMaterial, true)`
        //    （`…__SetCardMaterial.c:19,24` —— 换的是 **`minion3DRenderer`(+0x180)**，不是 2D 那层；
        //     `dissolveMaterial` 字段 `+0x190`、传奇那张 `+0x198`），
        //     复位 = `DelayedResetMaterial(GetHandToBoardAnimEventTime())` → 换回 `defaultMaterial`(+0x188)。
        //  `_DissolveAmount` **不是代码写的** —— 它由 **clip 的材质曲线**驱动
        //    （出战那条 `Card Hand To Board` 里 1→0、区间 0.1667–0.7；阵亡那条我们还没查到，
        //     见 `资料/待办判据_战场与战斗视图.md` 第 9 条）。
        //  ⇒ 我们这套 2D 卡没有 clip，改成**按同一条时间轴用代码喂值**（调用方 = `CardFeel`）。
        //  ⚠️ **取不到原版那个 shader 就不做**：`DissolveSupported == false`，调用方退回「淡出 + 上浮 + 缩」
        //     并**出声**（不静默画个假的）。

        static Material _dissolveProto;
        Material _dissolveMat;
        Material _bodyMat;              // 正常态那份卡体材质（`BuildBody3D` 建的）
        bool _dissolveTried;
        float _dissolveAmount = -1f;

        // ── 2D 立绘那一层自己的不透明度（回手时「3D 体溶解出、2D 卡面淡回来」那一幕）──
        //  原版 `Card Hand To Board` 的镜像：`2DCard.m_Alpha`（CanvasGroup）0.25→0.5833 淡回 1、
        //  `2DCard.m_IsActive` 在 0.2167 打开（出处 → `CardFeel.ReturnToHand` 那一段注释）。
        //  ⚠️ **不能拿整卡 `SetAlpha` 顶替** —— 那个会连 3D 体和数值层一起淡（原版只淡 2D 那一层）。
        float _artAlpha = 1f;

        /// <summary>2D 立绘层自己的不透明度（0 = 全透明）。**只影响 `_art` 那一层**。</summary>
        public void SetArtAlpha(float a)
        {
            _artAlpha = Mathf.Clamp01(a);
            if (_art != null) Show(_art, _artAlpha > 0f);
            ApplyTint();
        }

        /// <summary>2D 立绘层现在的不透明度（自检断言用）。</summary>
        public float ArtAlpha { get { return _artAlpha; } }

        /// <summary>原版溶解 shader = **`Everguild/Cards/3D Card Dissolve`**（随包 `wf_shaders_extra.bundle`
        /// 里的**原件**，不是自建替代；拿到的 `src` 会打日志）。取不到返回 null。
        /// 常量表（`工具/dump_shader_blob.py "3D Card Dissolve"`）里有 `_DissolveTex` / `_DissolveAmount` /
        /// `_BorderColor(_2)` / `_BorderWidth` / `_DISSOLVE_CHANNEL_R|_B` —— **没有 `_Color`**。</summary>
        public static Material DissolveMaterialProto()
        {
            if (_dissolveProto != null) return _dissolveProto;
            const string Name = "Everguild/Cards/3D Card Dissolve";
            if (!WarpforgeVFX.WarpforgeShaderMap.TryResolve(Name, out var sh, out var src) || sh == null)
            {
                Debug.LogWarning($"[CardView] 解析不到原版 shader `{Name}` ⇒ **卡体溶解这一路不可用**"
                               + "（阵亡/回手会退回「淡出+上浮+缩」）。随包 shader bundle 在不在？"
                               + "跑 `python 工具/extract_missing_shaders.py`（见 `资料/特效还原_进度与交接.md` §三）");
                return null;
            }
            _dissolveProto = new Material(sh) { name = "Card3DDissolve" };
            Debug.Log($"[CardView] 卡体溶解材质：`{Name}` ← {src}");
            return _dissolveProto;
        }

        /// <summary>这张卡自己的溶解材质（**每张一份** —— `_CardImage` 是逐卡的立绘）。
        /// 取不到就返回 null（调用方如实退回）。</summary>
        Material DissolveMaterial()
        {
            if (_dissolveTried) return _dissolveMat;
            _dissolveTried = true;
            if (_body3D == null) return null;
            var proto = DissolveMaterialProto();
            if (proto == null) return null;

            _dissolveMat = new Material(proto);
            // 把正常态那份材质上的贴图/强度抄过来（原版两份材质本来就共用同一批图，
            // 只有溶解那几项不一样）——逐项 `HasProperty` 判，别假设两边字段一样多。
            var cur = _body3D.sharedMaterial;
            if (cur != null)
            {
                CopyTex(cur, _dissolveMat, "_BaseMap");
                CopyTex(cur, _dissolveMat, "_MatCap");
                CopyTex(cur, _dissolveMat, "_CardImage");
                CopyF(cur, _dissolveMat, "_MatCap_Intensity");
                CopyF(cur, _dissolveMat, "_MatCapPower");
                CopyF(cur, _dissolveMat, "_CountersIntensity");
            }
            // `_DissolveTex`：原版那张噪声图**不在我们工程里**（本仓 0 命中）⇒ 用工程里已有的一张
            // 噪声图顶上，**这是我们的选择，不是原版的做法**（原版材质指向哪张，判据见文件尾那条注释）。
            var noise = CardArt.DissolveNoiseTex();
            if (noise != null && _dissolveMat.HasProperty("_DissolveTex"))
                _dissolveMat.SetTexture("_DissolveTex", noise);
            _dissolveMat.SetFloat("_DissolveAmount", 0f);
            _dissolveAmount = 0f;
            return _dissolveMat;
        }

        static void CopyTex(Material from, Material to, string prop)
        {
            if (from.HasProperty(prop) && to.HasProperty(prop)) to.SetTexture(prop, from.GetTexture(prop));
        }
        static void CopyF(Material from, Material to, string prop)
        {
            if (from.HasProperty(prop) && to.HasProperty(prop)) to.SetFloat(prop, from.GetFloat(prop));
        }

        /// <summary>这张卡能不能做「材质溶解」（有 3D 卡体 + 取到原版溶解 shader）。</summary>
        public bool DissolveSupported { get { return _body3D != null && DissolveMaterial() != null; } }

        /// <summary>当前 `_DissolveAmount`（自检断言用）。没上过溶解材质时返回 **−1**。</summary>
        public float DissolveAmount { get { return _dissolveAmount; } }

        /// <summary>现在的 3D 卡体材质是不是**溶解那份**（自检断言用 —— 截图上看不出用的是哪份材质）。</summary>
        public bool OnDissolveMaterial
        {
            get { return _dissolveMat != null && _body3D != null && _body3D.sharedMaterial == _dissolveMat; }
        }

        /// <summary>换上溶解材质并把 `_DissolveAmount` 归 0（原版那条路的起点）。返回 false = 做不了。</summary>
        public bool BeginDissolve()
        {
            var m = DissolveMaterial();
            if (_body3D == null || m == null) return false;
            if (_body3D.sharedMaterial != m) _body3D.sharedMaterial = m;
            SetDissolveAmount(0f);
            return true;
        }

        /// <summary>喂 `_DissolveAmount`（0 = 完好 · 1 = 溶没了）。没上溶解材质时是**空操作**，
        /// 但**不改调用方的行为**（调用方要么先 `BeginDissolve()` 判过，要么按 `DissolveSupported` 分流）。</summary>
        public void SetDissolveAmount(float a)
        {
            if (_dissolveMat == null) return;
            _dissolveAmount = Mathf.Clamp01(a);
            _dissolveMat.SetFloat("_DissolveAmount", _dissolveAmount);
        }

        /// <summary>换回正常那张卡体材质（原版 `DelayedResetMaterial` 那一步）。</summary>
        public void EndDissolve()
        {
            if (_body3D == null || _body3D.sharedMaterial == _dissolveMat) return;
            // 正常材质是建体时那份 —— 直接由 `BuildBody3D` 的逻辑重建一份不值当，留个引用更省。
            if (_bodyMat != null) _body3D.sharedMaterial = _bodyMat;
            _dissolveAmount = -1f;
        }

        // ==================================================================
        //  🆕 2026-10-01：`WFModuleChangeMaterial` 要的那三个回调
        //  （原版 `BattleCardUI.SetCardMaterial` / `RestoreOriginalMaterial`）
        // ==================================================================
        // 判据（**逐行读过**，不是按名字猜）：
        //   `d:/2/tools/decomp_full/BattleCardUI__SetCardMaterial.c` 与
        //   `…__RestoreOriginalMaterial.c`：
        //   · **全在一个 Renderer 上**（原版 `+0x180`）—— 就是我们的 `_body3D`（卡 3D 体），
        //     别的层（卡框 / 立绘 quad / 徽标）**一律不碰**；
        //   · 原版先把「当前材质」**实例化**后缓存进 `+0x2c8`（`Renderer.GetMaterial()` 给的是一份实例），
        //     `RestoreOriginalMaterial` 就是 `SetMaterial(那一份)` ⇒ 我们直接用建体时那份 `_bodyMat`；
        //   · `Renderer.SetMaterial(m)` 是 Unity 6 的「设材质、**不实例化**」⇒ 等价于 `sharedMaterial = `
        //     （模块那边本来就是 `new Material(...)` 的私有实例，见 `WFModuleChangeMaterial` 文件头 3）；
        //   · 末参 `initializeWithCardImage` ⇒ 原版调 `SetCardImageTo3DBase()`，见下面那个方法。

        /// <summary>把 3D 卡体换成 <paramref name="m"/>（原版 `BattleCardUI.SetCardMaterial`）。
        /// 返回**换上去的那份材质**（= 原版的返回值）；<paramref name="initializeWithCardImage"/> 为真时
        /// 顺带把卡图喂进 `_CardImage`。⚠️ **取不到 3D 卡体**（平面模式 / 还没建体）返回 `null`，不静默。</summary>
        public Material SetCardMaterial(Material m, bool initializeWithCardImage)
        {
            if (_body3D == null) return null;
            _body3D.sharedMaterial = m;
            if (initializeWithCardImage) SetCardImageTo3DBase();
            return _body3D.sharedMaterial;
        }

        /// <summary>换回建体时那份材质（原版 `BattleCardUI.RestoreOriginalMaterial` —— 它同时也会
        /// 再喂一次卡图，照抄）。</summary>
        public void RestoreOriginalMaterial()
        {
            if (_body3D == null) return;
            if (_bodyMat != null) _body3D.sharedMaterial = _bodyMat;
            SetCardImageTo3DBase();
        }

        /// <summary>把这张卡的立绘喂进 3D 卡体材质的 `_CardImage`（原版 `SetCardImageTo3DBase`）。
        ///
        /// 🔴 **判据只此一处**（2026-10-01 抽出来）：原来 `SetData` 里**内联写过同一句**
        /// （`if (_body3D…HasProperty("_CardImage")) _body3D.sharedMaterial.SetTexture("_CardImage", …)`），
        /// 现在那条路改调本方法 —— 「两处写同一条规则 = 迟早不一致」（CLAUDE.md 三）。</summary>
        public void SetCardImageTo3DBase()
        {
            if (_body3D == null || _body3D.sharedMaterial == null) return;
            if (!_body3D.sharedMaterial.HasProperty("_CardImage")) return;
            _body3D.sharedMaterial.SetTexture("_CardImage", ArtTexture(Data));
        }

        /// <summary>这张卡的立绘贴图（原版 `actingCard.rawCard.cardSprite.texture`）——
        /// 给 `WFModuleChangeMaterial.CardTexture` 那个钩子用。</summary>
        public Texture CardImageTexture { get { return ArtTexture(Data); } }

        /// <summary>现在的 3D 卡体材质是不是建体时那份（自检断言用）。</summary>
        public bool OnOriginalBodyMaterial
        {
            get { return _bodyMat != null && _body3D != null && _body3D.sharedMaterial == _bodyMat; }
        }
        /// <summary>现在的 3D 卡体材质（自检断言用）。</summary>
        public Material Body3DMaterial { get { return _body3D != null ? _body3D.sharedMaterial : null; } }

        /// <summary>🆕 2026-09-25：卡底那枚软阴影（原版 `BlobShadowController`，见 `Core/BlobShadow.cs`）</summary>
        BlobShadow _blobShadow;
        /// <summary>🆕 2026-09-25：盖在卡上的**残骸体**（原版 `RemnantBody3D &lt;阵营>`）——
        /// 懒建、建好就留着（见 <see cref="SetRemnantBody"/>）。**不进 `_layers`**。</summary>
        GameObject _remnantBody;

        // ==================================================================
        //  「未行动」绿光（原版 `3DBody/CanActParticles` + 它的子节点 `RotatingRing`）
        //  —— 🆕 2026-09-26
        // ==================================================================
        // 逐值出处（**唯一出处**，别在这儿抄第二份）：
        //   `项目任务.md` §三 第 12 条 第 3 项 · 资产在
        //   `bundle_battleprefabs_vfxandmisc_assets_all/ParticleSystem/ParticleSystem_-467847035904746560.json`
        //   （`CanActParticles`）与 `…_-2548938496250242112.json`（`RotatingRing`）。
        //
        // 🔴🔴 **父链必须解对，否则整层是错的**（这一条差点写错，记下来）：
        //   原版树 = `CardPrefab / Board Elements / 3DBody / {Card 3D, CanActParticles, …}` ——
        //   **`CanActParticles` 是 `3DBody` 的直接子节点、和 `Card 3D` 是平级的兄弟**，
        //   活在**未缩放**的 3DBody 空间里（`CardPrefab`→`Board Elements`→`3DBody` 三级 scale 全 1）。
        //   ⇒ 它的 `localScale 1.735563` 是**相对卡自己**的。
        //
        //   ⚠️ **我们的 `_body3D` 是原版的那张 `Card 3D`（scale 0.88586 + 绕 Y 180°）**，
        //      **不是** `3DBody`。所以**绝不能挂在 `_body3D` 底下** —— 那样会白白多乘一次
        //      0.88586（世界尺寸 1.7356 → 1.5377），还会被那个 yaw 180° 带着转。
        //   ✅ 我们的卡根节点（`transform`）**就是** `3DBody` 的等价物：卡本体中心在原点、
        //      链上无缩放无旋转（`ArenaSlots.RootPosition` 补的就是「原版卡根落在地面上」那一步）。
        //      ⇒ 挂在 `transform` 下，坐标只差**一步**换算：`ourY = 3DBody.y − Height/2`
        //      （同 `资料/3DBody_原版场上卡体规格.md` §四之二那条，**数值层/徽标层也是这么换的**）。
        //
        // 材质（实读 `Material/<pathID>.json`）：
        //   · `CanActParticles` → **`Circle_Hoop Additive`**，shader = **URP 自带的
        //     `Universal Render Pipeline/Particles/Unlit`**（`Shader.Find` 就有），`_BaseMap` = `Circle_Hoop`，
        //     `_SrcBlend 5 / _DstBlend 1`（加性）、`_ZWrite 0`、`_Cull 0`、`_Surface 1`、
        //     关键字 `_SURFACE_TYPE_TRANSPARENT`、`m_CustomRenderQueue` = **3000**。
        //   · `RotatingRing`   → **`Sparks UI Additive Scroll`**，shader = **`Everguild/FX/Halo UV scroll`**
        //     （**随包 bundle 里有**，实测 `WarpforgeShaderLoader` 那份 115 个 shader 里就有它；
        //      **名字里的 `Scroll` 就是那圈会转的原因** —— UV 在滚），`_MainTex` = `Spark UI`，混合同上、队列 3000。
        //
        // ⚠️ **别照战场粒子那条路走**：那边一律 `Shader.Find("URP/Particles/Unlit")`、**从没查过原版 shader**
        //    （`项目任务.md` §三 第 3 条 第 11 项 ② 就是这条账）。这里两张材质**要么本来就是 URP 自带、
        //    要么真的取得到**，两件都按原版来。

        /// <summary>绿光那棵树（懒建、建好留着）。**不进 `_layers`** —— 颜色是它自己说了算。</summary>
        GameObject _canAct;

        /// <summary>绿光亮着没有（自检用）。</summary>
        public bool CanActVisible { get { return _canAct != null && _canAct.activeSelf; } }

        /// <summary>「未行动」绿光亮 / 灭。判据在**调用方**（`BattleDriver.SyncBoard` 用
        /// `RuleCore.CanActNow` —— 那是唯一判据，本层只管画）。</summary>
        public void SetCanAct(bool on)
        {
            if (!on)
            {
                if (_canAct != null && _canAct.activeSelf) _canAct.SetActive(false);
                return;
            }
            if (_canAct == null)
            {
                // 只有**场上形态**才有这层（原版它在 `Board Elements` 下，手牌那张是 `2DCard`）。
                if (_faceMode != CardFace.Board) return;
                _canAct = BuildCanAct();
                if (_canAct == null) return;
            }
            if (!_canAct.activeSelf) _canAct.SetActive(true);
        }

        /// <summary>原版 `CanActParticles` 的父节点 `3DBody` 坐标 → 我们的卡根坐标。
        /// **只有一步**：`y −= Height/2`（我们卡根的原点在卡中心，原版在卡底边）。</summary>
        static Vector3 From3DBody(float x, float y, float z)
        {
            return new Vector3(x, y - Height * 0.5f, z);
        }

        UnityEngine.Mesh _quadMesh;
        /// <summary>原版 `CanActParticles` 的 `ParticleSystemRenderer.m_Mesh` =
        /// `{FileID: 5, PathID: 10210}` —— **`10210` 是 Unity 内置网格 `Quad` 的 PathID**
        /// （`FileID 5` = 外部引用，即编辑器的 `unity default resources`，本包真实资产全是 64 位哈希）。
        /// 已用 UnityPy 实读证实：`10202 Cube · 10206 Cylinder · 10207 Sphere · 10208 Capsule ·
        /// 10209 Plane · **10210 Quad** · 10211 Icosphere`。</summary>
        UnityEngine.Mesh QuadMesh()
        {
            if (_quadMesh != null) return _quadMesh;
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _quadMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            // 批处理下没有帧循环 ⇒ 必须立刻销毁（`Object.Destroy` 不生效）
            DestroyImmediate(tmp);
            return _quadMesh;
        }

        static void SetF(Material m, string p, float v) { if (m.HasProperty(p)) m.SetFloat(p, v); }
        static void SetC(Material m, string p, Color v) { if (m.HasProperty(p)) m.SetColor(p, v); }

        /// <summary>按**原版 shader** 建一份粒子材质。
        /// ⚠️ **只在 shader 真声明了那个属性时才写**（`HasProperty`）—— 原版那批材质上带着
        /// 内置 Standard shader 的**残留值**（`_BumpScale` / `_Metallic` / `_Glossiness` / `_Parallax` /
        /// `_Mode` / `_DistortionStrength` …），照抄会让自建 shader 把它们当活值用（这工程踩过：
        /// 抓屏扭曲变成了不透明覆盖）。用原版 shader 时它们本来就是死值，**不写才是对的**。</summary>
        static Material BuildFxMaterial(string name, string shaderName, Texture2D tex, string texProp)
        {
            Shader sh = null;
            string src = null;
            if (WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(shaderName, out sh) && sh != null)
            {
                src = "随包 bundle";
            }
            else
            {
                // `Circle_Hoop Additive` 用的那个 shader **本来就是 URP 自带的**
                // （`Universal Render Pipeline/Particles/Unlit`）⇒ 工程里也有，退一步用它。
                // ⚠️ 但 `Everguild/FX/Halo UV scroll` 这类**只有随包 bundle 里才有**，退不了 —— 那种就得如实报错。
                sh = Shader.Find(shaderName);
                if (sh != null) src = "工程内建";
            }
            if (sh == null)
            {
                Debug.LogWarning($"[CardView] 未行动绿光：取不到原版 shader `{shaderName}`"
                               + $"（随包 shader bundle 在不在？见 `资料/特效还原_进度与交接.md` §三）"
                               + $" ⇒ **整层不建**（宁可没有，也不拿别的 shader 顶替 —— 那会静默画成另一副样子）");
                return null;
            }
            var m = new Material(sh) { name = name };
            SetC(m, "_BaseColor", Color.white);
            SetC(m, "_Color", Color.white);
            SetF(m, "_SrcBlend", 5f); SetF(m, "_DstBlend", 1f);          // 原版：SrcAlpha / One = 加性
            SetF(m, "_SrcBlendAlpha", 1f); SetF(m, "_DstBlendAlpha", 1f);
            SetF(m, "_ZWrite", 0f);
            SetF(m, "_Cull", 0f);
            SetF(m, "_Surface", 1f);
            SetF(m, "_Blend", 2f);
            SetF(m, "_AlphaClip", 0f);
            SetF(m, "_Cutoff", 0.5f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");                 // 原版 `m_ValidKeywords` 就这一条
            m.renderQueue = 3000;                                        // 原版 `m_CustomRenderQueue`
            if (tex != null)
            {
                if (m.HasProperty(texProp)) m.SetTexture(texProp, tex);
                else Debug.LogWarning($"[CardView] 绿光材质 `{name}`：原版 shader 没有 `{texProp}` 槽"
                                    + $" ⇒ 贴图 **没进去**（那圈会画成纯色）");
            }
            else Debug.LogWarning($"[CardView] 绿光材质 `{name}`：贴图取不到（`Resources/Art/card3d/{name}`？）");
            Debug.Log($"[CardView] 未行动绿光材质：`{name}` ← 原版 shader `{src}`");
            return m;
        }

        GameObject BuildCanAct()
        {
            var matHoop  = BuildFxMaterial("Circle_Hoop Additive", "Universal Render Pipeline/Particles/Unlit",
                                           CardArt.CanActCircleHoop(), "_BaseMap");
            var matSpark = BuildFxMaterial("Sparks UI Additive Scroll", "Everguild/FX/Halo UV scroll",
                                           CardArt.CanActSparkUI(), "_MainTex");
            if (matHoop == null || matSpark == null) return null;

            // ---- CanActParticles（父：我们卡根 = 原版 `3DBody`）----
            var root = new GameObject("CanActParticles");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = From3DBody(0f, 0.030168533f, 0.006052971f);
            root.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // 四元数 (0.7071068,0,0,0.7071068)
            root.transform.localScale = Vector3.one * 1.7355630f;

            var psA = root.AddComponent<ParticleSystem>();
            var mA = psA.main;
            mA.duration = 1f;                     // lengthInSec
            mA.simulationSpeed = 0.5f;
            mA.loop = false;
            mA.prewarm = false;
            mA.playOnAwake = true;
            mA.startLifetime = 1f;
            mA.startSpeed = 0f;
            mA.startSize = 1.9f;
            mA.startRotation = 6.283185f;         // 弧度 = 2π（每颗随机一个初始 Z 转角）
            mA.startColor = new Color(0.30103764f, 0.94509804f, 0.09019607f, 0.22745098f);
            mA.gravityModifier = 0f;
            mA.maxParticles = 2;                  // 原版 maxNumParticles = 2
            mA.simulationSpace = ParticleSystemSimulationSpace.Local;   // 原版 moveWithTransform = 0
            // 🔴 `looping = false` 但 `ringBufferMode = Loop`(2)、range (0.1,0.9)
            //    ⇒ **那一颗粒子被循环回收**，这才是「常亮」的真因（照抄，别改成 looping）。
            mA.ringBufferMode = ParticleSystemRingBufferMode.LoopUntilReplaced;
            mA.ringBufferLoopRange = new Vector2(0.1f, 0.9f);
            mA.cullingMode = ParticleSystemCullingMode.Automatic;
            mA.stopAction = ParticleSystemStopAction.None;
            mA.scalingMode = ParticleSystemScalingMode.Hierarchy;       // 原版 scalingMode = 0

            var shA = psA.shape;
            shA.enabled = true;
            shA.shapeType = (ParticleSystemShapeType)6;   // 原版序列化原值 = 6 = **Mesh**
            //   🔴 **`type 6` 是 Mesh，不是 Donut**（枚举实据 = `d:/2/tools/il2cpp_out/dump.cs:1089537`：
            //      `Sphere0 … Cone4 Box5 **Mesh6** … **Donut17**`）。而 `ShapeModule.m_Mesh` 是**空的**
            //      （PathID 0）⇒ 原版就是「形状等于一个点」。`radius 0.01 / donutRadius 0.2 /
            //      radiusThickness 1` 都是**同结构里的残留值**，照原样设、别按名字改。
            //   ⚠️ 那圈「环」的样子**全来自贴图**（`Circle_Hoop`），不是这个形状给的。
            shA.radiusThickness = 1f;
            shA.donutRadius = 0.2f;
            shA.radius = 0.01f;
            shA.arc = 360f;
            shA.angle = 0f;

            var emA = psA.emission;
            emA.enabled = true;
            emA.rateOverTime = 0f;                // 只有 t=0 那一次 burst
            emA.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var colA = psA.colorOverLifetime;
            colA.enabled = true;
            var gA = new Gradient();
            gA.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                // 原版 m_NumAlphaKeys = 3：0.05→0.502 · 0.4882→0.9629 · 0.95→0.502（之后保持）
                new[] { new GradientAlphaKey(0.50196081f, 0.05f),
                        new GradientAlphaKey(0.96288514f, 0.48823529f),
                        new GradientAlphaKey(0.50196081f, 0.95f) });
            colA.color = new ParticleSystem.MinMaxGradient(gA);

            var prA = root.GetComponent<ParticleSystemRenderer>();
            prA.renderMode = ParticleSystemRenderMode.Mesh;   // 原版 m_RenderMode = 4
            prA.mesh = QuadMesh();
            prA.sharedMaterial = matHoop;
            prA.alignment = ParticleSystemRenderSpace.Local;  // 原版 m_RenderAlignment = 2
            prA.sortMode = ParticleSystemSortMode.None;       // 原版 m_SortMode = 0
            prA.sortingFudge = -3f;                           // 原版 m_SortingFudge = −3
            prA.pivot = Vector3.zero;
            prA.minParticleSize = 0f;
            prA.maxParticleSize = 7f;
            prA.lengthScale = 2f;

            // ---- RotatingRing（`CanActParticles` 的子节点）----
            var ring = new GameObject("RotatingRing");
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0f, -0.04743f);
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            ring.transform.localScale = new Vector3(0.59292f, 0.59292f, 0.05929f);

            var psB = ring.AddComponent<ParticleSystem>();
            var mB = psB.main;
            mB.duration = 1f;                     // lengthInSec
            mB.simulationSpeed = 1f;
            mB.loop = false;
            mB.prewarm = false;
            mB.playOnAwake = true;
            mB.startLifetime = 1f;
            mB.startSpeed = 0f;
            mB.startSize = 1.4f;
            mB.startRotation = 0f;
            mB.startColor = new Color(0.45084f, 0.94510f, 0.25098f, 1f);
            mB.gravityModifier = 0f;
            mB.maxParticles = 1;                  // 原版 maxNumParticles = 1
            mB.simulationSpace = ParticleSystemSimulationSpace.Local;
            mB.ringBufferMode = ParticleSystemRingBufferMode.LoopUntilReplaced;
            mB.ringBufferLoopRange = new Vector2(0.25f, 0.5f);   // ⚠️ 与上层的 (0.1,0.9) **不同**，别抄错
            mB.cullingMode = ParticleSystemCullingMode.Automatic;
            mB.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var shB = psB.shape;
            shB.enabled = false;                  // 原版 Shape/Color/Rotation/Velocity **全部 disabled**

            var emB = psB.emission;
            emB.enabled = true;
            emB.rateOverTime = 0f;
            emB.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var prB = ring.GetComponent<ParticleSystemRenderer>();
            prB.renderMode = ParticleSystemRenderMode.Mesh;
            prB.mesh = CardArt.CanActRingMesh();
            prB.sharedMaterial = matSpark;
            prB.alignment = ParticleSystemRenderSpace.Local;
            prB.sortMode = ParticleSystemSortMode.Distance;   // 原版 m_SortMode = 1
            prB.sortingFudge = -2f;
            prB.pivot = Vector3.zero;
            prB.minParticleSize = 0f;
            prB.maxParticleSize = 5f;
            prB.lengthScale = 2f;

            root.SetActive(false);                // 由 `SetCanAct` 开关
            return root;
        }

        /// <summary>批处理下没有帧循环 ⇒ 粒子不会自己走。自检里手动推一把。</summary>
        public void SimulateCanAct(float seconds)
        {
            if (_canAct == null) return;
            foreach (var ps in _canAct.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Simulate(seconds, true, true);
                ps.Play();
            }
        }

        /// <summary>这一层建出来没有（自检用；`false` = 资源缺，报警告了）。</summary>
        public bool CanActBuilt { get { return _canAct != null; } }

        /// <summary>绿光那棵树（自检逐值对参数用；没建出来时是 null）。</summary>
        public GameObject CanActRoot { get { return _canAct; } }

        /// <summary>建场上那张 3D 卡体。返回 null = 资源不在（调用方退回 2D 立绘）。</summary>
        MeshRenderer BuildBody3D(Texture2D artTex)
        {
            // 🔴 诊断 / 自检开关（默认 false）—— 造出「网格或 shader 取不到」那条退回路径，
            //    见 `DebugNoBody3D` 的注释。⚠️ 必须排在**建 GameObject 之前**（真那条路也是先返回 null、
            //    连 `body3D` 这个子节点都不建）。
            if (DebugNoBody3D)
            {
                Debug.LogWarning("[CardView] `DebugNoBody3D` 开着 ⇒ 场上退回 2D 立绘"
                               + "（**诊断 / 自检专用** —— 产品里这个开关恒为 false，跑完请关回去）。");
                return null;
            }
            var mesh = CardArt.Card3DMesh();
            var sh = Shader.Find("CardPresentation/Card3D");
            if (mesh == null || sh == null)
            {
                Debug.LogWarning("[CardView] 建不出 3D 卡体（网格 " + (mesh == null ? "缺" : "有")
                               + " / shader " + (sh == null ? "缺" : "有")
                               + "）⇒ 场上退回 2D 立绘。跑 `工具/import_original_3dcard.py` 补资源，"
                               + "shader 在 `Assets/CardPresentation/Shaders/Card3D.shader`");
                return null;
            }

            var go = new GameObject("body3D");
            go.transform.SetParent(transform, false);
            // 🔴 **2026-09-20 补 `localPosition`（原来漏了，真 bug）**：
            //    这个网格的**原点在卡的底边**（实测 Y 0.0124…2.9729，见 `资料/3DBody_原版场上卡体规格.md` §一），
            //    而卡根节点这一套坐标（数值层 / 7 槽徽标 / 高亮 / 命中盒）全都是**卡中心在原点**的
            //    （卡本体 2.0927 × 3.3313）⇒ 不补偿的话 3D 体整个长在卡**上半截**、还探出卡顶，
            //    数值层则孤零零吊在卡体下方约 1.5 卡单位（自检截图里每张场卡都看得到）。
            //    补 `−Height/2` 之后：**3D 体的底边 = 卡本体底边**，于是 mesh 自带的
            //    「正面下部计数器面板」（卡空间 y 0.19…0.96）正好落在我们画攻/远/血的那几个位上。
            //    出处：面板位置由我自己量 mesh 的 UV2.x==1 顶点（111 个）得到，见下文注释。
            go.transform.localPosition = new Vector3(0f, -Height * 0.5f, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // 原版 `Card 3D` 的 localRotation
            go.transform.localScale = Vector3.one * Body3DScale;           // 原版 `Card 3D` 的 localScale
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var m = new Material(sh) { name = "Card3D" };
            // 材质属性值 = 原版 `Card 3d Lvl1`（`资料/3DBody_原版场上卡体规格.md` §一 那一行）
            m.SetTexture("_BaseMap", CardArt.Card3DBase());
            m.SetTexture("_MatCap", CardArt.Card3DMatcap());
            m.SetTexture("_CardImage", artTex != null ? artTex : Texture2D.whiteTexture);
            if (m.HasProperty("_MatCap_Intensity"))   m.SetFloat("_MatCap_Intensity", 1.69f);
            if (m.HasProperty("_MatCapPower"))        m.SetFloat("_MatCapPower", 1.24f);
            if (m.HasProperty("_CountersIntensity"))  m.SetFloat("_CountersIntensity", 1.12f);
            mr.sharedMaterial = m;
            _bodyMat = m;               // 溶解那一套要换回来（原版 `DelayedResetMaterial` 换的就是它）
            _layers.Add(mr);            // 参与整卡着色（`_Color` 在 shader 里）—— 高亮/置灰/淡出都靠它

            // 🆕 2026-09-25：**卡底那枚软阴影**（原版 `CardPrefab / … / Card 3D / <软阴影>`）。
            // 🔴 **它不进 `_layers`** —— `_layers` 是「整卡着色」名单（`ApplyTint` 会把每层刷成
            //    状态色 × 透明度），而影子那层的颜色**是它自己说了算**（原版 `m_Color` = 纯黑 α0.361）。
            //    只把**整卡淡出**跟过去（`ApplyTint` 里那句 `SetCardAlpha`），免得卡消散完了影子还实着。
            // 逐值与行为 → `Core/BlobShadow.cs` 文件头（唯一出处）。
            _blobShadow = BlobShadow.Create(go.transform, go.transform);
            // 建的时候整卡可能已经淡过一轮（`SetAlpha` 早于建体）⇒ 把当前不透明度补给它
            if (_blobShadow != null) _blobShadow.SetCardAlpha(_alpha);
            return mr;
        }

        /// <summary>`Sprites/Default`：吃 alpha + 有 `_Color` 可以着色（模板，用的时候要 `new Material`）</summary>
        static Material BaseMaterial()
        {
            if (_quadMat == null) _quadMat = new Material(Shader.Find("Sprites/Default"));
            return _quadMat;
        }

        /// <summary>徽标图标层的**模板材质**（自建灰化 shader，见 `Shaders/TraitDisabled.shader`）。
        /// `AddLayer` 会对它 `new Material(...)` 拷一份 ⇒ 每个徽标位各改各的，互不影响。</summary>
        static Material TraitDisabledMaterial()
        {
            if (_traitDisabledMat == null)
            {
                var sh = Shader.Find("CardPresentation/TraitDisabled");
                if (sh == null)
                {
                    Debug.LogError("[CardView] 找不到 `CardPresentation/TraitDisabled` —— "
                                 + "徽标的「未激活态」退不了灰（原版 `disabledMaterial` 是 `Sprite Greyscale`）");
                    return BaseMaterial();
                }
                _traitDisabledMat = new Material(sh);
            }
            return _traitDisabledMat;
        }
        static Material _traitDisabledMat;

        /// <summary>把一个徽标图标层的**灰化程度**设成 `active ? 0 : 1`
        /// —— 原版是同名属性 `_GreyScale`（`disabledMaterial` 那份材质上就是 1.0）。
        /// ⚠️ 认不出这个属性时**出声**（模板退回 `Sprites/Default` 的那条路）—— 静默退灰不生效很隐蔽。</summary>
        static void SetBadgeGrey(MeshRenderer layer, bool active)
        {
            if (layer == null || layer.sharedMaterial == null) return;
            var m = layer.sharedMaterial;
            if (!m.HasProperty("_GreyScale"))
            {
                Debug.LogWarning("[CardView] 徽标材质没有 `_GreyScale` —— 未激活态不会变灰");
                return;
            }
            m.SetFloat("_GreyScale", active ? 0f : 1f);
        }

        /// <summary>当前的状态色。`SetData` 原地重建 TMP 文字时要把它乘回去</summary>
        Color _tint = Color.white;

        /// <summary>整卡的不透明度（0..1）。和状态色是**两个维度** ——
        /// 状态色改颜色、它改 alpha。`SetHighlight` 重设状态色时**不动它**
        /// （消散到一半的卡被点亮一下又变回不透明的，就是没分开的后果）。
        /// 用户 2026-09-13 点名的「阵亡消散 / 发牌入场」用得到它。</summary>
        float _alpha = 1f;

        public float Alpha { get { return _alpha; } }

        /// <summary>整卡透明度。**TMP 的字不吃材质 `_Color`**（走顶点色），所以要单独乘一遍 ——
        /// 不乘的话卡都透明了、字还在（和 `SetTint` 里那条是同一个坑）。</summary>
        public void SetAlpha(float a)
        {
            _alpha = Mathf.Clamp01(a);
            ApplyTint();
        }

        /// <summary>把「状态色 × 不透明度」写进所有层。**改颜色和改 alpha 都走这里**，
        /// 免得两条路各写一遍、迟早不一致。</summary>
        void ApplyTint()
        {
            var c = new Color(_tint.r, _tint.g, _tint.b, _tint.a * _alpha);
            foreach (var r in _layers)
            {
                if (r == null || r.sharedMaterial == null) continue;
                // ⚠️ **溶解材质（原版 `3D Card Dissolve`）没有 `_Color`** —— 直接写 `.color` 会报
                //    「Material doesn't have a color property '_Color'」并且**那一层整个被跳过**。
                //    判一下再写（正常态那份 `Card3D.shader` 有 `_Color`，行为不变）。
                if (!r.sharedMaterial.HasProperty("_Color")) continue;
                r.sharedMaterial.color = c;
            }
            // 🆕 2026-09-29：**2D 立绘那一层再乘上它自己的不透明度**（回手那一幕的「卡面淡回来」，
            //    见 `SetArtAlpha`）。放在整卡循环之后覆盖，理由同下面 `_textBg` 那两段。
            if (_art != null && _art.sharedMaterial != null && _art.sharedMaterial.HasProperty("_Color"))
                _art.sharedMaterial.color = new Color(_tint.r, _tint.g, _tint.b, _tint.a * _alpha * _artAlpha);

            // 🔴🔴 **文字底板不参与整卡着色**（2026-09-19 修，用户报的「白底挡住插图」就是它）。
            //    `_textBg` 也是 `_layers` 里的一员，所以上面那个 foreach 会把它**当成普通图层刷成
            //    `_tint`** —— 而 `_tint` 平时是**白 α1** ⇒ 那块半透明黑底被刷成**不透明白底**，
            //    把下面的立绘整个盖住（对战里手牌/场上卡的下半截就是这样）。
            //    原版这块底板的颜色是**写死的** `m_Color = (0,0,0,0.647)`，**不跟着卡的状态变**。
            //    ⇒ 刷完统一色之后**单独把它刷回去**（只乘卡的整体淡出 `_alpha`，不乘 tint 的 RGB）。
            if (_textBg != null && _textBg.sharedMaterial != null)
                _textBg.sharedMaterial.color = new Color(0f, 0f, 0f, TextBgAlpha * _alpha);

            // 🔴 **同理（2026-09-19）**：软光/影那层的颜色**是它自己说了算**的
            //    （原版 shader 用的是 `_ShadowColor` / `_Outline`，没有 `_Color`）——
            //    上面那个 foreach 只会给它设一个它没有的 `_Color`（no-op）。
            //    这里只把**卡的整体淡出**跟过去：卡淡出了、影子还实着，一眼就假。
            if (_shadowLayer != null && _shadowLayer.sharedMaterial != null)
            {
                var sm = _shadowLayer.sharedMaterial;
                if (sm.HasProperty("_ShadowColor"))
                    sm.SetColor("_ShadowColor", new Color(ShadowColor.r, ShadowColor.g,
                                                          ShadowColor.b, ShadowColor.a * _alpha));
                // 描边（`_Outline`）同理要跟着淡 —— 但它可能是**正在补间**的（`SetOutline`），
                // 那种时候别插手：补间的每一帧自己会乘 `_alpha`。
                bool outlineBusy = _outlineTween != null && _outlineTween.IsActive();
                if (!outlineBusy && sm.HasProperty("_Outline"))
                {
                    var oc = _outlineTarget;
                    oc.a *= _alpha;
                    sm.SetColor("_Outline", oc);
                }
            }

            // 🆕 2026-09-25：**卡底那枚软阴影也要跟着整卡淡出**（同「软光/影」那条的理由）——
            // 它是 `SpriteRenderer.color` 说了算（原版 shader 没有 `_Color`），所以走 `SetCardAlpha`。
            if (_blobShadow != null) _blobShadow.SetCardAlpha(_alpha);

            if (_title != null) _title.color = (Color)InkName * c;
            if (_keywords != null) _keywords.color = (Color)InkDesc * c;
            if (_army != null) _army.color = (Color)InkArmy * c;
            if (_race != null) _race.color = (Color)InkArmy * c;
            // 🆕 2026-10-18（`A985④`）：「由谁造出来的」也一样跟整卡状态色 × 淡出走
            // （它不在 `_layers` 里，上面那个 foreach 刷不到它 —— 同 `_title` 那一族）。
            if (_createdBy != null) _createdBy.color = (Color)CreatedByInk * c;

            // 🆕 2026-09-29：整组徽标自己的那个不透明度（原版 `NestedFadeGroup.alpha`）——
            // 上面那个 foreach 已经把徽标各层刷成「状态色 × 整卡 α」了，这里再乘一层**只属于徽标**的。
            ApplyBadgeAlpha();

            // 🆕 2026-09-29：四个数值的**闪现色**同理要重贴（上面那个 foreach 会把它刷成状态色，
            // 不重贴的话受击变色撑不过一帧）。
            ApplyStatFlashes();
        }

        /// <summary>整组徽标的不透明度（原版 `BoardTraitIcon.fadeGroup` 那个 `NestedFadeGroup.alpha`）。
        /// 独立于整卡 α：督军落场时要把徽标单独淡回来（见 <see cref="FadeBadges"/>）。</summary>
        float _badgeAlpha = 1f;
        Tweener _badgeFade;

        void ApplyBadgeAlpha()
        {
            if (_badgeAlpha >= 1f) return;                  // 常态（1）不用动 —— 上面那个 foreach 已经刷好了
            for (int i = 0; i < _badgeIcons.Count; i++)
            {
                if (_badgeIcons[i] != null && _badgeIcons[i].sharedMaterial != null)
                {
                    var c = _badgeIcons[i].sharedMaterial.color;
                    c.a *= _badgeAlpha;
                    _badgeIcons[i].sharedMaterial.color = c;
                }
                if (i < _badgePlates.Count && _badgePlates[i] != null && _badgePlates[i].sharedMaterial != null)
                {
                    var c = _badgePlates[i].sharedMaterial.color;
                    c.a *= _badgeAlpha;
                    _badgePlates[i].sharedMaterial.color = c;
                }
                if (i < _badgeCounters.Count && _badgeCounters[i] != null)
                {
                    // ⚠️ 角标文字**不在 `_layers` 里**（上面那个 foreach 刷不到它）⇒ 这里必须**赋绝对值**，
                    //    写成 `c.a *= _badgeAlpha` 会一次次叠乘（补间每帧调一次 ApplyTint）。
                    var c = (Color)BadgeCounterInk;
                    c.a = (BadgeCounterInk.a / 255f) * _alpha * _badgeAlpha;
                    _badgeCounters[i].color = c;
                }
            }
        }

        /// <summary>
        /// **整组徽标淡入 / 淡出** —— 原版 `BattleCardUI.FadeAllTraitsIcons(targetAlpha, time)`：
        /// 逐个 `BoardTraitIcon.DOFade(targetAlpha, time)`，而 `DOFade` 里补间的是
        /// `NestedFadeGroup.alpha`（`BoardTraitIcon__DOFade.c` 的 getter/setter 就是 `fadeGroup` 的
        /// `&lt;DOFade>b__18_0/&lt;b__18_1`）。
        ///
        /// 🔴 **原版唯一的调用点**（全量反编译逐文件 grep 过，只有这一处）：
        /// `CardScript.&lt;HeroLandIntoField>d__318.MoveNext:86` =
        /// `FadeAllTraitsIcons(fVar17, DAT_1834b2dc8)` —— `fVar17` 取自 `DAT_1834b2bb8` = **1.0f**、
        /// 时长 `DAT_1834b2dc8` = **0.3f**（两个都是从 `GameAssembly.dll` 浮点池实读的）。
        /// ⇒ 语义 = **督军落场时，把徽标用 0.3 秒淡回 1.0**。
        /// </summary>
        public void FadeBadges(float targetAlpha, float time)
        {
            if (_badgeFade != null && _badgeFade.IsActive()) _badgeFade.Kill();
            // `DOTween.To` 用 getter 读**当前值**当起点 ⇒ 补间中途再叫一次是接力，不是跳回 0/1
            _badgeFade = DOTween.To(() => _badgeAlpha, v => { _badgeAlpha = v; ApplyTint(); },
                                    Mathf.Clamp01(targetAlpha), time)
                                .SetUpdate(CardTween.Mode);
        }

        /// <summary>自检用：整组徽标现在的不透明度（原版 `NestedFadeGroup.alpha`）。</summary>
        public float BadgeAlpha { get { return _badgeAlpha; } }

        /// <summary>**立刻**把整组徽标设成某个不透明度（不补间）。
        /// 给「落场那一刻先隐掉、再淡回来」用 —— 原版是淡入前的初始态。</summary>
        public void SetBadgeAlpha(float a)
        {
            if (_badgeFade != null && _badgeFade.IsActive()) _badgeFade.Kill();
            _badgeAlpha = Mathf.Clamp01(a);
            ApplyTint();
        }

        /// <summary>卡的底色（高亮态改它）。1 = 原色，0.55 = 置灰不可打出</summary>
        public void SetTint(Color c)
        {
            _tint = c;
            ApplyTint();
        }

        /// <summary>当前底色（**自检读它** —— 详情窗那叠「前台白 / 相关卡 0.65」全靠它验，
        /// 见 `CardFan.BackTint` 与 `资料/阶段二_卡片详情窗_原版规格.md` §十·2）。</summary>
        public Color Tint { get { return _tint; } }

        /// <summary>当前状态色（状态机在 CardInteraction 那边，这里只负责显示）</summary>
        public CardHighlightState State { get; private set; }

        /// <summary>状态描边（卡后面那圈）。
        ///
        /// 为什么不用「整卡着色」表达状态：`material.color` 是**乘法** ——
        /// 红卡染「置灰」出来还是暗红，状态根本读不出来（试过）。
        /// 描边是独立一层，不受卡面配色影响，也是真实卡牌游戏的通行做法。</summary>
        public void SetHighlight(CardHighlightState s)
        {
            State = s;
            SetTint(CardHighlight.ColorOf(s));
            // 原版那一下是 **`CardBodyToScale × ScaleFactor` 的放大 + 补间**（2026-09-19 接上）
            // ⚠️ 原版**同一个色有手牌/棋盘两档**（手牌那档 `potentialTargetInHand` **不缩放**）
            //    ⇒ 要把「这张卡在不在棋盘上」传下去（见 `CardHighlight.ScaleOf` 的参数说明）。
            SetHighlightScale(CardHighlight.ScaleOf(s, _faceMode == CardFace.Board) > 1f);
            if (_rim != null)
            {
                // 🔴 **2026-09-29：这一层改照原版的 6 态走**（`CardHighlight.FrameColorOf`）。
                //   两处关键：
                //   ① **`Playable` 不再点亮它** —— 原版那个黄色是「**正在展示主动技能**」，
                //      而「打得出去」走的是 SDF `_Outline`（`OutlineOf`，**我们已接**）⇒ 原来那层
                //      **整卡染黄**既不是原版的做法、又和 `_Outline` 重复表达同一件事（判据 → §8b）。
                //   ② 颜色写进 **`_Outline`** —— 这个 shader **没有 `_Color`**（见 `RimMaterial` 的注释）。
                //   ⚠️ `Unplayable` 依旧不亮（alpha 0）：置灰靠 `SetTint`，而这圈是块比方大的矩形，
                //      在卡的透明角/透明边上会露出一圈灰白（用户 2026-09-12 报的「手牌里有白边」就是它）。
                var fc = CardHighlight.FrameColorOf(s);
                // 🆕 2026-09-29：残骸体那圈光**跟着状态色走**（原版 `CardHighlight.SetRemnantHighlight` 就是把
                //   主状态环的 `SpriteRenderer.color` **原样拷**给残骸那具）。⚠️ 传的是**没减过的**状态色。
                SetRemnantLight(fc);
                // 🆕 2026-09-29：**补间照原版的逐态时长走**（原来是一步跳过去、还带一个自定的 `0.95` 系数）。
                //   原版（`CardHighlight__ChangeFrameColor.c` 实读）：补的是 `SpriteRenderer.color`，
                //   时长 = `ChangeState` 传进来的那个（`CardHighlightAnimTime` 0.1 或 **0 = 瞬切**）；
                //   **alpha == 0 ⇒ 补间走完才 `ToggleFrames(false)`** = **淡出**（不是立刻消失），
                //   状态一进来先 `ToggleFrames(true)` —— 我们这里就是 `enabled = true`。
                _rim.enabled = true;
                ApplyRimColor(fc, CardHighlight.TweenTimeOf(s));
            }
        }

        Color _rimTarget = new Color(0f, 0f, 0f, 0f);
        Color _rimNow = new Color(0f, 0f, 0f, 0f);
        DG.Tweening.Tween _rimTween;

        /// <summary>把那圈状态环补到目标色（**时长照原版的逐态表**；alpha 到 0 就是「关」，补间走完才真关）。</summary>
        void ApplyRimColor(Color target, float time)
        {
            if (_rim.sharedMaterial == null) return;
            target.a *= CardHighlight.RimAlphaScale;   // 原版是「渲染器色 × 材质常量」，我们只有一个槽（见那个常量的注释）
            if (SameColor(_rimTarget, target))
            {
                // 目标没变 ⇒ **不重开补间**：刷新比补间快得多，重开会永远推不动、停在起点
                //（同 `SetOutline` 那条踩过的教训）。只在「本来就是不亮」时补一下关。
                if (target.a <= 0f && (_rimTween == null || !_rimTween.IsActive())) _rim.enabled = false;
                return;
            }
            _rimTarget = target;
            if (_rimTween != null && _rimTween.IsActive()) _rimTween.Kill();
            if (time <= 0f)
            {
                // 原版 state 2 `selected` / state 5 `displayingActiveAbility` 走的就是这一支（**瞬切**）
                _rimNow = target;
                WriteRim(_rimNow);
                if (target.a <= 0f) _rim.enabled = false;
                return;
            }
            var from = _rimNow;
            var tw = DOTween.To(() => 0f, v => { _rimNow = Color.Lerp(from, target, v); WriteRim(_rimNow); }, 1f, time);
            // 原版那条判据在**补间收尾那一帧**（`ChangeFrameColor` 的 AppendCallback）：alpha == 0 ⇒ 关掉那一层
            tw.OnComplete(() => { if (_rimTarget.a <= 0f) _rim.enabled = false; });
            _rimTween = CardTween.Use(tw, Ease.Linear, this);
        }

        /// <summary>写进那圈的颜色。这个 shader **没有 `_Color`** ⇒ 走 `_Outline`；兜底那条读 `color`。</summary>
        void WriteRim(Color c)
        {
            var m = _rim.sharedMaterial;
            if (m == null) return;
            if (m.HasProperty("_Outline")) m.SetColor("_Outline", c);
            else m.color = c;
        }

        /// <summary>那圈状态环的**位置与尺寸**（照原版的 `MinionLight`）。
        ///
        /// 🔴 **2026-09-29 照原版摆**（原来是我们自己定的：摆在卡中心、1.09×1.06 —— 那是拿 **2D 卡**量的）。
        ///  · 原版那层 = `3DBody/MinionLight`（**和 `Card 3D` 平级的兄弟**，活在**未缩放**的 3DBody 空间里）：
        ///    `localPos (−0.001, 1.326, 0)` · `localScale (3.1555, 3.0920, 2.99)`；
        ///    sprite `Card board frame SDF` = **79×107 @PPU100**（0.79 × 1.07 世界单位）
        ///    ⇒ **实绘 2.4928 × 3.3084**。
        ///  · **它圈的是 3D 卡体**（不是 2D 卡）：那个网格在 3DBody 空间里 y `[0.011, 2.6336]`、宽 2.09
        ///    ⇒ 环比卡体四周各宽 **0.20（左右）/ 0.33–0.35（上下）** = 两个方向都约 10%，这才是一圈**光晕**。
        ///    旁证两条：`SortingOrder = −1`（排在卡体之后）；shader 的 **`zTest = LEqual`、`zWrite = 0`**
        ///    （实读 pass 状态）⇒ 环被卡体挡住中间、**只在轮廓外沿露出来**（这也解释了它为什么要 `SortingOrder = −1`）。
        ///  · ⚠️ 我们的 `body3D` 是**底边对齐卡底**的（`localPosition = −Height/2`）⇒ 3D 卡体的中心在
        ///    **卡中心下方 0.3397**（`From3DBody(−0.001, 1.326, 0)`）；与「网格中心 −0.343」差 **0.003**
        ///    —— **两条独立路径互证**（原版节点坐标 vs 我们自己网格的包围盒）。
        ///  · ⚠️ **手牌没有 3D 卡体**（原版那一档是 `2DCard` 那套壳）⇒ 手牌这一档保持**居中**（我们的 2D 卡就是居中的）。
        ///    **如实标注**：原版只有一个固定 transform，我们按「环圈住**看得见的那张卡**」分两档 ——
        ///    **这是我们按实情收的口径**（同族取舍见 `CardDisplayWindow.ContainsPointer`）。
        ///  · `z = +0.05`：相机看 +Z、**z 越大越远** ⇒ 这一档是**贴在那张卡后面**（原版靠 SortingGroup 的
        ///    `SortingOrder = −1`，我们两层都是 MeshRenderer、只能用 z）。</summary>
        void PlaceRim(Transform t)
        {
            bool board = _faceMode == CardFace.Board;
            // 原版那一层的实绘尺寸 = sprite(0.79 × 1.07) × `MinionLight.localScale`(3.1555 / 3.0920)
            const float RingW = 0.79f * 3.1555f, RingH = 1.07f * 3.0920f;     // 2.4928 × 3.3084
            t.localPosition = board ? From3DBody(-0.001f, 1.326f, 0f) + new Vector3(0f, 0f, 0.05f)
                                    : new Vector3(0f, 0f, 0.05f);
            t.localScale = board ? new Vector3(RingW / Width, RingH / Height, 1f)   // 网格是 `Quad()`（= 卡大小）
                                 : new Vector3(1.09f, 1.06f, 1f);
        }

        /// <summary>自检用：那圈现在的**尺寸**（原版 `MinionLight` 那档 = 2.4928 × 3.3084）。</summary>
        public Vector2 RimSize { get { return _rim != null ? new Vector2(_rim.transform.localScale.x * Width,
                                                                        _rim.transform.localScale.y * Height) : Vector2.zero; } }
        /// <summary>自检用：那圈的中心相对卡根的位置。</summary>
        public Vector3 RimLocalPos { get { return _rim != null ? _rim.transform.localPosition : Vector3.zero; } }

        static readonly Color TintNormal = Color.white;
        public void ResetTint() { SetTint(TintNormal); }

        /// <summary>自检用：状态环那层现在**开着吗**（原版 `regular`/`Playable`/`Unplayable` 都是关的）。</summary>
        public bool RimVisible { get { return _rim != null && _rim.enabled; } }
        /// <summary>自检用：状态环那层用的 **shader 名**（应当是原版那个 `Everguild/FX/Card Highlight And Shadow`）。</summary>
        public string RimShaderName
        {
            get
            {
                var m = _rim != null ? _rim.sharedMaterial : null;
                return (m != null && m.shader != null) ? m.shader.name : null;
            }
        }
        /// <summary>自检用：状态环那层贴的**图名**（应当是原版那张 `Card board frame SDF`）。</summary>
        public string RimTexName
        {
            get
            {
                var m = _rim != null ? _rim.sharedMaterial : null;
                return (m != null && m.mainTexture != null) ? m.mainTexture.name : null;
            }
        }
        /// <summary>自检用：状态环现在的**颜色**（这个 shader 没有 `_Color` ⇒ 读 `_Outline`；兜底那条读 `color`）。</summary>
        public Color RimColor
        {
            get
            {
                var m = _rim != null ? _rim.sharedMaterial : null;
                if (m == null) return Color.clear;
                return m.HasProperty("_Outline") ? m.GetColor("_Outline") : m.color;
            }
        }

        // ==================================================================
        //  原版 `Minion Death Icon` —— 「这一下会打死它」的预览图标（🆕 2026-09-29）
        // ==================================================================
        //
        // 判据（原版实读，出处 `资料/待办判据_战场与战斗视图.md` §8b）：
        //  · 触发 = `CardHighlight.ToggleCombatPreviewHighlight`：**预览伤害够致死**时
        //    `Animation.Play("Minion Death Icon")` + `minionWillDieObject.SetActive(1)`；否则关。
        //  · 那一层挂在 `3DBody` 下（**和 `Card 3D` 平级**，3DBody 空间 scale 全 1）
        //    · localPos **(0, 1.710, −0.128)** · sprite **127×180 @PPU100 = 1.27 × 1.8 世界单位**
        //    （原版 `SpriteRenderer.m_Size` 逐位相同）· 材质内建 `Sprites-Default` · `m_SortingOrder = 2` · 色白。
        //  · 那条 clip（legacy · 60fps · 不循环 · 曲线走到 **0.25 s**）：
        //    `m_Color.a` **0 → 1**，**外加一条 `m_PositionCurves`：y 1.7100 → 2.8380**（上浮 1.128）。
        //    ⚠️ 曲线里 `z = −0.2`（≠ Transform 序列化的 −0.128）—— **播放期间以曲线为准**，所以本节按 −0.2 用。
        //  ⇒ 这一层 = **淡入 + 向上飘**（原写「就是个淡入」只翻了 `m_FloatCurves`，2026-09-29 更正）。
        //  ⚠️ **只在场上的卡上调用** —— 原版也只有棋盘单位有它（手牌那条路没接，见 §8b 的「要查清的那条」）。
        static readonly Vector3 WillDieFrom = From3DBody(0f, 1.7100f, -0.2f);
        static readonly Vector3 WillDieTo   = From3DBody(0f, 2.8380f, -0.2f);
        /// <summary>那条 clip 的曲线长度（`m_StopTime` 是 1.0，但两条曲线都只走到 0.25 s）。</summary>
        const float WillDieAnimTime = 0.25f;
        GameObject _willDie;
        MeshRenderer _willDieMr;
        DG.Tweening.Tween _willDieTween;

        /// <summary>原版 `minionWillDieObject.SetActive(...)` —— `on` = 淡入 + 上浮；关是**瞬时**的
        /// （原版关的那支只 `SetActive(0)`，没有第二条 clip）。</summary>
        public void SetWillDie(bool on)
        {
            if (!on)
            {
                if (_willDie != null && _willDie.activeSelf)
                {
                    if (_willDieTween != null && _willDieTween.IsActive()) _willDieTween.Kill();
                    _willDieTween = null;
                    _willDie.SetActive(false);
                    _willDie.transform.localPosition = WillDieFrom;
                }
                return;
            }
            if (_willDie == null) BuildWillDie();
            if (_willDie == null) return;          // 图取不到 —— `BuildWillDie` 已经出过声（不静默）
            if (_willDie.activeSelf) return;       // 已经亮着 —— **不重播**（刷新比补间快，重播会永远停在起点）
            _willDie.SetActive(true);
            _willDie.transform.localPosition = WillDieFrom;
            var mr = _willDieMr;
            if (mr != null && mr.sharedMaterial != null) mr.sharedMaterial.color = new Color(1f, 1f, 1f, 0f);
            if (_willDieTween != null && _willDieTween.IsActive()) _willDieTween.Kill();
            float t = 0f;
            var tw = DOTween.To(() => t, v =>
            {
                t = v;
                _willDie.transform.localPosition = Vector3.Lerp(WillDieFrom, WillDieTo, v);
                if (mr != null && mr.sharedMaterial != null) mr.sharedMaterial.color = new Color(1f, 1f, 1f, v);
            }, 1f, WillDieAnimTime);
            _willDieTween = CardTween.Use(tw, Ease.Linear, this);
        }

        void BuildWillDie()
        {
            var tex = CardArt.WillDieIcon();
            if (tex == null)
            {
                Debug.LogWarning("[CardView] 「这一下会打死它」那层：取不到贴图 "
                               + "`Resources/Art/ui/Minion_Death_Icon.png`（`Resources/Art/` 整个在 `.gitignore` 里 "
                               + "⇒ 新克隆没有）⇒ **整层不建**。重建：把 `素材/Warpforge原版/UI图集/图集/"
                               + "battleatlasui/sliced/Minion_Death_Icon.png` 拷成那个路径即可");
                return;
            }
            _willDie = new GameObject("Minion Death Icon");
            // ⚠️ 挂在**卡根**下 —— 我们的卡根就是原版 `3DBody` 的等价物（见 `From3DBody` 与
            //    `CanActParticles` 那段注释：挂到 `_body3D` 底下会**多乘一次 0.88586**）。
            _willDie.transform.SetParent(transform, false);
            _willDie.transform.localPosition = WillDieFrom;
            _willDie.transform.localRotation = Quaternion.identity;
            // `QuadMesh()` 是 Unity 内置 **1×1** 的 Quad ⇒ localScale 直接就是世界尺寸（原版 `m_Size`）
            _willDie.transform.localScale = new Vector3(1.27f, 1.8f, 1f);
            _willDie.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            _willDieMr = _willDie.AddComponent<MeshRenderer>();
            var m = new Material(BaseMaterial()) { name = "Minion Death Icon" };
            m.mainTexture = tex;
            m.color = new Color(1f, 1f, 1f, 0f);
            _willDieMr.sharedMaterial = m;
            _willDieMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _willDieMr.receiveShadows = false;
            _willDie.SetActive(false);
            // 🔴 **不进 `_layers`** —— 那是「整卡着色」名单（高亮/置灰/淡出都会遍历它），
            //    而这层的颜色是它自己按 clip 补间说了算的，进去会被 `ApplyTint` 刷成卡的颜色。
            //    同族的坑见 `CLAUDE.md` 铁律 10 第 4 条（残骸体、未行动绿光两棵树也是这么处理的）。
        }

        /// <summary>自检用：那层现在**亮着吗**。</summary>
        public bool WillDieVisible { get { return _willDie != null && _willDie.activeSelf; } }
        /// <summary>自检用：那层的贴图名（应当是 `Minion_Death_Icon`）。图取不到时是 null。</summary>
        public string WillDieTexName
        {
            get
            {
                var m = _willDieMr != null ? _willDieMr.sharedMaterial : null;
                return (m != null && m.mainTexture != null) ? m.mainTexture.name : null;
            }
        }
        /// <summary>自检用：那层的 alpha（淡入到 1 = 补间跑完）。</summary>
        public float WillDieAlpha
        {
            get
            {
                var m = _willDieMr != null ? _willDieMr.sharedMaterial : null;
                return m != null ? m.color.a : 0f;
            }
        }
        /// <summary>自检用：那层**当前**的卡根局部坐标（起止见 `WillDieFrom` / `WillDieTo`）。</summary>
        public Vector3 WillDieLocalPos { get { return _willDie != null ? _willDie.transform.localPosition : Vector3.zero; } }
        /// <summary>自检用：那层的**尺寸**（原版 `m_Size` = 1.27 × 1.8 世界单位）。</summary>
        public Vector3 WillDieLocalScale { get { return _willDie != null ? _willDie.transform.localScale : Vector3.zero; } }
        /// <summary>自检用：原版那条 clip 的**起点/终点**（3DBody 坐标已换算到卡根）。</summary>
        public static Vector3 WillDieFromForCheck { get { return WillDieFrom; } }
        public static Vector3 WillDieToForCheck { get { return WillDieTo; } }

        // ==================================================================
        //  合法目标的底光（原版 `Highlight` / `Highlight ranged`）
        // ==================================================================

        /// <summary>底光直径 ÷ 卡宽。算式见 `SetTargetGem`</summary>
        const float GlowOfCard = 1.13f / 2.09f;      // ≈ 0.54

        /// <summary>底光的 z：夹在卡框（0）和数值层（-0.02）之间 —— 即**画在数字后面**</summary>
        const float GlowZ = -0.01f;

        ImageQuad _glowMelee, _glowRanged;
        bool _glowTried;

        /// <summary>
        /// 点亮某颗攻击数值格的底光 —— 「这个单位**能被选中**」。
        ///
        /// 出处：原版卡预制体 `Base Attack Counters/{Melee,Range} Attack Container/` 下各挂一个
        /// `Highlight`（图 `40K_melee_glow`，红）/ `Highlight ranged`（图 `40K_ranged_glow`，紫），
        /// 3D 版 `HighlightAttackType.highlightObject` 指向它们。
        /// ⚠️ **锚点是钉死的**（四条独立证据）：它在**卡体那颗攻击数值格上**，与数字同点同心、
        ///    `SortingOrder` 比数字小（所以画在数字**后面**）、旋转 identity 且等比缩放。
        ///    **不是**单位脚下的地板（同 prefab 里真贴地的东西必须翻 -90°X），也**不是** 2D 印刷卡面
        ///    那颗（那条链是 RectTransform + CanvasRenderer，而且运行时 32 个 UI 版容器
        ///    `highlightObject` 全是 0 —— 根本没接光圈）。
        ///
        /// 尺寸：sprite 128 px ÷ PPU 100 = 1.28（原版卡体空间）× `m_LocalScale` 0.655 = **0.84**；
        /// 点亮瞬间再 × `scaleModifierOnHighLight` **1.35** = 1.13。卡体宽 `Card 3D` = 2.09
        /// → **1.13 / 2.09 = 54% 卡宽**。
        ///
        /// ⚠️ **技能没有对应的那一档** —— 原版就两个 `Highlight`（近战/远程），所以放技能时不点亮底光，
        ///    靠准星和弧线的金色来表达。
        ///
        /// 没有图（删掉美术目录）时什么都不做 —— 和 `CardArt.Available` 一个路子。
        /// </summary>
        public void SetTargetGem(TargetGem gem)
        {
            CurrentGem = gem;
            if (gem != TargetGem.None) EnsureGlow();
            if (_glowMelee != null) _glowMelee.gameObject.SetActive(gem == TargetGem.Melee);
            if (_glowRanged != null) _glowRanged.gameObject.SetActive(gem == TargetGem.Ranged);
        }

        /// <summary>现在点亮的是哪颗数值格（自检断言用 —— 截图上看不出「点亮的是不是该亮的那颗」）</summary>
        public TargetGem CurrentGem { get; private set; }

        void EnsureGlow()
        {
            if (_glowTried) return;
            _glowTried = true;
            float dia = Width * GlowOfCard;
            _glowMelee = MakeGlow("40K_melee_glow", MeleeAt, dia, "glow_melee");
            _glowRanged = MakeGlow("40K_ranged_glow", RangedAt, dia, "glow_ranged");
        }

        ImageQuad MakeGlow(string artName, Vector2 at, float dia, string name)
        {
            var q = ImageQuad.Create(transform, CardArt.Ui(artName), Vector3.zero, dia,
                                     new Vector2(0.5f, 0.5f), name);
            if (q == null) return null;
            // `at` 是卡面归一化坐标（x 从左、y 从**顶部**），换成卡的局部坐标
            q.transform.localPosition = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height, GlowZ);
            q.gameObject.SetActive(false);
            return q;
        }

        /// <summary>世界坐标在不在这张卡上。**用卡自己的局部坐标判** —— 扇形里每张卡都带旋转，
        /// 拿屏幕空间的包围盒判会错。</summary>
        public bool Contains(Vector3 world)
        {
            var l = transform.InverseTransformPoint(world);
            return Mathf.Abs(l.x) <= Width * 0.5f && Mathf.Abs(l.y) <= Height * 0.5f;
        }

        // ==================================================================
        //  数值区的命中（给悬停 tooltip 用）
        // ==================================================================
        // 原版是**每个数值容器上挂一个 `EverguildTooltipTrigger`**
        // （`Health Container` / `Range Attack Container` / `Melee Attack Container` / `Cost Container`，
        //  见 `assets_full/bundle_staticgeneralassets_assets_all/GameObject/`），
        // 触发器的 rect 就是那个容器的 rect（节点树 `0.4×0.4` 卡单位）。
        // 我们这套世界空间 quad 没有 uGUI 的射线靶 ⇒ 用「点到数值中心的距离」判，
        // 半径 `StatHitR` 取 0.4 卡单位的一半多一点（**这是我们挑的**，原版是容器 rect）。

        /// <summary>数值标识（和 <see cref="StatAt"/> 的返回值一一对应）。</summary>
        public const int StatNone = 0, StatMelee = 1, StatRanged = 2, StatArmour = 3, StatHealth = 4, StatCost = 5;
        const float StatHitR = 0.22f;      // ⚠️ 我们挑的（见上面那段的说明）

        /// <summary>世界坐标压在卡的哪个数值上（<see cref="StatNone"/> = 没压上）。
        /// ⚠️ 场上那套（`CardFace.Board`）用的是**另一组位置**（`BoardMeleeAt/…`），
        /// 和画数值时是**同一份判据**，别在这里换一套。</summary>
        public int StatAt(Vector3 world)
        {
            var l = transform.InverseTransformPoint(world);
            bool board = _faceMode == CardFace.Board;
            if (HitAt(l, board ? BoardMeleeAt  : MeleeAt))  return StatMelee;
            if (HitAt(l, board ? BoardRangedAt : RangedAt)) return StatRanged;
            if (HitAt(l, board ? BoardArmourAt : ArmourAt)) return StatArmour;
            if (HitAt(l, board ? BoardHealthAt : HealthAt)) return StatHealth;
            if (HitAt(l, CostAt))                           return StatCost;
            return StatNone;
        }

        /// <summary>
        /// 🆕 2026-09-21：世界坐标压在哪一枚**关键词**上（返回它的规范键；没压上返回 null）。
        /// 判据**不在我们这边** —— 转发 `TmpFont.LinkAt`（TMP 自己的 `FindIntersectingLink`）。
        /// 卡面里带 `&lt;link>` 的只有**关键词段**（`CardText.KeywordSegment` 给每一项套了 `&lt;link=规范键>`），
        /// 由 `TipText.Trait` 出文案（原版同一条链：`GameStaticData__TraitNameToString.c:84-109`
        /// 套 link → `TextTooltipController__GetTraitTooltip.c:30` 命中）。
        /// ⚠️ **两层守卫**：① `_keywords` 没建 / 是空的 ⇒ null；
        ///    ② 那层**现在没显示**（场上那套 `CardFace.Board` 把整个 `2DCard` 关掉了，
        ///    只剩攻血甲 + 徽标）⇒ 也要返回 null —— TMP 的 `textInfo` 在对象关掉之后**还在**，
        ///    不判这一下会「鼠标划过一张看不见的字也弹 tooltip」。
        /// </summary>
        public string LinkAt(Vector3 world, Camera cam)
        {
            if (_keywords == null || string.IsNullOrEmpty(_keywords.text)) return null;
            if (!_keywords.gameObject.activeInHierarchy) return null;
            return TmpFont.LinkAt(_keywords, world, cam);
        }

        /// <summary>🆕 2026-09-21：关键词那层文字的**锚点**（摆 trait tooltip 用）。
        /// 原版 tooltip 摆在**触发器自己**的位置上、**不跟鼠标**（见 `资料/tooltip_原版规格与实现.md` §一），
        /// 而那条链的触发器就是**那段描述文字**本身（`TextTooltipController` 挂在它上面）
        /// ⇒ 我们取这一层的 transform 位置，口径与之一致。没建那层就退回卡本体。</summary>
        public Vector3 KeywordAnchor
        {
            get { return _keywords != null ? _keywords.transform.position : transform.position; }
        }

        /// <summary>🆕 2026-09-21：关键词那层文字在**世界坐标**里的包围盒（自检扫 `&lt;link>` 用）。
        /// 直接从 TMP 自己的 `textBounds` + transform 算 —— **不另算一份版面**
        /// （「那一段字画在哪儿」只有 TMP 知道；自己算第二份迟早不一致）。没那层返回 false。</summary>
        public bool KeywordRect(out Vector3 center, out Vector2 halfSize)
        {
            center = Vector3.zero; halfSize = Vector2.zero;
            if (_keywords == null) return false;
            var b = _keywords.textBounds;                 // 本地空间
            center = _keywords.transform.TransformPoint((Vector3)b.center);
            halfSize = new Vector2(
                _keywords.transform.TransformVector(new Vector3(b.extents.x, 0f, 0f)).magnitude,
                _keywords.transform.TransformVector(new Vector3(0f, b.extents.y, 0f)).magnitude);
            return halfSize.x > 0f && halfSize.y > 0f;
        }

        /// <summary>那个数值在世界坐标的哪儿（摆 tooltip 用；口径与 <see cref="StatAt"/> 同一份）。</summary>
        public Vector3 StatWorld(int stat)
        {
            bool board = _faceMode == CardFace.Board;
            Vector2 at;
            switch (stat)
            {
                case StatMelee:  at = board ? BoardMeleeAt  : MeleeAt;  break;
                case StatRanged: at = board ? BoardRangedAt : RangedAt; break;
                case StatArmour: at = board ? BoardArmourAt : ArmourAt; break;
                case StatHealth: at = board ? BoardHealthAt : HealthAt; break;
                case StatCost:   at = CostAt; break;
                default: return transform.position;
            }
            // 归一化坐标 y 从**顶部**数（和 `CostAt` 那批同口径）⇒ 局部 y 要翻过来
            return transform.TransformPoint(new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height, 0f));
        }

        bool HitAt(Vector3 local, Vector2 at)
        {
            var c = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height, 0f);
            return (local - c).sqrMagnitude <= StatHitR * StatHitR;
        }

        /// <summary>摆位：位置(世界) + 绕 Z 的倾角 + 缩放。手牌扇形/战场落位都调它。
        /// ⚠️ 缩放**不直接写 `localScale`** —— 高亮那层会在基础缩放上乘一个系数，见 `ApplyScale`。</summary>
        /// <param name="clip">🆕 2026-10-08（A181）：**可选**视口裁切（画布像素 · 左上原点）。
        /// 传了就用它当这张卡的裁切（`null` = **不改**当前状态：已经裁着的继续按老矩形重裁 ——
        /// 位姿变了要重算，见下面「视口裁切」那一大段）。</param>
        public void SetPose(Vector3 pos, float rotZ, float scale, PxRect? clip = null)
        {
            transform.localPosition = pos;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            _baseScale = scale;
            ApplyScale();
            // 🆕 2026-09-25：影子是**贴在地上**的（世界位置 = 卡心 x/z + 固定 y），不是普通子节点
            // ⇒ 卡一动就要重算一遍。⚠️ **批处理下没有帧循环**，`BlobShadow.Update` 不会跑，
            //    所以这里必须显式来一下（和 `Reflow`/`RefreshAll` 那几处同一个道理）。
            if (_blobShadow != null) _blobShadow.Sync();
            if (clip.HasValue) { _clip = clip; _clipFromTree = false; }
            // 🔴 判据是 **`Clipping`**（不是 `_clipOn`）—— 第一次带裁切摆位时 `_clipOn` 还是 false
            //    （它由 `ApplyClip` 里那一句置上），写成 `if (_clipOn)` 会让**第一次的裁切永远不生效**。
            //    位姿是「画布矩形 → 卡的局部系」那个换算的输入 ⇒ 裁着的卡每次摆位都要重裁。
            //    🆕 2026-10-13（A435 辛 · A774）：显式矩形那一档原样（形参非空 ⇒ 形参赢、顺手关掉「跟随节点」）；
            //       **传 `null` 仍是「不改当前状态」**（跟随节点那一档就靠它保住 —— ⛔ 别把这一句改成无条件赋值）。
            if (Clipping) ApplyClip();
        }

        // ==================================================================
        //  视口裁切（`RectMask2D` 的等效物）—— 🆕 2026-10-08（A181）
        // ==================================================================
        //
        // 🔴 **为什么要有它**：收藏窗那几个视口在原版都挂着 `RectMask2D`（逐节点实读 ——
        //    `bundle_menus_assets_all`：卡池 `…/CardsTab/Collection Display/Scroll View/Viewport`、
        //    异画 `…/Alternate Art Tab/…/Scroll View/Viewport`、卡背 `…/Cardback Tab/Cardback Display/Scroll View/Viewport`、
        //    卡组 `…/Decks Tab/…/Deck Scroll View/Viewport` **四处** —— `m_Softness=(0,0) · m_Padding=(0,0,0,0)`，
        //    即**硬边、不内缩**），而那几处只把 `Clip` 设给了**窗口自己的绘图助手**
        //    （`MenuWindowBase.Rect/Text/Nine/AddHit` 那一族）—— `CardView` 的层是**自建网格**，看不见那个 `Clip`
        //    ⇒ **部分可见的卡整张画出去**（实测：卡池往下滚 192px，第一排卡的上半截画到视口上沿 155.9 以上，
        //    压在页头那条空带上；原版是裁掉的）。
        //
        // **两条判据（照它们实现）**：
        //   · **图的裁法** = `MenuDraw.Rect` / `ClipNineChildren` 那一套：与框求交 → 几何缩到交集、
        //     **uv 按同一段比例跟着截**（只缩几何不截 uv 会把图压扁，同 `ImageQuad.SetUvRect` 的注释）；
        //   · **字的裁法** = `MenuDraw.ClipTmpMesh` / `ClipQuad` 那一套：逐字把四角夹进框、uv 按同一仿射改。
        // ✅ **2026-10-11（A250）已收口**：字那一半**不再有第二份实现** —— 本文件的 `ClipTextMesh` 现在
        //    **转调** `MenuDraw.ClipTmpMesh(tmp, clip, Vector2.zero)`（那份已公开成公共件），本地那份
        //    `ClipQuad` 副本**已删除**。留给本处的只有「未激活不裁 / 先 `ForceMeshUpdate` / 记 `_clippedTexts`」
        //    这三件卡自己的事（判据都在 `ClipTextMesh` 的注释里）。
        //    ⚠️ **图那一半仍是本文件自己的**（`CropLayer` + `IsRectQuad` + `RemapChannel`）——
        //    它不是 `MenuDraw` 那套的副本：那边是「建的时候按框求交」（`Rect`/`ClipNineChildren`），
        //    本处是「**已经建好的**网格按当前位姿重裁 + 还原」，判据同源但形状不同。
        //
        // ⚠️ **口径与边界（如实写）**：
        //   · 只在**卡在画布上轴对齐**时逐位等价（`rotZ = 0` 且 `AutoCardRotation` 倾摆为 0）——
        //     收藏窗那几页的卡**静止**（倾摆只在**位移**时才有，见 `AutoCardRotation` 文件头）⇒ 成立。
        //     转过（扇形角/倾摆）的卡**还没做**：那时视口矩形在卡的局部系里是**斜的**，这里取的是它的
        //     外接矩形 ⇒ 会**多画一点**（越界方向上不裁到底）。检测到旋转会**出声一次**（不静默）。
        //   · 只裁 `MeshRenderer`（且**跳过 `ImageQuad`** —— 它自己会 `RebuildMesh`，是战斗里光环/描边那一族，
        //     收藏窗不建它们）与 TMP 文字。
        //   · 网格只支持 **4 顶点、轴对齐矩形**的 quad（本卡每一层都是：卡框/立绘/数值/宝石/SDF 影）。
        //     别的网格（3D 卡体那种）**只挡「整块进出」**，部分越界时**出声一次不裁**（⛔ 不静默）。
        //   · **代价**：每设一次都会重裁一遍（网格重建成品 + TMP 各 `ForceMeshUpdate` 一次）。
        //     收藏窗一张卡从 `Create` 到 `SetData` 会走 2~3 次（`SetClip` 放最后 = 1 次）；
        //     若将来这条路变成每帧热点，再按「只在最后一次设」收口（本件没做）。
        /// <summary>当前裁切矩形（画布像素 · 左上原点；`null` = 不裁）。
        /// ⚠️ 「跟随视口节点」那一档（<see cref="SetClipFromTree"/>）下它是**上一次解析出来的快照** ——
        /// 下一次重裁（`SetPose` / `SetData` / `SetFace` 的尾巴、或再调一次 `SetClipFromTree`）会刷新它。</summary>
        public PxRect? Clip { get { return _clip; } }
        /// <summary>现在这张卡**被裁着**吗（自检用；`SetClip(null)` 之后是 false）。</summary>
        public bool Clipped { get { return _clipOn; } }

        PxRect? _clip;
        bool _clipFromTree;      // 🆕 A774：裁切边界**每次重裁都沿父链重新解析**（不是上面那个裸矩形）
        bool _clipOn;            // 已经裁过一次 ⇒ 之后位姿/数据/形态一变就得重裁
        bool _clipWarned;        // 「转过 / 非四顶点 / 非矩形」那几种**只响一次**
        static int _softnessWarned;   // 节点带软边那一档**只响前 3 次**（限流；同 `MenuDraw._paddedClipDegenerates`）

        /// <summary>这张卡「**当前有裁切要维护**」吗 —— 显式矩形（`_clip`）或**跟随视口节点**（`_clipFromTree`）
        /// 任一条成立。🔴 三个重裁尾巴（`SetPose` / `SetData` / `SetFace`）看的是**它**，⛔ 不是 `_clip.HasValue`：
        /// 后者在「已开跟随、但这一刻父链上没有节点」时会漏掉后面挂上来的节点（**静默**）。</summary>
        bool Clipping { get { return _clipFromTree || _clip.HasValue; } }

        /// <summary>设 / 清**【显式覆盖】**的视口裁切（= `ViewportClip.Resolve` 的第 1 支：形参非空 ⇒ 形参赢）。
        /// 🔴 **它会把「跟随视口节点」那一档关掉**（<see cref="SetClipFromTree"/> 开的那个）——
        /// 与 `Resolve` 的优先级一致：显式覆盖 &gt; 父链上的节点 &gt; 不裁。
        /// 🔴 **推荐的调用时机 = 「位姿摆好、数据灌好之后」的最后一步**（本类只在**设过一次之后**
        /// 才会自己重裁：`SetPose` / `SetData` / `SetFace` 的尾巴各挂了一次）。</summary>
        public void SetClip(PxRect? clip) { _clipFromTree = false; _clip = clip; ApplyClip(); }

        /// <summary>🔴 **2026-10-13（A435 辛 · A774）：「裁切边界沿父链解析」那一档**（= 宿主不再自己算视口矩形）。
        ///
        /// <para>**为什么要有它**：本类原来的 `clip` 口是个**裸矩形**（`_clip`）—— 宿主必须把视口算好传进来，
        /// 于是视口节点上那份状态（框 / `padding` / `softness`）**长出了第二份**：节点改了，卡不跟。
        /// 而「直接传 `null`」**表达不了「跟节点走」** —— `SetPose(…, null)` 的语义是
        /// 「**不改**当前状态」（见它那个形参的注释）⇒ 新建的卡会**整张不裁**（静默）。</para>
        ///
        /// <para>解析走**全壳唯一那份** `ViewportClip.Resolve`（形参传 `null` ⇒ 它沿父链找最近的
        /// `ViewportClip`，**含本卡自己那一级**）⇒ 与 `MenuDraw.Rect/Nine/Text/ClipText` 那几路
        /// **同一个口径、同一份状态**。⛔ 别在这里再写一套找节点的逻辑。</para>
        ///
        /// <para>⚠️ **找不到节点 ⇒ 不裁**（不是「退回旧矩形」）：= 这一处没有 `RectMask2D` 那一档。</para>
        /// <para>⚠️ **软边（`m_Softness`）本类不做**：我们的裁法是「按框切几何」，没有渐隐带
        /// ⇒ 节点 `softness` 非零时**出声**（限流），⛔ 不静默当硬边。</para>
        /// <para>⚠️ **重裁时机**：位姿 / 数据 / 形态一变会重新解析（`SetPose` / `SetData` / `SetFace` 的尾巴）；
        /// **只挪节点、不碰卡时不会自动跟** —— 本类没有帧循环、也没有「节点变了」的通知
        /// ⇒ 宿主批量改完节点状态（改 `padding` / 搬节点）后要**显式再调一次**本方法。</para></summary>
        /// <returns>`true` = 解析到了框并裁上了（= <see cref="Clipped"/>）。</returns>
        public bool SetClipFromTree() { _clipFromTree = true; ApplyClip(); return _clipOn; }

        /// <summary>一个被我们改过几何的层：记着**原件**，好还原 / 换裁切时从原件重裁。</summary>
        class LayerCrop
        {
            public MeshRenderer mr;
            public MeshFilter mf;
            public Mesh original;
            public Mesh cropped;
            public bool wasEnabled;
        }
        readonly List<LayerCrop> _crops = new List<LayerCrop>();
        readonly List<TextMeshPro> _clippedTexts = new List<TextMeshPro>();

        void ApplyClip()
        {
            RestoreClip();
            // 🔴 **2026-10-13（A435 辛 · A774）**：跟随节点那一档 ⇒ **每次重裁都重新解析**
            //    （节点后挂 / 搬走 / `padding` 改了都跟得上 —— 这正是加这一档的目的）。
            //    `_clip` 收的是 `RenderClip`（= 框 − `padding`；与 `MenuDraw.Rect/Text` 吃的**同一份**）：
            //    命中区那一份不吃 `padding`，而本类不做命中区（卡的点击区在宿主那边）。
            if (_clipFromTree)
            {
                var st = ViewportClip.Resolve(transform, null, Vector2.zero, default(Vector4));
                _clip = st.RenderClip;
                if (_clip.HasValue && (st.Softness.x > 0f || st.Softness.y > 0f) && _softnessWarned++ < 3)
                    Debug.LogWarning($"[CardView] 「{name}」所在的视口节点带 `softness`({st.Softness.x},{st.Softness.y})，"
                                   + "而本类只会**硬边切几何**（原版那个渐隐带我们还没做）⇒ 这一段按硬边裁"
                                   + "（**如实出声，不静默**）。");
            }
            _clipOn = _clip.HasValue;
            if (!_clip.HasValue) return;

            var clip = _clip.Value;
            // 🔴 **换算**：画布矩形 → 卡面那一层（`FaceRoot`）的局部系。
            //    卡在画布上轴对齐时它还是个轴对齐矩形 ⇒ 与「沿画布轴裁」逐位等价；
            //    转过的话取外接矩形（会多画一点）⇒ 出声一次。
            var fr = FaceRoot;
            if (!_clipWarned
                && (Quaternion.Angle(fr.rotation, transform.rotation) > 0.01f
                    || Quaternion.Angle(transform.rotation, Quaternion.identity) > 0.01f))
            {
                _clipWarned = true;
                Debug.LogWarning($"[CardView] 「{name}」**转过**（倾摆/扇形角 ≠ 0）却带裁切 —— "
                               + "当前这套裁切只在**轴对齐**时与 `RectMask2D` 等价（转过时取的是外接矩形 ⇒ 会多画一点）。"
                               + "要真裁转过的情况得走多边形裁剪，**还没做**（如实出声，不静默）。");
            }
            Vector3 a = fr.InverseTransformPoint(LayoutSpace.FromPixel(clip.x1, clip.y1));
            Vector3 b = fr.InverseTransformPoint(LayoutSpace.FromPixel(clip.x2, clip.y2));
            float lx1 = Mathf.Min(a.x, b.x), lx2 = Mathf.Max(a.x, b.x);
            float ly1 = Mathf.Min(a.y, b.y), ly2 = Mathf.Max(a.y, b.y);
            if (lx2 - lx1 < 1e-4f || ly2 - ly1 < 1e-4f)
            {
                // 退化矩形：原版 `RectMask2D` 那个框宽/高为 0 时**一个像素都不画** ⇒ 全关
                var all = GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null) continue;
                    _crops.Add(new LayerCrop { mr = all[i], wasEnabled = all[i].enabled });
                    all[i].enabled = false;
                }
                return;
            }

            var mrs = GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < mrs.Length; i++) CropLayer(mrs[i], lx1, lx2, ly1, ly2);
            var tts = GetComponentsInChildren<TextMeshPro>(true);
            for (int i = 0; i < tts.Length; i++) ClipTextMesh(tts[i], clip);
        }

        /// <summary>把上一次裁切的痕迹**全部还原**（网格换回原件、关掉的层恢复原状态、被裁过的字重排回原样）。
        /// ⚠️ 顺序无副作用：`RestoreClip` 之后 `_crops` / `_clippedTexts` 都是空的 ⇒ 可以随便重复调。</summary>
        void RestoreClip()
        {
            for (int i = 0; i < _crops.Count; i++)
            {
                var c = _crops[i];
                // ⚠️ **只还原还挂着我们那份网格的层** —— 期间若有别的写入点换过网格
                //    （`SetFace` 的护甲盾 / `SetBadgeIconTexture` / `SetData` 换卡框那种），
                //    无条件写回 `original` 会把**别人的**新网格盖掉（静默回到旧图）。
                if (c.mf != null && c.original != null && c.mf.sharedMesh == c.cropped) c.mf.sharedMesh = c.original;
                DestroyMesh(c.cropped);
                if (c.mr != null) c.mr.enabled = c.wasEnabled;
            }
            _crops.Clear();
            for (int i = 0; i < _clippedTexts.Count; i++)
            {
                var t = _clippedTexts[i];
                // 重排一次 = 把我们写进去的那一刀冲掉（TMP 会按当前文字重建整份网格）
                if (t != null) t.ForceMeshUpdate();
            }
            _clippedTexts.Clear();
        }

        /// <summary>只销毁我们建的网格（销毁对象时不需要「还原」—— 那些层级已经跟着没了）。</summary>
        void DisposeCrops()
        {
            for (int i = 0; i < _crops.Count; i++) DestroyMesh(_crops[i].cropped);
            _crops.Clear();
        }

        /// <summary>批处理下 `Object.Destroy` 不生效（同 `MenuWindowBase.DestroySafe` 那条）。</summary>
        static void DestroyMesh(Mesh m)
        {
            if (m == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) { Object.DestroyImmediate(m); return; }
#endif
            Object.Destroy(m);
        }

        /// <summary>一个层（`MeshRenderer` + `MeshFilter`）的裁切。判据见上面那一大段。
        /// ⚠️ 网格顶点就在**层自己的局部系**里（层只有 z 偏移、没有旋转缩放）⇒ 与 `FaceRoot` 的局部系
        /// 只差一个 z，x/y 可以直接比。</summary>
        void CropLayer(MeshRenderer mr, float lx1, float lx2, float ly1, float ly2)
        {
            if (mr == null) return;
            if (mr.GetComponent<ImageQuad>() != null) return;      // 自建 quad 自己会重建网格（见「边界」）
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            var mesh = mf.sharedMesh;
            var vs = mesh.vertices;
            if (vs == null || vs.Length < 3) return;

            float x1 = float.MaxValue, x2 = float.MinValue, y1 = float.MaxValue, y2 = float.MinValue;
            for (int i = 0; i < vs.Length; i++)
            {
                if (vs[i].x < x1) x1 = vs[i].x;
                if (vs[i].x > x2) x2 = vs[i].x;
                if (vs[i].y < y1) y1 = vs[i].y;
                if (vs[i].y > y2) y2 = vs[i].y;
            }
            // ① 整块在视口外 ⇒ 关掉（记**原来的** enabled，还原时别把它打开）
            if (x2 <= lx1 || x1 >= lx2 || y2 <= ly1 || y1 >= ly2)
            {
                _crops.Add(new LayerCrop { mr = mr, wasEnabled = mr.enabled });
                mr.enabled = false;
                return;
            }
            // ② 整块在视口内 ⇒ **一个字节都不动**（所以没被裁到的那几排卡与旧行为逐字相同）
            if (x1 >= lx1 && x2 <= lx2 && y1 >= ly1 && y2 <= ly2) return;
            // ③ 部分越界：只支持 4 顶点、轴对齐矩形、无顶点色的 quad（本卡每一层都是）
            if (vs.Length != 4 || !IsRectQuad(vs, x1, x2, y1, y2)
                || (mesh.colors != null && mesh.colors.Length == vs.Length)
                || (mesh.uv3 != null && mesh.uv3.Length == vs.Length)
                || (mesh.uv4 != null && mesh.uv4.Length == vs.Length))
            {
                WarnNotCroppable(mesh.name);
                return;
            }

            float cx1 = Mathf.Max(x1, lx1), cx2 = Mathf.Min(x2, lx2);
            float cy1 = Mathf.Max(y1, ly1), cy2 = Mathf.Min(y2, ly2);
            if (cx2 - cx1 < 1e-4f || cy2 - cy1 < 1e-4f)
            {
                _crops.Add(new LayerCrop { mr = mr, wasEnabled = mr.enabled });
                mr.enabled = false;
                return;
            }
            // 参数化：本块在自身矩形里的归一化坐标（左 0 / 右 1、下 0 / 上 1）
            float[] s = new float[4], tt = new float[4];
            for (int i = 0; i < 4; i++)
            {
                s[i] = (vs[i].x - x1) / (x2 - x1);
                tt[i] = (vs[i].y - y1) / (y2 - y1);
            }
            float sA = (cx1 - x1) / (x2 - x1), sB = (cx2 - x1) / (x2 - x1);
            float tA = (cy1 - y1) / (y2 - y1), tB = (cy2 - y1) / (y2 - y1);

            var nv = new Vector3[]
            {
                new Vector3(cx1, cy1, vs[0].z), new Vector3(cx2, cy1, vs[0].z),
                new Vector3(cx2, cy2, vs[0].z), new Vector3(cx1, cy2, vs[0].z),
            };
            float[] ns = { sA, sB, sB, sA }, nt = { tA, tA, tB, tB };

            var m = new Mesh { name = mesh.name + "_clip" };
            m.vertices = nv;
            // ⚠️ 最后一个实参 = **新网格的顶点数（4）**，不是「uv 是几维」——
            //    传 2 的话 `m.uv` 只有两个元素、和 4 个顶点对不上（Unity 当场报数组尺寸不符 ⇒ 网格作废）。
            m.uv = RemapChannel(mesh.uv, s, tt, ns, nt, 4);
            bool hasUv2 = mesh.uv2 != null && mesh.uv2.Length == 4;
            if (hasUv2) m.uv2 = RemapChannel(mesh.uv2, s, tt, ns, nt, 4);
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };      // 顶点序 BL·BR·TR·TL（与各层原件同序）
            m.RecalculateBounds();

            var rec = new LayerCrop { mr = mr, mf = mf, original = mesh, cropped = m, wasEnabled = mr.enabled };
            mf.sharedMesh = m;
            _crops.Add(rec);
        }

        /// <summary>4 个顶点是不是**一个轴对齐矩形的四个角**（每个角都落在 `{x1,x2} × {y1,y2}` 上、且四角齐）。
        /// 不是的话这套「矩形求交 + 按比例截 uv」就不成立 ⇒ 调用方只挡整块并出声。</summary>
        static bool IsRectQuad(Vector3[] vs, float x1, float x2, float y1, float y2)
        {
            int mask = 0;
            for (int i = 0; i < 4; i++)
            {
                bool left = Mathf.Abs(vs[i].x - x1) < 1e-5f, right = Mathf.Abs(vs[i].x - x2) < 1e-5f;
                bool bot = Mathf.Abs(vs[i].y - y1) < 1e-5f, top = Mathf.Abs(vs[i].y - y2) < 1e-5f;
                if (!(left || right) || !(bot || top)) return false;
                mask |= 1 << (((left ? 0 : 1)) + ((bot ? 0 : 1)) * 2);
            }
            return mask == 0xF;                            // 四个角都出现过（双线性插值才可解）
        }

        /// <summary>按**同一段相对几何**把一条 `uv2` 通道重采样到裁剩的那块上 —— 判据同 `MenuDraw.Rect`
        /// 那两行（uv 的 y 自下而上、而像素的 y 自上而下，所以这里全部用**归一化坐标**，不再翻）。
        /// 四角值先按 (s,t) 归到 `00/10/01/11` 四个槽上，再双线性插值（轴对齐矩形上等于线性映射 ✓）。</summary>
        static Vector2[] RemapChannel(Vector2[] src, float[] s, float[] t, float[] ns, float[] nt, int len)
        {
            var corner = new Vector2[4];
            for (int i = 0; i < 4; i++)
            {
                int idx = (s[i] > 0.5f ? 1 : 0) + (t[i] > 0.5f ? 2 : 0);
                corner[idx] = src[i];
            }
            var dst = new Vector2[len];
            for (int i = 0; i < len; i++)
            {
                float sA = Mathf.Clamp01(ns[i]), tA = Mathf.Clamp01(nt[i]);
                dst[i] = new Vector2(
                    (corner[0].x * (1 - sA) + corner[1].x * sA) * (1 - tA)
                        + (corner[2].x * (1 - sA) + corner[3].x * sA) * tA,
                    (corner[0].y * (1 - sA) + corner[1].y * sA) * (1 - tA)
                        + (corner[2].y * (1 - sA) + corner[3].y * sA) * tA);
            }
            return dst;
        }

        void WarnNotCroppable(string meshName)
        {
            if (_clipWarned) return;
            _clipWarned = true;
            Debug.LogWarning($"[CardView] 「{name}」的网格 `{meshName}` 不是 4 顶点轴对齐矩形 ⇒ "
                           + "**部分越界那一段没裁**（只挡了「整块在视口外」）。要真裁得给它做多边形裁剪，**还没做**"
                           + "（本卡每一层都是矩形 quad ⇒ 正常情况不会走到这里；出声是为了不静默）。");
        }

        /// <summary>把一段 TMP 的渲染网格裁进视口（硬边）—— 🆕 **2026-10-11（A250）起【转调】
        /// `MenuDraw.ClipTmpMesh`**（那是全工程唯一一份实现），本处只剩「卡自己需要的三件」：
        /// ① **未激活的 TMP 不裁**（原实现如此，见下面那句）；② **先重排拿一份新鲜网格**（`ForceMeshUpdate`）；
        /// ③ 记进 `_clippedTexts`（`RestoreClip` 靠重排把我们写进去的那一刀冲掉）。
        ///
        /// 🔴 **本处【不要】软边**：收藏窗那几个视口的 `m_Softness` 实测都是 `(0,0)`（硬边）
        /// ⇒ `softPx` 传 `Vector2.zero`（`MenuDraw.SoftAlpha` 在 `soft ≤ 0` 时恒 1 ⇒ 不削 alpha）。
        /// <para>🔴 **为什么必须先 `ForceMeshUpdate`**：重裁必须从**原件**出发（在已经夹过的网格上再夹 =
        /// 几何被夹第二次、而 uv 只按第一次的比例走 ⇒ 越裁越错，且**静默**）。这也让「设两次同样的裁切」幂等
        /// —— 公共件那边**不做**这件事（它的契约里写明「网格新不新鲜归调用方」）。</para>
        /// <para>⚠️ **保留「未激活就不裁」这道闸**：不保留的话，那些 TMP 上车（`MenuDraw.ClipTmpMesh` 的
        /// `MeshFilter.sharedMesh` 守卫）会数进 `TextClipUploadSkipped` 并**出声**—— 那是给「本该在渲染、
        /// 却没有渲染网格」那一档用的信号，卡这里**不是**那一档（卡本来就允许在未激活的页签里）。</para></summary>
        void ClipTextMesh(TextMeshPro tmp, PxRect clip)
        {
            if (tmp == null || !tmp.gameObject.activeInHierarchy) return;
            tmp.ForceMeshUpdate();
            if (MenuDraw.ClipTmpMesh(tmp, clip, Vector2.zero) && !_clippedTexts.Contains(tmp))
                _clippedTexts.Add(tmp);
        }

        void OnDestroy() { DisposeCrops(); }

        // ==================================================================
        //  高亮那一下的缩放（原版 `CardBodyToScale × ScaleFactor`，带补间）
        // ==================================================================
        //
        // 🔴 **2026-09-19 接上**（第 12 行：「放大 1.05 + 补间」那层原来一直没接）。
        //    原版：`ScaleFactor = 1.05` · 补间时长 `CardHighlightAnimTime = 0.1`（`Core/CardHighlight.cs`）。
        //    **做法必须是「基础缩放 × 高亮系数」**，不能直接改 `transform.localScale`：
        //    `SetPose`（手牌扇形重排 / 战场落位 / 切分辨率 `Relayout`）每次都把缩放按布局写死一遍，
        //    而布局**不知道**高亮开着 ⇒ 直接改会被下一次重排抹掉（6 条手牌断言 + `CardBaseDemo`
        //    的版面断言全盯着 `localScale`）。
        float _baseScale = 1f;
        float _hlMul = 1f;
        DG.Tweening.Tween _hlTween;

        void ApplyScale() { transform.localScale = Vector3.one * (_baseScale * _hlMul); }

        /// <summary>自检用：高亮系数（原版 `ScaleFactor` = 1.05；`selectedTargetInBoard` 也是这一档）。</summary>
        public float HighlightMul { get { return _hlMul; } }

        /// <summary>高亮的放大/还原（带 `AnimTime` 补间）。由 `SetHighlight` 调，外部一般不用直接调。</summary>
        public void SetHighlightScale(bool on)
        {
            float target = on ? CardHighlight.ScaleFactor : 1f;
            if (Mathf.Approximately(_hlMul, target)) return;
            if (_hlTween != null && _hlTween.IsActive()) _hlTween.Kill();
            float from = _hlMul;
            var t = DOTween.To(() => from, v => { from = v; _hlMul = v; ApplyScale(); },
                               target, CardHighlight.AnimTime);
            _hlTween = CardTween.Use(t, Ease.Linear, this);
        }

        // ---- 原版那圈高亮描边（SDF 层材质的 `_Outline`）----
        //
        // 判据与颜色 = `CardHighlight.OutlineOf`（**判据只留那一处**）；这里只管「怎么上屏 + 补间」。
        // ⚠️ **只在 2D 卡上画**（手牌 / 放大窗 / 卡组）：原版那层 SDF 属于 `2DCard`，
        //    而场上是 3D 卡体（原版把整张 `2DCard` 关掉）—— 我们场上**只留那层的影子**当落地感，不描边。
        Color _outlineTarget = new Color(0f, 0f, 0f, 0f);
        DG.Tweening.Tween _outlineTween;

        /// <summary>颜色比一下（描边只需要 &#34;变化了没有&#34; 这个粒度，不做逐位比较）</summary>
        static bool SameColor(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a) < 1e-4f;
        }

        /// <summary>设那圈描边的颜色（原版 `ChangeHighlightColor`，**补间 0.2s**；alpha 0 = 关）。</summary>
        public void SetOutline(Color c)
        {
            // 🔴 **目标没变就直接返回**（2026-09-19 实测踩到）：`BattleDriver.SyncHand` → `RefreshHandPlayable`
            //    在每次刷新（掉血/加 buff/每回合）都会调到这里。不判这一下的话，每一轮都是
            //    「杀掉旧补间 → 从**当前色**重新补间」，而刷新比补间快得多 ⇒ **补间永远推不动、停在起点**
            //    （表现：描边一直不亮，而判据、颜色、材质全都是对的）。
            if (SameColor(_outlineTarget, c)) return;
            _outlineTarget = c;
            var m = (_faceMode != CardFace.Board && _shadowLayer != null) ? _shadowLayer.sharedMaterial : null;
            if (m == null || !m.HasProperty("_Outline")) return;

            if (_outlineTween != null && _outlineTween.IsActive()) _outlineTween.Kill();
            var from = m.GetColor("_Outline");
            var to = c;
            float a = _alpha;
            var t = DOTween.To(() => 0f, k =>
            {
                var col = Color.Lerp(from, to, k);
                col.a *= a;                       // 卡整体淡出时描边跟着淡（不然卡没了圈还在）
                m.SetColor("_Outline", col);
            }, 1f, CardHighlight.OutlineAnimTime);
            _outlineTween = CardTween.Use(t, Ease.Linear, this);
        }

        // ==================================================================
        //  卡框的 UV 裁剪
        // ==================================================================

        // 原版卡框 PNG 是 1024×1024，**卡本体只占中间一块**，四周是透明留白。
        // 直接铺满 quad 的话卡会缩成一小块还偏 —— 所以要量出不透明包围盒，
        // 把 UV 裁到那一块。量一次按贴图缓存。
        static readonly Dictionary<Texture2D, Rect> _uvCache = new Dictionary<Texture2D, Rect>();
        static readonly Dictionary<Texture2D, Mesh> _meshCache = new Dictionary<Texture2D, Mesh>();

        /// <summary>卡外框在贴图里的 UV 矩形 (uMin, vMin, uMax, vMax)。量不出来的话退回整张贴图。</summary>
        public static Rect FrameUv(Texture2D tex)
        {
            Rect r;
            if (_uvCache.TryGetValue(tex, out r)) return r;

            int W = tex.width, H = tex.height;
            Color32[] px = null;
            try { px = tex.GetPixels32(); }
            catch { /* 贴图没开 Read/Write —— 见 ArtBaker.ApplyImportSettings */ }

            if (px == null)
            {
                r = new Rect(0f, 0f, 1f, 1f);
            }
            else
            {
                // ⚠️ `GetPixels32` 的 row 0 在**图的下边**，而 Unity 的 UV v=0 也在下边 ——
                //    两边一致，所以这里不用翻，直接按数组坐标算 UV。
                int x0 = W, x1 = -1, y0 = H, y1 = -1;
                for (int y = 0; y < H; y++)
                {
                    int row = y * W;
                    for (int x = 0; x < W; x++)
                    {
                        if (px[row + x].a <= 8) continue;
                        if (x < x0) x0 = x;
                        if (x > x1) x1 = x;
                        if (y < y0) y0 = y;
                        if (y > y1) y1 = y;
                    }
                }
                r = (x1 < x0 || y1 < y0)
                  ? new Rect(0f, 0f, 1f, 1f)
                  : new Rect((x0 + 0.5f) / W, (y0 + 0.5f) / H,
                             (x1 - x0 + 1) / (float)W, (y1 - y0 + 1) / (float)H);
            }

            _uvCache[tex] = r;
            return r;
        }

        static readonly Dictionary<int, Mesh> _artMeshCache = new Dictionary<int, Mesh>();

        /// <summary>
        /// **有原版卡框时**立绘那一层的网格。
        ///
        /// 两件事一起定：**原版把立绘摆在哪** + **我们的板有多大**。
        ///
        /// · 原版（`子代理读报_2dcard_0827.md` D2）：`CardImage` 是 **2.7484² 的正方形、故意比卡大**，
        ///   按自身比例装进那个方形（见 `ArtUnitSquare`），外溢部分由**卡框的 alpha 遮罩**切掉。
        /// · 我们的板 = **卡框那块 quad**（`FrameQuad`），UV 取「原版立绘落在板里的那一块」，
        ///   于是原版的构图被原样搬进卡里，而**不会溢出到卡框外面**。
        ///   ⚠️ 2026-09-12 踩过：板取 2.7484² 全铺时，立绘会**画到卡框外的空白处**
        ///     （对照 `D:/2/Warpforge部队卡片/` 的原版卡图一眼就能看出来：卡框外应该是空的）。
        /// · 我们不用 SoftMask：**卡框那层就画在立绘上面**，不透明处盖住、透空处露出 —— 等价、少一层。
        ///   于是战术卡框那条**横隔断**（本身是框上的不透明金属）会自然把立绘切在窗口里。
        ///
        /// UV 可能略微出界（立绘比方框小的时候）—— 贴图导入设的是 `Clamp`，边界外会拉伸，
        /// 正好把原版那块「立绘够不到」的边缘补上。
        /// </summary>
        /// <summary>立绘在卡面上「摆出来的范围」：宽 aw、高 ah、中心 y = acy（world 单位）。
        /// **判据只有这一份** —— 立绘网格（`ArtMeshInFrame`）和卡框的 uv2（`FrameMesh`）都读它，
        /// 各算一遍迟早错位（破框 shader 靠 uv2 对齐遮罩，错一点就会切歪）。</summary>
        static void ArtExtent(Texture2D art, out float aw, out float ah, out float acy)
        {
            float s = Width / CardUnitW;
            float sq = ArtUnitSquare * s;
            acy = ArtUnitY * s;
            aw = sq; ah = sq;                                  // 按比例装进那块方形
            if (art != null && art.height > 0)
            {
                float a = art.width / (float)art.height;
                if (a > 1f) ah = sq / a; else aw = sq * a;
            }
        }

        /// <summary>立绘**实际画出来**的那块板（含 `ArtCoverMargin`）：宽 w、高 h、中心 y = cy（world 单位）。
        /// ⚠️ 这两个数**必须成对读**：`ArtMeshInFrame` 用它们摆网格，破框 shader 的 `uv2` 用它们算 UV
        /// —— 各读各的就会差一个 1.04（<see cref="ArtUvAt"/>）。</summary>
        static void ArtBoard(Texture2D art, out float w, out float h, out float cy)
        {
            ArtExtent(art, out float aw, out float ah, out float acy);
            w = aw * ArtCoverMargin; h = ah * ArtCoverMargin; cy = acy;
        }

        /// <summary>
        /// 卡面上的一个世界点 `(x, y)` → **立绘贴图的 UV**（左上原点，和 `Texture2D` 一致）。
        ///
        /// 🔴 **判据只有这一份**：立绘网格（`ArtMeshInFrame`）与卡框的 `uv2`（`FrameMesh`）都走它。
        ///   破框 shader 拿 `uv2` 去采立绘的 alpha 当遮罩，**错一点就切歪**：
        ///   · 2026-09-13 踩过「uv2 缓存键没带立绘」⇒ 全部卡共用第一张的 uv2 ⇒ 遮罩切花；
        ///   · 2026-10-07 静态复核出第二条：这里原来漏了 `ArtCoverMargin`（立绘实际画的是 1.04 倍大），
        ///     ⇒ 遮罩比看得见的立绘**放大约 4%**：671×1024 的单位卡算下来
        ///     **框边缘处 u 差 16.1 纹素 / v 差 23.5 纹素**（0.024 / 0.023 UV），
        ///     表现是角色轮廓外多一圈被挖掉的卡框。改完两边共用本函数 ⇒ 偏差 0.0。
        /// </summary>
        static Vector2 ArtUvAt(Texture2D art, float x, float y)
        {
            ArtBoard(art, out float w, out float h, out float cy);
            return new Vector2(0.5f + x / w, (y - (cy - h * 0.5f)) / h);
        }

        static Mesh ArtMeshInFrame(Texture2D art, Vector3 frameQuad)
        {
            float s = Width / CardUnitW;
            float aw, ah, acy;
            ArtExtent(art, out aw, out ah, out acy);
            int aspectKey = 0;
            if (art != null && art.height > 0)
                aspectKey = Mathf.RoundToInt(art.width / (float)art.height * 1000f);

            int key = aspectKey * 1000
                    + Mathf.RoundToInt(aw * 100f) * 10
                    + Mathf.RoundToInt(ah * 100f);
            Mesh cached;
            if (_artMeshCache.TryGetValue(key, out cached) && cached != null) return cached;

            // 板 = **立绘自己那块**（原版摆出来的大小）×`ArtCoverMargin`，UV 正好整张图。
            // ⚠️ 2026-09-12 踩过：板取「卡框 quad」时 UV 会算出 [0,1] 之外的值（立绘比板小），
            //    贴图导入是 `Clamp` —— 立绘**最外圈像素被拉到卡片四边**，手牌上看起来就是
            //    「卡片四周多了一圈白边」（用户报的）。板 = 立绘自己那块，就没有出界的采样了。
            //    卡框的金属本来就盖在两侧，立绘不需要铺到框边。
            // 🔴 这块板的**宽/高/中心**只从 `ArtBoard` 取一份 —— 破框 shader 的 `uv2`（`ArtUvAt`）
            //    读的是同一份，两处各算一遍就会「遮罩和立绘差 4%」（2026-10-07 修，见 `ArtUvAt`）。
            ArtBoard(art, out float w, out float h, out float cy);
            float u0 = 0f, u1 = 1f, v0 = 0f, v1 = 1f;

            // **裁到卡本体那块矩形** —— 原版卡根上有 RectMask2D：卡图 2.7484² 比卡大，
            // 溢出部分先被卡的矩形裁掉、再被卡框 alpha 遮罩（D2）。
            // 我们等价地做：板超出卡本体的部分直接不画，UV 跟着裁（裁的是画面，不是拉伸）。
            // ⚠️ 方形立绘（战术卡那种 1024² 的图）一定会比卡宽，非裁不可，
            //    否则立绘会**画到卡框外面**去（和手牌那圈白边是同一类毛病）。
            if (w > Width)
            {
                u0 = 0.5f - Width * 0.5f / w;
                u1 = 0.5f + Width * 0.5f / w;
                w = Width;
            }
            if (cy - h * 0.5f < -Height * 0.5f)
            {
                v0 = (-Height * 0.5f - (cy - h * 0.5f)) / h;
                cy = -Height * 0.5f + h * 0.5f;
                h = Mathf.Min(h, Height);
            }
            if (cy + h * 0.5f > Height * 0.5f)
            {
                v1 = (Height * 0.5f - (cy - h * 0.5f)) / h;
                cy = Height * 0.5f - h * 0.5f;
                h = Mathf.Min(h, Height);
            }

            var m = new Mesh { name = "CardArtQuad" };
            m.vertices = new[]
            {
                new Vector3(-w * 0.5f, cy - h * 0.5f, 0f), new Vector3(w * 0.5f, cy - h * 0.5f, 0f),
                new Vector3(w * 0.5f, cy + h * 0.5f, 0f),  new Vector3(-w * 0.5f, cy + h * 0.5f, 0f),
            };
            m.uv = new[]
            {
                new Vector2(u0, v0), new Vector2(u1, v0),
                new Vector2(u1, v1), new Vector2(u0, v1),
            };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _artMeshCache[key] = m;
            return m;
        }

        /// <summary>
        /// **没有原版卡框时**（占位卡面）立绘那一层的网格：铺满插图位、UV 走 cover ——
        /// 那里的洞是个矩形，必须盖满、不能留缝。
        /// </summary>
        static Mesh ArtMeshInWell(Texture2D art, float rectW, float rectH, float rectY)
        {
            int key = art != null && art.height > 0
                    ? Mathf.RoundToInt(art.width / (float)art.height * 1000f) : 0;
            int cacheKey = key * 10 + 1;                       // 1 = 占位插图位
            Mesh cached;
            if (_artMeshCache.TryGetValue(cacheKey, out cached) && cached != null) return cached;

            float s = Width / CardUnitW;                       // 卡单位 → 世界
            float w = rectW * s, h = rectH * s;
            float cx = 0f, cy = rectY * s;

            float uw = 1f, uh = 1f;
            if (key > 0)
            {
                float artAspect = key / 1000f, rectAspect = rectW / rectH;
                if (artAspect > rectAspect) uw = rectAspect / artAspect;   // 立绘更宽 → 横向裁掉两边
                else                        uh = artAspect / rectAspect;   // 立绘更高 → 纵向裁掉上下
            }
            float u0 = (1f - uw) * 0.5f, v0 = (1f - uh) * 0.5f;

            var m = new Mesh { name = "CardArtQuad" };
            m.vertices = new[]
            {
                new Vector3(cx - w * 0.5f, cy - h * 0.5f, 0f), new Vector3(cx + w * 0.5f, cy - h * 0.5f, 0f),
                new Vector3(cx + w * 0.5f, cy + h * 0.5f, 0f), new Vector3(cx - w * 0.5f, cy + h * 0.5f, 0f),
            };
            m.uv = new[]
            {
                new Vector2(u0, v0), new Vector2(u0 + uw, v0),
                new Vector2(u0 + uw, v0 + uh), new Vector2(u0, v0 + uh),
            };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _artMeshCache[cacheKey] = m;      // ⚠️ 用 cacheKey（含 slot），不能只按 key —— 两条路的网格不一样
            return m;
        }

        static readonly Dictionary<string, Mesh> _spriteQuads = new Dictionary<string, Mesh>();

        /// <summary>
        /// 一块**给定中心和尺寸**的 quad（卡单位 → 世界），UV 拉满整张贴图。
        /// 给「原版是独立小图、我们原来没画」的那几样用：费用底板、护甲盾牌。
        /// `at01` 是卡面归一化坐标（x 从左、y 从**顶部**，和 `CostAt` 那批同一个口径）。
        /// </summary>
        static Mesh SpriteQuad(string name, Vector2 at01, float w, float h)
        {
            Mesh m;
            if (_spriteQuads.TryGetValue(name, out m) && m != null) return m;

            float s = Width / CardUnitW;                        // 卡单位 → 世界
            float hw = w * s * 0.5f, hh = h * s * 0.5f;
            float x = (at01.x - 0.5f) * Width, y = (0.5f - at01.y) * Height;

            m = new Mesh { name = "SpriteQuad_" + name };
            m.vertices = new[]
            {
                new Vector3(x - hw, y - hh, 0f), new Vector3(x + hw, y - hh, 0f),
                new Vector3(x + hw, y + hh, 0f), new Vector3(x - hw, y + hh, 0f),
            };
            m.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f),
            };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _spriteQuads[name] = m;
            return m;
        }

        /// <summary>
        /// 卡面**底部中央那颗菱形宝石**的网格（原版 `Rarity` 节点）。
        /// 出处：`资料/战斗规格/战斗重建_0827/子代理读报_2dcard_0827.md` A2 表
        /// —— `Rarity` **0.4 × 0.4 @ (0, −1.458)**（卡单位）。
        ///
        /// ⚠️ **它和「卡框按稀有度分档」是两件事**：卡框分档改的是**框的形制**（tier1 素 → tier4 华丽），
        ///    而卡框纹理上那颗菱形是个**空的暗色凹槽**（四档都一样）；稀有度还要靠**另外叠一张彩色宝石**
        ///    才看得出来（common 浅蓝 / rare 绿 / epic 紫 / legendary 金 / special 橙红）。
        ///    对照图：`资料/留档_排查证据/卡面组装_0912/gems.png`（上排原版卡面的宝石 / 下排我们四档框的凹槽）。
        /// </summary>
        static Mesh GemMesh()
        {
            if (_gemMesh != null) return _gemMesh;
            float s = Width / CardUnitW;
            float w = RarityGemW * s * 0.5f, h = RarityGemH * s * 0.5f;
            float cy = RarityGemY * s;
            _gemMesh = new Mesh { name = "CardRarityGemQuad" };
            _gemMesh.vertices = new[]
            {
                new Vector3(-w, -h + cy, 0f), new Vector3(w, -h + cy, 0f),
                new Vector3(w, h + cy, 0f),   new Vector3(-w, h + cy, 0f),
            };
            _gemMesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f),
            };
            _gemMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _gemMesh.RecalculateBounds();
            return _gemMesh;
        }
        static Mesh _gemMesh;

        /// <summary>稀有度宝石的贴图名（图在 `Resources/Art/ui_deck/`）。
        /// 取不到就返回 null —— 那颗宝石不画，卡面其余部分照常。</summary>
        static Texture2D RarityGem(string rarity)
        {
            string r;
            switch ((rarity ?? "").ToLowerInvariant())
            {
                case "rare":      r = "rare";      break;
                case "epic":      r = "epic";      break;
                case "legendary": r = "legendary"; break;
                case "special":   r = "special";   break;
                default:          r = "common";    break;   // common / 空 / 不认识
            }
            string idx = r == "rare" ? "2" : r == "epic" ? "3" : r == "legendary" ? "4" : r == "special" ? "5" : "1";
            return CardArt.DeckUi(idx + "_40k_cardframe_rarity_" + r);
        }

        /// <summary>破框走哪条路 —— **默认 true = 立绘画两层**（底层完整插图 + 前景角色抠图）。
        ///
        /// ⚠️ false 那条（`FrameCutout`：卡框按遮罩挖洞、立绘只画一次）**静态上已经查清并修过两处**，
        ///    但**还没有人渲图看过**（渲染验证归调度台，见 `资料/普查产出_1007/A111_A112_卡面两件.md`）：
        ///    · 2026-09-13 记录的「遮罩采样错 → 插图碎成彩色马赛克」**根因是 uv2 的缓存键**：
        ///      没带立绘 ⇒ 所有卡共用第一张卡的 uv2（修法就在 `FrameMesh` 那条缓存键上，**同一天已修**）。
        ///      ⚠️ 「还没调通」那句一直留在文档里，其实是**没跟上这次修复**（铁律 5）。
        ///    · 2026-10-07 静态复核又查出一处**真偏差**：uv2 少乘了 `ArtCoverMargin`（立绘画的是 1.04 倍大）
        ///      ⇒ 遮罩比看得见的立绘放大约 4%（实测框边缘 u 16.1 纹素 / v 23.5 纹素）。已改成两边共用 `ArtUvAt`。
        ///    · 2026-10-07 第三处（在 shader 里）：遮罩原先**无条件**采 `uv2`，而 `uv2` 在框的上下缘
        ///      跑出立绘矩形外、被贴图 Clamp 收边 —— 668 张里有 **22 张**上边缘像素 alpha > 32
        ///      ⇒ 那些卡的卡框上沿会被整条挖掉。现在矩形外 mask = 0（不挖）。
        ///    ⇒ 两条判据都落在 `CheckCutoutUvAlignment`（自检直接断，不用出图）。
        ///    ⚠️ 还没查清的部分：遮罩本身用的是**软 alpha**（立绘那圈 25% 的过渡），
        ///      挖洞边缘因此是**渐隐**而不是硬边 —— 原版是怎么处理这圈过渡的**没查到**（本地没有对应代码/资产）。
        /// </summary>
        public static bool UseFrontLayer = true;

        /// <summary>`CardPresentation/FrameCutout`：卡框按立绘 alpha 挖洞（见 shader 头注释）。</summary>
        static Material FrameCutoutMaterial()
        {
            if (_frameCutMat == null)
            {
                var sh = Shader.Find("CardPresentation/FrameCutout");
                if (sh == null)
                {
                    Debug.LogError("[CardView] 找不到 `CardPresentation/FrameCutout` —— 破框效果没了（卡框照常画）");
                    return BaseMaterial();
                }
                _frameCutMat = new Material(sh);
            }
            return _frameCutMat;
        }
        static Material _frameCutMat;

        // ==================================================================
        //  破框（`FrameCutout`）的自检判据 —— 不用出图就能断
        //
        //  这条路坏过的两次（2026-09-13 缓存键 / 2026-10-07 `ArtCoverMargin`）**都是「采样对不齐」**，
        //  而「对不齐」在断言的层面是**可以直接算的**：卡框 mesh 的 `uv2` 必须等于
        //  **立绘 mesh 在同一点上的 uv**。把它做成一函数 ⇒ 以后任何一处改歪都会当场红。
        // ==================================================================

        /// <summary>
        /// 自检：这张卡上，**卡框 mesh 的 `uv2`** 与 **立绘 mesh 在同一点上的 uv** 是否一致。
        /// 判据 = 逐顶点比（容差 1.5 纹素 / 1024）。破框 shader 就是靠这份对齐把卡框挖开的。
        /// 返回 false 时 <paramref name="detail"/> 给实测偏差。
        /// </summary>
        public bool CheckCutoutUvAlignment(out string detail)
        {
            detail = "";
            if (_frame == null || _art == null) { detail = "这张卡没有卡框层或立绘层"; return false; }
            var fmf = _frame.GetComponent<MeshFilter>();
            var amf = _art.GetComponent<MeshFilter>();
            var fm = fmf != null ? fmf.sharedMesh : null;
            var am = amf != null ? amf.sharedMesh : null;
            if (fm == null || am == null) { detail = "缺 mesh"; return false; }
            var uv2 = fm.uv2;
            if (uv2 == null || uv2.Length != 4)
            {
                detail = "卡框 mesh 没有 uv2（破框 shader 采不到遮罩）";
                return false;
            }
            var b = am.bounds;
            if (b.size.x <= 0f || b.size.y <= 0f) { detail = "立绘 mesh 的 bounds 是空的"; return false; }

            // 立绘 mesh 是**矩形**（`ArtMeshInFrame`）：顶点序 BL/BR/TR/TL、uv 与之一一对应
            // ⇒ 「世界点 → 立绘 uv」是仿射的，反解即可。
            // ⚠️ 两个 mesh 都建在**卡面那一层节点**的局部坐标里（`2DCard` 下、各层只差一个 z），
            //    但这里**不假设两边 transform 一样** —— 先把卡框顶点换算到**立绘那一层的局部坐标**再比。
            var artT = _art.transform;
            var auv = am.uv;
            var fv = fm.vertices;
            float du = 0f, dv = 0f, uv2Max = 0f;
            for (int i = 0; i < 4 && i < fv.Length; i++)
            {
                var local = artT.InverseTransformPoint(_frame.transform.TransformPoint(fv[i]));
                // 🔴 **这里不能用 `Mathf.InverseLerp` / `Mathf.Lerp`** —— 两个都会 `Clamp01`，
                //    而卡框顶点**本来就在立绘矩形外**（框 2.2452×3.2572 比立绘 1.873×2.858 大一圈）：
                //    夹断之后拿边上的 0/1 去和 `uv2` 比 ⇒ **一条假红**。
                //    2026-10-07 实测就是这么来的：`uv2` 报 u 0.09936 / v 0.07467，而那两个数
                //    **恰好等于「夹到边上」与「按仿射外插」之差**（672×1024 单位卡算得一模一样），
                //    **不是**采样对不齐（并排图里两张卡都正常）。⇒ 反解一律用**不夹断**的仿射式。
                float fx = (local.x - b.min.x) / b.size.x;
                float fy = (local.y - b.min.y) / b.size.y;
                float u = auv[0].x + (auv[1].x - auv[0].x) * fx;
                float v = auv[0].y + (auv[2].y - auv[0].y) * fy;
                du = Mathf.Max(du, Mathf.Abs(u - uv2[i].x));
                dv = Mathf.Max(dv, Mathf.Abs(v - uv2[i].y));
                uv2Max = Mathf.Max(uv2Max, Mathf.Max(uv2[i].x, uv2[i].y));
            }
            const float Tol = 1.5f / 1024f;
            detail = $"uv2 与立绘 uv 的最大偏差 u {du:F5} / v {dv:F5}（容差 {Tol:F5} = 1.5 纹素/1024）"
                   + $"；uv2 最大分量 {uv2Max:F4}"
                   + (uv2Max > 1f ? "（>1 ⇒ 框的上下缘落在立绘矩形外，shader 那道 [0,1] 判定会真的生效）"
                                  : "（全都 ≤1 ⇒ 框没超出立绘矩形，shader 那道判定不参与）");
            return du <= Tol && dv <= Tol;
        }

        /// <summary>
        /// 自检：`UseFrontLayer = false` 那条路上，卡框那一层的**材质接线**对不对 ——
        /// shader 是 `CardPresentation/FrameCutout`、`_CutMask` 挂的**就是这张卡的立绘贴图**、
        /// `_CutAmount` = 1。三条里任何一条断了，破框都会**静默变成一张普通卡框**。
        /// </summary>
        public bool CheckCutoutWiring(out string detail)
        {
            detail = "";
            if (_frame == null) { detail = "这张卡没有卡框层"; return false; }
            var m = _frame.sharedMaterial;
            if (m == null) { detail = "卡框层没有材质"; return false; }
            bool shaderOk = m.shader != null && m.shader.name == "CardPresentation/FrameCutout";
            bool maskOk = m.HasProperty("_CutMask") && m.GetTexture("_CutMask") == ArtTexture(Data);
            bool amountOk = m.HasProperty("_CutAmount") && Mathf.Approximately(m.GetFloat("_CutAmount"), 1f);
            detail = $"shader `{(m.shader != null ? m.shader.name : "<无>")}`（要 FrameCutout）· "
                   + $"_CutMask {(maskOk ? "= 本卡立绘贴图" : "**不是**本卡立绘贴图")} · "
                   + $"_CutAmount {(m.HasProperty("_CutAmount") ? m.GetFloat("_CutAmount").ToString("F2") : "<无>")}（要 1.00）";
            return shaderOk && maskOk && amountOk;
        }

        /// <summary>
        /// 卡框网格。`art` 只用来写 **uv2**（= 立绘那套 UV）—— 破框 shader 靠它采立绘的 alpha 遮罩。
        /// ⚠️ 缓存键要把 art 也算进去：同一张卡框配不同立绘，uv2 不一样。
        /// </summary>
        static Mesh FrameMesh(Texture2D tex, Texture2D art = null)
        {
            Mesh m;
            // ⚠️ 缓存键**必须把立绘也算进去**：uv2 是「立绘那套 UV」，同一张卡框配不同立绘就不一样。
            //    只按卡框缓存的话，所有卡都会拿到**第一张卡的 uv2** —— 破框遮罩就切花了
            //    （2026-09-13 踩过：整张卡的插图变成一片彩色马赛克）。
            int cacheKey = tex.GetInstanceID() * 397 ^ (art != null ? art.GetInstanceID() : 0);
            if (_frameMeshCache.TryGetValue(cacheKey, out m) && m != null
                && (art == null || m.uv2 != null && m.uv2.Length == 4)) return m;

            var uv = FrameUv(tex);
            var q = FrameQuad(tex);                            // (宽, 高, 中心y)，world
            float w = q.x * 0.5f, h = q.y * 0.5f, oy = q.z;

            // ⚠️ 用**卡框自己的**尺寸（2.2452×3.2572 @y−0.03 卡单位），不是卡本体的。
            // ⚠️ **按 sprite 自身的比例 fit 进去，不拉伸**（2026-09-12 改）：
            //    卡框 PNG 的金属 bbox 是 608×936（宽高比 0.6496），而 rect 是 0.689 ——
            //    直接拉满会把框横向撑宽 6%。fit 出来是 **2.1159 × 3.2572**，宽度正好落在
            //    卡本体（2.0927）那条线上，和原版卡面量出来的「金属基本顶到卡边」一致
            //    （原版卡面实测金属横跨卡宽的 0～98%）。
            //    出处：预制体 JSON `RectTransform_-3192049446263720900` 给了 rect 2.2452×3.2572，
            //    但 sprite 带左右透明留白，所以**画的时候要按 sprite 的实宽**。
            m = new Mesh { name = "CardFrameQuad" };
            m.vertices = new[]
            {
                new Vector3(-w, -h + oy, 0f), new Vector3(w, -h + oy, 0f),
                new Vector3(w, h + oy, 0f),   new Vector3(-w, h + oy, 0f),
            };
            m.uv = new[]
            {
                new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMax, uv.yMin),
                new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMin, uv.yMax),
            };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();

            // **uv2 = 立绘那套 UV**（破框 shader 用它采立绘的 alpha 遮罩）。
            // 🔴 判据**只有一份**：立绘网格和这里都走 `ArtUvAt`（含 `ArtCoverMargin` ——
            //    立绘实际画的是 1.04 倍大，这里少乘那 1.04 的话遮罩会比看得见的立绘放大约 4%，
            //    卡框边缘处 ≈ 20 纹素，表现为「角色轮廓外多一圈被挖掉的卡框」。2026-10-07 修）。
            if (art != null)
            {
                var uv2 = new Vector2[4];
                for (int i = 0; i < 4; i++)
                    uv2[i] = ArtUvAt(art, m.vertices[i].x, m.vertices[i].y);
                m.uv2 = uv2;
            }

            _frameMeshCache[cacheKey] = m;
            return m;
        }

        static readonly Dictionary<int, Mesh> _frameMeshCache = new Dictionary<int, Mesh>();
        static readonly Dictionary<Texture2D, Vector3> _frameQuadCache = new Dictionary<Texture2D, Vector3>();

        /// <summary>
        /// 卡框那一层的 quad：**宽、高、中心 y**（world 单位）。**判据只有这一份** ——
        /// `FrameMesh`（画框）和 `ArtMeshInFrame`（把立绘裁进这块板）都读它，
        /// 两处各算一遍的话立绘和框就会错位。
        /// ⚠️ 返回值依赖 `FrameUv`（量贴图的不透明 bbox）—— 贴图**必须开 Read/Write**，
        ///    否则 UV 退回整张图，框会被画成一块带透明边的方片（2026-09-12 踩过：
        ///    战术卡框 `frame_*_strat_tier*.png` 当时没开，整个战术卡面都是错的）。
        /// </summary>
        static Vector3 FrameQuad(Texture2D tex)
        {
            Vector3 v;
            if (_frameQuadCache.TryGetValue(tex, out v)) return v;

            float s = Width / CardUnitW;                       // 卡单位 → 世界
            // 🔴🔴 **2026-09-27 用户拍板：改成【按固定矩形画】（原来按贴图不透明 bbox 等比内接）—— 之前的设计是错的。**
            //
            // **硬证据（原版预制体实读）**：`2DCard/CardFrame` 那个 Image 是
            //   **`m_PreserveAspect = 0`**（`m_Type = 0` Simple；`m_Sprite = 0` 是因为贴图**运行时才赋**）
            //   ⇒ **原版就是把卡框贴图【拉伸】进那个固定矩形**，不是等比内接。
            //
            // **为什么原来那样会错**：四档卡框虽然都是 1024² 画布，但**画形不一样**（实测 alpha bbox w/h）：
            //   ultramarines tier1/tier2 = **0.6720** · tier3 = **0.7360** · tier4 = **0.7331**
            //   （sororitas 同趋势：0.70 → 0.74）；而框的矩形比值 `FrameUnitW/FrameUnitH` = **0.6893**。
            //   按 bbox 等比内接 ⇒ tier1/2 恰好填满矩形（814.25px），**tier3/4 被按「宽」定、高只有 768.2px（矮 5.7%）**。
            //   ⇒ 「一律用 tier4」之后**每张卡的框都矮了 5.7%**，正是这么来的。
            // 📌 `uv`（= 贴图不透明的那一块）**仍然只取那一块** —— 透明边不参与拉伸，
            //   所以「画多大」= 矩形，而「画什么」= 那一档真正的画。
            v = new Vector3(FrameUnitW * s, FrameUnitH * s, FrameUnitY * s);
            _frameQuadCache[tex] = v;
            return v;
        }

        /// <summary>卡框矩形的**世界尺寸** —— 自检与并排图用（`FrameQuad` 的 x/y；z 是中心 y）。</summary>
        public static Vector3 FrameQuadSize { get { return new Vector3(FrameUnitW, FrameUnitH, FrameUnitY) * (Width / CardUnitW); } }

        // ==================================================================
        //  程序生成的卡面：整张卡（没有原版卡框时）/ 数值层（有卡框时）
        // ==================================================================

        /// <summary>状态环那层的材质 —— **原版 shader + 原版材质 `Card board Frame SDF` 的值**。
        /// **每个 rim 一份**（我们改的是它自己的 `_Outline`，绝不能落到共享材质上 —— 那会改到所有卡）。
        /// 拿不到原版 shader 时**退回**我们原来那套（羽化图 + `Sprites/Default`）并出声说明。
        /// 材质值出处 = 原版 `Card board Frame SDF`（`m_Colors` / `m_Floats`，2026-09-29 实读）。</summary>
        static Material RimMaterial()
        {
            const string Name = "Everguild/FX/Card Highlight And Shadow";
            if (!WarpforgeVFX.WarpforgeShaderMap.TryResolve(Name, out var sh, out _) || sh == null)
            {
                Debug.LogWarning($"[CardView] 解析不到原版 shader `{Name}` ⇒ 状态环**退回**我们那张羽化图"
                               + "（形状/羽化会与原版不同；随包 shader bundle 在不在？）");
                return new Material(Shader.Find("Sprites/Default"))
                {
                    name = "CardRimFallback", mainTexture = SoftRimTexture(), color = Color.clear,
                };
            }
            var m = new Material(sh) { name = "CardFrameSdf" };
            var frame = CardArt.CardFrameSdf();
            m.mainTexture = frame != null ? frame : SoftRimTexture();
            if (frame == null)
                Debug.LogWarning("[CardView] `card_sdf/Card_board_frame_SDF` 取不到 ⇒ 状态环用我们那张羽化图代替（不静默）");
            void C(string p, Color v) { if (m.HasProperty(p)) m.SetColor(p, v); }
            void F(string p, float v) { if (m.HasProperty(p)) m.SetFloat(p, v); }
            C("_Outline",     new Color(1f, 1f, 1f, 0f));    // 平时**不亮**（原版 0.447；亮不亮由状态改写）
            C("_ShadowColor", new Color(0f, 0f, 0f, 0f));
            C("_Scale",       new Color(1f, 1f, 0f, 0f));
            F("_Inner_Edge", 0.242f); F("_Inner_Fallof", 1.0f);
            F("_Outer_Edge", 0.465f); F("_Outer_Fallof", 1.053f);
            F("_SpriteAlphaEdge_Outer", 0.484f); F("_SpriteAlphaEdge_Inner", 0.339f);
            F("_NOISE_CHANNEL", 2f);  F("_NoiseIntensity", 1.15f);
            return m;
        }

        /// <summary>
        /// 状态描边那张**羽化**的底图（64×64，四边各 12 px 渐隐、四角切掉）。
        /// ⚠️ **2026-09-29 起它只是【兜底】** —— 正常这条路走原版那张 `Card board frame SDF`
        /// （见 `RimMaterial`）；解析不到原版 shader 时才退回这里。
        /// 为什么不用纯色方片：描边那块 quad 比卡大（1.09 × 1.06），卡的四角/四边是透明的 ——
        /// 纯色方片会在那里**露出一圈硬边**，看起来就像「卡片多了一圈白边」。
        /// 原版的高亮层是 `Card Highlight And Shadow`（4.4281² 的 SDF，软光/影），不是硬方框。
        /// </summary>
        static Texture2D SoftRimTexture()
        {
            if (_softRim != null) return _softRim;
            // ⚠️ 羽化宽度只能占**卡外那一圈**：描边 quad 比卡大 9%（1.09×1.06），
            //    card 盖住中间、只有最外面约 4.5% 露得出来。F 给大了（试过 12/64=19%）
            //    等于把露出来的那一圈也做成透明的 —— 描边就「没影了」。
            const int N = 128, F = 6;                  // 6/128 = 4.7% ≈ 露出来的那一圈
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                // 到四条边的距离，取最小 → 0=边上、F=羽化完
                float d = Mathf.Min(Mathf.Min(x, N - 1 - x), Mathf.Min(y, N - 1 - y));
                float a = Mathf.Clamp01(d / F);
                a = a * a * (3f - 2f * a);            // 平滑一点，别是直线
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            _softRim = new Texture2D(N, N, TextureFormat.RGBA32, false);
            _softRim.SetPixels32(px);
            _softRim.Apply();
            _softRim.name = "SoftRim";
            return _softRim;
        }
        static Texture2D _softRim;

        static readonly Color32 Ink       = new Color32(240, 240, 245, 255);   // 主板字：白（数值用）
        static readonly Color32 InkDim    = new Color32(180, 182, 195, 255);   // 兜底点阵效果文字：灰
        // 卡名 / 效果文字的**原版颜色**（A3 表「颜色确认」）：
        //   卡名橙 (0.9922,0.6039,0.3451) → (253,154,88)；效果文字米白 (0.9804,0.9137,0.8667) → (250,233,221)
        // ⚠️ 2026-09-12 改：原来卡名用白、效果文字用灰，**没有出处**（是我们挑的）。
        /// <summary>卡名：**白**。⚠️ 2026-09-12 与成品卡核对后定的 —— A3 表把 `NameTextUnit` 记成橙、
        /// `ArmyTextUnit`（阵营行）记成白，但 `D:/2/Warpforge部队卡片/` 的成品卡**恰好反过来**
        /// （卡名白、阵营橙，`Warpforge_02_Heavy-Intercessor-2.png` 和 `pariah.png` 都是），
        /// 数字版实况拿不到，按成品卡走。</summary>
        static readonly Color32 InkName   = new Color32(255, 255, 255, 255);
        /// <summary>阵营行 / 兵种行：原版橙 (0.9922,0.6039,0.3451)</summary>
        static readonly Color32 InkArmy   = new Color32(253, 154, 88, 255);
        static readonly Color32 InkDesc   = new Color32(250, 233, 221, 255);
        static readonly Color32 InkMelee  = new Color32(255, 226, 150, 255);   // 近战：暖黄（红宝石上）
        static readonly Color32 InkRanged = new Color32(205, 175, 255, 255);   // 远程：淡紫（紫宝石上）
        static readonly Color32 InkHealth = new Color32(190, 255, 200, 255);   // 生命：淡绿（绿宝石上）
        // 护甲：冷白（原版那面盾是深灰钢色，白字最清楚。**这是我们挑的颜色**，原版数值色是纯白）
        static readonly Color32 InkArmour = new Color32(228, 238, 248, 255);
        static readonly Color32 InkCost   = new Color32(255, 255, 255, 255);   // 费用：白（蓝宝石上）
        static readonly Color32 InkCostGem= new Color32(38, 132, 214, 255);    // 费用宝石底：原版就是蓝的
        static readonly Color32 BgCard    = new Color32(10, 10, 14, 255);      // 占位卡面：近黑
        static readonly Color32 BgWell    = new Color32(20, 20, 26, 255);      // 占位卡面：插图位
        static readonly Color32 BgArt     = new Color32(26, 26, 34, 255);      // 立绘占位底色

        static Material FaceMaterial(CardData d)
        {
            // `Sprites/Default`：吃 alpha（圆角透明处才不会被画成黑块）+ 有 `_Color` 可以着色
            //（Unlit/Texture 两样都不行 —— 高亮/置灰全靠这个 _Color）
            if (_quadMat == null) _quadMat = new Material(Shader.Find("Sprites/Default"));
            var m = new Material(_quadMat);
            m.mainTexture = FaceTexture(d);
            return m;
        }

        // ⚠️ 缓存键必须带**所有画进贴图的字段** —— 只按阵营色做键的话，
        //    同色不同数值的卡会共用同一张脸（踩过）。
        static readonly Dictionary<string, Texture2D> FaceCache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Texture2D> InfoCache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Texture2D> ArtCache  = new Dictionary<string, Texture2D>();

        static string CacheKey(CardData d)
        {
            return $"{ColorUtility.ToHtmlStringRGBA(d.frame)}|{d.title}|{d.cost}|{d.melee}|{d.ranged}|{d.health}|{d.armor}|{d.keywords}|{d.isUnit}";
        }

        /// <summary>数值层（透明底）：费用 + 卡名 + 关键词 + 三个数值</summary>
        /// <param name="statsOnly">**只烘「四个数值」**（攻/远/甲/血），不烘费用 / 卡名 / 效果文字。
        /// 场上用（原版在 inPlay 把整张 `2DCard` 关掉，只留 `Board Elements` 那棵子树里的数值）——
        /// 见 `CardFace` 的注释。⚠️ 缓存键要带上这一位，否则场上和手牌会互相拿到对方的贴图。</param>
        static Texture2D InfoTexture(CardData d, bool statsOnly = false, bool skipStats = false)
        {
            string key = (statsOnly ? "infoStat|" : "info|") + (skipStats ? "nostat|" : "") + CacheKey(d);
            Texture2D cached;
            if (InfoCache.TryGetValue(key, out cached) && cached != null) return cached;

            var px = Blank(FaceW, FaceH);
            if (!statsOnly)
            {
            // ① 费用：右上角徽记的位置。
            //    ⚠️ **2026-09-12 改**：原来这里画的是「我们自己拼的蓝色六边形」（比原版小 18%、没花纹）。
            //    原版这一格是**一张真图** —— `Cost Container/Cost Background`，sprite `Card Frame Cost Icon`
            //    （248×244，0.4891×0.4809 卡单位），由 `Build` 里那个 `costBg` 图层画（那样才用得上原图）。
            //    只有**取不到那张图**时才退回六边形（不静默失败：形状差一点，但费用数字照常看得见）。
            if (d.cost >= 0)
            {
                if (CardArt.DeckUi("Card_Frame_Cost_Icon") == null)
                {
                    // 徽记本身花纹很花，所以要用**不透明的宝石**盖住它 —— 半透明/阵营色都读不出来
                    var rim = new Color32(14, 14, 18, 255);
                    // 半径：原版费用格是 0.4×0.4 卡单位 → 0.2/2.0927 = 0.0956 卡宽
                    FillHexagon(px, FaceW, FaceH, CostAt.x * FaceW, CostAt.y * FaceH, 0.0956f * FaceW, rim);
                    FillHexagon(px, FaceW, FaceH, CostAt.x * FaceW, CostAt.y * FaceH, 0.0779f * FaceW, InkCostGem);
                }
                // 费用数字：原版是 `Cost Container/CostText`（TMP + Pragati，**Thick 描边**、白字）
                if (!PragatiDigits.Available)
                    DrawCenteredAt(px, CostAt, d.cost.ToString(), 4, InkCost);
                else
                    PragatiDigits.Draw(px, FaceW, FaceH, d.cost.ToString(),
                                       Mathf.RoundToInt(CostAt.x * FaceW),
                                       Mathf.RoundToInt(CostAt.y * FaceH), true,
                                       // 费用六边形半径 0.0956 卡宽（见上面 `FillHexagon` 那两行）
                                       // ⇒ 净宽 ≈ 2×0.0956×256 ≈ 49 面像素，留边距取 41
                                       Mathf.RoundToInt(0.0956f * FaceW * 2f * 0.84f));
            }

            // ①b 🔴 **2026-09-15 撤掉了原来那层「我们自己加的柔和压暗」**（`DrawTextScrim`）。
            //     当初的注释写「原版卡面的文字是直接压在立绘上的，立绘亮的时候字就看不清」——
            //     **那个前提是错的**：原版有一层真的文字底板，只是它静态 `m_IsActive=false`，
            //     运行时由 `CardTextsController` 打开（见 `TextBgAt` 那段注释的出处）。
            //     现在按原版加上了真底板（`textBg` 图层）+ 材质描边，这层自造的压暗就没必要了 ——
            //     留着会**在底板之外多压一圈**，和原版对不上。
            //     ⚠️ `DrawTextScrim` 函数**留着**（没别的地方用，但删了这段历史就断了）。
            //     出处：`资料/PnP卡图_逐张对账_0915.md` §六末 A5。

            // 兜底点阵那条路（没有 TMP 字体资产时）也要用**同一条判据**挑位置 ——
            // 单位卡 / 战术卡两套，和 `BuildTextLayers` 读同一组常量，别各写一份。
            var nameAt = d.isUnit ? UnitNameAt : TacticNameAt;
            var descAt = d.isUnit ? UnitDescAt : TacticDescAt;

            // ② 卡名：立绘中段，过长自动缩号
            //    ⚠️ **有 TMP 字体资产时这里不画** —— 卡名由 `BuildTextLayers` 挂的
            //       `TextMeshPro` 出（中文只有那条路画得出来）。两条路不能同时画，会重影。
            if (!TmpFont.Available && !string.IsNullOrEmpty(d.title))
            {
                int scale = 4;
                int maxW = (int)(0.86f * FaceW);
                while (scale > 1 && TextCanvas.Measure(d.title, scale) > maxW) scale--;
                int h = TextCanvas.LineHeight(scale);
                TextCanvas.DrawCentered(px, FaceW, FaceH, d.title, scale, nameAt.x,
                                        Mathf.RoundToInt(nameAt.y * FaceH - h * 0.5f), InkName);
            }

            // ③ 效果文字：名字下面折行（同上，有 TMP 就交给 TMP）
            if (!TmpFont.Available && !string.IsNullOrEmpty(d.keywords))
            {
                int h = TextCanvas.LineHeight(2);
                // ⚠️ 点阵字库不认识 `<sprite>` 标签 —— 原样喂进去会画出一串空格/乱码。
                //    **剥掉再画**（图标没了，字是对的；宁可少个图标，别画一串垃圾）。
                TextCanvas.DrawWrapped(px, FaceW, FaceH, CardIcons.StripTags(d.keywords), 2,
                                       Mathf.RoundToInt(0.10f * FaceW),
                                       Mathf.RoundToInt(descAt.y * FaceH - h * 0.5f),
                                       Mathf.RoundToInt(0.80f * FaceW), 3, InkDesc);
            }

            }   // ← if (!statsOnly)：费用 / 卡名 / 效果文字**到这儿为止**，下面只画数值

            // ④ 四个数值：画在卡框的宝石上（左下红/紫、右侧盾、右下绿）
            //    ⚠️ **战术卡不画数值** —— 原版战术卡面只有费用和稀有度（对照
            //       `D:/2/Warpforge部队卡片/<阵营>/4计策/*.png`）。原来不加这个判断，
            //       战术卡上会画出「近战 0 / 生命 0」这种根本不存在的数字。
            //    ⚠️ 护甲那面**盾牌底图**由 `Build` 的 `armourIcon` 图层画（原版 `pedestal_icon_armor`）；
            //       这里只画数字，且**只有 armor > 0 才画**（原版 `ArmourText` 有值时才有字）。
            //    🔴 **2026-09-15 修**：远程原来也写成 `if (d.ranged > 0)` ⇒ **0 不画**，
            //       而原版卡面**会印一个 0**（逐张并排验收查出来的，**影响 56 张**
            //       `type∈{unit,hero}` 且 `ranged==0` 的卡）。
            //       **实据**：`D:/2/Warpforge部队卡片/Chaos/3部队/Warpforge_9_Chaos-Spawn.png`
            //       与 `Genestealer Cult/3部队/Warpforge_07_Genestealer-Familiar.png` 亲读 ——
            //       紫圈里都清楚印着「0」。`armor` 那条**不是**同一个错，别一起改。
            if (d.isUnit && !skipStats)
            {
                //    （`BoardMeleeAt/…`）—— 混用会让数值浮在卡体外面（2026-09-20 修的就是这个）。
                var meleeAt  = statsOnly ? BoardMeleeAt  : MeleeAt;
                var rangedAt = statsOnly ? BoardRangedAt : RangedAt;
                var armourAt = statsOnly ? BoardArmourAt : ArmourAt;
                var healthAt = statsOnly ? BoardHealthAt : HealthAt;
                DrawStatAt(px, meleeAt, d.melee, false);
                DrawStatAt(px, rangedAt, d.ranged, false);
                if (d.armor > 0) DrawStatAt(px, armourAt, d.armor, true);   // 护甲 = 原版 Thick 描边
                DrawStatAt(px, healthAt, d.health, false);
            }

            return BakeInfo(key, px);
        }

        static Texture2D BakeInfo(string key, Color32[] px)
        {
            var tex = new Texture2D(FaceW, FaceH, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            InfoCache[key] = tex;
            return tex;
        }

        // ==================================================================
        //  场上四个数值 —— **各占一个图层**（原版 `CardTextCountersController` 的四个 counter）
        //
        //  为什么要拆（2026-09-29）：原版「受击时**那一个**数值变色 + bump」是两个按 counter 做的动作：
        //   · `DoColorChange(old, new)` —— `new > old` 涂涨色、否则涂落色（`CardFeel.StatUpColor` /
        //     `StatDownColor`），并且**下一次 `old == new` 时刷回原色**；
        //   · `cardTextBumpSize / cardTextBumpTime` —— 那一下的缩放。
        //  我们原来把攻/远/甲/血四个数字**烘在同一张大图**（`InfoTexture`）里 ⇒ 单个动不了。
        //
        //  ⚠️ **只在 `CardFace.Board` 走这条**：手牌那个 2D 卡面的数值仍旧烘在 `_info` 里
        //     （那边不随战斗变，拆了还要重做一套几何）。两边的判据都是 `_faceMode`，只有一处。
        //  ⚠️ 字形表不在（`PragatiDigits.Available == false`）⇒ **不拆**，退回老路（不静默不画）。
        // ==================================================================

        /// <summary>一个数值格独立贴图的边长（面像素）。🔴 **与 <see cref="StatCell"/> 成对**，改一个必须改另一个。</summary>
        const int StatCanvas = 64;
        /// <summary>数值格在**卡单位**里的边长 = `CardUnitW × StatCanvas / FaceW`。
        /// 这样算出来，数字的像素尺寸与「烘在 `FaceW` 宽的大图里」时**逐像素相同** ——
        /// 卡面上别处的数字（手牌那张脸）与场上这四个看上去一样大。</summary>
        static float StatCell { get { return CardUnitW * StatCanvas / FaceW; } }
        static readonly System.Collections.Generic.Dictionary<string, Texture2D> StatCache =
            new System.Collections.Generic.Dictionary<string, Texture2D>();

        /// <summary>第 `s` 个数值格用哪一套坐标（两套位置见 `MeleeAt` 那一段的注释）。</summary>
        static Vector2 StatAt01(int s, bool board)
        {
            switch (s)
            {
                case StatMelee:  return board ? BoardMeleeAt  : MeleeAt;
                case StatRanged: return board ? BoardRangedAt : RangedAt;
                case StatArmour: return board ? BoardArmourAt : ArmourAt;
                default:         return board ? BoardHealthAt : HealthAt;
            }
        }

        static int StatValue(CardData d, int s)
        {
            switch (s)
            {
                case StatMelee:  return d.melee;
                case StatRanged: return d.ranged;
                case StatArmour: return d.armor;
                default:         return d.health;
            }
        }

        /// <summary>护甲那个数字原版是 **Thick 描边**（`DrawStatAt` 的 `thick` 参数同一条判据）。</summary>
        static bool StatThick(int s) { return s == StatArmour; }

        /// <summary>一个数值格的独立贴图（`StatCanvas`²、数字居中）。字形表不在时返回 **null**
        /// ⇒ 调用方不拆（数值仍旧烘在 `_info` 里）。</summary>
        static Texture2D StatTexture(int s, int value)
        {
            if (!PragatiDigits.Available) return null;
            int v = Mathf.Max(0, value);
            string key = s + "|" + v;
            Texture2D cached;
            if (StatCache.TryGetValue(key, out cached) && cached != null) return cached;

            var px = Blank(StatCanvas, StatCanvas);
            PragatiDigits.Draw(px, StatCanvas, StatCanvas, v.ToString(),
                               StatCanvas / 2, StatCanvas / 2, StatThick(s), StatDigitMaxW);
            var tex = new Texture2D(StatCanvas, StatCanvas, TextureFormat.RGBA32, false) { name = "Stat_" + key };
            tex.SetPixels32(px);
            tex.Apply();
            StatCache[key] = tex;
            return tex;
        }

        /// <summary>
        /// **懒建**四个数值层。建不出来的两种情形都**不静默**：
        /// ① 不是单位卡（战术卡卡面本来就没有数值格）；② 字形表不在（退回 `_info` 那条老路）。
        /// ⚠️ 网格**居中**、GameObject 落在数值格中心 ⇒ `localScale` 绕自己缩放（bump 要的就是这个）。
        /// </summary>
        void BuildStatLayers()
        {
            if (_stats[1] != null) return;
            // ⚠️ `CardData` 是 struct ——「还没数据」看 `id` 空不空，不是 `== null`。
            if (string.IsNullOrEmpty(Data.id) || !Data.isUnit) return;
            if (!PragatiDigits.Available) return;

            for (int s = StatMelee; s <= StatHealth; s++)
            {
                // ⚠️ **`Color[]` 的默认元素是 (0,0,0,0) 不是白** —— 不显式填白的话，
                //    这四个数值层的颜色会被 `ApplyStatFlashes` 乘成**全透明**（卡上看不见数字）。
                _statFlash[s] = Color.white;
                var at = StatAt01(s, true);
                var go = new GameObject("stat" + s);
                go.transform.SetParent(FaceRoot, false);  // 四个数值层也是卡面（场上的倾摆是关的，见 `SetFace`）
                go.transform.localPosition = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height, BoardInfoZ);
                go.AddComponent<MeshFilter>().sharedMesh = CenteredQuad("stat" + s, StatCell, StatCell, null);
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = new Material(BaseMaterial());
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                _layers.Add(mr);
                _stats[s] = mr;
            }
        }

        /// <summary>把四个数值层的位置 / 贴图 / 显隐刷一遍。`board` = 现在是场上形态。</summary>
        void SyncStatLayers()
        {
            if (_stats[1] == null) return;
            bool board = _faceMode == CardFace.Board;
            for (int s = StatMelee; s <= StatHealth; s++)
            {
                var mr = _stats[s];
                if (mr == null) continue;
                int v = StatValue(Data, s);
                // 护甲**只有 > 0 才画**（原版 `ArmourText` 有值才有字）；其余三个 0 也画
                // （`ranged == 0` 要印一个 0 —— 判据见 `InfoTexture` 里那段注释）。
                bool show = board && Data.isUnit && !(s == StatArmour && v <= 0);
                var tex = show ? StatTexture(s, v) : null;
                if (tex == null) show = false;
                mr.gameObject.SetActive(show);
                if (!show) continue;

                mr.sharedMaterial.mainTexture = tex;
                var at = StatAt01(s, true);
                mr.transform.localPosition = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height, BoardInfoZ);
                var c = _statFlash[s];
                c.a *= _alpha;
                mr.sharedMaterial.color = (_tint * c);
            }
        }

        /// <summary>那一个数值**涨 / 落**了：变色 + bump（原版 `CardTextCountersController` 那两件事）。
        /// 判据与「哪两个色」→ <see cref="CardFeel.StatUpColor"/>。</summary>
        void FlashStat(int s, bool up)
        {
            var mr = _stats[s];
            if (mr == null || !mr.gameObject.activeSelf) return;
            _statFlash[s] = up ? CardFeel.StatUpColor : CardFeel.StatDownColor;
            // ⚠️ **这里不刷色** —— `SetData` 末了那次 `SyncStatLayers` 才画上新值，
            //    此刻 `Data` 还是旧的（见 `CaptureStatChanges` 的注释）。
            //
            // 🔴 **bump 的四个参数是从 9 参 `SetText` 的指令流里读出来的**（2026-09-29，VA `0x18060C180`）：
            //    · 先把 `localScale` **复位**到基准（`set_localScale`，不是 `DOScale`）；
            //    · 再 `DOPunchScale(基准 × cardTextBumpSize, cardTextBumpTime, vibrato = 10, elasticity = 2.0f)`
            //      —— **幅度是「乘」不是「打到」**（三处 `mulss`），弹性那个 `2.0f` 是常量池 `0x1834B2BBC`。
            //    · 基准 = 攻击类型高亮态用 `HighlightAttackType.TargetScale`、否则 `originalScales[i]`
            //      （我们这四个数值层没有那套高亮，基准恒 `Vector3.one`）。
            var tr = mr.transform;
            tr.DOKill();
            tr.localScale = Vector3.one;                     // 原版先复位
            CardTween.Use(tr.DOPunchScale(Vector3.one * CardFeel.StatBumpSize, CardFeel.StatBumpTime,
                                          CardFeel.StatBumpVibrato, CardFeel.StatBumpElasticity),
                          Ease.Linear, tr);
            StatBumps++;
        }

        /// <summary>自检用：bump 触发过几次（截图看不出「那一下有没有播」）。</summary>
        public int StatBumps { get; private set; }

        /// <summary>自检用：第 `s` 个数值现在是什么色（`CardFeel.StatUpColor` / `StatDownColor` / 白）。</summary>
        public Color StatFlash(int s) { return _statFlash[s]; }

        /// <summary>自检用：第 `s` 个数值现在画着哪个数（没画返回 -1）。</summary>
        public int StatShown(int s)
        {
            var mr = _stats[s];
            return (mr != null && mr.gameObject.activeSelf) ? StatValue(Data, s) : -1;
        }

        /// <summary>四个数值的闪现色全部刷回白（原版那条「`old == new` 就回原色」）。</summary>
        void ClearStatFlashes()
        {
            for (int s = StatMelee; s <= StatHealth; s++) _statFlash[s] = Color.white;
        }

        /// <summary>把四个数值的闪现色贴回材质（`ApplyTint` 每条路都调 —— 它会把 `_layers` 统一刷一遍色，
        /// 不重贴的话变色撑不过一帧）。</summary>
        void ApplyStatFlashes()
        {
            if (_stats[1] == null) return;
            for (int s = StatMelee; s <= StatHealth; s++)
            {
                var mr = _stats[s];
                if (mr == null || !mr.gameObject.activeSelf || mr.sharedMaterial == null) continue;
                var c = _statFlash[s];
                c.a *= _alpha;
                mr.sharedMaterial.color = _tint * c;
            }
        }

        /// <summary>立绘：优先用原版立绘（`Art/cards/art_&lt;卡名>.png`），没有再退回程序生成的占位图。
        /// 换自己的美术就是把同名文件换掉。</summary>
        static Texture2D ArtTexture(CardData d)
        {
            var real = d.artOverride != null ? d.artOverride : CardArt.Portrait(d.artId);   // ⚠️ 同上：立绘按 id 取名
            if (real != null) return real;

            string key = "art|" + ColorUtility.ToHtmlStringRGBA(d.frame) + "|" + d.title;
            Texture2D cached;
            if (ArtCache.TryGetValue(key, out cached) && cached != null) return cached;

            // ⚠️ **别在中间放大字**：卡名就画在这块中间（原版也是），
            //    大字会和名字打架（踩过）。占位图只要「看得出这里该有画」就够。
            const int W = 128, H = 179;
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float t = y / (float)(H - 1);
                // 上亮下暗的斜渐变 + 一点阵营色，像张「待填的画」
                float k = 0.20f + 0.35f * t;
                var c = new Color32((byte)(BgArt.r * k + d.frame.r * 90 * (1f - t)),
                                    (byte)(BgArt.g * k + d.frame.g * 90 * (1f - t)),
                                    (byte)(BgArt.b * k + d.frame.b * 90 * (1f - t)), 255);
                for (int x = 0; x < W; x++) px[y * W + x] = c;
            }
            // 对角斜线，明确是「占位」
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (((x + y) / 9) % 2 == 0 && (y % 3) == 0)
                        px[y * W + x] = new Color32((byte)(d.frame.r * 70 + 18),
                                                    (byte)(d.frame.g * 70 + 18),
                                                    (byte)(d.frame.b * 70 + 18), 255);

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            ArtCache[key] = tex;
            return tex;
        }

        static Color32[] Blank(int W, int H)
        {
            var px = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);
            return px;
        }

        /// <summary>把一个数字居中画在 (x01, y01) 那个点上</summary>
        static void DrawCenteredAt(Color32[] px, Vector2 at, string s, int scale, Color32 c)
        {
            int w = TextCanvas.Measure(s, scale), h = TextCanvas.LineHeight(scale);
            TextCanvas.Draw(px, FaceW, FaceH, s, scale,
                            Mathf.RoundToInt(at.x * FaceW - w * 0.5f),
                            Mathf.RoundToInt(at.y * FaceH - h * 0.5f), c);
        }

        /// <summary>文字区底下的柔和压暗（竖渐变 + 左右淡出）。见 `InfoTexture` 里的注释。</summary>
        static void DrawTextScrim(Color32[] px, int W, int H, float y0, float y1, float x0, float x1, int maxA)
        {
            int py0 = Mathf.Clamp(Mathf.RoundToInt(y0 * H), 0, H - 1);
            int py1 = Mathf.Clamp(Mathf.RoundToInt(y1 * H), 0, H - 1);
            int px0 = Mathf.Clamp(Mathf.RoundToInt(x0 * W), 0, W - 1);
            int px1 = Mathf.Clamp(Mathf.RoundToInt(x1 * W), 0, W - 1);
            int span = Mathf.Max(1, py1 - py0);
            for (int y = py0; y <= py1; y++)
            {
                // 纵向：中间最重、上下淡出
                float t = (y - py0) / (float)span;
                float vy = Mathf.Sin(t * Mathf.PI);
                int row = (H - 1 - y) * W;        // 数组 row 0 在下边（和本文件其它画法一致）
                for (int x = px0; x <= px1; x++)
                {
                    float u = (x - px0) / (float)Mathf.Max(1, px1 - px0);
                    float vx = Mathf.Sin(u * Mathf.PI);
                    int a = Mathf.RoundToInt(maxA * vy * vx);
                    if (a <= 0) continue;
                    int i = row + x;
                    // 直接盖上去（`_info` 在卡框之上，所以这是「压暗」不是「混色」）
                    px[i] = new Color32(0, 0, 0, (byte)Mathf.Min(255, px[i].a + a));
                }
            }
        }

        /// <summary>
        /// 一个数值格里的数字。🔴 **2026-09-15 换成原版字体**（原来是自写 5×7 点阵）。
        ///
        /// 出处：`资料/PnP卡图_逐张对账_0915.md` §六末 **A3** —— 原版费用与四个数值全是
        /// `TextMeshProUGUI` + **`Pragati-Regular SDF`**（逐字段读 `MonoBehaviour_{3782,4009,…}.json`），
        /// **没有任何点阵字形**。颜色是**白字 + 黑描边**（`m_fontColor=(1,1,1,1)`；
        /// 描边在材质里：生命/近战/远程 `Thin` `_OutlineWidth=0.05`，**护甲/费用 `Thick` 0.097**）。
        /// ⇒ 所以这里的 `thick` 不是我们挑的，是照原版材质分的。
        /// 字形表 = `Core/PragatiDigits.Data.cs`（生成物，从原版 SDF 图集抠的）。
        /// </summary>
        static void DrawStatAt(Color32[] px, Vector2 at, int value, bool thick)
        {
            if (!PragatiDigits.Available)
            {
                // 拿不到字形表就退回点阵（**不静默不画**）
                DrawCenteredAt(px, at, Mathf.Max(0, value).ToString(), 4, InkHealth);
                return;
            }
            PragatiDigits.Draw(px, FaceW, FaceH, Mathf.Max(0, value).ToString(),
                               Mathf.RoundToInt(at.x * FaceW), Mathf.RoundToInt(at.y * FaceH), thick,
                               StatDigitMaxW);
        }

        /// <summary>数值格里的数字**最多多宽**（面像素）—— 超过就整体缩号（见 `PragatiDigits.Draw` 的 `maxW`）。
        ///
        /// 来历：数值圆是**烘在卡框图里**的，代码里没有半径常量。2026-09-16 量出来的算法是：
        /// 子代理在 700 px 宽的渲染图上量紫圈内沿为 x 113→203 = **90 px** ⇒ 占卡宽 12.86%
        /// ⇒ 折算到我们 `FaceW = 256` ⇒ **32.9 px**，两边各留 ~1 px 边距 ⇒ **31**。
        /// 一位数总宽 ≈ 18 面像素，不受影响；两位数（`Measure("10")` = 36.6）缩到 0.85 倍后进得去。
        /// ⚠️ 原版的两位数**本来就比一位数小**（同一张 `EC41` 卡图上，原版紫圈里的 `10` 四周都有余量），
        /// 所以「按位数缩号」不是我们的将就，是照原版。
        /// </summary>
        const int StatDigitMaxW = 31;

        /// <summary>尖朝左右的正六边形（费用底）。
        /// ⚠️ `cy` 是**从顶部**数的像素行（和本文件其它画法一致）——
        ///    画布数组本身 row 0 在底部，所以这里要先翻一次，别直接拿它当行号（踩过）</summary>
        static void FillHexagon(Color32[] px, int W, int H, float cx, float cyFromTop, float r, Color32 c)
        {
            float cy = (H - 1) - cyFromTop;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(cx + r));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(cy + r));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - cx) / r, dy = Mathf.Abs(y + 0.5f - cy) / r;
                    if (dy <= 1f && dx <= 1f - 0.5f * dy) px[y * W + x] = c;
                }
        }

        // ---- 占位卡面（没有原版卡框时用）：整张黑卡 ----
        static Texture2D FaceTexture(CardData d)
        {
            string key = CacheKey(d);
            Texture2D cached;
            if (FaceCache.TryGetValue(key, out cached) && cached != null) return cached;

            var px = Blank(FaceW, FaceH);

            // ① 圆角黑底
            FillRoundedRect(px, FaceW, FaceH, 0.015f, 0.02f, BgCard);

            // ② 阵营色边框（细，不喧宾夺主）
            var edge = (Color32)d.frame; edge.a = 255;
            StrokeRoundedRect(px, FaceW, FaceH, 0.015f, 0.02f, 3, edge);

            // ③ 插图位：**抠成透明**（不是填底色）—— 立绘那一层就画在这个位置（`ArtMesh(.., WellW..)`），
            //    卡面在 z=0、立绘在 z=0.03（更远），所以这里透明才能让立绘露出来。
            //    ⚠️ 立绘缺了也不会开天窗：`ArtTexture()` 会退回程序生成的占位画。
            ClearRect(px, FaceW, FaceH, 0.06f, 0.20f, 0.94f, 0.58f);
            StrokeRect(px, FaceW, FaceH, 0.06f, 0.20f, 0.94f, 0.58f, 1, new Color32(48, 48, 58, 255));

            // ④⑤⑥⑦ 复用数值层那套画法（位置和原版卡框一致）
            var info = InfoTexture(d);
            var infoPx = info.GetPixels32();
            for (int i = 0; i < px.Length && i < infoPx.Length; i++)
            {
                var s = infoPx[i];
                if (s.a > 0) px[i] = s;
            }

            var tex = new Texture2D(FaceW, FaceH, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            FaceCache[key] = tex;
            return tex;
        }

        // ---- 画布基本图元 ----
        // ⚠️ 坐标都是归一化的，**y 从顶部往下**（和 TextCanvas 一致）。
        //    画布数组本身是 row 0 在底部，所以这里统一翻一次，调用处就不用操心。

        static int RowFromTop(int H, float y01Top)
        {
            return H - 1 - Mathf.RoundToInt(y01Top * H);
        }

        /// <summary>把一块矩形**抠成透明**（alpha=0）—— 占位卡面的插图位用它，
        /// 好让后面那层立绘透出来（见 `Build` 里没有卡框那条分支）。</summary>
        static void ClearRect(Color32[] px, int W, int H, float x0, float y0, float x1, float y1)
        {
            FillRect(px, W, H, x0, y0, x1, y1, new Color32(0, 0, 0, 0));
        }

        static void FillRect(Color32[] px, int W, int H, float x0, float y0, float x1, float y1, Color32 c)
        {
            int ix0 = Mathf.RoundToInt(x0 * W), ix1 = Mathf.RoundToInt(x1 * W);
            int rowTop = RowFromTop(H, y0), rowBot = RowFromTop(H, y1);
            for (int y = rowBot; y <= rowTop; y++)
            {
                if (y < 0 || y >= H) continue;
                for (int x = ix0; x < ix1; x++)
                {
                    if (x < 0 || x >= W) continue;
                    px[y * W + x] = c;
                }
            }
        }

        static void StrokeRect(Color32[] px, int W, int H, float x0, float y0, float x1, float y1,
                               int thickness, Color32 c)
        {
            int ix0 = Mathf.RoundToInt(x0 * W), ix1 = Mathf.RoundToInt(x1 * W);
            int rowTop = RowFromTop(H, y0), rowBot = RowFromTop(H, y1);
            for (int t = 0; t < thickness; t++)
            {
                for (int x = ix0; x < ix1; x++)
                {
                    Plot(px, W, H, x, rowTop - t, c);
                    Plot(px, W, H, x, rowBot + t, c);
                }
                for (int y = rowBot; y <= rowTop; y++)
                {
                    Plot(px, W, H, ix0 + t, y, c);
                    Plot(px, W, H, ix1 - 1 - t, y, c);
                }
            }
        }

        static void Plot(Color32[] px, int W, int H, int x, int y, Color32 c)
        {
            if (x < 0 || x >= W || y < 0 || y >= H) return;
            px[y * W + x] = c;
        }

        /// <summary>归一化坐标是否落在「内缩 margin、圆角 radius」的圆角矩形里</summary>
        static bool InRoundedRect(float nx, float ny, float margin, float radius)
        {
            float x0 = margin, x1 = 1f - margin, y0 = margin, y1 = 1f - margin;
            if (nx < x0 || nx > x1 || ny < y0 || ny > y1) return false;
            float cx = Mathf.Clamp(nx, x0 + radius, x1 - radius);
            float cy = Mathf.Clamp(ny, y0 + radius, y1 - radius);
            float dx = nx - cx, dy = ny - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        static void FillRoundedRect(Color32[] px, int W, int H, float margin, float radius, Color32 c)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (InRoundedRect((x + 0.5f) / W, (y + 0.5f) / H, margin, radius))
                        px[y * W + x] = c;
        }

        static void StrokeRoundedRect(Color32[] px, int W, int H, float margin, float radius,
                                      int thickness, Color32 c)
        {
            float innerMargin = margin + (float)thickness / Mathf.Min(W, H);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float nx = (x + 0.5f) / W, ny = (y + 0.5f) / H;
                    if (InRoundedRect(nx, ny, margin, radius) && !InRoundedRect(nx, ny, innerMargin, radius))
                        px[y * W + x] = c;
                }
        }

        static Mesh _quad;
        static Mesh Quad()
        {
            if (_quad != null) return _quad;
            var m = new Mesh { name = "CardQuad" };
            float w = Width * 0.5f, h = Height * 0.5f;
            m.vertices = new[]
            {
                new Vector3(-w, -h, 0f), new Vector3(w, -h, 0f),
                new Vector3(w, h, 0f),   new Vector3(-w, h, 0f),
            };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _quad = m;
            return m;
        }
    }
}
