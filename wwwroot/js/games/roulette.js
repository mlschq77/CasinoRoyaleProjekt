'use strict';

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

let selectedBetType = null;
let selectedBetValue = null;
let spinning = false;

const spinBtn     = document.getElementById('roulette-spin');
const betDisplay  = document.getElementById('roulette-bet-display');
const statusEl    = document.getElementById('roulette-status');
const winEl       = document.getElementById('roulette-win');
const historyEl   = document.getElementById('roulette-history');
const balanceEl   = document.getElementById('balance-display');

document.getElementById('roulette-table').addEventListener('click', e => {
    const cell = e.target.closest('[data-bet]');
    if (!cell || spinning) return;

    document.querySelectorAll('.roulette-table td.selected').forEach(el => el.classList.remove('selected'));
    cell.classList.add('selected');

    selectedBetType  = cell.dataset.bet;
    selectedBetValue = cell.dataset.value ?? '';

    const label = BET_LABELS[selectedBetType]?.(selectedBetValue) ?? selectedBetType;
    betDisplay.textContent = label;
    spinBtn.disabled = false;
});

spinBtn.addEventListener('click', async () => {
    if (spinning || !selectedBetType) return;

    const bet = parseFloat(document.getElementById('roulette-bet').value);
    if (!bet || bet <= 0) {
        statusEl.textContent = 'Podaj poprawna stawke.';
        return;
    }

    spinning = true;
    spinBtn.classList.add('roulette-spinning');
    spinBtn.disabled = true;
    statusEl.textContent = 'Krecenie...';
    winEl.textContent = '';

    try {
        const res = await fetch('/api/roulette/spin', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                bet: bet,
                betType: selectedBetType,
                betValue: selectedBetValue
            })
        });

        const data = await res.json();

        if (!res.ok) {
            statusEl.textContent = data.error ?? 'Blad serwera.';
            if (data.balance != null) updateBalance(data.balance);
            return;
        }

        highlightResult(data.number);
        addToHistory(data.number, data.color);
        updateBalance(data.balance);

        if (data.win > 0) {
            statusEl.textContent = `Wypadlo: ${data.number} — Wygrales!`;
            winEl.style.color = '#d4af37';
            winEl.textContent = `+${data.win.toFixed(2)} PLN`;
        } else {
            statusEl.textContent = `Wypadlo: ${data.number} — Przegrales.`;
            winEl.style.color = '#e74c3c';
            winEl.textContent = `-${bet.toFixed(2)} PLN`;
        }
    } catch {
        statusEl.textContent = 'Blad polaczenia.';
    } finally {
        spinning = false;
        spinBtn.classList.remove('roulette-spinning');
        spinBtn.disabled = false;
    }
});

function highlightResult(number) {
    const cell = document.querySelector(`[data-bet="number"][data-value="${number}"]`);
    if (!cell) return;
    cell.classList.remove('result-flash');
    void cell.offsetWidth;
    cell.classList.add('result-flash');
    setTimeout(() => cell.classList.remove('result-flash'), 1600);
}

function addToHistory(number, color) {
    const chip = document.createElement('span');
    chip.className = `roulette-chip ${color}`;
    chip.textContent = number;
    chip.title = number;
    historyEl.prepend(chip);

    const chips = historyEl.querySelectorAll('.roulette-chip');
    if (chips.length > 20) chips[chips.length - 1].remove();
}

function updateBalance(balance) {
    if (balanceEl) balanceEl.textContent = parseFloat(balance).toFixed(2);
}
