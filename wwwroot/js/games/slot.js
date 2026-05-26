const slotSymbols = [
    { id: "cherry", icon: "🍒" },
    { id: "lemon", icon: "🍋" },
    { id: "plum", icon: "🍇" },
    { id: "watermelon", icon: "🍉" },
    { id: "bell", icon: "🔔" },
    { id: "seven", icon: "7" }
];

let slotBusy = false;

const slotBet = document.getElementById("slot-bet");
const slotSpinButton = document.getElementById("slot-spin");
const slotStatus = document.getElementById("slot-status");
const slotWin = document.getElementById("slot-win");
const slotReels = [...document.querySelectorAll(".slot-reel")];
const quickBetButtons = document.querySelectorAll("[data-bet]");

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

function buildStrip(finalIcon, reelIndex) {
    const pool = [];
    for (let i = 0; i < 22 + reelIndex * 5; i++) {
        pool.push(slotSymbols[(i + reelIndex * 2) % slotSymbols.length].icon);
    }

    pool.push(finalIcon);
    return pool.map(icon => `<span>${icon}</span>`).join("");
}

function settleReel(reel, finalIcon, reelIndex) {
    const strip = reel.querySelector(".slot-strip");
    strip.innerHTML = buildStrip(finalIcon, reelIndex);
    strip.style.transition = "none";
    strip.style.transform = "translateY(0)";
    reel.classList.remove("slot-reel-win");

    requestAnimationFrame(() => {
        const itemHeight = strip.querySelector("span")?.offsetHeight || 126;
        const target = -(strip.children.length - 1) * itemHeight;
        strip.style.transition = `transform ${1.05 + reelIndex * 0.32}s cubic-bezier(.14,.82,.18,1)`;
        strip.style.transform = `translateY(${target}px)`;
    });
}

function setIdleSpin() {
    slotReels.forEach((reel, reelIndex) => {
        const strip = reel.querySelector(".slot-strip");
        strip.innerHTML = buildStrip(slotSymbols[(reelIndex + 1) % slotSymbols.length].icon, reelIndex);
        strip.style.transition = "none";
        strip.style.transform = "translateY(0)";
    });
}

function highlightWin(reels, won) {
    slotReels.forEach((reel, index) => {
        const sameAsAnyPair = reels.filter(symbol => symbol.id === reels[index].id).length > 1;
        reel.classList.toggle("slot-reel-win", won && sameAsAnyPair);
    });
}

async function spinSlot() {
    if (slotBusy) return;

    slotBusy = true;
    slotSpinButton.disabled = true;
    slotStatus.textContent = "Bebny sie kreca...";
    slotWin.textContent = "-";
    slotReels.forEach(reel => reel.classList.remove("slot-reel-win"));

    try {
        const res = await fetch("/api/slot/spin", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ bet: Number(slotBet.value) })
        });
        const data = await parseResponse(res);
        const reels = data.reels || [];

        reels.forEach((symbol, index) => settleReel(slotReels[index], symbol.icon, index));
        updateBalance(data.balance);

        window.setTimeout(() => {
            const win = Number(data.win);
            const multiplier = Number(data.multiplier);
            const won = win > 0;

            highlightWin(reels, won);
            slotStatus.textContent = won
                ? `Trafione ${multiplier.toFixed(2)}x`
                : "Tym razem bez wygranej";
            slotWin.textContent = won ? `Wygrana: ${win.toFixed(2)}` : "Brak wygranej";
            slotBusy = false;
            slotSpinButton.disabled = false;
        }, 1900);
    } catch (err) {
        console.error(err);
        slotStatus.textContent = err.message || "Nie udalo sie zagrac.";
        slotBusy = false;
        slotSpinButton.disabled = false;
    }
}

quickBetButtons.forEach(button => {
    button.addEventListener("click", () => {
        slotBet.value = button.dataset.bet;
    });
});

slotSpinButton.addEventListener("click", spinSlot);
setIdleSpin();
