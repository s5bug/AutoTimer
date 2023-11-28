# AutoTimer

Based on the Triggernometry trigger by aya liz.

`/autotimer` to display the Auto Timer bar.

`/autotimerconfig` to open the configuration window:
- Bar Type: Controls whether hints for optimal timing are shown on the bar. Green is best, empty is OK, red is worst.
- Predictive TCJ: Controls whether or not the bar delays to a standard 2.85s Ten-Chi-Jin.
- Lock Bar: Controls whether or not the bar can be repositioned / interacted with.

## About Ninja

Optimal timing for Ten-Chi-Jin depends on quite a few factors:
- GCD speed
- Time since the last GCD
- Whether TCJ Mudras are pressed on cooldown

The background texture for the Ninja bar assumes a late-weave TCJ, with 2.85s between TCJ being pressed and falling off.

## Plugin Repository

**This repository is not reviewed by the Dalamud team and should be considered unstable (cause game crashes, etc.) and
unsafe (compromise your computer, perform unintended actions, etc.).** It is better to install this plugin from the
Dalamud Testing repository (activated in Dalamud Settings). Once this plugin is in the official plugin repository, this
README will be updated and the custom repository will be fully removed in favor of Dalamud Testing builds.

Adding the custom repository `https://raw.githubusercontent.com/s5bug/AutoTimer/main/repo.json` will allow you to search
for `AutoTimer`.
