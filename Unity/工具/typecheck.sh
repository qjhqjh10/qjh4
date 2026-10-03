#!/bin/bash
# 秒级 C# 类型检查（改完 .cs 先跑它、再跑 Unity 批处理）—— 2026-09-29 从临时目录搬进仓库
#
# 为什么快：Unity 一次批处理 1–9 分钟，**大半是编译**；这里直接用 Roslyn `csc`
# 拿 Unity 生成的 `.csproj` 当「源清单 + 引用清单」编两遍（运行时程序集 / 编辑器程序集），**几秒**。
# ⚠️ 它**只管编不编得过、不跑任何断言**（断言仍然走 `工具/_run_8_checks.sh` 那 11 条）。
# ⚠️ **批处理跑着的时候别改 `.cs`**（见 CLAUDE.md 铁律 12 / 已知的坑）。
#
# 用法：bash d:/4/Unity/工具/typecheck.sh
# 输出：两行「xxx错误数: N」，**0 = 过**；非 0 时上面会列错误行。
#
# 原理与三个前提（缺一个会报一堆假的 `CS0006 找不到元数据文件`）：
#  ① cwd 必须是 `Unity/MyGame`（csproj 的 HintPath 是相对路径）；
#  ② `dotnet msbuild` 走不通（DOTween 是针对 .NET 4.7.2 编的，本机只有 4.7.1 的引用程序集 ⇒ MSB3274），
#     而 `csc` 不做框架版本检查 ⇒ 直接 `dotnet <sdk>/Roslyn/bincore/csc.dll @rsp`；
#  ③ **新建的 `.cs` 不在 csproj 里**（那是上次 Unity 生成的）⇒ `gen_csc_rsp.py` 会**自动扫** `Assets/`
#     补进去（2026-09-29 改的：原来是一串手写的「新增文件」名单，每加一个文件就得改一次，
#     漏了就报 `CS0246 找不到类型` —— **看着像代码错，其实是源清单没跟上**）。
set -u
cd /d/4/Unity/MyGame || exit 1
TMP="${TMPDIR:-/tmp}"
PY="${PY:-D:/2/Warpforge_tools/py312/python.exe}"
CSC="/c/Program Files/dotnet/sdk/8.0.425/Roslyn/bincore/csc.dll"
# 🔴 2026-10-03：这两个目录**必须先建**。缺 `wfcheck/` 时 csc 连输出都写不出，
#    只报一行**行首**的 `error CS0006`（找不到元数据文件），**根本不解析源码**
#    ⇒ 而下面原来的判据 `grep ' error '`（前后都要空格）**匹配不到行首的 error**
#    ⇒ **编译根本没跑起来，却报「错误数: 0」**。实测：一个少了个 `)` 的语法错被报成 0/0。
#    ⚠️ 换一个新的 `TMPDIR` 时这两层都会命中 —— **新建 TMPDIR 的并发跑尤其危险**。
mkdir -p "$TMP/wfcheck"
"$PY" d:/4/Unity/工具/gen_csc_rsp.py >/dev/null || exit 1
R1="$(cygpath -w "$TMP/wf_csc.rsp")"
R2="$(cygpath -w "$TMP/wf_csc_editor.rsp")"

# 🔴 判据必须是「有 `error CS`」，**不是**「有 ` error `」——
#    后者要求 error 前后都有空格，**行首的 error（CS0006/CS0016/CS2001）一条都匹配不到**。
#    同时盯 csc 的退出码：非 0 却一条 error 都数不到 ⇒ **编译没跑起来**，必须吼出来（别静默报 0）。
check () {                     # $1 = 名字（运行时/编辑器）  $2 = rsp 路径
  local out rc n
  out="$(dotnet "$CSC" "@$2" 2>&1)"; rc=$?
  echo "$out" | grep -E "error CS" | head -20
  n="$(echo "$out" | grep -c 'error CS')"
  echo "$1错误数: $n"
  if [ "$rc" != "0" ] && [ "$n" = "0" ]; then
    echo "🔴 $1：csc 退出码 $rc 却数不到任何 error ⇒ 【编译根本没跑起来】，这个 0 不算数！下面是原始输出尾部："
    echo "$out" | grep -v 'warning CS2002' | tail -6
  fi
  return 0
}
echo "--- 运行时程序集 ---"; check 运行时 "$R1"
echo "--- 编辑器程序集 ---"; check 编辑器 "$R2"
