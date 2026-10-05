# W3 · A340 + A341（同一宿主：`Battle/ScenarioBlendables.cs` 的 AnimFX 工厂）（写手 · 2026-10-12）

> 一句话：**A340 做完**（生成器新开 packer 收下三层 → 重生成旁挂 → 工厂接上两层音效 + `modules` 如实出声 →
> 那条「三层没收」的 LogWarning 删除）；**A341 做完**（工厂补排自定自毁 + 一个可观测计数）。
> 验收 `python 工具/gen_env_blendables.py --check` ⇒ **✅ 两侧自检全过**（`_unresolved` 空）。
> ⚠️ **断言一条都没落盘** —— 落点 `Editor/BattleScene.cs` 本波被另一个写手占着 ⇒ 见 §五「待接线」（给了位置/判据/改坏法）。

---

## 一、结论（两件各一句）

| 件 | 结论 | 落地 |
|---|---|---|
| **A340** | ✅ **做完**。`sounds` / `exitSounds` / `modules` 三层**已收进旁挂**，运行时**两层音效真的建出来**（cue 名 → `PlaySoundOnTime` 逐字段），`modules` 只留档 + 出声（场景侧没有模块基类，见 §六·1）。原那条「三层旁挂没收」的 `LogWarning` **已删**。 | 2 改（生成器 + 工厂）+ 2 份 JSON 重生成 + 1 份注释订正 |
| **A341** | ✅ **做完**（代码）。`MakeAnimFx` 末尾照抄 `OnEnable()` 第二句补排自毁，条件**逐字照原版**（含 `destroyTime > 0`）；另加 `SelfDestroyScheduled` 计数当**可观测点**。 | 断言**待接线**（§五） |

---

## 二、判据（逐条带 出处；原版值 vs 我们值）

### 2.1 cue 名怎么解出来（A340 的关键一跳）

* 原版 `PlaySoundOnTime.sound` 是 **`AudioCue` 资产引用**：`{m_FileID: 12, m_PathID: …}`。
  `m_FileID: 12` = **那份 CAB 的 `externals[11]`** = `archive:/CAB-62d1945b7005c115fb946f0d264a205b/…`
  （实测：`scenes_scenes_battlearenatauviorla.bundle` 主 CAB `CAB-b969b7b6…` 的 externals 表第 12 条）。
* 那个 CAB 住在 **`soundcollection_assets_all.bundle`** 里（逐包核对过：`soundcollection` 命中该 CAB，
  `battleprefabs_vfxandmisc` 不命中）⇒ 本件为此新开 `CueNames`（**按需**读那个包）。
* cue 的形状 = 带 **`clipList`** 的 MonoBehaviour，它的 **`m_Name` 就是 cue 名**
  —— 与 `工具/import_original_sfx.py` 写 `Resources/animfx_sounds.json` 用的键**同一处判据**
  （那边 `key = d.get("m_Name")`）。⇒ 解出来的名字能直接被 `WarpforgeVFX.WFSoundBank.TryGetCue` 拿到。

**实测解出的 3 条**（全库 15 个 `scenes_scenes_*` 包里一共只有 7 个 `AnimFXController`，见 §七·1）：

| 宿主（原版路径） | `sound` 的 pid | cue 名 | 表里有没有 |
|---|---|---|---|
| `…/Railgun Turret 2/…/Cylinder.003/Railgun turret`（GO 206） | `2700193000380154422` | **`Railgun Turret`** | ✅（`animfx_sounds.json`） |
| `…/Railgun Turret 1/…/Cylinder.001/Railgun turret`（GO 819） | `-7999625259853248332` | **`Railgun Turret Far`** | ✅ |
| `…/Railgun BIG (1)/Big Gun Effect`（GO 1168，**不归任何 blendable 管**） | `-7136025203040646187` | `Railgun Huge` | ✅ |

* **两个 blendable 管的实例**（= 旁挂里 `kind == "animfx"` 的那两条）= `TauCannonAnimationStopper.animFXController`
  = MB **4767**（炮塔 2）/ **5247**（炮塔 1）：`sounds` 各 **1 条**（`time=0 · is2d=0 · repeat=1 · loops=5 ·
  timeInterval=0.75`）、`exitSounds` **空**、`modules` **空**、`preventDestroy=1`、`destroyTime=1.8`。
