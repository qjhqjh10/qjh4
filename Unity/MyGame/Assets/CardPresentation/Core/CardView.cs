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

        /// <summary>🆕 2026-10-25（`K4`）：**基准值**（原版 `CardTextCountersController.SetText` 的
        /// `baseValue` 实参）—— 四个数值的**颜色**比的是「**当前 vs 基准**」，不是「新 vs 旧」
        /// （判据：9 参 `SetText` 的指令流 VA `0x18060C180` 把 `currentValue`/`baseValue` 传给
        /// `DoColorChange`，见 `CardView.StatColorVsBase` 的 doc）。
        /// <para>⚠️ **`statBasesKnown == false`（出厂值）= 基准未知** ⇒ 退回 2026-09-29 那套
        /// 「变了一下就涂一下」的近似；填了之后才是原版那套**持续色**
        /// （受伤的生命一直红、被 buff 的攻击一直绿，回到基准才白）。</para>
        /// <para>🔴 **这两个成员要由 `BattleDriver.ToCardData` 那一侧填**（`u.Card.Attack` / `u.Card.Health` /
        /// `u.Card.RangedAttack` / `u.Card.KwValue(Armour)` / `u.Card.Cost`）—— 那一步在 `Battle/**`，
        /// **不在卡面这一侧的文件所有权里** ⇒ 今天全工程**没有调用点**：机制已就位、接线另记一笔
        /// （报告 → `资料/普查产出_1025/K4_卡牌域复刻审查_执行报告.md`）。</para></summary>
        public bool statBasesKnown;
        /// <summary>见 <see cref="statBasesKnown"/>。基准值（卡面印刷值）；`statBasesKnown == false` 时一律忽略。</summary>
        public int baseCost, baseMelee, baseRanged, baseArmour, baseHealth;

        public string keywords;  // 已格式化的关键词串，直接画在卡面上
        public bool isUnit;      // 单位还是战术（战术不上战场）
        public Color frame;      // 阵营色 —— 占位卡面的边框、费用框用它
        public string faction;   // 阵营 key，用来找 `Art/cards/frame_<faction>[_tierN].png`
        /// <summary>稀有度（`common` / `rare` / `epic` / `legendary` / `special`）。
        /// 🔴 **2026-10-18 就地更正（铁律 5）**：本行原来写「**卡框按稀有度分四档**（原版就是）」——
        /// **不成立**：原版卡框按**升级档 `CardTier`** 取（`CardFramesSO.GetClanFrame(army, tier, cardType)`，
        /// 入参里没有稀有度）；稀有度只驱动底部那颗菱形宝石。判据全文 → `CardArt.TierOf` 的注释。
        /// 本字段现在只给**宝石**用（卡框档走 `CardProgress.Level(artId)` → `CardArt.Frame(…, tier)`）。</summary>
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
        /// —— 后者只记 `art_&lt;id>` 那批，拿它去问 `alt_*` 会恒假 ⇒ 异画在 opt-in 那条路上会**少一层**
        /// （⚠️ 出厂已经不走那条路了：前景层默认不建，见 `UseFrontLayer`）。</summary>
        public Texture2D artOverride;

        /// <summary>🆕 棋盘单位卡身上的 buff/debuff 徽标（原版 `BattleCardUI.boardTraitIcons`，最多 7 个）。
        /// **只有场上的单位画它** —— 手牌不画（理由与出处见 `Badges.cs` 文件头）。
        /// `null` 或空 = 一个不画，和原版「先把 7 个位全部 Toggle(false)」一致。</summary>
        public System.Collections.Generic.List<Badge> badges;

        /// <summary>🆕 2026-10-25（R16）：原版 `RawCardScript.spellType@0x90` —— **战术卡**的
        /// 「文字态 3/4」与「兵种行印不印」共用这一个字段。⚠️ `0` 是合法值（`Normal`）⇒
        /// 「未填」由 <see cref="spellTypeKnown"/> 表达（同 `statBasesKnown`）。判据与接线见类尾
        /// `TacticSubtypeLineShown` 的 doc。</summary>
        public int spellType;
        /// <summary>见 <see cref="spellType"/>：`true` 时 `spellType` 才算数（`0` = `Normal` 也合法）。
        /// 🔴 今天全工程 **没有任何构造点**置它（生产者 `BattleDriver.ToCardData` 在 `Battle/**`）。</summary>
        public bool spellTypeKnown;

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
    /// 卡面左下那两颗攻击数值格的老三态读口（`None / Melee / Ranged`）。
    /// <para>🔴 **2026-10-25（`CF`）：它已经只是 <see cref="CardView.SetOwnAttackType"/> 的**同义映射**，
    /// ⛔ 不再表达「目标卡该亮哪一颗」。**原版没有「拿攻击方的打法去点亮**目标**卡底光」这条语义**
    /// —— 底光与数值放大都由**这张卡自己的** `attackType` 驱动（判据 →
    /// `资料/普查产出_1025/J2_判据_卡面高亮族与渲染残项.md` §A1·6，两条触发链逐句读过）。</para>
    /// <para>保留它只为一件事：`Battle/BattleDriver.HighlightTargets` 与 `Editor/BattleScene` 的自检
    /// 还在读 `CurrentGem` / 调 `SetTargetGem`。⚠️ **那几处要改接 `SetOwnAttackType`**
    /// （数据源 = 引擎的 `UnitState.LastAttackType`）—— 那一跳在 `Battle/BattleDriver.cs`，不在本文件。</para>
    /// </summary>
    public enum TargetGem { None, Melee, Ranged }

    /// <summary>原版 `AttackTypes`（`dump.cs:24798-24807`）—— **数值逐位照抄**：
    /// `None 0 / Melee 1 / Ranged 2 / Spell 3 / Active 4`。
    /// <para>卡面只有 **Melee / Ranged 两档**有 `HighlightAttackType` 实例（每卡恰好 2 个，
    /// `Spell` / `Active` 没有实例）⇒ 后两档传进来时卡面底光与数值**都不动**。</para>
    /// <para>⚠️ 与 `Battle/AttackSelector.cs` 的 `AttackKind`（`None 0 / Melee 1 / Ranged 2 / Ability 4`）
    /// **数值天然兼容**（`Ability` = 4 = `Active`）—— 接线时直接整数转即可，别另写一张映射表。</para></summary>
    public enum AttackTypes { None = 0, Melee = 1, Ranged = 2, Spell = 3, Active = 4 }

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

    /// <summary>
    /// **这张卡建在【哪一棵树】里** —— 原版有几个卡面节点**逐树不同值**，与 <see cref="CardFace"/>
    /// （按**状态**决定画哪几层）是**两条正交的轴**，别合并。
    ///
    /// 🔴 **为什么要有这一档（铁律 5·c 的落地）**：`NameTextUnit` 是**全卡面唯一的双值文字节点** ——
    ///   扫 13 arena × 8 实例 = **104 个**：`fontSize = 16` **65 个**（= 每场 5 张，正是**放大窗**那一族）、
    ///   `fontSize = 19` **39 个**（多卡窗 / debug 卡池 / 墓地 各 1）；手牌·场上的 `BattleCardUI` 预制体
    ///   **也是 19**。⇒ **19 是绝大多数情况，只有【放大窗】那 5 张是 16**（我们原来窗内也按 19 画 ⇒ **大 18.75%**）。
    ///   同族的阴影色也是三个值：材质默认 `0.604`（卡面）/ `0.53333336`（**卡背**，另一份材质）/
    ///   **`InitializeCardForDisplay` 运行时显式设 `(0,0,0,0.8)`**（**放大窗**）。
    ///
    /// 出处 = `资料/普查产出_1015战斗/C1_逐格对账_卡面放大窗与卡面渲染.md` 表三·1 / 表三·2。
    /// 🆕 **2026-10-15 现核**：C1 表三·2 留了一个前提「`DoColorChange` 写的若不是 `_ShadowColor` 则本条不成立」——
    ///   已从反编译 + `GameAssembly.dll` + `global-metadata.dat` 解开：
    ///   `Card2DController..ctor` 把 `Shader.PropertyToID(<字面量>)` 存进 `+0x80`/`+0x84`，
    ///   两个字面量的 **il2cpp token** = `0xA0009B75` / `0xA0009A51` ⇒ `(token & 0x1FFFFFFF) >> 1`
    ///   = **19898 / 19752** ⇒ 查 `global-metadata.dat` 的 stringLiteral 表 = **`_ShadowColor` / `_Outline`**。
    ///   （`ChangeShadowColor` 走 `+0x80`、`ChangeHighlightColor` 走 `+0x84` ⇒ **阴影色确实是 `_ShadowColor`**，前提成立。）
    ///   ⚠️ 解 token 的通用办法见 `资料/命令速查.md`「`Shader.PropertyToID` 的字面量怎么解」。
    ///
    /// ⚠️ **只覆盖【战斗 13 场】这一侧** —— 菜单侧那扇详情窗（`Shell/CardDetailPopup.cs`）**用的是哪一棵树、
    ///   值是多少，还没核**（C1 的扫描范围是 `battlearena1/2/3` 那 13 份）。⇒ 那一扇**暂按 `Standard`**，
    ///   另立账待核（铁律 11：先记着，查清再对齐）。
    /// </summary>
    public enum CardUse
    {
        /// <summary>**除战斗放大窗以外的一切**：手牌 · 场上 · 多卡展示窗 · 墓地日志 · 卡池 / 收藏 / 卡组编辑。
        /// 原版 `fontSize = 19`、阴影 alpha = 材质默认 `0.604`。也是默认档。</summary>
        Standard = 0,
        /// <summary>**战斗放大窗**（`Card Display Window/Card Display/Cards`）。原版那 5 张
        /// `fontSize = 16`，且 `InitializeCardForDisplay` 运行时显式把阴影设成 `(0,0,0,0.8)`。</summary>
        ZoomWindow = 1,
    }

    /// <summary>原版 `CardTextsController` 的**九个文字态**（2026-10-25 `CF-6`）。
    /// <para>出处（一手）：`dump.cs` 的同名枚举 + 卡 prefab 上 `CardTextsController` 的 9 份
    /// `objectsToActivate` / `objectsToDeactivate` 名单（逐态表 → `资料/普查产出_1025/
    /// J2_判据_卡面高亮族与渲染残项.md` §B6）。**数值与枚举名逐位照抄**（`None` 是 0）。</para>
    /// <para>逐态的「哪些文字件显示」：</para>
    /// <list type="bullet">
    ///   <item>1 `MinionWithDescription`：卡名 + 效果文字 + 兵种行（+ Big 暗底）</item>
    ///   <item>2 `MinionWithoutDescription`：卡名（无 desc 那件）+ 兵种行（+ Small 暗底）</item>
    ///   <item>3 `Tactic` / 4 `TacticWithRace`：战术卡名 + 战术效果文字（4 带兵种行）</item>
    ///   <item>5 `MinionWithDescOnlyDescription`：**只有** Big 效果文字（+ Big 暗底）</item>
    ///   <item>6 `MinionWithoutDescriptionSimplifiedView`：**只有** Big 卡名（+ Small 暗底）</item>
    ///   <item>7 `TacticOnlyName`：**只有** Big 战术卡名（**无暗底**）</item>
    ///   <item>8 `TacticOnlyDescription`：**只有** Big 战术效果文字（**无暗底**）</item>
    /// </list></summary>
    public enum CardTextState
    {
        None = 0,
        MinionWithDescription = 1,
        MinionWithoutDescription = 2,
        Tactic = 3,
        TacticWithRace = 4,
        MinionWithDescOnlyDescription = 5,
        MinionWithoutDescriptionSimplifiedView = 6,
        TacticOnlyName = 7,
        TacticOnlyDescription = 8,
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

        // 🔴 **2026-10-20（`W58`）：可行动/可选目标那颗底光在原版里是【独立子节点】**，不是「直接画在
        //    数值中心」。判据 = 原版 prefab 的 `Highlight` / `Highlight ranged` Transform（3DBody 空间，
        //    逐级 `Base Attack Counters(0.139241) × Melee/Range Attack Container(7.1817865)` 现算）：
        //    · `Highlight`       局部 (−0.006, 0.005) ⇒ 3DBody **(−0.6810, 0.7599)**；
        //      同法算出 `Melee AttackText` 中心 = (−0.6800, 0.7399) ⇒ 底光偏 **(−0.001, +0.020)**
        //    · `Highlight ranged` 局部 (+0.011, 0)   ⇒ 3DBody **(−0.4230, 0.4207)**；
        //      `Range Attack Text` 中心 = (−0.4340, 0.4057) ⇒ 偏 **(+0.011, +0.015)**
        //    （`HighlightAttackType.highlightObject` 指向的就是这两个节点；尺寸/层级不变）
        //    两张 glow 的缩放（0.6548 / 0.6557）我们仍按 `GlowOfCard` 一套走 —— 两者差 0.14%，可忽略。
        static readonly Vector2 BoardMeleeGlowAt  = new Vector2(BoardMeleeAt.x  - 0.001f / CardUnitW,
                                                               BoardMeleeAt.y  - 0.020f / CardUnitH);
        static readonly Vector2 BoardRangedGlowAt = new Vector2(BoardRangedAt.x + 0.011f / CardUnitW,
                                                                BoardRangedAt.y - 0.015f / CardUnitH);

        // ==================================================================
        // 🔴 **2026-10-18（W15）—— 2D 卡面上那三个数 / 费用六边形【跟着卡框凹槽走】，⛔ 不再用固定常量。**
        //
        // 🔴 **2026-10-19（`A2138`）就地订正 —— 下面这段「根因」只对【改画框之前】成立，现在已不成立：**
        //    原版 CardFrame 是 `m_UseSpriteMesh=1` ⇒ 铺进固定矩形的是 **sprite rect（常量 171,11,682,998）**，
        //    不是 alpha bbox（判据与参考实拍反证见 `FrameSpriteRectX`）。映射按 sprite rect 之后
        //    **凹槽落点逐帧恒定** —— 这正是原版敢用「固定容器常量」的原因；实拍里数字正落在凹槽中心
        //    （4 张泰伦卡逐像素核：dx −1.5 / −1.0 / +0.5 px，按 m_Rect 预测 0.0 / −1.7 / +0.9）。
        //    ⇒ `FaceStatAt01` / `FaceGemAt01` 现在算出来就落在 prefab 常量附近（melee x −0.8362 vs −0.836）；
        //      **保留「跟着凹槽走」无害**（同一份判据、换 uv 也不再漂），但⛔别再拿下面「x01 跨 11%」当理由 ——
        //      那是 alpha-bbox 映射时代的现象。
        //
        //    用户点名两条：① 近战/远程/血量三个圈里的数字**不在图标中央**；
        //                  ② 费用图标**太靠下**、没贴住右上角的阵营徽记。
        //
        // **根因（实测，不是猜的）**：`MeleeAt/RangedAt/HealthAt/CostAt` 取的是**原版 prefab 里四个容器**
        //   的位置（容器固定不动），而我们的卡框是**按贴图 alpha bbox 拉伸进固定矩形**画出来的
        //   （`FrameMesh` + `FrameUv` + `FrameQuad`）。凹槽（红圆/紫圈/绿框/阵营徽记）是**卡框贴图里画着的**，
        //   于是它的**卡单位落点 = f(这张卡框的 uv)**。
        //    · 15 个阵营的卡框都画在同一张 **1024² 画布**、凹槽**像素位置完全一致**（逐张实测）；
        //    · 但每个 tier 的 alpha bbox 宽窄不同（`frame_uv.json` 里 `sw` 0.594～0.659）⇒ 拉伸比不同；
        //    · ⇒ 同一个凹槽在不同卡框上落到**不同的卡单位**：x01 跨 0.021～0.132（= **11% 卡宽**）。
        //    ⇒ 固定常量只可能对一种卡框正确。原来那组常量对 `frame_ultramarines_tier3` 也偏
        //      「上 7～12 贴图像素」，对 tier1/2 偏得更多。
        //
        // **判据（2026-10-18 逐张实测；脚本口径与全表 → `资料/普查产出_1018/W15_卡面数值与费用图标.md`）**：
        //   · 凹槽圆心（**贴图像素**，y 从贴图顶部数；15 个阵营 tier3 各测一次，括号里是极差）：
        //       近战红圆 **(257.5, 862.5)**（±2）· 远程紫圈 **(335.0, 929.5)**（±3）·
        //       生命绿框 **(734.0, 916.0)**（±1）
        //     （测量法：红=亮红环最小二乘圆拟合；紫=品红环拟合；绿=绿光连通域 bbox 中心。）
        //   · 阵营徽记圆：**中心 (723.5, 158.0)、半径 77.5** ⇒ **底边 y = 235.5**
        //     （13 个阵营各拟合一次；各阵营画形不同、半径 69～83，底边 225～248，均值 235）。
        //   · 原版实拍（`C:\Users\qjh36\Desktop\warpforge参考图\点击卡片查看详情的参考.png`）：
        //       费用八边形 **上沿贴着徽记底边**（1577×887 里徽记底 ≈195、八边形上沿 ≈200）、
        //       **水平中心与徽记同心**（两者 x 都是 ≈950）。
        //   · 三个圈里的数字**原版就是居中的**（同一张实拍：红圈 (537.5,617.5) vs 数字 (536,615)；
        //       PnP 成品卡 `Ultramarines/3部队/Warpforge_02_Heavy-Intercessor-2.png` 同结论）。
        // ==================================================================
        const float FrameMeleePx  = 257.5f, FrameMeleePy  = 862.5f;
        const float FrameRangedPx = 335.0f, FrameRangedPy = 929.5f;
        const float FrameHealthPx = 734.0f, FrameHealthPy = 916.0f;
        /// <summary>阵营徽记圆（**13 个阵营各拟合一次的均值**：各阵营半径 69～83、底边 225～248）。
        /// <para>⚠️ **2026-10-18：费用八边形不拿它当锚点** —— 它是**均值**，逐阵营本来就有偏差
        /// （半径就跨 69～83），拿均值去定位 = **逐阵营系统性偏**（正是
        /// `资料/普查产出_1018/R_卡面元素对账A_6阵营.md` / `…B_7阵营.md` 里「费用 x 逐阵营 −14…−37」那条）。</para>
        /// <para>🔴 **2026-10-19（主对话裁定）就地订正**：本行 2026-10-18 那条「现行 = 逐帧实测徽记下缘
        /// （W27）」**已作废** —— 费用回到 **prefab 固定容器 `(0.788, 0.666)`**（判据与换算见
        /// <see cref="FaceCostAt01"/>）。这组均值**只作对照/存档**，本文件**没有任何代码**再用它。</para></summary>
        const float FrameBadgePx  = 723.5f, FrameBadgePy  = 158.0f, FrameBadgeRad = 77.5f;

        /// <summary>🔴 **W27（2026-10-18）遗留 —— 2026-10-19 主对话裁定后【已作废】，仅作历史/复核。**
        /// <para>费用八边形（底板 + 里面那个数字）在【卡框贴图】上的**旧落点**（贴图像素，y 从顶部数）。
        /// W27 曾把它降级为「量不出徽记」的兜底；现在 <see cref="FaceCostAt01"/> **不再调用它**
        /// （原版是固定容器）。</para>
        /// <para>**旧推导（存档）**：PnP 成品卡 12 张（12 个阵营各 1 张）逐张反算 ——
        ///   ① 每张 PnP 取「近战红圆 / 远程紫圈 / 生命绿框」三个凹槽的 **bbox 中心**，
        ///      与卡框贴图上同名的三处（257.5,862.5 / 335,929.5 / 734,916）配准，
        ///      求**各向同性相似变换**（scale + 平移，无旋转 —— 印刷品就是同一张画的等比缩放）；
        ///   ② 拟合优度：三个凹槽残差 **≤3.5 px**、第 4 处（宝石凹槽 511,950）**≤3 px**
        ///      ⇒ 模型站得住（`R_卡面元素对账B` 说的「纵向 6% 压缩」是它拿**卡体 alpha 矩形**
        ///      当尺子引入的：那矩形被立绘外溢撑大、逐张不同，不是版式差）；
        ///   ③ 再用这个变换把 PnP 上量到的**费用八边形可见中心**反算回贴图像素：
        ///      **x = 750.0 ± 1.0、y = 304.6 ± 2.3**（12 张的均值±标准差）。
        ///      我们原来那套在同一批卡上是 **(723.5, 309.4±0.8)** ⇒ **右移 ≈28、上移 ≈4 贴图像素**。</para>
        /// <para>⚠️ 这里写的是**网格中心**，不是**可见中心**：`Card_Frame_Cost_Icon.png`（248×244）
        ///   的不透明 bbox 是 x3..238 / y3..238 ⇒ 可见内容比贴图中心**左 3 px、上 1 px**
        ///   （= 左 1.7 / 上 0.6 贴图像素）⇒ 网格中心要比目标可见中心**右 1.7、下 0.6**。</para>
        /// <para>⚠️ 为什么不能继续用徽记均值 `FrameBadgePx/Py/Rad`：PnP 12 张反算出来的费用 x
        ///   离散度只有 **1.0 px** ⇒ 费用相对**卡框本身**是**固定**的。
        ///   🔴 **2026-10-19 就地订正**：本行后面原写「**逐帧实测**（W27）同理：150 张卡框的徽记下缘跨
        ///   177..280 贴图像素，固定值必然逐帧偏」——**这句已作废**。原版费用是 **prefab 固定容器**
        ///   `(0.788, 0.746)`（本文件卡根空间 `(0.788, 0.666)`），**不随卡框贴图变**；
        ///   而且凹槽/徽记在贴图上的像素位置本身是恒定的（见 `FrameSpriteRectX` 那段）。
        ///   W27 的逐帧规则只由一张实拍反推，逐帧差 0.16 卡单位（7% 卡宽）⇒ 主对话裁定**作废**。</para></summary>
        const float FrameCostPx = 751.7f, FrameCostPy = 305.2f;

        /// <summary>卡框贴图上任意一点（**贴图像素**，y 从顶部数）→ **卡面归一化坐标**（x 左、y 顶）。
        /// 走的就是画框那一条链：`FrameUv`（贴图不透明块）→ `FrameQuad`（固定矩形）——
        /// 与 `FrameMesh` 逐字同一套，别在这儿另写一份换算。
        /// <para>`frameTex == null` / uv 退化 ⇒ 返回 **(NaN, NaN)**，调用方退回固定常量
        /// （⛔ 不静默给一个假的点）。</para></summary>
        static Vector2 FramePxAt01(Texture2D frameTex, float px, float py)
        {
            if (frameTex == null || frameTex.width <= 0 || frameTex.height <= 0)
                return new Vector2(float.NaN, float.NaN);
            Rect uv = FrameUv(frameTex);
            if (uv.width <= 1e-5f || uv.height <= 1e-5f) return new Vector2(float.NaN, float.NaN);
            float u = (px + 0.5f) / frameTex.width;
            float v = 1f - (py + 0.5f) / frameTex.height;
            float fu = (u - uv.xMin) / uv.width;        // 0..1 = 画出来的那块贴在固定矩形里的位置
            float fv = (v - uv.yMin) / uv.height;
            float x = (fu - 0.5f) * FrameUnitW;         // 卡单位（中心原点）
            float y = (fv - 0.5f) * FrameUnitH + FrameUnitY;
            return new Vector2(0.5f + x / CardUnitW, 0.5f - y / CardUnitH);
        }

        /// <summary>2D 卡面上第 `s` 个数值**该画在哪**（卡面归一化，x 左 / y 顶）。
        /// 红圆/紫圈/绿框取卡框贴图里的凹槽圆心（见上面那一段）；**护甲不跟着走** ——
        /// 那面盾是**我们自己的图**（`pedestal_icon_armor`），卡框里没有它的凹槽 ⇒ 仍用原版容器位置。</summary>
        static Vector2 FaceStatAt01(int s, Texture2D frameTex)
        {
            float px, py;
            switch (s)
            {
                case StatMelee:  px = FrameMeleePx;  py = FrameMeleePy;  break;
                case StatRanged: px = FrameRangedPx; py = FrameRangedPy; break;
                case StatArmour: return ArmourAt;
                default:         px = FrameHealthPx; py = FrameHealthPy; break;
            }
            var at = FramePxAt01(frameTex, px, py);
            if (float.IsNaN(at.x))
            {
                // 没有卡框（占位卡面）：退回原版容器常量
                return s == StatMelee ? MeleeAt : s == StatRanged ? RangedAt : HealthAt;
            }
            return at;
        }

        /// <summary>卡面底部那颗**稀有度宝石**该画在哪（卡面归一化，x 左 / y 顶）。
        /// **跟着当前卡框的宝石凹槽走**（与 `FaceStatAt01` 同一套：贴图凹槽像素 → 卡面归一化）；
        /// 没有卡框（占位卡面）时退回旧固定常量（那组只对 `frame_saimhann_tier3` 一档准）。</summary>
        static Vector2 FaceGemAt01(Texture2D frameTex)
        {
            var at = FramePxAt01(frameTex, FrameGemPx, FrameGemPy);
            if (float.IsNaN(at.x))
                return new Vector2(0.5f + RarityGemX / CardUnitW, 0.5f - RarityGemY / CardUnitH);
            return at;
        }

        /// <summary>费用八边形（底板 + 里面那个数）的**中心**（卡面归一化，x 左 / y 顶）。
        ///
        /// <para>🔴 **2026-10-19（主对话裁定 · 铁律 3/5·c）—— 回到原版 prefab 的【固定容器】`(0.788, 0.666)`。**
        /// 判据（一手，`资料/普查产出_第十八会话/D2_卡框几何量法.md` §三全表）：
        ///   · `Card Info/Cost Container` 的 `m_AnchoredPosition = (0.788, 0.746)`（anchor/pivot 都 (0.5,0.5)），
        ///     它挂在**纯 `Transform`** `Card Info` 下 ⇒ 锚点基准 = `Front` 原点的**局部**坐标；
        ///   · **坐标换算（必写清）**：本文件各层的常量一贯是「**`Front` 局部**」——
        ///     证据 = `FrameUnitY = −0.03` 正好等于 D2 的 `CardFrame (0, 0.05)` **再减 0.08**
        ///     （0.08 = `Front` 在 2DCard 空间的 apos；`MeleeAt` 等也逐一对得上：D2 (−1.110) → 本文件 −1.190）。
        ///     🔴 **2026-10-25（`M2`）补一句**：上面「卡根空间 = `Front` 局部」**已不再成立** ——
        ///     当天补出了 `Front` 那一级节点（+0.08，见 `FrontAposY`）⇒ 这些常量仍是 `Front` 局部值
        ///     （**所以本段所有换算一个字节都不用改**），但它们画出来的位置比改前**整体高 0.08**。
        ///     ⇒ 费用中心 = **(0.788, 0.746 − 0.08) = (0.788, 0.666)** 卡单位
        ///     ⇔ 归一化 **(0.8766, 0.3001)**，**正是本文件的 `CostAt`**（下方直接返回它）。
        ///   · 独立旁证：`A2138` 之前的 PnP 反算固定点 `FrameCostPx/Py = (751.7, 305.2)` 在
        ///     **sprite-rect 画框映射**下落点 = **(0.791, 0.650)** —— 与 prefab 只差 **0.016** 卡单位，
        ///     两条互不相干的来源（序列化字段 / PnP 成品图）互证。
        ///   · `Cost Container Big` = `(0.788, 0.723)` ⇒ 卡根空间 `(0.788, 0.643)` = **同一 x、下移 0.023**
        ///     （= `CostBigDropUnit`，未动，在 <see cref="CostAt01"/> 统一加）。</para>
        ///
        /// <para>⛔ **W27（2026-10-18）的「可见右缘贴卡框 alpha 右缘 / 可见上缘贴徽记下缘」作废** ——
        /// 它只由**一张泰伦实拍**反推，逐帧 `cx` 会差 **0.16 卡单位（7% 卡宽）**、且把费用**向内拉 0.15**，
        /// 与用户原话「费用图标太下面了」方向相反。单张截图不能压过 prefab 序列化常量。
        /// 作废那套实现（`FrameCostPx/Py` · `FramePxYToCard` · `TryBadgeBottomPx` · `_badgeTable` ·
        /// `CostBadgeGapPx` · `CostVisRightFrac/TopFromCenter`）**保留在本文件里当历史/复核材料**，
        /// 但**本函数不再调用**（改动前它算出的是 0.7477…0.9057 的逐帧值）。</para>
        ///
        /// <para>⚠️ 底板层（`Build` 的 `_costBg`）、数字（`InfoTexture`）、命中区（`HitFace`）**都走这一个函数**
        /// ⇒ 三者天然同源（画面/点击/烘焙不会各偏一套）。`frameTex` 参数保留只为**调用点不变**
        /// （原版这份几何与卡框贴图无关 —— 逐帧跟着卡框走正是 W27 错的地方）。</para></summary>
        static Vector2 FaceCostAt01(Texture2D frameTex)
        {
            // `CostAt` = 归一化 (0.8766, 0.3001) ⇔ 卡单位 (0.788, 0.666)（`CostAt` 的注释里有换算），
            // 就是原版 `Cost Container` 的固定容器位置 —— **判据只此一处**，别在这里再写一份字面量。
            return CostAt;
        }

        // ==================================================================================
        //  🔴 2026-10-18（`A1549`）：费用格的**两档** —— `Cost Container` ↔ `Cost Container Big`
        //
        //  **判据（原版一手，逐字段现读）**
        //    · 控制器 = `CardTextCountersController`（`MonoScript_-1746985122272567298`；预制体实例
        //      `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_6320885271709195200.json`）
        //      —— `containers[]` 每项带 `activationRules`，枚举在桩 `CardTextCountersController.cs`：
        //        `Normal = 0 · SmallScreen = 5 · SimplifiedView = 10 · NoSimplifiedView = 15`
        //      实读（同上 JSON）：
        //        · `Cost Container`（GO `3506277890724895680`）= **[0, 15]**
        //        · `Cost Container Big`（GO `-4971020442760602688`）= **[5, 10]**
        //    · `Toggle(value, simplifiedView)`（`CardTextCountersController__Toggle.c` 逐句读）：
        //        规则 0 ⇒ 开；规则 5 ⇒ 看静态 `GameStaticData.smallScreenUI`（+0x11c）；
        //        规则 10 ⇒ 看 `simplifiedView`；规则 15 ⇒ **`!simplifiedView`**。
        //      `value == false`（整格关掉）⇒ 两个容器都关。
        //    · `simplifiedView` 的实参：`BasicCardUI.SetRawCardData` / `ToggleStats` **全部传 0**
        //      （`BasicCardUI__SetRawCardData.c:84,91,96,98,100`），而
        //      `CardTextCountersController.ToggleSimplifiedView` **在反编译集里没有任何调用者**
        //      ⇒ **今天真正会切的那一档 = `smallScreenUI`** —— 我们的等价物 =
        //      `CardPresentation.SmallScreenUI.Enabled`（全工程唯一来源，见那个类）。
        //  **几何（同包 `RectTransform/` 实读；两档逐项 ×1.2）**
        //    · 背景 `Cost Background`：0.4891×0.4809 → **0.58692×0.57708**
        //    · 数字框 `CostText`：0.38×0.35 → 0.456×0.42（同一张图 / 同一个锚点偏移）
        //    · 容器 apos：**(0.788, 0.666) → (0.788, 0.643)** ⇒ **x 不动、y 下移 0.023 卡单位**
        //    · 两档背景是**同一张 sprite**（Image MB 逐字段相同：`m_Type=0` · `m_PreserveAspect=0` ·
        //      `m_Color` 白）⇒ **只放大、不换图**。
        //  ⚠️ 两档的关系（2026-10-19 复核仍成立）：原版两档**容器 x 相同（0.788）**、y 差 0.023
        //     ⇒ 大号档就是在同一个中心上放大（右缘会跟着往外）。⛔ 别把「可见右缘」再重新贴一次边
        //     —— W27 那条「右缘贴卡框 alpha 右缘」已作废（见 `FaceCostAt01`）。
        // ==================================================================================
        /// <summary>大号费用格相对普通档的**尺寸倍率**（原版两档逐项实测 = 1.2）。</summary>
        public const float CostBigScale = 1.2f;
        /// <summary>大号费用格相对普通档**下移多少卡单位**（原版容器 apos y 0.666 → 0.643）。</summary>
        public const float CostBigDropUnit = 0.023f;
        /// <summary>现在用哪一档（= 原版 `GameStaticData.smallScreenUI` 那一路；判据见上面那一段）。</summary>
        public static bool CostBig { get { return SmallScreenUI.Enabled; } }
        /// <summary>费用格尺寸倍率（普通档 1.0 / 大号档 1.2）。</summary>
        static float CostScale { get { return CostBig ? CostBigScale : 1f; } }

        /// <summary>费用格在卡面上的**实际**落点（两档统一走这一个口）：
        /// `FaceCostAt01`（原版 prefab **固定容器** `(0.788, 0.666)`；W27 的贴徽记规则已于 2026-10-19 作废）
        /// + 大号档的**下移**（原版容器 apos 差 0.023）。
        /// ⚠️ 底板层 / 烘进 `_info` 的数字 / 命中区**三处都走它**（「只此一处」口径）。</summary>
        static Vector2 CostAt01(Texture2D frameTex)
        {
            var at = FaceCostAt01(frameTex);
            if (CostBig) at.y += CostBigDropUnit / CardUnitH;   // 向下 ⇒ at01 的 y（从顶部数）变大
            return at;
        }

        /// <summary>贴图像素 y（**从顶部数**）→ 卡单位（y 向上）。与 <see cref="FramePxAt01"/> 同一条
        /// 换算链（`FrameUv` + `FrameQuad`），只取纵坐标。UV 退化 ⇒ `NaN`（调用方退回旧常量）。
        /// <para>⚠️ **W27（2026-10-18）遗留**：2026-10-19 主对话裁定「费用回到 prefab 固定容器」之后
        /// **本函数没有调用者**了（原调用点 = `FaceCostAt01`）。保留作历史/复核材料，⛔ 不要拿它再搭逐帧规则。</para></summary>
        static float FramePxYToCard(Texture2D frameTex, float py)
        {
            if (frameTex == null || frameTex.width <= 0 || frameTex.height <= 0) return float.NaN;
            Rect uv = FrameUv(frameTex);
            if (uv.height <= 1e-5f) return float.NaN;
            float v = 1f - (py + 0.5f) / frameTex.height;
            float fv = (v - uv.yMin) / uv.height;
            return (fv - 0.5f) * FrameUnitH + FrameUnitY;
        }

        /// <summary>这张卡框的**阵营徽记下缘**（贴图像素，y 从顶部数），数据 = `frame_uv.json`。
        /// 表里没有 / 数值不合理（≤0 或 ≥贴图高）⇒ `false`（调用方走兜底）。
        /// <para>⚠️ **W27 遗留**：2026-10-19 起**没有调用者**（原调用点 = `FaceCostAt01`）。
        /// `frame_uv.json` 里的 `bx/by/br/bb` 四项**仍然照常解析/保留**（数据无损，将来复核用）。</para></summary>
        static bool TryBadgeBottomPx(Texture2D frameTex, out float py)
        {
            py = 0f;
            if (frameTex == null) return false;
            EnsureUvTable();
            Vector4 b;
            if (_badgeTable == null || !_badgeTable.TryGetValue(frameTex.name, out b)) return false;
            if (b.w <= 0f || b.w >= frameTex.height) return false;
            py = b.w;
            return true;
        }

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
        //   战术卡 `ArmyTextTactc`(0,+0.13) → y01 **0.6935**；`RaceText`（在 `Tactic Container` 下）
        //   (0,−0.490) → y01 **0.8796** —— ⚠️ **老注释写「战术卡没有兵种行」已作废**：战术卡印不印
        //   那一行由 `SubtypeLine` 判（9 个具名系列印、其余不印），**印的时候用 `TacticRaceAt`**。
        // ⚠️ 颜色：A3 表把阵营行记成**白**、兵种行记成**橙**；但两张成品卡（`Warpforge_02_Heavy-Intercessor-2.png`、
        //    `pariah.png`）上**两行都是橙**（(0.9922,0.6039,0.3451) 同一支）。这里按**成品卡**走，
        //    要是以后拿到数字版实况发现阵营行是白的，改这一处即可。
        static readonly Vector2 UnitArmyAt   = new Vector2(0.5000f, 0.6394f);
        static readonly Vector2 UnitRaceAt   = new Vector2(0.5000f, 0.8682f);
        static readonly Vector2 TacticArmyAt = new Vector2(0.5000f, 0.6935f);

        // 🔴 **2026-10-24（`E24`）：兵种行有【两颗】原版节点**（铁律 5·c 的「状态 → 参数」表）。
        //  判据（一手，逐字段实读；三份 bundle **逐位相同** —— `bundle_battleprefabs_vfxandmisc_assets_all` /
        //  `bundle_staticgeneralassets_assets_all` / `bundle_menus_assets_all`）：
        //
        //  | 状态（`CardTextsController.states[]`） | 卡型 | 用哪颗 | 父容器 | sizeDelta | apos.y | 我们的 y01 |
        //  |---|---|---|---|---|---|---|
        //  | 1 / 2（有/无描述） | unit · hero | `RaceText` | `MinionOrWarlord Container` | 90×21 | **−0.452** | **0.8681** |
        //  | **4** | **tactic · defence** | `RaceText` | `Tactic Container` | **155×19** | **−0.490** | **0.8796** |
        //  | 3 / 7 / 8 | tactic 其余三档 | —（`RaceText` 被 `objectsToDeactivate` 关掉） | — | — | — | 不印 |
        //  | 5 / 6 | 大号档 | —（`RaceText` 关） | — | — | — | 不印 |
        //
        //  状态怎么选（一手）：`CardTextsController__SetTexts.c`——
        //    `state = (cardType ∈ {0x14(20), 0x28(40)}) ? 3 + (param_7 != 0) : 1 + IsNullOrEmpty(desc)`；
        //    `param_6 = RawCard.+0x94`（= 卡型）、`param_7 = RawCard.+0x90`。
        //    ⇒ **卡型 20 的卡（战术【和】防御，见 `TacticArtScale` 那条：「防御卡的 SO `cardType` 也是 20」）
        //      命中 `Tactic Container` 那颗**；单位/督军命中 `MinionOrWarlord Container` 那颗。
        //  ⚠️ **我们印不印那一行**是另一条判据（`SubtypeLine`：9 个具名系列 + 战略/防御恒印），
        //     **一条都不用改** —— 这里只决定「印的时候摆在哪」。两条不要合并：原版 `state 3` 是
        //     「战术卡 + 有描述 ⇒ 不印」，而我们的判据是**照 PnP 成品卡图逐张核**出来的（`SubtypeLine`）。
        //  ⚠️ 老注释说「战术卡没有兵种行」—— **已作废**（`SubtypeLine` 的 doc 已订正过：战术卡印不印看 subtype）。
        static readonly Vector2 TacticRaceAt = new Vector2(0.5000f, 0.8796f);
        /// <summary>效果文字那块**版面框的顶边**（y01）—— 中心减半高：
        /// 单位 0.7323 − 0.7616/2/3.3313 = 0.6180；战术 0.7876 − 0.7898/2/3.3313 = 0.6691</summary>
        const float UnitDescTop01 = 0.6180f;
        const float TacticDescTop01 = 0.6691f;
        /// <summary>版面框的**底边**（中心 + 半高）：单位 0.7323+0.1143、战术 0.7876+0.1185。
        /// <para>🔴 **2026-10-24（`E16`）口径更新**：它现在是效果文字那块**下的门**（`FaceDescBand01`
        /// 的 `botGuard`），**不再是「文字的下沿」** —— 摆位改成「块中心 = 原版矩形中心」之后，
        /// 文字块离这条线还有一段距离。⚠️ 但它仍然是**矩形**的底边，⛔ 意义没变。</para></summary>
        const float UnitDescBottom01 = 0.8466f;
        const float TacticDescBottom01 = 0.9061f;

        /// <summary>**卡面上要印兵种行/类型行**时，效果文字那块的**下门**（y01，从卡顶算）。
        /// 比 `UnitDescBottom01` 再往上让一点 —— 让出来那点是**量出来的，不是抄来的**：
        /// 🔴 2026-09-15 逐条比对 `_tmp_view/cardface/Lychguard.png` 与 PnP 成品卡：
        ///   原值 0.8466 时，**效果文字的可见下沿在 0.808、类型行的可见上沿在 0.803 ⇒ 叠字**；
        ///   而 PnP 那边同样是「效果文字 + 类型行」，两个值分别是 0.799 / 0.818，**不叠**。
        ///   让到 0.834 后我们的可见下沿约 0.795，留出 ~0.008 的间隙。
        /// ⚠️ **别只按公式推**：TMP `textBounds`（含基线以下那一段）和肉眼看到的字形下沿
        ///   差约 **0.039 卡高**；纯按公式算会得出「本来就不该叠」的错误结论。
        /// ⚠️ 这条一旦再改，`UnitDescBottom01` / `TacticDescBottom01` 也跟着失去意义 —— 三个是一组。
        /// <para>🔴 **2026-10-24（`E16`）**：上面 0.039 那个「行盒 vs 字墨」的差**只在「下沿对齐」
        /// 那个旧口径下是偏差**。现在中心对齐（`FaceDescBand01`），块中心就是 `textBounds` 的心、
        /// 字墨对称地落在它两侧 ⇒ 这条线只当**门**用，不再需要靠它去对肉眼位置。</para></summary>
        const float RaceDescBottom01 = 0.834f;

        /// <summary>原版**数值容器**的上沿（卡单位，y 向上；卡中心为原点）—— 效果文字往下不许越过的门。
        /// <para>一手判据（逐字段实读 `bundle_battleprefabs_vfxandmisc_assets_all/GameObject/`）：
        /// `Melee Attack Container` apos **(−0.836, −1.190)** · `m_SizeDelta` **0.4 × 0.4**；
        /// `Health Container` apos (0.740, −1.360) 同尺寸 ⇒ 四只容器里**上沿最高**的是近战那只：
        /// `−1.190 + 0.4/2 = −0.990` 卡单位 ⇒ 归一化 **y01 = 0.5 + 0.990/3.3313 = 0.7972**。</para>
        /// <para>⚠️ 这是**容器方框**（原版挂在 `2DCard` 下的那颗）的上沿，不是圆圈可见边缘 —— 拿它当门
        /// 宁可多让一点，也不让字压到数字上。</para></summary>
        const float StatContainerTopUnit = -0.990f;

        /// <summary>上面那条换成卡面归一化（y 从卡顶算）。⚠️ 只有**有数值格**的卡（单位/督军）才用它。</summary>
        public static float StatBandTop01 { get { return 0.5f - StatContainerTopUnit / CardUnitH; } }

        /// <summary>🔴 **效果文字那一块能用的纵向区间**（卡面归一化，y 从卡顶算）—— **判据只此一处**。
        /// <para>生产排版（<see cref="BuildTextLayers"/> ②）与自检断言（`CardBaseDemo.AssertFaceTextFit`）
        /// **都读这一个函数**；⛔ 别在别处再算一份（两处写同一条规则迟早不一致）。</para>
        ///
        /// <para>**原版怎么摆（判据）**：`DescTextUnit` / `DescTextTactic` 是**固定矩形**、
        /// `m_VerticalAlignment = 512 (Middle)` · `m_HorizontalAlignment = 2 (Center)` ⇒
        /// **文字块的中心 = 矩形中心**（`UnitDescAt.y` = 0.7323 / `TacticDescAt.y` = 0.7876）；
        /// `m_enableAutoSizing = 1` · `m_fontSizeMin/Max = 1/24` ⇒ 装不下就按**矩形**缩小。
        /// 我们 2026-09 起改成「下沿对齐 + 可用高度」是为了避开阵营行/兵种行 —— 但那样
        /// **短文字会被一直压到数值格上**（真机可见缺陷，E16 2026-10-24）。这里改回「中心 = 矩形中心」，
        /// 并把它两侧的门收进一个**对称预算**里。</para>
        ///
        /// <para>`budget01` = 以 `center` 为中轴、两侧都装得下的最大高度（y01）：取三者的最小值 ×2 ——
        /// ① 矩形半高（原版 autosize 的盒）；② `center` 到**阵营行墨下沿**；③ `center` 到**下门**。
        /// 下门 = 版面框底边（`UnitDescBottom01` / `TacticDescBottom01`；印兵种行时 `RaceDescBottom01`）；
        /// **有数值格的卡**再夹到 <see cref="StatBandTop01"/> 上面 0.01。</para>
        ///
        /// <para>实测（单位卡 + 有兵种行）：center **0.7323**（= 原版矩形中心，Δ0）· 预算 **0.1098 y01
        /// = 0.3658 卡单位**（binding = 数值容器那道门）。战术卡：center 0.7876 · 预算 0.1272 y01。</para></summary>
        public static void FaceDescBand01(bool unitCard, bool hasRace, out float center, out float budget01)
        {
            center = (unitCard ? UnitDescAt : TacticDescAt).y;
            var armyAt = unitCard ? UnitArmyAt : TacticArmyAt;
            float topGuard = armyAt.y + ArmyEmOfCard * 0.5f + 0.008f;      // 阵营行**墨**下沿（再让 0.008）
            float botGuard = hasRace ? RaceDescBottom01
                                     : (unitCard ? UnitDescBottom01 : TacticDescBottom01);
            if (unitCard) botGuard = Mathf.Min(botGuard, StatBandTop01 - 0.01f);   // 数值容器上沿
            float boxHalf = (unitCard ? DescBoxH : TacticDescBoxH) * 0.5f / CardUnitH;  // 原版矩形半高（y01）
            budget01 = 2f * Mathf.Min(boxHalf, Mathf.Min(center - topGuard, botGuard - center));
        }

        /// <summary>版面框的尺寸（卡单位）—— 原版 `DescTextUnit` **152.74 × 76.16 px**
        /// / `DescTextTactic` **160 × 78.98 px**（文字节点的 `m_LocalScale = 0.01` ⇒ ×0.01 卡单位）。
        /// 自检拿它当「渲染宽度对不对」的尺子，⛔ 别在断言里再抄一份数。</summary>
        public static Vector2 DescBoxSize(bool unitCard)
        {
            return unitCard ? new Vector2(DescBoxW, DescBoxH) : new Vector2(TacticDescBoxW, TacticDescBoxH);
        }

        /// <summary>效果文字块该以哪个点为中心（卡面归一化）—— 原版矩形中心，见 <see cref="FaceDescBand01"/>。</summary>
        public static Vector2 DescCenter01(bool unitCard)
        {
            return unitCard ? UnitDescAt : TacticDescAt;
        }

        /// <summary>🆕 2026-10-25（`X17`）自检口：**卡名那一层的版面框宽**（卡单位）——
        /// 就是 `BuildTextLayers` ① 传给 `Fill` 的那个 `nameW`（`FitToWidth` 拿它当尺子）。
        /// ⛔ 期望值不许从这里读回去当「对不对」；它只让自检能算出「卡名有没有越框」。</summary>
        public static float UiNameBoxWidth(bool unitCard)
        {
            return unitCard ? NameBoxW : TacticNameBoxW;
        }

        /// <summary>这张卡印不印卡面下部那行橙字（原版 `RaceText`）—— `SubtypeLine` 的非空判定。
        /// 自检要按**同一条判据**算效果文字的可用区间（<see cref="FaceDescBand01"/> 的 `hasRace`），
        /// ⛔ 别在断言里另判一份（那两处一旦分叉，自检就会去量一个生产上不存在的版面）。</summary>
        public static bool FaceShowsRace(CardData d)
        {
            // ⚠️ `CardData` 是 **struct**（`d == null` 编不过）—— `default(CardData)` 的 `type` 是空串，
            //    `SubtypeLine` 走「没填 type 的老路径」⇒ 返回 null ⇒ 这里如实回 false。
            return !string.IsNullOrEmpty(SubtypeLine(d));
        }

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

        // ==================================================================
        //  🔴 **2026-10-19（`A2138`）—— 卡框画的到底是【贴图上哪一块】**
        //
        //  **原版事实（一手，逐字段实读）**：`2DCard/CardFrame` 那个 uGUI `Image`
        //    `m_Type = 0`(Simple) · `m_PreserveAspect = 0` · **`m_UseSpriteMesh = 1`**
        //    ⇒ 走 `Image.GenerateSprite`（不是 `GenerateSimpleSprite` 的四角 quad）：
        //      `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Image.cs:1087-1122`
        //        spriteSize      = activeSprite.rect.size          // ← 682 × 998
        //        drawingSize     = RectTransform 的像素矩形        // ← 2.2452 × 3.2572
        //        spriteBoundSize = activeSprite.bounds.size        // ← = sprite rect / ppu
        //        vh.AddVert(vertices[i] / spriteBoundSize × drawingSize − drawOffset)
        //    ⇒ **整块 sprite rect（含透明留白）被拉进 Image 的 RectTransform**；`textureRect`
        //      （tight 网格 = 不透明内容）只是其中的一块，按 `textureRectOffset` 偏在里面。
        //
        //  **为什么不是 alpha bbox**：`Sprite.bounds` 对这批 sprite 就是 **m_Rect**（682×998），
        //    不是 tight 网格的包围盒（`textureRect` 逐帧不同：614.9×929.85 … 680.9×931.9）。
        //    ⚠️ **这条由参考实拍反证、不是只读源码**：原版三个数值框是 **prefab 固定容器**
        //    （Melee/Range/Health 的 anchoredPosition，与阵营/档位无关），而凹槽（红圆/紫圈/绿框）
        //    是**画在卡框贴图里**、像素位置逐帧恒定 ⇒ 只有「按 m_Rect 映射」才能让两者在**所有**卡框上
        //    同时对齐。逐像素量 `桌面/warpforge参考图/战斗—替换手卡参考图（后手）.png` 的 4 张泰伦卡：
        //    数字（画在固定容器上）与凹槽圆心 **dx −1.5 / −1.0 / +0.5 px**、dy ≈ 0
        //    —— 按 m_Rect 预测 **dx 0.0 / −1.7 / +0.9**，按 alpha bbox 预测 **dx +11.3 / +8.0 / +2.7**
        //    ⇒ **m_Rect 胜出**（y 上四个值统一偏 ~4 px，是 prefab 容器 y 参照系的常量差，两模型同偏，不参与判别）。
        //
        //  **影响**：卡框可见内容按 `textureRect/rect` 缩在固定矩形里（我们原来把 alpha bbox 拉满，
        //    ⇒ 平均大 7.7% 宽 / 6.1% 高 —— 逐帧表 `资料/普查产出_1019/K1_A2138_逐帧映射差.tsv`）。
        //    判据全文 → `资料/普查产出_1019/K1_报告.md` §四 · 本仓 `资料/任务线/卡牌线/任务/T04_卡面几何与图标.md`。
        //
        //  **常量（全盘 88 份 `Orc Frame*.json` + `frames_resolved.json` 52 条 × 96 槽位，0 变体）**：
        //    `m_Rect = (x 171, y 11, w 682, h 998)`，`m_Pivot = (0.5, 0.5)`，`ppu = 100`。
        //    ⚠️ **y 的原点是【贴图底边】**（Unity `Sprite.rect` 约定）⇒ UV 的 `vMin = 11/1024`、
        //    `vMax = 1009/1024`。**反证**：`textureRectOffset.y = 18.08 = textureRect.y(29.08) − 11`
        //    （同一坐标系）；若把 11 当顶边，alpha 内容会溢出 textureRect 36 px —— 不成立。
        //    ⛔ **别把 11 当「从顶部数」**（K1 报告里的 `1-(11+998-0.5)/H` 就是按顶边写的，会整体偏 4.5 px）。
        // ==================================================================
        const float FrameSpriteRectX = 171f, FrameSpriteRectY = 11f;
        const float FrameSpriteRectW = 682f, FrameSpriteRectH = 998f;

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
        // 🔴 **2026-10-19（`A1492` 收口 —— 改成原版 prefab 的绝对尺寸）**：
        //    判据 = `Card Rarity Sprite`（uGUI `Image`：`m_Type=0`(Simple) · `m_PreserveAspect=0` ·
        //    **`m_UseSpriteMesh=0`** ⇒ 整张 84×76 被拉进它自己的 RectTransform）的
        //    **`m_SizeDelta = 0.25796 × 0.23345`**（子节点 RectTransform 逐字段实读）。
        //    ⇒ 目标值就是它，**与画框映射无关**（宝石是独立 Image、固定 RT，不吃卡框的 uv）。
        //    改回它之后：可见菱形（84×76 里 alpha>128 的 75×73 px）= **0.2303 × 0.2242** 卡单位，
        //    对卡框凹槽（sprite-rect 映射下 67/682×2.2452 = 0.2206 宽、63/998×3.2572 = 0.2056 高）
        //    = **1.044× / 1.090×** —— 与 6 张 PnP 实测的「宝石外沿 ≈ 凹槽 1.0–1.12×」一致。
        //    ⚠️ **旧值 0.24620 × 0.22159 是 2026-10-11 按【错的画框映射】量出来的**
        //    （当时把 alpha bbox 拉满固定矩形）：它让可见菱形只有凹槽的 **0.893×** = 用户看到的「宝石小了」。
        //    ⛔ 别再按「相对凹槽」反算尺寸 —— 凹槽经各卡框 uv 换算后逐帧 **0.2149..0.2532** 宽，
        //    拿它当基准会逐帧漂；原版是固定 RT。
        //    判据全文 → `资料/普查产出_1019/K1_报告.md` §五·1 · `W_卡牌线_卡面侧2.md` §二·3。
        const float RarityGemW = 0.25796f, RarityGemH = 0.23345f;

        /// <summary>宝石凹槽中心在【卡框贴图】上的位置（贴图像素，y 从顶部数）。
        /// <para>🔴 **2026-10-18 改**：宝石**不再用固定卡单位常量**（那组是按
        /// `frame_saimhann_tier3` 一张量出来的，卡框换个 uv 就落错）——改成**跟着当前卡框的凹槽走**，
        /// 与 `FrameMeleePx/RangedPx/HealthPx` 那三个**同一套做法**（贴图像素 → `FramePxAt01` → 卡面归一化）。</para>
        /// <para>**一个常量对 150 个卡框都成立，因为凹槽本身就画在同一处**：15 个阵营的卡框都画在同一张
        /// 1024² 画布、凹槽像素位置完全一致（逐张实测）⇒ (511,950) 对所有卡框都对。
        /// 判据：`frame_saimhann_tier3.png` 黑菱形连通域 x478..544 / y919..981 ⇒ (511,950)。
        /// 2026-10-18 又拿 **12 张 PnP** 复核：把 (511,950) 经「三凹槽配准」的相似变换投到 PnP，
        /// 与 PnP 上量到的宝石中心残差 —— 10 张 **≤3 px**，另 2 张的 PnP 自身偏低 ≈14 px
        /// （那两张的宝石离卡底更近，属印刷品自身差异，不是我们这侧的错）。</para>
        /// <para>🔴 **2026-10-19（`A1492` + `A2138` 同批收口）**：尺寸改成 **prefab 绝对值
        /// `0.25796 × 0.23345`**（判据见上面 `RarityGemW/H` 那段），画框映射也同时改成 **sprite rect**
        /// ⇒ 宝石落点 `FaceGemAt01` 自动回到凹槽上（≈ −1.4547），与 prefab 的 (0, −1.4635) 差 0.0088。
        /// ⚠️ 本节原文写「尺寸判据等主对话裁『alpha 块还是 sprite rect』、本笔不动数值」——**已执行完毕**，
        ///    留此痕迹。旧记的「PnP 宝石比凹槽小 ~20%」**不成立** —— 那个数只对得上宝石的**彩色内核**
        ///    （common 的彩色区 53 px vs 凹槽 67 px）；把**金属边**算进去后 6 张 PnP 实测
        ///    「宝石外沿 / 卡框可见宽」= **0.098–0.132**，而 prefab 绝对值对卡框 2.2452 宽 = **0.1149** ✓。
        ///    判据全文 → `W_卡牌线_卡面侧2.md` §二·3 · `资料/普查产出_1019/K1_报告.md` §五·1。</para>
        /// <para>🔴 **`A1492` 尾巴「0.042」复核（2026-10-18）**：prefab 里 `Rarity` 与
        /// `Card Rarity Sprite` 两个 `RectTransform` 的 **`m_AnchoredPosition.x` 都是 0**、`pivot (0.5,0.5)`
        /// （Rarity apos y = **−1.458**、子件 y = **−0.0055** ⇒ 宝石中心 = **−1.4635** 卡单位，正是旧常量）；
        /// `CardFrame` 的 Image `PA=0`；frame sprite `m_Rect = (171,11,682,998)` / `pivot (0.5,0.5)`
        /// （13 军 × 4 档 = 52 条实测）。**按原版映射（sprite rect 拉满矩形）**重算：
        /// 凹槽中心 = **(−0.0016, −1.4547)**（`Front` 局部空间；🔴 **2026-10-25（`M2`）更新**：原来这里写
        /// 「`FrameUnitY = −0.03` 已含『整卡面低 0.08』」—— 那个「低 0.08」**已经在当天修掉**
        /// （卡面各层改挂 `2DCard/Front`，见 `FrontAposY`）⇒ 本行数值现在就是**原版绝对位置**，不再是「偏低」的那一版；
        /// x 用连续坐标 `(511.5−171)/682`，⛔ 不是 `(511−171)/682`），
        /// 与 prefab 宝石中心 **(0, −1.4635)** 相差 **(+0.0016, −0.0088)** ⇒ **原版宝石就是基本对齐凹槽的**，
        /// **推不出 0.042**（疑因原版实拍那张卡带 `AutoCardRotation`（±10°）透视、或量的是屏幕坐标）。
        /// 🔴 **真正能复算的差异全在【画框映射】**：我们用的是 `FrameUv` 的**不透明块**（x 201..811 =
        /// 611×926 px）拉满 `FrameUnit`，而原版把 **sprite rect（171..853 = 682×998 px，含透明留白）**
        /// 拉满 ⇒ 同一个凹槽像素在我们的卡空间落在 **(+0.0147, −1.5126)**（= 现在的 `RarityGemX/Y`），
        /// 比原版映射**偏 (+0.018, −0.058)**。**旧常量 (0, −1.4635) 与 prefab 一致**；`A1492` 把宝石挪到
        /// 凹槽上，补偿的其实是这条画框映射差 —— **改画框映射时要连宝石/费用/三围一起回看**，
        /// ⛔ 别只把宝石改回去（那会让它离开我们的凹槽）。</para></summary>
        const float FrameGemPx = 511.0f, FrameGemPy = 950.0f;

        /// <summary>宝石中心的**固定值**（卡单位）—— ⚠️ 只在**没有卡框（占位卡面）**时兜底：
        /// 有卡框时走 <see cref="FaceGemAt01"/>（跟着贴图上的凹槽，逐帧不一样）。
        /// <para>🔴 **2026-10-19（与 `A2138` 同批）：改回 prefab 的 (0, −1.4635)** —— 它就是
        /// `Rarity` apos **−1.458** + 子件 **−0.0055**（两个 `m_AnchoredPosition` 之和）；
        /// 在**原版映射（sprite rect 拉满矩形）**下与凹槽中心只差 **0.0088**（判据见上面 `FrameGemPx`
        /// 的「0.042 复核」那一节）。旧值 **(+0.0147, −1.5126) 是补偿当时错的 alpha-bbox 画框映射**
        /// 反算出来的；映射已按 `A2138` 修正 ⇒ 那组补偿值退场。
        /// ⛔ 别再按 alpha 映射把它改回去（那会让没有卡框的占位卡偏离原版常量）。</para></summary>
        const float RarityGemY = -1.4635f;
        const float RarityGemX = 0f;

        // ---- 原版立绘区（`CardImage`，A1 表）----
        // 2.7484 × 2.7484 @ (0, −0.044) 卡单位 —— **正方形、比卡本体大**，故意外溢，
        // 由卡框的 alpha 遮罩切掉（`子代理读报_2dcard_0827.md` D2）。
        // ⚠️ prefab 里那张 Image 是 `m_PreserveAspect = 0`（照字面 = 把立绘横向拉 1.53×成方形）。
        //    立绘 sprite 的 textureRect 宽高比是 0.6548，拉成 1.0 会明显变形；
        //    而**按比例 fit 进这个方形**得到 1.80 × 2.7484，正好盖住卡框拱窗（实测 1.69 × 2.76）——
        //    所以这里按 **preserveAspect 装进方形** 来做（见 `ArtMesh`）。
        //    ⚠️ 这一条**与原版字段不一致**，跑得了实况时要复核（原版关服，战斗界面进不去）。
        const float ArtUnitSquare = 2.7484f, ArtUnitY = -0.044f;

        // ==================================================================
        //  🔴 2026-10-24（`E24`）立绘中心的 y **按原版预制体分两族**（铁律 5·c：一个值 ≠ 全部情况）
        // ==================================================================
        //  原版解包**全库普查**（`d:/2/新解包资源/assets_full/` 91 个 bundle，逐个数 `CardImage` 的
        //  `RectTransform.m_AnchoredPosition.y`；脚本口径 = GO 名 `CardImage` → `m_Component` → RT；
        //  `m_LocalPosition` 在 RT 上是 (0,0,0)（Unity 由 anchoredPosition 反推）⇒ **判据取 anchoredPosition**）：
        //
        //  | 族 | 节点路径（根 → 叶） | apos.y | 实例数 | 我们对应谁 |
        //  |---|---|---|---|---|
        //  | **战斗卡 prefab** | `CardPrefab / 2DCard / Front / CardImage`（`bundle_battleprefabs_vfxandmisc_assets_all`） | **−0.065** | **1** | 手牌 / 场上 / 出牌飞行那一张（`CardFace.Hand` / `CardFace.Board`） |
        //  | **菜单 / HUD 卡 UI** | `CardUI( Reference) / 2DCard / Front / CardImage`（`Card Drawer` / `Generic Multi Card Display` / `Card Display Window` / `Card Picker` / `CemeteryGroup` / 卡池 / 收藏 / 抽卡包 / 主菜单…） | **−0.044** | **151** | 放大窗 · 多卡窗 · 墓地 · 卡池 · 收藏 · 详情窗（`CardFace.Full`） |
        //
        //  （另有 39 个同名 `CardImage` 是**头像气泡**里的 `Mask/CardImage`（176×176 @ y −5），
        //    与卡面无关 ⇒ 不在本表。）
        //
        //  ⚠️ **差额 = 0.021 卡单位**，卡高 3.3313 ⇒ **0.63% 卡高**；手牌高 263 px 时 ≈ **1.7 px**，
        //     详情窗大卡 1076 px 时 ≈ **6.8 px**。这正是上一轮账上记的「≈7 px」。
        //  🔴 **纠正上一轮的口径**：那条账写「原版 −0.065 vs 我们 −0.044 ⇒ 照原版改成 −0.065」——
        //     **只对手牌/场上那张卡成立**；-0.044 **不是我们抄错，是菜单族的原版值**（151 个实例）。
        //     ⇒ 全局改成 −0.065 会把**放大窗 / 卡池 / 收藏 / 详情窗**全部改错 7 px。所以分族。
        //  判据补充：「战斗卡 prefab 的 −0.065」在**场景里没有对应实例**（13 个 arena 场景的那 8 个
        //     `2DCard/Front/CardImage` 全是 `CardUI` 族 = −0.044 —— 那是 HUD 里那几扇窗的卡，
        //     不是手牌/场上卡；真正的战场卡是运行时从 `CardPrefab` 实例化的）⇒ 只能从 prefab 读。
        const float ArtUnitYFight = -0.065f;

        /// <summary>这张卡的立绘层走**哪一族**原版预制体的 y（`CardFace.Hand` / `CardFace.Board` =
        /// 战斗卡 `CardPrefab`；其余 = 菜单/HUD 卡 UI）。判据与两族的值 → <see cref="ArtUnitYFight"/> 那一节。</summary>
        bool ArtUsesFightFamily { get { return _faceMode == CardFace.Hand || _faceMode == CardFace.Board; } }

        /// <summary>立绘中心 y 的**族差**（卡单位）—— 直接加在 `ArtExtent` 的 `typeDy` 上
        /// （`acy = ArtUnitY·s + typeDy·s`，`s = Width / CardUnitW = 1` ⇒ 这里就是 0 或 −0.021）。</summary>
        float ArtFamilyDy { get { return ArtUsesFightFamily ? (ArtUnitYFight - ArtUnitY) : 0f; } }

        /// <summary>这张卡的**立绘中心偏移** = 卡型偏移（`tacticsOffsetPosition`）+ 族差。
        /// 🔴 生产路径**只走这一个口**（`ArtLayerMeshFor` / `ArtFrontMeshFor` / `FrameMesh` 的 uv2 三处）——
        /// 各自拼一遍 = 迟早不一致（`ArtBoard` / `ArtUvAt` 那条老坑的同族问题）。</summary>
        float ArtDyFor(CardData d) { return ArtTypeDyFor(d) + ArtFamilyDy; }

        /// <summary>立绘板在「装进方形」的基础上再放大一点（4%）。
        /// 为什么要有余量：原版的拱窗（实测 1.69 × 2.76 卡单位）和 fit 出来的立绘
        /// （0.6548 的画 → 1.80 × 2.7484）**纵向只差 0.012**，留一点余量免得拱顶露出背景。
        /// ⚠️ 不能靠「把板拉到卡框那么大」来解决 —— 那会让 UV 出界、贴图 Clamp 把边上的像素
        ///    拉成一圈白边（踩过，见 `ArtMeshInFrame`）。</summary>
        const float ArtCoverMargin = 1.04f;

        /// <summary>🔴 **2026-10-24（`E9`）**：非单位卡（原版 `cardType = 20` —— 战术**和**防御）的立绘倍率。
        /// <para>**一手判据**：`bundle_duplicateassetisolationso_assets_all/MonoBehaviour/CardScalesByCardType.json`
        /// 的 `cardScalesByType` = `0 → 1.0`（Minion）· `10 → 1.0`（Hero）· **`20 → 0.6100000143051147`**（Tactic）·
        /// `5 → 0.60`（⚠️ **全池没有任何一张卡读到 5** —— 防御卡的 SO `cardType` 也是 **20** ⇒ **防御卡同样 0.61**）。
        /// 落地那一行：`CardImageController__SetImage.c:103-118` 把 `cardScales[卡型]` 乘进 `CardImage.localScale`
        /// （绕 pivot 等比缩放 ⇒ 等价于把整块立绘板按比例缩小）。</para>
        /// <para>⚠️ 别把 `0.60` 那条当防御卡的（键是 `5`，判据订正 → `资料/普查产出_1024b/E5_卡面补间与锻造链.md` §七）。</para></summary>
        public const float TacticArtScale = 0.61f;

        /// <summary>🔴 **2026-10-24（`E9`）**：非单位卡立绘中心的**纵向偏移**（卡单位）。
        /// 一手：`CardImageController` 的序列化字段 `tacticsOffsetPosition = {x:0, y:0.5, z:0}`
        /// （`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_5393623139443973056.json`）；
        /// `CardImageController__SetImage.c:126-128`（**只给** `param_3 == 0x14` 取它）＋ `:145-155`
        /// 把它**加在** `CardImage.localPosition` 上。
        /// <para>🔴 **它与 `localScale` 是两件独立的事**（原版两条独立的 Transform 写入）——
        /// ⛔ **不是** `(acy + 0.5) * 0.61`：那个式子把偏移也乘了 0.61，中心 y 会少 0.195 卡单位（≈66 px）。</para></summary>
        public const float TacticArtDy = 0.5f;

        // ==================================================================
        //  🔴 原版立绘的 `_Clamp_Range` 矩形（2026-10-24 · A7 / `X1` 订正语义）
        // ==================================================================
        //
        // **它是什么**：原版 `CardImage`（那张 Image，材质 = `Card Image Overflow`）的 shader
        //   `Everguild/Card ImageUI`（解包 `bundle_shaders_assets_all/Shader/Shader_-2318977921299023320.json`，
        //   `m_ParsedForm.m_Name`）片元里**只**对 α 做的一件事：
        //
        //     alpha = saturate(alpha + ((z < uv.x ≤ x && w < uv.y ≤ y) ? 1 : 0))
        //     // 四个 PS 变体（无 / ALPHACLIP / CLIP_RECT / 两者；600/668/768/844 B）这段逐字节相同
        //     // 指令流：ge → and → movc → mad_sat（`mad_sat dst,a,b,c` = saturate(a*b + c)）
        //
        //   即 **UV 空间的一个矩形**：**矩形内把 α 顶到 1；矩形外 α 原样保留（用贴图自己的 alpha）**。
        //   🔴 **⚠️ 2026-10-24 更正（铁律 5 · 判据 = `资料/普查产出_1024d/X1_三处判据补查.md` §一）**：
        //   原来这里写的是 `α *= (…) ? 1 : 0` /「矩形外整块 α=0（不是按轮廓、也不是渐变）」—— **两句都错**：
        //   反汇编是 `mad_sat`（**加法**），既不是乘法、也不是「丢弃」。
        //   ⇒ **原版从来不把 α 置 0 ⇒ 它从来不切掉任何插图**。这段错口径正是 `H1`
        //   「非单位卡也套硬矩形」那一版的判据来源。
        //   ⚠️ 命名：本文件下文仍沿旧称「硬矩形 / clampKind」，指的都是**这块 UV 矩形**；
        //   它**不裁画面** —— 凡是写「裁 / 切」的地方都是**我们自己把网格缩进矩形**的近似（见下面「我们怎么落」）。
        //
        // **绑定（一手）**：该属性在 `$Globals` 的 **byte 112 = cb0[7]**（`m_CommonParameters.VectorParams`），
        //   `m_Attributes = []`（**没有 Toggle**）⇒ **没有任何关键字门控**，四个 PS 变体无条件执行；
        //   常量表里唯一的 Toggle 是 `_UseUIAlphaClip`（与它无关）。`m_DefValue = (0.81, 1, 0.18, 0)`。
        //
        // **真值（一手，不是猜）**：`CardImageController__.cctor.c` 把两个 Vector4 写进静态字段
        //   （静态区 +0x00 = SMALL、+0x10 = BIG），字节来自 `_DAT_1834b33d0…0x1834b33ec` ——
        //   `工具/read_literal.py` 现读（4 字节档，本文件上下文就是 float）：
        //     · SMALL = (x 0.81, y 1, z 0.18, w 0)      ← 0x1834b33d0 / d4 / d8 / dc
        //     · BIG   = (x 0.75, y 0.89, z 0.25, w 0.1) ← 0x1834b33e0 / e4 / e8 / ec
        //   作用层级 = `CardImageController.DoChange` 的 `Material.SetVector(materialOffsetAndScaleID, …)`，
        //   那个 ID 来自 ctor 的 `Shader.PropertyToID("_Clamp_Range")`（字符串字面量 VA 0x1842c5010，
        //   `script.json` 的 `ScriptString` 逐字节命中；`Card Image Overflow.mat` 里那份序列化值也是
        //   `(0.82, 1, 0.18, 0)`）。
        //
        // ⚠️ **UV 是【贴图 UV】，不是 sprite 局部 UV**（`_MainTex_ST` 恒 = 1,0 ⇒ 就是 `Sprite.uv`）。
        //   原版卡图贴图统一 **1024²**，两族 sprite rect（全池 1207 个 Sprite 实测：657 + 548 + 2 杂项）：
        //     · 单位 / 督军 = **670.5×1024 @ x=176.5**（671 张 unit/hero 卡）⇒ 贴图 UV 的 (0.18,0.81]
        //       在这族上只落在**图宽两端各 1.2% / 2.6% 之外**（不是「中间 64%」—— 那是把贴图 UV 当局部 UV 的误读）；
        //     · 战术 / 防御 = **整张 1024²**（548 张）⇒ 那族上才是 (0.18,0.81] / BIG (0.25,0.75]。
        //   ⚠️ 这个区间是**「矩形内 α 顶到 1」的作用范围**，⛔ 不是「切掉」。
        //   ⇒ 换算到**我们这张 PNG 的局部 UV** 走 `ArtClampUv01`（下面），一张图一份。
        //
        // ⚠️ **两个小图值**：材质序列化默认 `x=0.82`，cctor 常量 `x=0.81`。产品路径
        //   （`CardDisplayWindow.ShowBattleCard` → `ChangeSize(Small, instant)` → `ChangeState(Small)`）
        //   在 `currentState` 已经是 Small(0) 时**提前 return**（`ChangeState` 第一句）⇒ 画面上用的是
        //   **材质默认 0.82**；0.81 只在 Big→Small 之后出现（本 build 只有调试组件 `CardImageControllerTester`
        //   会走到）。两者差 1% 图宽（≈10 纹素），落点都在卡框不透明金属/或框外 ⇒ 这里取**产品实际值 0.82**
        //   （BIG 只可能由 ChangeState(Big) 写入 ⇒ 用 cctor 的 0.75/0.89/0.25/0.1）。
        //
        // ⚠️ **状态**：`ChangeState` 只在**尺寸切换**时改它；`DoChange` 把 `Vector4` 写进材质。
        //   BIG 那一档只在 `currentState == 1` 时取（本 build 的产品路径**不可达** —— 见 `BigCardOverdraw`）。
        //
        // ⚠️ **我们怎么落**（🔴 **2026-10-24 第三次订正**）：本段此前两版都不对 ——
        //   第一版写「硬矩形**只套 `_artFront`**，底层 `_art` 不套」；`H1` 改成「非单位卡的 `_art` 也套 1/2」。
        //   🔴 **`H1` 那一版现在由反汇编直接否掉**（判据 = `X1` §一）：`_Clamp_Range` 的算式是
        //   `alpha = saturate(alpha + inside01)`，**它根本不裁画面**（矩形外原样保留）⇒
        //   「给 `_art` 套一个硬矩形」这件事在原版里**没有对应物**。
        //   当时用来翻面的四条「实况」证据**方向仍然对**，但只算旁证（决定性判据是指令流）：
        //   ① 原版游戏三张截图（收藏 / 详情窗大卡）里战术卡插图**铺满拱窗、无黑边**；
        //   ② 把我们的 1024² 立绘按 NCC 匹配进「先知之道」大卡（0.994）⇒ 原版画的是**整张插画**；
        //   ③ 套上之后板 1.7436 只剩 ±0.5579，而画窗是 ±0.839…±0.872 ⇒ 每侧 0.28 卡单位黑带；
        //   ④ 离线跑遍 75 张 `*_strat*` 卡框：`clampKind = 0` 时覆盖画窗所需半宽最大值
        //      **= 0.8718 = 板半宽本身** ⇒ 不套时板刚好铺满每一张画窗。
        //   ⇒ 现行口径：
        //   · **底层 `_art`**（`ArtOpaque`，忽略 alpha 的整幅插图）：**恒 0（不套）**，
        //     单位卡与战术/防御卡一视同仁（判据收在 `ArtClampKindFor` 一处，`Build` 与状态重烘共用）。
        //   · **`_artFront`**（真 alpha 那层）：照旧套 —— 小图档 `clampKind 1`、大图档 `2`。
        //     🔴 但那**是我们自己的近似、判据不足**：原版矩形外**照用贴图 alpha**，我们是把**网格缩进矩形**
        //     （`ArtMeshInFrame`）⇒ ⛔ 不能再写成「原版那一层 `_Clamp_Range` 的等价物」。
        //   ✅ `CardImageController__SetImage.c` 那条反证（战术分支只关 `frameMask`、sprite 无条件赋值）
        //   仍然成立，且与 `X1` 一致：C# 层没有按卡型分支、shader 那段对所有卡型都生效 ——
        //   只是它**不裁**，所以「生效」在画面上不会表现为「削边」。
        // ⚠️ opt-in 开 `_artFront`（`UseFrontLayer` 出厂 false）时它会**真的切掉一条**（单位卡贴图的 alpha
        //   伸到局部 u 0.006…1.049，超出小窗口 u 0.0117…0.9883）—— **这是我们的做法**；
        //   原版在同一个矩形上做的是「矩形内强制 α=1、矩形外照用贴图 alpha」。
        //   （`CardFaceProbe` ④b 断「切了」且「只切在矩形外」= 断**我们自己这层的几何**；④a 两种卡型都断
        //   「差集 = 0」—— `M2` 起战术/防御卡那一半也翻面了。）
        // ⚠️ **已知的可见偏差**（`X1` §一·⑤，如实记、不阻塞）：原版矩形**外**用贴图 alpha ⇒ 对内接图的单位卡，
        //   矩形外那圈**原版比我们更透明**（我们 `_art` 是 `ArtOpaque` 整幅不透明）。要不要为它加一层，
        //   留待真机并排复核。
        static readonly Vector4 ClampRangeSmall = new Vector4(0.82f, 1f, 0.18f, 0f);
        static readonly Vector4 ClampRangeBig = new Vector4(0.75f, 0.89f, 0.25f, 0.1f);

        /// <summary>原版卡图**贴图**边长（1207 个卡图 Sprite 全是 1024² 画布）。</summary>
        const float ArtSourceTexPx = 1024f;

        /// <summary>探针/自检用：**关掉**原版 `_Clamp_Range` 硬矩形（回到 2026-10-24 之前）。
        /// ⚠️ 只给「小图态 A/B 逐像素不变」那条断言用，⛔ 别在业务代码里翻它。</summary>
        public static bool DebugDisableArtClamp = false;

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

        // 🔴 **2026-10-20（`W58`）：场上那面盾是【另一个尺寸/另一个落点】，⛔ 别拿 2D 这套顶。**
        //    判据（原版 prefab，`bundle_battleprefabs_vfxandmisc_assets_all`）：
        //    · `Armour Text/Armour Background` 局部 `(−0.003, 0.024, 0.06145)`、
        //      localScale **0.26422**，父 `Armour Text` 的**世界 scale = 0.48287**
        //      · `m_Size = 2.48 × 3.06` ⇒ 实绘 **0.3164 × 0.3904** 卡单位（我们原来是 0.3067 × 0.3784，−3.1%）
        //    · 中心（3DBody 空间）= Armour Container (0.663, 0.771) + local × 0.48287
        //      = **(0.675, 0.7956)** ⇒ 比数字中心（`BoardArmourAt`，3DBody y 0.771）**高 0.024**
        //    ⚠️ 2D 卡面那套（`ArmourIconAt` / `ArmourIconW/H`）是卡框上另一个 Image，**不改**。
        const float BoardArmourIconW = 0.3164f, BoardArmourIconH = 0.3904f;
        /// <summary>场上护甲盾中心 = **数字中心 + (−0.003, +0.024)**（原版 `Armour Background` 局部
        /// `(−0.003, 0.024)`；报告 `W58` 的裁定值 ⇒ 3DBody **(0.675, 0.7956)**）。
        /// ⚠️ 见报告「没查清」：父级 scale 0.48287 是否也作用在这个局部偏移上，报告**没有乘**
        /// （它乘了尺寸、没乘位置）；本批按任务口径「比数字高 **0.024**」落地。
        /// 2D 卡面那面盾（`ArmourIconAt`）**不动**。</summary>
        static readonly Vector2 BoardArmourShieldAt =
            new Vector2(BoardArmourAt.x - 0.003f / CardUnitW, BoardArmourAt.y - 0.024f / CardUnitH);

        /// <summary>🔴 **W27（2026-10-18）遗留 —— 2026-10-19 主对话裁定后【无消费者】，仅历史/复核。**
        /// <para>费用底板贴图 `Card_Frame_Cost_Icon.png` 的**不透明 bbox**（实测 alpha&gt;8：
        /// x 3..238 / y 3..238，贴图 248×244）。⇒ 四边留白 = 左 3 / 右 9 / 上 3 / 下 5 贴图像素。</para>
        /// <para>W27 曾用它把「可见边」折回**网格中心**（`CostBgW/H` 是**整张 sprite 的网格尺寸**，
        /// 不是可见宽 —— 直接拿可见边去对网格边会内缩 9 像素 ≈ 0.018 卡单位）。现在费用走
        /// <see cref="FaceCostAt01"/> 的**固定容器**，这一套不再参与。</para></summary>
        const float CostIconW = 248f, CostIconH = 244f;
        const float CostIconPadL = 3f, CostIconPadT = 3f;
        const float CostIconPadR = 9f;                 // 248 − 1 − 238
        // 可见右边在网格里的归一位置（0..1）= (238+1)/248 = 0.96371
        const float CostVisRightFrac = (CostIconW - CostIconPadR) / CostIconW;
        // 可见上边离网格上边 = 3/244 ⇒ 中心到可见上边 = (0.5 − 3/244) × CostBgH
        const float CostVisTopFromCenter = 0.5f - CostIconPadT / CostIconH;

        /// <summary>🔴 **W27（2026-10-18）遗留 —— 2026-10-19 主对话裁定后【无消费者】，仅历史/复核。**
        /// <para>费用八边形**可见上缘**与**阵营徽记下缘**之间的常量缝（**贴图像素**）。
        /// 判据 = `桌面/warpforge参考图/战斗—替换手卡参考图（后手）.png`（泰伦 4 张逐张量：
        /// 徽记亮部下缘 y=269、八边形外沿上缘 ≈277，卡高 391px ⇒ **≈8 屏幕 px**），当时折算成 19 贴图像素。</para>
        /// <para>⛔ **别再拿它改版式**：原版费用是 prefab 固定容器 `(0.788, 0.666)`，不贴徽记。</para></summary>
        const float CostBadgeGapPx = 19f;

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
        //  🔴 **2026-10-19 就地更正（`A1500②`）—— 下面两条 `y` 原来把父级 `Front` 的 `+0.08` 【只加给了底板、没加给文字】。**
        //  现核父链（`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`，
        //  `GameObject/*.json` → `m_Component` → `RectTransform_*` / `Transform_*`）：
        //     `2DCard`(apos 0,0) → `Front`(**apos y = +0.08**) → `Textbackgrounds`(**纯 `Transform`，lpos 0,0,0**)
        //                                                       └ `Card Info`   (**纯 `Transform`，lpos 0,0,0**)
        //  ⇒ 底板相对卡中心 = `+0.08 − 0.65`、文字块 = `+0.08 − 0.7745` —— **两条路的父级结构逐字相同**。
        //  ⚠️ 所以**不能只给其中一条加**：原来只给底板加了、文字没加 ⇒ 底板比文字高 `0.08 卡单位`
        //     （详情窗尺度约 **20 px**）—— 就是用户点名的那条。
        //     ⛔ **反方向（把六条文字 y01 减 `0.08/3.3313` 抬上去）不取**：那会把
        //     **文字与卡框 / 立绘 / 宝石的关系一起改错 0.08** —— 本文件里 `CardImage`(−0.044) /
        //     `CardFrame`(−0.03) / `Cost Container`(+0.666) 等**全部取的是不含 `+0.08` 的原始值**，
        //     底板是唯一的异类 ⇒ 改底板。
        //  ✅ **2026-10-25（`M2`）已收口**：整张卡面（立绘/框/宝石/数值/文字）相对 `2DCard` 低 `0.08` 那件事
        //     已经修掉 —— 卡面各层改挂 **`2DCard/Front`**（`localPosition.y = +0.08`，见 `FrontAposY`），
        //     即原版那条父链本身；**层与层之间的相对关系一个字节没动**（各层仍用 `Front` 局部坐标）。
        //     ⇒ 下面这两条 `TextBgAt` 的换算（`0.5 + |apos| / CardUnitH`）**仍然成立**（apos 本来就是 Front 局部值）。
        //  现核读数：`TextBackground Big UI` apos y = **−0.65** · `TextBackground Small UI` apos y = **−1.02**。
        static readonly Vector2 TextBgAt      = new Vector2(0.49952f, 0.69512f);  // 0.5+(−0.001)/2.0927 · 0.5+0.65/3.3313
        static readonly Vector2 TextBgSmallAt = new Vector2(0.50000f, 0.80621f);  // 0.5+0/2.0927 · **0.5+1.02/3.3313**（原版 apos y = −1.02 的**原值**）
        // 🔴 **2026-10-11 就地订正（铁律 5 · 代理 `J` 现核，主对话独立复核）**：上面这条常量在一个小时前被改成
        //   `0.78217f`（= `0.5+(1.02−0.08)/3.3313`）—— **那是错的**，已改回 `0.80621f`。
        //   **判据**：两块底板走的是**同一条父链**（`Textbackgrounds`(0,0,0) → `Front`(**+0.08**) → `2DCard`）⇒ 它们相对卡面**只能一起平移**，
        //   所以两块的 cy **必须差 `0.37`**（`−0.65` vs `−1.02`）。落地值差 `0.29` = 只给 Small 多减了 `0.08`。
        //   🔑 **为什么 Big 该「减」而 Small 不该**：Big 原来那条 `0.67111` 是**我们自己**把它写成了 World-1 复合值（`0.08−0.65`），
        //   而 **Small 原来那条 `0.80621` 本来就是原版 apos 的原值**（`−1.02`）⇒ 它不需要任何平移。
        //   （本文件其它层一律用不含 `+0.08` 的原值：`CardImage −0.044` / `CardFrame −0.03` / `UnitNameAt.y 0.5821 ↔ −0.2735` 逐位符合。）
        const float TextBgW = 1.7766f, TextBgH = 1.55f;
        const float TextBgSmallW = 1.8635f, TextBgSmallH = 0.9621f;
        const float TextBgAlpha = 0.647f;          // Image `m_Color.a`，原值 0.64705884
        /// <summary>底板的 z。**比卡框(0)大 ⇒ 画在卡框【之后】**（`CardView.cs:379-382` 记的原版子序
        /// `CardImage → Textbackgrounds → CardFrame → Card Info` 要的就是这个）。
        ///
        /// 🔴 **2026-10-12 就地更正（`A1658`，用户报「这块黑色半透明底板压在卡框之上」）—— 值由 `−0.012` 改成 `+0.015`。**
        ///   **改前为什么是缺陷**：本仓 z 约定「**z 越小 = 离相机越近 = 画在上面**」（`_art`+0.03 → `frame`0
        ///   → `_info`−0.02 → 文字−0.03）⇒ `−0.012` 落在卡框的**靠前侧**，那块 64.7% 的黑底**盖住卡框自己那一圈**。
        ///   **改后的层表**（`FaceRoot` 局部 z，由远到近）：`shadow +0.06` → `art +0.03` → **`textBg +0.015`**
        ///   → `frame 0` → `info −0.02` → 文字 `−0.03` ⇒ 立绘 →（底板）→ 卡框 → 数值 → 文字，与原版子序逐层吻合。
        ///
        /// ⚠️ **原来那段「不能摆 +0.015，否则 `_artFront` 一开就被整个盖住」的担心【不成立】**（本次现核）：
        ///   `_artFront` 的 z = **−0.008**，比新底板 **+0.015 更靠相机**（更小）⇒ opt-in 那层抠图
        ///   **仍然画在底板之上**，被盖住的是底板、不是它。当年那条大概是按「+0.015 比 −0.008 大 ⇒ 更靠前」
        ///   读反了方向（本仓是**越小越靠前**）。
        ///   ⇒ 现在两条并存且自洽：**出厂**（`_artFront` 不建）= 原版子序；**opt-in 开** = 抠图在最前。
        /// ⚠️ **2026-10-18 订正**：`_artFront` 现在**不再是「出厂都不建」** —— 大图档（卡片详情窗扇形，
        ///   见 `BigCardOverdraw`）默认就建它，所以「抠图在底板之上」这条**从今天起可观测**，
        ///   层级表按上面那句读（立绘底板 → 卡框 → 数值 → 文字，抠图层落在卡框之上、数值之下）。
        ///   小卡档仍然不建（`UseFrontLayer == false`）⇒ 那条差异对小卡依旧不可观测。
        /// ⚠️ 顺序判据只此一处（`TextBgLayer` / `TextBgZ` 两个 accessor 给自检读）—— 别在别处再写一份。</summary>
        const float TextBgZ = 0.015f;
        /// <summary>护甲盾的中心：容器 (0.899,−0.991) + 图自己的 (−0.005,−0.032)，换算成 x01/y01</summary>
        static readonly Vector2 ArmourIconAt = new Vector2(0.92721f, 0.80710f);

        // ---- 🆕 「临时卡（Ephemeral）」角标 —— 2026-09-13 第三十二轮 ----
        //
        // **为什么是图标而不是原版那套**：原版的卡面标记是
        // `BattleCardUI.ShowEphemeral()`（`BattleCardUI__ShowEphemeral.c`，77 行 —— 在卡的位置
        // 实例化一个状态图标 prefab）+ `Card2DController.ToggleGlitch()`（把卡面材质换成
        // `glitchMaterial`）。🔴 **2026-10-25（R16）就地订正（铁律 5）**：本行原来写「**glitch 素材本地没有**」
        // （`d:/2` 全盘 `*glitch*` 零命中）—— **不成立**，那只是「按名字搜不到」：
        // 材质 `Card Frame Glitch`（`bundle_duplicateassetisolation_assets_all/Material/Material_-2877403367212247079.json`）、
        // shader **`Everguild/FX/Sprite Scan Lines`**（`bundle_shaders_assets_all/Shader/Shader_-5025784757600742591.json`）、
        // 噪声图 `Noise Combined.png` **三件都在本地**（原版走 PPtr，名字里没有 "glitch"）。
        // ⇒ 结论改成「**要做，只差自建那个 shader**」（铁律 11）。✅ **2026-10-25（E7 批次 3）已补上**：
        // `SetGlitch` + 自建 `Resources/Shaders/CardFrameGlitch.shader`（算式逐条照抄原版 DXBC，见类尾判据块）。
        // 这张关键词图标**仍然保留** —— 原版本来就是「角标 + 换材质」**两件事**，不是二选一。
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
        //   —— **比卡本体大**，是**故意**要外溢的。可见形状由**卡框贴图自己的 alpha** 决定：立绘画在
        //   框**下面**、框按标准 alpha-over 盖上来 ⇒ 开口（α=0）处全透、半透明斜面处露 `1−α`
        //   （**一次乘法**，⛔ 不是 `(1−α)²`）。
        //   🔴 **2026-10-24 订正（`新账-66` alpha 口径 · 反编译现核）**：这里原写「由卡框上的 SoftMask
        //   （`MaskArea`=立绘的 RT、`FlipAlphaMask=1`）拿**卡框的 alpha** 把它切掉」—— **两处都不对**：
        //   ① 那颗 `SoftMaskScript` 的 `AlphaMask` 序列化值是 **null**（151/153 实例；默认白 ⇒ α_mask=1
        //      ⇒ 被它管的那颗 Graphic 直接变 0），它只会把**承载它的 Graphic**（CardFrame / textBackground）
        //      整层淡没，**不碰立绘**；② 立绘的可见范围来自卡框贴图 alpha（`_Clamp_Range` **不裁画面** ——
        //      它只把**矩形内**的 α 顶到 1、矩形外原样保留；🔴 2026-10-24 更正：原来把后者也算进「可见范围」，
        //      判据 → `资料/普查产出_1024d/X1_三处判据补查.md` §一）。
        //   判据全文 / 出处 → `ArtVisibleThroughFrame` 的 doc。
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

        /// <summary>自检用（`H1`）：**底层立绘那一层**（`_art`）真交给渲染器的网格。
        /// <para>为什么要这个口：立绘网格的顶点是**烘出来**的（`ArtMeshInFrame`），`ArtDrawSize` 只给 bounds
        /// 的宽高、给不出**左右极值** ⇒ 判不了「我们那层网格有没有被 `clampKind` 的矩形缩进去」。
        /// （⚠️ 2026-10-24 更正：原写「原版 `_Clamp_Range` 的硬矩形有没有真的夹住」—— 原版那个矩形
        /// **不裁画面**，见 `ClampRangeSmall` 的 doc。）
        /// <para>`null` = 这一层**根本没建**（卡框取不到等）—— 调用方**别读成通过**。</para></summary>
        public Mesh ArtLayerMesh
        {
            get
            {
                if (_art == null) return null;
                var mf = _art.GetComponent<MeshFilter>();
                return mf != null ? mf.sharedMesh : null;
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
        /// <summary>卡框那层用的 UV 矩形 —— 🔴 **2026-10-19（`A2138`）起 = 原版 sprite rect
        /// `m_Rect`（171,11,682,998，含透明留白）**，不再是 alpha bbox（判据见 `FrameSpriteRectX`）。
        /// ⚠️ 自检拿它把**贴图像素**换算到卡框层局部坐标时仍然成立（网格 bounds × 这个 uv 是同一套映射）。</summary>
        public Rect FrameUvRect
        {
            get
            {
                var t = FrameTexture;
                return t != null ? FrameUv(t) : new Rect(0f, 0f, 1f, 1f);
            }
        }
        /// <summary>自检用：前景层（角色抠图）用的贴图 —— **没有那一层就是 null**。
        /// ⚠️ 小卡档**恒为 null**（`UseFrontLayer` = false 且不是大图档 ⇒ 那一层根本不建）；
        /// 大图档（详情窗扇形）的抠图卡才有它 —— 见 <see cref="BigCardOverdraw"/>。</summary>
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
        /// <summary>自检用：前景层的 z（要比卡框(0)靠前、比数值层(−0.02)靠后）；
        /// **没有那一层时返回 `float.NaN`**（出厂态就是这样 —— 判「它建没建出来」用它）</summary>
        public float ArtFrontZ { get { return _artFront != null ? _artFront.transform.localPosition.z : float.NaN; } }
        /// <summary>卡框那层的 z（判据用，恒为 0）</summary>
        public float FrameZ { get { return _frame != null ? _frame.transform.localPosition.z : float.NaN; } }
        /// <summary>立绘底层那层的 z（判据用，+0.03）</summary>
        public float ArtZ { get { return _art != null ? _art.transform.localPosition.z : float.NaN; } }
        /// <summary>数值层（原版 `Card Info`）那层的 z（判据用，−0.02）。
        /// ⚠️ 名字带 `Layer` 是为了避开**同名的私有常量** `InfoZ`（那个是建层用的值，不公开）。</summary>
        public float InfoLayerZ { get { return _info != null ? _info.transform.localPosition.z : float.NaN; } }

        /// <summary>🔴 **2026-10-25（`M2`）**：`_info`（整卡大小那一张 quad）的局部位置 —— **按形态取 y 和 z**。
        /// <para>为什么 y 分形态：卡面各层现在挂在 `2DCard/Front`（`+0.08`，见 `FrontAposY`），
        /// 而这张 `_info` 烘进去的内容**两套坐标系都用** ——</para>
        /// <list type="bullet">
        ///   <item>**非场上**（`Full`/`Hand`）：费用/卡名/效果/数值全按 **2D 卡面**归一化坐标烘
        ///   （`FaceStatAt01` 一族）⇒ 与其它卡面层同一套 ⇒ **y = 0**。</item>
        ///   <item>**场上**（`Board`）：四个数按 **`3DBody` 空间**烘（`BoardMeleeAt` 一族，卡根空间，
        ///   见 `InfoTexture` 的 `statsOnly` 那几行）⇒ 要把 `Front` 的 `+0.08` **抵消掉**，
        ///   否则场上的数字整体上浮 0.08 卡单位（≈2.4% 卡高）。
        ///   ⚠️ 平时场上这张贴图是**全透明**（`skipStats`）看不出，只有「字形表不在 ⇒ 数字退回烘进 `_info`」
        ///   那条兜底路才会现形 —— 正因为如此才更要写对，⛔ 别当成「反正看不见」。</item>
        /// </list>
        /// <para>⚠️ z 沿用原口径（场上 `BoardInfoZ` / 其它 `InfoZ`）。两个入口
        /// （出生 `Build` · 换形态 `SetFace`）**都走这一个函数**（铁律 10 第 5 条）。</para></summary>
        void PlaceInfoLayer()
        {
            if (_info == null) return;
            bool board = _faceMode == CardFace.Board;
            _info.transform.localPosition = new Vector3(0f, board ? -FrontAposY : 0f,
                                                        board ? BoardInfoZ : InfoZ);
        }

        /// <summary>🔴 **2026-10-25（`M2`）**：护甲盾那层的局部位置 —— **按形态取 y 和 z**（同 `PlaceInfoLayer`）。
        /// 场上那块网格（`armourIconBoard`）把 `BoardArmourShieldAt`（= `3DBody` 坐标）**烘进顶点**
        /// ⇒ 它和卡面各层不在同一套坐标里 ⇒ 挂在 `Front`（+0.08）下要**抵消 0.08**。
        /// 两个入口（出生 `Build` / 换形态 `SetFace`）都走这一个函数。</summary>
        void PlaceArmourIcon()
        {
            if (_armourIcon == null) return;
            bool board = _faceMode == CardFace.Board;
            _armourIcon.transform.localPosition = new Vector3(0f, board ? -FrontAposY : 0f,
                                                              board ? BoardArmourIconZ : ArmourIconZ);
        }
        /// <summary>🆕 2026-10-12（`A1658`）效果文字底板那层的 z（判据用）。
        /// 判据 = 原版 `Front` 子序 `CardImage → Textbackgrounds → CardFrame → Card Info`
        /// ⇒ **必须比 `FrameZ` 大**（本仓 z 越小越靠前）、**比 `ArtZ` 小**。取不到那一层时 `NaN`。
        /// ⚠️ 名字带 `Layer` 是为了避开**同名的私有常量** `TextBgZ`（那个是建层用的值，不公开）。</summary>
        public float TextBgLayerZ { get { return _textBg != null ? _textBg.transform.localPosition.z : float.NaN; } }
        /// <summary>🆕 2026-10-12（`A1659`）效果文字底板那层的渲染器（判据用；没有那一层时 `null`）</summary>
        public MeshRenderer TextBgLayer { get { return _textBg; } }
        /// <summary>🆕 2026-10-12（`A1659`）卡框那层的渲染器（判据用）</summary>
        public MeshRenderer FrameLayer { get { return _frame; } }
        /// <summary>🆕 2026-10-12（`A1659`）效果文字那层 TMP（判据用：**它必须挂在 `FaceLayerRoot` 下**，
        /// 否则倾摆非零时它与底板/卡框分成两个平面 —— 那是用户报的「图标与文字不在一个层面上」的真因）</summary>
        public TextMeshPro KeywordsText { get { return _keywords; } }
        /// <summary>🆕 2026-10-12（`A1659`）卡名那层 TMP（判据同 `KeywordsText`）</summary>
        public TextMeshPro TitleText { get { return _title; } }
        /// <summary>🆕 2026-10-12（`A1659`）卡面各层的父节点 = 原版 `2DCard`（倾摆所在的那一层）。
        /// 自检拿它断「文字层挂对了父节点没有」。⚠️ `Build` 之前退回卡根。</summary>
        public Transform FaceLayerRoot { get { return FaceRoot; } }

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
        /// <summary>这张卡用的**卡框贴图**（`Build` 里挑的那一张）。🆕 2026-10-18（W15）：
        /// 2D 卡面上的**四个数值 / 费用六边形**要按它的 uv 反算落点（凹槽在哪，数就画在哪）——
        /// 见 `FramePxAt01` 那一段。占位卡面（`frameTex == null`）⇒ null。
        /// ⚠️ 与 `_frame` 不是一回事：`_frame` **只在占位卡面那条兜底支路**才非 null。</summary>
        Texture2D _frameTex;
        /// <summary>🆕 2026-10-24（`H5`）：**`Build` 那一趟用的立绘贴图 / 卡数据** —— 只给
        /// `RebakeBigStateMeshes` 用（Big 状态轴在 `Create` 之后才赋值 ⇒ 要按**建网格时那份**重烘，
        /// ⛔ 不是读当时的 `Data`：`Create` 里 `Build` 跑在 `SetData` **之前**，那时 `Data` 还是默认值）。
        /// ⚠️ 与 `_art.sharedMaterial.mainTexture` 不是同一个概念：后者会跟 `SetData` 换（那是贴图），
        /// 这两份是**网格几何**的输入（`SetData` 至今不重建几何，见 `SetData` 那段注释）。</summary>
        Texture2D _artTexBuilt;
        CardData _builtData;
        MeshRenderer _textBg;    // 🆕 文字底板（原版 `Front/Textbackgrounds/TextBackground * UI`）
        MeshRenderer _art;       // 立绘：**完整插图**（忽略 alpha，垫在卡框下）
        MeshRenderer _artFront;  // 立绘：**角色抠图**（真 alpha，盖在卡框上 ⇒ 越出卡框）
                                 // 🆕 2026-10-18：**大图档**（卡片详情窗扇形，`BigCardOverdraw` 默认开）会建它；
                                 // 小卡档不建（`UseFrontLayer` = false）⇒ 小卡上恒为 null
        MeshRenderer _gem;       // 卡面底部那颗稀有度宝石（原版 `Rarity`）
        MeshRenderer _costBg;    // 费用底板（原版 `Cost Container/Cost Background`，图 `Card_Frame_Cost_Icon`）
        /// <summary>🆕 2026-10-18（`A1549` 尾巴）**费用格建出来时用的是哪一档**：`0` = 普通、`1` = 大号、
        /// `-1` = 这张卡**没有**费用格（场上形态 / 督军 / 无底图）。运行时切 `SmallScreenUI` 之后
        /// 靠它判「已建好的卡要不要重做网格」。见 <see cref="RefreshCostTier"/>。</summary>
        int _costTierBuilt = -1;
        MeshRenderer _armourIcon;// 护甲盾牌底（原版 `Armour Container/Image`，图 `pedestal_icon_armor`）
        MeshRenderer _info;      // 数值层
        MeshRenderer _rim;       // 状态描边
        /// <summary>场上那四个数值**各占一个图层**（索引 `StatMelee..StatHealth` = 1..4，0 空着不用）。
        /// 为什么要拆：原版「受击时**那一个**数值变色 + bump」是按 counter 分别做的
        /// （`CardTextCountersController.DoColorChange` + `cardTextBumpSize/Time`），
        /// 四个数字烘在一张贴图里就动不了单个。判据 → `资料/待办判据_战场与战斗视图.md` §26 第 3 条。
        /// ⚠️ **只在场上（`CardFace.Board`）建与显示** —— 非场上那四档（手牌 / 详情窗 / 卡池）的数值
        /// 是**另一套载体**：🔴 2026-10-25（`CF-3`）起 = 四件**真 TMP**（`_faceStatText`，原版
        /// `2DCard/Front/Card Info/…` 同构）；再早是烘在 `_info` 贴图里（现在只剩「字体资产取不到」的兜底）。
        /// 两条路互斥，判据是 `_faceMode` + `SplitStats`。</summary>
        readonly MeshRenderer[] _stats = new MeshRenderer[5];
        /// <summary>四个数值各自的「闪现色」（`CardFeel.StatUpColor` / `StatDownColor`；白 = 没在闪）。
        /// 🔴 **必须存一份** —— `ApplyTint` 会把 `_layers` 里每一层刷成状态色，不重贴的话
        /// 受击变色撑不过一帧（和徽标那个 `_badgeAlpha` 是同一个坑）。</summary>
        readonly Color[] _statFlash = new Color[6];   // 索引 1..4 = 四个数值；**5 = 费用**
                                                      // （原版第五个 counter `MonoBehaviour_6320885271709195200`；
                                                      //  ⚠️ 费用的数字我们烘在 `_info` 里 ⇒ 涂不上色，见 `CaptureStatChanges` 开头那段注释）
        /// <summary>🆕 2026-10-25（`CF`）原版 `HighlightAttackType.originalSize`（`+0x48`）—— 那一格数值
        /// **首次被调用时**的 `localScale`。索引 = `StatMelee..StatHealth`。
        /// ⚠️ **惰性捕获**：原版**没有** `Start` / `Awake`（方法表只有 `get_TargetScale` / `HighlightIfNeeded` /
        /// `.ctor`）⇒ 初值 `(0,0,0)`，什么时候第一次被调到才什么时候记（判据 → `J2 …md` §A2）。</summary>
        readonly Vector3[] _statBase = new Vector3[5];
        /// <summary>🆕 2026-10-25（`CF`）那一格数值上正在跑的那条 `DOScale`（自检读时长用；重开前先杀）。索引同 `_statBase`。</summary>
        readonly Tween[] _statScaleTween = new Tween[5];
        /// <summary>底光取不到图（删了 `Resources/Art/`）时只警告一次 —— 那一档是支持的配置，但**不许静默**。</summary>
        bool _glowMissingWarned;

        // ---- 🆕 2026-10-25（`CF-3`）**2D 卡面那四个数值 = 真 TMP 节点**（原版同构）------------
        // 🔴 判据（一手，逐值抄自 prefab）：`CardPrefab/2DCard/Front/Card Info/`
        //    · `Melee Attack Container/Melee Attack Text` · `Range Attack Container/Range Attack Text`
        //    · `Health Container/HealthText` · `Armour Container/Armour Text`
        //    四个都是 `TextMeshPro`（**不是**烘进位图），`m_LocalScale = 1`、`m_fontSize = 0.34`、
        //    `m_enableAutoSizing = 1`（`0.025 ~ 0.34`）、`m_fontColor = 白`、对齐 Center/Middle、
        //    字体 = `Pragati-Regular`（= 我们的 `TmpFont.Face.LatinCardBody`）、
        //    材质 = `Pragati-Regular Atlas Material **Thin** Outline`（近战/远程/生命）
        //         / `… **Thick** Outline`（**护甲**）—— 描边值逐项见 `ApplyStatOutline`。
        //    · 数值来源 = `CardTextCountersController.SetText(...)`：近战/远程来自
        //      `CardScript.UpdateAttackText`（`CurrentMeleeAttack` / `CurrentRangeAttack`）、
        //      生命来自 `CardScript.UpdateHealthText`、护甲来自 `BasicCardUI.SetRawCardData`
        //      （`RawCardScript +0x78`）；**我们这边统一走 `StatValue(Data, s)`**（与场上那四层同一个判据）。
        //    ⛔ **这四件不挂 `HighlightAttackType`** —— 原版手牌/详情窗**没有**那一档（判据见 `J2 …md` §A1 与本节
        //      「攻击类型高亮」那一整段的父链证据），别把高亮接上来。
        readonly TextMeshPro[] _faceStatText = new TextMeshPro[5];      // 索引 = `StatMelee..StatHealth`

        // ---- 🆕 2026-10-25（`E` · K4 §②·D3）：**费用数字 = 第五件真 TMP**（原版 `Cost Container/CostText`）----
        //  判据（prefab 实读，`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/CostText*.json`）：
        //   · 普通档 `CostText`：sizeDelta **0.38×0.35** · apos **(−0.00999,−0.01)** · scale 1 ·
        //     `m_fontSize 0.34` · **autoSizing 1**（min 0.025 / max 0.34）· `m_fontStyle 0`(Normal) ·
        //     白 · hAlign **2 (Center)** · vAlign **512 (Middle)** · `m_TextWrappingMode 0`（不折行）·
        //     材质 `<Patern> Thin Outline`；
        //   · 大号档另一颗 `CostText_3722786347926395840`：sizeDelta **0.456×0.42** · `m_fontSize 0.475` ·
        //     **autoSizing 0**（固定字号）· 其余同。
        //  ⇒ 两档是**两个节点**（与 `Cost Container`/`Cost Container Big` 成对），用 `SmallScreenUI` 切。
        /// <summary>费用数字那件 TMP（没建 = null；建的条件 = `ShowsCost(Data) && TmpFont.Available && 非场上`）。</summary>
        TextMeshPro _faceCostText;
        RectTransform _faceCostRect;
        GameObject _faceCostContainer;
        /// <summary>费用 TMP 普通档的框（原版 `CostText` 0.38×0.35）。</summary>
        public static readonly Vector2 CostTextSize = new Vector2(0.38000f, 0.35000f);
        /// <summary>大号档的框（原版 `CostText_…` 0.456×0.42 = 普通 ×1.2）。</summary>
        public static readonly Vector2 CostTextSizeBig = new Vector2(0.45600f, 0.42000f);
        /// <summary>普通档字号（em，原版 `m_fontSize 0.34`）与 auto 下限（0.025）。</summary>
        public const float CostTextEm = 0.34f, CostTextAutoMinEm = 0.025f;
        /// <summary>大号档字号（em，原版那颗 `m_fontSize 0.475`、auto 关）。</summary>
        public const float CostTextBigEm = 0.475f;
        /// <summary>数字框相对容器中心的小偏移（原版 `m_AnchoredPosition (−0.00999, −0.01)`；
        /// 原版这一颗挂在**被 scale 1.2 的容器**下 ⇒ 我们手动乘 `CostScale`）。</summary>
        public const float CostTextDx = -0.00999f, CostTextDy = -0.01000f;

        readonly RectTransform[] _faceStatRect = new RectTransform[5];  // 那件 TMP 自己的 rect（原版 sizeDelta）
        readonly GameObject[] _faceStatContainer = new GameObject[5];   // 原版四个 Container（显隐挂在它上面）

        // 原版逐值（`Card Info` 空间 = 卡单位，与 `MeleeAt` 那一套同标架）
        /// <summary>原版 `Melee/Range/Health/Armour *Text` 的 `m_fontSize`（= autoSize 上限，同一个值）。
        /// 🔴 **这是【原版 em】，不是我们该填的 `fontSize`** —— 原版那四件的 `m_LocalScale = 1`，
        /// 而原版卡面文字的换算就是 `em = m_fontSize × m_LocalScale`（同 `NameTextUnit` 19 × 0.01 = 0.19）；
        /// 我们这份拉丁字体资产是「em = fontSize × 0.1」（`TmpFont.TrueEmPerFontSize`）
        /// ⇒ 填进 TMP 要 ×10（`FaceStatFontSizeOur`）。⛔ 直接把 0.34 填进去会**小 10 倍**。</summary>
        public const float FaceStatFontSize = 0.34f;
        /// <summary>原版那四件的 `m_fontSizeMin`（同样要 ×10 才填进我们的 TMP）。</summary>
        public const float FaceStatFontSizeMin = 0.025f;
        /// <summary>折算到我们 TMP 的 `fontSize`（= 原版 em ÷ `TrueEmPerFontSize` = 0.34 / 0.1 = **3.4**）。</summary>
        public static float FaceStatFontSizeOur
        {
            get { return TmpFont.FontSizeForEm(TmpFont.Face.LatinCardBody, FaceStatFontSize); }
        }
        /// <summary>原版 `Melee/Range Attack Text` 的 `m_SizeDelta` = **0.30006 × 0.35**。</summary>
        public static readonly Vector2 FaceStatTextSize = new Vector2(0.30006f, 0.35f);
        /// <summary>原版 `HealthText` 的 `m_SizeDelta` = **0.33 × 0.35**。</summary>
        public static readonly Vector2 FaceStatTextSizeHealth = new Vector2(0.33f, 0.35f);
        /// <summary>原版 `Armour Text` 的 `m_SizeDelta` = **0.32 × 0.35**、`m_AnchoredPosition` = **(−0.00495, −0.04197)**。</summary>
        public static readonly Vector2 FaceStatTextSizeArmour = new Vector2(0.32f, 0.35f);
        static readonly Vector2 FaceStatArmourTextOffset = new Vector2(-0.0049498f, -0.0419734f);
        /// <summary>原版三个容器（近战/远程/生命）的 `m_SizeDelta` = **0.4 × 0.4**；**护甲那个是 0.3 × 0.3**。</summary>
        public const float FaceStatContainerSize = 0.4f;
        /// <summary>原版 `Armour Container` 的 `m_SizeDelta` = **0.3 × 0.3**。</summary>
        public const float FaceStatContainerSizeArmour = 0.3f;
        // 描边（`bundle_fonts_assets_all/Material/Material_-8115038251467144366.json` 等，逐字段实读）
        const float StatOutlineThin = 0.05f, StatOutlineThick = 0.097f;
        const float StatFaceDilateThin = 0.05f, StatFaceDilateThick = 0.105f;
        static readonly Color StatOutlineColorThin = new Color(0f, 0f, 0f, 0.8745098f);
        static readonly Color StatOutlineColorThick = new Color(0f, 0f, 0f, 0.7882353f);
        MeshRenderer _ephemeral; // 🆕 「临时卡」角标（原版关键词图标；默认关掉）

        // ---- 🆕 棋盘单位卡的 buff / debuff 徽标（原版 `BattleCardUI.boardTraitIcons`）--------
        // 判据（哪些关键词进位、认不认得出图、角标画不画）**全在 `Core/Badges.cs`**；
        // 这里只管画。位子/大小取自原版预制体 `TraitIconContainer*.json` 的 Transform，
        // 换算过程写在 `Badges.SlotAt` 上面 —— **不在这里写第二份**。
        readonly List<MeshRenderer> _badgePlates = new List<MeshRenderer>();
        readonly List<MeshRenderer> _badgeIcons = new List<MeshRenderer>();
        /// <summary>🆕 2026-10-18（`A1676`）**未激活那枚 ✕**（原版 `traitNotActiveIcons[]` / `Trait Not Active`）。
        /// 与 `_badgeIcons` **同位、同中心**、边长 ×0.64533、z 更靠前 0.0030 —— 判据与出处见
        /// `Badges.NotActiveScale` 那一段。⚠️ 懒建：这一位从没进过未激活态 ⇒ 这个列表里可能是 `null`。</summary>
        readonly List<MeshRenderer> _badgeCrosses = new List<MeshRenderer>();
        readonly List<TextMeshPro> _badgeCounters = new List<TextMeshPro>();
        /// <summary>现在画着的那几位（`SetBadges` 进来的那一份）—— 「按关键词脉冲」要拿它配对。</summary>
        readonly List<Badge> _badges = new List<Badge>();
        TextMeshPro _title;      // 卡名（TMP。没有字体资产时为 null，字烘在 _info 里）
        TextMeshPro _keywords;   // 关键词（同上）
        TextMeshPro _army;       // 阵营行（原版 `ArmyTextUnit` / `ArmyTextTactc`）
        TextMeshPro _race;       // 兵种行（原版 `RaceText`；战术/防御卡印不印由 `SubtypeLine` 判）
        /// <summary>兵种行这一层**摆位用的 y01**（y 从卡顶算）—— 判据见 `TacticRaceAt` 的 doc。
        /// `NaN` = 这张卡没印那一行（`_race == null` 时由 <see cref="RaceAnchorY01ForTest"/> 报出来）。</summary>
        float _raceY01 = float.NaN;
        /// <summary>🆕 2026-10-18（`A985④`）**「由谁造出来的」那行字**（原版 `CreatedByText` 节点）。
        /// `null` = 没这一层（没有来源 / 没字体资产 / 场上形态）。判据见 <see cref="CreatedByAt01"/> 那一段。</summary>
        TextMeshPro _createdBy;
        /// <summary>这张视图现在要显示**哪个创建者**的卡名（`null` = 不显示）。
        /// 🔴 **必须存一份**：`BuildTextLayers` 会被 `SetData` 反复调（战场每刷新一次都走）
        ///    —— 不存的话每次刷新这行字都会被 `Fill(空)` 拆掉（`EphemeralShown` 那类状态同理）。</summary>
        string _createdByName;
        /// <summary>🆕 2026-10-22（`W62/D5①`）这张视图现在处在本层的**哪一档原版状态**
        /// （`CardStateOptions` 那 18 档里我们用到的那四档，见 `SetCreatedBy` 注释）。
        /// ⚠️ 默认**必须是 `Hidden`**（枚举里它是 `-1`，而 C# 字段默认值是 `0` ⇒ 这里显式初始化）——
        ///   漏了这一步，所有没推过状态的视图（详情窗 / 收藏 / 展示窗）都会落在「亮着」那一档。</summary>
        CreatedByState _createdByState = CreatedByState.Hidden;
        /// <summary>🆕 2026-10-22（`W62/D5①`）原版 `CardScript +0x40`（`isPlayer`）：只影响
        /// `InHandPlaying` 那一档的显隐（我方打出的那张**不印**，见 `SetCreatedBy` 的判据表）。</summary>
        bool _createdByIsLocalPlayer = true;
        static Material _quadMat;

        readonly List<MeshRenderer> _layers = new List<MeshRenderer>();

        // ==================================================================
        //  🆕 2026-10-12（`A1665`）**每张卡一段渲染队列** —— 卡与卡之间的前后关系
        // ==================================================================
        //
        // **为什么必须有它**：卡面**每一层**原本都拿 `new Material(Sprites/Default)`（Transparent 队列
        // **3000**）⇒ 两张卡叠在一起时，「谁盖谁」只剩「**到相机的 3D 距离**」在排 —— 而本仓明令
        // 禁止拿它当分层手段（透明队列里同队列按距离排；手牌的步长 `HandLayout.zOrderStep = 0.08`
        // 甚至**小于卡内真实 z 跨度 0.09**（`shadow +0.06` … 文字 `−0.03`））⇒ **后面那张卡的
        // 卡面文字会穿到前面那张卡上面**（用户 2026-09-12 报过一次、2026-10-12 又报了一次）。
        //
        // **做法**（与 `Core/CardFan.cs` 的 `SetCardQueue` 同一套，**判据只此一处**）：
        //   · **一张卡 = 一段队列**：这张卡**所有**层（含 TMP、含后建的数值/徽标层）共用同一个号；
        //   · **卡内层序仍靠 z**（只在一张卡的范围内排序，跨度 0.09、各层几乎同屏 ⇒ 可靠）；
        //   · **卡间**由布局（`HandLayout` / 未来的 `BoardLayout`）给**不同的号**。
        // ⇒ 只要「前卡整段 > 后卡整段」，前卡**任何**一层都压得住后卡**任何**一层 —— 这条是
        //   `Editor/CardFaceProbe.cs` 的 `CheckLayers` 里那条**必红**断言（退回「全 3000」就红）。
        //
        // 🔴 **号段约束**：`QCard = 3000` 是**上限**，不是随便挑的 —— 全仓有几处按「卡 ≤ 3000」写死：
        //   `Board/BoardLayout.cs:111`（`MarkerQ = 3100` **必须大于卡的 3000**）·
        //   `Core/CardWinBox.cs:83`（`QEffect = 3020` 画在卡之上）·
        //   `Battle/CardDisplayWindow.cs:83`（`QChrome = 3000` 遮罩，靠 z=−2 压住手牌）。
        //   ⇒ 手牌那一段是**从 3000 往下**排的（见 `HandLayout.queueTop`），不是往上。
        /// <summary>卡面各层的**默认**渲染队列（= `Sprites/Default` 的 `Transparent` 队列）。
        /// 也是**卡这张卡自己的号段上限** —— 见上面那段「号段约束」。</summary>
        public const int QCard = 3000;

        /// <summary>🆕 2026-10-23（D · **F6**）**手牌「活动中」那一段**（原版排序层 `HandMoving` 的等价物）。
        ///
        /// <para>**原版判据**（`d:/2/tools/decomp_full/`，四个方法逐句核过）：手牌常态 = `Hand` 层
        /// （`PlayerHand__SetupCardInHand.c` → `SetSortingLayer(Hand)` + `SetSortingOrder(0)`）；
        /// 而**「这张牌正在动」的三态**一律进 **`HandMoving`** 层 ——
        /// `CardScript__ShowCardInHand.c`（抬起：设 state 8 后 `SetSortingLayer(<HandMoving>)`）·
        /// `CardScript__ShowCardBeingDrawn.c`（抽出来给人看）·
        /// `CardScript__ShowCardInCenter.c`（打出后居中展示）。
        /// `StopShowingCardInHand` / `MoveBackToHand` 再退回 `Hand`。
        /// ⇒ **`HandMoving` 高于 `Hand`** 是原版明写的，不是我们猜的。</para>
        ///
        /// <para>**我们怎么映射**：我们是「一张卡 = 一段 renderQueue」，没有 `HandMoving` 层。
        /// 手牌段 = `HandLayout.queueTop`(3000) 往下排（下限 `QueueFloor` 2980）⇒ 它的**上一格**
        /// 就是「高于整条手牌」的等价物 = **3001**。号段是空的：`3001–3008` 无人使用，
        /// 3009 起是 `CardFan.QCardBase`（两扇详情窗的卡叠，见那条注释）、3020 `CardWinBox.QEffect`、
        /// 3100 `BoardLayout.MarkerQ` ⇒ 取 3001 **不撞号、也不越过任何卡之外的件**。</para>
        ///
        /// <para>⚠️ **抬起**那一档**不用**它：`HandLayout.Refresh` 已经把悬停那张给到 `queueTop`(3000)、
        /// 其余给 `queueTop − (slot+1)` ⇒ **悬停严格高于所有手牌**，相对序与 `Hand` vs `HandMoving`
        /// 同向（见 `HandLayout` 那一行注释）。</para>
        /// <para>⚠️ **`ShowCardBeingDrawn` 这一态我们暂无对应件**（原版用在「从手里抽出来给人看」：
        /// `ResolveDiscardFromHand` / `ResolveStealFromDeck`）—— 如实记着，⛔ 别为它硬造一条路径。</para></summary>
        public const int QHandMoving = QCard + 1;    // 3001

        int _queue = QCard;

        /// <summary>这张卡现在那一整段的队列号（布局给的；没被布局摆过就是 `QCard`）。
        /// 自检拿它断「卡与卡的号不重复 / 前卡整段大于后卡整段」。</summary>
        public int Queue { get { return _queue; } }

        /// <summary>**把这张卡的每一层都设成同一个队列号**（= 给这张卡一段队列）。
        /// <para>🔴 **本工程「每张卡一段队列」的唯一实现** —— `CardFan.SetCardQueue` 只是转发到这儿
        /// （⛔ 别在任何地方再写第二份；两处写同一条规则 = 迟早不一致）。</para>
        /// <para>🔴 **2026-10-19（`A1967`）起不再调用 `.material`**：编辑模式下 `Renderer.material`
        /// **每实例化一次**就打一条 `Instantiating material due to calling renderer.material during edit mode …`
        /// （带 14–16 行托管栈；`BattleScene.Run` 2026-10-14 实测 **10,642 条 = 全文 52.6%**）。
        /// 现在走 <see cref="QueueMaterial"/> 的 **(源材质, 渲染队列) 共享缓存**：同一份源材质在同一队列号上
        /// 只克隆一次、赋给 `sharedMaterial`。文字那层的 `sharedMaterial` 是**字体资产里那一份**
        /// （TMP 的 MeshRenderer）—— 我们克隆的是它，**原件一个字节都不写**（直接写会把
        /// `Resources/Fonts/Warpforge Trait TextSprites.asset` 的 `m_CustomRenderQueue` 写脏并**落盘**、
        /// 还连累所有用同一字体的文字，2026-09-27 实测踩过）。</para>
        /// <para>⚠️ 点阵后端（`Label` 自己挂 MeshRenderer）走 `Label.SetRenderQueue`（它也会先实例化）。</para>
        /// <para>⚠️ 它**扫整棵子树**，所以「后建的层」（数值层 `BuildStatLayers` / 徽标 / 重建的 TMP）
        /// 只要下一次 `SetQueue` 时已经存在就会被带上；建层那几处另有 `StampQueue` 兜当场那一次。</para></summary>
        public void SetQueue(int q)
        {
            _queue = q;
            foreach (var mr in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.GetComponent<Label>() != null) continue;   // 点阵文字：走下面那个（它自己实例化）
                StampQueue(mr);
            }
            // 🆕 2026-10-18（`A1674`）：**粒子层也盖章**（`ParticleSystemRenderer` 不是 `MeshRenderer`，
            // 上面那个循环扫不到它）。判据（原版 prefab 逐字段实读，见 `BuildCanAct` 那一段）：
            //   · `CanActParticles` / `RotatingRing` 的 `m_SortingOrder = 0`、`m_SortingLayerID = 0`、
            //     `m_SortingFudge = −3 / −2`、材质 `m_CustomRenderQueue = 3000`；
            //   · Unity 的语义：**负的 `sortingFudge` = 排在同队列其它透明物【之前】（= 后面）** ——
            //     原版就是靠这个把绿光压在卡面其它透明层之后的；它和卡面**本来就在同一个队列（3000）**。
            //   ⇒ 我们把这几个粒子也盖成**这张卡的队列号**，复现的是同一件事：
            //     卡内仍由距离 + fudge 排序（原版值不动），卡间则由队列段隔离（不再盖到邻居卡上）。
            //   ⛔ **`SpriteRenderer` 不在此列**：① `BlobShadow` 已判「不需要」（卡体 `ZWrite On`、
            //     影子 `zT=LEqual` ⇒ 深度测试挡掉，与队列无关，见 `资料/普查产出_第二十二会话/W4_场上卡层级.md`）；
            //     ② 残骸体那圈光（`RemnantLight`）属于**特效 prefab 实例**，`SetQueue` 不能替它重排层序。
            //   ⚠️ 残骸体子树里的粒子同理**不盖章**：那批 prefab 自带 `m_SortingOrder`（实测 0 / 1 / 155 …）
            //     与 `m_SortingFudge`（0 / 4.38 / 17.07 …）在排层，硬盖队列会把它的内部层序压平。
            foreach (var pr in GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (_remnantBody != null && pr.transform.IsChildOf(_remnantBody.transform)) continue;
                StampQueue(pr);
            }
            foreach (var lb in GetComponentsInChildren<Label>(true)) lb.SetRenderQueue(q);
            // 🆕 2026-10-23（D · `F4`）：**卡上特效跟着这张卡换段**（判据见 `StampEffectRoot` 那段）。
            //   顺序要在 `ApplyBoardSortingOrders` **之前** —— 那一趟会按层名给卡面定序，
            //   而特效根的层号由这里自己管（`ApplySortingOrderTree` 已显式跳过登记的根）。
            for (int i = _effectRoots.Count - 1; i >= 0; i--)
            {
                var fx = _effectRoots[i];
                if (fx == null) { _effectRoots.RemoveAt(i); continue; }   // 特效播完会自毁 ⇒ 清账
                StampEffectRootNow(fx);
            }
            ApplyBoardSortingOrders();   // 🔴 2026-10-21：同队列内再按层号定序（见方法注释）
        }

        /// <summary>🔴 **2026-10-21（第六十会话 · 真机缺陷：场上词条徽标被底板盖住 / 数值被底光盖掉）**。
        ///
        /// <para>**为什么 z 不够**：Unity 的透明队列**按到相机的包围盒中心距离排序，不是按 z**
        /// （本文件 `:1105-1111` 早写过这条限制）。卡内各层 z 差只有 0.006（徽标板 −0.118 / 图标 −0.124）
        /// 或 0.14（底光 −0.01 / 数值 −0.150）—— 卡越偏离相机轴、横向差越大，距离就把 z 序翻过来
        /// ⇒ 徽标底板压住图标、合法目标底光压住数值（真机 07/19 截图两处都中）。</para>
        /// <para>**修法**：同队列内 `Renderer.sortingOrder` **先于**距离比较 ⇒ 给卡内每层显式层号
        /// （数值 60 > 护甲 50 > 徽标 42/41/40 > 文字层 32 … > 立绘 10）。**只对场上形态生效**
        /// （`CardFace.Board`）—— 场上每张卡由 `BoardLayout.QueueOf` 给了一段**独立队列**，
        /// 卡与卡之间仍由队列段隔离，不会互相穿插；手牌/收藏/大图档一个字节不动。</para>
        /// <para>⛔ **别改 `BoardLayout` 那套队列号**（那是一张卡一段的唯一实现）。</para></summary>
        void ApplyBoardSortingOrders()
        {
            if (_faceMode != CardFace.Board) return;
            // ⚠️ **按 FaceRoot 的【每个直接子件】分别定序** —— 整棵树按 FaceRoot 的名字取号
            //    会把所有层都设成同一个号（2026-10-21 实现时踩过，当场改）。
            // 🔴 **2026-10-25（`M2`）**：卡面各层现在在 `2DCard/Front` 那一级下面（见 `FrontAposY`）
            //    ⇒ 碰到那颗 `Front` 要**再下钻一层**（否则整棵卡面树按名字 "Front" 取号 ⇒ 恒 −1 ⇒
            //    一条层号都不设，场上「徽标被底板盖住」那类缺陷会静默回来）。
            if (FaceRoot != null)
                foreach (Transform c in FaceRoot)
                {
                    if (c == _frontRoot) { foreach (Transform cc in c) ApplySortingOrderTree(cc); continue; }
                    ApplySortingOrderTree(c);
                }
            // 卡根下还挂着：合法目标底光（`glow_melee/ranged`）与 3D 卡体 —— 也一并定序。
            foreach (Transform c in transform)
                if (c != FaceRoot) ApplySortingOrderTree(c);
        }

        void ApplySortingOrderTree(Transform root)
        {
            if (root == null) return;
            // 🆕 2026-10-23（D · `F4`）：**卡上特效根的层号归 `StampEffectRootNow` 管**（基准 70 + 原值）。
            //   不跳过的话，特效根一旦跟卡面某个层**重名**（如 `shadow`）就会被这趟按层名改成 1，
            //   于是「特效在卡面之上」静默失效。
            if (_effectRoots.Contains(root)) return;
            int o = SortingOrderFor(root.name);
            if (o < 0) return;   // 未列名的层（VFX / 血痕 / 3D 卡体等）**不动** —— 别把它们统一压成 0
            foreach (var r in root.GetComponentsInChildren<Renderer>(true)) r.sortingOrder = o;
        }

        /// <summary>卡内一层的排序号（只在**同一队列**内比较；判据见 `ApplyBoardSortingOrders`）。
        /// 数值 = 原版 `Highlight.SortingOrder` 那一套的语义：越大越靠前。</summary>
        static int SortingOrderFor(string n)
        {
            if (string.IsNullOrEmpty(n)) return 0;
            if (n == "shadow") return 1;
            if (n == "art") return 10;
            if (n == "glow_melee" || n == "glow_ranged") return 15;
            if (n == "frame") return 20;
            if (n == "artFront") return 25;
            if (n == "textBg") return 30;
            if (n == "costBg") return 31;
            if (n == "info") return 32;
            if (n == "gem") return 33;
            if (n == "ephemeral") return 34;
            if (n.StartsWith("badgePlate")) return 40;
            if (n.StartsWith("badgeIcon")) return 41;
            if (n.StartsWith("badgeCounter")) return 42;
            if (n == "armourIcon") return 50;
            if (n.StartsWith("stat")) return 60;
            return -1;  // 未列名的层（VFX / 血痕 / 3D 卡体…）：**不动它的 sortingOrder**（保持原值/默认）
        }

        /// <summary>把**这一层**的队列设成这张卡当前那一段（建层当场调一次 —— 免得「建出来之后
        /// 还没被布局摆过」的那段时间里它是默认 3000）。
        /// <para>🔴 **值已经对了就一个字节都不写**：`new Material(...)` 会**多一份材质**，而 TMP 那几层的
        /// `sharedMaterial` 是**字体资产里那一份**（全卡共用 ⇒ 能合批）⇒ 无脑克隆会把每张卡的每层文字
        /// 都拆成独立材质（多出几十个 draw call），而且是**白拆**（默认 3000 == 卡片队列段的默认号）。
        /// 只有布局真给了别的号（`≠ QCard`）时才需要一份实例 —— 那时不合批是应该的。</para>
        /// <para>🔴 **2026-10-19（`A1967`）：不再走 `mr.material`** —— 编辑模式下它每实例化一次就打一条
        /// `Instantiating material due to calling renderer.material during edit mode …`
        /// （2026-10-14 实测 **10,642 条 = 全文 52.6%**）。改成 <see cref="QueueMaterial"/> 的
        /// **(源材质, 队列)** 共享缓存：同源同队列只克隆一次，赋给 `sharedMaterial`。
        /// ⛔ **键必须含队列**，否则不同队列段的卡会共用一份材质、被压到同一层。
        /// ⚠️ 用「这一层**第一次**被盖章时的那份材质」当源（<see cref="_stampState"/>）：布局会把同一张卡
        /// 从一档队列改到另一档（悬停 / 重排），若拿「当前那份（已经是上一档的克隆）」当键会克隆出一条链。
        /// 外部换了材质（`SetFace` / `SetData` 换卡面/卡背）⇒ 当前那份不再是本口给的克隆 ⇒ 重新以它为源。</para>
        /// <para>⚠️ **「每张卡一份材质」这个既有假设仍然成立**：卡面各层（`AddLayer` / 徽标 / 数值层 …）
        /// 的源材质都是 `new Material(...)` 的**私有副本** ⇒ 缓存出来的克隆也是**逐层唯一**的
        /// ⇒ `ApplyTint` / `ApplyShadowColor` 写 `sharedMaterial.color/_ShadowColor` **不会串卡**。
        /// 真正会被共享的只有 TMP 文字那几层（字体资产材质）—— 它们本来就不吃 `sharedMaterial` 的着色
        /// （TMP 走顶点色）。⚠️ 将来若新增「多张卡共用同一份源材质、又要被整卡着色」的层，
        /// 那一路必须改走 `MaterialPropertyBlock`，⛔ 不能写共享材质。</para>
        /// <para>⚠️ 形参是 <see cref="Renderer"/>（2026-10-18 `A1674` 起）：`ParticleSystemRenderer`
        /// 不是 `MeshRenderer`，但要盖的是同一件事（`sharedMaterial.renderQueue`）。</para></summary>
        void StampQueue(Renderer mr)
        {
            if (mr == null || mr.sharedMaterial == null) return;
            // 🔴 2026-10-23（D4 · F1 Step 2）：**3D 卡体不参与「一张卡一段队列」。**
            //   判据 = 原版 `Card 3d Lvl1` 的 shader `Everguild/Cards/3D Card` 子标签
            //   `QUEUE=Geometry`（不透明档）+ `blend (1,0)` + `zWrite 1`（原始 bundle 实读；
            //   全文 → `资料/普查产出_1023/D3_F1判据与方案.md`）。
            //   为什么必须在这里拦：`renderQueue` **高于** sortingLayer/sortingOrder（⚠️ 只在
            //   **两个世界 `MeshRenderer` 之间**成立 —— 面对**画布件**时反过来：画布排序层先比、
            //   队列轮不到，见 `Battle/HudCanvas.cs:21` 与 2026-10-24 换牌压暗层那条记录）
            //   ⇒ 一旦盖成场上号段（2962–2979）或手牌号段（2980–3000），卡体就掉回透明档，
            //   shader 那两条标签等于白改（`Shaders/Card3D.shader` 的 SubShader 头注里也写了这条）。
            //   顺带治掉一个**老隐患**：溶解进行中被盖章时，`StampQueue` 会把渲染器换成
            //   `QueueMaterial` 的**克隆**，而 `SetDissolveAmount` 写的是 `_dissolveMat` 原件
            //   ⇒ 溶解会**静默卡住**（写进没人用的那份）。拦在这里，两条一起没了。
            //   ⚠️ 只拦**卡体这一颗**：卡面各层 / 粒子仍照旧盖章（`A1665`/`A1674` 那两族判据靠它）。
            if (mr == _body3D) return;
            var cur = mr.sharedMaterial;
            if (cur.renderQueue == _queue) return;   // 无操作就别拆材质（见 doc）

            StampState st;
            if (!_stampState.TryGetValue(mr, out st) || st.Assigned == null || cur != st.Assigned)
                st = new StampState { Origin = cur, Assigned = null };   // 第一次，或外部换了材质
            // 源材质本身就是这一档 ⇒ 直接用回**原件**（常见：悬停抬号后又落回 3000）
            //   —— 少一份克隆，也把 TMP 文字层还回字体资产那一份（恢复合批）。
            var cached = (st.Origin != null && st.Origin.renderQueue == _queue)
                       ? st.Origin : QueueMaterial(st.Origin, _queue);
            if (cached == null) return;
            mr.sharedMaterial = cached;              // ⛔ 不写 `mr.material`（见 doc）
            st.Assigned = cached;
            _stampState[mr] = st;
        }

        // ---- 🆕 2026-10-19（`A1967`）按 **(源材质, 队列)** 共享的实例缓存 ----
        //  为什么：`Renderer.material` 在编辑模式下**每次实例化**都打一条
        //  `Instantiating material due to calling renderer.material during edit mode …`（14–16 行栈）。
        //  2026-10-14 的 `BattleScene.Run` 日志里 **10,642 条 = 全文 52.6%**（`A1967` 原始读数；
        //  全仓历史日志里这一族 100% 的栈都落在本方法与 `CardFan.SetCardQueue` 转发上）。
        //  做法：自己 `new Material(src)`（**不走 `Renderer.material`，不触发那条警告**），
        //  按 `(src, 队列号)` 缓存后赋给 `sharedMaterial`。
        //  ⚠️ **条目数有上限**：源材质绝大多数是逐卡私有的（= 与修复前「一层一份」同量级，没有放大），
        //    真机长局里卡建了又销会攒；超过 `QueueMatCap` 就整表丢掉 —— 之后按需重建，
        //    行为与修复前逐次克隆等价（只是那一小段恢复不了合批）。
        struct QueueMatKey : System.IEquatable<QueueMatKey>
        {
            public int SrcId, Queue;
            public bool Equals(QueueMatKey o) { return SrcId == o.SrcId && Queue == o.Queue; }
            public override bool Equals(object o) { return o is QueueMatKey && Equals((QueueMatKey)o); }
            public override int GetHashCode() { return unchecked(SrcId * 397) ^ Queue; }
        }

        sealed class QueueMatEntry { public Material Origin, Clone; }

        const int QueueMatCap = 4096;
        static readonly Dictionary<QueueMatKey, QueueMatEntry> _queueMats =
            new Dictionary<QueueMatKey, QueueMatEntry>();

        /// <summary>（本视图内）每个渲染器「第一次盖章时的源材质 / 上一次由本口赋上去的克隆」——
        /// 用途见 `StampQueue` 的 doc（反复换队列号时始终以最初那份为键，避免克隆链）。
        /// ⚠️ 句柄随本视图一起丢；渲染器被单独销毁时残留的键只占一个槽，不影响正确性。</summary>
        struct StampState { public Material Origin, Assigned; }
        readonly Dictionary<Renderer, StampState> _stampState = new Dictionary<Renderer, StampState>();

        /// <summary>取「源材质 src + 队列 q」那一份共享实例（没有就建一份）。
        /// <para>⚠️ 返回值**可能被同队列的多个渲染器共用** ⇒ 调用方**不许**在上面写任何逐卡属性
        /// （颜色 / 贴图 / 关键字）。逐卡属性走 `MaterialPropertyBlock`，或先自己
        /// `new Material(...)` 复制一份私有副本（`BattleDriver.StartRevealFlight` 的淡出就是后者）。</para>
        /// <para>⚠️ 命中时校验 `Origin` 仍是同一份（Unity 的 `==` 会把已销毁对象判成 false）
        /// ——实例 ID 在旧对象销毁后可能被复用，只按 ID 命中会静默拿到别人的贴图。</para></summary>
        static Material QueueMaterial(Material src, int q)
        {
            if (src == null) return null;
            var key = new QueueMatKey { SrcId = src.GetInstanceID(), Queue = q };
            QueueMatEntry hit;
            if (_queueMats.TryGetValue(key, out hit) && hit != null && hit.Clone != null && hit.Origin == src)
                return hit.Clone;
            if (_queueMats.Count >= QueueMatCap) _queueMats.Clear();   // 见上面「条目数有上限」
            var m = new Material(src) { name = src.name + " (q" + q + ")" };
            m.renderQueue = q;
            _queueMats[key] = new QueueMatEntry { Origin = src, Clone = m };
            return m;
        }

        /// <summary>一颗 TMP 的**全部**渲染器（主网格 + 它的 `TMP_SubMesh`，行内 `&lt;sprite&gt;` 图标
        /// 在子网格上、**自带一个 MeshRenderer** ⇒ 「图标与文字同面/同队列」靠它一起刷）。
        /// ⚠️ 子网格是**文本里出现 `&lt;sprite&gt;` 之后**才建的 ⇒ 建层当场刷一次、`Fill` 收尾再刷一次
        /// （`SetQueue` 扫整棵子树是最后一道兜底）。</summary>
        void StampTmpQueue(TextMeshPro t)
        {
            if (t == null) return;
            foreach (var mr in t.GetComponentsInChildren<MeshRenderer>(true)) StampQueue(mr);
        }

        /// <summary>🆕 自检用：这张卡**子树里所有渲染器**的队列号（含 TMP 与其 sprite 子网格、含 `Label`、
        /// 🆕 `A1674` 起还含**卡自己的粒子层**）。
        /// 「前卡整段 &gt; 后卡整段」那条断言用的是它的 `Min`/`Max` —— 用集合而不是单个号，
        /// 是为了**将来哪天卡内层改成 `q + 偏移`** 时那条断言仍然成立（现在就写死成「一个号」会锁住实现）。
        /// ⚠️ 残骸体（特效 prefab 实例）的粒子**不进集合** —— 它不由 `SetQueue` 盖章（见 `SetQueue` 的注释）。</summary>
        public int[] SubtreeQueues()
        {
            var all = GetComponentsInChildren<MeshRenderer>(true);
            var list = new List<int>(all.Length + 2);
            foreach (var mr in all)
            {
                // 🔴 2026-10-23（D4 · F1 Step 2）：**3D 卡体不算「这段队列」的成员** ——
                //   它自本批起是**不透明档**（shader 标签 `QUEUE=Geometry` = 2000），
                //   已经不在「一张卡一段队列」那套里（`StampQueue` 同批跳过它）。
                //   ⚠️ 不排除的话，所有拿本方法做 min/max 的判据都会把 2000 算进来：
                //     · `CardFaceProbe` 的 `A1665`「前卡整段 > 后卡整段」
                //     · `BattleScene` 的 `A1672②`「场上每张卡的每一层都在它自己那一段里」
                //   那两条量的是**透明层**的号段 ⇒ 卡体本来就不该出现在集合里。
                //   （判据全文 → `资料/普查产出_1023/D3_F1判据与方案.md` §4.1。）
                if (mr == _body3D) continue;
                if (mr.sharedMaterial != null) list.Add(mr.sharedMaterial.renderQueue);
            }
            foreach (var pr in GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (_remnantBody != null && pr.transform.IsChildOf(_remnantBody.transform)) continue;
                if (pr.sharedMaterial != null) list.Add(pr.sharedMaterial.renderQueue);
            }
            return list.ToArray();
        }

        // ==================================================================
        //  🆕 2026-10-23（D · **F4**）**卡上特效跟着这张卡排队**
        // ==================================================================
        //  判据（原版）：
        //   · 场上可视体 = `3DBody` 的 **SortingGroup**（layer `Default` / order 0）；
        //     卡上特效挂在 `CardElements/Board Elements/EffectAnchor`（**无 SortingGroup**）
        //     ⇒ 用 prefab 默认（`Default` / 0）⇒ **两者同 key、由距离排** —— 也就是说
        //     原版里「这张卡的特效」**和这张卡一起被排序**，不会跑到别的卡那边去。
        //     出处：`普查产出_1023/D_战斗层级审查.md` §②·B「谁压谁（场上卡 vs 自己的特效）」。
        //   · 反编译侧还有一条旁证：`DepthSorting.SetSortingOrder` 把 `sortOrder + n` **批量**
        //     落到 sprites/particles/meshes 三张表（`d:/2/tools/decomp_full/DepthSorting__SetSortingOrder.c`）；
        //     而 `2DCard/Front/EffectAnchor` 带 `SortingGroup order=1`（用途未查清，见 D 报告 §④·3b）。
        //     ⇒ 原版确有「把一组 renderer 抬到卡 canvas 之上」的口子。
        //  🔴 **我们的模型与上面不同，所以映射要写清楚**（D 报告 §②·0）：我们是
        //     「**一张卡 = 一段 renderQueue**」（场上 2962–2979 / 手牌 2980–3000），
        //     而 `renderQueue` 在**两个世界 `MeshRenderer` 之间**先于 sortingLayer / sortingOrder
        //     （⚠️ **2026-10-24 就地补注（铁律 5）**：本条原文只写了「`renderQueue` 优先级**高于**
        //      sortingLayer / sortingOrder」—— **对画布件不成立**：`Canvas` 的排序层先比、队列轮不到。
        //      实测 = 换牌期队列 3001 的换牌卡仍被 `OverHand` 画布上的压暗层盖住（换牌卡区 max = 123
        //      ≈ 线性 0.198 = 1 − 0.8）；详见 `Battle/HudCanvas.cs:21` 与
        //      `Battle/MulliganPanel.QMulliganShade` 的 doc。下面这条「特效停在 3000 ⇒ 恒压在场上卡之上」
        //      说的是 **mesh 之间**，不受影响）⇒
        //     一件没被盖章的透明件（所有特效）停在 3000 ⇒ **恒画在所有场上卡之上**。
        //  ⇒ 本条的落地 = **让卡上的特效进这张卡自己的那一段**：
        //     `renderQueue = 这张卡的 Queue`，`sortingOrder = EffectSortingOrder + 原值`。
        //     · ⛔ **不要写成「卡队列号 + 1」**：我们的号段是**连续**的
        //       （`BoardLayout.QBoardTop 2979` → `QBoardFloor 2962`，相邻两格正好差 1）
        //       ⇒ +1 会落进**邻格**那一段（`BoardLayout.RankInRow` 保证名次连续）。
        //     · 为什么还要抬 `sortingOrder`：同一个队列里 `sortingOrder` **先于**距离比较，
        //       而卡面各层的层号是 1…60（`SortingOrderFor`）⇒ 基准取 **70** 才在它们之上。
        //       原版那里「卡与特效同 key、靠距离」，我们是「同段 + 显式号」——
        //       方向一致（特效在卡前），机制不同（已如实记在报告里）。
        //  ⚠️ **触发口** = `CardEffects.Fire`（那才是「谁生了这个特效、挂给哪张卡」的唯一入口）：
        //     它在本类之外（静态类），所以这里只提供 `StampEffectRoot`，由 `CardEffects` 调。
        //  ⚠️ 残骸体（`_remnantBody`）**不走这条路** —— 它是 `CardView` 自己
        //     `WarpforgeEffectPlayer.Play` 出来的，从不经过 `CardEffects`；而且
        //     `SetQueue`/`SubtreeQueues` 本来就显式跳过它（那一族 prefab 自带
        //     `m_SortingOrder` 0/1/155 在排内部层序，硬盖章会把层序压平）。

        /// <summary>卡上特效在本卡队列段内的**排序基准号**（判据见上面那段）。
        /// 必须 &gt; 卡面最高层号（`SortingOrderFor` 的上限 60，数值层），且远小于 `BoardLayout.MarkerQ`
        /// （3100）与 `CardWinBox.QEffect`（3020）—— 那两个是**卡之外**的东西。</summary>
        public const int EffectSortingOrder = 70;

        /// <summary>挂在**本卡**身上的特效实例根（`CardEffects.Fire` 登记）。
        /// `SetQueue` 每次换队列都会把它们一起重盖 —— 这就是「特效随卡重排」。</summary>
        readonly List<Transform> _effectRoots = new List<Transform>();

        /// <summary>每颗特效 renderer **第一次**被盖章时的 `sortingOrder`（保留 prefab 内部的相对层序；
        /// 反复盖章永远以它为准，避免「70 再加一次」的棘轮）。</summary>
        readonly Dictionary<Renderer, int> _effectBaseOrder = new Dictionary<Renderer, int>();

        /// <summary>把一个**特效实例根**挂到这张卡的排序段上（幂等）。
        /// <para>调用点只有一个 = <see cref="CardEffects.Fire"/>（判据见上面那段）。</para>
        /// <para>⚠️ 传进来的必须是**特效根**（`CardEffects` 刚建出来那个子节点）——
        /// 别把锚点或卡自己的层传进来：那会把卡面层号一并改成 70+。</para></summary>
        public void StampEffectRoot(Transform fxRoot)
        {
            if (fxRoot == null) return;
            // 残骸体不走这条路（上面那段注释）；真传进来也不盖，免得把它的内部层序压平。
            if (_remnantBody != null && fxRoot.IsChildOf(_remnantBody.transform)) return;
            if (!_effectRoots.Contains(fxRoot)) _effectRoots.Add(fxRoot);
            StampEffectRootNow(fxRoot);
        }

        void StampEffectRootNow(Transform fxRoot)
        {
            if (fxRoot == null) return;
            foreach (var r in fxRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.sharedMaterial == null) continue;
                int b;
                if (!_effectBaseOrder.TryGetValue(r, out b))
                {
                    b = r.sortingOrder;          // 第一次：记下 prefab 自己的号（相对序的基准）
                    _effectBaseOrder[r] = b;
                }
                r.sortingOrder = EffectSortingOrder + b;
                StampQueue(r);                    // renderQueue = 这张卡那一段（唯一实现，⛔ 别另写）
            }
        }

        /// <summary>这张卡身上现在登记着几个特效根（自检口）。</summary>
        public int EffectRootCount { get { return _effectRoots.Count; } }

        /// <summary>按一个**特效根**的 `sortingOrder` 读回它被盖成的值（自检口；判据见上）。</summary>
        public int EffectSortingOrderOf(Transform fxRoot)
        {
            if (fxRoot == null) return -1;
            foreach (var r in fxRoot.GetComponentsInChildren<Renderer>(true))
                if (r != null && r.sharedMaterial != null) return r.sortingOrder;
            return -1;
        }

        /// <summary>某个 `Transform` 归属哪张卡（沿父链找最近的一个）。`CardEffects` 用它决定「这个特效挂给谁」。
        /// <para>⚠️ 找不到就返回 null（世界特效 / 棋盘特效）—— ⛔ 别退回「找场景里任意一张卡」。</para></summary>
        public static CardView OwnerOf(Transform t)
        {
            return t != null ? t.GetComponentInParent<CardView>() : null;
        }

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

        // ---- 🔴 2026-10-25（`M2`）原版第三级节点 `Front`（`apos.y = +0.08`）----
        //  判据（UnityPy 逐节点读 `m_AnchoredPosition`，全库普查 → `普查产出_1024d/X1_三处判据补查.md` §二）：
        //    战斗卡 `CardPrefab/2DCard/Front` **+0.08**（1 实例）
        //    菜单/HUD/详情窗 `CardUI/2DCard/Front` **+0.08**（152 实例，无例外）
        //  而 `CardImage(−0.044 / −0.065)` / `CardFrame(−0.03)` / 文字族那些 y **全是 `Front` 局部值**。
        //  ⇒ 我们原来把「Front 局部」当作「2DCard 局部」用 ⇒ **整张卡面（框/文字/立绘）比原版低 0.08 卡单位**
        //     （= 2.4% 卡高；详情窗尺度约 20 px）。层与层之间是自洽的，差的只有这一次整体平移。
        //  ⛔ 别把这个 `+0.08` 分别加进各层的 y 常量（几十处、必漏）；也⛔别加在 `_tiltRoot` 上 ——
        //     原版 `2DCard` 自己的 apos 是 (0,0)，而且命中区（`HitFrame()` = `_tiltRoot`）、倾摆枢轴、
        //     手牌特效锚点（`EffectAnchorHand` = `card2DController.transform` = `2DCard`）都锚在它身上。
        /// <summary>原版 `Front` 在 `2DCard` 空间的 `m_AnchoredPosition.y` —— **判据只此一处**
        /// （建节点、`PlaceRim`、`HitAt`/`StatWorld` 三处共用）。</summary>
        public const float FrontAposY = 0.08f;

        Transform _frontRoot;

        /// <summary>**卡面各层的真正父节点** = 原版 `2DCard/Front`（层坐标全是 `Front` 局部系）。
        /// <para>⚠️ 与 <see cref="FaceRoot"/> 的分工：`FaceRoot` = `2DCard`（倾摆 / 命中 / 特效锚点那一级），
        /// 本属性 = 它下面那颗 `Front`（卡面各层）。⛔ 别把两者混用。</para>
        /// <para>还没建（`Build` 之前 / 老调用点）时退回 `FaceRoot`。</para></summary>
        public Transform FaceContentRoot { get { return _frontRoot != null ? _frontRoot : FaceRoot; } }

        // ==================================================================
        //  🆕 2026-10-16 **卡上特效的锚点**（原版 `CardScript.GetEffectAnchor()`）
        // ==================================================================
        //  判据（`d:/2/tools/decomp_full/CardScript__GetEffectAnchor.c`，**全篇 18 行、逐句亲读**）——
        //  🔴 **它不是一个锚点，是【按卡的状态二选一】：**
        //   · **手牌档**（`CardStateOptions ∈ {0,1,4,8,9,10,12,16}`，判据 = `CardScript+0x228`）
        //     = `BattleCardUI+0x30` 那个组件的 transform；`+0x30` 现读 `il2cpp_out/dump.cs` =
        //     **`BasicCardUI.card2DController`** ⇒ **就是 `2DCard` 那一级** = 本类的 `FaceRoot`。
        //   · **场上档（其余状态）** = `BattleCardUI+0x1A0` = **`public Transform effectsAnchor`**。
        //     🔴 **它的真身 = `CardPrefab/Board Elements/EffectAnchor`** —— 判据是**序列化 PPtr 本身**：
        //     实读卡 prefab 里 `BattleCardUI` 那份 MonoBehaviour，`effectsAnchor` 指向的是
        //     **`Board Elements` 下面那颗**，`localPosition = (0, **1.34**, **−0.128**)`
        //     （Prefab 根的原点 = 卡底边，见 `BuildBody3D` 那条注释；该节点挂的是一个 follow/align
        //     约束组件，**没有 SortingGroup**）。
        //     ⚠️ prefab 里还有**另一颗同名的** `2DCard/Front/EffectAnchor`（`localPosition (0,0,0)`、
        //     **带 `SortingGroup order=1`**）—— 它**不被 `effectsAnchor` 引用**（`GetEffectAnchor` 也不返回它）。
        //     🔴 **2026-10-23 就地订正（铁律 5）**：本段原来写「场上档 = `2DCard/Front/EffectAnchor`，
        //     理由是两颗同名节点里只有它带 SortingGroup」—— **那条推断是反的**，错因是
        //     **「同名节点里挑特征最像的那个」这种判法不成立**：字段的 PPtr 才是判据。
        //     当场后果：锚点位置被写成 `(0, +0.08, 0)`（= `Front` 的 apos）而不是 `(0, 1.34, −0.128)`，
        //     Y 高 ≈0.406 卡单位、Z 多 0.128（1080p 下约 27 px）。
        //  ⛔ **别把两个合成一个** —— 手牌档锚在 `2DCard`（含整张卡面），场上档锚在 `Board Elements`（卡的兄弟级）。
        //  ⚠️ 谁在用它：`BattleManager.GetAnimTransform` 的 `self`/`cardTarget`/`abilityTarget` 三档
        //     （判据 → `任务线/战斗线/任务/T05` §2·6 那张逐档表）。

        /// <summary>**场上档锚点在我们卡根坐标系里的局部位** —— **判据只此一处**（`EffectAnchorBoard` 与自检共用）。
        /// <para>原版值 `(0, 1.34, −0.128)` 是 `Board Elements` 空间（= prefab 根空间，原点在**卡底边**）；
        /// 我们卡根的原点在**卡心** ⇒ 只有 `y −= Height/2` 这一步，就是 <see cref="From3DBody"/>。
        /// ⛔ **别在别处再写一遍 `1.34 − Height/2`**（两处写同一条规则 = 迟早不一致）。</para>
        /// <para>判据出处：卡 prefab `CardPrefab/Board Elements/EffectAnchor` 的 `localPosition`
        /// + `BattleCardUI.effectsAnchor` 的序列化 PPtr（2026-10-23 实读）。</para></summary>
        public static Vector3 BoardEffectAnchorLocal { get { return From3DBody(0f, 1.34f, -0.128f); } }

        /// <summary>🔴 **2026-10-24（执行代理 `W24` · `D2_BattleScene11红诊断.md` §3）**：
        /// **残骸体（RemnantBody）挂点的局部位** —— 判据**只此一处**（`CreateRemnantBody` 与自检共用）。
        /// <para>原版 `BattleCardUI__CreateRemnantBody.c:52-75`：`Instantiate(prefab, this.transform.position,
        /// …, **this.transform**)` ⇒ 父 = 卡自身、**局部位置 = (0,0,0)**，以**原版卡根**为原点；
        /// 而原版 `CardPrefab` 根的原点 = **卡底边**（`C6 §2·1`）。我们卡根在**卡心**
        /// ⇒ 等价局部位 = 卡底边 = `From3DBody(0,0,0)` = `(0, −Height/2, 0)`。</para>
        /// <para>⚠️ 与 <see cref="BoardEffectAnchorLocal"/> 同一族（都是「原版 prefab 根空间 → 我们卡根空间」）。
        /// ⛔ 别在别处再写一遍 `−Height/2`；`CardFeel.SpawnRemnantDestroy` 里也⛔ 别再减一次（会重复扣，
        /// 见 `CreateRemnantBody` 那段注释）。</para></summary>
        public static Vector3 RemnantBodyAnchorLocal { get { return From3DBody(0f, 0f, 0f); } }

        Transform _fxAnchorBoard;
        /// <summary>**手牌档**的卡上特效锚点（原版 = `card2DController.transform` = `2DCard` 那一级）＝ `FaceRoot`。</summary>
        public Transform EffectAnchorHand { get { return FaceRoot; } }
        /// <summary>**场上档**的卡上特效锚点（原版 `effectsAnchor` = `CardPrefab/Board Elements/EffectAnchor`，
        /// `localPosition (0, 1.34, −0.128)`）。懒建、建好留着。
        /// <para>⚠️ 它挂在**卡根**（`transform`）下、**不是** `FaceRoot`：原版那颗节点是 `2DCard` 的
        /// **兄弟**（`Board Elements` 与 `2DCard` 同级）⇒ 它不跟 2D 卡面的倾摆走（场上倾摆本来也关着）。</para>
        /// <para>局部位取 <see cref="BoardEffectAnchorLocal"/>（与自检同一份判据）。</para></summary>
        public Transform EffectAnchorBoard
        {
            get
            {
                if (_fxAnchorBoard == null)
                {
                    var go = new GameObject("EffectAnchor");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = BoardEffectAnchorLocal;
                    _fxAnchorBoard = go.transform;
                }
                return _fxAnchorBoard;
            }
        }

        /// <summary>按**卡的状态**取那一个特效锚点（原版 `GetEffectAnchor` 的两支）。
        /// `onBoard = true` ⇒ 场上档（`effectsAnchor`）；否则手牌档（`2DCard`）。
        /// ⛔ 别在调用点自己判「该用哪个」—— **判据只此一处**。</summary>
        public Transform EffectAnchor(bool onBoard)
        {
            return onBoard ? EffectAnchorBoard : EffectAnchorHand;
        }

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
        /// <param name="use">🆕 2026-10-15（`A2110`）：**可选**「建在哪棵树里」那一档（判据 / 两处被它改掉的
        /// 值全在 <see cref="CardUse"/> 的注释里）。默认 `CardUse.Standard` ⇒ **既有 20 个调用点零影响**。
        /// 🔴 **只有战斗放大窗要传 `CardUse.ZoomWindow`**（`Battle/CardDisplayWindow.cs` 那一处）。</param>
        /// <param name="artOverdraw">🆕 2026-10-24（`E5`）：**可选**「这一格是不是前台」那条 overDraw 轴
        /// （原版 `SetImageOverDraw` 的第一条轴；`null` = 不传 = 保持出厂 `true`，**既有 20 个调用点逐位不变**）。
        /// 判据与语义见 <see cref="ArtOverdraw"/>。</param>
        /// <param name="useOverDraw">🆕 2026-10-24（`E5`）：**可选**「这张卡允不允许溢出」那条逐卡轴
        /// （原版 `RawCardScript.useOverDraw`；`null` = 不传 = 保持出厂 `true`）。
        /// 判据与语义见 <see cref="UseOverDraw"/>。
        /// <para>🔑 **为什么要这个形参（H5 §六·2 记的结构性根因）**：两条轴原来只能在 `Create` **之后**赋值
        /// ⇒ 出生那一趟先按「状态 1（Big）」烘三块网格，赋值时再由 setter **重烘一次**。让 `Create` 收它们，
        /// `Build` 就一次到位（省掉重烘），而且「出生状态」与「轴的值」不会再有一帧不一致。
        /// ⚠️ 传了任一条 ⇒ `Create` 末尾补一次 `Show(_artFront, ArtFrontVisible)`（`Build` 自己不查显隐，
        /// 层建出来就是亮的）—— 不补的话「传 `useOverDraw: false`」会画出一条本该藏起来的前景层。
        /// ⛔ 两条都传 `null` 时**一个字节都不多做**（连那句 `Show` 都不调）。</para></param>
        public static CardView Create(Transform parent, CardData d, string name = null,
                                      CardFace face = CardFace.Full, PxRect? clip = null,
                                      CardUse use = CardUse.Standard,
                                      bool? artOverdraw = null, bool? useOverDraw = null)
        {
            var go = new GameObject(name ?? d.title);
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<CardView>();
            v._faceMode = face;
            v._use = use;                     // ⚠️ 必须在 `Build` **之前** —— 卡名字号与阴影色都在 `Build` 里定
            // 🆕 2026-10-18：「大图档」的判定（立绘越出卡框 + ×1.27 只在这一档生效）——
            //   必须在 `Build` 之前定，`Build` 那一趟就按它选立绘网格。判据见 `BigCardOverdraw`。
            v._bigImage = IsBigCardView(parent, go.name, use);
            // 🆕 2026-10-24（`E5`）：两条 overDraw 轴也必须在 `Build` **之前**定 —— `Build` 按 `BigImageState`
            //   选网格，事后再赋会白烘一趟（H5 那版的重烘就是为这个补的）。`null` ⇒ 保持出厂 `true`。
            bool axesGiven = artOverdraw.HasValue || useOverDraw.HasValue;
            if (artOverdraw.HasValue) v._artOverdraw = artOverdraw.Value;
            if (useOverDraw.HasValue) v._useOverDraw = useOverDraw.Value;
            v.Build(d);
            if (axesGiven) Show(v._artFront, v.ArtFrontVisible);
            if (clip.HasValue) v.SetClip(clip);
            return v;
        }

        /// <summary>
        /// 这张卡是不是**大图档**（= 卡片详情窗那一叠扇形里的一格）—— **判据只此一处**。
        ///
        /// 两支成立其一：
        ///  ① `use == CardUse.ZoomWindow` —— 战斗那扇详情窗显式传（`Battle/CardDisplayWindow.cs:1092`）；
        ///  ② 卡名是原版 prefab 的扇形槽名（`CardUI` / `CardUI (1..8)`，判据 = `Core/CardFan.SlotName`）
        ///     **且父链上有一级叫 `Card Display`** —— 菜单那扇详情窗
        ///     （`Shell/CardDetailPopup.cs:766`）不传 `use`（走默认 `Standard`），只能靠这条认；
        ///     两级条件缺一不可：**抽卡包**那扇窗也把自己的卡格命名为 `CardUI`
        ///     （`Shell/BoosterPackOpenWindow.cs:607`），但它挂在 `Booster Animation Parent/Cards` 下、
        ///     **没有 `Card Display` 祖先** ⇒ 不会被误判成大图档。
        ///
        /// ⚠️ 小卡（手牌 `Hand_*` / 卡池 `pool_*` / 收藏 `CollectionCard_*` / 多卡窗 `mcd_card*` /
        ///    卡组格）两条都不满足 ⇒ **零影响**。判不出来一律当小卡（宁可不出效果，也不能让小卡出血）。
        /// </summary>
        static bool IsBigCardView(Transform parent, string name, CardUse use)
        {
            if (use == CardUse.ZoomWindow) return true;
            if (name != "CardUI" && (name == null || !name.StartsWith("CardUI ("))) return false;
            for (var t = parent; t != null; t = t.parent)
                if (t.name == "Card Display") return true;
            return false;
        }

        /// <summary>这张卡的展示场景。</summary>
        public CardFace Face { get { return _faceMode; } }
        CardFace _faceMode = CardFace.Full;

        /// <summary>这张卡建在**哪一棵树**里（原版逐树不同值的那几个节点跟它走）。判据见 <see cref="CardUse"/>。
        /// 只由 `Create(..., use:)` 赋值 —— ⛔ 别做成属性在运行期改（字号/阴影都是**建层时**定的）。</summary>
        public CardUse Use { get { return _use; } }
        CardUse _use = CardUse.Standard;

        /// <summary>🆕 2026-10-18：这张卡是不是**大图档**（= 卡片详情窗那一叠扇形里的一格）。
        /// 只有大图档吃「立绘越出卡框」与「立绘 ×1.27」，小卡（手牌/卡池/收藏/卡组格）**零影响**。
        /// 判定在 <see cref="Create"/> 里一次定死（`Build` 选网格要用），判据 → <see cref="IsBigCardView"/>。</summary>
        public bool BigImage { get { return _bigImage; } }
        bool _bigImage;

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
            // 🔴 2026-10-24（`E24`）：立绘中心 y **按原版预制体分两族**（见 `ArtUnitYFight`）——
            //    `Full`（菜单/HUD 卡 UI，−0.044）⇄ `Hand`/`Board`（战斗卡 `CardPrefab`，−0.065）。
            //    族差是**烘进顶点**的（`ArtExtent` 的 `acy`）⇒ 跨族换形态必须重烘，否则网格停在旧族
            //    （静默：只差 0.021 卡单位 ≈ 263 px 高的手牌上 1.7 px，截图看不出来）。
            bool wasFightFamily = ArtUsesFightFamily;
            _faceMode = f;
            if (wasFightFamily != ArtUsesFightFamily) RebakeArtFamilyMeshes();
            bool board = f == CardFace.Board;

            // 🆕 2026-10-19（`T09` 步骤 B 前半，`W57`）：**换形态 = 换命中档** —— 真 collider 的
            //   `size/offset` 跟着 `HitRect()` 走（in-hand `1.97 × 3.0502 @ y −0.0151` / 其余 `1.97 × 3.02 @ 0`）。
            //   🔴 铁律 10 第 5 条：换形态的入口**只有这一处**（手牌 → 场上），出生那一档在 `Build`
            //   （`EnsureHitCollider`）⇒ **两个入口各显式刷一次**；漏了这里就是「形态切了、collider
            //   还停在 in-hand 档」—— 静默（下沿多 0.0151 的命中高，画面上完全看不出来）。
            SyncHitCollider();

            // 🔴 整卡倾摆跟着形态开关（原版：场上把 `2DCard` 整个 `Toggle(false)` ⇒ 倾摆也停）。
            //    入口**只有这一处**（铁律 10 第 5 条）—— 手牌打出去走的就是这里。
            // 🔴 **2026-10-21（`新账-67`）：停用 ≠ 复位**。`AutoCardRotation` 只在 `Update` 里写
            //    `cacheTransform.localRotation`（= 这个节点自己的 `localRotation`），`enabled=false`
            //    只是**不再更新那一帧之后的值** ⇒ 手牌**正倾着**（拖拽时 ≤±10°，见
            //    `AutoCardRotation.Step`）被打出去时，那份倾摆会**永久留在这个节点上**。
            //    画面上看不出（场上那一套 2D 层在下面全关掉了），但：① `HitFrame()` 的标架**就是**
            //    `_tiltRoot`（`BoxCollider2D` 挂在它身上）⇒ 命中框跟着歪、投影包围盒变大；
            //    ② 将来任何挂在这一级的东西（3D 卡体若改挂这里）都会跟着歪 —— 静默。
            //    ⇒ 换到场上时**显式复位**。（出生入口 `Build` 那个新节点的 `localRotation` 天然是
            //    identity ⇒ 那边不用写；两个入口都符合铁律 10 第 5 条。）
            if (_tilt != null) _tilt.enabled = !board;
            if (board && _tiltRoot != null) _tiltRoot.localRotation = Quaternion.identity;

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
            // 🔴 **2026-10-24（L1 · `C1-B03` 并层）：描边不再是独立 GameObject** —— 它与落地阴影
            //    同层同材质（原版 `2DCard.prefab` 只有 1 颗 `Card Highlight And Shadow`）。
            //    ⇒ 场上「不画描边」**不能再靠藏那一层**（藏了连阴影也没了，那是落地感的来源），
            //      必须显式把 `_Outline` 归零；**并且要连 `_outlineTarget` 一起清**——
            //      否则回手时 `SetOutline(同一颜色)` 会因 `SameColor` 早退 ⇒ **描边永不重亮**
            //      （与 `_canActWanted` 那次是同一族「请求被丢掉」的坑，见 `SetCanAct` 注释）。
            //    铁律 10 第 5 条：换形态入口**只有这一处**（出生那一档默认就是 α0）。
            if (board) ClearOutline();
            // 🆕 2026-10-22（卡牌线 `T04` 第二笔）：这一层比 `!board` 多一个门 —— **大图档里只有前台那张
            //   允许角色压框**（原版 `CardDisplayWindow__ChangeCardPosition.c:75-79`）。判据收在
            //   `ArtFrontVisible` 一处，⛔ 别在这儿再拼一遍 `_bigImage / _artOverdraw`。
            Show(_artFront, ArtFrontVisible);
            Show(_textBg, !board);       // 效果文字底板
            // 🔴 **2026-10-14（`A2006①`）：费用六边形多一个门 —— 督军整格不印。**
            //   判据 = `ShowsCost(d)`（出处见那个函数的注释；⛔ 别在这里另写一份 `cost >= 0`）。
            //   ⚠️ 这里是**换形态入口**，`Build` 是**出生入口**（铁律 10 第 5 条）—— 两个都要走同一个口径，
            //      否则就是「手牌打出去的督军带着费用六边形上场 / 出生在场的督军没有」这种一形态一套值。
            Show(_costBg, !board && ShowsCost(Data));   // 费用六边形
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
            // 🔴 **2026-10-22（`W62/D5①`）**：这里原来只判 `!board`；现在**状态门也要过** ——
            //    判据共用 `CreatedByVisible` 一个口（⛔ 别在这儿再拼一遍「8 / 10 + isPlayer」）。
            //    ⚠️ `_faceMode` 在本函数开头就写过了 ⇒ 这个属性读到的已经是**新**形态。
            Show(_createdBy, CreatedByVisible);
            // 🔴 2026-10-25（`CF-6`）：上面那几句是**按形态**开/关文字层的，而**文字档**（原版 9 态）是
            //   **另一条轴**（手牌 = 简化档时只该显示一件）。⇒ 非场上这一支**最后**再按档位走一次，
            //   否则「SetFace 一把全开」会把简化档的显示规则盖掉（`BuildTextLayers` 自身幂等：
            //   初始态原地重刷、简化态走 5–8 那一支；Board 不建文字层）。
            if (!board && TmpFont.Available) BuildTextLayers(Data);
            // ⚠️ **`_armourIcon` 不关**：护甲是数值，原版场上也显示（`Board Elements` 里有它）
            // 🔴 但**位置要跟着换**（2026-09-20）：2D 卡面与 3D 卡体是两套坐标，盾牌不跟着走
            //    就会和它上面那个数字**分家**（`SpriteQuad` 把位置烘进 mesh ⇒ 换 mesh，不是换 transform）。
            if (_armourIcon != null)
            {
                var amf = _armourIcon.GetComponent<MeshFilter>();
                if (amf != null) amf.sharedMesh = SpriteQuad(board ? "armourIconBoard" : "armourIcon",
                                                             board ? BoardArmourShieldAt : ArmourIconAt,
                                                             board ? BoardArmourIconW : ArmourIconW,
                                                             board ? BoardArmourIconH : ArmourIconH);
                // z 与 y 都要换（y：场上那块网格烘的是 `3DBody` 坐标 ⇒ 要抵消 `Front` 的 +0.08，
                // 见 `PlaceArmourIcon`）；z：场上是 3D 卡体，覆盖层必须落在**卡体正面之前**。
                PlaceArmourIcon();
            }
            // 数值层同理（它是**整卡大小的一张 quad**，场上的数字靠它画）
            PlaceInfoLayer();
            // 徽标的三个层 z 是常量（`BadgePlateZ`/`BadgeIconZ`/`BadgeCounterZ`）——
            // 徽标**只在场上出现**，那些常量本身已经改到卡体正面之前了。
            // 🔴 2026-10-18（W15）：把**这张卡的卡框**传进去 —— 四个数值/费用的落点按它的 uv 反算。
            if (_info != null) _info.sharedMaterial.mainTexture = InfoTexture(Data, board, SplitStats(Data), _frameTex);

            // 🆕 2026-09-29：四个数值层跟着形态一起开关。🔴 **这里的 `skipStats` 与 `Build` 那条
            //   必须是同一个判据**（`SplitStats`）—— 不然手牌→场上那一步会把数字**画两遍**
            //    （`_info` 里烘一份 + 四个独立层再画一份）。
            BuildStatLayers();
            SyncStatLayers();
            // 🆕 2026-10-25（`CF-3`）：换形态入口（铁律 10 第 5 条）—— 四个数的载体**按档换**：
            //   场上 = 四个网格层（`_stats`）、非场上 = 四件 TMP（`_faceStatText`）。
            //   两个方向都要刷：手牌 → 场上要把 TMP 收起来，反之要放出来并重排一次。
            BuildFaceStatTexts();
            SyncFaceStatTexts();
            BuildFaceCostText();      // 🆕 2026-10-25（K4 D3）：费用 = 第五件真 TMP
            SyncFaceCostText();
            // 🆕 2026-10-25（`CF`）：**换形态入口**（铁律 10 第 5 条）—— 攻击类型高亮的倍数按
            //   「卡 prefab 族」取（`AttackScaleModifier` ← `ArtUsesFightFamily`），而族跟着形态走。
            //   今天唯一的迁移是 Hand → Board（同族 ⇒ 倍数不变），仍然在这里显式补一次：
            //   将来加 Full ↔ Hand 路径时不必再想起来这一层（漏了就是「形态切了、数值还停在上一个族」）。
            if (OwnAttackType != AttackTypes.None) HighlightAttackTypes();
            // 🔴 **2026-10-20（`W58`）**：徽标 / 虫群图标 / 治疗飘字**只在场上出现** → 换到非场上形态
            //    必须显式关掉（铁律 10 第 5 条：换形态入口只有这一处）。
            //    ⚠️ `SetFace(Hand)` 原来**不隐藏**那 7 个 `badge*` 层（今天没有 board→hand 路径，
            //    所以一直没暴露；加回手路径时会看到「手牌上挂着词条徽标」）。`SetBadges(null)`
            //    只是把层关掉、**不销毁**（手牌→场上再出牌时仍是懒建好的那几层）。
            if (!board)
            {
                SetBadges(null);
                SetSwarm(false);
                HideHeal();
            }
            // 🆕 2026-10-08（A181）：换形态 = 换掉/开关了一堆层 ⇒ **重裁一次**（只在已裁过时走）
            //    🔴 2026-10-13（A435 辛 · A774）：判据 = `Clipping`（显式矩形**或**跟随节点），理由见 `SetData` 那一处。
            if (Clipping) ApplyClip();
            // 🆕 2026-10-14（`A1863`）：换形态是**第二个入口**（手牌 ↔ 场上），「面朝下」是**按场景分层**
            //    （只有场上那一档才藏数值）⇒ 必须在这儿也重放一次。理由同 `SetData` 末尾那一处。
            ApplyAmbush();
            // 🆕 2026-10-19（`A2139`）：换形态可能**新建层**（3D 卡体/数值/徽标）⇒ 整卡灰补到新层上
            //    （铁律 10 第 5 条：多入口各显式设一次；`_grey = 0` 时零成本）。
            if (_grey > 0f) ApplyGrey();
            // 🔴 2026-10-22（「出牌后绿光整回合不亮」的根因那一半）：**换形态要重放「要不要亮」**。
            //   绿光那层**只有场上形态才建**（见 `ApplyCanAct`），而调用方的请求常常在**还是手牌形态**
            //   的时候就到了（`BattleDriver.SyncBoard` 不是逐帧跑 ⇒ 等它下一次跑时请求已经丢了）。
            //   真机判据：turn 5 `built=0` → turn 7 才 `built=1`；而出牌后那张卡要到登场 clip **0.7**
            //   （`CardFeel.cs:1351-1357`）才走到这里 ⇒ 不在这儿补就是「绿光整回合不亮」。
            //   ⚠️ 排在**最末**：上面那些 `Show(...)` 可能刚把 3D 体建出来，绿光要挂在建好之后。
            //   ⚠️ 出生入口（`Build`，按 `CardFace.Board` 建）那边不用写 —— 调用方每次同步都会调
            //      `SetCanAct` 把请求落下去（两个入口各显式设一次，铁律 10 第 5 条）。
            ApplyCanAct();
            // 🆕 2026-10-22（`B04`）：换形态也会重建/开关 `_art` 那一层（`Build` 走 `AddLayer` 会把
            //   局部位姿设回出厂值）⇒ 立绘的局部 y/缩放要重贴一次（判据 → `SetArtLocalY` 的 doc）。
            ApplyArtTransform();
        }

        /// <summary>**这一档的四个数值要不要拆成独立件（不烘进 `_info`）** —— 判据只有这一处
        /// （`Build` 与 `SetFace` 的 `_info` 重烘都要用它，两处不一致就会把数字画两遍）。
        /// <para>🔴 **2026-10-25（`CF-3`）起两档各有各的独立件**（原版同构）：
        /// · **场上**（`Board`）= 四个独立**网格层**（`BuildStatLayers`，原版 `Board Elements/3DBody/…`）；
        /// · **非场上**（`Hand` / `Full`）= 四件**真 TMP**（`BuildFaceStatTexts`，原版 `2DCard/Front/Card Info/…`）。
        /// 两档各自**取不到素材**时才退回「烘进 `_info`」（那一档仍有数字，只是载体不同）—— ⛔ 别把它变成「没有数字」。</para></summary>
        bool SplitStats(CardData d)
        {
            if (!d.isUnit) return false;
            if (_faceMode == CardFace.Board) return PragatiDigits.Available;
            return TmpFont.Available;
        }

        /// <summary>卡面上要不要画「**费用**」那一格（右上角那块六边形底板 + 圆里那个数）。
        /// <para>🔴 **判据 = 原版 `BasicCardUI.SetRawCardData`**（`d:/2/tools/decomp_full/BasicCardUI__SetRawCardData.c`
        /// —— 2026-10-14 现读全文，内容与文件名相符；`A2006①`）：
        /// `:51` 取 `GameStaticData.IsWarlordEquivalent(card)` → `:56` 先给 `bVar6 = 0` →
        /// `:80-82` **只在「不是督军」时**才把它算成 `(manaCost 的符号位取反)` = **`manaCost >= 0`** →
        /// `:77-78` **无论是不是督军都照样 `SetText`** 把值写进去 → `:84` 拿 `bVar6` 去 `Toggle(costCtrl, …)`。
        /// ⇒ **督军：值照写、整格不显示（与值无关）；非督军：`cost >= 0` 才显示。**
        /// <br/>`IsWarlordEquivalent` 的判据是 `cardType == 10`（`GameStaticData__IsWarlordEquivalent.c:6`），
        /// 而 `CardTypeOptions`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardTypeOptions.cs`）里
        /// `Hero = 10` ⇒ 落到我们这份数据上就是 **`type == "hero"`**。</para>
        /// <para>⚠️ **不能只留 `cost >= 0`**：卡池里 56 张督军 `cost` 恒 `0`（`A1773` 换真值后会变 `2`），
        /// `cost >= 0` **恒真 ⇒ 一颗都藏不住** —— 今天印 `0`、`A1773` 之后印 `2`，**两边都错**
        /// （这正是 `A2006` 说「只改数字 = 把一个错数换成另一个错数」的那件事）。</para>
        /// <para>⚠️ 判据**只有这一处**：`Build`（出生入口）· `SetFace`（手牌 ↔ 场上）· `InfoTexture`（烘数字）
        /// 三个入口都要用它 —— 铁律 10 第 5 条（多入口各显式设一次）**且**口径共用一份。</para>
        /// <para>⚠️ **没填 `type` 的构造点**（`CardData.Simple` / `Placeholder` / 自检夹具）仍走老口径
        /// （只看 `cost >= 0`）—— 与 `SubtypeLine` 对空 `type` 的处理同一个态度：
        /// 判不出来时**不改行为**。</para>
        /// <para>⚠️ **底板（`_costBg` 图层）与烘进 `_info` 的那个**数字**是**两处**，
        /// 两处都必须过这一条 —— 只关一处会留下「一块空六边形」或「一颗飘着的数字」。</para>
        /// <para>🔴 **2026-10-19（用户裁定）：`cost &lt; 0` = 「**无费用**」的数据表达** —— 不是缺值、
        /// 更不能拿 `0` 顶。4 张 `Dark Pact of *`（`Fate/Blood/Excess/Resilience`）是战术卡触发生成的
        /// **契约/附魔模板**，当前 build 的卡 SO 里 `manaCost = -1`（原版「不可直接打出」的哨兵）；
        /// `工具/gen_cards_engine.py` 把它**原样**落进 `cost`。⇒ 本条 `d.cost >= 0` 就是卡面
        /// 「**不画费用格**（底板 + 数字都不建）+ 命中区不给 `StatCost`」的唯一判据。
        /// ⚠️ 引擎侧同一条口径 = `RuleCodes.ErrNotPlayable`（`RuleCore.CanPlayCard` /
        /// `CanPlayTactic` 的 `card.Cost &lt; 0` 守卫）—— `CostOf` 会把负值 clamp 成 0，不挡就会被当成免费。</para></summary>
        public static bool ShowsCost(CardData d) { return d.cost >= 0 && d.type != "hero"; }

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
        ///      同族症状见 `WarpforgeEffectPlayer.Play` 的注释）⇒ 实例化后**必须显式写 `localPosition`**
        ///      （= 卡底边 `From3DBody(0,0,0)`，见下面创建那一跳的 ⚠️）；
        ///      不写就是整具残骸体画在场景外面 / 位置高错，**一声不响**。
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
                // 🔴 **2026-10-22（P5 §⑦ 缺口 1/2 收口）**：撤掉分两档 —— 判据 = 反编译逐行读的：
                //   · **死灵**（`RemnantNecrons__Toggle.c`，`param_2 == 0` 且 `param_3 != 0` 那一支）：
                //       `GameObject.SetActive(go, 1)`                        ← **保持可见**
                //       `if (this+0x70 != 0) ParticleSystem.Stop(this+0x70)` ← 闲置粒子停（`remnantIdleParticleSystem`）
                //       `Animation.Play(this.reanimateAnimationName)`        ← `From Remnant Necrons`
                //     演完由片段里的动画事件 **`AnimationResurrectionEnd`**（**1.8 s**，实读
                //     `WarpforgeVFX/Animations/From Remnant Necrons.anim` 的 `m_Events`）收尾：
                //     `BodyVisibilityToggle(true)` + `Toggle(0, 0, 0)` → `SetActive(false)`
                //     （`RemnantNecrons__AnimationResurrectionEnd.c`）。
                //   · **灵族**（`RemnantAeldari$$Toggle` @ RVA `0x640200`，`withAnimation && !option` 那一支）：
                //     `SetActive(go, 0)` + `BodyVisibilityToggle(true)` —— 它**没有 `From Remnant` 片段**
                //     （字段表里只有 `toRemnantAnimationName`）⇒ **立即撤、不播动画**。
                //
                // ⚠️ `BattleDriver.SyncBoard` **每次刷新都会调一次** `SetRemnantBody(false, …)`
                //    （判据是 `v.RemnantBodyVisible`，而演动画期间残骸体**故意还可见**）⇒ 这一支必须**幂等**。
                if (_remnantHideAt >= 0f) return;

                if (_remnantBody != null && _remnantBody.activeSelf)
                {
                    string fromClip = RemnantFromClipOf(RemnantBodyPrefabName);
                    if (!string.IsNullOrEmpty(fromClip))
                    {
                        StopRemnantIdleParticles();
                        float len = PlayRemnantClip(fromClip);
                        if (len > 0f)
                        {
                            _remnantHideAt = len;          // 到点再藏（原版那条动画事件的时刻）
                            _remnantHideClock = 0f;
                            return;                        // ⛔ 卡身先不恢复：`From Remnant` 演完才恢复
                        }
                        // 播不出来 ⇒ `PlayRemnantClip` 已经出声，落回「立即撤」
                    }
                }
                HideRemnantBodyNow();
                return;
            }

            // 🔴 **2026-10-22（`W64` · B16）**：残骸体现在**逐卡不同**（原版 `RawCardScript.remnantBodyPrefab`，
            //   判据链 → `BattleDriver.RemnantBodyTable`）⇒ 同一张视图**换了一件**时必须**重建**。
            //   ⚠️ 原来这里只判 `_remnantBody == null`：换了 prefab 名会**静默**沿用旧那一具
            //   （过去两件是按阵营分的、一张卡一辈子不会换，所以没现形；接真值表后就现形了）。
            if (_remnantBody != null && RemnantBodyPrefabName != prefabName)
            {
                if (Application.isPlaying) Object.Destroy(_remnantBody); else Object.DestroyImmediate(_remnantBody);
                _remnantBody = null;
                RemnantBodyPrefabName = null;
                _remnantLight = null;      // 旧的 SpriteRenderer 随上一具一起没了 —— 别留悬空缓存
                _remnantIdlePs = null;     // 同上（`Particles Death Idle`）
            }

            // 「出生」= **这一拍残骸体才真的变得可见**（新建 / 从藏着打开 / 正在演撤场又变回残骸）。
            // ⚠️ 判据**不能**是「调了这个方法」：`BattleDriver.SyncBoard` 每次刷新都会对残骸调一次
            //    `SetRemnantBody(true, …)`，那样 `To Remnant` 会每帧重播（原版只在 `Toggle` 那一拍播）。
            bool born = false;
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
                // ⚠️ `Play` 会把根 `localPosition` 设成我们传的那个值（见它的注释）——
                //    那族 prefab 自己停的 `x = 100` 因此被归位。
                //    `scale = 1` 时它**不动** prefab 自己的缩放（下一步我们才自己设）。
                // 🔴 **2026-10-24（执行代理 `W15` · W11 顺手发现 1）就地修正挂载点**：
                //    原版 `BattleCardUI__CreateRemnantBody.c:54-75`（**现读、逐句**）是
                //      `Instantiate(prefab, this.transform.position, <静态 Quaternion>, this.transform)`
                //    ⇒ 父 = **卡的 transform**、世界位置 = **卡 transform 的位置** ⇒ **局部位置 = (0,0,0)**，
                //    以**原版卡根**为原点；而原版 `CardPrefab` 根的原点 = **卡底边**（C6 §2·1）。
                //    我们的卡根在**卡心** ⇒ 等价局部位置 = 卡底边 = `From3DBody(0,0,0)` = `(0, −Height/2, 0)`。
                //    ⚠️ `Play` 的第 3 参是**局部**位置（它内部写 `t.localPosition`）⇒ ⛔ 别传
                //    `transform.TransformPoint(...)` 那个**世界**点（传了会被当成本地值再落一次）。
                //    ⚠️ 改前传的是 `Vector3.zero` ⇒ 残骸体（以及 `CardFeel.SpawnRemnantDestroy` 取的那颗落点）
                //    **比原版高半张卡**（= `Height × 0.5 × 卡的 lossyScale.y`，我方兵 0.36 档 ≈ 0.6 世界单位）。
                //    ⛔ 别在 `CardFeel.SpawnRemnantDestroy` 里再减一次（W11 特意没减，否则会重复扣）。
                var player = WarpforgeVFX.WarpforgeEffectPlayer.Play(
                    entry, transform, RemnantBodyAnchorLocal, 1f, float.PositiveInfinity);
                if (player == null) return;
                _remnantBody = player.gameObject;
                RemnantBodyPrefabName = prefabName;      // ⬅ 自检用它验「用的是哪一件」
                // 缩放照抄原版：**卡身那个节点的 localScale**（见上面注释里的出处）。
                _remnantBody.transform.localScale = _body3D != null
                    ? _body3D.transform.localScale
                    : Vector3.one * Body3DScale;
                // 残骸体**不参与整卡着色**（`_layers` 是「整卡着色」名单：高亮/置灰/淡出都会
                // 把每一层刷成同一个色）。残骸体有自己的材质与光照，进去会被刷坏。
                // ⚠️ 别把它 `_layers.Add` —— 同族的坑见 `CLAUDE.md` 铁律 10 第 4 条。
                born = true;
            }
            else
            {
                if (!_remnantBody.activeSelf) { _remnantBody.SetActive(true); born = true; }
                else if (_remnantHideAt >= 0f) born = true;     // 撤场演到一半又变回残骸 ⇒ 从头演「转成残骸」
            }

            // 只要这一拍是「盖残骸体」，就取消挂起的撤场（否则 `From Remnant` 到点会把刚盖上的藏掉）
            CancelRemnantHide();

            // 关掉原卡卡身（原版 `ToggleBody3D(false)`）。2D 立绘此时一定已经是关的（场上是 3D 形态）
            if (_body3D != null) Show(_body3D, false);
            if (_art != null) Show(_art, false);
            // 🆕 2026-09-29：原版 `CreateRemnantBody` **就在这一步**调 `SetRemnantHighlight` ⇒ 出生时先把
            // 当前那一档的状态色刷上去（不刷的话它会停在内建的 `(0,1,1,0)` 上 = 看着像「永远不亮」）。
            SetRemnantLight(CardHighlight.FrameColorOf(State));

            if (born)
            {
                // ① 原版 `RemnantBody.Initialize` / `RemnantNecrons.Initialize` 那两次
                //    `CardMaterialHelper.ConfigureMaterial(mat, card)` —— 只在**建出来那一拍**做一次。
                ConfigureRemnantMaterials();
                // ② 原版 `Toggle(..., withAnimation: true)` 末尾那句 `Animation.Play(toRemnantAnimationName)`。
                PlayRemnantClip(RemnantToClipOf(prefabName));
            }
        }

        /// <summary>残骸体现在显示着吗（没建过 / 已撤掉都是 false）。自检用它 ——
        /// 比的是「残骸体在不在」，不是我们自己的常量。</summary>
        public bool RemnantBodyVisible { get { return _remnantBody != null && _remnantBody.activeSelf; } }

        /// <summary>残骸体根节点（没建过是 null）—— 自检量它的**渲染真值**（位置 / 缩放 / 材质）用。</summary>
        public GameObject RemnantBodyRoot { get { return _remnantBody; } }

        // ==================================================================
        //  残骸体的动画 / 闲置粒子 / 卡面材质
        //  🆕 2026-10-22（P5 §⑦ 两条缺口的等价接线）—— 判据全是**反编译方法体**逐行读的
        // ==================================================================

        /// <summary>灵族那具的「转成残骸」片段名（= prefab 上 `toRemnantAnimationName` 的实读字符串）。</summary>
        public const string RemnantToAeldariClip = "To Remnant Aeldari";
        /// <summary>死灵那具的「转成残骸」片段名（`RemnantNecrons.toRemnantAnimationName`）。</summary>
        public const string RemnantToNecronsClip = "To Remnant Necrons";
        /// <summary>死灵那具的「从残骸变回来」片段名（`RemnantNecrons.reanimateAnimationName`）。
        /// ⚠️ **灵族没有这一条**（`RemnantAeldari` 的字段表里只有 `toRemnantAnimationName`）⇒ 灵族撤场**不播**。</summary>
        public const string RemnantFromNecronsClip = "From Remnant Necrons";

        /// <summary>`RemnantNecrons.remnantIdleParticleSystem` 落到的节点名（P5 实读：GO `Particles Death Idle`）。</summary>
        public const string RemnantIdleParticleNode = "Particles Death Idle";
        /// <summary>`RemnantNecrons.remnant`（P5 实读：GO `Card Remnant`）—— 原版 `Initialize` 里配材质的那一颗。</summary>
        public const string RemnantNecronsRemnantNode = "Card Remnant";
        /// <summary>`RemnantNecrons.card3D`（P5 实读：GO `Card 3D`）—— 原版 `RemnantNecrons.Initialize` 额外配的那一颗。</summary>
        public const string RemnantNecronsCard3DNode = "Card 3D";
        /// <summary>`RemnantBody.remnant`（灵族那件：GO `To remnant`）。</summary>
        public const string RemnantAeldariRemnantNode = "To remnant";
        /// <summary>`CardMaterialHelper.ConfigureMaterial` 写的两个槽（原版属性表实读：`Necrons Base Death` 与灵族那件都有）。</summary>
        public const string RemnantCardImageProp = "_CardImage";
        /// <inheritdoc cref="RemnantCardImageProp"/>
        public const string RemnantMatCapProp = "_MatCap";

        /// <summary>🆕 2026-10-25（B2）：`Card3D.shader` 上那个**立绘 UV 窗**属性
        /// （`float4`，xy = min / zw = max）。写入点 = <see cref="ApplyArtUvRect"/>，
        /// 唯一算法 = <see cref="CardArt.ArtUvRect"/>（判据在那里）。</summary>
        public const string ArtUvRectProp = "_ArtUvRect";

        /// <summary>这具残骸体「转成残骸」该播哪条片段（未知 prefab ⇒ `null`，`PlayRemnantClip` 会出声）。</summary>
        static string RemnantToClipOf(string prefabName)
        {
            if (prefabName == "RemnantBody3D Aeldari") return RemnantToAeldariClip;
            if (prefabName == "RemnantBody3D Necrons") return RemnantToNecronsClip;
            return null;
        }

        /// <summary>「从残骸变回来」该播哪条片段。**只有死灵有**（灵族那件 prefab 里就没有 `From Remnant` 片段）
        /// ⇒ 返回 `null` 的调用方必须走「立即撤」，⛔ 不是「播一条别的」。</summary>
        static string RemnantFromClipOf(string prefabName)
        {
            if (prefabName == "RemnantBody3D Necrons") return RemnantFromNecronsClip;
            return null;
        }

        /// <summary>自检口：`Animation.Play(...)` **真的被调过几次**。
        /// 🔴 edit mode（批处理自检）里 `Animation.isPlaying` / 采样都不可靠 —— 与
        /// `ParticleEmitOnEnable.PlayCalls` 同一条理由：调用计数是唯一与引擎状态无关的观察点
        /// （片段没找到 / `Animation` 组件没挂 ⇒ 计数恒 0 ⇒ 断言照红，⛔ 不是放宽）。</summary>
        public int RemnantAnimationPlayCalls { get; private set; }

        /// <summary>自检口：最近一次真的调出去的片段名（`null` = 还没播过）。</summary>
        public string RemnantLastAnimation { get; private set; }

        /// <summary>自检口：撤场时 `ParticleSystem.Stop()` **真的被调过几次**。</summary>
        public int RemnantIdleStopCalls { get; private set; }

        /// <summary>自检口：残骸体内部渲染器被**写上卡面材质**的次数（`_CardImage` / `_MatCap` 至少写进一个算一次）。</summary>
        public int RemnantMaterialWrites { get; private set; }

        /// <summary>正在演「从残骸变回来」那段动画（演完之前残骸体**保持可见** —— 原版就是这么做的）。</summary>
        public bool RemnantHiding { get { return _remnantHideAt >= 0f; } }

        /// <summary>**手动推**撤场计时（批处理没有帧循环 ⇒ 自检用它；产品路径走 `Update`）。</summary>
        public void TickRemnantHide(float dt)
        {
            if (_remnantHideAt < 0f) return;
            if (dt > 0f) _remnantHideClock += dt;
            if (_remnantHideClock + 1e-4f < _remnantHideAt) return;
            HideRemnantBodyNow();       // 到点：藏掉残骸体 + 恢复原卡卡身（原版 `AnimationResurrectionEnd` 那三句）
        }

        void Update()
        {
            if (_remnantHideAt >= 0f) TickRemnantHide(Time.deltaTime);
            // 🆕 X17（2026-10-25）：激活那一刻没量到（TMP 还没 `Awake`、或版面还停在半成品）⇒
            //   再试几帧。真 Play 有帧循环 ⇒ 一定会收敛；**批处理没有帧循环** ⇒ 这一段跑不到，
            //   自检里就按「量不出来」直接红（⛔ 不假绿 —— 见 `RefitTextForTest`）。
            if (_textNeedsRefit && _refitTries < 3) { _refitTries++; RefitTextLayers(); }
        }

        /// <summary>立刻藏掉残骸体并恢复原卡卡身（原版 `Toggle(0, 0, 0)` 那一支 / 动画事件到点后的收尾）。
        /// <para>原卡卡身**两条路都要管**：有 3D 卡体（正常情形）⇒ 按形态开关它（`SetFace` 里那条同一口径）；
        /// **没有 3D 卡体**（网格/shader 取不到，`BuildBody3D` 已报警告的那条退回路径）⇒ 卡身就是 **2D 立绘层**
        /// —— `SetRemnantBody(true, …)` 把它关了，这里不恢复的话，那一格会**只剩残骸体、撤掉之后变成空白**（静默）。</para>
        void HideRemnantBodyNow()
        {
            _remnantHideAt = -1f;
            _remnantHideClock = 0f;
            if (_remnantBody != null) _remnantBody.SetActive(false);
            if (_body3D != null) Show(_body3D, _faceMode == CardFace.Board);
            else if (_faceMode == CardFace.Board && _art != null) Show(_art, true);
        }

        /// <summary>取消挂起的撤场计时（又变回残骸了 —— 别让 `From Remnant` 到点把刚盖上的那具藏掉）。</summary>
        void CancelRemnantHide()
        {
            _remnantHideAt = -1f;
            _remnantHideClock = 0f;
        }

        /// <summary>播残骸体上的一条 `Animation` 片段（原版 `Animation.Play(name)`）。
        /// <para>返回**收尾时刻（秒）**；`&lt;= 0` = 播不出来（已经出声）。</para>
        /// <para>⚠️ 按**名字**找组件、不缓存某个 `Animation`：灵族那具 prefab 上有**两份** `Animation`
        /// （`To Remnant Aeldari` 挂在根上、另一份 `m_PlayAutomatically: 1` 的在内层节点），
        /// 死灵那份把两条片段都挂在根上 —— 只有「谁认这个名字」是稳的。</para></summary>
        float PlayRemnantClip(string clipName)
        {
            if (_remnantBody == null || string.IsNullOrEmpty(clipName)) return -1f;
            Animation anim = null; AnimationClip clip = null;
            foreach (var a in _remnantBody.GetComponentsInChildren<Animation>(true))
            {
                if (a == null) continue;
                var c = a.GetClip(clipName);
                if (c != null) { anim = a; clip = c; break; }
            }
            if (anim == null)
            {
                Debug.LogWarning($"[CardView] 残骸体 `{RemnantBodyPrefabName}` 上找不到含片段 `{clipName}` 的 "
                               + "`Animation` 组件 ⇒ 这一拍**没有动画**（原版 `RemnantNecrons__Toggle.c` 末尾 / "
                               + "`RemnantAeldari$$Toggle` 都调 `Animation.Play(...)`）。片段与 prefab 是同一条导入链"
                               + "产出的 —— 重导过 prefab 就重跑 `工具/extract_missing_shaders.py --prefabs`。");
                return -1f;
            }
            anim.Play(clipName);
            RemnantAnimationPlayCalls++;
            RemnantLastAnimation = clipName;
            return RemnantAnimationEnd(clip);
        }

        /// <summary>这条片段「该收尾」的时刻 —— 优先用**动画事件**（原版就是靠事件收尾的），没有才退回 `clip.length`。
        /// `From Remnant Necrons` 的事件 = `AnimationResurrectionEnd` @ **1.8 s**
        /// （实读 `WarpforgeVFX/Animations/From Remnant Necrons.anim` 的 `m_Events`；收尾动作见
        /// `RemnantNecrons__AnimationResurrectionEnd.c`：`BodyVisibilityToggle(true)` → `Toggle(0,0,0)`）。</summary>
        static float RemnantAnimationEnd(AnimationClip clip)
        {
            if (clip == null) return 0f;
            var evts = clip.events;
            if (evts != null)
                foreach (var e in evts)
                    if (e != null && e.functionName == "AnimationResurrectionEnd") return e.time;
            return clip.length;
        }

        /// <summary>原版 `RemnantNecrons__Toggle.c` 撤场那一支的 `ParticleSystem.Stop(this + 0x70)`。
        /// ⚠️ 原版调的是**无参** `Stop()`（= `withChildren: true`、`StopEmitting`）—— ⛔ 别改成清空式。</summary>
        void StopRemnantIdleParticles()
        {
            var ps = RemnantIdleParticle();
            if (ps == null)
            {
                Debug.LogWarning($"[CardView] 残骸体 `{RemnantBodyPrefabName}` 上找不到 `{RemnantIdleParticleNode}` "
                               + "的 ParticleSystem ⇒ 撤场时不会停闲置粒子（原版 `RemnantNecrons__Toggle.c` 里那句 "
                               + "`ParticleSystem.Stop(this+0x70)`）");
                return;
            }
            ps.Stop();
            RemnantIdleStopCalls++;
        }

        ParticleSystem RemnantIdleParticle()
        {
            if (_remnantIdlePs != null) return _remnantIdlePs;
            if (_remnantBody == null) return null;
            foreach (var ps in _remnantBody.GetComponentsInChildren<ParticleSystem>(true))
                if (ps != null && ps.gameObject.name == RemnantIdleParticleNode) { _remnantIdlePs = ps; break; }
            return _remnantIdlePs;
        }

        /// <summary>原版 `RemnantBody.Initialize` / `RemnantNecrons.Initialize` 对残骸体内部渲染器调的那两次
        /// `CardMaterialHelper.ConfigureMaterial(mat, card)`（两个方法体都逐行读过）。
        ///
        /// <para>它**全部的内容**只有两条 `SetTexture`：
        /// ① `card.cardSprite != null` ⇒ `_CardImage = cardSprite.texture`（取不到 sprite 就跳过）；
        /// ② `_MatCap = matcapTexturesByCardTier[clamp(CardTier)]`（越界夹到末位）。
        /// ⇒ 我们的等价物（同一对槽、同一取数口，与 `CardFeel.SpawnDeathBody` 一字不差）：
        /// `_CardImage = <see cref="BodyArtTexture"/>` ·
        /// `_MatCap = CardArt.Card3DMatcap(CardProgress.Level(artId))`（`0` = tier1）。
        /// **不是近似** —— 原版那两行能写的也就是这两个槽。</para>
        ///
        /// <para>节点：死灵 = `remnant`(`+0x40`) + `card3D`(`+0x68`)；灵族 = 只有 `remnant`。
        /// 两件材质实读都有这两个属性（`Remnant Shatter` = 原版 `Everguild/Cards/Necrons Base Death`
        /// `_CardImage` def=black / `_MatCap` def=white；灵族那件 `Card 3d WH40K Explosion Green` 同表）。</para></summary>
        void ConfigureRemnantMaterials()
        {
            if (_remnantBody == null) return;
            string[] nodes = RemnantMaterialNodesOf(RemnantBodyPrefabName);
            if (nodes == null) return;
            var art = BodyArtTexture;
            int tierLevel = !string.IsNullOrEmpty(Data.artId) ? CardProgress.Level(Data.artId) : 0;
            var matcap = CardArt.Card3DMatcap(tierLevel);
            foreach (var path in nodes) ApplyRemnantCardMaterial(path, art, matcap);
        }

        /// <summary>原版 `Initialize` 里逐个 `Renderer` 调 `ConfigureMaterial` 的那份「哪几颗」。</summary>
        static string[] RemnantMaterialNodesOf(string prefabName)
        {
            if (prefabName == "RemnantBody3D Necrons")
                return new[] { RemnantNecronsRemnantNode, RemnantNecronsCard3DNode };
            if (prefabName == "RemnantBody3D Aeldari")
                return new[] { RemnantAeldariRemnantNode };
            return null;
        }

        /// <summary>把卡面那两个槽写进残骸体内部的一颗 `MeshRenderer`（**复制一份材质**再写，见
        /// `CardFeel.WritableMaterial` 那条 `A1967` 的理由；这里每具残骸体本来就各有一份实例）。</summary>
        void ApplyRemnantCardMaterial(string childPath, Texture2D art, Texture2D matcap)
        {
            var t = _remnantBody != null ? _remnantBody.transform.Find(childPath) : null;
            var mr = t != null ? t.GetComponent<MeshRenderer>() : null;
            if (mr == null)
            {
                Debug.LogWarning($"[CardView] 残骸体 `{RemnantBodyPrefabName}` 里找不到 `{childPath}` 的 MeshRenderer "
                               + "⇒ 原版 `CardMaterialHelper.ConfigureMaterial` 那一层**没接上**（prefab 结构变了？）");
                return;
            }
            var src = mr.sharedMaterial;
            if (src == null)
            {
                Debug.LogWarning($"[CardView] 残骸体 `{RemnantBodyPrefabName}/{childPath}` 没有材质 "
                               + "⇒ 卡面贴图配不上去（`WarpforgeEffectBinder` 没跑？）");
                return;
            }
            // ⚠️ 先问属性、再复制：属性一个都没有就不该留下那份废材质
            bool canImage = art != null && src.HasProperty(RemnantCardImageProp);
            bool canMatcap = matcap != null && src.HasProperty(RemnantMatCapProp);
            if (!canImage && !canMatcap)
            {
                // ⛔ 不静默：属性不在 = 这一层配了也没用（原版那两个 shader 属性表里都有它俩）
                Debug.LogWarning($"[CardView] 残骸体 `{RemnantBodyPrefabName}/{childPath}` 的 shader "
                               + $"`{(src.shader != null ? src.shader.name : "<null>")}` 上既没有 "
                               + $"`{RemnantCardImageProp}` 也没有 `{RemnantMatCapProp}` ⇒ 原版 `ConfigureMaterial` "
                               + "那一层在这颗渲染器上**没有等价物**");
                return;
            }
            var m = new Material(src) { name = src.name + " (remnant card)" };
            if (canImage) m.SetTexture(RemnantCardImageProp, art);
            if (canMatcap) m.SetTexture(RemnantMatCapProp, matcap);
            mr.sharedMaterial = m;
            RemnantMaterialWrites++;
        }

        /// <summary>自检口：残骸体内部某颗渲染器**现在**的贴图槽（真值回读 —— ⛔ 不用「调过几次」自证）。
        /// `null` = 没那个节点 / 没那个属性 / 槽里是空的。</summary>
        public Texture2D RemnantMaterialTexture(string childPath, string propName)
        {
            if (_remnantBody == null || string.IsNullOrEmpty(childPath) || string.IsNullOrEmpty(propName)) return null;
            var t = _remnantBody.transform.Find(childPath);
            var mr = t != null ? t.GetComponent<MeshRenderer>() : null;
            if (mr == null || mr.sharedMaterial == null || !mr.sharedMaterial.HasProperty(propName)) return null;
            return mr.sharedMaterial.GetTexture(propName) as Texture2D;
        }

        /// <summary>🆕 2026-10-22（P6 · M1/M2）：**残骸体里「灵石待机」那颗节点的相对路径**。
        ///
        /// <para>判据（第一权威，逐行读的）= `RemnantAeldari__GetDeathParticlePosition.c`：
        /// 取 `this + 0x68` 那个 Transform 的 `position` 当**阵亡/收集粒子的出生点**；P5 实读那一位
        /// = `wayStoneIdle`，落到 prefab 节点 **`RemnantBody3D Aeldari / Idle / Spirit Stone Idle`**
        /// （localPosition `(0, 1.203, 0)`、scale 0.4431599）。判据全文 →
        /// `资料/普查产出_1022/P5_四未查清.md:139` + `W64_素材导出缺口.md` §四⑦。</para>
        /// <para>⚠️ 只有**灵族**那一具走这颗节点；死灵取的是残骸体**自己的** `transform.position`
        /// （`RemnantNecrons__GetDeathParticlePosition.c`）⇒ 那条路**不要**用这个路径。</para></summary>
        public const string RemnantIdleNodePath = "Idle/Spirit Stone Idle";

        /// <summary>🆕 2026-10-22（P6 · M1/M2）：从**残骸体根**往下找一颗节点（只读口）。
        /// ⛔ 不把 `_remnantBody` 本身交出去 —— 调用方只该「按路径取一颗 Transform」，
        /// 别在外面改残骸体的层级 / 开关（那是 `SetRemnantBody` 的事）。
        /// <para>没建过残骸体 / 路径不存在 / 路径为空 ⇒ `false`（调用方负责出声并走自己的兜底）。</para></summary>
        public bool TryGetRemnantNode(string path, out Transform node)
        {
            node = null;
            if (_remnantBody == null || string.IsNullOrEmpty(path)) return false;
            node = _remnantBody.transform.Find(path);      // ⚠️ `Find` 只吃 `a/b` 形式的相对路径
            return node != null;
        }

        /// <summary>🆕 2026-10-22（`W64` · B16）**这一格用的是哪一件残骸体 prefab**（没建过是 null）。
        /// 自检用它区分「表里那件」与「按关键词猜那件」—— ⛔ 别只比 `RemnantBodyRoot.name`
        /// （那是 `…(Clone)`，而且两件同族名字前缀一样时不好判）。</summary>
        public string RemnantBodyPrefabName { get; private set; }

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
        //    ⚠️ **2026-10-22 更正（铁律 5）**：上面那句「原版是资产里就挂好的，我们运行时建」
        //    **现在只剩一半对** —— 2026-10-22 重导之后，**两个 prefab 的
        //    `RemnantLight → SpriteRenderer.m_Sprite` 已经挂上原版那颗真件了**
        //    （实读 `Assets/WarpforgeVFX/Prefabs/RemnantBody3D Aeldari.prefab` /
        //    `RemnantBody3D Necrons.prefab`：`m_Sprite` guid `d1d0e521e3fe489418b015ad0eb65622`
        //    = `Assets/WarpforgeVFX/Textures/Glow UI W40K_sprite.png`，123×123 @PPU100，
        //    且 `m_WasSpriteAssigned: 1`）。
        //    ⇒ **运行期兜底（`RemnantGlowSprite()`）现在只在 `m_Sprite == null` 时才生效**
        //      （即「prefab 里又没有 / 换了别的 prefab」那种情况），不再是常态路径。
        //    ⇒ 自检那边（`Editor/BattleScene.cs` 那条「残骸体那圈的 sprite 挂上了」）已把判据
        //      从「名字相等」升级成「**存在性 + 123×123 的尺寸**」：真件名是 `Glow UI W40K_sprite`，
        //      `Glow UI W40K` 是「prefab 里没有时自建」那一档，两档都过。
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

        /// <summary>原版那颗 `Glow UI W40K`（123×123 @PPU100 · pivot .5/.5）。见上面「为什么运行时建」。
        /// ⚠️ **2026-10-22 更正**：重导后 prefab 里已有真件 ⇒ 这是**兜底**（只在 `m_Sprite == null` 时用）。</summary>
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
            sp.name = "Glow UI W40K";                    // 自检认这一档（=「prefab 里没有时自建」那档；
                                                         // 真件名是 `Glow UI W40K_sprite`，两档都过）
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
        /// <summary>自检用：残骸体那圈光那颗 sprite 的**像素矩形**（没建 / 没挂是 `default(Rect)` = 0×0）。
        /// 🔴 2026-10-22：判据本体是**尺寸**（原版 `Glow UI W40K` = **123×123**）—— 挂上去的那颗
        /// sprite 有**两个合法来源**：prefab 里的真件（`Glow UI W40K_sprite`，重导后已是常态）
        /// 与运行期自建（`Glow UI W40K`，只在 `m_Sprite == null` 时的兜底）⇒ **名字只能用来分档**，
        /// 「是不是原版那颗」要看尺寸。
        /// ⛔ 别把尺寸塞进 `RemnantLightSpriteName` —— 那个的口径是「名字」，调用方按名分档。</summary>
        public Rect RemnantLightSpriteRect
        {
            get
            {
                var sr = RemnantLightRenderer();
                return (sr != null && sr.sprite != null) ? sr.sprite.rect : default(Rect);
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
        /// <summary>自检用：文字底板用的是哪一张网格（`TextBgQuad_textBg` = Big · `…textBgSmall` = Small ·
        /// 没建 = null）。简化视图那两档（原版 6/5）要靠它验「换档」。</summary>
        public string TextBgMeshName
        {
            get
            {
                if (_textBg == null) return null;
                var mf = _textBg.GetComponent<MeshFilter>();
                return mf != null && mf.sharedMesh != null ? mf.sharedMesh.name : null;
            }
        }
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

        // ==================================================================
        //  🆕 2026-10-18（`A1549` 尾巴）**运行时切档 → 已建好的卡怎么跟上**
        // ==================================================================
        //
        // **问题**：费用格的两档（`Cost Container` / `Cost Container Big`）在**建层那一刻**
        //   就烘进了网格（尺寸 ×`CostScale`、落点含 `CostBigDropUnit`），`_info` 里那颗数字也按
        //   `CostAt01` 烘死了 ⇒ 之后再改 `SmallScreenUI.Enabled`，**已经建出来的卡不会变**。
        // **原版怎么做的**：`CardTextCountersController.Toggle(...)` 每次被调都按当时的
        //   `GameStaticData.smallScreenUI` **重新选容器**（`CardTextCountersController__Toggle.c`），
        //   即「状态变了就重算」。我们这边要补的就是这条刷新链。
        // **缺什么（如实写）**：`SmallScreenUI.Set(bool)` 是**唯一写入点**，但它在
        //   `Shell/TransformScalerBySmallScreenUI.cs`（本写手白名单之外）⇒ 这次**没有**在
        //   设置窗那颗开关后面接线。做到的是：① 每张卡自己能 `RefreshCostTier()`；
        //   ② 卡在 `SetPose` / `SetData` 这两个**必经过**的口上自动比一次档位（所以切完设置之后，
        //      下一次布局/刷新就会跟上）；③ `RefreshAllCostTiers()` 供设置窗（或自检）一次性刷全场。
        //   ✅ **2026-10-19 订正（铁律 5 · `A2139` 顺手核）**：上面那句「⏭ 接线待办」**已经做完了** ——
        //      界面线在 `Shell/SettingsWindow.cs:4500-4512`（`ToggleSmallScreenUI`）里，
        //      `SmallScreenUI.Set(...)` 之后已经补了 `CardView.RefreshAllCostTiers()` 并出声报重做张数。
        // ⚠️ **只重做费用格 + `_info` 里那颗数字**，其它层一概不动（切档只影响这两处；判据见
        //   `CostAt01` / `InfoTexture` 的注释）。`HitAt` / `StatWorld` 是**调用时**才算 `CostAt01`
        //   ⇒ 它们自动是当前档，不需要刷。

        /// <summary>🆕 这张卡的**费用格档位建出来时是哪一档**（`0` 普通 / `1` 大号 / `-1` 没建）。
        /// 自检用；产品侧只看 <see cref="RefreshCostTier"/> 的返回值。</summary>
        public int CostTierBuilt { get { return _costTierBuilt; } }

        /// <summary>🆕 把这张卡的**费用格重做成当前档**（`CostBig` 变了才动）。
        /// <para>返回 `true` = 真的重做了（网格 + `_info` 数字）。返回 `false` = 这张卡没有费用格
        /// （场上形态 / 督军 / 底图取不到）或档位本来就对。</para>
        /// <para>🔴 **不销毁重建图层**：只换 `MeshFilter.sharedMesh`（`SpriteQuad` 按「卡框名 + 档位」
        /// 缓存）⇒ 材质的整卡着色/淡出（`ApplyTint`）原样保留，不会闪一帧。</para></summary>
        public bool RefreshCostTier()
        {
            int want = CostBig ? 1 : 0;
            if (_costTierBuilt < 0 || _costTierBuilt == want) return false;
            if (_costBg != null)
            {
                var mf = _costBg.GetComponent<MeshFilter>();
                if (mf != null)
                    mf.sharedMesh = SpriteQuad((_frameTex != null ? "costBg_" + _frameTex.name : "costBg")
                                               + (CostBig ? "_big" : ""),
                                               CostAt01(_frameTex),
                                               CostBgW * CostScale, CostBgH * CostScale);
            }
            _costTierBuilt = want;
            // 🆕 2026-10-25（K4 D3）：费用数字现在是**真 TMP**（两档 = 两个原版节点）⇒ 换档要**重建**它
            //   （字号/框/autoSizing 三样两档都不同）；建完立刻刷一次值/显隐。
            if (_faceCostContainer != null)
            {
                TmpFont.Kill(_faceCostContainer);
                _faceCostContainer = null; _faceCostText = null; _faceCostRect = null;
            }
            BuildFaceCostText();
            SyncFaceCostText();
            // 数字是烘在 `_info` 那张贴图里的（`InfoTexture` 的缓存键**已经带档位**）⇒ 必须重烘。
            if (_info != null && _info.sharedMaterial != null)
                _info.sharedMaterial.mainTexture =
                    InfoTexture(Data, _faceMode == CardFace.Board, SplitStats(Data), _frameTex);
            ApplyTint();        // 新网格用的还是原来那份材质 ⇒ 其实不必，但保住「换完和别的层同色」这条不变量
            return true;
        }

        /// <summary>🆕 档位变过就刷（`SetPose` / `SetData` 开头调；没变时只是一次 bool 比较）。</summary>
        void SyncCostTier()
        {
            int want = CostBig ? 1 : 0;
            if (_costTierBuilt >= 0 && _costTierBuilt != want) RefreshCostTier();
        }

        /// <summary>🆕 把**场上所有**已建好的卡刷到当前档（给设置窗那颗开关 / 自检用）。
        /// 返回真的重做了几张 —— 0 = 全场没有卡需要改（或一张卡都没有）。</summary>
        public static int RefreshAllCostTiers()
        {
            var all = Object.FindObjectsByType<CardView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int n = 0;
            foreach (var v in all) if (v != null && v.RefreshCostTier()) n++;
            return n;
        }

        /// <summary>🆕 自检用：**不重建**、只算「此刻该用哪一档」。与 `RefreshCostTier` 同一条判据。</summary>
        public static bool CostBigNow { get { return CostBig; } }


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

        /// <summary>那圈高亮描边现在的颜色（自检用；场上恒为透明 —— 场上是 3D 卡体，原版那层 SDF 关着）。
        /// 🔴 2026-10-24（`C1-B03` 并层）：读的是 <see cref="OutlineMat"/> —— 它恒指**软光/影那一层**的
        /// `_Outline`（原版只有一颗 `Card Highlight And Shadow`；原来的 `outlineGlow` 专用层已删）。
        /// 自检 `BattleScene` ⑦ 就是读这个（「打不打得出去」那一条），**别改语义**。</summary>
        public Color OutlineColor
        {
            get
            {
                var m = OutlineMat;
                return (m != null && m.HasProperty("_Outline")) ? m.GetColor("_Outline") : new Color(0f, 0f, 0f, 0f);
            }
        }

        /// <summary>🆕 **2026-10-23（`C3`）自检用：描边的**目标色**（批处理没有帧循环 ⇒ 0.2 s 补间不会自己跑，
        /// 想断「悬停把目标设成了什么」只能读它；断「真落到材质上」用 <see cref="OutlineColor"/> + `CardTween.Advance`）。
        /// 用途 = 收藏窗卡格的**菜单档悬停**（`SetHoverHighlight`）那几条断言。</summary>
        public Color OutlineTargetForTest { get { return _outlineTarget; } }

        /// <summary>🆕 **2026-10-23（`C3`）自检用：描边那层 quad 的**设计尺寸**（卡单位；原版 = 4.4281²）。
        /// 🔴 2026-10-24（`C1-B03` 并层）：描边画在**软光/影那一层**（原版只有一颗
        /// `Card Highlight And Shadow`）⇒ 量 `_shadowLayer` 就是量描边层。
        /// 🔴 **2026-10-24（`P2` · 并层把量法带出来的坑）**：⛔ **不能量 `mr.bounds`**。
        /// 收藏页 / 卡池的卡是**带视口裁切**的（`SetClipFromTree` → `CropLayer` 会把这一层的
        /// `sharedMesh` 换成**按视口裁过的副本**）⇒ `bounds` 量到的是「**视口里还剩多少**」，
        /// 不是原版节点自己的 `m_SizeDelta`（`2DCard.prefab:1527` = `4.4281 × 4.4281`）。
        /// **实测坐实**：第一排那张影子上沿被视口裁掉 **61.7 px = 0.535 卡单位**
        /// （`CollectionScene.cs:5845-5857` 那一段自己就写着「影子本来就压在视口上沿之上 ≈62px」）
        /// ⇒ 高量成 `4.4281 − 0.535 = 3.8923`（那次实得就是 **3.8923**）；横向左沿也被裁 ⇒ 宽 **3.5495**。
        /// **几何本身一个字节都没变**，变的是量法。
        /// ⚠️ 并层**之前**那条断言是绿的，**不是因为那一层没被裁** —— 而是 `outlineGlow` 专用层是
        /// **懒建**的（第一次要亮才建），建的时候 `ApplyClip` 早跑完了 ⇒ 它**从没被裁过**。
        /// 那是它的 **bug**（那圈光会画到视口外），不是它的正确性；并层顺手把这个 bug 也消掉了。
        /// ⇒ 现在量**设计网格**（`_crops` 里记着裁前那份原件）+ 这一层相对卡根的缩放。</summary>
        public Vector2 OutlineQuadSize
        {
            get
            {
                var mr = _shadowLayer;
                if (mr == null) return Vector2.zero;
                // ① **设计网格**：被裁过的层，`sharedMesh` 是裁剪后的副本 ⇒ 回到 `_crops` 里那份原件。
                //    （整层被裁到视口外那一档 `_crops` 只记 `mr`/`wasEnabled`、没有 `original` ⇒
                //      `sharedMesh` 仍是原件，照样对。）
                var mf = mr.GetComponent<MeshFilter>();
                var mesh = mf != null ? mf.sharedMesh : null;
                for (int i = 0; i < _crops.Count; i++)
                    if (_crops[i].mr == mr && _crops[i].original != null) { mesh = _crops[i].original; break; }
                if (mesh == null) return Vector2.zero;
                // ② 网格 bounds 是**层自己的局部系**（卡单位）⇒ 乘「层 → 卡根」的缩放。
                //    用两边的 `lossyScale` 之比，**不用渲染包围盒** ⇒ 转过 90° 也不会被外接盒撑大。
                var bs = mesh.bounds.size;
                var ls = mr.transform.lossyScale;
                var rs = transform.lossyScale;
                float kx = Mathf.Abs(rs.x) > 1e-6f ? Mathf.Abs(ls.x) / Mathf.Abs(rs.x) : Mathf.Abs(ls.x);
                float ky = Mathf.Abs(rs.y) > 1e-6f ? Mathf.Abs(ls.y) / Mathf.Abs(rs.y) : Mathf.Abs(ls.y);
                return new Vector2(bs.x * kx, bs.y * ky);
            }
        }

        /// <summary>🆕 **2026-10-23（`C3`）自检用：描边那张材质某个 float 属性的当前值（读不到 ⇒ **−1**）。</summary>
        public float OutlineFloatForTest(string p)
        {
            var m = OutlineMat;
            return (m != null && m.HasProperty(p)) ? m.GetFloat(p) : -1f;
        }

        /// <summary>描边的诊断串（自检失败时打出来用）</summary>
        public string OutlineDebug
        {
            get
            {
                string tw = _outlineTween == null ? "无补间"
                          : (_outlineTween.IsActive() ? $"补间跑着 {_outlineTween.Elapsed():F3}s" : "补间已停");
                return $"{tw}·目标={_outlineTarget}·现在={OutlineColor}·alpha={_alpha:F2}"
                     + $"·层=SDF软光/影(并层·C1-B03)（软光/影={(_shadowLayer != null)}）";
            }
        }

        /// <summary>换卡面（同一张卡换数据时用，比如手牌换牌）。</summary>
        public void SetData(CardData d)
        {
            // ⚠️ 🆕 2026-10-22（`T04`）：**`UseOverDraw` 不在这里重置** —— `CardData` 不带这一列
            //   （`CardDef` → `CardData` 的唯一转换点 `BattleDriver.ToCardData` 不在本笔的改动范围），
            //   它由**大图档那两个窗口**在 `Create` 之后按卡表项显式设一次（`CardDisplayWindow.BuildFan` /
            //   `CardDetailPopup.BuildCardStack`）。那两处都是「一张卡一次建一个视图」，不复用视图换卡
            //   ⇒ 不存在「换了卡还带着上一张的溢出许可」这条路。⛔ 别在这儿写 `_useOverDraw = true`
            //   图省事：那会把窗口刚设好的逐卡门控**静默抹掉**。
            // 🔴 **必须赶在 `Data = d` 之前** —— 之后就看不到旧值了，而「涨了还是落了」要旧值。
            CaptureStatChanges(d);
            Data = d;
            // 🆕 2026-10-18（`A1549` 尾巴）：档位若被切过，先跟上再刷别的层
            //   （`InfoTexture` 的缓存键带档位 ⇒ 档位变了必须重烘，见 `RefreshCostTier`）。
            SyncCostTier();
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
                // 🔴 2026-10-10 修：原来**漏了第三个实参** ⇒ `skipStats` 落回默认 `false` ⇒ 棋盘单位卡**每次
                //    `SetData`（每刷一次棋盘都走）都把四个数字重新烘进 `_info`**，而烘的落点是【2D 卡面】坐标；
                //    同时四个独立数值层又在**板位**（`BoardMeleeAt…`）再画一遍 ⇒ **数字画两遍**（截图里“散在卡框内”的就是多出来那一份）。
                //    判据就在本文件：`SetFace` 那句 `InfoTexture(Data, board, SplitStats(Data))` 是写对的，
                //    且它下一段注释**逐字**写着这个坑（「必须是同一个判据 —— 不然会把数字画两遍」）。
                _info.sharedMaterial.mainTexture = InfoTexture(d, _faceMode == CardFace.Board, SplitStats(d), _frameTex);
                if (_art != null) _art.sharedMaterial.mainTexture = ArtTexture(d);
                // 🆕 2026-10-18：大图档那层**真 alpha 角色抠图**也要跟着换（异画切风格 / 换卡走的就是这条路）。
                //    漏了它 = 前景层停在**上一张的立绘**上（静默：看起来只是「角色没换」）。
                if (_artFront != null) _artFront.sharedMaterial.mainTexture = ArtTexture(d);
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
            // 🆕 2026-10-25（`CF-3`）：**每刷新一次都要刷**（掉血 / 加 buff / 换卡都走这条路）——
            //   四件 TMP 的值、护甲那一格的显隐、以及「手牌 ⇄ 场上」换载体，全由它一处收口。
            BuildFaceStatTexts();
            SyncFaceStatTexts();
            BuildFaceCostText();      // 🆕 2026-10-25（K4 D3）：费用 = 第五件真 TMP
            SyncFaceCostText();
            // 🆕 2026-10-08（A181）：**重裁一次**（换卡 = 换网格/换文字 ⇒ 上一次那一刀作废）。
            //    ⚠️ 只在**已经裁过一次**时走（`_clipOn`）—— 没设过裁切的调用点（战斗/手牌那一族）一个字节都不动。
            //    🔴 2026-10-13（A435 辛 · A774）：判据是 `Clipping`（显式矩形**或**跟随节点），
            //       ⛔ 不是 `_clip.HasValue` —— 后者在「已开跟随、但这一刻没有节点」时漏掉后面挂上来的节点。
            if (Clipping) ApplyClip();
            // 🆕 2026-10-14（`A1863`）：**面朝下那一档要在每一次刷新之后重放一遍** ——
            //   上面那句 `SyncStatLayers()` 会把四个数值层按数据**重新显示出来**，
            //   而 `_art` 那层也被上面的 `ArtTexture(d)` 刷回了自己的立绘 ⇒ 不在这儿补一次，
            //   面朝下的单位每刷一次棋盘就「翻开」一次（`SyncBoard` 每轮都调 `SetData`）。
            ApplyAmbush();
            // 🆕 2026-10-19（`A2139`）：`SetData` 可能**新建层**（徽标/数值/文字）⇒ 整卡灰补到新层上。
            //    ⚠️ 判 `_grey > 0` 再扫：这一句在「战场每刷新一次」的热路径上，`_grey = 0` 时一个字节都不动。
            if (_grey > 0f) ApplyGrey();
            // 🔴 2026-10-21：每一次刷新都可能**新建层**（徽标/数值/文字）⇒ 场上的层号补一次
            //   （同队列内定序；非场上形态直接 return，零影响）。
            ApplyBoardSortingOrders();
            // 🆕 2026-10-22（`B04`）：`SetData` 可能**重建** `_art` 层（`Build` 里 `AddLayer` 会把
            //   局部位姿设回出厂值）⇒ 立绘的局部 y/缩放要重贴一次。判据 → `SetArtLocalY` 的 doc。
            ApplyArtTransform();
        }

        /// <summary>原版 `CardTextCountersController.DoColorChange` 那一支：**每次 `SetText` 都重算颜色**。
        /// <para>🔴 **2026-10-25（`K4`）判据订正（铁律 5）—— 本段原文两处都不成立**：
        /// 原来写「`DoColorChange(old, new)`；涨/落了的才涂色，其余刷白；只对场上那张卡做」。</para>
        /// <list type="number">
        /// <item><b>比的是【当前 vs 基准】，不是【新 vs 旧】</b>。9 参 `SetText` 的指令流
        ///   （VA `0x18060C180`，2026-10-25 用 `工具/disasm_va.py` 现读）三句就锁死了：
        ///   `mov ebx,r9d`（= `baseValue`）· `mov edi,r8d`（= `currentValue`）·
        ///   `call DoColorChange(edx=currentValue, r8d=baseValue)`。
        ///   ⇒ 颜色是**持续态**：生命 3/5 一直红，被 buff 的攻击一直绿，
        ///   **只有回到基准值**才刷回白（`DoColorChange` 里 `param_2 == param_3` 那一支）。</item>
        /// <item><b>两套载体一起涂、一起 bump</b>。每个数值 counter 的 `textsToSet` 有 **2 件**
        ///   （2D 卡面 `fontSize 0.34` 那件 + 3D 卡体 `fontSize 7/8` 那件，后者带 `highlightAttackType`；
        ///   费用只有 1 件）。实测于 `bundle_battleprefabs_vfxandmisc_assets_all` 的
        ///   `MonoBehaviour_{-3358613892933444672,…}` 五份 —— 明细见 `BumpStat` 的 doc。
        ///   ⇒ 「只对场上那张卡做」是**缺的一半**，手牌的数值涨落原来完全无声。</item>
        /// </list>
        /// <para>⇒ 本节现行：**基准已知**就按原版算持续色（<see cref="StatColorVsBase"/>），
        /// **基准未知**（出厂：全工程还没有 `BattleDriver` 填 `CardData.base*`）才退回 09-29 那套近似。
        /// ⚠️ 视图被**复用换卡**（卡池 / 卡组列表那类）时不算「变了」—— 判据是 `Data.id` 相同。</para>
        /// <para>⚠️ 颜色要等 `SetData` 末了那次 `SyncStatLayers` / `SyncFaceStatTexts` 才画上新值 ——
        /// 这里只记「该涂什么色」。</para></summary>
        void CaptureStatChanges(CardData d)
        {
            ClearStatFlashes();
            // ⚠️ `CardData` 是 **struct**（没有 `== null` 这回事）——「视图里还是上一张卡」的判据是 `id` 不同。
            // 🔴 **持续色（基准已知那一路）与「有没有旧数据」无关**：原版是 `CardScript.UpdateHealthText`
            //    这类调用每次都 `doColorChange = 1` ⇒ **一张建出来就已经受伤的卡，第一次就该是红的**。
            //    只有 **bump** 需要旧值（`doAnimation` 只在 `newAmount != currentAmount` 时播）。
            bool sameCard = !string.IsNullOrEmpty(Data.id) && Data.id == d.id;
            // 费用是原版**第五个** counter（`statIncreaseIsPositiveToThePlayer = 0` ⇒ 越低越好）；
            // **战术卡也有这一格**（它不随 `isUnit` 走）⇒ 放在 isUnit 早退之前。
            // ⚠️ 我们的费用数字**烘在 `_info` 贴图里**（原版是第五件真 TMP `CostText`：`m_fontSize 0.34`、
            //    框 0.456×0.42、Center/Middle、Thin Outline）⇒ 这里只记颜色、**画不上去** ——
            //    结构差异已记账（报告 §②·D3）。
            if (d.statBasesKnown) _statFlash[StatCost] = StatColorVsBase(StatCost, d.cost, d.baseCost);
            if (!d.isUnit) return;
            for (int s = StatMelee; s <= StatHealth; s++)
            {
                int oldV = StatValue(Data, s), newV = StatValue(d, s);
                if (d.statBasesKnown)
                {
                    // 原版口径：**持续色**（current vs base）+ 值变了才 bump（`doAnimation` 那一支）
                    _statFlash[s] = StatColorVsBase(s, newV, StatBaseValue(d, s));
                    if (sameCard && oldV != newV) BumpStat(s);
                }
                else if (sameCard && oldV != newV)
                {
                    FlashStat(s, newV > oldV);            // 基准未知 ⇒ 旧近似（判据见上面那段订正）
                }
            }
        }

        /// <summary>
        /// **这张卡是不是「临时卡（Ephemeral）」的标记** —— 打开它 = 原版那**两件事一起做**（规则书 `:183`/`:229`）。
        ///
        /// 规则书说临时卡「回合结束若在手牌则移除」—— **玩家得看得出哪张是临时的**，否则牌忽然没了就是
        /// 「静默失败」。原版 `CardScript.cs:8046` 的写法是**两行紧挨着**：`ToggleGlitch(true)`（换卡框材质）
        /// + `CardUI.ShowEphemeral()`（在卡的位置实例化状态角标）。**两件我们都有**：
        /// ① 角标 = 原版**关键词图标** `Atlas_trait_icon_ephemeral.png`（取舍与出处见 `EphemeralIconAt` 上面那段）
        /// ② glitch 材质 = `SetGlitch`（自建 `CardPresentation/CardFrameGlitch`，算式照抄原版 DXBC）
        /// 🔴 原版那两行外面还套了一层 `isPlayer` 闸；我们的唯一调用点 `BattleDriver.SyncHand` **只对手牌视图**调
        /// ⇒ 结构上已经是「玩家侧」，不必再传布尔（见类尾 glitch 判据块）；⚠️ 图标没导进来时角标那条**什么都不做**（不画错的），自检有一条断言盯着 `CardArt.Trait("ephemeral")` 非空。
        ///
        /// </summary>
        public void ShowEphemeral(bool on)
        {
            if (_ephemeral != null) _ephemeral.enabled = on;
            SetGlitch(on);      // 原版同一个调用点紧挨着的那一行（`CardScript.cs:8046` 的 `ToggleGlitch(true)`）
        }

        /// <summary>现在画着临时角标吗（自检用）</summary>
        public bool EphemeralShown { get { return _ephemeral != null && _ephemeral.enabled; } }

        // ---- 🆕 2026-10-18（`A985④`）「由谁造出来的」（原版 `CreatedByText`）--------------

        /// <summary>
        /// **这张牌是谁造出来的** —— 传 `null` = 不是被造出来的（初始牌库 / 抽到的牌 / 督军…）。
        ///
        /// 🔴 **2026-10-22（`W62/D5①`）：这一层现在是【有显隐门】的** —— 名字非空**不等于**要印，
        /// 还要看这张牌处在原版哪一档状态。改前「只要有来源就亮」，真机上静置手牌全挂着这行字。
        ///
        /// **原版状态表（逐值照抄，不挑档）** —— `CardStateOptions`（`d:/2/tools/il2cpp_out/dump.cs:45465-45486`，
        /// 18 档全列在这里，⛔ 别再缩成「手牌那两档」）：
        /// ```
        ///   inDeck            = 0     inHand            = 1     inPlay               = 2
        ///   inPlayAttacking   = 3     waitingToBePlayed = 4     waitingToDie         = 5
        ///   inCemetery        = 6     drawing           = 7     inHandShowing        = 8  ← 点亮
        ///   inHandMoving      = 9     inHandPlaying     = 10 ← 点亮（仅对手那张）
        ///   removedFromGame   = 11    inHandJustCreated = 12    inMulliganSelected   = 13
        ///   inMulliganDiscarded = 14  inPlayTransforming = 15   inDeckDrawing        = 16
        ///   inPlayAminingAttack = 17
        /// ```
        /// **点亮判据**（`BattleCardUI__DisplayCreatedByText.c:9-50`，逐行读过）：节点 **先 `SetActive(false)`**，
        /// 之后只在 `createdBy(+0x268) != null` 且：
        ///   · `param_2 == 8`（`inHandShowing`）⇒ 点亮（**两边都亮**，10 那一支的 `isPlayer` 闸走不到）；
        ///   · `param_2 == 10`（`inHandPlaying`）⇒ `*(card + 0x40)`（= `isPlayer`）**非 0 就直接 return**
        ///     ⇒ **只点亮对手那张**；`isPlayer == 0` 才继续去取卡名。
        ///   · 其余 **16 档**（含 `inHand=1` 静置手牌、`inPlay=2` 场上、`inDeck=0` 牌堆、`inCemetery=6`、
        ///     `inMulligan*` 13/14、`drawing` 7 …）⇒ 函数在上面就 `return`，节点保持关着。
        /// **这两档是谁置的**（同一份判据链，⛔ 别只记「手牌」两个字）：
        ///   · 8 = `CardScript__ShowCardInHand.c:38` —— `CardScript__OnTouchEnter.c:30` 在**指针进入一张
        ///     `inHand(1)` 的自己的手牌**时调它（把牌放大展示）；我们这边的等价物 = 手牌悬停
        ///     （`Hand/CardInteraction.UpdateHover` ⇒ `HandLayout.Refresh(hover)` 抬 30 px、放大 1.3）;
        ///   · 10 = `CardScript__MoveIntoFieldFromHand.c:17`（打出飞行段）与
        ///     `BattleManager._ResolveDisplayFromHand_d__548__MoveNext.c:193`（中央展示）。
        ///     我方打出的那张**不点亮**（`isPlayer != 0`），对手那张才点亮 —— 与上面那条 `+0x40` 闸一致。
        /// 文案 = `SupportMethods.GetCreatedByText(创建者卡名)`
        ///   （`SupportMethods__GetCreatedByText.c`：`GetTranslation("Battle/HUD/CreatedBy").Replace("{0}", 名字)`），
        ///   而「创建者卡名」= `RawCardScript.GetLocalizedCardName(creator.rawCard)`
        ///   ⇒ **吃的是创建者那张【卡】**，不是玩家名。
        /// 清掉的时机：`_DisplayOffensiveCardInCenter_d__264` 的 state 3 把那个节点
        ///   `SetActive(false)`（`…MoveNext.c:79-80`）—— 也就是**那一段大图演完就收**。
        ///
        /// ⇒ 本方法**只吃「创建者那张卡」+「当前状态」**：卡名走 `CardText.Name`（= 原版那个
        ///   `GetLocalizedCardName`）。什么时候调、传哪一档由战斗表现层定（`BattleDriver.PushCreatedByStates`
        ///   每轮 + 悬停变化各推一次）。
        ///
        /// ⚠️ **默认值 = `InHandShowing`**：那是**唯一「名字非空就亮」的原版档**，给自检探针与
        ///   非手牌形态的临时视图用；⛔ 产品里的手牌路径必须**显式传档**（`BattleDriver` 就是这么做的）。
        /// </summary>
        /// <param name="state">原版 `CardStateOptions` 里与本层有关的那几档（其余档一律传 `Hidden`）。</param>
        /// <param name="isLocalPlayer">原版 `CardScript +0x40`（`isPlayer`）—— 只对 `InHandPlaying` 有意义：
        /// `true`（我方那张）⇒ **不印**，`false`（对手那张）⇒ 印。</param>
        public void SetCreatedBy(RuleEngine.CardDef creator,
                                 CreatedByState state = CreatedByState.InHandShowing,
                                 bool isLocalPlayer = true)
        {
            // 🔴 **2026-10-17（`N42` 收尾）**：原来传 `(Name, NameZh)` ⇒ 非中英语言档印英文。
            //   改走 `Name(CardDef)`（P1 新增的那个口）—— 它按 `CardDef.Id` 取 13 语言表的 `Card_Name/<id>`。
            string name = creator != null ? CardText.Name(creator) : null;
            // 去重：**名字与状态都要比**（同一张卡每帧都推；但悬停进出只改状态、名字不变）
            if (name == _createdByName && state == _createdByState && isLocalPlayer == _createdByIsLocalPlayer)
                return;
            _createdByName = name;
            _createdByState = state;
            _createdByIsLocalPlayer = isLocalPlayer;
            // 场上形态没有这一层（原版那里整张 `2DCard` 关掉；`BuildTextLayers` 也走不到）
            if (_faceMode != CardFace.Board) FillCreatedBy();
        }

        /// <summary>本层要用的那几档原版状态 —— **值 = `CardStateOptions` 原文**（见 `SetCreatedBy` 那张表）。
        /// ⚠️ 只列我们用到的四档；其余各档（18 档减去 8/10 这两档）一律走 <see cref="Hidden"/>。</summary>
        public enum CreatedByState
        {
            /// <summary>不印：静置手牌 `inHand=1` / 场上 `inPlay=2` / 牌堆 `inDeck=0` / 墓地 `inCemetery=6` /
            /// 换牌 13·14 / 详情窗 …（原版那 16 档的统一落点）。</summary>
            Hidden = -1,
            /// <summary>静置手牌（原版 `inHand = 1`）—— 原版**不印**；单列一档是为了让调用点写得出
            /// 「这是手牌、但静置」，⛔ 别拿它当「亮着」。</summary>
            InHand = 1,
            /// <summary>手牌「展示」档（原版 `inHandShowing = 8`）—— 原版在指针进入手牌、把牌放大展示时置。</summary>
            InHandShowing = 8,
            /// <summary>手牌「打出」档（原版 `inHandPlaying = 10`）—— 打出飞行 / 中央展示。
            /// **只有对手那张**（`isPlayer == 0`）才点亮。</summary>
            InHandPlaying = 10,
        }

        /// <summary>这一档到底印不印（判据 = 上面那张原版状态表；⛔ 别在别处再拼一次）。
        ///
        /// 🔴 **2026-10-24（`G8`）补上原版的【第一道闸】：名字为空 ⇒ 不印**。
        /// 原版 `BattleCardUI__DisplayCreatedByText.c` 是**两道闸**：
        ///   ① `:22` `op_Equality(creator, null)` —— 相等就**整段跳过**（`:16` 那句 `SetActive(false)` 是它之前的事），
        ///      即「`createdBy == null` ⇒ 不印」；
        ///   ② 之后才看 `CardStateOptions`（`:24-31`，展示档 8 / 打出档 10 + `isPlayer`）。
        /// 我们原来**只有 ②** ⇒ 谓词对「名字为空 + 展示/打出档」判成「印」。
        /// ⚠️ **今天看不出画面差异**（`Fill` 收到空文本会把节点整个拆掉、`_createdBy` 变 null，
        ///   ⇒ `CreatedByShown` 两边都是 null）；所以这是**谓词错**、不是现行可见缺陷。
        ///   但谓词错正是下一个会话会照着它写新调用点的东西（本仓第五次踩「一处分叉两处口径」），
        ///   而且 `BattleDriver.cs:21570` 与 `PushCreatedByStates` 今天**真的**以
        ///   「null 名字 + 展示档」的形状调进来（见 `G8` 报告的调用点核查）⇒ 补上，判据只此一处。
        /// ⛔ 档位那半（`InHandShowing` / `InHandPlaying + !isPlayer`）**一个字没动**。</summary>
        bool CreatedByVisible
        {
            get
            {
                if (_faceMode == CardFace.Board) return false;                  // 原版场上（inPlay 那几档）先把节点关掉
                // 原版第一道闸（`:22`）：没有创建者 ⇒ 不印（比档位更早，见上）
                if (string.IsNullOrEmpty(_createdByName)) return false;
                if (_createdByState == CreatedByState.InHandShowing) return true;
                return _createdByState == CreatedByState.InHandPlaying && !_createdByIsLocalPlayer;
            }
        }

        /// <summary>现在这行字印的是什么（`null` = 没印 / 印着但这一档关着）—— 自检用。
        /// ⚠️ 判据取**真的 `activeSelf`**（不是「层建出来没有」）：`SetFace(Board)` 会把这一层关掉
        /// 而对象还在 —— 正是那种「建起来了、该藏的时候没藏」的缺陷要靠这条断言抓。</summary>
        public string CreatedByShown
        {
            get { return _createdBy != null && _createdBy.gameObject.activeSelf ? _createdBy.text : null; }
        }

        /// <summary>自检用：**那两道闸合起来**判成印不印（原版 `DisplayCreatedByText` 的 ① 名字 + ② 档位）。
        ///
        /// 🔴 **2026-10-24（`G8`）为什么必须另开这个口**：`CreatedByShown` **证明不了 ① 那道闸** ——
        ///   名字为空时 `Fill` 会把节点整个拆掉（`_createdBy = null`）⇒ 有没有那道闸，`CreatedByShown` 都是 null
        ///   （拿它写断言 = 恒绿 = 自证）。这个属性读的是**真正驱动 `Show(...)` 的谓词本身**
        ///   （`FillCreatedBy` / `SetFace` 两处都只用它），所以「名字为空 ⇒ false」才是一条真能红的断言。
        /// ⚠️ 只是**读**，不改变任何行为；产品代码 ⛔ 别用它（它就是为了让断言能验那个私有谓词）。</summary>
        public bool CreatedByVisibleForTest { get { return CreatedByVisible; } }

        // ---- 🆕 棋盘单位卡的 buff/debuff 徽标（原版 `boardTraitIcons`）------------------

        /// <summary>角标数字的字号：字形高 ≈ 半个图标（图标 0.456 卡单位 ⇒ 0.16）。</summary>
        static float BadgeCounterFontSize
        {
            // 🔴 **2026-10-20（`W58`）就地订正：原版 `TraitCounter` 的 TMP `m_fontSize = 5.5`。**
            //    但那个字挂在 `TraitIconContainer`（localScale **0.7502**）下，而我们的 TMP 挂在
            //    `FaceRoot`（scale 1）⇒ 换算成我们的字号 = **5.5 × 0.7502 = 4.1261**。
            //    ⚠️ 旧实现是 `FontSizeForGlyphHeight(IconSize × 0.35)`（按**汉字墨高**反推，≈1.68）
            //    —— 只有原版的 **41%**，而且用错了面（这层是 Pragati 拉丁面，em=0.1×fontSize）。
            //    判据 = `GameObject/TraitCounter*.json` 六份 TMP 逐份同值（`m_fontSize 5.5`）+
            //    父链 `Transform` 实读（容器 scale 0.7502）。
            get { return 5.5f * 0.7502f; }
        }

        /// <summary>原版 `TraitCounter` 的**字重与描边** —— 全部在材质/组件字段里，逐值照抄：
        /// · TMP `m_fontStyle = 1`（**Bold**）
        /// · 材质 `Pragati-Regular Atlas Material Extra Thick Outline`
        ///   （`bundle_fonts_assets_all/Material/Material_-7686741775824149781.json`）：
        ///   `_OutlineWidth = 0.106` · `_OutlineColor = (0,0,0,0.7882)` · `_FaceDilate = 0.093`
        /// ⚠️ 用 `fontMaterial`（**实例材质**）—— 改共享材质会让全工程所有卡一起变。</summary>
        static void ApplyBadgeCounterStyle(TextMeshPro t)
        {
            if (t == null) return;
            t.fontStyle = FontStyles.Bold;
            var mat = t.fontMaterial;                 // ⚠️ 实例材质
            if (mat == null) return;
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.7882f));
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.106f);
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.093f);
            // 描边会把字形画到原 quad 之外 ⇒ 重算网格留白，不然描边被裁掉
            t.UpdateMeshPadding();
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
                    SetBadgeCross(i, false);
                    if (_badgeCounters[i] != null) _badgeCounters[i].text = "";
                    continue;
                }
                var b = badges[i];
                SetBadgeIconTexture(i, CardArt.Trait(b.sprite));
                // 🆕 2026-09-29 未激活态：**同一张图换材质**（每回合状态会变，所以每次刷新都要重设）
                SetBadgeGrey(_badgeIcons[i], b.active);
                // 🔴 2026-10-18（`A1676`）：未激活还有**第二条通道** —— 叠一枚 ✕（原版 `Trait Not Active`）。
                //    判据 = `BoardTraitIcon.Initialize` 里**同一个 `traitEnabled`** 同时驱动
                //    「换 disabledMaterial」与「`traitNotActiveIcons[].enabled = !traitEnabled`」⇒ **叠加**。
                SetBadgeCross(i, !b.active);
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

        /// <summary>🆕 自检用（`A1661`）：第 i 个位底板**网格**里全部顶点的 x（**卡单位**，去重后升序）。
        /// 判「是不是原版那种 Sliced」就看它**有几条竖线**：整图一个 quad = **2** 条，
        /// 原版那样切一刀 = **3** 条（左片外缘 / 那条竖切线 / 右片内缘）。
        /// <para>⚠️ 从**网格顶点**读（不是记一个数）—— 记数的话「改法」和「断言」会一起错（自证）。
        /// 这一位没建（缺图 / 没给徽标）返回 `null`。</para></summary>
        public float[] BadgePlateVertexXs(int i)
        {
            if (i < 0 || i >= _badgePlates.Count || _badgePlates[i] == null) return null;
            var mf = _badgePlates[i].GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return null;
            float k = CardUnitW / Width;                 // 世界 → 卡单位（今天 = 1，别写死）
            var xs = new List<float>();
            foreach (var v in mf.sharedMesh.vertices)
            {
                float x = v.x * k;
                bool dup = false;
                for (int j = 0; j < xs.Count; j++)
                    if (Mathf.Abs(xs[j] - x) < 1e-3f) { dup = true; break; }
                if (!dup) xs.Add(x);
            }
            xs.Sort();
            return xs.ToArray();
        }

        /// <summary>🆕 自检用（`A1661`）：第 i 个位底板的材质色（原版 = <see cref="BadgePlateInk"/> 那个深灰）。
        /// ⚠️ 材质不在 / 没有 `_Color` 时返回 `Color.clear`（**不是白**）—— 好让断言能分辨
        /// 「没上色」和「上成别的色」两件事。</summary>
        public Color BadgePlateColor(int i)
        {
            if (i < 0 || i >= _badgePlates.Count || _badgePlates[i] == null) return Color.clear;
            var m = _badgePlates[i].sharedMaterial;
            if (m == null || !m.HasProperty("_Color")) return Color.clear;
            return m.color;
        }

        /// <summary>卡单位坐标 → `SpriteQuad` 要的 0..1（左上原点）。</summary>
        static Vector2 ToAt01(Vector2 cardUnit)
        {
            return new Vector2(cardUnit.x / CardUnitW + 0.5f, 0.5f - cardUnit.y / CardUnitH);
        }

        /// <summary>**图标本体**的位置 = **容器中心**（原版 `TraitIconContainer`）。
        /// <para>🔴 **2026-10-20 就地订正（`W58`）**：这里原来再加一档 `Badges.IconOutward`
        /// （0.287 × 0.750 = 0.21525）—— **那是误读**：容器下 `Container`(∓0.287) 与它自己下一级
        /// `Without counter`/`With counter`(±0.287) **互相抵消** ⇒ 原版图标就在容器中心。
        /// 多偏那一档会把图标外缘画到 −1.006，超出**可见 3D 卡体**半宽 0.9286 有 0.078
        /// （用户报的「词条图标超出卡面」）。判据全文 = `资料/普查产出_1020/W58_战斗线_场上词条判据.md` §二。
        /// 角标文字跟着图标走，所以也用它。</para></summary>
        static Vector2 BadgeIconAt01(int i)
        {
            return ToAt01(Badges.SlotAt(i));
        }

        // ==================================================================
        //  场上那一叠覆盖层的 z（**一整套，必须整体一起看**）
        // ==================================================================
        //
        // 🔴 **2026-09-20 起的原则**：这些层必须落在 **3D 卡体正面之前**
        //    （原来在 −0.02…−0.055 ⇒ 整块被卡体挡住，症状 = 场卡上的数字/徽标**直接看不见**，
        //     而卡体自带的三色彩钮还在）。⚠️ 这几个层**只在场上出现**，手牌那个 2D 卡面不受影响
        //    （2D 那一套是 `InfoZ`/`ArmourIconZ`，见下面两行）。
        //
        // 🔴 **2026-10-12（用户真机报「场上的战将和部队没有近战/远程/血量数值」）—— 两处改动**：
        //
        // ① **把整叠一起往相机方向挪，留出【明确】的间隙。** 原来数字在 **−0.09**，而卡体正面
        //    实测在 **−0.0115…−0.079**（见 `Body3DFrontZ`）⇒ 最薄处只剩 **0.011 卡单位**，
        //    和深度缓冲的量化步长同量级 ⇒ **逐格、逐帧地翻**（这正是「同一张卡有的格有、有的格没有，
        //    而且和卡/阵营/稀有度都无关」那种症状的来源）。
        //    ⚠️ **原版只给了相对关系**（数值贴在卡面上、在徽标**之前**），**绝对值是我们挑的**
        //       —— 铁律 3 / 铁律 10 第 3 条：这里明确写成「我们挑的」，别当成原版值。
        //    新间隙：数字离卡体最靠前的那个面 **≥ 0.07 卡单位**（实测 0.0714，比原来大 6 倍）。
        //
        // ② **数值挪到徽标那一组【之前】**（原来徽标底板 −0.095 比数字 −0.09 **更靠前**）。
        //    右列第 4 行的底板 rect（x 0.236…0.788 · y −0.978…−0.277，见 `Badges.SlotAt`）正好罩住
        //    护甲格 (0.678, −0.894) ⇒ **7 个关键词的单位，护甲数会被底板压掉**。
        //    原版也是「数值在前」：`TraitIcons` 挂在 3DBody **z=−0.014**，而那四个数值文本节点在
        //    **z=−0.063…−0.080**（`资料/3DBody_原版场上卡体规格.md:187-190`）。
        //
        // ⇒ **一整套的次序**（越靠前越负；断这条的是 `Editor/BattleScene.cs` 里那条 z 序断言）：
        //      卡体正面(`Body3DFrontZ` −0.079)  →  徽标底板  →  徽标图标  →  徽标角标
        //      →  护甲盾  →  **四个数值**（最靠前）
        /// <summary>徽标**底板**的 z（场上）。⚠️ 在数值层**之后**（见上面那一整段）。</summary>
        public const float BadgePlateZ = -0.118f;
        /// <summary>徽标**图标**的 z（场上）。</summary>
        public const float BadgeIconZ = -0.124f;
        /// <summary>徽标**角标数字**的 z（场上）。</summary>
        public const float BadgeCounterZ = -0.130f;
        /// <summary>场上的**护甲盾**（数值的背景）—— 在数值**之后**、徽标**之前**。</summary>
        public const float BoardArmourIconZ = -0.140f;
        /// <summary>场上的数值层（`_info` 与四个独立数值格共用这一个 z）。最靠前的一层。</summary>
        public const float BoardInfoZ = -0.150f;
        /// <summary>**3D 卡体正面在卡根空间里最靠前的那一点** —— 场上一叠覆盖层的「底线」。
        /// <para>⚠️ 这个数是**从网格实读算出来的**，不是任何一个序列化字段：把
        /// `bundle_battleprefabs_vfxandmisc_assets_all/Mesh/Card 3D WH40k.obj` 的 833 个面
        /// 按 XY 投影插值，取四个数值格那块矩形（±0.17 卡单位）里**最靠前**的表面，得
        /// **−0.0786**（≈ −0.079）⇒ 四个数值格都坐在**凸起的计数器面板**上。
        /// 卡面中段那片平面在 **−0.0115**；滚圆的底边（mesh |z|>0.5，卡 y ≤ −1.51）够不到数值格，已排除。</para>
        /// <para>数值层与它之间必须留 ≥ 0.05（现在是 0.071）—— 这就是上面 ①。</para></summary>
        public const float Body3DFrontZ = -0.079f;
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
            // ⚠️ **这个点是板的【外缘】**（不是中心）—— 原版 sprite 的 `m_Pivot` 在左边缘，
            //    所以两列都是「从这个点往卡内铺」；居中是错的（见 `BadgePlateQuad`）。
            var plateAt = ToAt01(new Vector2(at.x + (at.x < 0f ? -Badges.PlateOutward : Badges.PlateOutward),
                                             at.y + Badges.PlateDy));

            // 🔴 **2026-10-12（`A1661`）**：原来这里是
            //    `FitQuad("badgePlate"+i, plateAt, PlateW, PlateH, plate)`（**整图按自身比例内接成一个
            //    【居中】quad、且不上色**）—— 三处与原版不符，用户真机上报的「**卡框外的白色斜方块**」就是它：
            //      ① **没上色**：原版乘 `m_Color`（0.3230/0.3431/0.3585 的深灰），我们画的是奶油原色；
            //      ② **居中**：可见板整体往卡外挪 0.172 卡单位（左列可见板左边 −0.9601，卡左边才 −1.0463）；
            //      ③ **整图内接**：可见板只有 0.2064 宽（还把透明边与右下投影一起画了进去），
            //         原版 Sliced 出来是 **0.3312**。
            var plateMr = AddLayer("badgePlate" + i, plate, BadgePlateZ, plate,
                                   BadgePlateQuad("badgePlate" + i, plateAt, at.x < 0f), parent: FaceRoot);
            SetBadgePlateInk(plateMr, _alpha);
            _badgePlates.Add(plateMr);
            // 图标层走**自建灰化 shader**（原版 `disabledMaterial` = `Sprite Greyscale`，见 `TraitDisabled.shader`）：
            // 徽标的「未激活态」**不是换图、也不是缩小**，而是**同一张图换材质**（`BoardTraitIcon.Initialize`）。
            // ⇒ 一个 shader 两态共存，激活不激活只差 `_GreyScale`（0 / 1），重建视图时不用换层。
            // ⚠️ **它和别的层不一样**：见 `AddBadgeIconLayer` 的注释（网格居中，好让脉冲绕自己缩放）。
            _badgeIcons.Add(AddBadgeIconLayer(i, icon, b.active));

            // 🔴 **2026-09-17 更正**：这里原来写「角标数字**用深色**……原版那个计数器用的什么颜色
            //    本地查不到 ⇒ **这一条是我们挑的**」—— **错的**：原版查得到（见 `BadgeCounterInk` 的注释），
            //    是**暖白 + Bold**，已经改回。压在浅色底板上读不出来那件事，原版就是这么印的。
            // 🔴 **2026-10-12（`A1659`）：父节点由 `transform`（卡根）改成 `FaceRoot`（`2DCard`）** ——
            //    理由与 `Fill` 里那段逐字相同（倾摆挂在 `2DCard` 上，挂错父节点 ⇒ 角标数字与徽标底板
            //    在位移中分成两个平面）。徽标底板/图标本来就在 `FaceRoot` 下。
            // 🔴 **2026-10-17（`A1024`③ 批 6）**：角标数字 = 原版 `TraitCounter` 的 `m_fontAsset` = **Pragati**
            //    （普查表 §2·5 卡面那一格）⇒ `LatinCardBody`。
            // ⚠️ **2026-10-25（`M2`）徽标【不挂】`Front`**：`Badges.SlotAt` 的坐标是 **`3DBody` 空间**
            //    （原版 `TraitIcons` 挂在 `3DBody` 下，见 `Core/Badges.cs` 头部），与 `BoardMeleeAt` 一族同源
            //    ⇒ 挂在 `2DCard`（`FaceRoot`），⛔ 别搬进 `Front`（那会让整排徽标上浮 0.08）。
            //    ⚠️ 徽标本来就只在场上出现（那里 2D 卡面是关的），与「卡根空间」一致。
            var t = TmpFont.NewText(FaceRoot, "badgeCounter" + i, "", BadgeCounterFontSize, BadgeCounterInk,
                                    TmpFont.Face.LatinCardBody);
            // 🔴 2026-10-20（`W58`）：原版 `TraitCounter` 是 **Bold + Extra Thick Outline 0.106**
            //    （`m_fontStyle = 1`；材质 `Pragati-Regular Atlas Material Extra Thick Outline`）。
            //    判据与色值见 `ApplyBadgeCounterStyle`。
            ApplyBadgeCounterStyle(t);
            StampTmpQueue(t);   // 🆕 A1665：角标数字也是这张卡的一层
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
            StampQueue(mr);          // 🆕 A1665：徽标也是这张卡的一层
            _layers.Add(mr);
            SetBadgeGrey(mr, active);
            return mr;
        }

        // ── 🆕 2026-10-18（`A1676`）：未激活那枚 ✕──────────────────────────────────────
        //  原版 `BoardTraitIcon.Initialize` 在 `traitEnabled == false` 时**两条通道一起走**：
        //    ① `traitIcons` 换 `disabledMaterial`（= `Sprite Greyscale`，我们已在 `SetBadgeGrey` 里做）
        //    ② `traitNotActiveIcons` 逐个 `enabled = true`  ← **本方法补的是这一条**
        //  判据原文与几何出处 → `Badges.NotActiveScale` 那一段（⛔ 别在这儿另写一份数）。
        //  ⚠️ 贴图固定 `Art/ui/40k_Combat_Icon_Cross`：**所有词条共用同一枚 ✕**（预制体里写死）。
        static bool _crossWarned;

        /// <summary>把第 i 位的 ✕ 打开 / 关掉。`on == true` 时**懒建**那一层（这一位从没未激活过就一个对象都不建）。</summary>
        void SetBadgeCross(int i, bool on)
        {
            if (i < 0 || i >= Badges.MaxSlots) return;
            if (!on && _badgeCrosses.Count <= i) return;          // 没建过、也不用建
            if (_badgeCrosses.Count <= i)
            {
                while (_badgeCrosses.Count < i) _badgeCrosses.Add(null);   // 中间那些位没建过 ⇒ 占位对齐（⛔ 别让 Add 落到错的索引上）
                _badgeCrosses.Add(BuildBadgeCross(i));
            }
            var mr = _badgeCrosses[i];
            if (mr != null) mr.enabled = on;
        }

        /// <summary>建一位的 ✕。取不到图 ⇒ 返回 `null` 并**出声**（不静默：那样未激活态只剩灰化，
        // 而原版是两条通道都有）。</summary>
        MeshRenderer BuildBadgeCross(int i)
        {
            var tex = CardArt.Ui("40k_Combat_Icon_Cross");
            if (tex == null)
            {
                if (!_crossWarned)
                {
                    _crossWarned = true;
                    Debug.LogWarning("[CardView] `Art/ui/40k_Combat_Icon_Cross` 取不到 ⇒ 未激活的徽标**只灰化、不画 ✕**"
                                   + "（原版 `BoardTraitIcon` 是两条通道：灰化 + `traitNotActiveIcons[].enabled`）。");
                }
                return null;
            }
            var at = BadgeIconAt01(i);
            var go = new GameObject("badgeCross" + i);
            go.transform.SetParent(FaceRoot, false);          // 徽标是卡面的一层 ⇒ 跟着倾摆
            go.transform.localPosition = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height,
                                                     BadgeIconZ - Badges.NotActiveCrossZOffset);   // z 更负 = 更靠前
            go.AddComponent<MeshFilter>().sharedMesh =
                CenteredQuad("badgeCross", Badges.NotActiveCrossSize, Badges.NotActiveCrossSize, tex);
            var mr = go.AddComponent<MeshRenderer>();
            // ⚠️ 用**普通**材质（不是 `TraitDisabledMaterial`）：这枚 ✕ 自己**不跟着灰化** ——
            //    原版那是个独立 SpriteRenderer，`Initialize` 只改 `traitIcons` 的材质。
            var m = new Material(BaseMaterial());
            m.mainTexture = tex;
            mr.sharedMaterial = m;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            StampQueue(mr);          // A1665：徽标也是这张卡的一层
            _layers.Add(mr);
            mr.enabled = false;
            return mr;
        }

        /// <summary>自检用：第 i 位的 ✕ 现在画不画（原版 `traitNotActiveIcons[i].enabled`）。
        /// `false` 也可能是「这一位从没未激活过、压根没建」—— 那与「画着但关了」在视觉上等价。</summary>
        public bool BadgeCrossShown(int i)
        {
            return i >= 0 && i < _badgeCrosses.Count && _badgeCrosses[i] != null && _badgeCrosses[i].enabled;
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
        /// ⚠️ **徽标底板不走这条**（原版那层是 **Sliced**，⛔ 不是「整图按自身比例内接」）—— 见 `BadgePlateQuad`。
        /// </summary>
        static Mesh FitQuad(string name, Vector2 at01, float w, float h, Texture2D tex)
        {
            float a = (tex != null && tex.height > 0) ? (float)tex.width / tex.height : w / h;
            float bw = w, bh = h;
            if (a < w / h) bw = h * a; else bh = w / a;
            return SpriteQuad($"{name}_{bw:F3}x{bh:F3}", at01, bw, bh);
        }

        // ── 徽标底板（原版 `IconBackground`：一个 **Sliced** 的 `SpriteRenderer`）────────────────
        // 原版出处（`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/IconBackground*.json`
        // 7 份逐份实读、同值；判据全文 = `资料/普查产出_第二十二会话/DIAG_徽标底板图.md` §二）：
        //   · sprite = `Base3d Trait Background`（PathID 2729514481590049735）—— **本地有**，我们画的就是它
        //     （`Art/ui/Base3d_Trait_Background.png` 与原版**逐字节相同**）
        //     `m_Rect = 105×214` px · `m_PixelsToUnits = 200` · **`m_Pivot = (0, 0.5)`（左边缘！）**
        //     `m_Border = (0, 0, 96, 0)` —— 左/下/上都是 0 ⇒ **只有一条竖切线**
        //   · renderer：**`m_DrawMode = 1 (Sliced)`** · `m_Size = (0.85, 0.874)` ·
        //     `m_Color = (0.32298, 0.34305, 0.35849)`（**深灰**，见 `BadgePlateInk`）· `m_SortingOrder = 0`
        //   · `localScale = (±0.8656, 1.0692)`（**右列 x 为负 = 镜像**）× 容器 0.7502 = `Badges.PlateW/H`
        /// <summary>原版底板 sprite 的宽度（px，`m_Rect.x`）</summary>
        public const float PlateSpriteW = 105f;
        /// <summary>原版底板 sprite 的**右**边界（px，`m_Border.z`）；左/下/上都是 0。</summary>
        public const float PlateSpriteRightBorder = 96f;
        /// <summary>贴图里**不透明板区**的右端（px）：x `0..62` 是不透明的板，`63..94` 是右边的柔和投影。
        /// 出处：`Art/ui/Base3d_Trait_Background.png`（105×214）**逐像素实测**（alpha ≥ 128 的列范围）。</summary>
        public const float PlateSpriteInkRight = 63f;
        /// <summary>九宫格那条竖切线的位置（占整块 quad 宽的比例）= `(105−96)/105` = **9/105**。</summary>
        public static float PlateSliceFrac { get { return (PlateSpriteW - PlateSpriteRightBorder) / PlateSpriteW; } }
        /// <summary>**可见**（不透明）板区占整块 quad 宽的比例 = `63/105` = **0.6**
        /// ⇒ 可见板宽 = `Badges.PlateW × 它` = **0.3312** 卡单位（我们原来「整图内接」只有 0.2064 = 0.62 倍）。</summary>
        public static float PlateInkFrac { get { return PlateSpriteInkRight / PlateSpriteW; } }

        static readonly Dictionary<string, Mesh> _badgePlateQuads = new Dictionary<string, Mesh>();

        /// <summary>徽标底板的 **Sliced 九宫格**网格（原版 `m_DrawMode = 1`）。
        /// 原版左/下/上边界都是 0 ⇒ 只有**两片**：左 `u∈[0, 9/105]`、右 `u∈[9/105, 1]`。
        /// <para>🔴 **锚点 = `at01` 的横向【外缘】**：原版 sprite 的 `m_Pivot = (0, 0.5)` 在**左边缘**，
        /// 右列靠 `localScale.x = −0.8656` 镜像 ⇒ 两列都**从外缘往卡内铺**。
        /// ⛔ **不是居中** —— 居中会让整块板往卡外伸半个板宽（`A1661`：用户 2026-10-12 在真机上看到的
        /// 「卡框外的白色斜方块」，就是「居中 + 整图内接 + 不上色」这三件事一起造成的）。</para>
        /// <para>`leftColumn`：位子在**左列**（板往 **+x** 铺、x 是负的）传 `true`；右列（往 **−x**）传 `false`。
        /// ⚠️ 名字里带位子（`badgePlate0..6`）—— 网格烘的是**绝对**坐标（和 `SpriteQuad` 同一套做法）。</para></summary>
        static Mesh BadgePlateQuad(string name, Vector2 at01, bool leftColumn)
        {
            Mesh m;
            if (_badgePlateQuads.TryGetValue(name, out m) && m != null) return m;

            float s = Width / CardUnitW;                            // 卡单位 → 世界
            float w = Badges.PlateW * s, h = Badges.PlateH * s;
            float ax = (at01.x - 0.5f) * Width;                     // 锚点 = 板的**外缘**
            float cy = (0.5f - at01.y) * Height;                    // 纵向仍是**居中**（原版 pivot y = 0.5）
            float y0 = cy - h * 0.5f, y1 = cy + h * 0.5f;
            int dir = leftColumn ? 1 : -1;
            float cut = ax + dir * w * PlateSliceFrac;              // 那条竖切线

            var verts = new Vector3[8];
            var uvs = new Vector2[8];
            var tris = new int[12];
            AddSlice(0, ax, cut, 0f, PlateSliceFrac, 0);            // 左片（贴图 x 0..9）
            AddSlice(4, cut, ax + dir * w, PlateSliceFrac, 1f, 6);  // 右片（贴图 x 9..105）

            void AddSlice(int o, float xa, float xb, float ua, float ub, int t)
            {
                float xlo = Mathf.Min(xa, xb), xhi = Mathf.Max(xa, xb);
                float ulo = xa <= xb ? ua : ub, uhi = xa <= xb ? ub : ua;   // 右列 x 反向 ⇒ u 也跟着反
                verts[o + 0] = new Vector3(xlo, y0, 0f); uvs[o + 0] = new Vector2(ulo, 0f);
                verts[o + 1] = new Vector3(xhi, y0, 0f); uvs[o + 1] = new Vector2(uhi, 0f);
                verts[o + 2] = new Vector3(xhi, y1, 0f); uvs[o + 2] = new Vector2(uhi, 1f);
                verts[o + 3] = new Vector3(xlo, y1, 0f); uvs[o + 3] = new Vector2(ulo, 1f);
                // 绕序与 `SpriteQuad` 一致（⛔ 别按 u 的方向决定绕序 —— 右列是镜像的）
                tris[t + 0] = o + 0; tris[t + 1] = o + 2; tris[t + 2] = o + 1;
                tris[t + 3] = o + 0; tris[t + 4] = o + 3; tris[t + 5] = o + 2;
            }

            m = new Mesh { name = "BadgePlateQuad_" + name };
            m.vertices = verts;
            m.uv = uvs;
            m.triangles = tris;
            m.RecalculateBounds();
            _badgePlateQuads[name] = m;
            return m;
        }

        // 🆕 **2026-10-17（T09 第 2 步）**：`Build` 分段计时 —— **只在真 Play 且开关打开时出声**
        //   （批处理/自检零影响）。用途 = 把「每格 2.6 s」拆成 图加载 / FrameMesh / 其他建层 / rim / 文字。
        //   判据与下一步 → `资料/普查产出_1017/UI/PERF_T09_收藏窗性能诊断.md` §3 第 2a 步。
        public static bool BuildTiming = true;
        static readonly System.Diagnostics.Stopwatch _buildWatch = new System.Diagnostics.Stopwatch();

        void Build(CardData d)
        {
            // 🆕 2026-10-25（`K4`）：**建卡这一趟也要算一次数值颜色** —— 原版那张卡建出来之后
            //   `CardScript.UpdateHealthText / UpdateAttackText` 会立刻带 `doColorChange = 1` 再刷一遍
            //   ⇒ 「建出来就已经受伤的兵」第一次就该是**红**的（基准已知时；未知时本调用是空操作）。
            //   ⚠️ 必须在 `Data = d` **之前**调（它要读旧 `Data` 判 bump；首次 `Data.id` 空 ⇒ 不 bump）。
            CaptureStatChanges(d);
            Data = d;
            bool bt = BuildTiming && Application.isPlaying;
            long m0 = 0, m1 = 0, m2 = 0, m3 = 0, m4 = 0;
            if (bt) _buildWatch.Restart();

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

            // 🔴 **2026-10-25（`M2`）：原版是三级节点 `2DCard → Front(+0.08) → 卡面各层`** ——
            //   我们原来把各层直接挂在 `2DCard`（= 用 `Front` 局部坐标当 `2DCard` 局部坐标用）
            //   ⇒ 整张卡面低 0.08 卡单位。这里补出 `Front` 那一级（判据与理由 → `FrontAposY` 的 doc）。
            //   ⚠️ 与 collider 一样：**不嵌在 `if (_tiltRoot == null)` 里**，`Build` 重入时也要保证它在
            //   （幂等；两个出生入口 `Build` / `SetFace`→`Build` 都从这一处走）。
            if (_frontRoot == null && _tiltRoot != null)
            {
                var frontGo = new GameObject("Front");
                frontGo.transform.SetParent(_tiltRoot, false);
                frontGo.transform.localPosition = new Vector3(0f, FrontAposY, 0f);
                _frontRoot = frontGo.transform;
            }

            // 🆕 2026-10-19（`T09` 步骤 B 前半，`W57`）：真 collider 挂在 `_tiltRoot`（原版 `2DCard` 那一级）
            //   —— 出生入口（另一个入口 = `SetFace` 换形态，两处各显式刷一次，铁律 10 第 5 条）。
            //   ⚠️ 放在 `_tiltRoot` 建好**之后**、且**不嵌在 `if (_tiltRoot == null)` 里**：
            //   `Build` 重入时也要保证 collider 在（`EnsureHitCollider` 幂等）。
            EnsureHitCollider();

            var mf = GetComponent<MeshFilter>();
            mf.sharedMesh = Quad();

            CardArt.Load();
            // 战术卡用**另一套框**（原版 troop / stratagem 分开）—— `isUnit=false` 就是战术卡
            // 🆕 **2026-10-18（用户拍板「通过升级按钮进行不同等级的卡框切换」）**：卡框档 = **这张卡的等级**。
            //   判据（原版一手）：`UpgradePanel.ChangeFrameOnLevelUp` → `CardTierUIController.SetTier`
            //   → `CardFramesSO.GetClanFrame(army, **tier**, cardType)` —— **等级就是卡框档**（1 基，1..4）。
            //   ⇒ 取数口 = `CardProgress.Level(artId)`（`Core/CardArt` 的 `Frame/Sdf` 新增了 `tier` 形参；
            //      `TierOf` 那条「一律最高档」的老路**只留给不带 tier 的调用点**，两处不打架）。
            //   ⚠️ `artId` 为空（演示卡 / 没有引擎 id 的占位卡）⇒ **退回老行为 `MaxTier`**：
            //      那种卡不在 `CardProgress` 的表里，按「缺省等级 1」画会把演示卡框无故降档。
            int frameTier = CardTierFor(d);
            var frameTex = CardArt.Frame(d.faction, d.rarity, !d.isUnit, frameTier);
            // 🔴 2026-10-18（W15）：记下来 —— `SetFace` / `SetData` 重烘 `_info` 时要按**同一张卡框**
            //    的 uv 反算四个数值/费用的落点（`FaceStatAt01` / `FaceCostAt01`）。
            //    占位卡面那条支路 `frameTex == null` ⇒ 两处都退回原版容器常量。
            _frameTex = frameTex;

            if (frameTex != null)
            {
                // ---- 三层：立绘（后）→ 卡框（中）→ 数值（前）----
                // 根节点自带的那个 MeshRenderer 用不上 —— 不关掉它会拿默认材质渲出一块品红
                GetComponent<MeshRenderer>().enabled = false;
                var artTex = ArtTexture(d);
                if (bt) m0 = _buildWatch.ElapsedMilliseconds;      // 图加载（CardArt.Load + Frame + ArtTexture）到此
                // 🔴 **2026-10-21（第六十会话 · 用户报「插图超过卡框」）**：**放大只给前景抠图层**。
                //    原来 `_art`（底层、opaque=true）与 `_artFront` **共用同一块 ×1.27 的网格** ⇒
                //    大图档里那张**矩形**立绘也放大，越过卡框不透明外沿露出「天空/底色带」
                //    （受影响的卡：useOverDraw=0 的战术/防御卡 485 张 + 部队卡 264 张，判据 →
                //     `普查产出_1021/W60_真机与Unity_结果.md` §五）。原版只放大「角色抠图」那一层。
                // 🔴 **2026-10-24（D3）**：把「大图档**状态**」与「立绘**放大倍率**」拆成两个量 ——
                //    原版里它们本来就是两件事：`_Clamp_Range = BIG` 在状态 1 对**所有卡类型**都设
                //    （`CardImageController__ChangeState.c:99-105`），而 ×1.27 只在状态 1 **且**
                //    卡类型 ∈ {Minion, Hero} 时才乘（`SetImage.c:110-111`，判据全本见 `BigArtScaleApplies`）。
                //    ⚠️ 原来两件事都挂在 `artScale > 1f` 上 ⇒ 一旦按类型把倍率降到 1.0，硬矩形档位会
                //    跟着从「大图档」掉成「小图档」（`clampKind` 1 = 窗口更小）—— 那是**另一个**静默偏差。
                //    ⇒ 下面 `clampKind` 只认 `bigState`，不认 `artScale`。
                // 🔴 **2026-10-24（H5）**：`bigState` 就是原版 `CardImageController` 的**状态 1（Big）** ——
                //    判据收在 `BigImageState` 一处（= D3 那一版 + 两条逐卡 / 逐格的 overdraw 轴，doc 见那里）。
                //    ⚠️ 那两条轴是**窗口在 `Create` 之后**才赋值的（`CardDisplayWindow.BuildFan` :1393-1394 /
                //    `CardDetailPopup.BuildCardStack` :928-929）⇒ 出生这一趟两轴都是默认 `true`
                //    （= 与改前**逐位相同**）；之后轴一变，由那两个 setter 走 `RebakeBigStateMeshes`
                //    按新状态重烘三块网格 —— 那正是原版 `SetImageOverDraw → ChangeState → DoChange` 那一步。
                bool bigState = BigImageState;
                float artScale = ArtScaleFor(d, bigState);
                _builtData = d;            // 重烘要用**建这块网格时那份**数据（`Data` 这时可能还没赋，见 `Create`）
                _artTexBuilt = artTex;
                // 🔴 **2026-10-24（`H1` → 第七十三会话 `M2` 翻面；同日 `X1` 换判据）**：`_art` 底层的硬矩形档位**恒 0（不套）**。
                //    `H1` 那一版曾给战术/防御卡接上 `小图档 1 · 大图档 2`，理由是「原版战术卡只有 `CardImage`
                //    这一层立绘、硬矩形对所有卡型生效」。🔴 **那条推断由反汇编直接否掉**（`X1` §一）：
                //    `_Clamp_Range` = `saturate(alpha + inside01)` —— **矩形内把 α 顶到 1、矩形外原样保留**
                //    ⇒ 它**从不把 α 置 0、从不切掉插图** ⇒ 「给 `_art` 套硬矩形」在原版里**没有对应物**。
                //    旁证（方向一致，只算旁证）：原版游戏截图里战术卡插图铺满拱窗无黑边；把我们的 1024² 立绘
                //    NCC 匹配进原版大卡（0.994）证明画的是**整张插画**；套上之后板 1.7436 被缩到 ±0.5579、
                //    画窗 ±0.839…±0.872 ⇒ 每侧 0.28 卡单位黑带（= 用户报的「插图不覆盖战术卡框上面空着的地方」）；
                //    75 张卡框的离线覆盖验证不套时刚好铺满。
                //    → `ArtClampKindFor` 的 doc ＋ `ClampRangeSmall` 上面那段「我们怎么落」。
                //    ⛔ 所以这里**不要**再按卡型/状态传 clampKind —— 档位仍收在 `ArtClampKindFor` 一处
                //    （`Build` 与重烘共用），但那个函数现在恒返回 0。
                var artMesh = ArtLayerMeshFor(d, artTex, frameTex, bigState, ArtDyFor(d));
                // 🔴 2026-10-24（A7）：硬矩形照旧套 `_artFront` —— 但它是**我们自己的近似**，
                //    ⛔ 别再写成「原版 `CardImage` 在那条链上的等价物」：原版矩形外**保留**贴图 alpha、
                //    我们是把网格缩进矩形（判据/更正 → `ClampRangeSmall` 的 doc「我们怎么落」）。
                //    ⚠️ **`M2` 落定**：`_art`（整幅插图）**不套** —— 上一条就是判据。
                //    小图档**生产路径**上 opt-in 的前景层（`UseFrontLayer` 出厂 false）照旧套小图档
                //    （= 同一份近似，只在 opt-in 时可见）。
                // 🔴 **2026-10-24（H5）· 铁律 5 就地订正**：这里原来写
                //    `var artFrontMesh = bigState ? <大图档那份> : artMesh;`，注释说「小图档那一支复用 `artMesh`」——
                //    **不成立**：`_artFront` 真正用的是下面 `AddLayer` 里那个三元，`bigState == false` 时走的是
                //    **它自己的一次 `ArtMeshInFrame(..., 1)`**（`clipWindow` 跟着卡型）。两份只在「非单位卡」时
                //    逐位相同（单位卡差 `clampKind` 0 vs 1、且 `clipWindow` false vs true）⇒ 那个 `: artMesh`
                //    兜底**一次都没被读到** = 死代码（`H1` §六·4 记账，`H5` 件 2·2 核清）。
                //    现在把「小图档那一支」搬进 `ArtFrontMeshFor`、直接当 `_artFront` 的网格用 ——
                //    参数与原来那一次**逐字相同** ⇒ 行为不变，死支消失。
                var artFrontMesh = ArtFrontMeshFor(d, artTex, frameTex, bigState, ArtDyFor(d));
                // ---- 立绘：**画在框下、由框的 alpha 剪出来**（原版小卡档就是这个，**不越出**）----
                //   ① 底层「完整插图」：**忽略贴图的 alpha**（那张图的 alpha 是角色抠图，不是不透明信息），
                //      垫在卡框下面 —— 卡框拱窗自己也是透明的（实测 alpha=0），窗里的背景就靠这一层。
                //   🔴 **2026-10-18 订正**：本行原来写「⛔ 没有 ②：原版**不**在框上再重画一层角色」——
                //      **对【小卡档】成立，对【大图档】不成立**：原版大图态（`CardImageController` 状态 1）
                //      会打开那颗 `SoftMaskScript`（+ 立绘 ×1.27）。⚠️ **2026-10-24 订正**：那颗 mask 的
                //      `AlphaMask` 序列化值是 null ⇒ 它只是把**框与文字底板整层淡没**，并**不是**
                //      「用立绘 alpha 给卡框挖洞」（判据 → `ArtVisibleThroughFrame`）。我们用 ②（`_artFront`）
                //      近似大图态「角色压在金属框上」这件事，**只在大图档建**（见下；与原版那一步不完全等价）。
                //   判据是**清单**（`card_cutouts.json`，张数看该文件的 `count` —— ⚠️ 2026-10-06 更正：原写 665 张，实际 **668 张**）：单位卡基本都有、战术卡基本都没有；
                //   ⚠️ 这张清单现在决定「要不要给这张卡建前景层（抠图）」——
                //      对底层立绘与卡框那两层没有任何影响（那两层每张卡都照画）。
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
                var sdfTex = CardArt.Sdf(d.faction, d.rarity, !d.isUnit, frameTier);
                if (sdfMat != null && sdfTex != null)
                {
                    _shadowLayer = AddLayer("shadow", sdfTex, ShadowZ, sdfTex, ShadowMesh(), mat: sdfMat);
                    // 🔴 **建完立刻按本档刷一次** —— 基材质上那份是「材质默认 0.604」，
                    //    而放大窗那一档原版运行时显式设成 0.8（`A2111` 表三·2，判据见 `ShadowAlpha`）。
                    ApplyShadowColor();
                    // 🔴 **2026-10-24（L1 · `C1-B03` 并层）：描边与阴影是【同一颗件、同一份材质】**
                    //    —— 原版 `2DCard.prefab` 全文件只有 **1 颗** `Card Highlight And Shadow`，材质
                    //    `Card Frame SDF` **同时**带 `_Outline*`（绿光）与 `_ShadowColor`（阴影）
                    //    ⇒ 这里建的那一层**就是**描边层（`OutlineMat` = 它）。⛔ 别再另建 `outlineGlow`。
                    //    判据 → `资料/普查产出_1023e/C1_战斗线判据_布置与层级.md` 的 `C1-B03`
                    //    （证据：`2DCard.prefab:59-78` + 材质 `Material_-3316280387615011577.json`）。
                }
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
                // 🔴 **2026-10-10：小卡档【不建】前景层**（`UseFrontLayer` 出厂 false）——
                //    这一层就是「插图超出卡框」的真因：它在框上**重画同一张立绘、带真 alpha**
                //    ⇒ 整条角色轮廓盖住卡框（剑尖/肩甲出拱顶），再叠一层 `ArtCoverMargin`(1.04)。
                //    ⚠️ 2026-10-18 订正上面那句「原版默认不这么画」：**原版确实会**，只是**只在大图档**
                //    （`CardImageController` 状态 1：`frameMask` 把卡框整层淡没 + 立绘 ×1.27；⚠️ 2026-10-24 订正：
                //     原写「用立绘 alpha 给卡框挖洞」—— `AlphaMask` = null ⇒ 整层淡出，见 `ArtVisibleThroughFrame`）。
                //    小卡档（手牌/卡池/收藏/卡组格）原版仍是「立绘在框下、由框的 alpha 剪出来」⇒ 不越出。
                //    ⇒ 两条并存的判据都收在 `BigCardOverdraw`（大图档，默认开）与 `UseFrontLayer`（小卡档，
                //      默认关、只为「将来按 `card_cutouts.json` 做每卡实参」留的口子）。
                //    🔴 **2026-10-22（W64 · `B14`）订正**：本行原来写「**`cut` 是硬前提**：非抠图立绘
                //      （战术卡那种满幅方图）在框上重画会把整块框盖死 ⇒ 那种卡不建前景层」—— **错的**，
                //      而且与它上面刚写的 mask 语义自相矛盾：
                //      原版 `frameMask` 是「**按立绘 alpha 挖卡框**」⇒ 非抠图立绘的 alpha 是**整个立绘矩形**
                //      （2.7484² 的方图，比卡框 2.2452 × 3.2572 **宽**）⇒ 框上被挖掉的正好也是那一整块
                //      ⇒ **等价于「立绘 ×1.27 盖在框上」**（alpha=1 的像素上，两条路逐像素同结果；
                //      框的上下两条边保留，因为方图够不到）。所以「盖死」不是画错、**就是原版的画法**。
                //      ⚠️ 唯一保留的门是「**这张卡有没有真立绘**」—— 占位图（`ArtTexture` 现画的渐变）
                //      不是原版的东西，盖上去只会把框糊掉。
                bool hasRealArt = d.artOverride != null || CardArt.Portrait(d.artId) != null;
                bool bigFront = !board && hasRealArt && _bigImage && BigCardOverdraw;
                if (bigFront || (!board && cut && UseFrontLayer))
                    // ⚠️ 档位认 `bigState`（状态），**不认 `artScale`**（倍率）—— 见上面那段的判据。
                    //    战术/防御卡的大图档：`artScale` 可能是 1.0，但硬矩形仍然是**大图档**那组值。
                    // 🔴 **2026-10-24（`F1` · `R1` P1-3 就地订正）**：小图档这一支原来传的是 `null, false`
                    //    （`frameTex` 与 `clipWindow` **都丢了** —— 改前该支复用 `artMesh`，两样都在）⇒
                    //    opt-in `UseFrontLayer` 的**战术/防御卡**前景立绘不再被卡框开口裁，满幅方图盖住拱窗。
                    //    ⇒ 补回 `frameTex` 与 `clipWindow: !d.isUnit`（与上面 `artMesh` 那一处同形）。
                    //    `clampKind` 仍取 **1 = 小图档** —— 它认的是 `bigState`（本支恒 false），
                    //    ⛔ 不跟着 `artScale` 走（A7/D3 的口径，见上面那条 🔴）。
                    // ⚠️ **2026-10-24（H5）**：网格由 `ArtFrontMeshFor` **一份**给出（大图档 = ×倍率 + 大图档硬矩形；
                    //    小图档 = `clipWindow: !d.isUnit` + 小图档硬矩形）—— 原来这里那个三元就是它的两支，
                    //    收进函数后「建出来」与「状态轴变了重烘」走的是**同一个判据**（⛔ 别再在这儿拼一遍）。
                    _artFront = AddLayer("artFront", frameTex, -0.008f, artTex, artFrontMesh);
                if (!board) _frame = AddLayer("frame", frameTex, 0f, null);
                // 🆕 2026-10-24（`E5`）：**出生这一趟把状态记账**（原版 `CardImageController.currentState`）。
                //   两条 overDraw 轴现在可以经 `Create(..., artOverdraw:, useOverDraw:)` 在建卡**之前**给定，
                //   所以这里记下的就是 `Build` 真正烘进网格的那一档；之后 setter 拿它判「状态变没变」
                //   （原版 `ChangeState` 第一句 `currentState == stateToChange ⇒ return`）。
                _bigStateBaked = bigState;
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
                    // 🔴 **2026-10-14（`A1933`）：网格由 `SpriteQuad`（整张拉伸、4 顶点）换成
                    //    `TextBgQuad`（原版 `m_Type = 1` 的 **Sliced 九宫格**、6 块 = 24 顶点）** ——
                    //    判据（`m_Border = {7,0,7,9}` · `m_PixelsPerUnitMultiplier = 14.7` · 边宽换算走
                    //    uGUI `Image.GenerateSlicedSprite`）全文在 `TextBgQuad` 那一段，⛔ 别在别处再写一份。
                    _textBg = AddLayer("textBg", textBgTex, TextBgZ, textBgTex,
                                       hasDesc ? TextBgQuad("textBg", TextBgAt, TextBgW, TextBgH)
                                               : TextBgQuad("textBgSmall", TextBgSmallAt,
                                                            TextBgSmallW, TextBgSmallH));
                    _textBg.sharedMaterial.color = new Color(0f, 0f, 0f, TextBgAlpha);
                }
                else
                {
                    // 不许静默失败：少一层黑底要说出来（文字仍有描边，不至于读不出来）
                    Debug.LogWarning("[CardView] 文字底板图 `Card_Text_smooth_background` 取不到 —— " +
                                     "卡面少一层黑底。重建：python Unity/工具/sync_battle_ui_art.py");
                }
                // ⛔ **2026-10-10 删掉了一条路**：原来这里还有 `!UseFrontLayer` 那一支 —— 把卡框换成
                //    `CardPresentation/FrameCutout` shader、按立绘 alpha 给框挖洞。它是**我们自造的近似**
                //    （原版没有这个机制：原版就是「立绘在框下、框自己按 alpha 盖上去」；⚠️ 2026-10-24 订正：
                //     原来把判据写成 `SoftMask` + `FlipAlphaMask` 是错的 —— 那颗 SoftMask 不碰立绘，
                //     判据见 `ArtVisibleThroughFrame`），而且**至今没人渲图看过** ⇒ 连同 `FrameCutoutMaterial` /
                //    `CheckCutoutWiring` 一并不要了。要复活它：先渲一张并排图，⛔ 别只凭静态读字段。
                // UV 裁到卡外框；**uv2 = 立绘的 UV**（破框 shader 靠它采立绘的 alpha —— 现在没有 shader
                // 采了，但 `CheckCutoutUvAlignment` 仍在守这条对齐判据，**留着**，见那一段注释）
                if (_frame != null) _frame.GetComponent<MeshFilter>().sharedMesh =
                    FrameMesh(frameTex, artTex, artScale, ArtTypeScaleFor(d), ArtDyFor(d));
                if (bt) m1 = _buildWatch.ElapsedMilliseconds;      // FrameMesh（含 uv2 遮罩对齐）到此
                // 费用底板 / 护甲盾：原版的**两张真图**（A2 表），垫在数值底下 ——
                // z 比 `_info`(−0.02) 远、比卡框(0) 近，所以数字盖在它们上面，和原版层级一致。
                var costTex = CardArt.DeckUi("Card_Frame_Cost_Icon");
                // 🔴 **2026-10-14（`A2006①`）：督军不建这一层** —— 判据 = `ShowsCost(d)`（出处见它的注释）。
                //    ⚠️ 这一条是**出生入口**、`SetFace` 是**换形态入口**（铁律 10 第 5 条）：
                //      出生就在手牌/池子里的卡**从不走 `SetFace`**（它开头 `if (_faceMode == f) return;` 直接返回）
                //      ⇒ 只在 `SetFace` 里关**等于没关**（这正是 `A2006` 说的「卡组池 / 收藏 / 详情里督军会印出费用」）。
                if (!board && costTex != null && ShowsCost(d))
                {
                    // 🔴 2026-10-18（W15）：位置**跟着这张卡框上的阵营徽记走**（`FaceCostAt01`）。
                    //    ⚠️ `SpriteQuad` 把位置烘进**网格**（不是 transform）⇒ 每种卡框要一份自己的网格
                    //    ⇒ 名字里带卡框名（缓存按名字走）。150 个卡框 = 150 份小网格，可接受。
                    // 🔴 2026-10-18（`A1549`）：**两档**（`Cost Container` / `Cost Container Big`）——
                    //    尺寸 ×`CostScale`、落点走 `CostAt01`（含大号档的下移）。
                    //    ⚠️ 网格名**必须带档位**：`SpriteQuad` 只按名字缓存 ⇒ 两档同名会拿到另一个档的网格
                    //    （与 `A1940` 那条「键撞车」同一类）。
                    _costBg = AddLayer("costBg", costTex, -0.015f, costTex,
                                       SpriteQuad((frameTex != null ? "costBg_" + frameTex.name : "costBg")
                                                  + (CostBig ? "_big" : ""),
                                                  CostAt01(frameTex), CostBgW * CostScale, CostBgH * CostScale));
                    // 🆕 `A1549` 尾巴：记下**建出来时的档位** —— 运行时切 `SmallScreenUI` 后
                    //    `RefreshCostTier()` 靠它判「要不要重做」（见那里的注释）。
                    _costTierBuilt = CostBig ? 1 : 0;
                }
                // ⚠️ 只有**单位卡且有护甲**才画那面盾：战术卡的卡面上没有数值格
                //    （对照 `D:/2/Warpforge部队卡片/…/4计策/*.png` —— 战术卡只有费用和稀有度）
                var armourTex = d.isUnit && d.armor > 0 ? CardArt.DeckUi("pedestal_icon_armor") : null;
                if (armourTex != null)
                {
                    // 🔴 **2026-10-12：z 与位置要按形态取**（原来无条件写 2D 那一档）。
                    //    `Build` 是**出生入口**、`SetFace` 是**换形态入口**（铁律 10 第 5 条：两个入口
                    //    都要显式设一次）—— 而 `SetFace` 在「出生就在场上」（`Create(face: Board)`）
                    //    这条路上**根本不会被调用**（它开头那句 `if (_faceMode == f) return;` 直接返回）
                    //    ⇒ 出生在场上的卡（督军 / AI 出的兵）的护甲盾一直停在 **−0.015**：
                    //    比徽标那一叠（−0.118…−0.130）还**远** ⇒ 徽标一压就把盾盖掉，
                    //    而**从手牌打出去**的同类卡是 −0.140（在徽标之前）。同一个形态两套值。
                    //    （位置同理：`SetFace` 那边换的是 `armourIconBoard` 那块 mesh，这里也得换，
                    //      否则出生在场的卡盾牌停在 2D 卡面的坐标上。）
                    _armourIcon = AddLayer("armourIcon", armourTex,
                                           board ? BoardArmourIconZ : ArmourIconZ, armourTex,
                                           SpriteQuad(board ? "armourIconBoard" : "armourIcon",
                                                      board ? BoardArmourShieldAt : ArmourIconAt,
                                                      board ? BoardArmourIconW : ArmourIconW,
                                                      board ? BoardArmourIconH : ArmourIconH));
                    // 🔴 2026-10-25（`M2`）：y 按形态取（场上要抵消 `Front` 的 +0.08）——
                    //    判据与两个入口 → `PlaceArmourIcon`。⚠️ 必须在 `AddLayer` 之后。
                    PlaceArmourIcon();
                }
                // ⚠️ 数值层：场上传 `statsOnly` —— **费用/卡名/效果文字都不烘进去**
                //    （原版场上那些层整棵关掉；只有攻/血/甲留着）
                // 🆕 2026-09-29：**场上那四个数字改由四个独立图层画**（`BuildStatLayers`）——
                //    原版「受击时那一个数值变色 + bump」是按 counter 做的，烘在一张图里动不了单个。
                //    ⇒ 这一张 `_info` 在场上就**只剩底**（`skipStats`），拆不出来时（字形表不在）
                //    照旧走老路把数字烘进来。
                // 🔴 **2026-10-12：z 同样按形态取**（理由与上面 `armourIcon` 那句一字不差 ——
                //    出生在场的卡原来拿的是 2D 的 −0.02）。⚠️ 场上这张 `_info` 是**全卡大小的一张
                //    quad**，而场上它烘出来的是**全透明**（`skipStats` 那两条跳过）⇒ 挪不挪都不改变
                //    画面；这里跟着挪只为「两个入口同一个值」+ 免得它与四个数值层**同 z 共面**。
                _info = AddLayer("info", frameTex, board ? BoardInfoZ : InfoZ,
                                 InfoTexture(d, board, SplitStats(d), frameTex));
                // 🔴 2026-10-25（`M2`）：这张 quad 的 y 按形态取（场上要抵消 `Front` 的 +0.08，
                //    判据与两个入口 → `PlaceInfoLayer`）。⚠️ **必须在 `AddLayer` 之后**（它写 (0,0,z)）。
                PlaceInfoLayer();
                if (SplitStats(d)) { BuildStatLayers(); SyncStatLayers(); }
                // 稀有度宝石：叠在卡框那颗**暗色凹槽**上（原版 `Rarity` 节点）。
                // 卡框分档改的是框的形制，**光靠它看不出稀有度** —— 颜色在这颗宝石上。
                // 图取不到就不画（不静默失败：卡框和数值照常）。
                var gemTex = RarityGem(d.rarity);
                if (!board && gemTex != null) _gem = AddLayer("gem", gemTex, -0.01f, gemTex, GemMesh(frameTex));

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
                StampQueue(_face);       // 🆕 A1665：占位卡面那一层也跟着这张卡的队列段走
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
            // ⚠️ **2026-10-25（`M2`）这一层【不挂】`Front`**：原版那颗 `MinionLight` 是卡体上的
            //    SpriteRenderer（不在 `2DCard/Front` 树里）⇒ 挂在 `2DCard`，`+0.08` 由 `PlaceRim` 显式补。
            rimGo.transform.SetParent(FaceRoot, false);   // 环圈的是「看得见的那张卡」⇒ 跟着倾摆
            PlaceRim(rimGo.transform);            // 🆕 2026-09-29：位置与尺寸照原版（见 `PlaceRim`）
            rimGo.AddComponent<MeshFilter>().sharedMesh = Quad();
            _rim = rimGo.AddComponent<MeshRenderer>();
            if (bt) m2 = _buildWatch.ElapsedMilliseconds;          // 进入 rim 之前
            _rim.sharedMaterial = RimMaterial();
            if (bt) m3 = _buildWatch.ElapsedMilliseconds;          // rim 材质（`RimMaterial()` 每次都新建一份）
            StampQueue(_rim);            // 🆕 A1665：状态环也是这张卡的一层（`RimMaterial()` 每次都新建一份 ⇒ 安全）
            _rim.enabled = false;

            // 卡名/关键词走 TMP（拿不到字体资产时它自己会跳过，字仍旧烘在 _info 里）
            // ⚠️ **场上不挂**（原版整张 `2DCard` 关掉，卡名/阵营行/兵种行/效果文字一起没有）
            if (_faceMode != CardFace.Board) BuildTextLayers(d);
            if (bt) m4 = _buildWatch.ElapsedMilliseconds;          // 文字层（5 段 TMP + FitToBox 二分）到此

            // 🆕 棋盘徽标（`CardData.badges` 为空时这一句什么都不建 —— 手牌/战术卡都是空的）
            SetBadges(d.badges);

            // 🆕 2026-09-29 四个数值层：**在这里兜一次**。
            //    ⚠️ 上面 `if (frameTex != null)` 那条分支里虽然也调了，但**卡框贴图取不到时整块会跳过**
            //    （真实卡都有框，可探针卡/占位数据没有）—— 而数值层**不依赖卡框**，
            //    只靠那条路就会出现「这张卡没有数字」这种**静默缺件**（自检就是这么抓到的：
            //    `face=Board · isUnit=True · Pragati=True` 却是 `[-1,-1,-1,-1]`）。
            BuildStatLayers();
            SyncStatLayers();
            // 🆕 2026-10-25（`CF-3`）：出生入口（`Create` → `Build`）—— 2D 那四件 TMP 也在这里建/刷。
            //   场上档建出来是**关着**的（`SyncFaceStatTexts` 里 `face = false`），只有手牌/详情窗才显示。
            BuildFaceStatTexts();
            SyncFaceStatTexts();
            BuildFaceCostText();      // 🆕 2026-10-25（K4 D3）：费用 = 第五件真 TMP
            SyncFaceCostText();

            // 🆕 **2026-10-17（T09 第 2 步）**：分段计时只在真 Play 出声；批处理/自检零影响。
            if (bt)
            {
                long end = _buildWatch.ElapsedMilliseconds;
                // 🔴 **2026-10-18（`W10`）：判「有没有卡框」只能看 `frameTex != null`，⛔ 不是 `m0 > 0`。**
                //   原来那句拿**计时**当判据：`m0` = `_buildWatch.ElapsedMilliseconds`，贴图全命中缓存时
                //   它就是 **0 ms** ⇒ 真机上**每张有框的卡**都会打「（无卡框，走占位路径）」。
                //   实证（2026-10-18 第四轮真机，`Player.log`）：`ASH83` 在收藏页首建是
                //   `图加载 4 · FrameMesh 11`，同一个窗里重建两次却都打「无卡框」—— 而卡框其实**一直在**，
                //   而且**确实跟着等级换了档**（`03_carddetail` = tier1、`05_after_upgrade` = tier2，
                //   逐档模板比对 22.65 / 24.55 的残差，另一档是 ~48）。这条错日志把排查引到了
                //   「框丢了」那条错路上 ⇒ 就地改准，并把**档位/贴图名/阵营/稀有度/兵种**一并印出来
                //   （升级换框那条链的唯一真值就是这几个值）。⚠️ `m0` 的计时语义**一个字没动**。
                string frame = frameTex == null
                    ? "无卡框，走占位路径"
                    : $"卡框 {frameTex.name} · tier{frameTier}"
                      + $"（{d.faction}/{d.rarity}/{(d.isUnit ? "unit" : "tactic")}）";
                Debug.Log($"[CardView.Build] {(string.IsNullOrEmpty(d.artId) ? "?" : d.artId)} 总 {end} ms · {frame} · "
                        + $"图加载 {m0} · FrameMesh {m1 - m0} · 其他建层 {m2 - m1} · rim {m3 - m2} · "
                        + $"文字 {m4 - m3} · 收尾 {end - m4}");
            }

            // 🔴 **2026-10-25（`X17`）**：记「这一趟是不是在**未激活父链**下建的」——
            //   是 ⇒ 激活那一刻要**补一次完整重排**（`OnEnable` → `RefitTextLayers`）。
            //   为什么必须记录（而不是激活时无条件重排）：卡池/手牌每次 `SetActive` 都会触发 `OnEnable`，
            //   无条件重排会把「滚动复用」的每张卡都白重排一遍；只在**这一趟确实量不准**时才补。
            //   两个触发条件（任一）：
            //   ① 建的时候 `activeInHierarchy == false` —— `ForceMeshUpdate` 全程早退；
            //   ② 拟合路自己报了「量不出来」（`_textNeedsRefit`，见 `Fill` / `FitToBox`）。
            _built = true;
            _builtWhileInactive = !gameObject.activeInHierarchy;
        }

        // ==================================================================
        //  激活那一刻的补重排（`X17` · 2026-10-25）
        // ==================================================================
        //  🔴 **为什么需要它**（真机 2026-10-25 的证据链）：
        //   · `05_deckeditor.png`（卡池）：效果文字**收进去了**，但**卡名**「瓦利乌斯·帕克索恩」
        //     被右边框切掉 ⇒ 说明这两条拟合路在同一次建卡里**一条成功、一条失败** ——
        //     失败的正是原来还走裸 `ForceMeshUpdate` 的 `FitToWidth`（卡名/阵营/兵种那一档），
        //     而它早退的条件只有一个：**建的时候父链不活跃**。
        //   · `b14_B_hand_zoom.png`（手牌）：卡名与效果文字**都**越框 ⇒ 那一批连 `FitToBox` 也没量到。
        //   ⇒ 共同的触发入口 = 「卡在未激活父链下被建出来」。生产上至少这两处：
        //     ① `Deck/DeckRuntime.cs` 的卡池：空位那几格 `SetActive(false)` 保留视图、
        //        `RefreshPool` 装配时先 `Create`/`SetActive(true)` 再 `SetData` —— 而**换档/筛选/切页**
        //        这些装配可能整棵窗还是关着（`GameWindow.OpenByState` 的 `Closed` 支才会 `SetActive(true)`）。
        //     ② `Battle/BattleDriver.cs` 的 `SyncHand`（`Create(transform, …)`）：战斗 HUD 根
        //        在进场动画/预建阶段可能还没激活。
        //   ⚠️ 这两条是**代码可读出来的形状**；「真机那一次到底是哪一支」需要 `CardBaseDemo` 的
        //      断言 ⑥（未激活父链下建卡 → 激活 → 量字墨）跑一次才能钉死 —— 见报告。

        bool _built;                 // `Build` 跑过（`Create` 里 `AddComponent` 那一刻的 `OnEnable` 要早退）
        bool _builtWhileInactive;    // 这一趟是在**未激活父链**下建的
        bool _textNeedsRefit;        // 拟合路自报「这一次量不到可用版面」
        int _refitTries;             // `Update` 里已经重试过几帧（上限 3；批处理没有帧循环 ⇒ 只靠 `OnEnable` 那一刀）

        /// <summary>自检口径：这张卡的拟合**有没有留下待补的重排**（`X17`）。
        /// ⚠️ `false` **不代表版面一定对** —— 它只说明「量到了可用版面」；版面**像不像**由 ①②③④⑥ 去断。</summary>
        public bool TextNeedsRefit { get { return _textNeedsRefit; } }
        /// <summary>自检口径：这张卡是不是在**未激活父链**下建出来的（`X17`，断言 ⑥ 的前提）。</summary>
        public bool BuiltInactive { get { return _builtWhileInactive; } }
        /// <summary>自检口径：激活补排跑过几次（`X17`；🧨 删掉 `OnEnable` 的那一跳 ⇒ 停在 0 ⇒ 断言 ⑥ 红）。</summary>
        public int TextRefitCount { get; private set; }

        void OnEnable()
        {
            // ⚠️ `Create` 里 `AddComponent` 那一刻（对象本来就活跃）会先来一发 `OnEnable` ——
            //    那时 `_built == false`（`Build` 还没跑）⇒ 必须早退，否则会对 `default(CardData)` 建层。
            if (!_built) return;
            if (!_textNeedsRefit && !_builtWhileInactive) return;
            // 🔴 **2026-10-25（`X17` 第二轮）**：**出声记一笔**。真机/自检里「这一刀到底有没有被调用」
            //    此前只能靠间接推断（`TextRefitCount` 停在 0 时，分不清「`OnEnable` 没来」还是
            //    「来了但被某道闸挡回去」）。带上两个触发标志，下一趟日志就能一眼判死。
            //    ⚠️ 只在**真的打算补排**时打（滚动复用的常态重激活不会打，避免刷屏）。
            if (Debug.isDebugBuild || Application.isEditor)
                Debug.Log($"[CardView] `{name}` 激活补排：`_builtWhileInactive={_builtWhileInactive}`"
                        + $" · `_textNeedsRefit={_textNeedsRefit}` · `_faceMode={_faceMode}`"
                        + $" · `TmpFont.Available={TmpFont.Available}`"
                        + $" · `title={(Data.title == null ? "<null>" : "set")}`"
                        + $" · `keywords={(Data.keywords == null ? "<null>" : "set")}`");
            _refitTries = 0;
            RefitTextLayers();
        }

        /// <summary>把五层文字**整套重排一遍**（`BuildTextLayers` 是唯一入口 —— 它自己会重算文字态）。
        /// <para>⚠️ 场上档（`CardFace.Board`）根本不该有文字层（`Build` 里那条 `if`）⇒ 这里照同一条判据早退。</para>
        /// <para>⚠️ 补排之后再量不出来 ⇒ **出声**（红线：不许静默失败），并交给 `Update` 再试两帧
        /// （真 Play 有帧循环；批处理没有 ⇒ 自检里就是直接红，不会假绿）。</para></summary>
        void RefitTextLayers()
        {
            if (!TmpFont.Available) return;
            if (_faceMode == CardFace.Board) { _textNeedsRefit = false; _builtWhileInactive = false; return; }
            // ⚠️ 不是一张真卡（`default(CardData)`：卡名与效果文字都是 null）⇒ **不要**在这里凭空把文字层建出来
            //    —— 那些「空卡位」视图的层集合由 `Build` 的空卡分支决定，补排不该改变它。
            if (Data.title == null && Data.keywords == null) { _textNeedsRefit = false; _builtWhileInactive = false; return; }
            _builtWhileInactive = false;
            _textNeedsRefit = false;
            TextRefitCount++;
            BuildTextLayers(Data);
            if (_textNeedsRefit)
            {
                if (TextRefitCount <= 1)
                    Debug.LogWarning($"[CardView] `{name}`：**激活之后**仍然量不出可用文字版面"
                                   + "（`_textNeedsRefit` 又立起来）—— 版面可能停在名义字号上；不静默。");
            }
        }

        /// <summary>自检用：**立刻**补一次并返回是否真的量到了（`X17`）。
        /// ⛔ 生产不要用它代替激活钩子 —— 它不给 `TextRefitCount` 之外任何保障，存在只为「批处理没有帧循环」
        /// 这一档（那条路 `Update` 跑不到）。</summary>
        public bool RefitTextForTest() { RefitTextLayers(); return !_textNeedsRefit; }

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
        ///     🔴 **2026-10-22（`W62/D5③`）补一跳**：节点 `m_LocalScale = 1` 而 **`m_isOrthographic = 0`**
        ///     ⇒ 真 em = `0.1 × 2.45` = **0.245 卡单位**（见 `CreatedByEmOfCard` 那条注释）。
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
        ///   ② 文案：原版是个**本地化键** `Battle/HUD/CreatedBy`，值就在我们的表里（`Core/Loc.cs`，
        ///      与官方 `官方本地化_en+zh-CN.json` 逐字相同 —— ⚠️ 2026-10-22 就地订正：这里原来写
        ///      「值在**远端 I2 表**、本地没有」，那是官方表解出来**之前**的结论，已过期），见 `CreatedByLine`。
        /// </summary>
        static readonly Vector2 CreatedByAt01 = ToAt01(new Vector2(0f, 1.65f));
        const float CreatedByBoxW = 2.5f;                    // 卡单位（= 原版 `m_SizeDelta.x`）
        const float CreatedByBoxH = 0.38f;                   // 卡单位（= 原版 `m_SizeDelta.y`）
        /// <summary>基准字号 = 原版 `CreatedByText` 那颗 TMP 的 **em**（em ÷ 卡高），再按框回缩
        /// （我们的 `FitToBox` 与它那条 autosize 同语义：先给上限、量着缩）。
        ///
        /// 🔴 **2026-10-22（`W62/D5③`）就地订正 —— 原值是 `2.45f / CardUnitH`，**10 倍错**。**
        /// 实读 `bundle_menus_assets_all/GameObject/CreatedByText.json`：
        ///   · TMP `m_fontSize = 2.45` · 该节点的 `m_LocalScale = (1,1,1)` · **`m_isOrthographic = 0`**；
        ///   ⇒ 非正交那一档的换算律（`TMP_Text.cs:7183`）`em = fontSize × 0.1 × localScale`
        ///   ⇒ 原版 em = **0.245 卡单位**（= 卡高 3.3313 的 **7.35%**），不是 2.45。
        /// ⚠️ 改前「看着没问题」是因为它永远被 `FitToBox`（框高 `CreatedByBoxH = 0.38`）压到装得下 ——
        ///   两种写法都被同一只框压住 ⇒ 屏幕上几乎一样；但**常量本身必须是原版那个 em**，
        ///   否则「框够大时它该多大」这一题永远答错（同族先例：`Battle/PopNumber.cs` 头部记的那次 10 倍）。
        /// 判据出处：`资料/普查产出_1022/W62_缺陷诊断.md` §D5 + `W62_卡面红修复.md` §四·1。</summary>
        const float CreatedByEmOfCard = 0.245f / CardUnitH;

        /// <summary>自检用（`W62/D5③`）：这条常量本身 = **em ÷ 卡高**（原版 0.245 ÷ 3.3313 = 0.07354）。
        /// ⛔ 产品代码别用它 —— 要字号用 <see cref="CreatedByFontSize"/>。</summary>
        public static float CreatedByEmPerCardHeight { get { return CreatedByEmOfCard; } }
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
        ///    表里（键 `Battle/HUD/CreatedBy`；值 = `由 {0} 创建` / `Created by {0}`，与原来那两句**逐字相同**
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
        /// 没来源时 `Fill` 收到空串 ⇒ 把对象拆掉、返回 null（= 原版 `SetActive(false)`）；
        /// 有来源但**当前档不印**（`CreatedByVisible == false`）⇒ **层建着、只是关着**
        /// （= 原版那句「先 `SetActive(false)`、够条件再点亮」，⛔ 别把层拆掉 ——
        ///  拆了就看不出「该藏的时候藏住了没有」，而 `CreatedByShown` 正是靠 `activeSelf` 分辨这两态）。</summary>
        void FillCreatedBy()
        {
            if (!TmpFont.Available) return;
            // 🔴 **量之前必须先激活**：TMP 在**未激活**对象上 `ForceMeshUpdate` 量不出尺寸
            //   （本仓已知坑，见 `CLAUDE.md` 第三节「面板画出来了、字不在」的三条）
            //   ⇒ 上一轮若把它关掉了，这一轮的 `FitToBox` 会量到垃圾、把字号顶到上限。
            if (_createdBy != null) _createdBy.gameObject.SetActive(true);
            float k = Width / CardUnitW;                  // 卡单位 → 世界（当前 = 1）
            _createdBy = Fill(_createdBy, "createdBy", CreatedByLine(_createdByName),
                              CreatedByAt01, CreatedByBoxW * k, CreatedByFontSize,
                              CreatedByInk, false, CreatedByBoxH * k,
                              face: TmpFont.Face.LatinUi);       // 原版 `CreatedByText` = Asar（§2·5）
            ApplyCreatedByOutline(_createdBy);
            Show(_createdBy, CreatedByVisible);           // ← 显隐门只此一处（`SetFace` 那条走同一个属性）
        }

        /// <summary>那行字的基准字号（em = 原版 `CreatedByText` 的 em ⇒ 折成我们这套字号）。
        /// 🔴 **2026-10-22（`W62/T2`）按面标定**：这层渲的是 **Asar**（`Face.LatinUi`，见 `FillCreatedBy`），
        /// 而 `FontSizeForGlyphHeight` 的标定值量的是**汉字墨高**（`WorldGlyphPerFontSize`）——
        /// CJK 的 `m_Scale` 一改（2026-10-21 对齐成 0.9），拉丁面就跟着**凭空变大 11%**。
        /// ⇒ 改用按面的 <see cref="TmpFont.FontSizeForEm"/>（原版真 em = `0.1 × fontSize`，与 `m_Scale` 解耦）。
        /// ✅ **同日（`W62/D5③`）`CreatedByEmOfCard` 那条 10 倍错已订正**（2.45 → **0.245**，
        /// 判据写在那个常量上）⇒ 现在 `Height × CreatedByEmOfCard` 就是原版的 0.245 卡单位。
        /// ⚠️ 这是**可见变化**：`FitToBox` 不再需要把它压掉 10 倍，成字尺寸回到原版那一档。</summary>
        static float CreatedByFontSize
        {
            get { return TmpFont.FontSizeForEm(TmpFont.Face.LatinUi, Height * CreatedByEmOfCard); }
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
            _textNeedsRefit = false;      // 🆕 X17：本趟结束时由 `Fill` 按「量到没有」重新置位

            // 🔴🔴 **2026-10-25（`CF-6`）：先定「文字态」** —— 原版 `CardTextsController` 9 态里，
            //   手牌是 **always simplified**（`BasicCardUI.SetCardTextInHand` 传的是 `on = 1`），
            //   档位由全局 `GameStaticData.cardsTextInHandFormat`（`+0x140`）决定落到 5–8 中的哪一个。
            //   ⇒ 本函数是**唯一入口**：初始态走下面原来的那套（一个字节没改），简化态走
            //   `BuildSimplifiedTextLayers`（只显示一件 + Big 字号/版面）。
            _textState = ComputeTextState(d);
            if (IsSimplifiedTextState(TextState))
            {
                BuildSimplifiedTextLayers(d);
                return;
            }

            // 🔴 2026-10-25（`CF-6`）：**从简化视图/场上回来要显式放开这三层** —— `Fill` 只改字，
            //    **不会**把藏起来的对象重新激活；而 `SetFace` 那处只按「形态」开关，管不到「文字态」这条轴。
            Show(_title, true);
            Show(_keywords, true);
            Show(_race, true);
            ApplyTextBgForState(_textState);

            // 单位卡 / 战术卡**两套模板**：位置和版面框都不一样（见上面那两组常量，A3 表）
            bool unit = d.isUnit;
            float k = Width / CardUnitW;                  // 卡单位 → 世界（当前 = 1）
            var nameAt = unit ? UnitNameAt : TacticNameAt;
            var descAt = unit ? UnitDescAt : TacticDescAt;
            // 🔴 **2026-10-25（`CF-8`）：态 2（单位·无 desc）的卡名是【另一个节点】**
            //    `NameTextUnit No description`：apos (0,0)（容器 −0.7745）· size 160×23.7666 ⇒ **1.6000×0.2377** ·
            //    fs 19（em 0.19，与我们 `NameFontSize` 同值）· `Midline`。原来我们沿用了态 1 那个
            //    `NameTextUnit`（y01 0.5821 / 高 0.2973）⇒ 无 desc 的单位卡**卡名偏低、框偏高**。
            bool noDescName = _textState == CardTextState.MinionWithoutDescription;
            var armyAt = unit ? UnitArmyAt : TacticArmyAt;
            float nameW = (noDescName ? UnitNameNoDescW : (unit ? NameBoxW : TacticNameBoxW)) * k;
            float nameH = (noDescName ? UnitNameNoDescH : (unit ? NameBoxH : TacticNameBoxH)) * k;
            if (noDescName) nameAt = UnitNameNoDescAt;
            float descW = (unit ? DescBoxW : TacticDescBoxW) * k;
            // ⚠️ 效果文字的**高度预算**不在这里定 —— 它由 `FaceDescBand01` 现算（见 ② 那一段），
            //    因为那是个**对称预算**（中心 = 原版矩形中心），不是「矩形高」也不是「区块剩余高」。

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
            // 🔴 **字号用实例那份 `NameFontSize`（按 `CardUse` 分档），⛔ 不是静态的 `TitleFontSize`** ——
            //    `A2110`：放大窗那 5 张原版是 `fs16`、其余全 19。判据全在 `CardUse` 的注释里。
            _title = Fill(_title, "title", d.title, nameAt, nameW, NameFontSize, InkName, false, nameH,
                          face: TmpFont.Face.LatinUi, outline: TmpFont.TextOutline.Name);
                          // 原版 `NameTextUnit/NameTextTactic` = Asar（§2·5）；描边**在拟合之前**贴，见 `Fill` 里那段
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
            //
            //  🔴 **2026-10-24（`E16`）改摆位：从「下沿对齐」改回【原版那样以矩形中心对齐】。**
            //  **原版判据**（逐字段实读 `bundle_staticgeneralassets_assets_all` 的
            //  `GameObject/DescTextUnit.json` / `DescTextTactic.json` → 各自的 TMP + RectTransform）：
            //    · 文字节点是**固定矩形**（152.74×76.16 / 160×78.98 px，`m_LocalScale = 0.01`，
            //      anchor/pivot 全 0.5）· **`m_VerticalAlignment = 512 (Middle)`** · `m_HorizontalAlignment = 2`；
            //    · `m_enableAutoSizing = 1` · `m_fontSizeMin/Max = 1/24`（战术卡 1/19）⇒ 装不下就**按矩形缩字号**。
            //    ⇒ 原版**文字块的中心 = 矩形中心**（单位 0.7323 / 战术 0.7876），不是「下沿贴某条线」。
            //  **为什么原来不是这样**：2026-09-12 为防「行数一多就往上长、盖住阵营行」改成了下沿对齐 +
            //    可用高度；2026-09-15 又为兵种行把底边提到 0.834。**代价**（真机 2026-10-24 反复看到）：
            //    **短文字被一直压到底** ⇒ 字贴着/压着攻·甲·血那几个数字，「战将」也挤在数字行里。
            //  **现在的口径**：中心 = 原版矩形中心；预算 = `FaceDescBand01` 给的**对称**预算
            //    （= 2 × min(矩形半高, 中心↔阵营行墨下沿, 中心↔下门)，下门对有数值格的卡还夹到
            //     `StatBandTop01 − 0.01`）⇒ ① 中心与原版**逐位相同** ② 永不压阵营行 ③ 永不压数值格。
            //    ⚠️ **不要**改回下沿对齐：`CardBaseDemo.AssertFaceTextFit` 的 ③ 就是照这条判据钉的。
            bool hasRace = !string.IsNullOrEmpty(SubtypeLine(d));
            float descCenter01, descBudget01;
            FaceDescBand01(unit, hasRace, out descCenter01, out descBudget01);
            //    拟合预算（世界单位）= 对称预算 × 卡高；`Mathf.Max(0.2f, …)` 只是「门挨太近」时的兜底。
            float descH = Mathf.Max(0.2f, descBudget01 * Height);
            var descAt01 = new Vector2(descAt.x, descCenter01);
            //    ⚠️ `descAt.y` 就是 `descCenter01`（同一个常量，见 `FaceDescBand01`）——
            //       这里显式取一次只是让「x 用原版矩形、y 用区间中心」这件事在调用点上看得出来。
            // 🔴 **2026-10-25（`CF-8`）：态 4（战术·带兵种行）的效果文字是【另一个节点】**
            //    `DescTacticSmall`：apos (0,−0.0901)（容器 −0.7745 ⇒ 绝对 −0.8646 ⇒ y01 **0.7595**）·
            //    size 160×62.6409 ⇒ 框高 **0.6264** · fs **19**（em 0.19）· ls 5 / ps 8 · `Middle`。
            //    原来我们沿用了态 3 的 `DescTextTactic`（中心 0.7876 / 框高 0.7898）⇒ 排版偏下、框偏高。
            bool smallDesc = _textState == CardTextState.TacticWithRace;
            if (smallDesc)
            {
                descAt01 = new Vector2(TacticDescSmallAt.x, TacticDescSmallAt.y);
                descH = TacticDescSmallH;
            }
            // 🔴 **同批订正（同一判据链，`CF-8`）**：战术卡两档效果文字的 em 都是 **0.19**
            //    （`DescTextTactic` 与 `DescTacticSmall` 都是 `m_fontSize = 19`、`m_LocalScale = 0.01`）；
            //    而 `KeywordFontSize` 取的是**单位卡**那档 `DescTextUnit`（23.55 ⇒ 0.2355）—— 战术卡原来**大 24%**。
            //    ⛔ 别把两档合成一个常量：`DescTextUnit` 与 `DescTextTactic[Small]` 在原版就是两个字号。

            // ②·图标：`d.keywords` 里的 `[Attack]` 这类**记号**已经在 `BattleDriver.FaceTextFull`
            //    里换成了 TMP 行内 sprite（**只换那一处**，见那里的注释）。
            //    `FontScaleFor` 现在**恒返回 1** —— 试过按标高放大字号，反而把字号缩死
            //    （行更宽 ⇒ 折行更多 ⇒ 整块更高 ⇒ `FitToBox` 往死里缩），原因写在那个函数的注释里。
            float kwScale = CardIcons.FontScaleFor(d.keywords);
            float descFont = (unit ? KeywordFontSize : TacticKeywordFontSize) * kwScale;
            //    ⚠️ `bottomAlign = false` ⇒ 走 `PlaceAt`（块中心摆到 `descAt01`），**与原版 Middle 对齐同构**；
            //       改成 `true` 就是退回「下沿对齐」那个已被真机否掉的旧口径。
            _keywords = Fill(_keywords, "keywords", d.keywords, descAt01, descW, descFont,
                             InkDesc, true, descH, false, DescLineSpacing, unit ? 0f : DescParaSpacing,
                             face: TmpFont.Face.LatinCardBody, outline: TmpFont.TextOutline.Body);
                             // 原版 `DescTextUnit*` = Pragati（§2·5）；描边在拟合之前贴，见 `Fill` 里那段

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
            _army = Fill(_army, "army", CardText.Faction(d.faction), armyAt, nameW, ArmyFontSize, InkArmy, false,
                         face: TmpFont.Face.LatinCardBody, outline: TmpFont.TextOutline.Body);
                         // 原版 `ArmyText*` = Pragati（§2·5）；描边在拟合之前贴（同 `Fill` 里那段）
            Show(_army, FaceShowsArmy(_faceMode));

            // 🔴🔴 **2026-10-25（`E` · P0 · 用户/调度台并排比抓到的真缺陷）**：
            //    **态 4（战术卡 + 兵种行）的效果文字会压到阵营行身上。**
            //    复现：`Sense of Stone`（`TAU46`，中文档）—— 我们渲出来「钛帝国」（阵营行）与效果文字
            //    第一行「向己方军队送出」**压在同一 y**（截图 `_tmp_view/shots/20261025_r1/我们_Sense_of_Stone.png`）。
            //    **成因（几何算得出来，不是玄学）**：
            //      · 态 4 的效果文字走原版另一个节点 `DescTacticSmall`：rect **160×62.64 px**、中心 **y01 0.7595**
            //        （`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/DescTacticSmall.json` 实读，
            //         本地 y = −0.0901 + 容器 −0.7745 = −0.8646 卡单位）；
            //      · 那个矩形是**按一行英文**设计的（`Give [shield]Armour 1 to your troops`，fs19 在 160px 宽里一行放得下）；
            //      · 中文（`descZh` = 「给予你的部队护甲 1」→ 换图标后更长）在 **fs19 / 160px 宽**里**折两行**
            //        ⇒ 块高 ≈0.506 卡单位、**块顶升到 y01 ≈0.683**，而阵营行的行盒底在 **≈0.720**
            //        ⇒ 顶边越过阵营行底 **≈0.037 y01（≈22 px）** = 用户看到的重叠。
            //    **修法（只动「往下让」，不动原版那个中心）**：见 `ClampDescBelowArmy` ——
            //      一行时中心仍是原版 0.7595（逐位不动）；装不下时先往下让、再不够才缩字号（出声，不静默）。
            //    ⛔ **别改成「把阵营行往下挪」**：那三行（卡名/阵营行/兵种行）的原版 apos 是 prefab 判据，
            //       而且单位卡没有这个问题（`UnitDescAt` 中心 0.7323 与 `UnitArmyAt` 0.6394 之间有足够带宽）。
            if (smallDesc) ClampDescBelowArmy(_keywords, _army);

            // ④ 兵种行 —— 在卡面下部。**印不印不是「只有单位卡」那么简单**，判据见 `SubtypeLine`
            //    🆕 2026-10-17（D5）：这一格的字现在是**按当前语言**取的
            //    （`SubtypeLine` → `RaceTerm` → `Loc.T("Card_Race/<race>")`）。
            //    🔴 **2026-10-24（`E24`）**：**摆在哪**按原版的两颗节点分档（表 → `TacticRaceAt` 的 doc）——
            //      单位/督军 = `MinionOrWarlord Container/RaceText`（y01 0.8682）；
            //      战术/防御 = `Tactic Container/RaceText`（y01 **0.8796**，低 0.038 ⇒ 0.0126 卡单位）。
            //    ⚠️ 「印不印」与「摆哪」是**两条独立判据**，别合并（见 `TacticRaceAt` 的 doc 末段）。
            _raceY01 = RaceUsesTacticAt(d) ? TacticRaceAt.y : UnitRaceAt.y;
            _race = Fill(_race, "race", SubtypeLine(d), new Vector2(UnitRaceAt.x, _raceY01),
                         nameW, RaceFontSize, InkArmy, false,
                         face: TmpFont.Face.LatinCardBody, outline: TmpFont.TextOutline.Body);
                         // 原版 `RaceText*` = Pragati（§2·5）；描边在拟合之前贴（同 `Fill` 里那段）

            // ⑤ 🆕 描边 —— **原版在材质里，不在 TMP 组件上**，见 `TmpFont.ApplyOutline` 的注释。
            //    卡名 0.15 + 一团软黑影；效果/阵营/兵种 0.05 细描边。
            //    这是「文字压在亮色立绘上读不出来」那条的正面修法（原版就是这么保证可读性的）。
            // 🔴 **2026-10-25（`X17` 第四轮）**：描边现在**在 `Fill` 里、拟合之前**就贴好了
            //    （理由：`ApplyOutline` 末尾的 `UpdateMeshPadding()` 会把字形四边形按 `m_padding` 外扩，
            //     拟合时若还没贴 ⇒ 量到的字墨比画出来的小 `2p`，实测差 1.36%）。⇒ 下面这几句**幂等兜底**，
            //    保留是因为别处（`FillCreatedBy` 之外的写层点）也可能建层；⛔ 不要把它们当成「第一次贴」。
            TmpFont.ApplyOutline(_title, TmpFont.TextOutline.Name);
            TmpFont.ApplyOutline(_keywords, TmpFont.TextOutline.Body);
            TmpFont.ApplyOutline(_army, TmpFont.TextOutline.Body);
            TmpFont.ApplyOutline(_race, TmpFont.TextOutline.Body);

            // ⑥ 🆕 2026-10-18（`A985④`）「由谁造出来的」（原版 `CreatedByText`；判据见上面那组常量）。
            //    ⚠️ **原地刷、不重建**：`BuildTextLayers` 每次 `SetData` 都走（战场每刷新一次都走）
            //    ⇒ 内容从 `_createdByName` 现取（那个字段由 `SetCreatedBy` 维护），没来源时这一层被拆掉。
            FillCreatedBy();
        }

        // ==================================================================
        //  手牌文字档（原版 `CardTextsController` 9 态里的 5–8 态）—— 2026-10-25（`CF-6`）
        // ==================================================================
        // 🔴 判据（一手）：
        //   · `BasicCardUI__SetCardTextInHand.c` —— 手牌态**永远** `ToggleSimplifiedView(on = 1, option)`，
        //     option = `GameStaticData.cardsTextInHandFormat`（静态 +0x140，`CardTextsInHandOptions` = 0/1/2）；
        //   · `CardTextsController__ToggleSimplifiedView.c` 逐句：`on == 0` ⇒ 回初始态（1/2/3/4）；
        //     初始 1 + option 1 ⇒ **5**、+2 ⇒ **6**；初始 2 + 1/2 ⇒ **6**；战术 + 1 ⇒ **8**、+2 ⇒ **7**；
        //     **option 0 ⇒ 什么都不改**（保持初始态）—— 所以我们出厂（0）渲染逐位不变；
        //   · 逐态「哪些件显示 / 字号 / 版面」= prefab `Name and description` 子树逐字段实读（见下表常量）。
        // ⚠️ `CardTextsController.ToggleSimplifiedCardView` 那一处反编译出的是 **0 参调用**（实参丢了）⇒
        //    「它传什么」**查不到**；但手牌这条路（`SetCardTextInHand`）的实参是齐的，本节按它实现。
        // ==================================================================

        /// <summary>= 原版 `GameStaticData.cardsTextInHandFormat`（`CardTextsInHandOptions`：
        /// **0 Default / 1 OnlyDescription / 2 OnlyName**）。⚠️ **只影响手牌**（非手牌恒走初始态）。出厂 **0**。</summary>
        public static int HandTextFormat { get; private set; }

        /// <summary>本卡**当前的文字态**（原版 `CardTextsController` 9 态；见 <see cref="CardTextState"/>）。
        /// ⚠️ **每次读都现算**（不缓存）⇒ 不会出现「场上的卡停在 None」这种过期值；缓存只用于
        /// 「档位变了才重建」那条判断（`_textState`）。</summary>
        public CardTextState TextState { get { return ComputeTextState(Data); } }

        CardTextState _textState;      // 上一次**真正建出来**的态（只给 `RefreshTextState` 做变化检测）

        /// <summary>改全局手牌文字档并**立刻重刷在场所有卡**（原版换语言那条路同构）。返回真重刷的张数。
        /// ⚠️ 越界值**出声**并归 0 —— ⛔ 不静默。</summary>
        public static int SetHandTextFormat(int option)
        {
            if (option < 0 || option > 2)
            {
                Debug.LogWarning("[CardView] `cardsTextInHandFormat` 只允许 0/1/2"
                               + $"（`CardTextsInHandOptions`），收到 {option} ⇒ 按 0（Default）处理");
                option = 0;
            }
            HandTextFormat = option;
            return RefreshAllHandTextStates();
        }

        /// <summary>按当前档位把所有卡的**文字态**重算一遍（只有手牌形态、且态真的变了才动手）。</summary>
        public static int RefreshAllHandTextStates()
        {
            var all = Object.FindObjectsByType<CardView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int n = 0;
            foreach (var v in all) if (v != null && v.RefreshTextState()) n++;
            return n;
        }

        /// <summary>按当前全局档位重算**这一张**。返回 true = 真的重建了文字层。</summary>
        public bool RefreshTextState()
        {
            if (_faceMode != CardFace.Hand || !TmpFont.Available) return false;
            var want = ComputeTextState(Data);
            if (want == _textState) return false;
            BuildTextLayers(Data);
            return true;
        }

        /// <summary>5–8 那四态是「简化视图」（只显示一件）。</summary>
        public static bool IsSimplifiedTextState(CardTextState s)
        {
            return s == CardTextState.MinionWithDescOnlyDescription
                || s == CardTextState.MinionWithoutDescriptionSimplifiedView
                || s == CardTextState.TacticOnlyName
                || s == CardTextState.TacticOnlyDescription;
        }

        /// <summary>原版那套映射（逐句见本节开头）。**判据只此一处**。</summary>
        CardTextState ComputeTextState(CardData d)
        {
            bool unit = d.isUnit;
            bool hasDesc = !string.IsNullOrEmpty(d.keywords);          // 我们的 `keywords` 就是效果文字（desc）
            bool hasRace = !string.IsNullOrEmpty(SubtypeLine(d));
            // 初始态（原版 `CardTextsController.SetStartingState`）：单位 = 1/2（按 desc 有无）· 战术 = 3/4（按 race）
            // 🔴 2026-10-25（R16）：战术那支的 `race` **就是**原版的 `spellType != 0` —— 原版
            //   `SetStartingState.c` / `SetCardTextLayout.c` 写的是 `state = 3 + (spellType != 0)`，
            //   而 `CardRaceToString(spellType == 0)` 返回空串（兵种行不印）⇒ 两条判据是**同一件事**。
            //   于是这里不必另写一份 spellType 逻辑：`SubtypeLine` 背后的 `TacticSubtypeLineShown` 已经是
            //   `spellTypeKnown ? spellType != 0 : 白名单` ⇒ **态 3/4 自动跟着换轨**（判据只留一处）。
            var init = unit
                ? (hasDesc ? CardTextState.MinionWithDescription : CardTextState.MinionWithoutDescription)
                : (hasRace ? CardTextState.TacticWithRace : CardTextState.Tactic);
            if (_faceMode != CardFace.Hand) return init;               // 非手牌 = 初始态（原版 `on = 0`）
            int opt = HandTextFormat;
            if (opt == 0) return init;                                 // 原版 option 0 ⇒ **不改**
            if (unit)
                return (init == CardTextState.MinionWithDescription && opt == 1)
                    ? CardTextState.MinionWithDescOnlyDescription          // 5：单位有 desc + 只看 desc
                    : CardTextState.MinionWithoutDescriptionSimplifiedView; // 6：其余单位档
            return opt == 1 ? CardTextState.TacticOnlyDescription : CardTextState.TacticOnlyName;  // 8 / 7
        }

        // 4 个 Big 变体的**原版逐值**（`Name and description` 子树；节点 `m_LocalScale = 0.01`
        // ⇒ 字号 × 0.01 = em、rect 尺寸 × 0.01 = 卡单位；容器 `Name and description` 的 apos = (0, −0.7745)）
        //  · `DescTextUnit Big`            fs 26   · size 160×103.6453 · apos (0, +0.1183) · ls 5 · ps 0  · Middle
        //  · `NameTextUnit No Description Big` fs 24 · size 160×57.5474 · apos (0, −0.1370) · ls **−8** · Midline
        //  · `NameTextTacticBig`           fs 24   · size 160×80      · apos (0, −0.0700) · ls 0        · Midline
        //  · `DescTextTactic Big`          fs 25.2 · size 160×99.2930 · apos (0, −0.0883) · ls 5 · ps 8 · Middle
        static readonly Vector2 UnitDescBigAt        = new Vector2(0.5f, 0.6970f);  // y01 = 0.5 + (0.7745−0.1183)/3.3313
        static readonly Vector2 UnitNameNoDescBigAt  = new Vector2(0.5f, 0.7736f);  // 0.5 + (0.7745+0.1370)/3.3313
        static readonly Vector2 TacticNameBigAt      = new Vector2(0.5f, 0.7535f);  // 0.5 + (0.7745+0.0700)/3.3313
        static readonly Vector2 TacticDescBigAt      = new Vector2(0.5f, 0.7590f);  // 0.5 + (0.7745+0.0883)/3.3313
        const float BigTextW        = 1.6000f;                 // 四件全 160 px × 0.01
        const float UnitDescBigH    = 1.0365f;                 // 103.6453 × 0.01
        const float UnitNameBigH    = 0.5755f;                 // 57.5474 × 0.01
        const float TacticNameBigH  = 0.8000f;                 // 80 × 0.01
        const float TacticDescBigH  = 0.9929f;                 // 99.2930 × 0.01
        const float UnitDescBigEm   = 0.26f;                   // 26 × 0.01
        const float UnitNameBigEm   = 0.24f;                   // 24 × 0.01
        const float TacticNameBigEm = 0.24f;
        const float TacticDescBigEm = 0.252f;                  // 25.2 × 0.01
        const float BigLineSpacing  = 0.05f;                   // 5 × 0.01（战术 desc / 单位 desc 同值）
        const float UnitNameBigLine = -0.08f;                  // **−8** × 0.01：`NameTextUnit No Description Big` 是负行距（三行挤在一起）
        const float TacticDescBigPara = 0.08f;                 // 8 × 0.01

        // ---- CF-8：**态 2 / 态 4 那两个专用节点**（原版逐值，容器 apos 同为 (0, −0.7745)）----
        //  · `NameTextUnit No description`：size 160×23.7666 · apos (0, 0)      · fs 19（em 0.19）· Midline
        //  · `DescTacticSmall`            ：size 160×62.6409 · apos (0, −0.0901) · fs 19（em 0.19）· ls 5 / ps 8
        static readonly Vector2 UnitNameNoDescAt = new Vector2(0.5f, 0.7325f);   // 0.5 + 0.7745/3.3313
        const float UnitNameNoDescW = 1.6000f, UnitNameNoDescH = 0.2377f;        // 23.7666 × 0.01
        static readonly Vector2 TacticDescSmallAt = new Vector2(0.5f, 0.7595f);  // 0.5 + (0.7745+0.0901)/3.3313
        const float TacticDescSmallH = 0.6264f;                                  // 62.6409 × 0.01

        /// <summary>简化态（5–8）的文字层：**只显示一件**，字号/版面按上表那四个 Big 变体。</summary>
        void BuildSimplifiedTextLayers(CardData d)
        {
            bool tactic = TextState == CardTextState.TacticOnlyName || TextState == CardTextState.TacticOnlyDescription;
            bool nameOnly = TextState == CardTextState.MinionWithoutDescriptionSimplifiedView
                         || TextState == CardTextState.TacticOnlyName;

            if (nameOnly)
            {
                var at = tactic ? TacticNameBigAt : UnitNameNoDescBigAt;
                float h = tactic ? TacticNameBigH : UnitNameBigH;
                float em = tactic ? TacticNameBigEm : UnitNameBigEm;
                float ls = tactic ? 0f : UnitNameBigLine;
                // 🔴 次序守卫同 ①（`A994③`）：`Fill` 末句是 `PlaceAt`，必须先用出厂 `Middle` 摆、再设 `Midline` 真档位
                if (_title != null) _title.verticalAlignment = VerticalAlignmentOptions.Middle;
                _title = Fill(_title, "title", d.title, at, BigTextW,
                              TmpFont.FontSizeForEm(TmpFont.Face.LatinUi, em), InkName, false, h, false, ls, 0f,
                              face: TmpFont.Face.LatinUi, outline: TmpFont.TextOutline.Name);
                if (_title != null)
                {
                    _title.verticalAlignment = VerticalAlignmentOptions.Geometry;   // 原版 `Midline`(4096)
                    _title.ForceMeshUpdate();
                }
                TmpFont.ApplyOutline(_title, TmpFont.TextOutline.Name);
                Show(_title, true);
                Show(_keywords, false);
            }
            else
            {
                var at = tactic ? TacticDescBigAt : UnitDescBigAt;
                float h = tactic ? TacticDescBigH : UnitDescBigH;
                float em = tactic ? TacticDescBigEm : UnitDescBigEm;
                float ps = tactic ? TacticDescBigPara : 0f;
                _keywords = Fill(_keywords, "keywords", d.keywords, at, BigTextW,
                                 TmpFont.FontSizeForEm(TmpFont.Face.LatinCardBody, em), InkDesc, true, h, false,
                                 BigLineSpacing, ps, face: TmpFont.Face.LatinCardBody,
                                 outline: TmpFont.TextOutline.Body);
                TmpFont.ApplyOutline(_keywords, TmpFont.TextOutline.Body);
                Show(_keywords, true);
                Show(_title, false);
            }

            // 原版 5–8 的 `objectsToActivate` 名单里**没有**这三行 ⇒ 显式关（铁律 10 第 5 条：这一档入口也要设）
            Show(_army, false);
            Show(_race, false);
            Show(_createdBy, CreatedByVisible);   // 这一行不归 `CardTextsController` 管（`SetCreatedBy` 自己维护）
            ApplyTextBgForState(TextState);
        }

        /// <summary>文字底板**按态取** —— 判据只此一处（初始态与简化态共用）。
        /// <para>原版 `CardTextsController` 9 态的 `objectsToActivate` 名单（J2 §B6 逐态实读）：
        /// **只有态 1（Big）/ 态 2（Small）/ 态 5（Big）/ 态 6（Small）** 里有 `Textbackgrounds`；
        /// **态 3 / 4（战术）与态 7 / 8（战术简化）都没有** ⇒ 战术卡**不画**文字底板。</para>
        /// <para>⚠️ 网格走 `TextBgQuad` 的缓存（按名字），只换 `sharedMesh`、不重建层。</para></summary>
        void ApplyTextBgForState(CardTextState st)
        {
            if (_textBg == null) return;
            bool big = st == CardTextState.MinionWithDescription
                    || st == CardTextState.MinionWithDescOnlyDescription;
            bool small = st == CardTextState.MinionWithoutDescription
                      || st == CardTextState.MinionWithoutDescriptionSimplifiedView;
            if (!big && !small) { Show(_textBg, false); return; }   // 态 3/4/7/8：原版没有这一层
            var target = small ? TextBgQuad("textBgSmall", TextBgSmallAt, TextBgSmallW, TextBgSmallH)
                               : TextBgQuad("textBg", TextBgAt, TextBgW, TextBgH);
            var mf = _textBg.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != target) mf.sharedMesh = target;
            Show(_textBg, true);
        }

        /// <summary>🔴 2026-10-24（`E24`）：兵种行摆位**走哪颗原版节点** —— 判据只有这一处
        /// （表与出处 → `TacticRaceAt` 的 doc）。
        /// <para>非单位卡（`tactic` **和** `defence`，原版 `cardType` 都是 **20**）走 `Tactic Container/RaceText`
        /// （apos y **−0.490**）；单位/督军走 `MinionOrWarlord Container/RaceText`（**−0.452**）。</para>
        /// <para>⚠️ 没填 `type` 的构造点（自检夹具）沿用 `isUnit` 那条老路径 —— 与 `SubtypeLine` 同一个态度。</para></summary>
        static bool RaceUsesTacticAt(CardData d)
        {
            if (string.IsNullOrEmpty(d.type)) return !d.isUnit;
            return d.type == "tactic" || d.type == "defence";
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
                case "tactic":  return TacticSubtypeLineShown(d) ? RaceTerm(d.subtype) : null;   // 判据在类尾（R16）
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

        /// <summary>🆕 2026-10-25（`K4`）：**卡表的 `subtype` 展示名 → 原版词条键里的枚举名**。
        /// <para>判据（一手）：`GameStaticData__CardRaceToString.c` —— `cardType == 0x14`（战术/防御）那一支是
        /// `name = Enum.ToString(spellType@0x90)` 然后 `GetTranslation("Card_Race/" + name)`；
        /// `SpellType` 的成员表见 `dump.cs`（`PsychicPower = 90` / `CombatElixir = 260` / `Genomic = 240` /
        /// `OverlordPower = 180` / `MarkOfChaos = 190` / `DefensiveCard = 210` …）。
        /// ⇒ **原版键里没有空格**，而卡表 `subtype` 写的是**展示名**（带空格）⇒ 必须映射一次。</para>
        /// <para>⚠️ 只列**写法不同**的 6 个；`Infantry` / `Vehicle` / `Monster` / `Beast` / `Battlesuit` / `Drone` /
        /// `Daemon` / `Weapon` / `Structure` / `Secret` / `Sabotage` / `Rune` / `Invocation` / `Codicil`
        /// 的枚举名与卡表写法**逐字相同**，⛔ 别硬塞无用条目（塞了也会被自己打脸：`RaceKeyForTest` 有断言）。</para>
        /// <para>🔴 **2026-10-25（`E`）就地订正（铁律 5）**：本段原来写「官方 `Card_Race/*` 那 23 条目前**不在**
        /// `Resources/official_names.json` ⇒ 中文值仍是手写表旧译 ⇒ **57 张卡**与官方不符」—— **已不成立**：
        /// ① 生成器 `工具/_loc_official/gen_official_names.py` 的 `FAMILIES` 已加 `Card_Race`、产物已重生成
        /// （3,240 → **3,263** 条）；② `Core/Loc.cs` 那 6 条手写值也同时换成官方 zh（键名已改枚举名）。
        /// ⇒ **卡面那行字今天 = 官方 zh**（心灵力量/战斗灵药/基因增强/霸主力量/防守/黑暗契约）。
        /// 断言：`Editor/CardBaseDemo.cs` 的 `AssertSubtypeLine` ④（缺一即红）。</para></summary>
        static readonly Dictionary<string, string> RaceKeyBySubtype = new Dictionary<string, string>
        {
            { "Psychic Power",       "PsychicPower"  },   // SpellType 90
            { "Combat Elixir",       "CombatElixir"  },   // 260
            { "Genomic Enhancement", "Genomic"       },   // 240
            { "Overlord Power",      "OverlordPower" },   // 180
            { "Dark Pact",           "MarkOfChaos"   },   // 190
            { "Defence",             "DefensiveCard" },   // 210
        };

        /// <summary>自检读口（`Editor/CardBaseDemo.AssertSubtypeLine` ④）：映射表的那 6 对。</summary>
        public static string[][] RaceKeyForTest
        {
            get
            {
                var a = new string[RaceKeyBySubtype.Count][];
                int i = 0;
                foreach (var kv in RaceKeyBySubtype) a[i++] = new[] { kv.Key, kv.Value };
                System.Array.Sort(a, (x, y) => string.CompareOrdinal(x[0], y[0]));
                return a;
            }
        }

        /// <summary>🆕 **2026-10-25（收藏域搜索用）**：卡面那行「兵种」的**本地化显示名** ——
        /// 入参是卡表的 `subtype` 写法（`CardDef.Subtype`，例 `"Psychic Power"`），
        /// 返回当前语言下印在卡面底部的那串字（例 `心灵力量`）。
        /// <para>语义 = 原版 `GameStaticData.CardRaceToString` + `GetTranslation("Card_Race/" + 枚举名)`。
        /// 本函数只是把 private 的 <see cref="RaceTerm"/> 暴露出去（**判据仍只有那一处**，⛔ 别在调用方另抄一份
        /// 归一化/取词逻辑）—— 收藏域的「兵种」搜索字段用它。</para>
        /// <para>⚠️ 与 <see cref="SubtypeLine"/> 的分工：这个**只管「印哪个词」**（不判「印不印」）；
        /// 要「这张卡到底有没有那一行」用下面那个按 <see cref="CardData"/> 取的重载。</para>
        /// <param name="subtype">卡表 `subtype`（展示名写法）；空 ⇒ 原样返回空。</param>
        public static string RaceDisplayName(string subtype) { return RaceTerm(subtype); }

        /// <summary>🆕 **2026-10-25（收藏域搜索用）**：按一份卡数据取**卡面那行兵种**的显示名 ——
        /// 不印那一行的卡（原版 `RaceText` 不激活）返回 **null**。
        /// <para>判据 = <see cref="SubtypeLine"/>（印不印 + 印哪个词，**唯一一处**）。</para></summary>
        public string RaceDisplayName(CardData d) { return SubtypeLine(d); }

        /// <summary>兵种行的**显示名**（`Card_Race/&lt;原版枚举名&gt;` → 当前语言）。
        /// <para>取不到就**照原样返回那个英文串**（= 加 D5 之前的行为）并**出声** —— ⛔ 不静默。
        /// ⚠️ 同一个值**只出声一次**：卡面每次刷新都会走这条路，逐次出声会把日志刷爆。</para>
        /// <para>🔴 **2026-10-25（`K4`）**：键先过 <see cref="RaceKeyBySubtype"/> 换成**原版枚举名**
        /// （战术/防御那 6 个写法不同的），查表顺序 = ① 官方数据面（`Loc.HasBulk`，原版就是 `GetTranslation`）
        /// → ② 手写表（`Loc.HasEntry`）→ ③ **老键**（卡表写法那一条，今天实际生效的就是它）。
        /// 三者都取不到才回英文原文 + 出声。
        /// 🔴 **2026-10-25（`E`）就地订正（铁律 5）**：本行原写「今天渲染逐字节不变（官方 `Card_Race/*` 还没进数据面、
        /// 手写表也只有空格版）」—— **已过期**：官方 23 条已进数据面、手写表那 6 条也已换成枚举名 + 官方值
        /// ⇒ ① 那一档今天**真的会命中**（中文档 = 官方译法）。</para></summary>
        static string RaceTerm(string race)
        {
            if (string.IsNullOrEmpty(race)) return race;
            string enumName;
            string key = RaceTermPrefix + (RaceKeyBySubtype.TryGetValue(race, out enumName) ? enumName : race);

            if (Loc.HasBulk(key))                                  // ① 官方数据面（原版 `GetTranslation`）
            {
                string v = Loc.BulkOr(key, null);
                if (!string.IsNullOrEmpty(v)) return v;
            }
            if (Loc.HasEntry(key)) return Loc.T(key);               // ② 手写表（键已是枚举名的那几族）

            string oldKey = RaceTermPrefix + race;                  // ③ 老键（卡表写法）：今天实际走这条
            if (!string.Equals(oldKey, key, System.StringComparison.Ordinal) && Loc.HasEntry(oldKey))
                return Loc.T(oldKey);

            if (UnknownRace.Add(race))
                Debug.LogWarning($"[CardView] 兵种行 `{race}` 在语言表里查不到词条（试过 `{key}`"
                               + (string.Equals(oldKey, key, System.StringComparison.Ordinal) ? "" : "` / `" + oldKey)
                               + "`）⇒ **卡面照原样印英文**。"
                               + "加词条见 `Core/Loc.cs`（键名照原版 `Card_Race/<SpellType 枚举名>`）。");
            return race;
        }
        static readonly HashSet<string> UnknownRace = new HashSet<string>();

        /// <summary>会印在卡面底部那一行的 tactic subtype（**白名单**，实测推出来的）。
        /// ⚠️ 加新卡系时看它卡面上有没有这一行：**有就加进来，没有就别加** ——
        /// 这不是「引擎支持的 subtype 列表」，是「卡面印这一行的 subtype 列表」。
        /// <para>🔴 **2026-10-25（`K4`）找到的原版真判据 —— 只差一步接线**：
        /// `CardTextsController__SetTexts.c` 里战术卡那支是 `state = 3 + (spellType != 0)`，
        /// `CardTextsController__SetDescription.c` / `SetCardTextLayout.c` 同一条；
        /// 而 `CardRaceToString` 在 `spellType == 0` 时**返回空串** ⇒ **原版 = 「`spellType != 0` 才印」**。
        /// 本会话现算（1128 张）：白名单与 `spellType != 0` **只差一张** ——
        /// `d94f91051ecfe014f97577ff573571aa`（`Spear of Macragge` 马库拉格之矛）的 `spellType` 是 **0**
        /// 而 `subtype` 写 `Psychic Power` ⇒ **原版不印那一行，我们印**。
        /// ⇒ 判据换轨要 `CardData.spellType`（`0 = Normal` 是合法值、缺省得用 **-1** 表示「未填」；
        /// 接线在 `BattleDriver.ToCardData`）。在那之前白名单**逐张等价**（另 445 张战术卡 0 差），
        /// ⛔ 别手工把这一张从白名单踢掉 —— 那会让**别的**同名 subtype 卡一起掉行。</para>
        /// <para>🔴 **2026-10-25（R16）已落地一半（铁律 5 就地补记）**：上面的「只差一步接线」已经走进来了 ——
        /// `CardData` 新增 `spellType` + `spellTypeKnown`，判据收口到 <see cref="TacticSubtypeLineShown"/>
        /// （`spellTypeKnown` ⇒ 原版真判据；否则退回本白名单）。**本白名单同时降级为「未填时的兜底」**，
        /// ⛔ 别删（`BattleDriver.ToCardData` 填 `spellType` 之前它仍然是**唯一**在用的那一档）。
        /// ⇒ 还差的那一半只在 `Battle/**`（生产者），见报告 `R16_卡牌面遗留C_第2轮.md`。</para></summary>
        static readonly HashSet<string> TacticSubtypeShown = new HashSet<string>
        {
            "Dark Pact", "Overlord Power", "Combat Elixir", "Codicil", "Psychic Power",
            "Rune", "Invocation", "Genomic Enhancement", "Sabotage",
            // 🆕 2026-09-15 补：`Secret`（DA 的 5 张 `6秘密` 卡）—— 卡面底部橙字印 `Secret`。
            //    ⚠️ 它**不在**当初那 70 张抽样里（那批是 `.png`，而 DA `6秘密/` 全是**手机翻拍 `.jpg`**），
            //    是清「认不出的关键词」那一轮逐张开图时补上的。
            "Secret",
        };

        /// <summary>阵营行字号（原版 `ArmyTextUnit` fs15 × scale0.01 = 0.15 卡单位）。
        /// 🔴 **2026-10-22（`W62/T2`）按面标定**：这层渲的是 **Pragati**（`Face.LatinCardBody`）⇒ 走
        /// `FontSizeForEm`，⛔ 别再拿汉字墨高（`FontSizeForGlyphHeight`）标定 —— 那会让字号跟着 CJK 的
        /// `m_Scale` 一起漂（2026-10-21 的 1→0.9 就把这行放大了 11%）。判据见 `TmpFont.EmPerFontSize`。</summary>
        public static float ArmyFontSize
        {
            get { return TmpFont.FontSizeForEm(TmpFont.Face.LatinCardBody, Height * ArmyEmOfCard); }
        }

        /// <summary>兵种行字号（原版 `RaceText` fs18 × scale0.01 = 0.18 卡单位）。
        /// 🔴 **2026-10-22（`W62/T2`）按面标定**：渲的是 **Pragati** ⇒ `FontSizeForEm`（同 `ArmyFontSize`）。</summary>
        public static float RaceFontSize
        {
            get { return TmpFont.FontSizeForEm(TmpFont.Face.LatinCardBody, Height * RaceEmOfCard); }
        }

        /// <summary>
        /// 建或**原地更新**一层 TMP 文字，返回它（文字为空时返回 null 并把旧的清掉）。
        /// 原地更新是必须的 —— 这条路径每次战场刷新都会走，见 `SetData` 的注释。
        /// <para>🔴 **2026-10-24（`E16`）`bottomAlign` 现在没有调用点传 `true`** —— 效果文字那层
        /// （唯一用过它的层）改回原版的「块中心 = 矩形中心」（见 `FaceDescBand01`）。
        /// 形参留着（默认 `false`）只为不动 5 个既有调用点的位置参数；
        /// ⛔ **别再给效果文字传 `true`**：那就是退回真机否掉的「文字压着数字」那一版。</para>
        /// </summary>
        TextMeshPro Fill(TextMeshPro t, string name, string text, Vector2 at, float maxWidth,
                         float fontSize, Color32 baseColor, bool wrap, float maxHeight = 0f,
                         bool bottomAlign = false, float lineSpacing = 0f, float paragraphSpacing = 0f,
                         TmpFont.Face face = TmpFont.Face.Cjk,
                         TmpFont.TextOutline outline = TmpFont.TextOutline.None)
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
                // 🔴 **2026-10-12 就地更正（`A1659`，用户报「图标与文字不在一个层面上」）——
                //  父节点原来是 `transform`（卡根），改成 `FaceRoot`（= 子节点 `2DCard`，**倾摆挂它身上**）。**
                //  **改前为什么是缺陷**：**所有 quad 层都挂 `FaceRoot`**（`AddLayer` / 徽标 / 环 / 四个数值层
                //  —— 见 `AddLayer` 那句 `SetParent(FaceRoot)`），而文字层与角标文字挂的是**卡根**
                //  ⇒ 位移中 `AutoCardRotation` 把 `2DCard` 转到 ≤±10°、**文字/图标不跟着转**
                //  ⇒ 字面意义上的「文字/图标与底板、卡框**分成两个平面**」。静止时 `2DCard` 是 identity，
                //  位置逐位不变（所以只看截图看不出来）。
                // ⚠️ **图标本身（`<sprite>` 行内字形）不用单独处理**：它在**同一条 TMP 网格**里
                //  （`TMP_SubMesh` 与主网格同父、同 z、同队列）⇒ 父节点修完它自然跟着文字同面。
                // 🔴 **2026-10-17（`A1024`③ 批 6）**：字体面**按层**传（默认 `Cjk` ⇒ 旧调用点零行为变化）。
                //    判据 → `资料/普查产出_1017/UI/R_A1024_字体按用途普查.md` §2·5 卡面那一格
                //    （`NameTextUnit*` / `NameTextTactic*` / `CreatedByText` = Asar；其余 = Pragati）。
                t = TmpFont.NewText(FaceContentRoot, name, "", fontSize, baseColor, face);
                // 图标 sprite asset 由 `TmpFont.NewText` 统一挂上（全工程只那一处设）。
                // 原地更新的那条路上它已经在 TMP 里了，不用重设。
                StampTmpQueue(t);      // 🆕 A1665：TMP 也是这张卡的一层（含它的 sprite 子网格）
            }

            t.text = CardIcons.PinSpriteFace(text);   // 🔴 2026-10-23（新账-100）：`<sprite>` 的面钉在**本层字体**上（`<font=Default>` 零宽作用域）—— TMP 默认让它继承「前一个字符」的 faceInfo，中文里差 1.406 倍；判据与改法见 `CardIcons.PinSpriteFace` 的 doc
            t.fontSize = fontSize;                   // `FitToWidth` 会改字号，每次得先重置回基准
            t.color = (Color)baseColor * _tint;      // 高亮/置灰的状态色要跟着走，不然刷新一次就白了
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            // 行距/段距**必须在 `FitToBox` 之前设** —— 它按文本高度回缩字号，间距会改变高度
            t.lineSpacing = lineSpacing;
            t.paragraphSpacing = paragraphSpacing;

            // 🔴🔴 **2026-10-25（`X17` 第四轮 · 1.36% 的根因）**：**描边必须在这里贴 —— 拟合之前**。
            //   `TmpFont.ApplyOutline` 的最后一句是 `t.UpdateMeshPadding()`：开 `OUTLINE_ON` / 调
            //   `_OutlineWidth` / `_FaceDilate` 之后，TMP 会把**每个字形四边形按 `m_padding` 外扩**
            //   （`SaveGlyphVertexInfo(padding, …)`，`TMP_Text.cs:5290` 起）⇒ **字墨（四角并集）
            //   比贴描边之前宽 `2 × padding`**（两端各一份）。
            //   原来描边写在 `BuildTextLayers` ⑤（**所有 `Fill` 之后**）⇒ 拟合时量的是**没描边那份留白**，
            //   而画出来的是**描边那份** ⇒ 实测「未激活量到 1.5961 / 激活后 1.6178」（同一张卡、同一个
            //   `fontSize`）⇒ 差值 `0.0217 卡单位 ≈ 2p`，逐位对得上。
            //   ⇒ **拟合与渲染必须用同一份留白** ⇒ 描边在 `Fill` 里、拟合之前贴。
            //   ⚠️ `BuildTextLayers` ⑤ 那一组调用**保留**（幂等，且是别处写层的兜底），但它不再是「第一次贴」。
            if (outline != TmpFont.TextOutline.None) TmpFont.ApplyOutline(t, outline);

            // 🔴 **2026-10-25（`X17`）**：两条拟合路都回「这一次量到了可用版面没有」——
            //   量不出来时**不缩、不写缓存**，并记一笔待办：激活那一刻由 `OnEnable` → `RefitTextLayers`
            //   补一次完整重排（未激活父链下建卡的唯一兜底口，判据见 `Relayout` 的 doc）。
            bool fitOk;
            if (wrap) fitOk = Relayout(t, maxWidth, true);       // 就等于 `SetWrapWidth` + 一次生成
            else fitOk = FitToWidth(t, maxWidth);
            if (maxHeight > 0f) fitOk = FitToBox(t, maxWidth, maxHeight, wrap) && fitOk;
            if (!fitOk) _textNeedsRefit = true;

            if (bottomAlign) PlaceBottomAt(t, at, TextZ);
            else PlaceAt(t, at, TextZ);
            StampTmpQueue(t);   // 🆕 A1665：文本定下来之后再刷一次（`<sprite>` 子网格是这时候才有的）
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
        // 🔴 **2026-10-18（T09 · 首次开窗性能）**：`FitToBox` 的**量测结果缓存**。
        //   为什么要它：一次 fit 会跑 **1 + 最多 8 + 1 次 `ForceMeshUpdate`**（`FitsBox` 每次都重排 + 生成网格）；
        //   卡面在**战场每刷新一次 `SetData`**、收藏窗重建、详情窗重开时都会重走 `BuildTextLayers`
        //   ⇒ 同一段文本被反复量。缓存键 = 文本 + 字体资产 + 名义字号 + 框 + 折行 + 行距/段距
        //   ⇒ **只跳过「量」，不跳过排版**：命中时把字号设回去，后面的 `PlaceAt` / `PlaceBottomAt`
        //   那一次 `ForceMeshUpdate` 照常把网格排出来（所以命中时**不要**在这里再量一次）。
        //   ⚠️ 键里**必须带文本与字体资产**：换语言 / 换卡 / 换字体面都会让结果不同。
        //   ⚠️ 浮点按 1e-3 取整：同一个框/字号两次调用可能差 1e-7，不该漏命中。
        static readonly Dictionary<string, float> _fitCache = new Dictionary<string, float>();

        static string FitKey(TextMeshPro t, float maxWidth, float maxHeight, bool wrap, float nominal)
        {
            // 🔴 **2026-10-25（`X1`）补 `spriteAsset`**：`<sprite>` 的**绘制尺寸**由 sprite asset 的
            //   每颗 `m_Scale` 决定（见 <see cref="InkBox"/> 的 doc），换一份 sprite asset ⇒ 同一个键
            //   量出来的字墨不一样 ⇒ 不把它算进键里就是**跨上下文串味**的缓存。
            return (wrap ? "w" : "n") + "|" + (t.font != null ? t.font.GetInstanceID() : 0) + "|"
                 + (t.spriteAsset != null ? t.spriteAsset.GetInstanceID() : 0) + "|"
                 + Mathf.RoundToInt(nominal * 1000f) + "|"
                 + Mathf.RoundToInt(maxWidth * 1000f) + "|" + Mathf.RoundToInt(maxHeight * 1000f) + "|"
                 + Mathf.RoundToInt(t.lineSpacing * 1000f) + "|" + Mathf.RoundToInt(t.paragraphSpacing * 1000f) + "|"
                 + (t.text ?? "");
        }

        /// <summary>把「当前字号」的版面**权威地重算一遍** —— **激活 / 未激活都有效**。
        /// <para>🔴 **2026-10-25（`X1`）**：`ForceMeshUpdate()` 在**未激活**对象上会**早退** ——
        /// `TextMeshPro.cs:2119`：`if (!m_isAwake || (this.IsActive() == false &amp;&amp; m_ignoreActiveState == false)) return;`
        /// ⇒ 那一刻量到的 `textBounds` / `textInfo.meshInfo[].vertices` 全是**上一档字号留下的**
        /// （刚新建时是 0）⇒ `FitsBox` 一律答「装得下」⇒ **字号一次都不缩**（静默）。
        /// `TmpFont.GenerateLayout` 走 `GetTextInfo(string)`（**没有那道闸**，判据见它的 doc）⇒ 版面数与顶点都重算。
        /// ⇒ 两条都走：先 `GetTextInfo`（未激活时靠它），再 `ForceMeshUpdate`（激活时把渲染网格也推上去；未激活时无害）。</para>
        /// <para>🔴 **2026-10-25（`X17`）两处加严**：
        /// ① **返回值 = 这一次量到的版面是不是【可用测量】**（见 <see cref="LayoutUsable"/>）。
        ///    原来本函数没有返回值 ⇒ 调用方（`FitsBox`）只看 `textBounds`，而**量不出来那次**
        ///    `textBounds` 要么是 0（判「装得下」）、要么是 `GetTextBounds` 的空集哨兵 ±2.147e9
        ///    ⇒ 前者让 fit 整段不缩、后者把字号打到 5%。两种都是静默错，都没人知道「这次没量到」。
        /// ② **未激活那一档补一道收敛循环**（最多 8 趟）—— 同 `Battle/Label.cs` 的 `PushLayoutInactive`：
        ///    单跑一遍 `GetTextInfo` 在某些档位上停在半成品（`characterCount` 仍是 0），
        ///    判据全文在那个方法的 doc 里（那儿是同一族坑的正本）。</para></summary>
        static bool Relayout(TextMeshPro t, float maxWidth, bool wrap)
        {
            if (t == null) return false;
            if (wrap) TmpFont.SetWrapWidth(t, maxWidth);       // 内含 GenerateLayout
            else TmpFont.GenerateLayout(t);                    // 不折行的那几层（卡名/阵营/兵种）同一道闸
            t.ForceMeshUpdate();
            if (LayoutUsable(t)) return true;
            // 未激活 / 还没 `Awake` ⇒ `ForceMeshUpdate` 早退、`GetTextInfo` 可能停在半成品 ⇒ 再推几趟。
            // ⚠️ 已激活的对象走不到这里（TMP 自己的 `OnPreRenderObject` 会收敛）；多跑的代价只在少数几帧。
            for (int i = 0; i < 8; i++)
            {
                TmpFont.GenerateLayout(t);
                if (LayoutUsable(t)) return true;
            }
            return LayoutUsable(t);
        }

        /// <summary>这一次量到的版面**能不能当测量用**（`X17` 2026-10-25）。
        /// <para>判据三样都要：① `textInfo` 在；② `characterCount > 0`；③ `textBounds.size` 有限、非 0、且**不是哨兵**。
        /// 哨兵的来源：`TMP_Text.GetTextBounds` 在没有排版结果时返回 `k_LargePositiveVector2`/`k_LargeNegativeVector2`
        /// （±2.147e9）⇒ 尺寸 ≈ 4.29e9（同 `Battle/Label.cs` 的 `PushLayoutInactive` 记的那条）。
        /// 卡单位下正常版面 ≤ 个位数 ⇒ 用 `1e5` 当「不像测量值」的界，留足余量。</para>
        /// <para>空串**算可用**（没有东西可量，`FitsBox` 自然会答「装得下」—— 这是对的，不是静默错）。</para></summary>
        static bool LayoutUsable(TextMeshPro t)
        {
            if (t == null) return false;
            if (string.IsNullOrEmpty(t.text)) return true;
            var ti = t.textInfo;
            if (ti == null || ti.characterCount <= 0) return false;
            var sz = t.textBounds.size;
            return sz.x > 0f && Mathf.Abs(sz.x) < 1e5f && Mathf.Abs(sz.y) < 1e5f;
        }

        /// <summary>字墨 bbox（**网格顶点**，含 `<sprite>` 与全部材质槽）—— 尺寸与中心都在**文字自身局部系**。
        /// <para>🔴 **2026-10-25（`X1`）为什么不能只信 `t.textBounds`**：
        /// ① `<sprite>` 的**绘制尺寸被 `m_Scale` 放大**（现读 `Resources/Fonts/Warpforge Trait TextSprites.asset`：
        ///    每颗 glyph 的 `m_Scale = 1.65 / 1.70`，而 `m_HorizontalAdvance` 仍是 80/68.6）⇒
        ///    **图标的可见方框溢出行盒**；② 字形 overshoot 也不在行盒里。
        ///    ⇒ 「行盒装得下」≠「**看得见的字**装得下」。
        /// ② 自检那条断言（`CardBaseDemo.AssertFaceTextFit` 的 ①②④，量的是同一份顶点 bbox）
        ///    与生产**必须用同一个量** —— 否则就是「自检全绿、真机溢出」。</para>
        /// <para>🔴🔴 **2026-10-25（`X17`）就地订正上面 ① 的口径（铁律 5）—— 原句（以及 `X1` 报告 §二 里
        /// 那句量化版）写的是「图标可见方框**比排版宽度**大 65~70%」，**那只对【竖向】成立，横向是错的**：
        /// · **横向**：sprite 四边形的宽度走的仍是 `m_HorizontalAdvance`（80/68.6 px × 0.01）
        ///   ⇒ **宽 == 前进宽**，`m_Scale` **不放大宽度**（TMP 只把 `m_Scale` 乘在竖直方向/绘制高度上）
        ///   ⇒ **水平方向的「越框」不可能由 `m_Scale` 解释**。
        /// · **竖向**：四边形**高 = `m_Scale` × ascender**（1.65 / 1.70 倍 ≈ **13% 卡单位**），
        ///   而行盒只给它 `1 em ≈ 7.9%` ⇒ 上下各溢出 ~2.5%。
        /// ⇒ 所以 `ClampToInk` 对图标行**真正管的是【竖向】**；横向那半条只在「字形本身 overshoot」
        ///   与「整块比框宽」时起作用，而后者是**字号**问题（归 `FitToBox` 的二分）。
        ///   ⚠️ **横向溢出（真机 2026-10-25 截图里那一族）⛔ 不能用本条解释** —— 它的嫌疑在
        ///   「fit 根本没跑 / 量到的版面不可用」，见 `Relayout` 的返回值、`FitToWidth` 与 `FitsBox` 的注释。
        /// **错因**：`m_Scale` 这个名字读起来像「整体缩放」，而资产里那一项只管竖直；当时没有逐轴验算。</para>
        /// ⚠️ **位置无关**（只取 max−min 与中点）⇒ 在 `PlaceAt` 之前调用也有效。
        /// ⚠️ 量的**不是墨迹**，是**字形四边形**的并集（含 `<sprite>` 放大后的方框、含 ascender/descender）
        /// —— 它与自检 `CardBaseDemo.InkBoxCardLocal` 是同一个量（那边也读这四角），
        /// 所以「自检绿 = 真机不越框」这条链才成立。⛔ 别改成量贴图 alpha 的紧包围盒（两边会分叉）。
        /// 📌 **2026-10-25（`X17`）**：原来这里写「槽号按 `materialReferenceIndex` 取」，现在**不再有槽号**
        /// （改读 `characterInfo[].vertex_*`，逐字自带四角 —— 见下面那段注释）。</summary>
        /// <summary>🆕 2026-10-25（`X17` 第三轮）**自检口径**：把生产那份 `InkBox` 原样开出去。
        /// <para>🔴 为什么要它：自检原来自己算一份（`CardBaseDemo.InkBoxCardLocal`，还把四角**变换到
        /// `FaceContentRoot` 的局部系**再取 min/max）。上一轮出现了「生产判『够了』、自检量到 1.6178」
        /// 这种**同一张卡两个数**的局面 —— 而 `maxWidth`（卡名框 1.6）是**文字自己局部系**里的量
        /// （`sizeDelta` 就在那个系里）⇒ 宽度必须在**同一个系**里比。⇒ 宽度一律走这一个口；
        /// 位置类（y01）才用 `Front` 系那份（`CardBaseDemo.InkBoxCardLocal`）。</para>
        /// <para>⛔ 自检不要再写第四份（`CLAUDE.md` 那条「两处写同一条规则迟早不一致」）。</para></summary>
        public static bool InkBoxLocal(TextMeshPro t, out Vector2 size, out Vector2 center)
        {
            return InkBox(t, out size, out center);
        }

        static bool InkBox(TextMeshPro t, out Vector2 size, out Vector2 center)
        {
            size = Vector2.zero; center = Vector2.zero;
            var ti = t != null ? t.textInfo : null;
            if (ti == null || ti.characterInfo == null) return false;
            var chr = ti.characterInfo;
            float lx = float.MaxValue, hx = float.MinValue, ly = float.MaxValue, hy = float.MinValue;
            int n = Mathf.Min(ti.characterCount, chr.Length);
            for (int i = 0; i < n; i++)
            {
                // 🔴 **2026-10-25（`X17` 第三轮）判据从「`isVisible`」改成「四角是否全 0」**：
                //   `isVisible` 是 TMP 内部的可见性标记（`TextMeshPro.cs:2977/3667/3679/4540` 好几处会写它，
                //   其中 4540 那一段在**逐字循环之后**才把不可见字清零），而**画出来的范围**由四角决定。
                //   只看 `isVisible` 会在它被后续流程改掉时**静默漏掉整行字**（→ `InkBox` 回 false →
                //   `ClampToInk`/`FitToWidth` 以为没东西可收）。改成「四角全 0 才跳过」= 与「画了什么」直接对齐。
                var bl = chr[i].vertex_BL.position; var tl = chr[i].vertex_TL.position;
                var tr = chr[i].vertex_TR.position; var br = chr[i].vertex_BR.position;
                if (bl.x == 0f && bl.y == 0f && tl.x == 0f && tl.y == 0f
                    && tr.x == 0f && tr.y == 0f && br.x == 0f && br.y == 0f) continue;
                // 🔴 **2026-10-25（`X17`）改读 `characterInfo[i].vertex_*.position`（原来读 `meshInfo[slot].vertices`）**：
                //   两者**是同一批数**——`SaveGlyphVertexInfo` / `SaveSpriteVertexInfo`（`TMP_Text.cs:5290` / `:5429` 起）
                //   把字形的四角写进 `characterInfo[i].vertex_BL/TL/TR/BR.position`，这一步在**逐字循环（Phase II）**里；
                //   而 `meshInfo[].vertices` 是 **Phase III**（`TextMeshPro.cs:5023`，闸 = `m_renderMode == Render && IsActive()`）
                //   才把同一批角抄进网格数组。`GetTextInfo` 走 `DontRender` ⇒ **Phase III 被跳过** ⇒ 未激活 / 还没 `Awake`
                //   的时候 `meshInfo` 里是**上一档的旧顶点**（这一条正是「`ClampToInk` 在真机那条路上量不到字墨」的隐患）。
                //   四角字段没有这个闸；而且**不再依赖材质槽号**（`<sprite>` 会切第二份槽，槽号写死会静默串字）。
                if (bl.x < lx) lx = bl.x; if (bl.x > hx) hx = bl.x;
                if (bl.y < ly) ly = bl.y; if (bl.y > hy) hy = bl.y;
                if (tl.x < lx) lx = tl.x; if (tl.x > hx) hx = tl.x;
                if (tl.y < ly) ly = tl.y; if (tl.y > hy) hy = tl.y;
                if (tr.x < lx) lx = tr.x; if (tr.x > hx) hx = tr.x;
                if (tr.y < ly) ly = tr.y; if (tr.y > hy) hy = tr.y;
                if (br.x < lx) lx = br.x; if (br.x > hx) hx = br.x;
                if (br.y < ly) ly = br.y; if (br.y > hy) hy = br.y;
            }
            if (lx > hx || ly > hy) return false;
            size = new Vector2(hx - lx, hy - ly);
            center = new Vector2((lx + hx) * 0.5f, (ly + hy) * 0.5f);
            return true;
        }

        /// <summary>按**字墨**把字号再收到框里（`FitToBox` 的第二段）。
        /// <para>先用行盒二分（原版 `m_enableAutoSizing` 的语义、既有行为），再按**顶点 bbox** 收 ——
        /// 原版真正保证的是「**看得见的字形**装进那块固定矩形」，而行盒在含 `<sprite>` 时会**少算**
        /// （见 <see cref="InkBox"/> 的 doc）。⛔ 只缩不放 ⇒ 不会把原来排得下的卡撑大。</para>
        /// <para>🔴 **2026-10-25（`X17`）**：对**图标行**来说，本条真正起作用的方向是**竖向**
        /// （sprite 四边形高 = `m_Scale × ascender` ≈ 13% 卡单位，行盒只给 7.9%）；横向那一半
        /// **不是 `m_Scale` 的事**（宽 == 前进宽）—— 横向越框归 `FitToBox` 的二分与 `FitsBox` 的量测
        /// （见 <see cref="InkBox"/> 那条订正）。</para>
        /// <para>收不下去（`InkBox` 一直报大）⇒ **出声一次**（卡名 + 两个尺寸），⛔ 不静默。</para></summary>
        static void ClampToInk(TextMeshPro t, float maxWidth, float maxHeight, bool wrap)
        {
            const float slack = 0.998f;                      // 留一点点余量，别卡在边界上
            for (int pass = 0; pass < 6; pass++)
            {
                Vector2 sz, ct;
                if (!InkBox(t, out sz, out ct)) return;
                float f = 1f;
                if (sz.x > maxWidth * slack && sz.x > 1e-6f) f = Mathf.Min(f, maxWidth * slack / sz.x);
                if (maxHeight > 0f && sz.y > maxHeight * slack && sz.y > 1e-6f)
                    f = Mathf.Min(f, maxHeight * slack / sz.y);
                if (f >= 0.9995f) return;                    // 已经收进去了
                // 一次最多降 55%（等比缩放对折行不是线性的，分几趟收敛；趟数有上限 ⇒ 不会死循环）
                t.fontSize = Mathf.Max(t.fontSize * f, t.fontSize * 0.45f);
                Relayout(t, maxWidth, wrap);
            }
            Vector2 s2, c2;
            if (InkBox(t, out s2, out c2)
                && (s2.x > maxWidth * 1.001f || (maxHeight > 0f && s2.y > maxHeight * 1.001f)))
            {
                string tag = (t.name ?? "?") + "｜" + (t.text != null && t.text.Length > 24 ? t.text.Substring(0, 24) + "…" : t.text);
                if (_inkWarned.Add(tag))
                    Debug.LogWarning($"[CardView] 「{tag}」的字墨收不进版面框（墨 {s2.x:F4}×{s2.y:F4} vs 框 "
                                   + $"{maxWidth:F4}×{maxHeight:F4} 卡单位，字号 {t.fontSize:F3}）—— "
                                   + "图标/字形 overshoot 比行盒大；宁可出声，不静默。");
            }
        }

        /// <summary>字墨收不进去时只出声一次的去重表（键 = 层名 + 文本前缀）。</summary>
        static readonly HashSet<string> _inkWarned = new HashSet<string>();

        /// <summary>把字号拟合进版面框。**返回真 = 这一次量到了可用版面**（`X17` 2026-10-25 新加的返回值）。
        /// <para>🔴 **为什么要有返回值**：`FitsBox` 在两处会给「装得下」这个**假绿**——
        /// ① `ForceMeshUpdate` 在未激活对象上早退 ⇒ `textBounds` 是**上一档/0**（现在 `Relayout` 挡住了）；
        /// ② 即便走 `GetTextInfo`，某些档位仍会停在 `characterCount == 0` / 哨兵 `±2.147e9`
        ///    ⇒ 前者 → 判「装得下」→ 字号停在名义（= 真机越框），后者 → 判「装不下」→ 字号被打到 5%。
        /// 两种都**不允许**再往 `_fitCache` 里写（否则那次错读数会被全进程复用，
        /// 之后就算激活了再重排也**永远命不中真测量**）。调用方（`Fill`）拿到假 ⇒ 记 `_textNeedsRefit`，
        /// 由激活钩子（`OnEnable` → `RefitTextLayers`）在**真的能测**的时候补一刀。</para></summary>
        static bool FitToBox(TextMeshPro t, float maxWidth, float maxHeight, bool wrap)
        {
            if (t == null) return false;
            float nominal = t.fontSize;
            string key = FitKey(t, maxWidth, maxHeight, wrap, nominal);
            float hit;
            if (_fitCache.TryGetValue(key, out hit))
            {
                t.fontSize = hit;
                // 🔴 **2026-10-25（`X1`）**：命中只跳过**量**，不跳过**排版** —— 但这一句原来是 `return`，
                //    把「排版」全押在调用方那次 `PlaceAt → ForceMeshUpdate` 上；而那个口在**未激活**时早退。
                //    ⇒ 这里显式重算一次（`Relayout` 对激活/未激活都有效）。
                bool okHit = Relayout(t, maxWidth, wrap);
                if (!okHit) t.fontSize = nominal;        // 量不出来 ⇒ 别把缓存值当前提（见方法头）
                return okHit;
            }

            if (FitsBox(t, maxWidth, maxHeight, wrap, out bool measured) && measured)   // 装得下就别缩
            {
                ClampToInk(t, maxWidth, maxHeight, wrap);           // 🆕 X1：行盒装得下 ≠ 字墨装得下
                _fitCache[key] = t.fontSize;
                return true;
            }
            if (!measured) return false;                            // 量不出来 ⇒ 不缩、不缓存、不打 5%

            float lo = nominal * 0.05f, hi = nominal;               // lo = 已知能装下（极小），hi = 装不下
            for (int i = 0; i < 8; i++)
            {
                float mid = (lo + hi) * 0.5f;
                t.fontSize = mid;
                if (FitsBox(t, maxWidth, maxHeight, wrap, out measured) && measured) lo = mid; else hi = mid;
                if (!measured) { t.fontSize = nominal; return false; }
            }
            t.fontSize = lo;
            FitsBox(t, maxWidth, maxHeight, wrap, out measured);    // 把字号定下来的那次量测留下
            ClampToInk(t, maxWidth, maxHeight, wrap);               // 🆕 X1 ②：再按字墨收一遍
            if (measured) _fitCache[key] = t.fontSize;
            return measured;
        }

        /// <summary>量一次：当前字号装得进框吗？（顺带把折行宽度和网格更新做掉）
        /// <paramref name="measured"/> 回传「这一次的读数**是不是可用测量**」（`X17`）——
        /// ⇐ `false` 时 `fits` 那个返回值**没有意义**（⛔ 别当成「装得下」）。</summary>
        static bool FitsBox(TextMeshPro t, float maxWidth, float maxHeight, bool wrap, out bool measured)
        {
            // 🔴 **2026-10-25（`X1`）**：原来这里是 `if (wrap) SetWrapWidth(...); ForceMeshUpdate();`
            //    —— 不折行那几层（卡名/阵营/兵种）只靠 `ForceMeshUpdate`，而它在**未激活**对象上早退
            //    ⇒ 量到上一档（新建时是 0）的 `textBounds` ⇒ 判「装得下」⇒ 字号不缩。改走 `Relayout`。
            // 🔴 **2026-10-25（`X17`）**：`Relayout` 现在带返回值 ⇒ 量不出来时如实回 `measured = false`。
            measured = Relayout(t, maxWidth, wrap);
            var b = t.textBounds.size;
            const float eps = 1.002f;                               // 一点点余量，别卡在边界上来回跳
            if (!measured) return false;
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

        /// <summary>🔴 **放大窗那一档的卡名字号** —— 同一条换算，只是 `m_fontSize` 是 **16** 不是 19：
        /// `16 × 0.01 ÷ 3.3313 = 0.0480`（两位都取到小数点后 4 位时，`0.0480 ÷ 0.0570` **恰好** = `16 ÷ 19`）。
        /// 出处 = `资料/普查产出_1015战斗/C1_逐格对账_卡面放大窗与卡面渲染.md` 表三·1
        /// （`Card Display/Cards/CardUI (4)/…/NameTextUnit` 的 TMP：`m_fontSize=16` · `min=1` · `max=19`
        /// · `enableAutoSizing=1`；扫 13 arena × 8 实例 = 104 个里 `fs=16` 恰好 65 个 = 每场那 5 张）。
        /// ⚠️ 原版这 5 张也开着 `enableAutoSizing`（`m_fontSizeMax = 19`）⇒ **16 是起始值、不是硬顶**；
        ///    我们只有 `FitToWidth` 的固定回缩，同构。</summary>
        const float TitleEmOfCardZoom = 0.0480f;

        /// <summary>卡名的字号。原版 `NameTextUnit` 的 `m_fontSize = 19`、`m_LocalScale = 0.01`
        /// ⇒ **em = 0.19 卡单位**（19 × 0.01）⇒ 喂给按面的 <see cref="TmpFont.FontSizeForEm"/>。
        /// 🔴 **为什么是 `FontSizeForEm` 而不是 `FontSizeForGlyphHeight`（2026-10-22 · `W62/T2`）**：
        /// ① 这一层渲的是 **Asar**（`Face.LatinUi`），而 `FontSizeForGlyphHeight` 标定的是**汉字墨高**
        ///    ⇒ CJK 的 `m_Scale` 1→0.9 之后卡面拉丁字**真大 11%**（`W62_卡面红诊断.md` §2·2）；
        /// ② 原版那枚 TMP 是 **`m_isOrthographic = 1`**
        ///    （`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_-6860425428091954240.json`）
        ///    ⇒ `em = fontSize × face.scale × localScale`；我们是 3D `TextMeshPro`（出厂 false）
        ///    ⇒ `em = 0.1 × fontSize × face.scale`（`TMP_Text.cs:7183`）⇒ **fontSize 必须取 em ÷ 0.1 = 1.9**。
        ///    ⛔ 别把 0.19 当「19 × 0.01 的巧合」改掉 —— 两边 `isOrthographic` 那一跳差 10 倍。
        /// ⚠️ **静态这一份 = 默认档（19）** —— 编辑器的探针（`TmpSetup` / `IconSizeProbe`）用它。
        /// 卡面**实际用的是实例属性** <see cref="NameFontSize"/>（会按 <see cref="CardUse"/> 分档）。</summary>
        public static float TitleFontSize
        {
            get { return TmpFont.FontSizeForEm(TmpFont.Face.LatinUi, Height * TitleEmOfCard); }
        }

        /// <summary>🔴 **本卡实际用的卡名字号**（按 <see cref="CardUse"/> 分档）—— `BuildTextLayers` 用它。
        /// ⛔ 别改回静态的 <see cref="TitleFontSize"/>：那会让放大窗又大 18.75%（`A2110` 就是这笔账）。
        /// ⚠️ 换算与 <see cref="TitleFontSize"/> **同一档**（按面 `FontSizeForEm`，2026-10-22 `W62/T2`）。</summary>
        public float NameFontSize
        {
            get
            {
                return TmpFont.FontSizeForEm(TmpFont.Face.LatinUi,
                    Height * (_use == CardUse.ZoomWindow ? TitleEmOfCardZoom : TitleEmOfCard));
            }
        }

        /// <summary>
        /// 效果文字的字号。**2026-09-12 改：之前这条是我们挑的，现在按原版来。**
        /// 原版 `DescTextUnit`：`m_fontSize = 23.55`、`m_LocalScale = 0.01` → em = 0.2355 卡单位
        /// → 0.2355 ÷ 3.3313 = **7.07%** 卡高（旧值 3.75% 只有它一半，字明显偏小）。
        /// ⚠️ 原版同时开着 `m_enableAutoSizing`（`m_fontSizeMax = 24`），长描述运行时会被缩下来 ——
        ///    所以 7.07% 是**上限**；我们靠 `FitToBox` 按 `DescBoxH`（原版那块版面框高）收缩，同构。
        /// 出处：`子代理读报_2dcard_0827.md` A3 表 `DescTextUnit`。
        /// 🔴 **2026-10-22（`W62/T2`）按面标定**：这一层渲的是 **Pragati**（`Face.LatinCardBody`）⇒ 走
        /// `FontSizeForEm`（原版真 em = `0.1 × fontSize`）。⛔ 别再拿汉字墨高标定 —— CJK 的 `m_Scale`
        /// 1→0.9 之后，效果文字会跟着放大 11%（`W62_卡面红诊断.md` §2·3）。
        /// </summary>
        public static float KeywordFontSize
        {
            get { return TmpFont.FontSizeForEm(TmpFont.Face.LatinCardBody, Height * DescEmOfCard); }
        }

        /// <summary>0.2355 / 3.3313 —— 效果文字的 em 对卡高比</summary>
        const float DescEmOfCard = 0.0707f;

        /// <summary>🔴 **2026-10-25（`CF-8`）**：**战术卡**效果文字的 em —— 原版 `DescTextTactic` 与
        /// `DescTacticSmall` **都是 `m_fontSize = 19`、`m_LocalScale = 0.01` ⇒ em 0.19**；
        /// 而 <see cref="KeywordFontSize"/> 取的是**单位卡**那档 `DescTextUnit`（23.55 ⇒ 0.2355）。
        /// 原来两档共用一个常量 ⇒ **战术卡效果文字大 24%**（本轮订正）。</summary>
        public static float TacticKeywordFontSize
        {
            get { return TmpFont.FontSizeForEm(TmpFont.Face.LatinCardBody, 0.19f); }
        }

        /// <summary>
        /// 把整块文字**居中**摆到卡面归一化坐标 (x 从左、y 从**顶部**) 上。
        /// 用 `textBounds` 反推而不是「把原点放上去」：TMP 的原点在**第一行的基线**上，
        /// 折成两行之后整块会往下长、盖住底下的宝石（而且行数还不固定）。
        /// </summary>
        static void PlaceAt(TextMeshPro t, Vector2 at, float z)
        {
            if (t == null) return;
            // 🆕 X1：`ForceMeshUpdate` 在**未激活**对象上早退（见 `Relayout` 的 doc）⇒ 那一档改走
            //    `GetTextInfo`；**激活时保持旧路径**（一次 `ForceMeshUpdate` 就够）⇒ 热路径不多付一次 `GetTextInfo`。
            bool wrap = t.textWrappingMode == TextWrappingModes.Normal;
            float wrapW = t.rectTransform != null ? t.rectTransform.sizeDelta.x : 0f;
            if (wrap && wrapW <= 0f) wrap = false;      // 没设过折行宽 ⇒ 别拿 0 去折行
            if (t.gameObject.activeInHierarchy) t.ForceMeshUpdate();
            else Relayout(t, wrapW, wrap);
            var center = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height, z);
            // ⚠️ **2026-10-25（`X1`）这里【仍旧】按行盒中心摆**（= 原版 `m_VerticalAlignment = Middle`
            //    的语义：矩形里装的是**行盒**）。⛔ 别改成「按字墨中心摆」：那会让每一层字整体挪
            //    0.003~0.004 y01（墨心 ≠ 行盒心），而且含 `<sprite>` 的行会被额外推半个图标宽 ——
            //    那是**偏离原版**，不是修缺陷。真正要收的是**字号**（见 `FitToBox` 的 `ClampToInk`）。
            t.rectTransform.localPosition = center - (Vector3)t.textBounds.center;
        }

        /// <summary>
        /// 🔴 **2026-10-25（`E` · P0）**：把**态 4（战术·带兵种行）**的效果文字块夹在
        /// 「阵营行行盒底」与「`RaceDescBottom01` 那道下门」之间 —— **只在装不下时才动**。
        /// <para>为什么要它：原版 `DescTacticSmall`（160×62.64 @y01 0.7595）是按**一行英文**设计的；
        /// 中文折两行 ⇒ 块顶升到阵营行身上（实测重叠 ≈22px，见 `BuildTextLayers` 调用点那段注释）。</para>
        /// <para>口径（⛔ 别放宽/收紧成别的）：</para>
        /// <list type="number">
        ///   <item>先量**两层的行盒**（`textBounds`）在同一个父空间里的 `top/bottom`；</item>
        ///   <item>`块顶 ≤ 阵营行底 − 0.008 卡高` **且** `块底 ≥ 下门` ⇒ **一字不动**
        ///     （= 一行文本那一档：中心仍是原版 0.7595）；</item>
        ///   <item>否则**先往下让**（顶边贴到阵营行底 − 0.008）；让完仍越下门 ⇒ **缩字号**再摆
        ///     （最多 3 趟；真收不住就**出声**，⛔ 不静默）；</item>
        ///   <item>`army == null` / 没激活（手牌 / 场上）⇒ **空操作**（那两档本来就不印阵营行）。</item>
        /// </list>
        /// <para>⚠️ 这是**偏离**原版那个固定矩形的一处：原版遇到两行本地化文本也会越界 —— 我们选「不叠字」。
        /// 一行文本（英文档 / 短中文）时与原版**逐位相同**。</para>
        /// </summary>
        static void ClampDescBelowArmy(TextMeshPro desc, TextMeshPro army)
        {
            if (desc == null || army == null || !army.gameObject.activeSelf) return;
            float gap = 0.008f * Height;                       // 与 `FaceDescBand01` 的 0.008 y01 同一条
            float gate = (0.5f - RaceDescBottom01) * Height;    // 块的**最低**允许底边（局部 y，向上）
            for (int pass = 0; pass < 3; pass++)
            {
                float dBot, dTop, dH, aBot, aTop, aH;
                if (!TmpBoxLocal(desc, out dBot, out dTop, out dH)) return;
                if (!TmpBoxLocal(army, out aBot, out aTop, out aH)) return;
                float topLimit = aBot - gap;                   // 块顶不许高于它（y 向上 ⇒ 上界）
                if (dTop <= topLimit + 1e-4f && dBot >= gate - 1e-4f) return;    // 装得下 ⇒ 不动
                float band = topLimit - gate;
                if (band > 0f && dH > band)
                {
                    // 让完也装不下 ⇒ 按可用带宽缩字号（⛔ 不低于 1）
                    float k = Mathf.Clamp(band / dH, 0.35f, 1f);
                    desc.fontSize = Mathf.Max(1f, desc.fontSize * k);
                    if (desc.gameObject.activeInHierarchy) desc.ForceMeshUpdate();
                    else Relayout(desc, desc.rectTransform.sizeDelta.x,
                                  desc.textWrappingMode == TextWrappingModes.Normal);
                    if (!TmpBoxLocal(desc, out dBot, out dTop, out dH)) return;
                    if (dH > band && pass == 2)
                        Debug.LogWarning($"[CardView] {desc.name}：态 4 效果文字缩到 fs{desc.fontSize:F2} 仍比"
                                       + $"可用带 {band:F3} 高（块 {dH:F3}）⇒ 会越门。判据见 `ClampDescBelowArmy`");
                }
                // 顶边贴到 (阵营行底 − 0.008) —— 只在压到阵营行 / 越下门时执行
                float wantCenter = topLimit - dH * 0.5f;
                float curCenter = (dBot + dTop) * 0.5f;
                desc.rectTransform.localPosition += new Vector3(0f, wantCenter - curCenter, 0f);
            }
        }

        /// <summary>`ClampDescBelowArmy` 的量法：TMP **行盒**在父空间里的 `bottom/top/height`。
        /// 批处理没有帧循环 ⇒ 未激活的走 `Relayout`（同 `PlaceAt` 那条规矩）。
        /// ⚠️ 前提：这一层 `localScale == 1`（`Fill` 只改字号/尺寸，不改缩放）——
        /// 真改过就出声并**不夹**（宁可不改，也不按错标架挪字）。</summary>
        static bool TmpBoxLocal(TextMeshPro t, out float bottom, out float top, out float h)
        {
            bottom = top = h = 0f;
            if (t == null) return false;
            var s = t.rectTransform.localScale;
            if (Mathf.Abs(s.x - 1f) > 1e-3f || Mathf.Abs(s.y - 1f) > 1e-3f)
            {
                if (!_tmpBoxScaleWarned)
                {
                    _tmpBoxScaleWarned = true;
                    Debug.LogWarning($"[CardView] {t.name}：localScale = ({s.x:F3},{s.y:F3}) ≠ 1 ⇒ "
                                   + "`ClampDescBelowArmy` 按未缩放行盒算会把字挪错，本轮**不夹**");
                }
                return false;
            }
            bool wrap = t.textWrappingMode == TextWrappingModes.Normal;
            float wrapW = t.rectTransform != null ? t.rectTransform.sizeDelta.x : 0f;
            if (wrap && wrapW <= 0f) wrap = false;
            if (t.gameObject.activeInHierarchy) t.ForceMeshUpdate();
            else Relayout(t, wrapW, wrap);
            var b = t.textBounds;
            var p = t.rectTransform.localPosition;
            h = b.size.y;
            bottom = p.y + b.center.y - h * 0.5f;
            top = p.y + b.center.y + h * 0.5f;
            return h > 0f;
        }
        static bool _tmpBoxScaleWarned;

        /// <summary>
        /// 把整块文字**下沿对齐**摆到 (x01, y01) 那个点上（y 从顶算）。
        /// 🔴 **2026-10-24（`E16`）起【没有调用点】** —— 效果文字那层改回原版的「块中心 = 矩形中心」
        /// （见 `FaceDescBand01` 的 doc 与 `Fill` 的 `bottomAlign` 形参注释），所以下沿对齐这条
        /// 旧口径整个下线了。**留着是为了**将来真有「必须贴底」的层时不必重写；
        /// ⚠️ 但它**不是**效果文字的摆法 —— 旧 doc 这里写「效果文字用这个」的那半句**已作废**。
        /// </summary>
        static void PlaceBottomAt(TextMeshPro t, Vector2 at, float z)
        {
            if (t == null) return;
            // 🆕 X17（2026-10-25）：与 `PlaceAt` 同一条 —— `ForceMeshUpdate` 在未激活对象上早退，
            //   那一档必须走 `Relayout`（`GetTextInfo`）才量得到 `textBounds`。
            bool wrap = t.textWrappingMode == TextWrappingModes.Normal;
            float wrapW = t.rectTransform != null ? t.rectTransform.sizeDelta.x : 0f;
            if (wrap && wrapW <= 0f) wrap = false;
            if (t.gameObject.activeInHierarchy) t.ForceMeshUpdate();
            else Relayout(t, wrapW, wrap);
            float botY = (0.5f - at.y) * Height;
            var center = new Vector3((at.x - 0.5f) * Width, botY + t.textBounds.size.y * 0.5f, z);
            t.rectTransform.localPosition = center - (Vector3)t.textBounds.center;
        }

        /// <summary>🆕 2026-10-25（`X17` 第三轮）`FitToWidth` 的**逐趟诊断**（只在 Debug/Editor 下出声，
        /// 且只在「真的要缩一刀」或「行盒贴顶但量不到字墨」时打 —— 短卡名一趟都不打，不会刷屏）。
        /// <para>为什么要它：上一轮复验的 `fontSize` 与 `字墨宽` **一个数都没变**，而这两条在 `FitToWidth`
        /// 里没有别的写入点 ⇒ 只能是「这一趟判了够了」或「`InkBox` 拿不到」。这行日志把两者一眼分开：
        /// `inkOk=0` ⇒ 这一趟**没量到字墨**（`characterCount` / `isVisible` / 顶点三者里有一个是空）。</para></summary>
        static void LogFitW(TextMeshPro t, string why, int pass, float boxW, bool inkOk, float inkW, float maxWidth)
        {
            if (!Debug.isDebugBuild && !Application.isEditor) return;
            Debug.Log($"[CardView.fitW] `{(t != null ? t.name : "?")}` {why} pass{pass}"
                    + $" fs={(t != null ? t.fontSize : 0f):F4} 行盒={boxW:F4}"
                    + $" 字墨={(inkOk ? inkW.ToString("F4") : "n/a")} inkOk={(inkOk ? 1 : 0)} 框={maxWidth:F4}");
        }

        /// <summary>太宽就把字号整体回缩到装得下（原来点阵那条路也是「缩号到放得下」）。
        /// <para>🔴 **2026-10-25（`X17`）**：不折行的那几层（**卡名 / 阵营行 / 兵种行**）走的是本函数，
        /// 而它原来用的是**裸 `ForceMeshUpdate()`** —— 那个口在**未激活**对象上早退
        /// （`TextMeshPro.cs:2119`），量到 0 ⇒ `w &lt;= 0f` ⇒ **一次都不缩**。`X1` 只把
        /// `FitsBox`（`FitToBox` 那条）改成了 `Relayout`，**这条漏了** ⇒ 未激活父链下建出来的卡，
        /// **效果文字会缩、卡名不缩** —— 真机 2026-10-25 `05_deckeditor.png` 里
        /// 「瓦利乌斯·帕克索恩」卡名被右边框切掉正是这一支（效果文字那两行反而是收进去的）。
        /// ⇒ 改走 `Relayout`（内含 `GetTextInfo` 收敛），并把「量不出来」如实往上报。</para>
        /// <para>🔴🔴 **2026-10-25（`X17` 第二轮复核）—— 为什么现在按【字墨】收，而不是只按行盒**：
        /// 只按 `textBounds`（行盒）收会**差一点点**：实机自检实测「行盒收到 = 卡名框 1.6000」之后，
        /// **字墨仍是 1.6178（越框 +1.1%）** —— 首末字形的墨迹会略微探出各自的 `advance`。
        /// 这与效果文字那条（`ClampToInk`）是同一条口径：**「行盒装得下」≠「看得见的字装得下」**
        /// （见 `InkBox` 的 doc）。⇒ 现在 `max(行盒宽, 字墨宽)` 一起收，最多 3 趟（只缩不放）。
        /// ⚠️ 与 `ClampToInk` 的区别：那条按宽×高两个方向收，这条**只收宽**（不折行的层高度由 `FitsBox`/`height` 管）。</para></summary>
        static bool FitToWidth(TextMeshPro t, float maxWidth)
        {
            if (t == null) return false;
            bool ok = Relayout(t, maxWidth, false);
            if (!ok) return false;
            bool inkSeen = false;
            for (int pass = 0; pass < 6; pass++)
            {
                float boxW = t.textBounds.size.x;               // 行盒（既有口径）
                Vector2 sz, ct;
                bool inkOk = InkBox(t, out sz, out ct);
                if (inkOk) inkSeen = true;
                float w = (inkOk && sz.x > boxW) ? sz.x : boxW;  // 🆕 字墨（看得见的那一半）说了算
                if (w <= maxWidth || w <= 1e-6f)
                {
                    // ⚠️ **只按行盒判「够了」不算验过**：字形墨迹可比 advance 累积宽 ~1.3%
                    //    ⇒ 行盒贴着上限（> 98%）而这一趟**没量到字墨**时，如实回 false
                    //    （= 「我证明不了它装得下」）⇒ 调用方 `Fill` 记 `_textNeedsRefit`，
                    //    等激活那一刻（`OnEnable` → `RefitTextLayers`，那时一定量得到）再收一刀。
                    //    ⛔ 不许在这里「算了，差不多」—— 那正是真机越框那一支。
                    if (!inkSeen && boxW > maxWidth * 0.98f)
                    {
                        LogFitW(t, "未量到字墨 · 行盒贴顶", pass, boxW, inkOk, sz.x, maxWidth);
                        return false;
                    }
                    return true;
                }
                // 只缩不放；等比缩对字形是线性的，几趟就收敛
                LogFitW(t, "缩一刀", pass, boxW, inkOk, sz.x, maxWidth);
                t.fontSize *= maxWidth / w;
                ok = Relayout(t, maxWidth, false);
                if (!ok) return false;
            }
            Vector2 s2, c2;
            if (InkBox(t, out s2, out c2) && s2.x > maxWidth * 1.001f)
            {
                string tag = "fitWidth｜" + (t.name ?? "?") + "｜"
                           + (t.text != null && t.text.Length > 16 ? t.text.Substring(0, 16) + "…" : t.text);
                if (_inkWarned.Add(tag))         // 与 `ClampToInk` 共用去重表；⛔ 不静默
                    Debug.LogWarning($"[CardView] 「{tag}」按字墨也收不进版面宽度（墨 {s2.x:F4} vs 框 {maxWidth:F4} 卡单位，"
                                   + $"字号 {t.fontSize:F3}）—— 字形墨迹比前进宽大；宁可出声，不静默。");
            }
            return true;
        }

        /// <param name="opaque">true = 用 `CardPresentation/ArtOpaque` 画（**只取 RGB、忽略贴图 alpha**）。
        /// 只有立绘底层要它 —— 立绘的 alpha 是角色抠图，直接按它混合的话底层就没法铺满拱窗。</param>
        MeshRenderer AddLayer(string name, Texture2D basis, float z, Texture2D tex, Mesh mesh = null,
                              bool opaque = false, Material mat = null, Transform parent = null)
        {
            var go = new GameObject(name);
            // 默认挂 `2DCard/Front`（卡面各层，见 `FrontAposY`）；`parent` 只给「坐标其实是
            // `3DBody` 空间」的那几层用（今天的唯一调用点 = 徽标底板 `badgePlate`，见它的注释）。
            go.transform.SetParent(parent != null ? parent : FaceContentRoot, false);
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
            StampQueue(mr);          // 🆕 A1665：这一层跟着这张卡的队列段走（见 `SetQueue`）
            _layers.Add(mr);
            return mr;
        }

        /// <summary>🆕 **2026-10-11（A1507）**：把一颗**外面建的** quad 的材质换成「**只取 RGB、忽略贴图 alpha**」
        /// 那一档（= 立绘底层用的 `CardPresentation/ArtOpaque`，见下面 `OpaqueMaterial`）。
        /// <para>**为什么要有这个口**：原版 1126 张卡的立绘里有 **668 张**是「角色抠图 alpha」
        /// （`Resources/Art/cards/card_cutouts.json` 的清单；判据 = 全透明像素 > 5%）。
        /// **卡面**一直是按「忽略 alpha」画的（`AddLayer(..., opaque: true)`）⇒ 看着是完整插图；
        /// 而**卡格**（`MenuDraw.DeckCell` 拿同一张图当整格图）走的是普通 `Sprites/Default` ⇒
        /// 抠图卡只剩一个淡淡的人影、整格近乎空的（2026-10-11 实测就是这样）。
        /// ⇒ 卡格共用同一份材质，两边才一致。</para>
        /// <para>⚠️ **会保住原来的渲染队列**（`SetRenderQueue` 设过的那一档）——
        /// 新建材质会把它清零，而卡格是靠队列分层的。</para></summary>
        public static void MakeOpaqueQuad(ImageQuad q)
        {
            if (q == null) return;
            var mr = q.GetComponent<MeshRenderer>();
            if (mr == null) return;
            int oldQ = mr.sharedMaterial != null ? mr.sharedMaterial.renderQueue : 0;
            var m = new Material(OpaqueMaterial());
            m.mainTexture = q.Texture;
            if (oldQ != 0) m.renderQueue = oldQ;
            mr.sharedMaterial = m;
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

        // 🔴 **2026-10-10 删掉了 `DebugNoArtFront`** —— 它原来的用途是「关掉前景层、单独看底层」
        //    （分辨杂色是底层带来的还是双层引起的），而前景层现在**出厂就不建**（见 `UseFrontLayer`）
        //    ⇒ 它已恒为空操作，留着只会和 `UseFrontLayer` 互相打架（**静默失效的开关**）。
        //    要看 opt-in 那一档，直接翻 `UseFrontLayer`。

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
        /// （`BuildBody3D` 那条 warning 会把「是谁开的」写进日志，不静默。）</summary>
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

        /// <summary>🔴 **本档的阴影 alpha** —— 原版这一项也**逐树不同值**（铁律 5·c）：
        /// 材质默认 **0.604**（手牌 · 场上 · 多卡窗走它），而**放大窗**由
        /// `CardDisplayWindow.InitializeCardForDisplay` 运行时**显式**设成 **(0,0,0,0.8)**
        /// （`_DAT_1834b3310`，2026-10-15 现解；属性名我当天也解出来了 = `_ShadowColor` —— 全过程见 <see cref="CardUse"/> 注释）。
        /// ⚠️ 第三个值 `0.53333336` 是**卡背**那份材质（`Card Backs SDF`），与这里无关，别混。</summary>
        float ShadowAlpha { get { return _use == CardUse.ZoomWindow ? 0.8f : 0.604f; } }

        /// <summary>把本档的阴影色（只乘整卡淡出 `_alpha`）刷进这一层的**实例材质**
        /// （`AddLayer` 每层都 `new Material(...)` 克隆过 ⇒ 改它不会串到别的卡）。
        /// 🔴 **判据只此一处** —— 建层那处与 `ApplyTint` 那处都调它（两处各写一遍 = 迟早不一致）。</summary>
        void ApplyShadowColor()
        {
            if (_shadowLayer == null || _shadowLayer.sharedMaterial == null) return;
            var sm = _shadowLayer.sharedMaterial;
            if (sm.HasProperty("_ShadowColor"))
                sm.SetColor("_ShadowColor", new Color(0f, 0f, 0f, ShadowAlpha * _alpha));
        }

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
            // 🔴 **2026-10-22：原版材质里还躺着一个 `_SpriteAlphaEdge = 0.397`**（逐值现读
            //    `bundle_duplicateassetisolation_assets_all/Material/Material_-3316280387615011577.json`
            //    的 `m_SavedProperties.m_Floats` = 0.3970000147819519）。卡背那份材质抄过它（见 `CardbackSdfMaterialBase`），
            //    卡面这条一直漏着 ⇒ 补齐。
            //    ⚠️ **本版 shader 并不声明它**：`Everguild/FX/Card Highlight And Shadow` 的 21 个属性里
            //    只有 `_SpriteAlphaEdge_Outer` / `_SpriteAlphaEdge_Inner`（判据 = `资料/普查产出_0917/shader属性表_块1.md:248`）
            //    ⇒ 它在原版材质上属**残留死值**（同 `_Metallic` / `_FaceDilate` 那批），`HasProperty` 守 ⇒
            //    今天**不改变任何像素**。抄它只为让材质表与原版逐项对齐，别据此推断行为。
            F("_SpriteAlphaEdge",        0.397f);
            F("_NoiseIntensity",         1.15f);
            F("_NoiseMaskScale",         1f);
            // 🔴 **2026-10-21（`W61` 卡牌线 `新账-66`）：补上 `_Noise` 槽。**
            //    判据 = `bundle_duplicateassetisolation_assets_all/Material/Material_-3316280387615011577.json`
            //    的 `m_TexEnvs`：`_Noise` → pathID **-8214795764591836323** = 贴图 **`Noise Combined`**
            //    （128²；卡背那份材质 `Card Backs SDF` 绑的是**同一张**——卡背那条路早就绑了，
            //    **卡面这条一直漏着** ⇒ 采样落到默认白图上，`_NoiseIntensity=1.15` 等于白算）。
            //    用户参考图（`桌面/warpforge参考图/战斗截图参考.png`）里那圈**带颗粒、有流动感**的绿光
            //    就是这张噪声 + `_Noise_Speed_Scale_StepLow_StepHigh` 的产物；不绑 = 一条干净的带子。
            var noise = CardArt.CardbackSdfNoise();
            if (noise != null && _sdfMat.HasProperty("_Noise")) _sdfMat.SetTexture("_Noise", noise);
            else Debug.LogWarning("[CardView] 卡面 SDF：`Noise Combined` 取不到 ⇒ 描边发光少一层颗粒"
                               + "（跑 `工具/import_original_card_sdf.py` 补）");
            // 同一条材质里的 `_Noise_Speed_Scale_StepLow_StepHigh = (0.01, 2, 0.06, 0.21)`（逐值现读）。
            C("_Noise_Speed_Scale_StepLow_StepHigh", new Color(0.01f, 2f, 0.06f, 0.21f));
            _sdfMat.EnableKeyword("_ALPHACHANNEL_R");     // 原版 `m_ValidKeywords`
            _sdfMat.EnableKeyword("_NOISE_CHANNEL_B");
            _sdfMat.renderQueue = 3000;                   // 原版 `m_CustomRenderQueue`
            Debug.Log($"[CardView] 卡面软光/影：`{Name}` ← {src}（材质值照原版 `Card Frame SDF`）");
            return _sdfMat;
        }

        // ==================================================================
        //  描边（`_Outline`）—— **与软光/影同一颗件、同一份材质**（2026-10-24 `C1-B03` 并层）
        // ==================================================================
        //
        // 🔴 **判据（铁律 10 第 1 条：先查原版，别自己发明结构）**：原版 `2DCard.prefab` 全文件
        //   **只有 1 颗** `Card Highlight And Shadow`（`2DCard.prefab:59-78`，`Front` 的第一个子件；
        //   `grep -c "m_Name: Card Highlight And Shadow"` = **1**）。它的 Image 材质 `Card Frame SDF`
        //   **同一份材质同时带** `_Outline*`（绿光）与 `_ShadowColor`（阴影）
        //   （`bundle_duplicateassetisolation_assets_all/Material/Material_-3316280387615011577.json`）
        //   ⇒ 阴影与描边是**同一个 Image 的两个材质属性**，**没有第二层**。
        //   ⇒ 我们这里只建**一层**（`_shadowLayer`）：`_ShadowColor` 出落地阴影、`_Outline` 出那圈光。
        //   判据全文 → `资料/普查产出_1023e/C1_战斗线判据_布置与层级.md` 的 `C1-B03`（裁定 = **并层**）。
        //
        // ⚠️ **历史上曾经另开过一层**（2026-10-21 `W61` 卡牌线 `新账-66` 的 `outlineGlow` 专用层，
        //   quad = `4.4281² × OutlineGlowScale`、`_ShadowColor.a=0`、z 0.055）。它 2026-10-22 就把
        //   `OutlineGlowScale` 降回 **1.0**（= 原版节点的 `localScale`），于是两层**几何完全相同、只差 z 0.005**
        //   ⇒ 纯冗余过绘制。那段历史（以及「1.35 是在给 Linear/Gamma 差做补偿」的成因）
        //   留在 `资料/普查产出_1022/W63_换牌倾斜与绿光判据_调查.md` §2，⛔ 别在这里重开一层。
        //   ⚠️ 若并排渲图后光带偏内/偏外：**先查 `card_sdf/*.png.meta` 的 `sRGBTexture`（应为 0）**，
        //   再考虑改 `ShadowSize`/几何 —— 那是全局量，别为描边单独调。

        /// <summary>描边现在画在哪份材质上 —— **判据只此一处**（`SetOutline` / `OutlineColor` /
        /// `ApplyTint` / `ClearOutline` 都读它）。
        /// 并层之后它恒等于软光/影那一层的材质；那一层建不出来（原版 shader / SDF 取不到）时返回 null
        /// ⇒ 描边随阴影一起没有（原版本来就是同一颗件，缺件就是缺件，⛔ 不另建替身）。</summary>
        Material OutlineMat
        {
            get { return _shadowLayer != null ? _shadowLayer.sharedMaterial : null; }
        }

        /// <summary>把描边灭掉（材质 `_Outline` 的 α → 0，**并且清掉 `_outlineTarget` + 杀掉补间**）。
        ///
        /// <para>🔴 **为什么必须清 `_outlineTarget`**：`SetOutline` 开头有一条 `SameColor(_outlineTarget, c)` 早退
        /// （那是 2026-09-19 为了治「补间永远推不动」加的，**不能删**）。只把材质 α 写成 0、目标留着旧色的话，
        /// 下次 `SetOutline(同一个颜色)` 会**直接返回** ⇒ 描边**永不重亮**（静默，画面只是「不亮」）。
        /// 这与 `_canActWanted` 那一族「请求被丢掉」是同一个坑（见 `SetCanAct` / `ApplyCanAct`）。</para>
        ///
        /// <para>⚠️ **谁调**：`SetFace(Board)` —— 并层之后「场上不画描边」不能再靠藏那一层
        /// （藏了连落地阴影也没了）。场上**出生**那一档不用调：模板的 `_Outline` α 本来就是 0。</para></summary>
        void ClearOutline()
        {
            if (_outlineTween != null && _outlineTween.IsActive()) _outlineTween.Kill();
            _outlineTarget = new Color(0f, 0f, 0f, 0f);
            var m = OutlineMat;
            if (m == null || !m.HasProperty("_Outline")) return;
            var c = m.GetColor("_Outline");
            m.SetColor("_Outline", new Color(c.r, c.g, c.b, 0f));
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

        /// <summary>2D 立绘层自己的不透明度（0 = 全透明）。**只影响 `_art` 那一层**。
        /// ⚠️ 2026-10-18：大图档那层 `_artFront`（角色抠图）**不跟它** —— 它只在「非场上」的详情窗里有，
        /// 而本函数只被「手牌 ↔ 场上」那条动作用（`CardFeel.ReturnToHand`），两者不可能同时存在。</summary>
        public void SetArtAlpha(float a)
        {
            _artAlpha = Mathf.Clamp01(a);
            if (_art != null) Show(_art, _artAlpha > 0f);
            ApplyTint();
        }

        /// <summary>2D 立绘层现在的不透明度（自检断言用）。</summary>
        public float ArtAlpha { get { return _artAlpha; } }

        // ── 只作用于「2D 卡面那一套」的不透明度（原版 `2DCard` 那个 CanvasGroup）──────────────
        //  🔴 2026-10-22（W64 · `B05`）：原版登场 clip 的 `2DCard.m_Alpha` 是**一个 CanvasGroup**，
        //  只淡 `2DCard` 那棵子树 —— 3D 卡体（`Board Elements/3DBody`）**不在里面**。
        //  我们原来只有整卡 `SetAlpha`（连 3D 卡体一起淡）⇒ 材质复位只能拖到 0.7（原版是 **0.55**，
        //  `DelayedResetMaterial(0.55)`）。有了这一档，0.55 换回正常材质之后剩下的 2D 淡出
        //  **不会再把 3D 体淡一遍**，与 `CardScript._MinionPlayedIntoField_d__314` 的时序一致。
        float _alpha2D = 1f;

        /// <summary>只给 2D 卡面那一套乘的不透明度（1 = 不动）。**3D 卡体不受影响**。</summary>
        public void SetAlpha2D(float a) { _alpha2D = Mathf.Clamp01(a); ApplyTint(); }

        /// <summary>2D 卡面那一档的不透明度（自检断言用）。</summary>
        public float Alpha2D { get { return _alpha2D; } }

        // ==================================================================
        //  🆕 2026-10-22（W64 · `B04`）登场 clip 缺的**三条曲线**要用的口
        // ==================================================================
        //  判据（一手）：`bundle_battleprefabs_vfxandmisc_assets_all/AnimationClip/
        //    AnimationClip_-3614292623624764332.json`（`Card Hand To Board`）：
        //    · `2DCard/Front/CardFrame` 的 `m_Enabled`：0 → **1 @0.0333** → **0 @0.75**
        //    · `2DCard/Front/CardImage` 的 `m_LocalPosition.y` = **−0.065**（整条曲线只有一个关键帧 = 常量）
        //    · `2DCard/Front/CardImage` 的 ScaleCurves：1 → 1 @0.3333 → **0.9049578 @0.55**
        //    · `2DCard` 自己的 `m_LocalPosition.z`：0 → **−0.043 @0.1667**
        //  ⚠️ 这些值都是**卡局部单位**，与 `CardFeel.HandToBoardBodyY*` 用 clip 原值的方式一致。

        // 🔴 **2026-10-22（`B04` 尾巴）订正**：这里原来有个 `SetFrameVisible(bool)`，是照
        //   `2DCard/Front/CardFrame.m_Enabled` 那条曲线写的 —— **读错了目标**：那条曲线的
        //   `classID = 114`（MonoBehaviour）、`script` 与 `floatProperty1` **完全相同** ⇒
        //   它开关的是 **`AnimateUIMaterialProperty` 这个组件**，**不是卡框的显隐**。
        //   卡框整段都在（跟着 `2DCard` 的 α 淡出、0.7 随 `SetFace(Board)` 一起关）。
        //   ⇒ 该 API 已删，`m_Enabled` 那两个时刻改接到**材质置换的窗口**（见下）。

        // ==================================================================
        //  🆕 2026-10-22（`B04` 尾巴）：**2D 卡框的溶解**
        //    = 原版 `AnimateUIMaterialProperty`（在 `2DCard/Front/CardFrame` 上）
        //    ＋ 材质 `SpriteUI Frame Dissolve`（shader `Everguild/FX/Sprite Dissolve Mask`）
        // ==================================================================
        //  判据（一手，逐条带出处 → `资料/普查产出_1022/W64_B04_floatProperty1材质.md`）：
        //   · clip `Card Hand To Board` 的 `floatProperty1.value`：`(0,1)(0.0167,1)(0.7,0)`、
        //     `materialProperty = "_DissolveAmount"`；**同一个组件的** `m_Enabled`：`(0,0)(0.0333,1)(0.75,0)`。
        //   · 运行时（`decomp_full/AnimateUIMaterialProperty__*.c`）：`OnEnable` → 存原材质、以
        //     `changeMaterialOnAnimation` 为底 `new Material` 换给 Image；`Update` 每帧
        //     `SetFloat("_DissolveAmount", value)`；`OnDisable` → 还原原材质；`OnDestroy` → 销毁实例。
        //   · 原版那份材质（`Material_-1050090670530995784.json`，全库**只有这一个组件引用它**）的
        //     序列化值见下面几个常量。⚠️ `_SrcBlend/_DstBlend/_ZWrite/_Surface/_Cull/_Cutoff` 在
        //     shader 属性表里**没有** ⇒ 全是死值，⛔ 一个都不设（CLAUDE.md §三 那条坑）。
        //   · 🔴 我们不是 UGUI ⇒ `_MainTex` 要**自己绑**（原版由 CanvasRenderer 喂 sprite）：
        //     绑的就是卡框那层当前那张图（`_frame.sharedMaterial.mainTexture`）。
        public const string FrameDissolveShader = "Everguild/FX/Sprite Dissolve Mask";
        /// <summary>`Visual1_2780b5644c1e463480f9eca2dba65f9a` = 0.07（shader 里叫 **`Dissolve Borded Width`**，
        /// 原版拼写如此；默认 0.04、这份材质改成 0.07）。</summary>
        public const float FrameDissolveBorderWidth = 0.07f;
        /// <summary>`Vector4_d8c31f3b5dd04c76a4cc43d5d7873e6d` = (1.2, 1.2, 0, 0)（DissolveUVScale XY / Offset ZW）。</summary>
        public static readonly Vector4 FrameDissolveUVScale = new Vector4(1.2f, 1.2f, 0f, 0f);
        /// <summary>`_Color` = Border Color（HDR 蓝）= (0.0588235, 0.3289232, 0.9058824, 1)。</summary>
        public static readonly Vector4 FrameDissolveColor = new Vector4(0.0588235f, 0.3289232f, 0.9058824f, 1f);
        /// <summary>`_DISSOLVE_CHANNEL` = 2 + 关键字 `_DISSOLVE_CHANNEL_B`</summary>
        public const float FrameDissolveChannel = 2f;

        public const string FrameDissolveBorderWidthProp = "Vector1_2780b5644c1e463480f9eca2dba65f9a";
        public const string FrameDissolveUVProp = "Vector4_d8c31f3b5dd04c76a4cc43d5d7873e6d";
        public const string FrameDissolveMultiplyEdgeProp = "BOOLEAN_D80F51B627624648A0B3B2D563AA7541";

        Material _frameDissolveMat;
        Material _frameMatBeforeDissolve;
        bool _frameDissolveTried;
        float _frameDissolveAmount = -1f;

        // ==================================================================
        //  🆕 2026-10-22（`B04` 尾巴②）：**`ToBoardFX` / `Spawn Particles` / `Trail`**
        // ==================================================================
        //  判据（一手，全文 → `资料/普查产出_1022/W64_B04_ToBoardFX与粒子.md`）：
        //   · `ToBoardFX` = 原版 `CardScript.toBoardVFXAnchor`（**空锚点**，`m_Children: []`、
        //     `m_IsActive = 1` 常开），localPos = **(0.0033340454, 0.007981658, 0.046000004)**
        //     （`AssetRipper …/CardPrefab.prefab` 的 Transform 逐位实读）。
        //   · `ToBoardFX/Spawn Particles` **不在预制体里**：它是 `CardSetup.c:305-333` 在运行时把
        //     `normalSummon`（传说档 `legendarySummon`）实例**挂到锚点下、改名 `"Spawn Particles"`、
        //     再 `SetActive(false)`** 的产物；`Trail` 是那件 prefab 自带的第三颗粒子。
        //   · clip 的三条 `m_IsActive` 曲线：`ToBoardFX` 恒 1 · `Spawn Particles` 0→1 @**0.0167** ·
        //     `…/Trail` 0→1 @**0.55**。
        public const float ToBoardFxAnchorX = 0.0033340454f;
        public const float ToBoardFxAnchorY = 0.007981658f;
        public const float ToBoardFxAnchorZ = 0.046000004f;
        /// <summary>原版 `CardSetup.c:330` 改名用的字面量（`va_map.txt:6254` 解出 = `'Spawn Particles'`）。</summary>
        public const string SpawnParticlesNodeName = "Spawn Particles";

        Transform _toBoardFx;
        GameObject _spawnParticles;
        Transform _trail;

        /// <summary>`ToBoardFX` 空锚点（懒建；原版是卡预制体的直接子件，localPos 见上面三个常量）。
        /// 没有它 ⇒ 建出来（我们这套卡是程序化建的，原版那个节点来自 `CardPrefab`）。</summary>
        public Transform ToBoardFxAnchor
        {
            get
            {
                if (_toBoardFx == null)
                {
                    var go = new GameObject("ToBoardFX");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = new Vector3(ToBoardFxAnchorX, ToBoardFxAnchorY, ToBoardFxAnchorZ);
                    _toBoardFx = go.transform;
                }
                return _toBoardFx;
            }
        }

        /// <summary>`Spawn Particles` 挂上了没有（自检用）。</summary>
        public bool SpawnParticlesMounted { get { return _spawnParticles != null; } }
        /// <summary>`Spawn Particles` 现在开着没有（自检用）。</summary>
        public bool SpawnParticlesActive { get { return _spawnParticles != null && _spawnParticles.activeSelf; } }
        /// <summary>`Trail` 那颗粒子现在开着没有（自检用）。没有那个节点 ⇒ false。</summary>
        public bool TrailActive { get { return _trail != null && _trail.gameObject.activeSelf; } }
        /// <summary>`Trail` 上的 `ParticleSystem` 在播没有（自检用；没有节点 ⇒ false）。</summary>
        public bool TrailPlaying
        {
            get
            {
                var ps = _trail != null ? _trail.GetComponent<ParticleSystem>() : null;
                return ps != null && ps.isPlaying;
            }
        }

        /// <summary>原版 `CardSetup.c:305-333`：把 `normalSummon` 的实例**挂到 `ToBoardFX` 下、
        /// 改名 `Spawn Particles`、先关掉**。
        /// <para>`instance` = 由调用方实例化好的那件（`WarpforgeEffectPlayer.Play` 的产物）——
        /// `localPosition` 由调用方传 **prefab 自己的值**（原版 `set_localPosition(prefab 自身)`）。
        /// 同时给 `Trail` 补上 <see cref="ParticleEmitOnEnable"/>（我们导入的 prefab 里没有那个组件）。</para>
        /// <returns>挂上了没有。</returns></summary>
        public bool MountSpawnParticles(GameObject instance)
        {
            if (instance == null) return false;
            var anchor = ToBoardFxAnchor;
            instance.transform.SetParent(anchor, false);
            instance.name = SpawnParticlesNodeName;
            instance.SetActive(false);                 // 原版 `SetActive(false)`，等 clip 在 0.0167 打开
            _spawnParticles = instance;
            var trail = instance.transform.Find("Trail");
            _trail = trail;
            if (trail != null && trail.GetComponent<ParticleEmitOnEnable>() == null)
                trail.gameObject.AddComponent<ParticleEmitOnEnable>();
            return true;
        }

        /// <summary>clip 的 `ToBoardFX/Spawn Particles.m_IsActive`（0→1 @0.0167）。</summary>
        public void SetSpawnParticlesActive(bool on)
        {
            if (_spawnParticles != null) _spawnParticles.SetActive(on);
        }

        /// <summary>clip 的 `ToBoardFX/Spawn Particles/Trail.m_IsActive`（0→1 @0.55）。</summary>
        public void SetTrailActive(bool on)
        {
            if (_trail != null) _trail.gameObject.SetActive(on);
        }


        static Material _frameDissolveProto;
        /// <summary>卡框溶解材质的**模板**（原版 shader 原件；取不到 ⇒ null，调用方如实退回并出声）。</summary>
        public static Material FrameDissolveMaterialProto()
        {
            if (_frameDissolveProto != null) return _frameDissolveProto;
            if (!WarpforgeVFX.WarpforgeShaderMap.TryResolve(FrameDissolveShader, out var sh, out var src) || sh == null)
            {
                Debug.LogWarning($"[CardView] 解析不到原版 shader `{FrameDissolveShader}` ⇒ **2D 卡框溶解不可用**"
                               + "（登场 clip 的 `floatProperty1` 那一段不演，卡框照常显示）。见 `WarpforgeShaderMap`");
                return null;
            }
            var m = new Material(sh) { name = "CardFrameDissolve" };
            SetMatF(m, "_DISSOLVE_CHANNEL", FrameDissolveChannel);
            if (m.HasProperty(DissolveKeywordB)) m.EnableKeyword(DissolveKeywordB);
            SetMatF(m, FrameDissolveBorderWidthProp, FrameDissolveBorderWidth);
            SetMatV(m, FrameDissolveUVProp, FrameDissolveUVScale);
            SetMatV(m, "_Color", FrameDissolveColor);
            SetMatF(m, FrameDissolveMultiplyEdgeProp, 0f);
            SetMatF(m, "_DissolveAmount", 1f);
            _frameDissolveProto = m;
            Debug.Log($"[CardView] 卡框溶解材质：`{FrameDissolveShader}` ← {src}");
            return _frameDissolveProto;
        }

        /// <summary>这张卡自己的卡框溶解材质（每张一份 —— `_MainTex` 是逐卡的卡框图）。取不到 ⇒ null。</summary>
        Material FrameDissolveMaterial()
        {
            if (_frameDissolveTried) return _frameDissolveMat;
            _frameDissolveTried = true;
            if (_frame == null || _frame.sharedMaterial == null) return null;
            var proto = FrameDissolveMaterialProto();
            if (proto == null) return null;
            _frameDissolveMat = new Material(proto) { name = "CardFrameDissolve" };
            var tex = _frame.sharedMaterial.mainTexture;
            if (tex != null && _frameDissolveMat.HasProperty("_MainTex")) _frameDissolveMat.SetTexture("_MainTex", tex);
            var noise = CardArt.CardbackSdfNoise();
            if (noise != null && _frameDissolveMat.HasProperty("_Disolve")) _frameDissolveMat.SetTexture("_Disolve", noise);
            _frameDissolveAmount = 1f;
            _frameDissolveMat.SetFloat("_DissolveAmount", 1f);
            return _frameDissolveMat;
        }

        /// <summary>卡框能不能做溶解（有卡框层 + 取到原版 shader）。</summary>
        public bool FrameDissolveSupported { get { return _frame != null && FrameDissolveMaterial() != null; } }

        /// <summary>现在的卡框材质是不是**溶解那份**（自检断言用）。</summary>
        public bool OnFrameDissolveMaterial
        {
            get { return _frameDissolveMat != null && _frame != null && _frame.sharedMaterial == _frameDissolveMat; }
        }
        /// <summary>当前 `_DissolveAmount`（没上过溶解材质时 **−1**）。</summary>
        public float FrameDissolveAmount { get { return _frameDissolveAmount; } }
        /// <summary>这张卡的卡框溶解材质（自检断言用；没建过 = null）。</summary>
        public Material FrameDissolveMatView { get { return FrameDissolveMaterial(); } }
        /// <summary>卡框层现在挂的材质（自检断言用）。</summary>
        public Material FrameMatView { get { return _frame != null ? _frame.sharedMaterial : null; } }

        /// <summary>原版 `AnimateUIMaterialProperty.OnEnable`：**记住原材质**，把卡框换成溶解实例。
        /// 返回 false = 做不了（没有卡框层 / 取不到 shader）—— 调用方出声，别静默当成功。</summary>
        public bool BeginFrameDissolve()
        {
            var m = FrameDissolveMaterial();
            if (_frame == null || m == null) return false;
            if (_frame.sharedMaterial != m)
            {
                _frameMatBeforeDissolve = _frame.sharedMaterial;
                _frame.sharedMaterial = m;
            }
            SetFrameDissolveAmount(1f);
            return true;
        }

        /// <summary>喂 `_DissolveAmount`（0 = 框还在 · 1 = 框溶没了）。没上溶解材质时空操作。</summary>
        public void SetFrameDissolveAmount(float a)
        {
            if (_frameDissolveMat == null) return;
            _frameDissolveAmount = Mathf.Clamp01(a);
            _frameDissolveMat.SetFloat("_DissolveAmount", _frameDissolveAmount);
        }

        /// <summary>原版 `OnDisable` + `OnDestroy`：**还原原材质**，再销毁实例。
        /// ⚠️ 与「灰度控制器」那条路各管各的：我们这条只还原**自己换上去的那一份**，
        /// 不碰别人（同族坑见报告 §⑦4）。</summary>
        public void EndFrameDissolve()
        {
            if (_frame != null && _frameDissolveMat != null && _frame.sharedMaterial == _frameDissolveMat)
                _frame.sharedMaterial = _frameMatBeforeDissolve != null ? _frameMatBeforeDissolve : _frame.sharedMaterial;
            _frameDissolveAmount = -1f;
            if (_frameDissolveMat != null)
            {
                if (Application.isPlaying) Object.Destroy(_frameDissolveMat); else Object.DestroyImmediate(_frameDissolveMat);
                _frameDissolveMat = null;
                _frameDissolveTried = false;      // 还能再建（同一张卡可以多次登场/回手）
            }
        }

        /// <summary>**立绘层**的局部 y 偏移（原版 `CardImage.m_LocalPosition.y = −0.065`）。
        /// 只动 `_art`（底层完整插图）；`_artFront` 是大图档专用、不进本动作。</summary>
        public void SetArtLocalY(float y)
        {
            _artLocalY = y;
            ApplyArtTransform();
        }

        /// <summary>**立绘层**的局部缩放（原版 `CardImage` 的 ScaleCurves 终点 `0.9049578 @0.55`）。</summary>
        public void SetArtLocalScale(float s)
        {
            _artLocalScale = Mathf.Max(1e-4f, s);
            ApplyArtTransform();
        }

        /// <summary>立绘层现在记着的局部 y / 缩放（自检断言用）。</summary>
        public float ArtLocalY { get { return _artLocalY; } }
        public float ArtLocalScale { get { return _artLocalScale; } }

        /// <summary>`2DCard` 那一级（我们的 `FaceRoot`）的局部 z —— 原版 clip 的 `2DCard.m_LocalPosition.z`
        /// 从 0 走到 **−0.043 @0.1667**。⚠️ 只动 z，不动 x/y。</summary>
        public void SetFaceRootLocalZ(float z)
        {
            if (FaceRoot == null) return;
            var p = FaceRoot.localPosition;
            p.z = z;
            FaceRoot.localPosition = p;
        }

        /// <summary>`2DCard` 那一级现在的局部 z（自检断言用）。</summary>
        public float FaceRootLocalZ { get { return FaceRoot != null ? FaceRoot.localPosition.z : float.NaN; } }

        float _artLocalY;
        float _artLocalScale = 1f;
        void ApplyArtTransform()
        {
            if (_art == null) return;
            var p = _art.transform.localPosition;
            p.y = _artLocalY;
            _art.transform.localPosition = p;
            _art.transform.localScale = Vector3.one * _artLocalScale;
        }

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

        // ==================================================================
        //  🔴 2026-10-22（W64 · `B08`）原版溶解材质的**序列化值**（逐项实读，一个都不许再"近似"）
        // ==================================================================
        //  出处（一手）：`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/
        //    Material/Material_-6651017123104198842.json`
        //    （`m_Name = "Card 3d WH40K Dissolve"` · `m_Shader.m_PathID = -8335403570401031012` ·
        //      `m_ValidKeywords = ["_DISSOLVE_CHANNEL_B"]` · `m_InvalidKeywords = ["_EMISSION"]`）。
        //  ⚠️ 它和**正常体**那份材质（`Card 3d Lvl1`）**不是同一批数**：
        //    `_BorderWidth` 这里是 **0.23**、正常体是 **0.03**；`_CountersIntensity` 这里是 **2.0**、
        //    正常体是 **1.12** ⇒ ⛔ 别拿"从当前材质抄过来"当原版（那正是本轮改掉的那个近似）。
        //  两份**相同**的只有 `_MatCap_Intensity 1.69` / `_MatCapPower 1.24`（抄不抄都一样，仍照抄）。
        public const float DissolveBorderWidth = 0.23f;        // `_BorderWidth`
        public const float DissolveCountersIntensity = 2.0f;   // `_CountersIntensity`
        public const float DissolveAlphaClipThreshold = 0.145f;// `_AlphaClipThreshold`
        public const float DissolveChannel = 2f;               // `_DISSOLVE_CHANNEL`
        public const string DissolveKeywordB = "_DISSOLVE_CHANNEL_B";
        /// <summary>`_BorderColor` = (0.71408665, 1.91458, 2.6390157, 0) —— 原版序列化原值（HDR 过 1）。</summary>
        public static readonly Vector4 DissolveBorderColor = new Vector4(0.71408665f, 1.91457999f, 2.63901567f, 0f);
        /// <summary>`_BorderColor2` = (0, 0.02859164, 0.57647061, 0)</summary>
        public static readonly Vector4 DissolveBorderColor2 = new Vector4(0f, 0.02859164f, 0.57647061f, 0f);
        /// <summary>`_ClampRange` = (0.82, 1, 0.18, 0.01)</summary>
        public static readonly Vector4 DissolveClampRange = new Vector4(0.82f, 1f, 0.18f, 0.01f);
        /// <summary>`_DissolveUVScale_XY_Offset_ZW` = (3, 3, 0, 0)</summary>
        public static readonly Vector4 DissolveUVScale = new Vector4(3f, 3f, 0f, 0f);

        // 🔴 **2026-10-22（`CardBootDemo` 三条红之一 → 现核）：下面三个是【死值】，⛔ 不设、也别加回来。**
        //   判据 = **shader 属性表**（`bundle_battleprefabs_vfxandmisc_assets_all/Shader/
        //   Shader_-8335403570401031012.json` 的 `m_ParsedForm.m_PropInfo.m_Props`，23 个名字逐项读过）：
        //   `_BaseMap _CardImage _ClampRange _CountersIntensity _EnvironmentColor _MatCap _MatCap_Intensity
        //    _MatCapPower _SCREENSPACEUV _AlphaClipThreshold _DissolveTex _DissolveAmount _BorderColor
        //    _BorderColor2 _DissolveUVScale_XY_Offset_ZW _BorderWidth _var3DCardColor_*（两条）_QueueOffset
        //    _QueueControl unity_Lightmaps unity_LightmapsInd unity_ShadowMasks _USE_BLEND _DISSOLVE_CHANNEL`
        //   —— **没有** `_BorderWidth2` / `_Borderwidth` / `_SmoothStep`。
        //   它们在**材质 JSON 的 `m_SavedProperties` 里**（属 Standard shader 的残留），但 shader 不认
        //   ⇒ 与 `_SrcBlend/_DstBlend/_ZWrite/_Surface/_Cull/_Cutoff` 同一族 = **死值**（CLAUDE.md §三）。
        //   原来那条断言要求它们等于 0.11/0.12/(0.32,0.24) —— 那是**拿材质序列化冒充 shader 参数**，已改。

        /// <summary>原版**材质**里 `_BorderWidth2` 的序列化值 —— ⚠️ **死值**（shader 属性表里没有这个名），
        /// 只作与材质 JSON 逐项对照用，⛔ 别拿它设材质。</summary>
        public const float DissolveBorderWidth2SerializedDead = 0.11f;
        /// <summary>同上，`_Borderwidth`（原版**大小写两个都在材质里**，但 shader 只认 `_BorderWidth`）。</summary>
        public const float DissolveBorderwidthSerializedDead = 0.12f;
        /// <summary>同上，`_SmoothStep` = (0.32, 0.24, 0, 0) —— **死值**（shader 属性表里没有）。</summary>
        public static readonly Vector4 DissolveSmoothStepSerializedDead = new Vector4(0.32f, 0.24f, 0f, 0f);

        /// <summary>这张卡的溶解材质（**每张一份** —— `_CardImage` 是逐卡的立绘）。
        /// 取不到就返回 null（调用方如实退回）。</summary>
        Material DissolveMaterial()
        {
            if (_dissolveTried) return _dissolveMat;
            _dissolveTried = true;
            if (_body3D == null) return null;
            var proto = DissolveMaterialProto();
            if (proto == null) return null;

            _dissolveMat = new Material(proto) { name = "Card3DDissolve" };
            // ① 逐卡那几张图 / 两份材质同值的强度：从正常体材质抄（`_BaseMap` / `_MatCap` / `_CardImage` /
            //    `_MatCap_Intensity 1.69` / `_MatCapPower 1.24`）。
            var cur = _body3D.sharedMaterial;
            if (cur != null)
            {
                CopyTex(cur, _dissolveMat, "_BaseMap");
                CopyTex(cur, _dissolveMat, "_MatCap");
                CopyTex(cur, _dissolveMat, "_CardImage");
                CopyF(cur, _dissolveMat, "_MatCap_Intensity");
                CopyF(cur, _dissolveMat, "_MatCapPower");
            }
            // ② 🔴 原版**溶解材质自己那几项**（判据 = 上面那串常量）。**必须放在抄之后** ——
            //    `_CountersIntensity` 两份材质不同值，先抄后盖正是为了让它落回原版的 **2.0**。
            SetMatF(_dissolveMat, "_BorderWidth", DissolveBorderWidth);
            // ⛔ `_BorderWidth2` / `_Borderwidth` **不设** —— shader 属性表里没有这两个名字（死值）。
            SetMatF(_dissolveMat, "_CountersIntensity", DissolveCountersIntensity);
            SetMatF(_dissolveMat, "_AlphaClipThreshold", DissolveAlphaClipThreshold);
            SetMatF(_dissolveMat, "_DISSOLVE_CHANNEL", DissolveChannel);
            SetMatV(_dissolveMat, "_BorderColor", DissolveBorderColor);
            SetMatV(_dissolveMat, "_BorderColor2", DissolveBorderColor2);
            SetMatV(_dissolveMat, "_ClampRange", DissolveClampRange);
            SetMatV(_dissolveMat, "_DissolveUVScale_XY_Offset_ZW", DissolveUVScale);
            // ⛔ `_SmoothStep` **不设** —— 同上，shader 属性表里没有（死值）。
            if (_dissolveMat.HasProperty(DissolveKeywordB)) _dissolveMat.EnableKeyword(DissolveKeywordB);
            // ③ `_DissolveTex` = 原版那张 **`Noise Combined`**（pathID `−8214795764591836323`，在
            //    `bundle_duplicateassetisolation_assets_all`）。🔴 **2026-10-22（`B08`/`D6`）订正铁律 5**：
            //    本行原来写「原版那张噪声图**不在我们工程里**（本仓 0 命中）」—— **错的**，它就是
            //    `CardArt.CardbackSdfNoise()`（卡背 SDF 的 `_Noise`，同一个 pathID，早就导入并在用）。
            //    错因：当年按文件名 `DissolveNoise` 找 ⇒ 那张图在工程里叫 `card_sdf/cardback_noise`。
            var noise = CardArt.CardbackSdfNoise();
            if (noise != null)
            {
                if (_dissolveMat.HasProperty("_DissolveTex")) _dissolveMat.SetTexture("_DissolveTex", noise);
            }
            else
            {
                Debug.LogWarning("[CardView] 取不到原版 `Noise Combined`（`Art/card_sdf/cardback_noise`）"
                               + " ⇒ `_DissolveTex` 落回 shader 默认值（**与原版不同**，不静默）");
            }
            _dissolveMat.SetFloat("_DissolveAmount", 0f);
            _dissolveAmount = 0f;
            return _dissolveMat;
        }

        /// <summary>写四分量/标量材质属性（`HasProperty` 判过才写，缺了就跳过 —— 不同 shader 变体的属性表不一样）。</summary>
        static void SetMatF(Material m, string prop, float v)
        {
            if (m != null && m.HasProperty(prop)) m.SetFloat(prop, v);
        }
        /// <summary>同上，四分量。⚠️ 用 `SetVector` 而不是 `SetColor`：原版序列化值里有 >1 的分量
        /// （`_BorderColor` 的 1.91 / 2.63），要**逐位**写进去、不能被任何颜色处理改。.</summary>
        static void SetMatV(Material m, string prop, Vector4 v)
        {
            if (m != null && m.HasProperty(prop)) m.SetVector(prop, v);
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

        /// <summary>🆕 2026-10-22（`B08`）**这张卡的溶解材质本身**（自检断言用 —— 那 10 项序列化值
        /// 与 `_DissolveTex` 只有从这里读得到）。没建过/建不出来 ⇒ `null`。</summary>
        public Material DissolveMatView { get { return DissolveMaterial(); } }

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

        /// <summary>换回正常那张卡体材质（原版 `DelayedResetMaterial` 那一步 —— 它等价于
        /// `SetCardMaterial(defaultMaterial, alsoSetCardImage: true)`：**无条件**换回 + 重喂卡图）。
        ///
        /// <para>🔴 **2026-10-23（D5）就地订正（铁律 5）—— 早退条件原来**判反了**。**
        /// 原来写的是 `if (_body3D == null || _body3D.sharedMaterial == _dissolveMat) return;`，
        /// 含义变成「**正好在**溶解材质上就**不**还原」⇒ 任何 `BeginDissolve()` 之后接
        /// `EndDissolve()` 的调用点都会**永远留在溶解材质上**（卡体一直是溶解态）。
        /// 判据（反编译逐句实读）：`BattleCardUI__RestoreOriginalMaterial.c` 整段只有
        /// `Renderer.SetMaterial(+0x2c8 缓存的原件)` + `SetCardImageTo3DBase()`，
        /// **没有任何「当前是不是溶解材质」的分支**；`DelayedResetMaterial` 那条协程的收尾
        /// （`BattleCardUI._DelayedResetMaterial_d__121__MoveNext.c`）也是无条件
        /// `SetCardMaterial(defaultMaterial, true)`。⇒ 这里**只保留 null 守卫**。</para>
        ///
        /// <para>还原动作**转调** <see cref="RestoreOriginalMaterial"/>（它就是原版同名方法的对应物，
        /// 判据只留一处，⛔ 别在这儿再写第二份「换回 `_bodyMat` + 喂卡图」）。</para>
        ///
        /// <para>⚠️ 本方法当前**没有调用点**（2026-10-23 全仓 `grep EndDissolve` 只命中定义）
        /// ⇒ 这条是**潜伏缺陷**（来自 A4 的审查）。修它是给下一个调用点铺路：
        /// 少这一修，`BeginDissolve → EndDissolve` 这条路会静默停在溶解材质上。</para></summary>
        public void EndDissolve()
        {
            if (_body3D == null) return;
            RestoreOriginalMaterial();
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
            // 🔴 **2026-10-14（`A1863`）：面朝下那一档喂的是「伏击图」，不是这张卡自己的立绘。**
            //    原版 `BattleCardUI__SetCardImageTo3DBase.c` 的**开头就是这道闸**（逐行读过）：
            //      `if (HasCurrentTrait(card, ambush /*0x4B0*/)) tex = CardTraitCollection.GetTrait(ambush).CustomObject;
            //       else                                    tex = rawCard.cardSprite.texture;`
            //      → `renderer(+0x180 = 卡 3D 体).GetMaterial().SetTexture(<_CardImage>, tex)`。
            //    我们这边的对应物 = `UnitState.FaceDown`（= 「身上还挂着伏击词条」那件事，
            //    见 `RuleEngine/Core/RuleCore.cs` 的 `PlaysFaceDown` / `u.FaceDown = false`）。
            //    ⚠️ 这道闸**判据只此一处**：`SetData` / `SetCardMaterial` / `RestoreOriginalMaterial`
            //      / `SetAmbush` 四条路最后都走到这里，⛔ 别在调用点各写一份。
            //    ⚠️ 伏击图**取不到**时退成这张卡自己的立绘（**绝不喂 null** —— 喂 null 会让 3D 卡体
            //      整个没有 `_CardImage`，画面上是一张空壳；那条路已经由 `FaceDownArtTexture()` 出声）。
            var tex = ArtTexture(Data);
            if (_faceDown) { var amb = FaceDownArtTexture(); if (amb != null) tex = amb; }
            _body3D.sharedMaterial.SetTexture("_CardImage", tex);
            // 🆕 2026-10-25（B2）：立绘 UV 窗**按这张贴图是哪种来源**给（sprite 裁块 / 整页方图）。
            //   原版只采「整页上 UV1 那一窗」，而我们的 `art_*.png` 有两种来源（672 裁块 + 496 整页）
            //   ⇒ 同一组常量盖不住两种。判据与算式**只写在 `CardArt.ArtUvRect`**（⛔ 别在这儿再写一份）。
            //   ⚠️ 放在这一处 = 四条路（`SetData` / `SetCardMaterial` / `RestoreOriginalMaterial` /
            //      `SetAmbush`）都覆盖到，与上面 `_CardImage` 同一口径。
            ApplyArtUvRect(_body3D.sharedMaterial, tex);
        }

        /// <summary>把立绘的 UV 窗写进 `Card3D` 材质的 `_ArtUvRect`（属性不在 ⇒ 说明用的不是我们的 shader，跳过）。
        /// <para>判据/算式唯一出处 = <see cref="CardArt.ArtUvRect"/>。</para></summary>
        static void ApplyArtUvRect(Material m, Texture2D tex)
        {
            if (m == null || !m.HasProperty(ArtUvRectProp)) return;
            m.SetVector(ArtUvRectProp, CardArt.ArtUvRect(tex));
        }

        // ==================================================================
        //  伏击「面朝下」（原版 `CardScript.SetAmbush(card, true)`）
        //  —— 2026-10-14（`A1863`）。在此之前 `CardPresentation` **完全没有**这一档
        //     （`grep FaceDown` 只在 `RuleEngine/**` 命中）⇒ 面上那张牌**从来就是正面**。
        // ==================================================================
        //  判据（**逐行读过**）：`d:/2/tools/decomp_full/CardScript__SetAmbush.c:21-67`
        //  = `param_2 != 0`（**设为**面朝下）那一支：
        //    ① `if (*(char *)(card + 0x40) == '\0') { … }` —— **只有对面那一方才藏**：
        //       · `BattleCardUI.SetAmbushTexts(cardUI, false)`（2D 卡面的名字/说明 → 占位串）
        //       · `BattleCardUI.ToggleStats(false, false)`     ← **藏数值**
        //       · `BasicCardUI.SetImageSprite(伏击图, cardType)`（2D 卡面的图也换成伏击图）
        //       · `BasicCardUI.SetEmptyRarityStars()` · `PlayerManager.ToggleCurrentMana(pm, false, false)`
        //       · `cardUI.effectsAnchor.gameObject.SetActive(false)`（`BattleCardUI +0x1A0` = `effectsAnchor`）
        //    ② 两支**都**走末尾那句 `BattleCardUI.SetCardImageTo3DBase()` ⇒
        //       **3D 卡体的 `_CardImage` 两边都换**（那道闸在方法自己开头，见上面）。
        //    🔴 `isPlayer` 的偏移是**坐实的**（不是猜）：`d:/2/tools/il2cpp_out/dump.cs` 的
        //       `EntityScript` 字段表里 `private bool <isPlayer>k__BackingField; // 0x40`
        //       （`CardScript : EntityScript`，基类字段在前）⇒ `0x40` 就是 `isPlayer`。
        //       ⛔ 别按「面朝下 = 两边都不给看」想当然 —— **自己的那张，主人看得见自己的数值**。
        //    ⚠️ 那张「伏击图」= **伏击词条（`DefinedTrait.ambush` = 1200 = `0x4B0`）的 `customObject` 贴图**。
        //       CardTrait 资产本身**本地没有**（全库 grep `_traitIcon` / `customObject` **0 命中**），
        //       但同一张图有另一条**可查的引用**：原版材质 `Card 3d Dissolve Blend Image Ambush`
        //       的 `_CardImage` —— 实读 = `GenestealerCults_special_Ambush`
        //       （`资料/普查产出_1001/资产导入路三件_侦察.md:258`），而那张贴图**已经在工程里**
        //       （`WarpforgeVFX/Textures/GenestealerCults_special_Ambush.png`，
        //       被 `Prefabs/AmbushEffect.prefab` 的 `WFMatDef.texNames[_CardImage]` 引用）
        //       ⇒ 按 `FaceDownArtTexture()` 那条路取它，⛔ 不另抄一张图、⛔ 不用卡背顶替。

        static Texture2D _faceDownTex;      // 全进程一张（原版也是「伏击词条上那一张图」，与卡无关）
        static bool _faceDownTexTried;
        bool _faceDown;              // 面朝下（原版 = 身上挂着伏击词条）
        bool _ambushHideStats;       // 原版那道 `isPlayer == 0` 闸：**只有对面那一方**才藏数值
        bool _ambushHidden;          // 数值是不是**正被**藏起来（幂等用）
        bool _ambushStashValid, _ambushStashInfo, _ambushStashArmour;

        /// <summary>现在是不是面朝下（自检断言用 —— ⛔ 别拿 `grep FaceDown` 当判据）。</summary>
        public bool FaceDown { get { return _faceDown; } }
        /// <summary>数值现在被藏起来了没有（= 面朝下 **且** 在场上 **且** 是对面那一方）。</summary>
        public bool AmbushHidesStats { get { return _ambushHidden; } }

        /// <summary>面朝下那张图（原版 = 伏击词条的 `customObject`，见上面那段判据）。
        ///
        /// **取法**：从效果库里的 `AmbushEffect` prefab 上那份 `WarpforgeEffectBinder` 里，
        /// 按材质名找 `Card 3d Dissolve Blend Image Ambush`，读它的 `_CardImage` 槽 ——
        /// 那正是原版 `SetCardImageTo3DBase` 会喂给同一个槽的那张图（两边同源）。
        /// ⚠️ **取不到时只报一次警告**，退成这张卡自己的立绘（画面上就是「看着还是正面」）—— **不静默**。
        /// </summary>
        public static Texture2D FaceDownArtTexture()
        {
            if (_faceDownTexTried) return _faceDownTex;
            _faceDownTexTried = true;

            const string kEffect = "AmbushEffect";
            const string kMat    = "Card 3d Dissolve Blend Image Ambush";
            const string kProp   = "_CardImage";

            var lib = WarpforgeVFX.WarpforgeEffectLibrary.Instance;
            WarpforgeVFX.WFEffectEntry entry;
            if (lib != null && lib.TryGet(kEffect, out entry) && entry.prefab != null)
            {
                var binders = entry.prefab.GetComponentsInChildren<WarpforgeVFX.WarpforgeEffectBinder>(true);
                for (int b = 0; b < binders.Length && _faceDownTex == null; b++)
                {
                    var defs = binders[b] != null ? binders[b].materials : null;
                    if (defs == null) continue;
                    for (int i = 0; i < defs.Length; i++)
                    {
                        var d = defs[i];
                        if (d == null || d.name != kMat || d.texNames == null || d.texVals == null) continue;
                        for (int t = 0; t < d.texNames.Length && t < d.texVals.Length; t++)
                            if (d.texNames[t] == kProp) { _faceDownTex = d.texVals[t] as Texture2D; break; }
                        break;
                    }
                }
            }

            if (_faceDownTex == null)
                Debug.LogWarning("[CardView] 🔴 伏击「面朝下」那张图取不到（`" + kEffect + "` → `" + kMat
                    + "` → `" + kProp + "`）—— 面朝下的单位会**照旧画成正面**。"
                    + "修法：确认效果库里有 `AmbushEffect`，且 `EffectExporter` 导过它"
                    + "（那张图就在它 binder 的 `WFMatDef.texNames[_CardImage]` 里）。"
                    + "这条警告只报一次。");
            return _faceDownTex;
        }

        /// <summary>伏击「面朝下」（原版 `CardScript.SetAmbush(card, true)`）。
        /// <paramref name="hideStats"/> = 原版那道 `isPlayer == 0` 闸 ——
        /// **只有【对面】那些面朝下的单位才藏数值**（自己那张，主人本来就知道它是什么）。
        ///
        /// 🔴 **每个出生 / 迁移入口都要设一次**（CLAUDE.md 铁律 10 第 5 条）：本方法把状态**存下来**，
        /// 并在 `SetData` / `SetFace` **末尾各重放一次**（`ApplyAmbush`）⇒ 视图重建、形态切换、
        /// 每刷一次棋盘都不会把它忘掉。调用点 = `BattleDriver.SyncBoard`（棋盘视图唯一的收口处）。</summary>
        public void SetAmbush(bool faceDown, bool hideStats)
        {
            _faceDown = faceDown;
            _ambushHideStats = faceDown && hideStats;
            if (_body3D != null) SetCardImageTo3DBase();   // 原版那一支末尾就是它
            ApplyAmbush();
        }

        /// <summary>把「面朝下」那一档的画法落到层上。
        /// 本层只管**藏数值**那一半（原版 `BattleCardUI.ToggleStats(false, false)`）；
        /// **3D 卡体那张图**由 `SetCardImageTo3DBase` 管（判据只在那儿一份）。
        ///
        /// ⚠️ **原版藏的是 2D 卡面那一套**（文字 / 图 / 稀有度星 / 费用 / `effectsAnchor`）——
        /// 场上（`CardFace.Board`）那套 2D 卡面**本来就没建**（`SetFace` 里全 `!board` 关掉了）
        /// ⇒ 我们场上要藏的就是**数值**：`ToggleStats` 的对应物是四个数值层 + `_info` + 护甲盾。
        /// 徽标**不藏**（原版那一支没碰 `boardTraitIcons` —— 伏击这个词条图标本来就该看得见）。</summary>
        void ApplyAmbush()
        {
            // ① **2D 立绘那一层**（原版 `BasicCardUI.SetImageSprite(伏击图, cardType)`）——
            //    场上正常走 3D 卡体、这层是关的；**只有取不到 3D 卡体**那一档它才顶上来
            //    （`SetFace` 的 `if (_body3D != null) Show(_art, !board);`）。
            //    ⚠️ 放在**最前面**：`SetData` 每次都把 `_art` 刷回自己的立绘，这里必须压过它。
            if (_art != null && _art.sharedMaterial != null)
            {
                var t2d = ArtTexture(Data);
                if (_faceDown) { var amb2 = FaceDownArtTexture(); if (amb2 != null) t2d = amb2; }
                _art.sharedMaterial.mainTexture = t2d;
            }

            bool hide = _ambushHideStats && _faceMode == CardFace.Board;
            if (hide)
            {
                if (!_ambushHidden)
                {
                    _ambushStashValid = true;
                    _ambushStashInfo = _info != null && _info.gameObject.activeSelf;
                    _ambushStashArmour = _armourIcon != null && _armourIcon.gameObject.activeSelf;
                    _ambushHidden = true;
                }
                for (int s = StatMelee; s <= StatHealth; s++)
                    if (_stats[s] != null) _stats[s].gameObject.SetActive(false);
                Show(_info, false);
                Show(_armourIcon, false);
                return;
            }
            if (!_ambushHidden) return;              // 没藏过 ⇒ 什么都不动（别把正常态刷掉）
            _ambushHidden = false;
            if (_ambushStashValid)
            {
                Show(_info, _ambushStashInfo);
                Show(_armourIcon, _ambushStashArmour);
            }
            else { Show(_info, true); Show(_armourIcon, true); }
            // 四个数值层的显隐由它按数据重算（⛔ 别在这儿自己判一遍 —— 那是第二份判据）
            SyncStatLayers();
        }

        /// <summary>这张卡的立绘贴图（原版 `actingCard.rawCard.cardSprite.texture`）——
        /// 给 `WFModuleChangeMaterial.CardTexture` 那个钩子用。</summary>
        public Texture CardImageTexture { get { return ArtTexture(Data); } }

        /// <summary>现在的 3D 卡体材质是不是建体时那份（自检断言用）。</summary>
        /// <summary>🆕 2026-10-25（`E`）自检用：**3D 卡体材质上的 `_MatCap`**（没建 3D 体 / 没材质 ⇒ `null`）。
        /// 判「matcap 是不是按档位取的」只能用这个口 —— 读贴图名/引用，⛔ 别读我们自己记的档位变量（那是自证）。</summary>
        public Texture2D BodyMatcap
        {
            get { return _bodyMat != null ? _bodyMat.GetTexture("_MatCap") as Texture2D : null; }
        }

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

        // ---- 残骸体的动画 / 闲置粒子 / 卡面材质（🆕 2026-10-22 · P5 §⑦ 那两个缺口的等价接线）----
        //  判据全是**反编译方法体**逐行读的（不是猜的）：
        //   · 出生：`RemnantNecrons__Toggle.c` 末尾那句 `Animation.Play(this.toRemnantAnimationName)`；
        //     灵族同一条在**它的 override** 里（`RemnantAeldari$$Toggle` @ RVA `0x640200`，
        //     `withAnimation && option` 那一支：`BodyVisibilityToggle(false)` → `Animation.Play(+0x30)`）。
        //     ⚠️ `decomp_full/RemnantAeldari__Toggle.c` 那个文件是**私有重载** `Toggle(bool)`（两份重载同名、
        //        产物互相覆盖）—— 别拿它当「灵族不播动画」的证据。
        //   · 撤场：死灵 `Toggle(false, withAnimation: true)` = **保持可见** + `ParticleSystem.Stop(+0x70)`
        //     + `Animation.Play(reanimateAnimationName)`；演完由片段里的动画事件
        //     `AnimationResurrectionEnd`（**1.8 s**）收尾。灵族**没有** `From Remnant` 片段 ⇒ 立即撤。
        ParticleSystem _remnantIdlePs;          // 原版 `RemnantNecrons.remnantIdleParticleSystem`（GO `Particles Death Idle`）
        float _remnantHideAt = -1f;             // >= 0 = 正在演 `From Remnant`，到这一刻藏（秒）
        float _remnantHideClock;

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

        /// <summary>绿光**要不要亮**（调用方最近一次给的请求）。判据 / 为什么必须记下来
        /// → <see cref="SetCanAct"/> 的注释（非场上形态时那次请求**不能丢**）。</summary>
        bool _canActWanted;

        // ==================================================================
        //  🆕 2026-10-16 **卡背那一层**（抽我方牌翻面的前半段看的就是它）
        // ==================================================================
        //  判据：原版 `CardScript.TurnCardAround` 绕 Y 转 180°（**两段各 90°**，`.rdata 0x1834b2e04`
        //  实读 90.0、每段占抽牌时长 0.25），**只在我方那一支**（`CardScript__DrawCard.c:52-59` 判 `isPlayer`）
        //  ⇒ 前 90° 看到的是**卡背**、后 90° 才是卡面。
        //  我们是 2D，「绕 Y 侧立」在屏幕上就是**宽度收到 0** ⇒ 等价物 = **前半段盖一张卡背，
        //  在两段之间的「侧立」那一刻换回卡面**（那一刻宽度为 0，换面看不见）。
        //  ⚠️ 尺寸取**卡身** `Width × Height` —— 原版卡背是同一条网格上的另一面、与卡面同尺寸；
        //     **别拿牌堆那个 `Cardback` 节点的框**（它比卡身宽 3.88%、矮 5.85%，那是牌堆自己的）。

        /// <summary>卡背那一层（懒建、建好留着）。**不进 `_layers`** —— 那是「整卡着色」名单
        /// （高亮/置灰/淡出都会遍历它），而卡背是「翻过去看到的那一面」，颜色由它自己说了算；
        /// 但它**要跟 alpha**（否则抽牌淡入时卡背长亮、与卡面不同步），那一档补在 `ApplyTint` 末尾。
        /// 同族样板 = `_remnantBody` / `_canAct`。</summary>
        GameObject _backLayer;
        MeshRenderer _backMr;

        /// <summary>卡背层现在亮着没有（自检用）。</summary>
        public bool BackVisible { get { return _backLayer != null && _backLayer.activeSelf; } }
        /// <summary>卡背层的渲染队列（自检用；`-1` = 还没建）。</summary>
        public int BackQueue { get { return (_backMr != null && _backMr.sharedMaterial != null) ? _backMr.sharedMaterial.renderQueue : -1; } }

        /// <summary>卡背那一层的 z —— **必须比所有卡面层更靠近相机**（相机在 −Z ⇒ 更负 = 更前）。
        /// 现有卡面层最前的是 `BoardInfoZ = −0.150`（见上面那一串 z 常量）。</summary>
        public const float CardBackZ = -0.20f;

        /// <summary>把**卡背**亮/灭。`back == null` 且要亮 ⇒ **出声并什么都不做**
        /// （⛔ 不静默造一个白块）。调用点只有一处：`CardFeel.DealInPose`（翻面的前半段）。</summary>
        public void SetBackVisible(bool on, Texture2D back)
        {
            if (!on)
            {
                if (_backLayer != null && _backLayer.activeSelf) _backLayer.SetActive(false);
                return;
            }
            if (back == null)
            {
                Debug.LogWarning("[CardView] 要显示卡背但没给贴图 ⇒ 不显示（⛔ 不静默造一个白块）");
                return;
            }
            if (_backLayer == null)
            {
                _backLayer = new GameObject("cardBack");
                // ⚠️ 挂**卡根**（不是 `FaceRoot`）：卡背是这张卡的另一面，不该跟着卡面的倾摆走。
                _backLayer.transform.SetParent(transform, false);
                _backLayer.transform.localPosition = new Vector3(0f, 0f, CardBackZ);
                // 🔴🔴 **2026-10-17（W5 · BSC62 `136547`）：这里原来走 `CenteredQuad("cardBack", Width, Height, back)`
                //   —— 它**按贴图自身比例内接**（见 `CenteredQuad`:1823-1848），而这张卡背贴图比 ≈0.752 ≠ 卡身比
                //   `Width/Height` ≈ 0.628 ⇒ 网格**高**被压到 `Width / 0.752` = **2.782**（比卡矮 16.5%），
                //   于是翻面前半段那张卡背比卡面矮一截。卡背是**这张卡的另一面**（原版 = 同一条网格的另一面、
                //   与卡面**同尺寸**，见上面 :3503-3504）⇒ 尺寸必须取**卡身** `Width × Height`，
                //   不能用贴图比例去内接。改走 `Quad()`（= **卡大小的居中 quad**，UV 拉满 0..1 ⇒ 贴图铺满整张卡）。
                //   判据：`Editor/BattleScene.cs:13056-13061`（断 `sharedMesh.bounds.size` = `Width × Height`）。
                //   ⛔ 别改贴图本身（`CardArt.CardBack(...)` 那张仍是原样）。
                _backLayer.AddComponent<MeshFilter>().sharedMesh = Quad();
                _backMr = _backLayer.AddComponent<MeshRenderer>();
                _backMr.sharedMaterial = new Material(BaseMaterial());
                _backMr.sharedMaterial.mainTexture = back;
                _backMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _backMr.receiveShadows = false;
                StampQueue(_backMr);
                _backLayer.SetActive(false);
            }
            _backMr.sharedMaterial.mainTexture = back;
            _backLayer.SetActive(true);
        }

        /// <summary>绿光亮着没有（自检用）。</summary>
        public bool CanActVisible { get { return _canAct != null && _canAct.activeSelf; } }

        /// <summary>「未行动」绿光亮 / 灭。判据在**调用方**（`BattleDriver.SyncBoard` 用
        /// `RuleCore.CanActNow` —— 那是唯一判据，本层只管画）。
        /// <para>🔴 2026-10-22 **修「出牌后绿光整回合不亮」**：原来这个方法在**非场上形态直接 `return`、
        /// 不留痕** ⇒ 那一次请求就丢了。而形态要到登场 clip **0.7 s** 才切成 `Board`
        /// （`CardFeel.cs:1351-1357`，真机拖拽那档），`SyncBoard` **又不是逐帧跑**
        /// ⇒ 出牌那一刻发下来的 `SetCanAct(true)` 落空，要等**下一次** refresh 才补上
        /// （真机日志：turn 5 `built=0` → turn 7 `built=1`；战将 `built=1` ⇒ 不是 `BuildCanAct` 失败）。
        /// 现在**只记请求**（<see cref="_canActWanted"/>），形态切到场上时由 `SetFace` 末尾那把
        /// `ApplyCanAct()` **当场**补上 —— ⛔ 别再退回「等下一次 refresh 才补」。</para></summary>
        public void SetCanAct(bool on)
        {
            _canActWanted = on;
            ApplyCanAct();
        }

        /// <summary>按 <see cref="_canActWanted"/> 把绿光落到实际那棵树上 —— **原 `SetCanAct` 的方法体**，
        /// 逻辑一个字没改，只是把形参换成了字段。「请求与落地分开」的两个入口：
        /// ① 调用方每次同步（`SetCanAct`）；② **换形态**（`SetFace` 末尾，铁律 10 第 5 条）。</summary>
        void ApplyCanAct()
        {
            if (!_canActWanted)
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
        static void SetV(Material m, string p, Vector4 v) { if (m.HasProperty(p)) m.SetVector(p, v); }

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

            // ---- 原版 `Sparks UI Additive Scroll` 的两条 ShaderGraph 哈希名属性 ----
            // 判据 = 原版材质 `08_预制体特效/战斗预制体/Material/Material_-5419391226463376386.json`
            //        （`m_Name` = `Sparks UI Additive Scroll`，shader PathID 8443069757019562635 =
            //        `Everguild/FX/Halo UV scroll`）的 `m_SavedProperties/m_Colors` 实读：
            //   · `Vector4_62056e41ff4042358d02be808b0352f9`（property 描述 `UV Scale (XY) Speed (ZW)`）
            //     = **(1, 1, 0.63, 0)**；shader 默认是 (1, 1, **0.2**, 0) ⇒ 不写就慢 **3.15 倍**，
            //     绿光是「亮弧沿环爬」，速度不够看起来就是走一半就停。
            //   · `Vector3_65160a41016e4bb0ad3f5043d258b214`（`Border Fade (X) Clamp(YZ)`）
            //     = **(1, 0, 1, 0)**（正好等于 shader 默认值，仍照抄坐实，别让默认值哪天变）。
            // 两条在 shader 里都是 `m_Type = 1`（Vector），所以用 `SetVector`；同族写法见
            // `Battle/TargetReticle.cs:403-405`。`HasProperty` 守 ⇒ 另一个 `Circle_Hoop Additive`
            // （URP Particles/Unlit，没有这两个属性）不受影响。
            // 报告：`资料/普查产出_1022/W63_场上卡牌与底座绿光_调查.md` §2 根因 1 / §5 第 2 条。
            SetV(m, "Vector4_62056e41ff4042358d02be808b0352f9", new Vector4(1f, 1f, 0.63f, 0f));
            SetV(m, "Vector3_65160a41016e4bb0ad3f5043d258b214", new Vector4(1f, 0f, 1f, 0f));

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
            // 🆕 2026-10-18（`A1674`）：**建出来当场盖一次队列**（同其它层「建完紧跟 StampQueue」的口径）。
            //   判据（原版 prefab 实读，逐字段）：
            //     `ParticleSystemRenderer_605582064029899712.json`：`m_SortingOrder = 0` ·
            //     `m_SortingLayerID = 0` · `m_SortingFudge = −3` · 材质 `Circle_Hoop Additive` 的
            //     `m_CustomRenderQueue = 3000`；`RotatingRing` 那颗是 `m_SortingFudge = −2` ·
            //     材质 `Sparks UI Additive Scroll` 同样 **3000** ⇒ **两档都与卡面同队列**。
            //   Unity 语义：负 `sortingFudge` = 排在同队列其它透明物**之前**（画在它们**后面**）——
            //     原版就是靠它把绿光压在卡面之后的。我们「一张卡一段队列」把卡间隔开，卡内仍由
            //     距离 + 这两个 fudge 排（原版值原样保留）。
            StampQueue(prA);

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
            StampQueue(prB);          // 🆕 `A1674`：理由见上面 `StampQueue(prA)` 那一段（fudge −2）

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
            // 🔴 **2026-10-23（D2 · 铁律 5 订正）：阴影开关照原版打开。**
            //   判据 = 卡 prefab `CardPrefab/Board Elements/3DBody/Card 3D` 的 `MeshRenderer` 实读：
            //   `m_CastShadows = 1`、`m_ReceiveShadows = 1`（另有 `m_DynamicOccludee = 1`）。
            //   原来这里写的是 `Off / false` —— **与判据相反**；当时没有出处。
            //   ⚠️ 开它之前先确认场上真有光源（原版场景自带 `Scenario/Directional Light`）；
            //   没有光的话这两句只是空转，不会画错（不会静默失败）。
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
            var m = new Material(sh) { name = "Card3D" };
            // 材质属性值 = 原版 `Card 3d Lvl1`（`资料/3DBody_原版场上卡体规格.md` §一 那一行）
            m.SetTexture("_BaseMap", CardArt.Card3DBase());
            // 🔴 **2026-10-25（`E` · 代理 D 查实）：matcap 按【卡档位】取，不再写死 tier1。**
            //   判据 = 原版 `CardMaterialHelper.matcapTexturesByCardTier[CardTier]`
            //   （`CardMaterialHelper__GetCardMatCapByCardTier.c`）—— 四个档位贴图**本地全在**
            //   （`CardArt.Card3DMatcap` 的 doc 有 PathID 对位：`MatCap Card Level 1` / `Matcap Bronze` /
            //    `Matcap Silver` / `Matcap Gold`）。档位口径**与卡框同一处**：`CardTierFor(Data)`
            //   （原版也是同源：`SetCardImageTo3DBase` 与 `CardTierUIController.SetTier` 都吃 `CardTier`）。
            m.SetTexture("_MatCap", CardArt.Card3DMatcap(CardTierFor(Data)));
            m.SetTexture("_CardImage", artTex != null ? artTex : Texture2D.whiteTexture);
            // 🆕 2026-10-25（B2）：立绘 UV 窗（sprite 裁块 / 整页方图两支）—— 算式只写在 `CardArt.ArtUvRect`
            ApplyArtUvRect(m, artTex != null ? artTex : Texture2D.whiteTexture);
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

        /// <summary>`CardPresentation/CardSprite`：`Sprites/Default` 的等价物 + **原版去饱和通道**
        /// `_GreyScale`（默认 0 = 与原来逐像素一致）。见 `Resources/Shaders/CardSprite.shader` 头部。
        /// <para>🔴 **2026-10-19（`A2139`）**：原来直接用 `Shader.Find("Sprites/Default")` —— 内置 shader
        /// 没有 `_GreyScale`，卡面就做不了原版那种「去饱和」，只能退成乘法压暗（用户报的那条）。
        /// 找不到自建 shader 时**退回**内置并把「去饱和不会生效」喊出来（不许静默）。</para></summary>
        static Material BaseMaterial()
        {
            if (_quadMat == null)
            {
                var sh = Shader.Find("CardPresentation/CardSprite");
                if (sh == null)
                {
                    Debug.LogWarning("[CardView] 找不到 `CardPresentation/CardSprite` —— 卡面基材退回 `Sprites/Default`，"
                                   + "`SetGrey` 的真去饱和**不会生效**"
                                   + "（`Resources/Shaders/CardSprite.shader` 在不在？）");
                    sh = Shader.Find("Sprites/Default");
                }
                _quadMat = new Material(sh);
            }
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
        /// ⚠️ 认不出这个属性时**出声**（模板退回 `Sprites/Default` 的那条路）—— 静默退灰不生效很隐蔽。
        /// <para>🔴 2026-10-19（`A2139`）：改成实例方法 —— 写完材质要立刻重贴一次 MPB
        /// （整卡灰 = `SetGrey` 会把 MPB 的 `_GreyScale` 设成 max(整卡, 自身)；只改材质会被 MPB 盖住，
        /// 症状是「未激活徽标在灰卡上变彩 / 在彩卡上不灰」）。</para></summary>
        void SetBadgeGrey(MeshRenderer layer, bool active)
        {
            if (layer == null || layer.sharedMaterial == null) return;
            var m = layer.sharedMaterial;
            if (!m.HasProperty("_GreyScale"))
            {
                Debug.LogWarning("[CardView] 徽标材质没有 `_GreyScale` —— 未激活态不会变灰");
                return;
            }
            m.SetFloat("_GreyScale", active ? 0f : 1f);
            ApplyGreyTo(layer);
        }

        /// <summary>徽标底板的底色 —— 原版 `IconBackground` 那个 `SpriteRenderer` 的
        /// `m_Color = (0.32298, 0.34305, 0.35849)`（**深灰**，7 份实例逐份同值）。
        /// 🔴 **别让 `_tint` 刷它**：原版这个颜色是**写死的**（挂在它上面的 `NestedFadeGroup`
        /// 只动 alpha、不改色），而 `ApplyTint()` 的 `_layers` 循环会把它当成普通图层刷成状态色
        /// （平时 = **白**）—— 那正是用户 2026-10-12 看到的「**白色方块**」的直接来源
        /// ⇒ `ApplyTint` 末尾要**单独刷回来**（同 `_textBg`、`_shadowLayer` 那两族）。</summary>
        public static readonly Color BadgePlateInk = new Color(0.32298f, 0.34305f, 0.35849f, 1f);

        /// <summary>把底板刷成原版那个深灰。`alpha` = 卡的整卡淡出（`_alpha`）；
        /// **整组徽标**自己的 `_badgeAlpha` 由 `ApplyBadgeAlpha()` 在这之后**再乘一次** —— 次序别反。</summary>
        static void SetBadgePlateInk(MeshRenderer plate, float alpha)
        {
            if (plate == null || plate.sharedMaterial == null) return;
            if (!plate.sharedMaterial.HasProperty("_Color"))
            {
                Debug.LogWarning("[CardView] 徽标底板的材质没有 `_Color` —— 它会画成原色（偏白），"
                               + "而原版 `IconBackground.m_Color` 是深灰 (0.3230, 0.3431, 0.3585)");
                return;
            }
            plate.sharedMaterial.color = new Color(BadgePlateInk.r, BadgePlateInk.g, BadgePlateInk.b,
                                                   BadgePlateInk.a * alpha);
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
                // 🔴 2026-10-22（`B04` 尾巴）：**卡框溶解材质不吃整卡着色** —— 它的 `_Color` 是
                //   原版材质里那个 **Border Color（HDR 蓝 0.0588/0.3289/0.9059）**，被整卡着色刷成
                //   白/状态色就不是原版了（`AnimateUIMaterialProperty` 那份实例材质在原版也不受着色管）。
                if (r.sharedMaterial == _frameDissolveMat)
                {
                    // 颜色保持原版那个 Border Color（HDR 蓝），**只**让它跟着整卡 α 与 2D 那一档淡 ——
                    // 原版它是 UGUI Image、CanvasGroup 的 α 一样会乘上去。
                    var bc = FrameDissolveColor;
                    r.sharedMaterial.color = new Color(bc.x, bc.y, bc.z, bc.w * _alpha * _alpha2D);
                    continue;
                }
                // 🔴 2026-10-22（`B05`）：3D 卡体**不在** `2DCard` 那棵子树里（原版 `m_Alpha` 是
                //   `2DCard` 的 CanvasGroup）⇒ 它不吃 2D 那一档；其余层各乘一份 `_alpha2D`。
                r.sharedMaterial.color = (r == _body3D)
                    ? c
                    : new Color(c.r, c.g, c.b, c.a * _alpha2D);
            }
            // 🆕 2026-09-29：**2D 立绘那一层再乘上它自己的不透明度**（回手那一幕的「卡面淡回来」，
            //    见 `SetArtAlpha`）。放在整卡循环之后覆盖，理由同下面 `_textBg` 那两段。
            if (_art != null && _art.sharedMaterial != null && _art.sharedMaterial.HasProperty("_Color"))
                _art.sharedMaterial.color = new Color(_tint.r, _tint.g, _tint.b,
                                                      _tint.a * _alpha * _artAlpha * _alpha2D);

            // 🆕 2026-10-16：**卡背那一层【只跟 alpha】、不跟状态色**（整卡着色是给卡面用的）。
            //    它不在 `_layers` 里 ⇒ 上面那个 foreach 刷不到它 ⇒ 在这里单独补一档；
            //    不补的话，抽牌淡入那半秒卡背会**长亮着**（卡面从透明淡进来、卡背却已经是实的）。
            if (_backMr != null && _backMr.sharedMaterial != null && _backMr.sharedMaterial.HasProperty("_Color"))
                _backMr.sharedMaterial.color = new Color(1f, 1f, 1f, _alpha);

            // 🔴🔴 **文字底板不参与整卡着色**（2026-09-19 修，用户报的「白底挡住插图」就是它）。
            //    `_textBg` 也是 `_layers` 里的一员，所以上面那个 foreach 会把它**当成普通图层刷成
            //    `_tint`** —— 而 `_tint` 平时是**白 α1** ⇒ 那块半透明黑底被刷成**不透明白底**，
            //    把下面的立绘整个盖住（对战里手牌/场上卡的下半截就是这样）。
            //    原版这块底板的颜色是**写死的** `m_Color = (0,0,0,0.647)`，**不跟着卡的状态变**。
            //    ⇒ 刷完统一色之后**单独把它刷回去**（只乘卡的整体淡出 `_alpha`，不乘 tint 的 RGB）。
            if (_textBg != null && _textBg.sharedMaterial != null)
                _textBg.sharedMaterial.color = new Color(0f, 0f, 0f, TextBgAlpha * _alpha * _alpha2D);

            // 🔴 **徽标底板同理（2026-10-12 · `A1661`）**：原版 `IconBackground.m_Color` 是**写死的深灰**
            //    （见 `BadgePlateInk`），上面那个 foreach 会把它刷成 `_tint`（平时 = **白**）——
            //    而底板比图标大、位置又贴着卡框左缘，**白底板**在真机上就是用户报的那个白方块。
            //    ⚠️ 位置放在 `ApplyBadgeAlpha()` 之前：那一步会在这个底色上**再乘** `_badgeAlpha`。
            for (int i = 0; i < _badgePlates.Count; i++) SetBadgePlateInk(_badgePlates[i], _alpha);

            // 🔴 **同理（2026-09-19）**：软光/影那层的颜色**是它自己说了算**的
            //    （原版 shader 用的是 `_ShadowColor` / `_Outline`，没有 `_Color`）——
            //    上面那个 foreach 只会给它设一个它没有的 `_Color`（no-op）。
            //    这里只把**卡的整体淡出**跟过去：卡淡出了、影子还实着，一眼就假。
            if (_shadowLayer != null && _shadowLayer.sharedMaterial != null)
            {
                // 🔴 **阴影色走 `ApplyShadowColor()`（按本档取 alpha）** —— 2026-10-15（`A2111`）之前这里
                //    写死用材质默认那个 0.604，放大窗就比原版淡了 0.196（原版显式设 0.8）。
                ApplyShadowColor();
            }
            // 描边（`_Outline`）同理要跟着淡 —— 但它可能是**正在补间**的（`SetOutline`），
            // 那种时候别插手：补间的每一帧自己会乘 `_alpha`。
            // 🔴 2026-10-21（`W61`）/ 2026-10-24（`C1-B03` 并层）：描边走 `OutlineMat`
            //    （现在恒指**软光/影那一层**的材质）。这一块仍然**单独**写 `_Outline` ——
            //    与上面那句 `ApplyShadowColor()` 写的是同一份材质的**另一个属性**，互不覆盖；
            //    留在外面是为了「补间跑着就别动」这条闸不被软光/影那段吞掉。
            {
                bool outlineBusy = _outlineTween != null && _outlineTween.IsActive();
                var om = OutlineMat;
                if (!outlineBusy && om != null && om.HasProperty("_Outline"))
                {
                    var oc = _outlineTarget;
                    oc.a *= _alpha;
                    om.SetColor("_Outline", oc);
                }
            }

            // 🆕 2026-09-25：**卡底那枚软阴影也要跟着整卡淡出**（同「软光/影」那条的理由）——
            // 它是 `SpriteRenderer.color` 说了算（原版 shader 没有 `_Color`），所以走 `SetCardAlpha`。
            if (_blobShadow != null) _blobShadow.SetCardAlpha(_alpha);

            // 🔴 2026-10-19（`A2139`）：TMP 的字**不吃材质 `_GreyScale`**（走顶点色）⇒ 在这里按同一个
            //     `_grey` 去饱和（`Greyed`）。放在最后：整卡状态色 × 淡出算完之后再降饱和，两张口径不打架。
            if (_title != null) _title.color = Greyed((Color)InkName * c);
            if (_keywords != null) _keywords.color = Greyed((Color)InkDesc * c);
            if (_army != null) _army.color = Greyed((Color)InkArmy * c);
            if (_race != null) _race.color = Greyed((Color)InkArmy * c);
            // 🆕 2026-10-18（`A985④`）：「由谁造出来的」也一样跟整卡状态色 × 淡出走
            // （它不在 `_layers` 里，上面那个 foreach 刷不到它 —— 同 `_title` 那一族）。
            if (_createdBy != null) _createdBy.color = Greyed((Color)CreatedByInk * c);
            // 🆕 2026-10-25（`CF-3`）：2D 卡面那四个**数值 TMP** 也一样走顶点色（原版 `m_fontColor` = 白）
            // —— 它们不在 `_layers` 里（TMP 没有 `_Color` 材质属性可刷），同 `_title` 那一族。
            // 🔴 **2026-10-25（`K4`）**：颜色口径收进 `FaceStatInk`（状态色 × 淡出 × 灰化 × **涨落色**）——
            //    原来漏了 `_statFlash`，2D 卡面的数值涨落根本没有颜色可涂。
            for (int i = StatMelee; i <= StatHealth; i++)
                if (_faceStatText[i] != null) _faceStatText[i].color = FaceStatInk(i);
            // 🆕 2026-10-25（K4 D3）：费用那件 TMP 同一条（`StatCost` 的极性在 `StatColorVsBase` 里已反）
            if (_faceCostText != null) _faceCostText.color = FaceStatInk(StatCost);

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
                    _badgeCounters[i].color = Greyed(c);   // A2139：整卡灰时角标字也去饱和（TMP 走顶点色）
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

        // ==================================================================
        //  🆕 2026-10-19（`A2139`）**真去饱和**（原版 `_GreyScale`）
        // ==================================================================
        //
        // **原版判据**（逐句实读）：
        //   · `CollectionCard__UpdateCardVisuals.c:19-20`：
        //       `UIImageGreyscaleController__ToggleGreyScale(controller, canAdd ^ 1, …)`
        //     —— `canAdd` 为 0 时**打开**灰度（`param_2 ^ 1`）。
        //   · `UIImageGreyscaleController.ToggleGreyScale(bool option, float time = 0.25f)`（签名桩，
        //     `d:/2/Warpforge_code/Scripts/Assembly-CSharp/UIImageGreyscaleController.cs:59`）：
        //     把灰度补间成 0..1，`ImageMaterialPair.Update(greyScaleValue)` 写进材质；
        //     属性名 = 静态字段 `GREYSCALE_SHADER_PROPERTY`。
        //   · 属性名 = **`_GreyScale`**：原版材质 `Sprite Greyscale`
        //     （`bundle_battleprefabs_vfxandmisc_assets_all/Material/Material_-1344813161239158161.json`）
        //     的属性表只有它一条、值 `1.0`；shader = `Everguild/UI/Greyscale Add`
        //     （`bundle_menus_assets_all/Shader/Shader_-5597991350116227388.json`，属性表含 `_GreyScale`）。
        //   · 取值语义 = **0 原色 / 1 全灰**（原版「灰那份材质」上就是 1.0）。
        //
        // **落地**：
        //   · 材质侧：卡面基材 = `CardPresentation/CardSprite`（`Sprites/Default` 的等价物 + `_GreyScale`，
        //     见 `Resources/Shaders/CardSprite.shader` 头部）；立绘层 `ArtOpaque`、场上 3D 体 `Card3D`
        //     各补同一个属性（默认 0 ⇒ 画面不变）。
        //   · 传递走 **MaterialPropertyBlock**：每层材质虽然本来就是我们自己 `new` 的，但 MPB 能保证
        //     「灰度」这一档永远不落到共享材质上（字体/TMP 是共享资产 —— 同 `SetQueue` 那条教训）。
        //   · TMP 的字**不吃材质属性**（走顶点色）⇒ 在 `ApplyTint` 里按同一个 `_grey` 去饱和。
        //   · 徽标图标（`TraitDisabled`）自己的 `_GreyScale` 是「未激活态」的判据 ⇒ 取 **max(整卡灰, 自身灰)**，
        //     ⛔ 别把它覆盖成整卡的值（否则未激活徽标会变彩）。

        /// <summary>这张卡当前的去饱和度：0 = 原色，1 = 全灰（原版 `_GreyScale` 的语义）。</summary>
        public float Grey { get { return _grey; } }

        float _grey;
        MaterialPropertyBlock _greyMpb;
        DG.Tweening.Tween _greyTween;

        /// <summary>设置去饱和度（0..1）。`time &gt; 0` 时照原版那 0.25s 补间（`ToggleGreyScale` 的 `time`）。
        /// <para>⚠️ 批处理/自检环境没有帧循环 ⇒ 默认 **0 = 立刻生效**（`DeckRuntime` 的卡池装配走这一档）。</para></summary>
        public void SetGrey(float g, float time = 0f)
        {
            g = Mathf.Clamp01(g);
            if (_greyTween != null && _greyTween.IsActive()) _greyTween.Kill();
            if (time <= 0f || !Application.isPlaying)
            {
                // 值没变就不扫（`SetHighlight` 每次刷新都会调它；`_grey` 默认 0 ⇒ 建卡那条路零开销）。
                // ⚠️ 「之后新建的层漏灰」由 `SetData` / `SetFace` 末尾那两处补（多入口各设一次）。
                if (Mathf.Abs(_grey - g) < 1e-5f) return;
                _grey = g;
                ApplyGrey();
                return;
            }
            _greyTween = DOTween.To(() => _grey, v => { _grey = v; ApplyGrey(); }, g, time)
                                .SetUpdate(CardTween.Mode);
        }

        /// <summary>把 `_grey` 写进**每一层**能认 `_GreyScale` 的材质（MPB），再让文字颜色跟上。</summary>
        void ApplyGrey()
        {
            if (_greyMpb == null) _greyMpb = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<MeshRenderer>(true)) ApplyGreyTo(r);
            // TMP 字走顶点色（材质属性对它无效）⇒ 颜色在 `ApplyTint` 里按同一个 `_grey` 去饱和
            ApplyTint();
        }

        /// <summary>把整卡灰度写进**这一层**（与它自己材质上的 `_GreyScale` 取 max —— 见上面徽标那条）。</summary>
        void ApplyGreyTo(MeshRenderer r)
        {
            if (r == null) return;
            if (_greyMpb == null) _greyMpb = new MaterialPropertyBlock();
            var m = r.sharedMaterial;
            if (m == null || !m.HasProperty("_GreyScale")) return;
            float own = m.GetFloat("_GreyScale");
            r.GetPropertyBlock(_greyMpb);
            _greyMpb.SetFloat("_GreyScale", Mathf.Max(_grey, own));
            r.SetPropertyBlock(_greyMpb);
        }

        /// <summary>把颜色按 `_grey` 去饱和（Rec.601 亮度；**只动 RGB、不动 alpha**）。
        /// TMP 文字/角标走这一支 —— 它们没有 `_GreyScale` 可写。</summary>
        Color Greyed(Color c)
        {
            if (_grey <= 0f) return c;
            float t = Mathf.Clamp01(_grey);
            float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            return new Color(Mathf.Lerp(c.r, lum, t), Mathf.Lerp(c.g, lum, t),
                             Mathf.Lerp(c.b, lum, t), c.a);
        }

        /// <summary>自检用：卡面某层（按 `GameObject` 名，如 `"art"` / `"frame"` / `"info"`）
        /// **渲染器上真正生效**的 `_GreyScale`：先读 `MaterialPropertyBlock`，没设过再读材质；
        /// 那一层不存在 / 材质没这个属性 ⇒ 返回 `-1`（与「0 = 不灰」区分开）。</summary>
        public float LayerGrey(string layerName)
        {
            foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r == null || r.gameObject.name != layerName) continue;
                var m = r.sharedMaterial;
                if (m == null || !m.HasProperty("_GreyScale")) return -1f;
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                return mpb.isEmpty ? m.GetFloat("_GreyScale") : mpb.GetFloat("_GreyScale");
            }
            return -1f;
        }

        /// <summary>当前状态色（状态机在 CardInteraction 那边，这里只负责显示）</summary>
        public CardHighlightState State { get; private set; }

        /// <summary>自检用：那圈状态环**当前的目标色**（含 `RimAlphaScale`）。🆕 **2026-10-19（`N70`）**：
        /// 菜单卡格的悬停走同一层（`SetHoverHighlight`）⇒ 悬停时这里 alpha &gt; 0、离开时 = 0，
        /// 不依赖帧循环（批处理里补间不会自己跑）。</summary>
        public Color RimTargetForTest { get { return _rimTarget; } }

        /// <summary>状态描边（卡后面那圈）。
        ///
        /// 为什么不用「整卡着色」表达状态：`material.color` 是**乘法** ——
        /// 红卡染「置灰」出来还是暗红，状态根本读不出来（试过）。
        /// 描边是独立一层，不受卡面配色影响，也是真实卡牌游戏的通行做法。</summary>
        public void SetHighlight(CardHighlightState s)
        {
            State = s;
            // 🔴 **2026-10-19（`A2139`）：`Unplayable` 不再用乘法压暗** —— 原版是**去饱和**：
            //   判据 = `CollectionCard__UpdateCardVisuals.c:19-20`（`ToggleGreyScale(canAdd ^ 1)`）
            //   ⇒ 这里把状态色的那一份乘法让给 `_GreyScale`（`ColorOf(Unplayable)` 那个
            //   `(0.55, 0.55, 0.58)` 从此**不参与**整卡着色；`State` 仍是 `Unplayable`，自检照旧读得到）。
            //   ⚠️ 别的状态（含 `Normal`）把灰度归零 —— 状态机是「同一时刻一个态」。
            if (s == CardHighlightState.Unplayable)
            {
                SetTint(TintNormal);
                SetGrey(1f);
            }
            else
            {
                SetTint(CardHighlight.ColorOf(s));
                SetGrey(0f);
            }
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
                //   ⚠️ `Unplayable` 依旧不亮（alpha 0）：置灰靠**去饱和**（`SetGrey`，2026-10-19 `A2139`），
                //      而这圈是块比方大的矩形，在卡的透明角/透明边上会露出一圈灰白
                //      （用户 2026-09-12 报的「手牌里有白边」就是它）。
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

        /// <summary>🆕 **2026-10-19（`N70` P0-5）：菜单侧卡格的悬停高亮**（收藏窗卡牌格 / 异画格共用）。
        /// <para>判据（反编译逐句，`d:/2/tools/decomp_full/Card2DController__ToggleHighlight.c`）：
        /// 取那颗图形材质里某个颜色的 **`a`**，按 `hover` 写成 **1 / 0**，再交给
        /// `UIImageMaterialColorChanger.DoColorChange(..., 0.2f, …)` **补间 0.2 秒**（常数 `DAT_1834b2bb0` = 0.2，
        /// 与 `CardHighlight.OutlineAnimTime` 同源）。原版**不改缩放**（缩放那一条是 `CollectionDeck` 卡组格专用的 1.05）。</para>
        /// <para>🔴 **2026-10-23（`C3`）就地改判：这一条走【卡面那层 `Card Highlight And Shadow`】，
        /// ⛔ 不再借战斗的 `_rim`（`3DBody/MinionLight` = 战斗 6 态状态环）。** 两层判据完全不同：
        ///  · **菜单悬停**（本方法的判据，全部实读）：quad **4.4281²** @ y **−0.01265**
        ///    （`MENUS/RectTransform/RectTransform_4117944419202364760.json` 的 `m_SizeDelta`/`m_AnchoredPosition`）
        ///    · 贴图 = **本卡自己那张 SDF**（`CardArt.Sdf(faction, rarity, tactics, tier)`）
        ///    · 材质 = **`Card Frame SDF`**（`bundle_duplicateassetisolation_assets_all/Material/
        ///    Material_-3316280387615011577.json`：`_Inner_Edge 0.333` / `_Inner_Fallof 0.224` /
        ///    `_SpriteAlphaEdge_Inner 0.043`）· 颜色 = `CollectionCard.highlightColor` **#0080FF** ·
        ///    alpha 目标 **1.0**（`ToggleHighlight` 直接写 1/0，**不乘任何材质常量**）。
        ///  · **战斗状态环**（`_rim`）：quad 2.4928 × 3.3084（手牌 1.09×1.06）· 贴图 `Card board frame SDF`(79×107) ·
        ///    材质 `Card board Frame SDF`（0.242 / 1.0 / 0.339）· alpha 还要 × `RimAlphaScale 0.447`。
        /// ⇒ 我们这棵树里，菜单那一层**本来就已经有了** —— `SdfMaterial()`（值 = `Card Frame SDF` 逐项）
        ///   + `ShadowMesh()`（4.4281² @ −0.0126）+ 每卡各自的 SDF 贴图，就是原版那个节点。
        ///   所以本方法只写 `OutlineMat`（= 那一层材质的 `_Outline`；2026-10-24 `C1-B03` 并层后
        ///   它恒指 `_shadowLayer`，原 `outlineGlow` 专用层已删），**`_rim` 一个字都不碰** —— 这就是
        ///   「菜单档 / 战斗档分离」的落地方式（物理上就是两层不同的东西，⛔ 不是靠一个 bool 档位开关）。</para>
        /// <para>⚠️ 只在**常规态**用（收藏窗每格建完都 `SetHighlight(Normal)`）；置灰不点亮
        /// —— 原版那颗 `ToggleHighlight` 也是挂在菜单卡格上、与战斗状态机无关。</para></summary>
        public void SetHoverHighlight(bool on)
        {
            if (on && State == CardHighlightState.Unplayable) return;
            // 颜色 = 原版 `CollectionCard.highlightColor`（MB 40971030049256792 实读 (0, 0.5026950836181641, 1, 1)）；
            // alpha 目标 = 原版 `ToggleHighlight` 直接写的 **1 / 0**（`SetOutline` 那条链不乘 `RimAlphaScale`）。
            SetOutline(new Color(MenuHoverColor.r, MenuHoverColor.g, MenuHoverColor.b, on ? 1f : 0f));
        }

        /// <summary>菜单卡格的悬停色 = 原版 `CollectionCard.highlightColor` = **`#0080FF`**。
        /// ⚠️ 与战斗那 5 个状态色（`CardHighlight.ColorOf/FrameColorOf`）是**两套**，⛔ 别合并。</summary>
        public static readonly Color MenuHoverColor = new Color(0f, 0.5026951f, 1f);

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
            // 🔴 **2026-10-25（`M2`）**：这一层**不挂 `Front`**（它固定在 `FaceRoot` 下，见 `Build` 那处），
            //   所以非场上那支要把 `Front` 的 `+0.08` 显式补进 `localPosition.y` —— 否则整脸抬上去、
            //   状态环留在原地（环圈的是「看得见的那张卡」，原版那颗 `MinionLight` 也在卡体上）。
            //   ⚠️ 场上那支取的是 `From3DBody`（3DBody 空间、与 2D 卡面无关）⇒ **不加**。
            //   ⚠️ 两个入口（`Build` 建完即调 / `SetFace` 换形态）都走这一个函数 ⇒ 只此一处。
            t.localPosition = board ? From3DBody(-0.001f, 1.326f, 0f) + new Vector3(0f, 0f, 0.05f)
                                    : new Vector3(0f, FrontAposY, 0.05f);
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
        /// <summary>自检用：状态环材质某个 float 属性的当前值（没有这个属性 ⇒ **−1**，不静默给 0）。
        /// 用途 = 反向断「菜单那三个 SDF 值没有串进战斗材质」（判据 → `SetHoverHighlight` 的注释）。</summary>
        public float RimFloatForTest(string p)
        {
            var m = _rim != null ? _rim.sharedMaterial : null;
            return (m != null && m.HasProperty(p)) ? m.GetFloat(p) : -1f;
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
            // 🔴 **2026-10-25（`X8` · 顺手①）两处照原版补（铁律 5）**：
            //   ① `m_SortingOrder = 2` —— 原版那层是 `SpriteRenderer`，序列化值就是 **2**
            //      （`CardHighlight.minionWillDieObject` 的那颗；同族的卡面层是 0/1）。我们建的是
            //      `MeshRenderer`，默认 0 ⇒ 卡内层序与卡面其它层**同号**，谁压谁只能靠距离碰运气。
            //   ② **建层当场自己盖一次队列** —— `SetQueue` 虽然会扫整棵子树，但它是**建层之前**跑的
            //      （这层是 `SetWillDie` 第一次调用才懒建的）⇒ 不补这一句，它就一直停在材质默认队列
            //      （3000），而卡面那一整段可能已经被布局抬到 ≥3000 的另一档 ⇒ **这层被卡压住**。
            //      同族先例：徽标 `:3819` / 数值层 `:12380` / 状态环 `:4465` 都是「晚建 ⇒ 必须当场补号」。
            //   🧨 改坏法：删掉 `StampQueue` 那一句 ⇒ `Editor/BattleScene.cs` 的
            //      「晚建的会死层建出来就落在本卡队列段」那条红（它先 `SetQueue(3005)` 再首次建层）。
            _willDieMr.sortingOrder = 2;
            StampQueue(_willDieMr);
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
        /// <summary>自检用：那层材质的**渲染队列**（应当 = 这张卡当前的 `Queue`；`-1` = 还没建）。
        /// <para>🔴 2026-10-25（`X8`）：这条是给「晚建层被卡压住」那一档配的 —— 先 `SetQueue(3005)`
        /// 再首次建层，读它必须是 3005（不是材质默认的 3000）。</para></summary>
        public int WillDieRenderQueue
        {
            get
            {
                var mt = _willDieMr != null ? _willDieMr.sharedMaterial : null;
                return mt != null ? mt.renderQueue : -1;
            }
        }
        /// <summary>自检用：那层的 `sortingOrder`（原版 `SpriteRenderer.m_SortingOrder` = **2**；`-1` = 还没建）。</summary>
        public int WillDieSortingOrder { get { return _willDieMr != null ? _willDieMr.sortingOrder : -1; } }
        /// <summary>自检用：原版那条 clip 的**起点/终点**（3DBody 坐标已换算到卡根）。</summary>
        public static Vector3 WillDieFromForCheck { get { return WillDieFrom; } }
        public static Vector3 WillDieToForCheck { get { return WillDieTo; } }

        // ==================================================================
        //  🆕 2026-10-20（`W58`）：原版场上卡还有两层我们一直没做 —— `SwarmIcon` · `HealText`
        // ==================================================================
        //
        // 判据（原版 prefab `bundle_battleprefabs_vfxandmisc_assets_all`，逐份实读）：
        //  · `3DBody/SwarmIcon`：local **(0, 3.329, −0.05)**、scale **1.016651**、
        //    `SpriteRenderer.m_Size = 3.06 × 4.45`、`m_SortingOrder = 1`、**默认关**；
        //    sprite = **`Atlas_trait_icon_swarm`**（PathID −8621708576942405447，已用 UnityPy 在
        //    `atlasindividual_assets_40ktraiticonatlas.bundle` 里解析出名字；
        //    我们的 `Resources/Art/traits/swarm.png` 就是同一张图）。
        //  · 运行时那一份是 Addressable 预制体 **`Swarm Battle Card Icon`**（`CardScript.swarmObject`，
        //    GUID `87a100c766fda41059e261e3e1ed817b`）：根下子节点 `Icon` 的 scale/size 与上面逐值相同，
        //    挂 `SwarmIconController`（`offAnimationTime = 0.3`、`myAnimation`、`toggleOnSound`）。
        //    `SwarmIconController.Toggle`（反编译实读）：on = 直接 SetActive + 开 Animation + 放声音；
        //    off = **DOFade 到 0 + DOLocalMove 回原位，时长 `offAnimationTime`（0.3s）**，完成后关掉。
        //    ⚠️ **触发链也没有接**（`CardScript.DisplaySwarm/RemoveSwarm` 的调用点在 `BattleDriver`，
        //    不在本批白名单里）：本类只提供 `SetSwarm(bool)`，接线由战斗线另行安排。
        //
        //  ✅ **2026-10-22（L-D）：上面那两条「缺件」已接上** —— 判据逐条：
        //   · **clip 在运行时怎么拿**（三条候选里选**第 1 条**）：
        //     `Swarm Battle Card Icon` 那份 prefab 的 `Animation` 组件**已经序列化**了片段引用 ——
        //     `Assets/WarpforgeVFX/Prefabs/Swarm Battle Card Icon.prefab`：
        //       `m_Animations: - {fileID: 7400000, guid: eca670d7134f9704aab1c6058384e579, type: 2}`
        //       `m_PlayAutomatically: 0` · `m_CullingType: 0`(= AlwaysAnimate) · `m_WrapMode: 0`
        //     该 guid = `Assets/WarpforgeVFX/Animations/SwarmIconOn.anim`（`m_Name = SwarmIconOn` ·
        //     `m_Legacy: 1` · `m_SampleRate: 60`；原版 = `bundle_cardanimsgeneral_assets_all/
        //     AnimationClip/AnimationClip_6836189287277267510.json`，**同名**）。
        //     运行时那份 prefab 从效果库取（`WarpforgeEffectLibrary` 是 `Resources/` 下的 SO ⇒
        //     prefab 与它的 clip 一起进 player）⇒ 见 `SwarmClip()`。
        //     ⛔ **不用第 2 条**（`WarpforgeAnimatorBridge.TryGetClip`）：那条路只认 SimpleAnimation
        //     的 `<效果名>|<状态名>` 容器别名（`gen_simple_animation_clips.py` 产的），这枚图标不是那族。
        //     ⛔ **也就用不上第 3 条**（dump 成 JSON 自己插值）—— 真 `Animation` 组件能直接播原件。
        //   · **音**：`SwarmIconController.toggleOnSound` = PPtr(**外部 CAB**, −4158225771113159080)
        //     ⇒ `bundle_soundcollection_assets_all` 里 `m_Name = "Swarm Trait UI Enabled"` 的 `AudioCue`
        //     （`clipList` 唯一一条 = `Tyranid_BulletHit` · pitch 0.9~1.1 · vol 1~1 · ttp 0.01）。
        //     **原版只有「开」那一条**：`SwarmIconController` 上没有 toggleOffSound 字段，
        //     反编译出来的 `Toggle(false)` 整支里**一个 `SoundManager` 调用都没有** ⇒ ⛔ 别自己补关音。
        GameObject _swarmIcon;
        /// <summary>`SwarmIconOn` 的三条曲线 path 全是 **`Icon`**（位置 / 缩放 / `m_Color.a`）⇒
        /// 必须有一层同名子件给 `Animation` 绑。原版 prefab 就是这么分的（根 = 控制器 + Animation）。
        /// <para>⚠️ 我们的基准 scale 放**下一层 `Mesh`**（`SwarmLocalScale` 已经把 prefab 那层的
        /// `1.016651` 乘进去了）⇒ 这一层的基准 = `Vector3.one`，别在这里再乘一次。</para></summary>
        Transform _swarmIconNode;
        MeshRenderer _swarmIconMr;
        Animation _swarmAnim;
        Tweener _swarmFade;
        float _swarmAlpha;

        /// <summary>原版 `SwarmIconController.offAnimationTime = 0.3`（预制体字段，秒）。</summary>
        public const float SwarmOffFadeTime = 0.3f;
        /// <summary>效果库里那份 prefab 的名字（= 原版 Addressable 预制体名，`CardScript.swarmObject`）。</summary>
        public const string SwarmPrefabName = "Swarm Battle Card Icon";
        /// <summary>prefab 的 `Animation.m_Animations[0]`（`SwarmIconOn.anim`，`m_Name` 同名）。</summary>
        public const string SwarmClipName = "SwarmIconOn";
        /// <summary>原版 `toggleOnSound` 的 cue 名（判据见上面那一段）。**只有开音**。</summary>
        public const string SwarmOnCue = "Swarm Trait UI Enabled";
        /// <summary>clip 里 `m_Color.a` 那条曲线的时长：两个关键帧 t=0/0.35、value 0/1。
        /// 实读 `WarpforgeVFX/Animations/SwarmIconOn.anim` 的 `m_FloatCurves[0]`。
        /// ⚠️ 那条曲线绑的是 **SpriteRenderer**（classID 212）⇒ 我们的网格图标收不到，
        /// 由 `_swarmFade` 照这三个数补一遍（不是近似：起点 / 终点 / 时长逐一相同）。</summary>
        public const float SwarmFadeInTime = 0.35f;
        /// <summary>Swarm 图标中心（卡根 = 原版 3DBody 空间；y = 3.329 − 半卡高）。</summary>
        public static Vector3 SwarmLocalPos { get { return new Vector3(0f, 3.329f - CardUnitH * 0.5f, -0.05f); } }
        /// <summary>Swarm 图标尺寸 = `m_Size 3.06×4.45` × localScale 1.016651（卡单位；SpriteRenderer 是拉伸画）。</summary>
        public static Vector3 SwarmLocalScale { get { return new Vector3(3.06f * 1.016651f, 4.45f * 1.016651f, 1f); } }

        /// <summary>显示 / 收起「虫群合并」图标（原版 `CardScript.DisplaySwarm/RemoveSwarm` → `SwarmIconController.Toggle`）。
        /// 三支逐句对齐 `SwarmIconController__Toggle.c`（判据见上面那一段）。</summary>
        public void SetSwarm(bool on)
        {
            if (on && _swarmIcon == null) BuildSwarm();
            if (_swarmIcon == null) return;
            if (_swarmFade != null && _swarmFade.IsActive()) _swarmFade.Kill();

            if (on)
            {
                // 原版 `Toggle(true)`：`DOTween.KillAll(spriteRenderer.gameObject)` / `KillAll(transform.gameObject)`
                //  → `transform.localPosition = originalPosition` → `gameObject.SetActive(true)`
                //  → `myAnimation.enabled = true` → `SoundManager.Play2D(toggleOnSound)` → `myAnimation.Play()`。
                _swarmIcon.transform.localPosition = SwarmLocalPos;      // 回原位（原版缓存下来的 originalPosition）
                _swarmIcon.SetActive(true);                              // ⚠️ 必须在 Play 之前（见 BuildSwarm 注释）
                PlaySwarmOnSound();                                      // 原版顺序：先 `Play2D` 再 `Play()`
                PlaySwarmAnimation();
                // 淡入：clip 的 `m_Color.a` 曲线（0 → 1 @ 0.35 s）绑不到 MeshRenderer ⇒ 自己补一遍。
                // （原版 `Play()` 一调，那条曲线也是从 0 重新走 ⇒ 我们要的正是「开的那一拍 alpha 归 0」。）
                _swarmAlpha = 0f;
                ApplySwarmAlpha();
                _swarmFade = DOTween.To(() => _swarmAlpha, v => { _swarmAlpha = v; ApplySwarmAlpha(); },
                                        1f, SwarmFadeInTime).SetUpdate(CardTween.Mode);
                return;
            }

            if (!_swarmIcon.activeSelf) return;
            // 原版 `Toggle(false)`：`myAnimation.enabled = false` → `spriteRenderer.DOFade(0, offAnimationTime)`
            //  → `transform.DOLocalMove(originalPosition, offAnimationTime).OnComplete(SetActive(false))`。
            // ⚠️ 原版这一整支被 `if (myAnimation != null)` 包着（= clip 缺件时连淡出都不做）——
            //    那是写法上的意外、不是设计 ⇒ 我们照旧淡出（真缺件时 `PlaySwarmAnimation` 那边会出声说明）。
            // ⚠️ 原版那句 `DOLocalMove` 在我们这儿**是空操作**：根节点从建起就没被移动过，脚本里也没有
            //    任何东西移动它（原版同理，它缓存的 originalPosition 就是出厂位）⇒ 不额外造一条同值补间
            //    （那会多一个没 `SetLink` 的 Transform 补间，见 `CardTween.Use` 的注释）。
            // ⚠️ 原版**关的时候不播声音**（见上面判据），⛔ 别补。
            if (_swarmAnim != null) _swarmAnim.enabled = false;   // 关掉 = 让 Unity 把 `Icon` 那层弹回基准
            ResetSwarmNode();                                     // 不指望引擎还原：显式再写一遍
            _swarmFade = DOTween.To(() => _swarmAlpha, v => { _swarmAlpha = v; ApplySwarmAlpha(); }, 0f, SwarmOffFadeTime)
                                .SetUpdate(CardTween.Mode)
                                .OnComplete(() => { if (_swarmIcon != null) _swarmIcon.SetActive(false); });
        }

        /// <summary>原版 `Toggle(true)` 末两句：`myAnimation.enabled = true; myAnimation.Play();`。
        /// ⚠️ 原版调的是 **无参 `Play()`**（播 `m_Animation` 那个默认片段，prefab 上 = 同一条）；
        /// 我们按**名字**播（`m_Animations[0]`，名字 = <see cref="SwarmClipName"/>）—— 同一条资产，
        /// 但名字这条路在「以后有人往 prefab 上多挂一条」时不会静默播错。</summary>
        void PlaySwarmAnimation()
        {
            if (_swarmAnim == null) return;                       // `BuildSwarm` 已经出过声（为什么没挂上）
            var clip = _swarmAnim.GetClip(SwarmClipName);
            if (clip == null) return;                             // 同上
            _swarmAnim.enabled = true;
            _swarmAnim.Play(SwarmClipName);
            SwarmAnimationPlayCalls++;
            SwarmLastAnimationClip = clip.name;
        }

        /// <summary>原版 `SoundManager.Play2D(toggleOnSound)`（**2D**，不衰减）—— 走工程既有的 cue 通道
        /// （`WFSoundBank` / `WFSoundPlayer`，与 `DraggableController.PlayDragSound` 同一条），⛔ 不另造播放器。
        /// 取不到 cue **必须出声**（只报一次，免得逐张卡刷屏）。</summary>
        static readonly HashSet<string> _swarmCueWarned = new HashSet<string>();
        void PlaySwarmOnSound()
        {
            WarpforgeVFX.WFSoundCue cue;
            if (!WarpforgeVFX.WFSoundBank.TryGetCue(SwarmOnCue, out cue))
            {
                if (_swarmCueWarned.Add(SwarmOnCue))
                    Debug.LogWarning("[CardView] 虫群图标开音 `" + SwarmOnCue + "` 在 `WFSoundBank` 里取不到 ⇒ 这一拍**不出声**"
                                   + "（**不是静默**：原版 = `SwarmIconController.toggleOnSound`，cue 名与 clip 都已解出；"
                                   + "缺的是 cue 表里那一行 —— 跑 `工具/import_original_sfx.py` 补）");
                return;
            }
            WarpforgeVFX.WFSoundPlayer.Play(cue, Vector3.zero, true);   // is2d = true ⇒ 原版 `Play2D`
            SwarmOnSoundCalls++;
            SwarmLastSoundCue = cue.name;
        }

        /// <summary>把 clip 动过的那一层 `Icon` 弹回基准。原版靠 `Animation.enabled = false` 让 Unity 还原；
        /// **批处理 / 自检里不能指望引擎帮你还原** ⇒ 显式写一遍（位置 0、scale 1，见字段注释）。</summary>
        void ResetSwarmNode()
        {
            if (_swarmIconNode == null) return;
            _swarmIconNode.localPosition = Vector3.zero;
            _swarmIconNode.localScale = Vector3.one;
        }

        static AnimationClip _swarmClip;
        static bool _swarmClipTried;              // 负缓存：取不到只报一次
        /// <summary>从效果库那份 prefab 上取 `SwarmIconOn`（判据：prefab 的 `Animation.m_Animations[0]`
        /// 已序列化 guid `eca670d7134f9704aab1c6058384e579` = `Assets/WarpforgeVFX/Animations/SwarmIconOn.anim`）。
        /// 取不到返回 null **并出声**（不许静默失败）。</summary>
        static AnimationClip SwarmClip()
        {
            if (_swarmClip != null) return _swarmClip;
            if (_swarmClipTried) return null;
            _swarmClipTried = true;

            var lib = WarpforgeVFX.WarpforgeEffectLibrary.Instance;
            WarpforgeVFX.WFEffectEntry entry;
            if (lib == null || !lib.TryGet(SwarmPrefabName, out entry) || entry.prefab == null)
            {
                Debug.LogWarning($"[CardView] 效果库里取不到 `{SwarmPrefabName}` ⇒ 虫群图标**没有动画**"
                               + "（开音不受影响：它走 cue 表，与效果库无关）。"
                               + "**不是静默**；修法 = 重跑 `工具/extract_missing_shaders.py --prefabs` + "
                               + "Tools > Warpforge > 生成效果库");
                return null;
            }
            var src = entry.prefab.GetComponent<Animation>();
            // 先按名字取（prefab 的 `m_Animations`），取不到再退 `m_Animation`（同一份资产的默认片段）——
            // 两条都是 prefab 上**已序列化**的字段，不依赖任何运行时状态。
            var clip = src != null ? (src.GetClip(SwarmClipName) ?? src.clip) : null;
            if (clip == null)
            {
                Debug.LogWarning($"[CardView] `{SwarmPrefabName}` 的 `Animation` 组件上取不到片段 `{SwarmClipName}`"
                               + "（prefab 的 `m_Animations` 应指向 `WarpforgeVFX/Animations/SwarmIconOn.anim`）"
                               + " ⇒ 图标照旧显示，但**不播动画**");
                return null;
            }
            _swarmClip = clip;
            return clip;
        }

        void ApplySwarmAlpha()
        {
            if (_swarmIconMr == null || _swarmIconMr.sharedMaterial == null) return;
            var c = _swarmIconMr.sharedMaterial.color;
            c.a = _swarmAlpha;
            _swarmIconMr.sharedMaterial.color = c;
        }

        /// <summary>层级照搬原版 prefab（只把 SpriteRenderer 换成网格画）：
        /// <code>
        ///   SwarmIcon   ← 根：挂 `Animation`（= 原版 prefab 根上的那份）；localPos = SwarmLocalPos
        ///     Icon      ← clip 三条曲线的 path 都是它（放大 / 上浮 / 淡入）
        ///       Mesh    ← Quad + 材质，localScale = SwarmLocalScale（**非等比** 3.06 × 4.45）
        /// </code>
        /// 为什么要多一层 `Mesh`：clip 的缩放曲线是 **uniform**（0.6123 → 1.385），直接作用在网格上会把
        /// 原版那对非等比尺寸抹成正方形；而原版是「SpriteRenderer 的 `m_Size` 3.06×4.45 × 均匀变换缩放」
        /// ⇒ 把尺寸放在固定的一层、缩放交给上面对应的一层，两边等价。</summary>
        void BuildSwarm()
        {
            var tex = CardArt.Trait("swarm");
            if (tex == null)
            {
                Debug.LogWarning("[CardView] `SwarmIcon` 取不到贴图 `Art/traits/swarm.png`"
                               + "（原版那枚 = `Atlas_trait_icon_swarm`，`40ktraiticonatlas`）⇒ 整层不建");
                return;
            }
            _swarmIcon = new GameObject("SwarmIcon");
            // ⚠️ 挂在**卡根**下 —— 我们的卡根就是原版 `3DBody` 的等价物（见 `CanActParticles` 那段：
            //    挂到 `_body3D` 底下会多乘一次 0.88586）。
            _swarmIcon.transform.SetParent(transform, false);
            _swarmIcon.transform.localPosition = SwarmLocalPos;
            _swarmIcon.transform.localRotation = Quaternion.identity;
            _swarmIcon.transform.localScale = Vector3.one;   // 基准尺寸在 `Mesh` 那层（见方法注释）

            var node = new GameObject("Icon");               // ⚠️ 名字不能改：clip 的 path 就是 `Icon`
            node.transform.SetParent(_swarmIcon.transform, false);
            node.transform.localPosition = Vector3.zero;
            node.transform.localRotation = Quaternion.identity;
            node.transform.localScale = Vector3.one;
            _swarmIconNode = node.transform;

            var mesh = new GameObject("Mesh");
            mesh.transform.SetParent(node.transform, false);
            mesh.transform.localPosition = Vector3.zero;
            mesh.transform.localRotation = Quaternion.identity;
            // `QuadMesh()` 是 1×1 的 Quad ⇒ localScale 就是世界尺寸。原版 `m_Size` 是**非等比**的
            // （3.06 × 4.45），SpriteRenderer 照拉不误 ⇒ 这里也照拉，⛔ 别按贴图比例内接。
            mesh.transform.localScale = SwarmLocalScale;
            mesh.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            _swarmIconMr = mesh.AddComponent<MeshRenderer>();
            var m = new Material(BaseMaterial()) { name = "SwarmIcon" };
            m.mainTexture = tex;
            m.color = Color.white;
            _swarmIconMr.sharedMaterial = m;
            _swarmIconMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _swarmIconMr.receiveShadows = false;
            // ⚠️ **不进 `_layers`**：颜色/alpha 由本类按 `SetSwarm` 的补间说了算（同 `Minion Death Icon`）。

            // `Animation` 挂在**根**上（与原版 prefab 同构）。三个字段照抄 prefab：
            // `m_PlayAutomatically: 0` ⇒ 必须我们自己调 `Play()`；`m_CullingType: 0` = AlwaysAnimate；
            // `m_WrapMode: 0` = Default（clip 自己的 `m_WrapMode` 也是 0 ⇒ Once）。
            // ⚠️ 一条 clip 都没挂上时**不建组件**（免得留一个空 `Animation` 骗过「有没有接上」的判读）。
            var clip = SwarmClip();
            if (clip != null)
            {
                _swarmAnim = _swarmIcon.AddComponent<Animation>();
                _swarmAnim.playAutomatically = false;
                _swarmAnim.cullingType = AnimationCullingType.AlwaysAnimate;
                _swarmAnim.AddClip(clip, SwarmClipName);
            }
            _swarmIcon.SetActive(false);
        }

        /// <summary>自检口：`Animation.Play(...)` **真的被调过几次**（同 `RemnantAnimationPlayCalls` ——
        /// edit mode / 批处理里 `Animation.isPlaying` 与采样都不可靠，调用计数是唯一与引擎状态无关的观察点；
        /// 没挂上组件 / 没播 ⇒ 恒 0 ⇒ 断言照红，⛔ 不是放宽）。</summary>
        public int SwarmAnimationPlayCalls { get; private set; }
        /// <summary>自检口：最近一次真的播出去的片段名（null = 还没播过）。</summary>
        public string SwarmLastAnimationClip { get; private set; }
        /// <summary>自检口：`toggleOnSound` 真的交给播放器几次。</summary>
        public int SwarmOnSoundCalls { get; private set; }
        /// <summary>自检口：最近一次交出去的 cue 名（= <see cref="SwarmOnCue"/>）。</summary>
        public string SwarmLastSoundCue { get; private set; }
        /// <summary>自检口：当前 alpha（淡入 0→1 / 淡出 1→0）。</summary>
        public float SwarmAlphaNow { get { return _swarmAlpha; } }
        /// <summary>自检口：`Icon` 那层当前的 localScale（批处理里验「曲线有没有真绑上」用它）。</summary>
        public Vector3 SwarmNodeLocalScale { get { return _swarmIconNode != null ? _swarmIconNode.localScale : Vector3.zero; } }

        /// <summary>自检用：Swarm 图标现在显示着吗（没建过 = false）。</summary>
        public bool SwarmVisible { get { return _swarmIcon != null && _swarmIcon.activeSelf; } }
        /// <summary>自检用：Swarm 图标的贴图名（取不到 = null）。</summary>
        public string SwarmTexName
        {
            get
            {
                var m = _swarmIconMr != null ? _swarmIconMr.sharedMaterial : null;
                return (m != null && m.mainTexture != null) ? m.mainTexture.name : null;
            }
        }
        /// <summary>自检用：Swarm 图标在卡根空间的**当前位置**（原版 3DBody (0,3.329,−0.05) → 卡根）。</summary>
        public Vector3 SwarmIconLocalPos { get { return _swarmIcon != null ? _swarmIcon.transform.localPosition : Vector3.zero; } }
        /// <summary>自检用：Swarm 图标的 localScale（原版 m_Size × 1.016651）。</summary>
        public Vector3 SwarmIconLocalScale { get { return _swarmIcon != null ? _swarmIcon.transform.localScale : Vector3.zero; } }

        // ── 治疗飘字（原版 `Board Elements/HealText`）──────────────────────────────
        //
        //  · 位置：`m_AnchoredPosition = (0, 2)`（Board Elements 与 3DBody 同位、scale 1）
        //    ⇒ 卡根 **(0, 2 − 3.3313/2 = +0.33435, −0.05)**（z = RectTransform localPosition z）
        //  · TMP：`m_text "+5"` · `m_fontSize 0.7` · **`m_isOrthographic = 1`**（⇒ 世界 em = 0.7，
        //    不是 3D 那档的 0.07）· `m_fontColor` 纯白 · Center/Middle（2 / 512）· 默认关
        //  · ✅ **2026-10-20（`W58`）：触发链查到了** —— 原版唯一显示口 = `CardScript.Heal` 协程
        //    （`d:/2/tools/decomp_full/CardScript._Heal_d__468__MoveNext.c:155-225`；判据 →
        //    `资料/普查产出_1020/W58_战斗线_HealText与InBattleId判据.md` §A）：
        //      实际量 = `min(请求, MaxHealth − Health)`（`:109-112`）；**实际量 < 1 不显示**（`:136`）；
        //      文案 `"+" + 实际量`；**先压到 `localScale = one × 0.05`**（`:166-196`，常量 `0x1834b2f94`），
        //      再 `DOScale(one, 0.7s)`（`0x1834b3188`）· `SetEase(6 = OutQuad)` · `SetLoops(2, 1 = Yoyo)`
        //      （`:195-200`）⇒ 0.05→1.0→0.05 共 **1.4 s**；之后 `WaitForSeconds(1.5)`（`0x1834b3090`）
        //      才 `SetActive(false)`（`:204` → 第二拍 `:64-84`）。
        //    ⚠️ 原来这段写着「何时显示查不到 / `decomp_full` 按 `healText` 零命中」—— **那是没找到**：
        //      显示点走的是字段偏移 `BattleCardUI.healText (+0x138)`，按字段名 grep 命不中（铁律 5）。
        TextMeshPro _healText;
        /// <summary>治疗飘字当前那条补间（重入时先 `Kill`；自检用 `HealTweenNow` 读时长）。</summary>
        Tween _healSeq;

        /// <summary>原版 HealText 的**缩放段**时长：`DOScale(0.7)` × `SetLoops(2, Yoyo)` ⇒ 1.4 s 全程。
        /// 判据 = `CardScript._Heal_d__468__MoveNext.c:195-200` 的 `0x1834b3188`（实读 0.7）。</summary>
        public const float HealTextScaleSeconds = 0.7f;
        /// <summary>原版 HealText 的**生命周期**（显示 → `SetActive(false)`）= **1.5 s**。
        /// 判据 = `CardScript._Heal_d__468__MoveNext.c:204` 的 `WaitForSeconds(0x1834b3090 = 1.5)`。
        /// 🔴 **这是「一条治疗占多久」的唯一判据**：`BattleDriver.PlaySignals` 的时间线、
        /// 以及多目标治疗的**串行**（每目标 1.5 s）都读它（⛔ 别在别处再写一个字面量 1.5）。</summary>
        public const float HealTextHoldSeconds = 1.5f;

        /// <summary>原版 HealText 的世界字号（3D TMP 的 `fontSize`）—— `0.7` 在 `m_isOrthographic = 1`
        /// 那一档就是世界 em 0.7；我们的 TMP 是 3D 档（em = 0.1 × fontSize）⇒ 传 **7.0**。</summary>
        public const float HealTextFontSize = 7.0f;
        /// <summary>HealText 在卡根空间的中心。</summary>
        public static Vector3 HealTextLocalPos { get { return new Vector3(0f, 2f - CardUnitH * 0.5f, -0.05f); } }

        /// <summary>显示「+N」治疗飘字（原版 `HealText` / `CardScript.Heal`）。
        ///
        /// <para>🔴 **`amount` 必须是【实际治疗量】**（= `min(请求, MaxHealth − Health)`）——
        /// 原版 `CardScript._Heal…:109-112` 先 clamp、`:136` 再判 `< 1`；
        /// ⛔ **别再在这里夹 `Mathf.Max(1, …)`**：那会把「0 / 负数治疗」显示成 `+1`，
        /// 满血被治疗也会冒出一颗 `+1`（原版那一档**什么都不显示**）。</para>
        ///
        /// <para>动画照原版（`:166-200`）：`localScale = one × 0.05` → `DOScale(one, 0.7s)`
        /// `OutQuad` `Loops(2, Yoyo)` → 停到 `1.5 s` 关。重入 = 先 `Kill` 旧序列再重开
        /// （同一张卡连续被治疗时原版也是重新压小再涨一次）。</para></summary>
        public void ShowHeal(int amount)
        {
            // 原版 `:136`：实际量 < 1 ⇒ 连显示那一支都不进（文本保持上一次的值也无所谓 —— 反正关着）
            if (amount < 1) { HideHeal(); return; }
            if (_healText == null) BuildHealText();
            if (_healText == null) return;

            if (_healSeq != null) { _healSeq.Kill(false); _healSeq = null; }

            _healText.text = "+" + amount.ToString();
            _healText.gameObject.SetActive(true);

            var tr = _healText.transform;
            // 原版 `:166-196`：先把 scale 写死成 `one × 0.05`（常量 `0x1834b2f94` 实读 0.05），再起补间
            tr.localScale = Vector3.one * 0.05f;
            var seq = DOTween.Sequence();
            // `DOScale(one, 0.7)` + `SetEase(6 = OutQuad)` + `SetLoops(2, Yoyo)`（`:195-200`）
            // ⇒ 第一段 0.05→1.0、第二段 1.0→0.05，共 1.4 s。
            seq.Append(tr.DOScale(Vector3.one, HealTextScaleSeconds)
                         .SetEase(Ease.OutQuad)
                         .SetLoops(2, LoopType.Yoyo));
            // 原版 `:204` 的 `WaitForSeconds(1.5)` 从**显示那一刻**起算 ⇒ 补足剩下的 0.1 s 再关。
            float rest = HealTextHoldSeconds - HealTextScaleSeconds * 2f;
            if (rest > 0f) seq.AppendInterval(rest);
            // ⚠️ 序列自己的 ease 用 `Linear`（= 恒等映射）—— 否则 `SetEase` 会**覆盖**子补间的 OutQuad。
            seq.OnComplete(() => { _healSeq = null; HideHeal(); });
            _healSeq = CardTween.Use(seq, Ease.Linear, tr);
        }

        /// <summary>收起治疗飘字（生命周期到点、换到非场上形态、或重入时先清）。
        /// 原版另一个关闭点 = `BattleCardUI.HideExtraEffects`（`mineuLight / canAttackAnim /
        /// damageCounter / healText` 一起关，由 `SetObjectVisibility` 调）——
        /// 我们对应的入口 = <see cref="SetFace"/> 的「非场上」那一支。</summary>
        public void HideHeal()
        {
            if (_healSeq != null) { _healSeq.Kill(false); _healSeq = null; }
            if (_healText != null) _healText.gameObject.SetActive(false);
        }

        void BuildHealText()
        {
            // 原版字体 = Pragati（`m_fontAsset` PathID 3485036404935369831，`bundle_fonts_assets_all`）
            // ⇒ 我们的 `Face.LatinCardBody`。
            _healText = TmpFont.NewText(transform, "HealText", "+5", HealTextFontSize, Color.white,
                                        TmpFont.Face.LatinCardBody);
            if (_healText == null)
            {
                Debug.LogWarning("[CardView] `HealText` 建不出来（TMP 字体资产取不到）⇒ 治疗飘字整层没有");
                return;
            }
            _healText.transform.localPosition = HealTextLocalPos;
            _healText.alignment = TextAlignmentOptions.Center;    // 原版 H=Center(2) / V=Middle(512)
            _healText.gameObject.SetActive(false);
            // ⚠️ **不进 `_layers`**：它是「只属于治疗那一下」的飘字，颜色由自己说了算
            //    （同 `Minion Death Icon` / `SwarmIcon` 的理由）。
        }

        /// <summary>自检用：治疗飘字现在显示着吗。</summary>
        public bool HealTextVisible { get { return _healText != null && _healText.gameObject.activeSelf; } }
        /// <summary>自检用：治疗飘字当前文本（没建过 = null）。</summary>
        public string HealTextText { get { return _healText != null ? _healText.text : null; } }
        /// <summary>自检用：治疗飘字在卡根空间的位置。</summary>
        public Vector3 HealTextLocalPosNow { get { return _healText != null ? _healText.transform.localPosition : Vector3.zero; } }
        /// <summary>自检用：治疗飘字当前的 <c>localScale</c>（没建过 = 零向量）。</summary>
        public Vector3 HealTextScaleNow { get { return _healText != null ? _healText.transform.localScale : Vector3.zero; } }
        /// <summary>自检用：治疗飘字当前那条补间（时长应 == <see cref="HealTextHoldSeconds"/>；没在播 = null）。</summary>
        public Tween HealTweenNow { get { return _healSeq; } }

        // ==================================================================
        //  攻击类型底光（原版 `HighlightAttackType.highlightObject` = `Highlight` / `Highlight ranged`）
        //  2026-10-25（`CF`）：驱动源已改成「**这张卡自己的** attackType」（见 `SetOwnAttackType`）。
        // ==================================================================

        /// <summary>底光直径 ÷ 卡宽。算式见 `SetOwnAttackType`</summary>
        const float GlowOfCard = 1.13f / 2.09f;      // ≈ 0.54

        /// <summary>底光的 z。判据 = **画在数字后面**（原版 `Highlight.SortingOrder` 比数值小）。
        /// <para>⚠️ 两个形态用的是**同一套覆盖层 z**（`GlowZ` 一手牌/2D 卡面；场上也是它）：
        /// 场上四个数值在 **`BoardInfoZ`(−0.150)** ⇒ 底光（−0.01）比它们**远 0.14**，
        /// 同队列下先画 ⇒ 数字盖在底光上（这就是要的效果）。
        /// 🔴 **但队号必须与本卡那一段相同**（`MakeGlow` 里那句 `SetRenderQueue(Queue)`）——
        /// 只靠 z 不够：底光原来是 3000、比整张卡的 2962..2979 **大** ⇒ 队列优先，z 再远也压在最上面。</para></summary>
        const float GlowZ = -0.01f;

        ImageQuad _glowMelee, _glowRanged;
        bool _glowTried;

        /// <summary>
        /// ⛔ **已弃用（2026-10-25 `CF`）—— 新代码请用 <see cref="SetOwnAttackType"/>。**
        ///
        /// <para>它原来是「**这张卡是合法目标**」那条语义的入口（点亮哪一颗数值格的底光）。
        /// 原版**没有**这条语义：底光与数值的 `DOScale` 都由**这张卡自己的 `attackType`** 驱动
        /// （判据 → `资料/普查产出_1025/J2_判据_卡面高亮族与渲染残项.md` §A1·6 —— 两条触发链
        /// `ChangeAttackType` / `ToggleCombatPreviewHighlight` 逐句读过，受体永远是「这张卡自己的两颗实例」）。</para>
        ///
        /// <para>为什么还留着：`Battle/BattleDriver.HighlightTargets` 与 `Editor/BattleScene` 仍在调它。
        /// 这里只做**同义转发**（旧三态 → <see cref="AttackTypes"/>），行为与原来逐位一致（含 `CurrentGem`），
        /// 好让那几个调用点先编得过、不静默丢功能。</para>
        ///
        /// 🔴 **还欠的一跳（不在本文件）**：`Battle/BattleDriver.cs` 要把这条调用改成「把**这张卡自己的**
        /// `UnitState.LastAttackType` 喂给 `SetOwnAttackType`」（`CommitCommand` 在 `:13997` 写那个字段；
        /// 广播点 ≈ `RefreshAll` / `HighlightTargetsCore`）。改完「目标合法性」只剩
        /// `SetHighlight(CardHighlightState.ValidTarget)` 那圈描边（原版 `CardHighlight.ChangeState`
        /// 只改描边色 + 卡体缩放，**不碰底光**）。</para>
        ///
        /// <para>⚠️ 底光那层的锚点是钉死的（四条独立证据）：在**卡体那颗攻击数值格上**，与数字同点同心、
        /// `SortingOrder` 比数字小（画在数字**后面**）、旋转 identity 且等比缩放。
        /// **不是**单位脚下的地板，也**不是** 2D 印刷卡面那颗（那条链运行时 `highlightObject` 全是 0）。
        /// 尺寸：sprite 128 px ÷ PPU 100 = 1.28 × `m_LocalScale` 0.655 = 0.84 卡单位
        /// ⇒ **0.84 / 2.09 = 54% 卡宽**（`GlowOfCard`）。
        /// ⚠️ **2026-10-25（`CF`）就地订正（铁律 5）**：这段注释原来还写着「点亮瞬间再 ×
        /// `scaleModifierOnHighLight` **1.35** = 1.13」—— **不成立**：1.35 / 1.2 在原版**只作用在数值 TMP
        /// （`textCounter`）**上，底光节点 `Highlight` / `Highlight ranged` 身上**没有**
        /// `HighlightAttackType` 组件 ⇒ 底光自己不会被 ×1.35。那句把两件事混成一件了。
        /// （`GlowOfCard` 的 1.13 是「0.84 → 54% 卡宽」那个算式的历史中间量，**不是**高亮倍数。）</para>
        /// </summary>
        public void SetTargetGem(TargetGem gem)
        {
            SetOwnAttackType(gem == TargetGem.Melee ? AttackTypes.Melee
                           : gem == TargetGem.Ranged ? AttackTypes.Ranged
                           : AttackTypes.None);
        }

        /// <summary>现在点亮的是哪颗数值格（自检断言用 —— 截图上看不出「点亮的是不是该亮的那颗」）。
        /// ⚠️ 2026-10-25（`CF`）起它只是 <see cref="OwnAttackType"/> 的同义映射（旧读口，别在新代码里用）。</summary>
        public TargetGem CurrentGem { get; private set; }

        // ==================================================================
        //  卡面「攻击类型高亮」（原版 `HighlightAttackType`，每卡 2 个实例）—— 2026-10-25（`CF`）
        // ==================================================================
        // 🔴 判据全文 → `资料/普查产出_1025/J2_判据_卡面高亮族与渲染残项.md` §A1 / §A2（逐句反编译 + UnityPy 直读）
        //    · 组件：`Melee Attack Container`（attackType=1）/ `Range Attack Container`（attackType=2）各一个；
        //      `Spell` / `Active` **没有**实例 ⇒ 那两档卡面底光与数字都不动。
        //    · 行为（`HighlightAttackType__HighlightIfNeeded.c` 逐句）：
        //        if (textCounter == null) return;
        //        if (|originalSize − Vector3.zero|² < 1e-10f) originalSize = textCounter.localScale;   ← 惰性捕获
        //        if (selected == attackType) { highlightObject.SetActive(true);  DOScale(originalSize × 倍数, 0.15f); }
        //        else                        { DOScale(originalSize, 0.15f);      highlightObject.SetActive(false); }
        //      **没有 `SetEase`** ⇒ 吃 DOTween 全局默认（本工程 `Assets/Resources/DOTweenSettings.asset`
        //      `defaultEaseType: 6` = `OutQuad`）。
        //    · 倍数 = `scaleModifierOnHighLight`，**两族两个值**：战斗 `CardPrefab`（手牌 / 场上 / 督军）= **1.35**
        //      （全库 304 个实例里只有这 2 个）；其余 302 个（menus / 13 场 HUD / 主菜单）= **1.2**（= ctor 默认值）。
        //      ⚠️ 这与选择器那颗钮的 1.3 / 0.3 s / `OutBack` 是**另一个类**（`CardDisplayAttackTypeButton`），
        //      两套参数别混（判据 → 同文件 §A3）。
        //    · 我们这侧的对应物：原版 `textCounter`（那颗数值 TMP 的 transform）= 我们的 `_stats[StatMelee]` /
        //      `_stats[StatRanged]`；原版 `highlightObject` = `_glowMelee` / `_glowRanged`。
        // ✅ **手牌 / 详情窗那一档：原版本来就没有这一套**（2026-10-25 `CF` 现核，父链 + MB 计数两条证据）：
        //    · 整份 `CardPrefab` 里 `HighlightAttackType` **只有 2 个**，父链都是
        //      `CardPrefab / Board Elements / 3DBody / Base Attack Counters / {Melee,Range} Attack Container`；
        //    · 2D 卡那一支（`CardPrefab / 2DCard / Front / Card Info / Melee Attack Container / Melee Attack Text`）
        //      **没有** `HighlightAttackType`；场上 / 手牌是**两棵互斥的子树**
        //      （`BattleCardUI.SetObjectVisibility` 同时切 `Card2DController` 与 `ToggleBody3D`）。
        //    ⇒ 原版的「底光 + ×1.35」**只作用于场上（3D 卡体）那一档**。我们这侧的 `_stats` 就是那四个
        //    3D 数值节点的对应物；手牌/详情窗的四个数是**另一套**（`Card Info` 里，
        //    我们目前烘在 `_info` 贴图里）—— ⛔ **别为了「让手牌也能放大」把它们接进来**，
        //    那是**多做的自我设计**（铁律 11 例外①：原版本身就没有）。
        // ==================================================================

        /// <summary>原版 `HighlightAttackType.scaleTime` —— `private const float = 0.15`（`dump.cs:38853`）。
        /// 两条独立证据：① 反汇编里那个常量 `DAT_1834b3178` 用 `read_literal` 读 4 字节 = `0.15f`；
        /// ② 符号表那条 `const`。两者逐位一致。</summary>
        public const float AttackHighlightScaleTime = 0.15f;

        /// <summary>`DAT_1834b2ba8 = 1e-10f` —— 原版判「这个 `Vector3` 还算不算零」用的**平方**距离阈值
        /// （= (1e-5)²；同族判据 `NetworkingPeer.AlmostEquals` / `CardScript.AlreadyInPosition` 都用它）。</summary>
        const float VectorZeroSqr = 1e-10f;

        /// <summary>战斗 `CardPrefab`（手牌 / 场上 / 督军）的 `scaleModifierOnHighLight` = **1.35**。
        /// 出处：`bundle_battleprefabs_vfxandmisc_assets_all` 的两个实例（全库 304 个里只有这 2 个）。</summary>
        public const float AttackScaleModifierFight = 1.35f;

        /// <summary>其余卡 prefab（menus 82 · 13 个 arena 各 16 · mainmenu 10 · staticgeneralassets 2 = 302 个）
        /// 的 `scaleModifierOnHighLight` = **1.2** —— 与 ctor 里的默认值（`0x3f99999a`）同值。</summary>
        public const float AttackScaleModifierOther = 1.2f;

        /// <summary>现在高亮的是**这张卡自己的**哪个攻击类型（原版 `HighlightAttackTypes(int)` 的那个入参）。</summary>
        public AttackTypes OwnAttackType { get; private set; }

        /// <summary>`HighlightAttackType.scaleModifierOnHighLight` —— **按卡 prefab 族**取两档之一。
        /// 🔴 族判据复用 <see cref="ArtUsesFightFamily"/>（`Hand` / `Board` = 战斗 `CardPrefab`；`Full` = 通用
        /// `CardUI`）—— 与「立绘中心 y 分两族」是**同一个族**（原版 304 个 prefab 实例的分布正落在这两族上）。</summary>
        public float AttackScaleModifier
        {
            get { return ArtUsesFightFamily ? AttackScaleModifierFight : AttackScaleModifierOther; }
        }

        /// <summary>点亮 / 熄灭「**这张卡自己的**攻击类型」那一格（原版 `BasicCardUI.AttackTypeChanged`
        /// → 逐实例 `HighlightAttackType.HighlightIfNeeded`）。
        /// <para>传 <see cref="AttackTypes.None"/> = 两颗都熄、两个数值都回基准 —— 原版 `CardHighlight.Start`
        /// 与 `BattleManager._ResolveAttack` 里的敌方卡走的就是这一支。</para>
        /// <para>⚠️ 原版**每调用一次就重开一次** 0.15 s 补间（没有「值没变就跳过」的短路）—— 这里照抄；
        /// ⇒ 别每帧调（那会永远推不动，停在起点）。</para></summary>
        public void SetOwnAttackType(AttackTypes type)
        {
            OwnAttackType = type;
            // 旧读口（`SetTargetGem` 时代）留一份同义映射：`Editor/BattleScene` 那几条断言还在读它。
            CurrentGem = type == AttackTypes.Melee ? TargetGem.Melee
                       : type == AttackTypes.Ranged ? TargetGem.Ranged
                       : TargetGem.None;
            HighlightAttackTypes();
        }

        /// <summary>原版 `CardHighlight.HighlightAttackTypes(int)`：遍历那两个实例逐个 `HighlightIfNeeded`。</summary>
        void HighlightAttackTypes()
        {
            HighlightOneAttackStat(StatMelee, OwnAttackType == AttackTypes.Melee);
            HighlightOneAttackStat(StatRanged, OwnAttackType == AttackTypes.Ranged);
        }

        /// <summary>一个 `HighlightAttackType` 实例的 `HighlightIfNeeded`（逐句对照见本节开头那张伪代码）。</summary>
        void HighlightOneAttackStat(int s, bool lit)
        {
            var mr = _stats[s];
            // 原版第一句 `if (textCounter == null) return;` —— **连底光都不碰**（不是「只跳过缩放」）。
            if (mr == null) return;

            var tr = mr.transform;
            var baseScale = StatBaseScaleOf(s);
            float mult = lit ? AttackScaleModifier : 1f;
            if (_statScaleTween[s] != null && _statScaleTween[s].IsActive()) _statScaleTween[s].Kill();
            // 原版 `DOScale(textCounter, originalSize × 倍数, 0.15f)`，**没有 `SetEase`**
            // ⇒ 显式传 `DOTween.defaultEaseType`（本工程 = 6 `OutQuad`），与「不写 `SetEase`」逐位等价。
            // ⛔ 别在这里换成 `OutBack` / `OutQuad` 之类的硬编码值（那就成了「我们挑的缓动」）。
            var tw = tr.DOScale(baseScale * mult, AttackHighlightScaleTime);
            _statScaleTween[s] = CardTween.Use(tw, DOTween.defaultEaseType, this);

            // 底光 = 原版 `highlightObject.SetActive(...)`。
            // ⚠️ 原版**不防御**「命中但 `highlightObject == null`」（那一支直接空引用抛，用来暴露预制体接错）；
            //    我们这边的 null 只有一个**正当**来源 —— `Resources/Art/` 被删（`CardArt.Ui` 取不到图）。
            //    那一档不能抛（它是全工程支持的配置）⇒ **警告一次**，出声、不静默。
            if (lit) EnsureGlow();
            var q = s == StatMelee ? _glowMelee : _glowRanged;
            if (q != null) q.gameObject.SetActive(lit);
            else if (lit && !_glowMissingWarned)
            {
                _glowMissingWarned = true;
                Debug.LogWarning("[CardView] 攻击类型底光取不到图（`40K_melee_glow` / `40K_ranged_glow`）"
                               + " ⇒ 数值照常放大，但底光不亮。多半是删了 `Resources/Art/`"
                               + "（那一档是支持的配置；若美术在，就是 `CardArt.Ui` 的名字对不上）。");
            }
        }

        /// <summary>原版 `originalSize`（`+0x48`）的惰性捕获 —— **判据只有这一处**（高亮与 bump 都要它）。
        /// 条件照抄原版：`|base − zero|² < 1e-10f` 时才把**当刻**的 `localScale` 记下来。
        /// ⚠️ 原版没处理「初始 scale 真的是 0」的边界（那种格每次调用都会重记）—— 同上，别自己加短路。</summary>
        Vector3 StatBaseScaleOf(int s)
        {
            var mr = _stats[s];
            if (mr == null) return Vector3.one;
            if (_statBase[s].sqrMagnitude < VectorZeroSqr) _statBase[s] = mr.transform.localScale;
            return _statBase[s];
        }

        // ---- 自检读口（截图上看不出「放大的是不是那一格、放大了多少」）------------------

        /// <summary>自检用：第 `s` 格数值**现在**的 `localScale`（层没建 ⇒ 零向量）。</summary>
        public Vector3 StatScaleNow(int s)
        {
            var mr = _stats[s];
            return mr != null ? mr.transform.localScale : Vector3.zero;
        }

        /// <summary>自检用：第 `s` 格的**基准**（原版 `originalSize`；还没被捕获过 = 零向量）。</summary>
        public Vector3 StatBaseScale(int s) { return _statBase[s]; }

        /// <summary>自检用：第 `s` 格那条 `DOScale`（没起过 / 已被 bump 杀掉 = null）。
        /// 读 `Duration()` 验 0.15 s。</summary>
        public Tween StatHighlightTween(int s) { return _statScaleTween[s]; }

        /// <summary>自检用：那一颗底光**亮着没有**（`ranged` = 远程那颗；没建出来也算 false）。</summary>
        public bool AttackGlowOn(bool ranged)
        {
            var q = ranged ? _glowRanged : _glowMelee;
            return q != null && q.gameObject.activeSelf;
        }

        /// <summary>自检用：两颗底光建出来没有 —— `Resources/Art/` 被删时是 false（那几条断言算「没验到」）。</summary>
        public bool AttackGlowBuilt { get { return _glowMelee != null || _glowRanged != null; } }

        /// <summary>自检用：把某一格的 `localScale` 硬写成某值（验惰性捕获的**阈值判别**）。⛔ 生产代码别调。</summary>
        public void StatScaleForTest(int s, Vector3 v)
        {
            var mr = _stats[s];
            if (mr != null) mr.transform.localScale = v;
        }

        /// <summary>自检用：把某一格的**基准**硬写成某值（同上）。⛔ 生产代码别调。</summary>
        public void StatBaseForTest(int s, Vector3 v) { _statBase[s] = v; }

        void EnsureGlow()
        {
            if (_glowTried) return;
            _glowTried = true;
            float dia = Width * GlowOfCard;
            // 🔴 **2026-10-12：位置按形态取** —— 底光的锚点是「在卡体那颗攻击数值格上、**与数字同点同心**」
            //    （四条独立证据见 `SetTargetGem` 的注释），而**同一个数值格在两种形态下位置不同**：
            //    场上是 `BoardMeleeAt/BoardRangedAt`、2D 卡面是 `MeleeAt/RangedAt`。
            //    原来无条件用 2D 那一套 ⇒ **场卡上的底光偏了 (0.156, 0.264) 卡单位**（约半径的 30%）。
            //    ⚠️ 底光只在**场上**点亮：唯一的调用点 `BattleDriver.HighlightTargets` 只遍历
            //       `_myUnits` / `_foeUnits`（场卡视图）⇒ 按形态取即可，不必再跟 `SetFace` 同步。
            bool board = _faceMode == CardFace.Board;
            // 🔴 2026-10-18（W15）：2D 那一档也跟 `FaceStatAt01` 走 —— 与数字/命中区**同一份判据**
            //    （底光锚点就是「那颗数值格」，三处各写一套迟早分家）。
            _glowMelee = MakeGlow("40K_melee_glow",
                                  board ? BoardMeleeGlowAt : FaceStatAt01(StatMelee, _frameTex), dia, "glow_melee");
            _glowRanged = MakeGlow("40K_ranged_glow",
                                   board ? BoardRangedGlowAt : FaceStatAt01(StatRanged, _frameTex), dia, "glow_ranged");
        }

        ImageQuad MakeGlow(string artName, Vector2 at, float dia, string name)
        {
            var q = ImageQuad.Create(transform, CardArt.Ui(artName), Vector3.zero, dia,
                                     new Vector2(0.5f, 0.5f), name);
            if (q == null) return null;
            // `at` 是卡面归一化坐标（x 从左、y 从**顶部**），换成卡的局部坐标
            q.transform.localPosition = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height, GlowZ);
            // 🔴 **2026-10-12（用户真机报「场上没有近战/远程/血量数值」）：底光必须【当场】盖上
            //    这张卡当前的队列号。** 这是 `A1665`「一张卡 = 一段队列」里**唯一漏网的一层** ——
            //    别的每一层建完都紧跟一次 `StampQueue`/`Label.SetRenderQueue`，只有它没有 ⇒
            //    `ImageQuad.Create` 给的是 `Sprites/Default` 的 `Transparent` = **3000**，
            //    而场上那一段由 `BoardLayout.QueueOf` 给的是 **2962..2979**
            //    ⇒ **底光整张画在这张卡的所有层之上**；而这两张 sprite 的**中心是不透明的**
            //    （`Art/原版/battleatlasui/40K_{melee,ranged}_glow.png` 亲读：亮盘/准星圈 + 不透明内芯）
            //    ⇒ **正好把它盖住的那一格数值涂掉**（实拍里「亮红盘 / 紫圈里没有数」就是它）。
            //
            // ⚠️ **号必须取 `Queue`（本卡那一段）** —— `SetQueue` 是「每张卡一段队列」的唯一实现，
            //    ⛔ 别在这儿另发明一个号、也别去动 `BoardLayout` 那套号段（见 `QCard` 那段约束）。
            // ⚠️ **只盖上号、不动 z**：同号之后卡内层序仍靠 z，而 `GlowZ`(−0.01) 比四个数值
            //    (−0.150) **远** ⇒ 底光仍画在数字**后面**，与原版 `Highlight.SortingOrder 比数字小` 同义。
            // ⚠️ 底光是**懒建**的（`SetTargetGem`→`EnsureGlow`），调用点在最后一次 `SyncBoard` 的
            //    `SetQueue` **之后** ⇒ 不在这里补一次，下一次 `SetQueue` 之前它就一直是 3000。
            q.SetRenderQueue(Queue);
            // 🔴 2026-10-21：底光是懒建的（`SetQueue` 之后才建）⇒ 当场补层号，
            //    否则同队列内仍按距离排、会盖住数值（真机「红盘/紫圈里没有数」就是这个）。
            ApplySortingOrderTree(q.transform);
            q.gameObject.SetActive(false);
            return q;
        }

        // ==================================================================
        //  可点矩形（原版 `CardUI/2DCard` 上那两颗 `BoxCollider2D`）—— 2026-10-17（`T09` 卡体批 ②）
        // ==================================================================
        // 🔴 **判据 = 原版 `2DCard` 上的两颗 `BoxCollider2D`**（卡 prefab 原件在
        //    `bundle_battleprefabs_vfxandmisc_assets_all/`，**不在战斗场景包里**）：
        //    · 主面 **1.97 × 3.02 @ (0,0)** —— `BoxCollider2D_7520353667969031104.json`
        //    · 第二面 **1.25 × 2.53 @ (0,−2.62)** —— `BoxCollider2D_2582590304383179712.json`
        //      （只在 `inHandShowing` 打开；用途 = 接住「卡被放大抬走后指针留在原处」那一段）
        //    · in-hand 档 = 序列化 `colliderHeightMultiplierInHand = **1.01**`（⚠️ ctor 默认 `1.2` **不是**游戏用的）：
        //      `size.y ×= 1.01` · `offset.y −= (newH − origH) × 0.5` ⇒ **1.97 × 3.0502 @ y −0.0151**
        //      （**上沿 1.51 不动**，只往下拉长）。
        //    ⚠️ **本批只做【行为级】前两档**（常态 / in-hand）。第三档（悬停放大 = 主面 ∪ 第二面）**还没落**：
        //      我们手里**没有** `inHandShowing` 这个状态 —— 手牌唯一的悬停态在 `CardInteraction._hovered` /
        //      `HandLayout.hoveredIndex`，不在 `CardView` 上，而 `Contains` 正是**判出它**的那一步
        //      ⇒ 从这里读不到（详见 `资料/普查产出_1017/BSC62/W13_卡体②行为级.md` §二）。
        //      ⇒ 第三档与「真建 collider」同属 `T09` 第 4/5 步，同批落。
        // 🔴 **标架 = 内层 `2DCard`（`_tiltRoot`）**，⛔ 不是卡根 —— 两颗 collider 都挂在 `2DCard` 上，而
        //    `AutoCardRotation`（10/10/10）也挂在同一 GO ⇒ **可点区跟着卡面的倾摆一起转**、
        //    与「画到哪」同标架（`A2093`）。⇒ 也顺手解掉 `CardFan.cs:151`「碰撞与卡自己的转角无关」
        //    与 `A2093` 的**互斥**记录（那条互斥记录 → `资料/任务线/战斗线/准则与做法.md` §六 坑 13）。
        // ⚠️ `CardFan.HitRatioX/Y` / `HitCyDropPx`（批① 用的**旧 `UI Collider`** 那套）**仍保留**：
        //    它们现在是**战斗展示窗「拿不到 `CardView`」时的轴对齐退路**（`CardDisplayWindow.cs:874`）
        //    与**菜单详情窗** `CardDetailPopup.BuildCardHit` 的判据 —— 与本文件这套**不是同一个来源**，
        //    ⛔ 别把两套混成一套（合并要另开一件、另配断言）。

        /// <summary>可点矩形的**半宽**（卡局部单位）= 原版主面 `1.97 ÷ 2` = **0.985**（**三档共用**，in-hand 不改宽）。</summary>
        public const float HitHalfW = 1.97f * 0.5f;
        /// <summary>**常态 / 板上**可点矩形的**半高** = 原版主面 `3.02 ÷ 2` = **1.51**。</summary>
        public const float HitHalfH = 3.02f * 0.5f;
        /// <summary>常态可点矩形**中心**相对卡心的局部 y 偏移 = 原版主面 `offset.y` = **0**。</summary>
        public const float HitCenterDy = 0f;
        /// <summary>**in-hand** 档的**半高** = 主面 `3.02 × colliderHeightMultiplierInHand(1.01) ÷ 2`
        /// = **1.5251**（原版矩形 = `1.97 × 3.0502`）。</summary>
        public const float HitHalfHInHand = HitHalfH * 1.01f;
        /// <summary>**in-hand** 档的中心 y 偏移 = `−(3.0502 − 3.02) ÷ 2` = **−0.0151**
        /// （原版那一步只把矩形**往下**拉长 ⇒ **上沿仍是 1.51**）。</summary>
        public const float HitCenterDyInHand = -(HitHalfHInHand - HitHalfH);

        // ------------------------------------------------------------------
        //  真 collider（`Physics2D`）—— 2026-10-19（`T09` 步骤 B 前半，`W57`）
        // ------------------------------------------------------------------
        // 🔴 **口径**：几何判据从「手算矩形」（`Contains`）搬到**真 `BoxCollider2D`** ——
        //    `Contains` 本身**不动**（它继续当一致性断言的对照物，灭自证要靠两份独立来源）。
        //    原版依据 = `2DCard` 上那两颗 `BoxCollider2D`（尺寸/偏移见上面那一段），
        //    这里只建**主面那一颗**（第二面 = 悬停放大那第三档，仍没落，见文件头）。

        /// <summary>原版 `2DCard` 所在的层 = **5（Unity 内置 `UI`）**。
        /// 判据（一手）：`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/GameObject/2DCard.json`
        /// 的 `m_Layer: 5` —— 同一份 JSON 的组件表里就挂着那两颗 `BoxCollider2D`
        /// （`7520353667969031104` / `2582590304383179712`，尺寸见上面）。
        /// <para>⚠️ **这个值只作【原版判据记录】，⛔ 不拿来给 GameObject 赋层。** 本仓 `SetLayer`
        /// 把整张卡**递归**设成 **0**（手牌 / 卡池 / 换牌）或 `ArenaSlots.ArenaLayer = 8`（场上），
        /// 两台相机的 `cullingMask` 就按这两层分（`Editor/BattleScene.cs:28821/29200`）。
        /// 把 `2DCard` 改挂 5 会连带改掉 `EffectAnchorHand`（= `FaceRoot` = `2DCard`）的层，
        /// 破坏 `BattleDriver.cs:15510` 记的那条不变量「手牌 = 层 0 / 场上 = ArenaLayer」。
        /// ⇒ 物理查询走 <see cref="CardHitLayerMask"/>。</para></summary>
        public const int CardHitLayer = 5;

        /// <summary>`Physics2D` 查询用的层掩码。
        /// <para>卡实际会落在两层：**0**（手牌 / 卡池 / 换牌）与 **`ArenaSlots.ArenaLayer` = 8**
        /// （场上 —— `SetLayer` 递归改的那一档），而**出牌途中也会切层**
        /// （`BattleDriver` 飞行中先留在 0、落地才 `SetLayer(ArenaLayer)`）⇒ 掩码必须同时盖住两层。</para>
        /// <para>⚠️ 取 `Physics2D.DefaultRaycastLayers`（= 除 `Ignore Raycast` 外全部）而**不是**写死
        /// `(1 &lt;&lt; 0) | (1 &lt;&lt; 8)`：卡的层是**运行时**改的，写死一个掩码在换层那一帧就会
        /// **漏掉自己**（静默）。目标碰撞体由 `HitByCollider` **逐颗比对**（必须是本卡那颗），
        /// 别的层上的 collider 不会造成误判。</para></summary>
        public static int CardHitLayerMask { get { return Physics2D.DefaultRaycastLayers; } }

        /// <summary>本卡的**真 `BoxCollider2D`**（挂在 `_tiltRoot` = 原版 `2DCard` 那一级）。
        /// `Build` 里建（`EnsureHitCollider`），in-hand 档切换时由 `SyncHitCollider` 刷 `size/offset`。
        /// ⚠️ 这是**主面**那一颗；**第二颗**（原版 `colliderShowInBig`）叫 `_hitColliderBig`，声明在**类尾**（2026-10-25 R16）。</summary>
        BoxCollider2D _hitCollider;

        /// <summary>`OverlapPoint` 的结果缓冲。64 张卡叠在同一点的上限远高于实际
        /// （手牌最多十来张、场上最多十来张）—— 满了只影响**更靠后**的 collider；
        /// 本方法只关心**本卡那一颗**，所以正常情况无影响，但真撞到上限会在下面出声。</summary>
        static readonly Collider2D[] _hitQueryBuf = new Collider2D[64];
        static bool _hitBufFullWarned;

        /// <summary>`Physics2D.OverlapPoint` 的过滤器：**不采触发器**（原版 `m_IsTrigger: false`）、
        /// 层掩码 = <see cref="CardHitLayerMask"/>。深度（z）不参与 —— `Physics2D` 本来就没有 z，
        /// 「谁压谁」由调用方按 z 自己排（见 <see cref="HitByCollider"/> 的警告）。</summary>
        static readonly ContactFilter2D _hitFilter = MakeHitFilter();

        static ContactFilter2D MakeHitFilter()
        {
            var f = new ContactFilter2D();
            f.useTriggers = false;     // 原版 `BoxCollider2D.m_IsTrigger = false`
            f.useLayerMask = true;
            f.layerMask = CardHitLayerMask;
            return f;
        }

        /// <summary>把真 collider 建到 `_tiltRoot` 上（原版 `2DCard` 那一级；标架与 `Contains` 相同）。
        /// 幂等：已有就复用（`Build` 可能重入）。`isTrigger` 照原版设 `false`。</summary>
        void EnsureHitCollider()
        {
            var frame = HitFrame();                 // `_tiltRoot` 没建时它会出声一次并退回卡根
            var col = frame.GetComponent<BoxCollider2D>();
            if (col == null) col = frame.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            _hitCollider = col; EnsureBigHitCollider(frame);   // 第二颗（`colliderShowInBig`）同 GO，见类尾
            SyncHitCollider();
        }

        /// <summary>把 `HitRect()` 那一档写进真 collider（**判据只有 `HitRect` 一处**）。
        /// 常态 `size = 1.97 × 3.02`、`offset = (0, 0)`；
        /// in-hand `size = 1.97 × 3.0502`、`offset = (0, −0.0151)`（= 原版 `SetColliderInHand` 那一步：
        /// `size.y ×= colliderHeightMultiplierInHand(1.01)`、`offset.y −= (newH − origH) × 0.5`；
        /// 反编译实证 → `CardCollider2D__SetColliderInHand.c`，常数 `0.5` 在 `GameAssembly.dll +0x34b07b4`）。</summary>
        void SyncHitCollider()
        {
            if (_hitCollider == null) return;
            HitRect(out float halfH, out float centerDy);
            _hitCollider.size   = new Vector2(HitHalfW * 2f, halfH * 2f);
            _hitCollider.offset = new Vector2(0f, centerDy);
            _hitCollider.enabled = _hitEnabled;      // 命中开关（原版 `CardCollider2D.Toggle`）—— 唯一写点
        }

        // ---- 命中开关（原版 `CardCollider2D.Toggle(bool)` 的等价物）----
        /// <summary>这张卡现在**吃不吃命中**。原版 = `CardCollider2D.Toggle(option)` → `boxCollider2D.enabled`
        /// （`CardCollider2D__Toggle.c`），由 `CardScript.SetColliderNotInHand(state)` 按状态给：
        /// `removedFromGame(11)` / `inCemetery(6)` ⇒ 关（`CardScript__RemoveFromGameCard.c:22-26`、
        /// `CardCollider2D__SetColliderNotInHand.c:11-24`）。
        /// <para>⚠️ 开关只管**两条命中判据**（<see cref="Contains"/> / <see cref="HitByCollider"/>）——
        /// **不动几何**（`HitRect` 照旧，几何归常态档）。</para>
        /// <para>🔴 **2026-10-25（第 3 轮）：只有 helper、还没有调用方** —— 接线点在上游状态机
        /// （`BattleDriver` 收到 `removedFromGame` 时），那一半不在本批白名单。⛔ 别在这里猜状态。</para></summary>
        public bool HitEnabled { get { return _hitEnabled; } }

        /// <summary>开/关这张卡的命中（缺省 **true**）。已建 collider ⇒ 当场生效；没建 ⇒ 由 `SyncHitCollider` 落。</summary>
        public void SetHitEnabled(bool on)
        {
            if (_hitEnabled == on) return;
            _hitEnabled = on;
            if (_hitCollider != null) _hitCollider.enabled = on; if (!on) ShowInBigColliderEnabled = false;   // 原版 `Toggle(false)` 连第二面一起关（`CardCollider2D__Toggle.c:6-9`）
        }
        bool _hitEnabled = true;

        // ------------------------------------------------------------------
        //  第二套矩形：原版 uGUI 那颗 `UI Collider`（**只给战斗展示窗**用）—— 2026-10-17（`BSC62·W16`）
        // ------------------------------------------------------------------
        // 🔴 **为什么需要第二套**：上面那套 `BoxCollider2D` 是原版**手牌/棋盘**的送达路径
        //    （`CardCollider` 实现 8 个 pointer/drag 接口），⛔ **不要动**；
        //    而**战斗展示窗**的点击在原版走的是另一颗件：`2DCard/UI Collider`（uGUI，挂
        //    `UIGenericEventCatcher`）。硬证据：
        //    · `BasicCardUI.uiEventCatcher`（`+0xd0`）正指向它 —— `BasicCardUI__get_UIEventCatcher.c:5`
        //    · `CardDisplayWindow.AddCardsListeners.c:25/30` 往它的 `OnClick` 上 `AddListener`
        //    · 该节点 `Image.raycastTarget = 1`，`UICollider.OnEnable` 把 `BoxCollider2D.size` 设成 `RT.rect`
        //    ⇒ ⚠️ `R5`/`EW12` 那句「`UI Collider` 三事件全空 ⇒ 不是送达路径」**漏了运行时 `AddListener`**，
        //      已由 `BSC62` 只读诊断现核推翻（判定与处方 → `资料/普查产出_1017/D_新红11条诊断.md` A1）。
        // 🔴 **判据 = 解包原件字段**（与 `Core/CardFan.cs` 的「点击区」同一份原件）：
        //    该节点 `m_AnchorMin=(0,0)` · `m_AnchorMax=(1,1)`（**拉伸锚**）· `m_AnchoredPosition=(0,−0.02)`
        //    · `m_SizeDelta=(−0.2,−0.44)`；父 `2DCard` = **`2.0927 × 3.3313`**（= 本类的 `Width/Height`）
        //    ⇒ rect = **1.8927 × 2.8913 卡单位、中心比卡心低 0.02**。
        //    ⚠️ **没有 in-hand 档**：`colliderHeightMultiplierInHand` 是 `2DCard` 上那颗 `BoxCollider2D`
        //       的属性，拉伸锚这颗 `RectTransform` 不随它变 ⇒ 只有一档。
        // ⚠️ 标架**仍是内层 `2DCard`（`_tiltRoot`）** —— `UI Collider` 就是 `2DCard` 的子件
        //    （`RectTransform_-5674508438467339328.json` 的 `m_Father` = `2DCard`）⇒ 与
        //    `Contains` 同标架，**扇形角与倾摆都跟着转**（`EW12` 那条修不丢）。
        /// <summary>原版 `UI Collider` 矩形**半宽**（卡局部单位）= `(2.0927 − 0.2) ÷ 2` = **0.94635**。</summary>
        public const float UiColliderHalfW = (Width - 0.2f) * 0.5f;
        /// <summary>原版 `UI Collider` 矩形**半高**（卡局部单位）= `(3.3313 − 0.44) ÷ 2` = **1.44565**。</summary>
        public const float UiColliderHalfH = (Height - 0.44f) * 0.5f;
        /// <summary>原版 `UI Collider` 矩形**中心**相对卡心的局部 y 偏移 = `m_AnchoredPosition.y` = **−0.02**。</summary>
        public const float UiColliderCenterDy = -0.02f;

        bool _hitFrameWarned;
        /// <summary>`HitByCollider` 那条警告的「只出声一次」闩 —— **与 `_hitFrameWarned` 分开**：
        /// 两条警告的成因不同（一个是标架退回卡根、一个是 collider 没建），共用一个闩会吞掉后一条。</summary>
        bool _hitColliderWarned;

        /// <summary>可点矩形所在的那一级节点 = 内层 `2DCard`（`_tiltRoot`，`Build` 里建；
        /// `AutoCardRotation` 挂在它身上 ⇒ 它转 = 倾摆）。
        /// ⚠️ 取不到就退回卡根并**出声一次**（⛔ 不静默：「退回卡根」= 不跟倾摆 = 判据之外的状态）。</summary>
        Transform HitFrame()
        {
            if (_tiltRoot != null) return _tiltRoot;
            if (!_hitFrameWarned)
            {
                _hitFrameWarned = true;
                Debug.LogWarning("[CardPresentation] " + name + "：内层 `2DCard`（`_tiltRoot`）还没建 ⇒ "
                               + "命中判定退回**卡根**（= 不跟倾摆，与原版那两颗 `BoxCollider2D` 挂在 `2DCard` 上不符）；"
                               + "唯一成因 = `Build()` 没跑过（正常路径 `Create` 就建）");
            }
            return transform;
        }

        /// <summary>这张卡当前该用**哪一档**可点矩形（⚠️ 两档、不是三档 —— 悬停放大那档见文件头）：
        /// **`CardFace.Hand` ⇒ in-hand 档**（`1.97 × 3.0502 @ y −0.0151`）；
        /// **其余一律常态档**（`1.97 × 3.02 @ (0,0)` —— 含 `CardFace.Board`/`Full`）。
        /// 判据 = 原版那两颗 `BoxCollider2D` + 序列化 `colliderHeightMultiplierInHand`（见文件头）。</summary>
        void HitRect(out float halfH, out float centerDy)
        {
            bool inHand = _faceMode == CardFace.Hand && !_inHandBigGeometry;   // 「showing in hand」档 ⇒ 主面回常态矩形（= 原版 `ResetCollider`，判据见类尾 `SetCollidersShowBig`）
            halfH   = inHand ? HitHalfHInHand     : HitHalfH;
            centerDy = inHand ? HitCenterDyInHand : HitCenterDy;
        }

        // ------------------------------------------------------------------
        //  真 collider 版的命中查询 —— 2026-10-19（`T09` 步骤 B 前半，`W57`）
        // ------------------------------------------------------------------
        // 🔴 **`HitByCollider` 与 `Contains` 的关系**：两条**独立**的判据，同一个矩形。
        //    · `Contains`  = 手算（`InverseTransformPoint` + 三个字面量）；
        //    · `HitByCollider` = 问物理世界（真 `BoxCollider2D`）。
        //    断言要求两者在**0° 倾摆**下逐点一致（采样网格见 `W57` 计划 §步骤 B 断言②）；
        //    ⇒ 两边都写错成同一个值才会「一起绿」—— 所以断言还必须有灭自证那条
        //    （把 collider 临时放大 1.2 倍必须红）。
        // ⚠️ **已知偏离（与原版同构，⛔ 不是偷懒）**：`BoxCollider2D` 只支持 **Z 轴旋转**，
        //    而卡面的 `AutoCardRotation` 是 **X/Y 两轴 ±10° 的 3D 倾摆**（`Core/AutoCardRotation.cs`）
        //    ⇒ 倾摆起来时 collider **不跟**（`Contains` 跟）。实测差最大 ~2.6% 卡高。
        //    原版的卡也挂在 `2DCard` 上、同样的限制 ⇒ 这是「照原版」；断言要在 0° / ±10° 两档各测一次
        //    并**把差异记下来**（⛔ 别试图用 3D collider —— 原版没有，且会引入 z 排序）。
        // ⚠️ `Physics2D` **没有 z**：`OverlapPoint` 会同时命中前后叠着的所有卡
        //    ⇒ 「谁最上面」必须由调用方按 z 取最前（`Hand/CardInteraction.Frontmost` 那一侧，W2 的活）。
        //    ⛔ 别指望查询顺序 —— 它是物理世界的内部顺序，不是渲染顺序。

        /// <summary>世界坐标在不在这张卡的**真 collider** 上（= `Physics2D` 版 <see cref="Contains"/>）。
        /// <para>标架/矩形与 `Contains` **同一个**（`_tiltRoot` + `HitRect()`）—— 区别只是判据来自
        /// 真 `BoxCollider2D`，不是手算。</para>
        /// <para>🔴 **查询前必须 `Physics2D.SyncTransforms()`**：卡的位移/旋转是**每帧直接写
        /// `transform`** 的（`SetPose` / 布局 / 飞行 / 出牌），而 `Physics2D` 只在
        /// `FixedUpdate` 或显式同步时把 transform 灌进物理世界
        /// （`Physics2D.autoSyncTransforms` 默认 `false`）⇒ 不同步就会读到**上一帧**的 collider。
        /// 这是本步骤的**头号静默坑**：画面完全正常、命中差一帧。
        /// ⚠️ 代价 = 一次**全局**同步（把所有脏 transform 刷进物理世界）⇒ W2 在每帧循环里
        /// 多张卡连续查询时，**每帧调一次**即可（本方法内部仍照调，幂等）。</para>
        /// <para>实现 = `Physics2D.OverlapPoint` + `ContactFilter2D` + 预分配缓冲，然后**逐颗比对
        /// 是不是本卡那一颗** —— 不能只看「有没有命中」，因为叠着的别的卡也会被返回。</para>
        /// <returns>`_hitCollider` 没建起来（`Build` 没跑过）时**退回 `Contains` 并出声一次**；
        /// 正常路径永远走 collider。</returns></summary>
        public bool HitByCollider(Vector3 world)
        {
            if (!_hitEnabled) return false;      // 原版 `CardCollider2D.Toggle(false)` ⇒ 物理层也查不到（见 `SetHitEnabled`）
            var col = _hitCollider;
            if (col == null)
            {
                if (!_hitColliderWarned)
                {
                    _hitColliderWarned = true;
                    Debug.LogWarning("[CardPresentation] " + name + "：`BoxCollider2D` 还没建 ⇒ "
                                   + "`HitByCollider` 退回手算 `Contains`（几何相同，但**不是**真 collider）；"
                                   + "唯一成因 = `Build()` 没跑过（正常路径 `Create` 就建）");
                }
                return Contains(world);
            }
            Physics2D.SyncTransforms();
            int n = Physics2D.OverlapPoint(world, _hitFilter, _hitQueryBuf);
            if (n >= _hitQueryBuf.Length && !_hitBufFullWarned)
            {
                _hitBufFullWarned = true;
                Debug.LogWarning("[CardPresentation] `HitByCollider` 结果缓冲已满（" + _hitQueryBuf.Length
                               + " 颗）⇒ 更靠后的 collider 没被检查到；实际场景不该有这么多卡叠在一点。");
            }
            for (int i = 0; i < n; i++)
                if (_hitQueryBuf[i] == col || (_hitColliderBig != null && _hitQueryBuf[i] == _hitColliderBig)) return true;   // 两颗都算「在本卡上」（原版两颗挂在同一 GO ⇒ 同一套 pointer 处理）
            return false;
        }

#if UNITY_EDITOR
        // ---- 只读探针（只给自检；生产零调用）—— 2026-10-19（`T09` 步骤 B 前半，`W57`）----
        //  后续断言（`Editor/BattleScene.cs`，由 T 写）要靠它们**直接量真 collider**，
        //  而不是拿本文件的常量反推（那样就是自证）。⛔ 不要在这里开写口。

        /// <summary>自检用：本卡的真 `BoxCollider2D`（`_tiltRoot` 上那颗；没建 ⇒ `null`）。
        /// 断言「存在 + `isTrigger == false` + 层 == 卡当前 `SetLayer` 层」用它。</summary>
        public BoxCollider2D ColliderForTest { get { return _hitCollider; } }

        /// <summary>自检用：真 collider 的 `size`（卡局部单位）。期望值从**原版字面量**来
        /// （常态 `1.97 × 3.02` / in-hand `1.97 × 3.0502`），⛔ 不从 `HitHalfW` 反推。</summary>
        public Vector2 ColliderSizeForTest { get { return _hitCollider != null ? _hitCollider.size : Vector2.zero; } }

        /// <summary>自检用：真 collider 的 `offset`（卡局部单位）。常态 `(0, 0)` / in-hand `(0, −0.0151)`。</summary>
        public Vector2 ColliderOffsetForTest { get { return _hitCollider != null ? _hitCollider.offset : Vector2.zero; } }

        /// <summary>自检用：真 collider 所在 GameObject 的层（手牌 0 / 场上 `ArenaSlots.ArenaLayer`；
        /// 没建 ⇒ `−1`）。用途 = 钉住「换层跟着 `SetLayer` 走、⛔ 不是被常量写死」。</summary>
        public int ColliderLayerForTest { get { return _hitCollider != null ? _hitCollider.gameObject.layer : -1; } }
#endif

        /// <summary>世界坐标在不在这张卡上。
        /// 🔴 **2026-10-17（`T09` 卡体批 ②）起底矩形 = 原版 `2DCard` 的 `BoxCollider2D`**：标架 = 内层
        /// `2DCard`（⇒ **跟卡面的倾摆一起转**），矩形按 `CardFace` 取档 ——
        /// **常态** `|x| ≤ 0.985` 且 `|y| ≤ 1.51`；**in-hand**（`CardFace.Hand`）`|x| ≤ 0.985` 且
        /// `|y + 0.0151| ≤ 1.5251`（= 主面 × 1.01，**只往下长**）。
        /// <para>改前 = 批① 那颗 `UI Collider` 的 **1.8927 × 2.8913 @ −0.02**（小 4.1% 宽 / 4.5% 高）。</para>
        /// <para>⚠️ **两个标架都已经跟上了扇形角**（它由 `SetPose` 写在**卡根**上，`_tiltRoot` 是它的子件）
        /// —— 换标架真正补上的是**倾摆**：`AutoCardRotation` 只写 `_tiltRoot` 的 `localRotation`。</para></summary>
        public bool Contains(Vector3 world)
        {
            if (!_hitEnabled) return false;      // 原版 `CardCollider2D.Toggle(false)` ⇒ 不吃命中（见 `SetHitEnabled`）
            var l = HitFrame().InverseTransformPoint(world);
            HitRect(out float halfH, out float centerDy);
            return (Mathf.Abs(l.x) <= HitHalfW && Mathf.Abs(l.y - centerDy) <= halfH) || BigRectContains(l);   // 并集 = 原版两颗 collider（见类尾 `BigRectContains`）
        }

        /// <summary>世界坐标在不在**原版 `UI Collider`** 那个矩形里（**只给战斗展示窗**
        /// <see cref="CardDisplayWindow.HitSlot"/> 用；⛔ 手牌/棋盘仍走 <see cref="Contains"/>）。
        /// 标架与 <see cref="Contains"/> **相同** = 内层 `2DCard`（`_tiltRoot`）⇒ 命中区跟着卡的
        /// **扇形角与倾摆**一起转；只有矩形数值换回 `UI Collider` 那套（**1.8927 × 2.8913 @ y −0.02**，
        /// 判据见上面「第二套矩形」那一块的注释）。
        /// <para>⚠️ 为什么不直接复用 <see cref="Contains"/>：`BoxCollider2D` 主面（`1.97 × 3.02 @ 0`）
        /// 是原版**手牌/棋盘**的送达件，而展示窗的点击在原版由 `2DCard/UI Collider` 送达 ——
        /// 两套矩形各有各的判据，⛔ 别合并、也别把 `Contains` 本身改回旧矩形。</para></summary>
        public bool ContainsUiCollider(Vector3 world)
        {
            var l = HitFrame().InverseTransformPoint(world);
            return Mathf.Abs(l.x) <= UiColliderHalfW
                && Mathf.Abs(l.y - UiColliderCenterDy) <= UiColliderHalfH;
        }

        // ---- 🆕 2026-10-19（`T03`）：**可点矩形边界探针**（⛔ 只给自检，生产不用）----
        //  为什么加这两颗：`Contains` 的三个调用点（`CardInteraction.HitTest` · `BattleDriver.HitSlot` ·
        //  `MulliganPanel.CardAt`）**一条边界断言都没有**（`T03` §六-4 / `资料/普查产出_1016战斗判据/
        //  T03_命中与可点区域判据.md` §五）。要断「边界上 / 外 1 px」就得把**卡局部标架**里的点
        //  换成世界点，而那个标架（内层 `2DCard`）是私有的 ⇒ 给自检开一个**与 `Contains` 同一个**
        //  `HitFrame()` 的读口（⛔ 别让断言自己再拼一份 `transform.InverseTransformPoint` —— 那会把
        //  「标架选错」这类缺陷一起放过）。
        //  ⚠️ 断言里的**期望值仍然从原版 collider 的字面量来**（1.97×3.02 / ×1.01 / −0.0151），
        //     ⛔ 不从本文件的常量反推 —— 否则就是自证。

        /// <summary>自检用：把**可点矩形那一级标架**（<see cref="HitFrame"/> = 内层 `2DCard`）里的
        /// 一个点换算成世界坐标。`x/y` 是本类的局部单位（与 `Width/Height` 同一把尺）。</summary>
        public Vector3 HitLocalToWorldForTest(float x, float y)
        {
            return HitFrame().TransformPoint(new Vector3(x, y, 0f));
        }

        /// <summary>自检用：这张卡当前是 **in-hand 档**吗（`CardFace.Hand`）——
        /// 断言据此选**原版**那一档的期望矩形（手上 `3.02×1.01` / 其余 `3.02`）。</summary>
        public bool IsHandFaceForTest { get { return _faceMode == CardFace.Hand; } }

        /// <summary>和 <see cref="Contains"/> 同一套算法（**同一标架、同一矩形**），但**四周外扩 `margin`**
        /// （同一局部单位）。用途 = 悬停那条链的**廉价前置门**：先问一句「指针在不在这一带」，
        /// 不在就**别去跑那次 `TMP_TextUtilities.FindIntersectingLink`**。
        ///
        /// 为什么要有这道门 —— 判据是原版：`TextTooltipController.__Update` 每帧**只做一次字节比较**
        /// （`+0x48` 只在 `OnPointerEnter` 置 1），指针压着那一个 TMP 时才调
        /// `FindIntersectingLink(那一个 TMP, 鼠标, 相机)` ⇒ **一次调用、一个 TMP**；
        /// ⛔ **原版不是每帧遍历所有触发器**（`d:/2/tools/decomp_full/TextTooltipController__Update.c`
        /// 与 `__CheckForLinkCollision.c:29`）。我们是每帧轮询 ⇒ 只能靠这道门把范围缩回来。
        ///
        /// ⚠️ 用 `margin` 而不是精确 `Contains`，是因为原版靠 `OnPointerEnter` 触发、
        /// 边界处比「精确矩形」宽松一点点 —— 留外扩免得贴边时反而不如原版灵敏。
        /// ⚠️ `margin` 是**局部单位**（与 `Width`/`Height` 同一把尺），卡被缩放时它的世界大小会跟着变
        ///   —— 这是有意的：本方法问的是「在这张卡的坐标系里近不近」。
        /// 🔴 **2026-10-16（`T09` 卡体批 ①）：底矩形跟着 `Contains` 一起换**（原来是整卡矩形、挂卡根）。
        ///   复算过一遍：唯一的消费者 `BattleDriver.TickTraitTip` 传 `margin = 0.15`，而底矩形每边只缩
        ///   **0.1（左右）/ 0.24（上）/ 0.2（下）**卡单位 ⇒ 带 `margin` 的那一圈**横向仍盖满整卡宽**、
        ///   纵向盖住卡身高度的 **0.03…0.985**，而关键词那段文字在卡面中段 ⇒ **这道门的松紧没有变**
        ///   （只是标架从卡根换成跟着倾摆的 `2DCard`，与文字实际所在的层对上了）。
        /// 🔴 **2026-10-17（`T09` 卡体批 ②）：底矩形再跟着换成 `BoxCollider2D` 那套**（`1.97×3.02`
        ///   ＋ in-hand 档 `1.97×3.0502`）⇒ 比批① **更大**（横向每边 +0.03865；常态档上沿 +0.08435、
        ///   下沿 +0.04435 卡单位）
        ///   ⇒ 这道门对唯一的消费者**只会更松**（`margin = 0.15` 不变）：「关键词在卡面中段」这个前提不受影响。</summary>
        public bool ContainsNear(Vector3 world, float margin)
        {
            var l = HitFrame().InverseTransformPoint(world);
            HitRect(out float halfH, out float centerDy);
            return Mathf.Abs(l.x) <= HitHalfW + margin
                && Mathf.Abs(l.y - centerDy) <= halfH + margin;
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
            // 🔴 2026-10-18（W15）：**2D 卡面那一档要跟「数字画在哪」同一份判据**
            //    （`FaceStatAt01`）—— 数字现在跟着卡框凹槽走，命中区还停在原版容器常量上的话，
            //    就会出现「看着在这颗圈里、点上去弹出隔壁那颗的说明」。
            if (HitAt(l, board ? BoardMeleeAt  : FaceStatAt01(StatMelee,  _frameTex))) return StatMelee;
            if (HitAt(l, board ? BoardRangedAt : FaceStatAt01(StatRanged, _frameTex))) return StatRanged;
            if (HitAt(l, board ? BoardArmourAt : ArmourAt))                            return StatArmour;
            if (HitAt(l, board ? BoardHealthAt : FaceStatAt01(StatHealth, _frameTex))) return StatHealth;
            // 🔴 **费用那一格要跟「画不画」走同一份判据**（2026-10-14 第三十一会话 · `A2006` 顺手发现）：
            //    原版对督军是 `Toggle(costCtrl, bVar6)`（`d:/2/tools/decomp_full/BasicCardUI__SetRawCardData.c:84`，
            //    督军 `bVar6` 恒 0）⇒ **容器整个 `SetActive(false)`**，挂在它上面的触发器**也不响应射线**。
            //    我们这套没有 uGUI 射线靶，是按「点到数值中心的距离」判的 ⇒ 不加这道门，
            //    督军卡上那块**空白**仍会弹出费用 tooltip（消费者：`BattleDriver.cs:15252` / `DeckRuntime.cs:1181`）。
            // ⚠️ 判据**只此一处** `ShowsCost`（与 `:901` 的显示口径同一份，铁律 10 第 5 条：两处写同一条规则 = 迟早不一致）。
            //    ⚠️ `!board` 那一半照 `:901` —— 场上那套（`CardFace.Board`）本来就不画费用。
            //    🔴 2026-10-18（W15）：费用格的位置也**跟着卡框走**（`FaceCostAt01`，与画它那一处同一份）。
            if (!board && ShowsCost(Data) && HitAt(l, CostAt01(_frameTex))) return StatCost;
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
                case StatMelee:  at = board ? BoardMeleeAt  : FaceStatAt01(StatMelee,  _frameTex);  break;
                case StatRanged: at = board ? BoardRangedAt : FaceStatAt01(StatRanged, _frameTex); break;
                case StatArmour: at = board ? BoardArmourAt : ArmourAt; break;
                case StatHealth: at = board ? BoardHealthAt : FaceStatAt01(StatHealth, _frameTex); break;
                case StatCost:   at = CostAt01(_frameTex); break;
                default: return transform.position;
            }
            // 归一化坐标 y 从**顶部**数（和 `CostAt` 那批同口径）⇒ 局部 y 要翻过来
            // 🔴 **2026-10-25（`M2`）**：非场上那套要补 `Front` 的 `+0.08`（卡面层挂在 `2DCard/Front` 下）；
            //    场上那套取 `BoardMeleeAt` 一族（3DBody 空间）⇒ 不补。同 `HitAt`。
            return transform.TransformPoint(new Vector3((at.x - 0.5f) * Width,
                                                       (0.5f - at.y) * Height + (board ? 0f : FrontAposY), 0f));
        }

        bool HitAt(Vector3 local, Vector2 at)
        {
            // 🔴 **2026-10-25（`M2`）**：`local` 是**卡根**空间（`StatAt` 那句 `transform.InverseTransformPoint`），
            //   而 `at` 是**卡面归一化**坐标 —— 非场上那套卡面层现在挂 `2DCard/Front`（+0.08，见 `FrontAposY`）
            //   ⇒ 要补回那 0.08，否则「看着在这颗圈里、点上去弹出隔壁那颗的说明」会以 2.4% 卡高的偏移回来。
            //   场上那套（`BoardMeleeAt` 一族）本来就在 3DBody / 卡根空间 ⇒ **不加**。
            float dy = _faceMode == CardFace.Board ? 0f : FrontAposY;
            var c = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height + dy, 0f);
            return (local - c).sqrMagnitude <= StatHitR * StatHitR;
        }

        /// <summary>摆位：位置(世界) + 绕 Z 的倾角 + 缩放。手牌扇形/战场落位都调它。
        /// ⚠️ 缩放**不直接写 `localScale`** —— 高亮那层会在基础缩放上乘一个系数，见 `ApplyScale`。</summary>
        /// <param name="clip">🆕 2026-10-08（A181）：**可选**视口裁切（画布像素 · 左上原点）。
        /// 传了就用它当这张卡的裁切（`null` = **不改**当前状态：已经裁着的继续按老矩形重裁 ——
        /// 位姿变了要重算，见下面「视口裁切」那一大段）。</param>
        public void SetPose(Vector3 pos, float rotZ, float scale, PxRect? clip = null)
        {
            // 🆕 2026-10-18（`A1549` 尾巴）：布局每次摆位都顺手比一次费用档位 —— 切完
            //   `SmallScreenUI` 之后，下一次布局就会把已建好的卡刷到新档（没变时只是一次 bool 比较）。
            SyncCostTier();
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

        /// <summary>把这张卡**补间到**「局部位置 + 绕 Z 倾角 + 缩放」—— **每帧都经 <see cref="SetPose"/> 写**。
        ///
        /// <para>🔴 **2026-10-19（`T04`）：为什么不能用 `CardTween.ToPose`**（该入口已于 2026-10-24 `A11`
        /// **整段删除** —— 下面这段是「⛔ 别照原样加回来」的理由，退役注见 `Core/CardTween.cs:62`）——
        /// 那个入口走 `DOMove/DORotate/DOScale`，直接写 `transform.localScale`，而本类的缩放是
        /// 「**基础缩放 × 高亮系数**」（<see cref="BaseScale"/> / `ApplyScale`）⇒ tween 期间 `_baseScale`
        /// **一直停在起点值**：补间一结束，任何一次 `SetPose`（手牌重排 / 战场落位 / 切分辨率）或
        /// `SetHighlightScale` 都会把卡**弹回旧缩放**（静默、图上只表现为「换位后位置对、大小不对」）。
        /// 判据 = 原版 `CardDisplayWindow.ChangeCardPosition` 的两条 tween 写的是**同一个 transform**，
        /// 而原版的缩放**没有**我们这层「基础 × 高亮」⇒ 等价物必须把 `_baseScale` 一起带过去。
        /// ⚠️ **2026-10-24 就地订正（铁律 5 · `A11`）**：本行原写「菜单版详情窗 `Shell/CardDetailPopup`
        /// 仍是 `CardTween.ToPose`（同一潜伏缺陷）」—— **已不成立**：`CardDetailPopup.SwapToFront` 那两处
        /// 已改走本函数，且 `CardTween.ToPose` **本身已整段删除**（全仓零代码调用方，退役注见
        /// `Core/CardTween.cs:62`）。⇒ 那条潜伏缺陷**已消**，别按旧注释去「修」一个不存在的入口。</para>
        ///
        /// <para>⚠️ **走 `SetPose` 的连带开销**：`SyncCostTier()`（一次 bool 比较）· `_blobShadow.Sync()`
        /// （只有场上 3D 卡有影子；展示窗的卡是 `CardFace.Full` ⇒ 没有）· `ApplyClip()`（只有裁着的卡）。
        /// ⚠️ **`pos` 是【局部坐标】**（同 `SetPose`）—— ⛔ 别传世界坐标（被删掉的 `CardTween.ToPose`
        /// 收的是世界坐标，两者只在父链 `lossyScale == 1` 且父件在原点时同值；该入口 2026-10-24 `A11` 已删，
        /// 这条差异说明留着防「照旧签名猜坐标」）。</para>
        /// <returns>补间本体（`duration &lt;= 0` 时直接落终态、返回 null）。调用方自己 `OnComplete`。</returns>
        public Tween TweenPose(Vector3 pos, float rotZ, float scale, float duration, Ease ease)
        {
            // 🆕 2026-10-24（`A3`）：**同一条位姿链只留一条补间** —— 上一次没跑完的先杀掉。
            //   为什么必须在这里杀：`DOTween.To` 建出来的补间默认**没有 target**，
            //   `transform.DOKill()` 按 `target` 比 ⇒ 杀不到它。而调用方（`HandLayout.Place`
            //   第一句、`Battle/MulliganPanel.cs:681/977`）历史上一直靠 `transform.DOKill()`
            //   防「补间叠补间」⇒ 见下面 `SetTarget(transform)` 那一句（把那层的语义接回来）。
            KillPoseTween();

            if (duration <= 0f)
            {
                SetPose(pos, rotZ, scale);
                return null;
            }
            Vector3 fromPos = transform.localPosition;
            float fromRot = transform.localEulerAngles.z;
            float fromScale = _baseScale;
            float dRot = Mathf.DeltaAngle(fromRot, rotZ);      // 走短弧；与 DOTween 的 `DORotate` 同向
            float t = 0f;
            var tw = DG.Tweening.DOTween.To(() => t, x =>
            {
                t = x;
                SetPose(Vector3.LerpUnclamped(fromPos, pos, x),
                        fromRot + dRot * x,
                        Mathf.LerpUnclamped(fromScale, scale, x));
            }, 1f, duration);
            var used = CardTween.Use(tw, ease, this);
            // 🔴 2026-10-24（`A3`）：把 tween 的 `target` 指成**卡根 Transform**。
            //   `CardTween.ToPose`（旧入口，2026-10-24 `A11` 已删）那条 Sequence 的
            //   `DOMove/DOLocalRotate/DOScale` 自带 target = transform，
            //   ⇒ 外部 `transform.DOKill()` 能杀掉；换成 `DOTween.To` 之后这层语义会**静默丢掉**
            //   （`HandLayout.Place` 防叠、`MulliganPanel` 掐重排补间都靠它）。这里把它接回来。
            used.SetTarget(transform);
            // 🔴 2026-10-24（`E4`）：再给这条补间一个**本卡唯一**的 id —— `KillPoseTween` 的「非 play」兜底要用
            //   （`TweenExtensions.Kill` 在 `DOTween.initialized == false` 时会直接 return，判据见 `KillPoseTween`）。
            //   用**独立对象**而不是 `this`/`transform`：`DOTween.Kill(x)` 是「target **或** id 命中」，
            //   拿 `transform` 当 id 会把本卡上 `DOMove/DOScale` 那一族一起收掉。
            used.SetId(_poseTweenId);
            _poseTween = used;
            return used;
        }

        /// <summary>位姿补间本体（`TweenPose` 建的那一条）—— 见 <see cref="TweenPose"/> 的 doc。
        /// ⚠️ 只用来「同一张卡不叠两条位姿补间」；要停它请调 <see cref="KillPoseTween"/> 或 `transform.DOKill()`。</summary>
        Tween _poseTween;
        /// <summary>`TweenPose` 那条补间的 id（每张卡一个独立对象）。
        /// 唯一用途 = `KillPoseTween` 在 `DOTween.initialized == false`（编辑器批处理 / 非 play）时的兜底击杀。
        /// ⚠️ ⛔ 别把它换成 `transform` / `this` —— `DOTween.Kill(x)` 是「target **或** id 命中」 ⇒ 会误杀别的补间。</summary>
        readonly object _poseTweenId = new object();

        /// <summary>掐掉在飞的**位姿补间**（只掐 `TweenPose` 那一条，不碰这个 Transform 上的别的补间）。
        /// 给「要当场 snap 的场景」用 —— 比如 `HandLayout.Place` 的 `!animateRelayout` 那一支：
        /// 不掐的话上一次重排的补间会继续把刚 snap 好的位姿改回去（静默、只在动起来时看得见）。</summary>
        public void KillPoseTween()
        {
            // 🔴 **2026-10-24（`E3` · `DG3` 红 #5）：⛔ 别再加回 `IsActive()` 闸。**
            //   原来写的是 `if (_poseTween != null && _poseTween.IsActive()) _poseTween.Kill();`
            //   —— 闸不成立时**什么都不做**，下一句又把引用丢掉 ⇒ 那条补间**还活着、再也没人杀得掉**，
            //   继续把位姿推走（静默；`CardBaseDemo ③1` 实测：割在 40% 处，推完一整段后到了终点）。
            //   `DG.Tweening.TweenExtensions.Kill` 对**已完成 / 已被杀**的补间本身就是 no-op ⇒
            //   无条件杀是安全的；判据见 `CardBaseDemo.AssertHandPoseTween` ③ 那一支（它连着断
            //   「掐完 `tw.IsActive() == false`」这半条自证）。
            //
            // 🔴 **2026-10-24（`E4` · `DG3` 红 #5 的第二半）：`TweenExtensions.Kill` 在非 play 环境里是空转。**
            //   DOTween.dll 的 IL（`TweenExtensions::Kill` 开头）是：
            //     `ldsfld DOTween::initialized; brtrue.s IL_0008; ret`
            //   —— `initialized == false` 时**直接 return**。而**编辑器批处理（`-executeMethod`，不进 play）
            //   里 `initialized` 永远是 false**：`DOTween.Init()` 自己在 `!Application.isPlaying` 时返回 null，
            //   `InitCheck()`（`DOTween.To` 每次创建补间都会调）同样**不调** `AutoInit()`。
            //   ⇒ 批处理里 `tw.Kill()` 静默空转：实测 `CardBaseDemo ③1` 掐完 `tw.IsActive()` 仍为 True、
            //   位姿被继续推到终点（起点 0.7301 → 割在 40% 的 0.5508 → 终点 0.4500）。
            //   🔑 **同一批里的对照**：③2 用 `transform.DOKill()` 却是绿的 —— 那条走 `DOTween.Kill(target)` →
            //   `TweenManager.FilteredOperation`，**这条链不读 `initialized`**。⇒ 「补间本身没问题，是 `Kill` 这条路被闸住」。
            //   兜底：**按 id 杀**（`DOTween.Kill(_poseTweenId)`，同样不读 `initialized`，且只命中本卡这一条）。
            //   ⛔ 别改成 `transform.DOKill()`：那会把本卡 Transform 上别的补间一起收掉，违背本函数的约定。
            Tween tw = _poseTween;
            _poseTween = null;
            if (tw == null) return;
            tw.Kill();                                  // play 模式下的正常路径（批处理里会被 DOTween 空转）
            if (tw.IsActive()) DOTween.Kill(_poseTweenId);   // 非 play 兜底（按 id，精确到这一条）
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
        //   · 网格按「**每 4 个顶点一块**」逐块裁：块内要求是 **轴对齐矩形**、四角齐、无顶点色
        //     （卡框/立绘/数值/宝石/SDF 影是 1 块；🆕 **文字底板自 `A1933` 起是 6 块 / 24 顶点**）。
        //     别的网格（3D 卡体那种）**只挡「整块进出」**，部分越界时**出声一次不裁**（⛔ 不静默）。
        //     🔴 **2026-10-14 就地订正（铁律 5）**：本行原来写「网格只支持 **4 顶点**」——
        //        那是当时的实情，`A1933` 把文字底板改成 6 块之后**已不成立**，
        //        而且它当场把一个真缺陷做出来了（`A1702③`：卡池第一排的底板画到页头空带上）。
        //        ⇒ 改成逐块裁，同时解掉账 `A1728`（「`CropLayer` 只支持 4 顶点」）。
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
            // 🔴 **换算**：画布矩形 → 卡面那一层（`2DCard/Front`）的局部系。
            //    卡在画布上轴对齐时它还是个轴对齐矩形 ⇒ 与「沿画布轴裁」逐位等价；
            //    转过的话取外接矩形（会多画一点）⇒ 出声一次。
            //    ⚠️ **2026-10-25（`M2`）**：标架从 `FaceRoot`（`2DCard`）改成 `FaceContentRoot`（`Front`）
            //    —— 被裁的网格（`CropLayer` 直接吃 meshes 顶点）都在 `Front` 局部系里。
            //    ⚠️ 两个例外：`rim` 与四个数值层**不挂 `Front`**（见那两处注释）⇒ 它们被裁时会差
            //    `+0.08`（≈2.4% 卡高）。这两层平时 `enabled=false` / 只在场上出现，且裁切本身是
            //    「转过时取外接矩形」的近似档 ⇒ **按近似处理，不另开第二套标架**（如实记在这里）。
            var fr = FaceContentRoot;
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
            // 🔴 **2026-10-14（第二十九会话）**：TMP 的文字网格是**多顶点**的，而它**另有专管** ——
            //   `ApplyClip` 收尾那个 `ClipTextMesh` → `MenuDraw.ClipTmpMesh`（全工程唯一一份）。
            //   ⇒ 本函数对 TMP **只保留上面那两档**（整块在外 ⇒ 关掉；整块在内 ⇒ 不动），
            //     到这就交还给文字那条路。否则它会掉进下面「不是 4 顶点轴对齐矩形」那一支，
            //     刷出**误导**的告警（文案说「没裁」，其实 TMP 那半裁了）—— 实测卡池那一屏刷了一整排。
            if (mr.GetComponent<TMPro.TextMeshPro>() != null) return;

            // ③ 部分越界：**按「每 4 个顶点一块」逐块裁**。
            //    🔴 **2026-10-14（第二十九会话）就地订正** —— 这一段原来写死「**只支持 4 顶点**」
            //      （`vs.Length != 4` ⇒ `WarnNotCroppable` 然后**整层不裁**）。而卡面「文字底板」自 `A1933`
            //      起是 **6 块 / 24 顶点**的网格（`TextBgQuad`，照原版的 `Sliced` 画）
            //      ⇒ 它一旦**部分越界**就整层不裁 ⇒ 实测把**卡池第一排的底板画到页头空带上**
            //      （`DeckScene.Run` 的 `A1702③` 当场红 —— 这正是账 `A1940` 记的那条风险的**第一次兑现**）。
            //      ⇒ 改成**逐块**处理：块内那份「矩形求交 + 按参数截 uv」的逻辑与原来**逐字相同**，
            //      只是从「整网格算一块」变成「每 4 个顶点算一块」；4 顶点网格（块数 = 1）与旧行为**逐位相同**。
            //      ⚠️ 这同时把账 `A1728`（「`CropLayer` 只支持 4 顶点」）那条限制**解掉**了。
            //    ⚠️ **仍要求**：顶点数能被 4 整除 · 每块是**轴对齐矩形**且四角齐（顶点序 BL·BR·TR·TL，
            //      `SpriteQuad` 与 `TextBgQuad` 都是这个序）· 网格**没有顶点色 / uv3 / uv4**
            //      —— 任一条不满足就**出声一次、不裁**（⛔ 不静默）。
            if (vs.Length % 4 != 0
                || (mesh.colors != null && mesh.colors.Length == vs.Length)
                || (mesh.uv3 != null && mesh.uv3.Length == vs.Length)
                || (mesh.uv4 != null && mesh.uv4.Length == vs.Length))
            {
                WarnNotCroppable(mesh.name);
                return;
            }

            bool hasUv2 = mesh.uv2 != null && mesh.uv2.Length == vs.Length;
            var nv = new List<Vector3>(vs.Length);
            var nUv = new List<Vector2>(vs.Length);
            var nUv2 = hasUv2 ? new List<Vector2>(vs.Length) : null;
            var nTris = new List<int>(vs.Length / 4 * 6);

            for (int q = 0; q < vs.Length; q += 4)
            {
                float qx1 = float.MaxValue, qx2 = float.MinValue, qy1 = float.MaxValue, qy2 = float.MinValue;
                for (int i = q; i < q + 4; i++)
                {
                    if (vs[i].x < qx1) qx1 = vs[i].x;
                    if (vs[i].x > qx2) qx2 = vs[i].x;
                    if (vs[i].y < qy1) qy1 = vs[i].y;
                    if (vs[i].y > qy2) qy2 = vs[i].y;
                }
                if (!IsRectQuad(vs, q, qx1, qx2, qy1, qy2)) { WarnNotCroppable(mesh.name); return; }

                // 本块**整块在视口外** ⇒ 丢掉它，其余块照常（⛔ 不是把整层关掉）
                if (qx2 <= lx1 || qx1 >= lx2 || qy2 <= ly1 || qy1 >= ly2) continue;

                int b = nv.Count;
                if (qx1 >= lx1 && qx2 <= lx2 && qy1 >= ly1 && qy2 <= ly2)
                {
                    // 本块**整块在视口内** ⇒ 原样搬（⛔ 一个字节都不改）
                    for (int i = 0; i < 4; i++) { nv.Add(vs[q + i]); nUv.Add(mesh.uv[q + i]); }
                    if (hasUv2) for (int i = 0; i < 4; i++) nUv2.Add(mesh.uv2[q + i]);
                }
                else
                {
                    float cx1 = Mathf.Max(qx1, lx1), cx2 = Mathf.Min(qx2, lx2);
                    float cy1 = Mathf.Max(qy1, ly1), cy2 = Mathf.Min(qy2, ly2);
                    // 参数化：本块在**自身矩形**里的归一化坐标（左 0 / 右 1、下 0 / 上 1）
                    var sq = new float[4]; var tq = new float[4];
                    for (int i = 0; i < 4; i++)
                    {
                        sq[i] = (vs[q + i].x - qx1) / (qx2 - qx1);
                        tq[i] = (vs[q + i].y - qy1) / (qy2 - qy1);
                    }
                    float sA = (cx1 - qx1) / (qx2 - qx1), sB = (cx2 - qx1) / (qx2 - qx1);
                    float tA = (cy1 - qy1) / (qy2 - qy1), tB = (cy2 - qy1) / (qy2 - qy1);
                    nv.Add(new Vector3(cx1, cy1, vs[q].z));
                    nv.Add(new Vector3(cx2, cy1, vs[q].z));
                    nv.Add(new Vector3(cx2, cy2, vs[q].z));
                    nv.Add(new Vector3(cx1, cy2, vs[q].z));
                    float[] ns = { sA, sB, sB, sA }, nts = { tA, tA, tB, tB };
                    var c4 = new Vector2[4];
                    for (int i = 0; i < 4; i++) c4[i] = mesh.uv[q + i];
                    var got = RemapChannel(c4, sq, tq, ns, nts, 4);
                    for (int i = 0; i < 4; i++) nUv.Add(got[i]);
                    if (hasUv2)
                    {
                        var d4 = new Vector2[4];
                        for (int i = 0; i < 4; i++) d4[i] = mesh.uv2[q + i];
                        var g2 = RemapChannel(d4, sq, tq, ns, nts, 4);
                        for (int i = 0; i < 4; i++) nUv2.Add(g2[i]);
                    }
                }
                nTris.Add(b); nTris.Add(b + 2); nTris.Add(b + 1);   // 顶点序 BL·BR·TR·TL（与各层原件同序）
                nTris.Add(b); nTris.Add(b + 3); nTris.Add(b + 2);
            }

            // 一块都没剩下 ⇒ 这一层整体关掉（同 ①：记**原来的** enabled，还原时别把它打开）
            if (nv.Count == 0)
            {
                _crops.Add(new LayerCrop { mr = mr, wasEnabled = mr.enabled });
                mr.enabled = false;
                return;
            }

            var m = new Mesh { name = mesh.name + "_clip" };
            // ⚠️ 用 `SetVertices/SetUVs/SetTriangles`（**按长度自己配**）—— 原来那句 `m.uv = RemapChannel(…, 4)`
            //    的注释警告过「实参传错会让数组尺寸对不上、网格当场作废」；多块之后顶点数不再是常量，
            //    这条路把那个坑从**手写长度**改成**由列表决定**。
            m.SetVertices(nv);
            m.SetUVs(0, nUv);
            if (hasUv2) m.SetUVs(1, nUv2);
            m.SetTriangles(nTris, 0);
            m.RecalculateBounds();

            var rec = new LayerCrop { mr = mr, mf = mf, original = mesh, cropped = m, wasEnabled = mr.enabled };
            mf.sharedMesh = m;
            _crops.Add(rec);
        }

        /// <summary>4 个顶点是不是**一个轴对齐矩形的四个角**（每个角都落在 `{x1,x2} × {y1,y2}` 上、且四角齐）。
        /// 不是的话这套「矩形求交 + 按比例截 uv」就不成立 ⇒ 调用方只挡整块并出声。</summary>
        static bool IsRectQuad(Vector3[] vs, float x1, float x2, float y1, float y2)
        {
            return IsRectQuad(vs, 0, x1, x2, y1, y2);
        }

        /// <summary>同上，但**从第 `off` 个顶点起**看 4 个 —— 🆕 2026-10-14（`CropLayer` 改成逐块裁）。
        /// 判据逐字相同：4 个角都落在 `{x1,x2} × {y1,y2}` 上、且四个角各出现一次（双线性插值才可解）。</summary>
        static bool IsRectQuad(Vector3[] vs, int off, float x1, float x2, float y1, float y2)
        {
            int mask = 0;
            for (int k = 0; k < 4; k++)
            {
                int i = off + k;
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

        void OnDestroy() { DisposeCrops(); DisposeGlitchMaterial(); }

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

        /// <summary>**基础缩放**（不含高亮系数）—— `ApplyScale` 的那一半底数。
        /// <para>🔴 **2026-10-19（`T04`）：换位补间必须经 <see cref="TweenPose"/> 写它** ——
        /// `CardTween.ToPose`（旧入口；2026-10-24 `A11` 已删，这里说的是它当时的写法）直接写
        /// `transform.localScale`，`_baseScale` 会**停在换位前的槽位值**上；
        /// 之后任何一次 `SetPose` / `SetHighlightScale`（→ `ApplyScale`）都会把卡**弹回旧缩放**（静默）。
        /// 原先 `CardDisplayWindow.SwapToFront` 就是这条潜伏缺陷（2026-10-16 顺手发现、账在战斗线 `T04`）。</para>
        /// 自检口：换位收尾后断「它 = 该槽的 `CardFan.Hpx(i) / (Height×108)`」。</summary>
        public float BaseScale { get { return _baseScale; } }

        /// <summary>**只改基础缩放**并当场落到 `localScale`（走 <see cref="ApplyScale"/>）——
        /// 给「逐帧按战场深度重算缩放」那类调用用（原版 `CardScript` 的 `Sequence.OnUpdate` 每帧
        /// `transform.localScale = scale`，判据 → `CardFeel` 的「按战场深度重算缩放」那一节）。
        /// <para>⚠️ 与 <see cref="SetPose"/> 的分工：只碰缩放；位置 / 倾角 / 影子 / 裁切一个都不动
        /// （逐帧改那几样会和位移补间抢同一个 transform）。</para>
        /// <para>🔴 **必须走这个口**：直接写 `transform.localScale` 会让 `_baseScale` 停在旧值上，
        /// 之后任何一次 `SetPose` / `SetHighlightScale`（→ `ApplyScale`）都会把卡**弹回旧缩放**（静默）</para>
        /// <para>⚠️ 调用方负责记住「这一侧静止该多大」并在收尾写回 —— 这里不认业务语义。</para></summary>
        public void SetBaseScale(float scale)
        {
            _baseScale = scale;
            ApplyScale();
        }

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
            // 🔴 2026-10-24（L1 · `C1-B03` 并层）：描边画在**软光/影那一层**上
            //    （原版 `2DCard.prefab` 只有 1 颗 `Card Highlight And Shadow`，`_Outline*` 与 `_ShadowColor`
            //    是同一份 `Card Frame SDF` 的两个属性）⇒ 没有「懒建的专用层」这回事了，
            //    `OutlineMat` 恒指那一层（判据只此一处，见它的注释）。
            //    ⚠️ 原来的「懒建专用层」`EnsureOutlineLayer` **已删**（2026-10-24 并层），⛔ 别再建回来。
            var m = (_faceMode != CardFace.Board) ? OutlineMat : null;
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

        // 原版卡框 PNG 是 1024×1024。🔴 **2026-10-19（`A2138`）**：原版 `Image` 是 `m_UseSpriteMesh=1`
        // ⇒ 画的是 **sprite rect（`m_Rect` = 171,11,682,998，含透明留白）**，不是 alpha bbox
        // （判据与反证见 `FrameSpriteRectX` 那一段）。所以这里**两份缓存**：
        //   · `_uvCache`      = 画框真正用的 UV（卡框 = sprite rect；其它贴图 = alpha bbox）
        //   · `_alphaUvCache` = 不透明内容的位置（只给**非卡框贴图**的兜底；W27 那条逐帧费用规则已作废）
        static readonly Dictionary<Texture2D, Rect> _uvCache = new Dictionary<Texture2D, Rect>();
        static readonly Dictionary<Texture2D, Rect> _alphaUvCache = new Dictionary<Texture2D, Rect>();
        static readonly Dictionary<Texture2D, Mesh> _meshCache = new Dictionary<Texture2D, Mesh>();

        // 🆕 **2026-10-17（T09 第 2 步 · A1655②）**：卡框 UV 的**离线表**（`Resources/frame_uv.json`）。
        //   生成器 = `工具/gen_frame_uv.py`（判据与 `FrameUv` 逐字同一套：`alpha <= 8` 透明、
        //   `(x0+0.5)/W, (y0+0.5)/H, (x1-x0+1)/W, (y1-y0+1)/H`）。
        //   有它 ⇒ 运行时**不再 `GetPixels32()` + 全图扫描**（实测 ≈55 ms/格，见
        //   `资料/普查产出_1017/UI/PERF_T09_收藏窗性能诊断.md` §4）；表里没有的贴图**退回原扫描**。
        [System.Serializable] class FrameUvRow
        {
            public string t; public int w, h; public float u, v, sw, sh;
            // 🆕 2026-10-18（W27）：阵营徽记（贴图像素，y 从顶部数）—— bx/by = 圆心、br = 内切半径、
            //   bb = **下缘**（费用八边形上缘贴它）。没有这四项的旧表 ⇒ 全 0 ⇒ `TryBadgeBottomPx` 返回 false。
            public float bx, by, br, bb;
        }
        [System.Serializable] class FrameUvFile { public int count; public FrameUvRow[] entries; }
        static Dictionary<string, Rect> _uvTable;
        /// <summary>贴图名 → (bx, by, br, bb)。与 `_uvTable` 同一次解析填好。</summary>
        static Dictionary<string, Vector4> _badgeTable;
        static bool _uvTableTried;
        const string FrameUvResource = "frame_uv";

        static void EnsureUvTable()
        {
            if (_uvTableTried) return;
            _uvTableTried = true;
            var ta = Resources.Load<TextAsset>(FrameUvResource);
            if (ta == null) return;           // 没有表 = 正常（退回运行时扫描，不报错）
            var f = JsonUtility.FromJson<FrameUvFile>(ta.text);
            if (f == null || f.entries == null) return;
            _uvTable = new Dictionary<string, Rect>(f.entries.Length);
            _badgeTable = new Dictionary<string, Vector4>(f.entries.Length);
            foreach (var e in f.entries)
            {
                if (e == null || string.IsNullOrEmpty(e.t)) continue;
                _uvTable[e.t] = new Rect(e.u, e.v, e.sw, e.sh);
                if (e.bb > 0f) _badgeTable[e.t] = new Vector4(e.bx, e.by, e.br, e.bb);
            }
        }

        /// <summary>卡框那一层**在贴图上画哪一块**的 UV 矩形 (uMin, vMin, uMax, vMax)。
        /// <para>🔴 **2026-10-19（`A2138`）：卡框 = 原版 sprite rect（`m_Rect`），不是 alpha bbox。**
        /// 判据见 <see cref="FrameSpriteRectX"/> 那一段（`m_UseSpriteMesh=1` → `Image.GenerateSprite`
        /// → 整块 sprite rect 被拉进 RectTransform）。**visible 内容**（`textureRect` 那一块）
        /// 由渲染时的贴图内容自然落在 rect 里面 —— 不用（也⛔不许）在这里再按 alpha 裁一次。</para>
        /// <para>`frame_*` 且 1024² 的卡框 ⇒ `(171,11,682,998)/1024`（**y 以贴图底边为原点**；
        /// 反证见常量段）。其余贴图 ⇒ 退回 <see cref="FrameAlphaUv"/>（旧的 alpha bbox 语义）。
        /// `tex == null` / 量不出来 ⇒ 整张贴图 (0,0,1,1)。</para></summary>
        public static Rect FrameUv(Texture2D tex)
        {
            if (tex == null) return new Rect(0f, 0f, 1f, 1f);
            Rect r;
            if (_uvCache.TryGetValue(tex, out r)) return r;

            r = IsCardFrame(tex)
                ? new Rect(FrameSpriteRectX / tex.width, FrameSpriteRectY / tex.height,
                           FrameSpriteRectW / tex.width, FrameSpriteRectH / tex.height)
                : FrameAlphaUv(tex);

            _uvCache[tex] = r;
            return r;
        }

        /// <summary>这张贴图是不是「原版卡框」—— 判据 = **名字以 `frame_` 开头 + 1024²**。
        /// <para>与 `ArtBaker.ApplyImportSettings`（给 `cards/frame_*` 开 Read/Write）用的是同一条命名约定；
        /// 现读 **150 张卡框全部 1024²**、`frame_uv.json` 150 条一一对应。</para>
        /// <para>⚠️ 未来若出现**非 1024²** 的卡框，这里会退回 alpha 语义 —— 那是**故意的**：
        /// `m_Rect` 是 1024 画布上的绝对像素，直接套到别的尺寸上会静默画错（宁可退回旧行为并出声）。</para></summary>
        static bool IsCardFrame(Texture2D tex)
        {
            return tex != null && tex.width == 1024 && tex.height == 1024
                && tex.name != null && tex.name.StartsWith("frame_", System.StringComparison.Ordinal);
        }

        /// <summary>卡框贴图上**不透明内容**的 UV 矩形（`alpha &lt;= 8` 视为透明；口径与
        /// `工具/gen_frame_uv.py` 逐字相同）。
        /// <para>🆕 **2026-10-19（`A2138`）从 `FrameUv` 拆出来**：画框**不再**用它（原版画的是 sprite rect），
        /// 现在唯一的消费者 = <see cref="FrameUv"/> 对**非卡框**贴图的兜底（旧的 alpha 语义）。
        /// ⚠️ W27 曾经用它算「卡框可见右缘」（`FaceCostAt01` 横向）——**那条规则 2026-10-19 已作废**
        /// （费用回到 prefab 固定容器），所以这里不再有逐帧消费者。</para>
        /// <para>数据：先查离线表 `Resources/frame_uv.json`，表里没有才 `GetPixels32()` 全图扫描。</para></summary>
        static Rect FrameAlphaUv(Texture2D tex)
        {
            Rect r;
            if (_alphaUvCache.TryGetValue(tex, out r)) return r;

            // 🆕 **2026-10-17（T09 第 2 步）**：先查离线表（按贴图名）；没有才走原来的像素扫描。
            EnsureUvTable();
            if (_uvTable != null && _uvTable.TryGetValue(tex.name, out r))
            {
                _alphaUvCache[tex] = r;
                return r;
            }

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

            _alphaUvCache[tex] = r;
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
        /// 各算一遍迟早错位（破框 shader 靠 uv2 对齐遮罩，错一点就会切歪）。
        /// <para>🆕 **2026-10-24（`E9`）`typeScale` / `typeDy`** = 原版 `CardImageController` 的两条**按卡型**的变换：
        /// `CardScalesByCardType`（20 → 0.61，见 <see cref="TacticArtScale"/>）乘在**尺寸**上、
        /// `tacticsOffsetPosition`（y +0.5，见 <see cref="TacticArtDy"/>）加在**中心**上。
        /// 两者**互相独立**（原版一条写 `localScale`、一条写 `localPosition`）⇒ 先缩尺寸、再平移中心，
        /// ⛔ 别把偏移也乘进倍率。</para>
        /// <para>调用方：生产路径按 `!CardData.isUnit` 取（<see cref="ArtTypeScaleFor"/> / <see cref="ArtTypeDyFor"/>）；
        /// 不传 ⇒ 1 / 0 = 单位卡口径（既有探针逐位不变）。</para></summary>
        static void ArtExtent(Texture2D art, out float aw, out float ah, out float acy,
                              float typeScale = 1f, float typeDy = 0f)
        {
            float s = Width / CardUnitW;
            float sq = ArtUnitSquare * s * typeScale;           // 卡型倍率乘在【尺寸】上（原版 localScale）
            acy = ArtUnitY * s + typeDy * s;                    // 卡型偏移加在【中心】上（原版 localPosition）
            aw = sq; ah = sq;                                  // 按比例装进那块方形
            if (art != null && art.height > 0)
            {
                float a = art.width / (float)art.height;
                if (a > 1f) ah = sq / a; else aw = sq * a;
            }
        }

        /// <summary>立绘**实际画出来**的那块板（含 `ArtCoverMargin`）：宽 w、高 h、中心 y = cy（world 单位）。
        /// ⚠️ 这两个数**必须成对读**：`ArtMeshInFrame` 用它们摆网格，卡框 `uv2` 用它们算 UV
        /// —— 各读各的就会差一个 1.04（<see cref="ArtUvAt"/>）。
        /// <para>🆕 2026-10-18：`artScale` = 大图档的 ×1.27（原版 `CardImageController.bigSizeMultiplier`），
        /// 小卡一律 **1**。它乘在 `ArtCoverMargin` **之后**（等价于把整块立绘板连同留白一起放大，
        /// 与原版「缩放 `CardImage` 那个 RectTransform」一致）；中心 `cy` **不动**（pivot 居中 ⇒ 绕中心放大）。</para>
        /// <para>🆕 2026-10-24（`E9`）：`typeScale` / `typeDy` 原样转给 <see cref="ArtExtent"/> —— 卡型倍率与
        /// 大图档倍率**各自独立地乘在尺寸上**（原版是两个不同的字段），偏移只加中心。</para></summary>
        static void ArtBoard(Texture2D art, float artScale, out float w, out float h, out float cy,
                             float typeScale = 1f, float typeDy = 0f)
        {
            ArtExtent(art, out float aw, out float ah, out float acy, typeScale, typeDy);
            w = aw * ArtCoverMargin * artScale;
            h = ah * ArtCoverMargin * artScale;
            cy = acy;
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
        /// <para>🆕 2026-10-24（`E9`）：`typeScale` / `typeDy` 与 <see cref="ArtBoard"/> 同一份口径（卡型倍率/偏移）；
        /// **卡框 uv2 必须传与生产网格相同的两个值**，否则非单位卡的遮罩会整体错位（偏 0.5 卡单位 + 缩到 61%）。</para>
        /// </summary>
        static Vector2 ArtUvAt(Texture2D art, float x, float y, float artScale = 1f,
                               float typeScale = 1f, float typeDy = 0f)
        {
            ArtBoard(art, artScale, out float w, out float h, out float cy, typeScale, typeDy);
            return new Vector2(0.5f + x / w, (y - (cy - h * 0.5f)) / h);
        }

        /// <summary>🔴 **2026-10-21（第六十会话）**：从**卡框贴图**里现算「插图画窗」在卡面坐标里的矩形
        /// （卡单位，中心原点）。
        ///
        /// <para>为什么需要：战术/防御卡的卡框**只有上半留出透明画窗**，下半是实心框面 + 文字底板；
        /// 而我们把立绘裁到**整个卡体矩形** ⇒ 立绘从框的透明侧边/下沿漏出来（用户报的
        /// 「战术卡插图跑到下面」）。原版的立绘同样画在**框下面**、由**卡框贴图自己的 alpha** 决定可见
        /// 形状（⚠️ 2026-10-24 订正：原写「用 `SoftMaskScript` 拿卡框 alpha 给立绘挖洞」—— 那颗 SoftMask
        /// 的 `AlphaMask` 序列化值是 null、只淡没它自己那颗 Graphic，**不碰立绘**，见 `ArtVisibleThroughFrame`）；
        /// 我们没那层 ⇒ 退一步：**把非单位卡的立绘直接裁进这个矩形**。</para>
        /// <para>算法：只在不透明块（`FrameUv`）内，沿中列自上而下找**最长的透明带**（= 画窗），
        /// 在该带中线再向左右找透明带边界 ⇒ 一个保守的内接矩形。取不到（不可读 / 没有透明带）
        /// 就返回 `Rect.zero`（调用方不裁，行为与改前一致）。</para></summary>
        static Rect FrameArtWindow(Texture2D frameTex)
        {
            if (frameTex == null) return Rect.zero;
            int id = frameTex.GetInstanceID();
            Rect hit;
            if (_frameArtWindowCache.TryGetValue(id, out hit)) return hit;
            Rect result = Rect.zero;
            if (frameTex.isReadable && frameTex.width > 0 && frameTex.height > 0)
            {
                int W = frameTex.width, H = frameTex.height;
                var px = frameTex.GetPixels32();
                Rect uv = FrameUv(frameTex);
                int x0 = Mathf.Clamp(Mathf.FloorToInt(uv.xMin * W), 0, W - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt(uv.xMax * W) - 1, 0, W - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(uv.yMin * H), 0, H - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt(uv.yMax * H) - 1, 0, H - 1);
                int cx = (x0 + x1) / 2;
                int runTop = -1, bestTop = -1, bestBot = -1;
                for (int y = y1; y >= y0; y--)
                {
                    bool clear = px[y * W + cx].a <= 8;
                    if (clear) { if (runTop < 0) runTop = y; }
                    else if (runTop >= 0)
                    {
                        if (bestTop < 0 || (runTop - y) > (bestTop - bestBot)) { bestTop = runTop; bestBot = y + 1; }
                        runTop = -1;
                    }
                }
                if (runTop >= 0 && (bestTop < 0 || (runTop - y0) > (bestTop - bestBot)))
                { bestTop = runTop; bestBot = y0; }
                if (bestTop >= 0 && bestTop - bestBot >= Mathf.RoundToInt(H * 0.08f))
                {
                    // 🔴 2026-10-21（第二次真机）：横向取**整个开口的最高/最宽处**（union），
                    //    ⛔ 不是中线那一行 —— 卡窗是拱形，中线最窄 ⇒ 拱顶两角会切出黑三角；
                    //    而只取「卡体宽」又会从框外侧的透明余量漏出去。取 union = 填满拱形、
                    //    同时不会越过框的不透明内沿（拱顶窄处由框自身的不透明部分盖住）。
                    int l = cx, r = cx;
                    for (int y = bestBot; y <= bestTop; y++)
                    {
                        if (px[y * W + cx].a > 8) continue;
                        int ll = cx, rr = cx;
                        while (ll > x0 && px[y * W + ll - 1].a <= 8) ll--;
                        while (rr < x1 && px[y * W + rr + 1].a <= 8) rr++;
                        if (ll < l) l = ll;
                        if (rr > r) r = rr;
                    }
                    // 内缩 1 px，避开半透明边（框边是抗锯齿的）
                    int l2 = Mathf.Min(l + 1, r), r2 = Mathf.Max(r - 1, l);
                    int b2 = Mathf.Min(bestBot + 1, bestTop), t2 = Mathf.Max(bestTop - 1, bestBot);
                    // `GetPixels32` 的 y=0 在图**底**；`FramePxAt01` 的 py 从**顶部**数
                    var tl = FramePxAt01(frameTex, l2, H - 1 - t2);
                    var br = FramePxAt01(frameTex, r2, H - 1 - b2);
                    if (!float.IsNaN(tl.x) && !float.IsNaN(br.x))
                    {
                        float xL = (tl.x - 0.5f) * Width, xR = (br.x - 0.5f) * Width;
                        float yT = (0.5f - tl.y) * Height, yB = (0.5f - br.y) * Height;
                        if (xR > xL && yT > yB) result = new Rect(xL, yB, xR - xL, yT - yB);
                    }
                }
            }
            _frameArtWindowCache[id] = result;
            return result;
        }

        static readonly Dictionary<int, Rect> _frameArtWindowCache = new Dictionary<int, Rect>();

        // ==================================================================
        //  卡框「透明开口」的**逐行区间** —— 🆕 2026-10-21（`W61` 卡牌线 `新账-66`）
        // ==================================================================
        //
        // **为什么不能再只用一个矩形**：`FrameArtWindow` 给的是「开口最宽处的矩形」。
        //   战术卡 tier3/tier4 的开口是**拱形 / 异形**，矩形装不满 —— 逐张量了全部 75 张 `_strat` 卡框
        //   （泛洪算「被框围住的透明区」，再与矩形求差）：中位缺 **0.75%**、最差 **6.8%**
        //   （`genestealers_strat_tier4` 6.8% · `blacklegion/ember_strat_tier3` 6.3% ·
        //   `astramilitarum_strat_tier4` 5.8%）。缺的不是散点，是**开口上/下沿的整条带**。
        //   用户 2026-10-21 原话：「它是覆盖了战术卡框的上面的空白地方（允许有边缘被卡框盖着），
        //   你现在是不覆盖全部的空白处」。
        //
        // **做法**：从卡框贴图的 alpha 现算「**被框围住的透明区**」——`free`（透明）里**到不了
        //   画框矩形四边**的那些像素就是它（4 连通泛洪；框的金属是闭合的环 ⇒ 窗里的透明区到不了外面）。
        //   逐行取左右边界 ⇒ 立绘网格按行切成条带（`ArtWindowMesh`）。
        //   ⚠️ 立绘仍然画在**卡框下面**（z 0.03 > 框 0），所以条带左右各外扩 `FrameArtBleedPx`
        //   像素**钻进框的金属下面**，抗锯齿边不会漏底；框的不透明部分照常盖住立绘 ——
        //   这正是用户说的「允许有边缘被卡框盖着」。

        /// <summary>开口左右/上下各外扩几像素（贴图像素）：让立绘钻进框的金属下面，避免抗锯齿边露缝。</summary>
        const int FrameArtBleedPx = 3;

        /// <summary>🔴 **2026-10-21（`W61b` · `新账-66` 残余）**：卡框 alpha ≥ 这个值就算「不透明」
        /// —— 立绘在它底下露不出来（合成式 `frame·α + art·(1−α)` 里贡献不足 3%）。
        ///
        /// <para>**原版口径**（🔴 2026-10-24 重写 · `新账-66` alpha 口径）：立绘画在**框下面**，框按标准
        /// alpha-over 盖上来 ⇒ 卡框半透明处立绘按 **`1−α`（一次乘法）** 透出来，⛔ 不是 `(1−α)²`。
        /// 判据（一手）：① 层序 `Front` = `Card Highlight And Shadow → CardImage → Textbackgrounds →
        /// CardFrame → Card Info`（`bundle_staticgeneralassets_assets_all/RectTransform_-8145770350504326770.json`
        /// 的 `m_Children`）⇒ **框在立绘之上**；② 框的混合 = `SrcAlpha OneMinusSrcAlpha`
        /// （`Everguild/Card ImageUI` 的反编译体 / UI 默认材质；我们那层 `CardPresentation/CardSprite` 是等价的
        /// 预乘写法 `Blend One OneMinusSrcAlpha` + `c.rgb *= c.a`）；③ **立绘不是被遮罩图层的子节点**
        /// （`CardImage` 与 `CardFrame` 是**兄弟**，`m_Children` 索引 2 vs 4）⇒「乘两次」的前提不存在。
        /// ⚠️ 那颗 `SoftMaskScript`（`MonoBehaviour_5590396362908354958.json`，`AlphaMask=null` ·
        /// `FlipAlphaMask=1` · `CutOff=0` · `HardBlend=0`）只作用于**它自己那颗 Graphic**（CardFrame）与
        /// `textBackground`，**与立绘无关** —— 详见 `ArtVisibleThroughFrame` 的 doc。</para>
        ///
        /// <para>⚠️ 我们没做真 mask（那要原版那只 mask shader + Unity 里试），等价物 = **让立绘铺到
        /// 卡框半透明的那一圈下面**：合成后它的贡献就是 `1−α`（实测见
        /// `资料/普查产出_1021/W61b_卡牌_斜面遮罩.md` §2）。</para></summary>
        const int FrameBevelOpaquePx = 248;

        sealed class FrameRows
        {
            public int X0, X1;        // 贴图列范围（闭区间）
            public int Y0, Y1;        // 贴图行范围（闭区间，**自底向上**，与 `GetPixels32` 的索引一致）
            // 🔴 **2026-10-22（`W62/T1`）删掉了原来的 `L`/`R`（每行开口的 min..max）** ——
            //    它是 `FrameArtCoverage` 的旧基线，而那个基线把「同一行里被实心金属隔开的两块开口之间那段金属」
            //    也算成该有图 ⇒ 三张 tier4 卡框凭空多出 5.10%～7.37% 的假「漏」。现在覆盖统计**自己**
            //    走 `FrameMaskData` 的逐像素掩码（`hole ∪ ring` ∩ 立绘板 ∩ 卡体），不再从这里取基线。
            //    ⛔ 别把它加回来当第二份基线 —— 一份基线两处存，迟早又不一致（这次就是这么来的）。
            /// <summary>每行的**绘制片段** =`开口 ∪ 半透明内斜面`，已按 `FrameArtBleedPx` 外扩、
            /// 并按「框外面」夹过。长度 = Y1-Y0+1；元素 = 扁平数组 `[l0,r0,l1,r1,…]`（贴图列，已减 X0），
            /// `null` = 该行不画。
            /// <para>🔴 **为什么要拆成多段**：斜面在个别行会**被框上的透明缺口隔开**
            /// （实测 `frame_ultramarines_strat_tier4` 底部 16 行）—— 取 min..max 会把立绘画进那个缺口，
            /// 正是用户报的「战术卡插图跑到下面」。拆段后缺口一点不碰（离线 75 张实测 `spill = 0`）。</para></summary>
            public short[][] Runs;
            public int RunRows;       // 有绘制片段的行数（日志/断言用）
            public bool HasAny;
        }
        static readonly Dictionary<int, FrameRows> _frameRowsCache = new Dictionary<int, FrameRows>();

        /// <summary>卡框的逐行开口（按贴图缓存）。拿不到（贴图不可读 / 一个开口都没有）⇒ **null**，
        /// 调用方退回 <see cref="FrameArtWindow"/> 那个矩形（= 改前的行为）。</summary>
        static FrameRows FrameRowsOf(Texture2D frameTex)
        {
            if (frameTex == null) return null;
            int id = frameTex.GetInstanceID();
            FrameRows hit;
            if (_frameRowsCache.TryGetValue(id, out hit)) return hit;
            FrameRows rows = BuildFrameRows(frameTex);
            _frameRowsCache[id] = rows;
            return rows;
        }

        static FrameRows BuildFrameRows(Texture2D frameTex)
        {
            byte[] alpha; bool[] hole, outside, ring;
            int x0, y0, w, h;
            if (!FrameMaskData(frameTex, out alpha, out hole, out outside, out ring, out x0, out y0, out w, out h))
                return null;

            var res = new FrameRows
            {
                X0 = x0, X1 = x0 + w - 1, Y0 = y0, Y1 = y0 + h - 1,
                Runs = new short[h][],
            };
            var runs = new List<short>(8);
            for (int j = 0; j < h; j++)
            {
                int rowBase = j * w;

                // ---- ① 画法：`开口 ∪ 半透明内斜面` 的**连通片段** ----
                runs.Clear();
                int runL = -1;
                for (int i = 0; i < w; i++)
                {
                    if (hole[rowBase + i] || ring[rowBase + i]) { if (runL < 0) runL = i; }
                    else if (runL >= 0) { AddBevelRun(runs, rowBase, runL, i - 1, outside, w); runL = -1; }
                }
                if (runL >= 0) AddBevelRun(runs, rowBase, runL, w - 1, outside, w);
                if (runs.Count > 0) { res.Runs[j] = runs.ToArray(); res.RunRows++; }

                // ---- ② 这张卡框**有没有闭合开口**（`ArtMeshInFrame` 读 `HasAny` 决定走条带还是退回矩形）----
                // 🔴 **2026-10-22（`W62/T1`）**：这一段原来是「每行开口的 min..max」基线（`L`/`R`），
                //    现在基线改由 `FrameArtCoverage` **自己**从 `FrameMaskData` 的逐像素掩码取
                //    （`hole ∪ ring` ∩ 立绘板 ∩ 卡体）⇒ 这里只留「有没有」这一个判据，⛔ 别再存第二份基线。
                if (!res.HasAny)
                    for (int i = 0; i < w; i++) if (hole[rowBase + i]) { res.HasAny = true; break; }
            }
            if (!res.HasAny) return null;
            // 上下各扩 `FrameArtBleedPx` 行（沿用最近那行的片段，再按「框外面」夹一次）——
            // 开口的上下沿同样是抗锯齿边
            int top = -1, bot = -1;
            for (int j = 0; j < h; j++) if (res.Runs[j] != null) { if (bot < 0) bot = j; top = j; }
            for (int j = top + 1; j <= Mathf.Min(h - 1, top + FrameArtBleedPx); j++)
                res.Runs[j] = ClampBevelRuns(res.Runs[top], j * w, outside, w);
            for (int j = Mathf.Max(0, bot - FrameArtBleedPx); j < bot; j++)
                res.Runs[j] = ClampBevelRuns(res.Runs[bot], j * w, outside, w);
            return res;
        }

        /// <summary>卡框贴图 → 「开口 / 框外面 / 半透明内斜面」三张掩码（行号 j 自底向上，与 `GetPixels32` 一致）。
        ///
        /// <para>· `hole` = 透明（alpha ≤ 8）**且到不了 sprite rect 四边**的像素 = 被框围住的开口；
        /// · `outside` = 透明且与四边连通 = 框**外面**（立绘⛔不许画到这里）；
        /// · `ring` = alpha 在 **9..247**、**且与开口 4 连通**的像素 = 半透明内斜面。</para>
        ///
        /// <para>🔴 上界取 `FrameBevelOpaquePx`(248) 而不是 255：α ≥ 248 时立绘贡献 &lt; 3%（不可辨），
        /// 再往里就是框的实心金属 —— 立绘钻进去也看不见。
        /// ⚠️ **必须从开口长出去**（4 连通、只走半透明像素）：外发光/投影那圈半透明离开口很远、
        /// 中间隔着不透明金属 ⇒ 泛洪走不过去 ⇒ 不会把立绘铺到卡框外面（实测 75 张 `_strat`：`spill = 0`）。
        /// 判据出处 → `资料/普查产出_1021/W61b_卡牌_斜面遮罩.md`。</para>
        ///
        /// <para>返回 false = 贴图不可读 / sprite rect 太小（调用方退回矩形方案）。</para></summary>
        static bool FrameMaskData(Texture2D frameTex, out byte[] alpha, out bool[] hole,
                                  out bool[] outside, out bool[] ring,
                                  out int x0, out int y0, out int w, out int h)
        {
            alpha = null; hole = null; outside = null; ring = null;
            x0 = y0 = w = h = 0;
            if (frameTex == null || !frameTex.isReadable || frameTex.width <= 0 || frameTex.height <= 0)
                return false;
            int tw = frameTex.width, th = frameTex.height;
            Rect uv = FrameUv(frameTex);
            x0 = Mathf.Clamp(Mathf.FloorToInt(uv.xMin * tw), 0, tw - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(uv.xMax * tw) - 1, 0, tw - 1);
            y0 = Mathf.Clamp(Mathf.FloorToInt(uv.yMin * th), 0, th - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(uv.yMax * th) - 1, 0, th - 1);
            w = x1 - x0 + 1; h = y1 - y0 + 1;
            if (w < 8 || h < 8) return false;

            var px = frameTex.GetPixels32();
            alpha = new byte[w * h];
            // free[]：贴图透明（alpha <= 8）= 可通行。行号 j 自底向上（= `GetPixels32` 的索引约定）。
            var free = new bool[w * h];
            for (int j = 0; j < h; j++)
            {
                int src = (y0 + j) * tw + x0, dst = j * w;
                for (int i = 0; i < w; i++)
                {
                    byte a = px[src + i].a;
                    alpha[dst + i] = a;
                    free[dst + i] = a <= 8;
                }
            }

            // 4 连通泛洪：从**画框矩形**的四边出发 ⇒ 走得到的透明像素 = 框**外面**。
            // ⚠️ 从画框矩形而不是整张贴图的边出发：sprite rect 之外的像素不参与画框（判据同 `FrameUv`）。
            outside = new bool[w * h];
            var stack = new Stack<int>();
            for (int i = 0; i < w; i++)
            {
                if (free[i]) stack.Push(i);                                  // j = 0
                if (free[(h - 1) * w + i]) stack.Push((h - 1) * w + i);      // j = h-1
            }
            for (int j = 0; j < h; j++)
            {
                if (free[j * w]) stack.Push(j * w);
                if (free[j * w + w - 1]) stack.Push(j * w + w - 1);
            }
            while (stack.Count > 0)
            {
                int idx = stack.Pop();
                if (outside[idx] || !free[idx]) continue;
                int j = idx / w, i = idx - j * w;
                int l = i; while (l > 0 && free[j * w + l - 1] && !outside[j * w + l - 1]) l--;
                int r = i; while (r < w - 1 && free[j * w + r + 1] && !outside[j * w + r + 1]) r++;
                int rowBase = j * w;
                for (int k = l; k <= r; k++) outside[rowBase + k] = true;
                for (int nj = j - 1; nj <= j + 1; nj += 2)
                {
                    if (nj < 0 || nj >= h) continue;
                    int nb = nj * w;
                    // **每段只压一个种子**（扫描线泛洪的标准做法）—— 逐像素压栈在 68 万格的
                    // 卡框上会多出几十万次无用的 push/pop（每张卡框只算一次，但 150 张卡框就是秒级）。
                    int k = l;
                    while (k <= r)
                    {
                        while (k <= r && !(free[nb + k] && !outside[nb + k])) k++;
                        if (k > r) break;
                        stack.Push(nb + k);
                        while (k <= r && free[nb + k] && !outside[nb + k]) k++;
                    }
                }
            }

            hole = new bool[w * h];
            for (int k = 0; k < w * h; k++) hole[k] = free[k] && !outside[k];

            // ---- 半透明内斜面：从开口往外 4 连通（只走 8 < alpha < 248）----
            // 先找「贴着开口的半透明像素」当种子，再泛洪（一次全场扫描 + 一次 BFS）。
            ring = new bool[w * h];
            var ringStack = new Stack<int>();
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    int idx = j * w + i;
                    if (hole[idx] || ring[idx]) continue;
                    byte a = alpha[idx];
                    if (a <= 8 || a >= FrameBevelOpaquePx) continue;
                    if ((i > 0 && hole[idx - 1]) || (i < w - 1 && hole[idx + 1])
                        || (j > 0 && hole[idx - w]) || (j < h - 1 && hole[idx + w]))
                        ringStack.Push(idx);
                }
            while (ringStack.Count > 0)
            {
                int idx = ringStack.Pop();
                if (ring[idx]) continue;
                ring[idx] = true;
                int j = idx / w, i = idx - j * w;
                for (int d = 0; d < 4; d++)
                {
                    int ni = i + (d == 0 ? -1 : d == 1 ? 1 : 0);
                    int nj = j + (d == 2 ? -1 : d == 3 ? 1 : 0);
                    if (ni < 0 || ni >= w || nj < 0 || nj >= h) continue;
                    int nIdx = nj * w + ni;
                    if (ring[nIdx] || hole[nIdx]) continue;
                    byte a = alpha[nIdx];
                    if (a <= 8 || a >= FrameBevelOpaquePx) continue;
                    ringStack.Push(nIdx);
                }
            }
            return true;
        }

        /// <summary>把一行的片段 [a,b] 左右各外扩最多 `FrameArtBleedPx` 像素（⛔ 不越过「框外面」），
        /// 与外扩后相接的上一段合并。贴图列**已减 X0**（= 相对 sprite rect）。</summary>
        static void AddBevelRun(List<short> runs, int rowBase, int a, int b, bool[] outside, int w)
        {
            for (int k = 0; k < FrameArtBleedPx && a > 0 && !outside[rowBase + a - 1]; k++) a--;
            for (int k = 0; k < FrameArtBleedPx && b < w - 1 && !outside[rowBase + b + 1]; k++) b++;
            int n = runs.Count;
            if (n >= 2 && runs[n - 1] + 1 >= a)
            {
                if (b > runs[n - 1]) runs[n - 1] = (short)b;
                return;
            }
            runs.Add((short)a);
            runs.Add((short)b);
        }

        /// <summary>把上一行的片段搬到 `rowBase` 这一行，并**逐像素剔掉「框外面」**
        /// （上下沿那圈抗锯齿的补丁；⛔ 不夹的话立绘会从缺口漏到卡框外 —— 离线实测 5025 px）。</summary>
        static short[] ClampBevelRuns(short[] src, int rowBase, bool[] outside, int w)
        {
            if (src == null || src.Length == 0) return null;
            List<short> dst = null;
            for (int k = 0; k + 1 < src.Length; k += 2)
            {
                int a = src[k], b = src[k + 1];
                while (a <= b && outside[rowBase + a]) a++;
                while (b >= a && outside[rowBase + b]) b--;
                if (a > b) continue;
                if (dst == null) dst = new List<short>(src.Length);
                if (dst.Count >= 2 && dst[dst.Count - 1] + 1 >= a)
                { if (b > dst[dst.Count - 1]) dst[dst.Count - 1] = (short)b; }
                else { dst.Add((short)a); dst.Add((short)b); }
            }
            return dst == null ? null : dst.ToArray();
        }

        /// <summary>贴图列（**像素边界**，不是像素中心）→ 卡面 x（卡单位，中心原点）。
        /// 与 `FramePxAt01` / `FrameMesh` 同一条链（`FrameUv` → `FrameQuad`），只是这里画的是条带的**边**。</summary>
        static float FramePxXEdge(Texture2D tex, int col)
        {
            Rect uv = FrameUv(tex);
            float fu = (col / (float)tex.width - uv.xMin) / uv.width;
            return (fu - 0.5f) * FrameUnitW;
        }

        /// <summary>贴图行（**像素边界**，自底向上）→ 卡面 y（卡单位）。同 `FramePxXEdge` 的那条链。</summary>
        static float FramePxYEdge(Texture2D tex, int row)
        {
            Rect uv = FrameUv(tex);
            float fv = (row / (float)tex.height - uv.yMin) / uv.height;
            return (fv - 0.5f) * FrameUnitH + FrameUnitY;
        }

        /// <summary>按卡框的**逐行片段**建立绘网格（每行每段一条 quad；相邻行的片段列表相同的合并）。
        /// 顶点序/三角形序与 `ArtMeshInFrame` 的单 quad 那条路一致（BL/BR/TR/TL）。
        /// <para>🔴 `rows.Runs` 是**已经算好的画法**（`开口 ∪ 半透明内斜面`，含 `FrameArtBleedPx` 外扩、
        /// 已按「框外面」夹过）—— 覆盖统计（`FrameArtCoverage`）**另走一条路**：它拿
        /// `FrameMaskData` 的逐像素掩码当基线（`hole ∪ ring` ∩ 立绘板 ∩ 卡体），⛔ 不从 `Runs` 反推、
        /// 也不从外扩后的值反推（混了就会把「钻到框底下那一圈」算成开口、覆盖率永远是 0）。
        /// 2026-10-22（`W62/T1`）之前 `FrameRows` 里还存过一份逐行 `L/R` 基线 —— 已删（见 `FrameRows` 的注释）。</para></summary>
        static Mesh ArtWindowMesh(Texture2D art, Texture2D frameTex, FrameRows rows,
                                  float xL0, float yB0, float boardW, float boardH,
                                  float bxL, float bxR, float byB, float byT)
        {
            var verts = new List<Vector3>(512);
            var uvs = new List<Vector2>(512);
            var tris = new List<int>(768);
            int h = rows.Y1 - rows.Y0 + 1;
            int j = 0;
            while (j < h)
            {
                short[] r0 = rows.Runs[j];
                if (r0 == null || r0.Length == 0) { j++; continue; }
                int j0 = j;
                while (j + 1 < h && SameBevelRuns(r0, rows.Runs[j + 1])) j++;
                int j1 = j;                                      // 闭区间
                float yB = Mathf.Max(FramePxYEdge(frameTex, rows.Y0 + j0), byB);
                float yT = Mathf.Min(FramePxYEdge(frameTex, rows.Y0 + j1 + 1), byT);
                if (yT > yB)
                    for (int k = 0; k + 1 < r0.Length; k += 2)
                    {
                        float xL = Mathf.Max(FramePxXEdge(frameTex, rows.X0 + r0[k]), bxL);
                        float xR = Mathf.Min(FramePxXEdge(frameTex, rows.X0 + r0[k + 1] + 1), bxR);
                        if (xR <= xL) continue;
                        int b = verts.Count;
                        verts.Add(new Vector3(xL, yB, 0f)); verts.Add(new Vector3(xR, yB, 0f));
                        verts.Add(new Vector3(xR, yT, 0f)); verts.Add(new Vector3(xL, yT, 0f));
                        float uL = (xL - xL0) / boardW, uR = (xR - xL0) / boardW;
                        float vB = (yB - yB0) / boardH, vT = (yT - yB0) / boardH;
                        uvs.Add(new Vector2(uL, vB)); uvs.Add(new Vector2(uR, vB));
                        uvs.Add(new Vector2(uR, vT)); uvs.Add(new Vector2(uL, vT));
                        tris.Add(b);     tris.Add(b + 2); tris.Add(b + 1);
                        tris.Add(b);     tris.Add(b + 3); tris.Add(b + 2);
                    }
                j++;
            }
            if (verts.Count == 0) return null;
            var m = new Mesh { name = "CardArtWindow" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>两行的片段列表是否逐值相同（= 可以合并成一条 quad 带）。</summary>
        static bool SameBevelRuns(short[] a, short[] b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int k = 0; k < a.Length; k++) if (a[k] != b[k]) return false;
            return true;
        }

        /// <summary>原版 `_Clamp_Range`（**贴图 UV**，见 `ClampRangeSmall` 的 doc）→ **这张立绘 PNG 的局部 UV**。
        /// <para>返回 `(u0, u1, v0, v1)`（0..1、v 自下往上）；false = 这张图不在实测的两族里 ⇒ **不套硬矩形**。</para>
        /// <para>换算：我们的 PNG = 原版 sprite rect 的裁剪（`工具/import_original_art.py` 的 `write_portrait`
        /// 按 `m_RD.textureRect` 裁）。原版贴图恒 1024²、rect 水平居中（657 张单位卡 rect x=176.5 / 宽 670.5
        /// ⇒ 中心 511.75 vs 512，偏 0.25 px）⇒ `spanU = art.width / 1024` · `uMin = (1 − spanU) / 2`，纵向同理。</para>
        /// <para>⚠️ **只覆盖两族**（1207 个卡图 Sprite 里的 1206 个）：单位/督军 670.5×1024、战术/防御整张 1024²。
        /// 占位图（128×179）与那张杂项 512×512 **不套** —— 拿一个不在判据里的尺寸去猜 = 静默错一张。</para>
        /// <para>⚠️ 原版有两个小图值（材质默认 `x=0.82` / cctor `x=0.81`），见 `ClampRangeSmall` 的 doc。</para></summary>
        static bool ArtClampUv01(Texture2D art, bool big, out Vector4 uv01)
            => ArtClampUv01(art, big ? ClampRangeBig : ClampRangeSmall, out uv01);

        /// <summary>同上，但硬矩形**直接给**（🆕 2026-10-24 `E5`：状态补间要**在两档之间连续取值** ——
        /// 原版 `DoChange` 那条 `DOColor` 补的就是这个 `Vector4`，所以补间中间态不能只有 0.61/1 两档）。
        /// <para>判据：`CardImageController__DoChange.c` 的 `DOColor(material, clampRange, propID, scaleTime)`
        /// —— 整个 `Vector4` 被 DOTween 连续插值；我们把它烘进网格，所以补间要按插值后的值重算 UV。</para></summary>
        static bool ArtClampUv01(Texture2D art, Vector4 c, out Vector4 uv01)
        {
            uv01 = Vector4.zero;
            if (art == null || art.width <= 0 || art.height <= 0) return false;
            if (art.height < 1000 || art.height > 1032 || art.width < 500 || art.width > 1032) return false;
            float spanU = Mathf.Clamp01(art.width / ArtSourceTexPx);
            float spanV = Mathf.Clamp01(art.height / ArtSourceTexPx);
            if (spanU <= 0.01f || spanV <= 0.01f) return false;
            float uMin = (1f - spanU) * 0.5f;
            float vMin = (1f - spanV) * 0.5f;
            uv01 = new Vector4(
                Mathf.Clamp01((c.z - uMin) / spanU),      // u0
                Mathf.Clamp01((c.x - uMin) / spanU),      // u1
                Mathf.Clamp01((c.w - vMin) / spanV),      // v0
                Mathf.Clamp01((c.y - vMin) / spanV));     // v1
            return uv01.y > uv01.x && uv01.w > uv01.z;
        }

        /// <summary>把立绘摆进卡里那一块网格。**板走 `ArtBoard`**（立绘自己那块 ×`ArtCoverMargin`）——
        /// 🔴 **2026-10-24（`H5`）删掉了形参 `frameQuad`（`Vector3`）**：它从 2026-09 起**一次都没被方法体读过**
        /// （尺寸/中心全从 `ArtBoard` 取，见下方那段 2026-09-12 的注释），调用点却因此要传 `FrameQuad(frameTex)`
        /// 或 `Vector3.zero` 两种假值 —— 那是死参数（`H1` §六·3 记账、`H5` 件 2·1 核清后删除）。
        /// ⚠️ 卡框那一份 quad 仍然由 `FrameMesh` / `FrameQuad` 那条链用，**没有删**。
        /// <para>🆕 **2026-10-24（`E9`）`typeScale` / `typeDy`**：原版 `CardImageController` 按卡型做的两条变换
        /// （`CardScalesByCardType` 20 → 0.61 乘尺寸、`tacticsOffsetPosition` y +0.5 加中心，见
        /// <see cref="ArtExtent"/>）。**不传 = 1 / 0**（单位卡口径 ⇒ 既有探针与调用点逐位不变）。
        /// ⚠️ 两个值都进缓存键 —— 否则单位卡与战术卡（大图档两边都是 `clipWindow:false` + `clampKind:2`）
        /// 会共用同一份网格（随机先建先赢）。</para></summary>
        static Mesh ArtMeshInFrame(Texture2D art, float artScale = 1f,
                                   Texture2D frameTex = null, bool clipWindow = false, int clampKind = 0,
                                   Vector4? clampRange = null, float typeScale = 1f, float typeDy = 0f)
        {
            // `clampKind`：0 = 不套硬矩形 · 1 = 小图档 · 2 = 大图档（见 `ClampRangeSmall` / `ClampRangeBig`）。
            // 🆕 2026-10-24（`E5`）：`clampRange` = **直接给硬矩形**（= 原版那条 `DOColor` 补间的中间态），
            //   `null` ⇒ 仍按 `clampKind` 取 `ClampRangeSmall` / `ClampRangeBig`（**既有调用点逐位不变**）。
            //   ⚠️ `clampKind == 0`（不套）时**一律忽略** `clampRange` —— 否则「不套」那条路会被悄悄打开。
            // 🔴 关掉时**必须折算成 0 再进缓存键** —— 否则同一份键会存下「关掉时算的网格」，
            //    之后打开的人拿到的是没裁的那块（静默失效）。
            clampKind = DebugDisableArtClamp ? 0 : clampKind;
            if (clampKind == 0) clampRange = null;
            // 连续值也要进缓存键（否则补间第一帧算的那块会被后面所有帧拿到 = 补间静默失效）。
            // 量化到 1/1000 够用（补间本身也已量化到 1/32 档，见 `ApplyBigStateTweenAt`）。
            int clampKey = 0;
            if (clampRange.HasValue)
            {
                Vector4 cr = clampRange.Value;
                clampKey = unchecked(Mathf.RoundToInt(cr.x * 1000f) * 7919
                                   + Mathf.RoundToInt(cr.y * 1000f) * 104729
                                   + Mathf.RoundToInt(cr.z * 1000f) * 1299709
                                   + Mathf.RoundToInt(cr.w * 1000f) * 15485863);
                if (clampKey == 0) clampKey = 1;
            }
            float s = Width / CardUnitW;
            float aw, ah, acy;
            ArtExtent(art, out aw, out ah, out acy, typeScale, typeDy);
            int aspectKey = 0;
            if (art != null && art.height > 0)
                aspectKey = Mathf.RoundToInt(art.width / (float)art.height * 1000f);

            // 🔴 键要把 `artScale` 也算进去：`aw/ah` 是**没放大前**的范围，
            //    小卡(1.0)与大图档(1.27)会算出同一组 aw/ah ⇒ 不带它就会**共用同一份网格**
            //    （大图拿到小卡那块板 = 放大静默失效，而且 card pool 先建就先赢，随机）。
            int key = aspectKey * 10000
                    + Mathf.RoundToInt(aw * 10f) * 100
                    + Mathf.RoundToInt(ah * 10f);
            key = key * 100 + Mathf.RoundToInt(artScale * 100f);
            // 🔴 2026-10-21：战术卡「裁到卡框开口」也进键（不同阵营/tier 的开口不同）。
            key = key * 10 + (clipWindow ? 1 : 0);
            // 🔴 2026-10-24（A7）：`_Clamp_Range` 硬矩形档位也进键（0/1/2，见 `ClampRangeSmall`）。
            //    ⚠️ 乘数取**奇数**（397）而不是 10 —— 键在这一步早就超过 `int` 范围（`unchecked` 回绕），
            //    奇数乘法在 mod 2³² 上是**双射**（10 不是，会丢掉最低位 ⇒ 两个不同的档位可能撞到同一格）。
            key = unchecked(key * 397 + clampKind);
            if (clampKey != 0) key = unchecked(key * 397 + clampKey);
            // 🔴 2026-10-24（E9）：卡型倍率 / 偏移也进键 —— 它们同样烘在顶点里，不进键就会跨卡型串网格
            //    （大图档那条两边都是 `clipWindow:false` + `clampKind:2`，唯一的差别就是这两个数）。
            //    乘数仍取**奇数**（397）：键早已溢出 int，奇数乘法在 mod 2³² 上是双射。
            key = unchecked(key * 397 + Mathf.RoundToInt(typeScale * 10000f));
            key = unchecked(key * 397 + Mathf.RoundToInt(typeDy * 10000f));
            if (clipWindow && frameTex != null) key = unchecked(key * 31 + frameTex.GetInstanceID());
            Mesh cached;
            if (_artMeshCache.TryGetValue(key, out cached) && cached != null) return cached;

            // 板 = **立绘自己那块**（原版摆出来的大小）×`ArtCoverMargin`，UV 正好整张图。
            // ⚠️ 2026-09-12 踩过：板取「卡框 quad」时 UV 会算出 [0,1] 之外的值（立绘比板小），
            //    贴图导入是 `Clamp` —— 立绘**最外圈像素被拉到卡片四边**，手牌上看起来就是
            //    「卡片四周多了一圈白边」（用户报的）。板 = 立绘自己那块，就没有出界的采样了。
            //    卡框的金属本来就盖在两侧，立绘不需要铺到框边。
            // 🔴 这块板的**宽/高/中心**只从 `ArtBoard` 取一份 —— 卡框的 `uv2`（`ArtUvAt`）
            //    读的是同一份，两处各算一遍就会「遮罩和立绘差 4%」（2026-10-07 修，见 `ArtUvAt`）。
            ArtBoard(art, artScale, out float w, out float h, out float cy, typeScale, typeDy);
            float u0 = 0f, u1 = 1f, v0 = 0f, v1 = 1f;

            // 🔴 2026-10-24（A7）：原版 `_Clamp_Range` 硬矩形 → **卡面坐标里的一个矩形**
            //    （`.y > .x` 才算有；空 = 这张图不在实测两族里 ⇒ 不套）。见 `ClampRangeSmall` 的 doc。
            Vector4 clampCard = Vector4.zero;
            if (clampKind != 0
                && (clampRange.HasValue
                        ? ArtClampUv01(art, clampRange.Value, out Vector4 clampUv)
                        : ArtClampUv01(art, clampKind == 2, out clampUv)))
            {
                clampCard = new Vector4(
                    -w * 0.5f + clampUv.x * w, -w * 0.5f + clampUv.y * w,
                    cy - h * 0.5f + clampUv.z * h, cy - h * 0.5f + clampUv.w * h);
            }

            // **裁到卡本体那块矩形** —— 原版卡图 2.7484² 比卡大，溢出部分先被卡的矩形收边、
            //  再被卡框 alpha 遮罩（判据 → `资料/普查产出_1018/R_卡片详情大图三问.md` §二）。
            // 我们等价地做：板超出卡本体的部分直接不画，UV 跟着裁（裁的是画面，不是拉伸）。
            // ⚠️ 方形立绘（战术卡那种 1024² 的图）一定会比卡宽，非裁不可，
            //    否则立绘会**画到卡框外面**去（和手牌那圈白边是同一类毛病）。
            // 🔴 **2026-10-18 重写这段**（原来横竖各一套、竖的还有一条潜在 bug）：大图档 ×1.27 之后
            //    立绘**纵向**也会超出卡体（4:5 立绘放大后 3.63 > 卡高 3.33），旧写法在「上下都要裁」时
            //    用**改过之后的** cy 去算 v1 ⇒ UV 错 ~0.016（画面整体下移）。
            //    现在统一成「先把板夹进卡体矩形、再由两条边的**原始**位置反算 UV」——几何与 UV 一一对应，
            //    不再分横竖两套，也不可能出现 h>Height 时 cy 被推错的组合。
            //    ⚠️ 对**没触发放大**的卡（小卡 / 非大图档）结果与旧写法逐位一致（只裁宽那种情形算出来同一个数）。
            {
                float xL0 = -w * 0.5f, xR0 = w * 0.5f;
                float yB0 = cy - h * 0.5f, yT0 = cy + h * 0.5f;
                float hw = Width * 0.5f, hh = Height * 0.5f;
                float xL = Mathf.Max(xL0, -hw), xR = Mathf.Min(xR0, hw);
                float yB = Mathf.Max(yB0, -hh), yT = Mathf.Min(yT0, hh);
                // 🔴 **2026-10-21（第六十会话 · 用户报「战术卡插图跑到下面」）**：战术/防御卡框的
                //    **下半块是空心的**（框只在上半留出插图画窗，下半靠文字底板遮），而我们把立绘
                //    裁到**整个卡体矩形** ⇒ 立绘从框的透明侧边/下沿漏出来（大图档一眼可见）。
                //    原版的可见形状同样由**卡框自己的 alpha** 决定（⚠️ 2026-10-24 订正：原写「靠 SoftMask
                //    给立绘挖洞」—— 那颗 SoftMask 的 `AlphaMask` 是 null、只淡没它自己那颗 Graphic，
                //    见 `ArtVisibleThroughFrame`）；我们没有卡框贴图以外的东西 ⇒ **把非单位卡的立绘
                //    直接裁到「卡框的透明开口」**。
                //    单位卡**不裁**：角色立绘本来就要铺到框下、由框的 alpha 轮廓剪（大图档另有 opt-in 越框）。
                if (clipWindow && frameTex != null)
                {
                    // 🔴 **2026-10-21（`W61` · 用户第二次报：拱顶那块空白没盖住）**：
                    //    优先走**逐行开口条带**（`FrameRowsOf` / `ArtWindowMesh`）——
                    //    拱形开口才能被填满（逐张量了 75 张 `_strat` 卡框：矩形 union 中位缺开口的
                    //    **0.75%**、最差 **6.8%**，缺的是开口上/下沿的**整条带** ⇒ 一眼可见）。
                    var rowsForArt = FrameRowsOf(frameTex);
                    if (rowsForArt != null && rowsForArt.HasAny)
                    {
                        // 🔴 **2026-10-24（`F4` · `F1` §六·1 交叉面）**：`_Clamp_Range` 硬矩形也必须夹**这条早退路**。
                        //    原来这里拿到条带就直接 `return`，跳过了下面「与硬矩形取交集」那一段
                        //    ⇒ **非单位卡**（`clipWindow = true`）的 opt-in 小图档前景层上，A7 的
                        //    `_Clamp_Range` **是空转的**（A7 ④b 只跑单位卡 `clipWindow = false`，所以断不到）。
                        //    判据（原版一手）：战术/防御卡的立绘与单位卡走**同一个** `cardImage`
                        //    （`CardImageController__SetImage.c:174-177` 在 `param_3 == 0x14` 分支**之后**、
                        //    无条件执行 `Image.sprite = sprite`；tactic 那一支只把 `frameMask` 关掉
                        //    `:130-136`，**不动** sprite、也不换材质）；`ChangeState` / `DoChange`
                        //    对卡类型**没有任何判断**，`_Clamp_Range` 写的是同一份 `cardImage.material`
                        //    ⇒ 这个矩形对战术/防御卡同样生效。判据全文 → `资料/普查产出_1024/F4_卡面条带与断言.md`。
                        //    ⚠️ **2026-10-24 补（`X1` §一）**：原版这个矩形**只把矩形内 α 顶到 1、不裁画面**
                        //    （`alpha = saturate(alpha + inside01)`）；这里的「取交集」是**我们自己的近似**。
                        //    ⚠️ 只在 `clampKind != 0` 时 `clampCard` 非空 ⇒ 生产小卡（`clampKind = 0`）**逐位不变**；
                        //       单位卡 `clipWindow = false` 根本走不到本条 ⇒ 行为也不变。
                        float sL = xL, sR = xR, sB = yB, sT = yT;
                        if (clampCard.y > clampCard.x)
                        {
                            sL = Mathf.Max(sL, clampCard.x); sR = Mathf.Min(sR, clampCard.y);
                            sB = Mathf.Max(sB, clampCard.z); sT = Mathf.Min(sT, clampCard.w);
                        }
                        var strip = ArtWindowMesh(art, frameTex, rowsForArt, xL0, yB0, w, h, sL, sR, sB, sT);
                        if (strip != null) { _artMeshCache[key] = strip; return strip; }
                    }
                    // 拿不到逐行开口（贴图不可读 / 没有闭合开口）⇒ **退回矩形**（= 改前的行为）。
                    Rect win = FrameArtWindow(frameTex);
                    if (win.width > 0.01f && win.height > 0.01f)
                    {
                        // 纵向：裁到开口的上下沿（下半块不许有插图）。
                        // 横向：裁到**开口的最宽处（union）**—— 拱顶窄处由框自身的不透明部分盖住，
                        //       而框外侧的透明余量里不会再漏出矩形插图（2026-10-21 第二次真机修）。
                        xL = Mathf.Max(xL, win.xMin); xR = Mathf.Min(xR, win.xMax);
                        yB = Mathf.Max(yB, win.yMin); yT = Mathf.Min(yT, win.yMax);
                    }
                }
                // 🔴 2026-10-24（A7）：`_Clamp_Range` 矩形 —— 与「卡体矩形 / 卡框开口」取交集。
                //    **裁画面，不拉伸**（UV 由 `u0..v1` 那两行按原始板位反算）。
                //    ⚠️ **2026-10-24 更正（`X1` §一）**：这里原来写「这正是原版 shader『矩形外 α=0』的等价物」——
                //    原版**从不把 α 置 0**（`alpha = saturate(alpha + inside01)`，矩形外原样保留）⇒
                //    「裁画面」是**我们自己的近似、判据不足**。只在 `clampKind != 0` 时生效（生产恒 0）。
                //    见 `ClampRangeSmall` 的 doc。
                if (clampCard.y > clampCard.x)
                {
                    xL = Mathf.Max(xL, clampCard.x); xR = Mathf.Min(xR, clampCard.y);
                    yB = Mathf.Max(yB, clampCard.z); yT = Mathf.Min(yT, clampCard.w);
                }
                if (xR <= xL || yT <= yB)
                {
                    // 理论上不会发生（板总有交集）；真发生了就退回整张图，别画出负尺寸的网格
                    xL = xL0; xR = xR0; yB = yB0; yT = yT0;
                }
                u0 = (xL - xL0) / w; u1 = (xR - xL0) / w;
                v0 = (yB - yB0) / h; v1 = (yT - yB0) / h;
                w = xR - xL; h = yT - yB; cy = (yB + yT) * 0.5f;
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

        /// <summary>自检用：原版 `_Clamp_Range` 换算到**这张立绘 PNG 的局部 UV**（判据见 `ArtClampUv01`）。
        /// 返回 false = 这张图不在实测两族里（**没套硬矩形**，别读成通过）。</summary>
        public static bool ArtClampWindowForTest(Texture2D art, bool big, out Vector4 uv01)
            => ArtClampUv01(art, big, out uv01);

        /// <summary>自检用：**立绘网格**（与 `Build` 走同一条 `ArtMeshInFrame` 路）。`clampKind`：
        /// 0 = 不套硬矩形 · 1 = 小图档 · 2 = 大图档（判据见 `ClampRangeSmall`）。探针拿它做 A/B（灭自证）。
        /// <para>🆕 2026-10-24（`E9`）：`typeScale` / `typeDy` 透传（原版 `CardScalesByCardType` / `tacticsOffsetPosition`）。
        /// **不传 = 1 / 0** = 单位卡口径；量非单位卡的生产网格时必须传 `0.61f / 0.5f`
        /// （取值收在 `TacticArtScale` / `TacticArtDy`，⛔ 别在探针里另写一份）。</para></summary>
        public static Mesh ArtMeshForTest(Texture2D art, float artScale, Texture2D frameTex,
                                          bool clipWindow, int clampKind,
                                          float typeScale = 1f, float typeDy = 0f)
            => ArtMeshInFrame(art, artScale, frameTex, clipWindow, clampKind,
                              null, typeScale, typeDy);

        /// <summary>自检用（🆕 2026-10-24 `E24`）：卡面某一层（按 `GameObject` 名，如 `"art"` / `"frame"`）
        /// 真交给渲染器的那块网格；那一层不存在 ⇒ `null`。
        /// <para>为什么要有它：卡框的 **uv2** 是拿**立绘的板**算出来的（`FrameMesh` ← `ArtUvAt` ← `ArtBoard`），
        /// 立绘中心一动遮罩就得跟着动 —— 断言要能读到卡框那块网格才断得了这条。</para></summary>
        public Mesh LayerMeshForTest(string layerName)
        {
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
                if (mf != null && mf.gameObject.name == layerName) return mf.sharedMesh;
            return null;
        }

        /// <summary>自检用（🆕 2026-10-24 `E24`）：**底层立绘层**（`_art`）真交给渲染器的那块网格。
        /// <para>立绘中心的 y **烘在顶点里**（`ArtExtent` 的 `acy`，不是节点的 `localPosition`）⇒
        /// 「战斗卡 prefab −0.065 / 菜单卡 UI −0.044」这一条只能从这块网格的包围盒中心读到
        /// （见 `ArtUnitYFight`）。`null` = 这一层没建（没有立绘 / 占位图）—— 调用方**别读成通过**。</para></summary>
        public Mesh ArtLayerMeshForTest
        {
            get
            {
                if (_art == null) return null;
                var mf = _art.GetComponent<MeshFilter>();
                return mf != null ? mf.sharedMesh : null;
            }
        }

        /// <summary>自检用（🆕 2026-10-24 `E24`）：兵种行（`_race`）这一层摆位用的 **y01**（y 从卡顶算）。
        /// <para>为什么要有它：`PlaceAt` 把 `at.y` 换算成 `(0.5 − y01) × Height` 之后还要减
        /// `textBounds.center`（随文字变）⇒ 从 `localPosition` 反推要再借 TMP 的 bounds，量具本身会漂。
        /// 这里记的是**判据值**（原版 prefab 的 apos 换算出来的那个 y01），断言直接对原版字面量比。</para>
        /// <para>`NaN` = 这张卡**没有兵种行**（`_race == null`）。</para></summary>
        public float RaceAnchorY01ForTest { get { return _race != null ? _raceY01 : float.NaN; } }

        /// <summary>自检用（🆕 2026-10-24 `E24`）：兵种行这一层**真摆出去的中心 y**（卡面局部、卡单位）。
        /// <para>= `rectTransform.localPosition.y + textBounds.center.y` —— `PlaceAt` 摆完之后这两项之和
        /// 恒等于 `(0.5 − y01) × Height`。它与 <see cref="RaceAnchorY01ForTest"/> 是**两条独立的读数**
        /// （一条是判据、一条是实况）⇒ 断言两边都断，「判据写了但没落到层上」那种静默错才拦得住。</para>
        /// <para>`NaN` = 这张卡没有兵种行。</para></summary>
        public float RacePlacedCenterYForTest
        {
            get
            {
                if (_race == null) return float.NaN;
                return _race.rectTransform.localPosition.y + _race.textBounds.center.y;
            }
        }

        /// <summary>自检用（`D3`）：**大图档前景抠图层**（`_artFront`）真交给渲染器的那块网格 ——
        /// 「×1.27 只给 Minion/Hero」这道门控（<see cref="BigArtScaleApplies"/>）就烘在这块网格的顶点里。
        /// <para>`null` = 这一层**根本没建**（不是大图档 / 这张卡没有真立绘 / `BigCardOverdraw` 关）——
        /// 调用方**别读成通过**。</para>
        /// <para>为什么不能用 `transform.localScale` 量：倍率是烘进**网格顶点**的（`ArtBoard` /
        /// `ArtMeshInFrame`），那个节点上的 scale 恒为 1 ⇒ 只有这块网格能证明门控生效。</para></summary>
        public Mesh ArtFrontMeshForTest
        {
            get
            {
                if (_artFront == null) return null;
                var mf = _artFront.GetComponent<MeshFilter>();
                return mf != null ? mf.sharedMesh : null;
            }
        }

        /// <summary>自检用（🆕 2026-10-24 `E5`）：**角色压框层现在是不是亮着**（= `ArtFrontVisible` 的实况）。
        /// <para>与 `ArtFrontVisible` 的分工：那条是**口径**（该不该亮），这条是**实况**（`Show` 真的执行了）——
        /// 两条分开才能断到「判据改了但没落到层上」（`Build` 自己不查显隐，是 `Create` / `SetFace` /
        /// 两个 setter 各自补的那一句）。层没建（`_artFront == null`）⇒ 恒 `false`。</para></summary>
        public bool ArtFrontShown { get { return _artFront != null && _artFront.gameObject.activeSelf; } }

        /// <summary>自检用（`新账-66` 收口）：**卡框开口的逐像素覆盖统计**（卡面单位口径）。
        ///
        /// <para>返回 false = 量不了（卡框不可读 / 没有闭合开口 / 网格没建出来）—— 调用方**别读成通过**。</para>
        /// <para>`rectMissPct` = 改前「矩形 union」方案（`FrameArtWindow` + 夹取）漏掉的基线面积占比；
        /// `stripMissPct` = **这张卡真正用的那块网格**（`ArtMeshInFrame(clipWindow: true)`）漏掉的占比；
        /// 两个都按「漏 / 基线总面积」算，`holePctOfCard` = 基线占卡面（`Width × Height`）的百分比。</para>
        ///
        /// <para>🔴 **基线口径（2026-10-22 · `W62/T1` 改，⛔ 别改回逐行 min..max）**：**逐像素**
        /// `hole ∪ ring` ∩ **立绘板** ∩ **卡体** —— 与 <see cref="FrameArtBevelCoverage"/> 同一套。
        /// 为什么不能再用「每行开口的 min..max」：那个口径把**同一行里被实心金属隔开的两块开口之间那段金属**
        /// 也算成「该有图」，而条带只画**连通段**（`BuildFrameRows` 的 `Runs`）⇒ 三张 tier4 卡框凭空多出
        /// **5.10%～7.37%** 的「漏」，逐像素复核那一段全是 **alpha 254–255 的实心金属**（画面上零差别；
        /// 判据 = `资料/普查产出_1022/W62_卡面红诊断.md` §1·2：三张 tier4 开口漏 0 个、斜面漏 0 个）。
        /// 逐行那套还顺带把**立绘板之上**的几行（gene y≈1.398–1.408 &gt; 板顶 1.385）算进基线，原版那里也没有图。</para>
        ///
        /// <para>判据（原版口径 · 🔴 2026-10-24 重写）= **层序 + 标准 alpha-over**：立绘在框**下面**
        /// （兄弟序 2 vs 4）、框按 `SrcAlpha OneMinusSrcAlpha` 盖上来 ⇒ 透明处（开口）必须有图、
        /// 半透明处按 **`1−α`（一次乘法，⛔ 不是平方）** 透出；**不透明金属底下有没有图看不出来**
        /// ⇒ 基线**不该含金属**。这就是「改断言口径、不改渲染」的原版依据。
        /// ⚠️ 原写「判据 = `SoftMaskScript` + `FlipAlphaMask = 1`」—— **不成立**：那颗 SoftMask 的
        /// `AlphaMask` 序列化值是 null（默认白 ⇒ α_mask=1 ⇒ 输出 0），它只淡没**自己那颗 Graphic**，
        /// 不参与立绘的可见性。逐条判据 → `ArtVisibleThroughFrame` 的 doc。</para>
        ///
        /// <para>🔴 **灭自证**：`stripMissPct` 量的是 `ArtMeshInFrame` **真交给渲染器的那块网格**（把它的三角形
        /// 逐行投影回来），⛔ 不是把条带算法再算一遍 —— 谁把 `ArtMeshInFrame` 改回矩形，
        /// `stripMissPct` 会跟 `rectMissPct` 变成一个数、断言立刻红。</para>
        /// </summary>
        public static bool FrameArtCoverage(Texture2D frameTex, Texture2D art, float artScale,
                                            out float rectMissPct, out float stripMissPct,
                                            out float holePctOfCard, out int rows, out int quads)
        {
            rectMissPct = stripMissPct = 100f; holePctOfCard = 0f; rows = 0; quads = 0;
            byte[] alpha; bool[] hole, outside, ring;
            int x0, y0, w, h;
            if (!FrameMaskData(frameTex, out alpha, out hole, out outside, out ring, out x0, out y0, out w, out h))
                return false;
            var mesh = ArtMeshInFrame(art, artScale, frameTex, true);
            if (mesh == null || mesh.vertexCount < 4) return false;

            // 网格三角形 → 逐个 quad（顶点序 BL/BR/TR/TL，见 `ArtWindowMesh` / 单 quad 那条路）
            var vs = mesh.vertices;
            int nq = vs.Length / 4;
            var qx0 = new float[nq]; var qx1 = new float[nq];
            var qy0 = new float[nq]; var qy1 = new float[nq];
            for (int q = 0; q < nq; q++)
            {
                qx0[q] = Mathf.Min(vs[q * 4].x, vs[q * 4 + 1].x);
                qx1[q] = Mathf.Max(vs[q * 4].x, vs[q * 4 + 1].x);
                qy0[q] = Mathf.Min(vs[q * 4].y, vs[q * 4 + 2].y);
                qy1[q] = Mathf.Max(vs[q * 4].y, vs[q * 4 + 2].y);
            }
            quads = nq;

            // 立绘真正能画到的范围 = **立绘板 ∩ 卡体**（与 `FrameArtBevelCoverage` / `ArtMeshInFrame` 同一口径）——
            // 板外的开口（框最外侧的小装饰洞）原版也不该有图，⛔ 不能算进「漏」里。
            ArtBoard(art, artScale, out float bw, out float bh, out float bcy);
            float bxL = Mathf.Max(-bw * 0.5f, -Width * 0.5f), bxR = Mathf.Min(bw * 0.5f, Width * 0.5f);
            float byB = Mathf.Max(bcy - bh * 0.5f, -Height * 0.5f), byT = Mathf.Min(bcy + bh * 0.5f, Height * 0.5f);
            if (bxR <= bxL || byT <= byB) return false;

            Rect win = FrameArtWindow(frameTex);
            // 一格像素在卡面上占多大（面积口径用；x/y 两条映射都是均匀的 ⇒ 一列/一行一个常数）
            float pxW = (FramePxXEdge(frameTex, x0 + w) - FramePxXEdge(frameTex, x0)) / w;
            float pxH = (FramePxYEdge(frameTex, y0 + h) - FramePxYEdge(frameTex, y0)) / h;

            double basePx = 0, rectMissPx = 0, stripMissPx = 0;
            var iv = new List<Vector2>(16);
            for (int j = 0; j < h; j++)
            {
                float yB = FramePxYEdge(frameTex, y0 + j);
                float yT = FramePxYEdge(frameTex, y0 + j + 1);
                float yMid = (yB + yT) * 0.5f;
                if (yMid < byB || yMid > byT) continue;
                iv.Clear();
                for (int q = 0; q < nq; q++)
                {
                    if (qy1[q] <= yB || qy0[q] >= yT) continue;
                    float a = Mathf.Max(qx0[q], bxL), b = Mathf.Min(qx1[q], bxR);
                    if (b > a) iv.Add(new Vector2(a, b));
                }
                // ⚠️ **不许**在 `iv.Count == 0` 时跳过整行 —— 那等于「这一带没有网格 ⇒ 不计入」，
                //    正是自证：把整条带删掉反而全绿（同 `FrameArtBevelCoverage` 那条注释）。
                //    空表就让它逐像素判「没盖住」。
                bool rowCounted = false;
                for (int i = 0; i < w; i++)
                {
                    int idx = j * w + i;
                    if (!hole[idx] && !ring[idx]) continue;
                    float xc = (FramePxXEdge(frameTex, x0 + i) + FramePxXEdge(frameTex, x0 + i + 1)) * 0.5f;
                    if (xc < bxL || xc > bxR) continue;
                    rowCounted = true;
                    basePx += 1.0;

                    // ① 改前那个矩形方案（`FrameArtWindow` + 夹取）
                    bool rectCov = win.width > 0.01f && win.height > 0.01f
                                && xc >= win.xMin && xc <= win.xMax
                                && yMid >= win.yMin && yMid <= win.yMax;
                    if (!rectCov) rectMissPx += 1.0;

                    // ② 这张卡**真用的网格**
                    if (!InIntervals(iv, xc)) stripMissPx += 1.0;
                }
                if (rowCounted) rows++;
            }
            if (basePx <= 0) return false;
            rectMissPct = (float)(100.0 * rectMissPx / basePx);
            stripMissPct = (float)(100.0 * stripMissPx / basePx);
            holePctOfCard = (float)(100.0 * basePx * pxW * pxH / (Width * Height));
            return true;
        }

        /// <summary>立绘在「卡框半透明处」的**可见份额** = **`1 − α框`（一次乘法；⛔ 不是 `(1−α)²`）**。
        ///
        /// <para>🔴 **判据（一手 · 2026-10-24 现查，`新账-66` alpha 口径收口）**：
        /// ① **层序**：原版 prefab `Front` 的 `m_Children` = `Card Highlight And Shadow → CardImage →
        ///    Textbackgrounds → CardFrame → Card Info`（`bundle_staticgeneralassets_assets_all/
        ///    RectTransform_-8145770350504326770.json`，逐个按 `m_GameObject` 反查名字）⇒ **立绘在下、
        ///    卡框在上**（`CardImage` 索引 2 · `CardFrame` 索引 4）。
        /// ② **卡框的混合 = 标准 alpha-over**：`Everguild/Card ImageUI` 反编译体与 UI 默认材质都是
        ///    `Blend SrcAlpha OneMinusSrcAlpha`；我们那层 `CardPresentation/CardSprite` 是等价的预乘写法
        ///    （`Blend One OneMinusSrcAlpha` + `c.rgb *= c.a`，两者合成结果逐像素相同）。
        ///    ⇒ 立绘透出来的份额 = `a_立绘 × (1 − a_框)`，**只乘一次**。
        /// ③ **没有第二层遮罩**。全 prefab 只有一颗 `SoftMaskScript`（`MonoBehaviour_5590396362908354958.json`，
        ///    挂在 **CardFrame**、`MaskArea` = `CardImage` 的 RectTransform），它的片元（反编译体
        ///    `D:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/UI Extensions_SoftMaskShader.shader`）
        ///    是 `a_out = M·((1−α_mask)·a_obj − a_obj) + a_obj` = **`a_obj·(1 − M·α_mask)`（一次乘法）**；
        ///    而它的 `AlphaMask` 序列化值是 **null**（151/153 个实例如此；默认白 ⇒ `α_mask = 1` ⇒
        ///    `a_out = 0`）⇒ 它的实际作用是**把它挂到的那颗 Graphic 整层淡没**（`CardImageController__ChangeState.c`
        ///    的 `+0x50` 那一路 = `textBackground`；`SoftMaskScript.ToggleMask` 自己那颗 = CardFrame），
        ///    **不作用于立绘**。
        /// ④ **立绘不是被遮罩图层的子节点**（`CardImage` 与 `CardFrame` 是**兄弟**）⇒ 那条
        ///    「立绘会被乘两次 ⇒ `(1−α)²`」的担心 **不成立**。
        /// ⑤ 立绘另有一个 `_Clamp_Range` 矩形（`Everguild/Card ImageUI` 的 `_Clamp_Range`：
        ///    `alpha = saturate(alpha + inside01)` —— **矩形内把 α 顶到 1、矩形外原样保留**），与这条公式无关。
        ///    🔴 **2026-10-24 更正（铁律 5）**：原来这里写「UV 不在 `(z, x] × (w, y]` 内 ⇒ α=0」—— **不成立**，
        ///    原版**从不把 α 置 0**（判据 → `资料/普查产出_1024d/X1_三处判据补查.md` §一）。</para>
        ///
        /// <para>⚠️ 我们这层立绘用 `CardPresentation/ArtOpaque`（**忽略贴图 alpha，α 当 1**，见 `OpaqueMaterial`）
        /// ⇒ 上式在本工程退化成 `1 − a_框`。`FrameArtBevelCoverage` 的期望值就走这里（**只此一处**）。</para>
        /// </summary>
        public static float ArtVisibleThroughFrame(float frameAlpha01)
        {
            return 1f - Mathf.Clamp01(frameAlpha01);
        }

        /// <summary>自检用（`新账-66` 残余 · **半透明内斜面**）：量两件事 ——
        /// ① 「立绘在该点的贡献 = `1 − 卡框α`」（**标准 alpha-over**：立绘在框下、框在上，一次乘法；
        ///    ⛔ 不是 SoftMask、也不是 `(1−α)²` —— 判据全文见 `ArtVisibleThroughFrame`）；
        /// ② 「立绘有没有画到卡框外面」（本轮那条「按框外面夹取」的**灭自证**）。
        ///
        /// <para>`ringPx` = **立绘板 ∩ 卡体之内**、与开口 4 连通、alpha 在 9..247 的像素数（= 斜面面积）；
        /// `missPct` = 其中**没被立绘网格盖住**的像素占比；
        /// `meanErr` / `maxErr` = 贡献误差 —— 盖住 ⇒ 贡献 `1−α`（误差 0）；没盖住 ⇒ 贡献 0，
        /// 误差 = `1−α`。两个都按**环像素**做分母（不是按「漏掉的像素」），所以它是「整圈平均丢了多少」。</para>
        ///
        /// <para>🔴 **灭自证**：`missPct` 量的是 `ArtMeshInFrame` **真交给渲染器的那块网格**
        /// （把它的三角形逐行投影回来），⛔ 不是把画法再算一遍 —— 谁把 `ArtWindowMesh` 改回「只画开口
        /// + 3 px 外扩」，`missPct` 会从 0 跳回 **~90%**（离线 75 张实测），本条立刻红。
        /// 谁把 `AddBevelRun` / `ClampBevelRuns` 里的「框外面」夹取去掉，`spillPct` 会跳起来
        /// （`frame_ultramarines_strat_tier4` 底部 5025 px）。</para>
        ///
        /// <para>返回 false = 量不了（卡框不可读 / 没有开口 / 网格没建出来）—— 调用方**别读成通过**。</para>
        /// </summary>
        public static bool FrameArtBevelCoverage(Texture2D frameTex, Texture2D art, float artScale,
                                                 out int ringPx, out float missPct,
                                                 out float meanErr, out float maxErr, out float spillPct)
        {
            ringPx = 0; missPct = 100f; meanErr = 1f; maxErr = 1f; spillPct = 100f;
            byte[] alpha; bool[] hole, outside, ring;
            int x0, y0, w, h;
            if (!FrameMaskData(frameTex, out alpha, out hole, out outside, out ring, out x0, out y0, out w, out h))
                return false;
            var mesh = ArtMeshInFrame(art, artScale, frameTex, true);
            if (mesh == null || mesh.vertexCount < 4) return false;

            // 网格三角形 → 逐个 quad（顶点序 BL/BR/TR/TL，见 `ArtWindowMesh` / 单 quad 那条路）
            var vs = mesh.vertices;
            int nq = vs.Length / 4;
            var qx0 = new float[nq]; var qx1 = new float[nq];
            var qy0 = new float[nq]; var qy1 = new float[nq];
            for (int q = 0; q < nq; q++)
            {
                qx0[q] = Mathf.Min(vs[q * 4].x, vs[q * 4 + 1].x);
                qx1[q] = Mathf.Max(vs[q * 4].x, vs[q * 4 + 1].x);
                qy0[q] = Mathf.Min(vs[q * 4].y, vs[q * 4 + 2].y);
                qy1[q] = Mathf.Max(vs[q * 4].y, vs[q * 4 + 2].y);
            }

            // 立绘真正能画到的范围 = **立绘板 ∩ 卡体**（与 `ArtMeshInFrame` 同一口径）——
            // 板比卡窄时（671×1024 那档立绘）板外那圈本来就没有立绘，⛔ 不能算进「漏」里。
            ArtBoard(art, artScale, out float bw, out float bh, out float bcy);
            float bxL = Mathf.Max(-bw * 0.5f, -Width * 0.5f), bxR = Mathf.Min(bw * 0.5f, Width * 0.5f);
            float byB = Mathf.Max(bcy - bh * 0.5f, -Height * 0.5f), byT = Mathf.Min(bcy + bh * 0.5f, Height * 0.5f);
            double ringA = 0, missA = 0, errSum = 0, spillA = 0;
            float worst = 0f;
            var iv = new List<Vector2>(16);
            for (int j = 0; j < h; j++)
            {
                float yB = FramePxYEdge(frameTex, y0 + j);
                float yT = FramePxYEdge(frameTex, y0 + j + 1);
                float yMid = (yB + yT) * 0.5f;
                // 口径同 `FrameArtCoverage`：只算**立绘板 ∩ 卡体**之内
                if (yMid < byB || yMid > byT) continue;
                iv.Clear();
                for (int q = 0; q < nq; q++)
                {
                    if (qy1[q] <= yB || qy0[q] >= yT) continue;
                    float a = Mathf.Max(qx0[q], bxL), b = Mathf.Min(qx1[q], bxR);
                    if (b > a) iv.Add(new Vector2(a, b));
                }
                // ⚠️ **不许**在 `iv.Count == 0` 时跳过这一行 —— 那等于「这一带没有网格 ⇒ 不计入」，
                //    正是自证：把整条带删掉反而全绿。空表就让它逐像素判「没盖住」。
                for (int i = 0; i < w; i++)
                {
                    int idx = j * w + i;
                    bool isOut = outside[idx];
                    if (!isOut && !ring[idx]) continue;
                    float xc = (FramePxXEdge(frameTex, x0 + i) + FramePxXEdge(frameTex, x0 + i + 1)) * 0.5f;
                    if (xc < bxL || xc > bxR) continue;
                    bool cov = InIntervals(iv, xc);
                    if (isOut)
                    {
                        if (cov) spillA += 1.0;          // ② 画到框外面
                        continue;
                    }
                    ringA += 1.0;                        // ① 斜面像素
                    if (cov) continue;                   // 盖住 ⇒ 贡献正好是 `1−α`（误差 0）
                    // 没盖住 ⇒ 那一点该有的 `1−α` 全丢了（**一次乘法**，判据见 `ArtVisibleThroughFrame`）
                    float exp = ArtVisibleThroughFrame(alpha[idx] / 255f);
                    missA += 1.0;
                    errSum += exp;
                    if (exp > worst) worst = exp;
                }
            }
            if (ringA <= 0) return false;
            ringPx = (int)ringA;
            missPct = (float)(100.0 * missA / ringA);
            meanErr = (float)(errSum / ringA);
            maxErr = worst;
            spillPct = (float)(100.0 * spillA / ringA);
            return true;
        }

        /// <summary>x 是否落在（未合并的）区间表里 —— 区间按 x0 升序，逐个比即可（表很短）。</summary>
        static bool InIntervals(List<Vector2> iv, float x)
        {
            for (int k = 0; k < iv.Count; k++) if (x >= iv[k].x && x <= iv[k].y) return true;
            return false;
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

        // 🔴 **2026-10-18（`A1940`）：网格缓存**三族各自一份，防键撞车** ——
        //   原来这三族共用一个 `_spriteQuads`，键空间是**平铺**的：
        //     · `SpriteQuad`  键 = 图名（`armourIcon` / `costBg_<卡框名>` / `ephemeral` / `FitQuad` 的 `名字_WxH`）
        //     · `TextBgQuad`  键 = `textBg` / `textBgSmall`
        //     · `GemMesh`     键 = `gem_<卡框名>`
        //   ⇒ 只要有一个 `SpriteQuad` 的图名恰好叫 `textBg*` 或 `gem_*`，就会**静默拿到别族的网格**
        //   （顶点数都不一样：6 块九宫格 24 顶点 vs 单 quad 4 顶点）。今天调用点没有撞，但那是巧合。
        //  ⚠️ 拆开后**几何逐字节不变**（每个函数自己算自己的顶点，缓存只影响「要不要重算」）。
        static readonly Dictionary<string, Mesh> _spriteQuads = new Dictionary<string, Mesh>();   // `SpriteQuad` 一族
        static readonly Dictionary<string, Mesh> _textBgQuads = new Dictionary<string, Mesh>();   // `TextBgQuad` 一族（文字底板九宫格）
        static readonly Dictionary<string, Mesh> _gemQuads    = new Dictionary<string, Mesh>();   // `GemMesh` 一族（稀有度宝石）

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

        // ==================================================================================
        //  🔴 **文字底板 = 原版的 Sliced（九宫格）** —— 账 `A1933`（2026-10-14）
        //
        //  原来这一层走 `SpriteQuad`：**一张贴图整块拉伸**（4 顶点、UV 拉满 0..1）。**原版不是这么画的。**
        //
        //  **判据（解包资源 = 第二权威，逐字段现读）**
        //    · Image：`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/`
        //        `MonoBehaviour_-2228335281872200768.json`（`TextBackground Big UI`）
        //        `MonoBehaviour_-1216900743962780736.json`（`TextBackground Small UI`，**同款**）
        //        → **`m_Type = 1`（Sliced）** · **`m_PixelsPerUnitMultiplier = 14.699999809265137`**
        //          · `m_FillCenter = 1` · `m_PreserveAspect = 0` · `m_UseSpriteMesh = 0` · `m_RaycastTarget = 0`
        //          ⚠️ **这个 Image 上没有 `m_Border`**（uGUI 的 Image 本来就没有这个字段）——
        //             边宽只存在 sprite 里（下一行）。⚠️ 2026-10-14 现核：本文件全部键 = `m_GameObject /
        //             m_Enabled / m_Script / m_Name / m_Material / m_Color / m_RaycastTarget / m_RaycastPadding /
        //             m_Maskable / m_OnCullStateChanged / m_Sprite / m_Type / m_PreserveAspect / m_FillCenter /
        //             m_FillMethod / m_FillAmount / m_FillClockwise / m_FillOrigin / m_UseSpriteMesh /
        //             m_PixelsPerUnitMultiplier`（20 个，**没有 border**）。
        //    · sprite：`bundle_duplicateassetisolation_assets_all/Sprite/Card Text smooth background.json`
        //        → 32×32 · **`m_Border = {x:7, y:0, z:7, w:9}`**（**边宽的唯一出处**）
        //          · **`m_PixelsToUnits = 100`** · `m_Pivot = (.5,.5)`
        //    · 贴图：同包 `Texture2D/Card Text smooth background.png` —— RGB 全 `(255,255,255)` ·
        //        alpha 均值 **0.8776**（我们导入那份逐项相同）· **上缘 7 行是羽化带**
        //        （行 0..6 的 alpha = 4,21,58,110,170,219,243）、行 7..29 = 255
        //
        //  **边宽怎么换成卡单位**（照 **uGUI 自己的算式**，⛔ 不是我们挑的数）：
        //    `…/Hub/Editor/6000.3.23f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui/`
        //      `Runtime/UGUI/UI/Core/Image.cs:1157`
        //        `adjustedBorders = GetAdjustedBorders(sprite.border / multipliedPixelsPerUnit, rect)`
        //    其中 `pixelsPerUnit = spritePPU ÷ canvas.referencePixelsPerUnit`（同文件 `:741`）、
        //         `multipliedPixelsPerUnit = pixelsPerUnit × m_PixelsPerUnitMultiplier`（`:757`）
        //    ⇒ **画出来的边宽 = 边宽px × refPPU ÷ (spritePPU × ppuMul)**（同一份 `rect` 单位 = 卡单位）。
        //    `canvasRefPPU = 100` —— 原版 **两款 Canvas 实测都只有这一个值**：`07_场景/battlearena1/Canvas/*`
        //      与 `bundle_menus*` 的 `m_ReferencePixelsPerUnit` 全库只有 `100.0`（30 处）；
        //      `spritePPU = 100` ⇒ **1 贴图px = 1/14.7 卡单位**。
        //    ⚠️ 是 **÷ 14.7**，⛔ 不是 ×14.7 —— 与 `MenuDraw.Nine` 那条「角块 = `m_Border ÷ ppuMul`」
        //      （左栏 `Highlight`：30 ÷ 0.92 = **32.61**，**渲图核过**）同一条算式。
        //    ⇒ 左/右 = 7/14.7 = **0.4762 卡单位**（占底板宽 1.7766 的 **26.8%**）·
        //      上 = 9/14.7 = **0.6122**（占底板高 1.55 的 **39.5%**）· **下 = 0**。
        //
        //  **为什么是 6 块不是 9 块**：sprite 的 `m_Border.y`（下）= **0** ⇒ 下面那一行的 3 个格子**高为 0**，
        //    uGUI 的 `GenerateSlicedSprite` 对零尺寸格是 `continue`（同文件 `:1193` 那句，
        //    `Check for zero or negative dimensions to prevent invalid quads`）⇒ **下面那 3 块不画**。
        //    ⇒ 落地就是 **上左 / 上中 / 上右 / 下(中)左 / 下(中)中 / 下(中)右 = 6 块**（横竖各切两刀、只保留下两行）。
        //    ⚠️ **同族先例**：`ImageQuad.CreateNineSlice` 的 `if (w <= 0f || h <= 0f) continue;`
        //      与 `Editor/CollectionScene.cs:2909` 那条「这一档尺寸下**原版只画得出 6 块**」（`A1726`）。
        //
        //  **改出来的可观测差别**：上缘羽化带占底板高 **原版 39.5%**（7/9 × 0.6122 ÷ 1.55 = **30.7%** 是那条
        //    从近透明到不透明的斜坡，剩下 8.8% 是羽化带下面那 2 行实心）**↔ 我们原来只占 21.9%**（7/32）；
        //    整块矩形上的 alpha 均值 **原版 ≈ 0.830 ↔ 我们原来 0.8776**。
        // ==================================================================================

        /// <summary>原版 sprite 的 `m_Border`（**贴图像素**；Unity 的顺序 = **左 / 下 / 右 / 上**）
        /// —— ⚠️ 这个字段在 **sprite** 上，Image 上**没有**（见上面那一段的现核）。</summary>
        static readonly Vector4 TextBgBorderPx = new Vector4(7f, 0f, 7f, 9f);
        /// <summary>原版 `Image.m_PixelsPerUnitMultiplier`（实读 `14.699999809265137`）。</summary>
        const float TextBgPPUMult = 14.7f;
        /// <summary>原版 sprite 的 `m_PixelsToUnits`（`Card Text smooth background`）。</summary>
        const float TextBgSpritePPU = 100f;
        /// <summary>原版 `Canvas.m_ReferencePixelsPerUnit`（全库实测只有 `100` 这一个值）。</summary>
        const float TextBgCanvasRefPPU = 100f;
        /// <summary>原版贴图的边长（px）—— `Card Text smooth background` = **32×32**（UV 切点按它切）。</summary>
        const float TextBgTexPx = 32f;

        /// <summary>文字底板的 **Sliced 九宫格**网格（原版 `Image.m_Type = 1`；上面那一段是判据全文）。
        /// 一块 mesh 里放 **6 个 quad**（下边 border = 0 ⇒ 下面那一行整排不画），
        /// 顶点烘的是**绝对坐标**、单位与 `SpriteQuad` 一致（卡单位 × `Width/CardUnitW`，今天 = 1）
        /// —— 位置在网格里、⛔ 不在 transform 上。
        ///
        /// 🔴 **必须仍然是【一个 GameObject / 一个 MeshRenderer / 一份材质 / 同一个 z】**：
        ///    卡内所有层共用一个 `renderQueue`（`StampQueue`，实测 3000）⇒ 卡内的层序**完全靠「到相机的距离」排**。
        ///    把 6 块拆成 6 个 GameObject 会让每块各自有一个 z ⇒ 层序就乱了
        ///    （诊断细节 → `资料/普查产出_第二十九会话/A1922_文字底板_诊断.md` §七③）。
        ///    ⇒ 6 块进**同一个** mesh、全部 `z = 0`（层自己的 `localPosition.z` 仍是 `TextBgZ`）。
        /// ⚠️ 网格按 `name` 缓存（**2026-10-18 `A1940` 起独占 `_textBgQuads`，⛔ 不再与 `SpriteQuad` 共用**）：
        ///    只有 `textBg` / `textBgSmall` 两个名字，
        ///    各自的几何是**常量**（不随卡变），所以按名字缓存是安全的。</summary>
        static Mesh TextBgQuad(string name, Vector2 at01, float w, float h)
        {
            Mesh m;
            if (_textBgQuads.TryGetValue(name, out m) && m != null) return m;

            float s = Width / CardUnitW;                          // 卡单位 → 世界（今天 = 1）
            float ww = w * s, hh = h * s;
            float x0 = (at01.x - 0.5f) * Width - ww * 0.5f;       // 底板左下角（世界）
            float y0 = (0.5f - at01.y) * Height - hh * 0.5f;

            // 画出来的边宽（世界）= 边宽px × (refPPU ÷ (spritePPU × ppuMul)) × s   ← 判据见上面那一段
            float k = TextBgCanvasRefPPU / (TextBgSpritePPU * TextBgPPUMult) * s;
            float bl = TextBgBorderPx.x * k, bb = TextBgBorderPx.y * k;
            float br = TextBgBorderPx.z * k, bt = TextBgBorderPx.w * k;
            // 目标比「两边角加起来」还小 ⇒ 按【轴】缩（uGUI `GetAdjustedBorders`，`Image.cs:1479`——
            // 它是 `for (axis = 0; axis <= 1; axis++)` 逐轴判，⛔ 不是两轴共用一个比例）。
            // ⚠️ 今天这两轴都够（0.4762+0.4762 = 0.9524 < 1.7766 · 0+0.6122 < 1.55）⇒ 这一段**不出力**，
            //    留着是为了「以后有人把底板改窄」时与原版同形，别删。
            float scX = (bl + br) > ww ? ww / Mathf.Max(1e-4f, bl + br) : 1f;
            float scY = (bb + bt) > hh ? hh / Mathf.Max(1e-4f, bb + bt) : 1f;
            bl *= scX; br *= scX; bb *= scY; bt *= scY;

            float[] xs = { x0, x0 + bl, x0 + ww - br, x0 + ww };
            float[] ys = { y0, y0 + bb, y0 + hh - bt, y0 + hh };
            // UV 切点按**贴图 px ÷ 边长**（⛔ 与上面那个 ÷14.7 是两码事：一个切 UV、一个定画出来的宽度）
            float[] us = { 0f, TextBgBorderPx.x / TextBgTexPx, 1f - TextBgBorderPx.z / TextBgTexPx, 1f };
            float[] vs = { 0f, TextBgBorderPx.y / TextBgTexPx, 1f - TextBgBorderPx.w / TextBgTexPx, 1f };

            var verts = new List<Vector3>(24);
            var uvs = new List<Vector2>(24);
            var tris = new List<int>(36);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    float xa = xs[i], xb = xs[i + 1], ya = ys[j], yb = ys[j + 1];
                    // 零尺寸格跳过（uGUI `GenerateSlicedSprite` 同款）—— 下边 border = 0 ⇒ 下面那一行整排没有
                    // ⇒ 正常一档落地就是 **6 块**（`A1933` 的验收数：24 顶点 / 12 三角 / 6 个 quad）。
                    if (xb - xa <= 0f || yb - ya <= 0f) continue;
                    int o = verts.Count;
                    verts.Add(new Vector3(xa, ya, 0f)); uvs.Add(new Vector2(us[i], vs[j]));
                    verts.Add(new Vector3(xb, ya, 0f)); uvs.Add(new Vector2(us[i + 1], vs[j]));
                    verts.Add(new Vector3(xb, yb, 0f)); uvs.Add(new Vector2(us[i + 1], vs[j + 1]));
                    verts.Add(new Vector3(xa, yb, 0f)); uvs.Add(new Vector2(us[i], vs[j + 1]));
                    // 绕序与 `SpriteQuad` 逐位一致（⛔ 别按 u/v 的方向去定绕序）
                    tris.Add(o); tris.Add(o + 2); tris.Add(o + 1);
                    tris.Add(o); tris.Add(o + 3); tris.Add(o + 2);
                }

            m = new Mesh { name = "TextBgQuad_" + name };
            m.vertices = verts.ToArray();
            m.uv = uvs.ToArray();
            m.triangles = tris.ToArray();
            m.RecalculateBounds();
            _textBgQuads[name] = m;
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
        ///
        /// 🔴 **2026-10-18**：落点改成**跟着这张卡框的凹槽走**（`FaceGemAt01`）⇒ 网格也随卡框变，
        ///    按卡框名缓存进 `_gemQuads`（**2026-10-18 `A1940` 起独占这一份，⛔ 不再与 `SpriteQuad` 共用**；
        ///    150 个卡框 = 150 份小网格）。
        ///    ⚠️ 原来这里缓存的是一个**全局唯一**的 `_gemMesh`，那只有在「所有卡框凹槽落到同一处」时才对 —— 不成立。
        /// </summary>
        static Mesh GemMesh(Texture2D frameTex)
        {
            string key = "gem_" + (frameTex != null ? frameTex.name : "none");
            Mesh m;
            if (_gemQuads.TryGetValue(key, out m) && m != null) return m;
            float s = Width / CardUnitW;
            float w = RarityGemW * s * 0.5f, h = RarityGemH * s * 0.5f;
            var at = FaceGemAt01(frameTex);
            float cx = (at.x - 0.5f) * Width, cy = (0.5f - at.y) * Height;
            m = new Mesh { name = "CardRarityGemQuad_" + key };
            m.vertices = new[]
            {
                new Vector3(cx - w, cy - h, 0f), new Vector3(cx + w, cy - h, 0f),
                new Vector3(cx + w, cy + h, 0f), new Vector3(cx - w, cy + h, 0f),
            };
            m.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f),
            };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            _gemQuads[key] = m;
            return m;
        }

        /// <summary>稀有度宝石的贴图名（图在 `Resources/Art/ui_deck/`）。
        /// 取不到就返回 null —— 那颗宝石不画，卡面其余部分照常。
        /// <para>🆕 **2026-10-18（`A2136`）判据已查清 ⇒ 色相**不是**缺陷**：
        ///   · 原版的颜色**只由换 sprite 决定**：`CardRarityHolderController.SetCardRarity` 的方法体
        ///     里**只有** `UnityEngine_UI_Image__set_sprite`（`d:/2/tools/decomp_full/CardRarityHolderController__SetCardRarity.c`），
        ///     **没有任何写 `m_Color` 的分支**；预制体里那颗 `Card Rarity Sprite` 是 **UI `Image`**、
        ///     `m_Color` 恒白（`MonoBehaviour_-1177477944281752640.json`），5 张图 = `rarityLevelSprites[5]`
        ///     （`MonoBehaviour_857099535673695168.json` 的 5 个 PathID 依次落到
        ///     `1_40k_cardframe_rarity_common` … `5_…_special`）。
        ///   · 我们的 5 张图与解包件**逐字节相同**（common 三份导出 + `Resources/` 那份 md5 同为 `a1449a2394`；
        ///     rare 与 assetripper 导出逐像素同）。
        ///   · 用户对照的两张卡**稀有度本来就不同**：`司战`（`ASH83`）= **common** ⇒ 青；
        ///     `伊莱亚克·泽菲尔刃`（`ASH2`）= **legendary** ⇒ 金。「原版偏金」= 那张是传奇。
        ///   ⇒ **不加任何 tint**；换成正确的稀有度图即可。</para></summary>
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

        /// <summary>要不要给每张卡**再画一层前景角色抠图**（盖在卡框上 ⇒ 角色**越出卡框**）。
        ///
        /// 🔴 **默认 `false` = 不越出，这才是原版的做法**（**2026-10-10 改**；用户点名
        ///    「最严重的还是插图超出卡框」⇒ 真因就是这一层）。
        ///    原版的破框 = **立绘画在框下、由框的 alpha 剪出来** —— 我们的 `_art`（z = +0.03，在框下）
        ///    + `_frame`（z = 0）那两层就是它，**一字不动**。
        ///    「越出」在原版是**逐卡 opt-in**：`RawCardScript.useOverDraw`（偏移 **0x58**）→
        ///    `CardDisplayWindow` 调 `BasicCardUI.SetImageOverDraw(卡, 卡.useOverDraw)`
        ///    → `CardImageController.ChangeSize() → ChangeState() → DoChange()`（把立绘 re-parent + ×1.26999998）；
        ///    而 `currentState` 是**非序列化 private** ⇒ **出厂态 = `Small(0)` = 不溢出**。
        ///    🔴 **2026-10-22 就地更正（铁律 5）**：本段原来写「**值在服务器**（我们拿不到）」——
        ///    **不成立**：`cards_engine.json` **v9 起就有 `useOverDraw` 列**（现读 **641×1 / 486×0**），
        ///    已接进 `CardDef.UseOverDraw` → 本文件的 `UseOverDraw` → `ArtFrontVisible`（大图档两轴门控）。
        ///    ⛔ 上面那条「出厂态 = 不溢出」说的是**原版字段的缺省**，与本文件那个开关的**默认 `true`**
        ///    （= 保持我们现状）不是一回事，别混。
        ///    出处：`资料/战斗规格/战斗重建_0827/子代理读报_2dcard_0827.md:310`（D2 条）· 反编译
        ///    `d:/2/tools/decomp_full/CardDisplayWindow__CardOverdrawHandle.c` /
        ///    `BasicCardUI__SetImageOverDraw.c` / `CardImageController__ChangeState.c`。
        ///    **已裁定、⛔ 别推翻**：`资料/卡面复刻_失误复盘_0919.md:95-96`（含「『一律 ×1.27 更接近原版』
        ///    **是错的、已收回**」）· `资料/待办判据_卡面卡池与双语.md:120`（2026-09-28 已销账）。
        ///
        /// ⛔ **改成 `true` 就是复现那个缺陷**：那一层在框上**重画同一张立绘、带真 alpha** ⇒ 整条角色
        ///    轮廓盖住卡框（剑尖/肩甲出拱顶），再叠一层 `ArtCoverMargin`(1.04)。
        /// ⚠️ 口子留着只为「将来能恢复」：**小卡档**（本开关管的那一族）在原版**从不**溢出，
        ///    所以这一档仍然只有「整卡池一起开」这一种用法 —— ⛔ 别拿 `UseOverDraw` 来给**小卡**开门
        ///    （那个逐卡值在原版只喂 `CardDisplayWindow`，见 `CardDef.UseOverDraw` 的 doc）。
        ///
        /// 📌 `false` 这一档（原来是「`FrameCutout`：卡框按遮罩挖洞」）**已随 2026-10-10 那次改动删掉**
        ///    —— 它是我们自造的近似、非原版机制、且至今没人渲图看过（见那条建层处的注释）。
        /// 🆕 2026-10-18：本开关**现在只管【小卡档】**。「大图档越出」已经由 <see cref="BigCardOverdraw"/>
        ///    单独接管（默认开），两者互不影响 —— 别为了让详情窗出血而把这里改成 `true`
        ///    （那会把**所有**小卡一起变成出血，正是 2026-10-10 用户报的那个缺陷）。
        /// </summary>
        public static bool UseFrontLayer = false;

        /// <summary>探针/自检用：**临时指定卡框档**（1..4；`0` = 不覆盖，走正常口径）。
        ///
        /// <para>为什么要有这个口：正常口径是 `CardProgress.Level(artId)`（玩家存档，缺省 1），
        /// 而 **tier3/4 的战术卡框才是哥特拱窗**（tier1/2 是圆角矩形 + 徽记处的外凸缺口）
        /// —— 想渲一张拱顶的图，就必须能不改存档地把档位抬上去
        /// （`CardProgress.SetLevel` 会**落盘到玩家存档**，探针里⛔不许用，见那段的 `_persist` 说明）。</para>
        /// <para>⚠️ 只在**探针/自检**里临时改，改完立刻还原（`CardFaceProbe` 里那一段就是 3/4 两档各渲一张）。</para>
        /// <para>判据（原版一手）：卡框档 = 卡等级（`CardFramesSO.GetClanFrame(army, tier, cardType)`），
        /// 见建层处 `frameTier` 那一段。</para></summary>
        public static int DebugFrameTier = 0;

        /// <summary>这张卡的**档位**（1 基，1..`CardArt.MaxTier` = 原版 `CardTier + 1`）—— **判据只此一处**。
        /// <para>卡框（`CardArt.Frame/Sdf`）与**3D 卡体的 matcap**（`CardArt.Card3DMatcap`）**共读这一个值** ——
        /// 原版这两处同源（`CardMaterialHelper.matcapTexturesByCardTier[CardTier]` 与
        /// `CardTierUIController.SetTier` 都吃 `CardTier`）。</para>
        /// <para>取值：`DebugFrameTier`（自检探针覆盖，&gt;0 才生效）→ `CardProgress.Level(artId)`（玩家存档，缺省 1）→
        /// **没 `artId`** 的演示卡/占位卡退回 `CardArt.MaxTier`（那种卡不在 `CardProgress` 的表里，
        /// 按缺省 1 画会把演示卡框无故降档）。</para></summary>
        static int CardTierFor(CardData d)
        {
            return DebugFrameTier > 0
                 ? Mathf.Clamp(DebugFrameTier, 1, CardArt.MaxTier)
                 : (string.IsNullOrEmpty(d.artId) ? CardArt.MaxTier : CardProgress.Level(d.artId));
        }

        // ==================================================================
        //  🆕 2026-10-18 **大图档：立绘越出卡框 + 立绘 ×1.27**
        // ==================================================================
        //
        // 🔴 用户 2026-10-18 点名：「**卡片详情大图的立绘应该超出卡框**」
        //    （像 PnP 成品卡 `d:/2/Warpforge部队卡片/Aeldari/1督军/Warpforge_1_Anvirr-Keltoc.png`
        //     那样：头冠/剑/闪电压在金属框上）。
        //    判据（现核，整份读）→ `资料/普查产出_1018/R_卡片详情大图三问.md` §二（Q1 证据）· §五 第 1 条：
        //      · 原版卡图 rect **2.7484 × 2.7484 @ y=−0.044**、卡框 **2.2452 × 3.2572 @ y=−0.030**；
        //        卡框上挂 `SoftMaskScript`（`MaskArea` = 卡图、`FlipAlphaMask=1`；⚠️ 2026-10-24 订正：
        //        `AlphaMask` = null ⇒ 它做的是**整层淡没卡框**，不是「按立绘 alpha 挖洞」，见 `ArtVisibleThroughFrame`）
        //        ⇒ 角色从框下透出（立绘比框宽 **1.31×**）。
        //      · 大图态 `CardImageController.ChangeState`（状态 1）还会把立绘**放大 ×1.27**
        //        （`bigSizeMultiplier`）—— 框不动。
        //      · 最终可见范围 = **卡体矩形收边（2.0927 × 3.3313）+ 卡框 alpha 像素级挖洞**。
        //
        // 🔑 实现取 A（`_artFront`：真 alpha 盖在框上），不走 B（给卡框做 alpha 挖洞）：
        //    B 要在本工程渲染管线下写一个「按立绘 alpha 挖卡框」的 shader —— 2026-10-10 曾有一条
        //    `FrameCutout` 支路，**从没人渲图看过**就删了（见下面那条注释），且 shader 与 SoftMask
        //    的翻转语义都没在实况里验过。A 用的是**现成的、已接好的那一层**（`_artFront`，z=−0.008，
        //    本来就在框之上、数值/文字之下），立绘是抠图（84.4% 像素 alpha=0）⇒ 天然只把角色轮廓画到框上。
        //    ⚠️ 如实标注：A 与 SoftMask **不完全等价**（SoftMask 在**半透明像素**处是「框被部分抠掉」，
        //    A 是「立绘半透明地压在框上」）—— 逐像素差在边框处，肉眼是同一件事。
        //
        // ⛔ **只在大图档生效**（`_bigImage`：`CardUse.ZoomWindow` 或详情窗扇形槽名 `CardUI` / `CardUI (n)`，
        //    判据 → `IsBigCardView`）—— 小卡（手牌 / 卡池 / 收藏 / 卡组格 / 多卡窗）**一个字节都不动**：
        //    它们既不是大图档、也不传 `use: ZoomWindow`，且 `UseFrontLayer` 仍是 false。
        //
        // 🔴 **默认开**。回退：`CardView.BigCardOverdraw = false;`（**必须在新卡建出来之前**设 ——
        //    网格/层都在 `Build` 那一趟定死；已建好的卡不会跟着变）。一键回到 2026-10-18 之前的画法
        //    （不放大、不建前景层），便于并排比。
        /// <summary>大图档「角色压框」总开关（默认 **true**）。判据与取舍 → 上面那一段。</summary>
        public static bool BigCardOverdraw = true;

        /// <summary>大图档立绘的放大倍率 = 原版 `CardImageController.bigSizeMultiplier`（**1.27**）。
        /// 🔴 **2026-10-21 订正**：只乘**前景抠图层**（`_artFront`）；底层 `_art`（opaque 矩形底图）**保持 1.0**
        /// —— 共用一块 ×1.27 的网格会把矩形立绘放大到卡框外（用户报的「插图超过卡框」）。卡框不动；
        /// 放大发生在「收边到卡体」之前 ⇒ 放多大都不会画到卡体外面（原版也是卡体矩形收边）。
        /// 🔴 **2026-10-24（`D3`）**：本倍率**不是对所有卡类型都乘** —— 只有 Minion(0) / Hero(10) 乘，
        /// 判据与单一门控函数见 <see cref="BigArtScaleApplies"/>。</summary>
        public const float BigArtScale = 1.27f;

        /// <summary>自检钩子：**临时取消「×1.27 只给 Minion/Hero」这道门**（= 回到 2026-10-24 之前
        /// 「所有卡类型都乘」的旧行为）。**只给 `CardFaceProbe` 做灭自证用**，出厂 `false`。
        /// <para>⚠️ 必须在**建卡之前**设（倍率是 `Build` 那一趟烘进网格顶点的；
        /// 已建好的卡不会跟着变）—— 与 `BigCardOverdraw` / `DebugDisableArtClamp` 同一类口子。</para></summary>
        public static bool DebugDisableBigArtScaleGate = false;

        /// <summary>🆕 2026-10-24（`D3`）：**这张卡的大图档立绘该不该乘 ×1.27** —— 判据只此一处。
        ///
        /// <para>🔴 **判据（一手，亲读）**：`d:/2/tools/decomp_full/CardImageController__SetImage.c:31,79,110-118`
        /// <code>
        /// fVar12 = *(float *)(param_1 + 0x80);              // 组件上的一个倍率场（局部初值）
        /// …
        /// fVar12 = DAT_1834b2bb8;                            // 随后重设成 **1.0f**
        /// if ((*(int *)(param_1 + 100) == 1) &amp;&amp; ((param_3 == 10 || (param_3 == 0)))) {
        ///     fVar12 = *(float *)(param_1 + 0x20);           // ← 只有这里才取 `bigSizeMultiplier`
        /// }
        /// local_60 = local_40 * fVar12;  local_68 = (fVar11 * fVar12, fVar10 * fVar12);
        /// UnityEngine_Transform__set_localScale(lVar5, &amp;local_58, 0);   // ← 乘进 localScale
        /// </code>
        /// · `DAT_1834b2bb8` 用 `工具/read_literal.py` 从 `D:/2/unity_run_ref/GameAssembly.dll` 现读
        ///   = **4 字节 float `1`**（8 字节读是 `2`，按上下文是 float ⇒ 取 1）。
        /// · `param_1 + 0x20` = 序列化字段 `bigSizeMultiplier`；`CardImage.json`
        ///   （`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_5393623139443973056.json`）
        ///   现读 = **1.2699999809**。
        /// · `param_3` 的类型 = `CardTypeOptions`（签名桩 `CardImageController.cs` 的
        ///   `SetImage(Sprite, CardTypeOptions)`；枚举 `CardTypeOptions.cs` = `Minion 0 / Hero 10 / Tactic 20 / Whispers 40`）。
        /// ⇒ **状态 1（Big）∧ 卡类型 ∈ {0 Minion, 10 Hero}** 才乘；**Tactic(20) / 防御 / 其余一律 ×1.0**。</para>
        ///
        /// <para>⚠️ **防御卡「原版 cardType 是几」不值得再查**：`数据/游戏数据/cards_truth.json` 里
        /// 我们卡池里那 33 张 `type="defence"` 读出来是 **20**；而 `CardScalesByCardType.json` 另有一条
        /// `cardTypeOptions: 5 → 0.60`（`A7` 把它叫「防御」），但**没有任何一张卡读得到 5**。
        /// 两条路都不是 0/10 ⇒ **结论不受这个分叉影响**（门照样挡掉防御卡）。</para>
        ///
        /// <para>🔑 **为什么原版里这条门控「只看一处看不出来」**（2026-10-24 `D3` 现核补全 —— `A7` 只引了
        /// `SetImage` 那一处）：`ChangeState.c:90-95`（Small→Big 的正常路径）**不判卡类型**，
        /// 一律 `originalImageScale × bigSizeMultiplier`；但**只有 `useOverDraw=1` 的卡才会被送进状态 1**
        /// （`CardDisplayWindow__ShowCard.c:62-84` → `BasicCardUI.SetImageOverDraw` → `ChangeSize(Big)`），
        /// 而卡池现读 **`useOverDraw=1` 的正好只有 unit 585/586 + hero 56/56，
        /// tactic 446 + defence 39 全是 0**（`cards_engine.json`）⇒
        /// 「×1.27 只对 Minion/Hero 生效」是**这两条合起来**的结果，不是单靠 `SetImage` 那道 if。
        /// 我们这一侧**没有**「`useOverDraw=0` ⇒ 不放大」那一步（`_artFront` 照建，只由
        /// `ArtFrontVisible` 把它藏起来）⇒ **必须**在倍率上补这道门，否则战术/防御卡会烘出一块
        /// 按 1.27 算的网格（今天看不见，`useOverDraw` 数据一变就现形）。</para>
        ///
        /// <para>🔴 **2026-10-24（`H5`）就地订正（铁律 5）**：上一段最后那句「我们这一侧**没有**
        /// 『`useOverDraw=0` ⇒ 不放大』那一步」**已不成立** —— `H5` 把**状态**也接上了两轴
        /// （`BigImageState`：前台 × 逐卡许可），`useOverDraw=0` 的卡现在**根本不进状态 1**
        /// ⇒ 连 `ArtScaleFor(…, bigState)` 都拿到 `false`、倍率天然是 1.0。
        /// 本函数（按**卡类型**判）仍然**必须留着**，它挡的是另一半：原版 `SetImage.c:110-111` 那道
        /// `cardType ∈ {0,10}` 的 if（状态 1 时**再**乘的条件）。两条判据是**与**关系，⛔ 别合并、别删任一条
        /// （删了本条 ⇒ `DebugDisableBigArtScaleGate` 那条灭自证失去对象；
        /// 删了状态那条 ⇒ 相关卡 / 战术卡会被烘成 BIG 硬矩形）。</para>
        ///
        /// <para>🔑 **我们这一列里的单一判据 = `CardData.isUnit`**：它的上游 `CardDef.IsUnit` 的定义字面就是
        /// `Type == "unit" || Type == "hero"`（`RuleEngine/Core/CardDef.cs` 的 `IsUnit`），而那个定义本身又是
        /// 原版 `RawCardScript.IsUnit`（= `cardType` 0 或 10）的镜像
        /// （`RuleEngine/Core/ConditionEval.cs` 里 `IsUnitOrHero` 的 doc 写明了这条一一对应）。
        /// 同一段建层代码里 `clipWindow: !d.isUnit` 判的也是这一条 ⇒ **两处同源，不会各说各话**。
        /// ⛔ 别在这里改成读 `d.type` 字符串：那是同一件事的第二个数据源，空串（老构造点）时还要再定一套兜底，
        /// 两份判据迟早打架（`SubtypeLine` 已经为「空 `type`」留了一条兜底，本函数不需要再留一条）。</para>
        ///
        /// <para>⚠️ 池内实测（2026-10-24，`cards_engine.json` × `cards_truth.json` 按唯一名对账）：
        /// `unit → 0` 575 张 · `hero → 10` 48 张 · `tactic → 20` 432 张 · `defence → 20` 33 张
        /// —— **`isUnit` 与 `cardType ∈ {0,10}` 逐张一致，0 张不一致**。</para></summary>
        static bool BigArtScaleApplies(CardData d)
        {
            // 自检钩子：复现「一律 ×1.27」（灭自证用；见 `DebugDisableBigArtScaleGate`）。
            if (DebugDisableBigArtScaleGate) return true;
            return d.isUnit;        // = 原版 `cardType ∈ {Minion 0, Hero 10}`
        }

        /// <summary>🆕 2026-10-22（卡牌线 `T04` 第二笔）：**这张卡当前允不允许「角色压框」** ——
        /// 原版 `BasicCardUI.SetImageOverDraw(bool)` 的等价物（**默认 `true`** = 建出来的样子不变）。
        ///
        /// <para>🔴 **判据（一手，亲读）**：`CardDisplayWindow__ChangeCardPosition.c:75-79` —— 换位一开始
        /// `SetImageOverDraw(旧前台, false)` + `SetImageOverDraw(新前台, 它的 useOverDraw)`；
        /// `CardDisplayWindow__CardOverdrawHandle.c:14-33` 对 `idx != 0` 的相关卡**一律 false**
        /// ⇒ **卡片详情扇形的 1 主卡 + N 相关卡里，只有前台那张能溢出**。
        /// 报告 → `资料/普查产出_1021/W60_真机与Unity_结果.md` §四 ⑤ · §五（「扇形只有前台那张可溢出」这条待办）。</para>
        ///
        /// <para>⚠️ `false` = 原版 `CardImageController` 的**状态 0**（`ChangeState(0)`）：立绘在框下、
        /// **不放大、不在框上重画** ⇒ 我们的等价物是**整层关掉 `_artFront`**，露出来的就是底层 `_art`
        /// （1.0 那一档）。⛔ 别把它理解成「把 `_artFront` 缩到 1.0 继续画」—— 抠图板本身比卡框宽 1.22×
        /// （2.7484 vs 2.2452，判据同 <see cref="BigArtScale"/> 的 doc），画在框上仍然是「插图超出卡框」。</para>
        /// <para>🔴 **2026-10-24（`H5`）补全**：`false` 还**改底层 `_art` 的硬矩形档位** —— 原版状态 0 的
        /// `_Clamp_Range` 是 SMALL（`ChangeState.c:53-82`），我们原来只把 `_artFront` 藏掉、`_art` 仍按
        /// **BIG** 硬矩形烘（相关卡 / 战术卡因此一直画错窗口）。现在本开关是
        /// <see cref="BigImageState"/> 的一条轴 ⇒ 置 `false` 时 setter 会把三块网格按「状态 0」重烘。</para>
        ///
        /// <para>⚠️ 只在**大图档**（<see cref="BigImage"/>）上真起作用：小卡档若靠 `UseFrontLayer` 建了
        /// 同名那一层，那个老口子**不受本开关管**（见 `ArtFrontVisible`）。层是在 `Build` 那一趟建的 ⇒
        /// `BigCardOverdraw = false` 建出来的卡根本没有这一层，切本开关是空操作（不静默造层）。</para>
        public bool ArtOverdraw
        {
            get { return _artOverdraw; }
            set
            {
                if (_artOverdraw == value) return;
                _artOverdraw = value;
                // ⚠️ 判据与 `SetFace` 那条**共用同一个函数**（`ArtFrontVisible`）—— 两处各写一遍 = 迟早不一致。
                // 🔴 2026-10-24（`H5`）：这一轴**同时是大图档 BIG 状态的一条轴**（原版 `idx != 0 ⇒ Small`，
                //    判据见 `BigImageState`）⇒ 走 `ApplyBigStateAxes`（开关层 + 按新状态重烘网格）。
                ApplyBigStateAxes();
            }
        }
        bool _artOverdraw = true;

        /// <summary>🔴 🆕 **2026-10-22（卡牌线 `T04` 第一笔）：【这张卡】允不允许角色压框** ——
        /// 原版 `RawCardScript.useOverDraw` 的等价物（**默认 `true`** = 建出来的样子不变）。
        ///
        /// <para>🔴 **判据（一手 · 全量反编译）**：`CardDisplayWindow__CardOverdrawHandle.c:14-33` ——
        /// `useOverDraw == 0 || idx != 0` ⇒ `SetImageOverDraw(Small)`；`!= 0 &amp;&amp; idx == 0` ⇒ `Big`。
        /// 同一条口径还出现在 `CardDisplayWindow__ShowCard.c:75-84`（开窗那张前台）与
        /// `CardDisplayWindow__ChangeCardPosition.c:75-79`（换位时接位那张传
        /// `*(char *)(它的 RawCardScript + 0x58) != '\0'`，让位那张恒 `Small`）。
        /// 数据侧 = `CardDef.UseOverDraw`（`cards_engine.json` 的 `useOverDraw` 列，现读 641×1 / 486×0）。</para>
        ///
        /// <para>⚠️ **它和 <see cref="ArtOverdraw"/> 是两层，⛔ 别合并**：
        ///   · <see cref="ArtOverdraw"/> = **这一格是不是前台**（场景态；换位/开窗时由窗改）；
        ///   · 本开关 = **这张卡允不允许溢出**（逐卡数据；建卡时定，换位不改）。
        /// 原版把两者压成了一个参数的两次调用，但语义就是这两个轴 —— 合成 `ArtFrontVisible` 里那一个 `&amp;&amp;`。</para>
        ///
        /// <para>⚠️ `false` 的效果 = **关掉 `_artFront` 那一层**（露出 1.0 的底层 `_art`，不放大、不压框）——
        /// 与原版 `CardImageController` 状态 0 同义；⛔ 不是「把前景缩到 1.0 继续画」（那张抠图板本身比卡框宽 1.22×）。
        /// 只在**大图档**真起作用（`_artFront` 只在大图档建，见 `BigCardOverdraw`）。</para>
        /// <para>🔴 **2026-10-24（`H5`）补全**：本开关**同时是「进不进状态 1」那条逐卡轴** ——
        /// 原版 `useOverDraw == 0 ⇒ SetImageOverDraw(Small)`（`ShowCard.c:62-84`），状态 0 的
        /// `_Clamp_Range` = SMALL、且不乘 `bigSizeMultiplier`。原来我们只拿它开关 `_artFront`，
        /// 底层 `_art` 仍是 BIG 硬矩形（判据缺口 → `D3` §1·3 / `H1` §五·4）⇒ 现在它进
        /// <see cref="BigImageState"/>，置 `false` 时由 setter 重烘三块网格（见 `RebakeBigStateMeshes`）。</para>
        public bool UseOverDraw
        {
            get { return _useOverDraw; }
            set
            {
                if (_useOverDraw == value) return;
                _useOverDraw = value;
                // ⚠️ 与 `SetFace` / `ArtOverdraw` **共用同一个判据函数**（`ArtFrontVisible`）—— 两处各写一遍 = 迟早不一致。
                // 🔴 2026-10-24（`H5`）：这一轴也决定原版**进不进状态 1**（`useOverDraw == 0 ⇒ Small`，
                //    判据 `ShowCard.c:62-84` / `CardOverdrawHandle.c:7-39`，见 `BigImageState`）
                //    ⇒ 走 `ApplyBigStateAxes`（开关层 + 按新状态重烘网格）。
                ApplyBigStateAxes();
            }
        }
        bool _useOverDraw = true;

        /// <summary>`_artFront`（角色抠图压框层）现在该不该亮 —— **判据只此一处**（换形态口 `SetFace` 与
        /// 两个切换口 <see cref="ArtOverdraw"/> / <see cref="UseOverDraw"/> 共用；出生态 `Build` 建出来即亮，
        /// 与两者的默认 `true` 一致）。
        /// 小卡档那条老口子（`UseFrontLayer`）不受这两个开关管；大图档由它们决定。
        ///
        /// 🔴 **三层门控（2026-10-22 `T04` 合成完整，由外到内）**：
        ///   ① `_artFront == null` —— 这一层根本没建（非大图档 / 非抠图 / `BigCardOverdraw = false`）；
        ///   ② `_bigImage` —— 只有大图档才谈得上「溢出」；
        ///   ③ `_artOverdraw`（**前台**）× `_useOverDraw`（**逐卡** `useOverDraw` 列）—— 两个都真才亮。</summary>
        bool ArtFrontVisible
        {
            get
            {
                if (_artFront == null) return false;
                if (_faceMode == CardFace.Board) return false;
                return !_bigImage || (_artOverdraw && _useOverDraw);
            }
        }

        // ==================================================================
        //  🆕 2026-10-24（H5）**大图档的「状态」= 原版 `CardImageController` 的状态 1（Big）**
        // ==================================================================
        //
        // 🔴 判据（一手，全部亲读 `d:/2/tools/decomp_full/`，⛔ 不是照 D3 的报告转述）：
        //
        //  1. **谁把卡送进状态 1** —— `BasicCardUI.SetImageOverDraw(ImageInFrameSize, bool)`
        //     （签名桩 `BasicCardUI.cs:334`）→ `CardImageController.ChangeSize`（`CardImageController__ChangeSize.c:5`）
        //     → `ChangeState`（`CardImageController__ChangeState.c`）。**`SetImageOverDraw` 只有下面三处调用**：
        //       · `CardDisplayWindow__ShowCard.c:62-84`（开窗那张前台）
        //         —— `useOverDraw == 0`（`RawCardScript + 0x58`）⇒ `SetImageOverDraw(Small, instant)`；
        //            `!= 0` ⇒ 先 `SetImageOverDraw(Small)` 再 `SetImageOverDraw(Big)`；
        //       · `CardDisplayWindow__CardOverdrawHandle.c:7-39`（扇形每一格）
        //         —— `useOverDraw == 0 || idx != 0` ⇒ `Small`；只有 **`idx == 0` 且 `useOverDraw != 0`** ⇒ `Big`；
        //       · `CardDisplayWindow__ChangeCardPosition.c:74-79`（换位）
        //         —— 让位那张 `SetImageOverDraw(false)`；接位那张传 `useOverDraw`。
        //     ⇒ **状态 1 ⇔ 「这一格是前台」∧「这张卡 `useOverDraw != 0`」**（⛔ 与卡类型无关）。
        //
        //  2. **状态 1 到底改了什么** —— `ChangeState.c:84-113`：
        //       · `_Clamp_Range` 换成 BIG（`:100-104` 取的是静态字段 `+0x10` 那份 = `(0.75, 0.89, 0.25, 0.1)`）；
        //       · 立绘 `originalImageScale × bigSizeMultiplier`（`:90-95`，即我们烘进顶点的 ×1.27）；
        //       · `frameMask` 打开（`:105-108`）。
        //     `ChangeState` 第一句 `if (currentState == stateToChange) return;`（`:30-32`）——
        //     **Small 是出厂态**（`currentState` 非序列化）⇒ SMALL 那一档用的是材质序列化默认 `x=0.82`
        //     （= 我们的 `ClampRangeSmall`，判据见那段 doc）。
        //
        //  3. ⇒ **`bigState` 必须是这个合取**，⛔ 不能只看「是不是大图档窗口」——
        //     D3 那版 `_bigImage && BigCardOverdraw` 只在**两轴都是默认 `true`** 时与它一致，
        //     而生产里 **9 格扇形只有 1 格两轴全真**（相关卡 `ArtOverdraw = false`；战术/防御卡
        //     `useOverDraw = 0`，`cards_engine.json` 现读 tactic 446 + defence 39 全 0）。
        /// <summary>这张卡**现在**处不处于原版 `CardImageController` 的**状态 1（Big）** —— 判据只此一处
        /// （`Build` 建层、两个状态轴 setter 的重烘，两处都读它）。四个合取项 = 大图档 × 总开关 × 前台 × 逐卡许可。
        /// <para>⚠️ `BigCardOverdraw` 是**建卡前**定的静态总开关（后面翻它不会重烘，见它的 doc）——
        /// 这里读它只为让「建卡前关掉」这条路仍然是 Small。</para></summary>
        bool BigImageState
        {
            get { return _bigImage && BigCardOverdraw && _artOverdraw && _useOverDraw; }
        }

        /// <summary>这张卡当前该乘的立绘倍率（状态 1 **且** 卡类型 ∈ Minion/Hero 才 ×1.27，见 <see cref="BigArtScaleApplies"/>）。</summary>
        static float ArtScaleFor(CardData d, bool bigState)
            => (bigState && BigArtScaleApplies(d)) ? BigArtScale : 1f;

        /// <summary>`_art`（底层完整插图）的硬矩形档位 —— **恒 0（不套）**，单位卡与战术/防御卡一视同仁。
        /// <para>🔴 **2026-10-24（第七十三会话 `M2`）判据翻面**：`H1`（同日早些）曾把非单位卡改成
        /// `小图档 1 · 大图档 2`，理由是「原版战术卡只有 `CardImage` 一层立绘、硬矩形对所有卡型生效」。</para>
        /// <para>🔴 **2026-10-24 同日再订正（铁律 5 · `X1` §一 反汇编现核）—— 翻面的【理由】换了**：
        /// `_Clamp_Range` 的算式是 `alpha = saturate(alpha + inside01)`（`mad_sat`），
        /// **矩形内把 α 顶到 1、矩形外原样保留** ⇒ **它根本不裁画面**、从不把 α 置 0。
        /// ⇒ 「给 `_art` 套一个硬矩形」这件事在原版里**没有对应物**。下面四条「实况」证据**方向仍然对**，
        /// 但现在只算旁证（决定性判据是指令流）：</para>
        /// <list type="number">
        /// <item>用户自己截的**原版游戏**三张图（`桌面/warpforge参考图/`：`点击卡片查看详情的参考.png`
        /// 的「先知之道」大卡 · `收藏—张牌的界面参考图…png` 的战术卡 · `收藏—卡组的参考图.png`）
        /// 里，战术/防御卡的插图**铺满整个拱窗**（左右到框沿、上到拱顶），**没有任何黑边**。</item>
        /// <item>把我们的立绘 PNG（1024²）按多尺度 NCC 匹配进「先知之道」大卡（ncc **0.994**，k=0.335），
        /// 三个互不相邻的 patch 给出同一组缩放/原点 ⇒ 原版画的是**整张插画**
        /// （`u` 全段 0..1），**不是** `_Clamp_Range` 的中央 64%。</item>
        /// <item>套上硬矩形后：板 1.7436 → 只剩 ±0.5579，而战术卡框的画窗是 ±0.839…±0.872
        /// ⇒ 左右各留 **≈0.28 卡单位**的黑带、顶部也被压下去 —— 正是用户报的
        /// 「插图不覆盖战术卡框上面空着的地方」。</item>
        /// <item>离线复刻（`FrameMaskData`/`BuildFrameRows`/`ArtWindowMesh` 口径）跑遍
        /// **75 张 `*_strat*` 卡框**：`clampKind = 0` + `0.61/0.5` 时，覆盖画窗所需的半宽
        /// **最大值 = 0.8718 = 立绘板半宽本身** ⇒ **不套硬矩形时板刚好铺满每一张画窗**（一张都不漏）。
        /// 这条是「板 1.7436 够不够」的正面证据，⛔ 别再靠 `ArtCoverMargin` 猜。</item>
        /// </list>
        /// <para>⚠️ `_artFront`（大图档角色抠图层）**不受影响** —— 它的档位由 `ArtFrontMeshFor` 显式给
        /// （小图档 1 / 大图档 2）。🔴 **2026-10-24 更正（`X1` §一）**：原来这里写「原版那一层确实是
        /// `_Clamp_Range` 的等价物（有真 alpha 的抠图）」—— **不成立**：原版**从不把 α 置 0**
        /// （矩形外照用贴图 alpha），而我们是**把网格缩进矩形**（`ArtMeshInFrame`）
        /// ⇒ `_artFront` 的硬裁**是我们自己的近似、判据不足**。它只在 opt-in（`UseFrontLayer` 出厂 false）
        /// 时可见 ⇒ 生产画面不变。</para>
        /// <para>🧨 改坏法：返回非 0 ⇒ 战术/防御卡的插图立刻在拱窗两侧各缩进 0.28 卡单位
        /// （`CardBaseDemo.AssertTacticArtClamp` ① / `CardFaceProbe` A7 ④a·④c 都会红）。</para></summary>
        static int ArtClampKindFor(CardData d, bool bigState) => 0;

        /// <summary>🆕 **2026-10-24（`E9`）**：这张卡的**卡型立绘倍率** —— 原版 `CardScalesByCardType`
        /// 的 `cardScales[cardType]`。单列**一份判据**：`!isUnit ⇒ 0.61`（原版 20 = Tactic，战术**和**防御；
        /// `isUnit` 与 `cardType ∈ {0,10}` 逐张一致，见 `BigArtScaleApplies` 的实测那一段）。
        /// <para>⛔ 别改成读 `d.type` 字符串（那是同一件事的第二个数据源）；也⛔ 别把 0.60 给防御卡
        /// （那条键是 5，全池无卡）；<see cref="TacticArtScale"/> 的 doc 有判据。</para></summary>
        static float ArtTypeScaleFor(CardData d) => d.isUnit ? 1f : TacticArtScale;

        /// <summary>🆕 **2026-10-24（`E9`）**：这张卡的**卡型立绘偏移**（卡单位）—— 原版
        /// `tacticsOffsetPosition.y = 0.5`，**只给 `cardType == 20`**（<see cref="TacticArtDy"/>）。</summary>
        static float ArtTypeDyFor(CardData d) => d.isUnit ? 0f : TacticArtDy;

        /// <summary>`_art`（底层完整插图）的网格 —— `Build` 与状态重烘**共用**（原来只写在 `Build` 里）。
        /// ⚠️ 倍率恒 **1.0**（2026-10-21 用户报「插图超过卡框」那条：放大只给 `_artFront`）；
        /// 状态只改**硬矩形档位**。
        /// <para>🆕 2026-10-24（`E9`）：**卡型**倍率/偏移（`0.61` / `+0.5`）照传 —— 它和「大图档 ×1.27」
        /// 是两条独立的轴（原版一个在 `SetImage`、一个在 `ChangeState`）。</para></summary>
        static Mesh ArtLayerMeshFor(CardData d, Texture2D artTex, Texture2D frameTex, bool bigState, float dy)
            => ArtMeshInFrame(artTex, 1f, frameTex,
                              clipWindow: !d.isUnit, clampKind: ArtClampKindFor(d, bigState),
                              clampRange: null,
                              typeScale: ArtTypeScaleFor(d), typeDy: dy);

        /// <summary>`_artFront`（角色压框层）的网格 —— `Build` 与状态重烘**共用**。
        /// 大图档 = 卡类型倍率 + **大图档**硬矩形（`clipWindow: false`、`frameTex: null` —— 单位卡那支原样）；
        /// 小图档 = 1.0 + **小图档**硬矩形 + `clipWindow: !d.isUnit`（= 2026-10-24 `F1`/`R1` 补的那一支）。
        /// <para>🆕 2026-10-24（`E9`）：卡型倍率/偏移（非单位卡 `0.61` / `+0.5`）两支都传。</para></summary>
        static Mesh ArtFrontMeshFor(CardData d, Texture2D artTex, Texture2D frameTex, bool bigState, float dy)
            => bigState
                ? ArtMeshInFrame(artTex, ArtScaleFor(d, true), null, false, 2, null,
                                 ArtTypeScaleFor(d), dy)
                : ArtMeshInFrame(artTex, ArtScaleFor(d, false), frameTex, clipWindow: !d.isUnit, clampKind: 1,
                                 clampRange: null, typeScale: ArtTypeScaleFor(d), typeDy: dy);

        /// <summary>状态轴变了 ⇒ **按新状态重烘三块网格**（原版 `SetImageOverDraw → ChangeState → DoChange`）。
        /// <para>为什么必须重烘：档位与 ×1.27 都是**烘进顶点**的（`ArtMeshInFrame` / `FrameMesh` 的 uv2），
        /// 不是节点 `localScale`、也不是材质参数 ⇒ 改属性改不到。</para>
        /// <para>幂等且便宜：三块网格都走静态缓存（键含倍率 / 档位 / `clipWindow`）⇒ 切回去拿到的就是原来那一块，
        /// 断言可以逐顶点比「切回去 = 原来那份」。</para>
        /// <para>⛔ 只在**轴真的变了**时调（两个 setter 的 `if (value == _field) return;` 已经挡住重复赋值）。</para></summary>
        void RebakeBigStateMeshes()
        {
            if (!_bigImage) return;
            RebakeArtMeshes();
        }

        /// <summary>🔴 2026-10-24（`E24`）：**跨族换形态**（菜单/HUD 族 ⇄ 战斗卡族，见 `ArtUnitYFight`）时重烘。
        /// <para>为什么不能借 `RebakeBigStateMeshes`：它开头有 `if (!_bigImage) return;` —— 小卡（手牌 /
        /// 卡池）一个都不会被重烘，而族差**恰恰**落在小卡那 263 px 上（≈1.7 px）。</para>
        /// <para>⚠️ **只有跨族才调**：`Full ↔ Hand` 才变族，`Hand ↔ Board` 同族（都是战斗卡 prefab）。
        /// 入口只有 `SetFace` 一处（铁律 10 第 5 条）—— 出生那一档在 `Build`，它建网格时 `_faceMode` 已定。</para></summary>
        void RebakeArtFamilyMeshes() { RebakeArtMeshes(); }

        /// <summary>三块立绘相关网格（`_art` / 卡框 uv2 / `_artFront`）按**当前形态与状态**重烘一份。
        /// ⚠️ 场上（`CardFace.Board`）直接返回 —— 那一档不画 2D 卡面（3D 体那条路）。</summary>
        void RebakeArtMeshes()
        {
            if (_frameTex == null || _art == null || _artTexBuilt == null) return;
            if (_faceMode == CardFace.Board) return;          // 场上不画 2D 卡面（3D 体那条路）
            bool bigState = BigImageState;
            var amf = _art.GetComponent<MeshFilter>();
            if (amf != null) amf.sharedMesh = ArtLayerMeshFor(_builtData, _artTexBuilt, _frameTex, bigState, ArtDyFor(_builtData));
            if (_frame != null)
            {
                var fmf = _frame.GetComponent<MeshFilter>();
                if (fmf != null) fmf.sharedMesh = FrameMesh(_frameTex, _artTexBuilt,
                                                           ArtScaleFor(_builtData, bigState),
                                                           ArtTypeScaleFor(_builtData),
                                                           ArtDyFor(_builtData));
            }
            if (_artFront != null)
            {
                var afmf = _artFront.GetComponent<MeshFilter>();
                if (afmf != null) afmf.sharedMesh = ArtFrontMeshFor(_builtData, _artTexBuilt, _frameTex, bigState, ArtDyFor(_builtData));
            }
        }

        // ==================================================================
        //  🆕 2026-10-24（E5）**状态切换的补间** = 原版 `CardImageController.DoChange` 的 `scaleTime`
        // ==================================================================
        //
        // 🔴 判据（一手，逐条亲读 `d:/2/tools/decomp_full/` ＋ 解包资产）：
        //
        //  1. **时长 = 0.3 s**：`CardImageController__DoChange.c` 把 `*(float *)(this + 0x24)` 当
        //     `DOScale` / `DOColor` / `DOFloat` 三条补间的 duration；`CardImageController__.ctor.c:16`
        //     给 `+0x24` 写 `0x3e99999a` = **0.3f**；序列化侧同名同值：
        //     `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_5393623139443973056.json`
        //     的 `"scaleTime": 0.30000001192092896`。
        //  2. **缓动 = `Ease.OutBack`**：`DoChange.c` 三处 `SetEase<object>(tween, 0x1b, …)` —— `0x1b = 27`
        //     = `DG.Tweening.Ease.OutBack`（…InBack 26 / **OutBack 27** / InOutBack 28）。
        //     ⚠️ 这条是**显式**写的 SetEase，不是「没写 ⇒ 默认 OutQuad」那一族（本文件别处才是那种）。
        //  3. **触发条件 = `ChangeState` 的 `instant == false`**：`ChangeState.c` 的 `param_3 == '\0'`
        //     那一支才走 DOTween；`CardDisplayWindow__ChangeCardPosition.c:74-79`（换位）传的就是
        //     `instant = 0` ⇒ **换位要补**。`instant != 0` 那一支是 `set_localScale` + `Material.SetVector`
        //     直写（`DoChange.c` 后半段）⇒ 瞬时。
        //  4. **补的是三样、同一时长、同一条时间轴**：立绘 `localScale`（1.0 ↔ ×`bigSizeMultiplier` 1.27）·
        //     `_Clamp_Range` 那个 `Vector4`（SMALL ↔ BIG，`DOColor` 连续插值）· `frameMask` 材质的
        //     float（0 ↔ 1）。DoChange 里是三兄弟、**没有** Sequence。
        //  5. `ChangeState` 第一句 `if (currentState == stateToChange) return;` ⇒ **状态没变不补**。
        //
        // 🔑 **我们怎么落**：状态 1 的两件事（×1.27 与硬矩形）都**烘在网格顶点里**（`ArtMeshInFrame`），
        //   不是节点 scale / 材质参数 ⇒ 补间只能**逐帧重烘** `_artFront` 那一块（`ApplyBigStateTweenAt`）。
        //   网格走静态缓存 + 量化到 1/32 档（0.3 s @60fps ≈ 18 帧 ⇒ 一帧最多一档），
        //   收尾 `RebakeBigStateMeshes()` 拿回**精确**那一块（缓存命中，幂等）。
        //
        // ⚠️ **只在补间真能推进时才补**：批处理没有帧循环 ⇒ DOTween 不推进 ⇒ 补了会把卡停在中间态
        //   （同 `Shell/CollectionWindow` 那条 `Application.isPlaying` 闸的教训）。判据 =
        //   `Application.isPlaying || CardTween.Mode == UpdateType.Manual`（后者专给自检）。
        // ⚠️ **只补单位卡**：`_artFront` 两支结构不同（小图档 `clipWindow: !isUnit`、大图档不裁窗），
        //   中间态没有唯一解；而战术/防御卡池内 `useOverDraw` 全 0 ⇒ **生产上进不了状态 1**
        //   （`cards_engine.json` 现读 tactic 446 + defence 39 全 0）⇒ 那条路走瞬时，不引入近似。
        //   同一句也覆盖 `_art`（非单位卡的硬矩形档位会变，但那条状态切换交互上走不到）。

        /// <summary>状态切换补间的时长 = 原版 `CardImageController.scaleTime`（**0.3 s**）。
        /// 判据（一手）→ 上面那段第 1 条（ctor `+0x24 = 0x3e99999a` ＋ 序列化 `scaleTime` ＋ duration 实参）。</summary>
        public const float BigStateTweenDuration = 0.3f;

        /// <summary>状态切换补间的缓动 = 原版 `DoChange` 的 `SetEase(0x1b)` = **`Ease.OutBack`**
        /// （`0x1b` = 27）。判据 → 上面那段第 2 条。</summary>
        public const Ease BigStateTweenEase = Ease.OutBack;

        /// <summary>状态切换补间现在在不在播（自检用）。</summary>
        public bool BigStateTweenActive { get { return _bigStateTween != null; } }

        /// <summary>补间推进到的进度（0 = 起状态 · 1 = 终状态；`OutBack` 会略微过冲）。自检用。</summary>
        public float BigStateTweenProgress { get; private set; }

        /// <summary>这一帧的状态**已经烘进网格**的是哪一档（= 原版 `CardImageController.currentState`）。
        /// `Build` 结束时按出生状态写一次；补间收尾 / 瞬时切换时更新。</summary>
        bool _bigStateBaked;

        Tween _bigStateTween;
        /// <summary>那条补间的**目标态**（`KillBigStateTween` 收口时用它 —— 那一刻 `BigImageState`
        /// 已经被 setter 改成新值了，读不到「上一条要补到哪」）。</summary>
        bool _bigStateTweenTo;

        /// <summary>补间能不能真的推进（批处理里 DOTween 不跑帧循环 ⇒ 只能走瞬时那一支）。</summary>
        static bool BigStateTweenCanAdvance
        {
            get { return Application.isPlaying || CardTween.Mode == UpdateType.Manual; }
        }

        /// <summary>逐帧把「中间态」烘进 `_artFront`（原版 `DoChange` 那三条补间的等价物）。
        /// <paramref name="k"/> = 补间进度（0..1，`OutBack` 会过冲）；缓存键含量化后的倍率与硬矩形 ⇒
        /// 同一档只建一次网格。</summary>
        void ApplyBigStateTweenAt(float k, bool fromBig, bool toBig)
        {
            BigStateTweenProgress = k;
            if (_artFront == null || _artTexBuilt == null) return;
            var mf = _artFront.GetComponent<MeshFilter>();
            if (mf == null) return;
            // 量化到 1/32（一帧最多一档），过冲夹在 ±0.125（`OutBack` 约 10%；夹死只为不出现负尺寸网格）。
            float kk = Mathf.Clamp(Mathf.Round(k * 32f) / 32f, -0.125f, 1.125f);
            float scale = Mathf.LerpUnclamped(ArtScaleFor(_builtData, fromBig),
                                              ArtScaleFor(_builtData, toBig), kk);
            Vector4 clamp = Vector4.LerpUnclamped(fromBig ? ClampRangeBig : ClampRangeSmall,
                                                  toBig ? ClampRangeBig : ClampRangeSmall, kk);
            // 结构取**目标那一支**：单位卡两支的 `clipWindow` 都是 false ⇒ 只有倍率与硬矩形在动。
            var mesh = ArtMeshInFrame(_artTexBuilt, scale,
                                      toBig ? null : _frameTex,
                                      clipWindow: toBig ? false : !_builtData.isUnit,
                                      clampKind: toBig ? 2 : 1,
                                      clampRange: clamp,
                                      typeScale: ArtTypeScaleFor(_builtData),
                                      typeDy: ArtDyFor(_builtData));
            if (mesh != null) mf.sharedMesh = mesh;
        }

        /// <summary>起那条补间（原版 `DoChange` 的非 instant 支；`ChangeState` 已把 `frameMask` 翻开）。</summary>
        void StartBigStateTween(bool fromBig, bool target)
        {
            float t = 0f;
            var tw = DG.Tweening.DOTween.To(() => t, x => { t = x; ApplyBigStateTweenAt(x, fromBig, target); },
                                            1f, BigStateTweenDuration);
            var used = CardTween.Use(tw, BigStateTweenEase, this);
            used.SetTarget(transform);        // 外部 `transform.DOKill()` 能杀掉（同 `TweenPose` 那条的理由）
            used.OnComplete(() =>
            {
                _bigStateTween = null;
                BigStateTweenProgress = 1f;
                _bigStateBaked = target;
                RebakeBigStateMeshes();
                Show(_artFront, ArtFrontVisible);
            });
            _bigStateTween = used;
            _bigStateTweenTo = target;
            // `.From(…, setImmediately: true)` 的等价物：立刻摆到**起点**（原版那两个 bool 实参读出来是
            //   `setImmediately = true` / `isRelative = false`，同 `CollectionCard.AnimateIn` 那套）。
            ApplyBigStateTweenAt(0f, fromBig, target);
        }

        /// <summary>杀掉正在播的状态补间。**被杀时也要收口**（否则停在中间态：网格是一档、
        /// `_bigStateBaked` 是另一档 ⇒ 下一次切换会从错的起点再补一遍）。
        /// 收口到**它自己的目标态**（不是杀掉那一刻的 `BigImageState` —— 后者已经被 setter 改成新值了）。</summary>
        void KillBigStateTween()
        {
            var tw = _bigStateTween;
            if (tw == null) return;
            _bigStateTween = null;
            if (tw.IsActive()) tw.Kill();      // `Kill()` 只触发 OnKill、**不触发 OnComplete** ⇒ 收口在这里自己做
            _bigStateBaked = _bigStateTweenTo;
            BigStateTweenProgress = 1f;
            RebakeBigStateMeshes();
            Show(_artFront, ArtFrontVisible);
        }

        /// <summary>两个状态轴（<see cref="ArtOverdraw"/> / <see cref="UseOverDraw"/>）setter 的**唯一落点**：
        /// 先按合成判据开关 `_artFront`，再按新状态换网格（能推进时走补间，否则瞬时）。
        /// ⛔ 两处各写一遍 = 迟早不一致。</summary>
        void ApplyBigStateAxes()
        {
            bool target = BigImageState;
            if (_bigStateTween != null) KillBigStateTween();   // 叠补间：先收口到上一条的目标态
            bool from = _bigStateBaked;
            if (from == target)
            {
                Show(_artFront, ArtFrontVisible);              // 状态没变（原版 `ChangeState` 同样直接 return）
                return;
            }
            // 原版 `ChangeState` 在 `DoChange` **之前**就翻 `frameMask.enabled`（收 :49 / 开 :105）
            //   ⇒ 我们也在**补间起点**翻显隐。变小时先**留着**那层、让它在补间里缩回去，收尾那一刻再藏
            //   （我们是「抠图叠在框上」的分层，提前藏掉就看不到这一缩了 —— 如实标注的等价物差异）。
            if (target) Show(_artFront, true);
            if (!BigStateTweenCanAdvance || !_bigImage || !BigCardOverdraw || !_builtData.isUnit
                || _artFront == null || _frameTex == null || _faceMode == CardFace.Board)
            {
                Show(_artFront, ArtFrontVisible);
                RebakeBigStateMeshes();
                _bigStateBaked = target;
                BigStateTweenProgress = 1f;
                return;
            }
            StartBigStateTween(from, target);
        }

        // 🔴 **2026-10-10 删掉了 `FrameCutoutMaterial()` / `_frameCutMat`** —— 它们只服务于
        //    `UseFrontLayer = false` 那条「卡框挖洞」支路，而那支路已删（理由见建层处的注释）。
        //    `Assets/CardPresentation/Shaders/FrameCutout.shader` **本身没删**（不在本文件管辖范围）——
        //    现在**没有任何 shader 引用它**。

        // ==================================================================
        //  卡框 `uv2` 与立绘 uv 的对齐判据 —— 不用出图就能断
        //
        //  这条判据坏过的两次（2026-09-13 缓存键 / 2026-10-07 `ArtCoverMargin`）**都是「采样对不齐」**，
        //  而「对不齐」在断言的层面是**可以直接算的**：卡框 mesh 的 `uv2` 必须等于
        //  **立绘 mesh 在同一点上的 uv**。把它做成一函数 ⇒ 以后任何一处改歪都会当场红。
        //
        //  ⚠️ **2026-10-10：现在没有任何 shader 会去采这份 `uv2` 了**（采它的 `FrameCutout` 支路已删）。
        //     判据**留着** —— 它是「框与立绘两套坐标同一份（`ArtUvAt`）」的守卫；将来若按原版
        //     `useOverDraw` 逐卡 opt-in 复活那条路，靠的还是这份对齐。
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

        // 🔴 **2026-10-10 删掉了 `CheckCutoutWiring`** —— 它断的是「`UseFrontLayer = false` 那条路上
        //    卡框材质接成了 `CardPresentation/FrameCutout` + `_CutMask`」；那条支路已删（见建层处注释），
        //    这个判据**没有对象了**。新的判据（小卡档不建前景层 / opt-in 那条路仍然接得通）落在
        //    `Editor/CardBaseDemo.cs` 的 `AssertFrameCutout` 里。
        // ⚠️ 2026-10-18：那里断的是**小卡档**（探针卡名 `cutout_probe`，不是大图档）⇒ 仍然成立；
        //    大图档建 `_artFront` 是另一条路，判据见 `BigCardOverdraw`。

        /// <summary>
        /// 卡框网格。`art` 只用来写 **uv2**（= 立绘那套 UV）—— 破框 shader 靠它采立绘的 alpha 遮罩。
        /// ⚠️ 缓存键要把 art 也算进去：同一张卡框配不同立绘，uv2 不一样。
        /// 🆕 2026-10-18：`artScale` 也要进键 —— 大图档立绘 ×1.27，它的 uv2 与小卡那份**不是同一个映射**
        /// （判据：`ArtUvAt` 走 `ArtBoard(art, artScale, …)`，同一份判据 ⇒ 这里也必须传同一个倍率）。
        /// <para>🆕 2026-10-24（`E9`）：`typeScale` / `typeDy` 同样进键与透传 —— uv2 必须和**生产立绘网格**
        /// 用同一组卡型变换，否则非单位卡的遮罩整体错位（同样两个值：`0.61` / `+0.5`）。</para>
        /// </summary>
        static Mesh FrameMesh(Texture2D tex, Texture2D art = null, float artScale = 1f,
                              float typeScale = 1f, float typeDy = 0f)
        {
            Mesh m;
            // ⚠️ 缓存键**必须把立绘也算进去**：uv2 是「立绘那套 UV」，同一张卡框配不同立绘就不一样。
            //    只按卡框缓存的话，所有卡都会拿到**第一张卡的 uv2** —— 破框遮罩就切花了
            //    （2026-09-13 踩过：整张卡的插图变成一片彩色马赛克）。
            int cacheKey = tex.GetInstanceID() * 397 ^ (art != null ? art.GetInstanceID() : 0);
            cacheKey = cacheKey * 397 ^ Mathf.RoundToInt(artScale * 1000f);
            cacheKey = cacheKey * 397 ^ Mathf.RoundToInt(typeScale * 10000f);
            cacheKey = cacheKey * 397 ^ Mathf.RoundToInt(typeDy * 10000f);
            if (_frameMeshCache.TryGetValue(cacheKey, out m) && m != null
                && (art == null || m.uv2 != null && m.uv2.Length == 4)) return m;

            var uv = FrameUv(tex);
            var q = FrameQuad(tex);                            // (宽, 高, 中心y)，world
            float w = q.x * 0.5f, h = q.y * 0.5f, oy = q.z;

            // ⚠️ 用**卡框自己的**尺寸（2.2452×3.2572 @y−0.03 卡单位），不是卡本体的。
            // 🔴 **2026-10-19（`A2138`）**：UV 取 **sprite rect `m_Rect`(171,11,682,998)**（见 `FrameUv`）——
            //    原版 `Image` 是 `m_UseSpriteMesh=1` ⇒ 整块 sprite rect（**含透明留白**）被拉进这个矩形，
            //    不透明内容（`textureRect`）自然缩在 rect 里面、并按 `textureRectOffset` 偏置。
            //    ⛔ **别再把 UV 裁到 alpha bbox**：那会把可见内容放大 7.7% 宽 / 6.1% 高（逐帧表
            //    `资料/普查产出_1019/K1_A2138_逐帧映射差.tsv`）。
            //    📌 本节原写「按 sprite 自身的比例 fit 进去，不拉伸（2.2452 → 2.1159）」——那是 2026-09-12 的
            //    旧口径，2026-09-27 已按原版 `m_PreserveAspect=0` 改成**固定矩形**，本行是它的延续说明。
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
                    uv2[i] = ArtUvAt(art, m.vertices[i].x, m.vertices[i].y, artScale, typeScale, typeDy);
                m.uv2 = uv2;
            }

            _frameMeshCache[cacheKey] = m;
            return m;
        }

        static readonly Dictionary<int, Mesh> _frameMeshCache = new Dictionary<int, Mesh>();
        static readonly Dictionary<Texture2D, Vector3> _frameQuadCache = new Dictionary<Texture2D, Vector3>();

        /// <summary>
        /// 卡框那一层的 quad：**宽、高、中心 y**（world 单位）。**判据只有这一份** ——
        /// `FrameMesh`（画框）读它（`ArtMeshInFrame` 的板走 `ArtBoard`，
        /// 不从这里取尺寸；两处各算一遍的话立绘和框就会错位）。
        /// 🔴 **2026-10-24（`H5`）**：`ArtMeshInFrame` 原来还收一份 `frameQuad`，**方法体一次没读过**
        /// ⇒ 那个死形参已删（判据 / 清账见它的 doc）。本函数的消费者只剩 `FrameMesh`。
        /// <para>🆕 **2026-10-19（`A2138`）**：矩形**不受 uv 影响**（恒 = `FrameUnitW/H`，原版 `m_PreserveAspect=0`
        /// 的固定矩形）；「画什么」由 `FrameUv` 决定（= sprite rect）。本节旧注「返回值依赖 `FrameUv`
        /// （量不透明 bbox）、贴图必须开 Read/Write」**已过期**：卡框的 uv 现在是**常量**，
        /// 不需要读像素；Read/Write 仍留着只为**非 1024² 的兜底**与 alpha 右缘（`FrameAlphaUv`）。</para>
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
            // 📌 🔴 **2026-10-19（`A2138`）就地订正**：本行原文是「`uv`（= 贴图不透明的那一块）仍然
            //   只取那一块 —— 透明边不参与拉伸」——**不成立**。原版 `m_UseSpriteMesh=1`
            //   ⇒ 拉进矩形的是 **sprite rect（`m_Rect` 682×998，含透明留白）**，不透明内容只是
            //   rect 里面按 `textureRectOffset` 偏置的一块（见 `FrameUv` / `FrameSpriteRectX`）。
            //   ⇒ 「画多大」= 固定矩形 ✓，而「画什么」= **整块 sprite rect**。
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
        /// <summary>卡名：**数字版材质 `Pragati-Regular Light Cream` 的 `_FaceColor`** = (0.992, 0.868, 0.733)
        /// → `Color32(253, 221, 187)`（顶点色白 ⇒ 屏上就是材质本色）。
        /// <para>🔴 **2026-10-17（界面线 `T07` · `A1753` #18）就地改判（铁律 5）**：原来这里写「卡名**白**」、
        /// 依据是 `D:/2/Warpforge部队卡片/` 的**成品卡图**（PnP）。**那条判据不成立** ——
        /// 铁律 10 第 3 条：「PnP 成品卡图是**印刷品**，不是数字版规格」，它的白是印刷版式，不是屏上值。
        /// 数字版真值 = 原版 TMP 的 `m_sharedMaterial`（`NameTextUnit` / `NameTextTactic` 逐颗实读）
        /// 所挂 **`Pragati-Regular Light Cream`**，`_FaceColor = (0.992, 0.868, 0.733)`（暖白）、顶点白。
        /// 判据全文 = `资料/普查产出_1017/UI/R_T07_剩余四项.md` §4·2 #18。</para>
        /// <para>⚠️ 数值取法与 <see cref="InkArmy"/> 同一档（3 位小数 × 255 取整）：0.992→253 · 0.868→221 · 0.733→187。</para>
        static readonly Color32 InkName   = new Color32(253, 221, 187, 255);
        /// <summary>阵营行 / 兵种行：原版材质 `Pragati-Regular Card Unit type` 的 `_FaceColor` = (1.0, 0.6055, 0.3443)
        /// ⇒ (255, 154, 88)。🆕 **2026-10-17（`A1753` #19）**：红通道由 **253 → 255**（原来取的是 0.9922 那个
        /// 「卡名橙」的近似值；这里改用材质原值 ⇒ 字面量能逐位对上 (255, 154, 88)）。</summary>
        static readonly Color32 InkArmy   = new Color32(255, 154, 88, 255);
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
            // 基材走 `BaseMaterial()`（`CardPresentation/CardSprite`）：吃 alpha（圆角透明处才不会被画成黑块）
            // + 有 `_Color` 可以着色 + 🆕 `_GreyScale` 可以做原版那种去饱和。
            // ⚠️ 2026-10-19（`A2139`）前这里自己 `Shader.Find("Sprites/Default")`、绕开了基材那一处 ——
            //    程序生成的卡面（没有原版卡框时）会**灰不了**；现在收口到同一支。
            var m = new Material(BaseMaterial());
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
            // 🔴 **2026-10-14（`A2006①`）末尾补上 `d.type`**：费用那一格画不画由 `ShowsCost(d)` 判，
            //    而它读 `d.type` ⇒ 照上面那条「缓存键必须带**所有画进贴图的字段**」的口径，它必须在键里。
            return $"{ColorUtility.ToHtmlStringRGBA(d.frame)}|{d.title}|{d.cost}|{d.melee}|{d.ranged}|{d.health}|{d.armor}|{d.keywords}|{d.isUnit}|{d.type}";
        }

        /// <summary>数值层（透明底）：费用 + 卡名 + 关键词 + 三个数值</summary>
        /// <param name="statsOnly">**只烘「四个数值」**（攻/远/甲/血），不烘费用 / 卡名 / 效果文字。
        /// 场上用（原版在 inPlay 把整张 `2DCard` 关掉，只留 `Board Elements` 那棵子树里的数值）——
        /// 见 `CardFace` 的注释。⚠️ 缓存键要带上这一位，否则场上和手牌会互相拿到对方的贴图。</param>
        static Texture2D InfoTexture(CardData d, bool statsOnly = false, bool skipStats = false,
                                     Texture2D frameTex = null)
        {
            // 🔴 2026-10-18（W15）：**卡框要进缓存键** —— 四个数值 / 费用的落点是按这张卡框的
            //    uv 反算的（`FaceStatAt01` / `FaceCostAt01`）⇒ 同一张卡换一种卡框，烘出来的图不一样。
            string key = (statsOnly ? "infoStat|" : "info|") + (skipStats ? "nostat|" : "")
                       + (CostBig ? "costbig|" : "")
                       + (TmpFont.Available ? "costtmp|" : "costbake|")   // 🆕 K4 D3：有 TMP 就不烘数字
                       + (frameTex != null ? "f" + frameTex.GetInstanceID() + "|" : "")
                       + CacheKey(d);
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
            // 🔴 **2026-10-14（`A2006①`）：判据改成 `ShowsCost(d)`**（督军不印费用数字；出处见那个函数）。
            //    原来这里只写 `cost >= 0` ⇒ **督军照样把数字烘进 `_info`**（今天印 `0`、`A1773` 之后印 `2`）。
            //    ⚠️ 底板那一层（`Build` 的 `_costBg`）是**另一处**，两处都要过这一条 —— 见 `ShowsCost` 末段。
            if (ShowsCost(d))
            {
                // 🔴 2026-10-18（W15）：**跟着卡框上的阵营徽记走**（水平同心、上沿贴徽记底边）——
                //    判据与实测数字见 `FaceCostAt01` 上面那一段。没有卡框时它退回 `CostAt`。
                // 🔴 2026-10-18（`A1549`）：**两档**（`CostBig`）—— 落点走 `CostAt01`、数字与六边形
                //    半径同乘 `CostScale`（原版 `Cost Container Big` 的数字框也正好是 ×1.2）。
                var costAt = CostAt01(frameTex);
                float cs = CostScale;
                if (CardArt.DeckUi("Card_Frame_Cost_Icon") == null)
                {
                    // 徽记本身花纹很花，所以要用**不透明的宝石**盖住它 —— 半透明/阵营色都读不出来
                    var rim = new Color32(14, 14, 18, 255);
                    // 半径：原版费用格是 0.4×0.4 卡单位 → 0.2/2.0927 = 0.0956 卡宽（大号档 ×1.2）
                    FillHexagon(px, FaceW, FaceH, costAt.x * FaceW, costAt.y * FaceH, 0.0956f * cs * FaceW, rim);
                    FillHexagon(px, FaceW, FaceH, costAt.x * FaceW, costAt.y * FaceH, 0.0779f * cs * FaceW, InkCostGem);
                }
                // 🔴🔴 **2026-10-25（`E` · K4 §②·D3）：数字改走【第五件真 TMP】**（原版 `Cost Container/CostText`）
                //   ⇒ 这里**不再烘**（否则一张卡上会同时有 TMP 与烘图 = 重影）。
                //   只有**字体资产取不到**（`TmpFont.Available == false`，那档根本建不出 TMP）才退回烘图，
                //   与卡名/正文那几层「有 TMP 就不点阵」是同一条规矩（见上面 ② 那两段）。
                //   ⚠️ 六边形/宝石**照旧烘**（那是底板 `Cost Background` 的替身，不是数字）。
                if (!TmpFont.Available)
                {
                    if (!PragatiDigits.Available)
                        DrawCenteredAt(px, costAt, d.cost.ToString(), 4, InkCost);
                    else
                        PragatiDigits.Draw(px, FaceW, FaceH, d.cost.ToString(),
                                           Mathf.RoundToInt(costAt.x * FaceW),
                                           Mathf.RoundToInt(costAt.y * FaceH), true,
                                           // 费用六边形半径 0.0956 卡宽（见上面 `FillHexagon` 那两行）
                                           // ⇒ 净宽 ≈ 2×0.0956×256 ≈ 49 面像素，留边距取 41（大号档 ×1.2）
                                           Mathf.RoundToInt(0.0956f * cs * FaceW * 2f * 0.84f));
                }
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
                //    🔴 **2026-10-18（W15）：2D 卡面那三个改成「跟着卡框凹槽走」**（`FaceStatAt01`）——
                //    原来写死 `MeleeAt/RangedAt/HealthAt`（= 原版容器位置），而卡框是按各档自己的
                //    alpha bbox 拉伸的 ⇒ 凹槽落点随卡框变，数字就偏出圈了（判据与实测见 `FramePxAt01` 那一段）。
                //    场上（`statsOnly`）那套**不动**：3D 卡体没有这张卡框图。
                var meleeAt  = statsOnly ? BoardMeleeAt  : FaceStatAt01(StatMelee,  frameTex);
                var rangedAt = statsOnly ? BoardRangedAt : FaceStatAt01(StatRanged, frameTex);
                var armourAt = statsOnly ? BoardArmourAt : ArmourAt;
                var healthAt = statsOnly ? BoardHealthAt : FaceStatAt01(StatHealth, frameTex);
                // 🔴 **2026-10-20（`W58`）：场上（`statsOnly`）的两个尺子都与 2D 卡面分开** ——
                //    ① 四个数值原版**全是 thick 0.12**（同一份 `White_thick Offset for 3D` 材质）；
                //    ② 最大墨宽按原版 3D TMP 的 Rect（53/59/45 px），⛔ 不套 2D 的 31。
                //    ⚠️ 这条路只在字形表缺失时才会真画（`SplitStats` 为假）；判据仍要与
                //    `StatTexture` 那四个独立层**同一份**，所以两边都走 `StatVariant` / `StatMaxW`。
                DrawStatAt(px, meleeAt, d.melee, StatVariant(StatMelee, statsOnly),
                           statsOnly ? StatMaxW(StatMelee) : 0);
                DrawStatAt(px, rangedAt, d.ranged, StatVariant(StatRanged, statsOnly),
                           statsOnly ? StatMaxW(StatRanged) : 0);
                if (d.armor > 0) DrawStatAt(px, armourAt, d.armor, StatVariant(StatArmour, statsOnly),
                                            statsOnly ? StatMaxW(StatArmour) : 0);   // 护甲 = 原版 Thick 描边
                DrawStatAt(px, healthAt, d.health, StatVariant(StatHealth, statsOnly),
                           statsOnly ? StatMaxW(StatHealth) : 0);
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
        //  ⚠️ **只在 `CardFace.Board` 走这条**：非场上那四档的数值是**另一套载体** ——
        //     🔴 2026-10-25（`CF-3`）起 = 四件真 TMP（`BuildFaceStatTexts`，原版 `2DCard/Front/Card Info/…` 同构）；
        //     再早是烘在 `_info` 贴图里，现在那条只剩「字体资产取不到」的兜底。两边的判据都是 `SplitStats`（一处）。
        //  ⚠️ 字形表不在（`PragatiDigits.Available == false`）⇒ **不拆**，退回老路（不静默不画）。
        // ==================================================================

        /// <summary>一个数值格独立贴图的边长（面像素）。🔴 **与 <see cref="StatCell"/> 成对**，改一个必须改另一个。</summary>
        const int StatCanvas = 64;
        /// <summary>Health 那一格自己的画布边长 —— 原版字号 **8**（其余三个 **7**，+14.3%）。
        /// <para>🔴 **2026-10-20（`W58`）**：四个 3D TMP 的 `m_fontSize` 是 7/7/8/7，而**它们各自的
        /// RectTransform scale 不同**（Melee/Range/Armour 0.48287、Health 3.46789 × 父 0.13924），
        /// 有效世界字号 3.380 vs 3.863 —— 正好 8/7。`PragatiDigits.Draw` 没有字号参数（字形表
        /// `DigitH = 28` 是常量）⇒ **画进一块 64×7/8 = 56 的画布、再铺满同一个 `StatCell`**，
        /// 数字就按 8/7 放大（最近邻，像素密度差一档可忽略）。</para></summary>
        const int StatHealthCanvas = StatCanvas * 7 / 8;      // = 56

        static int StatCanvasOf(int s) { return s == StatHealth ? StatHealthCanvas : StatCanvas; }

        /// <summary>数值格在**卡单位**里的边长 = `CardUnitW × StatCanvas / FaceW`。
        /// 这样算出来，数字的像素尺寸与「烘在 `FaceW` 宽的大图里」时**逐像素相同** ——
        /// 卡面上别处的数字（手牌那张脸）与场上这四个看上去一样大。</summary>
        static float StatCell { get { return CardUnitW * StatCanvas / FaceW; } }
        static readonly System.Collections.Generic.Dictionary<string, Texture2D> StatCache =
            new System.Collections.Generic.Dictionary<string, Texture2D>();

        /// <summary>第 `s` 个数值格用哪一套坐标（两套位置见 `MeleeAt` 那一段的注释）。
        /// 🔴 2026-10-18（W15）：2D 那一档走 `FaceStatAt01`（跟着卡框凹槽），与画数字/命中区同一份。</summary>
        static Vector2 StatAt01(int s, bool board, Texture2D frameTex = null)
        {
            switch (s)
            {
                case StatMelee:  return board ? BoardMeleeAt  : FaceStatAt01(StatMelee,  frameTex);
                case StatRanged: return board ? BoardRangedAt : FaceStatAt01(StatRanged, frameTex);
                case StatArmour: return board ? BoardArmourAt : ArmourAt;
                default:         return board ? BoardHealthAt : FaceStatAt01(StatHealth, frameTex);
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

        /// <summary>某一格的**基准值**（原版 9 参 `SetText` 的 `baseValue` 实参）。
        /// `statBasesKnown == false` 时调用方**不许用**它（见 `CardData.statBasesKnown` 的 doc）。</summary>
        static int StatBaseValue(CardData d, int s)
        {
            switch (s)
            {
                case StatMelee:  return d.baseMelee;
                case StatRanged: return d.baseRanged;
                case StatArmour: return d.baseArmour;
                case StatHealth: return d.baseHealth;
                case StatCost:   return d.baseCost;
                default:         return 0;
            }
        }

        /// <summary>**数值颜色 = 原版 `CardTextCountersController.DoColorChange`** —— 判据只此一处。
        /// <para>一手判据：`CardTextCountersController__DoColorChange.c` 逐句 ——
        /// ① `param_2 == param_3` ⇒ 刷回 `originalColors[i]`（白）；
        /// ② 否则按 `statIncreaseIsPositiveToThePlayer` 的极性取两色
        /// （`CardTextCountersColorsSO`：`+0x18 statIncreaseColor` = **(0,1,0)** ·
        ///  `+0x28 statDecrease` = **(0.9725,0.306,0.353)**；工具提示写的正是「health/attack 涨 / 落」）。</para>
        /// <para>🔴 **比的是【当前值 vs 基准值】，不是【新 vs 旧】** —— 9 参 `SetText` 的指令流
        /// （VA `0x18060C180`，2026-10-25 用 `工具/disasm_va.py` 现读）那三句：
        /// `mov ebx,r9d`（= `baseValue`）· `mov edi,r8d`（= `currentValue`）·
        /// `call DoColorChange(this, edx=currentValue, r8d=baseValue)`
        /// ⇒ 颜色是**持续态**：受伤的生命一直红、被 buff 的攻击一直绿，**回到基准才变白**。
        /// （我们 2026-09-29 那版按「新 vs 旧」闪一下，**判据不同**；只在基准未知时才走它。）</para>
        /// <para>极性：近战 / 远程 / 生命 / 护甲四个 counter 的 `statIncreaseIsPositiveToThePlayer` = **1**；
        /// **费用**那个 counter = **0**（越低越好）—— 实测五份 `MonoBehaviour_*.json`：
        /// `…_6320885271709195200`（费用，**0**）· `…_-3358613892933444672`（远程，1）·
        /// `…_1339511855435914176`（生命，1）· `…_4301255605536791488`（近战，1）·
        /// `…_576116891963464640`（护甲，1）。</para></summary>
        public static Color StatColorVsBase(int s, int cur, int bas)
        {
            if (cur == bas) return Color.white;
            bool increaseIsGood = s != StatCost;                   // 费用：越低越好（`statIncreaseIsPositiveToThePlayer = 0`）
            bool good = increaseIsGood ? cur > bas : cur < bas;
            return good ? CardFeel.StatUpColor : CardFeel.StatDownColor;
        }

        /// <summary>某一格数值**该不该显示** —— 🔴 **判据只此一处**（两档载体都调它：场上的网格层
        /// `SyncStatLayers` · 非场上的四件 TMP `SyncFaceStatTexts`；⛔ 别在任一处再拼一遍）。
        /// <para>判据（一手）= 原版 `BasicCardUI__SetRawCardData.c` 对**每个** counter 各一次
        /// `CardTextCountersController.Toggle(...)`，配合 `dump.cs` 的 `RawCardScript` 字段表：</para>
        /// <list type="bullet">
        ///   <item>近战 `+0xb0` ← `meleeAttack(+0xA4)` · 远程 `+0xb8` ← `rangeAttack(+0xA8)` · 生命 `+0xa0` ← `maxHealth`
        ///   —— 三者的 Toggle 参数**都只是 `IsUnit`** ⇒ **远程 = 0 也照印一个 `0`**。</item>
        ///   <item>护甲 `+0xc0` ← `armour(+0xA0)` —— Toggle 的参数是 `IsUnit && armour != 0` ⇒ **0 就整格不显示**
        ///   （那条「值 ≠ 0」的门**只属于护甲**，别推广到远程）。</item>
        /// </list>
        /// <para>⚠️ 督军不印**费用**是另一件事（`ShowsCost`，`manaCost(+0x78)` 的高字节哨兵），与本函数无关。</para></summary>
        static bool StatShownFor(int s, CardData d)
        {
            if (!d.isUnit) return false;
            return s != StatArmour || d.armor != 0;
        }

        /// <summary>这个数值用哪一档字形（描边宽度由 <see cref="PragatiDigits.OutlineWidth(PragatiDigits.Variant)"/> 读，
        /// 那个数就是烘字形时用的数）。
        /// <para>🔴 **2026-10-20（`W58`）：场上四个数值原版【全是 thick 0.12】** —— 四份 TMP 的
        /// `m_sharedMaterial` 逐份实读都是 `Pragati-Regular White_thick Offset for 3D`
        /// （`_OutlineWidth 0.12` / `_FaceDilate 0.12` / `_ColorMask 15`，同一份
        /// `Material_-1760077249967186717`）⇒ 走 <see cref="PragatiDigits.Variant.Thick3D"/>。
        /// 我们原来只有 armour 给了 thick（0.097，那一档是 **2D** 卡面的材质）⇒ 场上另外三个偏细。</para>
        /// <para>2D 卡面（手牌 / 详情 / 卡池）**不变**：那边只有护甲是 thick(0.097)。判据只有这一处。</para>
        /// ⚠️ 字形表三档：thin 0.05 / thick 0.097 / **thick3d 0.12**（2026-10-20 由
        /// `工具/gen_pragati_digits.py` 补烘；`Core/PragatiDigits.Data.cs` 是生成物）。</summary>
        static PragatiDigits.Variant StatVariant(int s, bool board)
        {
            if (board) return PragatiDigits.Variant.Thick3D;
            return s == StatArmour ? PragatiDigits.Variant.Thick : PragatiDigits.Variant.Thin;
        }

        /// <summary>场上数值 `s` 用的字形档标签（自检读口 —— `Editor/BattleScene.cs` 用它断
        /// 「四个数值都在 0.12 那档上」）。</summary>
        public static string BoardStatVariantTag(int s) { return PragatiDigits.Tag(StatVariant(s, true)); }

        /// <summary>场上数值的**最大墨宽**（面像素；0 = 不夹）。
        /// <para>🔴 **2026-10-20（`W58`）：场上的尺子不能再借 2D 卡面的 `StatDigitMaxW = 31`。**
        /// 原版四个 3D TMP 的 Rect 宽（× 各自 RectTransform scale）：
        /// Melee/Range `0.9 × 0.48287 = 0.4346` · Health `1.0 × 0.48287 = 0.4830` ·
        /// Armour `0.7747 × 0.48287 = 0.3741` 卡单位；我们 `FaceW=256` 时 1 px = 2.0927/256 卡单位
        /// ⇒ **53 / 59 / 45 px**（`StatCanvas` 与 `FaceW` 每像素密度相同，所以两个画布共用这组数）。
        /// 旧值 31 px 对应 0.2534 卡单位 —— 只有原版两位数额度（0.4346）的 58%、
        /// 三位数会被明显缩号（用户从原版视频里看到的是**不缩**）。</para>
        /// ⚠️ 这里按**原版 Rect 宽**取上限，不是「量截图」—— 视频截图有透视，量不出精确值。</summary>
        static int StatMaxW(int s)
        {
            switch (s)
            {
                case StatMelee:  return 53;
                case StatRanged: return 53;
                case StatArmour: return 45;
                default:         return 59;                       // Health（字号也大 8/7）
            }
        }

        /// <summary>一个数值格的独立贴图（`StatCanvas`²、数字居中）。字形表不在时返回 **null**
        /// ⇒ 调用方不拆（数值仍旧烘在 `_info` 里）。</summary>
        static Texture2D StatTexture(int s, int value)
        {
            if (!PragatiDigits.Available) return null;
            int v = Mathf.Max(0, value);
            string key = s + "|" + v;
            Texture2D cached;
            if (StatCache.TryGetValue(key, out cached) && cached != null) return cached;

            // 🔴 2026-10-20（`W58`）：画布边长按 stat 取（Health 56 = 64×7/8 ⇒ 字号 ×8/7）；
            //    场上的最大墨宽按原版 Rect（`StatMaxW`），⛔ 不再套 2D 的 31。
            //    场上四个数值原版**全是 thick 0.12**（同一份 `White_thick Offset for 3D`）。
            int canvas = StatCanvasOf(s);
            var px = Blank(canvas, canvas);
            PragatiDigits.Draw(px, canvas, canvas, v.ToString(),
                               canvas / 2, canvas / 2, StatVariant(s, true), StatMaxW(s));
            var tex = new Texture2D(canvas, canvas, TextureFormat.RGBA32, false) { name = "Stat_" + key };
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
                // ⚠️ **2026-10-25（`M2`）四个数值层【不挂】`Front`**：它们的位置取 `StatAt01(s, true)`
                //    = 原版 `3DBody` 空间的四个 TMP 节点（`BoardMeleeAt` 那一族），**不是** `Front` 局部系
                //    ⇒ 挂在 `2DCard`（`FaceRoot`），跟 3D 卡体同一套坐标。⛔ 别把它们搬进 `Front`。
                go.transform.SetParent(FaceRoot, false);  // 四个数值层也是卡面（场上的倾摆是关的，见 `SetFace`）
                go.transform.localPosition = new Vector3((at.x - 0.5f) * Width, (0.5f - at.y) * Height, BoardInfoZ);
                go.AddComponent<MeshFilter>().sharedMesh = CenteredQuad("stat" + s, StatCell, StatCell, null);
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = new Material(BaseMaterial());
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                StampQueue(mr);          // 🆕 A1665：四个数值层也是这张卡的一层（晚建 ⇒ 必须当场补号）
                _layers.Add(mr);
                _stats[s] = mr;
            }
            ApplyBoardSortingOrders();   // 🔴 2026-10-21：数值层晚建 ⇒ 当场补层号（同队列内定序）
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
                // 逐格显隐走**唯一判据** `StatShownFor`（原版逐 counter `Toggle`；**远程 = 0 照印 `0`**，
                // 只有**护甲 0** 才整格不显示 —— 判据与出处见那个函数的 doc）。
                bool show = board && StatShownFor(s, Data);
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

        // ==================================================================
        //  2D 卡面那四个数值 = 真 TMP 节点（原版 `2DCard/Front/Card Info/…`）—— 2026-10-25（`CF-3`）
        // ==================================================================
        // 🔴 **为什么要有这一套**：原版 2D 卡（手牌 / 详情窗 / 卡池）的四个数是**四件独立 TMP**，
        //    不是烘进位图；我们原来把它们烘在 `_info` 里（结构差异）。逐值判据见
        //    `_faceStatText` 那段字段注释（父链 / rect / 字号 / 颜色 / 材质 / 数值来源）。
        // ⛔ **与原版一样：这四件不参与「攻击类型高亮」**（那一档只有场上的 `Base Attack Counters` 有）。

        /// <summary>懒建那四件 TMP（第一次用到时）。拿不到字体资产就**不建** ——
        /// 那时 `SplitStats` 返回 false，数字仍旧烘在 `_info` 里（两条路互斥、由 `SplitStats` 一处判）。</summary>
        void BuildFaceStatTexts()
        {
            if (_faceStatText[StatMelee] != null) return;
            if (string.IsNullOrEmpty(Data.id) || !Data.isUnit) return;
            if (!TmpFont.Available) return;

            for (int s = StatMelee; s <= StatHealth; s++)
            {
                // 名字与原版 prefab 逐字一致（自检 / 排查时能直接对上）
                string cname = s == StatMelee ? "Melee Attack Container"
                             : s == StatRanged ? "Range Attack Container"
                             : s == StatHealth ? "Health Container"
                             : "Armour Container";
                string tname = s == StatMelee ? "Melee Attack Text"
                             : s == StatRanged ? "Range Attack Text"
                             : s == StatHealth ? "HealthText"
                             : "Armour Text";

                var cgo = new GameObject(cname);
                cgo.transform.SetParent(FaceContentRoot, false);
                cgo.transform.localScale = Vector3.one;      // 原版四个 Container 的 `m_LocalScale` 全是 1
                var at = StatAt01(s, false, _frameTex);
                // z 跟 `_info` 同层：原版这两样都挂在 `Card Info` 里（同一层），
                // 且必须是「卡框之前、正文文字之后」（`InfoZ = −0.02` < `TextZ = −0.03` 那个次序）。
                cgo.transform.localPosition = new Vector3((at.x - 0.5f) * Width,
                                                          (0.5f - at.y) * Height, InfoZ);
                _faceStatContainer[s] = cgo;

                var t = TmpFont.NewText(cgo.transform, tname, "", FaceStatFontSizeOur, Color.white,
                                        TmpFont.Face.LatinCardBody);
                if (t == null) { DestroyImmediate(cgo); _faceStatContainer[s] = null; return; }
                var rt = t.rectTransform;
                var size = s == StatHealth ? FaceStatTextSizeHealth
                         : s == StatArmour ? FaceStatTextSizeArmour
                         : FaceStatTextSize;
                rt.sizeDelta = size;
                // 原版 `Armour Text` 在容器里带一个很小的偏移（其余三个是 (0,0)）
                rt.localPosition = s == StatArmour
                    ? new Vector3(FaceStatArmourTextOffset.x, FaceStatArmourTextOffset.y, 0f)
                    : Vector3.zero;
                rt.localScale = Vector3.one;                 // 原版四件 `m_LocalScale` 全是 1（不是 0.01 那一档）
                // 原版 `m_enableAutoSizing = 1`、`fontSizeMin/Max = 0.025 / 0.34`（**原版 em 口径**）
                // ⇒ 两个界都要过 `FontSizeForEm` 折成我们的 fontSize（见 `FaceStatFontSizeOur` 的 doc）
                t.enableAutoSizing = true;
                t.fontSizeMin = TmpFont.FontSizeForEm(TmpFont.Face.LatinCardBody, FaceStatFontSizeMin);
                t.fontSizeMax = FaceStatFontSizeOur;
                t.fontSize = FaceStatFontSizeOur;
                t.alignment = TextAlignmentOptions.Center;   // 原版 hAlign 2 (Center) + vAlign 512 (Middle)
                ApplyStatOutline(t, s == StatArmour);        // 护甲那一件是 Thick，其余 Thin
                cgo.SetActive(false);                        // 由 `SyncFaceStatTexts` 按形态/数值开关
                StampTmpQueue(t);                            // A1665：TMP 也是这张卡的一层
                _faceStatText[s] = t;
                _faceStatRect[s] = rt;
            }
        }

        /// <summary>把四件 TMP 的**值 / 显隐**刷一遍（`_info` 那半已由 `SplitStats` 停掉）。
        /// 判据：数值走 `StatValue(Data, s)`（与场上那四层同一个口）；**护甲 ≤ 0 不显示**
        /// （`BasicCardUI.SetRawCardData` 的 `Toggle(armour, bVar6)` 那一支；口径与 `SyncStatLayers` 同一份）。</summary>
        void SyncFaceStatTexts()
        {
            if (_faceStatText[StatMelee] == null) return;
            bool face = _faceMode != CardFace.Board;         // 场上走四个网格层（`_stats`）
            for (int s = StatMelee; s <= StatHealth; s++)
            {
                var t = _faceStatText[s];
                var cgo = _faceStatContainer[s];
                if (t == null || cgo == null) continue;
                int v = StatValue(Data, s);
                // 显隐走**唯一判据** `StatShownFor`（与场上那四个网格层同一份：原版逐 counter `Toggle`，
                // **远程 = 0 照印**、只有**护甲 0** 不显示）。⚠️ 简化视图 / `SmallScreenUI` **不参与**这一条 ——
                // 原版那四个 `CardTextCounterContainer.activationRules` 实测**全是 `[0] = Normal`**
                // （只有 `Cost Container` 是 `[0,15]`、`Cost Container Big` 是 `[5,10]`）。
                bool on = face && StatShownFor(s, Data);
                bool wasOn = cgo.activeSelf;
                if (wasOn != on) cgo.SetActive(on);
                if (!on) continue;
                string txt = v.ToString();
                bool changed = t.text != txt;
                if (changed) t.text = txt;
                // ⚠️ 批处理没有帧循环（CLAUDE.md §三）⇒ 值变了 / 刚显示出来的那一拍必须手动排版
                //    （`ForceMeshUpdate` 对**未激活**对象会早退 —— 所以这句只能写在 `SetActive(true)` 之后）
                if (changed || !wasOn) t.ForceMeshUpdate();
                // 🆕 2026-10-25（`K4`）：**涨落色 / 持续色**（原版 `DoColorChange` 涂的就是这个 TMP 的顶点色）。
                //   ⚠️ 不能只靠 `ApplyTint` 那一趟 —— `SetData` 这条路当下不一定调它（数值刷了色却没跟上 = 静默）。
                t.color = FaceStatInk(s);
            }
        }

        /// <summary>2D 卡面那四件数值 TMP 的**顶点色**（原版 `m_fontColor` 白 × `DoColorChange` 的涂色）。
        /// <para>TMP 的字不吃材质 `_Color`（走顶点色）⇒ 整卡状态色 / 淡出 / 灰化 / 涨落色都在这一处乘齐
        /// —— 与 `_title` / `_keywords` / `_createdBy` 那几族同构（见 `ApplyTint` 末尾）。判据只此一处。</para></summary>
        Color FaceStatInk(int s)
        {
            var c = new Color(_tint.r, _tint.g, _tint.b, _tint.a * _alpha);
            return Greyed(Color.white * _statFlash[s] * c);
        }

        // ==================================================================
        //  🆕 2026-10-25（`E` · K4 §②·D3）：**费用数字 = 第五件真 TMP**（原版 `Cost Container/CostText`）
        // ==================================================================
        //  为什么要有这一段：费用原来是**烘在 `_info` 贴图里**的（`InfoTexture` 的 `ShowsCost` 那一支），
        //  而原版是**第五个 `CardTextCountersController`** 驱动的真 TMP ⇒ 它的两件事我们一样都做不了：
        //    ① `DoColorChange`（费用比基准低/高时涂绿/红 —— `statIncreaseIsPositiveToThePlayer = 0`）；
        //    ② `DOPunchScale`（数值变化那一下的弹跳）。
        //  判据（prefab 逐字段实读）见上面 `_faceCostText` 那一段注释；两档（普通 / 大号）是**两个节点**。
        //  ⚠️ 烘图那边要**成对**关掉（`InfoTexture` 的 `TmpFont.Available` 闸）—— 否则会重影。
        //  ⚠️ 只给**非场上**档建（场上那张是 3D 卡体，费用格本来就不显示：`Show(_costBg, !board && …)`）。

        /// <summary>建费用 TMP（幂等）。条件：字体资产在 + 这张卡**该印费用**（`ShowsCost`）+ 非场上。</summary>
        void BuildFaceCostText()
        {
            if (_faceCostText != null) return;
            if (!TmpFont.Available) return;
            if (!ShowsCost(Data)) return;
            if (_faceMode == CardFace.Board) return;
            var cgo = new GameObject("Cost Container");            // 原版节点名（排查/自检能对上）
            cgo.transform.SetParent(FaceContentRoot, false);
            cgo.transform.localScale = Vector3.one;
            var at = CostAt01(_frameTex);
            // z 与 `_info` / 四件数值同层（原版这些都在 `Card Info` 里；见 `InfoZ` 的注释）
            cgo.transform.localPosition = new Vector3((at.x - 0.5f) * Width,
                                                      (0.5f - at.y) * Height, InfoZ);
            _faceCostContainer = cgo;

            float em = CostBig ? CostTextBigEm : CostTextEm;
            var t = TmpFont.NewText(cgo.transform, "CostText", "",
                                    TmpFont.FontSizeForEm(TmpFont.Face.LatinCardBody, em),
                                    Color.white, TmpFont.Face.LatinCardBody);
            if (t == null)
            {
                Debug.LogWarning("[CardView] 费用 TMP 建不出来（`TmpFont.NewText` 回 null）⇒ 这一张退回烘图"
                               + "（`InfoTexture` 那条 `TmpFont.Available` 闸会放行），颜色/弹跳仍然没有");
                DestroyImmediate(cgo);
                _faceCostContainer = null;
                return;
            }
            var rt = t.rectTransform;
            rt.sizeDelta = CostBig ? CostTextSizeBig : CostTextSize;
            // 原版这一颗挂在**被 scale 1.2 的容器**下 ⇒ 我们平铺在这一级，偏移要手动乘 `CostScale`
            rt.localPosition = new Vector3(CostTextDx * CostScale, CostTextDy * CostScale, 0f);
            rt.localScale = Vector3.one;
            if (!CostBig)
            {
                // 普通档：autoSizing 1（min 0.025 / max 0.34）、Normal、白、Center/Middle、不折行
                t.enableAutoSizing = true;
                t.fontSizeMin = TmpFont.FontSizeForEm(TmpFont.Face.LatinCardBody, CostTextAutoMinEm);
                t.fontSizeMax = TmpFont.FontSizeForEm(TmpFont.Face.LatinCardBody, CostTextEm);
                t.fontSize = t.fontSizeMax;
            }
            else
            {
                // 大号档（另一颗节点）：**autoSizing 关**、字号固定 0.475
                t.enableAutoSizing = false;
                t.fontSize = TmpFont.FontSizeForEm(TmpFont.Face.LatinCardBody, CostTextBigEm);
            }
            t.alignment = TextAlignmentOptions.Center;             // 原版 hAlign 2 + vAlign 512
            t.textWrappingMode = TextWrappingModes.NoWrap;         // 原版 `m_TextWrappingMode = 0`
            ApplyStatOutline(t, thick: false);                     // 原版材质 `<Patern> Thin Outline`
            cgo.SetActive(false);                                  // 由 `SyncFaceCostText` 开关
            StampTmpQueue(t);
            _faceCostText = t;
            _faceCostRect = rt;
        }

        /// <summary>刷费用 TMP 的**值 / 显隐 / 颜色**（与 `SyncFaceStatTexts` 同一套口径：
        /// 显隐走 `ShowsCost` + 形态；颜色走 `FaceStatInk(StatCost)`（状态色 × 淡出 × 灰化 × 涨落色））。</summary>
        void SyncFaceCostText()
        {
            if (_faceCostText == null) return;
            bool on = _faceMode != CardFace.Board && ShowsCost(Data);
            var cgo = _faceCostContainer;
            bool wasOn = cgo != null && cgo.activeSelf;
            if (cgo != null && wasOn != on) cgo.SetActive(on);
            if (!on) return;
            string txt = Data.cost.ToString();
            bool changed = _faceCostText.text != txt;
            if (changed) _faceCostText.text = txt;
            if (changed || !wasOn) _faceCostText.ForceMeshUpdate(); // 批处理没有帧循环（同四件数值那条）
            _faceCostText.color = FaceStatInk(StatCost);
        }

        /// <summary>🆕 **2026-10-25（`E` · 原版 `Card2DController.ToggleTooltips` 的逐卡那一半）**：
        /// **这张卡上的 tooltip 开不开**（默认开）。原版是逐卡把卡上那批 `EverguildTooltipTrigger` /
        /// `TextTooltipController` 一起 `Toggle(option)`：
        /// · `CardDisplayWindow` 摆卡时 **只有前台（index 0）开**、相关卡关（`CardDisplayWindow.cs:965`）；
        /// · 换位时 **旧前台关、新前台开**（`:1181/1184`）；
        /// · 新建的战斗卡先关、摆好后开（`BattleCardManager:128` 等）。
        /// ⚠️ **消费侧**（我们的悬停驱动 = `Battle/Tooltip` 的调用点）必须读这一格：
        ///   `Battle/BattleDriver` 的 `TickKeywordTooltip`/`TickTooltipAt` 在弹之前先判 `v.TooltipsEnabled`
        ///   （那一处在 BattleDriver = A 域，本批只落**开关本身 + 设置点**，见报告）。
        /// ⚠️ 与 `Core/Tooltip` 的**全局抑制计数**分工：那条服务「窗口/开包期间整片不要 tooltip」；
        ///   本格服务「同一棵树里只让前台那张弹」—— 两条可同时用。⛔ 别把两者合并成一个 bool。</summary>
        public bool TooltipsEnabled = true;

        /// <summary>自检用：费用那件 TMP（没建 = null）。</summary>
        public TextMeshPro FaceCostText { get { return _faceCostText; } }
        /// <summary>自检用：费用那一格现在显示着吗（= `ShowsCost` 且非场上）。</summary>
        public bool FaceCostVisible { get { return _faceCostContainer != null && _faceCostContainer.activeSelf; } }
        /// <summary>自检用：费用 TMP 的 rect（`sizeDelta` 对原版 0.38×0.35 / 大号 0.456×0.42）。</summary>
        public RectTransform FaceCostRect { get { return _faceCostRect; } }

        /// <summary>原版两档描边材质（`Pragati-Regular Atlas Material {Thin,Thick} Outline`）——
        /// `_OutlineWidth` / `_FaceDilate` / `_OutlineColor` 逐字段实读，写在 `StatOutline*` 那几个常量上。
        /// ⚠️ 必须用**实例材质**（`fontMaterial`），改共享材质会让全工程的字一起变（同 `TmpFont.ApplyOutline`）。</summary>
        void ApplyStatOutline(TextMeshPro t, bool thick)
        {
            var mat = t.fontMaterial;
            if (mat == null) return;
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor(ShaderUtilities.ID_OutlineColor,
                         thick ? StatOutlineColorThick : StatOutlineColorThin);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, thick ? StatOutlineThick : StatOutlineThin);
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, thick ? StatFaceDilateThick : StatFaceDilateThin);
            t.UpdateMeshPadding();   // 描边会画到 quad 之外 ⇒ 重算留白（同 `TmpFont.ApplyOutline`）
        }

        // ---- 自检读口（这四个数是 TMP，读 `_info` 那张贴图的像素是读不到「哪件画了什么」的）----
        /// <summary>自检用：第 `s` 格 2D 数值的 TMP（没建 = null）。</summary>
        public TextMeshPro FaceStatText(int s) { return _faceStatText[s]; }
        /// <summary>自检用：那件 TMP 自己的 rect（`sizeDelta` / `localScale` 都对原版用）。</summary>
        public RectTransform FaceStatRect(int s) { return _faceStatRect[s]; }
        /// <summary>自检用：那个 Container 现在显示着吗（= 这一格该不该画）。</summary>
        public bool FaceStatVisible(int s)
        {
            var c = _faceStatContainer[s];
            return c != null && c.activeSelf;
        }
        /// <summary>自检用：Container 在 `Front` 局部系的坐标（与原版 `Card Info` 的 `m_AnchoredPosition` 同标架）。</summary>
        public Vector3 FaceStatLocalPos(int s)
        {
            var c = _faceStatContainer[s];
            return c != null ? c.transform.localPosition : Vector3.zero;
        }

        /// <summary>那一个数值**变好 / 变坏**了：涂色 + bump（原版 `CardTextCountersController` 那两件事）。
        /// <para>⚠️ **本函数只服务「基准未知」那条近似路**（出厂）：判据是「新 vs 旧」（`up = 新 > 旧`）。
        /// 基准已知时颜色走 <see cref="StatColorVsBase"/> 的**持续态**、这里只剩 bump ——
        /// 两条路都在 <see cref="CaptureStatChanges"/> 里分叉，⛔ 别在别处再判一次极性。</para></summary>
        void FlashStat(int s, bool up)
        {
            _statFlash[s] = up ? CardFeel.StatUpColor : CardFeel.StatDownColor;
            // ⚠️ **这里不刷色** —— `SetData` 末了那次 `SyncStatLayers` / `SyncFaceStatTexts` 才画上新值，
            //    此刻 `Data` 还是旧的（见 `CaptureStatChanges` 的注释）。
            BumpStat(s);
        }

        /// <summary>原版 9 参 `SetText` 里 `doAnimation` 那一支 —— **对每个 `textsToSet` 都做一次**。
        /// <para>参数是从指令流里读出来的（VA `0x18060C180` 的 `0x18060C5DB…0x18060C6B5` 那一段）：
        /// · 先把 `localScale` **复位**到基准（`set_localScale`，不是 `DOScale`）；
        /// · 再 `DOPunchScale(基准 × cardTextBumpSize, cardTextBumpTime, vibrato = 10, elasticity = 2.0f)`
        ///   —— **幅度是「乘」不是「打到」**（三处 `mulss`），弹性那个 `2.0f` 是常量池 `0x1834B2BBC`。</para>
        /// <para>🔴 **2026-10-25（`K4`）：两套载体都要动** —— 原版每个数值 counter 的 `textsToSet` 有 **2 件**：
        /// 2D 卡面那件（`m_fontSize 0.34`、无 `highlightAttackType`）+ 3D 卡体那件（`m_fontSize 7 / 8`、
        /// 带 `highlightAttackType`）。实测（`bundle_battleprefabs_vfxandmisc_assets_all`；按每份 counter 的
        /// `textsToSet` 指向的 TMP 节点名反查：`Range Attack Text` → `…_-3358613892933444672`、
        /// `HealthText` → `…_1339511855435914176`、`Armour Text` → `…_576116891963464640`，
        /// 剩下那份 `…_4301255605536791488` = **近战**）⇒ 四个数值各 **2 件**；
        /// **费用 `…_6320885271709195200` 只有 1 件**（3D 卡体不印费用）。
        /// 我们原来只 bump 场上那四个网格层 ⇒ **手牌 / 卡池的数值涨落是「无声」的**。</para>
        /// <para>基准 scale：场上那层走 `StatBaseScaleOf`（= 原版 `originalSize` 惰性捕获）+ 高亮倍数；
        /// 2D 那四件原版 `originalScales[i]` 恒 **1**（`CardTextCountersController.CacheData` 记的就是 1）。</para></summary>
        void BumpStat(int s)
        {
            // 🆕 2026-10-25（K4 D3）：**费用 = 第五个 counter**，它的载体是 `_faceCostText`（不是 `_stats[5]`）。
            if (s == StatCost)
            {
                if (_faceCostText == null) return;
                var trc = _faceCostText.transform;
                trc.DOKill();
                trc.localScale = Vector3.one;                // 原版复位到 `originalScales[i]` = 1
                CardTween.Use(trc.DOPunchScale(Vector3.one * CardFeel.StatBumpSize, CardFeel.StatBumpTime,
                                               CardFeel.StatBumpVibrato, CardFeel.StatBumpElasticity),
                              Ease.Linear, trc);
                StatBumps++;
                return;
            }
            // 0 = 空档；1..4 = 四个数值（场上走网格层、非场上走 `_faceStatText`）
            if (s <= 0 || s >= _stats.Length) return;
            var mr = _stats[s];
            if (mr != null)
            {
                // 场上那四个网格层（= 原版 3D 卡体那一件）
                var tr0 = mr.transform;
                tr0.DOKill();
                _statScaleTween[s] = null;                   // `DOKill` 刚才把那条高亮补间一起杀了
                bool lit = (s == StatMelee && OwnAttackType == AttackTypes.Melee)
                        || (s == StatRanged && OwnAttackType == AttackTypes.Ranged);
                var bumpBase = StatBaseScaleOf(s) * (lit ? AttackScaleModifier : 1f);
                tr0.localScale = bumpBase;                   // 原版先复位到基准
                CardTween.Use(tr0.DOPunchScale(bumpBase * CardFeel.StatBumpSize, CardFeel.StatBumpTime,
                                               CardFeel.StatBumpVibrato, CardFeel.StatBumpElasticity),
                              Ease.Linear, tr0);
                StatBumps++;
                return;
            }
            // 2D 卡面那四件真 TMP（`CF-3` 起）—— 原版 2D 那一件也在 `textsToSet` 里
            var t = _faceStatText[s];
            if (t == null) return;
            var tr = t.transform;
            tr.DOKill();
            tr.localScale = Vector3.one;                     // 原版复位到 `originalScales[i]` = 1
            CardTween.Use(tr.DOPunchScale(Vector3.one * CardFeel.StatBumpSize, CardFeel.StatBumpTime,
                                          CardFeel.StatBumpVibrato, CardFeel.StatBumpElasticity),
                          Ease.Linear, tr);
            StatBumps++;
        }

        /// <summary>自检用：bump 触发过几次（截图看不出「那一下有没有播」）。</summary>
        public int StatBumps { get; private set; }

        /// <summary>自检用：第 `s` 个数值现在是什么色（`CardFeel.StatUpColor` / `StatDownColor` / 白）。</summary>
        public Color StatFlash(int s) { return _statFlash[s]; }

        /// <summary>自检用：**场上那一层数值的材质色**（层没建 / 没材质 ⇒ `Color.clear`）。
        /// 断「2D / 3D 两套载体吃同一套涨落色」用它 —— 2D 那套走 `FaceStatText(s).color`（顶点色），
        /// 3D 那套走这里（`_stats[s]` 的材质 `_Color`）。</summary>
        public Color StatLayerColor(int s)
        {
            var mr = _stats[s];
            return (mr != null && mr.sharedMaterial != null) ? mr.sharedMaterial.color : Color.clear;
        }

        /// <summary>自检用：第 `s` 个数值现在画着哪个数（没画返回 -1）。
        /// <para>🔴 **这一条读的是 `activeSelf`，读不到「屏幕上有没有」** —— 2026-10-12 用户真机报的
        /// 「场上的战将/部队没有近战·远程·血量数值」里，同一次确定性渲染场卡只画出了一个数，
        /// 而它一路全绿。⇒ 它管**行为**（该显示的格是不是活的、画的哪个数），
        /// 「真画在屏幕上没有」由 `Editor/BattleScene.cs` 里那条**渲图数像素**的断言盯（两条都要留）。</para></summary>
        public int StatShown(int s)
        {
            var mr = _stats[s];
            return (mr != null && mr.gameObject.activeSelf) ? StatValue(Data, s) : -1;
        }

        /// <summary>自检用：第 `s` 个数值层的**世界坐标**（测「画在屏幕上没有」时要把它投到屏幕）。
        /// 层还没建时退回「按形态算出来的那个位置」（这样断言照样能跑，不会因为没建而静默跳过）。</summary>
        public Vector3 StatLayerWorldPos(int s)
        {
            var mr = _stats[s];
            if (mr != null) return mr.transform.position;
            var at = StatAt01(s, _faceMode == CardFace.Board, _frameTex);
            bool boardFallback = _faceMode == CardFace.Board;
            return transform.TransformPoint(new Vector3((at.x - 0.5f) * Width,
                                                        (0.5f - at.y) * Height + (boardFallback ? 0f : FrontAposY),
                                                        boardFallback ? BoardInfoZ : InfoZ));
        }

        /// <summary>自检用：第 `s` 个数值层的 z（**卡根空间**；层没建就用该形态该用的常量）。
        /// 断「数值在徽标/卡体正面**之前**」用它。</summary>
        public float StatLayerZ(int s)
        {
            var mr = _stats[s];
            if (mr != null) return mr.transform.localPosition.z;
            return _faceMode == CardFace.Board ? BoardInfoZ : InfoZ;
        }

        /// <summary>自检用：合法目标底光那两层**在卡根空间的 z**。断「底光画在数值后面」用它
        /// （同队列时靠 z 定序，见 `MakeGlow` 的注释）。</summary>
        public float TargetGlowZ
        {
            get
            {
                var q = _glowMelee != null ? _glowMelee : _glowRanged;
                return q != null ? q.transform.localPosition.z : 0f;
            }
        }

        /// <summary>自检用：底光那两层的**渲染队列号**（还没建出来返回 −1）。
        /// 断「底光也在本卡那一段队列里」用它 —— 它原来是唯一没有盖号的一层（固定 3000）。</summary>
        public int TargetGlowQueue
        {
            get
            {
                var q = _glowMelee != null ? _glowMelee : _glowRanged;
                return q != null ? q.RenderQueue : -1;
            }
        }

        /// <summary>四个数值的闪现色全部刷回白（原版那条「`old == new` 就回原色」）。</summary>
        void ClearStatFlashes()
        {
            for (int s = StatMelee; s <= StatHealth; s++) _statFlash[s] = Color.white;
            _statFlash[StatCost] = Color.white;          // 费用（原版第五个 counter；见 `_statFlash` 的注释）
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
        /// 描边在材质里：生命/近战/远程 `Thin` `_OutlineWidth=0.05`，护甲/费用 `Thick` 0.097）。
        /// ⇒ 所以这里的 `thick` 不是我们挑的，是照原版材质分的。
        /// 🔴 **2026-10-20（`W58`）：上面那两档是【2D 卡面】的材质。场上四个 3D 数值原版共用
        /// `Pragati-Regular White_thick Offset for 3D`（`_OutlineWidth 0.12`）⇒ 场上四个都走
        /// <see cref="PragatiDigits.Variant.Thick3D"/>（字形表里烘好的 0.12 一档），
        /// 判据见 <see cref="StatVariant"/>。**
        /// 字形表 = `Core/PragatiDigits.Data.cs`（生成物，从原版 SDF 图集抠的）。
        /// </summary>
        static void DrawStatAt(Color32[] px, Vector2 at, int value, PragatiDigits.Variant v, int maxW = 0)
        {
            if (!PragatiDigits.Available)
            {
                // 拿不到字形表就退回点阵（**不静默不画**）
                DrawCenteredAt(px, at, Mathf.Max(0, value).ToString(), 4, InkHealth);
                return;
            }
            PragatiDigits.Draw(px, FaceW, FaceH, Mathf.Max(0, value).ToString(),
                               Mathf.RoundToInt(at.x * FaceW), Mathf.RoundToInt(at.y * FaceH), v,
                               maxW > 0 ? maxW : StatDigitMaxW);
        }

        /// <summary>**2D 卡面**数值格里的数字最多多宽（面像素）—— 超过就整体缩号（见 `PragatiDigits.Draw` 的 `maxW`）。
        ///
        /// 来历：数值圆是**烘在卡框图里**的，代码里没有半径常量。2026-09-16 量出来的算法是：
        /// 子代理在 700 px 宽的渲染图上量紫圈内沿为 x 113→203 = **90 px** ⇒ 占卡宽 12.86%
        /// ⇒ 折算到我们 `FaceW = 256` ⇒ **32.9 px**，两边各留 ~1 px 边距 ⇒ **31**。
        /// 一位数总宽 ≈ 18 面像素，不受影响；两位数（`Measure("10")` = 36.6）缩到 0.85 倍后进得去。
        /// ⚠️ 原版的两位数**本来就比一位数小**（同一张 `EC41` 卡图上，原版紫圈里的 `10` 四周都有余量），
        /// 所以「按位数缩号」不是我们的将就，是照原版。
        /// 🔴 **2026-10-20（`W58`）：这个 31 只属于【2D 卡面】。场上那四个 3D TMP 的尺子是
        /// <see cref="StatMaxW"/>（53/59/45 px，原版 Rect 宽）—— 2D 的尺子套到 3D 卡体会把
        /// 两/三位数夹小 25–40%。</summary>
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

        // ==================================================================
        //  第三档命中：原版 `CardCollider2D.colliderShowInBig`（悬停放大档）—— 2026-10-25（R16）
        // ==================================================================
        // 🔴 **为什么追加在类尾**：本文件被几本账按 `<文件>:<行号>` 引用（`CardView.cs` 共 159 处），
        //    往中间插成员会让**下游全部行号平移**（`工具/check_ledger_points.py` 报「漂移」，别的代理的账也跟着脏）
        //    ⇒ **新成员一律追加在类尾**；就地改动保持**行数不变**（只替换同一行）。
        //
        // 判据（一手，四条互证）：
        //  ① 预制体**两颗 `BoxCollider2D` 挂在同一个 GO（`2DCard`）** —
        //     `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/BoxCollider2D/`
        //     · `BoxCollider2D_7520353667969031104.json` = 主面 `m_Size 1.97 × 3.02` · `m_Offset (0,0)`
        //     · `BoxCollider2D_2582590304383179712.json` = **第二面 `m_Size 1.25 × 2.53` · `m_Offset (0,−2.62)`**
        //       （矩形 y ∈ [−3.885, −1.355]：**贴在卡心下方**，只与主面下沿重叠 0.155）
        //     两颗都 `m_IsTrigger: false`，`m_GameObject` 同为 `9095217088215161792`（= `GameObject/2DCard.json`）。
        //  ② 字段归属：`CardCollider2D.colliderShowInBig`（`BoxCollider2D`）· `colliderHeightMultiplierInHand`；
        //     `CardCollider2D__.ctor.c` 只把 `+0x50` 写成 `0x3f99999a`（= **1.2f** 的 ctor 默认），
        //     而**跑游戏的是 prefab 序列化值 1.01**（本文件 `HitHalfHInHand` 已按 1.01 落地）。
        //  ③ 行为（`d:/2/tools/decomp_full/CardCollider2D__{Initialize,SetCollidersShowBig,Toggle,ToggleShowInBigCollider}.c`
        //     ＋ `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CardCollider2D.cs`）逐支：
        //     · `Initialize()`            ⇒ 抓 originalSize/offset → **`SetCollidersShowBig(false)`**（出厂 = 第二面关）
        //     · `SetCollidersShowBig(on)` ⇒ `big.enabled = on`；
        //         `on`  ⇒ `Toggle(true)` + `ResetCollider()`（主面回**常态**矩形）
        //         `!on` ⇒ 主面写成 **in-hand 档**（`size.y ×= 1.01`、`offset.y −= Δ/2`）+ `Toggle(true)`
        //     · `Toggle(false)`           ⇒ 主面关，**并且把第二面也关**（`CardCollider2D__Toggle.c:6-9`）
        //     · `ToggleShowInBigCollider` ⇒ **只**写 `big.enabled`（原版 `private`、**全库 0 调用点** = 死代码）
        //     ⚠️ 两个方法的 `int indexInHand` 形参在**机器码里完全没读**（`__SetCollidersShowBig.c` 的签名
        //        只有 `(this, option)`；`__ToggleShowInBigCollider.c` 同）⇒ 我们**不保留这个死形参**。
        //  ④ 调用面：`CardScript.StartShowingCardInHand` ⇒ `SetCollidersShowBig(true, PositionInHand)`；
        //     `CardScript.StopShowingCardInHand` ⇒ `SetCollidersShowBig(false)`（`CardScript.cs:10595` / `:11030`）。
        //     「showing in hand」= 手牌里那张**抬起 + 放大**的态；我们的等价物 = `HandLayout.hoveredIndex`
        //     （`hoverLift` / `hoverScale 1.3` → `HandLayout.cs:265-268`、`:796-800`，2026-10-23 `F6` 已认过这条对应）。
        //     🔴 **接线点 = `Hand/CardInteraction`（`_hovered` 变迁处）或 `HandLayout.Refresh`** ——
        //     这两个文件**不在 R16 白名单** ⇒ 本批只落 **Core 侧 API + 几何 + 命中并集**，
        //     接线指令写在 `资料/完全复刻/报告/R16_卡牌面遗留C_第2轮.md`。
        //
        // ⚠️ 与 `Contains` 的关系：`Contains`（手算）与 `HitByCollider`（物理）必须仍然等价（`W57` 那条
        //    「两份独立来源」的约定）⇒ 两边都取**并集**：`Contains` 加 `BigRectContains`，
        //    `HitByCollider` 把 `_hitColliderBig` 也当成「本卡那一颗」。

        /// <summary>原版第二颗 collider 的**半宽** = `m_Size.x 1.25 ÷ 2` = **0.625**。</summary>
        public const float HitBigHalfW = 1.25f * 0.5f;
        /// <summary>第二颗 collider 的**半高** = `m_Size.y 2.53 ÷ 2` = **1.265**。</summary>
        public const float HitBigHalfH = 2.53f * 0.5f;
        /// <summary>第二颗 collider 的**中心 y** = `m_Offset.y` = **−2.62**（整块在卡心**下方**）。
        /// ⚠️ 这个矩形**不是**「放大的卡」的形状 —— 它是「卡被抬走后指针留在原处」那一段的接住区，
        /// ⛔ 别拿 `hoverScale(1.3)` 或卡面尺寸去推它。</summary>
        public const float HitBigCenterDy = -2.62f;

        /// <summary>原版第二颗 `BoxCollider2D`（`colliderShowInBig`）。`EnsureHitCollider` 里建（幂等）。</summary>
        BoxCollider2D _hitColliderBig;

        /// <summary>「主面用常态矩形」那一档 = 原版 `SetCollidersShowBig(true)` 之后（内部调了 `ResetCollider`）。
        /// ⚠️ 它**只管主面几何**；第二面开没开是另一件事（见 <see cref="ShowInBigColliderEnabled"/>）——
        /// 原版也只有 `SetCollidersShowBig` 会 `ResetCollider`，`ToggleShowInBigCollider` 不动几何。</summary>
        bool _inHandBigGeometry;

        /// <summary>collider 还没建时先记在这里的逻辑态（`Build` 之后由 `EnsureBigHitCollider` 落进真组件）。</summary>
        bool _bigEnabled;

        static bool _hitColliderBigWarned;

        /// <summary>第二颗 collider 现在**开没开** —— 物理世界的事实（`Contains` 的并集判据读它）。
        /// <para>没建起来时退回逻辑态 <see cref="_bigEnabled"/>：这样「`Build` 之前设状态」不会丢（⛔ 不静默丢）。</para></summary>
        public bool ShowInBigColliderEnabled
        {
            get { return _hitColliderBig != null ? _hitColliderBig.enabled : _bigEnabled; }
            private set
            {
                _bigEnabled = value;
                if (_hitColliderBig != null) _hitColliderBig.enabled = value;
                else if (value && !_hitColliderBigWarned)
                {
                    _hitColliderBigWarned = true;
                    Debug.LogWarning("[CardPresentation] " + name + "：第二颗 collider（`colliderShowInBig`）还没建就"
                                   + "被要求打开 ⇒ 状态先记着，等 `Build()` 时落进真组件（唯一成因 = `Build()` 没跑过）");
                }
            }
        }

        /// <summary>建第二颗 `BoxCollider2D`（原版 `colliderShowInBig`；**幂等**，`Build` 重入安全）。
        /// <para>出厂 **`enabled = false`** —— 对应原版 `Initialize()` 末尾那句 `SetCollidersShowBig(false)`
        /// （`CardCollider2D__Initialize.c:20-24`）。</para>
        /// <para>⚠️ 两颗 collider 在**同一个 GameObject**（原版 `2DCard`）上 —— `AddComponent` 第二颗是合法的，
        /// Unity 允许同一 GO 挂多颗 `Collider2D`；`Physics2D.OverlapPoint` 会**逐颗**返回它们，
        /// 所以 `HitByCollider` 必须两颗都比（已改）。</para></summary>
        void EnsureBigHitCollider(Transform frame)
        {
            var cols = frame.GetComponents<BoxCollider2D>();
            BoxCollider2D big = null;
            for (int i = 0; i < cols.Length; i++)
                if (cols[i] != _hitCollider) { big = cols[i]; break; }   // 排除主面那一颗
            if (big == null) big = frame.gameObject.AddComponent<BoxCollider2D>();
            big.isTrigger = false;                                       // 原版 `m_IsTrigger: false`
            big.size = new Vector2(HitBigHalfW * 2f, HitBigHalfH * 2f);
            big.offset = new Vector2(0f, HitBigCenterDy);
            big.enabled = _bigEnabled;                                   // 幂等：重入时跟着逻辑态走（出厂 false）
            _hitColliderBig = big;
        }

        /// <summary>原版 `CardCollider2D.SetCollidersShowBig(bool)` 的等价物 —— **手牌「亮出来」档的总开关**。
        /// <para>行数顺序照 `__SetCollidersShowBig.c` 逐句：① `big.enabled = option` ② `Toggle(true)`
        /// ③ 几何（`on` ⇒ 常态矩形 / `!on` ⇒ in-hand 档）。</para>
        /// <para>⚠️ 与 `SetColliderInHand`/`SetColliderNotInHand` 的分工：那两条按**卡状态**给几何，
        /// 我们这边几何归 `HitRect()`（按 `CardFace`），所以 `!on` 那一支**落到 `_faceMode == Hand`
        /// 时自然就是 in-hand 档**；若调用方不在手牌档，得到的是常态矩形 ——
        /// 原版那一支会无条件写 in-hand 矩形（**已知的极窄偏离**：原版调用点只有
        /// `Start/StopShowingCardInHand` 两条，都在手牌档内）。</para></summary>
        public void SetCollidersShowBig(bool on)
        {
            ShowInBigColliderEnabled = on;      // ①
            _inHandBigGeometry = on;            // ③ 的前置：on ⇒ `ResetCollider` 那一档
            SetHitEnabled(true);                // ② 原版两支都 `Toggle(true)`
            SyncHitCollider();                  // ③ 把几何写进真 collider
        }

        /// <summary>原版 `CardCollider2D.ToggleShowInBigCollider(bool)`：**只**写 `colliderShowInBig.enabled`。
        /// <para>⚠️ 原版是 `private` 且**全库 0 调用点**（死代码）—— 我们公开它只为 1:1 对账 + 断言；
        /// ⛔ 生产路径应当走 <see cref="SetCollidersShowBig"/>（那条会连带主面几何）。</para></summary>
        public void ToggleShowInBigCollider(bool on)
        {
            ShowInBigColliderEnabled = on;
        }

        /// <summary>`Contains` 的**并集**那一半：点落在第二颗 collider 的矩形里吗（参数是**卡局部**坐标）。
        /// <para>只在第二面**开着**时才算 —— 两边（手算 / 物理）读的是同一个开关（<see cref="ShowInBigColliderEnabled"/>），
        /// 所以 `Contains` ≡ `HitByCollider` 在「第二面开 / 关」两档都成立。</para></summary>
        bool BigRectContains(Vector3 local)
        {
            if (!ShowInBigColliderEnabled) return false;
            return Mathf.Abs(local.x) <= HitBigHalfW
                && Mathf.Abs(local.y - HitBigCenterDy) <= HitBigHalfH;
        }

#if UNITY_EDITOR
        /// <summary>自检用：第二颗 collider（没建 ⇒ `null`）。断言「存在 + 同 GO + 尺寸/偏移 = 原版字面量」用它。</summary>
        public BoxCollider2D BigColliderForTest { get { return _hitColliderBig; } }
        /// <summary>自检用：第二颗 collider 的 `size`（没建 ⇒ 零向量）。期望值从**原版字面量**来（`1.25 × 2.53`）。</summary>
        public Vector2 BigColliderSizeForTest { get { return _hitColliderBig != null ? _hitColliderBig.size : Vector2.zero; } }
        /// <summary>自检用：第二颗 collider 的 `offset`（没建 ⇒ 零向量）。期望 = `(0, −2.62)`。</summary>
        public Vector2 BigColliderOffsetForTest { get { return _hitColliderBig != null ? _hitColliderBig.offset : Vector2.zero; } }
#endif

        // ==================================================================
        //  战术卡的「印不印兵种行」/「文字态 3 还是 4」—— 同一件事（2026-10-25 · R16）
        // ==================================================================
        // 🔴 **为什么这个方法在类尾**：本文件被几本账按 `<文件>:<行号>` 引用，判据块放在调用点旁边会让
        //    下游行号整体平移（`check_ledger_points.py` 报漂移）⇒ 只留调用点一行，判据全搬到这里。
        //
        // 🥇 原版判据（一手，两份方法体互证）：
        //   · `decomp_full/GameStaticData__CardRaceToString.c` —— `cardType == 0x14`（战术/防御）那一支：
        //     `name = Enum.ToString(spellType@0x90)` → `GetTranslation("Card_Race/" + name)`，
        //     而 **`spellType == 0`（`Normal`）时返回空串** ⇒ **兵种行不印**。
        //   · `decomp_full/CardTextsController__SetStartingState.c` / `__SetCardTextLayout.c` ——
        //     `cardType == Tactic(0x14) || Whispers(0x28)` ⇒ `state = 3 + (spellType != 0)`；其余按 desc 有无。
        //   ⇒ 两条是**同一个谓语**，所以我们只实现一处（这里），文字态读的是它的上游 `SubtypeLine`
        //     （`ComputeTextState` 里 `hasRace = !IsNullOrEmpty(SubtypeLine(d))`）。
        //
        // ⚠️ **两档口径**：`spellTypeKnown == true` ⇒ 原版真判据；`false`（**今天全工程的状态**：生产者
        //    `BattleDriver.ToCardData` 还没填，那个文件在 `Battle/**`）⇒ 退回 `TacticSubtypeShown` 白名单。
        //    实测口径（`cards_engine.json` 1128 张 / tactic 446 张）：白名单与 `spellType != 0` **445/446 一致**，
        //    唯一差的一张 = `Spear of Macragge`（`d94f91051ecfe014f97577ff573571aa`，`spellType=0` 而
        //    subtype 写 `Psychic Power`）。调度台 2026-10-25 已裁定「**副标题行不印，以数字版字段为准**」
        //    ⇒ 生产者一填 `spellType`，这张自动掉行（⛔ 别手工把它从白名单踢掉：那会让**别的**同名 subtype 卡一起掉行）。
        //
        // ⚠️ **已知未覆盖的边角**：`spellTypeKnown && spellType != 0` 而 `subtype` 为空串时，原版仍会印
        //    `Enum.ToString(spellType)`；我们这条返回 true，但 `RaceTerm("")` 也是空 ⇒ `SubtypeLine` 得 null
        //    （态会是 3 而不是 4）。**今天 0 例**（tactic 卡 `subtype` 全非空）；要闭合得再补一张
        //    `spellType → 枚举名` 表（`RaceKeyBySubtype` 已有 6 对，其余枚举名与卡表写法逐字相同）。
        static bool TacticSubtypeLineShown(CardData d)
        {
            if (d.spellTypeKnown) return d.spellType != 0;
            return TacticSubtypeShown.Contains(d.subtype);
        }

        // ==================================================================
        //  glitch —— 原版 `Card2DController.ToggleGlitch(bool)`（2026-10-25 · E7 批次 3）
        // ==================================================================
        // 🔴 **为什么这一段在类尾**：本文件被几本账按 `<文件>:<行号>` 引用（`CardView.cs` 共 159 处），
        //    往中间插成员会让下游整体平移（`check_ledger_points.py` 报「漂移」，别的代理的账也跟着脏）
        //    ⇒ **新成员一律追加在类尾**；就地改动保持**行数不变**（只替换同一行）。
        //
        // 🥇 判据（`Card2DController.cs:132` 现读，cpp2il 真体）：
        //      `ToggleGlitch(bool option) { frame.material = option ? glitchMaterial : originalFrameMaterial; }`
        //    · `Awake()` 里 `originalFrameMaterial = frame.material`（= 出厂那一份，**只缓存一次**）。
        //    · `glitchMaterial` 是**预制体上的 PPtr**（`MonoBehaviour_8218469410760203200.json` →
        //      `{m_FileID: 4, m_PathID: -2877403367212247079}` = 材质 **`Card Frame Glitch`**）。
        //    · **调用面只有一处**：`CardScript.cs:8046` —— `if (HasCurrentTrait(ephemeral) && isPlayer)
        //      card2DController.ToggleGlitch(true);`（紧挨着 `ShowEphemeral()`；**AI 侧不换材质**）。
        //      `CardUI.ShowEphemeral()` 本身是**另一件事**（在卡的位置实例化状态角标 prefab）。
        //    ⇒ 我们把两步合成一个入口 `ShowEphemeral(bool)`（见那个方法的新注释）：
        //      我们的调用点 `BattleDriver.SyncHand` **只对手牌视图**调它（手牌 = 玩家侧），
        //      所以原版那个 `isPlayer` 闸在结构上已经满足，不需要再传一个布尔。
        //
        // 🥇 材质参数（`Material_-2877403367212247079.json` 实读，逐条抄，不取默认值）：
        //      `_GlitchPercentaje` 0.1 · `_ScanLinesIntensity` 0.547 · `_NoiseIntensity` 0.0035 ·
        //      `_AntialiasScale` 1.3 · `_MainColorIntensityFix` 1.29 ·
        //      `_NoiseColor` (0.056604, 0.040851, 0.056604, 1) · `_NoiseScaleAndSpeed` (10, 18.05, 1, 1)
        //    · `_Noise` = 贴图 `Noise Combined`（128²）—— 我们工程里就是
        //      `Resources/Art/card_sdf/cardback_noise.png`，**逐字节相同**
        //      （sha256 前缀 `4b0742aa5fb9f85f`，两侧实测一致）⇒ 走 `CardArt.CardbackSdfNoise()`。
        //    · `_MainTex` 在原版材质里是 **null**（UGUI 的 CanvasRenderer 每帧把 sprite 贴图喂进去）
        //      ⇒ 我们这边必须**显式**写当前卡框那张图（`SyncGlitchMainTex`）。
        //    🔴 **顺手发现（铁律 13·4⑧）**：那份材质上还有 4 个**该 shader 根本没声明**的残留属性 ——
        //      `_Aplha` 0.5 · `_MainColorIntensity` 0.29 · `_NoiseIntensity_Color_Alpha` (0.01,1,0,0) ·
        //      `_NoiseScaleAndSpeed2` (1,1,0,1)。判据 = `Shader.json` 的 `m_PropInfo` 只有 9 个自定义属性
        //      （上面那 7 个 + `_MainTex` + `_Noise`）。**与「原版材质带内置 Standard 残留死值」同一族**
        //      ⇒ ⛔ 不抄、也不要以为它们生效。
        //
        // 🥇 自建 shader = `Resources/Shaders/CardFrameGlitch.shader`（`CardPresentation/CardFrameGlitch`）。
        //    **为什么不直接拿原版 shader**：它在本地也在 `wf_shaders.bundle` 里，但它的 VS 把顶点色乘了
        //    `unity_SpriteColor`（`SpriteRenderer` 专用通道），而我们的卡面全是 **MeshRenderer**
        //    ⇒ 直接用会渲染成黑/透明（本轮不能跑 Unity 验证 ⇒ 不赌）。算式**逐条照抄** DXBC，见那个文件头。

        /// <summary>自建 glitch shader 的名字（`Resources/Shaders/CardFrameGlitch.shader`）。</summary>
        const string GlitchShaderName = "CardPresentation/CardFrameGlitch";

        static readonly int GlitchUvRectId = Shader.PropertyToID("_UvRect");

        Material _glitchMat;                 // 每张卡一份（`_MainTex` 是逐卡的卡框图）
        Material _frameMatBeforeGlitch;      // 原版 `originalFrameMaterial`（换上去之前那一份）
        bool     _glitchShown;
        static bool _glitchWarned;

        /// <summary>现在卡框是不是挂着 glitch 材质（原版「`frame.material == glitchMaterial`」那一档）。</summary>
        public bool GlitchShown { get { return _glitchShown; } }

        /// <summary>原版 `Card2DController.ToggleGlitch(bool)`：把**卡框那一层**的材质在
        /// 「glitch」与「换之前那一份」之间来回换。
        /// <para>=== 与 `BeginFrameDissolve`/`EndFrameDissolve` 的分工 ===</para>
        /// <para>两条都换 `_frame` 的材质：溶解记自己的 `_frameMatBeforeDissolve`、我们记
        /// `_frameMatBeforeGlitch`，各自**只还原自己换上去的那一份**（同族约定见 `EndFrameDissolve` 的注释）。
        /// 两者同时开启（临时卡 + 卡框溶解）时**后开的赢**，先关的那个发现当前不是自己那份就只清状态、不抢材质。</para>
        /// <para>⚠️ `_frame == null`（`frameTex` 取不到时走的占位卡面支路）⇒ 什么都不做并**出声一次**：
        /// 那条支路没有「卡框层」可换（原版也没有 —— 它是同一个 `frame` Image）。</para></summary>
        public void SetGlitch(bool on)
        {
            if (_frame == null)
            {
                if (on && !_glitchWarned)
                {
                    _glitchWarned = true;
                    Debug.LogWarning("[CardView] 这张卡没有卡框层（`_frame == null`）⇒ glitch 材质无处可换"
                                   + "（原版 `Card2DController.ToggleGlitch` 换的就是卡框 Image 的材质）。");
                }
                return;
            }
            var cur = _frame.sharedMaterial;
            bool isGlitch = cur != null && cur.shader != null && cur.shader.name == GlitchShaderName;

            if (on)
            {
                // 🔴 **溶解优先**（两条都换 `_frame` 材质；`ShowEphemeral` 每轮 `SyncHand` 都会重刷，
                //    不挡的话 glitch 会**每帧把溶解材质抢回来**，卡就溶不掉了）。原版两者分属 2D/3D 两条 prefab，
                //    不会撞；我们合并在一个 CardView 上，所以这里显式定优先级（先开的让位给后开的）。
                if (OnFrameDissolveMaterial) { _glitchShown = false; return; }
                var m = GlitchMaterial();
                if (m == null) return;                 // 取不到 shader ⇒ 已出声，卡照常
                SyncGlitchMainTex(m);
                if (!isGlitch)
                {
                    _frameMatBeforeGlitch = cur;
                    m.renderQueue = _queue;            // 跟这张卡的队列段（`SetQueue` 靠 `renderQueue == _queue` 早退）
                    _frame.sharedMaterial = m;
                }
                _glitchShown = true;
                return;
            }

            _glitchShown = false;
            if (!isGlitch)
            {
                // 当前挂的不是我们那份（没开过 / 已被溶解抢占）⇒ 只清状态，别抢别人的材质
                _frameMatBeforeGlitch = null;
                return;
            }
            var back = _frameMatBeforeGlitch;
            _frameMatBeforeGlitch = null;
            if (back != null)
            {
                _frame.sharedMaterial = back;
                StampQueue(_frame);                    // 换回来的那一份可能不是这一档队列 ⇒ 重新盖章
            }
        }

        /// <summary>这张卡的 glitch 材质（懒建；取不到 shader ⇒ `null` + **出声一次**）。
        /// <para>⚠️ 参数**全部来自原版材质**（见上面判据块），不取 shader 默认值 ——
        /// shader 默认 `_NoiseColor` 是**纯白**、`_ScanLinesIntensity` 是 1，
        /// 直接吃默认值会渲成一片白噪音。</para></summary>
        Material GlitchMaterial()
        {
            if (_glitchMat != null) return _glitchMat;
            var sh = Shader.Find(GlitchShaderName);
            if (sh == null)
            {
                if (!_glitchWarned)
                {
                    _glitchWarned = true;
                    Debug.LogWarning("[CardView] 找不到 `" + GlitchShaderName + "` shader ⇒ 临时卡**不换 glitch 材质**，"
                                   + "只留关键词角标（`Resources/Shaders/CardFrameGlitch.shader` 在不在？）");
                }
                return null;
            }
            var m = new Material(sh) { name = "Card Frame Glitch" };
            m.SetFloat("_GlitchPercentaje", 0.1f);            // 原版材质值
            m.SetFloat("_ScanLinesIntensity", 0.547f);
            m.SetFloat("_NoiseIntensity", 0.0035f);
            m.SetFloat("_AntialiasScale", 1.3f);              // ⚠️ 只在 SUPERSAMPLING_ON 那支被读（本材质没开）
            m.SetFloat("_MainColorIntensityFix", 1.29f);      // ⚠️ 可达那支里**没读**（死属性），照抄只为对账
            m.SetColor("_NoiseColor", new Color(0.056604f, 0.040851f, 0.056604f, 1f));
            m.SetVector("_NoiseScaleAndSpeed", new Vector4(10f, 18.05f, 1f, 1f));
            var noise = CardArt.CardbackSdfNoise();
            if (noise != null) m.SetTexture("_Noise", noise);
            else Debug.LogWarning("[CardView] `Art/card_sdf/cardback_noise` 取不到 ⇒ glitch 的噪声图是 shader 默认（白）"
                                + "—— 原版是 `Noise Combined`（128²，与原图逐字节相同的那一张）。");
            m.renderQueue = _queue;
            _glitchMat = m;
            return m;
        }

        /// <summary>把「当前那张卡框图 + 它的 uv 矩形」同步进 glitch 材质。
        /// <para>⚠️ 原版不需要这一步（`_MainTex` 由 CanvasRenderer 喂）；我们的卡框是 MeshRenderer，
        /// `_MainTex` 必须自己写。**每次 `SetGlitch(true)` 都同步**，因为 `SetFace` 会换卡框图而
        /// 我们不知道（本文件 `SetData` 不碰 `_frame`）。</para></summary>
        void SyncGlitchMainTex(Material m)
        {
            if (m == null || _frame == null) return;
            // ⚠️ **优先读 `_frameTex` 这个字段**（`Build` 里记下的「这张卡该用哪张卡框图」，`SetFace` 会更新它）——
            //    已经换着 glitch 材质时 `_frame.sharedMaterial.mainTexture` 就是我们自己写的那张，读它认不出「图变了」。
            var tex = _frameTex;
            if (tex == null)
            {
                var cur = _frame.sharedMaterial;
                tex = cur != null ? cur.mainTexture as Texture2D : null;
            }
            if (tex == null || m.mainTexture == tex) return;
            m.mainTexture = tex;
            // 我们的卡框 mesh uv 是**图集子矩形** ⇒ 噪声 UV 要按 0..1 归一化（见 shader 文件头 `_UvRect`）
            var r = FrameUv(tex);
            m.SetVector(GlitchUvRectId, new Vector4(r.x, r.y, r.width, r.height));
        }

        /// <summary>销毁本卡那份 glitch 材质（`OnDestroy` 里调；`SetGlitch` 之后可能从没建过 ⇒ 空操作）。</summary>
        void DisposeGlitchMaterial()
        {
            _frameMatBeforeGlitch = null;
            _glitchShown = false;
            if (_glitchMat == null) return;
            if (Application.isPlaying) Object.Destroy(_glitchMat); else Object.DestroyImmediate(_glitchMat);
            _glitchMat = null;
        }

#if UNITY_EDITOR
        /// <summary>自检用：这张卡的 glitch 材质（没建过 = `null`；建过才能断参数）。</summary>
        public Material GlitchMatForTest { get { return _glitchMat; } }
        /// <summary>自检用：换 glitch 之前那一份卡框材质（没开过 / 已关 = `null`）。</summary>
        public Material FrameMatBeforeGlitchForTest { get { return _frameMatBeforeGlitch; } }
        /// <summary>自检用：卡框层当前材质（= `FrameMatView`，这里给个短名字给灭自证断言读）。</summary>
        public Material FrameMatNowForTest { get { return _frame != null ? _frame.sharedMaterial : null; } }
#endif
    }
}
