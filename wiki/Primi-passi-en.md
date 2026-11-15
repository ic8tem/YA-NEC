# Getting started

1. **Examples** (command bar) opens one of the ready models: dipoles, ground plane, VHF Yagis (DK7ZB, I4GBZ, DJ9BV BVO, YU7EF), HF Yagis (Force 12 XR-5, ARRL 520-40H), hexbeam, four-square, 1296 MHz loop Yagi and more. **Open model** opens a NEC, MMANA, AutoEZ, EZNEC or K6STI file; you can also drag the file onto the window.
2. Choose the **band** (from 2200 m to 3 cm, IARU Region 1 limits) or a free frequency, then **Calculate**.
3. The **result boxes** show impedance, SWR, gain, F/B, F/R, -3 dB beamwidths, efficiency and AGT (convergence test: close to 0 dB means the model is reliable).
4. **Sweep** calculates the band around the frequency: SWR, R/X, gain, F/B and Smith chart.

## The tabs

- **3D structure**: the antenna (drag = rotate, wheel = zoom, right button = pan, double click = initial view), with the ground drawn according to its type and height. Below, the **wire table** in spreadsheet style: cells accept numbers, variables and formulas. Bottom right, the **model objects**: sources, loads, lines, transformers.
- **Patterns**: horizontal, vertical and 3D, with the -3 dB angle.
- **Frequency, Smith, Currents**: sweep results and wire currents.
- **NEC model**: the NEC cards, editable by hand.
- **Variables** and **Optimizer**: give a name to the dimensions and let the program change them to reach a goal (SWR, gain, F/B, impedance...). The manual has step-by-step examples.
- **Calculators**: see [Calculators](Calcolatori-en).
- **Receiving antennas** and **Noise/GT/Stack**: RDF of receiving antennas, antenna temperature and G/T (TANT and XLGTa method, VE7BQH and DG7YBN reference temperatures), stacking.
- **Warnings**: everything doubtful the program found in the model (segments too short or too long, wires close to the ground, crowded junctions). The **Check...** button in the top bar checks 42 typical input errors without calculating.
- **Settings**, **Manual**, **Credits**. Top right: **Language**, **Colours** (18 schemes) and **About**.

## Saving

**Save** and **Save as** write the model as NEC, MMANA, AutoEZ, EZNEC or in the program's own **.yanec** format, which also keeps the description and the original file.
