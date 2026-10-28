// CardHighlight.cs — 卡牌的状态色
//
// 🔴 **2026-09-18 更正：原版那 5 个状态色已经找到了，不再是「自己定」。**
//    出处（**亲读**）：`d:/2/解包整理/08_预制体特效/战斗预制体/MonoBehaviour/MonoBehaviour_-3885077450169410624.json`
//    （卡预制体上的 `CardHighlight` 组件，1344 B）——
//      ValidTargetColor    (0.0, 1.0, 0.1294)
//      SelectedColor       (1, 1, 1, 1)
//      PlayableColor       (1, 1, 0)
//      SelectedTargetColor (1, 0.5176, 0)
//      RegularColor        (1, 1, 1, **0**)      ← alpha 0 = **完全不亮**
//      CardHighlightAnimTime = 0.1 · ScaleFactor = 1.05
//    旁证：与 `Unity/_资源评估_场景特效动画.md:506,617`（2026-09-10 录的）**逐值吻合** ⇒ 两条独立来源一致。
//    ⚠️ 原写「原版的 5 色态是**结构**参考、具体颜色自己定（原版配色是它的美术资产）」——
//       **版权红线 2026-09-18 已由用户取消**，而且值本来就在本地，**没有理由再自己定**。
//
// 🔴 **但原版值不能整表照搬**，原因有二（照搬会坏）：
//   ① 原版那 5 色是给 `FrameHighlight` / `FrameHighlightRemnant` **两个 SpriteRenderer（描边层）** 用的；
//      而本类的 `ColorOf` 在我们这里**同时喂给「整卡着色」(`SetTint`) 与「描边」(`_rim`)** 两个地方
//      （`CardView.cs` 的 `SetTint` 与 `_rim`）。
//   ② 原版 `Regular` 的 **alpha = 0**（= 不点亮）。把 `Normal` 直接改成 alpha 0，
//      整卡着色那条路会把常规卡**染成全透明**。
//   ⇒ 所以下面**按状态逐个映射**，并在每行注明它对应原版哪一个、以及哪些是**我们自己的状态**。
//
// 🔴🔴 **2026-10-24 就地改判（用户真机报「加攻/加血时数值变红」）—— `ColorOf` 的棋盘两态
//    `ValidTarget` / `SelectedTargetInBoard` 改成返回【白】= 整卡不着色。**
//    判据（全量反编译，第一权威）：
//      · `decomp_full/CardHighlight__ChangeFrameColor.c` —— 整个方法只做一件事：
//        `SpriteRenderer.set_color`（`param_1 + 0x70` / `+0x78` 两颗 = `FrameHighlight` /
//        `FrameHighlightRemnant`），外加 `DOColor` 补间与 `ToggleFrames`。**从头到尾没有一处
//        碰卡的材质 / 非环图层**。
//      · `decomp_full/CardHighlight__ChangeState.c` —— 6 个 case 全部只走
//        `ChangeFrameColor`（颜色）+ `ChangeTargetScale`（缩放，仅 case 0/3/4）。**没有第三种出口。**
//      · 原版那个组件在**同一个卡预制体**上、手牌与棋盘共用（state 1 `potentialTargetInHand`
//        与 state 3 `potentialTargetInBoard` 都取 `+0x80` 同一份绿色）
//        ⇒ **手牌那档原版也不整卡着色**，所以下面这处的白是两档一起的，不是只修棋盘。
//    ⇒ 颜色**只走** `FrameColorOf` → `_rim`（那才是原版那一层）。
//    ⚠️ 反过来说：原来「整卡染绿/染橙」是**我们自己加的**（旧注释自己写着「我们的表达方式」），
//       而它和数值的涨落色是**乘法**叠加（`CardView.SyncStatLayers`：`_tint * c`）
//       ⇒ 白字 × #FF8400 = (255,132,0)，用户看到的就是「加攻变红」。这就是本次改判的直接起因。
//
// ⚠️ 现在**状态色只剩「环 + 缩放」两条表达**（整卡着色这条路对棋盘目标态已经关掉），
//    一张卡同时是「可打出」又「被悬停」时分不开的问题**依然存在** —— 正式版仍该走描边/流光，
//    那时改这张表即可，状态机不用动。
//
//    ✅ **2026-09-19：`CardBodyToScale × ScaleFactor` 的缩放补间已接上**（`CardView.SetHighlightScale`，
//       时长用 `CardHighlightAnimTime`）。原写「我们只放大、没有补间」—— 那句已作废。
//    ⏭ **还差的**：原版卡面最底层那层 **SDF 软光/影**（`Card Highlight And Shadow`，4.4281²）。
//       ✅ **2026-09-19 已查实**：**不是运行时生成、也不归 `CardHighlight` 管** —— 是 Addressables 里
//       **预生成的 SDF 资产**（`40k_Cardframe_{troop|stratagem}_<阵营>_SDF_tier1..4`，104 张，本地就有），
//       shader = `Everguild/FX/Card Highlight And Shadow`，状态只改 `_Outline.rgb` / `_ShadowColor.a`、
//       补间 0.2s。**正本 = `资料/普查产出_0919/卡面SDF软光影_查证.md`**（含我们这侧的复刻清单）。
using UnityEngine;

namespace CardPresentation
{
    public enum CardHighlightState
    {
        Normal,        // 常规          —— 对应原版 `regular`（原版值 alpha 0 = 不点亮；我们保留原色不染）
        Playable,      // 可打出 —— 费用够、轮次对  —— 对应原版「打得出去」那一档：
                       //   🔴 **原版的表现是 SDF `_Outline` 变色**（`OutlineOf`，我们已接），
                       //   **不是**那圈状态环、也不是整卡染色。原来的注释把它标成 `playable` + 黄色是**误标**
                       //   （原版那个黄是 `displayingActiveAbility`）—— 2026-09-29 更正，见 `ColorOf` 与 `FrameColorOf`。
        Unplayable,    // 不可打出 —— 置灰             —— ⚠️ **我们自己的状态**，原版没有对应的
        Selected,      // 已选中                        —— 对应原版 `selected`
        ValidTarget,   // 合法目标（选目标阶段）          —— 对应原版 `potentialTarget*`（手牌/场上两档共用同一色）
        Hover,         // 鼠标悬停                      —— ⚠️ **我们自己的状态**，原版没有对应的
        // 🆕 **2026-09-29 补上**（原来这里写着「原版还有一个 `selectedTargetInBoard`，我们没有这个状态」）：
        //   原版第 5 态 `selectedTargetInBoard` = **当前指针压着的那个（棋盘上的）目标**，
        //   橙 `SelectedTargetColor` **#FF8400** + **×1.05**（判据 → `资料/待办判据_战场与战斗视图.md` §8b / Q7）。
        //   ⚠️ 它和 `ValidTarget` 是**同一时刻的两档**：合法目标一律绿（原版 `potentialTargetInBoard`），
        //      **指针正压着的那一个**换成橙。原版由一个状态机写，我们照同一套。
        SelectedTargetInBoard,
    }

    public static class CardHighlight
    {
        /// <summary>原版 `CardHighlightAnimTime` —— 状态切换那一下的**补间时长（秒）**。
        /// ✅ **两处补间现在都接上了**：缩放（2026-09-19，`CardView.SetHighlightScale`）+
        /// 状态环的颜色（2026-09-29，`CardView.ApplyRimColor`）。
        /// ⚠️ **不是所有态都补间** —— `selected` / `displayingActiveAbility` 原版传的是 **0（瞬切）**，
        /// 见 `TweenTimeOf`。（本条原写「我们目前没有补间实现」—— 那句已作废。）</summary>
        public const float AnimTime = 0.1f;

        /// <summary>原版 `ScaleFactor` —— 高亮时卡体放大到多少倍（`CardBodyToScale`）。见 `ScaleOf`。</summary>
        public const float ScaleFactor = 1.05f;

        /// <summary>原版 `HIGHLIGHT_COLOR_CHANGE_TIME` —— 那圈描边换色的**补间时长**。
        /// 出处：`Card2DController.ChangeHighlightColor` 里传的是 `.rdata DAT_1834b2bb0` = **float 0.2**
        /// （全段唯一一个字面量；见 `资料/普查产出_0919/卡面SDF软光影_查证.md` §六）。</summary>
        public const float OutlineAnimTime = 0.2f;

        // 原版那 5 个状态色（**实读**，逐位照抄 —— 见文件头出处）。
        // ⚠️ **2026-10-24 起它们只喂 `FrameColorOf`（那圈环）**，不再喂 `ColorOf` 的整卡着色 ——
        //    `ColorOf` 现在对 `ValidTarget` / `SelectedTargetInBoard` 返回白（原因见文件头 2026-10-24 那段）。
        /// <summary>原版 `ValidTargetColor` —— 绿 **#00FF21**（手牌/场上两档「潜在目标」共用）。**只点环**。</summary>
        public static readonly Color ValidTargetColor = new Color(0.00f, 1.00f, 0.1294118f);
        /// <summary>原版 `SelectedColor` —— 白（`selected` 是**瞬变**态）。</summary>
        public static readonly Color SelectedColor = new Color(1.00f, 1.00f, 1.00f);
        /// <summary>原版 `SelectedTargetColor` —— 橙 **#FF8400**（`selectedTargetInBoard`，**×1.05**）。**只点环**。
        /// ⚠️ 它一旦被拿去做整卡着色，就会和数值的涨/落色相乘（白字 × 本值 = (255,132,0) 橙红）。</summary>
        public static readonly Color SelectedTargetColor = new Color(1.00f, 0.5176f, 0f);

