using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AutoDoc;

internal class AutoDocBuilding : Building_CryptosleepCasket
{
    public const int tickRate = 60;

    public CompAutoDoc autoDoc;

    private CompBreakdownable breakdownable;
    private CompPowerTrader powerComp;

    public bool AutoDocActive
    {
        get
        {
            var compPowerTrader = powerComp;
            if (compPowerTrader == null || compPowerTrader.PowerOn)
            {
                return !(breakdownable?.BrokenDown ?? false);
            }

            return false;
        }
    }

    public bool SurgeryInProgress { get; set; }

    public Pawn PawnContained => innerContainer.FirstOrDefault() as Pawn;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        SurgeryInProgress = false;
        base.SpawnSetup(map, respawningAfterLoad);
        powerComp = GetComp<CompPowerTrader>();
        breakdownable = GetComp<CompBreakdownable>();
        autoDoc = GetComp<CompAutoDoc>();
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
    {
        JobDef jobDef;
        if (!myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
        {
            yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
        }
        else if (PawnContained == null)
        {
            jobDef = JobDefOf.EnterCryptosleepCasket;
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption("AuDo_Enter".Translate(), MakeJob),
                myPawn, (LocalTargetInfo)this);
        }

        yield break;

        void MakeJob()
        {
            var job = JobMaker.MakeJob(jobDef, this);
            myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        yield return ExitAutoDoc();
    }

    private Gizmo ExitAutoDoc()
    {
        var command_Action = new Command_Action
        {
            defaultLabel = "AuDo_Exit".Translate(),
            action = EjectContents,
            defaultDesc = "AuDo_ExitTT".Translate(),
            Disabled = false,
            icon = ContentFinder<Texture2D>.Get("Icons/ExitAutoDoc")
        };
        if (SurgeryInProgress)
        {
            command_Action.Disable("AuDo_Busy".Translate());
        }
        else if (PawnContained == null)
        {
            command_Action.Disable("AuDo_Empty".Translate());
        }

        return command_Action;
    }

    public void SetSurgeryInProgress(bool setting)
    {
        SurgeryInProgress = setting;
    }

    public override void EjectContents()
    {
        base.EjectContents();
        autoDoc.Reset();
    }
}