(function () {
  const list = document.getElementById("heats-list");
  const detail = document.getElementById("heat-detail");
  const antiForgeryToken = document.querySelector("#heats-antiforgery input[name='__RequestVerificationToken']")?.value;
  if (!list || !detail || !window.MockstarState) {
    return;
  }

  function requestConfig(heatId, stateJson) {
    return {
      target: "#heat-detail",
      swap: "innerHTML",
      headers: antiForgeryToken ? { RequestVerificationToken: antiForgeryToken } : {},
      values: {
        heatId,
        stateJson
      }
    };
  }

  function render() {
    const state = window.MockstarState.loadState();
    if (!state.eventRecords.length) {
      list.innerHTML = '<p class="placeholder-copy">Import a roster first to populate heat selection.</p>';
      return;
    }

    list.innerHTML = state.eventRecords.map((eventRecord) => `
      <section class="stack">
        <h3>${eventRecord.name}</h3>
        ${eventRecord.heats.map((heat) => `
          <button
            type="button"
            class="button ${state.selectedHeatId === heat.id ? "button-primary" : "button-secondary"} heat-picker"
            data-heat-id="${heat.id}">
            ${heat.name}
          </button>
        `).join("")}
      </section>
    `).join("");

    list.querySelectorAll(".heat-picker").forEach((button) => {
      button.addEventListener("click", function () {
        const nextState = window.MockstarState.selectHeat(this.dataset.heatId);
        htmx.ajax("POST", "/Heats?handler=Detail", requestConfig(this.dataset.heatId, JSON.stringify(nextState)));
        render();
      });
    });

    if (state.selectedHeatId) {
      htmx.ajax("POST", "/Heats?handler=Detail", requestConfig(state.selectedHeatId, JSON.stringify(state)));
    }
  }

  document.addEventListener("DOMContentLoaded", render);
})();