        /// <summary>**整卡着色**用哪一色（`CardView.SetHighlight` → `SetTint` → 所有 `_layers` 的 `_Color`）。
        /// 🔴 **2026-10-24 起：棋盘两态返回白 —— 本函数不再是「原版状态的表达」，而是「整卡该不该被染色」的闸。**
        /// 原版的五个状态色**只喂那圈描边**（`FrameColorOf`），本函数这边的整卡着色是**我们自加的通道**
        /// （判据与来龙去脉见文件头 2026-10-24 那一段）；它一旦非白，会和数值的涨/落色**相乘**
        /// （`SyncStatLayers` 的 `_tint * c`）⇒ 白字被染成橙红 = 用户报的「加攻变红」。
        /// ⚠️ 手牌与棋盘共用同一行（`Hand/CardInteraction.cs:715` 拖牌时也给 `ValidTarget`）——
        /// 原版两档是同一个组件的同一份绿色、同样只点环，所以这里一起返回白，不是只修棋盘。</summary>
        public static Color ColorOf(CardHighlightState s)
        {
            switch (s)
            {
                // ↓ 以下三行 = **原版实读值**，逐位照抄
                // 🔴 **2026-09-29 更正**：这一行原来写的是「原版 PlayableColor：黄」并返回 (1,1,0)。
                //    原版那个黄色属于 **`displayingActiveAbility`（正在展示主动技能）**，**不是** playable；
                //    「打得出去」表现走的是 **SDF `_Outline`** 那条（`OutlineOf`，**我们已接**，见 `CardView.SetOutline`）。
                //    两层是分开的、**别合并**（判据 → `资料/待办判据_战场与战斗视图.md` §8b）。
                //    ⇒ 这里改成**不上色**：原来那层黄是「整卡染黄」，既不是原版的做法、也和 `_Outline` 重复表达同一件事。
                case CardHighlightState.Playable:    return Color.white;
                case CardHighlightState.Selected:    return SelectedColor;                         // 原版 SelectedColor：白
                // 🔴 **2026-10-24 改判：原来这里 `return ValidTargetColor`（绿 #00FF21）⇒ 整卡染绿。**
                //    原版的绿只点那圈环（`FrameColorOf` + `ChangeFrameColor` 实证），整卡绿是我们自加的。
                //    🧨 改坏法：把这一行改回 `ValidTargetColor` ⇒ 棋盘/手牌的合法目标整卡变绿、
                //       数值色 = 绿，`CardBaseDemo` 的「数值层是白」那条必红。
                case CardHighlightState.ValidTarget: return Color.white;                           // 绿只点环（见 FrameColorOf）
                // 🔴 **2026-10-24 改判：原来这里 `return SelectedTargetColor`（橙 #FF8400）⇒ 整卡染橙。**
                //    白字 × #FF8400 = (255,132,0)，就是用户 2026-10-24 报的「加攻/加血时数值变红」。
                //    原版那一档同样只点环（`ChangeState` case 4 只调 `ChangeFrameColor` + `ChangeTargetScale`）。
                //    🧨 改坏法：把这一行改回 `SelectedTargetColor` ⇒ 数值层红分量 1 / 绿分量 0.5176 ⇒ 断言必红。
                case CardHighlightState.SelectedTargetInBoard: return Color.white;                 // 橙只点环（见 FrameColorOf）

                // ↓ 以下三行 = **我们自己的**（原版没有这三个状态；色沿用我们原来的）
                case CardHighlightState.Unplayable:  return new Color(0.55f, 0.55f, 0.58f);        // 置灰
                case CardHighlightState.Hover:       return new Color(1.00f, 1.00f, 1.00f);        // 原色（抬起靠位移表达）
                // 原版 `RegularColor` 是 (1,1,1,**0**)。**这里故意不照抄 alpha=0** ——
                // 本函数同时喂 `SetTint`，alpha 0 会把常规卡染成全透明。**原版的「不亮」由不上色实现，
                // 我们由「不调用」实现**（`CardView.cs:1177` 那条 `show` 判据）。
                default:                             return Color.white;
            }
        }

        /// <summary>那圈**状态环**（原版 `FrameHighlight` 这个 SpriteRenderer 的颜色）—— 原版 6 态：
        /// `regular`(**alpha 0** = 不亮) · `potentialTargetInHand`(绿 `#00FF21`) · `selected`(白) ·
        /// `potentialTargetInBoard`(绿 · ×1.05) · `selectedTargetInBoard`(橙 `#FF8400` · ×1.05) ·
        /// `displayingActiveAbility`(黄)。出处 → `资料/待办判据_战场与战斗视图.md` §8b。
        /// **alpha == 0 ⇒ 那层整个关掉**（原版补间完就 `ToggleFrames(false)`）。
        /// 🔴 **和 `ColorOf` 是两条路**：`ColorOf` 那条是「整卡着色」（**我们自加的通道**，
        /// 2026-10-24 起棋盘两态已经收成白、等于关掉），**这条才是原版那一层**（`ChangeFrameColor` 实证）。
        /// `Playable` 在这一路**不亮** —— 见 `ColorOf` 里那条更正。</summary>
        public static Color FrameColorOf(CardHighlightState s)
        {
            switch (s)
            {
                case CardHighlightState.Selected:    return SelectedColor;       // 白（瞬变）
                case CardHighlightState.ValidTarget: return ValidTargetColor;    // 绿
                // 原版 `selectedTargetInBoard` —— 橙 **#FF8400**（指针压着的那个棋盘目标）
                case CardHighlightState.SelectedTargetInBoard: return SelectedTargetColor;
                // ⚠️ `Hover` 是**我们自己的**状态（原版没有）—— 沿用我们原来的「白环」
                case CardHighlightState.Hover:       return SelectedColor;
                default:                             return new Color(0f, 0f, 0f, 0f);   // 不亮（含 Playable / Unplayable / Normal）
            }
        }

        /// <summary>高亮时卡体放大到多少倍。**原版 `ScaleFactor` = 1.05**（`CardBodyToScale` × `ScaleFactor`）。
        /// ⚠️ 原值 1.08 是**我们挑的**，2026-09-18 换成原版值。
        /// ✅ **2026-09-19 起有调用点了**：`CardView.SetHighlight` → `SetHighlightScale`
        /// （做在「基础缩放 × 高亮系数」上、带 `AnimTime` 补间 —— 直接改 `localScale` 会被布局重排抹掉）。
        /// （本条原写「当前没有任何调用点用到本函数」—— 那句已作废。）</summary>
        /// <param name="onBoard">这张卡是不是在**棋盘**上 —— 原版同一个「潜在目标」色有**两档**：
        /// state 1 `potentialTargetInHand` **不缩放**、state 3 `potentialTargetInBoard` **×1.05**。</param>
        public static float ScaleOf(CardHighlightState s, bool onBoard = false)
        {
            switch (s)
            {
                case CardHighlightState.Hover: return ScaleFactor;                    // ⚠️ 我们自己的状态（手牌悬停抬起）
                case CardHighlightState.SelectedTargetInBoard: return ScaleFactor;    // 原版 state 4
                case CardHighlightState.ValidTarget: return onBoard ? ScaleFactor : 1f;   // 原版 1（手牌）不缩放 / 3（棋盘）×1.05
                // 🔴 **2026-09-29 更正**：`Selected` 原版**不缩放** —— 实读 `CardHighlight__ChangeState.c`：
                //    只有 **case 0 / 3 / 4** 里有 `ChangeTargetScale` 调用，**case 2 `selected` 没有**。
                //    原来这里给 1.05 是**误标**（当时把它和 `selectedTargetInBoard` 当成同一档了）。
                default: return 1f;
            }
        }

        /// <summary>原版 `ChangeState` 传给 `ChangeFrameColor` 的**补间时长**
        /// （实读 `CardHighlight__ChangeState.c`）：**state 2 `selected` 与 state 5 `displayingActiveAbility`
        /// 走 `uVar2 = 0` 那一支（瞬切）**，其余（0 `regular` / 1 / 3 / 4）都是 `CardHighlightAnimTime` = 0.1。</summary>
        public static float TweenTimeOf(CardHighlightState s)
        {
            return s == CardHighlightState.Selected ? 0f : AnimTime;
        }

        /// <summary>🔴 **我们这条通道的合成系数** = 原版材质 `Card board Frame SDF` 里那个
        /// **`_Outline.a` = 0.4470588**（实读材质 JSON）。
        /// 原版是**两个输入**：渲染器色（脚本写进 `spriteRenderer.color`，alpha 1）× 材质常量 `_Outline`；
        /// 我们是 **MeshRenderer**、**没有 `unity_SpriteColor` 那条通道**（见 `CardView.RimMaterial`）
        /// ⇒ 只有 `_Outline` 一个槽，只能把状态色与这个常量**乘在一起**写进去。
        /// ⚠️ **这是推断**：C# 层已证「脚本不乘材质」（`CardHighlight` 13 个方法里没有任何
        /// `Material.SetColor/SetFloat`），shader 字节码那条**正在查**（`工具/dump_shader_blob.py`）——
        /// 若查出来的关系不是这个，**只改这一个常量**即可。</summary>
        public const float RimAlphaScale = 0.4470588f;

        // ==================================================================
        //  `_Outline` —— 原版卡面那圈**高亮描边**（SDF 那层材质上的 `_Outline`）
        // ==================================================================
        //
        // 🔴 **和上面那 5 个状态色是两套东西**：那 5 个喂的是原版**另一个组件**（`CardHighlight` →
        //    对象上的 `FrameHighlight` SpriteRenderer）；这一套喂的是 `Card2DController` 管的
        //    **SDF 层材质的 `_Outline`**。两套互不相干，别合并。
        //
        // 颜色与判据 = **原版预制体直读**（`CardPrefab` 那具 `BattleCardUI`，字段名落盘），
        // 逐值见 `资料/普查产出_0919/卡面SDF软光影_查证.md` §六。
        public static readonly Color OutlinePlayable  = new Color(0.10980392f, 0.44313726f, 0.00392157f, 1f);   // #1C7101 深橄榄绿
        public static readonly Color OutlineEphemeral = new Color(0.35686275f, 0.05882353f, 0.32549021f, 1f);   // #5B0F53 紫
        public static readonly Color OutlineSpecial   = new Color(0.25882354f, 0.89803922f, 1.00000000f, 1f);   // #42E5FF 青（teleport / oath 同色）
        public static readonly Color OutlineSabotage  = new Color(1.00000000f, 0.13217452f, 0.00000000f, 1f);   // #FF2200 红
        public static readonly Color OutlineSelected  = new Color(0.00000000f, 0.21568628f, 0.39607844f, 1f);   // #003765 深蓝（选中）
        public static readonly Color OutlineLegendary = new Color(1.30411887f, 0.38584328f, 0.01365572f, 1f);   // #FF6203 亮橙（传说选中，原值是 HDR r>1）

        /// <summary>
        /// 原版 `BattleCardUI.ChangeToHighlightColor` 的判据（外层先判；出处同上 §六）：
        /// **打得出去 → 按 trait 分色；打不出去 → alpha 0（根本不描边）**。
        /// ⚠️ 两条判据我们**没法在这里自己算**，由调用方算好传进来：
        ///   · `drawnThisTurn` = **这一份**是不是本回合从牌库抽到的（`CardInstance.DrawnThisTurn`）；
        ///   · `oathAffordable` = 原版那条是 `manaLeft >= cost + oath值`（**誓约的钱够不够**）。
        ///   （费用/能量只有引擎那侧知道 —— 判据不在这里重算，见工程规矩「两处写同一条规则 = 迟早不一致」。）
        /// </summary>
        public static Color OutlineOf(RuleEngine.CardDef card, bool canBePlayed,
                                      bool drawnThisTurn = false, bool oathAffordable = false)
        {
            if (!canBePlayed) return new Color(0f, 0f, 0f, 0f);          // 关：取当前 rgb、只把 alpha 换 0
            if (card == null) return OutlinePlayable;
            // 🔴 **破坏的判据是「卡类」，不是关键词**（2026-09-21 改）——
            //    原版 `BattleCardUI.ChangeToHighlightColor`
            //    （`d:/2/tools/decomp_full/BattleCardUI__ChangeToHighlightColor.c`）判的是
            //    `spellType == 0xe6 (= 230)`（`SpellType.cs:27 Sabotage = 230`），
            //    而 trait 那一族（`HasCurrentTrait(5 / 0x4ce / 0x4fb)`）里**没有**它。
            //    ⚠️ 这里原来写的是 `card.Has("sabotage")`（读 `keywords` 那一列）——
            //    那三个词是 OCR 把**卡面下方那行橙字（兵种行）**误当关键词抽出来的，
            //    已清（见 `资料/关键词图标_现状与总表.md` §六 第 7 条）⇒ 判据跟着落到 subtype，
            //    否则这三张高亮会掉，而且第 4 张破坏卡 `GSC_Jammed_Communications`
            //    （`subtype` 也是 Sabotage、`keywords` 本来就是空的）**一直拿不到红描边** —— 三有一无本就不一致。
            //    兵种这一维的判据全仓只有一份：`CreatePool.MatchesKind`（`CardCriteria` 也转调它）。
            if (RuleEngine.CreatePool.MatchesKind(card, "sabotage")) return OutlineSabotage;
            if (card.Has("ephemeral")) return OutlineEphemeral;
            if (card.Has("teleport") && drawnThisTurn) return OutlineSpecial;
            if (card.Has("oath") && oathAffordable) return OutlineSpecial;
            return OutlinePlayable;
        }

