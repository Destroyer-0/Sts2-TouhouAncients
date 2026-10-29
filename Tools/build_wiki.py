# /// 生成 Tools/wiki/data.js（先古 → 行 → 遗物 → 衍生牌/关键词）。
# /// 图片一律走相对路径 ../../images/，本地双击与 Pages 根目录发布都可用，不复制素材。
import argparse
import datetime
import html
import json
import os
import re
import sys

TOOLS = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(TOOLS)
WIKI = os.path.join(TOOLS, "wiki")
sys.path.insert(0, os.path.join(ROOT, ".agents", "skills", "ancient-relic-list", "scripts"))

import relic_list as rl  # noqa: E402

ASSET_PREFIX = "../../images/"

# 取自游戏 StsColors，保证网页配色与游戏一致
TAG_COLORS = {
    "gold": "efc851", "blue": "87ceeb", "dark_blue": "67aeeb", "red": "ff5555",
    "green": "7fff00", "orange": "ffa518", "pink": "ff78a0", "purple": "ee82ee",
    "aqua": "2aebbe",
}
COLOR_TAG_RE = re.compile(r'\[(gold|blue|dark_blue|red|green|orange|pink|purple|aqua)\](.*?)\[/\1\]', re.S)
ANY_TAG_RE = re.compile(r'\[/?(?:[a-zA-Z_]+)(?:=[^\]\s]*)?\]')


def rich(raw, vars_, deltas=None, upgrade=None):
    """保留颜色标签渲染成 HTML（其余特效标签只留文字）。

    用栈处理嵌套：`[red]第[blue]3[/blue]回合[/red]` 必须渲染成嵌套 span，
    哨兵占位的老写法会把外层颜色吃掉并把标签名漏成正文。
    upgrade：None/False/True 透传给 render，控制数值取升级前、升级后还是写成 `基础（升级）`。
    """
    text = rl.render(raw or "", vars_, deltas, keep_tags=True, upgrade=upgrade)
    out, stack = [], []
    pos = 0
    for m in ANY_TAG_RE.finditer(text):
        out.append(html.escape(text[pos:m.start()]).replace('\n', '<br>'))
        pos = m.end()
        tag = m.group(0)[1:-1]
        name = tag.lstrip('/').lower()
        color = TAG_COLORS.get(name)
        if not color:
            continue                    # 非颜色标签（jitter / sine / b …）只去掉标记
        if tag.startswith('/'):
            if name in stack:           # 只闭合最近的同名颜色，错配时忽略
                while stack:
                    popped = stack.pop()
                    out.append('</span>')
                    if popped == name:
                        break
        else:
            stack.append(name)
            out.append('<span style="color:#%s">' % color)
    out.append(html.escape(text[pos:]).replace('\n', '<br>'))
    out.extend('</span>' for _ in stack)   # 未闭合的颜色兜底
    return ''.join(out)


def img(rel):
    return ASSET_PREFIX + rel


def icon_for(kind, cls, folder=None):
    """有图标就返回路径，没有返回 None（前端回落到 default）。"""
    if kind == "card":
        rel = "cards/%s.png" % cls
    elif folder:
        rel = "%s/%s.png" % (folder, cls)
    else:
        return None
    path = os.path.join(ROOT, "images", rel.replace("/", os.sep))
    return img(rel) if os.path.isfile(path) else None


CARD_TYPE_CN = {"Attack": "攻击", "Skill": "技能", "Power": "能力", "Curse": "诅咒",
                "Status": "状态", "Quest": "任务"}

# ShowAct：1/2/3 = 只在对应幕出现，None = 二、三幕通用（按此分档排序）
SHOW_ACT_ORDER = {1: 0, 2: 1, 3: 2, None: 3}
SHOW_ACT_LABEL = {1: "1层先古之民", 2: "2层先古之民", 3: "3层先古之民", None: "2/3层先古之民"}

