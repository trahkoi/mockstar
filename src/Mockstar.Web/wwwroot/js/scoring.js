(function () {
  const header = document.getElementById("scoring-header");
  const roleSwitcher = document.getElementById("role-switcher");
  const pairingPanel = document.getElementById("pairing-panel");
  const scoreboard = document.getElementById("scoreboard");
  const finalizeButton = document.getElementById("finalize-button");

  if (!header || !roleSwitcher || !pairingPanel || !scoreboard || !finalizeButton || !window.MockstarState) {
    return;
  }

  let activeRole = "couple";

  function getActiveContext() {
    const state = window.MockstarState.loadState();
    const heat = window.MockstarState.getHeatById(state, state.selectedHeatId);
    if (!heat) {
      return { state, heat: null, sheet: null };
    }

    const roles = availableRoles(heat);
    if (!roles.includes(activeRole)) {
      activeRole = roles[0];
    }

    return {
      state,
      heat,
      sheet: window.MockstarState.getScoreSheet(state, heat.id, activeRole)
    };
  }

  function availableRoles(heat) {
    if (heat.type === "jack-and-jill-prelim") {
      return [
        heat.leaderEntries.length ? "leader" : null,
        heat.followerEntries.length ? "follower" : null
      ].filter(Boolean);
    }

    return ["couple"];
  }

  function renderRoleSwitcher(heat) {
    roleSwitcher.innerHTML = availableRoles(heat).map((role) => `
      <button type="button" class="placeholder-chip ${role === activeRole ? "active-chip" : ""}" data-role="${role}">
        ${role}
      </button>
    `).join("");

    roleSwitcher.querySelectorAll("[data-role]").forEach((button) => {
      button.addEventListener("click", function () {
        activeRole = this.dataset.role;
        render();
      });
    });
  }

  function renderPairingPanel(heat) {
    if (heat.type !== "jack-and-jill-final" || !heat.followerEntries.length) {
      pairingPanel.innerHTML = "";
      return;
    }

    pairingPanel.innerHTML = `
      <article class="placeholder-card">
        <h2>Pair finals</h2>
        <div class="placeholder-layout pairing-grid">
          <label class="field">
            <span>Leader bib</span>
            <select id="pair-leader">
              ${heat.leaderEntries.map((entry) => `<option value="${entry.bib}">${entry.display}</option>`).join("")}
            </select>
          </label>
          <label class="field">
            <span>Follower bib</span>
            <select id="pair-follower">
              ${heat.followerEntries.map((entry) => `<option value="${entry.bib}">${entry.display}</option>`).join("")}
            </select>
          </label>
          <button id="pair-submit" type="button" class="button button-secondary">Link pair</button>
        </div>
      </article>
    `;

    document.getElementById("pair-submit").addEventListener("click", function () {
      const leaderBib = Number(document.getElementById("pair-leader").value);
      const followerBib = Number(document.getElementById("pair-follower").value);
      window.MockstarState.pairFinalHeat(heat.id, leaderBib, followerBib);
      render();
    });
  }

  function renderHeader(heat, sheet) {
    header.innerHTML = `
      <div class="eyebrow">${heat.type.replaceAll("-", " ")}</div>
      <h2>${heat.name}</h2>
      <p class="placeholder-copy">Scoring role: ${sheet.role}. Status: ${sheet.status}${sheet.finalizedAt ? ` at ${sheet.finalizedAt}` : ""}</p>
    `;
  }

  function renderRows(heat, sheet) {
    const entries = window.MockstarState.getEntriesForRole(heat, sheet.role);
    const ranked = window.MockstarState.rankEntries(heat, entries, sheet.scores);
    const ranking = new Map(ranked.map((entry, index) => [entry.id, index + 1]));

    scoreboard.innerHTML = entries.map((entry) => `
      <article
        class="score-row"
        data-entry-id="${entry.id}"
        _="on scorestate if event.detail.tiedIds.includes(my @data-entry-id) add .is-tied else remove .is-tied end">
        <div>
          <strong>${window.MockstarState.entryDisplay(heat, entry)}</strong>
          <div class="placeholder-copy">Rank <span class="rank-value">${ranking.get(entry.id)}</span></div>
        </div>
        <label class="score-input">
          <input
            class="score-slider"
            type="range"
            min="0"
            max="1000"
            value="${sheet.scores[entry.id] ?? 500}"
            data-entry-id="${entry.id}"
            ${sheet.status === "finalized" ? "disabled" : ""}
            _="on input call window.MockstarScoring.onSliderInput(me)">
          <span class="score-value">${sheet.scores[entry.id] ?? 500}</span>
        </label>
      </article>
    `).join("");

    window.MockstarScoring.refresh();
  }

  function renderEmpty() {
    header.innerHTML = '<p class="placeholder-copy">Import a roster and pick a heat before scoring.</p>';
    roleSwitcher.innerHTML = "";
    pairingPanel.innerHTML = "";
    scoreboard.innerHTML = "";
  }

  function render() {
    const { heat, sheet } = getActiveContext();
    if (!heat || !sheet) {
      renderEmpty();
      return;
    }

    renderHeader(heat, sheet);
    renderRoleSwitcher(heat);
    renderPairingPanel(heat);
    renderRows(heat, sheet);
  }

  window.MockstarScoring = {
    onSliderInput(input) {
      const state = window.MockstarState.updateScore(
        window.MockstarState.loadState().selectedHeatId,
        activeRole,
        input.dataset.entryId,
        Number(input.value)
      );
      input.closest(".score-input").querySelector(".score-value").textContent = String(input.value);
      this.refresh(state);
    },
    refresh(stateOverride) {
      const state = stateOverride ?? window.MockstarState.loadState();
      const heat = window.MockstarState.getHeatById(state, state.selectedHeatId);
      const sheet = heat ? window.MockstarState.getScoreSheet(state, heat.id, activeRole) : null;
      if (!heat || !sheet) {
        return;
      }

      const entries = window.MockstarState.getEntriesForRole(heat, sheet.role);
      const ranked = window.MockstarState.rankEntries(heat, entries, sheet.scores);
      ranked.forEach((entry, index) => {
        const row = scoreboard.querySelector(`[data-entry-id="${entry.id}"]`);
        if (row) {
          row.querySelector(".rank-value").textContent = String(index + 1);
          row.querySelector(".score-value").textContent = String(sheet.scores[entry.id] ?? 500);
        }
      });

      const tiedIds = window.MockstarState.getTiedEntryIds(sheet.scores);
      document.body.dispatchEvent(new CustomEvent("scorestate", {
        bubbles: true,
        detail: { tiedIds, hasTies: tiedIds.length > 0 }
      }));
    },
    isTied(row, tiedIds) {
      return tiedIds.includes(row.dataset.entryId);
    }
  };

  finalizeButton.addEventListener("click", function () {
    const state = window.MockstarState.loadState();
    const heat = window.MockstarState.getHeatById(state, state.selectedHeatId);
    if (!heat) {
      return;
    }

    try {
      window.MockstarState.finalizeSheet(heat.id, activeRole);
      render();
    } catch (error) {
      alert(error instanceof Error ? error.message : "Unable to finalize score sheet.");
    }
  });

  document.addEventListener("DOMContentLoaded", render);
})();