* **原版值 vs 我们值（改前/改后）**：改前 = 工厂建出的实例 `sounds/exitSounds/modules` **三项恒空**
  （且打一条「旁挂没收」警告）；改后 = `sounds[0]` 逐字段对上、`exitSounds.Length == 0`、`modules.Count == 0`。

### 2.2 A341 的判据

* `d:/2/tools/decomp_full/AnimFXController__OnEnable.c` 第二句：
  `if (destroyTime > 0 && !preventDestroy) Destroy(gameObject, destroyTime);`
  （`Battle/AnimFXController.cs:203-208` 的 `OnEnable` 已逐句照抄；我们**不能**靠它，因为
  `AddComponent` 会**先跑一次** `OnEnable`，那一刻字段还是默认值 ⇒ 必须由工厂补排，见偏离 ⑤）。
* 顺带核实（本件实证）：`GO 206 / 819 的 m_IsActive = True`、那两个 `AnimFXController` 的 `m_Enabled = 1`
  ⇒ 我们 `AddComponent` 出来默认 enabled、`OnEnable` 已跑过，**与原版同一档**（`TauCannonAnimationStopper.Toggle`
  靠 `enabled = option` 开关它，语义不变）。

---

## 三、改动清单（文件:行号 —— 行号 = 本件改完那一刻现读）

| # | 文件:行号 | 改了什么 | 为什么 |
|---|---|---|---|
| 1 | `工具/gen_env_blendables.py:171` | 新增 `ANIMFX_SOUND_FIELDS`（5 个数值字段名） | 判据 = 签名桩 + `PlaySoundOnTime__{ctor,Update,Reset}.c` 的字段偏移；只留一处 |
| 2 | `工具/gen_env_blendables.py:177` | 新增 `FIELD_S_IS_PATH = ('particleSystemPrefab','target')` | 🔴 原来「所有 `s` 都是对象路径」那个前提**被 A340 打破**（`s` 现在还装 cue 名与模块类名） |
| 3 | `工具/gen_env_blendables.py:181` | 新增 `CUE_BUNDLE = 'soundcollection_assets_all.bundle'` | cue 住在那个包里（实测那条外部引用） |
| 4 | `工具/gen_env_blendables.py:198-263` | **新增 `class CueNames`** | 跨包引用 `Bundle.resolve_ref` 解不了（实测返回 `(None,None)`）；按需加载 + `(CAB,pid)→名字` 缓存 + 解不出**出声** |
| 5 | `工具/gen_env_blendables.py:295` | `Bundle.__init__` 加 `self.cues = None` | 由 `main()` 接上；没接上时 packer 会出声（不许静默） |
| 6 | `工具/gen_env_blendables.py:320-338` | **新增 `Bundle.ref_cab()`**，`resolve_ref` 改成调它 | 跨包那条路要「引用落在哪份 CAB」；判据只留一处（铁律 6），行为逐条等价（老路三种返回全部保住） |
| 7 | `工具/gen_env_blendables.py:506-580` | **新增 `pack_animfx_defs()`**（照 `pack_controller_defs` 摊平） | A340 的正体：三层 → `TargetField`（键带下标），**不新开 DTO**（`Core/EnvBlendables.cs` 在白名单外，`GetF/GetS` 本来就能读） |
| 8 | `工具/gen_env_blendables.py:450-451` | `target_fields()` 里 `cn == 'AnimFXController'` ⇒ 转调新 packer | 让「blendable 引用的目标」这条路也收三层（与 `…Controller` 那条同款接法） |
| 9 | `工具/gen_env_blendables.py:776` | `check_prefab_side` 的「`s` = 路径」那条闸**只查 `FIELD_S_IS_PATH`** | 不挑键的话 `Railgun Turret`（cue 名）/ `AnimFXModuleScreenShake`（类名）会被当成「prefab 里没这个 GameObject」**误报** |
| 10 | `工具/gen_env_blendables.py:955 / 960 / 981 / 991` | `main()`：建 `CueNames`、接到 prefab 侧与 13 个场景 `Bundle` 上、`cues.warnings` 并进 `_unresolved` | cue 解不出的那几条**必须**出现在自检输出里 |
| 11 | `工具/gen_env_blendables.py:139-145 / 1061-1063` | 两处注释 + `_schema` 订正（「三层仍然没收」→「已收」，并写明新键） | 铁律 5：记错的就地改掉，别留第二份说法 |
| 12 | `…/ScenarioBlendables.cs:1667-1669` | `c.sounds` / `c.exitSounds` 走 `BuildSoundTrack`；`WarnAnimFxModules(t)` | A340 的接线：三层照旁挂填 |
| 13 | `…/ScenarioBlendables.cs:1660-1666`（注释块）+ 原 `1659-1661` 那条 `LogWarning` | **删掉**「三层旁挂没收」那条警告，改写成「已收」的订正注释 | 验收要求；且它现在是**错的**（留着就是噪音） |
| 14 | `…/ScenarioBlendables.cs:1671-1683` | **A341**：`if (!c.preventDestroy && c.destroyTime > 0f)` ⇒ `SelfDestroyScheduled++` + 排定自毁 | 照抄 `OnEnable` 第二句；运行时那一档 `Destroy(go, destroyTime)` 与原版逐字同路 |
| 15 | `…/ScenarioBlendables.cs:1694-1703` | 新增 `public static int SelfDestroyScheduled { get; private set; }` | A341 那条支路的**可观测点**（数据走不到，只能靠计数 + 合成目标验） |
| 16 | `…/ScenarioBlendables.cs:1705-1792` | 新增 `BuildSoundTrack()`（两层共用一份）+ `WarnAnimFxModules()` | 两层同形 ⇒ 共用（铁律 6）；模块那一层建不出来 ⇒ **点名到类名**出声，不假装覆盖 |
| 17 | `…/ScenarioBlendables.cs:1620-1623 / 1640-1647 / 998-1000` | XML 注释订正三处 | ①`MakeAnimFx` 那句「三层旁挂没收 / `preventDestroy=false` 没实现」已过期；② `preventDestroy` 那句「原版 **4/4** 是 `true`」**订正为 `2/2`**（能走到工厂的两条）＋点明全库另 3 条 `= 0` 走不到；③ `TauCannonAnimationStopper` 那句「实测 **4/4** 都指到了 `Railgun turret`」订正为 **2/2**（那两个 stopper 各 1 条 = MB 4767 / 5247） |
| 18 | `…/AnimFXController.cs:55-59 / 68-72 / 84-87 / 89-96 / 189-191 / 198-200` | **只改注释**（六处：场景侧实测那句 4→7 并点明「**其中 3 个 `preventDestroy = 0`**」、偏离 ③ 的模块计数 4→7、偏离 ⑤ 的已知缺口、文件头「旁挂里的三层」块、`preventDestroy` 字段、`modules` 字段） | 白名单：本文件只准改注释/已知缺口标注 —— `git diff --numstat` = **+34/−12**，逐行核过**非注释变更行 = 0** |
| 19 | `MyGame/Assets/Resources/EnvBlendables.json` · `数据/游戏数据/env_blendables.json` | **由生成器重生成**（没手改一个字节） | 见 §四 |

