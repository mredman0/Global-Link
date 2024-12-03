using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TutorialInstructionStep : MonoBehaviour
{
    public TutorialInstructionStep Next;
    public bool LockCameraInput;
    public bool LockPuzzleInput;

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
            gameObject.SetActive(false);
            if (Next)
            {
                Next.gameObject.SetActive(true);
            }
        }
    }

    protected abstract bool ShouldGoToNextStep();
}