        /// <summary>原版 `ChangeHighlightToSelectColor`：**按稀有度**选色（`rarity == Legendary(4)` 走 HDR 橙）。</summary>
        public static Color OutlineSelectOf(string rarity)
        {
            return string.Equals(rarity, "legendary", System.StringComparison.OrdinalIgnoreCase)
                 ? OutlineLegendary : OutlineSelected;
        }

        // ==================================================================
        //  🆕 **场上卡高亮【状态机】** —— 原版 `CardHighlight` 组件的等价物
        //  2026-10-26（执行代理 `AW`）· 判据全部**亲读**（全量反编译，🥇 `decomp_full/*.c`）
        // ==================================================================
        //
        //  🔴 **本类补的是哪一半**：渲染侧（状态环 6 色 / 卡体缩放 / 会死图标 / 攻击类型底光 /
        //     残骸环拷色）**我们早就有了**；缺的是**那台状态机本身** ——
        //     谁持有 `State`、谁能从 `potentialTargetInBoard(3)` 变到 `selectedTargetInBoard(4)`
        //     再退回 `3`、以及 `state == regular(0)` 那道**入口守卫**。四个输入入口
        //     （`OnTouchDown` / `OnTouchEnter` / `OnTouchExit` / `OnTouchUpAsButton`）**唯一的共同卡点**
        //     就是它 —— 没有它，那四个入口里所有「置高亮态」的句子都没有落点。
        //
        //  判据（**逐份亲读**，行号是现读）：
        //   · `CardHighlight__ChangeState.c:1-77`      —— 6 档本体（早退 :14-16 · `ToggleFrames(true)` :17 ·
        //       写 State :18 · 逐 case 的色字段 + 补间时长 + 是否缩放）
        //   · `CardHighlight__ChangeFrameColor.c:1-94`  —— `SpriteRenderer.set_color` ×2（`+0x70` 主环 /
        //       `+0x78` 残骸环）+ `DOColor` 补间 + `AppendCallback`（alpha 到 0 才 `ToggleFrames(false)`）
        //   · `CardHighlight__ChangeTargetScale.c:1-41` —— `localScale = initialScale(+0x60/68) × ScaleFactor(+0x30)`
        //       或补到入参；`time <= 0` 走瞬切
        //   · `CardHighlight__ToggleFrames.c:1-33`      —— `SetActive(bool)` ×2（`+0x70` / `+0x78` 的 gameObject）
        //   · `CardHighlight__Start.c:1-29`             —— 捕获 initialScale · `ToggleFrames(true)` ·
        //       `ChangeFrameColor(RegularColor, 0)` · `ToggleCombatPreviewHighlight(null,0,0,1,0)`
        //   · `CardHighlight__SetCombatHighlights.c:1-11` —— 只做 `minionWillDieObject.SetActive(false)`
        //   · `CardHighlight__HighlightAttackTypes.c:1-28` —— 遍历 `highlightAttackTypes[]` 逐个
        //       `HighlightAttackType.HighlightIfNeeded(attackType)`（我们的等价物 = `CardView.SetOwnAttackType`）
        //   · `CardHighlight__ToggleCombatPreviewHighlight.c:1-178` —— 伤害预览 / 「会死」/ 攻击类型底光
        //   · `CardHighlight__SetRemnantHighlight.c:1-29` —— 把主环的 `color` 原样拷给残骸那个
        //       `SpriteRenderer`（✅ 我们已由 `CardView.SetHighlight` → `SetRemnantLight(FrameColorOf(s))` 承担）
        //   · 四个入口：`CardScript__OnTouchDown.c:1-97` · `__OnTouchEnter.c:1-40` · `__OnTouchExit.c:1-57` ·
        //       `__OnTouchUpAsButton.c:1-87` · `__TryCardHighlight.c:1-99`
        //
        //  字段偏移（`d:/2/tools/il2cpp_out/dump.cs`，**本轮展开核过**）：
        //   · `CardHighlight`（`:34145-34205`）：`+0x20 CardHighlightAnimTime` · `+0x24 state` ·
        //       `+0x28 CardBodyToScale` · `+0x30 ScaleFactor` · `+0x38 minionWillDieAnimation` ·
        //       `+0x40 minionWillDieObject` · `+0x48 highlightAttackTypes` · `+0x50 cardScript` ·
        //       `+0x58 scaleChangeAnimation` · `+0x60 initialScale` · `+0x70 FrameHighlight` ·
        //       `+0x78 FrameHighlightRemnant` · `+0x80 ValidTargetColor` · `+0x90 SelectedColor` ·
        //       `+0xA0 PlayableColor` · `+0xB0 SelectedTargetColor` · `+0xC0 RegularColor` ·
        //       `+0xD0 colorChangeAnimation`
        //   · `CardHighlight.HighlightStates`（`:34132-34141`）= **6 值，逐值照抄**（见下面的枚举）
        //   · `CardScript`（`:35370-35532`）：`+0x218 cardHighlight` · `+0x228 cardState` ·
        //       `+0x248 IsAnimatingCombat` · `+0x24A displayingCardFlag` · `+0x290 CardUI` ·
        //       `+0x320 timeSinceLastClickOnCard`（= 反汇编里的 `param_1 + 800`！**十进制 800 = 0x320**）
        //   · `EntityScript`（`:21963-22250`）：`+0x28 rawCardData` · `+0x40 isPlayer` · `+0x65 isRemnant` ·
        //       `+0x120 currentAttackType`
        //   · `RawCardScript`（`:23244+`）：`+0x90 spellType` · `+0x94 cardType`
        //       🔴 **这就是 B8_AL 留下的那个缺口**（「`+0x28`（含 `+0x90 == 0xE6`）字段名未锚定」）：
        //       `+0x28` = `rawCardData`、`+0x90` = `spellType`、`0xE6(230)` = `SpellType.Sabotage`；
        //       同一族的 `+0x94 == 0x14(20)` = `CardTypeOptions.Tactic`。**已锚定，不用再猜。**
        //   · `BattleManager`（`:30709+`）：`+0x1E8 mulliganManager` · `+0x290 UIstate` ·
        //       `+0x330 cardTracked`（指针正悬着的那张）· `+0x419 waitingToResolveUseWaystoneFlag`
        //   · 枚举：`UIstateOptions` 15 档 / `CardStateOptions` 18 档 / `CardTypeOptions` / `SpellType`
        //       / `AttackTypes`（`None 0 / Melee 1 / Ranged 2 / Spell 3 / Active 4`）
        //
        //  ⚠️ **两个枚举别合并**（本文件里现在有两套「状态」）：
        //    · `CardHighlightState` = **我们自己的视图枚举**（多 `Playable` / `Unplayable` / `Hover` 三档，
        //      **没有** `displayingActiveAbility`）—— 它是「渲染开关」，`CardView.SetHighlight` 的入参；
        //    · `CardBoardHighlightState` = **原版那 6 档**，是状态机的状态。
        //    映射只有一处：`CardBoardHighlight.MapToView`。

        /// <summary>原版 `CardHighlight.PlayableColor` = **`(1,1,0)` 黄**（`+0xA0`，`ChangeState` **case 5**
        /// `displayingActiveAbility` 用的那一色）。
        /// <para>🔴 与我们枚举里的 <see cref="CardHighlightState.Playable"/> **不是一回事** —— 那个是
        /// 「打得出去」，原版**没有**这一档（走 SDF `_Outline`，见 `OutlineOf`）。这一色只喂状态环。</para></summary>
        public static readonly Color PlayableColor = new Color(1.00f, 1.00f, 0f);

        /// <summary>原版 `CardHighlight.HighlightStates`（`dump.cs:34132-34141`）—— **名字与数值逐条照抄**。
        /// <para>⛔ 别拿它替 <see cref="CardHighlightState"/>（那是我们的视图枚举，理由见上面那段「两个枚举别合并」）。</para></summary>
        public enum CardBoardHighlightState
        {
            regular = 0,                 // 常规（原版 RegularColor 的 alpha = 0 ⇒ 环不亮）
            potentialTargetInHand = 1,   // 手牌里的合法目标（绿，**不缩放**）
            selected = 2,                // 已选中（白，**瞬切**、不缩放）
            potentialTargetInBoard = 3,  // 场上的合法目标（绿，×1.05）
            selectedTargetInBoard = 4,   // 指针正压着的场上目标（橙 #FF8400，×1.05）
            displayingActiveAbility = 5, // 正在展示主动技能（黄，**瞬切**、不缩放）
        }

        /// <summary>原版 `CardStateOptions`（`dump.cs:45465-45487`）里本状态机读到的几档 —— **数值逐条照抄**。
        /// ⚠️ 我们引擎里**没有** `cardState` 这个字段（方案：我们按「按下/移动/抬起 + 四道闸」分派）⇒ 这几个
        /// 常量只给**宿主**在调本状态机的入口时当实参用（调用点必须自己已知这张卡在哪一档）。</summary>
        public static class CardStateId
        {
            public const int inDeck = 0;
            public const int inHand = 1;
            public const int inPlay = 2;
            public const int inPlayAttacking = 3;
            public const int inHandShowing = 8;
            public const int inMulliganSelected = 13;
            public const int inMulliganDiscarded = 14;
            public const int inPlayAimingAttack = 17;
        }

        /// <summary>伤害预览探针要回给状态机的东西（原版 `ToggleCombatPreviewHighlight` 里那四句算式的**结果**）。
        /// <para>🔴 **为什么做成探针**：原版那四句（`GetThisCardPlayedDamage` / `EnemyReturnsAttack` /
        /// `GetCombatDamageWithType` / `GetActiveAbilityDamage`）与
        /// `EnoughPendingDamageToDieWithDamageValues` 全在**引擎那一侧**（要 `BattleContext` /
        /// `UnitState`）；本类在表现层、拿不到，也**不该**重算一遍（工程规矩：两处写同一条规则 = 迟早不一致）。
        /// 探针由宿主接（我们这边现成的等价物 = `RuleCore.AttackDamageEntries` /
        /// `ActiveAbilityDamageEntries` + `RuleCore.WouldKillByEntries`，见 `BattleDriver.SetReticleTarget`）。</para></summary>
        public struct CombatPreviewInfo
        {
            /// <summary>探针有没有算出结果。`false` ⇒ 状态机把预览整层收掉并**出声**（⛔ 不静默假装没这回事）。</summary>
            public bool Valid;
            /// <summary>原版 `otherCard.rawCardData.cardType == CardTypeOptions.Tactic(20)`（`:44` / `:132`）。
            /// 计策卡在预览里走**另一套算式**，而且最后**不点亮**攻击类型底光（`:131-134` 直接 return）。</summary>
            public bool OtherIsTactic;
            /// <summary>原版 `CardScript.EnoughPendingDamageToDieWithDamageValues(...)` 的结果（`:95-120`）：
            /// 真 ⇒ 播「会死」动画 + `minionWillDieObject.SetActive(true)`。</summary>
            public bool WillDie;
            /// <summary>原版传给 `HighlightAttackTypes` 的那个 `AttackTypes` 值。
            /// · **预览支**（`other` 活着）= **`other` 自己的** `currentAttackType`（`:141`）；
            /// · **收的支** = 自己的（`isPlayer` ? 自己的 `currentAttackType` : `None`，`:163-168`）。</summary>
            public int AttackTypeToHighlight;
        }

