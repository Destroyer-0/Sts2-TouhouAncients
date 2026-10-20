# /// 把某位（或全部）先古之民的遗物导出成 docs/THA-Relic.md 用的 markdown。
# /// 数据来源：scripts/ancients 的选项池 → zhs/relics.json 的文案 + 遗物 .cs 的 CanonicalVars。
import argparse
import glob
import json
import os
import re
import sys

SKILL_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def find_root(start):
    cur = os.path.abspath(start)
    while True:
        if os.path.isfile(os.path.join(cur, ".agents", "local.md")):
            return cur
        parent = os.path.dirname(cur)
        if parent == cur:
            raise SystemExit("找不到仓库根目录（缺 .agents/local.md）")
        cur = parent


ROOT = find_root(SKILL_DIR)
LANGUAGES = ("zhs", "eng")
LANG = "zhs"
LOC = os.path.join(ROOT, "TouhouAncients", "localization", LANG)


def sts2_src():
    """从 .agents/local.md 读本机的本体反编译源码路径（拿原版关键词/卡牌名用）。"""
    path = os.path.join(ROOT, ".agents", "local.md")
    text = open(path, encoding="utf-8-sig", errors="ignore").read()
    m = re.search(r'`sts2-src`\s*:\s*`([^`]+)`', text)
    return m.group(1).strip() if m else None


STS2_SRC = sts2_src()
BASE_LOC = os.path.join(STS2_SRC, "localization", LANG) if STS2_SRC else None


def load(name, base=False):
    folder = BASE_LOC if base else LOC
    if not folder:
        return {}
    path = os.path.join(folder, name)
    if not os.path.isfile(path):
        return {}
    return json.load(open(path, encoding="utf-8-sig"))


# 各语言的本地化表，由 set_language() 装载（模块底部默认装载中文）
relics_loc, cards_loc, ench_loc, pow_loc, pot_loc = {}, {}, {}, {}, {}
mod_kw_loc, rest_loc, ancients_loc = {}, {}, {}
base_kw, base_ench_loc, base_pow_loc, base_cards_loc, chars_loc = {}, {}, {}, {}, {}
mod_aff_loc, base_aff_loc = {}, {}
REST_OPTIONS = {}


def snake(s):
    s = re.sub(r'(?<=[a-z0-9])([A-Z])', r'_\1', s)
    s = re.sub(r'(?<=[A-Z])([A-Z][a-z])', r'_\1', s)
    return s.upper()


def read(p):
    return open(p, encoding="utf-8", errors="ignore").read()


def strip_comments(src):
    """解析前先剥注释，否则被注释掉的 TARelicOption<X>() 会被算进池子。"""
    src = re.sub(r'/\*.*?\*/', '', src, flags=re.S)
    src = re.sub(r'(?m)^\s*//.*$', '', src)
    src = re.sub(r'(?<![:/])//[^\n"\x27]*$', '', src, flags=re.M)
    return src


def arrow_body(src, anchor):
    """取 `anchor => ...` 的主体。

    mod 代码是 `=> [ … ]`；本体反编译出来是 `=> new global::…ReadOnlySingleElementList<T>(…);`。
    先看箭头后面是不是直接跟括号体，否则取到该语句的分号为止（不能直接用 body_after，
    否则会一路扫到后面构造函数的空 `{ }`）。
    """
    m = re.search(anchor, src)
    if not m:
        return None
    rest = src[m.end():]
    head = rest.lstrip()
    if head.startswith('[') or head.startswith('{'):
        return body_after(src, anchor)
    i = rest.find(';')
    return rest[:i] if i >= 0 else rest


def body_after(src, anchor):
    """取 anchor 之后的第一个 [...] 或 {...} 主体（跳过 new[] 的空方括号）。"""
    m = re.search(anchor, src)
    if not m:
        return None
    src2 = src[m.end():].replace("new[]", "new ").replace("new [", "new ")
    i = 0
    while i < len(src2) and src2[i] not in '[{':
        i += 1
    if i >= len(src2):
        return None
    op = src2[i]
    cl = ']' if op == '[' else '}'
    depth = 0
    j = i
    while j < len(src2):
        if src2[j] == op:
            depth += 1
        elif src2[j] == cl:
            depth -= 1
            if depth == 0:
                break
        j += 1
    return src2[i:j + 1]


def match_bracket(s, start, op='[', cl=']'):
    depth = 1
    i = start
    while i < len(s) and depth > 0:
        if s[i] == op:
            depth += 1
        elif s[i] == cl:
            depth -= 1
        i += 1
    return i


# ---------------- 数值解析 ----------------
NUM = re.compile(r'^-?\d+(?:\.\d+)?m?$')


def symtab(src):
    t = {}
    for m in re.finditer(r'(?:const|(?:static\s+)?readonly)\s+(?:int|decimal|float|double)\s+(\w+)\s*=\s*([^;]+);', src):
        t[m.group(1)] = ("num", m.group(2).strip())
    for m in re.finditer(r'const\s+string\s+(\w+)\s*=\s*"([^"]*)";', src):
        t[m.group(1)] = ("str", m.group(2))
    return t


def eval_expr(expr, cs, depth=0):
    expr = re.sub(r'\s+', ' ', expr.strip())
    if depth > 8:
        raise ValueError(expr)
    if NUM.match(expr):
        return float(expr.rstrip('m'))
    m = re.fullmatch(r'\(?\s*(?:int|decimal|float|double)\s*\)?\s*(.+)', expr)
    if m:
        return eval_expr(m.group(1), cs, depth + 1)
    if expr in cs and cs[expr][0] == "num":
        return eval_expr(cs[expr][1], cs, depth + 1)
    if not re.search(r'[+\-*/]', expr[1:]):
        raise ValueError(expr)

    def repl(m):
        name = m.group(1)
        if name in cs and cs[name][0] == "num":
            return "(" + cs[name][1].replace("m", "") + ")"
        if name == "CurrentUpgradeLevel":
            return "0"
        raise ValueError(name)

    e = re.sub(r'(\d)m\b', r'\1', expr)
    e = re.sub(r'\b([A-Za-z_]\w*)\b', repl, e)
    return float(eval(e, {"__builtins__": {}}, {}))


def resolve_str(expr, cs):
    expr = expr.strip()
    if expr.startswith('"'):
        return expr.strip('"')
    if expr in cs and cs[expr][0] == "str":
        return cs[expr][1]
    return None


