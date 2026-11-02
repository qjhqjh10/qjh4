# B121_AUD 执行报告 · 音频四条收口

> 执行写手 `B121-AUD` · **2026-11-03** · 白名单内改动：`工具/import_battle_sfx.py` ·
> `CardPresentation/Resources/battle_sounds.json` · `Shell/BaseEventPopup.cs` · `Battle/BattleDoors.cs` ·
> `Deck/DeckRuntimePartial_B121WC.cs` · 新建 `Editor/B121_AUD_Assert.cs` · 本报告 · `账/_parts/B121_AUD.tsv`。
> ⛔ 未跑 Unity 宿主 · ⛔ 未动 git · ⛔ 未动账主表 / 活状态表 / 正本 / `WFSoundBank.cs` / 其他音效表。

## §一 逐条

| ID | 改了什么 | 判据 | 落点 | 验证 | 还差什么 |
|---|---|---|---|---|---|
| **B121-AUD-03** | 生成器加**手写组** `HAND_WRITTEN`（7 条）：只钉「谁指向哪条 cue」，clips / 音量 / 音高 / ttp **仍当场从 shipped cue 束重解**；`--check` 对 C 组另加「按 stem 查 Resources 在位」；**裸跑行为一个字没改** | 引用者不在 `SoundAssetCollection` 的 28 字段里（场景序列化字段 / 活动 SO 的 `EventAssets[key=8]`）⇒ 无自动取材口。判据全文 → `报告/B121_D2_音效cue覆盖.md` §三·A / §四 | `工具/import_battle_sfx.py:330`（`HAND_WRITTEN`）· `Resources/battle_sounds.json`（**33 → 34 条**：16 collection + 11 card + 5 scene + 2 event） | 真跑 `python 工具/import_battle_sfx.py`（重写表）→ 再 `--check` ⇒ **✅ 产物齐全**（音频 29 个 + C 组手写 clip 7 个按 stem 在位）；6 条既有条目**逐字段与改前一致** | 无 |
| **B121-AUD-04** | `BaseEventPopup.Open()` 补第 ⑤ 步：事件数据 `EventAssets[key=8]` → GUID 表 → `WindowSounds.PlayJingles`（原版 `MixerType.Jingles`）；解不出**出声 + 不播**；`HasAsset` 为假静默不播（照原版） | cpp2il `BaseEventPopup.cs:55-63` · `decomp_full/BaseEventPopup__Open.c` 末段（`HasAsset(…,8)` → `LoadAsset<AudioCue>` → `AudioCue__Play2D(…,3)`）；2 份活动 SO 的 key=8 GUID 实测可解（Orks1 / Space Wolves） | `Shell/BaseEventPopup.cs:627`（`PlayOpenSoundForTest` = 与 `Open()` 同一条实现）· 调用点 `:293`（同物理行追加，行数零变化）· 表 `OpenSoundCueByGuid`（`:605`） | `typecheck 0/0`；断言 B1–B4 | 无（Emperors Children 那条 GUID 本地**悬挂**，原版也解不出 ⇒ 只出声，⛔ 不顶别的 cue） |
| **B121-AUD-05** | 开门音效**改走 cue**：`CueName` 值由 clip 名改成 **cue 资产名**（`Victory`/`Defeat`/`Draw`），播放改 `WindowSounds.PlayJingles`（0.7 音量 + ttp 0.1 由 cue 给）；`HasCue` 判据改成「cue 表里有没有」 | `decomp_full/EndBattleDoors__SetupDoor.c:113`（第 4 实参 `3` = Jingles）；13 场 arena 的 `victoryCue`/`defeatAudioCue`/`drawAudioCue` → pid → cue（三条包装 min/maxVolume **0.7**） | `Battle/BattleDoors.cs:472`（`PlayCue`）· `:458`（`HasCue`）· `:60`（`CueName`）· 类尾 `CueAssetOf` | `typecheck 0/0`；断言 C1–C4 | 无（`_sfx` 专属音源已按「零行数变化」改成留档注释，见 §四·2） |
| **B121-COLL-11** | Done 钮那一声**cue 名解出来了** = **`Deck Editing Valid Deck`**（clip `Progress Jingle` · vol 1.0 · ttp 0.1），在「不合法 → 合法」那一拍播（`WindowSounds.PlayFx` = 原版 `MixerType.FX`）；另留 `UpdateDoneButtonSound(valid)` 供精确接线 | il2csharp `DeckEditingWindow.cs:558-589` `UpdateDoneButton`；cue 链 = `Deck Editing Menu` 根 MB `MonoBehaviour_5918047927837519652.json` 的 `doneButtonEnabled` = PPtr(fileID 6, pid `-8540943293993850510`) → externals[5] = `CAB-62d1945b…`（soundcollection） | `Deck/DeckRuntimePartial_B121WC.cs:194`（`PlayDoneButtonEnabledCue`）· `:203`（`UpdateDoneButtonSound`）· `:93`（`PunchDoneButton` 里那句） | `typecheck 0/0`；断言 D1–D4 | 🟠 **两小项要动 `DeckRuntime.cs`**（不在白名单）：① 原版**每次合法刷新都播**（不只变合法那一拍）② 首帧 `Toggle(valid, 0)` 硬切 ⇒ 补丁见 §三。`Toggle(valid, 0.3f)` 的 **0.3 s 本身就对**（见 §二·3） |

