namespace NetworkToolBox {
    public class ProfileNetwork {
        public ProfileNetwork() { }

        public string Name { get; set; }
        public bool IPAuto { get; set; }
        public bool DnsAuto { get; set; }
        public string IPAddress { get; set; }
        public string Mask { get; set; }
        public string Gateway { get; set; }
        public string Dns1 { get; set; }
        public string Dns2 { get; set; }
        public override string ToString() {
            return Name;
        }
    }
}