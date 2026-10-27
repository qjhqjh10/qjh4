# B9_W4NFG —— 卡面「嵌套淡入组」（原版 `NestedFadeGroup`）落地

> 执行代理 `B9_W4NFG` · 2026-10-27 · 批次 9
> 独占文件：`Assets/CardPresentation/Core/CardView.cs` · `Assets/CardPresentation/Editor/CardFaceAssertE5.cs`
> **未跑任何 Unity**（红线）；`typecheck` = **运行时 0 / 编辑器 0**。

## 一、结论

1. **原版判据我自己重算了一遍**（UnityPy 现读 `battleprefabs_vfxandmisc_assets_all.bundle` 的 `CardPrefab`，不是转述）：
   **57 个实例 = 9 `NestedFadeGroup` + 38 `NestedFadeGroupSpriteRenderer` + 10 `NestedFadeGroupTextMeshPro`**，
   与 `判据/NestedFadeGroup_结构判定.md` 一致。9 个组根的子树子件数 = **4 / 1 / 6×7**，也与之一致。
   **另补两条判据件没写的**（见 §五「顺手发现」① ②）：
   - 子树里实际只有 **47** 个子件（37 SR + 10 TMP），第 57 个 SR 是**孤儿 `Armour Background`**（无组根祖先）；
   - 7 个 trait 组根那 6 件里，`TraitIcon` / `Trait Not Active` 各有「**With counter / Without counter**」两套变体
     （原版靠 activeSelf 二选一）⇒ **语义角色只有 4 个**（底板 / 图标 / 角标 / ✕），所以我们的子件上限是 4 不是 6。

2. **已实现「per-group alpha」**：`CardView` 里 9 个组根各一份 `_fadeGroupAlpha`（默认全 1），
   `ApplyBadgeAlpha()` 逐组根广播给子树子件：
   - 7 个 trait 组根 → 徽标槽 0..6 的 `_badgePlates` / `_badgeIcons` / `_badgeCrosses`（MR 相对乘）+ `_badgeCounters`（TMP 绝对赋值）；
   - `Base Attack Counters` → `_stats[StatMelee]` / `_stats[StatRanged]` + `_glowMelee` / `_glowRanged`（`ImageQuad`）；
   - `Base Health Counters` → `_stats[StatHealth]`。
   **只淡某一组时，别的组一点不动**（新 API `SetFadeGroupAlpha` / `FadeGroupTo`）；
   旧的 `SetBadgeAlpha` / `FadeBadges` 保持原语义（= 原版 `FadeAllTraitsIcons` 一次推**全部 7 个** trait 组根），
   ⇒ `BattleDriver` / `BattleScene` 那些既有调用点**零改动、零影响**。

3. **断言 E 组 12 条写进 `CardFaceAssertE5`**（E1–E12，另含 4 条前提硬检查）：
   组根数/名表、逐根子件对照、只淡组 A ⇒ 其余 8 组**逐位不变**（灭自证）、
   角标 TMP 那条**独立通道**也跟着组 α、淡到 0 真的全透明、**回归保护**（`SetBadgeAlpha(0.5)` ≡ 逐组设 0.5）、
   数值组只淡自己、全部复位无残留。

4. **账补丁 `账/_parts/B9_W4NFG.tsv` = 26 行**：18 条 `NestedFadeGroup*` 的 B → **A**（落点 + 差异改写，
   把 G12 那句已证伪的「组树不嵌套」就地订正），另 8 条既有 A 行的落点行号/文案同步。
   `merge_ledger_parts.py --dry` ⇒ **更新 26 / 新增 0**；补齐后跑 `check_ledger_points --all-A` ⇒ **这 26 行全 `OK`**。

5. ⚠️ **定案不在本代理**：`CardFaceAssertE5` 我**不能跑**（红线 1）⇒ 断言未执行，等调度台在同步点跑。

## 二、证据（文件:行号）

