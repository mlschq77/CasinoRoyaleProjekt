let diceMode = "over";
let diceBusy = false;

const diceBet = document.getElementById("dice-bet");
const diceTarget = document.getElementById("dice-target");
const diceTargetValue = document.getElementById("dice-target-value");
const diceChance = document.getElementById("dice-chance");
const diceMultiplier = document.getElementById("dice-multiplier");
const diceRollButton = document.getElementById("dice-roll");
const diceStatus = document.getElementById("dice-status");
const diceWin = document.getElementById("dice-win");
const diceMarker = document.getElementById("dice-marker");
const diceModeButtons = document.querySelectorAll(".dice-mode");

function setDiceMarkerPosition(roll) {
    const markerSize = diceMarker.offsetWidth || 84;
    const meter = diceMarker.closest(".dice-meter");
    const meterWidth = meter?.clientWidth || 1;
    const safePadding = markerSize / 2 + 10;
    const usableWidth = Math.max(1, meterWidth - safePadding * 2);
    const clampedRoll = Math.min(100, Math.max(1, Number(roll)));
    const left = safePadding + usableWidth * ((clampedRoll - 1) / 99);

    diceMarker.style.left = `${left}px`;
}



async function parseResponse(res) {
    const text = await res.text();
    let data = null;

    if (text) {
        try {
            data = JSON.parse(text);
        } catch {
            data = { error: text };
        }
    }

    if (!res.ok) {
        throw new Error(data?.error || "Request failed");
    }

    return data || {};
}

function updateDicePreview() {
    const target = Number(diceTarget.value);
    const chance = diceMode === "over" ? 100 - target : target - 1;
    const multiplier = 100 / chance * 0.99;

    diceTargetValue.textContent = target;
    diceChance.textContent = `${chance}%`;
    diceMultiplier.textContent = `${multiplier.toFixed(2)}x`;
}

function setMode(mode) {
    diceMode = mode;
    diceModeButtons.forEach(button => {
        const active = button.dataset.mode === diceMode;
        button.classList.toggle("casino-btn-primary", active);
        button.classList.toggle("btn-outline-light", !active);
        button.classList.toggle("casino-btn-secondary", !active);
    });
    updateDicePreview();
}

async function rollDice() {
    if (diceBusy) return;

    diceBusy = true;
    diceRollButton.disabled = true;
    diceStatus.textContent = "Rzut trwa...";
    diceWin.textContent = "-";
    diceMarker.textContent = "?";
    diceMarker.classList.remove("dice-marker-win");
    setDiceMarkerPosition(50);

    try {
        const res = await fetch("/api/dice/roll", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                bet: Number(diceBet.value),
                mode: diceMode,
                target: Number(diceTarget.value)
            })
        });
        const data = await parseResponse(res);

        updateBalanceDisplay({ balance: data.balance, balanceBonus: data.balanceBonus });
        diceMarker.textContent = data.roll;
        setDiceMarkerPosition(data.roll);

        const won = Number(data.win) > 0;
        diceStatus.textContent = won ? `Trafione ${data.mode} ${data.target}` : `Wypadlo ${data.roll}`;
        diceWin.textContent = won ? `Wygrana: ${Number(data.win).toFixed(2)}` : "Brak wygranej";
        diceMarker.classList.toggle("dice-marker-win", won);
    } catch (err) {
        console.error(err);
        diceStatus.textContent = err.message || "Nie udalo sie zagrac.";
    } finally {
        diceBusy = false;
        diceRollButton.disabled = false;
    }
}

diceTarget.addEventListener("input", updateDicePreview);
diceRollButton.addEventListener("click", rollDice);
diceModeButtons.forEach(button => button.addEventListener("click", () => setMode(button.dataset.mode)));
window.addEventListener("resize", () => {
    const markerValue = diceMarker.textContent === "?" ? 50 : Number(diceMarker.textContent);
    setDiceMarkerPosition(Number.isFinite(markerValue) ? markerValue : 50);
});

updateDicePreview();
setDiceMarkerPosition(50);
