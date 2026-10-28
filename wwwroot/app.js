// CRC 在线计算前端逻辑 —— 复刻 ip33.com/crc.html

const $ = (id) => document.getElementById(id);

let models = [];

async function init() {
    try {
        const res = await fetch('/api/models');
        models = await res.json();
    } catch (e) {
        showError('无法加载参数模型列表：' + e.message);
        return;
    }

    const sel = $('model');
    sel.innerHTML = '';
    // 按位宽分组：自定义 / 4~7 位 / 8 位 / 16 位 / 32 位
    const groups = new Map();
    models.forEach((m, i) => {
        const g = m.name === '自定义' ? '自定义'
            : m.width <= 7 ? '4~7 位'
            : m.width + ' 位';
        if (!groups.has(g)) {
            const og = document.createElement('optgroup');
            og.label = g;
            sel.appendChild(og);
            groups.set(g, og);
        }
        const opt = document.createElement('option');
        opt.value = String(i);
        opt.textContent = m.name;
        groups.get(g).appendChild(opt);
    });

    sel.addEventListener('change', onModelChange);
    renderModelsTable();
    initModelSearch();
    $('calc').addEventListener('click', compute);

    // 输入变化即实时计算
    ['data', 'width', 'poly', 'init', 'xorout'].forEach(id =>
        $(id).addEventListener('input', compute));
    ['refin', 'refout'].forEach(id =>
        $(id).addEventListener('change', compute));
    // 特殊校验复选框：默认全部勾选（init 末尾 compute() 会触发首轮填充）
    ['ck-sum', 'ck-sumh', 'ck-bcc', 'ck-lrc'].forEach(id => {
        $(id).checked = true;
    });
    ['ck-sum', 'ck-sumh', 'ck-bcc', 'ck-lrc'].forEach(id =>
        $(id).addEventListener('change', compute));
    document.querySelectorAll('input[name="inputType"]')
        .forEach(r => r.addEventListener('change', onInputTypeChange));

    // 默认选中 CRC-16/MODBUS（index 经查找）
    const idx = models.findIndex(m => m.name === 'CRC-16/MODBUS');
    if (idx >= 0) {
        sel.value = String(idx);
        onModelChange();
        $('model-search').value = models[idx].name;
    }

    compute();
    initCopy();
}

// 一键复制（整帧 = 输入数据 + 校验值）：Hex 结果按钮 / 大小端徽章 / 特殊校验结果单元格
function initCopy() {
    const live = $('copy-live');

    document.querySelectorAll('.copy-btn').forEach(btn => {
        btn.setAttribute('title', '复制整帧：数据 + 校验值（校验按大端序）');
        btn.addEventListener('click', () => {
            let text;
            if (btn.dataset.copy === 'hex') {
                // 整帧 = 数据 + 校验字节；Hex 输入为 xx xx 字节对，可直接粘贴到串口工具发送
                text = buildFrame('be');
            } else {
                const target = $(btn.dataset.copy);
                text = target ? target.textContent.trim() : '';
            }
            if (!text) return;
            copyText(text, () => {
                btn.textContent = '已复制';
                btn.classList.add('copied');
                if (live) live.textContent = '已复制整帧（数据+校验）到剪贴板';
                showToast('已复制整帧：' + text);
                setTimeout(() => {
                    btn.textContent = '复制';
                    btn.classList.remove('copied');
                }, 1500);
            });
        });
    });

    // 大端 / 小端徽章：点击复制对应字节序的整帧（小端 = 低字节在前，MODBUS 等）
    [['be', '大端序'], ['le', '小端序（低字节在前，如 MODBUS）']].forEach(([id, tip]) => {
        const el = $(id);
        if (!el) return;
        el.setAttribute('title', '点击复制整帧：数据 + 校验（' + tip + '）');
        el.setAttribute('tabindex', '0');
        el.setAttribute('role', 'button');
        const copyBadge = () => {
            const hexVal = ($('hex').textContent || '').trim();
            if (!hexVal || hexVal === '—') return;   // 尚无计算结果
            const frame = buildFrame(id);
            copyText(frame, () => {
                el.classList.remove('copied-flash');
                void el.offsetWidth;
                el.classList.add('copied-flash');
                if (live) live.textContent = '已复制整帧（数据+校验）到剪贴板';
                showToast('已复制整帧（' + (id === 'be' ? '大端序' : '小端序') + '）：' + frame);
            });
        };
        el.addEventListener('click', copyBadge);
        el.addEventListener('keydown', e => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                copyBadge();
            }
        });
    });

    document.querySelectorAll('.special-val').forEach(td => {
        td.setAttribute('title', '点击复制整帧（数据 + 校验值）');
        td.setAttribute('tabindex', '0');
        td.setAttribute('role', 'button');
        td.setAttribute('aria-label', '复制数据与该校验值');

        const copyCell = () => {
            const val = td.textContent.trim();
            if (!val || val === '—') return;
            const frame = buildSpecialFrame(val);
            const row = td.closest('tr');
            const label = row && row.cells[1] ? row.cells[1].textContent.trim() : '';
            copyText(frame, () => {
                td.classList.remove('copied-flash');
                void td.offsetWidth;
                td.classList.add('copied-flash');
                if (live) live.textContent = '已复制（数据+校验值）到剪贴板';
                showToast('已复制整帧（' + label + '）：' + frame);
            });
        };
        td.addEventListener('click', copyCell);
        td.addEventListener('keydown', e => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                copyCell();
            }
        });
    });
}

