# E2 · `dump_animfx.py` 的 `eventToFire` 深度护栏（干跑）+ `EffectName` 的 `(Clone)` 审计

> 代理 E · 2026-10-23 · 承接 `资料/普查产出_1023/E_R25事件通道.md` 的 ⑤·1 / ⑤·2
> ⛔ 本批**没有**覆盖任何 `数据/游戏数据/animfx_*.json`；所有产物在 `_tmp_view/E2_dry/`。

---

## ① 结论

### 一、`eventToFire` 护栏 —— **改好了，干跑通过，可以落盘（等拍板）**

- 修法照前两次先例：**只在 `_depth_limit()` 里给 `eventToFire` 单独放宽**，⛔ 没动 `DEFAULT_MAX_DEPTH`。
- **干跑逐键对比结果：只有 1 个效果 × 1 个类受影响**（`VanguardIdleEffect` / `AnimFXModuleEvent`），
  **全库没有任何别的 key 被这道护栏截断**（新增 30 键全在那 2 条 call 底下，删除 1 键 = 那个 `<深>`，
  **值变化 0 条**）。
- 体积：`animfx_components.json` **2,296,976 → 2,298,577 B（+1,601 B / +0.070%）**；
  下游 `animfx_modules.json` **2,639,304 → 2,641,576 B（+2,272 B / +0.086%）**。
- **基线是字节级可信的**：干跑出来的「修前」文件 md5 = `308e13a7ac95981debd8bb13af29cc99`，
  与**仓库里现在那份 `数据/游戏数据/animfx_components.json` 完全一致**
  ⇒ 我比的就是「真·当前产物 vs 修后」，不是拿两份不同来源凑。
- ⚠️ 放大护栏后读出来的内容，与 R25 从 `assets_full` 的 prefab 原始 JSON 读的那 2 条 **逐字段一致**
  （方法名 / 类型名 / `m_Mode=6` / `m_BoolArgument=1` / `m_CallState=1,2` / `m_Target` 路径）——
  两条**互相独立**的取数路径对上了。

### 二、`(Clone)` 审计 —— **全仓只有 1 处是「查表用的键」，而且 `(Clone)` 现在根本到不了那儿**

- **真正的键只有 1 处**：`WFModuleAnimation` 用 `EffectName + "|" + 状态名` 去
  `WarpforgeAnimatorBridge.TryGetClip(...)` 取原版片段别名。其余 20 多处全**只是日志文案**。
- `WFModuleTween` / `WFModulePostProcess` 把 `EffectName` 塞进请求结构体，但**下游从不拿它当键**
  （`UnitTweenTable.Build` 的第 4 个参数名字就叫 `why`，只进日志；`BattlePostFx.Handle` 里
  **一次都没读** `r.effect`）。
- `WFModuleScreenShake` / `WFModuleCardback` **都不按效果名查表**
  （ScreenShake 的字典键是 **preset 名**；Cardback 的 `EffectName` 只出现在两条 `LogError` 里）。
- 🔑 **`(Clone)` 现在不可达**：全工程**只有** `WarpforgeEffectPlayer.Play()` 一处 `Instantiate` 效果 prefab，
  它**紧接着就 `go.name = entry.name;`（注释明写「去掉 Instantiate 加的 `(Clone)`」）**，
  再 `p.Init(entry, …)` 把 `_entry` 填上。而且全仓 `.unity` / `.prefab` 里**没有任何一处预挂
  `WarpforgeEffectPlayer`**（按脚本 guid `c32a845caaa548541888245d90a5344c` 搜，0 命中）
  ⇒ 不存在「`_entry == null` 的玩家实例」。
- ⇒ 这条**目前是「上膛但没装弹」**。但它**不是零风险**：只要将来有人直接 `Instantiate` 一份效果 prefab
  并 `AddComponent<WarpforgeEffectPlayer>()`（不调 `Init`），`EffectName` 就会退回带 `(Clone)` 的
  `gameObject.name`，`WFModuleAnimation` 会**静默取不到别名**（`EnsureClips` 里 `def==null && exit==null`
  直接 `return`，一路退到「Animator 没 controller」那条失败路）。

### 三、审计里**多查到一条**（比 `(Clone)` 更容易踩）：`WFEffectModule.EffectName` 自己的兜底

