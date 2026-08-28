public interface IToggleableView
{
    bool IsOpen { get; }
    void ToggleVisibility();
    void Open();
    void Close();
}
