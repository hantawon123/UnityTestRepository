namespace Game.Core.Ports
{
    /// <summary>
    /// Lets the player hear whether a microphone works before committing to
    /// it: 마이크 테스트 on the 사운드 tab.
    /// </summary>
    /// <remarks>
    /// What "hearing whether it works" means — playing the voice straight
    /// back, or showing how loud it is — has not been decided, so this only
    /// says when the test runs and on which device. The screen's button is
    /// wired to it either way, and the decision changes the implementation
    /// rather than the screen.
    /// </remarks>
    public interface IMicrophoneTest
    {
        bool IsRunning { get; }

        /// <param name="deviceName">
        /// The device being considered, by the machine's name for it, or
        /// <see cref="Settings.SoundCatalog.DefaultDevice"/>. The one in the
        /// draft rather than the applied one: the point is to try before
        /// applying.
        /// </param>
        void Start(string deviceName);

        void Stop();
    }
}
