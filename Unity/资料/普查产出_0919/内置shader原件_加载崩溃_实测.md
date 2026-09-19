# 内置管线 shader 的「原件」其实能用 —— 差点被一个重打包的坑判成走不通（2026-09-19）

> **一句话**：`Mobile/Particles/*` · `Legacy Shaders/Particles/*` · `Particles/Standard Unlit` 这 **8 个内置管线 shader**
> 的**原件一直在原版包里**，而且**在 URP 工程里照常渲染** —— 我们自建近似顶了很久，
> 理由是「Built-in 老 shader 在 URP 工程里渲染不了」，**那条论断从没实测过**
> （`项目任务.md` ⛔ 行 ④ 原文就写着「可验的一条路 = 直接挂一次看渲不渲得出（约半小时）」）。
> **这条路 2026-09-19 走通了**，中间差点因为一个 UnityPy 重打包的坑把它判成「Unity 加载即崩、走不通」。
>
> 🔴 **本文是这条路的唯一正本**：判据、变体、崩溃栈、根因、复现命令都在这儿。
> 关联：`资料/普查产出_0919/E组_shader算式_块3_内置粒子族.md`（那批算式：**现在用不上了**，见 §六）·
> `资料/特效还原_进度与交接.md` §三（shader 三个包的清单）· `项目任务.md` ⛔ 行 ④。

---

## 一、这 8 个是谁、影响多少

`WarpforgeShaderMap.Replacements` 里**仍走自建**的只有 12 个名（其余 21 个早走原件）：

| 名 | 引用效果数 | 自建替代 |
|---|---|---|
| `Mobile/Particles/Additive` | **423** | `WarpforgeVFX/Particles/Extra Color` |
| `Mobile/Particles/Alpha Blended` | **159** | 同上 |
| `Legacy Shaders/Particles/Additive` | 19 | 同上 |
| `Particles/Standard Unlit` | 17 | 同上 |
| `Legacy Shaders/Particles/Alpha Blended Premultiply` | 9 | 同上 |
| `Legacy Shaders/Particles/Anim Alpha Blended` | 8 | 同上 |
| `Legacy Shaders/Particles/Alpha Blended` | 4 | 同上 |
| `Mobile/Particles/Multiply` | 3 | 同上 |
| **小计** | **642** | |
| `Particles/Additive` | **0** | **死条目**（原版包里根本没有，别实现） |
| `Everguild/FX/Extra Color`（741）· `Everguild/FX/Particle Distortion Affect Transparents`（233） | — | **暂缓的两个大头**，不在本次范围 |
| `TextMeshPro/Distance Field Offset` | 1 | 指到工程自带的 TMP Distance Field |

引用数出处：`资料/普查产出_0917/效果_shader对账.tsv`（按 `,` 切第 3 列统计）。

---

## 二、原件在哪个包

`D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/Warpforge_unitybuiltinassets.bundle`
（**106 KB / 30 个对象**）——

- **15 个 Shader**：这批内置管线 shader（`Mobile/Particles/*` ×3 · `Legacy Shaders/Particles/*` ×4 ·
  `Particles/Standard Unlit` · `UI/Default` · `Sprites/Default` · `Sprites/Mask` · `Skybox/Cubemap` ·
  `Legacy Shaders/VertexLit` · `Hidden/VideoDecode` · `Hidden/VideoComposite`）
- 另有 Texture2D ×6 / Material ×3 / Sprite ×5
- 构建版本 **Unity 6000.2.6f2**（本工程 6000.3.23f1，**同一大版本**；我们已经在用的
  `wf_shaders.bundle` 也是这个版本，加载链早验证过）

**为什么原版包里会有内置 shader**：游戏是 URP，但材质引用了这批 legacy unlit 粒子 shader ⇒ 出包时被打进随包。
**它们没有光源 tag / 没有 forward base pass**，所以「URP 渲染不了」对**lit 的内置 shader**成立、对这批**不成立**。

---

## 三、实测：七个变体，崩与不崩

### 3.1 第一次尝试：逐字节复制 → 加载成功但**一个资产都取不到**

`LoadAllAssets<Shader>()` = **0 个**。原因和当年 `battleprefabs` 那个包一模一样：
**`m_Container` 是空的**（实读 0 条），Unity 枚举不出资产 ⇒ 只能重打包。

### 3.2 重打包之后：**段错误**

用 `工具/extract_missing_shaders.py` 的同一套做法（只留 Shader + AssetBundle、重写容器、丢资源流）重打，
加载时**直接段错误**（exit 139）。崩溃栈：

