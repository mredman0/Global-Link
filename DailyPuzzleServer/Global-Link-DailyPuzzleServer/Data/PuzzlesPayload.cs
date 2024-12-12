
[Serializable]
public class PuzzlesPayload
{
	public PuzzleConfigPayload[] Puzzles;

	public PuzzlesPayload(List<PuzzleConfigPayload> puzzles)
	{
		Puzzles = puzzles.ToArray();
	}
}