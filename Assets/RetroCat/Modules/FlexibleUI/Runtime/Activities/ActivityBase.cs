using UnityEngine;

public abstract class ActivityBase : MonoBehaviour
{
    protected abstract void OnInit();
    protected abstract void OnOpenStarted();
    protected abstract void OnOpenFinished();
    protected abstract void OnCloseStarted();
    protected abstract void OnCloseFinished();
    public abstract void Open();
    public abstract void Close();
}