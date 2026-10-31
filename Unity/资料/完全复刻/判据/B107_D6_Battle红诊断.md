# B107_D6 · `BattleScene` 两条红诊断（只读）

> 输入：`d:/4/_tmp_view/battle.log`（全量自检 `BattleScene` = **7582 通过 / 2 失败**）。
> 只读诊断：⛔ 未改任何源码 / 账 / 正本；⛔ 未跑 Unity。
> 结论口径：**α** = 断言错（期望值/判据/文案写错）· **β** = 实现缺陷 · **γ** = 本批回归 · **δ** = 夹具前提不成立。

| # | 断言 | 判定 | 一句话 |
|---|---|---|---|
| 1 | `trait 文案表 62 条（实际 65）` | **α** | 期望值钉在**旧 json 的条数**上；权威源是 `Core/Loc.cs` 的 `Card_Trait/*` 键数（现读 65）。数据侧是本批 `B107_W23` 的**正当改善**，不是回归。 |
| 2 | `T21-6 锚 = 手牌卡`（实得 (3.74,-4.15,0.24) / 期望 (3.76,-3.91,0.33)） | **δ**（+文案 α） | 实得那一拍锚在**这张卡的场上档锚**，不是手牌档 —— 差值 `(-0.02,-0.24,-0.09)` 与 `BoardEffectAnchorLocal × 0.73005` **逐位吻合**。原因是**夹具把 `AnimAnchors.OnBoardOf` 钉成 `_ => true`**，而 `sp=2 abilityTarget` 无目标时 `AnimAnchors` 会**退到行动卡自己**并听这个钩子。生产代码没错；与 `B107_W29` 的延迟档改动**无关**（那一拍是同步播的）。 |

---

## 失败 1 · `trait 文案表 62 条（实际 65）`

### 结论：**α（断言期望过期）** —— 不是 β，也不是 γ

数据侧是**对的**：`B107_W23` 把 `Resources/trait_tips.json` 按 `工具/gen_trait_tips.py` 重生成到 **65 条**，而这 65 条**恰好等于** `Core/Loc.cs` 里 `Card_Trait/*` 这一族键的条数（= 生成器的取键源）。

### 证据

| 事实 | 出处 |
|---|---|
| 红行 | `d:/4/_tmp_view/battle.log:125901` |
| 断言本体 `Check(TipText.TraitCount == 62, …)` | `MyGame/Assets/CardPresentation/Editor/BattleScene.cs:25278`（文案 `:25279-25281`） |
| `TraitCount` = json 的 entries 条数 | `Core/Tooltip.cs:1040`（`{ LoadTips(); return _tips.Count; }`）；装载 `:1020-1036` |
| 现 json = **65** 条（`src`：official **61** / none **4**） | `Resources/trait_tips.json`（现读；`entries` 数组） |
| 现 `Core/Loc.cs` 的 `Card_Trait/*` 键 = **65**（去重后仍 65） | 现数 `Loc.cs`；与 json 的 65 个 `key` **逐个同名** |
| 旧（HEAD）json = **62** 条（official 56 / rulebook 5 / none 1），**缺 `melee` / `ranged` / `sneakattack` 三个键** | `git show HEAD:Unity/MyGame/Assets/CardPresentation/Resources/trait_tips.json` |
| 旧那 5 条 `rulebook`（`ferocity`/`huntmark`/`oath`/`pack`/`sentry`）**现已全部命中官方** | 新旧 json 逐键对比（现读） |
| 生成器取键 = `read_loc_traits()`（`Card_Trait/*`），打印“规范键 %d 个（`Core/Loc.cs` 的 `Card_Trait/*`，现读）” | `Unity/工具/gen_trait_tips.py:106-116, 165-171, 231` |

