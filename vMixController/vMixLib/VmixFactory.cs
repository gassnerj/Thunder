using System.Globalization;

namespace vMixLib;

public class VmixFactory
{
    public Vmix CreateInstance(string inputId, string selectedName)
    {
        var vmix = new Vmix();
        vmix.Function     = VmixFunctions.SetText.ToString();
        vmix.InputId      = inputId;
        vmix.SelectedName = selectedName;
        vmix.Value        = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        
        vmix.UpdateUrl();
        return vmix;
    }
}