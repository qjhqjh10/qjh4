# F5 —— 补导轮抽进度条描边图 `UI_Draft_event_result_bar_holder`

> 一句话：**这不是缺代码，是缺一张图**。`DraftModeWindow` 早就在按 `UI_Draft_event_result_bar_holder`
> 这个名字找图，只是这张图**从来没被导入过** ⇒ `MissingArt` 里挂着它 ⇒ `DraftWindowAssertD2` 那条
> `miss.Count == 0` 翻红。本次把图补进 `Resources/Art/ui_menu/`，并把名字登记进导入器，防下次重导又漏。

## §一 源图在哪

| 项 | 值 |
|---|---|
| 路径（备查库） | `d:/4/Unity/MyGame/Assets/CardPresentation/Art/原版/0_mainmenu/UI_Draft_event_result_bar_holder.png` |
| 尺寸 | **633 × 282**（PNG IHDR 现读；与 `Sprite/*.json` 的 `m_Rect` 完全一致） |
| 字节数 | `190927` |
| md5 | **`f31501796ca11e2a530dbfd2050258df`** |

**切片缓存（导入器的真正源）里也在，5 个 bundle 各一份，逐字节全同**（md5 全 = `f31501796c…`）：

```
ui_extract/atlasindividual_assets_0_mainmenu/Sprite/UI_Draft event result bar holder.png
ui_extract/liveopsicons_assets_all_sprites/Sprite/UI_Draft event result bar holder.png
ui_extract/menus_assets_all_sprites/Sprite/UI_Draft event result bar holder.png
ui_extract/scenes_scenes_mainmenuwarpforge_sprites/Sprite/UI_Draft event result bar holder.png
ui_extract/staticgeneralassets_assets_all_sprites/Sprite/UI_Draft event result bar holder.png
```

⇒ **没有「同名不同图」那个坑**，取哪一份都对。取 `atlasindividual_assets_0_mainmenu`，与同族条目
（`40K_Info_button_draft menu` / `40K_Cards_button_draft menu`）一致。

判据 JSON（`assets_full` 现读）：

```
bundle_atlasindividual_assets_0_mainmenu/Sprite/UI_Draft event result bar holder.json
  m_Rect              = {x:0, y:0, width:633, height:282}
  m_RD.textureRectOffset = {x:0, y:0}
  m_PixelsToUnits     = 100 · m_Pivot = (0.5,0.5) · m_Border = (0,0,0,0)
```

（`m_Rect` == PNG 实际尺寸、offset 0 ⇒ 导入器的 `copy_ui_sprite` 会走 **`'asis'` 逐字节拷**，
不会补画布、不会缩放。）

## §二 落盘到哪

| 项 | 值 |
|---|---|
| 路径 | `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu/UI_Draft_event_result_bar_holder.png` |
| 字节数 | `190927` |
| md5 | **`f31501796ca11e2a530dbfd2050258df`**（**与源图逐字节相同**） |

- 落盘名 = **下划线形式**，与目录现有惯例一致（`ls` 现读：`40K_Chat_icon_Alliance_v2.png` ·
  `40K_Cards_button_draft_menu.png` · `WF_Lock_Icon_Simple.png` 等一律「空格→下划线」，⛔ 无空格）。
- **内容一个字节没改**：直接二进制拷贝，不缩放、不转格式、不重编码。
- 同时补了一份 `.meta`（`UI_Draft_event_result_bar_holder.png.meta`）：**设置照抄这张图自己的原版 `.meta`**
  （`enableMipMap 1` · `maxTextureSize 2048` · `textureType 0` · `ppu 100` · `pivot 0.5` · `wrap clamp`），
  **只换了一个新 guid** `f2008d3135704edd923916916c2143c5`（uuid4）——
  ⛔ 不能照抄原 guid，否则与 `Art/原版/` 那份撞 guid（Unity 报重复 GUID）。
  - `.meta` 是 LF 行尾（117 行），与 `ui_menu/` 所有既有 `.meta` 一致（现读三份对比：全 LF、0 裸 CR）。
  - 新 guid 全工程唯一（`grep -rl` 在 `Assets/` 只命中它自己）。
- ⚠️ `Resources/Art/**` 整目录在 `.gitignore`（`.gitignore:93`，构建产物）⇒ 这张图**不进 git**，
  靠导入器重建 —— 这正是「必须把它登记进 `MENU_IMAGES`」的原因。

## §三 导入器补了什么

`d:/4/Unity/工具/import_original_art.py` 的 `MENU_IMAGES` 末尾（`40K_profile_campaign_bg_SW` 之后、
列表收尾 `]` 之前）新增一条：

```python
('UI_Draft event result bar holder',      'atlasindividual_assets_0_mainmenu'),  # 轮抽进度条描边（633×282）
```

并在它上方写了注释块，写清：判据 JSON 的 `m_Rect` / `offset` / `ppu`、为什么必须登记（哪一行代码要它、
哪条断言因此翻红）、5 份源 md5 全同、落盘名按下划线惯例。

**只读验证**（`--check` 不写盘，代码里 `if not args.check:` 挡着全部写动作）：

```
cd d:/4/Unity/工具 && python -I -X utf8 import_original_art.py --check --only-menu
  → 菜单 UI 图：源 292 张（来自 14 个 bundle 目录）
  → 检查完成：785 / 785          ← 零 `缺:`（新条目解析到了源）
  → 补画布清单里**没有**这张（正确：`m_Rect` == 原尺寸 ⇒ `'asis'`，逐字节）
  → 也没有 `noinfo` 告警（判据取到了）
```

## §四 为什么这样就够（`CardArt.MenuUi` 的查找路径）

