// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

/**
 * Aktualizuje wyświetlanie bonusu w navbarze.
 * Wywoływana z gier po otrzymaniu odpowiedzi z API.
 */
function updateBonusDisplay(balanceBonus) {
    const bonusDisplay = document.getElementById("balance-bonus-display");
    if (!bonusDisplay) return;

    if (balanceBonus !== undefined && balanceBonus !== null && Number(balanceBonus) > 0) {
        bonusDisplay.innerText = "+" + Number(balanceBonus).toFixed(2) + " bonus";
        bonusDisplay.classList.remove("d-none");
    } else {
        bonusDisplay.classList.add("d-none");
    }
}

/**
 * Aktualizuje cały wyświetlacz balansu (główne saldo + bonus) na podstawie odpowiedzi z API.
 * Przyjmuje obiekt z polami: balance, balanceBonus.
 */
function updateBalanceDisplay(data) {
    const balanceDisplay = document.getElementById("balance-display");
    if (!balanceDisplay) return;

    // Użyj balanceReal jeśli dostępne (z /api/balance), inaczej oblicz z balance - balanceBonus (z API gier)
    let realAmount;
    if (data.balanceReal !== undefined && data.balanceReal !== null) {
        realAmount = data.balanceReal;
    } else if (data.balance !== undefined && data.balance !== null) {
        realAmount = Number(data.balance) - Number(data.balanceBonus || 0);
    } else {
        return;
    }

    balanceDisplay.innerText = Number(realAmount).toFixed(2);
    updateBonusDisplay(data.balanceBonus);
}

async function refreshBalance() {
    const balanceDisplay = document.getElementById("balance-display");
    if (!balanceDisplay) return;

    try {
        const response = await fetch("/api/balance");
        if (!response.ok) return;

        const data = await response.json();
        updateBalanceDisplay(data);
    } catch (error) {
        console.debug("Balance refresh failed", error);
    }
}

refreshBalance();

// Odświeżaj balans co 15 sekund
setInterval(refreshBalance, 15000);