# A192 ① 那两个 `AnimationClip` 进包 + 运行时取用接通

日期：2026-10-07 · 执行代理（全权限）· 工程 `d:/4/Unity/MyGame`
上游判据：`资料/普查产出_1007/波9离线_A136_A135.md` §3.2（那一条当时记的是「没查清这两个 GUID 落在哪个包」，
本件按派单给的结论落地，并**自己复核了 GUID→clip 的对应**，见下面「复核」一节）

---

## 1. 结论（三步各一句）

| 步 | 做了什么 | 状态 |
|---|---|---|
| ① **加线索** | 在**同一个数据表** `数据/游戏数据/animator_controllers.json` 里加了一节 `clipsByGuid`（2 条：`guid` + `name` + `source` + `pathId`）；工具侧新增 `clips_by_guid()` 按 `source` 读它 —— **工具里一个名字都没写死** | ✅ 做完 |
| ② **重打包** | 重跑 `extract_missing_shaders.py --prefabs`：`wf_prefabs_extra.bundle` 里现在 **4 条 `AnimationClip`**（原有 2 条 + **新收的这 2 条**），容器 **16 条**（7 件根 × 2 别名 + **2 条 GUID 别名**）/ 预加载 **7** 条，产物 **471650 B**（改前 469917 B） | ✅ 做完（有原始输出） |
| ③ **接线** | `ScenarioBlendables.cs` 的 `AnimationClipByGuid` 从「恒 null + 出声」改成：**按 assetGUID 从包里取原件**（`LoadFromFile` + 已加载包兜底，机制照抄 `WarpforgeAnimatorBridge`），取不到**照旧出声**（三种情形分别点名，含补救办法） | ✅ 做完（类型检查 0 错） |

🔴 **一句话提醒**：**导进来了 ≠ 会播** —— 两条原因都在下面 §3，都不是本件能补的（一条是原版自己数据差一个 `al`，一条是宿主对象归 A191）。

---

## 2. 证据

### 2.1 复核（我自己做的，别当成没查）

GUID → clip 的对应**不是照抄派单**，是从原版源包的容器表读出来的：

```text
d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/AssetBundle/AssetBundle_1.json   (m_Container 988 条，键全是 32 位十六进制 GUID)
  58db0a1f684b5ee4ba197e8d344012d2 -> PathID 1230949865609814630   == AnimationClip/AnimationClip_1230949865609814630.json  m_Name = "LightAnimationOrbit"           (m_Legacy=true, m_SampleRate=60)
  aac3fe87a4618f5478ceaad364650105 -> PathID 2397232529203406555   == AnimationClip/AnimationClip_2397232529203406555.json  m_Name = "Dark Angels Void Combat animations" (m_Legacy=true, m_SampleRate=25)
```

⇒ **原版源包的容器键本身就是 assetGUID**。这一条是本件设计的地基（见 §2.4）。

### 2.2 核对命令 + 原始输出（**可以直接复跑**）

```bash
"D:/2/Warpforge_tools/py312/python.exe" -c "
import UnityPy
p=r'd:/4/Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle'
env=UnityPy.load(p); bf=list(env.files.values())[0]
sf=next(v for v in bf.files.values() if type(v).__name__=='SerializedFile')
clips={o.path_id:o.read().m_Name for o in sf.objects.values() if o.type.name=='AnimationClip'}
ab=[o.read() for o in sf.objects.values() if o.type.name=='AssetBundle'][0]
cont={e[0]:e[1].asset.m_PathID for e in ab.m_Container}
print('[1] AnimationClips in bundle (%d):' % len(clips))
for pid,n in sorted(clips.items(), key=lambda kv: kv[1]): print('      %-20s %s' % (pid, n))
print('[2] container entries=%d  preload=%d' % (len(cont), len(ab.m_PreloadTable)))
bad=0
for g,n in (('58db0a1f684b5ee4ba197e8d344012d2','LightAnimationOrbit'),
            ('aac3fe87a4618f5478ceaad364650105','Dark Angels Void Combat animations')):
    pid=cont.get(g); ok=clips.get(pid)==n; bad+=0 if ok else 1
    print('[3] GUID %s -> PathID %s = %r  %s' % (g,pid,clips.get(pid),'OK' if ok else 'FAIL'))
    for a in (n,'assets/'+n.lower()+'.anim'):
        ok2=cont.get(a)==pid; bad+=0 if ok2 else 1
        print('      alias %-40r -> %s  %s' % (a,cont.get(a),'OK' if ok2 else 'FAIL'))
print('[4] RESULT:', 'ALL OK' if bad==0 else ('%d FAIL'%bad))
"
```

