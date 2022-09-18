﻿using System.Collections.Generic;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;
using System;
using Verse.Sound;
using Verse.AI;
using CombatExtended;
using System.Runtime.Remoting.Messaging;
using CombatExtended.Compatibility;
using System.Linq;
using System.Net.NetworkInformation;

namespace BDsPlasmaWeapon
{
    public class CompDropExtinguisherWhenUndrafted : CompRangedGizmoGiver
    {
        public DropLogic dropLogic;

        CompReloadableFromFiller compTank;

        CompEquippable compEquippable;

        public CompProperties_DropExtinguisherWhenUndrafted Props
        {
            get
            {
                return (CompProperties_DropExtinguisherWhenUndrafted)props;
            }
        }

        public bool isEquipment => compEquippable != null;

        public bool isApparel => parent is Apparel;

        public Pawn pawn
        {
            get
            {
                if (isEquipment)
                {
                    return compEquippable.PrimaryVerb.CasterPawn;
                }
                if (isApparel)
                {
                    return (parent as Apparel).Wearer;
                }
                return null;
            }
        }
        public void switchMode(DropLogic DropLogic)
        {
            dropLogic = DropLogic;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compTank = parent.TryGetComp<CompReloadableFromFiller>();
            compEquippable = parent.TryGetComp<CompEquippable>();
            if (compTank == null)
            {
                Log.Error("CompDropExtinguisherWhenUndrafted is meant to work with CompReloadableFromFiller!");
            }
            if (!respawningAfterLoad)
            {
                dropLogic = Props.defaultDropLogic;
            }
        }


        public bool shouldDrop
        {
            get
            {
                if (pawn != null || !pawn.Drafted)
                {
                    switch (dropLogic)
                    {
                        case DropLogic.DontDrop:
                            return false;
                        case DropLogic.AlwaysDrop:
                            return true;
                        case DropLogic.DropWhenEmpty:
                            if (compTank.remainingCharges == 0)
                            {
                                return true;
                            }
                            return false;
                        case DropLogic.DropWhenFull:
                            if (compTank.emptySpace == 0)
                            {
                                return true;
                            }
                            return false;
                        case DropLogic.DropIfNotFull:
                            if (compTank.emptySpace > 0)
                            {
                                return true;
                            }
                            return false;
                        default:
                            return false;
                    }
                }
                return false;
            }
        }

        private string getIconTex(DropLogic dropLogic)
        {
            switch (dropLogic)
            {
                case DropLogic.DontDrop:
                    return Props.dontDropIcon;
                case DropLogic.AlwaysDrop:
                    return Props.alwaysDropIcon;
                case DropLogic.DropWhenEmpty:
                    return Props.dropWhenEmptyIcon;
                case DropLogic.DropWhenFull:
                    return Props.dropWhenFullIcon;
                case DropLogic.DropIfNotFull:
                    return Props.dropIfNotFullIcon;
                default:
                    return "UI/Commands/DesirePower";
            }
        }

        private string getLabel(DropLogic dropLogic)
        {
            switch (dropLogic)
            {
                case DropLogic.DontDrop:
                    return Props.dontDropLabel;
                case DropLogic.AlwaysDrop:
                    return Props.alwaysDropLabel;
                case DropLogic.DropWhenEmpty:
                    return Props.dropWhenEmptyLabel;
                case DropLogic.DropWhenFull:
                    return Props.dropWhenFullLabel;
                case DropLogic.DropIfNotFull:
                    return Props.dropIfNotFullLabel;
                default:
                    return "None";
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent == null)
            {
                yield break;
            }
            yield return new Command_SwitchDropLogic
            {
                compExtinguisher = this,
                defaultLabel = Props.label + ": " + getLabel(dropLogic),
                defaultDesc = Props.description,
                icon = ContentFinder<Texture2D>.Get(getIconTex(dropLogic)),
            };
            yield break;
        }
    }

