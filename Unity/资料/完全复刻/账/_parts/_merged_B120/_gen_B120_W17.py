# -*- coding: utf-8 -*-
"""生成 `账/_parts/B120_W17.tsv`（执行代理 `B120-W17` · 2026-11-02）。

为什么用脚本生成补丁、而不是手打 5 行 11 列：
  · 这几行的「判据出处 / 判据要点 / 等级」**一个字都不该变**，手打只会引入抄写差异；
  · 只改**落点行号**与**差异/备注**三列 —— 按 (域, 原版类, 方法) 从主表取原行、逐列改、再写回补丁。

⚠️ 本脚本**只读主表、只写 `账/_parts/B120_W17.tsv`**；⛔ 不碰主表（合并由调度台跑
   `merge_ledger_parts.py --only B120_W17`）。补丁读写的列分隔符 = **裸 `\\t`**（消费方就是裸 split）。
"""
import io
import os

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))   # …/资料/完全复刻
MAIN = {
    "battle": os.path.join(ROOT, "账", "战斗逻辑驱动.tsv"),
    "shell": os.path.join(ROOT, "账", "菜单_外壳.tsv"),
}
OUT = os.path.join(ROOT, "账", "_parts", "B120_W17.tsv")

HEADER = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
          "我们的落点", "差异", "等级", "轮次", "备注"]

# (文件, 域, 原版类, 方法关键字, {列下标: 新值 或 (旧, 新) 替换})
# 列：0 域 · 1 原版类 · 2 方法 · 3 判据出处 · 4 判据要点 · 5 状态 · 6 我们的落点
#     7 差异 · 8 等级 · 9 轮次 · 10 备注
JOBS = [
    # ── F18：FIFO 载荷改成「入队时已解好的 clip」 ─────────────────────────────────────
    ("battle", "战斗逻辑驱动", "BattleManager", "PlaySoundAsset(SoundAsset_OLD soundAsset)",
     {
         6: "CardPresentation/Battle/BattleSfx.cs:335（Enqueue）（等价实现 = 原版 `PlaySoundAsset` 的「入队」："
            "`Enqueue` 把 **clip / 音高 / 音量**在入队那一刻定死（`WFSoundBank.PickClip` + cue 区间取一次随机）"
            "再压进 FIFO —— 与原版 `SoundAsset_OLD`（`+0x10 = clip` / `+0x18 = volume`）同形；"
            "出队侧 `:377 DrainOne` 只播载荷里的 `p.clip`、**不再查表**）",
         7: "① 🔴 **2026-11-02（`B120-W17` · F18）已改**：载荷原来只存「原版字段名 + cue」、clip 在**出队那一刻**"
            "才由 `WFSoundPlayer` 随机挑（与原版「入队即持 clip」不同形）⇒ 现在 clip / 音高 / 音量**入队即定死**，"
            "`DrainOne` 不查表（随机分布不变，只是取的时刻提前；`Rng` = 固定种子的 `System.Random`，同 `WFSoundPlayer` 规矩）。"
            "② 原版 `clip != null` 那道闸：我们改成「cue 表查得到」才入队；**clip 挑不出来（`Resources/Art/` 未导入）仍入队**"
            "、出队静音 —— ⛔ 不把「资产没导入」当成「原版没这声」。"
            "③ `LastField` 仍写在**入队**那一刻（既有自检问的是「这一拍请求了哪条 cue」，改到出队会让批处理里全读空）。"
            "④ 还剩一处结构差：原版这条 FIFO 的源 = `BattleManager.audioSource`（**无混音组 ⇒ Master**），"
            "我们走 `WFSoundPlayer.Play(AudioClip…)` ⇒ 挂 **FX 组**；路径今天零生产方（原版唯一生产方 = `AiScripted.cs:2060`，未复刻）"
            "⇒ 当前无可听差异。",
         10: "与上一行同一处改动。🔴 2026-11-02（`B120-W17` · F18）记录变更：载荷 = 入队时已解好的 clip；"
             "自检读口新增 `BattleSfx.FrontClip`（入队后立刻可读，= 原版「入队即持 clip」），"
             "断言 = `Editor/B120_W17_Assert.cs` B 组。",
     }),
    ("battle", "战斗逻辑驱动", "BattleManager", "PlayNextSoundInQueue()",
     {
         6: "CardPresentation/Battle/BattleSfx.cs:377（DrainOne）（等价实现 = 原版 `PlayNextSoundInQueue` 的「出队一口」："
            "`DrainOne` 取一条、过冷却闸、播**入队时已经解好的那条 clip**（不查表）；"
            "泵 = `Tick` + `InstallPump` 的 PlayerLoop 子系统 ⇒ 与 `BattleManager__Update.c:152` **每帧取一条**同形状）",
         7: "**出队节奏**：原版 = **每帧取一条**（`BattleManager__Update.c:152`，≈16 ms 间隔，`PlayOneShot` 之间**仍然叠着响**）"
            "⇒ 我们的默认 `Gap = GapPolicy.OnePerFrame` **就是原版形状**，⛔ 不是我们挑的；"
            "`GapPolicy.SerialByClipLength`（等前一条 clip 播完）是 **🔶 我们自己编的可选演示档**，会拉长演出窗口，⛔ 别当默认。"
            "🔴 2026-10-26 订正：本行原写「默认 = SerialByClipLength、一行切回 OnePerFrame」—— 那是**派活包验收口（我们自己编的）**的残留，已按实读订正。"
            "🆕 2026-11-02（`B120-W17` · F18）：出队侧**不再查表、不再随机挑** —— 原版 `.c:20-67` 只读入队时那个 `SoundAsset_OLD`；"
            "冷却闸（`AudioCue.ConfigAudioSource.c:19-21`，键 = cue 资产）仍在出队这一拍、且排在 `!Enabled` 之外。"
            "另：原版的全局音量 = `GameStaticData.SoundFxVolume`，我们的全局音量在 mixer 组上（`WFSoundPlayer.FxGroup`）。",
     }),
    # ── F20：三根音量滑块 ⇒ `GameStaticData` 镜像同步 ───────────────────────────────
    ("shell", "菜单/外壳", "MediaTab", "SoundFXValueChange(float newValue)",
     {
         6: "Core/WarpforgeAudio.cs:199（SetSoundFx）（等价实现 = `WarpforgeAudio.SetSoundFx`；"
            "接线点 = `Shell/SettingsWindow.cs:5448` + `:5346`、`Battle/SettingsPanel.cs:1245`）",
         7: "同 `MusicValueChange` 那一条：原版全量重写四参、我们只写这两条（FX / Jingles）；另两条入参未变 ⇒ 净结果相同。"
            "🆕 2026-11-02（`B120-W17` · F20）：**镜像已同步** —— 原版 `MediaTab.SoundFXValueChange` 先写 "
            "`GameStaticData.SoundFxVolume`、`SetMusicAndFxVolumes()` 再 read 它去写 mixer（`PlayerDataManager.cs:10438-10439`）；"
            "我们改前只写 mixer（`GameStaticData.SoundFxVolume` 只在 `LocalPlayerData.Apply` 时刷 ⇒ 滑块拖完到下次 Apply 之间是旧值），"
            "现 `SetSoundFx` 末尾同步写镜像。音乐 / 语音两根同形（`MediaTab.cs:131-147`）一并同步。",
     }),
    ("shell", "菜单/外壳", "MediaTab", "MusicValueChange(float newValue)",
     {
         6: "Core/WarpforgeAudio.cs:189（SetMusic）（等价实现 = `WarpforgeAudio.SetMusic`；"
            "接线点 = `Shell/SettingsWindow.cs:5448` + `:5346`、`Battle/SettingsPanel.cs:1244`）",
         7: "原版每改一根音量都把四条 mixer 参数**全重写**一遍；我们只写被改的那一条 —— 另三条的入参就是当前值 ⇒ 幂等、净结果相同。"
            "守卫：原版 `v == 0`、我们 `v <= 0`；所有调用点先 `Clamp01` ⇒ 负值那档到不了。"
            "🆕 2026-11-02（`B120-W17` · F20）：`SetMusic` 现在同步写 `GameStaticData.MusicVolume` 镜像（原版口径 = 静态量是唯一真相，见 `SoundFXValueChange` 那条）。",
     }),
    ("shell", "菜单/外壳", "MediaTab", "VoiceOversValueChange(float newValue)",
     {
         6: "Core/WarpforgeAudio.cs:208（SetVoiceOver）（等价实现 = `WarpforgeAudio.SetVoiceOver`；"
            "接线点 = `Shell/SettingsWindow.cs:5448` + `:5346`、`Battle/SettingsPanel.cs:1246`）",
         7: "同上：原版全量重写四参、我们只写 Voices 一条；净结果相同。"
            "🆕 2026-11-02（`B120-W17` · F20）：`SetVoiceOver` 现在同步写 `GameStaticData.VoiceOversVolume` 镜像。",
     }),
]