原始输出（本机实跑，2026-10-07）：

```text
[1] AnimationClips in bundle (4):
      8359881181387397000  Card Explosion
      2397232529203406555  Dark Angels Void Combat animations
      1230949865609814630  LightAnimationOrbit
      8189764618720115800  Vanguard Frame Animation
[2] container entries=16  preload=7
[3] GUID 58db0a1f684b5ee4ba197e8d344012d2 -> PathID 1230949865609814630 = 'LightAnimationOrbit'  OK
      alias 'LightAnimationOrbit'                    -> 1230949865609814630  OK
      alias 'assets/lightanimationorbit.anim'        -> 1230949865609814630  OK
[3] GUID aac3fe87a4618f5478ceaad364650105 -> PathID 2397232529203406555 = 'Dark Angels Void Combat animations'  OK
      alias 'Dark Angels Void Combat animations'     -> 2397232529203406555  OK
      alias 'assets/dark angels void combat animations.anim' -> 2397232529203406555  OK
[4] RESULT: ALL OK
```

**打包器自己那三行**（`extract_missing_shaders.py --prefabs` 的 stdout，节选）：

```text
[P0] 控制器数据表里「源包 = bundle_battleprefabs_vfxandmisc_assets_all」：AnimationClip ['Card Explosion', 'LightAnimationOrbit', 'Dark Angels Void Combat animations'] · AnimatorController ['Card 3D WH40K Explosion']
[P0] 其中**按 assetGUID 引用**（表里的 `clipsByGuid` 那一节）2 条：58db0a1f684b5ee4ba197e8d344012d2 → `LightAnimationOrbit` · aac3fe87a4618f5478ceaad364650105 → `Dark Angels Void Combat animations`
[P1] 找到 3/3 个 AnimationClip 根：LightAnimationOrbit(PathID 1230949865609814630) · Dark Angels Void Combat animations(PathID 2397232529203406555) · Card Explosion(PathID 8359881181387397000)
[P4] 按 assetGUID 登记的容器别名 2 条：aac3fe87a4618f5478ceaad364650105 → `Dark Angels Void Combat animations` · 58db0a1f684b5ee4ba197e8d344012d2 → `LightAnimationOrbit`
[7] m_Container 重写为 16 条，m_PreloadTable 补齐 7 条
[9] 已写出 …/StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle  (461 KB)
```

**「取出来的就是原件、不是空壳」**（源包 vs 产物的 `AnimationClip` typetree 逐字段哈希）：

```text
Dark Angels Void Combat animations   src ('736f4ab7abc24122', legacy=True, rate=25.0, floatCurves=2, muscleClipSize=0, has m_MuscleClip=True)
                                     prod 同上，typetree sha 相同 = True
LightAnimationOrbit                  src ('9c3298d079b05d86', legacy=True, rate=60.0, floatCurves=1, …) · prod 同上，sha 相同 = True
```

⇒ 两条都是 **`m_Legacy=true` 且带真曲线**（`m_FloatCurves` 1 / 2 条）—— 正是 `Animation.AddClip` / `CrossFade` 这条老组件路要的东西。

### 2.3 改了哪几行