`WarpforgeVFX/Runtime/WFEffectModule.cs:134`
```csharp
public string EffectName { get { return Controller != null ? Controller.EffectName : name; } }
```
`Controller` 要等 `Initialize(controller)` 才注入，而 `WFModuleFactory.Create()` 是
**先 `Configure(def)`、后广播 `Initialize`**（`WarpforgeEffectPlayer.cs:280-286`）。
⇒ 在 `Configure` / `Awake` / `OnEnable` 里读 `EffectName`，拿到的是
**模块自己所在 GameObject 的名字**（例如 `Glow bottom R`），**不是效果名**。
它和 `(Clone)` 是**两个不同的错值、同一个后果（拼出取不到的键）**。
今天没出事纯属侥幸：`WFModuleAnimation.EnsureClips()` 是在 `Initialize` 里 `base.Initialize` **之后**调的
（`WFModuleAnimation.cs:75-83`）。

---

## ② 证据

### 2·1 改了 `工具/dump_animfx.py` 的三处（`git diff --numstat` = 39+/4−，CRLF 保持 522/522）

| 位置 | 改动 |
|---|---|
| `:237` 后新增常量块 | `EVENT_TO_FIRE_MAX_DEPTH = 18` + 一整套注释（判据 / 形状 / **面** / ⛔ 别抬 `DEFAULT_MAX_DEPTH`） |
| `_depth_limit()` | 加 `if seg.split("[", 1)[0] == "eventToFire": return EVENT_TO_FIRE_MAX_DEPTH` |
| `main()` / 文件头 | 🆕 `--out <路径>`（**干跑**用，不覆盖正式产物；输出到正式路径时会先打一行提醒） |

**为什么 18**：`eventToFire` 这棵子树与已经放宽过的 `collisionEvent` **同形**
（`UnityEvent → m_PersistentCalls → m_Calls[] → {m_Target, 6 个 m_Arguments.*}`），
最深的叶子同样在 depth 15 ⇒ 沿用 18 = 「刚刚够 + 一点余量」。

**「面」怎么核的**（放宽前先扫，照前两次先例）：
```
grep -rn "eventToFire" d:/2/Warpforge_code/Scripts/
```
⇒ **全库 1 处命中**：`Assembly-CSharp/AnimFXModuleEvent.cs:11`（`private UnityEvent eventToFire;`，
是嵌套类 `FireEvent` 的字段）。别的任何类**都没有**这个字段名
⇒ 放宽它不可能连带放过别的字段（这一点已被下面的干跑逐键对比**实测证实**）。

### 2·2 干跑（⛔ 正式产物零改动）

产物全在 `d:/4/Unity/_tmp_view/E2_dry/`：

| 文件 | 是什么 |
|---|---|
| `before.json` | 修前语义（collisionEvent + references 两条照旧，只去掉 eventToFire）· md5 `308e13a7…` = **与仓库现在的 `animfx_components.json` 字节一致** |
| `after.json` | 修后（真脚本 `--out`） |
| `modules_before.json` / `modules_after.json` | 下游 `gen_animfx_modules.py` 的干跑产物（输入=上面两份） |
| `run_before.py` / `run_modules.py` | 干跑包装（monkeypatch 输入输出路径，**不改正式脚本的落点**） |

**逐键对比（components 层）**

```
受影响的效果×类： 1        VanguardIdleEffect | AnimFXModuleEvent
删除（修前是 <深>）： 1    eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls
新增：              30    全部在 …m_Calls[0] / …m_Calls[1] 底下
值变化：             0
summary / effect_count(912) / 每个效果的组件数：全部不变
体积： 2,296,976 → 2,298,577 B  (+1,601 B, +0.070%)
```

**逐键对比（下游 modules 层）**

```
effect_count 912 → 912        module_count 2346 → 2346   （都不变）
受影响模块： 1   VanguardIdleEffect | AnimFXModuleEvent
新增 20 键 / 删除 0 / 改值 0
refs: node 3123 → 3125 (+2)   asset 1928 → 1928   cardAnimResolved 1 → 1
cardAnimUnresolved: 0 → 0
"collisionEvent 子树被深度护栏截断的键" 哨兵：0 个（前后都是 0）
体积： 2,639,304 → 2,641,576 B  (+2,272 B, +0.086%)
```

**读出来的 20 个键**（`modules_after.json` 里 `AnimFXModuleEvent` 模块）：

```
eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[0].m_Target                 = @node:MonoBehaviour:VanguardIdleEffect
eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[0].m_TargetAssemblyTypeName = AnimFXModuleChangeMaterial, Assembly-CSharp
eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[0].m_MethodName             = ToggleMaterial
eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[0].m_Mode                   = 6
eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[0].m_Arguments.m_BoolArgument = 1
eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[0].m_CallState              = 1
…（另外 4 个 m_Arguments.* + m_ObjectArgumentAssemblyTypeName）
eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[1].m_Target                 = @node:MonoBehaviour:VanguardIdleEffect/Vanguard Frame Animated VAT#1
eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[1].m_TargetAssemblyTypeName = StoryProgramming.VATGPUPlayer, Assembly-CSharp-firstpass
eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[1].m_MethodName             = UpdateMaterials
eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[1].m_Mode                   = 6
eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[1].m_Arguments.m_BoolArgument = 1
eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[1].m_CallState              = 2
```