LANG_META = {
    "zhs": {
        "file": "data.js",
        "htmlLang": "zh-CN",
        "labels": {
            "docTitle": "东方先古之民 · 遗物图鉴",
            "energy": "耗能",
            "relicCount": "件遗物",
            "derivedBadge": "衍生",
            "upgradeToggle": "显示升级后",
            "rowPrefix": "",
            "share": "分享网页",
            "shared": "已分享",
            "copied": "已复制链接",
            "copyFail": "复制失败，请手动复制",
            "back": "← 返回全部先古之民",
            "empty": "没有匹配的先古之民或遗物。",
            "noData": "没有数据，请先运行 Tools/build_wiki.py。",
            "workshop": "前往创意工坊 ↗",
            "searchPlaceholder": "筛选先古之民 / 遗物…",
            "mapNodeAlt": "节点图",
            "derivedHint": "点击查看衍生内容",
            "footer": "原作：杀戮尖塔2 / 东方project<br>模组作者：阿迪　画师：河城绿茶",
            "langButton": "EN",
        },
        "showActLabel": {1: "1层先古之民", 2: "2层先古之民", 3: "3层先古之民", None: "2/3层先古之民"},
        "cardType": {"Attack": "攻击", "Skill": "技能", "Power": "能力", "Curse": "诅咒",
                     "Status": "状态", "Quest": "任务"},
    },
    "eng": {
        "file": "data.en.js",
        "htmlLang": "en",
        "labels": {
            "docTitle": "Touhou Ancients · Relic Codex",
            "energy": "Cost",
            "relicCount": "relics",
            "derivedBadge": "Derived",
            "upgradeToggle": "Show upgraded",
            "rowPrefix": "",
            "share": "Share",
            "shared": "Shared",
            "copied": "Link copied",
            "copyFail": "Copy failed — please copy manually",
            "back": "← Back to all Ancients",
            "empty": "No matching Ancients or relics.",
            "noData": "No data. Run Tools/build_wiki.py first.",
            "workshop": "Steam Workshop ↗",
            "searchPlaceholder": "Filter Ancients / relics…",
            "mapNodeAlt": "map node",
            "derivedHint": "Click to view derived content",
            "footer": "Original: Slay the Spire 2 / Touhou Project<br>Mod authors: Adi · Artist: Kawashiro Ryokucha",
            "langButton": "日本語",
        },
        "showActLabel": {1: "Act 1 Ancient", 2: "Act 2 Ancient", 3: "Act 3 Ancient",
                         None: "Act 2 & 3 Ancient"},
        "cardType": {"Attack": "Attack", "Skill": "Skill", "Power": "Power", "Curse": "Curse",
                     "Status": "Status", "Quest": "Quest"},
    },
    "jpn": {
        "file": "data.jpn.js",
        "htmlLang": "ja",
        "labels": {
            "docTitle": "東方エンシェント · レリック図鑑",
            "energy": "コスト",
            "relicCount": "個のレリック",
            "derivedBadge": "派生",
            "upgradeToggle": "アップグレード後を表示",
            "rowPrefix": "",
            "share": "共有",
            "shared": "共有しました",
            "copied": "リンクをコピーしました",
            "copyFail": "コピーできませんでした。手動でコピーしてください",
            "back": "← エンシェント一覧へ",
            "empty": "一致するエンシェント・レリックはありません。",
            "noData": "データがありません。先に Tools/build_wiki.py を実行してください。",
            "workshop": "Steamワークショップへ ↗",
            "searchPlaceholder": "エンシェント / レリックを絞り込む…",
            "mapNodeAlt": "マップノード",
            "derivedHint": "クリックで派生コンテンツを表示",
            "footer": "原作：Slay the Spire 2 / 東方Project<br>MOD作者：阿迪　イラスト：河城绿茶",
            "langButton": "中文",
        },
        "showActLabel": {1: "第1幕のエンシェント", 2: "第2幕のエンシェント", 3: "第3幕のエンシェント",
                         None: "第2・3幕のエンシェント"},
        "cardType": {"Attack": "アタック", "Skill": "スキル", "Power": "パワー", "Curse": "呪い",
                     "Status": "状態異常", "Quest": "クエスト"},
    },
}


