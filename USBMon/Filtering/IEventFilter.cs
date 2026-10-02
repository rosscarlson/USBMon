using USBMon.Models;

namespace USBMon.Filtering;

/// <summary>
/// A predicate applied to the display list only — the captured event model is
/// never touched by filtering. No filters are defined for v1; this is the seam
/// a future filter bar hooks into.
/// </summary>
public interface IEventFilter
{
    bool Matches(DeviceRecord record);
}