def model_title(model, kind):
    """mod 优先，缺了回退本体（如本体附魔 Steady → 稳定）。"""
    key = "TOUHOUANCIENTS-" + snake(model)
    mod_tables = {"ench": ench_loc, "power": pow_loc, "card": cards_loc, "potion": pot_loc, "relic": relics_loc}
    base_tables = {"ench": base_ench_loc, "power": base_pow_loc, "card": base_cards_loc}
    table = mod_tables.get(kind, {})
    if key + ".title" in table:
        return table[key + ".title"]
    btable = base_tables.get(kind, {})
    for cand in (snake(model) + ".title", model.upper() + ".title"):
        if cand in btable:
            return btable[cand]
    return model


SIMPLE_VARS = ('Cards', 'Energy', 'Gold', 'Block', 'MaxHp', 'HpLoss', 'Damage', 'Heal', 'Hp', 'Strength',
               'Dexterity', 'Intangible', 'Poison', 'Thorns', 'Vulnerable', 'Weak', 'Regen', 'Power', 'Amount',
               'Countdown', 'ExtraDamage', 'Increase', 'Multiplier', 'Need', 'StrengthLoss', 'Repeat', 'Rests',
               'Copies')


def parse_vars(src):
    """取 CanonicalVars 里的静态默认值。算不出来的留 unresolved，由 render 保留原占位或走覆盖表。"""
    out = {}
    cs = symtab(src)
    body = arrow_body(src, r'CanonicalVars\s*=>')
    if not body:
        return out
    for mm in re.finditer(r'new\s+(\w+)\s*(?:<\s*(\w+)\s*>)?\s*\(([^()]*(?:\([^()]*\)[^()]*)*)\)', body):
        kind, gen, args = mm.group(1), mm.group(2), mm.group(3)
        sv = re.search(r'ModelDb\.(Enchantment|Power|Character)<(\w+)>\(\)\.Title', args)
        if kind == 'StringVar' or sv:
            nm = re.match(r'\s*("(?:\w+)"|[\w\.]+)', args)
            name = resolve_str(nm.group(1), cs) if nm else None
            if sv:
                out[name or args] = ('title', sv.group(1), sv.group(2))
            else:
                out[name or 'Str'] = ('string', None, args)
            continue
        if kind == 'PowerVar':
            if gen:
                try:
                    out[gen] = ('num', eval_expr(args.strip(), cs), args.strip())
                except Exception:
                    pass
            continue
        if kind in ('CalculatedVar', 'CalculatedDamageVar', 'CalculationBaseVar', 'CalculationExtraVar'):
            continue
        name, val = None, None
        nm = re.match(r'^"(\w+)"\s*,\s*(.+)$', args, re.S)
        if nm:
            name, val = nm.group(1), nm.group(2).strip()
        elif kind == 'DynamicVar':
            am = re.match(r'\s*("(?:\w+)"|[\w\.]+)\s*,\s*(.+)$', args, re.S)
            if not am:
                continue
            name = resolve_str(am.group(1), cs)
            if name is None:
                continue
            val = am.group(2).strip()
        elif kind.endswith('Var') and kind[:-3] in SIMPLE_VARS:
            name = kind[:-3]
            val = args.split(',')[0].strip()
        else:
            continue
        if val is None:
            continue
        if val.startswith('"'):
            out[name] = ('string', None, val)
            continue
        try:
            out[name] = ('num', eval_expr(val, cs), val)
        except Exception:
            out[name] = ('unresolved', None, val)
    return out


def runtime_vars(src, vars_):
    """补运行时才填的 StringVar，如 desc.Add("EnchantmentName", ModelDb.Enchantment<X>().Title…)。"""
    out = dict(vars_)
    for m in re.finditer(r'\.Add\("(\w+)",\s*([^;]+?)\);', src):
        name, expr = m.group(1), m.group(2)
        t = re.search(r'ModelDb\.(Enchantment|Power|Card|Character)<(\w+)>\(\)\.Title', expr)
        if t:
            out[name] = ('title', t.group(1), t.group(2))
            continue
        v = re.search(r'DynamicVars\["(\w+)"\]\.BaseValue', expr)
        if v and v.group(1) in vars_:
            out[name] = vars_[v.group(1)]
    return out


def fmt_num(v):
    return str(int(v)) if isinstance(v, float) and v == int(v) else str(v)


TAGS = re.compile(r'\[/?(?:[a-zA-Z_]+)(?:=[^\]\s]*)?\]')
DYN_BLOCK = re.compile(r'\{(?:InCombat|OutOfCombat|NotInCombat)\b.*?\|\}', re.S)


def charac(v):
    if v[0] == 'num':
        return 'num', v[1]
    if v[0] == 'title':
        kind, model = v[1], v[2]
        if kind == 'Enchantment':
            return 'str', model_title(model, 'ench')
        if kind == 'Power':
            return 'str', model_title(model, 'power')
        if kind == 'Character':
            return 'str', chars_loc.get(snake(model) + ".title", model)
    return v[0], None


def render(text, vars_, deltas=None, keep_tags=False, upgrade=None):
    """占位符替换 + 去富文本标签。

    upgrade：None = 打成 `基础（升级）`（markdown 用）；False/True = 只取升级前/后的数值（网页的升级开关用）。
    """
    t = DYN_BLOCK.sub('', text)
    energy_word = ENERGY_WORD.get(LANG, "能量")
    energy_join = " " if LANG == "eng" else ""

    def ifup(m):
        up, base = m.group(1), m.group(2)
        if upgrade is True:
            return up
        if upgrade is False:
            return base
        return ("（升级后：" + up + "）") if not base else base + "（" + up + "）"

    t = re.sub(r'\{IfUpgraded:show:([^|{}]*)\|([^|{}]*)\}', ifup, t)

    def rep(m):
        body = m.group(1)
        head = body.split(':')[0]
        name = head.split('.')[0]
        suffix = body[len(head):]
        if head == 'energyPrefix':
            return energy_word
        v = vars_.get(name)
        if not v:
            return '' if suffix else m.group(0)
        kind, val = charac(v)
        if kind == 'string':
            return str(val)
        if kind == 'unresolved' or val is None:
            return m.group(0)
        s = fmt_num(val)
        if suffix.startswith(':plural:'):          # {Cards:plural:card|cards}
            forms = suffix[len(':plural:'):].split('|')
            return forms[0] if abs(val) <= 1 else (forms[1] if len(forms) > 1 else forms[0])
        d = (deltas or {}).get(name)
        if d:
            if upgrade is True:
                s = fmt_num(val + d)
            elif upgrade is False:
                s = fmt_num(val)
            else:
                s = "%s（%s）" % (s, fmt_num(val + d))
        if suffix.startswith(':energyIcons'):
            return s + energy_join + energy_word
        return s

    t = re.sub(r'\{([^{}]*)\}', rep, t)
    return t if keep_tags else TAGS.sub('', t)