def card_keywords(cls):
    """(升级前关键词, 升级后关键词)：升级中的 Add/RemoveKeyword 决定差异。"""
    meta = rl.card_meta(cls)
    removed = [k for add, k in meta["kwdelta"] if not add]
    added = [k for add, k in meta["kwdelta"] if add]
    up = [k for k in meta["keywords"] if k not in removed] + \
         [k for k in added if k not in meta["keywords"]]
    return meta["keywords"], up


COST_UP_COLOR = "#7fff00"        # 升级减费时费用按游戏惯例显示为绿色

CURRENT = "zhs"                  # 当前生成语言，由 main() 逐个切换


def label(key):
    return LANG_META[CURRENT]["labels"][key]


def card_type_label(t):
    return LANG_META[CURRENT]["cardType"].get(t, t or "")


def act_label(v):
    return LANG_META[CURRENT]["showActLabel"][v]


def cost_meta(meta, upgraded=False):
    """(类型 · 费用) 文本；升级后费用带绿色 span（升级减费才变色）。"""
    sep = "" if CURRENT == "zhs" else " "
    cost = meta["cost"]
    if cost == "X":
        cost_txt = rl.COST_WORD.get(CURRENT, "耗能") + sep + "X"
    elif cost is None or cost == -1:
        cost_txt = ""
    else:
        value = cost + (meta.get("cost_delta") or 0) if upgraded else cost
        shown = str(value)
        if upgraded and (meta.get("cost_delta") or 0) < 0:
            shown = '<span style="color:%s">%s</span>' % (COST_UP_COLOR, value)
        cost_txt = rl.COST_WORD.get(CURRENT, "耗能") + sep + shown
    parts = [card_type_label(meta["type"])]
    if cost_txt:
        parts.append(cost_txt)
    return " · ".join(p for p in parts if p)


def card_detail(cls):
    key = "TOUHOUANCIENTS-" + rl.snake(cls)
    meta = rl.card_meta(cls)
    override = rl.CARD_OVERRIDE.get(cls)
    if override:
        title = override["title"]
        body = rich(override["body"], {})
        body_up = rich(override.get("bodyUp", override["body"]), {})
    else:
        src = rl.strip_comments(rl.read(rl.mod_card_files[cls]))
        v = rl.parse_vars(src)
        raw = rl.cards_loc.get(key + ".description", "")
        body = rich(raw, v, meta["deltas"], upgrade=False)
        body_up = rich(raw, v, meta["deltas"], upgrade=True)
        if "{" in body:
            alt = rich(rl.cards_loc.get(key + ".descriptionIdle", ""), v, meta["deltas"], upgrade=False)
            if alt and "{" not in alt:
                body = body_up = alt
        title = rl.cards_loc.get(key + ".title", cls)
    kw_base, kw_up = card_keywords(cls)
    upgradable = meta["maxup"] > 0
    return {"name": title, "html": body, "meta": cost_meta(meta),
            "htmlUp": body_up if upgradable else None,
            "metaUp": cost_meta(meta, upgraded=True) if upgradable else None,
            "keywords": [rl.kw_cn(k) for k in kw_base],
            "keywordsUp": [rl.kw_cn(k) for k in kw_up] if upgradable else None}


def generic_detail(kind, cls, text, folder, raw_key):
    name, _, body = text.partition("：")
    return {"name": name, "html": html.escape(body), "meta": "", "keywords": [],
            "icon": icon_for(kind, cls, folder), "hasIcon": True}


