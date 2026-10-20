#!/usr/bin/env python
# W58-K12: typed static scan for reads of RawCardScript.cardShortId (+0x40)
# Read-only. Parses il2cpp dump.cs + Ghidra decomp_full .c text.
import io, os, re, sys, json, collections

DUMP = r"d:/2/tools/il2cpp_out/dump.cs"
DEC  = r"d:/2/tools/decomp_full"

def read(p):
    with io.open(p, "r", encoding="utf-8", errors="replace") as f:
        return f.read().splitlines()

lines = read(DUMP)

# --- 1. parse classes and their methods -------------------------------
cls_re = re.compile(r'^(?:public |private |internal |protected )?(?:static |sealed |abstract )*(?:class|struct|interface|enum)\s+([A-Za-z0-9_\.<>`]+)')
cur = None
methods = []  # (cls, name, sig, lineno)
for i, ln in enumerate(lines, 1):
    m = cls_re.match(ln)
    if m:
        cur = m.group(1)
        continue
    s = ln.strip()
    if cur and s.endswith("{ }") and "(" in s and not s.startswith("//"):
        # method / property accessor line
        name_m = re.search(r'([A-Za-z0-9_<>\.`]+)\s*\(', s)
        if name_m:
            name = name_m.group(1).split(".")[-1]
            methods.append((cur, name, s, i))

print("classes parsed, methods total =", len(methods))

raw_methods = [m for m in methods if m[0] == "RawCardScript"]
sig_raw = [m for m in methods if "RawCardScript" in m[2]]
print("RawCardScript instance methods =", len(raw_methods))
print("methods with RawCardScript in signature =", len(sig_raw))

# --- 2. map methods to decompiled files --------------------------------
files = set(os.listdir(DEC))
def cand_files(cls, name):
    out = []
    # top-level or nested class names in decomp use '.' -> '__'? sample: AI.__c__.cctor.c
    c1 = cls.replace("+", ".").replace("/", ".")
    out.append(c1 + "__" + name + ".c")
    out.append(c1.replace(".", "__") + "__" + name + ".c")
    out.append(c1.split(".")[-1] + "__" + name + ".c")
    return [x for x in out if x in files]

targets = []
missing = []
for cls, name, sig, ln in sig_raw + [m for m in raw_methods if m not in sig_raw]:
    cf = cand_files(cls, name)
    if cf:
        targets.append((cls, name, sig, ln, cf))
    else:
        missing.append((cls, name, sig, ln))

print("mapped to decomp files =", len(targets), " missing =", len(missing))

# --- 3. scan bodies for +0x40 ------------------------------------------
pat = re.compile(r'\+\s*0x40\b')
hits = []
for cls, name, sig, ln, cf in targets:
    for fn in cf:
        body = read(os.path.join(DEC, fn))
        for j, bl in enumerate(body, 1):
            if pat.search(bl):
                hits.append((fn, j, bl.strip(), sig, ln))

print("files with +0x40 access =", len(hits))
for h in hits:
    print("HIT %s:%d  %s\n    sig(dump.cs:%d): %s" % (h[0], h[1], h[2], h[4], h[3]))
