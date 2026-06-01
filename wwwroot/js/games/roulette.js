'use strict';

// ─── Koło europejskiej ruletki ────────────────────────────────────────────────

const WHEEL_ORDER = [
    0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13,
    36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14,
    31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26
];

const RED_NUMBERS = new Set([1,3,5,7,9,12,14,16,18,19,21,23,25,27,30,32,34,36]);

function getNumberColor(n) {
    if (n === 0) return 'green';
    return RED_NUMBERS.has(n) ? 'red' : 'black';
}

// ─── Pasek animacji ───────────────────────────────────────────────────────────

const REPS      = 12;
const CELL_SIZE = 64;   // 60px szerokość + 4px gap
const ANIM_MS   = 4000;
const START_REP = 2;
const END_REP   = 8;

let stripInner = null;

function buildStrip() {
    stripInner = document.getElementById('roulette-strip-inner');
    if (!stripInner) return;
    for (let r = 0; r < REPS; r++) {
        WHEEL_ORDER.forEach(n => {
            const c = document.createElement('div');
            c.className = `roulette-strip-cell ${getNumberColor(n)}`;
            c.textContent = n;
            stripInner.appendChild(c);
        });
    }
    setStripPosition(4 * 37, false);
}

function setStripPosition(cellIndex, animate) {
    if (!stripInner) return;
    const wrap = document.getElementById('roulette-strip-wrap');
    const halfW = wrap ? wrap.offsetWidth / 2 : 300;
    const tx = halfW - (cellIndex * CELL_SIZE + CELL_SIZE / 2);
    stripInner.style.transition = animate
        ? `transform ${ANIM_MS}ms cubic-bezier(0.12,0.82,0.18,1.0)`
        : 'none';
    stripInner.style.transform = `translateX(${tx}px)`;
}

function animateToNumber(number) {
    return new Promise(resolve => {
        if (!stripInner) { setTimeout(resolve, 500); return; }
        const wi = WHEEL_ORDER.indexOf(number);
        setStripPosition(START_REP * 37 + wi, false);
        void stripInner.offsetWidth;
        stripInner.querySelectorAll('.strip-winner').forEach(el => el.classList.remove('strip-winner'));
        const endIdx = END_REP * 37 + wi;
        setStripPosition(endIdx, true);
        setTimeout(() => {
            const cells = stripInner.querySelectorAll('.roulette-strip-cell');
            if (cells[endIdx]) cells[endIdx].classList.add('strip-winner');
            resolve();
        }, ANIM_MS + 80);
    });
}

// ─── Układ stołu ──────────────────────────────────────────────────────────────
// Kolumna c 0..11, rząd r 0=góra(3) 1=środek(2) 2=dół(1)
// numAt(c,r) = c*3 + (3-r)

function numAt(c, r) { return c * 3 + (3 - r); }

function sortedKey(nums) {
    return [...nums].sort((a, b) => a - b).join('-');
}

// ─── Zakłady ──────────────────────────────────────────────────────────────────

// key → { betType, betValue, label, element, amount }
const selectedBets = new Map();

let spinning = false;

const spinBtn   = document.getElementById('roulette-spin');
const statusEl  = document.getElementById('roulette-status');
const winEl     = document.getElementById('roulette-win');
const detailsEl = document.getElementById('roulette-result-details');
const basketEl  = document.getElementById('roulette-basket');
const totalEl   = document.getElementById('roulette-basket-total');
const historyEl = document.getElementById('roulette-history');
const betInput  = document.getElementById('roulette-bet');
const balanceEl = document.getElementById('balance-display');

function getCurrentBet() {
    const v = parseFloat(betInput.value);
    return (!isNaN(v) && v > 0) ? v : 0;
}

