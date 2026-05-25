// Crash Game - Main JavaScript
const GROWTH_RATE = 0.06;
const POLL_INTERVAL = 250; // ms - polling co 250ms dla lepszej responsywności
const DISPLAY_DELAY = 0.20; // sekund - opóźnienie wyświetlanego mnożnika względem serwera,
                           // aby auto-wypłata nigdy nie zadziałała po faktycznym crashu

let state = {
    gameId: null,
    startTime: null,
    clientStartTime: null, // kliencki timer - startuje gdy gra zaczyna się na ekranie
    isPlaying: false,
    hasCrashed: false,
    autoCashoutMultiplier: null,
    animationFrame: null,
    pollInterval: null,
    chartData: [],
    chartCanvas: null,
    chartCtx: null,
    chartWidth: 700,
    chartHeight: 400
};

function $(id) { return document.getElementById(id); }

function updateBalance(balance) {
    const el = document.getElementById("balance-display");
    if (el && balance !== undefined && balance !== null) {
        el.innerText = Number(balance).toFixed(2);
    }
}

async function parseResponse(res) {
    const text = await res.text();
    let data = null;
    if (text) {
        try { data = JSON.parse(text); } catch { data = { error: text }; }
    }
    if (!res.ok) throw new Error(data?.error || "Request failed");
    return data || {};
}

function calculateMultiplier(seconds) {
    return Math.exp(GROWTH_RATE * seconds);
}

// --- Canvas rendering ---

function initCanvas() {
    state.chartCanvas = $('crash-chart');
    if (!state.chartCanvas) return;
    state.chartCtx = state.chartCanvas.getContext('2d');
    state.chartWidth = state.chartCanvas.width;
    state.chartHeight = state.chartCanvas.height;
}

function drawChart() {
    const ctx = state.chartCtx;
    if (!ctx) return;
    const w = state.chartWidth;
    const h = state.chartHeight;

    // Clear
    ctx.clearRect(0, 0, w, h);

    // Background
    const bg = ctx.createLinearGradient(0, 0, 0, h);
    bg.addColorStop(0, 'rgba(17, 17, 17, 0.95)');
    bg.addColorStop(1, 'rgba(5, 5, 5, 0.95)');
    ctx.fillStyle = bg;
    ctx.fillRect(0, 0, w, h);

    if (state.chartData.length < 2) return;

    // Grid lines
    ctx.strokeStyle = 'rgba(212, 175, 55, 0.08)';
    ctx.lineWidth = 1;
    for (let i = 0; i <= 5; i++) {
        const y = (h / 5) * i;
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(w, y);
        ctx.stroke();
    }

    // Max multiplier for scaling
    const maxMultiplier = Math.max(2.0, ...state.chartData.map(d => d.multiplier)) * 1.15;

    // Line segments with glow
    ctx.shadowColor = 'rgba(212, 175, 55, 0.4)';
    ctx.shadowBlur = 10;
    ctx.strokeStyle = '#d4af37';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(0, h);

    for (let i = 0; i < state.chartData.length; i++) {
        const x = (state.chartData[i].time / state.chartData[state.chartData.length - 1].time) * w;
        const y = h - (state.chartData[i].multiplier / maxMultiplier) * h * 0.85;
        ctx.lineTo(x, y);
    }
    ctx.stroke();

    // Fill area under curve
    ctx.shadowBlur = 0;
    ctx.lineTo(w, h);
    ctx.closePath();
    const gradient = ctx.createLinearGradient(0, 0, 0, h);
    gradient.addColorStop(0, 'rgba(212, 175, 55, 0.2)');
    gradient.addColorStop(1, 'rgba(212, 175, 55, 0.02)');
    ctx.fillStyle = gradient;
    ctx.fill();
}

// --- Red flash on crash ---
function showCrashEffect() {
    const displayArea = $('crash-display-area');
    if (!displayArea) return;

    displayArea.classList.remove('crash-crash-effect');
    // Force reflow
    void displayArea.offsetWidth;
    displayArea.classList.add('crash-crash-effect');

    // Remove class after animation
    setTimeout(() => displayArea.classList.remove('crash-crash-effect'), 800);
}

// --- Game logic ---

