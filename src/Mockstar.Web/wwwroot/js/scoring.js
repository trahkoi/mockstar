(function () {
  const shell = document.getElementById("scoring-shell");
  const antiForgeryToken = document.querySelector("#scoring-antiforgery input[name='__RequestVerificationToken']")?.value;

  if (!shell || !window.MockstarState) {
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

  function requestConfig(state) {
    return {
      target: "#scoring-shell",
      swap: "innerHTML",
      headers: antiForgeryToken ? { RequestVerificationToken: antiForgeryToken } : {},
      values: {
        stateJson: JSON.stringify(state),
        activeRole
      }
    };
  }

  function render() {
    htmx.ajax("POST", "/Scoring?handler=Shell", requestConfig(window.MockstarState.loadState()));
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
        const row = shell.querySelector(`[data-entry-id="${entry.id}"]`);
        if (row) {
          row.querySelector(".rank-value").textContent = String(index + 1);
          row.querySelector(".score-value").textContent = String(sheet.scores[entry.id] ?? 500);
        }
      });

      const tiedIds = window.MockstarState.getTiedEntryIds(sheet.scores);
      entries.forEach((entry) => {
        const row = shell.querySelector(`[data-entry-id="${entry.id}"]`);
        if (row) {
          row.classList.toggle("is-tied", tiedIds.includes(entry.id));
        }
      });

      const finalizeButton = shell.querySelector("[data-finalize]");
      if (finalizeButton) {
        finalizeButton.disabled = tiedIds.length > 0 || sheet.status === "finalized";
      }
    },
    isTied(row, tiedIds) {
      return tiedIds.includes(row.dataset.entryId);
    }
  };

  shell.addEventListener("click", function (event) {
    const roleButton = event.target.closest("[data-role]");
    if (roleButton?.dataset.role) {
      activeRole = roleButton.dataset.role;
      render();
      return;
    }

    const pairButton = event.target.closest("[data-pair-submit]");
    if (pairButton?.dataset.heatId) {
      const leaderBib = Number(shell.querySelector("[data-pair-leader]")?.value);
      const followerBib = Number(shell.querySelector("[data-pair-follower]")?.value);
      if (!Number.isNaN(leaderBib) && !Number.isNaN(followerBib)) {
        window.MockstarState.pairFinalHeat(pairButton.dataset.heatId, leaderBib, followerBib);
        render();
      }
      return;
    }

    const finalizeButton = event.target.closest("[data-finalize]");
    if (!finalizeButton) {
      return;
    }

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

  shell.addEventListener("input", function (event) {
    const input = event.target.closest(".score-slider");
    if (input) {
      window.MockstarScoring.onSliderInput(input);
    }
  });

  document.body.addEventListener("htmx:afterSwap", function (event) {
    if (event.target.id !== "scoring-shell") {
      return;
    }

    const nextRole = shell.querySelector("[data-active-role]")?.dataset.activeRole;
    if (nextRole) {
      activeRole = nextRole;
    }

    window.MockstarScoring.refresh();
  });

  document.addEventListener("DOMContentLoaded", render);
})();
