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

    private CompAutoDoc autoDoc;

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

    private bool SurgeryInProgress { get; set; }

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
                new FloatMenuOption("AuDo_Enter".Translate(), makeJob),
                myPawn, (LocalTargetInfo)this);
        }

        yield break;

        void makeJob()
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

        yield return exitAutoDoc();
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        GenDraw.DrawFieldEdges(getMaterialSearchRect().Cells.ToList());
    }

    private Gizmo exitAutoDoc()
    {
        var commandAction = new Command_Action
        {
            defaultLabel = "AuDo_Exit".Translate(),
            action = EjectContents,
            defaultDesc = "AuDo_ExitTT".Translate(),
            Disabled = false,
            icon = ContentFinder<Texture2D>.Get("Icons/ExitAutoDoc")
        };
        if (SurgeryInProgress)
        {
            commandAction.Disable("AuDo_Busy".Translate());
        }
        else if (PawnContained == null)
        {
            commandAction.Disable("AuDo_Empty".Translate());
        }

        return commandAction;
    }

    public void SetSurgeryInProgress(bool setting)
    {
        SurgeryInProgress = setting;
    }

    public override void EjectContents()
    {
        innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
        contentsKnown = true;
        autoDoc.Reset();
    }

    private CellRect getMaterialSearchRect()
    {
        var position = Position;
        var dimensions = deterDimensions();
        position.x += dimensions[2];
        position.z += dimensions[3];
        return CellRect.CenteredOn(position, dimensions[0], dimensions[1]);
    }

    private int[] deterDimensions()
    {
        return Rotation.ToString() switch
        {
            "0" => [3, 4, 0, 1],
            "1" => [4, 3, 1, 0],
            "2" => [3, 4, 0, 0],
            _ => [4, 3, 0, 0]
        };
    }
}