⇒ 与 `数据/游戏数据/module_events.json`（从 `assets_full` 读的那份）**逐字段一致**；
`m_Target` 的 `@node:` 路径也正是 `WFEffectModule.ResolvePath` 认的那套写法
（`VanguardIdleEffect/Vanguard Frame Animated VAT#1`，和我们 prefab 里的兄弟下标 1 对得上）。

### 2·3 `EffectName` 全仓清点

| 文件:行 | 用法 | 有风险吗 |
|---|---|---|
| `WFModuleAnimation.cs:105` | `TryGetClip(EffectName + "\|Default")` | 🔴 **键** |
| `WFModuleAnimation.cs:106` | `TryGetClip(EffectName + "\|" + ExitState)` | 🔴 **键** |
| `WFModuleAnimation.cs:199` | `TryGetClip(EffectName + "\|" + state)` | 🔴 **键** |
| `WFModuleTween.cs:249` | `TweenRequest.effect = EffectName` → `UnitTweenTable.Build(..., why)` 的第 4 参 **名叫 `why`** | 🟢 只进日志 |
| `WFModulePostProcess.cs:318,336,391,432,452` | `PostFxRequest.effect = EffectName` → `BattlePostFx.Handle` **从不读 `r.effect`**（只有 `:522` 一条警告用） | 🟢 只进日志 |
| `WFModuleCardback.cs:110,122` | `LogError` 文案 | 🟢 只进日志 |
| `WFModuleChangeMaterial.cs:524,532,548,652` / `WFModuleChangeVelocity.cs:151,198,303` / `WFModuleDestroyInTime.cs:96` / `WFModuleDoDestroyAnimation.cs:95` / `WFModuleInstanceParticleAdjacent.cs:192,233` / `WFModuleMoveParticlesToTarget.cs:200,209,246,321` / `WFModuleTransformModifier.cs:353,476,515` / `WFModuleTween.cs:316` / `WFModuleAnimation.cs:174` | 全是 `Debug.LogWarning` 文案 | 🟢 只进日志 |
| `CardPresentation/Editor/BattleScene.cs:36721,36732,8070,8114,8751` | `ps[i].EffectName == "FerocityEffect"` / `== CardFeel.DeathBodyFx` | 🟡 **自检比较**：错了会**红**（不静默），且这些效果都走 `Play()`（`_entry` 一定非空） |
| `WFEffectModule.cs:134` | 属性定义（见 ③ 那条） | 🟠 **兜底值语义错**（不是 `(Clone)`，是**节点名**） |
| `WarpforgeEffectPlayer.cs:117` | 属性定义 | 🟠 `(Clone)` 源头 |
| `WFEffectInfo.cs:45` / `EffectLibraryBuilder.cs:261` | **另一个类**（资产级 metadata）/ 建库时 | 🟢 无关（构建期、无实例） |

`CardPresentation/Battle/**` 里 **0 处** 使用 `EffectName`
（`Core/VfxMap.cs:908,920,924` 那个 `NoEffectNames` 是另一个标识符，别误读）。

**`(Clone)` 不可达的三条证据**：
1. 全工程只有 `WarpforgeEffectPlayer.cs:193` 一处 `Instantiate` 效果 prefab，`:194` 立刻
   `go.name = entry.name;`（注释原文：「去掉 Instantiate 加的 "(Clone)"，日志里好认」）。
2. `:216` `AddComponent<WarpforgeEffectPlayer>()` 之后**同一函数内**就 `p.Init(entry, …)`（`:218`），
   `Init` 第一句 `_entry = entry;`（`:223`）。
3. 按脚本 guid 搜全部 `.unity` / `.prefab`：`WarpforgeEffectPlayer` **0 命中**
   ⇒ 没有「预挂在场景/prefab 上、运行时 `_entry` 为 null」的实例。

---

## ③ 没查清的部分

