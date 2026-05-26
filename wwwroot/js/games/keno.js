let kenoBusy = false;
const selectedNumbers = new Set();

const kenoGrid = document.getElementById("keno-grid");
const kenoBet = document.getElementById("keno-bet");
const kenoDrawButton = document.getElementById("keno-draw");
const kenoClearButton = document.getElementById("keno-clear");
const kenoPicked = document.getElementById("keno-picked");
const kenoHits = document.getElementById("keno-hits");
const kenoStatus = document.getElementById("keno-status");
const kenoWin = document.getElementById("keno-win");

function wait(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function updateBalance(balance) {
    const balanceDisplay = document.getElementById("balance-display");
    if (!balanceDisplay || balance === undefined || balance === null) return;
    balanceDisplay.innerText = Number(balance).toFixed(2);
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

function updatePicked() {
    kenoPicked.textContent = `${selectedNumbers.size}/10`;
    kenoStatus.textContent = selectedNumbers.size === 0 ? "Wybierz liczby" : "Gotowy do losowania";
}

function renderGrid() {
    kenoGrid.innerHTML = "";

    for (let number = 1; number <= 40; number++) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "keno-cell";
        button.textContent = number;
        button.dataset.number = number;
        button.addEventListener("click", () => toggleNumber(number, button));
        kenoGrid.appendChild(button);
    }
}

function toggleNumber(number, button) {
    if (kenoBusy) return;

    if (selectedNumbers.has(number)) {
        selectedNumbers.delete(number);
        button.classList.remove("keno-cell-selected");
    } else {
        if (selectedNumbers.size >= 10) return;
        selectedNumbers.add(number);
        button.classList.add("keno-cell-selected");
    }

    updatePicked();
}

function clearBoard() {
    if (kenoBusy) return;

    selectedNumbers.clear();
    document.querySelectorAll(".keno-cell").forEach(cell => {
        cell.classList.remove("keno-cell-selected", "keno-cell-drawn", "keno-cell-hit", "keno-cell-drawing");
    });
    kenoHits.textContent = "-";
    kenoWin.textContent = "-";
    updatePicked();
}

async function animateResults(drawnNumbers, selected) {
    const picked = new Set(selected);
    const cellsByNumber = new Map();

    document.querySelectorAll(".keno-cell").forEach(cell => {
        cell.classList.remove("keno-cell-drawn", "keno-cell-hit", "keno-cell-drawing");
        cellsByNumber.set(Number(cell.dataset.number), cell);
    });

    for (let index = 0; index < drawnNumbers.length; index++) {
        const number = Number(drawnNumbers[index]);
        const cell = cellsByNumber.get(number);
        if (!cell) continue;

        cell.classList.add("keno-cell-drawing");
        await wait(90);
        cell.classList.remove("keno-cell-drawing");
        cell.classList.add(picked.has(number) ? "keno-cell-hit" : "keno-cell-drawn");
        kenoStatus.textContent = `Losowanie ${index + 1}/10`;
        await wait(70);
    }
}

async function drawKeno() {
    if (kenoBusy) return;

    kenoBusy = true;
    kenoDrawButton.disabled = true;
    kenoStatus.textContent = "Losowanie...";
    kenoWin.textContent = "-";

    try {
        const numbers = Array.from(selectedNumbers).sort((a, b) => a - b);
        const res = await fetch("/api/keno/draw", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                bet: Number(kenoBet.value),
                numbers
            })
        });
        const data = await parseResponse(res);

        updateBalance(data.balance);
        await animateResults(data.drawnNumbers, data.selectedNumbers);
        kenoHits.textContent = data.hits;
        kenoStatus.textContent = `Mnoznik x${Number(data.multiplier).toFixed(2)}`;
        kenoWin.textContent = Number(data.win) > 0 ? `Wygrana: ${Number(data.win).toFixed(2)}` : "Brak wygranej";
    } catch (err) {
        console.error(err);
        kenoStatus.textContent = err.message || "Nie udalo sie zagrac.";
    } finally {
        kenoBusy = false;
        kenoDrawButton.disabled = false;
    }
}

kenoDrawButton.addEventListener("click", drawKeno);
kenoClearButton.addEventListener("click", clearBoard);
renderGrid();
updatePicked();
