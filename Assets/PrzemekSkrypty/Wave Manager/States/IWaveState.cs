using System.Collections;

namespace ElementumDefense.Waves
{
    /// <summary>
    /// One phase of the wave flow (draft / announce / spawn / mayhem / etc.).
    /// 
    /// A state runs to completion by yielding an IEnumerator. Before returning
    /// it must call <see cref="WaveStateMachine.GoTo"/> to declare the next
    /// state, or leave it null to stop the machine.
    /// 
    /// States hold no long-lived references — they're lightweight and can be
    /// constructed fresh on every transition.
    /// </summary>
    public interface IWaveState
    {
        IEnumerator Run(WaveStateMachine machine);
    }
}
