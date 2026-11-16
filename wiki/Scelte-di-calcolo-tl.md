# Mga pagpili sa kalkulasyon

Ipinapaliwanag ng manwal (kabanata 21) ang bawat pagpili kasama ang mga sanggunian. Sa madaling sabi:

- **Engine**: salin sa C# ng nec2c, ang NEC-2 ng Lawrence Livermore National Laboratory. Ikinukumpara ito ng self-test sa nec2c; ikinukumpara ng mga acceptance test ang daan-daang model sa EZNEC. Ginagawa nang pa-block ang factorization ng matrix (tulad ng LAPACK) at ginagamit ang lahat ng processor maliban sa isa, na para sa window.
- **Lupa**: free space, perpekto, reflection coefficient, Sommerfeld (may ayos sa depekto ng nec2c sa mga talahanayan, sinuri gamit ang necpp), radial screen; karaniwan at pasadyang lupa. Awtomatikong babala kapag masyadong malapit sa lupa ang wire para sa NEC-2.
- **Teleskopikong tubo**: ginagawang masyadong mababa ng NEC-2 ang resonance ng mga element na baitang-baitang ang diyametro. Ginagamit ng YA-NEC ang koreksyon mula sa aklat ni D. Leeson W6NL (*Physical Design of Yagi Antennas*, ARRL 1992), pareho sa EZNEC: sa ibaba ng resonance palagi, sa itaas hanggang 45 %, na may maliit na kalibrasyon ng haba batay sa mga halaga ng EZNEC.
- **Segmentation**: pagsuri sa mga segment (sa pagitan ng lambda/1000 at lambda/10, ratio ng haba/radius) na may awtomatikong ayos; gansal na bilang ng segment sa mga wire na may feed sa gitna.
- **G/T**: integration ng 3D diagram tulad ng XLGTa, pag-scan ng elevation tulad ng TANT, temperatura ng langit at lupa mula sa VE7BQH (luma at bago) at DG7YBN.
- **Pagsuri ng datos**: 42 pagsuri na nagbababala sa maling datos nang hindi binabago ang model.
