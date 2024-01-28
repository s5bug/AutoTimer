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