| # | 没查清 | 缺什么 |
|---|---|---|
| 1 | **落盘后没跑 Unity 批处理验证**（`EffectLibraryBuilder` 重建 `WarpforgeEffectLibrary.asset` 这一步要 Unity） | 等拍板落盘后，主对话跑一次「Tools > Warpforge > 生成效果库」+ 现有自检 |
| 2 | `eventToFire` 这条路径将来若被**别的类**复用（新增同名序列化字段），护栏会照样放宽 —— 我只核了**当前** build 的 `d:/2/Warpforge_code/Scripts/`（1 处） | 无（当前是空集）；换 build 时重跑那条 grep |
| 3 | `WFModuleAnimation` 的别名表 `<效果名>\|<状态名>` 是谁登记的、会不会随重导漂移 | 不在本批范围（`extract_missing_shaders.py --prefabs` / `gen_simple_animation_clips.py`），**没查** |
| 4 | `WFEffectModule.EffectName` 在 `Controller == null` 时会被**哪些**模块真的读到 | 我只逐个看了 `EffectName` 的调用点：今天**只有** `WFModuleAnimation` 拿它当键，而它在 `Initialize` 之后读 ⇒ 现在安全。将来新增模块会不会在 `Configure` 里读，**没法预判** |

---

## ④ 改动清单

| # | 文件 | 一句话 |
|---|---|---|
| 1 | `d:/4/Unity/工具/dump_animfx.py` | **改**：`EVENT_TO_FIRE_MAX_DEPTH = 18` + `_depth_limit()` 第三条路径 + `--out` 干跑开关（39+/4−，CRLF 522/522，`py_compile` 过） |
| 2 | `d:/4/Unity/资料/普查产出_1023/E2_dump护栏与EffectName审计.md` | **新建**：本报告 |
| — | `d:/4/Unity/_tmp_view/E2_dry/**` | 干跑产物（`before/after.json`、`modules_before/after.json`、两个包装脚本）—— **不属于正式产物，可随时删** |

**⛔ 没碰**：`数据/游戏数据/animfx_components.json` / `animfx_modules.json` / `module_materials.tsv`
（`git status` 干净，mtime 仍是 `2026-10-22 13:26`）· `gen_animfx_modules.py`（一行没改）·
`WFModuleEvent.cs`（这一批没动，表**留着**）· `CardPresentation/**` · 正本 · `d:/2`（只读）。
**没跑 Unity、没动 git。**

### ④-1 「改成读数据」的接法与判据（等落盘后再做）

护栏修好并**真的重跑落盘**之后，`gen_animfx_modules.py` 会把这 20 个键自动拍进
`animfx_modules.json` 的 `AnimFXModuleEvent` 模块（干跑已实测），于是 `WFModuleDef.keys` 里就能读到
——**那张 `TableCall[] Table` 就可以退休**，接法照 `WFModuleCollisions` 读 `collisionEvent`
一模一样（它就是同一个形状）：

```csharp
// 1) 条数：别用 Has("eventsToFire[0].eventToFire.m_PersistentCalls.m_Calls[0]")（那个键不存在）
int nCalls = def.CountList("eventsToFire[i].eventToFire.m_PersistentCalls.m_Calls");
// 2) 每条：
string node = def.GetString(p + ".m_Target");            // "@node:MonoBehaviour:VanguardIdleEffect/Vanguard Frame Animated VAT#1"
WFModuleDef.SplitRef(node, out kind, out type, out rest); // → ResolvePath(root, rest)
string asmTy = def.GetString(p + ".m_TargetAssemblyTypeName"); // 诊断用（我们的类名不同）
string meth  = def.GetString(p + ".m_MethodName");
int    mode  = def.GetInt(p + ".m_Mode");                // 6 = Bool
bool   arg   = def.GetBool(p + ".m_Arguments.m_BoolArgument");
int    state = def.GetInt(p + ".m_CallState");           // 0=Off 不接
```
（`p` = `"eventsToFire[" + i + "].eventToFire.m_PersistentCalls.m_Calls[" + j + "]"`）
**匹配组件那一步不用改**：仍然按「方法名 + 参数表」在原版目标节点上找（0 个报、>1 个认输）。

**切换的判据（四条全绿才动）**：
1. `数据/游戏数据/animfx_components.json` 里 `VanguardIdleEffect` 的 `m_Calls` **不再是 `<深>`**；
2. `animfx_modules.json` 里该模块的 `keys` 含上面那 20 个键（干跑已证明会有）；
3. `WarpforgeEffectLibrary.asset` 重建成 **keys 里带着这些键**（要 Unity）；
4. 切完跑 `AnimFXCheck.Run` 的 R25 那几条：`WiredCount` / `FiredCount` / `WiringProblems`
   与现在**一致**（接上 2 条、Exit 触发 1 次、无未接）。
   —— 因为表是按同一份数据生成、且 `--check` 一直保证两者同步，**行为应当一模一样**；
   不一样就说明 `def` 那条路少了什么，**回退到表**（表留着就是为了这个）。

