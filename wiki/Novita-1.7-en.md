# What's new in 1.7 (1.7.0 – 1.7.2)

[Italiano](Novita-1.7) · Manual, chapter 28 · Video course, lessons 34-37

## 1.7.2 – security
- **Signed automatic update**: only packages signed by YA-NEC (ECDSA P-256) are installed, besides the SHA-256 checksum. Download limited to 10 minutes and 400 MB.
- Files and models prepared on purpose no longer close the program: expressions nested over 200 levels, models over 40000 segments and broken GeoTIFFs give a message.
- Lighter update check for GitHub (conditional request); warning if the update repository is not ic8tem/YA-NEC.

## 1.7.1
- **3D structure**: the heights (h, Δ and level heights) can be clicked and changed; h moves the whole antenna, Δ that level and those above. «↶ Undo» goes back.
- **Patterns**: with a stack of identical antennas, a box at the top right changes the spacing live, with the starting lobe dashed.
- Fixes: half-sloper (default slope 30°), broadband dipole, Direct match to Z0 keeping the variables, English translations.

## 1.7.0
Ideas and classic formulas from RadioRivista, RadioKit, QST and the ARRL Handbook (sources cited in the manual):
- **New calculators**: 2-3 element HF Yagi with tuning guide, interlaced dual-band Yagi, trap multiband vertical, inverted L / T for 160 m, half-sloper on the tower, broadband dipole, relay multiband dipole, linear loading with coil / top hat comparison, wire resonant on harmonics (EFLW), gamma match and stub matching with the Smith chart.
- **Multiband quad**, rectangular **magnetic loop** with turns in parallel, **Wire antennas** with wire material and FT-82-43.
- **Stack**: effective area and spacing chosen with a click on the table; **SWR** showing which side R lies on; **Real terrain** with take-off angles towards a locator; new **Tips** tab.