def entries_for(kind, t, v, src):
    """单个引用 → 衍生条目列表（卡牌会额外带上自己的关键词说明）。"""
    out = []
    if kind in ("affliction", "base_ench"):
        hit = rl.ref_entry(kind, t, v, src)
        if hit and hit[1]:
            out.append({"kind": kind, "id": t, "name": hit[0], "html": rich(hit[1], {}),
                        "meta": "", "keywords": [], "icon": None})
        return out
    if kind == "base_card":
        d = rl.base_card_detail(t)
        if d:
            out.append({"kind": kind, "id": t, "name": d["name"],
                        "html": html.escape(d["html"]).replace("\n", "<br>"),
                        "meta": d["meta"], "keywords": d["keywords"], "icon": None})
        return out
    if kind == "keyword":
        if rl.snake(t) not in rl.KW_DERIVED_SKIP and rl.kw_desc(t):
            out.append({"kind": kind, "id": t, "name": rl.kw_cn(t), "html": rich(rl.kw_desc(t), {}),
                        "meta": "", "keywords": [], "icon": None})
        return out
    if t in rl.DERIVED_OVERRIDE:
        text = rl.DERIVED_OVERRIDE[t]
        if not text:
            return out
        if kind == "card" and t in rl.mod_card_files:
            name, _, body = text.partition("：")
            detail = {"name": name, "html": html.escape(body), "meta": "", "keywords": [],
                      "icon": icon_for("card", t)}
        else:
            detail = generic_detail(kind, t, text, {"ench": "icon/enchantment", "power": "icon/power",
                                                    "potion": "potion"}.get(kind), None)
        detail.update({"kind": kind, "id": t})
        out.append(detail)
        return out
    if kind == "card":
        d = card_detail(t)
        d.update({"kind": kind, "id": t, "icon": icon_for("card", t)})
        out.append(d)
        # 关键词说明：消耗/虚无/保留 太常见不列，其余（无意识、沉底、永恒…）各占一条
        for kw in dict.fromkeys(rl.card_meta(t)["keywords"] + rl.card_meta(t)["extra_keywords"]):
            if rl.snake(kw) in rl.KW_DERIVED_SKIP or not rl.kw_desc(kw):
                continue
            out.append({"kind": "keyword", "id": t + ":" + kw, "name": rl.kw_cn(kw),
                        "html": rich(rl.kw_desc(kw), {}), "meta": "", "keywords": [], "icon": None})
        return out
    if kind in ("ench", "power", "potion"):
        folder = {"ench": "icon/enchantment", "power": "icon/power", "potion": "potion"}[kind]
        fn = {"ench": rl.enchant_entry, "power": rl.power_entry}[kind] if kind != "potion" else None
        text = fn(t) if fn else rl.potion_entry(t, v)
        if text:
            d = generic_detail(kind, t, text, folder, None)
            d.update({"kind": kind, "id": t})
            out.append(d)
    return out


def derived_for(cls, src, raw, v, desc_plain):
    """遗物的衍生条目：卡片带卡面/类型/关键词，附魔/能力/药水带图标，其余为纯文本行。"""
    out = []
    refs = list(rl.relic_refs(src, raw, cls))
    # 衍生卡自己的 hover 提示也算（例：仆从牌提示原版「晕眩」「小刀」，心之瞳提示原版附魔「本能」）
    for kind, t in list(refs):
        if kind == "card" and t in rl.mod_card_files:
            refs.extend((k2, t2) for k2, t2 in rl.hover_refs(rl.read(rl.mod_card_files[t]))
                        if k2 in ("base_card", "base_ench", "ench", "affliction", "keyword"))
    for kind, t in refs:
        out.extend(entries_for(kind, t, v, src))
    # 休息处选项
    low = desc_plain.lower()
    rest_word = rl.REST_SITE_WORD.get(rl.LANG, "休息处").lower()
    for opt_key, opt_name in rl.REST_OPTIONS.items():
        if opt_name.lower() in low and rest_word in low:
            body = rl.render(rl.rest_loc[opt_key + ".description"], v).replace("\n", "")
            if "{" in body:
                body = rl.REST_OVERRIDE.get(opt_key, body)
            out.append({"kind": "text", "id": opt_key, "name": opt_name, "html": html.escape(body),
                        "meta": "", "keywords": [], "icon": None})
    for line in rl.KEYWORD_LINES.get(cls, []):
        name, _, body = line.partition("：")
        out.append({"kind": "text", "id": cls + ":" + name, "name": name, "html": html.escape(body),
                    "meta": "", "keywords": [], "icon": None})
    for line in rl.EXTRA_LINES.get(cls, []):
        name, _, body = line.partition("：")
        out.append({"kind": "text", "id": cls + ":" + name, "name": name, "html": html.escape(body),
                    "meta": "", "keywords": [], "icon": None})
    for kind, t in rl.relic_refs(src, raw, cls):
        for line in rl.EXTRA_LINES.get(t, []):
            name, _, body = line.partition("：")
            out.append({"kind": "text", "id": t + ":" + name, "name": name, "html": html.escape(body),
                        "meta": "", "keywords": [], "icon": None})
    seen, uniq = set(), []                      # 同一名字只留第一条（手写覆盖项先入，优先保留）
    for d in out:
        if d["name"] in seen:
            continue
        seen.add(d["name"])
        uniq.append(d)
    return uniq


