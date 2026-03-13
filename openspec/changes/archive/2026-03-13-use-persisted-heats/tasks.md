## 1. Import Page - Save to DB

- [x] 1.1 Add HeatApiClient.SaveEventAsync method for POST /api/heats/{eventId}
- [x] 1.2 Add server-side activation handler in Import page
- [x] 1.3 Replace client-side activation (JS) with server POST
- [x] 1.4 Redirect to Heats page after successful save

## 2. Heats Page - Load from DB

- [x] 2.1 Rewrite Heats page to load events from HeatApiClient
- [x] 2.2 Create HeatsViewModel with event list
- [x] 2.3 Create _HeatListPartial to render event/heat tree
- [x] 2.4 Update _HeatDetailPartial to link to /Scoring/{heatId}

## 3. Cleanup Old Code

- [x] 3.1 Delete Pages/Scoring/Index.cshtml and Index.cshtml.cs
- [x] 3.2 Delete wwwroot/js/scoring.js (old version)
- [x] 3.3 Delete wwwroot/js/mockstar-state.js
- [x] 3.4 Simplify wwwroot/js/heats.js (or delete if not needed)
- [x] 3.5 Remove client-side activation code from import.js

## 4. Navigation Updates

- [x] 4.1 Update layout nav link for Scoring (or remove if no default page)
- [x] 4.2 Update home page scoring link