async function startGame() {
    if (state.isPlaying) return;

    const bet = $('betAmount').value;
    if (!bet || parseFloat(bet) <= 0) {
        showStatus('Wprowadz poprawna stawke.');
        return;
    }

    const autoCashout = parseFloat($('autoCashout').value) || null;

    setButtonsDisabled(true, true);

    try {
        const res = await fetch(`/api/crash/start?bet=${encodeURIComponent(bet)}`, { method: 'POST' });
        const data = await parseResponse(res);

        if (!data.success) {
            showStatus(data.error || 'Nie udalo sie rozpoczac gry.');
            setButtonsDisabled(false, true);
            return;
        }

        state.gameId = data.sessionId;
        state.startTime = new Date(data.startTime); // czas serwera do walidacji
        state.clientStartTime = new Date(); // własny timer - od tego momentu liczymy czas na ekranie
        state.isPlaying = true;
        state.hasCrashed = false;
        state.autoCashoutMultiplier = autoCashout;
        state.chartData = [{ time: 0, multiplier: 1.0 }];

        updateBalance(data.balance);
        showStatus('Gra trwa...');
        $('crash-win').textContent = '';

        // Update canvas size
        const container = state.chartCanvas?.parentElement;
        if (container && state.chartCanvas) {
            const rect = container.getBoundingClientRect();
            state.chartCanvas.width = rect.width || 700;
            state.chartHeight = 400;
            state.chartWidth = state.chartCanvas.width;
        }

        initCanvas();
        setButtonsDisabled(false, false);

        // Start animation loop
        animateMultiplier();

        // Start polling
        state.pollInterval = setInterval(checkStatus, POLL_INTERVAL);

    } catch (err) {
        console.error(err);
        showStatus(err.message || 'Blad serwera');
        setButtonsDisabled(false, true);
    }
}

function animateMultiplier() {
    if (!state.isPlaying) return;

    const now = new Date();
    const timeElapsedSeconds = (now - state.clientStartTime) / 1000;

    // Opóźniamy wyświetlany mnożnik o DISPLAY_DELAY sekund względem serwera.
    // Dzięki temu mnożnik na ekranie gracza jest zawsze nieco "z tyłu" za rzeczywistym
    // stanem na serwerze. Jeśli serwer zcrashuje przy 1.19x, klient będzie wtedy
    // pokazywał ~1.17x — auto-wypłata na 1.20x nie zdąży się odpalić.
    const displaySeconds = Math.max(0, timeElapsedSeconds - DISPLAY_DELAY);
    const multiplier = calculateMultiplier(displaySeconds);

    // Update display
    const display = $('multiplierDisplay');
    if (display) {
        display.textContent = multiplier.toFixed(2) + 'x';
        // Color changes as multiplier grows
        if (multiplier >= 5) display.style.color = '#ff6b35';
        else if (multiplier >= 3) display.style.color = '#f3d67a';
        else display.style.color = '#d4af37';
    }

    // Record data for chart
    state.chartData.push({
        time: timeElapsedSeconds,
        multiplier: multiplier
    });

    // Limit data points for performance
    if (state.chartData.length > 500) {
        state.chartData = state.chartData.slice(-250);
    }

    // Draw chart
    drawChart();

    // Auto cashout check — tylko jeśli gra jeszcze nie wybuchła
    if (state.autoCashoutMultiplier && multiplier >= state.autoCashoutMultiplier && !state.hasCrashed) {
        cashout();
        return;
    }

    state.animationFrame = requestAnimationFrame(animateMultiplier);
}