# ---------------- 覆盖表 ----------------
# 值依赖运行时状态、静态解析不出来时，在这里手写整行；None 表示不要这一条。
# 每种语言一份：中文照原文，英文按游戏英文文案口径写（只影响网页，不改 localization）。
_RELIC_OVERRIDE = {
    "zhs": {
        "IridescentDream": "拾起时，将你的初始遗物替换为先古版本，将一张初始卡牌从你的牌组中移除。",
        "BottledGalaxy": "拾起时，从你的牌组中选择3张牌移除。在每场战斗的前三回合，每回合开始时选择其中一张放入手牌。",
    },
    "eng": {
        "IridescentDream": "Upon pickup, replace your Starter Relic with its Ancient version and remove a Starter card from your Deck.",
        "BottledGalaxy": "Upon pickup, remove 3 cards from your Deck. For the first 3 turns of each combat, at the start of your turn, choose one to add to your hand.",
    },
}
_CARD_OVERRIDE = {
    "zhs": {
        # 卡面是运行时拼串、静态解析不出数值时手写正文（类型/费用仍从源码读，正文本体按升级前后各写一份）
        "IllusoryDreamWhisper": {
            "title": "幻梦无垠",
            "body": "获得7点格挡。\n造成7点伤害。\n将2张随机牌加入你的抽牌堆。\n每当你在休息处休息时，你可以随机变化这张牌上的数值。",
            "bodyUp": "获得10点格挡。\n造成10点伤害。\n将3张随机牌加入你的抽牌堆。\n每当你在休息处休息时，你可以随机变化这张牌上的数值。",
        },
        "YoukaiExtermination": {
            "title": "妖怪治退",
            "body": "造成6点伤害2次。\n斩杀时，获得1能量，这张牌在本局游戏中的伤害永久性增加1。",
            "bodyUp": "造成8点伤害2次。\n斩杀时，获得1能量，这张牌在本局游戏中的伤害永久性增加2。",
        },
        # {LeafText} 是运行时拼串，静态渲染会得到 None，只能手写正文
        "MysteriousLeaf": {
            "title": "迷之树叶",
            "body": "每当你将一张牌加入你的牌组时，此牌将伪装为那张牌。\n这也全都是妖怪干的吗？",
        },
    },
    "eng": {
        "IllusoryDreamWhisper": {
            "title": "Illusory Dream Boundless",
            "body": "Gain 7 Block.\nDeal 7 damage.\nAdd 2 random cards to your draw pile.\nWhenever you rest at a Rest Site, you may randomly change the values on this card.",
            "bodyUp": "Gain 10 Block.\nDeal 10 damage.\nAdd 3 random cards to your draw pile.\nWhenever you rest at a Rest Site, you may randomly change the values on this card.",
        },
        "YoukaiExtermination": {
            "title": "Youkai Extermination",
            "body": "Deal 6 damage 2 times.\nIf Fatal, gain 1 Energy and permanently increase this card's damage by 1.",
            "bodyUp": "Deal 8 damage 2 times.\nIf Fatal, gain 1 Energy and permanently increase this card's damage by 2.",
        },
        "MysteriousLeaf": {
            "title": "Mysterious Leaf",
            "body": "Whenever you add a card to your Deck, this card becomes a Disguise of that card.\nIs This All the Work of Youkai Too?",
        },
    },
}
_DERIVED_OVERRIDE = {
    "zhs": {
        "HeavyDiceRecordPower": "“老千”：已记录被你记录的牌。你的抽牌阶段结束后，你可以弃置一张手牌，将记录卡牌的复制品加入手牌，其拥有消耗、虚无。",
        "GlowingBambooPower": "竹林秘宝：被记录的牌将在战斗结束后升级。",
        "YuanChouPower": "怨仇：造成的伤害降低25%。玩家打出攻击牌时，受到等同于层数的伤害并使层数减少1。",
        "KeystonePower": "要石：打出技能牌时插下，打出攻击牌时拔出，对所有敌人造成等同于层数的伤害。",
        "PossessedBySeigaPower": "走火入魔：回合开始时，霍青娥接管你的回合：立即失去5点生命并获得1能量，随后按照费用从高到低、从左到右的顺序依次打出可打出的手牌。",
        "DetectiveManuscriptDexterityPower": None,
        "Tsukumogami": "付丧之力：获得虚无。当前计数X。计数首次降至0时，无论何处，将这张牌打出。在你的回合结束时，如果这张牌在本回合没有被打出，计数减少1。",
        "MedicinePoison": "蛊毒：获得永恒。额外给予X层中毒。回合结束时，如果这张牌在手中，本场战斗额外给予X层中毒。",
    },
    "eng": {
        "HeavyDiceRecordPower": '"Cardsharp": You have recorded cards. After your draw phase ends, you may discard a card from your hand to add a copy of the recorded card to your hand, with Exhaust and Ethereal.',
        "GlowingBambooPower": "Bamboo Forest Treasure: The recorded card will be Upgraded after combat ends.",
        "YuanChouPower": "Grudge: Deals 25% less damage to the source of this effect. Whenever the source plays an Attack, they take damage equal to this effect's amount and Grudge is reduced by 1.",
        "KeystonePower": "Keystone: Whenever you play a Skill, plant the Keystone. Whenever you play an Attack, pull it out and deal damage equal to this effect's amount to ALL enemies.",
        "PossessedBySeigaPower": "Demonic Possession: At the start of your turn, Seiga Kaku takes over your turn: lose 5 HP and gain 1 Energy, then play all playable cards in your hand from highest to lowest cost, left to right.",
        "DetectiveManuscriptDexterityPower": None,
        "Tsukumogami": "Tsukumogami: Gains Ethereal. Current count X. When the count first reaches 0, play this card wherever it is. At the end of your turn, if this card was not played this turn, reduce the count by 1.",
        "MedicinePoison": "Medicine Poison: Gains Eternal. Additionally applies X Poison. At the end of your turn, if this card is in your hand, additionally apply X Poison this combat.",
    },
}
_REST_OVERRIDE = {
    "zhs": {"OPTION_INK_DYED_CHERRY_BLOSSOMS": "恢复因墨染的樱花而失去的最大生命。"},
    "eng": {"OPTION_INK_DYED_CHERRY_BLOSSOMS": "Restore the Max HP you lost due to Ink-Dyed Cherry Blossoms."},
}
_EXTRA_LINES = {
    "zhs": {"MysteriousLeaf": ["伪装：此牌的耗能与伪装目标牌一致，打出此牌时打出伪装目标牌的复制。"]},
    "eng": {"MysteriousLeaf": ["Disguise: This card's cost matches the disguised card's cost. Playing it plays a copy of the disguised card."]},
}
_KEYWORD_LINES = {
    "zhs": {"HyakkiYagyoScroll": ["收录：查看来自指定角色的卡牌奖励，选择的卡牌将被加入所有百鬼夜行的收录中，若已存在该角色的收录则替换原先的收录牌。"]},
    "eng": {"HyakkiYagyoScroll": ["Collection: View card rewards from a chosen character. The selected card is added to every Hyakki Yagyo's Collection; if that character already has one, it is replaced."]},
}

