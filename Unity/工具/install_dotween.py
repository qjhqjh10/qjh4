#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""install_dotween.py — 无头安装 DOTween（不依赖 Asset Store / 编辑器 GUI）

为什么要有这个脚本
------------------
DOTween 正常是从 Asset Store 装，但那需要**开编辑器 GUI + 登录账号**，命令行做不了。
本机实测：`codeload.github.com` 通（GitHub 的官网域名不通，见 README），
而 DOTween 的官方仓库 `Demigiant/dotween` 里有**现成的包目录**（DLL + 模块源码 + 编辑器 DLL），
而且仓库里带着 `UnityTests.Unity6000.3` —— 正好是本工程用的 Unity 6，版本对得上。

装完还差两步（Utility Panel 里点的那几下），这里也一并做掉：
  1. **关掉用不上的模块**：模块开关是「默认全开、用 `DOTWEEN_NOxxx` 关掉」。
     本工程 manifest 是瘦的、没装 `com.unity.modules.physics2d`，
     所以 `DOTweenModulePhysics2D.cs` 引用 Rigidbody2D 会编译不过 → 加 `DOTWEEN_NOPHYSICS2D`。
  2. 建 `Assets/Resources/DOTweenSettings.asset`（DOTween 靠它记模块开关和默认缓动）。
     ⚠️ 类型真名是 **`DG.Tweening.Core.DOTweenSettings`**，不是 `DG.Tweening.DOTweenSettings`（踩过）。

用法：
  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/install_dotween.py"
  # --check 只检查装没装、不下载
幂等：已经装好就直接跳过。
"""
import argparse
import io
import os
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile

ROOT = r"d:/4/Unity"
PROJ = os.path.join(ROOT, "MyGame")
PLUGINS = os.path.join(PROJ, "Assets/Plugins/Demigiant")
SETTINGS = os.path.join(PROJ, "Assets/Resources/DOTweenSettings.asset")
PROJSETTINGS = os.path.join(PROJ, "ProjectSettings/ProjectSettings.asset")

# 仓库里的包目录（用 Unity 6 那份，版本对得上）
REPO = "Demigiant/dotween"
BRANCH = "develop"
PKG_IN_REPO = "UnityTests.Unity6000.3/Assets/Plugins/Demigiant"
URL = f"https://codeload.github.com/{REPO}/tar.gz/refs/heads/{BRANCH}"

DEFINE = "DOTWEEN_NOPHYSICS2D"


def installed():
    return os.path.exists(os.path.join(PLUGINS, "DOTween/DOTween.dll"))


def download(dest):
    print(f"下载 {URL}")
    print("（codeload 通但慢，约 28 MB / 3 分钟，别当成卡死）")
    r = subprocess.run(["curl", "-sS", "-m", "600", "-L", "-o", dest, URL])
    if r.returncode != 0 or not os.path.exists(dest):
        print("❌ 下载失败 —— 改用 Asset Store：Window > Asset Store 搜 DOTween（免费版），"
              "装完跑 Tools > Demigiant > DOTween Utility Panel > Setup")
        return False
    print(f"  下载完成：{os.path.getsize(dest) / 1048576:.1f} MB")
    return True


def extract(tar_path, workdir):
    print("解压…")
    with tarfile.open(tar_path) as tf:
        members = [m for m in tf.getmembers() if f"/{PKG_IN_REPO}/" in m.name or m.name.endswith("/LICENSE")]
        tf.extractall(workdir, members=members)
    # 找到解出来的 Demigiant 目录
    for root, dirs, _ in os.walk(workdir):
        if root.endswith(PKG_IN_REPO):
            return root, os.path.join(workdir, os.path.dirname(PKG_IN_REPO).split("/")[0])
    return None, None


def copy_in(src_demigiant):
    print(f"复制到 {PLUGINS}")
    os.makedirs(os.path.dirname(PLUGINS), exist_ok=True)
    if os.path.exists(PLUGINS):
        shutil.rmtree(PLUGINS)
    shutil.copytree(src_demigiant, PLUGINS)
    # 调试符号（.mdb）没用，删掉免得占地方
    for root, _, files in os.walk(PLUGINS):
        for f in files:
            if f.endswith(".mdb") or f.endswith(".mdb.meta"):
                os.remove(os.path.join(root, f))
    n = sum(len(fs) for _, _, fs in os.walk(PLUGINS))
    print(f"  {n} 个文件")


def set_define():
    """把 DOTWEEN_NOPHYSICS2D 写进 ProjectSettings 的 Standalone define。
    ⚠️ 必须直接改文件 —— 工程编译不过时 Unity 根本不会执行 -executeMethod（先有鸡还是先有蛋）。"""
    t = io.open(PROJSETTINGS, encoding="utf-8").read()
    if DEFINE in t:
        print(f"  define 已有 {DEFINE}")
        return
    if "  scriptingDefineSymbols: {}" in t:
        t = t.replace("  scriptingDefineSymbols: {}", f"  scriptingDefineSymbols:\n    Standalone: {DEFINE}", 1)
    else:
        m = re.search(r"(  scriptingDefineSymbols:\n(?:    \w+: .*\n)+)", t)
        if not m:
            print("  ⚠️ 认不出 define 段，手动加：Project Settings > Player > Scripting Define Symbols")
            return
        t = t[:m.end(1)] + f"    Standalone: {DEFINE}\n" + t[m.end(1):]
    io.open(PROJSETTINGS, "w", encoding="utf-8").write(t)
    print(f"  已写入 define: {DEFINE}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true")
    a = ap.parse_args()

    if installed():
        print(f"✅ 已装：{PLUGINS}")
        print(f"   设置资产：{'有' if os.path.exists(SETTINGS) else '没有'}")
        return 0
    if a.check:
        print("❌ 没装")
        return 1

    with tempfile.TemporaryDirectory() as wd:
        tar = os.path.join(wd, "dotween.tar.gz")
        if not download(tar):
            return 1
        src, _ = extract(tar, wd)
        if not src:
            print("❌ 包里没找到 " + PKG_IN_REPO)
            return 1
        copy_in(src)

    set_define()
    print()
    print("装完了。接下来让 Unity 编译一次：")
    print('  unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \\')
    print(f'    -batchmode -quit -projectPath "{PROJ}" -executeMethod DOTweenSetup.Run -logFile -')
    print("  （DOTweenSetup.Run 会建 DOTweenSettings.asset，等价于 Utility Panel 里的 Setup）")
    print("  再验一遍：-executeMethod DOTweenSmokeTest.Run")
    return 0


if __name__ == "__main__":
    sys.exit(main())