function getBetLabel(betType, betValue) {
    const labels = {
        red: 'Czerwone (1:1)', black: 'Czarne (1:1)',
        odd: 'Nieparzyste (1:1)', even: 'Parzyste (1:1)',
        low: '1–18 (1:1)', high: '19–36 (1:1)',
        dozen1: '1–12 (2:1)', dozen2: '13–24 (2:1)', dozen3: '25–36 (2:1)',
        column1: 'Kolumna 1 (2:1)', column2: 'Kolumna 2 (2:1)', column3: 'Kolumna 3 (2:1)'
    };
    if (labels[betType]) return labels[betType];
    switch (betType) {
        case 'number':  return `Numer ${betValue}`;
        case 'split':   return `Split ${betValue} (×18)`;
        case 'street':  return `Street ${betValue} (×12)`;
        case 'corner':  return `Narożnik ${betValue} (×9)`;
        case 'sixline': return `Six-line ${betValue} (×6)`;
        default:        return betType;
    }
}

function addBet(element, amount) {
    if (spinning || !element || !element.dataset.bet) return;
    if (amount <= 0) return;
    const betType  = element.dataset.bet;
    const betValue = element.dataset.value ?? '';
    const key      = `${betType}:${betValue}`;

    if (selectedBets.has(key)) {
        selectedBets.get(key).amount += amount;
    } else {
        selectedBets.set(key, {
            betType, betValue,
            label:   getBetLabel(betType, betValue),
            element, amount
        });
        element.classList.add('selected');
    }
    renderBasket();
    spinBtn.disabled = false;
}

function removeBet(key) {
    const entry = selectedBets.get(key);
    if (entry) {
        entry.element?.classList.remove('selected');
        selectedBets.delete(key);
    }
    renderBasket();
    spinBtn.disabled = selectedBets.size === 0;
}

function clearAllBets() {
    selectedBets.forEach(b => b.element?.classList.remove('selected'));
    selectedBets.clear();
    renderBasket();
    spinBtn.disabled = true;
}

function handleClick(element) {
    const bet = getCurrentBet();
    if (bet <= 0) { statusEl.textContent = 'Podaj stawkę większą od 0.'; return; }
    addBet(element, bet);
}

// Kliknięcia na komórki stołu (numery, outside bety)
document.getElementById('roulette-table').addEventListener('click', e => {
    const cell = e.target.closest('[data-bet]');
    if (!cell || !cell.dataset.bet || cell.classList.contains('roulette-marker')) return;
    handleClick(cell);
});

// ─── Koszyk ───────────────────────────────────────────────────────────────────

function renderBasket() {
    if (selectedBets.size === 0) {
        basketEl.innerHTML = '<span class="text-secondary small">Kliknij na stole aby dodać zakłady</span>';
        totalEl.textContent = '';
        return;
    }

    basketEl.innerHTML = [...selectedBets.entries()].map(([key, b]) =>
        `<span class="roulette-bet-chip">
            <span class="chip-label">${b.label}</span>
            <span class="chip-amount">${b.amount.toLocaleString('pl-PL', {maximumFractionDigits:2})} PLN</span>
            <button class="roulette-chip-remove" data-key="${key}">&times;</button>
         </span>`
    ).join('');

    basketEl.querySelectorAll('.roulette-chip-remove').forEach(btn =>
        btn.addEventListener('click', () => removeBet(btn.dataset.key))
    );

    const total = [...selectedBets.values()].reduce((s, b) => s + b.amount, 0);
    const count = selectedBets.size;
    const suffix = count === 1 ? '' : count < 5 ? 'y' : 'ów';
    totalEl.textContent = `Łączna stawka: ${total.toFixed(2)} PLN (${count} zakład${suffix})`;
}

betInput.addEventListener('input', renderBasket);
document.getElementById('roulette-clear').addEventListener('click', clearAllBets);

// Szybkie żetony
document.querySelectorAll('.roulette-quick-chip').forEach(btn =>
    btn.addEventListener('click', () => { betInput.value = btn.dataset.amount; renderBasket(); })
);

// ─── Spin ─────────────────────────────────────────────────────────────────────