**原版（一手）**
- 树/统计：`d:/2/.../StreamingAssets/aa/StandaloneWindows64/battleprefabs_vfxandmisc_assets_all.bundle`，
  用 UnityPy 1.25.3 现读（9 组根逐棵 DFS 展开，脚本一次性临时运行，未入库）。
- 判据整理件：`d:/4/Unity/资料/完全复刻/判据/NestedFadeGroup_结构判定.md` §二/§三。

**我们的实现**（`Assets/CardPresentation/Core/CardView.cs`）
- `:7983-7994` `ApplyTint` 尾部 —— `ApplyStatFlashes()` **先**、`ApplyBadgeAlpha()` **后**（次序是判据，见 §五 ④）。
- `:8001-8040` `ApplyBadgeAlpha()` —— 9 组根逐个广播（trait 相对乘 / TMP 绝对赋值 / `ImageQuad` 直赋 α）。
- `:8054-8070` `FadeBadges()` —— setter 现在**同时**把值写进 7 个 trait 组根（= 原版逐个 `DOFade` 的形状）。
- `:8072` `BadgeAlpha` —— doc 就地订正为「上次**统一命令**的值」。
- `:8076-8083` `SetBadgeAlpha()` —— 同步 7 个 trait 组根。
- `:14840` `public enum FadeGroup`（9 个组根；`0..8` 顺序 = 原版组根出现顺序）。
- `:14867` `GetFadeGroupAlpha` · `:14877` `SetFadeGroupAlpha` · `:14891` `FadeGroupTo`。
- `:14908` `MulLayerAlpha`（MR 相对乘）· `:14919` `SetQuadAlpha`（ImageQuad）。
- `:14928` `ForEachFadeGroupMember`（组根 → 子件表；也是自检的唯一读口）。
- `:14951` `FadeGroupName` · `:14969` `FadeGroupMemberCount` · `:14979` `FadeGroupMemberAlpha` · `:14993` `FadeGroupMemberName` · `:15001` `FadeGroupMemberBuilt`。

**断言**（`Assets/CardPresentation/Editor/CardFaceAssertE5.cs`）
- `:66` 调用点 `RunNestedFadeGroups();` · `:581` `static void RunNestedFadeGroups()`（E1–E12）。

**账**
- `资料/完全复刻/账/_parts/B9_W4NFG.tsv`（26 行）。

## 三、没查清的部分

1. **`FadeGroupTo`（补间口）没有断言覆盖**：断言只用 `SetFadeGroupAlpha`（瞬时口）。
   理由：批处理里补间要靠 `CardTween.Mode = Manual` + `Advance(dt)`，本宿主已在用这套；
   但「补间中途接力 / 起点从当前值读」这两条我没有写死断言 —— **还没验**。
2. **原版「逐帧复核」那一半没做**：`NestedFadeGroup{Base,SpriteRenderer,TextMeshPro}.LateUpdate` 每帧把被第三方改掉的
   alpha 写回；我们仍是「每次 `ApplyTint` 重算」。这是**既有差异**（旧 A 行已记），本批没动。
3. **两个 `Base …Counters` 组根永远没有驱动者**：判据（G12）说它们**没有任何序列化引用**，
   所以原版里它们的 α 也恒为 1。我们结构上已具备（`SetFadeGroupAlpha` 可用），但**没有任何调用点**去驱动它们 ——
   这与原版一致，不是缺件。真机上看不到它们淡入，是**原版行为**。
4. **未跑真机 / 未跑 Unity**：断言红绿未知；E2/E4 的前提条（徽标层、`_stats[]` 是否建出来）如果红，
   那是环境（`CardArt` / `PragatiDigits` 资源）问题，不是实现问题 —— 断言里已经把「前提不成立」写成红而不是静默跳过。

## 四、改动清单

