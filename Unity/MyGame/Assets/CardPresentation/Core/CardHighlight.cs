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
// ⚠️ 现在只用**整卡着色**表达状态，够原型验证用。正式版该换成描边/流光
//    （一张卡同时是「可打出」又「被悬停」的话，单靠颜色分不开）——
//    那时改这张表 + 换一个支持描边的 shader 即可，状态机不用动。
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
        /// <summary>原版 `ValidTargetColor` —— 绿 **#00FF21**（手牌/场上两档「潜在目标」共用）。</summary>
        public static readonly Color ValidTargetColor = new Color(0.00f, 1.00f, 0.1294118f);
        /// <summary>原版 `SelectedColor` —— 白（`selected` 是**瞬变**态）。</summary>
        public static readonly Color SelectedColor = new Color(1.00f, 1.00f, 1.00f);
        /// <summary>原版 `SelectedTargetColor` —— 橙 **#FF8400**（`selectedTargetInBoard`，**×1.05**）。</summary>
        public static readonly Color SelectedTargetColor = new Color(1.00f, 0.5176f, 0f);

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
                case CardHighlightState.ValidTarget: return ValidTargetColor;                      // 原版 ValidTargetColor：绿
                // 原版 `SelectedTargetColor`：橙 #FF8400。⚠️ 原版**只拿它点那圈状态环**（见 `FrameColorOf`），
                // 「整卡着色」这一路是**我们的表达方式**（同上面 `ValidTarget` 那行的处境）——
                // 颜色是原版的，不是我们挑的；这样「指针移到哪个目标身上」在整卡上也读得出来。
                case CardHighlightState.SelectedTargetInBoard: return SelectedTargetColor;

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
        /// 🔴 **和 `ColorOf` 是两条路**：那个是「整卡着色」（我们在用的表达），这个才是**原版那一层**。
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
    }
}