* **行尾**：改完二进制数过 —— `gen_env_blendables.py` **CRLF 0 / LF 1157** · `ScenarioBlendables.cs` **0 / 1959** ·
  `AnimFXController.cs` **0 / 317** · 两份 JSON **0 / 29163 · 0 / 28149**。全程 Edit/Write/生成器，**没用 `sed -i`**。

---

## 四、数据变更（两份 JSON 的差，逐条核过）

* **本件带来**：只有 tauviorla 那 **2 条** `kind == "animfx"` 的 target 各 **+9 个键**
  （`sounds.count` · `sounds.0.sound` · `sounds.0.{time,is2d,repeat,loops,timeInterval}` ·
  `exitSounds.count` · `modules.count`）；**target 总数 2328 → 2328**（一条没丢、一条没多）。
  `Resources/EnvBlendables.json` 的 `git diff` = **+72/−0**，逐行看过：**全是这 18 个键的对象行**。
* **顺带落回真值（不是本件改的，是输入变了）**：这两份 JSON 上次重生成**早于 A191 的 arena prefab 重建**
  ⇒ 这次重跑把 `stats.scene_arenas_ok 11 → 13` · `standalone_roots_ok 12 → 13` ·
  `standalone_missing_roots 1 → 0` · **`_missingTargets 5 → 0`** 一起落回当前真值
  （判据：`check_scene_side` 按 **prefab ∪ 清单** 查存在性，而 `Resources/ArenaPrefabs/*.prefab` 已被 A191 重建）。
  ⇒ `数据/游戏数据/env_blendables.json` 的 diff 因此是 +78/−64（其中 64 行删除全在 `_schema` / 3 个 stats 计数 /
  `_missingTargets` 这 5 条上，**没有一条 target 或字段被删**）。
