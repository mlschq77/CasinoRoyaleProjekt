const SUIT_SYMBOLS = { hearts: '\u2665', diamonds: '\u2666', clubs: '\u2663', spades: '\u2660' };
const RED_SUITS = new Set(['hearts', 'diamonds']);
const TIMING = {
    actionPause: 1450,
    doublePeek: 2100,
    revealDealer: 1900,
    dealerCard: 1650,
    resultPause: 1450
};

let state = {
    gameId: null,
    bet: 0,
    busy: false,
    game: null,
    renderedCards: {}
};

const $ = id => document.getElementById(id);

function formatMoney(val) {
    return parseFloat(val).toFixed(2) + ' PLN';
}

function setBalance(val) {
    if (val !== 0 && !val) return;
    $('bj-balance').textContent = formatMoney(val);
}

function wait(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
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
        return wrapper;
    }

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
    return wrapper;
}

function renderCards(containerId, cards, startDelay = 0, revealLast = false) {
    const container = $(containerId);
    const previousCards = state.renderedCards[containerId] || [];
    container.innerHTML = '';

    cards.forEach((card, i) => {
        const previous = previousCards[i];
        const isNewCard = !previous;
        const wasFaceDown = previous && previous.faceDown;
        const isRevealedNow = wasFaceDown && !card.faceDown;
        const shouldReveal = isRevealedNow || (revealLast && i === cards.length - 1 && !card.faceDown);
        const el = buildCardElement(card, isNewCard ? startDelay + i * 160 : 0);

        if (isNewCard) {
            el.classList.add('bj-card-deal');
        }

        if (shouldReveal) {
            el.classList.add('bj-card-reveal');
        }

        container.appendChild(el);
    });

    state.renderedCards[containerId] = cards.map(card => ({
        suit: card.suit,
        rank: card.rank,
        faceDown: card.faceDown
    }));
}

function calculateVisibleHandValue(hand) {
    let total = 0;
    let aces = 0;

    hand.forEach(card => {
        if (card.faceDown) return;

        if (card.rank === 'A') {
            aces++;
            total += 11;
        } else if (['J', 'Q', 'K'].includes(card.rank)) {
            total += 10;
        } else {
            total += parseInt(card.rank);
        }
    });

    while (total > 21 && aces > 0) {
        total -= 10;
        aces--;
    }

    return total;
}

function setActiveHand(gameState) {
    const main = document.querySelector('.bj-main-hand');
    const split = document.querySelector('.bj-split-hand');

    main.classList.remove('bj-hand-active');
    split.classList.remove('bj-hand-active');

    if (!gameState.isActive || !gameState.isPlayerTurn) return;

    if (gameState.activeHand === 'split') {
        split.classList.add('bj-hand-active');
    } else {
        main.classList.add('bj-hand-active');
    }
}

function getHandResultText(handResult) {
    if (!handResult) return '';

    if (handResult.result === 'win') return 'Wygrana +' + formatMoney(handResult.winAmount);
    if (handResult.result === 'push') return 'Remis ' + formatMoney(handResult.winAmount);
    return 'Przegrana';
}

function applyHandResult(selector, result) {
    const hand = document.querySelector(selector);
    if (!hand || !result) return;

    hand.classList.add('bj-hand-' + result.result);

    const badge = document.createElement('div');
    badge.className = 'bj-hand-result';
    badge.textContent = getHandResultText(result);
    hand.appendChild(badge);
}

function setHandResults(gameState) {
    document.querySelectorAll('.bj-hand-result').forEach(el => el.remove());
    document.querySelectorAll('.bj-main-hand, .bj-split-hand').forEach(el => {
        el.classList.remove('bj-hand-win', 'bj-hand-lose', 'bj-hand-push');
    });

    if (gameState.isActive || !gameState.handResults) return;

    applyHandResult('.bj-main-hand', gameState.handResults.main);
    if (gameState.isSplitActive) {
        applyHandResult('.bj-split-hand', gameState.handResults.split);
    }
}

function setDoubleChoices(show) {
    const choice = $('double-choice');
    if (choice) choice.style.display = show ? 'inline-flex' : 'none';
}

function ensureDoubleChoices() {
    if ($('double-choice')) return;

    const choice = document.createElement('div');
    choice.id = 'double-choice';
    choice.className = 'bj-double-choice';
    choice.style.display = 'none';
    choice.innerHTML = `
        <button class="casino-btn-secondary btn bj-action-btn" id="btn-double-faceup">
            <i class="fa-solid fa-eye me-1"></i>Face up
        </button>
        <button class="casino-btn-secondary btn bj-action-btn" id="btn-double-facedown">
            <i class="fa-solid fa-eye-slash me-1"></i>Face down
        </button>
    `;

    $('btn-double').insertAdjacentElement('afterend', choice);
    $('btn-double-faceup').addEventListener('click', () => doDouble(false));
    $('btn-double-facedown').addEventListener('click', () => doDouble(true));
}