| 文件 | 改动 |
|---|---|
| `Core/CardView.cs` | ① `ApplyTint` 尾部：`ApplyBadgeAlpha()` 挪到 `ApplyStatFlashes()` **之后**（+1 行，语义必需）<br>② `ApplyBadgeAlpha()` 从「整卡单标量」重写成「9 组根逐个广播」（8001-8040）<br>③ `FadeBadges` / `SetBadgeAlpha` 镜像到 7 个 trait 组根<br>④ `BadgeAlpha` 的 doc 就地订正（行数不变）<br>⑤ 类尾新增 204 行：`FadeGroup` 枚举 + 通道/补间/广播/自检读口（含判据整树注释）<br>🔶 只有 1 处打了 `自己设计` 标记（`FadeGroup` 枚举名，原版只有组件实例没有可枚举 id） |
| `Editor/CardFaceAssertE5.cs` | 新增 E 组（`RunNestedFadeGroups`，12 条 + 4 条前提硬检查）+ 调用点；CRLF 保持 |
| `账/_parts/B9_W4NFG.tsv` | 26 行账补丁（新建） |
| `报告/B9_W4NFG_NestedFadeGroup.md` | 本文件（新建） |

**没碰**：`Battle/BattleDriver.cs` · `Core/CardFeel.cs` · `Battle/AnimFXController.cs` · `Hand/CardInteraction.cs` ·
`Core/CardHighlight.cs` · `Assets/ThirdParty/**` · `Deck/**` · `Shell/*` · `Core/CardArt.cs` · 其它自检宿主 · git · 两张正本 · 主账。

## 五、顺手发现

1. 🔴 **判据件里的「38 SR」= 37 子树子件 + 1 孤儿**。引「子树子件数」时**别用 38**：子树里 SR 只有 37、TMP 10、合计 **47**
   （+9 组根 = 56，再加孤儿 1 = 57）。判据件 §三 那张逐根表是对的（4/1/6×7 = 47），只是 §一/§三 的 57 分解容易被读成「38 都在子树里」。
2. 🔴 **7 个 trait 组根各 6 件里有一半是「变体」**：`Without counter/{TraitIcon, Trait Not Active}` 与
   `With counter/{TraitIcon, TraitCounter, Trait Not Active}` 是**并排两套**、原版靠 activeSelf 二选一
   ⇒ **语义角色 4 个**。所以「我们 4 件 vs 原版 6 件」**不是缺件**，别按 6 去补。
3. 🟡 **旧 `ApplyBadgeAlpha` 有两个小残留缺陷，本批顺带修掉**（写在这里，⛔ 免得下个会话当成「我们故意那样」）：
   - `if (_badgeAlpha >= 1f) return;` ⇒ 补间最后一帧落到 1.0 时不写角标 TMP ⇒ **角标停在 ≈0.99**（肉眼不可见，但读数不对）；
   - 同一句早退 ⇒ α=1 时角标**不跟 `Greyed()`**，整卡灰化时角标字**不变灰**（图标走 `_GreyScale` 会变）。
   新实现里角标**每次都绝对赋值**，两条都消失。
4. 🔴 **组 α 必须排在 `ApplyStatFlashes()` 之后**：那一步会把 `_stats[]` 刷回「状态色 × 闪现色」，
   组 α 提前乘会被抹掉 —— 而且**症状是静默的**（徽标那 7 组照常工作、只有 2 个数值组不淡）。
   旧代码里 `ApplyBadgeAlpha()` 本来就在前面（那时它不碰 `_stats[]`，所以没暴露）。
5. 🟡 **本批造成 36 条既有 A 行的行号漂移**（都是 `CardView.cs` 里 `>8001` 的落点）：
   `--fix` 可按符号名重定位（我改的 8 条已在补丁里改好）。另有 **2 条 range 落点失败**
   （`CardCollider2D.Toggle` 的 `9324-9340` / `CardHighlight.SetCombatHighlights` 的 `8421-8449`）是**既有的**
   —— 改前改后 fail 计数都是 16，不是本批引入。
6. 🟡 `_stats[]` 的组 α 走的是**相对乘**，所以它乘在 `ApplyStatFlashes` 算完的「状态色 × 闪现色」之上 ——
   受击变色的**颜色**不受影响（只动 α），这个次序别调换。
