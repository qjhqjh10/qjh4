# 地址表：CardAnim → AssetGUID → 本地 prefab/资产（2026-09-29）

> 工具：`d:/4/Unity/工具/gen_anim_address_map.py`（可重跑、幂等、只读 `d:/2/`、只写下面那个 json）
> 数据：`d:/4/Unity/数据/索引/anim_address_map.json`（5.2 MB）
> 跑法：`PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_anim_address_map.py`
> 顺带跑：`--quiet`（不打未解出明细）· `--limit-bundles N`（调试，只处理前 N 个包）

用途：① 从 `CardAnim` 名找本地那个 **VFX prefab**；② 从逐卡 `animInfo.animAdressable` 找对应特效。
正本待办 = `项目任务.md` §〇 第 5 条末的 **trait 粒子**（链路 `CardAnim → AssetGUID → 本地 VFX prefab`）。

---

## 一、规模

| 项 | 数 |
|---|---|
| 覆盖 bundle | **84**（`assets_full` 下全部有 `AssetBundle/m_Container` 的包） |
| 容器项总数 | **13448**（`m_FileID` 全 0 ⇒ 全是包内引用，不用跨包解依赖） |
| 唯一容器键 | **10325**（GUID 键 10104 · 非 GUID 键 221） |
| 键 → 多对象 | 2490 个键对应 ≥2 个对象（Addressables 把**主资产 + 子资产**挂在同一 GUID 下，例：`008b2df2…` → Texture2D + 同名 Sprite）；主记录进正字段、其余进 `also` |
| pathId 反解 | **13432 / 13448**；缺的 16 条全是 `m_PathID == 0` |
| dump 路径定位到 | **13429 / 13448**；剩 3 条没有 dump 文件 |
| **CardAnim 名字** | **1099 个**（14 个 `bundle_*cardanims*` 共 1097 + `battleprefabs` 里 2 个 `Invoke Minion Card Fade *`） |
| ├ 解出（名字+类型+dump 三样齐） | **1067**，且 **1067/1067 全是 `GameObject`，全在 `bundle_battleprefabs_vfxandmisc_assets_all`** |
| ├ `m_AssetGUID` 为空 | **27**（条目照进表，`unresolved` 写明） |
| └ GUID 悬空 | **5** |

## 二、join 办法（一句话）

**走「UnityPy 重读原 bundle」+「文件名后缀」两条并用**：`m_Container[键].asset.m_PathID` 就是该 bundle 内对象的 `path_id`（用 `env.objects` 建 `{path_id: obj}` 反解出类型与名字）；落到哪个 dump 文件则按 `extract_full.py:183-198` 的命名规则**在磁盘上反查** —— 先 `{safeName}_{pathId}.<ext>`（同名重复才有后缀），再 `{safeName}.<ext>`（同名里的第一个），最后 `{Type}_{pathId}.<ext>`（兜底名）。
⚠️ **光靠文件名后缀不够**：同名里**第一个**是不带后缀的 —— 实测 `a5834a7ae2dd92745a7d0e2637239c47` → `-1939833645471234905` → `battleprefabs…/GameObject/Swarm_Trigger_OnTarget.json`，文件名里**没有**这个 path_id。

## 三、没解出的原因（前 3 大，各带例子）

1. **`m_PathID == 0`（16 条，占未解出的最大头）** —— 容器项压根没带本地对象指针。
   例：`bundle_scenes_scenes_battlearena1` 的键 `Assets/Data/Scenes/Battle Scenarios/Battle Arena 1.unity`（场景 bundle 的键是**资产路径**不是 GUID，指向场景自身）。
2. **GUID 悬空：m_Container 与 `catalog.bin` 里都搜不到（5 条 CardAnim）** —— 该资产没随包发。
   例：`cardanimsgeneral/MonoBehaviour/VanguardProcAnim.json` 的 `bde8e58a2e58b114b95b63edf4d8ad3e`，**全库只有它自己这一处**提到这个 GUID。另 4 个：`VanguardTraitPlay` · `VanguardTraitStart` · `Rally_MasterOfExecutions` · `Self-Destruction_Effect`。