* `_unresolved` = `[]`（跑完写盘的实测值）。

---

## 五、待接线（断言 —— **本件没碰 `Editor/BattleScene.cs`**）

> 落点：`MyGame/Assets/CardPresentation/Editor/BattleScene.cs` 的 **tauviorla 块内**
> （现读：`A191 ②` 块 `:1565-1634` 之后、`A201` 块 `:1636` 之前；或 `A201` 块之后、`finally` `:1739` 之前）。
> ⚠️ **行号会漂**（那个文件正被另一个写手改）—— 认**注释锚点**别认行号。
> ⚠️ **别在 Editor 里另写一份「按名字+最近位置找对象」** —— 用生产那份
> `new CardPresentation.EnvironmentApplier.SceneResolver(tvInst.transform)`（A201 块 `:1655` 就是这么做的，铁律 6）。
> ⚠️ `resTv`（`:1586`）**声明在 `if (tvSc != null)` 里** ⇒ 出了那个花括号就没它了，新块要**自己新建一份**。

### 5.1 A340 两条（数据 → 组件这一跳）

```csharp
var resTv2 = new CardPresentation.EnvironmentApplier.SceneResolver(tvInst.transform);
int nFx = 0, nWired = 0; string fxWhat = "";
foreach (var it in CardPresentation.EnvBlendables.ForArena("battlearenatauviorla"))
{
    if (it == null || it.cls != "TauCannonAnimationStopper") continue;
    CardPresentation.EnvBlendables.Target at = null;
    foreach (var t in it.targets) if (t != null && t.kind == "animfx") { at = t; break; }
    if (at == null) continue;
    nFx++;
    // ①「三层在位」的证书：三个 count 键**都得在**（缺 = 旁挂是旧版 / packer 没接上）
    bool layers = at.GetF("sounds.count", -1f) >= 0f && at.GetF("exitSounds.count", -1f) >= 0f
                  && at.GetF("modules.count", -1f) >= 0f;
    var host = resTv2.GoOf(at);
    var st = host != null
           ? CardPresentation.ScenarioBlendableFactory.Create(it, host, resTv2, true)
             as CardPresentation.TauCannonAnimationStopper : null;
    var fa = st != null ? st.animFXController : null;
    if (layers && fa != null && fa.sounds.Length == 1) nWired++;
    ...
}
Check(nFx == 2 && nWired == 2,
      "★ A340：两条 `TauCannonAnimationStopper.animFXController` 的 `sounds` 层**照旁挂建出来了**"
    + "（原版那 2 个实例各有 1 条 `sounds`；改前这里恒 0）…");
```

**期望值（写死的那几个 —— 判据是原版包，不是我们的实现）**：

* 炮塔 **2** 那条（`at.path` 含 `Railgun Turret 2`）⇒ `fa.sounds[0].sound == "Railgun Turret"`；
  炮塔 **1** 那条 ⇒ **`"Railgun Turret Far"`**（别按名字直觉猜反：**近的那个是 `Far`**）。
* 5 个数值：`time == 0f` · `!is2d` · `repeat` · `loops == 5` · `timeInterval ≈ 0.75f`。
* 再加一条**跨文件**的：`WarpforgeVFX.WFSoundBank.HasCue(fa.sounds[0].sound)` ⇒ 这条 cue 在
  `Resources/animfx_sounds.json` 里**真能解到**（`HasCue` 只查表、不播音、不计 `BadCues`）。
* `fa.exitSounds.Length == 0 && fa.modules.Count == 0`（**原版就是空的**，不是「没收」——与 `count` 键区分开）。

🧨 **改坏法**：把 `gen_env_blendables.py` 里 `target_fields()` 的 `if cn == 'AnimFXController'` 那两行去掉、
重生成一次旁挂 ⇒ `sounds.count` 缺 ⇒ 工厂**出声**且 `sounds.Length == 0` ⇒ **红**。

### 5.2 A341 一条（排定自毁那条支路）