        /// <summary>伤害预览探针（宿主接）。入参逐位等于原版
        /// `ToggleCombatPreviewHighlight(CardScript otherCard, bool isAttacker, bool changeAttackType, bool formUnityAbility)`。</summary>
        public delegate CombatPreviewInfo CombatPreviewProbe(CardView other, bool isAttacker,
                                                             bool changeAttackType, bool formUnityAbility);

        /// <summary>调四个入口时要喂的原版读数。**每个字段都标了它替原版哪一个字段/表达式** ——
        /// ⛔ 别凭名字猜（例如 `HoverAllowed` 不是「UIstate == normalBattle」，见它自己的 doc）。</summary>
        public struct TouchInput
        {
            /// <summary>原版 `CardScript.cardState`（`+0x228`，`CardStateOptions`）。用 <see cref="CardStateId"/> 的常量。</summary>
            public int CardState;
            /// <summary>原版 `EntityScript.isPlayer`（`+0x40`）。</summary>
            public bool IsPlayer;
            /// <summary>原版 `CardScript.TryCardHighlight` 开头那道 UIstate 闸（`:19-24`）：
            /// **`UIstate ∉ {inactive 0, finishBattleSequence 7, postBattle 8, mulligan 11, mulliganFinished 12}`**。
            /// ⚠️ 它**不是**「UIstate == normalBattle」—— `playingMinion 3` / `minionAttacking 6` /
            /// `choosingUnitTarget 9` / `targetedActiveAbility 10` / `choosingAbilityTarget 13` 都**允许**悬停高亮。
            /// 映射可由 `BattleDriver.BattleUiIsNormalBattle` 那一套状态位**反推**（口径见该属性的 doc）。</summary>
            public bool HoverAllowed;
            /// <summary>原版 `OnTouchEnter.c:21` 与 `OnTouchUpAsButton.c:17` 那道 `UIstate == choosingResolutionCard(14)`
            /// ⇒ 只打一句 Log 后整条早退。⚠️ **`14` 不在 `HoverAllowed` 的封锁集里**，所以这两条闸必须分开传
            /// （少了这一条，选牌窗开着时悬停照样会改高亮态 = 多出来的行为）。</summary>
            public bool ChoosingResolutionCard;
            /// <summary>原版 `CardScript.OnTouchEnter.c:12-14` 的第一道闸：`Input.GetMouseButton(1) || touchCount &gt;= 2`。
            /// 我们现成的等价物 = `CardInteraction.IsRightButtonOrTwoFingerTouch()`（那条已是 A 档）。</summary>
            public bool RightButtonOrTwoFingers;
            /// <summary>原版 `TryCardHighlight` 里那个 `formUnityAbility` 的 UIstate 那一半
            /// （`UIstate ∈ {choosingUnitTarget 9, targetedActiveAbility 10}`，`:62`）。
            /// 另一半（被指的那张卡 `currentAttackType == Active(4)`）由状态机自己补 —— 见 `TryCardHighlight`。</summary>
            public bool ChoosingUnitOrAbilityTarget;
            /// <summary>原版 `BattleManager.mulliganManager.MulliganElementsAreVisible()`（`+0x1E8`，`:75`）。
            /// 换牌元素**看得见**时，开大卡窗那个参数是 `0`（`:76` 传的是 `cVar2 == 0` 的取反结果）。</summary>
            public bool MulliganElementsVisible;
            /// <summary>原版 `BattleManager.waitingToResolveUseWaystoneFlag`（`+0x419`，`TryCardHighlight.c:80`）。</summary>
            public bool WaystonePending;
            /// <summary>原版 `BattleManager.cardTracked`（`+0x330`）：指针正悬着的**那张卡**（`TryCardHighlight.c:57`）。
            /// `null` = 没有（原版那时直接不走预览那段）。</summary>
            public CardView TrackedCard;
            /// <summary>原版 `Time.timeSinceLevelLoad − CardScript.timeSinceLastClickOnCard(+0x320)`，
            /// 只用在与 `THRESHOLD_FOR_OPENING_CARD_DISPLAY`（**0.2 s**）比的那两档（`OnTouchUpAsButton.c:37-45`）。</summary>
            public float PressSeconds;
        }

        /// <summary>
        /// 原版 `CardHighlight` 组件的**状态机**（一台机器 = 一张卡一份）。
        ///
        /// <para>🔴 **`State` 不另存一份** —— 它**派生自** `CardView.State`（我们唯一的渲染真值）。
        /// 理由：`CardView.SetHighlight` 有 20+ 个既有调用点（`SyncBoard` / `HighlightTargets` /
        /// `AwaitingPlayTarget` 的选目标 …），再存一份状态必然两处打架，而「两处写同一条规则 = 迟早不一致」
        /// 是本工程红线。派生之后：外部把视图刷成 `ValidTarget` ⇒ 本机立刻知道「现在是 3」，
        /// 于是 `TryCardHighlight` 走「3 ⇒ 4」那一支（**这正是原版的行为**）。</para>
        ///
        /// <para>⚠️ 唯一存下来的一档是 `displayingActiveAbility(5)`：我们的视图枚举里没有它
        /// （`CardView.State` 表达不了），所以用一个布尔记住。它由
        /// `CardScript.DisplayingActiveAbility()` → `ChangeState(5)` 进入（`DisplayingActiveAbility.c:6`），
        /// 由 `StopDisplayingActiveAbility()` → `ChangeState(0)` 退出（`StopDisplayingActiveAbility.c:6`）。
        /// **本批没有接线方**（那两条是技能展示那一族，另有人负责）⇒ 如实记，不冒充已接通。</para>
        /// </summary>
        public sealed class CardBoardHighlight
        {
            readonly CardView _view;

            internal CardBoardHighlight(CardView view) { _view = view; }

            /// <summary>这张卡的视图（= 原版 `CardHighlight` 组件所在的那个 `CardScript` 的视图等价物）。</summary>
            public CardView View { get { return _view; } }

            // ---------------------------------------------------------------
            //  原版同一台机器上的字段（我们这边缺的那几个由宿主喂）
            // ---------------------------------------------------------------

            /// <summary>原版 `CardHighlight.state`（`+0x24`）—— **派生**（见类 doc），不是独立字段。</summary>
            public CardBoardHighlightState State
            {
                get
                {
                    if (_displayingActiveAbility) return CardBoardHighlightState.displayingActiveAbility;
                    switch (_view.State)
                    {
                        case CardHighlightState.ValidTarget:
                            // 原版同一个「潜在目标」色有手牌/场上两档（state 1 / state 3），
                            // 我们靠「这张卡现在按哪个形态组装」区分（`CardFace.Board` = 场上）。
                            return _view.Face == CardFace.Board
                                 ? CardBoardHighlightState.potentialTargetInBoard
                                 : CardBoardHighlightState.potentialTargetInHand;
                        case CardHighlightState.Selected: return CardBoardHighlightState.selected;
                        case CardHighlightState.SelectedTargetInBoard: return CardBoardHighlightState.selectedTargetInBoard;
                        // `Playable` / `Unplayable` / `Hover` 是**我们自己加的**三档，原版这台机器没有对应状态
                        // ⇒ 一律报 `regular`（= 原版「没有高亮态」）。⚠️ 它们只出现在手牌/收藏卡格上，
                        // 棋盘卡不会走到这三档（见 `SetHighlight` 的调用点）。
                        default: return CardBoardHighlightState.regular;
                    }
                }
            }

            bool _displayingActiveAbility;

            /// <summary>原版 `CardScript.displayingCardFlag`（`+0x24A`，**私有**）：按下去时开的大卡窗；指针离开时要由它负责关（`OnTouchExit.c:15-20`）；`OnTouchDown` 清它、`OnTouchUpAsButton` 置它。⚠️ **驱动层已经有它的等价物**（`BattleDriver._cardWinSource` + `CardDisplayWindow.Visible`，见 `TickCardWinPointerExit`）⇒ 接线时⛔ **别两处都写**（二选一：要么宿主继续用自己那两个状态位、本字段不赋值；要么把 `DisplayCard` 钩子接到 `BattleDriver` 的开关窗并让它只读本字段）。</summary>
            public bool DisplayingCardFlag { get; set; }

            /// <summary>原版 `CardScript.IsAnimatingCombat`（`+0x248`）。只在 `OnTouchExit` 一处被读
            /// （`:27`：演出中关预览时**不改攻击类型底光** —— 原版那里传的是 `IsAnimatingCombat == 0`）。</summary>
            public bool IsAnimatingCombat { get; set; }

            /// <summary>原版 `EntityScript.isRemnant`（`+0x65`）：残骸体走「展示原卡」那一支。</summary>
            public bool IsRemnant { get; set; }

            /// <summary>原版 `EntityScript.currentAttackType`（`+0x120`）。只在「收预览」那一支被读
            /// （`:163-168`：`isPlayer ? 自己的 attackType : None`）。</summary>
            public AttackTypes AttackTypeSelf { get; set; }

            /// <summary>原版 `EntityScript.isPlayer`（`+0x40`）。同上。</summary>
            public bool IsPlayerSide { get; set; }

            /// <summary>原版 `BattleManager.IsPlayerTurn() == 自己的 isPlayer`（`TryCardHighlight.c:39-42`）:
            /// 真 ⇒ 残骸体**不**展示原卡（轮到它自己时看的是残骸本体）。</summary>
            public bool OwnerTurnNow { get; set; }

            // ---------------------------------------------------------------
            //  宿主钩子（原版这些动作落在 BattleManager / BattleCardUI 上）
            // ---------------------------------------------------------------

            /// <summary>原版 `ToggleCombatPreviewHighlight` 里那四句伤害算式 + 判死（见 <see cref="CombatPreviewInfo"/>）。</summary>
            public CombatPreviewProbe PreviewProbe { get; set; }

            /// <summary>原版 `BattleCardUI.ShowOriginalRemnantCard(bool)`（`+0x290`）。</summary>
            public System.Action<bool> ShowOriginalRemnantCard { get; set; }

            /// <summary>原版 `BattleManager.DisplayCard(card, show, …, mulliganException)` 的**开/关大卡窗**那一半
            /// （`:19` 关 / `:47` 开 / `:58` 开 / `:76` 开）。⚠️ 我们现成的是 `BattleDriver.TapBoardCard` +
            /// `TickCardWinPointerExit`，接线方式见报告 §七。</summary>
            public System.Action<bool> DisplayCard { get; set; }

            /// <summary>原版 `BattleManager.TryUsingWaystone(card)` 那一支（`OnTouchUpAsButton.c:81`）。</summary>
            public System.Func<bool> CollectWaystone { get; set; }

            bool _warnedProbe, _warnedRemnant, _warnedDisplay, _warnedWaystone;

            void WarnOnce(ref bool flag, string what)
            {
                if (flag) return;
                flag = true;
                Debug.LogWarning("[CardHighlight] `CardBoardHighlight` 的宿主钩子 `" + what
                               + "` 没接线 ⇒ 原版这一段**没有执行**（出声，不静默）");
            }

            // ---------------------------------------------------------------
            //  原版 CardHighlight 的方法
            // ---------------------------------------------------------------

