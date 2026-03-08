(function () {
  const list = document.getElementById("heats-list");
  const detail = document.getElementById("heat-detail");
  const antiForgeryToken = document.querySelector("#heats-antiforgery input[name='__RequestVerificationToken']")?.value;
  if (!list || !detail || !window.MockstarState) {
    return;
  }

  function requestConfig(target, values) {
    return {
      target,
      swap: "innerHTML",
      headers: antiForgeryToken ? { RequestVerificationToken: antiForgeryToken } : {},
      values
    };
  }

  function refresh() {
    const state = window.MockstarState.loadState();
    const stateJson = JSON.stringify(state);
    htmx.ajax("POST", "/Heats?handler=List", requestConfig("#heats-list", { stateJson }));
    htmx.ajax("POST", "/Heats?handler=Detail", requestConfig("#heat-detail", {
      heatId: state.selectedHeatId ?? "",
      stateJson
    }));
  }

  list.addEventListener("click", function (event) {
    const button = event.target.closest(".heat-picker");
    if (!button?.dataset.heatId) {
      return;
    }

    window.MockstarState.selectHeat(button.dataset.heatId);
    refresh();
  });

  document.addEventListener("DOMContentLoaded", refresh);
})();
