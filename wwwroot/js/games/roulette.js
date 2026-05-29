'use strict';

// ─── Stałe ruletki europejskiej ───────────────────────────────────────────────

// Kolejność numerów na kole (zgodnie z ruchem wskazówek zegara)
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

// ─── Animacja paska ────────────────────────────────────────────────────────────

const REPS      = 12;        // liczba powtórzeń koła na pasku
const CELL_SIZE = 64;        // szerokość komórki + odstęp (60 + 4 px)
const ANIM_MS   = 4000;      // czas animacji [ms]
const START_REP = 2;         // od której repetycji startujemy przed spiniem (0-indexed)
const END_REP   = 8;         // w której repetycji lądujemy

let stripInner = null;

function buildStrip() {
    stripInner = document.getElementById('roulette-strip-inner');
    if (!stripInner) return;

    for (let r = 0; r < REPS; r++) {
        WHEEL_ORDER.forEach(n => {
            const cell = document.createElement('div');
            cell.className = `roulette-strip-cell ${getNumberColor(n)}`;
            cell.textContent = n;
            stripInner.appendChild(cell);
        });
    }

    // Pozycja startowa: pokazujemy środek paska (5. rep) bez animacji
    setStripPosition(4 * 37 + 0, false);
}

function getHalfW() {
    const wrap = document.getElementById('roulette-strip-wrap');
    return wrap ? wrap.offsetWidth / 2 : 300;
}

/**
 * Ustawia pozycję paska tak, żeby komórka o indeksie `cellIndex` była wyśrodkowana.
 * @param {number} cellIndex  - globalny indeks komórki (rep * 37 + pozycja_na_kole)
 * @param {boolean} animate   - true = CSS transition, false = natychmiastowo
 */
function setStripPosition(cellIndex, animate) {
    if (!stripInner) return;
    const halfW = getHalfW();
    const targetX = halfW - (cellIndex * CELL_SIZE + CELL_SIZE / 2);

    if (animate) {
        stripInner.style.transition = `transform ${ANIM_MS}ms cubic-bezier(0.12, 0.82, 0.18, 1.0)`;
    } else {
        stripInner.style.transition = 'none';
    }
    stripInner.style.transform = `translateX(${targetX}px)`;
}

/**
 * Animuje pasek do wskazanego numeru.
 * Zwraca Promise rozwiązywany po zakończeniu animacji.
 */
function animateToNumber(number) {
    return new Promise(resolve => {
        if (!stripInner) { setTimeout(resolve, 500); return; }

        const wheelIndex = WHEEL_ORDER.indexOf(number);

        // Resetuj do pozycji startowej bez animacji
        const startCellIndex = START_REP * 37 + wheelIndex;
        setStripPosition(startCellIndex, false);
        void stripInner.offsetWidth; // force reflow

        // Usuń poprzednio zaznaczone zwycięskie komórki
        stripInner.querySelectorAll('.strip-winner').forEach(el => el.classList.remove('strip-winner'));

        // Animacja do docelowej pozycji
        const endCellIndex = END_REP * 37 + wheelIndex;
        setStripPosition(endCellIndex, true);

        // Po zakończeniu animacji — zaznacz komórkę
        setTimeout(() => {
            const cells = stripInner.querySelectorAll('.roulette-strip-cell');
            if (cells[endCellIndex]) cells[endCellIndex].classList.add('strip-winner');
            resolve();
        }, ANIM_MS + 80);
    });
}

// ─── Multi-bet — koszyk zakładów ──────────────────────────────────────────────

// Etykiety dla każdego typu zakładu
const BET_LABELS = {
    number:  v => `Numer ${v}`,
    red:     () => 'Czerwone (1:1)',
    black:   () => 'Czarne (1:1)',
    odd:     () => 'Nieparzyste (1:1)',
    even:    () => 'Parzyste (1:1)',
    low:     () => '1–18 (1:1)',
    high:    () => '19–36 (1:1)',
    dozen1:  () => '1–12 (2:1)',
    dozen2:  () => '13–24 (2:1)',
    dozen3:  () => '25–36 (2:1)',
    column1: () => 'Kolumna 1 (2:1)',
    column2: () => 'Kolumna 2 (2:1)',
    column3: () => 'Kolumna 3 (2:1)',
};

// Map: klucz → { betType, betValue, label }
const selectedBets = new Map();

let spinning = false;

// Elementy DOM
const spinBtn    = document.getElementById('roulette-spin');
const statusEl   = document.getElementById('roulette-status');
const winEl      = document.getElementById('roulette-win');
const detailsEl  = document.getElementById('roulette-result-details');
const basketEl   = document.getElementById('roulette-basket');
const totalEl    = document.getElementById('roulette-basket-total');
const historyEl  = document.getElementById('roulette-history');
const betInput   = document.getElementById('roulette-bet');
const balanceEl  = document.getElementById('balance-display');

// ─── Obsługa kliknięć na stole ─────────────────────────────────────────────────

document.getElementById('roulette-table').addEventListener('click', e => {
    const cell = e.target.closest('[data-bet]');
    if (!cell || !cell.dataset.bet || spinning) return;

    const betType  = cell.dataset.bet;
    const betValue = cell.dataset.value ?? '';
    const key      = `${betType}_${betValue}`;
    const label    = BET_LABELS[betType]?.(betValue) ?? betType;

    if (selectedBets.has(key)) {
        // Odznacz
        selectedBets.delete(key);
        cell.classList.remove('selected');
    } else {
        // Zaznacz
        selectedBets.set(key, { betType, betValue, label, cell });
        cell.classList.add('selected');
    }

    renderBasket();
    spinBtn.disabled = selectedBets.size === 0;
});