> 🔴 今天**数据走不到**（能走到工厂的两个实例 `preventDestroy` 都是 `1`；全库另 3 个 `= 0` 的实例
> **不归任何 blendable 管**，见 §七·1）⇒ 只能**合成一条目标**去点这条支路。
> ⚠️ 不合成就写断言 = **恒真的假断言**（本仓明令禁止）。

```csharp
// 探针宿主：**独立根对象**（不挂在 tvInst 里 ⇒ 不扰动别的断言，也不被 A191 那棵树遍历到），
// 解析器用 `SceneResolver(null)` —— `FindNearest` 在 root == null 时改走全场景按名找（`EnvironmentApplier.cs:750`）。
var probe = new GameObject("A341 Probe Host");
try
{
    CardPresentation.EnvBlendables.Target mk(float pd, float dt) => new CardPresentation.EnvBlendables.Target {
        leaf = "A341 Probe Host", path = "A341 Probe Host", kind = "animfx", pos = new float[] { 0f, 0f, 0f },
        fields = new [] {
            new CardPresentation.EnvBlendables.TargetField { k = "preventDestroy", f = pd },
            new CardPresentation.EnvBlendables.TargetField { k = "destroyTime",    f = dt },
            new CardPresentation.EnvBlendables.TargetField { k = "sounds.count",     f = 0f },
            new CardPresentation.EnvBlendables.TargetField { k = "exitSounds.count", f = 0f },
            new CardPresentation.EnvBlendables.TargetField { k = "modules.count",    f = 0f },
        } };
    var itSyn = new CardPresentation.EnvBlendables.Item {
        cls = "TauCannonAnimationStopper", owner = "A341 Probe Host", ownerLeaf = "A341 Probe Host",
        targets = new [] { mk(0f, 1.8f) } };
    var resProbe = new CardPresentation.EnvironmentApplier.SceneResolver(null);
    int before = CardPresentation.ScenarioBlendableFactory.SelfDestroyScheduled;
    var stopper2 = CardPresentation.ScenarioBlendableFactory.Create(itSyn, probe, resProbe, true)
                   as CardPresentation.TauCannonAnimationStopper;
    Check(stopper2 != null && stopper2.animFXController != null
          && CardPresentation.ScenarioBlendableFactory.SelfDestroyScheduled == before + 1,
          "★ A341：`preventDestroy = false` 那条支路**真的执行了**（工厂补排了自毁）…");
    Check(probe == null,
          "★ A341：批处理那一档走的是 `DestroyImmediate` ⇒ 探针宿主**当场没了**"
        + "（运行时那一档是 `Destroy(go, destroyTime)`，与本条**判据同一句**、只差「排定 vs 当场」）…");
    // 负对照：`preventDestroy = 1` ⇒ 不许排定、也不许销毁
    var probe2 = new GameObject("A341 Probe Host 2");
    ...
}
finally { if (probe != null) UnityEngine.Object.DestroyImmediate(probe); ... }
```

🧨 **改坏法**：删掉 `MakeAnimFx` 里 `if (!c.preventDestroy && c.destroyTime > 0f) { … }` 那一段 ⇒ **①红**（负对照仍绿）。

**两条都在提醒**：`SelfDestroyScheduled` 是**累积**计数、没有重置 ⇒ 断言一律**读差**（`before + 1`），别写绝对值。

---

## 六、没查清的部分（⛔ 不猜）

1. **`modules` 那一层要不要真做**：旁挂已经把原版那层的**类名**收下来了（`modules.<i>`），但场景侧**没有模块基类**
   —— 特效那条线的 `WarpforgeVFX.WFEffectModule.Initialize` 收的是 `WarpforgeEffectPlayer`（另一条线的控制器）
   ⇒ 建出来也接不上（`AnimFXController.SetData` 里那条出声写的就是这件事）。要真做 = **先给场景侧一条模块线**
   （本件白名单里没有那个落点）。**今天能走到工厂的 2 个实例 `modules` 都是空的** ⇒ 这一跳**数据走不到**，
   但**要记录、之后完全复刻**（铁律 11）：已知的模块类型 = `AnimFXModuleScreenShake`（带 1 条
   `manualTriggerCameraShakes` → `Shake Earthquake` SO，实测在 `animfx_components.json` 的 `Scenario` 组里）。
