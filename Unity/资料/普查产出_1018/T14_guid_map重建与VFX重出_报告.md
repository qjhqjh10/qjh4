# T14 · 重建 `guid_map` + 用 1,351 张真值重出逐卡 VFX —— 收口报告

> 日期：2026-10-18（第五十会话）· 线：**特效线 `T14`**（关联战斗线 `T21` 余账）
> ⚠️ 本轮按现行口径：**只做不验** —— 没跑任何 `*.Run`、没进游戏；`gen_*` 是纯 Python（不涉 `.cs`，typescript 检查不适用）。
> 🔴 **2026-10-18 同日订正（`K06` 之后）**：池内 114 张旧 id 已重编号为真 id ⇒ 所有卡都按 **id** 命中真值
> （不再有 37 张走名字匹配）⇒ 覆盖率再升到 **1,071 / 1,127（95.0%）**、`row_but_event_never` 36→**18**、`no_row` 40→**38**。
> 判据 → `资料/普查产出_1018/K06_池id迁移_报告.md`。下文 §一/§四 的数字是 **K06 之前**的读数（保留留痕）。

---

## 一、结论（一句话 + 数字）

**逐卡 VFX 覆盖率 983 → 1051 / 1127（93.3%）**；`attack_ranged` 逐卡条目 465 → **526**；
**117 张「无逐卡行」→ 40 张**；新增 `guid_map` 覆盖 **2,372 → 6,140 个 GUID**。

| 口径 | 之前（旧 2022 快照） | 现在（当前 build 1,351 张） |
|---|---|---|
| 真值快照卡数 | 1,195 | **1,351** |
| 池内卡在快照里查不到 | 168 | **1** |
| `guid_map` 覆盖 GUID | 2,372 | **6,140** |
| 权威档条目 / 卡 | — / 959 | **3,983 条 / 1,126 张** |
| 运行时表（`Resources/`） | 1,017 张 / 1,766 对 | **1,096 张 / 2,009 对** |
| **可达覆盖率（P3 §2·1 口径）** | **983 / 1,127** | **1,051 / 1,127（93.3%）** |
| 无逐卡行 | 117 | **40** |

分类型（可达 / 总数）：unit **567/586** · hero **56/56** · tactic **393/446** · defence **35/39**。

---

## 二、根因（为什么旧的解不出）

旧的 `资料/普查产出_第二十七会话/guid_map.tsv`（4,985 行 / 2,372 GUID）由
`工具/_guid_resolve_probe.py map` 生成，而**它从 Addressables 目录（`catalog_main.json`，2025-07-30 快照）出发**：
目录里没有的 GUID **根本没被查过** —— 哪怕那个资产**就在本地束里**。

实测：当前 build 引用的 **276 个**缺失 GUID 中，**直接枚举本地束的 `AssetBundle.m_Container` 就解出 272 个**。
其中 SW 一族最集中：`spacemarinesspacewolvescardassets` 一处中 **114 个**、`…cardanims` 中 **24 个**。

> 🔑 **判据**：容器键本身就是 **GUID（32 位十六进制）** —— 旧表 4,704/4,985 行都是这个形态。
> 所以「GUID → 资产」**不需要目录**，写 `AssetBundle` 对象的 `m_Container` 就够了。

---

## 三、改动清单

| 文件 | 改了什么 |
|---|---|
| 🆕 `Unity/工具/gen_guid_map_from_bundles.py` | 新工具：直接枚举本地束的 `m_Container` → `GUID → 束 / 容器键 / 类型 / 类名 / 资产名`；**旧表只按缺口回填**（148 行），⛔ 不覆盖旧表 |
| 🆕 `Unity/资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` | 产物：**6,173 行 / 6,140 GUID**（新枚举 6,025 行 + 旧表回填 148 行） |
| `Unity/工具/gen_card_vfx_by_card.py` | ① `CARD_SO_TRUTH` 换当前 build 的 `cards_raw.json`；② `GUID_MAP_TSV` 换新表；③ `build_authoritative` 兼容 `{cards:[{full_raw}]}` 形态；④ 文件头与报错里的重建命令同步订正（铁律 5） |
| `Unity/数据/游戏数据/card_vfx_by_card.json` | 重生成（全量件，含 `_assetRefs` / `soField` / `guid`） |
| `Unity/MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json` | 重生成（运行时件） |

**备份**（⛔ 别删）：`d:/4/_tmp_view/backup_1018/vfx_t14/` —— 两张表换源前的原文。

---

## 四、仍开的部分（如实记，⛔ 不是「已归零」）

1. **4 条语音 GUID 本地确实没有**（唯一剩下的「解不出」）：
   `GOF2`（deathAudio）· `EC57`（attackAudio / deathAudio）· `BL47`（summonAudio）。
   ⇒ 不是解错，是**源包里就没有这件音频**（工具已把它们如实计入「解不出」）。
2. **12 条 `Wait_1s` / `Wait_1,5s` / `Wait_300ms` / `Wait_2s`** —— 原版是**纯计时占位动画**，
   `animInfo.animAdressable` 本来就是空 ⇒ 按原判据**不出 prefab 是对的**，不是缺口。
3. **40 张无逐卡行**：tactic 28 · unit 9 · defence 3（代表：`ASH71` `BL37` `BL49` `DA39` `GSC41` `UM87` …）。
   ⇒ 这些卡在 SO 里**本来就一个 VFX 绑定都没有**（不是我们解不出）；要不要给它们补「通用兜底」是**另一笔账**。
4. **36 张「有行但事件我们引擎不发」**：`ability` 33 条 · `attack_ranged` 13 条 · `death` 2 条
   （tactic 25 · unit 10 · defence 1）。⇒ 属**战斗线 `T21`** 的「引擎不发那个事件」，**不是本件的缺口**。

---

## 五、复现命令

```bash
# ① 重建 guid 表（约 3 分钟，扫 30 个卡相关束）
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_guid_map_from_bundles.py --cards-only

# ② 重出逐卡 VFX 表（默认已指向新真值 + 新表）
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_card_vfx_by_card.py

# ③ 覆盖率复算（P3 §2·1 的脚本；只读 4 个文件）
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 <p3_cov.py>
```

**验收读数**：`TOTAL.reachable = 1051 / 1127`；`runtime cards = 1096 / pairs = 2009`；
`stats.cardsUncovered = 0`；`resolved names NOT in library = 0`。

---

## 六、出处

| 内容 | 位置 |
|---|---|
| 当前 build 的 allcards 解析 | `资料/普查产出_1018/allcards_1350/cards_raw.json` · `报告.md` |
| 新 GUID 表 | `资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` |
| 旧 GUID 表（保留，别删） | `资料/普查产出_第二十七会话/guid_map.tsv` |
| 覆盖率口径（P3 §2·1） | `资料/普查产出_1017/诊断_第四十八会话/P3_动画特效覆盖率.md` |
| 换源前的两张表 | `d:/4/_tmp_view/backup_1018/vfx_t14/` |
