// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using EnvDTE;
using System;
using System.Collections.Generic;

namespace Microsoft.NewProjectWizard
{
    public interface IWizardData
    {
        /// <summary>
        /// Gets or sets the Visual Studio automation object.
        /// </summary>
        DTE DTE { get; set; }

        /// <summary>
        /// Attempts to execute the actions for the wizard.
        /// </summary>
        /// <returns>Value indicating whether the wizard finished successfully.</returns>
        bool OnTryFinish();
    }
}
