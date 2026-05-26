const plinkoRows = 8;
let isDropping = false;

const board = document.getElementById("plinko-board");
const pegs = document.getElementById("plinko-pegs");
const ball = document.getElementById("plinko-ball");
const slots = document.getElementById("plinko-slots");
const playButton = document.getElementById("plinko-play");
const betInput = document.getElementById("plinko-bet");
const riskInput = document.getElementById("plinko-risk");
const statusText = document.getElementById("plinko-status");
const winText = document.getElementById("plinko-win");

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

function getBoardMetrics() {
    const rect = board.getBoundingClientRect();
    return {
        width: rect.width,
        height: rect.height,
        rowGap: rect.height / (plinkoRows + 1),
        slotWidth: rect.width / (plinkoRows + 1)
    };
}

function renderPegs() {
    pegs.innerHTML = "";
    const metrics = getBoardMetrics();

    for (let row = 0; row < plinkoRows; row++) {
        const pegCount = row + 2;
        const y = metrics.rowGap * (row + 1);

        for (let col = 0; col < pegCount; col++) {
            const x = metrics.width / 2 + (col - (pegCount - 1) / 2) * metrics.slotWidth;
            const peg = document.createElement("span");
            peg.className = "plinko-peg";
            peg.style.left = `${x}px`;
            peg.style.top = `${y}px`;
            pegs.appendChild(peg);
        }
    }
}

async function renderMultipliers() {
    const risk = riskInput.value;

    try {
        const res = await fetch(`/api/plinko/multipliers?risk=${encodeURIComponent(risk)}`);
        const data = await parseResponse(res);

        slots.innerHTML = "";
        data.multipliers.forEach(multiplier => {
            const slot = document.createElement("div");
            slot.className = "plinko-slot";
            slot.textContent = `${Number(multiplier).toFixed(1)}x`;
            slots.appendChild(slot);
        });
    } catch (err) {
        console.error(err);
    }
}

function setBallPosition(x, y) {
    ball.style.transform = `translate(${x}px, ${y}px) translate(-50%, -50%)`;
}

function wait(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function animateDrop(path, landingSlot) {
    const metrics = getBoardMetrics();
    let slot = plinkoRows / 2;

    ball.hidden = false;
    ball.classList.remove("plinko-ball-win");
    setBallPosition(metrics.width / 2, 14);
    await wait(120);

    for (let row = 0; row < path.length; row++) {
        slot += path[row] === "R" ? 0.5 : -0.5;
        const x = metrics.width / 2 + (slot - plinkoRows / 2) * metrics.slotWidth;
        const y = metrics.rowGap * (row + 1);

        ball.style.transition = "transform 180ms cubic-bezier(.2,.8,.2,1.15)";
        setBallPosition(x, y);
        await wait(190);
    }

    ball.style.transition = "transform 260ms cubic-bezier(.25,.8,.25,1)";
    setBallPosition((landingSlot + 0.5) * metrics.slotWidth, metrics.height - 12);
    await wait(280);
    ball.classList.add("plinko-ball-win");
}

async function playPlinko() {
    if (isDropping) return;

    isDropping = true;
    playButton.disabled = true;
    statusText.textContent = "Kulka spada...";
    winText.textContent = "-";

    try {
        const bet = betInput.value;
        const risk = riskInput.value;
        const res = await fetch(`/api/plinko/play?bet=${encodeURIComponent(bet)}&risk=${encodeURIComponent(risk)}`, {
            method: "POST"
        });
        const data = await parseResponse(res);

        updateBalanceDisplay(data);
        await animateDrop(data.path, data.landingSlot);

        const multiplier = Number(data.multiplier).toFixed(2);
        const win = Number(data.win).toFixed(2);
        statusText.textContent = `Mnoznik x${multiplier}`;
        winText.textContent = `Wygrana: ${win}`;

        document.querySelectorAll(".plinko-slot").forEach((slot, index) => {
            slot.classList.toggle("plinko-slot-hit", index === data.landingSlot);
        });
    } catch (err) {
        console.error(err);
        statusText.textContent = err.message || "Nie udalo sie zagrac.";
    } finally {
        isDropping = false;
        playButton.disabled = false;
    }
}

window.addEventListener("resize", renderPegs);
riskInput.addEventListener("change", renderMultipliers);
playButton.addEventListener("click", playPlinko);

renderPegs();
renderMultipliers();
