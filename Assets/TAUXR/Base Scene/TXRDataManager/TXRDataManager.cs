using System;
using UnityEngine;


#region Analytics Data Classes
public interface AnalyticsDataClass
{
    string TableName { get; }
}

[Serializable]
public class AnalyticsLogLine : AnalyticsDataClass
{
    public string TableName => "TAUXR_Logs";
    public float LogTime;
    public string LogText;

    public AnalyticsLogLine(string line)
    {
        LogTime = Time.time;
        LogText = line;
    }
}

public class BoxesEvents : AnalyticsDataClass
{
    public string TableName => "BoxesEvents";
    public float LogTime;
    public int ScenarioIndex;
    public string BoxType;
    public int BoxLayoutOrder;
    public string Action; // Opened, closed

    public BoxesEvents(int scenarioIndex, string boxType, int boxLayoutOrder, string action)
    {
        LogTime = Time.time;
        ScenarioIndex = scenarioIndex;
        BoxType = boxType;
        BoxLayoutOrder = boxLayoutOrder;
        Action = action;
    }
}

public class ScenarioInfo : AnalyticsDataClass
{
    public string TableName => "ScenarioInfo";
    public float LogTime;
    public int ScenarioIndex;
    public string ScenarioType; // e.g., "Bail", "Sentencing".
    public string CrimeType;
    public string Description;
    public string AttorneysStatement;
    public string ProcecutorsStatement;
    public float AnnualIncome;
    public string DefandantsFirstName;
    public string DefandantsLastName;
    public string DefandantsGender;
    public string DefandantsRace;
    public string DefandantsPhoto;
    public string VictimsGender;
    public string VictimsRace;
    public string VictimsPhoto;

    public ScenarioInfo(ScenarioData scenarioData)
    {
        LogTime = Time.time;
        ScenarioIndex = scenarioData.ScenarioIndex;
        ScenarioType = scenarioData.ScenarioType.ToString();
        CrimeType = scenarioData.CrimeType.ToString();
        Description = scenarioData.ScenarioDescription;
        AttorneysStatement = scenarioData.AttorneyStatement;
        ProcecutorsStatement = scenarioData.ProcecutorStatement;
        AnnualIncome = scenarioData.AnnualIncome;
        DefandantsFirstName = scenarioData.DefendantFirstName;
        DefandantsLastName = scenarioData.DefendantLastName;
        DefandantsGender = scenarioData.DefendantPhoto.Gender.ToString();
        DefandantsRace = scenarioData.DefendantPhoto.Race.ToString();
        DefandantsPhoto = scenarioData.DefendantPhoto.Sprite.name;
        VictimsGender = scenarioData.VictimPhoto.Gender.ToString();
        VictimsRace = scenarioData.VictimPhoto.Race.ToString();
        VictimsPhoto = scenarioData.VictimPhoto.Sprite.name;
    }
}

public class Decisions : AnalyticsDataClass
{
    public string TableName => "Decisions";
    public float LogTime;
    public int ScenarioIndex;
    public string ScenarioType; // e.g., "Bail", "Sentencing".
    public string Decision; // e.g., "ROB", ROR", "Jail".
    public float BailAmount; // (-1) if not applicable.
    public float SentenceLength; // (-1) if not applicable.
    public float RT; // time taken to make the decision, in seconds.

    public Decisions(int scenarioIndex, string scenarioType, string decision, float bailAmount, float sentenceLength, float rt)
    {
        LogTime = Time.time;
        ScenarioIndex = scenarioIndex;
        ScenarioType = scenarioType;
        Decision = decision;
        BailAmount = bailAmount;
        SentenceLength = sentenceLength;
        RT = rt;
    }
}

public class PanelsAndConfirmationEvents : AnalyticsDataClass
{
    public string TableName => "PanelsAndConfirmationEvents";
    public float LogTime;
    public int ScenarioIndex;
    public string PanelName; // e.g., "DecisionPanel", "InstructionsPanel", "FeedbackPanel".
    public string Action; // e.g., "Shown", "Confirmed".
    public PanelsAndConfirmationEvents(int scenarioIndex, string panelName, string action)
    {
        LogTime = Time.time;
        ScenarioIndex = scenarioIndex;
        PanelName = panelName;
        Action = action;
    }
}

// Declare here new AnalyticsDataClasses for every table file output you desire.

#endregion