```
(Unity) AddAssetsToPreload
(Unity) PreparePreloadAssets
(Unity) ProcessAssetBundleEntries
(Unity) LoadAssetWithSubAssets_Internal
(Unity) AssetBundle::LoadAssetWithSubAssets_Internal
UnityEngine.AssetBundle:LoadAllAssets<T>()
WarpforgeVFX.WarpforgeShaderLoader:LoadShadersFrom
```

### 3.3 逐条排除（每一条都是一次独立的 Unity 运行）

| # | 变体 | 结果 | 排除了什么 |
|---|---|---|---|
| 1 | 容器只指 1 个 shader（`Sprites/Default`） | **崩** | 「某个特定 shader 有毒」 |
| 2 | 把**容器名**改成 `WarpforgeVFX/Original/…` | **崩** | 「名字撞上引擎内置注册表」 |
| 3 | 把 shader **自己的名字**（`m_ParsedForm.m_Name`）也改掉 | **崩** | 同上，连内部名也不是元凶 |
| 4 | **一个对象都不删**（保留 Texture2D/Material/Sprite） | **崩** | 「删对象造成悬空引用」 |
| 5 | 容器只指**非 shader**对象（贴图/材质/精灵） | ✅ **正常** | **包本身、容器机制、改写方式都没问题** |
| 6 | 容器只指**材质**（材质会带出它引用的 shader） | **崩** | 「只要不经容器直接点名 shader 就行」 |
| 7a | 容器 `preloadIndex=i` + **补齐 `m_PreloadTable`** | ✅ **15 个全载入** | —— |
| 7b | 容器写 `preloadSize=0` | ✅ **15 个全载入** | —— |

### 3.4 根因

**`m_Container` 里的 `preloadIndex` 是「对 `m_PreloadTable` 的索引」——两张表必须一起写。**

`extract_missing_shaders.py` 原来把容器写成 `preloadIndex=0/preloadSize=1`，
而**源包的 `m_PreloadTable` 是空的**（内置包 = 0 条）⇒ Unity 在 `AddAssetsToPreload` 里**越界索引** ⇒ 段错误。
**引擎不报错、直接崩**，所以症状看起来完全像「Unity 不支持加载内置 shader」。

🔴 **这个坑一直藏着**，因为上一个源包 `battleprefabs_vfxandmisc_assets_all.bundle` 恰好带着
**86348 条**的预加载表（其中 85667 条指向已删对象，但索引 0 在界内）⇒ 侥幸能跑。
换成预加载表为空的源包立刻现形。**「能跑」不等于「写对了」。**

---

## 四、修好之后的验证（2026-09-19 全部实跑）

| 检查 | 命令 | 结果 |
|---|---|---|
| 工具修好、包重生 | `extract_missing_shaders.py --builtin` | `m_Container 15 条 / m_PreloadTable 补齐 15 条`，产物 **89 KB** |
| 原件**渲得出**吗 | `BuiltinShaderProbe.Run` | **8/8 渲出内容、洋红 0.0%**，合计 **9 通过 / 0 失败** |
| 原件 vs 自建差多少 | 同上 | 亮度比 `Additive 1.253` · `Alpha Blended 1.0` · `Multiply **0.174**` · `Standard Unlit **0.654**` · `Legacy Additive 1.142` · `Legacy Alpha Blended 0.82` · `Legacy Premultiply 1.0` · `Legacy Anim 0.963` |
| 白名单真的生效 | `ShaderResolveProbe.Run` | **改走原件白名单：29 个走原件 / 0 个没走成**；点名 6/6 |
| 混合与解析没被带坏 | `BlendProbe.Run` | **混合 1099 通过 · shader 解析 70 通过 / 0 失败** |

⚠️ **那个亮度比只当线索，不当判据**（拍的是「整屏一块四边形」这种极端工况，而且判定级噪声底是
「E ±1、`|ln| ≲ 0.6` 分不出真假」）。`Multiply` / `Standard Unlit` 那两处差得很大 ⇒ 自建那套算法
在这两个名上**本来就不对**，但这要**全量 sweep** 才定得下来（见 §七）。

---

## 五、动了哪几处（判据只有一处）