def read_rows(path):
    rows = []
    with io.open(path, "r", encoding="utf-8", newline="") as f:
        for line in f:
            line = line.rstrip("\r\n")
            if not line:
                continue
            rows.append(line.split("\t"))
    return rows


def main():
    cache = {}
    out = [HEADER]
    for src_key, dom, cls, method, edits in JOBS:
        if src_key not in cache:
            cache[src_key] = read_rows(MAIN[src_key])
        hit = None
        for r in cache[src_key]:
            if len(r) >= 3 and r[0] == dom and r[1] == cls and r[2] == method:
                hit = r
                break
        if hit is None:
            raise SystemExit("🔴 主表里找不到：%s / %s / %s（%s）" % (dom, cls, method, MAIN[src_key]))
        if len(hit) != len(HEADER):
            raise SystemExit("🔴 原行不是 %d 列（%d 列）：%s / %s" % (len(HEADER), len(hit), cls, method))
        r = list(hit)
        for col, val in edits.items():
            if isinstance(val, tuple):
                old, new = val
                if old not in r[col]:
                    raise SystemExit("🔴 第 %d 列里找不到要替换的片段：%s" % (col, old))
                r[col] = r[col].replace(old, new)
            else:
                r[col] = val
        if any("\t" in c for c in r):
            raise SystemExit("🔴 某个单元格里混进了制表符：%s / %s" % (cls, method))
        out.append(r)

    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        for r in out:
            f.write("\t".join(r) + "\n")
    print("✅ 写了 %d 行（含表头）→ %s" % (len(out), OUT))
    for r in out[1:]:
        print("   ", r[0], "/", r[1], "/", r[2], "→ 落点", r[6][:80])


if __name__ == "__main__":
    main()