# 说话者键与头像文件名不完全一致时的别名（jyoon → YorigamiSister_Joon.png）
SPEAKER_ALIAS = {"jyoon": "joon"}


def speaker_avatar(short, speaker):
    """差分头像：文件名大小写不一定与 speaker 键一致（jyoon → YorigamiSister_Joon.png）。"""
    folder = os.path.join(ROOT, "images", "icon", "Character")
    if not os.path.isdir(folder):
        return None
    names = os.listdir(folder)
    for key in (speaker, SPEAKER_ALIAS.get(speaker.lower(), speaker)):
        want = ("%s_%s.png" % (short, key)).lower()
        for name in names:
            if name.lower() == want:
                return img("icon/Character/" + name)
    return None


def dialogues_for(a):
    """通用对话：段内每句一行；女苑/紫苑按 speaker 带差分头像。"""
    out = []
    short = a["file"][:-len("Ancient")]
    for seg in a["dialogues"]:
        lines = []
        for line in seg["lines"]:
            speaker = (line["speaker"] or "").strip()
            lines.append({"html": rich(line["text"], {}), "speaker": speaker,
                          "avatar": speaker_avatar(short, speaker) if speaker else None})
        if lines:
            out.append(lines)
    return out


def build(ancients_sel=None, relics_sel=None):
    out = []
    ancients = sorted(rl.parse_ancients(), key=lambda x: (SHOW_ACT_ORDER[x["showAct"]], x["file"]))
    for a in ancients:
        if ancients_sel is not None and a["file"] not in ancients_sel:
            continue
        short = a["file"][:-len("Ancient")]
        multi_pool = len(a["pools"]) > 1     # 与 markdown 同口径：单池角色平铺不标行
        pools, row = [], 0
        for pool_info in a["pools"]:
            cnt = pool_info["count"]
            label = None
            if multi_pool:
                label = rl.ROW_CN[row] if cnt == 1 else "/".join(rl.ROW_CN[row:row + cnt])
            row += cnt
            relics = []
            for c in pool_info["relics"]:
                if relics_sel is not None and c not in relics_sel:
                    continue
                key = "TOUHOUANCIENTS-" + rl.snake(c)
                raw = rl.relics_loc.get(key + ".description", "")
                src = rl.strip_comments(rl.read(rl.relic_files[c]))
                v = rl.runtime_vars(src, rl.parse_vars(src))
                if c in rl.RELIC_OVERRIDE:
                    plain = rl.RELIC_OVERRIDE[c]
                    body = html.escape(plain)
                else:
                    plain = rl.render(raw, v).replace("\n", "")
                    body = rich(raw, v)
                flavor_raw = rl.relics_loc.get(key + ".flavor", "").replace("\n", " ")
                plain_flavor = rl.render(flavor_raw, v).strip()
                relics.append({
                    "id": c,
                    "name": rl.relics_loc.get(key + ".title", c),
                    "icon": icon_for("relic", c, "icon/relics") or img("icon/relics/default.png"),
                    "html": body,
                    "flavor": rich(flavor_raw, v) or None,
                    "plain": (plain + " " + plain_flavor).strip(),
                    "derived": derived_for(c, src, raw, v, plain),
                })
            if relics:
                pools.append({"label": label, "relics": relics})
        if not pools:
            continue
        out.append({
            "id": a["file"],
            "name": a["title"],
            "epithet": a["epithet"],
            "showAct": a["showAct"],
            "showActLabel": act_label(a["showAct"]),
            "avatar": img("icon/Character/%s.png" % short),
            "avatarOutline": img("icon/Character/Outline/%s.png" % short),
            "mapNode": img("icon/MapNode/%s_MapNode.png" % short),
            "mapNodeOutline": img("icon/MapNode/Outline/%s_MapNode.png" % short),
            "dialogues": dialogues_for(a),
            "pools": pools,
        })
    return {"lang": CURRENT,
            "htmlLang": LANG_META[CURRENT]["htmlLang"],
            "labels": LANG_META[CURRENT]["labels"],
            "generatedAt": datetime.datetime.now().strftime("%Y-%m-%d %H:%M"),
            "assetPrefix": ASSET_PREFIX, "ancients": out}