| 文件 | 位置 | 改了什么 |
|---|---|---|
| `数据/游戏数据/animator_controllers.json` | `:235` `_clipsByGuid_note` · `:236-252` `clipsByGuid[2]` | **只加**（`controllers[]` 6 条一字未动、未重排；JSON 重解析校验通过：`controllers=6 / clipsByGuid=2`） |
| `工具/extract_missing_shaders.py` | `:228-231`（表头注释）· `:233-235`（`CTRL_TABLE` 注释）· `:236-254` `_load_ctrl_table()` + `controllers_of` 改用缓存 · `:258-289` `clips_by_guid()` · `:640` `repack_tree(..., extra_clip_guids=None)` · `:697-701`（①·c 注释）· `:938-952`（**容器别名里加 GUID** + `[P4]` 打印）· `:968-1046`（`run_prefabs` 合并名字 + 传 `extra_clip_guids`） | 线索读取 + 别名登记 + 日志 |
| `MyGame/Assets/CardPresentation/Battle/ScenarioBlendables.cs` | 文件头 `:17-28` · `ScenarioAnimationBlend` 类注释 `:872-884` · `clipLoader` 字段注释 · `DoScenarioBlend` 取不到时那句报错 · **`AnimationClipByGuid` `:1575` / `ClipFrom` `:1617` / `EnsureClipBundle` `:1636` / `ResetClipCache` `:1658` / 4 个静态字段 `:1663-1668`** | 恒 null → 真取原件；**取不到仍出声** |

### 2.4 映射表写在哪、为什么放这儿（派单要求说明）

- **GUID→名字的映射全仓只有一处**：`数据/游戏数据/animator_controllers.json` 的 `clipsByGuid`（2 条）。
- **C# 一侧一个 GUID、一个 clip 名字都没写死**：`AnimationClipByGuid(guid)` = `LoadAsset<AnimationClip>(guid)`，
  因为**原版源包的容器键就是 GUID**（§2.1 实读）⇒ 打包时**照原样登记一条 GUID 别名**，
  运行时这条取值路**就是原版那条路**（原版是 `AssetReferenceTyped<AnimationClip>.Load()`，手里也只有 GUID）。
  ⇒ 不需要在 C# 里抄第二份「GUID→名字」表（**符合验收第 2 条**）。
- **为什么不在 C# 里写那张 2 条的表**：那样「同一个对应关系」会在 JSON 与 `.cs` 里各存一份，
  改一处忘一处就是**静默取错 clip**；而运行时又读不到 `数据/游戏数据/*.json`（不在 `Resources/` 下）。
  走「GUID 当容器键」这条路，**数据表是唯一真源、表变了重跑一次工具即可**（工具读的就是它）。
- **为什么不做成「先查名字、再按名字取」**：名字那一跳需要一个 C# 侧的映射（同上），且名字里的空格/大小写
  还得再抄一份别名规则；GUID 直取少一跳、少一处能写错的地方。
- 表里那节是**手加**的（生成器 `gen_animator_controllers.py` 不认识它）—— 已写进表自己的
  `_clipsByGuid_note`，见 §3·4。

---

## 3. 没查清 / 已知未生效（⛔ 不猜、不假装能播）

### 3.1 🔴 **这两条 clip 现在取得到，但（暂时）都不会真的播** —— 两条原因都在别处

1. **`LightAnimationOrbit` 拿不到「消费它的那一颗」**：场景实例 `Scenario/Directional Light` 的
   `filterCode` **实读是 `LightAnimationOrbital`**，而 SO `Environmental Condition Dark Angels Orbiting` 里
   那条写的是 **`LightAnimationOrbit`** —— **原版自己差一个 `al`，那一对永远匹配不上**
   （`DoScenarioBlend` 第一句 `if (atc.filterCode != this.filterCode) continue`）。
   ⇒ **照抄，不替原版改数据**（铁律 2/3）；`filterCode` 那一节一个字都没动。