            /// <summary>原版 `CardHighlight.ChangeState`（`.c:1-77`）。
            /// <para>逐句：`:14-16` 同态早退 → `:17 ToggleFrames(true)` → `:18` 写 State →
            /// `:19-76` 按 case 取色/时长/缩放。</para>
            /// <para>逐 case 的落地（**判据是反编译那几行，不是我们的表**）：</para>
            /// · `regular 0`（`:20-29`）：色 `+0xC0` = `RegularColor`（alpha **0**）、时长 `+0x20` = 0.1、
            ///   **且** `ChangeTargetScale(initialScale, 0.1)` ⇒ 缩放回 1（带补间）
            /// · `potentialTargetInHand 1`（`:30-36`）：色 `+0x80` = 绿、时长 0.1、**不缩放**
            /// · `selected 2`（`:37-42`）：色 `+0x90` = 白、时长 **0**（`uVar2 = 0`，`:69`）、**不缩放**
            /// · `potentialTargetInBoard 3`（`:43-48` →`:54-61`）：色 `+0x80` = 绿、时长 0.1、
            ///   `initialScale × ScaleFactor(+0x30)` = **×1.05**
            /// · `selectedTargetInBoard 4`（`:49-61`）：色 `+0xB0` = 橙 #FF8400、时长 0.1、**×1.05**
            /// · `displayingActiveAbility 5`（`:63-70`）：色 `+0xA0` = 黄 (1,1,0)、时长 **0**、**不缩放**
            /// <returns>真 = 状态真的变了（同态返回假，**且一个像素都不改** —— 原版那句早退就是这个意思）。</returns>
            public bool ChangeState(CardBoardHighlightState newState)
            {
                if (newState == State) return false;         // 原版 :14-16
                // 原版 :17 `ToggleFrames(true)` —— 由 `CardView.SetHighlight` 里的 `_rim.enabled = true` 承担
                _displayingActiveAbility = newState == CardBoardHighlightState.displayingActiveAbility;
                _view.ApplyBoardHighlightState(newState);
                return true;
            }

            /// <summary>原版 6 档 → 我们**视图**枚举的映射（**只有这一处**；`CardView.ApplyBoardHighlightState` 读它）。
            /// <para>两处不是一一对应，理由都在映射里：</para>
            /// · `1` 与 `3` 都落 `ValidTarget` —— 原版是**同一个绿色**、只差「手牌不缩放 / 场上 ×1.05」，
            ///   而缩放那一半我们早就按「这张卡按哪个形态组装」分好了（`CardHighlight.ScaleOf` 的 `onBoard` 参数）
            /// · `5` `displayingActiveAbility` → `Normal` 是**占位**：它**不许**走 `SetHighlight`（那会把环收掉），
            ///   真正那一支在 `CardView.ApplyBoardHighlightState` 里单独写（黄环 + 瞬切 + 不缩放）
            /// <para>⚠️ 我们的 `Playable` / `Unplayable` / `Hover` 三档**没有**原版来源，别在这里给它们编一个反向映射。</para></summary>
            public static CardHighlightState MapToView(CardBoardHighlightState s)
            {
                switch (s)
                {
                    case CardBoardHighlightState.selected: return CardHighlightState.Selected;
                    case CardBoardHighlightState.potentialTargetInHand:
                    case CardBoardHighlightState.potentialTargetInBoard: return CardHighlightState.ValidTarget;
                    case CardBoardHighlightState.selectedTargetInBoard: return CardHighlightState.SelectedTargetInBoard;
                    default: return CardHighlightState.Normal;   // regular（`displayingActiveAbility` 见上面那条说明）
                }
            }

            /// <summary>原版 `CardHighlight.Start`（`.c:1-29`）：捕获 `initialScale`（我们由 `CardView` 自己的
            /// 基准缩放承担）→ `ToggleFrames(true)` → `ChangeFrameColor(RegularColor, **0**)`（**瞬切**，
            /// 不是走 `ChangeState` 的早退）→ `ToggleCombatPreviewHighlight(null, 0, 0, 1, 0)`。</summary>
            public void OnStart()
            {
                _displayingActiveAbility = false;
                _view.ForceRegularFrame();
                SetCombatHighlights();
            }

            /// <summary>原版 `CardHighlight.SetCombatHighlights`（`.c:1-11`）：**只做一件事**
            /// —— `minionWillDieObject.SetActive(false)`。</summary>
            public void SetCombatHighlights() { _view.SetWillDie(false); }

            /// <summary>现在点亮的是哪一档攻击类型底光（自检读口 = 原版 `HighlightAttackTypes` 的入参）。
            /// 初值 `None` = 原版 `Start` 之后「两颗都熄」。</summary>
            public AttackTypes HighlightedAttackType { get; private set; }

            /// <summary>原版 `CardHighlight.HighlightAttackTypes(int)`（`.c:1-28`）：遍历这台机器上的
            /// `highlightAttackTypes[]`，逐个 `HighlightAttackType.HighlightIfNeeded(attackType)`。
            /// 我们这侧那两颗实例 = `CardView._stats[StatMelee] / [StatRanged]` 的底光 + 数值放大，
            /// ✅ 已由 `CardView.SetOwnAttackType` 逐句实现（判据 → `CardView.cs:8940-9110` 那一节）。</summary>
            public void HighlightAttackTypes(AttackTypes attackType)
            {
                HighlightedAttackType = attackType;
                _view.SetOwnAttackType(attackType);
            }

            /// <summary>原版 `CardHighlight.ToggleCombatPreviewHighlight(otherCard, isAttacker, changeAttackType, formUnityAbility)`
            /// （`.c:1-178`）—— 伤害预览那一层。
            ///
            /// <para>控制流（**照反编译的标签走**）：</para>
            /// ① `:26-40` 两台对象都在 ⇒ ②；任一为空 ⇒ 走 `:145-173` 的「收」支
            /// ② `:41-93` 按 `other` 是否计策卡（`cardType == Tactic(20)`）/ `formUnityAbility` 选算式算伤害条目；
            ///    结果喂给 `hasCurrentTrait(0x4b0)` 与 `EnoughPendingDamageToDieWithDamageValues` ⇒ `willDie`
            /// ③ `:104-120` `willDie` ⇒ 播 `minionWillDieAnimation` + `minionWillDieObject.SetActive(IsAnimatingCombat==0)`；
            ///    否则 `:115-120` `SetActive(false)`
            /// ④ `:124-127` 自己 `isRemnant` ⇒ `CardUI.ShowOriginalRemnantCard(true)` 后 **return**
            /// ⑤ `:131-134` `other` 是计策卡 ⇒ **return**（不点亮底光）
            /// ⑥ `:135-140` `!changeAttackType || formUnityAbility` ⇒ **return**
            /// ⑦ `:141-142 → :169-170` `HighlightAttackTypes(**other 的** currentAttackType)`
            /// <para>「收」支（`:145-173`）：`ShowOriginalRemnantCard(false)`（若残骸）→ `SetActive(false)` →
            /// `HighlightAttackTypes(isPlayer ? 自己的 attackType : None)`。</para>
            ///
            /// <para>⚠️ **算不算得出「会不会死」靠宿主探针**（见 <see cref="CombatPreviewInfo"/>）——
            /// 探针没接时本方法把预览收掉并**出声一次**，⛔ 不假装算过。</para></summary>
            public void ToggleCombatPreviewHighlight(CardView other, bool isAttacker,
                                                     bool changeAttackType = true, bool formUnityAbility = false)
            {
                if (other != null)
                {
                    var info = new CombatPreviewInfo();
                    bool ok = false;
                    if (PreviewProbe != null)
                    {
                        info = PreviewProbe(other, isAttacker, changeAttackType, formUnityAbility);
                        ok = info.Valid;
                    }
                    if (!ok)
                    {
                        if (!_warnedProbe)
                        {
                            _warnedProbe = true;
                            Debug.LogWarning("[CardHighlight] 伤害预览探针（`CardBoardHighlight.PreviewProbe`）没接线"
                                           + " ⇒ 原版 `ToggleCombatPreviewHighlight:41-93`（计策卡算式 / EnemyReturnsAttack /"
                                           + " 星镖 / 标记光 / 判死）这一段本次**没有执行**；「会死」图标与攻击类型底光都不会亮"
                                           + "（出声，不静默）");
                        }
                        HideCombatPreview();
                        return;
                    }
                    // ③ 「会不会死」那一层（原版 :104-120）
                    _view.SetWillDie(info.WillDie);
                    // ④ 残骸体 ⇒ 展示原卡（原版 :124-127）
                    if (IsRemnant)
                    {
                        if (ShowOriginalRemnantCard != null) ShowOriginalRemnantCard(true);
                        else WarnOnce(ref _warnedRemnant, "ShowOriginalRemnantCard");
                        return;
                    }
                    // ⑤ 计策卡不点亮底光（原版 :131-134）
                    if (info.OtherIsTactic) return;
                    // ⑥ 原版 :135-140
                    if (!changeAttackType) return;
                    if (formUnityAbility) return;
                    // ⑦ 原版 :141-142 → :169-170（**other 的** attackType）
                    HighlightAttackTypes((AttackTypes)info.AttackTypeToHighlight);
                    return;
                }
                // 「收」支（原版 :145-173）
                HideCombatPreview();
                HighlightAttackTypes(IsPlayerSide ? AttackTypeSelf : AttackTypes.None);
            }

            void HideCombatPreview()
            {
                if (IsRemnant && ShowOriginalRemnantCard != null) ShowOriginalRemnantCard(false);  // 原版 :149
                _view.SetWillDie(false);                                                           // 原版 :152
            }

            // ---------------------------------------------------------------
            //  四个输入入口（原版 `CardScript` 上的四个方法）—— 本状态机**承接的那一半**
            // ---------------------------------------------------------------
            //
            //  🔴 **口径**：这四个方法在原版里各含一大段**不属于高亮**的行为（显牌库数 / 开大卡窗 /
            //     停「手牌悬停」/ 走灵魂石 …）。那几段在我们工程里**已经有落点**（`CardHoldPeek` /
            //     `TapBoardCard` / `CardInteraction.ApplyShowInHand`），**不在本类里重写**；
            //     本类只承接「高亮态 + displayingCardFlag 协议 + 伤害预览」这一半。
            //     ⇒ 每个入口方法都返回「这一半我处理了吗」，其余由宿主那一侧照旧。

            /// <summary>原版 `CardScript.TryCardHighlight`（`.c:1-99`）—— **本状态机的正门**。
            /// <para>逐句：`:19-24` UIstate 闸（`{0,7,8,11,12}` ⇒ 直接 return）→
            /// `:27-48` 自己是 `regular` ⇒ `ChangeState(selected)` + 清 `displayingCardFlag` +
            /// （残骸那支：`IsPlayerTurn() == isPlayer` ⇒ return，否则 `ShowOriginalRemnantCard(true)`）→
            /// `:49-93` 自己是 `potentialTargetInBoard(3)` ⇒ `ChangeState(selectedTargetInBoard)` +
            /// `ToggleCombatPreviewHighlight(BM.cardTracked, isAttacker:false, changeAttackType:true,
            /// formUnityAbility: (tracked.attackType == Active(4) || UIstate ∈ {9,10}))`；
            /// 之后 `attackType == Active(4)` ⇒ return、`UIstate ∈ {9,10}` ⇒ return、
            /// `waitingToResolveUseWaystoneFlag` ⇒ return，否则**再给 `cardTracked` 那张**来一次
            /// `ToggleCombatPreviewHighlight(this, isAttacker:true, changeAttackType:false)`。</para>
            /// <para>⚠️ 两张卡**互相**开预览（`isAttacker` 一假一真）是原版的行为，别只做一半。</para>
            /// <returns>真 = 这一次「悬停」被高亮状态机吃掉了。</returns></summary>
            public bool TryCardHighlight(TouchInput t)
            {
                if (!t.HoverAllowed) return false;                      // 原版 :19-24
                if (State == CardBoardHighlightState.regular)           // 原版 :27-48
                {
                    ChangeState(CardBoardHighlightState.selected);
                    DisplayingCardFlag = false;
                    if (IsRemnant)
                    {
                        if (OwnerTurnNow) return true;                  // 原版 :39-42
                        if (ShowOriginalRemnantCard != null) ShowOriginalRemnantCard(true);
                        else WarnOnce(ref _warnedRemnant, "ShowOriginalRemnantCard");
                    }
                    return true;
                }
                if (State != CardBoardHighlightState.potentialTargetInBoard) return false;  // 原版 :50-52

                ChangeState(CardBoardHighlightState.selectedTargetInBoard);                  // 原版 :53
                var tracked = t.TrackedCard;
                if (tracked == null) return true;                        // 原版 :54-57 只在 BM.cardTracked 存在时才走预览
                int trackedAttack = (int)tracked.OwnAttackType;           // 原版 `+0x120`
                bool unityAbility = trackedAttack == (int)AttackTypes.Active || t.ChoosingUnitOrAbilityTarget;
                ToggleCombatPreviewHighlight(tracked, isAttacker: false,                   // 原版 :60-62
                                             changeAttackType: true, formUnityAbility: unityAbility);
                if (trackedAttack == (int)AttackTypes.Active) return true;        // 原版 :63-65
                if (t.ChoosingUnitOrAbilityTarget) return true;                            // 原版 :66-68
                if (t.WaystonePending) return true;                                        // 原版 :80-82
                tracked.BoardHighlight.ToggleCombatPreviewHighlight(_view, isAttacker: true,  // 原版 :85
                                                                   changeAttackType: false, formUnityAbility: false);
                return true;
            }

