#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""setup_audio_mixer.py — 把 `Main Mixer.mixer` 补成**原版那套通道**

## 为什么要有这一步

原版的音量三滑块走 `AudioMixer.SetFloat("Volume"+MixerType枚举名, 10*log10(v))`，
而 `SetFloat` **只认 exposed parameter**。工程里原本一个 mixer 都没有，原版那份在
`bundle_audiocontrol_assets_all` 里（native 资产，导不进工程）⇒ 只能自己建。

建 mixer 本身用 `-executeMethod AudioSetup.CreateMixer`（Unity 内部 API
`UnityEditor.Audio.AudioMixerController.CreateMixerControllerAtPath` + `CreateNewGroup`）。
⚠️ 但那个 API **有两个它自己搞不定的地方**，就是这份脚本补的：

  1. **`CreateNewGroup` 建出来的组是「孤儿」** —— 它只把组加进「当前视图」，**不挂到 Master 下**。
     实测生成的 YAML 里 `Master.m_Children: []`、四个组各自 `m_Children: []`。
     不挂上去 ⇒ 组跟 Master 之间没有信号通路，**声音出不来**。
  2. **暴露参数（exposed parameter）在 API 里没法按名字加** ——
     `AudioParameterPath` 只有一个 `GUID parameter` 字段，没有「哪种参数」的信息。

好在 `.mixer` 是**文本 YAML**（实测：Unity 6.3 生成的这份就是 YAML，5.6 KB）。
而**组的音量参数 GUID 就是组自己的 `m_Volume` 字段** ⇒ 直接补 `m_ExposedParameters` 即可。
⚠️ **不是** `m_Effects[0]`（那个 `Attenuation` 效果）的 `m_MixLevel` —— 第一版取错了它，
`AudioMixer.GetFloat("VolumeFX")` 直接返回 false。**别再改回去。**

## 做法
  1. `AudioSetup.CreateMixer` 生成基础文件（组名 FX / Music / Voices / Jingles + Master）
  2. 本脚本：`Master.m_Children` ← 四个组；`m_ExposedParameters` ← 四条 `Volume<组名>`
  3. `-executeMethod AudioSetup.Verify` 复核（读回参数表 + 组树）

用法：
    PYTHONIOENCODING=utf-8 python 工具/setup_audio_mixer.py
