using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace AutoDoc;

internal class CompAutoDoc : ThingComp
{
    private List<Thing> ingredients;
    private CellRect materialSearch;

    private Bill_Medical surgeryBill;

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
            completeSurgery();
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
            if (bill is not Bill_Medical medicalBill)
            {
                continue;
            }

            var availableIngredients = checkMat();
            if (availableIngredients == null)
            {
                continue;
            }

            if (!tryBuildIngredientList(medicalBill, availableIngredients, out var ingredientList))
            {
                continue;
            }

            surgeryBill = medicalBill;
            ingredients = ingredientList;
            timer = surgeryBill.recipe.workAmount;
            AutoDoc.SetSurgeryInProgress(true);
            break;
        }
    }

    private void completeSurgery()
    {
        if (surgeryBill == null || PawnContained == null)
        {
            return;
        }

        try
        {
            surgeryBill.iterationCompleted = true;
            if (isRemoveBodyPartRecipe(surgeryBill))
            {
                spawnRemovedBodyParts();
                surgeryBill.recipe.Worker.ApplyOnPawn(PawnContained, surgeryBill.Part, null, ingredients, surgeryBill);
            }
            else
            {
                surgeryBill.recipe.Worker.ApplyOnPawn(PawnContained, surgeryBill.Part, PawnContained, ingredients,
                    surgeryBill);
            }

            consumeIngredients();
            if (PawnContained.RaceProps.IsFlesh)
            {
                PawnContained.records.Increment(RecordDefOf.OperationsReceived);
            }
        }
        catch
        {
            // ignored
        }

        if (!surgeryBill.DeletedOrDereferenced && surgeryBill.billStack != null)
        {
            surgeryBill.billStack.Delete(surgeryBill);
        }
    }

    private bool isRemoveBodyPartRecipe(Bill_Medical bill)
    {
        return bill.recipe.Worker is Recipe_RemoveBodyPart ||
               bill.recipe.Worker.GetType().IsSubclassOf(typeof(Recipe_RemoveBodyPart));
    }

    private void spawnRemovedBodyParts()
    {
        if (surgeryBill?.Part == null || ParentMap == null)
        {
            return;
        }

        var spawnCell = materialSearch.RandomCell;
        if (surgeryBill.Part.def.spawnThingOnRemoved != null)
        {
            MedicalRecipesUtility.SpawnNaturalPartIfClean(PawnContained, surgeryBill.Part, spawnCell, ParentMap);
        }

        MedicalRecipesUtility.SpawnThingsFromHediffs(PawnContained, surgeryBill.Part, spawnCell, ParentMap);
    }

    private bool tryBuildIngredientList(Bill_Medical bill, List<Thing> availableThings, out List<Thing> ingredientList)
    {
        ingredientList = new List<Thing>();
        var availableCountByThing = new Dictionary<Thing, int>();
        foreach (var availableThing in availableThings)
        {
            if (availableThing == null || availableThing.Destroyed)
            {
                continue;
            }

            availableCountByThing[availableThing] = availableThing.stackCount;
        }

        if (!bill.uniqueRequiredIngredients.NullOrEmpty())
        {
            foreach (var uniqueIngredient in bill.uniqueRequiredIngredients)
            {
                if (uniqueIngredient == null || uniqueIngredient.Destroyed ||
                    !availableCountByThing.TryGetValue(uniqueIngredient, out var count) || count < 1)
                {
                    ingredientList = null;
                    return false;
                }

                ingredientList.Add(uniqueIngredient);
                availableCountByThing[uniqueIngredient] = count - 1;
            }
        }

        foreach (var ingredientCount in bill.recipe.ingredients)
        {
            var requiredAmount = bill.recipe.Worker.GetIngredientCount(ingredientCount, bill);
            if (requiredAmount <= 0f)
            {
                continue;
            }

            var matchingThings = availableCountByThing.Keys
                .Where(thing => availableCountByThing[thing] > 0 && ingredientCount.filter.Allows(thing))
                .OrderByDescending(thing => bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def))
                .ToList();

            foreach (var thing in matchingThings)
            {
                var availableUnits = availableCountByThing[thing];
                if (availableUnits <= 0)
                {
                    continue;
                }

                var valuePerUnit = bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def);
                if (valuePerUnit <= 0f)
                {
                    continue;
                }

                var neededUnits = Math.Max(1, (int)Math.Ceiling(requiredAmount / valuePerUnit));
                var takeUnits = Math.Min(availableUnits, neededUnits);
                for (var i = 0; i < takeUnits; i++)
                {
                    ingredientList.Add(thing);
                }

                availableCountByThing[thing] = availableUnits - takeUnits;
                requiredAmount -= takeUnits * valuePerUnit;
                if (requiredAmount <= 0.001f)
                {
                    break;
                }
            }

            if (!(requiredAmount > 0.001f))
            {
                continue;
            }

            ingredientList = null;
            return false;
        }

        return true;
    }

    private void consumeIngredients()
    {
        if (ingredients == null || surgeryBill == null)
        {
            return;
        }

        var groupedIngredients = ingredients
            .Where(thing => thing != null && !thing.Destroyed)
            .GroupBy(thing => thing);

        foreach (var ingredientGroup in groupedIngredients)
        {
            var sourceThing = ingredientGroup.Key;
            var consumeCount = Math.Min(sourceThing.stackCount, ingredientGroup.Count());
            if (consumeCount <= 0)
            {
                continue;
            }

            Thing thingToConsume;
            if (consumeCount >= sourceThing.stackCount)
            {
                thingToConsume = sourceThing;
            }
            else
            {
                thingToConsume = sourceThing.SplitOff(consumeCount);
            }

            surgeryBill.recipe.Worker.ConsumeIngredient(thingToConsume, surgeryBill.recipe, ParentMap);
        }
    }

    private void drawRect()
    {
        var position = parent.Position;
        var array = deterDimensions();
        position.x += array[2];
        position.z += array[3];
        materialSearch = CellRect.CenteredOn(position, array[0], array[1]);
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

        var ingredientsText = surgeryBill.recipe.ingredients.Select(ingredient => ingredient.ToString()).ToCommaList();
        var requiresLabel = "AuDo_Requires".Translate().ToString().TrimEnd();
        if (!ingredientsText.NullOrEmpty())
        {
            stringBuilder.Append(requiresLabel).Append(' ').Append(ingredientsText);
        }
        else
        {
            stringBuilder.Append(requiresLabel);
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