| 处 | 改了什么 |
|---|---|
| `WarpforgeShaderLoader.cs` | 新增第三个包 `BuiltinBundleRelPath = "WarpforgeVFX/wf_builtin.bundle"`，与主包/补充包并列加载；冲突处理从「只看主包」改成「任一包被顶掉就捡」 |
| `WarpforgeShaderMap.cs` | `UseOriginal` 加这 8 个名；🔴 **解析顺序改了**：`wantOriginal` 的名字**先问 bundle**，再回落 `Shader.Find(原版名)` —— 不换序的话这 8 个名在编辑器里 `Shader.Find` 拿得到（引擎自带的那一份），白名单**永远走不到 bundle 且是静默的**。对原有 21 条零影响（都是私有名，`Shader.Find` 本来就返回 null），由 `ShaderResolveProbe` 的 29/29 断言钉住 |
| `工具/extract_missing_shaders.py` | ① `repack()` 补写 `m_PreloadTable` + 逐个 `preloadIndex`（§3.4）② 新增 `--builtin` 模式 ③ 更正文件头那条**已作废的版权红线** |
| `BuiltinShaderProbe.cs`（新） | `Run` = 挂上去渲一次量 lit/洋红；`Diagnose` = 逐包加载诊断（`BSP_DIR` 环境变量可指向一个装满小包的目录做二分，**崩了也还剩前面的输出**） |

⚠️ **`StreamingAssets/WarpforgeVFX/*.bundle` 是 gitignore 的本地件**（原版编译字节码，不进仓库）
⇒ 换机器/重拉仓库后要重跑 `extract_missing_shaders.py --builtin` 把 `wf_builtin.bundle` 生成出来。

---

## 六、对 E 组那批算式文档的影响

`E组_shader算式_块3_内置粒子族.md` 的前提是「这 9 个内置 shader 只能继续用自建」，
于是给出「**先只给 A 档那 2 个换乘子**、C 档 4 个整条算式另写」的方案。
**这个前提现在不成立了**：这 8 个名改走原件之后，

- **A 档 4 个**（`Mobile/Particles/{Alpha Blended, Additive}` · `Legacy .../Alpha Blended{, Premultiply}`）：
  原件就是原件，**不用再管**（自建近似里那套「不该乘 `_Color`」的推理也随之作废）
- **B 档 2 个**（`Legacy .../Additive` 的 `_TintColor` · `Particles/Standard Unlit` 的 `cb0[5]`）：
  原件自己会读这些属性 ⇒ **不需要给自建加「颜色乘子模式」**
- **C 档 4 个**（`Mobile/Particles/Multiply` · `Legacy .../Anim Alpha Blended` · 以及 `UI/Additive`——它**早已走原件**）：
  同上
- **`Particles/Additive`**：仍然是死条目，仍然别实现

⚠️ **块1（遮罩族）· 块2（预乘与拖尾族）· 块4（走原件族）不受影响** —— 那些名字本来就在
`UseOriginal` 里或属于「暂缓的两个大头」，本次没动。

---

## 七、全量 sweep 验收（2026-09-19 晚跑完）

两趟分进程（`WFSWEEP_SIDE=orig` → 不设），各 **957 成功 / 1 跳过**，`analyze_sweep.py` 已重生台账。

**改动确实生效**：exp 趟日志里 `Mobile/Particles/Additive → 原版bundle（白名单）`、
`共 101 个原版 shader 可用（主包成功 + 补充包 + 内置包成功）`、`找不到 shader` **0 次**。
（**原版那一侧不走 binder**，用的就是 bundle 自带材质 ⇒ 参照系没动，变的全是导出侧。）

### 7.1 结果：**净改善，但幅度不大 —— 是「换了一种问题」不是纯赚**

| 指标（921 个可比效果） | 改前 | 改后 |
|---|---|---|
| 逐时刻 `\|ln((exp+1)/(orig+1))\|` 的均值（**无掩码**）· 中位 | 0.0681 | **0.0654** |
| 同上 · 均值 | 0.1646 | **0.1510** |
| 同上 · `<0.05` 的条数 | 404 | **411** |
| **变好 / 变差 / 持平** | — | **52 / 24 / 845** |

**典型好转（逐时刻亮点数，原版 → 导出改前 → 导出改后）**：

- `Screen_Gold`：`1360/2305/3074/3457/3442/3527/2794` → `1044/1914/2714…` → **`1364/2312/3081/3457/3443/3529/2792`（几乎逐帧相同）**
- `BloodySplash`：`144/294/420/381` → `165/455/723/381` → **`144/296/421/381`**
- `StunEffect_proc`：台账 偏暗 0.731 → **对得上 0.037**（内容时段也对上了）