spinBtn.addEventListener('click', async () => {
    if (spinning || selectedBets.size === 0) return;
    const bet = getCurrentBet();
    if (bet <= 0) { statusEl.textContent = 'Podaj poprawną stawkę.'; return; }

    spinning = true;
    spinBtn.classList.add('roulette-spinning');
    spinBtn.disabled = true;
    statusEl.textContent = 'Losowanie...';
    winEl.textContent   = '';
    detailsEl.innerHTML = '';

    try {
        const bets = [...selectedBets.values()].map(b => ({
            betType:  b.betType,
            betValue: b.betValue,
            bet:      b.amount
        }));

        const res  = await fetch('/api/roulette/spin', {
            method:  'POST',
            headers: { 'Content-Type': 'application/json' },
            body:    JSON.stringify({ bets })
        });
        const data = await res.json();

        if (!res.ok) {
            statusEl.textContent = data.error ?? 'Błąd serwera.';
            if (data.balance != null) updateBalance(data.balance);
            return;
        }

        await animateToNumber(data.number);
        highlightTableResult(data.number);
        highlightWinningMarkers(data.number);
        addToHistory(data.number, data.color);
        updateBalance(data.balance);
        showResults(data);

    } catch (err) {
        statusEl.textContent = 'Błąd połączenia.';
        console.error(err);
    } finally {
        spinning = false;
        spinBtn.classList.remove('roulette-spinning');
        spinBtn.disabled = selectedBets.size === 0;
    }
});

// ─── Wyniki ───────────────────────────────────────────────────────────────────

function showResults(data) {
    const colorPL = { red: 'czerwone', black: 'czarne', green: 'zielone' }[data.color] ?? data.color;
    statusEl.textContent = `Wypadło: ${data.number} (${colorPL})`;

    const totalWin = data.totalWin ?? 0;
    if (totalWin > 0) {
        winEl.style.color = '#d4af37';
        winEl.textContent = `+${Number(totalWin).toFixed(2)} PLN`;
    } else {
        winEl.style.color = '#ef4444';
        const totalBet = (data.results ?? []).reduce((s, r) => s + r.bet, 0);
        winEl.textContent = `-${Number(totalBet).toFixed(2)} PLN`;
    }

    detailsEl.innerHTML = '';
    (data.results ?? []).forEach(r => {
        const row = document.createElement('div');
        row.className = `roulette-result-row ${r.won ? 'won' : 'lost'}`;
        const label  = getBetLabel(r.betType, r.betValue);
        const amtStr = r.won
            ? `+${Number(r.win).toFixed(2)} PLN`
            : `-${Number(r.bet).toFixed(2)} PLN`;
        row.innerHTML = `<span>${r.won ? '✓' : '✗'} ${label}</span>
                         <span class="roulette-result-amount">${amtStr}</span>`;
        detailsEl.appendChild(row);
    });
}

function highlightTableResult(number) {
    document.querySelectorAll('.result-flash').forEach(el => el.classList.remove('result-flash'));
    const cell = document.querySelector(`.roulette-table [data-bet="number"][data-value="${number}"]`);
    if (!cell) return;
    void cell.offsetWidth;
    cell.classList.add('result-flash');
    setTimeout(() => cell.classList.remove('result-flash'), 1600);
}

function highlightWinningMarkers(number) {
    document.querySelectorAll('.roulette-marker').forEach(m => {
        m.classList.remove('marker-win');
        const nums = (m.dataset.value ?? '').split('-').map(Number);
        if (nums.includes(number)) {
            m.classList.add('marker-win');
            setTimeout(() => m.classList.remove('marker-win'), 2000);
        }
    });
}

function addToHistory(number, color) {
    if (!historyEl) return;
    const chip = document.createElement('span');
    chip.className = `roulette-chip ${color}`;
    chip.textContent = number;
    historyEl.prepend(chip);
    const chips = historyEl.querySelectorAll('.roulette-chip');
    if (chips.length > 20) chips[chips.length - 1].remove();
}

function updateBalance(balance) {
    if (balanceEl && balance != null) {
        balanceEl.textContent = parseFloat(balance).toFixed(2);
    }
}