            /// <summary>原版 `CardScript.OnTouchDown`（`.c:1-97`）—— **只承接 `cardState == inPlay(2)` 那一支**
            /// （`:61-70`：`ChangeState(selected)`（仅当现在是 `regular`）+ **无条件**清 `displayingCardFlag`）。
            /// <para>其余各档（`0/1` 显牌库数 · `3/8/13-15/17` 早退 · `4-7` 只 LogWarning）**不在本类**：
            /// 牌库/手牌数那一档的落点 = `BattleDriver.CardHoldPeek`，按下时刻的写入 = `CardInteraction.BeginPressCandidate`
            /// （两条都已 A 档，见账 `战斗逻辑驱动.tsv`）。</para>
            /// <returns>真 = 这一半处理了。</returns></summary>
            public bool OnTouchDown(TouchInput t)
            {
                if (t.CardState != CardStateId.inPlay) return false;
                if (State == CardBoardHighlightState.regular) ChangeState(CardBoardHighlightState.selected);
                DisplayingCardFlag = false;
                return true;
            }

            /// <summary>原版 `CardScript.OnTouchEnter`（`.c:1-40`）—— 只承接 `cardState == inPlay(2)` 那一支
            /// （`:34-37` ⇒ `TryCardHighlight`）。
            /// <para>① `:12-14` 右键/双指闸（我们那条已 A：`CardInteraction.IsRightButtonOrTwoFingerTouch`）；
            /// ② `:21-24` `UIstate == choosingResolutionCard(14)` ⇒ 只 Log；
            /// ③ `:25-33` `cardState == inHand(1)` 那一支（`CanDisplayHandCard` ⇒ `ShowCardInHand`）**归手牌**
            /// —— 落点 = `CardInteraction.UpdateHover`（已 A）。</para>
            /// <returns>真 = 高亮这一半处理了。</returns></summary>
            public bool OnTouchEnter(TouchInput t)
            {
                if (t.RightButtonOrTwoFingers) return false;   // 原版 :12-14
                if (t.ChoosingResolutionCard) return false;    // 原版 :21-24（只打一句 Log）
                if (t.CardState != CardStateId.inPlay) return false;   // 手牌那支归 CardInteraction
                return TryCardHighlight(t);                    // 原版 :34-37
            }

            /// <summary>原版 `CardScript.OnTouchExit`（`.c:1-57`）—— 承接 ①③④ 三段：
            /// <para>① `:15-20` `displayingCardFlag != 0` ⇒ 清零 + `DisplayCard(false)`（关大卡窗）；
            /// ② `:21-23` `isPlayer &amp;&amp; cardState == inHandShowing(8)` ⇒ `StopShowingCardInHand(true)`
            /// —— **归手牌**（落点 = `CardInteraction.ApplyShowInHand`，已 A）；
            /// ③ `:24-39` 预览全收：先自己那张（`isAttacker:false`, `changeAttackType = !IsAnimatingCombat`），
            /// 再**对方那张**（`BM.cardTracked` 的，`isAttacker:true, changeAttackType:true`）；
            /// ④ `:40-50` 自己 `state == selected(2)` ⇒ `ChangeState(regular)`、
            /// `state == selectedTargetInBoard(4)` ⇒ `ChangeState(potentialTargetInBoard)`。</para>
            /// <para>⚠️ 原版这一段**通篇没有闸**（离开就必须收）—— 别在调用点加「只在某种 UIstate 下才收」。</para>
            /// </summary>
            public void OnTouchExit(TouchInput t)
            {
                if (DisplayingCardFlag)                                     // ① 原版 :15-20
                {
                    DisplayingCardFlag = false;
                    if (DisplayCard != null) DisplayCard(false);
                    else WarnOnce(ref _warnedDisplay, "DisplayCard");
                }
                // ③ 自己那张先收（原版 :26-27：`ToggleCombatPreviewHighlight(ch, 0, 0, IsAnimatingCombat==0, 0)`）
                ToggleCombatPreviewHighlight(null, isAttacker: false,
                                             changeAttackType: !IsAnimatingCombat, formUnityAbility: false);
                // ③′ 对面那张也收（原版 :28-39）
                var tracked = t.TrackedCard;
                if (tracked != null)
                    tracked.BoardHighlight.ToggleCombatPreviewHighlight(null, isAttacker: true,
                                                                       changeAttackType: true, formUnityAbility: false);
                // ④ 原版 :40-50
                if (State == CardBoardHighlightState.selected) ChangeState(CardBoardHighlightState.regular);
                else if (State == CardBoardHighlightState.selectedTargetInBoard)
                    ChangeState(CardBoardHighlightState.potentialTargetInBoard);
            }

            /// <summary>原版 `CardScript.OnTouchUpAsButton`（`.c:1-87`）—— 承接 `displayingCardFlag` 协议那一半。
            /// <para>逐档（`:21-83`）：</para>
            /// · `{inHand 1, inHandShowing 8}`（`:22-51`）：`elapsed = 现在 − +0x320`，
            ///   `0.2 &lt;= elapsed` ⇒ **不开窗**、否则 `DisplayCard(1, 0, 0)`；`state == 8` ⇒ 收「手牌悬停」。
            ///   **归手牌那一侧**（`CardInteraction.EndPressCandidate` / `Release`，已 A）—— 本类**返回假**。
            ///   ⚠️ 非我方牌另有一道守卫：`rawCardData != 0 && rawCardData.spellType(+0x90) == 0xE6(Sabotage)`，
            ///   否则直接 return（`+0x28` = `rawCardData`，**本轮已锚定**）—— 见 <see cref="EnemyHandCardClickable"/>。
            /// · `{inMulliganSelected 13, inMulliganDiscarded 14}`（`:52-61`）：`displayingCardFlag = 1`，
            ///   **非我方则 return**；否则 `DisplayCard(1, 1, 0)`（换牌档那一支归 `MulliganPanel`）。
            /// · `{inPlay 2, inPlayAttacking 3, inPlayAimingAttack 17}`（`:63-83`）：`IsAeldariRemnant` ⇒
            ///   `TryUsingWaystone`；否则 `displayingCardFlag = 1` + `DisplayCard(1, !MulliganElementsAreVisible, 0)`。
            ///   **棋盘段没有敌我判断**（原版全文如此）。
            /// · 其余（`0/4-7/15/16`）⇒ return。</para>
            /// <returns>真 = 这一档由 `displayingCardFlag` 协议（本类）处理；假 = 归宿主那一侧。</returns>
            /// </summary>
            public bool OnTouchUpAsButton(TouchInput t)
            {
                if (t.ChoosingResolutionCard) return false;              // 原版 :17-20（LogWarning + return）
                switch (t.CardState)
                {
                    case CardStateId.inHand:                             // 原版 :22-51 —— 归手牌
                    case CardStateId.inHandShowing:
                        return false;
                    case CardStateId.inMulliganSelected:                 // 原版 :52-61
                    case CardStateId.inMulliganDiscarded:
                        DisplayingCardFlag = true;
                        if (!t.IsPlayer) return true;
                        if (DisplayCard != null) DisplayCard(true);
                        else WarnOnce(ref _warnedDisplay, "DisplayCard");
                        return true;
                    case CardStateId.inPlay:                             // 原版 :63-83
                    case CardStateId.inPlayAttacking:
                    case CardStateId.inPlayAimingAttack:
                        if (IsRemnant)                                   // 原版 :70-83（`IsAeldariRemnant`）
                        {
                            if (CollectWaystone != null) { CollectWaystone(); return true; }
                            WarnOnce(ref _warnedWaystone, "CollectWaystone");
                            return true;
                        }
                        DisplayingCardFlag = true;
                        if (DisplayCard != null) DisplayCard(!t.MulliganElementsVisible);
                        else WarnOnce(ref _warnedDisplay, "DisplayCard");
                        return true;
                    default:
                        return false;                                    // 原版 :24-26 / :63-65
                }
            }

            /// <summary>原版 `OnTouchUpAsButton.c:31-36` 那道守卫：**非我方的手牌卡**只有在
            /// `rawCardData.spellType == SpellType.Sabotage(0xE6 = 230)` 时才能点开
            /// （`+0x28` = `rawCardData`、`+0x90` = `spellType`，`dump.cs:23244+`；`0xE6` 见 `SpellType` 枚举
            /// `Sabotage = 230`）。`hasRawCard == false` = 原版那句 `+0x28 == 0` ⇒ 也不点。
            /// <para>⚠️ 我们 `CardDef` 侧没有 `spellType` 这个字段的等价物 —— 已有的判据是
            /// `CreatePool.MatchesKind(card, "sabotage")`（兵种那一维，`CardHighlight.OutlineOf` 用的同一个）。
            /// 宿主接线时按那个喂 `sabotageKind`（⛔ 别再造一份判据）。</para></summary>
            public static bool EnemyHandCardClickable(bool isPlayer, bool hasRawCard, bool sabotageKind)
            {
                if (isPlayer) return true;
                if (!hasRawCard) return false;
                return sabotageKind;
            }

            /// <summary>自检 / 报告用的单行读数（⛔ 别在断言里自己拼字符串，读这里）。</summary>
            public string Describe()
            {
                return "state=" + (int)State + "(" + State + ")"
                     + " view=" + _view.State
                     + " face=" + _view.Face
                     + " rim=" + (_view.RimVisible ? "on" : "off")
                     + " rimRGB=" + (int)(_view.RimTargetForTest.r * 255) + "/"
                                    + (int)(_view.RimTargetForTest.g * 255) + "/"
                                    + (int)(_view.RimTargetForTest.b * 255)
                     + " willDie=" + (_view.WillDieVisible ? "on" : "off")
                     + " atkGlow=" + HighlightedAttackType
                     + " displayingCard=" + (DisplayingCardFlag ? 1 : 0)
                     + " scale=" + _view.HighlightScaleMulForTest.ToString("0.###");
            }