def listing():
    """给 exe 用的勾选清单：先古 → 遗物（只有名字）。"""
    rows = []
    for a in sorted(rl.parse_ancients(), key=lambda x: (SHOW_ACT_ORDER[x["showAct"]], x["file"])):
        relics = []
        for pool in a["pools"]:
            for c in pool["relics"]:
                key = "TOUHOUANCIENTS-" + rl.snake(c)
                relics.append({"id": c, "name": rl.relics_loc.get(key + ".title", c)})
        rows.append({"id": a["file"], "name": a["title"], "epithet": a["epithet"],
                     "showAct": a["showAct"], "showActLabel": act_label(a["showAct"]),
                     "relics": relics})
    return rows


def write_data(data, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("window.THA_DATA = ")
        json.dump(data, f, ensure_ascii=False, separators=(",", ":"))
        f.write(";\n")


def main():
    ap = argparse.ArgumentParser(description="生成 Tools/wiki/data.js（中文）与 data.en.js（英文）")
    ap.add_argument("--select", help="勾选结果 JSON：{ancients:[类名], relics:[类名]}；缺省=全部")
    ap.add_argument("--out", help="单语言输出路径（配合 --lang）；缺省写 wiki/data.js 或 data.en.js")
    ap.add_argument("--lang", choices=list(rl.LANGUAGES), help="只生成某一种语言；缺省=中英各一份")
    ap.add_argument("--list", action="store_true", help="输出勾选清单 JSON 到 stdout（中文）")
    args = ap.parse_args()

    if args.list:
        rl.set_language("zhs")
        json.dump(listing(), sys.stdout, ensure_ascii=False)
        return 0

    a_sel = r_sel = None
    if args.select:
        sel = json.load(open(args.select, encoding="utf-8-sig"))
        a_sel = set(sel.get("ancients") or [])
        r_sel = set(sel.get("relics") or [])

    global CURRENT
    langs = [args.lang] if args.lang else list(rl.LANGUAGES)
    for lang in langs:
        rl.set_language(lang)
        CURRENT = lang
        data = build(a_sel, r_sel)
        out = args.out or os.path.join(WIKI, LANG_META[lang]["file"])
        write_data(data, out)
        relic_count = sum(len(p["relics"]) for a in data["ancients"] for p in a["pools"])
        derived_count = sum(len(r["derived"]) for a in data["ancients"]
                           for p in a["pools"] for r in p["relics"])
        print("[%s] %s：%d 位先古之民 / %d 件遗物 / %d 条衍生说明" %
              (lang, out, len(data["ancients"]), relic_count, derived_count))
    return 0


if __name__ == "__main__":
    sys.exit(main())
