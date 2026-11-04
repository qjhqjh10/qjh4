# B139_E — `Battle/PopNumber.cs` + `Core/CardFeel.cs`（`B138` 派活索引 W5）

> 执行代理 `B139-E` · 2026-11-04 · 包 E。
> **改动文件**：`Unity/MyGame/Assets/CardPresentation/Battle/PopNumber.cs` · `Unity/MyGame/Assets/CardPresentation/Core/CardFeel.cs`
> **新增文件**：`Unity/MyGame/Assets/CardPresentation/Editor/B139_E_Assert.cs`（+ `.meta`）· `Unity/资料/完全复刻/账/_parts/B139_E.tsv`
> **没碰**：`CardView.cs` / `BattleDriver.cs` / `RuleEngine/**` / `WarpforgeVFX/**` / `Editor/BattleScene.cs` / `Editor/CardBaseDemo.cs` / 活状态表 / 正本 / git / Unity。
> `TMPDIR=/tmp/wf_e bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

---

## 一、结论

| 项 | 数 |
|---|---:|
| 本包条目 | **8** |
| 改到位 | **8**（其中 `5-13` 的**生产路径**要跨包接一行，见 §四） |
| 新断言 | **8 组 E1–E8（Check 调用点 41 处 ＋ 7 处 Near，全走同一个 Check）** |
| 存疑 / 未验 | **3**（见 §五） |
| 跨包待接线 | **3**（见 §四） |

---

## 二、逐条

### `1-01` 伤害飘字多一个 `-` 前缀 ✅
- **判据**：`decomp_full/CardDamageCounterController__DamageReceived.c:10-16` —— `SetText(Int32.ToString())` **直接**塞 TMP，无任何符号前缀；prefab `MonoBehaviour_5890981294211439552.json` 的 `m_text = "-5"` 只是占位（运行时被覆盖）。
- **改法**：`PopNumber.cs:236` `string txt = heal ? ("+" + Mathf.Abs(amount)) : Mathf.Abs(amount).ToString();`（治疗档的 `"+"` 保留 —— 原版 `CardScript._Heal…:155-204` 是 `"+"+量`）。
- **断言**：E1 / E1b / E1c（伤害 `"7"`、治疗 `"+7"`、且**不是** `"-7"`）。

### `1-02` 飘字每次新建节点，不重播同一颗 ✅（API 侧完成；生产路径要跨包传 key）
- **判据**：同 `__DamageReceived.c:10-18` —— 每卡**只有一颗** `DamageCounter`（`BattleCardUI +0x1a8`），再中一次 = `SetActive(true)` + `SetText` + `Animation.Play()`（**重启同一节点**）。
- **改法**：`PopDamage` / `PopHeal` 新增可选参 `object key = null`（`PopNumber.cs:216` / `:223`）。带 key ⇒ 走复用表（伤害 / 治疗**各一张**，原版是两颗不同的 TMP）：
  - `:247` 查表 → `:263-270` 杀掉上一条补间、换文本、复位位姿与显隐、`ReusedCount++`；
  - `:341` 整条时间轴由 `BuildPopSequence`（`:371`）**重播**（= `Animation.Play()`）；
  - `:345-358` 收尾 **只藏不毁**（原版那颗是常驻节点）；不带 key 仍是「每次新建、播完销毁」（自检里「同帧连建两颗比字号」那批靠这一档）。
- **断言**：E2（同一 key 两次 ⇒ **同一个 `Label`**、文本刷新、α 复位、`ReusedCount==1`）· E2e（不传 key ⇒ 两个不同节点=灭自证）· E2f（治疗档独立复用）。
- ⚠️ **生产路径要接线**：`BattleDriver.cs:25500` 现在调的是 `PopNumber.PopDamage(transform, popAt, e.Amount, enemy: popFoe)` —— **不传 key** ⇒ 行为等于改前。要让易伤两拍真的只显示一颗，需在那一行加 `key: v`（`v` = 受击那张卡，同一段上文已有）。**跨包 A，见 §四**。

### `5-02` 伤害飘字缺描边 ✅
- **判据**：Damage TMP 的 `m_sharedMaterial` = `-455814643003338116` → `bundle_fonts_assets_all/Material/Material_-455814643003338116.json`（「Pragati-Regular Atlas Material Thick Outline」）实读 `_OutlineWidth 0.097` · `_OutlineColor (0,0,0,0.7882353)` · `_FaceDilate 0.105` · `_GradientScale 21`。
- **治疗档不贴**：Heal TMP 的材质是 `-5050103597772954521`（「Pragati-Regular Atlas Material」）实读 `_OutlineWidth 0` / `_FaceDilate 0` ⇒ **⛔ 不一刀切**。
- **改法**：`PopNumber.cs:313` `if (!heal) ErrorMessageBanner.ApplyBannerOutline(lbl);` —— 复用已有那份配方（**同一份材质 pid**，铁律 6：判据只留一处）。
- **断言**：E3/E3b/E3c/E3d（三项值 + `OUTLINE_ON`）· E3e（治疗档 `_OutlineWidth = 0`，灭自证）。

### `5-13` 治疗图标 `HealIcon` 未建 ✅（`PopNumber` 这一档建完；生产路径跨包缺一行）
- **判据**：`CardPrefab.prefab` 的 `HealIcon`（`RectTransform` fileID 7056487514831563120 的父件 `HealText`，渲染件 3355248066842936500 实读）—— sprite `Card Heal` 256² · `m_Color (1,1,1,0.853)` · `sortingOrder 100` · `localScale 0.56046` · 自己 `localPosition (0,0,0)`（那一跳 z 在父件 `HealText` 的 `localPosition.z = −0.05`）。
- **改法**：`PopNumber.cs:556`（`HealIconScale` / `:558` `HealIconOrder` / `:560` `HealIconAlpha` / `:553` `HealIconLocalPos`）+ `CreateCounterIcon`（`:615`，从效果库的 `CardPrefab` 取现成节点）+ `ResetCounterIcon`（`:677`）+ 生产接线口 `AttachHealIcon(Transform healTextRoot)`（`:657`）。
  - ⚠️ 它**没有动画**（原版治疗那一下只 `DOScale` 父件 `HealText`，α 恒 0.853）⇒ 本文件不给它挂补间。
- **断言**：E4（节点在 / order 100 / α 0.853 / scale 0.56046×桥 / z −0.05×桥）· E4a2（伤害档不建它，灭自证）。
- ⚠️ **生产路径仍缺**：真正的治疗飘字走 `CardView.ShowHeal` → `Core/CardView.cs:9149（BuildHealText）`，那里**只建 TMP、没建这颗 sprite**。补齐 = 在 `BuildHealText` 末尾加一行 `PopNumber.AttachHealIcon(_healText.transform);`（API 已就位）。**跨包 B，见 §四**。

### `11-01` 伤害血痕缩放的切线装反了 ✅
- **判据**：`AnimationClip_-3234148433819516763.json` 的 `m_ScaleCurves[path="DamageIcon"]` 段 `[0.11666667, 0.2]` —— **首键（0.5763395）`inSlope = outSlope = 0`**、**末键（0.7）`inSlope = 0.06277556`、`outSlope = 0`**。
- **改法**：`PopNumber.cs:486（IconScaleRamp）` —— `inSlope` 改 **0.06277556f**（原来写 0.0628 且挂在首键）、关键帧改成
  `new Keyframe(0f, 0f, 0f, 0f)` + `new Keyframe(1f, 1f, m1, 0f)`。
- **注释一并订正（铁律 5）**：`:460-461` 的「首端 `inSlope = 0.0628`、末端 0.0」改成「首键两端 0、末键 `inSlope = 0.06277556`」；`IconScaleRamp` 的 doc 写明「原来写的是错的（装反了）+ 反证」。
- **断言**：E5（首键出切线 = 0）· E5b（末键入切线 ≈ 0.0423）· E5c（**形状判别式**：`u=0.5` 处 0.4947；切线装反是 0.5053）。

### `11-06` `Impact Light Tween` 三条补间「零消费者」 —— **判据订正：不是缺实现** ✅（数据已逐值核实 + 断言钉死）
- **审计原判**（`B135` 动画 #14/#15/#16）：常量在、补间没跑 ⇒ 🟠/⛔缺实现。
- **现核结论：不成立**。那三条补间走的是**数据驱动**那条链，不是 `CardFeel` 的手写常量：
  1. `CardPresentation/Resources/UnitTweens.json` 有 `Impact Light Tween` 的**四条**，逐值 = `数据/游戏数据/tween/Impact Light Tween.json`（0.3 s 位置 punch / 0.5 s **InQuad** 旋转 punch / ResetBody 0.25 ×2）；
  2. `UnitTweenTable.Build` 对 `punch` / `resetbody` 都有实现（`Core/UnitTweenTable.cs:250-266 / 499-527`）；
  3. 入口已挂：`BattleDriver.cs:28725` 的 `UnitTweenRuntime.Install()` → `WFModuleTween.OnInvoke`；
  4. 池内 **73 个**效果引用它（`animfx_modules.json`：56 `BulletImpact_*` + 12 `Lasrifle*` + 5 其它）。
- **改法**：**不加第二条手写补间**（那会同一播两遍）。只在 `Core/CardFeel.cs:144` **就地订正那条注释**（原文「将来做远程弹着要用」⇒ 写明这条数据链已通、常量只是读数留痕、⛔ 别手写第二份），并写进账（`特效动画` 的 `UnitTweenSO.BuildSequence` 行）。
- **断言**：E8（表里四条在）· E8b–E8e（逐格值与 SO 相同：ease 5 / targetUnit 10-30-20-10 / appendType 0-5-0-5 / ResetBody 三复位）· E8f（`waitAnimation = 0`）。
- ⚠️ **未验**：实机画面（要跑一次 Battle 才看得到），见 §五。

### `11-07` 血痕静态 z 少 0.05 ✅
- **判据**：`CardPrefab.prefab` 的 `DamageIcon` `Transform`（fileID 2087739079688891721）`localPosition = (0.09400177, −0.104, **−0.05000019**)`。
- **改法**：`PopNumber.cs:548` `DamageIconLocalPos = (0.09400177f, −0.104f, −0.05000019f)`（原 `(0.094, −0.104, 0)`），落点仍按卡局部 ×桥（`:335` 起经 `ResetCounterIcon`）。
- **断言**：E6（z = −0.05000019 × 桥）· E6b（z ≠ 0）。

### `11-09` 单位抖动 ease 1 vs 6 ✅
- **判据**：`CardScript.DoUnitShake`（VA `0x1805EB7A0`）调 `DOShakePosition` 时**没有 `SetEase`** ⇒ 吃 `DOTweenSettings.defaultEaseType`；本工程 `Resources/DOTweenSettings.asset:31 = 6（OutQuad）`。
- **改法**：`CardFeel.cs:3839` 改传 `UnitShakeEase`；`:4508` 新增 `public const Ease UnitShakeEase = Ease.OutQuad;`（含判据注释）。
- **断言**：E7（常量 = OutQuad）· E7c（**反射读那条补间实际的 ease** = OutQuad；读不到会出声、⛔ 不算通过）。

---

## 三、判据订正记录（铁律 5）

| 位置 | 原来写的 | 实际 | 反证 |
|---|---|---|---|
| `PopNumber.cs` 血痕曲线注释 + `IconScaleRamp` doc | `0.0628` 是**首键**切线 | **末键** `inSlope = 0.06277556`；首键两端 0 | `AnimationClip_-3234148433819516763.json` 逐键实读 |
| `CardFeel.cs:144` | 「`Impact Light/Heavy Tween` …将来做远程弹着要用」（隐含「还没做」） | 那三条补间**已经**走数据链在跑（见 §二 `11-06`） | `UnitTweens.json` 四条 + `UnitTweenRuntime.Install()` + 73 个引用效果 |

---

## 四、待调度台接线（⛔ 我没有动这些文件）

### 4·1 我的断言类（宿主 = `BattleScene`）

```
Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs:47035 | B139EAssert.Run(); pass += B139EAssert.Pass; fail += B139EAssert.Fail;
```

> 插在**同一物理行**、`B124_W10_Assert.Run(); pass += B124_W10_Assert.Pass; fail += B124_W10_Assert.Fail;` **之后**、`Debug.Log(P + $"=== 合计：…` **之前**（⛔ 别落到该行已有的 `//` 注释之后）。
> 选这条聚合行的理由：`B139EAssert` **全部零前提**（自带临时根、`finally` 里 `DestroyImmediate` + 还原 `CardTween.Mode` + `PopNumber.ResetLive()`），不需要活 `driver`。
> ⚠️ 但它**需要效果库**（`WarpforgeEffectLibrary.Instance`，`CardPrefab` 从那儿取）—— 这条聚合行在 `BattleScene.Run` 里满足；若改挂 `CardBaseDemo.cs`，E4/E6 会报「前提不成立」的红（那是宿主选错，不是代码错）。

### 4·2 跨包：`1-01` 会让一条既有断言变红（**必须先改，否则 `BattleScene.Run` 红**）

```
Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs:23897 | Check(!drv.LastPop.Text.StartsWith("-") && drv.LastPop.Text == Mathf.Abs(amount).ToString(), $"……是**伤害**（无符号）：「{drv.LastPop.Text}」");
```

> 原文是 `Check(drv.LastPop.Text.StartsWith("-"), $"……是**伤害**（负数）：「{drv.LastPop.Text}」");`
> —— 判据 `CardDamageCounterController__DamageReceived.c:10-16`（原版无符号）⇒ 这条断言的**期望值本身过期了**（这正是 `B138` 条 `1-01` 要修的那件事）。
> `amount` 若要引，直接用该段的实际伤害变量；最省事的是只把 `StartsWith("-")` 换成 `!StartsWith("-")`。

### 4·3 跨包：`1-02` 生产路径吃复用（可选但**建议做**，否则这一条在生产里等于没改）

```
Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs:25500 | LastPop = PopNumber.PopDamage(transform, popAt, e.Amount, enemy: popFoe, key: v);
```

> `v` = 本段上文那个受击 `CardView`（同一作用域已有）。传了它，易伤/连击才会**重播同一颗**而不是并排两颗。
> ⛔ 不传也不报错（= 改前行为），所以这条**不影响自检**，只影响原版一致性。

### 4·4 跨包：`5-13` 生产路径补 `HealIcon`（包 B 的 `CardView.cs`）

```
Unity/MyGame/Assets/CardPresentation/Core/CardView.cs:9162 | PopNumber.AttachHealIcon(_healText.transform);
```

> 位置 = `BuildHealText`（`:9149`）里 `_healText.gameObject.SetActive(false);`（`:9162`）**之后**、方法结束之前。
> `AttachHealIcon` 已就位（幂等、取不到 prefab 节点会**出声**）：位置 `(0,0,0)`、`scale 0.56046`、`order 100`、`α 0.853` —— 与 prefab 逐值同形（z 已由 `HealText` 自己的 −0.05 承担）。

---

## 五、存疑 / 未验（如实记，⛔ 没拿猜测填）

1. **`11-06` 实机画面未逐帧验**：数据四条 + 入口 + 73 个引用效果都核过，但「`BulletImpact_*` 播放时 `_animfxCtx` 里真的带着 actor/target」这一步**要跑一次 Battle 才看得到**。若跑出来 `[UnitTween] 跳过` 的警告，那就是 `_animfxCtx` 在这一路为空（另一件事，落点在 `BattleDriver`，不在本包）。**判据口**：`UnitTweenRuntime.Invoked / Skipped / LastSkip`。
2. **`5-02` 描边在自适应之后的次序**：我按 `ErrorMessageBanner` 那条既有配方**后置**贴（文本定完才贴），它内部会 `UpdateMeshPadding` + `ForceMeshUpdate` + `RefreshBounds`。理论上只影响字形 padding、不改 `fontSize`；**但批处理里 `RefreshBounds` 走的是「量不到就不动」那一支**，这条要在宿主里跑一次确认飘字**没有横向偏移**（`BattleScene.cs:23915` 那条「飘在挨打那张卡上」的既有断言会替我抓住它）。
3. **`1-02` 的复用表是静态字典**：`key` 传 `CardView`（Unity 对象）时，卡被销毁后 key 变假 null ⇒ 下一次同 key 会判「已销毁」并重建；表 >32 条时按「key 或节点已销毁」清理。**跨局**不会无限涨，但**没有**在局末主动清（`ResetLive()` 只给自检用）。若调度台要求「换局立刻清」，挂在 `BattleDriver` 的局末（跨包）。

---

## 六、账补丁

`Unity/资料/完全复刻/账/_parts/B139_E.tsv`（11 列表头，4 行；`方法` 列全是完整签名）：

| 行 | 键（原版类 / 方法） | 覆盖的 `B138` 条 |
|---|---|---|
| 1 | `CardDamageCounterController / DamageReceived(int damageValue)` | 1-01 · 1-02 · 5-02 · 11-01 · 11-07 |
| 2 | `CardScript / DoUnitShake(int shakeScale, float duration)` | 11-09 |
| 3 | `UnitTweenSO / BuildSequence(CardScript, CardScript, Action)` | 11-06（**判据订正**，不是缺实现） |
| 4 | `CardScript / Heal(CardScript actingCard, HealInfo healInfo)` | 5-13（生产路径仍缺，跨包 B） |

> 这 4 个键在主表里**各只有一行**（已全库核过），合并不会撞键；合并请带 `--only B139_E`。

---

## 七、行号漂移（给 `check_ledger_points --fix` 的提示）

- `PopNumber.cs`：`PopDamage` **仍在 `:216`**（与主表一致）；其余落点见上表。
- `CardFeel.cs`：`ShakeUnit` 仍在 `:3827`；但它**方法体内部** +3 行、类尾 +14 行 ⇒ 主表里 `:3906 / :3919 / :4003 / :4100 / :4436 / :4455 / :4480` 这几条会漂 **+3**（类尾那 14 行在它们之后，不影响）。
  ⚠️ 顺带发现：主表那条 `DoUnitShake` 行里写的 `:3552-3555` / `:3783` 本来就是**过期的**（现读 = `:3565-3568` / `:3796`），本包补丁已按现读改正。
- 行尾：三个文件全是 **LF / 无裸 CR**（`b.count(b'\r\n')=0`，逐文件核过），未翻行尾。