            // ---------------------------------------------------------------
            //  原版**另一个组件**：`ActiveWarlordAbility`（同一张卡上、挂在子节点）
            // ---------------------------------------------------------------
            ActiveWarlordAbilityTouch _warlordAbility;

            /// <summary>原版 `ActiveWarlordAbility`（卡预制体**子节点**上那颗组件，`Start()` 里
            /// `GetComponentInParent&lt;CardScript&gt;()` 反查父卡）—— 一台机器 = 一张卡一份。
            /// <para>⛔ **别和 <see cref="CardBoardHighlight"/> 合并**：原版是**两个类、两颗组件** ——
            /// 高亮 6 态那台机器（本类）与「按住 0.3 s 弹卡 / 轻点填技能面板并试放主动技」（那颗）
            /// 互不引用，判据也各自独立。</para>
            /// <para>⚠️ 惰性建（与 `CardView.BoardHighlight` 同一个套路）：没人调的卡不占内存。</para></summary>
            public ActiveWarlordAbilityTouch WarlordAbility
            {
                get
                {
                    if (_warlordAbility == null) _warlordAbility = new ActiveWarlordAbilityTouch();
                    return _warlordAbility;
                }
            }
        }

        // ==================================================================
        //  原版 `ActiveWarlordAbility` —— 「按住 0.3 s 弹卡面 / 轻点填技能面板并试放主动技」
        //  2026-10-26（执行代理 `W3K04` · 账 `战斗界面_HUD` 第 682/683/684/686/687 行）
        // ==================================================================
        //
        //  🔴🔴 **本文件里【最容易做反】的一处** —— 把这条和 `CardScript` 那条分开看：
        //    · `CardScript.OnTouchUpAsButton`（`decomp_full/CardScript__OnTouchUpAsButton.c:37-44`）的
        //      时长门 = **0.2 s**（`.rdata DAT_1834b2bb0` 实读 0.2f），语义是
        //      「**长按反而【不】开窗**」—— 那一条归**手牌**（`Hand/CardInteraction.cs` 的
        //      `LongPressNoDisplaySeconds` / `PressShortEnough`），**我们早就实现对了，⛔ 别改它**。
        //    · `ActiveWarlordAbility.Update`（`.c:1-26`）的时长门 = **0.3 s**（`cardShowTime`，字段
        //      偏移 `+0x40`），语义是「**按住【就】弹窗**」—— 就是下面这台机器。
        //    ⇒ 两条门**时长不同、方向相反、挂在不同类上**；账里原来那句「阈值 0.3 VS 0.2、语义相反」
        //      是**拿两个类比的**（判据代理 `K1P0` 2026-10-26 专门核过，见 `判据/B9_P0剩余C_派活包.md` §6·1）。
        //
        //  判据（🥇 全量反编译 `d:/2/tools/decomp_full/`，逐份亲读；字段偏移 =
        //        `d:/2/tools/il2cpp_out/dump.cs:38882-38921`）：
        //    · `.ctor`（`ActiveWarlordAbility__.ctor.c:1-8`）：`+0x40 = 0x3e99999a` = **0.3f**
        //      （⇒ `cardShowTime` 不是「字段默认 0」，而是构造里就写死的 0.3）
        //    · `__OnTouchDown.c:1-14`：`+0x3c = Time.time`；`+0x38 = 1` —— **全文就这两句**
        //    · `__OnTouchExit.c:1-7`：`+0x38 = 0` —— **全文一句**
        //    · `__Update.c:1-26`：flag 真 ⇒ `elapsed = Time.time − +0x3c`；
        //        `cardShowTime <= elapsed && elapsed != cardShowTime`（浮点上 = **严格大于** 0.3）
        //        ⇒ 对 `+0x38` 写**两字节** `0x100`（小端 = `+0x38 inPlayClickFlag=0` +
        //        `+0x39 displayingCardFlag=1`）⇒ `parentCard(+0x20).ForceDisplayCard()`
        //    · `__OnTouchUpAsButton.c:1-72`：
        //        ① `parentCard(+0x20) == null` ⇒ 落 NRE 桩；`parentCard.isPlayer(+0x40) == 0` ⇒ **return**
        //        ② `manager.activeSkillDesc(+0x168).SetupActiveSkill(parentCard)`（**先填面板**）
        //        ③ `manager.TryUsingActiveAbilityOfCard(parentCard, forceSelectCard: **1**)`
        //        ④ ③ 为真 **且** `PressedBtn(+0x30) != null` ⇒ `PressedBtn.SetActive(true)` +
        //           `DOVirtual.DelayedCall(0.4f, b__12_0)`；`PressedBtn == null` ⇒ **什么都不做**
        //    · `___OnTouchUpAsButton_b__12_0.c:1-9`：`PressedBtn.SetActive(false)`
        //    · `CardScript__ForceDisplayCard.c:1-14`：`CardScript.displayingCardFlag(+0x24A) = 1` +
        //        `BattleManager.DisplayCard(card, show:1, mulliganException:0)`
        //
        //  🔴 **顺读出来的两条（本轮新发现，都会决定接线怎么做对）**：
        //   ① `BattleManager__DisplayCard.c`：**同一张卡重复开窗是幂等的** ——
        //      `card == +0x588`（当前那面窗的卡）⇒ 只把 `+0x588` 清成 null 后 return；
        //      `card != +0x588` 且窗口 `activeSelf == show` ⇒ **直接 return**。
        //      ⇒ 原版「按住 0.3 s 已弹窗、再松手」**不会再开关一次**（松手那句 `DisplayCard` 被吃掉）。
        //      ⇒ 我们这边 `CardDisplayWindow.Toggle` 是**真 toggle** ⇒ 宿主必须在松手那一支显式挡一次
        //      （读 <see cref="ActiveWarlordAbilityTouch.HoldDisplayedThisPress"/>），⛔ 别指望它自己幂等。
        //   ② `BattleManager__TryUsingActiveAbilityOfCard.c`：**返回值决定 PressedBtn 亮不亮** ——
        //      ① `CanUseActiveAbility(card, isPlayer:1) == 0` ⇒ Log + **return 0**
        //      ② 技能**不需要选目标** ⇒ `+0x418 = 1` + `ExecuteActiveAbility()` ⇒ **return 1**
        //      ③ 需要选目标：无合法目标 ⇒ `NotifyCantDoAction` + **return 0**；
        //         有合法目标 ⇒（`forceSelectCard` 时记 `+0x410`）+ `ShowActingLight` +
        //         `StartChoosingAbilityTargetByClick` ⇒ **return 0**
        //      ⇒ 0.4 s 按压反馈 = 「**这一下就把它放掉了**（技能不需要选目标）」，
        //        **不是**「进入了选目标」。⛔ 宿主给 `TryUseActiveAbility` 钩子时必须照这个返回语义。
        //
        //  ⚠️ **拿不到的判据（如实记，⛔ 别自己挑一个图）**：`PressedBtn` 那颗粒子的
        //     **节点名 / rect / sprite** 本地取不到 —— 类元数据在
        //     `assets_full/globalgamemanagers/MonoScript/MonoScript_2686.json`（`m_ClassName = ActiveWarlordAbility`），
        //     但**全库没有任何一份 prefab/场景实例引用它**：
        //       · `assets_full` 全树 `grep "PressedBtn"` / `"inPlayClickTime"` / 引 `m_PathID: 2686`
        //         ⇒ 零命中（命中的全是小整数 pathID 的假阳性，逐个核过）；
        //       · `d:/2/解包整理/08_预制体特效/战斗预制体/GameObject/CardPrefab_-7895724578163876928.json`
        //         的 **4 个子 GameObject 没有导出**（`-4334370980763821120` / `3756633904034061248` /
        //         `-8834377289179948096` / `1809300772220869568`）—— 那颗组件就可能在其中一个上面。
        //     ⇒ **状态与 0.4 s 时序**在这里；**建层**归拿到判据的人（钩子没接时本类**出声一次**，不静默）。

        /// <summary>原版 `ActiveWarlordAbility` 组件的等价物（一张卡一台，经
        /// <see cref="CardBoardHighlight.WarlordAbility"/> 惰性取）。
        /// <para>⚠️ **本类只承状态与时序，不建件**：`PressedBtn` 那颗按钮的节点判据本地拿不到
        /// （见上面那段），建层与接线是宿主的事（`Battle/BattleDriver.cs`，全项目只有 K-01 一个写手）。</para></summary>
        public sealed class ActiveWarlordAbilityTouch
        {
            /// <summary>原版 `cardShowTime`（`+0x40`）—— **按住多久弹卡面**。
            /// 值 = `.ctor` 里的立即数 `0x3e99999a` = **0.3f**。
            /// 🔴 ⛔ **别和手牌那 0.2 s（`CardInteraction.LongPressNoDisplaySeconds`）合并**：
            /// 那是**另一个类**、方向相反（长按**不**开窗），见类头那段。</summary>
            public const float CardShowTime = 0.3f;

            /// <summary>原版 `DOVirtual.DelayedCall` 的时长 —— **PressedBtn 亮多久**。
            /// 值 = `.rdata DAT_1834b3158` = **float 0.4**（4 字节读；同一张表见
            /// `工具/read_literal.py` 文件头「已经靠它读出来的」那一节）。</summary>
            public const float PressedBtnHideDelay = 0.4f;

            /// <summary>时间泵 —— 原版 `OnTouchDown` / `Update` 直接读 `Time.time`。
            /// <para>⚠️ 批处理里 **`Time.time` 不前进**（没有帧循环）⇒ 自检要把它钉成假时间
            /// （先 `Clock = () =&gt; 100f` 按下、再 `Clock = () =&gt; 100.4f` 推进一次 `Tick()`），
            /// 否则「按住 0.4 s」那一档**永远验不到**（会退化成假绿）。生产恒为默认值。</para></summary>
            public System.Func<float> Clock = () => Time.time;

            /// <summary>原版 `parentCard.isPlayer`（`EntityScript +0x40`）。假 ⇒
            /// `OnTouchUpAsButton` **整条早退**（原版 `.c:55-58`）。</summary>
            public bool IsPlayer;

            /// <summary>原版 `manager.activeSkillDesc(+0x168).SetupActiveSkill(parentCard)` ——
            /// **填技能面板**（我们的等价物 = `BattleDriver.ShowSkillPanel(u, targets)`）。
            /// ⚠️ 原版**先填面板、再试放**，两步都要；面板填了但打不了是**正常结果**（不是错误）。</summary>
            public System.Action SetupActiveSkill;

            /// <summary>原版 `BattleManager.TryUsingActiveAbilityOfCard(parentCard, forceSelectCard: true)`。
            /// **返回语义必须照抄**（见类头 ①②③）：真 = 「**这一下就把它放掉了**（技能不需要选目标）」；
            /// 假 = 「打不了」**或**「需要选目标、已进入选目标态」。
            /// ⛔ 别把「进入了选目标」也算成真 —— 那会让按压反馈多亮一次（原版那里 return 0）。</summary>
            public System.Func<bool> TryUseActiveAbility;

            /// <summary>原版 `CardScript.ForceDisplayCard()` —— 按住 0.3 s 后**弹大卡窗**
            /// （我们那一支 = `BattleDriver.ToggleUnitCard` / `CardDisplayWindow` 的开窗）。
            /// ⚠️ 原版同刻还会把 `CardScript.displayingCardFlag(+0x24A)` 置 1；我们的等价位是
            /// 驱动层的 `_cardWinSource`（`AfterCardWinToggle` 已写）⇒ ⛔ **别两处都写**
            /// （见 `CardBoardHighlight.DisplayingCardFlag` 的 doc）。</summary>
            public System.Action ForceDisplayCard;