function copyText(text, onDone) {
    const done = () => onDone && onDone();
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(text).then(done).catch(() => fallbackCopy(text, done));
    } else {
        fallbackCopy(text, done);
    }
}

function fallbackCopy(text, done) {
    const ta = document.createElement('textarea');
    ta.value = text;
    ta.style.position = 'fixed';
    ta.style.opacity = '0';
    document.body.appendChild(ta);
    ta.select();
    try {
        document.execCommand('copy');
        done();
    } catch (e) { }
    document.body.removeChild(ta);
}

// 复制成功提示（toast）：单例复用，1.6s 后自动淡出
let toastTimer = null;
function showToast(msg) {
    let toast = document.getElementById('copy-toast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'copy-toast';
        toast.setAttribute('role', 'status');
        document.body.appendChild(toast);
    }
    toast.textContent = msg;
    toast.classList.remove('show');
    void toast.offsetWidth;   // 重启动画
    toast.classList.add('show');
    if (toastTimer) clearTimeout(toastTimer);
    toastTimer = setTimeout(() => toast.classList.remove('show'), 1600);
}

// ===== 整帧复制：内容 = 输入数据 + 校验值，可直接粘贴到串口工具发送 =====

// 当前输入模式
function currentInputType() {
    const r = document.querySelector('input[name="inputType"]:checked');
    return r ? r.value : 'hex';
}

// 数据部分：Hex 模式规范化为两位大写字节对（单空格分隔）；Ascii 模式保留原文本
function dataPart() {
    const raw = $('data').value.trim();
    if (currentInputType() !== 'hex') return raw;
    const clean = raw.replace(/[^0-9A-Fa-f]/g, '');
    const even = clean.length % 2 === 0 ? clean : clean.slice(0, -1);
    const pairs = [];
    for (let i = 0; i < even.length; i += 2) {
        pairs.push(even.substr(i, 2).toUpperCase());
    }
    return pairs.join(' ');
}

// 校验值 hex -> 字节对数组（奇数位左补 0）；order='le' 时反序（低字节在前）
function crcBytes(hexVal, order) {
    let h = (hexVal || '').replace(/[^0-9A-Fa-f]/g, '').toUpperCase();
    if (!h) return [];
    if (h.length % 2 !== 0) h = '0' + h;
    const pairs = h.match(/.{2}/g) || [];
    return order === 'le' ? pairs.reverse() : pairs;
}