# 当前语言生效的覆盖表（由 set_language 填充）
RELIC_OVERRIDE, CARD_OVERRIDE, DERIVED_OVERRIDE = {}, {}, {}
REST_OVERRIDE, EXTRA_LINES, KEYWORD_LINES = {}, {}, {}

CARD_TYPE_LABEL = {
    "zhs": {"Attack": "攻击", "Skill": "技能", "Power": "能力", "Curse": "诅咒", "Status": "状态", "Quest": "任务"},
    "eng": {"Attack": "Attack", "Skill": "Skill", "Power": "Power", "Curse": "Curse", "Status": "Status", "Quest": "Quest"},
}
ROW_LABEL = {
    "zhs": ["第一行", "第二行", "第三行", "第四行", "第五行", "第六行"],
    "eng": ["Row 1", "Row 2", "Row 3", "Row 4", "Row 5", "Row 6"],
}
CARD_TYPE_CN = dict(CARD_TYPE_LABEL["zhs"])
ROW_CN = list(ROW_LABEL["zhs"])
ENERGY_WORD = {"zhs": "能量", "eng": "Energy"}
COST_WORD = {"zhs": "耗能", "eng": "Cost"}
REST_SITE_WORD = {"zhs": "休息处", "eng": "Rest Site"}

# ---------------- 资源索引 ----------------
mod_card_files = {os.path.basename(p)[:-3]: p for p in glob.glob(os.path.join(ROOT, "scripts", "cards", "**", "*.cs"), recursive=True)}
mod_ench_files = {os.path.basename(p)[:-3]: p for p in glob.glob(os.path.join(ROOT, "scripts", "Enchantment", "*.cs"))}
mod_pow_files = {os.path.basename(p)[:-3]: p for p in glob.glob(os.path.join(ROOT, "scripts", "powers", "*.cs"))}
mod_pot_files = {os.path.basename(p)[:-3]: p for p in glob.glob(os.path.join(ROOT, "scripts", "potions", "**", "*.cs"), recursive=True)}
relic_files = {os.path.basename(p)[:-3]: p for p in glob.glob(os.path.join(ROOT, "scripts", "relics", "**", "*.cs"), recursive=True)}
mod_aff_files = {os.path.basename(p)[:-3] for p in glob.glob(os.path.join(ROOT, "scripts", "Afflictions", "*.cs"))}
# 原版卡牌源码：悬停提示里出现的原版牌（灼伤 / 蛇咬 / 贪婪…）要能列出类型费用与正文
BASE_CARD_DIR = os.path.join(STS2_SRC, "src", "Core", "Models", "Cards") if STS2_SRC else ""
base_card_files = {os.path.basename(p)[:-3]: p
                   for p in glob.glob(os.path.join(BASE_CARD_DIR, "**", "*.cs"), recursive=True)
                   if "Mocks" not in p} if BASE_CARD_DIR else {}

KW_CN, KW_DESC, BASE_KW_NAMES = {}, {}, set()
KW_DERIVED_SKIP = {"EXHAUST", "ETHEREAL", "RETAIN"}
BASE_AFF_TABLE, MOD_AFF_TABLE = {}, {}
AFFLICTION_BY_CLASS, MOD_AFFLICTION_CLASSES, BASE_AFFLICTION_CLASSES = {}, set(), set()
BASE_ENCH_NAMES = set()


def kw_cn(name):
    return KW_CN.get(snake(name), name)


def kw_desc(name):
    return KW_DESC.get(snake(name), "")


def kw_is_mod(name):
    return snake(name) not in BASE_KW_NAMES


def _affliction_table(table, prefix):
    out = {}
    for k, v in table.items():
        if not k.endswith(".title") or k.startswith("MOCK_"):
            continue
        cls = k[:-len(".title")][len(prefix):]
        out[snake(cls)] = (v, table.get(k[:-len(".title")] + ".description", ""))
    return out


def _fix_slash(path):
    """JSON 里的路径有时用 /，Windows 上也能读，无需处理；这里只用于说明。"""
    return path


def _rebuild_indices():
    """切语言后重建所有「本地化派生」的索引。"""
    global KW_CN, KW_DESC, BASE_KW_NAMES, BASE_ENCH_NAMES
    global BASE_AFF_TABLE, MOD_AFF_TABLE, AFFLICTION_BY_CLASS
    global MOD_AFFLICTION_CLASSES, BASE_AFFLICTION_CLASSES
    global _TITLE_INDEX, REST_OPTIONS

    KW_CN = {}
    for k, v in base_kw.items():
        if k.endswith(".title"):
            KW_CN[snake(k[:-6])] = v
    KW_DESC = {}
    for k, v in base_kw.items():
        if k.endswith(".description"):
            KW_DESC[snake(k[:-len(".description")])] = v
    for m in re.finditer(r'public\s+static\s+CardKeyword\s+(\w+)\s*;',
                         read(os.path.join(ROOT, "scripts", "keywords", "TouhouAncientKeywords.cs"))):
        key = "TOUHOUANCIENTS-" + m.group(1).upper()
        KW_CN[snake(m.group(1))] = mod_kw_loc.get(key + ".title", m.group(1))
        KW_DESC[snake(m.group(1))] = mod_kw_loc.get(key + ".description", "")
    BASE_KW_NAMES = {snake(k[:-6]) for k in base_kw if k.endswith(".title")}
    BASE_ENCH_NAMES = {snake(k[:-len(".title")]) for k in base_ench_loc if k.endswith(".title")}

    BASE_AFF_TABLE = _affliction_table(base_aff_loc, "")
    MOD_AFF_TABLE = _affliction_table(mod_aff_loc, "TOUHOUANCIENTS-")
    AFFLICTION_BY_CLASS = {**BASE_AFF_TABLE, **MOD_AFF_TABLE}
    MOD_AFFLICTION_CLASSES = set(MOD_AFF_TABLE) | {snake(c) for c in mod_aff_files}
    BASE_AFFLICTION_CLASSES = set(BASE_AFF_TABLE)

    _TITLE_INDEX = None
    _card_meta_cache.clear()
    REST_OPTIONS = {k[:-5]: v for k, v in rest_loc.items() if k.endswith(".name")}


