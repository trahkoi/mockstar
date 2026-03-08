(function () {
  const STORAGE_KEY = "mockstar-state";
  let memoryState = emptyState();
  const storageAvailable = detectStorage();

  function emptyState() {
    return {
      eventRecords: [],
      scoreSheets: [],
      selectedHeatId: null,
      lastImportedHeatIds: []
    };
  }

  function clone(value) {
    return JSON.parse(JSON.stringify(value));
  }

  function detectStorage() {
    try {
      localStorage.setItem("__mockstar__", "1");
      localStorage.removeItem("__mockstar__");
      return true;
    } catch {
      return false;
    }
  }

  function normalizeState(state) {
    return {
      eventRecords: state?.eventRecords ?? [],
      scoreSheets: state?.scoreSheets ?? [],
      selectedHeatId: state?.selectedHeatId ?? null,
      lastImportedHeatIds: state?.lastImportedHeatIds ?? []
    };
  }

  function loadState() {
    if (!storageAvailable) {
      showStorageWarning();
      return clone(memoryState);
    }

    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? normalizeState(JSON.parse(raw)) : emptyState();
    } catch {
      return emptyState();
    }
  }

  function saveState(state) {
    const next = normalizeState(state);
    if (storageAvailable) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    } else {
      memoryState = clone(next);
      showStorageWarning();
    }

    return next;
  }

  function showStorageWarning() {
    const warning = document.getElementById("storage-warning");
    if (warning && !storageAvailable) {
      warning.classList.remove("is-hidden");
    }
  }

  function getHeatById(state, heatId) {
    for (const eventRecord of state.eventRecords) {
      const heat = eventRecord.heats.find((item) => item.id === heatId);
      if (heat) {
        return heat;
      }
    }

    return null;
  }

  function getScoreSheet(state, heatId, role) {
    return state.scoreSheets.find((sheet) => sheet.heatId === heatId && sheet.role === role) ?? null;
  }

  function createDraftSheets(heat) {
    if (heat.type === "strictly") {
      return [buildScoreSheet(heat.id, "couple", heat.coupleEntries)];
    }

    if (heat.type === "jack-and-jill-final") {
      return [buildScoreSheet(heat.id, "couple", heat.leaderEntries)];
    }

    const sheets = [];
    if (heat.leaderEntries.length > 0) {
      sheets.push(buildScoreSheet(heat.id, "leader", heat.leaderEntries));
    }
    if (heat.followerEntries.length > 0) {
      sheets.push(buildScoreSheet(heat.id, "follower", heat.followerEntries));
    }
    return sheets;
  }

  function buildScoreSheet(heatId, role, entries) {
    return {
      heatId,
      role,
      status: "draft",
      scores: Object.fromEntries(entries.map((entry) => [entry.id, 500])),
      finalizedAt: null
    };
  }

  function applyRoleAssignments(eventRecord, container) {
    for (const heat of eventRecord.heats) {
      if (!heat.ambiguousEntries.length) {
        continue;
      }

      const assignments = Array.from(container.querySelectorAll(`[data-heat-id="${heat.id}"] select`));
      heat.leaderEntries = heat.leaderEntries ?? [];
      heat.followerEntries = heat.followerEntries ?? [];

      for (let index = 0; index < assignments.length; index += 1) {
        const assignment = assignments[index].value;
        const entry = heat.ambiguousEntries[index];
        if (!entry || assignment === "Assign role") {
          continue;
        }

        if (assignment === "Leader") {
          heat.leaderEntries.push(entry);
        } else {
          heat.followerEntries.push(entry);
        }
      }

      heat.ambiguousEntries = [];
      heat.leaderEntries.sort((left, right) => left.bib - right.bib);
      heat.followerEntries.sort((left, right) => left.bib - right.bib);
    }
  }

  function activateImportFromReview(button) {
    const payload = button.dataset.activationPayload;
    if (!payload) {
      return;
    }

    const review = button.closest(".review-card");
    const eventRecord = JSON.parse(atob(payload));
    applyRoleAssignments(eventRecord, review);

    const unresolvedHeat = eventRecord.heats.find((heat) => heat.ambiguousEntries.length);
    if (unresolvedHeat) {
      alert(`Assign all ambiguous roles before activating ${unresolvedHeat.name}.`);
      return;
    }

    const state = loadState();
    const importedHeatIds = eventRecord.heats.map((heat) => heat.id);

    state.eventRecords = [...state.eventRecords.filter((record) => record.id !== eventRecord.id), eventRecord];
    state.scoreSheets = [
      ...state.scoreSheets.filter((sheet) => !importedHeatIds.includes(sheet.heatId)),
      ...eventRecord.heats.flatMap(createDraftSheets)
    ];
    state.lastImportedHeatIds = importedHeatIds;
    state.selectedHeatId = importedHeatIds[0] ?? state.selectedHeatId;

    saveState(state);
    window.location.href = "/Heats";
  }

  function selectHeat(heatId) {
    const state = loadState();
    state.selectedHeatId = heatId;
    return saveState(state);
  }

  function updateScore(heatId, role, entryId, value) {
    const state = loadState();
    const sheet = getScoreSheet(state, heatId, role);
    if (!sheet || sheet.status === "finalized") {
      return state;
    }

    sheet.scores[entryId] = value;
    return saveState(state);
  }

  function pairFinalHeat(heatId, leaderBib, followerBib) {
    const state = loadState();
    const heat = getHeatById(state, heatId);
    if (!heat) {
      return state;
    }

    heat.pairings = heat.pairings.filter((pair) => pair.leaderBib !== leaderBib);
    heat.pairings.push({ leaderBib, followerBib });
    return saveState(state);
  }

  function getEntriesForRole(heat, role) {
    let entries;
    if (role === "leader") {
      entries = heat.leaderEntries;
    } else if (role === "follower") {
      entries = heat.followerEntries;
    } else {
      entries = heat.type === "strictly" ? heat.coupleEntries : heat.leaderEntries;
    }

    return [...entries].sort((left, right) => entryDisplay(heat, left).localeCompare(entryDisplay(heat, right), undefined, { numeric: true }));
  }

  function entryDisplay(heat, entry) {
    if (heat.type === "strictly") {
      return entry.display;
    }

    if (heat.type === "jack-and-jill-final") {
      const pairing = heat.pairings.find((pair) => pair.leaderBib === entry.bib);
      return pairing ? `${entry.display}/${pairing.followerBib}` : entry.display;
    }

    return entry.display;
  }

  function getTiedEntryIds(scores) {
    const grouped = Object.entries(scores).reduce((map, [entryId, score]) => {
      const key = String(score);
      map[key] = map[key] ?? [];
      map[key].push(entryId);
      return map;
    }, {});

    return Object.values(grouped).flatMap((entryIds) => entryIds.length > 1 ? entryIds : []);
  }

  function rankEntries(heat, entries, scores) {
    return [...entries].sort((left, right) => {
      const scoreDifference = (scores[right.id] ?? 500) - (scores[left.id] ?? 500);
      return scoreDifference !== 0
        ? scoreDifference
        : entryDisplay(heat, left).localeCompare(entryDisplay(heat, right), undefined, { numeric: true });
    });
  }

  function finalizeSheet(heatId, role) {
    const state = loadState();
    const sheet = getScoreSheet(state, heatId, role);
    if (!sheet) {
      return state;
    }

    if (getTiedEntryIds(sheet.scores).length > 0) {
      throw new Error("Resolve ties before finalizing this score sheet.");
    }

    sheet.status = "finalized";
    sheet.finalizedAt = new Date().toISOString();
    return saveState(state);
  }

  window.MockstarState = {
    emptyState,
    loadState,
    saveState,
    getHeatById,
    getScoreSheet,
    getEntriesForRole,
    entryDisplay,
    getTiedEntryIds,
    rankEntries,
    activateImportFromReview,
    selectHeat,
    updateScore,
    pairFinalHeat,
    finalizeSheet,
    showStorageWarning,
    storageAvailable
  };

  document.addEventListener("DOMContentLoaded", showStorageWarning);
})();
