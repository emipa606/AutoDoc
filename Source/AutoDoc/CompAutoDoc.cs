using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace AutoDoc;

internal class CompAutoDoc : ThingComp
{
    private List<Thing> ingredients;
    private CellRect materialSearch;

    private Bill surgeryBill;

    private float timer = -1f;

    public CompPropertiesAutoDocBuilding Properties => props as CompPropertiesAutoDocBuilding;

    private AutoDocBuilding AutoDoc => parent as AutoDocBuilding;

    private Pawn PawnContained => AutoDoc.PawnContained;

    private Map ParentMap { get; set; }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        ParentMap = parent.Map;
        drawRect();
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!AutoDoc.AutoDocActive || PawnContained == null)
        {
            return;
        }

        if (PawnContained.health.HasHediffsNeedingTend())
        {
            tendHediffs();
        }

        if (timer > 0f)
        {
            timer -= 1f;
            return;
        }

        if (surgeryBill != null)
        {
            if (surgeryBill.recipe.Worker is Recipe_RemoveBodyPart ||
                surgeryBill.recipe.Worker.GetType().IsSubclassOf(typeof(Recipe_RemoveBodyPart)))
            {
                var medicalBill = (Bill_Medical)surgeryBill;
                if (medicalBill.Part.def.spawnThingOnRemoved != null)
                {
                    MedicalRecipesUtility.SpawnNaturalPartIfClean(PawnContained, medicalBill.Part,
                        materialSearch.RandomCell, parent.Map);
                    MedicalRecipesUtility.SpawnThingsFromHediffs(PawnContained, medicalBill.Part,
                        materialSearch.RandomCell, parent.Map);
                }
            }

            if (ingredients != null && ingredients.Any())
            {
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var index = 0; index < ingredients.Count; index++)
                {
                    var item3 = ingredients[index];
                    try
                    {
                        surgeryBill.Notify_IterationCompleted(null, null);
                    }
                    catch
                    {
                        // ignored
                    }

                    if (item3 == null || item3.Destroyed)
                    {
                        continue;
                    }

                    if (item3.stackCount > 1)
                    {
                        item3.stackCount--;
                    }
                    else
                    {
                        item3.Destroy();
                    }
                }
            }


            AutoDoc.SetSurgeryInProgress(false);
            timer = -1f;
            surgeryBill = null;
            ingredients = null;
        }

        if (PawnContained.health.surgeryBills.Bills.Count > 0)
        {
            doSurgery();
        }
    }

    private void tendHediffs()
    {
        var hediffs = PawnContained.health.hediffSet.hediffs;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < hediffs.Count; i++)
        {
            if (!hediffs[i].TendableNow())
            {
                continue;
            }

            hediffs[i].Tended(0.8f, 1f);
            break;
        }
    }

    private void doSurgery()
    {
        var bills = PawnContained.health.surgeryBills.Bills;
        foreach (var bill in bills)
        {
            surgeryBill = bill;
            var list = checkMat();
            if (list == null)
            {
                continue;
            }

            var ingredientList = new List<Thing>();

            foreach (var thing in list)
            {
                if (!surgeryBill.recipe.IsIngredient(thing.def))
                {
                    continue;
                }

                var fixedIngredientCount =
                    surgeryBill.recipe.ingredients.FirstOrDefault(count =>
                        count.IsFixedIngredient && count.FixedIngredient == thing.def);

                if (fixedIngredientCount != null)
                {
                    ingredientList.Add(thing);
                    continue;
                }

                var ingredientsCount =
                    surgeryBill.recipe.ingredients.FirstOrDefault(count =>
                        !count.IsFixedIngredient && count.filter.Allows(thing));

                if (ingredientsCount != null &&
                    ingredientsCount.CountRequiredOfFor(thing.def, surgeryBill.recipe, surgeryBill) >
                    ingredientList.Count(currentThing => currentThing.def == thing.def))
                {
                    ingredientList.Add(thing);
                }
            }

            if (ingredientList.Count < surgeryBill.recipe.ingredients.Count)
            {
                continue;
            }

            ingredients = ingredientList;
            timer = surgeryBill.recipe.workAmount;
            AutoDoc.SetSurgeryInProgress(true);
            break;
        }
    }

    private void drawRect()
    {
        var position = parent.Position;
        var array = deterDimensions();
        position.x += array[2];
        position.z += array[3];
        materialSearch = CellRect.CenteredOn(position, array[0], array[1]);
        materialSearch.DebugDraw();
    }

    private List<Thing> checkMat()
    {
        var list = new List<Thing>();
        foreach (var item in materialSearch)
        {
            if (item.GetFirstItem(ParentMap) != null)
            {
                list.AddRange(item.GetThingList(ParentMap));
            }
        }

        return list;
    }

    public override string CompInspectStringExtra()
    {
        if (surgeryBill == null)
        {
            return "AuDo_NoTask".Translate();
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("AuDo_CurrentBill".Translate(surgeryBill.Label));
        stringBuilder.AppendLine(timer > 0f ? "AuDo_TimeLeft".Translate((int)timer / 10) : "AuDo_Done".Translate());
        stringBuilder.Append("AuDo_Requires".Translate());
        foreach (var ingredient in surgeryBill.recipe.ingredients)
        {
            stringBuilder.Append(ingredient);
        }

        return stringBuilder.ToString();
    }

    private int[] deterDimensions()
    {
        return parent.Rotation.ToString() switch
        {
            "0" => [3, 4, 0, 1],
            "1" => [4, 3, 1, 0],
            "2" => [3, 4, 0, 0],
            _ => [4, 3, 0, 0]
        };
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref timer, "timer", -1f);
        Scribe_References.Look(ref surgeryBill, "surgeryBill");
        Scribe_Collections.Look(ref ingredients, "ingredients", LookMode.Reference);
    }

    public void Reset()
    {
        ingredients = null;
        timer = -1;
        surgeryBill = null;
    }
}