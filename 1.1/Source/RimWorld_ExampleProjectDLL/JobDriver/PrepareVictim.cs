using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;

namespace HeadsMightRoll
{
	public class JobDriver_TakeToGuillotine : JobDriver
	{
		private const TargetIndex TakeeIndex = TargetIndex.A;

		private const TargetIndex BedIndex = TargetIndex.B;

		protected Pawn Takee
		{
			get
			{
				return (Pawn)job.GetTarget(TargetIndex.A).Thing;
			}
		}

		protected Building_Bed DropBed
		{
			get
			{
				return (Building_Bed)job.GetTarget(TargetIndex.B).Thing;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo target = Takee;
			Job job = this.job;
			bool arg_6A_0;
			if (pawn.Reserve(target, job, 1, -1, null, errorOnFailed))
			{
				pawn = this.pawn;
				target = DropBed;
				job = this.job;
				int sleepingSlotsCount = DropBed.SleepingSlotsCount;
				int stackCount = 0;
				arg_6A_0 = pawn.Reserve(target, job, sleepingSlotsCount, stackCount, null, errorOnFailed);
			}
			else
			{
				arg_6A_0 = false;
			}
			return arg_6A_0;
		}

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnAggroMentalStateAndHostile(TargetIndex.A);
            this.FailOn(delegate
            {
                if (job.def.makeTargetPrisoner)
                {
                    if (!DropBed.ForPrisoners)
                    {
                        return true;
                    }
                }
                else if (DropBed.ForPrisoners != Takee.IsPrisoner)
                {
                    return true;
                }
                return false;
            });
            yield return Toils_Bed.ClaimBedIfNonMedical(TargetIndex.B, TargetIndex.A);
            base.AddFinishAction(delegate
            {
                if (job.def.makeTargetPrisoner && Takee.ownership.OwnedBed == DropBed && Takee.Position != RestUtility.GetBedSleepingSlotPosFor(Takee, DropBed))
                {
                    Takee.ownership.UnclaimBed();
                }
            });
            yield return Toils_Goto.GotoThing(
                TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOn(() =>
                job.def == JobDefOf.Arrest && Takee.CanBeArrestedBy(pawn)).FailOn(() =>
                pawn.CanReach(DropBed, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn)).FailOn(() => job.def == JobDefOf.Rescue && !Takee.Downed).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate
                {
                    if (job.def.makeTargetPrisoner)
                    {
                        Pawn pawn = (Pawn)job.targetA.Thing;
                        Lord lord = pawn.GetLord();
                        if (lord != null)
                        {
                            lord.Notify_PawnAttemptArrested(pawn);
                        }
                        GenClamor.DoClamor(pawn, 10f, ClamorDefOf.Harm);
                        if (job.def == JobDefOf.Arrest && !pawn.CheckAcceptArrest(pawn))
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                        }
                    }
                }
            };
            Toil startCarrying = Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false).FailOnNonMedicalBedNotOwned(TargetIndex.B, TargetIndex.A);
            startCarrying.AddPreInitAction(new Action(CheckMakeTakeeGuest));
            yield return startCarrying;
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            yield return new Toil
            {
                initAction = delegate
                {
                    CheckMakeTakeePrisoner();
                    if (Takee.playerSettings == null)
                    {
                        Takee.playerSettings = new Pawn_PlayerSettings(Takee);
                    }
                }
            };
            yield return Toils_Reserve.Release(TargetIndex.B);
            yield return new Toil
            {
                initAction = delegate
                {
                    IntVec3 position = DropBed.Position;
                    Thing thing;
                    pawn.carryTracker.TryDropCarriedThing(position, ThingPlaceMode.Direct, out thing, null);
                    //if (!DropBed.Destroyed && (DropBed.owners.Contains(Takee) || (DropBed.Medical && DropBed.AnyUnoccupiedSleepingSlot) || Takee.ownership == null))
                    if (!DropBed.Destroyed && (DropBed.OwnersForReading.Contains(Takee) || (DropBed.Medical && DropBed.AnyUnoccupiedSleepingSlot) || Takee.ownership == null))
                    {
                        Takee.jobs.Notify_TuckedIntoBed(DropBed);
                        if (Takee.RaceProps.Humanlike && job.def != JobDefOf.Arrest && !Takee.IsPrisonerOfColony)
                        {
                            Takee.relations.Notify_RescuedBy(pawn);
                        }
                        Takee.mindState.Notify_TuckedIntoBed();
                    }
                    if (Takee.IsPrisonerOfColony)
                    {
                        LessonAutoActivator.TeachOpportunity(ConceptDefOf.PrisonerTab, Takee, OpportunityType.GoodToKnow);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private void CheckMakeTakeePrisoner()
        {
            if (job.def.makeTargetPrisoner)
            {
                if (Takee.guest.Released)
                {
                    Takee.guest.Released = false;
                    Takee.guest.interactionMode = PrisonerInteractionModeDefOf.NoInteraction;
                }
                if (!Takee.IsPrisonerOfColony)
                {
                    Takee.guest.CapturedBy(Faction.OfPlayer, pawn);
                }
            }
        }

        private void CheckMakeTakeeGuest()
        {
            if (!job.def.makeTargetPrisoner && Takee.Faction != Faction.OfPlayer && Takee.HostFaction != Faction.OfPlayer && Takee.guest != null && !Takee.IsWildMan())
            {
                Takee.guest.SetGuestStatus(Faction.OfPlayer, false);
            }
        }
	}
}