let gameId = null;

function createGrid() {
    const grid = document.getElementById("grid");
    grid.innerHTML = "";

    for (let i = 0; i < 25; i++) {
        const tile = document.createElement("div");
        tile.classList.add("tile");

        tile.onclick = () => clickTile(i, tile);

        grid.appendChild(tile);
    }
}

async function startGame() {
    const bet = document.getElementById("bet").value;
    const mines = document.getElementById("mines").value;

    try {
        const res = await fetch(`/api/mines/start?mineCount=${mines}&bet=${bet}`, {
            method: "POST"
        });

        const data = await res.json();

        gameId = data.id;

        createGrid();

        document.getElementById("status").innerText = "Game started 🎰";
    }
    catch (err) {
        console.error(err);
        document.getElementById("status").innerText = "Error starting game";
    }
}

async function clickTile(position, tile) {
    if (!gameId) return;

    tile.onclick = null;

    try {
        const res = await fetch(`/api/mines/click?gameId=${gameId}&position=${position}`, {
            method: "POST"
        });

        const data = await res.json();

        if (data.result === "lose") {
            tile.classList.add("mine");
            tile.innerText = "💣";

            revealMines();

            document.getElementById("status").innerText = "💥 You lost!";
        } else {
            tile.classList.add("safe");
            tile.innerText = "✅";
        }
    }
    catch (err) {
        console.error(err);
        document.getElementById("status").innerText = "Error...";
    }
}

async function cashout() {
    if (!gameId) return;

    try {
        const res = await fetch(`/api/mines/cashout?gameId=${gameId}`, {
            method: "POST"
        });

        const data = await res.json();

        await revealMines();

        document.getElementById("status").innerText =
            `💰 Win: ${data.win} (x${data.multiplier})`;

        gameId = null;
    }
    catch (err) {
        console.error(err);
        document.getElementById("status").innerText = "Error...";
    }
}

async function revealMines() {
    try {
        const res = await fetch(`/api/mines/reveal?gameId=${gameId}`);
        const data = await res.json();

        const mines = data.mines;
        const tiles = document.querySelectorAll(".tile");

        mines.forEach(pos => {
            tiles[pos].classList.add("mine");
            tiles[pos].innerText = "💣";
        });
    } catch (err) {
        console.error(err);
    }
}