⇒ 65 不是“多出来了”，是**旧 json 比 `Loc.cs` 少 3 个键**（`melee`/`ranged`/`sneakattack`），且旧的 5 条规则书兜底**已全部升级成官方正文**（official 56→61）。**内容上这是一次净改善**，`62` 只是旧产物的尺码。
另外 4 条 `src: none`（`ability` / `melee` / `ranged` / `sneakattack`，正文空）**不是本条红的原因**：`ability` 在旧 json 里也是 `none`，另外 3 条旧 json 里**根本没有**。

### 最小修法（可粘贴）

**首选（期望值来自 `Loc.cs` 那一族键，⛔ 不写死 65）**：给 `Core/Loc.cs` **追加一个只读口**（**加在类尾** —— `账/*.tsv` 按 `文件:行号` 引用，插在中间会让下游行号漂移）：

```csharp
/// <summary>「规范关键词键」的条数 = 手写表里 `Card_Trait/*` 那一族键。
/// <para>这是 `TipText.TraitCount` 的**唯一期望源**：`工具/gen_trait_tips.py` 的取键也走这一族
/// （`read_loc_traits()`）。⛔ 别在断言里再写死数字（2026-10-31 `B107_D6`：写死的 62 在换源/补键后必红）。</para></summary>
public static int CardTraitKeyCount
{
    get
    {
        int n = 0;
        foreach (var kv in Table)
            if (kv.Key.StartsWith("Card_Trait/", System.StringComparison.Ordinal)) n++;
        return n;
    }
}
```

然后把断言换成（`Editor/BattleScene.cs:25278-25281` 整段替换）：

```csharp
            // ① 文案表本身：**从规范键表生成的**，不是手抄进 C# 的 —— 期望值取 `Loc` 的 `Card_Trait/*` 键数（现读 65）
            Check(TipText.TraitCount == Loc.CardTraitKeyCount,
                  $"trait 文案表条数 = `Card_Trait/*` 规范键数（实际 {TipText.TraitCount} / 期望 {Loc.CardTraitKeyCount}）"
                + "—— 🔴 2026-10-14（`W4`/`A1803`）换源后语义是「**官方 `CardTraitDescription/*` 的规范键条数**」；"
                + "2026-10-31（`B107_W23`）按 `gen_trait_tips.py` 重生成：official 61 / none 4（`ability`/`melee`/`ranged`/`sneakattack`）");
```

**备选（不许动 `Loc.cs` 时）**：写成一个**有名常量 + 出处注释**（`const int ExpectedTraitTips = 65; // 见 工具/gen_trait_tips.py 输出：Loc.cs 的 Card_Trait/* = 65`），仍比裸 62 可复核。

### 还差什么

- 这 4 条 `src:none` 要不要补正文（原版 `CardTraitDescription/*` 没有、规则书表也匹配不上）**是另一条账**，与本条红无关；要判只能回官方词条表 + `资料/关键词图标/_规则书关键词表.md` 逐词核，⛔ 别在这里猜。
- 我**没有跑 Unity** 验证改完转绿；结论来自静态数据对账（json ↔ `Loc.cs` ↔ 生成器）。

---

## 失败 2 · `T21-6 锚 = 手牌卡`（实得 (3.74, -4.15, 0.24) / 期望 (3.76, -3.91, 0.33)）

### 日志完整行（前后）

`d:/4/_tmp_view/battle.log:242317`（这一拍的**唯一**一次实例化）

```
BT    [特效] Card_Buff_Hand @ (3.74, -4.15, 0.24) → Card_Buff_Hand（活着的播放器 195 个）
```

`d:/4/_tmp_view/battle.log:242354`（红行，全文）

```
BT    ✗ [B107_W8] ★ T21-6 锚 = **手牌卡**（`sp=2 abilityTarget` 而事件**没有目标** ⇒ `handAnchored` 的降级那一跳；原版那一格是抛异常/不返回，我们选降级回手牌档）（实得 (3.74, -4.15, 0.24) / 期望 (3.76, -3.91, 0.33)）
```

前后两条（同段）都比这条红早/晚，**均绿**：`:242340` 播 1 次 ✓ · `:242368` 父层 ✓ · `:242382` ≠ 中央展示位 ✓。

### 结论：**δ（夹具前提不成立）** + 同一条断言的文案/灭自证是 **α**

实得与期望的差 = **(−0.02, −0.24, −0.09)**。把它和这张卡的两颗锚点差比一下：

| 量 | 值 | 出处 |
|---|---|---|
| `CardView.BoardEffectAnchorLocal` = `From3DBody(0, 1.34, −0.128)` = `(0, 1.34 − Height/2, −0.128)`，`Height = 3.3313` ⇒ `(0, −0.32565, −0.128)`（**卡根空间**） | — | `Core/CardView.cs:2123`（`Height` 见 `:303`） |
| 手牌卡世界缩放 = `HandLayout.DefaultCardScale` = **0.73005**（16:9 下 `LayoutSpace.Scale = 1`） | — | `Hand/HandLayout.cs` 的 `DefaultCardScale`；`Core/LayoutSpace.cs:121-124` |
| ⇒ 场上档锚 − 手牌档锚（卡根未旋转时）= `(0, −0.2377, −0.0934)` | **实测差 = (−0.24, −0.09) 的 y/z** | — |
| x 那 0.02：扇面倾角把局部 y 转进 x（≈4.8°） | — | 手牌 `RotationAt` 的扇形角 |

⇒ **实得那一拍锚在「这张卡的场上档锚 `EffectAnchorBoard`」，不是手牌档 `FaceRoot`。** 0.24 不是容差问题（容差是 `sqrMagnitude <= 1e-6`，见 `Editor/BattleScene.cs:50135-50139`），也不是手牌锚公式算错。

**为什么会落到场上档**（链条逐句）：

1. `PlaySignal` 的「手牌里那张卡发的一拍 Trigger」把 `handAnchored: true` 传下去（`Battle/BattleDriver.cs:23362-23381`）；
2. `PlayAbilityBeat` 先解起锚：`ResolveAnimAnchor((PositionOption)sp=2, …)`（`BattleDriver.cs:23646`）；手牌档那两句覆盖只对 `sp == self(0)` 与 `anchor == null` 生效（`:23652-23659`），`sp=2` 两处都不碰；
3. `AnimAnchors.Resolve` 的 `abilityTarget` 分支：**`target` 为 null 时退到 `self`**（`Battle/AnimAnchors.cs:171-172`），然后 `CardAnchor` 按 `OnBoardOf` 二选一（`:275-284`）；
4. 本测试在 `sp=2` 这一段**把钩子钉成“在场上”**：`AnimAnchors.OnBoardOf = _ => true;`（`Editor/BattleScene.cs:50473`）⇒ `EffectAnchor(true)` = **场上档**；
5. 生产里钩子是 `AnimAnchors.OnBoardOf = IsViewOnBoard`（`BattleDriver.cs:3800`）：手牌卡不在 `_myUnits/_foeUnits` ⇒ `false` ⇒ `FaceRoot` = **手牌档**，与原版 `CardScript.GetEffectAnchor`「按卡状态：inHand 那几档取 `2DCard`」一致（`Core/CardView.cs:2093-2164`）。

⇒ **实现是对的**；测试为了证明 `handAnchored` 接线而钉死钩子，恰恰把 `sp=2` 这条**从手牌档掰成了场上档**。

**与 `B107_W29` 无关（明确排除 γ）**：W29 的 C-4 改的是**延迟档**（`ScheduleVfxCue(..., Transform owner, bool requireOwner)`，见 `git diff BattleDriver.cs`）。这一拍 `Card_Buff_Hand` 是**同步**播的 —— 日志栈 `:242317` 那段逐帧写着 `PlaySignalForTest` → `PlaySignal`(`BattleDriver.cs:23381`) → `PlayPlacedPieces`(`:24158`) → `PlayPlacedOne`(`:24212`) → `PlayAbilityBeat`(`:23727`) → `CardEffects.Fire`，全程在 `CheckT21TriggerAnchors`(`BattleScene.cs:50476`) 的调用链里，**没有经过时间线排队**。

**顺带（这条断言的文字也是 α）**：

- `sp=2 abilityTarget` 无目标时**不是**走 `PlayAbilityBeat` 里 `anchor == null && handAnchored` 那条降级 —— `AnimAnchors` 对 `abilityTarget` **永远不返回 null**（它换成 `self`）。那条降级只有 **`sp=1 cardTarget`** 够得着（`CardAnchor(ctx.target=null)` → `Missing()` 返回 null，见 `AnimAnchors.cs:166-167, 370-376`）。
- 所以 `BattleDriver.cs:23654-23655` 那句注释「`1 cardTarget` / `2 abilityTarget` 在**没有目标**时会解析成 null」**对 `2` 不成立**（需就地订正，铁律 5）。
- `★ T21-6 挂在那张手牌卡下（父层 0 = …）`（`BattleScene.cs:50490`）与 `★ 灭自证 ≠ 中央展示位`（`:50493-50496`）在这条上**都没有判别力**：场上档锚的 GameObject 也是层 0，它也不等于中央展示位 —— 所以它们双双为绿，却盖不住 0.24 的错锚。

### 最小修法（可粘贴）

`Editor/BattleScene.cs:50473`（只改这一行 + 下面两条文案）：

```csharp
                            // ⛔ 别钉 true：`abilityTarget` 无目标时 `AnimAnchors.Resolve` 退到**行动卡自己**（`AnimAnchors.cs:171-172`），
                            //   而那一跳按 `OnBoardOf` 二选一 —— 钉成 true 会取**场上档**（偏差 = `BoardEffectAnchorLocal`×卡缩放
                            //   = (0,−0.238,−0.093)），而本条要验的是**手牌档**。手牌卡的自然状态 = false（生产 `OnBoardOf = IsViewOnBoard`）。
                            AnimAnchors.OnBoardOf = _ => false;
```

`Editor/BattleScene.cs:50487-50489`（文案）：

```csharp
                            check(FiredAtB107("Card_Buff_Hand", hand6.position),
                                  Tag + "★ T21-6 锚 = **手牌卡**（`sp=2 abilityTarget` 而事件**没有目标** ⇒ `AnimAnchors` 退到行动卡自己的 `GetEffectAnchor`，"
                                      + "手牌卡 ⇒ 手牌档；判据 `CardScript__GetEffectAnchor.c:9-19`）"
                                      + $"（实得 {PosTextB107("Card_Buff_Hand")} / 期望 {hand6.position}）");
```

`Editor/BattleScene.cs:50493-50496`（把「灭自证」换成**这条上真能判别**的一半）：

```csharp
                            check(FiredPosOf("Card_Buff_Hand").HasValue
                                  && (FiredPosOf("Card_Buff_Hand").Value - hv6.EffectAnchor(true).position).sqrMagnitude > 0.0025f,
                                  Tag + "★ T21-6 **灭自证**：≠ 这张卡的**场上档锚**（`EffectAnchor(true)`）—— "
                                      + "把 `OnBoardOf` 钉成 true（或把 `GetEffectAnchor` 写成恒取场上档）⇒ 红");
```

### 还差什么

- 我**没跑 Unity** 验证改完转绿；但差值 `(−0.24, −0.09)` 与 `BoardEffectAnchorLocal × 0.73005` 的 `(−0.2377, −0.0934)` 逐位吻合，定位已足够硬。
- 若将来真想给 `anchor == null && handAnchored` 那条**降级**配断言，得找一张 `sp = 1 cardTarget` 且事件无目标的卡（或走 `PlayAbilityBeatHandForTest` 的探针口，`BattleDriver.cs:39345-39350`）—— 用 `sp=2` 的 `AM56` 结构上测不到。