// ─── Koszyk zakładów ──────────────────────────────────────────────────────────

function renderBasket() {
    if (selectedBets.size === 0) {
        basketEl.innerHTML = '<span class="text-secondary small">Kliknij na stole aby dodać zakłady</span>';
        totalEl.textContent = '';
        return;
    }

    const betAmount = parseFloat(betInput.value) || 0;

    basketEl.innerHTML = [...selectedBets.entries()].map(([key, b]) =>
        `<span class="roulette-bet-chip">
            ${b.label}
            <button class="roulette-chip-remove" data-key="${key}" title="Usuń">&times;</button>
         </span>`
    ).join('');

    // Obsługa usuwania chipów
    basketEl.querySelectorAll('.roulette-chip-remove').forEach(btn => {
        btn.addEventListener('click', () => removeBet(btn.dataset.key));
    });

    const total = selectedBets.size * betAmount;
    totalEl.textContent = betAmount > 0
        ? `Łączna stawka: ${total.toFixed(2)} PLN (${selectedBets.size} × ${betAmount.toFixed(2)})`
        : '';
}

function removeBet(key) {
    const entry = selectedBets.get(key);
    if (entry) {
        entry.cell.classList.remove('selected');
        selectedBets.delete(key);
    }
    renderBasket();
    spinBtn.disabled = selectedBets.size === 0;
}

function clearAllBets() {
    selectedBets.forEach(b => b.cell.classList.remove('selected'));
    selectedBets.clear();
    renderBasket();
    spinBtn.disabled = true;
}

// Aktualizuj total przy zmianie kwoty
betInput.addEventListener('input', renderBasket);

// Przycisk "Wyczyść"
document.getElementById('roulette-clear').addEventListener('click', clearAllBets);

// ─── Spin ─────────────────────────────────────────────────────────────────────

spinBtn.addEventListener('click', async () => {
    if (spinning || selectedBets.size === 0) return;

    const bet = parseFloat(betInput.value);
    if (!bet || bet <= 0) {
        statusEl.textContent = 'Podaj poprawną stawkę.';
        return;
    }

    spinning = true;
    spinBtn.classList.add('roulette-spinning');
    spinBtn.disabled = true;
    statusEl.textContent = 'Losowanie...';
    winEl.textContent = '';
    detailsEl.innerHTML = '';

    try {
        // Budujemy listę zakładów do wysłania
        const bets = [...selectedBets.values()].map(b => ({
            betType:  b.betType,
            betValue: b.betValue,
            bet:      bet
        }));

        const res = await fetch('/api/roulette/spin', {
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

        // Animacja paska — czekamy na jej koniec
        await animateToNumber(data.number);

        // Podświetl komórkę na stole
        highlightTableResult(data.number);

        // Historia
        addToHistory(data.number, data.color);

        // Balans
        updateBalance(data.balance);

        // Wyniki
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

// ─── Wyświetlanie wyników ─────────────────────────────────────────────────────

function showResults(data) {
    const colorPL  = data.color === 'red' ? 'czerwone' : data.color === 'black' ? 'czarne' : 'zielone';
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

    // Szczegóły per-zakład (tylko gdy > 1 zakład lub chcemy zawsze pokazać)
    detailsEl.innerHTML = '';
    if (data.results && data.results.length > 0) {
        data.results.forEach(r => {
            const row = document.createElement('div');
            row.className = `roulette-result-row ${r.won ? 'won' : 'lost'}`;

            const label = BET_LABELS[r.betType]?.(r.betValue) ?? r.betType;
            const icon  = r.won ? '✓' : '✗';
            const amtStr = r.won
                ? `+${Number(r.win).toFixed(2)} PLN`
                : `-${Number(r.bet).toFixed(2)} PLN`;

            row.innerHTML = `
                <span>${icon} ${label}</span>
                <span class="roulette-result-amount">${amtStr}</span>
            `;
            detailsEl.appendChild(row);
        });
    }
}

// ─── Podświetlenie na stole ───────────────────────────────────────────────────

function highlightTableResult(number) {
    const cell = document.querySelector(`.roulette-table [data-bet="number"][data-value="${number}"]`);
    if (!cell) return;
    cell.classList.remove('result-flash');
    void cell.offsetWidth; // reflow
    cell.classList.add('result-flash');
    setTimeout(() => cell.classList.remove('result-flash'), 1600);
}

// ─── Historia ─────────────────────────────────────────────────────────────────

function addToHistory(number, color) {
    if (!historyEl) return;
    const chip = document.createElement('span');
    chip.className = `roulette-chip ${color}`;
    chip.textContent = number;
    chip.title = number;
    historyEl.prepend(chip);

    // Maks 20 wpisów
    const chips = historyEl.querySelectorAll('.roulette-chip');
    if (chips.length > 20) chips[chips.length - 1].remove();
}

// ─── Balans ───────────────────────────────────────────────────────────────────

function updateBalance(balance) {
    if (balanceEl && balance !== undefined && balance !== null) {
        balanceEl.textContent = parseFloat(balance).toFixed(2);
    }
}

// ─── Init ─────────────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    buildStrip();
});