2. **`Dark Angels Void Combat animations` 的宿主不存在**：那颗 `ScenarioAnimationBlend` 挂在
   `Scenario/Battle Arena Dark Angels baked` 上，**那个分组节点我们工程里没有**（`ArenaBuilder` 建场时没建）
   —— **那是 A191，不归本件**。
3. **并且**：我们 13 件 arena prefab 里**一个 `Animation` 组件都没有**（`myAnimation` 恒 null）⇒
   即使上面两条成立，`DoScenarioBlend` 也会在 `myAnimation == null` 那一支 `LogError` 点名并返回。
   ⇒ 本件接通的是「**clip 取得到**」这一段；「**谁去播**」这一段还缺（A191 + `Animation` 组件两件）。

### 3.2 ⚠️ **「按 GUID 取得到」这一跳没在 Unity 里实跑过**（本件按红线不跑 Unity）

能证明的三件事：① **原版的容器键就是 GUID**（§2.1 实读）② **重打包后容器里确实有这条键**（§2.2 实读）
③ **同一条取值路有先例**——`WarpforgeAnimatorBridge` 从**同一只包**用裸名做过
`LoadAsset<RuntimeAnimatorController>(名字)`（2026-10-01 起跑通，`AnimClipProbe` 量过）。
⇒ 但 `LoadAsset<AnimationClip>(guid)` 这一跳**没在 Unity 里量过**：`AssetBundle.LoadAsset(string)` 认的是
**容器键**（键里有空格、大小写混排的先例已有），而**GUID 形态的键是我们新加的**，只有静态证据。
**建议的验收路**（留给调度台/A191 之后的批次）：等宿主/`Animation` 组件齐了，在 `BattleScene.Run` 里
切到 darkangels 那条环境后加一条「取到的是原件（`length`/`legacy`）+ `Animation.IsPlaying`」的断言；
**在那之前别把这一跳算成「已验收」**。

### 3.3 自检没跑（本件也不该跑）

按铁律 12：**Unity 自检由调度台在同步点跑**，本件只跑了**秒级类型检查**：
`TMPDIR=/tmp/wf_w9b bash d:/4/Unity/工具/typecheck.sh` → **运行时错误数 0 · 编辑器错误数 0**。
⚠️ 覆盖本件的自检宿主是 **`BattleScene.Run`**；但要说清：**上面 §3.1 那三条决定了这条路上
`clipLoader` 在自检里多半一次都不会被调到**（filterCode 不匹配）⇒ 「自检绿」**不能当成「这条接通了」的证据**，
真正的证据是 §2.2 那份包内容核对。

### 3.4 已知的两处「下次会踩」（本件没权限修，如实记）

1. **重跑 `工具/gen_animator_controllers.py` 会把 `clipsByGuid` 整节冲掉**（生成器只写 `controllers`）。
   冲掉之后：工具收不到那两条 clip ⇒ 重打包后 GUID 别名消失 ⇒ 运行时 `LogError` 点名 GUID（**不静默**），
   但现象会像「本件白做了」。⇒ 要长留就得给生成器加「保留/重写 `clipsByGuid`」那一段（**不在本件白名单**）。
2. **`工具/_verify_prefab_bundle.py` 的期望值已过期**（它不是本件白名单里的文件）：它写死
   `WANT = 5 件根` ⇒ 现在会报 `预加载表 7 条（应为 5 = 根个数）🔴` 并 `exit 1`。
   新期望应是 **7 件根 / 容器 16 条 / 预加载 7 条**（并把两条 GUID 别名也纳入核对）。
   ⚠️ 顺手实测：这个脚本**没有 `sys.stdout.reconfigure(encoding='utf-8')`**，`> log.txt` 时会在
   打印 `✅` 那一步 `UnicodeEncodeError` **整脚本崩掉**（与 `extract_missing_shaders.py` 顶上那段注释
   记的是同一个坑）—— 复跑时请**直接在终端里跑**，别重定向。
   ✅ **2026-10-07 已从脚本侧修掉**（`工具/_verify_prefab_bundle.py:62-68` 加了 `sys.stdout.reconfigure(encoding="utf-8")` 兜底
   ⇒ 重定向到文件实测 exit 0、不再崩）；这一条里的**期望值那半也已修**（`5 件根` → **7 件根 / 容器 16 条 / 预加载 7 条**
   + 两条 GUID 别名逐个查「有 / 没有」并查「指向对不对」）。⚠️ 顺带更正下面那句的**边界**：只说「别重定向」偏窄 ——
   实测**只要 stdout 被接走**（**管道**或重定向）当年都会崩（真·交互式控制台大概不崩，**未验**）。
   全过程 → `资料/普查产出_1007/波9_A192收尾_验证脚本与生成器.md` §2.1。