def set_language(lang):
    """切换 mod + 本体本地化语言，并重建派生索引。lang ∈ {"zhs", "eng"}。"""
    if lang not in LANGUAGES:
        raise SystemExit("不支持的语言：%s（可选：%s）" % (lang, ", ".join(LANGUAGES)))
    global LANG, LOC, BASE_LOC
    global relics_loc, cards_loc, ench_loc, pow_loc, pot_loc
    global mod_kw_loc, rest_loc, ancients_loc
    global base_kw, base_ench_loc, base_pow_loc, base_cards_loc, chars_loc
    global mod_aff_loc, base_aff_loc
    global RELIC_OVERRIDE, CARD_OVERRIDE, DERIVED_OVERRIDE
    global REST_OVERRIDE, EXTRA_LINES, KEYWORD_LINES
    global CARD_TYPE_CN, ROW_CN

    LANG = lang
    LOC = os.path.join(ROOT, "TouhouAncients", "localization", lang)
    BASE_LOC = os.path.join(STS2_SRC, "localization", lang) if STS2_SRC else None

    relics_loc = load("relics.json")
    cards_loc = load("cards.json")
    ench_loc = load("enchantments.json")
    pow_loc = load("powers.json")
    pot_loc = load("potions.json")
    mod_kw_loc = load("card_keywords.json")
    rest_loc = load("rest_site_ui.json")
    ancients_loc = load("ancients.json")
    mod_aff_loc = load("afflictions.json")
    base_kw = load("card_keywords.json", True)
    base_ench_loc = load("enchantments.json", True)
    base_pow_loc = load("powers.json", True)
    base_cards_loc = load("cards.json", True)
    base_aff_loc = load("afflictions.json", True)
    chars_loc = load("characters.json", True)

    RELIC_OVERRIDE = dict(_RELIC_OVERRIDE[lang])
    CARD_OVERRIDE = {k: dict(v) for k, v in _CARD_OVERRIDE[lang].items()}
    DERIVED_OVERRIDE = dict(_DERIVED_OVERRIDE[lang])
    REST_OVERRIDE = dict(_REST_OVERRIDE[lang])
    EXTRA_LINES = dict(_EXTRA_LINES[lang])
    KEYWORD_LINES = dict(_KEYWORD_LINES[lang])
    CARD_TYPE_CN = dict(CARD_TYPE_LABEL[lang])
    ROW_CN = list(ROW_LABEL[lang])

    if not BASE_LOC:
        print("警告：.agents/local.md 里没有 sts2-src，原版关键词/卡牌名会退化成英文原名。", file=sys.stderr)
    _rebuild_indices()


# ---------------- 各模型 → 一行衍生说明 ----------------
_card_meta_cache = {}


def card_meta(cls, path=None):
    """path 传原版卡牌源码时也能解析（原版和 mod 可能有同名类）。"""
    path = path or mod_card_files[cls]
    if path in _card_meta_cache:
        return _card_meta_cache[path]
    src = strip_comments(read(path))
    cs = symtab(src)
    cost, ctype = None, None
    m = re.search(r'const\s+int\s+energyCost\s*=\s*([^;]+);', src)
    if m:
        try:
            cost = int(eval_expr(m.group(1), cs))
        except Exception:
            pass
    m = re.search(r'const\s+CardType\s+type\s*=\s*CardType\.(\w+)', src)
    if m:
        ctype = m.group(1)
    if cost is None or ctype is None:            # 有些卡把费用/类型直接写在 : base(...) 里
        m = re.search(r':\s*base\s*\(([^)]*)\)', src)
        if m:
            parts = [x.strip() for x in m.group(1).split(',')]
            if cost is None and parts:
                try:
                    cost = int(eval_expr(parts[0], cs))
                except Exception:
                    pass
            if ctype is None:
                tm = re.search(r'CardType\.(\w+)', m.group(1))
                if tm:
                    ctype = tm.group(1)
    if re.search(r'HasEnergyCostX\s*=>\s*true', src):
        cost = 'X'
    m = re.search(r'MaxUpgradeLevel\s*=>\s*(-?\d+)', src)
    maxup = int(m.group(1)) if m else 1
    m = re.search(r'EnergyCost\.UpgradeBy\(\s*(-?\d+)\s*\)', src)   # 升级减少费用
    cost_delta = int(m.group(1)) if m else 0
    deltas, kws = {}, []
    ob = body_after(src, r'protected\s+override\s+void\s+OnUpgrade\s*\(\s*\)')
    if ob:
        for mm in re.finditer(r'DynamicVars(?:\.(\w+)|\["(\w+)"\])\.UpgradeValueBy\(([^)]+)\)', ob):
            nm = mm.group(1) or mm.group(2)
            try:
                deltas[nm] = eval_expr(mm.group(3), cs)
            except Exception:
                pass
        for mm in re.finditer(r'(Add|Remove)Keyword\((?:CardKeyword|TouhouAncientKeywords)\.(\w+)\)', ob):
            kws.append((mm.group(1) == 'Add', mm.group(2)))
    body = arrow_body(src, r'CanonicalKeywords\s*=>')
    canonical = re.findall(r'(?:CardKeyword|TouhouAncientKeywords)\.(\w+)', body or "")
    # 无意识/预见这类是运行时 AddKeyword 挂上去的，CanonicalKeywords 里没有，全文再扫一遍补上
    extra = [k for k in dict.fromkeys(re.findall(r'(?:CardKeyword|TouhouAncientKeywords)\.(\w+)', src))
             if k not in canonical]
    _card_meta_cache[path] = {"cost": cost, "type": ctype, "maxup": maxup, "deltas": deltas,
                              "cost_delta": cost_delta, "kwdelta": kws, "keywords": canonical,
                              "extra_keywords": extra}
    return _card_meta_cache[path]


