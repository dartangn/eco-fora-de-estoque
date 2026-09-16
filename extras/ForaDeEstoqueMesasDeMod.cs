// ForaDeEstoqueMesasDeMod.cs -- OPCIONAL / OPTIONAL. Fora do pacote de proposito.
//
// PT: so instale este arquivo se voce TIVER os mods citados. Referenciar um tipo de mod
//     que nao esta instalado NAO COMPILA, e servidor que nao compila NAO SOBE. Apague a
//     linha de qualquer mod que voce nao tenha.
//
// EN: install this file ONLY if you have the mods below. Referencing a type from a mod
//     that is not installed fails to compile and the whole server refuses to start.
//     Delete the line for any mod you do not have.
//
// MEDIDO no servidor de teste em 15/09/2026, com 29 mods instalados: apenas IceCream e
// Mixology declaram [RequireComponent(typeof(CraftingComponent))]. Sao tres mesas, as
// tres `partial` e no namespace Eco.Mods.TechTree -- por isso o molde de uma linha serve.
//
// COMO ACRESCENTAR OUTRA MESA DE MOD -- os tres requisitos, nesta ordem:
//   1. a mesa tem de declarar [RequireComponent(typeof(CraftingComponent))];
//      sem isso a aba aparece e a lista sai sempre vazia (o codigo devolve lista vazia
//      quando o objeto nao tem CraftingComponent -- nao quebra, so nao serve para nada).
//   2. a classe tem de ser `public partial class` no mod. Se nao for partial, da CS0260
//      e o servidor nao sobe.
//   3. o namespace aqui tem de ser o MESMO do objeto no mod. Quase todo mod de Eco usa
//      Eco.Mods.TechTree, mas isso se confere, nao se supoe.
//
// Achar as candidatas no seu servidor:
//   grep -rlE 'RequireComponent\(typeof\(CraftingComponent\)\)' Mods/UserCode

namespace Eco.Mods.TechTree
{
    using Eco.Gameplay.Objects;

    // --- Mixology 14.0.3 ---------------------------------------------------------
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class MixologyTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class AdvancedMixologyTableObject { }

    // --- IceCream 14.2 -----------------------------------------------------------
    // "Machin" sem o "e" NAO e erro de digitacao nosso: e o nome da classe no mod, que
    // o autor renomeou de IceCreamMachine.cs para 'IceCreamMachin.cs no porte para a 14.
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class IceCreamMachinObject { }
}
