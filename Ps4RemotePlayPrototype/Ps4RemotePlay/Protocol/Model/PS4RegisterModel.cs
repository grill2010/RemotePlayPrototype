namespace Ps4RemotePlay.Protocol.Model
{
    public class PS4RegisterModel
    {
        public string ApSsid { get; }

        public string ApBsid { get; }

        public string ApKey { get; }

        public string Name { get; }

        public string Mac { get; }

        public string RegistrationKey { get; }

        public string Nickname { get; }

        public string RpKeyType { get; }

        public string RpKey { get; }

        public string RegisterHeaderInfoComplete { get; }

        public PS4RegisterModel(string apSsid, string apBssid, string apKey, string name, string mac, string registrationKey, string nickname, string rpKeyType, string rpKey, string registerHeaderInfoComplete)
        {
            this.ApSsid = apSsid;
            this.ApBsid = apBssid;
            this.ApKey = apKey;
            this.Name = name;
            this.Mac = mac;
            this.RegistrationKey = registrationKey;
            this.Nickname = nickname;
            this.RpKeyType = rpKeyType;
            this.RpKey = rpKey;
            this.RegisterHeaderInfoComplete = registerHeaderInfoComplete;
        }
    }
}