7. 📌 **孤儿 `Armour Background`（SR，无组根祖先）我们本来就没有对应物**：`_armourIcon` 不在任何组里，α 恒 1 ——
   与原版一致（原版它 `ParentGroup = null`、`AlphaTotal` 恒 1）。

## 六、组根 → 子件对照表

「原版子树件数」= 该组根子树里 NFG 家庭子件个数（§一 的重算值）；
「我们建了几个」= 我们这一侧真建出来的件数（懒建的那几件没触发时会是 0/少几件）。

| # | 原版组根（`CardPrefab` 下路径） | 原版子树件数 | 我们对应哪个节点 | 建了几件 | 缺哪几件 / 说明 |
|---|---|---|---|---|---|
| 0 | `Board Elements/3DBody/TraitIcons/TraitIconContainer 1` | 6 | 徽标槽 0：`_badgePlates[0]`(IconBackground) · `_badgeIcons[0]`(TraitIcon) · `_badgeCounters[0]`(TraitCounter) · `_badgeCrosses[0]`(Trait Not Active) | 3（✕ 惰建） | 原版 6 里 2 件是「带角标/不带角标」变体 ⇒ 角色 4 件，**不缺** |
| 1 | `…/TraitIconContainer 2` | 6 | 徽标槽 1（同上四件） | 3（✕ 惰建） | 同上 |
| 2 | `…/TraitIconContainer 3` | 6 | 徽标槽 2（同上四件） | 3（✕ 惰建） | 同上 |
| 3 | `…/TraitIconContainer Right 1` | 6 | 徽标槽 3（同上四件） | 4（本宿主第 4 枚徽标是未激活 ⇒ ✕ 建了） | 同上 |
| 4 | `…/TraitIconContainer Right 2` | 6 | 徽标槽 4 | 0（本次探针只有 4 枚徽标） | **没建**：槽 5/6/7 没有徽标 ⇒ 建层是懒的（原版也是按关键词数开位） |
| 5 | `…/TraitIconContainer Right 3` | 6 | 徽标槽 5 | 0 | 同上 |
| 6 | `…/TraitIconContainer Right 4` | 6 | 徽标槽 6 | 0 | 同上 |
| 7 | `Board Elements/3DBody/Base Attack Counters` | 4（`Melee AttackText`[TMP] · `Highlight`[SR] · `Range Attack Text`[TMP] · `Highlight ranged`[SR]） | `_stats[StatMelee]` · `_stats[StatRanged]` · `_glowMelee` · `_glowRanged` | 4（后两件靠 `SetOwnAttackType` 唤醒，断言里显式点了 Melee） | 两个底光是懒建的（`EnsureGlow`）；取不到 `40K_melee_glow`/`40K_ranged_glow` 时会少 2 件并**出声** |
| 8 | `Board Elements/3DBody/Base Health Counters` | 1（`HealthText`[TMP]） | `_stats[StatHealth]` | 1 | 无 |
| — | （孤儿）`Armour Background`[SR]，**无组根祖先** | — | 无对应物（`_armourIcon` 不属于任何组，α 恒 1） | — | **与原版一致**：原版它 `ParentGroup = null`、`AlphaTotal` 恒 1 |

**汇总**：组根 **9/9** 已建模。本次探针（4 枚徽标 + 点了 Melee）实建子件 = 槽 0/1/2 各 **3** + 槽 3 **4** + 槽 4/5/6 **0**（没有徽标）+ `Base Attack Counters` **4** + `Base Health Counters` **1** = **18 件**。
「缺」的全部是**这一档没触发懒建**（槽 4/5/6 没有第 5~7 枚徽标、底光要等 `SetOwnAttackType`）——**不是实现缺**；断言 E2 会把这 9 行逐根打一张对照表进日志（含 `〔缺:节点名〕` 占位），所以「数不出来」时日志里**一定写明缺哪几个**。
