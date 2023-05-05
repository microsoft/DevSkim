namespace Microsoft.DevSkim.VisualStudio
{
    using Microsoft.VisualStudio.Settings;
    using Microsoft.VisualStudio.Shell;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    internal class VisualStudioSettingsManager
    {
        private ISettingsManager _settingsManager;

        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { }

        public VisualStudioSettingsManager([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _settingsManager = serviceProvider.GetService(typeof(SVsSettingsPersistenceManager)) as ISettingsManager;

            var props = typeof(GeneralOptionsPage).GetProperties().Select(x => (x.Name, x.PropertyType));

            var setting = _settingsManager.GetSubset("Microsoft.DevSkim.VisualStudio.*");
            //changed method
            //setting.SettingChangedAsync += async (sender, args) => await StaticSettings.PushAsync();
        }
    }
}
