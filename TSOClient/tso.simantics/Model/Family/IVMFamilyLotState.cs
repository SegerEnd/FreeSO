namespace FSO.SimAntics.Model.Family
{
    /// <summary>
    /// Marker for platform states that can host a TS1-style family on the lot.
    /// Implemented by <see cref="TS1Platform.VMTS1LotState"/> (Simitone) and
    /// <see cref="TSOPlatform.VMOfflineLotState"/> (Archive / Sandbox).
    /// </summary>
    public interface IVMFamilyLotState
    {
        VMFamilyComponent Family { get; }
    }
}
