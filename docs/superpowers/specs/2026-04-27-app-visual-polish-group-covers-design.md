# App Visual Polish and Group Covers Design

Date: 2026-04-27

## Goal

Improve the general first impression of TasteBudz without expanding MVP workflow scope. The approved direction combines the cleaner app structure from the "Cleaner App Shell" concept with the richer social meal cards from the "Social Meal Cards" concept. Realistic food and dining photography should be used for general site imagery, while group owners can choose from built-in thematic group background presets.

## Documents Reviewed

- `docs/TasteBudz_Functional_Requirements.md`
- `docs/backend/backend-architecture.md`
- `docs/backend/domain-model.md`
- `docs/backend/api-endpoints.md`
- `docs/backend/testing-strategy.md`

## Approved Direction

The app should move toward a polished social dining dashboard:

- Keep the app shell clean, readable, and task-oriented.
- Use richer event and group cards with food imagery where the image adds context.
- Use realistic food and shared-dining photography for general public-site, auth, onboarding, dashboard, and empty-state imagery.
- Let group owners select a built-in thematic background for their own group.
- Include both realistic and illustrated group-background preset styles.
- Do not add admin cover-library management in this phase.

## Scope

In scope:

- Visual polish for the logged-in app shell and dashboard-style surfaces.
- Group/event card improvements that make people, groups, restaurants, and dining plans feel more alive.
- Built-in group background presets.
- Group-owner selection of a group background from the preset set.
- Realistic global/site imagery where the app uses broad background or hero images.
- Desktop and mobile visual verification before calling the UI complete.

Out of scope:

- Admin background-library management.
- User-uploaded custom group backgrounds.
- AI image generation inside the running product.
- Changing group ownership, membership, invites, chat authorization, or event participation rules.
- Replacing the current MVC application architecture.
- Broad landing-page redesign unrelated to the MVP app workflows.

## Visual Design

The core UI should feel calm, polished, and useful rather than decorative.

Primary visual direction:

- Use a cleaner dashboard shell with stronger layout hierarchy, less empty space, and clearer next actions.
- Keep the existing warm TasteBudz palette, but avoid making every element the same orange/cream tone.
- Use food imagery as a controlled accent inside hero bands, group cards, event cards, restaurant cards, and selected empty states.
- Keep text on photos behind dark or warm gradient overlays so it remains readable.
- Use cards for repeated items only, not nested page-section decoration.
- Preserve dense, scannable task flows for dashboard, events, groups, restaurants, and chat entry points.

General imagery:

- Should be realistic photography.
- Should show actual food, restaurants, shared tables, or social dining.
- Should not look like generic stock photography with dark blur, heavy bokeh, or vague atmosphere.
- Should not dominate operational app screens where users need to scan lists or take actions.

Group background presets:

- A group owner chooses one preset background when creating or editing a group.
- Presets can include both realistic and illustrated styles.
- Preset examples: Default, Sushi, Tacos, Brunch, Pizza, Curry, Noodles, Vegetarian, Coffee/Dessert.
- Presets are presentation metadata only and must not affect visibility, membership, chat, events, or moderation.

## UX Rules

- Group owner controls their group's selected background.
- Non-owner group members and public viewers can see the selected background when they are allowed to view the group.
- Users do not upload custom backgrounds in this phase.
- Admins do not curate, approve, or manage cover assets in this phase.
- If a cover image fails to load, the UI falls back to a readable default gradient.
- Background selection must be optional; a group can stay on the default cover.

## Technical Shape

Preferred MVP implementation:

- Add a constrained `GroupCoverTheme` or equivalent string/enum field to group data.
- Store only the selected preset key, not arbitrary image URLs.
- Map preset keys to local static assets or stable bundled assets in the MVC frontend.
- Include the selected cover key in group detail and group summary view models where needed.
- Validate cover keys server-side if the value crosses an API or MVC form boundary.

No admin module changes are needed for this slice.

## Documentation Updates Required During Implementation

If this is implemented, update:

- `docs/TasteBudz_Functional_Requirements.md`: group owner can select a built-in group background preset.
- `docs/backend/domain-model.md`: group presentation metadata includes a preset cover theme if persisted.
- `docs/backend/api-endpoints.md`: group DTO/update contract includes the preset cover field if exposed through API.
- `docs/backend/testing-strategy.md`: owner-only group cover selection and invalid preset coverage.

No `backend-decisions.md` update is required unless the implementation introduces a broader policy decision beyond preset-only owner-selected covers.

## Visual Quality Gate

Before the UI work is considered complete:

- Run the app locally.
- Verify the updated pages in a real browser.
- Capture and inspect desktop and mobile screenshots.
- Check that text does not overlap, clip, or overflow.
- Check that cards do not shift size unexpectedly because of hover states, labels, images, or long names.
- Check that photo overlays preserve readable contrast.
- Check that mobile navigation, floating buttons, and card actions do not cover important content.
- Check that the palette does not collapse into a one-note orange/cream theme.

Minimum viewports:

- Desktop: 1365px wide or similar.
- Mobile: 390px wide or similar.

## Risks

- Too much photography could make the app feel noisy and less usable.
- Unsafely placed text over images could hurt readability.
- Adding cover selection could drift into a media-management feature if custom uploads or admin tooling are added too early.
- Group cover selection must not accidentally change group access rules.

## Recommendation

Proceed with the B+C mixed UI direction as a focused visual-polish slice. Use realistic photography for general app/site imagery. Use preset group covers with both realistic and illustrated options. Keep the cover selector owner-only and avoid admin management for now.
