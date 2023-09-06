namespace SlopCrew.Common {
    public interface IStatefullApp {
        string GetLabel();
        void OnStart();
        bool IsBusy();
    }
}
