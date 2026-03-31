using System.Collections.Generic;
using UnityEngine;

public class TutorialController
{
    private readonly TutorialInputHandler input;

    private List<TutorialStep> steps;
    private int currentStep;

    public bool IsRunning { get; private set; }

    public TutorialStep CurrentStep => steps[currentStep];
    public int StepIndex => currentStep;
    public int StepCount => steps.Count;

    public TutorialController(TutorialInputHandler input)
    {
        this.input = input;
    }

    public void InitializeSteps()
    {
        steps = new List<TutorialStep>
        {
            new("Drive forward", "MoveForward", holdTime: 0.25f),
            new("Brake", "Brake", holdTime:0.25f),
            new("Steer left", "Move", Vector2.left),
            new("Steer right", "Move", Vector2.right),
            new("Drift", "Drift", holdTime:0.25f),
            new("Turbo", "Turbo", holdTime:0.25f),
            new("Turbo + Drift", new[] { "Turbo", "Drift" },0.25f),
            new("Respawn", "Respawn")
        };
    }

    public void Start()
    {
        currentStep = 0;
        IsRunning = true;
    }

    public void Update()
    {
        if (!IsRunning) return;

        var step = steps[currentStep];

        if (HandleStep(step))
            Advance();
    }

    private bool HandleStep(TutorialStep step)
    {
        if (step.IsCombo)
            return UpdateHold(step, input.CheckCombo(step.ComboActions));

        if (step.HasDirection)
            return UpdateHold(step, input.CheckDirection(step.ActionName, step.Direction));

        if (step.RequiresHold)
            return UpdateHold(step, input.IsPressed(step.ActionName));

        return input.WasPressed(step.ActionName);
    }

    private bool UpdateHold(TutorialStep step, bool condition)
    {
        if (!condition)
        {
            step.Reset();
            return false;
        }

        step.AddTime(Time.unscaledDeltaTime);
        return step.IsComplete;
    }

    private void Advance()
    {
        currentStep++;

        if (currentStep >= steps.Count)
            IsRunning = false;
    }

    public void Skip()
    {
        currentStep = steps.Count;
        IsRunning = false;
    }

    public void DebugNext()
    {
        if (currentStep < steps.Count - 1)
            currentStep++;
    }

    public void DebugPrevious()
    {
        if (currentStep > 0)
            currentStep--;
    }
}