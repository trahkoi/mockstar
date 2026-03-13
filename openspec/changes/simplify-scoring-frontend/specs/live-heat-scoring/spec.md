## MODIFIED Requirements

### Requirement: Ranks are assigned client-side by sorting entries descending by score
The system SHALL use minimal client-side JavaScript (~50 lines) to update rank numbers and detect ties on slider input.

#### Scenario: Slider input updates rank
- **WHEN** a user moves a slider to a new value
- **THEN** JavaScript recalculates ranks for all entries and updates rank display numbers

#### Scenario: Tie detection
- **WHEN** two or more entries have the same slider value
- **THEN** JavaScript adds `.is-tied` class to those entries for visual highlighting

#### Scenario: Score state preserved across role switch
- **WHEN** a user switches from leader to follower and back to leader
- **THEN** JavaScript restores the previously set slider values for leaders

### Requirement: No server round-trip for scoring interactions
The system SHALL NOT make server calls when sliders are moved. Rank and tie updates are purely client-side.

#### Scenario: Slider interaction is offline-capable
- **WHEN** network connection is lost during scoring
- **THEN** slider interactions continue to work and ranks update correctly
