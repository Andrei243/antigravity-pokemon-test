using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

public class MoveData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PokemonType Type { get; set; }
    public MoveCategory Category { get; set; }
    public int Power { get; set; }
    public int Accuracy { get; set; } // 1-100, or 0 for never-miss
    public int MaxPP { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Priority { get; set; } = 0;
    public string Description { get; set; } = string.Empty;

    // Secondary effect definitions
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public StatusCondition InflictStatus { get; set; } = StatusCondition.None;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StatusChancePercent { get; set; } = 0;
    public StatType? TargetStatChange { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StatStageAmount { get; set; } = 0; // +1, -1, etc.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool StatChangeTargetSelf { get; set; } = false;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int StatChangeChancePercent { get; set; } = 0;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int RecoilPercent { get; set; } = 0;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int DrainPercent { get; set; } = 0;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int CritStage { get; set; } = 0;
}

public class Move
{
    public MoveData Data { get; }
    public int CurrentPP { get; set; }
    public int MaxPP => Data.MaxPP;

    public string Name => Data.Name;
    public PokemonType Type => Data.Type;
    public MoveCategory Category => Data.Category;
    public int Power => Data.Power;
    public int Accuracy => Data.Accuracy;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Priority => Data.Priority;
    public string Description => Data.Description;

    public Move(MoveData data)
    {
        Data = data;
        CurrentPP = data.MaxPP;
    }

    public Move(MoveData data, int currentPP)
    {
        Data = data;
        CurrentPP = currentPP;
    }

    public void RestorePP()
    {
        CurrentPP = MaxPP;
    }
}
