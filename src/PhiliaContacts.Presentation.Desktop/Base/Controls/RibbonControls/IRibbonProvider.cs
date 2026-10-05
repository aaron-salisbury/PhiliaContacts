using System;

namespace PhiliaContacts.Presentation.Desktop.Base.Controls.RibbonControls;

public interface IRibbonProvider
{
    Type RibbonControlType { get; }
}
