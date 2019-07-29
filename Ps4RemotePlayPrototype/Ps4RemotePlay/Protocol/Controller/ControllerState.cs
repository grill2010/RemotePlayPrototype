namespace Ps4RemotePlay.Protocol.Controller
{
    public class ControllerState
    {
        public uint Buttons { get; set; }

        public byte L2State { get; set; }

        public byte R2State { get; set; }

        public short LeftX { get; set; }
        public short LeftY { get; set; }

        public short RightX { get; set; }
        public short RightY { get; set; }

        public ControllerState()
        {
            this.Buttons = 0;
            this.L2State = 0;
            this.R2State = 0;
            this.LeftX = 0;
            this.LeftY = 0;
            this.RightX = 0;
            this.RightY = 0;
        }
    }
}