def card_entry(cls):
    key = "TOUHOUANCIENTS-" + snake(cls)
    meta = card_meta(cls)
    if cls in CARD_OVERRIDE:
        title, text = CARD_OVERRIDE[cls]["title"], CARD_OVERRIDE[cls]["body"]
    else:
        title = cards_loc.get(key + ".title", cls)
        v = parse_vars(strip_comments(read(mod_card_files[cls])))
        text = render(cards_loc.get(key + ".description", ""), v, meta["deltas"])
        if '{' in text:                          # 卡面是运行时字符串时退回 descriptionIdle
            alt = render(cards_loc.get(key + ".descriptionIdle", ""), v, meta["deltas"])
            if alt and '{' not in alt:
                text = alt
    if meta["cost"] == 'X':
        coststr = "耗能X"
    elif meta["cost"] is None or meta["cost"] == -1 or (meta["cost"] == 0 and meta["type"] == "Curse"):
        coststr = ""
    else:
        coststr = "耗能%d" % meta["cost"]
    head = title + "：" + (CARD_TYPE_CN.get(meta["type"], meta["type"] or "")) + "。"
    if coststr:
        head += coststr + "。"
    if meta["keywords"]:
        head += "、".join(kw_cn(k) for k in meta["keywords"]) + "。"
    if meta["kwdelta"]:
        head += "（" + "、".join(kw_cn(k) + ("+" if a else "-") for a, k in meta["kwdelta"]) + "）"
    return head + text.replace("\n", "")


def enchant_entry(cls):
    key = "TOUHOUANCIENTS-" + snake(cls)
    return ench_loc.get(key + ".title", cls) + "：" + render(
        ench_loc.get(key + ".description", ""), parse_vars(strip_comments(read(mod_ench_files[cls])))).replace("\n", "")


def power_entry(cls):
    key = "TOUHOUANCIENTS-" + snake(cls)
    title = pow_loc.get(key + ".title", "")
    if not title:
        return None
    v = parse_vars(strip_comments(read(mod_pow_files[cls])))
    for field in (".smartDescription", ".description"):     # smart 版算不出来就用普通版
        raw = pow_loc.get(key + field)
        if not raw:
            continue
        out = render(raw, v).replace("\n", "")
        if '{' not in out:
            return title + "：" + out
    return None


def potion_entry(cls, relic_vars):
    key = "TOUHOUANCIENTS-" + snake(cls)
    title = pot_loc.get(key + ".title", "")
    if not title:
        return None
    v = dict(relic_vars or {})
    v.update(parse_vars(strip_comments(read(mod_pot_files[cls]))))
    return title + "：" + render(pot_loc.get(key + ".description", ""), v).replace("\n", "")


# ---------------- 解析先古之民的选项池 ----------------
def parse_ancients():
    out = []
    for path in sorted(glob.glob(os.path.join(ROOT, "scripts", "ancients", "*.cs"))):
        name = os.path.basename(path)[:-3]
        if name == "TouhouAncientBase":
            continue
        src = strip_comments(read(path))
        named = {}
        for m in re.finditer(r'(?:private|protected|public)[^\n=]*?\b(\w+)\s*=>\s*CreateTARelicOptionPool\(', src):
            named[m.group(1)] = re.findall(r'TARelicOption<(\w+)>', src[m.end():match_bracket(src, m.end(), '(', ')')])
        pools = []
        m = re.search(r'TARelicOptionPools\s*=>\s*\[', src)
        if m:
            lb = m.end() - 1
            block = src[lb + 1:match_bracket(src, lb + 1)]
            # 两种写法：(命名池, N) 与 (CreateTARelicOptionPool(...), N)，N 缺省为 1
            for m2 in re.finditer(r'(CreateTARelicOptionPool\()|(\(\s*(\w+)\s*,\s*(\d+)\s*\))', block):
                if m2.group(1):
                    inner = block[m2.end():match_bracket(block, m2.end(), '(', ')')]
                    tail = block[m2.end() + len(inner):m2.end() + len(inner) + 8]
                    tm = re.match(r'\s*,\s*(\d+)\s*\)', tail)
                    pools.append({"relics": re.findall(r'TARelicOption<(\w+)>', inner),
                                  "count": int(tm.group(1)) if tm else 1})
                else:
                    pools.append({"relics": named.get(m2.group(3), []), "count": int(m2.group(4))})
        aid = snake(re.sub(r'Ancient$', '', name))
        key = "TOUHOUANCIENTS-" + aid + "_ANCIENT"
        # ShowAct == null 表示不进第一幕、二/三幕通用（见 TouhouAncientBase.IsValidForAct）
        m3 = re.search(r'ShowAct\s*=>\s*(null|\d+)', src)
        show_act = None if (not m3 or m3.group(1) == 'null') else int(m3.group(1))
        out.append({"file": name, "key": key, "title": ancients_loc.get(key + ".title", name),
                    "epithet": ancients_loc.get(key + ".epithet", ""), "showAct": show_act,
                    "dialogues": parse_dialogues(key), "pools": pools})
    return out


def parse_dialogues(key):
    """通用对话 talk.ANY：按 N 分段，段内按 M 排序，多句一段（网页分行显示）。"""
    seg = {}
    for k, val in ancients_loc.items():
        m = re.match(re.escape(key) + r'\.talk\.ANY\.(\d+)-(\d+)(r?)\.ancient$', k)
        if m:
            speaker = ancients_loc.get(k[:-len(".ancient")] + ".speaker", "")
            seg.setdefault(int(m.group(1)), []).append(
                (int(m.group(2)), m.group(3), {"text": val, "speaker": speaker}))
    return [{"index": i, "lines": [line for _, _, line in sorted(seg[i], key=lambda x: (x[0], x[1]))]}
            for i in sorted(seg)]




_TITLE_INDEX = None


