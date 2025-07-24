using System.Collections.Generic;
using UnityEngine;

public class ScenarioData
{
    public ScenarioType ScenarioType { get; private set; }
    public CrimeType CrimeType { get; private set; }

    public int[] LayoutOrder { get; private set; } = new int[4];

    public Sprite DefendantPhoto { get; private set; }
    public string DefendantFirstName { get; private set; }
    public string DefendantLastName { get; private set; }

    public Sprite VictimPhoto { get; private set; }
    public string VictimFirstName { get; private set; }
    public string VictimLastName { get; private set; }

    public string ScenarioDescription { get; private set; }
    public string ProcecutorStatement { get; private set; }
    public string AttorneyStatement { get; private set; }

    // Optional: constructor
    public ScenarioData(
    ScenarioTemplate template,
    int[] layoutOrder,
    Sprite defendantPhoto,
    string defendantFirstName,
    string defendantLastName,
    Sprite victimPhoto,
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

        public ScenarioTemplate(
            string description,
            ScenarioType scenarioType,
            CrimeType crimeType,
            string prosecutorStatement,
            string attorneyStatement)
        {
            Description = description;
            ScenarioType = scenarioType;
            CrimeType = crimeType;
            ProsecutorStatement = prosecutorStatement;
            AttorneyStatement = attorneyStatement;
        }

        public void AddNamesToStatements(string defandantFirstName, string defandantLastName, string victimFirstName, string victimLastName)
        {
            Dictionary<string, string> namesCodes = new Dictionary<string, string>
        {
            { "*defandantFirstName*", defandantFirstName },
            { "*defandantLastName*", defandantLastName },
            { "*victimFirstName*", victimFirstName },
            { "*victimLastName*", victimLastName }
        };

            foreach (var pair in namesCodes)
            {
                ProsecutorStatement = ProsecutorStatement.Replace(pair.Key, pair.Value);
                AttorneyStatement = AttorneyStatement.Replace(pair.Key, pair.Value);
            }
        }
    }



