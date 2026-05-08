// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


async function refreshBalance() {
    const balanceDisplay = document.getElementById("balance-display");
    if (!balanceDisplay) return;

    try {
        const response = await fetch("/api/balance");
        if (!response.ok) return;

        const data = await response.json();
        if (data.balance !== undefined && data.balance !== null) {
            balanceDisplay.innerText = Number(data.balance).toFixed(2);
        }
    } catch (error) {
        console.debug("Balance refresh failed", error);
    }
}

refreshBalance();