"""
import io
import os
import re
import sys

sys.stdout.reconfigure(encoding="utf-8")

MIXER = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Audio/Main Mixer.mixer"
# 基础版（未打补丁）的备份。⚠️ **必须放在 Assets/ 外面** —— 放旁边会被 Unity 当资产导入、
# 还生成 .meta（第一次就是这么干的，Assets 里多出个 `Main Mixer.mixer.orig.meta`）。
BASE = "d:/4/Unity/工具/_audio_mixer_base.yaml"
# 要挂到 Master 下、并按原版名暴露的组（顺序 = 原版 `MixerType` 的 FX/Music/Voices/Jingles）
GROUPS = ["FX", "Music", "Voices", "Jingles"]
# 🆕 2026-10-31（B109_E17）：**暴露参数的顺序照原版**（`Main Mixer.mixer` 的 `m_ExposedParameters`：
# FX / Jingles / Master / Music / Voices）。`Master` 在原版**无调用方**（死参数），但**照建不接**（铁律 18·0）。
# ⚠️ 与 `GROUPS` 分开：`GROUPS` 决定 `Master.m_Children` 的顺序，两者在原版里**本来就不一样**。
EXPOSED = ["FX", "Jingles", "Master", "Music", "Voices"]
# 🆕 2026-10-31：原版 `Main Mixer` 唯一那条快照 `Snapshot` 的组值（dB 的**原文字串**，照抄原版 YAML；
# `m_MixerConstant.snapshots[0].values` 逐格现读 —— pitch 全 1.0，音量 `Master = −0.16685188`、其余四组 0）。
# 顺序 = 原版 YAML 的行序（Master vol / Master pitch / FX vol / FX pitch / …）。
SNAP_DB = [("Master", "-0.16685188"), ("FX", "0"), ("Music", "0"), ("Voices", "0"), ("Jingles", "0")]

_BLOCK = re.compile(r"^--- !u!(\d+) &(-?\d+)\n(\w+):\n((?:  .*\n|\n)*)", re.M)


def blocks(text):
    """→ [(classId, fileID, 类型名, 块正文)]"""
    return [(int(m.group(1)), int(m.group(2)), m.group(3), m.group(4))
            for m in _BLOCK.finditer(text)]


def field(body, name):
    m = re.search(r"^  %s: (.*)$" % re.escape(name), body, re.M)
    return m.group(1).strip() if m else None


def main():
    if not os.path.exists(MIXER):
        print(f"❌ 找不到 {MIXER}\n   先跑：-executeMethod AudioSetup.CreateMixer")
        return 1
    if os.path.exists(BASE):
        src = io.open(BASE, encoding="utf-8", newline="").read()
        print(f"（用备份 {BASE} 重新生成 —— 保证幂等，不叠加）")
    else:
        src = io.open(MIXER, encoding="utf-8", newline="").read()
        io.open(BASE, "w", encoding="utf-8", newline="").write(src)
    nl = "\r\n" if "\r\n" in src else "\n"

    bs = blocks(src)
    groups = {field(b, "m_Name"): fid for cid, fid, tn, b in bs if tn == "AudioMixerGroupController"}
    effects = {fid: b for cid, fid, tn, b in bs if tn == "AudioMixerEffectController"}
    ctrl = [(fid, b) for cid, fid, tn, b in bs if tn == "AudioMixerController"]
    if not ctrl:
        print("❌ YAML 里没有 AudioMixerController 块")
        return 1
    ctrl_fid, ctrl_body = ctrl[0]
    master_fid = int(re.search(r"m_MasterGroup: \{fileID: (-?\d+)\}", ctrl_body).group(1))

    print(f"组：{sorted(groups)}")
    missing = [g for g in GROUPS if g not in groups]
    if missing:
        print(f"❌ 缺组 {missing} —— 先跑 AudioSetup.CreateMixer 把组建齐")
        return 1

    # ---- 每个组的「音量参数 GUID」= 组自己的 `m_Volume` 字段 ----
    # ⚠️ **不是** `m_Effects[0]`（那个 `Attenuation` 效果）的 `m_MixLevel` —— 实测取错时
    #    `AudioMixer.GetFloat("VolumeFX")` 返回 false（Unity 认得的是前者）。这条踩过一次，别再改回去。
    vol = {}
    for g in GROUPS + ["Master"]:
        fid = groups[g]
        grp_body = next(b for cid, f, tn, b in bs if f == fid)
        mix = field(grp_body, "m_Volume")
        if not mix or set(mix) == {"0"}:
            print(f"❌ 组 {g} 读不到 m_Volume")
            return 1
        vol[g] = mix
        print(f"  Volume{g:<8} ← m_Volume {mix}")

    def pitch_of(g):
        """组自己的 `m_Pitch` 参数 GUID（原版快照里每组都带一格 pitch = 1）。"""
        return field(next(b for cid, f, tn, b in bs if f == groups[g]), "m_Pitch")

    # ---- ① Master.m_Children ----
    master_children = "".join(f"{nl}  - {{fileID: {groups[g]}}}" for g in GROUPS)
    out = re.sub(r"(  m_Name: Master\n(?:  .*\n)*?  m_Children: )\[\]",
                 lambda m: m.group(1) + master_children, src)
    if out == src:
        print("⚠️ Master.m_Children 没改到（可能已经是挂好的）")

    # ---- ② m_ExposedParameters ----
    #   ⚠️ 块式（`- guid:` / `  name:`）—— Unity 自己写出来就是这个形状；flow 式（`{guid: …, name: …}`）
    #   也能读，但会和 Unity 的序列化反复对刷 diff。
    exposed = "".join(f"{nl}  - guid: {vol[g]}{nl}    name: Volume{g}" for g in EXPOSED)
    out2 = re.sub(r"  m_ExposedParameters: \[\]",
                  "  m_ExposedParameters:" + exposed, out)
    if out2 == out:
        print("⚠️ m_ExposedParameters 没改到（可能已经写过了）")
    out = out2

    # ---- ④ 🆕 2026-10-31（B109_E17）：对齐原版 `Main Mixer` 的三处 ----
    #   · `m_SuspendThreshold` = **−50**（原版值；我们原来是 −80）
    #   · 暴露参数的第五格 `VolumeMaster`（上面的 EXPOSED 已经带上；原版代码**从不 SetFloat 它**，照建不接）
    #   · 唯一那条快照 `Snapshot` 的 `m_FloatValues`（原版 10 格：5 组音量 + 5 组 pitch）
    #  判据 → `资料/完全复刻/判据/B109_R11_音频mixer.md` §1.4 / §2 第 9、10 项。
    before = out
    out = out.replace("  m_SuspendThreshold: -80" + nl, "  m_SuspendThreshold: -50" + nl)
    if out == before:
        print("⚠️ m_SuspendThreshold 没改到（已经是 −50？）")
    snap_rows = []
    for g, db in SNAP_DB:
        snap_rows.append("    %s: %s" % (vol[g], db))
        snap_rows.append("    %s: 1" % pitch_of(g))
    snap_block = nl.join(["  m_FloatValues:"] + snap_rows)
    out2 = re.sub(r"  m_FloatValues: \{\}", snap_block, out, count=1)
    if out2 == out:
        print("⚠️ 起始快照的 m_FloatValues 没改到（已经不是空的了）")
    out = out2

    # ---- ③ 资产名 = 文件名（原版叫 `Main Mixer`）----
    out = out.replace("  m_Name: MainMixer" + nl, "  m_Name: Main Mixer" + nl)

    io.open(MIXER, "w", encoding="utf-8", newline="").write(out)
    print(f"✅ 已写回 {MIXER}")
    print(f"   Master.m_Children ← {[groups[g] for g in GROUPS]}")
    print(f"   m_ExposedParameters ← {['Volume' + g for g in EXPOSED]}（含原版死参数 VolumeMaster）")
    print("   复核：-executeMethod AudioSetup.Verify")
    return 0


if __name__ == "__main__":
    sys.exit(main())
