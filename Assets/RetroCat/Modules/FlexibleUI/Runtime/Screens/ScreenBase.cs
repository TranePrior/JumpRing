namespace RetroCat.Modules.FlexibleUI.Runtime.Screens
{
    public abstract class ScreenBase : ActivityBase
    {
        private void Awake()
        {
            OnInit();
        }

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
