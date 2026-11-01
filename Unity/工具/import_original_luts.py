# -*- coding: utf-8 -*-
"""import_original_luts.py — 把 `AnimFXModulePostProcess` 要用到的原版 LUT 贴图导进工程。

**为什么需要**：`WFModulePostProcess`（**52 实例 / 52 效果**）要 `CustomLUT1/2` 那两张
**color-grading LUT**（256×16 展开的 16³ 色带）。它们在原版包里、**工程里一张都没有**
（工程里只有 7 张战场 LUT，那是 `gen_arena_texslots.py` 那条线导的，与本线不通用）。

**名单是数据驱动的**，不手写：从 `数据/游戏数据/animfx_modules.json` 里把所有
`customLUT1` / `customLUT2` 的 `@asset:Texture2D:<名字>` 收上来，再补一张 **`LUT Normal`**
（原版默认 LUT —— `LUTBlender.originalLUTTexture` 那个角色的候选；⚠️ 它具体由谁赋值**原版没查到**，
见 `资料/普查产出_1001/资产导入路三件_侦察.md` §3·6）。

🔴 **为什么要手写 `.meta`**（而不是拷完让 Unity 自己生成）：
LUT 必须 **`sRGBTexture: 0`**（它是**表**不是**色彩**；按 sRGB 采会多做一次转换 ⇒ 颜色整体偏），
而 Unity 默认是 1。工程里现成那几个导入器（`import_original_3dcard.py` 等）**只拷 PNG、
meta 交给 Unity 生成** —— 对它们是对的，对 LUT 不对。
这里**克隆战场 LUT 那份 meta**（它已经被 7 个战场验证过），只换 `guid`。
GUID 按**文件名确定性生成**（md5）⇒ 这个脚本可重跑、**幂等**，不会每次换 guid 把引用打散。

🔴 **`wrapU/V/W = 0`（Repeat）也是本脚本的硬不变量**（2026-11-01 `B-119` 的续账 `W14` 补）
------------------------------------------------------------------------------
**判据（两源一致，14 张 LUT 全部）**：
* `d:/2/tools/assetripper_out/ExportedProject/Assets/Texture2D/LUT*.png.meta` —— 三轴 `wrapU/V/W` 全 `0`；
* `数据/游戏数据/vfx_texture_mips.tsv` 的 `LUT *` 行 —— `wrapU=0 / wrapV=0`（含导出工程里没有的 `LUT Normal`）。
**错因**：本脚本原来**逐字克隆**模板，若模板是 Clamp（`1`）就**整批继承** —— 2026-10-31 查实
14 份产物全是 `1`（模板 `LUT Battle Arena Aeldari.png.meta` 当时正是 Clamp）。
⇒ 现在**不依赖「模板恰好是对的」**：克隆后**显式**把三轴钉成 `0`，并在写盘后**读回自检**（非 0 就报错退出）。
（`.meta` 里 `ti.wrapMode` 一次写三轴，所以只改 U/V 会让 meta 与运行期不一致 —— 三轴一起写。）

用法（🔴 **不带参 = 只打印本用法并退出 0，零读零写**；真写盘必须显式 `--apply`）：
  PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/import_original_luts.py --apply
  … 加 `--check` 只体检（不写文件）
  -h, --help  同「不带参」：只打印用法并退出 0
"""
import hashlib
import io
import json
import os
import re
import struct
import sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
## W14 2026-11-01: 扩成「**不带参** / -h / --help」三种一律只打印用法并退出 0。本文件是**写盘类**
##   （拷 PNG + 写 `.meta`），默认行为绝不能是真干活 —— 先例：`merge_ledger_parts.py --help`
##   不解析 `--help`、不带参就真合并 ⇒ 冲掉 232 行手工修订（CLAUDE.md「会写盘的脚本」那一条）。
##   真写盘必须显式 `--apply`；`--check` 仍是「只读体检」。
USAGE = (
    '用法：\n'
    '  PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" '
    'd:/4/Unity/工具/import_original_luts.py --apply\n'
    '  … 加 `--check` 只体检（不写文件，零写盘）\n'
    '  … 加 `--apply` 真写盘（拷 PNG + 生成/覆盖 .meta）\n'
    '  不带参 / -h / --help  只打印本用法并退出（退出码 0），**不执行任何真活**。'
)
import sys as _wf_help_sys
if (len(_wf_help_sys.argv) <= 1
        or any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv)))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print(USAGE)
    _wf_help_sys.exit(0)

sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"d:/4/Unity"
MODULES = os.path.join(ROOT, "数据/游戏数据/animfx_modules.json")
ASSETS_FULL = r"d:/2/新解包资源/assets_full"
# 🔴 **落在 `Resources/` 下**（不是 `WarpforgeVFX/Textures/`）：`WFModulePostProcess` 给下游的是
#    `customLUT1/2` 的**资产名字符串**（不是引用）⇒ 运行时要能**按名字**取到
#    ⇒ 只有 `Resources.Load<Texture2D>("WarpforgeVFX/LUT/<名>")` 这条路走得通。
#    （战场那 7 张 LUT 不在 Resources 里是因为它们被 `<场>_PostFx.asset` **直接引用**，
#      不需要按名字查 —— 两条线不同，别照抄。）
DEST = os.path.join(ROOT, "MyGame/Assets/Resources/WarpforgeVFX/LUT")
# 模板：战场 LUT 那份 meta（**已被 7 个战场验证过**），只换 guid
META_TEMPLATE = os.path.join(ROOT, "MyGame/Assets/WarpforgeArena1/Textures/LUT",
                             "LUT Battle Arena Aeldari.png.meta")
# 原版默认 LUT（`LUTBlender.originalLUTTexture` 的候选）—— 不在模块字段里，单独补
EXTRA = ["LUT Normal"]
# 期望尺寸：16³ 展开 = 256×16（例外在下面单独报，不静默放过）
WANT_WH = (256, 16)


