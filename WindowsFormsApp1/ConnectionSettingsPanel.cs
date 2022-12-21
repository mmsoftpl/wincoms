using SDKTemplate;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFiDirect;
using Windows.UI.Core;

namespace WindowsFormsApp1
{
    public partial class ConnectionSettingsPanel : UserControl
    {
        public ConnectionSettingsPanel()
        {
            InitializeComponent();
            
            // Initialize the GroupOwnerIntent combo box.
            // The options are "Default", then values 0 through 15.
            //cmbGOIntent.Items.Add(-1);
            for (int i = 0; i <= 15; i++)
            {
                cmbGOIntent.Items.Add((short?)i);
            }
            cmbGOIntent.SelectedIndex = -1;


            cmbPreferredPairingProcedure.Items.Add(WiFiDirectPairingProcedure.GroupOwnerNegotiation);
            cmbPreferredPairingProcedure.Items.Add(WiFiDirectPairingProcedure.Invitation);
            cmbPreferredPairingProcedure.SelectedIndex = 1;
        }

        static List<WiFiDirectConfigurationMethod> _supportedConfigMethods = new List<WiFiDirectConfigurationMethod>();


        public static async Task<bool> RequestPairDeviceAsync(DeviceInformationPairing pairing)
        {
            WiFiDirectConnectionParameters connectionParams = new WiFiDirectConnectionParameters();

            short? groupOwnerIntent = null;//Utils.GetSelectedItemTag<short?>(cmbGOIntent);
            if (groupOwnerIntent.HasValue)
            {
                connectionParams.GroupOwnerIntent = groupOwnerIntent.Value;
            }

            DevicePairingKinds devicePairingKinds = DevicePairingKinds.None;

            // If specific configuration methods were added, then use them.
            if (_supportedConfigMethods.Count > 0)
            {
                foreach (var configMethod in _supportedConfigMethods)
                {
                    connectionParams.PreferenceOrderedConfigurationMethods.Add(configMethod);
                    devicePairingKinds |= WiFiDirectConnectionParameters.GetDevicePairingKinds(configMethod);
                }
            }
            else
            {
                // If specific configuration methods were not added, then we'll use these pairing kinds.
                devicePairingKinds = DevicePairingKinds.ConfirmOnly;// | DevicePairingKinds.DisplayPin | DevicePairingKinds.ProvidePin;
            }

            connectionParams.PreferredPairingProcedure = WiFiDirectPairingProcedure.GroupOwnerNegotiation;// Utils.GetSelectedItemTag<WiFiDirectPairingProcedure>(cmbPreferredPairingProcedure);
            DeviceInformationCustomPairing customPairing = pairing.Custom;
            customPairing.PairingRequested += OnPairingRequested;
//            customPairing.

            DevicePairingResult result = await customPairing.PairAsync(devicePairingKinds, DevicePairingProtectionLevel.Default, connectionParams);
            if (result.Status == DevicePairingResultStatus.AlreadyPaired)
            {
                await pairing.UnpairAsync();
                result = await customPairing.PairAsync(devicePairingKinds, DevicePairingProtectionLevel.Default, connectionParams);
            }

            if (result.Status != DevicePairingResultStatus.Paired)
            {                
                MainPage.Log($"PairAsync failed, Status: {result.Status}", NotifyType.ErrorMessage);
                return false;
            }
            return true;
        }

        public void Reset()
        {
            _supportedConfigMethods.Clear();
        }

        private static void OnPairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            CoreDispatcher Dispatcher = null;

            Utils.HandlePairing(Dispatcher, args);
        }
    }
}
