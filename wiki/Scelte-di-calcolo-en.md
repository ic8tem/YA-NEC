# Calculation choices

The manual (chapter 21) explains every choice with its sources. In short:

- **Engine**: C# translation of nec2c, the NEC-2 of Lawrence Livermore National Laboratory. The self-test compares it with nec2c; the acceptance tests compare hundreds of models with EZNEC. Matrix factorization is blocked (as in LAPACK) and uses all processors but one, left to the window.
- **Grounds**: free space, perfect, reflection coefficient, Sommerfeld (with the fix of a nec2c defect in the tables, checked with necpp), radial screen; typical and custom grounds. Automatic warnings when wires are too close to the ground for NEC-2.
- **Telescopic tubing**: NEC-2 makes elements with stepped diameters resonate too low. YA-NEC uses the correction from the book by D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), the same as EZNEC: below resonance it always applies, above up to 45 %, with a small length calibration made on EZNEC values.
- **Segmentation**: segment check (between lambda/1000 and lambda/10, length/radius ratio) with automatic fix; odd number of segments on centre-fed wires.
- **G/T**: integration of the 3D pattern as in XLGTa, elevation scan as in TANT, sky and ground temperatures from VE7BQH (old and new) and DG7YBN.
- **Data check**: 42 checks that warn about wrong data without changing the model.
