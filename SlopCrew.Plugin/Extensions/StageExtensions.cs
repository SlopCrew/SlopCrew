using Reptile;

namespace SlopCrew.Plugin.Extensions {
    public static class StageExtensions {

        public static Stage ToBRCStage(this int stage) {
            return (Stage) stage;
        }
    }
}