⚠️ 切之前**别删** `module_events.json` 与那张表：它是**唯一已验过的通道**，
而且 `gen_module_events.py --check` 现在兼任「数据与表是否同步」的哨兵。

### ④-2 建议的唯一收口（`EffectName`，**等拍板，我没改**）

**收口点放在 `WarpforgeEffectPlayer`，只留一处实现，别在两个类里各剥一次**：

```csharp
// WarpforgeEffectPlayer.cs:117 —— 单一真名来源
public string EffectName
{
    get
    {
        if (_entry != null) return _entry.name;     // 正常路径（Play() 一定走这条）
        // 兜底：手工 Instantiate / 预挂实例 —— 剥掉引擎加的 "(Clone)"，并只报一次
        var n = name;
        const string k = "(Clone)";
        if (n.Length > k.Length && n.EndsWith(k, StringComparison.Ordinal)) n = n.Substring(0, n.Length - k.Length).TrimEnd();
        WarnNoEntryOnce(n);
        return n;
    }
}
```
```csharp
// WFEffectModule.cs:134 —— ⛔ 不要再退回 `name`（那是**模块所在节点**的名字，永远不是效果名）
public string EffectName
{
    get
    {
        if (Controller != null) return Controller.EffectName;
        WarnNameBeforeInitOnce();                   // 每个模块只报一次
        return "";                                  // 让「拼出来的键取不到」变**响**，不变**静默错**
    }
}
```

- **改动面**：2 个文件、2 个属性体；所有调用点（~25 处）**签名不变**。
- **风险 1**：`EffectName` 变 `""` 之后，那些 `LogWarning` 文案里的效果名会空掉
  （可在各调用点用 `string.IsNullOrEmpty(x) ? name : x`，但那样又回到两套口径 —— 建议**就让它空**，
  并且这些警告都发生在「`Controller` 为空的异常路径」上，空名字本身就是线索）。
- **风险 2**：`BattleScene` 那 5 条自检比较 `EffectName == "X"`，走的是 `Play()` ⇒ 不受影响。
- **风险 3**：`WarnNoEntryOnce` 会让「有人绕过 `Play()`」**出声**——这正是要的效果（本项目红线），
  但要确认它不会在正常的 `OnDestroy` 时序里误报（`_entry` 一旦赋值就不会被清）。
- **替代方案（更彻底，但改动大）**：在 `Init(entry, …)` 里把名字写进一个 `[NonSerialized] string _stableName`，
  `EffectName` 只读它；连 `_entry` 都不依赖。**不推荐**：`_entry` 现在就是唯一真源，多一层反而容易分叉。

---

## ⑤ 顺手发现（只报不改）

1. **`gen_animfx_modules.py` 少一条哨兵**：它已经有 `collisionEvent 子树被深度护栏截断的键：N 个`
   这条「不该有 `<深>`」的哨兵（`enc()` 里 `if "collisionEvent" in prefix: DEEP_KEYS.append(prefix)`），
   但**没有 `eventToFire` 的**。建议照抄成 `if "collisionEvent" in prefix or "eventToFire" in prefix:` ——
   否则护栏将来被谁改回去，这条链又会**悄悄**断掉，而这次是靠 R25 偶然发现的。
   ⚠️ 该文件本批按「只读/干跑」处理，**我一行没改**。

2. **`dump_animfx.py` 现在多了 `--out`**（本次新增）。它**默认行为完全不变**（不带就写正式产物），
   但以后改护栏都可以先 `--out _tmp_view/…` 干跑再落盘 —— 这条建议写进该脚本的文件头了。

3. **`WFModuleAnimation` 的别名解析失败是「静默退化」**：`EnsureClips()` 里
   `if (def == null && exit == null) return;` —— 名字拼错（无论 `(Clone)` 还是节点名）
   都会走这条，然后一路退到「Animator 没有 controller」，只靠 `Exit()` 时一条 warning 兜底
   （`WFModuleAnimation.cs:174`）。**别名取不到 ≠ 这个效果没动画**，两种情况被合并成了一条路。
   建议将来在 `EnsureClips` 失败时也 `PlayFailures++` 或报一次（与 R24 的 `PlayFailures` 口径统一）。

4. **干跑的「基线」陷阱（记一笔，别人也会踩）**：我第一次做「修前」基线时用
   `lambda path: DEFAULT_MAX_DEPTH` 一把抹掉了**全部三条**放宽规则，结果 diff 出来
   `collisionEvent` 那 355 个组件全在「新增」里，看着像是我这次改动放开了几百个字段。
   正确做法是**只去掉本次要加的那一条**。这也是「一次只放宽一条、并逐键 diff」的价值所在。
