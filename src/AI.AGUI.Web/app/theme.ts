import type { Types } from "@a2ui/lit/0.8";

/**
 * A2UI Theme — uses the official litTheme from @copilotkit/a2ui-renderer v1.54.1
 * (the version bundled inside @copilotkitnext/react), which is the same theme
 * used by the a2ui-composer.ag-ui.com gallery.
 *
 * The top-level @copilotkit/a2ui-renderer (v1.55.1) exports an empty litTheme
 * because v0.9+ moved to server-driven theming. We must reference the nested
 * v1.54.1 copy directly to get the actual theme object.
 *
 * Tabs styling is added on top because the gallery uses the Lit (Shadow DOM)
 * renderer, where <button> UA styles are scoped automatically. The React
 * (Light DOM) renderer needs explicit theme classes to achieve the same result.
 */

// eslint-disable-next-line @typescript-eslint/no-require-imports
const { litTheme: galleryTheme } = require(
  "../node_modules/@copilotkitnext/react/node_modules/@copilotkit/a2ui-renderer"
) as { litTheme: Types.Theme };

export const a2uiTheme: Types.Theme = {
  ...galleryTheme,
  components: {
    ...galleryTheme.components,
    // Tabs: extend the empty litTheme Tabs with React Light-DOM-specific styles.
    // These replicate the underline-tab look the gallery achieves via Shadow DOM UA styles.
    Tabs: {
      container: {
        // Stack tab-bar and content vertically with a gap below the separator line
        "layout-dsp-flexvert": true,
        "layout-g-4": true,
      },
      controls: {
        all: {
          // Reset native <button> appearance (border, background) for all tabs
          "border-bw-0": true,
          "color-bgc-transparent": true,
          "layout-pt-2": true,
          "layout-pb-2": true,
          "layout-pl-3": true,
          "layout-pr-3": true,
          "color-c-n50": true,
        },
        selected: {
          // Dark 2px underline + dark text for the active tab
          "border-bbw-2": true,
          "border-bs-s": true,
          "color-bc-n10": true,
          "color-c-n10": true,
        },
      },
      element: {
        // Full-width separator line (1px light grey) below all tab buttons
        "border-bw-0": true,
        "border-bbw-1": true,
        "border-bs-s": true,
        "color-bc-n90": true,
        "layout-dsp-flexhor": true,
        "layout-g-1": true,
      },
    },
  },
};
