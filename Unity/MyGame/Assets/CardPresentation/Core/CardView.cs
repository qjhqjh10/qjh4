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
        /// 战术卡没有这一行。原版数据里是英文，**我们还没有中文对照表**，照原样显示。</summary>
        public string subtype;

        /// <summary>🆕 立绘 / 抠图清单的**文件键**（= 引擎卡 id，例 `UM82`）。
        /// 🔴 2026-09-15 起立绘**按 id 命名**（原来是卡名）—— 同名跨阵营的卡（`Terminator` /
        /// `Aggressor` / `Maulerfiend` / `Bladeguard Veteran` / `Terminator Champion`）原来会
        /// **互相覆盖**，实测 `art_aggressor.png` 是太空野狼那张、暗黑天使那张挂着别人的画。
        /// 见 `资料/PnP卡图_逐张对账_0915.md` §五。⚠️ 我们自己设计的那 26 张没有引擎 id，仍用**卡名**。</summary>
        public string artId;

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
        const float CardUnitW = 2.0927f, CardUnitH = 3.3313f;
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

        MeshRenderer _face;      // 没有卡框时的整张卡面
        MeshRenderer _frame;     // 原版卡框
        MeshRenderer _art;       // 立绘：**完整插图**（忽略 alpha，垫在卡框下）
        MeshRenderer _artFront;  // 立绘：**角色抠图**（真 alpha，盖在卡框上）—— 没有抠图的卡是 null
        MeshRenderer _gem;       // 卡面底部那颗稀有度宝石（原版 `Rarity`）
        MeshRenderer _costBg;    // 费用底板（原版 `Cost Container/Cost Background`，图 `Card_Frame_Cost_Icon`）
        MeshRenderer _armourIcon;// 护甲盾牌底（原版 `Armour Container/Image`，图 `pedestal_icon_armor`）
        MeshRenderer _info;      // 数值层
        MeshRenderer _rim;       // 状态描边
        MeshRenderer _ephemeral; // 🆕 「临时卡」角标（原版关键词图标；默认关掉）

        // ---- 🆕 棋盘单位卡的 buff / debuff 徽标（原版 `BattleCardUI.boardTraitIcons`）--------
        // 判据（哪些关键词进位、认不认得出图、角标画不画）**全在 `Core/Badges.cs`**；
        // 这里只管画。位子/大小取自原版预制体 `TraitIconContainer*.json` 的 Transform，
        // 换算过程写在 `Badges.SlotAt` 上面 —— **不在这里写第二份**。
        readonly List<MeshRenderer> _badgePlates = new List<MeshRenderer>();
        readonly List<MeshRenderer> _badgeIcons = new List<MeshRenderer>();
        readonly List<TextMeshPro> _badgeCounters = new List<TextMeshPro>();
        TextMeshPro _title;      // 卡名（TMP。没有字体资产时为 null，字烘在 _info 里）
        TextMeshPro _keywords;   // 关键词（同上）
        TextMeshPro _army;       // 阵营行（原版 `ArmyTextUnit` / `ArmyTextTactc`）
        TextMeshPro _race;       // 兵种行（原版 `RaceText`；战术卡没有）
        static Material _quadMat;

        readonly List<MeshRenderer> _layers = new List<MeshRenderer>();

        /// <summary>造一张卡。parent 传手牌容器或战场容器。</summary>
        public static CardView Create(Transform parent, CardData d, string name = null)
        {
            var go = new GameObject(name ?? d.title);
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<CardView>();
            v.Build(d);
            return v;
        }

        /// <summary>换卡面（同一张卡换数据时用，比如手牌换牌）。</summary>
        public void SetData(CardData d)
        {
            Data = d;
            if (_face != null) _face.sharedMaterial = FaceMaterial(d);
            if (_info != null)
            {
                _info.sharedMaterial.mainTexture = InfoTexture(d);
                if (_art != null) _art.sharedMaterial.mainTexture = ArtTexture(d);
            }

            // TMP 那两层跟数据走（名字/关键词可能整条换掉）。
            // ⚠️ **原地更新，不要销毁重建** —— 这条路在「战场每刷新一次」时都会走
            //    （`BattleDriver.cs:600`，掉血/疲劳都要反映到卡面），而 Play 模式下
            //    `Destroy` 要等帧末，重建的话**那一帧新旧两份字会叠在一起**。
            BuildTextLayers(d);

            // 🆕 徽标跟着走：掉血/中 buff/获得关键词都会走到这条路（`BattleDriver` 每刷新一次都调）
            SetBadges(d.badges);
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
                _badgeIcons[i].sharedMaterial.mainTexture = CardArt.Trait(b.sprite);
                if (_badgeCounters[i] != null)
                {
                    // 原版带角标的那一支叫 `With counter`，不带的那支叫 `Without counter`
                    // —— 两支是**并排的两套 renderer**，我们按 `b.counter` 决定显不显示。
                    _badgeCounters[i].text = b.counter > 0 ? b.counter.ToString() : "";
                    if (b.counter > 0) PlaceAt(_badgeCounters[i], BadgeAt01(i), BadgeCounterZ);
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

        /// <summary>卡单位坐标 → `SpriteQuad` 要的 0..1（左上原点）。</summary>
        static Vector2 ToAt01(Vector2 cardUnit)
        {
            return new Vector2(cardUnit.x / CardUnitW + 0.5f, 0.5f - cardUnit.y / CardUnitH);
        }

        static Vector2 BadgeAt01(int i) { return ToAt01(Badges.SlotAt(i)); }

        const float BadgePlateZ = -0.045f;    // 在数值层(−0.02)与临时角标(−0.03)**之前**
        const float BadgeIconZ = -0.05f;
        const float BadgeCounterZ = -0.055f;
        /// <summary>徽标角标的字色 —— **深褐**（压在浅米色徽标底板上要读得出来）。**我们挑的**，理由见 `BuildBadgeSlot`。</summary>
        static readonly Color32 BadgeCounterInk = new Color32(46, 36, 28, 255);

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
            // 底板在**原版里**往卡外偏 0.30 卡单位（`IconBackground` 的 localPosition，左右镜像）——
            // 那是给 `3DBody` 那套**斜着的 3D 卡**用的，底板是一块斜插的铭牌。
            // ⚠️ **我们把它摆到图标正后方**（`Badges.PlateOffset` = 0）：我们是平面 2D 卡，
            //    照搬 0.30 会让底板和图标**分家**（7 倍放大图里一眼可见，2026-09-15 试过）。
            //    这是一处**明写的偏离**，不是抄错字段。
            var plateAt = ToAt01(new Vector2(at.x + (at.x < 0f ? -Badges.PlateOffset : Badges.PlateOffset),
                                             at.y - 0.02f));

            _badgePlates.Add(AddLayer("badgePlate" + i, plate, BadgePlateZ, plate,
                                      FitQuad("badgePlate" + i, plateAt, Badges.PlateW, Badges.PlateH, plate)));
            _badgeIcons.Add(AddLayer("badgeIcon" + i, icon, BadgeIconZ, icon,
                                     FitQuad("badgeIcon" + i, BadgeAt01(i), Badges.IconSize, Badges.IconSize, icon)));

            // ⚠️ 角标数字**用深色**（不是卡面其它的白字）：它压在**浅米色底板**上，
            //    白字在 7 倍放大图里几乎读不出来（2026-09-15 试过）。原版那个计数器用的什么颜色
            //    本地查不到（`TraitCounter` 的 TMP 资产没解出来）⇒ **这一条是我们挑的**。
            var t = TmpFont.NewText(transform, "badgeCounter" + i, "", BadgeCounterFontSize, BadgeCounterInk);
            _badgeCounters.Add(t);
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
                //   判据是**清单**（`card_cutouts.json`，665 张）：单位卡基本都有、战术卡基本都没有；
                //   没有立绘（回退占位图）的卡不在清单里 —— 给它加前景层会把卡框整个盖住。
                bool cut = CardArt.HasCutout(d.artId);      // ⚠️ 立绘按 **id** 取名，不是卡名（见 `CardData.artId`）
                _art = AddLayer("art", frameTex, 0.03f, artTex, artMesh, opaque: true);
                if (cut && UseFrontLayer && !DebugNoArtFront)
                    _artFront = AddLayer("artFront", frameTex, -0.008f, artTex, artMesh);
                _frame = AddLayer("frame", frameTex, 0f, null);
                if (cut && !UseFrontLayer && artTex != null)   // ⚠️ 这条路没调通，见 `UseFrontLayer` 的注释
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
                _frame.GetComponent<MeshFilter>().sharedMesh = FrameMesh(frameTex, artTex);
                // 费用底板 / 护甲盾：原版的**两张真图**（A2 表），垫在数值底下 ——
                // z 比 `_info`(−0.02) 远、比卡框(0) 近，所以数字盖在它们上面，和原版层级一致。
                var costTex = CardArt.DeckUi("Card_Frame_Cost_Icon");
                if (costTex != null)
                    _costBg = AddLayer("costBg", costTex, -0.015f, costTex,
                                       SpriteQuad("costBg", CostAt, CostBgW, CostBgH));
                // ⚠️ 只有**单位卡且有护甲**才画那面盾：战术卡的卡面上没有数值格
                //    （对照 `D:/2/Warpforge部队卡片/…/4计策/*.png` —— 战术卡只有费用和稀有度）
                var armourTex = d.isUnit && d.armor > 0 ? CardArt.DeckUi("pedestal_icon_armor") : null;
                if (armourTex != null)
                    _armourIcon = AddLayer("armourIcon", armourTex, -0.015f, armourTex,
                                           SpriteQuad("armourIcon", ArmourIconAt, ArmourIconW, ArmourIconH));
                _info = AddLayer("info", frameTex, -0.02f, InfoTexture(d));
                // 稀有度宝石：叠在卡框那颗**暗色凹槽**上（原版 `Rarity` 节点）。
                // 卡框分档改的是框的形制，**光靠它看不出稀有度** —— 颜色在这颗宝石上。
                // 图取不到就不画（不静默失败：卡框和数值照常）。
                var gemTex = RarityGem(d.rarity);
                if (gemTex != null) _gem = AddLayer("gem", gemTex, -0.01f, gemTex, GemMesh());

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
                // ⚠️ 这条路**以前不画立绘**（原版插图白导进来了）—— 9 个没导卡框的阵营
                //    在卡组编辑器里就是一片空白。现在补上：立绘画在**插图位**，
                //    占位卡面那块是透明的（见 `FaceTexture` ③），立绘正好从那里露出来。
                _face = GetComponent<MeshRenderer>();
                _face.sharedMaterial = FaceMaterial(d);
                _layers.Add(_face);
                var wellArt = ArtTexture(d);
                _art = AddLayer("art", null, 0.03f, wellArt, ArtMeshInWell(wellArt, WellW, WellH, WellY));
            }

            // 状态描边：比卡面大一圈、贴在后面（z 大一点 = 更远），默认关掉
            var rimGo = new GameObject("rim");
            rimGo.transform.SetParent(transform, false);
            rimGo.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            rimGo.transform.localScale = new Vector3(1.09f, 1.06f, 1f);
            rimGo.AddComponent<MeshFilter>().sharedMesh = Quad();
            _rim = rimGo.AddComponent<MeshRenderer>();
            _rim.sharedMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                mainTexture = SoftRimTexture(),   // ⚠️ 不是纯色方片，见 `SoftRimTexture`
                color = Color.clear,
            };
            _rim.enabled = false;

            // 卡名/关键词走 TMP（拿不到字体资产时它自己会跳过，字仍旧烘在 _info 里）
            BuildTextLayers(d);

            // 🆕 棋盘徽标（`CardData.badges` 为空时这一句什么都不建 —— 手牌/战术卡都是空的）
            SetBadges(d.badges);
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
            _title = Fill(_title, "title", d.title, nameAt, nameW, TitleFontSize, InkName, false, nameH);

            // ② 效果文字：名字下面，超宽折行；行数多了按**原版那块版面框**的高度回缩。
            //    ⚠️ **上沿对齐**（不是居中）：我们的块会随行数长高，居中的话行数一多就往上长、
            //       撞到阵营行（2026-09-12 实测战术卡 3 行描述把 `ULTRAMARINES` 盖住了）。
            //    顶边取版面框顶边；战术卡那边阵营行（原版 `ArmyTextTactc` y01 0.6935）本来就压在
            //    框顶(0.6691)下面，所以再让一行的高度，保证「阵营行在效果文字上面」。
            //    **下沿**贴着版面框底边（原版那块框的下沿就在最底下那颗稀有度宝石上方一点点）
            float descBot01 = unit ? UnitDescBottom01 : TacticDescBottom01;
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

            // ③ 阵营行（单位/战术都有）—— 名字的下方
            _army = Fill(_army, "army", CardText.Faction(d.faction), armyAt, nameW, ArmyFontSize, InkArmy, false);

            // ④ 兵种行（**只有单位卡**；战术卡成品卡图上没有这一行）—— 在卡面下部
            _race = Fill(_race, "race", unit ? d.subtype : null, UnitRaceAt, nameW, RaceFontSize, InkArmy, false);
        }

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
        /// 原版的 `NameTextUnit` / `DescTextUnit` 都开着 `m_enableAutoSizing`（`m_fontSizeMax` = 19 / 24），
        /// 长文本会自动缩 —— 我们这套是固定字号 + 折行，所以自己缩。
        /// ⚠️ 缩字号会改变折行结果：多跑几轮收敛（3 轮足够；每轮都缩，不会来回振荡）。
        /// </summary>
        static void FitToBox(TextMeshPro t, float maxWidth, float maxHeight, bool wrap)
        {
            if (t == null) return;
            for (int i = 0; i < 3; i++)
            {
                t.ForceMeshUpdate();
                var b = t.textBounds.size;
                float k = 1f;
                if (b.x > maxWidth && b.x > 0f) k = Mathf.Min(k, maxWidth / b.x);
                if (b.y > maxHeight && b.y > 0f) k = Mathf.Min(k, maxHeight / b.y);
                if (k >= 0.999f) break;
                t.fontSize *= k;
                if (wrap) TmpFont.SetWrapWidth(t, maxWidth);
            }
            t.ForceMeshUpdate();
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
                              bool opaque = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh != null ? mesh : Quad();
            var mr = go.AddComponent<MeshRenderer>();
            // ⚠️ 每层一份**新材质**：直接用共享材质再改 mainTexture 会让所有卡共用同一张贴图（踩过）
            mr.sharedMaterial = new Material(opaque ? OpaqueMaterial() : BaseMaterial())
            {
                mainTexture = tex != null ? tex : basis,
            };
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

        /// <summary>`Sprites/Default`：吃 alpha + 有 `_Color` 可以着色（模板，用的时候要 `new Material`）</summary>
        static Material BaseMaterial()
        {
            if (_quadMat == null) _quadMat = new Material(Shader.Find("Sprites/Default"));
            return _quadMat;
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
                if (r != null && r.sharedMaterial != null) r.sharedMaterial.color = c;

            if (_title != null) _title.color = (Color)InkName * c;
            if (_keywords != null) _keywords.color = (Color)InkDesc * c;
            if (_army != null) _army.color = (Color)InkArmy * c;
            if (_race != null) _race.color = (Color)InkArmy * c;
        }

        /// <summary>卡的底色（高亮态改它）。1 = 原色，0.55 = 置灰不可打出</summary>
        public void SetTint(Color c)
        {
            _tint = c;
            ApplyTint();
        }

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
            if (_rim != null)
            {
                // ⚠️ `Unplayable` **不描边**：置灰靠 `SetTint` 就够了，而描边是块比方大的矩形，
                //    在卡的透明角/透明边上会露出一圈灰白 —— 用户 2026-09-12 报的「手牌里有白边」
                //    就是它（付不起的卡 = Unplayable）。原版也没有这种硬边方框。
                bool show = s != CardHighlightState.Normal && s != CardHighlightState.Unplayable;
                _rim.enabled = show;
                if (show)
                {
                    var c = CardHighlight.ColorOf(s);
                    c.a = 0.95f;
                    _rim.sharedMaterial.color = c;
                }
            }
        }

        static readonly Color TintNormal = Color.white;
        public void ResetTint() { SetTint(TintNormal); }

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

        /// <summary>摆位：位置(世界) + 绕 Z 的倾角 + 缩放。手牌扇形/战场落位都调它。</summary>
        public void SetPose(Vector3 pos, float rotZ, float scale)
        {
            transform.localPosition = pos;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
            transform.localScale = Vector3.one * scale;
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

            // 板 = **立绘自己那块**（原版摆出来的大小），UV 正好整张图。
            // ⚠️ 2026-09-12 踩过：板取「卡框 quad」时 UV 会算出 [0,1] 之外的值（立绘比板小），
            //    贴图导入是 `Clamp` —— 立绘**最外圈像素被拉到卡片四边**，手牌上看起来就是
            //    「卡片四周多了一圈白边」（用户报的）。板 = 立绘自己那块，就没有出界的采样了。
            //    卡框的金属本来就盖在两侧，立绘不需要铺到框边。
            float w = aw * ArtCoverMargin, h = ah * ArtCoverMargin, cy = acy;
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
        /// ⚠️ false 那条（`FrameCutout`：卡框按遮罩挖洞、立绘只画一次）**还没调通**：
        ///    实测遮罩采样是错的，整张插图会碎成彩色马赛克（2026-09-13，见交接文档「未完成」）。
        ///    留在这里是因为思路对（没有重影），下个会话可以接着调 uv2 那条线。</summary>
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
            // 判据和立绘网格同源：都读 `ArtExtent`（避免「两处各算一遍」）。
            if (art != null)
            {
                float aw, ah, acy;
                ArtExtent(art, out aw, out ah, out acy);
                var uv2 = new Vector2[4];
                for (int i = 0; i < 4; i++)
                {
                    var v = m.vertices[i];
                    uv2[i] = new Vector2(0.5f + v.x / aw, (v.y - (acy - ah * 0.5f)) / ah);
                }
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

            var uv = FrameUv(tex);
            float s = Width / CardUnitW;                       // 卡单位 → 世界
            float spriteW = Mathf.Max(1f, uv.width * tex.width);
            float spriteH = Mathf.Max(1f, uv.height * tex.height);
            float fit = Mathf.Min(FrameUnitW / spriteW, FrameUnitH / spriteH);
            v = new Vector3(spriteW * fit * s, spriteH * fit * s, FrameUnitY * s);
            _frameQuadCache[tex] = v;
            return v;
        }

        // ==================================================================
        //  程序生成的卡面：整张卡（没有原版卡框时）/ 数值层（有卡框时）
        // ==================================================================

        /// <summary>
        /// 状态描边那张**羽化**的底图（64×64，四边各 12 px 渐隐、四角切掉）。
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
        static Texture2D InfoTexture(CardData d)
        {
            string key = "info|" + CacheKey(d);
            Texture2D cached;
            if (InfoCache.TryGetValue(key, out cached) && cached != null) return cached;

            var px = Blank(FaceW, FaceH);

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
                DrawCenteredAt(px, CostAt, d.cost.ToString(), 4, InkCost);
            }

            // ①b 文字区底下垫一层**柔和压暗** —— 原版卡面的卡名/效果文字是直接压在立绘上的
            //     （`Name and description` 那个块没有自己的底图），立绘亮的时候字就看不清。
            //     ⚠️ **这层压暗是我们加的**，不是原版的节点 —— 原版靠的是立绘本身在那一带偏暗。
            //     范围取文字块 `1.3×0.68 @(0,−0.7745)` 换算后的那一条（y01 0.60~0.85），
            //     两端用渐变淡出，免得在卡面上留一条硬边。
            DrawTextScrim(px, FaceW, FaceH, 0.60f, 0.85f, 0.08f, 0.92f, 120);

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

            // ④ 四个数值：画在卡框的宝石上（左下红/紫、右侧盾、右下绿）
            //    ⚠️ **战术卡不画数值** —— 原版战术卡面只有费用和稀有度（对照
            //       `D:/2/Warpforge部队卡片/<阵营>/4计策/*.png`）。原来不加这个判断，
            //       战术卡上会画出「近战 0 / 生命 0」这种根本不存在的数字。
            //    ⚠️ 护甲那面**盾牌底图**由 `Build` 的 `armourIcon` 图层画（原版 `pedestal_icon_armor`）；
            //       这里只画数字，且**只有 armor > 0 才画**（原版 `ArmourText` 有值时才有字）。
            if (d.isUnit)
            {
                DrawStatAt(px, MeleeAt, d.melee, InkMelee);
                if (d.ranged > 0) DrawStatAt(px, RangedAt, d.ranged, InkRanged);
                if (d.armor > 0) DrawStatAt(px, ArmourAt, d.armor, InkArmour);
                DrawStatAt(px, HealthAt, d.health, InkHealth);
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

        /// <summary>立绘：优先用原版立绘（`Art/cards/art_<卡名>.png`），没有再退回程序生成的占位图。
        /// 换自己的美术就是把同名文件换掉。</summary>
        static Texture2D ArtTexture(CardData d)
        {
            var real = CardArt.Portrait(d.artId);       // ⚠️ 同上：立绘按 id 取名
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

        static void DrawStatAt(Color32[] px, Vector2 at, int value, Color32 c)
        {
            DrawCenteredAt(px, at, Mathf.Max(0, value).ToString(), 4, c);
        }

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
