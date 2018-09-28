using System;
using InstaSharper.API;

namespace InstaSharper.Classes.Android.DeviceInfo
{
    [Serializable]
    public class AndroidDevice
    {
        public Guid PhoneGuid { get; set; }
        public Guid DeviceGuid { get; set; }
        public Guid GoogleAdId { get; set; } = Guid.NewGuid();
        public Guid RankToken { get; set; } = Guid.NewGuid();


        public string AndroidBoardName { get; set; }
        public string AndroidBootloader { get; set; }
        public string DeviceBrand { get; set; }
        public string DeviceId { get; set; }
        public string DeviceModel { get; set; }
        public string DeviceModelBoot { get; set; }
        public string DeviceModelIdentifier { get; set; }
        public string FirmwareBrand { get; set; }
        public string FirmwareFingerprint { get; set; }
        public string FirmwareTags { get; set; }
        public string FirmwareType { get; set; }
        public string HardwareManufacturer { get; set; }
        public string HardwareModel { get; set; }






        public string AndroidVersion { get; set; }
        public string AndroidRelease { get; set; }
        public string Dpi { get; set; }
        public string Resolution { get; set; }
        public string Manufacturer { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Device { get; set; }
        public string Cpu { get; set; }

        public string UserAgent =>
            string.Format(
                "Instagram {0} Android ({1}/{2}; {3}; {4}; {5}; {6}; {7}; {8}; {9}; {10})",
                InstaApiConstants.IG_VERSION,
                AndroidVersion,
                AndroidRelease,
                Dpi,
                Resolution,
                Manufacturer + (!string.IsNullOrEmpty(Brand) ? "/" + Brand : ""),
                Model,
                Device,
                Cpu,
                InstaApiConstants.USER_AGENT_LOCALE,
                InstaApiConstants.VERSION_CODE
            );

    }
}