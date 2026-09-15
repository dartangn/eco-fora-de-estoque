// GERADO por gerar-mesas-fora-de-estoque.py -- NAO EDITAR A MAO.
// Pendura a aba "Fora de Estoque" nas 69 mesas de fabricacao do jogo base.
//
// A lista e lida do proprio servidor, nunca escrita a mao: assim uma atualizacao do Eco
// que acrescente ou tire mesa aparece na contagem em vez de passar despercebida.
//
// Mesa de MOD fica de fora: referenciar tipo de mod nao instalado nao compila e derruba
// o servidor (a armadilha do PergaminhosDosMods.cs).
//
// Tecnica: [RequireComponent] em partial class -- o que o Mods/UserCode/README.md chama
// de "existing classes customization". ZERO override, ZERO Harmony.

namespace Eco.Mods.TechTree
{
    using Eco.Gameplay.Objects;

    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class AdvancedCarpentryTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class AdvancedMasonryTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class AdvancedTailoringTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class AnvilObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ArrastraObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class AssemblyLineObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class AutomaticLoomObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class BakeryOvenObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class BlacksmithTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class BlastFurnaceObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class BloomeryObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ButcheryTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class CampfireObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class CampsiteObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class CarpentryTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class CastIronStoveObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class CementKilnObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ElectricLatheObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ElectricMachinistTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ElectricPlanerObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ElectricStampingPressObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ElectronicsAssemblyObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ExtruderObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class FarmersTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class FiberScutchingStationObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class FishRackObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class FisheryObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class FletchingTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class FrothFloatationCellObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class GlassworksObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class GrindstoneObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class IncineratorObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class IndustrialMillObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class InjectionMoldMachineObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class JawCrusherObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class KilnObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class KitchenObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class LaboratoryObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class LatheObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class LoomObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class MachinistTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class MasonryTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class MediumShipyardObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class MillObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class OilRefineryObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class PaintMixerObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class PotteryTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class PowerHammerObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class PrintingPressObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class PumpJackObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ResearchTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class RoboticAssemblyLineObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class RockerBoxObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class RollingMillObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class SawmillObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ScreeningMachineObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ScrewPressObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class SensorBasedBeltSorterObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class SettlementCraftingTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ShaperObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class SmallPaperMachineObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class SmallShipyardObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class SpinMelterObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class StampMillObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class StoveObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class TailoringTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class ToolBenchObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class WainwrightTableObject { }
    [RequireComponent(typeof(ForaDeEstoqueComponent))] public partial class WorkbenchObject { }
}