`Core/CardArt.cs:1064` 的 `Get(path)` = `Resources.Load<Texture2D>(path)`，
`Root` = `"Art/"`（`Core/CardArt.cs:18`），`MenuUi(name)` 依次试三处（`Core/CardArt.cs:890-901`）：

```
Art/ui_menu/<name>   ← 本次落盘在这里（第一优先）
Art/ui_deck/<name>
Art/ui/<name>
```

**`MenuUi` 不做「空格→下划线」归一**（那个换名发生在**导入器**里：`name.replace(' ', '_')`），
所以**落盘名必须就是代码传的名字**：`DraftModeWindow.cs:4189` 传的是
`Tex("UI_Draft_event_result_bar_holder")` ⇒ 落盘 `UI_Draft_event_result_bar_holder.png` **逐字对上**。

图到位后：
- `DraftModeWindow` 那层 `Outline` 走 `outlineTex != null` 的**真图分支**（不再用实心兜底色
  `(0.28,0.20,0.10,1)`）；
- `Editor/DraftWindowAssertD2.cs:414` 的 `Ok(miss.Count == 0, …)` 应该回空
  （红时日志 `d:/4/_tmp_view/menu.log:163962` 实得 `[UI_Draft_event_result_bar_holder]`，只有这一项）。

⚠️ 一个会话内的边界：`CardArt.Get` **把 null 也缓存**（`CardArt.cs:1065`），所以
「运行中才把图放进去」在同一进程里不会生效 —— 本次是先落盘再跑的，不受影响。
⚠️ **要绿还得等主对话跑一次自检**（本代理按红线不跑 Unity）。

## §五 顺手更正的过期注释（唯一一处 `.cs` 改动，注释行内原地改，行数不变）

`Shell/DraftModeWindow.cs:4188` 原写「**该图未导入** ⇒ 实心兜底 + 出声一次（不静默）」——
现在这句已经是假的（铁律 5：记录错了就地改），改成为：

```
// Outline（sprite `UI_Draft event result bar holder` 633×282）—— 该图 2026-10-28 已补导进 `Resources/Art/ui_menu/`（判据同 `MENU_IMAGES`）；图缺失时仍走实心兜底 + 出声（不静默）
```

- **取图逻辑一个字没动**（4189 行 `Tex(...)` 与下面的 `if (outlineTex != null)` 分支原样）。
- **零行数变化**：文件 4360 行 → 4360 行（LF 4360 · 裸 CR 0）；字节 `328132 → 328211`（+79 = 注释加长的字数）。
- 理由：这条注释会把下一个会话引到「图还没导」的旧结论上，重新查一遍。

## §六 还欠什么

1. **自检要复跑**（本代理不跑 Unity）：`DraftWindowAssertD2` 宿主那一条，
   期望 `⑦ MissingArt 是空的` 转 ✓。日志里那一项的 `实得 […]` 应为 `[]`。
2. **别的引用点：全工程只有一处代码引用**（`grep -rn "UI_Draft_event_result_bar_holder|UI_Draft event result bar holder"`
   在 `Assets/` + `工具/` 现读）：
   - `Shell/DraftModeWindow.cs:4189`（本次服务的调用点）；
   - `Art/原版/0_mainmenu/_atlas_rects.json:338`（图集矩形清单，数据文件，不是取图点）；
   - `工具/_同步日志_assets_small.json`（同步日志，历史产物）。
   ⇒ **没有第二处取图点**，不需要额外接图。
3. **`Art/原版/` 那份是备查库、不是运行时**（本次没动它，也没删它）。

## §七 账补丁：`账/_parts/B90_F5.tsv`（1 行 · 11 列 · 首行逐字表头）

账里**确实有一条**记着「图未导入」，在**差异列（第 8 列）**，故按简报写了个补丁：

- 命中 = `账/轮抽.tsv:58`，键 `DraftModeDeckInfoPanel . ToggleAllianceInfo(bool value)`。
- 改动 = 差异列里
  「`UI_Draft_event_result_bar_holder` 图未导入 ⇒ 实心兜底+出声」
  → 「`UI_Draft_event_result_bar_holder` **图已于 2026-10-28 补导进 `Resources/Art/ui_menu/`
  （导入器 `MENU_IMAGES` 已登记；落盘 md5 `f31501796ca11e2a530dbfd2050258df` = 源图）** ⇒ 走真图分支」。
- 该行**其余 10 列逐字照抄主表现状**（含 `B90-N6` 已并进去的那段）；**状态不变（B）**、等级 `P2`、轮次 `B90`；
  方法列是完整签名 `ToggleAllianceInfo(bool value)`；`轮次`（第 10 列）= `B90`；11 列校验通过、表头与主表逐字相同。
- 🔴 **合并顺序有约束（请调度台排定，我不动主表）**：`工具/check_patch_conflicts.py` 现读报
  这个键在 **`B90_F5.tsv:2`** 与 **`B90_N6.tsv:3`** 重复，且**字典序会选 `B90_N6` 赢**
  ⇒ 若用 `--only B90_` 一把合，本次改写会被 N6 的旧文案**静默盖回去**。
  ⇒ **必须先合 `--only B90_N6.tsv`、再合 `--only B90_F5.tsv`**（或先把 N6 归档，再合 F5）。
  注：N6 那一行的其余内容与主表现状**完全相同**（已并过），所以「F5 最后合」是无损的。

## §八 红线自查

- ⛔ 未跑 Unity（只有 `--check` 的**只读**导入器检查，不写盘；秒级 typecheck 已跑）。
- ⛔ 未动 git。
- ⛔ 未动 `d:/2`（只读源）。
- ⛔ 未改账主表 / `CLAUDE.md` / `项目任务.md`；未碰 F1–F4 的任何文件。
- ✅ `TMPDIR=/tmp/wf_B90F5 bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**。