function renderGameState(gameState, balance, options = {}) {
    state.game = gameState;

    renderCards('dealer-cards', gameState.dealerHand);
    renderCards('player-cards', gameState.playerHand, gameState.dealerHand.length * 160, options.revealMainLast);

    $('dealer-value').textContent = gameState.dealerValue > 0 ? gameState.dealerValue : '';
    $('player-value').textContent = gameState.playerValue > 0 ? gameState.playerValue : '';

    if (gameState.isSplitActive && gameState.splitHand && gameState.splitHand.length > 0) {
        $('split-hand-area').style.display = 'block';
        renderCards('split-cards', gameState.splitHand, 0, options.revealSplitLast);
        $('split-value').textContent = gameState.splitValue > 0 ? gameState.splitValue : '';
    } else {
        $('split-hand-area').style.display = 'none';
    }

    setActiveHand(gameState);
    setHandResults(gameState);
    setDoubleChoices(false);

    if (!gameState.isActive) {
        if (!options.suppressResult) showResult(gameState);
        showNewGameButton();
        if (balance !== undefined && balance !== null) setBalance(balance);
        return;
    }

    clearResult();

    if (balance !== undefined && balance !== null) setBalance(balance);

    $('btn-double').style.display = gameState.canDouble ? 'inline-flex' : 'none';
    $('btn-split').style.display = gameState.canSplit ? 'inline-flex' : 'none';
    $('btn-new-game').style.display = 'none';
}

function showResult(gameState) {
    const banner = $('bj-result-banner');
    const result = gameState.result;
    const winAmount = gameState.winAmount;
    let text = '';
    let cls = '';

    if (gameState.isSplitActive && gameState.handResults) {
        const values = [gameState.handResults.main.result, gameState.handResults.split.result];

        if (values.includes('win') && values.includes('lose')) {
            text = 'Split rozliczony osobno';
            cls = 'push';
        } else if (values.includes('win')) {
            text = 'Split wygrany +' + formatMoney(winAmount);
            cls = 'win';
        } else if (values.every(v => v === 'push')) {
            text = 'Split remis';
            cls = 'push';
        } else {
            text = 'Split przegrany';
            cls = 'lose';
        }
    } else if (result === 'blackjack') {
        text = 'Blackjack! +' + formatMoney(winAmount);
        cls = 'win';
    } else if (result === 'player_wins') {
        text = 'Wygrywasz! +' + formatMoney(winAmount);
        cls = 'win';
    } else if (result === 'dealer_wins') {
        text = 'Krupier wygrywa';
        cls = 'lose';
    } else if (result === 'push') {
        text = 'Remis - zwrot zakladu';
        cls = 'push';
    }

    banner.textContent = text;
    banner.className = 'bj-result-banner ' + cls;
}

function clearResult() {
    const banner = $('bj-result-banner');
    banner.textContent = '';
    banner.className = 'bj-result-banner';
    document.querySelectorAll('.bj-hand-result').forEach(el => el.remove());
    document.querySelectorAll('.bj-main-hand, .bj-split-hand').forEach(el => {
        el.classList.remove('bj-hand-win', 'bj-hand-lose', 'bj-hand-push');
    });
}

function showNewGameButton() {
    $('btn-hit').style.display = 'none';
    $('btn-stand').style.display = 'none';
    $('btn-double').style.display = 'none';
    $('btn-split').style.display = 'none';
    setDoubleChoices(false);
    $('btn-new-game').style.display = 'inline-flex';
}

function showActionButtons() {
    $('bet-area').style.display = 'none';
    $('action-area').style.display = 'flex';
    $('btn-hit').style.display = 'inline-flex';
    $('btn-stand').style.display = 'inline-flex';
    $('btn-new-game').style.display = 'none';
    setDoubleChoices(false);
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
    state.game = null;
    state.renderedCards = {};
}

async function apiPost(endpoint, body) {
    const res = await fetch('/api/blackjack/' + endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
    });

    const data = await res.json();
    if (!res.ok) {
        throw new Error(data.error || 'Blad serwera');
    }
    return data;
}

function setBusy(busy) {
    state.busy = busy;
    ['btn-hit', 'btn-stand', 'btn-double', 'btn-double-faceup', 'btn-double-facedown', 'btn-split', 'btn-new-game', 'deal-btn'].forEach(id => {
        const el = $(id);
        if (el) el.disabled = busy;
    });
}

