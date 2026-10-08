#!/bin/bash
# 秒级 C# 类型检查（改完 .cs 先跑它、再跑 Unity 批处理）—— 2026-09-29 从临时目录搬进仓库
#
# 为什么快：Unity 一次批处理 1–9 分钟，**大半是编译**；这里直接用 Roslyn `csc`
# 拿 Unity 生成的 `.csproj` 当「源清单 + 引用清单」编两遍（运行时程序集 / 编辑器程序集），**几秒**。
# ⚠️ 它**只管编不编得过、不跑任何断言**（断言仍然走 `工具/_run_8_checks.sh` 那 11 条）。
# ⚠️ **批处理跑着的时候别改 `.cs`**（见 CLAUDE.md 铁律 12 / 已知的坑）。
#
# 用法：bash d:/4/Unity/工具/typecheck.sh
#       WF_DOC=1 bash d:/4/Unity/工具/typecheck.sh     # ← 🆕 可选「XML doc 档」（A453）：默认【关】
# 输出：两行「xxx错误数: N」，**0 = 过**；非 0 时上面会列错误行。
#       WF_DOC=1 时**再多两行**「xxx XML doc 警告数: M」（M>0 时上面列前 20 条 `warning CS15xx`）。
#
# 🆕 WF_DOC=1（可选档，2026-10-12 A453）—— 为什么非有它不可（两层原因，H13/H16 查实）：
#   ① 生成的 rsp 里**没有 `-doc:`** ⇒ 编译器**根本不解析 XML doc** ⇒ CS1570 连产生都不产生；
#   ② 这个脚本原来只 `grep "error CS"`，而这一族（CS1570/1571/1572/1573/1587 …）全是 **warning**。
#   ⇒ 不开这一档，「XML doc 写坏了」在本工程**没有任何可观测点**（清完也会悄悄长回来）。
#   ⚠️ **不额外多一次编译**：只是给同一次 csc 多传一个 `-doc:`，产物 DLL 路径与常规档**完全相同**，
#      只在 `$TMP/wfcheck/` 里多写一个 .xml（**不用它就别管它**）。两个程序集各一份 .xml。
#   ⚠️ 默认（WF_DOC 不设或 ≠ 1）时，下面**每一步都与以前逐字相同**（零影响）。
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
DOC_MODE="${WF_DOC:-0}"        # 🆕 A453：可选「XML doc 档」，**默认 0 = 关**（关着时下面每一步与以前逐字相同）
# 🔴 2026-10-03：这两个目录**必须先建**。缺 `wfcheck/` 时 csc 连输出都写不出，
#    只报一行**行首**的 `error CS0006`（找不到元数据文件），**根本不解析源码**
#    ⇒ 而下面原来的判据 `grep ' error '`（前后都要空格）**匹配不到行首的 error**
#    ⇒ **编译根本没跑起来，却报「错误数: 0」**。实测：一个少了个 `)` 的语法错被报成 0/0。
#    ⚠️ 换一个新的 `TMPDIR` 时这两层都会命中 —— **新建 TMPDIR 的并发跑尤其危险**。
mkdir -p "$TMP/wfcheck"
"$PY" d:/4/Unity/工具/gen_csc_rsp.py >/dev/null || exit 1
R1="$(cygpath -w "$TMP/wf_csc.rsp")"
R2="$(cygpath -w "$TMP/wf_csc_editor.rsp")"

# ---- 🆕 A453：`-doc:` 档（**只在 WF_DOC=1 时**走这一段；默认一行都不执行）------------
#   mkdoc：把一份 rsp 复制出来，在 `-out:` 那一行**前面**插一行 `-doc:"<xml>"`。
#   ⚠️ **不动 `-out:`**（产物 DLL 路径不变）⇒ 编辑器那份 rsp 仍然找得到刚编出的 `WFCheck.dll`。
mkdoc () {                     # $1 = 源 rsp（win 路径）  $2 = 目标 rsp（win 路径）  $3 = doc xml（win 路径）
  "$PY" - "$1" "$2" "$3" <<'PYEOF'
import io, sys
src, dst, xml = sys.argv[1], sys.argv[2], sys.argv[3]
out = []
for l in io.open(src, encoding='utf-8').read().split('\n'):
    if l.startswith('-out:'):
        out.append('-doc:"%s"' % xml)
    out.append(l)
io.open(dst, 'w', encoding='utf-8', newline='').write('\n'.join(out))
PYEOF
}
D1=""; D2=""
if [ "$DOC_MODE" = "1" ]; then
  D1="$(cygpath -w "$TMP/wf_csc_doc.rsp")"
  D2="$(cygpath -w "$TMP/wf_csc_editor_doc.rsp")"
  mkdoc "$R1" "$D1" "$(cygpath -w "$TMP/wfcheck/WFCheckDoc.xml")"       || exit 1
  mkdoc "$R2" "$D2" "$(cygpath -w "$TMP/wfcheck/WFCheckEditorDoc.xml")" || exit 1