// ─── Budowanie markerów splitów / narożników / streetów ──────────────────────

function buildMarkers() {
    const container = document.getElementById('roulette-table-container');
    if (!container) return;

    container.querySelectorAll('.roulette-marker').forEach(m => m.remove());

    const contRect = container.getBoundingClientRect();
    if (contRect.width === 0) { setTimeout(buildMarkers, 100); return; }

    const numCell = {};
    document.querySelectorAll('.roulette-num[data-value]').forEach(el => {
        numCell[parseInt(el.dataset.value)] = el;
    });
    const zeroCell = document.querySelector('.roulette-zero');
    if (!numCell[1]) return;

    function cx(el) {
        const r = el.getBoundingClientRect();
        return r.left - contRect.left + r.width / 2;
    }
    function cy(el) {
        const r = el.getBoundingClientRect();
        return r.top - contRect.top + r.height / 2;
    }
    function rightOf(el) { return el.getBoundingClientRect().right - contRect.left; }
    function leftOf(el)  { return el.getBoundingClientRect().left  - contRect.left; }

    function makeMarker(x, y, betType, betValue, cssClass) {
        const m = document.createElement('div');
        m.className   = `roulette-marker ${cssClass}`;
        m.dataset.bet   = betType;
        m.dataset.value = betValue;
        m.style.left    = x + 'px';
        m.style.top     = y + 'px';
        m.title         = getBetLabel(betType, betValue);
        m.addEventListener('click', e => { e.stopPropagation(); handleClick(m); });
        container.appendChild(m);
        return m;
    }

    // Splity poziome (sąsiednie kolumny, ten sam rząd)
    for (let col = 0; col < 11; col++) {
        for (let row = 0; row < 3; row++) {
            const a = numAt(col, row), b = numAt(col + 1, row);
            const x = (cx(numCell[a]) + cx(numCell[b])) / 2;
            const y = (cy(numCell[a]) + cy(numCell[b])) / 2;
            makeMarker(x, y, 'split', sortedKey([a, b]), 'marker-split');
        }
    }

    // Splity pionowe (ten sam słupek, sąsiednie rzędy)
    for (let col = 0; col < 12; col++) {
        for (let row = 0; row < 2; row++) {
            const a = numAt(col, row), b = numAt(col, row + 1);
            const x = (cx(numCell[a]) + cx(numCell[b])) / 2;
            const y = (cy(numCell[a]) + cy(numCell[b])) / 2;
            makeMarker(x, y, 'split', sortedKey([a, b]), 'marker-split');
        }
    }

    // Narożniki (4 sąsiednie komórki)
    for (let col = 0; col < 11; col++) {
        for (let row = 0; row < 2; row++) {
            const n = [numAt(col,row), numAt(col,row+1), numAt(col+1,row), numAt(col+1,row+1)];
            const x = (cx(numCell[n[0]]) + cx(numCell[n[3]])) / 2;
            const y = (cy(numCell[n[0]]) + cy(numCell[n[3]])) / 2;
            makeMarker(x, y, 'corner', sortedKey(n), 'marker-corner');
        }
    }

    // Six-lines (2 sąsiednie streety, 6 numerów) — między kolumnami
    for (let col = 0; col < 11; col++) {
        const nums = [
            numAt(col,0), numAt(col,1), numAt(col,2),
            numAt(col+1,0), numAt(col+1,1), numAt(col+1,2)
        ];
        const x = (cx(numCell[nums[0]]) + cx(numCell[nums[3]])) / 2;
        const y = (cy(numCell[nums[0]]) + cy(numCell[nums[2]])) / 2;
        makeMarker(x, y, 'sixline', sortedKey(nums), 'marker-sixline');
    }

    // Streets (3 numery w pionie) — na lewej krawędzi każdej grupy
    for (let col = 0; col < 12; col++) {
        const n1 = numAt(col,0), n2 = numAt(col,1), n3 = numAt(col,2);
        const x = leftOf(numCell[n1]) - 8;
        const y = (cy(numCell[n1]) + cy(numCell[n3])) / 2;
        makeMarker(x, y, 'street', sortedKey([n1,n2,n3]), 'marker-street');
    }

    // Splity z zerem (0-1, 0-2, 0-3) + tria (0-1-2, 0-2-3)
    if (zeroCell) {
        const midX = (rightOf(zeroCell) + leftOf(numCell[3])) / 2;
        [[0,1],[0,2],[0,3]].forEach(([a,b]) => {
            makeMarker(midX, cy(numCell[b]), 'split', `${a}-${b}`, 'marker-split');
        });
        makeMarker(midX, (cy(numCell[1]) + cy(numCell[2])) / 2, 'street', '0-1-2', 'marker-street');
        makeMarker(midX, (cy(numCell[2]) + cy(numCell[3])) / 2, 'street', '0-2-3', 'marker-street');
    }

    // Przywróć stan .selected dla istniejących zakładów po rebuild
    selectedBets.forEach((entry, key) => {
        const betType  = entry.betType;
        const betValue = entry.betValue;
        const el = container.querySelector(`.roulette-marker[data-bet="${betType}"][data-value="${betValue}"]`)
                || document.querySelector(`[data-bet="${betType}"][data-value="${betValue}"]`);
        if (el) {
            entry.element = el;
            el.classList.add('selected');
        }
    });
}

