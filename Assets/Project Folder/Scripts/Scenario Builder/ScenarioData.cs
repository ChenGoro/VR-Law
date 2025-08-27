using System.Collections.Generic;

public class ScenarioData
{
    /// <summary>
    /// a container class for all the data nescacary to run a trial.
    /// </summary>
    public ScenarioType ScenarioType { get; private set; }
    public CrimeType CrimeType { get; private set; }

    public int[] LayoutOrder { get; private set; } = new int[4];

    public Photo DefendantPhoto { get; private set; }
    public string DefendantFirstName { get; private set; }
    public string DefendantLastName { get; private set; }

    public Photo VictimPhoto { get; private set; }
    public string VictimFirstName { get; private set; }
    public string VictimLastName { get; private set; }

    public string ScenarioDescription { get; private set; }
    public string ProcecutorStatement { get; private set; }
    public string AttorneyStatement { get; private set; }

    public int ScenarioIndex { get; private set; }

    // Optional: constructor
    public ScenarioData(
    ScenarioTemplate template,
    int[] layoutOrder,
    Photo defendantPhoto,
    string defendantFirstName,
    string defendantLastName,
    Photo victimPhoto,
    string victimFirstName,
    string victimLastName)
    {
        ScenarioType = template.ScenarioType;
        CrimeType = template.CrimeType;
        LayoutOrder = layoutOrder;
        DefendantPhoto = defendantPhoto;
        DefendantFirstName = defendantFirstName;
        DefendantLastName = defendantLastName;
        VictimPhoto = victimPhoto;
        VictimFirstName = victimFirstName;
        VictimLastName = victimLastName;
        ScenarioDescription = template.Description;
        ProcecutorStatement = template.ProsecutorStatement;
        AttorneyStatement = template.AttorneyStatement;
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
/*
 * sub container for the scenario specific data. while photos and names change, scenario tenplate data stays the same for all runs
 */
{
    public string Description { get; private set; }
    public ScenarioType ScenarioType { get; private set; }
    public CrimeType CrimeType { get; private set; }
    public string ProsecutorStatement { get; private set; }
    public string AttorneyStatement { get; private set; }

    public int ScnarioIndex { get; private set; }

    public ScenarioTemplate(
        string description,
        ScenarioType scenarioType,
        CrimeType crimeType,
        string prosecutorStatement,
        string attorneyStatement,
        int scnarioIndex)
    {
        Description = description;
        ScenarioType = scenarioType;
        CrimeType = crimeType;
        ProsecutorStatement = prosecutorStatement;
        AttorneyStatement = attorneyStatement;
        ScnarioIndex = scnarioIndex;
    }

    public void AddNamesToStatements(string defandantFirstName, string defandantLastName, string victimFirstName, string victimLastName)
    {
        Dictionary<string, string> namesCodes = new Dictionary<string, string>
        {
            { "*defandantFirstName*", defandantFirstName },
            { "*defandantLastName*", defandantLastName },
            { "*victimFirstName*", victimFirstName },
            { "*victimLastName*", victimLastName },

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