3. **`m_AssetGUID` 为空（27 条 CardAnim）** —— 例：`SwarmAnim`。这些**照进表**，`unresolved` 写明。
   （另：3 条 `Mesh` 有对象但磁盘上没有 dump 文件 —— `CandleFlame_mid/big/small`，`fix_exports.py` 没写出来；`_stats.json` 只统计主循环、不计这一步。）

## 四、示例查询

**从 CardAnim 名打出它的 prefab 名**（子串匹配，直接把 `<关键词>` 换掉）：

```bash
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" -c "import json;d=json.load(open(r'd:/4/Unity/数据/索引/anim_address_map.json',encoding='utf-8'))['cardanim_to_asset'];import sys;[print(k,'->',v.get('targetName'),'|',v.get('targetType'),'|',v.get('dump')) for k,v in d.items() if sys.argv[1].lower() in k.lower()]" Swarm
```

实测输出：`SwarmTraitFromCode -> Swarm_Trigger_OnTarget | GameObject | bundle_battleprefabs_vfxandmisc_assets_all/GameObject/Swarm_Trigger_OnTarget.json`

**反向：从 guid 打出 prefab**（子资产会一起带出）：

```bash
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" -c "import json,sys;d=json.load(open(r'd:/4/Unity/数据/索引/anim_address_map.json',encoding='utf-8'))['guid_to_asset'];print(json.dumps(d.get(sys.argv[1]),ensure_ascii=False,indent=1))" 5792bee22b9580d4db0e70516adebe4f
```

## 五、自检 / 旁证（每次重跑都会打出来）

- **跟三张老表逐条交叉校验，guid 0 条不符**：`animinfo_lookup.json` 418/418（按 `m` 对齐，只比前 8 位）· `animinfo_0824.json` 591 条（565 相同 + 26 两边都空）· `card_anim_map.json` 506 条（505 相同 + 1 两边都空）。
- **`_stats.json`：84 个 bundle 的 dump `fail` 全为 0** ⇒ 「按名字反查 dump 文件」这条路对 bundle 对象是安全的（有 fail 就会落错到同名兄弟身上，脚本会点名报警）。
- **`aa/catalog.bin` 旁证**：10104 个 GUID 键里 **10102 个**能在 catalog 的 hex 串里找到（判「悬空」用的就是它）。
- **幂等**：连跑两次，除 `generatedAt` 外逐字节相同。

## 六、坑（别推翻）

1. **`extract_full.py` 只对 6 类读名字** —— `("MonoBehaviour","GameObject","Sprite","Texture2D","AudioClip","Mesh")`。别的类型（**Material / SpriteAtlas / AnimationClip / ComputeShader / AudioMixer\* / Cubemap / LightProbes / VisualEffectAsset**）**一律是兜底名** `{Type}_{pathId}`：实测 `battleprefabs…/Material/` 下 90 个文件**全部**叫 `Material_<pid>.json`。不认这条会白丢 300+ 条。
2. **`fix_exports.py` 是另一套名字集合**（`Texture2D/AudioClip/Mesh/Font/VideoClip/TextAsset`），而且**没有 pathId 消歧后缀**（同名直接覆盖）。VideoClip 就栽在这 —— `Defeat Video.mp4` 是按名字写的。
3. **两个 `chaosspacemarinesemperorschildren*` 包的容器键根本不是 GUID** —— `bundle_chaosspacemarinesemperorschildrencardanims_assets_all` 52 个键、`…cardassets_assets_all` 278 个键，全是 `'0'` `'0c'` `'1b'` 这种短 hex（该包 **GUID 键 0 个**）。所以「EC 阵营的卡面资产按 GUID 查不到」是**原版就这样**，不是我们解错。
