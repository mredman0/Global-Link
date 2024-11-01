using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageButton : Button
{
	public Image Icon;
	public Color IconTintNormal;
	public Color IconTintHighlighted;
	public Color IconTintPressed;
	public Color IconTintSelected;
	public Color IconTintDisabled;

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		base.DoStateTransition(state, instant);

		if(Icon)
		{
			var color = IconTintNormal;
			switch (state)
			{
				case SelectionState.Highlighted:
					color = IconTintHighlighted;
					break;
				case SelectionState.Pressed:
					color = IconTintPressed;
					break;
				case SelectionState.Selected:
					color = IconTintSelected;
					break;
				case SelectionState.Disabled:
					color = IconTintDisabled;
					break;
			}
			Icon.color = color;
		}
	}
}
