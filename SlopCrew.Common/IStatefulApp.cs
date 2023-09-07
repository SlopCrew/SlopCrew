namespace SlopCrew.Common {
    public interface IStatefulApp {
        string GetLabel();
        void OnStart();
        bool IsBusy();
    }
}
