using System.Collections.Generic;
using UnityEngine;

/// <summary>Text plus optional audio and word-times JSON for one narrated segment (description, prosecutor, or attorney).</summary>
public class NarrativeBlock
{
    public string Text { get; private set; }
    public AudioClip AudioClip { get; private set; }
    public string WordTimesJson { get; private set; }

    public NarrativeBlock(string text, AudioClip audioClip, string wordTimesJson)
    {
        Text = text ?? "";
        AudioClip = audioClip;
        WordTimesJson = wordTimesJson ?? "";
    }
}

public class ScenarioData
{
    public ScenarioType ScenarioType { get; private set; }
    public CrimeType CrimeType { get; private set; }

    public int[] LayoutOrder { get; private set; } = new int[4];

    public Photo DefendantPhoto { get; private set; }
    public string DefendantFirstName { get; private set; }
    public string DefendantLastName { get; private set; }

    public Photo VictimPhoto { get; private set; }

    public string ScenarioDescription { get; private set; }
    public string ProcecutorStatement { get; private set; }
    public string AttorneyStatement { get; private set; }

    public NarrativeBlock DescriptionBlock { get; private set; }
    public NarrativeBlock ProsecutorBlock { get; private set; }
    public NarrativeBlock AttorneyBlock { get; private set; }

    public float AnnualIncome { get; private set; }
    public int ScenarioIndex { get; private set; }

    public ScenarioData(
        ScenarioTemplate template,
        int[] layoutOrder,
        Photo defendantPhoto,
        string defendantFirstName,
        string defendantLastName,
        Photo victimPhoto)
    {
        ScenarioType = template.ScenarioType;
        CrimeType = template.CrimeType;
        LayoutOrder = layoutOrder;
        DefendantPhoto = defendantPhoto;
        DefendantFirstName = defendantFirstName;
        DefendantLastName = defendantLastName;
        VictimPhoto = victimPhoto;
        ScenarioDescription = template.Description;
        ProcecutorStatement = template.ProsecutorStatement;
        AttorneyStatement = template.AttorneyStatement;
        DescriptionBlock = new NarrativeBlock(template.Description, template.DescriptionBlock?.AudioClip, template.DescriptionBlock?.WordTimesJson);
        ProsecutorBlock = new NarrativeBlock(template.ProsecutorStatement, template.ProsecutorBlock?.AudioClip, template.ProsecutorBlock?.WordTimesJson);
        AttorneyBlock = new NarrativeBlock(template.AttorneyStatement, template.AttorneyBlock?.AudioClip, template.AttorneyBlock?.WordTimesJson);
        AnnualIncome = template.AnnualIncome;
        ScenarioIndex = template.ScnarioIndex;
    }
}


public enum ScenarioType
{
    Bail, Sentencing
}

public enum CrimeType
{
    Burglary, Murder, Rape, VehicularManslaughter, Fraud
}



public class ScenarioTemplate
{
    public string Description { get; private set; }
    public ScenarioType ScenarioType { get; private set; }
    public CrimeType CrimeType { get; private set; }
    public string ProsecutorStatement { get; private set; }
    public string AttorneyStatement { get; private set; }
    public NarrativeBlock DescriptionBlock { get; private set; }
    public NarrativeBlock ProsecutorBlock { get; private set; }
    public NarrativeBlock AttorneyBlock { get; private set; }
    public float AnnualIncome { get; private set; }
    public int ScnarioIndex { get; private set; }

    public ScenarioTemplate(
        string description,
        ScenarioType scenarioType,
        CrimeType crimeType,
        string prosecutorStatement,
        string attorneyStatement,
        NarrativeBlock descriptionBlock,
        NarrativeBlock prosecutorBlock,
        NarrativeBlock attorneyBlock,
        float annualIncome,
        int scnarioIndex)
    {
        Description = description;
        ScenarioType = scenarioType;
        CrimeType = crimeType;
        ProsecutorStatement = prosecutorStatement;
        AttorneyStatement = attorneyStatement;
        DescriptionBlock = descriptionBlock ?? new NarrativeBlock(description, null, null);
        ProsecutorBlock = prosecutorBlock ?? new NarrativeBlock(prosecutorStatement, null, null);
        AttorneyBlock = attorneyBlock ?? new NarrativeBlock(attorneyStatement, null, null);
        AnnualIncome = annualIncome;
        ScnarioIndex = scnarioIndex;
    }

    public void AddNamesToStatements(string defandantFirstName, string defandantLastName)
    {
        Dictionary<string, string> namesCodes = new Dictionary<string, string>
        {
            { "*defandantFirstName*", defandantFirstName },
            { "*defandantLastName*", defandantLastName },

            { "John", defandantFirstName },
            { "Doe", defandantLastName },
        };

        foreach (var pair in namesCodes)
        {
            ProsecutorStatement = ProsecutorStatement.Replace(pair.Key, pair.Value);
            AttorneyStatement = AttorneyStatement.Replace(pair.Key, pair.Value);
            Description = Description.Replace(pair.Key, pair.Value);
        }
    }
}



