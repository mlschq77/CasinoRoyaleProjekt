const SUIT_SYMBOLS = { hearts: '♥', diamonds: '♦', clubs: '♣', spades: '♠' };
const RED_SUITS = new Set(['hearts', 'diamonds']);

let state = {
    gameId: null,
    bet: 0,
    busy: false
};

const $ = id => document.getElementById(id);

function formatMoney(val) {
    return parseFloat(val).toFixed(2) + ' PLN';
}

function setBalance(val) {
    if (val !== 0 && !val) return;
    $('bj-balance').textContent = formatMoney(val);
}

async function fetchBalance() {
    const res = await fetch('/api/blackjack/balance');
    if (res.ok) {
        const data = await res.json();
        setBalance(data.balance);
    }
}

function buildCardElement(card, delay = 0) {
    const wrapper = document.createElement('div');
    wrapper.className = 'bj-card';
    wrapper.style.animationDelay = delay + 'ms';

    if (card.faceDown) {
        const back = document.createElement('div');
        back.className = 'bj-card-back';
        wrapper.appendChild(back);
    } else {
        const suit = card.suit;
        const rank = card.rank;
        const sym = SUIT_SYMBOLS[suit] || suit;
        const colorClass = RED_SUITS.has(suit) ? 'red' : 'black';

        const face = document.createElement('div');
        face.className = `bj-card-face ${colorClass}`;

        face.innerHTML = `
            <div class="bj-card-corner">
                <div>${rank}</div>
                <div>${sym}</div>
            </div>
            <div class="bj-card-center">${sym}</div>
            <div class="bj-card-corner bottom">
                <div>${rank}</div>
                <div>${sym}</div>
            </div>
        `;
        wrapper.appendChild(face);
    }
    return wrapper;
}

function renderCards(containerId, cards, startDelay = 0) {
    const container = $(containerId);
    container.innerHTML = '';
    cards.forEach((card, i) => {
        const el = buildCardElement(card, startDelay + i * 120);
        container.appendChild(el);
    });
}

function renderGameState(gameState, balance) {
    renderCards('dealer-cards', gameState.dealerHand);
    renderCards('player-cards', gameState.playerHand, gameState.dealerHand.length * 120);

    const dealerVal = gameState.dealerValue;
    $('dealer-value').textContent = dealerVal > 0 ? dealerVal : '';
    $('player-value').textContent = gameState.playerValue > 0 ? gameState.playerValue : '';

    if (gameState.isSplitActive && gameState.splitHand && gameState.splitHand.length > 0) {
        $('split-hand-area').style.display = 'block';
        renderCards('split-cards', gameState.splitHand);
        $('split-value').textContent = gameState.splitValue > 0 ? gameState.splitValue : '';
    } else {
        $('split-hand-area').style.display = 'none';
    }

    if (!gameState.isActive) {
        showResult(gameState.result, gameState.winAmount);
        showNewGameButton();
        if (balance && balance > 0) setBalance(balance);
        return;
    }

    clearResult();

    if (balance && balance > 0) setBalance(balance);

    $('btn-double').style.display = gameState.canDouble ? 'inline-flex' : 'none';
    $('btn-split').style.display = gameState.canSplit ? 'inline-flex' : 'none';
    $('btn-new-game').style.display = 'none';
}

function showResult(result, winAmount) {
    const banner = $('bj-result-banner');
    let text = '';
    let cls = '';

    if (result === 'blackjack') {
        text = '🃏 Blackjack! +' + formatMoney(winAmount);
        cls = 'win';
    } else if (result === 'player_wins') {
        text = '✓ Wygrywasz! +' + formatMoney(winAmount);
        cls = 'win';
    } else if (result === 'dealer_wins') {
        text = '✗ Krupier wygrywa';
        cls = 'lose';
    } else if (result === 'push') {
        text = '≡ Remis – zwrot zakładu';
        cls = 'push';
    }

    banner.textContent = text;
    banner.className = 'bj-result-banner ' + cls;
}

function clearResult() {
    const banner = $('bj-result-banner');
    banner.textContent = '';
    banner.className = 'bj-result-banner';
}

function showNewGameButton() {
    $('btn-hit').style.display = 'none';
    $('btn-stand').style.display = 'none';
    $('btn-double').style.display = 'none';
    $('btn-split').style.display = 'none';
    $('btn-new-game').style.display = 'inline-flex';
}

function showActionButtons() {
    $('bet-area').style.display = 'none';
    $('action-area').style.display = 'flex';
    $('btn-hit').style.display = 'inline-flex';
    $('btn-stand').style.display = 'inline-flex';
    $('btn-new-game').style.display = 'none';
}

function showBetArea() {
    $('bet-area').style.display = 'block';
    $('action-area').style.display = 'none';
    clearResult();
    $('dealer-cards').innerHTML = '';
    $('player-cards').innerHTML = '';
    $('split-cards').innerHTML = '';
    $('split-hand-area').style.display = 'none';
    $('dealer-value').textContent = '';
    $('player-value').textContent = '';
    $('split-value').textContent = '';
    state.gameId = null;
}

async function apiPost(endpoint, body) {
    const res = await fetch('/api/blackjack/' + endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });

    const data = await res.json();
    if (!res.ok) {
        throw new Error(data.error || 'Błąd serwera');
    }
    return data;
}

function setBusy(busy) {
    state.busy = busy;
    ['btn-hit', 'btn-stand', 'btn-double', 'btn-split', 'btn-new-game', 'deal-btn'].forEach(id => {
        const el = $(id);
        if (el) el.disabled = busy;
    });
}

async function startGame() {
    if (state.busy) return;
    if (state.bet <= 0) {
        alert('Wybierz zakład przed rozdaniem.');
        return;
    }

    setBusy(true);
    try {
        const data = await apiPost('start', { bet: state.bet });
        state.gameId = data.state.gameId;
        showActionButtons();
        renderGameState(data.state, data.balance);
    } catch (e) {
        alert(e.message);
    } finally {
        setBusy(false);
    }
}

async function doAction(action) {
    if (state.busy || !state.gameId) return;
    setBusy(true);
    try {
        const data = await apiPost(action, { gameId: state.gameId });
        renderGameState(data.state, data.balance);
    } catch (e) {
        alert(e.message);
    } finally {
        setBusy(false);
    }
}

function addChip(value) {
    state.bet += value;
    $('bet-display').textContent = state.bet;
}

function clearBet() {
    state.bet = 0;
    $('bet-display').textContent = '0';
}

document.addEventListener('DOMContentLoaded', () => {
    fetchBalance();

    document.querySelectorAll('.bj-chip').forEach(chip => {
        chip.addEventListener('click', () => addChip(parseInt(chip.dataset.value)));
    });

    $('clear-bet-btn').addEventListener('click', clearBet);
    $('deal-btn').addEventListener('click', startGame);

    $('btn-hit').addEventListener('click', () => doAction('hit'));
    $('btn-stand').addEventListener('click', () => doAction('stand'));
    $('btn-double').addEventListener('click', () => doAction('double'));
    $('btn-split').addEventListener('click', () => doAction('split'));

    $('btn-new-game').addEventListener('click', () => {
        showBetArea();
        clearBet();
    });
});
