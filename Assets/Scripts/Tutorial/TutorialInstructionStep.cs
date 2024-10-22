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
    }

    private void GetCurrentPuzzle()
    {
        Puzzle = Puzzle.Current;
        if (!Puzzle)
        {
            Debug.LogWarning("No current puzzle for tutorial instructions to be based off of!");
        }
    }

    // Update is called once per frame
    void Update()
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
}
