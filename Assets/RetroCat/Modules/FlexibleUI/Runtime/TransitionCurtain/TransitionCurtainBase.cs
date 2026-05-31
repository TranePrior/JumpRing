namespace RetroCat.Modules.FlexibleUI.Runtime.TransitionCurtain
{
    public abstract class TransitionCurtainBase : ActivityBase
    {
        public override void Open()
        {
            OnOpenStarted();
            gameObject.SetActive(true);
            OnOpenFinished();
        }

        public override void Close()
        {
            OnCloseStarted();
            gameObject.SetActive(false);
            OnCloseFinished();
        }
    }
}