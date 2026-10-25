# F · UI 特效三组件调查（UIBorderGlow / NestedFadeGroupBase / UITransformTweenController）

> 只查不做。判据全部来自原版：① 反编译 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`（逐方法可读）
> ② `assets_full` 的实例序列化字段 ③ 实例归属 = `m_Script` PPtr → `CAB-17b1b5f8…`（monoscripts 包）
> → `m_ClassName` → `GameObject.m_Component` 反查 → RectTransform 父链。
> ⛔ 没改账、没改别人的 `.cs`。

---

## §0 一句话结论

| 组件 | 账里 | 原版实例 | 我们有没有 | 判定 |
|---|---|---|---|---|
| **UIBorderGlow** | 13 C（P1×3 · P2×10）+ 3 D | **89** | 同节点静态辉光**已画**（`ArtGlowBorder`），**呼吸动画没做** | **部分有 + 真缺口（但只有 3 处会真的动）** |
| **NestedFadeGroupBase** | 11 C（全 P2）+ 3 D | 基类 **abstract**；三个子类共 **57** | **语义已实现**（`CardView` 里按 `NestedFadeGroup.alpha` 做了整组淡入淡出） | **不是缺口（语义已覆盖）**；只有逐字复刻「组件本体」才有差 |
| **UITransformTweenController** | 9 C（P1×3 · P2×6）+ 1 D | **15** | **没有**（`RewardTransform` 我们只建空容器） | **真缺口，但 14/15 落在暂停线**；只有 **Forge 那 1 处**现在就该做 |

---

## §1 `UIBorderGlow` —— 部分有；**真正会动的只有 3 处**

### 1.1 原版是什么
`public class UIBorderGlow : UIBehaviour, IMeshModifier`（`UIBorderGlow.cs`，19 个方法/成员）：
它给 uGUI 的 `Graphic` 做**边框辉光**（`ModifyMesh` 改顶点 UV2，把边框拉伸到外扩区），
配一段「呼吸」动画（`DoAnimation` / `DealyedAnim` 协程，改 `ChangeGraphicAlpha`）。

序列化字段（`:84-88`）：**`animTime`** · **`animDelay`** · **`playOnEnable`**。

### 1.2 实例有多少、在哪、**会不会动**（这是关键）
全 84 个 bundle 扫下来 **89 个实例**：

| bundle | 数量 |
|---|---|
| `menus` | 85 |
| `generalgamewindows` | 2 |
| `scenes_mainmenuwarpforge`（主菜单场景） | 2 |

**逐实例读三个字段的分布**：

| `(animDelay, animTime, playOnEnable)` | 实例数 | 含义 |
|---|---|---|
| `(0, 1, **0**)` | **85** | **出厂静帧** —— `OnEnable` 不播，只有别处显式 `Play()` 才动 |
| `(0, 1, **1**)` | **3** | 会自己呼吸 |
| `(0, 2, 0)` | 1 | 静帧（主菜单 `Card Displayer` 那件） |

**会动的 3 处**（`playOnEnable = 1`，全是「奖励可领取」的高亮）：
- `Expansion Pass Reward Container / Collect Highlight Effect`
- `Forge Menu Reward Button / Collect Highlight Effect`
- `Raid Reward Container / Collect Highlight Effect`

代码侧的驱动点（反编译直读）：`ExpansionPassRewardItemContainer.cs:316 highlightEffect.Play()` ·
`RaidEventRewardItemContainer.cs:284 highlightEffect.Play()`；
`ForgeRewardItemContainer.cs:125/339` 是对 `rewardHighlighterForCollection.gameObject` 做 `SetActive`。

### 1.3 我们有什么
- **没有任何 `class UIBorderGlow`**（`grep class` 0 命中；5 个文件提到它，**全是注释**）。
- **同族的静态辉光已经画了**：`Shell/RaidEventWindow.cs:818-821` 建了同名节点
  `Collect Highlight Effect`（九宫格 `ArtGlowBorder`，默认隐藏）；
  `Shell/OfferContainer.cs:199-205` 与 `Shell/BoosterInfoPopup.cs:690` 都**如实标注**了「辉光动画没做」。
- 🔴 **我们自己的旧结论（2026-10-04 `A34-F3`，写在 `OfferContainer.cs:201-203`）**：
  「19 份逐份实读 `UIBorderGlow` 的三个字段 = `animDelay 0` · `animTime 1` · **`playOnEnable 0`** ⇒
  它根本不会自己动，出厂就是静帧 ⇒ 静态画在出厂色（α0.8）就是原版的静止态」。
  **本轮把这条从「19 份」扩到「全 89 个实例」复核：结论成立 —— 86/89 是静帧**，
  只有上面那 **3 个**例外。

### 1.4 判定
**部分有**：静态辉光 = 已覆盖（且是原版的静止态）；**动画缺口只在 3 处**（Expansion Pass / Forge / Raid 的「领取高亮」呼吸）。
⇒ **要做的话，范围是「3 处 × 一条呼吸曲线」，不是「重建 13 个方法」。**

---

## §2 `NestedFadeGroupBase` —— 语义已实现，组件本体不需要

### 2.1 原版是什么
`public abstract class NestedFadeGroupBase : MonoBehaviour`（`NestedFadeGroup/NestedFadeGroupBase.cs`，14 个方法/成员）。
**抽象类** ⇒ 它自己没有实例；真正挂上去的是**三个子类**：

| 子类 | 方法数 | 实例数 |
|---|---|---|
| `NestedFadeGroup`（组根） | 8 | **9** |
| `NestedFadeGroupSpriteRenderer` | 5 | **38** |
| `NestedFadeGroupTextMeshPro` | 5 | **10** |

序列化字段只有 `alpha` / `exclude`（`previousAlpha`/`previousExclude` 是运行期缓存，**不进序列化**）。

### 2.2 实例落在哪
**57 个全在 `battleprefabs_vfxandmisc` 的 `CardPrefab` 上**，路径集中在
`CardPrefab/Board Elements/3DBody/` 下：
- `TraitIcons/TraitIconContainer {1..4, Right 1..4}`（词条图标）
- `Base Health Counters` / `Base Attack Counters`（攻血计数、含 `Highlight range/melee`）

⇒ 它负责的是「**整组一起淡入淡出**」（父组的 `AlphaTotal` 乘到每个子件）。

### 2.3 我们有什么
- **没有 `class NestedFadeGroup*`**（`grep class` 0；4 个文件提到，全是注释）。
- 但**语义已经实现了**：`CardView.cs:7956/7965/8001` 那段按
  「原版 `NestedFadeGroup.alpha`」做了**整组徽标的不透明度**（`CardView` 自己那套 `_layers` 里逐层刷 α），
  判据注释就写着 `BoardTraitIcon__DOFade.c` 的 getter/setter 就是 `fadeGroup` 的。

### 2.4 判定
**不是缺口** —— 我们**不用原版 prefab**，而是重建卡体；重建的那套已经实现了「组 α 广播」这一件事。
⚠️ 唯一可能有差的是 **`exclude` / 嵌套组（`ParentGroup`）的乘算规则**：
我们没建「组的树」，只做了「一组徽标一个 α」。若哪天要复刻**多层嵌套组**，才需要补。

---

## §3 `UITransformTweenController` —— 真缺口，但 14/15 在暂停线

### 3.1 原版是什么
`public class UITransformTweenController : MonoBehaviour`（`UITransformTweenController.cs`，11 个方法/成员）。
序列化字段：`animTime` · `animPosition` · `targetPosition` · `animScale` · `targetScale` · `initializeOnAwake`。
实例样例（现读 `Forge Menu Reward Button/RewardTransform` 那一个）：
**`animTime 1.5` · `animPosition 1` · `animScale 1` · `initializeOnAwake 0` ·
`targetScale (1.12, 1.12, 1.12)` · `targetPosition (0, 217.5)`**
⇒ 就是「奖励图标**边飞边放大**到槽位」那一下（1.5 秒）。

### 3.2 15 个实例的落点（现读）

| 窗 | 实例数 | 我们的状态 |
|---|---|---|
| **Daily Reward Popup**（含 Entry / Item Drawer / Streak） | **12** | 🔴 **暂停线** |
| Expansion Pass Reward Container | 1 | 🔴 暂停线 |
| Raid Reward Container | 1 | 🔴 暂停线 |
| **Forge Menu Reward Button** | **1** | ✅ **未暂停（锻造线）** |

### 3.3 我们有什么
- **没有 `class UITransformTweenController`**（`grep class` 0；仅 `Shell/RaidEventWindow.cs:824` 一条注释提到它）。
- 我们对应节点 `RewardTransform` **只建了空容器**（`RaidEventWindow.cs:826-829`：`MenuDraw.Node(root, "RewardTransform", …)`
  + 一句「奖励物品由 `ItemDrawer` 运行时逐件实例化 ⇒ 只建空容器」）—— **没有这个补间**。

### 3.4 判定
**真缺口**，但**范围受暂停线约束**：
- **现在就该做的只有 1 处**：`Forge Menu Reward Button / RewardTransform`（锻造线未暂停）。
- 其余 14 处（Daily ×12 · Expansion Pass ×1 · Raid ×1）**等对应活动线恢复时一起做**，
  参数已经现成（每个实例的 `animTime/targetPosition/targetScale` 都能逐个读出来，做法同上）。

---

## §4 给下一手的执行建议（按收益/成本排）

1. **`UIBorderGlow` 的 3 处呼吸**（Expansion Pass / Forge / Raid）—— **最小的真缺口**：
   一条 α 呼吸曲线（`animTime 1`）+ 我们已有的静态辉光节点。⛔ 别重建 13 个方法，
   只要把 `Play()` 那一下的**观感**做出来（`ChangeGraphicAlpha` 的往返）。
2. **`UITransformTweenController` 只做 Forge 那 1 处**（1.5 s 飞到 `(0,217.5)` + 缩到 1.12）。
   其余 14 处**进暂停线台账**，恢复时按实例值逐个抄。
3. **`NestedFadeGroupBase` 不用做**（语义已实现）；只有当要复刻「嵌套组」时才补 `exclude`/`ParentGroup` 乘算。

---

## §5 本调查没查清的

1. **`UIBorderGlow` 的呼吸曲线形状没读**：`DoAnimation`/`DealyedAnim` 的反编译里有
   `NoteDecompilerIssue("Unmanaged memory load …")` 若干处，**α 的往返区间/缓动没拿到直读值**
   （只能从 `animTime`/`animDelay` 推时长）。要精确复刻得靠实机拍或逐字节读 IL。
2. **`NestedFadeGroup` 的嵌套规则**：`ParentGroup` 是运行期赋值，序列化里看不到树结构 ⇒
   「多层组怎么乘」**判不了**（我们没建组树，所以现在也没这个需求）。
3. **`UITransformTweenController` 的 15 个实例字段值我只逐读了 1 个**（Forge 那个）；
   其余 14 个的 `targetPosition/targetScale` 没逐个抄 —— 但它们都在暂停线，恢复时现读即可。
4. **`playOnEnable=1` 那 3 个之外，是否还有代码调 `Play()`**：反编译里只找到
   `ExpansionPassRewardItemContainer` 与 `RaidEventRewardItemContainer` 两处 `highlightEffect.Play()`；
   `ForgeRewardItemContainer` 是 `SetActive`。**没有第三处**（全 26k 方法 grep 过 `UIBorderGlow` 只这 3 个类持有它）。