// CRC 整帧：数据 + 校验字节（be=大端原序 / le=小端反序）
function buildFrame(order) {
    const hexVal = ($('hex').textContent || '').trim();
    const data = dataPart();
    if (!hexVal || hexVal === '—') return data;   // 尚无结果：退化为规范化后的数据
    if (currentInputType() === 'hex') {
        return [data, crcBytes(hexVal, order).join(' ')].filter(Boolean).join(' ');
    }
    const h = hexVal.replace(/[^0-9A-Fa-f]/g, '').toUpperCase();
    return [data, h].filter(Boolean).join(' ');   // Ascii 模式：原文本 + 校验值 hex
}

// 特殊校验整帧：数据 + 单字节校验值
function buildSpecialFrame(value) {
    let byte = (value || '').replace(/[^0-9A-Fa-f]/g, '').toUpperCase();
    if (!byte) return dataPart();
    if (byte.length % 2 !== 0) byte = '0' + byte;
    return [dataPart(), byte].filter(Boolean).join(' ');
}

function onModelChange() {
    const m = models[Number($('model').value)];
    if (!m || m.name === '自定义') return; // 自定义：保留手动输入
    $('width').value = m.width;
    $('poly').value = m.polyHex;
    $('init').value = m.initHex;
    $('xorout').value = m.xorOutHex;
    $('refin').checked = m.refIn;
    $('refout').checked = m.refOut;
    compute();
}

function showError(msg) {
    $('error').textContent = msg || '';
}

// 由多项式数值与宽度推导代数公式，如 x16 + x15 + x2 + 1
function polyToFormula(poly, width) {
    const terms = ['x' + width];
    for (let i = width - 1; i >= 0; i--) {
        if ((poly >> i) & 1) terms.push(i === 0 ? '1' : (i === 1 ? 'x' : 'x' + i));
    }
    return terms.join(' + ');
}

// 渲染“常见 CRC 参数模型”表格（跳过“自定义”）
function renderModelsTable() {
    const tbody = document.querySelector('#models-table tbody');
    tbody.innerHTML = '';
    models.forEach(m => {
        if (m.name === '自定义') return;
        const tr = document.createElement('tr');
        tr.innerHTML =
            `<td>${m.name}</td>` +
            `<td class="mono">${polyToFormula(m.poly, m.width)}</td>` +
            `<td>${m.width}</td>` +
            `<td class="mono">${m.polyHex}</td>` +
            `<td class="mono">${m.initHex}</td>` +
            `<td class="mono">${m.xorOutHex}</td>` +
            `<td>${m.refIn ? 'true' : 'false'}</td>` +
            `<td>${m.refOut ? 'true' : 'false'}</td>` +
            `<td class="usage">${m.usage || ''}</td>`;
        tr.addEventListener('click', () => {
            $('width').value = m.width;
            $('poly').value = m.polyHex;
            $('init').value = m.initHex;
            $('xorout').value = m.xorOutHex;
            $('refin').checked = m.refIn;
            $('refout').checked = m.refOut;
            // 同步下拉框选中项与查询框显示
            const idx = models.indexOf(m);
            if (idx >= 0) $('model').value = String(idx);
            $('model-search').value = m.name;
            compute();
            // 回填反馈：输入卡片闪一下 + 屏幕阅读器播报
            const inputCard = document.querySelector('.card');
            inputCard.classList.remove('flash');
            void inputCard.offsetWidth;
            inputCard.classList.add('flash');
            const live = $('copy-live');
            if (live) live.textContent = '已填入 ' + m.name + ' 参数';
        });
        tbody.appendChild(tr);
    });
}