2. **批处理那一档（`DestroyImmediate`）没有一次 Unity 实测**：本件跑不了 Unity（红线）⇒ 只能靠静态核对；
   `BattleScene.Run` 接线之后才算真验过（§五·5.2）。
3. **cue 名与 `animfx_sounds.json` 的耦合方向**：本件实测那 3 个 cue（`Railgun Turret` / `Railgun Turret Far` /
   `Railgun Huge`）**今天都在表里**，但它们**现在是靠特效侧**（`数据/游戏数据/animfx_modules.json` 里那个
   `Scenario` 组）才被 `import_original_sfx.py` 的 `used_cues()` 收进去的 —— 场景侧这条链**不在那个来源里**。
   现状**不会**掉 cue（那 3 条确实被引用着），但**判据是间接的**：若哪天那些引用消失、而表重生成，
   场景侧这 3 条会**静默变成「表里没有」**（`AnimFXController.PlayCue` 会出声、但不播）。
   建议把 `env_blendables.json` 的 `sounds.*.sound` 也并进 `used_cues()` 的来源 —— 落点
   `工具/import_original_sfx.py`（**不在本件白名单**）。
4. **`soundUnresolved` 这个标记键是本件新造的形制**：它替的是 `EnvironmentApplier.HasField`（`private`、
   本件改不了）那件事 —— 「`TargetField` 没有『有没有这个键』那一问」。
   建议由调度台裁：要么把那个 `HasField` 收口到 `Core/EnvBlendables.cs`（两处共用，铁律 6），
   要么就认下这个键（现在只有「解不出」时才写，今天 0 条）。**别在两处各写一份 `HasField`。**
5. **`self.cues is None`** 那条分支（生成器里忘了接解析器）只做了「warning + 标记键」，**没跑过**（`main()` 一定接）。

---

## 七、顺手发现（⛔ 一个都别改，只报）

1. 🔴 **「场景侧的 `AnimFXController` = 4 个、`preventDestroy` 4/4 = `true`」这条计数是错的**
   （A 表 / `S4_外壳共用件_开账现核.md` 的 A340/A341 两行都这么写，`W9` §二也只数了 tauviorla）。
   **本件实测（15 个 `scenes_scenes_*` 包全扫、不设过滤）**：一共 **7** 个，其中 **3 个 `preventDestroy = 0`**——

   | 场 | pid | `m_Enabled` | `preventDestroy` | `destroyTime` | sounds/modules | 宿主（原版路径） |
   |---|---|---|---|---|---|---|
   | battlearena2 | 4317 | 1 | **0** | 6.0 | 0 / 0 | `Scenario/Battle Arena 2 Particles/RocketTrail` |
   | battlearena3 | 4187 | **0** | **0** | 4.0 | 0 / 0 | `Scenario/Particle Effects/Lightning_Green` |
   | battlearena3 | 4450 | **0** | **0** | 4.0 | 0 / 0 | `Scenario/Particle Effects/Lightning_Green (1)` |
   | tauviorla | 4767 / 5247 | 1 | 1 | 1.8 | **1** / 0 | 两个 `Railgun turret`（旁挂 `animfx` 目标） |
   | tauviorla | 5360 | 1 | 1 | 4.0 | **1** / 0 | `…/Railgun BIG (1)/Big Gun Effect` |
   | tauviorla | 5474 | 1 | 1 | 4.0 | 0 / **1** | `…/Railgun BIG (1)`（带 `AnimFXModuleScreenShake`） |

   ⇒ ① **旁挂（这条链）只覆盖 tauviorla 的 2 条**，另 **5 条我们从来没建过**（它们不归任何 blendable 管）
   —— 与本仓已有的 `standalone`（A137）是同一类缺口，但**那是给它自己那族 spawner 用的**，这条没接；
   ② A341 那条支路的「今天走不到」**仍然成立**（能走到工厂的还是只有那 2 条 `= 1` 的），
   但**理由要改**：「4/4 都是 true」→「**能走到工厂的 2 条是 1；全库另有 3 条 = 0，但我们不建它们**」。
   *(我上一版扫描带了一个 `sounds 或 modules 非空才打印` 的过滤 ⇒ 漏掉那 3 条；本表是去掉过滤重扫的。)*
