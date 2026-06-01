'use strict';

const fruitsSymbols = ['🍒', '🍋', '🍊', '🍇', '🍉', '7'];
let fruitsSpinning = false;

const fruitsSpinButton = document.getElementById('fruits-spin');
const fruitsBetInput = document.getElementById('fruits-bet');
const fruitsStatus = document.getElementById('fruits-status');
const fruitsWin = document.getElementById('fruits-win');
const fruitsMultiplier = document.getElementById('fruits-multiplier');
const fruitsReels = Array.from(document.querySelectorAll('.fruits-reel span'));
const fruitsPaytableRows = document.getElementById('fruits-paytable-rows');

function updateFruitsBalance(balance) {
    const balanceDisplay = document.getElementById('balance-display');
    if (balanceDisplay && balance !== undefined && balance !== null) {
        balanceDisplay.textContent = Number(balance).toFixed(2);
    }
}

async function parseFruitsResponse(response) {
    const text = await response.text();
    const data = text ? JSON.parse(text) : {};

    if (!response.ok) {
        throw new Error(data.error || 'Nie udalo sie zakrecic.');
    }

    return data;
}

function wait(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function animateFruitsReels(finalReels) {
    const spinTimers = fruitsReels.map((reel, index) => {
        reel.parentElement.classList.add('is-spinning');

        return window.setInterval(() => {
            reel.textContent = fruitsSymbols[Math.floor(Math.random() * fruitsSymbols.length)];
        }, 68 + index * 12);
    });

    for (let index = 0; index < fruitsReels.length; index++) {
        await wait(360 + index * 170);
        window.clearInterval(spinTimers[index]);
        fruitsReels[index].textContent = finalReels[index].icon;
        fruitsReels[index].parentElement.classList.remove('is-spinning');
        fruitsReels[index].parentElement.classList.add('fruits-reel-stop');
        window.setTimeout(() => fruitsReels[index].parentElement.classList.remove('fruits-reel-stop'), 360);
    }
}

function setFruitsSpinState(isSpinning) {
    fruitsSpinning = isSpinning;
    fruitsSpinButton.disabled = isSpinning;
    fruitsSpinButton.classList.toggle('fruits-button-spinning', isSpinning);
}

async function spinFruits() {
    if (fruitsSpinning) return;

    const bet = Number(fruitsBetInput.value);
    if (!bet || bet <= 0) {
        fruitsStatus.textContent = 'Podaj poprawna stawke.';
        return;
    }

    setFruitsSpinState(true);
    fruitsStatus.textContent = 'Bebny sie kreca...';
    fruitsWin.textContent = '-';
    fruitsMultiplier.textContent = 'x0.00';

    try {
        const response = await fetch(`/api/fruits/spin?bet=${encodeURIComponent(bet)}`, {
            method: 'POST'
        });
        const data = await parseFruitsResponse(response);

        await animateFruitsReels(data.reels);
        updateFruitsBalance(data.balance);

        const multiplier = Number(data.multiplier);
        const win = Number(data.win);
        fruitsMultiplier.textContent = `x${multiplier.toFixed(2)}`;
        fruitsStatus.textContent = data.message;
        fruitsWin.textContent = win > 0 ? `Wygrana: ${win.toFixed(2)}` : `Strata: ${bet.toFixed(2)}`;
        fruitsWin.classList.toggle('fruits-loss', win <= 0);
    } catch (error) {
        fruitsStatus.textContent = error.message || 'Blad polaczenia.';
    } finally {
        setFruitsSpinState(false);
    }
}

async function loadFruitsPaytable() {
    try {
        const response = await fetch('/api/fruits/paytable');
        const data = await parseFruitsResponse(response);
        fruitsPaytableRows.innerHTML = '';

        data.payouts.forEach(payout => {
            const row = document.createElement('div');
            row.className = 'fruits-paytable-row';
            row.innerHTML = `
                <span><strong>${payout.icon}</strong> ${payout.name}</span>
                <span>x${Number(payout.three).toFixed(2)}</span>
                <span>x${Number(payout.four).toFixed(2)}</span>
                <span>x${Number(payout.five).toFixed(2)}</span>
            `;
            fruitsPaytableRows.appendChild(row);
        });
    } catch (error) {
        console.error(error);
    }
}

document.querySelectorAll('.fruits-quick-bets button').forEach(button => {
    button.addEventListener('click', () => {
        fruitsBetInput.value = button.dataset.bet;
    });
});

fruitsSpinButton.addEventListener('click', spinFruits);
loadFruitsPaytable();
