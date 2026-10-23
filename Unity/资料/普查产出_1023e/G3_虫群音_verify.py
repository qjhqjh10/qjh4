# -*- coding: utf-8 -*-
"""G3 自检：模拟 `WFSoundBank.TryGetCue` + `PickClip` 的判据（不跑 Unity）。

判据来源 = `WarpforgeUiFxProbe.cs` D 组（A31 §四·3）：
  `TryGetCue(cue)` 命中 · `PickClip(cue) != null`（= 能 `Resources.Load` 到真 clip）。
Unity 的 `Resources.Load<AudioClip>` 在这里等价于「`Resources/<ClipRoot><clip 名>.<ext>` 文件存在且是可解的 wav/ogg」。
"""
import io, json, struct, sys, os

RES = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources"
SFX = RES + "/Art/audio/sfx"
CUE = "Trait Swarm Tyranid"
SRC_WAV = ("d:/2/新解包资源/assets_full/bundle_soundcollection_assets_all/"
           "AudioClip/Trait Swarm Tyranid.wav")
SRC_CUE_JSON = ("d:/2/新解包资源/assets_full/bundle_soundcollection_assets_all/"
                "MonoBehaviour/Trait Swarm Tyranid.json")

ok = bad = 0
def chk(cond, msg):
    global ok, bad
    if cond: ok += 1; print("G3 ✓ " + msg)
    else:    bad += 1; print("G3 ✗ " + msg)

tbl = json.load(io.open(RES + "/animfx_sounds.json", encoding="utf-8"))
names = [c["name"] for c in tbl["cues"]]
chk(tbl.get("version") == 1, "表 version == 1")
chk(names.count(CUE) == 1, f"cue `{CUE}` 在表里恰好 1 条（实测 {names.count(CUE)}）")

entry = next((c for c in tbl["cues"] if c["name"] == CUE), None)
chk(entry is not None, "TryGetCue 命中")
orig = json.load(io.open(SRC_CUE_JSON, encoding="utf-8"))
if entry:
    chk(entry["clips"] == ["Trait Swarm Tyranid"],
        f"clips == 原版 clipList 解出的那一条（实测 {entry['clips']}）")
    chk(abs(entry["minPitch"] - round(orig["minPitch"], 4)) < 1e-9 and
        abs(entry["maxPitch"] - round(orig["maxPitch"], 4)) < 1e-9,
        f"pitch 区间 == 原版 {orig['minPitch']:.3f}..{orig['maxPitch']:.3f}（实测 {entry['minPitch']}..{entry['maxPitch']}）")
    chk(abs(entry["minVolume"] - orig["minVolume"]) < 1e-9 and
        abs(entry["maxVolume"] - orig["maxVolume"]) < 1e-9, "音量区间 == 原版 1..1")
    chk(abs(entry["timeToPlayAgain"] - round(orig["timeToPlayAgain"], 4)) < 1e-9,
        f"timeToPlayAgain == 原版 {orig['timeToPlayAgain']:.2f}（实测 {entry['timeToPlayAgain']}）")

# PickClip：每条 clip 都要能在 Resources 根下解出文件
for cl in (entry or {}).get("clips", []):
    hits = [f for f in os.listdir(SFX) if os.path.splitext(f)[0] == cl
            and os.path.splitext(f)[1].lower() in (".wav", ".ogg")]
    chk(len(hits) == 1, f"PickClip：`{cl}` 在 `Art/audio/sfx/` 下唯一命中（实测 {hits}）")
    if hits:
        p = os.path.join(SFX, hits[0])
        b = io.open(p, "rb").read()
        chk(b[:4] == b"RIFF" and b[8:12] == b"WAVE", f"`{hits[0]}` 是 RIFF/WAVE")
        i, fmt, data = 12, None, None
        while i + 8 <= len(b):
            cid, sz = b[i:i+4], struct.unpack("<I", b[i+4:i+8])[0]
            if cid == b"fmt ": fmt = struct.unpack("<HHIIHH", b[i+8:i+24])
            if cid == b"data": data = sz
            i += 8 + sz + (sz & 1)
        chk(fmt is not None and fmt[1] == 1 and fmt[5] == 16,
            f"`{hits[0]}` fmt = PCM/16bit（实测 {fmt}）")
        chk(data == 321152 and fmt is not None and abs(data / (fmt[3] or 1) - 3.642) < 0.01,
            f"`{hits[0]}` data {data} 字节 · 时长 {data / (fmt[3] or 1):.3f}s（非空）")

# 不许覆盖：与解包原件逐字节相同
a = io.open(SRC_WAV, "rb").read()
b = io.open(SFX + "/Trait Swarm Tyranid.wav", "rb").read()
chk(a == b, f"工程里那份 wav 与解包原件逐字节相同（{len(a)} B）")

# 撞名：WFSoundBank 把两张表装进同一个字典，重名会静默丢一条（它会告警）
battle = json.load(io.open(RES + "/battle_sounds.json", encoding="utf-8"))
bn = [c["name"] for c in battle["cues"]]
chk(CUE not in bn, "`battle_sounds.json` 里没有同名 cue（无跨表撞名）")

print(f"G3 === {ok} 过 / {bad} 红 ===")
sys.exit(0 if bad == 0 else 1)
