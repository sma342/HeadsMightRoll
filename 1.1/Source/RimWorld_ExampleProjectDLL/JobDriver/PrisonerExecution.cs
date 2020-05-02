using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;
using RimWorld;

namespace HeadsMightRoll
{
	public class JobDriver_Execute : JobDriver
	{
		protected Pawn Victim
		{
			get
			{
				return (Pawn)job.targetA.Thing;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo target = Victim;
			Job job = this.job;
			return pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
		}

		[DebuggerHidden]
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnAggroMentalState(TargetIndex.A);
			yield return Toils_Interpersonal.GotoPrisoner(pawn, Victim, PrisonerInteractionModeDefOf.Execution).FailOn(() => !Victim.IsPrisonerOfColony || !Victim.guest.PrisonerIsSecure);
			Toil execute = new Toil();
			execute.initAction = delegate
			{
				ExecutionUtility.DoExecutionByCut(execute.actor, Victim);
				ThoughtUtility.GiveThoughtsForPawnExecuted(Victim, PawnExecutionKind.GenericBrutal);
				TaleRecorder.RecordTale(TaleDefOf.ExecutedPrisoner, new object[]
				{
					pawn,
					Victim
				});
			};
			execute.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return execute;
		}
	}
}