(function () {
  // Score state persists across role switches within session
  const scores = {};

  function getPanel() {
    return document.getElementById('scoring-panel');
  }

  function getCurrentRole() {
    const panel = getPanel();
    return panel?.querySelector('.scoring-entries')?.dataset.role;
  }

  function updateRanksAndTies() {
    const panel = getPanel();
    if (!panel) return;

    const entries = [...panel.querySelectorAll('.scoring-entry')];
    
    // Get current values
    const values = entries.map(el => ({
      el,
      id: el.dataset.entryId,
      value: Number(el.querySelector('.score-slider').value)
    }));

    // Sort by value descending
    values.sort((a, b) => b.value - a.value);

    // Count occurrences for tie detection
    const valueCounts = {};
    values.forEach(v => valueCounts[v.value] = (valueCounts[v.value] || 0) + 1);

    // Update ranks and tie status
    values.forEach((v, i) => {
      v.el.querySelector('.rank').textContent = i + 1;
      v.el.classList.toggle('is-tied', valueCounts[v.value] > 1);
    });
  }

  function saveCurrentScores() {
    const role = getCurrentRole();
    if (!role) return;

    const panel = getPanel();
    if (!panel) return;

    scores[role] = {};
    panel.querySelectorAll('.score-slider').forEach(slider => {
      scores[role][slider.dataset.entryId] = Number(slider.value);
    });
  }

  function restoreScores() {
    const role = getCurrentRole();
    if (!role || !scores[role]) return;

    const panel = getPanel();
    if (!panel) return;

    panel.querySelectorAll('.score-slider').forEach(slider => {
      const entryId = slider.dataset.entryId;
      if (scores[role][entryId] !== undefined) {
        slider.value = scores[role][entryId];
        slider.closest('.scoring-entry').querySelector('.score-value').textContent = scores[role][entryId];
      }
    });

    updateRanksAndTies();
  }

  // Handle slider input
  document.addEventListener('input', function (e) {
    if (!e.target.matches('.score-slider')) return;

    const value = e.target.value;
    e.target.closest('.scoring-entry').querySelector('.score-value').textContent = value;

    saveCurrentScores();
    updateRanksAndTies();
  });

  // Handle HTMX swap (role switch)
  document.addEventListener('htmx:afterSwap', function (e) {
    if (e.target.id !== 'scoring-panel') return;

    restoreScores();
    updateRanksAndTies();
  });

  // Initial setup
  document.addEventListener('DOMContentLoaded', function () {
    updateRanksAndTies();
  });
})();