function buildDealerPreview(finalState) {
    const preview = JSON.parse(JSON.stringify(finalState));
    const previousDealer = state.game ? state.game.dealerHand : finalState.dealerHand.slice(0, 2);

    preview.dealerHand = previousDealer;
    preview.dealerValue = state.game ? state.game.dealerValue : calculateVisibleHandValue(previousDealer);
    preview.isActive = true;
    preview.isPlayerTurn = false;

    return preview;
}

async function renderDealerResolution(finalState, balance) {
    const baseState = buildDealerPreview(finalState);
    renderGameState(baseState, undefined, { suppressResult: true });
    await wait(TIMING.actionPause);

    const revealedState = JSON.parse(JSON.stringify(baseState));
    const revealedCount = Math.max(baseState.dealerHand.length, Math.min(2, finalState.dealerHand.length));
    revealedState.dealerHand = finalState.dealerHand.slice(0, revealedCount);
    revealedState.dealerValue = calculateVisibleHandValue(revealedState.dealerHand);
    renderGameState(revealedState, undefined, { suppressResult: true });
    await wait(TIMING.revealDealer);

    for (let i = revealedState.dealerHand.length + 1; i <= finalState.dealerHand.length; i++) {
        const step = JSON.parse(JSON.stringify(finalState));
        step.dealerHand = finalState.dealerHand.slice(0, i);
        step.dealerValue = calculateVisibleHandValue(step.dealerHand);
        step.isActive = true;
        step.isPlayerTurn = false;
        renderGameState(step, undefined, { suppressResult: true });
        await wait(TIMING.dealerCard);
    }

    await wait(TIMING.resultPause);
    renderGameState(finalState, balance);
}

async function renderActionResponse(data) {
    if (data.state && !data.state.isActive) {
        await renderDealerResolution(data.state, data.balance);
        return;
    }

    renderGameState(data.state, data.balance);
}

async function startGame() {
    if (state.busy) return;
    if (state.bet <= 0) {
        alert('Wybierz zaklad przed rozdaniem.');
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
        await renderActionResponse(data);
    } catch (e) {
        alert(e.message);
    } finally {
        setBusy(false);
    }
}

function buildDoublePreview(finalState) {
    const preview = JSON.parse(JSON.stringify(finalState));
    const useSplit = state.game && state.game.activeHand === 'split';
    const handKey = useSplit ? 'splitHand' : 'playerHand';
    const hand = preview[handKey];

    if (hand && hand.length > 0) {
        hand[hand.length - 1] = { suit: '', rank: '?', faceDown: true };
    }

    preview.isActive = true;
    preview.isPlayerTurn = true;
    preview.dealerHand = state.game ? state.game.dealerHand : preview.dealerHand;
    preview.dealerValue = state.game ? state.game.dealerValue : preview.dealerValue;

    return { preview, useSplit };
}

async function doDouble(faceDown) {
    if (state.busy || !state.gameId) return;
    setBusy(true);
    setDoubleChoices(false);

    try {
        const data = await apiPost('double', { gameId: state.gameId, faceDown });

        if (faceDown) {
            const { preview, useSplit } = buildDoublePreview(data.state);
            renderGameState(preview);
            await wait(TIMING.doublePeek);

            const revealState = buildDealerPreview(data.state);
            renderGameState(revealState, undefined, {
                revealMainLast: !useSplit,
                revealSplitLast: useSplit,
                suppressResult: true
            });
            await wait(TIMING.revealDealer);
            await renderDealerResolution(data.state, data.balance);
        } else {
            const preview = buildDealerPreview(data.state);
            renderGameState(preview, undefined, { revealMainLast: true, suppressResult: true });
            await wait(TIMING.doublePeek);
            await renderDealerResolution(data.state, data.balance);
        }
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
    ensureDoubleChoices();
    fetchBalance();

    document.querySelectorAll('.bj-chip').forEach(chip => {
        chip.addEventListener('click', () => addChip(parseInt(chip.dataset.value)));
    });

    $('clear-bet-btn').addEventListener('click', clearBet);
    $('deal-btn').addEventListener('click', startGame);

    $('btn-hit').addEventListener('click', () => doAction('hit'));
    $('btn-stand').addEventListener('click', () => doAction('stand'));
    $('btn-double').addEventListener('click', () => setDoubleChoices($('double-choice').style.display !== 'inline-flex'));
    $('btn-split').addEventListener('click', () => doAction('split'));

    $('btn-new-game').addEventListener('click', () => {
        showBetArea();
        clearBet();
    });
});
