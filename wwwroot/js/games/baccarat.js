const SUIT_SYMBOLS = { hearts: '\u2665', diamonds: '\u2666', clubs: '\u2663', spades: '\u2660' };
const RED_SUITS = new Set(['hearts', 'diamonds']);
const TIMING = {
    perCardDelay: 400,
    dealCards: 900,
    revealAll: 600,
    resultPause: 400
};

let state = {
    bet: 0,
    betType: 'player',
    busy: false,
    game: null
};

const $ = id => document.getElementById(id);

function formatMoney(val) {
    return parseFloat(val).toFixed(2) + ' PLN';
}

function setBalance(val) {
    if (val !== 0 && !val) return;
    $('bac-balance').textContent = formatMoney(val);
}

function wait(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function fetchBalance() {
    const res = await fetch('/api/baccarat/balance');
    if (res.ok) {
        const data = await res.json();
        setBalance(data.balance);
        updateBalanceDisplay(data);
    }
}

// ── Obliczanie wartości kart w Baccarat (zgodne z serwerem) ──

function getCardValue(rank) {
    if (rank === 'A') return 1;
    if (rank === 'J' || rank === 'Q' || rank === 'K' || rank === '10') return 0;
    return parseInt(rank);
}

function calculateBaccaratValue(cards) {
    let total = 0;
    for (const card of cards) total += getCardValue(card.rank);
    return total % 10;
}

function buildCardElement(card, delay = 0) {
    const wrapper = document.createElement('div');
    wrapper.className = 'bac-card';
    wrapper.style.animationDelay = delay + 'ms';

    const suit = card.suit;
    const rank = card.rank;
    const sym = SUIT_SYMBOLS[suit] || suit;
    const colorClass = RED_SUITS.has(suit) ? 'red' : 'black';
    const face = document.createElement('div');

    face.className = `bac-card-face ${colorClass}`;
    face.innerHTML = `
        <div class="bac-card-corner">
            <div>${rank}</div>
            <div>${sym}</div>
        </div>
        <div class="bac-card-center">${sym}</div>
        <div class="bac-card-corner bottom">
            <div>${rank}</div>
            <div>${sym}</div>
        </div>
    `;

    wrapper.appendChild(face);
    return wrapper;
}

function clearTable() {
    $('player-cards').innerHTML = '';
    $('banker-cards').innerHTML = '';
    $('player-value').textContent = '';
    $('banker-value').textContent = '';
    $('bac-result-banner').textContent = '';
    $('bac-result-banner').className = 'bac-result-banner';
    $('action-area').style.display = 'none';
    $('bet-area').style.display = 'flex';
    $('bet-type-area').style.display = 'flex';
    state.game = null;
}

async function animateDeal(gameState) {
    // Rozdawanie naprzemienne z sumowaniem na bieżąco: Gracz → Bankier → Gracz → Bankier
    const playerCards = gameState.playerHand;
    const bankerCards = gameState.bankerHand;
    const maxCards = Math.max(playerCards.length, bankerCards.length);

    $('player-cards').innerHTML = '';
    $('banker-cards').innerHTML = '';

    // Wyczyść wartości i przygotuj do animacji
    $('player-value').textContent = '';
    $('banker-value').textContent = '';
    $('player-value').style.transition = 'opacity 0.3s ease';
    $('banker-value').style.transition = 'opacity 0.3s ease';

    const dealtPlayer = [];
    const dealtBanker = [];

    for (let i = 0; i < maxCards; i++) {
        // Karta Gracza
        if (i < playerCards.length) {
            const el = buildCardElement(playerCards[i], 0);
            el.classList.add('bac-card-deal');
            $('player-cards').appendChild(el);
            dealtPlayer.push(playerCards[i]);

            // Czekamy aż animacja karty się zakończy
            await wait(TIMING.perCardDelay - 50);

            // Aktualizacja sumy na bieżąco
            const runningValue = calculateBaccaratValue(dealtPlayer);
            $('player-value').textContent = runningValue;
            $('player-value').style.opacity = '0';
            requestAnimationFrame(() => {
                $('player-value').style.opacity = '1';
            });
            await wait(50);
        }

        // Karta Bankiera
        if (i < bankerCards.length) {
            const el = buildCardElement(bankerCards[i], 0);
            el.classList.add('bac-card-deal');
            $('banker-cards').appendChild(el);
            dealtBanker.push(bankerCards[i]);

            await wait(TIMING.perCardDelay - 50);

            const runningValue = calculateBaccaratValue(dealtBanker);
            $('banker-value').textContent = runningValue;
            $('banker-value').style.opacity = '0';
            requestAnimationFrame(() => {
                $('banker-value').style.opacity = '1';
            });
            await wait(50);
        }
    }

    // Dodatkowy moment na ochłonięcie przed bannerem wyniku
    await wait(TIMING.dealCards);
}

function showResult(gameState) {
    const banner = $('bac-result-banner');
    const betType = gameState.betType;
    const result = gameState.result;
    const netWin = gameState.netWin;
    let text = '';
    let cls = '';

    if (result === 'tie' && betType === 'tie') {
        text = `Remis! +${formatMoney(netWin)} (8:1)`;
        cls = 'win';
    } else if (result === 'tie') {
        text = 'Remis – zwrot zakładów';
        cls = 'push';
    } else if (result === 'player_wins' && betType === 'player') {
        text = `Gracz wygrywa! +${formatMoney(netWin)}`;
        cls = 'win';
    } else if (result === 'banker_wins' && betType === 'banker') {
        text = `Bankier wygrywa! +${formatMoney(netWin)}`;
        cls = 'win';
    } else if (result === 'player_wins') {
        text = 'Wygrywa Gracz – przegrany zakład';
        cls = 'lose';
    } else if (result === 'banker_wins') {
        text = 'Wygrywa Bankier – przegrany zakład';
        cls = 'lose';
    }

    // Podświetlenie zwycięzcy
    document.querySelectorAll('.bac-hand-zone').forEach(el => {
        el.classList.remove('bac-hand-win', 'bac-hand-lose');
    });

    if (result === 'player_wins') {
        document.querySelector('.bac-player-zone').classList.add('bac-hand-win');
        document.querySelector('.bac-banker-zone').classList.add('bac-hand-lose');
    } else if (result === 'banker_wins') {
        document.querySelector('.bac-banker-zone').classList.add('bac-hand-win');
        document.querySelector('.bac-player-zone').classList.add('bac-hand-lose');
    }

    banner.textContent = text;
    banner.className = 'bac-result-banner ' + cls;
}

function addHistoryDot(result) {
    const container = $('bac-history-dots');
    const dot = document.createElement('div');
    let cls = 'bac-hist-dot ';
    let letter = '';

    if (result === 'player_wins') {
        cls += 'bac-dot-player';
        letter = 'P';
    } else if (result === 'banker_wins') {
        cls += 'bac-dot-banker';
        letter = 'B';
    } else {
        cls += 'bac-dot-tie';
        letter = 'T';
    }

    dot.className = cls;
    dot.textContent = letter;
    container.appendChild(dot);

    // Przewiń do najnowszego wyniku
    $('bac-history-track').scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

async function playRound() {
    if (state.busy) return;
    if (state.bet <= 0) {
        alert('Wybierz zaklad przed rozdaniem.');
        return;
    }

    setBusy(true);

    try {
        const res = await fetch('/api/baccarat/play', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                bet: state.bet,
                betType: state.betType
            })
        });

        const data = await res.json();
        if (!res.ok) {
            throw new Error(data.error || 'Blad serwera');
        }

        $('bet-area').style.display = 'none';
        $('bet-type-area').style.display = 'none';
        $('action-area').style.display = 'flex';
        $('btn-new-game').style.display = 'inline-flex';

        state.game = data.state;

        await animateDeal(data.state);

        await wait(TIMING.revealAll);

        showResult(data.state);

        addHistoryDot(data.state.result);

        if (data.balance !== undefined && data.balance !== null) {
            setBalance(data.balance);
            updateBalanceDisplay({ balance: data.balance, balanceBonus: data.balanceBonus });
        }

    } catch (e) {
        alert(e.message);
    } finally {
        setBusy(false);
    }
}

function setBusy(busy) {
    state.busy = busy;
    ['deal-btn', 'btn-new-game'].forEach(id => {
        const el = $(id);
        if (el) el.disabled = busy;
    });
}

function addChip(value) {
    state.bet += value;
    $('bet-display').textContent = state.bet;
}

function clearBet() {
    state.bet = 0;
    $('bet-display').textContent = '0';
}

function setBetType(type) {
    state.betType = type;
    document.querySelectorAll('.bac-bet-option').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.bet === type);
    });
}

document.addEventListener('DOMContentLoaded', () => {
    fetchBalance();

    // Przyciski żetonów
    document.querySelectorAll('.bac-chip').forEach(chip => {
        chip.addEventListener('click', () => addChip(parseInt(chip.dataset.value)));
    });

    $('clear-bet-btn').addEventListener('click', clearBet);
    $('deal-btn').addEventListener('click', playRound);

    // Wybór typu zakładu
    document.querySelectorAll('.bac-bet-option').forEach(btn => {
        btn.addEventListener('click', () => setBetType(btn.dataset.bet));
    });

    $('btn-new-game').addEventListener('click', () => {
        clearTable();
        clearBet();
    });
});