    public class CompProperties_DropExtinguisherWhenUndrafted : CompProperties
    {
        public DropLogic defaultDropLogic = DropLogic.DontDrop;
        public string dontDropIcon = "UI/Icons/DropLogicIcons_dontDrop";
        public string dropWhenEmptyIcon = "UI/Icons/DropLogicIcons_dropWhenEmpty";
        public string dropIfNotFullIcon = "UI/Icons/DropLogicIcons_dropIfNotFull";
        public string dropWhenFullIcon = "UI/Icons/DropLogicIcons_dropWhenFull";
        public string alwaysDropIcon = "UI/Icons/DropLogicIcons_alwaysDrop";
        public string description = "UI/Commands/DesirePower";
        public string dontDropLabel = "Dont drop";
        public string dropWhenEmptyLabel = "When Empty";
        public string dropIfNotFullLabel = "Not full";
        public string dropWhenFullLabel = "When full";
        public string alwaysDropLabel = "Always drop";
        public string label = "Drop logic";
        public CompProperties_DropExtinguisherWhenUndrafted()
        {
            compClass = typeof(CompDropExtinguisherWhenUndrafted);
        }
    }

    public enum DropLogic : byte
    {
        DontDrop,
        DropWhenEmpty,
        DropIfNotFull,
        DropWhenFull,
        AlwaysDrop,
    }

    public class Command_SwitchDropLogic : Command_Action
    {
        private List<Command_SwitchDropLogic> others;

        public CompDropExtinguisherWhenUndrafted compExtinguisher;

        public override bool GroupsWith(Gizmo other)
        {
            Command_SwitchDropLogic command_Reload = other as Command_SwitchDropLogic;
            return command_Reload != null;
        }

        public override void MergeWith(Gizmo other)
        {
            Command_SwitchDropLogic item = other as Command_SwitchDropLogic;
            if (others == null)
            {
                others = new List<Command_SwitchDropLogic>();
                others.Add(this);
            }
            others.Add(item);
        }

        public override void ProcessInput(Event ev)
        {
            if (compExtinguisher.pawn.TryGetComp<CompInventory>() != null)
            {
                Find.WindowStack.Add(MakeAmmoMenu());
            }
        }

        private FloatMenu MakeAmmoMenu()
        {
            return new FloatMenu(BuildAmmoOptions());
        }

        private List<FloatMenuOption> BuildAmmoOptions()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            Action action1 = delegate
            {
                compExtinguisher.switchMode(DropLogic.DontDrop);
            };
            Action action2 = delegate
            {
                compExtinguisher.switchMode(DropLogic.AlwaysDrop);
            };
            Action action3 = delegate
            {
                compExtinguisher.switchMode(DropLogic.DropIfNotFull);
            };
            Action action4 = delegate
            {
                compExtinguisher.switchMode(DropLogic.DropWhenFull);
            };
            Action action5 = delegate
            {
                compExtinguisher.switchMode(DropLogic.DropWhenEmpty);
            };

            list.Add(new FloatMenuOption("dontDrop".Translate(), action1));
            list.Add(new FloatMenuOption("alwaysDrop".Translate(), action2));
            list.Add(new FloatMenuOption("dropIfNotFull".Translate(), action3));
            list.Add(new FloatMenuOption("dropWhenFull".Translate(), action4));
            list.Add(new FloatMenuOption("dropWhenEmpty".Translate(), action5));
            return list;
        }
    }

    public class JobGiver_DropExtinguisher : ThinkNode_JobGiver
    {
        private const bool forceReloadWhenLookingForWork = true;
        public override float GetPriority(Pawn pawn)
        {
            return 10f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            ThingWithComps Thing = FindExtinguisher(pawn);
            if (!pawn.CanReserve(Thing, 1, -1, null))
            {
                return null;
            }
            if (Thing == null || Thing.TryGetComp<CompDropExtinguisherWhenUndrafted>() == null)
            {
                return null;
            }
            if (!StoreUtility.TryFindBestBetterStorageFor(Thing, pawn, pawn.Map, StoragePriority.Unstored, pawn.Faction, out var _, out var _, needAccurateResult: false))
            {
                return null;
            }
            if (pawn.equipment.TryDropEquipment(Thing, out ThingWithComps t, pawn.Position, false)) ;
            {
                return HaulAIUtility.HaulToStorageJob(pawn, Thing);
            }
            return null;
        }

        public static ThingWithComps FindExtinguisher(Pawn pawn)
        {
            List<ThingWithComps> weapons = pawn?.equipment.AllEquipmentListForReading;
            for (int i = 0; i < weapons.Count; i++)
            {
                CompDropExtinguisherWhenUndrafted comp = weapons[i].TryGetComp<CompDropExtinguisherWhenUndrafted>();
                if (comp != null && comp.shouldDrop)
                {
                    return weapons[i];
                }
            }
            return null;
        }
    }
}