## §二 关键发现（都改变了施工内容）

1. 🔴 **`Draw` 那条 cue 是 D2 漏的**：`EndBattleDoors` 的**同一颗 MB**里 `drawAudioCue`(+0xC0) = pid `-2870999988157826586`
   → cue **`Draw`** → clip `MatchDraw`（vol **0.7**）。⇒ 表里 6 条变 **7 条**、总数 33 → **34**。
   我们原来三档都按 vol 1.0 直放 ⇒ **平局那一档也响 1.43 倍**。
   （⚠️ `animfx_sounds.json` 里本来就有一条手写的 `Draw`（参数逐值相同，见 §五·6），但它的**生成器产不出它**
   ⇒ 靠手写的东西迟早丢；本批把它放进可重生成的 `battle_sounds.json`，两份参数一致时 `WFSoundBank` 撞名只警告一次。）
2. 🔴 **Emperors Children 的 `OpenSound` 是悬挂引用**：`Release Emperors Children Parent Event` 也带 key=8
   （GUID `ba6b95f76b1003141b098fc85bed0a3b`），但该 GUID 在**任何 catalog / 任何束的容器键里 0 命中**
   ⇒ 原版本地也解不出。**⛔ 不拿 `EC Expansion 1 jingle` 顶**（那是另一条资产；D2 把它归进「零引用」是对的）。
3. ✅ **`Toggle(valid, 0.3)` 的 0.3 一秒都不用补**：`UIEffectTogglerController.Toggle(bool, float duration = **0.3f**, …)`
   —— 默认实参就是原版那个 0.3；而 `FootShine`（扫光层）**不该**吃这个时长（原版那颗的 `DoUpdate` 是空桩，
   真正动的是 tweener 自己的 1.0 s + Loop）。断言 D3 直接钉那个默认值。
4. ✅ **Done 钮那条 cue 本来就在 `animfx_sounds.json`**（`import_original_sfx.py` 的 `EXTRA_CUES` 收的，
   参数与我从束里解出来的逐值相同）⇒ **不需要改音效表**，只差接线。
5. ⚠️ **`OvertimeStart` 的扩展名不一致**：解包目录是 `.vorbis`、我们 `Resources/` 里是 `.wav` ⇒ 手写组的 clip
   **不参与「从解包目录拷音频」**（否则会出现**同名两扩展名**，`Resources.Load` 挑哪个不可控）。`--check` 改为按 **stem** 验在位。

## §三 补丁（两处，都不在本写手白名单 ⇒ 交回）

**补丁 1 · `Assets/CardPresentation/Editor/B117_G10_12_Assert.cs:246-248`（D2 建的断言，必须跟着改）**

```csharp
        Check(tbl != null && tbl.cues != null && tbl.cues.Length == 34,
              $"C1 战斗 cue 表 = **34 条**（16 `collection` + 11 `card` + 5 `scene` + 2 `event`；实得 {(tbl != null && tbl.cues != null ? tbl.cues.Length : -1)}）"
            + "｜🧨 有人把 12 条静音 cue 也塞进表 ⇒ 条数变多 ⇒ 本条红；重跑 `import_battle_sfx.py` 丢掉 scene/event 那 7 条 ⇒ 条数变少 ⇒ 本条也红");
```

（只改数字与文案里的 33→34、4 scene→5 scene、6 条→7 条；`C1b` 的 6 个名字**不用动**——`Draw` 由本批
`B121_AUD_Assert` C1 钉住。⚠️ 不改 ⇒ 下次跑 `BattleScene.Run` 会红一条。）

**补丁 2 · `Assets/CardPresentation/Deck/DeckRuntime.cs`（精确对齐 `UpdateDoneButton`，可选、优先级 P2）**

```csharp
// ① :4869 同一物理行末尾追加一句（行数零变化）：
//    … _footDoneValidLast = err == DeckError.None;  →  末尾再加  UpdateDoneButtonSound(err == DeckError.None);
//    并把 PunchDoneButton 里那句 PlayDoneButtonEnabledCue() 删掉（否则同帧两声，靠 ttp 0.1 也只是挡住第二声）
// ② ToggleDoneHighlight 拆一个带时长的重载（首帧硬切）：
        void ToggleDoneHighlight(bool on) { ToggleDoneHighlight(on, 0.3f); }
        void ToggleDoneHighlight(bool on, float duration) { /* 原体，末尾 _doneHlCtl.Toggle(on, duration); */ }
// ③ :4869 那句调用改成： ToggleDoneHighlight(err == DeckError.None, _doneHlCtlFirst ? 0f : 0.3f); （_doneHlCtlFirst 用完置 false）
```

## §四 验证