async function compute() {
    showError('');
    const inputType = document.querySelector('input[name="inputType"]:checked').value;
    const payload = {
        inputType,
        data: $('data').value,
        width: parseInt($('width').value, 10),
        poly: $('poly').value.trim(),
        init: $('init').value.trim(),
        xorOut: $('xorout').value.trim(),
        refIn: $('refin').checked,
        refOut: $('refout').checked
    };

    try {
        const res = await fetch('/api/crc', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const body = await res.json();
        if (!res.ok) {
            showError(body.error || '计算失败');
            $('hex').textContent = '—';
            $('bin').textContent = '—';
            $('be').textContent = '—';
            $('le').textContent = '—';
            return;
        }
        $('hex').textContent = body.hex;
        $('bin').textContent = body.bin;
        const { be, le } = byteOrders(body.hex);
        $('be').textContent = be || '—';
        $('le').textContent = le || '—';
    } catch (e) {
        showError('请求出错：' + e.message);
    }

    await computeSpecial();
}

// 特殊校验值单元格写入：有值移除空态样式，无值显示占位符并加空态样式
function setSpecialVal(td, value) {
    if (value == null || value === '') {
        td.textContent = '—';
        td.classList.add('val-empty');
    } else {
        td.textContent = value;
        td.classList.remove('val-empty');
    }
}

// 特殊校验（SUM/BCC/LRC）：表格形式，勾选「启用」后结果填入对应行
async function computeSpecial() {
    const anyOn = $('ck-sum').checked || $('ck-sumh').checked ||
        $('ck-bcc').checked || $('ck-lrc').checked;
    const blank = () => {
        setSpecialVal($('sum'), null);
        setSpecialVal($('sumh'), null);
        setSpecialVal($('bcc'), null);
        setSpecialVal($('lrc'), null);
    };
    if (!anyOn) { blank(); return; }

    const inputType = document.querySelector('input[name="inputType"]:checked').value;
    try {
        const res = await fetch('/api/checksum', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ inputType, data: $('data').value })
        });
        const body = await res.json();
        if (!res.ok) { blank(); return; }
        setSpecialVal($('sum'), $('ck-sum').checked ? body.sum : null);
        setSpecialVal($('sumh'), $('ck-sumh').checked ? body.sumHigh : null);
        setSpecialVal($('bcc'), $('ck-bcc').checked ? body.bcc : null);
        setSpecialVal($('lrc'), $('ck-lrc').checked ? body.lrc : null);
    } catch {
        blank();
    }
}

// 切换 Hex/Ascii 时，把文本框内容在两种表示之间互转，使显示始终符合所选模式
function onInputTypeChange() {
    const newType = document.querySelector('input[name="inputType"]:checked').value;
    const cur = $('data').value;
    $('data').value = newType === 'hex' ? asciiToHex(cur) : hexToAscii(cur);
    compute();
}

// "1234" -> "31 32 33 34"（按 UTF-8 取字节，空输入返回空）
function asciiToHex(str) {
    if (!str) return '';
    const bytes = new TextEncoder().encode(str);
    return Array.from(bytes)
        .map(b => b.toString(16).padStart(2, '0').toUpperCase())
        .join(' ');
}

// "31 32 33 34" -> "1234"（仅取合法十六进制字节，忽略分隔符/非法字符）
function hexToAscii(str) {
    const clean = (str || '').replace(/[^0-9A-Fa-f]/g, '');
    if (clean.length === 0) return '';
    const even = clean.length % 2 === 0 ? clean : clean.slice(0, -1);
    const bytes = [];
    for (let i = 0; i < even.length; i += 2) {
        bytes.push(parseInt(even.substr(i, 2), 16));
    }
    try {
        return new TextDecoder('latin1').decode(new Uint8Array(bytes));
    } catch {
        return '';
    }
}

// 把结果 hex 按字节拆分，给出大端（原序）/ 小端（字节反序）两种字节序列
function byteOrders(hex) {
    let h = hex || '';
    if (h.length % 2 !== 0) h = '0' + h;
    const pairs = h.match(/.{2}/g) || [];
    return {
        be: pairs.join(' '),
        le: pairs.slice().reverse().join(' ')
    };
}

// ===== 参数模型模糊查询（组合框：输入过滤 + 键盘上下选择）=====
let comboItems = [];   // 当前列表项 [{index, model, score}]
let comboActive = -1;  // 键盘高亮项下标

