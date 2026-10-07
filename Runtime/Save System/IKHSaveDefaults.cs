namespace KH
{
    /// <summary>
    /// Implement on a save class that needs starting values.
    /// Called only when a brand-new save is created, never after loading an existing one.
    /// </summary>
    public interface IKHSaveDefaults
    {
        void InitializeDefaults();
    }
}