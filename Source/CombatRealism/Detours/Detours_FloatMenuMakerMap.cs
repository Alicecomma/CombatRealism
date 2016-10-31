using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityCoreLibrary;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Combat_Realism.Detours
{
    internal class Detours_FloatMenuMakerMap
    {
        internal static bool CanTakeOrder(Pawn pawn)
        {
            return pawn.IsColonistPlayerControlled && pawn.drafter.CanTakeOrderedJob();
        }

        [DetourClassMethod(typeof(FloatMenuMakerMap), "ChoicesAtFor", InjectionSequence.DLLLoad, InjectionTiming.Priority_23)]
        internal static List<FloatMenuOption> ChoicesAtFor(Vector3 clickPos, Pawn pawn)
        {
            IntVec3 clickCell = IntVec3.FromVector3(clickPos);
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            if (!clickCell.InBounds() || !CanTakeOrder(pawn))
            {
                return list;
            }
            DangerUtility.NotifyDirectOrderingThisFrame(pawn);

            // ***** Beginning of drafted options *****
            FloatMenuMakerMap.making = true;
            try
            {
                if (pawn.Drafted)
                {
                    AddDraftedOrders(clickPos, pawn, list);
                }
                if (pawn.RaceProps.Humanlike)
                {
                    AddHumanlikeOrders(clickPos, pawn, list);
                }
                if (!pawn.Drafted)
                {
                    AddUndraftedOrders(clickPos, pawn, list);
                }
                foreach (FloatMenuOption current in pawn.GetExtraFloatMenuOptionsFor(clickCell))
                {
                    list.Add(current);
                }
            }
            finally
            {
                DangerUtility.DoneDirectOrdering();
                FloatMenuMakerMap.making = false;
            }
            return list;
        }

        internal static void AddDraftedOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 b = IntVec3.FromVector3(clickPos);
            foreach (TargetInfo attackTarg in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttackHostile(), true))
            {
                if (pawn.equipment.Primary != null && !pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.MeleeRange)
                {
                    string str;
                    Action rangedAct = FloatMenuUtility.GetRangedAttackAction(pawn, attackTarg, out str);
                    string text = "FireAt".Translate(new object[]
                    {
                        attackTarg.Thing.LabelCap
                    });
                    FloatMenuOption floatMenuOption = new FloatMenuOption();
                    floatMenuOption.priority = MenuOptionPriority.High;
                    if (rangedAct == null)
                    {
                        text = text + " (" + str + ")";
                    }
                    else
                    {
                        floatMenuOption.autoTakeable = true;
                        floatMenuOption.action = new Action(delegate
                        {
                            MoteMaker.MakeStaticMote(attackTarg.Thing.DrawPos, ThingDefOf.Mote_FeedbackAttack, 1f);
                            rangedAct();
                        });
                    }
                    floatMenuOption.Label = text;
                    opts.Add(floatMenuOption);
                }
                string str2;
                Action meleeAct = FloatMenuUtility.GetMeleeAttackAction(pawn, attackTarg, out str2);
                Pawn pawn2 = attackTarg.Thing as Pawn;
                string text2;
                if (pawn2 != null && pawn2.Downed)
                {
                    text2 = "MeleeAttackToDeath".Translate(new object[]
                    {

                        attackTarg.Thing.LabelCap
                    });
                }
                else
                {
                    text2 = "MeleeAttack".Translate(new object[]
                    {

                        attackTarg.Thing.LabelCap
                    });
                }
                Thing thing = attackTarg.Thing;
                FloatMenuOption floatMenuOption2 = new FloatMenuOption(string.Empty, null, MenuOptionPriority.High, null, thing, 0f, null);
                if (meleeAct == null)
                {
                    text2 = text2 + " (" + str2 + ")";
                }
                else
                {
                    floatMenuOption2.action = delegate
                    {
                        MoteMaker.MakeStaticMote(attackTarg.Thing.DrawPos, ThingDefOf.Mote_FeedbackAttack, 1f);
                        meleeAct();
                    };
                }
                floatMenuOption2.Label = text2;
                opts.Add(floatMenuOption2);
            }
            if (pawn.RaceProps.Humanlike && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                foreach (TargetInfo current2 in GenUI.TargetsAt(clickPos, TargetingParameters.ForArrest(pawn), true))
                {
                    TargetInfo dest = current2;
                    if (!((Pawn)dest.Thing).Downed)
                    {
                        if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            opts.Add(new FloatMenuOption("CannotArrest".Translate() + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null, 0f, null));
                        }
                        else if (!pawn.CanReserve(dest.Thing, 1))
                        {
                            opts.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "Reserved".Translate(), null, MenuOptionPriority.Medium, null, null, 0f, null));
                        }
                        else
                        {
                            Pawn pTarg = (Pawn)dest.Thing;
                            Action action = delegate
                            {
                                Building_Bed building_Bed = RestUtility.FindBedFor(pTarg, pawn, true, false, false);
                                if (building_Bed == null)
                                {
                                    Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(), pTarg, MessageSound.RejectInput);
                                    return;
                                }
                                Job job = new Job(JobDefOf.Arrest, pTarg, building_Bed);
                                job.playerForced = true;
                                job.maxNumToCarry = 1;
                                pawn.drafter.TakeOrderedJob(job);
                                TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies);
                            };
                            Thing thing = dest.Thing;
                            opts.Add(new FloatMenuOption("TryToArrest".Translate(new object[]
                            {
                                dest.Thing.LabelCap
                            }), action, MenuOptionPriority.Medium, null, thing, 0f, null));
                        }
                    }
                }
            }
            int num = GenRadial.NumCellsInRadius(2.9f);
            IntVec3 curLoc;
            for (int i = 0; i < num; i++)
            {
                curLoc = GenRadial.RadialPattern[i] + b;
                if (curLoc.Standable())
                {
                    if (curLoc != pawn.Position)
                    {
                        if (!pawn.CanReach(curLoc, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            FloatMenuOption item = new FloatMenuOption("CannotGoNoPath".Translate(), null, MenuOptionPriority.Low, null, null, 0f, null);
                            opts.Add(item);
                        }
                        else
                        {
                            Action action2 = delegate
                            {
                                IntVec3 intVec = Pawn_DraftController.BestGotoDestNear(curLoc, pawn);
                                Job job = new Job(JobDefOf.Goto, intVec);
                                job.playerForced = true;
                                pawn.drafter.TakeOrderedJob(job);
                                MoteMaker.MakeStaticMote(intVec, ThingDefOf.Mote_FeedbackGoto, 1f);
                            };
                            opts.Add(new FloatMenuOption("GoHere".Translate(), action2, MenuOptionPriority.Low, null, null, 0f, null)
                            {
                                autoTakeable = true
                            });
                        }
                    }
                    break;
                }
            }
        }

        internal static void AddHumanlikeOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 c2 = IntVec3.FromVector3(clickPos);
            int num = 0;
            if (pawn.story != null)
            {
                num = pawn.story.traits.DegreeOfTrait(TraitDefOf.DrugDesire);
            }
            foreach (Thing current in c2.GetThingList())
            {
                Thing t = current;
                if (t.def.ingestible != null && pawn.RaceProps.CanEverEat(t) && t.IngestibleNow)
                {
                    FloatMenuOption item;
                    if (t.def.IsPleasureDrug && num < 0)
                    {
                        item = new FloatMenuOption("ConsumeThing".Translate(new object[]
                        {
                            t.LabelShort
                        }) + " (" + "Teetotaler".Translate() + ")", null, MenuOptionPriority.Medium, null, null, 0f, null);
                    }
                    else if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                    {
                        item = new FloatMenuOption("ConsumeThing".Translate(new object[]
                        {
                            t.LabelShort
                        }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null, 0f, null);
                    }
                    else if (!pawn.CanReserve(t, 1))
                    {
                        item = new FloatMenuOption("ConsumeThing".Translate(new object[]
                        {
                            t.LabelShort
                        }) + " (" + "ReservedBy".Translate(new object[]
                        {
                            Find.Reservations.FirstReserverOf(t, pawn.Faction, true).LabelShort
                        }) + ")", null, MenuOptionPriority.Medium, null, null, 0f, null);
                    }
                    else
                    {
                        item = new FloatMenuOption("ConsumeThing".Translate(new object[]
                        {
                            t.LabelShort
                        }), delegate
                        {
                            t.SetForbidden(false, true);
                            Job job = new Job(JobDefOf.Ingest, t);
                            job.maxNumToCarry = FoodUtility.WillIngestStackCountOf(pawn, t.def);
                            pawn.drafter.TakeOrderedJob(job);
                        }, MenuOptionPriority.Medium, null, null, 0f, null);
                    }
                    opts.Add(item);
                }
            }
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                foreach (TargetInfo current2 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    Pawn victim = (Pawn)current2.Thing;
                    if (!victim.InBed() && pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1) && !victim.IsPrisonerOfColony)
                    {
                        if ((victim.Faction == Faction.OfPlayer && victim.MentalStateDef == null) || (victim.Faction != Faction.OfPlayer && victim.MentalStateDef == null && !victim.IsPrisonerOfColony && (victim.Faction == null || !victim.Faction.HostileTo(Faction.OfPlayer))))
                        {
                            Pawn victim2 = victim;
                            opts.Add(new FloatMenuOption("Rescue".Translate(new object[]
                            {
                                victim.LabelCap
                            }), delegate
                            {
                                Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, false, false, false);
                                if (building_Bed == null)
                                {
                                    string str2;
                                    if (victim.RaceProps.Animal)
                                    {
                                        str2 = "NoAnimalBed".Translate();
                                    }
                                    else
                                    {
                                        str2 = "NoNonPrisonerBed".Translate();
                                    }
                                    Messages.Message("CannotRescue".Translate() + ": " + str2, victim, MessageSound.RejectInput);
                                    return;
                                }
                                Job job = new Job(JobDefOf.Rescue, victim, building_Bed);
                                job.maxNumToCarry = 1;
                                job.playerForced = true;
                                pawn.drafter.TakeOrderedJob(job);
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                            }, MenuOptionPriority.Medium, null, victim2, 0f, null));
                        }
                        if (victim.MentalStateDef != null || (victim.RaceProps.Humanlike && victim.Faction != Faction.OfPlayer))
                        {
                            Pawn victim2 = victim;
                            opts.Add(new FloatMenuOption("Capture".Translate(new object[]
                            {
                                victim.LabelCap
                            }), delegate
                            {
                                Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, false);
                                if (building_Bed == null)
                                {
                                    Messages.Message("CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(), victim, MessageSound.RejectInput);
                                    return;
                                }
                                Job job = new Job(JobDefOf.Capture, victim, building_Bed);
                                job.maxNumToCarry = 1;
                                job.playerForced = true;
                                pawn.drafter.TakeOrderedJob(job);
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                            }, MenuOptionPriority.Medium, null, victim2, 0f, null));
                        }
                    }
                }
                foreach (TargetInfo current3 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    TargetInfo targetInfo = current3;
                    Pawn victim = (Pawn)targetInfo.Thing;
                    if (victim.Downed && pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1) && Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn) != null)
                    {
                        string label = "CarryToCryptosleepCasket".Translate(new object[]
                        {
                            targetInfo.Thing.LabelCap
                        });
                        JobDef jDef = JobDefOf.CarryToCryptosleepCasket;
                        Action action = delegate
                        {
                            Building_CryptosleepCasket building_CryptosleepCasket = Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn);
                            if (building_CryptosleepCasket == null)
                            {
                                Messages.Message("CannotCarryToCryptosleepCasket".Translate() + ": " + "NoCryptosleepCasket".Translate(), victim, MessageSound.RejectInput);
                                return;
                            }
                            Job job = new Job(jDef, victim, building_CryptosleepCasket);
                            job.maxNumToCarry = 1;
                            job.playerForced = true;
                            pawn.drafter.TakeOrderedJob(job);
                        };
                        Pawn victim2 = victim;
                        opts.Add(new FloatMenuOption(label, action, MenuOptionPriority.Medium, null, victim2, 0f, null));
                    }
                }
            }
            foreach (TargetInfo current4 in GenUI.TargetsAt(clickPos, TargetingParameters.ForStrip(pawn), true))
            {
                TargetInfo stripTarg = current4;
                FloatMenuOption item2;
                if (!pawn.CanReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    item2 = new FloatMenuOption("CannotStrip".Translate(new object[]
                    {
                        stripTarg.Thing.LabelCap
                    }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null, 0f, null);
                }
                else if (!pawn.CanReserveAndReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly, 1))
                {
                    item2 = new FloatMenuOption("CannotStrip".Translate(new object[]
                    {
                        stripTarg.Thing.LabelCap
                    }) + " (" + "ReservedBy".Translate(new object[]
                    {
                        Find.Reservations.FirstReserverOf(stripTarg, pawn.Faction, true).LabelShort
                    }) + ")", null, MenuOptionPriority.Medium, null, null, 0f, null);
                }
                else
                {
                    item2 = new FloatMenuOption("Strip".Translate(new object[]
                    {
                        stripTarg.Thing.LabelCap
                    }), delegate
                    {
                        stripTarg.Thing.SetForbidden(false, false);
                        Job job = new Job(JobDefOf.Strip, stripTarg);
                        job.playerForced = true;
                        pawn.drafter.TakeOrderedJob(job);
                    }, MenuOptionPriority.Medium, null, null, 0f, null);
                }
                opts.Add(item2);
            }


            CompInventory compInventory = pawn.TryGetComp<CompInventory>();      // Need compInventory here for equip and wear options
            if (pawn.equipment != null)
            {
                ThingWithComps equipment = null;
                List<Thing> thingList = c2.GetThingList();
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i].TryGetComp<CompEquippable>() != null)
                    {
                        equipment = (ThingWithComps)thingList[i];
                        break;
                    }
                }
                if (equipment != null)
                {
                    string label2 = equipment.Label;
                    FloatMenuOption item3;
                    if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                    {
                        item3 = new FloatMenuOption("CannotEquip".Translate(new object[]
                        {
                            label2
                        }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null, 0f, null);
                    }
                    else if (!pawn.CanReserve(equipment, 1))
                    {
                        item3 = new FloatMenuOption("CannotEquip".Translate(new object[]
                        {
                            label2
                        }) + " (" + "ReservedBy".Translate(new object[]
                        {
                            Find.Reservations.FirstReserverOf(equipment, pawn.Faction, true).LabelShort
                        }) + ")", null, MenuOptionPriority.Medium, null, null, 0f, null);
                    }
                    else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    {
                        item3 = new FloatMenuOption("CannotEquip".Translate(new object[]
                        {
                            label2
                        }) + " (" + "Incapable".Translate() + ")", null, MenuOptionPriority.Medium, null, null, 0f, null);
                    }
                    else
                    {
                        string text = "Equip".Translate(new object[]
                        {
                            label2
                        });
                        if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                        {
                            text = text + " " + "EquipWarningBrawler".Translate();
                        }
                        item3 = new FloatMenuOption(text, delegate
                        {
                            equipment.SetForbidden(false, true);
                            Job job = new Job(JobDefOf.Equip, equipment);
                            job.playerForced = true;
                            pawn.drafter.TakeOrderedJob(job);
                            MoteMaker.MakeStaticMote(equipment.DrawPos, ThingDefOf.Mote_FeedbackEquip, 1f);
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                        }, MenuOptionPriority.Medium, null, null, 0f, null);
                    }
                    opts.Add(item3);
                }
            }
            if (pawn.apparel != null)
            {
                Apparel apparel = Find.ThingGrid.ThingAt<Apparel>(c2);
                if (apparel != null)
                {
                    FloatMenuOption item4;
                    if (!pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                    {
                        item4 = new FloatMenuOption("CannotWear".Translate(new object[]
                        {
                            apparel.Label
                        }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null, 0f, null);
                    }
                    else if (!pawn.CanReserve(apparel, 1))
                    {
                        Pawn pawn2 = Find.Reservations.FirstReserverOf(apparel, pawn.Faction, true);
                        item4 = new FloatMenuOption("CannotWear".Translate(new object[]
                        {
                            apparel.Label
                        }) + " (" + "ReservedBy".Translate(new object[]
                        {
                            pawn2.LabelShort
                        }) + ")", null, MenuOptionPriority.Medium, null, null, 0f, null);
                    }
                    else if (!ApparelUtility.HasPartsToWear(pawn, apparel.def))
                    {
                        item4 = new FloatMenuOption("CannotWear".Translate(new object[]
                        {
                            apparel.Label
                        }) + " (" + "CannotWearBecauseOfMissingBodyParts".Translate() + ")", null, MenuOptionPriority.Medium, null, null, 0f, null);
                    }
                    else
                    {
                        // Added check for inventory capacity
                        int count;
                        if (compInventory != null && !compInventory.CanFitInInventory(apparel, out count, false, true))
                        {
                            item4 = new FloatMenuOption("CannotWear".Translate(new object[] { apparel.Label }) + " (" + "CR_InventoryFull".Translate() + ")", null);
                        }
                        else
                        {
                            item4 = new FloatMenuOption("ForceWear".Translate(new object[] { apparel.LabelShort }),
                                new Action(delegate
                                {
                                    apparel.SetForbidden(false, true);
                                    Job job = new Job(JobDefOf.Wear, apparel);
                                    job.playerForced = true;
                                    pawn.drafter.TakeOrderedJob(job);
                                }),
                                    MenuOptionPriority.Medium, null, null);
                        }
                    }
                    opts.Add(item4);
                }
            }


            // *** NEW: Pick up option ***
            if (compInventory != null)
            {
                List<Thing> thingList = c2.GetThingList();
                if (!thingList.NullOrEmpty<Thing>())
                {
                    Thing item = thingList.FirstOrDefault(thing => thing.def.alwaysHaulable && !(thing is Corpse));
                    if (item != null)
                    {
                        FloatMenuOption pickUpOption;
                        int count = 0;
                        if (!pawn.CanReach(item, PathEndMode.Touch, Danger.Deadly))
                        {
                            pickUpOption = new FloatMenuOption("CR_CannotPickUp".Translate() + " " + item.LabelShort + " (" + "NoPath".Translate() + ")", null);
                        }
                        else if (!pawn.CanReserve(item))
                        {
                            pickUpOption = new FloatMenuOption("CR_CannotPickUp".Translate() + " " + item.LabelShort + " (" + "ReservedBy".Translate(new object[] { Find.Reservations.FirstReserverOf(item, pawn.Faction) }), null);
                        }
                        else if (!compInventory.CanFitInInventory(item, out count))
                        {
                            pickUpOption = new FloatMenuOption("CR_CannotPickUp".Translate() + " " + item.LabelShort + " (" + "CR_InventoryFull".Translate() + ")", null);
                        }
                        else
                        {
                            pickUpOption = new FloatMenuOption("CR_PickUp".Translate() + " " + item.LabelShort,
                                new Action(delegate
                                {
                                    item.SetForbidden(false);
                                    Job job = new Job(JobDefOf.TakeInventory, item) { maxNumToCarry = 1 };
                                    job.playerForced = true;
                                    pawn.drafter.TakeOrderedJob(job);
                                }));
                        }
                        opts.Add(pickUpOption);
                        if (count > 1 && item.stackCount > 1)
                        {
                            int numToCarry = Math.Min(count, item.stackCount);
                            FloatMenuOption pickUpStackOption = new FloatMenuOption("CR_PickUp".Translate() + " " + item.LabelShort + " x" + numToCarry.ToString(),
                                new Action(delegate
                                {
                                    item.SetForbidden(false);
                                    Job job = new Job(JobDefOf.TakeInventory, item) { maxNumToCarry = numToCarry };
                                    job.playerForced = true;
                                    pawn.drafter.TakeOrderedJob(job);
                                }));
                            opts.Add(pickUpStackOption);

                            FloatMenuOption pickUpHalfStackOption = new FloatMenuOption("CE_PickUpHalf" + " " + item.LabelShort + " x" + (numToCarry / 2).ToString(),
                               new Action(delegate
                               {
                                   item.SetForbidden(false);
                                   Job job = new Job(JobDefOf.TakeInventory, item) { maxNumToCarry = numToCarry / 2 };
                                   job.playerForced = true;
                                   pawn.drafter.TakeOrderedJob(job);
                               }));
                            opts.Add(pickUpHalfStackOption);
                        }
                    }
                }
            }

            if (pawn.equipment != null && pawn.equipment.Primary != null)
            {
                Thing thing = Find.ThingGrid.ThingAt(c2, ThingDefOf.EquipmentRack);
                if (thing != null)
                {
                    if (!pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                    {
                        opts.Add(new FloatMenuOption("CannotDeposit".Translate(new object[]
                        {
                            pawn.equipment.Primary.LabelCap,
                            thing.def.label
                        }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null, 0f, null));
                    }
                    else
                    {
                        foreach (IntVec3 c in GenAdj.CellsOccupiedBy(thing))
                        {
                            if (c.GetStorable() == null && pawn.CanReserveAndReach(c, PathEndMode.ClosestTouch, Danger.Deadly, 1))
                            {
                                Action action2 = delegate
                                {
                                    ThingWithComps t;
                                    if (pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out t, pawn.Position, true))
                                    {
                                        t.SetForbidden(false, true);
                                        Job job = new Job(JobDefOf.HaulToCell, t, c);
                                        job.haulMode = HaulMode.ToCellStorage;
                                        job.maxNumToCarry = 1;
                                        job.playerForced = true;
                                        pawn.drafter.TakeOrderedJob(job);
                                    }
                                };
                                opts.Add(new FloatMenuOption("Deposit".Translate(new object[]
                                {
                                    pawn.equipment.Primary.LabelCap,
                                    thing.def.label
                                }), action2, MenuOptionPriority.Medium, null, null, 0f, null));
                                break;
                            }
                        }
                    }
                }
                if (pawn.equipment != null && GenUI.TargetsAt(clickPos, TargetingParameters.ForSelf(pawn), true).Any<TargetInfo>())
                {
                    Action action3 = delegate
                    {
                        ThingWithComps thingWithComps;
                        pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out thingWithComps, pawn.Position, true);
                        pawn.drafter.TakeOrderedJob(new Job(JobDefOf.Wait, 20, false));
                    };
                    opts.Add(new FloatMenuOption("Drop".Translate(new object[]
                    {
                        pawn.equipment.Primary.Label
                    }), action3, MenuOptionPriority.Medium, null, null, 0f, null));
                }
            }
            foreach (TargetInfo current5 in GenUI.TargetsAt(clickPos, TargetingParameters.ForTrade(), true))
            {
                TargetInfo dest = current5;
                if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    opts.Add(new FloatMenuOption("CannotTrade".Translate() + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null, 0f, null));
                }
                else if (!pawn.CanReserve(dest.Thing, 1))
                {
                    opts.Add(new FloatMenuOption("CannotTrade".Translate() + " (" + "Reserved".Translate() + ")", null, MenuOptionPriority.Medium, null, null, 0f, null));
                }
                else
                {
                    Pawn pTarg = (Pawn)dest.Thing;
                    Action action4 = delegate
                    {
                        Job job = new Job(JobDefOf.TradeWithPawn, pTarg);
                        job.playerForced = true;
                        pawn.drafter.TakeOrderedJob(job);
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InteractingWithTraders, KnowledgeAmount.Total);
                    };
                    string str = string.Empty;
                    if (pTarg.Faction != null)
                    {
                        str = " (" + pTarg.Faction.Name + ")";
                    }
                    Thing thing2 = dest.Thing;
                    opts.Add(new FloatMenuOption("TradeWith".Translate(new object[]
                    {
                        pTarg.LabelShort
                    }) + str, action4, MenuOptionPriority.Medium, null, thing2, 0f, null));
                }
            }
            foreach (Thing current6 in Find.ThingGrid.ThingsAt(c2))
            {
                foreach (FloatMenuOption current7 in current6.GetFloatMenuOptions(pawn))
                {
                    opts.Add(current7);
                }
            }
        }

        internal static void AddUndraftedOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 clickCell = IntVec3.FromVector3(clickPos);
            bool flag = false;
            bool flag2 = false;
            foreach (Thing current in Find.ThingGrid.ThingsAt(clickCell))
            {
                flag2 = true;
                if (pawn.CanReach(current, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    flag = true;
                    break;
                }
            }
            if (flag2 && !flag)
            {
                opts.Add(new FloatMenuOption("(" + "NoPath".Translate() + ")", null, MenuOptionPriority.Medium, null, null, 0f, null));
                return;
            }
            JobGiver_Work jobGiver_Work = pawn.thinker.TryGetMainTreeThinkNode<JobGiver_Work>();
            if (jobGiver_Work != null)
            {
                foreach (Thing current2 in Find.ThingGrid.ThingsAt(clickCell))
                {
                    Pawn pawn2 = Find.Reservations.FirstReserverOf(current2, pawn.Faction, true);
                    if (pawn2 != null && pawn2 != pawn)
                    {
                        opts.Add(new FloatMenuOption("IsReservedBy".Translate(new object[]
                        {
                            current2.LabelShort.CapitalizeFirst(),
                            pawn2.LabelShort
                        }), null, MenuOptionPriority.Medium, null, null, 0f, null));
                    }
                    else
                    {
                        foreach (WorkTypeDef current3 in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                        {
                            for (int i = 0; i < current3.workGiversByPriority.Count; i++)
                            {
                                WorkGiver_Scanner workGiver_Scanner = current3.workGiversByPriority[i].Worker as WorkGiver_Scanner;
                                if (workGiver_Scanner != null && workGiver_Scanner.def.directOrderable && !workGiver_Scanner.ShouldSkip(pawn))
                                {
                                    JobFailReason.Clear();
                                    if (workGiver_Scanner.PotentialWorkThingRequest.Accepts(current2) || (workGiver_Scanner.PotentialWorkThingsGlobal(pawn) != null && workGiver_Scanner.PotentialWorkThingsGlobal(pawn).Contains(current2)))
                                    {
                                        Job job;
                                        if (!workGiver_Scanner.HasJobOnThingForced(pawn, current2))
                                        {
                                            job = null;
                                        }
                                        else
                                        {
                                            job = workGiver_Scanner.JobOnThingForced(pawn, current2);
                                        }
                                        if (job == null)
                                        {
                                            if (JobFailReason.HaveReason)
                                            {
                                                string label3 = "CannotGenericWork".Translate(new object[]
                                                {
                                                    workGiver_Scanner.def.verb,
                                                    current2.LabelShort
                                                }) + " (" + JobFailReason.Reason + ")";
                                                opts.Add(new FloatMenuOption(label3, null, MenuOptionPriority.Medium, null, null, 0f, null));
                                            }
                                        }
                                        else
                                        {
                                            WorkTypeDef workType = workGiver_Scanner.def.workType;
                                            Action action = null;
                                            PawnCapacityDef pawnCapacityDef = workGiver_Scanner.MissingRequiredCapacity(pawn);
                                            string label;
                                            if (pawnCapacityDef != null)
                                            {
                                                label = "CannotMissingHealthActivities".Translate(new object[]
                                                {
                                                    pawnCapacityDef.label
                                                });
                                            }
                                            else if (pawn.jobs.curJob != null && pawn.jobs.curJob.JobIsSameAs(job))
                                            {
                                                label = "CannotGenericAlreadyAm".Translate(new object[]
                                                {
                                                    workType.gerundLabel,
                                                    current2.LabelShort
                                                });
                                            }
                                            else if (pawn.workSettings.GetPriority(workType) == 0)
                                            {
                                                label = "CannotPrioritizeIsNotA".Translate(new object[]
                                                {
                                                    pawn.NameStringShort,
                                                    workType.pawnLabel
                                                });
                                            }
                                            else if (job.def == JobDefOf.Research && current2 is Building_ResearchBench)
                                            {
                                                label = "CannotPrioritizeResearch".Translate();
                                            }
                                            else if (current2.IsForbidden(pawn))
                                            {
                                                if (!current2.Position.InAllowedArea(pawn))
                                                {
                                                    label = "CannotPrioritizeForbiddenOutsideAllowedArea".Translate(new object[]
                                                    {
                                                        current2.Label
                                                    });
                                                }
                                                else
                                                {
                                                    label = "CannotPrioritizeForbidden".Translate(new object[]
                                                    {
                                                        current2.Label
                                                    });
                                                }
                                            }
                                            else if (!pawn.CanReach(current2, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
                                            {
                                                label = current2.Label + ": " + "NoPath".Translate();
                                            }
                                            else
                                            {
                                                label = "PrioritizeGeneric".Translate(new object[]
                                                {
                                                    workGiver_Scanner.def.gerund,
                                                    current2.Label
                                                });
                                                Job localJob = job;
                                                WorkGiver_Scanner localScanner = workGiver_Scanner;
                                                action = delegate
                                                {
                                                    pawn.thinker.GetMainTreeThinkNode<JobGiver_Work>().TryStartPrioritizedWorkOn(pawn, localJob, localScanner, clickCell);
                                                };
                                            }
                                            if (!opts.Any((FloatMenuOption op) => op.Label == label.TrimEnd(new char[0])))
                                            {
                                                opts.Add(new FloatMenuOption(label, action, MenuOptionPriority.Medium, null, null, 0f, null));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Pawn pawn3 = Find.Reservations.FirstReserverOf(clickCell, pawn.Faction, true);
                if (pawn3 != null && pawn3 != pawn)
                {
                    opts.Add(new FloatMenuOption("IsReservedBy".Translate(new object[]
                    {
                        "AreaLower".Translate(),
                        pawn3.LabelShort
                    }).CapitalizeFirst(), null, MenuOptionPriority.Medium, null, null, 0f, null));
                }
                else
                {
                    foreach (WorkTypeDef current4 in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                    {
                        for (int j = 0; j < current4.workGiversByPriority.Count; j++)
                        {
                            WorkGiver_Scanner workGiver_Scanner2 = current4.workGiversByPriority[j].Worker as WorkGiver_Scanner;
                            if (workGiver_Scanner2 != null && workGiver_Scanner2.def.directOrderable && !workGiver_Scanner2.ShouldSkip(pawn))
                            {
                                JobFailReason.Clear();
                                if (workGiver_Scanner2.PotentialWorkCellsGlobal(pawn).Contains(clickCell))
                                {
                                    Job job2;
                                    if (!workGiver_Scanner2.HasJobOnCell(pawn, clickCell))
                                    {
                                        job2 = null;
                                    }
                                    else
                                    {
                                        job2 = workGiver_Scanner2.JobOnCell(pawn, clickCell);
                                    }
                                    if (job2 == null)
                                    {
                                        if (JobFailReason.HaveReason)
                                        {
                                            string label2 = "CannotGenericWork".Translate(new object[]
                                            {
                                                workGiver_Scanner2.def.verb,
                                                "AreaLower".Translate()
                                            }) + " (" + JobFailReason.Reason + ")";
                                            opts.Add(new FloatMenuOption(label2, null, MenuOptionPriority.Medium, null, null, 0f, null));
                                        }
                                    }
                                    else
                                    {
                                        WorkTypeDef workType2 = workGiver_Scanner2.def.workType;
                                        Action action2 = null;
                                        PawnCapacityDef pawnCapacityDef2 = workGiver_Scanner2.MissingRequiredCapacity(pawn);
                                        string label;
                                        if (pawnCapacityDef2 != null)
                                        {
                                            label = "CannotMissingHealthActivities".Translate(new object[]
                                            {
                                                pawnCapacityDef2.label
                                            });
                                        }
                                        else if (pawn.jobs.curJob != null && pawn.jobs.curJob.JobIsSameAs(job2))
                                        {
                                            label = "CannotGenericAlreadyAm".Translate(new object[]
                                            {
                                                workType2.gerundLabel,
                                                "AreaLower".Translate()
                                            });
                                        }
                                        else if (pawn.workSettings.GetPriority(workType2) == 0)
                                        {
                                            label = "CannotPrioritizeIsNotA".Translate(new object[]
                                            {
                                                pawn.NameStringShort,
                                                workType2.pawnLabel
                                            });
                                        }
                                        else if (!pawn.CanReach(clickCell, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
                                        {
                                            label = "AreaLower".Translate().CapitalizeFirst() + ": " + "NoPath".Translate();
                                        }
                                        else
                                        {
                                            label = "PrioritizeGeneric".Translate(new object[]
                                            {
                                                workGiver_Scanner2.def.gerund,
                                                "AreaLower".Translate()
                                            });
                                            Job localJob = job2;
                                            WorkGiver_Scanner localScanner = workGiver_Scanner2;
                                            action2 = delegate
                                            {
                                                pawn.thinker.GetMainTreeThinkNode<JobGiver_Work>().TryStartPrioritizedWorkOn(pawn, localJob, localScanner, clickCell);
                                            };
                                        }
                                        if (!opts.Any((FloatMenuOption op) => op.Label == label.TrimEnd(new char[0])))
                                        {
                                            opts.Add(new FloatMenuOption(label, action2, MenuOptionPriority.Medium, null, null, 0f, null));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
