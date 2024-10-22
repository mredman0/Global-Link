using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadPuzzleButton : MonoBehaviour
{
    public Button Button;
    public PuzzleLoader PuzzleLoader;

    public Sprite UncompletedSprite;
    public Sprite CompletedSprite;

    public string PuzzlePack;
    public string PuzzleIdInPack;

    // Start is called before the first frame update
    void Start()
    {
        SetButtonSpriteBasedOnCompletion();
        PuzzleLoader.PuzzlePack = PuzzlePack;
        PuzzleLoader.PuzzleIdInPack = PuzzleIdInPack;
    }

    public void SetButtonSpriteBasedOnCompletion()
    {
        if(string.IsNullOrWhiteSpace(PuzzlePack) || string.IsNullOrWhiteSpace(PuzzleIdInPack))
        {
            Button.image.sprite = UncompletedSprite;
            return;
        }
        if(!PuzzleCompletionManager.Instance)
        {
            Button.image.sprite = UncompletedSprite;
            return;
        }
        Button.image.sprite = PuzzleCompletionManager.Instance.IsPuzzleCompleted($"{PuzzlePack}_{PuzzleIdInPack}") ? CompletedSprite : UncompletedSprite;
    }
}
