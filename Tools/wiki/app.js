// 东方先古之民 · 遗物图鉴：数据全来自 data.js（由 Tools/build_wiki.py 生成），此处只做渲染。
(function () {
  'use strict';

  var DATA = window.THA_DATA || { ancients: [] };
  var app = document.getElementById('app');
  var modal = document.getElementById('modal');
  var modalBody = document.getElementById('modal-body');
  var filterBox = document.getElementById('filter');
  var FALLBACK_ICON = (DATA.assetPrefix || '') + 'icon/relics/default.png';

  function el(tag, cls, text) {
    var n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    return n;
  }

  // 图标缺失时回落，避免破图
  function img(src, cls, alt) {
    var i = document.createElement('img');
    i.loading = 'lazy';
    i.alt = alt || '';
    if (cls) i.className = cls;
    i.src = src;
    i.addEventListener('error', function () {
      if (i.src.indexOf('default.png') === -1) i.src = FALLBACK_ICON;
    });
    return i;
  }

  function totalRelics(a) {
    return a.pools.reduce(function (n, p) { return n + p.relics.length; }, 0);
  }

  // 搜索索引：名字/称号/层数/遗物名与说明，全部去掉空白 —— 输入「雾雨魔理沙」也能命中「雾雨 魔理沙」
  function squash(s) { return (s || '').replace(/\s+/g, '').toLowerCase(); }

  var SEARCH = {};
  DATA.ancients.forEach(function (a) {
    var parts = [a.name, a.epithet, a.showActLabel];
    a.pools.forEach(function (p) {
      p.relics.forEach(function (r) { parts.push(r.name, r.plain); });
    });
    SEARCH[a.id] = squash(parts.join(' '));
  });

  // 首页与筛选结果共用同一张卡
  function ancientCard(a) {
    var card = el('div', 'ancient-card');
    card.appendChild(img(a.avatar, '', a.name));
    card.appendChild(el('div', 'name', a.name));
    card.appendChild(el('div', 'epithet', a.epithet || ''));
    if (a.showActLabel) card.appendChild(el('div', 'tier', a.showActLabel));
    card.appendChild(el('div', 'count', totalRelics(a) + ' 件遗物'));
    card.addEventListener('click', function () { openAncient(a.id); });
    return card;
  }

  // ---------- 首页 ----------
  function renderHome() {
    app.innerHTML = '';
    var grid = el('div', 'grid');
    DATA.ancients.forEach(function (a) { grid.appendChild(ancientCard(a)); });
    app.appendChild(grid);
    if (!DATA.ancients.length) app.appendChild(el('div', 'empty', '没有数据，请先运行 Tools/build_wiki.py。'));
  }

  // ---------- 先古详情 ----------
  function renderAncient(id) {
    var a = DATA.ancients.filter(function (x) { return x.id === id; })[0];
    if (!a) return renderHome();
    app.innerHTML = '';

    var back = el('button', 'back', '← 返回全部先古之民');
    back.addEventListener('click', function () { location.hash = ''; renderHome(); });
    app.appendChild(back);

    var head = el('div', 'ancient-head');
    head.appendChild(img(a.mapNode, 'node', a.name + ' 节点图'));
    var info = el('div');
    var nameRow = el('div', 'name-row');
    nameRow.appendChild(el('h2', null, a.name));
    nameRow.appendChild(img(a.avatar, 'avatar', a.name));
    info.appendChild(nameRow);
    info.appendChild(el('div', 'epithet', a.epithet || ''));
    info.appendChild(el('div', 'meta', [a.showActLabel, totalRelics(a) + ' 件遗物']
      .filter(Boolean).join(' · ')));
    var dialog = pickDialogue(a.dialogues);
    if (dialog) {
      var box = el('div', 'dialog');
      dialog.forEach(function (line) {
        var row = el('div', 'dialog-line');
        if (line.avatar) row.appendChild(img(line.avatar, 'speaker', ''));
        var text = el('div', 'dialog-text');
        text.innerHTML = line.html;
        row.appendChild(text);
        box.appendChild(row);
      });
      info.appendChild(box);
    }
    head.appendChild(info);
    app.appendChild(head);

    a.pools.forEach(function (pool) {
      if (pool.label) app.appendChild(el('div', 'pool-label', pool.label));
      var list = el('div', 'relics');
      pool.relics.forEach(function (r) {
        var row = el('div', 'relic' + (r.derived.length ? ' has-derived' : ''));
        row.appendChild(img(r.icon, '', r.name));
        var body = el('div', 'body');
        var title = el('div', 'title');
        title.appendChild(el('span', null, r.name));
        if (r.derived.length) title.appendChild(el('span', 'badge', '衍生 ' + r.derived.length));
        body.appendChild(title);
        var text = el('div', 'text');
        text.innerHTML = r.html;
        body.appendChild(text);
        if (r.flavor) {
          var flavor = el('div', 'flavor');
          flavor.innerHTML = r.flavor;
          body.appendChild(flavor);
        }
        row.appendChild(body);
        if (r.derived.length) {
          row.addEventListener('click', function () { openRelic(a, r); });
          row.title = '点击查看衍生内容';
        }
        list.appendChild(row);
      });
      app.appendChild(list);
    });
  }

  // 通用对话：每次进入页面随机取一段；段内多句各自一行，女苑/紫苑带说话者差分头像
  function pickDialogue(dialogues) {
    if (!dialogues || !dialogues.length) return null;
    return dialogues[Math.floor(Math.random() * dialogues.length)];
  }

  // ---------- 衍生内容弹层 ----------
  function derivedItem(d, list, r) {
    var box = el('div', 'derived');
    var art = el('div', 'art');
    if (d.icon) art.appendChild(img(d.icon, d.kind === 'card' ? '' : 'icon-only', d.name));
    box.appendChild(art);
    var info = el('div', 'info');
    var head = el('div', 'dhead');
    head.appendChild(el('div', 'dname', d.name));
    if (d.htmlUp) {
      var label = el('label', 'up-toggle');
      var cb = document.createElement('input');
      cb.type = 'checkbox';
      cb.checked = !!d.up;                // 每张牌各自记忆升级前后
      cb.addEventListener('change', function () {
        d.up = cb.checked;
        fillDerived(list, r);
      });
      label.appendChild(cb);
      label.appendChild(document.createTextNode('显示升级后'));
      head.appendChild(label);
    }
    info.appendChild(head);
    var meta = (d.up && d.metaUp) ? d.metaUp : d.meta;
    if (meta) {
      var mline = el('div', 'dmeta');
      mline.innerHTML = meta;
      info.appendChild(mline);
    }
    var kws = (d.up && d.keywordsUp) ? d.keywordsUp : d.keywords;
    if (kws && kws.length) {
      var kbox = el('div', 'kws');
      kws.forEach(function (k) { kbox.appendChild(el('span', 'kw', k)); });
      info.appendChild(kbox);
    }
    var text = el('div', 'dtext');
    text.innerHTML = (d.up && d.htmlUp) ? d.htmlUp : d.html;
    info.appendChild(text);
    box.appendChild(info);
    return box;
  }

  function fillDerived(list, r) {
    list.innerHTML = '';
    r.derived.forEach(function (d) { list.appendChild(derivedItem(d, list, r)); });
  }

  function openRelic(a, r) {
    modalBody.innerHTML = '';
    modalBody.appendChild(el('h3', null, r.name));
    var desc = el('div', 'relic-desc');
    desc.innerHTML = r.html;
    modalBody.appendChild(desc);
    if (r.flavor) {
      var mflavor = el('div', 'relic-flavor');
      mflavor.innerHTML = r.flavor;
      modalBody.appendChild(mflavor);
    }

    var list = el('div', 'derived-list');
    modalBody.appendChild(list);
    fillDerived(list, r);
    modal.hidden = false;
    document.body.style.overflow = 'hidden';
  }

  function closeModal() {
    modal.hidden = true;
    modalBody.innerHTML = '';
    document.body.style.overflow = '';
  }

  modal.addEventListener('click', function (e) {
    if (e.target.hasAttribute('data-close')) closeModal();
  });
  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeModal();
  });

  // ---------- 路由与筛选 ----------
  function route() {
    var m = /^#\/ancient\/(.+)$/.exec(location.hash);
    if (m) renderAncient(decodeURIComponent(m[1]));
    else renderHome();
    window.scrollTo(0, 0);
  }

  function openAncient(id) { location.hash = '#/ancient/' + encodeURIComponent(id); }

  filterBox.addEventListener('input', function () {
    var q = squash(filterBox.value);
    // 清 hash 但不能触发 hashchange，否则 route() 会立刻把筛选结果覆盖回首页
    if (location.hash) history.replaceState(null, '', location.pathname + location.search);
    app.innerHTML = '';
    if (!q) return renderHome();
    var grid = el('div', 'grid');
    var hits = 0;
    DATA.ancients.forEach(function (a) {
      if (SEARCH[a.id].indexOf(q) === -1) return;
      hits++;
      grid.appendChild(ancientCard(a));
    });
    app.appendChild(grid);
    if (!hits) app.appendChild(el('div', 'empty', '没有匹配的先古之民或遗物。'));
  });

  document.getElementById('foot').innerHTML =
    '原作：杀戮尖塔2 / 东方project<br>模组作者：阿迪　画师：河城绿茶';
  window.addEventListener('hashchange', route);
  route();
})();
