# Calculators

The **Calculators** tab gathers sheets that design an antenna from published formulas and sources, and then check it with the NEC engine. Every sheet has the same layout: data on the left (with a tooltip on every field), formulas and sources below the data, result boxes, drawing, warnings (orange triangle) and confirmations (green tick). The buttons:

- **Create the NEC model and check it**: calculates the model with the engine and shows impedance, SWR and gain;
- **Bring to resonance (NEC)** or **Optimize (NEC)**, where available;
- **Save .nec** and **Open as model**, which brings the antenna into the main window.

When the model is an approximation (for example wires very close to the ground) the button has a yellow tooltip saying so.

## The sheets

- **Dipoles**: half-wave, inverted V, folded, trapped (W3DZZ), OCF/Windom, G5RV, T2FD; **multiband dipole**.
- **Verticals**: quarter-wave with ground radials (ON4UN) and elevated radials, 5/8, loaded with a coil, J-pole, **multiband vertical** like the DX Commander, **four-square**.
- **Directional**: HB9CV, Moxon, quad (also DK7ZB multiband), delta loop, rectangular loop, Hentenna, **hexbeam 20-10 m**, **multiband Yagi**, **DL6WU long Yagi** with four feeds (open and folded dipole, LFA, DG7YBN folded radiator), **Quagi**, **1296 MHz loop Yagi**, **LPDA**.
- **Mechanics and stacking**: DL6WU stacking distances (the stack is built in the open model), boom correction, element **mounting brackets**.
- **Satellites and microwaves**: eggbeater, QFH, PCB antennas for 2.4 GHz, **prime-focus dish** (with ribs and a printable template), **horns and feeds**, **offset dish**.
- **Other**: magnetic loop, discone, EH antenna, rescale antenna, antenna on a concrete roof, LC network, radiation resistance.
- **Lines and transformers**: RG cables and the 50 and 75 ohm coax chart, impedance change with a length of cable, iron powder and ferrite toroids, baluns and ununs.

The sources of every sheet are written below the data and in the Credits tab; where sources disagree, the differing dimensions are shown side by side.

> Since 1.1 the sheets are grouped in families (HF, VHF, Receiving antennas, Tools, Dishes and feeds, 2.4 GHz) and the stacking distances are in the Noise/GT/Stack super-tab: [What's new in 1.1](Novita-1.1-en).