2. **`battlearena2` 的清单与 prefab 不同步（或闸门挡掉）**：`battlearena2_manifest.json` 的 `particles` 里有一条
   `RocketTrail`（`active: true`，`pos [96.5, 5.619, 46.704]`），而 `Resources/ArenaPrefabs/battlearena2.prefab`
   里 **`RocketTrail` 0 命中**（13 场里只有 darkangels / tauviorla 两件是 A191 重建过的 ⇒ 嫌疑是「prefab 比清单旧」）。
   ⚠️ **本件没查清**是「prefab 没重建」还是「`ArenaBuilder` 的闸门挡掉」—— 归战场线。
3. **开场就会响炮塔声**：那两个 `Railgun turret` 的宿主 `m_IsActive = True`、组件 `m_Enabled = 1`、
   `sounds[0].time = 0` ⇒ 这版**进 tauviorla 就会响「炮塔机枪声」（各 5 连发 / 0.75s 间隔）**。
   这是**照原版**（原版那两条组件也是开着的、`Update` 一帧就播），**不是新 bug** —— 但第一次听到的人会以为是啊。
4. **派单里的白名单路径有一处不存在**：`Assets/CardPresentation/Resources/EnvBlendables.json`
   —— 生成器的摊平产物实际在 **`MyGame/Assets/Resources/EnvBlendables.json`**（`gen_env_blendables.py` 的 `FLAT`）。
   本件按生成器的真实路径重生成（两份产物都写了，见 §四）。`Assets/CardPresentation/Resources/` 下没有这个文件。
5. **另一个写手的半成品**（⛔ 本件没碰、**收工时已自行补完**）：`Shell/AlliancesTab.cs`（9 处）+
   `Shell/FriendsTab.cs`（4 处）曾报 `SocialView.Text` / `SocialPage.Text` **少一个 `alignLeft` 实参**
   ⇒ 那一刻运行时程序集**编不过**（13 条，**全在这两个文件里**；本件的 `.cs` 一条诊断都没有）。
   本件收工前的**最后一次**类型检查已经是 **0 / 0**（见 §八）。

---

## 八、跑过的检查

```text
# ① 生成器自检（本件的验收条件）
PYTHONIOENCODING=utf-8 python 工具/gen_env_blendables.py --check
  ⇒ ✅ 两侧自检全过（`_unresolved` 空；已知缺口见上面那条「（信息）」与 `_missingTargets`）
  ⇒ 条目 139 · standalone：13 个根（13 个对上）/ 27 条 = spawner 24 + controller 3 · 缺根 0
  ⇒ 读 soundcollection_assets_all.bundle（解 cue 名用，第一次遇到 cue 才读）…   ← 只在 tauviorla 那一条路上真读
# ② 写盘（两份产物都由它写；本件一个字节没手改）
PYTHONIOENCODING=utf-8 python 工具/gen_env_blendables.py   ⇒ 写出两份 JSON
# ③ 秒级类型检查（改完 .cs 的最后一次）
TMPDIR=/tmp/wf_w3 bash d:/4/Unity/工具/typecheck.sh
  运行时错误数: 0
  编辑器错误数: 0
  （本件改的 ScenarioBlendables.cs / AnimFXController.cs **零诊断**。
    ⚠️ 中间某一跑曾报 `运行时错误数: 13`，**全部**在 `Shell/AlliancesTab.cs`(9) / `Shell/FriendsTab.cs`(4)
    —— 那是**别的写手**在改 `SocialView.Text` / `SocialPage.Text` 的签名、调用点还没跟上的半成品；
    收工时那两处已经补完 ⇒ 最终两行都是 0。判据：诊断**只出现在不是本件的文件**上 ⇒ 不是本件的问题。）
```

* ⛔ **没跑 Unity**（红线）· ⛔ 没动 git（只读了 `git diff --numstat` / `git status`）· ⛔ 没改两张正本 ·
  ⛔ 没碰 `Editor/BattleScene.cs`（另一个写手占着）· ⛔ `Core/EnvBlendables.cs` / `EnvironmentApplier.cs` /
  `工具/import_original_sfx.py` 都只读不动。
* ✅ **改完 `.cs` 立刻跑了类型检查**（独立 `TMPDIR=/tmp/wf_w3`）—— 第一次跑出 2 条**我自己的**错
  （`ScenarioBlendableFactory` 是 `static class`、不是 `MonoBehaviour` ⇒ `Destroy` 要写全限定名），当场改掉；
  之后再跑，我的文件零错误。