fi

# 🔴 判据必须是「有 `error CS`」，**不是**「有 ` error `」——
#    后者要求 error 前后都有空格，**行首的 error（CS0006/CS0016/CS2001）一条都匹配不到**。
#    同时盯 csc 的退出码：非 0 却一条 error 都数不到 ⇒ **编译没跑起来**，必须吼出来（别静默报 0）。
check () {                     # $1 = 名字（运行时/编辑器）  $2 = rsp 路径  $3 = doc rsp 路径（WF_DOC=1 才用）
  local out rc n rsp w opts
  rsp="$2"; opts=""
  if [ "$DOC_MODE" = "1" ]; then
    rsp="$3"; opts="-utf8output"    # ⚠️ 只在 doc 档加：csc 默认按控制台代码页(GBK)输出，中文诊断会乱码；
  fi                                #    默认档**一个字节都不动**（保持与以前逐字相同）
  out="$(dotnet "$CSC" "@$rsp" $opts 2>&1)"; rc=$?
  echo "$out" | grep -E "error CS" | head -20
  n="$(echo "$out" | grep -c 'error CS')"
  echo "$1错误数: $n"
  if [ "$DOC_MODE" = "1" ]; then
    # 🆕 A453：XML doc 这一族（CS1570/1571/1572/1573/1574/1580/1587/1589…）**全是 warning**，
    #    且**只有传了 `-doc:` 才会有** —— 所以它必须单独打、单独数（原来那句只 grep `error CS`）。
    #    ⚠️ **CS1591「缺 XML 注释」单列**：本工程实测 3502 条（那是「没写注释」，不是「写坏了」），
    #       混在一起会把真正的格式错淹掉（`head -20` 那 20 行会全是 1591）。
    w="$(echo "$out" | grep -E 'warning CS15[0-9][0-9]' | grep -vc 'warning CS1591')"
    echo "$out" | grep -E 'warning CS15[0-9][0-9]' | grep -v 'warning CS1591' | head -20
    # 🔴 **2026-10-18（第三会话）：上面那 20 行是【随机 20 条】，⛔ 不能当「全部」用。**
    #    实测：同一条命令连跑两次，取前 20 条求 md5 = `b06cbbd1…` vs `bb8c6af5…`（**总数两次一样**）
    #    ⇒ **csc 的告警发射顺序每次跑都不同**，`head -20` 拿到的是**随机样本**。
    #    代价：本会话就有人拿那 20 行当全集，把 **223 条 / 28 文件读成了 36 条 / 6 文件**。
    #    ⇒ 下面这条**按文件汇总**（顺序无关）才是全集视图。
    echo "--- XML doc 告警【按文件汇总 · 全集 · 顺序无关】---"
    echo "$out" | grep -E 'warning CS15[0-9][0-9]' | grep -v 'warning CS1591' \
      | sed -E 's/\([0-9]+,[0-9]+\):.*//' | sort | uniq -c | sort -rn
    m="$(echo "$out" | grep -c 'warning CS1591')"
    echo "$1 XML doc 警告数: $w（格式类，不含 CS1591；另有「缺 XML 注释」CS1591 共 $m 条）"
  fi
  if [ "$rc" != "0" ] && [ "$n" = "0" ]; then
    echo "🔴 $1：csc 退出码 $rc 却数不到任何 error ⇒ 【编译根本没跑起来】，这个 0 不算数！下面是原始输出尾部："
    echo "$out" | grep -v 'warning CS2002' | tail -6
  fi
  return 0
}
echo "--- 运行时程序集 ---"; check 运行时 "$R1" "$D1"
echo "--- 编辑器程序集 ---"; check 编辑器 "$R2" "$D2"