public class TXRDataManager : TXRSingleton<TXRDataManager>
{
    private static string uniqueParticipantId;
    public static string UniqueParticipantId => uniqueParticipantId;

    // updated from TAUXRPlayer
    private bool exportEyeTracking = false;
    private bool exportFaceTracking = false;

    // automatically switched to true if not in editor.
    [SerializeField]
    private bool shouldExport = false;

    private AnalyticsWriter analyticsWriter;
    private DataContinuousWriter continuousWriter;
    private DataExporterFaceExpression faceExpressionWriter;

    #region Analytics Data Classes
    // declare pointers for all experience-specific analytics classes
    private AnalyticsLogLine logLine;
    private BoxesEvents boxEvent;
    private ScenarioInfo scenarioInfo;
    private Decisions decisions;
    private PanelsAndConfirmationEvents panelsAndConfirmationEvents;

    // write additional events here..


    #endregion

    #region Project Specific Analytics Reporters
    // Write here all the functions you'll want to use to report relevant data.

    // log a new string line with the time logged to TAUXR_Logs file.
    public void LogLineToFile(string line)
    {
        // creates a new instance of AnalyticsLogLine data class. In it's constructor, it gets the line and automatically assign Time.time to the log time.
        logLine = new AnalyticsLogLine(line);

        // tells the analytics writer to write a new line in file.
        WriteAnalyticsToFile(logLine);
    }

    // log a box event to BoxesEvents file.
    public void ReportBoxEvent(int scenarioIndex, string boxType, int boxLayoutOrder, string action)
    {
        boxEvent = new BoxesEvents(scenarioIndex, boxType, boxLayoutOrder, action);
        WriteAnalyticsToFile(boxEvent);
    }

    // log the scenario info to ScenarioInfo file.
    public void ReportScenarioInfo(ScenarioData scenarioData)
    {
        scenarioInfo = new ScenarioInfo(scenarioData);
        WriteAnalyticsToFile(scenarioInfo);
    }

    // log a decision to Decisions file.
    public void ReportDecision(int scenarioIndex, string scenarioType, string decision, float bailAmount, float sentenceLength, float fineAmount, float rt)
    {
        decisions = new Decisions(scenarioIndex, scenarioType, decision, bailAmount, sentenceLength, rt);
        WriteAnalyticsToFile(decisions);
    }

    // log panel show/confirm events to PanelsAndConfirmationEvents file.
    public void ReportPanelOrConfirmationEvent(int scenarioIndex, string panelName, string action)
    {
        panelsAndConfirmationEvents = new PanelsAndConfirmationEvents(scenarioIndex, panelName, action);
        WriteAnalyticsToFile(panelsAndConfirmationEvents);
    }
    #endregion

    private void WriteAnalyticsToFile(AnalyticsDataClass analyticsDataClass)
    {
        if (!shouldExport) return;

        analyticsWriter.WriteAnalyticsDataFile(analyticsDataClass);
    }

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        // set a run specific Id
        uniqueParticipantId = "#" + KeyGenerator.GetUniqueKey(4);

        shouldExport = ShouldExportData();
        if (!shouldExport) return;

        exportEyeTracking = TXRPlayer.Instance.IsEyeTrackingEnabled;
        exportFaceTracking = TXRPlayer.Instance.IsFaceTrackingEnabled;

        analyticsWriter = new AnalyticsWriter();

        // for now, instead of making the whole interface in the datamanager, it will split between the different scripts.
        continuousWriter = GetComponent<DataContinuousWriter>();
        continuousWriter.Init(exportEyeTracking);

        if (exportFaceTracking)
        {
            faceExpressionWriter = GetComponent<DataExporterFaceExpression>();
            faceExpressionWriter.Init();
        }
    }

    // default data export on false in editor. always export on build.
    private bool ShouldExportData()
    {
        if (Application.isEditor && !shouldExport)
        {
            Debug.Log("Data Manager won't export data because it is running in editor. To export, manually enable ShouldExport");
        }
        return shouldExport || !Application.isEditor;
    }

    private void FixedUpdate()
    {
        if (!shouldExport) return;

        continuousWriter.RecordContinuousData();

        if (exportFaceTracking)
        {
            faceExpressionWriter.CollectWriteDataToFile();
        }
    }

    private void OnApplicationQuit()
    {
        if (!shouldExport) return;

        analyticsWriter.Close();
        continuousWriter.Close();
        faceExpressionWriter.Close();
    }

}