### 3.5 「顺手发现」（⛔ 我没改，按规矩报给调度台）

1. **`Core/EnvironmentConditions.cs` 的注释已过期**（黑名单，没改）：`:30` 与 `:72` 还写着
   「那两条 clip **不在**我们工程里 / 我们工程里**没有这两个 clip 资产**」—— 本件之后**不成立了**。
   （同文件 `:34` 那句「运行时出声」仍然成立。）
2. **`Vanguard Frame Animation` 这条 clip 有一条真外部引用**：`m_FloatCurves[0].script → m_FileID=1`
   （打到别的包去），是 `--prefabs` 那批的**既有状况**（工具 `[P3] 真外部引用` 一直在报）。
   我复核过：**本件新收的两条 clip 一条外部引用都没有**，是自足的。
3. **`--prefabs` 是「一个命令三只包」**：这次也顺带重写了 `wf_menus_extra.bundle`（360650 B）与
   `wf_boosters_extra.bundle`（5972456 B）—— 两件**字节大小与改前一致**，预期内容无变化，只是 mtime 变了。
4. **产物不进 git**：`StreamingAssets/WarpforgeVFX/` 整目录被 `.gitignore:33` 排除
   ⇒ **别的机器要自己重跑一次** `python 工具/extract_missing_shaders.py --prefabs`（既有状况）。

---

## 4. 改动清单（逐文件 + `git diff --numstat`）

| 文件 | numstat（相对 HEAD） | 说明 |
|---|---|---|
| `Unity/数据/游戏数据/animator_controllers.json` | **`19  0`** | 全部是本件（纯新增，0 删除） |
| `Unity/工具/extract_missing_shaders.py` | **`94  11`** | 全部是本件（改前该文件**没有**未提交改动） |
| `Unity/MyGame/Assets/CardPresentation/Battle/ScenarioBlendables.cs` | `843  80` | ⚠️ **含上一个写手**（A136/A135）未提交的 `+737/−81`（那组数字来自派单简报；与 numstat 差 1 行，口径可能略有出入）；**本件净 +108 行**（改前 1577 行 → 改后 1685 行，5 处编辑，按行数直算） |
| `Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle` | **（被 `.gitignore:33` 排除，不进 git）** | 469917 B → **471650 B**；容器 10 条 → **16 条**；预加载 5 → **7** |
| `…/wf_menus_extra.bundle` · `…/wf_boosters_extra.bundle` | （同上，被排除） | 同一条命令顺带重写，字节大小不变 |
| `Unity/资料/普查产出_1007/波9_A192_两个clip进包.md` | 本报告（新文件） | —— |

行尾：三个被改文件**改完仍是纯 LF**（Edit 工具改的，未用 `sed -i`/文本写；`ScenarioBlendables.cs` 现 1685 行 / CRLF **0**）。
类型检查：**最后那次注释改动之后又跑了一遍** `typecheck.sh` → 运行时 0 / 编辑器 0。
`git status`：本件只动了上面这些（**没碰** `Shell/**` · `Editor/**` · `Core/**` · `Deck/**` · 两个 `gen_*.py` · 两张正本 · `d:/2/**`）。
