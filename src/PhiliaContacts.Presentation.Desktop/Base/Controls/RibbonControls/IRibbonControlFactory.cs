using RunnethOverStudio.AppToolkit.Modules.ComponentModel;

namespace PhiliaContacts.Presentation.Desktop.Base.Controls.RibbonControls;

public interface IRibbonControlFactory
{
    /// <summary>
    /// Creates a ribbon control based on the provided view model.
    /// </summary>
    /// <param name="viewModel">The view model for which to create a ribbon control.</param>
    /// <returns>A ribbon control corresponding to the provided view model, or null if none is available.</returns>
    BaseRibbonControl? Create(BaseViewModel viewModel);
}