def title_index():
    """名称 -> (kind, 类名)，用于把文案里带色标的名字映射回模型。"""
    global _TITLE_INDEX
    if _TITLE_INDEX is None:
        idx = {}
        for table, kind in ((mod_card_files, 'card'), (mod_ench_files, 'ench'),
                            (mod_pow_files, 'power'), (mod_pot_files, 'potion')):
            for k in table:
                idx.setdefault(model_title(k, kind), (kind, k))
        _TITLE_INDEX = idx
    return _TITLE_INDEX


# 代码泛型名字有歧义时（例：mod 附魔 Greed  vs 原版诅咒牌 Greed）在这里显式指定
REF_OVERRIDE = {
    ("SinisterPactDream", "Greed"): ("base_card", "Greed"),
}


HOVER_KIND = {
    "Affliction": "affliction",
    "Card": "card", "CardWithCardHoverTips": "card",
    "Enchantment": "ench",
    "Power": "power", "PowerWithPowerHoverTips": "power",
    "Potion": "potion",
}
HOVER_TABLES = {"power": mod_pow_files, "potion": mod_pot_files}


def hover_refs(src):
    """以代码的 HoverTip 为准。

    文案里的名字会骗人（饕餮尤魔的「吞噬遗物」是动词，不是苦痛「吞噬」），所以苦痛一律只看代码：
    `FromAffliction<T>` / `ModelDb.Affliction<T>()` / `CardCmd.Afflict<T>`。
    原版卡牌与附魔也照列（网页端不显示卡图），原版能力/药水不列，避免噪音。
    """
    out = []
    for m in re.finditer(r'HoverTipFactory\.From(\w+)\s*<\s*([\w\.]+)\s*>', src):
        kind = HOVER_KIND.get(m.group(1))
        if not kind:
            continue
        name = m.group(2).split('.')[-1]
        if kind == "affliction":
            if snake(name) in MOD_AFFLICTION_CLASSES or snake(name) in BASE_AFFLICTION_CLASSES:
                out.append(("affliction", name))
        elif kind == "card":
            if name in mod_card_files:
                out.append(("card", name))
            elif name in base_card_files:
                out.append(("base_card", name))
        elif kind == "ench":
            if name in mod_ench_files:
                out.append(("ench", name))
            elif snake(name) in BASE_ENCH_NAMES:
                out.append(("base_ench", name))
        elif name in HOVER_TABLES[kind]:
            out.append((kind, name))
    for m in re.finditer(r'ModelDb\.Affliction<\s*([\w\.]+)\s*>\(\)', src):
        out.append(("affliction", m.group(1).split('.')[-1]))
    for m in re.finditer(r'CardCmd\.Afflict<\s*([\w\.]+)\s*>', src):
        out.append(("affliction", m.group(1).split('.')[-1]))
    for m in re.finditer(r'HoverTipFactory\.FromKeyword\s*\(\s*(?:CardKeyword|TouhouAncientKeywords)\.(\w+)\s*\)', src):
        out.append(("keyword", m.group(1)))
    return out


def relic_refs(src, raw, owner=None):
    """遗物引用的模型：代码泛型 + 代码 HoverTip + 文案色标名字，按出现顺序去重。"""
    refs = []
    for m in re.finditer(r'<\s*([A-Z]\w*)\s*>', src):
        t = m.group(1)
        if owner is not None and (owner, t) in REF_OVERRIDE:
            refs.append(REF_OVERRIDE[(owner, t)])
        elif t in mod_card_files:
            refs.append(("card", t))
        elif t in mod_ench_files:
            refs.append(("ench", t))
        elif t in mod_pow_files:
            refs.append(("power", t))
        elif t in mod_pot_files:
            refs.append(("potion", t))
    refs.extend(hover_refs(src))
    idx = title_index()
    for tag in re.findall(r'\[(?:gold|purple|aqua|pink|red|green|blue|orange)\]([^\[]+?)\[/', raw):
        if tag in idx:
            refs.append(idx[tag])
    seen, out = set(), []
    for r in refs:
        if r in seen:
            continue
        seen.add(r)
        out.append(r)
    return out


def strip_unresolved(text):
    """侵蚀等文案里的 {Amount} 在本条上下文取不到，直接写成 X，别把花括号漏给读者。"""
    return re.sub(r'\{[^{}]*\}', 'X', text)


def base_card_entry(cls):
    """原版卡牌：正文取本体 localization（有些诅咒/状态牌没有正文，返回 None）。"""
    if cls not in base_card_files:
        return None
    key = snake(cls)
    title = base_cards_loc.get(key + ".title", "").strip() or cls
    meta = card_meta(cls, base_card_files[cls])
    raw = base_cards_loc.get(key + ".description", "") or ""
    if raw.lower() == "none":
        raw = ""
    text = strip_unresolved(render(raw, parse_vars(strip_comments(read(base_card_files[cls]))), upgrade=False))
    return title, text


def base_enchant_entry(cls):
    key = snake(cls)
    title = base_ench_loc.get(key + ".title", "").strip()
    if not title:
        return None
    desc = base_ench_loc.get(key + ".description", "") or ""
    return title, strip_unresolved(render(desc, {}, upgrade=False))


def meta_line(meta):
    """类型 · 费用（按当前语言）。原版卡牌与 mod 卡牌共用。"""
    sep = "" if LANG == "zhs" else " "
    word = COST_WORD.get(LANG, "耗能")
    if meta["cost"] == "X":
        cost = word + sep + "X"
    elif meta["cost"] is None or meta["cost"] == -1:
        cost = ""
    else:
        cost = word + sep + str(meta["cost"])
    parts = [CARD_TYPE_CN.get(meta["type"], meta["type"] or "")]
    if cost:
        parts.append(cost)
    return " · ".join(p for p in parts if p)


def base_card_detail(cls):
    """原版卡牌的 wiki 条目：类型/费用/关键词从本体源码读，正文取本体 localization（可能为空）。"""
    hit = base_card_entry(cls)
    if not hit:
        return None
    title, text = hit
    meta = card_meta(cls, base_card_files[cls])
    return {"name": title, "html": text, "meta": meta_line(meta),
            "keywords": [kw_cn(k) for k in meta["keywords"]]}


def hover_arg(src, cls):
    """苦痛悬停里的数值参数：`FromAffliction<X>(arg)` 或 `CardCmd.Afflict<X>(card, arg)` 的最后一个实参。"""
    m = re.search(r'FromAffliction<\s*%s\s*>\s*\(([^()]*)\)' % re.escape(cls), src)
    if m and m.group(1).strip():
        return m.group(1).strip()
    m = re.search(r'CardCmd\.Afflict<\s*%s\s*>\s*\(([^()]*)\)' % re.escape(cls), src)
    if m:
        parts = [x.strip() for x in m.group(1).split(',') if x.strip()]
        if parts:
            return parts[-1]
    return None