            /// <summary>原版 `PressedBtn.SetActive(bool)`（`+0x30` 那颗 GameObject）。
            /// ⚠️ **节点判据本地拿不到**（见类头末段）⇒ 建层的人接这里；没接时**出声一次**（不静默）。</summary>
            public System.Action<bool> PressedBtnSetActive;

            bool _inPlayClickFlag;       // 原版 `+0x38`
            float _inPlayClickTime;      // 原版 `+0x3c`
            bool _displayingCardFlag;    // 原版 `+0x39`
            bool _holdDisplayedThisPress;   // 🔶 自己设计（非原版）：原版靠 `BattleManager.DisplayCard`「同卡重复开窗幂等」避免「按住弹窗→松手又 toggle」；我们没复刻那条幂等（`CardDisplayWindow.Toggle` 是**真 toggle**）⇒ 用这一格挡一次（判据 = `BattleManager__DisplayCard.c:1-40` 的三支早退；替代语义覆盖 = 只挡「同一按」）· 调度台 2026-10-27 裁定
            bool _pressedBtnOn;
            bool _pressedBtnHidePending;    // 🔶 自己设计（非原版）：原版是 `DOVirtual.DelayedCall(0.4f, b__12_0)`（`ignoreTimeScale = 1` ⇒ **unscaled**）；我们收进 `Tick` 的同一台 `Clock`（= `Time.time`，**scaled**）⇒ 只有 `Time.timeScale != 1` 时看得出差别（本仓无全局时间缩放）· 判据 = `ActiveWarlordAbility___OnTouchUpAsButton_b__12_0.c:1-9` · 调度台 2026-10-27 裁定
            float _pressedBtnHideAt;
            bool _warnedDisplay, _warnedPressedBtn, _warnedSetup, _warnedUse;

            // ---------------------------------------------------------------
            //  原版 4 个触控入口 + Update
            // ---------------------------------------------------------------

            /// <summary>原版 `OnTouchDown`（`.c:1-14`，全文两句）：记按下时刻 + 置按下标志。
            /// <para>⚠️ 调用点 = 棋盘卡的**按下那一沿**（我们 = `BattleDriver` ⑤ 段记 `_pressSlot` 的那一句，
            /// 与 `BoardCardTouchDown` 同刻）。⛔ 别在「按住每帧」里调它（会把 elapsed 一直清零 ⇒ 永远不到 0.3）。</para></summary>
            public void OnTouchDown()
            {
                _inPlayClickTime = Clock();
                _inPlayClickFlag = true;
                _holdDisplayedThisPress = false;
            }

            /// <summary>原版 `OnTouchExit`（`.c:1-7`，全文一句）：清按下标志。
            /// <para>调用点三条（松手 / 拖够 / 指针出框）**都要清一次**；出框判据用真 collider
            /// （与手牌的 `CardInteraction._pressInside` 同一个口，⛔ 别另立一套）。</para></summary>
            public void OnTouchExit() { _inPlayClickFlag = false; }

            /// <summary>原版 `Update`（`.c:1-26`）—— **每帧一次**。
            /// <para>两件事：① 按住超过 <see cref="CardShowTime"/> ⇒ 弹卡面（并记
            /// <see cref="HoldDisplayedThisPress"/>，供松手那支挡「再开关一次」）；
            /// ② PressedBtn 的 0.4 s 到点 ⇒ 熄灭（原版那半是 `DOVirtual.DelayedCall`，
            /// 我们收进同一台时钟 ⇒ 批处理可验）。</para>
            /// <returns>真 = 这一帧刚弹了卡面。</returns></summary>
            public bool Tick()
            {
                float now = Clock();
                bool fired = false;
                if (_inPlayClickFlag)
                {
                    float elapsed = now - _inPlayClickTime;
                    // 原版那一句是 `cardShowTime <= elapsed && elapsed != cardShowTime`
                    // ⇒ 浮点上等价于**严格大于**；⛔ 别改成 `>=`（正好 0.3 那一下原版不弹）。
                    if (elapsed > CardShowTime)
                    {
                        _inPlayClickFlag = false;
                        _displayingCardFlag = true;      // 原版对 +0x38 写两字节 0x100（= flag 清 0 + 本格置 1）
                        _holdDisplayedThisPress = true;
                        if (ForceDisplayCard != null) ForceDisplayCard();
                        else WarnOnce(ref _warnedDisplay, "ForceDisplayCard");
                        fired = true;
                    }
                }
                if (_pressedBtnHidePending && now >= _pressedBtnHideAt)
                {
                    _pressedBtnHidePending = false;
                    PressedBtn(false);
                }
                return fired;
            }

            /// <summary>原版 `OnTouchUpAsButton`（`.c:1-72`）—— 轻点/松手那一沿。
            /// <para>逐句照类头版权 ①②③④；**只有**「技能不需要选目标且真放掉了」才亮 PressedBtn。</para>
            /// <returns>真 = 这条链走完了（含「打不了」「需选目标」两档，那两档也是正常终态）；
            /// 假 = 被 `isPlayer` 闸挡掉（原版 `.c:55-58`）。</returns></summary>
            public bool OnTouchUpAsButton()
            {
                if (!IsPlayer) return false;                                  // 原版 `:55-58`
                if (SetupActiveSkill != null) SetupActiveSkill();              // 原版 `:60`
                else WarnOnce(ref _warnedSetup, "SetupActiveSkill");
                bool used;
                if (TryUseActiveAbility != null) used = TryUseActiveAbility();
                else { WarnOnce(ref _warnedUse, "TryUseActiveAbility"); used = false; }
                if (!used) return true;                                       // 原版：打不了 / 进选目标 ⇒ 到此为止
                if (PressedBtnSetActive == null)                              // 原版 `PressedBtn == null` ⇒ 什么都不做
                {
                    WarnOnce(ref _warnedPressedBtn, "PressedBtnSetActive");
                    return true;
                }
                PressedBtn(true);
                _pressedBtnHideAt = Clock() + PressedBtnHideDelay;             // 原版 `DelayedCall(0.4f)`
                _pressedBtnHidePending = true;
                return true;
            }

            void PressedBtn(bool on)
            {
                _pressedBtnOn = on;
                if (PressedBtnSetActive != null) PressedBtnSetActive(on);
            }

            void WarnOnce(ref bool flag, string what)
            {
                if (flag) return;
                flag = true;
                Debug.LogWarning("[CardHighlight] `ActiveWarlordAbilityTouch` 的宿主钩子 `" + what
                               + "` 没接线 ⇒ 原版 `ActiveWarlordAbility` 那一段本次**没有执行**"
                               + "（出声，不静默）");
            }

            // ---------------------------------------------------------------
            //  读数 / 自检口（⛔ 别在断言里自己拼字符串，读这里）
            // ---------------------------------------------------------------

            /// <summary>原版 `inPlayClickFlag`（`+0x38`）：现在正按着、且还没到 0.3 s。</summary>
            public bool InPlayClickFlag { get { return _inPlayClickFlag; } }
            /// <summary>原版 `inPlayClickTime`（`+0x3c`）。</summary>
            public float InPlayClickTime { get { return _inPlayClickTime; } }
            /// <summary>原版 `displayingCardFlag`（`+0x39`）—— 按住 0.3 s 那一支写 1。
            /// ⚠️ 这是**本组件**那一格，与 `CardScript.displayingCardFlag(+0x24A)` 是两个字段。</summary>
            public bool DisplayingCardFlag { get { return _displayingCardFlag; } }
            /// <summary>这一按**已经**因为长按弹过卡面（宿主在松手那支读它，挡一次重复开关）。</summary>
            public bool HoldDisplayedThisPress { get { return _holdDisplayedThisPress; } }
            /// <summary>PressedBtn 现在亮着吗（读口）。</summary>
            public bool PressedBtnVisible { get { return _pressedBtnOn; } }
            /// <summary>0.4 s 熄灭还挂着吗（读口 —— 「亮了但没灭」这种半成品一眼能看出来）。</summary>
            public bool PressedBtnHidePending { get { return _pressedBtnHidePending; } }
            /// <summary>这一按按了多久（秒）；没按着 = 0。</summary>
            public float PressElapsedSeconds
            {
                get { return _inPlayClickFlag ? Mathf.Max(0f, Clock() - _inPlayClickTime) : 0f; }
            }

            /// <summary>自检/报告用的单行读数。</summary>
            public string Describe()
            {
                return "holdFlag=" + (_inPlayClickFlag ? 1 : 0)
                     + " elapsed=" + PressElapsedSeconds.ToString("0.###")
                     + " displayingCard=" + (_displayingCardFlag ? 1 : 0)
                     + " holdDisplayed=" + (_holdDisplayedThisPress ? 1 : 0)
                     + " pressedBtn=" + (_pressedBtnOn ? 1 : 0)
                     + " hidePending=" + (_pressedBtnHidePending ? 1 : 0);
            }
        }

        // ==================================================================
        //  🆕 2026-10-28（执行代理 `B89_E5e` · 账 `战斗界面_HUD:761`）：
        //  **「敌手牌那一格能不能点开」—— 本链里可以单独断言的那一步**
        //
        //  原版判据（`decomp_full/CardScript__OnTouchUpAsButton.c:22-51` 逐句现读）：
        //    `cardState ∈ {inHand(1), inHandShowing(8)}` 且 `isPlayer(+0x40) == 0` 时：
        //    必须 `rawCardData(+0x28) != null` **且** `rawCardData.spellType(+0x90) == 0xE6 (Sabotage)`
        //    才继续走「短按（≥0.2 s 的门）⇒ `BattleManager.DisplayCard(bm, this, 1, 0, 0)`」；
        //    否则 `return` —— 这一下**不认**。
        //
        //  🔴 **同一批现读推翻施工单的前提**：`BattleCardUI__ShouldShowCardback.c:29-39` =
        //     `inHand ∧ !isPlayer ⇒ return 1`（**恒显示卡背**，任何状态都不翻面）
        //     ⇒ 「把敌方手牌翻面成可点 `CardView`」这条链路**原版并不存在**：原版点的是**卡背那一格**、
        //     开的是卡面**展示窗**（语义 = 偷看那张 sabotage 牌）。
        //     ⛔ 所以本链**不该**新建 CardView、也不该有「亮出敌方手牌」这个开关 —— 那会是**自造**。
        // ==================================================================

        /// <summary>敌手牌那一格的**点击闸**（把原版那两跳合成一次调用；判据见上方长注）。
        /// <para>⛔ `sabotage` 的判据**只此一处**：`CreatePool.MatchesKind(card, "sabotage")`
        /// （与 <see cref="OutlineOf"/> 用的是同一个；`CardDef.spellType` 我们这边没有等价列）。</para>
        /// <para>宿主只需给三样：**是不是我方手牌** / **有没有卡数据** / **那张卡的 `CardDef`**。</para>
        /// <para>⚠️ 返回值只回答「这一下认不认」；「认了之后开哪扇窗」归宿主那侧（原版是
        /// `BattleManager.DisplayCard(bm, this, 1, 0, 0)` = 我们的卡面展示窗）。</para></summary>
        public static bool FoeHandCardTapOpens(bool isPlayer, bool hasRawCard, RuleEngine.CardDef card)
        {
            bool sabotage = card != null && RuleEngine.CreatePool.MatchesKind(card, "sabotage");
            return CardBoardHighlight.EnemyHandCardClickable(isPlayer, hasRawCard, sabotage);
        }
    }
}