// 模糊打分：原始名连续子串 > 压缩名子串（忽略 - / 空格等分隔符）> 子序列；-1 = 不匹配
function fuzzyScore(query, name) {
    const q = query.toLowerCase();
    const n = name.toLowerCase();
    if (!q) return 0;
    const i = n.indexOf(q);
    if (i >= 0) return 1000 - i;               // 越靠前分越高
    const compact = n.replace(/[^a-z0-9\u4e00-\u9fa5]/g, '');
    const qc = q.replace(/[^a-z0-9\u4e00-\u9fa5]/g, '');
    if (!qc) return -1;
    const j = compact.indexOf(qc);
    if (j >= 0) return 800 - j;                // "crc16modbus" 可命中 "CRC-16/MODBUS"
    let pos = -1;
    for (const ch of qc) {                     // 子序列：按顺序出现即可
        pos = compact.indexOf(ch, pos + 1);
        if (pos < 0) return -1;
    }
    return 1;
}

function filterModels(query) {
    const q = (query || '').trim().toLowerCase();
    if (!q) return models.map((m, index) => ({ index, model: m }));
    const hits = [];
    models.forEach((m, index) => {
        const s = fuzzyScore(q, m.name);
        if (s >= 0) hits.push({ index, model: m, score: s });
    });
    hits.sort((a, b) => b.score - a.score || a.index - b.index);
    return hits;
}

function renderModelList() {
    const input = $('model-search');
    const list = $('model-list');
    comboItems = filterModels(input.value);
    comboActive = comboItems.length ? 0 : -1;
    list.innerHTML = '';
    if (!comboItems.length) {
        list.innerHTML = '<div class="combo-empty">没有匹配的模型</div>';
    } else {
        comboItems.forEach((it, i) => {
            const div = document.createElement('div');
            div.className = 'combo-item' + (i === comboActive ? ' active' : '');
            div.setAttribute('role', 'option');
            const name = document.createElement('span');
            name.className = 'combo-name';
            name.textContent = it.model.name;
            const w = document.createElement('span');
            w.className = 'combo-w';
            w.textContent = it.model.width + ' 位';
            div.appendChild(name);
            div.appendChild(w);
            div.addEventListener('pointerdown', e => e.preventDefault()); // 防止输入框先失焦
            div.addEventListener('click', () => pickModel(it.index));
            list.appendChild(div);
        });
    }
    list.hidden = false;
    input.setAttribute('aria-expanded', 'true');
}

function updateActive() {
    document.querySelectorAll('#model-list .combo-item').forEach((el, i) => {
        el.classList.toggle('active', i === comboActive);
        if (i === comboActive) el.scrollIntoView({ block: 'nearest' });
    });
}

function currentModelName() {
    const m = models[Number($('model').value)];
    return m ? m.name : '';
}

function closeModelList(restore) {
    $('model-list').hidden = true;
    $('model-search').setAttribute('aria-expanded', 'false');
    comboActive = -1;
    if (restore) $('model-search').value = currentModelName();
}

function pickModel(index) {
    const sel = $('model');
    sel.value = String(index);
    sel.dispatchEvent(new Event('change'));    // 复用原有 onModelChange 填参 + 计算
    $('model-search').value = models[index].name;
    closeModelList(false);
    $('model-search').blur();
}

function initModelSearch() {
    const input = $('model-search');
    input.setAttribute('role', 'combobox');
    input.setAttribute('aria-autocomplete', 'list');
    input.setAttribute('aria-controls', 'model-list');
    input.setAttribute('aria-expanded', 'false');

    input.addEventListener('input', renderModelList);
    input.addEventListener('focus', renderModelList);
    input.addEventListener('keydown', e => {
        const open = !$('model-list').hidden;
        if (!open && (e.key === 'ArrowDown' || e.key === 'ArrowUp')) {
            renderModelList();
            return;
        }
        if (!open) return;
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            comboActive = Math.min(comboActive + 1, comboItems.length - 1);
            updateActive();
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            comboActive = Math.max(comboActive - 1, 0);
            updateActive();
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (comboActive >= 0 && comboItems[comboActive]) {
                pickModel(comboItems[comboActive].index);
            }
        } else if (e.key === 'Escape') {
            closeModelList(true);
        } else if (e.key === 'Tab') {
            closeModelList(false);
        }
    });

    // 点击组合框以外区域：关闭下拉并还原为当前选中模型名
    document.addEventListener('pointerdown', e => {
        if (!e.target.closest('#model-combo') && !$('model-list').hidden) {
            closeModelList(true);
        }
    });
}

init();