async function cashout() {
    if (!state.isPlaying || !state.gameId) return;

    // Zapisz mnożnik wyświetlany w momencie kliknięcia
    const display = $('multiplierDisplay');
    const clientMultiplier = display ? parseFloat(display.textContent) || null : null;

    state.isPlaying = false;
    if (state.animationFrame) cancelAnimationFrame(state.animationFrame);
    if (state.pollInterval) clearInterval(state.pollInterval);

    try {
        const res = await fetch(`/api/crash/cashout?sessionId=${state.gameId}&clientMultiplier=${clientMultiplier}`, { method: 'POST' });
        const data = await parseResponse(res);

        if (data.won) {
            showStatus(`Wygrałeś! Wypłacono przy ${Number(data.multiplier).toFixed(2)}x (crash przy ${Number(data.crashPoint).toFixed(2)}x)`);
            $('crash-win').textContent = `+${Number(data.winAmount).toFixed(2)} PLN`;
            $('crash-win').className = 'crash-result-win';
            updateBalance(data.balance);
            addToHistory(Number(data.winAmount).toFixed(2), Number(data.multiplier).toFixed(2), true, Number(data.crashPoint).toFixed(2));
        } else {
            showStatus(`Crash! Wykres załamał się przy ${Number(data.crashPoint).toFixed(2)}x`);
            $('crash-win').textContent = 'Przegrana';
            $('crash-win').className = 'crash-result-lose';
            showCrashEffect();
            if (data.balance !== undefined) updateBalance(data.balance);
            addToHistory('CRASH', Number(data.crashPoint).toFixed(2), false);
        }



    } catch (err) {
        console.error(err);
        showStatus(err.message || 'Blad podczas wyplaty');
    } finally {
        stopGame();
    }
}

async function checkStatus() {
    if (!state.isPlaying || !state.gameId) return;
    if (state.hasCrashed) return;

    try {
        const res = await fetch(`/api/crash/status?sessionId=${state.gameId}`);
        const data = await parseResponse(res);

        if (data.crashed) {
            state.hasCrashed = true;
            state.isPlaying = false;
            if (state.animationFrame) cancelAnimationFrame(state.animationFrame);
            if (state.pollInterval) clearInterval(state.pollInterval);

            showStatus(`CRASH! Wykres załamał się przy ${Number(data.crashPoint).toFixed(2)}x`);
            showCrashEffect();
            addToHistory('CRASH', Number(data.crashPoint).toFixed(2), false);

            const display = $('multiplierDisplay');
            if (display) {
                display.textContent = Number(data.crashPoint).toFixed(2) + 'x';
                display.style.color = '#ef4444';
            }

            stopGame();
        }
    } catch (err) {
        console.error(err);
    }
}

function stopGame() {
    // Reload balance after game ends
    fetchBalance();
    setButtonsDisabled(false, true);
}

function setButtonsDisabled(startDisabled, cashoutDisabled) {
    const startBtn = $('startBtn');
    const cashoutBtn = $('cashoutBtn');
    if (startBtn) startBtn.disabled = startDisabled;
    if (cashoutBtn) cashoutBtn.disabled = cashoutDisabled;
}

function showStatus(msg) {
    const el = $('crash-status');
    if (el) el.textContent = msg;
}

async function fetchBalance() {
    try {
        const res = await fetch('/api/balance');
        if (res.ok) {
            const data = await res.json();
            updateBalance(data.balance);
        }
    } catch (err) { /* ignore */ }
}

// --- History ---
function addToHistory(win, multiplier, won, crashPoint) {
    const list = $('crash-history-list');
    if (!list) return;

    const item = document.createElement('div');
    item.className = `crash-history-item ${won ? 'crash-history-win' : 'crash-history-lose'}`;

    const icon = won ? '✓' : '✕';
    const color = won ? '#22c55e' : '#ef4444';

    item.innerHTML = `
        <span class="crash-history-icon" style="color:${color}">${icon}</span>
        <span class="crash-history-multiplier">${multiplier}x</span>
        <span class="crash-history-crashpoint">${won ? '~' + crashPoint + 'x' : ''}</span>
        <span class="crash-history-winamount">${won ? win + ' PLN' : ''}</span>
    `;

    list.insertBefore(item, list.firstChild);

    // Limit history to 20 items
    while (list.children.length > 20) {
        list.removeChild(list.lastChild);
    }
}

// --- Window resize ---
window.addEventListener('resize', () => {
    if (state.chartCanvas && state.isPlaying) {
        const container = state.chartCanvas.parentElement;
        if (container) {
            const rect = container.getBoundingClientRect();
            state.chartCanvas.width = rect.width || 700;
            state.chartWidth = state.chartCanvas.width;
        }
    }
});

// --- Init ---
document.addEventListener('DOMContentLoaded', () => {
    initCanvas();
    fetchBalance();
    drawChart(); // Draw empty chart
});