def resolve_amount(expr, vars_):
    """把 GalvanizedDamage / base.DynamicVars["X"].IntValue / base.DynamicVars.Energy.IntValue / 1m 解成数字。"""
    if not expr:
        return None
    expr = expr.strip()
    m = re.fullmatch(r'base\.DynamicVars(?:\["(\w+)"\]|\.(\w+))\.(?:IntValue|BaseValue)', expr)
    if m:
        name = m.group(1) or m.group(2)
        v = (vars_ or {}).get(name)
        return v[1] if v and v[0] == 'num' else None
    v = (vars_ or {}).get(expr)
    if v and v[0] == 'num':
        return v[1]
    try:
        return float(expr.rstrip('m'))
    except ValueError:
        return None


def ref_entry(kind, name, vars_, src=None):
    """侵蚀 / 原版卡牌 / 原版附魔这类没有 mod 模型的引用 → (标题, 正文)；取不到返回 None。"""
    if kind == 'affliction':
        title, desc = AFFLICTION_BY_CLASS.get(snake(name), (None, ""))
        if not title:
            return None
        # {Amount} 只认悬停/施加调用里的数值：遗物自己的同名 Amount 常是另一个数（例：伪灵异珠 Amount=2、沉重=1）
        amount = resolve_amount(hover_arg(src or "", name), vars_)
        v2 = {'Amount': ('num', amount, '')} if amount is not None else {}
        return title, strip_unresolved(render(desc, v2)).replace("\n", "")
    if kind == 'base_card':
        return base_card_entry(name)
    if kind == 'base_ench':
        return base_enchant_entry(name)
    return None


def derived_entries(src, raw, vars_, owner=None):
    """[(kind, 类名/标题, 一行说明)]；覆盖表里为 None 的条目不返回。"""
    out = []
    for kind, t in relic_refs(src, raw, owner):
        if kind in ('affliction', 'base_card', 'base_ench'):
            hit = ref_entry(kind, t, vars_, src)
            if hit:
                out.append((kind, t, hit[0] + "：" + hit[1]))
            continue
        if t in DERIVED_OVERRIDE:
            if DERIVED_OVERRIDE[t]:
                out.append((kind, t, DERIVED_OVERRIDE[t]))
            continue
        if kind == 'card':
            e = card_entry(t)
        elif kind == 'ench':
            e = enchant_entry(t)
        elif kind == 'potion':
            e = potion_entry(t, vars_)
        else:
            e = power_entry(t)
        if e:
            out.append((kind, t, e))
    return out


def build_ancient(a):
    pools, idx = [], 0
    for pool_info in a["pools"]:
        cnt = pool_info["count"]
        label = ROW_CN[idx] if cnt == 1 else "/".join(ROW_CN[idx:idx + cnt])
        idx += cnt
        relics = []
        for c in pool_info["relics"]:
            key = "TOUHOUANCIENTS-" + snake(c)
            raw = relics_loc.get(key + ".description", "")
            src = strip_comments(read(relic_files[c]))
            v = runtime_vars(src, parse_vars(src))
            desc = RELIC_OVERRIDE.get(c) or render(raw, v).replace("\n", "")
            lines = [text for _, _, text in derived_entries(src, raw, v, c)]
            for opt_key, opt_name in REST_OPTIONS.items():     # 休息处选项
                if opt_name in desc and "休息处" in desc:
                    body = render(rest_loc[opt_key + ".description"], v).replace("\n", "")
                    if "{" in body:
                        body = REST_OVERRIDE.get(opt_key, body)
                    lines.append(opt_name + "：" + body)
            lines.extend(KEYWORD_LINES.get(c, []))
            for kind, t in relic_refs(src, raw, c):
                lines.extend(EXTRA_LINES.get(t, []))
            lines.extend(EXTRA_LINES.get(c, []))
            relics.append({"title": relics_loc.get(key + ".title", c), "desc": desc, "derived": lines})
        pools.append({"label": label, "relics": relics})
    return {"title": a["title"], "epithet": a["epithet"], "pools": pools}


def to_markdown(ancients):
    """层级：Tab + '-'。单池角色不标行，多池用 ROW_CN 标签（同池占多行时写 第一行/第二行）。"""
    lines = []
    for a in ancients:
        lines.append("# " + a["title"])
        lines.append("")
        lines.append("> " + (a["epithet"] or ""))
        lines.append("")
        multi = len(a["pools"]) > 1
        for pool in a["pools"]:
            base = 1
            if multi:
                lines.append("\t" * 0 + "- " + pool["label"])
                base = 2
            for r in pool["relics"]:
                pad = "\t" * (base - 1)
                lines.append(pad + "- " + r["title"])
                if r["desc"].strip():
                    lines.append(pad + "\t- " + r["desc"])
                for d in r["derived"]:
                    if d.strip():
                        lines.append(pad + "\t- " + d)
    return "\n".join(lines) + "\n"


def main():
    ap = argparse.ArgumentParser(description="导出先古之民遗物 markdown")
    ap.add_argument("ancient", nargs="?", help="先古之民：中文名片段 / 类名 / 文件名；留空=全部")
    ap.add_argument("--out", help="写到文件；缺省打印到 stdout")
    ap.add_argument("--list", action="store_true", help="只列出所有先古之民")
    ap.add_argument("--lang", default="zhs", choices=list(LANGUAGES), help="本地化语言（默认 zhs）")
    args = ap.parse_args()

    set_language(args.lang)

    all_ancients = parse_ancients()
    if args.list:
        for a in all_ancients:
            print("%s\t%s\t%s" % (a["file"], a["title"], a["epithet"]))
        return 0

    picked = all_ancients
    if args.ancient:
        needle = args.ancient
        picked = [a for a in all_ancients
                  if needle in a["title"] or needle.lower() in a["file"].lower()]
        if not picked:
            print("没匹配到先古之民：%s（用 --list 看可选值）" % needle, file=sys.stderr)
            return 2

    text = to_markdown([build_ancient(a) for a in picked])
    if args.out:
        path = args.out if os.path.isabs(args.out) else os.path.join(ROOT, args.out)
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        print("已写入 %s（%d 行）" % (path, len(text.splitlines())))
    else:
        sys.stdout.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