### 7.2 🔴 顺带暴露一个**尺子的弱点**（记着，别当成「这次改坏了」）

`Protocol Base` 与 `Sau_DamageProtocol` 在台账里从「对得上」变成「偏亮」（`|ln|` 0.045 → 0.514 / 0.039 → 0.344），
**但真相是改好了**：改前导出侧在 8 个时刻里有 **5 个（/ 3 个）压根没渲出任何像素**
（`Protocol Base` 改前 `lit = 0,0,0,0,0,249,169,0` vs 原版 `42,65,79,74,82,295,168,0`），
而台账按「两边都有内容的时刻」算比值 ⇒ **把它判成了「对得上」**。
改后导出侧在全部时刻都有内容（`89,116,123,119,123,373,169,0`），但**偏亮 ~1.7×** ⇒ 被判偏亮。

⇒ **两条要记住的**：
1. **台账判定是挂在这个掩码上的** —— 「导出侧某些时刻是空的」不会让它变红（`原版有内容时段` / `导出有内容时段` 两列
   记下了差异、但**不参与判定**）。看到「对得上」时，**顺手扫一眼那两列**。
2. 🔴 **`Protocol Base` / `Sau_DamageProtocol` 这类「改后偏亮 1.4~1.7×」是真问题，但它是新暴露的、不是这次改坏的** ——
   下一步该查**导出材质的属性值 vs 原版材质的属性值**（`WFCMP_MATDUMP` + `工具/diff_matdump.py`），
   而不是回滚这次改动。

### 7.3 那 24 条「变差」的分类

逐条查过：**没有一条是「改前有空帧」那类假象**；幅度多数只有 **+0.02~0.05**（在尺子的噪声底附近），
只有 4 条在 +0.1~0.16（`EnvironmentalCondition Sororitas Raging Storm` · `BulletImpact_1shot_random_chaos` ·
`Space Wolves Summon OLD` · `BulletImpact_Tyranid_Stranglethorn`）。**判据留在 7.1 那张表里，别拿单条摆动作结论。**

### 7.4 结论：**保留这次改动**，理由

1. 原件按定义就是正确目标 —— 任何偏离都是**我们导出/绑定链的问题**，是能修的；反之自建近似是「永远对不了」的。
2. 有 52 条实打实变好、其中若干条**逐帧几乎完全相同**；变差的 24 条多数在噪声内。
3. 净指标（中位/均值/达标条数）三项都朝好的方向动。

### 7.5 还没验的（别当成已定案）

1. **真包（player）没验** —— 编辑器里能加载 ≠ 打包后能加载。这 8 个名现在有 642 处效果引用，
   真要收口得跑一次 `-wfdrive`（见 `资料/特效还原_进度与交接.md` §七 步 5）。
2. 🔴 **7.2 那条真问题**：新暴露的「偏亮 1.4~1.7×」要按材质属性逐个对（`diff_matdump.py`）。
3. **`wf_shaders_extra.bundle` 里 85667 条悬空预加载引用**（源包遗留）—— 现在能跑，
   但既然根因是「预加载表与容器索引必须配套」，**要不要把补充包也照新写法重生成一次**是个待定项
   （⚠️ 重生成会改变 shader 字节码所在文件 ⇒ **必须重新扫一遍 sweep**）。
4. **本次没动的两个大头**（`Everguild/FX/Extra Color` 741 · `Particle Distortion` 233）—— 仍走自建，
   `E组_shader算式_块1~4` 的 A/B/C 判定**留给它们**。

---

## 八、复现命令

```bash
# 1. 打内置包（幂等）
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" \
  d:/4/Unity/工具/extract_missing_shaders.py --builtin
#    --check 只体检不写文件

# 2. 挂上去渲一次，量 lit / 洋红 / 原件vs自建亮度比
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod BuiltinShaderProbe.Run \
  -logFile "d:/4/_tmp_view/builtin_shader.log"        # 筛 "^BSP "

# 3. 逐包加载诊断（可选；BSP_DIR 指向一个装满小包的目录 = 二分实验）
BSP_DIR=<目录> ... -executeMethod BuiltinShaderProbe.Diagnose -logFile "d:/4/_tmp_view/bisect.log"

# 4. 两条守卫
... -executeMethod ShaderResolveProbe.Run -logFile "d:/4/_tmp_view/shaders.log"   # 筛 "^SRP "
... -executeMethod BlendProbe.Run         -logFile "d:/4/_tmp_view/blend.log"     # 筛 "^BP "
```