def png_size(path):
    """读 PNG 的 IHDR，拿宽高（不引 PIL）。"""
    with io.open(path, "rb") as fh:
        head = fh.read(33)
    if len(head) < 33 or head[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    return struct.unpack(">II", head[16:24])


def wanted_names():
    """数据驱动：所有 `customLUT1/2` 引用的贴图名（+ EXTRA）。"""
    doc = json.load(io.open(MODULES, encoding="utf-8"))
    names, by_effect = set(), {}
    for e in doc.get("effects", []):
        for m in e.get("modules", []):
            if m.get("kind") != "AnimFXModulePostProcess":
                continue
            for k, v in zip(m["keys"], m["values"]):
                if k not in ("customLUT1", "customLUT2"):
                    continue
                if not isinstance(v, str) or not v.startswith("@asset:Texture2D:"):
                    continue
                n = v[len("@asset:Texture2D:"):]
                names.add(n)
                by_effect.setdefault(n, []).append(e["name"])
    return sorted(names | set(EXTRA)), by_effect


def find_src(name):
    """在 assets_full 里找这张 PNG（按名字，第一个命中）。"""
    for d in sorted(os.listdir(ASSETS_FULL)):
        p = os.path.join(ASSETS_FULL, d, "Texture2D", name + ".png")
        if os.path.isfile(p):
            return p, d
    return None, None


def guid_for(name):
    """按名字确定性生成 32 位 hex guid（可重跑幂等；命名空间前缀避免与别的生成器撞）。"""
    return hashlib.md5(("warpforge-lut:" + name).encode("utf-8")).hexdigest()


## W14 2026-11-01: 三轴 wrapMode 显式钉成 Repeat(0)。**不依赖模板「恰好是对的」** ——
##   模板是外部文件，随时可能被改回 Clamp（`B-119` 就是这么来的：模板当时是 Clamp，
##   克隆出来 14 份全错）。判据见文件头「`wrapU/V/W = 0` 也是本脚本的硬不变量」一节。
WRAP_KEYS = ("wrapU", "wrapV", "wrapW")


def force_repeat_wrap(text):
    """把 meta 文本里的 `wrapU/V/W: <n>` 全部改成 `0`。返回 (新文本, 命中数)。

    只匹配**顶格/缩进的整行**，其它一个字节都不动（行尾由调用方的 `split('\\n')` 语义保留）。
    """
    hits = [0]

    def _sub(m):
        hits[0] += 1
        return "%swrap%s: 0" % (m.group(1), m.group(2))

    out = re.sub(r"(?m)^([ \t]*)wrap([UVW]):[ \t]*\d+[ \t]*$", _sub, text)
    return out, hits[0]


def meta_wrap_ok(path):
    """读回一份 `.meta`，返回 (是否通过, 说明)。通过 = 存在且三轴全是 0。"""
    if not os.path.isfile(path):
        return False, "文件不存在"
    blob = io.open(path, "rb").read().decode("utf-8", errors="replace")
    bad = []
    for k in WRAP_KEYS:
        m = re.search(r"(?m)^[ \t]*%s:[ \t]*(\d+)[ \t]*$" % k, blob)
        if not m:
            bad.append("%s 缺" % k)
        elif m.group(1) != "0":
            bad.append("%s=%s" % (k, m.group(1)))
    if "sRGBTexture: 0" not in blob:
        bad.append("sRGBTexture 不是 0")
    return (not bad), ("; ".join(bad) if bad else "wrapU/V/W=0")


def main():
    dry = "--check" in sys.argv
    # 🔴 不带显式动作参数 ⇒ 只打印用法并退出（本文件是写盘类；见文件头 USAGE 那一段）。
    #    `-h/--help`/不带参 已在文件顶部早退；这里只处理「带了别的没用参数」的情况。
    if not dry and "--apply" not in sys.argv:
        print(USAGE)
        return 0 if len(sys.argv) <= 1 else 2
    names, by_effect = wanted_names()
    print("要导的 LUT %d 张（%d 张来自模块字段 + %d 张补的）：%s"
          % (len(names), len(names) - len(EXTRA), len(EXTRA), "只体检" if dry else "真写盘"))

    tpl = io.open(META_TEMPLATE, encoding="utf-8", newline="").read()
    if "sRGBTexture: 0" not in tpl:
        print("🔴 模板里没有 `sRGBTexture: 0` —— 停手（按 sRGB 采 LUT 会整体偏色）")
        return 1
    tpl, wrap_hits = force_repeat_wrap(tpl)
    if wrap_hits != len(WRAP_KEYS):
        print("🔴 模板里三轴 wrap 只命中 %d 处（应为 %d）—— 停手（meta 形状变了，别静默克隆）"
              % (wrap_hits, len(WRAP_KEYS)))
        return 1
    if not os.path.isdir(DEST):
        if dry:
            print("  （%s 不存在，--check 不建）" % DEST)
        else:
            os.makedirs(DEST)

    bad, missing = 0, []
    for n in names:
        src, pkg = find_src(n)
        if src is None:
            print("  ✗ **没找到源图**：%s" % n)
            missing.append(n)
            continue
        wh = png_size(src)
        size_kb = os.path.getsize(src) / 1024.0
        flag = ""
        if wh != WANT_WH:
            flag = "  🔴 尺寸不是 %s（16³ 展开应是它）" % (WANT_WH,)
            bad += 1
        uses = len(by_effect.get(n, []))
        dst = os.path.join(DEST, n + ".png")
        state = "已存在" if os.path.isfile(dst) else ("--check" if dry else "写入")
        print("  %-8s %-42s %s×%s %6.1f KB  包=%s  用于 %d 个效果%s"
              % (state, n, wh[0] if wh else "?", wh[1] if wh else "?", size_kb, pkg, uses, flag))
        if dry:
            continue
        with io.open(src, "rb") as fi, io.open(dst, "wb") as fo:
            fo.write(fi.read())
        g = guid_for(n)
        # 换 guid 那一行；其余设置**逐字克隆**模板（三轴 wrap 已在上面显式钉成 0）
        out = []
        for ln in tpl.split("\n"):
            out.append("guid: " + g if ln.startswith("guid: ") else ln)
        # 二进制写：不经过文本模式 ⇒ 行尾由模板原样带过来（CLAUDE.md：别让脚本翻行尾）
        with io.open(dst + ".meta", "wb") as fh:
            fh.write("\n".join(out).encode("utf-8"))
        # 🔴 写盘后**读回自检**：三轴对称 Repeat(0) + `sRGBTexture: 0`。不过就停手（⛔ 不静默）。
        ok, why = meta_wrap_ok(dst + ".meta")
        if not ok:
            print("🔴 %s：刚写出的 `.meta` 自检不过（%s）—— 停手" % (n, why))
            return 1

    print()
    if missing:
        print("🔴 缺 %d 张：%s" % (len(missing), missing))
    if bad:
        print("🔴 %d 张尺寸不符（上面逐条标了）—— **别当没看见**，确认过再决定收不收" % bad)
    if not missing and not bad:
        print("✅ 全部到位（尺寸都是 %s；meta 三轴 wrap = Repeat(0)，写盘后逐份读回自检过）" % (WANT_WH,))
    print("产物目录：%s" % DEST)
    return 0


if __name__ == "__main__":
    sys.exit(main())
