using System.Threading;

namespace SlopCrew.Plugin;

public class InputBlocker {
    private int shouldIgnoreInputInternal = 0;
    public bool ShouldIgnoreInput {
        get => Interlocked.CompareExchange(ref this.shouldIgnoreInputInternal, 0, 0) == 1;
        set => Interlocked.Exchange(ref this.shouldIgnoreInputInternal, value ? 1 : 0);
    }
}
