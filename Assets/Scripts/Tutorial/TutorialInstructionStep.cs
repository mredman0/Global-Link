using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TutorialInstructionStep : MonoBehaviour
{
    public TutorialInstructionStep Next;
    public bool LockCameraInput;
    public bool LockPuzzleInput;

    public List<int> SolveColorsOnShown;

    protected Puzzle Puzzle { get; set; }

    private void OnEnable()
    {
        if(!Puzzle)
        {
            GetCurrentPuzzle();
        }
        if(LockCameraInput)
        {
            Puzzle.CameraController.LockInput();
        }
        if(LockPuzzleInput)
        {
            Puzzle.LockInput();
        }
        SolveColors();
        OnShown();
    }

    private void OnDisable()
    {
        if(LockCameraInput)
        {
            Puzzle.CameraController.FreeInput();
        }
        if (LockPuzzleInput)
        {
            Puzzle.FreeInput();
        }
        OnHidden();
    }

    protected virtual void OnShown() { }
    protected virtual void OnHidden() { }

    private void GetCurrentPuzzle()
    {
        Puzzle = Puzzle.Current;
        if (!Puzzle)
        {
            Debug.LogWarning("No current puzzle for tutorial instructions to be based off of!");
        }
    }

    // Update is called once per frame
    protected void Update()
    {
        if(ShouldGoToNextStep())
        {
            if(Next)
            {
                Next.gameObject.SetActive(true);
            }
            gameObject.SetActive(false);
        }
    }

    protected abstract bool ShouldGoToNextStep();

    private void SolveColors()
    {
        if(!Puzzle)
        {
            return;
        }
        foreach(var color in SolveColorsOnShown)
        {
            Puzzle.SolveColor(color);
        }
    }
}
