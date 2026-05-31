using System;
using UnityEngine.UI;

namespace RetroCat.Modules.Core.UI.Contols.Buttons
{
    public class ExtendedButton : Button
    {
        public event Action<ButtonState> StateChanged;

        public ButtonState State => Map(currentSelectionState);

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            ButtonState mapState = ButtonState.Normal;

            switch (state)
            {
                case SelectionState.Normal:
                    mapState = ButtonState.Normal;
                    break;
                case SelectionState.Highlighted:
                    mapState = ButtonState.Highlighted;
                    break;
                case SelectionState.Pressed:
                    mapState = ButtonState.Pressed;
                    break;
                case SelectionState.Selected:
                    mapState = ButtonState.Selected;
                    break;
                case SelectionState.Disabled:
                    mapState = ButtonState.Disabled;
                    break;
            }

            StateChanged?.Invoke(mapState);
        }

        private ButtonState Map(SelectionState state)
        {
            return state switch
            {
                SelectionState.Normal => ButtonState.Normal,
                SelectionState.Highlighted => ButtonState.Highlighted,
                SelectionState.Selected => ButtonState.Selected,
                SelectionState.Pressed => ButtonState.Pressed,
                SelectionState.Disabled => ButtonState.Disabled,
                _ => throw new NotImplementedException(),
            };
        }
    }
}