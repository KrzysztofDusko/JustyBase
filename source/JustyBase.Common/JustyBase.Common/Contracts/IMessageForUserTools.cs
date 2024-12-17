namespace JustyBase.Common.Contracts;

public interface IMessageForUserTools
{
    void ShowSimpleMessageBoxInstance(Exception ex);
    void ShowSimpleMessageBoxInstance(string messageForUser, string title = "Information");
    void FlashWindowExIfNeeded();
    void DispatcherActionInstance(Action actionToDispatch);
    void DispatcherActionInstance(Action actionToDispatch, object dispatcherPriority);
    void ScreenShot();
    void ShowOrShowInExplorerHelper(string path, string? argOverRide = null);
    void OpenInExplorerHelper(string path);
}
