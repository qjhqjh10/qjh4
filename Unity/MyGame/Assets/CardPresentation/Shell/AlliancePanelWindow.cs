// AlliancePanelWindow.cs — A985③：原版 `FrontCanvas/Alliance Panel`（组件类 `BattleAlliancePanel`）整扇窗
//
// ============================ 这是什么 ============================
// 对局里点 HUD 上那颗联盟钮（原版 `BattleHud.AlliancePanelOpenButton`）弹出来的**对手档案**窗：
// 头像框 + `Name:` / 名字 + `Title:` / 称号 + `Alliance:` / 联盟名 + 徽章 + 评级；
// **对手没有联盟时**改成一行「这个玩家还没有加入任何联盟」的提示。
//
// ============================ 判据（第一权威：解包资源逐字段亲读）============================
// 包 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`（13 个战场各一份；**逐字段比对过
// arena1 vs arena2**：根框五元组、子件 rect、贴图 pid **逐位相同** ⇒ 13 场共用同一份 prefab）。
//
//   · 根  `GameObject/Alliance Panel.json`（GO 1130，`m_IsActive = 0`）+ `RectTransform_2859.json`：
//         `m_AnchorMin = m_AnchorMax = (0,1)` · `m_Pivot = (0.13, 0.825)` ·
//         `m_AnchoredPosition = (-1.044435, -25.271729)` · `m_SizeDelta = (815.044006, 475.469971)` ·
//         `m_LocalScale = 0.9999718070030212`
//         父链 = `Canvas(RT 2759) → FrontCanvas(2900) → Safe area FrontCanvas(2862)`（两条 stretch、零偏移）
//         ⇒ **绝对矩形 = x[-107.00, 708.02] y[-57.93, 417.52]**（**顶出屏 57.9 px —— 原版就长这样**）。
//         ⚠️ 父链三级的 `m_SizeDelta` 都是 `0`（stretch 到 1920×1080）⇒ 按 1920×1080 解；
//            `Canvas` 自己 `m_LocalScale = 0` 是**序列化残留**（ScreenSpaceOverlay 的 Canvas 由引擎驱动）
//            ⇒ 按 `1` 解，⛔ 别拿那个 0 去乘。根自己那个 `0.9999718` 同理按 `1` 解（差 0.0028%）。
//   · 控制器 `MonoBehaviour_4117.json`（`m_Script = BattleAlliancePanel`）的 12 个字段，按偏移顺序：
//     0x20 `avatarDisplay` · 0x28 `playerName` · 0x30 `playerTitle` · 0x38 `playerTitleHolder` ·
//     0x40 `playerScore` · 0x48 `allianceGO` · 0x50 `playerAllianceName` · 0x58 `allianceRatingDisplay` ·
//     0x60 `backgroundCloseButton` · 0x68 `canvasGroup` · 0x70 `allianceBadgeDrawer` ·
//     0x78 `notInAnAllianceMessage`（= GO 762 / RT 2660 / `MonoBehaviour_4475`）
//   · 方法体（`d:/2/tools/decomp_full/`，第一权威）：
//     `BattleAlliancePanel__Awake.c`  ⇒ **开局 `SetActive(false)`**，并给 `backgroundCloseButton` 挂
//                                       「点了就 `CloseButtonClick`」；`…__CloseButtonClick.c` = `SetActive(false)`。
//     `BattleAlliancePanel__Open.c`   ⇒ 显隐/赋值全在里面（逐句等价见 `Apply`）。
//     `BattleHud__AlliancePanelOpenButton.c` ⇒ HUD 取 `BattleManager.matchData` 的
//                                      `+0x10` 名 / `+0x50` 称号 / `+0x38` 评级 / `+0x40`,`+0x48` 头像 id /
//                                      `+0x68` 联盟名 / `+0x7c` 联盟评级 / `+0x80` 徽章 id 调 `Open(...)`。
//   · 节点树逐行核对表：`资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md` §五
//     （⚠️ 它给的是「**未乘 `localScale`** 的 rect」；本文件用**乘过的实绘矩形**，两者中心相同）。
//
// ============================ 🔴 三个「一个值 ≠ 全部情况」的坑 ============================
//  ① **`PlayerInfo` 身上有 `VerticalLayoutGroup`**（`MonoBehaviour_4213`：`m_Spacing = 0` ·
//     `m_ChildAlignment = 0`(UpperLeft) · `m_ChildControlWidth = 1` · **`m_ChildControlHeight = 0`**）⇒
//     `NameHolder` / `TitleHolder` / `Player Rating Display` 三个 holder 的序列化 rect **全是 0×0、
//     且都落在 `PlayerInfo` 的锚点上 = 「布局跑之前的模板位」** ⇒ 照抄会让三行叠成一坨。
//     ✅ 正确值按 `Core/UguiRect.cs` 的 `UguiLayout.VerticalChild`（那一条刚好就是本组合）重算 —— 见 `Build`。
//     ⚠️ 与 CLAUDE.md §三「权威坐标表里的 0×0 / 越界 rect 是模板位」是同一条，本件是**第二个实例**。
//  ② **文字那一族的行内 `m_LocalScale = 0.8`**（头像框 1.25 · 头像图 2.0）⇒ 字号与框都要乘它。
//     序列化 `m_fontSize` 是**没乘的 em**。
//  ③ **`Close Background` 的 rect 是 11668×8012**（远超屏幕、中心在面板右上）⇒ 那是原版「铺满全屏的
//     熄灯层」，⛔ 别当它是排版错误。
//
// ============================ 🔴 分层：只用渲染队列 ============================
//  全部件在**透明队列 3000**（`ImageQuad` 材质的默认档）⇒ 同队列下谁压谁由「到相机的 3D 距离」定，
//  而 `Close Background` 中心在 x≈+650 px（**离相机比面板本体近**）⇒ **靠 z 排会把熄灯层画到面板上面**。
//  ⇒ 逐层显式写队列（`QShade`…`QText`），并把「压暗层档 < 内容最低档」这条交给 `MenuDraw.ShadeHit` 自己校验。
//
// ============================ 🔴 2026-10-16（`T09` 批 3 · `OverHand` 组）：已迁 uGUI（双模式）============
//  配方与全部口径 → `资料/任务线/战斗线/任务/T09_整层换uGUI.md` **§五·补⑨ / §五·补⑩**；
//  映射那一行 → `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一（`Alliance Panel` @
//  `…/Safe area FrontCanvas` · **`OverHand`/order 0** · 兄弟序 **13/14**）；
//  跨线裁定全文 → `资料/普查产出_1016并行/S2_AlliancePanel跨线裁定_报告.md` §⑨（**路 B：就地翻、⛔ 不新建第二份**）。
//   · **子画布** = `HudCanvas.EnsureSubCanvas(null, SubCanvasName, OverHand, 0)` —— 原版那一格**自身没有
//     `Canvas` 组件**、随 `FrontCanvas`（`OverHand`/0）渲染 ⇒ 我们给它一块**同层同序 + `overrideSorting`**
//     的子画布（继承根 canvas 那个唯一的 `CanvasScaler`；⛔ 子画布不加 `CanvasScaler`）。
//   · 🔴 **双模式（`uguiHost == null` ⇒ 与改本件之前【逐位相同】）**：`Create(parent, name, uguiHost)` ——
//     `null` ⇒ 今天的世界档（`MenuDraw.*` + `renderQueue` 阶梯，**那些数一个都没动**）；非 null ⇒ 画布档。
//     先例 = `Tooltip.UseHost(host)` / `ErrorMessageBanner.Create(…, uguiHost)` / `UnitChatPanel.Create(parent, uguiHost)`。
//     （上游调用点 = `Battle/BattleDriver.cs` **现核 `:15272-15286`** —— ✅ **收口波 `G1` 已接线**；
//       本条原写作 `:15084`，那份文件随后被 `G1`/`G3` 改过 ⇒ 行号已漂，⛔ 别照旧号找。）
//       ⚠️ 本文件末尾「入口」那一节原来写着「待接线」—— ✅ **已就地订正**（见那一节）。
//   · 🔴🔴 **层深度：队列 → 兄弟序【要取负】** —— 本件最要命的一处换算，⛔ 别「照搬队列值」：
//     世界档靠 `renderQueue` 分层（**队列大的画在【上面】**），而 `HudCanvas.Add` 的 z 是**兄弟序**
//     （**z 大的画在【底下】** —— 判据 = `HudCanvas.Group.Add` 的「按 z 降序插入」＋ `UnitChatPanel.ZPortrait`
//     与 `BattleLogPanel.ZShade` 两条注释都写着「z 大的在后面/最底下」）⇒ **两者极性相反**。
//     照搬的后果：熄灯层 3980 变成**最小的 z** ⇒ 被排到最后、**画在面板上面**（整屏 α0.4627 黑罩住面板）。
//     ⇒ 画布档一律走 **`ZOf(q) = −q`**（唯一一处换算，见那个助手）：相对先后**逐件不变** ✓。
//   · ⚠️ **命中**：`MenuDraw.ShadeHit` / `MenuDraw.Absorb` 走的是 `WindowButton` + 世界空间 quad
//     （只有壳侧 `PointerLayer` 会派发 —— 本文件 `Build` ① 那段「⛔ 不建 `MenuDraw.ShadeHit`」
//      与 `HitBody` 的 doc 各自自陈一遍）⇒ **画布档不建它们**
//      ⚠️ 本条原来写的是**本文件行号**（`:370-372`/`:486-488`）—— 本件注释一改就会漂，
//      ⇒ **改成按内容指路**（判据在本文件里、⛔ 别再补行号）。
//     （本轮口径：**不建 `EventSystem`、不开 `raycastTarget`** —— 判据 → `Battle/HudCanvas.cs:33-35`）。
//     战斗里「点窗外关窗」照旧由 `BattleDriver.HandleAlliancePanel` 用本类 `HitBody` 接 ⇒ `HitBody`
//     **已按档分支**（画布档走 `RectTransform.InverseTransformPoint` + `RootCenterLocal`，任何宽高比都对）。
//   · ✅ **2026-10-16（`A14` · `T09` 批 3 锚点补正）：原来那一处「如实记的口径分歧」已按【原版】裁掉。**
//     原版那一级 = `aMin = aMax = (0,1)` **顶左点锚** + `piv(0.13,0.825)` + `ap(-1.0444,−25.2717)` +
//     `sd(815.044,475.470)`（**不拉伸**）；画布档原先是「中心锚 + 固定设计 px」
//     （`HudCanvas.SetPxSize` + `PosPx`，批 2/3 共用那一套）⇒ **16:9 下逐位相同**、**非 16:9 整扇跑偏**
//     （4:3 差 **135 屏 px**；通式 = `(屏高 − 0.5625 × 屏宽) / 2`）。
//     ⇒ 现在画布档**逐值照抄那五元组**（`SetRootAnchor` + `RootAnchor*`/`RootPivot`/`RootAnchoredPos`/
//     `RootSizeDelta`），`HitBody` 同批补了「枢轴 ≠ 中心」那一项（`RootCenterLocal`）。
//     **世界档逐字节未动**（判据 → `资料/普查产出_1016并行/T09_AlliancePanel_锚点补正_报告.md` §③/§④）。
//   · ⚠️ **⛔ 没顺手做的四件老缺口**（与迁 uGUI 无关，见下面「已知缺口」①②③④）：头像立绘 / 两处评级 /
//     徽章 `Frame`·`Badge` / 出场 `DOFade`+`DOScale`（三常量没读）。
//     🔴🔴 **2026-10-17（`S11` 现核，铁律 5）就地订正 —— 下面 ①③④ 三条的【理由】都不成立，只有 ② 成立**：
//     ① 头像**资源全在**（`bundle_cosmeticavatarsimages_assets_all` 的 `Avatar_*`，**本仓 `Resources/Art/avatars/` 已导入 944 文件**），
//        而且**我们 HUD 早就在画同一套**（`AvatarImage_Me/Foe`：我方 = `ProfileData.AvatarArt`、对手 = 按本局种子从本地清单挑）
//        ⇒ `m_Sprite = 0` **只是出厂态**（`AvatarDisplay__ChangeAvatar.c:53-70` 有图就开、取不到还回落 `DefaultAvatarItem`）
//        ⇒ **能做**；唯一近似 = bot 的**允许清单** `AIBotsConfig` 在 LiveOps 远端。
//     ③ 徽章**图和 SO 两层都在本地**（22 张 `Alliance_Trophies_<阵营>` + **Frame 四档 `TIER1..4`** + `cosmeticsso` 的 16 个 `*_Badge.json`）；
//        真原因是 **`Open` 里 `!IsNullOrEmpty(allianceName)` 才做那一支**（`BattleAlliancePanel__Open.c:93-108`）
//        ⇒ **走不到 `Draw`**，⛔ **不是「原版出厂态没有」**。⚠️ 我们连那份 `*_Badge` 都还没接（零代码引用）。
//     ④ **三个 DOTween 常量已读出**：`0x1834b2bb8 = 1.0`（目标 alpha）· `0x1834b31d4 = 0.35`（共用时长）·
//        `0x1834b2e84 = 0.75`（起始缩放系数）⇒ **能做，属「欠做」**。
//     ✅ **只有 ② 两处评级仍归「等远端」**（`eloRating@0x38` / `allianceRating@0x7C` 来自服务端 profile、图标来自 LiveOps）。
//     判据全文 → `资料/普查产出_1016并行/S11_四件缺口与PlayedThirdCard_报告.md`。
//
// ============================ 🆕 相对原版的**已知缺口**（如实记，⛔ 不静默）============================
//  ① **头像立绘不画**：`Avatar Item Small/Image Container/Image` 的 `m_Sprite` 是 `0`（运行期由
//     `AvatarDisplay.Initialize` 灌）⇒ **原版出厂态本来就不画**（同本仓名牌那两处的口径）；
//     我们单机的 bot 也没有头像数据源 ⇒ 这一格保持原版出厂态（**只摆框**）。
//     🔴 **2026-10-17 就地订正（铁律 5）—— 上面三句【全不成立】**（`S11` 现核）：
//     · 「原版出厂态本来就不画」**错**：`AvatarDisplay__ChangeAvatar.c:53-70` = `sprite = CosmeticItem.get_Image(item)`
//       然后 **`set_enabled(img, sprite != null)`（有图就开）**，且 `Initialize` 的实参取不到时**回落 `get_DefaultAvatarItem()`**
//       ⇒ **原版永远有图**；`m_Sprite = 0` 只是**序列化出厂态**。
//     · 「我们单机的 bot 也没有头像数据源」**错**：HUD 里 `AvatarImage_Foe` **早就在画**（按本局种子从 `ProfileData.Avatars` 确定性挑）。
//     · ⇒ **本格【可做】**：把 HUD 那套搬进来即可。唯一真实差异 = 原版 bot 的**允许清单** `AIBotsConfig.GetRandomAvatar`
//       在 **LiveOps 远端** ⇒ 我们「从本地清单挑」是**已如实标注的近似**。
//  ② **两处评级不建**：`Player Rating Display` / `Alliance Rating Display` 里的 `Main Icon` /
//     `Individual rating value` 序列化都是 **0×0**，尺寸由 `RatingDisplay` 脚本运行时算
//     （两个节点身上各挂一个 `HorizontalLayoutGroup`、`m_ChildControlWidth/Height = 1`
//     ⇒ 尺寸 =「TMP/LayoutElement 首选宽」× 容器高，**静态取不到**）；而且我们单机**没有对手评级数据**。
//     ⇒ 不建那两格（要补：先有一个真评级数据源 + 一次运行期读数）。
//  ③ **徽章 `Frame` / `Badge` 不建**：两张 `m_Sprite` 也是 `0`（`AllianceBadgeDrawer.Draw(id)` 运行期灌）
//     ⇒ 同样是「出厂态就没有」；且我们单机恒走「无联盟」那一支，那两格根本不显。
//     🔴 **2026-10-17 就地订正（铁律 5）—— 前半句错、后半句对**（`S11` 现核）：
//     · 「出厂态就没有」**错**：**图和 SO 两层都在本地** ——
//       `bundle_alliancesbadgesimages_assets_all/Sprite|Texture2D/` 的 **22 张 `Alliance_Trophies_<阵营>`**
//       ＋ **Frame 四档 `Alliance_Trophies_TIER1..TIER4`**（`AllianceBadgeDrawer__SetTierFrame` → `LoadTierFrame`，索引 `Max(0, tier-1)`）
//       ＋ `bundle_cosmeticsso_assets_all/MonoBehaviour/` 的 **16 个 `*_Badge.json`**；
//       **`Badge`** 走 `GroupBadge.featuredTrophy@0x10 → AllianceTrophyData.reward@0x58 → AllianceBadge.badgeImage@0x38`（`AllianceBadgeDrawer__Draw.c:24-30`，null 回落 `DefaultTrophy`）。
//     · 🔴 **真原因是「走不到那一支」**：`BattleAlliancePanel__Open.c:93-108` 是 **`bVar9 = !IsNullOrEmpty(allianceName)`** 才做联盟那一块
//       ⇒ 我们传 `Open(name, "", "")` ⇒ **`allianceGO` 关、`Draw` 压根不执行** ⇒ ⛔ **不是「原版没有」**。
//     · ⚠️ 而且**我们连那份 `*_Badge` 都没接**（`grep Alliance_Trophies` 在 `d:/4` 只命中两份盘点文档、零代码/零生成器引用）。
//     · ⚠️ **真要建 ③ 还缺一样**：`guid_map.tsv` **不覆盖徽章 SO**（`AM_Forge_Badge.badgeImage` 的 GUID 在那表里 0 命中）⇒ 要另找一份「徽章 GUID → sprite」映射。
//  ④ **出场动画（`DOFade` + `DOScale`）没做**：原版 `Open` 末尾是 `CanvasGroup.alpha = 0` → `DOFade`
//     + 「先缩到 `×DAT_1834b2e84` 再 `DOScale` 弹回」。本件**直接显/隐**（本仓其它窗同此）；
//     ⚠️ 三个 DOTween 常量（目标 alpha / 时长 / 缩放系数）**没读**（要在 `GameAssembly.dll` 里取值）。
//     🔴 **2026-10-17 就地订正（铁律 5）—— 最后那句【已过期】：三个常量【已读出】**（`S11` 现核，`BattleAlliancePanel__Open.c:99-125`）：
//     `0x1834b2bb8` = **1.0**（`DOFade` 目标 alpha；4B float —— 判据 = 形参类型 `float`）
//     · `0x1834b31d4` = **0.35**（`DOFade` 与 `DOScale` **共用的时长**，秒）
//     · `0x1834b2e84` = **0.75**（**起始缩放系数**）。
//     ⇒ **本件属「欠做」（能做、只是没做）**，⛔ 别再写成「判据拿不到」。
//     （读法：`python -I -X utf8 工具/read_literal.py "D:/2/unity_run_ref/GameAssembly.dll" <地址>`；⚠️ 要 `PYTHONIOENCODING=utf-8`。）
//  ⑤ ~~**HUD 上那颗钮我们还没有**（见文件末「入口」一节）~~ ⇒ ✅ **2026-10-18（A985③）已接**：
//     `Battle/BattleDriver.cs` 的 `BuildHudExtras` 里建了那颗钮，点击入口 = `HandleAlliancePanel`。
//     🔴 **那颗钮【没有贴图】** —— 原版它的 `m_TargetGraphic` 类是 `NonDrawingGraphic`
//     （`OnPopulateMesh` 方法体为空）、节点就是 `LeftArea/EnemyInfo` **本身** ⇒ 纯命中区。
//     判据全文（rect / padding / 13 场同构 / 点击沿）**只写在那一处**，本文件不重复。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `FrontCanvas/Alliance Panel`（`BattleAlliancePanel`）—— 对局内的**对手档案**窗。
    /// <para>⚠️ **它是「面板」不是「窗」**：根组件是普通 `MonoBehaviour`、**不是 `GameWindow`**
    /// ⇒ 没有 `type` / `placement` / `closeOnESC` 可填、也⛔ **不注册进 `WindowsManager`**
    /// （同 `Shell/AllianceEventScorePanel.cs` 的口径；别为了「凑一条注册」给它编一个窗身份）。</para></summary>
    public class AlliancePanelWindow : MonoBehaviour
    {
        // ============================================================ 分层（渲染队列，⛔ 不是 z）
        //  全部 ≥ 3980 ⇒ 都在战斗 HUD（默认档 3000）之上；`QShade` 是唯一低于内容的档。
        public const int QShade = 3980;     // 熄灯层 `Close Background`（**必须严格低于 `QContentMin`**）
        public const int QBg = 4000;        // `Background` / `BG`
        public const int QFrame = 4010;     // `BGFrame` 四块
        public const int QAvatar = 4020;    // 头像框 `Border`
        public const int QText = 4030;      // 全部文字
        /// <summary>传给 `MenuDraw.ShadeHit` / `MenuDraw.Absorb` 的「本窗内容最低档」——
        /// 那两个口靠它算「压暗/吸收命中区用哪一档」并**当场校验**（不合法会告警）。</summary>
        public const int QContentMin = QBg;

        // ============================================================ 🆕 uGUI 档（`T09` 批 3）
        /// <summary>本扇窗那块 **uGUI 子画布**的名字（= 原版宿主名 + `" Canvas"`，`T09` §五·补⑩ 第 1 步）。
        /// 判据 = `T09c` §一 我这一行：`Alliance Panel` @ `…/Safe area FrontCanvas` · `OverHand` / order 0 ·
        /// 兄弟序 13/14。⛔ **只此一份**（建它、自检找它都取这里）。</summary>
        public const string SubCanvasName = "Alliance Panel Canvas";

        /// <summary>🔴 **队列 → 画布档的 z（唯一一处换算）**：`HudCanvas.Add` 按 z **降序**排兄弟序
        /// （**z 大的画在【底下】**），而 `renderQueue` 恰好相反（**队列大的画在【上面】**）⇒ **取负**，
        /// 相对先后才逐件不变。⛔ 别「照搬」队列值 —— 那样熄灯层 3980 会变成最小 z、**画到面板上面**。
        /// 判据 → 文件头那条 + `HudCanvas.Group.Add` 的 doc + `UnitChatPanel.ZPortrait`（z 大 = 在底下）。</summary>
        static float ZOf(int q) { return -q; }

        // ============================================================ 根框（画布像素 · 左上原点 · 1920×1080）
        //  ⚠️ 这是原版矩形的**实绘**值（乘过根那个 `m_LocalScale = 0.9999718`；与 `_probe_node_rect.py` 的
        //     原始输出 `x [-107.000, 708.044] y [-57.936, 417.534]` 差 ≤0.024 px）。
        //     用途 = ① 世界档的摆位 ② 面板内**所有子件**的「相对父中心」参照（`LocalIn` 的 `parentR`）。
        static readonly PxRect RootR = new PxRect(-107.00f, -57.93f, 708.02f, 417.52f);

        // 🔴🔴 **2026-10-16（`A14` · `T09` 批 3 锚点补正）：根那一级的【五元组】= 原版 `RectTransform_3052`**
        //   出处 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_3052.json`
        //   （⚠️ arena1 那份的 pid 是 `2859` —— **13 场逐字段比对过、五元组逐位相同**，本文件头那一节）。
        //   · `m_AnchorMin = m_AnchorMax = (0,1)` ⇒ **顶左【点锚】**（父链两跳都是 stretch ⇒ 屏幕左上角）。
        //     🔴 **点锚只管「尺寸固定」；它的【位置】由「锚点落在父件的哪个角/边」定** ——
        //     只有锚点 = 父件**中心** `(0.5,0.5)` 时，「点锚 + 固定 px」才等价于「中心锚 + 固定 px」。
        //     **本件原来就是被这条坑到的**（画布档写成中心锚 ⇒ 16:9 逐位相同、非 16:9 整扇跑偏）。
        //   · `pivot` / `anchoredPosition` / `sizeDelta` 逐值照抄（⛔ 一个数都没改）。
        //   · `m_LocalScale = 0.9999718070030212` **不设**（与 `A9` 那档一致）：0.0028% ⇒ 实绘差 ≤0.024 px，
        //     而设它会把**整棵子树**一起缩（判据 → 报告 §④ 那条「16:9 自证」）。
        static readonly Vector2 RootAnchorMin = new Vector2(0f, 1f);
        static readonly Vector2 RootAnchorMax = new Vector2(0f, 1f);
        static readonly Vector2 RootPivot = new Vector2(0.13f, 0.825f);
        static readonly Vector2 RootAnchoredPos = new Vector2(-1.04443359375f, -25.271728515625f);
        static readonly Vector2 RootSizeDelta = new Vector2(815.0440063476562f, 475.470970703125f);

        // ============================================================ 逐件几何（**全 = 实绘矩形**，已乘 localScale）
        //  命名 = 原版节点名；括号里的 pid = `RectTransform` 的 PathID（可逐条复查）。
        //  ⚠️ 这一组**不是从报告里抄的**，是按 `RectTransform_*.json` 的五元组 + 父链 + `m_LocalScale` 现算的
        //     （算式 = 文件头那三条；与 `子代理读报_front弹层_0827.md` §五 那张表**中心逐一吻合**，
        //      差别只在「它没乘 `localScale`」）。

        // 熄灯层 `Close Background`（RT 2698）：图 = 同族底图 + 纯黑 α0.4627；`m_RaycastTarget = 1`
        static readonly PxRect ShadeR = new PxRect(-5181.741f, -3867.769f, 6486.258f, 4144.042f);
        const float ShadeAlpha = 0.4627f;                       // 实读 `m_Color = (0,0,0,0.4627)`

        // 面板本体 `Background`（RT 3166）。⚠️ 它自己**没有** Image（只有 `BG` 那张有）—— 见 `Build` 吸收层那条
        static readonly PxRect BackgroundR = new PxRect(-97.327f, 9.970f, 625.312f, 413.089f);
        // 内容底 `BG`（RT 3225）：贴图 `40K_shop_offer_bg_Sororitas_0`、染色 (0.349,0.349,0.349,1)、Type=Simple
        static readonly PxRect BgR = new PxRect(14.158f, 55.246f, 571.457f, 356.583f);
        static readonly Color BgTint = new Color(0.349f, 0.349f, 0.349f, 1f);
        // `BGFrame`（RT 3339）与它四块（RT 2734/3407/3414/3122）；图 = `40k_UnitChat_Background_*`
        static readonly PxRect FrameR = new PxRect(-92.217f, 13.557f, 624.645f, 412.646f);
        static readonly PxRect FrameLeftR = new PxRect(-92.673f, 25.022f, 24.645f, 411.994f);
        static readonly PxRect FrameRightR = new PxRect(541.475f, 13.985f, 625.123f, 385.038f);
        static readonly PxRect FrameTopR = new PxRect(23.750f, 24.840f, 541.422f, 58.762f);
        static readonly PxRect FrameBottomR = new PxRect(24.487f, 348.664f, 542.159f, 385.195f);

        // 头像框 `Border`（RT 3040）：图 `Player Profile Border` 256×286、`m_PreserveAspect = 1` ⇒ 等比内接
        static readonly PxRect AvatarBorderR = new PxRect(-11.283f, 60.573f, 201.246f, 209.590f);

        // `Name:` / 名字（`NameHolder` = VLG 第 0 格，高 42.573 ⇒ y[62.498, 105.071]）
        static readonly PxRect NameRowR = new PxRect(180.000f, 62.498f, 556.959f, 105.071f);
        static readonly PxRect NameLabelR = new PxRect(180.000f, 63.436f, 273.941f, 103.636f);
        static readonly PxRect NameTextR = new PxRect(276.220f, 63.436f, 555.228f, 103.636f);
        // `Title:` / 称号（`TitleHolder` = VLG 第 1 格，高 38.442 ⇒ y[105.071, 143.513]）
        static readonly PxRect TitleRowR = new PxRect(180.000f, 105.071f, 556.959f, 143.513f);
        static readonly PxRect TitleLabelR = new PxRect(179.995f, 104.586f, 274.429f, 145.386f);
        static readonly PxRect TitleTextR = new PxRect(276.215f, 104.102f, 555.223f, 145.166f);
        // `Alliance` 组（RT 3495）与它那两行字（组里**没有**布局组 ⇒ 用它自己的 rect）
        static readonly PxRect AllianceGroupR = new PxRect(23.094f, 220.581f, 574.688f, 356.530f);
        static readonly PxRect AllianceLabelR = new PxRect(183.090f, 244.780f, 278.002f, 285.579f);
        static readonly PxRect AllianceNameR = new PxRect(278.940f, 244.780f, 535.827f, 285.579f);
        // `NotInaAllianceText`（RT 2660）—— 🔴 原版那句 `is is` 双 is 是笔误，照抄勿改
        static readonly PxRect NotInAllianceR = new PxRect(55.765f, 257.013f, 529.877f, 293.972f);
        const float NotInAllianceAlpha = 0.772549f;             // 实读 `m_fontColor.a`

        // 字号 = 原版 `m_fontSize` **× 该节点累计 localScale**（这一族全是 0.8）。
        //  ⚠️ 这几颗都 `m_enableAutoSizing = 1`，但序列化 `m_fontSize` **就是**上一次算出来的拟合值
        //     ⇒ 直接用它（`MenuDraw.Text` 走 `SetGlyphHeight`，不做二次 autosize —— 见 `Text` 的注释）。
        const float NameLabelPx = 28.00f;      // 35   × 0.8
        const float NameTextPx = 32.00f;       // 40   × 0.8
        const float TitleLabelPx = 28.00f;     // 35   × 0.8
        const float TitleTextPx = 28.00f;      // 35   × 0.8
        const float AllianceLabelPx = 26.72f;  // 33.4 × 0.8
        const float AllianceNamePx = 28.00f;   // 35   × 0.8
        const float NotInAlliancePx = 26.32f;  // 32.9 × 0.8

        // 🔴 **2026-10-09（A1126 · A2 档）**：上面这七颗原版 **全是 `m_enableAutoSizing = 1`**
        //   （现读 `scenes_battlearena1 ▸ Alliance Panel` 那 7 颗 TMP：
        //    `PlayerLabel` / `Name Text` / `TitleLabel` / `Title Text` / `Alliance Label` / `Alliance Name` /
        //    `NotInaAllianceText`）—— 我们原来**一个 autosize 实参都没传** ⇒ 固定字号。
        //   四格真值：`min = 18` · `base = 36` · **折行 = 0** · `max = 35`（六颗）／**40**（只有 `Name Text`）。
        //   🔴 **三格都要乘本族那个 `localScale = 0.8`** —— 与上面那几个 `*Px` 是**同一条换算**
        //   （原版 `m_fontSizeMin/Max/Base` 是**未缩放的设计空间原值**）。
        //   判据 = 本仓既有先例 `Shell/MissionsTab.cs` 的 `FS()`：**「自适应上下限也要一起乘」**
        //   （落地在 `:345` `FS(autoMinPx)` 与 `:380` `FS(minPx), FS(maxPx), FS(basePx)`；说明见 `:1302`）。
        //   ⛔ **不是我们挑的**：换算是「照原版实绘」，裸传原值反而会让上限比原版高 25%。
        const float AutoMinPx = 18f * 0.8f;       // 14.4（七颗同值）
        const float AutoBasePx = 36f * 0.8f;      // 28.8（七颗同值）
        const float AutoMaxPx = 35f * 0.8f;       // 28  （六颗）
        const float AutoMaxNamePx = 40f * 0.8f;   // 32  （只有 `Name Text` 是 40 —— ⛔ 别一刀切）

        // 原版那 4 颗 TMP 的**静态原文**（= prefab 出厂值，也是运行期 `GetTranslation` 的兜底）。
        //  🔴 运行期那 4 条真值在**远端 I2 表**（本仓没有，见 `资料/普查产出_1018/G2b_战斗本地化收口.md`）
        //     ⇒ 先查 `Loc` 表；`Loc` 里没这一条就用 prefab 原文（= 原版出厂就是那句英文）。
        const string NameLabelTerm = "Battle/AlliancePanel/PlayerLabel";
        const string TitleLabelTerm = "Battle/AlliancePanel/TitleLabel";
        const string AllianceLabelTerm = "Battle/AlliancePanel/AllianceLabel";
        const string NotInAllianceTerm = "Battle/AlliancePanel/NotInAnAlliance";
        const string NameLabelEn = "Name:";
        const string TitleLabelEn = "Title:";
        const string AllianceLabelEn = "Alliance:";
        // 🔴 **2026-10-14 就地订正（铁律 5 · `W4`/`A1803`）**：原来写 `"This player is is still not part of an Alliance"`
        //   —— 那个 `is is` 是**我们抄错的**（当时的旧注还写着「真值在远端 I2 表」）：官方
        //   `Battle/AlliancePanel/NotInAnAlliance` = **`This player is still not part of an Alliance`**
        //   （中文列「该玩家仍未加入联盟」）。⛔ 这是**死兜底**（正常走 `Loc.T`），但值要对。
        const string NotInAllianceEn = "This player is still not part of an Alliance";

        // ============================================================ 建出来的东西
        Transform _root, _background, _allianceGo, _titleHolder, _notInAlliance;
        Label _nameLb, _titleLb, _allianceLb;
        readonly List<string> _missArt = new List<string>();
        bool _visible;
        /// <summary>本扇窗是不是建在 **uGUI 子画布**上（`false` = 今天的世界档，逐位不变）。
        /// ⚠️ 必须在 `Build()` **之前**赋值（`Build` 里按它选建件口与单位）。</summary>
        bool _ugui;

        public bool Visible { get { return _visible; } }
        /// <summary>自检用（`T09` 批 3 的【灭自证】）：本扇窗的图与字**是不是都建在 uGUI 子画布上**
        /// （而不是有条漏网、退回世界空间 quad）。⛔ 别用「`Label` 非 null」顶替（那个**两档都真**）。
        /// <para>🔴 **2026-10-16（`F8` · 修波）就地订正（铁律 5）**：本口原来是
        /// `public bool OnCanvas { get { return _ugui; } }` —— `_ugui` 是**建件开关本身**
        /// （`Build` / `Node` / `Img` / `Txt` 都按它分支）⇒ 拿它当「灭自证」= **自证**
        /// （出处 = `R1_面板迁移审查_报告.md` §②-9 · `_收口台账_第四十二会话.md` `F8`）。
        /// 现在改成**逐件走父链**，形状照同批另三扇
        /// （`SkillPanel.OnCanvas` · `UnitChatPanel.OnCanvas` · `ChatPopupPanel.OnCanvas`）：
        /// 每一件拿**它自己的节点**去 `GetComponentInParent<Canvas>()`，**⛔ 不读 `_ugui`**。</para>
        /// <para>🔴 **为什么是「根 + 底下那一图一字」这三件，⛔ 不是面板里那 14 件逐颗 `Find`**：
        /// ① **判别力一分不差** —— `Build` 里那 7 图 + 7 字**全建在 `_root` 这棵子树里**（每个
        ///    `Node`/`Img`/`Txt` 的 `parent` 都是 `_root` 或它的子件）⇒ 它们**共用同一条父链**：
        ///    根不在画布上，那 14 件一件都不可能是在。② 而 `TitleHolder` / `Alliance` /
        ///    `NotInaAllianceText` 那三棵（含其 4 颗字）的显隐是**数据驱动**的（`Apply` 按
        ///    「有没有称号 / 有没有联盟」关它们，单机 bot 恒走空串那一支）；`GetComponentInParent`
        ///    的**无参**重载默认 `includeInactive = false`（判据 = 本机 `UnityEngine.CoreModule.dll`
        ///    里那个重载的 IL：给该形参压的是 `ldc.i4.0`；⚠️ 它的**默认值语义**在本机 XML 文档里
        ///    **查不到** ⇒ **本件不拿它当判据**）⇒ 对**没激活**的件，这个口答的是「此刻可不可见」、
        ///    **不是**「建在哪个空间」。把那三棵一并纳入这条判据，`OnCanvas` 就会在「对手没称号」时变假，
        ///    而唯一的消费者（`Editor/BattleScene.cs` 里那句 `left985`，现核 `:22040`）正是拿它**选左缘
        ///    公式的档**、另有一句拿它当闸决定跑不跑那两条子画布结构断言（`if (panel985.OnCanvas)`，
        ///    现核 `:21927`；⚠️ 那份文件本会话还在被 `G2` 改 ⇒ 两个号都会漂，**按那两句内容对**）
        ///    ⇒ 会把**画布档判成世界档**（假红 + 静默跳过）。③ 余下 11 件与这 3 件同父链 ⇒ 见 ①。</para>
        /// <para>⚠️ 任一件**该有的**没建出来（贴图/字体缺失）⇒ 也返 `false`，如实回答「不成立」
        /// （同 `UnitChatPanel.OnCanvas` 的 doc）。两档答得都对：世界档根挂在裸 `GameObject`
        /// （`HudRoot`，不在任何 `Canvas` 下）⇒ 假；画布档那三条父链都通到子画布 ⇒ 真。</para></summary>
        public bool OnCanvas
        {
            get { return OnCanvasOf(_root) && OnCanvasOf(_background) && OnCanvasOf(_nameLb); }
        }
        /// <summary>逐件判据（**只此一份**）：这个节点在不在某块 `Canvas` 的父链下。
        /// 与 `SkillPanel.IsUgui` / `UnitChatPanel.IsUguiOnCanvas` / `ChatPopupPanel.IsUguiOnCanvas`
        /// 同一个口、同一条式子（那三处多一项「本件是 uGUI 后端」= `HudPiece` / `Label` 的 `IsUgui`；
        /// 本扇手上的图件是裸 `Transform`、没有 `HudPiece` ⇒ 只留父链那一项）。</summary>
        static bool OnCanvasOf(Transform t)
        {
            return t != null && t.GetComponentInParent<Canvas>() != null;
        }
        static bool OnCanvasOf(Label l)
        {
            return l != null && l.transform.GetComponentInParent<Canvas>() != null;
        }
        public Transform Root { get { return _root; } }
        /// <summary>取不到的美术名（`MenuDraw.Rect` 取不到图会**静默 return null** ⇒ 整层消失，所以自己记账）。</summary>
        public List<string> MissingArt { get { return _missArt; } }
        /// <summary>`NotInaAllianceText` 那个节点（自检：无联盟那一档该显、有联盟时该藏）。</summary>
        public Transform NotInAllianceNode { get { return _notInAlliance; } }
        /// <summary>`Alliance:` 那一组（原版字段 `allianceGO`）。</summary>
        public Transform AllianceGroupNode { get { return _allianceGo; } }
        /// <summary>`Title:` 那一组（原版字段 `playerTitleHolder`）。</summary>
        public Transform TitleHolderNode { get { return _titleHolder; } }
        /// <summary>名字那一段（原版字段 `playerName`）。</summary>
        public Label NameLabel { get { return _nameLb; } }

        // ============================================================ 建
        /// <summary>在 <paramref name="parent"/> 下建出整扇窗（**默认关着**，照原版 `Awake` 的
        /// `SetActive(false)`）。根框位置/尺寸 = 原版那一级（含「顶出屏 57.9 px」那一条）——
        /// 世界档按**绝对矩形** `RootR` 摆（`MenuDraw.ApplyPxRect` ⇒ 在 `LayoutSpace` 那个固定设计帧里，
        /// 非 16:9 会怎样**另论**，不属本件）；画布档按原版**五元组**（`aMin/aMax` + `pivot` + `ap` + `sd`，
        /// 见 `SetRootAnchor`）⇒ 两者**只在 16:9 下等价**；非 16:9 下「点锚跟屏幕左上角走」这条
        /// **只有画布档**表达得出来（差 `(屏高 − 0.5625×屏宽)/2`）。
        /// <para>🔴 **2026-10-16（`T09` 批 3）：第三个实参 `uguiHost` = 双模式开关**（见文件头那一节）。
        /// `null`（默认，= 今天唯一的调用形态 `AlliancePanelWindow.Create(root)`）⇒ **世界档**，
        /// 与加本档之前**逐位相同**；非 null ⇒ **画布档**（建在给定宿主下，走设计 px + 兄弟序）。</para>
        /// <para>⚠️ `uguiHost` 由**调用方**建/取（`HudCanvas.EnsureSubCanvas(null, <see cref="SubCanvasName"/>,
        /// HudCanvas.SortingLayer.OverHand, 0)` 的返回值）——它的名字/层/order 都取本类那两个常量，
        /// ⛔ 别在调用点另写一份。宿主不在任何 `Canvas` 下 ⇒ **出声并且不建**（半迁移态比不迁移更坏：
        /// 一张 `Image` 挂在没有 `Canvas` 的父链下**一个像素都不画**，而调用点会以为建成功了）。</para></summary>
        public static AlliancePanelWindow Create(Transform parent, string name = "Alliance Panel",
                                                 Transform uguiHost = null)
        {
            bool ugui = uguiHost != null;
            if (ugui && uguiHost.GetComponentInParent<Canvas>() == null)
            {
                Debug.LogError("[AlliancePanel] `uguiHost` 不在任何 `Canvas` 下 ⇒ **本扇窗不建**"
                             + "（⛔ 不退回世界空间 quad —— 半迁移态在 16:9 下看不出来）");
                return null;
            }
            // 🔴 **两档的根节点不同**：世界档 = **裸 `Transform`**（与改本件之前**逐字相同** —— `ApplyPxRect`
            //    里那句 `SetPxSize` 对非 `RectTransform` 是空操作，所以今天根上**没有** `sizeDelta`）；
            //    画布档 = **`RectTransform`**（画布下必须有它，否则 `RectTransform` 链会断）。
            var go = ugui ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
            var c = go.AddComponent<AlliancePanelWindow>();
            c._ugui = ugui;                       // ⚠️ 必须在 `Build()` **之前**赋值
            c._root = go.transform;
            if (ugui)
            {
                go.transform.SetParent(uguiHost, false);
                // 根框 = 原版 `RectTransform_3052` 那**五元组**（见上面那组 `RootAnchor*` / `RootPivot` /
                // `RootAnchoredPos` / `RootSizeDelta`）—— **锚点语义逐值照抄**。
                // ⛔ **不是** `HudCanvas.SetPxSize` + `PosPx`（那是「中心锚 + 固定设计 px」，与批 2/3 各扇
                //    同一套）：16:9 下与锚点档**逐位相同**，非 16:9 会**整扇跑偏**（4:3 差 **135 屏 px**）。
                // ⛔ **不是** `FillParent`（那是「铺满父件」；本件根有**自己的** rect）·
                // ⛔ **不是** `MenuDraw.ApplyPxRect`（它体内 `LayoutSpace.Px` = **÷108** ⇒ 缩成 1/108、不报错）。
                // ✅ **换枢轴【不动】子件**：下面那十几个件一律 `anchorMin = anchorMax = (0.5,0.5)`
                //    ⇒ 它们的锚点落在**父件中心**（本地坐标 `(0.5 − 父pivot) × 父尺寸`，**与父 pivot 无关**）
                //    ⇒ 换 pivot 只动「父件这块矩形摆在画布哪儿」，子件的相对落点逐件不变（`HudCanvas` 那三个
                //    `Make*InParent` 口算的正是「相对父中心」）。唯一**受**影响的是 `HitBody`（它读的是
                //    `InverseTransformPoint` = 相对**枢轴**）—— 那条已同批补了 `RootCenterLocal`。
                SetRootAnchor((RectTransform)go.transform);
            }
            else
            {
                go.transform.SetParent(parent, false);
                MenuDraw.ApplyPxRect(go.transform, parent, RootR);
            }
            c.Build();
            c.Close();
            return c;
        }

        /// <summary>🔴 把**根那一级**摆成原版 `RectTransform_3052` 的五元组（**只画布档用**）；
        /// 世界档不动（它走 `MenuDraw.ApplyPxRect` + `RootR`，逐位不变）。
        /// <para>⚠️ 父件（子画布 `Alliance Panel Canvas`）是**铺满根 canvas** 的 stretch 矩形、枢轴居中 ⇒
        /// 锚点 `(0,1)` 落在**屏幕左上角**。🔴 `Expand` 档下画布**宽恒 1920 单位**、**高随屏变**
        /// （4:3 ⇒ 1440）⇒ 「屏幕左上角」与「画布中心」**不是一回事** —— 后者正是旧写法隐含的假设，
        /// 也是那 135 屏 px 的来源。</para>
        /// <para>✔ **自证**（算式 → 报告 §④）：`(CW,CH) = (1920,1080)` 时本式给出
        /// `x[-107.000, 708.044] y[-57.936, 417.534]`，与旧写法那份 `RootR`（`x[-107.00,708.02]
        /// y[-57.93,417.52]`）**逐边差 ≤0.024 px**（`RootR` 乘过根那个 `0.9999718`、`sizeDelta` 没乘）。</para></summary>
        static void SetRootAnchor(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = RootAnchorMin;
            rt.anchorMax = RootAnchorMax;
            rt.pivot = RootPivot;
            rt.anchoredPosition = RootAnchoredPos;
            rt.sizeDelta = RootSizeDelta;
        }

        /// <summary>🔴 **面板【中心】在根本地坐标里的偏移**（本地原点 = **枢轴**、`y` **向上**、单位 = 设计 px）。
        /// <para>给 <see cref="HitBody"/> 用：`RectTransform.InverseTransformPoint` 给的是**相对枢轴**的坐标，
        /// 而原版这根 `RootPivot = (0.13, 0.825)` **不是**中心 ⇒ 这一项 = `(0.5 − pivot) × sizeDelta`
        /// = `(+301.566, −154.528)` px，**非零**。⛔ 别当成 0 —— 那是「枢轴居中」那一档的隐含假设
        /// （旧写法正是那样，所以旧代码里没有这一项）。</para>
        /// <para>⚠️ 与 `RootPivot` / `RootSizeDelta` **同一份判据**（⛔ 别在 `HitBody` 里另抄一遍数）。</para></summary>
        static Vector2 RootCenterLocal
        {
            get
            {
                return new Vector2((0.5f - RootPivot.x) * RootSizeDelta.x,
                                   (0.5f - RootPivot.y) * RootSizeDelta.y);
            }
        }

        void Build()
        {
            _missArt.Clear();

            // ---- ① 熄灯层（`Close Background`）：铺满全屏的黑 + **点它 = 关窗** ----
            // 原版：那颗 `Image` 的 `m_RaycastTarget = 1`，而「关窗」是 `Awake` 给它的 `Button.m_OnClick`
            // 挂上 `CloseButtonClick`（`BattleAlliancePanel__Awake.c` 逐句）⇒ 语义 = 「点窗外关」。
            // ⚠️ 原版那张图挂了贴图，但染色是**纯黑 α0.4627** ⇒ 乘出来 = 「黑，透明度 = 贴图 α × 0.4627」，
            //    而那张底图的 α 满幅 ⇒ 用**实底**画等价（这是**我们这处的做法**，如实标）。
            if (_ugui)
            {
                // 画布档：同一块矩形、同一份染色，只是改由 `HudCanvas` 建（设计 px + 兄弟序）。
                // 🔴 **⛔ 不建 `MenuDraw.ShadeHit` 那颗命中区**：它是 `WindowButton` + 世界空间 quad
                //    （只有壳侧 `PointerLayer` 派发），而本轮**不建 `EventSystem`、不开 `raycastTarget`**
                //    （判据 → `Battle/HudCanvas.cs:33-35`）⇒ 「点窗外关窗」在战场里照旧由
                //    `BattleDriver.HandleAlliancePanel` 用 `HitBody` 接（那条已经按档分支）。
                Img(_root, CardArt.Solid(), ShadeR, RootR, "Close Background", QShade,
                    new Color(0f, 0f, 0f, ShadeAlpha));
            }
            else
            {
                // 世界档：走公共件 `MenuDraw.ShadeHit`（它自己校验档 < `QContentMin`，不合法当场告警）。
                var shade = MenuDraw.Rect(_root, CardArt.Solid(), ShadeR, "Close Background", QShade,
                                          new Color(0f, 0f, 0f, ShadeAlpha));
                if (shade != null)
                    MenuDraw.ShadeHit(shade.transform, ShadeR, QShade, QContentMin, Close, "CloseHit");
            }

            // ---- ② 面板本体：`Background`（空节点）+ `BG`（内容底）+ `BGFrame` 四块 ----
            _background = Node(_root, RootR, "Background", BackgroundR, QBg);
            Img(_background, Art("40K_shop_offer_bg_Sororitas_0"), BgR, BackgroundR, "BG", QBg, BgTint);
            var frame = Node(_background, BackgroundR, "BGFrame", FrameR, QFrame);
            Img(frame, Art("40k_UnitChat_Background_Left"), FrameLeftR, FrameR, "BGFrame Left", QFrame);
            Img(frame, Art("40k_UnitChat_Background_Right"), FrameRightR, FrameR, "BGFrame Right", QFrame);
            Img(frame, Art("40k_UnitChat_Background_Top"), FrameTopR, FrameR, "BGFrame Top", QFrame);
            Img(frame, Art("40k_UnitChat_Background_Bottom"), FrameBottomR, FrameR, "BGFrame Bottom", QFrame);

            // ---- ③ 吸收层：点窗内空白处**什么都不该发生** ----
            // 原版：`BG` 那张图 `m_RaycastTarget = 1`，而它的父链上没有 `IPointerClickHandler`
            // （「关窗」那颗挂在**兄弟** `Close Background` 上）⇒ 射线打到面板自己 ⇒ 无事发生。
            // 我们这边命中候选只收 `WindowButton` ⇒ 不补这一层，鼠标会**穿过面板**落到熄灯层上，
            // 点窗内任意空白都会关窗。判据与三条红线 → `Shell/MenuDraw.cs` 的 `Absorb` 段。
            // ⚠️ **吸收的是 `Background` 整块**（不是原版 `BG` 那一张图的框）—— 这是**我们挑的**：
            //    原版 `BGFrame` 四块各自也是 Image（`m_RaycastTarget` 没逐块核）⇒ 取整块更保守。
            // 🔴 **2026-10-16（`T09` 批 3）**：画布档**不建**这一层 —— 它与 ① 那颗同属
            //    「世界空间命中件」（唯一消费者是壳侧 `PointerLayer`），而画布档本轮不建
            //    `EventSystem`、不开 `raycastTarget`（同 ① 那条）⇒ 「点窗内不关窗」由 `HitBody` 分支承担。
            if (!_ugui) MenuDraw.Absorb(_root, "BackgroundAbsorb", BackgroundR, QShade, QContentMin);

            // ---- ④ 头像框（`Avatar Item Small` → `Image Container` → `Border`）----
            // `Avatar Item Small` 自己是个 `Button` 但 `m_interactable = 0` ⇒ 不可点、不加命中区。
            // ⚠️ `Image`（立绘）`m_Sprite = 0`（运行期灌）· `Highlight` `m_Enabled = 0` · `Raycast Target` `act=F`
            //    ⇒ **原版出厂态就都不画** ⇒ 本件只摆框（缺口①）。
            Img(_root, Art("Player_Profile_Border"), AvatarBorderR, RootR, "Border", QAvatar,
                null, keepAspect: true);

            // ---- ⑤ `EnemyInfo`：`Name:` / 名字 · `Title:` / 称号 ----
            //  🔴 那三个 holder 在 prefab 里是 **0×0 且都落在 `PlayerInfo` 的锚点上** = `VerticalLayoutGroup`
            //     **跑之前的模板位**（`MonoBehaviour_4213`：spacing 0 · UpperLeft · `childControlWidth = 1` ·
            //      **`childControlHeight = 0`**）⇒ 按 `UguiLayout.VerticalChild` 重算：
            //       `PlayerInfo` 实绘框 = x[180.000, 556.959] y[62.498, 213.723]（RT 3363）
            //       第 0 格 `NameHolder`  高 42.573 ⇒ y[62.498, 105.071]
            //       第 1 格 `TitleHolder` 高 38.442 ⇒ y[105.071, 143.513]
            //       第 2 格 `Player Rating Display` 高 60.539 ⇒ y[143.513, 204.052]（**不建**，缺口②）
            //     ⛔ 别照抄报告里那三个重叠的 y（那是模板位）。
            var nameRow = Node(_root, RootR, "NameHolder", NameRowR, QText);
            // 🔴 **A1024③（2026-10-17）换面**：原版这七颗（`PlayerLabel` / `Name Text` / `TitleLabel` /
            //    `Title Text` / `Alliance Label` / `Alliance Name` / `NotInaAllianceText`）的
            //    `m_fontAsset` **全是 `Asar-Regular SDF`**（判据 = `menu_dump … bundle_scenes_scenes_battlearena1
            //    "Alliance Panel"` 逐颗现读；§2·5 表第一行同结论）⇒ 走 `LatinUi`。
            //    `Avatar Name` / `Individual rating value` 是 Pragati —— 但那两颗本扇**不建**（见文件头缺口②）。
            Txt(nameRow, NameRowR, NameLabelR, LocOr(NameLabelTerm, NameLabelEn), Color.white, "PlayerLabel",
                NameLabelPx, Label.VAlign.Capline, face: TmpFont.Face.LatinUi);
            // ⚠️ **只有这一颗**原版 `m_fontSizeMax` 是 **40**（其余六颗都是 35）⇒ 显式给上限，⛔ 别一刀切
            _nameLb = Txt(nameRow, NameRowR, NameTextR, "", Color.white, "Name Text", NameTextPx,
                          Label.VAlign.Capline, AutoMaxNamePx, TmpFont.Face.LatinUi);

            _titleHolder = Node(_root, RootR, "TitleHolder", TitleRowR, QText);
            Txt(_titleHolder, TitleRowR, TitleLabelR, LocOr(TitleLabelTerm, TitleLabelEn), Color.white,
                "TitleLabel", TitleLabelPx, Label.VAlign.Capline, face: TmpFont.Face.LatinUi);
            _titleLb = Txt(_titleHolder, TitleRowR, TitleTextR, "", Color.white, "Title Text", TitleTextPx,
                           Label.VAlign.Midline, face: TmpFont.Face.LatinUi);

            // ---- ⑥ `Alliance` 组：`Alliance:` / 联盟名 ----
            //  `allianceGO`（原版字段 0x48）—— **无联盟时整组关掉**（`BattleAlliancePanel__Open.c`）。
            //  ⚠️ 组里的 `Alliance Badge Drawer/{Frame,Badge}` 两张图 `m_Sprite` 也是 `0`（`AllianceBadgeDrawer.Draw`
            //     运行期灌）⇒ 出厂态不画；`Alliance Rating Display` 同缺口② ⇒ 本件只建两行字。
            _allianceGo = Node(_root, RootR, "Alliance", AllianceGroupR, QText);
            Txt(_allianceGo, AllianceGroupR, AllianceLabelR, LocOr(AllianceLabelTerm, AllianceLabelEn),
                Color.white, "Alliance Label", AllianceLabelPx, Label.VAlign.Capline, face: TmpFont.Face.LatinUi);
            _allianceLb = Txt(_allianceGo, AllianceGroupR, AllianceNameR, "", Color.white, "Alliance Name",
                              AllianceNamePx, Label.VAlign.Capline, face: TmpFont.Face.LatinUi);

            // ---- ⑦ `NotInaAllianceText`：无联盟那一行提示（**原文含 `is is` 双 is = 原版笔误，勿改**）----
            var msg = Txt(_root, RootR, NotInAllianceR, LocOr(NotInAllianceTerm, NotInAllianceEn),
                          new Color(1f, 1f, 1f, NotInAllianceAlpha), "NotInaAllianceText",
                          NotInAlliancePx, Label.VAlign.Midline, face: TmpFont.Face.LatinUi);
            _notInAlliance = msg != null ? msg.transform : null;

            Apply("", "", "");      // 出厂态 = 原版 `Awake` 那一支（`Create` 末尾再关一次）
        }

        // ============================================================ 状态 → 参数（原版 `Open` 的逐句等价物）
        /// <summary>原版 `BattleAlliancePanel.Open(name, title, rank, avatarItem, avatarItem2, allianceName, …)`
        /// 的**等价物**（`BattleAlliancePanel__Open.c` 逐句读过；头像/评级/徽章那三条见文件头缺口①②）。
        /// <para>**显隐三支**（原版就是这三条 `SetActive`）：</para>
        /// · `playerTitleHolder` —— **称号为空 ⇒ 关**；<br/>
        /// · `allianceGO` —— **联盟名为空 ⇒ 关**；<br/>
        /// · `notInAnAllianceMessage`（那个 TMP 的 `gameObject`）—— **联盟名为空 ⇒ 开**（并把文案设成
        ///   `GetTranslation("Battle/AlliancePanel/NotInAnAlliance")`），否则关。<br/>
        /// ⚠️ 原版判「称号空不空」用的是**称号**那一个实参（`param_3`）、判「有没有联盟」用的是**联盟名**
        /// （`param_7`）—— 本方法逐条照抄，⛔ 别把两者合并成一个「有没有档案」。</summary>
        public void Open(string playerName, string title, string allianceName)
        {
            gameObject.SetActive(true);
            Apply(playerName, title, allianceName);
            _visible = true;
        }

        /// <summary>原版 `BattleAlliancePanel.CloseButtonClick`（= `Close Background` 那颗钮的 `onClick`）：
        /// **只 `SetActive(false)`** —— 不清数据、不动别的窗。</summary>
        public void Close()
        {
            gameObject.SetActive(false);
            _visible = false;
        }

        /// <summary>世界坐标落在**面板本体**上吗（**不含**那张铺满全屏的熄灯层）。
        /// <para>判据 = `Build` ③ 那层**吸收区**用的**同一个矩形** `BackgroundR`（⛔ 别在这儿另写一份；
        /// 原版那边是 `BG` / `BGFrame` 几张 Image 的 `m_RaycastTarget = 1` 把它们上面的射线吃掉、
        /// 传不到底下那颗 `Close Background`；而**吸收范围取整块 `Background` 是我们挑的**，
        /// 见 `Build` ③ 那条注释）。</para>
        /// <para>用途：战斗侧 `BattleDriver.HandleAlliancePanel` 靠它分「点窗内（什么都不做）」与
        /// 「点窗外（= 原版那颗 `Close Background` ⇒ 关窗）」。⚠️ 那扇窗在 `Build` 里挂的
        /// `MenuDraw.ShadeHit` 上是个 `WindowButton`，**只有外壳那套 `PointerLayer` 会派发它**
        /// —— 战斗里不走那条路 ⇒ 关窗那一手由调用方接。</para>
        /// <para>🔴 **2026-10-16（`T09` 批 3）：本方法已按档分支**（⛔ 别只改一半 —— 那是「看得见的地方
        /// 点不到」那类静默错）：
        /// · **世界档**（`false`）= 与改本件之前**逐字相同**（`LayoutSpace.ToPixel` 折成绝对 px 再比 `BackgroundR`）；
        /// · **画布档**：面板已挂到子画布上（分辨率一变，世界 ↔ px 那条换算就**不是** `LayoutSpace` 那一套了）
        ///   ⇒ 走 **`RectTransform.InverseTransformPoint`**：把世界点投进**本面板自己的局部坐标**
        ///   （单位 = 设计 px），再与「`BackgroundR` 相对 `RootR` 的那个矩形」比。
        ///   🔴 **2026-10-16（`A14` · 锚点补正）**：那个局部坐标的**原点 = 枢轴**，而原版这根
        ///   `RootPivot = (0.13, 0.825)` **不是中心** ⇒ 先减 `RootCenterLocal`（= `(0.5−pivot)×sizeDelta`）
        ///   才是「相对面板中心」。⛔ 漏了这一项 = 命中区整体偏 `(+301.6, −154.5)` px（**静默**：
        ///   16:9 下也不对，只是「窗内点空 / 窗外点中」要正好落在边界附近才看得出来）。
        ///   判据 = 建件那一侧也是同一套（`SetRootAnchor` 的锚点语义 + 那三个 `Make*InParent` 的「相对父中心」）
        ///   ⇒ 量的就是**真正画出来的那块**。</para></summary>
        public bool HitBody(Vector3 world)
        {
            if (_ugui)
            {
                var rt = _root as RectTransform;
                if (rt == null) return false;                  // 没建起来 ⇒ 如实答「不中」
                var local = rt.InverseTransformPoint(world);   // 相对本面板**枢轴**、y **向上**、设计 px
                var cc = RootCenterLocal;                      // 🔴 枢轴 ≠ 中心 ⇒ 先挪到「相对面板中心」
                float lx = local.x - cc.x, ly = local.y - cc.y;
                float cx = BackgroundR.CX - RootR.CX;          // `BackgroundR` 相对面板中心（y 向下）
                float cy = BackgroundR.CY - RootR.CY;
                float hw = BackgroundR.W * 0.5f, hh = BackgroundR.H * 0.5f;
                return lx >= cx - hw && lx <= cx + hw
                    && ly >= -cy - hh && ly <= -cy + hh;
            }
            var p = LayoutSpace.ToPixel(world);
            return p.x >= BackgroundR.x1 && p.x <= BackgroundR.x2
                && p.y >= BackgroundR.y1 && p.y <= BackgroundR.y2;
        }

        void Apply(string playerName, string title, string allianceName)
        {
            if (_nameLb != null) _nameLb.SetText(playerName ?? "");
            bool hasTitle = !string.IsNullOrEmpty(title);
            if (_titleHolder != null) _titleHolder.gameObject.SetActive(hasTitle);
            if (_titleLb != null) _titleLb.SetText(title ?? "");

            bool hasAlliance = !string.IsNullOrEmpty(allianceName);
            if (_allianceGo != null) _allianceGo.gameObject.SetActive(hasAlliance);
            if (_allianceLb != null) _allianceLb.SetText(allianceName ?? "");
            if (_notInAlliance != null) _notInAlliance.gameObject.SetActive(!hasAlliance);

            // 🔴 换过字就要**重新左对齐一次**：`Label.SetText` 会重排，而 `RefreshBounds` 把字形块
            //    **居中**到锚点上（见 `Battle/CardDisplayWindow.cs` 的 `PlaceLeft` 那条同族教训）。
            //    ⚠️ 本句**只在窗口已激活时**才有效（TMP 在未激活时量不出宽 ⇒ `AlignLeftOn` 会自己早退）
            //    ⇒ `Open` 先 `SetActive(true)` 再调 `Apply`，顺序是死的。
            // 🔴 **2026-10-16（`T09` 批 3）**：画布档**不需要**这一手、也**不许**调它 —— 那七颗字的
            //    `anchor` 是 **(0, 0.5)**（见 `Txt`）⇒ `RefreshBounds` 让**字形块左缘恒贴节点 x**
            //    （同一个函数里 `-anchor.x*W` 那一项），换字重排也自动贴住。⛔ 调 `MenuDraw.AlignLeft`
            //    反而会把节点整颗挪走（那个口喂的是**世界单位**）。
            if (!_ugui && gameObject.activeInHierarchy)
            {
                MenuDraw.AlignLeft(_nameLb, NameTextR);
                MenuDraw.AlignLeft(_titleLb, TitleTextR);
                MenuDraw.AlignLeft(_allianceLb, AllianceNameR);
            }
        }

        // ============================================================ 助手
        //  🔴 **2026-10-16（`T09` 批 3）：下面四个助手各有两个分支**（世界档 / 画布档），
        //     ⛔ **判据各只此一份** —— 面板里十几处建件全走它们，别在 `Build` 里各写各的。
        //  ⚠️ 形参里那个 `q` 一律 = **渲染队列档**（`QShade`…`QText`）；画布档由 <see cref="ZOf"/> 取负
        //     换成兄弟序的 z（理由 → 文件头那条「队列 → 兄弟序要取负」）。
        //  ⚠️ 那个 `parentR` = **本件父节点自己的矩形**（`PxRect`，绝对 px、左上原点）：画布档要拿它算
        //     「相对父中心」的偏移；世界档用不上（那边 `MenuDraw.Local` 自己会减父件的世界位置）。

        /// <summary>面板内**绝对 px 矩形 → 相对某个父矩形的中心的偏移**（**y 向下**，单位 = 设计 px）。
        /// 画布档所有子件的坐标都由它算出来 —— 与建件口成对（给图用 <see cref="HudCanvas.MakeImageInParent"/>，
        /// 它的 `yPxFromTop` 就是**向下**；给字要取负，`MakeText` 的 py 是**向上**）。
        /// <para>⚠️ **别用 `HudCanvas.MakeImage` / `QuadFromTopPx`**：那两个把 `x/y` 当**屏幕绝对 px**
        /// （锚点 = `x/1920`、`1 − y/1080`）—— 只有在**父件恰好是整屏矩形**时才等于「相对父中心」，
        /// 而本件的 `Background` / `BGFrame` / `NameHolder` 那几个容器**都不是** ⇒ 套错了会
        /// **静默摆到别处**（`T09` §五·补⑩ 那条；先例 = `MulliganPanel.Img` / `UnitChatPanel.Quad`）。</para></summary>
        static Vector2 LocalIn(PxRect r, PxRect parentR)
        {
            return new Vector2(r.CX - parentR.CX, r.CY - parentR.CY);
        }

        /// <summary>空节点（**两档同名同 rect**）。世界档 = `MenuDraw.Node`（`ApplyPxRect`，写世界单位）；
        /// 画布档 = 中心锚 + 设计 px + 登记兄弟序。原版每个容器节点都有自己的 rect，两档都要带上。</summary>
        Transform Node(Transform parent, PxRect parentR, string name, PxRect r, int q)
        {
            if (!_ugui) return MenuDraw.Node(parent, name, r);
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            var l = LocalIn(r, parentR);
            HudCanvas.SetPxSize(rt, r.W, r.H);            // ⛔ 不是 `MenuDraw.SetPxSize`（那个 ÷108）
            // 🔴 **用 `anchoredPosition`（⛔ 不是 `localPosition`）**：锚点/枢轴都是 `(0.5,0.5)`
            //    ⇒ 锚点落在**父件中心**、与**父件的 pivot 无关**（uGUI 自己算那项 `(0.5−父pivot)×父尺寸`）。
            //    `localPosition` 的原点是**父件枢轴** ⇒ 根那一级一改 pivot（`(0.13,0.825)`），
            //    直接挂在 `_root` 下的 `Background` / `NameHolder` / `TitleHolder` / `Alliance` 四块
            //    会**整体挪 301.6 × 154.5 px**（判据 → 报告 §①「连带必修②」与 §⑥·1；那正是本批差点漏掉的一处）。
            rt.anchoredPosition = new Vector2(l.x, -l.y);
            HudCanvas.Add(rt, ZOf(q));                    // 容器的档位决定**它整棵子树**落在哪一段
            return go.transform;
        }

        /// <summary>一张图（**两档同名同 rect 同染色**）。
        /// 世界档 = `MenuDraw.Rect`（`ImageQuad` + `renderQueue`）；画布档 = `HudCanvas.MakeImageInParent`
        /// （中心锚 + 设计 px + 兄弟序）。
        /// <para>⚠️ `keepAspect` 那一支**两档共用同一份判据** `CardbackFace.Fit`（原版 `m_PreserveAspect`：
        /// 先按 sprite 的 `m_Rect` 定框、再按 `padding` 内缩并挪位）—— 算式与 `MenuDraw.Rect` 里那一段
        /// **逐字一致**（`ccx = 中心 + dox` · `ccy = 中心 − doy`），⛔ 别自己再推一遍。</para></summary>
        HudPiece Img(Transform parent, Texture2D tex, PxRect r, PxRect parentR, string name, int q,
                     Color? tint = null, bool keepAspect = false)
        {
            if (tex == null) return null;                 // 取不到图 ⇒ 那一层不画（`Art()` 已记账 + 出声）
            if (!_ugui) return HudPiece.Of(MenuDraw.Rect(parent, tex, r, name, q, tint, keepAspect));

            float w = r.W, h = r.H, dx = 0f, dyUp = 0f;
            if (keepAspect) CardbackFace.Fit(tex, r.W, r.H, out w, out h, out dx, out dyUp);
            float ccx = r.CX + dx, ccy = r.CY - dyUp;     // `dyUp` 是 uGUI 口径（向上为正）⇒ 减
            var box = new PxRect(ccx - w * 0.5f, ccy - h * 0.5f, ccx + w * 0.5f, ccy + h * 0.5f);
            var l = LocalIn(box, parentR);
            return HudCanvas.MakeImageInParent(parent, tex, name, l.x, l.y, w, h, ZOf(q),
                                               tint.HasValue ? tint.Value : Color.white);
        }

        /// <summary>一段**左对齐**的文字（原版这几颗全是 `m_HorizontalAlignment = 1`=Left）。
        /// 字号 = `px`（**已乘过 localScale 的实绘 em**）；垂直档 = 原版 `m_VerticalAlignment`。
        /// <para>🔴 **两档的单位不同、⛔ 别混**：世界档 `px` 会被 `MenuDraw` 折成**世界单位**（÷108）；
        /// 画布档 `px` **就是设计 px**（canvas 单位 = 设计 px）。喂错 = 字小 108 倍、**不报警**。</para></summary>
        /// <remarks>🔴 **2026-10-09（A1126 · A2 档）**：本函数原来**连 `autoMinPx` 都没有**（= 固定字号），
        /// 而原版这七颗 **全是 `m_enableAutoSizing = 1`**。现在补上三格 —— 取值见上面
        /// `AutoMinPx` / `AutoMaxPx` / `AutoMaxNamePx` / `AutoBasePx` 那一段（**已乘 0.8**）。
        /// ⚠️ **折行 0**：原版这七颗 `m_TextWrappingMode = 0`，而 `SetAutoFitBox` 内部那句 `SetWrapWidth`
        /// 会**无条件**把它开成 `Normal` ⇒ 传 `wrapPx = r.W` 之后**紧接着**关掉（⚠️ 必须在 ①②③ 之前 ——
        /// `SetWrapping` 会推版面，排在对齐后面会把刚摆好的位置抹掉）。
        /// ⚠️ `wrapPx = r.W` 不只是为了折行：那道闸是
        /// `wrapPx &gt; 0 ∧ autoMinPx &gt; 0 ∧ fontPx &gt; autoMinPx` **三条全真**才调 `SetAutoFitBox`
        /// ⇒ **只传 `autoMinPx` 不传 `wrapPx` = 死实参**。</remarks>
        Label Txt(Transform parent, PxRect parentR, PxRect r, string text, Color col, string name,
                  float px, Label.VAlign v, float autoMaxPx = AutoMaxPx,
                  TmpFont.Face face = TmpFont.Face.Cjk)
        {
            Label lb;
            if (_ugui)
            {
                // 🔴 **画布档那颗字的 `anchor` 是 (0, 0.5)、不是在 `MenuDraw` 那一档的 (0.5,0.5) + `AlignLeft`**：
                //    `RefreshBounds` 的算式是 `-anchor.x*W − b.min.x` ⇒ 锚点 x 取 0 时**字形块左缘恒贴节点 x**
                //    （换字重排也自动贴住，`Apply` 里那次补对齐因此**不需要** —— 见那里的分支）。
                //    ⇒ 节点 x 直接给**框左缘**（`r.x1` 相对父中心），y 给**框中心**（与 `anchor.y = 0.5` 配套）。
                //    ⛔ 先例 = `UnitChatPanel` 那三颗字（`new Vector2(0f, 0.5f)`）。
                var l = LocalIn(r, parentR);
                // 🔴 **`localPosition` 那一路要补一项**（⛔ 别删）：`Label.CreateCore` 的 uGUI 档写的是
                //    `go.transform.localPosition = pos`，而 `localPosition` 的原点是**父件【枢轴】**
                //    —— 我们的入参语义却是「相对父件**中心**」（`LocalIn` / `anchoredPosition` 那一套）。
                //    父件枢轴居中时两项同值（改本件之前就是这样）⇒ **只有 `_root` 这一个父件**（原版
                //    `pivot (0.13,0.825)`）需要加 `RootCenterLocal`。本扇窗直接挂在 `_root` 下的字**只有
                //    `NotInaAllianceText` 这一颗**；漏了它，那句提示会整块挪到面板左缘外 301.6 px、下移 154.5 px。
                //    ⚠️ 图与容器**不走这条路**（`MakeImageInParent` / `Node` 都用 `anchoredPosition`，与父 pivot 无关）
                //    —— 也正是**不能**靠 `_tmp.rectTransform` 之外的地方补的原因：一处漏、一处不错最难查。
                var fix = (parent == _root) ? RootCenterLocal : Vector2.zero;
                lb = HudCanvas.MakeText(parent, text, r.x1 - parentR.CX + fix.x, -l.y + fix.y, 5, col,
                                        new Vector2(0f, 0.5f), ZOf(QText), name, face);
                if (lb == null) return null;              // 拿不到 TMP 字体：`CreateUgui` 已出声（⛔ 不退化成点阵）
                // 字号：uGUI 档 = **设计 px**（`Label` 内部按 `PxPerUnit = 1` 算，见它那一节）
                lb.SetGlyphHeight(px);
                // 框：**设计 px** —— ⛔ 不走 `LayoutSpace.Px`（那个 ÷108 ⇒ 框变 1/108、字号收敛到荒谬的小值，不报错）。
                // ⚠️ 里面那句 `SetWrapWidth` 会**无条件**把折行开成 `Normal` ⇒ 下面紧接 `SetWrapping(false)` 还原。
                // ⚠️ min/max/base 三个实参**两档都是画布 px**（`Label.NominalPx()` 按档折算）⇒ 原样传。
                lb.SetAutoFitBox(r.W, r.H, AutoMinPx, autoMaxPx, AutoBasePx);
            }
            else
            {
                lb = MenuDraw.Text(parent, r, text, col, name, px, QText, r.W, AutoMinPx, autoMaxPx, AutoBasePx,
                                   face: face);
                if (lb == null) return null;
            }
            lb.SetWrapping(false);              // ⓪ 原版折行=0（`SetAutoFitBox` 会无条件开成 Normal）
            lb.SetAlignLeft();                  // ① 逐行左对齐（真折行时才看得见）
            // ② 垂直档（⚠️ 必须排在 ① 后面：`Left` 自带 V=Middle）。框高两个单位的换算是**两个口**
            //    （世界档走 `MenuDraw.SetVAlign` 折 ÷108；画布档直接给设计 px），⛔ 别互相套用。
            if (_ugui) lb.SetVAlign(v, r.H);
            else MenuDraw.SetVAlign(lb, v, r);
            // ③ 整块左缘贴框左缘（⚠️ 只调 ① 不够，见 `Apply` 那条注释）—— **只有世界档需要**：
            //    画布档靠上面那个 `anchor = (0, 0.5)` 表达同一件事（`AlignLeftOn` 喂的也是世界单位，⛔ 别调）。
            if (!_ugui) MenuDraw.AlignLeft(lb, r);
            return lb;
        }

        /// <summary>词条 → 文案。**远端 I2 表本地没有** ⇒ `Loc` 里没这一条时用 prefab 的静态原文
        /// （= 原版出厂就是那句英文）。⛔ **别把「查不到」写成猜测** —— 这里显式回落到原版原文。</summary>
        static string LocOr(string term, string prefabEn)
            => Loc.HasEntry(term) ? Loc.T(term) : prefabEn;

        /// <summary>取图 + 记账（`MenuDraw.Rect` 取不到图会**静默 return null** ⇒ 整层消失）。</summary>
        Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            // 三级兜底 `ui/ → ui_menu/ → ui_deck/`（与 `Shell/AllianceEventScorePanel.cs` 同一条路）
            var t = CardArt.Ui(name) ?? CardArt.MenuUi(name) ?? CardArt.DeckUi(name);
            if (t == null && !_missArt.Contains(name))
            {
                _missArt.Add(name);
                Debug.LogWarning($"[AlliancePanel] 取不到美术 `{name}` ⇒ 那一层**不画**（⛔ 不静默："
                               + "缺的是哪张见本行；名单也记在 `MissingArt` 里）。");
            }
            return t;
        }
    }

    // ============================================================ 入口（✅ 2026-10-18 · A985③ 已接）
    //  原版：HUD 那颗 `BattleHud.alliancePanelOpenButton` 点了 → `BattleAlliancePanel.Open(...)`。
    //  🔴 **那颗钮就是敌方名牌本身**（`BackCanvas/…/LeftArea/EnemyInfo`，13 场同构）——
    //     判据（rect · `m_RaycastPadding` 外扩 · 「它没有贴图」· 抬起沿）**全部写在**
    //     `Battle/BattleDriver.cs` 的 `BuildHudExtras` 建它那一段 + `HandleAlliancePanel`。
    //  ✅ 我们这边：钮建在 `BattleDriver.BuildHudExtras`、窗建在同一处（`AlliancePanelWindow.Create(root)`）、
    //     点击链 = `BattleDriver.HandleAlliancePanel`（模态，接在 `Update` 的 `HandleBattleLog` 之后）。
    //     名字那一格 = `BattleDriver.AlliancePanelName`（联机取对端真名 / 单机取名牌上那一串；
    //     ⛔ 不编造 · 称号与联盟名恒空 = **原版单机形态**）。
    //  ⚠️ **战斗里不走 `PointerLayer`** ⇒ 本窗 `Build` 里那两个 `MenuDraw` 命中节点在战场里**不会被派发**，
    //     所以「点窗外关窗」由 `BattleDriver` 那一侧接（它用本类的 `HitBody` 分窗内 / 窗外）。
    //  ✅ **2026-10-16（第四十二会话 · 收口波）：uGUI 档【已接线】—— 🔴 就地订正（铁律 5）**：
    //     本条原来写着「🔴 **待接线**（`T09` 批 3，`BattleDriver.cs` 不在本件白名单）…… 只需在
    //     建点（`Battle/BattleDriver.cs:15084`）前面加两行」⇒ **那三件今天全办完了**，⛔ 别再照它接一次。
    //     ① **接线**（`G1` · 台账 `W50`）→ `Battle/BattleDriver.cs` **现核 `:15272-15286`**：
    //        `HudCanvas.Ensure(LayoutSpace.Cam)` → `EnsureRankedSubCanvas(AlliancePanelWindow.SubCanvasName,
    //        HudCanvas.SortingLayer.OverHand, 0)` → `AlliancePanelWindow.Create(root, uguiHost: allianceCv)`；
    //        `cv == null` 那一支 = **出声 + 本扇窗不建**（⛔ 不退回世界档 —— 与同批
    //        `MulliganPanel` / `CardDisplayWindow` / `MultiCardDisplay` 三处逐字同口径）。
    //        ⚠️ 原句里那个行号 `:15084` 是**当时的**建点，后来 `G1` 改过那份文件 ⇒ 行号已漂（现核见上）。
    //        ⚠️ 配方里那句 `HudCanvas.EnsureSubCanvas(null, …)` 也**已被 `G3` 取代**：现在统一走
    //        `BattleDriver` 的私有口 `EnsureRankedSubCanvas`（按名字幂等 + **建完读回来验一次**名字）。
    //     ② **`SubCanvasRank` 补一条** → `Battle/HudCanvas.cs` 的 `SubCanvasRank` 表里那条
    //        `"Alliance Panel Canvas"`（行尾注 `// 13/14`；**现核 ≈ `:574`** —— ⚠️ 那张表连同表头 doc
    //        本会话被 `G3` 改过，行号会漂，**认「`// 13/14` 那一行」最稳**）。
    //        那条行尾注还写着：将来迁 `DebugButtons`(7)/`PopupAnchor`(9)/`Tutorial`(11)/`LoadingMenu`(12)
    //        时**一律插在它前面**。
    //     ③ **断言两处改画布档**（`G2` · 台账 `W52`）→ `Editor/BattleScene.cs` **现核 `:22040`**（`left985`
    //        的左缘：画布档那颗字 `anchor = (0, 0.5)` ⇒ **不再减半个 `WorldW`**）与 **`:22094-22113`**
    //        （`RW1192` 按 `lb.IsUgui` 取尺 —— 画布档不乘 `lossyScale × 108`）。另加了三条（子画布
    //        `overrideSorting`/层/order + **不带 `CanvasScaler`** + 逐件 `GetComponentInParent<Canvas>()`
    //        灭自证），**现核 `:21927-21980`**。
    //     ⚠️ **上面几个行号全是【现核】**，而且 `BattleScene.cs` / `HudCanvas.cs` / `BattleDriver.cs`
    //        本会话都还在被别的收口代理改（`G1`/`G2`/`G3`）⇒ 它们**还会漂**。
    //        🔴 找这几处时**按内容对**（「`left985` 那一句」「`RW1192` 那个按档 `if`」「`// 13/14` 那一行」
    //        「`EnsureRankedSubCanvas(AlliancePanelWindow.SubCanvasName` 那一句」），⛔ 别照号找。
}
