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

    /// <summary>
    /// Brings the activity to a settled closed state before it is shown again. UIActivities calls
    /// this on a reused instance before the caller wires up its per-open subscriptions, so a close
    /// animation that never finished can not swallow them.
    /// </summary>
    public virtual void SettleBeforeOpen()
    {
    }
}