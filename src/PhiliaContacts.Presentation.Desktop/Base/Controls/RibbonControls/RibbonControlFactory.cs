using Microsoft.Extensions.DependencyInjection;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using System;

namespace PhiliaContacts.Presentation.Desktop.Base.Controls.RibbonControls;

public sealed class RibbonControlFactory : IRibbonControlFactory
{
    private readonly IServiceProvider _ribbonControlProvider;

    public RibbonControlFactory(IServiceProvider ribbonControlProvider)
    {
        _ribbonControlProvider = ribbonControlProvider ?? throw new ArgumentNullException(nameof(ribbonControlProvider));
    }

    /// <inheritdoc/>
    public BaseRibbonControl? Create(BaseViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        if (viewModel is not IRibbonProvider viewModelWithRibbonContent || !typeof(BaseRibbonControl).IsAssignableFrom(viewModelWithRibbonContent.RibbonControlType))
        {
            return null;
        }

        BaseRibbonControl ribbonControl = (BaseRibbonControl)ActivatorUtilities.CreateInstance(_ribbonControlProvider, viewModelWithRibbonContent.RibbonControlType);
        ribbonControl.DataContext = viewModel;

        return ribbonControl;
    }
}