1. `TMPDIR=/tmp/wf_aud bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（含新建的 `Editor/B121_AUD_Assert.cs` 的 A0–A7/B/C/D 全部断言）。
   ⚠️ 期间多次红**全部落在别人的文件**（`RuleEngine/Core/EnchantmentExecutor_B115.cs` · `ChoiceOptionCards_B116W32.cs` ·
   `RuleEngine/Core/EffectResolver.cs` · `Editor/B121_COLL16_Assert.cs` · `Editor/B121_D4_Assert.cs` ·
   `WarpforgeArena1/Editor/ArenaBuilder.cs`）—— 并发写手的中间态；本批自己的文件**一次都没红过**，复跑即 0/0。
2. `python -I -X utf8 工具/check_ledger_points.py` ⇒ **合计 A 5876 · 漂移 0 · 失败 0**（终跑；输出里
   `BattleDoors` / `BaseEventPopup` / `DeckRuntimePartial` / `battle_sounds` **0 行命中**）。
   ⚠️ 中途两次中间态（漂移 77 / 28）**全部 100% 落在 `Shell/CollectionWindow.cs`**（并发写手 COLL16 正在重写该文件），
   等它交回后复跑即归零 ⇒ ⛔ 与本批无关，也 ⛔ 没跑 `--fix`。
3. 生成器：真跑一次（写表）→ `--check` ⇒ **✅ 产物齐全**；6 条既有条目逐字段未变，新增 `Draw`。
4. 行数守恒：`BattleDoors.cs` 的账落点 `:352` / `:434` 编辑后仍落在原声明上（逐行 awk 核过）；
   `BaseEventPopup.cs` 只在 `:293` 同一物理行追加、其余全加在类尾（`:590` 之后）。
5. 断言（**未接线**，按所有权表由调度台接）：`B121_AUD_Assert.Run()` → A0–A7 / B1–B4 / C1–C4 / D1–D4，
   三对「灭自证」见文件头。

## §五 顺手发现（只记录，没改）

1. 🔴 **`WFSoundBank.cs:55` 的 doc 已过期**（不在白名单）：写「`import_battle_sfx.py` **只产出 `collection`/`card` 两组**、
   那 6 条不在取材面」——现读是 **7 条、四组、生成器自己产出**。建议改成「四组；③④ 见 `HAND_WRITTEN`」。
2. 🟠 **`BattleDoors.SetupDoor` 的 cue 位置和原版不同**：原版 `Play2D` 在**选片之后、建 RT 之前**无条件播；
   我们的 `PlayCue(r)` 在 `Resources.Load<VideoClip>` **取不到就 return 0f** 之后 ⇒ **没有视频资产时连音效也不播**
   （`Art/` 是 gitignore，删掉就静默少一声）。修它要把 `PlayCue` 提到取片之前 —— 那段夹在账落点 `:434` 附近，本批没动。
3. 🟡 `Editor/BattleScene.cs` 里那条断言文案还写着「`Art/audio/sfx/Match{r1}.ogg` 加载得到」—— 现在判据是
   「cue 表里有没有这条 cue」，文案该跟着改（`Editor/*Scene.cs` ⛔ 不在白名单）。
4. 🟡 `B121_D2_Assert.cs` 的 `AddedCues` 只有 6 个名字（不含 `Draw`）——它不断总数所以**不会红**，但漏了 `Draw`；
   本批的 `B121_AUD_Assert` C1 补上了。
5. ℹ️ `Release Emperors Children Parent Event` 的 OpenSound 悬挂 ⇒ 若将来远端包解出来，**补一行 `OpenSoundCueByGuid`** 即可。
6. ⚠️ **`Draw` 在 `animfx_sounds.json` 里还有一条同名的旧手写副本**：现读 `import_original_sfx.py` 的取材面
   （`animfx_modules.json` 的 `sounds`/`exitSounds`/`collisionEvent`）**产不出它**（`"Draw"` / `MatchDraw` /
   该 cue 的 pathID 在模块数据里 **0 命中**）⇒ 那条是**历史手写**，重生成 `animfx_sounds.json` 会掉。
   两条参数**逐值相同**（clip `MatchDraw` · vol 0.7 · ttp 0.1）⇒ 行为无差别；`WFSoundBank` 撞名时**保留先装的**
   （animfx 那条）并警告一次。本批把 `Draw` 放进 `battle_sounds.json`（生成器产出 ⇒ **可重生成**）就是为了不让它再靠手写。
   （建议：下次重生成 `animfx_sounds.json` 时顺手确认那条旧副本已消失，警告自然没了。）

## §六 账补丁的合并顺序（同键冲突，⚠️ 必须先 D2 后本批）

`工具/check_patch_conflicts.py` 现读：`BaseEventPopup . Open()` 在 `B121_AUD.tsv`(A) 与 `B121_D2.tsv`(B) 同键，
**字典序会让 D2 赢**（`B121_AUD` < `B121_D2`）。
⇒ 合并顺序 **先 `--only B121_D2.tsv`、后 `--only B121_AUD.tsv`**；否则主表会留下 D2 那条 B +「第 ⑤ 步未实现」。
