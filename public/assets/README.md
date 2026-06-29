# Assets

The single brand logo for the app lives here as `forkly-transparent-logo.png`.

- Expected path: `public/assets/forkly-transparent-logo.png` → served at `/assets/forkly-transparent-logo.png`
- Recommended: transparent PNG/SVG, ~120×36px (the header caps it at 36px tall)
- `BrandLogo.vue` points at `/assets/forkly-transparent-logo.png`. To swap the
  logo, replace this file (keep the name). If it is ever missing, a
  `[ FORKLY LOGO ]` text fallback is shown automatically.
