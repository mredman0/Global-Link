using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadPuzzleButton : MonoBehaviour
{
    public Button Button;
    public TMP_Text ButtonText;
    public PuzzleLoader PuzzleLoader;

    public Sprite LockedSprite;
    public Sprite UncompletedSprite;
    public Sprite CompletedSprite;

    public string PuzzlePack;
    public string PuzzleIdInPack;

    // Start is called before the first frame update
    void Start()
    {
        PuzzleLoader.PuzzlePack = PuzzlePack;
        PuzzleLoader.PuzzleIdInPack = PuzzleIdInPack;
    }

    private void OnEnable()
    {
        SetButtonSpriteBasedOnCompletion();
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

        var completed = PuzzleCompletionManager.Instance.IsPuzzleCompleted(PuzzlePack, PuzzleIdInPack);
        var unlocked = completed || PuzzleCompletionManager.Instance.IsPuzzleUnlocked(PuzzlePack, PuzzleIdInPack);

        Button.interactable = unlocked;
        ButtonText.gameObject.SetActive(unlocked);

        Button.image.sprite =
            completed ? CompletedSprite : unlocked ? UncompletedSprite : LockedSprite;
    }
}