// ─── Zakłady seryjne ──────────────────────────────────────────────────────────

function findMarker(betType, betValue) {
    return document.querySelector(
        `.roulette-marker[data-bet="${betType}"][data-value="${betValue}"]`
    );
}
function findCell(betType, betValue) {
    return document.querySelector(`[data-bet="${betType}"][data-value="${betValue}"]`);
}

// Voisins du Zéro — 9 żetonów
// 0-2-3 trio (2), corner 25-26-28-29 (2), splity: 4-7, 12-15, 18-21, 19-22, 32-35 (po 1)
document.getElementById('btn-voisins')?.addEventListener('click', () => {
    const chip = getCurrentBet();
    if (chip <= 0) { statusEl.textContent = 'Podaj stawkę.'; return; }

    const trio = findMarker('street', '0-2-3');
    if (trio) addBet(trio, chip * 2);

    ['4-7','12-15','18-21','19-22','32-35'].forEach(bv => {
        const el = findMarker('split', bv);
        if (el) addBet(el, chip);
    });

    const corn = findMarker('corner', '25-26-28-29');
    if (corn) addBet(corn, chip * 2);
});

// Tiers du Cylindre — 6 żetonów
// Splity: 5-8, 10-11, 13-16, 23-24, 27-30, 33-36
document.getElementById('btn-tiers')?.addEventListener('click', () => {
    const chip = getCurrentBet();
    if (chip <= 0) { statusEl.textContent = 'Podaj stawkę.'; return; }

    ['5-8','10-11','13-16','23-24','27-30','33-36'].forEach(bv => {
        const el = findMarker('split', bv);
        if (el) addBet(el, chip);
    });
});

// Orphelins — 5 żetonów
// Numer 1 (1), splity: 6-9, 14-17, 17-20, 31-34 (po 1)
document.getElementById('btn-orphelins')?.addEventListener('click', () => {
    const chip = getCurrentBet();
    if (chip <= 0) { statusEl.textContent = 'Podaj stawkę.'; return; }

    const el1 = findCell('number', '1');
    if (el1) addBet(el1, chip);

    ['6-9','14-17','17-20','31-34'].forEach(bv => {
        const el = findMarker('split', bv);
        if (el) addBet(el, chip);
    });
});

// ─── Init ─────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    buildStrip();
    // Dwa rAF żeby tabela zdążyła się wyrenderować przed obliczeniami pozycji
    requestAnimationFrame(() => requestAnimationFrame(buildMarkers));
});

let resizeTimer = null;
window.addEventListener('resize', () => {
    clearTimeout(resizeTimer);
    resizeTimer = setTimeout(buildMarkers, 200);
});
