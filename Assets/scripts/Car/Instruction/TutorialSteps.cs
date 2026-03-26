using UnityEngine;

public class TutorialStep
{
    public string Instruction;
    public string ActionName;
    public string[] ComboActions;
    public Vector2 Direction;

    public float HoldTime;
    private float timer;

    public bool RequiresHold => HoldTime > 0;
    public bool IsCombo => ComboActions != null;
    public bool HasDirection => Direction != Vector2.zero;
    public bool IsComplete => timer >= HoldTime;

    public TutorialStep(string instruction, string action, float holdTime = 0)
    {
        Instruction = instruction;
        ActionName = action;
        HoldTime = holdTime;
    }

    public TutorialStep(string instruction, string action, Vector2 direction)
    {
        Instruction = instruction;
        ActionName = action;
        Direction = direction;
    }

    public TutorialStep(string instruction, string[] combo, float holdTime)
    {
        Instruction = instruction;
        ComboActions = combo;
        HoldTime = holdTime;
    }

    public void AddTime(float dt) => timer += dt;
    public void Reset() => timer = 0;
}